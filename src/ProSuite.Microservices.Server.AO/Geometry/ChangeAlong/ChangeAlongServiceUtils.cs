using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.Geometry.ChangeAlong
{
	public static class ChangeAlongServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static CalculateReshapeLinesResponse CalculateReshapeLines(
			[NotNull] CalculateReshapeLinesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			Stopwatch watch = Stopwatch.StartNew();

			IList<IFeature> sourceFeatures =
				ProtobufConversionUtils.FromGdbObjectMsgList(request.SourceFeatures,
				                                             request.ClassDefinitions);

			IList<IFeature> targetFeatures =
				ProtobufConversionUtils.FromGdbObjectMsgList(request.TargetFeatures,
				                                             request.ClassDefinitions);

			_msg.DebugStopTiming(
				watch,
				"CalculateReshapeLinesImpl: Unpacked feature lists from request params");

			IList<CutSubcurve> resultLines = CalculateReshapeLines(
				sourceFeatures, targetFeatures, request, trackCancel,
				out ReshapeAlongCurveUsability usability);

			CalculateReshapeLinesResponse result =
				PackCalculateReshapeLinesResponse(usability, resultLines);

			return result;
		}

		//[NotNull]
		//public static CalculateCutLinesResponse CalculateCutLines(
		//	[NotNull] CalculateCutLinesRequest request,
		//	[CanBeNull] ITrackCancel trackCancel)
		//{

		//}

		[NotNull]
		private static IList<CutSubcurve> CalculateReshapeLines(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			CalculateReshapeLinesRequest request,
			ITrackCancel trackCancel,
			out ReshapeAlongCurveUsability usability)
		{
			Stopwatch watch = Stopwatch.StartNew();

			List<IEnvelope> extents;
			ReshapeCurveFilterOptions filterOptions =
				GetLineFilterOptions(request, out extents);

			TargetBufferOptions targetBufferOptions = GetTargetBufferOptions(request);

			IList<CutSubcurve> resultLines = new List<CutSubcurve>();

			// TODO: Determine the policy on the main map extent vs. any visible extent?
			IEnvelope visibleExtent = extents?.Count > 0 ? extents[0] : null;

			// TODO: Actual tolerance that can be specified (using double for forward compatibility)
			bool useMinimalTolerance = MathUtils.AreEqual(0, request.Tolerance);

			usability = ChangeGeometryAlongUtils.CalculateReshapeCurves(
				sourceFeatures, targetFeatures, visibleExtent,
				useMinimalTolerance, targetBufferOptions, filterOptions,
				resultLines, trackCancel);

			_msg.DebugStopTiming(watch, "Calculated {0} reshape lines", resultLines.Count);

			return resultLines;
		}

		private static TargetBufferOptions GetTargetBufferOptions(
			CalculateReshapeLinesRequest request)
		{
			TargetBufferOptionsMsg targetBufferOptionsMsg = request.TargetBufferOptions;

			TargetBufferOptions targetBufferOptions =
				targetBufferOptionsMsg == null
					? new TargetBufferOptions()
					: new TargetBufferOptions(
						targetBufferOptionsMsg.BufferDistance,
						targetBufferOptionsMsg.BufferMinimumSegmentLength);
			return targetBufferOptions;
		}

		private static ReshapeCurveFilterOptions GetLineFilterOptions(
			CalculateReshapeLinesRequest request, out List<IEnvelope> extents)
		{
			ReshapeLineFilterOptionsMsg filterOptionsMsg = request.FilterOptions;

			ReshapeCurveFilterOptions filterOptions =
				filterOptionsMsg == null
					? new ReshapeCurveFilterOptions()
					: new ReshapeCurveFilterOptions(
						filterOptionsMsg.ClipLinesOnVisibleExtent,
						filterOptionsMsg.ExcludeOutsideTolerance,
						filterOptionsMsg.ExcludeOutsideSource,
						filterOptionsMsg.ExcludeResultingInOverlaps);

			extents = filterOptionsMsg?.VisibleExtents.Select(
				                          ProtobufGeometryUtils.FromEnvelopeMsg)
			                          .ToList();

			return filterOptions;
		}

		private static CalculateReshapeLinesResponse PackCalculateReshapeLinesResponse(
			ReshapeAlongCurveUsability usability, IList<CutSubcurve> resultLines)
		{
			Stopwatch watch = Stopwatch.StartNew();

			var result = new CalculateReshapeLinesResponse
			             {
				             ReshapeLinesUsability = (int) usability
			             };

			foreach (CutSubcurve resultCurve in resultLines)
			{
				result.ReshapeLines.Add(ToReshapeLineMsg(resultCurve));
			}

			//foreach (INotification notification in selectableOverlaps.Notifications)
			//{
			//	result.Notifications.Add(notification.Message);
			//}

			_msg.DebugStopTiming(
				watch, "Packed reshape along calculation results into response");

			return result;
		}

		#region Apply reshape lines

		public static ApplyReshapeLinesResponse ApplyReshapeLines(
			ApplyReshapeLinesRequest request,
			ITrackCancel trackCancel)
		{
			var subcurves = new List<CutSubcurve>();

			foreach (ReshapeLineMsg reshapeLineMsg in request.ReshapeLines)
			{
				subcurves.Add(FromReshapeLineMsg(reshapeLineMsg));
			}

			GeometryReshaperBase reshaper = CreateReshaper(request);

			var notifications = new NotificationCollection();
			Dictionary<IGeometry, NotificationCollection> reshapedGeometries =
				reshaper.Reshape(subcurves, notifications,
				                 request.UseNonDefaultReshapeSide);

			var response = new ApplyReshapeLinesResponse();

			if (reshapedGeometries.Count > 0)
			{
				// TODO: CacheGeometrySizes...
				IList<IFeature> updates = reshaper.Save(reshapedGeometries);

				foreach (IFeature update in updates)
				{
					IGeometry newGeometry = update.Shape;
					ResultFeatureMsg resultProto =
						new ResultFeatureMsg
						{
							UpdatedFeature = ProtobufGdbUtils.ToGdbObjectMsg(
								update, newGeometry, update.Class.ObjectClassID)
						};

					if (reshapedGeometries.ContainsKey(newGeometry) &&
					    reshapedGeometries[newGeometry] != null)
					{
						foreach (INotification notification in reshapedGeometries[
							newGeometry])
						{
							resultProto.Notifications.Add(notification.Message);
						}
					}

					response.ResultFeatures.Add(resultProto);
				}
			}

			// Calculate new reshape lines based on current source and target states:
			CalculateReshapeLinesRequest calculationParams = request.CalculationRequest;

			List<IFeature> newSourceFeatures = reshaper.ReshapeGeometryCloneByFeature.Keys.ToList();
			IList<IFeature> newTargetFeatures =
				GetUpToDateTargets(reshaper, request.CalculationRequest);

			ReshapeAlongCurveUsability curveUsability;

			IList<CutSubcurve> newSubcurves =
				CalculateReshapeLines(newSourceFeatures, newTargetFeatures,
				                      calculationParams, trackCancel, out curveUsability);

			response.ReshapeLinesUsability = (int) curveUsability;

			foreach (CutSubcurve resultCurve in newSubcurves)
			{
				response.NewReshapeLines.Add(ToReshapeLineMsg(resultCurve));
			}

			return response;
		}

		private static IList<IFeature> GetUpToDateTargets(
			[NotNull] GeometryReshaperBase reshaper,
			[NotNull] CalculateReshapeLinesRequest originalRequest)
		{
			var result = new HashSet<IFeature>();

			if (reshaper.UpdatedTargets != null)
			{
				foreach (var updatedTarget in reshaper.UpdatedTargets.Keys)
				{
					result.Add(updatedTarget);
				}
			}

			if (reshaper.TargetFeatures != null)
			{
				foreach (IFeature origTarget in reshaper.TargetFeatures)
				{
					if (! result.Contains(origTarget))
					{
						result.Add(origTarget);
					}
				}
			}
			else
			{
				IList<IFeature> targetFeatures =
					ProtobufConversionUtils.FromGdbObjectMsgList(originalRequest.TargetFeatures,
					                                             originalRequest.ClassDefinitions);
				return targetFeatures;
			}

			return result.ToList();
		}

		private static GeometryReshaperBase CreateReshaper(
			ApplyReshapeLinesRequest request)
		{
			GeometryReshaperBase result;

			IList<IFeature> sourceFeatures =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.CalculationRequest.SourceFeatures,
					request.CalculationRequest.ClassDefinitions);

			IList<IFeature> targetFeatures =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.CalculationRequest.TargetFeatures,
					request.CalculationRequest.ClassDefinitions);

			GeometryReshaperBase reshaper =
				sourceFeatures.Count == 1
					? (GeometryReshaperBase) new GeometryReshaper(sourceFeatures[0])
					: new MultipleGeometriesReshaper(sourceFeatures)
					  {
						  MultipleSourcesTreatIndividually = true,
						  MultipleSourcesTreatAsUnion = false
					  };

			if (request.InsertVerticesInTarget)
			{
				reshaper.TargetFeatures = targetFeatures;
			}

			List<IEnvelope> extents;
			ReshapeCurveFilterOptions filterOptions =
				GetLineFilterOptions(request.CalculationRequest, out extents);

			IEnumerable<IFeature> unallowedOverlapFeatures = null;

			if (filterOptions.ExcludeResultingInOverlaps)
			{
				unallowedOverlapFeatures = targetFeatures;

				reshaper.RemoveClosedReshapePathAreas = true;
			}

			List<IEnvelope> allowedExtents = null;

			//if (filterOptions.OnlyInVisibleExtent)
			//{
			//	var extentProvider =
			//		new VisibleMapExtentProvider(ArcMapUtils.GetMxApplication());

			//	allowedExtents = new List<IEnvelope>();
			//	allowedExtents.AddRange(extentProvider.GetVisibleLensWindowExtents());
			//	allowedExtents.Add(extentProvider.GetCurrentExtent());
			//}

			bool useMinimalTolerance = MathUtils.AreEqual(0, request.CalculationRequest.Tolerance);

			reshaper.ResultFilter = new ReshapeResultFilter(
				extents, unallowedOverlapFeatures, useMinimalTolerance);

			reshaper.ResultFilter.UseNonDefaultReshapeSide = request.UseNonDefaultReshapeSide;

			reshaper.UseMinimumTolerance = useMinimalTolerance;

			return reshaper;
		}

		#endregion

		#region Protobuf conversions

		public static ReshapeLineMsg ToReshapeLineMsg(CutSubcurve cutSubcurve)
		{
			var result = new ReshapeLineMsg();

			result.Path = ProtobufGeometryUtils.ToShapeMsg(cutSubcurve.Path);

			result.CanReshape = cutSubcurve.CanReshape;
			result.IsCandidate = cutSubcurve.IsReshapeMemberCandidate;
			result.IsFiltered = cutSubcurve.IsFiltered;

			result.TargetSegmentAtFrom =
				cutSubcurve.FromPointIsStitchPoint
					? ProtobufGeometryUtils.ToShapeMsg(cutSubcurve.TargetSegmentAtFromPoint)
					: null;

			result.TargetSegmentAtTo =
				cutSubcurve.ToPointIsStitchPoint
					? ProtobufGeometryUtils.ToShapeMsg(cutSubcurve.TargetSegmentAtToPoint)
					: null;

			if (cutSubcurve.ExtraTargetInsertPoints != null)
			{
				result.ExtraTargetInsertPoints =
					ProtobufGeometryUtils.ToShapeMsg(
						GeometryFactory.CreateMultipoint(cutSubcurve.ExtraTargetInsertPoints));
			}

			if (cutSubcurve.Source != null)
			{
				GdbObjectReference sourceObjRef = cutSubcurve.Source.Value;

				result.Source = new GdbObjRefMsg
				                {
					                ClassHandle = sourceObjRef.ClassId,
					                ObjectId = sourceObjRef.ObjectId
				                };
			}

			return result;
		}

		public static CutSubcurve FromReshapeLineMsg(ReshapeLineMsg reshapeCurveProto)
		{
			IPath path = GetPath(reshapeCurveProto.Path);

			var result = new CutSubcurve(
				path, reshapeCurveProto.CanReshape,
				reshapeCurveProto.IsCandidate,
				reshapeCurveProto.IsFiltered,
				GetSegment(reshapeCurveProto.TargetSegmentAtFrom),
				GetSegment(reshapeCurveProto.TargetSegmentAtTo),
				PointsFromShapeProtoBuffer(reshapeCurveProto.ExtraTargetInsertPoints));

			if (reshapeCurveProto.Source != null)
			{
				result.Source = new GdbObjectReference(
					reshapeCurveProto.Source.ClassHandle, reshapeCurveProto.Source.ObjectId);
			}

			return result;
		}

		private static ISegment GetSegment(ShapeMsg polylineProto)
		{
			IPolyline polyline =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(polylineProto);

			if (polyline == null || polyline.IsEmpty)
			{
				return null;
			}

			ISegment result = ((ISegmentCollection) polyline).get_Segment(0);

			return result;
		}

		private static IPath GetPath(ShapeMsg polylineProto)
		{
			IPolyline polyline =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(polylineProto);

			if (polyline == null || polyline.IsEmpty)
			{
				return null;
			}

			IPath result = (IPath) ((IGeometryCollection) polyline).Geometry[0];

			return result;
		}

		[CanBeNull]
		private static IList<IPoint> PointsFromShapeProtoBuffer(ShapeMsg shapeBuffer)
		{
			var geometry = ProtobufGeometryUtils.FromShapeMsg(shapeBuffer);

			if (geometry == null)
			{
				return null;
			}

			return GeometryUtils.GetPoints(geometry).ToList();
		}

		#endregion
	}
}

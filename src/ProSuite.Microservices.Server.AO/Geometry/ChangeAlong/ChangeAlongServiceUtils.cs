using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Geometry.Cut;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.ChangeAlong
{
	public static class ChangeAlongServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Calculate reshape/cut lines

		[NotNull]
		public static CalculateReshapeLinesResponse CalculateReshapeLines(
			[NotNull] CalculateReshapeLinesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			GetFeatures(request.SourceFeatures, request.TargetFeatures,
			            request.ClassDefinitions,
			            out IList<IFeature> sourceFeatures, out IList<IFeature> targetFeatures);

			ReshapeAlongResult reshapeResult = CalculateReshapeLines(
				sourceFeatures, targetFeatures, request, trackCancel);

			CalculateReshapeLinesResponse result =
				PackCalculateReshapeLinesResponse(reshapeResult);

			return result;
		}

		[NotNull]
		public static CalculateCutLinesResponse CalculateCutLines(
			[NotNull] CalculateCutLinesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			GetFeatures(request.SourceFeatures, request.TargetFeatures,
			            request.ClassDefinitions,
			            out IList<IFeature> sourceFeatures, out IList<IFeature> targetFeatures);

			IList<CutSubcurve> resultLines = CalculateCutLines(
				sourceFeatures, targetFeatures, request, trackCancel,
				out ReshapeAlongCurveUsability usability);

			CalculateCutLinesResponse
				result = PackCalculateCutLinesResponse(usability, resultLines);

			return result;
		}

		private static IList<CutSubcurve> CalculateCutLines(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			[NotNull] CalculateCutLinesRequest calculateCutLinesRequest,
			[CanBeNull] ITrackCancel trackCancel,
			out ReshapeAlongCurveUsability usability)
		{
			Stopwatch watch = Stopwatch.StartNew();

			IEnvelope visibleExtent;
			ReshapeCurveFilterOptions filterOptions =
				GetLineFilterOptions(calculateCutLinesRequest.FilterOptions, out visibleExtent);

			TargetBufferOptions targetBufferOptions =
				GetTargetBufferOptions(calculateCutLinesRequest.TargetBufferOptions);

			IList<CutSubcurve> resultLines = new List<CutSubcurve>();

			usability = ChangeGeometryAlongUtils.CalculateCutCurves(
				sourceFeatures, targetFeatures, visibleExtent, calculateCutLinesRequest.Tolerance,
				targetBufferOptions, filterOptions,
				resultLines, trackCancel);

			_msg.DebugStopTiming(watch, "Calculated {0} reshape lines", resultLines.Count);

			return resultLines;
		}

		[NotNull]
		private static ReshapeAlongResult CalculateReshapeLines(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			[NotNull] CalculateReshapeLinesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			Stopwatch watch = Stopwatch.StartNew();

			IEnvelope visibleExtent;
			ReshapeCurveFilterOptions filterOptions =
				GetLineFilterOptions(request.FilterOptions, out visibleExtent);

			TargetBufferOptions targetBufferOptions =
				GetTargetBufferOptions(request.TargetBufferOptions);

			ReshapeAlongResult result = ChangeGeometryAlongUtils.CalculateReshapeCurves(
				sourceFeatures, targetFeatures, visibleExtent,
				request.Tolerance, targetBufferOptions, filterOptions, trackCancel);

			_msg.DebugStopTiming(watch, "Calculated {0} reshape lines",
			                     result.Subcurves.Count);

			return result;
		}

		private static TargetBufferOptions GetTargetBufferOptions(
			[CanBeNull] TargetBufferOptionsMsg targetBufferOptionsMsg)
		{
			if (targetBufferOptionsMsg == null)
			{
				return new TargetBufferOptions();
			}

			var targetBufferOptions = new TargetBufferOptions(
				targetBufferOptionsMsg.BufferDistance,
				targetBufferOptionsMsg.BufferMinimumSegmentLength);

			return targetBufferOptions;
		}

		[NotNull]
		private static ReshapeCurveFilterOptions GetLineFilterOptions(
			[CanBeNull] ReshapeLineFilterOptionsMsg filterOptionsMsg,
			[CanBeNull] out IEnvelope visibleExtent)
		{
			visibleExtent = null;

			if (filterOptionsMsg == null)
			{
				return new ReshapeCurveFilterOptions();
			}

			var result = new ReshapeCurveFilterOptions(
				filterOptionsMsg.ClipLinesOnVisibleExtent,
				filterOptionsMsg.ExcludeOutsideTolerance,
				filterOptionsMsg.ExcludeOutsideSource,
				filterOptionsMsg.ExcludeResultingInOverlaps);

			List<IEnvelope> extents = filterOptionsMsg.VisibleExtents.Select(
				                                          ProtobufGeometryUtils.FromEnvelopeMsg)
			                                          .ToList();

			// TODO: Determine the policy on the main map extent vs. any visible extent?
			visibleExtent = extents.Count > 0 ? extents[0] : null;

			return result;
		}

		private static CalculateReshapeLinesResponse PackCalculateReshapeLinesResponse(
			ReshapeAlongResult reshapeResult)
		{
			Stopwatch watch = Stopwatch.StartNew();

			var result = new CalculateReshapeLinesResponse
			             {
				             ReshapeLinesUsability = (int) reshapeResult.Usability
			             };

			foreach (CutSubcurve resultCurve in reshapeResult.Subcurves)
			{
				result.ReshapeLines.Add(ToReshapeLineMsg(resultCurve));
			}

			result.FilterBuffer =
				ProtobufGeometryUtils.ToShapeMsg(reshapeResult.FilterBuffer);

			_msg.DebugStopTiming(
				watch, "Packed {0} reshape along calculation results into response",
				reshapeResult.Subcurves.Count);

			return result;
		}

		private static CalculateCutLinesResponse PackCalculateCutLinesResponse(
			ReshapeAlongCurveUsability usability,
			[NotNull] IList<CutSubcurve> resultLines)
		{
			var watch = Stopwatch.StartNew();

			var result = new CalculateCutLinesResponse
			             {
				             ReshapeLinesUsability = (int) usability
			             };

			foreach (CutSubcurve resultCurve in resultLines)
			{
				result.CutLines.Add(ToReshapeLineMsg(resultCurve));

				// TODO: result.Notifications
			}

			_msg.DebugStopTiming(
				watch, "Packed {0} cut along calculation results into response",
				resultLines.Count);

			return result;
		}

		#endregion

		#region Apply reshape/cut lines

		public static ApplyReshapeLinesResponse ApplyReshapeLines(
			[NotNull] ApplyReshapeLinesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var subcurves = new List<CutSubcurve>();

			foreach (ReshapeLineMsg reshapeLineMsg in request.ReshapeLines)
			{
				CutSubcurve cutSubcurve = FromReshapeLineMsg(reshapeLineMsg);

				if (! cutSubcurve.IsFiltered)
				{
					subcurves.Add(cutSubcurve);
				}
			}

			GeometryReshaperBase reshaper = CreateReshaper(request);

			var notifications = new NotificationCollection();

			Dictionary<IGeometry, NotificationCollection> reshapedGeometries =
				reshaper.Reshape(subcurves, notifications, request.UseNonDefaultReshapeSide);

			var response = new ApplyReshapeLinesResponse();

			if (reshapedGeometries.Count > 0)
			{
				// TODO: CacheGeometrySizes...
				IList<IFeature> updatedFeatures = reshaper.Save(reshapedGeometries);

				IList<ResultObjectMsg> resultObjectMsgs =
					GetResultFeatureMessages(
						null, updatedFeatures,
						f => GetNotifications(reshapedGeometries, reshaper, f),
						f => reshaper.NotificationIsWarning);

				response.ResultFeatures.AddRange(resultObjectMsgs);

				reshaper.LogSuccessfulReshape(reshapedGeometries.Keys,
				                              esriUnits.esriMeters, esriUnits.esriMeters);
			}

			// Calculate new reshape lines based on current source and target states:
			CalculateReshapeLinesRequest calculationRequest = request.CalculationRequest;

			List<IFeature> newSourceFeatures = reshaper.ReshapeGeometryCloneByFeature.Keys.ToList();
			IList<IFeature> newTargetFeatures =
				GetUpToDateTargets(reshaper, request.CalculationRequest);

			ReshapeAlongCurveUsability curveUsability;

			var newResult =
				CalculateReshapeLines(newSourceFeatures, newTargetFeatures,
				                      calculationRequest, trackCancel);

			response.ReshapeLinesUsability = (int) newResult.Usability;

			foreach (CutSubcurve resultCurve in newResult.Subcurves)
			{
				response.NewReshapeLines.Add(ToReshapeLineMsg(resultCurve));
			}

			return response;
		}

		public static ApplyCutLinesResponse ApplyCutLines(
			[NotNull] ApplyCutLinesRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			IList<CutSubcurve> cutCurves = request.CutLines.Select(FromReshapeLineMsg).ToList();

			FeatureCutter cutter = CreateFeatureCutter(request, out IList<IFeature> targetFeatures);

			cutter.Cut(cutCurves);

			List<IFeature> storedFeatures = new List<IFeature>();

			var response = new ApplyCutLinesResponse();

			if (cutter.ResultGeometriesByFeature.Count > 0)
			{
				cutter.StoreResultFeatures(storedFeatures);

				cutter.LogSuccessfulCut();

				ICollection<KeyValuePair<IFeature, IList<IFeature>>> insertsByOriginal =
					cutter.InsertedFeaturesByOriginal;

				IList<ResultObjectMsg> ResultObjectMsgs =
					GetResultFeatureMessages(insertsByOriginal, storedFeatures);

				response.ResultFeatures.AddRange(ResultObjectMsgs);

				// Calculate the new cut lines:
				List<IFeature> newSourceFeatures = new List<IFeature>(cutter.SourceFeatures);

				newSourceFeatures.AddRange(
					insertsByOriginal.SelectMany(kvp => kvp.Value));

				var newSubcurves =
					CalculateCutLines(newSourceFeatures, targetFeatures,
					                  request.CalculationRequest, trackCancel,
					                  out ReshapeAlongCurveUsability usability);

				// TODO: Ideally, new features can also be referenced by the CutSubcurve's Source
				// And the ObjectIDs would be re-assigned after the store happened on the client.
				// However, for the time being, clear the Source property if it references a new row
				foreach (CutSubcurve newSubcurve in newSubcurves)
				{
					if (newSubcurve.Source != null &&
					    newSourceFeatures.Any(f => newSubcurve.Source.Value.References(f)))
					{
						newSubcurve.Source = null;
					}
				}

				response.CutLinesUsability = (int) usability;

				response.NewCutLines.AddRange(newSubcurves.Select(ToReshapeLineMsg));

				return response;
			}

			_msg.WarnFormat("The selection was not cut. Please select the lines to cut along");

			return response;
		}

		[NotNull]
		private static GeometryReshaperBase CreateReshaper(
			[NotNull] ApplyReshapeLinesRequest request)
		{
			GetFeatures(request.CalculationRequest.SourceFeatures,
			            request.CalculationRequest.TargetFeatures,
			            request.CalculationRequest.ClassDefinitions,
			            out IList<IFeature> sourceFeatures, out IList<IFeature> targetFeatures);

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

			IEnvelope visibleExtent;
			ReshapeCurveFilterOptions filterOptions =
				GetLineFilterOptions(request.CalculationRequest.FilterOptions, out visibleExtent);

			IEnumerable<IFeature> unallowedOverlapFeatures = null;

			if (filterOptions.ExcludeResultingInOverlaps)
			{
				unallowedOverlapFeatures = targetFeatures;

				reshaper.RemoveClosedReshapePathAreas = true;
			}

			List<IEnvelope> allowedExtents =
				visibleExtent == null ? null : new List<IEnvelope> { visibleExtent };

			bool useMinimalTolerance = MathUtils.AreEqual(0, request.CalculationRequest.Tolerance);

			reshaper.ResultFilter = new ReshapeResultFilter(allowedExtents,
			                                                unallowedOverlapFeatures,
			                                                useMinimalTolerance);

			reshaper.ResultFilter.UseNonDefaultReshapeSide = request.UseNonDefaultReshapeSide;

			reshaper.UseMinimumTolerance = useMinimalTolerance;

			return reshaper;
		}

		[NotNull]
		private static FeatureCutter CreateFeatureCutter([NotNull] ApplyCutLinesRequest request,
		                                                 out IList<IFeature> targetFeatures)
		{
			GetFeatures(request.CalculationRequest.SourceFeatures,
			            request.CalculationRequest.TargetFeatures,
			            request.CalculationRequest.ClassDefinitions,
			            out IList<IFeature> sourceFeatures, out targetFeatures);

			ChangeAlongZSource zSource = (ChangeAlongZSource) request.ChangedVerticesZSource;

			DatasetSpecificSettingProvider<ChangeAlongZSource> zSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					"Z values for changed vertices", zSource);

			var cutter = new FeatureCutter(sourceFeatures)
			             {
				             ZSourceProvider = zSourceProvider
			             };

			if (request.InsertVerticesInTarget)
			{
				cutter.TargetFeatures = targetFeatures;
			}

			return cutter;
		}

		[NotNull]
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

		private static IEnumerable<string> GetNotifications(
			[NotNull] IReadOnlyDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			GeometryReshaperBase reshaper,
			[NotNull] IFeature feature)
		{
			if (reshapedGeometries == null)
			{
				throw new ArgumentNullException(nameof(reshapedGeometries));
			}

			IGeometry updatedGeometry = feature.Shape;

			if (reshapedGeometries.TryGetValue(updatedGeometry,
			                                   out NotificationCollection notifications))
			{
				foreach (INotification notification in notifications)
				{
					yield return notification.Message;
				}
			}

			if (! reshaper.NotificationIsWarning)
			{
				// Add the standard size text information

				var proj = updatedGeometry.SpatialReference as IProjectedCoordinateSystem;
				//TODO: return correct sizeChangeMessage when SpatialReference is not projected but geographic
				if (proj != null)
				{
					int coordinateUnitFactoryCode = proj.CoordinateUnit.FactoryCode;

					esriUnits linearUnits = esriUnits.esriMeters;

					string sizeChangeMessage = reshaper.GetSizeChangeMessage(
						updatedGeometry, feature, linearUnits, linearUnits);

					if (! string.IsNullOrEmpty(sizeChangeMessage))
					{
						yield return sizeChangeMessage;
					}
				}
			}
		}

		#endregion

		#region Protobuf conversions

		private static void GetFeatures([NotNull] RepeatedField<GdbObjectMsg> requestSourceFeatures,
		                                [NotNull] RepeatedField<GdbObjectMsg> requestTargetFeatures,
		                                [NotNull] RepeatedField<ObjectClassMsg> classDefinitions,
		                                [NotNull] out IList<IFeature> sourceFeatures,
		                                [NotNull] out IList<IFeature> targetFeatures)
		{
			Stopwatch watch = Stopwatch.StartNew();

			sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(requestSourceFeatures,
				classDefinitions);

			targetFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(requestTargetFeatures,
				classDefinitions);

			_msg.DebugStopTiming(
				watch,
				"GetFeatures: Unpacked {0} source and {1} target features from request params",
				sourceFeatures.Count, targetFeatures.Count);
		}

		[NotNull]
		private static ReshapeLineMsg ToReshapeLineMsg([NotNull] CutSubcurve cutSubcurve)
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

		[NotNull]
		private static CutSubcurve FromReshapeLineMsg([NotNull] ReshapeLineMsg reshapeLineMsg)
		{
			IPath path = Assert.NotNull(GetPath(reshapeLineMsg.Path), "Reshapeline's path is null");

			var result = new CutSubcurve(
				path, reshapeLineMsg.CanReshape,
				reshapeLineMsg.IsCandidate,
				reshapeLineMsg.IsFiltered,
				GetSegment(reshapeLineMsg.TargetSegmentAtFrom),
				GetSegment(reshapeLineMsg.TargetSegmentAtTo),
				PointsFromShapeMsg(reshapeLineMsg.ExtraTargetInsertPoints));

			if (reshapeLineMsg.Source != null)
			{
				result.Source = new GdbObjectReference(
					(int) reshapeLineMsg.Source.ClassHandle, (int) reshapeLineMsg.Source.ObjectId);
			}

			return result;
		}

		[CanBeNull]
		private static ISegment GetSegment([CanBeNull] ShapeMsg segmentMsg)
		{
			IPolyline polyline =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(segmentMsg);

			if (polyline == null || polyline.IsEmpty)
			{
				return null;
			}

			ISegment result = ((ISegmentCollection) polyline).Segment[0];

			return result;
		}

		[CanBeNull]
		private static IPath GetPath([CanBeNull] ShapeMsg polylineMsg)
		{
			IPolyline polyline = (IPolyline) ProtobufGeometryUtils.FromShapeMsg(polylineMsg);

			if (polyline == null || polyline.IsEmpty)
			{
				return null;
			}

			IPath result = (IPath) ((IGeometryCollection) polyline).Geometry[0];

			return result;
		}

		[CanBeNull]
		private static IList<IPoint> PointsFromShapeMsg([CanBeNull] ShapeMsg shapeMsg)
		{
			var geometry = ProtobufGeometryUtils.FromShapeMsg(shapeMsg);

			if (geometry == null)
			{
				return null;
			}

			return GeometryUtils.GetPoints(geometry).ToList();
		}

		[NotNull]
		private static IList<ResultObjectMsg> GetResultFeatureMessages(
			[CanBeNull] ICollection<KeyValuePair<IFeature, IList<IFeature>>> insertsByOriginal,
			[CanBeNull] IEnumerable<IFeature> allResultFeatures,
			[CanBeNull] Func<IFeature, IEnumerable<string>> notificationsForFeature = null,
			[CanBeNull] Func<IFeature, bool> warningForFeature = null)
		{
			IList<ResultObjectMsg> resultObjectMsgs = new List<ResultObjectMsg>();

			HashSet<IFeature> allInserts = new HashSet<IFeature>();

			if (insertsByOriginal != null)
			{
				foreach (KeyValuePair<IFeature, IList<IFeature>> kvp in insertsByOriginal)
				{
					IList<IFeature> inserts = kvp.Value;
					IFeature original = kvp.Key;

					var originalRef = new GdbObjRefMsg
					                  {
						                  ClassHandle = original.Class.ObjectClassID,
						                  ObjectId = original.OID
					                  };

					foreach (IFeature insert in inserts)
					{
						allInserts.Add(insert);

						var insertMsg =
							new InsertedObjectMsg
							{
								InsertedObject = ProtobufGdbUtils.ToGdbObjectMsg(insert),
								OriginalReference = originalRef
							};

						var featureMsg = new ResultObjectMsg
						                 {
							                 Insert = insertMsg
						                 };

						AddNotification(insert, featureMsg, notificationsForFeature,
						                warningForFeature);

						resultObjectMsgs.Add(featureMsg);
					}
				}
			}

			if (allResultFeatures != null)
			{
				foreach (IFeature resultFeature in allResultFeatures)
				{
					if (allInserts.Contains(resultFeature))
					{
						continue;
					}

					ResultObjectMsg updateMsg =
						new ResultObjectMsg
						{
							Update = ProtobufGdbUtils.ToGdbObjectMsg(resultFeature)
						};

					AddNotification(resultFeature, updateMsg, notificationsForFeature,
					                warningForFeature);

					resultObjectMsgs.Add(updateMsg);
				}
			}

			return resultObjectMsgs;
		}

		private static void AddNotification(IFeature feature, ResultObjectMsg featureMsg,
		                                    Func<IFeature, IEnumerable<string>>
			                                    notificationsForFeature,
		                                    Func<IFeature, bool> warningForFeature)
		{
			if (notificationsForFeature != null)
			{
				foreach (string notification in notificationsForFeature(feature))
				{
					featureMsg.Notifications.Add(notification);
				}
			}

			if (warningForFeature != null)
			{
				featureMsg.HasWarning = warningForFeature(feature);
			}
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.AO.Geometry.Generalize;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.AdvancedGeneralize
{
	public static class GeneralizeServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static CalculateRemovableSegmentsResponse CalculateRemovableSegments(
			[NotNull] CalculateRemovableSegmentsRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			GeneralizeOptionsMsg optionsMsg =
				Assert.NotNull(request.GeneralizeOptions, "Generalization options is null");

			GeometryProcessingUtils.GetFeatures(
				request.SourceFeatures, request.TargetFeatures, request.ClassDefinitions,
				out IList<IFeature> sourceFeatures, out IList<IFeature> targetFeatures);

			IGeometry perimeter = ProtobufGeometryUtils.FromShapeMsg(request.Perimeter);

			AdvancedGeneralizeOptions options = new AdvancedGeneralizeOptions(
				null, FromOptionsMsg(optionsMsg));

			Func<IGeometry, IList<IFeature>> targetFeatureFinder = searchGeometry =>
				FindTargetFeature(searchGeometry, targetFeatures);

			IList<FeatureVertexInfo> result = CalculateRemovableSegments(
				sourceFeatures, perimeter, options, targetFeatureFinder, trackCancel);

			CalculateRemovableSegmentsResponse response = PackShortSegments(result);

			_msg.DebugStopTiming(watch,
			                     "Calculated short segments and weed points for {0} features.",
			                     response.RemovableSegments.Count);

			return response;
		}

		public static ApplySegmentRemovalResponse ApplySegmentRemoval(
			ApplySegmentRemovalRequest request,
			ITrackCancel trackCancel)
		{
			// Unpack request
			IList<IFeature> sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(
				Assert.NotNull(request.SourceFeatures, "SourceFeatures are null"),
				Assert.NotNull(request.ClassDefinitions, "ClassDefinitions are null"));

			AdvancedGeneralizeOptions options = new AdvancedGeneralizeOptions(
				null, FromOptionsMsg(request.GeneralizeOptions));

			IGeometry perimeter = ProtobufGeometryUtils.FromShapeMsg(request.Perimeter);

			Dictionary<GdbObjectReference, IFeature> featureByObjRef =
				sourceFeatures.ToDictionary(s => new GdbObjectReference(s), s => s);

			var updateGeometryByFeature = new Dictionary<IFeature, IGeometry>();

			var nonStorableMessagess = new List<string>();

			var featureCount = 0;
			var totalRemovedCount = 0;

			foreach (RemovableSegmentsMsg removableSegmentsMsg in request.RemovableSegments)
			{
				if (removableSegmentsMsg.ShortSegments.Count == 0 &&
				    removableSegmentsMsg.PointsToDelete == null)
				{
					continue;
				}

				// TODO: Class handle in GdbObjectReference: long!
				GdbObjectReference gdbObjRef = new GdbObjectReference(
					Convert.ToInt32(removableSegmentsMsg.OriginalFeatureRef.ClassHandle),
					removableSegmentsMsg.OriginalFeatureRef.ObjectId);

				IFeature feature = featureByObjRef[gdbObjRef];

				IGeometry updateGeometry = feature.ShapeCopy;

				if (options.WeedNonLinearSegments && updateGeometry is IPolycurve polycurve)
				{
					GeometryUtils.EnsureLinearized(polycurve, 0);
				}

				try
				{
					IPointCollection pointsToDelete =
						(IPointCollection) ProtobufGeometryUtils.FromShapeMsg(
							removableSegmentsMsg.PointsToDelete);

					bool deletePoints =
						pointsToDelete != null && ! ((IGeometry) pointsToDelete).IsEmpty;

					int removeCount = 0;

					if (deletePoints)
					{
						RemoveCutPointsService.RemovePoints(
							updateGeometry, GeometryUtils.GetPoints(pointsToDelete));
					}

					if (options.EnforceMinimumSegmentLength &&
					    removableSegmentsMsg.ShortSegments.Count > 0)
					{
						var featureVertexInfo =
							new FeatureVertexInfo(feature, perimeter?.Envelope)
							{
								MinimumSegmentLength = options.MinimumSegmentLength,
								PointsToDelete = pointsToDelete
							};

						AddProtectedPoints(removableSegmentsMsg.ProtectedPoints, featureVertexInfo);

						featureVertexInfo.ShortSegments =
							GetShortSegments(updateGeometry, removableSegmentsMsg, deletePoints,
							                 options, perimeter);

						var polycurveToUpdate = updateGeometry as IPolycurve;

						Assert.NotNull(polycurveToUpdate, "Feature's shape must be a polycurve");

						removeCount = GeneralizeUtils.DeleteShortSegments(
							polycurveToUpdate, featureVertexInfo, options.Only2D, perimeter);
					}

					if (updateGeometry.IsEmpty)
					{
						string message =
							$"Feature {GdbObjectUtils.ToString(feature)} would become " +
							$"empty after removing points. The feature was not changed.";
						_msg.Warn(message);
						nonStorableMessagess.Add(message);
					}
					else
					{
						if (deletePoints)
						{
							totalRemovedCount += pointsToDelete.PointCount;
						}

						totalRemovedCount += removeCount;

						featureCount++;

						updateGeometryByFeature[feature] = updateGeometry;
					}
				}
				catch (Exception e)
				{
					_msg.Debug($"Error storing feature {feature.OID}", e);

					string message =
						$"Error while generalizing {GdbObjectUtils.ToString(feature)}: " +
						$"{e.Message}. The feature was not changed.";

					_msg.InfoFormat(message);
					nonStorableMessagess.Add(message);
				}
			}

			// Pack response
			return PackApplySegmentRemovalResponse(updateGeometryByFeature, nonStorableMessagess);
		}

		private static void AddProtectedPoints(ShapeMsg protectedPointsMsg,
		                                       FeatureVertexInfo toFeatureVertexInfo)
		{
			IPointCollection protectedPoints =
				(IPointCollection) ProtobufGeometryUtils.FromShapeMsg(
					protectedPointsMsg);

			if (protectedPoints != null)
			{
				var crackPoints = GeometryUtils.GetPoints(protectedPoints)
				                               .Select(p => new CrackPoint(p)).ToList();

				toFeatureVertexInfo.AddCrackPoints(crackPoints);
			}
		}

		private static IList<esriSegmentInfo> GetShortSegments(IGeometry inGeometry,
		                                                       RemovableSegmentsMsg
			                                                       removableSegmentsMsg,
		                                                       bool recalculate,
		                                                       AdvancedGeneralizeOptions options,
		                                                       IGeometry perimeter)
		{
			IList<esriSegmentInfo> shortSegments;

			if (recalculate)
			{
				// The geometry was already changed - filter out the short segments
				// that do not exist any more due to the previous point removal
				// and do not rely on the indexes in the short segment message!
				shortSegments =
					GeneralizeUtils.GetShortSegments(
						(IPolycurve) inGeometry, perimeter,
						options.MinimumSegmentLength, options.Only2D);
			}
			else
			{
				shortSegments =
					removableSegmentsMsg.ShortSegments.Select(FromShortSegmentMsg)
					                    .ToList();
			}

			return shortSegments;
		}

		private static CalculateRemovableSegmentsResponse PackShortSegments(
			[CanBeNull] ICollection<FeatureVertexInfo> featureVertexInfos)
		{
			var response = new CalculateRemovableSegmentsResponse();

			if (featureVertexInfos == null)
			{
				return response;
			}

			foreach (FeatureVertexInfo featureInfo in featureVertexInfos)
			{
				RemovableSegmentsMsg segmentsPerFeature = new RemovableSegmentsMsg();

				segmentsPerFeature.OriginalFeatureRef =
					ProtobufGdbUtils.ToGdbObjRefMsg(featureInfo.Feature);
				segmentsPerFeature.PointsToDelete =
					ProtobufGeometryUtils.ToShapeMsg((IGeometry) featureInfo.PointsToDelete);
				segmentsPerFeature.ProtectedPoints =
					ProtobufGeometryUtils.ToShapeMsg((IGeometry) featureInfo.CrackPointCollection);

				if (featureInfo.ShortSegments != null)
				{
					foreach (esriSegmentInfo segmentInfo in featureInfo.ShortSegments)
					{
						ShortSegmentMsg shortSegmentMsg = ToShortSegmentMsg(segmentInfo);

						segmentsPerFeature.ShortSegments.Add(shortSegmentMsg);
					}
				}

				response.RemovableSegments.Add(segmentsPerFeature);
			}

			return response;
		}

		private static ApplySegmentRemovalResponse PackApplySegmentRemovalResponse(
			Dictionary<IFeature, IGeometry> updateGeometryByFeature,
			List<string> nonStorableMessagess)
		{
			var response = new ApplySegmentRemovalResponse();

			foreach (var kvp in updateGeometryByFeature)
			{
				IFeature feature = kvp.Key;
				IGeometry newGeometry = kvp.Value;

				var resultObject = new ResultObjectMsg();

				resultObject.Update = ProtobufGdbUtils.ToGdbObjectMsg(
					feature, newGeometry, feature.Class.ObjectClassID);

				response.ResultFeatures.Add(resultObject);
			}

			response.NonStorableMessages.AddRange(nonStorableMessagess);

			return response;
		}

		private static ShortSegmentMsg ToShortSegmentMsg(esriSegmentInfo segmentInfo)
		{
			ShortSegmentMsg shortSegmentMsg = new ShortSegmentMsg();

			ISegment segment = segmentInfo.pSegment;

			shortSegmentMsg.FromPoint =
				ProtobufGeometryUtils.ToShapeMsg(segment.FromPoint);
			shortSegmentMsg.ToPoint =
				ProtobufGeometryUtils.ToShapeMsg(segment.ToPoint);

			shortSegmentMsg.PartIndex = segmentInfo.iPart;
			shortSegmentMsg.AbsoluteIndex = segmentInfo.iAbsSegment;
			shortSegmentMsg.RelativeIndex = segmentInfo.iRelSegment;

			return shortSegmentMsg;
		}

		private static esriSegmentInfo FromShortSegmentMsg(ShortSegmentMsg shortSegmentMsg)
		{
			IPoint fromPoint =
				(IPoint) ProtobufGeometryUtils.FromShapeMsg(shortSegmentMsg.FromPoint);

			IPoint toPoint = (IPoint) ProtobufGeometryUtils.FromShapeMsg(shortSegmentMsg.ToPoint);

			ISegment segment = null;

			if (fromPoint != null && toPoint != null)
			{
				segment = new LineClass
				          {
					          FromPoint = fromPoint,
					          ToPoint = toPoint
				          };
			}

			return new esriSegmentInfo
			       {
				       pSegment = segment,
				       iPart = shortSegmentMsg.PartIndex,
				       iAbsSegment = shortSegmentMsg.AbsoluteIndex,
				       iRelSegment = shortSegmentMsg.RelativeIndex
			       };
		}

		private static List<IFeature> FindTargetFeature(
			[NotNull] IGeometry searchGeometry,
			[NotNull] IList<IFeature> allTargetFeatures)
		{
			var result = new List<IFeature>();
			foreach (IFeature feature in allTargetFeatures)
			{
				if (GeometryUtils.Intersects(searchGeometry, feature.Shape))
				{
					result.Add(feature);
				}
			}

			return result;
		}

		private static PartialAdvancedGeneralizeOptions FromOptionsMsg(
			GeneralizeOptionsMsg optionsMsg)
		{
			bool weed = optionsMsg.WeedTolerance >= 0;
			bool enforceMinimumSegmentLength = optionsMsg.MinimumSegmentLength > 0;

			// This is rather hacky.
			// TODO: Extract interface, implement dedicated class for server-side options.
			return new PartialAdvancedGeneralizeOptions()
			       {
				       Weed = new OverridableSetting<bool>(weed, false),
				       WeedTolerance =
					       new OverridableSetting<double>(optionsMsg.WeedTolerance, false),
				       WeedNonLinearSegments =
					       new OverridableSetting<bool>(optionsMsg.WeedNonLinearSegments, false),

				       EnforceMinimumSegmentLength =
					       new OverridableSetting<bool>(enforceMinimumSegmentLength, false),
				       MinimumSegmentLength =
					       new OverridableSetting<double>(optionsMsg.MinimumSegmentLength, false),
				       ProtectTopologicalVertices =
					       new OverridableSetting<bool>(optionsMsg.ProtectTopologicalVertices,
					                                    false),

				       Only2D = new OverridableSetting<bool>(optionsMsg.Use2DLength, false),

				       VertexProtectingFeatureSelection =
					       optionsMsg.ProtectOnlyWithinSameClass
						       ? new OverridableSetting<TargetFeatureSelection>(
							       TargetFeatureSelection.SameClass, false)
						       : new OverridableSetting<TargetFeatureSelection>(
							       TargetFeatureSelection.VisibleFeatures, false)
			       };
		}

		private static IList<FeatureVertexInfo> CalculateRemovableSegments(
			[NotNull] IList<IFeature> selectedFeatures,
			[CanBeNull] IGeometry processingPerimeter, AdvancedGeneralizeOptions options,
			[CanBeNull] Func<IGeometry, IList<IFeature>> targetFeatureFinder,
			[CanBeNull] ITrackCancel trackCancel)
		{
			IEnvelope searchExtent = processingPerimeter?.Envelope;

			// NOTE: Assuming that only a few features have weed points / short segments, we do not need to 
			// calculate protection points for all of them -> Use the short segments / weed points as search 
			// geometry to find relevant target features. But: weed cuts the geometries on the crack points, 
			// therefore the protection points are needed before the actual weeding takes place.
			// However, compared to the protection point calculation weeding / short segment calculation is
			// very fast, therefore it is worth to perform the operation twice.

			IList<FeatureVertexInfo> featureVertexInfos =
				InitializeFeatureVertexInfos(selectedFeatures, searchExtent, options);

			CalculateRemovableSegments(featureVertexInfos, searchExtent, options, trackCancel);

			// Optimization: remove the featureVertexInfos that have no points to weed / segments to remove
			RemoveIrrelevant(featureVertexInfos);

			if (options.ProtectTopologicalVertices)
			{
				Assert.NotNull(targetFeatureFinder, "Target feature finder is null");

				IMultipoint searchGeometry =
					GeometryFactory.CreateMultipoint(GetAllProtectablePoints(featureVertexInfos));

				// Only fetch the target features that intersect the short segments / weed points
				if (! searchGeometry.IsEmpty)
				{
					IList<IFeature> targetFeatures = targetFeatureFinder(searchGeometry);

					GeneralizeUtils.CalculateProtectionPoints(
						featureVertexInfos, selectedFeatures, targetFeatures,
						options.WeedNonLinearSegments, options.VertexProtectingFeatureSelection,
						trackCancel);

					CalculateRemovableSegments(featureVertexInfos, processingPerimeter, options,
					                           trackCancel);
				}
			}

			return featureVertexInfos;
		}

		[NotNull]
		private static IList<FeatureVertexInfo> InitializeFeatureVertexInfos(
			[NotNull] IEnumerable<IFeature> selectedFeatures,
			[CanBeNull] IEnvelope inExtent,
			[NotNull] AdvancedGeneralizeOptions options)
		{
			double? snapTolerance = null;

			// NOTE: do not set the minimum segment length here because a non-null value
			//		 means that crack points should be excluded if they are too close to the 
			//       next vertex -> make this more explicit by new property on VertexInfo
			double? minimumSegmentLength = null;

			IList<FeatureVertexInfo> generalizationInfos =
				CrackUtils.CreateFeatureVertexInfos(selectedFeatures, inExtent, snapTolerance,
				                                    minimumSegmentLength);

			foreach (FeatureVertexInfo generalizationInfo in generalizationInfos)
			{
				// Set the minimum segment length for using it during minimum segment removal
				if (options.EnforceMinimumSegmentLength)
				{
					generalizationInfo.MinimumSegmentLength = options.MinimumSegmentLength;
				}

				if (options.Weed && options.WeedNonLinearSegments)
				{
					generalizationInfo.LinearizeSegments = true;
				}
			}

			return generalizationInfos;
		}

		private static void CalculateRemovableSegments(
			[NotNull] ICollection<FeatureVertexInfo> featureVertexInfos,
			[CanBeNull] IGeometry inPerimeter,
			[NotNull] AdvancedGeneralizeOptions options,
			[CanBeNull] ITrackCancel cancelTracker)
		{
			if (options.Weed)
			{
				double weedTolerance = options.WeedTolerance;
				bool only2D = options.Only2D;
				bool omitNonLinearSegments = ! options.WeedNonLinearSegments;

				GeneralizeUtils.CalculateWeedPoints(
					featureVertexInfos, weedTolerance, only2D, omitNonLinearSegments, inPerimeter,
					cancelTracker);
			}

			if (options.EnforceMinimumSegmentLength)
			{
				GeneralizeUtils.CalculateShortSegments(featureVertexInfos,
				                                       options.Only2D,
				                                       inPerimeter,
				                                       cancelTracker);
			}
		}

		private static void RemoveIrrelevant(
			ICollection<FeatureVertexInfo> featureVertexInfos)
		{
			var itemsToRemove = new List<FeatureVertexInfo>();

			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				if (! featureVertexInfo.HasPointsToDelete &&
				    ! featureVertexInfo.HasShortSegments)
				{
					itemsToRemove.Add(featureVertexInfo);
				}
			}

			foreach (FeatureVertexInfo featureVertexInfoToRemove in itemsToRemove)
			{
				featureVertexInfos.Remove(featureVertexInfoToRemove);
			}
		}

		private static IEnumerable<IPoint> GetAllProtectablePoints(
			[NotNull] IEnumerable<FeatureVertexInfo> featureVertexInfos)
		{
			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				if (featureVertexInfo.PointsToDelete != null)
				{
					foreach (
						IPoint point in GeometryUtils.GetPoints(featureVertexInfo.PointsToDelete))
					{
						yield return point;
					}
				}

				if (featureVertexInfo.ShortSegments != null)
				{
					foreach (esriSegmentInfo segmentInfo in featureVertexInfo.ShortSegments)
					{
						yield return segmentInfo.pSegment.FromPoint;
						yield return segmentInfo.pSegment.ToPoint;
					}
				}
			}
		}
	}
}

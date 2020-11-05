using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.RemoveOverlaps;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps
{
	public static class RemoveOverlapsServiceUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull]
		public static CalculateOverlapsResponse CalculateOverlaps(
			[NotNull] CalculateOverlapsRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			IList<IFeature> sourceFeatures =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.SourceFeatures, request.ClassDefinitions);

			IList<IFeature> targetFeatures =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.TargetFeatures, request.ClassDefinitions);

			_msg.DebugStopTiming(watch, "Unpacked feature lists from request params");

			var selectableOverlaps = RemoveOverlapsUtils.GetSelectableOverlaps(
				sourceFeatures, targetFeatures, trackCancel);

			watch = Stopwatch.StartNew();

			var result = new CalculateOverlapsResponse();

			foreach (var overlap in selectableOverlaps.OverlapGeometries)
				result.Overlaps.Add(ProtobufConversionUtils.ToShapeMsg(overlap));

			foreach (var notification in selectableOverlaps.Notifications)
				result.Notifications.Add(notification.Message);

			_msg.DebugStopTiming(watch, "Packed overlaps into response");

			return result;
		}

		public static RemoveOverlapsResponse RemoveOverlaps(
			[NotNull] RemoveOverlapsRequest request,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			// Unpack request
			bool explodeMultiparts = request.ExplodeMultipartResults;
			bool storeOverlapsAsNewFeatures = request.StoreOverlapsAsNewFeatures;

			IList<IFeature> selectedFeatureList =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.SourceFeatures, request.ClassDefinitions);

			IList<IFeature> targetFeaturesForVertexInsertion =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.UpdatableTargetFeatures, request.ClassDefinitions);

			List<IGeometry> overlaps =
				ProtobufConversionUtils.FromShapeMsgList<IGeometry>(request.Overlaps);

			// Remove overlaps
			OverlapsRemover overlapsRemover = RemoveOverlaps(
				selectedFeatureList, overlaps, targetFeaturesForVertexInsertion, explodeMultiparts,
				storeOverlapsAsNewFeatures, trackCancel);

			// Pack response
			var result = overlapsRemover.Result;

			var response = new RemoveOverlapsResponse();

			PackResultGeometries(result.ResultsByFeature,
			                     response.ResultsByFeature);

			response.NonStorableMessages.AddRange(result.NonStorableMessages);

			if (result.TargetFeaturesToUpdate != null)
			{
				foreach (var keyValuePair in result.TargetFeaturesToUpdate)
				{
					IFeature feature = keyValuePair.Key;
					IGeometry newGeometry = keyValuePair.Value;

					GdbObjectMsg targetFeatureMsg =
						ProtobufConversionUtils.ToGdbObjectMsg(
							feature, newGeometry, feature.Class.ObjectClassID);

					response.TargetFeaturesToUpdate.Add(targetFeatureMsg);
				}
			}

			response.ResultHasMultiparts = result.ResultHasMultiparts;

			return response;
		}

		private static OverlapsRemover RemoveOverlaps(
			[NotNull] IList<IFeature> selectedFeatureList,
			[NotNull] List<IGeometry> overlaps,
			[NotNull] IList<IFeature> targetFeaturesForVertexInsertion,
			bool explodeMultiparts,
			bool storeOverlapsAsNewFeatures,
			[CanBeNull] ITrackCancel trackCancel)
		{
			IPolycurve removePolyline;
			IPolycurve removePolygon;
			RemoveOverlapsUtils.SelectOverlapsToRemove(overlaps, null, false, trackCancel,
			                                           out removePolyline, out removePolygon);

			// Remove overlaps
			var overlapsRemover =
				new OverlapsRemover(explodeMultiparts, storeOverlapsAsNewFeatures);

			if (removePolyline != null)
			{
				overlapsRemover.CalculateResults(
					selectedFeatureList.Where(
						feature =>
							feature.Shape.GeometryType ==
							esriGeometryType.esriGeometryPolyline),
					removePolyline, targetFeaturesForVertexInsertion, trackCancel);
			}

			if (removePolygon != null)
			{
				overlapsRemover.CalculateResults(
					selectedFeatureList.Where(
						feature =>
							feature.Shape.GeometryType ==
							esriGeometryType.esriGeometryPolygon ||
							feature.Shape.GeometryType ==
							esriGeometryType.esriGeometryMultiPatch),
					removePolygon, targetFeaturesForVertexInsertion, trackCancel);
			}

			return overlapsRemover;
		}

		private static void PackResultGeometries(
			[CanBeNull] IList<OverlapResultGeometries> resultsByFeature,
			[NotNull] ICollection<ResultGeometriesByFeature> intoResponseGeometriesByFeature)
		{
			if (resultsByFeature == null) return;

			foreach (OverlapResultGeometries resultByFeature in resultsByFeature)
			{
				var feature = resultByFeature.OriginalFeature;

				var resultGeometriesByFeature = new ResultGeometriesByFeature();

				// Original feature's geometry does not need to be transported back.
				resultGeometriesByFeature.OriginalFeatureRef =
					new GdbObjRefMsg
					{
						ClassHandle = feature.Class.ObjectClassID,
						ObjectId = feature.OID
					};

				resultGeometriesByFeature.UpdatedGeometry =
					ProtobufConversionUtils.ToShapeMsg(resultByFeature.LargestResult);

				foreach (var insertGeometry in resultByFeature.NonLargestResults)
				{
					resultGeometriesByFeature.NewGeometries.Add(
						ProtobufConversionUtils
							.ToShapeMsg(insertGeometry));
				}

				foreach (IGeometry newOverlapFeature in resultByFeature.OverlappingGeometries)
				{
					resultGeometriesByFeature.NewGeometries.Add(
						ProtobufConversionUtils.ToShapeMsg(
							newOverlapFeature));
				}

				intoResponseGeometriesByFeature.Add(resultGeometriesByFeature);
			}
		}
	}
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.RemoveOverlaps;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.Microservices.Server.AO.Geodatabase;

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

			Overlaps selectableOverlaps = RemoveOverlapsUtils.GetSelectableOverlaps(
				sourceFeatures, targetFeatures, trackCancel);

			watch = Stopwatch.StartNew();

			var result = new CalculateOverlapsResponse();

			foreach (var overlapByGdbRef
				in selectableOverlaps.OverlapGeometries)
			{
				var gdbObjRefMsg = ProtobufConversionUtils.ToGdbObjRefMsg(overlapByGdbRef.Key);

				var overlap = overlapByGdbRef.Value;

				var overlapsMsg = new OverlapMsg()
				                  {
					                  OriginalFeatureRef = gdbObjRefMsg
				                  };

				foreach (IGeometry geometry in overlap)
				{
					overlapsMsg.Overlaps.Add(ProtobufConversionUtils.ToShapeMsg(geometry));
				}

				result.Overlaps.Add(overlapsMsg);
			}

			foreach (var notification in selectableOverlaps.Notifications)
			{
				result.Notifications.Add(notification.Message);
			}

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

			GdbTableContainer container = ProtobufConversionUtils.CreateGdbTableContainer(
				request.ClassDefinitions);

			IList<IFeature> selectedFeatureList =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.SourceFeatures, container);

			IList<IFeature> targetFeaturesForVertexInsertion =
				ProtobufConversionUtils.FromGdbObjectMsgList(
					request.UpdatableTargetFeatures, container);

			Overlaps overlaps = new Overlaps();

			foreach (OverlapMsg overlapMsg in request.Overlaps)
			{
				GdbObjectReference gdbRef = new GdbObjectReference(
					overlapMsg.OriginalFeatureRef.ClassHandle,
					overlapMsg.OriginalFeatureRef.ObjectId);

				IFeatureClass fClass = (IFeatureClass) container.GetByClassId(gdbRef.ClassId);

				List<IGeometry> overlapGeometries =
					ProtobufConversionUtils.FromShapeMsgList<IGeometry>(
						overlapMsg.Overlaps,
						DatasetUtils.GetSpatialReference(fClass));

				overlaps.AddGeometries(gdbRef, overlapGeometries);
			}

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
			[NotNull] Overlaps overlaps,
			[NotNull] IList<IFeature> targetFeaturesForVertexInsertion,
			bool explodeMultiparts,
			bool storeOverlapsAsNewFeatures,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var overlapsRemover =
				new OverlapsRemover(explodeMultiparts, storeOverlapsAsNewFeatures);

			overlapsRemover.CalculateResults(
				selectedFeatureList, overlaps, targetFeaturesForVertexInsertion, trackCancel);

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
					ProtobufConversionUtils.ToGdbObjRefMsg(feature);

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

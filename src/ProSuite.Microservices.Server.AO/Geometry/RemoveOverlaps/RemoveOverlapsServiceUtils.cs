using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.RemoveOverlaps;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps
{
	public static class RemoveOverlapsServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static CalculateOverlapsResponse CalculateOverlaps(
			[NotNull] CalculateOverlapsRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			GeometryProcessingUtils.GetFeatures(
				request.SourceFeatures, request.TargetFeatures, request.ClassDefinitions,
				out IList<IFeature> sourceFeatures, out IList<IFeature> targetFeatures);

			_msg.DebugStopTiming(watch, "Unpacked feature lists from request params");

			Overlaps selectableOverlaps = RemoveOverlapsUtils.GetSelectableOverlaps(
				sourceFeatures, targetFeatures, trackCancel);

			watch = Stopwatch.StartNew();

			var result = new CalculateOverlapsResponse();

			foreach (var overlapByGdbRef
			         in selectableOverlaps.OverlapsBySourceRef)
			{
				var gdbObjRefMsg = ProtobufGdbUtils.ToGdbObjRefMsg(overlapByGdbRef.Key);

				var overlap = overlapByGdbRef.Value;

				var overlapsMsg = new OverlapMsg()
				                  {
					                  OriginalFeatureRef = gdbObjRefMsg
				                  };

				foreach (IGeometry geometry in overlap)
				{
					// TODO: At some point the SR-XY tol/res should be transferred separately (via class-lookup?)
					var shapeFormat = ShapeMsg.FormatOneofCase.EsriShape;
					var srFormat = SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml;

					overlapsMsg.Overlaps.Add(
						ProtobufGeometryUtils.ToShapeMsg(
							geometry, shapeFormat, srFormat));

					_msg.VerboseDebug(
						() => $"Calculated overlap: {GeometryUtils.ToString(geometry)}");
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

			int defaultZSourceValue =
				request.DatasetSpecificZSources
				       .FirstOrDefault(dsz => dsz.DatasetName == string.Empty)
				       ?.ZSource ?? (int) ChangeAlongZSource.Target;

			ChangeAlongZSource fallBack = (ChangeAlongZSource) defaultZSourceValue;
			List<DatasetSpecificValue<ChangeAlongZSource>> zSourcesByDatasetName =
				request.DatasetSpecificZSources
				       .Select(dsz => new DatasetSpecificValue<ChangeAlongZSource>(
					               dsz.DatasetName, (ChangeAlongZSource) dsz.ZSource)).ToList();

			IFlexibleSettingProvider<ChangeAlongZSource> zSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					"Z values for changed vertices", fallBack, zSourcesByDatasetName);

			//GdbTableContainer container = ProtobufConversionUtils.CreateGdbTableContainer(
			//	request.ClassDefinitions, null, out _);

			//IList<IFeature> selectedFeatureList =
			//	ProtobufConversionUtils.FromGdbObjectMsgList(
			//		request.SourceFeatures, container);

			//IList<IFeature> targetFeaturesForVertexInsertion =
			//	ProtobufConversionUtils.FromGdbObjectMsgList(
			//		request.UpdatableTargetFeatures, container);

			GeometryProcessingUtils.GetFeatures(
				request.SourceFeatures, request.UpdatableTargetFeatures, request.ClassDefinitions,
				out IList<IFeature> selectedFeatureList,
				out IList<IFeature> targetFeaturesForVertexInsertion);

			Overlaps overlaps = new Overlaps();

			foreach (OverlapMsg overlapMsg in request.Overlaps)
			{
				GdbObjectReference gdbRef = new GdbObjectReference(
					(int) overlapMsg.OriginalFeatureRef.ClassHandle,
					(int) overlapMsg.OriginalFeatureRef.ObjectId);

				IFeatureClass fClass =
					selectedFeatureList
						.Select(f => f.Class)
						.First(c => c.ObjectClassID == gdbRef.ClassId) as IFeatureClass;

				Assert.NotNull(fClass);

				List<IGeometry> overlapGeometries =
					ProtobufGeometryUtils.FromShapeMsgList<IGeometry>(
						overlapMsg.Overlaps,
						DatasetUtils.GetSpatialReference(fClass));

				overlaps.AddGeometries(gdbRef, overlapGeometries);
			}

			// Remove overlaps
			OverlapsRemover overlapsRemover = RemoveOverlaps(
				selectedFeatureList, overlaps, targetFeaturesForVertexInsertion, explodeMultiparts,
				storeOverlapsAsNewFeatures, zSourceProvider, trackCancel);

			// Pack response
			RemoveOverlapsResult result = overlapsRemover.Result;

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
						ProtobufGdbUtils.ToGdbObjectMsg(
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
			IFlexibleSettingProvider<ChangeAlongZSource> zSourceProvider,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var overlapsRemover =
				new OverlapsRemover(explodeMultiparts, storeOverlapsAsNewFeatures)
				{
					ZSourceProvider = zSourceProvider
				};

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
					ProtobufGdbUtils.ToGdbObjRefMsg(feature);

				resultGeometriesByFeature.UpdatedGeometry =
					ProtobufGeometryUtils.ToShapeMsg(resultByFeature.LargestResult);

				foreach (var insertGeometry in resultByFeature.NonLargestResults)
				{
					resultGeometriesByFeature.NewGeometries.Add(
						ProtobufGeometryUtils.ToShapeMsg(insertGeometry));
				}

				foreach (IGeometry newOverlapFeature in resultByFeature.OverlappingGeometries)
				{
					resultGeometriesByFeature.NewGeometries.Add(
						ProtobufGeometryUtils.ToShapeMsg(
							newOverlapFeature));
				}

				intoResponseGeometriesByFeature.Add(resultGeometriesByFeature);
			}
		}
	}
}

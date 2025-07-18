using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Notifications;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps
{
	public static class RemoveOverlapsClientUtils
	{
		#region Calculate Overlaps

		[CanBeNull]
		public static Overlaps CalculateOverlaps(
			[NotNull] RemoveOverlapsGrpc.RemoveOverlapsGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> overlappingFeatures,
			[CanBeNull] Envelope inExtent,
			CancellationToken cancellationToken)
		{
			CalculateOverlapsResponse response =
				CalculateOverlapsRpc(rpcClient, selectedFeatures, overlappingFeatures, inExtent,
				                     cancellationToken);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			var result = new Overlaps();

			// Get the spatial reference from a shape (== map spatial reference) rather than a feature class.
			SpatialReference spatialReference = selectedFeatures
			                                    .Select(f => f.GetShape().SpatialReference)
			                                    .FirstOrDefault();

			foreach (OverlapMsg overlapMsg in response.Overlaps)
			{
				GdbObjectReference gdbObjRef = new GdbObjectReference(
					overlapMsg.OriginalFeatureRef.ClassHandle,
					overlapMsg.OriginalFeatureRef.ObjectId);

				List<Geometry> overlapGeometries =
					ProtobufConversionUtils.FromShapeMsgList(overlapMsg.Overlaps, spatialReference);

				result.AddGeometries(gdbObjRef, overlapGeometries);
			}

			result.Notifications.AddRange(
				response.Notifications.Select(n => new Notification(n)));

			return result;
		}

		[CanBeNull]
		private static CalculateOverlapsResponse CalculateOverlapsRpc(
			[NotNull] RemoveOverlapsGrpc.RemoveOverlapsGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> overlappingFeatures,
			[CanBeNull] Envelope inExtent,
			CancellationToken cancellationToken)
		{
			CalculateOverlapsRequest request =
				CreateCalculateOverlapsRequest(selectedFeatures, overlappingFeatures, inExtent);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * selectedFeatures.Count;

			CalculateOverlapsResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.CalculateOverlaps(request, o),
					cancellationToken, deadline);

			return response;
		}

		private static CalculateOverlapsRequest CreateCalculateOverlapsRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> overlappingFeatures,
			[CanBeNull] Envelope inExtent)
		{
			var request = new CalculateOverlapsRequest();

			Func<Feature, Geometry> getFeatureGeometry = null;

			if (inExtent != null)
			{
				getFeatureGeometry = f => GetClippedGeometry(f, inExtent);
			}

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions,
			                                           false, getFeatureGeometry);

			ProtobufConversionUtils.ToGdbObjectMsgList(overlappingFeatures,
			                                           request.TargetFeatures,
			                                           request.ClassDefinitions,
			                                           false, getFeatureGeometry);

			return request;
		}

		private static Geometry GetClippedGeometry(Feature feature, Envelope extent)
		{
			Geometry geometry = feature.GetShape();

			if (geometry.GeometryType != GeometryType.Polygon &&
			    geometry.GeometryType != GeometryType.Polyline)
			{
				// Multipatches etc.:
				return geometry;
			}

			geometry = GeometryEngine.Instance.Clip(geometry, extent);

			return geometry.IsEmpty ? null : geometry;
		}

		#endregion

		#region Remove Overlaps

		public static RemoveOverlapsResult RemoveOverlaps(
			[NotNull] RemoveOverlapsGrpc.RemoveOverlapsGrpcClient rpcClient,
			[NotNull] IEnumerable<Feature> selectedFeatures,
			[NotNull] Overlaps overlapsToRemove,
			[CanBeNull] IList<Feature> overlappingFeatures,
			[NotNull] RemoveOverlapsOptions options,
			CancellationToken cancellationToken)
		{
			List<Feature> updateFeatures;
			RemoveOverlapsRequest request = CreateRemoveOverlapsRequest(
				selectedFeatures, overlapsToRemove, overlappingFeatures, options,
				out updateFeatures);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() *
			               request.SourceFeatures.Count;

			RemoveOverlapsResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.RemoveOverlaps(request, o),
					cancellationToken, deadline);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			return GetRemoveOverlapsResult(response, updateFeatures);
		}

		private static RemoveOverlapsResult GetRemoveOverlapsResult(
			RemoveOverlapsResponse response,
			List<Feature> updateFeatures)
		{
			// unpack
			var result = new RemoveOverlapsResult
			             {
				             ResultHasMultiparts = response.ResultHasMultiparts
			             };

			IList<OverlapResultGeometries> resultGeometriesByFeature = result.ResultsByFeature;

			// match the selected features with the protobuf features -> use GdbObjRef (shapefile support!)

			ReAssociateResponseGeometries(response, resultGeometriesByFeature,
			                              updateFeatures);

			if (response.TargetFeaturesToUpdate != null)
			{
				result.TargetFeaturesToUpdate = new Dictionary<Feature, Geometry>();

				foreach (GdbObjectMsg targetMsg in response.TargetFeaturesToUpdate)
				{
					Feature originalFeature =
						GetOriginalFeature(targetMsg.ObjectId, targetMsg.ClassHandle,
						                   updateFeatures);

					// It's important to assign the full spatial reference from the original to avoid
					// losing the VCS:
					SpatialReference sr = originalFeature.GetShape().SpatialReference;

					result.TargetFeaturesToUpdate.Add(
						originalFeature, ProtobufConversionUtils.FromShapeMsg(targetMsg.Shape, sr));
				}
			}

			foreach (string message in response.NonStorableMessages)
			{
				result.NonStorableMessages.Add(message);
			}

			return result;
		}

		private static void ReAssociateResponseGeometries(
			RemoveOverlapsResponse response,
			IList<OverlapResultGeometries> results,
			List<Feature> updateFeatures)
		{
			foreach (var resultByFeature in response.ResultsByFeature)
			{
				GdbObjRefMsg featureRef = resultByFeature.OriginalFeatureRef;

				Feature originalFeature = GetOriginalFeature(featureRef, updateFeatures);

				// It's important to assign the full spatial reference from the original to avoid
				// losing the VCS. Get it from the shape, because all calculations are in Map SR!
				SpatialReference sr = originalFeature.GetShape().SpatialReference;

				Geometry updatedGeometry =
					ProtobufConversionUtils.FromShapeMsg(resultByFeature.UpdatedGeometry, sr);

				List<Geometry> newGeometries =
					ProtobufConversionUtils.FromShapeMsgList(resultByFeature.NewGeometries, sr);

				var overlapResultGeometries = new OverlapResultGeometries(
					originalFeature, Assert.NotNull(updatedGeometry), newGeometries);

				results.Add(overlapResultGeometries);
			}
		}

		private static Feature GetOriginalFeature(GdbObjRefMsg featureBuffer,
		                                          List<Feature> updateFeatures)
		{
			// consider using anything unique as an identifier, e.g. a GUID
			long classId = featureBuffer.ClassHandle;
			long objectId = featureBuffer.ObjectId;

			return GetOriginalFeature(objectId, classId, updateFeatures);
		}

		private static Feature GetOriginalFeature(long objectId, long classId,
		                                          List<Feature> updateFeatures)
		{
			return updateFeatures.First(f => f.GetObjectID() == objectId &&
			                                 GeometryProcessingUtils.GetUniqueClassId(f) ==
			                                 classId);
		}

		private static RemoveOverlapsRequest CreateRemoveOverlapsRequest(
			[NotNull] IEnumerable<Feature> selectedFeatures,
			[NotNull] Overlaps overlapsToRemove,
			[CanBeNull] IList<Feature> targetFeaturesForVertexInsertion,
			[NotNull] RemoveOverlapsOptions options,
			out List<Feature> updateFeatures)
		{
			var request = new RemoveOverlapsRequest
			              {
				              ExplodeMultipartResults = options.ExplodeMultipartResults,
				              StoreOverlapsAsNewFeatures =
					              false // options.StoreOverlapsAsNewFeatures
			              };

			DatasetSpecificSettingProvider<ChangeAlongZSource> datasetSpecificValues =
				options.GetZSourceOptionProvider() as
					DatasetSpecificSettingProvider<ChangeAlongZSource>;

			if (datasetSpecificValues?.DatasetSpecificValues != null)
			{
				var datasetZSources = datasetSpecificValues.DatasetSpecificValues.Select(
					dss => new DatasetZSource
					       {
						       DatasetName = dss.Dataset,
						       ZSource = (int) dss.Value
					       });

				request.ZSources.AddRange(datasetZSources);
			}

			// Add the fallback value with an empty string as dataset name
			request.ZSources.Add(
				new DatasetZSource
				{
					DatasetName = string.Empty,
					ZSource = (int) options.ZSource
				});

			updateFeatures = new List<Feature>();

			var selectedFeatureList = CollectionUtils.GetCollection(selectedFeatures);

			ProtobufConversionUtils.ToGdbObjectMsgList(
				selectedFeatureList, request.SourceFeatures, request.ClassDefinitions);

			updateFeatures.AddRange(selectedFeatureList);

			foreach (var overlapsBySourceRef in overlapsToRemove.OverlapGeometries)
			{
				int classId = (int) overlapsBySourceRef.Key.ClassId;
				int objectId = (int) overlapsBySourceRef.Key.ObjectId;

				var overlapMsg = new OverlapMsg();
				overlapMsg.OriginalFeatureRef = new GdbObjRefMsg()
				                                {
					                                ClassHandle = classId,
					                                ObjectId = objectId
				                                };

				foreach (Geometry overlap in overlapsBySourceRef.Value)
				{
					overlapMsg.Overlaps.Add(ProtobufConversionUtils.ToShapeMsg(overlap, true));
				}

				request.Overlaps.Add(overlapMsg);
			}

			if (targetFeaturesForVertexInsertion != null && options.InsertVerticesInTarget)
			{
				ProtobufConversionUtils.ToGdbObjectMsgList(
					targetFeaturesForVertexInsertion, request.UpdatableTargetFeatures,
					request.ClassDefinitions);

				updateFeatures.AddRange(targetFeaturesForVertexInsertion);
			}

			return request;
		}

		#endregion
	}
}

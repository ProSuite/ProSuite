using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps
{
	public static class RemoveOverlapsClientUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Calculate Overlaps

		[CanBeNull]
		public static Overlaps CalculateOverlaps(
			RemoveOverlapsGrpc.RemoveOverlapsGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> overlappingFeatures,
			CancellationToken cancellationToken)
		{
			CalculateOverlapsResponse response =
				CalculateOverlapsRpc(rpcClient, selectedFeatures, overlappingFeatures,
				                     cancellationToken);

			if (cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			var result = new Overlaps();

			foreach (OverlapMsg overlapMsg in response.Overlaps)
			{
				GdbObjectReference gdbObjRef = new GdbObjectReference(
					overlapMsg.OriginalFeatureRef.ClassHandle,
					overlapMsg.OriginalFeatureRef.ObjectId);

				List<Geometry> overlapGeometries =
					ProtobufConversionUtils.FromShapeMsgList(overlapMsg.Overlaps);

				result.AddGeometries(gdbObjRef, overlapGeometries);
			}

			result.Notifications.AddRange(
				response.Notifications.Select(n => new Notification(n)));

			return result;
		}

		private static CalculateOverlapsResponse CalculateOverlapsRpc(
			RemoveOverlapsGrpc.RemoveOverlapsGrpcClient rpcClient,
			IList<Feature> selectedFeatures,
			IList<Feature> overlappingFeatures,
			CancellationToken cancellationToken)
		{
			CalculateOverlapsRequest request =
				CreateCalculateOverlapsRequest(selectedFeatures, overlappingFeatures);

			CalculateOverlapsResponse response;

			try
			{
				response = rpcClient.CalculateOverlaps(request, null, null,
				                                       cancellationToken);
			}
			catch (Exception e)
			{
				_msg.Debug($"Error calling remote procedure: {e.Message} ", e);

				throw;
			}

			return response;
		}

		private static CalculateOverlapsRequest CreateCalculateOverlapsRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> overlappingFeatures)
		{
			var request = new CalculateOverlapsRequest();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions);

			ProtobufConversionUtils.ToGdbObjectMsgList(overlappingFeatures,
			                                           request.TargetFeatures,
			                                           request.ClassDefinitions);

			return request;
		}

		#endregion

		#region Remove Overlaps

		public static RemoveOverlapsResult RemoveOverlaps(
			RemoveOverlapsGrpc.RemoveOverlapsGrpcClient rpcClient,
			IEnumerable<Feature> selectedFeatures,
		                                                  Overlaps overlapsToRemove,
		                                                  IList<Feature> overlappingFeatures,
		                                                  CancellationToken cancellationToken)
		{
			List<Feature> updateFeatures;
			RemoveOverlapsRequest request = CreateRemoveOverlapsRequest(
				selectedFeatures, overlapsToRemove, overlappingFeatures,
				out updateFeatures);

			RemoveOverlapsResponse response =
				rpcClient.RemoveOverlaps(request, null, null, cancellationToken);

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

					result.TargetFeaturesToUpdate.Add(
						originalFeature,
						ProtobufConversionUtils.FromShapeMsg(targetMsg.Shape));
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

				Geometry updatedGeometry =
					ProtobufConversionUtils.FromShapeMsg(resultByFeature.UpdatedGeometry);

				List<Geometry> newGeometries =
					ProtobufConversionUtils.FromShapeMsgList(resultByFeature.NewGeometries);

				var overlapResultGeometries = new OverlapResultGeometries(
					originalFeature, Assert.NotNull(updatedGeometry), newGeometries);

				results.Add(overlapResultGeometries);
			}
		}

		private static Feature GetOriginalFeature(GdbObjRefMsg featureBuffer,
		                                          List<Feature> updateFeatures)
		{
			// consider using anything unique as an identifier, e.g. a GUID
			int classId = featureBuffer.ClassHandle;
			int objectId = featureBuffer.ObjectId;

			return GetOriginalFeature(objectId, classId, updateFeatures);
		}

		private static Feature GetOriginalFeature(int objectId, int classId,
		                                          List<Feature> updateFeatures)
		{
			return updateFeatures.First(f => f.GetObjectID() == objectId &&
			                                 f.GetTable().GetID() == classId);
		}


		private static RemoveOverlapsRequest CreateRemoveOverlapsRequest(
			IEnumerable<Feature> selectedFeatures,
			Overlaps overlapsToRemove,
			IList<Feature> targetFeaturesForVertexInsertion, //RemoveOverlapsOptions options,
			out List<Feature> updateFeatures)
		{
			var request = new RemoveOverlapsRequest
			              {
				              ExplodeMultipartResults = true, // options.ExplodeMultipartResults,
				              StoreOverlapsAsNewFeatures =
					              false // options.StoreOverlapsAsNewFeatures
			              };

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

			if (targetFeaturesForVertexInsertion != null)
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

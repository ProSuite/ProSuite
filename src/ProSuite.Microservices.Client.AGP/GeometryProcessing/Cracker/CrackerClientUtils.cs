using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.Cracker
{
	public static class CrackerClientUtils
	{
		#region Calculate CrackPoints

		[CanBeNull]
		public static CrackPoints CalculateCrackPoints(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> intersectingFeatures,
			CancellationToken cancellationToken)
		{
			CalculateCrackPointsResponse response =
				CalculateCrackPointsRpc(rpcClient, selectedFeatures, intersectingFeatures,
				                        cancellationToken);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			var result = new CrackPoints();

			// Get the spatial reference from a shape (== map spatial reference) rather than a feature class.
			SpatialReference spatialReference = selectedFeatures
			                                    .Select(f => f.GetShape().SpatialReference)
			                                    .FirstOrDefault();

			foreach (CrackPointsMsg crackPointMsg in response.CrackPoints)
			{
				GdbObjectReference gdbObjRef = new GdbObjectReference(
					crackPointMsg.OriginalFeatureRef.ClassHandle,
					crackPointMsg.OriginalFeatureRef.ObjectId);

				List<Geometry> intersectGeometries =
					ProtobufConversionUtils.FromShapeMsgList(crackPointMsg.CrackPoints, spatialReference);

				result.AddGeometries(gdbObjRef, intersectGeometries);
			}

			result.Notifications.AddRange(
				response.Notifications.Select(n => new Notification(n)));

			return result;
		}

		[CanBeNull]
		private static CalculateCrackPointsResponse CalculateCrackPointsRpc(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> intersectingFeatures,
			CancellationToken cancellationToken)
		{
			CalculateCrackPointsRequest request =
				CreateCalculateCrackPointsRequest(selectedFeatures, intersectingFeatures);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * selectedFeatures.Count;

			CalculateCrackPointsResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.CalculateCrackPoints(request, o),
					cancellationToken, deadline);

			return response;
		}

		private static CalculateCrackPointsRequest CreateCalculateCrackPointsRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> intersectingFeatures)
		{
			var request = new CalculateCrackPointsRequest();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions);

			ProtobufConversionUtils.ToGdbObjectMsgList(intersectingFeatures,
			                                           request.TargetFeatures,
			                                           request.ClassDefinitions);

			return request;
		}

		#endregion

		#region Insert CrackPoints

		public static CrackerResult InsertCrackPoints(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			[NotNull] IEnumerable<Feature> selectedFeatures,
			[NotNull] CrackPoints crackPointsToAdd,
			[CanBeNull] IList<Feature> intersectingFeatures,
			CancellationToken cancellationToken)
		{
			List<Feature> updateFeatures;
			CalculateCrackPointsRequest request = CreateCalculateCrackPointsRequest(
				selectedFeatures, intersectingFeatures);


			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() *
			               request.SourceFeatures.Count;

			CalculateCrackPointsResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.CalculateCrackPoints(request, o),
					cancellationToken, deadline);

			return GetCalculateCrackPointsResponse(response, updateFeatures);
		}

		private static CalculateCrackPointsResponse GetCalculateCrackPointsResponse(
			CalculateCrackPointsResponse response,
			List<Feature> updateFeatures)
		{
			// unpack 
			var result = new CrackerResult
			             {
				            
			             };

			IList<CrackerResultPoints> resultPointsByFeature = result.ResultsByFeature;

			// match the selected features with the protobuf features -> use GdbObjRef (shapefile support!)

			ReAssociateResponsePoints(response, resultPointsByFeature,
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

		private static void ReAssociateResponsePoints(
			RemoveOverlapsResponse response,
			IList<CrackerResultPoints> results,
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

				var crackerResultPoints = new CrackerResultPoints(
					originalFeature, Assert.NotNull(updatedGeometry), newGeometries);

				results.Add(crackerResultPoints);
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
			[NotNull] CrackPoints crackPointsToAdd,
			[CanBeNull]
			IList<Feature> targetFeaturesForVertexInsertion, //RemoveOverlapsOptions options,
			out List<Feature> updateFeatures)
		{
			var request = new ApplyCrackPointsRequest
			              {
		
			              };

			updateFeatures = new List<Feature>();

			var selectedFeatureList = CollectionUtils.GetCollection(selectedFeatures);

			ProtobufConversionUtils.ToGdbObjectMsgList(
				selectedFeatureList, request.SourceFeatures, request.ClassDefinitions);

			updateFeatures.AddRange(selectedFeatureList);

			foreach (var crackPointsBySourceRef in crackPointsToAdd.CrackPointLocations)
			{
				int classId = (int)crackPointsBySourceRef.Key.ClassId;
				int objectId = (int)crackPointsBySourceRef.Key.ObjectId;

				var crackPointsMsg = new CrackPointsMsg();
				crackPointsMsg.OriginalFeatureRef = new GdbObjRefMsg()
				                                    {
					                                    ClassHandle = classId,
					                                    ObjectId = objectId
				                                    };

				foreach (Geometry crackPoint in crackPointsBySourceRef.Value)
				{
					crackPointsMsg.CrackPoints.Add(ProtobufConversionUtils.ToShapeMsg(crackPoint, true));
				}

				request.CrackPoints.Add(crackPointsMsg);
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

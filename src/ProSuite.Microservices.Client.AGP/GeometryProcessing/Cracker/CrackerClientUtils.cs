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
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.Cracker
{
	public static class CrackerClientUtils
	{
		#region Calculate CrackPoints

		[CanBeNull]
		public static CrackerResult CalculateCrackPoints(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> intersectingFeatures,
			ICrackerToolOptions crackerToolOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices,
			CancellationToken cancellationToken)
		{
			CalculateCrackPointsResponse response =
				CalculateCrackPointsRpc(rpcClient, selectedFeatures, intersectingFeatures,
				                        crackerToolOptions, intersectionPointOptions,
				                        addCrackPointsOnExistingVertices, cancellationToken);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			// match the selected features with the protobuf features -> use GdbObjRef (shapefile support!)

			var resultFeaturesWithCrackPoints = new List<CrackedFeature>();

			ReAssociateResponsePoints(response, resultFeaturesWithCrackPoints, selectedFeatures);

			var result = new CrackerResult { };
			result.ResultsByFeature = resultFeaturesWithCrackPoints;
			//if (response.CrackPoints != null)
			//{
			//	result.TargetFeaturesToUpdate = new Dictionary<Feature, Geometry>();

			//	foreach (CrackPointsMsg crackPointsMsg in response.CrackPoints)
			//	{
			//		GdbObjRefMsg gdbObjRefMsg = crackPointsMsg.OriginalFeatureRef;

			//		Feature originalFeature =
			//			GetOriginalFeature(gdbObjRefMsg.ObjectId, gdbObjRefMsg.ClassHandle,
			//			                   updateFeatures);
			//	}
			//}

			// TODO: Handle notifications/nonstrorable messages

			return result;

			//var result = new CrackPoints();

			//// Get the spatial reference from a shape (== map spatial reference) rather than a feature class.
			//SpatialReference spatialReference = selectedFeatures
			//                                    .Select(f => f.GetShape().SpatialReference)
			//                                    .FirstOrDefault();

			//foreach (CrackPointsMsg crackPointMsg in response.CrackPoints)
			//{
			//	GdbObjectReference gdbObjRef = new GdbObjectReference(
			//		crackPointMsg.OriginalFeatureRef.ClassHandle,
			//		crackPointMsg.OriginalFeatureRef.ObjectId);

			//	CrackPoints newCrackPoints =
			//		ProtobufConversionUtils.FromShapeMsgList(
			//			crackPointMsg.CrackPoints, spatialReference);

			//	result.AddCrackPoints(gdbObjRef, (IList<CrackPoint>) newCrackPoints);
			//}

			//result.Notifications.AddRange(
			//	response.Notifications.Select(n => new Notification(n)));

			return result;
		}

		[CanBeNull]
		private static CalculateCrackPointsResponse CalculateCrackPointsRpc(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> intersectingFeatures,
			ICrackerToolOptions crackerToolOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices,
			CancellationToken cancellationToken)
		{
			CalculateCrackPointsRequest request =
				CreateCalculateCrackPointsRequest(selectedFeatures, intersectingFeatures,
				                                  crackerToolOptions,
				                                  intersectionPointOptions,
				                                  addCrackPointsOnExistingVertices);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * selectedFeatures.Count;

			CalculateCrackPointsResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.CalculateCrackPoints(request, o),
					cancellationToken, deadline);

			return response;
		}

		private static CalculateCrackPointsRequest CreateCalculateCrackPointsRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> intersectingFeatures,
			ICrackerToolOptions crackerToolOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices)
		{
			var request = new CalculateCrackPointsRequest();
			request.CrackOptions = new CrackOptionsMsg();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions);

			ProtobufConversionUtils.ToGdbObjectMsgList(intersectingFeatures,
			                                           request.TargetFeatures,
			                                           request.ClassDefinitions);

			request.CrackOptions = ToCrackerToolOptionsMsg(crackerToolOptions,
														   intersectionPointOptions,
			                                               addCrackPointsOnExistingVertices);

			return request;
		}

		private static CrackOptionsMsg ToCrackerToolOptionsMsg(
			ICrackerToolOptions crackerToolOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices)
		{
			var result = new CrackOptionsMsg();

			result.CrackOnlyWithinSameClass =
				crackerToolOptions.TargetFeatureSelection == TargetFeatureSelection.SameClass;
			result.RespectMinimumSegmentLength = crackerToolOptions.RespectMinimumSegmentLength;
			result.MinimumSegmentLength = crackerToolOptions.MinimumSegmentLength;
			result.SnapToTargetVertices = crackerToolOptions.SnapToTargetVertices;
			result.SnapTolerance = crackerToolOptions.SnapTolerance;
			result.UseSourceZs = crackerToolOptions.UseSourceZs;
			result.ExcludeInteriorInteriorIntersection =
				crackerToolOptions.ExcludeInteriorInteriorIntersections;
			result.IntersectionPointOptions = (int) intersectionPointOptions;
			result.AddCrackPointsOnExistingVertices = addCrackPointsOnExistingVertices;

			return result;
		}

		#endregion

		#region Insert CrackPoints

		//public static CrackerResult InsertCrackPoints(
		//	[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
		//	[NotNull] IList<Feature> selectedFeatures,
		//	[NotNull] IList<Feature> intersectingFeatures,
		//	[NotNull] CrackPoints crackPointsToAdd,
		//	List<Feature> updateFeatures,
		//	CancellationToken cancellationToken)
		//{
		//	CalculateCrackPointsRequest request = CreateCalculateCrackPointsRequest(
		//		selectedFeatures, intersectingFeatures);

		//	int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() *
		//	               request.SourceFeatures.Count;

		//	CalculateCrackPointsResponse response =
		//		GrpcClientUtils.Try(
		//			o => rpcClient.CalculateCrackPoints(request, o),
		//			cancellationToken, deadline);

		//	return GetCalculateCrackPointsResult(response, updateFeatures);
		//}

		//private static CrackerResult GetCalculateCrackPointsResult(
		//	CalculateCrackPointsResponse response,
		//	List<Feature> updateFeatures)
		//{
		//	// unpack 
		//	var result = new CrackerResult
		//	             { };
		//	// match the selected features with the protobuf features -> use GdbObjRef (shapefile support!)

		//	var resultFeaturesWithCrackPoints = new List<CrackPoints>();

		//	ReAssociateResponsePoints(response, resultFeaturesWithCrackPoints,                          updateFeatures);

		//	result.ResultsByFeature = resultFeaturesWithCrackPoints;
		//	//if (response.CrackPoints != null)
		//	//{
		//	//	result.TargetFeaturesToUpdate = new Dictionary<Feature, Geometry>();

		//	//	foreach (CrackPointsMsg crackPointsMsg in response.CrackPoints)
		//	//	{
		//	//		GdbObjRefMsg gdbObjRefMsg = crackPointsMsg.OriginalFeatureRef;

		//	//		Feature originalFeature =
		//	//			GetOriginalFeature(gdbObjRefMsg.ObjectId, gdbObjRefMsg.ClassHandle,
		//	//			                   updateFeatures);
		//	//	}
		//	//}

		//	return result;
		//}

		private static void ReAssociateResponsePoints(
			CalculateCrackPointsResponse response,
			IList<CrackedFeature> results,
			IList<Feature> updateFeatures)
		{
			foreach (var returnedFeature in response.CrackPoints)
			{
				GdbObjRefMsg featureRef = returnedFeature.OriginalFeatureRef;

				Feature originalFeature = GetOriginalFeature(featureRef, updateFeatures);

				var originalFeatureRef = new GdbObjectReference(originalFeature);

				CrackedFeature crackPointsPerFeature = new CrackedFeature(originalFeature);

				// It's important to assign the full spatial reference from the original to avoid
				// losing the VCS. Get it from the shape, because all calculations are in Map SR!
				SpatialReference sr = originalFeature.GetShape().SpatialReference;

				foreach (CrackPointMsg crackPointMsg in returnedFeature.CrackPoints)
				{
					MapPoint point =
						(MapPoint) ProtobufConversionUtils.FromShapeMsg(crackPointMsg.Point, sr);

					var crackPoint = new CrackPoint(Assert.NotNull(point));
					crackPoint.ViolatesMinimumSegmentLength =
						crackPointMsg.ViolatesMinimumSegmentLength;
					crackPoint.TargetVertexOnlyDifferentInZ =
						crackPointMsg.TargetVertexOnlyDifferentInZ;
					crackPoint.TargetVertexDifferentWithinTolerance =
						crackPointMsg.TargetVertexDifferentWithinTolerance;

					crackPointsPerFeature.CrackPoints.Add(crackPoint);
				}

				//var crackerResultPoints = new CrackerResultPoints(
				//	originalFeature,
				//	ProtobufConversionUtils.FromShapeMsgList(returnedFeature.CrackPoints, sr));

				results.Add(crackPointsPerFeature);
			}
		}

		private static Feature GetOriginalFeature(GdbObjRefMsg featureBuffer,
		                                          IList<Feature> updateFeatures)
		{
			// consider using anything unique as an identifier, e.g. a GUID
			long classId = featureBuffer.ClassHandle;
			long objectId = featureBuffer.ObjectId;

			return GetOriginalFeature(objectId, classId, updateFeatures);
		}

		private static Feature GetOriginalFeature(long objectId, long classId,
		                                          IList<Feature> updateFeatures)
		{
			return updateFeatures.First(f => f.GetObjectID() == objectId &&
			                                 GeometryProcessingUtils.GetUniqueClassId(f) ==
			                                 classId);
		}

		//private static CaculateCrackPointsRequest CreateCaculateCrackPointsRequest(
		//	[NotNull] IEnumerable<Feature> selectedFeatures,
		//	[NotNull] CrackPoints crackPointsToAdd,
		//	out List<Feature> updateFeatures)
		//{
		//	var request = new CaculateCrackPointsRequest { };

		//	updateFeatures = new List<Feature>();

		//	var selectedFeatureList = CollectionUtils.GetCollection(selectedFeatures);

		//	ProtobufConversionUtils.ToGdbObjectMsgList(
		//		selectedFeatureList, request.SourceFeatures, request.ClassDefinitions);

		//	updateFeatures.AddRange(selectedFeatureList);

		//	foreach (var crackPointsBySourceRef in crackPointsToAdd.CrackPointLocations)
		//	{
		//		int classId = (int) crackPointsBySourceRef.Key.ClassId;
		//		int objectId = (int) crackPointsBySourceRef.Key.ObjectId;

		//		var crackPointsMsg = new CrackPointsMsg();
		//		crackPointsMsg.OriginalFeatureRef = new GdbObjRefMsg()
		//		                                    {
		//			                                    ClassHandle = classId,
		//			                                    ObjectId = objectId
		//		                                    };

		//		foreach (Geometry crackPoint in crackPointsBySourceRef.Value)
		//		{
		//			crackPointsMsg.CrackPoints.Add(
		//				ProtobufConversionUtils.ToShapeMsg(crackPoint, true));
		//		}

		//		request.CrackPoints.Add(crackPointsMsg);
		//	}

		//	return request;
		//}

		#endregion

		public static IList<ResultFeature> ApplyCrackPoints(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			IEnumerable<Feature> selectedFeatures,
			CrackerResult crackPointsToAdd,
			IList<Feature> intersectingFeatures,
			ICrackerToolOptions crackerToolOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices,
			CancellationToken cancellationToken)
		{
			ApplyCrackPointsRequest request =
				CreateApplyCrackPointsRequest(selectedFeatures, crackPointsToAdd,
				                              crackerToolOptions, intersectionPointOptions,
				                              addCrackPointsOnExistingVertices,
				                              out List<Feature> updatedFeatures);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() *
			               request.SourceFeatures.Count;

			ApplyCrackPointsResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.ApplyCrackPoints(request, o),
					cancellationToken, deadline);

			return GetApplyCrackPointsResult(response, updatedFeatures);
		}

		private static ApplyCrackPointsRequest CreateApplyCrackPointsRequest(
			[NotNull] IEnumerable<Feature> selectedFeatures,
			[NotNull] CrackerResult crackPointsToAdd,
			ICrackerToolOptions crackerToolOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices,
			out List<Feature> updateFeatures)
		{
			var request = new ApplyCrackPointsRequest();
			request.CrackOptions = new CrackOptionsMsg();

			updateFeatures = new List<Feature>();

			var selectedFeatureList = CollectionUtils.GetCollection(selectedFeatures);

			ProtobufConversionUtils.ToGdbObjectMsgList(
				selectedFeatureList, request.SourceFeatures, request.ClassDefinitions);

			updateFeatures.AddRange(selectedFeatureList);

			foreach (var crackedFeature in crackPointsToAdd.ResultsByFeature)
			{
				int classId = (int) crackedFeature.GdbFeatureReference.ClassId;
				int objectId = (int) crackedFeature.GdbFeatureReference.ObjectId;

				var crackPointsMsg = new CrackPointsMsg();
				crackPointsMsg.OriginalFeatureRef =
					new GdbObjRefMsg()
					{
						ClassHandle = classId,
						ObjectId = objectId
					};

				foreach (var crackPoint in crackedFeature.CrackPoints)
				{
					var crackPointMsg =
						new CrackPointMsg()
						{
							Point = ProtobufConversionUtils.ToShapeMsg(
								crackPoint.Point, true),
							ViolatesMinimumSegmentLength =
								crackPoint.ViolatesMinimumSegmentLength,
							TargetVertexOnlyDifferentInZ =
								crackPoint.TargetVertexOnlyDifferentInZ,
							TargetVertexDifferentWithinTolerance =
								crackPoint.TargetVertexDifferentWithinTolerance
						};
					crackPointsMsg.CrackPoints.Add(crackPointMsg);
				}

				request.CrackPoints.Add(crackPointsMsg);
			}

			request.CrackOptions = ToCrackerToolOptionsMsg(crackerToolOptions,
														   intersectionPointOptions,
			                                               addCrackPointsOnExistingVertices);

			return request;
		}

		private static List<ResultFeature> GetApplyCrackPointsResult(
			ApplyCrackPointsResponse response,
			List<Feature> updateFeatures)
		{
			// unpack

			var featuresByObjRef = new Dictionary<GdbObjectReference, Feature>();

			FeatureProcessingUtils.AddInputFeatures(updateFeatures, featuresByObjRef);

			//var result = new RemoveOverlapsResult {
			//	                                      ResultHasMultiparts = response.ResultHasMultiparts
			//                                      };

			//IList<OverlapResultGeometries> resultGeometriesByFeature = result.ResultsByFeature;

			// match the selected features with the protobuf features -> use GdbObjRef (shapefile support!)

			SpatialReference resultSpatialReference =
				updateFeatures.FirstOrDefault()?.GetShape().SpatialReference;

			var resultFeatures = new List<ResultFeature>(
				FeatureDtoConversionUtils.FromUpdateMsgs(response.ResultFeatures, featuresByObjRef,
				                                         resultSpatialReference));

			return resultFeatures;
			//foreach (ResultObjectMsg resultFeatureMsg in response.ResultFeatures)
			//  {
			//   resultFeatureMsg.
			//  }

			//  ReAssociateResponsePoints(response, resultGeometriesByFeature,
			//                            updateFeatures);

			//ReAssociateResponseGeometries(response, resultGeometriesByFeature,
			//                              updateFeatures);

			//if (response.TargetFeaturesToUpdate != null) {
			//	result.TargetFeaturesToUpdate = new Dictionary<Feature, Geometry>();

			//	foreach (GdbObjectMsg targetMsg in response.TargetFeaturesToUpdate) {
			//		Feature originalFeature =
			//			GetOriginalFeature(targetMsg.ObjectId, targetMsg.ClassHandle,
			//			                   updateFeatures);

			//		// It's important to assign the full spatial reference from the original to avoid
			//		// losing the VCS:
			//		SpatialReference sr = originalFeature.GetShape().SpatialReference;

			//		result.TargetFeaturesToUpdate.Add(
			//			originalFeature, ProtobufConversionUtils.FromShapeMsg(targetMsg.Shape, sr));
			//	}
			//}

			//foreach (string message in response.NonStorableMessages) {
			//	result.NonStorableMessages.Add(message);
			//}

			//return result;
		}

		public static IList<ResultFeature> ChopLines(
			[NotNull] CrackGrpc.CrackGrpcClient rpcClient,
			IEnumerable<Feature> selectedFeatures,
			CrackerResult crackPointsToAdd,
			IList<Feature> intersectingFeatures,
			ICrackerToolOptions chopperOptions,
			IntersectionPointOptions intersectionPointOptions,
			bool addCrackPointsOnExistingVertices,
			CancellationToken cancellationToken)
		{
			ApplyCrackPointsRequest request =
				CreateApplyCrackPointsRequest(selectedFeatures, crackPointsToAdd,
				                              chopperOptions,
				                              intersectionPointOptions,
				                              addCrackPointsOnExistingVertices,
				                              out List<Feature> updatedFeatures);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() *
			               request.SourceFeatures.Count;

			ChopLinesResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.ChopLines(request, o),
					cancellationToken, deadline);

			return GetChopLinesResult(response, updatedFeatures);
		}

		private static List<ResultFeature> GetChopLinesResult(
			ChopLinesResponse response,
			List<Feature> updatedFeatures)
		{
			// Unpack chop lines response

			var featuresByObjRef = new Dictionary<GdbObjectReference, Feature>();

			FeatureProcessingUtils.AddInputFeatures(updatedFeatures, featuresByObjRef);

			SpatialReference resultSpatialReference =
				updatedFeatures.FirstOrDefault()?.GetShape().SpatialReference;

			var resultFeatures = new List<ResultFeature>(
				FeatureDtoConversionUtils.FromUpdateMsgs(response.ResultFeatures, featuresByObjRef,
				                                         resultSpatialReference));

			return resultFeatures;
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedGeneralize
{
	public static class GeneralizeClientUtils
	{
		#region Calculate removable segments

		[CanBeNull]
		public static GeneralizeResult CalculateRemovableSegments(
			[NotNull] GeneralizeGrpc.GeneralizeGrpcClient generalizeClient,
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] IList<Feature> intersectingFeatures,
			bool protectVerticesWithinSameClassOnly,
			double? weedTolerance,
			bool weedNonLinearSegments,
			double? minimumSegmentLength,
			bool use2DLength,
			[CanBeNull] Geometry perimeter,
			CancellationToken cancellationToken)
		{
			CalculateRemovableSegmentsRequest request = CreateCalculateRemovableSegmentsRequest(
				selectedFeatures, intersectingFeatures, protectVerticesWithinSameClassOnly,
				weedTolerance, weedNonLinearSegments, minimumSegmentLength, use2DLength, perimeter);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * selectedFeatures.Count;

			CalculateRemovableSegmentsResponse response = GrpcClientUtils.Try(
				o => generalizeClient.CalculateRemovableSegments(request, o), cancellationToken,
				deadline);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			return UnpackResponse(response, selectedFeatures);
		}

		[NotNull]
		private static CalculateRemovableSegmentsRequest CreateCalculateRemovableSegmentsRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] IList<Feature> intersectingFeatures,
			bool protectVerticesWithinSameClassOnly,
			double? weedTolerance,
			bool weedNonLinearSegments,
			double? minimumSegmentLength,
			bool use2DLength,
			[CanBeNull] Geometry perimeter)
		{
			var request = new CalculateRemovableSegmentsRequest();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions);

			bool protectTopologicalVertices = intersectingFeatures != null;

			if (protectTopologicalVertices)
			{
				ProtobufConversionUtils.ToGdbObjectMsgList(intersectingFeatures,
				                                           request.TargetFeatures,
				                                           request.ClassDefinitions);
			}

			request.GeneralizeOptions =
				CreateGeneralizeOptionsMsg(protectTopologicalVertices,
				                           protectVerticesWithinSameClassOnly,
				                           weedTolerance, weedNonLinearSegments,
				                           minimumSegmentLength, use2DLength);

			request.Perimeter = ProtobufConversionUtils.ToShapeMsg(perimeter);

			return request;
		}

		private static GeneralizeResult UnpackResponse(CalculateRemovableSegmentsResponse response,
		                                               [NotNull] IList<Feature> selectedFeatures)
		{
			var result = new GeneralizeResult();

			IList<GeneralizedFeature> generalizedFeatures = new List<GeneralizedFeature>();

			ReAssociateResponsePoints(response, generalizedFeatures, selectedFeatures);

			result.ResultsByFeature = generalizedFeatures;

			return result;
		}

		private static void ReAssociateResponsePoints(
			CalculateRemovableSegmentsResponse response,
			IList<GeneralizedFeature> results,
			IList<Feature> updateFeatures)
		{
			foreach (var returnedFeature in response.RemovableSegments)
			{
				GdbObjRefMsg featureRef = returnedFeature.OriginalFeatureRef;

				Feature originalFeature = GetOriginalFeature(featureRef, updateFeatures);

				GeneralizedFeature resultPerFeature = new GeneralizedFeature(originalFeature);

				// It's important to assign the full spatial reference from the original to avoid
				// losing the VCS. Get it from the shape, because all calculations are in Map SR!
				Multipart originalShape = (Multipart) originalFeature.GetShape();

				SpatialReference sr = originalShape.SpatialReference;

				resultPerFeature.DeletablePoints =
					(Multipoint) ProtobufConversionUtils.FromShapeMsg(
						returnedFeature.PointsToDelete, sr);

				resultPerFeature.ProtectedPoints =
					(Multipoint) ProtobufConversionUtils.FromShapeMsg(
						returnedFeature.ProtectedPoints, sr);

				ICollection<Segment> segmentCollection = new List<Segment>();
				originalShape.GetAllSegments(ref segmentCollection);

				IList<Segment> segmentList = (IList<Segment>) segmentCollection;

				foreach (ShortSegmentMsg shortSegmentMsg in returnedFeature.ShortSegments)
				{
					Segment segment = segmentList[shortSegmentMsg.AbsoluteIndex];

					var segmentInfo = new SegmentInfo(segment, shortSegmentMsg.AbsoluteIndex,
					                                  shortSegmentMsg.PartIndex,
					                                  shortSegmentMsg.RelativeIndex);

					resultPerFeature.RemovableSegments.Add(segmentInfo);
				}

				results.Add(resultPerFeature);
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

		#endregion

		#region Remove segments

		public static IList<ResultFeature> ApplySegmentRemoval(
			GeneralizeGrpc.GeneralizeGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<GeneralizedFeature> segmentsToRemove,
			double? weedTolerance,
			bool weedNonLinearSegments,
			double? minimumSegmentLength,
			bool use2DLength,
			[CanBeNull] Geometry perimeter,
			CancellationToken cancellationToken)
		{
			var request = CreateApplySegmentRemovalRequest(selectedFeatures, segmentsToRemove,
			                                               weedTolerance, weedNonLinearSegments,
			                                               minimumSegmentLength, use2DLength,
			                                               perimeter);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() *
			               request.SourceFeatures.Count;

			ApplySegmentRemovalResponse response =
				GrpcClientUtils.Try(
					o => rpcClient.ApplySegmentRemoval(request, o), cancellationToken, deadline);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			return GetRemovedSegmentsResult(response, selectedFeatures);
		}

		private static ApplySegmentRemovalRequest CreateApplySegmentRemovalRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<GeneralizedFeature> segmentsToRemovePerFeature,
			double? weedTolerance,
			bool weedNonLinearSegments,
			double? minimumSegmentLength,
			bool use2DLength,
			[CanBeNull] Geometry perimeter)
		{
			var request = new ApplySegmentRemovalRequest();

			var selectedFeatureList = CollectionUtils.GetCollection(selectedFeatures);

			ProtobufConversionUtils.ToGdbObjectMsgList(
				selectedFeatureList, request.SourceFeatures, request.ClassDefinitions);

			bool protectTopologicalVertices =
				segmentsToRemovePerFeature.Any(f => f.ProtectedPoints != null);

			request.GeneralizeOptions = CreateGeneralizeOptionsMsg(
				protectTopologicalVertices, false, weedTolerance, weedNonLinearSegments,
				minimumSegmentLength, use2DLength);

			request.Perimeter = ProtobufConversionUtils.ToShapeMsg(perimeter);

			foreach (GeneralizedFeature generalizedFeature in segmentsToRemovePerFeature)
			{
				RemovableSegmentsMsg removableSegmentsMsg =
					ToRemovableSegmentsMsg(generalizedFeature);

				request.RemovableSegments.Add(removableSegmentsMsg);
			}

			return request;
		}

		private static RemovableSegmentsMsg ToRemovableSegmentsMsg(
			GeneralizedFeature generalizedFeature)
		{
			int classId = (int) generalizedFeature.GdbFeatureReference.ClassId;
			int objectId = (int) generalizedFeature.GdbFeatureReference.ObjectId;

			var removableSegmentsMsg = new RemovableSegmentsMsg();

			removableSegmentsMsg.OriginalFeatureRef =
				new GdbObjRefMsg()
				{
					ClassHandle = classId,
					ObjectId = objectId
				};

			removableSegmentsMsg.PointsToDelete = ProtobufConversionUtils.ToShapeMsg(
				generalizedFeature.DeletablePoints, true);

			removableSegmentsMsg.ProtectedPoints = ProtobufConversionUtils.ToShapeMsg(
				generalizedFeature.ProtectedPoints, true);

			foreach (SegmentInfo segmentInfo in generalizedFeature.RemovableSegments)
			{
				ShortSegmentMsg shortSegmentMsg = ToShortSegmentMsg(segmentInfo);

				removableSegmentsMsg.ShortSegments.Add(shortSegmentMsg);
			}

			return removableSegmentsMsg;
		}

		private static ShortSegmentMsg ToShortSegmentMsg(SegmentInfo segmentInfo)
		{
			// The actual segment is not needed, only the index
			ShortSegmentMsg shortSegmentMsg = new ShortSegmentMsg();
			{
				shortSegmentMsg.AbsoluteIndex = segmentInfo.GlobalIndex;
				shortSegmentMsg.PartIndex = segmentInfo.SegmentIndex.PartIndex;
				shortSegmentMsg.RelativeIndex = segmentInfo.SegmentIndex.LocalIndex;
			}
			return shortSegmentMsg;
		}

		[NotNull]
		private static List<ResultFeature> GetRemovedSegmentsResult(
			[NotNull] ApplySegmentRemovalResponse response,
			[NotNull] IList<Feature> featuresToUpdate)
		{
			// Unpack result features

			var featuresByObjRef = new Dictionary<GdbObjectReference, Feature>();

			FeatureProcessingUtils.AddInputFeatures(featuresToUpdate, featuresByObjRef);

			SpatialReference resultSpatialReference =
				featuresToUpdate.FirstOrDefault()?.GetShape().SpatialReference;

			var resultFeatures = new List<ResultFeature>(
				FeatureDtoConversionUtils.FromUpdateMsgs(response.ResultFeatures, featuresByObjRef,
				                                         resultSpatialReference));

			return resultFeatures;
		}

		#endregion

		private static GeneralizeOptionsMsg CreateGeneralizeOptionsMsg(
			bool protectTopologicalVertices,
			bool protectVerticesWithinSameClassOnly,
			double? weedTolerance,
			bool weedNonLinearSegments,
			double? minimumSegmentLength,
			bool use2DLength)
		{
			return new GeneralizeOptionsMsg
			       {
				       WeedTolerance = weedTolerance ?? -1,
				       WeedNonLinearSegments = weedNonLinearSegments,
				       MinimumSegmentLength = minimumSegmentLength ?? -1,
				       Use2DLength = use2DLength,
				       ProtectTopologicalVertices = protectTopologicalVertices,
				       ProtectOnlyWithinSameClass = protectVerticesWithinSameClassOnly
			       };
		}
	}
}

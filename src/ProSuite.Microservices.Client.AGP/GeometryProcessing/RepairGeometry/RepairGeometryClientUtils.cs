using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RepairGeometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.RepairGeometry;

public static class RepairGeometryClientUtils
{
	#region Calculate repair info

	[CanBeNull]
	public static RepairGeometryResult CalculateRepairInfo(
		[NotNull] RepairGeometryGrpc.RepairGeometryGrpcClient rpcClient,
		[NotNull] IList<Feature> sourceFeatures,
		double minimumSegmentLength,
		bool allowLoops,
		bool allowLinearSelfIntersections,
		bool addCrackPointsBetweenParts,
		double crackPointTolerance,
		bool use2D,
		CancellationToken cancellationToken)
	{
		CalculateRepairInfoRequest request = CreateCalculateRepairInfoRequest(
			sourceFeatures, minimumSegmentLength, allowLoops, allowLinearSelfIntersections,
			addCrackPointsBetweenParts, crackPointTolerance, use2D);

		const int extraFactor = 3;
		int deadline = FeatureProcessingUtils.GetProcessingTimeout(sourceFeatures.Count) *
		               extraFactor;

		CalculateRepairInfoResponse response = GrpcClientUtils.Try(
			o => rpcClient.CalculateRepairInfo(request, o), cancellationToken, deadline);

		if (response == null || cancellationToken.IsCancellationRequested)
		{
			return null;
		}

		return UnpackCalculateRepairInfoResponse(response, sourceFeatures);
	}

	private static CalculateRepairInfoRequest CreateCalculateRepairInfoRequest(
		[NotNull] IList<Feature> sourceFeatures,
		double minimumSegmentLength,
		bool allowLoops,
		bool allowLinearSelfIntersections,
		bool addCrackPointsBetweenParts,
		double crackPointTolerance,
		bool use2D)
	{
		var request = new CalculateRepairInfoRequest();

		ProtobufConversionUtils.ToGdbObjectMsgList(
			sourceFeatures, request.SourceFeatures, request.ClassDefinitions);

		request.RepairOptions = new RepairOptionsMsg
		                        {
			                        MinimumSegmentLength = minimumSegmentLength,
			                        AllowLoops = allowLoops,
			                        AllowLinearSelfIntersections = allowLinearSelfIntersections,
			                        AddCrackPointsBetweenParts = addCrackPointsBetweenParts,
			                        CrackPointTolerance = crackPointTolerance,
			                        Use2D = use2D
		                        };

		return request;
	}

	private static RepairGeometryResult UnpackCalculateRepairInfoResponse(
		[NotNull] CalculateRepairInfoResponse response,
		[NotNull] IList<Feature> sourceFeatures)
	{
		var result = new RepairGeometryResult();

		foreach (RepairInfoMsg repairInfoMsg in response.RepairInfos)
		{
			Feature originalFeature = GetOriginalFeature(
				repairInfoMsg.OriginalFeatureRef, sourceFeatures);

			var repairableFeature = new RepairableFeature(originalFeature);

			Multipart originalShape = (Multipart) originalFeature.GetShape();
			SpatialReference sr = originalShape.SpatialReference;

			repairableFeature.PointsToDelete =
				(Multipoint) ProtobufConversionUtils.FromShapeMsg(
					repairInfoMsg.PointsToDelete, sr);

			repairableFeature.CrackPointsToAdd =
				(Multipoint) ProtobufConversionUtils.FromShapeMsg(
					repairInfoMsg.CrackPointsToAdd, sr);

			ICollection<Segment> segmentCollection = new List<Segment>();
			originalShape.GetAllSegments(ref segmentCollection);
			IList<Segment> segmentList = (IList<Segment>) segmentCollection;

			foreach (InvalidSegmentMsg invalidSegmentMsg in repairInfoMsg.InvalidSegments)
			{
				if (invalidSegmentMsg.AbsoluteIndex < segmentList.Count)
				{
					Segment segment = segmentList[invalidSegmentMsg.AbsoluteIndex];
					var invalidSegment = new InvalidSegment(
						segment, invalidSegmentMsg.AbsoluteIndex,
						invalidSegmentMsg.PartIndex, invalidSegmentMsg.RelativeIndex);
					repairableFeature.InvalidSegments.Add(invalidSegment);
				}
			}

			result.ResultsByFeature.Add(repairableFeature);
		}

		foreach (string notification in response.Notifications)
		{
			result.NonStorableMessages.Add(notification);
		}

		return result;
	}

	#endregion

	#region Apply repair geometry

	[CanBeNull]
	public static IList<ResultFeature> ApplyRepairGeometry(
		[NotNull] RepairGeometryGrpc.RepairGeometryGrpcClient rpcClient,
		[NotNull] IList<Feature> sourceFeatures,
		[NotNull] IList<RepairableFeature> repairInfos,
		double minimumSegmentLength,
		bool allowLoops,
		bool allowLinearSelfIntersections,
		double crackPointTolerance,
		bool use2D,
		CancellationToken cancellationToken)
	{
		ApplyRepairGeometryRequest request = CreateApplyRepairGeometryRequest(
			sourceFeatures, repairInfos, minimumSegmentLength,
			allowLoops, allowLinearSelfIntersections, crackPointTolerance, use2D);

		int deadline = FeatureProcessingUtils.GetProcessingTimeout(sourceFeatures.Count);

		ApplyRepairGeometryResponse response = GrpcClientUtils.Try(
			o => rpcClient.ApplyRepairGeometry(request, o), cancellationToken, deadline);

		if (response == null || cancellationToken.IsCancellationRequested)
		{
			return null;
		}

		return GetApplyRepairGeometryResult(response, sourceFeatures);
	}

	private static ApplyRepairGeometryRequest CreateApplyRepairGeometryRequest(
		[NotNull] IList<Feature> sourceFeatures,
		[NotNull] IList<RepairableFeature> repairInfos,
		double minimumSegmentLength,
		bool allowLoops,
		bool allowLinearSelfIntersections,
		double crackPointTolerance,
		bool use2D)
	{
		var request = new ApplyRepairGeometryRequest();

		ProtobufConversionUtils.ToGdbObjectMsgList(
			sourceFeatures, request.SourceFeatures, request.ClassDefinitions);

		request.RepairOptions = new RepairOptionsMsg
		                        {
			                        MinimumSegmentLength = minimumSegmentLength,
			                        AllowLoops = allowLoops,
			                        AllowLinearSelfIntersections = allowLinearSelfIntersections,
			                        CrackPointTolerance = crackPointTolerance,
			                        Use2D = use2D
		                        };

		foreach (RepairableFeature repairableFeature in repairInfos)
		{
			request.RepairInfos.Add(ToRepairInfoMsg(repairableFeature));
		}

		return request;
	}

	private static RepairInfoMsg ToRepairInfoMsg([NotNull] RepairableFeature repairableFeature)
	{
		int classId = (int) repairableFeature.GdbFeatureReference.ClassId;
		int objectId = (int) repairableFeature.GdbFeatureReference.ObjectId;

		var msg = new RepairInfoMsg();

		msg.OriginalFeatureRef = new GdbObjRefMsg
		                         {
			                         ClassHandle = classId,
			                         ObjectId = objectId
		                         };

		msg.PointsToDelete = ProtobufConversionUtils.ToShapeMsg(
			repairableFeature.PointsToDelete, true);

		msg.CrackPointsToAdd = ProtobufConversionUtils.ToShapeMsg(
			repairableFeature.CrackPointsToAdd, true);

		foreach (InvalidSegment invalidSegment in repairableFeature.InvalidSegments)
		{
			msg.InvalidSegments.Add(ToInvalidSegmentMsg(invalidSegment));
		}

		return msg;
	}

	private static InvalidSegmentMsg ToInvalidSegmentMsg(
		[NotNull] InvalidSegment invalidSegment)
	{
		return new InvalidSegmentMsg
		       {
			       AbsoluteIndex = invalidSegment.AbsoluteIndex,
			       PartIndex = invalidSegment.PartIndex,
			       RelativeIndex = invalidSegment.RelativeIndex
		       };
	}

	[NotNull]
	private static IList<ResultFeature> GetApplyRepairGeometryResult(
		[NotNull] ApplyRepairGeometryResponse response,
		[NotNull] IList<Feature> sourceFeatures)
	{
		var featuresByObjRef = new Dictionary<GdbObjectReference, Feature>();
		FeatureProcessingUtils.AddInputFeatures(sourceFeatures, featuresByObjRef);

		SpatialReference resultSpatialReference =
			sourceFeatures.FirstOrDefault()?.GetShape().SpatialReference;

		return new List<ResultFeature>(
			FeatureDtoConversionUtils.FromUpdateMsgs(
				response.ResultFeatures, featuresByObjRef, resultSpatialReference));
	}

	#endregion

	private static Feature GetOriginalFeature(
		[NotNull] GdbObjRefMsg featureRef,
		[NotNull] IList<Feature> sourceFeatures)
	{
		long classId = featureRef.ClassHandle;
		long objectId = featureRef.ObjectId;

		return sourceFeatures.First(f => f.GetObjectID() == objectId &&
		                                 GeometryProcessingUtils.GetUniqueClassId(f) ==
		                                 classId);
	}
}

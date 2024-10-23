using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong
{
	public static class ChangeAlongClientUtils
	{
		#region Calculate subcurves

		[NotNull]
		public static ChangeAlongCurves CalculateReshapeLines(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			var response =
				CalculateReshapeCurvesRpc(rpcClient, sourceFeatures, targetFeatures,
				                          cancellationToken);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return new ChangeAlongCurves(new List<CutSubcurve>(0),
				                             ReshapeAlongCurveUsability.Undefined);
			}

			var result = PopulateReshapeAlongCurves(
				targetFeatures, response.ReshapeLines,
				(ReshapeAlongCurveUsability) response.ReshapeLinesUsability);

			return result;
		}

		[NotNull]
		public static ChangeAlongCurves CalculateCutLines(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			var response =
				CalculateCutCurvesRpc(rpcClient, sourceFeatures, targetFeatures,
				                      cancellationToken);

			if (response == null || cancellationToken.IsCancellationRequested)
			{
				return new ChangeAlongCurves(new List<CutSubcurve>(0),
				                             ReshapeAlongCurveUsability.Undefined);
			}

			var result = PopulateReshapeAlongCurves(
				targetFeatures, response.CutLines,
				(ReshapeAlongCurveUsability) response.ReshapeLinesUsability);

			return result;
		}

		private static CalculateReshapeLinesResponse CalculateReshapeCurvesRpc(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			var request = CreateCalculateReshapeLinesRequest(selectedFeatures, targetFeatures);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * selectedFeatures.Count;

			return GrpcClientUtils.Try(
				options => rpcClient.CalculateReshapeLines(request, options),
				cancellationToken, deadline);
		}

		private static CalculateCutLinesResponse CalculateCutCurvesRpc(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			var request = CreateCalculateCutLinesRequest(selectedFeatures, targetFeatures);

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * selectedFeatures.Count;

			return GrpcClientUtils.Try(
				options => rpcClient.CalculateCutLines(request, options),
				cancellationToken, deadline);
		}

		private static CalculateReshapeLinesRequest CreateCalculateReshapeLinesRequest(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures)
		{
			var request = new CalculateReshapeLinesRequest();

			PopulateCalculationRequestLists(selectedFeatures, targetFeatures,
			                                request.SourceFeatures, request.TargetFeatures,
			                                request.ClassDefinitions);

			request.Tolerance = selectedFeatures.FirstOrDefault()?.GetShape().SpatialReference
			                                    .XYTolerance ?? 0;

			// TODO: The other options

			return request;
		}

		private static CalculateCutLinesRequest CreateCalculateCutLinesRequest(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures)
		{
			var request = new CalculateCutLinesRequest();

			PopulateCalculationRequestLists(selectedFeatures, targetFeatures,
			                                request.SourceFeatures, request.TargetFeatures,
			                                request.ClassDefinitions);

			// TODO: The other options

			return request;
		}

		private static void PopulateCalculationRequestLists(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures,
			ICollection<GdbObjectMsg> sourceFeatureMsgs,
			ICollection<GdbObjectMsg> targetFeatureMsgs,
			ICollection<ObjectClassMsg> classDefinitions)
		{
			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           sourceFeatureMsgs, classDefinitions);

			ProtobufConversionUtils.ToGdbObjectMsgList(targetFeatures,
			                                           targetFeatureMsgs, classDefinitions);
		}

		#endregion

		[NotNull]
		public static List<ResultFeature> ApplyReshapeCurves(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			[NotNull] IList<CutSubcurve> selectedSubcurves,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			Dictionary<GdbObjectReference, Feature> featuresByObjRef =
				CreateFeatureDictionary(sourceFeatures, targetFeatures);

			ApplyReshapeLinesRequest request =
				CreateApplyReshapeCurvesRequest(sourceFeatures, targetFeatures, selectedSubcurves);

			ApplyReshapeLinesResponse response =
				rpcClient.ApplyReshapeLines(request, null, null, cancellationToken);

			List<ResultObjectMsg> responseResultFeatures = response.ResultFeatures.ToList();

			SpatialReference resultSpatialReference =
				sourceFeatures.FirstOrDefault()?.GetShape().SpatialReference;

			var resultFeatures = new List<ResultFeature>(
				FeatureDtoConversionUtils.FromUpdateMsgs(responseResultFeatures, featuresByObjRef,
				                                         resultSpatialReference));

			newChangeAlongCurves = PopulateReshapeAlongCurves(
				targetFeatures, response.NewReshapeLines,
				(ReshapeAlongCurveUsability) response.ReshapeLinesUsability);

			return resultFeatures;
		}

		[NotNull]
		public static List<ResultFeature> ApplyCutCurves(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			[NotNull] IList<CutSubcurve> selectedSubcurves,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			Dictionary<GdbObjectReference, Feature> featuresByObjRef =
				CreateFeatureDictionary(sourceFeatures, targetFeatures);

			ApplyCutLinesRequest request =
				CreateApplyCutCurvesRequest(sourceFeatures, targetFeatures, selectedSubcurves);

			ApplyCutLinesResponse response =
				rpcClient.ApplyCutLines(request, null, null, cancellationToken);

			List<ResultObjectMsg> responseResultFeatures = response.ResultFeatures.ToList();

			SpatialReference resultSpatialReference =
				sourceFeatures.FirstOrDefault()?.GetShape().SpatialReference;

			var resultFeatures = new List<ResultFeature>(
				FeatureDtoConversionUtils.FromUpdateMsgs(responseResultFeatures, featuresByObjRef,
				                                         resultSpatialReference));

			newChangeAlongCurves = PopulateReshapeAlongCurves(
				targetFeatures, response.NewCutLines,
				(ReshapeAlongCurveUsability) response.CutLinesUsability);

			return resultFeatures;
		}

		private static Dictionary<GdbObjectReference, Feature> CreateFeatureDictionary(
			IList<Feature> sourceFeatures, IList<Feature> targetFeatures)
		{
			var featuresByObjRef = new Dictionary<GdbObjectReference, Feature>();

			foreach (Feature sourceFeature in sourceFeatures)
			{
				featuresByObjRef.Add(
					ProtobufConversionUtils.ToObjectReferenceWithUniqueClassId(sourceFeature),
					sourceFeature);
			}

			foreach (Feature targetFeature in targetFeatures)
			{
				featuresByObjRef.Add(
					ProtobufConversionUtils.ToObjectReferenceWithUniqueClassId(targetFeature),
					targetFeature);
			}

			return featuresByObjRef;
		}

		private static ApplyReshapeLinesRequest CreateApplyReshapeCurvesRequest(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures,
			IList<CutSubcurve> selectedSubcurves)
		{
			var result =
				new ApplyReshapeLinesRequest
				{
					CalculationRequest =
						CreateCalculateReshapeLinesRequest(selectedFeatures, targetFeatures)
				};

			foreach (CutSubcurve subcurve in selectedSubcurves)
			{
				result.ReshapeLines.Add(ToReshapeLineMsg(subcurve));
			}

			// TODO: Options
			result.InsertVerticesInTarget = true;

			return result;
		}

		private static ApplyCutLinesRequest CreateApplyCutCurvesRequest(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures,
			IList<CutSubcurve> selectedSubcurves)
		{
			var result =
				new ApplyCutLinesRequest
				{
					CalculationRequest =
						CreateCalculateCutLinesRequest(selectedFeatures, targetFeatures)
				};

			foreach (CutSubcurve subcurve in selectedSubcurves)
			{
				result.CutLines.Add(ToReshapeLineMsg(subcurve));
			}

			// TODO: Options
			result.InsertVerticesInTarget = true;

			return result;
		}

		#region Protobuf conversions

		private static ChangeAlongCurves PopulateReshapeAlongCurves(
			[NotNull] IList<Feature> targetFeatures,
			IEnumerable<ReshapeLineMsg> reshapeLineMsgs,
			ReshapeAlongCurveUsability cutSubcurveUsability)
		{
			IList<CutSubcurve> resultSubcurves = new List<CutSubcurve>();

			SpatialReference sr = targetFeatures.Select(f => f.GetShape().SpatialReference)
			                                    .FirstOrDefault();

			foreach (var reshapeLineMsg in reshapeLineMsgs)
			{
				CutSubcurve cutSubcurve = FromReshapeLineMsg(reshapeLineMsg, sr);

				Assert.NotNull(cutSubcurve);

				GdbObjRefMsg sourceObjRefMsg = reshapeLineMsg.Source;

				if (sourceObjRefMsg != null)
				{
					cutSubcurve.Source =
						new GdbObjectReference(sourceObjRefMsg.ClassHandle,
						                       sourceObjRefMsg.ObjectId);
				}

				resultSubcurves.Add(cutSubcurve);
			}

			return new ChangeAlongCurves(resultSubcurves, cutSubcurveUsability)
			       {
				       TargetFeatures = targetFeatures
			       };
		}

		private static CutSubcurve FromReshapeLineMsg(ReshapeLineMsg reshapeLineMsg,
		                                              SpatialReference spatialReference)
		{
			var path =
				(Polyline) ProtobufConversionUtils.FromShapeMsg(
					reshapeLineMsg.Path, spatialReference);

			var targetSegmentAtFrom =
				(Polyline) ProtobufConversionUtils.FromShapeMsg(
					reshapeLineMsg.TargetSegmentAtFrom, spatialReference);
			var targetSegmentAtTo =
				(Polyline) ProtobufConversionUtils.FromShapeMsg(
					reshapeLineMsg.TargetSegmentAtTo, spatialReference);

			//var extraInsertPoints =
			//	ProtobufConversionUtils.FromShapeMsg(reshapeLineMsg.ExtraTargetInsertPoints,spatialReference);

			IList<MapPoint> extraInsertPoints =
				PointsFromShapeMsg(reshapeLineMsg.ExtraTargetInsertPoints);

			var result = new CutSubcurve(Assert.NotNull(path),
			                             reshapeLineMsg.CanReshape, reshapeLineMsg.IsCandidate,
			                             reshapeLineMsg.IsFiltered,
			                             targetSegmentAtFrom, targetSegmentAtTo, extraInsertPoints);

			return result;
		}

		[CanBeNull]
		private static IList<MapPoint> PointsFromShapeMsg([CanBeNull] ShapeMsg shapeMsg)
		{
			var geometry = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			if (geometry == null)
			{
				return null;
			}

			return GetPoints(geometry).ToList();
		}

		private static IEnumerable<MapPoint> GetPoints(Geometry geometry)
		{
			//if (geometry == null) yield break;

			int geometryPointCount = geometry.PointCount;

			ReadOnlyPointCollection points = null;

			if (geometry is Multipoint multipoint)
			{
				points = multipoint.Points;
			}
			else if (geometry is Polyline polyline)
			{
				points = polyline.Points;
			}
			else if (geometry is Polygon polygon)
			{
				points = polygon.Points;
			}
			else if (geometry is Envelope envelope)
			{
				Polygon polygon1 = GeometryFactory.CreatePolygon(envelope);
				points = polygon1.Points;
			}
			else if (geometry is Multipart multipart)
			{
				points = multipart.Points;
			}

			if (points != null)
			{
				foreach (MapPoint point in points)
				{
					yield return point;
				}
			}

			if (geometry is MapPoint mapPoint)
			{
				yield return mapPoint;
			}
		}

		private static ReshapeLineMsg ToReshapeLineMsg([NotNull] CutSubcurve subcurve)
		{
			var result = new ReshapeLineMsg();

			result.Path = ProtobufConversionUtils.ToShapeMsg(subcurve.Path);
			result.CanReshape = subcurve.CanReshape;
			result.IsCandidate = subcurve.IsReshapeMemberCandidate;
			result.IsFiltered = subcurve.IsFiltered;

			if (subcurve.Source != null)
			{
				result.Source = new GdbObjRefMsg
				                {
					                ClassHandle = subcurve.Source.Value.ClassId,
					                ObjectId = subcurve.Source.Value.ObjectId
				                };
			}

			result.TargetSegmentAtFrom =
				ProtobufConversionUtils.ToShapeMsg(subcurve.TargetSegmentAtFromPoint);
			result.TargetSegmentAtTo =
				ProtobufConversionUtils.ToShapeMsg(subcurve.TargetSegmentAtToPoint);

			result.ExtraTargetInsertPoints =
				ProtobufConversionUtils.ToShapeMsg(MultipointBuilderEx.CreateMultipoint(subcurve.ExtraTargetInsertPoints));

			return result;
		}

		#endregion
	}
}

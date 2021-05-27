using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong
{
	public static class ChangeAlongClientUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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
				response.ReshapeLines, (ReshapeAlongCurveUsability) response.ReshapeLinesUsability);

			result.TargetFeatures = targetFeatures;

			return result;
		}

		private static CalculateReshapeLinesResponse CalculateReshapeCurvesRpc(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			var request = CreateCalculateReshapeLinesRequest(selectedFeatures, targetFeatures);

			CalculateReshapeLinesResponse response;

			try
			{
				response =
					rpcClient.CalculateReshapeLines(request, null, null,
					                                cancellationToken);
			}
			catch (Exception e)
			{
				_msg.Debug($"Error calling remote procedure: {e.Message} ", e);

				//Exception detailedException = GetError(e, calculateOverlapsCall);

				throw e;
			}

			return response;
		}

		private static CalculateReshapeLinesRequest CreateCalculateReshapeLinesRequest(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures)
		{
			var request = new CalculateReshapeLinesRequest();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.SourceFeatures,
			                                           request.ClassDefinitions);

			ProtobufConversionUtils.ToGdbObjectMsgList(targetFeatures,
			                                           request.TargetFeatures,
			                                           request.ClassDefinitions);

			// TODO: The other options

			return request;
		}

		private static ChangeAlongCurves PopulateReshapeAlongCurves(
			IEnumerable<ReshapeLineMsg> reshapeLineMsgs,
			ReshapeAlongCurveUsability cutSubcurveUsability)
		{
			//PocoDtoMap.Clear();

			IList<CutSubcurve> resultSubcurves = new List<CutSubcurve>();
			foreach (var reshapeLineMsg in reshapeLineMsgs)
			{
				CutSubcurve cutSubcurve = FromReshapeLineMsg(reshapeLineMsg);

				Assert.NotNull(cutSubcurve);
				resultSubcurves.Add(cutSubcurve);

				//PocoDtoMap.Add(cutSubcurve, reshapeLineMsg);
			}

			return new ChangeAlongCurves(resultSubcurves, cutSubcurveUsability);
		}

		[NotNull]
		public static List<ReshapeResultFeature> ApplyReshapeCurves(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			[NotNull] IList<CutSubcurve> selectedSubcurves,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			var featuresByObjRef = new Dictionary<GdbObjectReference, Feature>();

			foreach (Feature sourceFeature in sourceFeatures)
			{
				featuresByObjRef.Add(new GdbObjectReference(sourceFeature), sourceFeature);
			}

			foreach (Feature targetFeature in targetFeatures)
			{
				featuresByObjRef.Add(new GdbObjectReference(targetFeature), targetFeature);
			}

			ApplyReshapeLinesRequest request =
				CreateApplyReshapeCurvesRequest(sourceFeatures, targetFeatures, selectedSubcurves);

			ApplyReshapeLinesResponse response;

			//try
			{
				response = rpcClient.ApplyReshapeLines(request, null, null, cancellationToken);
			}
			//catch (Exception e)
			//{
			//	_msg.Debug($"Error calling remote procedure: {e.Message} ", e);

			//	//Exception detailedException = GetError(e, calculateOverlapsCall);

			//	throw e;
			//}

			List<ResultObjectMsg> responseResultFeatures = response.ResultFeatures.ToList();

			var resultFeatures = new List<ReshapeResultFeature>();

			foreach (ResultObjectMsg resultObjectMsg in responseResultFeatures)
			{
				GdbObjectMsg updateMsg = Assert.NotNull(resultObjectMsg.Update);

				var updateObjRef =
					new GdbObjectReference(updateMsg.ClassHandle, updateMsg.ObjectId);

				Feature originalFeature = featuresByObjRef[updateObjRef];

				resultFeatures.Add(new ReshapeResultFeature(originalFeature, resultObjectMsg));
			}

			//foreach (var selectedFeature in sourceFeatures)
			//{
			//	var resultFeature = CreateResultFeature(selectedFeature, responseResultFeatures);

			//	if (resultFeature != null)
			//		resultFeatures.Add(resultFeature);
			//}

			//foreach (var targetFeature in targetFeatures)
			//{
			//	var resultFeature = CreateResultFeature(targetFeature, responseResultFeatures);

			//	if (resultFeature != null)
			//		resultFeatures.Add(resultFeature);
			//}

			newChangeAlongCurves = PopulateReshapeAlongCurves(
				response.NewReshapeLines,
				(ReshapeAlongCurveUsability) response.ReshapeLinesUsability);

			return resultFeatures;

			//var result =
			//	ApplyReshapeCurvesRpc(sourceFeatures, targetFeatures, selectedSubcurves,
			//	                      cancellationToken, out newChangeAlongCurves);

			//if (cancellationToken.IsCancellationRequested)
			//	newChangeAlongCurves = new ChangeAlongCurves(new List<CutSubcurve>(0),
			//	                                             ReshapeAlongCurveUsability.Undefined);

			//return result;
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

		//private static ReshapeResultFeature CreateResultFeature(Feature clientFeature,
		//                                                        List<ResultObjectMsg> responseResultFeatures)
		//{
		//	var reshapeResult = responseResultFeatures.FirstOrDefault(
		//		f => clientFeature.GetTable().GetID() == f.UpdatedFeature.Class.ClassId &&
		//		     clientFeature.GetObjectID() == f.UpdatedFeature.ObjectId);

		//	if (reshapeResult == null) return null;

		//	var newGeometry = ProtoBufUtils.FromShapeProtoBuffer(reshapeResult.UpdatedFeature.Shape);

		//	var resultFeature = new ReshapeResultFeature(clientFeature, newGeometry, reshapeResult.Notifications);
		//	return resultFeature;
		//}

		#region Protobuf conversions

		public static CutSubcurve FromReshapeLineMsg(ReshapeLineMsg reshapeLineMsg)
		{
			var path = (Polyline) ProtobufConversionUtils.FromShapeMsg(reshapeLineMsg.Path);

			var targetSegmentAtFrom =
				(Polyline) ProtobufConversionUtils.FromShapeMsg(reshapeLineMsg.TargetSegmentAtFrom);
			var targetSegmentAtTo =
				(Polyline) ProtobufConversionUtils.FromShapeMsg(reshapeLineMsg.TargetSegmentAtTo);

			var extraInsertPoints =
				ProtobufConversionUtils.FromShapeMsg(reshapeLineMsg.ExtraTargetInsertPoints);

			var result = new CutSubcurve(Assert.NotNull(path),
			                             reshapeLineMsg.CanReshape, reshapeLineMsg.IsCandidate,
			                             reshapeLineMsg.IsFiltered,
			                             targetSegmentAtFrom, targetSegmentAtTo, extraInsertPoints);

			return result;
		}

		private static ReshapeLineMsg ToReshapeLineMsg(CutSubcurve subcurve)
		{
			var result = new ReshapeLineMsg();

			result.Path = ProtobufConversionUtils.ToShapeMsg(subcurve.Path);
			result.CanReshape = subcurve.CanReshape;
			result.IsCandidate = subcurve.IsReshapeMemberCandidate;
			result.IsFiltered = subcurve.IsFiltered;

			if (subcurve.Source != null)
			{
				result.Source = ProtobufConversionUtils.ToGdbObjRefMsg(subcurve.Source);
			}

			result.TargetSegmentAtFrom =
				ProtobufConversionUtils.ToShapeMsg(subcurve.TargetSegmentAtFromPoint);
			result.TargetSegmentAtTo =
				ProtobufConversionUtils.ToShapeMsg(subcurve.TargetSegmentAtToPoint);

			result.ExtraTargetInsertPoints =
				ProtobufConversionUtils.ToShapeMsg(subcurve.ExtraTargetInsertPoints);

			return result;
		}

		private static Segment SegmentFromShapeProtoBuffer(ShapeMsg polylineMsg)
		{
			var targetPolylineAtFrom = (Polyline) ProtobufConversionUtils.FromShapeMsg(polylineMsg);

			var targetSegmentAtFrom =
				targetPolylineAtFrom?.Parts.FirstOrDefault()?.FirstOrDefault();
			return targetSegmentAtFrom;
		}

		#endregion
	}
}

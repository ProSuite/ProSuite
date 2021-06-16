using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong
{
	public static class ChangeAlongClientUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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
				sourceFeatures, targetFeatures, response.ReshapeLines,
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
				sourceFeatures, targetFeatures, response.CutLines,
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

			return TryRpc(
				request,
				r => rpcClient.CalculateReshapeLines(r, null, null, cancellationToken));
		}

		private static CalculateCutLinesResponse CalculateCutCurvesRpc(
			[NotNull] ChangeAlongGrpc.ChangeAlongGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			var request = CreateCalculateCutLinesRequest(selectedFeatures, targetFeatures);

			return TryRpc(
				request,
				r => rpcClient.CalculateCutLines(r, null, null, cancellationToken));
		}

		private static CalculateReshapeLinesRequest CreateCalculateReshapeLinesRequest(
			IList<Feature> selectedFeatures,
			IList<Feature> targetFeatures)
		{
			var request = new CalculateReshapeLinesRequest();

			PopulateCalculationRequestLists(selectedFeatures, targetFeatures,
			                                request.SourceFeatures, request.TargetFeatures,
			                                request.ClassDefinitions);

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
			                                           sourceFeatureMsgs,
			                                           classDefinitions);

			ProtobufConversionUtils.ToGdbObjectMsgList(targetFeatures,
			                                           targetFeatureMsgs,
			                                           classDefinitions);
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

			var resultFeatures = new List<ResultFeature>();

			foreach (ResultObjectMsg resultObjectMsg in responseResultFeatures)
			{
				GdbObjectMsg updateMsg = Assert.NotNull(resultObjectMsg.Update);

				var updateObjRef =
					new GdbObjectReference(updateMsg.ClassHandle, updateMsg.ObjectId);

				Feature originalFeature = featuresByObjRef[updateObjRef];

				resultFeatures.Add(new ResultFeature(originalFeature, resultObjectMsg));
			}

			newChangeAlongCurves = PopulateReshapeAlongCurves(
				sourceFeatures, targetFeatures, response.NewReshapeLines,
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

			var resultFeatures = new List<ResultFeature>();

			foreach (ResultObjectMsg resultObjectMsg in responseResultFeatures)
			{
				GdbObjectReference originalFeatureRef =
					GetOriginalGdbObjectReference(resultObjectMsg);

				Feature originalFeature = featuresByObjRef[originalFeatureRef];

				ResultFeature resultFeature = new ResultFeature(
					originalFeature, resultObjectMsg);

				resultFeatures.Add(resultFeature);
			}

			newChangeAlongCurves = PopulateReshapeAlongCurves(
				sourceFeatures, targetFeatures, response.NewCutLines,
				(ReshapeAlongCurveUsability) response.CutLinesUsability);

			return resultFeatures;
		}

		private static GdbObjectReference GetOriginalGdbObjectReference(
			[NotNull] ResultObjectMsg resultObjectMsg)
		{
			Assert.ArgumentNotNull(nameof(resultObjectMsg));

			// TODO: long int!
			int classHandle, objectId;

			if (resultObjectMsg.FeatureCase == ResultObjectMsg.FeatureOneofCase.Insert)
			{
				InsertedObjectMsg insert = Assert.NotNull(resultObjectMsg.Insert);

				GdbObjRefMsg originalObjRefMsg = insert.OriginalReference;

				classHandle = originalObjRefMsg.ClassHandle;
				objectId = originalObjRefMsg.ObjectId;
			}
			else
			{
				GdbObjectMsg updateMsg = Assert.NotNull(resultObjectMsg.Update);

				classHandle = updateMsg.ClassHandle;
				objectId = updateMsg.ObjectId;
			}

			return new GdbObjectReference(classHandle, objectId);
		}

		private static Dictionary<GdbObjectReference, Feature> CreateFeatureDictionary(
			IList<Feature> sourceFeatures, IList<Feature> targetFeatures)
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
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			IEnumerable<ReshapeLineMsg> reshapeLineMsgs,
			ReshapeAlongCurveUsability cutSubcurveUsability)
		{
			IList<CutSubcurve> resultSubcurves = new List<CutSubcurve>();
			foreach (var reshapeLineMsg in reshapeLineMsgs)
			{
				CutSubcurve cutSubcurve = FromReshapeLineMsg(reshapeLineMsg);

				Assert.NotNull(cutSubcurve);

				if (reshapeLineMsg.Source != null)
				{
					var sourceRef = new GdbObjectReference(reshapeLineMsg.Source.ClassHandle,
					                                       reshapeLineMsg.Source.ObjectId);

					cutSubcurve.Source = sourceFeatures.First(f => sourceRef.References(f));
				}

				resultSubcurves.Add(cutSubcurve);
			}

			return new ChangeAlongCurves(resultSubcurves, cutSubcurveUsability)
			       {
				       TargetFeatures = targetFeatures
			       };
		}

		private static CutSubcurve FromReshapeLineMsg(ReshapeLineMsg reshapeLineMsg)
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

		#endregion

		private static TResponse TryRpc<TResponse, TRequest>(
			[NotNull] TRequest request,
			Func<TRequest, TResponse> func)
		{
			TResponse response;

			try
			{
				response = func(request);
			}
			catch (Exception e)
			{
				_msg.Debug($"Error calling remote procedure: {e.Message} ", e);

				throw;
			}

			return response;
		}
	}
}

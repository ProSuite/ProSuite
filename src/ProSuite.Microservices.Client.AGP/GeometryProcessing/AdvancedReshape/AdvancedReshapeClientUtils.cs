using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape
{
	public static class AdvancedReshapeClientUtils
	{
		public static async Task<MapPoint> GetOpenJawReplacementPointAsync(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] Feature polylineFeature,
			[NotNull] Polyline reshapeLine,
			bool useNonDefaultReshapeSide)
		{
			var request = new OpenJawReshapeLineReplacementRequest()
			              {
				              UseNonDefaultReshapeSide = useNonDefaultReshapeSide
			              };

			SpatialReference sr =
				await QueuedTaskUtils.Run(
					() =>
					{
						Geometry geometryToReshape = polylineFeature.GetShape();

						request.Feature = ProtobufConversionUtils.ToGdbObjectMsg(
							polylineFeature, geometryToReshape, true);

						request.ReshapePath = ProtobufConversionUtils.ToShapeMsg(reshapeLine);

						return geometryToReshape.SpatialReference;
					});

			// Not in a queued task!
			ShapeMsg pointMsg = rpcClient.GetOpenJawReshapeLineReplaceEndPoint(request);

			return await QueuedTaskUtils.Run(
				       () => (MapPoint) ProtobufConversionUtils.FromShapeMsg(pointMsg, sr));
		}

		public static async Task<ReshapeResult> ReshapeAsync(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault)
		{
			var allInputFeatures = new Dictionary<GdbObjectReference, Feature>();

			AdvancedReshapeRequest request =
				await QueuedTaskUtils.Run(
					() =>
					{
						AddInputFeatures(selectedFeatures, allInputFeatures);

						return CreateReshapeRequest(
							selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
							multiReshapeAsUnion, tryReshapeNonDefault);
					});

			request.AllowOpenJawReshape = true;

			// TODO:
			// Inside a QueuedTask this will be executed up to 4 times! -> Use background task (> 2.6)
			AdvancedReshapeResponse reshapeResultMsg = rpcClient.AdvancedReshape(request);

			var result = new ReshapeResult
			             {
				             OpenJawReshapeHappened = reshapeResultMsg.OpenJawReshapeHappened,
				             OpenJawIntersectionCount = reshapeResultMsg.OpenJawIntersectionCount,
				             FailureMessage = reshapeResultMsg.WarningMessage
			             };

			if (reshapeResultMsg.ResultFeatures.Count == 0)
			{
				return result;
			}

			foreach (ResultFeatureMsg resultFeatureMsg in reshapeResultMsg.ResultFeatures)
			{
				GdbObjectReference objRef = new GdbObjectReference(
					resultFeatureMsg.UpdatedFeature.ClassHandle,
					resultFeatureMsg.UpdatedFeature.ObjectId);

				Feature inputFeature = allInputFeatures[objRef];

				var reshapeResultFeature = new ReshapeResultFeature(inputFeature, resultFeatureMsg);

				result.ResultFeatures.Add(reshapeResultFeature);
			}

			return result;
		}

		public static ReshapeResult Reshape(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault)
		{
			var allInputFeatures = new Dictionary<GdbObjectReference, Feature>();

			AddInputFeatures(selectedFeatures, allInputFeatures);

			if (adjacentFeatures != null)
				AddInputFeatures(adjacentFeatures, allInputFeatures);

			AdvancedReshapeRequest request = CreateReshapeRequest(
				selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault);

			return Reshape(rpcClient, request, allInputFeatures);
		}

		private static ReshapeResult Reshape(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] AdvancedReshapeRequest request,
			[NotNull] IReadOnlyDictionary<GdbObjectReference, Feature> allInputFeatures)
		{
			request.AllowOpenJawReshape = true;

			// TODO:
			// Inside a QueuedTask this will be executed up to 4 times! -> Use background task (> 2.6)
			// Or alternatively, use a single TaskCompletionSource and take it from there. 
			AdvancedReshapeResponse reshapeResultMsg = rpcClient.AdvancedReshape(request);

			var result = new ReshapeResult
			             {
				             OpenJawReshapeHappened = reshapeResultMsg.OpenJawReshapeHappened,
				             OpenJawIntersectionCount = reshapeResultMsg.OpenJawIntersectionCount,
				             FailureMessage = reshapeResultMsg.WarningMessage
			             };

			if (reshapeResultMsg.ResultFeatures.Count == 0)
			{
				return result;
			}

			foreach (ResultFeatureMsg resultFeatureMsg in reshapeResultMsg.ResultFeatures)
			{
				GdbObjectReference objRef = new GdbObjectReference(
					resultFeatureMsg.UpdatedFeature.ClassHandle,
					resultFeatureMsg.UpdatedFeature.ObjectId);

				Feature inputFeature = allInputFeatures[objRef];

				var reshapeResultFeature = new ReshapeResultFeature(inputFeature, resultFeatureMsg);

				result.ResultFeatures.Add(reshapeResultFeature);
			}

			return result;
		}

		private static void AddInputFeatures(
			[NotNull] IList<Feature> features,
			[NotNull] Dictionary<GdbObjectReference, Feature> toDictionary)
		{
			foreach (Feature selectedFeature in features)
			{
				toDictionary.Add(new GdbObjectReference(selectedFeature),
				                 selectedFeature);
			}
		}

		private static AdvancedReshapeRequest CreateReshapeRequest(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapePath,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJaw, bool multiReshapeAsUnion, bool useNonDefaultSide)
		{
			var request = new AdvancedReshapeRequest();

			ProtobufConversionUtils.ToGdbObjectMsgList(selectedFeatures,
			                                           request.Features,
			                                           request.ClassDefinitions);

			ShapeMsg reshapePathMsg = ProtobufConversionUtils.ToShapeMsg(reshapePath);

			request.ReshapePaths = reshapePathMsg;
			request.AllowOpenJawReshape = allowOpenJaw;
			request.UseNonDefaultReshapeSide = useNonDefaultSide;
			request.MultipleSourcesTryUnion = multiReshapeAsUnion;

			// TODO: from options
			request.MoveOpenJawEndJunction = true;

			if (adjacentFeatures != null)
			{
				ProtobufConversionUtils.ToGdbObjectMsgList(
					adjacentFeatures, request.PotentiallyConnectedFeatures,
					request.ClassDefinitions);
			}

			return request;
		}
	}
}

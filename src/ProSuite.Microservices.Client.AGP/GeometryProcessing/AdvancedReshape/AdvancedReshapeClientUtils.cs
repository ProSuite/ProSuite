using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

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
			var request = new OpenJawReshapeLineReplacementRequest
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

			// Extra-short deadline because it's only for the preview
			const int deadlineMilliseconds = 800;

			// Not in a queued task (but it is still called multiple times because...)
			ShapeMsg pointMsg = GrpcClientUtils.Try(
				o => rpcClient.GetOpenJawReshapeLineReplaceEndPoint(request, o),
				CancellationToken.None, deadlineMilliseconds);

			return await QueuedTaskUtils.Run(
				       () => (MapPoint) ProtobufConversionUtils.FromShapeMsg(pointMsg, sr));
		}

		public static ReshapeResult TryReshape(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault,
			CancellationToken cancellationToken)
		{
			var allInputFeatures = new Dictionary<GdbObjectReference, Feature>();

			FeatureProcessingUtils.AddInputFeatures(selectedFeatures, allInputFeatures);

			var request = CreateReshapeRequest(
				selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault);

			request.AllowOpenJawReshape = true;

			// TODO: If the server blocks for any reason, e.g. because
			// - it is overwhelmed by requests
			// - it hang in native code (DPS-#4)
			// the calls block (and cannot even be cancelled)
			// -> It is vital to use a request deadline to avoid hanging the entire application
			int deadline = 2000 * selectedFeatures.Count;

			AdvancedReshapeResponse reshapeResultMsg = GrpcClientUtils.Try(
				o => rpcClient.AdvancedReshape(request, o),
				cancellationToken, deadline, true);

			if (reshapeResultMsg == null)
			{
				return null;
			}

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

			// Create the result features with their new geometries:
			SpatialReference resultSpatialReference =
				selectedFeatures.FirstOrDefault()?.GetShape().SpatialReference;

			result.Add(FeatureDtoConversionUtils.FromUpdateMsgs(
				           reshapeResultMsg.ResultFeatures, allInputFeatures,
				           resultSpatialReference));

			return result;
		}

		public static ReshapeResult Reshape(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault,
			CancellationToken cancellationToken)
		{
			var allInputFeatures = new Dictionary<GdbObjectReference, Feature>();

			FeatureProcessingUtils.AddInputFeatures(selectedFeatures, allInputFeatures);

			if (adjacentFeatures != null)
				FeatureProcessingUtils.AddInputFeatures(adjacentFeatures, allInputFeatures);

			AdvancedReshapeRequest request = CreateReshapeRequest(
				selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault);

			return Reshape(rpcClient, request, allInputFeatures, cancellationToken);
		}

		private static ReshapeResult Reshape(
			[NotNull] ReshapeGrpc.ReshapeGrpcClient rpcClient,
			[NotNull] AdvancedReshapeRequest request,
			[NotNull] IReadOnlyDictionary<GdbObjectReference, Feature> allInputFeatures,
			CancellationToken cancellationToken)
		{
			request.AllowOpenJawReshape = true;

			int deadline = FeatureProcessingUtils.GetPerFeatureTimeOut() * request.Features.Count;

			AdvancedReshapeResponse reshapeResultMsg = GrpcClientUtils.Try(
				o => rpcClient.AdvancedReshape(request, o),
				cancellationToken, deadline);

			if (reshapeResultMsg == null)
			{
				return null;
			}

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

			// TODO: Consider separate converter / builder class or at least a record
			//       holding the necessary things such as spatial reference or class-id etc.
			SpatialReference resultSpatialReference =
				allInputFeatures.Values.FirstOrDefault()?.GetShape().SpatialReference;

			result.Add(FeatureDtoConversionUtils.FromUpdateMsgs(
				           reshapeResultMsg.ResultFeatures, allInputFeatures,
				           resultSpatialReference));

			return result;
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

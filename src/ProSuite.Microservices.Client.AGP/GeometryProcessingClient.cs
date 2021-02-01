using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Client.AGP
{
	public class GeometryProcessingClient : MicroserviceClientBase
	{
		private RemoveOverlapsGrpc.RemoveOverlapsGrpcClient RemoveOverlapsClient { get; set; }
		private ReshapeGrpc.ReshapeGrpcClient ReshapeClient { get; set; }

		public GeometryProcessingClient([NotNull] ClientChannelConfig channelConfig) : base(
			channelConfig) { }

		protected override string ServiceName =>
			RemoveOverlapsClient?.GetType().DeclaringType?.Name;

		protected override void ChannelOpenedCore(Channel channel)
		{
			RemoveOverlapsClient = new RemoveOverlapsGrpc.RemoveOverlapsGrpcClient(channel);
			ReshapeClient = new ReshapeGrpc.ReshapeGrpcClient(channel);
		}

		[CanBeNull]
		public Overlaps CalculateOverlaps(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> overlappingFeatures,
			CancellationToken cancellationToken)
		{
			return RemoveOverlapsClientUtils.CalculateOverlaps(
				RemoveOverlapsClient, selectedFeatures, overlappingFeatures, cancellationToken);
		}

		public RemoveOverlapsResult RemoveOverlaps(IEnumerable<Feature> selectedFeatures,
		                                           Overlaps overlapsToRemove,
		                                           IList<Feature> overlappingFeatures,
		                                           CancellationToken cancellationToken)
		{
			return RemoveOverlapsClientUtils.RemoveOverlaps(
				RemoveOverlapsClient, selectedFeatures, overlapsToRemove, overlappingFeatures,
				cancellationToken);
		}

		public async Task<ReshapeResult> ReshapeAsync(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault)
		{
			return await AdvancedReshapeClientUtils.ReshapeAsync(
				       ReshapeClient, selectedFeatures, reshapeLine, allowOpenJawReshape,
				       multiReshapeAsUnion, tryReshapeNonDefault);
		}

		public ReshapeResult Reshape(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault)
		{
			return AdvancedReshapeClientUtils.Reshape(
				ReshapeClient, selectedFeatures, reshapeLine, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault);
		}

		public async Task<MapPoint> GetOpenJawReplacementPointAsync(
			[NotNull] Feature polylineFeature,
			[NotNull] Polyline reshapeLine,
			bool useNonDefaultReshapeSide)
		{
			return await AdvancedReshapeClientUtils.GetOpenJawReplacementPointAsync(
				       ReshapeClient, polylineFeature, reshapeLine, useNonDefaultReshapeSide);
		}
	}
}

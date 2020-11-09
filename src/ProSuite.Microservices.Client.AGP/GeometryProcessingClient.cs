using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ArcGIS.Core.Data;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Client.AGP
{
	public class GeometryProcessingClient : MicroserviceClientBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private RemoveOverlapsGrpc.RemoveOverlapsGrpcClient RemoveOverlapsClient { get; set; }

		public GeometryProcessingClient([NotNull] ClientChannelConfig channelConfig) : base(
			channelConfig) { }

		protected override string ServiceName =>
			RemoveOverlapsClient?.GetType().DeclaringType?.Name;

		protected override void ChannelOpenedCore(Channel channel)
		{
			RemoveOverlapsClient = new RemoveOverlapsGrpc.RemoveOverlapsGrpcClient(channel);
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
	}
}

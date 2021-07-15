using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Client.AGP
{
	public class GeometryProcessingClient : MicroserviceClientBase
	{
		private RemoveOverlapsGrpc.RemoveOverlapsGrpcClient RemoveOverlapsClient { get; set; }
		private ChangeAlongGrpc.ChangeAlongGrpcClient ChangeAlongClient { get; set; }
		private ReshapeGrpc.ReshapeGrpcClient ReshapeClient { get; set; }

		public GeometryProcessingClient([NotNull] ClientChannelConfig channelConfig) : base(
			channelConfig) { }

		protected override string ServiceName =>
			RemoveOverlapsClient?.GetType().DeclaringType?.Name;

		protected override void ChannelOpenedCore(Channel channel)
		{
			RemoveOverlapsClient = new RemoveOverlapsGrpc.RemoveOverlapsGrpcClient(channel);
			ChangeAlongClient = new ChangeAlongGrpc.ChangeAlongGrpcClient(channel);
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

		[NotNull]
		public ChangeAlongCurves CalculateReshapeLines(
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			return ChangeAlongClientUtils.CalculateReshapeLines(
				ChangeAlongClient, sourceFeatures, targetFeatures, cancellationToken);
		}

		[NotNull]
		public ChangeAlongCurves CalculateCutLines(
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			return ChangeAlongClientUtils.CalculateCutLines(
				ChangeAlongClient, sourceFeatures, targetFeatures, cancellationToken);
		}

		[NotNull]
		public List<ResultFeature> ApplyReshapeLines(
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			[NotNull] IList<CutSubcurve> selectedReshapeLines,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			if (targetFeatures == null)
			{
				throw new ArgumentNullException(nameof(targetFeatures));
			}

			return ChangeAlongClientUtils.ApplyReshapeCurves(
				ChangeAlongClient, sourceFeatures, targetFeatures, selectedReshapeLines,
				cancellationToken, out newChangeAlongCurves);
		}

		[NotNull]
		public List<ResultFeature> ApplyCutLines(
			[NotNull] IList<Feature> sourceFeatures,
			[NotNull] IList<Feature> targetFeatures,
			[NotNull] IList<CutSubcurve> selectedReshapeLines,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			if (targetFeatures == null)
			{
				throw new ArgumentNullException(nameof(targetFeatures));
			}

			return ChangeAlongClientUtils.ApplyCutCurves(
				ChangeAlongClient, sourceFeatures, targetFeatures, selectedReshapeLines,
				cancellationToken, out newChangeAlongCurves);
		}

		public ReshapeResult TryReshape(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault,
			CancellationToken cancellationToken)
		{
			return AdvancedReshapeClientUtils.TryReshape(
				ReshapeClient, selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault, cancellationToken);
		}

		public ReshapeResult Reshape(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] Polyline reshapeLine,
			[CanBeNull] IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault,
			CancellationToken cancellationToken)
		{
			return AdvancedReshapeClientUtils.Reshape(
				ReshapeClient, selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault, cancellationToken);
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

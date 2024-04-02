using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using Grpc.Core;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Holes;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.FillHole;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps;
using ProSuite.Microservices.Client.GrpcCore;
using ProSuite.Microservices.Definitions.Geometry;

namespace ProSuite.Microservices.Client.AGP
{
	public class GeometryProcessingClient : MicroserviceClientBase,
	                                        IAdvancedReshapeService,
	                                        IRemoveOverlapsService,
	                                        ICalculateHolesService,
	                                        IChangeAlongService
	{
		private RemoveOverlapsGrpc.RemoveOverlapsGrpcClient RemoveOverlapsClient { get; set; }
		private ChangeAlongGrpc.ChangeAlongGrpcClient ChangeAlongClient { get; set; }
		private ReshapeGrpc.ReshapeGrpcClient ReshapeClient { get; set; }
		private FillHolesGrpc.FillHolesGrpcClient RemoveHolesClient { get; set; }

		public GeometryProcessingClient([NotNull] IClientChannelConfig channelConfig)
			: base(channelConfig) { }

		public GeometryProcessingClient([NotNull] IList<IClientChannelConfig> channelConfigs)
			: base(channelConfigs) { }

		public override string ServiceName =>
			RemoveOverlapsClient?.GetType().DeclaringType?.Name ?? "<no service name>";

		public override string ServiceDisplayName => "Geometry Service";

		protected override void ChannelOpenedCore(Channel channel)
		{
			RemoveOverlapsClient = new RemoveOverlapsGrpc.RemoveOverlapsGrpcClient(channel);
			ChangeAlongClient = new ChangeAlongGrpc.ChangeAlongGrpcClient(channel);
			ReshapeClient = new ReshapeGrpc.ReshapeGrpcClient(channel);
			RemoveHolesClient = new FillHolesGrpc.FillHolesGrpcClient(channel);
		}

		[CanBeNull]
		public Overlaps CalculateOverlaps(
			IList<Feature> selectedFeatures,
			IList<Feature> overlappingFeatures,
			CancellationToken cancellationToken)
		{
			if (RemoveOverlapsClient == null)
				throw new InvalidOperationException("No microservice available.");

			return RemoveOverlapsClientUtils.CalculateOverlaps(
				RemoveOverlapsClient, selectedFeatures, overlappingFeatures, cancellationToken);
		}

		public RemoveOverlapsResult RemoveOverlaps(IEnumerable<Feature> selectedFeatures,
		                                           Overlaps overlapsToRemove,
		                                           IList<Feature> overlappingFeatures,
		                                           CancellationToken cancellationToken)
		{
			if (RemoveOverlapsClient == null)
				throw new InvalidOperationException("No microservice available.");

			return RemoveOverlapsClientUtils.RemoveOverlaps(
				RemoveOverlapsClient, selectedFeatures, overlapsToRemove, overlappingFeatures,
				cancellationToken);
		}

		[CanBeNull]
		public IList<Holes> CalculateHoles(
			IList<Feature> selectedFeatures,
			IList<Envelope> clipEnvelopes,
			bool unionFeatures,
			CancellationToken cancellationToken)
		{
			if (RemoveHolesClient == null)
				throw new InvalidOperationException("No microservice available.");

			return HoleClientUtils.CalculateHoles(
				RemoveHolesClient, selectedFeatures, clipEnvelopes, unionFeatures,
				cancellationToken);
		}

		[NotNull]
		public ChangeAlongCurves CalculateReshapeLines(
			IList<Feature> sourceFeatures,
			IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			if (ChangeAlongClient == null)
				throw new InvalidOperationException("No microservice available.");

			return ChangeAlongClientUtils.CalculateReshapeLines(
				ChangeAlongClient, sourceFeatures, targetFeatures, cancellationToken);
		}

		[NotNull]
		public ChangeAlongCurves CalculateCutLines(
			IList<Feature> sourceFeatures,
			IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			if (ChangeAlongClient == null)
				throw new InvalidOperationException("No microservice available.");

			return ChangeAlongClientUtils.CalculateCutLines(
				ChangeAlongClient, sourceFeatures, targetFeatures, cancellationToken);
		}

		[NotNull]
		public List<ResultFeature> ApplyReshapeLines(
			IList<Feature> sourceFeatures,
			IList<Feature> targetFeatures,
			IList<CutSubcurve> selectedReshapeLines,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			if (targetFeatures == null)
			{
				throw new ArgumentNullException(nameof(targetFeatures));
			}

			if (ChangeAlongClient == null)
				throw new InvalidOperationException("No microservice available.");

			return ChangeAlongClientUtils.ApplyReshapeCurves(
				ChangeAlongClient, sourceFeatures, targetFeatures, selectedReshapeLines,
				cancellationToken, out newChangeAlongCurves);
		}

		[NotNull]
		public List<ResultFeature> ApplyCutLines(
			IList<Feature> sourceFeatures,
			IList<Feature> targetFeatures,
			IList<CutSubcurve> selectedReshapeLines,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			if (targetFeatures == null)
			{
				throw new ArgumentNullException(nameof(targetFeatures));
			}

			if (ChangeAlongClient == null)
				throw new InvalidOperationException("No microservice available.");

			return ChangeAlongClientUtils.ApplyCutCurves(
				ChangeAlongClient, sourceFeatures, targetFeatures, selectedReshapeLines,
				cancellationToken, out newChangeAlongCurves);
		}

		public ReshapeResult TryReshape(
			IList<Feature> selectedFeatures,
			Polyline reshapeLine,
			IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault,
			CancellationToken cancellationToken)
		{
			if (ReshapeClient == null)
				throw new InvalidOperationException("No microservice available.");

			return AdvancedReshapeClientUtils.TryReshape(
				ReshapeClient, selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault, cancellationToken);
		}

		public ReshapeResult Reshape(
			IList<Feature> selectedFeatures,
			Polyline reshapeLine,
			IList<Feature> adjacentFeatures,
			bool allowOpenJawReshape,
			bool multiReshapeAsUnion,
			bool tryReshapeNonDefault,
			CancellationToken cancellationToken)
		{
			if (ReshapeClient == null)
				throw new InvalidOperationException("No microservice available.");

			return AdvancedReshapeClientUtils.Reshape(
				ReshapeClient, selectedFeatures, reshapeLine, adjacentFeatures, allowOpenJawReshape,
				multiReshapeAsUnion, tryReshapeNonDefault, cancellationToken);
		}

		public async Task<MapPoint> GetOpenJawReplacementPointAsync(
			Feature polylineFeature,
			Polyline reshapeLine,
			bool useNonDefaultReshapeSide)
		{
			if (ReshapeClient == null)
				throw new InvalidOperationException("No microservice available.");

			return await AdvancedReshapeClientUtils.GetOpenJawReplacementPointAsync(
				       ReshapeClient, polylineFeature, reshapeLine, useNonDefaultReshapeSide);
		}
	}
}

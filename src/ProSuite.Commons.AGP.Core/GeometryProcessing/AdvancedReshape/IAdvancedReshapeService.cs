using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;

/// <summary>
/// Reshape service abstraction.
/// </summary>
public interface IAdvancedReshapeService
{
	ReshapeResult TryReshape(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] Polyline reshapeLine,
		[CanBeNull] IList<Feature> adjacentFeatures,
		bool allowOpenJawReshape,
		bool multiReshapeAsUnion,
		bool tryReshapeNonDefault,
		CancellationToken cancellationToken);

	ReshapeResult Reshape(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] Polyline reshapeLine,
		[CanBeNull] IList<Feature> adjacentFeatures,
		bool allowOpenJawReshape,
		bool multiReshapeAsUnion,
		bool tryReshapeNonDefault,
		CancellationToken cancellationToken, bool moveOpenJawEndJunction);

	Task<MapPoint> GetOpenJawReplacementPointAsync(
		[NotNull] Feature polylineFeature,
		[NotNull] Polyline reshapeLine,
		bool useNonDefaultReshapeSide);
}

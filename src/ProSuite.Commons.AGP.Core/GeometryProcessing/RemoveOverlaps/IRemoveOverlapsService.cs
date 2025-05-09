using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;

/// <summary>
/// Abstraction for the calculation of overlaps and the removal of overlaps.
/// </summary>
public interface IRemoveOverlapsService
{
	Overlaps CalculateOverlaps(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] IList<Feature> overlappingFeatures,
		CancellationToken cancellationToken);

	RemoveOverlapsResult RemoveOverlaps([NotNull] IEnumerable<Feature> selectedFeatures,
	                                    [NotNull] Overlaps overlapsToRemove,
	                                    [NotNull] IList<Feature> overlappingFeatures,
	                                    [NotNull] RemoveOverlapsOptions options,
	                                    CancellationToken cancellationToken);
}

using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;

/// <summary>
/// Abstraction for the calculation of overlaps and the removal of overlaps.
/// </summary>
public interface IRemoveOverlapsService
{
	[CanBeNull]
	Overlaps CalculateOverlaps(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] IList<Feature> overlappingFeatures,
		[CanBeNull] Envelope inExtent,
		CancellationToken cancellationToken);

	[CanBeNull]
	RemoveOverlapsResult RemoveOverlaps([NotNull] IEnumerable<Feature> selectedFeatures,
	                                    [NotNull] Overlaps overlapsToRemove,
	                                    [NotNull] IList<Feature> overlappingFeatures,
	                                    [NotNull] RemoveOverlapsToolOptions options,
	                                    CancellationToken cancellationToken);
}

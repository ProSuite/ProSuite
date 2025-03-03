using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize;

/// <summary>
/// Abstraction for the calculation of overlaps and the removal of overlaps.
/// </summary>
public interface IAdvancedGeneralizeService
{
	GeneralizeResult CalculateRemovableSegments([NotNull] IList<Feature> selectedFeatures,
	                                            [CanBeNull] IList<Feature> targetFeatures,
	                                            bool protectVerticesWithinSameClassOnly,
	                                            double? weedTolerance,
	                                            bool weedNonLinearSegments,
	                                            double? minimumSegmentLength,
	                                            bool use2DLength,
	                                            [CanBeNull] Geometry perimeter,
	                                            CancellationToken cancellationToken);

	[CanBeNull]
	IList<ResultFeature> ApplySegmentRemoval([NotNull] IList<Feature> selectedFeatures,
	                                         [NotNull] IList<GeneralizedFeature> segmentsToRemove,
	                                         double? weedTolerance,
	                                         bool weedNonLinearSegments,
	                                         double? minimumSegmentLength,
	                                         bool use2DLength,
	                                         [CanBeNull] Geometry perimeter,
	                                         CancellationToken cancellationToken);
}

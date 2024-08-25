using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;

/// <summary>
/// Abstraction for the calculation of crack points and the insertion of such in source geometries.
/// </summary>
public interface ICrackerService
{
	CrackPoints CalculateCrackPoints(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] IList<Feature> intersectingFeatures,
		CancellationToken cancellationToken);

	CrackerResult Cracker([NotNull] IEnumerable<Feature> selectedFeatures,
	                                    [NotNull] CrackPoints crackPointsToAdd,
	                                    [NotNull] IList<Feature> intersectingFeatures,
	                                    CancellationToken cancellationToken);
}

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
	CrackerResult CalculateCrackPoints([NotNull] IList<Feature> selectedFeatures,
	                                   [NotNull] IList<Feature> targetFeatures,
	                                   ICrackerToolOptions crackerToolOptions,
	                                   IntersectionPointOptions intersectionPointOptions,
	                                   bool addCrackPointsOnExistingVertices,
	                                   CancellationToken cancellationToken);

	IList<ResultFeature> ApplyCrackPoints([NotNull] IEnumerable<Feature> selectedFeatures,
	                                      [NotNull] CrackerResult crackPointsToAdd,
	                                      [NotNull] IList<Feature> intersectingFeatures,
	                                      ICrackerToolOptions crackerOptions,
	                                      IntersectionPointOptions intersectionPointOptions,
	                                      bool addCrackPointsOnExistingVertices,
	                                      CancellationToken cancellationToken);

	IList<ResultFeature> ChopLines([NotNull] IEnumerable<Feature> selectedFeatures,
	                               [NotNull] CrackerResult splitPoints,
	                               [NotNull] IList<Feature> intersectingFeatures,
	                               ICrackerToolOptions chopperOptions,
	                               IntersectionPointOptions intersectionPointOptions,
	                               bool addCrackPointsOnExistingVertices,
	                               CancellationToken cancellationToken);
}

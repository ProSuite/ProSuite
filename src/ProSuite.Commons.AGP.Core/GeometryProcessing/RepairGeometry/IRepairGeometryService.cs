using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RepairGeometry;

public interface IRepairGeometryService
{
	[CanBeNull]
	RepairGeometryResult CalculateRepairInfo(
		[NotNull] IList<Feature> sourceFeatures,
		double minimumSegmentLength,
		bool allowLoops,
		bool allowLinearSelfIntersections,
		bool addCrackPointsBetweenParts,
		double crackPointTolerance,
		bool use2D,
		CancellationToken cancellationToken);

	[CanBeNull]
	IList<ResultFeature> ApplyRepairGeometry(
		[NotNull] IList<Feature> sourceFeatures,
		[NotNull] IList<RepairableFeature> repairInfos,
		double minimumSegmentLength,
		bool allowLoops,
		bool allowLinearSelfIntersections,
		double crackPointTolerance,
		bool use2D,
		CancellationToken cancellationToken);
}

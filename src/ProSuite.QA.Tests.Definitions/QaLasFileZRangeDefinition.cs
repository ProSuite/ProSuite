using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Checks the LAS files of a point cloud that cover the verified features: that their header
	/// can be read at all, and that the Z range it declares is within the given limits.
	/// </summary>
	[UsedImplicitly]
	[ZValuesTest]
	public class QaLasFileZRangeDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PerimeterClass { get; }

		public IPointCloudDatasetDef PointCloud { get; }

		public double MinimumZ { get; }

		public double MaximumZ { get; }

		[Doc(nameof(DocStrings.QaLasFileZRange_0))]
		public QaLasFileZRangeDefinition(
			[Doc(nameof(DocStrings.QaLasFileZRange_perimeterClass))] [NotNull]
			IFeatureClassSchemaDef perimeterClass,
			[Doc(nameof(DocStrings.QaLasFileZRange_pointCloud))] [NotNull]
			IPointCloudDatasetDef pointCloud,
			[Doc(nameof(DocStrings.QaLasFileZRange_minimumZ))]
			double minimumZ,
			[Doc(nameof(DocStrings.QaLasFileZRange_maximumZ))]
			double maximumZ)
			: base(perimeterClass)
		{
			Assert.ArgumentNotNull(perimeterClass, nameof(perimeterClass));
			Assert.ArgumentNotNull(pointCloud, nameof(pointCloud));

			PerimeterClass = perimeterClass;
			PointCloud = pointCloud;
			MinimumZ = minimumZ;
			MaximumZ = maximumZ;
		}
	}
}

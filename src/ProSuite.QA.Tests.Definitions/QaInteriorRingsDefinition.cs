using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaInteriorRingsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolygonClass { get; }
		public int MaximumInteriorRingCount { get; }

		private const double _defaultIgnoreInnerRingsLargerThan = -1;
		private const bool _defaultReportIndividualRings = false;
		private const bool _defaultReportOnlySmallestRingsExceedingMaximumCount = true;

		[Doc(nameof(DocStrings.QaInteriorRings_0))]
		public QaInteriorRingsDefinition(
			[Doc(nameof(DocStrings.QaInteriorRings_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaInteriorRings_maximumInteriorRingCount))]
			int maximumInteriorRingCount)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentCondition(
				polygonClass.ShapeType == ProSuiteGeometryType.Polygon,
				"polygon feature class expected");

			PolygonClass = polygonClass;
			MaximumInteriorRingCount = maximumInteriorRingCount;

			IgnoreInnerRingsLargerThan = _defaultIgnoreInnerRingsLargerThan;
			ReportIndividualRings = _defaultReportIndividualRings;
			ReportOnlySmallestRingsExceedingMaximumCount =
				_defaultReportOnlySmallestRingsExceedingMaximumCount;
		}

		[TestParameter(_defaultIgnoreInnerRingsLargerThan)]
		[Doc(nameof(DocStrings.QaInteriorRings_IgnoreInnerRingsLargerThan))]
		[UsedImplicitly]
		public double IgnoreInnerRingsLargerThan { get; set; }

		[TestParameter(_defaultReportIndividualRings)]
		[Doc(nameof(DocStrings.QaInteriorRings_ReportIndividualRings))]
		public bool ReportIndividualRings { get; set; }

		[TestParameter(_defaultReportOnlySmallestRingsExceedingMaximumCount)]
		[Doc(nameof(DocStrings.QaInteriorRings_ReportOnlySmallestRingsExceedingMaximumCount))]
		public bool ReportOnlySmallestRingsExceedingMaximumCount { get; set; }
	}
}

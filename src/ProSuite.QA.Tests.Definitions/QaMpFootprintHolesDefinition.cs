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
	public class QaMpFootprintHolesDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public InnerRingHandling InnerRingHandling { get; }

		private readonly InnerRingHandling _innerRingHandling;
		private const double _defaultResolutionFactor = 1;
		private const double _defaultMinimumArea = -1;

		[Doc(nameof(DocStrings.QaMpFootprintHoles_0))]
		public QaMpFootprintHolesDefinition(
			[Doc(nameof(DocStrings.QaMpFootprintHoles_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings.QaMpFootprintHoles_innerRingHandling))]
			InnerRingHandling innerRingHandling)
			: base(multiPatchClass)
		{
			Assert.ArgumentNotNull(multiPatchClass, nameof(multiPatchClass));
			Assert.ArgumentCondition(
				multiPatchClass.ShapeType == ProSuiteGeometryType.MultiPatch,
				"Multipatch feature class expected");

			MultiPatchClass = multiPatchClass;
			InnerRingHandling = innerRingHandling;
		}

		[UsedImplicitly]
		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaMpFootprintHoles_HorizontalZTolerance))]
		public double HorizontalZTolerance { get; set; }

		[UsedImplicitly]
		[TestParameter(_defaultResolutionFactor)]
		[Doc(nameof(DocStrings.QaMpFootprintHoles_ResolutionFactor))]
		public double ResolutionFactor { get; set; } = 1;

		[UsedImplicitly]
		[TestParameter(_defaultMinimumArea)]
		[Doc(nameof(DocStrings.QaMpFootprintHoles_MinimumArea))]
		public double MinimumArea { get; set; } = -1;

		[UsedImplicitly]
		[TestParameter(true)]
		[Doc(nameof(DocStrings
			            .QaMpFootprintHoles_ReportVerticalPatchesNotCompletelyWithinFootprint))]
		public bool ReportVerticalPatchesNotCompletelyWithinFootprint { get; set; } =
			true;
	}
}

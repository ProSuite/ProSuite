using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpSinglePartFootprintDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }

		private const double _defaultResolutionFactor = 100;
		private double _resolutionFactor = _defaultResolutionFactor;

		[Doc(nameof(DocStrings.QaMpSinglePartFootprint_0))]
		public QaMpSinglePartFootprintDefinition(
			[Doc(nameof(DocStrings.QaMpSinglePartFootprint_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass)
			: base(multiPatchClass)
		{
			Assert.ArgumentNotNull(multiPatchClass, nameof(multiPatchClass));

			MultiPatchClass = multiPatchClass;
		}

		[UsedImplicitly]
		[TestParameter(_defaultResolutionFactor)]
		[Doc(nameof(DocStrings.QaMpSinglePartFootprint_ResolutionFactor))]
		public double ResolutionFactor
		{
			get { return _resolutionFactor; }
			set
			{
				Assert.ArgumentCondition(value >= 1, "value must be >= 1");

				_resolutionFactor = value;
			}
		}
	}
}

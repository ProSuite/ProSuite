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
	[ZValuesTest]
	public class Qa3dConstantZDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Tolerance { get; }

		[Doc(nameof(DocStrings.Qa3dConstantZ_0))]
		public Qa3dConstantZDefinition(
			[Doc(nameof(DocStrings.Qa3dConstantZ_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.Qa3dConstantZ_tolerance))]
			double tolerance)
			: base(featureClass)
		{
			Assert.ArgumentCondition(tolerance >= 0, "tolerance must be >= 0");

			FeatureClass = featureClass;
			Tolerance = tolerance;
		}
	}
}

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
	public class QaNoTouchingPartsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		[Doc(nameof(DocStrings.QaNoTouchingParts_0))]
		public QaNoTouchingPartsDefinition(
			[Doc(nameof(DocStrings.QaNoTouchingParts_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			FeatureClass = featureClass;
		}
	}
}

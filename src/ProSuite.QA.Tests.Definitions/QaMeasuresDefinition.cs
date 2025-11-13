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
	[MValuesTest]
	public class QaMeasuresDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double InvalidValue { get; }

		[Doc(nameof(DocStrings.QaMeasures_0))]
		public QaMeasuresDefinition(
				[Doc(nameof(DocStrings.QaMeasures_featureClass))] [NotNull]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, double.NaN) { }

		[Doc(nameof(DocStrings.QaMeasures_1))]
		public QaMeasuresDefinition(
			[Doc(nameof(DocStrings.QaMeasures_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMeasures_invalidValue))]
			double invalidValue)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			FeatureClass = featureClass;
			InvalidValue = invalidValue;
		}
	}
}

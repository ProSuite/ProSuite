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
	public class QaNonEmptyGeometryDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public bool DontFilterPolycurvesByZeroLength { get; }

		[Doc(nameof(DocStrings.QaNonEmptyGeometry_0))]
		public QaNonEmptyGeometryDefinition(
				[NotNull] [Doc(nameof(DocStrings.QaNonEmptyGeometry_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, false) { }

		[Doc(nameof(DocStrings.QaNonEmptyGeometry_0))]
		public QaNonEmptyGeometryDefinition(
			[NotNull] [Doc(nameof(DocStrings.QaNonEmptyGeometry_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNonEmptyGeometry_dontFilterPolycurvesByZeroLength))]
			bool dontFilterPolycurvesByZeroLength)
			: base(new[] { (ITableSchemaDef) featureClass })
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			FeatureClass = featureClass;
			DontFilterPolycurvesByZeroLength = dontFilterPolycurvesByZeroLength;
		}
	}
}

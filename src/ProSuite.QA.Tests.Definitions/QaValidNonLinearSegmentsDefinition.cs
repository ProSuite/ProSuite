using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[GeometryTest]
	[UsedImplicitly]
	public class QaValidNonLinearSegmentsDefinition : AlgorithmDefinition

	{
		public IFeatureClassSchemaDef FeatureClass { get; set; }
		public double MinimumChordHeight { get; }

		[Doc(nameof(DocStrings.QaValidNonLinearSegments_0))]
		public QaValidNonLinearSegmentsDefinition(
				[Doc(nameof(DocStrings.QaValidNonLinearSegments_featureClass))] [NotNull]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, 0d) { }

		[Doc(nameof(DocStrings.QaValidNonLinearSegments_1))]
		public QaValidNonLinearSegmentsDefinition(
			[Doc(nameof(DocStrings.QaValidNonLinearSegments_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaValidNonLinearSegments_minimumChordHeight))]
			double minimumChordHeight)
			: base(new[] { (ITableSchemaDef) featureClass })
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			FeatureClass = featureClass;
			MinimumChordHeight = minimumChordHeight;
		}
	}
}

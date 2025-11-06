using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Finds all invalid geometries (null, empty, not simple)
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaSimpleGeometryDefinition : AlgorithmDefinition
	{
		public bool AllowNonPlanarLines { get; }
		public double ToleranceFactor { get; }

		public IFeatureClassSchemaDef FeatureClass { get; set; }

		private const double _defaultToleranceFactor = 0.4; // 0.3546??  -> 1 / (sqrt(2) / 2)

		[Doc(nameof(DocStrings.QaSimpleGeometry_0))]
		public QaSimpleGeometryDefinition(
				[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, false, _defaultToleranceFactor) { }

		[Doc(nameof(DocStrings.QaSimpleGeometry_1))]
		public QaSimpleGeometryDefinition(
				[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaSimpleGeometry_allowNonPlanarLines))]
				bool allowNonPlanarLines)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, allowNonPlanarLines, _defaultToleranceFactor) { }

		[Doc(nameof(DocStrings.QaSimpleGeometry_2))]
		public QaSimpleGeometryDefinition(
			[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSimpleGeometry_allowNonPlanarLines))]
			bool allowNonPlanarLines,
			[Doc(nameof(DocStrings.QaSimpleGeometry_toleranceFactor))]
			double toleranceFactor)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			FeatureClass = featureClass;
			AllowNonPlanarLines = allowNonPlanarLines;
			ToleranceFactor = toleranceFactor;
		}
	}
}

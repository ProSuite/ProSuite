using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaSpatialReferenceDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public IFeatureClassSchemaDef ReferenceFeatureClass { get; }
		public string SpatialReferenceXml { get; }
		public bool CompareXYPrecision { get; }
		public bool CompareZPrecision { get; }
		public bool CompareMPrecision { get; }
		public bool? CompareUsedPrecisions { get; }
		public bool CompareTolerances { get; }
		public bool CompareXyTolerance { get; }
		public bool CompareZTolerance { get; }
		public bool CompareMTolerance { get; }
		public bool CompareVerticalCoordinateSystems { get; }

		private const bool _defaultCompareXyDomainOrigin = false;
		private const bool _defaultCompareZDomainOrigin = false;
		private const bool _defaultCompareMDomainOrigin = false;

		private const bool _defaultCompareXyResolution = false;
		private const bool _defaultCompareZResolution = false;
		private const bool _defaultCompareMResolution = false;

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_0))]
		public QaSchemaSpatialReferenceDefinition(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_referenceFeatureClass))] [NotNull]
			IFeatureClassSchemaDef referenceFeatureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_compareXYPrecision))]
			bool compareXYPrecision, bool compareZPrecision,
			bool compareMPrecision, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: base(new[] { featureClass }.Union(new[] { referenceFeatureClass }))
		{
			FeatureClass = featureClass;
			ReferenceFeatureClass = referenceFeatureClass;
			CompareXYPrecision = compareXYPrecision;
			CompareZPrecision = compareZPrecision;
			CompareMPrecision = compareMPrecision;
			CompareXyTolerance = compareTolerances;
			CompareZTolerance = compareTolerances;
			CompareMTolerance = compareTolerances;
			CompareVerticalCoordinateSystems = compareVerticalCoordinateSystems;
		}

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_1))]
		public QaSchemaSpatialReferenceDefinition(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string spatialReferenceXml,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_compareXYPrecision))]
			bool compareXYPrecision, bool compareZPrecision,
			bool compareMPrecision, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			SpatialReferenceXml = spatialReferenceXml;
			CompareXYPrecision = compareXYPrecision;
			CompareZPrecision = compareZPrecision;
			CompareMPrecision = compareMPrecision;
			CompareTolerances = compareTolerances;
			CompareVerticalCoordinateSystems = compareVerticalCoordinateSystems;
		}

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_2))]
		public QaSchemaSpatialReferenceDefinition(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_referenceFeatureClass))] [NotNull]
			IFeatureClassSchemaDef referenceFeatureClass,
			bool compareUsedPrecisions, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: base(new[] { featureClass }.Union(new[] { referenceFeatureClass }))
		{
			FeatureClass = featureClass;
			ReferenceFeatureClass = referenceFeatureClass;
			CompareUsedPrecisions = compareUsedPrecisions;
			CompareTolerances = compareTolerances;
			CompareVerticalCoordinateSystems = compareVerticalCoordinateSystems;
		}

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_3))]
		public QaSchemaSpatialReferenceDefinition(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string spatialReferenceXml,
			bool compareUsedPrecisions, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			SpatialReferenceXml = spatialReferenceXml;
			CompareUsedPrecisions = compareUsedPrecisions;
			CompareTolerances = compareTolerances;
			CompareVerticalCoordinateSystems = compareVerticalCoordinateSystems;
		}

		[TestParameter(_defaultCompareXyDomainOrigin)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareXYDomainOrigin))]
		public bool CompareXYDomainOrigin { get; set; }

		[TestParameter(_defaultCompareZDomainOrigin)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareZDomainOrigin))]
		public bool CompareZDomainOrigin { get; set; }

		[TestParameter(_defaultCompareMDomainOrigin)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareMDomainOrigin))]
		public bool CompareMDomainOrigin { get; set; }

		[TestParameter(_defaultCompareXyResolution)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareXYResolution))]
		public bool CompareXYResolution { get; set; }

		[TestParameter(_defaultCompareZResolution)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareZResolution))]
		public bool CompareZResolution { get; set; }

		[TestParameter(_defaultCompareMResolution)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareMResolution))]
		public bool CompareMResolution { get; set; }
	}
}

using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaNotNearDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public IFeatureClassSchemaDef Reference { get; }
		public double Near { get; }
		public double MinLength { get; }
		public bool Is3D { get; }
		public double TileSize { get; }

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaNotNear_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D, tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: base(new[] { featureClass })
		{
			FeatureClass = featureClass;
			Near = near;
			MinLength = minLength;
			Is3D = is3D;
			TileSize = tileSize;
		}

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IFeatureClassSchemaDef reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaNotNear_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D: is3D,
			       tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: base(new[] { featureClass, reference })
		{
			FeatureClass = featureClass;
			Reference = reference;
			Near = near;
			MinLength = minLength;
			Is3D = is3D;
			TileSize = tileSize;
		}

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D: false, tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, near, minLength, is3D: false, tileSize: tileSize) { }

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IFeatureClassSchemaDef reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D: false, tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, is3D: false) { }

		[TestParameter]
		[Doc(nameof(DocStrings.QaNotNear_IgnoreNeighborCondition))]
		public string IgnoreNeighborCondition { get; set; }
	}
}

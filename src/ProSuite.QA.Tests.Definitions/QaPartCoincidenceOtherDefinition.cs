using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPartCoincidenceOtherDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public IFeatureClassSchemaDef Reference { get; }
		public double Near { get; }
		public double ConnectedMinLength { get; }
		public double DisjointMinLength { get; }
		public bool Is3D { get; }
		public double TileSize { get; }
		public double CoincidenceTolerance { get; }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOtherDefinition(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
				IFeatureClassSchemaDef reference,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOtherDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength,
			       minLength, is3D, tileSize, 0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOtherDefinition(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
				IFeatureClassSchemaDef reference,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, false, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOtherDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, false, tileSize) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_4))]
		public QaPartCoincidenceOtherDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_connectedMinLength))]
			double connectedMinLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_disjointMinLength))]
			double disjointMinLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize,
			[Doc(nameof(DocStrings.QaPartCoincidence_coincidenceTolerance))]
			double coincidenceTolerance)
			: base(new[] { featureClass, reference })
		{
			FeatureClass = featureClass;
			Reference = reference;
			Near = near;
			ConnectedMinLength = connectedMinLength;
			DisjointMinLength = disjointMinLength;
			Is3D = is3D;
			TileSize = tileSize;
			CoincidenceTolerance = coincidenceTolerance;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaPartCoincidenceOther_IgnoreNeighborCondition))]
		public string IgnoreNeighborCondition { get; set; }
	}
}

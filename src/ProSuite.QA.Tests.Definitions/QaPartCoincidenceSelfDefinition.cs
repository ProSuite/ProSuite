using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPartCoincidenceSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public double Near { get; }
		public double ConnectedMinLength { get; }
		public double DisjointMinLength { get; }
		public bool Is3D { get; }
		public double TileSize { get; }
		public double CoincidenceTolerance { get; }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_0))]
		public QaPartCoincidenceSelfDefinition(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_0))]
		public QaPartCoincidenceSelfDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(new[] { featureClass }, near, minLength, is3D, tileSize) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelfDefinition(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, near, minLength, is3D, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelfDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClasses, near, minLength, minLength, is3D, tileSize, 0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelfDefinition(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, near, minLength, false, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_2))]
		public QaPartCoincidenceSelfDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClasses, near, minLength, false, tileSize) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_6))]
		public QaPartCoincidenceSelfDefinition(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
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
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			Near = near;
			ConnectedMinLength = connectedMinLength;
			DisjointMinLength = disjointMinLength;
			Is3D = is3D;
			TileSize = tileSize;
			CoincidenceTolerance = coincidenceTolerance;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaPartCoincidenceSelf_IgnoreNeighborConditions))]
		public IList<string> IgnoreNeighborConditions { get; set; }
	}
}

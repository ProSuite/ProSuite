using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaFullCoincidenceDefinition : AlgorithmDefinition
	{
		// TODO should be dependent at least on sref
		private const double _defaultTileSize = 1000.0;

		public IFeatureClassSchemaDef FeatureClass { get; }
		public IList<IFeatureClassSchemaDef> References { get; }
		public double Near { get;}
		public bool Is3D { get; }
		public double TileSize { get;}

		[Doc(nameof(DocStrings.QaFullCoincidence_0))]
		public QaFullCoincidenceDefinition(
				[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaFullCoincidence_reference))]
				IFeatureClassSchemaDef reference,
				[Doc(nameof(DocStrings.QaFullCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, is3D, _defaultTileSize) { }

		[Doc(nameof(DocStrings.QaFullCoincidence_0))]
		public QaFullCoincidenceDefinition(
			[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaFullCoincidence_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaFullCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaFullCoincidence_tileSize))]
			double tileSize)
			: base(new[] { featureClass, reference })
		{
			Assert.ArgumentNotNull(reference, nameof(reference));
			FeatureClass = featureClass;
			References = new[] { reference };
			Near = near;
			Is3D = is3D;
			TileSize = tileSize;
		}

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidenceDefinition(
				[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaFullCoincidence_references))]
				IList<IFeatureClassSchemaDef> references,
				[Doc(nameof(DocStrings.QaFullCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, references, near, is3D, _defaultTileSize) { }

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidenceDefinition(
			[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaFullCoincidence_references))]
			IList<IFeatureClassSchemaDef> references,
			[Doc(nameof(DocStrings.QaFullCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaFullCoincidence_tileSize))]
			double tileSize)
			: base(
				Union(new[] { featureClass }, references))
		{
			Assert.ArgumentNotNull(references, nameof(references));
			FeatureClass = featureClass;
			References = references;
			Near = near;
			Is3D = is3D;
			TileSize = tileSize;
		}

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidenceDefinition(
				[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaFullCoincidence_references))]
				IList<IFeatureClassSchemaDef> references,
				[Doc(nameof(DocStrings.QaFullCoincidence_near))]
				double near)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, references, near, false, _defaultTileSize) { }

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidenceDefinition(
			[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaFullCoincidence_references))]
			IList<IFeatureClassSchemaDef> references,
			[Doc(nameof(DocStrings.QaFullCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaFullCoincidence_tileSize))]
			double tileSize)
			: this(featureClass, references, near, false, tileSize) { }

		[TestParameter]
		[Doc(nameof(DocStrings.QaFullCoincidence_IgnoreNeighborConditions))]
		public IList<string> IgnoreNeighborConditions { get; set; }
	}
}

using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMultipartDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public bool SingleRing { get; }

		private readonly bool _singleRing;
		private readonly string _shapeFieldName;

		[Doc(nameof(DocStrings.QaMultipart_0))]
		public QaMultipartDefinition(
				[Doc(nameof(DocStrings.QaMultipart_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this((IReadOnlyFeatureClass) featureClass, singleRing: false) { }

		[Doc(nameof(DocStrings.QaMultipart_0))]
		public QaMultipartDefinition(
			[Doc(nameof(DocStrings.QaMultipart_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMultipart_singleRing))]
			bool singleRing)
			: base((ITableSchemaDef) featureClass)
		{
			FeatureClass = (IFeatureClassSchemaDef) featureClass;
			SingleRing = singleRing;

			_singleRing = singleRing;
			_shapeFieldName = featureClass.ShapeFieldName;
		}
	}
}

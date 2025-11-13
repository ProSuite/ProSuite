using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers
{
	[GeometryTransformer]
	[ZValuesTest]
	public class TrZAssignDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public IRasterDatasetDef Raster { get; }
		public IMosaicRasterDatasetDef RasterMosaic { get; }

		private const AssignOption _defaultZAssignOption = AssignOption.Tile;

		[DocTr(nameof(DocTrStrings.TrZAssign_0))]
		public TrZAssignDefinition(
			[DocTr(nameof(DocTrStrings.TrZAssign_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[DocTr(nameof(DocTrStrings.TrZAssign_raster))]
			IRasterDatasetDef raster)
			: base(featureClass, featureClass.ShapeType)
		{
			FeatureClass = featureClass;
			Raster = raster;
		}

		[DocTr(nameof(DocTrStrings.TrZAssign_0))]
		public TrZAssignDefinition(
			[DocTr(nameof(DocTrStrings.TrZAssign_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[DocTr(nameof(DocTrStrings.TrZAssign_rasterMosaic))]
			IMosaicRasterDatasetDef rasterMosaic)
			: base(featureClass, featureClass.ShapeType)
		{
			FeatureClass = featureClass;
			RasterMosaic = rasterMosaic;
		}

		[TestParameter(_defaultZAssignOption)]
		[DocTr(nameof(DocTrStrings.TrZAssign_ZAssignOption))]
		public AssignOption ZAssignOption { get; set; } = _defaultZAssignOption;
	}
}

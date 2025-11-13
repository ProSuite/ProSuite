using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ZValuesTest]
	public class QaSurfaceVertexDefinition : QaSurfaceOffsetDefinition
	{
		public IFeatureClassSchemaDef FeatureClass =>
			(IFeatureClassSchemaDef) InvolvedTables.FirstOrDefault();

		[Doc(nameof(DocStrings.Qa3dSmoothing_0))]
		public QaSurfaceVertexDefinition(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.Qa3dSmoothing_terrain))] [NotNull]
			ITerrainDef terrain,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mustBeLarger))]
			bool mustBeLarger)
			: this(featureClass, terrain, limit, mustBeLarger
				                                     ? ZOffsetConstraint.AboveLimit
				                                     : ZOffsetConstraint.WithinLimit) { }

		[Doc(nameof(DocStrings.Qa3dSmoothing_0))]
		public QaSurfaceVertexDefinition(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.Qa3dSmoothing_terrain))] [NotNull]
			ITerrainDef terrain,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint)
			: base(featureClass, terrain, 0, limit, zOffsetConstraint) { }

		[Doc(nameof(DocStrings.QaSurfaceVertex_2))]
		public QaSurfaceVertexDefinition(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_raster))] [NotNull]
			IRasterDatasetDef raster,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mustBeLarger))]
			bool mustBeLarger)
			: this(featureClass, raster, limit, mustBeLarger
				                                    ? ZOffsetConstraint.AboveLimit
				                                    : ZOffsetConstraint.WithinLimit) { }

		[Doc(nameof(DocStrings.QaSurfaceVertex_2))]
		public QaSurfaceVertexDefinition(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_raster))] [NotNull]
			IRasterDatasetDef raster,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint)
			: base(featureClass, raster, limit, zOffsetConstraint) { }

		[Doc(nameof(DocStrings.QaSurfaceVertex_4))]
		public QaSurfaceVertexDefinition(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mosaic))] [NotNull]
			IMosaicRasterDatasetDef rasterMosaic,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mustBeLarger))]
			bool mustBeLarger)
			: this(featureClass, rasterMosaic, limit, mustBeLarger
				                                          ? ZOffsetConstraint.AboveLimit
				                                          : ZOffsetConstraint.WithinLimit) { }

		[Doc(nameof(DocStrings.QaSurfaceVertex_4))]
		public QaSurfaceVertexDefinition(
			[Doc(nameof(DocStrings.QaSurfaceVertex_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfaceVertex_mosaic))] [NotNull]
			IMosaicRasterDatasetDef rasterMosaic,
			[Doc(nameof(DocStrings.QaSurfaceVertex_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfaceVertex_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint)
			: base(featureClass, rasterMosaic, limit, zOffsetConstraint) { }
	}
}

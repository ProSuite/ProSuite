using System;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// checks whether interpolated height values are close enough to height model
	/// </summary>
	//Remark: Implement for "ISurfaceProvider"
	[UsedImplicitly]
	[ZValuesTest]
	public class QaSurfacePipeDefinition : QaSurfaceOffsetDefinition
	{
		public double StartEndIgnoreLength { get; }
		public bool AsRatio { get; }

		//#region issue codes

		//[CanBeNull] private static TestIssueCodes _codes;

		//[NotNull]
		//[UsedImplicitly]
		//public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		//private class Code : LocalTestIssueCodes
		//{
		//	public const string NoTerrainData = "NoTerrainData";

		//	public const string ZOffset_NotEnoughAboveTerrain =
		//		"ZOffset.NotEnoughAboveTerrain";

		//	public const string ZOffset_NotEnoughBelowTerrain =
		//		"ZOffset.NotEnoughBelowTerrain";

		//	public const string ZOffset_TooCloseToTerrain =
		//		"ZOffset.TooCloseToTerrain";

		//	public const string ZOffset_TooFarFromTerrain =
		//		"ZOffset.TooFarFromTerrain";

		//	public Code() : base("GeometryToTerrainZOffset") { }
		//}

		//#endregion

		#region constructors

		[Doc(nameof(DocStrings.Qa3dPipe_0))]
		public QaSurfacePipeDefinition(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_terrain))] [NotNull]
			ITerrainDef terrain,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit)
			: this(featureClass, terrain, limit,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ZOffsetConstraint.WithinLimit, 0, false) { }

		[Doc(nameof(DocStrings.QaSurfacePipe_1))]
		public QaSurfacePipeDefinition(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_terrain))] [NotNull]
			ITerrainDef terrain,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfacePipe_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint,
			[Doc(nameof(DocStrings.QaSurfacePipe_startEndIgnoreLength))]
			double startEndIgnoreLength,
			[Doc(nameof(DocStrings.QaSurfacePipe_asRatio))]
			bool asRatio)
			: base(featureClass, terrain, 0, limit, zOffsetConstraint)
		{
			ValidateAsRatio(startEndIgnoreLength, asRatio);

			StartEndIgnoreLength = startEndIgnoreLength;
			AsRatio = asRatio;

			//_interpolateTolerance =
			//	2 * SpatialReferenceUtils.GetXyResolution(featureClass.SpatialReference);
		}

		// Consider changing the signature of the test to any RasterReference and do the junction within the test
		// or add IReadOnlyRaster -> IDbRaster

		[Doc(nameof(DocStrings.QaSurfacePipe_2))]
		public QaSurfacePipeDefinition(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_raster))] [NotNull]
			IRasterDatasetDef raster,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit)
			: this(featureClass, raster, limit,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ZOffsetConstraint.WithinLimit, 0, false) { }

		[Doc(nameof(DocStrings.QaSurfacePipe_2))]
		public QaSurfacePipeDefinition(
			[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSurfacePipe_raster))] [NotNull]
			IRasterDatasetDef raster,
			[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSurfacePipe_zOffsetConstraint))]
			ZOffsetConstraint zOffsetConstraint,
			[Doc(nameof(DocStrings.QaSurfacePipe_startEndIgnoreLength))]
			double startEndIgnoreLength,
			[Doc(nameof(DocStrings.QaSurfacePipe_asRatio))]
			bool asRatio)
			: base(featureClass, raster, limit, zOffsetConstraint)
		{
			ValidateAsRatio(startEndIgnoreLength, asRatio);

			StartEndIgnoreLength = startEndIgnoreLength;
			AsRatio = asRatio;
		}

		// TODO: Can we reduce the constructor count to 4? -> Always use IDbRaster which could be both a file or mosaic raster?
		//[Doc(nameof(DocStrings.QaSurfacePipe_4))]
		//public QaSurfacePipeDefinition(
		//	[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
		//	IReadOnlyFeatureClass featureClass,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_mosaic))] [NotNull]
		//	SimpleRasterMosaic rasterMosaic,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
		//	double limit)
		//	: this(featureClass, rasterMosaic, limit,
		//	       // ReSharper disable once IntroduceOptionalParameters.Global
		//	       ZOffsetConstraint.WithinLimit, 0, false) { }

		//[Doc(nameof(DocStrings.QaSurfacePipe_4))]
		//public QaSurfacePipeDefinition(
		//	[Doc(nameof(DocStrings.QaSurfacePipe_featureClass))] [NotNull]
		//	IReadOnlyFeatureClass featureClass,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_mosaic))] [NotNull]
		//	SimpleRasterMosaic rasterMosaic,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_limit))]
		//	double limit,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_zOffsetConstraint))]
		//	ZOffsetConstraint zOffsetConstraint,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_startEndIgnoreLength))]
		//	double startEndIgnoreLength,
		//	[Doc(nameof(DocStrings.QaSurfacePipe_asRatio))]
		//	bool asRatio)
		//	: base(
		//		featureClass, new MosaicRasterReference(rasterMosaic), limit,
		//		zOffsetConstraint)
		//{
		//	ValidateAsRatio(startEndIgnoreLength, asRatio);

		//	_shapeType = featureClass.ShapeType;
		//	StartEndIgnoreLength = startEndIgnoreLength;
		//	AsRatio = asRatio;
		//}

		#endregion

		private static void ValidateAsRatio(double startEndIgnoreLength, bool asRatio)
		{
			if (! asRatio || ! (startEndIgnoreLength >= 0.5))
			{
				return;
			}

			throw new ArgumentOutOfRangeException(
				nameof(startEndIgnoreLength), startEndIgnoreLength,
				$@"StartEndIgnoreLength {startEndIgnoreLength} >= 0.5 not allowed for AsRatio = 'true'");
		}
	}
}

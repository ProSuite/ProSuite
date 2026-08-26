using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Surface
{
	/// <summary>
	/// Tests the catalog-based <see cref="SimpleRasterMosaic"/>, i.e. a polygon feature class with
	/// one tile per raster file and a file-path field. Such a catalog can legitimately contain
	/// tiles without a file (e.g. the archive tile index lists every tile, whether a file has been
	/// delivered or not); those tiles must be skipped rather than fail.
	/// </summary>
	[TestFixture]
	public class SimpleRasterMosaicTest
	{
		private const string _pathFieldName = "RASTER";
		private const string _zOrderFieldName = "ZORDER";

		private string _rasterPath;
		private IEnvelope _rasterExtent;
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			TestUtils.InitializeLicense();

			_rasterPath = TestData.GetTiffDtm();

			IRasterDataset rasterDataset = DatasetUtils.OpenRasterDataset(_rasterPath);

			var geoDataset = (IGeoDataset) rasterDataset;

			_rasterExtent = geoDataset.Extent;
			_spatialReference = geoDataset.SpatialReference;
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void Can_get_raster_of_catalog_tile()
		{
			IFeatureClass catalog = CreateCatalog(
				nameof(Can_get_raster_of_catalog_tile),
				new CatalogTile(_rasterExtent, _rasterPath, zOrder: 1));

			SimpleRasterMosaic mosaic = CreateMosaic(catalog);

			List<ISimpleRaster> rasters = mosaic.GetSimpleRasters(null).ToList();

			Assert.AreEqual(1, rasters.Count);

			ISimpleRaster raster = mosaic.GetSimpleRaster(CenterX(_rasterExtent),
			                                              CenterY(_rasterExtent));

			Assert.NotNull(raster, "No raster at the center of the tile");
			Assert.AreEqual(_rasterPath, ((SimpleAoRaster) raster).Path);
		}

		[Test]
		public void Catalog_tiles_without_file_are_skipped()
		{
			// One tile references the raster file, two do not: a null path and an empty path.
			IFeatureClass catalog = CreateCatalog(
				nameof(Catalog_tiles_without_file_are_skipped),
				new CatalogTile(_rasterExtent, _rasterPath, zOrder: 1),
				new CatalogTile(NeighbourTile(_rasterExtent), null, zOrder: 2),
				new CatalogTile(NeighbourTile(_rasterExtent, 2), string.Empty, zOrder: 3));

			SimpleRasterMosaic mosaic = CreateMosaic(catalog);

			List<ISimpleRaster> rasters = mosaic.GetSimpleRasters(null).ToList();

			Assert.AreEqual(1, rasters.Count, "Tiles without a file were not skipped");
			Assert.AreEqual(_rasterPath, ((SimpleAoRaster) rasters[0]).Path);
		}

		[Test]
		public void No_raster_at_location_of_tile_without_file()
		{
			IEnvelope emptyTile = NeighbourTile(_rasterExtent);

			IFeatureClass catalog = CreateCatalog(
				nameof(No_raster_at_location_of_tile_without_file),
				new CatalogTile(_rasterExtent, _rasterPath, zOrder: 1),
				new CatalogTile(emptyTile, null, zOrder: 2));

			SimpleRasterMosaic mosaic = CreateMosaic(catalog);

			Assert.IsNull(mosaic.GetSimpleRaster(CenterX(emptyTile), CenterY(emptyTile)));

			Assert.IsEmpty(mosaic.GetSimpleRasters(emptyTile).ToList());
		}

		[Test]
		public void Overlapping_tile_without_file_does_not_hide_the_raster()
		{
			// The tile without a file comes first in the Z-order, at the same location as the tile
			// that has one. The mosaic must move on to the next tile, not report 'no raster'.
			IFeatureClass catalog = CreateCatalog(
				nameof(Overlapping_tile_without_file_does_not_hide_the_raster),
				new CatalogTile(_rasterExtent, null, zOrder: 1),
				new CatalogTile(_rasterExtent, _rasterPath, zOrder: 2));

			SimpleRasterMosaic mosaic = CreateMosaic(catalog);

			ISimpleRaster raster = mosaic.GetSimpleRaster(CenterX(_rasterExtent),
			                                              CenterY(_rasterExtent));

			Assert.NotNull(raster, "The empty tile hides the raster of the overlapping tile");
			Assert.AreEqual(_rasterPath, ((SimpleAoRaster) raster).Path);
		}

		#region Test setup

		private SimpleRasterMosaic CreateMosaic([NotNull] IFeatureClass catalog)
		{
			return new SimpleRasterMosaic("catalog", catalog, null, _zOrderFieldName, false,
			                              _pathFieldName, null);
		}

		[NotNull]
		private IFeatureClass CreateCatalog([NotNull] string name,
		                                    [NotNull] params CatalogTile[] tiles)
		{
			IFeatureWorkspace workspace = TestWorkspaceUtils.CreateInMemoryWorkspace(name);

			IFeatureClass result = DatasetUtils.CreateSimpleFeatureClass(
				workspace, name, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
				                            _spatialReference),
				FieldUtils.CreateTextField(_pathFieldName, 500),
				FieldUtils.CreateIntegerField(_zOrderFieldName));

			int pathFieldIndex = result.FindField(_pathFieldName);
			int zOrderFieldIndex = result.FindField(_zOrderFieldName);

			foreach (CatalogTile tile in tiles)
			{
				IFeature feature = result.CreateFeature();

				feature.Shape = GeometryFactory.CreatePolygon(tile.Extent);
				feature.Value[pathFieldIndex] = (object) tile.Path ?? DBNull.Value;
				feature.Value[zOrderFieldIndex] = tile.ZOrder;

				feature.Store();
			}

			return result;
		}

		/// <summary>
		/// An extent of the same size as the raster, offset to the east by the given number of
		/// tiles, i.e. neither covered by nor touching the raster.
		/// </summary>
		[NotNull]
		private static IEnvelope NeighbourTile([NotNull] IEnvelope extent, int tileCount = 1)
		{
			IEnvelope result = GeometryFactory.Clone(extent);

			// Twice the width, to keep a gap: a spatial filter also finds touching tiles.
			result.Offset(tileCount * 2 * extent.Width, 0);

			return result;
		}

		private static double CenterX([NotNull] IEnvelope extent)
		{
			return (extent.XMin + extent.XMax) / 2;
		}

		private static double CenterY([NotNull] IEnvelope extent)
		{
			return (extent.YMin + extent.YMax) / 2;
		}

		private class CatalogTile
		{
			public CatalogTile([NotNull] IEnvelope extent, [CanBeNull] string path, int zOrder)
			{
				Extent = extent;
				Path = path;
				ZOrder = zOrder;
			}

			[NotNull]
			public IEnvelope Extent { get; }

			[CanBeNull]
			public string Path { get; }

			public int ZOrder { get; }
		}

		#endregion
	}
}

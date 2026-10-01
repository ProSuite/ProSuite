using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using CommonsTestData = ProSuite.Commons.AO.Test.TestData;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	/// <summary>
	/// End-to-end verification over a raster catalog, i.e. a polygon feature class with one tile
	/// per raster file and a file-path field (the elevation-raster dataset shape). The chain under
	/// test is the one the verification service runs: quality condition -> test factory ->
	/// <see cref="SimpleDatasetOpener"/> -> <see cref="IDatasetContext.OpenSimpleRasterMosaic"/> ->
	/// the catalog branch -> <see cref="MosaicRasterReference"/> -> <see cref="ISimpleSurface"/> ->
	/// the test container.
	/// <para>
	/// The datasets are opened through the model's master database workspace context, which is
	/// where the catalog branch lives; only the data source differs from a real deployment.
	/// </para>
	/// </summary>
	[TestFixture]
	public class RasterCatalogSurfaceTest
	{
		private const string _pathFieldName = "RASTER";
		private const string _catalogName = "catalog";
		private const string _pointClassName = "points";

		/// <summary>The elevation offset given to the test point, in meters.</summary>
		private const double _offset = 10;

		private string _rasterPath;
		private IEnvelope _rasterExtent;
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			TestUtils.InitializeLicense(true);

			_rasterPath = CommonsTestData.GetTiffDtm();

			var geoDataset = (IGeoDataset) DatasetUtils.OpenRasterDataset(_rasterPath);

			_rasterExtent = geoDataset.Extent;
			_spatialReference = geoDataset.SpatialReference;
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void Can_open_catalog_as_surface_via_master_database_context()
		{
			TestSetup setup = CreateSetup(
				nameof(Can_open_catalog_as_surface_via_master_database_context),
				new CatalogTile(_rasterExtent, _rasterPath));

			MosaicRasterReference reference = setup.OpenMosaic();

			Assert.NotNull(reference, "No mosaic opened for the catalog dataset");

			// The interpolation domain is the union of the catalog tiles.
			IEnvelope domain = reference.GeoDataset.Extent;

			Assert.IsFalse(domain.IsEmpty, "Empty interpolation domain");
			Assert.IsTrue(
				domain.XMin <= _rasterExtent.XMin + 1 && domain.XMax >= _rasterExtent.XMax - 1,
				"Interpolation domain does not cover the catalog tile");

			using (ISimpleSurface surface = reference.CreateSurface(_rasterExtent))
			{
				double z = surface.GetZ(CenterX(_rasterExtent), CenterY(_rasterExtent));

				Assert.IsFalse(double.IsNaN(z), "No elevation at the center of the catalog tile");
			}
		}

		[Test]
		public void Cell_size_of_catalog_without_cell_size_field_is_undefined()
		{
			// Pins actual behaviour that the design notes get wrong: with no CellSizeFieldName
			// configured - the first-slice choice for ElevationRasterDataset - the cell size is
			// NOT derived from the rasters, SimpleRasterMosaic.DetermineCellSize simply gives up
			// and GetCellSize reports NaN.
			//
			// That is harmless for a verification: the container reads a raster's cell size only
			// to size its tiles for rasters it assumes in memory, and a catalog mosaic never is.
			// It would bite a future consumer that takes CellSize at face value, so the day the
			// archive's per-tile CURRENT_FILE_PIXEL_SIZE_X is exposed through a cell-size
			// attribute role, this test is the one that should change.
			TestSetup setup = CreateSetup(
				nameof(Cell_size_of_catalog_without_cell_size_field_is_undefined),
				new CatalogTile(_rasterExtent, _rasterPath));

			MosaicRasterReference reference = setup.OpenMosaic();

			Assert.IsTrue(double.IsNaN(reference.CellSize),
			              "Cell size is derived from the rasters after all - see the comment");

			Assert.IsFalse(reference.AssumeInMemory,
			               "A catalog mosaic is assumed in memory, so the container now depends " +
			               "on its (undefined) cell size");
		}

		[Test]
		public void Can_verify_features_against_raster_catalog_surface()
		{
			TestSetup setup = CreateSetup(
				nameof(Can_verify_features_against_raster_catalog_surface),
				new CatalogTile(_rasterExtent, _rasterPath));

			double x = CenterX(_rasterExtent);
			double y = CenterY(_rasterExtent);

			setup.AddPointAboveSurface(x, y, _offset);

			// The point is _offset meters off the surface: within a large limit, outside a small
			// one. Both runs go through the container, i.e. the surface is queried per tile.
			Assert.AreEqual(0, Verify(setup, limit: _offset * 2),
			                "Unexpected issue although the point is within the limit");

			IList<QaError> errors = VerifyGetErrors(setup, limit: _offset / 2);

			Assert.AreEqual(1, errors.Count,
			                "The point off the catalog surface was not reported");
		}

		[Test]
		public void Catalog_tile_without_file_does_not_hide_the_surface()
		{
			// The archive tile index lists every tile, delivered or not: an empty tile stacked over
			// a tile that has a file must not make the elevation disappear during a verification.
			TestSetup setup = CreateSetup(
				nameof(Catalog_tile_without_file_does_not_hide_the_surface),
				new CatalogTile(_rasterExtent, path: null),
				new CatalogTile(_rasterExtent, _rasterPath));

			double x = CenterX(_rasterExtent);
			double y = CenterY(_rasterExtent);

			setup.AddPointAboveSurface(x, y, _offset);

			Assert.AreEqual(0, Verify(setup, limit: _offset * 2),
			                "The tile without a file hid the raster underneath");
		}

		[Test]
		public void Feature_over_catalog_tile_without_file_is_reported()
		{
			// A location covered only by a tile without a file has no elevation at all. That is a
			// legitimate archive state, and it must surface as an issue, not as an exception.
			IEnvelope emptyTile = NeighbourTile(_rasterExtent);

			TestSetup setup = CreateSetup(
				nameof(Feature_over_catalog_tile_without_file_is_reported),
				new CatalogTile(_rasterExtent, _rasterPath),
				new CatalogTile(emptyTile, path: null));

			setup.AddPoint(CenterX(emptyTile), CenterY(emptyTile), z: 1000);

			IList<QaError> errors = VerifyGetErrors(setup, limit: _offset);

			Assert.AreEqual(1, errors.Count,
			                "The feature without an underlying raster was not reported");
		}

		#region Verification

		private static int Verify([NotNull] TestSetup setup, double limit)
		{
			return VerifyGetErrors(setup, limit).Count;
		}

		/// <summary>
		/// Builds a QaSurfaceVertex condition over the catalog dataset, creates the test the way
		/// the verification service does, and runs it through the container.
		/// </summary>
		[NotNull]
		private static IList<QaError> VerifyGetErrors([NotNull] TestSetup setup, double limit)
		{
			// Constructor 4: (featureClass, rasterMosaic, limit, mustBeLarger)
			var testDescriptor = new TestDescriptor(
				"QaSurfaceVertex", new ClassDescriptor(typeof(QaSurfaceVertex)), 4);

			var condition = new QualityCondition("qc_surface_vertex", testDescriptor);

			TestParameterValueUtils.AddParameterValue(condition, "featureClass",
			                                          setup.PointDataset);
			TestParameterValueUtils.AddParameterValue(condition, "rasterMosaic",
			                                          setup.CatalogDataset);
			TestParameterValueUtils.AddParameterValue(condition, "limit", limit);
			TestParameterValueUtils.AddParameterValue(condition, "mustBeLarger", false);

			TestFactory factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.NotNull(factory);

			// This is where the catalog is opened: OpenKnownDatasetType -> OpenSimpleRasterMosaic.
			IList<ITest> tests = factory.CreateTests(
				new SimpleDatasetOpener(setup.Model.GetMasterDatabaseWorkspaceContext()));

			var runner = new QaContainerTestRunner(1000, tests[0]);
			runner.Execute();

			return runner.Errors;
		}

		#endregion

		#region Test setup

		[NotNull]
		private TestSetup CreateSetup([NotNull] string name,
		                              [NotNull] params CatalogTile[] tiles)
		{
			IFeatureWorkspace workspace = TestWorkspaceUtils.CreateInMemoryWorkspace(name);

			IFeatureClass catalogClass = CreateCatalogClass(workspace, tiles);
			IFeatureClass pointClass = CreatePointClass(workspace);

			var model = new SimpleModel(name, catalogClass);

			ModelVectorDataset catalogFeatureClassDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(catalogClass)));

			ModelVectorDataset pointDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(pointClass)));

			// The elevation-raster shape: a dataset that is both a vector dataset and a raster
			// catalog over a tile-index feature class with a file-path field.
			TestRasterCatalogDataset catalogDataset = model.AddDataset(
				new TestRasterCatalogDataset("dtm_catalog", catalogFeatureClassDataset,
				                             _pathFieldName));

			return new TestSetup(model, catalogDataset, pointDataset, pointClass);
		}

		[NotNull]
		private IFeatureClass CreateCatalogClass([NotNull] IFeatureWorkspace workspace,
		                                         [NotNull] IEnumerable<CatalogTile> tiles)
		{
			IFeatureClass result = DatasetUtils.CreateSimpleFeatureClass(
				workspace, _catalogName, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
				                            _spatialReference),
				FieldUtils.CreateTextField(_pathFieldName, 500));

			int pathFieldIndex = result.FindField(_pathFieldName);

			foreach (CatalogTile tile in tiles)
			{
				IFeature feature = result.CreateFeature();

				feature.Shape = GeometryFactory.CreatePolygon(tile.Extent);
				feature.Value[pathFieldIndex] = (object) tile.Path ?? DBNull.Value;

				feature.Store();
			}

			return result;
		}

		[NotNull]
		private IFeatureClass CreatePointClass([NotNull] IFeatureWorkspace workspace)
		{
			return DatasetUtils.CreateSimpleFeatureClass(
				workspace, _pointClassName, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPoint,
				                            _spatialReference, 0D, true));
		}

		/// <summary>
		/// An extent of the same size as the raster, offset to the east: neither covered by nor
		/// touching the raster (a spatial filter also finds touching tiles).
		/// </summary>
		[NotNull]
		private static IEnvelope NeighbourTile([NotNull] IEnvelope extent)
		{
			IEnvelope result = GeometryFactory.Clone(extent);

			result.Offset(2 * extent.Width, 0);

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
			public CatalogTile([NotNull] IEnvelope extent, [CanBeNull] string path)
			{
				Extent = extent;
				Path = path;
			}

			[NotNull]
			public IEnvelope Extent { get; }

			[CanBeNull]
			public string Path { get; }
		}

		/// <summary>
		/// A model whose raster catalog dataset and verified point feature class live in the same
		/// (in-memory) workspace, plus the means to add points relative to the catalog surface.
		/// </summary>
		private class TestSetup
		{
			private readonly IFeatureClass _pointClass;

			public TestSetup([NotNull] SimpleModel model,
			                 [NotNull] TestRasterCatalogDataset catalogDataset,
			                 [NotNull] ModelVectorDataset pointDataset,
			                 [NotNull] IFeatureClass pointClass)
			{
				Model = model;
				CatalogDataset = catalogDataset;
				PointDataset = pointDataset;
				_pointClass = pointClass;
			}

			[NotNull]
			public SimpleModel Model { get; }

			[NotNull]
			public TestRasterCatalogDataset CatalogDataset { get; }

			[NotNull]
			public ModelVectorDataset PointDataset { get; }

			/// <summary>
			/// Opens the catalog dataset as a mosaic the way the verification service does.
			/// </summary>
			[NotNull]
			public MosaicRasterReference OpenMosaic()
			{
				return Model.GetMasterDatabaseWorkspaceContext()
				            .OpenSimpleRasterMosaic(CatalogDataset);
			}

			/// <summary>
			/// Adds a point at the given location, the given distance above the elevation the
			/// catalog surface itself reports there. The test data is thus calibrated against the
			/// surface rather than against hard-coded elevations of the test raster.
			/// </summary>
			public void AddPointAboveSurface(double x, double y, double offset)
			{
				MosaicRasterReference reference = OpenMosaic();

				double surfaceZ;

				using (ISimpleSurface surface =
				       reference.CreateSurface(reference.GeoDataset.Extent))
				{
					surfaceZ = surface.GetZ(x, y);
				}

				Assert.IsFalse(double.IsNaN(surfaceZ),
				               "No elevation at ({0}, {1}) to calibrate the test point against",
				               x, y);

				AddPoint(x, y, surfaceZ + offset);
			}

			public void AddPoint(double x, double y, double z)
			{
				IFeature feature = _pointClass.CreateFeature();

				feature.Shape = GeometryFactory.CreatePoint(
					x, y, z, ((IGeoDataset) _pointClass).SpatialReference);

				feature.Store();
			}
		}

		#endregion
	}
}

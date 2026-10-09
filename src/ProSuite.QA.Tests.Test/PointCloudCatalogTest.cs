using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using LasTestFile = ProSuite.Commons.Test.PointCloud.LasTestFile;
using Path = System.IO.Path;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	/// <summary>
	/// End-to-end verification over a point cloud catalog, i.e. a polygon feature class with one
	/// tile per LAS file and a file-path field (the archive point cloud dataset shape). The chain
	/// under test is the one the verification service runs: quality condition -> test factory ->
	/// <see cref="SimpleDatasetOpener"/> -> <see cref="IDatasetContext.OpenPointCloud"/> -> the
	/// catalog branch -> <see cref="PointCloudReference"/> -> the test container.
	/// <para>
	/// The mirror image of <see cref="RasterCatalogSurfaceTest"/>, and it has the same limits: an
	/// in-memory workspace instead of the unregistered Oracle view, and local files instead of a
	/// UNC share.
	/// </para>
	/// </summary>
	[TestFixture]
	public class PointCloudCatalogTest
	{
		private const string _pathFieldName = "LASFILE";
		private const string _catalogName = "las_catalog";
		private const string _perimeterClassName = "perimeters";

		/// <summary>The Z range the LAS files of the test data declare.</summary>
		private const double _fileMinZ = 400;

		private const double _fileMaxZ = 1500;

		private string _directory;
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			TestUtils.InitializeLicense(true);

			_spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(
				Path.GetTempPath(), $"{nameof(PointCloudCatalogTest)}_{Path.GetRandomFileName()}");

			Directory.CreateDirectory(_directory);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_directory))
			{
				Directory.Delete(_directory, true);
			}
		}

		[Test]
		public void Can_open_catalog_as_point_cloud_via_master_database_context()
		{
			TestSetup setup = CreateSetup(
				nameof(Can_open_catalog_as_point_cloud_via_master_database_context),
				ValidTile(Tile(0)));

			PointCloudReference reference = setup.OpenPointCloud();

			Assert.NotNull(reference, "No point cloud opened for the catalog dataset");
			Assert.AreEqual("las_pointcloud", reference.Name);
			Assert.IsFalse(reference.Extent.IsEmpty, "Empty point cloud extent");
			Assert.AreEqual(1, reference.GetFiles(null).Count());
		}

		[Test]
		public void Can_verify_features_against_point_cloud_files()
		{
			TestSetup setup = CreateSetup(
				nameof(Can_verify_features_against_point_cloud_files),
				ValidTile(Tile(0)), ValidTile(Tile(1)));

			setup.AddPerimeter(Tile(0));

			// The files declare a Z range of 400..1500. Both runs go through the container, i.e.
			// the files are looked up per verified feature.
			Assert.AreEqual(0, Verify(setup, minimumZ: 0, maximumZ: 2000),
			                "Unexpected issue although the declared Z range is within the limits");

			IList<QaError> errors = VerifyGetErrors(setup, minimumZ: 0, maximumZ: 1000);

			Assert.AreEqual(1, errors.Count, "The file exceeding the Z limit was not reported");
			AssertIssueCode(errors[0], "ZRange.TooHigh");
		}

		[Test]
		public void Only_the_files_covering_the_verified_features_are_checked()
		{
			// The point of routing the lookup through the catalog: verifying one perimeter must
			// not read every LAS file of the archive.
			TestSetup setup = CreateSetup(
				nameof(Only_the_files_covering_the_verified_features_are_checked),
				ValidTile(Tile(0)), ValidTile(Tile(1)), ValidTile(Tile(2)));

			setup.AddPerimeter(Tile(1));

			IList<QaError> errors = VerifyGetErrors(setup, minimumZ: 0, maximumZ: 1000);

			Assert.AreEqual(1, errors.Count,
			                "Expected exactly the file of the covered tile to be reported");
		}

		[Test]
		public void Each_file_is_reported_once_however_many_features_it_covers()
		{
			TestSetup setup = CreateSetup(
				nameof(Each_file_is_reported_once_however_many_features_it_covers),
				ValidTile(Tile(0)));

			// Three perimeters over the same tile: the file, not the feature, is what is wrong.
			setup.AddPerimeter(Tile(0));
			setup.AddPerimeter(Tile(0));
			setup.AddPerimeter(Tile(0));

			IList<QaError> errors = VerifyGetErrors(setup, minimumZ: 0, maximumZ: 1000);

			Assert.AreEqual(1, errors.Count, "The same file was reported more than once");
		}

		[Test]
		public void Missing_las_file_is_reported()
		{
			// The catalog row references a file that is not there - a delivery that went missing,
			// as opposed to a tile that was never delivered.
			TestSetup setup = CreateSetup(
				nameof(Missing_las_file_is_reported),
				new CatalogTile(Tile(0), Path.Combine(_directory, "gone.las")));

			setup.AddPerimeter(Tile(0));

			IList<QaError> errors = VerifyGetErrors(setup, minimumZ: 0, maximumZ: 2000);

			Assert.AreEqual(1, errors.Count, "The missing LAS file was not reported");
			AssertIssueCode(errors[0], "NoLasFile");
		}

		[Test]
		public void Unreadable_las_header_is_reported_not_thrown()
		{
			TestSetup setup = CreateSetup(
				nameof(Unreadable_las_header_is_reported_not_thrown),
				new CatalogTile(Tile(0),
				                LasTestFile.WriteTruncated(GetPath("truncated.las"))));

			setup.AddPerimeter(Tile(0));

			IList<QaError> errors = VerifyGetErrors(setup, minimumZ: 0, maximumZ: 2000);

			Assert.AreEqual(1, errors.Count, "The unreadable LAS file was not reported");
			AssertIssueCode(errors[0], "InvalidLasHeader");
		}

		[Test]
		public void Catalog_tile_without_a_file_is_skipped()
		{
			// The archive tile index lists every registered tile, delivered or not. A tile with no
			// file at all is a legitimate state and must not be reported as a missing file: there
			// is nothing to check.
			TestSetup setup = CreateSetup(
				nameof(Catalog_tile_without_a_file_is_skipped),
				new CatalogTile(Tile(0), path: null));

			setup.AddPerimeter(Tile(0));

			Assert.AreEqual(0, Verify(setup, minimumZ: 0, maximumZ: 2000),
			                "A tile without a file was reported");
		}

		#region Verification

		private static int Verify([NotNull] TestSetup setup, double minimumZ, double maximumZ)
		{
			return VerifyGetErrors(setup, minimumZ, maximumZ).Count;
		}

		/// <summary>
		/// Builds a QaLasFileZRange condition over the catalog dataset, creates the test the way
		/// the verification service does, and runs it through the container.
		/// </summary>
		[NotNull]
		private static IList<QaError> VerifyGetErrors([NotNull] TestSetup setup,
		                                              double minimumZ, double maximumZ)
		{
			var testDescriptor = new TestDescriptor(
				"QaLasFileZRange", new ClassDescriptor(typeof(QaLasFileZRange)), 0);

			var condition = new QualityCondition("qc_las_file_z_range", testDescriptor);

			TestParameterValueUtils.AddParameterValue(condition, "perimeterClass",
			                                          setup.PerimeterDataset);
			TestParameterValueUtils.AddParameterValue(condition, "pointCloud",
			                                          setup.CatalogDataset);
			TestParameterValueUtils.AddParameterValue(condition, "minimumZ", minimumZ);
			TestParameterValueUtils.AddParameterValue(condition, "maximumZ", maximumZ);

			TestFactory factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.NotNull(factory);

			// This is where the catalog is opened: OpenKnownDatasetType -> OpenPointCloud.
			IList<ITest> tests = factory.CreateTests(
				new SimpleDatasetOpener(setup.Model.GetMasterDatabaseWorkspaceContext()));

			var runner = new QaContainerTestRunner(10000, tests[0]);
			runner.Execute();

			return runner.Errors;
		}

		private static void AssertIssueCode([NotNull] QaError error, [NotNull] string expected)
		{
			Assert.NotNull(error.IssueCode, "No issue code: {0}", error.Description);
			Assert.AreEqual($"LasFileZRange.{expected}", error.IssueCode.ID, error.Description);
		}

		#endregion

		#region Test setup

		[NotNull]
		private TestSetup CreateSetup([NotNull] string name,
		                              [NotNull] params CatalogTile[] tiles)
		{
			IFeatureWorkspace workspace = TestWorkspaceUtils.CreateInMemoryWorkspace(name);

			IFeatureClass catalogClass = CreateCatalogClass(workspace, tiles);
			IFeatureClass perimeterClass = CreatePerimeterClass(workspace);

			var model = new SimpleModel(name, catalogClass);

			ModelVectorDataset catalogFeatureClassDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(catalogClass)));

			ModelVectorDataset perimeterDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(perimeterClass)));

			// The archive point cloud shape: a dataset that is both a vector dataset and a point
			// cloud catalog over a tile-index feature class with a file-path field.
			TestPointCloudCatalogDataset catalogDataset = model.AddDataset(
				new TestPointCloudCatalogDataset("las_pointcloud", catalogFeatureClassDataset,
				                                 _pathFieldName));

			return new TestSetup(model, catalogDataset, perimeterDataset, perimeterClass);
		}

		/// <summary>The extent of tile number <paramref name="index"/>, west to east.</summary>
		[NotNull]
		private IEnvelope Tile(int index)
		{
			double xMin = 2600000 + index * 1000;

			return GeometryFactory.CreateEnvelope(xMin, 1200000, xMin + 1000, 1201000,
			                                      _spatialReference);
		}

		/// <summary>A catalog tile whose LAS file exists and declares the tile's own extent.</summary>
		[NotNull]
		private CatalogTile ValidTile([NotNull] IEnvelope extent)
		{
			string path = GetPath($"tile_{extent.XMin:F0}.las");

			LasTestFile.Write(path, extent.XMin, extent.YMin, extent.XMax, extent.YMax,
			                  _fileMinZ, _fileMaxZ);

			return new CatalogTile(extent, path);
		}

		[NotNull]
		private string GetPath([NotNull] string fileName)
		{
			return Path.Combine(_directory, fileName);
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
		private IFeatureClass CreatePerimeterClass([NotNull] IFeatureWorkspace workspace)
		{
			return DatasetUtils.CreateSimpleFeatureClass(
				workspace, _perimeterClassName, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
				                            _spatialReference));
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
		/// A model whose point cloud catalog dataset and verified perimeter feature class live in
		/// the same (in-memory) workspace.
		/// </summary>
		private class TestSetup
		{
			private readonly IFeatureClass _perimeterClass;

			public TestSetup([NotNull] SimpleModel model,
			                 [NotNull] TestPointCloudCatalogDataset catalogDataset,
			                 [NotNull] ModelVectorDataset perimeterDataset,
			                 [NotNull] IFeatureClass perimeterClass)
			{
				Model = model;
				CatalogDataset = catalogDataset;
				PerimeterDataset = perimeterDataset;
				_perimeterClass = perimeterClass;
			}

			[NotNull]
			public SimpleModel Model { get; }

			[NotNull]
			public TestPointCloudCatalogDataset CatalogDataset { get; }

			[NotNull]
			public ModelVectorDataset PerimeterDataset { get; }

			/// <summary>
			/// Opens the catalog dataset as a point cloud the way the verification service does.
			/// </summary>
			[NotNull]
			public PointCloudReference OpenPointCloud()
			{
				return Model.GetMasterDatabaseWorkspaceContext().OpenPointCloud(CatalogDataset);
			}

			/// <summary>
			/// Adds a perimeter feature covering the inner part of the given tile, inset so that
			/// it cannot touch the neighbouring tiles (a spatial filter also finds those).
			/// </summary>
			public void AddPerimeter([NotNull] IEnvelope tile)
			{
				IEnvelope inner = GeometryFactory.Clone(tile);
				inner.Expand(0.5, 0.5, true);

				IFeature feature = _perimeterClass.CreateFeature();

				feature.Shape = GeometryFactory.CreatePolygon(inner);

				feature.Store();
			}
		}

		#endregion
	}
}

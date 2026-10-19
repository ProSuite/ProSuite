using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using LasTestFile = ProSuite.Commons.Test.PointCloud.LasTestFile;
using Path = System.IO.Path;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	/// <summary>
	/// The shared seam that turns a point cloud catalog dataset - a polygon feature class with one
	/// feature per LAS tile and a file-path field - into the reference a quality test consumes.
	/// The mirror image of <see cref="CreateRasterCatalogMosaicTest"/>.
	/// </summary>
	[TestFixture]
	public class CreatePointCloudCatalogTest
	{
		private const string _pathFieldName = "LASFILE";

		private string _directory;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(true);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(
				Path.GetTempPath(),
				$"{nameof(CreatePointCloudCatalogTest)}_{Path.GetRandomFileName()}");

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
		public void Can_build_point_cloud_reference_from_polygon_catalog_feature_class()
		{
			var testModel = new TestDataModel(
				nameof(Can_build_point_cloud_reference_from_polygon_catalog_feature_class));

			// A polygon feature class acting as the LAS tile index (the archive point cloud idea):
			VectorDataset catalogFeatureClassDataset = testModel.GetPolygonDataset();
			Assert.NotNull(catalogFeatureClassDataset);

			var catalogDataset = new TestPointCloudCatalogDataset(
				"pointcloud", catalogFeatureClassDataset, _pathFieldName);

			IVectorDataset openedDataset = null;

			IFeatureClass Opener(IVectorDataset dataset)
			{
				openedDataset = dataset;
				return testModel.OpenFeatureClass(dataset);
			}

			PointCloudReference reference =
				ModelElementUtils.CreatePointCloudCatalog(catalogDataset, Opener);

			Assert.NotNull(reference, "No point cloud reference created");

			// The seam must never open the catalog itself: that is what lets the master database,
			// a simple workspace, a replica and the test contexts each supply their own opener.
			Assert.AreSame(catalogFeatureClassDataset, openedDataset,
			               "Catalog feature class was not opened via the supplied delegate");

			Assert.AreEqual(DatasetType.PointCloud, reference.DatasetType);
			Assert.AreEqual("pointcloud", reference.Name);
			Assert.NotNull(reference.SpatialReference);
			Assert.NotNull(reference.DbContainer);
		}

		[Test]
		public void Point_cloud_reference_returns_the_files_of_the_catalog()
		{
			IFeatureWorkspace workspace = TestWorkspaceUtils.CreateInMemoryWorkspace(
				nameof(Point_cloud_reference_returns_the_files_of_the_catalog));

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IEnvelope westTile = GeometryFactory.CreateEnvelope(
				2600000, 1200000, 2601000, 1201000, sr);
			IEnvelope eastTile = GeometryFactory.CreateEnvelope(
				2602000, 1200000, 2603000, 1201000, sr);

			string westFile = WriteLasFile("west.las", westTile, 400, 500);
			string eastFile = WriteLasFile("east.las", eastTile, 400, 500);

			IFeatureClass catalogClass = CreateCatalogClass(
				workspace, sr,
				(westTile, westFile),
				(eastTile, eastFile),
				// A registered tile whose file has not been delivered yet: a legitimate archive
				// state, and it must simply not show up among the files.
				(GeometryFactory.CreateEnvelope(2604000, 1200000, 2605000, 1201000, sr), null));

			PointCloudReference reference = CreateReference(catalogClass);

			Assert.AreEqual(new[] { eastFile, westFile }, GetPaths(reference, searchArea: null),
			                "All files of the catalog");

			Assert.AreEqual(new[] { westFile }, GetPaths(reference, westTile),
			                "Only the file of the tile intersecting the search area");

			(string _, IEnvelope tileExtent) = reference.GetFiles(westTile).Single();

			Assert.AreEqual(westTile.XMin, tileExtent.XMin, 0.001);
			Assert.AreEqual(westTile.YMax, tileExtent.YMax, 0.001);
		}

		[Test]
		public void Missing_file_path_field_is_a_configuration_error()
		{
			// The field name comes from the dataset's FilePath attribute role. If it does not
			// match the catalog, fail where the cause is rather than silently finding no files.
			var testModel = new TestDataModel(nameof(Missing_file_path_field_is_a_configuration_error));

			var catalogDataset = new TestPointCloudCatalogDataset(
				"pointcloud", testModel.GetPolygonDataset(), "NO_SUCH_FIELD");

			Assert.Throws<Commons.Exceptions.InvalidConfigurationException>(
				() => ModelElementUtils.CreatePointCloudCatalog(
					catalogDataset, testModel.OpenFeatureClass));
		}

		#region Test setup

		private static PointCloudReference CreateReference(IFeatureClass catalogClass)
		{
			var model = new SimpleModel("model", catalogClass);

			ModelVectorDataset catalogFeatureClassDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(catalogClass)));

			var catalogDataset = new TestPointCloudCatalogDataset(
				"pointcloud", catalogFeatureClassDataset, _pathFieldName);

			return model.GetMasterDatabaseWorkspaceContext().OpenPointCloud(catalogDataset);
		}

		private static IEnumerable<string> GetPaths(PointCloudReference reference,
		                                            IEnvelope searchArea)
		{
			return reference.GetFiles(searchArea).Select(file => file.FilePath).OrderBy(p => p);
		}

		private string WriteLasFile(string fileName, IEnvelope extent, double minZ, double maxZ)
		{
			return LasTestFile.Write(Path.Combine(_directory, fileName),
			                         extent.XMin, extent.YMin, extent.XMax, extent.YMax,
			                         minZ, maxZ);
		}

		private static IFeatureClass CreateCatalogClass(
			IFeatureWorkspace workspace,
			ISpatialReference spatialReference,
			params (IEnvelope Extent, string Path)[] tiles)
		{
			IFeatureClass result = DatasetUtils.CreateSimpleFeatureClass(
				workspace, "las_catalog", null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
				                            spatialReference),
				FieldUtils.CreateTextField(_pathFieldName, 500));

			int pathFieldIndex = result.FindField(_pathFieldName);

			foreach ((IEnvelope extent, string path) in tiles)
			{
				IFeature feature = result.CreateFeature();

				feature.Shape = GeometryFactory.CreatePolygon(extent);
				feature.Value[pathFieldIndex] = (object) path ?? System.DBNull.Value;

				feature.Store();
			}

			return result;
		}

		#endregion
	}
}

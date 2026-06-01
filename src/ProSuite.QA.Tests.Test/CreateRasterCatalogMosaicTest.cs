using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Tests.Test.TestData;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class CreateRasterCatalogMosaicTest
	{
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

		[Test]
		public void Can_build_mosaic_reference_from_polygon_catalog_feature_class()
		{
			var testModel = new TestDataModel(nameof(CreateRasterCatalogMosaicTest));

			// A polygon feature class acting as the raster catalog (the elevation-raster idea):
			VectorDataset catalogFeatureClassDataset = testModel.GetPolygonDataset();
			Assert.NotNull(catalogFeatureClassDataset);

			var catalogDataset = new TestRasterCatalogDataset(
				"mosaic", catalogFeatureClassDataset, filePathFieldName: "RASTER");

			IVectorDataset openedDataset = null;

			IFeatureClass Opener(IVectorDataset dataset)
			{
				openedDataset = dataset;
				return testModel.OpenFeatureClass(dataset);
			}

			MosaicRasterReference reference =
				ModelElementUtils.CreateRasterCatalogMosaic(catalogDataset, Opener);

			Assert.NotNull(reference, "No mosaic reference created");
			Assert.AreSame(catalogFeatureClassDataset, openedDataset,
			               "Catalog feature class was not opened via the supplied delegate");
			Assert.AreEqual(DatasetType.RasterMosaic, reference.DatasetType);
			Assert.AreEqual("mosaic", reference.Name);
			Assert.NotNull(reference.Dataset);
			Assert.NotNull(reference.GeoDataset);
		}
	}
}

using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.Test.QA.VerifiedDataModel
{
	[TestFixture]
	public class VerifiedModelFactoryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnitTestLogging();

			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanHarvestSimpleModel()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			VerifiedModelFactory modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			// NOTE: Harvesting the attributes is sometimes just as fast as with the (cached)
			// MasterDatabaseWorkspaceContextEx (ca. 30s) but sometimes extremely slow!
			modelFactory.HarvestAttributes = true;
			modelFactory.HarvestObjectTypes = true;

			Model model = modelFactory.CreateModel(workspace, "TestTLM", null, null, "TOPGIS_TLM");

			int tableCount = model.GetDatasets<TableDataset>().Count;
			int vectorCount = model.GetDatasets<VectorDataset>().Count;
			int topologyCount = model.GetDatasets<TopologyDataset>().Count;
			int simpleRasterMosaicCount = model.GetDatasets<RasterMosaicDataset>().Count;

			Assert.Greater(tableCount, 0);
			Assert.Greater(vectorCount, 0);
			Assert.Greater(topologyCount, 0);
			Assert.Greater(simpleRasterMosaicCount, 0);

			Console.WriteLine("Vector datasets: {0}", vectorCount);
			Console.WriteLine("Table datasets: {0}", tableCount);
			Console.WriteLine("Table datasets: {0}", topologyCount);
			Console.WriteLine("Simple raster mosaic datasets: {0}", simpleRasterMosaicCount);

			foreach (var objectDataset in model.GetDatasets<ObjectDataset>())
			{
				Assert.IsTrue(objectDataset.Attributes.Count > 0);
			}
		}
	}
}

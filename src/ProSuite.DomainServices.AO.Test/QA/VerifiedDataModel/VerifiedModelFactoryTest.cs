using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.Test.QA.VerifiedDataModel
{
	[TestFixture]
	public class VerifiedModelFactoryTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnittestLogging();

			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanHarvestSimpleModel()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			VerifiedModelFactory modelFactory =
				new VerifiedModelFactory(CreateWorkspaceContextSimple,
				                         new SimpleVerifiedDatasetHarvester());

			// NOTE: Harvesting the attributes is sometimes just as fast as with the (cached)
			// MasterDatabaseWorkspaceContextEx (ca. 30s) but sometimes extremely slow!
			modelFactory.HarvestAttributes = true;
			modelFactory.HarvestObjectTypes = true;

			Model model = modelFactory.CreateModel(workspace, "TestTLM", null, null, "TOPGIS_TLM");

			int tableCount = model.GetDatasets<TableDataset>().Count;
			int vectorCount = model.GetDatasets<VectorDataset>().Count;
			int simpleRasterMosaicCount = model.GetDatasets<RasterMosaicDataset>().Count;

			Assert.Greater(tableCount, 0);
			Assert.Greater(vectorCount, 0);
			Assert.Greater(simpleRasterMosaicCount, 0);

			Console.WriteLine("Vector datasets: {0}", vectorCount);
			Console.WriteLine("Table datasets: {0}", tableCount);
			Console.WriteLine("Simple raster mosaic datasets: {0}", simpleRasterMosaicCount);

			foreach (var objectDataset in model.GetDatasets<ObjectDataset>())
			{
				Assert.IsTrue(objectDataset.Attributes.Count > 0);
			}
		}

		private static IWorkspaceContext CreateWorkspaceContextSimple(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace)
		{
			return new MasterDatabaseWorkspaceContext(workspace, model);
		}
	}
}

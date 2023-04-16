using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
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
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

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

			Model model = modelFactory.CreateModel(workspace, "TestTLM", null, "TOPGIS_TLM");

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

		[Test]
		[Category(TestCategory.Sde)]
		public void CanHarvestUsedSimpleModel()
		{
			IWorkspace userWorkspace = TestUtils.OpenUserWorkspaceOracle();
			IList<string> qualifiedUsedDatasetNames =
				new[]
				{
					"TOPGIS_TLM.TLM_STRASSE",
					"TOPGIS_TLM.TLM_STRASSEN_NAME",
					"TOPGIS_TLM.TLM_STRASSEN_TOPO",
					"TOPGIS_TLM.TLM_DTM_MOSAIC"
				};
			IList<string> unqualifiedUsedDatasetNames =
				new[]
				{
					"TLM_STRASSE",
					"TLM_STRASSEN_NAME",
					"TLM_STRASSEN_TOPO",
					"TLM_DTM_MOSAIC"
				};

			{
				Model model = CanHarvestUsedSimpleModel(userWorkspace, qualifiedUsedDatasetNames);
				Assert.AreEqual(1, model.GetDatasets<TopologyDataset>().Count);
				Assert.AreEqual(1, model.GetDatasets<RasterMosaicDataset>().Count);
			}

			{
				Model model = CanHarvestUsedSimpleModel(userWorkspace, unqualifiedUsedDatasetNames);
				Assert.AreEqual(1, model.GetDatasets<TopologyDataset>().Count);
				Assert.AreEqual(1, model.GetDatasets<RasterMosaicDataset>().Count);
			}

			IFeatureWorkspace fdgbWorkspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("HarvestUsedDatasets");

			TestWorkspaceUtils.CreateSimpleFeatureClass(
				fdgbWorkspace, "TLM_STRASSE", FieldUtils.CreateFields(FieldUtils.CreateOIDField()),
				esriGeometryType.esriGeometryLine, esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			TestWorkspaceUtils.CreateSimpleTable(
				fdgbWorkspace, "TLM_STRASSEN_NAME",
				FieldUtils.CreateFields(FieldUtils.CreateOIDField()));

			{
				var ws = (IWorkspace)fdgbWorkspace;
				CanHarvestUsedSimpleModel(ws, qualifiedUsedDatasetNames);
			}
			{
				var ws = (IWorkspace)fdgbWorkspace;
				CanHarvestUsedSimpleModel(ws, unqualifiedUsedDatasetNames);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void ComparePerformance()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			VerifiedModelFactory modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			// NOTE: Harvesting the attributes is sometimes just as fast as with the (cached)
			modelFactory.HarvestAttributes = false;
			modelFactory.HarvestObjectTypes = false;

			var w = Stopwatch.StartNew();
			Model model = modelFactory.CreateModel(workspace, "TestTLM", null, "TOPGIS_TLM");
			w.Stop();

			List<string> datasetNames = new List<string>();

			datasetNames.AddRange(model.GetDatasets<TableDataset>().Select(x => x.Name));
			datasetNames.AddRange(model.GetDatasets<VectorDataset>().Select(x => x.Name));
			datasetNames.AddRange(model.GetDatasets<TopologyDataset>().Select(x => x.Name));
			datasetNames.AddRange(model.GetDatasets<RasterMosaicDataset>().Select(x => x.Name));

			Console.WriteLine($"Full Model Creation: {w.ElapsedMilliseconds / 1000.0:N3} ms");

			w.Restart();
			Model usedModel = modelFactory.CreateModel(
				workspace, "TestTLM", null, "TOPGIS_TLM",
				usedDatasetNames: datasetNames);
			w.Stop();
			Console.WriteLine($"Used Model Creation: {w.ElapsedMilliseconds / 1000.0:N3} ms");
			Assert.NotNull(usedModel);

		}

		private Model CanHarvestUsedSimpleModel(IWorkspace workspace, IList<string> usedDatasetNames)
		{
			VerifiedModelFactory modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			modelFactory.HarvestAttributes = true;
			modelFactory.HarvestObjectTypes = true;


			Model model = modelFactory.CreateModel(workspace, "TestTLM", null, "TOPGIS_TLM",
			                                       usedDatasetNames: usedDatasetNames);

			int tableCount = model.GetDatasets<TableDataset>().Count;
			int vectorCount = model.GetDatasets<VectorDataset>().Count;

			Assert.AreEqual(tableCount, 1);
			Assert.AreEqual(vectorCount, 1);

			foreach (var objectDataset in model.GetDatasets<ObjectDataset>())
			{
				Assert.IsTrue(objectDataset.Attributes.Count > 0);
			}
			return model;
		}



	}
}

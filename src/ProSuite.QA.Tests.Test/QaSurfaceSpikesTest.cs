using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSurfaceSpikesTest
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

		//[Test]
		//[Ignore("requires connection to TOPGIST")]
		//public void TestDtm()
		//{
		//	IWorkspace dtmWs = TestDataUtils.OpenTopgisTlm();
		//	ITerrain terrain = TerrainUtils.OpenTerrain((IFeatureWorkspace) dtmWs,
		//	                                            "TOPGIS_TLM.TLM_DTM_TERRAIN",
		//	                                            "TOPGIS_TLM.TLM_DTM");

		//	var test = new QaSurfaceSpikes(terrain, 0.01, 70, 5);
		//	var runner = new QaContainerTestRunner(10000, test) {KeepGeometry = true};

		//	IFeatureWorkspace ws =
		//		TestWorkspaceUtils.CreateTestFgdbWorkspace("TestDtm");
		//	var logger = new SimpleErrorWorkspace(ws);
		//	runner.TestContainer.QaError += logger.TestContainer_QaError;

		//	const double xMin = 2750000;
		//	const double yMin = 1234000;
		//	IEnvelope box = GeometryFactory.CreateEnvelope(xMin, yMin, xMin + 1500, yMin + 1500);
		//	runner.Execute(box);

		//	Console.WriteLine(runner.Errors.Count);
		//}

		//[Test]
		//[Ignore("requires connection to TOPGIST")]
		//public void TestDtmSmallExtent()
		//{
		//	IWorkspace dtmWs = TestDataUtils.OpenTopgisTlm();
		//	ITerrain terrain = TerrainUtils.OpenTerrain((IFeatureWorkspace) dtmWs,
		//	                                            "TOPGIS_TLM.TLM_DTM_TERRAIN",
		//	                                            "TOPGIS_TLM.TLM_DTM");

		//	var test = new QaSurfaceSpikes(terrain, 0.01, 70, 5);
		//	var runner = new QaContainerTestRunner(10000, test) {KeepGeometry = true};

		//	const double xMin = 2751000;
		//	const double yMin = 1238000;
		//	IEnvelope box = GeometryFactory.CreateEnvelope(xMin, yMin, xMin + 100, yMin + 100);
		//	runner.Execute(box);

		//	Console.WriteLine(runner.Errors.Count);
		//}

		//[Test]
		//[Ignore("requires connection to TOPGIST")]
		//public void TestDtmFull()
		//{
		//	IWorkspace dtmWs = TestDataUtils.OpenTopgisTlm();
		//	ITerrain terrain = TerrainUtils.OpenTerrain((IFeatureWorkspace) dtmWs,
		//	                                            "TOPGIS_TLM.TLM_DTM_TERRAIN",
		//	                                            "TOPGIS_TLM.TLM_DTM");

		//	var test = new QaSurfaceSpikes(terrain, 0.01, 70, 5);
		//	var runner = new QaContainerTestRunner(10000, test) {KeepGeometry = true};

		//	runner.Execute();

		//	Console.WriteLine(runner.Errors.Count);
		//}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanRunFromCondition()
		{
			XmlSimpleTerrainDataset tds =
				new XmlSimpleTerrainDataset
				{
					Name = "TestSimpleTerrainDs",
					PointDensity = 7.8125,
					Sources =
						new List<XmlTerrainSource>
						{
							new XmlTerrainSource
							{
								Dataset = "TOPGIS_TLM.TLM_DTM_MASSENPUNKTE",
								Type = TinSurfaceType.MassPoint
							},
							new XmlTerrainSource
							{
								Dataset = "TOPGIS_TLM.TLM_DTM_BRUCHKANTE",
								Type = TinSurfaceType.HardLine
							}
						}
				};

			XmlSerializer serializer = new XmlSerializer(typeof(XmlSimpleTerrainDataset));
			StringBuilder sb = new StringBuilder();
			using (StringWriter xmlWriter = new StringWriter(sb))
			{
				serializer.Serialize(xmlWriter, tds);
			}

			IWorkspace dtmWs = TestDataUtils.OpenTopgisAlti();

			var model = new SimpleModel("model", dtmWs);

			var masspts = new ModelVectorDataset("TOPGIS_TLM.TLM_DTM_MASSENPUNKTE");
			var breaklns = new ModelVectorDataset("TOPGIS_TLM.TLM_DTM_BRUCHKANTE");

			model.AddDataset(masspts);
			model.AddDataset(breaklns);

			var ts1 = new TerrainSourceDataset(masspts, TinSurfaceType.MassPoint);
			var ts2 = new TerrainSourceDataset(breaklns, TinSurfaceType.HardLine);

			var simpleTerrainDataset = new ModelSimpleTerrainDataset(
				                           "TestSimpleTerrainDs", new[] { ts1, ts2 })
			                           {
				                           Abbreviation = "DTM",
				                           PointDensity = 7.8125
			                           };
			simpleTerrainDataset.Model = model;

			SimpleTerrainDataset terrainDatasetFromXml =
				ModelSimpleTerrainDataset.Create(
					sb.ToString(), model, (n) => model.GetDatasetByModelName(n) as VectorDataset);

			Assert.IsTrue(simpleTerrainDataset.Equals(terrainDatasetFromXml));

			Dataset mds1 = model.AddDataset(terrainDatasetFromXml);

			var clsDesc = new ClassDescriptor(typeof(QaSurfaceSpikes));
			var tstDesc = new TestDescriptor("QaSurfaceSpikes", clsDesc, testConstructorId: 0);
			var condition = new QualityCondition("QaSurfaceSpikes", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "terrain", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "terrainTolerance", 0.1);
			InstanceConfigurationUtils.AddParameterValue(condition, "maxSlopeDegrees", 30);
			InstanceConfigurationUtils.AddParameterValue(condition, "maxDeltaZ", 10);

			var fact = new DefaultTestFactory(typeof(QaSurfaceSpikes));
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));
			Assert.AreEqual(1, tests.Count);

			var runner = new QaContainerTestRunner(10000, tests[0]) { KeepGeometry = true };

			const double xMin = 2700000;
			const double yMin = 1255000;
			IEnvelope box = GeometryFactory.CreateEnvelope(xMin, yMin, xMin + 100, yMin + 100);
			int errorCount = runner.Execute(box);

			Assert.AreEqual(0, errorCount);

			// Can we load the tests again, now that the simple terrain is part of the model datasets?

			tests =
				fact.CreateTests(new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));
			Assert.AreEqual(1, tests.Count);
		}
	}
}

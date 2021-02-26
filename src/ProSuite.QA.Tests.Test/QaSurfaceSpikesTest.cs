using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSurfaceSpikesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor, EsriExtension.ThreeDAnalyst);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
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
		public void TestRelatedFactoryMinParameters()
		{
			IWorkspace dtmWs = TestDataUtils.OpenTopgisTlm();

			var model = new SimpleModel("model", dtmWs);
			Dataset mds1 =
				model.AddDataset(
					new ModelSimpleTerrainDataset("TOPGIS_TLM.TLM_DTM_TERRAIN",
					                              "TOPGIS_TLM.TLM_DTM"));

			var clsDesc = new ClassDescriptor(typeof(QaSurfaceSpikes));
			var tstDesc = new TestDescriptor("QaSurfaceSpikes", clsDesc, testConstructorId: 0);
			var condition = new QualityCondition("QaSurfaceSpikes", tstDesc);
			QualityConditionParameterUtils.AddParameterValue(condition, "terrain", mds1);
			QualityConditionParameterUtils.AddParameterValue(condition, "terrainTolerance", 0.1);
			QualityConditionParameterUtils.AddParameterValue(condition, "maxSlopeDegrees", 30);
			QualityConditionParameterUtils.AddParameterValue(condition, "maxDeltaZ", 10);

			var fact = new DefaultTestFactory(typeof(QaSurfaceSpikes));
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var runner = new QaContainerTestRunner(10000, tests[0]) {KeepGeometry = true};

			const double xMin = 2751000;
			const double yMin = 1238000;
			IEnvelope box = GeometryFactory.CreateEnvelope(xMin, yMin, xMin + 100, yMin + 100);
			runner.Execute(box);
		}
	}
}

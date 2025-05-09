using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSurfaceVertexTest
	{
		private IFeatureWorkspace _testFgdbWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(true);
		}

		private IFeatureWorkspace TestFgdbWs
			=> _testFgdbWs ??
			   (_testFgdbWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaSurfaceVertexTest"));

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void TestSurfacePolygonVertex()
		{
			TestSurfacePolygonVertex(TestFgdbWs);
		}

		private static void TestSurfacePolygonVertex(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestSurfacePolygonVertex",
				                                      fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			// identical parts
			IFeature row = fc.CreateFeature();
			row.Shape = CurveConstruction.StartPoly(2730000, 1250000, 849)
			                             .LineTo(2730000, 1250010, 849)
			                             .LineTo(2730010, 1250000, 849)
			                             .ClosePolygon();
			row.Store();

			IWorkspace dtmWs = TestDataUtils.OpenTopgisAlti();

			SimpleTerrain terrain = GetAltiDtmSimpleTerrain(dtmWs);

			//ITerrain terrain = TerrainUtils.OpenTerrain((IFeatureWorkspace)dtmWs,
			//                                            "TOPGIS_TLM.TLM_DTM_TERRAIN",
			//                                            "TOPGIS_TLM.TLM_DTM");

			var test = new QaSurfaceVertex(
				ReadOnlyTableFactory.Create(fc), terrain, 5,
				ZOffsetConstraint.AboveLimit);
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };

			IEnvelope box = row.Shape.Envelope;
			box.Expand(1.1, 1.1, true);
			runner.Execute(box);

			Assert.IsTrue(runner.ErrorGeometries[0].Envelope.ZMin > 800);
			Console.WriteLine(runner.Errors.Count);
		}

		private static SimpleTerrain GetAltiDtmSimpleTerrain(IWorkspace dtmWs)
		{
			SimpleTerrain terrain = new SimpleTerrain(
				"dataset.Name",
				new List<SimpleTerrainDataSource>
				{
					new SimpleTerrainDataSource(
						DatasetUtils.OpenFeatureClass(dtmWs, "TOPGIS_TLM.TLM_DTM_MASSENPUNKTE"),
						esriTinSurfaceType.esriTinMassPoint),
					new SimpleTerrainDataSource(
						DatasetUtils.OpenFeatureClass(dtmWs, "TOPGIS_TLM.TLM_DTM_BRUCHKANTE"),
						esriTinSurfaceType.esriTinHardLine)
				}, 7.8125, null);
			return terrain;
		}
	}
}

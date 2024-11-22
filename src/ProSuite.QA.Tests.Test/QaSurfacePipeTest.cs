using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSurfacePipeTest
	{
		private IFeatureWorkspace _testFgdbWs;

		private const string _vectorDsName = "TOPGIS_TLM.TLM_DTM_BRUCHKANTE";
		private const string _mosaicDatasetName = "TOPGIS_TLM.TLM_DTM_MOSAIC";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(true);
		}

		private IFeatureWorkspace TestFgdbWs
			=> _testFgdbWs ??
			   (_testFgdbWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("Qa3dPipeTest"));

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		/*
		 * Raster Testing
		 *
		 * Nach dem Erzeugen einer Surface (RasterSurfaceClass) konnte ich keinen Memory-Anstieg mehr feststellen,
		 * unabhängig davon, wo und wieviele Punkte abgefragt wurden. Dies widerspricht den Beobachtungen von Mäni
		 *
		 * Für die Performance ist jedoch sehr stark davon abhängen, wie nahe die abgefragten Punkte liegen.
		 * Je näher beieinander desto schneller. Dies deutet darauf hin, dass ISurface im Hintergrund Pixelblöck(e) cacht und
		 * jeweils wieder freigibt, wenn andere benötigt werden.
		 *
		 * Verhalten für Testcontainer/ QaTests
		 * Um eine vernünftige Performance zu erhalten ist es nötig, dass ein Pixelblock möglichst nur einmal geladen wird.
		 * Dazu drängt sich die class RasterRow auf
		 *
		 */

		/*
		 *  Der TestContainer erstellt die RasterRow-Instanzen und übergibt sie an die Tests via ExecuteCore(IRow row, int tableIndex)
		 *  Es soll darauf geachtet werden, dass alle Raster/Surfaces zur gleichen Zeit im gleichen Gebiet gecacht sind,
		 *  damit eine gegenseitige Abfrage möglichst performant ist.
		 *
		 *  Via RasterSurfaceClass() kann aus einem Raster einfach eine Surface erstellt werden, allerdings ist das caching in der Surface
		 *  nicht über den Benutzer steuerbar.
		 *
		 *  Via IPixelBlock hat man das Caching im Griff, jedoch fehlt einem die ganze Funktionalität von ISurface.
		 *  Vielleicht gibt es einen einfachen Weg um von IPixelBlock zu ISurface zu gelangen.
		 *
		 *  Als DummyTest soll ein Qa-Test "FeatureBetweenRasters" implementiert werden.
		 *  Dadurch sollten alle wesentlichen Aspekte untersucht werden können.
		 */

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void Qa3dPipeSynthMosaicLayerTest()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("Qa3dPipeSynthMosaicLayerTest");
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "VerifyErrorHasZ",
				fields);

			// Too far from terrain:
			IFeature f1 = fc.CreateFeature();
			f1.Shape = CurveConstruction.StartLine(2690010, 1254010, 5)
			                            .LineTo(2690020, 1254020, 5)
			                            .Curve;
			f1.Store();

			// Terrain is missing
			IFeature f2 = fc.CreateFeature();
			f2.Shape = CurveConstruction.StartLine(30, 50, 15)
			                            .LineTo(20, 50, 15)
			                            .Curve;
			f2.Store();

			// Correct
			IFeature f3 = fc.CreateFeature();
			f3.Shape = CurveConstruction.StartLine(2690010, 1254010, 445)
			                            .LineTo(2690020, 1254020, 445)
			                            .Curve;
			f3.Store();

			IWorkspace workspace = TestDataUtils.OpenTopgisAlti();

			IMosaicDataset mosaicDataset = DatasetUtils.OpenMosaicDataset(workspace,
				_mosaicDatasetName);

			var test = new QaSurfacePipe(
				ReadOnlyTableFactory.Create(fc),
				new MosaicRasterReference(new SimpleRasterMosaic(mosaicDataset)), 4);

			var runner = new QaContainerTestRunner(10000, test)
			             {
				             KeepGeometry = true
			             };
			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);

			// TOP-5914: Null spatial reference results in downstream exception in issue filter!
			Assert.NotNull(runner.Errors[0].Geometry?.SpatialReference);
			Assert.NotNull(runner.Errors[1].Geometry?.SpatialReference);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void CanRunMosaicDefinitionFromCondition()
		{
			IWorkspace dtmWs = TestDataUtils.OpenTopgisAlti();

			var modelAlti = new SimpleModel("alti", dtmWs);
			Dataset vectorDs = modelAlti.AddDataset(new ModelVectorDataset(_vectorDsName));
			Dataset mosaicDs =
				modelAlti.AddDataset(new ModelMosaicRasterDataset(_mosaicDatasetName));

			var clsDesc = new ClassDescriptor(typeof(QaSurfacePipe));
			const int testConstructorId = 4;
			var tstDesc = new TestDescriptor("QaSurfacePipe", clsDesc, testConstructorId);
			var condition = new QualityCondition("QaSurfacePipe", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "featureClass", vectorDs);
			InstanceConfigurationUtils.AddParameterValue(condition, "rasterMosaic", mosaicDs);
			InstanceConfigurationUtils.AddParameterValue(condition, "limit", 2);

			var fact = new DefaultTestFactory(typeof(QaSurfacePipe), testConstructorId);
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(modelAlti.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var runner = new QaContainerTestRunner(10000, tests[0]) { KeepGeometry = true };

			const double xMin = 2751000;
			const double yMin = 1238000;
			IEnvelope box = GeometryFactory.CreateEnvelope(xMin, yMin, xMin + 100, yMin + 100);
			runner.Execute(box);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void CanRunMosaicDatasetFromCondition()
		{
			IWorkspace dtmWs = TestDataUtils.OpenTopgisAlti();
			var modelAlti = new SimpleModel("alti", dtmWs);

			Dataset mds0 = modelAlti.AddDataset(new ModelVectorDataset(_vectorDsName));

			Dataset mds1 =
				modelAlti.AddDataset(new ModelMosaicRasterDataset(_mosaicDatasetName));

			var clsDesc = new ClassDescriptor(typeof(QaSurfacePipe));
			const int testConstructorId = 4;
			var tstDesc = new TestDescriptor("QaSurfacePipe", clsDesc, testConstructorId);
			var condition = new QualityCondition("QaSurfacePipe", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "featureClass", mds0);
			InstanceConfigurationUtils.AddParameterValue(condition, "rasterMosaic", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "limit", 4);

			var fact = new DefaultTestFactory(typeof(QaSurfacePipe), testConstructorId);
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(modelAlti.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var runner = new QaContainerTestRunner(10000, tests[0]) { KeepGeometry = true };

			const double xMin = 2751000;
			const double yMin = 1238000;
			IEnvelope box = GeometryFactory.CreateEnvelope(xMin, yMin, xMin + 100, yMin + 100);
			runner.Execute(box);
		}
	}
}

using System.Collections.Generic;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class Qa3dPipeTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testFgdbWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor, EsriExtension.ThreeDAnalyst);
		}

		private IFeatureWorkspace TestFgdbWs
			=> _testFgdbWs ??
			   (_testFgdbWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("Qa3dPipeTest"));

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
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

			IFeature f1 = fc.CreateFeature();
			f1.Shape = CurveConstruction.StartLine(10, 10, 5)
			                            .LineTo(20, 20, 5)
			                            .Curve;
			f1.Store();

			IFeature f2 = fc.CreateFeature();
			f2.Shape = CurveConstruction.StartLine(30, 50, 15)
			                            .LineTo(20, 50, 15)
			                            .Curve;
			f2.Store();

			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset mosaicDataset = DatasetUtils.OpenMosaicDataset(workspace,
			                                                              "TOPGIS_TLM.TLM_DTM_MOSAIC");

			IFeatureClass rasterCatalog = mosaicDataset.Catalog;

			List<IRaster> rs = new List<IRaster>();
			int count = 0;
			foreach (IFeature catalogFeature in GdbQueryUtils.GetFeatures(
				rasterCatalog, (IQueryFilter) null, false))
			{
				// Method 1 (slow):
				var rasterCatalogItem = (IRasterCatalogItem) catalogFeature;

				IRasterDataset rasterDataset = rasterCatalogItem.RasterDataset;
				var itemPaths = (IItemPaths) rasterDataset;
				IRaster r = rasterDataset.CreateDefaultRaster();
				rs.Add(r);
				count++;
				if (count > 0)
				{
					break;
				}
			}

			var test = new Qa3dPipeX(fc, new MosaicLayerDefinition("t", rs), 4);

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(runner.Errors.Count, 1);
		}
	}
}

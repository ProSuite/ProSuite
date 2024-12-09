#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	public class TrZAssignTest
	{
		private string _simpleGdbPath;
		private const string _dhm200BernRasterName = "DHM200_Bern";

		// NOTE: If these tests fail due to missing raster, you might have to delete the
		// cached gdb in C:\Temp\UnitTestData\ProSuite.Commons.AO.Test\GetGdb1Path
		// because overwrite is false!
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);

			_simpleGdbPath = Commons.AO.Test.TestData.GetGdb1Path();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanUseTrZAssign()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(2600000, 1200000)
				                           .LineTo(2601000, 1201000).Curve;
				f.Store();
			}

			var rds = new RasterDatasetReference(OpenDhm200GdbRasterDataset());

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rds);
			Qa3dConstantZ test =
				new Qa3dConstantZ(tr.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanUseTrZAssignPoint()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPoint, srLv95, 1000)));

			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(2600000, 1200000);
				f.Store();
			}

			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(2601000, 1201000);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(2601000, 1201000);
				f.Store();
			}

			var rds = new RasterDatasetReference(OpenDhm200GdbRasterDataset());

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rds);
			QaZDifferenceSelf test =
				new QaZDifferenceSelf(tr.GetTransformed(), 1, 2, ZComparisonMethod.BoundingBox,
				                      null);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanUseTrZAssignMultipointPoint()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryMultipoint, srLv95, 1000)));

			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreateMultipoint(
					new[]
					{
						new WKSPointZ { X = 2598000, Y = 1198000 },
						new WKSPointZ { X = 2600001, Y = 1200001 },
						new WKSPointZ { X = 2601000, Y = 1201000 }
					},
					(IGeometryDef) null);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreateMultipoint(
					new[]
					{
						new WKSPointZ { X = 2598010, Y = 1198020 },
						new WKSPointZ { X = 2600031, Y = 1200041 },
						new WKSPointZ { X = 2601000, Y = 1201000 }
					},
					(IGeometryDef) null);
				f.Store();
			}

			var rds = new RasterDatasetReference(OpenDhm200GdbRasterDataset());

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rds)
			               { ZAssignOption = AssignOption.All };
			QaZDifferenceSelf test =
				new QaZDifferenceSelf(tr.GetTransformed(), 1, 2, ZComparisonMethod.BoundingBox,
				                      null);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanUseTrZAssignMultitile()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(2598000, 1198000)
				                           .LineTo(2600001, 1200001)
				                           .LineTo(2601000, 1201000).Curve;
				f.Store();
			}

			var rds = new RasterDatasetReference(OpenDhm200GdbRasterDataset());

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rds);
			Qa3dConstantZ test =
				new Qa3dConstantZ(tr.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				tr.ZAssignOption = AssignOption.All;

				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanUseTrZAssignMosaic()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(2598000, 1198000)
				                           .LineTo(2600001, 1200001)
				                           .LineTo(2601000, 1201000).Curve;
				f.Store();
			}

			IWorkspace rws = WorkspaceUtils.OpenWorkspace(_simpleGdbPath);

			IMosaicDataset mosaic = DatasetUtils.OpenMosaicDataset(rws, "DHM200_Mosaic");
			var simpleRasterMosaic = new SimpleRasterMosaic(mosaic);
			var mosaicReference = new MosaicRasterReference(simpleRasterMosaic);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);

			TrZAssign tr = new TrZAssign(roFc, mosaicReference);

			Qa3dConstantZ test =
				new Qa3dConstantZ(tr.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				tr.ZAssignOption = AssignOption.All;

				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanMultiUseTrZAssign()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(2600000, 1200000)
				                           .LineTo(2601000, 1201000).Curve;
				f.Store();
			}

			var rds = new RasterDatasetReference(OpenDhm200GdbRasterDataset());

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr0 = new TrZAssign(roFc, rds);
			Qa3dConstantZ testConstZ =
				new Qa3dConstantZ(tr0.GetTransformed(), 0);

			TrZAssign tr1 = new TrZAssign(roFc, rds);
			QaLineIntersectZ testIntersect =
				new QaLineIntersectZ(tr1.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, testConstZ, testIntersect);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		private IRasterDataset OpenDhm200GdbRasterDataset()
		{
			IWorkspace rasterWorkspace = WorkspaceUtils.OpenWorkspace(_simpleGdbPath);
			IRasterDataset rds =
				DatasetUtils.OpenRasterDataset(rasterWorkspace, _dhm200BernRasterName);
			return rds;
		}
	}
}

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Surface;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	public class TrZAssignTest
	{
		private string _simpleGdbPath;

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

			IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
			IRasterDataset rds = ((IRasterWorkspace2) rws).OpenRasterDataset("DHM200_Bern");

			RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2) rds);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rasterRef);
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
			int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
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

			IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
			IRasterDataset rds = ((IRasterWorkspace2)rws).OpenRasterDataset("DHM200_Bern");

			RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2)rds);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rasterRef);
			QaZDifferenceSelf test =
				new QaZDifferenceSelf(tr.GetTransformed(), 1, 2, ZComparisonMethod.BoundingBox, null);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}


		[Test]
		public void CanUseTrZAssignMultipointPoint()
		{
			int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
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
					new []
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
					(IGeometryDef)null);
				f.Store();
			}

			IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
			IRasterDataset rds = ((IRasterWorkspace2)rws).OpenRasterDataset("DHM200_Bern");

			RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2)rds);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rasterRef)
			               { ZAssignOption = TrZAssign.AssignOption.All };
			QaZDifferenceSelf test =
				new QaZDifferenceSelf(tr.GetTransformed(), 1, 2, ZComparisonMethod.BoundingBox, null);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanUseTrZAssignMultitile()
		{
			int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
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

			IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
			IRasterDataset rds = ((IRasterWorkspace2)rws).OpenRasterDataset("DHM200_Bern");

			RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2)rds);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rasterRef);
			Qa3dConstantZ test =
				new Qa3dConstantZ(tr.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				tr.ZAssignOption = TrZAssign.AssignOption.All;

				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanUseTrZAssignMosaic()
		{
			int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
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

			IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
			IRasterDataset rds = ((IRasterWorkspace2)rws).OpenRasterDataset("DHM200_Mosaic");

			RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2)rds);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr = new TrZAssign(roFc, rasterRef);
			Qa3dConstantZ test =
				new Qa3dConstantZ(tr.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				tr.ZAssignOption = TrZAssign.AssignOption.All;

				var runner = new QaContainerTestRunner(2000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}


		[Test]
		public void CanMultiUseTrZAssign()
		{
			int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
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

			IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
			IRasterDataset rds = ((IRasterWorkspace2)rws).OpenRasterDataset("DHM200_Bern");

			RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2)rds);

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			TrZAssign tr0 = new TrZAssign(roFc, rasterRef);
			Qa3dConstantZ testConstZ =
				new Qa3dConstantZ(tr0.GetTransformed(), 0);

			TrZAssign tr1 = new TrZAssign(roFc, rasterRef);
			QaLineIntersectZ testIntersect =
				new QaLineIntersectZ(tr1.GetTransformed(), 0);

			{
				var runner = new QaContainerTestRunner(2000, testConstZ, testIntersect);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

	}
}

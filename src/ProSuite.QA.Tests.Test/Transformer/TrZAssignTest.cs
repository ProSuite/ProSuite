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
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using System.Collections.Generic;
using System;
using System.Linq;
using ProSuite.QA.Container;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

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
			               { ZAssignOption = TrZAssign.AssignOption.All };
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
				tr.ZAssignOption = TrZAssign.AssignOption.All;

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

			IMosaicDataset mosaic = MosaicUtils.OpenMosaicDataset(rws, "DHM200_Mosaic");
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
				tr.ZAssignOption = TrZAssign.AssignOption.All;

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

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrZAssign");

			IFeatureClass featureClass =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			ReadOnlyFeatureClass roPolyFc = ReadOnlyTableFactory.Create(featureClass);

			double tileSize = 100;
			double x = 2600000;
			double y = 1200000;

			// Left of first tile, NOT within search distance
			IFeature leftOfFirst = CreateFeature(featureClass, x - 20, y + 30, x - 15, y + 40);
			IFeature leftOfFirstIntersect =
				CreateFeature(featureClass, x - 20, y + 30, x - 15, y + 40);

			// Inside first tile:
			IFeature insideFirst = CreateFeature(featureClass, x, y, x + 10, y + 10);
			IFeature insideFirstIntersect = CreateFeature(featureClass, x, y, x + 10, y + 10);

			// Right of first tile, NOT within search distance
			IFeature rightOfFirst =
				CreateFeature(featureClass, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);
			IFeature rightOfFirstIntersect =
				CreateFeature(featureClass, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);

			// Left of second tile, NOT within the search distance:
			IFeature leftOfSecond =
				CreateFeature(featureClass, x + tileSize - 20, y, x + tileSize - 15, y + 10);
			IFeature leftOfSecondIntersect =
				CreateFeature(featureClass, x + tileSize - 20, y, x + tileSize - 15, y + 10);

			var rds = new RasterDatasetReference(OpenDhm200GdbRasterDataset());
			TrZAssign tr = new TrZAssign(roPolyFc, rds)
			               {
				               // NOTE: The search logic should work correctly even if search option is Tile! (e.g. due to downstream transformers)
				               //NeighborSearchOption = TrSpatialJoin.SearchOption.All
			               };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			var test =
				new ContainerOutOfTileDataAccessTest(transformedClass)
				{
					SearchDistanceIntoNeighbourTiles = 50
				};

			test.TileProcessed = (tile, outsideTileFeatures) =>
			{
				if (tile.CurrentEnvelope.XMin == x && tile.CurrentEnvelope.YMin == y)
				{
					// first tile: the leftOfFirst and rightOfFirst
					Assert.AreEqual(4, outsideTileFeatures.Count);

					foreach (IReadOnlyRow outsideTileFeature in outsideTileFeatures)
					{
						Assert.True(InvolvedRowUtils.GetInvolvedRows(outsideTileFeature).All(
							            r => r.OID == leftOfFirst.OID ||
							                 r.OID == leftOfFirstIntersect.OID ||
							                 r.OID == rightOfFirst.OID ||
							                 r.OID == rightOfFirstIntersect.OID));
					}
				}

				if (tile.CurrentEnvelope.XMin == x + tileSize && tile.CurrentEnvelope.YMin == y)
				{
					// second tile: leftOfSecond
					Assert.AreEqual(4, outsideTileFeatures.Count);

					foreach (IReadOnlyRow outsideTileFeature in outsideTileFeatures)
					{
						Assert.True(InvolvedRowUtils.GetInvolvedRows(outsideTileFeature).All(
							            r => r.OID == leftOfSecond.OID ||
							                 r.OID == leftOfSecondIntersect.OID));
					}
				}

				return 0;
			};

			test.SetSearchDistance(10);

			var container = new TestContainer { TileSize = tileSize };

			container.AddTest(test);

			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

			IEnvelope aoi = GeometryFactory.CreateEnvelope(
				2600000, 1200000.00, 2600000 + 2 * tileSize, 1200000.00 + tileSize, sr);

			// First, using FullGeometrySearch:
			test.UseFullGeometrySearch = true;
			container.Execute(aoi);

			// Now simulate full tile loading:
			test.UseFullGeometrySearch = false;
			test.UseTileEnvelope = true;
			container.Execute(aoi);
		}

		private static IFeature CreateFeature(IFeatureClass featureClass,
		                                      double xMin, double yMin,
		                                      double xMax, double yMax)
		{
			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

			IFeature row = featureClass.CreateFeature();
			row.Shape = GeometryFactory.CreatePolygon(xMin, yMin, xMax, yMax, sr);
			row.Store();
			return row;
		}

		private static void WriteFieldNames(IReadOnlyTable targetTable)
		{
			for (int i = 0; i < targetTable.Fields.FieldCount; i++)
			{
				IField field = targetTable.Fields.Field[i];

				Console.WriteLine(field.Name);
			}
		}

		private IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                         esriGeometryType geometryType,
		                                         IList<IField> customFields = null)
		{
			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}
	}
}

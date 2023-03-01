using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrDissolveTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense:true);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanDissolve()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrDissolve");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lineFc", fields);

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(50, 80).LineTo(90, 20)
										   .LineTo(10, 10).Curve;
				f.Store();
			}


			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(70, 70).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(80, 80).LineTo(70, 70).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 10).LineTo(60, 10).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(60, 10).LineTo(60, 40).Curve;
				f.Store();
			}

			TrDissolve dissolve =
				new TrDissolve(ReadOnlyTableFactory.Create(fc))
				{ Search = 1, NeighborSearchOption = TrDissolve.SearchOption.All };
			QaMinLength test = new QaMinLength(dissolve.GetTransformed(), 100);

			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				var runner = new QaContainerTestRunner(25, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanDissolvePolygonTile()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrDissolve");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			ISpatialReference sr =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			fields.AddField(
				FieldUtils.CreateShapeField("Shape", esriGeometryType.esriGeometryPolygon, sr,
				                            1000));

			const string fclassName = "polyFc";
			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, fclassName, fields);

			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(0, 0, 70, 70, sr);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(70, 70, 80, 80, sr);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(20, 70, 30, 80, sr);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(20, 20, 30, 30, sr);
				f.Store();
			}

			TrDissolve dissolve =
				new TrDissolve(ReadOnlyTableFactory.Create(fc))
				{ Search = 1, NeighborSearchOption = TrDissolve.SearchOption.Tile };

			// Ensure unique OID:
			List<long> objectIDs = dissolve
			                       .GetTransformed().EnumReadOnlyRows(new QueryFilterClass(), false)
			                       .Select(f => f.OID).ToList();

			Assert.AreEqual(objectIDs.Count, objectIDs.Distinct().Count());

			QaMinArea test = new QaMinArea(dissolve.GetTransformed(), 110);
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				// Each tile sees some different combination of features...
				var runner = new QaContainerTestRunner(25, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void CanDissolvePolygonAll()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrDissolve");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			ISpatialReference sr =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			fields.AddField(
				FieldUtils.CreateShapeField("Shape", esriGeometryType.esriGeometryPolygon, sr,
				                            1000));

			const string fclassName = "polyFc";
			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, fclassName, fields);

			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(0, 0, 70, 70, sr);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(70, 70, 80, 80, sr);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(20, 70, 30, 80, sr);
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = GeometryFactory.CreatePolygon(20, 20, 30, 30, sr);
				f.Store();
			}

			TrDissolve dissolve =
				new TrDissolve(ReadOnlyTableFactory.Create(fc))
				{ Search = 1, NeighborSearchOption = TrDissolve.SearchOption.All };

			// Ensure unique OID:
			List<long> objectIDs = dissolve
			                       .GetTransformed().EnumReadOnlyRows(new QueryFilterClass(), false)
			                       .Select(f => f.OID).ToList();

			Assert.AreEqual(objectIDs.Count, objectIDs.Distinct().Count());

			IGeometry errorShape;
			QaMinArea test = new QaMinArea(dissolve.GetTransformed(), 110);
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.KeepGeometry = true;
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
				errorShape = runner.ErrorGeometries[0];
			}
			{
				var runner = new QaContainerTestRunner(25, test);
				runner.KeepGeometry = true;
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);

				foreach (IGeometry geometry in runner.ErrorGeometries)
				{
					Assert.IsTrue(GeometryUtils.AreEqualInXY(errorShape, geometry));
				}
			}
		}

		[Test]
		public void CanDissolveMultipart()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrDissolve");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lineFc", fields);

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(60, 60).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(70, 70).LineTo(60, 60).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(60, 60).LineTo(60, 70).Curve;
				f.Store();
			}

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 10).LineTo(60, 10).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(60, 10).LineTo(60, 40).Curve;
				f.Store();
			}
			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(70, 10).LineTo(60, 10).Curve;
				f.Store();
			}

			TrDissolve dissolve =
				new TrDissolve(ReadOnlyTableFactory.Create(fc))
				{
					Search = 1,
					NeighborSearchOption = TrDissolve.SearchOption.All,
					CreateMultipartFeatures = true
				};
			QaMinLength test = new QaMinLength(dissolve.GetTransformed(), 100);

			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				var runner = new QaContainerTestRunner(25, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanGroupBy()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestTrDissolve");

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "lineFc", "config",
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape", esriGeometryType.esriGeometryPolyline,
					SpatialReferenceUtils.CreateSpatialReference
					((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					 true), 1000),
				FieldUtils.CreateIntegerField("RouteId"));

			ITable table = DatasetUtils.CreateTable(
				ws, "RouteTbl", "config",
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField("RouteFk"),
				FieldUtils.CreateIntegerField("RouteNr"));

			const string relRoute = "relRoute";
			TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, relRoute, (ITable) lineFc, table, "RouteId", "RouteFk");

			int iRouteId = lineFc.FindField("RouteId");
			int iRouteFk = table.FindField("RouteFk");
			int iRouteNr = table.FindField("RouteNr");
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve;
				f.Value[iRouteId] = 1;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 0).LineTo(10, 10).Curve;
				f.Value[iRouteId] = 2;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(10, 30).Curve;
				f.Value[iRouteId] = 3;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 30).LineTo(10, 30).Curve;
				f.Value[iRouteId] = 4;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 30).LineTo(10, 30).Curve;
				f.Value[iRouteId] = 5;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 30).LineTo(0, 0).Curve;
				f.Value[iRouteId] = 6;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(20, 30).LineTo(20, 0).Curve;
				f.Value[iRouteId] = 7;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(20, 0).Curve;
				f.Value[iRouteId] = 8;
				f.Store();
			}

			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 1;
				r.Value[iRouteNr] = 100;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 3;
				r.Value[iRouteNr] = 100;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 4;
				r.Value[iRouteNr] = 100;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 6;
				r.Value[iRouteNr] = 100;
				r.Store();
			}

			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 1;
				r.Value[iRouteNr] = 200;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 2;
				r.Value[iRouteNr] = 200;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 8;
				r.Value[iRouteNr] = 200;
				r.Store();
			}

			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 2;
				r.Value[iRouteNr] = 300;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 3;
				r.Value[iRouteNr] = 300;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 5;
				r.Value[iRouteNr] = 300;
				r.Store();
			}
			{
				IRow r = table.CreateRow();
				r.Value[iRouteFk] = 7;
				r.Value[iRouteNr] = 300;
				r.Store();
			}

			TrTableJoin joined =
				new TrTableJoin(ReadOnlyTableFactory.Create(lineFc),
				                ReadOnlyTableFactory.Create(table), relRoute, JoinType.InnerJoin);
			TrDissolve dissolve =
				new TrDissolve((IReadOnlyFeatureClass) joined.GetTransformed())
				{
					Search = 1,
					Attributes = new List<string> { "Min(RouteTbl.RouteFk) AS MinRouteFk" },
					GroupBy = new List<string> { "RouteTbl.RouteNr" }
				};
			TrLineToPolygon lineToPolygon =
				new TrLineToPolygon(dissolve.GetTransformed())
				{ Attributes = new[] { "RouteTbl.RouteNr" } };

			{
				QaMinArea test = new QaMinArea(lineToPolygon.GetTransformed(), 1000);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				QaMinArea test = new QaMinArea(lineToPolygon.GetTransformed(), 150);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaMinArea test = new QaMinArea(lineToPolygon.GetTransformed(), 1000);
				test.SetConstraint(0, "RouteTbl.RouteNr > 100");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void VerifyRowsCached()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrDissolve");

			IFeatureClass lineFc;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateShapeField(
					                "Shape", esriGeometryType.esriGeometryPolyline,
					                SpatialReferenceUtils.CreateSpatialReference
					                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					                 true), 1000));
				lineFc = DatasetUtils.CreateSimpleFeatureClass(ws, "lineFc", fields);
			}

			IFeatureClass ptFc;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateShapeField(
					                "Shape", esriGeometryType.esriGeometryPoint,
					                SpatialReferenceUtils.CreateSpatialReference
					                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					                 true), 1000));
				ptFc = DatasetUtils.CreateSimpleFeatureClass(ws, "pointFc", fields);
			}

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 10).LineTo(10, 0).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 10).LineTo(0, 80).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 80).LineTo(10, 90).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 90).LineTo(80, 90).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(80, 90).LineTo(90, 80).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(90, 10).LineTo(90, 80).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(80, 0).LineTo(90, 10).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 0).LineTo(80, 0).Curve;
				f.Store();
			}

			{
				IFeature f = ptFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(40, 40);
				f.Store();
			}

			TrDissolve dissolve =
				new TrDissolve(ReadOnlyTableFactory.Create(lineFc))
				{
					Search = 1,
					NeighborSearchOption = TrDissolve.SearchOption.All
				};
			TrLineToPolygon lineToPolygon = new TrLineToPolygon(dissolve.GetTransformed());
			QaIntersectsOther test = new QaIntersectsOther(
				lineToPolygon.GetTransformed(),
				ReadOnlyTableFactory.Create(ptFc));

			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				var runner = new QaContainerTestRunner(25, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void Test491()
		{
			var workspace = (IFeatureWorkspace)TestData.TestDataUtils.OpenTopgisTlm();

			IReadOnlyFeatureClass fg =
				ReadOnlyTableFactory.Create(
					workspace.OpenFeatureClass("TOPGIS_TLM.TLM_FLIESSGEWAESSER"));
			IReadOnlyFeatureClass bo =
				ReadOnlyTableFactory.Create(
					workspace.OpenFeatureClass("TOPGIS_TLM.TLM_BODENBEDECKUNG"));

			TrDissolve trDissolve = new TrDissolve(fg)
			                        {
				                        Search = 0,
				                        NeighborSearchOption = TrDissolve.SearchOption.All,
				                        CreateMultipartFeatures = true
			                        };
			trDissolve.SetConstraint(0, "OBJEKTART = 7");

			TrIntersect trIntersect = new TrIntersect(trDissolve.GetTransformed(), bo);
			trIntersect.SetConstraint(1, "OBJEKTART = 5");

			QaConstraint qa = new QaConstraint(trIntersect.GetTransformed(), "PartIntersected <= 0.1");

			var runner = new QaContainerTestRunner(10000, qa);
			runner.TestContainer.MaxCachedPointCount = 5000000;
			runner.Execute();

		}
	}
}

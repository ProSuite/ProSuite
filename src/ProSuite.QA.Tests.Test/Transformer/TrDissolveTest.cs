using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrDissolveTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
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
				new TrDissolve(fc) {Search = 1, NeighborSearchOption = TrDissolve.SearchOption.All};
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
				new TrDissolve(fc)
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
				new TrTableJoin((ITable) lineFc, table, relRoute, JoinType.InnerJoin);
			TrDissolve dissolve =
				new TrDissolve((IFeatureClass) joined.GetTransformed())
				{
					Search = 1,
					Attributes = new List<string> {"Min(RouteTbl.RouteFk) AS MinRouteFk"},
					GroupBy = new List<string> {"RouteTbl.RouteNr"}
				};
			TrLineToPoly lineToPoly =
				new TrLineToPoly(dissolve.GetTransformed())
				{Attributes = new[] {"RouteTbl.RouteNr"}};

			{
				QaMinArea test = new QaMinArea(lineToPoly.GetTransformed(), 1000);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				QaMinArea test = new QaMinArea(lineToPoly.GetTransformed(), 150);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaMinArea test = new QaMinArea(lineToPoly.GetTransformed(), 1000);
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
				new TrDissolve(lineFc)
				{
					Search = 1,
					NeighborSearchOption = TrDissolve.SearchOption.All
				};
			TrLineToPoly lineToPoly = new TrLineToPoly(dissolve.GetTransformed());
			QaIntersectsOther test = new QaIntersectsOther(lineToPoly.GetTransformed(), ptFc);

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
	}
}

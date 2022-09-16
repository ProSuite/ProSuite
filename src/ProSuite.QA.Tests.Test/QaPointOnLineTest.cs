using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaPointOnLineTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaPointOnLineTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestTileBorder()
		{
			TestTileBorder(_testWs);
		}

		private static void TestTileBorder(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, sr, 1000));

			IFeatureClass fcPoints =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TileBorderPoints", fieldsPoint);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr, 1000));

			IFeatureClass fcLines =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TileBorderLines", fieldsLine);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature point1 = fcPoints.CreateFeature();
			point1.Shape = GeometryFactory.CreatePoint(999.5, 100);
			// near fLine1, but error from tile 2 ?
			point1.Store();

			IFeature point2 = fcPoints.CreateFeature();
			point2.Shape = GeometryFactory.CreatePoint(999.5, 400);
			// near fLine2, but error from tile 1 ?
			point2.Store();

			IFeature fLine1 = fcLines.CreateFeature();
			IPolycurve l1 = CurveConstruction.StartLine(0, 10)
			                                 .LineTo(998.7, 10)
			                                 .LineTo(998.7, 200)
			                                 .Curve;
			fLine1.Shape = l1;
			fLine1.Store();

			IFeature fLine2 = fcLines.CreateFeature();
			IPolycurve l2 = CurveConstruction.StartLine(2000, 500)
			                                 .LineTo(1000.3, 500)
			                                 .LineTo(1000.3, 300)
			                                 .Curve;
			fLine2.Shape = l2;
			fLine2.Store();

			var test = new QaPointOnLine(
				ReadOnlyTableFactory.Create(fcPoints), new[] { ReadOnlyTableFactory.Create(fcLines)}, 1);

			using (var r = new QaTestRunner(test)) // no tiling no problem
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(1000, test);
			container.Execute(GeometryFactory.CreateEnvelope(0, 0, 2000, 500));
			Assert.AreEqual(0, container.Errors.Count);
		}
	}
}

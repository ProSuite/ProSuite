using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaLineIntersectZTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestLineIntersectZ");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestWithConstraint()
		{
			TestWithConstraint(_testWs);
		}

		private static void TestWithConstraint(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);
			SpatialReferenceUtils.SetZDomain(sr, 0, 1000, 0.001, 0.002);

			IFieldsEdit fieldsBahn = new FieldsClass();
			fieldsBahn.AddField(FieldUtils.CreateOIDField());
			fieldsBahn.AddField(FieldUtils.CreateIntegerField("Stufe"));
			fieldsBahn.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                    1000, true, false));

			IFeatureClass fcBahn =
				DatasetUtils.CreateSimpleFeatureClass(ws, "Bahn", fieldsBahn,
				                                      null);

			IFieldsEdit fieldsStr = new FieldsClass();
			fieldsStr.AddField(FieldUtils.CreateOIDField());
			fieldsStr.AddField(FieldUtils.CreateIntegerField("Dummy"));
			fieldsStr.AddField(FieldUtils.CreateIntegerField("Stufe"));
			fieldsStr.AddField(FieldUtils.CreateShapeField(
				                   "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                   1000, true, false));

			IFeatureClass fcStr =
				DatasetUtils.CreateSimpleFeatureClass(ws, "Strasse", fieldsStr,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			int fieldIndexStrStufe = fcStr.FindField("Stufe");
			int fieldIndexBahnStufe = fcBahn.FindField("Stufe");

			IFeature bahn1 = fcBahn.CreateFeature();
			bahn1.set_Value(fieldIndexBahnStufe, 1);
			bahn1.Shape = CurveConstruction.StartLine(100, 100, 100)
			                               .LineTo(200, 100, 100)
			                               .Curve;
			bahn1.Store();

			IFeature str1 = fcStr.CreateFeature();
			str1.set_Value(fieldIndexStrStufe, 0);
			str1.Shape = CurveConstruction.StartLine(150, 90, 101)
			                              .LineTo(150, 110, 101)
			                              .Curve;
			str1.Store();

			IFeature str2 = fcStr.CreateFeature();
			str2.set_Value(fieldIndexStrStufe, 2);
			str2.Shape = CurveConstruction.StartLine(180, 90, 99)
			                              .LineTo(180, 110, 99)
			                              .Curve;
			str2.Store();

			IFeature str3 = fcStr.CreateFeature();
			str3.set_Value(fieldIndexStrStufe, 0);
			str3.Shape = CurveConstruction.StartLine(120, 90, 101)
			                              .LineTo(120, 110, 101)
			                              .Curve;
			str3.Store();

			var test =
				new QaLineIntersectZ(new[] {fcBahn, fcStr}, 0.5, "U.Stufe > L.Stufe");

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute();
				Assert.AreEqual(
					6, testRunner.Errors.Count); // mirrored errors not eliminated
			}

			var ctr = new QaContainerTestRunner(1000, test);
			ctr.Execute();
			Assert.AreEqual(3, ctr.Errors.Count);
			// mirrored errors eliminated by QaErrorAdministrator

			Assert.AreEqual(
				"Strasse,1; Bahn,1: Constraint 'U.Stufe > L.Stufe' is not fulfilled: U.STUFE = 0; L.STUFE = 1 [LineIntersectZ.ConstraintNotFulfilled] {Shape}",
				ctr.Errors[0].ToString());
			Assert.AreEqual(
				"Bahn,1; Strasse,2: Constraint 'U.Stufe > L.Stufe' is not fulfilled: U.STUFE = 1; L.STUFE = 2 [LineIntersectZ.ConstraintNotFulfilled] {Shape}",
				ctr.Errors[1].ToString());
			Assert.AreEqual(
				"Strasse,3; Bahn,1: Constraint 'U.Stufe > L.Stufe' is not fulfilled: U.STUFE = 0; L.STUFE = 1 [LineIntersectZ.ConstraintNotFulfilled] {Shape}",
				ctr.Errors[2].ToString());
		}

		[Test]
		public void TestMinimumMaximum()
		{
			TestMinimumMaximum(_testWs);
		}

		private static void TestMinimumMaximum(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);
			SpatialReferenceUtils.SetZDomain(sr, 0, 1000, 0.001, 0.002);

			IFieldsEdit fieldsBahn = new FieldsClass();
			fieldsBahn.AddField(FieldUtils.CreateOIDField());
			fieldsBahn.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                    1000, true, false));

			IFeatureClass fcBahn =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestMinimumMaximum_Bahn",
				                                      fieldsBahn,
				                                      null);

			IFieldsEdit fieldsStr = new FieldsClass();
			fieldsStr.AddField(FieldUtils.CreateOIDField());
			fieldsStr.AddField(FieldUtils.CreateShapeField(
				                   "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                   1000, true, false));

			IFeatureClass fcStr =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestMinimumMaximum_Str",
				                                      fieldsStr,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature bahn1 = fcBahn.CreateFeature();
			bahn1.Shape = CurveConstruction.StartLine(100, 100, 100)
			                               .LineTo(200, 100, 100)
			                               .Curve;
			bahn1.Store();

			IFeature str1 = fcStr.CreateFeature();
			str1.Shape = CurveConstruction.StartLine(150, 90, 101)
			                              .LineTo(150, 110, 101)
			                              .Curve;
			str1.Store();

			IFeature str2 = fcStr.CreateFeature();
			str2.Shape = CurveConstruction.StartLine(180, 90, 99.8)
			                              .LineTo(180, 110, 99.8)
			                              .Curve;
			str2.Store();

			IFeature str3 = fcStr.CreateFeature();
			str3.Shape = CurveConstruction.StartLine(120, 90, 103)
			                              .LineTo(120, 110, 103)
			                              .Curve;
			str3.Store();

			var test = new QaLineIntersectZ(new[] {fcBahn, fcStr}, 0.5, 2.5, null);
			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute();
				Assert.AreEqual(
					4, testRunner.Errors.Count); // mirrored errors not elimated
			}

			var ctr = new QaContainerTestRunner(1000, test);
			ctr.Execute();
			Assert.AreEqual(2, ctr.Errors.Count);
			// mirrored errors eliminated by QaErrorAdministrator
		}

		[Test]
		public void TestMinimumMaximumExpressions()
		{
			TestMinimumMaximumExpressions(_testWs);
		}

		private static void TestMinimumMaximumExpressions(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);
			SpatialReferenceUtils.SetZDomain(sr, 0, 1000, 0.001, 0.002);

			IFieldsEdit fieldsBahn = new FieldsClass();
			fieldsBahn.AddField(FieldUtils.CreateOIDField());
			fieldsBahn.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                    1000, true, false));

			IFeatureClass fcBahn =
				DatasetUtils.CreateSimpleFeatureClass(ws,
				                                      "TestMinimumMaximumExpressions_Bahn",
				                                      fieldsBahn,
				                                      null);

			IFieldsEdit fieldsStr = new FieldsClass();
			fieldsStr.AddField(FieldUtils.CreateOIDField());
			fieldsStr.AddField(FieldUtils.CreateShapeField(
				                   "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                   1000, true, false));

			IFeatureClass fcStr =
				DatasetUtils.CreateSimpleFeatureClass(ws,
				                                      "TestMinimumMaximumExpressions_Str",
				                                      fieldsStr,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature bahn1 = fcBahn.CreateFeature();
			bahn1.Shape = CurveConstruction.StartLine(100, 100, 100)
			                               .LineTo(200, 100, 100)
			                               .Curve;
			bahn1.Store();

			IFeature str1 = fcStr.CreateFeature();
			str1.Shape = CurveConstruction.StartLine(150, 90, 101)
			                              .LineTo(150, 110, 101)
			                              .Curve;
			str1.Store();

			IFeature str2 = fcStr.CreateFeature();
			str2.Shape = CurveConstruction.StartLine(180, 90, 99.8)
			                              .LineTo(180, 110, 99.8)
			                              .Curve;
			str2.Store();

			IFeature str3 = fcStr.CreateFeature();
			str3.Shape = CurveConstruction.StartLine(120, 90, 103)
			                              .LineTo(120, 110, 103)
			                              .Curve;
			str3.Store();

			var test = new QaLineIntersectZ(new[] {fcBahn, fcStr}, 9, 99, null)
			           {
				           MinimumZDifferenceExpression = "IIF(L.OBJECTID > 0, 0.5, 999)",
				           MaximumZDifferenceExpression = "IIF(L.OBJECTID > 0, 2.5, 9999)"
			           };

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute();
				Assert.AreEqual(
					4, testRunner.Errors.Count); // mirrored errors not elimated
			}

			var ctr = new QaContainerTestRunner(1000, test);
			ctr.Execute();
			Assert.AreEqual(2, ctr.Errors.Count);
			// mirrored errors eliminated by QaErrorAdministrator
		}

		[Test]
		public void TestMultipleErrors()
		{
			TestMultipleErrors(_testWs);
		}

		private static void TestMultipleErrors(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);
			SpatialReferenceUtils.SetZDomain(sr, 0, 1000, 0.001, 0.002);

			IFieldsEdit fieldsBahn = new FieldsClass();
			fieldsBahn.AddField(FieldUtils.CreateOIDField());
			fieldsBahn.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                    1000, true, false));

			IFeatureClass fcBahn =
				DatasetUtils.CreateSimpleFeatureClass(ws, "BahnM", fieldsBahn,
				                                      null);

			IFieldsEdit fieldsStr = new FieldsClass();
			fieldsStr.AddField(FieldUtils.CreateOIDField());
			fieldsStr.AddField(FieldUtils.CreateShapeField(
				                   "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                   1000, true, false));

			IFeatureClass fcStr =
				DatasetUtils.CreateSimpleFeatureClass(ws, "StrasseM", fieldsStr,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature bahn1 = fcBahn.CreateFeature();
			bahn1.Shape = CurveConstruction.StartLine(100, 100, 100)
			                               .LineTo(200, 100, 100)
			                               .Curve;
			bahn1.Store();

			IFeature str1 = fcStr.CreateFeature();
			str1.Shape = CurveConstruction.StartLine(150, 90, 101)
			                              .LineTo(150, 110, 101)
			                              .Curve;
			str1.Store();

			IFeature bahn2 = fcBahn.CreateFeature();
			bahn2.Shape = CurveConstruction.StartLine(100, 200, 100)
			                               .LineTo(200, 200, 100)
			                               .Curve;
			bahn2.Store();

			IFeature str2 = fcStr.CreateFeature();
			str2.Shape = CurveConstruction.StartLine(150, 190, 101)
			                              .LineTo(150, 210, 101)
			                              .Curve;
			str2.Store();

			var test = new QaLineIntersectZ(new[] {fcBahn, fcStr}, 2.5);

			var handler = new ErrorHandler();
			var ctr = new QaContainerTestRunner(1000, test);
			ctr.TestContainer.QaError += handler.QaError;
			ctr.Execute();
			Assert.IsTrue(ctr.Errors.Count == 2);
			// mirrored errors eliminated by QaErrorAdministrator
		}

		private class ErrorHandler
		{
			private readonly Dictionary<IPoint, IPoint> _cloneOrig =
				new Dictionary<IPoint, IPoint>();

			public void QaError(object sender, QaErrorEventArgs e)
			{
				var orig = (IPoint) e.QaError.Geometry;
				Assert.IsNotNull(orig);
				IPoint clone = GeometryFactory.Clone(orig);
				_cloneOrig.Add(clone, orig);

				foreach (KeyValuePair<IPoint, IPoint> pair in _cloneOrig)
				{
					Assert.AreEqual(pair.Key.X, pair.Value.X);
					Assert.AreEqual(pair.Key.Y, pair.Value.Y);
				}
			}
		}

		[Test]
		[Ignore("Uses local Data")]
		public void Test1212()
		{
			//            <Dataset parameter="featureClass" value="TOPGIS_TLM.TLM_STRASSE" where="OBJEKTART IN (3,5,6) AND KUNSTBAUTE IN (100)" workspace="TLM_Production" />
			//<Scalar parameter="limit" value="3" />

			IWorkspace ws = TestDataUtils.OpenFileGdb("qaLineIntersectZ.gdb");

			IFeatureClass fc = ((IFeatureWorkspace) ws).OpenFeatureClass("TLM_STRASSE");

			var test1 = new QaLineIntersectZ(fc, 3);
			test1.SetConstraint(0, "OBJEKTART IN (3,5,6) AND KUNSTBAUTE IN (100)");

			var test2 = new QaLineIntersectZ(fc, 2, "U.Stufe > L.Stufe");

			var handler = new ErrorHandler();
			var ctr = new QaContainerTestRunner(10000, test1, test2);
			ctr.TestContainer.QaError += handler.QaError;
			ctr.Execute();
			Assert.IsTrue(ctr.Errors.Count > 1);
			// mirrored errors eliminated by QaErrorAdministrator
		}
	}
}

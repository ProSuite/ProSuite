using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaNotNearTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[NotNull]
		private IFeatureWorkspace TestWorkspace
		{
			get
			{
				return _testWs ??
				       (_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaNotNearTest"));
			}
		}

		[Test]
		public void VerifyErrorHasZ()
		{
			VerifyErrorHasZ(TestWorkspace);
		}

		private static void VerifyErrorHasZ(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true, false));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(ws, "VerifyErrorHasZ", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPolycurve line1 = CurveConstruction.StartLine(0, 0, 5)
			                                    .LineTo(2, 0, 5)
			                                    .Curve;
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = line1;
			row1.Store();

			IPolycurve line2 = CurveConstruction.StartLine(-1, 0.02, 5)
			                                    .LineTo(1, 0.02, 5)
			                                    .Curve;
			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = line2;
			row2.Store();

			var test = new QaNotNear(featureClass, 0.1, 0.5);

			var runners =
				new List<QaTestRunnerBase>
				{
					new QaTestRunner(test) {KeepGeometry = true},
					new QaContainerTestRunner(1000, test) {KeepGeometry = true}
				};

			foreach (QaTestRunnerBase runner in runners)
			{
				runner.Execute();
				Assert.True(runner.ErrorGeometries.Count > 0);
				foreach (IGeometry errorGeometry in runner.ErrorGeometries)
				{
					Assert.AreEqual(5, errorGeometry.Envelope.ZMin);
				}
			}
		}

		[Test]
		public void VerifyPolyErrorHasZ()
		{
			VerifyPolyErrorHasZ(TestWorkspace);
		}

		private static void VerifyPolyErrorHasZ(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true, false));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(ws, "VerifyPolyErrorHasZ", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IPolycurve poly1 = CurveConstruction.StartPoly(0, 0, 5.03)
			                                    .LineTo(-10, -1, 4.99)
			                                    .LineTo(-10, 1, 5.02)
			                                    .ClosePolygon();
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = poly1;
			row1.Store();

			IPolycurve poly2 = CurveConstruction.StartPoly(10, -1, 4.94)
			                                    .LineTo(0, 0, 5.01)
			                                    .LineTo(10, 1, 4.97)
			                                    .ClosePolygon();
			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = poly2;
			row2.Store();

			var test = new QaNotNear(featureClass, featureClass, 0.1, 0.1, is3D: true);
			test.IgnoreNeighborCondition = "G1.ObjectId = G2.ObjectId";

			var runners =
				new List<QaTestRunnerBase>
				{
					new QaTestRunner(test) {KeepGeometry = true},
					new QaContainerTestRunner(1000, test) {KeepGeometry = true}
				};

			foreach (QaTestRunnerBase runner in runners)
			{
				runner.Execute();
				Assert.True(runner.ErrorGeometries.Count > 0);
				foreach (IGeometry errorGeometry in runner.ErrorGeometries)
				{
					Assert.True(errorGeometry.Envelope.ZMin > 4);
				}
			}
		}

		[Test]
		public void TestConditionCoincidence()
		{
			TestConditionCoincidence(TestWorkspace);
		}

		private static void TestConditionCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField("LandId"));
			fields.AddField(FieldUtils.CreateIntegerField("OtherId"));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc1 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence1",
				                                      fields,
				                                      null);
			IFeatureClass fc2 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence2",
				                                      fields,
				                                      null);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc1.CreateFeature();
				row.set_Value(1, 1);
				row.Shape = CurveConstruction.StartLine(100, 100).LineTo(200, 200).Curve;
				row.Store();
			}
			{
				IFeature row = fc2.CreateFeature();
				row.set_Value(1, 1);
				row.Shape = CurveConstruction.StartLine(100, 100).LineTo(200, 200).Curve;
				row.Store();
			}

			// test without ignore conditions --> line is near
			var test = new QaNotNear(fc1, fc2, 1, 10);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(1, testRunner.Errors.Count);

			// Same test with ignore conditions --> nothing near
			test = new QaNotNear(fc1, fc2, 1, 10);
			test.IgnoreNeighborCondition = "G1.LandID = G2.LandID";

			testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);
		}
	}
}
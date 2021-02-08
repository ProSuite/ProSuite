using System;
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
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaPartCoincidenceSelfTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestPartCoincidenceSelf");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestValidLines()
		{
			TestValidLines(_testWs);
		}

		private static void TestValidLines(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestValidLines", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 100),
						GeometryFactory.CreatePoint(100, 200),
						GeometryFactory.CreatePoint(200, 200),
						GeometryFactory.CreatePoint(200, 100),
						GeometryFactory.CreatePoint(100, 100));
				row.Store();
			}
			{
				// Feature with exact coincidence
				{
					IFeature row = fc.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100, 100),
							GeometryFactory.CreatePoint(100, 200));
					row.Store();
				}
				{
					// Feature further than near

					IFeature row = fc.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(110, 120),
							GeometryFactory.CreatePoint(110, 180));
					row.Store();
				}
				{
					// near shorter than minLength

					IFeature row = fc.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(120, 190),
							GeometryFactory.CreatePoint(180, 210));
					row.Store();
				}
				{
					// near interrupted with exact coincidence

					IFeature row = fc.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(210, 180),
							GeometryFactory.CreatePoint(200, 200),
							GeometryFactory.CreatePoint(200, 100),
							GeometryFactory.CreatePoint(190, 120));
					row.Store();
				}
			}

			var test = new QaPartCoincidenceSelf(fc, 1, 10, false);
			using (var runner = new QaTestRunner(test))
			{
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestSmallSegments()
		{
			TestSmallSegments(_testWs);
		}

		private static void TestSmallSegments(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestSmallSegments", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 100);
				for (var i = 0; i < 10; i++)
				{
					constr = constr.Line(0, 1);
				}

				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 110);
				for (var i = 0; i < 10; i++)
				{
					constr = constr.Line(0, -1);
				}

				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(fc, 2.5, 5, false);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestSmallAngle()
		{
			TestSmallAngle(_testWs);
		}

		private static void TestSmallAngle(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestSmallAngle", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 100)
				                                            .Line(100, 0)
				                                            .Line(1, 0)
				                                            .Line(-1, 1)
				                                            .Line(0, 100);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(300, 300)
				                                            .Line(200, 0)
				                                            .Line(-100, 1)
				                                            .Line(0, 100);
				row.Shape = constr.Curve;
				row.Store();
			}

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(500, 500)
				                                            .Line(200, 0)
				                                            .Line(0, 1)
				                                            .Line(-99, 0)
				                                            .Line(-1, -0.2)
				                                            .Line(0, 100);
				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.9, 5, 0, false, 1000, 0);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(4, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(4, container.Errors.Count);
		}

		[Test]
		public void TestConditionCoincidence()
		{
			TestConditionCoincidence(
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestConditionCoincidence"));
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
				row.set_Value(2, 1);
				row.Shape = CurveConstruction.StartLine(100.5, 100.5).LineTo(200, 200).Curve;
				row.Store();
			}

			// test without ignore conditions --> near, but not conincident
			var test = new QaPartCoincidenceSelf(new[] {fc1, fc2}, 1, 10);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(2, testRunner.Errors.Count);

			// Same test with ignore conditions --> nothing near
			test = new QaPartCoincidenceSelf(new[] {fc1, fc2}, 1, 10);
			var success = false;
			try
			{
				test.IgnoreNeighborConditions = new[] {"too", "few", "conditions"};
				success = true;
			}
			catch { }

			Assert.False(success);
			test.IgnoreNeighborConditions =
				new[]
				{
					"G1.LandID = G2.LandID", "G1.LandID = G2.OtherID",
					"G1.OtherID = G2.LandID", "G1.OtherID = G2.OtherID"
				};

			testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);
		}

		[Test]
		public void TestNearSelf()
		{
			TestNearSelf(_testWs);
		}

		private static void TestNearSelf(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestNearSelf", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr =
					CurveConstruction.StartLine(500, 500)
					                 .Line(200, 0)
					                 .Line(0, 1)
					                 .Line(-99, 0)
					                 .Line(-1, -0.2)
					                 .Line(0, 100);
				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.9, 150, 0, false, 1000, 0);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(2, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);

			test = new QaPartCoincidenceSelf(new[] {fc}, 0.9, 250, 0, false, 1000, 0);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestSmallLargeSegments()
		{
			TestSmallLargeSegments(_testWs);
		}

		private static void TestSmallLargeSegments(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestSmallLargeSegments", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 100);
				for (var i = 0; i < 10; i++)
				{
					constr = constr.Line(0, 1);
				}

				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 110)
				                                            .LineTo(100, 100);
				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(fc, 2.5, 5, false);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestMultiTile()
		{
			TestMultiTile(_testWs);
		}

		private static void TestMultiTile(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestMultiTile", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(101, 99)
				                                            .LineTo(199, 99)
				                                            .LineTo(201, 101)
				                                            .LineTo(201, 199)
				                                            .LineTo(199, 201)
				                                            .LineTo(101, 201)
				                                            .LineTo(99, 199)
				                                            .LineTo(99, 101)
				                                            .LineTo(101, 99);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(101, 99)
				                                            .LineTo(199, 99)
				                                            .LineTo(201, 101)
				                                            .LineTo(201, 199)
				                                            .LineTo(199, 201)
				                                            .LineTo(101, 201)
				                                            .LineTo(99, 199)
				                                            .LineTo(99, 101)
				                                            .LineTo(101, 99);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(199, 99).LineTo(201, 101);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(201, 199).LineTo(199, 201);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(101, 201).LineTo(99, 199);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(99, 101).LineTo(101, 99);
				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 2, false);
			var ctr = new QaContainerTestRunner(70, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}

		[Test]
		public void TestWithCoincidenceTolerance()
		{
			TestWithCoincidenceTolerance(_testWs);
		}

		private static void TestWithCoincidenceTolerance(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestWithCoincidenceTolerance", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 100);
				for (var i = 0; i < 20; i++)
				{
					constr = constr.Line(0, 1);
				}

				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100.09, 110)
				                                            .LineTo(100.09, 100);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(99.91, 120)
				                                            .LineTo(99.91, 112)
				                                            .LineTo(90, 112)
				                                            .LineTo(90, 106)
				                                            .LineTo(99.91, 106)
				                                            .LineTo(99.91, 103);
				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 5, false, 1000, 0.1);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestWithNoTouch()
		{
			TestWithNoTouch(_testWs);
		}

		private static void TestWithNoTouch(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestWithNoTouch", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100, 100);
				for (var i = 0; i < 20; i++)
				{
					constr = constr.Line(0, 1);
				}

				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr = CurveConstruction.StartLine(100.09, 110)
				                                            .LineTo(100.09, 100);
				row.Shape = constr.Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				CurveConstruction constr =
					CurveConstruction.StartLine(99.91, 120)
					                 .LineTo(99.91, 112)
					                 .LineTo(90, 112)
					                 .LineTo(90, 106)
					                 .LineTo(99.51, 106)
					                 .LineTo(99.51, 103);
				row.Shape = constr.Curve;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 2.5, false, 1000, 0.1);
			using (var tr = new QaTestRunner(test))
			{
				tr.Execute();
				Assert.AreEqual(2, tr.Errors.Count);
			}

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(2, ctr.Errors.Count);
		}

		[Test]
		public void TestInvalidLines()
		{
			TestInvalidLines(_testWs);
		}

		private static void TestInvalidLines(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestInvalidLines", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature f0 = fc.CreateFeature();
				f0.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 100),
						GeometryFactory.CreatePoint(100, 200),
						GeometryFactory.CreatePoint(200, 200),
						GeometryFactory.CreatePoint(200, 100),
						GeometryFactory.CreatePoint(100, 100));
				f0.Store();
			}
			{
				{
					// will produce 2 Errors : f0 -> f1, f1 -> f0
					IFeature f1 = fc.CreateFeature();
					f1.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100.01, 100),
							GeometryFactory.CreatePoint(100.01, 110));
					f1.Store();
				}

				{
					// will produce 2 Errors : f2 -> f0 ist > 10 with buffer distance of 1
					IFeature f2 = fc.CreateFeature();
					f2.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100.01, 150),
							GeometryFactory.CreatePoint(100.01, 159.99));
					f2.Store();
				}
			}

			var test = new QaPartCoincidenceSelf(fc, 1, 10, false);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(3, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(3, container.Errors.Count);
		}

		[Test]
		public void TestInvalidSelf()
		{
			TestInvalidSelf(_testWs);
		}

		private static void TestInvalidSelf(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestInvalidSelf", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature f0 = fc.CreateFeature();
				f0.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 100),
						GeometryFactory.CreatePoint(100, 200),
						GeometryFactory.CreatePoint(200, 200),
						GeometryFactory.CreatePoint(100.1, 160),
						GeometryFactory.CreatePoint(100.1, 140),
						GeometryFactory.CreatePoint(200, 100),
						GeometryFactory.CreatePoint(100, 100));
				f0.Store();
			}

			var test = new QaPartCoincidenceSelf(fc, 1, 10, false);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(2, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);
		}

		[Test]
		public void TestValidCurve()
		{
			TestValidCurve(_testWs);
		}

		private static void TestValidCurve(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestValidCurve", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve line = CurveConstruction.StartLine(100, 100)
				                                   .LineTo(100, 195)
				                                   .CircleTo(105, 200)
				                                   .LineTo(200, 200)
				                                   .CircleTo(205, 205)
				                                   .LineTo(205, 300)
				                                   .Curve;

				row.Shape = line;
				row.Store();
			}
			{
				{
					// Curve with exact coincidence
					IFeature row = fc.CreateFeature();
					// short "tangent" needed to define start direction of circle
					IPolycurve line = CurveConstruction.StartLine(100, 194)
					                                   .LineTo(100, 195)
					                                   .CircleTo(105, 200)
					                                   .Curve;
					row.Shape = line;
					row.Store();
				}
				{
					// Curve with exact inverse coincidence
					IFeature row = fc.CreateFeature();
					// short "tangent" needed to define start direction of circle
					IPolycurve line = CurveConstruction.StartLine(205, 206)
					                                   .LineTo(205, 205)
					                                   .CircleTo(200, 200)
					                                   .Curve;
					row.Shape = line;
					row.Store();
				}
			}

			var test = new QaPartCoincidenceSelf(fc, 1, 5, false);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestInvalidCurve()
		{
			TestInvalidCurve(_testWs);
		}

		private static void TestInvalidCurve(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestInvalidCurve", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve line =
					CurveConstruction.StartLine(100, 100)
					                 .LineTo(100, 195)
					                 .CircleTo(105, 200)
					                 .LineTo(200, 200)
					                 .Curve;

				row.Shape = line;
				row.Store();
			}
			{
				{
					// Curve with exact coincidence
					IFeature row = fc.CreateFeature();
					// short "tangent" needed to define start direction of circle
					IPolycurve line =
						CurveConstruction.StartLine(100, 194)
						                 .LineTo(100, 195)
						                 .CircleTo(105.01, 200)
						                 .Curve;
					row.Shape = line;
					row.Store();
				}
			}

			var test = new QaPartCoincidenceSelf(fc, 1, 5, false);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(2, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);
		}

		[Test]
		public void TestValidFileGdb()
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaPartCoincidendeCurveTest.gdb");

			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenFileGdbWorkspace(path);

			IFeatureClass featureClass = ws.OpenFeatureClass("polygons");

			IEnvelope testArea = GeometryFactory.CreateEnvelope(2477000, 1136000, 2482000,
			                                                    1141000);
			{
				var test = new QaPartCoincidenceSelf(featureClass, 0.15, 5, false);
				var runner = new QaContainerTestRunner(10000, test);

				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);

				runner.Execute(testArea);
				Assert.AreEqual(0, runner.Errors.Count);
			}

			{
				var test = new QaPartCoincidenceSelf(featureClass, 1, 5, false);
				var runner = new QaContainerTestRunner(10000, test);

				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);

				runner.Execute(testArea);
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void TestVolumeNoExtent()
		{
			TestVolume(null);
		}

		[Test]
		public void TestVolumeWithExtent()
		{
			IEnvelope testArea = GeometryFactory.CreateEnvelope(2477000, 1136000, 2482000,
			                                                    1141000);
			TestVolume(testArea);
		}

		private static void TestVolume(IEnvelope testArea)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaPartCoincidenceVolumeTest.mdb");

			IFeatureWorkspace ws = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			IFeatureClass featureClass = ws.OpenFeatureClass("bigPolygons");

			var test = new QaPartCoincidenceSelf(featureClass, 0.15, 5, false);
			var runner = new QaContainerTestRunner(10000, test);

			if (testArea == null)
			{
				runner.Execute();
			}
			else
			{
				runner.Execute(testArea);
			}
		}

		[Test]
		public void TestTrapez()
		{
			TestTrapez(_testWs);
		}

		private static void TestTrapez(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestTrapez", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve line =
					CurveConstruction.StartLine(100, 100)
					                 .LineTo(200, 100)
					                 .LineTo(210, 200)
					                 .LineTo(180, 200)
					                 .LineTo(152, 102)
					                 .LineTo(148, 102)
					                 .LineTo(120, 200)
					                 .LineTo(100, 200)
					                 .LineTo(100, 100)
					                 .Curve;

				row.Shape = line;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 3, 20, 0.001, false, 1000, 0.5);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(2, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);
		}

		[Test]
		public void TestSmallStartSegments()
		{
			TestSmallStartSegments(_testWs);
		}

		private static void TestSmallStartSegments(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestSmallStartSegments", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve line = CurveConstruction.StartPoly(100, 100)
				                                   .LineTo(99, 100)
				                                   .LineTo(90, 100)
				                                   .LineTo(90, 110)
				                                   .LineTo(110, 110)
				                                   .LineTo(110, 100)
				                                   .ClosePolygon();

				row.Shape = line;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				IPolycurve line = CurveConstruction.StartPoly(100, 90)
				                                   .LineTo(100, 100)
				                                   .LineTo(110, 100)
				                                   .LineTo(110, 90)
				                                   .ClosePolygon();

				row.Shape = line;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 1.5, 5, 0.01, false, 1000, 0.04);
			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestSmallClosingSegments()
		{
			TestSmallClosingSegments(_testWs);
		}

		private static void TestSmallClosingSegments(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestSmallClosingSegments", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve line = CurveConstruction.StartPoly(99, 100)
				                                   .LineTo(90, 100)
				                                   .LineTo(90, 110)
				                                   .LineTo(110, 110)
				                                   .LineTo(110, 100)
				                                   .LineTo(100, 100)
				                                   .ClosePolygon();

				row.Shape = line;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				IPolycurve line = CurveConstruction.StartPoly(100, 90)
				                                   .LineTo(100, 100)
				                                   .LineTo(110, 100)
				                                   .LineTo(110, 90)
				                                   .ClosePolygon();

				row.Shape = line;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 1.5, 5, 0.01, false, 1000, 0.04);

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestNearSelfFeature()
		{
			TestNearSelfFeature(_testWs);
		}

		private static void TestNearSelfFeature(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestNearSelfFeature", fields,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve poly = CurveConstruction.StartPoly(100, 100)
				                                   .LineTo(90, 100)
				                                   .LineTo(90, 110)
				                                   .LineTo(100, 110)
				                                   .LineTo(100, 100)
				                                   .MoveTo(100.1, 100)
				                                   .LineTo(110, 100)
				                                   .LineTo(110, 110)
				                                   .LineTo(100.1, 110)
				                                   .LineTo(100.1, 100)
				                                   .ClosePolygon();

				row.Shape = poly;
				row.Store();
			}

			var test = new QaPartCoincidenceSelf(new[] {fc}, 1.5, 5, 0.01, false, 1000, 0.04);

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealData()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("PartConcidence93.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			{
				var test = new QaPartCoincidenceSelf(new[] {fc}, 2, 3, 1, false, 1000, 0.5);
				var ctr = new QaContainerTestRunner(10000, test);
				ctr.Execute();
				Assert.AreEqual(0, ctr.Errors.Count);
			}
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealData1()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120201_FGD.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			{
				var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0.01, false, 1000, 0.01);
				var ctr = new QaContainerTestRunner(10000, test);
				ctr.Execute();
				Assert.AreEqual(0, ctr.Errors.Count);
			}
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealData2()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120206.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("qacoincidence");

			{
				var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0, false, 1000, 0.01);
				var ctr = new QaContainerTestRunner(10000, test);
				ctr.Execute();
				Assert.AreEqual(0, ctr.Errors.Count);
			}
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataAG()
		{
			IWorkspace ws =
				TestDataUtils.OpenFileGdb(@"C:\data\projects\zero_length_elliptic_arcs.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("ka_arch_20151208");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5);
			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealData3()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120206.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("qacoincidence_1");

			{
				var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0, false, 1000, 0.01);
				var ctr = new QaContainerTestRunner(10000, test);
				ctr.Execute();
				Assert.AreEqual(2, ctr.Errors.Count);
			}
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealData4()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120220.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			{
				var test = new QaPartCoincidenceSelf(new[] {fc}, 1, 10, 0, false, 1000, 0.1);
				var ctr = new QaContainerTestRunner(10000, test);
				ctr.TestContainer.QaError += TestContainer_QaError;
				ctr.Execute();
				Assert.AreEqual(0, ctr.Errors.Count);
			}
		}

		private static void TestContainer_QaError(object sender, QaErrorEventArgs e)
		{
			Console.WriteLine(GeometryUtils.ToString(e.QaError.Geometry));
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF1()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120222\\f1.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0, false, 1000, 0.01);
			test.SetConstraint(0, "ObjectId = 4994");

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF2()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120222\\f2.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0, false, 1000, 0.01);
			test.SetConstraint(0, "ObjectId = 5799");

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF5()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120222\\f5.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0, false, 1000, 0.01);

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF7()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120222\\f7.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.5, 5, 0, false, 1000, 0.01);
			test.SetConstraint(0, "ObjectId = 766");

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF8()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120307\\F2.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.001, 0.01, 0.01, false, 1000, 0);
			test.SetConstraint(0, "ObjectId in (5398, 5966, 5971)");

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(3, ctr.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF9()
		{
			IWorkspace ws = TestDataUtils.OpenFileGdb("20120307\\F1.gdb");
			IFeatureClass fc =
				((IFeatureWorkspace) ws).OpenFeatureClass("xeig_fgdl_GISADMIN_fgd_we_f");

			var test = new QaPartCoincidenceSelf(new[] {fc}, 0.001, 0.01, 0.01, false, 1000, 0);
			test.SetConstraint(0, "ObjectId in (4582, 4579)");

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}
	}
}

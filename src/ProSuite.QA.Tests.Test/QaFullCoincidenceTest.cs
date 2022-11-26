using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaFullCoincidenceTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("TestFullCoincidence");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanDetectCircularArcDifference()
		{
			const double x0 = 100;
			const double y0 = 100;
			const double dx = 30;
			const double dy = 30;

			ICurve line = CurveConstruction.StartLine(x0, y0)
			                               .LineTo(x0 + dx / 2, y0)
			                               .LineTo(x0 + dx, y0 + dy)
			                               .Curve;
			GeometryUtils.Simplify(line);

			ICurve circularArc = CurveConstruction.StartLine(x0, y0)
			                                      .LineTo(x0 + dx / 2, y0)
			                                      .CircleTo(x0 + dx, y0 + dy)
			                                      .Curve;
			GeometryUtils.Simplify(circularArc);

			Console.WriteLine(line.Length);
			Console.WriteLine(circularArc.Length);

			Assert.IsFalse(((IRelationalOperator) line).Equals(circularArc));
			Assert.IsFalse(((IRelationalOperator) circularArc).Equals(line));
		}

		[Test]
		public void CanDetectCircularArcEquality()
		{
			const double x0 = 100;
			const double y0 = 100;
			const double dx = 30;
			const double dy = 30;

			ICurve line = CurveConstruction.StartLine(x0, y0)
			                               .LineTo(x0 + dx, y0 + dy)
			                               .Curve;
			GeometryUtils.Simplify(line);

			// construct a straight circular-arc (CircleTo needs preceding segment)
			ICurve circularArc = CurveConstruction.StartLine(x0, y0)
			                                      .LineTo(x0, y0)
			                                      .CircleTo(x0 + dx, y0 + dy)
			                                      .Curve;
			GeometryUtils.Simplify(circularArc);

			Console.WriteLine(line.Length);
			Console.WriteLine(circularArc.Length);

			Assert.IsTrue(((IRelationalOperator) line).Equals(circularArc));
			Assert.IsTrue(((IRelationalOperator) circularArc).Equals(line));
		}

		[Test]
		public void CanDetectBezierDifference()
		{
			const double x0 = 100;
			const double y0 = 100;
			const double dx = 30;
			const double dy = 30;
			const double f = 0.8;

			ICurve line = CurveConstruction.StartLine(x0, y0)
			                               .LineTo(x0 + dx, y0 + dy)
			                               .Curve;

			ICurve bezierLinear = CurveConstruction.StartLine(x0, y0)
			                                       .BezierTo(x0 + 1.0 / 3.0 * dx,
			                                                 y0 + 1.0 / 3.0 * dy,
			                                                 x0 + 2.0 / 3.0 * dx,
			                                                 y0 + 2.0 / 3.0 * dy,
			                                                 x0 + dx,
			                                                 y0 + dy)
			                                       .Curve;
			GeometryUtils.Simplify(bezierLinear);

			IPoint qPoint = new PointClass();
			bezierLinear.QueryPoint(esriSegmentExtension.esriNoExtension, 0.8, true,
			                        qPoint);
			double ddx = qPoint.X - f * dx - x0;
			double ddy = qPoint.Y - f * dy - y0;
			Assert.IsTrue(Math.Abs(ddx) < 1.0e-8);
			Assert.IsTrue(Math.Abs(ddy) < 1.0e-8);

			Assert.IsTrue(((IRelationalOperator) bezierLinear).Equals(line));
			Assert.IsTrue(((IRelationalOperator) line).Equals(bezierLinear));
			Assert.IsFalse(((IClone) line).IsEqual((IClone) bezierLinear));
			Assert.IsFalse(((IClone) bezierLinear).IsEqual((IClone) line));

			ICurve bezierCurved = CurveConstruction.StartLine(x0, y0)
			                                       .BezierTo(x0 + 1.0 / 3.0 * dx + 0.01,
			                                                 y0 + 1.0 / 3.0 * dy,
			                                                 x0 + 2.0 / 3.0 * dx,
			                                                 y0 + 2.0 / 3.0 * dy,
			                                                 x0 + dx,
			                                                 y0 + dy)
			                                       .Curve;
			GeometryUtils.Simplify(bezierCurved);

			Console.WriteLine(GeometryUtils.ToXmlString(line));
			Console.WriteLine(GeometryUtils.ToXmlString(bezierCurved));

			Assert.IsFalse(((IRelationalOperator) bezierCurved).Equals(line));
			Assert.IsFalse(((IRelationalOperator) line).Equals(bezierCurved));
			Assert.IsFalse(((IClone) line).IsEqual((IClone) bezierCurved));
			Assert.IsFalse(((IClone) bezierCurved).IsEqual((IClone) line));
		}

		[Test]
		public void TestFullCoincidence()
		{
			TestFullCoincidence(_testWs);
		}

		private static void TestFullCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestFullCoincidence1",
				                                      fields);
			IFeatureClass coincidence =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestFullCoincidence2",
				                                      fields);

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
				// Features of coincidence
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100, 100),
							GeometryFactory.CreatePoint(101, 201));
					row.Store();
				}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(101, 201),
							GeometryFactory.CreatePoint(199, 200));
					row.Store();
				}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(199, 200),
							GeometryFactory.CreatePoint(201, 99));
					row.Store();
				}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(201, 99),
							GeometryFactory.CreatePoint(99, 101));
					row.Store();
				}
			}

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(coincidence), 2, false);

			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(10000, test);
			containerRunner.Execute();
			Assert.AreEqual(0, containerRunner.Errors.Count);
		}

		[Test]
		public void TestNotFullCoincidence()
		{
			TestNotFullCoincidence(_testWs);
		}

		private static void TestNotFullCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestNotFullCoincidence1",
				                                      fields);
			IFeatureClass coincidence =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestNotFullCoincidence2",
				                                      fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					CurveConstruction.StartLine(100, 100, 50)
					                 .LineTo(100, 200, 50).LineTo(200, 200, 50)
					                 .LineTo(200, 100, 50)
					                 .LineTo(100, 100, 50)
					                 .Curve;
				row.Store();
			}
			{
				// Features of coincidence
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						CurveConstruction.StartLine(100, 100, 50).LineTo(101, 201, 50)
						                 .Curve;
					row.Store();
				}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						CurveConstruction.StartLine(101, 201, 50).LineTo(199, 200, 50)
						                 .Curve;
					row.Store();
				}
				//{
				//    IFeature row = coincidence.CreateFeature();
				//    row.Shape =
				//        GeometryFactory.CreateLine(
				//            GeometryFactory.CreatePoint(199, 200),
				//            GeometryFactory.CreatePoint(201, 99));
				//    row.Store();
				//}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						CurveConstruction.StartLine(201, 99, 50).LineTo(99, 101, 50).Curve;
					row.Store();
				}
			}

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(coincidence), 2, false);
			var testRunner = new QaTestRunner(test);
			testRunner.KeepGeometry = true;
			testRunner.Execute();
			Assert.AreEqual(1, testRunner.Errors.Count);
			Assert.IsTrue(testRunner.ErrorGeometries[0].Envelope.ZMin > 49);

			var containerRunner = new QaContainerTestRunner(10000, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);
		}

		[Test]
		public void TestTiledWithDifferentNear()
		{
			TestTiledWithDifferentNear(_testWs);
		}

		private static void TestTiledWithDifferentNear(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "DiffNear0", fields);
			IFeatureClass near1 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "DiffNear1", fields);

			IFeatureClass near2 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "DiffNear2", fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 100),
						// Feature ends near tileborder x=200 (see below)
						GeometryFactory.CreatePoint(199.8, 110));
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(210, 100),
						GeometryFactory.CreatePoint(300, 110));
				row.Store();
			}
			{
				// Features of coincidence
				{
					IFeature row = near1.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100, 100),
							GeometryFactory.CreatePoint(199.8, 110));
					row.Store();
				}
				{
					IFeature row = near1.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(210, 100),
							GeometryFactory.CreatePoint(300, 110));
					row.Store();
				}
				// Features of coincidence
				{
					IFeature row = near2.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100, 100),
							GeometryFactory.CreatePoint(199.8, 110));
					row.Store();
				}
				{
					IFeature row = near2.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(210, 100),
							GeometryFactory.CreatePoint(300, 110));
					row.Store();
				}
			}

			var test1 = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                  ReadOnlyTableFactory.Create(near1), 0.1, false);
			var test2 = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                  ReadOnlyTableFactory.Create(near2), 2, false);

			var testRunner = new QaTestRunner(test1);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);

			testRunner = new QaTestRunner(test2);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(100, test1, test2);
			IEnvelope box = new EnvelopeClass();
			box.PutCoords(100, 90, 300, 180);
			// Make sure that a tile border is at x=200
			box.SpatialReference =
				fc.Fields.get_Field(fc.FindField("Shape")).GeometryDef.SpatialReference;
			box.SnapToSpatialReference();

			containerRunner.Execute(box);
			Assert.AreEqual(0, containerRunner.Errors.Count);
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void PerformanceTest()
		{
			IWorkspace dtmWs = TestDataUtils.OpenTopgisTlm();
			IFeatureClass fc =
				((IFeatureWorkspace) dtmWs).OpenFeatureClass("TOPGIS_TLM.TLM_BODENBEDECKUNG");

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(fc), 0.1, false);
			test.SetConstraint(0, "Objektart = 12");
			test.SetConstraint(1, "Objektart = 12");
			//test.SetConstraint(0, "ObjectId = 1438259");
			//test.SetConstraint(1, "ObjectId = 1438259");
			IEnvelope box = new EnvelopeClass();
			box.PutCoords(2479000, 1145000, 2520000, 1200000);

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(box);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		[Ignore("Relies on local data")]
		public void TestTiledFullCoincidence1326()
		{
			// TODO add gdb to repository?
			_testWs = (IFeatureWorkspace) TestDataUtils.OpenFileGdb(@"topgis\Topgis1326.gdb");

			IFeatureClass fcSee = _testWs.OpenFeatureClass("see1");
			IFeatureClass fcSee_copy = _testWs.OpenFeatureClass("see1_copy");
			IFeatureClass fcUfer = _testWs.OpenFeatureClass("gew3");

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcUfer),
			                                 ReadOnlyTableFactory.Create(fcSee), 0.1, false);
			var test1 = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcUfer),
			                                  ReadOnlyTableFactory.Create(fcSee_copy), 2, false);

			var runner = new QaContainerTestRunner(10000, test, test1);
			IEnvelope box = new EnvelopeClass();
			box.PutCoords(2584999, 1097999, 2602501, 1110001);
			box.SpatialReference =
				fcSee.Fields.get_Field(fcSee.FindField("Shape")).GeometryDef.SpatialReference;
			box.SnapToSpatialReference();
			//container.Execute(box);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		[Ignore("Relies on local data")]
		public void TestFullCoincidenceIssueCom280()
		{
			// TODO add gdb to repository?
			_testWs =
				(IFeatureWorkspace)
				TestDataUtils.OpenFileGdb(@"esri_kreisboegen.gdb\esri_kreisboegen.gdb");

			IFeatureClass fc = _testWs.OpenFeatureClass("wf_def");
			IFeatureClass reference = _testWs.OpenFeatureClass("va_wflinie");

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(reference), 0.1, false);

			var runner = new QaContainerTestRunner(10000, test);
			IEnvelope box = new EnvelopeClass();
			box.PutCoords(663500, 257600, 665600, 259200);
			runner.KeepGeometry = true;
			runner.Execute(box);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void TestTiledFullCoincidence()
		{
			TestTiledFullCoincidence(_testWs);
		}

		private static void TestTiledFullCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields0 = new FieldsClass();
			fields0.AddField(FieldUtils.CreateOIDField());
			fields0.AddField(FieldUtils.CreateShapeField(
				                 "Shape", esriGeometryType.esriGeometryPolygon,
				                 SpatialReferenceUtils.CreateSpatialReference
				                 ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                  true), 1000));
			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestTiledFullCoincidence1",
				                                      fields0);
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));
			IFeatureClass coincidence =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestTiledFullCoincidence2",
				                                      fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolyline line =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(150, 100),
						GeometryFactory.CreatePoint(300, 100),
						GeometryFactory.CreatePoint(300, 150),
						GeometryFactory.CreatePoint(100, 150),
						GeometryFactory.CreatePoint(100, 100),
						GeometryFactory.CreatePoint(150, 100)
					);
				ISegmentCollection poly = new PolygonClass();
				poly.AddSegmentCollection((ISegmentCollection) line);
				row.Shape = (IPolygon) poly;
				row.Store();
			}
			{
				// Features of coincidence
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100, 120),
							GeometryFactory.CreatePoint(100, 100),
							GeometryFactory.CreatePoint(199, 100)
						);
					row.Store();
				}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(199, 100),
							GeometryFactory.CreatePoint(300, 100),
							GeometryFactory.CreatePoint(300, 150),
							GeometryFactory.CreatePoint(100, 150),
							GeometryFactory.CreatePoint(100, 120)
						);
					row.Store();
				}
			}

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(coincidence), 2, false);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);

			var containerRunner = new QaContainerTestRunner(100, test);
			containerRunner.Execute();
			Assert.AreEqual(0, containerRunner.Errors.Count);
		}

		[Test]
		public void TestCurveCoincidence()
		{
			TestCurveCoincidence(_testWs);
		}

		private static void TestCurveCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestCurveCoincidence1",
				                                      fields);
			IFeatureClass coincidence =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestCurveCoincidence2",
				                                      fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				IPolycurve line =
					CurveConstruction.StartLine(100, 100).LineTo(100, 195).CircleTo(105, 200)
					                 .LineTo(200, 200).Curve;

				row.Shape = line;
				row.Store();

				// Make temporary circular arc based on tree points
			}
			{
				// Features of coincidence
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(100, 100),
							GeometryFactory.CreatePoint(100, 195));
					row.Store();
				}
				{
					IFeature row = coincidence.CreateFeature();
					row.Shape =
						GeometryFactory.CreateLine(
							GeometryFactory.CreatePoint(105, 200),
							GeometryFactory.CreatePoint(200, 200));
					row.Store();
				}
			}

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(coincidence), 2, false);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(1, testRunner.Errors.Count);

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);
		}

		[Test]
		public void TestConditionCoincidence()
		{
			TestConditionCoincidence(_testWs);
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
				                 true), 1000));

			IFeatureClass fc1 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence1",
				                                      fields);
			IFeatureClass fc2 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence2",
				                                      fields);

			IFeatureClass fc3 =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConditionCoincidence3",
				                                      fields);

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
			{
				IFeature row = fc3.CreateFeature();
				row.set_Value(2, 1);
				row.Shape = CurveConstruction.StartLine(100, 100).LineTo(200, 200).Curve;
				row.Store();
			}

			// test without ignore conditions --> fully covered
			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc1),
			                                 new[]
			                                 {
				                                 ReadOnlyTableFactory.Create(fc2),
				                                 ReadOnlyTableFactory.Create(fc3)
			                                 }, 1, false);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(0, testRunner.Errors.Count);

			// Same test with ignore conditions --> part not covered
			test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc1),
			                             new[]
			                             {
				                             ReadOnlyTableFactory.Create(fc2),
				                             ReadOnlyTableFactory.Create(fc3)
			                             }, 1, false);
			bool success;
			try
			{
				test.IgnoreNeighborConditions = new[] { "too", "many", "conditions" };
				success = true;
			}
			catch
			{
				success = false;
			}

			Assert.False(success);
			test.IgnoreNeighborConditions = new[]
			                                {
				                                "G1.LandID = G2.LandID", "G1.LandID = G2.OtherID"
			                                };

			testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(1, testRunner.Errors.Count);
		}

		[Test]
		public void TestCurveCurveCoincidence()
		{
			TestCurveCurveCoincidence(_testWs);
		}

		private static void TestCurveCurveCoincidence(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestCurveCurveCoincidence1",
				                                      fields);
			IFeatureClass coincidence =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestCurveCurveCoincidence2",
				                                      fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 200),
						GeometryFactory.CreatePoint(105, 195));
				row.Store();
			}
			{
				IFeature row = coincidence.CreateFeature();
				IPolycurve line =
					CurveConstruction.StartLine(100, 100).LineTo(100, 195).CircleTo(105, 200)
					                 .LineTo(200, 200).Curve;

				row.Shape = line;
				row.Store();

				// Make temporary circular arc based on tree points
			}

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fc),
			                                 ReadOnlyTableFactory.Create(coincidence), 1, false);
			var testRunner = new QaTestRunner(test);
			testRunner.Execute();
			Assert.AreEqual(2, testRunner.Errors.Count);

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);
		}

		[Test]
		public void CanFindMissingParts()
		{
			CanFindMissingParts(_testWs);
		}

		[Test]
		[Ignore("Relies on local data")]
		public void Test_TOP4422_TGS483()
		{
			// TODO add gdb to repository?
			var testWs = (IFeatureWorkspace) TestDataUtils.OpenFileGdb("V200_TOP4422.gdb");

			IFeatureClass fcWater = testWs.OpenFeatureClass("stagnant_water01");
			IFeatureClass fcCover = testWs.OpenFeatureClass("landcover01");

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcCover),
			                                 ReadOnlyTableFactory.Create(fcWater), 1, false);
			var cnt = new QaContainerTestRunner(10000, test);
			int errorCount = cnt.Execute();
			Assert.IsTrue(errorCount > 0);

			test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcCover),
			                             ReadOnlyTableFactory.Create(fcWater), 2, false);
			cnt = new QaContainerTestRunner(10000, test);
			errorCount = cnt.Execute();
			Assert.AreEqual(0, errorCount);

			fcWater = testWs.OpenFeatureClass("stagnant_water02");
			fcCover = testWs.OpenFeatureClass("landcover02");

			test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcWater),
			                             ReadOnlyTableFactory.Create(fcCover), 0, false);
			cnt = new QaContainerTestRunner(10000, test);
			errorCount = cnt.Execute();
			Assert.AreEqual(0, errorCount);

			test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcWater),
			                             ReadOnlyTableFactory.Create(fcCover), 2, false);
			cnt = new QaContainerTestRunner(10000, test);
			errorCount = cnt.Execute();
			Assert.AreEqual(0, errorCount);

			fcWater = testWs.OpenFeatureClass("stagnant_water03");
			fcCover = testWs.OpenFeatureClass("landcover03");

			test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcWater),
			                             ReadOnlyTableFactory.Create(fcCover), 1, false);
			cnt = new QaContainerTestRunner(10000, test);
			errorCount = cnt.Execute();
			Assert.IsTrue(errorCount > 0);

			test = new QaFullCoincidence(ReadOnlyTableFactory.Create(fcWater),
			                             ReadOnlyTableFactory.Create(fcCover), 2, false);
			cnt = new QaContainerTestRunner(10000, test);
			errorCount = cnt.Execute();
			Assert.AreEqual(0, errorCount);
		}

		[Test]
		[Ignore("Relies on local data")]
		public void TestCommonsIssueUnknownId()
		{
			var testWs = (IFeatureWorkspace) TestDataUtils.OpenPgdb("fullCoincidence.mdb");

			IFeatureClass eo = testWs.OpenFeatureClass("AVZH_EINZELOBJEKTE_L");

			var test = new QaFullCoincidence(ReadOnlyTableFactory.Create(eo),
			                                 new[] { ReadOnlyTableFactory.Create(eo) }, 2, 10000);
			test.SetConstraint(0, "ARTZHID = 9");
			test.SetConstraint(1, "ARTZHID = 32");

			var cnt = new QaContainerTestRunner(10000, test);
			cnt.Execute();

			int fieldIndex = eo.FindField("ARTZHID");
			foreach (QaError error in cnt.Errors)
			{
				foreach (InvolvedRow row in error.InvolvedRows)
				{
					IFeature f = eo.GetFeature(row.OID);
					var artzhid = (int) f.get_Value(fieldIndex);
					Assert.AreEqual(9, artzhid);
				}
			}
		}

		[Test]
		public void CanAllowNearlyCoincidentSectionInLeftTile()
		{
			const string testName = "CanAllowNearlyCoincidentSectionInLeftTile";

			IFeatureClass testedClass =
				CreateFeatureClass(string.Format("{0}_tested", testName),
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass referenceClass =
				CreateFeatureClass(string.Format("{0}_reference", testName),
				                   esriGeometryType.esriGeometryPolyline);

			IFeature testedRow = testedClass.CreateFeature();
			testedRow.Shape =
				CurveConstruction.StartLine(201, 150).LineTo(201, 50).Curve;
			testedRow.Store();

			IFeature referenceRow = referenceClass.CreateFeature();
			referenceRow.Shape =
				CurveConstruction.StartLine(199, 150).LineTo(199, 50).Curve;
			referenceRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaFullCoincidence(
				ReadOnlyTableFactory.Create(testedClass),
				new List<IReadOnlyFeatureClass> { ReadOnlyTableFactory.Create(referenceClass) }, 3);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowNearlyCoincidentSectionRightTile()
		{
			const string testName = "CanAllowNearlyCoincidentSectionRightTile";

			IFeatureClass testedClass =
				CreateFeatureClass(string.Format("{0}_tested", testName),
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass referenceClass =
				CreateFeatureClass(string.Format("{0}_reference", testName),
				                   esriGeometryType.esriGeometryPolyline);

			IFeature testedRow = testedClass.CreateFeature();
			testedRow.Shape =
				CurveConstruction.StartLine(199, 150).LineTo(199, 50).Curve;
			testedRow.Store();

			IFeature referenceRow = referenceClass.CreateFeature();
			referenceRow.Shape = CurveConstruction.StartLine(201, 150)
			                                      .LineTo(201, 50)
			                                      .Curve;
			referenceRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaFullCoincidence(
				ReadOnlyTableFactory.Create(testedClass),
				new List<IReadOnlyFeatureClass> { ReadOnlyTableFactory.Create(referenceClass) }, 3);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowNearlyCoincidentSectionnSameTile()
		{
			const string testName = "CanAllowNearlyCoincidentSectionnSameTile";

			IFeatureClass testedClass =
				CreateFeatureClass(string.Format("{0}_tested", testName),
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass referenceClass =
				CreateFeatureClass(string.Format("{0}_reference", testName),
				                   esriGeometryType.esriGeometryPolyline);

			IFeature testedRow = testedClass.CreateFeature();
			testedRow.Shape =
				CurveConstruction.StartLine(197, 150).LineTo(197, 50).Curve;
			testedRow.Store();

			IFeature referenceRow = referenceClass.CreateFeature();
			referenceRow.Shape =
				CurveConstruction.StartLine(199, 150).LineTo(199, 50).Curve;
			referenceRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaFullCoincidence(
				ReadOnlyTableFactory.Create(testedClass),
				new List<IReadOnlyFeatureClass> { ReadOnlyTableFactory.Create(referenceClass) }, 3);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType type,
		                                         bool zAware = false)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape",
					type,
					sref, 1000, zAware));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}

		private static void CanFindMissingParts(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "CanFindMissingParts",
				                                      fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.Shape = CurveConstruction.StartLine(100, 100)
				                             .LineTo(100, 195)
				                             .LineTo(105, 200)
				                             .LineTo(200, 200)
				                             .Curve;

				row.Store();
			}

			var test = new QaPolycurveCoincidenceTest(ReadOnlyTableFactory.Create(fc));
			test.Execute();

			var container = new TestContainer();
			container.AddTest(test);
			container.Execute();
		}

		[Test]
		public void CanGetCorrectSearchFeatures()
		{
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFeatureWorkspace featureWorkspace = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaEdgeMatchCrossingLinesTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, spatialReference,
				                1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(featureWorkspace, "Lines",
				fields);

			AddFeature(fc, CurveConstruction.StartLine(0, 0).LineTo(1, 1).Curve);
			AddFeature(fc, CurveConstruction.StartLine(2, 50).LineTo(53, 48).Curve);
			AddFeature(fc, CurveConstruction.StartLine(47, 12).LineTo(45, 60).Curve);

			var test = new QaEnvelopeIntersects(ReadOnlyTableFactory.Create(fc));

			Assert.AreEqual(2, Run(test, 1000).Count);
			Assert.AreEqual(2, Run(test, 10).Count);
			Assert.AreEqual(2, Run(test, 1.1).Count);
		}

		[NotNull]
		private static IList<QaError> Run([NotNull] ITest test, double? tileSize = null)
		{
			Console.WriteLine(@"Tile size: {0}",
			                  tileSize == null ? "<null>" : tileSize.ToString());
			const string newLine = "\n";
			// r# unit test output adds 2 lines for Environment.NewLine
			Console.Write(newLine);

			QaTestRunnerBase runner = tileSize == null
				                          ? (QaTestRunnerBase) new QaTestRunner(test)
				                          : new QaContainerTestRunner(tileSize.Value, test);
			runner.Execute();

			return runner.Errors;
		}

		private static void AddFeature([NotNull] IFeatureClass featureClass,
		                               [NotNull] IGeometry geometry)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			feature.Store();
		}

		private class QaEnvelopeIntersects : ContainerTest
		{
			private readonly IReadOnlyFeatureClass _lineClass;
			private ISpatialFilter _filter;
			private QueryFilterHelper _helper;

			public QaEnvelopeIntersects(IReadOnlyFeatureClass lineClass)
				: base(lineClass)
			{
				_lineClass = lineClass;
			}

			private WKSEnvelope _currentTile;

			protected override void BeginTileCore(BeginTileParameters parameters)
			{
				if (parameters.TileEnvelope != null)
				{
					parameters.TileEnvelope.QueryWKSCoords(out _currentTile);
				}
			}

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				var errorCount = 0;
				Init();

				var feature = (IReadOnlyFeature) row;

				_filter.Geometry = feature.Shape.Envelope;

				foreach (IReadOnlyRow otherRow in Search(_lineClass, _filter, _helper))
				{
					var otherFeature = (IReadOnlyFeature) otherRow;
					if (otherFeature == feature)
					{
						continue;
					}

					string desc = string.Format("{0},{1}", feature.OID, otherFeature.OID);
					errorCount += ReportError(
						desc, InvolvedRowUtils.GetInvolvedRows(feature, otherFeature),
						otherFeature.Shape, null, null);
				}

				return errorCount;
			}

			private void Init()
			{
				if (_filter != null)
				{
					return;
				}

				IList<ISpatialFilter> filters;
				IList<QueryFilterHelper> helpers;
				CopyFilters(out filters, out helpers);

				_filter = filters[0];
				_helper = helpers[0];

				_filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}

		private class QaPolycurveCoincidenceTest : QaPolycurveCoincidenceBase
		{
			public QaPolycurveCoincidenceTest([NotNull] IReadOnlyFeatureClass fc)
				: base(new[] { fc }, 0, new ConstantFeatureDistanceProvider(0), false) { }

			protected override bool IsDirected => false;

			public override int Execute(IReadOnlyRow row)
			{
				var feature = (IReadOnlyFeature) row;
				IIndexedSegments geometry = IndexedSegmentUtils.GetIndexedGeometry(feature, true);

				OneExistingPartTest(feature, geometry);
				TwoDistinctExistingPartTest(feature, geometry);
				TwoTouchingExistingPartTest(feature, geometry);
				TwoOverlappingExistingPartTest(feature, geometry);

				TwoAdjacentSegmentsTest(feature, geometry);
				TwoSeparatedSegmentsTest(feature, geometry);

				return 0;
			}

			private static void OneExistingPartTest(IReadOnlyFeature feature,
			                                        IIndexedSegments geometry)
			{
				double fullLength;
				var parts = new SortedDictionary<SegmentPart, SegmentParts>();
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 0);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{
						var neighbor = new SegmentPart(-1, -1, 0.5, 0.8, false);
						neighbors.Add(neighbor);
					}
					parts.Add(part, neighbors);
					fullLength = segmentProxy.Length;
				}

				IList<Subcurve> missing = Subcurve.GetMissingSubcurves(feature,
					geometry, parts,
					null);
				Assert.AreEqual(missing.Count, 2);
				{
					double l = missing[0].GetLength();
					Assert.IsTrue(Math.Abs(l - 0.5 * fullLength) < 1.0e-5);
				}
				{
					double l = missing[1].GetLength();
					Assert.IsTrue(Math.Abs(l - 0.2 * fullLength) < 1.0e-5);
				}
			}

			private static void TwoDistinctExistingPartTest(IReadOnlyFeature feature,
			                                                IIndexedSegments geometry)
			{
				var parts = new SortedDictionary<SegmentPart, SegmentParts>();
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 0);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{
						var neighbor = new SegmentPart(-1, -1, 0.5, 0.8, false);
						neighbors.Add(neighbor);
					}
					{
						var neighbor = new SegmentPart(-1, -1, 0.2, 0.4, false);
						neighbors.Add(neighbor);
					}
					parts.Add(part, neighbors);
				}

				IList<Subcurve> missing = Subcurve.GetMissingSubcurves(feature,
					geometry, parts,
					null);
				Assert.AreEqual(3, missing.Count);
			}

			private static void TwoTouchingExistingPartTest(IReadOnlyFeature feature,
			                                                IIndexedSegments geometry)
			{
				var parts = new SortedDictionary<SegmentPart, SegmentParts>();
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 0);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{
						var neighbor = new SegmentPart(-1, -1, 0.5, 0.8, false);
						neighbors.Add(neighbor);
					}
					{
						var neighbor = new SegmentPart(-1, -1, 0.2, 0.5, false);
						neighbors.Add(neighbor);
					}
					parts.Add(part, neighbors);
				}

				IList<Subcurve> missing = Subcurve.GetMissingSubcurves(feature,
					geometry, parts,
					null);
				Assert.AreEqual(2, missing.Count);
			}

			private static void TwoOverlappingExistingPartTest(IReadOnlyFeature feature,
			                                                   IIndexedSegments
				                                                   geometry)
			{
				double fullLength;
				var parts = new SortedDictionary<SegmentPart, SegmentParts>();
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 0);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{
						var neighbor = new SegmentPart(-1, -1, 0.5, 0.8, false);
						neighbors.Add(neighbor);
					}
					{
						var neighbor = new SegmentPart(-1, -1, 0.2, 0.9, false);
						neighbors.Add(neighbor);
					}
					parts.Add(part, neighbors);
					fullLength = segmentProxy.Length;
				}

				IList<Subcurve> missing = Subcurve.GetMissingSubcurves(feature,
					geometry, parts,
					null);
				Assert.AreEqual(missing.Count, 2);
				{
					double l = missing[0].GetLength();
					Assert.IsTrue(Math.Abs(l - 0.2 * fullLength) < 1.0e-5);
				}
				{
					double l = missing[1].GetLength();
					Assert.IsTrue(Math.Abs(l - 0.1 * fullLength) < 1.0e-5);
				}
			}

			private static void TwoAdjacentSegmentsTest(IReadOnlyFeature feature,
			                                            IIndexedSegments geometry)
			{
				var parts =
					new SortedDictionary<SegmentPart, SegmentParts>(
						new SegmentPartComparer());
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 0);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{
						var neighbor = new SegmentPart(-1, -1, 0, 0.2, false);
						neighbors.Add(neighbor);
					}
					parts.Add(part, neighbors);
				}
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 1);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{ }
					parts.Add(part, neighbors);
				}

				IList<Subcurve> missing = Subcurve.GetMissingSubcurves(feature,
					geometry, parts,
					null);
				Assert.AreEqual(1, missing.Count);
			}

			private static void TwoSeparatedSegmentsTest(IReadOnlyFeature feature,
			                                             IIndexedSegments geometry)
			{
				var parts =
					new SortedDictionary<SegmentPart, SegmentParts>(
						new SegmentPartComparer());
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 0);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{
						var neighbor = new SegmentPart(-1, -1, 0, 0.2, false);
						neighbors.Add(neighbor);
					}
					parts.Add(part, neighbors);
				}
				{
					SegmentProxy segmentProxy = geometry.GetSegment(0, 2);
					var part = new SegmentPart(segmentProxy, 0, 1, true);
					var neighbors = new SegmentParts();
					{ }
					parts.Add(part, neighbors);
				}

				IList<Subcurve> missing = Subcurve.GetMissingSubcurves(feature,
					geometry, parts,
					null);
				Assert.AreEqual(2, missing.Count);
			}

			protected override NeighborhoodFinder GetNeighborhoodFinder(
				IFeatureRowsDistance rowsDistance, IReadOnlyFeature feature, int tableIndex,
				IReadOnlyFeature neighbor, int neighborTableIndex)
			{
				return new TestNeighborHoodFinder(rowsDistance, feature, tableIndex, neighbor,
				                                  neighborTableIndex);
			}

			private class TestNeighborHoodFinder : NeighborhoodFinder
			{
				public TestNeighborHoodFinder(
					IFeatureRowsDistance rowsDistance,
					[NotNull] IReadOnlyFeature feature, int tableIndex,
					[CanBeNull] IReadOnlyFeature neighbor, int neighborTableIndex)
					: base(rowsDistance, feature, tableIndex, neighbor, neighborTableIndex) { }

				protected override bool VerifyContinue(SegmentProxy seg0,
				                                       SegmentProxy seg1,
				                                       SegmentNeighbors processed1,
				                                       SegmentParts partsOfSeg0,
				                                       bool coincident)
				{
					return false;
				}
			}
		}
	}
}

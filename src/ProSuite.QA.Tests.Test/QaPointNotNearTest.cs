using System;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaPointNotNearTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestPointNotNearPolygon()
		{
			IFeatureWorkspace testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaPointNotNearTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass polyFc = DatasetUtils.CreateSimpleFeatureClass(
				testWs, "polygonFc", fields);

			IFeatureClass pointFc0 = CreatePointClass(testWs, "pointFc0");
			IFeatureClass pointFc1 = CreatePointClass(testWs, "pointFc1");
			IFeatureClass pointFc2 = CreatePointClass(testWs, "pointFc2");

			IFeature polyFeature = polyFc.CreateFeature();

			polyFeature.Shape =
				CurveConstruction.StartPoly(0, 0, 0)
				                 .LineTo(100, 0, 10)
				                 .LineTo(50, 100, 10)
				                 .ClosePolygon();
			polyFeature.Store();

			IFeature p0 = pointFc0.CreateFeature();
			p0.Shape = GeometryFactory.CreatePoint(20, 1.5, 0);
			p0.Store();

			IFeature p1 = pointFc1.CreateFeature();
			p1.Shape = GeometryFactory.CreatePoint(101, -1, 0);
			p1.Store();

			IFeature p2 = pointFc2.CreateFeature();
			p2.Shape = GeometryFactory.CreatePoint(1, 99, 0);
			p2.Store();

			var test1 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 1.0);
			var runner = new QaContainerTestRunner(1000, test1);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointWithin", 2);

			var test2 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 1.0);
			test2.GeometryComponents = new[] { GeometryComponent.Boundary };
			runner = new QaContainerTestRunner(1000, test2);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test3 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test3.GeometryComponents = new[] { GeometryComponent.Boundary };
			runner = new QaContainerTestRunner(1000, test3);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test3_1 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc1), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test3.GeometryComponents = new[] { GeometryComponent.Boundary };
			runner = new QaContainerTestRunner(1000, test3_1);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test4 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test4.GeometryComponents = new[] { GeometryComponent.Vertices };
			runner = new QaContainerTestRunner(1000, test4);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test5 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 22.0);
			test5.GeometryComponents = new[] { GeometryComponent.Vertices };
			runner = new QaContainerTestRunner(1000, test5);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test6 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc),
				44); // dist is 43.74
			test6.GeometryComponents = new[] { GeometryComponent.Centroid };
			runner = new QaContainerTestRunner(1000, test6);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test8 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 43);
			test8.GeometryComponents = new[] { GeometryComponent.Centroid };
			runner = new QaContainerTestRunner(1000, test8);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test7 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc),
				44); // dist is 43.74
			test7.GeometryComponents = new[] { GeometryComponent.LabelPoint };
			runner = new QaContainerTestRunner(1000, test7);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test9 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0), ReadOnlyTableFactory.Create(polyFc), 43);
			test9.GeometryComponents = new[] { GeometryComponent.LabelPoint };
			runner = new QaContainerTestRunner(1000, test9);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test10 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc0),
				new[] { ReadOnlyTableFactory.Create(polyFc) }, 43, "43", null);
			test10.GeometryComponents = new[] { GeometryComponent.LabelPoint };
			runner = new QaContainerTestRunner(1000, test10);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test11 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc2), ReadOnlyTableFactory.Create(polyFc), 0);
			runner = new QaContainerTestRunner(1000, test11);
			runner.Execute();
			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanTestPointNotNearPolyline()
		{
			IFeatureWorkspace testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaPointNotNearTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass polyFc = DatasetUtils.CreateSimpleFeatureClass(
				testWs, "polylineFc", fields);

			IFeatureClass pointFc = CreatePointClass(testWs, "pointFc");

			IFeature polyFeature = polyFc.CreateFeature();

			polyFeature.Shape = CurveConstruction.StartLine(0, 0, 0)
			                                     .LineTo(100, 0, 10)
			                                     .LineTo(50, 100, 10)
			                                     .Curve;
			polyFeature.Store();

			IFeature p1 = pointFc.CreateFeature();
			p1.Shape = GeometryFactory.CreatePoint(90, 0, 0);
			p1.Store();

			var test1 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 1.0);
			var runner = new QaContainerTestRunner(1000, test1);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test2 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 1.0);
			test2.GeometryComponents = new[] { GeometryComponent.Boundary };
			runner = new QaContainerTestRunner(1000, test2);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test3 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 95);
			test3.GeometryComponents = new[] { GeometryComponent.Boundary };
			runner = new QaContainerTestRunner(1000, test3);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test4 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test4.GeometryComponents = new[] { GeometryComponent.Vertices };
			runner = new QaContainerTestRunner(1000, test4);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test5 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 11.0);
			test5.GeometryComponents = new[] { GeometryComponent.Vertices };
			runner = new QaContainerTestRunner(1000, test5);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test6 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 11.0);
			test6.GeometryComponents = new[] { GeometryComponent.LineStartPoint };
			runner = new QaContainerTestRunner(1000, test6);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test7 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 91.0);
			test7.GeometryComponents = new[] { GeometryComponent.LineStartPoint };
			runner = new QaContainerTestRunner(1000, test7);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test8 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 91.0);
			test8.GeometryComponents = new[] { GeometryComponent.LineEndPoint };
			runner = new QaContainerTestRunner(1000, test8);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test9 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 120.0);
			test9.GeometryComponents = new[] { GeometryComponent.LineEndPoint };
			runner = new QaContainerTestRunner(1000, test9);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);

			var test10 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 89.0);
			test10.GeometryComponents = new[] { GeometryComponent.LineEndPoints };
			runner = new QaContainerTestRunner(1000, test10);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test11 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 92.0);
			test11.GeometryComponents = new[] { GeometryComponent.LineEndPoints };
			runner = new QaContainerTestRunner(1000, test11);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointTooClose", 2);
		}

		[Test]
		public void CanTestPointNotNearWithConstraint()
		{
			IFeatureWorkspace testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaPointNotNearTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass polyFc = DatasetUtils.CreateSimpleFeatureClass(
				testWs, "polygonFc", fields);

			IFeatureClass pointFc = CreatePointClass(testWs, "pointFc");

			IFeature polyFeature = polyFc.CreateFeature();

			polyFeature.Shape =
				CurveConstruction.StartPoly(0, 0, 0)
				                 .LineTo(100, 0, 10)
				                 .LineTo(50, 100, 10)
				                 .ClosePolygon();
			polyFeature.Store();

			IFeature p1 = pointFc.CreateFeature();
			p1.Shape = GeometryFactory.CreatePoint(20, 1.5, 0);
			p1.Store();

			var test1 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 1.0);
			var runner = new QaContainerTestRunner(1000, test1);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointWithin", 2);

			var test2 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 1.0);
			test2.ValidRelationConstraints = new[] { "G1.ObjectId = 1" };
			runner = new QaContainerTestRunner(1000, test2);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test3 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test3.ValidRelationConstraints = new[] { "G2.ObjectId = 1" };
			runner = new QaContainerTestRunner(1000, test3);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test4 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test4.ValidRelationConstraints = new[] { "G1.ObjectId = G2.ObjectId" };
			runner = new QaContainerTestRunner(1000, test4);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test5 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 2.0);
			test5.ValidRelationConstraints = new[] { "G1.ObjectId > G2.ObjectId" };
			runner = new QaContainerTestRunner(1000, test5);
			runner.Execute();
			AssertUtils.OneError(runner, "PointNotNear.PointWithin.ConstraintNotFulfilled", 2);
		}

		[Test]
		public void CanTestPointNotNearPolylineRightSide()
		{
			IFeatureWorkspace testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace(
					"CanTestPointNotNearPolylineRightSide");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateDoubleField("minDistance"));
			fields.AddField(FieldUtils.CreateDoubleField("rightSideDistance"));
			fields.AddField(FieldUtils.CreateIntegerField("flip"));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass polyFc = DatasetUtils.CreateSimpleFeatureClass(
				testWs, "polylineFc", fields);

			IFeatureClass pointFc = CreatePointClass(testWs, "pointFc");

			IFeature polyFeature = polyFc.CreateFeature();
			polyFeature.set_Value(1, 5);
			polyFeature.set_Value(2, 10);
			polyFeature.set_Value(3, 1);
			polyFeature.Shape = CurveConstruction.StartLine(0, 0, 0)
			                                     .LineTo(100, 0, 10)
			                                     .LineTo(100, 100, 10)
			                                     .Curve;
			polyFeature.Store();

			IFeature p1 = pointFc.CreateFeature();
			p1.Shape = GeometryFactory.CreatePoint(10, 7, 0);
			p1.Store();

			IFeature p2 = pointFc.CreateFeature();
			p2.Shape = GeometryFactory.CreatePoint(-0.1, 7, 0);
			p2.Store();

			IFeature p3 = pointFc.CreateFeature();
			p3.Shape = GeometryFactory.CreatePoint(93, 100.1, 10);
			p3.Store();

			//QaPointNotNear.UseQueryPointAndDistance = true;

			var test1 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(polyFc), 10.0);
			var runner = new QaContainerTestRunner(1000, test1);
			runner.Execute();
			Assert.AreEqual(3, runner.Errors.Count);

			var test2 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), new[] { ReadOnlyTableFactory.Create(polyFc) },
				10.0, null,
				new[] { "minDistance" });
			runner = new QaContainerTestRunner(1000, test2);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test3 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), new[] { ReadOnlyTableFactory.Create(polyFc) },
				10, null, new[] { "5" },
				new[] { "rightSideDistance" },
				null);
			runner = new QaContainerTestRunner(1000, test3);
			runner.Execute();
			AssertUtils.NoError(runner);

			var test4 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), new[] { ReadOnlyTableFactory.Create(polyFc) },
				10, null,
				new[] { "5" },
				new[] { "rightSideDistance" },
				new[] { "true" });

			runner = new QaContainerTestRunner(1000, test4);
			runner.Execute();
			Assert.AreEqual(3, runner.Errors.Count);

			var test5 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), new[] { ReadOnlyTableFactory.Create(polyFc) },
				10, null,
				new[] { "5" },
				new[] { "rightSideDistance" },
				new[] { "flip > 0" });

			runner = new QaContainerTestRunner(1000, test5);
			runner.Execute();
			Assert.AreEqual(3, runner.Errors.Count);
		}

		[Test]
		public void CanTestPointNotNearTwoPointPolylineInteriorVertices_Repro_PS248()
		{
			IFeatureWorkspace testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaPointNotNearTest_Repro248");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			IFeatureClass lineFC = DatasetUtils.CreateSimpleFeatureClass(
				testWs, "polylineFc", fields);

			IFeatureClass pointFc = CreatePointClass(testWs, "pointFc");

			IFeature lineFeature = lineFC.CreateFeature();

			lineFeature.Shape = CurveConstruction.StartLine(0, 0, 0)
			                                     .LineTo(50, 100, 10)
			                                     .Curve;
			lineFeature.Store();

			IFeature p1 = pointFc.CreateFeature();
			p1.Shape = GeometryFactory.CreatePoint(45, 90, 0);
			p1.Store();

			// A 2-point line has no interior vertices, so the test should not report an error:
			var test1 = new QaPointNotNear(
				ReadOnlyTableFactory.Create(pointFc), ReadOnlyTableFactory.Create(lineFC), 95);
			test1.GeometryComponents = new[] { GeometryComponent.InteriorVertices };
			var runner = new QaContainerTestRunner(1000, test1);
			runner.Execute();
			AssertUtils.NoError(runner);
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void PerformanceTest()
		{
			var ws = (IFeatureWorkspace) TestDataUtils.OpenTopgisTlm();

			IFeatureClass ptFc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_EINZELBAUM_GEBUESCH");
			IFeatureClass polyFc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_BODENBEDECKUNG");

			var test = new QaPointNotNear(
				ReadOnlyTableFactory.Create(ptFc), ReadOnlyTableFactory.Create(polyFc), 2);
			test.SetConstraint(1, "Objektart = 12"); // wald

			double tileSize = 10000;
			var runner = new QaContainerTestRunner(tileSize, test);

			IEnvelope testBox = GeometryFactory.CreateEnvelope(2554000, 1147000, 2561000, 1151000);
			var w = new Stopwatch();
			w.Start();
			runner.Execute(testBox);
			w.Stop();
			string msg = $"Direct:{w.ElapsedMilliseconds} ms";
			Console.WriteLine(msg);
			int nDirect = runner.Errors.Count;

			QaPointNotNear.UseQueryPointAndDistance = true;

			runner.ClearErrors();
			w.Reset();
			w.Start();
			runner.Execute(testBox);
			w.Stop();
			msg = $"QueryDistance:{w.ElapsedMilliseconds} ms";
			Console.WriteLine(msg);
			int nQuery = runner.Errors.Count;
			QaPointNotNear.UseQueryPointAndDistance = false;

			Assert.AreEqual(nQuery, nDirect);
		}

		private static IFeatureClass CreatePointClass([NotNull] IFeatureWorkspace ws,
		                                              [NotNull] string name)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPoint,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true));

			return DatasetUtils.CreateSimpleFeatureClass(ws, name, fields);
		}
	}
}

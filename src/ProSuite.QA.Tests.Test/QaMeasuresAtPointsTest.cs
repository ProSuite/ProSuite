using System;
using System.Data;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMeasuresAtPointsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaMeasuresAtPointsTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestValidWithPointM()
		{
			TestValidWithPointM(_testWs);
		}

		[Test]
		public void TestMExpressions()
		{
			TestMExpressions(_testWs);
		}

		[Test]
		public void TestWithErrors()
		{
			TestWithErrors(_testWs);
		}

		[Test]
		public void TestNearVertex()
		{
			TestNearVertex(_testWs);
		}

		[Test]
		public void MultiPointPerformanceTest()
		{
			IWorkspace ws = TestDataUtils.OpenTopgisTlm();
			IFeatureClass mpFc =
				((IFeatureWorkspace) ws).OpenFeatureClass("TOPGIS_TLM.TLM_DTM_MASSENPUNKTE");
			var watch = new Stopwatch();
			WKSPointVA[] sourcePointsArray = null;

			watch.Start();
			long sumArray = 0;
			long sumSingle = 0;
			long sumPoints = 0;
			int limit = 1000 * 120;
			var featureCount = 0;
			foreach (IFeature feature in new EnumCursor((ITable) mpFc, null, recycle: true))
			{
				var points = (IPointCollection5) feature.Shape;
				int pointCount = points.PointCount;
				sumPoints += pointCount;
				// Console.WriteLine(pointCount);

				if (sourcePointsArray == null || sourcePointsArray.Length < points.PointCount)
				{
					const int margin = 2000;
					sourcePointsArray = new WKSPointVA[pointCount + margin];
				}

				long start = watch.ElapsedTicks;
				double sumMArray = 0;
				points.QueryWKSPointVA(0, pointCount, out sourcePointsArray[0]);
				for (var i = 0; i < pointCount; i++)
				{
					double currentM = sourcePointsArray[i].m_m;
					sumMArray += currentM;
				}

				long stop = watch.ElapsedTicks;
				sumArray += stop - start;

				double sumMSingle = 0;
				start = watch.ElapsedTicks;
				for (var i = 0; i < pointCount; i++)
				{
					WKSPointVA currentPoint;
					points.QueryWKSPointVA(i, 1, out currentPoint);
					double currentM = currentPoint.m_m;
					sumMSingle += currentM;
				}

				stop = watch.ElapsedTicks;
				sumSingle += stop - start;

				Assert.AreEqual(sumMArray, sumMSingle);
				featureCount++;
				if (watch.ElapsedMilliseconds > limit)
				{
					break;
				}
			}

			Console.WriteLine(string.Format("Points: " + sumPoints));
			Console.WriteLine(string.Format("Array Tics: " + sumArray));
			Console.WriteLine(string.Format("Single Tics:" + sumSingle));
		}

		[Test]
		[Ignore(
			"Currently fails since expressions are checked lazily; reenable after redesigning test initialization (Initialize() method)"
		)]
		public void TestInvalidArguments()
		{
			TestInvalidArguments(_testWs);
		}

		private static void TestValidWithPointM(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetMDomain(sr, 0, 1000000, 0.001, 0.002);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, sr, 1000, false,
				                     true));

			IFeatureClass fcPoints =
				DatasetUtils.CreateSimpleFeatureClass(ws, "ValidWithPointMPoints", fieldsPoint);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr, 1000,
				                    false, true));

			IFeatureClass fcLines =
				DatasetUtils.CreateSimpleFeatureClass(ws, "ValidWithPointMLines", fieldsLine);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature point1 = fcPoints.CreateFeature();
			point1.Shape = CreatePoint(100, 100, 20);
			point1.Store();

			IFeature fLine1 = fcLines.CreateFeature();
			IPolycurve l1 =
				CurveConstruction.StartLine(CreatePoint(100, 100, 20))
				                 .LineTo(CreatePoint(200, 200, 50))
				                 .Curve;
			((IMAware) l1).MAware = true;
			fLine1.Shape = l1;
			fLine1.Store();

			IFeature fLine2 = fcLines.CreateFeature();
			IPolycurve l2 =
				CurveConstruction.StartLine(CreatePoint(50, 50, 10))
				                 .LineTo(CreatePoint(150, 150, 30))
				                 .Curve;
			((IMAware) l2).MAware = true;
			fLine2.Shape = l2;
			fLine2.Store();

			var test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 0.01, 0.01,
			                                  LineMSource.Nearest, false);

			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		private static void TestMExpressions(IFeatureWorkspace ws)
		{
			ISpatialReference srM = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetMDomain(srM, 0, 1000000, 0.001, 0.002);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateDoubleField("DoubleField"));
			fieldsPoint.AddField(FieldUtils.CreateIntegerField("IntField"));
			fieldsPoint.AddField(FieldUtils.CreateTextField("StringField", 50));
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, srM, 1000, false,
				                     true));

			IFeatureClass fcPoints =
				DatasetUtils.CreateSimpleFeatureClass(ws, "MExpressionsPoints", fieldsPoint);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, srM, 1000,
				                    false, true));

			IFeatureClass fcLines =
				DatasetUtils.CreateSimpleFeatureClass(ws, "MExpressionsLines", fieldsLine);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature point1 = fcPoints.CreateFeature();
			point1.set_Value(1, 1.0002);
			point1.set_Value(2, 1);
			point1.set_Value(3, "1");
			point1.Shape = CreatePoint(100, 100, 20);
			point1.Store();

			IFeature fLine1 = fcLines.CreateFeature();
			IPolycurve l1 =
				CurveConstruction.StartLine(CreatePoint(100, 100, 1.001))
				                 .LineTo(CreatePoint(200, 200, 1.001))
				                 .Curve;
			((IMAware) l1).MAware = true;
			fLine1.Shape = l1;
			fLine1.Store();

			var test = new QaMeasuresAtPoints(fcPoints, "DoubleField", new[] {fcLines}, 0.01,
			                                  0.01, LineMSource.Nearest, false);
			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "IntField", new[] {fcLines}, 0.01, 0.01,
			                              LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "StringField", new[] {fcLines}, 0.01,
			                              0.01, LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "1.00001", new[] {fcLines}, 0.01, 0.01,
			                              LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "1", new[] {fcLines}, 0.01, 0.01,
			                              LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "DoubleField + IntField - 1",
			                              new[] {fcLines}, 0.01, 0.01, LineMSource.Nearest,
			                              false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		private static void TestWithErrors(IFeatureWorkspace ws)
		{
			ISpatialReference srM = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetMDomain(srM, 0, 1000000, 0.001, 0.002);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateDoubleField("DoubleField"));
			fieldsPoint.AddField(FieldUtils.CreateIntegerField("IntField"));
			fieldsPoint.AddField(FieldUtils.CreateTextField("StringField", 50));
			fieldsPoint.AddField(FieldUtils.CreateDoubleField("EmptyField"));
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, srM, 1000, false,
				                     true));

			IFeatureClass fcPoints =
				DatasetUtils.CreateSimpleFeatureClass(ws, "WithErrorsPoints", fieldsPoint);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, srM, 1000,
				                    false, true));

			IFeatureClass fcLines =
				DatasetUtils.CreateSimpleFeatureClass(ws, "WithErrorsLines", fieldsLine);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature point1 = fcPoints.CreateFeature();
			point1.set_Value(1, 2); // DoubleField
			point1.set_Value(2, 2); // IntField
			point1.set_Value(3, "A"); // StringField
			point1.Shape = CreatePoint(100, 100, 2);
			point1.Store();

			IFeature fLine1 = fcLines.CreateFeature();
			IPolycurve l1 =
				CurveConstruction.StartLine(CreatePoint(100, 100, 1))
				                 .LineTo(CreatePoint(200, 200, 1))
				                 .Curve;
			((IMAware) l1).MAware = true;
			fLine1.Shape = l1;
			fLine1.Store();

			var test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 0.01, 0.01,
			                                  LineMSource.Nearest, false);
			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "DoubleField", new[] {fcLines}, 0.01,
			                              0.01, LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "IntField", new[] {fcLines}, 0.01, 0.01,
			                              LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "EmptyField", new[] {fcLines}, 0.01, 0.01,
			                              LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);

			test = new QaMeasuresAtPoints(fcPoints, "0.5 * EmptyField", new[] {fcLines}, 0.01,
			                              0.01, LineMSource.Nearest, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);
		}

		private static void TestNearVertex(IFeatureWorkspace ws)
		{
			ISpatialReference srM = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetMDomain(srM, 0, 1000000, 0.001, 0.002);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, srM, 1000, false,
				                     true));

			IFeatureClass fcPoints =
				DatasetUtils.CreateSimpleFeatureClass(ws, "NearVertexPoints", fieldsPoint);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, srM, 1000,
				                    false, true));

			IFeatureClass fcLines =
				DatasetUtils.CreateSimpleFeatureClass(ws, "NearVertexLines", fieldsLine);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature point1 = fcPoints.CreateFeature();
			point1.Shape = CreatePoint(100.1, 100.1, 2);
			point1.Store();

			IFeature fLine1 = fcLines.CreateFeature();
			IPolycurve l1 =
				CurveConstruction.StartLine(CreatePoint(100, 100, 2))
				                 .LineTo(CreatePoint(101, 100, 1000))
				                 .Curve;
			((IMAware) l1).MAware = true;
			fLine1.Shape = l1;
			fLine1.Store();

			var test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 1, 0.01,
			                                  LineMSource.Nearest, false);
			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count); // neareast --> error

			test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 1, 0.01,
			                              LineMSource.VertexPreferred, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count); // Vertex near point1 --> vertex --> OK

			test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 1, 0.01,
			                              LineMSource.VertexRequired, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count); // Vertex near point1 --> vertex --> OK

			test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 0.12, 0.01,
			                              LineMSource.VertexPreferred, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);
			// No Vertex near point1 --> neareast --> error 

			test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 0.12, 0.01,
			                              LineMSource.VertexRequired, false);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);
			// No Vertex near point1 --> required --> error 

			test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 0.05, 0.01,
			                              LineMSource.Nearest, true);
			container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);
			// No Line near point1 --> line required --> error 
		}

		private static void TestInvalidArguments(IFeatureWorkspace ws)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			ISpatialReference srM = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetMDomain(srM, 0, 1000000, 0.001, 0.002);

			IFieldsEdit fieldsPoint = new FieldsClass();
			fieldsPoint.AddField(FieldUtils.CreateOIDField());
			fieldsPoint.AddField(FieldUtils.CreateDoubleField("DoubleField"));
			fieldsPoint.AddField(FieldUtils.CreateIntegerField("IntField"));
			fieldsPoint.AddField(FieldUtils.CreateTextField("StringField", 50));
			fieldsPoint.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint, sr, 1000));

			IFeatureClass fcPoints =
				DatasetUtils.CreateSimpleFeatureClass(ws, "InvalidArgumentsPoints", fieldsPoint);

			IFieldsEdit fieldsLine = new FieldsClass();
			fieldsLine.AddField(FieldUtils.CreateOIDField());
			fieldsLine.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr, 1000));

			IFeatureClass fcLines =
				DatasetUtils.CreateSimpleFeatureClass(ws, "InvalidArgumentsLines", fieldsLine);

			IFieldsEdit fieldsLineM = new FieldsClass();
			fieldsLineM.AddField(FieldUtils.CreateOIDField());
			fieldsLineM.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPolyline, srM, 1000,
				                     false, true));

			IFeatureClass fcLinesM =
				DatasetUtils.CreateSimpleFeatureClass(ws, "InvalidArgumentsLinesM", fieldsLineM);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			var success = true;
			try
			{
				var test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLines}, 0.01, 0.01,
				                                  LineMSource.Nearest, false);
				Assert.IsNotNull(test);
			}
			catch (ArgumentException)
			{
				success = false;
			}

			Assert.IsFalse(success);

			success = true;
			try
			{
				var test = new QaMeasuresAtPoints(fcPoints, null, new[] {fcLinesM}, 0.01, 0.01,
				                                  LineMSource.Nearest, false);
				Assert.IsNotNull(test);
			}
			catch (ArgumentException)
			{
				success = false;
			}

			Assert.IsFalse(success);

			success = true;
			try
			{
				var test = new QaMeasuresAtPoints(fcPoints, "UnknownField", new[] {fcLinesM},
				                                  0.01, 0.01, LineMSource.Nearest, false);
				Assert.IsNotNull(test);
			}
			catch (EvaluateException)
			{
				success = false;
			}

			Assert.IsFalse(success);
		}

		private static IPoint CreatePoint(double x, double y, double m)
		{
			IPoint p = GeometryFactory.CreatePoint(x, y);
			p.M = m;
			((IMAware) p).MAware = true;
			return p;
		}
	}
}

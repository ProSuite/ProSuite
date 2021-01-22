using System;
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
	[CLSCompliant(false)]
	public class QaContainedPointsCountTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaContainedPointsCountTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestOnePointMissing()
		{
			IFeatureClass polygonFeatureClass;
			IFeatureClass pointFeatureClass;
			CreateTestFeatureClasses("polygons1", "points1",
			                         out polygonFeatureClass,
			                         out pointFeatureClass);

			CreateTestPolygons(polygonFeatureClass);

			IFeature pointFeature1 = pointFeatureClass.CreateFeature();
			pointFeature1.Shape = GeometryFactory.CreatePoint(150, 150);
			pointFeature1.Store();

			((IFeatureClassManage) polygonFeatureClass).UpdateExtent();
			((IFeatureClassManage) pointFeatureClass).UpdateExtent();

			const int expectedErrorCount = 2;
			const int minimumPointCount = 1;
			const int maximumPointCount = 1;
			const string relevantPointCondition = "POINT.OBJECTID = 0 AND POLYGON.OBJECTID = 0";
			// not matched
			const bool countPointOnPolygonBorder = false;

			RunTest(polygonFeatureClass, pointFeatureClass,
			        minimumPointCount, maximumPointCount,
			        relevantPointCondition, countPointOnPolygonBorder,
			        expectedErrorCount);
		}

		[Test]
		public void TestNoRelevantPoints()
		{
			IFeatureClass polygonFeatureClass;
			IFeatureClass pointFeatureClass;
			CreateTestFeatureClasses("polygons2", "points2", out polygonFeatureClass,
			                         out pointFeatureClass);

			CreateTestPolygons(polygonFeatureClass);

			IFeature pointFeature1 = pointFeatureClass.CreateFeature();
			pointFeature1.Shape = GeometryFactory.CreatePoint(150, 150);
			pointFeature1.Store();

			((IFeatureClassManage) polygonFeatureClass).UpdateExtent();
			((IFeatureClassManage) pointFeatureClass).UpdateExtent();

			const int expectedErrorCount = 1;
			const int minimumPointCount = 1;
			const int maximumPointCount = 1;
			const string relevantPointCondition = "POINT.OBJECTID > 0 AND POLYGON.OBJECTID > 0";
			const bool countPointOnPolygonBorder = false;

			RunTest(polygonFeatureClass, pointFeatureClass,
			        minimumPointCount, maximumPointCount,
			        relevantPointCondition, countPointOnPolygonBorder,
			        expectedErrorCount);
		}

		[Test]
		public void TestPointOnBoundary()
		{
			IFeatureClass polygonFeatureClass;
			IFeatureClass pointFeatureClass;
			CreateTestFeatureClasses("polygons3", "points3",
			                         out polygonFeatureClass,
			                         out pointFeatureClass);

			CreateTestPolygons(polygonFeatureClass);

			IFeature pointFeature1 = pointFeatureClass.CreateFeature();
			pointFeature1.Shape = GeometryFactory.CreatePoint(150, 150);
			pointFeature1.Store();

			IFeature pointFeature2 = pointFeatureClass.CreateFeature();
			pointFeature2.Shape = GeometryFactory.CreatePoint(200, 150);
			pointFeature2.Store();

			((IFeatureClassManage) polygonFeatureClass).UpdateExtent();
			((IFeatureClassManage) pointFeatureClass).UpdateExtent();

			const int expectedErrorCount = 1;
			const int minimumPointCount = 1;
			const int maximumPointCount = 1;
			const string relevantPointCondition = "POINT.OBJECTID > 0 AND POLYGON.OBJECTID > 0";
			const bool countPointOnPolygonBorder = false;

			RunTest(polygonFeatureClass, pointFeatureClass,
			        minimumPointCount, maximumPointCount,
			        relevantPointCondition, countPointOnPolygonBorder,
			        expectedErrorCount);
		}

		[Test]
		public void TestTooManyPoints()
		{
			IFeatureClass polygonFeatureClass;
			IFeatureClass pointFeatureClass;
			CreateTestFeatureClasses("polygons4", "points4",
			                         out polygonFeatureClass,
			                         out pointFeatureClass);

			CreateTestPolygons(polygonFeatureClass);

			IFeature pointFeature1 = pointFeatureClass.CreateFeature();
			pointFeature1.Shape = GeometryFactory.CreatePoint(150, 150);
			pointFeature1.Store();

			IFeature pointFeature2 = pointFeatureClass.CreateFeature();
			pointFeature2.Shape = GeometryFactory.CreatePoint(240, 150);
			pointFeature2.Store();

			IFeature pointFeature3 = pointFeatureClass.CreateFeature();
			pointFeature3.Shape = GeometryFactory.CreatePoint(250, 150);
			pointFeature3.Store();

			((IFeatureClassManage) polygonFeatureClass).UpdateExtent();
			((IFeatureClassManage) pointFeatureClass).UpdateExtent();

			const int expectedErrorCount = 1;
			const int minimumPointCount = 1;
			const int maximumPointCount = 1;
			const string relevantPointCondition = "POINT.OBJECTID > 0 AND POLYGON.OBJECTID > 0";
			const bool countPointOnPolygonBorder = false;

			RunTest(polygonFeatureClass, pointFeatureClass,
			        minimumPointCount, maximumPointCount,
			        relevantPointCondition, countPointOnPolygonBorder,
			        expectedErrorCount);
		}

		[Test]
		public void TestClosedLines()
		{
			var ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ClosedLines");
			IFeatureClass polylineFeatureClass;
			IFeatureClass pointFeatureClass;
			CreateTestFeatureClasses("polygons5", "points5",
			                         out polylineFeatureClass,
			                         out pointFeatureClass, ws, polygonAsClosedLines: true);

			IFeature emptyFeature = polylineFeatureClass.CreateFeature();
			emptyFeature.Shape =
				CurveConstruction.StartLine(100, 300)
				                 .LineTo(200, 300)
				                 .LineTo(200, 400)
				                 .LineTo(100, 400)
				                 .LineTo(100, 300)
				                 .Curve;
			emptyFeature.Store();

			IFeature counterClockwiseFeature = polylineFeatureClass.CreateFeature();
			counterClockwiseFeature.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(200, 100)
				                 .LineTo(200, 200)
				                 .LineTo(100, 200)
				                 .LineTo(100, 100)
				                 .Curve;
			counterClockwiseFeature.Store();

			IFeature clockwiseFeature = polylineFeatureClass.CreateFeature();
			clockwiseFeature.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .LineTo(100, 100)
				                 .Curve;
			clockwiseFeature.Store();

			IFeature notClosedFeature = polylineFeatureClass.CreateFeature();
			notClosedFeature.Shape =
				CurveConstruction.StartLine(300, 100)
				                 .LineTo(400, 100)
				                 .LineTo(400, 200)
				                 .Curve;
			notClosedFeature.Store();

			IFeature pointFeature1 = pointFeatureClass.CreateFeature();
			pointFeature1.Shape = GeometryFactory.CreatePoint(150, 150);
			pointFeature1.Store();

			IFeature pointFeature2 = pointFeatureClass.CreateFeature();
			pointFeature2.Shape = GeometryFactory.CreatePoint(350, 100);
			pointFeature2.Store();

			var test = new QaContainedPointsCount(polylineFeatureClass, pointFeatureClass, 1,
			                                      string.Empty)
			           {
				           PolylineUsage = PolylineUsage.AsIs
			           };
			var containerRunner = new QaContainerTestRunner(1000, test);
			containerRunner.Execute();
			Assert.AreEqual(3, containerRunner.Errors.Count);

			test = new QaContainedPointsCount(polylineFeatureClass, pointFeatureClass, 1,
			                                  string.Empty)
			       {
				       PolylineUsage = PolylineUsage.AsPolygonIfClosedElseReportIssue
			       };
			containerRunner = new QaContainerTestRunner(1000, test);
			containerRunner.Execute();
			Assert.AreEqual(2, containerRunner.Errors.Count);

			test = new QaContainedPointsCount(polylineFeatureClass, pointFeatureClass, 1,
			                                  string.Empty)
			       {
				       PolylineUsage = PolylineUsage.AsPolygonIfClosedElseIgnore
			       };
			containerRunner = new QaContainerTestRunner(1000, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);

			test = new QaContainedPointsCount(polylineFeatureClass, pointFeatureClass, 1,
			                                  string.Empty)
			       {
				       PolylineUsage = PolylineUsage.AsPolygonIfClosedElseAsPolyline
			       };
			containerRunner = new QaContainerTestRunner(1000, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);
		}

		private static void RunTest([NotNull] IFeatureClass polygonFeatureClass,
		                            [NotNull] IFeatureClass pointFeatureClass,
		                            int minimumPointCount,
		                            int maximumPointCount,
		                            [CanBeNull] string relevantPointCondition,
		                            bool countPointOnPolygonBorder,
		                            int expectedErrorCount)
		{
			RunTest(polygonFeatureClass, pointFeatureClass,
			        minimumPointCount, maximumPointCount,
			        relevantPointCondition, countPointOnPolygonBorder,
			        null, expectedErrorCount);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(-100, -100, 400, 400);
			RunTest(polygonFeatureClass, pointFeatureClass,
			        minimumPointCount, maximumPointCount,
			        relevantPointCondition, countPointOnPolygonBorder,
			        envelope, expectedErrorCount);
		}

		private static void RunTest([NotNull] IFeatureClass polygonFeatureClass,
		                            [NotNull] IFeatureClass pointFeatureClass,
		                            int minimumPointCount,
		                            int maximumPointCount,
		                            [CanBeNull] string relevantPointCondition,
		                            bool countPointOnPolygonBorder,
		                            [CanBeNull] IEnvelope envelope,
		                            int expectedErrorCount
		)
		{
			var tileSizes = new[] {22.2, 50.0, 100.0, 150.0, 200.0, 250.0};

			foreach (double tileSize in tileSizes)
			{
				Console.WriteLine(@"Tilesize: {0}", tileSize);

				RunTest(polygonFeatureClass, pointFeatureClass,
				        minimumPointCount, maximumPointCount,
				        relevantPointCondition, countPointOnPolygonBorder,
				        tileSize, envelope, expectedErrorCount);
			}
		}

		private static void RunTest([NotNull] IFeatureClass polygonFeatureClass,
		                            [NotNull] IFeatureClass pointFeatureClass,
		                            int minimumPointCount, int maximumPointCount,
		                            [CanBeNull] string relevantPointCondition,
		                            bool countPointOnPolygonBorder,
		                            double tileSize,
		                            [CanBeNull] IEnvelope envelope, int expectedErrorCount)
		{
			var test = new QaContainedPointsCount(polygonFeatureClass, pointFeatureClass,
			                                      minimumPointCount, maximumPointCount,
			                                      relevantPointCondition,
			                                      countPointOnPolygonBorder);

			// run in container without envelope

			var containerRunner = new QaContainerTestRunner(tileSize, test);
			if (envelope == null)
			{
				containerRunner.Execute();
			}
			else
			{
				containerRunner.Execute(envelope);
			}

			Assert.AreEqual(expectedErrorCount, containerRunner.Errors.Count);
		}

		private static void CreateTestPolygons([NotNull] IFeatureClass polygonFeatureClass)
		{
			IFeature feature1 = polygonFeatureClass.CreateFeature();
			feature1.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			feature1.Store();

			IFeature feature2 = polygonFeatureClass.CreateFeature();
			feature2.Shape =
				CurveConstruction.StartPoly(200, 100)
				                 .LineTo(200, 200)
				                 .LineTo(300, 200)
				                 .LineTo(300, 100)
				                 .ClosePolygon();
			feature2.Store();
		}

		private void CreateTestFeatureClasses(
			string polygonsName,
			string pointsName,
			[NotNull] out IFeatureClass polygonFeatureClass,
			[NotNull] out IFeatureClass pointFeatureClass,
			IFeatureWorkspace ws = null, bool polygonAsClosedLines = false)
		{
			ws = ws ?? _testWs;

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			var geomType = polygonAsClosedLines
				               ? esriGeometryType.esriGeometryPolyline
				               : esriGeometryType.esriGeometryPolygon;
			IFieldsEdit polygonFields = new FieldsClass();
			polygonFields.AddField(FieldUtils.CreateOIDField());
			polygonFields.AddField(FieldUtils.CreateShapeField(
				                       "Shape", geomType,
				                       sref, 1000, false, false));

			polygonFeatureClass = DatasetUtils.CreateSimpleFeatureClass(ws, polygonsName,
			                                                            polygonFields,
			                                                            null);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(FieldUtils.CreateShapeField(
				                     "Shape", esriGeometryType.esriGeometryPoint,
				                     sref, 1000, false, false));

			pointFeatureClass = DatasetUtils.CreateSimpleFeatureClass(ws, pointsName,
			                                                          pointFields,
			                                                          null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);
		}
	}
}

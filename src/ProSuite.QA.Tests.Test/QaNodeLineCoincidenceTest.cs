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
	public class QaNodeLineCoincidenceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaNodeLineCoincidenceTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanReportWithinFeature()
		{
			const string testName = "CanReportWithinFeature";

			IFeatureClass lineClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {lineClass};

			IPolycurve multipartLine = CurveConstruction.StartLine(0, 0)
			                                            .LineTo(100, 0)
			                                            .LineTo(101, 0)
			                                            .LineTo(100, 0)
			                                            .LineTo(100, 100)
			                                            .Curve;
			GeometryUtils.Simplify(multipartLine, true, true);

			IFeature nearRow = lineClass.CreateFeature();
			nearRow.Shape = multipartLine;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(lineClass, nearClasses, 2);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.ExpectedErrors(2, runner.Errors,
			                           "NodeLineCoincidence.NodeTooCloseToLine.WithinFeature");
		}

		[Test]
		public void CanReportVertexInLeftTileByNearDistance()
		{
			const string testName = "CanReportVertexInLeftTileByNearDistance";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(201, 100);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(100, 100)
			                                 .LineTo(100, 199)
			                                 .LineTo(199, 199)
			                                 .LineTo(199, 100)
			                                 .Curve;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses, 2.1);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NodeLineCoincidence.NodeTooCloseToLine.BetweenFeatures");
		}

		[Test]
		public void CanReportVertexInRightTileByNearDistance()
		{
			const string testName = "CanReportVertexInRightTileByNearDistance";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(199, 100);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(201, 101)
			                                 .LineTo(222, 133)
			                                 .Curve;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses, 3);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NodeLineCoincidence.NodeTooCloseToLine.BetweenFeatures");
		}

		[Test]
		public void CanAllowVertexWithinCoincidenceTolerance()
		{
			const string testName = "CanAllowVertexWithinCoincidenceTolerance";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(1, 0.1);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(0, 0)
			                                 .LineTo(2, 0)
			                                 .Curve;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses, 2)
			           {
				           CoincidenceTolerance = 0.2
			           };

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanReportVertexExactlyNearDistanceAway()
		{
			const string testName = "CanReportVertexExactlyNearDistanceAway";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(0, 0);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(0, 2)
			                                 .LineTo(2, 2)
			                                 .Curve;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses, 2);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NodeLineCoincidence.NodeTooCloseToLine.BetweenFeatures");
		}

		[Test]
		public void CanAllowSingleConnectedLineShorterThanNearDistance()
		{
			const string testName = "CanAllowSingleConnectedLineShorterThanNearDistance";

			IFeatureClass lineClass = CreateFeatureClass(string.Format("{0}_lines", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			IFeature longLine = lineClass.CreateFeature();
			longLine.Shape = CurveConstruction.StartLine(0, 0)
			                                  .LineTo(100, 0)
			                                  .Curve;
			longLine.Store();

			IFeature shortLine = lineClass.CreateFeature();
			shortLine.Shape = CurveConstruction.StartLine(100, 0)
			                                   .LineTo(101, 0)
			                                   .Curve;
			shortLine.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(lineClass, new[] {lineClass}, 2, false);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		// TODO add unit tests for
		// - connected short line that has a real undershoot at other end

		[Test]
		public void CanAllowVertexOnConnectedLineInsideNearDistance()
		{
			const string testName = "CanAllowVertexOnConnectedLineInsideNearDistance";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(0, 0);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(0, 0)
			                                 .LineTo(1, 1)
			                                 .LineTo(4, 3)
			                                 .Curve;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses, 2);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanUseMultipleTolerances()
		{
			const string testName = "CanUseMultipleTolerances";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);
			IFeatureClass nearClass1 = CreateFeatureClass(string.Format("{0}_near1", testName),
			                                              esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass, nearClass1};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(100, 100);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(0, 0)
			                                 .LineTo(99, 99)
			                                 .Curve;
			nearRow.Store();

			IFeature nearRow1 = nearClass1.CreateFeature();
			nearRow1.Shape = CurveConstruction.StartLine(0, 100)
			                                  .LineTo(99, 101)
			                                  .LineTo(4, 3)
			                                  .Curve;
			nearRow1.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses,
			                                     new List<double> {1.0, 2.0}, 0, true, false);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NodeLineCoincidence.NodeTooCloseToLine.BetweenFeatures");
		}

		[Test]
		public void CanReportErrorUseMultipleTolerancesInTileTopRight()
		{
			const string testName = "CanReportErrorUseMultipleTolerancesInTileTopRight";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);
			IFeatureClass nearClass1 = CreateFeatureClass(string.Format("{0}_near1", testName),
			                                              esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass, nearClass1};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(199, 199);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(202, 202)
			                                 .LineTo(204, 204)
			                                 .Curve;
			nearRow.Store();

			IFeature nearRow1 = nearClass1.CreateFeature();
			nearRow1.Shape = CurveConstruction.StartLine(205, 199)
			                                  .LineTo(205, 101)
			                                  .LineTo(400, 103)
			                                  .Curve;
			nearRow1.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses,
			                                     new List<double> {5.0, 1.0}, 0, false, false);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NodeLineCoincidence.NodeTooCloseToLine.BetweenFeatures");
		}

		[Test]
		public void CanReportManyErrors()
		{
			const string testName = "CanReportManyErrors";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(0, 0);
			nodeRow.Store();
			IFeature nodeRow1 = nodeClass.CreateFeature();
			nodeRow1.Shape = GeometryFactory.CreatePoint(4, 4);
			nodeRow1.Store();
			IFeature nodeRow2 = nodeClass.CreateFeature();
			nodeRow2.Shape = GeometryFactory.CreatePoint(1, 4);
			nodeRow2.Store();
			IFeature nodeRow3 = nodeClass.CreateFeature();
			nodeRow3.Shape = GeometryFactory.CreatePoint(5, 3);
			nodeRow3.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(1, 1)
			                                 .LineTo(2, 1)
			                                 .LineTo(3, 3)
			                                 .LineTo(1, 2)
			                                 .LineTo(1, 3)
			                                 .Curve;
			nearRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses, 4);

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(4, runner.Errors.Count);
		}

		[Test]
		public void CanReportErrorUseMultipleTolerancesInTileLeft()
		{
			const string testName = "CanReportErrorUseMultipleTolerancesInTileLeft";

			IFeatureClass nodeClass = CreateFeatureClass(string.Format("{0}_node", testName),
			                                             esriGeometryType.esriGeometryPoint);
			IFeatureClass nearClass = CreateFeatureClass(string.Format("{0}_near", testName),
			                                             esriGeometryType.esriGeometryPolyline);
			IFeatureClass nearClass1 = CreateFeatureClass(string.Format("{0}_near1", testName),
			                                              esriGeometryType.esriGeometryPolyline);

			var nearClasses = new List<IFeatureClass> {nearClass, nearClass1};

			IFeature nodeRow = nodeClass.CreateFeature();
			nodeRow.Shape = GeometryFactory.CreatePoint(201, 199);
			nodeRow.Store();

			IFeature nearRow = nearClass.CreateFeature();
			nearRow.Shape = CurveConstruction.StartLine(199, 198)
			                                 .LineTo(100, 100)
			                                 .Curve;
			nearRow.Store();

			IFeature nearRow1 = nearClass1.CreateFeature();
			nearRow1.Shape = CurveConstruction.StartLine(100, 205)
			                                  .LineTo(199, 199)
			                                  .LineTo(100, 103)
			                                  .Curve;
			nearRow1.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaNodeLineCoincidence(nodeClass, nearClasses,
			                                     new List<double> {2.0, 3.0}, 0, false, false);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "NodeLineCoincidence.NodeTooCloseToLine.BetweenFeatures");
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType type,
		                                         bool zAware = false)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 setDefaultXyDomain: true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("Shape", type, sref, 1000, zAware));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}
	}
}

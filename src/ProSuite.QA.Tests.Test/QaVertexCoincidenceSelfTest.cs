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
	public class QaVertexCoincidenceSelfTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaVertexCoincidenceSelfTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanReportPointErrorInSameTile()
		{
			const string testName = "CanReportPointErrorInSameTile";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryPoint);

			IFeature vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = GeometryFactory.CreatePoint(201, 199);
			vertexRow.Store();
			IFeature vertexRow1 = vertexClass.CreateFeature();
			vertexRow1.Shape = GeometryFactory.CreatePoint(199, 199);
			vertexRow1.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           PointTolerance = 3
			           };

			var runner = new QaContainerTestRunner(500, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanReportMultiPointErrorInSameTile()
		{
			const string testName = "CanReportMultiPointErrorInSameTile";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryMultipoint);

			IFeature multiPointClass = vertexClass.CreateFeature();
			multiPointClass.Shape =
				GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(201, 199));
			multiPointClass.Store();
			IFeature vertexRow1 = vertexClass.CreateFeature();
			vertexRow1.Shape =
				GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(199, 199));
			vertexRow1.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           PointTolerance = 3
			           };

			var runner = new QaContainerTestRunner(500, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanReportPointZError()
		{
			const string testName = "CanReportPointZError";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryPoint, zAware: true);

			IFeature vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = GeometryFactory.CreatePoint(199, 199, 10);
			vertexRow.Store();

			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = GeometryFactory.CreatePoint(199, 199, 11);
			vertexRow.Store();

			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = GeometryFactory.CreatePoint(150, 150, 10);
			vertexRow.Store();
			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape =
				GeometryFactory.CreatePoint(150, 150, 13); // dz > ZTolerance
			vertexRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           PointTolerance = 3,
				           ZTolerance = 2,
				           ZCoincidenceTolerance = -1
			           };

			var runner = new QaContainerTestRunner(500, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanReportLineZError()
		{
			const string testName = "CanReportLineZError";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_line", esriGeometryType.esriGeometryPolyline, zAware: true);

			IFeature vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = CurveConstruction.StartLine(10, 10, 10)
			                                   .LineTo(100, 10, 10)
			                                   .Curve;
			vertexRow.Store();

			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = CurveConstruction.StartLine(100, 20, 10)
			                                   .LineTo(100, 10, 8)
			                                   .Curve;
			vertexRow.Store();

			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = CurveConstruction.StartLine(10, 50, 10)
			                                   .LineTo(50, 50, 10)
			                                   .LineTo(100, 50, 10)
			                                   .LineTo(50, 40, 10)
			                                   .LineTo(50, 50, 8)
			                                   .LineTo(50, 60, 10).Curve;
			vertexRow.Store();

			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = CurveConstruction.StartLine(30, 60, 10)
			                                   .LineTo(30, 50, 10)
			                                   .LineTo(30, 10, 8)
			                                   .LineTo(30, 5, 0).Curve;
			vertexRow.Store();

			vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = CurveConstruction.StartLine(10, 80, 10)
			                                   .LineTo(100, 80, 10)
			                                   .LineTo(50, 70, 10)
			                                   .LineTo(50, 80, 8).Curve;
			vertexRow.Store();

			//vertexRow = vertexClass.CreateFeature();
			//vertexRow.Shape =
			//	GeometryFactory.CreatePoint(150, 150, 10);
			//vertexRow.Store();
			//vertexRow = vertexClass.CreateFeature();
			//vertexRow.Shape =
			//	GeometryFactory.CreatePoint(150, 150, 13); // dz > ZTolerance
			//vertexRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           PointTolerance = 3,
				           ZTolerance = -1,
				           ZCoincidenceTolerance = -1,
				           EdgeTolerance = 0.001,
				           RequireVertexOnNearbyEdge = false,
				           VerifyWithinFeature = true
			           };

			var runner = new QaContainerTestRunner(500, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(4, runner.Errors.Count);
		}

		[Test]
		public void CanReportPointErrorInDifferentTile()
		{
			const string testName = "CanReportPointErrorInDifferentTile";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryPoint);

			IFeature vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape = GeometryFactory.CreatePoint(201, 199);
			vertexRow.Store();
			IFeature vertexRow1 = vertexClass.CreateFeature();
			vertexRow1.Shape = GeometryFactory.CreatePoint(199, 199);
			vertexRow1.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           PointTolerance = 3,
				           ReportCoordinates = true
			           };

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanReportMultipointErrorInDifferentTile()
		{
			const string testName = "CanReportMultipointErrorInDifferentTile";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryMultipoint);

			IFeature vertexRow = vertexClass.CreateFeature();
			vertexRow.Shape =
				GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(201, 199));
			vertexRow.Store();
			IFeature vertexRow1 = vertexClass.CreateFeature();
			vertexRow1.Shape =
				GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(199, 199));
			vertexRow1.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           PointTolerance = 3,
				           ReportCoordinates = true
			           };

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanReportNoVertexOnNearbyEdgeLineErrorInRightTile()
		{
			const string testName = "CanReportNoVertexOnNearbyEdgeLineErrorInRightTile";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryPolyline);

			IFeature lineRow = vertexClass.CreateFeature();
			lineRow.Shape = CurveConstruction.StartLine(100, 100)
			                                 .LineTo(199, 100)
			                                 .Curve;
			lineRow.Store();
			IFeature lineRow2 = vertexClass.CreateFeature();
			lineRow2.Shape = CurveConstruction.StartLine(201, 150)
			                                  .LineTo(201, 50)
			                                  .Curve;
			lineRow2.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           EdgeTolerance = 3
			           };

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "VertexCoincidence.NoVertexOnNearbyEdge.DifferentFeature");
		}

		[Test]
		public void CanReportNoVertexOnNearbyEdgeLineErrorInLeftTile()
		{
			const string testName = "CanReportNoVertexOnNearbyEdgeLineErrorInLeftTile";

			IFeatureClass vertexClass = CreateFeatureClass(
				$"{testName}_vertex", esriGeometryType.esriGeometryPolyline);

			IFeature lineRow = vertexClass.CreateFeature();
			lineRow.Shape = CurveConstruction.StartLine(300, 100)
			                                 .LineTo(201, 100)
			                                 .Curve;
			lineRow.Store();
			IFeature lineRow2 = vertexClass.CreateFeature();
			lineRow2.Shape = CurveConstruction.StartLine(199, 150)
			                                  .LineTo(199, 50)
			                                  .Curve;
			lineRow2.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaVertexCoincidenceSelf(vertexClass)
			           {
				           EdgeTolerance = 3
			           };

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "VertexCoincidence.NoVertexOnNearbyEdge.DifferentFeature");
		}

		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType type,
		                                         bool zAware = false)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape",
					type,
					sref, 1000, zAware));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}
	}
}

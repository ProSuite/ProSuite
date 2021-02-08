using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaNonEmptyGeometryTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanTestNonEmptyGeometry_Polyline()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature feature = fc.CreateFeature();

			feature.Shape = CurveConstruction.StartLine(0, 0).LineTo(10, 10).Curve;
			feature.Store();

			var test = new QaNonEmptyGeometry(fc);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanTestNonEmptyGeometry_Point()
		{
			IFeatureClass fc = new FeatureClassMock(1, "PointFc",
			                                        esriGeometryType.esriGeometryPoint);

			IFeature feature = fc.CreateFeature();

			feature.Shape = GeometryFactory.CreatePoint(0, 0);
			feature.Store();

			var test = new QaNonEmptyGeometry(fc);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanTestNullGeometry()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature feature = fc.CreateFeature();

			feature.Shape = null;
			feature.Store();

			var test = new QaNonEmptyGeometry(fc);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner, "EmptyGeometry.GeometryNull", out error);
		}

		[Test]
		public void CanTestEmptyGeometry()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature feature = fc.CreateFeature();

			feature.Shape = new PolylineClass();
			feature.Store();

			var test = new QaNonEmptyGeometry(fc);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner, "EmptyGeometry.GeometryEmpty", out error);
		}

		[Test]
		public void CanTestNullGeometry_DontFilterPolycurvesByZeroLength()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature feature = fc.CreateFeature();

			feature.Shape = null;
			feature.Store();

			const bool dontFilterPolycurvesByZeroLength = true;
			var test = new QaNonEmptyGeometry(fc, dontFilterPolycurvesByZeroLength);

			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner, "EmptyGeometry.GeometryNull", out error);
		}
	}
}

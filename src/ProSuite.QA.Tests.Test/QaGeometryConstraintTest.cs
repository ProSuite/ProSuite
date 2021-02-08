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
	public class QaGeometryConstraintTest
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
		public void CanTestPolylineFeature()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature feature = fc.CreateFeature();

			feature.Shape = CurveConstruction.StartLine(0, 0)
			                                 .LineTo(10, 10)
			                                 .CircleTo(30, 10)
			                                 .Curve;
			feature.Store();

			var test = new QaGeometryConstraint(fc, "$CircularArcCount = 0", perPart: false);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner, "GeometryConstraint.ConstraintNotFulfilled.ForShape",
			                     out error);
			Assert.True(GeometryUtils.AreEqual(feature.Shape, error.Geometry));
		}

		[Test]
		public void CanTestPolylineFeaturePath()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature feature = fc.CreateFeature();

			IPolycurve correctPath = CurveConstruction.StartLine(100, 100)
			                                          .LineTo(110, 110)
			                                          .Curve;
			// note: union converts linear circular arcs to lines -> make sure the arc is not linear
			IPolycurve incorrectPath = CurveConstruction.StartLine(0, 0)
			                                            .LineTo(10, 10)
			                                            .CircleTo(30, 10)
			                                            .Curve;
			feature.Shape = GeometryUtils.Union(correctPath, incorrectPath);
			feature.Store();

			var test = new QaGeometryConstraint(fc, "$CircularArcCount = 0", perPart: true);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error;
			AssertUtils.OneError(runner,
			                     "GeometryConstraint.ConstraintNotFulfilled.ForShapePart",
			                     out error);

			Assert.True(GeometryUtils.AreEqual(incorrectPath,
			                                   GeometryFactory.CreatePolyline(error.Geometry)));
		}
	}
}

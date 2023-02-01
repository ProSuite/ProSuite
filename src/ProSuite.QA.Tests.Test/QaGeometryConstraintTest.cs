using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGeometryConstraintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestPolylineFeature()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

			IFeature feature = fc.CreateFeature();

			feature.Shape = CurveConstruction.StartLine(0, 0)
			                                 .LineTo(10, 10)
			                                 .CircleTo(30, 10)
			                                 .Curve;
			feature.Store();

			var test = new QaGeometryConstraint(ReadOnlyTableFactory.Create(fc),
			                                    "$CircularArcCount = 0", perPart: false);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error =
				AssertUtils.OneError(runner, "GeometryConstraint.ConstraintNotFulfilled.ForShape");
			Assert.True(GeometryUtils.AreEqual(feature.Shape, error.Geometry));
		}

		[Test]
		public void CanTestPolylineFeaturePath()
		{
			IFeatureClass fc = new FeatureClassMock("LineFc",
			                                        esriGeometryType.esriGeometryPolyline, 1);

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

			var test = new QaGeometryConstraint(ReadOnlyTableFactory.Create(fc),
			                                    "$CircularArcCount = 0", perPart: true);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);

			QaError error =
				AssertUtils.OneError(
					runner, "GeometryConstraint.ConstraintNotFulfilled.ForShapePart");

			Assert.True(GeometryUtils.AreEqual(incorrectPath,
			                                   GeometryFactory.CreatePolyline(error.Geometry)));
		}
	}
}

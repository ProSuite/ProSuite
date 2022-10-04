using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaCurveTest
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
		public void ValidateCustomParameters()
		{
			IFeatureClass fc = new FeatureClassMock(1, "LineFc",
			                                        esriGeometryType.esriGeometryPolyline);

			IFeature f = fc.CreateFeature();

			f.Shape = CurveConstruction.StartLine(0, 0)
			                           .LineTo(10, 10)
			                           .BezierTo(15, 15, 20, 20, 25, 20)
			                           .CircleTo(30, 30)
			                           .BezierTo(40, 40, 45, 45, 50, 45)
			                           .BezierTo(60, 45, 70, 45, 80, 40)
			                           .Curve;
			f.Store();

			var test = new QaCurve(ReadOnlyTableFactory.Create(fc));
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);

			// Group by curve type
			test = new QaCurve(ReadOnlyTableFactory.Create(fc)) {GroupIssuesBySegmentType = true};

			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(3, runner.Errors.Count);

			// allow circular arcs
			test = new QaCurve(ReadOnlyTableFactory.Create(fc))
			       {
				       AllowedNonLinearSegmentTypes =
					       new[]
					       {
						       NonLinearSegmentType.CircularArc
					       }
			       };

			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(2, runner.Errors.Count);

			// allow circular arcs and beziers
			test = new QaCurve(ReadOnlyTableFactory.Create(fc))
			       {
				       AllowedNonLinearSegmentTypes =
					       new[]
					       {
						       NonLinearSegmentType.CircularArc,
						       NonLinearSegmentType.Bezier
					       }
			       };

			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);
		}
	}
}

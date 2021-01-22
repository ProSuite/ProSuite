using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaInteriorRingsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanReportAllInteriorRingsCombined()
		{
			QaTestRunner testRunner = RunTestOnFeature();

			QaError error;
			AssertUtils.OneError(testRunner, "InteriorRings.UnallowedInteriorRings", out error);

			// message from previous releases, must be maintained for these parameters
			Assert.AreEqual(
				"Polygon has 4 interior ring(s), the maximum allowed number of interior rings is 0",
				error.Description);
		}

		[Test]
		public void CanReportSurplusInteriorRingsCombined()
		{
			QaTestRunner testRunner = RunTestOnFeature(maximumInteriorRingCount: 2);

			QaError error;
			AssertUtils.OneError(testRunner, "InteriorRings.UnallowedInteriorRings", out error);

			// message from previous releases, must be maintained for these parameters
			Assert.AreEqual(
				"Polygon has 4 interior ring(s), the maximum allowed number of interior rings is 2",
				error.Description);

			AssertErrorPartCount(2, error);
		}

		[Test]
		public void CanReportSurplusInteriorRingsCombinedAllRings()
		{
			QaTestRunner testRunner = RunTestOnFeature(
				maximumInteriorRingCount: 2,
				reportOnlySmallestRingsExceedingMaximumCount: false);

			QaError error;
			AssertUtils.OneError(testRunner, "InteriorRings.UnallowedInteriorRings", out error);

			Assert.AreEqual(
				"Polygon has 4 interior ring(s), the maximum allowed number of interior rings is 2",
				error.Description);

			AssertErrorPartCount(4, error);
		}

		[Test]
		public void CanReportSurplusSmallInteriorRingsCombined()
		{
			QaTestRunner testRunner = RunTestOnFeature(maximumInteriorRingCount: 2,
			                                           ignoreInnerRingsLargerThan: 200);

			QaError error;
			AssertUtils.OneError(testRunner, "InteriorRings.UnallowedInteriorRings", out error);

			Assert.AreEqual(
				"Polygon has 4 interior ring(s), of which 3 are smaller than the minimum area (200); " +
				"the maximum allowed number of interior rings is 2",
				error.Description);

			AssertErrorPartCount(1, error);
		}

		[Test]
		public void CanReportSurplusSmallInteriorRingsCombinedAllRings()
		{
			QaTestRunner testRunner = RunTestOnFeature(
				maximumInteriorRingCount: 2,
				ignoreInnerRingsLargerThan: 200,
				reportOnlySmallestRingsExceedingMaximumCount: false);

			QaError error;
			AssertUtils.OneError(testRunner, "InteriorRings.UnallowedInteriorRings", out error);

			Assert.AreEqual(
				"Polygon has 4 interior ring(s), of which 3 are smaller than the minimum area (200); " +
				"the maximum allowed number of interior rings is 2",
				error.Description);

			AssertErrorPartCount(3, error);
		}

		[Test]
		public void CanReportSmallInteriorRingsCombined()
		{
			QaTestRunner testRunner = RunTestOnFeature(maximumInteriorRingCount: 0,
			                                           ignoreInnerRingsLargerThan: 10);

			QaError error;
			AssertUtils.OneError(testRunner, "InteriorRings.UnallowedInteriorRings", out error);

			Assert.AreEqual(
				"Polygon has 4 interior ring(s), of which 1 is smaller than the minimum area (10)",
				error.Description);
			AssertErrorPartCount(1, error);
		}

		[Test]
		public void CanReportAllInteriorRingsIndividually()
		{
			QaTestRunner testRunner = RunTestOnFeature(reportIndividualRings: true);

			Assert.AreEqual(4, testRunner.Errors.Count);
			foreach (QaError error in testRunner.Errors)
			{
				Assert.AreEqual("Polygon has 4 interior ring(s)", error.Description);
				AssertErrorPartCount(1, error);
			}
		}

		[Test]
		public void CanReportSurplusInteriorRingsIndividually()
		{
			QaTestRunner testRunner = RunTestOnFeature(maximumInteriorRingCount: 2,
			                                           reportIndividualRings: true);

			Assert.AreEqual(2, testRunner.Errors.Count);

			foreach (QaError error in testRunner.Errors)
			{
				Assert.AreEqual(
					"Polygon has 4 interior ring(s). The maximum allowed number of interior rings is 2",
					error.Description);
				AssertErrorPartCount(1, error);
			}
		}

		[Test]
		public void CanReportSurplusInteriorRingsIndividuallyAllRings()
		{
			QaTestRunner testRunner = RunTestOnFeature(
				maximumInteriorRingCount: 2,
				reportIndividualRings: true,
				reportOnlySmallestRingsExceedingMaximumCount: false);

			Assert.AreEqual(4, testRunner.Errors.Count);

			foreach (QaError error in testRunner.Errors)
			{
				Assert.AreEqual(
					"Polygon has 4 interior ring(s). The maximum allowed number of interior rings is 2",
					error.Description);
				AssertErrorPartCount(1, error);
			}
		}

		[Test]
		public void CanReportSurplusSmallInteriorRingsIndividually()
		{
			QaTestRunner testRunner = RunTestOnFeature(maximumInteriorRingCount: 2,
			                                           ignoreInnerRingsLargerThan: 200,
			                                           reportIndividualRings: true);

			Assert.AreEqual(1, testRunner.Errors.Count);
			Assert.AreEqual(
				string.Format(
					"Polygon has 4 interior ring(s), of which 3 are smaller than the minimum area. " +
					"Area of this ring is {0:N2} < {1:N2}. The maximum allowed number of smaller interior rings is 2",
					1, 200),
				testRunner.Errors[0].Description);
		}

		[Test]
		public void CanReportSurplusSmallInteriorRingsIndividuallyAllRings()
		{
			QaTestRunner testRunner = RunTestOnFeature(
				maximumInteriorRingCount: 2,
				ignoreInnerRingsLargerThan: 200,
				reportIndividualRings: true,
				reportOnlySmallestRingsExceedingMaximumCount: false);

			Assert.AreEqual(3, testRunner.Errors.Count);
			Assert.AreEqual(
				string.Format(
					"Polygon has 4 interior ring(s), of which 3 are smaller than the minimum area. " +
					"Area of this ring is {0:N2} < {1:N2}. The maximum allowed number of smaller interior rings is 2",
					100, 200),
				testRunner.Errors[0].Description);
			Assert.AreEqual(
				string.Format(
					"Polygon has 4 interior ring(s), of which 3 are smaller than the minimum area. " +
					"Area of this ring is {0:N2} < {1:N2}. The maximum allowed number of smaller interior rings is 2",
					25, 200),
				testRunner.Errors[1].Description);
			Assert.AreEqual(
				string.Format(
					"Polygon has 4 interior ring(s), of which 3 are smaller than the minimum area. " +
					"Area of this ring is {0:N2} < {1:N2}. The maximum allowed number of smaller interior rings is 2",
					1, 200),
				testRunner.Errors[2].Description);
		}

		[Test]
		public void CanReportSmallInteriorRingsIndividually()
		{
			QaTestRunner testRunner = RunTestOnFeature(maximumInteriorRingCount: 0,
			                                           ignoreInnerRingsLargerThan: 10,
			                                           reportIndividualRings: true);

			Assert.AreEqual(1, testRunner.Errors.Count);
			Assert.AreEqual(
				string.Format(
					"Polygon has 4 interior ring(s), of which 1 is smaller than the minimum area. " +
					"Area of this ring is {0:N2} < {1:N2}",
					1, 10),
				testRunner.Errors[0].Description);
			AssertErrorPartCount(1, testRunner.Errors[0]);
		}

		private static void AssertErrorPartCount(int errorPartCount, [NotNull] QaError error)
		{
			IGeometry errorGeometry = error.Geometry;
			Assert.NotNull(errorGeometry);
			Assert.AreEqual(errorPartCount, GeometryUtils.GetPartCount(errorGeometry));
		}

		[NotNull]
		private static QaTestRunner RunTestOnFeature(
			int maximumInteriorRingCount = 0,
			double ignoreInnerRingsLargerThan = -1,
			bool reportIndividualRings = false,
			bool reportOnlySmallestRingsExceedingMaximumCount = true)
		{
			IFeature feature = CreateTestFeature();

			var test = new QaInteriorRings((IFeatureClass) feature.Class,
			                               maximumInteriorRingCount)
			           {
				           IgnoreInnerRingsLargerThan = ignoreInnerRingsLargerThan,
				           ReportIndividualRings = reportIndividualRings,
				           ReportOnlySmallestRingsExceedingMaximumCount =
					           reportOnlySmallestRingsExceedingMaximumCount
			           };

			var testRunner = new QaTestRunner(test);
			testRunner.Execute(feature);

			return testRunner;
		}

		/// <summary>
		/// Returns a feature in a mock feature class, with a polygon with one outer ring and 4 inner rings,
		/// with the following sizes:
		/// - 1m2
		/// - 25m2
		/// - 100m2
		/// - 400m2
		/// </summary>
		/// <returns></returns>
		[NotNull]
		private static IFeature CreateTestFeature()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			var featureClass = new FeatureClassMock(1, "polygons",
			                                        esriGeometryType.esriGeometryPolygon,
			                                        esriFeatureType.esriFTSimple,
			                                        sref, false, false);

			IFeature feature = featureClass.CreateFeature(
				CreatePolygon(sref,
				              CreateRing(0, 0, 100, 100), // outer ring
				              CreateRing(10, 10, 11, 11), // 1m2
				              CreateRing(20, 20, 25, 25), // 25m2
				              CreateRing(30, 30, 40, 40), // 100m2
				              CreateRing(50, 50, 70, 70)) // 400m2
			);
			return feature;
		}

		[NotNull]
		private static IRing CreateRing(double xmin, double ymin, double xmax, double ymax)
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(xmin, ymin, xmax, ymax);

			return (IRing) ((IGeometryCollection) polygon).Geometry[0];
		}

		[NotNull]
		private static IPolygon CreatePolygon([NotNull] ISpatialReference spatialReference,
		                                      [NotNull] params IRing[] rings)
		{
			IPolygon result = new PolygonClass();
			result.SpatialReference = spatialReference;

			var resultCollection = (IGeometryCollection) result;

			object missing = Type.Missing;

			foreach (IRing ring in rings)
			{
				resultCollection.AddGeometry(ring, ref missing, ref missing);
			}

			GeometryUtils.Simplify(result, true);

			return result;
		}
	}
}

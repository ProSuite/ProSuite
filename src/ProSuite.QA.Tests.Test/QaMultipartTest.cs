using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMultipartTest
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
		public void CanReportMultipleExteriorRings()
		{
			IFeature feature = CreatePolygonFeature();

			QaTestRunner testRunner = RunTestOnFeature(feature);

			QaError error;
			AssertUtils.OneError(testRunner, "Multipart.MultipleExteriorRings", out error);
			Assert.True(error.InvolvedRows.Count == 1);
			// message from previous releases, must be maintained for these parameters
			Assert.AreEqual("Polygon has 2 exterior rings, allowed is 1", error.Description);
		}

		[Test]
		public void CanReportMultipleParts()
		{
			IFeature feature = CreatePolygonFeature();

			QaTestRunner testRunner = RunTestOnFeature(feature, singleRing: true);

			QaError error;
			AssertUtils.OneError(testRunner, "Multipart.MultipleParts", out error);
			Assert.True(error.InvolvedRows.Count == 1);
			// message from previous releases, must be maintained for these parameters
			Assert.AreEqual("Geometry has 4 parts, allowed is 1", error.Description);
		}

		[NotNull]
		private static QaTestRunner RunTestOnFeature([NotNull] IFeature feature,
		                                             bool singleRing = false)
		{
			var test = new QaMultipart(ReadOnlyTableFactory.Create((IFeatureClass) feature.Class),
			                           singleRing);

			var testRunner = new QaTestRunner(test);
			testRunner.Execute(feature);

			return testRunner;
		}

		/// <summary>
		/// Returns a feature in a mock feature class, with a polygon with two outer rings and two inner rings
		/// </summary>
		[NotNull]
		private static IFeature CreatePolygonFeature()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			var featureClass = new FeatureClassMock("polygons",
			                                        esriGeometryType.esriGeometryPolygon,
			                                        null,
			                                        esriFeatureType.esriFTSimple, sref, false);

			IFeature feature = featureClass.CreateFeature(
				CreatePolygon(sref,
				              CreateRing(0, 0, 100, 100), // outer ring
				              CreateRing(110, 110, 210, 210), // outer ring
				              CreateRing(20, 20, 25, 25), // 25m2
				              CreateRing(30, 30, 40, 40)) // 100m2
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

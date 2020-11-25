using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class TestUtilsTest
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
		public void CanGetUniqueSpatialReference()
		{
			ISpatialReference sref1 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
			ISpatialReference sref2 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
			ISpatialReference sref3 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			const double maxRes = 0.00001;
			SpatialReferenceUtils.SetXYDomain(sref1, 0, 0, 1000, 1000, maxRes * 100,
			                                  0.01);
			SpatialReferenceUtils.SetXYDomain(sref2, 0, 0, 1000, 1000, maxRes, 0.0001);
			SpatialReferenceUtils.SetXYDomain(sref3, 0, 0, 1000, 1000, maxRes * 10,
			                                  0.001);

			var spatialReferences = new List<ISpatialReference> {sref1, sref2, sref3};

			ISpatialReference uniqueSpatialReference =
				TestUtils.GetUniqueSpatialReference(
					spatialReferences, requireEqualVerticalCoordinateSystems: true);

			Assert.IsNotNull(uniqueSpatialReference);

			Assert.AreEqual(maxRes,
			                SpatialReferenceUtils.GetXyResolution(
				                uniqueSpatialReference));
		}

		[Test]
		public void CanGetUniqueSpatialReferenceIgnoringVerticalCoordinateSystems()
		{
			ISpatialReference sref1 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
			ISpatialReference sref2 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);
			ISpatialReference sref3 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			const double maxRes = 0.00001;
			SpatialReferenceUtils.SetXYDomain(sref1, 0, 0, 1000, 1000, maxRes * 100,
			                                  0.01);
			SpatialReferenceUtils.SetXYDomain(sref2, 0, 0, 1000, 1000, maxRes * 10,
			                                  0.001);
			SpatialReferenceUtils.SetXYDomain(sref3, 0, 0, 1000, 1000, maxRes, 0.0001);

			var spatialReferences = new List<ISpatialReference> {sref1, sref2, sref3};

			ISpatialReference uniqueSpatialReference =
				TestUtils.GetUniqueSpatialReference(
					spatialReferences, requireEqualVerticalCoordinateSystems: false);

			Assert.IsNotNull(uniqueSpatialReference);

			Assert.AreEqual(maxRes,
			                SpatialReferenceUtils.GetXyResolution(
				                uniqueSpatialReference));
		}

		[Test]
		public void CanDetectNonUniqueSpatialReference()
		{
			ISpatialReference sref1 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
			ISpatialReference sref2 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV03);
			ISpatialReference sref3 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			SpatialReferenceUtils.SetXYDomain(sref1, 0, 0, 1000, 1000, 0.001, 0.01);
			SpatialReferenceUtils.SetXYDomain(sref2, 0, 0, 1000, 1000, 0.00001, 0.0001);
			SpatialReferenceUtils.SetXYDomain(sref3, 0, 0, 1000, 1000, 0.0001, 0.001);

			var spatialReferences = new List<ISpatialReference> {sref1, sref2, sref3};

			string exception = null;
			try
			{
				TestUtils.GetUniqueSpatialReference(spatialReferences,
				                                    requireEqualVerticalCoordinateSystems
				                                    : true);
			}
			catch (Exception ex)
			{
				exception = ex.Message;
			}

			Assert.True(exception == "Coordinate systems are not equal: CH1903_LV03, CH1903+_LV95");
		}

		[Test]
		public void CanDetectNonUniqueVerticalCoordinateSystem()
		{
			ISpatialReference sref1 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
			ISpatialReference sref2 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);
			ISpatialReference sref3 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			SpatialReferenceUtils.SetXYDomain(sref1, 0, 0, 1000, 1000, 0.001, 0.01);
			SpatialReferenceUtils.SetXYDomain(sref2, 0, 0, 1000, 1000, 0.00001, 0.0001);
			SpatialReferenceUtils.SetXYDomain(sref3, 0, 0, 1000, 1000, 0.0001, 0.001);

			var spatialReferences = new List<ISpatialReference> {sref1, sref2, sref3};

			string exception = null;
			try
			{
				TestUtils.GetUniqueSpatialReference(spatialReferences,
				                                    requireEqualVerticalCoordinateSystems
				                                    : true);
			}
			catch (Exception ex)
			{
				exception = ex.Message;
			}

			Assert.True(exception ==
			            "Defined vertical coordinate systems are not equal: LHN95, LN_1902");
		}

		[Test]
		public void CanEnsureMinimumEnvelopeSize()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var env = GeometryFactory.CreateEnvelope(10000, 100, 10000, 100, sref);
			var centroid = ((IArea) env).Centroid;
			var minSize = 1;
			var minSizeEnv = TestUtils.EnsureMinimumSize(env, minSize);

			Assert.AreEqual(minSize, minSizeEnv.Width);
			Assert.AreEqual(minSize, minSizeEnv.Height);
			Assert.True(GeometryUtils.AreEqual(centroid, ((IArea) minSizeEnv).Centroid));
		}

		[Test]
		public void CanEnsureMinimumEnvelopeSizeWidthOnly()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var env = GeometryFactory.CreateEnvelope(10000, 100, 10000, 200, sref);
			var centroid = ((IArea) env).Centroid;
			var minSize = 1;
			var minSizeEnv = TestUtils.EnsureMinimumSize(env, minSize);

			Assert.AreEqual(minSize, minSizeEnv.Width);
			Assert.AreEqual(100, minSizeEnv.Height);
			Assert.True(GeometryUtils.AreEqual(centroid, ((IArea) minSizeEnv).Centroid));
		}

		[Test]
		public void CanEnsureMinimumEnvelopeSizeHeightOnly()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var env = GeometryFactory.CreateEnvelope(10000, 100, 20000, 100, sref);
			var centroid = ((IArea) env).Centroid;
			var minSize = 1;
			var minSizeEnv = TestUtils.EnsureMinimumSize(env, minSize);

			Assert.AreEqual(10000, minSizeEnv.Width);
			Assert.AreEqual(minSize, minSizeEnv.Height);
			Assert.True(GeometryUtils.AreEqual(centroid, ((IArea) minSizeEnv).Centroid));
		}
	}
}

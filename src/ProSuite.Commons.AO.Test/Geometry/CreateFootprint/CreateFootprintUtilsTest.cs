using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;

namespace ProSuite.Commons.AO.Test.Geometry.CreateFootprint
{
	[TestFixture]
	public class CreateFootprintUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanCreateFootprintOnSlightOverlaps()
		{
			// TLM_GEBAEUDE {4575A818-C620-4ECF-BF93-1C8173E244A9 }
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithSliverOverlapsTop5759.wkb", sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, null, out _);

			Assert.IsNotNull(footprintGeom);

			Assert.AreEqual(65.630225, footprintGeom.Length, 0.01);
			Assert.AreEqual(238.567801, ((IArea) footprintGeom).Area, 0.01);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			GeometryUtils.Simplify(footprintAo);

			// NOTE: In 10.8.1 the AO footprint is incorrect (entire ring is missing)
			//Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}
	}
}

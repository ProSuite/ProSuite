using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Geometry;
using ProSuite.QA.Container.TestContainer;
using Pnt = ProSuite.Commons.Geometry.Pnt;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class OverlappingFeaturesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			_lic.Release();
		}

		[Test]
		public void CanRegisterTestedFeature()
		{
			var overlaps = new OverlappingFeatures();

			var fc = new FeatureClassMock(1, "", esriGeometryType.esriGeometryPolyline);
			IFeature feature = fc.CreateFeature(new Pt(0, 0), new Pt(9.95, 9.95));
			var cachedRow = new CachedRow(feature);

			overlaps.RegisterTestedFeature(cachedRow, null);
		}

		[Test]
		public void CheckWasAlreadyTested()
		{
			var overlaps = new OverlappingFeatures();

			var fc = new FeatureClassMock(1, "", esriGeometryType.esriGeometryPolyline);
			IFeature f1 = fc.CreateFeature(new Pt(100, 0), new Pt(109.95, 9.95));
			IFeature f2 = fc.CreateFeature(new Pt(100, 0), new Pt(109.8, 9.8));
			IFeature fx = fc.CreateFeature(new Pt(100, 0), new Pt(109.95, 9.8));
			IFeature fy = fc.CreateFeature(new Pt(100, 0), new Pt(109.8, 9.95));
			var c1 = new CachedRow(f1);

			var test = new VerifyingContainerTest(fc);
			var t2 = new VerifyingContainerTest(fc);
			var t3 = new VerifyingContainerTest(fc);

			overlaps.RegisterTestedFeature(c1, new ContainerTest[] {test});

			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));

			overlaps.RegisterTestedFeature(new CachedRow(f2), new ContainerTest[] {test});
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, test));
			overlaps.RegisterTestedFeature(new CachedRow(f2),
			                               new ContainerTest[] {test, t2});
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, t2));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, t3));

			overlaps.RegisterTestedFeature(new CachedRow(fx), new ContainerTest[] {test});
			overlaps.RegisterTestedFeature(new CachedRow(fy), new ContainerTest[] {test});

			overlaps.SetCurrentTile(CreateBox(100, 0, 105, 5));

			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fx, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fy, test));

			overlaps.AdaptSearchTolerance(fc, 0.1);
			overlaps.SetCurrentTile(CreateBox(110, 0, 120, 10));
			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fx, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fy, test));

			overlaps.SetCurrentTile(CreateBox(100, 10, 110, 20));
			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fx, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fy, test));

			overlaps.SetCurrentTile(CreateBox(110, 10, 120, 20));
			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fx, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fy, test));

			overlaps.SetCurrentTile(CreateBox(110.1, 10, 120, 20));
			Assert.IsFalse(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fx, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fy, test));
		}

		[Test]
		public void TestBaseRowTypes()
		{
			var overlaps = new OverlappingFeatures();

			var fc = new FeatureClassMock(1, "", esriGeometryType.esriGeometryPolyline);
			IFeature f1 = fc.CreateFeature(new Pt(100, 0), new Pt(109.95, 9.95));
			IFeature f2 = fc.CreateFeature(new Pt(100, 0), new Pt(109.8, 9.8));
			IFeature fx = fc.CreateFeature(new Pt(100, 0), new Pt(109.95, 9.8));
			IFeature fy = fc.CreateFeature(new Pt(100, 0), new Pt(109.8, 9.95));
			var c1 = new CachedRow(f1);

			var test = new VerifyingContainerTest(fc);
			var t2 = new VerifyingContainerTest(fc);
			var t3 = new VerifyingContainerTest(fc);

			overlaps.RegisterTestedFeature(c1, new ContainerTest[] {test});

			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));

			overlaps.RegisterTestedFeature(GetSimpleRow(f2), new ContainerTest[] {test});
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, test));
			overlaps.RegisterTestedFeature(GetSimpleRow(f2),
			                               new ContainerTest[] {test, t2});
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, t2));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, t3));

			overlaps.RegisterTestedFeature(GetSimpleRow(fx), new ContainerTest[] {test});
			overlaps.RegisterTestedFeature(GetSimpleRow(fy), new ContainerTest[] {test});

			overlaps.SetCurrentTile(CreateBox(100, 0, 105, 5));

			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(f2, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fx, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fy, test));

			overlaps.AdaptSearchTolerance(fc, 0.1);
			overlaps.SetCurrentTile(CreateBox(110, 0, 120, 10));
			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fx, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fy, test));

			overlaps.SetCurrentTile(CreateBox(100, 10, 110, 20));
			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fx, test));
			Assert.IsTrue(overlaps.WasAlreadyTested(fy, test));

			overlaps.SetCurrentTile(CreateBox(110, 10, 120, 20));
			Assert.IsTrue(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fx, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fy, test));

			overlaps.SetCurrentTile(CreateBox(110.1, 10, 120, 20));
			Assert.IsFalse(overlaps.WasAlreadyTested(f1, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(f2, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fx, test));
			Assert.IsFalse(overlaps.WasAlreadyTested(fy, test));
		}

		private static SimpleBaseRow GetSimpleRow(IFeature feature)
		{
			var row = new SimpleBaseRow(feature);
			return row;
		}

		private static Box CreateBox(double xMin, double yMin, double xMax, double yMax)
		{
			var box = new Box(CreatePoint(xMin, yMin), CreatePoint(xMax, yMax));
			return box;
		}

		private static Pnt CreatePoint(double x, double y)
		{
			Pnt p = Pnt.Create(2);
			p.X = x;
			p.Y = y;
			return p;
		}
	}
}
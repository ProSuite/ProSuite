using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class SegmentUtilsTest
	{
		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void CutCurveHullTest()
		{
			//seg0 = CreateSegment(GeometryFactory.CreatePoint(0, 0, 0), GeometryFactory.CreatePoint(100, 200, 10));
			//seg1 = CreateSegment(GeometryFactory.CreatePoint(0, 0, 0), GeometryFactory.CreatePoint(100, 200, 10));
			//SegmentUtils.CutCurveHull(seg0, seg1, 0, 0, true,
			//    out limits, out hullStartNear, out hullEndNear, out coincident);

			const double r = 0.0001;
			const double x0 = 600000;
			const double y0 = 200000;

			SegmentProxy seg0 = CreateSegment(
				GeometryFactory.CreatePoint(x0 + 100, y0 + 200, 10),
				GeometryFactory.CreatePoint(x0, y0, 500));
			SegmentProxy seg1 = CreateSegment(
				GeometryFactory.CreatePoint(x0, y0, 500 + 2 * r),
				GeometryFactory.CreatePoint(x0 - 200 - r, y0, 20));

			IList<double[]> limits;
			NearSegment hullStartNear;
			NearSegment hullEndNear;
			bool coincident;

			SegmentHull hull0 = seg0.CreateHull(0);
			SegmentHull hull1 = seg1.CreateHull(0);
			var pair2D = new SegmentPair2D(hull0, hull1);

			pair2D.CutCurveHull(0,
			                    out limits, out hullStartNear, out hullEndNear, out coincident);
			Assert.AreEqual(1, limits.Count);

			var pair3D = new SegmentPair3D(hull0, hull1);
			pair3D.CutCurveHull(0,
			                    out limits, out hullStartNear, out hullEndNear, out coincident);
			Assert.AreEqual(0, limits.Count);
		}

		[Test]
		public void CanCutRoundRound()
		{
			var cap = new RoundCap();

			SegmentProxy segment0 = CreateSegment(0, 0, 0, 10);
			var hull = new SegmentHull(segment0, 1, cap, cap);

			SegmentProxy segment1 = CreateSegment(-5, 5, 5, 5);
			var neighbor = new SegmentHull(segment1, 1, cap, cap);

			var pair = new SegmentPair2D(hull, neighbor);

			IList<double[]> limits;
			NearSegment startNear;
			NearSegment endNear;
			bool coincident;
			bool intersects = pair.CutCurveHull(0, out limits, out startNear, out endNear,
			                                    out coincident);

			Assert.IsTrue(intersects);
		}

		[Test]
		public void CanCutRoundRect()
		{
			var cap = new RoundCap();
			var rect = new RectCap(0);

			SegmentProxy segment0 = CreateSegment(0, 0, 0, 10);
			var hull = new SegmentHull(segment0, 1, cap, cap);

			SegmentProxy segment1 = CreateSegment(-5, 5, 5, 5);
			var neighbor = new SegmentHull(segment1, 1, cap, rect);

			var pair = new SegmentPair2D(hull, neighbor);

			IList<double[]> limits;
			NearSegment startNear;
			NearSegment endNear;
			bool coincident;
			bool intersects = pair.CutCurveHull(0, out limits, out startNear, out endNear,
			                                    out coincident);

			Assert.IsTrue(intersects);
		}

		[Test]
		public void CanCutRoundRect1()
		{
			var cap = new RoundCap();
			var rect = new RectCap(0.5);

			SegmentProxy segment0 = CreateSegment(0, 0, 0, 10);
			var hull = new SegmentHull(segment0, 1, cap, cap);

			SegmentProxy segment1 = CreateSegment(-5, 5, -1.49, 5);
			var neighbor = new SegmentHull(segment1, 1, cap, rect);

			var pair = new SegmentPair2D(hull, neighbor);

			IList<double[]> limits;
			NearSegment startNear;
			NearSegment endNear;
			bool coincident;
			bool intersects = pair.CutCurveHull(0, out limits, out startNear, out endNear,
			                                    out coincident);

			Assert.IsTrue(intersects);
		}

		[Test]
		public void CanCutRectRect()
		{
			var cap = new RoundCap();
			var rect = new RectCap(0.5);

			SegmentProxy segment0 = CreateSegment(0, 0, 0, 10);
			var hull = new SegmentHull(segment0, 1, cap, rect);

			SegmentProxy segment1 = CreateSegment(-5, 5, -1.49, 5);
			var neighbor = new SegmentHull(segment1, 1, cap, rect);

			var pair = new SegmentPair2D(hull, neighbor);

			IList<double[]> limits;
			NearSegment startNear;
			NearSegment endNear;
			bool coincident;
			bool intersects = pair.CutCurveHull(0, out limits, out startNear, out endNear,
			                                    out coincident);

			Assert.IsTrue(intersects);
		}

		[NotNull]
		private static SegmentProxy CreateSegment(double x0, double y0, double x1, double y1)
		{
			return CreateSegment(
				GeometryFactory.CreatePoint(x0, y0),
				GeometryFactory.CreatePoint(x1, y1));
		}

		[NotNull]
		private static SegmentProxy CreateSegment([NotNull] IPoint p0, [NotNull] IPoint p1)
		{
			IPolyline l0 = GeometryFactory.CreateLine(p0, p1);
			ISegment s0 = ((ISegmentCollection) l0).get_Segment(0);
			return new AoSegmentProxy(s0, 0, 0);
		}
	}
}

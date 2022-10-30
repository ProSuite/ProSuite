using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class ZRangeUtilsTest
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
		public void CanSplitSegmentAtZValueVeryNearLimit()
		{
			ISegment segment = new LineClass
			                   {
				                   FromPoint = GeometryFactory.CreatePoint(0, 0, 0),
				                   ToPoint = GeometryFactory.CreatePoint(0, 1, 1000)
			                   };

			ISegment fromSegment;
			ISegment toSegment;
			ZRangeUtils.SplitSegmentAtZValue(segment, 0, 1, 0.0000000001,
			                                 out fromSegment, out toSegment);

			Assert.IsNotNull(fromSegment);
			Assert.IsNotNull(toSegment);
		}

		[Test]
		public void CanGetPolygonErrorSegmentsAllAbove()
		{
			var polygon = new PolygonClass();
			GeometryUtils.MakeZAware(polygon);

			var points = (IPointCollection) polygon;
			object o = Type.Missing;
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 200), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 0, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 200), ref o, ref o);

			GeometryUtils.Simplify(polygon, false);

			Console.WriteLine(GeometryUtils.ToString(polygon));
			IRing ring = new List<IRing>(GeometryUtils.GetRings(polygon))[0];

			var list = new List<ZRangeErrorSegments>(ZRangeUtils.GetErrorSegments(ring, 0, 50));
			Assert.AreEqual(1, list.Count);

			ZRangeErrorSegments segments = list[0];
			Assert.AreEqual(4, segments.SegmentCount);

			IPolyline polyline = segments.CreatePolyline();
			Console.WriteLine(GeometryUtils.ToString(polyline));
		}

		[Test]
		public void CanGetPolygonErrorSegmentsAllWithin()
		{
			var polygon = new PolygonClass();
			GeometryUtils.MakeZAware(polygon);

			var points = (IPointCollection) polygon;
			object o = Type.Missing;
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 200), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 0, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 200), ref o, ref o);

			GeometryUtils.Simplify(polygon, false);

			Console.WriteLine(GeometryUtils.ToString(polygon));
			IRing ring = new List<IRing>(GeometryUtils.GetRings(polygon))[0];

			var list =
				new List<ZRangeErrorSegments>(ZRangeUtils.GetErrorSegments(ring, -100, 300));
			Assert.AreEqual(0, list.Count);
		}

		[Test]
		public void CanGetPolygonErrorSegmentsStartPointAbove()
		{
			var polygon = new PolygonClass();
			GeometryUtils.MakeZAware(polygon);

			var points = (IPointCollection) polygon;
			object o = Type.Missing;
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 200), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 0, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 200), ref o, ref o);

			GeometryUtils.Simplify(polygon, false);

			Console.WriteLine(GeometryUtils.ToString(polygon));
			IRing ring = new List<IRing>(GeometryUtils.GetRings(polygon))[0];

			var list = new List<ZRangeErrorSegments>(ZRangeUtils.GetErrorSegments(ring, 0, 150));
			Assert.AreEqual(1, list.Count);

			ZRangeErrorSegments segments = list[0];
			Assert.AreEqual(2, segments.SegmentCount);

			IPolyline polyline = segments.CreatePolyline();
			Console.WriteLine(GeometryUtils.ToString(polyline));
		}

		[Test]
		public void CanGetPolygonErrorSegmentsSecondPointAbove()
		{
			var polygon = new PolygonClass();
			GeometryUtils.MakeZAware(polygon);

			var points = (IPointCollection) polygon;
			object o = Type.Missing;
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 0, 200), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(100, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 100, 100), ref o, ref o);
			points.AddPoint(GeometryFactory.CreatePoint(0, 0, 100), ref o, ref o);

			GeometryUtils.Simplify(polygon, false);

			Console.WriteLine(GeometryUtils.ToString(polygon));
			IRing ring = new List<IRing>(GeometryUtils.GetRings(polygon))[0];

			var list = new List<ZRangeErrorSegments>(ZRangeUtils.GetErrorSegments(ring, 0, 150));
			Assert.AreEqual(1, list.Count);

			ZRangeErrorSegments segments = list[0];
			Assert.AreEqual(2, segments.SegmentCount);

			IPolyline polyline = segments.CreatePolyline();
			Console.WriteLine(GeometryUtils.ToString(polyline));
		}

		[Test]
		public void CantGetSplitRatioAscendingBelowZMin()
		{
			Assert.Throws<ArgumentException>(
				() => Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(0, 400, -100)));
		}

		[Test]
		public void CantGetSplitRatioAscendingAboveZMax()
		{
			Assert.Throws<ArgumentException>(
				() => Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(0, 400, 500)));
		}

		[Test]
		public void CantGetSplitRatioDescendingBelowZMin()
		{
			Assert.Throws<ArgumentException>(
				() => Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(400, 0, -100)));
		}

		[Test]
		public void CantGetSplitRatioDescendingAboveZMax()
		{
			Assert.Throws<ArgumentException>(
				() => Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(400, 0, 500)));
		}

		[Test]
		public void CantGetSplitRatioHorizontal()
		{
			Assert.Throws<ArgumentException>(
				() => Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(400, 400, 400)));
		}

		[Test]
		public void CanGetSplitRatioNegativeAscending0()
		{
			Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(-200, 200, -200));
		}

		[Test]
		public void CanGetSplitRatioAscending0()
		{
			Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(0, 400, 0));
		}

		[Test]
		public void CanGetSplitRatioAscending025()
		{
			Assert.AreEqual(0.25, ZRangeUtils.GetSplitRatio(0, 400, 100));
		}

		[Test]
		public void CanGetSplitRatioAscending05()
		{
			Assert.AreEqual(0.5, ZRangeUtils.GetSplitRatio(0, 400, 200));
		}

		[Test]
		public void CanGetSplitRatioAscending075()
		{
			Assert.AreEqual(0.75, ZRangeUtils.GetSplitRatio(0, 400, 300));
		}

		[Test]
		public void CanGetSplitRatioAscending1()
		{
			Assert.AreEqual(1, ZRangeUtils.GetSplitRatio(0, 400, 400));
		}

		[Test]
		public void CanGetSplitRatioNegativeDescending0()
		{
			Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(200, -200, 200));
		}

		[Test]
		public void CanGetSplitRatioDescending0()
		{
			Assert.AreEqual(0, ZRangeUtils.GetSplitRatio(400, 0, 400));
		}

		[Test]
		public void CanGetSplitRatioDescending025()
		{
			Assert.AreEqual(0.25, ZRangeUtils.GetSplitRatio(400, 0, 300));
		}

		[Test]
		public void CanGetSplitRatioDescending05()
		{
			Assert.AreEqual(0.5, ZRangeUtils.GetSplitRatio(400, 0, 200));
		}

		[Test]
		public void CanGetSplitRatioDescending075()
		{
			Assert.AreEqual(0.75, ZRangeUtils.GetSplitRatio(400, 0, 100));
		}

		[Test]
		public void CanGetSplitRatioDescending1()
		{
			Assert.AreEqual(1, ZRangeUtils.GetSplitRatio(400, 0, 0));
		}
	}
}

using System;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ControlPointUtilsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanCreateCircularArc()
	{
		// Due to the immutable geometries, setting a control point
		// on a polygon or polyline means we have to recreate two
		// segments. 
		MapPoint right = MapPointBuilderEx.CreateMapPoint(1, 0);
		MapPoint up = MapPointBuilderEx.CreateMapPoint(0, 1);
		MapPoint left = MapPointBuilderEx.CreateMapPoint(-1, 0);
		MapPoint down = MapPointBuilderEx.CreateMapPoint(0, -1);
		Coordinate2D origin = new Coordinate2D(0, 0);
		const ArcOrientation ccw = ArcOrientation.ArcCounterClockwise;

		// quarter circle:

		var arc = EllipticArcBuilderEx.CreateCircularArc(right, up, origin, ccw);

		Assert.True(arc.IsCircular);
		Assert.True(arc.IsMinor);

		// half circle: considered major because central angle >= pi

		arc = EllipticArcBuilderEx.CreateCircularArc(right, left, origin, ccw);

		Assert.True(arc.IsCircular);
		Assert.False(arc.IsMinor);
		Assert.AreEqual(Math.PI, arc.CentralAngle, 1E-7);

		// 3/4 circle:

		arc = EllipticArcBuilderEx.CreateCircularArc(right, down, origin, ccw);

		Assert.True(arc.IsCircular);
		Assert.False(arc.IsMinor);
		Assert.AreEqual(1.5 * Math.PI, arc.CentralAngle, 1E-7);

		// full circle:

		arc = EllipticArcBuilderEx.CreateCircularArc(right, right, origin, ccw);

		Assert.True(arc.IsCircular);
		Assert.False(arc.IsMinor);
		Assert.AreEqual(2 * Math.PI, arc.CentralAngle, 1E-7);
	}

	[Test]
	public void CanSetPointID()
	{
		var point = MapPointBuilderEx.CreateMapPoint(1.2, 3.4);
		Assert.IsFalse(point.HasID);
		Assert.AreEqual(0, point.ID);

		var point0 = ControlPointUtils.SetPointID(null, 42);
		Assert.IsNull(point0);

		var point1 = ControlPointUtils.SetPointID(point, 1);
		Assert.IsTrue(point1.HasID);
		Assert.AreEqual(1, point1.ID);

		var point2 = ControlPointUtils.SetPointID(point, 123);
		Assert.IsTrue(point2.HasID);
		Assert.AreEqual(123, point2.ID);

		var point3 = ControlPointUtils.SetPointID(point, 0);
		Assert.IsFalse(point3.HasID); // setting ID to zero clears HasID
		Assert.AreEqual(0, point3.ID);
	}

	[Test]
	public void CanSegmentSetPointID()
	{
		var line = LineBuilderEx.CreateLineSegment(Pt(0, 0), Pt(4, 2));
		Assert.False(line.StartPoint.HasID);
		Assert.AreEqual(0, line.StartPoint.ID);
		Assert.False(line.EndPoint.HasID);
		Assert.AreEqual(0, line.EndPoint.ID);
		
		var line0 = ControlPointUtils.SetPointID((Segment) null, null, null);
		Assert.IsNull(line0); // convenience: null is passed through

		var line1 = ControlPointUtils.SetPointID(line, 1, null);
		Assert.True(line1.StartPoint.HasID);
		Assert.AreEqual(1, line1.StartPoint.ID);
		Assert.False(line1.EndPoint.HasID);
		Assert.AreEqual(0, line1.EndPoint.ID);

		var line2 = ControlPointUtils.SetPointID(line, null, 2);
		Assert.False(line2.StartPoint.HasID);
		Assert.AreEqual(0, line2.StartPoint.ID);
		Assert.True(line2.EndPoint.HasID);
		Assert.AreEqual(2, line2.EndPoint.ID);

		var line3 = ControlPointUtils.SetPointID(line, 3, 4);
		Assert.True(line3.StartPoint.HasID);
		Assert.AreEqual(3, line3.StartPoint.ID);
		Assert.True(line3.EndPoint.HasID);
		Assert.AreEqual(4, line3.EndPoint.ID);

		var line4 = ControlPointUtils.SetPointID(line, null, null);
		Assert.AreSame(line, line4); // nothing updated: return same instance
	}

	[Test]
	public void CanBuilderSetPointID()
	{
		var env = GeometryFactory.CreateEnvelope(1, 2, 3, 4);
		var polygon = GeometryFactory.CreatePolygon(env);
		var builder = new PolygonBuilderEx(polygon);

		const int partIndex = 0;
		int pointCount = builder.GetPointCount(partIndex);
		for (int j = 0; j < pointCount; j++)
		{
			int id = j + 1;
			builder.SetPointID(partIndex, j, id);
		}

		var polygon1 = builder.ToGeometry();
		Assert.True(polygon1.HasID); // enabled by builder.SetPointID()

		var part = polygon1.Parts.Single();
		for (int j = 0; j < part.Count; j++)
		{
			var segment = part[j];
			if (j == 0)
			{
				// Beware: the ring's start point was overwritten
				// when we set the point ID on the ring's end point!
				Assert.IsTrue(segment.StartPoint.HasID);
				Assert.AreEqual(part.Count + 1, segment.StartPoint.ID);
			}
			else
			{
				Assert.IsTrue(segment.StartPoint.HasID);
				Assert.AreEqual(j + 1, segment.StartPoint.ID);
				Assert.IsTrue(segment.EndPoint.HasID);
				Assert.AreEqual(j + 2, segment.EndPoint.ID);
			}
		}
	}

	[Test]
	public void CanResetControlPoints()
	{
		var polygon = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(0, 0, 1), Pt(0, 3, 2), Pt(5, 5), Pt(5, 0, 1), Pt(0, 0, 1) });

		var p1 = ControlPointUtils.ResetControlPoints(polygon, out int c1);
		Assert.NotNull(p1);
		Assert.AreEqual(3, c1); // first/last point count as one for this change
		Assert.IsTrue(p1.Points.All(p => p.ID == 0));

		var p2 = ControlPointUtils.ResetControlPoints(polygon, out int c2, 2);
		Assert.NotNull(p2);
		Assert.AreEqual(1, c2);
		Assert.AreEqual(2, p2.Points.Count(p => p.ID == 0));
		Assert.AreEqual(3, p2.Points.Count(p => p.ID == 1));

		var p3 = ControlPointUtils.ResetControlPoints(polygon, out int c3, 1);
		Assert.NotNull(p3);
		Assert.AreEqual(2, c3); // first/last point count as one for this change
		Assert.AreEqual(4, p3.Points.Count(p => p.ID == 0));
		Assert.AreEqual(1, p3.Points.Count(p => p.ID == 2));

		var polyline = PolylineBuilderEx.CreatePolyline(polygon);

		var p4 = ControlPointUtils.ResetControlPoints(polyline, out int c4, 1);
		Assert.NotNull(p4);
		Assert.AreEqual(3, c4); // now first/last count as two because path (not ring)
		Assert.AreEqual(4, p4.Points.Count(p => p.ID == 0));
		Assert.AreEqual(1, p4.Points.Count(p => p.ID == 2));

		Assert.IsNull(ControlPointUtils.ResetControlPoints((Polygon) null, out _));

		// It also works straight on a MultipartBuilderEx instance:
		var builder = new PolylineBuilderEx(polyline);
		var c5 = ControlPointUtils.ResetControlPoints(builder);
		Assert.AreEqual(4, c5); // polyline builder: start/end counted separately
	}

	[Test]
	public void CanResetControlPointPairs()
	{
		var polygon1 = PolygonBuilderEx.CreatePolygon(
			new[]
			{
				Pt(0, 0), Pt(0, 3, 1), Pt(0, 6, 0), Pt(3, 6, 1),
				Pt(6, 6), Pt(6, 4, 2), Pt(6, 2, 2), Pt(6, 0), Pt(0, 0)
			});

		var p1 = ControlPointUtils.ResetControlPointPairs(polygon1, out int c1);
		Assert.NotNull(p1);
		Assert.AreEqual(4, c1);
		Assert.AreEqual(9, p1.Points.Count(p => p.ID == 0));

		var p2 = ControlPointUtils.ResetControlPointPairs(polygon1, out int c2, 1);
		Assert.NotNull(p2);
		Assert.AreEqual(2, c2);
		Assert.AreEqual(7, p2.Points.Count(p => p.ID == 0));
		Assert.AreEqual(2, p2.Points.Count(p => p.ID == 2));

		var peri1 = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(5, 1), Pt(5, 5), Pt(7, 5), Pt(7, 1) });
		var p3 = ControlPointUtils.ResetControlPointPairs(polygon1, out int c3, -1, peri1);
		Assert.NotNull(p3);
		Assert.AreEqual(2, c3);
		Assert.AreEqual(7, p3.Points.Count(p => p.ID == 0));
		Assert.AreEqual(2, p3.Points.Count(p => p.ID == 1));

		// pairs are reset if at least one endpoint is in perimeter:

		var peri2 = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(2, 3), Pt(2, 7), Pt(7, 7), Pt(7, 3) });
		var p4 = ControlPointUtils.ResetControlPointPairs(polygon1, out int c4, -1, peri2);
		Assert.NotNull(p4);
		Assert.AreEqual(4, c4);
		Assert.AreEqual(9, p4.Points.Count(p => p.ID == 0));

		// Special case: gap across Start/End, which really two gaps

		var polygon2 = PolygonBuilderEx.CreatePolygon(
			new[]
			{
				Pt(0, 0, 1), Pt(0, 3, 1), Pt(0, 6), Pt(6, 6),
				Pt(6, 0), Pt(3, 0, 1), Pt(0, 0)
			});

		var p5 = ControlPointUtils.ResetControlPointPairs(polygon2, out int c5);
		Assert.NotNull(p5);
		Assert.AreEqual(3, c5); // 2nd (last) gap was not closed!
		Assert.AreEqual(7, p5.Points.Count(p => p.ID == 0));

		// It also works for Polyline instances (notice that 2nd pair is not closed):
		var polyline1 = PolylineBuilderEx.CreatePolyline(
			new[]
			{
				Pt(0, 0), Pt(1, 0, 1), Pt(2, 0), Pt(3, 0, 1), Pt(4, 0), Pt(5, 0, 1), Pt(6, 0)
			});
		var p6 = ControlPointUtils.ResetControlPointPairs(polyline1, out int c6);
		Assert.AreEqual(typeof(Polyline), p6.GetType());
		Assert.AreEqual(3, c6);
		Assert.AreEqual(7, p6.Points.Count(p => p.ID == 0));

		// It also works straight on a MultipartBuilderEx instance:
		var builder = new PolylineBuilderEx(polygon2);
		var c7 = ControlPointUtils.ResetControlPointPairs(builder);
		Assert.AreEqual(3, c7);
	}

	#region Test utils

	private static MapPoint Pt(double x, double y)
	{
		return MapPointBuilderEx.CreateMapPoint(x, y);
	}

	private static MapPoint Pt(double x, double y, int id)
	{
		return MapPointBuilderEx.CreateMapPoint(x, y, false, 0, false, 0, true, id);
	}

	#endregion
}

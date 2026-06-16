using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
	public void CanCountControlPoints()
	{
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(null));

		var point1 = MapPointBuilderEx.CreateMapPoint(42, 53, 0.0, double.NaN, 1);
		Assert.AreEqual(1, ControlPointUtils.CountControlPoints(point1));

		var point2 = MapPointBuilderEx.CreateMapPoint(42, 53, 0.0, double.NaN, 0);
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(point2));

		var point3 = MapPointBuilderEx.CreateMapPoint(42, 53);
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(point3));

		var multipoint =
			MultipointBuilderEx.CreateMultipoint(
				new[] { Pt(1, 1), Pt(2, 2), Pt(3, 3, 1), Pt(4, 4), Pt(5, 5, 1) });
		Assert.AreEqual(2, ControlPointUtils.CountControlPoints(multipoint));

		var emptyMultipoint = new MultipointBuilderEx().ToGeometry();
		Assert.IsTrue(emptyMultipoint.IsEmpty);
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(emptyMultipoint));

		var polygon = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(0, 0, 1), Pt(0, 3, 2), Pt(5, 5), Pt(5, 0, 1), Pt(0, 0, 1) });
		Assert.AreEqual(4, ControlPointUtils.CountControlPoints(polygon));

		var emptyPolygon = new PolygonBuilderEx().ToGeometry();
		Assert.IsTrue(emptyPolygon.IsEmpty);
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(emptyPolygon));

		var emptyEnvelope = new EnvelopeBuilderEx().ToGeometry();
		Assert.IsTrue(emptyEnvelope.IsEmpty);
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(emptyEnvelope));

		var envelope = EnvelopeBuilderEx.CreateEnvelope(1, 2, 3, 4);
		Assert.AreEqual(0, ControlPointUtils.CountControlPoints(envelope));
	}

	[Test]
	public void CanGetAndSetControlPoint()
	{
		Polygon polygon = CreateMultipartPolygon();

		// CP at vertex#1 on part#0: initially zero
		Assert.AreEqual(0, ControlPointUtils.GetControlPoint(polygon, 0, 1));
		polygon = (Polygon)ControlPointUtils.SetControlPoint(polygon, 0, 1, 1);
		Assert.AreEqual(1, ControlPointUtils.GetControlPoint(polygon, 0, 1));

		// Set CP at FromPoint of part#0:
		polygon = (Polygon)ControlPointUtils.SetControlPoint(polygon, 0, 0, 2);
		Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0, 0));
		// Setting FromPoint also sets ToPoint on a closed geometry:
		Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0, 4));

		// Set CP at ToPoint of part#1:
		polygon = (Polygon)ControlPointUtils.SetControlPoint(polygon, 1, 4, 3);
		Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 1, 4));
		// Setting ToPoint also sets FromPoint on a closed geometry:
		Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 1, 0));

		// We have now 3 CPs, two on part#0 and one on part#1:
		polygon = ControlPointUtils.ResetControlPoints(polygon, out int reset, 1);
		Assert.AreEqual(1, reset);
		ControlPointUtils.ResetControlPoints(polygon, out reset); // all values
		Assert.AreEqual(2, reset);
	}

	[Test]
	public void CanResetControlPoints()
	{
		var polygon = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(0, 0, 1), Pt(0, 3, 2), Pt(5, 5), Pt(5, 0, 1), Pt(0, 0, 1) });

		var p1 = ControlPointUtils.ResetControlPoints(polygon, out _);
		Assert.NotNull(p1);
		//Assert.AreEqual(3, c1); // first/last point count as one for this change
		Assert.IsTrue(p1.Points.All(p => p.ID == 0));

		var p2 = ControlPointUtils.ResetControlPoints(polygon, out _, -1, 2);
		Assert.NotNull(p2);
		Assert.AreEqual(2, p2.Points.Count(p => p.ID == 0));
		Assert.AreEqual(3, p2.Points.Count(p => p.ID == 1));

		var p3 = ControlPointUtils.ResetControlPoints(polygon, out _, -1, 1);
		Assert.NotNull(p3);
		//Assert.AreEqual(2, c3); // first/last point count as one for this change
		Assert.AreEqual(4, p3.Points.Count(p => p.ID == 0));
		Assert.AreEqual(1, p3.Points.Count(p => p.ID == 2));

		var polyline = PolylineBuilderEx.CreatePolyline(polygon);

		var p4 = ControlPointUtils.ResetControlPoints(polyline, out _, -1, 1);
		Assert.NotNull(p4);
		//Assert.AreEqual(3, c4); // now first/last count as two because path (not ring)
		Assert.AreEqual(4, p4.Points.Count(p => p.ID == 0));
		Assert.AreEqual(1, p4.Points.Count(p => p.ID == 2));

		Assert.IsNull(ControlPointUtils.ResetControlPoints((Polygon) null));

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

	/// <summary>
	/// Create a two-part polygon according to the sketch below.
	/// Each ring consists of four segments; segment AB is linear,
	/// segment BC is a clockwise half-circle around center M,
	/// segment CD is a bezier curve with control points P and Q,
	/// and segment DA is the result of ring.Close().
	/// <code>
	/// 20 B----M----C
	/// 15 |  b-m-c  |
	/// 10 |  |   |  |P
	///  5 |  a---d Q|
	///  0 A---------D
	///    0   10   20
	/// </code>
	/// </summary>
	/// <remarks>
	/// Exterior rings are clockwise; interior rings are counterclockwise.
	/// </remarks>
	private static Polygon CreateMultipartPolygon()
	{
		var A = Pt(0, 0);
		var B = Pt(0, 20);
		var C = Pt(20, 20);
		var D = Pt(20, 0);
		var M = Pt(10, 20);
		var P = Pt(25, 10);
		var Q = Pt(15, 5);

		// Build segments for part 0 (exterior ring):
		var AB = LineBuilderEx.CreateLineSegment(A, B);
		var BC = EllipticArcBuilderEx.CreateCircularArc(
			B, C, M.Coordinate2D, ArcOrientation.ArcClockwise);
		var CD = CubicBezierBuilderEx.CreateCubicBezierSegment(C, P, Q, D);
		var DA = LineBuilderEx.CreateLineSegment(D, A); // explicit "Close()"

		var part0 = new List<Segment> { AB, BC, CD, DA };

		// Derive part 1 as a scaled/moved/orientation-reversed clone of part 0.
		// Geometries are immutable in AGP, so we transform via GeometryUtils:
		var polygon0 = PolygonBuilderEx.CreatePolygon(part0);
		var polygon1 = GeometryUtils.Scale(polygon0, A, 0.5, 0.5);
		polygon1 = GeometryUtils.Move(polygon1, 5, 5);
		polygon1 = GeometryUtils.ReverseOrientation(polygon1);

		Assert.AreEqual(5, polygon1.Points[0].X);
		Assert.AreEqual(5, polygon1.Points[0].Y);

		// Compose the multipart polygon from both rings:
		var builder = new PolygonBuilderEx();
		builder.AddPart(part0);
		builder.AddPart(polygon1.Parts[0]);
		var polygon = builder.ToGeometry();

		Assert.AreEqual(2, polygon.PartCount);

		return polygon;
	}

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

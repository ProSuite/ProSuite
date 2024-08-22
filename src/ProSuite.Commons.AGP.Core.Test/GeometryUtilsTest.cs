using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GeometryUtilsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void Can_get_distance_between_geometries()
	{
		var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
		var polygon = GeometryFactory.CreatePolygon(envelope);
		var mapPoint = MapPointBuilder.CreateMapPoint(50, 50);

		double distance = GeometryEngine.Instance.Distance(polygon, mapPoint);
		Assert.AreEqual(0, distance);
		Assert.False(GeometryUtils.Disjoint(polygon, mapPoint));

		ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);
		Assert.AreEqual(50, result.Distance);

		mapPoint = MapPointBuilder.CreateMapPoint(110, 100);

		distance = GeometryEngine.Instance.Distance(polygon, mapPoint);
		Assert.AreEqual(10, distance);
		Assert.True(GeometryUtils.Disjoint(polygon, mapPoint));
	}

	[Test]
	public void Can_get_nearest_point_to_geometry__point_inside_geometry()
	{
		var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
		var polygon = GeometryFactory.CreatePolygon(envelope);
		var mapPoint = MapPointBuilder.CreateMapPoint(75, 50);

		ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);

		Assert.NotNull(result);
		Assert.AreEqual(25, result.Distance);
		Assert.AreEqual(0, result.PartIndex);
		Assert.AreEqual(100, result.Point.X);
		Assert.AreEqual(50, result.Point.Y);
		Assert.True(result.IsRightSide);
	}

	[Test]
	public void Can_get_nearest_point_to_geometry__point_outside_geometry()
	{
		var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
		var polygon = GeometryFactory.CreatePolygon(envelope);
		var mapPoint = MapPointBuilder.CreateMapPoint(110, 100);

		ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);

		Assert.NotNull(result);
		Assert.AreEqual(10, result.Distance);
		Assert.AreEqual(0, result.PartIndex);
		Assert.AreEqual(100, result.Point.X);
		Assert.AreEqual(100, result.Point.Y);
		Assert.False(result.IsRightSide);
	}

	[Test]
	public void Can_get_nearest_point_to_geometry__point_on_geometry()
	{
		var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
		var polygon = GeometryFactory.CreatePolygon(envelope);
		var mapPoint = MapPointBuilder.CreateMapPoint(100, 50);

		ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);

		Assert.NotNull(result);
		Assert.AreEqual(0, result.Distance);
		Assert.AreEqual(0, result.PartIndex);
		Assert.AreEqual(100, result.Point.X);
		Assert.AreEqual(50, result.Point.Y);
		Assert.False(result.IsRightSide);
	}

	[Test]
	public void CheckLineSegmentAngle()
	{
		// About LineSegment.Angle property:
		// Documentation: angle in radians, ccw from positive x axis.
		// Empirical: angle is in range -pi..pi (not 0..2pi).

		var start = MapPointBuilderEx.CreateMapPoint(0, 0);
		var end = MapPointBuilderEx.CreateMapPoint(5, -5);
		var builder = new LineBuilderEx(start, end);
		var line = builder.ToSegment();

		const double delta = 0.000001;
		var angle = line.Angle;
		Assert.AreEqual(-Math.PI / 4, angle, delta);
	}

	[Test]
	public void CanPolygonBoundaryNull()
	{
		Assert.Null(GeometryUtils.Boundary(null));
	}

	[Test]
	public void CanPolygonBoundary()
	{
		var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 50);
		var polygon = GeometryFactory.CreatePolygon(envelope);
		var boundary = GeometryUtils.Boundary(polygon);
		Assert.NotNull(boundary);
		Assert.AreEqual(GeometryType.Polyline, boundary.GeometryType);
		Assert.AreEqual(polygon.PointCount, boundary.PointCount);
	}

	[Test]
	public void CanPolygonBoundaryPreserveCurves()
	{
		var polygon = GeometryFactory.CreateBezierCircle();
		Assert.True(polygon.HasCurves, "Oops, original polygon has no curves");
		var boundary = GeometryUtils.Boundary(polygon);

		Assert.NotNull(boundary);
		Assert.AreEqual(GeometryType.Polyline, boundary.GeometryType);
		Assert.True(boundary.HasCurves, "did not preserve curves");
		Assert.AreEqual(polygon.PointCount, boundary.PointCount);
	}

	[Test]
	public void CanDisjointEmpty()
	{
		var point = MapPointBuilderEx.CreateMapPoint(1.0, 1.0);
		var empty = PolygonBuilderEx.CreatePolygon();

		Assert.False(point.IsEmpty);
		Assert.True(empty.IsEmpty);

		Assert.True(GeometryUtils.Disjoint(point, empty));
		Assert.True(GeometryUtils.Disjoint(empty, point));
	}

	[Test]
	public void CanConnectedComponents()
	{
		var empty = PolygonBuilderEx.CreatePolygon();
		Assert.True(empty.IsEmpty);

		var r0 = GeometryUtils.ConnectedComponents(empty);
		Assert.NotNull(r0);
		Assert.IsEmpty(r0);

		var square = CreateUnitSquarePolygon(); // one (exterior) ring
		Assert.AreEqual(1, square.PartCount);

		var r1 = GeometryUtils.ConnectedComponents(square);
		Assert.NotNull(r1);
		Assert.AreEqual(1, r1.Count);
		Assert.AreEqual(1, r1.Single().PartCount);

		var polygon = CreateMultiPolygon();
		Assert.AreEqual(7, polygon.PartCount);

		var r2 = GeometryUtils.ConnectedComponents(polygon);
		Assert.NotNull(r2);
		Assert.AreEqual(4, r2.Count);
		var l2 = r2.OrderBy(p => p.Area).ToList();
		Assert.AreEqual(1, l2[0].PartCount);
		Assert.AreEqual(1, l2[1].PartCount);
		Assert.AreEqual(2, l2[2].PartCount);
		Assert.AreEqual(3, l2[3].PartCount);
	}

	[Test]
	public void CanRemoveHoles()
	{
		var empty = PolygonBuilderEx.CreatePolygon();
		Assert.True(empty.IsEmpty);

		var r0 = GeometryUtils.RemoveHoles(empty);
		Assert.NotNull(r0);
		Assert.True(r0.IsEmpty);
		Assert.AreEqual(0, r0.PartCount);

		var poly = CreateMultiPolygon();

		var r1 = GeometryUtils.RemoveHoles(poly);
		Assert.NotNull(r1);
		Assert.False(r1.IsEmpty);
		Assert.AreEqual(3, r1.PartCount);
		Assert.AreEqual(15, r1.PointCount);
		Assert.AreEqual(9 + 35 + 1, r1.Area, 0.001);
	}

	[Test]
	public void CanGetPointSetPoint()
	{
		const double delta = 0.00001;

		var square = CreateUnitSquarePolygon();

		var builder = new PolygonBuilderEx(square);

		var pointCount = builder.GetPointCount(0);
		Assert.AreEqual(square.PointCount, pointCount);

		builder.SetPoint(0, 2, Pt(2, 1));
		builder.SetPoint(0, 3, Pt(2, 0));

		var polygon2 = builder.ToGeometry();

		Assert.AreEqual(6.0, polygon2.Length, delta);
		Assert.AreEqual(2.0, polygon2.Area, delta);

		// Beware: setting the last point of a ring also sets its first point
		// (and vice versa); here we have a polygon, whose parts ar rings (closed paths):

		builder.SetPoint(0, 0, Pt(-1, 0)); // set first point
		var last = builder.GetPoint(0, pointCount - 1);
		Assert.AreEqual(-1.0, last.X, delta);
		Assert.AreEqual(0.0, last.Y, delta);

		builder.SetPoint(0, pointCount - 1, Pt(0.7, 0.3)); // set last point
		var first = builder.GetPoint(0, 0);
		Assert.AreEqual(0.7, first.X, delta);
		Assert.AreEqual(0.3, first.Y, delta);

		// However, if the builder is for a polyline, the start
		// and end point are distinct (even if they coincide):

		var polylineBuilder = new PolylineBuilderEx(square);
		pointCount = polylineBuilder.GetPointCount(0);
		polylineBuilder.SetPoint(0, 0, Pt(-1, 1)); // set first point
		polylineBuilder.SetPoint(0, pointCount - 1, Pt(1, -1)); // and last
		var start = polylineBuilder.GetPoint(0, 0); // check first point
		Assert.AreEqual(-1.0, start.X, delta);
		Assert.AreEqual(1.0, start.Y, delta);
		var end = polylineBuilder.GetPoint(0, pointCount - 1); // and last
		Assert.AreEqual(1.0, end.X, delta);
		Assert.AreEqual(-1.0, end.Y, delta);

		// Edge case: single-segment line

		var line = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(1, 0) });
		var builder3 = new PolylineBuilderEx(line);
		Assert.AreEqual(1, builder3.PartCount);
		Assert.AreEqual(1, builder3.Parts.Single().Count);
		builder3.SetPoint(0, 0, Pt(0, 1));
		builder3.SetPoint(0, 1, Pt(1, 2));
		Assert.AreEqual(1, builder3.GetPoint(0, 0).Y, delta);
		Assert.AreEqual(2, builder3.GetPoint(0, 1).Y, delta);

		// TODO Edge case: single-segment polygon
	}

	[Test]
	public void CanSegmentSetPoints()
	{
		var line = LineBuilderEx.CreateLineSegment(Pt(1, 1), Pt(2, 1));
		Assert.AreEqual(1.0, line.Length, 0.00001);
		line = GeometryUtils.SetPoints(line, Pt(0, 1), null);
		Assert.AreEqual(2.0, line.Length, 0.00001);
		line = GeometryUtils.SetPoints(line, null, Pt(3, 1));
		Assert.AreEqual(3.0, line.Length, 0.00001);

		var cubic = CubicBezierBuilderEx.CreateCubicBezierSegment(
			Pt(1, 1), Pt(1, 2), Pt(2, 1), Pt(2, 2));
		cubic = GeometryUtils.SetPoints(cubic, Pt(0, 0), Pt(3, 2));
		var extent = cubic.Get2DEnvelope();
		Assert.AreEqual(0.0, extent.XMin, 0.00001);
		Assert.AreEqual(0.0, extent.YMin, 0.00001);
		Assert.AreEqual(3.0, extent.XMax, 0.00001);
		Assert.AreEqual(2.0, extent.YMax, 0.00001);
	}

	[Test]
	public void CanEnsureSpatialReference()
	{
		var wgs84 = SpatialReferences.WGS84;

		// geometry can be null (and null shall be returned):

		Assert.IsNull(GeometryUtils.EnsureSpatialReference((Geometry) null, null));
		Assert.IsNull(GeometryUtils.EnsureSpatialReference((Geometry) null, wgs84));

		// make geometry with no spatial reference:

		var polygon = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(0, 0), Pt(0, 1), Pt(1, 1), Pt(1, 0), Pt(0, 0) });
		Assert.IsNull(polygon.SpatialReference);

		// geometry has no spatial reference (just set it):

		var g1 = GeometryUtils.EnsureSpatialReference(polygon, null);
		Assert.IsNull(g1.SpatialReference); // was a no-op

		var g2 = GeometryUtils.EnsureSpatialReference(polygon, wgs84);
		Assert.AreEqual(wgs84, g2.SpatialReference); // sref was set

		// make geometry with spatial reference:

		var ch03 = SpatialReferenceBuilder.CreateSpatialReference(21781);
		var ch95 = SpatialReferenceBuilder.CreateSpatialReference(2056);
		var sref1 = new SpatialReferenceBuilder(ch03) { XYTolerance = 0.0025 }
			.ToSpatialReference();
		var sref2 = new SpatialReferenceBuilder(ch03) { XYTolerance = 0.0125 }
			.ToSpatialReference();

		var polyline = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(600000, 200000), Pt(600100, 200100), Pt(600200, 200000) }, sref1);
		Assert.AreEqual(sref1, polyline.SpatialReference);

		// geometry has spatial reference (must project):

		var g3 = GeometryUtils.EnsureSpatialReference(polyline, null);
		Assert.AreEqual(sref1, g3.SpatialReference); // was a no-op

		var g4 = GeometryUtils.EnsureSpatialReference(polyline, polyline.SpatialReference);
		Assert.AreEqual(sref1, g4.SpatialReference); // was a no-op (already same sref)

		// TODO These fail in test runner with a NullRefEx from GeometryEngine.Project() -- seems PE not fully available?
		//
		//var g5 = GeometryUtils.EnsureSpatialReference(polyline, sref2);
		//Assert.AreEqual(sref2, g5.SpatialReference); // did "project" (change XY tolerance)
		//
		//var g6 = GeometryUtils.EnsureSpatialReference(polyline, ch95);
		//Assert.AreEqual(ch95, g6.SpatialReference); // did project (CH03 to CH95)
	}

	[Test]
	public void CanReplaceSpatialReference()
	{
		// Testing the Pro SDK function:

		var wgs84 = SpatialReferences.WGS84;
		var webMerc = SpatialReferences.WebMercator;

		var p0 = Pt(1.2, 3.4);
		Assert.IsNull(p0.SpatialReference);

		var p1 = (MapPoint) GeometryBuilderEx.ReplaceSpatialReference(p0, wgs84);
		Assert.AreEqual(wgs84, p1.SpatialReference);
		Assert.AreEqual(p0.X, p1.X);
		Assert.AreEqual(p0.Y, p1.Y);

		var p2 = (MapPoint) GeometryBuilderEx.ReplaceSpatialReference(p1, webMerc);
		Assert.AreEqual(webMerc, p2.SpatialReference);
		Assert.AreEqual(p0.X, p2.X);
		Assert.AreEqual(p0.Y, p2.Y);

		var p3 = (MapPoint) GeometryBuilderEx.ReplaceSpatialReference(p2, null);
		Assert.IsNull(p3.SpatialReference);
		Assert.AreEqual(p0.X, p3.X);
		Assert.AreEqual(p0.Y, p3.Y);
	}

	[Test]
	public void CanGetPartAndPartCount()
	{
		var myPoint = Pt(1.2, 3.4);

		Assert.AreEqual(1, GeometryUtils.GetPartCount(myPoint));
		Assert.AreEqual(myPoint, GeometryUtils.GetPart(myPoint, 0));
		Assert.Catch<ArgumentOutOfRangeException>(() => GeometryUtils.GetPart(myPoint, 1));

		var myMultipoint = CreateMultipointXY(1.1, 1.2, 2.1, 2.2, 3.1, 3.2, 4.1, 4.2);
		Assert.AreEqual(4, GeometryUtils.GetPartCount(myMultipoint));
		var p0 = GeometryUtils.GetPart(myMultipoint, 0);
		Assert.IsInstanceOf<MapPoint>(p0);
		Assert.AreEqual(1.1, ((MapPoint) p0).X);
		var p3 = GeometryUtils.GetPart(myMultipoint, 3);
		Assert.IsInstanceOf<MapPoint>(p3);
		Assert.AreEqual(4.2, ((MapPoint) p3).Y);
		Assert.Catch<ArgumentOutOfRangeException>(() => GeometryUtils.GetPart(myMultipoint, 4));

		var myPolygon = CreateMultiPolygon();
		Assert.AreEqual(7, GeometryUtils.GetPartCount(myPolygon));
		Assert.IsInstanceOf<Polygon>(GeometryUtils.GetPart(myPolygon, 0));
		Assert.IsInstanceOf<Polygon>(GeometryUtils.GetPart(myPolygon, 6));
		Assert.Catch<ArgumentOutOfRangeException>(() => GeometryUtils.GetPart(myPolygon, 7));

		Assert.Catch<ArgumentOutOfRangeException>(() => GeometryUtils.GetPart(myPoint, -1));
	}

	[Test]
	public void CanRemoveVerticesPolyline()
	{
		// Cannot remove anything from an empty builder:

		var emptyPolyline = CreatePolylineXY();
		Assert.True(emptyPolyline.IsEmpty);
		var builder = emptyPolyline.ToBuilder();
		Assert.True(builder.IsEmpty);
		Assert.AreEqual(0, builder.PartCount);

		Assert.Throws<InvalidOperationException>(() => GeometryUtils.RemoveVertices(builder, 0, 0));

		var polyline = CreatePolylineXY(0, 0, 1, 1, 2, 2, 3, 3, double.NaN, 4, 4, 5, 5);

		// Remove two vertices along the line:
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 0, 1, 2);
		Assert.AreEqual(2, builder.PartCount);
		Assert.AreEqual(1, builder.GetSegmentCount(0));
		Assert.AreEqual(0.0, builder.GetSegment(0, 0).StartCoordinate.X);
		Assert.AreEqual(3.0, builder.GetSegment(0, 0).EndCoordinate.X);
		Assert.AreEqual(1, builder.GetSegmentCount(1));

		// Remove one vertex at beginning:
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 0, 0, 0); // remove first vertex
		Assert.AreEqual(2, builder.PartCount);
		Assert.AreEqual(2, builder.GetSegmentCount(0));
		Assert.AreEqual(1.0, builder.GetSegment(0, 0).StartCoordinate.X);
		Assert.AreEqual(1, builder.GetSegmentCount(1));

		// Remove three vertices (of four) -> remove part:
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 0, 0, 2);
		Assert.AreEqual(1, builder.PartCount);
		// formerly 2nd part becomes the 1st and only part:
		Assert.AreEqual(4.0, builder.GetSegment(0, 0).StartCoordinate.X);

		// Remove one vertex at end (leaving 3 vertices):
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 0, 3, 3);
		Assert.AreEqual(2, builder.PartCount);
		Assert.AreEqual(2, builder.GetSegmentCount(0));
		Assert.AreEqual(2.0, builder.GetSegment(0, 1).EndCoordinate.X);

		// Remove two vertices at end (leaving 2 vertices):
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 0, 2, 3);
		Assert.AreEqual(2, builder.PartCount);
		Assert.AreEqual(1, builder.GetSegmentCount(0));
		Assert.AreEqual(1.0, builder.GetSegment(0, 0).EndCoordinate.X);

		// Remove all vertices of 2nd part:
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 1, 0, 1);
		Assert.AreEqual(1, builder.PartCount);
		Assert.AreEqual(3.0, builder.GetSegment(0, 2).EndCoordinate.X);

		// Remove vertices such that only ONE vertex remains:
		builder = polyline.ToBuilder();
		GeometryUtils.RemoveVertices(builder, 1, 1, 1);
		Assert.AreEqual(1, builder.PartCount);
		Assert.AreEqual(3.0, builder.GetSegment(0, 2).EndCoordinate.X);

		// Can catch invalid arguments:
		builder = polyline.ToBuilder();
		// part index out of range:
		Assert.Throws<ArgumentOutOfRangeException>(() => GeometryUtils.RemoveVertices(builder, 2, 0, 0));
		// first/last vertex index out of range
		Assert.Throws<ArgumentOutOfRangeException>(() => GeometryUtils.RemoveVertices(builder, 1, 2, 2));
		Assert.Throws<ArgumentOutOfRangeException>(() => GeometryUtils.RemoveVertices(builder, 1, 0, 2));

	}

	#region Creating test geometries

	private static MapPoint Pt(double x, double y)
	{
		return MapPointBuilderEx.CreateMapPoint(x, y);
	}

	private static Multipoint CreateMultipointXY(params double[] coords)
	{
		var builder = new MultipointBuilderEx();
		builder.AddPoints(MakeCoords(coords));
		return builder.ToGeometry();
	}

	private static Polygon CreateUnitSquarePolygon()
	{
		//  1 #####    vertex order:
		//    #####    0,0  0,1  1,1  1,0  cycle
		//  0 #####
		//    0   1

		var builder = new PolygonBuilderEx();

		builder.AddPart(MakeCoords(0, 0, 0, 1, 1, 1, 1, 0, 0, 0));

		return builder.ToGeometry();
	}

	private static Polygon CreateMultiPolygon()
	{
		// 5 . . . . . # # # # # # # . . .
		// 4 . # # # . # . . . # # # . . .
		// 3 . # . # . # . # . # . # . # .
		// 2 . # # # . # . . . # # # . . .
		// 1 . . . . . # # # # # # # . . .
		// 0 . . . . . . . . . . . . . . .
		//   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4

		var builder = new PolygonBuilderEx();

		builder.AddPart(MakeCoords(1, 2,  1, 5,  4, 5,  4, 2,  1, 2)); // outer
		builder.AddPart(MakeCoords(2, 3,  3, 3,  3, 4,  2, 4,  2, 3)); // inner

		builder.AddPart(MakeCoords(5, 1, 5, 6, 12, 6, 12, 1, 5, 1)); // outer
		builder.AddPart(MakeCoords(6, 2, 9, 2, 9, 5, 6, 5, 6, 2)); // inner
		builder.AddPart(MakeCoords(7, 3, 7, 4, 8, 4, 8, 3, 7, 3)); // outer in inner
		builder.AddPart(MakeCoords(10, 3, 11, 3, 11, 4, 10, 4, 10, 3)); // inner

		builder.AddPart(MakeCoords(13, 3,  13, 4,  14, 4,  14, 3,  13, 3)); // outer

		return builder.ToGeometry();
	}

	private static IEnumerable<Coordinate2D> MakeCoords(params double[] coords)
	{
		for (int i = 1; i < coords.Length; i += 2)
		{
			yield return new Coordinate2D(coords[i - 1], coords[i]);
		}
	}

	private static Polyline CreatePolylineXY(params double[] xys)
	{
		// TODO Copied from ShapeSelectionTest --> move to GeometryFactory?!
		var builder = new PolylineBuilderEx();
		builder.HasZ = builder.HasM = builder.HasID = false;

		var coords = new double[4];
		int j = 0; // index into coords
		bool startNewPart = true;

		foreach (double value in xys)
		{
			if (double.IsNaN(value))
			{
				startNewPart = true;
				j = 0; // flush coords
			}
			else
			{
				coords[j++] = value;

				if (j == 4)
				{
					var p0 = new Coordinate2D(coords[0], coords[1]);
					var p1 = new Coordinate2D(coords[2], coords[3]);
					var seg = LineBuilderEx.CreateLineSegment(p0, p1);
					builder.AddSegment(seg, startNewPart);
					startNewPart = false;
					// 2nd coord pair becomes 1st pair
					coords[0] = coords[2];
					coords[1] = coords[3];
					// prepare for another coord pair:
					j = 2;
				}
			}
		}

		return builder.ToGeometry();
	}

	#endregion

	[Test]
	public void Can_get_nearest_vertex()
	{
		var coords = new List<MapPoint>
		             {
			             MapPointBuilderEx.CreateMapPoint(140, 0, SpatialReferences.WebMercator),
			             MapPointBuilderEx.CreateMapPoint(160, 0, SpatialReferences.WebMercator),
			             MapPointBuilderEx.CreateMapPoint(175, 0, SpatialReferences.WebMercator),
			             MapPointBuilderEx.CreateMapPoint(-175, 10, SpatialReferences.WebMercator),
			             MapPointBuilderEx.CreateMapPoint(-145, 10, SpatialReferences.WebMercator),
			             MapPointBuilderEx.CreateMapPoint(-125, 10, SpatialReferences.WebMercator)
		             };

		//var coords = new List<MapPoint>
		//			 {
		//				 MapPointBuilder.CreateMapPoint(140, 0, SpatialReferences.WebMercator),
		//				 MapPointBuilder.CreateMapPoint(160, 0, SpatialReferences.WebMercator),
		//				 MapPointBuilder.CreateMapPoint(175, 0, SpatialReferences.WebMercator),
		//				 MapPointBuilder.CreateMapPoint(185, 10, SpatialReferences.WebMercator),
		//				 MapPointBuilder.CreateMapPoint(215, 10, SpatialReferences.WebMercator),
		//				 MapPointBuilder.CreateMapPoint(225, 10, SpatialReferences.WebMercator)
		//			 };

		Polyline line =
			PolylineBuilderEx.CreatePolyline(coords);

		Polyline dateline =
			PolylineBuilderEx.CreatePolyline(new List<MapPoint>
			                                 {
				                                 MapPointBuilderEx.CreateMapPoint(180, 90, SpatialReferences.WebMercator),
				                                 MapPointBuilderEx.CreateMapPoint(180, -90, SpatialReferences.WebMercator)
			                                 });

		Geometry intersection =
			GeometryEngine.Instance.Intersection(line, dateline,
			                                     GeometryDimensionType.EsriGeometry0Dimension);

		var multipoint = intersection as Multipoint;

		if (multipoint != null)
		{
			for (var i = 0; i < multipoint.PointCount; i++)
			{
				MapPoint point = multipoint.Points[i];
				ProximityResult result =
					GeometryEngine.Instance.NearestVertex(intersection, point);

				MapPoint resultPoint = result.Point;

				Console.WriteLine($"x: {resultPoint.X}");
				Console.WriteLine($"y: {resultPoint.Y}");
			}
		}
	}
}

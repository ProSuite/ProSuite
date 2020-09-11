using ArcGIS.Core.Geometry;
using Envelope = ArcGIS.Core.Geometry.Envelope;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using SpatialReference = ArcGIS.Core.Geometry.SpatialReference;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeometryUtils
	{
		public static MapPoint CreatePoint(double x, double y, SpatialReference sref = null)
		{
			return MapPointBuilder.CreateMapPoint(x, y, sref);
		}

		public static Envelope CreateEnvelope(
			double x0, double y0, double x1, double y1,
		    SpatialReference sref = null)
		{
			var p0 = new Coordinate2D(x0, y0);
			var p1 = new Coordinate2D(x1, y1);
			return EnvelopeBuilder.CreateEnvelope(p0, p1, sref);
		}

		public static Polygon CreatePolygon(Envelope envelope, SpatialReference sref = null)
		{
			return PolygonBuilder.CreatePolygon(envelope, sref);
		}

		public static Polygon CreateBezierCircle(double radius = 1, MapPoint center = null)
		{
			// Approximate a full circle with Bezier curves. (We could use
			// EllipticArc segments, of course, but in the context of markers,
			// Béziers are more appropriate.) It is customary to use four cubic
			// Bézier curves, one for each quadrant. The control points must be
			// on tangential lines to ensure continuity, their distance from
			// the axes is chosen for minimal deviation from a true circle.
			// See: https://spencermortensen.com/articles/bezier-circle/

			const double magic = 0.551915; // for best circle approximation

			double cx = 0, cy = 0;

			if (center != null)
			{
				cx = center.X;
				cy = center.Y;
			}

			var p0 = new Coordinate2D(radius, 0).Shifted(cx, cy);
			var p01 = new Coordinate2D(radius, magic * radius).Shifted(cx, cy);
			var p10 = new Coordinate2D(magic * radius, radius).Shifted(cx, cy);
			var p1 = new Coordinate2D(0, radius).Shifted(cx, cy);
			var p12 = new Coordinate2D(-magic * radius, radius).Shifted(cx, cy);
			var p21 = new Coordinate2D(-radius, magic * radius).Shifted(cx, cy);
			var p2 = new Coordinate2D(-radius, 0).Shifted(cx, cy);
			var p23 = new Coordinate2D(-radius, -magic * radius).Shifted(cx, cy);
			var p32 = new Coordinate2D(-magic * radius, -radius).Shifted(cx, cy);
			var p3 = new Coordinate2D(0, -radius).Shifted(cx, cy);
			var p30 = new Coordinate2D(magic * radius, -radius).Shifted(cx, cy);
			var p03 = new Coordinate2D(radius, -magic * radius).Shifted(cx, cy);

			// segments for each quadrant
			var q1 = CubicBezierBuilder.CreateCubicBezierSegment(p0, p01, p10, p1);
			var q2 = CubicBezierBuilder.CreateCubicBezierSegment(p1, p12, p21, p2);
			var q3 = CubicBezierBuilder.CreateCubicBezierSegment(p2, p23, p32, p3);
			var q4 = CubicBezierBuilder.CreateCubicBezierSegment(p3, p30, p03, p0);
			var segments = new[] {q1, q2, q3, q4};

			return PolygonBuilder.CreatePolygon(segments);
		}

		public static Coordinate2D Shifted(this Coordinate2D point, double dx, double dy)
		{
			return new Coordinate2D(point.X + dx, point.Y + dy);
		}

		public static SpatialReference CreateSpatialReference(int srid)
		{
			return SpatialReferenceBuilder.CreateSpatialReference(srid);
		}

		public static int GetPointCount(Geometry geometry)
		{
			if (geometry == null) return 0;

			return geometry.PointCount;
		}
	}
}

using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeometryFactory
	{
		[NotNull]
		public static T Clone<T>([NotNull] T prototype) where T : Geometry
		{
			return (T) prototype.Clone();
		}

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
			if (envelope == null) return null;
			return PolygonBuilder.CreatePolygon(envelope, sref);
		}

		[NotNull]
		public static Polyline CreatePolyline([NotNull] EnvelopeXY envelope,
		                                      [CanBeNull] SpatialReference sref = null)
		{
			return PolylineBuilder.CreatePolyline(To2DCoordinates(envelope), sref);
		}

		[NotNull]
		public static Polygon CreatePolygon([NotNull] EnvelopeXY envelope,
		                                    [CanBeNull] SpatialReference sref = null)
		{
			return PolygonBuilder.CreatePolygon(To2DCoordinates(envelope), sref);
		}

		public static IEnumerable<Coordinate2D> To2DCoordinates([NotNull] EnvelopeXY envelope)
		{
			yield return new Coordinate2D(envelope.XMin, envelope.YMin);
			yield return new Coordinate2D(envelope.XMin, envelope.YMax);
			yield return new Coordinate2D(envelope.XMax, envelope.YMax);
			yield return new Coordinate2D(envelope.XMax, envelope.YMin);
			yield return new Coordinate2D(envelope.XMin, envelope.YMin);
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

		public static SpatialReference CreateSpatialReference(int srid)
		{
			return SpatialReferenceBuilder.CreateSpatialReference(srid);
		}
	}
}

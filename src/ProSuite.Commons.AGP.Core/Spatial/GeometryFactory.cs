using System;
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

		/// <summary>
		/// Return the given <paramref name="builder"/> with HasZ, HasM, HasID,
		/// and SpatialReference set from the given <paramref name="template"/>.
		/// </summary>
		public static T Configure<T>(this T builder, Geometry template) where T : GeometryBuilderEx
		{
			if (builder is null) return null;
			if (template is null) return builder;

			builder.HasZ = template.HasZ;
			builder.HasM = template.HasM;
			builder.HasID = template.HasID;
			builder.SpatialReference = template.SpatialReference;

			return builder;
		}

		/// <summary>
		/// Return a new segment builder initialized from the given segment.
		/// </summary>
		public static SegmentBuilderEx ToBuilder(this Segment segment)
		{
			if (segment is null)
				throw new ArgumentNullException(nameof(segment));

			switch (segment)
			{
				case LineSegment line: return new LineBuilderEx(line);
				case EllipticArcSegment arc: return new EllipticArcBuilderEx(arc);
				case CubicBezierSegment cubic: return new CubicBezierBuilderEx(cubic);
			}

			throw new NotSupportedException($"Unknown segment type: {segment.GetType().Name}");
		}

		/// <summary>
		/// Return a new multipart builder initialized from the given
		/// multipart geometry (Polyline or Polygon).
		/// </summary>
		public static MultipartBuilderEx ToBuilder(this Multipart multipart)
		{
			if (multipart is null)
				throw new ArgumentNullException(nameof(multipart));

			switch (multipart)
			{
				case Polyline polyline: return new PolylineBuilderEx(polyline);
				case Polygon polygon: return new PolygonBuilderEx(polygon);
			}

			throw new NotSupportedException($"Unknown geometry type: {multipart.GetType().Name}");
		}

		public static GeometryBuilderEx ToBuilder(this Geometry geometry)
		{
			if (geometry is null)
				throw new ArgumentNullException(nameof(geometry));

			switch (geometry)
			{
				case Envelope env: return new EnvelopeBuilderEx(env);
				case MapPoint point: return new MapPointBuilderEx(point);
				case Multipoint multipoint: return new MultipointBuilderEx(multipoint);
				case Polyline polyline: return new PolylineBuilderEx(polyline);
				case Polygon polygon: return new PolygonBuilderEx(polygon);
				case Multipatch multipatch: return new MultipatchBuilderEx(multipatch);
				case GeometryBag bag: return new GeometryBagBuilderEx(bag);
			}

			throw new NotSupportedException(
				$"Unknown geometry type: {geometry.GetType().Name}");
		}

		public static MapPoint CreatePoint(double x, double y, SpatialReference sref = null)
		{
			return MapPointBuilderEx.CreateMapPoint(x, y, sref);
		}

		public static MapPoint CreatePoint(double x, double y, double z, SpatialReference sref = null)
		{
			return MapPointBuilderEx.CreateMapPoint(x, y, z, sref);
		}

		public static Envelope CreateEnvelope(
			double x0, double y0, double x1, double y1,
			SpatialReference sref = null)
		{
			var p0 = new Coordinate2D(x0, y0);
			var p1 = new Coordinate2D(x1, y1);
			return EnvelopeBuilderEx.CreateEnvelope(p0, p1, sref);
		}

		public static Envelope CreateEnvelope(
			MapPoint center, double width, double height,
			SpatialReference sref = null)
		{
			double halfWidth = width / 2;
			double halfHeight = height / 2;

			var p0 = new Coordinate2D(center.X - halfWidth, center.Y - halfHeight);
			var p1 = new Coordinate2D(center.X + halfWidth, center.Y + halfHeight);

			return EnvelopeBuilderEx.CreateEnvelope(p0, p1, sref);
		}

		public static Envelope CreateEnvelope(
			Envelope envelope, double expansionFactor)
		{
			Envelope result = (Envelope) envelope.Clone();

			const bool asRatio = true;

			return result.Expand(expansionFactor, expansionFactor, asRatio);
		}

		public static Envelope CreateEmptyEnvelope(Geometry template = null)
		{
			var builder = Configure(new EnvelopeBuilderEx(), template);

			return builder.ToGeometry();
		}

		public static Polygon CreatePolygon(Envelope envelope, SpatialReference sref = null)
		{
			if (envelope == null) return null;
			return PolygonBuilderEx.CreatePolygon(envelope, sref);
		}

		[NotNull]
		public static Polyline CreatePolyline([NotNull] EnvelopeXY envelope,
		                                      [CanBeNull] SpatialReference sref = null)
		{
			return PolylineBuilderEx.CreatePolyline(To2DCoordinates(envelope), sref);
		}

		[NotNull]
		public static Polyline CreatePolyline([NotNull] MapPoint startPoint,
		                                      [NotNull] MapPoint endPoint,
		                                      [CanBeNull] SpatialReference sref = null)
		{
			return PolylineBuilderEx.CreatePolyline(new[] { startPoint, endPoint },
			                                        AttributeFlags.None, sref);
		}

		[NotNull]
		public static Polygon CreatePolygon([NotNull] EnvelopeXY envelope,
		                                    [CanBeNull] SpatialReference sref = null)
		{
			return PolygonBuilderEx.CreatePolygon(To2DCoordinates(envelope), sref);
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
			// Approximate a full circle with Bézier curves. (We could use
			// EllipticArc segments, of course, but in the context of markers,
			// Bézier curves are more appropriate.) It is customary to use four
			// cubic Bézier curves, one for each quadrant. The control points must
			// be on tangential lines to ensure continuity, their distance from
			// the axes is chosen for minimal deviation from a true circle.
			// See: https://spencermortensen.com/articles/bezier-circle/

			const double magic = 0.551915; // for good circle approximation

			double cx = 0, cy = 0;

			if (center != null)
			{
				cx = center.X;
				cy = center.Y;
			}

			// Outer rings are clockwise (with Esri):
			var p0 = new Coordinate2D(radius, 0).Shifted(cx, cy);
			var p01 = new Coordinate2D(radius, -radius * magic).Shifted(cx, cy);
			var p10 = new Coordinate2D(radius * magic, -radius).Shifted(cx, cy);
			var p1 = new Coordinate2D(0, -radius).Shifted(cx, cy);
			var p12 = new Coordinate2D(-radius * magic, -radius).Shifted(cx, cy);
			var p21 = new Coordinate2D(-radius, -radius * magic).Shifted(cx, cy);
			var p2 = new Coordinate2D(-radius, 0).Shifted(cx, cy);
			var p23 = new Coordinate2D(-radius, radius * magic).Shifted(cx, cy);
			var p32 = new Coordinate2D(-radius * magic, radius).Shifted(cx, cy);
			var p3 = new Coordinate2D(0, radius).Shifted(cx, cy);
			var p30 = new Coordinate2D(radius * magic, radius).Shifted(cx, cy);
			var p03 = new Coordinate2D(radius, radius * magic).Shifted(cx, cy);

			// segments for each quadrant
			var q1 = CubicBezierBuilderEx.CreateCubicBezierSegment(p0, p01, p10, p1);
			var q2 = CubicBezierBuilderEx.CreateCubicBezierSegment(p1, p12, p21, p2);
			var q3 = CubicBezierBuilderEx.CreateCubicBezierSegment(p2, p23, p32, p3);
			var q4 = CubicBezierBuilderEx.CreateCubicBezierSegment(p3, p30, p03, p0);
			var segments = new[] { q1, q2, q3, q4 };

			return PolygonBuilderEx.CreatePolygon(segments, AttributeFlags.None);
		}

		public static SpatialReference CreateSpatialReference(int srid)
		{
			return SpatialReferenceBuilder.CreateSpatialReference(srid);
		}
	}
}

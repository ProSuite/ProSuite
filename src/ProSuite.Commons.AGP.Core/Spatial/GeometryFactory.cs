using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Spatial;

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

	public static PolylineBuilderEx ToBuilder(this Polyline polyline)
	{
		if (polyline is null)
			throw new ArgumentNullException(nameof(polyline));

		return new PolylineBuilderEx(polyline);
	}

	public static PolygonBuilderEx ToBuilder(this Polygon polygon)
	{
		if (polygon is null)
			throw new ArgumentNullException(nameof(polygon));

		return new PolygonBuilderEx(polygon);
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

		double xMin = center.X - halfWidth;
		double yMin = center.Y - halfHeight;
		double xMax = center.X + halfWidth;
		double yMax = center.Y + halfHeight;

		if (center.HasZ)
		{
			var ll = new Coordinate3D(xMin, yMin, center.Z);
			var ur = new Coordinate3D(xMax, yMax, center.Z);

			return EnvelopeBuilderEx.CreateEnvelope(ll, ur, sref);
		}

		var p0 = new Coordinate2D(xMin, yMin);
		var p1 = new Coordinate2D(xMax, yMax);

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

	/// <summary>
	/// Creates a (simple!) polygon from the specified multipatch geometry
	/// </summary>
	/// <param name="multipatch"></param>
	/// <param name="spatialReference"></param>
	/// <returns></returns>
	[NotNull]
	public static Polygon CreatePolygon([NotNull] Multipatch multipatch,
	                                    [CanBeNull] SpatialReference spatialReference)

	{
		Assert.ArgumentNotNull(multipatch, nameof(multipatch));

		var boundary = (Polyline) GeometryEngine.Instance.Boundary(multipatch);

		if (spatialReference == null)
		{
			spatialReference = multipatch.SpatialReference;
		}

		Polygon footprint =
			PolygonBuilderEx.CreatePolygon(boundary, spatialReference);

		const bool allowReorder = true;
		footprint = GeometryUtils.Simplify(footprint, allowReorder);

		return footprint;
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

	public static Multipoint CreateMultipoint(IEnumerable<MapPoint> points)
	{
		return MultipointBuilderEx.CreateMultipoint(points);
	}

	public static Multipoint CreateEmptyMultipoint(Geometry geometry)
	{
		return new MultipointBuilderEx().Configure(geometry).ToGeometry();
	}

	public static Polyline CreatePath(MapPoint sourcePoint, MapPoint toTargetPoint)
	{
		PolylineBuilderEx result = new PolylineBuilderEx();
		SegmentBuilderEx segmentBuilder =
			new LineBuilderEx(sourcePoint, toTargetPoint, sourcePoint.SpatialReference);
		result.AddSegment(segmentBuilder, false);
		return result.ToGeometry();
	}

	/// <summary>
	/// Create a multipoint from the given array of alternating X and Y
	/// coordinate values (the last value is ignored if the array has
	/// an odd number of values).
	/// </summary>
	public static Multipoint CreateMultipointXY(params double[] xys)
	{
		var builder = new MultipointBuilderEx();
		builder.HasZ = builder.HasM = builder.HasID = false;
		for (int i = 0; i < xys.Length - 1; i += 2)
		{
			builder.AddPoint(new Coordinate2D(xys[i], xys[i + 1]));
		}

		return builder.ToGeometry();
	}

	/// <summary>
	/// Create a polyline from the given array of alternating X and Y
	/// coordinate values. A value of NaN in the array starts a new part.
	/// </summary>
	public static Polyline CreatePolylineXY(params double[] xys)
	{
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

	/// <summary>
	/// Creates a polyline.
	/// </summary>
	/// <param name="baseGeometry">The base geometry.</param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline([CanBeNull] Geometry baseGeometry)
	{
		SpatialReference spatialReference = baseGeometry?.SpatialReference;

		return CreatePolyline(baseGeometry, spatialReference);
	}

	/// <summary>
	/// Creates a polyline.
	/// </summary>
	/// <param name="spatialReference">The spatial reference.</param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline(
		[CanBeNull] SpatialReference spatialReference)
	{
		return CreatePolyline((Geometry) null, spatialReference);
	}

	/// <summary>
	/// Creates a polyline from a polygon.
	/// </summary>
	/// <param name="fromPolygon"></param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline([NotNull] Polygon fromPolygon)
	{
		return CreatePolyline(fromPolygon, fromPolygon.SpatialReference,
		                      null, null);
	}

	/// <summary>
	/// Creates a polyline from a polygon. Make sure the input polygon is simple.
	/// </summary>
	/// <param name="fromPolygon"></param>
	/// <param name="spatialReference"></param>
	/// <param name="makeZAware"></param>
	/// <param name="makeMAware"></param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline(
		[NotNull] Polygon fromPolygon,
		[CanBeNull] SpatialReference spatialReference,
		[CanBeNull] bool? makeZAware,
		[CanBeNull] bool? makeMAware)
	{
		Assert.ArgumentNotNull(fromPolygon, nameof(fromPolygon));

		// TODO: test with non-simple geometries! Catch block with simple test?
		Polyline boundary = (Polyline) GeometryEngine.Instance.Boundary(fromPolygon);

		GeometryUtils.Simplify(boundary);

		boundary = GeometryUtils.EnsureSpatialReference(boundary, spatialReference);

		boundary = (Polyline) EnsureZM(boundary, fromPolygon, makeZAware, makeMAware);

		return boundary;
	}

	/// <summary>
	/// Creates a polyline from a polyline applying the specified spatial reference
	/// and Z/M attributes.
	/// </summary>
	/// <param name="fromPolyline"></param>
	/// <param name="spatialReference"></param>
	/// <param name="makeZAware"></param>
	/// <param name="makeMAware"></param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline(
		[NotNull] Polyline fromPolyline,
		[CanBeNull] SpatialReference spatialReference,
		[CanBeNull] bool? makeZAware,
		[CanBeNull] bool? makeMAware)
	{
		Polyline clonedPolyline = Clone(fromPolyline);

		clonedPolyline = GeometryUtils.EnsureSpatialReference(clonedPolyline, spatialReference);

		clonedPolyline = (Polyline) EnsureZM(clonedPolyline, fromPolyline, makeZAware, makeMAware);

		return clonedPolyline;
	}

	/// <summary>
	/// Creates an empty polyline geometry.
	/// </summary>
	/// <param name="spatialReference"></param>
	/// <param name="makeZAware"></param>
	/// <param name="makeMAware"></param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline(
		[CanBeNull] SpatialReference spatialReference,
		[CanBeNull] bool? makeZAware,
		[CanBeNull] bool? makeMAware)
	{
		PolylineBuilderEx polylineBuilder = new PolylineBuilderEx(spatialReference);
		polylineBuilder.HasZ = makeZAware ?? false;
		polylineBuilder.HasM = makeMAware ?? false;

		return polylineBuilder.ToGeometry();
	}

	/// <summary>
	/// Creates a polyline.
	/// </summary>
	/// <param name="baseGeometry">The base geometry.</param>
	/// <param name="spatialReference">The spatial ref.</param>
	/// <param name="makeZAware">The make Z aware.</param>
	/// <param name="makeMAware">The make M aware.</param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline([CanBeNull] Geometry baseGeometry,
	                                      [CanBeNull] SpatialReference spatialReference,
	                                      [CanBeNull] bool? makeZAware = null,
	                                      [CanBeNull] bool? makeMAware = null)
	{
		if (baseGeometry is Polygon polygon)
		{
			// TODO: respect spatial ref and ZM-awareness
			return CreatePolyline(polygon);
		}

		if (baseGeometry is Polyline polyline)
		{
			return CreatePolyline(polyline,
			                      spatialReference, makeZAware, makeMAware);
		}

		if (baseGeometry is Multipart segment)
		{
			ICollection<Segment> allSegments = new List<Segment>();
			segment.GetAllSegments(ref allSegments);

			if (allSegments.Count > 0)
			{
				return CreatePolyline(allSegments, spatialReference, makeZAware, makeMAware, false);
			}

			throw new ArgumentException(
				@"Geometry is not valid to create polyline.", nameof(baseGeometry));
		}

		if (baseGeometry != null)
		{
			throw new NotImplementedException(
				@"not all geometry types to create polyline implemented: " + nameof(baseGeometry));
		}

		return CreatePolyline(spatialReference, makeZAware, makeMAware);
	}

	/// <summary>
	/// Creates a polyline.
	/// </summary>
	/// <param name="segmentCollection"></param>
	/// <param name="spatialReference"></param>
	/// <param name="makeZAware"></param>
	/// <param name="makeMAware"></param>
	/// <param name="doNotCloneInput"></param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline(
		[NotNull] ICollection<Segment> segmentCollection,
		[CanBeNull] SpatialReference spatialReference,
		bool? makeZAware, bool? makeMAware,
		bool doNotCloneInput)
	{
		Polyline polyline =
			PolylineBuilderEx.CreatePolyline(segmentCollection, spatialReference);

		polyline = (Polyline) EnsureZM(polyline, polyline, makeZAware, makeMAware);

		return polyline;
	}

	/// <summary>
	/// Creates a polyline from a collection of paths or rings.
	/// </summary>
	/// <param name="pathCollection"></param>
	/// <param name="spatialReference"></param>
	/// <param name="makeZAware"></param>
	/// <param name="makeMAware"></param>
	/// <returns></returns>
	[NotNull]
	public static Polyline CreatePolyline(
		[NotNull] ICollection<Polyline> pathCollection,
		[CanBeNull] SpatialReference spatialReference = null,
		[CanBeNull] bool? makeZAware = null,
		[CanBeNull] bool? makeMAware = null)
	{
		Assert.ArgumentNotNull(pathCollection, nameof(pathCollection));
		Assert.ArgumentCondition(pathCollection.Count > 0,
		                         "pathCollection must contain at least 1 element");

		Polyline result = null;
		PolylineBuilderEx polylineBuilder = null;

		object missing = Type.Missing;

		foreach (Polyline path in pathCollection)
		{
			if (spatialReference == null)
			{
				spatialReference = path.SpatialReference;
			}

			if (makeZAware == null)
			{
				makeZAware = path.HasZ;
			}

			if (makeMAware == null)
			{
				makeMAware = path.HasM;
			}

			Assert.True(path.GeometryType == GeometryType.Polygon, "path is Polygon");
			Polyline pathToAdd = path;

			if (polylineBuilder == null)
			{
				result = CreatePolyline(pathToAdd, spatialReference, makeZAware, makeMAware);
				polylineBuilder = result.ToBuilder();
			}
			else
			{
				polylineBuilder.AddSegments(pathToAdd.Parts[0]);
			}
		}

		result = polylineBuilder.ToGeometry();

		return Assert.NotNull(result);
	}

	[NotNull]
	public static Polyline CreatePolyline(double x1, double y1,
	                                      double x2, double y2)
	{
		var coord1 = new Coordinate2D(x1, y1);
		var coord2 = new Coordinate2D(x2, y2);

		var polyline = PolylineBuilderEx.CreatePolyline(new List<Coordinate2D> { coord1, coord2 });
		return GeometryUtils.Simplify(polyline);
	}

	private static Geometry EnsureZM([NotNull] Geometry newGeometry,
	                                 [NotNull] Geometry baseGeometry,
	                                 bool? makeZAware,
	                                 bool? makeMAware)
	{
		GeometryBuilderEx geometryBuilder = newGeometry.ToBuilder();
		geometryBuilder.HasZ = makeZAware ?? baseGeometry.HasZ;
		geometryBuilder.HasM = makeMAware ?? baseGeometry.HasM;

		return geometryBuilder.ToGeometry();
	}

	/// <summary>
	/// Create a polygon from the given array of alternating X and Y
	/// coordinate values. A value of NaN in the array starts a new part.
	/// The last coordinate pair of each ring should equal the first pair.
	/// </summary>
	public static Polygon CreatePolygonXY(params double[] xys)
	{
		var builder = new PolygonBuilderEx();
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

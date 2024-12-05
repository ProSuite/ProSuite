using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AGP.Core.Spatial;

/// <summary>
/// A control point is a vertex with a non-zero ID value.
/// They may be used with certain geometric effects to
/// create nice cartography. Due to the immutable geometries
/// in the Pro SDK, the code here is very different from our
/// ArcObjects-based ControlPointUtils.
/// </summary>
public static class ControlPointUtils
{
	/// <summary>
	/// Get the point ID at the addressed vertex of the given shape.
	/// For a point geometry, pass zero as part and vertex index.
	/// For a multipoint, pass the point index as both part and vertex index.
	/// </summary>
	/// <returns>The point ID or zero of there is no point ID</returns>
	public static int GetPointID(Geometry shape, int partIndex, int vertexIndex)
	{
		const int noID = 0;

		if (shape is null || shape.IsEmpty)
		{
			return noID;
		}

		if (shape is MapPoint mapPoint)
		{
			if (partIndex != 0)
				throw new ArgumentOutOfRangeException(nameof(partIndex));
			if (vertexIndex != 0)
				throw new ArgumentOutOfRangeException(nameof(vertexIndex));

			return mapPoint.HasID ? mapPoint.ID : noID;
		}

		if (shape is Multipoint multipoint)
		{
			int pointIndex = GeometryUtils.GetMultipointIndex(partIndex, vertexIndex);
			if (pointIndex < 0 || pointIndex >= multipoint.PointCount)
				throw new ArgumentOutOfRangeException(
					"point index out of range for multipoint shape", (Exception)null);

			var point = multipoint.Points[vertexIndex];
			return multipoint.HasID ? point.ID : noID;
		}

		if (shape is Multipart multipart)
		{
			// Here GetGlobalVertexIndex validates the index arguments:
			var globalIndex = GeometryUtils.GetGlobalVertexIndex(multipart, partIndex, vertexIndex);
			var point = multipart.Points[globalIndex];
			return multipart.HasID ? point.ID : noID;
		}

		if (shape is Multipatch)
		{
			throw new NotImplementedException();
		}

		if (shape is Envelope)
		{
			return noID; // has no vertices and thus no control points
		}

		throw new NotSupportedException($"Geometry type {shape.GetType().Name} is not supported");
	}

	/// <summary>
	/// Set the point ID at the addressed vertex to the given value.
	/// For a point geometry, pass zero as part and vertex index.
	/// For a multipoint, pass the point index as both part and vertex index.
	/// </summary>
	/// <returns>A new geometry instance with the point ID set</returns>
	public static Geometry SetPointID(
		int value, Geometry shape, int partIndex, int vertexIndex)
	{
		if (shape is null || shape.IsEmpty)
		{
			return shape;
		}

		if (shape is MapPoint mapPoint)
		{
			if (partIndex != 0)
				throw new ArgumentOutOfRangeException(nameof(partIndex));
			if (vertexIndex != 0)
				throw new ArgumentOutOfRangeException(nameof(vertexIndex));

			return SetPointID(mapPoint, value);
		}

		if (shape is Multipoint multipoint)
		{
			int pointIndex = GeometryUtils.GetMultipointIndex(partIndex, vertexIndex);
			if (pointIndex < 0 || pointIndex >= multipoint.PointCount)
				throw new ArgumentOutOfRangeException(
					"point index out of range for multipoint shape", (Exception)null);

			var builder = new MultipointBuilderEx(multipoint);
			builder.HasID = true;
			builder.IDs[vertexIndex] = value;
			return builder.ToGeometry();
		}

		if (shape is Polyline polyline)
		{
			// Index arguments are validated by SetPointID()
			var builder = new PolylineBuilderEx(polyline);
			builder.SetPointID(partIndex, vertexIndex, value);
			return builder.ToGeometry();
		}

		if (shape is Polygon polygon)
		{
			// Index arguments are validated by SetPointID()
			var builder = new PolygonBuilderEx(polygon);
			builder.SetPointID(partIndex, vertexIndex, value);
			return builder.ToGeometry();
		}

		if (shape is Multipatch)
		{
			throw new NotImplementedException();
		}

		if (shape is Envelope)
		{
			throw new NotSupportedException("Cannot set control points on an Envelope");
		}

		throw new NotSupportedException($"Geometry type {shape.GetType().Name} is not supported");
	}

	public static void SetPointID(this MultipartBuilderEx builder,
	                              int partIndex, int pointIndex, int pointID)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));
		if (!(0 <= partIndex && partIndex < builder.PartCount))
			throw new ArgumentOutOfRangeException(nameof(partIndex));

		// assume point i is between segments i-1 and i
		var segmentCount = builder.GetSegmentCount(partIndex);
		if (!(0 <= pointIndex && pointIndex <= segmentCount))
			throw new ArgumentOutOfRangeException(nameof(pointIndex));

		if (pointID != 0)
		{
			builder.HasID = true;
		}

		switch (builder)
		{
			case PolylineBuilderEx:
				// update StartPoint on segment i (if exists)
				// update EndPoint on segment i-1 (if exists)
				if (pointIndex < segmentCount)
					UpdateStartPoint(builder, partIndex, pointIndex, pointID);
				pointIndex -= 1;
				if (pointIndex >= 0)
					UpdateEndPoint(builder, partIndex, pointIndex, pointID);
				break;

			case PolygonBuilderEx:
				// update StartPoint of segment i (mod N)
				// update EndPoint of segment (i-1) (mod N)
				pointIndex %= segmentCount;
				UpdateStartPoint(builder, partIndex, pointIndex, pointID);
				pointIndex -= 1;
				if (pointIndex < 0) pointIndex += segmentCount;
				UpdateEndPoint(builder, partIndex, pointIndex, pointID);
				break;
			default:
				throw new AssertionException(
					"multipart builder is neither polygon nor polyline builder");
		}
	}

	private static void UpdateStartPoint(MultipartBuilderEx builder, int k, int i, int value)
	{
		var segment = builder.GetSegment(k, i);
		var updated = SetPointID(segment, value, null);
		builder.ReplaceSegment(k, i, updated);
	}

	private static void UpdateEndPoint(MultipartBuilderEx builder, int k, int i, int value)
	{
		var segment = builder.GetSegment(k, i);
		var updated = SetPointID(segment, null, value);
		builder.ReplaceSegment(k, i, updated);
	}

	public static T SetPointID<T>(
		T segment, int? startPointID, int? endPointID) where T : Segment
	{
		if (segment is null) return null;

		switch (segment)
		{
			case LineSegment line:
				return (T) (Segment) SetPointID(line, startPointID, endPointID);

			case CubicBezierSegment bezier:
				return (T) (Segment) SetPointID(bezier, startPointID, endPointID);

			case EllipticArcSegment arc:
				return (T) (Segment) SetPointID(arc, startPointID, endPointID);

			default:
				throw new ArgumentException("Unknown segment type", nameof(segment));
		}
	}

	public static LineSegment SetPointID(
		LineSegment segment, int? startPointID, int? endPointID)
	{
		if (segment is null) return null;

		if (!startPointID.HasValue && !endPointID.HasValue)
		{
			return segment; // nothing to update
		}

		var builder = new LineBuilderEx(segment);

		if (startPointID.HasValue)
		{
			builder.StartPoint = SetPointID(segment.StartPoint, startPointID.Value);
		}

		if (endPointID.HasValue)
		{
			builder.EndPoint = SetPointID(segment.EndPoint, endPointID.Value);
		}

		return builder.ToSegment();
	}

	public static CubicBezierSegment SetPointID(
		CubicBezierSegment segment, int? startPointID, int? endPointID)
	{
		if (segment is null) return null;

		if (!startPointID.HasValue && !endPointID.HasValue)
		{
			return segment; // nothing to update
		}

		var builder = new CubicBezierBuilderEx(segment);

		if (startPointID.HasValue)
		{
			builder.StartPoint = SetPointID(segment.StartPoint, startPointID.Value);
		}

		if (endPointID.HasValue)
		{
			builder.EndPoint = SetPointID(segment.EndPoint, endPointID.Value);
		}

		return builder.ToSegment();
	}

	public static EllipticArcSegment SetPointID(
		EllipticArcSegment segment, int? startPointID, int? endPointID)
	{
		if (segment is null) return null;

		if (!startPointID.HasValue && !endPointID.HasValue)
		{
			return segment; // nothing to update
		}

		try
		{
			// The obvious approach here fails with "The point is not on
			// the arc", probably due to floating-point round-off troubles,
			// in many cases (it seems to work for full circles/ellipses):

			var builder = new EllipticArcBuilderEx(segment);

			if (startPointID.HasValue)
			{
				//bool check = IsPointOnCircle(builder.StartPoint, segment);

				builder.StartPoint = SetPointID(builder.StartPoint, startPointID.Value);
			}

			if (endPointID.HasValue)
			{

				//bool check = IsPointOnCircle(builder.StartPoint, segment);

				builder.EndPoint = SetPointID(builder.EndPoint, endPointID.Value);
			}

			return builder.ToSegment();
		}
		catch (Exception ignore)
		{
			// Alternative approach: recreate segment using an
			// appropriate utility method on the builder class:

			EllipticArcSegment updated;

			var startPoint = startPointID.HasValue
				                 ? SetPointID(segment.StartPoint, startPointID.Value)
				                 : segment.StartPoint;
			var endPoint = endPointID.HasValue
				               ? SetPointID(segment.EndPoint, endPointID.Value)
				               : segment.EndPoint;

			var orientation = segment.IsCounterClockwise
				                  ? ArcOrientation.ArcCounterClockwise
				                  : ArcOrientation.ArcClockwise;

			var sref = segment.SpatialReference;

			if (segment.IsCircular)
			{
				// seems to even work on full circles
				updated = EllipticArcBuilderEx.CreateCircularArc(
					startPoint, endPoint, segment.CenterPoint, orientation, sref);
			}
			else
			{
				var minor = segment.IsMinor
					            ? MinorOrMajor.Minor
					            : MinorOrMajor.Major;

				// Note: fails if startPoint==endPoint (full ellipse), but then the "obvious approach" seems to work
				updated = EllipticArcBuilderEx.CreateEllipticArcSegment(
					startPoint, endPoint, segment.SemiMajorAxis, segment.MinorMajorRatio,
					segment.RotationAngle, minor, orientation, sref);
			}

			return updated;
		}
	}

	private static bool IsPointOnCircle(MapPoint point, EllipticArcSegment arc)
	{
		// The assignment EllipticArcBuilderEx.StartPoint = point calls
		// code like the one here and throws on false (point not on arc).
		// With my test data (LV95 coords) this is false even when the XY
		// coords are exactly the same.

		double x1 = point.X;
		double y1 = point.Y;
		double x2 = arc.CenterPoint.X;
		double y2 = arc.CenterPoint.Y;
		double radius = arc.SemiMajorAxis;
		double dist = Math.Abs((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) - radius * radius);
		return dist < 1E-12;
	}

	public static MapPoint SetPointID(MapPoint point, int id)
	{
		if (point is null) return null;

		var builder = new MapPointBuilderEx(point);
		// Do NOT use object initializer:
		// we want control over assignment ordering
		builder.HasID = true;
		builder.ID = id;
		builder.HasID = id != 0;

		return builder.ToGeometry();
	}

	/// <summary>
	/// Reset control points by setting the vertex ID to zero.
	/// If the given <paramref name="value"/> is non-negative,
	/// reset only control points with this ID value. If the
	/// given <paramref name="perimeter"/> is non-null, reset
	/// only control points within this perimeter. Return the
	/// updated geometry and the number of modified vertices.
	/// </summary>
	public static Polygon ResetControlPoints(
		Polygon shape, out int count, int value = -1, Polygon perimeter = null)
	{
		count = 0;
		if (shape is null) return null;
		if (!shape.HasID) return shape;

		var builder = new PolygonBuilderEx(shape);
		count = ResetControlPoints(builder, value, perimeter);
		return builder.ToGeometry();
	}

	/// <summary>
	/// See <see cref="ResetControlPoints(Polygon,out int,int,Polygon)"/>
	/// </summary>
	public static Polyline ResetControlPoints(
		Polyline shape, out int count, int value = -1, Polygon perimeter = null)
	{
		count = 0;
		if (shape is null) return null;
		if (! shape.HasID) return shape;

		var builder = new PolylineBuilderEx(shape);
		count = ResetControlPoints(builder, value, perimeter);
		return builder.ToGeometry();
	}

	public static int ResetControlPoints(
		MultipartBuilderEx builder, int value = -1, Polygon perimeter = null)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));

		int count = 0;

		int partCount = builder.PartCount;
		for (int k = 0; k < partCount; k++)
		{
			int pointCount = builder.GetPointCount(k);
			for (int j = 0; j < pointCount; j++)
			{
				var point = builder.GetPoint(k, j);
				if ((value < 0 || point.ID == value) && Within(point, perimeter))
				{
					if (point.ID != 0) count += 1;
					point = SetPointID(point, 0);
					builder.SetPoint(k, j, point);
				}
			}
		}

		return count;
	}

	/// <summary>
	/// Reset control point pairs of the given <paramref name="value"/>
	/// to zero. If a <paramref name="perimeter"/> is given, only reset
	/// pairs if either (or both) endpoints are within the perimeter.
	/// If <paramref name="value"/> is negative, all values match.
	/// </summary>
	public static Polygon ResetControlPointPairs(
		Polygon shape, out int count, int value = -1, Polygon perimeter = null)
	{
		count = 0;
		if (shape is null) return null;
		if (! shape.HasID) return shape;

		var builder = new PolygonBuilderEx(shape);
		count = ResetControlPointPairs(builder, value, perimeter);
		return builder.ToGeometry();
	}

	/// <summary>
	/// See <see cref="ResetControlPointPairs(Polygon,out int,int,Polygon)"/>
	/// </summary>
	public static Polyline ResetControlPointPairs(
		Polyline shape, out int count, int value = -1, Polygon perimeter = null)
	{
		count = 0;
		if (shape is null) return null;
		if (!shape.HasID) return shape;

		var builder = new PolylineBuilderEx(shape);
		count = ResetControlPointPairs(builder, value, perimeter);
		return builder.ToGeometry();
	}

	public static int ResetControlPointPairs(
		MultipartBuilderEx builder, int value = -1, Polygon perimeter = null)
	{
		int count = 0;

		int partCount = builder.PartCount;
		for (int k = 0; k < partCount; k++)
		{
			int gapStartIndex = -1;
			var gapStartInPerimeter = false;

			int pointCount = builder.GetPointCount(k);
			for (int j = 0; j < pointCount; j++)
			{
				var point = builder.GetPoint(k, j);

				bool isMyCP = value < 0 && point.ID != 0 ||
				              value >= 0 && point.ID == value;
				bool inPerimeter = Within(point, perimeter);

				if (isMyCP)
				{
					if (gapStartIndex >= 0)
					{
						if (gapStartInPerimeter || inPerimeter)
						{
							point = SetPointID(point, 0);
							builder.SetPoint(k, j, point);
							point = builder.GetPoint(k, gapStartIndex);
							point = SetPointID(point, 0);
							builder.SetPoint(k, gapStartIndex, point);
							count += 2;
						}

						gapStartIndex = -1;
						gapStartInPerimeter = false;
					}
					else
					{
						gapStartIndex = j;
						gapStartInPerimeter = inPerimeter;
					}
				}

				bool isLastInPart = j == pointCount - 1;
				if (isLastInPart && gapStartIndex >= 0)
				{
					// An unpaired control point? Remove it:
					point = builder.GetPoint(k, gapStartIndex);
					point = SetPointID(point, 0);
					builder.SetPoint(k, gapStartIndex, point);
					count += 1;
				}
			}
		}

		return count;
	}

	private static bool Within(MapPoint point, Polygon perimeter)
	{
		if (point is null) return false;
		if (perimeter is null) return true;
		return GeometryUtils.Contains(perimeter, point);
	}
}

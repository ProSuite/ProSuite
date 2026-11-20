using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.SplitLine;

public static class SplitLineUtils
{
	/// <summary>
	/// The point at which the line is split when clicked at the specified point: the closest
	/// vertex if it is within the search radius, otherwise the closest point on the line.
	/// </summary>
	/// <returns>The split point, or null if it is too close to the start or end of a part.</returns>
	[CanBeNull]
	public static MapPoint GetSplitPoint([NotNull] Polyline polyline,
	                                     [NotNull] MapPoint clickPoint,
	                                     double searchRadius,
	                                     double xyTolerance)
	{
		Assert.ArgumentNotNull(polyline, nameof(polyline));
		Assert.ArgumentNotNull(clickPoint, nameof(clickPoint));

		ProximityResult vertex = GeometryEngine.Instance.NearestVertex(polyline, clickPoint);

		MapPoint result = vertex.Distance <= searchRadius
			                  ? vertex.Point
			                  : GeometryEngine.Instance.QueryPointAndDistance(
				                  polyline, SegmentExtensionType.NoExtension, clickPoint,
				                  AsRatioOrLength.AsLength, out _, out _, out _);

		// Simplify removes segments shorter than about 3 times the tolerance, which would
		// leave the line unsplit or the parts unable to merge again.
		double minDistanceToEnd = 3 * xyTolerance;

		foreach (ReadOnlySegmentCollection part in polyline.Parts)
		{
			if (GeometryEngine.Instance.Distance(result, part[0].StartPoint) <= minDistanceToEnd ||
			    GeometryEngine.Instance.Distance(result, part[part.Count - 1].EndPoint) <=
			    minDistanceToEnd)
			{
				return null;
			}
		}

		return result;
	}

	/// <summary>
	/// The candidate closest to the split point that lies on the interior of the line, such
	/// as an existing junction the line should rather be split at.
	/// </summary>
	[CanBeNull]
	public static MapPoint GetClosestPointOnInterior([NotNull] Polyline polyline,
	                                                 [NotNull] IEnumerable<MapPoint> candidates,
	                                                 [NotNull] MapPoint splitPoint,
	                                                 double xyTolerance)
	{
		return candidates
		       .Where(p => GeometryEngine.Instance.Distance(p, polyline) <= xyTolerance &&
		                   GetSplitPoint(polyline, p, 0, xyTolerance) != null)
		       .OrderBy(p => GeometryEngine.Instance.Distance(p, splitPoint))
		       .FirstOrDefault();
	}

	/// <summary>
	/// Creates a short line segment centered on <paramref name="splitPoint"/> and oriented
	/// perpendicular to <paramref name="polyline"/>. At an interior vertex the cut line bisects
	/// the angle between the adjacent segments.
	/// </summary>
	[CanBeNull]
	public static Polyline CreatePerpendicularCutLine([NotNull] Polyline polyline,
	                                                  [NotNull] MapPoint splitPoint,
	                                                  double halfLength)
	{
		Assert.ArgumentNotNull(polyline, nameof(polyline));
		Assert.ArgumentNotNull(splitPoint, nameof(splitPoint));

		if (polyline.IsEmpty || halfLength <= 0)
		{
			return null;
		}

		SpatialReference sr = polyline.SpatialReference;

		MapPoint pointOnLine = sr != null && splitPoint.SpatialReference != null &&
		                       ! sr.Equals(splitPoint.SpatialReference)
			                       ? GeometryUtils.EnsureSpatialReference(splitPoint, sr)
			                       : splitPoint;

		if (! TryGetTangent(polyline, pointOnLine, out double tx, out double ty))
		{
			return null;
		}

		double nx = -ty;
		double ny = tx;

		double x1 = pointOnLine.X - nx * halfLength;
		double y1 = pointOnLine.Y - ny * halfLength;
		double x2 = pointOnLine.X + nx * halfLength;
		double y2 = pointOnLine.Y + ny * halfLength;

		bool hasZ = pointOnLine.HasZ;

		MapPoint startPoint = hasZ
			                      ? MapPointBuilderEx.CreateMapPoint(x1, y1, pointOnLine.Z, sr)
			                      : MapPointBuilderEx.CreateMapPoint(x1, y1, sr);
		MapPoint endPoint = hasZ
			                    ? MapPointBuilderEx.CreateMapPoint(x2, y2, pointOnLine.Z, sr)
			                    : MapPointBuilderEx.CreateMapPoint(x2, y2, sr);

		var builder = new PolylineBuilderEx(sr) { HasZ = hasZ };
		builder.AddPart(new[] { startPoint, endPoint });
		return builder.ToGeometry();
	}

	private static bool TryGetTangent([NotNull] Polyline polyline,
	                                  [NotNull] MapPoint point,
	                                  out double tx,
	                                  out double ty)
	{
		double xyTolerance = polyline.SpatialReference?.XYTolerance ?? 1e-6;
		ProximityResult nearestVertex = GeometryEngine.Instance.NearestVertex(polyline, point);

		if (nearestVertex is { PointIndex: { } globalVertexIndex } &&
		    nearestVertex.Distance <= xyTolerance)
		{
			int localVertexIndex = GeometryUtils.GetLocalVertexIndex(
				polyline, globalVertexIndex, out int partIndex);
			ReadOnlySegmentCollection part = polyline.Parts[partIndex];

			if (localVertexIndex > 0 && localVertexIndex < part.Count &&
			    TryGetSegmentUnitTangent(part[localVertexIndex - 1], 1.0,
			                             out double t1x, out double t1y) &&
			    TryGetSegmentUnitTangent(part[localVertexIndex], 0.0,
			                             out double t2x, out double t2y))
			{
				double bx = t1x + t2x;
				double by = t1y + t2y;
				double bisectorLength = Math.Sqrt(bx * bx + by * by);

				if (bisectorLength > 1e-9)
				{
					tx = bx / bisectorLength;
					ty = by / bisectorLength;
					return true;
				}

				// 180-degree foldback: fall back to the incoming segment tangent
				tx = t1x;
				ty = t1y;
				return true;
			}
		}

		ProximityResult nearest = GeometryEngine.Instance.NearestPoint(polyline, point);
		if (nearest is null || nearest.PartIndex < 0 || nearest.PartIndex >= polyline.PartCount)
		{
			tx = 0;
			ty = 0;
			return false;
		}

		Polyline partLine = polyline.PartCount == 1
			                    ? polyline
			                    : PolylineBuilderEx.CreatePolyline(
				                    polyline.Parts[nearest.PartIndex], polyline.SpatialReference);

		GeometryEngine.Instance.QueryPointAndDistance(
			partLine, SegmentExtensionType.NoExtension, point,
			AsRatioOrLength.AsLength, out double distanceAlong, out _, out _);

		Polyline tangentLine = GeometryEngine.Instance.QueryTangent(
			partLine, SegmentExtensionType.NoExtension, distanceAlong,
			AsRatioOrLength.AsLength, 1.0);

		if (tangentLine is null || tangentLine.PointCount < 2)
		{
			tx = 0;
			ty = 0;
			return false;
		}

		MapPoint start = tangentLine.Points[0];
		MapPoint end = tangentLine.Points[tangentLine.PointCount - 1];

		return TryNormalize(end.X - start.X, end.Y - start.Y, out tx, out ty);
	}

	private static bool TryGetSegmentUnitTangent([NotNull] Segment segment,
	                                             double ratio,
	                                             out double tx,
	                                             out double ty)
	{
		if (segment.Length <= 0)
		{
			tx = 0;
			ty = 0;
			return false;
		}

		LineSegment tangent = GeometryEngine.Instance.QueryTangent(
			segment, SegmentExtensionType.NoExtension, ratio,
			AsRatioOrLength.AsRatio, 1.0);

		if (tangent is null)
		{
			tx = 0;
			ty = 0;
			return false;
		}

		return TryNormalize(tangent.EndCoordinate.X - tangent.StartCoordinate.X,
		                    tangent.EndCoordinate.Y - tangent.StartCoordinate.Y,
		                    out tx, out ty);
	}

	private static bool TryNormalize(double dx, double dy, out double ux, out double uy)
	{
		double length = Math.Sqrt(dx * dx + dy * dy);
		if (length <= 1e-9)
		{
			ux = 0;
			uy = 0;
			return false;
		}

		ux = dx / length;
		uy = dy / length;
		return true;
	}
}

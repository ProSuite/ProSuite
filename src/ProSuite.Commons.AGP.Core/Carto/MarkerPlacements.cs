using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Core.Carto;

/// <summary>
/// Implementation of some of the marker placements that ArcGIS Pro
/// offers as part of symbol definitions (<see cref="CIMMarkerPlacement"/>).
/// Other than marker placements in symbol definitions, these here
/// assume all size and distance parameters are in the same units
/// as the linear unit of the given shape's spatial reference!
/// </summary>
public static class MarkerPlacements
{
	public class Options
	{
		public bool PlacePerPart { get; set; }
	}

	public class FillOptions : Options { }

	public class StrokeOptions : Options
	{
		public bool AngleToLine { get; set; }
		public double PerpendicularOffset { get; set; } // in CIM just "Offset"
	}

	public enum Extremity
	{
		Both = 0, JustBegin = 1, JustEnd = 2, None = 3
	}

	public class AtExtremitiesOptions : StrokeOptions
	{
		public Extremity Extremity { get; set; }
		public double OffsetAlongLine { get; set; }
	}

	public static IEnumerable<Geometry> AtExtremities(
		Geometry marker, Geometry reference, AtExtremitiesOptions options)
	{
		if (marker is null) yield break;
		if (reference is not Polyline polyline) yield break;
		if (options is null) throw new ArgumentNullException(nameof(options));
		if (options.Extremity == Extremity.None) yield break;

		bool angleToLine = options.AngleToLine;
		double perpendicularOffset = options.PerpendicularOffset;
		var extremity = options.Extremity;
		double offsetAlong = options.OffsetAlongLine;

		if (options.PlacePerPart)
		{
			foreach (var line in GetPartLines(polyline))
			{
				if (extremity is Extremity.JustBegin or Extremity.Both)
				{
					if (offsetAlong <= line.Length && // not off other end
					    GetPointAndTangent(line, offsetAlong, out var position, out var tangent))
					{
						tangent *= -1; // flip tangent at begin of line
						// Negate begin perpendicular offset to match ArcGIS Pro behaviour
						yield return Placed(marker, position, tangent, angleToLine, -perpendicularOffset);
					}
				}

				if (extremity is Extremity.JustEnd or Extremity.Both)
				{
					double distance = line.Length - offsetAlong;
					if (distance >= 0 && // not off other end
					    GetPointAndTangent(line, distance, out var position, out var tangent))
					{
						yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
					}
				}
			}
		}
		else
		{
			if (extremity is Extremity.JustBegin or Extremity.Both)
			{
				if (offsetAlong <= polyline.Length && // not off other end
				    GetPointAndTangent(polyline, offsetAlong, out var position, out var tangent))
				{
					tangent *= -1; // flip tangent at begin of line
					// Negate begin perpendicular offset to match ArcGIS Pro behaviour
					yield return Placed(marker, position, tangent, angleToLine, -perpendicularOffset);
				}
			}

			if (extremity is Extremity.JustEnd or Extremity.Both)
			{
				double distance = polyline.Length - offsetAlong;
				if (distance >= 0 && // not off other end
				    GetPointAndTangent(polyline, distance, out var position, out var tangent))
				{
					yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
				}
			}
		}
	}

	public class OnVerticesOptions : StrokeOptions
	{
		public bool PlaceOnRegularVertices { get; set; }
		public bool PlaceOnControlPoints { get; set; }
		public bool PlaceOnEndPoints { get; set; }
	}

	public static IEnumerable<Geometry> OnVertices(
		Geometry marker, Geometry reference, OnVerticesOptions options)
	{
		if (marker is null) yield break;
		if (reference is not Multipart polycurve) yield break;
		if (options is null) throw new ArgumentNullException(nameof(options));

		bool angleToLine = options.AngleToLine;
		double perpendicularOffset = options.PerpendicularOffset;

		int partCount = polycurve.Parts.Count;
		for (int j = 0; j < partCount; j++)
		{
			var part = polycurve.Parts[j];
			var segmentCount = part.Count;
			if (segmentCount < 1) continue;

			var segment = part[0];
			var point = segment.StartPoint;
			var position = point.ToPair();
			var tangent = GetTangent(segment, 0);

			bool isEndPoint = j == 0 || options.PlacePerPart;
			bool isControlPoint = point.HasID && point.ID > 0;

			if (options.PlaceOnEndPoints && isEndPoint ||
			    options.PlaceOnControlPoints && isControlPoint)
			{
				yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
			}

			for (int i = 0; i < segmentCount-1; i++)
			{
				segment = part[i];
				point = segment.EndPoint;
				position = point.ToPair();
				tangent = GetTangent(segment, 1);

				bool lastInPart = i == segmentCount - 1;
				isEndPoint = lastInPart && (j == partCount - 1 || options.PlacePerPart);
				isControlPoint = point.HasID && point.ID > 0;
				bool isRegularVertex = ! isEndPoint && ! isControlPoint;

				if (isEndPoint && options.PlaceOnEndPoints ||
				    isControlPoint && options.PlaceOnControlPoints ||
				    isRegularVertex && options.PlaceOnRegularVertices)
				{
					yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
				}
			}
		}
	}

	public class PolygonCenterOptions : FillOptions
	{
		public bool UseBoundingBox { get; set; }
		public bool ForceInsidePolygon { get; set; }
		public double OffsetX { get; set; }
		public double OffsetY { get; set; }
	}

	public static IEnumerable<T> PolygonCenter<T>(
		T marker, Geometry reference, PolygonCenterOptions options) where T : Geometry
	{
		if (marker is null) yield break;
		if (reference is not Polygon polygon) yield break;
		if (options is null) throw new ArgumentNullException(nameof(options));

		if (options.PlacePerPart)
		{
			foreach (var part in polygon.Parts)
			{
				var partPoly = PolygonBuilderEx.CreatePolygon(part);
				MapPoint center = GetCenter(partPoly, options);
				double dx = center.X, dy = center.Y;
				yield return Translated(marker, dx, dy);
			}
		}
		else
		{
			MapPoint center = GetCenter(polygon, options);
			double dx = center.X, dy = center.Y;
			yield return Translated(marker, dx, dy);
		}
	}

	#region Private utilities

	private static MapPoint GetCenter(Polygon polygon, PolygonCenterOptions options)
	{
		if (options.UseBoundingBox) return polygon.Extent.Center;
		if (options.ForceInsidePolygon) return GeometryEngine.Instance.LabelPoint(polygon);
		return GeometryEngine.Instance.Centroid(polygon);
	}

	private static Pair GetTangent(Segment segment, double t)
	{
		if (segment is null) throw new ArgumentNullException(nameof(segment));
		if (t < 0) t = 0; else if (t > 1) t = 1; // clamp
		var line = GeometryEngine.Instance.QueryTangent(
			segment, SegmentExtensionType.NoExtension,
			t, AsRatioOrLength.AsRatio, 1.0);
		var position = line.StartPoint.ToPair();
		return line.EndPoint.ToPair() - position;
	}

	private static bool GetPointAndTangent(Polyline curve, double distanceAlong,
	                                       out Pair position, out Pair tangent)
	{
		position = tangent = Pair.Null;
		if (curve is null) return false;
		if (curve.IsEmpty) return false;

		const double tangentLength = 1.0; // unit length!
		var polyline = GeometryEngine.Instance.QueryTangent(
			curve, SegmentExtensionType.ExtendTangents,
			distanceAlong, AsRatioOrLength.AsLength, tangentLength);

		var pointCount = polyline.Points.Count;
		if (pointCount < 2) return false; // most illogical
		var startPoint = polyline.Points[0].ToPair();
		var endPoint = polyline.Points[pointCount - 1].ToPair();

		position = startPoint;
		tangent = endPoint - startPoint;
		return true;
	}

	private static IEnumerable<Polyline> GetPartLines(Polyline polyline)
	{
		if (polyline is null) return Enumerable.Empty<Polyline>();
		var parts = GeometryEngine.Instance.MultipartToSinglePart(polyline);
		return parts.OfType<Polyline>();
	}

	private static T Rotated<T>(T shape, double angle, MapPoint pivot = null) where T : Geometry
	{
		if (shape is null) return null;
		if (pivot is null) pivot = MapPointBuilderEx.CreateMapPoint(0, 0);
		return (T) GeometryEngine.Instance.Rotate(shape, pivot, angle);
	}

	private static T Translated<T>(T shape, double dx, double dy) where T : Geometry
	{
		if (shape is null) return null;
		return (T) GeometryEngine.Instance.Move(shape, dx, dy);
	}

	private static T Placed<T>(
		T marker, Pair position, Pair tangent, bool angleToLine, double offset) where T : Geometry
	{
		// if angleToLine: rotate marker by tangent.Angle (around origin)
		// move marker to tangent.StartPoint
		// if perpOfs <> 0: move by tangent rotated 90° scaled perpOfs

		if (angleToLine)
		{
			var angleRadians = Math.Atan2(tangent.Y, tangent.X);
			marker = Rotated(marker, angleRadians);
		}

		if (offset < 0 || offset > 0)
		{
			// assume tangent has unit length!
			var vector = tangent.Rotated(90) * offset;
			position += vector;
		}

		return Translated(marker, position.X, position.Y);
	}

	#endregion
}

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

	[Flags]
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

		// Beware!
		// The extremity flags tell where to NOT place a marker!
		// (This is to match ArcGIS, where Both is the default.)
		// Therefore, if JustBegin is NOT set, draw marker at begin!

		if (options.PlacePerPart)
		{
			foreach (var part in polyline.Parts)
			{
				if ((options.Extremity & Extremity.JustBegin) == 0)
				{
					var segment = part.FirstOrDefault();
					var tangent = GetTangent(segment, 0);

					if (tangent != null)
					{
						yield return Place(marker, tangent, angleToLine, perpendicularOffset);
					}
				}

				if ((options.Extremity & Extremity.JustEnd) == 0)
				{
					var segment = part.LastOrDefault();
					var tangent = GetTangent(segment, 1);

					if (tangent != null)
					{
						yield return Place(marker, tangent, angleToLine, perpendicularOffset);
					}
				}
			}
		}
		else
		{
			if ((options.Extremity & Extremity.JustBegin) == 0)
			{
				var firstPart = polyline.Parts.FirstOrDefault();
				var segment = firstPart?.FirstOrDefault();
				var tangent = GetTangent(segment, 0);

				if (tangent != null)
				{
					yield return Place(marker, tangent, angleToLine, perpendicularOffset);
				}
			}

			if ((options.Extremity & Extremity.JustEnd) == 0)
			{
				var lastPart = polyline.Parts.LastOrDefault();
				var segment = lastPart?.LastOrDefault();
				var tangent = GetTangent(segment, 1);

				if (tangent != null)
				{
					yield return Place(marker, tangent, angleToLine, perpendicularOffset);
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
			var tangent = GetTangent(segment, 0);
			var point = segment.StartPoint;

			bool isEndPoint = j == 0 || options.PlacePerPart;
			bool isControlPoint = point.HasID && point.ID > 0;

			if (options.PlaceOnEndPoints && isEndPoint ||
			    options.PlaceOnControlPoints && isControlPoint)
			{
				yield return Place(marker, tangent, angleToLine, perpendicularOffset);
			}

			for (int i = 0; i < segmentCount-1; i++)
			{
				segment = part[i];
				tangent = GetTangent(segment, 1);
				point = segment.EndPoint;

				bool lastInPart = i == segmentCount - 1;
				isEndPoint = lastInPart && (j == partCount - 1 || options.PlacePerPart);
				isControlPoint = point.HasID && point.ID > 0;
				bool isRegularVertex = ! isEndPoint && ! isControlPoint;

				if (isEndPoint && options.PlaceOnEndPoints ||
				    isControlPoint && options.PlaceOnControlPoints ||
				    isRegularVertex && options.PlaceOnRegularVertices)
				{
					yield return Place(marker, tangent, angleToLine, perpendicularOffset);
				}
			}
		}
	}

	public static T Place<T>(T marker, LineSegment tangent, bool angleToLine, double offset)
		where T : Geometry
	{
		// if angleToLine: rotate marker by tangent.Angle (around origin)
		// move marker to tangent.StartPoint
		// if perpOfs <> 0: move by tangent rotated 90° scaled perpOfs

		if (angleToLine)
		{
			marker = Rotated(marker, tangent.Angle);
		}

		var startPoint = tangent.StartCoordinate;
		double dx = startPoint.X, dy = startPoint.Y;

		if (offset < 0 || offset > 0)
		{
			var endPoint = tangent.EndCoordinate;
			double ox = endPoint.X - startPoint.X;
			double oy = endPoint.Y - startPoint.Y;
			// assume tangent has unit length!
			ox *= offset;
			oy *= offset;
			// rotate 90 degrees TODO which direction?
			(ox, oy) = (-oy, ox);
			// and add to the main translation
			dx += ox;
			dy += oy;
		}

		return Translated(marker, dx, dy);
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

	private static LineSegment GetTangent(Segment segment, double t)
	{
		if (segment is null) return null;
		if (t < 0) t = 0; else if (t > 1) t = 1; // clamp
		return GeometryEngine.Instance.QueryTangent(
			segment, SegmentExtensionType.NoExtension,
			t, AsRatioOrLength.AsRatio, 1.0);
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

	#endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Geom;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

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
	// TODO Just an idea: marker placements modify a transformation matrix that will be applied later;
	// signature sth like: IEnumerable<Matrix> MyPlacement(Matrix matrix, Geometry reference, MP parameters)
	// the input matrix already represents AnchorPoint, Rotation, Offset, ScaleX (and maybe Size)

	public abstract class Options
	{
		public bool PlacePerPart { get; set; }
	}

	public abstract class FillOptions : Options { }

	public abstract class StrokeOptions : Options
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

	public static IEnumerable<T> AtExtremities<T>(
		T marker, Geometry reference, AtExtremitiesOptions options) where T : Geometry
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

	public static IEnumerable<T> OnVertices<T>(
		T marker, Geometry reference, OnVerticesOptions options) where T : Geometry
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

			if (isEndPoint && options.PlaceOnEndPoints ||
			    isControlPoint && options.PlaceOnControlPoints)
			{
				yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
			}

			for (int i = 0; i < segmentCount - 1; i++)
			{
				segment = part[i];
				point = segment.EndPoint;
				position = point.ToPair();
				var pre = GetTangent(segment, segment.Length);
				var post = GetTangent(part[i + 1], 0);
				// average direction from segments before and after:
				tangent = (pre + post).Normalized(); // unit length!

				isControlPoint = point.HasID && point.ID > 0;
				bool isRegularVertex = ! isControlPoint;

				if (isControlPoint && options.PlaceOnControlPoints ||
				    isRegularVertex && options.PlaceOnRegularVertices)
				{
					yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
				}
			}

			segment = part[segmentCount - 1];
			point = segment.EndPoint;
			position = point.ToPair();
			tangent = GetTangent(segment, segment.Length);

			isEndPoint = j == partCount - 1 || options.PlacePerPart;
			isControlPoint = point.HasID && point.ID > 0;

			if (isEndPoint && options.PlaceOnEndPoints ||
			    isControlPoint && options.PlaceOnControlPoints)
			{
				yield return Placed(marker, position, tangent, angleToLine, perpendicularOffset);
			}
		}
	}

	public enum OnLinePosition
	{
		Middle, Start, End, SegmentMidpoints
	}

	public class OnLineOptions : StrokeOptions
	{
		public OnLinePosition RelativeTo { get; set; }
		public double StartPointOffset { get; set; }
	}

	public static IEnumerable<T> OnLine<T>(
		T marker, Geometry reference, OnLineOptions options) where T : Geometry
	{
		if (marker is null) yield break;
		if (reference is not Multipart polycurve) yield break;
		if (options is null) throw new ArgumentNullException(nameof(options));

		bool angleToLine = options.AngleToLine;
		double perpOffset = options.PerpendicularOffset;

		// positive is *in* from Start/End and along line from Middle
		double offsetAlong = options.StartPointOffset;

		if (options.RelativeTo == OnLinePosition.SegmentMidpoints)
		{
			foreach (var segment in polycurve.Parts.SelectMany(part => part))
			{
				var distanceAlong = segment.Length / 2 + offsetAlong;

				if (GetPointAndTangent(segment, distanceAlong, out var position, out var tangent))
				{
					yield return Placed(marker, position, tangent, angleToLine, perpOffset);
				}
			}
		}
		else
		{
			if (options.PlacePerPart)
			{
				foreach (var partLine in GetPartLines(GetPolyline(polycurve)))
				{
					var distanceAlong = GetOnLineDistance(options.RelativeTo, partLine.Length, offsetAlong);

					if (GetPointAndTangent(partLine, distanceAlong,
					                       out Pair position, out Pair tangent))
					{
						yield return Placed(marker, position, tangent, angleToLine, perpOffset);
					}
				}
			}
			else
			{
				var distanceAlong = GetOnLineDistance(options.RelativeTo, polycurve.Length, offsetAlong);

				if (GetPointAndTangent(polycurve, distanceAlong,
				                       out Pair position, out Pair tangent))
				{
					yield return Placed(marker, position, tangent, angleToLine, perpOffset);
				}
			}
		}
	}

	public enum EndingsType
	{
		Unconstrained, Marker, HalfStep, FullStep, Custom
	}

	public class AlongLineOptions : StrokeOptions
	{
		public double[] Pattern { get; set; }
		public EndingsType Endings { get; set; }
		public double OffsetAlongLine { get; set; } // only for Unconstrained and Custom
		public double CustomEndingOffset { get; set; } // only for Custom
	}

	public static IEnumerable<T> AlongLine<T>(
		T marker, Geometry reference, AlongLineOptions options)
	{
		throw new NotImplementedException(
			$"Marker placement {nameof(AlongLine)} not yet implemented");
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
				double dx = center.X + options.OffsetX;
				double dy = center.Y + options.OffsetY;
				yield return Translated(marker, dx, dy);
			}
		}
		else
		{
			MapPoint center = GetCenter(polygon, options);
			double dx = center.X + options.OffsetX;
			double dy = center.Y + options.OffsetY;
			yield return Translated(marker, dx, dy);
		}
	}

	public enum AroundPolygonPosition
	{
		Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight
	}

	public class AroundPolygonOptions : FillOptions
	{
		public AroundPolygonPosition Position { get; set; }
		public double Offset { get; set; }
	}

	public static IEnumerable<T> AroundPolygon<T>(
		T marker, Geometry reference, AroundPolygonOptions options) where T : Geometry
	{
		// Hmm, seems to relate to the convex hull, but details unsure
		// Offset seems to be in dirs 0 45 90 135 180 etc. depending on Position (negative is inward)
		throw new NotImplementedException(
			$"Marker placement {nameof(AroundPolygon)} is not yet implemented");
	}

	public static IEnumerable<T> OnPoint<T>(T marker, MapPoint point) where T : Geometry
	{
		if (marker is null) yield break;
		if (point is null) yield break;
		if (point.IsEmpty) yield break;

		yield return Translated(marker, point.X, point.Y);
	}

	#region Private utilities

	private static MapPoint GetCenter(Polygon polygon, PolygonCenterOptions options)
	{
		if (options.UseBoundingBox) return polygon.Extent.Center;
		if (options.ForceInsidePolygon) return GeometryEngine.Instance.LabelPoint(polygon);
		return GeometryEngine.Instance.Centroid(polygon);
	}

	private static Pair GetTangent(Segment segment, double distanceAlong)
	{
		if (segment is null) throw new ArgumentNullException(nameof(segment));
		if (distanceAlong > segment.Length) distanceAlong = segment.Length;
		else if(distanceAlong < 0) distanceAlong = 0; // clamp

		const double tangentLength = 1.0; // unit length!
		var line = GeometryEngine.Instance.QueryTangent(
			segment, SegmentExtensionType.NoExtension,
			distanceAlong, AsRatioOrLength.AsLength, tangentLength);

		var position = line.StartPoint.ToPair();
		return line.EndPoint.ToPair() - position;
	}

	private static bool GetPointAndTangent(Segment segment, double distanceAlong,
	                                       out Pair position, out Pair tangent)
	{
		position = tangent = Pair.Null;
		if (segment is null) return false;
		if (!(segment.Length > 0)) return false;

		const double tangentLength = 1.0; // unit length!
		var line = GeometryEngine.Instance.QueryTangent(
			segment, SegmentExtensionType.ExtendTangents,
			distanceAlong, AsRatioOrLength.AsLength, tangentLength);

		position = line.StartPoint.ToPair();
		tangent = line.EndPoint.ToPair() - position;
		return true;
	}

	private static bool GetPointAndTangent(Multipart curve, double distanceAlong,
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

	private static Polyline GetPolyline(Multipart polycurve)
	{
		return polycurve switch
		{
			null => null,
			Polyline polyline => polyline,
			Polygon polygon => GeometryUtils.Boundary(polygon),
			_ => throw new AssertionException("Multipart is neither Polyline nor Polygon")
		};
	}

	private static IEnumerable<Polyline> GetPartLines(Polyline polyline)
	{
		if (polyline is null) return Enumerable.Empty<Polyline>();
		var parts = GeometryEngine.Instance.MultipartToSinglePart(polyline);
		return parts.OfType<Polyline>();
	}

	private static double GetOnLineDistance(OnLinePosition position,
	                                        double lineLength, double offsetAlong)
	{
		return position switch
		{
			OnLinePosition.Start => offsetAlong, // positive is in from Start
			OnLinePosition.Middle => lineLength / 2 + offsetAlong, // pos is along line
			OnLinePosition.End => lineLength - offsetAlong, // pos is in from End
			_ => throw new ArgumentOutOfRangeException(nameof(position))
		};
	}

	private static T Rotated<T>(T shape, double angleRadians, MapPoint pivot = null) where T : Geometry
	{
		if (shape is null) return null;
		if (pivot is null) pivot = MapPointBuilderEx.CreateMapPoint(0, 0);
		return (T) GeometryEngine.Instance.Rotate(shape, pivot, angleRadians);
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
		// if (perpendicular) offset <> 0: move by tangent rotated 90° scaled offset

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

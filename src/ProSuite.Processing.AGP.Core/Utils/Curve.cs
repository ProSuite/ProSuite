using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;

namespace ProSuite.Processing.AGP.Core.Utils;

/// <summary>Emulate good old AO ICurve</summary>
public interface ICurve
{
	bool IsEmpty { get; }

	bool IsClosed { get; }

	Envelope Envelope { get; }

	double Length { get; }

	MapPoint StartPoint { get; }

	MapPoint EndPoint { get; }

	IReadOnlyList<Segment> Segments { get; }

	MapPoint GetPoint(double distanceAlong);

	double GetDirection(double distanceAlong); // radians, ccw from positive x axis

	bool GetTangent(double distanceAlong, out double dx, out double dy);

	// may add QueryPoint(), QueryPointAndDistance(), GetSubcurve(), Reversed(), etc.
}

public class Curve : ICurve
{
	public bool IsEmpty { get; }
	public bool IsClosed { get; }
	public Envelope Envelope { get; }
	public double Length { get; }
	public MapPoint StartPoint { get; }
	public MapPoint EndPoint { get; }
	public int SRID { get; }

	public IReadOnlyList<Segment> Segments { get; }

	public Curve(IEnumerable<Segment> path)
	{
		Segments = path?.ToArray() ?? Array.Empty<Segment>();

		var sref = Segments.Select(s => s.SpatialReference)
		                   .FirstOrDefault(s => s != null);

		var builder = new EnvelopeBuilderEx(sref);

		Length = 0.0;
		IsEmpty = true;
		IsClosed = false;

		foreach (var segment in Segments)
		{
			if (StartPoint is null)
			{
				StartPoint = segment.StartPoint;
			}

			EndPoint = segment.EndPoint;

			Length += segment.Length;
			IsEmpty = false;

			switch (segment)
			{
				case LineSegment line:
					builder.Union(
						EnvelopeBuilderEx.CreateEnvelope(
							line.StartCoordinate, line.EndCoordinate));
					break;
				case EllipticArcSegment arc:
					builder.Union(arc.Get2DEnvelope());
					break;
				case CubicBezierSegment bezier:
					builder.Union(bezier.Get2DEnvelope());
					break;
				default:
					throw new NotSupportedException(
						$"Unsupported segment type: {segment.GetType().Name}");
			}
		}

		Envelope = builder.ToGeometry().Extent;
		SRID = sref?.Wkid ?? 0;
	}

	public MapPoint GetPoint(double distanceAlong)
	{
		if (Segments.Count < 1)
		{
			return null;
		}

		if (distanceAlong < 0)
		{
			return StartPoint;
		}

		double length = 0.0;

		foreach (var segment in Segments)
		{
			if (length + segment.Length >= distanceAlong)
			{
				var segmentExtension = SegmentExtensionType.NoExtension;

				return GeometryEngine.Instance.QueryPoint(
					segment, segmentExtension,
					distanceAlong - length, AsRatioOrLength.AsLength);
			}

			length += segment.Length;
		}

		return EndPoint;
	}

	public bool GetTangent(double distanceAlong, out double dx, out double dy)
	{
		if (distanceAlong < 0)
		{
			distanceAlong = 0;
		}

		double length = 0.0;
		Segment segment = null;

		foreach (var current in Segments)
		{
			segment = current;

			if (length + current.Length >= distanceAlong)
			{
				break;
			}

			length += current.Length;
		}

		if (segment is null)
		{
			dx = dy = double.NaN;
			return false;
		}

		var segmentExtension = SegmentExtensionType.NoExtension;
		const double tangentLength = 10.0; // arbitrary finite positive number
		var line = GeometryEngine.Instance.QueryTangent(
			segment, segmentExtension,
			distanceAlong - length, AsRatioOrLength.AsLength,
			tangentLength);

		dx = line.EndCoordinate.X - line.StartCoordinate.X;
		dy = line.EndCoordinate.Y - line.StartCoordinate.Y;

		return true;
	}

	public double GetDirection(double distanceAlong)
	{
		var segment = FindSegment(distanceAlong, out double segmentOffset);

		var segmentExtension = SegmentExtensionType.ExtendTangents;
		const double tangentLength = 10.0; // arbitrary finite positive number
		var line = GeometryEngine.Instance.QueryTangent(
			segment, segmentExtension,
			segmentOffset, AsRatioOrLength.AsLength,
			tangentLength);

		var dx = line.EndCoordinate.X - line.StartCoordinate.X;
		var dy = line.EndCoordinate.Y - line.StartCoordinate.Y;

		return Math.Atan2(dy, dx);
	}

	private Segment FindSegment(double distanceAlongCurve, out double distanceAlongSegment)
	{
		if (Segments.Count < 1)
		{
			distanceAlongSegment = double.NaN;
			return null; // empty curve has no segments
		}

		if (distanceAlongCurve < 0)
		{
			// before first segment
			distanceAlongSegment = distanceAlongCurve;
			return Segments.First();
		}

		double distanceSeen = 0.0;

		foreach (var segment in Segments)
		{
			if (distanceSeen + segment.Length >= distanceAlongCurve)
			{
				distanceAlongSegment = distanceAlongCurve - distanceSeen;
				return segment;
			}

			distanceSeen += segment.Length;
		}

		// after last segment
		var last = Segments.Last();
		distanceAlongSegment = distanceAlongCurve - distanceSeen + last.Length;
		return last;
	}
}

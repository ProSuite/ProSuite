using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using Polyline = ArcGIS.Core.Geometry.Polyline;
using Segment = ArcGIS.Core.Geometry.Segment;

namespace ProSuite.Commons.AGP.Core.Carto;

/// <summary>
/// Implementation of some of the geometric effects that ArcGIS Pro
/// offers as part of symbol definitions (<see cref="CIMGeometricEffect"/>).
/// Other than geometric effects in symbol definitions, those here
/// assume all size and distance parameters are in the same units
/// as the linear unit of the given shape's spatial reference!
/// </summary>
public static class GeometricEffects
{
	public static Geometry AddControlPoints(
		Geometry shape, double maxAngleDegrees, int idValue = 1)
	{
		if (shape is null) return null;
		if (shape.IsEmpty) return shape;
		if (shape is not Multipart polycurve) return shape;

		// get angle to 0..180 range:
		maxAngleDegrees %= 360.0;
		if (maxAngleDegrees < 0)
			maxAngleDegrees *= -1;
		if (maxAngleDegrees > 180.0)
			maxAngleDegrees = 360.0 - maxAngleDegrees;

		MultipartBuilderEx builder = polycurve switch
		{
			Polyline polyline => new PolylineBuilderEx(polyline),
			Polygon polygon => new PolygonBuilderEx(polygon),
			_ => throw new AssertionException("multipart is neither polyline nor polygon")
		};

		builder.HasID = true; // make builder aware of ID values

		// Those builders cannot set points, just segments!
		// Update both: inbound.EndPoint, outbound.StartPoint!

		int partCount = polycurve.Parts.Count;
		for (int k = 0; k < partCount; k++)
		{
			var part = polycurve.Parts[k];

			int segmentCount = part.Count;
			if (segmentCount < 1) continue;

			for (int i = 0; i <= segmentCount; i++)
			{
				if (GetAngle(polycurve, k, i, out var angleDegrees))
				{
					if (angleDegrees <= maxAngleDegrees)
					{
						builder.SetPointID(k, i, idValue);
					}
				}
			}
		}

		return builder.ToGeometry();
	}

	public static Geometry Cut(
		Geometry shape, double beginCut, double endCut, bool invert = false, double middleCut = 0.0)
	{
		if (shape is null) return null;
		if (shape.IsEmpty) return shape;
		if (shape is not Polyline polyline) return shape;

		if (!(beginCut > 0)) beginCut = 0;
		if (!(endCut > 0)) endCut = 0;
		if (!(middleCut > 0)) middleCut = 0;

		// Do the cut for each part of the (potentially) multipart shape!

		var builder = Configure(new PolylineBuilderEx(), shape);

		foreach (var line in GetPartLines(polyline))
		{
			var length = line.Length;

			if (beginCut + endCut + middleCut >= length)
			{
				if (invert)
				{
					builder.AddParts(line.Parts);
				}
				continue;
			}

			if (invert)
			{
				// keep cuttings, drop rest: ===---===---===

				if (beginCut > 0)
				{
					var sub = GeometryEngine.Instance.GetSubCurve(
						line, 0, beginCut, AsRatioOrLength.AsLength);
					builder.AddParts(sub.Parts);
				}

				if (middleCut > 0)
				{
					var start = length / 2 - middleCut / 2;
					var end = length / 2 + middleCut / 2;
					var sub = GeometryEngine.Instance.GetSubCurve(
						line, start, end, AsRatioOrLength.AsLength);
					builder.AddParts(sub.Parts);
				}

				if (endCut > 0)
				{
					var start = length - endCut;
					var sub = GeometryEngine.Instance.GetSubCurve(
						line, start, length, AsRatioOrLength.AsLength);
					builder.AddParts(sub.Parts);
				}
			}
			else
			{
				// drop cuttings, keep rest: ---===---===---
				// (one piece if no middle cut: ---=========---)

				if (middleCut > 0)
				{
					var sub1 = GeometryEngine.Instance.GetSubCurve(
						line, beginCut, length / 2 - middleCut / 2,
						AsRatioOrLength.AsLength);
					builder.AddParts(sub1.Parts);

					var sub2 = GeometryEngine.Instance.GetSubCurve(
						line, length / 2 + middleCut / 2, length - endCut,
						AsRatioOrLength.AsLength);
					builder.AddParts(sub2.Parts);
				}
				else
				{
					var sub = GeometryEngine.Instance.GetSubCurve(
						line, beginCut, length - endCut, AsRatioOrLength.AsLength);
					builder.AddParts(sub.Parts);
				}
			}
		}

		return builder.ToGeometry();
	}

	public enum DashEndings
	{
		Unconstrained,
		HalfDash,
		HalfGap,
		FullDash,
		FullGap,
		Custom
	}

	public static Geometry Dashes(Geometry shape, double[] pattern,
	                              double offsetAlongLine = 0.0,
	                              DashEndings lineEnding = DashEndings.Unconstrained,
	                              DashEndings controlPointEnding = DashEndings.Unconstrained,
	                              double customEndOffset = 0.0)
	{
		if (shape is null) return null;
		if (pattern is not { Length: > 0 }) return shape;
		if (shape is not Multipart polycurve) return shape;

		// pattern is repeated; if odd, dashes and gaps alternate every other round
		// offsetAlongLine: applied only if DashEndings is Unconstrained or Custom
		// customPatternOffset: applied only if DashEndings is Custom
		// if controlPointEnding <> Unconstrained: treat sections between CPs individually
		// treat each original part separately!

		var result = Configure(new PolylineBuilderEx(), polycurve);
		var auxiliary = Configure(new PolylineBuilderEx(), polycurve);

		if (controlPointEnding == DashEndings.Unconstrained)
		{
			// control points unconstrained: treat each part
			foreach (var part in polycurve.Parts)
			{
				auxiliary.SetEmpty();
				auxiliary.AddSegments(part);
				var section = auxiliary.ToGeometry();
				DoDashLine(result, section, pattern, lineEnding, lineEnding, offsetAlongLine, customEndOffset);
			}
		}
		else
		{
			// treat sections between control points individually
			foreach (var triple in GetSections(polycurve))
			{
				var part = polycurve.Parts[triple.PartIndex];
				var segs = Range(part, triple.SegmentStart, triple.SegmentCount);

				auxiliary.SetEmpty();
				auxiliary.AddSegments(segs);
				var section = auxiliary.ToGeometry();

				var startType = triple.SegmentStart == 0 ? lineEnding : controlPointEnding;
				var endType = triple.SegmentStart + triple.SegmentCount >= part.Count
					              ? lineEnding
					              : controlPointEnding;

				DoDashLine(result, section, pattern, startType, endType, offsetAlongLine, customEndOffset);
			}
		}

		return result.ToGeometry();
	}

	#region Dashing utilities

	private static void DoDashLine(PolylineBuilderEx result, Polyline line, double[] pattern,
	                               DashEndings startType, DashEndings endType,
	                               double offsetAlongLine, double customEndOffset)
	{
		// NB. startType and endType is the same unless control points!
		// offsetAlongLine: where to start in pattern (i.e., positive shifts pattern left)
		// customEndOffset: where to end in pattern, *additive* to offsetAlongLine

		if (startType != DashEndings.Unconstrained || endType != DashEndings.Unconstrained)
		{
			// translate ending types to offsetAlongLine and customEndOffset:
			offsetAlongLine = GetDashesOffsetAlong(startType, pattern, offsetAlongLine);
			customEndOffset = GetDashesCustomEndOffset(endType, pattern, customEndOffset);

			double L = line.Length; // > 0
			double P = pattern.Sum(); // > 0
			if (pattern.Length % 2 != 0) P *= 2; // need two cycles if odd
			double A = offsetAlongLine;
			double E = customEndOffset;

			// L = s * (k*P + E)
			// where
			//   L,P,E as above (known)
			//   k a positive integer
			//   s >= 0 the stretch/squeeze factor

			A %= P;
			E %= P;
			double m = (L + A - E) / P;
			int q = (int) Math.Truncate(m); // quotient
			double r = m - q; // remainder
			int k = r >= 0.5 ? q + 1 : q; // prefer rounding up (prefer squeezing over stretching)
			if (k == 0 && r >= 0.25)
			{
				k = 1; // squeeze pattern at least once
			}
			double s = L / (k * P - A + E);


			pattern = pattern.Select(v => v * s).ToArray(); // scale pattern
			offsetAlongLine *= s; // and scale offset accordingly
		}

		DoDashLine(result, line, pattern, offsetAlongLine);
	}

	private static void DoDashLine(PolylineBuilderEx result, Polyline line,
	                               double[] pattern, double offsetAlongLine)
	{
		var patlen = pattern.Sum(); // effective pattern length
		if (pattern.Length % 2 != 0) patlen *= 2;
		offsetAlongLine %= patlen; // reduce to [0,patlen)

		bool inside = true;
		int index = 0;
		double linePos = offsetAlongLine < 0 ? -patlen - offsetAlongLine : -offsetAlongLine;
		double lineLength = line.Length;

		while (linePos < lineLength)
		{
			var p = pattern[index % pattern.Length];

			if (inside && linePos < line.Length && linePos + p > 0)
			{
				// inside a dash and not completely off the line
				var start = Math.Max(0, linePos);
				var end = Math.Min(linePos + p, line.Length);
				var dash = GeometryEngine.Instance.GetSubCurve(
					line, start, end, AsRatioOrLength.AsLength);
				result.AddParts(dash.Parts); // will always be a single part
			}

			index += 1;
			inside = ! inside;
			linePos += p;
		}
	}

	private static double GetDashesOffsetAlong(DashEndings endings, double[] pattern, double offsetAlongLine)
	{
		if (pattern is not { Length: > 0 })
			throw new ArgumentException("Need at least one element", nameof(pattern));

		return endings switch
		{
			DashEndings.Unconstrained => offsetAlongLine,
			DashEndings.HalfDash => 0.5 * pattern[0],
			DashEndings.HalfGap => -0.5 * pattern[pattern.Length - 1],
			DashEndings.FullDash => 0.0,
			DashEndings.FullGap => -1.0 * pattern[pattern.Length - 1],
			DashEndings.Custom => offsetAlongLine,
			_ => throw new ArgumentOutOfRangeException(nameof(endings), endings, null)
		};
	}

	private static double GetDashesCustomEndOffset(DashEndings endings, double[] pattern, double customEndOffset)
	{
		return endings switch
		{
			DashEndings.Unconstrained => customEndOffset,
			DashEndings.HalfDash => 0.5 * pattern[0],
			DashEndings.HalfGap => -0.5 * pattern[pattern.Length - 1],
			DashEndings.FullDash => 1.0 * pattern[0],
			DashEndings.FullGap => 0.0,
			DashEndings.Custom => customEndOffset,
			_ => throw new ArgumentOutOfRangeException(nameof(endings), endings, null)
		};
	}

	#endregion

	public static Geometry Offset(
		Geometry shape, double distance, OffsetType method)
	{
		if (shape is null) return null;

		if (shape is Multipart && (distance > 0 || distance < 0))
		{
			const double bevelRatio = 2.0; // TODO
			// NB: invert distance because Offset() method has positive=right convention
			return GeometryEngine.Instance.Offset(shape, -distance, method, bevelRatio);
		}

		// no effect on other geometry types or if no distance
		return shape;
	}

	public static Geometry Reverse(Geometry shape)
	{
		if (shape is null) return null;

		if (shape is Multipart polycurve)
		{
			return GeometryEngine.Instance.ReverseOrientation(polycurve);
		}

		// no effect on other geometry types
		return shape;
	}

	public static Geometry Suppress(Geometry shape, bool invert)
	{
		if (shape is null) return null;

		if (shape is Multipart { HasID: true } polycurve)
		{
			var builder = Configure(new PolylineBuilderEx(), shape);

			// treat each part separately!

			foreach (var segments in polycurve.Parts)
			{
				int count = segments.Count;
				if (count < 1) continue;

				var start = segments[0].StartPoint;
				bool suppress = start.ID > 0 ? ! invert : invert;

				for (int i = 0, i0 = 0; i < count; i++)
				{
					var point = segments[i].EndPoint;
					if (point.ID > 0)
					{
						suppress = ! suppress;
						if (suppress)
						{
							// emit what we traveled since last state change:
							const bool startNewPart = true;
							builder.AddSegments(Range(segments, i0, i-i0+1), startNewPart);
						}
					}
				}
			}

			return builder.ToGeometry();
		}

		// no effect if no control points
		return shape;
	}

	#region Private utilities

	private static IEnumerable<Segment> Range(ReadOnlySegmentCollection segments, int start, int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return segments[start + i];
		}
	}

	private readonly struct Section
	{
		public readonly int PartIndex;
		public readonly int SegmentStart;
		public readonly int SegmentCount;

		public Section(int partIndex, int segmentStart, int segmentCount)
		{
			PartIndex = partIndex;
			SegmentStart = segmentStart;
			SegmentCount = segmentCount;
		}
	}

	private static IEnumerable<Section> GetSections(Multipart polycurve)
	{
		int partCount = polycurve.Parts.Count;
		for (int k = 0; k < partCount; k++)
		{
			var part = polycurve.Parts[k];
			int segmentCount = part.Count;
			int start = 0, count = 0;

			for (int i = 0; i < segmentCount; i++)
			{
				count += 1;
				var endPoint = part[i].EndPoint;
				if (endPoint.HasID && endPoint.ID > 0)
				{
					yield return new Section(k, start, count);
					start += count;
					count = 0;
				}
			}

			if (count > 0)
			{
				yield return new Section(k, start, count);
			}
		}
	}

	private static IEnumerable<Polyline> GetPartLines(Polyline polyline)
	{
		if (polyline is null) return Enumerable.Empty<Polyline>();
		var parts = GeometryEngine.Instance.MultipartToSinglePart(polyline);
		return parts.OfType<Polyline>();
	}

	private static bool GetAngle(Multipart polycurve, int partIndex,
	                             int pointIndex, out double angleDegrees)
	{
		angleDegrees = double.NaN;
		var part = polycurve.Parts[partIndex];
		int segmentCount = part.Count;
		int pre, post;

		if (polycurve is Polygon)
		{
			post = pointIndex % segmentCount;
			if (post < 0) post += segmentCount;
			// now 0 <= j < segmentCount
			pre = post > 0 ? post - 1 : segmentCount - 1;
		}
		else
		{
			post = pointIndex;
			if (post >= segmentCount) return false;
			pre = post - 1;
			if (pre < 0) return false;
		}

		var inbound = GetTangent(part[pre], 1) * -1; // flip
		var outbound = GetTangent(part[post], 0);
		var cosAlpha = Pair.Dot(inbound, outbound); // assuming unit vectors!
		angleDegrees = Math.Acos(cosAlpha) * 180.0 / Math.PI;
		return true;
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

	private static T Configure<T>(T builder, Geometry template)
		where T : GeometryBuilderEx
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));
		if (template is null) return builder;

		builder.SpatialReference = template.SpatialReference;
		builder.HasZ = template.HasZ;
		builder.HasM = template.HasM;
		builder.HasID = template.HasID;

		return builder;
	}

	#endregion
}

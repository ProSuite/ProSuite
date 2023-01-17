using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	/// <summary>
	/// Implementation of some of the geometric effects that ArcGIS Pro
	/// offers as part of symbol definitions (<see cref="CIMGeometricEffect"/>).
	/// Other than geometric effects in symbol definitions, those here
	/// assume all size and distance parameters are in the same units
	/// as the linear unit of the given shape's spatial reference!
	/// </summary>
	public static class GeometricEffects
	{
		public static Geometry Cut(
			Geometry shape, double beginCut, double endCut, bool invert = false, double middleCut = 0.0)
		{
			if (shape is null) return null;

			if (shape is Multipart polycurve)
			{
				if (!(beginCut > 0)) beginCut = 0;
				if (!(endCut > 0)) endCut = 0;
				if (!(middleCut > 0)) middleCut = 0;

				var length = polycurve.Length;

				if (beginCut + endCut + middleCut >= length)
				{
					return invert ? shape : null;
				}

				var builder = Configure(new PolylineBuilderEx(), shape);

				if (invert)
				{
					// keep cuttings, drop rest
					if (beginCut > 0)
					{
						var sub = GeometryEngine.Instance.GetSubCurve(
							polycurve, 0, beginCut, AsRatioOrLength.AsLength);
						builder.AddParts(sub.Parts);
					}

					if (middleCut > 0)
					{
						var start = length / 2 - middleCut / 2;
						var end = length / 2 + middleCut / 2;
						var sub = GeometryEngine.Instance.GetSubCurve(
							polycurve, start, end, AsRatioOrLength.AsLength);
						builder.AddParts(sub.Parts);
					}

					if (endCut > 0)
					{
						var start = length - endCut;
						var sub = GeometryEngine.Instance.GetSubCurve(
							polycurve, start, length, AsRatioOrLength.AsLength);
						builder.AddParts(sub.Parts);
					}
				}
				else
				{
					if (middleCut > 0)
					{
						var sub1 = GeometryEngine.Instance.GetSubCurve(
							polycurve, beginCut, length / 2 - middleCut / 2,
							AsRatioOrLength.AsLength);
						builder.AddParts(sub1.Parts);

						var sub2 = GeometryEngine.Instance.GetSubCurve(
							polycurve, length / 2 + middleCut / 2, length - endCut,
							AsRatioOrLength.AsLength);
						builder.AddParts(sub2.Parts);
					}
					else
					{
						var sub = GeometryEngine.Instance.GetSubCurve(
							polycurve, beginCut, length - endCut, AsRatioOrLength.AsLength);
						builder.AddParts(sub.Parts);
					}
				}

				return builder.ToGeometry();
			}

			// no effect on other geometry types
			return shape;
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

		public static Geometry Dashes(
			Geometry shape, double[] pattern, DashEndings lineEnding, DashEndings controlPointEnding, double customOffset = 0.0, double offsetAlongLine = 0.0)
		{
			if (shape is null) return null;
			if (pattern is not { Length: > 0 }) return shape;
			if (shape is not Multipart polycurve) return shape;

			// offsetAlongLine: applied only if DashEndings is Unconstrained or Custom
			// customPatternOffset: applied only if DashEndings ???

			// treat each original part separately!
			// see CreateLineMarkers for hints on fitting stuff along a line

			throw new NotImplementedException();
		}

		public static Geometry Offset(
			Geometry shape, double distance, OffsetType method)
		{
			if (shape is null) return null;

			if (shape is Multipart && (distance > 0 || distance < 0))
			{
				const double bevelRatio = 2.0; // TODO
				return GeometryEngine.Instance.Offset(shape, distance, method, bevelRatio);
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

		private static IEnumerable<Segment> Range(ReadOnlySegmentCollection segments, int start, int count)
		{
			for (int i = 0; i < count; i++)
			{
				yield return segments[start + i];
			}
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
	}
}

using System;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class ControlPointUtils
	{
		// Note: very different from ArcObjects-based ControlPointUtils (immutable geoms)

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

		public static Segment SetPointID(Segment template, int? startPointID, int? endPointID)
		{
			if (! startPointID.HasValue && ! endPointID.HasValue)
			{
				return template; // nothing to update
			}

			switch (template)
			{
				case LineSegment lineSegment:
					using (var builder = new LineBuilder(lineSegment))
					{
						builder.StartPoint = SetPointID(builder.StartPoint, startPointID);
						builder.EndPoint = SetPointID(builder.EndPoint, endPointID);
						return builder.ToSegment();
					}

				case EllipticArcSegment ellipticSegment:
					using (var builder = new EllipticArcBuilder(ellipticSegment))
					{
						builder.StartPoint = SetPointID(builder.StartPoint, startPointID);
						builder.EndPoint = SetPointID(builder.EndPoint, endPointID);
						return builder.ToSegment();
					}

				case CubicBezierSegment bezierSegment:
					using (var builder = new CubicBezierBuilder(bezierSegment))
					{
						builder.StartPoint = SetPointID(builder.StartPoint, startPointID);
						builder.EndPoint = SetPointID(builder.EndPoint, endPointID);
						return builder.ToSegment();
					}

				default:
					throw new ArgumentException(@"Unknown segment type", nameof(template));
			}
		}

		public static MapPoint SetPointID(MapPoint template, int? pointID)
		{
			if (! pointID.HasValue)
			{
				return template; // nothing to update
			}

			var builder = new MapPointBuilderEx(template)
			              { HasID = true, ID = pointID.Value };

			return builder.ToGeometry();
		}
	}
}

using System;
using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class ControlPointUtils
	{
		// Note: very different from ArcObjects-based ControlPointUtils (immutable geoms)

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

			using (var builder = new MapPointBuilder(template))
			{
				builder.HasID = true;
				builder.ID = pointID.Value;
				builder.HasID = pointID.Value > 0;
				return builder.ToGeometry();
			}
		}
	}
}

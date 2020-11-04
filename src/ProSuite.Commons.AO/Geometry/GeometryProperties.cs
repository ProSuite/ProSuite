using System;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	[CLSCompliant(false)]
	public static class GeometryProperties
	{
		public static double? GetSliverRatio([NotNull] IGeometry geometry)
		{
			if (geometry.IsEmpty)
			{
				return null;
			}

			double length = GetLength(geometry);

			if (MathUtils.AreSignificantDigitsEqual(length, 0))
			{
				return null;
			}

			double area = GetArea(geometry);

			double result = length * length / area;

			return double.IsNaN(result) || double.IsInfinity(result)
				       ? (double?) null
				       : result;
		}

		public static double GetLength([NotNull] IGeometry geometry)
		{
			var curve = geometry as ICurve;
			if (curve != null)
			{
				return curve.Length;
			}

			var multiPatch = geometry as IMultiPatch;
			if (multiPatch != null)
			{
				IPolygon footprint = null;
				try
				{
					footprint = GeometryFactory.CreatePolygon(multiPatch);
					return footprint.Length;
				}
				finally
				{
					if (footprint != null)
					{
						Marshal.ReleaseComObject(footprint);
					}
				}
			}

			return 0;
		}

		public static double GetArea([NotNull] IGeometry geometry)
		{
			var area = geometry as IArea;
			if (area == null)
			{
				return 0;
			}

			double result = Math.Abs(area.Area);

			return double.IsNaN(result)
				       ? 0
				       : result;
		}

		[NotNull]
		public static SegmentCounts GetSegmentCounts(
			[NotNull] ISegmentCollection segmentCollection)
		{
			var result = new SegmentCounts();

			foreach (ISegment segment in
				GeometryUtils.GetSegments(segmentCollection.EnumSegments,
				                          allowRecycling: true))
			{
				switch (segment.GeometryType)
				{
					case esriGeometryType.esriGeometryLine:
						result.LinearSegmentCount++;
						break;

					case esriGeometryType.esriGeometryCircularArc:
						result.CircularArcCount++;
						break;

					case esriGeometryType.esriGeometryEllipticArc:
						result.EllipticArcCount++;
						break;

					case esriGeometryType.esriGeometryBezier3Curve:
						result.BezierCount++;
						break;

					default:
						throw new ArgumentOutOfRangeException(
							$"Unexpected segment geometry type: {segment.GeometryType}");
				}
			}

			return result;
		}

		public static bool? IsClosed([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var curve = geometry as ICurve;
			return curve?.IsClosed;
		}

		public static bool IsMultipart([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			esriGeometryType geometryType = geometry.GeometryType;

			if (geometryType == esriGeometryType.esriGeometryPoint)
			{
				return false;
			}

			if (geometryType == esriGeometryType.esriGeometryPolygon)
			{
				var polygon = (IPolygon) geometry;

				return GetExteriorRingCount(polygon) > 1;
			}

			var geometryCollection = geometry as IGeometryCollection;
			return geometryCollection != null && geometryCollection.GeometryCount > 1;
		}

		public static int GetExteriorRingCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryPolygon:
					return GetExteriorRingCount((IPolygon) geometry);

				case esriGeometryType.esriGeometryMultiPatch:
					return GetExteriorRingCount((IMultiPatch) geometry);

				case esriGeometryType.esriGeometryRing:
					return ((IRing) geometry).IsExterior ? 1 : 0;

				default:
					return 0;
			}
		}

		public static int GetInteriorRingCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryPolygon:
					return GetInteriorRingCount((IPolygon) geometry);

				case esriGeometryType.esriGeometryMultiPatch:
					return GetInteriorRingCount((IMultiPatch) geometry);

				case esriGeometryType.esriGeometryRing:
					return ((IRing) geometry).IsExterior ? 0 : 1;

				default:
					return 0;
			}
		}

		public static int GetRingCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryMultiPatch:
				case esriGeometryType.esriGeometryPolygon:
					return GeometryUtils.GetParts((IGeometryCollection) geometry)
					                    .OfType<IRing>().Count();

				case esriGeometryType.esriGeometryRing:
					return 1;

				default:
					return 0;
			}
		}

		public static int GetTriangleStripCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryMultiPatch:
					return GeometryUtils.GetParts((IGeometryCollection) geometry)
					                    .OfType<ITriangleStrip>().Count();

				case esriGeometryType.esriGeometryTriangleStrip:
					return 1;

				default:
					return 0;
			}
		}

		public static int GetTriangleFanCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryMultiPatch:
					return GeometryUtils.GetParts((IGeometryCollection) geometry)
					                    .OfType<ITriangleFan>().Count();

				case esriGeometryType.esriGeometryTriangleFan:
					return 1;

				default:
					return 0;
			}
		}

		public static int GetTrianglesPatchCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryMultiPatch:
					return GeometryUtils.GetParts((IGeometryCollection) geometry)
					                    .OfType<ITriangles>().Count();

				case esriGeometryType.esriGeometryTriangles:
					return 1;

				default:
					return 0;
			}
		}

		private static int GetExteriorRingCount([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			return GeometryUtils.GetRings(polygon).Count(ring => ring.IsExterior);
		}

		private static int GetInteriorRingCount([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			return GeometryUtils.GetRings(polygon).Count(ring => ! ring.IsExterior);
		}

		private static int GetExteriorRingCount([NotNull] IMultiPatch multiPatch)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));

			var count = 0;

			foreach (IGeometry part in GeometryUtils.GetParts((IGeometryCollection) multiPatch)
			)
			{
				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				var isBeginningRing = false;
				esriMultiPatchRingType type = multiPatch.GetRingType(ring, ref isBeginningRing);

				if ((type & esriMultiPatchRingType.esriMultiPatchOuterRing) != 0)
				{
					count++;
				}
			}

			return count;
		}

		private static int GetInteriorRingCount([NotNull] IMultiPatch multiPatch)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));

			var count = 0;

			foreach (IGeometry part in GeometryUtils.GetParts((IGeometryCollection) multiPatch)
			)
			{
				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				var isBeginningRing = false;
				esriMultiPatchRingType type = multiPatch.GetRingType(ring, ref isBeginningRing);

				if ((type & esriMultiPatchRingType.esriMultiPatchInnerRing) != 0)
				{
					count++;
				}
			}

			return count;
		}
	}
}

using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeometryUtils
	{
		public static Coordinate2D Shifted(this Coordinate2D point, double dx, double dy)
		{
			return new Coordinate2D(point.X + dx, point.Y + dy);
		}

		public static int GetPointCount(Geometry geometry)
		{
			return geometry?.PointCount ?? 0;
		}

		public static Envelope Union(Envelope a, Envelope b)
		{
			if (a == null) return b;
			if (b == null) return a;
			return a.Union(b);
		}

		public static Polyline Boundary(Polygon polygon)
		{
			if (polygon == null) return null;

			return (Polyline) Engine.Boundary(polygon);
		}

		public static Polygon Intersection(Envelope extent, Polygon perimeter)
		{
			if (extent == null) return perimeter;
			if (perimeter == null) return GeometryFactory.CreatePolygon(extent);
			return GetClippedPolygon(perimeter, extent);
		}

		public static Geometry Intersection(Geometry a, Geometry b)
		{
			if (a == null) return b;
			if (b == null) return a;
			return Engine.Intersection(a, b);
		}

		public static T Generalize<T>(T geometry, double maxDeviation, bool removeDegenerateParts = false, bool preserveCurves = false) where T : Geometry
		{
			if (maxDeviation < double.Epsilon)
				return geometry;

			return (T) Engine.Generalize(geometry, maxDeviation, removeDegenerateParts,
			                             preserveCurves);
		}

		public static Polyline Simplify(Polyline polyline, SimplifyType simplifyType,
		                                bool forceSimplify = false)
		{
			if (polyline == null) return null;

			return Engine.SimplifyPolyline(polyline, simplifyType, forceSimplify);
		}

		public static T Simplify<T>(T geometry, bool forceSimplify = false) where T : Geometry
		{
			if (geometry == null) return null;

			return (T) Engine.SimplifyAsFeature(geometry, forceSimplify);
		}

		public static bool Contains(Geometry containing,
		                            Geometry contained,
		                            bool suppressIndexing = false)
		{
			if (containing == null) return false;
			if (contained == null) return true;

			if (! suppressIndexing)
			{
				Engine.AccelerateForRelationalOperations(containing);
				Engine.AccelerateForRelationalOperations(containing);
			}

			return Engine.Contains(containing, contained);
		}

		public static double GetDistanceAlongCurve(Multipart curve, MapPoint point)
		{
			Engine.QueryPointAndDistance(
				curve, SegmentExtension.NoExtension, point, AsRatioOrLength.AsLength,
				out double distanceAlong, out _, out _);
			return distanceAlong;
		}

		public static bool Disjoint([NotNull] Geometry geometry1,
		                            [NotNull] Geometry geometry2,
		                            bool suppressIndexing = false)
		{
			if (! suppressIndexing)
			{
				Engine.AccelerateForRelationalOperations(geometry1);
				Engine.AccelerateForRelationalOperations(geometry2);
			}

			return GeometryEngine.Instance.Disjoint(geometry1, geometry2);
		}

		public static Polygon GetClippedPolygon(Polygon polygon, Envelope clipExtent)
		{
			return (Polygon) Engine.Clip(polygon, clipExtent);
		}

		public static Polyline GetClippedPolyline(Polyline polyline, Envelope clipExtent)
		{
			return (Polyline) Engine.Clip(polyline, clipExtent);
		}

		public static T EnsureSpatialReference<T>(T geometry, SpatialReference spatialReference)
			where T : Geometry
		{
			// TODO: Compare first

			return (T) Engine.Project(geometry, spatialReference);
		}

		public static IGeometryEngine Engine
		{
			get => _engine ?? GeometryEngine.Instance;
			set => _engine = value;
		}

		private static IGeometryEngine _engine;

		public static GeometryType TranslateEsriGeometryType(esriGeometryType esriGeometryType)
		{
			switch (esriGeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return GeometryType.Point;
				case esriGeometryType.esriGeometryMultipoint:
					return GeometryType.Multipoint;
				case esriGeometryType.esriGeometryPolyline:
					return GeometryType.Polyline;
				case esriGeometryType.esriGeometryPolygon:
					return GeometryType.Polygon;
				case esriGeometryType.esriGeometryMultiPatch:
					return GeometryType.Multipatch;
				case esriGeometryType.esriGeometryEnvelope:
					return GeometryType.Envelope;
				case esriGeometryType.esriGeometryBag:
					return GeometryType.GeometryBag;
				case esriGeometryType.esriGeometryAny:
					return GeometryType.Unknown;
				default:
					throw new ArgumentOutOfRangeException($"Cannot translate {esriGeometryType}");
			}
		}
	}
}

using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Spatial
{
	[CLSCompliant(false)]
	public static class GeometryUtils
	{
		public static IReadOnlyList<Coordinate3D> GetCoordinates(
			ArcGIS.Core.Geometry.Geometry geometry, GeometryType type)
		{
			switch (type)
			{
				case GeometryType.Point:
					return new[] {((MapPoint) geometry).Coordinate3D};
				case GeometryType.Multipoint:
					return ((Multipoint) geometry).Copy3DCoordinatesToList();
				case GeometryType.Polyline:
				case GeometryType.Polygon:
					return ((Multipart) geometry).Copy3DCoordinatesToList();
				case GeometryType.Envelope:
				case GeometryType.Unknown:
				case GeometryType.Multipatch:
				case GeometryType.GeometryBag:
					throw new NotImplementedException();
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		public static GeometryType GetGeometryType(FeatureClass featureClass)
		{
			return featureClass.GetDefinition().GetShapeType();
		}

		public static SpatialReference CreateSpatialReference(int srid)
		{
			return SpatialReferenceBuilder.CreateSpatialReference(srid);
		}

		public static MapPoint CreatePoint(double x, double y, SpatialReference sref = null)
		{
			return MapPointBuilder.CreateMapPoint(x, y, sref);
		}

		public static Envelope Union(Envelope first, Envelope second)
		{
			return first.Union(second);
		}
	}
}

using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AG.Spatial
{
	[CLSCompliant(false)]
	public static class GeometryUtils
	{
		public static IReadOnlyList<Coordinate3D> GetCoordinates(
			Geometry geometry, GeometryType type)
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
	}
}

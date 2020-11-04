using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using NUnit.Framework;

namespace ProSuite.AGP.WorkList.Test
{
	public class PolygonConstruction
	{
		private readonly IList<MapPoint> _points;
		private static SpatialReference _sref;

		public PolygonConstruction(IList<MapPoint> points)
		{
			_points = points;
		}

		public static PolygonConstruction StartPolygon(double x, double y, double z = double.NaN)
		{
			_sref = SpatialReferenceBuilder.CreateSpatialReference(2056, 5729);
			return new PolygonConstruction(new List<MapPoint> {CreateMapPoint(x, y, z)});
		}

		public static MapPoint CreateMapPoint(double x, double y)
		{
			return MapPointBuilder.CreateMapPoint(x, y, _sref);
		}

		public static MapPoint CreateMapPoint(double x, double y, double z)
		{
			return MapPointBuilder.CreateMapPoint(x, y, z, _sref);
		}

		public PolygonConstruction LineTo(double x, double y, double z = double.NaN)
		{
			_points.Add(CreateMapPoint(x, y, z));
			return this;
		}

		public Polygon ClosePolygon()
		{
			Polygon polygon = PolygonBuilder.CreatePolygon(_points);

			// todo daro: remove when equality is assured
			// CreatePolygon() should close the polygon. Simplify it to check equality
			Geometry simplifiedGeometry = GeometryEngine.Instance.SimplifyAsFeature(polygon);
			Assert.True(simplifiedGeometry.IsEqual(polygon));

			return polygon;
		}
	}
}

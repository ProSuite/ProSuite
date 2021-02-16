namespace ProSuite.Commons.Geometry.Wkb
{
	/// <summary>
	/// The WKB geometry types as defined in specification 1.2.1
	/// </summary>
	public enum WkbGeometryType
	{
		Point = 1,
		LineString = 2,
		Polygon = 3,
		MultiPoint = 4,
		MultiLineString = 5,
		MultiPolygon = 6,
		GeometryCollection = 7,
		CircularString = 8,
		CompoundCurve = 9,
		CurvePolygon = 10,
		MultiCurve = 11,
		MultiSurface = 12,
		Curve = 13,
		Surface = 14,
		PolyhedralSurface = 15,
		Tin = 16,
		Triangle = 17
	}
}

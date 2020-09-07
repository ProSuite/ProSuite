namespace ProSuite.Commons.Geometry.Wkb
{
	/// <summary>
	/// The basic WKB geometry types as defined in specification 1.1
	/// </summary>
	public enum WkbGeometryType
	{
		Point = 1,
		LineString = 2,
		Polygon = 3,
		MultiPoint = 4,
		MultiLineString = 5,
		MultiPolygon = 6,
		GeometryCollection = 7
	}
}

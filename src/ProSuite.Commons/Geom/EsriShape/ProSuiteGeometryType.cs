namespace ProSuite.Commons.Geom.EsriShape
{
	/// <summary>
	/// The type of geometry in the wider sense. A superset of the classic geometry types.
	/// The enum values corresponds with the esriGeometryType where applicable, i.e. for
	/// the classic geometry types below 30.
	/// </summary>
	public enum ProSuiteGeometryType
	{
		// Unknown is different from no geometry
		Unknown = -1,

		// Also used for tables without geometry
		Null = 0,

		Point = 1,
		Multipoint = 2,
		Polyline = 3,
		Polygon = 4,
		MultiPatch = 9,

		Envelope = 5,

		Path = 6,
		Ring = 11,

		Any = 7,

		Line = 13,
		CircularArc = 14,
		Bezier3Curve = 15,
		EllipticArc = 16,

		Bag = 17,

		TriangleStrip = 18,
		TriangleFan = 19,
		Ray = 20,
		Sphere = 21,
		Triangles = 22,

		// Extended geometry types:
		Raster = 32,
		RasterMosaic = 33,
		Terrain = 34,
		Topology = 35
	}
}

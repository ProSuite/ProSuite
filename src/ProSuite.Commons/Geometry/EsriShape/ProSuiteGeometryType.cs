namespace ProSuite.Commons.Geometry.EsriShape
{
	/// <summary>
	/// A geometry type enum that corresponds with the esriGeometryType.
	/// </summary>
	public enum ProSuiteGeometryType
	{
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
		Triangles = 22
	}
}

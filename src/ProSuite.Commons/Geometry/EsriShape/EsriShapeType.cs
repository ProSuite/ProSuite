namespace ProSuite.Commons.Geometry.EsriShape
{
	/// <summary>
	/// Shape types used by the Esri shape buffer format, corresponding to to the ArcObjects enum.
	/// </summary>
	public enum EsriShapeType
	{
		EsriShapeNull = 0,
		EsriShapePoint = 1,
		EsriShapePointM = 21,
		EsriShapePointZM = 11,
		EsriShapePointZ = 9,
		EsriShapeMultipoint = 8,
		EsriShapeMultipointM = 28,
		EsriShapeMultipointZM = 18,
		EsriShapeMultipointZ = 20,
		EsriShapePolyline = 3,
		EsriShapePolylineM = 23,
		EsriShapePolylineZM = 13,
		EsriShapePolylineZ = 10,
		EsriShapePolygon = 5,
		EsriShapePolygonM = 25,
		EsriShapePolygonZM = 15,
		EsriShapePolygonZ = 19,
		EsriShapeMultiPatchM = 31,
		EsriShapeMultiPatch = 32,
		EsriShapeGeneralPolyline = 50,
		EsriShapeGeneralPolygon = 51,
		EsriShapeGeneralPoint = 52,
		EsriShapeGeneralMultipoint = 53,
		EsriShapeGeneralMultiPatch = 54,
		EsriShapeTypeLast = 55
	}
}

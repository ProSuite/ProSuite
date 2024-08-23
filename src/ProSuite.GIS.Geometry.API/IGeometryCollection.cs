namespace ESRI.ArcGIS.Geometry
{
	public interface IGeometryCollection
	{
		int GeometryCount { get; }

		IGeometry get_Geometry(int index);
	}
}

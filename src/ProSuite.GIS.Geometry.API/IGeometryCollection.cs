namespace ProSuite.GIS.Geometry.API
{
	public interface IGeometryCollection
	{
		int GeometryCount { get; }

		IGeometry get_Geometry(int index);
	}
}

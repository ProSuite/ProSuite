namespace ProSuite.GIS.Geometry.API
{
	public interface IMultiPatch : IGeometry, IGeometryCollection
	{
		IGeometry XYFootprint { get; }

		void InvalXYFootprint();
	}
}

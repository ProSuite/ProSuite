using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public interface IMultiPatch : IGeometry, IGeometryCollection
	{
		IGeometry XYFootprint { get; }

		void InvalXYFootprint();
	}
}

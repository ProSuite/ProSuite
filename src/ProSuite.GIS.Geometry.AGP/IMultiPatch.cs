using ESRI.ArcGIS.Geometry;

namespace ProSuite.GIS.Geometry.AGP
{
	public interface IMultiPatch : IGeometry, IGeometryCollection
	{
		IGeometry XYFootprint { get; }

		void InvalXYFootprint();
	}
}

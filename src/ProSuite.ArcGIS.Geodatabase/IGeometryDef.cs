using ESRI.ArcGIS.Geometry;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IGeometryDef
	{
		int AvgNumPoints { get; }

		esriGeometryType GeometryType { get; }

		double get_GridSize(int index);

		int GridCount { get; }

		ISpatialReference SpatialReference { get; }

		bool HasZ { get; }

		bool HasM { get; }
	}
}

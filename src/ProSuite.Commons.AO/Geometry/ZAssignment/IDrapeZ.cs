using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	/// <summary>
	/// Interface for draping (addding more points to) the geometry 
	/// based on the surface when z-difference is more than tolerance
	/// </summary>
	public interface IDrapeZ
	{
		T DrapeGeometry<T>(ISurface surface, T geometry, double tolerance)
			where T : IGeometry;
	}
}

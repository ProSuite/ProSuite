using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	/// <summary>
	/// Interface for draping (addding more points to) the geometry 
	/// based on the surface when z-difference is more than tolerance
	/// </summary>
	[CLSCompliant(false)]
	public interface IDrapeZ
	{
		[CLSCompliant(false)]
		T DrapeGeometry<T>(ISurface surface, T geometry, double tolerance)
			where T : IGeometry;
	}
}

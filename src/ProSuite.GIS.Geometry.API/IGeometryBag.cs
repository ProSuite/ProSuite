using System.Collections.Generic;

namespace ProSuite.GIS.Geometry.API
{
	public interface IGeometryBag : IGeometry
	{
		IReadOnlyCollection<IGeometry> Geometries { get; }
	}
}
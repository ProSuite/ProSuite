using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IHasGeotransformation
	{
		T ProjectEx<T>([NotNull] T geometry) where T : IGeometry;

		bool Equals(IHasGeotransformation other);
	}
}

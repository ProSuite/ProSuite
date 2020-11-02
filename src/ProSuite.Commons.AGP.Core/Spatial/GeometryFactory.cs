using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeometryFactory
	{
		[NotNull]
		public static T Clone<T>([NotNull] T prototype) where T : Geometry
		{
			return (T) prototype.Clone();
		}
	}
}

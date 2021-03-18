using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public interface IRasterDatasetProvider
	{
		IPolygon GetInterpolationDomain();

		[CanBeNull]
		ISimpleRaster GetSimpleRaster(double atX, double atY);
	}
}

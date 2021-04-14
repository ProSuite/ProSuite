using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public interface IRasterProvider : IDisposable
	{
		IPolygon GetInterpolationDomain();

		[CanBeNull]
		ISimpleRaster GetSimpleRaster(double atX, double atY);
	}
}

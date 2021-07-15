using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public interface IRasterProvider : IDisposable
	{
		IPolygon GetInterpolationDomain();

		[CanBeNull]
		ISimpleRaster GetSimpleRaster(double atX, double atY);

		IEnumerable<ISimpleRaster> GetSimpleRasters([NotNull] IEnvelope envelope);
	}
}

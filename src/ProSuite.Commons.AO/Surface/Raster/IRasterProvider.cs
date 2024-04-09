using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public interface IRasterProvider : IDisposable
	{
		[NotNull]
		IPolygon GetInterpolationDomain();

		[CanBeNull]
		ISimpleRaster GetSimpleRaster(double atX, double atY);

		/// <summary>
		/// Gets the simple rasters that intersect the specified envelope. If the envelope is null,
		/// all rasters are returned.
		/// </summary>
		/// <param name="envelope"></param>
		/// <returns></returns>
		IEnumerable<ISimpleRaster> GetSimpleRasters([CanBeNull] IEnvelope envelope);
	}
}

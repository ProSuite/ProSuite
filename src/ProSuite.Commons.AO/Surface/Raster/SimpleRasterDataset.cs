using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public class SimpleRasterDataset : IRasterProvider
	{
		[NotNull] private readonly IRaster _raster;
		[NotNull] private readonly IPolygon _interpolationDomain;
		[CanBeNull] private readonly IDisposable _disposableParentDataset;

		public SimpleRasterDataset([NotNull] IRaster raster,
		                           [NotNull] IPolygon interpolationDomain,
		                           [CanBeNull] IDisposable disposableParentDataset = null)
		{
			_raster = raster;
			_interpolationDomain = interpolationDomain;
			_disposableParentDataset = disposableParentDataset;
		}

		public IPolygon GetInterpolationDomain()
		{
			return _interpolationDomain;
		}

		public ISimpleRaster GetSimpleRaster(double atX, double atY)
		{
			return new SimpleAoRaster(_raster);
		}

		public IEnumerable<ISimpleRaster> GetSimpleRasters(double x, double y,
		                                                   double searchTolerance)
		{
			yield return new SimpleAoRaster(_raster);
		}

		public void Dispose()
		{
			_disposableParentDataset?.Dispose();
		}
	}
}

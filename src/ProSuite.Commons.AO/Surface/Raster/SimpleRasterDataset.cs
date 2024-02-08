#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public class SimpleRasterDataset : IRasterProvider
	{
		[NotNull] private readonly IRaster _raster;
		[NotNull] private readonly IPolygon _interpolationDomain;
		[CanBeNull] private readonly IDisposable _disposableParentDataset;

		/// <summary>
		/// Encapsulates a simple raster.
		/// </summary>
		/// <param name="raster"></param>
		/// <param name="interpolationDomain">The interpolation domain in which valid raster values
		/// can be expected. If not specified, the raster extent minus half a cell size is used.
		/// </param>
		/// <param name="disposableParentDataset"></param>
		public SimpleRasterDataset([NotNull] IRaster raster,
		                           [CanBeNull] IPolygon interpolationDomain = null,
		                           [CanBeNull] IDisposable disposableParentDataset = null)
		{
			_raster = raster;
			_interpolationDomain = interpolationDomain ?? GetAssumedInterpolationDomain(raster);
			_disposableParentDataset = disposableParentDataset;
		}

		public IPolygon GetInterpolationDomain()
		{
			return _interpolationDomain;
		}

		public ISimpleRaster GetSimpleRaster(double atX, double atY)
		{
			return GeometryUtils.Intersects(
				       _interpolationDomain,
				       new PointClass
				       {
					       X = atX, Y = atY,
					       SpatialReference = _interpolationDomain.SpatialReference
				       })
				       ? new SimpleAoRaster(_raster)
				       : null;
		}

		public IEnumerable<ISimpleRaster> GetSimpleRasters(IEnvelope envelope)
		{
			if (envelope == null ||
			    GeometryUtils.Intersects(_interpolationDomain, envelope))
			{
				yield return new SimpleAoRaster(_raster);
			}
		}

		public void Dispose()
		{
			_disposableParentDataset?.Dispose();
		}

		[NotNull]
		private IPolygon GetAssumedInterpolationDomain([NotNull] IRaster raster)
		{
			IEnvelope extent = ((IGeoDataset) raster).Extent;

			var rasterProps = (IRasterProps) raster;

			IPnt cellSize = rasterProps.MeanCellSize();

			double halfCellX = Math.Abs(cellSize.X) / 2;
			double halfCellY = Math.Abs(cellSize.Y) / 2;

			extent.Expand(-halfCellX, -halfCellY, false);

			return GeometryFactory.CreatePolygon(extent);
		}
	}
}

#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ProSuite.QA.Container;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Surface
{
	public class RasterDatasetReference : RasterReference
	{
		[NotNull] private readonly IRasterDataset2 _rasterDataset;
		[CanBeNull] private IRaster _fullRaster;

		public RasterDatasetReference([NotNull] IRasterDataset2 rasterDataset)
		{
			Assert.ArgumentNotNull(rasterDataset, nameof(rasterDataset));

			_rasterDataset = rasterDataset;
		}

		public override IDataset Dataset => (IDataset) _rasterDataset;
		public override IGeoDataset GeoDataset => (IGeoDataset) _rasterDataset;
		public override double CellSize => RasterUtils.GetMeanCellSize(FullRaster);

		private IRaster FullRaster =>
			_fullRaster
			?? (_fullRaster = _rasterDataset.CreateFullRaster());

		public override ISimpleSurface CreateSurface(IEnvelope extent)
		{
			IDataset memoryRasterDataset;

			IRaster clipped =
				RasterUtils.GetClippedRaster(FullRaster, extent, out memoryRasterDataset);

			IDataset disposableDataset = memoryRasterDataset;

			IDisposable disposableCallback =
				new DisposableCallback(
					() => RasterUtils.ReleaseMemoryRasterDataset(disposableDataset));

			var simpleRasterDataset = new SimpleRasterDataset(
				clipped, GeometryFactory.CreatePolygon(extent), disposableCallback);

			ISpatialReference spatialReference = ((IGeoDataset) FullRaster).SpatialReference;

			return new SimpleRasterSurface(simpleRasterDataset, spatialReference);
		}

		public override bool EqualsCore(RasterReference rasterReference)
		{
			var other = rasterReference as RasterDatasetReference;

			return other != null && _rasterDataset == other._rasterDataset;
		}

		public override int GetHashCodeCore()
		{
			return _rasterDataset.GetHashCode();
		}
	}
}

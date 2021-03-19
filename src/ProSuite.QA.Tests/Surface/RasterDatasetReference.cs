using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

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
		public override IRasterProps RasterProps => (IRasterProps) FullRaster;

		private IRaster FullRaster =>
			_fullRaster
			?? (_fullRaster = _rasterDataset.CreateFullRaster());

		public override ISimpleSurface CreateSurface(IEnvelope extent,
		                                             out IDataset memoryRasterDataset)
		{
			IRaster clipped =
				RasterUtils.GetClippedRaster(FullRaster, extent, out memoryRasterDataset);

			var rasterSurface = new RasterSurface();
			rasterSurface.PutRaster(clipped, 0);

			return rasterSurface;
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

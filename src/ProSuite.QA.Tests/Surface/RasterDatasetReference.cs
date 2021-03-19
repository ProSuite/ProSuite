using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Surface
{
	public class RasterDatasetReference : RasterReference
	{
		[NotNull] private readonly IRasterDataset2 _rasterDataset;

		public RasterDatasetReference([NotNull] IRasterDataset2 rasterDataset)
		{
			Assert.ArgumentNotNull(rasterDataset, nameof(rasterDataset));

			_rasterDataset = rasterDataset;
		}

		public override ISimpleSurface CreateSurface(IRaster raster)
		{
			var rasterSurface = new RasterSurface();

			rasterSurface.PutRaster(raster, 0);

			return rasterSurface;
		}

		public override IDataset RasterDataset => (IDataset) _rasterDataset;

		public override IRaster CreateFullRaster()
		{
			return _rasterDataset.CreateFullRaster();
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

using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Surface
{
	public class MosaicRasterReference : RasterReference, IMosaicRasterDatasetDef
	{
		[NotNull] private readonly SimpleRasterMosaic _simpleRasterMosaic;

		public MosaicRasterReference([NotNull] SimpleRasterMosaic simpleRasterMosaic)
		{
			Assert.ArgumentNotNull(simpleRasterMosaic, nameof(simpleRasterMosaic));

			_simpleRasterMosaic = simpleRasterMosaic;
		}

		public override IReadOnlyDataset Dataset => _simpleRasterMosaic;

		public override IReadOnlyGeoDataset GeoDataset => _simpleRasterMosaic;

		public override double CellSize => _simpleRasterMosaic.GetCellSize();

		public override bool AssumeInMemory => false;

		public override DatasetType DatasetType => DatasetType.RasterMosaic;
		
		public override ISimpleSurface CreateSurface(IEnvelope extent,
		                                             double? defaultValueForUnassignedZs = null,
		                                             UnassignedZValueHandling? unassignedZValueHandling = null)
		{
			var surface = new SimpleRasterSurface(_simpleRasterMosaic);

			if (defaultValueForUnassignedZs.HasValue)
			{
				surface.DefaultValueForUnassignedZs = defaultValueForUnassignedZs.Value;
			}

			if (unassignedZValueHandling.HasValue)
			{
				surface.UnassignedZValueHandling = unassignedZValueHandling.Value;
			}

			return surface;
		}

		public override bool EqualsCore(RasterReference rasterReference)
		{
			var other = rasterReference as MosaicRasterReference;

			if (other == null)
			{
				return false;
			}

			return _simpleRasterMosaic.Equals(other._simpleRasterMosaic);
		}

		public override int GetHashCodeCore()
		{
			return _simpleRasterMosaic.GetHashCode();
		}
	}
}

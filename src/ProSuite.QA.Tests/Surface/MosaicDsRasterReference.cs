using System;
using System.Linq;
using System.Text;
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
	public class MosaicDsRasterReference : RasterReference
	{
		[NotNull] private readonly IMosaicDataset _mosaicDataset;
		[NotNull] private readonly byte[] _mosaicDefinitionBytes;
		[NotNull] private readonly IDataset _rasterDataset;

		[CanBeNull] private IRaster _defaultRaster;

		public MosaicDsRasterReference([NotNull] IMosaicDataset mosaicDataset)
		{
			Assert.ArgumentNotNull(mosaicDataset, nameof(mosaicDataset));

			_mosaicDataset = mosaicDataset;

			_mosaicDefinitionBytes = GetMosaicDefinitionBytes(mosaicDataset);

			// ReSharper disable once SuspiciousTypeConversion.Global
			_rasterDataset = (IDataset)_mosaicDataset;
		}

		public override IDataset Dataset => _rasterDataset;
		public override IGeoDataset GeoDataset => (IGeoDataset) _rasterDataset;

		public override IRasterProps RasterProps => (IRasterProps) DefaultRaster;

		private IRaster DefaultRaster =>
			_defaultRaster
			?? (_defaultRaster = ((IMosaicDataset3)_rasterDataset).GetRaster(string.Empty)); // TODO

		public override ISimpleSurface CreateSurface(IEnvelope extent,
		                                             out IDataset memoryRasterDataset)
		{
			IRaster clipped =
				RasterUtils.GetClippedRaster(DefaultRaster, extent, out memoryRasterDataset);

			var rasterSurface = new RasterSurface();
			rasterSurface.PutRaster(clipped, 0);

			return rasterSurface;
		}

		public override bool EqualsCore(RasterReference rasterReference)
		{
			var other = rasterReference as MosaicDsRasterReference;

			if (other == null)
			{
				return false;
			}

			return _rasterDataset == other._rasterDataset &&
			       _mosaicDefinitionBytes.SequenceEqual(other._mosaicDefinitionBytes);
		}

		public override int GetHashCodeCore()
		{
			return _rasterDataset.GetHashCode();
		}

		[NotNull]
		private static byte[] GetMosaicDefinitionBytes([NotNull] IMosaicDataset mosaicLayer)
		{
			return Encoding.UTF8.GetBytes(((IDataset)mosaicLayer).Name);
			//IImageServerLayer previewLayer = mosaicLayer.PreviewLayer;

			//IImageServiceInfo serviceInfo = previewLayer?.ServiceInfo;

			//return serviceInfo != null
			//	       ? SerializationUtils.SerializeComObject(serviceInfo)
			//	       : new byte[] { };
		}
	}
}

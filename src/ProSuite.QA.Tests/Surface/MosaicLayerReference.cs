using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Surface
{
	public class MosaicLayerReference : RasterReference
	{
		[NotNull] private readonly MosaicLayerDefinition _mosaicLayer;
		[NotNull] private readonly byte[] _mosaicDefinitionBytes;
		[NotNull] private readonly IDataset _rasterDataset;

		public MosaicLayerReference([NotNull] MosaicLayerDefinition mosaicLayer)
		{
			Assert.ArgumentNotNull(mosaicLayer, nameof(mosaicLayer));

			_mosaicLayer = mosaicLayer;

			_mosaicDefinitionBytes = GetMosaicDefinitionBytes(mosaicLayer);

			// ReSharper disable once SuspiciousTypeConversion.Global
			_rasterDataset = (IDataset) _mosaicLayer.MosaicDataset;
		}

		public override ISimpleSurface CreateSurface(IRaster raster)
		{
			var rasterSurface = new RasterSurface();

			rasterSurface.PutRaster(raster, 0);

			return rasterSurface;
		}

		public override IDataset RasterDataset => _rasterDataset;

		public override IRaster CreateFullRaster()
		{
			return _mosaicLayer.MosaicDataset.CreateDefaultRaster();
		}

		public override bool EqualsCore(RasterReference rasterReference)
		{
			var other = rasterReference as MosaicLayerReference;

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
		private static byte[] GetMosaicDefinitionBytes([NotNull] MosaicLayerDefinition mosaicLayer)
		{
			return Encoding.UTF8.GetBytes(mosaicLayer.DefinitionString);
			//IImageServerLayer previewLayer = mosaicLayer.PreviewLayer;

			//IImageServiceInfo serviceInfo = previewLayer?.ServiceInfo;

			//return serviceInfo != null
			//	       ? SerializationUtils.SerializeComObject(serviceInfo)
			//	       : new byte[] { };
		}
	}
}

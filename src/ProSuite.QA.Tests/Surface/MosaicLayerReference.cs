using System;
using System.Linq;
using System.Text;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Surface
{
	public class MosaicRasterReference : RasterReference
	{
		[NotNull] private readonly SimpleRasterMosaic _simpleRasterMosaic;
		[NotNull] private readonly byte[] _mosaicDefinitionBytes;

		public MosaicRasterReference([NotNull] SimpleRasterMosaic simpleRasterMosaic)
		{
			Assert.ArgumentNotNull(simpleRasterMosaic, nameof(simpleRasterMosaic));

			_simpleRasterMosaic = simpleRasterMosaic;

			_mosaicDefinitionBytes = GetMosaicDefinitionBytes(simpleRasterMosaic);
		}

		public override IDataset Dataset => throw new NotImplementedException();
		public override IGeoDataset GeoDataset => throw new NotImplementedException();

		public override IRasterProps RasterProps => throw new NotImplementedException();

		public override ISimpleSurface CreateSurface(IEnvelope extent,
		                                             out IDataset memoryRasterDataset)
		{
			throw new NotImplementedException();
		}

		public override bool EqualsCore(RasterReference rasterReference)
		{
			var other = rasterReference as MosaicRasterReference;

			if (other == null)
			{
				return false;
			}

			return _simpleRasterMosaic == other._simpleRasterMosaic && // TODO
			       _mosaicDefinitionBytes.SequenceEqual(other._mosaicDefinitionBytes);
		}

		public override int GetHashCodeCore()
		{
			return _simpleRasterMosaic.GetHashCode(); // TODO
		}

		[NotNull]
		private static byte[] GetMosaicDefinitionBytes([NotNull] SimpleRasterMosaic raster)
		{
			return Encoding.UTF8.GetBytes("TODO"); // TODO
			//IImageServerLayer previewLayer = mosaicLayer.PreviewLayer;

			//IImageServiceInfo serviceInfo = previewLayer?.ServiceInfo;

			//return serviceInfo != null
			//	       ? SerializationUtils.SerializeComObject(serviceInfo)
			//	       : new byte[] { };
		}
	}
}

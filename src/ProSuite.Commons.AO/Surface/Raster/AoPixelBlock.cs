using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public class AoPixelBlock<T> : ISimplePixelBlock<T>, IDisposable
	{
		private readonly int _rasterBandIndex;
		private readonly IPixelBlock3 _pixelBlock;

		public AoPixelBlock(IPixelBlock pixelBlock,
		                    int rasterBandIndex = 0)
		{
			_rasterBandIndex = rasterBandIndex;
			_pixelBlock = (IPixelBlock3) pixelBlock;
		}

		public IPixelBlock PixelBlock => (IPixelBlock) _pixelBlock;

		public IEnumerable<T> AllPixels()
		{
			// Keep the array around?
			Array pixels = (Array) _pixelBlock.PixelData[_rasterBandIndex];

			for (int i = 0; i < Width; i++)
			{
				for (int j = 0; j < Height; j++)
				{
					yield return (T) pixels.GetValue(i, j);
				}
			}
		}

		public int Width => _pixelBlock.Width;
		public int Height => _pixelBlock.Height;

		public T GetValue(int column, int row)
		{
			Array pixels = (Array) _pixelBlock.PixelData[_rasterBandIndex];
			return (T) pixels.GetValue(column, row);
		}

		public void Dispose()
		{
			Marshal.ReleaseComObject(_pixelBlock);
		}
	}
}

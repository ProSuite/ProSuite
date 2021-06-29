#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using IPnt = ESRI.ArcGIS.Geodatabase.IPnt;

namespace ProSuite.Commons.AO.Surface.Raster
{
	/// <summary>
	/// A 32-bit (float) single band raster based on a raster file.
	/// </summary>
	public class SimpleAoRaster : ISimpleRaster
	{
		private readonly IRaster _raster;

		private const int _bandCount = 1;
		private const rstPixelType _pixelType = rstPixelType.PT_FLOAT;

		public SimpleAoRaster(string filePath) : this(OpenRaster(filePath))
		{
			Path = filePath;
		}

		public SimpleAoRaster(IRaster raster)
		{
			_raster = raster;

			// GDAL pixel to georeferenced space relationship:
			// The pixel/line coordinates are from (0.0,0.0) at the top left corner of the top left pixel
			// to (width_in_pixels,height_in_pixels) at the bottom right corner of the bottom right pixel.
			// The pixel/line location of the center of the top left pixel would therefore be (0.5,0.5).

			IEnvelope extent = ((IGeoDataset) raster).Extent;
			OriginX = extent.UpperLeft.X;
			OriginY = extent.UpperLeft.Y;

			var rasterProperties = (IRasterProps) _raster;
			PixelSizeX = rasterProperties.MeanCellSize().X;

			// The math looks better when using -1 because the origin is the top left corner
			// This is consistent with GDAL
			PixelSizeY = -1 * rasterProperties.MeanCellSize().Y;

			Width = rasterProperties.Width;
			Height = rasterProperties.Height;

			NoDataValue = RasterUtils.GetNoDataValue(rasterProperties);
		}

		public object NoDataValue { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		/// <summary>
		/// The X coordinate of the origin (i.e. top left of the raster extent) in georeferenced space.
		/// </summary>
		public double OriginX { get; }

		/// <summary>
		/// The Y coordinate of the origin (i.e. top left of the raster extent) in georeferenced space.
		/// </summary>
		public double OriginY { get; }

		/// <summary>
		/// The east-west pixel resolution / cell size
		/// </summary>
		public double PixelSizeX { get; }

		/// <summary>
		/// The north-south pixel resolution / cell size
		/// </summary>
		public double PixelSizeY { get; }

		[CanBeNull]
		public string Path { get; }

		public ISimplePixelBlock<T> CreatePixelBlock<T>(int bufferSizeX, int bufferSizeY)
		{
			Assert.ArgumentCondition(typeof(T) == typeof(float),
			                         "Unsupported raster pixel value type. Only float is supported.");

			IPixelBlock pixelBlock = new PixelBlockClass();
			((IPixelBlock4) pixelBlock).Create(_bandCount, bufferSizeX, bufferSizeY, _pixelType);

			return new AoPixelBlock<T>(pixelBlock);
		}

		public void ReadPixelBlock<T>(int pixelOffsetX, int pixelOffsetY,
		                              ISimplePixelBlock<T> simplePixelBlock,
		                              int nPixelSpace = 0, int nLineSpace = 0)
		{
			AoPixelBlock<T> aoPixelBlock = (AoPixelBlock<T>) simplePixelBlock;

			IPixelBlock pixelBlock = aoPixelBlock.PixelBlock;

			IPnt topLeftColumn = new PntClass();
			topLeftColumn.SetCoords(pixelOffsetX, pixelOffsetY);

			// The memory increase is pretty dramatic...
			_raster.Read(topLeftColumn, pixelBlock);

			//... if the raster ist not flushed! But even with flushing there is a steady increase.
			((IRawBlocks) _raster).Flush();
		}

		public void Dispose()
		{
			if (_raster != null)
			{
				Marshal.ReleaseComObject(_raster);
			}
		}

		public EnvelopeXY GetEnvelope()
		{
			var rasterProperties = (IRasterProps) _raster;
			IEnvelope envelope = rasterProperties.Extent;

			// NOTE: YMin and YMax are inverted (probably due to GDAL)
			double yMin = Math.Min(envelope.YMin, envelope.YMax);
			double yMax = Math.Max(envelope.YMin, envelope.YMax);

			return new EnvelopeXY(envelope.XMin, yMin, envelope.XMax, yMax);
		}

		private static IRaster OpenRaster(string filePath)
		{
			IRasterDataset2 rasterDataset =
				(IRasterDataset2) DatasetUtils.OpenRasterDataset(filePath);

			return rasterDataset.CreateFullRaster();
		}
	}
}

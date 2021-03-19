using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Surface.Raster
{
	/// <summary>
	/// Simple surface implementation for a set of raster file. The appropriate raster
	/// to be used at a specific location is provided by the raster dataset provider. The
	/// raster datasets are cached.
	/// </summary>
	public class SimpleRasterSurface : ISimpleSurface
	{
		private readonly IRasterDatasetProvider _rasterDatasetProvider;

		public SimpleRasterSurface(IRasterDatasetProvider rasterDatasetProvider)
		{
			_rasterDatasetProvider = rasterDatasetProvider;
		}

		#region ISimpleSurface members

		IRaster ISimpleSurface.AsRaster() => throw new NotImplementedException();
		public void Dispose()
		{
			// TODO:
			// foreach rasterDataset in openRasters -> close
		}

		public IPolygon GetDomain()
		{
			return _rasterDatasetProvider.GetInterpolationDomain();
		}

		public double GetZ(double x, double y)
		{
			ISimpleRaster simpleRaster = _rasterDatasetProvider.GetSimpleRaster(x, y);

			if (simpleRaster == null)
			{
				return double.NaN;
			}

			// Credit: GDAL and Wikipedia
			// Convert x,y to raster space (pixel coordinates)
			double pxlX = (x - simpleRaster.OriginX) / simpleRaster.PixelSizeX;
			double pxlY = (y - simpleRaster.OriginY) / simpleRaster.PixelSizeY;

			// Convert from upper left corner of pixel coordinates to center of
			// pixel coordinates:
			double dfX = pxlX - 0.5;
			double dfY = pxlY - 0.5;
			int dX = (int) Math.Floor(dfX);
			int dY = (int) Math.Floor(dfY);

			// position in the (0, 0) - (1, 1) coordinate system
			double dfDeltaX = dfX - dX;
			double dfDeltaY = dfY - dY;

			// TODO: Load neighbouring raster dataset if necessary
			if (! (dX >= 0 && dY >= 0 &&
			       dX + 2 <= simpleRaster.Width && dY + 2 <= simpleRaster.Height))
			{
				throw new NotImplementedException("Raster boundary not yet supported");
			}

			// Read all 4 pixels
			ISimplePixelBlock<float> pixels = simpleRaster.CreatePixelBlock<float>(2, 2);
			simpleRaster.ReadPixelBlock(dX, dY, pixels);

			if (HasNoDataValues(pixels, (float) simpleRaster.NoDataValue))
			{
				return double.NaN;
			}

			// Bi-linear interpolation
			double dfDeltaX1 = 1.0 - dfDeltaX;
			double dfDeltaY1 = 1.0 - dfDeltaY;

			// See Wikipedia, section Unit square, simplified using distributive law
			// (f(0,0) * (1-x) + f(1,0) * x) * (1-y) +
			// (f(0,1) * (1-x) + f(1,1) * x ) * y
			// as it's also done by GDAL

			float v00 = pixels.GetValue(0, 0);
			float v10 = pixels.GetValue(1, 0);
			float v01 = pixels.GetValue(0, 1);
			float v11 = pixels.GetValue(1, 1);

			double value =
				(v00 * dfDeltaX1 + v10 * dfDeltaX) * dfDeltaY1 +
				(v01 * dfDeltaX1 + v11 * dfDeltaX) * dfDeltaY;

			return value;
		}

		private static bool HasNoDataValues(ISimplePixelBlock<float> pixelBlock,
		                                    float noDataValue)
		{
			foreach (float v in pixelBlock.AllPixels())
			{
				if (MathUtils.AreEqual(v, noDataValue))
				{
					return true;
				}
			}

			return false;
		}

		public IGeometry Drape(IGeometry shape, double densifyDistance = double.NaN)
		{
			throw new NotImplementedException();
		}

		public IGeometry SetShapeVerticesZ(IGeometry shape)
		{
			throw new NotImplementedException();
		}

		public ITin AsTin(IEnvelope extent = null)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}

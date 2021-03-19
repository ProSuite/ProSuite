using System;
using System.Runtime.InteropServices;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.QA.Tests.Surface
{
	internal class RasterSurface : ISimpleSurface
	{
		private IRaster _raster;
		private bool _isNoData;
		private ISurface _surface;

		public RasterSurface()
		{
		}

		public void PutRaster(IRaster raster, int bandIndex, double origCellSize = -1)
		{
			_raster = raster;

			IRasterProps props = (IRasterProps)raster;
			IPnt cellSize = props.MeanCellSize();
			_isNoData = props.Height == 1 && props.Width == 1 &&
									(origCellSize < 0 || cellSize.X > origCellSize);
		}

		public IPolygon GetDomain()
		{
			return _surface.Domain;
//			return SurfaceUtils.TryGetDomain(_surface);
		}

		public double GetZ(double x, double y)
		{
			if (_isNoData)
			{
				return double.NaN;
			}

			return _surface.get_Z(x, y);
		}

		public IGeometry Drape(IGeometry shape, double densifyDistance = double.NaN)
		{
			Assert.ArgumentNotNull(shape, nameof(shape));

			object stepSizeObj = densifyDistance > 0
														 ? densifyDistance
														 : Type.Missing;

			_surface.InterpolateShape(shape, out IGeometry outShape, ref stepSizeObj);

			if (_isNoData)
			{
				((IZ)outShape)?.SetConstantZ(double.NaN);
			}

			return outShape;
		}

		public IGeometry SetShapeVerticesZ(IGeometry shape)
		{
			Assert.ArgumentNotNull(shape, nameof(shape));

			_surface.InterpolateShapeVertices(shape, out IGeometry outShape);
			if (_isNoData)
			{
				((IZ)outShape)?.SetConstantZ(double.NaN);
			}

			return outShape;
		}

		ITin ISimpleSurface.AsTin(IEnvelope extent) => null;

		public IRaster AsRaster() => _raster;

		public void Dispose()
		{
			if (_raster != null)
			{
				Marshal.ReleaseComObject(_raster);
			}
		}
	}
}

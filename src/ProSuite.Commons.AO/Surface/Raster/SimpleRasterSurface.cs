using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.AO.Surface.Raster
{
	/// <summary>
	/// Simple surface implementation for a set of raster files. The appropriate raster
	/// to be used at a specific location is provided by the raster dataset provider. The
	/// raster datasets are cached.
	/// </summary>
	public class SimpleRasterSurface : ISimpleSurface
	{
		private readonly IRasterProvider _rasterProvider;

		private readonly RasterCache _rasterCache;

		private readonly ArrayProvider<WKSPointZ> _wksPointArrayProvider =
			new ArrayProvider<WKSPointZ>();

		public SimpleRasterSurface(IRasterProvider rasterProvider,
		                           ISpatialReference spatialReference = null)
		{
			_rasterProvider = rasterProvider;

			IEnvelope boundaryEnv = _rasterProvider.GetInterpolationDomain().Envelope;

			if (boundaryEnv.SpatialReference == null)
			{
				Assert.NotNull(spatialReference, "No spatial reference");
				boundaryEnv.SpatialReference = spatialReference;
			}

			EnvelopeXY envelope = new EnvelopeXY(
				boundaryEnv.XMin, boundaryEnv.YMin, boundaryEnv.XMax, boundaryEnv.YMax);

			_rasterCache = new RasterCache(envelope, GeometryUtils.GetXyTolerance(boundaryEnv));
		}

		/// <summary>
		/// If for any of the vertices no valid Z value can be calculated because
		/// no raster is available a the respective location, null shall be returned
		/// for the <see cref="Drape"/> and <see cref="SetShapeVerticesZ"/> methods.
		/// This is consistent with the ArcObjects behaviour.
		/// </summary>
		public bool ReturnNullGeometryIfNotCompletelyCovered { get; set; } = true;

		/// <summary>
		/// The value to return in <see cref="GetZ"/> or to assign to the respective vertices
		/// for the <see cref="Drape"/> and <see cref="SetShapeVerticesZ"/> methods if no valid
		/// Z value can be calculated.
		/// </summary>
		public double DefaultValueForUnassignedZs { get; set; } = double.NaN;

		#region ISimpleSurface members

		IRaster ISimpleSurface.AsRaster() => throw new NotImplementedException();

		public void Dispose()
		{
			_rasterCache?.Dispose();
		}

		public IPolygon GetDomain()
		{
			return _rasterProvider.GetInterpolationDomain();
		}

		public double GetZ(double x, double y)
		{
			double result = GetZCore(x, y);

			return double.IsNaN(result) ? DefaultValueForUnassignedZs : result;
		}

		public IGeometry Drape(IGeometry shape, double densifyDistance = double.NaN)
		{
			if (double.IsNaN(densifyDistance))
			{
				var rasters = GetRasters(shape.Envelope).ToList();

				densifyDistance = rasters.Min(r => Math.Min(r.PixelSizeX, r.PixelSizeY));
			}

			IGeometry result = GeometryFactory.Clone(shape);

			if (result is IPolycurve polycurve)
			{
				polycurve.Densify(densifyDistance, 0);
			}

			return TryUpdateShapeVerticesZ(result) ? result : null;
		}

		public IGeometry SetShapeVerticesZ(IGeometry shape)
		{
			IGeometry result = GeometryFactory.Clone(shape);

			return TryUpdateShapeVerticesZ(result) ? result : null;
		}

		public ITin AsTin(IEnvelope extent = null)
		{
			throw new NotImplementedException();
		}

		#endregion

		private bool TryGetZ(double x, double y, out double result)
		{
			result = GetZCore(x, y);

			if (double.IsNaN(result))
			{
				result = DefaultValueForUnassignedZs;

				return ! ReturnNullGeometryIfNotCompletelyCovered;
			}

			return true;
		}

		/// <summary>
		/// Returns the interpolated Z value at the specified location or NaN if
		/// not enough raster pixels can be loaded around the specified x/y values.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private double GetZCore(double x, double y)
		{
			ISimpleRaster simpleRaster = GetRaster(x, y);

			if (simpleRaster == null)
			{
				return double.NaN;
			}

			// 00 .. 11 builds a local coordinate system defined by the center points of the
			// adjacent pixels of the input x/y value.
			// The v00 .. v11 values are the Z values of the raster at the adjacent pixel centers.
			// * dfX/dfY is the x/y pixel coordinate relative to the raster origin.
			//   dX/dY is the pixel coordinate (i.e. pixel number) at 00
			//  01-----------------11
			//  |                  |
			//  |   * dfX/dfY      |
			//  |                  |
			//  |                  |
			//  |                  |
			//  |                  |
			//  00-----------------10

			if (! TryGetPixelValues(x, y, simpleRaster,
			                        out float v00, out float v10, out float v01, out float v11,
			                        out double dfX, out double dfY,
			                        out int dX, out int dY))
			{
				return double.NaN;
			}

			// Point position in the (0, 0) - (1, 1) coordinate system
			double dfDeltaX = dfX - dX;
			double dfDeltaY = dfY - dY;

			// distances from the upper bounds for the (1 - x) and (1 - y) terms
			double dfDeltaX1 = 1.0 - dfDeltaX;
			double dfDeltaY1 = 1.0 - dfDeltaY;

			// See Wikipedia, section Unit square, simplified using distributive law
			// (f(0,0) * (1-x) + f(1,0) * x) * (1-y) +
			// (f(0,1) * (1-x) + f(1,1) * x ) * y
			// as it's also done by GDAL

			double value =
				(v00 * dfDeltaX1 + v10 * dfDeltaX) * dfDeltaY1 +
				(v01 * dfDeltaX1 + v11 * dfDeltaX) * dfDeltaY;

			return value;
		}

		private bool TryUpdateShapeVerticesZ(IGeometry shape)
		{
			if (GeometryUtils.IsMAware(shape) || GeometryUtils.IsPointIDAware(shape))
			{
				throw new NotImplementedException("M and ID-aware geometries not yet supported.");
			}

			GeometryUtils.MakeZAware(shape);

			if (shape is IPoint point)
			{
				return TryUpdatePointZ(point);
			}

			if (shape is IPolycurve)
			{
				foreach (IGeometry part in GeometryUtils.GetParts(shape))
				{
					IPointCollection4 partPoints = (IPointCollection4) part;

					if (! TryUpdatePointCollection(partPoints))
					{
						return false;
					}
				}

				return true;
			}

			// Multipoints:
			if (shape is IPointCollection4 pointCollection)
			{
				return TryUpdatePointCollection(pointCollection);
			}

			// TODO: Multipatches
			throw new ArgumentOutOfRangeException(
				$"Unsupported geometry type: {shape.GeometryType}");
		}

		private bool TryUpdatePointCollection(IPointCollection4 pointCollection)
		{
			// TODO: Alternative implementation for M/Id aware geometries!

			int pointCount = pointCollection.PointCount;

			WKSPointZ[] wksPointZs = _wksPointArrayProvider.GetArray(pointCount);

			GeometryUtils.QueryWKSPointZs(pointCollection, wksPointZs, 0, pointCount);

			for (int i = 0; i < pointCount; i++)
			{
				WKSPointZ pt = wksPointZs[i];

				if (! TryGetZ(pt.X, pt.Y, out double resultZ))
				{
					return false;
				}

				pt.Z = resultZ;

				wksPointZs[i] = pt;
			}

			GeometryUtils.SetWKSPointZs(pointCollection, wksPointZs, pointCount);

			return true;
		}

		private bool TryUpdatePointZ(IPoint point)
		{
			if (! TryGetZ(point.X, point.Y, out double resultZ))
			{
				return false;
			}

			point.Z = resultZ;
			return true;
		}

		/// <summary>
		/// 00 .. 11 builds a local coordinate system defined by the center points of the
		/// adjacent pixels of the input x/y value.
		/// The v00 .. v11 values are the Z values of the raster at the adjacent pixel centers.
		/// * dfX/dfY is the x/y pixel coordinate relative to the raster origin.
		///   dX/dY is the pixel coordinate (i.e. pixel number) at 00
		///  01-----------------11  or, if the raster     00-----------------10
		///  |                  |   resolution is a       |                  |
		///  |   * dfX/dfY      |   positive number:      |   * dfX/dfY      |
		///  |                  |                         |                  |       x
		///  |                  |     ^                   |                  |      --->
		///  |                  |   y |                   |                  |    y |
		///  |                  |     --->                |                  |      v
		///  00-----------------10     x                  01-----------------11
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="simpleRaster"></param>
		/// <param name="v00">Pixel value of the 'lower' (as in lower number) left pixel's center</param>
		/// <param name="v10">Pixel value of the 'lower' right pixel's center</param>
		/// <param name="v01"></param>
		/// <param name="v11"></param>
		/// <param name="dfX">X coordinate of the specified point in pixel coordinates</param>
		/// <param name="dfY">Y coordinate of the specified point in pixel coordinates</param>
		/// <param name="dX">X coordinate of the lower left pixel's center in pixel coordinates</param>
		/// <param name="dY">Y coordinate of the lower left pixel's center in pixel coordinates</param>
		/// <returns></returns>
		private bool TryGetPixelValues(
			double x, double y,
			[NotNull] ISimpleRaster simpleRaster,
			out float v00, out float v10, out float v01, out float v11,
			out double dfX, out double dfY,
			out int dX, out int dY)
		{
			ToPixelSpace(x, y, simpleRaster, out dfX, out dfY);

			dX = (int) Math.Floor(dfX);
			dY = (int) Math.Floor(dfY);

			// Read all 4 pixels
			ISimplePixelBlock<float> pixels = simpleRaster.CreatePixelBlock<float>(2, 2);
			simpleRaster.ReadPixelBlock(dX, dY, pixels);

			// Get the 4 pixels with the known values of the (0, 0) - (1, 1) coordinate system
			v00 = pixels.GetValue(0, 0);
			v10 = pixels.GetValue(1, 0);
			v01 = pixels.GetValue(0, 1);
			v11 = pixels.GetValue(1, 1);

			pixels.Dispose();

			float noDataValue = (float) simpleRaster.NoDataValue;

			if (HasNoDataValues(noDataValue, v00, v10, v01, v11))
			{
				// Try loading neighbouring raster datasets:
				double searchTolerance = Math.Max(simpleRaster.PixelSizeX, simpleRaster.PixelSizeY);

				// This should return the rasters in descending priority:
				foreach (ISimpleRaster otherRaster in GetRasters(x, y, searchTolerance))
				{
					pixels = ReadPixelBlockAround(x, y, otherRaster);

					AddMissingValues(pixels, (float) otherRaster.NoDataValue,
					                 ref v00, ref v10, ref v01, ref v11);
				}

				return ! HasNoDataValues(noDataValue, v00, v10, v01, v11);
			}

			return true;
		}

		private ISimplePixelBlock<float> ReadPixelBlockAround(double x, double y,
		                                                      [NotNull] ISimpleRaster simpleRaster)
		{
			ToPixelSpace(x, y, simpleRaster, out double dfX, out double dfY);

			int dX = (int) Math.Floor(dfX);
			int dY = (int) Math.Floor(dfY);

			// Read all 4 pixels
			ISimplePixelBlock<float> pixels = simpleRaster.CreatePixelBlock<float>(2, 2);
			simpleRaster.ReadPixelBlock(dX, dY, pixels);

			return pixels;
		}

		private static void ToPixelSpace(double x, double y,
		                                 [NotNull] ISimpleRaster simpleRaster,
		                                 out double dfX, out double dfY)
		{
			// Convert projected x,y to raster space (pixel coordinates)
			double pxlX = (x - simpleRaster.OriginX) / simpleRaster.PixelSizeX;
			double pxlY = (y - simpleRaster.OriginY) / simpleRaster.PixelSizeY;

			// Convert from upper left corner of pixel coordinates to center of
			// pixel coordinates because the pixel coordinate system's origin
			// is the center of the pixel:

			dfX = pxlX - 0.5;
			dfY = pxlY - 0.5;
		}

		private static void AddMissingValues(
			ISimplePixelBlock<float> pixels, float noDataValue,
			ref float v00, ref float v10,
			ref float v01, ref float v11)
		{
			v00 = IsNoData(v00, noDataValue) ? pixels.GetValue(0, 0) : v00;
			v10 = IsNoData(v10, noDataValue) ? pixels.GetValue(1, 0) : v10;
			v01 = IsNoData(v01, noDataValue) ? pixels.GetValue(0, 1) : v01;
			v11 = IsNoData(v11, noDataValue) ? pixels.GetValue(1, 1) : v11;
		}

		private ISimpleRaster GetRaster(double x, double y)
		{
			// TODO: Get all rasters in an extent, Z-order stuff
			ISimpleRaster simpleRaster = _rasterCache.GetRasters(x, y).FirstOrDefault();

			if (simpleRaster == null)
			{
				simpleRaster = _rasterProvider.GetSimpleRaster(x, y);

				if (simpleRaster != null)
				{
					_rasterCache.AddRaster(simpleRaster);
				}
			}

			return simpleRaster;
		}

		private IEnumerable<ISimpleRaster> GetRasters(double x, double y,
		                                              double searchTolerance)
		{
			IEnvelope envelope = CreateSearchEnvelope(x, y, searchTolerance);

			return GetRasters(envelope);
		}

		private IEnumerable<ISimpleRaster> GetRasters(IEnvelope envelope)
		{
			// Do not use the cache to make sure we get every possible raster:
			foreach (var simpleRaster in _rasterProvider.GetSimpleRasters(envelope))
			{
				Pnt2D centerPoint = simpleRaster.GetEnvelope().GetCenterPoint();

				// Currently we assume to cache exactly 0 or 1 raster at a specific location
				// TODO: Proper Equality comparison
				bool alreadyCached = _rasterCache.GetRasters(centerPoint.X, centerPoint.Y).Any();

				if (! alreadyCached)
				{
					_rasterCache.AddRaster(simpleRaster);
				}

				yield return simpleRaster;
			}
		}

		private static bool HasNoDataValues(float noDataValue, params float[] values)
		{
			foreach (float value in values)
			{
				if (IsNoData(value, noDataValue))
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsNoData(float value, float noDataValue)
		{
			return MathUtils.AreEqual(value, noDataValue);
		}

		private IEnvelope CreateSearchEnvelope(double x, double y,
		                                       double searchTolerance)
		{
			ISpatialReference spatialReference =
				_rasterProvider.GetInterpolationDomain().SpatialReference;

			searchTolerance += SpatialReferenceUtils.GetXyResolution(spatialReference);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(
				x - searchTolerance, y - searchTolerance,
				x + searchTolerance, y + searchTolerance, spatialReference);

			return envelope;
		}
	}

	internal class RasterCache : IDisposable
	{
		private readonly EnvelopeXY _maximumExtent;
		private readonly double _searchTolerance;

		private SpatialHashSearcher<ISimpleRaster> _rasterIndex;

		public RasterCache(EnvelopeXY maximumExtent,
		                   double searchTolerance)
		{
			_maximumExtent = maximumExtent;
			_searchTolerance = searchTolerance;
		}

		public IEnumerable<ISimpleRaster> GetRasters(double x, double y)
		{
			if (_rasterIndex == null)
			{
				yield break;
			}

			foreach (ISimpleRaster simpleRaster in _rasterIndex.Search(new Pnt2D(x, y),
				_searchTolerance))
			{
				yield return simpleRaster;
			}
		}

		public void AddRaster([NotNull] ISimpleRaster simpleRaster)
		{
			EnvelopeXY envelope = simpleRaster.GetEnvelope();

			if (_rasterIndex == null)
			{
				_rasterIndex = CreateRasterIndex(envelope);
			}

			_rasterIndex.Add(simpleRaster, envelope);
		}

		private SpatialHashSearcher<ISimpleRaster> CreateRasterIndex(EnvelopeXY typicalRasterSize)
		{
			TilingDefinition tiling = new TilingDefinition(_maximumExtent.XMin, _maximumExtent.YMin,
			                                               typicalRasterSize.Width,
			                                               typicalRasterSize.Height);

			SpatialHashSearcher<ISimpleRaster> rasterIndex =
				new SpatialHashSearcher<ISimpleRaster>(tiling, 32, 4);

			return rasterIndex;
		}

		public void Dispose()
		{
			if (_rasterIndex == null)
			{
				return;
			}

			foreach (ISimpleRaster simpleRaster in _rasterIndex)
			{
				simpleRaster.Dispose();
			}
		}
	}
}

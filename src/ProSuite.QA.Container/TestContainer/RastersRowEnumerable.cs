using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	internal class RastersRowEnumerable
	{
		private const int _defaultMaxRasterPointCount = 4096 * 4096;
		[NotNull] private readonly HashSet<RasterReference> _rastersDict;
		[NotNull] private readonly ITestProgress _progress;

		private readonly Box _extent;
		private int _maxRasterPointCount;

		public RastersRowEnumerable(
			[NotNull] IEnumerable<RasterReference> rasterReferences,
			[NotNull] ITestProgress progress)
			: this(new HashSet<RasterReference>(rasterReferences), progress) { }

		public RastersRowEnumerable(
			[NotNull] HashSet<RasterReference> rasters,
			[NotNull] ITestProgress progress)
		{
			Assert.ArgumentNotNull(rasters, nameof(rasters));
			Assert.ArgumentNotNull(progress, nameof(progress));

			_rastersDict = rasters;
			_progress = progress;

			IEnvelope extent = null;
			var minDx = double.MaxValue;
			foreach (RasterReference raster in rasters)
			{
				
				if (extent == null)
				{
					extent = raster.GeoDataset.Extent.Envelope;
				}
				else
				{
					extent.Union(raster.GeoDataset.Extent.Envelope);
				}
			}

			if (extent != null)
			{
				_extent = QaGeometryUtils.CreateBox(extent);
			}

			// TODO: adapt cellsize according to minDx

			MaxRasterPointCount = _defaultMaxRasterPointCount;
		}

		public int MaxRasterPointCount
		{
			get { return _maxRasterPointCount; }
			set
			{
				_maxRasterPointCount = value;

				double sum = 0;
				foreach (var raster in _rastersDict)
				{
					double dx = raster.CellSize;
					sum += 1 / (dx * dx);
				}

				TileSize = Math.Sqrt(_maxRasterPointCount / sum);
			}
		}

		public double TileSize { get; set; }

		[PublicAPI]
		public double SearchDistance { get; set; }

		public int GetRastersTileCount([NotNull] Box currentTileBox)
		{
			if (_rastersDict.Count == 0)
			{
				return 0;
			}

			double minX;
			double minY;
			double maxX;
			double maxY;
			GetDataExtent(currentTileBox, out minX, out minY, out maxX, out maxY);

			int nx = GetTileCount(minX, maxX);
			int ny = GetTileCount(minY, maxY);

			if (nx <= 0 || ny <= 0)
			{
				return 0;
			}

			return nx * ny * _rastersDict.Count;
		}

		[NotNull]
		public IEnumerable<RasterRow> GetRasterRows([NotNull] Box box)
		{
			if (_rastersDict.Count == 0)
			{
				yield break;
			}

			double minX;
			double minY;
			double maxX;
			double maxY;
			GetDataExtent(box, out minX, out minY, out maxX, out maxY);

			int nx = GetTileCount(minX, maxX);
			int ny = GetTileCount(minY, maxY);

			double dx = (maxX - minX) / nx;
			double dy = (maxY - minY) / ny;

			if (nx <= 0 || ny <= 0)
			{
				yield break;
			}

			for (int ix = 0; ix < nx; ix++)
			{
				double rasterXMin = minX - SearchDistance + ix * dx;
				double rasterXMax = minX + SearchDistance + (ix + 1) * dx;

				for (int iy = 0; iy < ny; iy++)
				{
					double rasterYMin = minY - SearchDistance + iy * dy;
					double rasterYMax = minY + SearchDistance + (iy + 1) * dy;

					IEnvelope rasterBox = GeometryFactory.CreateEnvelope(
						rasterXMin, rasterYMin, rasterXMax, rasterYMax);

					var rows = _rastersDict
					           .Select(p => new RasterRow(rasterBox, p, _progress))
					           .ToList();

					foreach (RasterRow rasterRow in rows)
					{
						yield return rasterRow;
					}

					foreach (RasterRow rasterRow in rows)
					{
						rasterRow.DisposeSurface();
					}
				}
			}
		}

		private int GetTileCount(double min, double max)
		{
			return (int) Math.Ceiling((max - min) / TileSize);
		}

		private void GetDataExtent([NotNull] Box tileBox,
		                           out double minX, out double minY,
		                           out double maxX, out double maxY)
		{
			// tiles outside the extent of all rasters are ignored (by min > max)
			// this is not the same behavior as in TerrainRowEnumerable!
			//minX = Math.Max(_extent.Min.X, tileBox.Min.X);
			//minY = Math.Max(_extent.Min.Y, tileBox.Min.Y);

			//maxX = Math.Min(_extent.Max.X, tileBox.Max.X);
			//maxY = Math.Min(_extent.Max.Y, tileBox.Max.Y);

			// alternative: any tile is completely used to build rastertiles (for all rasters)
			//
			minX = tileBox.Min.X;
			minY = tileBox.Min.Y;

			maxX = tileBox.Max.X;
			maxY = tileBox.Max.Y;
		}
	}
}

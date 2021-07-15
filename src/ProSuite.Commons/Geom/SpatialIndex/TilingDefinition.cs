using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	/// <summary>
	/// Tiling definition that uses the border point allocation policy of bottom-left, i.e.
	/// points that are on the bottom or left border of a tile tile are assigned to that tile.
	/// </summary>
	public class TilingDefinition
	{
		public TilingDefinition(
			double originX, double originY,
			double tileWidth, double tileHeight)
		{
			OriginX = originX;
			OriginY = originY;
			TileWidth = tileWidth;
			TileHeight = tileHeight;
		}

		public double OriginX { get; }

		public double OriginY { get; }

		public double TileWidth { get; }

		public double TileHeight { get; }

		public TileIndex GetTileIndexAt([NotNull] Pnt3D point)
		{
			Assert.ArgumentNotNull(point, nameof(point));

			return GetTileIndexAt(point.X, point.Y);
		}

		public TileIndex GetTileIndexAt(double x, double y)
		{
			return GetTileIndex(x, y,
			                    OriginX, OriginY,
			                    TileWidth, TileHeight);
		}

		public IEnumerable<TileIndex> GetIntersectingTiles(
			double xMin, double yMin, double xMax, double yMax)
		{
			TileIndex minIndex = GetTileIndexAt(xMin, yMin);

			TileIndex maxIndex = GetTileIndexAt(xMax, yMax);

			return GetAllTilesBetween(minIndex, maxIndex);
		}

		public IEnumerable<TileIndex> GetIntersectingTiles(
			double xMin, double yMin, double xMax, double yMax,
			TileIndex minimumIndex, TileIndex maximumIndex)
		{
			TileIndex minExtentIndex = GetTileIndexAt(xMin, yMin);
			TileIndex maxExtentIndex = GetTileIndexAt(xMax, yMax);

			int minEast = Math.Max(minExtentIndex.East, minimumIndex.East);
			int minNorth = Math.Max(minExtentIndex.North, minimumIndex.North);

			int maxEast = Math.Min(maxExtentIndex.East, maximumIndex.East);
			int maxNorth = Math.Min(maxExtentIndex.North, maximumIndex.North);

			return GetAllTilesBetween(minEast, minNorth, maxEast, maxNorth);
		}

		public void QueryTileBounds(TileIndex forTile,
		                            [NotNull] IBox box)
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			GetTileBounds(forTile, OriginX, OriginY, TileWidth, TileHeight,
			              out xMin, out yMin, out xMax, out yMax);

			box.Min.X = xMin;
			box.Min.Y = yMin;
			box.Max.X = xMax;
			box.Max.Y = yMax;
		}

		public override string ToString()
		{
			return $"Origin: {OriginX} | {OriginY}, " +
			       $"tile width: {TileWidth}, tile height: {TileHeight}";
		}

		private static TileIndex GetTileIndex(double locationX, double locationY,
		                                      double originX, double originY,
		                                      double tileWidth, double tileHeight)
		{
			double tilePositionX = (locationX - originX) / tileWidth;
			double tilePositionY = (locationY - originY) / tileHeight;

			var indexEast = (int) Math.Floor(tilePositionX);
			var indexNorth = (int) Math.Floor(tilePositionY);

			return new TileIndex(indexEast, indexNorth);
		}

		private static void GetTileBounds(TileIndex forTile,
		                                  double originX, double originY,
		                                  double tileWidth, double tileHeight,
		                                  out double xMin, out double yMin,
		                                  out double xMax, out double yMax)
		{
			xMin = originX + forTile.East * tileWidth;
			yMin = originY + forTile.North * tileHeight;
			xMax = xMin + tileWidth;
			yMax = yMin + tileHeight;
		}

		private static IEnumerable<TileIndex> GetAllTilesBetween(
			TileIndex minIndex,
			TileIndex maxIndex)
		{
			return GetAllTilesBetween(minIndex.East, minIndex.North, maxIndex.East,
			                          maxIndex.North);
		}

		private static IEnumerable<TileIndex> GetAllTilesBetween(
			int minEast, int minNorth, int maxEast, int maxNorth)
		{
			for (int i = minEast; i <= maxEast; i++)
			{
				for (int j = minNorth; j <= maxNorth; j++)
				{
					yield return new TileIndex(i, j);
				}
			}
		}
	}
}

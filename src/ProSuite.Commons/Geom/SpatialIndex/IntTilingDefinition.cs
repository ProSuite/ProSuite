using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	/// <summary>
	/// Integer only implementation of TilingDefinition.
	/// TODO: We might need to change this to a generic type in the future.
	/// </summary>
	public class IntTilingDefinition
	{
		public IntTilingDefinition(
			int originX, int originY,
			int tileWidth, int tileHeight)
		{
			OriginX = originX;
			OriginY = originY;
			TileWidth = tileWidth;
			TileHeight = tileHeight;
		}

		public int OriginX { get; }

		public int OriginY { get; }

		public int TileWidth { get; }

		public int TileHeight { get; }

		public TileIndex GetTileIndexAt(int x, int y)
		{
			return GetTileIndex(x, y,
			                    OriginX, OriginY,
			                    TileWidth, TileHeight);
		}

		public IEnumerable<TileIndex> GetIntersectingTiles(
			int xMin, int yMin, int xMax, int yMax)
		{
			TileIndex minIndex = GetTileIndexAt(xMin, yMin);

			TileIndex maxIndex = GetTileIndexAt(xMax, yMax);

			return GetAllTilesBetween(minIndex, maxIndex);
		}

		public IEnumerable<TileIndex> GetIntersectingTiles(
			int xMin, int yMin, int xMax, int yMax,
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
			GetTileBounds(forTile, OriginX, OriginY, TileWidth, TileHeight,
			              out int xMin, out int yMin, out int xMax, out int yMax);

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

		private static TileIndex GetTileIndex(int locationX, int locationY,
		                                      int originX, int originY,
		                                      int tileWidth, int tileHeight)
		{
			int indexEast = (locationX - originX) / tileWidth;
			int indexNorth = (locationY - originY) / tileHeight;

			return new TileIndex(indexEast, indexNorth);
		}

		private static void GetTileBounds(TileIndex forTile,
		                                  int originX, int originY,
		                                  int tileWidth, int tileHeight,
		                                  out int xMin, out int yMin,
		                                  out int xMax, out int yMax)
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

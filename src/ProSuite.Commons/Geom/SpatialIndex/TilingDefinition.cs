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

		public IEnumerable<TileIndex> GetTileIndexAround(double x,
		                                                 double y,
		                                                 DistanceMetric distanceMetric = DistanceMetric.EuclideanDistance,
		                                                 int maxTileDistance = int.MaxValue)
		{
			switch (distanceMetric)
			{
				case DistanceMetric.EuclideanDistance:
					return GetTileIndexAroundEuclidean(x, y, maxTileDistance);
				case DistanceMetric.ChebyshevDistance:
					return GetTileIndexAroundChebyshev(x, y, maxTileDistance);
				case DistanceMetric.ManhattanDistance:
					return GetTileIndexAroundManhattan(x, y, maxTileDistance);
				default:
					throw new ArgumentException($"Unsupported distance metric: {distanceMetric}", nameof(distanceMetric));
			}
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

		// TODO: Test
		private IEnumerable<TileIndex> GetTileIndexAroundEuclidean(double x, double y, int maxTileDistance = int.MaxValue)
		{
			// Get tiles from Manhattan implementation and sort each ring by Euclidean distance

			using (var manhattanEnumerator =
			       GetTileIndexAroundManhattan(x, y, maxTileDistance).GetEnumerator())
			{
				if (! manhattanEnumerator.MoveNext())
					yield break;

				// First tile is always the center tile
				yield return manhattanEnumerator.Current;

				// Process remaining tiles in batches of 4, 8, 12, 16, ... (4 * manhattanDistance)
				for (int distance = 1; distance <= maxTileDistance; distance++)
				{
					var tilesInRing = new List<(TileIndex tile, double euclideanDist)>();
					int expectedTilesInRing = distance * 4;

					// Collect all tiles at this Manhattan distance
					for (int i = 0; i < expectedTilesInRing && manhattanEnumerator.MoveNext(); i++)
					{
						var tile = manhattanEnumerator.Current;

						// Calculate actual Euclidean distance from point to tile center
						double tileCenterX = OriginX + tile.East * TileWidth + TileWidth / 2;
						double tileCenterY = OriginY + tile.North * TileHeight + TileHeight / 2;
						double euclideanDistanceSquared =
							(x - tileCenterX) * (x - tileCenterX) +
							(y - tileCenterY) * (y - tileCenterY);

						tilesInRing.Add((tile, euclideanDistanceSquared));
					}

					// Sort by Euclidean distance and yield
					tilesInRing.Sort((a, b) => a.euclideanDist.CompareTo(b.euclideanDist));
					foreach ((TileIndex tile, var _) in tilesInRing)
					{
						yield return tile;
					}

					// If we didn't get the expected number of tiles, we've exhausted the enumerator
					if (tilesInRing.Count < expectedTilesInRing)
						break;
				}
			}
		}

		private IEnumerable<TileIndex> GetTileIndexAroundChebyshev(double x, double y, int maxTileDistance = int.MaxValue)
		{
			throw new NotImplementedException("Cannot use Chebyshev Distance. Not implemented.");
		}

		// TODO: Test
		private IEnumerable<TileIndex> GetTileIndexAroundManhattan(double x, double y, int maxTileDistance = int.MaxValue)
		{
			TileIndex centerTile = GetTileIndexAt(x, y);

			// Yield the center tile first (distance 0)
			yield return centerTile;

			// For each Manhattan distance from 1 to maxTileDistance
			for (int distance = 1; distance <= maxTileDistance; distance++)
			{
				// Note: For each distance we generate all tiles at exactly this Manhattan distance
				//		 => |dx| + |dy| = distance

				// We'll traverse the diamond shape clockwise starting from the top
				// This ensures a consistent order within each distance ring
				for (int dx = 0; dx <= distance; dx++)
				{
					int dy = distance - dx;

					// Generate the four points (or fewer if on axes)
					if (dx == 0)
					{
						// On vertical axis
						yield return new TileIndex(centerTile.East, centerTile.North + dy);
						yield return new TileIndex(centerTile.East, centerTile.North - dy);
					}
					else if (dy == 0)
					{
						// On horizontal axis
						yield return new TileIndex(centerTile.East + dx, centerTile.North);
						yield return new TileIndex(centerTile.East - dx, centerTile.North);
					}
					else
					{
						// In quadrants
						yield return new TileIndex(centerTile.East + dx, centerTile.North + dy);
						yield return new TileIndex(centerTile.East - dx, centerTile.North + dy);
						yield return new TileIndex(centerTile.East + dx, centerTile.North - dy);
						yield return new TileIndex(centerTile.East - dx, centerTile.North - dy);
					}
				}
			}
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

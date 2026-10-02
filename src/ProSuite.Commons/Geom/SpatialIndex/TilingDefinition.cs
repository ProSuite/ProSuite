using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	/// <summary>
	/// Tiling definition that uses the border point allocation policy of bottom-left, i.e.
	/// points that are on the bottom or left border of a tile are assigned to that tile.
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

		public TileIndex GetTileIndexAt([NotNull] ICoordinates point)
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
		                                                 DistanceMetric distanceMetric =
			                                                 DistanceMetric.EuclideanDistance,
		                                                 double maxDistance = double.MaxValue)
		{
			switch (distanceMetric)
			{
				case DistanceMetric.EuclideanDistance:
					return GetTileIndexAroundEuclidean(x, y, maxDistance);
				case DistanceMetric.ChebyshevDistance:
					return GetTileIndexAroundChebyshev(x, y, maxDistance);
				case DistanceMetric.ManhattanDistance:
					return GetTileIndexAroundManhattan(x, y, maxDistance);
				default:
					throw new ArgumentException($"Unsupported distance metric: {distanceMetric}",
					                            nameof(distanceMetric));
			}
		}

		public IEnumerable<TileIndex> GetIntersectingTiles(IBoundedXY bounds)
		{
			return GetIntersectingTiles(bounds.XMin, bounds.YMin, bounds.XMax, bounds.YMax);
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

		/// <summary>
		/// Efficiently calculates the number of tiles that intersect with the specified extent
		/// without enumerating all tile indices.
		/// </summary>
		/// <param name="bounds">The bounds to check for intersection.</param>
		/// <returns>The number of intersecting tiles.</returns>
		public long GetIntersectingTileCount(IBoundedXY bounds)
		{
			return GetIntersectingTileCount(bounds.XMin, bounds.YMin, bounds.XMax, bounds.YMax);
		}

		/// <summary>
		/// Efficiently calculates the number of tiles that intersect with the specified extent
		/// without enumerating all tile indices.
		/// </summary>
		/// <param name="xMin">The minimum X coordinate of the extent.</param>
		/// <param name="yMin">The minimum Y coordinate of the extent.</param>
		/// <param name="xMax">The maximum X coordinate of the extent.</param>
		/// <param name="yMax">The maximum Y coordinate of the extent.</param>
		/// <returns>The number of intersecting tiles.</returns>
		public long GetIntersectingTileCount(double xMin, double yMin, double xMax, double yMax)
		{
			TileIndex minIndex = GetTileIndexAt(xMin, yMin);
			TileIndex maxIndex = GetTileIndexAt(xMax, yMax);

			int tileXDifference = maxIndex.East - minIndex.East + 1;
			int tileYDifference = maxIndex.North - minIndex.North + 1;

			return tileXDifference * tileYDifference;
		}

		/// <summary>
		/// Efficiently calculates the number of tiles that intersect with the specified extent
		/// without enumerating all tile indices.
		/// </summary>
		/// <param name="xMin">The minimum X coordinate of the extent.</param>
		/// <param name="yMin">The minimum Y coordinate of the extent.</param>
		/// <param name="xMax">The maximum X coordinate of the extent.</param>
		/// <param name="yMax">The maximum Y coordinate of the extent.</param>
		/// <param name="minimumIndex">The minimum tile index where data is expected.</param>
		/// <param name="maximumIndex">The maximum tile index where data is expected.</param>
		/// <returns>The number of intersecting tiles.</returns>
		public long GetIntersectingTileCount(double xMin, double yMin, double xMax, double yMax,
		                                     TileIndex minimumIndex, TileIndex maximumIndex)
		{
			TileIndex minExtentIndex = GetTileIndexAt(xMin, yMin);
			TileIndex maxExtentIndex = GetTileIndexAt(xMax, yMax);

			int minEast = Math.Max(minExtentIndex.East, minimumIndex.East);
			int minNorth = Math.Max(minExtentIndex.North, minimumIndex.North);

			int maxEast = Math.Min(maxExtentIndex.East, maximumIndex.East);
			int maxNorth = Math.Min(maxExtentIndex.North, maximumIndex.North);

			int tileXDifference = maxEast - minEast + 1;
			int tileYDifference = maxNorth - minNorth + 1;

			return tileXDifference * tileYDifference;
		}

		public BoundedBox QueryTileBounds(TileIndex forTile)
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			GetTileBounds(forTile, OriginX, OriginY, TileWidth, TileHeight,
			              out xMin, out yMin, out xMax, out yMax);
			return new BoundedBox(xMin, yMin, xMax, yMax);
		}

		public void QueryTileBounds(TileIndex forTile,
		                            out double xMin, out double yMin,
		                            out double xMax, out double yMax)
		{
			GetTileBounds(forTile, OriginX, OriginY, TileWidth, TileHeight,
			              out xMin, out yMin, out xMax, out yMax);
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

		/// <summary>
		/// Tiles in order of their distance from the tile holding x/y, nearest first, out to
		/// <paramref name="maxDistance"/>.
		/// </summary>
		/// <remarks>
		/// Expanding square rings: no tile in ring r or later is nearer than r times the shorter
		/// tile side, so once ring r is generated everything nearer than the next ring's bound
		/// can be handed out. Rings do not overlap, so this needs neither a priority queue nor a
		/// visited set - it runs once per interpolated point, where allocating either shows.
		/// </remarks>
		private IEnumerable<TileIndex> GetTileIndexAroundEuclidean(
			double x, double y, double maxDistance = double.MaxValue)
		{
			double maxDistance2 = maxDistance * maxDistance;
			TileIndex centerTile = GetTileIndexAt(x, y);

			double ringStep = Math.Min(TileWidth, TileHeight);

			var pending = new List<(TileIndex Tile, double Distance2)>();

			for (int ring = 0; ring * ringStep <= maxDistance; ring++)
			{
				AddRing(centerTile, ring, maxDistance2, pending);

				// Strictly nearer: a tile exactly on the bound must still sort against the
				// next ring.
				double settled = (ring + 1) * ringStep;

				pending.Sort(_byDistanceThenIndex);

				int released = 0;

				while (released < pending.Count &&
				       pending[released].Distance2 < settled * settled)
				{
					yield return pending[released].Tile;
					released++;
				}

				pending.RemoveRange(0, released);
			}

			pending.Sort(_byDistanceThenIndex);

			foreach ((TileIndex tile, double _) in pending)
			{
				yield return tile;
			}
		}

		/// <summary>The border of the square <paramref name="ring"/> tiles out from the centre;
		/// the inside of it belongs to earlier rings.</summary>
		private void AddRing(TileIndex centerTile, int ring, double maxDistance2,
		                     List<(TileIndex Tile, double Distance2)> pending)
		{
			if (ring == 0)
			{
				pending.Add((centerTile, 0));
				return;
			}

			for (int east = centerTile.East - ring; east <= centerTile.East + ring; east++)
			{
				AddToRing(east, centerTile.North - ring, centerTile, maxDistance2, pending);
				AddToRing(east, centerTile.North + ring, centerTile, maxDistance2, pending);
			}

			for (int north = centerTile.North - ring + 1;
			     north <= centerTile.North + ring - 1;
			     north++)
			{
				AddToRing(centerTile.East - ring, north, centerTile, maxDistance2, pending);
				AddToRing(centerTile.East + ring, north, centerTile, maxDistance2, pending);
			}
		}

		private void AddToRing(int east, int north, TileIndex centerTile, double maxDistance2,
		                       List<(TileIndex Tile, double Distance2)> pending)
		{
			var tile = new TileIndex(east, north);

			double distance2 =
				TileUtils.EuclideanTileDistance2(tile, centerTile, TileWidth, TileHeight);

			if (distance2 <= maxDistance2)
			{
				pending.Add((tile, distance2));
			}
		}

		/// <summary>Hoisted so the per-ring sort does not allocate a delegate. Ties break on the
		/// tile index.</summary>
		private static readonly Comparison<(TileIndex Tile, double Distance2)>
			_byDistanceThenIndex =
				(a, b) =>
				{
					int byDistance = a.Distance2.CompareTo(b.Distance2);

					if (byDistance != 0)
					{
						return byDistance;
					}

					int byEast = a.Tile.East.CompareTo(b.Tile.East);

					return byEast != 0 ? byEast : a.Tile.North.CompareTo(b.Tile.North);
				};

		private IEnumerable<TileIndex> GetTileIndexAroundChebyshev(
			double x, double y, double maxDistance = double.MaxValue)
		{
			throw new NotImplementedException("Cannot use Chebyshev Distance. Not implemented.");
		}

		private IEnumerable<TileIndex> GetTileIndexAroundManhattan(
			double x, double y, double maxDistance = double.MaxValue)
		{
			TileIndex centerTile = GetTileIndexAt(x, y);

			// Yield the center tile first (distance 0)
			yield return centerTile;

			// For each Manhattan distance from 1 to maxDistance
			for (int distance = 1; distance <= maxDistance; distance++)
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

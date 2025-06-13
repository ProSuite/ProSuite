using System;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	// TODO: Consider to make this internal and expose it to the test project.
	public static class TileUtils
	{
		/// <summary>
		/// Returns the selected distance of the tile to another tile.
		/// </summary>
		/// <param name="t1">Tile 1</param>
		/// <param name="t2">Tile 2</param>
		/// <param name="tileWidth">The width of the tiles. Important: Both tiles need to have the same width.</param>
		/// <param name="tileHeight">The height of the tiles. Important: Both tiles need to have the same height.</param>
		/// <param name="distanceMetric">Which distance to calculate</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static double TileDistance(TileIndex t1, TileIndex t2, double tileWidth, double tileHeight,
		                                  DistanceMetric distanceMetric =
			                                  DistanceMetric.EuclideanDistance)
		{
			switch (distanceMetric)
			{
				case DistanceMetric.EuclideanDistance:
					return EuclideanTileDistance(t1, t2, tileWidth, tileHeight);
				case DistanceMetric.ChebyshevDistance:
					return ChebyshevTileDistance(t1, t2, tileWidth, tileHeight);
				case DistanceMetric.ManhattanDistance:
					return ManhattanTileDistance(t1, t2, tileWidth, tileHeight);

				default:
					throw new ArgumentException($"Unsupported distance metric: {distanceMetric}",
					                            nameof(distanceMetric));
			}
		}

		/// <summary>
		/// Get the euclidean distance squared (To prevent having to take the root which is expensive)
		/// Can be used for comparisons.
		/// </summary>
		/// <param name="t1">Tile 1</param>
		/// <param name="t2">Tile 2</param>
		/// <param name="tileWidth">The width of the tiles. Important: Both tiles need to have the same width.</param>
		/// <param name="tileHeight">The height of the tiles. Important: Both tiles need to have the same height.</param>
		/// <returns></returns>
		public static double EuclideanTileDistance2(TileIndex t1, TileIndex t2, double tileWidth,
		                                            double tileHeight)
		{
			double dx = Math.Abs(t1.East * tileWidth - t2.East * tileWidth);
			double dy = Math.Abs(t1.North * tileHeight - t2.East * tileHeight);

			return dx * dx + dy * dy;
		}

		private static double EuclideanTileDistance(TileIndex t1, TileIndex t2, double tileWidth, double tileHeight)
		{
			return Math.Sqrt(EuclideanTileDistance2(t1, t2, tileWidth, tileHeight));
		}

		private static double ManhattanTileDistance(TileIndex t1, TileIndex t2, double tileWidth, double tileHeight)
		{
			return Math.Abs(t1.East * tileWidth - t2.East * tileWidth) +
			       Math.Abs(t1.North * tileHeight - t2.North * tileHeight);
		}

		private static double ChebyshevTileDistance(TileIndex t1, TileIndex t2, double tileWidth, double tileHeight)
		{
			throw new NotImplementedException(
				"Chebyshev Distance is not implemented for Tile indexes");
		}
	}
}


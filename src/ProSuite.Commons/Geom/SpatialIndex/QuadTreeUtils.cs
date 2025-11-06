using System;
using System.Collections.Generic;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public static class QuadTreeUtils
	{
		/// <summary>
		/// Get all tiles between the specified tile bounds in quadtree order, starting from the bottom-left.
		/// </summary>
		/// <param name="minEast"></param>
		/// <param name="minNorth"></param>
		/// <param name="maxEast"></param>
		/// <param name="maxNorth"></param>
		/// <returns></returns>
		public static IEnumerable<TileIndex> GetAllTilesBetween(
			int minEast, int minNorth, int maxEast, int maxNorth)
		{
			int width = maxEast - minEast + 1;
			int height = maxNorth - minNorth + 1;

			// Find the smallest power of 2 that contains both dimensions
			int maxDim = Math.Max(width, height);
			int quadSize = 1;
			while (quadSize < maxDim)
			{
				quadSize *= 2;
			}

			// Recursively traverse the quadtree
			foreach (var tile in TraverseQuadTree(minEast, minNorth, quadSize, minEast, minNorth,
			                                      maxEast, maxNorth))
			{
				yield return tile;
			}
		}

		private static IEnumerable<TileIndex> TraverseQuadTree(
			int startEast, int startNorth, int size,
			int minEast, int minNorth, int maxEast, int maxNorth)
		{
			// Base case: single tile
			if (size == 1)
			{
				if (startEast >= minEast && startEast <= maxEast &&
				    startNorth >= minNorth && startNorth <= maxNorth)
				{
					yield return new TileIndex(startEast, startNorth);
				}

				yield break;
			}

			int halfSize = size / 2;

			// Check if this quadrant intersects with our bounds
			if (startEast > maxEast || startEast + size - 1 < minEast ||
			    startNorth > maxNorth || startNorth + size - 1 < minNorth)
			{
				yield break;
			}

			// Traverse in quadtree order: bottom-left, bottom-right, top-left, top-right
			// (0,0), (1,0), (0,1), (1,1) pattern

			// Bottom-left quadrant (0, 0)
			foreach (var tile in TraverseQuadTree(startEast, startNorth, halfSize, minEast,
			                                      minNorth, maxEast, maxNorth))
				yield return tile;

			// Bottom-right quadrant (1, 0)
			foreach (var tile in TraverseQuadTree(startEast + halfSize, startNorth, halfSize,
			                                      minEast, minNorth, maxEast, maxNorth))
				yield return tile;

			// Top-left quadrant (0, 1)
			foreach (var tile in TraverseQuadTree(startEast, startNorth + halfSize, halfSize,
			                                      minEast, minNorth, maxEast, maxNorth))
				yield return tile;

			// Top-right quadrant (1, 1)
			foreach (var tile in TraverseQuadTree(startEast + halfSize, startNorth + halfSize,
			                                      halfSize, minEast, minNorth, maxEast, maxNorth))
				yield return tile;
		}
	}
}

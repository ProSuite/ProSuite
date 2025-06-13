using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Test.Geom.SpatialIndex
{
	internal class TilingDefinitionTest
	{
		[Test]
		public void TestGetTileIndexAroundEuclidean()
		{
			var tiling = new TilingDefinition(-0.5, -0.5, 1, 1);
			var tileIndexesAround = tiling.GetTileIndexAround(0, 0, maxDistance: 5).ToList();

			var centerTile = tileIndexesAround[0];

			// Center point
			Assert.AreEqual(centerTile.East, 0);
			Assert.AreEqual(centerTile.North, 0);

			Assert.AreEqual(
				1.0,
				TileUtils.TileDistance(tileIndexesAround[1], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				1.41,
				TileUtils.TileDistance(tileIndexesAround[7], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				2.23,
				TileUtils.TileDistance(tileIndexesAround[15], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				2.23,
				TileUtils.TileDistance(tileIndexesAround[19], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				2.82,
				TileUtils.TileDistance(tileIndexesAround[24], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				3,
				TileUtils.TileDistance(tileIndexesAround[26], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				3.16,
				TileUtils.TileDistance(tileIndexesAround[32], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				3.16,
				TileUtils.TileDistance(tileIndexesAround[35], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				3.60,
				TileUtils.TileDistance(tileIndexesAround[37], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				3.60,
				TileUtils.TileDistance(tileIndexesAround[44], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				4.00,
				TileUtils.TileDistance(tileIndexesAround[45], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
			Assert.AreEqual(
				5.0,
				TileUtils.TileDistance(tileIndexesAround[80], centerTile, tiling.TileWidth,
				                       tiling.TileHeight), 0.01);
		}

		[Test]
		public void GetTileIndexAroundManhattan()
		{
			var tiling = new TilingDefinition(-0.5, -0.5, 1, 1);
			var tileIndexesAround = tiling
			                        .GetTileIndexAround(0, 0, maxDistance: 5,
			                                            distanceMetric: DistanceMetric
				                                            .ManhattanDistance).ToList();

			var centerTile = tileIndexesAround[0];

			// Center point
			Assert.AreEqual(centerTile.East, 0);
			Assert.AreEqual(centerTile.North, 0);

			// Bottom point
			Assert.AreEqual(
				1.0,
				TileUtils.TileDistance(centerTile, tileIndexesAround[1], tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance));
			Assert.AreEqual(
				2,
				TileUtils.TileDistance(tileIndexesAround[7], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				2,
				TileUtils.TileDistance(tileIndexesAround[12], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				3,
				TileUtils.TileDistance(tileIndexesAround[13], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				3,
				TileUtils.TileDistance(tileIndexesAround[24], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4,
				TileUtils.TileDistance(tileIndexesAround[26], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4,
				TileUtils.TileDistance(tileIndexesAround[32], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4,
				TileUtils.TileDistance(tileIndexesAround[35], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4,
				TileUtils.TileDistance(tileIndexesAround[37], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				5,
				TileUtils.TileDistance(tileIndexesAround[44], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				5,
				TileUtils.TileDistance(tileIndexesAround[45], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				5,
				TileUtils.TileDistance(tileIndexesAround[60], centerTile, tiling.TileWidth,
				                       tiling.TileHeight, DistanceMetric.ManhattanDistance),
				0.01);
		}
	}
}

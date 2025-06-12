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
			var tileIndexesAround = tiling.GetTileIndexAround(0, 0, maxTileDistance: 5).ToList();

			var centerTile = tileIndexesAround[0];

			// Center point
			Assert.AreEqual(centerTile.East, 0);
			Assert.AreEqual(centerTile.North, 0);

			// Bottom point
			Assert.AreEqual(1.0, tileIndexesAround[1].Distance(centerTile));
			Assert.AreEqual(1.41, tileIndexesAround[7].Distance(centerTile), 0.01);
			Assert.AreEqual(2.23, tileIndexesAround[15].Distance(centerTile), 0.01);
			Assert.AreEqual(2.23, tileIndexesAround[19].Distance(centerTile), 0.01);
			Assert.AreEqual(2.82, tileIndexesAround[24].Distance(centerTile), 0.01);
			Assert.AreEqual(3, tileIndexesAround[26].Distance(centerTile), 0.01);
			Assert.AreEqual(3.16, tileIndexesAround[32].Distance(centerTile), 0.01);
			Assert.AreEqual(3.16, tileIndexesAround[35].Distance(centerTile), 0.01);
			Assert.AreEqual(3.60, tileIndexesAround[37].Distance(centerTile), 0.01);
			Assert.AreEqual(3.60, tileIndexesAround[44].Distance(centerTile), 0.01);
			Assert.AreEqual(4.00, tileIndexesAround[45].Distance(centerTile), 0.01);
			Assert.AreEqual(5.0, tileIndexesAround[80].Distance(centerTile), 0.01);
		}

		[Test]
		public void GetTileIndexAroundManhattan()
		{
			var tiling = new TilingDefinition(-0.5, -0.5, 1, 1);
			var tileIndexesAround = tiling
			                        .GetTileIndexAround(0, 0, maxTileDistance: 5,
			                                            distanceMetric: DistanceMetric
				                                            .ManhattanDistance).ToList();

			var centerTile = tileIndexesAround[0];

			// Center point
			Assert.AreEqual(centerTile.East, 0);
			Assert.AreEqual(centerTile.North, 0);

			// Bottom point
			Assert.AreEqual(
				1.0, tileIndexesAround[1].Distance(centerTile, DistanceMetric.ManhattanDistance));
			Assert.AreEqual(
				2, tileIndexesAround[7].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				2, tileIndexesAround[12].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				3, tileIndexesAround[13].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				3, tileIndexesAround[24].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4, tileIndexesAround[26].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4, tileIndexesAround[32].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4, tileIndexesAround[35].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				4, tileIndexesAround[37].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				5, tileIndexesAround[44].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				5, tileIndexesAround[45].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
			Assert.AreEqual(
				5, tileIndexesAround[60].Distance(centerTile, DistanceMetric.ManhattanDistance),
				0.01);
		}
	}
}

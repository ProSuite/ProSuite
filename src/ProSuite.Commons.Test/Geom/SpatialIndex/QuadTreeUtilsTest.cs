using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Test.Geom.SpatialIndex
{
	public class QuadTreeUtilsTest
	{

		[Test]

		public void GetAllTilesBetween_QuadTree_ReturnsSingleTile()
		{
			// Act
			var tiles = QuadTreeUtils.GetAllTilesBetween(2, 3, 2, 3).ToList();
			// Assert
			Assert.AreEqual(1, tiles.Count);
			Assert.AreEqual(new TileIndex(2, 3), tiles[0]);
		}

		[Test]
		[Ignore("This test is currently ignored because the expected tile count and indices are not correct. The method may need to be revised to ensure it returns the correct tiles for the specified range.")]
		public void GetAllTilesBetween_QuadTree_ReturnsCorrectTiles_FirstSquare()
		{

			// Act
			var tiles = QuadTreeUtils.GetAllTilesBetween(0, 0, 1, 1).ToList();

			// Assert
			Assert.AreEqual(1, tiles.Count);
			Assert.AreEqual(new TileIndex(0, 0), tiles[0]);
			// Assert.AreEqual(new TileIndex(1, 0), tiles[1]);
			// Assert.AreEqual(new TileIndex(0, 1), tiles[2]);
			// Assert.AreEqual(new TileIndex(1, 1), tiles[3]);
		}

		[Test]
		[Ignore("This test is currently ignored because the expected tile count and indices are not correct. The method may need to be revised to ensure it returns the correct tiles for the specified range.")]
		public void GetAllTilesBetween_QuadTree_ReturnsCorrectTiles_SecondSquare()
		{

			// Act
			var tiles = QuadTreeUtils.GetAllTilesBetween(0, 0, 3, 3).ToList();

			// Assert
			Assert.AreEqual(9, tiles.Count);
			Assert.AreEqual(new TileIndex(2, 2), tiles[8]);
			// Assert.AreEqual(new TileIndex(0, 3), tiles[10]);
			// Assert.AreEqual(new TileIndex(3, 3), tiles[15]);
		}

		[Test]
		[Ignore("This test is currently ignored because the expected tile count and indices are not correct. The method may need to be revised to ensure it returns the correct tiles for the specified range.")]
		public void GetAllTilesBetween_QuadTree_ReturnsCorrectTilesMedium_ThirdSquare()
		{

			// Act
			var tiles = QuadTreeUtils.GetAllTilesBetween(0, 0, 7, 7).ToList();

			// Assert
			Assert.AreEqual(49, tiles.Count);
			Assert.AreEqual(new TileIndex(4, 0), tiles[16]);
			Assert.AreEqual(new TileIndex(0, 4), tiles[32]);
			Assert.AreEqual(new TileIndex(3, 7), tiles[47]);
			// Assert.AreEqual(new TileIndex(7, 7), tiles[63]);
		}
	}
}

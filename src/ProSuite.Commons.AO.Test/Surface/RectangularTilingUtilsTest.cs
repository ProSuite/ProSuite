using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Surface;

namespace ProSuite.Commons.AO.Test.Surface
{
	[TestFixture]
	public class RectangularTilingUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void Setup()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_lic.Release();
		}

		[Test]
		public void CanGetEnvelopeOneTile()
		{
			var tile = new RectangularTileIndex(0, 0);

			IEnvelope extent =
				RectangularTilingUtils.GetTileEnvelope(0, 10000, 10, 100, null, tile);

			Assert.AreEqual(0, extent.XMin);
			Assert.AreEqual(10000, extent.YMin);
			Assert.AreEqual(10, extent.XMax);
			Assert.AreEqual(10100, extent.YMax);
		}

		[Test]
		public void CanGetEnvelopeTwoTiles()
		{
			var tileLL = new RectangularTileIndex(0, 0);
			var tileUR = new RectangularTileIndex(1, 1);

			IEnvelope extent =
				RectangularTilingUtils.GetTileEnvelope(0, 10000, 10, 100, null, tileLL, tileUR);

			Assert.AreEqual(0, extent.XMin);
			Assert.AreEqual(10000, extent.YMin);
			Assert.AreEqual(20, extent.XMax);
			Assert.AreEqual(10200, extent.YMax);
		}

		[Test]
		public void CanGetIntersectedTilesExtent()
		{
			var tiling =
				new RectangularTilingStructure(0, 10000, 10, 100,
				                               BorderPointTileAllocationPolicy.BottomLeft,
				                               null);

			IEnvelope extent = GeometryFactory.CreateEnvelope(1, 10001, 9, 10099);
			IEnvelope constraintExtent =
				GeometryFactory.CreateEnvelope(-1, 9999, 11, 10101);

			IEnvelope tileExtent =
				tiling.GetIntersectedTilesExtent(extent, constraintExtent);

			Assert.AreEqual(0, tileExtent.XMin);
			Assert.AreEqual(10000, tileExtent.YMin);
			Assert.AreEqual(10, tileExtent.XMax);
			Assert.AreEqual(10100, tileExtent.YMax);
		}

		[Test]
		public void CanGetIntersectedTilesExtent2x2()
		{
			var tiling =
				new RectangularTilingStructure(0, 10000, 10, 100,
				                               BorderPointTileAllocationPolicy.BottomLeft,
				                               null);

			IEnvelope extent = GeometryFactory.CreateEnvelope(1, 10001, 12, 10120);
			IEnvelope constraintExtent =
				GeometryFactory.CreateEnvelope(-1, 9999, 20, 10200);

			IEnvelope tileExtent =
				tiling.GetIntersectedTilesExtent(extent, constraintExtent);

			Assert.AreEqual(0, tileExtent.XMin);
			Assert.AreEqual(10000, tileExtent.YMin);
			Assert.AreEqual(20, tileExtent.XMax);
			Assert.AreEqual(10200, tileExtent.YMax);
		}

		[Test]
		public void CanGetIntersectedTilesExtentExactMatch()
		{
			var tiling =
				new RectangularTilingStructure(0, 10000, 10, 100,
				                               BorderPointTileAllocationPolicy.BottomLeft,
				                               null);

			IEnvelope extent = GeometryFactory.CreateEnvelope(0, 10000, 10, 10100);
			IEnvelope constraintExtent =
				GeometryFactory.CreateEnvelope(-1, 9999, 11, 10101);

			IEnvelope tileExtent =
				tiling.GetIntersectedTilesExtent(extent, constraintExtent);

			Assert.AreEqual(0, tileExtent.XMin);
			Assert.AreEqual(10000, tileExtent.YMin);
			Assert.AreEqual(10, tileExtent.XMax);
			Assert.AreEqual(10100, tileExtent.YMax);
		}

		[Test]
		public void CanGetTileIndexes1DExactlyOnTile()
		{
			int? minIndex;
			int? maxIndex;

			RectangularTilingUtils.GetTileIndexes1D(0, 50, 0, 100, -10, 110, out minIndex,
			                                        out maxIndex);

			Assert.AreEqual(0, minIndex);
			Assert.AreEqual(1, maxIndex);
		}

		[Test]
		public void CanGetTileIndexes1DExactlyOnTileMatchingConstraint()
		{
			int? minIndex;
			int? maxIndex;

			RectangularTilingUtils.GetTileIndexes1D(0, 50, 0, 100, 0, 100, out minIndex,
			                                        out maxIndex);

			Assert.AreEqual(0, minIndex);
			Assert.AreEqual(1, maxIndex);
		}

		[Test]
		public void CanGetTileIndexes1DWithinTile()
		{
			int? minIndex;
			int? maxIndex;

			RectangularTilingUtils.GetTileIndexes1D(0, 50, 1, 99, -10, 110, out minIndex,
			                                        out maxIndex);

			Assert.AreEqual(0, minIndex);
			Assert.AreEqual(1, maxIndex);
		}
	}
}

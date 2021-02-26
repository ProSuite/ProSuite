using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class KnownGapsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private IEnvelope _tile11;
		private IEnvelope _tile12;
		private IEnvelope _tile21;
		private IEnvelope _tile22;

		private const double _tolerance = 0.01;
		private ISpatialReference _spatialReference;

		[SetUp]
		public void SetUp()
		{
			_spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903_LV03, true);
			SpatialReferenceUtils.SetXYDomain(_spatialReference, -1000, -1000, 1000, 1000,
			                                  _tolerance / 10, _tolerance);

			Console.WriteLine(((ISpatialReferenceTolerance) _spatialReference).XYTolerance);
			_tile11 = CreateEnvelope(0, 0, 50, 50);
			_tile12 = CreateEnvelope(50, 0, 100, 50);
			_tile21 = CreateEnvelope(0, 50, 50, 100);
			_tile22 = CreateEnvelope(50, 50, 100, 100);
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanIgnoreCrossingGapsLargerThanLimit()
		{
			const double maxArea = 100; // smaller than any of the polygons

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// 4 polygons 20x20, each one centered on the LL corner of a tile
			var gapPolygons = new[]
			                  {
				                  CreatePoly(-10, -10, 10, 10),
				                  CreatePoly(40, -10, 60, 10),
				                  CreatePoly(-10, 40, 10, 60),
				                  CreatePoly(40, 40, 60, 60)
			                  };

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(0, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		[Test]
		public void CanGetCrossingGaps()
		{
			const double maxArea = 1000; // larger than all polygons

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// 4 polygons 20x20, each one centered on the LL corner of a tile
			var gapPolygons = new[]
			                  {
				                  CreatePoly(-10, -10, 10, 10),
				                  CreatePoly(40, -10, 60, 10),
				                  CreatePoly(-10, 40, 10, 60),
				                  CreatePoly(40, 40, 60, 60)
			                  };

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(0, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(1, gaps22.Count);
		}

		[Test]
		public void CanGetCrossingGapsCombined()
		{
			const double maxArea = 200;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			var gapPolygons =
				new[]
				{
					CreatePoly(-5, 10, 5, 20), // < limit, cross L t11 (no gap)
					CreatePoly(10, 10, 15, 20), // < limit, inside t11 (gap)
					CreatePoly(10, 10, 51, 20), // > limit, cross t11 and t12, <limit in t12
					CreatePoly(49, 40, 51, 41), // < limit, cross t11 and t12 (gap)
					CreatePoly(50, 49, 51, 51), // < limit, cross t12 and t22 (gap)
					CreatePoly(10, 49, 11, 51), // < limit, cross t11 and t21 (gap)
					CreatePoly(80, 80, 110, 110), // > limit, exceed allbox (no gap)
					CreatePoly(99, 99, 101, 101) // < limit, exceed allbox (no gap)
				};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(1, gaps11.Count);
			Assert.AreEqual(1, gaps12.Count);
			Assert.AreEqual(1, gaps21.Count);
			Assert.AreEqual(1, gaps22.Count);
		}

		[Test]
		public void CanGetSmallCrossingGap()
		{
			const double maxArea = 200;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// < limit, cross t11 and t12
			var gapPolygons = new[] {CreatePoly(49, 10, 51, 11)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(1, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		[Test]
		public void CanGetSmallCrossingGapAtTileBoundary1()
		{
			const double maxArea = 200;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// < limit, xmax at right boundary of t11
			var gapPolygons = new[] {CreatePoly(49, 10, 50, 11)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(1, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		[Test]
		public void CanGetSmallCrossingGapAtTileBoundary2()
		{
			const double maxArea = 200;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// < limit, xmin at left boundary of t12
			var gapPolygons = new[] {CreatePoly(50, 10, 51, 11)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(1, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		[Test]
		public void CanGetSmallGapTouchingTileBoundary()
		{
			const double maxArea = 200;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// < limit, in UR corner of t11
			var gapPolygons = new[] {CreatePoly(49, 49, 50, 50)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(0, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(1, gaps22.Count); // reported in last tile
		}

		[Test]
		public void CanIgnoreLargeCrossingGap1()
		{
			const double maxArea = 100;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// tile intersections are all smaller than limit, but the entire gap is larger
			var gapPolygons = new[] {CreatePoly(45, 45, 55, 55.1)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(0, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		[Test]
		public void CanIgnoreLargeCrossingGap2()
		{
			const double maxArea = 100;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// t11 intersection is larger than limit, all others are smaller
			var gapPolygons = new[] {CreatePoly(20, 20, 50.1, 50.1)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(0, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		[Test]
		public void CanIgnoreLargeCrossingGap3()
		{
			const double maxArea = 100;

			Box allBox = GeomUtils.CreateBox(0, 0, 100, 100);

			var knownGaps = new KnownGaps(maxArea, _tolerance, allBox);

			// t22 intersection is larger than limit, t12 is smaller than limit
			var gapPolygons = new[] {CreatePoly(60, 49, 70, 90)};

			IList<IPolygon> gaps11 = GetGaps(knownGaps, _tile11, gapPolygons);
			IList<IPolygon> gaps12 = GetGaps(knownGaps, _tile12, gapPolygons);
			IList<IPolygon> gaps21 = GetGaps(knownGaps, _tile21, gapPolygons);
			IList<IPolygon> gaps22 = GetGaps(knownGaps, _tile22, gapPolygons);

			LogGaps(gaps11, gaps12, gaps21, gaps22);
			Assert.AreEqual(0, gaps11.Count);
			Assert.AreEqual(0, gaps12.Count);
			Assert.AreEqual(0, gaps21.Count);
			Assert.AreEqual(0, gaps22.Count);
		}

		private static void LogGaps(
			[NotNull] ICollection<IPolygon> gaps11,
			[NotNull] ICollection<IPolygon> gaps12,
			[NotNull] ICollection<IPolygon> gaps21,
			[NotNull] ICollection<IPolygon> gaps22)
		{
			Console.WriteLine(@"Tile 11");
			LogGaps(gaps11);
			Console.WriteLine(@"Tile 12");
			LogGaps(gaps12);
			Console.WriteLine(@"Tile 21");
			LogGaps(gaps21);
			Console.WriteLine(@"Tile 22");
			LogGaps(gaps22);
		}

		private static void LogGaps([NotNull] ICollection<IPolygon> gaps)
		{
			if (gaps.Count == 0)
			{
				Console.WriteLine(@"- no gaps reported");
				return;
			}

			foreach (IPolygon polygon in gaps)
			{
				double area = ((IArea) polygon).Area;
				IEnvelope ext = polygon.Envelope;
				Console.WriteLine(@"- Area: {0} Bounds: {1} {2} {3} {4}",
				                  area, ext.XMin, ext.YMin, ext.XMax, ext.YMax);
			}
		}

		[NotNull]
		private IList<IPolygon> GetGaps(
			[NotNull] KnownGaps knownGaps,
			[NotNull] IEnvelope tileEnvelope,
			params IPolygon[] gaps)
		{
			IEnvelope clipEnvelope = GetClipEnvelope(tileEnvelope, knownGaps.Tolerance);

			var clippedGaps = new List<IPolygon>();

			foreach (IPolygon polygon in gaps)
			{
				IPolygon clippedPolygon = GeometryUtils.GetClippedPolygon(polygon, clipEnvelope);

				if (! clippedPolygon.IsEmpty)
				{
					clippedGaps.Add(GeometryFactory.Clone(clippedPolygon));
				}
			}

			return new List<IPolygon>(
				knownGaps.GetCompletedGaps(clippedGaps, clipEnvelope, tileEnvelope));
		}

		[NotNull]
		private IPolygon CreatePoly(double xMin, double yMin, double xMax, double yMax)
		{
			return GeometryFactory.CreatePolygon(xMin, yMin, xMax, yMax, _spatialReference);
		}

		[NotNull]
		private IEnvelope CreateEnvelope(double xMin, double yMin, double xMax, double yMax)
		{
			return GeometryFactory.CreateEnvelope(xMin, yMin, xMax, yMax, _spatialReference);
		}

		[NotNull]
		private IEnvelope GetClipEnvelope([NotNull] IEnvelope envelope, double tolerance)
		{
			// TODO check if an enlarged clip envelope is really needed
			// return GeometryFactory.Clone(envelope);
			return GeometryFactory.CreateEnvelope(envelope.XMin - tolerance,
			                                      envelope.YMin - tolerance,
			                                      envelope.XMax,
			                                      envelope.YMax,
			                                      _spatialReference);
		}

		//[NotNull]
		//private static IEnumerable<Box> GetTiles(
		//    [NotNull] Box allBox,
		//    double tileSize, double minimumTileSize)
		//{
		//    double tileYMin = allBox.Min.Y;

		//    bool lastYTile = false;
		//    do
		//    {
		//        double remainderY = allBox.Max.Y - (tileYMin + tileSize);

		//        double tileYMax;
		//        if (remainderY > minimumTileSize)
		//        {
		//            tileYMax = tileYMin + tileSize;
		//        }
		//        else
		//        {
		//            // remainder is too small, return last tile to xMax
		//            tileYMax = allBox.Max.Y;
		//            lastYTile = true;
		//        }

		//        double tileXMin = allBox.Min.X;
		//        bool lastXTile = false;
		//        do
		//        {
		//            double remainderX = allBox.Max.X - (tileXMin + tileSize);

		//            double tileXMax;
		//            if (remainderX > minimumTileSize)
		//            {
		//                tileXMax = tileXMin + tileSize;
		//            }
		//            else
		//            {
		//                // remainder is too small, return last tile to xMax
		//                tileXMax = allBox.Max.X;
		//                lastXTile = true;
		//            }

		//            yield return QaGeometryUtils.CreateBox(tileXMin, tileYMin,
		//                                                   tileXMax, tileYMax);
		//            tileXMin = tileXMax;
		//        } while (!lastXTile);

		//        tileYMin = tileYMax;
		//    } while (! lastYTile);
		//}
	}
}

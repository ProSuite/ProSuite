using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Test.Geom.SpatialIndex
{
	[TestFixture]
	public class SpatialHashSearcherTest
	{
		[Test]
		public void SearchBetweenVeryDifferentExtentsSizes()
		{
			Linestring l1 = new Linestring(
				new[]
				{
					new Pnt3D(1200000, 2600000, 10),
					new Pnt3D(1200000, 2600000, 20),
					new Pnt3D(1200000, 2600000, 30),
					new Pnt3D(1200000, 2600000, 40),
					new Pnt3D(1200000, 2600000, 50),
					new Pnt3D(1200000, 2600000, 60),
					new Pnt3D(1200000, 2600000, 70),
					new Pnt3D(1200000, 2600000, 80),
					new Pnt3D(1200000, 2600000, 90),
					new Pnt3D(1200000, 2600000, 100),
					new Pnt3D(1200000, 2600000, 110),
					new Pnt3D(1200000, 2600000, 120),
					new Pnt3D(1200000, 2600000, 130),
					new Pnt3D(1200000, 2600000, 140),
				});

			l1.SpatialIndex = SpatialHashSearcher<int>.CreateSpatialSearcher(l1, 0.01);

			Stopwatch watch = Stopwatch.StartNew();

			List<KeyValuePair<int, Line3D>> found =
				l1.FindSegments(1000000, 2400000, 1400000, 2800000, 0.01).ToList();

			watch.Stop();

			Assert.AreEqual(l1.SegmentCount, found.Count);

			Assert.Less(watch.ElapsedMilliseconds, 50);
		}

		[Test]
		public void CanSearchSparseTilesInHugeAreaFastEnough()
		{
			// This test ensures that the search strategy is reversed if the area is huge
			// (i.e. the number of tiles to search is very large) compared with the number
			// of total entries in the _tiles dictionary of the SpatialHashSearcher.

			// Create a small number of items in a few tiles
			var points = new List<Pnt3D>
			             {
				             new Pnt3D(1200000, 2600000, 10), // tile (0, 0)
				             new Pnt3D(1200001, 2600000, 20), // tile (0, 0) 
				             new Pnt3D(1200100, 2600100, 30), // tile (1, 1)
				             new Pnt3D(1200101, 2600101, 40), // tile (1, 1)
			             };

			// Use a grid size of 10 resulting in a large number of empty tiles in the search area
			SpatialHashSearcher<Pnt3D> searcher =
				SpatialHashSearcher<Pnt3D>.CreateSpatialSearcher(
					points, p => new EnvelopeXY(p), 10);

			Stopwatch watch = Stopwatch.StartNew();

			// Now search a huge area that would intersect many more tiles than we actually have
			// This should trigger the optimization to iterate over all tiles instead of the search tiles
			// Without the optimization and without clamping to the data extent this takes almost 10s!
			var foundPoints = searcher.Search(
				1200020, 2600020, 1300000, 2700000, 0.0).ToList();

			watch.Stop();

			Assert.AreEqual(2, foundPoints.Count);

			Assert.Less(watch.ElapsedMilliseconds, 10);

			// Verify we got all the expected points
			var foundCoords = foundPoints.Select(p => new { p.X, p.Y }).OrderBy(p => p.X)
			                             .ThenBy(p => p.Y).ToList();

			Assert.AreEqual(1200100, foundCoords[0].X);
			Assert.AreEqual(2600100, foundCoords[0].Y);
			Assert.AreEqual(1200101, foundCoords[1].X);
			Assert.AreEqual(2600101, foundCoords[1].Y);

			// Add some points very far away that should not be found (and to make clamping less effective)
			var pnt4 = new Pnt3D(1300100, 2700100, 70);
			var pnt5 = new Pnt3D(1300101, 2700101, 80);
			points.Add(pnt4); // tile (101, 101)
			points.Add(pnt5); // tile (101, 101)

			searcher.Add(pnt4, pnt4);
			searcher.Add(pnt5, pnt5);

			watch.Restart();

			foundPoints = searcher.Search(1200020, 2600020, 1300000, 2700000, 0.0).ToList();

			Assert.AreEqual(2, foundPoints.Count);

			Assert.Less(watch.ElapsedMilliseconds, 10);

			foundCoords = foundPoints.Select(p => new { p.X, p.Y }).OrderBy(p => p.X)
			                         .ThenBy(p => p.Y).ToList();

			Assert.AreEqual(1200100, foundCoords[0].X);
			Assert.AreEqual(2600100, foundCoords[0].Y);
			Assert.AreEqual(1200101, foundCoords[1].X);
			Assert.AreEqual(2600101, foundCoords[1].Y);

			// Now search a smaller area that still contains the same points (to use the traditional dictionary-based search)
			watch.Restart();
			foundPoints = searcher.Search(1200100, 2600100, 1200101, 2600101, 0.0).ToList();

			Assert.AreEqual(2, foundPoints.Count);

			Assert.Less(watch.ElapsedMilliseconds, 10);

			foundCoords = foundPoints.Select(p => new { p.X, p.Y }).OrderBy(p => p.X)
			                         .ThenBy(p => p.Y).ToList();

			Assert.AreEqual(1200100, foundCoords[0].X);
			Assert.AreEqual(2600100, foundCoords[0].Y);
			Assert.AreEqual(1200101, foundCoords[1].X);
			Assert.AreEqual(2600101, foundCoords[1].Y);
		}

		[Test]
		public void CanSearchPoints()
		{
			var points =
				new[]
				{
					new Pnt3D(1200000, 2600000, 10),
					new Pnt3D(1200010, 2600000, 20),
					new Pnt3D(1200020, 2600000, 30),
					new Pnt3D(1200030, 2600000, 40),
					new Pnt3D(1200040, 2600000, 50),
					new Pnt3D(1200050, 2600000, 60),
					new Pnt3D(1200060, 2600000, 70),
					new Pnt3D(1200070, 2600000, 80),
					new Pnt3D(1200080, 2600000, 90),
					new Pnt3D(1200090, 2600000, 100),
					new Pnt3D(1200100, 2600000, 110),
					new Pnt3D(1200000, 2600010, 120),
					new Pnt3D(1200000, 2600020, 130),
					new Pnt3D(1200000, 2600030, 140),
				};

			SpatialHashSearcher<Pnt3D> searcher =
				SpatialHashSearcher<Pnt3D>.CreateSpatialSearcher(points, p => new EnvelopeXY(p));

			var foundPoints = searcher.Search(1200050, 2600000, 1200060, 2600010, 0.0).ToList();

			Assert.AreEqual(2, foundPoints.Count);
		}

		[Test]
		public void CanSearchSinglePoint()
		{
			var points =
				new[]
				{
					new Pnt3D(1200000, 2600000, 10),
				};

			SpatialHashSearcher<Pnt3D> searcher;

			Assert.Throws<ArgumentException>(
				() =>
				{
					searcher =
						SpatialHashSearcher<Pnt3D>.CreateSpatialSearcher(
							points, p => new EnvelopeXY(p));
				});

			searcher =
				SpatialHashSearcher<Pnt3D>.CreateSpatialSearcher(
					points, p => new EnvelopeXY(p), 10);

			var foundPoints = searcher.Search(1200050, 2600000, 1200060, 2600010, 0.0).ToList();
			Assert.AreEqual(0, foundPoints.Count);

			foundPoints = searcher.Search(1200000, 2600000, 1200000, 2600000, 0.1).ToList();
			Assert.AreEqual(1, foundPoints.Count);
		}
	}
}

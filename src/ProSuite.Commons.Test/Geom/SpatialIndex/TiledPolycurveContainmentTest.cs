using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Test.Geom.SpatialIndex
{
	[TestFixture]
	public class TiledPolycurveContainmentTest
	{
		private const double _tolerance = 0.001;

		[Test]
		public void CanTellContainedFromNot()
		{
			TiledPolycurveContainment containment = Containment(Square(20, 80));

			Assert.IsTrue(containment.ContainsXY(45, 45), "well inside");
			Assert.IsFalse(containment.ContainsXY(5, 5), "well outside");
		}

		/// <summary>Both answers out of one tile, which is what a tile the boundary crosses has
		/// to be able to give.</summary>
		[Test]
		public void ATileTheBoundaryCrossesAnswersPointByPoint()
		{
			// The polygon's corner sits in the middle of tile [20,30] x [20,30], so that tile
			// holds points on both sides of the boundary.
			TiledPolycurveContainment containment = Containment(Square(25, 80));

			Assert.AreEqual(EnvelopeRelation.Straddling,
			                containment.RelationOf(new TileIndex(2, 2)));

			Assert.IsTrue(containment.ContainsXY(26, 26));
			Assert.IsFalse(containment.ContainsXY(21, 21));
		}

		[Test]
		public void AskingTheSameTileTwiceGivesTheSameAnswer()
		{
			TiledPolycurveContainment containment = Containment(Square(20, 80));

			var tile = new TileIndex(4, 4);

			Assert.AreEqual(containment.RelationOf(tile), containment.RelationOf(tile));
			Assert.AreEqual(EnvelopeRelation.Inside, containment.RelationOf(tile));
		}

		/// <summary>
		/// The contract, over a shape whose boundary crosses most of its tiles: every point gets
		/// the answer it would have got from testing it directly. A point more or fewer here is
		/// a point an edit silently reaches or misses.
		/// </summary>
		[Test]
		public void AgreesWithTheDirectTestEverywhere([Values("diamond", "donut", "square")]
		                                              string shape)
		{
			MultiLinestring polygon = Shape(shape);
			TiledPolycurveContainment containment = Containment(polygon);

			var disagreements = new List<string>();

			for (double x = 0; x <= 100; x += 1.25)
			{
				for (double y = 0; y <= 100; y += 1.25)
				{
					bool expected = GeomRelationUtils.PolycurveContainsXY(
						polygon, new Coordinates2D(x, y), _tolerance);

					if (containment.ContainsXY(x, y) != expected)
					{
						disagreements.Add($"{x}/{y} (direct: {expected})");
					}
				}
			}

			Assert.IsEmpty(disagreements, "points answered differently: {0}",
			               string.Join(", ", disagreements));
		}

		/// <summary>The tiling only groups the points, so it must not change any answer.</summary>
		[Test]
		public void AgreesWithTheDirectTestOnAnyTiling([Values(0.5, 3, 10, 250)] double tileSize)
		{
			MultiLinestring polygon = Shape("diamond");

			var containment = new TiledPolycurveContainment(
				new TilingDefinition(-7.5, 3.25, tileSize, tileSize), polygon, _tolerance);

			for (double x = 0; x <= 100; x += 2.5)
			{
				for (double y = 0; y <= 100; y += 2.5)
				{
					Assert.AreEqual(
						GeomRelationUtils.PolycurveContainsXY(polygon, new Coordinates2D(x, y),
						                                      _tolerance),
						containment.ContainsXY(x, y), $"at {x}/{y}");
				}
			}
		}

		private static TiledPolycurveContainment Containment(ISegmentList polygon)
		{
			return new TiledPolycurveContainment(new TilingDefinition(0, 0, 10, 10), polygon,
			                                     _tolerance);
		}

		private static MultiLinestring Shape(string shape)
		{
			switch (shape)
			{
				case "diamond":
					return GeomTestUtils.CreatePoly(
						new List<Pnt3D>
						{
							new Pnt3D(50, 20, 0),
							new Pnt3D(80, 50, 0),
							new Pnt3D(50, 80, 0),
							new Pnt3D(20, 50, 0)
						});

				case "donut":
					return new RingGroup(GeomTestUtils.CreateRing(Corners(10, 90)),
					                     new[] { GeomTestUtils.CreateRing(Corners(35, 65)) });

				default:
					return Square(20, 80);
			}
		}

		private static MultiLinestring Square(double min, double max)
		{
			return GeomTestUtils.CreatePoly(Corners(min, max));
		}

		private static List<Pnt3D> Corners(double min, double max)
		{
			return new List<Pnt3D>
			       {
				       new Pnt3D(min, min, 0),
				       new Pnt3D(min, max, 0),
				       new Pnt3D(max, max, 0),
				       new Pnt3D(max, min, 0)
			       };
		}
	}
}

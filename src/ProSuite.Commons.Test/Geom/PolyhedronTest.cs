using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class PolyhedronTest
	{
		[Test]
		public void CanGetFootprint()
		{
			// 1----------------2
			// |                |
			// |                |
			// |                |
			// |                |
			// 0/4______________3

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0),
			           };

			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l), 0.01, 1, 5, 100 * 100));
		}

		[Test]
		public void CanGetFootprintWithIsland()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0),
			           };

			var island = new Linestring(new[]
			                            {
				                            new Pnt3D(20, 20, 0),
				                            new Pnt3D(40, 20, 0),
				                            new Pnt3D(40, 40, 0),
				                            new Pnt3D(20, 40, 0),
				                            new Pnt3D(20, 20, 0)
			                            });

			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l, island), 0.01, 2, 10, 100 * 100 - 20 * 20));
		}

		[Test]
		public void CanGetFootprintWithAlmostDuplicateVertex()
		{
			// 1----------------2/3
			// |                |
			// |                |
			// |                |
			// |                |
			// 0________________4

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 100.001, 0),
				           new Pnt3D(100, 0, 0),
			           };

			// The removal of duplicate segments (verticals) also cleans almost-duplicates
			const int expectedFootprintVertices = 5;
			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l), 0.01, 1, expectedFootprintVertices, 100 * 100.0005));
		}

		[Test]
		public void CanGetFootprintFromVerticalNeedle()
		{
			// 1        ^ z
			// |        |
			// 2        |
			// 3        |
			// |        |
			// 0        |

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 0, 100),
				           new Pnt3D(0, 0, 70),
				           new Pnt3D(0, 0, 60),
				           new Pnt3D(0, 0, 0),
			           };

			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l), 0.001, 0, 0, 0));
		}

		[Test]
		public void CanGetFootprintForSelfTangentRingGroup()
		{
			// Ring visits vertex (1,1,4) twice: at position 1 (v1) and 4 (v4).
			// At the rightmost-bottommost vertex v0=(0,0), the entering segment comes from
			// v4.XY=v1.XY, equal to the leaving segment's end → cross-product=0 → CW=null.
			// Bug: the ring is excluded as "vertical"; it should contribute ~4.5 sq m.
			var ringPoints = new List<Pnt3D>
			                 {
				                 new Pnt3D(0, 0, 5),
				                 new Pnt3D(1, 1, 4), // v1 – revisited at v4
				                 new Pnt3D(10, 5, 3),
				                 new Pnt3D(10, 4, 3),
				                 new Pnt3D(1, 1, 4), // v4 = v1 (self-tangent)
			                 };

			Linestring ring = GeomTestUtils.CreateRing(ringPoints);

			Assert.IsNull(ring.ClockwiseOriented,
			              "Precondition: self-tangent ring has indeterminate orientation");
			Assert.AreEqual(4.5, ring.GetArea2D(), 0.001,
			                "Precondition: ring has non-zero XY area");

			Polyhedron polyhedron = CreatePolyhedron(ring);
			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out List<Linestring> verticalRings);

			Assert.AreEqual(0, verticalRings.Count,
			                "Ring with non-zero XY area must not be classified as vertical");
			Assert.Greater(footprint.GetArea2D(), 0, "Footprint should have non-zero area");
		}

		[Test]
		public void CanGetFootprintForOid210Group6()
		{
			// OID 210, Group 6: a 3D ring from a barrel-roof multipatch that visits its
			// second XY vertex at both position 1 and 4 (self-tangent). In XY projection
			// ClockwiseOriented=null → excluded as "vertical ring".
			// Bug: ring has area ≈ 3.676 sq m that is lost from the footprint.
			// Local coordinates (offset: 2546489.9075, 1190047.7025).
			var ringPoints = new List<Pnt3D>
			                 {
				                 new Pnt3D(0.0000, 0.0000, 454.4575),
				                 new Pnt3D(0.1850, 0.0738, 454.4425),
				                 new Pnt3D(11.0900, 4.3600, 453.4987),
				                 new Pnt3D(11.3138, 3.7738, 453.4987),
				                 new Pnt3D(0.1850, 0.0738, 454.4425), // revisits vertex 1
			                 };

			Linestring ring = GeomTestUtils.CreateRing(ringPoints);

			Assert.IsNull(ring.ClockwiseOriented, "Precondition: ring is self-tangent, CW=null");
			Assert.AreEqual(3.676, ring.GetArea2D(), 0.01, "Precondition: non-zero XY area");

			Polyhedron polyhedron = CreatePolyhedron(ring);
			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out List<Linestring> verticalRings);

			Assert.AreEqual(0, verticalRings.Count,
			                "Ring with non-zero area must not be classified as vertical");
			Assert.AreEqual(3.676, footprint.GetArea2D(), 0.01,
			                "Footprint area should match the ring's XY area");
		}

		[Test]
		public void CanGetFootprintForFrameShape()
		{
			// Four rectangular ring groups arranged as a picture-frame (donut).
			// Their XY union should produce a polygon with a central hole.
			// If the union algorithm fails to create the hole, the result is a filled polygon.
			//
			// Frame layout (10 × 10, wall width = 2, hole = 6 × 6 at (2,2)-(8,8)):
			//   +----------+
			//   |  +----+  |
			//   |  |    |  |
			//   |  +----+  |
			//   +----------+
			//
			// Total frame area = 10*10 - 6*6 = 100 - 36 = 64.

			var topStrip = new List<Pnt3D>
			               {
				               new Pnt3D(0, 8, 0), new Pnt3D(0, 10, 0),
				               new Pnt3D(10, 10, 0), new Pnt3D(10, 8, 0),
			               };
			var bottomStrip = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0), new Pnt3D(0, 2, 0),
				                  new Pnt3D(10, 2, 0), new Pnt3D(10, 0, 0),
			                  };
			var leftStrip = new List<Pnt3D>
			                {
				                new Pnt3D(0, 0, 0), new Pnt3D(0, 10, 0),
				                new Pnt3D(2, 10, 0), new Pnt3D(2, 0, 0),
			                };
			var rightStrip = new List<Pnt3D>
			                 {
				                 new Pnt3D(8, 0, 0), new Pnt3D(8, 10, 0),
				                 new Pnt3D(10, 10, 0), new Pnt3D(10, 0, 0),
			                 };

			Polyhedron polyhedron =
				CreateMultiGroupPolyhedron(topStrip, bottomStrip, leftStrip, rightStrip);

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out List<Linestring> verticalRings);

			Assert.AreEqual(0, verticalRings.Count);
			Assert.AreEqual(2, footprint.PartCount,
			                "Frame must produce outer ring + inner hole ring");
			Assert.AreEqual(64.0, footprint.GetArea2D(), 0.001,
			                "Frame area = 10×10 − 6×6 = 64");
		}

		[Test]
		public void CanGetFootprintForDisjointTilesMergedByBridge()
		{
			// Rect1 (right half, CW): x=5-10, y=0-10, area=50
			var pointsRight = new List<Pnt3D>
			                  {
				                  new Pnt3D(5, 0, 0), new Pnt3D(5, 10, 0),
				                  new Pnt3D(10, 10, 0), new Pnt3D(10, 0, 0)
			                  };

			// Rect2 (left half, CW): x=0-4, y=0-10, area=40
			var pointsLeft = new List<Pnt3D>
			                 {
				                 new Pnt3D(0, 0, 0), new Pnt3D(0, 10, 0),
				                 new Pnt3D(4, 10, 0), new Pnt3D(4, 0, 0)
			                 };

			// Bridge (middle strip, CW): x=4-5, y=0-10, area=10
			var pointsBridge = new List<Pnt3D>
			                   {
				                   new Pnt3D(4, 0, 0), new Pnt3D(4, 10, 0),
				                   new Pnt3D(5, 10, 0), new Pnt3D(5, 0, 0)
			                   };

			Polyhedron polyhedron =
				CreateMultiGroupPolyhedron(pointsRight, pointsLeft, pointsBridge);

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out _);

			Assert.AreEqual(100.0, footprint.GetArea2D(), 0.001,
			                "Three tile pieces must union to a 10×10 square (area=100)");
			Assert.AreEqual(1, footprint.PartCount,
			                "Result must be a single polygon");
		}

		[Test]
		public void CanGetFootprintForOid208Blockrand()
		{
			// OID 208: 8 non-overlapping CW ring groups that tile a barrel-roof building
			// footprint. Sum of ring areas ≈ 4074.77 sq m = the AO reference area.
			// Bug: GetUnionAreasXY (sorted by area descending) unions Group 6 and Group 5
			// first; they are disjoint → two separate parts. Subsequent ring groups connect
			// them, but the union of a multi-part source with a connecting target ring
			// produces an incorrect result (area ≈ 7045, too large; 9 points instead of 20).
			// Local coordinates (offset: 2687695.5450, 1250317.3075).

			// Group 0: area≈28.7 (small triangle)
			var group0 = new List<Pnt3D>
			             {
				             new Pnt3D(57.6775, 0.0000, 454.6700),
				             new Pnt3D(36.9338, 4.8075, 454.6700),
				             new Pnt3D(37.5425, 7.4350, 454.6700),
			             };

			// Group 1: area≈181.9 (triangle on the left)
			var group1 = new List<Pnt3D>
			             {
				             new Pnt3D(0.0000, 16.1363, 454.6700),
				             new Pnt3D(17.0650, 89.7688, 454.6700),
				             new Pnt3D(21.7550, 88.6812, 454.6700),
			             };

			// Group 2: area≈842.3 (large polygon near top)
			var group2 = new List<Pnt3D>
			             {
				             new Pnt3D(34.5475, 94.9550, 454.6512),
				             new Pnt3D(21.7550, 88.6812, 454.6700),
				             new Pnt3D(31.8750, 132.3475, 454.6700),
				             new Pnt3D(85.4725, 119.9250, 454.5800),
				             new Pnt3D(69.4125, 112.0500, 454.6025),
				             new Pnt3D(69.5737, 112.7500, 454.6025),
				             new Pnt3D(40.2475, 119.5463, 454.6512),
			             };

			// Group 3: area≈238.5 (connector polygon bridging Group 5 and Group 6)
			var group3 = new List<Pnt3D>
			             {
				             new Pnt3D(57.6775, 0.0000, 454.6700),
				             new Pnt3D(37.5425, 7.4350, 454.6700),
				             new Pnt3D(35.2175, 19.4037, 454.6700),
				             new Pnt3D(47.2912, 16.6050, 454.6700),
				             new Pnt3D(48.6225, 22.3525, 454.6700),
			             };

			// Group 4: area≈49.9 (small triangle)
			var group4 = new List<Pnt3D>
			             {
				             new Pnt3D(21.7550, 88.6812, 454.6700),
				             new Pnt3D(28.9675, 70.8762, 454.6700),
				             new Pnt3D(26.9263, 62.0700, 454.6700),
			             };

			// Group 5: area≈1104.9 (large polygon left side) - disjoint from Group 6
			var group5 = new List<Pnt3D>
			             {
				             new Pnt3D(35.2175, 19.4037, 454.6700),
				             new Pnt3D(37.5425, 7.4350, 454.6700),
				             new Pnt3D(0.0000, 16.1363, 454.6700),
				             new Pnt3D(21.7550, 88.6812, 454.6700),
				             new Pnt3D(26.9263, 62.0700, 454.6700),
				             new Pnt3D(17.9637, 23.4025, 454.6700),
			             };

			// Group 6: area≈1492.0 (largest polygon right side) - disjoint from Group 5
			var group6 = new List<Pnt3D>
			             {
				             new Pnt3D(85.4725, 119.9250, 454.5800),
				             new Pnt3D(57.6775, 0.0000, 454.6700),
				             new Pnt3D(48.6225, 22.3525, 454.6700),
				             new Pnt3D(69.4125, 112.0500, 454.6025),
			             };

			// Group 7: area≈136.5 (small triangle)
			var group7 = new List<Pnt3D>
			             {
				             new Pnt3D(21.7550, 88.6812, 454.6700),
				             new Pnt3D(34.5475, 94.9550, 454.6512),
				             new Pnt3D(28.9675, 70.8775, 454.6700),
			             };

			Polyhedron polyhedron =
				CreateMultiGroupPolyhedron(group0, group1, group2, group3,
				                           group4, group5, group6, group7);

			// Sum of ring areas ≈ 4074.7 ≈ AO footprint area. All rings are non-overlapping.
			double sumOfAreas = polyhedron.RingGroups.Sum(g => g.GetArea2D());
			Assert.AreEqual(4074.77, sumOfAreas, 1.0, "Precondition: sum of ring areas");

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out _);

			Assert.AreEqual(4074.77, footprint.GetArea2D(), 1.0,
			                "Footprint area must match AO reference (≈4074.77 sq m). " +
			                "Buggy result is ≈7045 sq m with only 9 points.");
		}

		[Test]
		public void CanGetFootprintForOid166BarrelRoof()
		{
			// OID 166: four ring groups whose union footprint is wrong because Group 1
			// has ClockwiseOriented=True but GetArea2D()<0 (self-intersecting XY projection).
			// In GetUnionAreasXY groups are sorted by area2D descending; Group 1 sorts last
			// and its contribution (~60.9 sq m) is lost from the union.
			// Expected area ≈ 145.05 sq m (AO reference); current result ≈ 84.20 sq m.
			// Local coordinates (offset: 2699737.70, 1115821.65).

			// Group 0: zero-area degenerate (correctly excluded)
			var group0 = new List<Pnt3D>
			             {
				             new Pnt3D(1.1313, 3.7250, 287.4237),
				             new Pnt3D(-0.0038, 5.0988, 286.9287),
				             new Pnt3D(1.1313, 3.7250, 286.8062),
				             new Pnt3D(1.1313, 3.7250, 287.4237),
			             };

			// Group 1: CW=True but GetArea2D()=-60.854 (self-intersecting) → sorted last
			var group1 = new List<Pnt3D>
			             {
				             new Pnt3D(5.6863, 4.0513, 288.1612),
				             new Pnt3D(6.9400, 2.8437, 287.5350),
				             new Pnt3D(4.2050, 0.0050, 287.3737),
				             new Pnt3D(6.4425, 2.3275, 287.5050),
				             new Pnt3D(7.9275, 0.5337, 286.6625),
				             new Pnt3D(13.8925, 5.4637, 286.6625),
				             new Pnt3D(14.5937, 4.6150, 286.2650),
				             new Pnt3D(19.0425, 8.2900, 286.2650),
				             new Pnt3D(15.6075, 12.2500, 288.1612),
			             };

			// Group 2: CW=True, GetArea2D()=+65.267 → processed first in union
			var group2 = new List<Pnt3D>
			             {
				             new Pnt3D(15.6075, 12.2500, 288.1612),
				             new Pnt3D(5.6863, 4.0513, 288.1612),
				             new Pnt3D(5.3437, 4.3812, 288.0287),
				             new Pnt3D(4.7775, 4.9263, 287.8125),
				             new Pnt3D(3.4875, 6.1700, 287.3175),
				             new Pnt3D(1.1313, 3.7250, 287.4237),
				             new Pnt3D(-0.0038, 5.0988, 286.9287),
				             new Pnt3D(8.3600, 12.0112, 286.9287),
				             new Pnt3D(8.1250, 12.2962, 286.8250),
				             new Pnt3D(12.4600, 15.8787, 286.8250),
			             };

			// Group 3: CW=True, GetArea2D()=+18.932 → processed second in union
			var group3 = new List<Pnt3D>
			             {
				             new Pnt3D(6.9400, 2.8437, 287.7812),
				             new Pnt3D(4.2000, 0.0000, 287.7812),
				             new Pnt3D(0.7475, 3.3263, 287.7812),
				             new Pnt3D(3.4875, 6.1700, 287.7812),
			             };

			Linestring ring1 = GeomTestUtils.CreateRing(new List<Pnt3D>(group1));
			Assert.AreEqual(true, ring1.ClockwiseOriented,
			                "Precondition: Group 1 has CW=True");
			Assert.Less(ring1.GetArea2D(), 0,
			            "Precondition: Group 1 has negative GetArea2D() (self-intersecting)");

			Polyhedron polyhedron =
				CreateMultiGroupPolyhedron(group0, group1, group2, group3);

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out _);

			Assert.AreEqual(145.05, footprint.GetArea2D(), 1.0,
			                "Footprint should match AO reference area (≈145.05 sq m)");
		}

		[Test]
		public void CanUnionIslandInCourtyardWithBoundaryLoop()
		{
			// TLM_GEBAEUDE {AEEB32D7-A07E-4D64-B8A4-994DC51FCF66} (garden_center_giubiasco),
			// isolated union step 246. The accumulated footprint (source) has a pinch /
			// boundary loop on its main outer ring AND, far away, a building (~58 sq m)
			// with a courtyard hole (~-2.29 sq m) that contains a small island (~1.097 sq m).
			// A tiny target triangle is unioned at the pinch. Because the union has a
			// boundary loop, RingOperator.AddUnprocessedRings took its de-duplication path
			// and dropped the island - at the time it tested containment, the courtyard hole
			// had not yet been assigned to the building, so the island looked like a
			// duplicate of the (solid) building. The union area must never decrease when
			// adding a (mostly) disjoint ring.
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"island_in_courtyard_boundary_loop_source.wkb"), out _);
			MultiLinestring target = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"island_in_courtyard_boundary_loop_target.wkb"), out _);

			double tolerance = 0.0005;

			double sourceArea = source.GetArea2D();

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			// The target (~0.39) sits outside the existing footprint, so the union must
			// grow, not shrink. Before the fix this returned ~455.825 (the 1.097 island
			// was lost), now it is ~456.922 (= source + target).
			Assert.GreaterOrEqual(result.GetArea2D(), sourceArea,
			                      "Union with a (mostly) disjoint ring must not lose area.");
			Assert.AreEqual(sourceArea + target.GetArea2D(), result.GetArea2D(), 0.001);

			// The ~1.097 island in the courtyard must still be present in the result.
			Assert.IsTrue(
				result.GetLinestrings().Any(l => l.ClockwiseOriented == true &&
				                                 Math.Abs(l.GetArea2D() - 1.0971) < 0.01),
				"The island inside the courtyard was dropped from the union result.");
		}

		[Test]
		public void CanUnionValleeDeLaJeunesseAtStep93()
		{
			// TLM_GEBAEUDE {EEBB4D4C-756D-4EF2-B85D-2DA1991E2A65} (vallee_de_la_jeunesse),
			// isolated union step 93. The accumulated footprint (source, area ~913.352,
			// 15 parts) is unioned with a small ring (~3.371). This tests the case where the
			// source ring touches multiple interior rings.
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"vallee_jeunesse_union_step93_source.wkb"), out _);
			MultiLinestring ring = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"vallee_jeunesse_union_step93_ring.wkb"), out _);

			double tolerance = 0.0005;

			double sourceArea = source.GetArea2D();
			double ringArea = ring.GetArea2D();

			Console.WriteLine($"source area={sourceArea:F6} parts={source.PartCount}");
			Console.WriteLine($"ring   area={ringArea:F6} parts={ring.PartCount}");

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, ring, tolerance);

			Console.WriteLine($"result area={result.GetArea2D():F6} parts={result.PartCount}");

			Assert.GreaterOrEqual(result.GetArea2D(), sourceArea - 1e-6,
			                      "Union must not lose area when adding a ring.");
		}

		[Test]
		public void CanUnionValleeDeLaJeunesseAtStep24()
		{
			// TLM_GEBAEUDE {EEBB4D4C-756D-4EF2-B85D-2DA1991E2A65} (vallee_de_la_jeunesse),
			// isolated union step 24. A ring (~6.349) that lies completely OUTSIDE the
			// current accumulated footprint must not be dropped by the union (area stays
			// 603.673 instead of growing by 6.349).
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"vallee_jeunesse_union_step24_source.wkb"), out _);
			MultiLinestring ring = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"vallee_jeunesse_union_step24_ring.wkb"), out _);

			double tolerance = 0.0005;
			double sourceArea = source.GetArea2D();
			double ringArea = ring.GetArea2D();

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, ring, tolerance);

			Assert.AreEqual(sourceArea + ringArea, result.GetArea2D(), 0.001,
			                "A fully disjoint ring must be added to the union result.");
		}

		[Test]
		public void CanGetFootprintForGardenCenterGiubiasco()
		{
			// TLM_GEBAEUDE {AEEB32D7-A07E-4D64-B8A4-994DC51FCF66}.
			// The incremental ring-group union previously dropped islands nested inside
			// courtyards at the union steps that involve a boundary loop, losing ~1.25 sq m
			// and leaving two spurious holes. AO reference area: 479.1256.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("garden_center_giubiasco.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.0005;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(479.1256, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanGetFootprintForLabyrinthAventure()
		{
			// TLM_GEBAEUDE labyrinth_aventure multipatch.
			// The XY footprint of this polyhedron is built incrementally by sorting its
			// ring groups by area descending and unioning them one by one (see
			// Polyhedron.GetXYFootprint). Multiple of the union steps (around step 180,
			// 190, 195, 204, 214) trigger boundary-loop / pinch-point edge cases that
			// caused the result to lose holes or self-intersect. Expected area:
			// 655.276735 (AO reference).
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("labyrinth_aventure.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.0005;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(655.276735, footprint.GetArea2D(), 0.001);
		}

		[Test]
		public void CanGetFootprintForValleeDeLaJeunesse()
		{
			// TLM_GEBAEUDE {EEBB4D4C-756D-4EF2-B85D-2DA1991E2A65}.
			// The incremental ring-group union loses area at step 93 (913.3520 ->
			// 899.9141) and leaves spurious holes; it ends ~1028 instead of the AO
			// reference area 1051.300615 (length 118.319908).
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("vallee_de_la_jeunesse.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.0005;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(1051.300615, footprint.GetArea2D(), 0.05);
		}


		[Test]
		public void CanGetFootprintForSebastianskapelle()
		{
			// TLM_GEBAEUDE {0F578F49-4C2D-4CF3-AE2B-898CFED68F83} ("St. Sebastian's
			// chapel"). The multipatch roof is a fan of 10 triangles that ALL meet at one
			// apex A=(2642302.5125,1129570.9625) - a 10-ring pinch point. The XY footprint
			// is built by the area-descending incremental ring-group union. At the
			// production tolerance (resolution/2 = 0.00625) the union at the step that adds
			// the southern triangle (whose two base corners are the building's two
			// southernmost points) mis-handles a sub-tolerance sliver at the apex: the
			// target edge runs linearly along a source edge but overshoots the source's SE
			// corner by ~0.0072 m (just above tolerance). The turning-left walk then drops
			// the triangle and emits a duplicate outer ring, so the footprint becomes two
			// overlapping ~39.13 rings (area 78.26, 2 parts) that ArcGIS Simplify collapses
			// to a single 39.13 ring - the triangle is lost. AO reference area: 42.3496.
			// NB: the bug is razor-thin in tolerance (0.005 -> 42.366, 0.008 -> 42.362 are
			// both fine); only 0.00625 triggers it - which is exactly the production value.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("sebastianskapelle.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(1, footprint.PartCount,
			                "Footprint must be a single ring (no overlapping duplicate).");
			Assert.AreEqual(42.3496, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanUnionSebastianskapelleAtStep6()
		{
			// Isolated step 6 of the Sebastianskapelle incremental footprint union
			// (tolerance 0.00625). The accumulated footprint (source) has two CW parts that
			// only touch at the apex pinch A: the big part (~32.57) and one southern
			// triangle (~3.40). The target ring (~3.23) is the next southern triangle; its
			// edge A->P2 coincides with the small part's edge and near its other corner P1
			// it overshoots the big part's SE corner by a sub-tolerance sliver. The union
			// must (a) stay a single, simple set of non-overlapping outer rings and (b) grow
			// by the target's disjoint area. The bug instead duplicates the big part's
			// outline (two overlapping CW rings, area jumps 35.96 -> 68.51) and loses the
			// triangle.
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"sebastianskapelle_step6_source.wkb"), out _);
			MultiLinestring ring = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"sebastianskapelle_step6_ring.wkb"), out _);

			double tolerance = 0.00625;
			double sourceArea = source.GetArea2D();

			MultiLinestring outside =
				GeomTopoOpUtils.GetDifferenceAreasXY(ring, source, tolerance);
			double disjoint = outside.GetArea2D();

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, ring, tolerance);

			// The union must grow by ~the target's disjoint area and must not produce
			// overlapping duplicate outer rings. The buggy result is ~68.51; the tolerance
			// allows for a sub-cm sliver residual at the near-coincident apex corner (the
			// resolution at this tolerance is 0.0125).
			Assert.AreEqual(sourceArea + disjoint, result.GetArea2D(), 0.05,
			                "Union must add the target's disjoint area, not duplicate the " +
			                "source outline.");
		}

		private static Polyhedron CreatePolyhedron(Linestring linestring,
		                                           params Linestring[] islands)
		{
			var ringGroup = new RingGroup(linestring);

			foreach (Linestring island in islands)
			{
				ringGroup.AddInteriorRing(island);
			}

			return new Polyhedron(new List<RingGroup>(new[] { ringGroup }));
		}

		private static Polyhedron CreateMultiGroupPolyhedron(params IList<Pnt3D>[] ringGroupPoints)
		{
			var ringGroups = new List<RingGroup>();

			foreach (IList<Pnt3D> points in ringGroupPoints)
			{
				Linestring ring = GeomTestUtils.CreateRing(new List<Pnt3D>(points));
				ringGroups.Add(new RingGroup(ring));
			}

			return new Polyhedron(ringGroups);
		}

		private static void AssertCanCreateFootprint(Polyhedron polyhedron,
		                                             double tolerance,
		                                             int expectedPartCount,
		                                             int expectedPointCount,
		                                             double expectedArea)
		{
			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out List<Linestring> verticalRings);

			Assert.AreEqual(polyhedron.Count, footprint.Count + verticalRings.Count);
			Assert.NotNull(footprint);

			Assert.True(footprint.GetLinestrings().All(l => l.IsClosed));

			if (! footprint.IsEmpty)
			{
				Assert.True(footprint.GetLinestrings().FirstOrDefault()?.ClockwiseOriented);
			}

			Assert.AreEqual(expectedPartCount, footprint.PartCount);
			Assert.AreEqual(expectedPointCount, footprint.PointCount);
			Assert.AreEqual(expectedArea, footprint.GetArea2D(), 0.001);
		}

		private static void WithRotatedRing(IList<Pnt3D> ring,
		                                    Action<Linestring> proc)
		{
			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);

				Linestring linestring = GeomTestUtils.CreateRing(array1.ToList());

				proc(linestring);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
		public void CanGetFootprintForDennlerstrasse()
		{
			// TLM_GEBAEUDE {14B28567-ABDD-44D2-AA75-5428C7FFC58D} (Dennlerstrasse).
			// One ring group (group 8) carries two interior rings. One of them is a
			// real, coplanar hole in the face; the other is 2.69 m off the face's
			// best-fit plane, i.e. a different face mis-stored as an inner ring. This
			// violates the OGC simple-polyhedron definition (an interior ring must be
			// coplanar with its exterior ring). Subtracting that non-coplanar inner
			// ring punched a spurious 6.61 sq m hole into the footprint (area dropped
			// to 172.264). The non-coplanar interior ring must be ignored, leaving a
			// single, solid footprint of 178.8757.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("dennlerstrasse.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.0005;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(1, footprint.PartCount, "footprint must not have a hole");
			Assert.AreEqual(178.875676, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanGetFootprintIgnoringNonCoplanarInteriorRing()
		{
			// A flat 100x100 exterior face (z=0) with two interior rings:
			//  - a real, coplanar 20x20 hole at z=0 (must be subtracted), and
			//  - a 10x10 ring lifted to z=50, i.e. 50 m off the exterior plane, which
			//    is a different face mis-stored as an inner ring and must be ignored
			//    instead of punching a spurious hole into the footprint.
			var exterior = new Linestring(new[]
			                              {
				                              new Pnt3D(0, 0, 0),
				                              new Pnt3D(0, 100, 0),
				                              new Pnt3D(100, 100, 0),
				                              new Pnt3D(100, 0, 0),
				                              new Pnt3D(0, 0, 0)
			                              });

			var coplanarHole = new Linestring(new[]
			                                  {
				                                  new Pnt3D(20, 20, 0),
				                                  new Pnt3D(40, 20, 0),
				                                  new Pnt3D(40, 40, 0),
				                                  new Pnt3D(20, 40, 0),
				                                  new Pnt3D(20, 20, 0)
			                                  });

			var nonCoplanarHole = new Linestring(new[]
			                                     {
				                                     new Pnt3D(60, 60, 50),
				                                     new Pnt3D(70, 60, 50),
				                                     new Pnt3D(70, 70, 50),
				                                     new Pnt3D(60, 70, 50),
				                                     new Pnt3D(60, 60, 50)
			                                     });

			var ringGroup = new RingGroup(
				exterior, new[] { coplanarHole, nonCoplanarHole });
			var polyhedron = new Polyhedron(new List<RingGroup> { ringGroup });

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.01, 0.01, out _);

			// Exterior minus the coplanar hole only; the non-coplanar ring is ignored.
			Assert.AreEqual(2, footprint.PartCount);
			Assert.AreEqual(100 * 100 - 20 * 20, footprint.GetArea2D(), 0.001);
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

		[Test]
		public void CanGetUnionAreasXYWithoutException()
		{
			// Repro for a GetUnionAreasXY crash logged to
			// %TEMP%\GetUnionAreasXY_20260602_230931_515 during production.
			// The union threw a GeomException; the test asserts it completes without
			// error and that the result area is at least as large as the source area.
			// Feature-class tolerance: 1 cm (resolution 1 mm).
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("union_crash_repro_source.wkb"), out _);
			MultiLinestring target = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("union_crash_repro_target.wkb"), out _);

			const double tolerance = 0.0005;

			double sourceArea = source.GetArea2D();

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			Assert.GreaterOrEqual(result.GetArea2D(), sourceArea - 1e-6,
			                      "Union must not lose area.");
		}

		[Test]
		public void CanGetUnionAreasXYForClusterCrashRepro()
		{
			// Minimal license-free repro of the failing_polyhedron footprint crash
			// (AO CreateFootprintUtilsTest.AnalyzeMultipatchGeometry). Extracted as the
			// single failing pairwise GetUnionAreasXY step of that multipatch's footprint.
			// The source has a near-degenerate spike (vertices 11,12,13) whose two flanking
			// linear-intersection points reference the same target vertex but sit 0.00065 m
			// apart - just beyond the 0.0005 tolerance, so the along-target vertex check did
			// not group them and the turning-left walk entered a 3-node cycle, throwing
			// "Intersections seen twice". Now FIXED: the cluster gate also fires on XY
			// proximity (within Sqrt(2)*tolerance), so RingOperator clustering snaps the
			// flanks and removes the collapsed spike before navigation. (Same mechanism as
			// the Thanhalten needle, but here a pairwise union below the footprint level -
			// which is why the cluster+clean lives in RingOperator, not in Polyhedron.)
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("cluster_crash_repro_source.wkb"), out _);
			MultiLinestring target = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("cluster_crash_repro_target.wkb"), out _);

			const double tolerance = 0.0005;

			double sourceArea = source.GetArea2D();

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			Assert.GreaterOrEqual(result.GetArea2D(), sourceArea - 1e-6,
			                      "Union must not lose area.");
		}

		[Test]
		[Ignore("diagnostic")]
		public void DumpClusterCrashReproIntersections()
		{
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("cluster_crash_repro_source.wkb"), out _);
			MultiLinestring target = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("cluster_crash_repro_target.wkb"), out _);

			var ci = CultureInfo.InvariantCulture;
			const double tolerance = 0.0005;

			Console.WriteLine($"source segs={source.GetLinestring(0).SegmentCount} " +
			                  $"area={source.GetArea2D():F4}; target segs={target.GetLinestring(0).SegmentCount} " +
			                  $"area={target.GetArea2D():F4}");

			IList<IntersectionPoint3D> ips = GeomTopoOpUtils.GetIntersectionPoints(
				(ISegmentList) source, target, tolerance);

			Console.WriteLine($"=== {ips.Count} intersection points ===");
			int i = 0;
			foreach (IntersectionPoint3D ip in ips)
			{
				Console.WriteLine(string.Format(
					                  ci,
					                  "[{0,2}] {1,-26} srcV={2,8:F4} tgtV={3,8:F4} pt=({4:F4},{5:F4})",
					                  i++, ip.Type, ip.VirtualSourceVertex, ip.VirtualTargetVertex,
					                  ip.Point.X, ip.Point.Y));
			}

			Console.WriteLine("--- source segments near the spike ---");
			Linestring s = source.GetLinestring(0);
			for (int sx = 9; sx < Math.Min(16, s.SegmentCount); sx++)
			{
				Line3D seg = s.GetSegment(sx);
				Console.WriteLine(string.Format(
					                  ci,
					                  "  seg[{0,2}] ({1:F4},{2:F4})->({3:F4},{4:F4}) len={5:F5}",
					                  sx, seg.StartPoint.X, seg.StartPoint.Y, seg.EndPoint.X,
					                  seg.EndPoint.Y,
					                  seg.Length2D));
			}
		}

		[Test]
		public void CanUnionChampPittetAtStep32()
		{
			// TLM_GEBAEUDE {BE9C9B3B-B966-4719-94F3-EEC3093F2ACC} (Champ Pittet),
			// isolated incremental-union step 32 (tolerance 0.00625). The accumulated
			// footprint (source) is an outer ring with two interior holes; one hole
			// pinches the exterior boundary at point P. The target triangle sits in the
			// exterior pocket and touches the source ONLY at P, so it is fully disjoint
			// (its whole 2.254 sq m lies outside the source). One target edge runs
			// near-coincident with a source exterior edge, diverging from 0 at P to
			// 0.0123 m at the far corner - i.e. it crosses the 0.00625 tolerance around
			// mid-segment. The union must add the disjoint target (area grows by ~2.254);
			// the bug dropped it (area unchanged at 86.967) because the containment test
			// picked a mid-segment target point that landed within tolerance of the
			// source boundary and was read as "contained".
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"champ_pittet_step32_source.wkb"), out _);
			MultiLinestring ring = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"champ_pittet_step32_ring.wkb"), out _);

			double tolerance = 0.00625;
			double sourceArea = source.GetArea2D();

			MultiLinestring disjoint =
				GeomTopoOpUtils.GetDifferenceAreasXY(ring, source, tolerance);

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, ring, tolerance);

			Assert.AreEqual(sourceArea + disjoint.GetArea2D(), result.GetArea2D(), 0.001,
			                "Union must add the fully disjoint target ring's area.");
		}

		[Test]
		public void CanGetFootprintForChampPittet()
		{
			// TLM_GEBAEUDE {BE9C9B3B-B966-4719-94F3-EEC3093F2ACC} (Champ Pittet).
			// At the production tolerance (0.00625) the incremental ring-group union lost
			// ~2.27 sq m at step 32: a fully disjoint triangle that touches the
			// accumulated footprint at a single pinch point (where an interior hole meets
			// the exterior boundary) was dropped. The triangle shares a near-coincident
			// edge with a source exterior edge (gap crosses tolerance mid-segment), and the
			// touch-point containment test (AreTouchingExteriorAndInteriorRings ->
			// PolycurveContainsXY) picked a mid-segment target point that landed within
			// tolerance of the source boundary, reading it as "contained" -> the disjoint
			// ring was discarded. See CanUnionChampPittetAtStep32 for the isolated step.
			// AO reference area: 95.9063.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("champ_pittet.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(95.9063, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanGetFootprintForChateauDeDardagny()
		{
			// TLM_GEBAEUDE {75206440-317A-4132-8EA2-62FAF8DB7E90} (Chateau de Dardagny).
			// Two of the eight ring groups are figure-8 (bowtie) rings: a single ring with
			// a 0-dimensional (crossing) self-intersection that splits it into two lobes of
			// opposite orientation. The initial cleanup only removed LINEAR self-intersections
			// (spikes), so the bowtie passed through as one ring whose signed area is the
			// difference of the two lobes. One such ring (net signed area -8.76) was reversed
			// by AsProperlyOriented and then unioned in as a spurious hole, so the footprint
			// came out 44.134 (2 parts: outer 52.897 + hole -8.762) instead of the solid
			// AO reference 52.8966. Fix: the cleanup now also explodes self-crossing rings
			// into separate simple rings (Polyhedron.CleanRing -> TryCrackSelfCrossingRing).
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("chateau_de_dardagny.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(52.8966, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		public void CanGetFootprintForGrancy()
		{
			// TLM_GEBAEUDE {71DAC71B-9971-4612-B40F-5AC8DB201DCB} (Grancy).
			// The footprint had a missing part. AO reference area: 445.7855.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("grancy.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(445.7855, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		public void CanGetFootprintForThanhalten()
		{
			// TLM_GEBAEUDE {823CB5A8-2104-46C9-9851-3BBD1F88CA41} (Thanhalten).
			// The footprint had a missing part. AO reference area: 269.4976.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("thanhalten.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(269.4976, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		public void CanGetFootprintForBrutalismusInDuedingen()
		{
			// TLM_GEBAEUDE {41C4C3B5-9706-40DF-9D31-2ED609986C69} (Brutalismus in Duedingen).
			// The footprint is incorrect (missing part): the incremental ring-group union
			// over the 201 ring groups returns 779.1132 sq m in 2 parts instead of the solid
			// AO reference 783.9297 (1 part). The inflated boundary length (150.89 vs AO
			// 139.39) indicates a spurious hole / extra boundary loop, and the union logs
			// "Multiple boundary loops or otherwise unexpected self-intersections".
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("brutalismus_in_duedingen.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(783.9297, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		public void CanGetFootprintForHotelWaldhorn()
		{
			// TLM_GEBAEUDE {C95A3876-F4BC-4B09-AFAF-D3A224AE0171} (Hotel Waldhorn).
			// The footprint was incorrect (missing part): the incremental ring-group union
			// over the 20 ring groups returned 313.5880 sq m in 2 parts instead of the solid
			// AO reference 438.4846 (1 part) - ~125 sq m short.
			// The step-6 corner-touch drop is fixed; the area is correct but a tiny
			// spurious hole still splits the result into 2 parts (see [Ignore] note).
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("hotel_waldhorn.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(438.4846, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		public void CanGetFootprintForKirchwegTurgi()
		{
			// TLM_GEBAEUDE {D5FBCA73-C731-4895-AF38-7A42F383CEF4} (Kirchweg Turgi).
			// The footprint is incorrect (missing part): the incremental ring-group union
			// over the 9 ring groups returns 200.8778 sq m in 2 parts instead of the solid
			// AO reference 199.7703 (1 part). The inflated boundary length (64.35 vs AO
			// 57.96) reveals a spurious hole even though the net area is close.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("kirchweg_turgi.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(199.7703, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		[Ignore("Repro Test, to be fixed")]
		public void CanGetFootprintForFriedhofsmauerRoggwil()
		{
			// TLM_GEBAEUDE {5AB47BFF-2612-4B11-8FE0-4FCB123519C4} (Friedhofsmauer Roggwil).
			// The footprint is incorrect (missing part): the incremental ring-group union
			// over the 4 ring groups returns 34.5647 sq m in 2 parts instead of the solid
			// AO reference 58.8638 (1 part, confirmed by the user) - the footprint loses
			// ~24 sq m (~41%). This thin, elongated wall structure is the smallest fixture
			// (4 ring groups) and a good minimal repro.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("friedhofsmauer_roggwil.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(58.8638, footprint.GetArea2D(), 0.05);
			Assert.AreEqual(1, footprint.PartCount);
		}

		[Test]
		[Ignore("Diagnostic: writes current footprint area/parts to C:\\Temp")]
		public void DumpNewFootprintCases()
		{
			var sb = new StringBuilder();
			foreach (string name in new[]
			                        {
				                        "brutalismus_in_duedingen", "hotel_waldhorn",
				                        "kirchweg_turgi", "friedhofsmauer_roggwil"
			                        })
			{
				var polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
					GeomTestUtils.GetGeometryTestDataPath(name + ".wkb"),
					out WkbGeometryType wkbType);

				double tolerance = 0.00625;
				MultiLinestring footprint =
					polyhedron.GetXYFootprint(tolerance, tolerance, out _);

				sb.AppendLine(string.Format(
					              CultureInfo.InvariantCulture,
					              "{0}: wkbType={1} ringGroups={2} footprintArea={3} parts={4}",
					              name, wkbType, polyhedron.RingGroups.Count,
					              footprint.GetArea2D()
					                       .ToString("F4", CultureInfo.InvariantCulture),
					              footprint.PartCount));

				for (int i = 0; i < footprint.PartCount; i++)
				{
					Linestring part = footprint.GetPart(i);
					sb.AppendLine(string.Format(
						              CultureInfo.InvariantCulture,
						              "   part {0}: area2D={1} pointCount={2} closed={3}",
						              i,
						              part.GetArea2D().ToString("F4", CultureInfo.InvariantCulture),
						              part.PointCount, part.IsClosed));
				}
			}

			File.WriteAllText(@"C:\Temp\new_footprint_diag.txt", sb.ToString());
		}

		[Test]
		[Ignore("Diagnostic: per-step incremental union dump (replicates GetXYFootprint " +
		        "pipeline incl. pre-clean, merge tolerance and boundary-loop explode) for " +
		        "the four new failing cases. Writes C:\\Temp\\nf_<name>.txt and WKBs for " +
		        "loss/inflate steps.")]
		public void DumpNewFootprintCaseUnionSteps()
		{
			foreach (string name in new[]
			                        {
				                        "brutalismus_in_duedingen", "hotel_waldhorn",
				                        "kirchweg_turgi", "friedhofsmauer_roggwil"
			                        })
			{
				DumpFootprintUnionSteps(name, 0.00625);
			}
		}

		private static void DumpFootprintUnionSteps(string name, double tolerance)
		{
			var ci = CultureInfo.InvariantCulture;
			var sb = new StringBuilder();

			var polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(name + ".wkb"), out _);

			List<RingGroup> cleanedGroups =
				GetCleanedRingGroupsToUnionize(polyhedron, tolerance, out int verticalCount);

			sb.AppendLine(string.Format(
				              ci, "{0}: rawGroups={1} cleanedGroups={2} verticalRings={3}",
				              name, polyhedron.RingGroups.Count, cleanedGroups.Count,
				              verticalCount));

			var explodeMethod = typeof(GeomTopoOpUtils).GetMethod(
				"ExplodeExteriorBoundaryLoops",
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(explodeMethod);

			MultiLinestring result = null;
			int count = 0;
			foreach (RingGroup rg in cleanedGroups.OrderByDescending(r => r.GetArea2D()))
			{
				if (result == null)
				{
					result = rg.Clone();
					sb.AppendLine(string.Format(ci, "start area={0:F6} parts={1}",
					                            result.GetArea2D(), result.PartCount));
					continue;
				}

				count++;
				double before = result.GetArea2D();

				MultiLinestring disjointPart =
					GeomTopoOpUtils.GetDifferenceAreasXY(rg, result, tolerance);
				double disjoint = disjointPart.GetArea2D();

				MultiLinestring union;
				try
				{
					union = GeomTopoOpUtils.GetUnionAreasXY(result, rg, tolerance, tolerance);
					explodeMethod.Invoke(null, new object[] { union, tolerance });
				}
				catch (Exception e)
				{
					sb.AppendLine(string.Format(ci, "step {0,3}: EXCEPTION {1}", count,
					                            e.InnerException?.Message ?? e.Message));
					GeomUtils.ToWkbFile(result, $@"C:\Temp\nf_{name}_source_{count}.wkb");
					GeomUtils.ToWkbFile(rg, $@"C:\Temp\nf_{name}_ring_{count}.wkb");
					continue;
				}

				double after = union.GetArea2D();
				double grew = after - before;

				string flag = grew < disjoint - 1e-3 ? "  <<< LOSS"
				              : grew > disjoint + 1e-3 ? "  <<< INFLATE"
				              : "";

				sb.AppendLine(string.Format(ci,
				                            "step {0,3}: {1:F6} -> {2:F6} grew={3:F6} " +
				                            "ringArea={4:F6} disjoint={5:F6} parts {6}->{7}{8}",
				                            count, before, after, grew,
				                            rg.GetArea2D(), disjoint,
				                            result.PartCount, union.PartCount, flag));

				if (flag.Length > 0)
				{
					GeomUtils.ToWkbFile(result, $@"C:\Temp\nf_{name}_source_{count}.wkb");
					GeomUtils.ToWkbFile(rg, $@"C:\Temp\nf_{name}_ring_{count}.wkb");
					sb.AppendLine($"  -> wrote C:\\Temp\\nf_{name}_source/ring_{count}.wkb");

					var nav = new SubcurveNavigator(result, rg, tolerance);
					sb.AppendLine($"  HasBoundaryLoops={nav.HasBoundaryLoops()} " +
					              $"IPs={nav.IntersectionPoints.Count}");
					foreach (IntersectionPoint3D ip in nav.IntersectionPoints)
					{
						sb.AppendLine(string.Format(ci,
						                            "    ip type={0} srcP={1} srcV={2:F4} " +
						                            "tgtV={3:F4} pt=({4:F4},{5:F4})",
						                            ip.Type, ip.SourcePartIndex,
						                            ip.VirtualSourceVertex,
						                            ip.VirtualTargetVertex,
						                            ip.Point.X, ip.Point.Y));
					}

					foreach (Linestring r in union.GetLinestrings())
					{
						sb.AppendLine(string.Format(ci,
						                            "  result ring area={0:F6} cw={1} " +
						                            "env=({2:F4},{3:F4})-({4:F4},{5:F4})",
						                            r.GetArea2D(), r.ClockwiseOriented,
						                            r.XMin, r.YMin, r.XMax, r.YMax));
					}
				}

				result = union;
			}

			sb.AppendLine(string.Format(ci, "FINAL area={0:F6} parts={1}",
			                            result?.GetArea2D() ?? 0, result?.PartCount ?? 0));

			File.WriteAllText($@"C:\Temp\nf_{name}.txt", sb.ToString());
		}

		[Test]
		[Ignore("Diagnostic: per-failing-step navigator/walk dump for the four new cases. " +
		        "Requires the WKBs written by DumpNewFootprintCaseUnionSteps in C:\\Temp.")]
		public void DumpNewFootprintFailingStepDetails()
		{
			var cases = new (string name, int step)[]
			            {
				            ("friedhofsmauer_roggwil", 1),
				            ("kirchweg_turgi", 4),
				            ("hotel_waldhorn", 6),
				            ("brutalismus_in_duedingen", 65)
			            };

			var ci = CultureInfo.InvariantCulture;

			foreach ((string name, int step) in cases)
			{
				var sb = new StringBuilder();

				var source = (MultiLinestring) GeomUtils.FromWkbFile(
					$@"C:\Temp\nf_{name}_source_{step}.wkb", out _);
				var target = (MultiLinestring) GeomUtils.FromWkbFile(
					$@"C:\Temp\nf_{name}_ring_{step}.wkb", out _);

				double tolerance = 0.00625;

				sb.AppendLine($"=== {name} step {step} ===");
				sb.AppendLine(string.Format(ci, "source: parts={0} area={1:F6}",
				                            source.PartCount, source.GetArea2D()));
				for (int i = 0; i < source.PartCount; i++)
				{
					Linestring p = source.GetPart(i);
					sb.AppendLine(string.Format(
						              ci, "  srcPart {0}: area={1:F6} cw={2} points={3}",
						              i, p.GetArea2D(), p.ClockwiseOriented, p.PointCount));
				}

				sb.AppendLine(string.Format(ci, "target: parts={0} area={1:F6}",
				                            target.PartCount, target.GetArea2D()));
				foreach (Pnt3D pt in target.GetPoints())
				{
					sb.AppendLine(string.Format(ci, "  tgt ({0:F4},{1:F4},{2:F4})",
					                            pt.X, pt.Y, pt.Z));
				}

				// Vertices of source parts involved in intersections (keep it short):
				var nav = new SubcurveNavigator(source, target, tolerance);
				var involvedParts = new SortedSet<int>(
					nav.IntersectionPoints.Select(ip => ip.SourcePartIndex));

				foreach (int partIdx in involvedParts)
				{
					Linestring p = source.GetPart(partIdx);
					sb.AppendLine($"  srcPart {partIdx} vertices:");
					foreach (Pnt3D pt in p.GetPoints())
					{
						sb.AppendLine(string.Format(ci, "    ({0:F4},{1:F4},{2:F4})",
						                            pt.X, pt.Y, pt.Z));
					}
				}

				sb.AppendLine($"HasBoundaryLoops={nav.HasBoundaryLoops()}");
				foreach (IntersectionPoint3D ip in nav.IntersectionPoints)
				{
					sb.AppendLine(string.Format(
						              ci,
						              "  ip type={0} srcP={1} srcV={2:F4} tgtP={3} tgtV={4:F4} pt=({5:F4},{6:F4})",
						              ip.Type, ip.SourcePartIndex, ip.VirtualSourceVertex,
						              ip.TargetPartIndex, ip.VirtualTargetVertex,
						              ip.Point.X, ip.Point.Y));
				}

				try
				{
					IList<Linestring> walked = nav.FollowSubcurvesTurningLeft().ToList();
					sb.AppendLine($"turning-left walk: {walked.Count} rings");
					foreach (Linestring r in walked)
					{
						sb.AppendLine(string.Format(
							              ci, "  walked ring area={0:F6} cw={1} points={2}",
							              r.GetArea2D(), r.ClockwiseOriented, r.PointCount));
					}
				}
				catch (Exception e)
				{
					sb.AppendLine($"turning-left walk EXCEPTION: {e.Message}");
				}

				// And the real union result:
				try
				{
					MultiLinestring union =
						GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance, tolerance);
					sb.AppendLine(string.Format(ci, "union: area={0:F6} parts={1}",
					                            union.GetArea2D(), union.PartCount));
					foreach (Linestring r in union.GetLinestrings())
					{
						sb.AppendLine(string.Format(
							              ci, "  union ring area={0:F6} cw={1}",
							              r.GetArea2D(), r.ClockwiseOriented));
					}
				}
				catch (Exception e)
				{
					sb.AppendLine($"union EXCEPTION: {e.Message}");
				}

				File.WriteAllText($@"C:\Temp\nf_{name}_step{step}_detail.txt", sb.ToString());
			}
		}

		[Test]
		[Ignore("Diagnostic: locates the source ring dropped at brutalismus step 65. " +
		        "Requires the WKBs written by DumpNewFootprintCaseUnionSteps in C:\\Temp.")]
		public void DumpBrutalismusDroppedRing()
		{
			// Why is the unprocessed 4.8166 source ring dropped at step 65?
			var ci = CultureInfo.InvariantCulture;
			var sb = new StringBuilder();

			var source = (MultiLinestring) GeomUtils.FromWkbFile(
				@"C:\Temp\nf_brutalismus_in_duedingen_source_65.wkb", out _);
			var target = (MultiLinestring) GeomUtils.FromWkbFile(
				@"C:\Temp\nf_brutalismus_in_duedingen_ring_65.wkb", out _);

			double tolerance = 0.00625;

			Linestring dropped = source.GetLinestrings()
			                           .First(l => Math.Abs(l.GetArea2D() - 4.816588) < 1e-4);

			sb.AppendLine("dropped ring vertices:");
			foreach (Pnt3D pt in dropped.GetPoints())
			{
				sb.AppendLine(string.Format(ci, "  ({0:F4},{1:F4},{2:F4})", pt.X, pt.Y, pt.Z));
			}

			// The walked ring = union of srcPart3, srcPart10 and the target:
			Linestring part3 = source.GetLinestrings()
			                         .First(l => Math.Abs(l.GetArea2D() - 58.096884) < 1e-4);
			Linestring part10 = source.GetLinestrings()
			                          .First(l => Math.Abs(l.GetArea2D() - 4.887042) < 1e-4);

			var walkInput = new MultiPolycurve(new[] { part3.Clone(), part10.Clone() });
			MultiLinestring walked =
				GeomTopoOpUtils.GetUnionAreasXY(walkInput, target, tolerance, tolerance);

			sb.AppendLine(string.Format(ci, "walked: area={0:F6} parts={1}",
			                            walked.GetArea2D(), walked.PartCount));

			var droppedPoly = new MultiPolycurve(new[] { dropped.Clone() });

			MultiLinestring notCovered =
				GeomTopoOpUtils.GetDifferenceAreasXY(droppedPoly, walked, tolerance);
			sb.AppendLine(string.Format(ci, "dropped not covered by walked: {0:F6}",
			                            notCovered.GetArea2D()));

			foreach (Linestring walkedRing in walked.GetLinestrings())
			{
				var rg = new RingGroup(walkedRing.Clone());
				bool? contains =
					GeomRelationUtils.AreaContainsXY(rg, dropped, tolerance);
				sb.AppendLine(string.Format(
					              ci, "AreaContainsXY(walkedRing {0:F6}, dropped) = {1}",
					              walkedRing.GetArea2D(), contains));
			}

			File.WriteAllText(@"C:\Temp\nf_brutalismus_dropped_ring.txt", sb.ToString());
		}

		[Test]
		[Ignore("Diagnostic: probes the AddUnprocessedRings predicates for the island " +
		        "dropped at brutalismus step 65 (island-in-courtyard with all vertices " +
		        "on the hole boundary).")]
		public void DumpBrutalismusStep65Predicates()
		{
			// Rebuild the clean accumulated union up to step 64 (no WKB round-trip) and
			// probe the AddUnprocessedRings predicates for the island that gets dropped.
			var ci = CultureInfo.InvariantCulture;
			var sb = new StringBuilder();

			var polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("brutalismus_in_duedingen.wkb"), out _);

			double tolerance = 0.00625;

			List<RingGroup> cleanedGroups =
				GetCleanedRingGroupsToUnionize(polyhedron, tolerance, out _);

			var explodeMethod = typeof(GeomTopoOpUtils).GetMethod(
				"ExplodeExteriorBoundaryLoops",
				BindingFlags.NonPublic | BindingFlags.Static);

			MultiLinestring result = null;
			RingGroup step65Target = null;
			int count = 0;
			foreach (RingGroup rg in cleanedGroups.OrderByDescending(r => r.GetArea2D()))
			{
				if (result == null)
				{
					result = rg.Clone();
					continue;
				}

				count++;

				if (count == 65)
				{
					step65Target = rg;
					break;
				}

				result = GeomTopoOpUtils.GetUnionAreasXY(result, rg, tolerance, tolerance);
				explodeMethod.Invoke(null, new object[] { result, tolerance });
			}

			Assert.NotNull(step65Target);

			sb.AppendLine(string.Format(ci, "source at step 65: parts={0} area={1:F6}",
			                            result.PartCount, result.GetArea2D()));
			foreach (Linestring p in result.GetLinestrings())
			{
				sb.AppendLine(string.Format(
					              ci,
					              "  part area={0:F6} cw={1} env=({2:F4},{3:F4})-({4:F4},{5:F4})",
					              p.GetArea2D(), p.ClockwiseOriented,
					              p.XMin, p.YMin, p.XMax, p.YMax));
			}

			Linestring island = result.GetLinestrings()
			                          .First(l => Math.Abs(l.GetArea2D() - 4.816588) < 1e-3);
			Linestring hole = result.GetLinestrings()
			                        .First(l => l.ClockwiseOriented == false);
			Linestring bigRing = result.GetLinestrings()
			                           .First(l => Math.Abs(l.GetArea2D() - 306.195279) < 1e-3);

			var ringContains = typeof(RingOperator).GetMethod(
				"RingContains",
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(ringContains);

			var holeContainsIsland = (bool) ringContains.Invoke(
				null, new object[] { hole, island, tolerance });
			sb.AppendLine($"RingContains(hole, island) = {holeContainsIsland}");

			var bigGroup = new RingGroup(bigRing.Clone());
			bool? bigContainsIsland =
				GeomRelationUtils.AreaContainsXY(bigGroup, island, tolerance);
			sb.AppendLine($"AreaContainsXY(bigRing alone, island) = {bigContainsIsland}");

			// per-point conclusive test against the hole:
			foreach (Pnt3D pt in island.GetPoints())
			{
				bool? c = GeomRelationUtils.AreaContainsXY(hole, pt, tolerance, true);
				sb.AppendLine(string.Format(ci, "  AreaContainsXY(hole, ({0:F4},{1:F4})) = {2}",
				                            pt.X, pt.Y, c == null ? "null" : c.ToString()));
			}

			// Now the union and whether the island survives:
			MultiLinestring union =
				GeomTopoOpUtils.GetUnionAreasXY(result, step65Target, tolerance, tolerance);
			sb.AppendLine(string.Format(ci, "union: area={0:F6} parts={1}",
			                            union.GetArea2D(), union.PartCount));
			bool islandSurvives = union.GetLinestrings()
			                           .Any(l => Math.Abs(l.GetArea2D() - 4.816588) < 1e-3);
			sb.AppendLine($"island survives union: {islandSurvives}");

			File.WriteAllText(@"C:\Temp\nf_brutalismus_step65_predicates.txt", sb.ToString());
		}

		/// <summary>
		/// Replicates the private pre-clean part of <see cref="Polyhedron.GetXYFootprint"/>
		/// (RemoveNonCoplanarInteriorRings + SimplifyRings + AsProperlyOriented) via
		/// reflection, so the per-step dump operates on the same ring groups as production.
		/// </summary>
		private static List<RingGroup> GetCleanedRingGroupsToUnionize(
			Polyhedron polyhedron, double tolerance, out int verticalRingCount)
		{
			const BindingFlags flags =
				BindingFlags.NonPublic | BindingFlags.Static;

			var removeNonCoplanar =
				typeof(Polyhedron).GetMethod("RemoveNonCoplanarInteriorRings", flags);
			var simplifyRings = typeof(Polyhedron).GetMethod("SimplifyRings", flags);
			var asOriented = typeof(Polyhedron).GetMethod("AsProperlyOriented", flags);
			Assert.NotNull(removeNonCoplanar);
			Assert.NotNull(simplifyRings);
			Assert.NotNull(asOriented);

			verticalRingCount = 0;
			var ringGroupsToClean = new List<RingGroup>();
			foreach (RingGroup ringGroup in polyhedron.RingGroups)
			{
				var args = new object[] { ringGroup, tolerance, null };
				ringGroupsToClean.Add((RingGroup) removeNonCoplanar.Invoke(null, args));

				foreach (Linestring nonCoplanarRing in (List<Linestring>) args[2])
				{
					ringGroupsToClean.Add(new RingGroup(nonCoplanarRing));
				}
			}

			var result = new List<RingGroup>();
			foreach (RingGroup group in ringGroupsToClean)
			{
				var args = new object[] { group, tolerance, null };
				var cleaned = (IEnumerable<RingGroup>) simplifyRings.Invoke(null, args);
				verticalRingCount += ((List<Linestring>) args[2]).Count;

				foreach (RingGroup cleanedGroup in cleaned)
				{
					var oriented =
						(RingGroup) asOriented.Invoke(null, new object[] { cleanedGroup });

					if (oriented.IsEmpty)
					{
						verticalRingCount += cleanedGroup.GetLinestrings().Count();
						continue;
					}

					result.Add(oriented);
				}
			}

			return result;
		}

		[Test]
		public void CanGetFootprintForOid3925818()
		{
			// TLM_GEBAEUDE OID 3925818. Previously threw GeomException
			// ("Error calculating XY-union areas") at step 6 because
			// CalculateSourceBoundaryLoops created a BoundaryLoop with a
			// LinearIntersectionStart and a TouchingInPoint pinch at the same target
			// vertex, causing conflicting navigation in FollowSubcurvesTurningLeft.
			// AO reference area: 503.7757.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("oid_3925818.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.00625;

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, tolerance, out _);

			Assert.AreEqual(503.7757, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanGetFootprintAtFineToleranceWithParallelRun()
		{
			// TLM_GEBAEUDE OID 3939229. At a fine tolerance (0.0005, finer than the data
			// resolution 0.0125) a ~2.88 m shared wall between two building faces is classified
			// as two point-touches bounding a sub-resolution sliver (perpendicular offset
			// ~0.001 m, between the tolerance and the resolution) instead of one linear
			// intersection - the turning-left walk then cycled ("Intersections seen twice").
			// GetXYFootprint now passes the resolution as a merge tolerance so the union snaps
			// such near-coincident parallel edge runs into a clean linear intersection.
			// AO reference area: 334.49.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("fine_tolerance_parallel_run.wkb"), out _);

			MultiLinestring footprint = polyhedron.GetXYFootprint(0.0005, 0.0125, out _);

			Assert.AreEqual(334.49, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanGetFootprintAtFineToleranceWithDegenerateLinearRun()
		{
			// TLM_GEBAEUDE OID 3928757. At a fine tolerance a spurious sub-resolution
			// (~0.0003 m) linear intersection run appears next to a real one (a near-coincident
			// vertex artefact that survives the Sqrt(2)*tolerance point clustering). The union
			// now collapses such a degenerate micro-run to a single point. AO reference: 207.93.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("fine_tolerance_micro_linear.wkb"), out _);

			MultiLinestring footprint = polyhedron.GetXYFootprint(0.0005, 0.0125, out _);

			Assert.AreEqual(207.93, footprint.GetArea2D(), 0.05);
		}

		[Test]
		public void CanGetFootprintAtFineToleranceWithCoincidentCrossings()
		{
			// TLM_GEBAEUDE OID 3935928. At a fine tolerance a single crossing is duplicated into
			// two near-coincident crossings (~0.0009 m apart, just beyond the Sqrt(2)*tolerance
			// point-cluster distance), throwing a GeomException. The union now collapses such a
			// near-coincident crossing pair to a single point. AO reference: 180.83.
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("fine_tolerance_coincident_crossing.wkb"),
				out _);

			MultiLinestring footprint = polyhedron.GetXYFootprint(0.0005, 0.0125, out _);

			Assert.AreEqual(180.83, footprint.GetArea2D(), 0.05);
		}

		[Test]
		[Ignore("Diagnostic: per-step incremental union area / inflate-loss dump for " +
		        "thanhalten (TLM_GEBAEUDE {823CB5A8-2104-46C9-9851-3BBD1F88CA41}). " +
		        "Writes source/ring WKBs for loss/inflate steps to C:\\temp\\th_*.wkb.")]
		public void DumpThanhaltenUnionSteps()
		{
			Polyhedron polyhedron = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("thanhalten.wkb"), out _);

			double tolerance = 0.00625;
			var ci = CultureInfo.InvariantCulture;
			var groups = polyhedron.RingGroups.OrderByDescending(r => r.GetArea2D()).ToList();
			Console.WriteLine($"Total ring groups: {groups.Count}");

			MultiLinestring result = null;
			int count = 0;
			foreach (RingGroup rg in groups)
			{
				if (result == null)
				{
					result = rg.Clone();
					Console.WriteLine(string.Format(ci, "start area={0:F6} parts={1}",
					                                result.GetArea2D(), result.PartCount));
					continue;
				}

				count++;
				double before = result.GetArea2D();

				MultiLinestring disjointPart =
					GeomTopoOpUtils.GetDifferenceAreasXY(rg, result, tolerance);
				double disjoint = disjointPart.GetArea2D();

				MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(result, rg, tolerance);
				double after = union.GetArea2D();
				double grew = after - before;

				string flag = grew < disjoint - 1e-3 ? "  <<< LOSS"
				              : grew > disjoint + 1e-3 ? "  <<< INFLATE"
				              : "";

				Console.WriteLine(string.Format(ci,
				                                "step {0,3}: {1:F6} -> {2:F6} grew={3:F6} " +
				                                "ringArea={4:F6} disjoint={5:F6} parts {6}->{7}{8}",
				                                count, before, after, grew,
				                                rg.GetArea2D(), disjoint,
				                                result.PartCount, union.PartCount, flag));

				if (flag.Length > 0)
				{
					GeomUtils.ToWkbFile(result, $@"C:\temp\th_source_{count}.wkb");
					GeomUtils.ToWkbFile(rg, $@"C:\temp\th_ring_{count}.wkb");
					Console.WriteLine($"  -> wrote C:\\temp\\th_source/ring_{count}.wkb");

					var nav = new SubcurveNavigator(result, rg, tolerance);
					Console.WriteLine($"  HasBoundaryLoops={nav.HasBoundaryLoops()} " +
					                  $"IPs={nav.IntersectionPoints.Count}");
					foreach (IntersectionPoint3D ip in nav.IntersectionPoints)
					{
						Console.WriteLine(string.Format(ci,
						                                "    ip type={0} srcV={1:F4} tgtV={2:F4} " +
						                                "pt=({3:F4},{4:F4})",
						                                ip.Type, ip.VirtualSourceVertex,
						                                ip.VirtualTargetVertex,
						                                ip.Point.X, ip.Point.Y));
					}

					foreach (Linestring r in union.GetLinestrings())
					{
						Console.WriteLine(string.Format(ci,
						                                "  result ring area={0:F6} cw={1} " +
						                                "env=({2:F4},{3:F4})-({4:F4},{5:F4})",
						                                r.GetArea2D(), r.ClockwiseOriented,
						                                r.XMin, r.YMin, r.XMax, r.YMax));
					}
				}

				result = union;
			}

			Console.WriteLine(string.Format(ci, "FINAL area={0:F6} parts={1}",
			                                result.GetArea2D(), result.PartCount));
		}

		[Test]
		public void CanUnionThanhaltenAtStep6()
		{
			// TLM_GEBAEUDE {823CB5A8-2104-46C9-9851-3BBD1F88CA41} (Thanhalten), isolated
			// incremental-union step 6 (tolerance 0.00625). The accumulated footprint
			// (source) carries a sub-resolution needle (vertices 5,6,7 are an out-and-back
			// spike whose flanks are 0.0078 m apart - below the 0.0125 resolution but above
			// the tolerance). The target triangle (9.685) shares an edge with the source and
			// grazes the needle with its diagonal, so the navigator saw only spurious
			// sub-tolerance linear intersections (no clean crossing): the turning-left walk
			// produced no rings and the target's disjoint part (4.867) was dropped, leaving a
			// missing part. RingOperator now clusters the needle's flanks (cluster distance
			// Sqrt(2)*tolerance) and removes the resulting duplicate segment, so the union
			// adds the disjoint part correctly - directly on the raw source, no pre-cleaning.
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("thanhalten_step6_source.wkb"), out _);
			MultiLinestring ring = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("thanhalten_step6_ring.wkb"), out _);

			double tolerance = 0.00625;

			double sourceArea = source.GetArea2D();

			MultiLinestring disjoint =
				GeomTopoOpUtils.GetDifferenceAreasXY(ring, source, tolerance);

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, ring, tolerance);

			Assert.AreEqual(sourceArea + disjoint.GetArea2D(), result.GetArea2D(), 0.001,
			                "Union must add the disjoint part of the target ring.");
			Assert.AreEqual(1, result.PartCount);
		}

		[Test]
		public void CanUnionGrancyAtStep5()
		{
			// TLM_GEBAEUDE {71DAC71B-9971-4612-B40F-5AC8DB201DCB} (Grancy), isolated
			// incremental-union step 5 (tolerance 0.00625). The accumulated footprint
			// (source, 373.204) has a thin spike whose apex vertex lies 0.0092 m - just
			// ABOVE tolerance - from a vertex of the target ring (44.746). The source edge
			// leaving the apex is coincident with a target edge (a genuine linear
			// intersection), but the other spike segment only grazes the same target point
			// and is mis-classified as a Crossing at the very same XY. That contradictory
			// Crossing + LinearIntersectionStart derailed the turning-left walk into
			// emitting the whole target ring a second time, so the union came out 429.365
			// with two overlapping outer rings (correct 384.620 + a spurious 44.745
			// duplicate). The union must be a single ring of source + disjoint target.
			// Fixed at the root: RingOperator.ClusterGeometries clusters the near-coincident
			// vertices (within Sqrt(2)*tolerance) and CRACKS the spike's grazing segment,
			// inserting a vertex at the cluster point. The welded spike collapses to a
			// duplicate segment (removed by RemoveLinearSelfIntersections) and the
			// recalculated intersections no longer contain the spurious crossing - so this
			// no longer relies on a navigator special-case.
			MultiLinestring source = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("grancy_step5_source.wkb"), out _);
			MultiLinestring ring = (MultiLinestring) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("grancy_step5_ring.wkb"), out _);

			double tolerance = 0.00625;
			double sourceArea = source.GetArea2D();

			MultiLinestring disjoint =
				GeomTopoOpUtils.GetDifferenceAreasXY(ring, source, tolerance);

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(source, ring, tolerance);

			Assert.AreEqual(sourceArea + disjoint.GetArea2D(), result.GetArea2D(), 0.001,
			                "Union must merge into a single ring (no duplicated target).");
			Assert.AreEqual(1, result.PartCount);
		}

		[Test]
		public void CanGetFootprintExplodingSelfCrossingRing()
		{
			// A single figure-8 (bowtie) exterior ring: vertices ordered so that the edge
			// 1->2 crosses the edge 3->0, producing a 0-dimensional self-intersection at the
			// shared crossing point. The signed area of such a ring is the DIFFERENCE of the
			// two lobes (here ~0), but the footprint (the area covered when seen from above)
			// is the UNION of both lobes. The cleanup must explode the bowtie into two simple
			// rings so the footprint covers both lobes.
			//
			//   1-------------0
			//    \           /
			//     \         /          two 5x5 lobes meeting at the crossing point (5,5)
			//      \       /
			//       \     /
			//        \   /
			//         \ /
			//          X   (5,5)
			//         / \
			//        /   \
			//       2-----3
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(10, 10, 0), // 0
				           new Pnt3D(0, 10, 0), // 1
				           new Pnt3D(10, 0, 0), // 2  -> edge 1->2 crosses edge 3->0
				           new Pnt3D(0, 0, 0), // 3
			           };

			Polyhedron polyhedron = CreatePolyhedron(GeomTestUtils.CreateRing(ring));

			MultiLinestring footprint =
				polyhedron.GetXYFootprint(0.001, 0.001, out _);

			// Two triangular lobes of area 25 each (base 10, height 5) -> 50 covered.
			// (The two lobes touch only at the crossing point, so the union may keep them
			// as one or two rings; the invariant is the covered area and CW orientation.)
			Assert.AreEqual(50, footprint.GetArea2D(), 0.001);
			Assert.True(footprint.GetLinestrings().All(l => l.ClockwiseOriented == true));
		}

		private static void DumpNavigatorInfo(MultiLinestring source, MultiLinestring ring,
		                                      double tolerance, double sqrtTwiceTol,
		                                      CultureInfo ci)
		{
			SubcurveNavigator nav;
			try
			{
				nav = new SubcurveNavigator(source, ring, tolerance);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"    SubcurveNavigator THREW: {ex.Message}");
				return;
			}

			var ips = nav.IntersectionPoints;
			Console.WriteLine($"    HasBoundaryLoops={nav.HasBoundaryLoops()} IPs={ips.Count}");

			foreach (IntersectionPoint3D ip in ips)
				Console.WriteLine(string.Format(ci,
				                                "      ip type={0} srcV={1:F6} tgtV={2:F6} pt=({3:F6},{4:F6})",
				                                ip.Type, ip.VirtualSourceVertex,
				                                ip.VirtualTargetVertex,
				                                ip.Point.X, ip.Point.Y));

			// Report consecutive IP pairs within sqrt(2)*tolerance (clustering candidates)
			int closeCount = 0;
			for (int i = 0; i < ips.Count - 1; i++)
			{
				double dx = ips[i].Point.X - ips[i + 1].Point.X;
				double dy = ips[i].Point.Y - ips[i + 1].Point.Y;
				double dist = Math.Sqrt(dx * dx + dy * dy);
				if (dist < sqrtTwiceTol && dist > tolerance)
				{
					closeCount++;
					Console.WriteLine(string.Format(ci,
					                                "      near-cluster: ip[{0}]<->ip[{1}] dist={2:F6} (tol={3:F6} sqrt2tol={4:F6})",
					                                i, i + 1, dist, tolerance, sqrtTwiceTol));
				}
			}

			if (closeCount == 0)
				Console.WriteLine(
					$"      no near-cluster IP pairs in ({tolerance:F6},{sqrtTwiceTol:F6}]");
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

		private static void DumpSegments(MultiLinestring mls)
		{
			var ci = CultureInfo.InvariantCulture;
			Linestring ls = mls.GetLinestring(0);
			for (int s = 0; s < ls.SegmentCount; s++)
			{
				Line3D seg = ls.GetSegment(s);
				Console.WriteLine(string.Format(
					                  ci,
					                  "  seg[{0,2}] ({1:F4},{2:F4})->({3:F4},{4:F4}) len={5:F4}",
					                  s, seg.StartPoint.X, seg.StartPoint.Y, seg.EndPoint.X,
					                  seg.EndPoint.Y,
					                  seg.Length2D));
			}
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

		[Test]
		[Ignore("Diagnostic: sweeps each TLM_FootprintTest case across several tolerances " +
		        "and reports footprint area / part count / THROW, to verify the union is " +
		        "robust regardless of tolerance.")]
		public void DumpTLMFootprintToleranceSweep()
		{
			const string folder = @"C:\Temp\UnitTestData\TLM_FootprintTest";
			double[] tolerances = { 0.0005, 0.001, 0.00625, 0.0125 };
			const double verticalRingDetectionTolerance = 0.0125;
			var ci = CultureInfo.InvariantCulture;

			foreach (string subfolder in new[] { "Failing", "Incorrect" })
			{
				string dir = Path.Combine(folder, subfolder);
				string[] polyhedronFiles = Directory.GetFiles(dir, "*_Polyhedron.wkb")
				                                    .OrderBy(f => f).ToArray();

				Console.WriteLine($"\n===== {subfolder} ({polyhedronFiles.Length} cases) =====");
				Console.WriteLine(string.Format(ci, "{0,-16}{1,10}  {2}", "case", "AO",
				                                string.Join("  ",
				                                            tolerances.Select(t => $"tol={t}"))));

				foreach (string polyhedronFile in polyhedronFiles)
				{
					string stem = Path.GetFileNameWithoutExtension(polyhedronFile)
					                  .Replace("_Polyhedron", "");
					string aoFile = Path.Combine(dir, $"{stem}_AO.wkb");

					double aoArea = 0;
					try
					{
						var ao = GeomUtils.FromWkbFile(aoFile, out _) as MultiLinestring;
						aoArea = ao?.GetArea2D() ?? 0;
					}
					catch
					{
						// ignore
					}

					var cells = new List<string>();
					foreach (double tol in tolerances)
					{
						try
						{
							var poly = (Polyhedron) GeomUtils.FromWkbFile(polyhedronFile, out _);
							MultiLinestring fp = poly.GetXYFootprint(
								tol, verticalRingDetectionTolerance, out _);
							cells.Add(string.Format(ci, "{0:F2}({1}p)", fp.GetArea2D(),
							                        fp.PartCount));
						}
						catch (Exception ex)
						{
							string msg = (ex.InnerException ?? ex).Message;
							string tag = msg.Contains("seen twice") ? "THROW:seenTwice"
							             : msg.Contains("non-simple") ||
							               msg.Contains("non simple") ||
							               msg.Contains("simple") ? "THROW:nonSimple"
							             : msg.Contains("exterior ring") ? "THROW:nestedExt"
							             : "THROW:" + ex.GetType().Name;
							cells.Add(tag);
						}
					}

					Console.WriteLine(string.Format(ci, "{0,-16}{1,10:F2}  {2}", stem, aoArea,
					                                string.Join("  ", cells)));
				}
			}
		}

		[Test]
		[Ignore("Diagnostic: times GetXYFootprint on large fixtures to assess the per-step " +
		        "clustering performance.")]
		public void TimeFootprintPerformance()
		{
			var ci = CultureInfo.InvariantCulture;
			string[] fixtures =
			{
				"huge_lockergestein.wkb", "garden_center_giubiasco.wkb", "barrel_roof.wkb",
				"labyrinth_aventure.wkb", "vallee_de_la_jeunesse.wkb"
			};

			const double tolerance = 0.00625;
			const double verticalRingDetectionTolerance = 0.0125;

			foreach (string fixture in fixtures)
			{
				if (! (GeomUtils.FromWkbFile(
						       GeomTestUtils.GetGeometryTestDataPath(fixture), out _) is Polyhedron
					       poly))
				{
					Console.WriteLine($"{fixture,-32} (not a Polyhedron - skipped)");
					continue;
				}

				// Warm up (JIT + any caches).
				poly.GetXYFootprint(tolerance, verticalRingDetectionTolerance, out _);

				var sw = Stopwatch.StartNew();
				MultiLinestring fp = null;
				const int runs = 3;
				for (var i = 0; i < runs; i++)
				{
					fp = poly.GetXYFootprint(tolerance, verticalRingDetectionTolerance, out _);
				}

				sw.Stop();

				Console.WriteLine(string.Format(ci,
				                                "{0,-32} groups={1,4} area={2,12:F2} parts={3,3}  {4,8:F1} ms/run",
				                                fixture, poly.RingGroups.Count, fp.GetArea2D(),
				                                fp.PartCount,
				                                sw.Elapsed.TotalMilliseconds / runs));
			}
		}
	}
}

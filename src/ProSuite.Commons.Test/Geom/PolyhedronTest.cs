using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Geom;

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
				                 new Pnt3D(1, 1, 4),  // v1 – revisited at v4
				                 new Pnt3D(10, 5, 3),
				                 new Pnt3D(10, 4, 3),
				                 new Pnt3D(1, 1, 4),  // v4 = v1 (self-tangent)
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
				                 new Pnt3D(0.1850, 0.0738, 454.4425),  // revisits vertex 1
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

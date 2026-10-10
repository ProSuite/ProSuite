using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	/// <summary>
	/// Buildings from the July 2026 delivery (Kaspar) whose footprint differed from the AGD
	/// reference at every tolerance. <c>kaspar_july_&lt;case&gt;.wkb</c> is the multipatch
	/// of one such building, the expected areas are those of the AGD reference.
	/// </summary>
	[TestFixture]
	public class KasparJulyHardCaseGeomTest
	{
		private const double _tolerance = 0.003125;

		/// <summary>
		/// The courtyard of a warped roof face must survive: this face is 0.75 m off its own
		/// best-fit plane, so measuring its interior ring against that plane with an absolute
		/// tolerance declared the courtyard "not coplanar", promoted it to an exterior ring
		/// and filled it in (147.6 instead of 83.7 m2).
		/// </summary>
		[Test]
		public void CanKeepCourtyardOfNonPlanarRoofFace()
		{
			MultiLinestring footprint = GetFootprint("kaspar_july_hole_147");

			Assert.AreEqual(2, footprint.PartCount, "exterior ring plus the courtyard");
			Assert.AreEqual(1, CountInteriorRings(footprint), "the courtyard");
			Assert.AreEqual(83.746, footprint.GetArea2D(), 0.01);
		}

		/// <summary>
		/// The courtyard is stored with the same orientation as its exterior ring. Reversing
		/// the ring group as a whole kept the hole positive, so its area was added instead of
		/// subtracted (110.0 instead of 51.7 m2).
		/// </summary>
		[Test]
		public void CanSubtractCourtyardStoredWithExteriorOrientation()
		{
			MultiLinestring footprint = GetFootprint("kaspar_july_hole_81");

			Assert.AreEqual(1, CountInteriorRings(footprint), "the courtyard");
			Assert.AreEqual(51.695, footprint.GetArea2D(), 0.01);
		}

		/// <summary>
		/// Two building parts that only touch, one of them with an island whose first vertex
		/// lies on the shared wall. The containment test for the island used that vertex,
		/// got an undecidable answer and the union dropped the island - but only with the
		/// island's group as the target, so the result depended on the operand order.
		/// </summary>
		[Test]
		public void CanUnionRingGroupWithIslandInEitherOperandOrder()
		{
			List<RingGroup> groups = ReadPolyhedron("kaspar_july_hole_469")
			                         .RingGroups.Select(AsNestedGroup).ToList();

			RingGroup solid = groups.Single(g => g.InteriorRingCount == 0);
			RingGroup withIsland = groups.Single(g => g.InteriorRingCount == 1);

			const double expectedArea = 453.121; // the parts only touch: the sum

			Assert.AreEqual(expectedArea, solid.GetArea2D() + withIsland.GetArea2D(), 0.01);

			Assert.AreEqual(expectedArea,
			                GeomTopoOpUtils.GetUnionAreasXY(solid, withIsland, _tolerance,
			                                                _tolerance).GetArea2D(), 0.01,
			                "island group as target");

			Assert.AreEqual(expectedArea,
			                GeomTopoOpUtils.GetUnionAreasXY(withIsland, solid, _tolerance,
			                                                _tolerance).GetArea2D(), 0.01,
			                "island group as source");
		}

		/// <summary>
		/// The building is stored as ONE ring that touches itself, tracing an outline and two
		/// courtyards. Cracking the ring made every loop clockwise, so all three were taken as
		/// solid rings and the courtyards were filled in (1279.5 instead of 755.0 m2).
		/// </summary>
		[Test]
		public void CanReadCourtyardsOfSelfTouchingRingAsHoles()
		{
			MultiLinestring footprint = GetFootprint("kaspar_july_extra_1280");

			Assert.AreEqual(2, CountInteriorRings(footprint), "the two courtyards");
			Assert.AreEqual(754.983, footprint.GetArea2D(), 0.01);
		}

		/// <summary>
		/// One part of this building is a ring whose inner loop is wound the same way as its
		/// outer loop. It is nevertheless a hole (even-odd), as in ArcObjects: 453.1 m2, not
		/// 469.4 m2 with the loop taken as solid.
		/// </summary>
		[Test]
		public void CanReadSameWoundLoopOfSelfTouchingRingAsHole()
		{
			Assert.AreEqual(453.124, GetFootprint("kaspar_july_hole_469").GetArea2D(), 0.01);
		}

		/// <summary>
		/// Two roof faces of building 043349C2 (coordinates relative to 2671768.421 /
		/// 1247607.830). One edge of each ring runs along the same line in opposite
		/// directions; the two edges overlap for 0.23 m and diverge by 0.4 degrees. From a
		/// tolerance of 0.00125 the overlap was reported as two crossings instead of a linear
		/// intersection, and the union cut off a triangle of 2.68 m2 along a segment that is
		/// in neither ring (2044.59 instead of 2047.26 m2).
		/// </summary>
		[Test]
		public void CanUnionRingsWithShortOverlapOfNearlyParallelEdges()
		{
			var ring1 = new RingGroup(new Linestring(new List<Pnt3D>
			                                         {
				                                         new Pnt3D(0, 0, 0),
				                                         new Pnt3D(-20.6650, -25.9925, 0),
				                                         new Pnt3D(-37.1813, -12.8625, 0),
				                                         new Pnt3D(-34.7088, -9.9325, 0),
				                                         new Pnt3D(-47.2725, 0.1488, 0),
				                                         new Pnt3D(-30.6962, 20.9975, 0),
				                                         new Pnt3D(-9.7900, 4.3738, 0),
				                                         new Pnt3D(-8.1288, 6.4638, 0),
				                                         new Pnt3D(0, 0, 0)
			                                         }));

			var ring2 = new RingGroup(new Linestring(new List<Pnt3D>
			                                         {
				                                         new Pnt3D(-34.8900, -9.7887, 0),
				                                         new Pnt3D(-18.2812, -22.9937, 0),
				                                         new Pnt3D(-42.7850, -53.8150, 0),
				                                         new Pnt3D(-69.5738, -32.5162, 0),
				                                         new Pnt3D(-60.2913, -20.8425, 0),
				                                         new Pnt3D(-50.1125, -28.9362, 0),
				                                         new Pnt3D(-34.8900, -9.7887, 0)
			                                         }));

			const double expectedArea = 2047.26;

			foreach (double tolerance in new[] { 0.0005, 0.00125, 0.0025, 0.005, 0.01 })
			{
				Assert.AreEqual(expectedArea,
				                GeomTopoOpUtils.GetUnionAreasXY(ring1, ring2, tolerance,
				                                                tolerance).GetArea2D(), 0.05,
				                $"tolerance {tolerance}");

				Assert.AreEqual(expectedArea,
				                GeomTopoOpUtils.GetUnionAreasXY(ring2, ring1, tolerance,
				                                                tolerance).GetArea2D(), 0.05,
				                $"tolerance {tolerance}, operands swapped");
			}
		}

		/// <summary>
		/// The group's exterior ring split at its self-touching point, with the smaller loops
		/// as interior rings of the largest one.
		/// </summary>
		private static RingGroup AsNestedGroup(RingGroup group)
		{
			var fragments = new List<Linestring>();

			if (! RingSimplifier.TrySimplifyRingXY((Linestring) group.ExteriorRing.Clone(),
			                                       _tolerance, RingSimplifyFlags.Input,
			                                       fragments))
			{
				fragments.Add((Linestring) group.ExteriorRing.Clone());
			}

			fragments = fragments.OrderByDescending(f => Math.Abs(f.GetArea2D())).ToList();

			return RingGroup.CreateProperlyOriented(fragments[0], fragments.Skip(1).ToList());
		}

		private static MultiLinestring GetFootprint(string stem)
		{
			return ReadPolyhedron(stem).GetXYFootprint(_tolerance, _tolerance, out _);
		}

		/// <summary>
		/// A multipatch .wkb comes back as a MultiPolyhedron even when it holds a single
		/// polyhedron; merge all its ring groups into one.
		/// </summary>
		private static Polyhedron ReadPolyhedron(string stem)
		{
			object geometry = GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath($"{stem}.wkb"), out _);

			return geometry is Polyhedron single
				       ? single
				       : new Polyhedron(((MultiPolyhedron) geometry)
				                        .Polyhedra.SelectMany(p => p.RingGroups).ToList());
		}

		private static int CountInteriorRings(MultiLinestring rings)
		{
			return rings.GetLinestrings().Count(r => r.GetArea2D() < 0);
		}
	}
}

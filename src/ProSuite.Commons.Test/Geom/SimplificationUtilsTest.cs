using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class SimplificationUtilsTest
	{
		private const double _tolerance = 0.01;

		[Test]
		public void CanCrackTJunction()
		{
			// The lower ring has a vertex in the middle of the upper ring's bottom edge,
			// but the upper ring has no corresponding vertex there.
			RingGroup lower = GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(10, 0, 0),
					new Pnt3D(10, 5, 0),
					new Pnt3D(5, 5, 0),
					new Pnt3D(0, 5, 0)
				});

			RingGroup upper = GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(0, 5, 0),
					new Pnt3D(10, 5, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(0, 10, 0)
				});

			IList<RingGroup> result = SimplificationUtils.CrackAndCluster(
				new List<RingGroup> { lower, upper }, _tolerance, null, out int iterations);

			Assert.AreEqual(1, iterations);

			// The upper ring received the missing vertex at (5, 5).
			Linestring crackedUpper = result[1].ExteriorRing;

			Assert.AreEqual(6, crackedUpper.PointCount);
			Assert.IsTrue(crackedUpper.GetPoints()
			                          .Any(p => p.X == 5 && p.Y == 5));

			// The lower ring was already fully cracked.
			Assert.AreEqual(6, result[0].ExteriorRing.PointCount);
		}

		[Test]
		public void CanRemoveRingThinnerThanTolerance()
		{
			// A sliver 0.004 wide at a tolerance of 0.01: below the tolerance in every
			// direction across its width, so it carries no area the tolerance can resolve.
			RingGroup sliver = GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(5, 0, 0),
					new Pnt3D(5, 0.004, 0),
					new Pnt3D(0, 0.004, 0)
				});

			RingGroup solid = GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(0, 1, 0),
					new Pnt3D(5, 1, 0),
					new Pnt3D(5, 6, 0),
					new Pnt3D(0, 6, 0)
				});

			IList<RingGroup> result = SimplificationUtils.CrackAndCluster(
				new List<RingGroup> { sliver, solid }, _tolerance, null, out int _);

			// The sliver collapsed onto a line and was dropped, the solid ring survived.
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(25, System.Math.Abs(result[0].GetArea2D()), 0.001);
		}

		/// <summary>
		/// The point of clustering: "is within the tolerance of" is NOT transitive, so it
		/// does not by itself define which vertices are the same point. The sweep in
		/// GeomTopoOpUtils.Group chains such vertices into one group, so the chain ends up
		/// on a single node instead of on two nodes that depend on the visiting order.
		/// </summary>
		[Test]
		public void ChainedSubToleranceVerticesAllCollapseOntoOneNode()
		{
			// A - B - C, each 0.6 * tolerance apart, so A and C are 1.2 * tolerance apart
			// and do NOT pass a pairwise tolerance test against each other.
			const double step = 0.6 * _tolerance;

			RingGroup ring = GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(10, 0, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(5, 10, 0),
					new Pnt3D(5 - step, 10 + step, 0),
					new Pnt3D(5 - 2 * step, 10 + 2 * step, 0),
					new Pnt3D(0, 10, 0)
				});

			IList<RingGroup> result = SimplificationUtils.CrackAndCluster(
				new List<RingGroup> { ring }, _tolerance, null, out int _);

			// The three chained vertices became a single one, even though the outer two
			// are further apart than the tolerance.
			Assert.AreEqual(5, result[0].ExteriorRing.SegmentCount);
		}

		[Test]
		public void ResultIsIdempotent()
		{
			Polyhedron polyhedron = ReadPolyhedron("friedhofsmauer_roggwil.wkb");

			Polyhedron once = SimplificationUtils.CrackAndCluster(
				polyhedron, _tolerance, null, out int _);

			Polyhedron twice = SimplificationUtils.CrackAndCluster(
				once, _tolerance, null, out int secondIterations);

			// Nothing left to crack the second time round.
			Assert.AreEqual(0, secondIterations);

			Assert.AreEqual(once.RingGroups.Count, twice.RingGroups.Count);

			for (var i = 0; i < once.RingGroups.Count; i++)
			{
				Assert.AreEqual(once.RingGroups[i].GetArea2D(),
				                twice.RingGroups[i].GetArea2D(), 1e-9);
			}
		}

		[Test]
		public void DisabledOptionsLeaveTheInputUntouched()
		{
			Polyhedron polyhedron = ReadPolyhedron("friedhofsmauer_roggwil.wkb");

			Polyhedron result = SimplificationUtils.CrackAndCluster(
				polyhedron, _tolerance, CrackAndClusterOptions.Disabled, out int iterations);

			Assert.AreSame(polyhedron, result);
			Assert.AreEqual(0, iterations);
		}

		[Test]
		public void BothToleranceStrategiesFixFriedhofsmauer()
		{
			// The tolerance used by PolyhedronTest.CanGetFootprintForFriedhofsmauerRoggwil.
			const double tolerance = 0.00625;

			Polyhedron polyhedron = ReadPolyhedron("friedhofsmauer_roggwil.wkb");

			foreach (CrackAndClusterToleranceStrategy strategy in new[]
			         {
				         CrackAndClusterToleranceStrategy.Uniform,
				         CrackAndClusterToleranceStrategy.Aggressive
			         })
			{
				var options = new CrackAndClusterOptions { ToleranceStrategy = strategy };

				MultiLinestring footprint =
					polyhedron.GetXYFootprint(tolerance, tolerance, out _, options);

				// AO reference: 58.8638 in one part.
				Assert.AreEqual(58.8638, footprint.GetArea2D(), 0.05, strategy.ToString());
				Assert.AreEqual(1, footprint.PartCount, strategy.ToString());
			}
		}

		[Test]
		public void ArcObjectsStyleClustersMoreAggressivelyThanUniform()
		{
			// 1.5 * tolerance apart: beyond the uniform cluster radius, inside the
			// ArcObjects one (2 * sqrt(2) * tolerance = 2.83 * tolerance).
			const double gap = 1.5 * _tolerance;

			RingGroup ring = GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(10, 0, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(5, 10, 0),
					new Pnt3D(5 - gap, 10, 0),
					new Pnt3D(0, 10, 0)
				});

			IList<RingGroup> uniform = SimplificationUtils.CrackAndCluster(
				new List<RingGroup> { ring }, _tolerance,
				new CrackAndClusterOptions
				{
					ToleranceStrategy = CrackAndClusterToleranceStrategy.Uniform
				}, out int _);

			IList<RingGroup> arcObjects = SimplificationUtils.CrackAndCluster(
				new List<RingGroup> { ring }, _tolerance,
				new CrackAndClusterOptions
				{
					ToleranceStrategy = CrackAndClusterToleranceStrategy.Aggressive
				}, out int _);

			Assert.AreEqual(6, uniform[0].ExteriorRing.SegmentCount);
			Assert.AreEqual(5, arcObjects[0].ExteriorRing.SegmentCount);
		}

		private static Polyhedron ReadPolyhedron(string fileName)
		{
			object geometry = GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(fileName), out _);

			if (geometry is Polyhedron polyhedron)
			{
				return polyhedron;
			}

			var multiPolyhedron = (MultiPolyhedron) geometry;

			return new Polyhedron(
				multiPolyhedron.Polyhedra.SelectMany(ph => ph.RingGroups).ToList());
		}
	}
}

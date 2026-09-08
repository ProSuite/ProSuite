using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class RingSimplifierTest
	{
		private const double _tolerance = 0.01;

		[Test]
		public void SimpleRingIsLeftAlone()
		{
			MultiLinestring rings = CreateRings(Square(0, 0, 10));

			Linestring original = rings.GetPart(0);

			Assert.IsFalse(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(1, rings.PartCount);
			Assert.AreSame(original, rings.GetPart(0));
		}

		[Test]
		public void CanRemoveNeedle()
		{
			// A square with a zero-width spike sticking out of its top edge: the ring
			// leaves (5, 10) towards (5, 12) and returns along the very same segment.
			MultiLinestring rings = CreateRings(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(0, 10, 0),
					new Pnt3D(5, 10, 0),
					new Pnt3D(5, 12, 0),
					new Pnt3D(5, 10, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(10, 0, 0)
				});

			double areaBefore = rings.GetArea2D();

			Assert.IsTrue(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(1, rings.PartCount);
			Assert.AreEqual(areaBefore, rings.GetArea2D(), 0.0001);

			Assert.IsFalse(rings.GetPart(0).GetPoints().Any(p => p.Y > 10.0001));
		}

		[Test]
		public void CanRemoveLongNeedle()
		{
			// The spike has several vertices along it, so it must be peeled off pair by pair.
			MultiLinestring rings = CreateRings(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(0, 10, 0),
					new Pnt3D(5, 10, 0),
					new Pnt3D(5, 11, 0),
					new Pnt3D(5, 12, 0),
					new Pnt3D(5, 11, 0),
					new Pnt3D(5, 10, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(10, 0, 0)
				});

			Assert.IsTrue(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(1, rings.PartCount);
			Assert.AreEqual(100, rings.GetArea2D(), 0.0001);
			// (0,0) (0,10) (5,10) (10,10) (10,0) and the closing point: the vertex the
			// spike started from stays behind as a collinear vertex on the top edge.
			Assert.AreEqual(6, rings.GetPart(0).PointCount);
		}

		[Test]
		public void RingConsistingOfNeedlesOnlyDisappears()
		{
			MultiLinestring rings = CreateRings(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(10, 0, 0),
					new Pnt3D(0, 0, 0),
					new Pnt3D(0, 5, 0)
				});

			Assert.IsTrue(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(0, rings.PartCount);
		}

		[Test]
		public void CanExplodeExteriorBoundaryLoop()
		{
			// Two squares that touch in the single point (10, 10), modelled as one ring.
			MultiLinestring rings = CreateRings(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(0, 10, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(10, 20, 0),
					new Pnt3D(20, 20, 0),
					new Pnt3D(20, 10, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(10, 0, 0)
				});

			Assert.IsTrue(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(2, rings.PartCount);
			Assert.AreEqual(200, rings.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanRemoveSubToleranceBoundaryLoop()
		{
			// A square with a sliver spike whose two flanks are 0.002 apart, i.e. much
			// closer to each other than the tolerance of 0.01.
			MultiLinestring rings = CreateRings(
				new List<Pnt3D>
				{
					new Pnt3D(0, 0, 0),
					new Pnt3D(0, 10, 0),
					new Pnt3D(4.999, 10, 0),
					new Pnt3D(5, 15, 0),
					new Pnt3D(5.002, 15, 0),
					new Pnt3D(5.001, 10, 0),
					new Pnt3D(10, 10, 0),
					new Pnt3D(10, 0, 0)
				});

			Assert.IsTrue(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(1, rings.PartCount);
			Assert.AreEqual(100, rings.GetArea2D(), 0.05);
		}

		[Test]
		public void CanCrackSelfCrossingRingWithInputFlags()
		{
			// A figure-8: the boundary crosses itself in (5, 5).
			var ring = new Linestring(Close(new List<Pnt3D>
			                                {
				                                new Pnt3D(0, 0, 0),
				                                new Pnt3D(10, 10, 0),
				                                new Pnt3D(10, 0, 0),
				                                new Pnt3D(0, 10, 0)
			                                }));

			var results = new List<Linestring>();

			Assert.IsTrue(RingSimplifier.TrySimplifyRingXY(
				              ring, _tolerance, RingSimplifyFlags.Input, results));

			Assert.AreEqual(2, results.Count);
			Assert.IsTrue(results.All(r => r.IsClosed));
		}

		[Test]
		public void SelfCrossingRingIsNotCrackedByTheStepResultFlags()
		{
			// The incremental union must not re-polygonize its own result: only the
			// repairs of RingSimplifyFlags.StepResult apply.
			var ring = new Linestring(Close(new List<Pnt3D>
			                                {
				                                new Pnt3D(0, 0, 0),
				                                new Pnt3D(10, 10, 0),
				                                new Pnt3D(10, 0, 0),
				                                new Pnt3D(0, 10, 0)
			                                }));

			var results = new List<Linestring>();

			Assert.IsFalse(RingSimplifier.TrySimplifyRingXY(
				               ring, _tolerance, RingSimplifyFlags.StepResult, results));

			Assert.AreEqual(0, results.Count);
		}

		[Test]
		public void InteriorRingsAreSimplifiedToo()
		{
			var outerRing = new Linestring(Close(Square(0, 0, 20)));

			// An island with a needle.
			var island = new Linestring(Close(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(5, 5, 0),
				                                  new Pnt3D(5, 15, 0),
				                                  new Pnt3D(10, 15, 0),
				                                  new Pnt3D(10, 18, 0),
				                                  new Pnt3D(10, 15, 0),
				                                  new Pnt3D(15, 15, 0),
				                                  new Pnt3D(15, 5, 0)
			                                  }));

			var rings = new MultiPolycurve(new List<Linestring> { outerRing, island });

			Assert.IsTrue(RingSimplifier.SimplifyRingsXY(rings, _tolerance));

			Assert.AreEqual(2, rings.PartCount);
			Assert.IsFalse(rings.GetPart(1).GetPoints().Any(p => p.Y > 15.0001));
		}

		private static MultiLinestring CreateRings(List<Pnt3D> points)
		{
			return new MultiPolycurve(
				new List<Linestring> { new Linestring(Close(points)) });
		}

		private static List<Pnt3D> Square(double x, double y, double size)
		{
			// Clockwise, i.e. a proper exterior ring.
			return new List<Pnt3D>
			       {
				       new Pnt3D(x, y, 0),
				       new Pnt3D(x, y + size, 0),
				       new Pnt3D(x + size, y + size, 0),
				       new Pnt3D(x + size, y, 0)
			       };
		}

		private static List<Pnt3D> Close(List<Pnt3D> points)
		{
			var result = new List<Pnt3D>(points) { points[0].ClonePnt3D() };

			return result;
		}
	}
}

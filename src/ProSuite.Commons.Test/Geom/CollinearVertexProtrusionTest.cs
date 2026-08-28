using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	/// <summary>
	/// Minimal, round-number analogue of
	/// <see cref="GeomRelationUtilsTest.CanDetermineContainmentOfVicinoCimiteroSavosaStep5Ring"/>:
	/// a target ring whose apex pokes a hair through a straight source edge that carries
	/// an extra, collinear ("fake") vertex.
	///
	///           5/10.02  <- target apex, 0.02 ABOVE the top edge (tolerance 0.01)
	///              /\
	///   0/10 _____/__\_______ 10/10        source: the 10x10 square, but its top edge
	///       |    /    \      |             is split into two segments by the collinear
	///       |   /      \     |             vertex 5/10 - exactly like the source of the
	///       |  /        \    |             Savosa case, whose straight NW edge carries
	///       | 2/2 ------ 8/2 |             two extra collinear vertices.
	///       |________________|
	///   0/0                   10/0
	///
	/// In the real case the numbers are: target apex 0.016358 outside the straight source
	/// edge (tolerance 0.01), and the fake vertex 0.0087 / 0.0000004 away from the two
	/// target segments - i.e. within the tolerance of both, which is why both crossings
	/// are reported at that one vertex.
	/// </summary>
	[TestFixture]
	public class CollinearVertexProtrusionTest
	{
		private const double _tolerance = 0.01;

		private static RingGroup CreateSourceSquare(bool withCollinearVertex)
		{
			var points = new List<Pnt3D> { new Pnt3D(0, 0, 0), new Pnt3D(0, 10, 0) };

			if (withCollinearVertex)
			{
				points.Add(new Pnt3D(5, 10, 0)); // the extra, collinear vertex
			}

			points.Add(new Pnt3D(10, 10, 0));
			points.Add(new Pnt3D(10, 0, 0));
			points.Add(new Pnt3D(0, 0, 0));

			var ring = new Linestring(points);
			Assert.IsTrue(ring.ClockwiseOriented);

			return new RingGroup(ring);
		}

		private static RingGroup CreateTargetTriangle(double apexY)
		{
			var ring = new Linestring(
				new List<Pnt3D>
				{
					new Pnt3D(2, 2, 0),
					new Pnt3D(5, apexY, 0),
					new Pnt3D(8, 2, 0),
					new Pnt3D(2, 2, 0)
				});

			Assert.IsTrue(ring.ClockwiseOriented);

			return new RingGroup(ring);
		}

		private static List<IntersectionPoint3D> GetCrossings(ISegmentList source,
		                                                      ISegmentList target)
		{
			return GeomTopoOpUtils
			       .GetIntersectionPoints(source, target, _tolerance)
			       .Where(ip => ip.Type == IntersectionPointType.Crossing).ToList();
		}

		[Test]
		public void CanDetermineNonContainmentOfRingProtrudingAtCollinearSourceVertex()
		{
			// This is the analogue of the Savosa step-5 pair: two Crossing intersections
			// at one and the same XY - the collinear source vertex - and a False
			// containment verdict.
			RingGroup source = CreateSourceSquare(true);
			RingGroup target = CreateTargetTriangle(10.02);

			List<IntersectionPoint3D> crossings = GetCrossings(source, target);

			// Both crossings collapse onto the collinear vertex 5/10: it lies 0.007 from
			// each of the two target segments, i.e. within the tolerance of both. The
			// entry crossing (target segment 0) and the exit crossing (target segment 1)
			// are therefore reported at the identical location.
			Assert.AreEqual(2, crossings.Count);
			Assert.IsTrue(crossings[0].Point.EqualsXY(crossings[1].Point, _tolerance));
			Assert.IsTrue(crossings[0].Point.EqualsXY(new Pnt3D(5, 10, 0), 1e-9));
			Assert.AreEqual(0, crossings[0].SegmentIntersection.TargetIndex);
			Assert.AreEqual(1, crossings[1].SegmentIntersection.TargetIndex);

			// The crossings are real: the apex is 0.02 (twice the tolerance) outside.
			Assert.IsFalse(GeomRelationUtils.AreaContainsXY(source, target, _tolerance));
			Assert.IsFalse(GeomRelationUtils.IsContainedXY(target, source, _tolerance));

			// ...but the difference reports NOTHING outside - the 0.00015 sq m sliver
			// between the two coincident crossings degenerates and is dropped. This is
			// what makes the ring look "provably contained" in the Savosa test, although
			// it is not.
			MultiLinestring outside =
				GeomTopoOpUtils.GetDifferenceAreasXY(target, source, _tolerance);

			Assert.AreEqual(0, outside.PartCount);

			// The union nevertheless absorbs the target here (the Savosa case loses it
			// only after the Balanced crack-and-cluster pass).
			MultiLinestring union =
				GeomTopoOpUtils.GetUnionAreasXY(source, target, _tolerance);

			Assert.AreEqual(1, union.PartCount);
			Assert.AreEqual(100, union.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanDetermineContainmentOfRingTouchingCollinearSourceVertexFromInside()
		{
			// Same configuration, but the apex stays 0.005 INSIDE, i.e. within the
			// tolerance of the top edge: no crossing, a single touch point, contained.
			RingGroup source = CreateSourceSquare(true);
			RingGroup target = CreateTargetTriangle(9.995);

			Assert.AreEqual(0, GetCrossings(source, target).Count);

			List<IntersectionPoint3D> intersections =
				GeomTopoOpUtils.GetIntersectionPoints(source, target, _tolerance).ToList();

			Assert.AreEqual(1, intersections.Count);
			Assert.AreEqual(IntersectionPointType.TouchingInPoint, intersections[0].Type);

			Assert.IsTrue(GeomRelationUtils.AreaContainsXY(source, target, _tolerance));
			Assert.IsTrue(GeomRelationUtils.IsContainedXY(target, source, _tolerance));
		}

		[Test]
		public void CanDetermineNonContainmentOfRingProtrudingAtStraightSourceEdge()
		{
			// Control: the very same target against the very same square WITHOUT the
			// collinear vertex. The two crossings are now reported at their true, distinct
			// locations and the protruding sliver survives the difference. The containment
			// verdict is unchanged - so the collinear vertex is what hides the sliver, not
			// what causes the False verdict.
			RingGroup source = CreateSourceSquare(false);
			RingGroup target = CreateTargetTriangle(10.02);

			List<IntersectionPoint3D> crossings = GetCrossings(source, target);

			Assert.AreEqual(2, crossings.Count);
			Assert.IsFalse(crossings[0].Point.EqualsXY(crossings[1].Point, _tolerance));
			Assert.AreEqual(4.992519, crossings[0].Point.X, 0.000001);
			Assert.AreEqual(5.007481, crossings[1].Point.X, 0.000001);

			Assert.IsFalse(GeomRelationUtils.AreaContainsXY(source, target, _tolerance));
			Assert.IsFalse(GeomRelationUtils.IsContainedXY(target, source, _tolerance));

			MultiLinestring outside =
				GeomTopoOpUtils.GetDifferenceAreasXY(target, source, _tolerance);

			Assert.AreEqual(1, outside.PartCount);
			Assert.AreEqual(0.00014963, outside.GetArea2D(), 0.00000001);
		}
	}
}

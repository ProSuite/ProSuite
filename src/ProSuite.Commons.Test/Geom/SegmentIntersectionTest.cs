using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class SegmentIntersectionTest
	{
		[Test]
		public void CanDetermineOppositeDirection()
		{
			Line3D line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0));
			Line3D line2 = new Line3D(new Pnt3D(2, 0, 0), new Pnt3D(0, 0, 0));

			const double tolerance = 0.01;

			var intersection =
				SegmentIntersection.CalculateIntersectionXY(1, 2, line1, line2, tolerance);
			Assert.True(intersection.HasLinearIntersection);
			Assert.True(intersection.LinearIntersectionInOppositeDirection);

			intersection =
				SegmentIntersection.CalculateIntersectionXY(2, 1, line2, line1, tolerance);
			Assert.True(intersection.HasLinearIntersection);
			Assert.True(intersection.LinearIntersectionInOppositeDirection);

			// Special case: The source end is very far away and deviates more than the tolerance from the target straight:
			line1.SetEndPoint(new Pnt3D(100, 0.02, 0));

			intersection =
				SegmentIntersection.CalculateIntersectionXY(1, 2, line1, line2, tolerance);
			Assert.True(intersection.HasLinearIntersection);
			Assert.True(intersection.LinearIntersectionInOppositeDirection);
		}

		[Test]
		public void CanDetermineCorrectOrientationForPseudoLinearIntersecion()
		{
			// Acute, near-coincident corners can superficially look like a tiny linear
			// overlap: each segment's relevant end point lies within the tolerance of the
			// OTHER segment's interior, but the corner gap between the two end points is
			// just above the vertex-snap tolerance (so neither factor snaps to a vertex).
			// Because the two segments are NOT actually collinear, such a "pseudo-linear"
			// intersection must be classified as a point / corner touch
			// (HasLinearIntersection == false), not as a linear overlap - a spurious linear
			// stretch here mis-drives the area union walk (TOP: sebastianskapelle, the
			// 10-triangle apex pinch at tolerance = resolution/2 = 0.00625). The geometry is
			// still recognised by IsPotentialPseudoLinearIntersection.

			Line3D line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0));
			Line3D line2 = new Line3D(new Pnt3D(0.011, 0, 0), new Pnt3D(-10, 10, 0));

			const double tolerance = 0.01;

			AssertPseudoLinearCornerTouch(line1, line2, tolerance);

			line2.ReverseOrientation();
			AssertPseudoLinearCornerTouch(line1, line2, tolerance);

			line2 = new Line3D(new Pnt3D(0.009, -0.008, 0), new Pnt3D(-10, 10, 0));
			AssertPseudoLinearCornerTouch(line1, line2, tolerance);

			line2.ReverseOrientation();
			AssertPseudoLinearCornerTouch(line1, line2, tolerance);

			// Really acute:
			line2 = new Line3D(new Pnt3D(0.03, 0.0, 0), new Pnt3D(-10, 1, 0));
			AssertPseudoLinearCornerTouch(line1, line2, tolerance);

			line2.ReverseOrientation();
			AssertPseudoLinearCornerTouch(line1, line2, tolerance);
		}

		[Test]
		public void CanClassifyCollinearOverlapContinuingFurther()
		{
			// The collinearity guard in HasLinearIntersection must NOT mistake a real linear
			// overlap for a pseudo-linear corner touch. The decisive sub-case (raised in
			// review) is a collinear segment that overlaps part of the other and then runs
			// off its end: the far end point projects OUTSIDE [0,1] but is still perpendicular
			// -near the other's infinite line, so its factor is non-NaN. The guard keys on
			// perpendicular distance (collinearity), not on the [0,1] projection range, so the
			// overlap is correctly classified as linear.

			// source: [0,5], target: [2,10] - target overlaps the source's second half and
			// continues past the source end. Overlap is [2,5].
			var source = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0));
			var target = new Line3D(new Pnt3D(2, 0, 0), new Pnt3D(10, 0, 0));

			AssertLinearOverlap(source, target);

			// Same, with the target reversed ([10,2]): opposite direction, still a linear overlap.
			target.ReverseOrientation();
			AssertLinearOverlap(source, target);

			// Roles swapped (the long segment as source): still linear.
			AssertLinearOverlap(new Line3D(new Pnt3D(2, 0, 0), new Pnt3D(10, 0, 0)),
			                    new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0)));

			// "Staircase" overlap: one source end point lies on the target and one target end
			// point lies on the source ([0,5] vs [3,8], overlap [3,5]).
			AssertLinearOverlap(new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0)),
			                    new Line3D(new Pnt3D(3, 0, 0), new Pnt3D(8, 0, 0)));
		}

		[Test]
		public void CanClassifyCollinearButDisjointSegmentsAsNoIntersection()
		{
			// Collinear segments can have NO linear intersection at all when they do not
			// overlap. This is the configuration the review flagged: "no linear intersection
			// but the segments could still be collinear". The gap along the common line is
			// larger than the tolerance, so there is simply no intersection - and the
			// collinearity guard is never even consulted.
			var source = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0));
			var target = new Line3D(new Pnt3D(6, 0, 0), new Pnt3D(10, 0, 0));

			const double tolerance = 0.01;

			var intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, source, target, tolerance);

			Assert.False(intersection.HasIntersection);
			Assert.False(intersection.HasLinearIntersection);
		}

		[Test]
		public void CanClassifyCollinearEndToEndTouchAsPoint()
		{
			// Collinear segments that meet end-to-end share a single point, NOT a linear
			// overlap. (This holds independently of the collinearity guard - the shared point
			// sits on a source vertex - but it is asserted here to pin down the boundary
			// between a point touch and a linear overlap for collinear inputs.)
			var source = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0));
			var target = new Line3D(new Pnt3D(5, 0, 0), new Pnt3D(10, 0, 0));

			const double tolerance = 0.01;

			var intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, source, target, tolerance);

			Assert.True(intersection.HasIntersection);
			Assert.False(intersection.HasLinearIntersection);
		}

		private static void AssertLinearOverlap(Line3D source, Line3D target)
		{
			const double tolerance = 0.01;

			SegmentIntersection intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, source, target, tolerance);

			Assert.True(intersection.HasIntersection);
			Assert.True(intersection.HasLinearIntersection,
			            "Expected a linear overlap for collinear segments.");

			// The classification must be symmetric in which segment is the source.
			var reversed =
				SegmentIntersection.CalculateIntersectionXY(0, 0, target, source, tolerance);

			Assert.True(reversed.HasIntersection);
			Assert.True(reversed.HasLinearIntersection,
			            "Linear overlap classification must not depend on source/target order.");
		}

		[Test]
		public void CanGetSegmentIntersectionsXY()
		{
			var line1 = new Line3D(new Pnt3D(0, 5, 0), new Pnt3D(10, 5, 0));
			var line2 = new Line3D(new Pnt3D(6, 0, 0), new Pnt3D(6, 10, 0));

			SegmentIntersection intersection =
				SegmentIntersection.CalculateIntersectionXY(
					0, 0, line1, line2, 0.01);

			Assert.IsTrue(intersection.HasIntersection);
			Assert.AreEqual(0.6, intersection.SingleInteriorIntersectionFactor);

			line2 = new Line3D(new Pnt3D(10, 0, 0), new Pnt3D(10, 10, 0));

			intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, line1, line2, 0.01);

			Assert.IsTrue(intersection.HasIntersection);
			Assert.IsNull(intersection.SingleInteriorIntersectionFactor);
			Assert.IsTrue(intersection.SourceEndIntersects);

			line2 = new Line3D(new Pnt3D(10, 0, 0), new Pnt3D(10, 5, 0));

			intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, line1, line2, 0.01);
			Assert.IsTrue(intersection.HasIntersection);
			Assert.IsNull(intersection.SingleInteriorIntersectionFactor);
			Assert.IsTrue(intersection.SourceEndIntersects);
		}

		[Test]
		public void CanGetLinearIntersectionInteriorXY()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(10, 10, 0));
			var line2 = new Line3D(new Pnt3D(6, 6, 3), new Pnt3D(8, 8, 3));

			SegmentIntersection intersection =
				SegmentIntersection.CalculateIntersectionXY(
					0, 7, line1, line2, 0.01);

			Assert.IsTrue(intersection.HasIntersection);
			Assert.IsNull(intersection.SingleInteriorIntersectionFactor);

			Assert.IsTrue(intersection.HasLinearIntersection);
			Assert.IsFalse(intersection.SegmentsAreEqualInXy);
			Assert.IsFalse(intersection.SourceStartIntersects);
			Assert.IsFalse(intersection.SourceEndIntersects);

			Assert.IsTrue(intersection.TargetStartIntersects);
			Assert.IsTrue(intersection.TargetEndIntersects);

			Assert.IsFalse(intersection.LinearIntersectionInOppositeDirection);
			Assert.IsTrue(intersection.HasSourceInteriorIntersection);

			Assert.AreEqual(0.6, intersection.GetFirstIntersectionAlongSource());

			double startFactorAlongSource;
			Assert.IsTrue(new Pnt3D(6, 6, 0).Equals(
				              intersection.GetLinearIntersectionStart(
					              line1, out startFactorAlongSource)));
			Assert.AreEqual(0.6, startFactorAlongSource);

			double endFactorAlongSource;
			Assert.IsTrue(new Pnt3D(8, 8, 0).Equals(
				              intersection.GetLinearIntersectionEnd(
					              line1, out endFactorAlongSource)));
			Assert.AreEqual(0.8, endFactorAlongSource);

			Assert.AreEqual(0.6, intersection.GetLinearIntersectionStartFactor(true));
			Assert.AreEqual(0.8, intersection.GetLinearIntersectionEndFactor(true));

			Assert.IsTrue(new Pnt3D(6, 6, 3).Equals(
				              intersection.GetLinearIntersectionStartOnTarget(line2)));
			Assert.IsTrue(new Pnt3D(8, 8, 3).Equals(
				              intersection.GetLinearIntersectionEndOnTarget(line2)));

			Assert.IsTrue(new Line3D(
				              new Pnt3D(6, 6, 0),
				              new Pnt3D(8, 8, 0)
			              ).Equals(intersection.TryGetIntersectionLine(line1)));

			Assert.AreEqual(0, intersection.GetRatioAlongTargetLinearStart());
			Assert.AreEqual(1.0, intersection.GetRatioAlongTargetLinearEnd());

			// Now the source is inside the target and reversed:
			line1.ReverseOrientation();
			intersection =
				SegmentIntersection.CalculateIntersectionXY(
					0, 7, line2, line1, 0.01);

			Assert.IsTrue(intersection.HasIntersection);
			Assert.IsNull(intersection.SingleInteriorIntersectionFactor);

			Assert.IsTrue(intersection.HasLinearIntersection);
			Assert.IsFalse(intersection.SegmentsAreEqualInXy);
			Assert.IsTrue(intersection.SourceStartIntersects);
			Assert.IsTrue(intersection.SourceEndIntersects);

			Assert.IsFalse(intersection.TargetStartIntersects);
			Assert.IsFalse(intersection.TargetEndIntersects);

			Assert.IsTrue(intersection.LinearIntersectionInOppositeDirection);
			Assert.IsTrue(intersection.HasSourceInteriorIntersection);

			Assert.AreEqual(0, intersection.GetFirstIntersectionAlongSource());

			Assert.IsTrue(new Pnt3D(6, 6, 3).Equals(
				              intersection.GetLinearIntersectionStart(
					              line2, out startFactorAlongSource)));
			Assert.AreEqual(0, startFactorAlongSource);

			Assert.IsTrue(new Pnt3D(8, 8, 3).Equals(
				              intersection.GetLinearIntersectionEnd(
					              line2, out endFactorAlongSource)));
			Assert.AreEqual(1, endFactorAlongSource);

			Assert.AreEqual(0, intersection.GetLinearIntersectionStartFactor(true));
			Assert.AreEqual(1, intersection.GetLinearIntersectionEndFactor(true));

			Assert.IsTrue(new Pnt3D(6, 6, 0).Equals(
				              intersection.GetLinearIntersectionStartOnTarget(line1)));
			Assert.IsTrue(new Pnt3D(8, 8, 0).Equals(
				              intersection.GetLinearIntersectionEndOnTarget(line1)));

			Assert.IsTrue(new Line3D(
				              new Pnt3D(6, 6, 3),
				              new Pnt3D(8, 8, 3)
			              ).Equals(intersection.TryGetIntersectionLine(line2)));

			Assert.AreEqual(0.4, intersection.GetRatioAlongTargetLinearStart());
			Assert.AreEqual(0.2, intersection.GetRatioAlongTargetLinearEnd());
		}

		private static void AssertPseudoLinearCornerTouch(
			Line3D line1, Line3D line2, double tolerance)
		{
			var intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, line1, line2, tolerance);
			var intersectionTargetIsSource =
				SegmentIntersection.CalculateIntersectionXY(0, 0, line2, line1, tolerance);

			Assert.True(intersection.HasIntersection);
			Assert.True(intersectionTargetIsSource.HasIntersection);

			// The two segments are not collinear (they diverge at an acute angle), so the
			// near-coincident corner is a point intersection, NOT a linear overlap.
			Assert.False(intersection.HasLinearIntersection);
			Assert.False(intersectionTargetIsSource.HasLinearIntersection);

			// ... but the configuration is still recognised as a potential pseudo-linear
			// intersection (the reason it must not be treated as a linear overlap).
			Assert.True(intersection.IsPotentialPseudoLinearIntersection(
				            line1, line2, tolerance));
			Assert.True(
				intersectionTargetIsSource.IsPotentialPseudoLinearIntersection(
					line2, line1, tolerance));
		}
	}
}

using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geometry
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

			// Special case: The source end is very far away and deviates more than the tolerance from the target straigh:
			line1.SetEndPoint(new Pnt3D(100, 0.02, 0));

			intersection =
				SegmentIntersection.CalculateIntersectionXY(1, 2, line1, line2, tolerance);
			Assert.True(intersection.HasLinearIntersection);
			Assert.True(intersection.LinearIntersectionInOppositeDirection);
		}

		[Test]
		public void CanDetermineCorrectOrientationForPseudoLinearIntersecion()
		{
			// The linear intersection can be somewhat un-intuitive if the end points are witin the tolerance to the
			// other line but just outside the tolerance to the other end point.

			Line3D line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(5, 0, 0));
			Line3D line2 = new Line3D(new Pnt3D(0.011, 0, 0), new Pnt3D(-10, 10, 0));

			const double tolerance = 0.01;

			SegmentIntersection intersection =
				AssertLinearIntersection(line1, line2, tolerance, true);

			double startAlongSource;
			Pnt3D sourceStart =
				intersection.GetLinearIntersectionStart(line1, out startAlongSource);

			double endAlongSource;
			Pnt3D sourceEnd = intersection.GetLinearIntersectionEnd(line1, out endAlongSource);

			line2.ReverseOrientation();
			AssertLinearIntersection(line1, line2, tolerance, false);

			line2 = new Line3D(new Pnt3D(0.009, -0.008, 0), new Pnt3D(-10, 10, 0));
			AssertLinearIntersection(line1, line2, tolerance, true);

			line2.ReverseOrientation();
			AssertLinearIntersection(line1, line2, tolerance, false);

			// Really acute:
			line2 = new Line3D(new Pnt3D(0.03, 0.0, 0), new Pnt3D(-10, 1, 0));
			AssertLinearIntersection(line1, line2, tolerance, true);

			line2.ReverseOrientation();
			AssertLinearIntersection(line1, line2, tolerance, false);
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

		private static SegmentIntersection AssertLinearIntersection(
			Line3D line1, Line3D line2, double tolerance,
			bool? oppositeDirection = null)
		{
			var intersection =
				SegmentIntersection.CalculateIntersectionXY(0, 0, line1, line2, tolerance);
			var intersectionTargetIsSource =
				SegmentIntersection.CalculateIntersectionXY(0, 0, line2, line1, tolerance);

			Assert.True(intersection.HasLinearIntersection);
			Assert.True(intersectionTargetIsSource.HasLinearIntersection);

			Assert.True(intersection.IsPotentialPseudoLinearIntersection(
				            line1, line2, tolerance));
			Assert.True(
				intersectionTargetIsSource.IsPotentialPseudoLinearIntersection(
					line2, line1, tolerance));

			if (oppositeDirection != null)
			{
				if (oppositeDirection.Value)
				{
					Assert.True(intersection.LinearIntersectionInOppositeDirection);
					Assert.True(intersectionTargetIsSource.LinearIntersectionInOppositeDirection);
				}
				else
				{
					Assert.False(intersection.LinearIntersectionInOppositeDirection);
					Assert.False(intersectionTargetIsSource.LinearIntersectionInOppositeDirection);
				}
			}

			return intersection;
		}
	}
}
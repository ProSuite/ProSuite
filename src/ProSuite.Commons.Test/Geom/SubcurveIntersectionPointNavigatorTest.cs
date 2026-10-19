using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class SubcurveIntersectionPointNavigatorTest
	{
		[Test]
		public void CanIdentifyBoundaryLoop()
		{
			// The target intersects sourceRing1
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			for (var i = 0; i < 4; i++)
			{
				MultiPolycurve source =
					new MultiPolycurve(new[]
					                   {
						                   GeomTestUtils.CreateRing(
							                   GeomTestUtils.GetRotatedRing(sourceRing1, i))
					                   });

				for (var t = 0; t < 5; t++)
				{
					// The target is a boundary loop that encompasses ring2:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(100, 100, 0),
						                       new Pnt3D(200, 100, 0),
						                       new Pnt3D(200, 0, 0),
						                       new Pnt3D(100, 0, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(150, 20, 0),
						                       new Pnt3D(175, 50, 0),
						                       new Pnt3D(150, 70, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(100, 100, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					const double tolerance = 0.001;
					var intersectionPoints =
						GeomTopoOpUtils.GetIntersectionPoints(
							(ISegmentList) source, target, tolerance);

					SubcurveIntersectionPointNavigator navigator =
						new SubcurveIntersectionPointNavigator(
							intersectionPoints, source, target, tolerance);

					Assert.AreEqual(0, navigator.GetSourceBoundaryLoops().Count());
					Assert.AreEqual(1, navigator.GetTargetBoundaryLoops().Count());

					// With flipped arguments:
					intersectionPoints =
						GeomTopoOpUtils.GetIntersectionPoints(
							(ISegmentList) target, source, tolerance);

					navigator =
						new SubcurveIntersectionPointNavigator(
							intersectionPoints, target, source, tolerance);

					Assert.AreEqual(1, navigator.GetSourceBoundaryLoops().Count());
					Assert.AreEqual(0, navigator.GetTargetBoundaryLoops().Count());
				}
			}
		}

		[Test]
		public void CanIdentifyBoundaryLoopFilledWithOtherRing()
		{
			// The target intersects sourceRing1
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Now add another outer ring to the source that is contained by the target boundary loop
			// and touches sourceRing1 in a point
			var sourceRing2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(150, 20, 0),
				                  new Pnt3D(100, 50, 0),
				                  new Pnt3D(150, 70, 0),
				                  new Pnt3D(175, 50, 0)
			                  };

			for (var i = 0; i < 4; i++)
			{
				MultiPolycurve source =
					new MultiPolycurve(new[] { GeomTestUtils.CreateRing(sourceRing1) });
				Linestring ring2 =
					GeomTestUtils.CreateRing(GeomTestUtils.GetRotatedRing(sourceRing2, i));
				source.AddLinestring(ring2);

				for (var t = 0; t < 5; t++)
				{
					// The target is a boundary loop that encompasses ring2:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(100, 100, 0),
						                       new Pnt3D(200, 100, 0),
						                       new Pnt3D(200, 0, 0),
						                       new Pnt3D(100, 0, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(150, 20, 0),
						                       new Pnt3D(175, 50, 0),
						                       new Pnt3D(150, 70, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(100, 100, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					const double tolerance = 0.001;
					var intersectionPoints =
						GeomTopoOpUtils.GetIntersectionPoints(
							(ISegmentList) source, target, tolerance);

					SubcurveIntersectionPointNavigator navigator =
						new SubcurveIntersectionPointNavigator(
							intersectionPoints, source, target, tolerance);

					Assert.AreEqual(0, navigator.GetSourceBoundaryLoops().Count());
					Assert.AreEqual(2, navigator.GetTargetBoundaryLoops().Count());

					// With flipped arguments:
					intersectionPoints =
						GeomTopoOpUtils.GetIntersectionPoints(
							(ISegmentList) target, source, tolerance);

					navigator =
						new SubcurveIntersectionPointNavigator(
							intersectionPoints, target, source, tolerance);

					Assert.AreEqual(2, navigator.GetSourceBoundaryLoops().Count());
					Assert.AreEqual(0, navigator.GetTargetBoundaryLoops().Count());
				}
			}
		}

		[Test]
		public void CanIdentifyBoundaryLoopWithExtraLoop()
		{
			// The target intersects sourceRing1
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Now add another outer ring to the source that is contained by the target boundary loop
			// and touches sourceRing1 in a point
			var sourceRing2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(150, 20, 0),
				                  new Pnt3D(100, 50, 0),
				                  new Pnt3D(150, 70, 0),
				                  new Pnt3D(175, 50, 0)
			                  };

			for (var i = 0; i < 4; i++)
			{
				MultiPolycurve source =
					new MultiPolycurve(new[] { GeomTestUtils.CreateRing(sourceRing1) });
				Linestring ring2 =
					GeomTestUtils.CreateRing(GeomTestUtils.GetRotatedRing(sourceRing2, i));
				source.AddLinestring(ring2);

				for (var t = 0; t < 5; t++)
				{
					// The target is a boundary loop that encompasses ring2:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(100, 100, 0),
						                       new Pnt3D(200, 100, 0),
						                       new Pnt3D(200, 0, 0),
						                       new Pnt3D(100, 0, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(150, 20, 0),
						                       new Pnt3D(175, 50, 0),
						                       new Pnt3D(180, 30, 0),
						                       new Pnt3D(190, 50, 0),
						                       new Pnt3D(180, 60, 0),
						                       new Pnt3D(175, 50, 0),
						                       new Pnt3D(150, 70, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(100, 100, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					const double tolerance = 0.001;
					var intersectionPoints =
						GeomTopoOpUtils.GetIntersectionPoints(
							(ISegmentList) source, target, tolerance);

					SubcurveIntersectionPointNavigator navigator =
						new SubcurveIntersectionPointNavigator(
							intersectionPoints, source, target, tolerance);

					Assert.AreEqual(0, navigator.GetSourceBoundaryLoops().Count());
					Assert.AreEqual(3, navigator.GetTargetBoundaryLoops().Count());

					// With flipped arguments:
					intersectionPoints =
						GeomTopoOpUtils.GetIntersectionPoints(
							(ISegmentList) target, source, tolerance);

					navigator =
						new SubcurveIntersectionPointNavigator(
							intersectionPoints, target, source, tolerance);

					Assert.AreEqual(2, navigator.GetSourceBoundaryLoops().Count());
					Assert.AreEqual(0, navigator.GetTargetBoundaryLoops().Count());

					foreach (BoundaryLoop sourceBoundaryLoop in navigator.GetSourceBoundaryLoops())
					{
						List<Linestring> rings = new List<Linestring>();
						foreach (IList<IntersectionRun> intersectionRuns in sourceBoundaryLoop
							         .GetLoopSubcurves())
						{
							Linestring ring = SubcurveUtils.CreateClosedRing(
								intersectionRuns.Select(ir => ir.Subcurve).ToList(), null,
								tolerance);

							rings.Add(ring);
						}

						Assert.AreEqual(3, rings.Count);
						Assert.AreEqual(target.GetArea2D(), rings.Sum(r => r.GetArea2D()));
					}
				}
			}
		}

		[Test]
		public void CanIdentifyBoundaryLoopWithChainedExtraLoops()
		{
			// Three-level nested boundary loops on a single ring exercise the
			// chained-extras path in BoundaryLoop.GetLoopSubcurves: one kept BL
			// carries TWO ExtraLoopIntersections, and the inner extra is nested
			// inside the middle extra (so recursion must descend through them in
			// order without re-visiting consumed extras).
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Touches the outer pinch (100,50) AND the middle pinch (175,50).
			var sourceRing2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(150, 20, 0),
				                  new Pnt3D(100, 50, 0),
				                  new Pnt3D(150, 70, 0),
				                  new Pnt3D(175, 50, 0)
			                  };

			// Touches the innermost pinch (185,40).
			var sourceRing3 = new List<Pnt3D>
			                  {
				                  new Pnt3D(183, 38, 0),
				                  new Pnt3D(185, 40, 0),
				                  new Pnt3D(183, 42, 0)
			                  };

			MultiPolycurve source =
				new MultiPolycurve(new[]
				                   {
					                   GeomTestUtils.CreateRing(sourceRing1),
					                   GeomTestUtils.CreateRing(sourceRing2),
					                   GeomTestUtils.CreateRing(sourceRing3)
				                   });

			// Target ring with three nested pinches at (100,50), (175,50), (185,40).
			var targetRingPoints = new List<Pnt3D>
			                       {
				                       new Pnt3D(100, 100, 0),
				                       new Pnt3D(200, 100, 0),
				                       new Pnt3D(200, 0, 0),
				                       new Pnt3D(100, 0, 0),
				                       new Pnt3D(100, 50, 0), // outer pinch v1
				                       new Pnt3D(150, 20, 0),
				                       new Pnt3D(175, 50, 0), // middle pinch v1
				                       new Pnt3D(180, 30, 0),
				                       new Pnt3D(185, 40, 0), // inner pinch v1
				                       new Pnt3D(188, 38, 0),
				                       new Pnt3D(190, 40, 0),
				                       new Pnt3D(188, 42, 0),
				                       new Pnt3D(185, 40, 0), // inner pinch v2
				                       new Pnt3D(190, 50, 0),
				                       new Pnt3D(180, 60, 0),
				                       new Pnt3D(175, 50, 0), // middle pinch v2
				                       new Pnt3D(150, 70, 0),
				                       new Pnt3D(100, 50, 0), // outer pinch v2
				                       new Pnt3D(100, 100, 0)
			                       };

			var target = new RingGroup(new Linestring(targetRingPoints));

			const double tolerance = 0.001;

			// With arguments flipped, the 3-pinch ring becomes the source so we get
			// SOURCE boundary loops with chained extras (the cross-XY case).
			var intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
				(ISegmentList) target, source, tolerance);

			var navigator = new SubcurveIntersectionPointNavigator(
				intersectionPoints, target, source, tolerance);

			List<BoundaryLoop> sourceBoundaryLoops =
				navigator.GetSourceBoundaryLoops().ToList();

			// Outer pinch (100,50) is touched by sourceRing1 AND sourceRing2 → two
			// pinch groups at the same source vertex → kept as separate BLs.
			Assert.AreEqual(2, sourceBoundaryLoops.Count);

			// Each kept BL must carry the two cross-source-XY pinch groups
			// (middle at (175,50), inner at (185,40)) as ExtraLoopIntersections.
			foreach (BoundaryLoop bl in sourceBoundaryLoops)
			{
				Assert.IsNotNull(bl.ExtraLoopIntersections);
				Assert.AreEqual(2, bl.ExtraLoopIntersections.Count);
			}

			// Each BL must decompose into exactly 4 atomic sub-rings:
			//   - the wrap-around outer (between (100,50) visits going around the
			//     big rectangle) — span 2 of the 2-pinch group, no extras inside,
			//   - the outer-with-middle-cut (span 1's main outline split at the
			//     middle pinch),
			//   - the middle-with-innermost-cut (recursion into the middle extra,
			//     split at the innermost pinch),
			//   - the innermost diamond (recursion into the innermost extra).
			// All four together must sum to the full target ring area exactly.
			double targetArea = target.GetArea2D();
			foreach (BoundaryLoop bl in sourceBoundaryLoops)
			{
				var rings = new List<Linestring>();
				foreach (IList<IntersectionRun> intersectionRuns in bl.GetLoopSubcurves())
				{
					Linestring ring = SubcurveUtils.CreateClosedRing(
						intersectionRuns.Select(ir => ir.Subcurve).ToList(), null,
						tolerance);
					rings.Add(ring);
				}

				Assert.AreEqual(4, rings.Count,
				                "expected 4 atomic sub-rings (wrap + outer + middle + innermost)");
				Assert.AreEqual(targetArea, rings.Sum(r => r.GetArea2D()), tolerance,
				                "atomic sub-rings should partition the full ring exactly");
			}
		}

		[Test]
		public void CanKeepLinearRunsSharingSourceStartAtTargetSpike()
		{
			// TOP-5999, regression guard for GetSubsumedLinearIntersectionPoints /
			// LinearStretchSubsumes: two linear intersection runs that start at the SAME
			// source location must both be kept.
			//
			// Real data: TLM_GEBAEUDEKOERPER 8711144 (Lugano), footprint union step 14,
			// tolerance 0.01. The target is a needle-thin triangle whose tip is narrower
			// than the tolerance, so the source leaves the shared vertex 6
			// (2716583.925, 1095766.421) along two different target segments:
			//    run A: source [6, 7] -> target [0.0000, 0.0048]   (the short flank)
			//    run B: source [6, 8] -> target [3.0000, 1.0000]   (the long flank)
			// These are two genuinely different overlaps, not one overlap described twice.
			// Dropping the shorter one makes the turning-left walk cut into the source and
			// the union comes back SMALLER than the source alone (119.07 -> 117.03 sq m).
			var sourcePoints = new List<Pnt3D>
			                   {
				                   new Pnt3D(2716579.275, 1095781.461, 346.060),
				                   new Pnt3D(2716581.418919, 1095780.669780, 346.060),
				                   new Pnt3D(2716587.851, 1095778.296, 346.060),
				                   new Pnt3D(2716587.775203, 1095778.058384, 346.060),
				                   new Pnt3D(2716588.245, 1095778.151, 346.060),
				                   new Pnt3D(2716586.354635, 1095773.018134, 346.060),
				                   new Pnt3D(2716583.925, 1095766.421, 346.060),
				                   new Pnt3D(2716583.882871, 1095766.439034, 346.060),
				                   new Pnt3D(2716574.985, 1095769.761, 360.077),
				                   new Pnt3D(2716575.498466, 1095770.028124, 346.060),
				                   new Pnt3D(2716575.139, 1095770.182, 346.060),
				                   new Pnt3D(2716577.268, 1095775.987, 346.060)
			                   };

			var targetPoints = new List<Pnt3D>
			                   {
				                   new Pnt3D(2716583.925, 1095766.421, 346.060),
				                   new Pnt3D(2716574.985, 1095769.761, 346.060),
				                   new Pnt3D(2716575.139, 1095770.182, 346.060)
			                   };

			const double tolerance = 0.01;

			RingGroup source = GeomTestUtils.CreatePoly(sourcePoints);
			RingGroup target = GeomTestUtils.CreatePoly(targetPoints);

			// The union pipeline always works on properly oriented rings:
			Assert.IsTrue(source.ExteriorRing.ClockwiseOriented);
			Assert.IsTrue(target.ExteriorRing.ClockwiseOriented);

			IList<IntersectionPoint3D> intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints((ISegmentList) source, target, tolerance);

			int runsStartingAtSharedVertex =
				intersectionPoints.Count(
					ip => ip.Type == IntersectionPointType.LinearIntersectionStart &&
					      MathUtils.AreEqual(ip.VirtualSourceVertex, 6));

			Assert.AreEqual(2, runsStartingAtSharedVertex,
			                "expected both flanks of the target spike to be seeded at vertex 6");

			var navigator = new SubcurveIntersectionPointNavigator(
				intersectionPoints, source, target, tolerance);

			// Neither run may be dropped as 'subsumed' by the other:
			Assert.AreEqual(intersectionPoints.Count, navigator.IntersectionPoints.Count,
			                "no linear intersection run may be removed as subsumed");

			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			// The union must never lose source area:
			Assert.GreaterOrEqual(union.GetArea2D(), source.GetArea2D());
			Assert.AreEqual(119.1580, union.GetArea2D(), 0.0005);
			Assert.AreEqual(1, union.PartCount);
		}

		[Test]
		public void CanKeepLinearRunsSharingSourceStartAtCoincidentTargetEdge()
		{
			// TOP-5999, second real-data case for the same LinearStretchSubsumes guard:
			// TLM_GEBAEUDEKOERPER 8711671 (Lugano), footprint union step 24, tolerance 0.01.
			// Same mechanism as CanKeepLinearRunsSharingSourceStartAtTargetSpike: the target
			// is a needle triangle sharing its long edge with the source boundary, and two
			// linear runs start at the same source location. Dropping the shorter of the two
			// cost 4 of 451 sq m in the footprint of this feature.
			var sourcePoints = new List<Pnt3D>
			                   {
				                   new Pnt3D(2716518.960, 1095858.961, 357.528),
				                   new Pnt3D(2716522.214, 1095868.263, 343.952),
				                   new Pnt3D(2716534.270, 1095864.031, 343.952),
				                   new Pnt3D(2716534.570, 1095864.893, 355.634),
				                   new Pnt3D(2716535.069, 1095866.325, 355.634),
				                   new Pnt3D(2716550.714, 1095860.833, 355.634),
				                   new Pnt3D(2716550.189, 1095859.337, 355.634),
				                   new Pnt3D(2716549.910, 1095858.542, 343.952),
				                   new Pnt3D(2716558.957, 1095855.366, 357.418),
				                   new Pnt3D(2716559.690, 1095855.090, 343.952),
				                   new Pnt3D(2716558.954952, 1095853.023017, 346.277098),
				                   new Pnt3D(2716558.418827, 1095851.515413, 347.972964),
				                   new Pnt3D(2716558.807, 1095851.667, 357.391),
				                   new Pnt3D(2716557.735, 1095848.549, 357.378),
				                   new Pnt3D(2716556.376, 1095845.533, 357.484),
				                   new Pnt3D(2716556.301, 1095845.560, 343.952),
				                   new Pnt3D(2716518.890, 1095858.762, 343.952)
			                   };

			var targetPoints = new List<Pnt3D>
			                   {
				                   new Pnt3D(2716518.960, 1095858.961, 357.528),
				                   new Pnt3D(2716556.376, 1095845.533, 357.484),
				                   new Pnt3D(2716518.890, 1095858.762, 357.445)
			                   };

			const double tolerance = 0.01;

			RingGroup source = GeomTestUtils.CreatePoly(sourcePoints);
			RingGroup target = GeomTestUtils.CreatePoly(targetPoints);

			Assert.IsTrue(source.ExteriorRing.ClockwiseOriented);
			Assert.IsTrue(target.ExteriorRing.ClockwiseOriented);

			IList<IntersectionPoint3D> intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints((ISegmentList) source, target, tolerance);

			var navigator = new SubcurveIntersectionPointNavigator(
				intersectionPoints, source, target, tolerance);

			Assert.AreEqual(intersectionPoints.Count, navigator.IntersectionPoints.Count,
			                "no linear intersection run may be removed as subsumed");

			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			Assert.GreaterOrEqual(union.GetArea2D(), source.GetArea2D());
			Assert.AreEqual(1, union.PartCount);
		}

	}
}

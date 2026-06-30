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
	}
}

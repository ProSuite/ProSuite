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
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class RingOperatorTest
	{
		[Test]
		public void TargetInsideIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole in source:
			var sourceIsland = new[]
			                   {
				                   new Pnt3D(20, 80, 0),
				                   new Pnt3D(20, 20, 0),
				                   new Pnt3D(80, 20, 0),
				                   new Pnt3D(80, 80, 0)
			                   }.ToList();

			// The target is inside the source island
			var insideIsland = new[]
			                   {
				                   new Pnt3D(30, 30, 0),
				                   new Pnt3D(30, 70, 0),
				                   new Pnt3D(70, 70, 0),
				                   new Pnt3D(70, 30, 0)
			                   }.ToList();

			RingGroup source = GeomTestUtils.CreatePoly(ring1);

			var sourceInteriorRing = GeomTestUtils.CreateRing(sourceIsland);
			source.AddInteriorRing(sourceInteriorRing);

			RingGroup target = GeomTestUtils.CreatePoly(insideIsland);

			RingOperator ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring intersection = ringOperator.IntersectXY();
			Assert.IsTrue(intersection.IsEmpty);

			// Compare with difference:
			MultiLinestring difference = ringOperator.DifferenceXY();
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(source.GetArea2D(), difference.GetArea2D());

			// Union:
			ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring union = ringOperator.UnionXY();
			Assert.AreEqual(3, union.PartCount);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), union.GetArea2D());

			// Vice versa to check symmetry:
			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(target, source, tolerance);
			Assert.IsTrue(intersection.IsEmpty);

			difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, source, tolerance);
			Assert.AreEqual(1, difference.PartCount);
			Assert.AreEqual(target.GetArea2D(), difference.GetArea2D());

			union =
				GeomTopoOpUtils.GetUnionAreasXY(target, source, tolerance);
			Assert.AreEqual(3, union.PartCount);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), union.GetArea2D());
		}

		[Test]
		public void TargetWithIslandInsideIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole in source:
			var sourceIsland = new[]
			                   {
				                   new Pnt3D(20, 80, 0),
				                   new Pnt3D(20, 20, 0),
				                   new Pnt3D(80, 20, 0),
				                   new Pnt3D(80, 80, 0)
			                   }.ToList();

			// The target is inside the source island
			var insideIsland = new[]
			                   {
				                   new Pnt3D(30, 30, 0),
				                   new Pnt3D(30, 70, 0),
				                   new Pnt3D(70, 70, 0),
				                   new Pnt3D(70, 30, 0)
			                   }.ToList();

			var targetIsland = new[]
			                   {
				                   new Pnt3D(40, 60, 0),
				                   new Pnt3D(40, 40, 0),
				                   new Pnt3D(60, 40, 0),
				                   new Pnt3D(60, 60, 0)
			                   }.ToList();

			RingGroup source = GeomTestUtils.CreatePoly(ring1);

			var sourceInteriorRing = GeomTestUtils.CreateRing(sourceIsland);
			source.AddInteriorRing(sourceInteriorRing);

			RingGroup target = GeomTestUtils.CreatePoly(insideIsland);
			target.AddInteriorRing(GeomTestUtils.CreateRing(targetIsland));

			RingOperator ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring intersection = ringOperator.IntersectXY();
			Assert.IsTrue(intersection.IsEmpty);

			// Compare with difference:
			MultiLinestring difference = ringOperator.DifferenceXY();
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(source.GetArea2D(), difference.GetArea2D());

			// Union:
			ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring union = ringOperator.UnionXY();
			Assert.AreEqual(4, union.PartCount);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), union.GetArea2D());

			// Vice versa to check symmetry:
			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(target, source, tolerance);
			Assert.IsTrue(intersection.IsEmpty);

			difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, source, tolerance);
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(target.GetArea2D(), difference.GetArea2D());

			union =
				GeomTopoOpUtils.GetUnionAreasXY(target, source, tolerance);
			Assert.AreEqual(4, union.PartCount);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), union.GetArea2D());
		}

		[Test]
		public void TouchingRingsUnionCreatesIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 80, 9),
				            new Pnt3D(70, 80, 0),
				            new Pnt3D(70, 40, 9),
				            new Pnt3D(100, 40, 9),
				            new Pnt3D(100, 0, 9),
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(100, 0, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(200, 100, 0),
				            new Pnt3D(200, 0, 0)
			            };

			RingGroup source = GeomTestUtils.CreatePoly(ring1);
			RingGroup target = GeomTestUtils.CreatePoly(ring2);

			const double tolerance = 0.01;

			MultiLinestring unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);

			Assert.AreEqual(2, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(false, unionResult.GetLinestring(1).ClockwiseOriented);

			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), unionResult.GetArea2D(),
			                0.0001);

			MultiLinestring difference = GeomTopoOpUtils.GetDifferenceAreasXY(
				unionResult, source, tolerance);

			Assert.AreEqual(1, difference.PartCount);
			Assert.AreEqual(target.GetArea2D(), difference.GetArea2D(), 0.0001);

			MultiLinestring intersection = GeomTopoOpUtils.GetIntersectionAreasXY(
				unionResult, source, tolerance);

			Assert.AreEqual(1, difference.PartCount);
			Assert.AreEqual(source.GetArea2D(), intersection.GetArea2D(), 0.0001);
		}

		[Test]
		public void TargetWithIslandFillsIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole in source:
			var sourceIsland = new[]
			                   {
				                   new Pnt3D(20, 80, 0),
				                   new Pnt3D(20, 20, 0),
				                   new Pnt3D(80, 20, 0),
				                   new Pnt3D(80, 80, 0)
			                   }.ToList();

			// The target fills the source island
			var insideIsland = sourceIsland.AsEnumerable().Reverse().ToList();

			var targetIsland = new[]
			                   {
				                   new Pnt3D(40, 60, 0),
				                   new Pnt3D(40, 40, 0),
				                   new Pnt3D(60, 40, 0),
				                   new Pnt3D(60, 60, 0)
			                   }.ToList();

			RingGroup source = GeomTestUtils.CreatePoly(ring1);

			var sourceInteriorRing = GeomTestUtils.CreateRing(sourceIsland);
			source.AddInteriorRing(sourceInteriorRing);

			RingGroup target = GeomTestUtils.CreatePoly(insideIsland);
			target.AddInteriorRing(GeomTestUtils.CreateRing(targetIsland));

			RingOperator ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring intersection = ringOperator.IntersectXY();
			Assert.IsTrue(intersection.IsEmpty);

			// Compare with difference:
			MultiLinestring difference = ringOperator.DifferenceXY();
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(source.GetArea2D(), difference.GetArea2D());

			// Union:
			ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring union = ringOperator.UnionXY();
			Assert.AreEqual(2, union.PartCount);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), union.GetArea2D());

			// Vice versa to check symmetry:
			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(target, source, tolerance);
			Assert.IsTrue(intersection.IsEmpty);

			difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, source, tolerance);
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(target.GetArea2D(), difference.GetArea2D());

			union =
				GeomTopoOpUtils.GetUnionAreasXY(target, source, tolerance);
			Assert.AreEqual(2, union.PartCount);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), union.GetArea2D());
		}

		[Test]
		public void TargetWithIslandContainsIsland()
		{
			// Now the target outer ring contains the source island.
			// Additionally, the source island contains the target island.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole in source:
			var sourceIsland = new[]
			                   {
				                   new Pnt3D(20, 80, 0),
				                   new Pnt3D(20, 20, 0),
				                   new Pnt3D(80, 20, 0),
				                   new Pnt3D(80, 80, 0)
			                   }.ToList();

			// The target contains the above source island
			var insideIsland = new[]
			                   {
				                   new Pnt3D(10, 10, 0),
				                   new Pnt3D(10, 90, 0),
				                   new Pnt3D(90, 90, 0),
				                   new Pnt3D(90, 10, 0)
			                   }.ToList();

			var targetIsland = new[]
			                   {
				                   new Pnt3D(40, 60, 0),
				                   new Pnt3D(40, 40, 0),
				                   new Pnt3D(60, 40, 0),
				                   new Pnt3D(60, 60, 0)
			                   }.ToList();

			Linestring sourceOuterRing = GeomTestUtils.CreateRing(ring1);
			RingGroup source = new RingGroup(sourceOuterRing);

			var sourceInteriorRing = GeomTestUtils.CreateRing(sourceIsland);
			source.AddInteriorRing(sourceInteriorRing);

			Linestring targetOuterRing = GeomTestUtils.CreateRing(insideIsland);
			RingGroup target = new RingGroup(targetOuterRing);
			Linestring targetInteriorRing = GeomTestUtils.CreateRing(targetIsland);
			target.AddInteriorRing(targetInteriorRing);

			RingOperator ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring intersection = ringOperator.IntersectXY();
			Assert.AreEqual(2, intersection.PartCount);
			Assert.AreEqual(targetOuterRing.GetArea2D() + sourceInteriorRing.GetArea2D(),
			                intersection.GetArea2D());

			// Compare with difference:
			MultiLinestring difference = ringOperator.DifferenceXY();
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(sourceOuterRing.GetArea2D() - targetOuterRing.GetArea2D(),
			                difference.GetArea2D());

			// Union:
			ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring union = ringOperator.UnionXY();
			Assert.AreEqual(2, union.PartCount);
			Assert.AreEqual(sourceOuterRing.GetArea2D() + targetInteriorRing.GetArea2D(),
			                union.GetArea2D());

			// Vice versa to check symmetry:
			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(target, source, tolerance);
			Assert.AreEqual(2, intersection.PartCount);
			Assert.AreEqual(targetOuterRing.GetArea2D() + sourceInteriorRing.GetArea2D(),
			                intersection.GetArea2D());

			difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, source, tolerance);
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(targetInteriorRing.GetArea2D() - sourceInteriorRing.GetArea2D(),
			                difference.GetArea2D());

			union =
				GeomTopoOpUtils.GetUnionAreasXY(target, source, tolerance);
			Assert.AreEqual(2, union.PartCount);
			Assert.AreEqual(sourceOuterRing.GetArea2D() + targetInteriorRing.GetArea2D(),
			                union.GetArea2D());
		}

		[Test]
		public void BoundaryLoopFilled()
		{
			// The source has an esri-style boundary loop
			// The target fills the 'island', i.e. the boundary loop part that is not part of the polygon.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(20, 60, 0),
				            new Pnt3D(20, 40, 0),
				            new Pnt3D(50, 40, 0),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			WithRotatedRingGroup(ring1, poly1 =>
			{
				// The target fills the 'island' completely:
				var targetRingPoints = new List<Pnt3D>
				                       {
					                       new Pnt3D(50, 100, 9),
					                       new Pnt3D(50, 40, 0),
					                       new Pnt3D(20, 40, 0),
					                       new Pnt3D(20, 60, 0),
				                       };

				WithRotatedRingGroup(targetRingPoints, target =>
				{
					RingOperator ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring intersection = ringOperator.IntersectXY();
					Assert.IsTrue(intersection.IsEmpty);

					// Currently the boundary loop remains a boundary loop also in the result (esri style)
					//
					// Compare with difference:
					MultiLinestring difference = ringOperator.DifferenceXY();
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

					//MultiPolycurve multiTarget = new MultiPolycurve(target.GetLinestrings());
					//multiTarget.AddLinestring(new Linestring(new[]
					//										 {
					//											 new Pnt3D(80, 80, 10),
					//											 new Pnt3D(80, 90, 10),
					//											 new Pnt3D(120, 90, 10),
					//											 new Pnt3D(120, 80, 10),
					//											 new Pnt3D(80, 80, 10)
					//										 }));

					// Union:
					ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring union = ringOperator.UnionXY();
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D() + target.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(target.GetArea2D(), difference.GetArea2D());

					union =
						GeomTopoOpUtils.GetUnionAreasXY(target, poly1, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D() + target.GetArea2D(), union.GetArea2D());
				});
			});
		}

		[Test]
		public void BoundaryLoopFilledPartiallyTouchingLoopPoint()
		{
			// The source has an esri-style boundary loop
			// The target fills the 'island', i.e. the boundary loop part that is not part of the polygon.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(20, 60, 0),
				            new Pnt3D(20, 40, 0),
				            new Pnt3D(50, 40, 0),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			WithRotatedRingGroup(ring1, poly1 =>
			{
				// The target fills the 'island' completely:
				var targetRingPoints = new List<Pnt3D>
				                       {
					                       new Pnt3D(50, 60, 0),
					                       new Pnt3D(20, 60, 0),
					                       new Pnt3D(50, 100, 0),
				                       };

				WithRotatedRingGroup(targetRingPoints, target =>
				{
					RingOperator ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring intersection = ringOperator.IntersectXY();
					Assert.IsTrue(intersection.IsEmpty);

					// Currently the boundary loop remains a boundary loop also in the result (esri style)
					//
					// Compare with difference:
					MultiLinestring difference = ringOperator.DifferenceXY();
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

					//MultiPolycurve multiTarget = new MultiPolycurve(target.GetLinestrings());
					//multiTarget.AddLinestring(new Linestring(new[]
					//										 {
					//											 new Pnt3D(80, 80, 10),
					//											 new Pnt3D(80, 90, 10),
					//											 new Pnt3D(120, 90, 10),
					//											 new Pnt3D(120, 80, 10),
					//											 new Pnt3D(80, 80, 10)
					//										 }));

					// Union:
					ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring union = ringOperator.UnionXY();
					Assert.AreEqual(2, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D() + target.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(target.GetArea2D(), difference.GetArea2D());

					union =
						GeomTopoOpUtils.GetUnionAreasXY(target, poly1, tolerance);
					Assert.AreEqual(2, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D() + target.GetArea2D(), union.GetArea2D());
				});
			});
		}

		private static void WithRotatedRingGroup([NotNull] List<Pnt3D> ringPoints,
		                                         [NotNull] Action<RingGroup> procedure,
		                                         int intialRotation = 0,
		                                         int rotationCount = -1)
		{
			if (rotationCount < 0)
			{
				rotationCount = ringPoints.Count;
			}

			for (int i = intialRotation; i < rotationCount; i++)
			{
				RingGroup poly =
					GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ringPoints, i));

				procedure(poly);
			}
		}
	}
}

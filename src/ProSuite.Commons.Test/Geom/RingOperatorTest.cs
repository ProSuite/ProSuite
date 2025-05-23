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
		public void EqualWithBothInnerRings()
		{
			// The outer rings are equal, each with an inner ring that do not intersect each other.
			// The result should be a poly equal to the outer rings for union.
			var outerRing = new List<Pnt3D>
			                {
				                new Pnt3D(0, 0, 0),
				                new Pnt3D(0, 100, 0),
				                new Pnt3D(100, 100, 0),
				                new Pnt3D(100, 0, 0)
			                };

			var inner1Points = new List<Pnt3D>
			                   {
				                   new Pnt3D(25, 50, 0),
				                   new Pnt3D(50, 50, 0),
				                   new Pnt3D(50, 75, 0),
				                   new Pnt3D(25, 75, 0)
			                   };

			var inner2Points = new List<Pnt3D>
			                   {
				                   new Pnt3D(50, 50, 0),
				                   new Pnt3D(60, 50, 0),
				                   new Pnt3D(60, 75, 0),
				                   new Pnt3D(50, 75, 0)
			                   };

			Linestring inner1Ring = GeomTestUtils.CreateRing(inner1Points);
			Linestring inner2Ring = GeomTestUtils.CreateRing(inner2Points);

			RingGroup poly1 =
				new RingGroup(GeomTestUtils.CreateRing(outerRing), new[] { inner1Ring });
			RingGroup poly2 =
				new RingGroup(GeomTestUtils.CreateRing(outerRing), new[] { inner2Ring });

			double tolerance = 0.001;
			var ringOperator = new RingOperator(poly1, poly2, tolerance);
			MultiLinestring union = ringOperator.UnionXY();

			// All islands are removed
			Assert.AreEqual(1, union.PartCount);

			double expectedArea = poly1.GetLinestring(0).GetArea2D();
			Assert.AreEqual(expectedArea, union.GetArea2D());
		}

		[Test]
		public void EqualWithTouchingInnerRings()
		{
			// In touching inner rings, there is no start point for ring navigation
			var outerRing = new List<Pnt3D>
			                {
				                new Pnt3D(0, 0, 0),
				                new Pnt3D(0, 100, 0),
				                new Pnt3D(100, 100, 0),
				                new Pnt3D(100, 0, 0)
			                };

			var inner1Points = new List<Pnt3D>
			                   {
				                   new Pnt3D(25, 50, 0),
				                   new Pnt3D(50, 50, 0),
				                   new Pnt3D(50, 75, 0),
				                   new Pnt3D(25, 75, 0)
			                   };

			var inner2PointsTouchingFromOutside = new List<Pnt3D>
			                                      {
				                                      new Pnt3D(50, 50, 0),
				                                      new Pnt3D(60, 50, 0),
				                                      new Pnt3D(60, 75, 0),
				                                      new Pnt3D(50, 75, 0)
			                                      };

			Linestring inner1Ring = GeomTestUtils.CreateRing(inner1Points);
			Linestring inner2RingTouchingFromOutside =
				GeomTestUtils.CreateRing(inner2PointsTouchingFromOutside);

			RingGroup poly1 =
				new RingGroup(GeomTestUtils.CreateRing(outerRing), new[] { inner1Ring });
			RingGroup poly2 =
				new RingGroup(GeomTestUtils.CreateRing(outerRing),
				              new[] { inner2RingTouchingFromOutside });

			const double tolerance = 0.001;
			var ringOperator = new RingOperator(poly1, poly2, tolerance);
			MultiLinestring union = ringOperator.UnionXY();

			// All islands are removed
			Assert.AreEqual(1, union.PartCount);

			double expectedArea = poly1.GetLinestring(0).GetArea2D();

			Assert.AreEqual(expectedArea, union.GetArea2D());

			// Now the second poly's island is contained
			var inner2PointsInsideOtherIsland = new List<Pnt3D>
			                                    {
				                                    new Pnt3D(30, 60, 0),
				                                    new Pnt3D(40, 60, 0),
				                                    new Pnt3D(40, 70, 0),
				                                    new Pnt3D(30, 70, 0)
			                                    };
			Linestring inner2RingInsideOtherIsland =
				GeomTestUtils.CreateRing(inner2PointsInsideOtherIsland);
			poly2 = new RingGroup(GeomTestUtils.CreateRing(outerRing),
			                      new[] { inner2RingInsideOtherIsland });

			ringOperator = new RingOperator(poly1, poly2, tolerance);
			union = ringOperator.UnionXY();

			// The smaller island remains
			Assert.AreEqual(2, union.PartCount);
			expectedArea = poly1.GetLinestring(0).GetArea2D() +
			               inner2RingInsideOtherIsland.GetArea2D();

			// and vice-versa
			ringOperator = new RingOperator(poly2, poly1, tolerance);
			union = ringOperator.UnionXY();

			// The smaller island remains
			Assert.AreEqual(2, union.PartCount);
			expectedArea = poly1.GetLinestring(0).GetArea2D() +
			               inner2RingInsideOtherIsland.GetArea2D();

			Assert.AreEqual(expectedArea, union.GetArea2D());

			// Now the second poly's island is still contained but touching from the inside
			inner2PointsInsideOtherIsland = new List<Pnt3D>
			                                {
				                                new Pnt3D(30, 50, 0),
				                                new Pnt3D(40, 50, 0),
				                                new Pnt3D(40, 70, 0),
				                                new Pnt3D(30, 70, 0)
			                                };
			inner2RingInsideOtherIsland =
				GeomTestUtils.CreateRing(inner2PointsInsideOtherIsland);
			poly2 = new RingGroup(GeomTestUtils.CreateRing(outerRing),
			                      new[] { inner2RingInsideOtherIsland });

			ringOperator = new RingOperator(poly1, poly2, tolerance);
			union = ringOperator.UnionXY();

			// The smaller island remains
			Assert.AreEqual(2, union.PartCount);

			expectedArea = poly1.GetLinestring(0).GetArea2D() +
			               inner2RingInsideOtherIsland.GetArea2D();

			Assert.AreEqual(expectedArea, union.GetArea2D());

			// and vice versa:
			ringOperator = new RingOperator(poly2, poly1, tolerance);
			union = ringOperator.UnionXY();

			// The smaller island remains
			Assert.AreEqual(2, union.PartCount);

			Assert.AreEqual(expectedArea, union.GetArea2D());
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

		[Test]
		public void InteriorTouchingRingFilledPartiallyWithRingTouchingOuterRing()
		{
			// The source has an interior ring that touches the outer ring
			// The target partially fills the island and also touches the source's outer ring.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(20, 60, 0),
				            new Pnt3D(20, 40, 0),
				            new Pnt3D(50, 40, 0),
				            new Pnt3D(50, 100, 0)
			            };

			var targetRingPoints = new List<Pnt3D>
			                       {
				                       new Pnt3D(20, 60, 0),
				                       new Pnt3D(50, 100, 0),
				                       new Pnt3D(20, 0, 0),
			                       };

			const double tolerance = 0.01;

			int i = 0;
			WithRotatedRingGroup(ring1, poly1 =>
			{
				poly1.AddLinestring(new Linestring(GeomTestUtils.GetRotatedRing(ring2, i++)));

				WithRotatedRingGroup(targetRingPoints, target =>
				{
					RingOperator ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring intersection = ringOperator.IntersectXY();
					Assert.IsFalse(intersection.IsEmpty);

					Assert.AreEqual(240, intersection.GetArea2D(), 0.0001);

					// Compare with difference:
					MultiLinestring difference = ringOperator.DifferenceXY();
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(8560, difference.GetArea2D(), 0.0001);

					Assert.AreEqual(poly1.GetArea2D(),
					                intersection.GetArea2D() + difference.GetArea2D(), 0.0001);

					// Union:
					ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring union = ringOperator.UnionXY();
					Assert.AreEqual(2, union.PartCount);

					double targetPartInIsland = target.GetArea2D() - intersection.GetArea2D();
					double expectedArea = poly1.GetArea2D() + targetPartInIsland;
					Assert.AreEqual(expectedArea, union.GetArea2D(), 0.0001);

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(intersection.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(targetPartInIsland, difference.GetArea2D());

					union =
						GeomTopoOpUtils.GetUnionAreasXY(target, poly1, tolerance);
					Assert.AreEqual(2, union.PartCount);
					Assert.AreEqual(expectedArea, union.GetArea2D());
				});
			});
		}

		[Test]
		public void TouchingRingsInPointsFilled()
		{
			// The source has multiple parts that touch in a point
			// The target fills the 'island', i.e. the boundary loop part that is not part of the polygon.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(20, 60, 0),
				            new Pnt3D(20, 40, 0),
				            new Pnt3D(50, 0, 0),
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(50, 0, 0),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			const double tolerance = 0.01;

			WithRotatedRingGroup(ring1, poly1 =>
			{
				poly1.AddLinestring(GeomTestUtils.CreateRing(ring2));

				// The target fills the gap completely:
				var targetRingPoints = new List<Pnt3D>
				                       {
					                       new Pnt3D(50, 0, 9),
					                       new Pnt3D(20, 40, 0),
					                       new Pnt3D(20, 60, 0),
					                       new Pnt3D(50, 100, 9)
				                       };

				WithRotatedRingGroup(targetRingPoints, target =>
				{
					RingOperator ringOperator = new RingOperator(poly1, target, tolerance);
					MultiLinestring intersection = ringOperator.IntersectXY();
					Assert.IsTrue(intersection.IsEmpty);

					// Compare with difference:
					MultiLinestring difference = ringOperator.DifferenceXY();
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

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

					// TODO: This results in non-simple touching rings.
					// Either fix with simplification or call Union for each target ring
					//union =
					//	GeomTopoOpUtils.GetUnionAreasXY(target, poly1, tolerance);
					//Assert.AreEqual(1, union.PartCount);
					//Assert.AreEqual(poly1.GetArea2D() + target.GetArea2D(), union.GetArea2D());
				});
			});
		}

		[Test]
		public void CutWithCutLineEndingWithShortSegment()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(2600000, 1200000, 9),
				            new Pnt3D(2600000, 1200100, 9),
				            new Pnt3D(2600100, 1200100, 9),
				            new Pnt3D(2600100, 1200000, 9)
			            };

			const double tolerance = 0.01;

			WithRotatedRingGroup(ring1, poly1 =>
			{
				var targetPoints = new List<Pnt3D>
				                   {
					                   new Pnt3D(2600050.0, 1200100, 0),
					                   new Pnt3D(2600050.0, 1200100.001, 0),
					                   //new Pnt3D(2600020, 1200000.001, 0),
					                   new Pnt3D(2600020, 1200000, 0)
				                   };

				Linestring target = new Linestring(targetPoints);

				RingOperator ringOperator = new RingOperator(poly1, target, tolerance);
				bool success = ringOperator.CutXY(out IList<Linestring> leftRings,
				                                  out IList<Linestring> rightRings);

				Assert.IsTrue(success);
				Assert.AreEqual(1, leftRings.Count);
				Assert.AreEqual(1, rightRings.Count);

				// Re-assemble
				ringOperator = new RingOperator(leftRings[0], rightRings[0], tolerance);
				MultiLinestring union = ringOperator.UnionXY();
				Assert.AreEqual(1, union.PartCount);
				Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

				// flip the cut line
				target.ReverseOrientation();

				ringOperator = new RingOperator(poly1, target, tolerance);
				success = ringOperator.CutXY(out leftRings, out rightRings);

				Assert.IsTrue(success);
				Assert.AreEqual(1, leftRings.Count);
				Assert.AreEqual(1, rightRings.Count);

				ringOperator = new RingOperator(leftRings[0], rightRings[0], tolerance);
				union = ringOperator.UnionXY();
				Assert.AreEqual(1, union.PartCount);
				Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());
			});

			// Now with an extra vertex at the start point of the target:
			ring1 = new List<Pnt3D>
			        {
				        new Pnt3D(2600000, 1200000, 9),
				        new Pnt3D(2600000, 1200100, 9),
				        new Pnt3D(2600050, 1200100, 0), // target start
				        new Pnt3D(2600100, 1200100, 9),
				        new Pnt3D(2600100, 1200000, 9)
			        };

			WithRotatedRingGroup(ring1, poly1 =>
			{
				var targetPoints = new List<Pnt3D>
				                   {
					                   new Pnt3D(2600050.0, 1200100, 0),
					                   new Pnt3D(2600050.0, 1200100.001, 0),
					                   new Pnt3D(2600020, 1200000, 0)
				                   };

				Linestring target = new Linestring(targetPoints);

				RingOperator ringOperator = new RingOperator(poly1, target, tolerance);
				bool success = ringOperator.CutXY(out IList<Linestring> leftRings,
				                                  out IList<Linestring> rightRings);

				Assert.IsTrue(success);
				Assert.AreEqual(1, leftRings.Count);
				Assert.AreEqual(1, rightRings.Count);

				// Re-assemble
				ringOperator = new RingOperator(leftRings[0], rightRings[0], tolerance);
				MultiLinestring union = ringOperator.UnionXY();
				Assert.AreEqual(1, union.PartCount);
				Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

				// flip the cut line
				target.ReverseOrientation();

				ringOperator = new RingOperator(poly1, target, tolerance);
				success = ringOperator.CutXY(out leftRings, out rightRings);

				Assert.IsTrue(success);
				Assert.AreEqual(1, leftRings.Count);
				Assert.AreEqual(1, rightRings.Count);

				ringOperator = new RingOperator(leftRings[0], rightRings[0], tolerance);
				union = ringOperator.UnionXY();
				Assert.AreEqual(1, union.PartCount);
				Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());
			});
		}

		[Test]
		public void SourceCappedSpikeTouchingTargetCorner()
		{
			// This is what happens in TOP-5547: An empty 2-segment part is created during difference!
			var sourceSpike = new[]
			                  {
				                  new Pnt3D(105, 100, 0),
				                  new Pnt3D(110, 100, 0),
				                  new Pnt3D(100.006, 0, 9),
				                  new Pnt3D(100.003, 0.001, 9)
			                  }.ToList();

			// The target cuts the point of the spike:
			var targetRing = new List<Pnt3D>
			                 {
				                 new Pnt3D(0, 0, 9),
				                 new Pnt3D(0, 100, 9),
				                 new Pnt3D(100, 100, 9),
				                 new Pnt3D(100, 0, 9)
			                 };

			const double tolerance = 0.01;

			Linestring sourceSpikeRing = GeomTestUtils.CreateRing(sourceSpike);

			MultiPolycurve source = new MultiPolycurve(new[] { sourceSpikeRing });

			Linestring targetOuterRing = GeomTestUtils.CreateRing(targetRing);
			RingGroup target = new RingGroup(targetOuterRing);

			RingOperator ringOperator = new RingOperator(source, target, tolerance);
			MultiLinestring difference = ringOperator.DifferenceXY();
			Assert.AreEqual(1, difference.PartCount);
			Assert.AreEqual(sourceSpikeRing.GetArea2D(), difference.GetArea2D());

			// Compare with difference:
			MultiLinestring intersection = ringOperator.IntersectXY();
			Assert.IsTrue(intersection.IsEmpty);

			// Vice versa to check symmetry:
			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(target, source, tolerance);
			Assert.IsTrue(intersection.IsEmpty);

			difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, source, tolerance);
			Assert.AreEqual(1, difference.PartCount);
			Assert.AreEqual(target.GetArea2D(), difference.GetArea2D());
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

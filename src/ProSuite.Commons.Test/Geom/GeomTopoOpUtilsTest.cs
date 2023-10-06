using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class GeomTopoOpUtilsTest
	{
		[Test]
		public void CanProcessEmptyGeometries()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 9),
				           new Pnt3D(0, 100, 9),
				           new Pnt3D(100, 50, 9),
				           new Pnt3D(100, 20, 9)
			           };

			MultiLinestring poly = GeomTestUtils.CreatePoly(ring);
			MultiLinestring empty = MultiPolycurve.CreateEmpty();

			double tolerance = 0.001;

			// UnionAreasXY
			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(poly, empty, tolerance);
			Assert.AreEqual(poly.GetArea2D(), union.GetArea2D());
			Assert.IsFalse(GeomTopoOpUtils.CanDissolveAreasXY(poly, empty, tolerance, out _));

			union = GeomTopoOpUtils.GetUnionAreasXY(empty, poly, tolerance);
			Assert.AreEqual(poly.GetArea2D(), union.GetArea2D());
			Assert.IsFalse(GeomTopoOpUtils.CanDissolveAreasXY(empty, poly, tolerance, out _));

			// IntersectionAreasXY
			MultiLinestring intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly, empty, tolerance);
			Assert.AreEqual(0, intersection.GetArea2D());

			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(empty, poly, tolerance);
			Assert.AreEqual(0, intersection.GetArea2D());

			// DifferenceAreasXY
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(poly, empty, tolerance);
			Assert.AreEqual(poly.GetArea2D(), difference.GetArea2D());

			difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(empty, poly, tolerance);
			Assert.AreEqual(0, difference.GetArea2D());
		}

		[Test]
		public void CanRemoveOverlapsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var overlapping = new List<Pnt3D>();
			overlapping.Add(new Pnt3D(40, -10, 0));
			overlapping.Add(new Pnt3D(40, 30, 0));
			overlapping.Add(new Pnt3D(200, 30, 0));
			overlapping.Add(new Pnt3D(200, -10, 0));

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring overlap = GeomTestUtils.CreateRing(overlapping);

			const double tolerance = 0.01;

			MultiLinestring differenceResult = GeomTopoOpUtils.GetDifferenceAreasXY(
				poly1, new MultiPolycurve(new[] { overlap }), tolerance);
			Assert.AreEqual(1, differenceResult.PartCount);
			Assert.AreEqual(true, differenceResult.GetLinestring(0).ClockwiseOriented);

			var expected = GeomTestUtils.CreateRing(new List<Pnt3D>
			                                        {
				                                        new Pnt3D(0, 0, 9),
				                                        new Pnt3D(0, 100, 9),
				                                        new Pnt3D(100, 50, 9),
				                                        new Pnt3D(100, 30, 9),
				                                        new Pnt3D(40, 30, 9),
				                                        new Pnt3D(40, 8, 9),
			                                        });

			Assert.AreEqual(expected.GetArea2D(), differenceResult.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanRemoveIslandXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring containedRing = GeomTestUtils.CreateRing(new[]
			                                                    {
				                                                    new Pnt3D(25, 75, 0),
				                                                    new Pnt3D(50, 75, 0),
				                                                    new Pnt3D(50, 50, 0),
				                                                    new Pnt3D(25, 50, 0)
			                                                    }.ToList());

			const double tolerance = 0.01;

			var target = new MultiPolycurve(new[] { containedRing });
			MultiLinestring differenceResult =
				GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(2, differenceResult.PartCount);
			Assert.AreEqual(true, differenceResult.GetLinestring(0).ClockwiseOriented);

			Assert.True(
				GeomTopoOpUtils.AreEqualXY(poly1.ExteriorRing, differenceResult.GetLinestring(0),
				                           tolerance));
			Assert.AreEqual(poly1.GetArea2D() - containedRing.GetArea2D(),
			                differenceResult.GetArea2D(), 0.0001);

			// Now the same with the target being equal to the source-island:
			var sourceWithIsland = differenceResult.Clone();
			differenceResult =
				GeomTopoOpUtils.GetDifferenceAreasXY(sourceWithIsland, target, tolerance);

			Assert.AreEqual(2, differenceResult.PartCount);
			Assert.AreEqual(true, differenceResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(sourceWithIsland.GetArea2D(), differenceResult.GetArea2D(), 0.0001);

			// In contrast, the cut should not do anything for a cut line equal to the ring to cut:
			var cutResult = GeomTopoOpUtils.CutXY(sourceWithIsland, target, tolerance);
			Assert.AreEqual(0, cutResult.Count);
		}

		[Test]
		public void CanRemoveDisjointXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var disjoint = new List<Pnt3D>();
			disjoint.Add(new Pnt3D(140, -10, 0));
			disjoint.Add(new Pnt3D(140, 30, 0));
			disjoint.Add(new Pnt3D(300, 30, 0));
			disjoint.Add(new Pnt3D(300, -10, 0));

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring overlap = GeomTestUtils.CreateRing(disjoint);

			const double tolerance = 0.01;

			var target = new MultiPolycurve(new[] { overlap });
			MultiLinestring differenceResult =
				GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(1, differenceResult.PartCount);
			Assert.AreEqual(true, differenceResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(poly1.GetArea2D(), differenceResult.GetArea2D(), 0.0001);

			// Now the same with the target inside a source-island:
			poly1.AddInteriorRing(new Linestring(new[]
			                                     {
				                                     new Pnt3D(25, 50, 0),
				                                     new Pnt3D(50, 50, 0),
				                                     new Pnt3D(50, 75, 0),
				                                     new Pnt3D(25, 75, 0),
				                                     new Pnt3D(25, 50, 0)
			                                     }
			                      ));

			target.AddLinestring(new Linestring(new[]
			                                    {
				                                    new Pnt3D(30, 55, 0),
				                                    new Pnt3D(45, 55, 0),
				                                    new Pnt3D(45, 70, 0),
				                                    new Pnt3D(30, 70, 0),
				                                    new Pnt3D(30, 55, 0)
			                                    }
			                     ));

			differenceResult = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(2, differenceResult.PartCount);
			Assert.AreEqual(true, differenceResult.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(false, differenceResult.GetLinestring(1).ClockwiseOriented);

			Assert.AreEqual(poly1.GetArea2D(), differenceResult.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanRemoveIdenticalXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring equalRing = poly1.ExteriorRing.Clone();

			const double tolerance = 0.01;

			var target = new MultiPolycurve(new[] { equalRing });
			MultiLinestring differenceResult =
				GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);

			Assert.IsTrue(differenceResult.IsEmpty);
			Assert.AreEqual(0, differenceResult.PartCount);
		}

		[Test]
		public void CanDetectNonCuttingLine()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			// The 'cut line' intersects twice but does not cross the polygon.
			var notQuiteCutting = new List<Pnt3D>();

			notQuiteCutting.Add(new Pnt3D(50, 30, 0));
			notQuiteCutting.Add(new Pnt3D(200, 30, 0));
			notQuiteCutting.Add(new Pnt3D(200, -10, 0));
			notQuiteCutting.Add(new Pnt3D(40, -10, 0));
			notQuiteCutting.Add(new Pnt3D(40, 30, 0));

			RingGroup poly1 = new RingGroup(GeomTestUtils.CreateRing(ring1));
			Linestring target = new Linestring(notQuiteCutting);

			IList<RingGroup> result = CutPlanar(poly1, target, 0, 0);
			Assert.AreEqual(0, result.Count);

			// Now cutting through the interior ring but not the exterior => no cut

			var inner = new List<Pnt3D>
			            {
				            new Pnt3D(25, 50, 0),
				            new Pnt3D(50, 50, 0),
				            new Pnt3D(50, 75, 0),
				            new Pnt3D(25, 75, 0)
			            };

			var polyWithIsland = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                                   new[] { GeomTestUtils.CreateRing(inner) });

			var innerRingCutting = new List<Pnt3D>();

			innerRingCutting.Add(new Pnt3D(25, 50, 0));
			innerRingCutting.Add(new Pnt3D(50, 75, 0));
			target = new Linestring(innerRingCutting);

			CutPlanar(polyWithIsland, target, 0, 0);
		}

		[Test]
		public void CanCutOverlappingXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var overlapping = new List<Pnt3D>();
			overlapping.Add(new Pnt3D(40, -10, 0));
			overlapping.Add(new Pnt3D(40, 30, 0));
			overlapping.Add(new Pnt3D(200, 30, 0));
			overlapping.Add(new Pnt3D(200, -10, 0));

			WithRotatedRing(
				ring1,
				delegate(Linestring r1)
				{
					RingGroup poly1 = new RingGroup(r1);

					WithRotatedRing(
						overlapping,
						delegate(Linestring o)
						{
							IList<RingGroup> result = CutPlanarBothWays(poly1, o, 2, 0);

							var expected = GeomTestUtils.CreateRing(new List<Pnt3D>
								{
									new Pnt3D(0, 0, 9),
									new Pnt3D(0, 100, 9),
									new Pnt3D(100, 100, 9),
									new Pnt3D(100, 30, 9),
									new Pnt3D(40, 30, 9),
									new Pnt3D(40, 0, 9)
								});

							Assert.True(
								GeomTopoOpUtils.AreEqualXY(expected, result[0].ExteriorRing,
								                           0.01) ||
								GeomTopoOpUtils.AreEqualXY(expected, result[1].ExteriorRing,
								                           0.01));

							o.ReverseOrientation();
							result = CutPlanarBothWays(poly1, o, 2, 0);
							Assert.True(
								GeomTopoOpUtils.AreEqualXY(expected, result[0].ExteriorRing,
								                           0.01) ||
								GeomTopoOpUtils.AreEqualXY(expected, result[1].ExteriorRing,
								                           0.01));
						});
				});
		}

		[Test]
		public void CanCutOverlappingXYwithCorrectZ()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 100),
				            new Pnt3D(0, 100, 100),
				            new Pnt3D(100, 100, 100),
				            new Pnt3D(100, 0, 100)
			            };

			var overlapping = new List<Pnt3D>();
			overlapping.Add(new Pnt3D(40, -10, 40));
			overlapping.Add(new Pnt3D(40, 30, 40));
			overlapping.Add(new Pnt3D(200, 30, 40));
			overlapping.Add(new Pnt3D(200, -10, 40));

			RingGroup source = new RingGroup(GeomTestUtils.CreateRing(ring1));
			var targetLine = GeomTestUtils.CreateRing(overlapping);

			// The general consensus is that if possible, the target Zs should be used
			// at the intersection points.
			var expected = GeomTestUtils.CreateRing(new List<Pnt3D>
			                                        {
				                                        new Pnt3D(0, 0, 100),
				                                        new Pnt3D(0, 100, 100),
				                                        new Pnt3D(100, 100, 100),
				                                        new Pnt3D(100, 30, 40),
				                                        new Pnt3D(40, 30, 40),
				                                        new Pnt3D(40, 0, 40)
			                                        });

			var expectedRingGroup = new RingGroup(expected);

			IList<RingGroup> result = CutPlanarBothWays(source, targetLine, 2, 0);

			Assert.IsTrue(expectedRingGroup.Equals(result[0]));

			// The same with pre-existing vertices in the source at the intersection locations:
			ring1 = new List<Pnt3D>
			        {
				        new Pnt3D(0, 0, 100),
				        new Pnt3D(0, 100, 100),
				        new Pnt3D(100, 100, 100),
				        new Pnt3D(100, 30, 100),
				        new Pnt3D(100, 0, 100),
				        new Pnt3D(40, 0, 100)
			        };

			source = new RingGroup(GeomTestUtils.CreateRing(ring1));

			result = CutPlanarBothWays(source, targetLine, 2, 0);

			Assert.IsTrue(expectedRingGroup.Equals(result[0]));

			// But the source Z is used if the target line has no Zs:
			foreach (Pnt3D pnt in targetLine.GetPoints())
			{
				pnt.Z = double.NaN;
			}

			expected.GetPoint3D(3).Z = 100;
			expected.GetPoint3D(4).Z = double.NaN;
			expected.GetPoint3D(5).Z = 100;

			result = CutPlanarBothWays(source, targetLine, 2, 0);

			Assert.IsTrue(expectedRingGroup.Equals(result[0]));
		}

		[Test]
		public void CanCutWithInnerRing()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var innerRing2 = new List<Pnt3D>
			                 {
				                 new Pnt3D(25, 50, 0),
				                 new Pnt3D(50, 50, 0),
				                 new Pnt3D(50, 75, 0),
				                 new Pnt3D(25, 75, 0)
			                 };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(innerRing2) });

			var innerRing2Overlapping = new List<Pnt3D>
			                            {
				                            new Pnt3D(40, -10, 0),
				                            new Pnt3D(40, 30, 0),
				                            new Pnt3D(200, 30, 0),
				                            new Pnt3D(200, -10, 0)
			                            };

			Linestring target = GeomTestUtils.CreateRing(innerRing2Overlapping);

			IList<RingGroup> result = CutPlanarBothWays(poly, target, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			// Same with an 'open target'
			Linestring openTarget = new Linestring(innerRing2Overlapping);
			result = CutPlanarBothWays(poly, openTarget, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			// Now add another ring to the input and cut through the ring
			var inner2 = new List<Pnt3D>
			             {
				             new Pnt3D(50, 25, 0),
				             new Pnt3D(75, 25, 0),
				             new Pnt3D(75, 45, 0),
				             new Pnt3D(50, 45, 0)
			             };

			poly.AddInteriorRing(GeomTestUtils.CreateRing(inner2));
			result = CutPlanarBothWays(poly, target, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			result = CutPlanarBothWays(poly, openTarget, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			// Now with a multi-part target
			var outerRingCutting = new List<Pnt3D>()
			                       {
				                       new Pnt3D(80, 100, 0),
				                       new Pnt3D(80, 80, 0),
				                       new Pnt3D(100, 80, 0)
			                       };

			MultiLinestring multiTarget = new MultiPolycurve(new[]
			                                                 {
				                                                 openTarget,
				                                                 new Linestring(outerRingCutting)
			                                                 });

			CutXY(poly, multiTarget, 3, 1);

			// And with a cut line that cuts both the outer and the inner ring:
			var bothRingsCutting = new List<Pnt3D>()
			                       {
				                       new Pnt3D(80, 100, 0),
				                       new Pnt3D(80, 80, 0),
				                       new Pnt3D(100, 80, 0),
				                       new Pnt3D(120, 30, 0),
				                       new Pnt3D(40, 30, 0),
				                       new Pnt3D(40, 40, 0),
				                       new Pnt3D(60, 40, 0),
			                       };

			openTarget = new Linestring(bothRingsCutting);
			CutXY(poly, openTarget, 3, 2);
		}

		[Test]
		public void CanCutWithTwoInnerRings()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			var inner2 = new List<Pnt3D>
			             {
				             new Pnt3D(60, 50, 0),
				             new Pnt3D(80, 50, 0),
				             new Pnt3D(80, 75, 0),
				             new Pnt3D(60, 75, 0)
			             };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[]
			                         {
				                         GeomTestUtils.CreateRing(inner1),
				                         GeomTestUtils.CreateRing(inner2)
			                         });

			var overlapping = new List<Pnt3D>();
			overlapping.Add(new Pnt3D(40, -10, 0));
			overlapping.Add(new Pnt3D(40, 30, 0));
			overlapping.Add(new Pnt3D(200, 30, 0));
			overlapping.Add(new Pnt3D(200, -10, 0));

			Linestring target = GeomTestUtils.CreateRing(overlapping);

			IList<RingGroup> result = CutPlanarBothWays(poly, target, 2, 2);
			Assert.AreEqual(4, result.Sum(p => p.Count));

			// Now add another ring to the input and cut through the ring
			var inner3 = new List<Pnt3D>
			             {
				             new Pnt3D(50, 25, 0),
				             new Pnt3D(75, 25, 0),
				             new Pnt3D(75, 45, 0),
				             new Pnt3D(50, 45, 0)
			             };

			poly.AddInteriorRing(GeomTestUtils.CreateRing(inner3));
			result = CutPlanarBothWays(poly, target, 2, 2);
			Assert.AreEqual(4, result.Sum(p => p.Count));
		}

		[Test]
		public void CanCutCompletelyInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			var inner2 = new List<Pnt3D>
			             {
				             new Pnt3D(60, 50, 0),
				             new Pnt3D(80, 50, 0),
				             new Pnt3D(80, 75, 0),
				             new Pnt3D(60, 75, 0)
			             };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			Linestring target = GeomTestUtils.CreateRing(inner2);

			IList<RingGroup> result1 = CutPlanarBothWays(poly, target, 2, 2);
			Assert.AreEqual(4, result1.Sum(p => p.Count));

			// Now cut with cutline contained in outerring,  intersecting an existing inner ring
			var bridgeBetweenIslands = new List<Pnt3D>
			                           {
				                           new Pnt3D(40, 60, 0),
				                           new Pnt3D(70, 60, 0),
				                           new Pnt3D(70, 70, 0),
				                           new Pnt3D(40, 70, 0)
			                           };

			target = GeomTestUtils.CreateRing(bridgeBetweenIslands);

			IList<RingGroup> result2 = CutPlanarBothWays(poly, target, 2, 1);
			Assert.AreEqual(3, result2.Sum(p => p.Count));

			// The same using just a target line instead a ring (old method does not support this)
			target = new Linestring(bridgeBetweenIslands);
			IList<MultiLinestring> xyResult = CutXY(poly, target, 2, 1);
			Assert.AreEqual(3, xyResult.Sum(p => p.Count));

			// Now cut a new inner ring intersecting both existing inner rings
			target = GeomTestUtils.CreateRing(bridgeBetweenIslands);
			RingGroup polyWith2Islands = result1[0];
			IList<RingGroup> result3 = CutPlanarBothWays(polyWith2Islands, target, 2, 1);
			Assert.AreEqual(3, result3.Sum(p => p.Count));

			// ... and the same using just a target line instead a ring (old method does not support this)
			target = new Linestring(bridgeBetweenIslands);
			xyResult = CutXY(polyWith2Islands, target, 2, 1);
			Assert.AreEqual(3, xyResult.Sum(p => p.Count));
			Assert.AreEqual(polyWith2Islands.GetArea2D(),
			                xyResult.Sum(p => ((RingGroup) p).GetArea2D()));

			// Clip an area that contains an inner ring
			var containingInnerRing1 = new List<Pnt3D>
			                           {
				                           new Pnt3D(20, 45, 0),
				                           new Pnt3D(55, 45, 0),
				                           new Pnt3D(55, 80, 0),
				                           new Pnt3D(20, 80, 0)
			                           };

			target = GeomTestUtils.CreateRing(containingInnerRing1);
			xyResult = CutXY(polyWith2Islands, target, 2, 3);
			Assert.AreEqual(5, xyResult.Sum(p => p.Count));
			Assert.AreEqual(polyWith2Islands.GetArea2D(),
			                xyResult.Sum(p => ((RingGroup) p).GetArea2D()));

			// Try cutting with an existing inner ring
			target = GeomTestUtils.CreateRing(inner2);
			var unCutResult = CutXY(polyWith2Islands, target, 0, 0);
			Assert.AreEqual(0, unCutResult.Count);
		}

		[Test]
		public void CanCutWithCutlineStartingAlong()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var startingAlong = new List<Pnt3D>
			                    {
				                    new Pnt3D(60, 100, 0),
				                    new Pnt3D(80, 100, 0),
				                    new Pnt3D(80, 0, 0)
			                    };

			Linestring target = new Linestring(startingAlong);

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			CutPlanarBothWays(poly, target, 2, 0);
		}

		[Test]
		public void CannotCutWithCutlineAlmostAlongRing()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var startingAlong = new List<Pnt3D>
			                    {
				                    new Pnt3D(-0.001, 99.999, 0),
				                    //new Pnt3D(80, 100, 0),
				                    new Pnt3D(160, 100, 0)
			                    };

			Linestring target = new Linestring(startingAlong);

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			CutPlanarBothWays(poly, target, 0, 0);
		}

		[Test]
		public void CanCutWithCutlineStartEndTouchingIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			// Touching inner1 with start and end point
			var touchingIsland = new List<Pnt3D>
			                     {
				                     new Pnt3D(25, 50, 0),
				                     new Pnt3D(35, 30, 0),
				                     new Pnt3D(50, 50, 0),
			                     };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			Linestring target = new Linestring(touchingIsland);

			IList<MultiLinestring> result = CutXY(poly, target, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D(),
			                result.Sum(r => r.GetArea2D()));

			// Now touch the island AND the outer ring -> should create a boundary loop in outer ring
			// or an island that touches the outer ring (ogc style). ArcGIS is happy with both options
			// but 'corrects' geometries to boundary loops (only tested in the editor).
			var touchingIslandAndOuterRing = new List<Pnt3D>
			                                 {
				                                 new Pnt3D(25, 50, 0),
				                                 new Pnt3D(35, 0, 0),
				                                 new Pnt3D(50, 50, 0),
			                                 };

			target = new Linestring(touchingIslandAndOuterRing);

			result = CutXY(poly, target, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D(),
			                result.Sum(r => r.GetArea2D()));

			// Now the outer ring is touched in a line rather than just a point:
			touchingIslandAndOuterRing = new List<Pnt3D>
			                             {
				                             new Pnt3D(25, 50, 0),
				                             new Pnt3D(35, 0, 0),
				                             new Pnt3D(45, 0, 0),
				                             new Pnt3D(50, 50, 0),
			                             };

			target = new Linestring(touchingIslandAndOuterRing);

			result = CutXY(poly, target, 2, 0);
			Assert.AreEqual(2, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D(),
			                result.Sum(r => r.GetArea2D()));
		}

		[Test]
		public void CanCutWithCutlineStartEndTouchingBoundaryAndTouchingIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			// Touching ring1 with start and end point, touching inner1 in point
			var cutLine = new List<Pnt3D>
			              {
				              new Pnt3D(0, 0, 0),
				              new Pnt3D(35, 50, 0),
				              new Pnt3D(100, 0, 0),
			              };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			Linestring target = new Linestring(cutLine);

			IList<MultiLinestring> result = CutXY(poly, target, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D(),
			                result.Sum(r => r.GetArea2D()));

			// Now touch the island in a line rather than just a point:
			cutLine = new List<Pnt3D>
			          {
				          new Pnt3D(0, 0, 0),
				          new Pnt3D(35, 50, 0),
				          new Pnt3D(45, 50, 0),
				          new Pnt3D(100, 0, 0),
			          };

			target = new Linestring(cutLine);

			result = CutXY(poly, target, 2, 0);
			Assert.AreEqual(2, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D(),
			                result.Sum(r => r.GetArea2D()));

			// Now the cut line hugs the island:
			cutLine = new List<Pnt3D>
			          {
				          new Pnt3D(0, 0, 0),
				          new Pnt3D(25, 50, 0),
				          new Pnt3D(25, 75, 0),
				          new Pnt3D(50, 75, 0),
				          new Pnt3D(50, 0, 0),
			          };

			target = new Linestring(cutLine);

			result = CutXY(poly, target, 2, 0);
			Assert.AreEqual(2, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D(),
			                result.Sum(r => r.GetArea2D()));
		}

		[Test]
		public void CanCutWithCutlineEndingAlong()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var startingAlong = new List<Pnt3D>
			                    {
				                    new Pnt3D(80, 0, 0),
				                    new Pnt3D(60, 100, 0),
				                    new Pnt3D(80, 100, 0)
			                    };

			Linestring target = new Linestring(startingAlong);

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			CutPlanarBothWays(poly, target, 2, 0);
		}

		[Test]
		public void CanCutWithSeveralTargets()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var target1 = new List<Pnt3D>
			              {
				              new Pnt3D(80, 100, 0),
				              new Pnt3D(80, 80, 0),
				              new Pnt3D(100, 70, 0)
			              };

			var target2 = new List<Pnt3D>
			              {
				              new Pnt3D(30, 0, 0),
				              new Pnt3D(30, 20, 0),
				              new Pnt3D(0, 20, 0)
			              };

			MultiLinestring target = new MultiPolycurve(
				new[]
				{
					new Linestring(target1),
					new Linestring(target2)
				});

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			CutXY(poly, target, 3, 0);

			target2.Reverse();
			target = new MultiPolycurve(
				new[]
				{
					new Linestring(target1),
					new Linestring(target2)
				});

			CutXY(poly, target, 3, 0);
		}

		[Test]
		public void CanCutWithSeveralTargetsParallel()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var target1 = new List<Pnt3D>
			              {
				              new Pnt3D(80, 100, 0),
				              new Pnt3D(80, 80, 0),
				              new Pnt3D(100, 70, 0)
			              };

			var target2 = new List<Pnt3D>
			              {
				              new Pnt3D(50, 100, 0),
				              new Pnt3D(50, 50, 0),
				              new Pnt3D(100, 20, 0)
			              };

			MultiLinestring target = new MultiPolycurve(
				new[]
				{
					new Linestring(target1),
					new Linestring(target2)
				});

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			CutXY(poly, target, 3, 0);

			target2.Reverse();
			target = new MultiPolycurve(
				new[]
				{
					new Linestring(target1),
					new Linestring(target2)
				});

			CutXY(poly, target, 3, 0);
		}

		[Test]
		public void CanCutTouchingInLineFromInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			var touchingFromInside = new List<Pnt3D>
			                         {
				                         new Pnt3D(60, 50, 0),
				                         new Pnt3D(80, 50, 0),
				                         new Pnt3D(80, 100, 0),
				                         new Pnt3D(60, 100, 0)
			                         };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			WithRotatedRing(touchingFromInside,
			                target =>
			                {
				                target.TryOrientClockwise();

				                IList<RingGroup> result =
					                CutPlanarBothWays(poly, target, 2, 1);
				                Assert.AreEqual(3, result.Sum(p => p.Count));

				                Assert.AreEqual(poly.GetArea2D() - target.GetArea2D(),
				                                result.First(p => p.Count == 2).GetArea2D());
			                });

			// Now touch the island from inside:
		}

		[Test]
		public void CannotCutTouchingInLineFromOutside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			var touchingFromOutside = new List<Pnt3D>
			                          {
				                          new Pnt3D(100, 60, 0),
				                          new Pnt3D(100, 80, 0),
				                          new Pnt3D(200, 80, 0),
				                          new Pnt3D(200, 60, 0)
			                          };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			WithRotatedRing(touchingFromOutside,
			                target =>
			                {
				                target.TryOrientClockwise();

				                IList<RingGroup> result =
					                CutPlanarBothWays(poly, target, 0, 0);
				                Assert.AreEqual(0, result.Sum(p => p.Count));
			                });
		}

		[Test]
		public void CanCutTouchingInPointFromInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			var touchingFromInside = new List<Pnt3D>
			                         {
				                         new Pnt3D(60, 50, 0),
				                         new Pnt3D(80, 50, 0),
				                         new Pnt3D(80, 100, 0)
			                         };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			WithRotatedRing(touchingFromInside,
			                target =>
			                {
				                target.TryOrientClockwise();

				                IList<MultiLinestring> result = CutXY(poly, target, 2, 1);
				                Assert.AreEqual(3, result.Sum(p => p.Count));

				                Assert.AreEqual(poly.GetArea2D() - target.GetArea2D(),
				                                result.First(p => p.Count == 2).GetArea2D());
			                });

			// Touching the inner ring from the inside:
			var containingIslandWithTouch = new List<Pnt3D>
			                                {
				                                new Pnt3D(20, 40, 0),
				                                new Pnt3D(60, 40, 0),
				                                new Pnt3D(50, 75, 0),
				                                new Pnt3D(20, 80, 0)
			                                };

			WithRotatedRing(
				containingIslandWithTouch,
				target =>
				{
					target.TryOrientClockwise();

					// The original ring with the cut line as interior 
					// Plus cut line (as cookie) with the original interior ring incorporated as boundary loop
					IList<MultiLinestring> result = CutXY(poly, target, 2, 1);
					Assert.AreEqual(3, result.Sum(p => p.Count));

					Assert.AreEqual(poly.GetArea2D(),
					                result.Sum(p => p.GetArea2D()));
				});
		}

		[Test]
		public void CanCutWithLineTouchingInPointFromInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var touchingFromInside = new List<Pnt3D>
			                         {
				                         new Pnt3D(0, 0, 0),
				                         new Pnt3D(50, 100, 0),
				                         new Pnt3D(100, 0, 0)
			                         };

			var cutLine = new Linestring(touchingFromInside);

			WithRotatedRing(ring1,
			                source =>
			                {
				                var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

				                IList<MultiLinestring> result = CutXY(poly, cutLine, 3, 0);

				                Assert.AreEqual(poly.GetArea2D(), result.Sum(r => r.GetArea2D()));
			                });
		}

		[Test]
		public void CanCutTouchingIslandFromOutside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			// Touching the inner1 from the outside of the island (i.e. inside the polygon)
			// -> end result is two touching islands
			var touchingIsland = new List<Pnt3D>
			                     {
				                     new Pnt3D(30, 30, 0),
				                     new Pnt3D(50, 50, 0),
				                     new Pnt3D(40, 30, 0)
			                     };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			Linestring target = GeomTestUtils.CreateRing(touchingIsland);
			target.TryOrientClockwise();

			IList<MultiLinestring> result = CutXY(poly, target, 2, 2);
			Assert.AreEqual(4, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D() - target.GetArea2D(),
			                result.First(p => p.Count == 3).GetArea2D());

			// Now touch the island AND the outer ring -> should create a boundary loop in outer ring
			var touchingIslandAndOuterRing = new List<Pnt3D>
			                                 {
				                                 new Pnt3D(30, 30, 0),
				                                 new Pnt3D(50, 50, 0),
				                                 new Pnt3D(60, 0, 0)
			                                 };

			target = GeomTestUtils.CreateRing(touchingIslandAndOuterRing);
			target.TryOrientClockwise();

			result = CutXY(poly, target, 2, 1);
			Assert.AreEqual(3, result.Sum(p => p.Count));

			Assert.AreEqual(poly.GetArea2D() - target.GetArea2D(),
			                result.First(p => p.Count == 2).GetArea2D());
		}

		[Test]
		public void CannotCutTouchingIslandFromInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var inner1 = new List<Pnt3D>
			             {
				             new Pnt3D(25, 50, 0),
				             new Pnt3D(50, 50, 0),
				             new Pnt3D(50, 75, 0),
				             new Pnt3D(25, 75, 0)
			             };

			var touchingIsland = new List<Pnt3D>
			                     {
				                     new Pnt3D(40, 60, 0),
				                     new Pnt3D(50, 50, 0),
				                     new Pnt3D(30, 60, 0)
			                     };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(inner1) });

			WithRotatedRing(touchingIsland,
			                l =>
			                {
				                Linestring target = l;
				                target.TryOrientClockwise();

				                IList<MultiLinestring> result = CutXY(poly, target, 0, 0);
				                Assert.AreEqual(0, result.Count);
			                });
		}

		[Test]
		public void CanCutWithCutlineEndingWithNonCuttingDangles()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			var targetWithDangle = new List<Pnt3D>
			                       {
				                       new Pnt3D(80, 0, 0),
				                       new Pnt3D(60, 110, 0),
				                       new Pnt3D(40, 70, 0)
			                       };

			Linestring target1 = new Linestring(targetWithDangle);
			targetWithDangle.Reverse();
			Linestring flippedTarget1 = new Linestring(targetWithDangle);

			// Theoretically the already visited intersections would get removed:
			WithRotatedRing(ring1,
			                delegate
			                {
				                CutPlanar(poly, target1, 2, 0);
				                CutPlanar(poly, flippedTarget1, 2, 0);
			                });

			// This requires the dangle-filter when classifying the intersections as in-/out-bound:
			targetWithDangle = new List<Pnt3D>
			                   {
				                   new Pnt3D(40, 0, 0),
				                   new Pnt3D(60, 110, 0),
				                   new Pnt3D(80, 70, 0)
			                   };

			var target2 = new Linestring(targetWithDangle);
			targetWithDangle.Reverse();
			var flippedTarget2 = new Linestring(targetWithDangle);

			WithRotatedRing(ring1,
			                delegate
			                {
				                CutPlanar(poly, target2, 2, 0);
				                CutPlanar(poly, flippedTarget2, 2, 0);
			                });

			// Starting at the outside with one proper cut and one non-cutting dangle
			targetWithDangle = new List<Pnt3D>
			                   {
				                   new Pnt3D(80, 110, 0),
				                   new Pnt3D(60, 50, 0),
				                   new Pnt3D(40, 110, 0),
				                   new Pnt3D(20, 50, 0)
			                   };

			var target3 = new Linestring(targetWithDangle);
			targetWithDangle.Reverse();
			var flippedTarget3 = new Linestring(targetWithDangle);

			WithRotatedRing(ring1,
			                delegate
			                {
				                CutPlanar(poly, target3, 2, 0);
				                CutPlanar(poly, flippedTarget3, 2, 0);
			                });

			targetWithDangle = new List<Pnt3D>
			                   {
				                   new Pnt3D(40, 110, 0),
				                   new Pnt3D(60, 50, 0),
				                   new Pnt3D(80, 110, 0),
				                   new Pnt3D(85, 50, 0)
			                   };

			var target4 = new Linestring(targetWithDangle);
			targetWithDangle.Reverse();
			var flippedTarget4 = new Linestring(targetWithDangle);

			WithRotatedRing(ring1,
			                delegate
			                {
				                CutPlanar(poly, target4, 2, 0);
				                CutPlanar(poly, flippedTarget4, 2, 0);
			                });
		}

		[Test]
		public void CanCutWithCutlineEndingCuttingInNonSequentiallyAlongSource()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1));

			var targetWithCutBack = new List<Pnt3D>
			                        {
				                        new Pnt3D(80, 110, 0),
				                        new Pnt3D(60, 50, 0),
				                        new Pnt3D(60, 110, 0),
				                        new Pnt3D(40, 110, 0),
				                        new Pnt3D(40, 50, 0),
				                        new Pnt3D(50, 100, 0)
			                        };

			var target1 = new Linestring(targetWithCutBack);
			targetWithCutBack.Reverse();
			var flippedTarget1 = new Linestring(targetWithCutBack);

			WithRotatedRing(ring1,
			                delegate
			                {
				                CutPlanar(poly, target1, 3, 0);
				                CutPlanar(poly, flippedTarget1, 3, 0);
			                });

			targetWithCutBack = new List<Pnt3D>
			                    {
				                    new Pnt3D(20, 110, 0),
				                    new Pnt3D(40, 50, 0),
				                    new Pnt3D(50, 110, 0),
				                    new Pnt3D(80, 110, 0),
				                    new Pnt3D(60, 50, 0),
				                    new Pnt3D(60, 100, 0)
			                    };

			var target2 = new Linestring(targetWithCutBack);
			targetWithCutBack.Reverse();
			var flippedTarget2 = new Linestring(targetWithCutBack);

			WithRotatedRing(ring1,
			                delegate
			                {
				                CutPlanar(poly, target2, 3, 0);
				                CutPlanar(poly, flippedTarget2, 3, 0);
			                });
		}

		[Test]
		public void CanCutWithTargetEndsInsideIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var island = new List<Pnt3D>
			             {
				             new Pnt3D(40, 40, 0),
				             new Pnt3D(60, 40, 0),
				             new Pnt3D(60, 60, 0),
				             new Pnt3D(40, 60, 0)
			             };

			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1),
			                         new[] { GeomTestUtils.CreateRing(island) });

			var targetEnlargeIsland = new List<Pnt3D>
			                          {
				                          new Pnt3D(40, 40, 0),
				                          new Pnt3D(40, 30, 0),
				                          new Pnt3D(60, 30, 0),
				                          new Pnt3D(60, 40, 0)
			                          };

			Linestring target = new Linestring(targetEnlargeIsland);
			targetEnlargeIsland.Reverse();
			Linestring flippedTarget = new Linestring(targetEnlargeIsland);

			WithRotatedRing(ring1,
			                delegate
			                {
				                CutXY(poly, target, 2, 1);
				                CutXY(poly, flippedTarget, 2, 1);
			                });
		}

		[Test]
		public void CanDetermineCanCutConvex()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0),
				           new Pnt3D(0, 0, 0)
			           };

			var diag1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(100, 100, 0));
			var diag2 = new Line3D(new Pnt3D(0, 100, 0), new Pnt3D(100, 0, 0));
			var cross = new Line3D(new Pnt3D(0, 50, 0), new Pnt3D(100, 50, 0));
			var nonCutLine1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(0, 100, 0));
			var nonCutLine2 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(-100, 100, 0));

			var sourceRing = new Linestring(ring);
			var cutLineDiag1 = new Linestring(new[] { diag1.Clone() });
			var cutLineDiag2 = new Linestring(new[] { diag2.Clone() });
			var cutLineCross = new Linestring(new[] { cross.Clone() });
			var cutLineNonCut1 = new Linestring(new[] { nonCutLine1.Clone() });
			var cutLineNonCut2 = new Linestring(new[] { nonCutLine2.Clone() });

			// 3D - Can currently only cut at existing vertex locations using Line3D
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag1, 0.0001));
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag2, 0.0001));

			// 2D
			AssertCanCut(sourceRing, cutLineDiag1);
			cutLineDiag1.ReplacePoint(1, new Pnt3D(200, 100, 0));
			AssertCanCut(sourceRing, cutLineDiag1);

			AssertCanCut(sourceRing, cutLineDiag2);
			cutLineDiag2.ReplacePoint(1, new Pnt3D(200, -100, 0));
			AssertCanCut(sourceRing, cutLineDiag2);

			AssertCanCut(sourceRing, cutLineCross);
			cutLineDiag2.ReplacePoint(1, new Pnt3D(200, 50, 0));
			AssertCanCut(sourceRing, cutLineCross);

			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine1, 0.0001));
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine2, 0.0001));

			Assert.False(CanCutXY(sourceRing, cutLineNonCut1));
			Assert.False(CanCutXY(sourceRing, cutLineNonCut2));

			// XY-intersection of boundary:
			ring.Insert(1, new Pnt3D(0, 50, 2.7));
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine1, 0.0001));

			sourceRing = new Linestring(ring);
			Assert.False(CanCutXY(sourceRing, cutLineNonCut1));

			// Test also non-clockwise orientation:
			ring.Reverse();

			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag1, 0.0001));
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag2, 0.0001));

			// Cuts to the outside of the ring:
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine2, 0.0001));

			ring.Reverse();

			// almost-XY intersection (within tolerance) of boundary
			ring[1] = new Pnt3D(-0.006, 50, 2.7);
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine1, 0.01));
			Assert.False(CanCutXY(sourceRing, cutLineNonCut1));

			// with the small difference at a proper corner:
			ring.RemoveAt(1);
			ring[1] = new Pnt3D(-0.006, 50, 2.7);
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine1, 0.01));
		}

		private static void AssertCanCut(Linestring ring, Linestring cutLine)
		{
			Assert.True(CanCutXY(ring, cutLine));

			cutLine.ReverseOrientation();
			Assert.True(CanCutXY(ring, cutLine));

			//ring.ReverseOrientation();
			//Assert.True(CanCutXY(ring, cutLine));

			cutLine.ReverseOrientation();
			Assert.True(CanCutXY(ring, cutLine));

			//ring.ReverseOrientation();
		}

		[Test]
		public void CanDetermineCanCutConcave()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(70, 40, 0),
				           new Pnt3D(80, 50, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(50, 100, 0),
				           new Pnt3D(100, 100, 12),
				           new Pnt3D(100, 0, 12),
				           new Pnt3D(50, 0, 0),
				           new Pnt3D(0, 0, 0)
			           };

			var diag1 = new Line3D(new Pnt3D(80, 50, 0), new Pnt3D(100, 100, 12));
			var diag2 = new Line3D(new Pnt3D(80, 50, 0), new Pnt3D(100, 0, 12));

			var nonCutConcave1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(0, 100, 0));
			var nonCutConcave2 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(80, 50, 0));
			var nonCutLine3 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(-100, 100, 0));

			// Check also CutXY:
			var sourceRing = new Linestring(ring);
			var cutLineDiag1 = new Linestring(new[] { diag1 });
			var cutLineDiag2 = new Linestring(new[] { diag2 });
			var cutLineNonCut1 = new Linestring(new[] { nonCutConcave1 });
			var cutLineNonCut2 = new Linestring(new[] { nonCutConcave2 });
			var cutLineNonCut3 = new Linestring(new[] { nonCutLine3 });

			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag1, 0.0001));
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag2, 0.0001));

			AssertCanCut(sourceRing, cutLineDiag1);
			AssertCanCut(sourceRing, cutLineDiag2);

			// Test cut line through the outside to another vertex (concave part)
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutConcave1, 0.0001));
			Assert.False(CanCutXY(sourceRing, cutLineNonCut1));

			// Test cut line through the outside to another vertex (concave part), starting in the concave vertex
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutConcave2, 0.0001));
			Assert.False(CanCutXY(sourceRing, cutLineNonCut2));

			// Test cut line through the outside but to no other vertex
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine3, 0.0001));
			Assert.False(CanCutXY(sourceRing, cutLineNonCut3));

			// Test also non-clockwise orientation:
			ring.Reverse();

			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag1, 0.0001));
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag2, 0.0001));

			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutConcave1, 0.0001));
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutConcave2, 0.0001));
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine3, 0.0001));

			// TODO: interior ring cutting in XY, if needed
			//sourceRing.ReverseOrientation();
			//Assert.True(CanCutXY(sourceRing, cutLineDiag1));
			//Assert.True(CanCutXY(sourceRing, cutLineDiag2));
		}

		private static bool CanCutXY(Linestring sourceRing, Linestring cutLine)
		{
			IList<Linestring> left;
			IList<Linestring> right;
			bool canCutXy = GeomTopoOpUtils.CutRingXY(sourceRing, cutLine, 0.001,
			                                          out left, out right);

			if (canCutXy)
			{
				Assert.AreEqual(sourceRing.GetArea2D(),
				                left.Sum(r => r.GetArea2D()) +
				                right.Sum(r => r.GetArea2D()), 0.000001);

				Assert.AreEqual(1, left.Count);
				Assert.AreEqual(1, right.Count);
			}

			return canCutXy;
		}

		private static bool CanCutPlanar(Linestring sourceRing, Linestring cutLine)
		{
			RingGroup source = new RingGroup(sourceRing);

			MultiLinestring target = new MultiPolycurve(new[] { cutLine });

			IList<RingGroup> cutRings =
				GeomTopoOpUtils.CutPlanar(source, target, 0.001);

			if (cutRings.Count < 2)
			{
				return false;
			}
			else
			{
				Assert.AreEqual(sourceRing.GetArea2D(),
				                cutRings.Sum(r => r.GetArea2D()), 0.000001);
			}

			return true;
		}

		[Test]
		public void CanDetermineCanCutVertical()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(30, 100, 0),
				           new Pnt3D(30, 100, 55),
				           new Pnt3D(0, 0, 55),
				           new Pnt3D(0, 0, 0)
			           };

			var diag1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(30, 100, 55));
			var diag2 = new Line3D(new Pnt3D(30, 100, 0), new Pnt3D(0, 0, 55));

			var nonCutLine1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(0, 0, 55));
			var nonCutLine2 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(-100, 100, 0));

			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag1, 0.0001, true));
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag2, 0.0001, true));

			// In XY, we can only cut if the cut line crosses:
			var sourceRing = new Linestring(ring);
			var cutLineAcross =
				new Linestring(new List<Line3D>
				               {
					               new Line3D(new Pnt3D(15, 0, 0), new Pnt3D(15, 100, 0))
				               });

			Assert.False(CanCutXY(sourceRing, cutLineAcross));
			Assert.True(CanCutPlanar(sourceRing, cutLineAcross));

			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine1, 0.0001, true));
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine2, 0.0001, true));

			// Test also non-clockwise orientation:
			ring.Reverse();

			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag1, 0.0001, true));
			Assert.True(GeomTopoOpUtils.CanCutRing3D(ring, diag2, 0.0001, true));

			// Cuts to the outside of the ring:
			Assert.False(GeomTopoOpUtils.CanCutRing3D(ring, nonCutLine2, 0.0001, true));

			var cutLineVertical = new Linestring(new List<Line3D>
			                                     {
				                                     new Line3D(new Pnt3D(15, 50, 0),
				                                                new Pnt3D(15, 50, 55))
			                                     });

			// TODO: Identify the cut line as being vertical (i.e. the intersection points within tolerance in XY)
			//       -> rotate

			Assert.True(CanCutPlanar(sourceRing, cutLineVertical));
		}

		[Test]
		public void CanGetIntersectionPointsXYTouchInSinglePoint()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 50, 18));
			ring2.Add(new Pnt3D(200, 150, 18));
			ring2.Add(new Pnt3D(200, 10, 18));
			ring2.Add(new Pnt3D(100, 50, 18));

			IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
				ring1, ring2, 0.0001);

			Assert.AreEqual(1, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(100, 50, 9)));
			AssertRelationsXY(ring1, ring2, true);

			// The same from the inside:
			ring1.Reverse();
			AssertRelationsXY(ring1, ring2, false, true);
		}

		[Test]
		public void CanGetIntersectionPointsXYWithoutLinearIntersections()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, intersecting
			ring2.Add(new Pnt3D(90, 10, 18));
			ring2.Add(new Pnt3D(90, 110, 18));
			ring2.Add(new Pnt3D(200, 150, 18));
			ring2.Add(new Pnt3D(200, 10, 18));
			ring2.Add(new Pnt3D(90, 10, 18));

			IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
				ring1, ring2,
				0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(90, 100, 9)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 10, 9)));
			AssertRelationsXY(ring1, ring2, false);

			// swap source and target -> z from source
			intersectionPointsXY =
				GetIntersectionPointsXY(ring2, ring1, 0.0001);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(90, 100, 18)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 10, 18)));
			AssertRelationsXY(ring1, ring2, false);

			ring2.Reverse();
			intersectionPointsXY =
				GetIntersectionPointsXY(ring1, ring2, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(90, 100, 9)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 10, 9)));
			AssertRelationsXY(ring1, ring2, false);

			// insert vertex along intersecting segments of ring2, exactly at intersection
			ring2.Reverse();
			ring2.Insert(1, new Pnt3D(90, 100, 23));
			ring2.Insert(5, new Pnt3D(100, 10, 26));

			intersectionPointsXY =
				GetIntersectionPointsXY(ring1, ring2, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(90, 100, 9)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 10, 9)));
			AssertRelationsXY(ring1, ring2, false);

			// swap source and target:
			intersectionPointsXY =
				GetIntersectionPointsXY(ring2, ring1, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(90, 100, 23)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 10, 26)));
			AssertRelationsXY(ring2, ring1, false);

			// insert vertex along intersecting segments of ring1, exactly at intersection
			ring1.Insert(2, new Pnt3D(90, 100, 13));
			ring1.Insert(4, new Pnt3D(100, 10, 13));
			intersectionPointsXY =
				GetIntersectionPointsXY(ring2, ring1, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(90, 100, 23)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 10, 26)));
		}

		[Test]
		public void CanGetIntersectionPointsXYWithLinearIntersectionEndpoints()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			//ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 0, 9));
			ring2.Add(new Pnt3D(100, 100, 9));
			ring2.Add(new Pnt3D(200, 150, 9));
			ring2.Add(new Pnt3D(200, 0, 9));
			ring2.Add(new Pnt3D(100, 0, 9));

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				EnsureIntersectionPointsWithLinearIntersectionEndpoints(
					rotatedRing, ring2);
			}

			// insert vertex along intersecting segment
			ring2.Insert(1, new Pnt3D(100, 40, 6));

			IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
				ring1, ring2,
				0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY[0].Equals(new Pnt3D(100, 100, 9)));
			Assert.True(intersectionPointsXY[1].Equals(new Pnt3D(100, 0, 9)));

			intersectionPointsXY = GetIntersectionPointsXY(ring1, ring2,
			                                               0.0001, true, true);
			Assert.AreEqual(3, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 100, 9)));
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 40, 9)));
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 0, 9)));

			// same point exists in both paths:
			ring1.Insert(3, new Pnt3D(100, 40, 6));
			intersectionPointsXY = GetIntersectionPointsXY(ring1, ring2,
			                                               0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 100, 9)));
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 0, 9)));

			intersectionPointsXY = GetIntersectionPointsXY(ring1, ring2,
			                                               0.0001, true, true);
			Assert.AreEqual(3, intersectionPointsXY.Count);
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 100, 9)));
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 40, 6)));
			Assert.True(intersectionPointsXY.Contains(new Pnt3D(100, 0, 9)));
		}

		private static void EnsureIntersectionPointsWithLinearIntersectionEndpoints(
			List<Pnt3D> ring1, List<Pnt3D> ring2)
		{
			IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
				ring1, ring2, 0.0001);

			var intersectionPointsXYOtherSource =
				GetIntersectionPointsXY(ring2, ring1, 0.0001);

			Assert.AreEqual(intersectionPointsXY.Count, intersectionPointsXYOtherSource.Count);

			// The other way round they have different Z values
			//intersectionPointsXYOtherSource.SetEquals(intersectionPointsXY);

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(100, 0, 9),
				                  new Pnt3D(100, 100, 9)
			                  };

			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(goodResults.Contains(intersectionPointsXY[0]));
			Assert.True(goodResults.Contains(intersectionPointsXY[1]));

			ring2.Reverse();
			intersectionPointsXY = GetIntersectionPointsXY(ring1, ring2, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(goodResults.Contains(intersectionPointsXY[0]));
			Assert.True(goodResults.Contains(intersectionPointsXY[1]));

			ring1.Reverse();
			intersectionPointsXY = GetIntersectionPointsXY(ring1, ring2, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(goodResults.Contains(intersectionPointsXY[0]));
			Assert.True(goodResults.Contains(intersectionPointsXY[1]));

			ring2.Reverse();
			intersectionPointsXY = GetIntersectionPointsXY(ring1, ring2, 0.0001);
			Assert.AreEqual(2, intersectionPointsXY.Count);
			Assert.True(goodResults.Contains(intersectionPointsXY[0]));
			Assert.True(goodResults.Contains(intersectionPointsXY[1]));

			ring1.Reverse();
		}

		[Test]
		public void CanGetIntersectionPointsXYWithLinearIntersectionEndpointsButFilteredRingStart()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(110, 50, 9));
			ring1.Add(new Pnt3D(100, 0, 9));

			// ring 2: also horizontal, adjacent, ring start inside the linear intersection
			ring2.Add(new Pnt3D(110, 50, 9));
			ring2.Add(new Pnt3D(100, 100, 9));
			ring2.Add(new Pnt3D(200, 150, 9));
			ring2.Add(new Pnt3D(200, 0, 9));
			ring2.Add(new Pnt3D(100, 0, 9));
			ring2.Add(new Pnt3D(110, 50, 9));

			for (var i = 0; i < 4; i++)
			{
				List<Pnt3D> rotatedRing = GeomTestUtils.GetRotatedRing(ring1, i);

				const bool includedRingStarts = false;
				IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
					rotatedRing, ring2, 0.0001, includedRingStarts);

				var intersectionPointsXYOtherSource =
					GetIntersectionPointsXY(ring2, rotatedRing, 0.0001, includedRingStarts);

				Assert.AreEqual(intersectionPointsXY.Count, intersectionPointsXYOtherSource.Count);

				// The other way round they have different Z values
				//intersectionPointsXYOtherSource.SetEquals(intersectionPointsXY);

				var goodResults = new List<Pnt3D>
				                  {
					                  new Pnt3D(100, 0, 9),
					                  new Pnt3D(100, 100, 9)
				                  };

				Assert.AreEqual(2, intersectionPointsXY.Count);
				Assert.True(goodResults.Contains(intersectionPointsXY[0]));
				Assert.True(goodResults.Contains(intersectionPointsXY[1]));

				ring2.Reverse();
				intersectionPointsXY = GetIntersectionPointsXY(
					rotatedRing, ring2, 0.0001, includedRingStarts);
				Assert.AreEqual(2, intersectionPointsXY.Count);
				Assert.True(goodResults.Contains(intersectionPointsXY[0]));
				Assert.True(goodResults.Contains(intersectionPointsXY[1]));

				rotatedRing.Reverse();
				intersectionPointsXY = GetIntersectionPointsXY(
					rotatedRing, ring2, 0.0001, includedRingStarts);
				Assert.AreEqual(2, intersectionPointsXY.Count);
				Assert.True(goodResults.Contains(intersectionPointsXY[0]));
				Assert.True(goodResults.Contains(intersectionPointsXY[1]));

				ring2.Reverse();
				intersectionPointsXY = GetIntersectionPointsXY(
					rotatedRing, ring2, 0.0001, includedRingStarts);
				Assert.AreEqual(2, intersectionPointsXY.Count);
				Assert.True(goodResults.Contains(intersectionPointsXY[0]));
				Assert.True(goodResults.Contains(intersectionPointsXY[1]));

				rotatedRing.Reverse();
			}
		}

		[Test]
		public void CanGetIntersectionPointsMultipleIntersectinsPerSegmentAndFilteredRingStart()
		{
			// Extra crossings on the same segment that has linear intersections
			// Challenge: in order to filter the ring's start/end point intersections, the 
			// of intersections have to be properly ordered along the source
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));

			// ring 2: also horizontal, contained, crossing multiple times along long bottom segment
			ring2.Add(new Pnt3D(20, 0, 9));
			ring2.Add(new Pnt3D(0, 0, 9));
			ring2.Add(new Pnt3D(0, 100, 9));
			ring2.Add(new Pnt3D(60, 60, 9));
			ring2.Add(new Pnt3D(60, -5, 9));
			ring2.Add(new Pnt3D(30, 2, 9));
			ring2.Add(new Pnt3D(20, 0, 9));

			for (var i = 0; i < 4; i++)
			{
				List<Pnt3D> rotatedRing = GeomTestUtils.GetRotatedRing(ring1, i);

				IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
					rotatedRing, ring2, 0.0001, false);

				Assert.AreEqual(4, intersectionPointsXY.Count);

				var intersectionPointsXYOtherSource =
					GetIntersectionPointsXY(ring2, rotatedRing, 0.0001, false);

				Assert.AreEqual(intersectionPointsXY.Count, intersectionPointsXYOtherSource.Count);

				ring2.Reverse();
				intersectionPointsXY = GetIntersectionPointsXY(rotatedRing, ring2, 0.0001, false);
				Assert.AreEqual(4, intersectionPointsXY.Count);

				rotatedRing.Reverse();
				intersectionPointsXY = GetIntersectionPointsXY(rotatedRing, ring2, 0.0001, false);
				Assert.AreEqual(4, intersectionPointsXY.Count);

				ring2.Reverse();
				intersectionPointsXY = GetIntersectionPointsXY(rotatedRing, ring2, 0.0001, false);
				Assert.AreEqual(4, intersectionPointsXY.Count);

				rotatedRing.Reverse();
			}
		}

		[Test]
		public void CanGetIntersectionLinesMultipleIntersectinsPerSegmentAndFilteredRingStart()
		{
			// Extra crossings on the same segment that has linear intersections
			// Challenge: Find the correct differences also for segments that have linear intersections
			// that do not start at the from-point.
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));

			// ring 2: also horizontal, contained, crossing multiple times along long bottom segment
			ring2.Add(new Pnt3D(20, 0, 9));
			ring2.Add(new Pnt3D(0, 0, 9));
			ring2.Add(new Pnt3D(0, 100, 9));
			ring2.Add(new Pnt3D(60, 60, 9));
			ring2.Add(new Pnt3D(60, -5, 9));
			ring2.Add(new Pnt3D(30, 2, 9));
			ring2.Add(new Pnt3D(20, 0, 9));

			for (var i = 0; i < 4; i++)
			{
				List<Pnt3D> rotatedRing = GeomTestUtils.GetRotatedRing(ring1, i);

				var sourceLinestring = new Linestring(rotatedRing);
				double totalLength = sourceLinestring.GetLength2D();

				MultiLinestring multiLinestring =
					new MultiPolycurve(new List<Linestring> { new Linestring(ring2) });

				IList<Linestring> differenceLinesXY = GeomTopoOpUtils.GetDifferenceLinesXY(
					sourceLinestring, multiLinestring, 0.01);
				double differenceLength = differenceLinesXY.Sum(l => l.GetLength2D());

				IList<Linestring> intersectionLinesXY = GeomTopoOpUtils.GetIntersectionLinesXY(
					sourceLinestring, multiLinestring, 0.01);
				double intersectionLength = intersectionLinesXY.Sum(l => l.GetLength2D());

				Assert.AreEqual(totalLength, differenceLength + intersectionLength);

				ring2.Reverse();

				differenceLinesXY = GeomTopoOpUtils.GetDifferenceLinesXY(
					sourceLinestring, multiLinestring, 0.01);
				intersectionLinesXY = GeomTopoOpUtils.GetIntersectionLinesXY(
					sourceLinestring, multiLinestring, 0.01);

				Assert.AreEqual(differenceLength, differenceLinesXY.Sum(l => l.GetLength2D()));
				Assert.AreEqual(intersectionLength, intersectionLinesXY.Sum(l => l.GetLength2D()));

				rotatedRing.Reverse();
				differenceLinesXY = GeomTopoOpUtils.GetDifferenceLinesXY(
					sourceLinestring, multiLinestring, 0.01);
				intersectionLinesXY = GeomTopoOpUtils.GetIntersectionLinesXY(
					sourceLinestring, multiLinestring, 0.01);

				Assert.AreEqual(differenceLength, differenceLinesXY.Sum(l => l.GetLength2D()));
				Assert.AreEqual(intersectionLength, intersectionLinesXY.Sum(l => l.GetLength2D()));

				ring2.Reverse();
				differenceLinesXY = GeomTopoOpUtils.GetDifferenceLinesXY(
					sourceLinestring, multiLinestring, 0.01);
				intersectionLinesXY = GeomTopoOpUtils.GetIntersectionLinesXY(
					sourceLinestring, multiLinestring, 0.01);

				Assert.AreEqual(differenceLength, differenceLinesXY.Sum(l => l.GetLength2D()));
				Assert.AreEqual(intersectionLength, intersectionLinesXY.Sum(l => l.GetLength2D()));

				rotatedRing.Reverse();
			}
		}

		[Test]
		public void CanGetIntersectionPointsXYTouchingLine()
		{
			var path1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// Path1: horizontal:
			path1.Add(new Pnt3D(100, 0, 9));
			path1.Add(new Pnt3D(0, 0, 9));
			path1.Add(new Pnt3D(0, 100, 9));
			path1.Add(new Pnt3D(100, 50, 9));

			// ring 2: also horizontal, adjacent in XY
			ring2.Add(new Pnt3D(100, 0, 5));
			ring2.Add(new Pnt3D(100, 100, 5));
			ring2.Add(new Pnt3D(200, 150, 5));
			ring2.Add(new Pnt3D(200, 0, 5));
			//ring2.Add(new Pnt3D(100, 0, 5));

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(100, 0, 9),
				                  new Pnt3D(100, 50, 9)
			                  };

			var goodResultsZ5 = new List<Pnt3D>
			                    {
				                    new Pnt3D(100, 0, 5),
				                    new Pnt3D(100, 50, 5)
			                    };

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(path1, rotatedRing, goodResults);
				CheckIntersectionPoints(rotatedRing, path1, goodResultsZ5);

				rotatedRing.Reverse();
				CheckIntersectionPoints(path1, rotatedRing, goodResults);
				CheckIntersectionPoints(rotatedRing, path1, goodResultsZ5);
			}

			// Now touch at segment interior for both touch points
			path1[0].Y = 5;
			goodResults[0].Y = 5;
			goodResultsZ5[0].Y = 5;
			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(path1, rotatedRing, goodResults);
				CheckIntersectionPoints(rotatedRing, path1, goodResultsZ5);

				rotatedRing.Reverse();
				CheckIntersectionPoints(path1, rotatedRing, goodResults);
				CheckIntersectionPoints(rotatedRing, path1, goodResultsZ5);
			}
		}

		[Test]
		public void CanGetIntersectionPointsXYWithShortSegments()
		{
			var path1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// Path1: horizontal:
			path1.Add(new Pnt3D(0, 0, 9));
			path1.Add(new Pnt3D(0, 100, 9));
			path1.Add(new Pnt3D(100, 50, 9));
			path1.Add(new Pnt3D(100, 0, 9));
			path1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, equal except at one point there is a short segment
			ring2.Add(new Pnt3D(0, 0, 9));
			ring2.Add(new Pnt3D(0, 100, 9));
			ring2.Add(new Pnt3D(100, 50.001, 9));
			ring2.Add(new Pnt3D(100, 49.999, 9));
			ring2.Add(new Pnt3D(100, 0, 9));

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				Linestring linestring1 = new Linestring(path1);
				Linestring linestring2 = new Linestring(rotatedRing);

				IList<IntersectionPoint3D> intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring1, (ISegmentList) linestring2,
						0.01,
						false);

				Assert.AreEqual(0, intersectionPoints.Count);

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring2, (ISegmentList) linestring1,
						0.01,
						false);

				Assert.AreEqual(0, intersectionPoints.Count);

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring1, (ISegmentList) linestring2,
						0.01,
						true);

				Assert.IsTrue(intersectionPoints.Count > 0);

				// Inverted order has some extra challenges when sorting the intersections, if the
				// target is zero-length (requires comparer)
				linestring2.ReverseOrientation();

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring1, (ISegmentList) linestring2,
						0.01,
						false);

				Assert.AreEqual(0, intersectionPoints.Count);

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring1, (ISegmentList) linestring2,
						0.01,
						true);

				Assert.IsTrue(intersectionPoints.Count > 0);
			}
		}

		[Test]
		public void CanGetIntersectionPointsXYWithShortSegmentsAndCorrectRelativeOrientation()
		{
			var path1 = new List<Pnt3D>();
			var path2 = new List<Pnt3D>();

			// ring 2: also horizontal, equal except at one point there is a short segment
			path1.Add(new Pnt3D(0, 0, 9));
			path1.Add(new Pnt3D(0, 100, 9));
			path1.Add(new Pnt3D(100, 50.00, 9));
			path1.Add(new Pnt3D(100, 50.00, 9));
			path1.Add(new Pnt3D(100, 0, 9));

			// Path1: horizontal, adjacent, intersecting the short segment:
			path2.Add(new Pnt3D(100, 0, 9));
			path2.Add(new Pnt3D(100, 100, 9));
			path2.Add(new Pnt3D(200, 50, 9));
			path2.Add(new Pnt3D(200, 0, 9));

			for (var i = 0; i < 4; i++)
			{
				Linestring linestring1 = new Linestring(GeomTestUtils.GetRotatedRing(path1, i));
				Linestring linestring2 = GeomTestUtils.CreateRing(path2);

				IList<IntersectionPoint3D> intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring1, (ISegmentList) linestring2,
						0.01,
						false);

				Assert.AreEqual(2, intersectionPoints.Count);

				Assert.AreEqual(
					intersectionPoints[0].LinearIntersectionInOppositeDirection,
					true);

				if (i != 3)
				{
					// It cannot always be corrected...
					Assert.AreEqual(
						intersectionPoints[1].LinearIntersectionInOppositeDirection,
						true);
				}

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring2, (ISegmentList) linestring1,
						0.01,
						false);

				Assert.AreEqual(2, intersectionPoints.Count);

				Assert.AreEqual(
					intersectionPoints[0].LinearIntersectionInOppositeDirection,
					intersectionPoints[1].LinearIntersectionInOppositeDirection);

				linestring2.ReverseOrientation();

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring1, (ISegmentList) linestring2,
						0.01,
						false);

				Assert.AreEqual(2, intersectionPoints.Count);

				Assert.AreEqual(
					intersectionPoints[0].LinearIntersectionInOppositeDirection,
					intersectionPoints[1].LinearIntersectionInOppositeDirection);

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) linestring2, (ISegmentList) linestring1,
						0.01,
						false);

				Assert.AreEqual(2, intersectionPoints.Count);

				Assert.AreEqual(
					intersectionPoints[0].LinearIntersectionInOppositeDirection,
					intersectionPoints[1].LinearIntersectionInOppositeDirection);
			}
		}

		[Test]
		public void CanGetIntersectionPointsXYWithLinearIntersectionEndpointAtOtherInterior()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 50, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			//ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 0, 9));
			ring2.Add(new Pnt3D(100, 100, 9));
			ring2.Add(new Pnt3D(200, 150, 9));
			ring2.Add(new Pnt3D(200, 0, 9));
			ring2.Add(new Pnt3D(100, 0, 9));

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(100, 0, 9),
				                  new Pnt3D(100, 50, 9)
			                  };

			// TODO: Check point order and Z-values always from source

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
				CheckIntersectionPoints(ring2, rotatedRing, goodResults);

				AssertRelationsXY(rotatedRing, ring2, true);
				AssertRelationsXY(ring2, rotatedRing, true);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
				CheckIntersectionPoints(ring2, rotatedRing, goodResults);
			}

			// The same with non-closed path:
			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);

			// additional intermediate vertex:
			var pointAlongTargetSegment = new Pnt3D(100, 25, 0);
			ring1.Insert(3, pointAlongTargetSegment);

			var goodResultsForRingStartingAtInsertedPoint = new List<Pnt3D>(goodResults);
			goodResultsForRingStartingAtInsertedPoint.Add(pointAlongTargetSegment);
			goodResultsForRingStartingAtInsertedPoint.Add(pointAlongTargetSegment);

			var goodResultsForAllIntersections = new List<Pnt3D>(goodResults);
			goodResultsForAllIntersections.Add(pointAlongTargetSegment);

			CheckIntersectionPointsForTouchingRings(
				ring1, ring2, pointAlongTargetSegment, goodResults,
				goodResultsForRingStartingAtInsertedPoint,
				goodResultsForAllIntersections);

			// The same with non-closed path:
			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);
		}

		[Test]
		public void CanGetIntersectionPointsXYWithVerticalLinearIntersection()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 50, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			//ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: vertical, adjacent, touching in vertical segment
			ring2.Add(new Pnt3D(200, 100, 9));
			ring2.Add(new Pnt3D(50, 75, 9));
			ring2.Add(new Pnt3D(50, 75, 10));
			ring2.Add(new Pnt3D(200, 100, 9));

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(50, 75, 9),
				                  new Pnt3D(50, 75, 10)
			                  };

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, ring2, goodResults);

				AssertRelationsXY(rotatedRing, ring2, true);

				// Vertical source ring: unsupported
				//AssertRelationsXY(ring2, rotatedRing, true);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
				CheckIntersectionPoints(ring2, rotatedRing, goodResults);
			}

			// Now ring2 touches from within the ring1:
			ring2[0] = new Pnt3D(50, 50, 5);
			ring2[3] = new Pnt3D(50, 50, 5);

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, ring2, goodResults);

				AssertRelationsXY(rotatedRing, ring2, false);
				AssertRelationsXY(ring2, rotatedRing, false);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
				CheckIntersectionPoints(ring2, rotatedRing, goodResults);
			}

			// Now ring2 touches from within the ring1 but in a ring1 vertex:
			ring2[1] = new Pnt3D(0, 100, 5);
			ring2[2] = new Pnt3D(0, 100, 2);

			// The good result is actually 2 equal points for (ring1, ring2) and
			// 2 points with the ring2's z values for (ring2, ring)!
			goodResults = new List<Pnt3D>
			              {
				              new Pnt3D(0, 100, 9),
				              new Pnt3D(0, 100, 2)
			              };

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, ring2, goodResults);

				AssertRelationsXY(rotatedRing, ring2, false);
				AssertRelationsXY(ring2, rotatedRing, false);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
			}

			//// The same with non-closed path:
			//CheckIntersectionPoints(ring1, ring2, goodResults);
			//CheckIntersectionPoints(ring2, ring1, goodResults);

			//// additional intermediate vertex:
			//var pointAlongTargetSegment = new Pnt3D(100, 25, 0);
			//ring1.Insert(3, pointAlongTargetSegment);

			//var goodResultsForRingStartingAtInsertedPoint = new List<Pnt3D>(goodResults);
			//goodResultsForRingStartingAtInsertedPoint.Add(pointAlongTargetSegment);
			//goodResultsForRingStartingAtInsertedPoint.Add(pointAlongTargetSegment);

			//var goodResultsForAllIntersections = new List<Pnt3D>(goodResults);
			//goodResultsForAllIntersections.Add(pointAlongTargetSegment);

			//CheckIntersectionPointsForTouchingRings(
			//	ring1, ring2, pointAlongTargetSegment, goodResults,
			//	goodResultsForRingStartingAtInsertedPoint,
			//	goodResultsForAllIntersections);

			//// The same with non-closed path:
			//CheckIntersectionPoints(ring1, ring2, goodResults);
			//CheckIntersectionPoints(ring2, ring1, goodResults);
		}

		private static void CheckIntersectionPointsForTouchingRings(
			List<Pnt3D> ring1, List<Pnt3D> ring2, Pnt3D pointAlongTargetSegment,
			List<Pnt3D> goodResults,
			List<Pnt3D> goodResultsForRingStartingAtInsertedPoint,
			List<Pnt3D> goodResultsForAllIntersections)
		{
			for (var i = 0; i < ring1.Count; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, ring2,
				                        rotatedRing[0].Equals(pointAlongTargetSegment)
					                        ? goodResultsForRingStartingAtInsertedPoint
					                        : goodResults);

				CheckIntersectionPoints(rotatedRing, ring2,
				                        rotatedRing[0].Equals(pointAlongTargetSegment)
					                        ? goodResultsForRingStartingAtInsertedPoint
					                        : goodResultsForAllIntersections,
				                        true);

				CheckIntersectionPoints(rotatedRing, ring2, goodResults, false, true);

				AssertRelationsXY(rotatedRing, ring2, true);
				AssertRelationsXY(ring2, rotatedRing, true);

				rotatedRing.Reverse();

				CheckIntersectionPoints(rotatedRing, ring2,
				                        rotatedRing[0].Equals(pointAlongTargetSegment)
					                        ? goodResultsForRingStartingAtInsertedPoint
					                        : goodResults);

				CheckIntersectionPoints(rotatedRing, ring2,
				                        rotatedRing[0].Equals(pointAlongTargetSegment)
					                        ? goodResultsForRingStartingAtInsertedPoint
					                        : goodResultsForAllIntersections, true);

				AssertRelationsXY(rotatedRing, ring2, false, true);
				AssertRelationsXY(ring2, rotatedRing, false, true);
			}
		}

		private static void AssertRelationsXY(IList<Pnt3D> ring1Points,
		                                      IList<Pnt3D> ring2Points,
		                                      bool expectTouching,
		                                      bool? expectTouchingDisragardingOrientation
			                                      = null,
		                                      bool expectIntersecting = true,
		                                      bool expectEqual = false)
		{
			var ring1 = new Linestring(ring1Points);
			var ring2 = new Linestring(ring2Points);

			if (expectTouchingDisragardingOrientation == null)
			{
				expectTouchingDisragardingOrientation = expectTouching;
			}

			bool disjoint;
			Assert.AreEqual(expectTouchingDisragardingOrientation,
			                GeomRelationUtils.TouchesXY(ring1, ring2, 0.0001, out disjoint, true));

			Assert.AreEqual(expectIntersecting, ! disjoint);

			Assert.AreEqual(expectTouching,
			                GeomRelationUtils.TouchesXY(ring1, ring2, 0.0001, out disjoint));

			Assert.AreEqual(expectIntersecting, ! disjoint);

			bool equalXY = GeomTopoOpUtils.AreEqualXY(ring1, ring2, 0.0001);
			Assert.AreEqual(expectEqual, equalXY);
		}

		[Test]
		public void
			CanGetIntersectionPointsXYWithLinearIntersectionEndpointsAtOtherInterior
			()
		{
			// Now both source points are on the same target segment

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 50, 9));
			ring1.Add(new Pnt3D(100, 20, 9));
			//ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 0, 9));
			ring2.Add(new Pnt3D(100, 100, 9));
			ring2.Add(new Pnt3D(200, 150, 9));
			ring2.Add(new Pnt3D(200, 0, 9));
			ring2.Add(new Pnt3D(100, 0, 9));

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(100, 20, 9),
				                  new Pnt3D(100, 50, 9)
			                  };

			// TODO: Check point order and Z-values always from source

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
				CheckIntersectionPoints(ring2, rotatedRing, goodResults);

				AssertRelationsXY(rotatedRing, ring2, true);
				AssertRelationsXY(ring2, rotatedRing, true);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, ring2, goodResults);
				CheckIntersectionPoints(ring2, rotatedRing, goodResults);

				AssertRelationsXY(rotatedRing, ring2, false, true);
				AssertRelationsXY(ring2, rotatedRing, false, true);
			}

			// The same with non-closed path:
			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);

			// additional intermediate vertex:
			var pointAlongTargetSegment = new Pnt3D(100, 25, 0);
			ring1.Insert(3, pointAlongTargetSegment);

			var goodResultsForRingStartingAtInsertedPoint = new List<Pnt3D>(goodResults);
			goodResultsForRingStartingAtInsertedPoint.Add(pointAlongTargetSegment);
			goodResultsForRingStartingAtInsertedPoint.Add(pointAlongTargetSegment);

			var goodResultsForAllIntersections = new List<Pnt3D>(goodResults);
			goodResultsForAllIntersections.Add(pointAlongTargetSegment);

			CheckIntersectionPointsForTouchingRings(
				ring1, ring2, pointAlongTargetSegment, goodResults,
				goodResultsForRingStartingAtInsertedPoint,
				goodResultsForAllIntersections);

			// The same with non-closed path:
			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);

			// And with non-closed path with extra segment
			ring1.Add(new Pnt3D(100, -10, 9));
			goodResults[0].Y = 0;

			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);
		}

		[Test]
		public void CanGetIntersectionPointsXYIdenticalRings()
		{
			var ring1 = new List<Pnt3D>();

			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 50, 9));
			ring1.Add(new Pnt3D(100, 20, 9));
			ring1.Add(new Pnt3D(0, 0, 9));

			var ring2 = new List<Pnt3D>(ring1);

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 9), // linear intersection start
				                  new Pnt3D(0, 0, 9) // linear intersection end
			                  };

			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);

			ring1.RemoveAt(4);

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				var goodResultsRotated = new List<Pnt3D>
				                         {
					                         rotatedRing[0], // linear intersection start
					                         rotatedRing[0] // linear intersection end
				                         };

				if (i > 0)
				{
					// also include the start/end points of ring2
					goodResultsRotated.Add(ring2[0]);
					goodResultsRotated.Add(ring2[0]);
				}

				CheckIntersectionPoints(rotatedRing, ring2, goodResultsRotated);
				CheckIntersectionPoints(rotatedRing, ring2, new List<Pnt3D>(0), false,
				                        true);

				AssertRelationsXY(rotatedRing, ring2, false, false, true, true);
				AssertRelationsXY(ring2, rotatedRing, false, false, true, true);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, ring2, goodResultsRotated);
				CheckIntersectionPoints(rotatedRing, ring2, new List<Pnt3D>(0), false,
				                        true);

				// rotatedRing is interior, i.e. should be touching exterior ring2
				AssertRelationsXY(rotatedRing, ring2, true, false, true, true);
				AssertRelationsXY(ring2, rotatedRing, true, false, true, true);
			}

			// The same with non-closed path:
			goodResults.Add(new Pnt3D(100, 20, 9));

			goodResults = new List<Pnt3D>
			              {
				              ring1[0], // linear intersection start
				              ring1[ring1.Count - 1] // linear intersection end
			              };

			CheckIntersectionPoints(ring1, ring2, goodResults);
			CheckIntersectionPoints(ring2, ring1, goodResults);
		}

		[Test]
		public void CanGetIntersectionLinesXYWithLinearIntersectionEndpoints()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			//ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 0, 9));
			ring2.Add(new Pnt3D(100, 100, 9));
			ring2.Add(new Pnt3D(200, 150, 9));
			ring2.Add(new Pnt3D(200, 0, 9));
			ring2.Add(new Pnt3D(100, 0, 9));

			Linestring path1 = null;
			Linestring path2;
			IList<Linestring> intersectionLines;
			List<Line3D> goodResults;
			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				path1 = new Linestring(rotatedRing);
				path2 = new Linestring(ring2);
				intersectionLines =
					GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);

				goodResults = new List<Line3D>
				              {
					              new Line3D(new Pnt3D(100, 100, 9),
					                         new Pnt3D(100, 0, 9))
				              };

				Assert.AreEqual(1, intersectionLines.Count);
				Assert.True(goodResults.Contains(intersectionLines[0][0]));

				AssertRelationsXY(rotatedRing, ring2, true);
				AssertRelationsXY(ring2, rotatedRing, true);

				ring2.Reverse();
				intersectionLines =
					GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);
				Assert.AreEqual(1, intersectionLines.Count);
				Assert.True(goodResults.Contains(intersectionLines[0][0]));

				AssertRelationsXY(rotatedRing, ring2, false, true);
				AssertRelationsXY(ring2, rotatedRing, false, true);

				ring1.Reverse();
				intersectionLines =
					GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);
				Assert.AreEqual(1, intersectionLines.Count);
				Assert.True(goodResults.Contains(intersectionLines[0][0]));

				// TODO: Is touches true if two inner rings touch?
				AssertRelationsXY(rotatedRing, ring2, false, true);
				AssertRelationsXY(ring2, rotatedRing, false, true);

				ring2.Reverse();
				intersectionLines =
					GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);
				Assert.AreEqual(1, intersectionLines.Count);
				Assert.True(goodResults.Contains(intersectionLines[0][0]));

				AssertRelationsXY(rotatedRing, ring2, true);
				AssertRelationsXY(ring2, rotatedRing, true);

				ring1.Reverse();
			}

			// insert vertex along intersecting segment
			ring2.Insert(1, new Pnt3D(100, 40, 6));
			path2 = new Linestring(ring2);

			goodResults = new List<Line3D>
			              {
				              new Line3D(new Pnt3D(100, 100, 9),
				                         new Pnt3D(100, 40, 9)),
				              new Line3D(new Pnt3D(100, 40, 9),
				                         new Pnt3D(100, 0, 9))
			              };

			Assert.NotNull(path1);
			intersectionLines = GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);

			Assert.AreEqual(1, intersectionLines.Count);
			Assert.AreEqual(2, intersectionLines[0].SegmentCount);
			Assert.True(goodResults.Contains(intersectionLines[0][0]));
			Assert.True(goodResults.Contains(intersectionLines[0][1]));

			// same point exists in both paths:
			ring1.Insert(3, new Pnt3D(100, 40, 6));
			path1 = new Linestring(ring1);

			goodResults[0].EndPoint.Z = 6;
			goodResults[1].StartPoint.Z = 6;

			intersectionLines = GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);

			Assert.AreEqual(1, intersectionLines.Count);
			Assert.AreEqual(2, intersectionLines[0].SegmentCount);
			Assert.True(goodResults.Contains(intersectionLines[0][0]));
			Assert.True(goodResults.Contains(intersectionLines[0][1]));
		}

		[Test]
		public void CanGetIntersectionLinesXYEqual()
		{
			var ring1 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			//ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: the same, but with extra point along
			var ring2 = new List<Pnt3D>(ring1);
			ring2.Insert(1, new Pnt3D(0, 40, 6));
			ring2.Add(ring2[0]);

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				var path1 = new Linestring(rotatedRing);
				var path2 = new Linestring(ring2);
				IList<Linestring> intersectionLines = GeomTopoOpUtils.GetIntersectionLinesXY(
					path1, new MultiPolycurve(new[] { path2 }), 0.0001);

				Assert.AreEqual(1, intersectionLines.Count);
				Assert.IsTrue(
					GeomTopoOpUtils.AreEqualXY(path1, intersectionLines[0], 0.0001, true));

				// Difference: empty
				TestDifference(path1, path2, 0.0001,
				               new MultiPolycurve(new List<Linestring>()));

				path2.ReverseOrientation();
				intersectionLines =
					GeomTopoOpUtils.GetIntersectionLinesXY(path1, path2, 0.0001);

				Assert.AreEqual(1, intersectionLines.Count);
				Assert.IsTrue(
					GeomTopoOpUtils.AreEqualXY(path1, intersectionLines[0], 0.0001, true));
				Assert.IsTrue(intersectionLines[0].GetPoints().All(p => p.Z == 9));

				// Difference: empty
				TestDifference(path1, path2, 0.0001,
				               new MultiPolycurve(new List<Linestring>()));
			}
		}

		private static void TestDifference(Linestring source,
		                                   Linestring target,
		                                   double tolerance,
		                                   MultiLinestring expectedResult)
		{
			MultiLinestring multiLinestring =
				new MultiPolycurve(new List<Linestring> { target });

			MultiLinestring result = new MultiPolycurve(
				GeomTopoOpUtils.GetDifferenceLinesXY(source, multiLinestring, tolerance));

			Assert.True(expectedResult.Equals(result));
		}

		[Test]
		public void CanGetIntersectionPointsAlmostTouchingInVertex()
		{
			// Reproduces TOP-5165. The lines technically do not intersect and even the two
			// start-points are just over the tolerance.
			var segment1 = new[]
			               {
				               new Pnt3D(
					               166.599, 7000.856, 244.278999999995),
				               new Pnt3D(
					               181.65, 6996.056, 244.278999999995)
			               };
			var segment2 = new[]
			               {
				               new Pnt3D(
					               166.589, 7000.855, 242.471000000005),
				               new Pnt3D(
					               166.253, 6999.81, 242.470000000001)
			               };

			EnsureSegmentIntersections(segment1, segment2, 0.01, 0);
		}

		[Test]
		public void CanGetIntersectionPointTouchingVertexJustWithinTolerance()
		{
			var segment1 = new[]
			               {
				               new Pnt3D(
					               166.599, 7000.857, 244.278999999995),
				               new Pnt3D(
					               181.65, 6996.056, 244.278999999995)
			               };

			var segment2 = new[]
			               {
				               new Pnt3D(
					               166.590, 7000.853, 242.471000000005),
				               new Pnt3D(
					               166.253, 6999.81, 242.470000000001)
			               };

			const double tolerance = 0.01;

			EnsureSegmentIntersections(segment1, segment2, tolerance, 1,
			                           IntersectionPointType.TouchingInPoint);
		}

		[Test]
		public void CanGetIntersectionPointTouchingVertexExactlyWithinTolerance()
		{
			// The distance between the start points is exactly 1cm, but numerically it is slightly higher
			// The current idea is that the caller should provide a slightly increased tolerance if the points
			// exactly within the actual tolerance should yield an intersection

			var segment1 = new[]
			               {
				               new Pnt3D(
					               166.598, 7000.857, 244.278999999995),
				               new Pnt3D(
					               181.65, 6996.056, 244.278999999995)
			               };

			var segment2 = new[]
			               {
				               new Pnt3D(
					               166.590, 7000.851, 242.471000000005),
				               new Pnt3D(
					               166.253, 6999.81, 242.470000000001)
			               };

			// This is necessary to get consistent results:
			var tolerance = 0.01 + MathUtils.GetDoubleSignificanceEpsilon(
				                segment1[0].X, segment1[0].Y,
				                segment2[0].X, segment2[0].Y);

			EnsureSegmentIntersections(segment1, segment2, tolerance, 1,
			                           IntersectionPointType.TouchingInPoint);

			// Now the linestring2's start point is actually on the linestring1's interior:
			segment1 = new[]
			           {
				           new Pnt3D(166.598, 7000.857, 244.278999999995),
				           new Pnt3D(181.65, 7000.857, 244.278999999995)
			           };

			segment2 = new[]
			           {
				           new Pnt3D(166.599, 7000.847, 242.471000000005),
				           new Pnt3D(166.253, 6999.81, 242.470000000001)
			           };

			EnsureSegmentIntersections(segment1, segment2, tolerance, 1,
			                           IntersectionPointType.TouchingInPoint);
		}

		[Test]
		public void CanGetIntersectionPointsTouchingJustOverTolerance()
		{
			var segment1 = new[]
			               {
				               new Pnt3D(5, 10, 20),
				               new Pnt3D(15, 10, 20)
			               };

			var segment2 = new[]
			               {
				               new Pnt3D(4.99, 9.991, 20),
				               new Pnt3D(5, 0, 20)
			               };

			EnsureSegmentIntersections(segment1, segment2, 0.01, 0);
		}

		private static void EnsureSegmentIntersections(
			IEnumerable<Pnt3D> segment1,
			IEnumerable<Pnt3D> segment2,
			double tolerance,
			int expectedIntersections,
			IntersectionPointType expectedType = IntersectionPointType.Unknown)
		{
			var linestring1 = new Linestring(segment1);
			var linestring2 = new Linestring(segment2);

			var intersections =
				SegmentIntersectionUtils
					.GetSegmentIntersectionsXY(linestring1, linestring2, tolerance)
					.ToList();

			Assert.AreEqual(expectedIntersections, intersections.Count);

			IList<IntersectionPoint3D> intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints((ISegmentList) linestring1,
				                                      (ISegmentList) linestring2,
				                                      tolerance);

			Assert.AreEqual(expectedIntersections, intersectionPoints.Count);

			Assert.True(intersectionPoints.All(p => expectedType == p.Type));
			Assert.True(intersectionPoints.All(p => ! double.IsNaN(p.VirtualTargetVertex)));

			// packaged into multilinestrings
			intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints(
					(ISegmentList) linestring1, new MultiPolycurve(new[] { linestring2 }),
					tolerance);

			Assert.AreEqual(expectedIntersections, intersectionPoints.Count);
			Assert.True(intersectionPoints.All(p => expectedType == p.Type));
			Assert.True(intersectionPoints.All(p => ! double.IsNaN(p.VirtualTargetVertex)));

			// other way round
			intersections =
				SegmentIntersectionUtils
					.GetSegmentIntersectionsXY(linestring2, linestring1, tolerance)
					.ToList();

			Assert.AreEqual(expectedIntersections, intersections.Count);

			intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints((ISegmentList) linestring2,
				                                      (ISegmentList) linestring1,
				                                      tolerance);

			Assert.AreEqual(expectedIntersections, intersectionPoints.Count);
			Assert.True(intersectionPoints.All(p => expectedType == p.Type));
			Assert.True(intersectionPoints.All(p => ! double.IsNaN(p.VirtualTargetVertex)));

			// other way round, as multilinestrings:
			intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints(
					(ISegmentList) linestring2, new MultiPolycurve(new[] { linestring1 }),
					tolerance);

			Assert.AreEqual(expectedIntersections, intersectionPoints.Count);
			Assert.True(intersectionPoints.All(p => expectedType == p.Type));
			Assert.True(intersectionPoints.All(p => ! double.IsNaN(p.VirtualTargetVertex)));
		}

		private static void CheckIntersectionPoints(List<Pnt3D> ring1, List<Pnt3D> ring2,
		                                            List<Pnt3D> goodResults,
		                                            bool allIntersections = false,
		                                            bool filterRingStartEnd = false)
		{
			const double tolerance = 0.0001;

			IList<Pnt3D> intersectionPointsXY = GetIntersectionPointsXY(
				ring1, ring2, tolerance, ! filterRingStartEnd, allIntersections);

			Assert.AreEqual(goodResults.Count, intersectionPointsXY.Count);

			foreach (Pnt3D point in intersectionPointsXY)
			{
				Assert.True(goodResults.Contains(point));
			}

			// Check symmetry - at least in XY the result should be equal
			intersectionPointsXY = GetIntersectionPointsXY(
				ring2, ring1, tolerance, ! filterRingStartEnd, allIntersections);

			Assert.AreEqual(goodResults.Count, intersectionPointsXY.Count);

			foreach (Pnt3D point in intersectionPointsXY)
			{
				Assert.True(goodResults.Any(r => r.EqualsXY(point, tolerance)));
			}
		}

		private static IList<Pnt3D> GetIntersectionPointsXY(
			[NotNull] IList<Pnt3D> path1Points,
			[NotNull] IList<Pnt3D> path2Points,
			double tolerance,
			bool includeLinearIntersectionIntermediateRingStartEndPoints = true,
			bool includeLinearIntersectionIntermediatePoints = false)
		{
			Linestring linestring1 = new Linestring(path1Points);
			Linestring linestring2 = new Linestring(path2Points);

			IList<IntersectionPoint3D> intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints(
					(ISegmentList) linestring1, (ISegmentList) linestring2, tolerance,
					includeLinearIntersectionIntermediateRingStartEndPoints,
					includeLinearIntersectionIntermediatePoints);

			double? vertexIndex = null;

			foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
			{
				if (intersectionPoint.Type != IntersectionPointType.LinearIntersectionIntermediate)
				{
					if (vertexIndex != null)
					{
						Assert.IsTrue(vertexIndex.Value <= intersectionPoint.VirtualSourceVertex);
					}

					vertexIndex = intersectionPoint.VirtualSourceVertex;
				}

				double factor;
				int localSegmentIdx =
					intersectionPoint.GetLocalSourceIntersectionSegmentIdx(linestring1, out factor);

				Pnt3D pointAlong =
					linestring1.GetSegment(localSegmentIdx).GetPointAlong(factor, true);

				Assert.True(pointAlong.Equals(intersectionPoint.Point));

				Line3D sourceSegment =
					linestring1.GetSegment(intersectionPoint.SegmentIntersection.SourceIndex);
				Line3D targetSegment =
					linestring2.GetSegment(intersectionPoint.SegmentIntersection.TargetIndex);

				Assert.True(sourceSegment.IntersectsPointXY(pointAlong, 0.0001));
				Assert.True(targetSegment.IntersectsPointXY(pointAlong, 0.0001));

				pointAlong = intersectionPoint.GetTargetPoint((ISegmentList) linestring2);

				Assert.True(pointAlong.EqualsXY(intersectionPoint.Point, 0.0001));
			}

			// Test the same as MultiPolycurve with other linestrings
			Linestring dummyLinestring = new Linestring(path1Points.Select(p =>
			{
				var moved =
					p.ClonePnt3D();
				moved.X += 1234;
				return moved;
			}));
			MultiPolycurve polycurve1 = new MultiPolycurve(new[]
			                                               {
				                                               dummyLinestring,
				                                               linestring1
			                                               });
			MultiPolycurve polycurve2 = new MultiPolycurve(new[]
			                                               {
				                                               linestring2
			                                               });

			IList<IntersectionPoint3D> intersectionPointsPolyCurves =
				GeomTopoOpUtils.GetIntersectionPoints(
					(ISegmentList) polycurve1, (ISegmentList) polycurve2, tolerance,
					includeLinearIntersectionIntermediateRingStartEndPoints,
					includeLinearIntersectionIntermediatePoints);

			for (int i = 0; i < intersectionPoints.Count; i++)
			{
				var lp = intersectionPoints[i];
				var pp = intersectionPointsPolyCurves[i];

				Assert.AreEqual(lp.Type, pp.Type);
				Assert.True(lp.Point.Equals(pp.Point));
				Assert.AreEqual(lp.VirtualSourceVertex, pp.VirtualSourceVertex);
				Assert.AreEqual(lp.VirtualTargetVertex, pp.VirtualTargetVertex);
			}

			return intersectionPoints.Select(ip => ip.Point).ToList();
		}

		[Test]
		public void CanGetIntersectionPointsBetweenMultipointAndRing()
		{
			List<Pnt3D> ring = new List<Pnt3D>();
			ring.Add(new Pnt3D(0, 0, 9));
			ring.Add(new Pnt3D(0, 100, 9));
			ring.Add(new Pnt3D(100, 100, 9));
			ring.Add(new Pnt3D(100, 0, 9));
			ring.Add(new Pnt3D(0, 0, 9));

			Multipoint<Pnt3D> sourceMultipoint = new Multipoint<Pnt3D>(2);
			// on the segment, between vertices
			sourceMultipoint.AddPoint(new Pnt3D(0, 40, 6));
			// on a vertex
			sourceMultipoint.AddPoint(new Pnt3D(100, 100, 25));
			// on the start/end vertex of the ring
			sourceMultipoint.AddPoint(new Pnt3D(0, 0, 6));
			// non-intersecting
			sourceMultipoint.AddPoint(new Pnt3D(35, 33, 21));

			Linestring targetRing = new Linestring(ring, true);

			IList<IntersectionPoint3D> result =
				GeomTopoOpUtils
					.GetIntersectionPoints(sourceMultipoint, targetRing,
					                       0.001, false).ToList();

			Assert.AreEqual(3, result.Count);
			Assert.IsTrue(sourceMultipoint.GetPoint(0).Equals(result[0].Point));
			Assert.IsTrue(sourceMultipoint.GetPoint(1).Equals(result[1].Point));
			Assert.IsTrue(sourceMultipoint.GetPoint(2).Equals(result[2].Point));

			result =
				GeomTopoOpUtils
					.GetIntersectionPoints(sourceMultipoint, targetRing, 0.001, false, true)
					.ToList();

			Assert.AreEqual(4, result.Count);
			Assert.IsTrue(sourceMultipoint.GetPoint(2).Equals(result[3].Point));

			result =
				GeomTopoOpUtils
					.GetIntersectionPoints(sourceMultipoint, targetRing, 0.001, true, false)
					.ToList();

			Assert.AreEqual(4, result.Count);
			Assert.IsTrue(sourceMultipoint.GetPoint(3).Equals(result[3].Point));
		}

		[Test]
		public void CanGetIntersectionPointsBetweenRingAndMultipoint()
		{
			List<Pnt3D> ring = new List<Pnt3D>();
			ring.Add(new Pnt3D(0, 0, 9));
			ring.Add(new Pnt3D(0, 100, 9));
			ring.Add(new Pnt3D(100, 100, 9));
			ring.Add(new Pnt3D(100, 0, 9));
			ring.Add(new Pnt3D(0, 0, 9));

			Multipoint<Pnt3D> targetMultipoint = new Multipoint<Pnt3D>(2);
			// on the segment, between vertices
			targetMultipoint.AddPoint(new Pnt3D(0, 40, 6));
			// on a vertex
			targetMultipoint.AddPoint(new Pnt3D(100, 100, 25));
			// on the start/end vertex of the ring
			targetMultipoint.AddPoint(new Pnt3D(0, 0, 6));
			// area-interior intersecting 
			targetMultipoint.AddPoint(new Pnt3D(35, 33, 21));

			Linestring sourceRing = new Linestring(ring, true);

			IList<IntersectionPoint3D> result =
				GeomTopoOpUtils
					.GetIntersectionPoints(sourceRing, targetMultipoint,
					                       0.001, false).ToList();

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(9, result[0].Point.Z);
			Assert.IsTrue(ring[2].Equals(result[1].Point));
			Assert.IsTrue(ring[0].Equals(result[2].Point));

			result =
				GeomTopoOpUtils
					.GetIntersectionPoints(sourceRing, targetMultipoint, 0.001, false, true)
					.ToList();

			Assert.AreEqual(4, result.Count);
			Assert.IsTrue(ring[4].Equals(result[3].Point));

			result =
				GeomTopoOpUtils
					.GetIntersectionPoints(sourceRing, targetMultipoint, 0.001, true, false)
					.ToList();

			Assert.AreEqual(4, result.Count);
			Assert.IsTrue(targetMultipoint.GetPoint(3).Equals(result[3].Point));
		}

		[Test]
		public void CanGetIntersectionPointsBetweenMultipointAndMultipoint()
		{
			List<Pnt3D> source = new List<Pnt3D>();
			source.Add(new Pnt3D(0, 0, 9));
			source.Add(new Pnt3D(0, 100, 9));
			source.Add(new Pnt3D(100, 100, 9));
			source.Add(new Pnt3D(100, 0, 9));
			source.Add(new Pnt3D(0, 0, 9));

			IPointList sourceMultipoint = new Multipoint<Pnt3D>(source);

			Multipoint<Pnt3D> targetMultipoint = new Multipoint<Pnt3D>(2);
			// on the segment, between vertices
			targetMultipoint.AddPoint(new Pnt3D(0, 40, 6));
			// on a vertex
			targetMultipoint.AddPoint(new Pnt3D(100, 100, 25));
			// on the start/end vertex of the ring
			targetMultipoint.AddPoint(new Pnt3D(0, 0, 6));
			// area-interior intersecting 
			targetMultipoint.AddPoint(new Pnt3D(35, 33, 21));

			var result =
				GeomTopoOpUtils.GetIntersectionPoints(sourceMultipoint,
				                                      targetMultipoint,
				                                      0.001).ToList();

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(9, result[0].Point.Z);
			Assert.IsTrue(source[2].Equals(result[1].Point));
			Assert.IsTrue(source[0].Equals(result[2].Point));

			result =
				GeomTopoOpUtils.GetIntersectionPoints(targetMultipoint,
				                                      sourceMultipoint,
				                                      0.001).ToList();

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(25, result[0].Point.Z);
			Assert.IsTrue(targetMultipoint.GetPoint(2).Equals(result[1].Point));
			Assert.IsTrue(targetMultipoint.GetPoint(2).Equals(result[2].Point));
		}

		[Test]
		public void CanGetIntersectionPointsBetweenMultipointAndPoint()
		{
			List<Pnt3D> source = new List<Pnt3D>();
			source.Add(new Pnt3D(0, 0, 9));
			source.Add(new Pnt3D(0, 100, 9));
			source.Add(new Pnt3D(100, 100, 9));
			source.Add(new Pnt3D(100, 0, 9));
			source.Add(new Pnt3D(0, 0, 9));

			IPointList sourceMultipoint = new Multipoint<Pnt3D>(source);

			Pnt3D targetPointNonIntersecting = new Pnt3D(0, 40, 6);
			Pnt3D targetPointSinglePointIntersection = new Pnt3D(100, 100, 25);
			Pnt3D targetPointDoubleIntersection = new Pnt3D(0, 0, 6);

			var result =
				GeomTopoOpUtils.GetIntersectionPoints(sourceMultipoint,
				                                      targetPointNonIntersecting,
				                                      0.001).ToList();

			Assert.AreEqual(0, result.Count);

			result =
				GeomTopoOpUtils.GetIntersectionPoints(targetPointNonIntersecting,
				                                      sourceMultipoint,
				                                      0.001).ToList();

			Assert.AreEqual(0, result.Count);

			result =
				GeomTopoOpUtils.GetIntersectionPoints(sourceMultipoint,
				                                      targetPointSinglePointIntersection,
				                                      0.001).ToList();

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(9, result[0].Point.Z);

			result =
				GeomTopoOpUtils.GetIntersectionPoints(targetPointSinglePointIntersection,
				                                      sourceMultipoint,
				                                      0.001).ToList();

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(25, result[0].Point.Z);

			result =
				GeomTopoOpUtils.GetIntersectionPoints(sourceMultipoint,
				                                      targetPointDoubleIntersection,
				                                      0.001).ToList();

			Assert.AreEqual(2, result.Count);
			Assert.IsTrue(sourceMultipoint.GetPoint(0).Equals(result[0].Point));
			Assert.IsTrue(sourceMultipoint.GetPoint(4).Equals(result[1].Point));

			result =
				GeomTopoOpUtils.GetIntersectionPoints(targetPointDoubleIntersection,
				                                      sourceMultipoint,
				                                      0.001).ToList();

			Assert.AreEqual(2, result.Count);
			Assert.IsTrue(targetPointDoubleIntersection.Equals(result[0].Point));
			Assert.IsTrue(targetPointDoubleIntersection.Equals(result[1].Point));
		}

		[Test]
		public void CanDeleteLinearSelfIntersectionTypeStrait()
		{
			// ---------------------------        -------*          ---------
			// |      *-----------       |        |      *          |       |
			// |      |          |       |   ->   |      |          |       |
			// |      |          |       |        |      |          |       |
			// |______|          |_______|        |______|          |_______|

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0),
				           new Pnt3D(80, 0, 0),
				           new Pnt3D(80, 98, 0),
				           new Pnt3D(20, 98, 0),
				           new Pnt3D(20, 0, 0)
			           };

			const double tolerance = 2.1;

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 2, 10, 4000));

			// including extra vertex:
			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 2, 12, 4000, 1));
		}

		[Test]
		public void CanDeleteLinearSelfIntersectionTypeSpike()
		{
			// ---------------------------|       -------*
			// |      *-------------------|       |      *
			// |      |                      ->   |      |
			// |      |                           |      |
			// |______|                           |______|				 

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 98, 0),
				           new Pnt3D(20, 98, 0),
				           new Pnt3D(20, 0, 0)
			           };

			const double tolerance = 2.1;

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 1, 5, 2000));

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 1, 6, 2000, 1));
		}

		[Test]
		public void CanDeleteLinearSelfIntersectionTypeSinglePointSpike()
		{
			// -------------------------\         -------*
			// |                         >        |      |
			// |      *-----------------/         |      *
			// |      |                      ->   |      |
			// |      |                           |      |
			// |______|                           |______|				 

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(20, 98, 0),
				           new Pnt3D(20, 0, 0)
			           };

			const double tolerance = 2.1;

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 1, 5, 2000));

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 1, 6, 2000, 1));
		}

		[Test]
		public void CanDeleteLinearSelfIntersectionTypeZigZag()
		{
			// 1----3-------2---4
			// |                |
			// |                |
			// |                |
			// |                |
			// |________________|

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(60, 100, 0),
				           new Pnt3D(20, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0)
			           };

			const double tolerance = 2.1;

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 1, 7, 10000));

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 1, 7, 10000, 1));
		}

		[Test]
		public void CanDeleteLinearSelfIntersectionVerticalRing()
		{
			//   1/5---2---------3---4

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(20, 0, 15),
				           new Pnt3D(60, 0, 15),
				           new Pnt3D(100, 0, 0),
				           new Pnt3D(0, 0, 0)
			           };

			const double tolerance = 2.1;

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 0, 0, 0));

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 0, 0, 0));
		}

		[Test]
		public void CanDeleteLinearSelfIntersectionVerticalRingWithDuplicateVertex()
		{
			//   1/5---2---------3---4

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(20, 0, 15),
				           new Pnt3D(20, 0, 15),
				           new Pnt3D(60, 0, 15),
				           new Pnt3D(100, 0, 0),
				           new Pnt3D(0, 0, 0)
			           };

			const double tolerance = 2.1;

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 0, 0, 0));

			WithRotatedRing(ring, l => AssertCanDeleteLinearSelfIntersections(
				                l, tolerance, 0, 0, 0));
		}

		[Test]
		public void CanPlanarizeLinearSelfIntersections()
		{
			var linestring1 =
				new Linestring(new[] { new Line3D(new Pnt3D(40, 0, 0), new Pnt3D(100, 0, 0)) });
			var linestring2 =
				new Linestring(new[] { new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(40, 0, 0)) });
			var linestring3 =
				new Linestring(new[] { new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(100, 0, 0)) });

			var polycurve = new MultiPolycurve(new[]
			                                   {
				                                   linestring1, linestring2, linestring3
			                                   });

			MultiLinestring result = GeomTopoOpUtils.PlanarizeLines(polycurve, 0.001);
			Assert.AreEqual(1, result.PartCount);
			Assert.AreEqual(2, result.SegmentCount);

			// Now with slightly perturbed vertices (TOP-5543):
			linestring1 =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(2600040.00000001, 1200000, 0),
					                          new Pnt3D(2600100, 1200000, 0))
				               });
			linestring2 =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(2600000, 1200000, 0),
					                          new Pnt3D(2600040, 1200000, 0))
				               });
			linestring3 =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(2600000, 1200000, 0),
					                          new Pnt3D(2600100, 1200000, 0))
				               });

			polycurve = new MultiPolycurve(new[]
			                               {
				                               linestring1, linestring2, linestring3
			                               });

			result = GeomTopoOpUtils.PlanarizeLines(polycurve, 0.001);
			Assert.AreEqual(1, result.PartCount);
			Assert.AreEqual(2, result.SegmentCount);

			linestring1 =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(2600040, 1200000, 0),
					                          new Pnt3D(2600100, 1200000, 0))
				               });
			linestring2 =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(2600000, 1200000, 0),
					                          new Pnt3D(2600040.00000001, 1200000, 0))
				               });
			linestring3 =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(2600000, 1200000, 0),
					                          new Pnt3D(2600100, 1200000, 0))
				               });

			polycurve = new MultiPolycurve(new[]
			                               {
				                               linestring1, linestring2, linestring3
			                               });

			result = GeomTopoOpUtils.PlanarizeLines(polycurve, 0.001);
			Assert.AreEqual(1, result.PartCount);
			Assert.AreEqual(2, result.SegmentCount);
		}

		[Test]
		public void CanUnionSimpleRingsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var overlapping = new List<Pnt3D>();
			overlapping.Add(new Pnt3D(40, -10, 0));
			overlapping.Add(new Pnt3D(40, 30, 0));
			overlapping.Add(new Pnt3D(200, 30, 0));
			overlapping.Add(new Pnt3D(200, -10, 0));

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring overlap = GeomTestUtils.CreateRing(overlapping);

			const double tolerance = 0.01;

			MultiPolycurve overlapPoly = new MultiPolycurve(new[] { overlap });

			MultiLinestring unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				poly1, overlapPoly, tolerance);

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(poly1, overlapPoly, tolerance, out _));

			var expected = GeomTestUtils.CreateRing(new List<Pnt3D>
			                                        {
				                                        new Pnt3D(0, 0, 9),
				                                        new Pnt3D(0, 100, 9),
				                                        new Pnt3D(100, 50, 9),
				                                        new Pnt3D(100, 30, 9),
				                                        new Pnt3D(200, 30, 0),
				                                        new Pnt3D(200, -10, 0),
				                                        new Pnt3D(40, -10, 0),
				                                        new Pnt3D(40, 8, 0),
				                                        new Pnt3D(0, 0, 9)
			                                        });

			Assert.AreEqual(expected.GetArea2D(), unionResult.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanUnionContainedRingsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(20, 20, 9),
				            new Pnt3D(20, 50, 9),
				            new Pnt3D(40, 50, 9),
				            new Pnt3D(40, 20, 9)
			            };

			RingGroup source = GeomTestUtils.CreatePoly(ring1);
			RingGroup target = GeomTestUtils.CreatePoly(ring2);

			const double tolerance = 0.01;

			MultiLinestring unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);
			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(source, target, tolerance, out _));

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			var expected = GeomTestUtils.CreateRing(ring1);
			double expectedArea = expected.GetArea2D();

			Assert.AreEqual(expectedArea, unionResult.GetArea2D(), 0.0001);

			// and vice-versa:
			unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				target, source, tolerance);
			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(target, source, tolerance, out _));

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(expectedArea, unionResult.GetArea2D(), 0.0001);

			// In case the target is equal to a source ring -> The source ring should be deleted
			Linestring interiorRing = GeomTestUtils.CreateRing(ring2);
			interiorRing.ReverseOrientation();
			source.AddInteriorRing(interiorRing);

			unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);
			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(source, target, tolerance, out _));

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(expectedArea, unionResult.GetArea2D(), 0.0001);

			// and vice-versa:
			unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				target, source, tolerance);
			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(target, source, tolerance, out _));

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(expectedArea, unionResult.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanUnionTouchingRingsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
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
			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(source, target, tolerance, out _));

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), unionResult.GetArea2D(),
			                0.0001);

			// Now with an interior ring:
			var sourceInteriorRing = new List<Pnt3D>
			                         {
				                         new Pnt3D(20, 20, 9),
				                         new Pnt3D(40, 20, 9),
				                         new Pnt3D(40, 50, 9),
				                         new Pnt3D(20, 50, 9)
			                         };

			source.AddInteriorRing(GeomTestUtils.CreateRing(sourceInteriorRing));

			var targetSecondRing = new List<Pnt3D>
			                       {
				                       new Pnt3D(30, 50, 9),
				                       new Pnt3D(40, 50, 9),
				                       new Pnt3D(40, 30, 9),
				                       new Pnt3D(30, 30, 9),
			                       };

			target.AddLinestring(GeomTestUtils.CreateRing(targetSecondRing));

			unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);
			Assert.IsTrue(GeomTopoOpUtils.CanDissolveAreasXY(source, target, tolerance, out _));

			Assert.AreEqual(source.PartCount, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), unionResult.GetArea2D(),
			                0.0001);
		}

		[Test]
		public void CanUnionTouchingMultipartRingsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(100, 0, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(200, 100, 0),
				            new Pnt3D(200, 0, 0)
			            };

			var ring1b = new List<Pnt3D>
			             {
				             new Pnt3D(200, 0, 9),
				             new Pnt3D(200, 100, 9),
				             new Pnt3D(300, 50, 9),
				             new Pnt3D(300, 20, 9)
			             };

			MultiPolycurve source = new MultiPolycurve(new[]
			                                           {
				                                           GeomTestUtils.CreateRing(ring1),
				                                           GeomTestUtils.CreateRing(ring1b)
			                                           });

			RingGroup target = GeomTestUtils.CreatePoly(ring2);

			const double tolerance = 0.01;

			MultiLinestring unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), unionResult.GetArea2D(),
			                0.0001);
		}

		[Test]
		public void CanUnionDuplicateRingsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup source = GeomTestUtils.CreatePoly(ring1);
			MultiLinestring target = GeomTestUtils.CreatePoly(ring1);

			const double tolerance = 0.01;

			MultiLinestring unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);

			Assert.AreEqual(1, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(source.GetArea2D(), unionResult.GetArea2D(), 0.0001);

			// Now with an interior ring:
			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(20, 20, 9),
				            new Pnt3D(40, 20, 9),
				            new Pnt3D(40, 50, 9),
				            new Pnt3D(20, 50, 9)
			            };

			source.AddInteriorRing(GeomTestUtils.CreateRing(ring2));

			target = source.Clone();

			unionResult = GeomTopoOpUtils.GetUnionAreasXY(
				source, target, tolerance);

			Assert.AreEqual(source.PartCount, unionResult.PartCount);
			Assert.AreEqual(true, unionResult.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(source.GetArea2D(), unionResult.GetArea2D(), 0.0001);

			// Now check spaghetti union:
			var allInputs = new List<MultiLinestring> { source, target };
			MultiLinestring unionResult2 = GeomTopoOpUtils.GetUnionAreasXY(allInputs, tolerance);

			Assert.AreEqual(unionResult.PartCount, unionResult2.PartCount);
			Assert.AreEqual(true, unionResult2.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(unionResult2.GetArea2D(), unionResult.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanUnionWithInnerRings()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var sourceInner1 = new List<Pnt3D>
			                   {
				                   new Pnt3D(25, 50, 0),
				                   new Pnt3D(50, 50, 0),
				                   new Pnt3D(50, 75, 0),
				                   new Pnt3D(25, 75, 0)
			                   };

			Linestring sourceInner = GeomTestUtils.CreateRing(sourceInner1);
			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1), new[] { sourceInner });

			var innerRing2Overlapping = new List<Pnt3D>
			                            {
				                            new Pnt3D(40, -10, 0),
				                            new Pnt3D(40, 30, 0),
				                            new Pnt3D(200, 30, 0),
				                            new Pnt3D(200, -10, 0)
			                            };

			var target = new RingGroup(GeomTestUtils.CreateRing(innerRing2Overlapping));

			MultiLinestring result = UnionAreasXY(poly, target);

			Assert.AreEqual(2, result.PartCount);

			var expectedOuterRing = GeomTestUtils.CreateRing(new List<Pnt3D>
			                                                 {
				                                                 new Pnt3D(0, 0, 9),
				                                                 new Pnt3D(0, 100, 9),
				                                                 new Pnt3D(100, 100, 9),
				                                                 new Pnt3D(100, 30, 9),
				                                                 new Pnt3D(200, 30, 0),
				                                                 new Pnt3D(200, -10, 0),
				                                                 new Pnt3D(40, -10, 0),
				                                                 new Pnt3D(40, 0, 0),
				                                                 new Pnt3D(0, 0, 9)
			                                                 });

			double expectedOuterRingArea = expectedOuterRing.GetArea2D();
			double expectedOuterWithFirstInnerRingArea =
				expectedOuterRingArea + sourceInner.GetArea2D();

			Assert.AreEqual(expectedOuterRingArea, result.GetLinestring(0).GetArea2D());
			Assert.AreEqual(expectedOuterWithFirstInnerRingArea, result.GetArea2D());

			// Now add another ring to the input and cut through the ring
			var inner2 = new List<Pnt3D>
			             {
				             new Pnt3D(50, 25, 0),
				             new Pnt3D(75, 25, 0),
				             new Pnt3D(75, 45, 0),
				             new Pnt3D(50, 45, 0)
			             };

			poly.AddInteriorRing(GeomTestUtils.CreateRing(inner2));

			result = UnionAreasXY(poly, target);
			Assert.AreEqual(3, result.PartCount);

			// Minus the remaining area of the intersected island
			double expectedResultArea = expectedOuterWithFirstInnerRingArea - 25 * 15;

			Assert.AreEqual(expectedResultArea, result.GetArea2D());

			// Now with a source island that is fully covered by the target
			var inner3 = new List<Pnt3D>
			             {
				             new Pnt3D(80, 10, 123),
				             new Pnt3D(90, 10, 123),
				             new Pnt3D(90, 20, 123),
				             new Pnt3D(80, 20, 123),
			             };

			poly.AddInteriorRing(GeomTestUtils.CreateRing(inner3));

			// The third ring should have been erased by the target;
			result = UnionAreasXY(poly, target);
			Assert.AreEqual(3, result.PartCount);
			Assert.AreEqual(expectedResultArea, result.GetArea2D());

			// And with a target island fully covered by the source:
			var targetInner = new List<Pnt3D>
			                  {
				                  new Pnt3D(91, 10, 123),
				                  new Pnt3D(98, 10, 123),
				                  new Pnt3D(98, 20, 123),
				                  new Pnt3D(91, 20, 123),
			                  };

			target.AddInteriorRing(GeomTestUtils.CreateRing(targetInner));

			result = UnionAreasXY(poly, target);
			Assert.AreEqual(3, result.PartCount);
			Assert.AreEqual(expectedResultArea, result.GetArea2D());

			// And with a target island not covered by the source:
			var targetInner2 = new List<Pnt3D>
			                   {
				                   new Pnt3D(150, 10, 321),
				                   new Pnt3D(175, 10, 321),
				                   new Pnt3D(175, 20, 321),
				                   new Pnt3D(150, 20, 321),
			                   };

			target.AddInteriorRing(GeomTestUtils.CreateRing(targetInner2));

			expectedResultArea -= 25 * 10;
			result = UnionAreasXY(poly, target);
			Assert.AreEqual(4, result.PartCount);
			Assert.AreEqual(expectedResultArea, result.GetArea2D());

			//result = CutPlanarBothWays(poly, openTarget, 2, 1);
			//Assert.AreEqual(3, result.Sum(p => p.Count));
		}

		private static MultiLinestring UnionAreasXY(MultiLinestring poly,
		                                            MultiLinestring target,
		                                            double tolerance = 0.001)
		{
			MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(poly, target, tolerance);

			// In the meanwhile check the spaghetti union:
			var allInputs = new List<MultiLinestring> { poly, target };
			MultiLinestring unionResult2 = GeomTopoOpUtils.GetUnionAreasXY(allInputs, tolerance);

			Assert.AreEqual(result.PartCount, unionResult2.PartCount);
			if (result.PartCount > 0)
			{
				bool? clockwise1 = result.GetLinestring(0).ClockwiseOriented;
				bool? clockwise2 = unionResult2.GetLinestring(0).ClockwiseOriented;

				Assert.AreEqual(clockwise1, clockwise2);
			}

			Assert.AreEqual(result.GetArea2D(), unionResult2.GetArea2D(), 0.0001);

			return result;
		}

		[Test]
		public void CanUnionWithMultipleOuterRings()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 0),
				            new Pnt3D(0, 100, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 0)
			            };

			var sourceInner1 = new List<Pnt3D>
			                   {
				                   new Pnt3D(25, 50, 0),
				                   new Pnt3D(50, 50, 0),
				                   new Pnt3D(50, 75, 0),
				                   new Pnt3D(25, 75, 0)
			                   };

			Linestring sourceInner = GeomTestUtils.CreateRing(sourceInner1);
			var poly = new RingGroup(GeomTestUtils.CreateRing(ring1), new[] { sourceInner });

			var disjointTarget = new List<Pnt3D>
			                     {
				                     new Pnt3D(110, -10, 0),
				                     new Pnt3D(110, 30, 0),
				                     new Pnt3D(200, 30, 0),
				                     new Pnt3D(200, -10, 0)
			                     };

			var target = new RingGroup(GeomTestUtils.CreateRing(disjointTarget));

			MultiLinestring result = UnionAreasXY(poly, target);

			Assert.AreEqual(3, result.PartCount);

			double expectedArea = poly.GetArea2D() + target.GetArea2D();
			Assert.AreEqual(expectedArea, result.GetArea2D());

			// Now add another outer ring to the source that intersects the target
			var source2 = new List<Pnt3D>
			              {
				              new Pnt3D(150, 20, 0),
				              new Pnt3D(150, 50, 0),
				              new Pnt3D(175, 50, 0),
				              new Pnt3D(175, 20, 0)
			              };

			poly.AddLinestring(GeomTestUtils.CreateRing(source2));

			result = UnionAreasXY(poly, target);

			Assert.AreEqual(3, result.PartCount);

			// Minus the remaining area of the intersected island
			expectedArea += 25 * 20;

			Assert.AreEqual(expectedArea, result.GetArea2D());

			// Now with a target ring inside a source island:
			var target2 = new List<Pnt3D>
			              {
				              new Pnt3D(30, 60, 0),
				              new Pnt3D(30, 70, 0),
				              new Pnt3D(40, 70, 0),
				              new Pnt3D(40, 60, 0),
			              };

			target.AddLinestring(GeomTestUtils.CreateRing(target2));

			// The second target ring should be part of the result;
			result = UnionAreasXY(poly, target);
			Assert.AreEqual(4, result.PartCount);

			expectedArea += 10 * 10;
			Assert.AreEqual(expectedArea, result.GetArea2D());
		}

		[Test]
		public void CanUnionWithMultipleOuterRingsConnectedByTargetInPoint()
		{
			// The target intersects sourceRing1 and touches sourceRing2 in a point
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Now add another outer ring to the source that intersects the target
			var sourceRing2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(150, 20, 0),
				                  new Pnt3D(150, 50, 0),
				                  new Pnt3D(175, 50, 0),
				                  new Pnt3D(175, 20, 0)
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
					// The target touches the island including the touching point (from the inside) in a line:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(100, 100, 0),
						                       new Pnt3D(150, 20, 0),
						                       new Pnt3D(120, 0, 0),
						                       new Pnt3D(100, 0, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					double tolerance = 0.001;
					MultiLinestring union =
						UnionAreasXY(source, target, tolerance);

					Assert.AreEqual(2, union.PartCount);

					double expectedArea = source.GetArea2D() + target.GetArea2D();
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Intersection of result with target/source:
					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(union, target, tolerance);

					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D(), tolerance);

					// Difference, to compare
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(union, target, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(source.GetArea2D(), difference.GetArea2D(), tolerance);
				}
			}
		}

		[Test]
		public void CanUnionWithMultipleOuterRingsMergedByTargetTouchingInPoint()
		{
			// The target intersects sourceRing1
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Now add another outer ring to the source that is contained by the target and touches
			// target in a point
			var sourceRing2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(150, 20, 0),
				                  new Pnt3D(150, 50, 0),
				                  new Pnt3D(175, 50, 0),
				                  new Pnt3D(175, 20, 0)
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
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(100, 100, 0),
						                       new Pnt3D(175, 50, 0),
						                       new Pnt3D(200, 0, 0),
						                       new Pnt3D(100, 0, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					double tolerance = 0.001;
					MultiLinestring union =
						UnionAreasXY(source, target, tolerance);

					Assert.AreEqual(1, union.PartCount);

					double expectedArea = source.GetArea2D() + target.GetArea2D() -
					                      source.GetLinestring(1).GetArea2D();
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Intersection of result with target/source:
					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(union, target, tolerance);

					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D(), tolerance);

					// Difference, to compare
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(union, target, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(source.GetLinestring(0).GetArea2D(), difference.GetArea2D(),
					                tolerance);
				}
			}
		}

		[Test]
		public void CanUnionTouchingOuterRingsWithBoundaryLoopTargetEqualToRing2()
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

					double tolerance = 0.001;
					MultiLinestring union = UnionAreasXY(source, target, tolerance);

					Assert.AreEqual(1, union.PartCount);

					double expectedArea = source.GetLinestring(0).GetArea2D() * 2;
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Flip arguments:
					union = UnionAreasXY(target, source, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Intersection of result with target/source:
					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(union, target, tolerance);

					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D(), tolerance);

					// Difference, to compare
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(union, target, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(source.GetArea2D(), difference.GetArea2D(), tolerance);
				}
			}
		}

		[Test]
		public void CanUnionTouchingOuterRingsWithBoundaryLoopTargetContainedByRing2()
		{
			// The specialty here is that the source ring 2 is slightly larger than the inner
			// loop of the boundary loop and therefore nothing should remain as island.
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Now add another outer ring to the source, that contains the target boundary
			// loop (interior part) and touches sourceRing1 in a point
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
						                       //new Pnt3D(175, 50, 0), // Different from source -> contained
						                       new Pnt3D(150, 70, 0),
						                       new Pnt3D(100, 50, 0),
						                       new Pnt3D(100, 100, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					double tolerance = 0.001;
					MultiLinestring union =
						UnionAreasXY(source, target, tolerance);

					Assert.AreEqual(1, union.PartCount);

					double expectedArea = source.GetLinestring(0).GetArea2D() * 2;
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Flip arguments:
					union = UnionAreasXY(target, source, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Intersection of result with target/source:
					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(union, target, tolerance);

					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D(), tolerance);

					// Difference, to compare
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(union, target, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(union.GetArea2D() - intersection.GetArea2D(),
					                difference.GetArea2D(), tolerance);
				}
			}
		}

		[Test]
		public void CanUnionWithTouchingOuterRingsMergedByTargetToBoundaryLoop()
		{
			// The target intersects sourceRing1 and touches sourceRing2 in a point
			var sourceRing1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(0, 100, 0),
				                  new Pnt3D(100, 100, 0),
				                  new Pnt3D(100, 0, 0)
			                  };

			// Now add another outer ring to the source that intersects the target
			var sourceRing2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(0, 0, 0),
				                  new Pnt3D(50, -50, 0),
				                  new Pnt3D(50, -80, 0),
				                  new Pnt3D(-50, -80, 0),
				                  new Pnt3D(-50, 0, 0)
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
					// The target touches the island including the touching point (from the inside) in a line:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(50, -60, 0),
						                       new Pnt3D(50, 0, 0),
						                       new Pnt3D(100, 0, 0),
						                       new Pnt3D(100, -60, 0)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					double tolerance = 0.001;
					MultiLinestring union =
						UnionAreasXY(source, target, tolerance);

					Assert.AreEqual(1, union.PartCount);

					double expectedArea = source.GetArea2D() + target.GetArea2D();
					Assert.AreEqual(expectedArea, union.GetArea2D());

					// Intersection of result with target/source:
					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(union, target, tolerance);

					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D(), tolerance);

					// Difference, to compare
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(union, target, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(source.GetArea2D(), difference.GetArea2D(), tolerance);
				}
			}
		}

		[Test]
		public void CanUnionWithMultipleRingsTouchingInPointAndLine()
		{
			MultiPolycurve source = (MultiPolycurve) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("union_multipart_touching_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiPolygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("union_multipart_touching_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(source, target, 0.01);

			// Currently a touching island is preferred to the boundary loop:
			Assert.AreEqual(2, result.PartCount);

			// Minus the remaining area of the intersected island
			double expectedArea = source.GetArea2D() + target.GetArea2D();

			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanUnionTouchingRingsConnectedByLinearIntersectionWithTarget()
		{
			MultiPolycurve source = (MultiPolycurve) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("rings_touching_in_point_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiPolygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("rings_touching_in_point_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(source, target, 0.01);

			// Currently a touching island is preferred to the boundary loop:
			Assert.AreEqual(2, result.PartCount);

			// Minus the remaining area of the intersected island
			double expectedArea = source.GetArea2D() + target.GetArea2D();

			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.0001);

			// vice-versa
			result = GeomTopoOpUtils.GetUnionAreasXY(target, source, 0.01);
			Assert.AreEqual(2, result.PartCount);
			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanUnionTouchingRingsConnectedByTarget()
		{
			MultiPolycurve source = (MultiPolycurve) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"touching_rings_connected_by_target_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiPolygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"touching_rings_connected_by_target_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(source, target, 0.01);

			// Now it is a single ring with a boundary loop:
			Assert.AreEqual(1, result.PartCount);

			// Minus the remaining area of the intersected island
			double expectedArea = source.GetArea2D() + target.GetArea2D();

			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.001);
		}

		[Test]
		public void CanUnionTouchingRingsOneRingContainedByTargetBoundaryLoop()
		{
			MultiPolycurve source = (MultiPolycurve) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"touching_rings_one_ring_in_loop_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiPolygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"touching_rings_one_ring_in_loop_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(source, target, 0.01);

			// Now it is a single ring without boundary loop:
			Assert.AreEqual(1, result.PartCount);

			// Minus the remaining area of the intersected island
			double expectedArea = source.GetArea2D() + target.GetArea2D();

			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.001);
		}

		[Test]
		public void CanUnionTargetRingContainingSourceBoundaryLoop()
		{
			// This tests the boundary loop detection where both(!) boundary loops have additional
			// intersections with the target.
			RingGroup source = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("boundary_loops_union_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"boundary_loops_union_target_intersecting_both_loops.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(source, target, 0.01);

			// Now it is a single outer ring without boundary loop and the small island in the west.
			Assert.AreEqual(2, result.PartCount);

			// Minus the remaining area of the inner boundary loop island
			double expectedArea = 376.646893;
			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.001);

			// Vice versa:
			result = GeomTopoOpUtils.GetUnionAreasXY(target, source, 0.01);
			Assert.AreEqual(2, result.PartCount);
			Assert.AreEqual(expectedArea, result.GetArea2D(), 0.001);
		}

		[Test]
		public void CanUnionWithSubToleranceVertexToIntersection_Top5660()
		{
			RingGroup source = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("intersection_cluster_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("intersection_cluster_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			double tolerance = 0.01;

			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			double expectedAreaUnion = 90.19;
			Assert.AreEqual(expectedAreaUnion, union.GetArea2D(), 0.01);

			// Probably not very accurate due to intersection-jumping in cluster
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			double expectedAreaDifference = union.GetArea2D() - target.GetArea2D();
			Assert.AreEqual(expectedAreaDifference, difference.GetArea2D(), 0.05);
		}

		[Test]
		public void
			CanUnionWithIntersectionCloseToShortishSegmentWithNonRepresentativeAngle_Top5795()
		{
			RingGroup source = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"union_intersection_at_shortish_segment_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"union_intersection_at_shortish_segment_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			// The interesting situation only arises with tolerance 0.0005!
			double tolerance = 0.0005;

			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			double expectedAreaUnion = 349.015;
			Assert.AreEqual(expectedAreaUnion, union.GetArea2D(), 0.01);

			// Probably not very accurate due to intersection-jumping in cluster
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			double expectedAreaDifference = union.GetArea2D() - target.GetArea2D();
			Assert.AreEqual(expectedAreaDifference, difference.GetArea2D(), 0.05);
		}

		[Test]
		public void CanUnionAtShortishZigZagWithAcuteAngleIntersection()
		{
			RingGroup source = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"zigzag_line_with_acute_angle_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"zigzag_line_with_acute_angle_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			// At 0.0005 there is no short segment in the original (it is 0.0007)
			double tolerance = 0.0005;

			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			// Originally, a contained exterior ring resulted!
			Assert.AreEqual(source.PartCount, union.PartCount);

			double expectedAreaUnion = 199.16113;
			Assert.AreEqual(expectedAreaUnion, union.GetArea2D(), 0.01);

			// Probably not very accurate due to intersection-jumping in cluster
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			double expectedAreaDifference = union.GetArea2D() - target.GetArea2D();
			Assert.AreEqual(expectedAreaDifference, difference.GetArea2D(), 0.05);

			// The same with a real short segment does not work (yet):
			// Either preemptively clean up short segments or properly simplify the full geometry
			// or deals with more special cases
			tolerance = 0.01;

			//union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			//// Originally, a contained exterior ring resulted!
			//Assert.AreEqual(source.PartCount, union.PartCount);

			//Assert.AreEqual(expectedAreaUnion, union.GetArea2D(), 0.01);

			//// Probably not very accurate due to intersection-jumping in cluster
			//difference = GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			//Assert.AreEqual(expectedAreaDifference, difference.GetArea2D(), 0.05);
		}

		[Test]
		public void CanUnionMultiBoundaryLoops()
		{
			// In the long term it might be better and cleaner to ensure clean geometries
			// by exploding all (?) boundary loops or at least those where the orientation
			// of both connecting loops are the same. Outer loops are already exploded!
			MultiPolycurve source = (MultiPolycurve) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"multi_boundary_loop_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiPolygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"multi_boundary_loop_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			// At 0.0005 there is no short segment in the original (it is 0.0007)
			double tolerance = 0.001;

			MultiLinestring union = GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);

			// The target 'fills' the middle loop of the 3-boundary-loops ring
			Assert.AreEqual(source.PartCount + 1, union.PartCount);

			double expectedAreaUnion = source.GetArea2D() + target.GetArea2D();
			Assert.AreEqual(expectedAreaUnion, union.GetArea2D(), 0.01);

			// Probably not very accurate due to intersection-jumping in cluster
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			Assert.AreEqual(source.PartCount, difference.PartCount);
			double expectedAreaDifference = union.GetArea2D() - target.GetArea2D();
			Assert.AreEqual(expectedAreaDifference, difference.GetArea2D(), 0.05);
		}

		[Test]
		public void CanUnionManyUnCrackedRings_Top5714()
		{
			Polyhedron source = (Polyhedron) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("triangulated_building_uncracked.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiSurface, wkbType);

			double tolerance = 0.01;

			MultiLinestring union = source.GetXYFootprint(tolerance, tolerance, out _);

			double expectedAreaUnion = 130.324;
			Assert.AreEqual(expectedAreaUnion, union.GetArea2D(), 0.01);
		}

		[Test]
		public void CanIntersectXYSimpleRings()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var overlapping = new List<Pnt3D>();
			overlapping.Add(new Pnt3D(40, -10, 0));
			overlapping.Add(new Pnt3D(40, 30, 0));
			overlapping.Add(new Pnt3D(200, 30, 0));
			overlapping.Add(new Pnt3D(200, -10, 0));

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring overlap = GeomTestUtils.CreateRing(overlapping);
			var target = new MultiPolycurve(new[] { overlap });

			const double tolerance = 0.01;

			MultiLinestring intersectionResult = GeomTopoOpUtils.GetIntersectionAreasXY(
				poly1, target, tolerance);

			Assert.AreEqual(1, intersectionResult.PartCount);
			Assert.AreEqual(true, intersectionResult.GetLinestring(0).ClockwiseOriented);

			var expected = GeomTestUtils.CreateRing(new List<Pnt3D>
			                                        {
				                                        new Pnt3D(100, 30, 9),
				                                        new Pnt3D(100, 20, 9),
				                                        new Pnt3D(40, 8, 9),
				                                        new Pnt3D(40, 30, 0)
			                                        });

			Assert.AreEqual(expected.GetArea2D(), intersectionResult.GetArea2D(), 0.0001);

			// And vice versa:
			intersectionResult = GeomTopoOpUtils.GetIntersectionAreasXY(
				target, poly1, tolerance);

			Assert.AreEqual(1, intersectionResult.PartCount);
			Assert.AreEqual(true, intersectionResult.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(expected.GetArea2D(), intersectionResult.GetArea2D(), 0.0001);

			// Double-check:
			MultiLinestring difference = GeomTopoOpUtils.GetDifferenceAreasXY(
				poly1, target, tolerance);

			MultiLinestring reunion = GeomTopoOpUtils.GetUnionAreasXY(
				intersectionResult, difference, tolerance);

			Assert.AreEqual(poly1.GetArea2D(), reunion.GetArea2D(), 0.0001);
		}

		[Test]
		public void CanIntersectXYContainedRings()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(20, 20, 9),
				            new Pnt3D(20, 50, 9),
				            new Pnt3D(40, 50, 9),
				            new Pnt3D(40, 20, 9)
			            };

			RingGroup source = GeomTestUtils.CreatePoly(ring1);
			RingGroup target = GeomTestUtils.CreatePoly(ring2);

			const double tolerance = 0.01;

			MultiLinestring intersection = GeomTopoOpUtils.GetIntersectionAreasXY(
				source, target, tolerance);

			Assert.AreEqual(1, intersection.PartCount);
			Assert.AreEqual(true, intersection.GetLinestring(0).ClockwiseOriented);

			var expected = GeomTestUtils.CreateRing(ring2);
			double expectedArea = expected.GetArea2D();

			Assert.AreEqual(expectedArea, intersection.GetArea2D(), 0.0001);

			// and vice-versa:
			intersection = GeomTopoOpUtils.GetIntersectionAreasXY(
				target, source, tolerance);

			Assert.AreEqual(1, intersection.PartCount);
			Assert.AreEqual(true, intersection.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(expectedArea, intersection.GetArea2D(), 0.0001);

			// In case the target is equal to a source hole -> The intersection should be empty
			Linestring interiorRing = GeomTestUtils.CreateRing(ring2);
			interiorRing.ReverseOrientation();
			source.AddInteriorRing(interiorRing);

			intersection = GeomTopoOpUtils.GetIntersectionAreasXY(
				source, target, tolerance);

			Assert.AreEqual(0, intersection.PartCount);

			// and vice-versa:
			intersection = GeomTopoOpUtils.GetIntersectionAreasXY(
				target, source, tolerance);

			Assert.AreEqual(0, intersection.PartCount);
		}

		[Test]
		public void CanIntersectXYDisjoint()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 20, 9)
			            };

			var disjoint = new List<Pnt3D>();
			disjoint.Add(new Pnt3D(140, -10, 0));
			disjoint.Add(new Pnt3D(140, 30, 0));
			disjoint.Add(new Pnt3D(300, 30, 0));
			disjoint.Add(new Pnt3D(300, -10, 0));

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring overlap = GeomTestUtils.CreateRing(disjoint);

			const double tolerance = 0.01;

			var target = new MultiPolycurve(new[] { overlap });
			MultiLinestring result =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// Now the same with the target inside a source-island:
			poly1.AddInteriorRing(new Linestring(new[]
			                                     {
				                                     new Pnt3D(25, 50, 0),
				                                     new Pnt3D(50, 50, 0),
				                                     new Pnt3D(50, 75, 0),
				                                     new Pnt3D(25, 75, 0),
				                                     new Pnt3D(25, 50, 0)
			                                     }
			                      ));

			target.AddLinestring(new Linestring(new[]
			                                    {
				                                    new Pnt3D(30, 55, 0),
				                                    new Pnt3D(45, 55, 0),
				                                    new Pnt3D(45, 70, 0),
				                                    new Pnt3D(30, 70, 0),
				                                    new Pnt3D(30, 55, 0)
			                                    }
			                     ));

			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// vice versa
			result = GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
			Assert.IsTrue(result.IsEmpty);
		}

		[Test]
		public void CanIntersectXYIdentical()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);
			Linestring equalRing = poly1.ExteriorRing.Clone();

			const double tolerance = 0.01;

			var target = new MultiPolycurve(new[] { equalRing });
			MultiLinestring result =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);

			Assert.AreEqual(1, result.PartCount);
			Assert.AreEqual(true, result.GetLinestring(0).ClockwiseOriented);

			Assert.AreEqual(poly1.GetArea2D(), result.GetArea2D(), 0.0001);

			// Difference:
			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// And in case one of them has a hole:
			poly1.AddInteriorRing(new Linestring(new[]
			                                     {
				                                     new Pnt3D(20, 40, 0),
				                                     new Pnt3D(50, 40, 0),
				                                     new Pnt3D(50, 60, 0),
				                                     new Pnt3D(20, 60, 0),
				                                     new Pnt3D(20, 40, 0)
			                                     }
			                      ));

			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);

			Assert.AreEqual(2, result.PartCount);
			Assert.AreEqual(true, result.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(false, result.GetLinestring(1).ClockwiseOriented);
			Assert.AreEqual(poly1.GetArea2D(), result.GetArea2D(), 0.0001);

			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// vice versa:
			result = GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);

			Assert.AreEqual(2, result.PartCount);
			Assert.AreEqual(true, result.GetLinestring(0).ClockwiseOriented);
			Assert.AreEqual(false, result.GetLinestring(1).ClockwiseOriented);
			Assert.AreEqual(poly1.GetArea2D(), result.GetArea2D(), 0.0001);

			result = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
			Assert.IsFalse(result.IsEmpty);
			Assert.AreEqual(target.GetArea2D() - poly1.GetArea2D(), result.GetArea2D());
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceInsideIsland()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

			const double tolerance = 0.01;

			// One of them has a hole:
			var interiorRingPoints = new[]
			                         {
				                         new Pnt3D(20, 40, 0),
				                         new Pnt3D(50, 40, 0),
				                         new Pnt3D(50, 60, 0),
				                         new Pnt3D(20, 60, 0)
			                         }.ToList();

			var interiorRing = new Linestring(GeomTestUtils.GetRotatedRing(interiorRingPoints, 0));

			poly1.AddInteriorRing(interiorRing);

			// Which is filled by a ring that has the same segments:
			Linestring filledHole = interiorRing.Clone();
			filledHole.ReverseOrientation();
			MultiLinestring target = new MultiPolycurve(new[] { filledHole });

			MultiLinestring result =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);

			Assert.IsTrue(result.IsEmpty);

			filledHole = new Linestring(GeomTestUtils.GetRotatedRing(interiorRingPoints, 1));

			filledHole.ReverseOrientation();
			target = new MultiPolycurve(new[] { filledHole });

			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// Now the filling is only partial:
			filledHole.ReplacePoint(1, new Pnt3D(20, 50, 0));
			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// Compare with difference:
			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(poly1.GetArea2D(), result.GetArea2D());

			// And vice versa:
			result = GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// Compare with difference:
			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(poly1.GetArea2D(), result.GetArea2D());

			// Now the filling is not complete but only partial:
			interiorRingPoints.RemoveAt(1);
			filledHole = new Linestring(GeomTestUtils.GetRotatedRing(interiorRingPoints, 1));

			filledHole.ReverseOrientation();
			target = new MultiPolycurve(new[] { filledHole });

			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);
		}

		[Test]
		public void CanGetIntersectionAreaXYWithLinearBoundaryIntersectionFromInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9),
				            new Pnt3D(0, 0, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

			const double tolerance = 0.01;

			var ring2 = new[]
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 60, 9),
				            new Pnt3D(60, 60, 9),
				            new Pnt3D(60, 0, 0)
			            }.ToList();

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				RingGroup poly2 = GeomTestUtils.CreatePoly(rotatedRing);

				MultiLinestring result =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

				Assert.IsFalse(result.IsEmpty);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(60 * 60, result.GetArea2D(), 0.001);

				// Difference, to compare
				result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, poly2, tolerance);
				Assert.IsFalse(result.IsEmpty);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(100 * 100 - 60 * 60, result.GetArea2D(), 0.001);
			}

			// Now with the ring2 slightly (below tolerance) off 
			ring2[1].X += 0.0002;

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				RingGroup poly2 = GeomTestUtils.CreatePoly(rotatedRing);

				MultiLinestring result =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

				Assert.IsFalse(result.IsEmpty);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(60 * 60, result.GetArea2D(), 0.001);

				// Difference, to compare
				result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, poly2, tolerance);
				Assert.IsFalse(result.IsEmpty);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(100 * 100 - 60 * 60, result.GetArea2D(), 0.001);
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYTargetCutsAndTouchesFromInside()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

			const double tolerance = 0.01;

			// The target touches the island (from the inside) in a single point:
			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(50, 100, 9),
				            new Pnt3D(100, 0, 9),
				            new Pnt3D(0, 0, 9)
			            };

			var target = new RingGroup(new Linestring(ring2));

			MultiLinestring intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.AreEqual(1, intersection.PartCount);
			Assert.AreEqual(poly1.GetArea2D() / 2, intersection.GetArea2D());

			// Compare with difference:
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(2, difference.PartCount);
			Assert.AreEqual(poly1.GetArea2D() / 2, difference.GetArea2D());

			MultiLinestring union =
				GeomTopoOpUtils.GetUnionAreasXY(difference, intersection, tolerance);
			Assert.AreEqual(poly1.PartCount, union.PartCount);
			Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

			// TODO: support multi-part target with touching rings and enclosed source!
			// .. and multi-part target that touch twice and sandwich the source.
			// This requires some special case logic to start along the target rather than
			// along the source.
			//union =
			//	GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
			//Assert.AreEqual(poly1.PartCount, union.PartCount);
			//Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

			// Now the intersection is on the other side,
			// i.e. the target has the inverse orientation:
			ring2 = new List<Pnt3D>
			        {
				        new Pnt3D(-60, 200, 9),
				        new Pnt3D(160, 200, 9),
				        new Pnt3D(100, 0, 9),
				        new Pnt3D(50, 100, 9),
				        new Pnt3D(0, 0, 9),
				        new Pnt3D(-60, 200, 9)
			        };

			target = new RingGroup(new Linestring(ring2));

			// The result should be the same but inverted (intersection==above difference)
			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.AreEqual(2, intersection.PartCount);
			Assert.AreEqual(poly1.GetArea2D() / 2, intersection.GetArea2D());

			// Compare with difference:
			difference = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(1, difference.PartCount);
			Assert.AreEqual(poly1.GetArea2D() / 2, difference.GetArea2D());
		}

		[Test]
		public void CanGetIntersectionAreaXYTargetTouchesIslandInSinglePoint()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

			const double tolerance = 0.01;

			// One of them has a hole:
			var interiorRingPoints = new[]
			                         {
				                         new Pnt3D(20, 40, 0),
				                         new Pnt3D(50, 40, 0),
				                         new Pnt3D(50, 60, 0),
				                         new Pnt3D(20, 60, 0)
			                         }.ToList();

			var interiorRing = new Linestring(GeomTestUtils.GetRotatedRing(interiorRingPoints, 0));

			poly1.AddInteriorRing(interiorRing);

			// The target touches the island (from the inside) in a single point:
			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(30, 40, 9),
				            new Pnt3D(30, 50, 9),
				            new Pnt3D(40, 50, 9),
				            new Pnt3D(30, 40, 9)
			            };

			var target = new RingGroup(new Linestring(ring2));

			MultiLinestring result =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsTrue(result.IsEmpty);

			// Compare with difference:
			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(poly1.PartCount, result.PartCount);
			Assert.AreEqual(poly1.GetArea2D(), result.GetArea2D());

			// Vice versa to check symmetry:
			result =
				GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
			Assert.IsTrue(result.IsEmpty);

			result = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
			Assert.AreEqual(target.PartCount, result.PartCount);
			Assert.AreEqual(target.GetArea2D(), result.GetArea2D());

			//
			// Now the target touches the island from the other side:
			ring2 = new List<Pnt3D>
			        {
				        new Pnt3D(30, 40, 9),
				        new Pnt3D(40, 20, 9),
				        new Pnt3D(30, 20, 9),
				        new Pnt3D(30, 40, 9)
			        };

			target = new RingGroup(new Linestring(ring2));

			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsFalse(result.IsEmpty);
			Assert.AreEqual(1, result.PartCount);
			Assert.AreEqual(target.GetArea2D(), result.GetArea2D());

			// Compare with difference:
			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(poly1.PartCount + 1, result.PartCount);
			Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), result.GetArea2D());

			// Vice versa to check symmetry:
			result = GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
			Assert.IsFalse(result.IsEmpty);
			Assert.AreEqual(1, result.PartCount);
			Assert.AreEqual(target.GetArea2D(), result.GetArea2D());

			result = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
			Assert.IsTrue(result.IsEmpty);

			//
			// And finally the target also goes to the outside of the exterior ring of the source:
			ring2 = new List<Pnt3D>
			        {
				        new Pnt3D(30, 40, 9),
				        new Pnt3D(40, 0, 9),
				        new Pnt3D(30, 0, 9),
				        new Pnt3D(30, 40, 9)
			        };

			target = new RingGroup(new Linestring(ring2));

			result = GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
			Assert.IsFalse(result.IsEmpty);
			Assert.AreEqual(target.GetArea2D(), result.GetArea2D(), 0.0001);

			// Compare with difference:
			// Now we expect either a boundary loop (Esri style) or the outer ring touches the
			// inner ring (ogc style)
			result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
			Assert.AreEqual(2, result.PartCount);
			Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), result.GetArea2D());
		}

		[Test]
		public void CanGetIntersectionAreaXYLargePolyPerformanceEqualPoly()
		{
			RingGroup source = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("huge_lockergestein.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup target = (RingGroup) source.Clone();

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			const double tolerance = 0.01;

			Stopwatch watch = Stopwatch.StartNew();
			MultiLinestring intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(source, target, tolerance);

			watch.Stop();
			Console.WriteLine("Lockergestein with clone - intersect: {0} ms",
			                  watch.ElapsedMilliseconds);

			Assert.AreEqual(intersection.GetArea2D(), source.GetArea2D(), 0.01);

			watch.Restart();
			MultiLinestring difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			watch.Stop();
			Console.WriteLine("Lockergestein with clone - difference (with index): {0}ms",
			                  watch.ElapsedMilliseconds);

			Assert.IsTrue(difference.IsEmpty);

			target.SpatialIndex = null;
			watch.Restart();
			difference = GeomTopoOpUtils.GetDifferenceAreasXY(source, target, tolerance);

			watch.Stop();
			Console.WriteLine("Lockergestein with clone - difference (without index): {0}ms",
			                  watch.ElapsedMilliseconds);

			Assert.IsTrue(difference.IsEmpty);

			// Moved by a few km
			MultiPolycurve multiTarget =
				new MultiPolycurve(
					target.GetLinestrings().Select(
						l => GeomTopoOpUtils.Move(l, 4567, 1234, 0)));

			watch.Restart();

			intersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(source, multiTarget, tolerance);

			watch.Stop();
			Console.WriteLine("Lockergestein with moved clone - intersect ({0} sqm): {1}ms",
			                  intersection.GetArea2D(), watch.ElapsedMilliseconds);

			Assert.AreEqual(14197784, intersection.GetArea2D(), 1);

			watch.Restart();
			difference =
				GeomTopoOpUtils.GetDifferenceAreasXY(source, multiTarget, tolerance);

			watch.Stop();
			Console.WriteLine(
				"Lockergestein with moved clone - difference (with index) ({0} sqm): {1}ms",
				difference.GetArea2D(), watch.ElapsedMilliseconds);

			// TODO: There is a weird vertical cut-off! Needs investigation
			//Assert.AreEqual(49640701.2531033, difference.GetArea2D(), 1);

			multiTarget.SpatialIndex = null;
			watch.Restart();
			difference = GeomTopoOpUtils.GetDifferenceAreasXY(source, multiTarget, tolerance);

			watch.Stop();
			Console.WriteLine("Lockergestein with moved clone - difference (without index): {0}ms",
			                  watch.ElapsedMilliseconds);

			// TODO: There is a weird vertical cut-off! Needs investigation
			//Assert.AreEqual(49640701.2531033, difference.GetArea2D(), 1);
		}

		#region Source self-intersections

		[Test]
		public void CanGetIntersectionPointsXYAtBoundaryLoopWithLinearIntersection()
		{
			// The source has a boundary loop to the outside (i.e. it is non-simple).
			// The target touches the boundary loop point.
			var sourceRingPoints = new List<Pnt3D>
			                       {
				                       new Pnt3D(0, 0, 9),
				                       new Pnt3D(0, 100, 9),
				                       new Pnt3D(50, 100, 9),
				                       new Pnt3D(20, 140, 9),
				                       new Pnt3D(20, 160, 9),
				                       new Pnt3D(50, 160, 9),
				                       new Pnt3D(50, 100, 9),
				                       new Pnt3D(100, 100, 9),
				                       new Pnt3D(100, 0, 9)
			                       };

			var targetRingPoints = new List<Pnt3D>
			                       {
				                       new Pnt3D(30, 40, 9),
				                       new Pnt3D(30, 100, 9),
				                       new Pnt3D(80, 100, 9),
				                       new Pnt3D(80, 40, 9),
				                       new Pnt3D(30, 40, 9)
			                       };

			var goodResults = new List<Pnt3D>
			                  {
				                  new Pnt3D(30, 100, 9),
				                  new Pnt3D(50, 100, 9),
				                  new Pnt3D(50, 100, 9),
				                  new Pnt3D(80, 100, 9)
			                  };

			for (var i = 0; i < 10; i++)
			{
				Pnt3D[] array1 = sourceRingPoints.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				CheckIntersectionPoints(rotatedRing, targetRingPoints, goodResults);
				CheckIntersectionPoints(targetRingPoints, rotatedRing, goodResults);

				AssertRelationsXY(rotatedRing, targetRingPoints, false);
				AssertRelationsXY(targetRingPoints, rotatedRing, false);

				rotatedRing.Reverse();
				CheckIntersectionPoints(rotatedRing, targetRingPoints, goodResults);
				CheckIntersectionPoints(targetRingPoints, rotatedRing, goodResults);

				// TODO: Properly support Touches() for boundary loop sources
				//AssertRelationsXY(rotatedRing, targetRingPoints, true, false);
				AssertRelationsXY(targetRingPoints, rotatedRing, true, false);
			}

			// The same with non-closed path:
			CheckIntersectionPoints(sourceRingPoints, targetRingPoints, goodResults);
			CheckIntersectionPoints(targetRingPoints, sourceRingPoints, goodResults);
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceIslandTouchesExteriorRing()
		{
			// The source has an inner ring touching the exterior in a point (OGC style)
			// The target touches the connection point between the two source rings.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// One of them has a hole:
			var interiorRingPoints = new[]
			                         {
				                         new Pnt3D(20, 40, 0),
				                         new Pnt3D(50, 40, 0),
				                         new Pnt3D(50, 100, 0),
				                         new Pnt3D(20, 60, 0)
			                         }.ToList();

			for (var i = 0; i < 5; i++)
			{
				var interiorRing =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRingPoints, i));

				RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

				poly1.AddInteriorRing(interiorRing);

				for (var t = 0; t < 5; t++)
				{
					// The target touches the island including the touching point (from the inside) in a line:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(50, 100, 9),
						                       new Pnt3D(80, 80, 9),
						                       new Pnt3D(80, 40, 9),
						                       new Pnt3D(50, 40, 9),
						                       // NOTE: With an extra 0-length segment, the result contains 2 inner rings!
						                       // Probably these should be handled (ignored) explicitly
						                       //new Pnt3D(50, 100, 9)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target.GetLength2D(), intersection.GetLength2D());

					// Currently the touching islands remains a touching island also in the result (OGC style)
					//
					// Compare with difference:
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), difference.GetArea2D());

					MultiLinestring union =
						GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(target.GetArea2D(), union.GetArea2D());

					// Now the target touches also the source outer ring in a line
					// -> the result has no inner ring any more:
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(50, 100, 9),
						                        new Pnt3D(80, 100, 9),
						                        new Pnt3D(80, 40, 9),
						                        new Pnt3D(50, 40, 9)
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target3, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target3.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), intersection.GetLength2D());

					//
					// Compare with difference:
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target3, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(560, difference.GetLength2D());
					Assert.AreEqual(poly1.GetArea2D() - target3.GetArea2D(),
					                difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, poly1, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), intersection.GetLength2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target3, poly1, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(target3.GetArea2D(), union.GetArea2D());

					//
					// Now the target touches the source island from inside the island (i.e. outside the polygon)
					//
					var targetRingPoints2 = new List<Pnt3D>
					                        {
						                        new Pnt3D(50, 40, 0),
						                        new Pnt3D(40, 40, 0),
						                        new Pnt3D(40, 60, 0),
						                        new Pnt3D(50, 100, 0),
					                        };

					var target2 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints2, t)));

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target2, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					// Compare with difference:
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target2, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

					union =
						GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target2, poly1, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target2, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(target2.GetArea2D(), difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(target2.GetArea2D(), union.GetArea2D());
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceIslandTouchesOtherIsland()
		{
			// The source has two inner rings that touch each other in a vertex.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole 1:
			var interiorRing1Points = new[]
			                          {
				                          new Pnt3D(20, 80, 0),
				                          new Pnt3D(20, 40, 0),
				                          new Pnt3D(50, 40, 0),
				                          new Pnt3D(50, 80, 0)
			                          }.ToList();

			// Hole 2:
			var interiorRing2Points = new[]
			                          {
				                          new Pnt3D(70, 10, 0),
				                          new Pnt3D(70, 40, 0),
				                          new Pnt3D(50, 40, 0),
				                          new Pnt3D(50, 10, 0)
			                          }.ToList();

			for (var i = 0; i < 5; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

				var interiorRing1 =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRing1Points, i));
				var interiorRing2 =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRing2Points, -i));

				poly1.AddInteriorRing(interiorRing1);
				poly1.AddInteriorRing(interiorRing2);

				for (var t = 0; t < 5; t++)
				{
					// The target touches the island including the touching point in a line:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(20, 40, 0),
						                       new Pnt3D(50, 40, 0),
						                       new Pnt3D(30, 20, 9),
						                       new Pnt3D(20, 20, 9),
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring result =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
					Assert.IsFalse(result.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), result.GetArea2D());
					Assert.AreEqual(target.GetLength2D(), result.GetLength2D());

					//
					// Compare with difference:
					result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
					Assert.AreEqual(3, result.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), result.GetArea2D());

					// Vice versa to check symmetry:
					result =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(result.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), result.GetArea2D());

					result = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.IsTrue(result.IsEmpty);

					// Now the target touches also the source outer ring in a line
					// -> the result has one less inner ring:
					var targetRingPoints2 = new List<Pnt3D>
					                        {
						                        new Pnt3D(0, 40, 0),
						                        new Pnt3D(50, 40, 0),
						                        new Pnt3D(30, 20, 9),
						                        new Pnt3D(0, 20, 9),
					                        };

					var target2 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints2, t)));

					MultiLinestring result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target2, tolerance);
					Assert.IsFalse(result2.IsEmpty);
					Assert.AreEqual(1, result2.PartCount);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());
					Assert.AreEqual(target2.GetLength2D(), result2.GetLength2D());

					//
					// Compare with difference:
					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target2, tolerance);
					Assert.AreEqual(2, result2.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target2.GetArea2D(), result2.GetArea2D());

					// Vice versa to check symmetry:
					result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target2, poly1, tolerance);
					Assert.IsFalse(result2.IsEmpty);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());
					Assert.AreEqual(target2.GetLength2D(), result2.GetLength2D());

					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(target2, poly1, tolerance);
					Assert.IsTrue(result2.IsEmpty);

					// Now the target touches both touching rings in a line (including the touch point)
					// -> the result has one less inner ring:
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(20, 40, 0),
						                        new Pnt3D(50, 40, 0),
						                        new Pnt3D(50, 20, 9),
						                        new Pnt3D(20, 20, 9),
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					MultiLinestring result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target3, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(1, result3.PartCount);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					//
					// Compare with difference:
					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target3, tolerance);
					Assert.AreEqual(2, result3.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target3.GetArea2D(), result3.GetArea2D());

					// Vice versa to check symmetry:
					result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, poly1, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(target3, poly1, tolerance);
					Assert.IsTrue(result3.IsEmpty);
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceIslandTouchesOtherIslandTwice()
		{
			// The source has two inner L-shaped rings touching each other and hence contain an 'inside' part
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole 1 (upper left):
			var interiorRing1Points = new[]
			                          {
				                          new Pnt3D(20, 80, 0),
				                          new Pnt3D(20, 10, 0),
				                          new Pnt3D(60, 10, 0),
				                          new Pnt3D(60, 20, 0),
				                          new Pnt3D(40, 20, 0),
				                          new Pnt3D(40, 80, 0)
			                          }.ToList();

			// Hole 2 (lower right):
			var interiorRing2Points = new[]
			                          {
				                          new Pnt3D(70, 20, 0),
				                          new Pnt3D(70, 90, 0),
				                          new Pnt3D(40, 90, 0),
				                          new Pnt3D(40, 80, 0),
				                          new Pnt3D(60, 80, 0),
				                          new Pnt3D(60, 20, 0),
			                          }.ToList();

			for (var i = 0; i < 5; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

				var interiorRing1 =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRing1Points, i));
				var interiorRing2 =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRing2Points, -i));

				poly1.AddInteriorRing(interiorRing1);
				poly1.AddInteriorRing(interiorRing2);

				for (var t = 0; t < 5; t++)
				{
					// The target touches both islands in a line including a touching point:
					// -> only one island in result
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(40, 80, 0),
						                       new Pnt3D(50, 80, 0),
						                       new Pnt3D(50, 50, 9),
						                       new Pnt3D(40, 50, 9)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target.GetLength2D(), intersection.GetLength2D());

					//
					// Compare with difference:
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), difference.GetArea2D());

					var union =
						GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(3, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					// Now the target completely fills the area between the islands
					// -> only one island in result
					var targetRingPoints2 = new List<Pnt3D>
					                        {
						                        new Pnt3D(40, 80, 0),
						                        new Pnt3D(60, 80, 0),
						                        new Pnt3D(60, 20, 9),
						                        new Pnt3D(40, 20, 9)
					                        };

					var target2 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints2, t)));

					MultiLinestring result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target2, tolerance);
					Assert.IsFalse(result2.IsEmpty);
					Assert.AreEqual(1, result2.PartCount);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());
					Assert.AreEqual(target2.GetLength2D(), result2.GetLength2D());

					//
					// Compare with difference:
					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target2, tolerance);
					Assert.AreEqual(2, result2.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target2.GetArea2D(), result2.GetArea2D());

					// Vice versa to check symmetry:
					result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target2, poly1, tolerance);
					Assert.IsFalse(result2.IsEmpty);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());
					Assert.AreEqual(target2.GetLength2D(), result2.GetLength2D());

					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(target2, poly1, tolerance);
					Assert.IsTrue(result2.IsEmpty);

					// Now the target touches both touching rings in a touch point only
					// -> the difference has one more inner ring:
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(40, 80, 0),
						                        new Pnt3D(55, 70, 0),
						                        new Pnt3D(55, 30, 9),
						                        new Pnt3D(45, 30, 9)
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					MultiLinestring result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target3, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(1, result3.PartCount);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					//
					// Compare with difference:
					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target3, tolerance);
					Assert.AreEqual(4, result3.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target3.GetArea2D(), result3.GetArea2D());

					// Vice versa to check symmetry:
					result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, poly1, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(target3, poly1, tolerance);
					Assert.IsTrue(result3.IsEmpty);
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceIslandTouchesOtherIslandTwiceInsideOtherPart()
		{
			// Same as before, but the setup is inside an island of a bigger part
			var ring0 = new List<Pnt3D>
			            {
				            new Pnt3D(-200, -200, 9),
				            new Pnt3D(-200, 200, 9),
				            new Pnt3D(200, 200, 9),
				            new Pnt3D(200, -200, 9)
			            };

			var ring0Island = new List<Pnt3D>
			                  {
				                  new Pnt3D(-150, -150, 9),
				                  new Pnt3D(150, -150, 9),
				                  new Pnt3D(150, 150, 9),
				                  new Pnt3D(-150, 150, 9)
			                  };

			// The source has two inner L-shaped rings touching each other and hence contain an 'inside' part
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			// Hole 1 (upper left):
			var interiorRing1Points = new[]
			                          {
				                          new Pnt3D(20, 80, 0),
				                          new Pnt3D(20, 10, 0),
				                          new Pnt3D(60, 10, 0),
				                          new Pnt3D(60, 20, 0),
				                          new Pnt3D(40, 20, 0),
				                          new Pnt3D(40, 80, 0)
			                          }.ToList();

			// Hole 2 (lower right):
			var interiorRing2Points = new[]
			                          {
				                          new Pnt3D(70, 20, 0),
				                          new Pnt3D(70, 90, 0),
				                          new Pnt3D(40, 90, 0),
				                          new Pnt3D(40, 80, 0),
				                          new Pnt3D(60, 80, 0),
				                          new Pnt3D(60, 20, 0),
			                          }.ToList();

			for (var i = 0; i < 5; i++)
			{
				var poly0 = GeomTestUtils.CreatePoly(ring0);
				poly0.AddInteriorRing(GeomTestUtils.CreateRing(ring0Island));

				RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

				var interiorRing1 =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRing1Points, i));
				var interiorRing2 =
					new Linestring(GeomTestUtils.GetRotatedRing(interiorRing2Points, -i));

				poly1.AddInteriorRing(interiorRing1);
				poly1.AddInteriorRing(interiorRing2);

				MultiPolycurve multiPoly = new MultiPolycurve(new List<MultiLinestring>
				                                              {
					                                              poly0, poly1
				                                              });

				for (var t = 0; t < 5; t++)
				{
					// The target touches both islands in a line including a touching point:
					// -> only one island in result
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(40, 80, 0),
						                       new Pnt3D(50, 80, 0),
						                       new Pnt3D(50, 50, 9),
						                       new Pnt3D(40, 50, 9)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(multiPoly, target, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target.GetLength2D(), intersection.GetLength2D());

					//
					// Compare with difference:
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(multiPoly, target, tolerance);
					Assert.AreEqual(4, difference.PartCount);
					Assert.AreEqual(multiPoly.GetArea2D() - target.GetArea2D(),
					                difference.GetArea2D());

					// TODO: The structure of the result is different (ring ordering etc.)
					var union =
						GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(5, union.PartCount);
					Assert.AreEqual(multiPoly.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, multiPoly, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, multiPoly, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					// Now the target completely fills the area between the islands
					// -> only one island in result
					var targetRingPoints2 = new List<Pnt3D>
					                        {
						                        new Pnt3D(40, 80, 0),
						                        new Pnt3D(60, 80, 0),
						                        new Pnt3D(60, 20, 9),
						                        new Pnt3D(40, 20, 9)
					                        };

					var target2 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints2, t)));

					MultiLinestring result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(multiPoly, target2, tolerance);
					Assert.IsFalse(result2.IsEmpty);
					Assert.AreEqual(1, result2.PartCount);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());
					Assert.AreEqual(target2.GetLength2D(), result2.GetLength2D());

					//
					// Compare with difference:
					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(multiPoly, target2, tolerance);
					Assert.AreEqual(4, result2.PartCount);
					Assert.AreEqual(multiPoly.GetArea2D() - target2.GetArea2D(),
					                result2.GetArea2D());

					// Vice versa to check symmetry:
					result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target2, multiPoly, tolerance);
					Assert.IsFalse(result2.IsEmpty);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());
					Assert.AreEqual(target2.GetLength2D(), result2.GetLength2D());

					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(target2, multiPoly, tolerance);
					Assert.IsTrue(result2.IsEmpty);

					// Now the target touches both touching rings in a touch point only
					// -> the difference has one more inner ring:
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(40, 80, 0),
						                        new Pnt3D(55, 70, 0),
						                        new Pnt3D(55, 30, 9),
						                        new Pnt3D(45, 30, 9)
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					MultiLinestring result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(multiPoly, target3, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(1, result3.PartCount);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					//
					// Compare with difference:
					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(multiPoly, target3, tolerance);
					Assert.AreEqual(6, result3.PartCount);
					Assert.AreEqual(multiPoly.GetArea2D() - target3.GetArea2D(),
					                result3.GetArea2D());

					// Vice versa to check symmetry:
					result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, multiPoly, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(target3, multiPoly, tolerance);
					Assert.IsTrue(result3.IsEmpty);
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceHasBoundaryLoop()
		{
			// The source has an esri-style boundary loop
			// The target touches the boundary loop point.
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

			for (var i = 0; i < 9; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				for (var t = 0; t < 5; t++)
				{
					// The target touches the 'island' including the touching point (from the inside) in a line:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(50, 100, 9),
						                       new Pnt3D(80, 80, 9),
						                       new Pnt3D(80, 40, 9),
						                       new Pnt3D(50, 40, 9),
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target.GetLength2D(), intersection.GetLength2D());

					// Currently the boundary loop remains a boundary loop also in the result (esri style)
					//
					// Compare with difference:
					MultiLinestring difference =
						GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), difference.GetArea2D());

					// Union again:
					MultiLinestring union =
						GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), intersection.GetArea2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					union =
						GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(target.GetArea2D(), union.GetArea2D());

					// 
					// Now the target touches also the source outer ring in a line
					// -> the result has no inner ring any more:
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(50, 100, 9),
						                        new Pnt3D(80, 100, 9),
						                        new Pnt3D(80, 40, 9),
						                        new Pnt3D(50, 40, 9)
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target3, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target3.GetArea2D(), intersection.GetArea2D(), 0.0001);
					Assert.AreEqual(target3.GetLength2D(), intersection.GetLength2D());

					//
					// Compare with difference:
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target3, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(560, difference.GetLength2D());
					Assert.AreEqual(poly1.GetArea2D() - target3.GetArea2D(),
					                difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);

					// Currently, the original boundary loop turns into an island. This could theoretically be
					// detected and adjusted.
					Assert.AreEqual(2, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, poly1, tolerance);
					Assert.IsFalse(intersection.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), intersection.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), intersection.GetLength2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target3, poly1, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					//
					// Now the target touches the source 'island' from inside the island (i.e. outside the polygon)
					//
					var targetRingPoints2 = new List<Pnt3D>
					                        {
						                        new Pnt3D(50, 40, 0),
						                        new Pnt3D(40, 40, 0),
						                        new Pnt3D(40, 60, 0),
						                        new Pnt3D(50, 100, 0),
					                        };

					var target2 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints2, t)));

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target2, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					// Compare with difference:
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target2, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target2, poly1, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target2, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(target2.GetArea2D(), difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(target2.GetArea2D(), union.GetArea2D());

					//
					// Now the target touches both legs of the boundary loop from inside the 'island' (i.e. outside the polygon)
					//
					var targetRingPoints4 = new List<Pnt3D>
					                        {
						                        new Pnt3D(50, 60, 0),
						                        new Pnt3D(20, 60, 0),
						                        new Pnt3D(50, 100, 0),
					                        };

					var target4 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints4, t)));

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target4, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					// Compare with difference:
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target4, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D());

					// Vice versa to check symmetry:
					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target4, poly1, tolerance);
					Assert.IsTrue(intersection.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target4, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(target4.GetArea2D(), difference.GetArea2D());

					union = GeomTopoOpUtils.GetUnionAreasXY(intersection, difference, tolerance);
					Assert.AreEqual(1, union.PartCount);
					Assert.AreEqual(target4.GetArea2D(), union.GetArea2D());

					// Union the original inputs first:
					union = GeomTopoOpUtils.GetUnionAreasXY(poly1, target4, tolerance);
					Assert.AreEqual(2, union.PartCount);
					Assert.AreEqual(poly1.GetArea2D() + target4.GetArea2D(), union.GetArea2D());

					// .. and back
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(union, target4, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D());

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(target4, union, tolerance);
					Assert.IsTrue(difference.IsEmpty);

					difference = GeomTopoOpUtils.GetDifferenceAreasXY(union, poly1, tolerance);
					Assert.AreEqual(1, difference.PartCount);
					Assert.AreEqual(union.GetArea2D() - poly1.GetArea2D(), difference.GetArea2D(),
					                0.0001);

					// Currently the island-touching-outer ring (ogc style, 2 rings) is favored:
					difference = GeomTopoOpUtils.GetDifferenceAreasXY(union, target4, tolerance);
					Assert.AreEqual(2, difference.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), difference.GetArea2D(), 0.0001);

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(union, target4, tolerance);
					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target4.GetArea2D(), intersection.GetArea2D(), 0.0001);

					intersection =
						GeomTopoOpUtils.GetIntersectionAreasXY(target4, union, tolerance);
					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(target4.GetArea2D(), intersection.GetArea2D(), 0.0001);

					intersection = GeomTopoOpUtils.GetIntersectionAreasXY(union, poly1, tolerance);
					Assert.AreEqual(1, intersection.PartCount);
					Assert.AreEqual(poly1.GetArea2D(), intersection.GetArea2D(), 0.0001);
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceHasBoundaryLoopToOutside()
		{
			// The source has a boundary loop (relative to the tolerance) to the outside (i.e. it
			// is non-simple). The target touches the boundary loop point.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(20, 140, 0),
				            new Pnt3D(20, 160, 0),
				            new Pnt3D(50, 160, 0),
				            new Pnt3D(50, 100, 0),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			for (var i = 0; i < 9; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				for (var t = 0; t < 5; t++)
				{
					// The target touches the 'loop point' in a vertex:
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(50, 100, 9),
						                       new Pnt3D(80, 80, 9),
						                       new Pnt3D(80, 40, 9),
						                       new Pnt3D(50, 40, 9),
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring result =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
					Assert.IsFalse(result.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), result.GetArea2D());
					Assert.AreEqual(target.GetLength2D(), result.GetLength2D());

					// Currently the boundary loop remains a boundary loop also in the result (esri style)
					//
					// Compare with difference:
					result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
					Assert.AreEqual(3, result.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), result.GetArea2D(),
					                0.2);

					// Vice versa to check symmetry:
					result =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(result.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), result.GetArea2D());

					result = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.IsTrue(result.IsEmpty);

					// Now the target touches also the source outer ring in a line
					// -> the result has no inner ring any more:
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(50, 100, 9),
						                        new Pnt3D(80, 100, 9),
						                        new Pnt3D(80, 40, 9),
						                        new Pnt3D(50, 40, 9)
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					MultiLinestring result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target3, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(1, result3.PartCount);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D(), 0.0001);
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					//
					// Compare with difference:
					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target3, tolerance);
					Assert.AreEqual(2, result3.PartCount);

					Assert.AreEqual(poly1.GetArea2D() - target3.GetArea2D(), result3.GetArea2D(),
					                0.4);

					// Vice versa to check symmetry:
					result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, poly1, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(target3, poly1, tolerance);
					Assert.IsTrue(result3.IsEmpty);

					//
					// Now the target touches the boundary loop in a linear intersection from inside the main part
					//
					var targetRingPoints2 = new List<Pnt3D>
					                        {
						                        new Pnt3D(30, 100, 9),
						                        new Pnt3D(80, 100, 9),
						                        new Pnt3D(80, 40, 9),
						                        new Pnt3D(50, 40, 9)
					                        };

					var target2 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints2, t)));

					var result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target2, tolerance);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D(), 0.0001);

					// Compare with difference:
					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target2, tolerance);
					Assert.AreEqual(2, result2.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target2.GetArea2D(), result2.GetArea2D());

					// Vice versa to check symmetry:
					result2 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target2, poly1, tolerance);
					Assert.AreEqual(target2.GetArea2D(), result2.GetArea2D());

					result2 = GeomTopoOpUtils.GetDifferenceAreasXY(target2, poly1, tolerance);
					Assert.IsTrue(result2.IsEmpty);

					//
					// Now the target completely fills the main 'part' (the large boundary loop)
					var targetRingPoints4 = new List<Pnt3D>
					                        {
						                        new Pnt3D(0, 0, 9),
						                        new Pnt3D(0, 100, 9),
						                        new Pnt3D(100, 100, 9),
						                        new Pnt3D(100, 0, 9)
					                        };

					var target4 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints4, t)));

					var result4 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target4, tolerance);
					Assert.AreEqual(target4.GetArea2D(), result4.GetArea2D(), 0.0001);

					// Compare with difference:
					result4 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target4, tolerance);
					Assert.AreEqual(1, result4.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target4.GetArea2D(), result4.GetArea2D(),
					                0.0001);

					// Vice versa to check symmetry:
					result4 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target4, poly1, tolerance);
					Assert.AreEqual(target4.GetArea2D(), result4.GetArea2D());

					result4 = GeomTopoOpUtils.GetDifferenceAreasXY(target4, poly1, tolerance);
					Assert.IsTrue(result4.IsEmpty);
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYSourceHasDegenerateBoundaryLoopToOutside_Top5526()
		{
			// The source has a pretty degenerate boundary loop to the outside.
			// The target covers everything except the boundary loop.
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(150, 100, 0),
				            new Pnt3D(125, 99.98, 0),
				            new Pnt3D(100, 100, 0),
				            new Pnt3D(100, 0, 9)
			            };

			const double tolerance = 0.01;

			for (var i = 0; i < 9; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				for (var t = 0; t < 5; t++)
				{
					var targetRingPoints = new List<Pnt3D>
					                       {
						                       new Pnt3D(0, 0, 9),
						                       new Pnt3D(0, 100, 9),
						                       new Pnt3D(100, 100, 0),
						                       new Pnt3D(100, 0, 9)
					                       };

					var target =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints, t)));

					MultiLinestring result =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target, tolerance);
					Assert.IsFalse(result.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), result.GetArea2D(), 0.0001);
					Assert.AreEqual(target.GetLength2D(), result.GetLength2D(), 0.0001);

					// Compare with difference:
					result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target, tolerance);
					Assert.AreEqual(1, result.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target.GetArea2D(), result.GetArea2D(),
					                0.0001);

					// Vice versa to check symmetry:
					result =
						GeomTopoOpUtils.GetIntersectionAreasXY(target, poly1, tolerance);
					Assert.IsFalse(result.IsEmpty);
					Assert.AreEqual(target.GetArea2D(), result.GetArea2D());

					result = GeomTopoOpUtils.GetDifferenceAreasXY(target, poly1, tolerance);
					Assert.IsTrue(result.IsEmpty);

					// Now the source exceeds the target on the southern side
					var targetRingPoints3 = new List<Pnt3D>
					                        {
						                        new Pnt3D(0, 50, 9),
						                        new Pnt3D(0, 100, 9),
						                        new Pnt3D(100, 100, 0),
						                        new Pnt3D(100, 50, 9)
					                        };

					var target3 =
						new RingGroup(
							new Linestring(GeomTestUtils.GetRotatedRing(targetRingPoints3, t)));

					MultiLinestring result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(poly1, target3, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(1, result3.PartCount);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D(), 0.0001);

					//
					// Compare with difference:
					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, target3, tolerance);
					Assert.AreEqual(2, result3.PartCount);
					Assert.AreEqual(poly1.GetArea2D() - target3.GetArea2D(), result3.GetArea2D(),
					                0.4);

					// Vice versa to check symmetry:
					result3 =
						GeomTopoOpUtils.GetIntersectionAreasXY(target3, poly1, tolerance);
					Assert.IsFalse(result3.IsEmpty);
					Assert.AreEqual(target3.GetArea2D(), result3.GetArea2D());
					Assert.AreEqual(target3.GetLength2D(), result3.GetLength2D());

					result3 = GeomTopoOpUtils.GetDifferenceAreasXY(target3, poly1, tolerance);
					Assert.IsTrue(result3.IsEmpty);
				}
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYWithMultipleShortSegmentsAtMultipartTouchPoints()
		{
			// Zero-length segments:
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 50, 0),
				            new Pnt3D(100, 50, 0)
			            };

			var ring2 = new[]
			            {
				            new Pnt3D(50, 50, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 0),
				            //new Pnt3D(200, 0, 0)
			            }.ToList();

			for (int i = 0; i < 6; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				const double tolerance = 0.01;

				RingGroup poly2 = GeomTestUtils.CreatePoly(ring2);

				MultiLinestring result =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(1666.6666, result.GetArea2D(), 0.001);

				result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, poly2, tolerance);

				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(5833.3333, result.GetArea2D(), 0.001);
			}

			// Now with short but non-zero segments 
			ring1[3].X += 0.002;
			ring1[3].Y -= 0.002;

			ring1[5].X += 0.002;
			ring1[5].Y -= 0.002;

			for (int i = 0; i < 6; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				const double tolerance = 0.01;

				RingGroup poly2 = GeomTestUtils.CreatePoly(ring2);

				MultiLinestring result =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(1666.6666, result.GetArea2D(), 0.3);

				result = GeomTopoOpUtils.GetDifferenceAreasXY(poly1, poly2, tolerance);

				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(5833.3333, result.GetArea2D(), 0.3);
			}
		}

		[Test]
		public void CanGetUnionAreaXYWithMultipleShortSegmentsAtMultipartTouchPoints()
		{
			// Zero-length segments:
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 50, 0),
				            new Pnt3D(100, 50, 0)
			            };

			var ring2 = new[]
			            {
				            new Pnt3D(100, 0, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(200, 100, 0),
				            new Pnt3D(200, 0, 0)
			            }.ToList();

			for (int i = 0; i < 6; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				const double tolerance = 0.01;

				RingGroup poly2 = GeomTestUtils.CreatePoly(ring2);

				MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(poly1, poly2, tolerance);

				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(poly1.GetArea2D() + poly2.GetArea2D(), result.GetArea2D());

				// with swapped arguments
				result = GeomTopoOpUtils.GetUnionAreasXY(poly2, poly1, tolerance);

				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(poly1.GetArea2D() + poly2.GetArea2D(), result.GetArea2D());
			}

			// Now with short but non-zero segments 
			ring1[3].X += 0.002;
			ring1[3].Y -= 0.002;

			ring1[5].X += 0.002;
			ring1[5].Y -= 0.002;

			for (int i = 0; i < 6; i++)
			{
				RingGroup poly1 = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring1, i));

				const double tolerance = 0.01;

				RingGroup poly2 = GeomTestUtils.CreatePoly(ring2);

				MultiLinestring result = GeomTopoOpUtils.GetUnionAreasXY(poly1, poly2, tolerance);

				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(poly1.GetArea2D() + poly2.GetArea2D(), result.GetArea2D(), 0.5);

				// with swapped arguments
				result = GeomTopoOpUtils.GetUnionAreasXY(poly2, poly1, tolerance);

				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(poly1.GetArea2D() + poly2.GetArea2D(), result.GetArea2D(), 0.5);
			}
		}

		#endregion

		[Test]
		public void CanGetIntersectionAreaXYWithLinearBoundaryIntersection()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(100, 0, 9),
				            new Pnt3D(-10, 0, 9),
				            new Pnt3D(-10, 75, 9),
				            new Pnt3D(0, 75, 9),
				            new Pnt3D(0, 100, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

			const double tolerance = 0.01;

			var ring2 = new[]
			            {
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(200, 100, 9),
				            new Pnt3D(200, 50, 0),
				            //new Pnt3D(50, 50, 0),
				            new Pnt3D(0, 50, 0)
			            }.ToList();

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				RingGroup poly2 = GeomTestUtils.CreatePoly(rotatedRing);

				MultiLinestring result =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

				Assert.IsFalse(result.IsEmpty);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(100 * 100 / 2, result.GetArea2D());
			}

			// Now with the ring2 slightly (below tolerance) off 
			ring2[3].X += 0.0002;

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				RingGroup poly2 = GeomTestUtils.CreatePoly(rotatedRing);

				MultiLinestring result =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

				Assert.IsFalse(result.IsEmpty);
				Assert.AreEqual(1, result.PartCount);
				Assert.AreEqual(100 * 100 / 2d, result.GetArea2D(), 0.015);
			}
		}

		[Test]
		public void CanGetIntersectionAreaXYWithLinearIntersectionWithinToleranceAcuteAngle()
		{
			// The question in this test is what the correct result would be:
			// - Always the vertex of the target (200, 0) which is unique and always within the
			//   tolerance of the two intersection points. The result would be more symmetric
			//   but it's a deviation from preferring the source locations.
			// - Use the point that is visited first in the list and skip the other which means
			//   we're likely using the source vertex which can be at a distance > tolerance to the
			//   intersection points and certainly at a distance > tolerance to the target point.
			// Currently the latter method is used. Extra (phantom) vertices remain but should at
			// some point be removable anyway as this is an orthogonal concern.
			const double tolerance = 0.01;

			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(200.011, -0.01, 9),
				            new Pnt3D(-10, 0, 9),
				            new Pnt3D(-10, 75, 9),
				            new Pnt3D(0, 75, 9),
				            new Pnt3D(0, 100, 9)
			            };

			RingGroup poly1 = GeomTestUtils.CreatePoly(ring1);

			var ring2 = new[]
			            {
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 100, 9),
				            new Pnt3D(200, 0, 0),
				            new Pnt3D(0, 0, 0)
			            }.ToList();

			var expectedResult = new Linestring(
				new[]
				{
					new Pnt3D(0, 100, 9),
					new Pnt3D(100, 100, 9),
					new Pnt3D(200.011, -0.01, 0),
					new Pnt3D(0, 0, 0)
				});

			for (var i = 0; i < ring1.Count; i++)
			{
				Pnt3D[] array2 = ring2.ToArray();
				CollectionUtils.Rotate(array2, i);
				var rotatedRing = new List<Pnt3D>(array2);

				RingGroup poly2 = GeomTestUtils.CreatePoly(rotatedRing);

				MultiLinestring intersecion =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

				// NOTE: By declaring the two close by acute angle intersections as pseudo-break
				// not only the source is used and but additionally, no short segment is created
				// any more -> 5 result segments
				Assert.IsFalse(intersecion.IsEmpty);
				Assert.AreEqual(1, intersecion.PartCount);
				Assert.AreEqual(5, intersecion.SegmentCount);
				Console.WriteLine(intersecion.GetArea2D());

				Assert.IsTrue(
					GeomTopoOpUtils.AreEqualXY(expectedResult, intersecion.GetLinestring(0),
					                           0.0005));

				MultiLinestring difference =
					GeomTopoOpUtils.GetDifferenceAreasXY(poly1, poly2, tolerance);

				Assert.AreEqual(1, difference.PartCount);
				Assert.AreEqual(poly1.GetArea2D() - poly2.GetArea2D(), difference.GetArea2D(),
				                1.1);

				MultiLinestring union =
					GeomTopoOpUtils.GetUnionAreasXY(intersecion, difference, tolerance);

				Assert.AreEqual(1, union.PartCount);
				Assert.IsTrue(
					GeomTopoOpUtils.AreEqualXY(poly1.GetLinestring(0), union.GetLinestring(0),
					                           0.0005));

				// with flipped arguments:
				union = GeomTopoOpUtils.GetUnionAreasXY(difference, intersecion, tolerance);

				Assert.AreEqual(1, union.PartCount);

				// Equality finds the extra angle (should probably be accounted for), just compare the area for now:
				Assert.AreEqual(poly1.GetArea2D(), union.GetArea2D(), 0.001);

				// And vice-versa (the no deviation from poly2 which is now the source)
				intersecion =
					GeomTopoOpUtils.GetIntersectionAreasXY(poly2, poly1, tolerance);
				Assert.AreEqual(1, intersecion.PartCount);
				Assert.AreEqual(4, intersecion.SegmentCount);
				Console.WriteLine(intersecion.GetArea2D());

				Assert.IsTrue(
					GeomTopoOpUtils.AreEqualXY(poly2.GetLinestring(0), intersecion.GetLinestring(0),
					                           0.0001));

				difference =
					GeomTopoOpUtils.GetDifferenceAreasXY(poly2, poly1, tolerance);
				Assert.IsTrue(difference.IsEmpty);
			}
		}

		[Test]
		public void CanGetIntersectionAreaWithLinearIntersectionWithinToleranceAcuteAngleTop5502()
		{
			// Linear intersection is within the tolerance (1 mm)
			// However, the two vertices are just above the tolerance
			// The 'inner' vertex is within the tolerance of the line.
			// The situation is modeled in an even more pronounced way in unit test
			// CanGetIntersectionAreaXYWithLinearIntersectionWithinToleranceAcuteAngle()
			RingGroup ring1 = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"almost_linear_acute_intersection_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup ring2 = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath(
					"almost_linear_acute_intersection_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			var poly1 = new MultiPolycurve(ring1.GetLinestrings());
			var poly2 = new MultiPolycurve(ring2.GetLinestrings());
			const double tolerance = 0.001;

			MultiLinestring intersectionAreasXY =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

			Assert.IsFalse(intersectionAreasXY.IsEmpty);
			Assert.AreEqual(27.59559, intersectionAreasXY.GetArea2D(), tolerance);

			// In the new implementation the very close points get eliminated by pseudo-break
			// detection and ignoring of these intersection points during the subcurve navigation.
			Assert.AreEqual(5, intersectionAreasXY.PointCount);
		}

		[Test]
		public void CanGetDifferenceAreaWithLinearIntersectionWithVertexOnAcuteAngle()
		{
			// In this case the two wedges have a linear intersection in an acute angle.
			// One of them has a vertex > tolerance from the acute angle point but < tolerance
			// from the other segment. This is one of the classics where the acute angle would
			// collapse to a removable line in the simplify operation. However, in this case it
			// is better to ignore the intermediate linear intersection inside the larger
			// linear intersection stretch.

			RingGroup ring1 = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("wedge_without_vertex.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup ring2 = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("wedge_with_vertex.wkb"), out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			var poly1 = new MultiPolycurve(ring1.GetLinestrings());
			var poly2 = new MultiPolycurve(ring2.GetLinestrings());
			const double tolerance = 0.01;

			MultiLinestring differenceAreasXY =
				GeomTopoOpUtils.GetDifferenceAreasXY(poly1, poly2, tolerance);

			Assert.AreEqual(1, differenceAreasXY.PartCount);
			Assert.AreEqual(1.37566, differenceAreasXY.GetArea2D(), 0.001);
			Assert.AreEqual(5, differenceAreasXY.PointCount);

			MultiLinestring intersectionAreasXY =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, tolerance);

			Assert.AreEqual(1, intersectionAreasXY.PartCount);
			double expected = ring1.GetArea2D() - differenceAreasXY.GetArea2D();
			Assert.AreEqual(expected, intersectionAreasXY.GetArea2D(), 0.001);

			Assert.AreEqual(4, intersectionAreasXY.PointCount);

			// Make sure the southernmost tip is used in the result:
			Assert.AreEqual(1268417.929, intersectionAreasXY.YMin, 0.0001);
		}

		[Test]
		public void CanGetIntersectionAreaWithLinearIntersectionWithinTolerance()
		{
			// Linear intersection is within the tolerance (1 cm)
			RingGroup ring1 = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("almost_linear_intersection_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			RingGroup ring2 = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("almost_linear_intersection_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			var poly1 = new MultiPolycurve(ring1.GetLinestrings());
			var poly2 = new MultiPolycurve(ring2.GetLinestrings());
			MultiLinestring intersectionAreasXY =
				GeomTopoOpUtils.GetIntersectionAreasXY(poly1, poly2, 0.01);

			Assert.IsFalse(intersectionAreasXY.IsEmpty);
			Assert.AreEqual(poly1.GetArea2D(), intersectionAreasXY.GetArea2D());
		}

		#region 2D Line - Ring intersections

		[Test]
		public void CanGetRingIntersectionLinesXYLineAlongRing()
		{
			// Get the lines within a polygon and a line along & within a polygon:

			var path1 = new List<Pnt3D>
			            {
				            new Pnt3D(-5, 50, 2),
				            new Pnt3D(0, 50, 2),
				            new Pnt3D(200, 50, 2)
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			Linestring sourceLinestring = new Linestring(path1);

			RingGroup targetPoly = GeomTestUtils.CreatePoly(ring2);

			var intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					sourceLinestring, targetPoly, 0.001).ToList();

			Assert.AreEqual(1, intersectionLinesXY.Count);

			var expectedInterior = new Linestring(new[]
			                                      {
				                                      new Pnt3D(0, 50, 2),
				                                      new Pnt3D(100, 50, 2)
			                                      });

			Assert.IsTrue(intersectionLinesXY[0].Equals(expectedInterior));

			// Excluded target boundary line:
			var intersectionLinesOnlyWithin =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					sourceLinestring, targetPoly, 0.001, true).ToList();
			Assert.AreEqual(1, intersectionLinesOnlyWithin.Count);
			Assert.IsTrue(intersectionLinesOnlyWithin[0].Equals(expectedInterior));

			// With a stretch along the boundary:
			path1 = new List<Pnt3D>
			        {
				        new Pnt3D(-5, 50, 2),
				        new Pnt3D(0, 50, 2),
				        new Pnt3D(100, 50, 2),
				        new Pnt3D(100, 20, 2)
			        };

			sourceLinestring = new Linestring(path1);

			intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					sourceLinestring, targetPoly, 0.001).ToList();

			Assert.AreEqual(2, intersectionLinesXY.Count);

			Assert.IsTrue(intersectionLinesXY[0].Equals(expectedInterior));

			Assert.IsTrue(intersectionLinesXY[1].Equals(
				              new Linestring(new[]
				                             {
					                             new Pnt3D(100, 50, 2),
					                             new Pnt3D(100, 20, 2)
				                             })));

			// Excluded target boundary line:
			intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					sourceLinestring, targetPoly, 0.001, true).ToList();

			Assert.AreEqual(1, intersectionLinesXY.Count);
			Assert.IsTrue(intersectionLinesXY[0].Equals(expectedInterior));
		}

		[Test]
		public void CanGetRingIntersectionLinesXYMultipart()
		{
			// Get the lines within a polygon and a line along & within a polygon:

			var sourcePath1 = new List<Pnt3D>
			                  {
				                  new Pnt3D(-5, 50, 2),
				                  new Pnt3D(0, 50, 2),
				                  new Pnt3D(200, 50, 2)
			                  };

			var sourcePath2 = new List<Pnt3D>
			                  {
				                  new Pnt3D(20, 150, 2),
				                  new Pnt3D(20, 70, 2),
				                  new Pnt3D(200, 70, 2)
			                  };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			MultiPolycurve sourceLinestrings = new MultiPolycurve(
				new[]
				{
					new Linestring(sourcePath1),
					new Linestring(sourcePath2)
				});

			RingGroup targetPoly = GeomTestUtils.CreatePoly(ring2);

			var intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					sourceLinestrings, targetPoly, 0.001).ToList();

			Assert.AreEqual(2, intersectionLinesXY.Count);

			var expectedInterior1 = new Linestring(new[]
			                                       {
				                                       new Pnt3D(0, 50, 2),
				                                       new Pnt3D(100, 50, 2)
			                                       });

			Assert.IsTrue(intersectionLinesXY[0].Equals(expectedInterior1));

			var expectedInterior2 = new Linestring(new[]
			                                       {
				                                       new Pnt3D(20, 90, 2),
				                                       new Pnt3D(20, 70, 2),
				                                       new Pnt3D(60, 70, 2)
			                                       });

			Assert.IsTrue(intersectionLinesXY[1].Equals(expectedInterior2));

			// Excluded target boundary line:
			var intersectionLinesOnlyWithin =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					sourceLinestrings, targetPoly, 0.001, true).ToList();
			Assert.AreEqual(2, intersectionLinesOnlyWithin.Count);
			Assert.IsTrue(intersectionLinesOnlyWithin[0].Equals(expectedInterior1));
			Assert.IsTrue(intersectionLinesOnlyWithin[1].Equals(expectedInterior2));
		}

		[Test]
		public void CanGetRingIntersectionLinesXYAlmostAlongRing()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0)
			           };

			var startingAlong = new List<Pnt3D>
			                    {
				                    new Pnt3D(100.001, 99.999, 0),
				                    //new Pnt3D(80, 100, 0),
				                    new Pnt3D(-50, 100, 0)
			                    };

			for (var i = 0; i < 5; i++)
			{
				RingGroup poly = GeomTestUtils.CreatePoly(GeomTestUtils.GetRotatedRing(ring, i));

				Linestring sourceLinestring = new Linestring(startingAlong);

				var intersectionLinesXY =
					GeomTopoOpUtils.GetRingIntersectionLinesXY(
						sourceLinestring, poly, 0.001).ToList();

				Assert.AreEqual(1, intersectionLinesXY.Count);

				var expected = new Line3D(
					new Pnt3D(-0, 100, 0),
					new Pnt3D(100, 100, 0));

				Assert.IsTrue(intersectionLinesXY[0].Segments[0].EqualsXY(expected, 0.001));

				// Excluded target boundary line:
				intersectionLinesXY =
					GeomTopoOpUtils.GetRingIntersectionLinesXY(
						sourceLinestring, poly, 0.001, true).ToList();

				Assert.AreEqual(0, intersectionLinesXY.Count);
			}
		}

		[Test]
		public void CanGetRingIntersectionLinesXYLineWithinRing()
		{
			// Get the lines completely within a polygon:
			var path1 = new List<Pnt3D>
			            {
				            new Pnt3D(5, 50, 2),
				            new Pnt3D(70, 50, 2)
			            };

			var ring2 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 9),
				            new Pnt3D(100, 50, 9),
				            new Pnt3D(100, 20, 9)
			            };

			Linestring containedSource = new Linestring(path1);

			RingGroup targetPoly = GeomTestUtils.CreatePoly(ring2);

			var intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					containedSource, targetPoly, 0.001).ToList();

			Assert.AreEqual(1, intersectionLinesXY.Count);

			Assert.IsTrue(intersectionLinesXY[0].Equals(containedSource));

			// Starting from the inside with touch:
			path1 = new List<Pnt3D>
			        {
				        new Pnt3D(50, 50, 2),
				        new Pnt3D(100, 40, 2),
			        };

			containedSource = new Linestring(path1);

			intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					containedSource, targetPoly, 0.001).ToList();

			Assert.AreEqual(1, intersectionLinesXY.Count);
			Assert.IsTrue(intersectionLinesXY[0].Equals(containedSource));

			// Reversed:
			containedSource.ReverseOrientation();
			intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					containedSource, targetPoly, 0.001).ToList();
			Assert.AreEqual(1, intersectionLinesXY.Count);
			Assert.IsTrue(intersectionLinesXY[0].Equals(containedSource));

			// Partially along the boundary:
			path1.Add(new Pnt3D(100, 0, 2));
			var containedAndAlongBoundary = new Linestring(path1);

			intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					containedAndAlongBoundary, targetPoly, 0.001).ToList();

			Assert.AreEqual(2, intersectionLinesXY.Count);
			containedSource.ReverseOrientation();
			Assert.IsTrue(intersectionLinesXY[0].Equals(containedSource));
			Assert.IsTrue(intersectionLinesXY[1].Equals(new Linestring(new[]
				                                            {
					                                            new Pnt3D(100, 40, 2),
					                                            new Pnt3D(100, 20, 2)
				                                            })));

			// Excluded target boundary line:
			intersectionLinesXY =
				GeomTopoOpUtils.GetRingIntersectionLinesXY(
					containedAndAlongBoundary, targetPoly, 0.001, true).ToList();

			Assert.AreEqual(1, intersectionLinesXY.Count);
			Assert.IsTrue(intersectionLinesXY[0].Equals(containedSource));
		}

		#endregion

		#region 3D ring intersection

		[Test]
		public void Can3dIntersectOverlappingRings()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: Sloping southwards
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 120, 30));
			ring1.Add(new Pnt3D(100, 120, 30));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: sloping northwards, plane intersection line along the x-axis on y=100
			ring2.Add(new Pnt3D(50, 80, 30));
			ring2.Add(new Pnt3D(50, 200, 0));
			ring2.Add(new Pnt3D(130, 200, 0));
			ring2.Add(new Pnt3D(130, 80, 30));
			ring2.Add(new Pnt3D(50, 80, 30));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring1, ring2);
			Assert.AreEqual(1, intersectionLines3D.Count);
			Linestring expected =
				new Linestring(new[]
				               {
					               new Line3D(new Pnt3D(50, 100, 25),
					                          new Pnt3D(100, 100, 25))
				               });

			Assert.IsTrue(expected.Equals(intersectionLines3D[0].Segments));

			// Now do the actual intersection:
			var patch1 = new Polyhedron(new[] { new RingGroup(new Linestring(ring1)) });
			var patch2 = new Polyhedron(new[] { new RingGroup(new Linestring(ring2)) });

			const double xyTolerance = 0.001;

			IList<RingGroup> intersection =
				GeomTopoOpUtils.GetIntersectionAreas3D(patch1, patch2, xyTolerance);

			Assert.AreEqual(2, intersection.Count);

			MultiLinestring mergedIntersections =
				GeomTopoOpUtils.GetUnionAreasXY(intersection[0], intersection[1], xyTolerance);

			Assert.AreEqual(2000, mergedIntersections.GetArea2D(), 0.0001);

			Assert.IsTrue(ChangeZUtils.AreCoplanar(mergedIntersections.GetPoints().ToList(), 0.0001,
			                                       out double maxDeviation, out string message));

			Assert.IsTrue(maxDeviation < xyTolerance);

			var xyIntersection =
				GeomTopoOpUtils.GetIntersectionAreasXY(patch1.RingGroups[0], patch2.RingGroups[0],
				                                       xyTolerance);

			Assert.AreEqual(xyIntersection.GetArea2D(), mergedIntersections.GetArea2D(), .001);

			MultiLinestring diff1 =
				GeomTopoOpUtils.GetDifferenceAreasXY(patch1, patch2, xyTolerance);

			Assert.AreEqual(patch1.GetArea2D(), diff1.GetArea2D() + xyIntersection.GetArea2D(),
			                0.001);
		}

		[Test]
		public void Can3DIntersectRings()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal (proper orientation for a roof):
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			//ring1.Reverse();

			// ring 2: vertical, smaller triangle
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(10, 10, -10));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring1, ring2);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(1, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);

			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);
			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionLines3D[0].RingPlaneTopology);

			// Bottom:
			ring1.Reverse();
			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);
			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(RingPlaneTopology.LeftPositive,
			                intersectionLines3D[0].RingPlaneTopology);
		}

		private static IList<IntersectionPath3D> CreateIntersectionLines3D(
			[NotNull] List<Pnt3D> ring1,
			[NotNull] List<Pnt3D> ring2,
			double tolerance = 0.001)
		{
			IList<IntersectionPath3D> intersectionLines3dFromRings =
				GeomTopoOpUtils.IntersectRings3D(ring1, ring2, tolerance);

			RingGroup polygon1 = new RingGroup(new Linestring(ring1));
			RingGroup polygon2 = new RingGroup(new Linestring(ring2));

			IList<IntersectionPath3D> intersectionLines3dFromPolys =
				GeomTopoOpUtils
					.GetCoplanarPolygonIntersectionLines3D(
						polygon1, polygon2, tolerance)
					.ToList();

			Assert.AreEqual(intersectionLines3dFromRings?.Count,
			                intersectionLines3dFromPolys.Count);

			for (var i = 0; i < intersectionLines3dFromPolys.Count; i++)
			{
				IntersectionPath3D fromPoly = intersectionLines3dFromPolys[i];
				IntersectionPath3D fromRing = intersectionLines3dFromRings[i];

				//Assert.AreEqual(fromPoly.RingPlaneTopology, fromRing.RingPlaneTopology);

				Assert.IsTrue(
					GeomTopoOpUtils.AreEqualXY(fromPoly.Segments, fromRing.Segments, 0.001, true));
			}

			return intersectionLines3dFromRings;
		}

		[Test]
		public void Can3DIntersectPlanarPolygons()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal (proper orientation for a roof):
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			//ring1.Reverse();

			// ring 2: vertical, smaller triangle
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(10, 10, -10));

			RingGroup polygon1 = new RingGroup(new Linestring(ring1));
			RingGroup polygon2 = new RingGroup(new Linestring(ring2));

			double tolerance = 0.001;
			IList<IntersectionPath3D> intersectionLines3D = GeomTopoOpUtils
			                                                .GetCoplanarPolygonIntersectionLines3D(
				                                                polygon1, polygon2, tolerance)
			                                                .ToList();

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(1, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);

			intersectionLines3D = GeomTopoOpUtils
			                      .GetCoplanarPolygonIntersectionLines3D(
				                      polygon2, polygon1, tolerance)
			                      .ToList();

			// Down-facing ring 1:
			polygon1.ReverseOrientation();
			intersectionLines3D = GeomTopoOpUtils.GetCoplanarPolygonIntersectionLines3D(
				polygon1, polygon2, tolerance);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);
		}

		[Test]
		public void Can3DIntersectRings2Intersections()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal (proper orientation for a roof):
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, smaller triangles
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(40, 40, 10));
			ring2.Add(new Pnt3D(50, 50, -10));
			ring2.Add(new Pnt3D(30, 30, -15));
			ring2.Add(new Pnt3D(10, 10, -10));

			IList<IntersectionPath3D> intersectionLines3D =
				CreateIntersectionLines3D(ring1, ring2);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(2, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);

			intersectionPath = intersectionLines3D[1];

			Assert.AreEqual(new Pnt3D(35, 35, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(45, 45, 0), intersectionPath.Segments[0].EndPoint);

			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);
			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionLines3D[0].RingPlaneTopology);

			// Bottom:
			ring1.Reverse();
			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);
			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(RingPlaneTopology.LeftPositive,
			                intersectionLines3D[0].RingPlaneTopology);
		}

		[Test]
		public void Can3DIntersectRingsWithSegmentInOtherPlane()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, some horizontal segments
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(15, 15, 0));
			ring2.Add(new Pnt3D(20, 20, 0));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(25, 25, 0));
			ring2.Add(new Pnt3D(30, 30, 0));
			ring2.Add(new Pnt3D(35, 35, 0));
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(10, 10, -10));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring1, ring2);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(3, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(RingPlaneTopology.LeftPositive,
			                intersectionPath.RingPlaneTopology);

			// more interesting: the intersection lines relative to the complex ring
			intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(3, intersectionLines3D.Count);

			intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());
			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(20, 20, 0), intersectionPath.Segments[0].EndPoint);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionPath.RingPlaneTopology);
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(20, 20, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);

			intersectionPath = intersectionLines3D[2];
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);
			Assert.AreEqual(3, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(25, 25, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(30, 30, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(new Pnt3D(35, 35, 0), intersectionPath.Segments[1].EndPoint);
		}

		[Test]
		public void Can3DIntersectRingsWithSegmentInOtherPlaneAndCutThrough()
		{
			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, some horizontal segments
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(15, 15, 0));
			ring2.Add(new Pnt3D(20, 20, 0));
			ring2.Add(new Pnt3D(20, 20, 10));
			// and a cut-through:
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(10, 10, -10));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring1, ring2);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(2, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(20, 20, 0), intersectionPath.Segments[0].EndPoint);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(new Pnt3D(20, 20, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);

			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(2, intersectionLines3D.Count);

			intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);
		}

		[Test]
		public void Can3DIntersectRingsWithSegmentInOtherPlaneAndIndexReversal()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:           1
			//                /\
			//               /  \
			//              /    \
			//          0.5/  3___\2         ______________________cut line/plane______________________
			//            /    \
			//           /______\
			//          0/5      4

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, a horizontal segment that runs against the order of intersection points
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(30, 30, 0));
			ring2.Add(new Pnt3D(20, 20, 0));
			ring2.Add(new Pnt3D(10, 10, -10));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(2, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(20, 20, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(new Pnt3D(20, 20, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(30, 30, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);

			// 
			ring1.Reverse();
			ring2.Reverse();

			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(2, intersectionLines3D.Count);

			intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(20, 20, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.LeftPositive,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(new Pnt3D(20, 20, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(30, 30, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);
		}

		[Test]
		public void Can3DIntersectRingsWithSegmentInOtherPlaneAndIndexReversalAtStart()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:      1
			//            /\
			//           /  \
			//          /    \
			//      0/5/___4  \        ______________________cut line/plane______________________
			//            /    \
			//           /______\
			//          3        2

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, a horizontal segment that runs against the order of intersection points
			ring2.Add(new Pnt3D(10, 10, 0));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(20, 20, 0));
			ring2.Add(new Pnt3D(10, 10, 0));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(2, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());
			Assert.AreEqual(new Pnt3D(10, 10, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(20, 20, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(new Pnt3D(20, 20, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionPath.RingPlaneTopology);
		}

		[Test]
		public void
			Can3DIntersectRingsWithSegmentInOtherPlaneAndIndexReversalAtStartAndEnd()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:         1
			//               /\
			//              /  \
			//             /    \
			//            /      \
			//           /        \
			//          /          \
			//      0/7/___6    3___\2        ______________________cut line/plane______________________
			//            /      \
			//           /________\
			//          5          4

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, a horizontal segment that runs against the order of intersection points
			ring2.Add(new Pnt3D(10, 10, 0));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(30, 30, 0));
			ring2.Add(new Pnt3D(25, 25, 0));
			ring2.Add(new Pnt3D(30, 30, -10));
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(15, 15, 0));
			ring2.Add(new Pnt3D(10, 10, 0));

			IList<IntersectionPath3D> intersectionLines3D = CreateIntersectionLines3D(ring2, ring1);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(3, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());
			Assert.AreEqual(new Pnt3D(10, 10, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(15, 15, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[2];
			Assert.AreEqual(new Pnt3D(25, 25, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(30, 30, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);

			ring2.Reverse();
			intersectionLines3D = CreateIntersectionLines3D(ring2, ring1, 0.001);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(3, intersectionLines3D.Count);
		}

		[Test]
		public void Can3DIntersectRingsWithSegmentInOtherPlaneAndTwoAdjacentCutThroughs()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:    1         4
			//          /\        /\
			//         /  \______/  \    ______________________cut line/plane______________________
			//        /   2      3   \
			//       /________________\
			//       0/6               5

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: vertical, a horizontal segment that runs against the order of intersection points
			ring2.Add(new Pnt3D(10, 10, -10));
			ring2.Add(new Pnt3D(20, 20, 10));
			ring2.Add(new Pnt3D(25, 25, 0));
			ring2.Add(new Pnt3D(30, 30, 0));
			ring2.Add(new Pnt3D(35, 35, 10));
			ring2.Add(new Pnt3D(45, 45, -10));
			ring2.Add(new Pnt3D(10, 10, -10));

			IList<IntersectionPath3D> intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(3, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(15, 15, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(25, 25, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[1];
			Assert.AreEqual(new Pnt3D(25, 25, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(30, 30, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);

			intersectionPath = intersectionLines3D[2];
			Assert.AreEqual(new Pnt3D(30, 30, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(40, 40, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.LeftNegative,
			                intersectionPath.RingPlaneTopology);

			ring2.Reverse();
			intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(ring2, ring1, 0.001);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(3, intersectionLines3D.Count);
		}

		[Test]
		public void Cannot3DIntersectRingsInParallelDisjointPlanes()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:      
			//                ____ring2
			//      ring1_____            
			//             

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 0, 97));
			ring2.Add(new Pnt3D(100, 100, 97));
			ring2.Add(new Pnt3D(200, 100, 97));
			ring2.Add(new Pnt3D(200, 0, 97));
			ring2.Add(new Pnt3D(100, 0, 97));

			IList<IntersectionPath3D> intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001);

			Assert.Null(intersectionLines3D);
		}

		[Test]
		public void Cannot3DIntersectHorizontalRingsInTheSamePlaneButDisjoint()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:      
			//            
			//      ring1_____|   |____ring2    _________________cut line/plane______________________
			//             

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(101, 0, 0));
			ring2.Add(new Pnt3D(101, 100, 0));
			ring2.Add(new Pnt3D(200, 150, 0));
			ring2.Add(new Pnt3D(200, 0, 0));
			ring2.Add(new Pnt3D(101, 0, 0));

			IList<IntersectionPath3D> intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001);

			Assert.Null(intersectionLines3D);

			intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001, true);
			Assert.Null(intersectionLines3D);
		}

		[Test]
		public void Can3DIntersectHorizontalRingsInTheSamePlane()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:      
			//            
			//      ring1_____|____ring2    _________________cut line/plane______________________
			//             

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(100, 0, 9));
			ring1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent
			ring2.Add(new Pnt3D(100, 0, 9));
			ring2.Add(new Pnt3D(100, 100, 9));
			ring2.Add(new Pnt3D(200, 150, 9));
			ring2.Add(new Pnt3D(200, 0, 9));
			ring2.Add(new Pnt3D(100, 0, 9));

			IList<IntersectionPath3D> intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001, true);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(1, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(2, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(100, 0, 9),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(100, 100, 9),
			                intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);
		}

		[Test]
		public void Can3DIntersectHorizontalRingsInTheSamePlaneWithOverlap()
		{
			// Between the end of this path and the start of the next path the cut line is outside 
			// the polygon unless there is a reversal of the ring index order, and a segment on the line:

			// Ring:      
			//            
			//      rings: 1|_____2|__|1____|2    _________________cut line/plane______________________
			//             

			var ring1 = new List<Pnt3D>();
			var ring2 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 0));
			ring1.Add(new Pnt3D(0, 100, 0));
			ring1.Add(new Pnt3D(100, 100, 0));
			ring1.Add(new Pnt3D(100, 0, 0));
			ring1.Add(new Pnt3D(0, 0, 0));

			// ring 2: also horizontal
			ring2.Add(new Pnt3D(50, 50, 0));
			ring2.Add(new Pnt3D(50, 150, 0));
			ring2.Add(new Pnt3D(150, 150, 0));
			ring2.Add(new Pnt3D(150, 50, 0));
			ring2.Add(new Pnt3D(50, 50, 0));

			IList<IntersectionPath3D> intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring1, ring2, 0.001);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(1, intersectionLines3D.Count);

			IntersectionPath3D intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(5, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(50, 100, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(100, 100, 0),
			                intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(new Pnt3D(100, 50, 0), intersectionPath.Segments[1].EndPoint);
			Assert.AreEqual(new Pnt3D(50, 50, 0), intersectionPath.Segments[2].EndPoint);
			Assert.AreEqual(new Pnt3D(50, 100, 0), intersectionPath.Segments[3].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);

			//
			// vice-versa
			intersectionLines3D = GeomTopoOpUtils.IntersectRings3D(
				ring2, ring1, 0.001);

			Assert.NotNull(intersectionLines3D);
			Assert.AreEqual(1, intersectionLines3D.Count);

			intersectionPath = intersectionLines3D[0];
			Assert.AreEqual(5, intersectionPath.Segments.GetPoints().Count());

			Assert.AreEqual(new Pnt3D(50, 50, 0),
			                intersectionPath.Segments[0].StartPoint);
			Assert.AreEqual(new Pnt3D(50, 100, 0), intersectionPath.Segments[0].EndPoint);
			Assert.AreEqual(new Pnt3D(100, 100, 0),
			                intersectionPath.Segments[1].EndPoint);
			Assert.AreEqual(new Pnt3D(100, 50, 0), intersectionPath.Segments[2].EndPoint);
			Assert.AreEqual(new Pnt3D(50, 50, 0), intersectionPath.Segments[3].EndPoint);
			Assert.AreEqual(RingPlaneTopology.InPlane,
			                intersectionPath.RingPlaneTopology);
		}

		#endregion

		[Test]
		public void CanIntersectParallelPlanes()
		{
			var plane1 = new Plane3D(7.5, 29.1, -33.243, 28);
			var plane2 = new Plane3D(7.5, 29.1, -33.243, -12);

			Assert.IsNull(GeomTopoOpUtils.IntersectPlanes(plane1, plane2, out Pnt3D _));
		}

		[Test]
		public void CanIntersectPlanes()
		{
			var plane1 = new Plane3D(7.5, 29.1, -33.243, 28);
			var plane2 = new Plane3D(-17.45, -56.9, 29.1, -12);

			Pnt3D p0;
			Vector v = GeomTopoOpUtils.IntersectPlanes(plane1, plane2, out p0);
			Assert.NotNull(v);
			Assert.NotNull(p0);

			MathUtils.AreEqual(plane1.GetDistanceAbs(p0.X, p0.Y, p0.Z), 0);

			Pnt p1 = p0 + 100000 * v;

			MathUtils.AreEqual(plane1.GetDistanceAbs(p1.X, p1.Y, p1[2]), 0);
		}

		[Test]
		public void CanSimplifyMultipoint()
		{
			Multipoint<IPnt> original = new Multipoint<IPnt>(
				new[]
				{
					new Pnt3D(2600000, 1200000, 453),
					new Pnt3D(2600009, 1200002, 454),
					new Pnt3D(2600008, 1200003, 455),
					new Pnt3D(2600007, 1200004, 456),
					new Pnt3D(2600006, 1200005, 457),
					new Pnt3D(2600005, 1200006, 458),
					new Pnt3D(2600007.001, 1200004.001, 456.001), // almost duplicate in xyz
					new Pnt3D(2600004, 1200007, 459)
				});

			Multipoint<IPnt> test =
				new Multipoint<IPnt>(original.GetPoints(0, null, true));

			//
			// 3D simplify:
			GeomTopoOpUtils.Simplify(test, 0.01, 0.01);

			// Structurally equal: No (different point order)
			Assert.IsFalse(original.Equals(test));

			// Equal in 3D: No (simplified)
			Assert.IsFalse(GeomRelationUtils.AreEqual(original, test, 0.0, 0.0));

			// Equal in 2D: Yes (simplified, but occupies the same XY-space)
			Assert.IsTrue(GeomRelationUtils.AreEqualXY(original, test, 0.01));

			// Cluster center is found:
			Pnt3D expectedClusteredPoint = new Pnt3D(2600007.0005, 1200004.0005, 456.0005);
			var foundPoint = test.FindPointIndexes(expectedClusteredPoint).ToList();

			Assert.AreEqual(1, foundPoint.Count);

			// Ensure point Z:
			Assert.IsTrue(expectedClusteredPoint.Equals(test.GetPoint(foundPoint[0])));

			// 1 point eliminated
			Assert.AreEqual(original.PointCount - 1, test.PointCount);

			//
			// 2D simplify:
			test = new Multipoint<IPnt>(original.GetPoints(0, null, true));
			GeomTopoOpUtils.Simplify(test, 0.01);

			// Structurally equal: No (different point order)
			Assert.IsFalse(original.Equals(test));

			// Equal in 3D: Yes, within 1cm (simplified but occupies the same XYZ space)
			Assert.IsTrue(GeomRelationUtils.AreEqual(original, test, 0.01, 0.01));

			// Equal in 3D: No, within 0.5mm 
			Assert.IsFalse(GeomRelationUtils.AreEqual(original, test, 0.0005, 0.0005));

			// Equal in 2D: Yes within 1cm (simplified, but occupies the same XY-space)
			Assert.IsTrue(GeomRelationUtils.AreEqualXY(original, test, 0.01));

			// Equal in 2D: No, within 0.5mm (simplified, but occupies the same XY-space)
			Assert.IsFalse(GeomRelationUtils.AreEqualXY(original, test, 0.0005));

			// Cluster center is found:
			foundPoint = test.FindPointIndexes(expectedClusteredPoint).ToList();

			Assert.AreEqual(1, foundPoint.Count);

			// Ensure point Z:
			Assert.IsTrue(expectedClusteredPoint.Equals(test.GetPoint(foundPoint[0])));

			// 1 point eliminated
			Assert.AreEqual(original.PointCount - 1, test.PointCount);

			////
			//// Clustering in 2D while keeping different Zs:
			original.AddPoint(new Pnt3D(2600007, 1200004, 458));
		}

		[Test]
		public void CanSimplifyMultipointAlreadySimple()
		{
			Multipoint<IPnt> original = new Multipoint<IPnt>(
				new[]
				{
					new Pnt3D(2600000, 1200000, 453),
					new Pnt3D(2600009, 1200002, 454),
					new Pnt3D(2600008, 1200003, 455),
					new Pnt3D(2600007, 1200004, 456),
					new Pnt3D(2600006, 1200005, 457),
					new Pnt3D(2600005, 1200006, 458),
					new Pnt3D(2600004, 1200007, 459)
				});

			Multipoint<IPnt> test =
				new Multipoint<IPnt>(original.GetPoints(0, null, true));

			//
			// 3D simplify:
			GeomTopoOpUtils.Simplify(test, 0.01, 0.01);

			// Structurally equal: Never (even if nothing happens happened)
			Assert.IsFalse(original.Equals(test));

			Assert.IsTrue(GeomRelationUtils.AreEqualXY(original, test, 0.0));
			Assert.IsTrue(GeomRelationUtils.AreEqual(original, test, 0.0, 0.0));

			// 2D simplify:
			GeomTopoOpUtils.Simplify(test, 0.01, 0.01);

			// Structurally equal: Never (even if nothing happens happened)
			Assert.IsFalse(original.Equals(test));
			Assert.IsTrue(GeomRelationUtils.AreEqualXY(original, test, 0.0));
			Assert.IsTrue(GeomRelationUtils.AreEqual(original, test, 0.0, 0.0));

			// 3D simple, but not 2D:
			original.AddPoint(new Pnt3D(2600007, 1200004, 458));

			test = new Multipoint<IPnt>(original.GetPoints(0, null, true));

			//
			// 3D simplify:
			GeomTopoOpUtils.Simplify(test, 0.1, 0.1);

			// Structurally equal: No
			Assert.IsFalse(original.Equals(test));

			// Clementini equals: yes
			Assert.IsTrue(GeomRelationUtils.AreEqualXY(original, test, 0.0));
			Assert.IsTrue(GeomRelationUtils.AreEqual(original, test, 0.0, 0.0));

			//
			// 2D simplify:
			test = new Multipoint<IPnt>(original.GetPoints(0, null, true));
			GeomTopoOpUtils.Simplify(original, 0.01);

			// Structurally equal: No
			Assert.IsFalse(original.Equals(test));

			// Equal in 3D: No (simplified)
			Assert.IsFalse(GeomRelationUtils.AreEqual(original, test, 0.0, 0.0));

			// Equal in 2D: Yes (simplified, but occupies the same XY-space)
			Assert.IsTrue(GeomRelationUtils.AreEqualXY(original, test, 0.0));
		}

		[Test]
		public void CanClusterPoints()
		{
			List<Pnt2D> points = new List<Pnt2D>
			                     {
				                     new Pnt2D(30, 30),
				                     new Pnt2D(31, 35),
				                     new Pnt2D(31.5, 30),
				                     new Pnt2D(32, 25),
				                     new Pnt2D(32.1, 34.9)
			                     };

			IList<KeyValuePair<IPnt, List<Pnt2D>>> keyValuePairs =
				GeomTopoOpUtils.Cluster(points, p => p, 2);

			Assert.AreEqual(3, keyValuePairs.Count);
		}

		private void AssertCanDeleteLinearSelfIntersections(Linestring linestring,
		                                                    double tolerance,
		                                                    int expectedPartCount,
		                                                    int expectedPointCount,
		                                                    double expectedArea,
		                                                    double? minSegmentLength = null)
		{
			List<Linestring> results = new List<Linestring>();

			Assert.IsTrue(GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
				              linestring, tolerance, results, minSegmentLength));

			Assert.True(results.All(l => l.IsClosed));
			Assert.True(results.All(l => l.ClockwiseOriented == true));

			Assert.AreEqual(expectedPartCount, results.Count);
			Assert.AreEqual(expectedPointCount, results.Sum(l => l.PointCount));
			Assert.AreEqual(expectedArea, results.Sum(l => l.GetArea2D()));
		}

		private static void WithRotatedRing(IList<Pnt3D> ring,
		                                    Action<Linestring> proc)
		{
			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);

				Linestring linestring = GeomTestUtils.CreateRing(array1.ToList());

				proc(linestring);
			}
		}

		private static IList<RingGroup> CutPlanarBothWays(RingGroup source,
		                                                  Linestring target,
		                                                  int expectedResultCount,
		                                                  int expectedInnerRingCount)
		{
			var result = CutPlanar(source, target, expectedResultCount, expectedInnerRingCount);

			var target2 = target.Clone();
			target2.ReverseOrientation();

			var result2 = CutPlanar(source, target, expectedResultCount, expectedInnerRingCount);

			Assert.AreEqual(result.Count, result2.Count);

			return result;
		}

		private static IList<RingGroup> CutPlanar(RingGroup source,
		                                          Linestring target,
		                                          int expectedResultCount,
		                                          int expectedInnerRingCount)
		{
			const double tolerance = 0.01;

			IList<RingGroup> result =
				GeomTopoOpUtils.CutPlanar(source, new MultiPolycurve(new[] { target }), tolerance);

			IList<MultiLinestring> xyResult =
				GeomTopoOpUtils.CutXY(source, new MultiPolycurve(new[] { target }), tolerance,
				                      true);

			Assert.AreEqual(result.Count, xyResult.Count);
			Assert.AreEqual(result.Sum(p => p.PartCount), xyResult.Sum(p => p.PartCount));

			Assert.AreEqual(expectedResultCount, result.Count);
			Assert.AreEqual(expectedInnerRingCount, result.Sum(p => p.InteriorRingCount));

			double resultArea = result.Sum(p => p.GetArea2D());
			double xyResultArea = xyResult.Sum(p => p.GetLinestrings().Sum(l => l.GetArea2D()));

			if (expectedResultCount > 0)
			{
				Assert.AreEqual(resultArea, xyResultArea, 0.0001);
				Assert.AreEqual(source.GetArea2D(), resultArea, 0.0001);

				Assert.False(
					GeomRelationUtils.InteriorIntersectXY(result[0], result[1], tolerance));
			}

			return result;
		}

		private static IList<MultiLinestring> CutXY(RingGroup source,
		                                            Linestring target,
		                                            int expectedResultCount,
		                                            int expectedInnerRingCount)
		{
			var multiTarget = new MultiPolycurve(new[] { target });

			return CutXY(source, multiTarget, expectedResultCount, expectedInnerRingCount);
		}

		private static IList<MultiLinestring> CutXY(RingGroup source,
		                                            MultiLinestring multiTarget,
		                                            int expectedResultCount,
		                                            int expectedInnerRingCount)
		{
			const double tolerance = 0.01;

			IList<MultiLinestring> result =
				GeomTopoOpUtils.CutXY(source, multiTarget, tolerance, true);

			Assert.AreEqual(expectedResultCount, result.Count);
			Assert.AreEqual(expectedInnerRingCount,
			                result.Sum(
				                p => p.GetLinestrings().Count(l => l.ClockwiseOriented == false)));

			double resultArea = result.Sum(p => p.GetLinestrings().Sum(l => l.GetArea2D()));

			if (expectedResultCount > 0)
			{
				Assert.AreEqual(source.GetArea2D(), resultArea, 0.0001);

				foreach (var tuple in CollectionUtils.GetAllTuples(result))
				{
					RingGroup firstResult = (RingGroup) tuple.Key;
					RingGroup otherResult = (RingGroup) tuple.Value;

					Assert.False(GeomRelationUtils.InteriorIntersectXY(
						             firstResult, otherResult, tolerance));
				}
			}

			return result;
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class GeomRelationUtilsTest
	{
		[Test]
		public void CanDetermineEqualMultipointXyVertical()
		{
			Multipoint<IPnt> mp1 = new Multipoint<IPnt>(
				new[]
				{
					new Pnt3D(1234.5678, 9876.54321, 345.6),
					new Pnt3D(1234.5678, 9876.54321, 355.6),
					new Pnt3D(1234.5678, 9876.54321, 365.6),
					new Pnt3D(1234.5678, 9876.54321, 375.6)
				});

			Multipoint<IPnt> mp2 = new Multipoint<IPnt>(mp1.GetPoints(0, null, true));

			Assert.IsTrue(GeomRelationUtils.AreEqualXY(mp1, mp2, 0.00001));

			mp2 = new Multipoint<IPnt>(mp1.GetPoints(0, null, true).Reverse());

			Assert.IsTrue(GeomRelationUtils.AreEqualXY(mp1, mp2, 0.00001));
		}

		[Test]
		public void CanDetermineEqualMultipointXyChangedOrderWithDuplicates()
		{
			Multipoint<IPnt> mp1 = new Multipoint<IPnt>(
				new[]
				{
					new Pnt3D(1234.5678, 9876.54321, 345.6),
					new Pnt3D(234.5678, 987.54321, 355.6),
					new Pnt3D(34.5678, 98.54321, 365.6),
					new Pnt3D(4.5678, 9.54321, 375.6)
				});

			Multipoint<IPnt> mp2 = new Multipoint<IPnt>(mp1.GetPoints(0, null, true).Reverse());

			mp2.AddPoint(new Pnt3D(34.5678, 98.54321, 365.6));

			Assert.IsTrue(GeomRelationUtils.AreEqualXY(mp1, mp2, 0.00001));

			mp2 = new Multipoint<IPnt>(mp2.GetPoints(0, null, true).Reverse());

			Assert.IsTrue(GeomRelationUtils.AreEqualXY(mp1, mp2, 0.00001));

			mp2.AddPoint(new Pnt3D(34.4678, 98.64321, 365.6));

			Assert.IsFalse(GeomRelationUtils.AreEqualXY(mp1, mp2, 0.00001));
			Assert.IsTrue(GeomRelationUtils.AreBoundsEqual(mp1, mp2, 0.000001));
		}

		[Test]
		public void CanGetContainsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 10),
				            new Pnt3D(0, 100, 20),
				            new Pnt3D(100, 100, 10),
				            new Pnt3D(50, 50, 10),
				            new Pnt3D(25, 75, 10),
				            new Pnt3D(0, 0, 10)
			            };

			Linestring l = new Linestring(ring1);

			double tolerance = 0.001;

			var pointOutsideRightOfCape = new Pnt3D(70, 50, 2);
			Assert.IsFalse(
				GeomRelationUtils.AreBoundsDisjoint(l, pointOutsideRightOfCape, tolerance));
			Assert.IsFalse(GeomRelationUtils.LinesContainXY(l, pointOutsideRightOfCape, tolerance));
			Assert.IsFalse(
				GeomRelationUtils.PolycurveContainsXY(l, pointOutsideRightOfCape, tolerance));

			Assert.AreEqual(
				false, GeomRelationUtils.AreaContainsXY(l, pointOutsideRightOfCape, tolerance));
			Assert.AreEqual(
				false,
				GeomRelationUtils.AreaContainsXY(l, pointOutsideRightOfCape, tolerance, true));

			var pointOutsideLeftOfCape = new Pnt3D(40, 50, 2);
			Assert.IsFalse(
				GeomRelationUtils.AreBoundsDisjoint(l, pointOutsideLeftOfCape, tolerance));
			Assert.IsFalse(GeomRelationUtils.LinesContainXY(l, pointOutsideLeftOfCape, tolerance));
			Assert.IsFalse(
				GeomRelationUtils.PolycurveContainsXY(l, pointOutsideLeftOfCape, tolerance));

			Assert.AreEqual(
				false, GeomRelationUtils.AreaContainsXY(l, pointOutsideLeftOfCape, tolerance));
			Assert.AreEqual(
				false,
				GeomRelationUtils.AreaContainsXY(l, pointOutsideLeftOfCape, tolerance, true));

			var pointInside = new Pnt3D(10, 90, 123);
			Assert.IsFalse(GeomRelationUtils.AreBoundsDisjoint(l, pointInside, tolerance));
			Assert.IsFalse(GeomRelationUtils.LinesContainXY(l, pointInside, tolerance));
			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(l, pointInside, tolerance));

			Assert.AreEqual(true, GeomRelationUtils.AreaContainsXY(l, pointInside, tolerance));
			Assert.AreEqual(
				true, GeomRelationUtils.AreaContainsXY(l, pointInside, tolerance, true));

			var pointOnTopRightCorner = new Pnt3D(100, 100, 2);
			Assert.IsFalse(
				GeomRelationUtils.AreBoundsDisjoint(l, pointOnTopRightCorner, tolerance));
			Assert.IsTrue(GeomRelationUtils.LinesContainXY(l, pointOnTopRightCorner, tolerance));
			Assert.IsTrue(
				GeomRelationUtils.PolycurveContainsXY(l, pointOnTopRightCorner, tolerance));
			Assert.AreEqual(
				null, GeomRelationUtils.AreaContainsXY(l, pointOnTopRightCorner, tolerance));
			Assert.AreEqual(
				null, GeomRelationUtils.AreaContainsXY(l, pointOnTopRightCorner, tolerance, true));

			var pointOnCape = new Pnt3D(50, 50, 321);
			Assert.IsFalse(GeomRelationUtils.AreBoundsDisjoint(l, pointOnCape, tolerance));
			Assert.IsTrue(GeomRelationUtils.LinesContainXY(l, pointOnCape, tolerance));
			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(l, pointOnCape, tolerance));
			Assert.AreEqual(null, GeomRelationUtils.AreaContainsXY(l, pointOnCape, tolerance));
			Assert.AreEqual(
				null, GeomRelationUtils.AreaContainsXY(l, pointOnCape, tolerance, true));

			var pointOnLeftEdge = new Pnt3D(0, 50, 2);
			Assert.IsFalse(GeomRelationUtils.AreBoundsDisjoint(l, pointOnLeftEdge, tolerance));
			Assert.IsTrue(GeomRelationUtils.LinesContainXY(l, pointOnLeftEdge, tolerance));
			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(l, pointOnLeftEdge, tolerance));
			Assert.AreEqual(null, GeomRelationUtils.AreaContainsXY(l, pointOnLeftEdge, tolerance));
			Assert.AreEqual(
				null, GeomRelationUtils.AreaContainsXY(l, pointOnLeftEdge, tolerance, true));
		}

		[Test]
		public void CanDetermineContainsXY()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 10),
				            new Pnt3D(0, 100, 20),
				            new Pnt3D(100, 100, 10),
				            new Pnt3D(50, 50, 10),
				            new Pnt3D(25, 75, 10),
				            new Pnt3D(0, 0, 10)
			            };

			Linestring l = new Linestring(ring1);

			Assert.IsFalse(GeomRelationUtils.PolycurveContainsXY(l, new Pnt3D(70, 50, 2), 0.001));
			Assert.IsFalse(GeomRelationUtils.PolycurveContainsXY(l, new Pnt3D(40, 50, 2), 0.001));
			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(l, new Pnt3D(50, 50, 2), 0.001));
			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(l, new Pnt3D(100, 100, 2), 0.001));
			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(l, new Pnt3D(0, 50, 2), 0.001));
		}

		[Test]
		public void
			CanDeterminePolygonContainsPolygonWithLinearIntersectionOfNonIntersectingSegmentXY()
		{
			// The only segment that deviates from the source also has a linear intersection with the
			// source:
			//       ____ 	         
			//       |  |	         
			//   ____|  |	     _______
			//  |       |	    |       |
			//  |_______|	    |_______|
			//  *               *
			//  source           target

			// This test makes sure that the 'non-intersecting-target-point' logic is applied correctly

			var sourcePoints = new List<Pnt3D>
			                   {
				                   new Pnt3D(0, 0, 10),
				                   new Pnt3D(0, 50, 20),
				                   new Pnt3D(60, 50, 10),
				                   new Pnt3D(60, 100, 10),
				                   new Pnt3D(100, 100, 10),
				                   new Pnt3D(100, 0, 10),
				                   new Pnt3D(0, 0, 10)
			                   };

			Linestring source = new Linestring(sourcePoints);

			var targetPoints = new List<Pnt3D>()
			                   {
				                   new Pnt3D(0, 0, 10),
				                   new Pnt3D(0, 50, 20),
				                   new Pnt3D(100, 50, 10),
				                   new Pnt3D(100, 0, 10),
				                   new Pnt3D(0, 0, 10)
			                   };

			Linestring target = new Linestring(targetPoints);

			Assert.IsTrue(GeomRelationUtils.AreaContainsXY(source, target, 0.0001));

			// The target can have a different orientation:
			Linestring revertedTarget = target.Clone();
			revertedTarget.ReverseOrientation();

			// Now the source is an island -> Contains should be false!
			var outerRingPoints = new List<Pnt3D>
			                      {
				                      new Pnt3D(-10, -10, 0),
				                      new Pnt3D(-10, 110, 0),
				                      new Pnt3D(110, 110, 0),
				                      new Pnt3D(110, -10, 0),
				                      new Pnt3D(-10, -10, 0)
			                      };

			var outerRing = new Linestring(outerRingPoints);

			source.ReverseOrientation();

			var sourcePoly = new MultiPolycurve(new[] { outerRing, source });

			Assert.IsFalse(GeomRelationUtils.AreaContainsXY(sourcePoly, target, 0.0001));

			// And the reverted target:
			Assert.IsFalse(
				GeomRelationUtils.AreaContainsXY(sourcePoly, revertedTarget, 0.0001));
		}

		[Test]
		public void CanDetermineMultipartSourcePolygonContainsPolygonTouchingBoth()
		{
			//                       
			//    _____*_______     *
			//    |   /|      |	    |\
			//    | 1/ |   0  |     | \
			//    | /  |      |     |  \
			//    |/   |______|     |___\
			//     source rings    target

			//    _____*_______ 
			//    |   /|\  0  |
			//    | 1/ | \    |
			//    | /  |  \   |
			//    |/   |___\__|
			//     Source rings with target. The target is fully contained in source ring 0
			//     All three rings touch in *

			var source0Points = new List<Pnt3D>
			                    {
				                    new Pnt3D(0, 0, 10),
				                    new Pnt3D(0, 100, 20),
				                    new Pnt3D(50, 100, 10),
				                    new Pnt3D(0, 0, 10)
			                    };

			Linestring source0 = new Linestring(source0Points);

			var source1Points = new List<Pnt3D>
			                    {
				                    new Pnt3D(50, 0, 10),
				                    new Pnt3D(50, 100, 20),
				                    new Pnt3D(100, 100, 10),
				                    new Pnt3D(100, 0, 10),
				                    new Pnt3D(50, 0, 10)
			                    };
			Linestring source1 = new Linestring(source1Points);

			var targetPoints = new List<Pnt3D>()
			                   {
				                   new Pnt3D(80, 0, 10),
				                   new Pnt3D(50, 0, 20),
				                   new Pnt3D(50, 100, 10),
				                   new Pnt3D(80, 0, 10)
			                   };

			Linestring target = new Linestring(targetPoints);

			MultiPolycurve source = new MultiPolycurve(new[] { source0, source1 });

			const double tolerance = 0.0001;
			Assert.IsTrue(GeomRelationUtils.AreaContainsXY(source, target, tolerance));

			Assert.IsTrue(GeomRelationUtils.IsContainedXY(target, source, tolerance));

			// And hence the union should be equal to the source:
			MultiLinestring unionAreasXY =
				GeomTopoOpUtils.GetUnionAreasXY(source, new RingGroup(target), tolerance);
			Assert.AreEqual(source.GetArea2D(), unionAreasXY.GetArea2D());
		}

		[Test]
		public void CanDetermineMultipartSourcePolygonTouchingPolygonAtInnerRingTouchPoint()
		{
			// A touching interior ring (no boundary loop) should not result in an incorrect
			// determination of contains:
			//      ___
			//      \  | target
			//       \ |
			//        \|
			//    _____*_______ 
			//    |   /|      |
			//    | 1/ |   0  |
			//    | /__|      |
			//    |___________|
			//     source rings with interior ring 1 touching ring 0 from the inside.

			var source0Points = new List<Pnt3D>
			                    {
				                    new Pnt3D(0, 0, 10),
				                    new Pnt3D(0, 100, 20),
				                    new Pnt3D(100, 100, 10),
				                    new Pnt3D(100, 0, 10),
				                    new Pnt3D(0, 0, 10)
			                    };

			Linestring source0 = new Linestring(source0Points);

			var source1Points = new List<Pnt3D>
			                    {
				                    new Pnt3D(50, 100, 10),
				                    new Pnt3D(25, 20, 20),
				                    new Pnt3D(50, 20, 10),
				                    new Pnt3D(50, 100, 10)
			                    };
			Linestring source1 = new Linestring(source1Points);

			var targetPoints = new List<Pnt3D>()
			                   {
				                   new Pnt3D(50, 100, 10),
				                   new Pnt3D(30, 150, 20),
				                   new Pnt3D(50, 150, 10),
				                   new Pnt3D(50, 100, 10),
			                   };

			Linestring target = new Linestring(targetPoints);

			MultiPolycurve source = new MultiPolycurve(new[] { source0, source1 });

			const double tolerance = 0.0001;
			Assert.IsFalse(GeomRelationUtils.AreaContainsXY(source, target, tolerance));

			Assert.IsFalse(GeomRelationUtils.IsContainedXY(target, source, tolerance));

			// And hence the union should be equal to the sum:
			MultiLinestring unionAreasXY =
				GeomTopoOpUtils.GetUnionAreasXY(source, new RingGroup(target), tolerance);
			Assert.AreEqual(source.GetArea2D() + target.GetArea2D(), unionAreasXY.GetArea2D());
		}

		[Test]
		public void CanDetermineMultipartSourcePolygonMultitouch()
		{
			// The source polygon has two touching rings. The target touches the source-touch point
			// and additionally the ring 1 in 2 other points (from the inside)
			MultiPolycurve source = (MultiPolycurve) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("multitouch_source.wkb"),
				out WkbGeometryType wkbType);

			Assert.AreEqual(WkbGeometryType.MultiPolygon, wkbType);

			RingGroup target = (RingGroup) GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath("multitouch_target.wkb"),
				out wkbType);

			Assert.AreEqual(WkbGeometryType.Polygon, wkbType);

			double tolerance = 0.01;

			Assert.IsTrue(GeomRelationUtils.AreaContainsXY(source, target, tolerance));
			Assert.IsTrue(GeomRelationUtils.IsContainedXY(target, source, tolerance));

			// And hence the union should be equal to the source:
			MultiLinestring unionAreasXY =
				GeomTopoOpUtils.GetUnionAreasXY(source, target, tolerance);
			Assert.AreEqual(source.GetArea2D(), unionAreasXY.GetArea2D());

			//// TODO: The target is multi-part (touching in point) and the start point is at the touch point
			////       -> filter out touch point type intersections?
			//// Somehow check that we are not diving into another target part?
			//// The Outbound source must not be inbound into a touching other source part!
			//unionAreasXY = GeomTopoOpUtils.GetUnionAreasXY(target, source, tolerance);
			//Assert.AreEqual(source.GetArea2D(), unionAreasXY.GetArea2D());
		}

		[Test]
		public void CanDetermineTouchesXY()
		{
			var pnts1 = new List<Pnt3D>();
			var pnts2 = new List<Pnt3D>();
			var pnts3 = new List<Pnt3D>();

			// ring1: horizontal:
			pnts1.Add(new Pnt3D(0, 0, 9));
			pnts1.Add(new Pnt3D(0, 100, 9));
			pnts1.Add(new Pnt3D(100, 100, 9));
			pnts1.Add(new Pnt3D(100, 0, 9));
			pnts1.Add(new Pnt3D(0, 0, 9));

			// ring 2: also horizontal, adjacent, touching along line
			pnts2.Add(new Pnt3D(100, 10, 18));
			pnts2.Add(new Pnt3D(100, 90, 18));
			pnts2.Add(new Pnt3D(200, 90, 18));
			pnts2.Add(new Pnt3D(200, 10, 18));
			pnts2.Add(new Pnt3D(100, 10, 18));

			// ring 3: containing both rings, touching ring1
			pnts3.Add(new Pnt3D(0, -10, 0));
			pnts3.Add(new Pnt3D(0, 110, 0));
			pnts3.Add(new Pnt3D(250, 110, 0));
			pnts3.Add(new Pnt3D(250, -10, 0));
			pnts3.Add(new Pnt3D(0, -10, 0));

			var ring1 = new Linestring(pnts1);
			var ring2 = new Linestring(pnts2);
			var ring3 = new Linestring(pnts3);

			bool disjoint;
			Assert.IsTrue(GeomRelationUtils.TouchesXY(ring1, ring2, 0.00001, out disjoint));
			Assert.False(disjoint);

			// ring3 is exterior ring that contains ring1
			Assert.False(GeomRelationUtils.TouchesXY(ring1, ring3, 0.00001, out disjoint, true));
			Assert.False(disjoint);
			// use orientation information (ring3
			Assert.False(GeomRelationUtils.TouchesXY(ring1, ring3, 0.00001, out disjoint));

			ring3.ReverseOrientation();
			// Now ring3 is interior ring that contains ring1 (i.e. outside -> touches)
			Assert.True(GeomRelationUtils.TouchesXY(ring1, ring3, 0.00001, out disjoint));
			Assert.False(disjoint);

			const bool disregardRingOrientation = true;
			Assert.False(GeomRelationUtils.TouchesXY(ring1, ring3, 0.00001, out disjoint,
			                                         disregardRingOrientation));
			Assert.False(disjoint);

			// now ring2 becomes an inner ring and ring1 should not be touching it any more:
			pnts2.Reverse();
			var innerRing2 = new Linestring(pnts2);
			Assert.IsFalse(GeomRelationUtils.TouchesXY(ring1, innerRing2, 0.00001, out disjoint));
		}

		[Test]
		public void CanDetermineTouchesXYVerticalRing()
		{
			var verticalRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(30, 100, 0),
				                                  new Pnt3D(30, 100, 55),
				                                  new Pnt3D(0, 0, 55),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			var adjacentRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(30, 100, 0),
				                                  new Pnt3D(30, 0, 0),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			bool disjoint;
			Assert.True(GeomRelationUtils.TouchesXY(adjacentRing, verticalRing, 0.0001,
			                                        out disjoint, true,
			                                        true));
			Assert.False(disjoint);

			adjacentRing.ReverseOrientation();
			Assert.True(GeomRelationUtils.TouchesXY(adjacentRing, verticalRing, 0.0001,
			                                        out disjoint, true,
			                                        true));
			Assert.False(disjoint);

			var containedVerticalRing = new Linestring(new List<Pnt3D>
			                                           {
				                                           new Pnt3D(0, 0, 0),
				                                           new Pnt3D(30, 80, 0),
				                                           new Pnt3D(30, 80, 55),
				                                           new Pnt3D(0, 0, 55),
				                                           new Pnt3D(0, 0, 0)
			                                           });

			Assert.False(GeomRelationUtils.TouchesXY(adjacentRing, containedVerticalRing, 0.0001,
			                                         out disjoint, true, true));
		}

		[Test]
		public void CanDetermineTouchesXYVerticalRingUnCracked()
		{
			// The vertical ring has no correspondent vertex in the adjacent ring at one of the
			// vertical edges.
			// Extra difficulty with not-quite-vertical line -> orientation is not null
			var verticalRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(15, 50, 0),
				                                  new Pnt3D(15, 50.0001, 55),
				                                  new Pnt3D(0, 0, 55),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			var adjacentRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(30, 100, 0),
				                                  new Pnt3D(30, 0, 0),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			var touchingRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(100, 100, 0),
				                                  new Pnt3D(100, 0, 0),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			const double tolerance = 0.001;

			Assert.IsTrue(GeomRelationUtils.HaveLinearIntersectionsXY(
				              adjacentRing, verticalRing, tolerance));

			Assert.IsFalse(GeomRelationUtils.HaveLinearIntersectionsXY(
				               touchingRing, verticalRing, tolerance));

			bool touchXY = GeomRelationUtils.TouchesXY(adjacentRing, verticalRing, tolerance,
			                                           out bool disjoint, true, true);

			Assert.IsTrue(touchXY);
			Assert.IsFalse(disjoint);

			touchXY = GeomRelationUtils.TouchesXY(touchingRing, verticalRing, tolerance,
			                                      out disjoint, true, true);

			Assert.IsTrue(touchXY);
			Assert.IsFalse(disjoint);

			Assert.IsTrue(GeomRelationUtils.PolycurveContainsXY(
				              adjacentRing, verticalRing, tolerance));
			Assert.IsFalse(GeomRelationUtils.PolycurveContainsXY(
				               touchingRing, verticalRing, tolerance));
		}

		[Test]
		public void CanDetermineTouchesXYVerticalRingWithinTolerance()
		{
			var verticalRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(14.99, 50, 0),
				                                  new Pnt3D(60, 200, 0),
				                                  new Pnt3D(45, 150, 55),
				                                  new Pnt3D(0, 0, 55),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			var adjacentRing = new Linestring(new List<Pnt3D>
			                                  {
				                                  new Pnt3D(0, 0, 0),
				                                  new Pnt3D(30, 100, 0),
				                                  new Pnt3D(30, 0, 0),
				                                  new Pnt3D(0, 0, 0)
			                                  });

			bool disjoint;
			Assert.True(GeomRelationUtils.TouchesXY(adjacentRing, verticalRing, 0.01,
			                                        out disjoint, true,
			                                        true));
			Assert.False(disjoint);

			adjacentRing.ReverseOrientation();
			Assert.True(GeomRelationUtils.TouchesXY(adjacentRing, verticalRing, 0.01,
			                                        out disjoint, true,
			                                        true));
			Assert.False(disjoint);
		}

		[Test]
		public void CanDetermineTouchesIdenticalRings()
		{
			var ring1 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 50, 9));
			ring1.Add(new Pnt3D(100, 20, 9));

			var ring2 = new List<Pnt3D>(ring1);
			ring2.Add(ring2[0]);

			for (var i = 0; i < 4; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				var linestring1 = new Linestring(rotatedRing);
				var linestring2 = new Linestring(ring2);

				bool disjoint;
				Assert.False(
					GeomRelationUtils.TouchesXY(linestring1, linestring2, 0.0001, out disjoint));
				Assert.False(disjoint);

				linestring2.ReverseOrientation();

				Assert.False(GeomRelationUtils.TouchesXY(linestring1, linestring2, 0.0001,
				                                         out disjoint,
				                                         true));
				Assert.False(disjoint);
			}
		}

		[Test]
		public void CanDetermineTouchesWithAlmostLinearIntersection()
		{
			// Repro test for TOP-5552

			// The touch point is adjacent to a point that is just above the tolerance from
			// the target, i.e. the segment intersection is almost linear.

			var ring1 = new Linestring(new List<Pnt3D>
			                           {
				                           new Pnt3D(0, 0, 9),
				                           new Pnt3D(0, 100, 9),
				                           new Pnt3D(100, 100, 9),
				                           new Pnt3D(100, 0, 9),
				                           new Pnt3D(0, 0, 9),
			                           });

			List<Pnt3D> ring2Points = new List<Pnt3D>
			                          {
				                          new Pnt3D(50, 50, 0),
				                          new Pnt3D(100, 50, 0),
				                          new Pnt3D(99.985, 10, 55),
				                          new Pnt3D(50, 10, 55)
			                          };

			Linestring ring1AsIsland = ring1.Clone();
			ring1AsIsland.ReverseOrientation();

			double tolerance = 0.01;

			for (var i = 0; i < 4; i++)
			{
				var ring2 = new Linestring(GeomTestUtils.GetRotatedRing(ring2Points, i));

				bool touchesXY =
					GeomRelationUtils.TouchesXY(ring1, ring2, tolerance, out bool disjoint);

				Assert.False(touchesXY);
				Assert.False(disjoint);

				touchesXY =
					GeomRelationUtils.TouchesXY(ring1AsIsland, ring2, tolerance, out disjoint);

				Assert.True(touchesXY);
				Assert.False(disjoint);
			}
		}

		[Test]
		public void CanDetermineInteriorLineIntersectsXY()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(10, 10, 0));
			var line2 = new Line3D(new Pnt3D(0, 10, 0), new Pnt3D(10, 0, 0));

			Assert.True(GeomRelationUtils.SourceInteriorIntersectsXY(line1, line2, 0));
			Assert.True(GeomRelationUtils.SourceInteriorIntersectsXY(line2, line1, 0));

			var touchingLine = new Line3D(line1.EndPoint, line2.StartPoint);

			double tolerance = double.Epsilon;
			Assert.False(
				GeomRelationUtils.SourceInteriorIntersectsXY(line1, touchingLine, tolerance));

			var disjointLine = new Line3D(new Pnt3D(5, 5.05, 17), new Pnt3D(6, 18, 2));
			Assert.False(GeomRelationUtils.SourceInteriorIntersectsXY(line1, disjointLine, 0));
			Assert.True(GeomRelationUtils.SourceInteriorIntersectsXY(line1, disjointLine, 0.1));

			var otherLineInteriorIntersected = new Line3D(new Pnt3D(0, 20, 0),
			                                              new Pnt3D(20, 0, 0));
			// however, this line is not interior-intersected:
			Assert.False(
				GeomRelationUtils.SourceInteriorIntersectsXY(
					line1, otherLineInteriorIntersected, 0.01));
		}
	}
}

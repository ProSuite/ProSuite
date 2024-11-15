using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.Commons.AO.Test.Geometry.Cracking
{
	[TestFixture]
	public class CrackPointCalculatorTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanCalculateCrackPointPolygonWithPoint()
		{
			// TOP-5614
			ISpatialReference lv95 = GetLv95();

			IRing sourceRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200060, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200060, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, lv95)));

			IPoint targetPointWithin = GeometryFactory.CreatePoint(2600010, 1200010, lv95);
			IPoint targetPointOnVertex = GeometryFactory.CreatePoint(2600000, 1200060, lv95);
			IPoint targetPointOnSegment = GeometryFactory.CreatePoint(2600000, 1200030, lv95);
			IPoint targetPointOnSegmentNextToVertex =
				GeometryFactory.CreatePoint(2600000, 1200059.9, lv95);

			IPolygon sourcePoly = GeometryFactory.CreatePolygon(sourceRing);

			double? snapTolerance = 0.01;
			double? minimumSegmentLength = 0.5;

			var crackPointCalculatorTargetZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength, false, false, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			int expectedCrackPoints = 0;
			double expectedResultZ = double.NaN;
			IPoint crackedPoint = TestCracking(targetPointWithin, sourcePoly,
			                                   crackPointCalculatorTargetZ, expectedCrackPoints,
			                                   expectedResultZ);
			Assert.IsNull(crackedPoint);

			expectedCrackPoints = 0;
			expectedResultZ = double.NaN;
			crackedPoint = TestCracking(targetPointOnVertex, sourcePoly,
			                            crackPointCalculatorTargetZ, expectedCrackPoints,
			                            expectedResultZ);
			Assert.IsNull(crackedPoint);

			expectedCrackPoints = 1;
			expectedResultZ = double.NaN;
			crackedPoint = TestCracking(targetPointOnSegment, sourcePoly,
			                            crackPointCalculatorTargetZ, expectedCrackPoints,
			                            expectedResultZ);
			Assert.NotNull(crackedPoint);

			// Now the same but with UseSourceZ:
			var crackPointCalculatorSourceZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength, false,true, 
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			expectedCrackPoints = 1;
			expectedResultZ = 620;
			crackedPoint = TestCracking(targetPointOnSegment, sourcePoly,
			                            crackPointCalculatorSourceZ, expectedCrackPoints,
			                            expectedResultZ);
			Assert.NotNull(crackedPoint);

			expectedCrackPoints = 1;
			var featureVertexInfo = Crack(sourcePoly, targetPointOnSegmentNextToVertex,
			                              crackPointCalculatorTargetZ, out _, out _);
			Assert.IsNull(featureVertexInfo.CrackPointCollection);
			Assert.IsNotNull(featureVertexInfo.NonCrackablePoints);
			Assert.AreEqual(expectedCrackPoints, featureVertexInfo.CrackPoints?.Count);
		}

		[Test]
		public void CanCrackExistingVertexInZOnlySinglePointIntersection()
		{
			// TOP-5289 (type I)
			ISpatialReference lv95 = GetLv95();

			IRing targetRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200060, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200060, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, lv95)));

			IRing sourceRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200030, 605, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200030, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200010, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200030, 605, double.NaN, lv95)));

			IPolygon targetPoly = GeometryFactory.CreatePolygon(targetRing);
			IPolygon sourcePoly = GeometryFactory.CreatePolygon(sourceRing);

			double? snapTolerance = 0.06;
			double? minimumSegmentLength = 0.5;

			var crackPointCalculatorTargetZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength,
				false, false, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			int expectCrackPoints = 1;
			var expectedResultZ = 620;
			IPoint crackedPoint = TestCracking(targetPoly, sourcePoly, crackPointCalculatorTargetZ,
			                                   expectCrackPoints, expectedResultZ);
			Assert.AreEqual(expectedResultZ, crackedPoint.Z);

			// Now the same but with UseSourceZ:
			var crackPointCalculatorSourceZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength,
				false, true, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			expectCrackPoints = 0;
			expectedResultZ = 605;
			Assert.IsNull(TestCracking(targetPoly, sourcePoly, crackPointCalculatorSourceZ,
			                           expectCrackPoints, expectedResultZ));

			// If there is already a vertex in the target:
			GeometryUtils.UpdateVertexZ((IPointCollection) targetPoly, crackedPoint, 0.001, 620);

			expectCrackPoints = 1;
			expectedResultZ = 620;
			crackedPoint = TestCracking(targetPoly, sourcePoly, crackPointCalculatorTargetZ,
			                            expectCrackPoints, expectedResultZ);
			Assert.AreEqual(expectedResultZ, crackedPoint.Z);

			// SourceZ
			expectCrackPoints = 0;
			expectedResultZ = 605;
			Assert.IsNull(TestCracking(targetPoly, sourcePoly, crackPointCalculatorSourceZ,
			                           expectCrackPoints, expectedResultZ));
		}

		[Test]
		public void CanCrackExistingVertexInZOnlyLinearIntersection()
		{
			// TOP-5289 (type II)
			ISpatialReference lv95 = GetLv95();

			IRing targetRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200060, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200060, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 640, double.NaN, lv95)));

			// Now the intersection is a line:
			IRing sourceRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200010, 605, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200030, 605, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200030, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200010, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200010, 605, double.NaN, lv95)));

			IPolygon targetPoly = GeometryFactory.CreatePolygon(targetRing);
			IPolygon sourcePoly = GeometryFactory.CreatePolygon(sourceRing);

			double? snapTolerance = 0.06;
			double? minimumSegmentLength = 0.5;

			var crackPointCalculatorTargetZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength,
				false, false, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			int expectCrackPoints = 2;
			var expectedResultZ = 640;
			IPoint crackedPoint = TestCracking(targetPoly, sourcePoly, crackPointCalculatorTargetZ,
			                                   expectCrackPoints, expectedResultZ);
			Assert.AreEqual(expectedResultZ, crackedPoint.Z);

			// Now the same but with UseSourceZ:
			var crackPointCalculatorSourceZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength,
				false, true, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			expectCrackPoints = 0;
			expectedResultZ = 605;
			Assert.IsNull(TestCracking(targetPoly, sourcePoly, crackPointCalculatorSourceZ,
			                           expectCrackPoints, expectedResultZ));

			// If there is already a vertex in the target:
			GeometryUtils.UpdateVertexZ((IPointCollection) targetPoly, crackedPoint, 0.001, 640);

			expectCrackPoints = 2;
			expectedResultZ = 640;
			crackedPoint = TestCracking(targetPoly, sourcePoly, crackPointCalculatorTargetZ,
			                            expectCrackPoints, expectedResultZ);
			Assert.AreEqual(expectedResultZ, crackedPoint.Z);

			// SourceZ
			expectCrackPoints = 0;
			expectedResultZ = 605;
			Assert.IsNull(TestCracking(targetPoly, sourcePoly, crackPointCalculatorSourceZ,
			                           expectCrackPoints, expectedResultZ));
		}

		[Test]
		public void CanCrackExistingVertexInZOnlyLinearIntersectionWithIntermediatePoints()
		{
			// TOP-5289 (type III)
			ISpatialReference lv95 = GetLv95();

			IRing targetRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200060, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200060, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 640, double.NaN, lv95)));

			// Now the intersection is a line:
			IRing sourceRing = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200010, 605, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200020, 605, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200030, 605, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200030, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200010, 640, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200010, 605, double.NaN, lv95)));

			IPolygon targetPoly = GeometryFactory.CreatePolygon(targetRing);
			IPolygon sourcePoly = GeometryFactory.CreatePolygon(sourceRing);

			double? snapTolerance = 0.06;
			double? minimumSegmentLength = 0.5;

			var crackPointCalculatorTargetZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength,
				false, false, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			int expectCrackPoints = 3;
			var expectedResultZ = 640;
			IPoint crackedPoint = TestCracking(targetPoly, sourcePoly, crackPointCalculatorTargetZ,
			                                   expectCrackPoints, expectedResultZ);
			Assert.AreEqual(expectedResultZ, crackedPoint.Z);

			// Now the same but with UseSourceZ:
			var crackPointCalculatorSourceZ = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength,
				false, true, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);

			expectCrackPoints = 0;
			expectedResultZ = 605;
			Assert.IsNull(TestCracking(targetPoly, sourcePoly, crackPointCalculatorSourceZ,
			                           expectCrackPoints, expectedResultZ));

			// If there is already a vertex in the target:
			GeometryUtils.UpdateVertexZ((IPointCollection) targetPoly, crackedPoint, 0.001, 640);

			expectCrackPoints = 3;
			expectedResultZ = 640;
			crackedPoint = TestCracking(targetPoly, sourcePoly, crackPointCalculatorTargetZ,
			                            expectCrackPoints, expectedResultZ);
			Assert.AreEqual(expectedResultZ, crackedPoint.Z);

			// SourceZ
			expectCrackPoints = 0;
			expectedResultZ = 605;
			Assert.IsNull(TestCracking(targetPoly, sourcePoly, crackPointCalculatorSourceZ,
			                           expectCrackPoints, expectedResultZ));
		}

		[Test]
		public void CanCalculateCrackPoints_Multipatch_UnsnappedVerticesWithinTolerance()
		{
			// TLM_GEBAEUDE {02117753-A145-48EB-BBF7-AA9295005834}
			IFeature feature =
				TestUtils.CreateMockFeature(
					TestUtils.GetGeometryTestDataPath("MultipatchWithUnsnappedRingVertices.xml"));

			const double tolerance = 0.0125;
			var featureVertexInfo = new FeatureVertexInfo(feature, null, tolerance, tolerance);

			var crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance,
				true, false, IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);
			crackPointCalculator.UseCustomIntersect = true;

			CrackUtils.AddGeometryPartIntersectionCrackPoints(featureVertexInfo,
			                                                  crackPointCalculator);

			IPoint expectedCrackPoint = GeometryFactory.CreatePoint(2721188.9875, 1252328.11125);

			Assert.IsNotNull(featureVertexInfo.CrackPointCollection);
			Assert.IsTrue(
				GeometryUtils.Contains((IGeometry) featureVertexInfo.CrackPointCollection,
				                       expectedCrackPoint));
		}

		[Test]
		public void CanCalculateCrackPoints_Multipatch_AcuteAngleWithoutCreatingCutBacks()
		{
			// TOP-5227: Crack Multipatch: Es ist möglich, dass das Cracking zu einem Dangle/Cutback in einem Ring führt
			// Part of the TLM_GEBAEUDE {2786DCB6-9E80-4ACA-BDDD-02FCBDD98F65}
			IFeature feature =
				TestUtils.CreateMockFeature(
					TestUtils.GetGeometryTestDataPath("MultipatchWithCutBack_before.xml"));

			// Side-issue:
			// First make sure that clustering does not mess up the snapping (by snapping to an average point)
			double tolerance = 0.1;
			var featureVertexInfo = new FeatureVertexInfo(feature, null, tolerance, tolerance);

			var crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance, false, true,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);

			crackPointCalculator.TargetTransformation =
				g => GeometryFactory.CreateMultipoint((IPointCollection) g);

			CrackUtils.AddGeometryPartIntersectionCrackPoints(featureVertexInfo,
			                                                  crackPointCalculator);

			// TODO: Investigate. Is this due to inaccurate AO intersection points?
			int expected = IntersectionUtils.UseCustomIntersect ? 3 : 2;
			Assert.NotNull(featureVertexInfo.CrackPoints);
			Assert.AreEqual(expected, featureVertexInfo.CrackPoints.Count);

			// AO-implementation does not check if the projected point-to-insert is within the segment
			// -> false positive on segment 18
			expected = IntersectionUtils.UseCustomIntersect ? 0 : 1;
			Assert.AreEqual(
				expected, featureVertexInfo.CrackPoints.Count(p => p.ViolatesMinimumSegmentLength));

			// Ensure it was snapped exactly onto an existing vertex:
			// This is not the case any more with 3D intersections!?
			//foreach (var cp in featureVertexInfo.CrackPoints.Where(
			//	cp => ! cp.ViolatesMinimumSegmentLength))
			//{
			//	Assert.NotNull(GeometryUtils.FindHitVertexIndex(
			//		               feature.Shape, cp.Point, 0.001, out int _));
			//}

			// Now make sure it is not snapped to a vertex where an adjacent ring would get cracked on 
			// 2 adjacent segments (TOP-5227):
			tolerance = 0.015;
			featureVertexInfo = new FeatureVertexInfo(feature, null, tolerance, tolerance);

			crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance, false, true,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);

			crackPointCalculator.TargetTransformation =
				g => GeometryFactory.CreateMultipoint((IPointCollection) g);

			crackPointCalculator.UseCustomIntersect = true;

			CrackUtils.AddGeometryPartIntersectionCrackPoints(featureVertexInfo,
			                                                  crackPointCalculator);

			Assert.NotNull(featureVertexInfo.CrackPoints);
			Assert.AreEqual(1, featureVertexInfo.CrackPoints.Count);

			// It should be excluded because it will crack 2 adjacent segments resulting in a
			// cut-back (duplicate segment)!

			var crackPoint = featureVertexInfo.CrackPoints[0];

			Assert.True(crackPoint.ViolatesMinimumSegmentLength);

			var result = new Dictionary<IFeature, IGeometry>();
			CrackUtils.AddRemovePoints(
				new List<FeatureVertexInfo> { featureVertexInfo }, result, null, null);

			Assert.AreEqual(0, result.Count);
		}

		[Test]
		[Ignore("Inherent ArcObjects problem, use different implementation.")]
		public void
			CanCalculateIntersectionsAtSmallAngleIntersectionWithVerticesSlightlyOffTheIntersection
			()
		{
			var line1 =
				(IPolyline) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("CrackpointAtSmallAngleIntersect_Line1.xml"));
			var line2 =
				(IPolyline) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("CrackpointAtSmallAngleIntersect_Line2.xml"));

			const bool addCrackPointsOnExistingVertices = false;
			const bool useSourceZs = true;

			var snapTolerance = 0.2;
			var crackPointCalculator = new CrackPointCalculator(
				snapTolerance, null,
				addCrackPointsOnExistingVertices, useSourceZs, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);
			crackPointCalculator.UseCustomIntersect = true;

			var result = crackPointCalculator.GetIntersectionPoints(
				line1, line2, out IGeometry _);

			// This fails with the large exclusionToleranceFactor == 1:
			Assert.AreEqual(1, result.Count);

			// Supposedly only one of the two intersections should be shown - either the exact intersection or the snapped target vertex
			// BUT: depending on the exclusionToleranceFactor constant both the large tolerance intersection (snapped to the target vertex)
			// AND the small tolerance intersection are returned. If both points are applied the chopping results 
			// in short lines (and possibly duplicate features) if the minimum segment length does not prevent it.
			// However, if the exclusionToleranceFactor is set to for example 5.0, the chance for mis-matches between 
			// large-tolerance and small-tolerance intersections (which often are no real intersections either, see COM-344) is high when the snap tolerance
			// is larger than just the tolerance. This then results in 'missing' real crack points.
			// -> The solution is to calculate the exact (not just small-resolution) intersection for each large-tolerance intersection
			// But the question remains whether two vertices closer than the snap tolerance (but not intersecting)
			// should be crackable if there is a real intersection also on the same segment (but along a certain distance) or even some more segments away
			// With the proper target-vertex snapping the large-tolerance intersection is in most small-angle-intersection cases better because 
			// it favors existing vertices which results in less vertex inserts (at the cost of moving the actual intesection and the segments 
			// around).

			// Proabably the best solution would be to allow both options:
			// 1. Honouring the actual intersection points but give up on short segment prevention (or clean up short segments after chopping)
			//    This would be achieved with no snapping (or snap tolerance == 0). This would require to calculate the mathematically correct intersections
			// 2. Avoid short(ish) segments by snapping to the next target vertex and disregarding the exact intersections (completely or sometimes).
			//    The big decision will be whether to start the snapping process from the actual intersection or from the large-tolerance intersection
			//    - Actual intersecions: misses the lines that do not cross but that has vertices inside the tolerance to the target
			//    - Large-tolerance intersection (large tolerance = snap tolerance): can be very far away from the actual intersection, especially with 
			//      large snap tolerances (factor 5 and more). Has the advandage that two lines crossing (at a small angle) does not turn into an overlap 
			//      of the lines but can move the crossing point by a fair distance.
			//    - Large-tolerance if there is no actual intersection anywhere on the adjacent segment(s) (same as current logic but without mismatches 
			//      between intersection points) and actual tolerance otherwise. If there is still an intersection (possibly at a different location due to the
			//      changed source geometry) after cracking at the large-tolerance crack point another cracking operation could be performed. Hmmm.
			//      Possibly it would be more consistent to always use the large tolerance intersection except an intersection is reported despite no vertices
			//      are anywhere near (like the cases in COM-344, but with large tolerance -> This requires a repro case with large tolerances. It seems not to
			//      happen a lot any more except with small tolerances).
			//      Or consider providing two separate algorithms depending on the user's choice?

			// Required changes: 
			// - Calculate the mathematically correct intersections on the segments that get intersection points reported (probably not even necessary, see COM-344)
			// - Only show either the large-tolerance intersection OR the accurate intersection but not both. The association between large-tolerance intersections
			//   and the accurate intersection is probalby more complex than the current implementation.

			IPoint largeToleranceIntersection = GeometryFactory.CreatePoint(
				2750020.9274999984, 1222510.9274999984, line1.SpatialReference);

			bool largeToleranceIntersectionReported = GeometryUtils.Contains(
				(IGeometry) result, largeToleranceIntersection);

			IPoint actualIntersection = GeometryFactory.CreatePoint(
				2750020.74218395, 1222510.1615199968, line1.SpatialReference);

			bool actualIntersectionReported =
				GeometryUtils.Contains((IGeometry) result, actualIntersection);

			Assert.IsTrue(largeToleranceIntersectionReported ^ actualIntersectionReported,
			              "Only the large-tolerance intersection OR the actual intersection should be found");

			snapTolerance = 0.8;
			crackPointCalculator = new CrackPointCalculator(
				snapTolerance, null,
				addCrackPointsOnExistingVertices, useSourceZs, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);
			crackPointCalculator.UseCustomIntersect = true;

			result = crackPointCalculator.GetIntersectionPoints(
				line1, line2, out IGeometry _);

			// This fails even with the large exclusionToleranceFactor == 5:
			Assert.AreEqual(1, result.Count);
		}

		[Test]
		public void CanCalculateIntersectionsForTouchingPolygons()
		{
			// Reproduces COM-344

			var poly1 =
				(IPolygon) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("CrackpointOnTouchingPolygons_Poly1.xml"));
			var poly2 =
				(IPolygon) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("CrackpointOnTouchingPolygons_Poly2.xml"));

			const bool addCrackPointsOnExistingVertices = false;
			const bool useSourceZs = true;

			double? snapTolerance = null;
			var crackPointCalculator = new CrackPointCalculator(
				snapTolerance, null,
				addCrackPointsOnExistingVertices, useSourceZs, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);
			crackPointCalculator.UseCustomIntersect = true;

			IPolyline sourceGeometry = GeometryFactory.CreatePolyline(poly1);
			var result = crackPointCalculator.GetIntersectionPoints(
				sourceGeometry, poly2, out IGeometry _);

			// Two points should result, but the small-tolerance intersection reports an additional point
			Assert.AreEqual(2, result.Count);

			snapTolerance = 0.1;
			crackPointCalculator = new CrackPointCalculator(
				snapTolerance, null,
				addCrackPointsOnExistingVertices, useSourceZs, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, null);
			crackPointCalculator.UseCustomIntersect = true;

			result = crackPointCalculator.GetIntersectionPoints(
				sourceGeometry, poly2, out IGeometry _);

			Assert.AreEqual(2, result.Count);
		}

		[Test]
		public void
			CanCalculateCrackPoints_Multipatch_UnsnappedVerticesDifferentBy1Resolution()
		{
			IFeature feature =
				TestUtils.CreateMockFeature(
					TestUtils.GetGeometryTestDataPath(
						"MultipatchWithUnsnappedRingVerticesBy1Resolution.xml"));

			const double tolerance = 0.0125;
			var featureVertexInfo = new FeatureVertexInfo(feature, null, tolerance, tolerance);

			var crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance,
				true, false, IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);
			crackPointCalculator.UseCustomIntersect = true;

			CrackUtils.AddGeometryPartIntersectionCrackPoints(featureVertexInfo,
			                                                  crackPointCalculator);

			IPoint expectedCrackPoint = GeometryFactory.CreatePoint(2720892.10875,
				1252333.85625);

			Assert.IsNotNull(featureVertexInfo.CrackPointCollection);
			Assert.IsTrue(
				GeometryUtils.Contains((IGeometry) featureVertexInfo.CrackPointCollection,
				                       expectedCrackPoint));
		}

		[Test]
		public void CanCalculateCrackPoint_Top5470()
		{
			// The intersection point is just within the tolerance, but once the intersection point is snapped to 
			// the spatial reference, it is just outside and the source segment is not found any more using hit test.
			IGeometry multipatch = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath(@"Top5470_multipatch.xml"));

			IFeature mockRoof =
				TestUtils.CreateMockFeature(multipatch, 0.01, 0.001);

			IGeometry footprint = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath(@"Top5470_polygon.xml"));

			IFeature mockFootprint =
				TestUtils.CreateMockFeature(footprint, 0.01, 0.001);

			const double tolerance = 0.01;
			var featureVertexInfo = new FeatureVertexInfo(mockRoof, null, tolerance, tolerance);

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;
			IntersectionUtils.UseCustomIntersect = false;
			var crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance, true, false, IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);

			try
			{
				CrackUtils.AddCrackPoints(featureVertexInfo, mockFootprint, crackPointCalculator);
			}
			catch (Exception e)
			{
				Console.WriteLine($"TOP-5470 detected (using standard intersect): {e}");
			}

			// The new simplify only clusters (within the actual tolerance) but does not snap to
			// spatial reference.
			IntersectionUtils.UseCustomIntersect = true;
			crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance, true, false, IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);

			featureVertexInfo = new FeatureVertexInfo(mockRoof, null, tolerance, tolerance);
			CrackUtils.AddCrackPoints(featureVertexInfo, mockFootprint, crackPointCalculator);

			Assert.AreEqual(3, GetDistinctCrackPointLocationCount(featureVertexInfo));

			IntersectionUtils.UseCustomIntersect = customIntersectOrig;
		}

		[Test]
		public void CanCalculateCrackPoint_Top5553()
		{
			// The intersection point is just within the tolerance, but once the intersection point is snapped to 
			// the spatial reference, it is just outside and the source segment is not found any more using hit test.
			IGeometry multipatch = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("ErrorGeom_20220615_135828_roof.xml"));

			IFeature mockRoof =
				TestUtils.CreateMockFeature(multipatch, 0.01, 0.001);

			IGeometry footprint = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("ErrorGeom_20220615_135828_footprint.xml"));

			IFeature mockFootprint =
				TestUtils.CreateMockFeature(footprint, 0.01, 0.001);

			const double tolerance = 0.01;

			var features = new List<IFeature> { mockRoof, mockFootprint };

			var inExtent = GeometryUtils.UnionFeatureEnvelopes(features);

			ICrackingOptions options = new CrackerToolOptions(
				null, new PartialCrackerToolOptions()
				      {
					      MinimumSegmentLength = new OverridableSetting<double>(0.01, true),
					      RespectMinimumSegmentLength = new OverridableSetting<bool>(true, true),
					      SnapTolerance = new OverridableSetting<double>(0.01, true),
					      SnapToTargetVertices = new OverridableSetting<bool>(true, true),
					      TargetFeatureSelection =
						      new OverridableSetting<TargetFeatureSelection>(
							      TargetFeatureSelection.SelectedFeatures, true),
					      UseSourceZs = new OverridableSetting<bool>(true, true)
				      });

			IntersectionPointOptions intersectionPointOptions =
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints;
			bool addCrackPointsAlsoOnExistingVertices = true;

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;
			IntersectionUtils.UseCustomIntersect = true;

			CrackPointCalculator crackPointCalculator =
				CreateCrackPointCalculator(tolerance, inExtent);

			//
			// Symmetrical cracking:
			var result = CrackUtils.CalculateFeatureVertexInfos(
				features, null, crackPointCalculator, options, inExtent, null);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(71, GetDistinctCrackPointLocationCount(result[0]));
			Assert.AreEqual(65, GetDistinctCrackPointLocationCount(result[1]));

			var resultGeometries = new Dictionary<IFeature, IGeometry>();
			CrackUtils.AddRemovePoints(result, resultGeometries, null, null);

			List<IFeature> updatedFeatures = resultGeometries
			                                 .Select(kvp => TestUtils.CreateMockFeature(kvp.Value))
			                                 .ToList();

			crackPointCalculator =
				CreateCrackPointCalculator(tolerance, inExtent);

			result = CrackUtils.CalculateFeatureVertexInfos(
				updatedFeatures, null, crackPointCalculator, options, inExtent, null);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(4, GetDistinctCrackPointLocationCount(result[0]));
			Assert.AreEqual(10, GetDistinctCrackPointLocationCount(result[1]));

			// A-symmetrical cracking:
			multipatch = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("ErrorGeom_20220615_135828_roof.xml"));

			mockRoof =
				TestUtils.CreateMockFeature(multipatch, 0.01, 0.001);

			footprint = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("ErrorGeom_20220615_135828_footprint.xml"));

			mockFootprint =
				TestUtils.CreateMockFeature(footprint, 0.01, 0.001);

			var featureVertexInfo = new FeatureVertexInfo(mockRoof, null, tolerance, tolerance);

			crackPointCalculator =
				CreateCrackPointCalculator(tolerance, inExtent);

			CrackUtils.AddCrackPoints(featureVertexInfo, mockFootprint, crackPointCalculator);
			Assert.AreEqual(71, GetDistinctCrackPointLocationCount(featureVertexInfo));

			resultGeometries = new Dictionary<IFeature, IGeometry>();
			CrackUtils.AddRemovePoints(result, resultGeometries, null, null);

			featureVertexInfo = new FeatureVertexInfo(mockFootprint, null, tolerance, tolerance);
			crackPointCalculator = CreateCrackPointCalculator(
				tolerance, inExtent);
			CrackUtils.AddCrackPoints(featureVertexInfo, mockRoof, crackPointCalculator);

			Assert.AreEqual(65, GetDistinctCrackPointLocationCount(featureVertexInfo));

			IntersectionUtils.UseCustomIntersect = customIntersectOrig;
		}

		private static CrackPointCalculator CreateCrackPointCalculator(double tolerance,
			[NotNull] IEnvelope inExtent, bool useSourceZ = true)
		{
			var crackPointCalculator = new CrackPointCalculator(
				tolerance, tolerance, false, useSourceZ,
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints, inExtent);

			return crackPointCalculator;
		}

		[Test]
		public void CalculateCrackPointPerformance()
		{
			// Using small-ish multipatch with 13 rings - the more rings the more extreme is the difference
			// OID 2555 in Tobias' test data set 1093_22_20140512.gdb
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchUncrackedBetweenRings.xml");

			const double tolerance = 0.0125;
			var featureVertexInfo = new FeatureVertexInfo(mockFeature, null, tolerance,
			                                              tolerance);

			var crackPointCalculator =
				new CrackPointCalculator(
					tolerance, tolerance, true, true, IntersectionPointOptions.IncludeLinearIntersectionEndpoints, null);

			crackPointCalculator.In3D = false;

			Stopwatch watch = Stopwatch.StartNew();
			crackPointCalculator.UseCustomIntersect = false;
			CrackUtils.AddGeometryPartIntersectionCrackPoints(featureVertexInfo,
			                                                  crackPointCalculator);

			watch.Stop();
			Console.WriteLine("ArcObjects: {0}", watch.ElapsedMilliseconds);

			featureVertexInfo = new FeatureVertexInfo(mockFeature, null, tolerance, tolerance);

			watch = Stopwatch.StartNew();
			crackPointCalculator.UseCustomIntersect = true;

			CrackUtils.AddGeometryPartIntersectionCrackPoints(featureVertexInfo,
			                                                  crackPointCalculator);

			watch.Stop();

			Console.WriteLine("GeomUtils: {0}", watch.ElapsedMilliseconds);

			// Output:
			// ArcObjects: 157
			// GeomUtils: 45
		}

		private static ISpatialReference GetLv95()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			((ISpatialReferenceTolerance) lv95).XYTolerance = 0.01;
			((ISpatialReferenceTolerance) lv95).ZTolerance = 0.01;

			((ISpatialReferenceResolution) lv95).XYResolution[true] = 0.001;
			((ISpatialReferenceResolution) lv95).ZResolution[true] = 0.001;
			return lv95;
		}

		private static IPoint TestCracking(IGeometry target, IPolygon source,
		                                   CrackPointCalculator crackPointCalculator,
		                                   int expectCrackPoints,
		                                   double expectedResultZ)
		{
			FeatureVertexInfo featureVertexInfo = Crack(source, target, crackPointCalculator,
			                                            out IFeature sourceFeature,
			                                            out List<IFeature> targetFeatures);

			if (expectCrackPoints == 0)
			{
				Assert.IsNull(featureVertexInfo.CrackPointCollection);
				return null;
			}

			Assert.IsNotNull(featureVertexInfo.CrackPointCollection);
			Assert.AreEqual(expectCrackPoints, featureVertexInfo.CrackPointCollection.PointCount);

			IPoint pointToInsert = featureVertexInfo.CrackPointCollection.Point[0];

			Assert.AreEqual(expectedResultZ, pointToInsert.Z);
			var result = new Dictionary<IFeature, IGeometry>();
			CrackUtils.AddRemovePoints(
				new List<FeatureVertexInfo> { featureVertexInfo }, result, null, null);

			IGeometry resultGeometry = result[sourceFeature];

			var resultPointCollection = (IPointCollection) resultGeometry;

			IList<int> found = GeometryUtils.FindVertexIndices(
				resultPointCollection, pointToInsert, 0.001);

			Assert.IsTrue(found.Count > 0);

			foreach (int foundIndex in found)
			{
				Assert.AreEqual(expectedResultZ, resultPointCollection.Point[foundIndex].Z);
			}

			sourceFeature.Shape = resultGeometry;
			sourceFeature.Store();

			featureVertexInfo = new FeatureVertexInfo(
				sourceFeature, null, crackPointCalculator.SnapTolerance,
				crackPointCalculator.MinimumSegmentLength);

			CrackUtils.AddTargetIntersectionCrackPoints(featureVertexInfo,
			                                            targetFeatures,
			                                            TargetFeatureSelection.VisibleFeatures,
			                                            crackPointCalculator, null);

			Assert.IsNull(featureVertexInfo.CrackPointCollection);

			return pointToInsert;
		}

		private static FeatureVertexInfo Crack(IPolygon source, IGeometry target,
		                                       CrackPointCalculator crackPointCalculator,
		                                       out IFeature sourceFeature,
		                                       out List<IFeature> targetFeatures)
		{
			IFeature targetFeature = TestUtils.CreateMockFeature(target);
			sourceFeature = TestUtils.CreateMockFeature(source);

			FeatureVertexInfo featureVertexInfo = new FeatureVertexInfo(
				sourceFeature, null, crackPointCalculator.SnapTolerance,
				crackPointCalculator.MinimumSegmentLength);

			targetFeatures = new List<IFeature> { targetFeature };

			CrackUtils.AddTargetIntersectionCrackPoints(featureVertexInfo,
			                                            targetFeatures,
			                                            TargetFeatureSelection.VisibleFeatures,
			                                            crackPointCalculator, null);
			return featureVertexInfo;
		}

		private static int GetDistinctCrackPointLocationCount(FeatureVertexInfo featureVertexInfo,
		                                                      bool in2D = false)
		{
			if (featureVertexInfo.CrackPointCollection == null)
			{
				return 0;
			}

			var copy = GeometryFactory.Clone((IGeometry) featureVertexInfo.CrackPointCollection);

			if (in2D)
			{
				GeometryUtils.MakeNonZAware(copy);
			}

			GeometryUtils.Simplify(copy);

			return ((IPointCollection) copy).PointCount;
		}
	}
}

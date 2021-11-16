using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public static class GeomRelationUtils
	{
		public static bool AreBoundsDisjoint([NotNull] IBoundedXY geometry1,
		                                     [NotNull] IBoundedXY geometry2,
		                                     double tolerance)
		{
			return AreBoundsDisjoint(
				geometry1.XMin, geometry1.YMin, geometry1.XMax, geometry1.YMax,
				geometry2.XMin, geometry2.YMin, geometry2.XMax, geometry2.YMax,
				tolerance);
		}

		public static bool AreBoundsDisjoint([NotNull] IBoundedXY geometry1,
		                                     double pointX, double pointY,
		                                     double tolerance)
		{
			return AreDisjoint(
				geometry1.XMin, geometry1.YMin, geometry1.XMax, geometry1.YMax,
				pointX, pointY, tolerance);
		}

		public static bool AreBoundsDisjoint(
			double box1XMin, double box1YMin, double box1XMax, double box1YMax,
			double box2XMin, double box2YMin, double box2XMax, double box2YMax,
			double tolerance)
		{
			if (box1XMax + tolerance < box2XMin)
			{
				return true;
			}

			if (box1XMin - tolerance > box2XMax)
			{
				return true;
			}

			if (box1YMax + tolerance < box2YMin)
			{
				return true;
			}

			if (box1YMin - tolerance > box2YMax)
			{
				return true;
			}

			return false;
		}

		public static bool AreDisjoint([NotNull] EnvelopeXY envelope,
		                               [NotNull] IPnt point,
		                               double tolerance)
		{
			return AreDisjoint(envelope.XMin, envelope.YMin,
			                   envelope.XMax, envelope.YMax,
			                   point.X, point.Y, tolerance);
		}

		public static bool AreDisjoint(
			double box1XMin, double box1YMin, double box1XMax, double box1YMax,
			double pointX, double pointY, double tolerance)
		{
			if (box1XMax + tolerance < pointX)
			{
				return true;
			}

			if (box1XMin - tolerance > pointX)
			{
				return true;
			}

			if (box1YMax + tolerance < pointY)
			{
				return true;
			}

			if (box1YMin - tolerance > pointY)
			{
				return true;
			}

			return false;
		}

		public static bool AreBoundsEqual(
			[NotNull] IBoundedXY geometry1,
			[NotNull] IBoundedXY geometry2,
			double tolerance)
		{
			return AreBoundsEqual(
				geometry1.XMin, geometry1.YMin, geometry1.XMax, geometry1.YMax,
				geometry2.XMin, geometry2.YMin, geometry2.XMax, geometry2.YMax,
				tolerance);
		}

		public static bool AreBoundsEqual(
			double box1XMin, double box1YMin, double box1XMax, double box1YMax,
			double box2XMin, double box2YMin, double box2XMax, double box2YMax,
			double tolerance)
		{
			if (! MathUtils.AreEqual(box1XMin, box2XMin, tolerance))
			{
				return false;
			}

			if (! MathUtils.AreEqual(box1YMin, box2YMin, tolerance))
			{
				return false;
			}

			if (! MathUtils.AreEqual(box1XMax, box2XMax, tolerance))
			{
				return false;
			}

			if (! MathUtils.AreEqual(box1YMax, box2YMax, tolerance))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines whether <see cref="geometry1"/> is contained in <see cref="geometry2"/>.
		/// </summary>
		/// <param name="geometry1">The test geometry.</param>
		/// <param name="geometry2">The containing geometry.</param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool IsContained([NotNull] IBoundedXY geometry1,
		                               [NotNull] IBoundedXY geometry2,
		                               double tolerance)
		{
			return IsContained(
				geometry1.XMin, geometry1.YMin, geometry1.XMax, geometry1.YMax,
				geometry2.XMin, geometry2.YMin, geometry2.XMax, geometry2.YMax,
				tolerance);
		}

		/// <summary>
		/// Determines whether box1 is contained in box2.
		/// </summary>
		/// <param name="box1XMin"></param>
		/// <param name="box1YMin"></param>
		/// <param name="box1XMax"></param>
		/// <param name="box1YMax"></param>
		/// <param name="box2XMin"></param>
		/// <param name="box2YMin"></param>
		/// <param name="box2XMax"></param>
		/// <param name="box2YMax"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool IsContained(
			double box1XMin, double box1YMin, double box1XMax, double box1YMax,
			double box2XMin, double box2YMin, double box2XMax, double box2YMax,
			double tolerance)
		{
			if (box1XMax + tolerance > box2XMax)
			{
				return false;
			}

			if (box1XMin - tolerance < box2XMin)
			{
				return false;
			}

			if (box1YMax + tolerance > box2YMax)
			{
				return false;
			}

			if (box1YMin - tolerance < box2YMin)
			{
				return false;
			}

			return true;
		}

		public static bool AreEqual<T>([NotNull] T pnt1, [NotNull] T pnt2,
		                               double xyTolerance, double zTolerance) where T : IPnt
		{
			double z1 = double.NaN;
			double z2 = double.NaN;
			if (double.IsNaN(zTolerance))
			{
				z1 = double.NaN;
				z2 = double.NaN;
			}
			else
			{
				if (pnt1 is Pnt3D a3D)
				{
					z1 = a3D.Z;
				}

				if (pnt2 is Pnt3D b3D)
				{
					z2 = b3D.Z;
				}
			}

			return AreEqual(pnt1.X, pnt1.Y, z1, pnt2.X, pnt2.Y, z2,
			                xyTolerance, zTolerance);
		}

		public static bool AreEqual(double x1, double y1, double z1,
		                            double x2, double y2, double z2,
		                            double xyTolerance, double zTolerance)
		{
			double dx = x1 - x2;
			double dy = y1 - y2;
			double dd = dx * dx + dy * dy;
			double xyToleranceSquared = xyTolerance * xyTolerance;

			if (dd <= xyToleranceSquared)
			{
				if (double.IsNaN(zTolerance))
				{
					// No Z tolerance given: ignore Z coords
					return true;
				}

				if (double.IsNaN(z1) && double.IsNaN(z2))
				{
					// Both Z coords are NaN:
					return true;
				}

				if (double.IsNaN(z1) || double.IsNaN(z2))
				{
					// One Z coord is valid, the other is NaN:
					return false;
				}

				double dz = Math.Abs(z1 - z2);
				return dz <= zTolerance;
			}

			return false;
		}

		public static bool IsWithinTolerance(IPnt testPoint, IPnt searchPoint, double tolerance,
		                                     bool useSearchCircle)
		{
			bool withinSearchBox = IsWithinBox(testPoint, searchPoint, tolerance);

			if (! withinSearchBox)
			{
				return false;
			}

			if (! useSearchCircle)
			{
				return true;
			}

			double distanceSquaredXY = GeomUtils.GetDistanceSquaredXY(searchPoint, testPoint);

			double searchToleranceSquared = tolerance * tolerance;

			return distanceSquaredXY <= searchToleranceSquared;
		}

		public static bool IsWithinBox(IPnt testPoint, IPnt searchBoxCenterPoint, double tolerance)
		{
			return
				MathUtils.AreEqual(testPoint.X, searchBoxCenterPoint.X, tolerance) &&
				MathUtils.AreEqual(testPoint.Y, searchBoxCenterPoint.Y, tolerance);
		}

		/// <summary>
		/// Determines whether the two sets of points occupy the same XY space (XYZ if the
		/// zTolerance is set and both have z values), i.e. the symmetric difference is empty
		/// (Clementini-style).
		/// </summary>
		/// <param name="multipoint1"></param>
		/// <param name="multipoint2"></param>
		/// <param name="xyTolerance"></param>
		/// <param name="zTolerance">The z tolerance</param>
		/// <returns></returns>
		public static bool AreEqual([NotNull] Multipoint<IPnt> multipoint1,
		                            [NotNull] Multipoint<IPnt> multipoint2,
		                            double xyTolerance,
		                            double zTolerance = double.NaN)
		{
			if (double.IsNaN(zTolerance))
			{
				return AreEqualXY(multipoint1, multipoint2, xyTolerance);
			}

			if (ReferenceEquals(multipoint1, multipoint2))
			{
				return true;
			}

			if (! AreBoundsEqual(multipoint1, multipoint2, xyTolerance))
			{
				return false;
			}

			HashSet<int> foundIndexes = new HashSet<int>();

			foreach (IPnt point in multipoint1.GetPoints())
			{
				bool anyFound = false;
				foreach (int foundIdx in
					multipoint2.FindPointIndexes(point, xyTolerance, true))
				{
					Pnt3D searchPoint = point as Pnt3D;
					Pnt3D foundPoint = multipoint2.GetPoint(foundIdx) as Pnt3D;

					double z1 = searchPoint?.Z ?? double.NaN;
					double z2 = foundPoint?.Z ?? double.NaN;
					if ((searchPoint == null) != (foundPoint == null))
					{
						// one is null, the other not
						continue;
					}

					// both nan or equal in Z
					if ((double.IsNaN(z1) && double.IsNaN(z2)) ||
					    MathUtils.AreEqual(z1, z2, zTolerance))
					{
						anyFound = true;

						foundIndexes.Add(foundIdx);
					}
				}

				if (! anyFound)
				{
					return false;
				}
			}

			// Check if all points have been found at some point:
			for (int i = 0; i < multipoint2.PointCount; i++)
			{
				if (! foundIndexes.Contains(i))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Determines whether the two sets of points occupy the same XY space, i.e. the symmetric
		/// difference is empty (clementini-style).
		/// </summary>
		/// <param name="multipoint1"></param>
		/// <param name="multipoint2"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool AreEqualXY([NotNull] Multipoint<IPnt> multipoint1,
		                              [NotNull] Multipoint<IPnt> multipoint2,
		                              double tolerance)
		{
			if (ReferenceEquals(multipoint1, multipoint2))
			{
				return true;
			}

			if (! AreBoundsEqual(multipoint1, multipoint2, tolerance))
			{
				return false;
			}

			HashSet<int> foundIndexes = new HashSet<int>();

			foreach (IPnt point in multipoint1.GetPoints())
			{
				bool anyFound = false;
				foreach (int foundIdx in
					multipoint2.FindPointIndexes(point, tolerance, true))
				{
					anyFound = true;

					foundIndexes.Add(foundIdx);
				}

				if (! anyFound)
				{
					return false;
				}
			}

			// Check if all points have been found at some point:
			for (int i = 0; i < multipoint2.PointCount; i++)
			{
				if (! foundIndexes.Contains(i))
				{
					return false;
				}
			}

			return true;
		}

		public static bool LinesContainXY([NotNull] ISegmentList segments,
		                                  [NotNull] IPnt testPoint,
		                                  double tolerance)
		{
			foreach (KeyValuePair<int, Line3D> segmentsAroundPoint in
				segments.FindSegments(testPoint, tolerance))
			{
				Line3D segment = segmentsAroundPoint.Value;

				if (segment.IntersectsPointXY(testPoint, tolerance))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether the specified closed polycurve contains (including the boundary) the
		/// specified test geometry. This method ignores the orientation.
		/// </summary>
		/// <param name="closedPolycurve"></param>
		/// <param name="targetSegments"></param>
		/// <param name="tolerance"></param>
		/// <param name="knownIntersections"></param>
		/// <returns></returns>
		public static bool PolycurveContainsXY(
			[NotNull] ISegmentList closedPolycurve,
			[NotNull] Linestring targetSegments,
			double tolerance,
			IEnumerable<SegmentIntersection> knownIntersections = null)
		{
			if (AreBoundsDisjoint(closedPolycurve, targetSegments, tolerance))
			{
				return false;
			}

			if (knownIntersections == null)
			{
				knownIntersections =
					SegmentIntersectionUtils.GetSegmentIntersectionsXY(
						closedPolycurve, targetSegments, tolerance);
			}

			// TODO: GeomUtils.IEnumerable<IntersectionPoint3D> GetIntersectionPointsWithDeviation() for better performance
			var intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints(closedPolycurve, targetSegments, tolerance,
				                                      knownIntersections, false);

			Pnt3D nonIntersectingTargetPnt =
				GetNonIntersectingTargetPoint(targetSegments, intersectionPoints);

			IEnumerable<Pnt3D> checkPoints = nonIntersectingTargetPnt != null
				                                 ? new[] {nonIntersectingTargetPnt}
				                                 : targetSegments.GetPoints();

			return checkPoints.All(p => PolycurveContainsXY(closedPolycurve, p, tolerance));

			// The check points are assumed not to be on the boundary!
		}

		/// <summary>
		/// Determines whether the specified closed polycurve contains (including the boundary) the
		/// specified test point. This method ignores the orientation.
		/// </summary>
		/// <param name="closedPolycurve"></param>
		/// <param name="testPoint"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool PolycurveContainsXY([NotNull] ISegmentList closedPolycurve,
		                                       [NotNull] IPnt testPoint,
		                                       double tolerance)
		{
			if (AreBoundsDisjoint(closedPolycurve, testPoint.X, testPoint.Y, tolerance))
			{
				return false;
			}

			// Test boundary:
			if (LinesContainXY(closedPolycurve, testPoint, tolerance))
			{
				return true;
			}

			if (! closedPolycurve.IsClosed)
			{
				return false;
			}

			return HasRayOddCrossingNumber(closedPolycurve, testPoint, tolerance);
		}

		/// <summary>
		/// Determines whether the test point is completely contained (true) or
		/// on the boundary of the specified ring (i.e. intersecting the linestring).
		/// </summary>
		/// <param name="closedRing">The containing ring.</param>
		/// <param name="testPoint">The test point.</param>
		/// <param name="tolerance">The tolerance.</param>
		/// <param name="disregardingOrientation">Whether the orientation should be disregarded. 
		/// If false, a point inside the ring is considered outside if the ring orientation is 
		/// counter-clockwise.</param>
		/// <returns>Null, if the point is on the boundary, true if the point is inside the ring.</returns>
		public static bool? AreaContainsXY([NotNull] Linestring closedRing,
		                                   [NotNull] IPnt testPoint,
		                                   double tolerance,
		                                   bool disregardingOrientation = false)
		{
			Assert.ArgumentCondition(closedRing.IsClosed, "Ring must be closed");

			if (AreBoundsDisjoint(closedRing, testPoint.X, testPoint.Y, tolerance))
			{
				return disregardingOrientation ? false : closedRing.ClockwiseOriented == false;
			}

			// Test boundary:
			if (LinesContainXY(closedRing, testPoint, tolerance))
			{
				return null;
			}

			bool result = HasRayOddCrossingNumber(closedRing, testPoint, tolerance);

			if (disregardingOrientation)
			{
				return result;
			}

			if (closedRing.ClockwiseOriented == false)
			{
				result = ! result;
			}

			return result;
		}

		/// <summary>
		/// Determines whether the test point is completely contained (true) or
		/// on the boundary of the specified rings (e.e. intersecting the boundary).
		/// </summary>
		/// <param name="closedRings">The containing rings.</param>
		/// <param name="testPoint"></param>
		/// <param name="tolerance"></param>
		/// <returns>Null, if the point is on the boundary, true if the point is inside the ring.</returns>
		public static bool? AreaContainsXY([NotNull] ISegmentList closedRings,
		                                   [NotNull] IPnt testPoint,
		                                   double tolerance)
		{
			Assert.ArgumentCondition(closedRings.IsClosed, "Rings must be closed");

			if (AreBoundsDisjoint(closedRings, testPoint.X, testPoint.Y, tolerance))
			{
				return false;
			}

			// Test boundary:
			if (LinesContainXY(closedRings, testPoint, tolerance))
			{
				return null;
			}

			return HasRayOddCrossingNumber(closedRings, testPoint, tolerance);
		}

		/// <summary>
		/// Determines whether the source ring contains the target.
		/// </summary>
		/// <param name="sourceRing"></param>
		/// <param name="target"></param>
		/// <param name="intersectionPoints"></param>
		/// <param name="tolerance"></param>
		/// <param name="disregardRingOrientation"></param>
		/// <returns></returns>
		public static bool? AreaContainsXY(
			[NotNull] Linestring sourceRing,
			[NotNull] Linestring target,
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPoints,
			double tolerance, bool disregardRingOrientation)
		{
			Pnt3D nonIntersectingTargetPnt =
				GetNonIntersectingTargetPoint(target, intersectionPoints);

			// Check if ring1 contains ring2 (or its check points):
			IEnumerable<Pnt3D> checkPoints = nonIntersectingTargetPnt != null
				                                 ? new[] {nonIntersectingTargetPnt}
				                                 : target.GetPoints();

			return Contains(sourceRing, checkPoints, tolerance, disregardRingOrientation);
		}

		/// <summary>
		/// Determines whether the source ring is contained within the target.
		/// </summary>
		/// <param name="sourceRing"></param>
		/// <param name="targetRing"></param>
		/// <param name="intersectionPoints"></param>
		/// <param name="tolerance"></param>
		/// <param name="disregardRingOrientation"></param>
		/// <returns></returns>
		public static bool? WithinAreaXY(
			[NotNull] Linestring sourceRing,
			[NotNull] Linestring targetRing,
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPoints,
			double tolerance, bool disregardRingOrientation)
		{
			Pnt3D nonIntersectingSourcePnt =
				GetNonIntersectingSourcePoint(sourceRing, intersectionPoints);

			IEnumerable<Pnt3D> checkPoints = nonIntersectingSourcePnt != null
				                                 ? new[] {nonIntersectingSourcePnt}
				                                 : sourceRing.GetPoints();

			return Contains(targetRing, checkPoints, tolerance, disregardRingOrientation);
		}

		public static bool SourceInteriorIntersectsXY([NotNull] Line3D line1,
		                                              [NotNull] Line3D line2,
		                                              double tolerance)
		{
			SegmentIntersection segmentIntersection =
				SegmentIntersection.CalculateIntersectionXY(
					0, 0, line1, line2, tolerance);

			return segmentIntersection.HasSourceInteriorIntersection;
		}

		public static bool InteriorIntersectXY([NotNull] RingGroup poly1,
		                                       [NotNull] RingGroup poly2,
		                                       double tolerance)
		{
			bool disjoint;
			bool touchXY = TouchesXY(poly1, poly2, tolerance, out disjoint);

			if (disjoint)
			{
				return false;
			}

			return ! touchXY;
		}

		/// <summary>
		/// Determines whether any of the rings in rings1 interior-intersects the other ring.
		/// If available, the spatial index of the other ring is used.
		/// </summary>
		/// <param name="rings1"></param>
		/// <param name="otherRing"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool InteriorIntersectXY([NotNull] IEnumerable<Linestring> rings1,
		                                       [NotNull] Linestring otherRing,
		                                       double tolerance)
		{
			foreach (Linestring ring1 in rings1)
			{
				if (InteriorIntersectXY(ring1, otherRing, tolerance))
				{
					return true;
				}
			}

			return false;
		}

		public static bool InteriorIntersectXY([NotNull] Linestring ring1,
		                                       [NotNull] Linestring ring2,
		                                       double tolerance,
		                                       bool disregardRingOrientation = false,
		                                       bool ring2CanHaveLinearSelfIntersections = false)
		{
			bool ringsAreDisjoint;
			bool touchXY = TouchesXY(ring1, ring2, tolerance,
			                         out ringsAreDisjoint,
			                         disregardRingOrientation,
			                         ring2CanHaveLinearSelfIntersections);

			if (ringsAreDisjoint)
			{
				return false;
			}

			return ! touchXY;
		}

		public static bool TouchesXY([NotNull] RingGroup poly1,
		                             [NotNull] RingGroup poly2,
		                             double tolerance,
		                             out bool disjoint)
		{
			bool exteriorRingsTouch =
				TouchesXY(poly1.ExteriorRing, poly2.ExteriorRing, tolerance, out disjoint);

			if (disjoint || exteriorRingsTouch)
			{
				return exteriorRingsTouch;
			}

			if (poly1.InteriorRingCount == 0 && poly2.InteriorRingCount == 0)
			{
				return false;
			}

			// Check if the outer ring touches an inner ring from the inside (assuming proper orientation -> from the outside)
			// and is disjoint from all other inner rings
			if (TouchesXY(poly1.ExteriorRing, poly2.InteriorRings, tolerance, out disjoint))
			{
				return true;
			}

			if (disjoint)
			{
				return false;
			}

			if (TouchesXY(poly2.ExteriorRing, poly1.InteriorRings, tolerance, out disjoint))
			{
				return true;
			}

			return false;
		}

		public static bool TouchesXY([NotNull] Linestring ring1,
		                             [NotNull] IEnumerable<Linestring> interiorRings,
		                             double tolerance,
		                             out bool disjoint)
		{
			disjoint = false;

			bool polyTouchesAnyInnerRing = false;

			foreach (Linestring interiorRing in interiorRings)
			{
				// NOTE: disjoint with interor ring means the outer ring is inside:
				bool ring1WithinInterior;
				if (TouchesXY(ring1, interiorRing, tolerance,
				              out ring1WithinInterior))
				{
					polyTouchesAnyInnerRing = true;
				}
				else if (ring1WithinInterior)
				{
					// assuming interior rings do not intersect each other: ring1 within inner ring -> disjoint
					disjoint = true;
				}
			}

			return polyTouchesAnyInnerRing;
		}

		/// <summary>
		/// Determines whether the provided rings touch in xy. If available, the spatial index of the second ring is used.
		/// </summary>
		/// <param name="ring1"></param>
		/// <param name="ring2"></param>
		/// <param name="tolerance"></param>
		/// <param name="ringsAreDisjoint">Whether the two rings are disjoint.</param>
		/// <param name="disregardRingOrientation">Whether the ring orientation can be used to determine
		/// the inside/outside. Use true if the rings are known not to be simple in terms of ring orientation.</param>
		/// <param name="ring2CanHaveLinearSelfIntersections">Whether the second ring can have self-intersections,
		/// for example because it is vertical. If true, it will be reported as touching if it does not intersect
		/// the interior of ring1, even if there are linear intersections in both directions. This does not exactly
		/// conform with Clementini logic.</param>
		/// <returns></returns>
		public static bool TouchesXY([NotNull] Linestring ring1,
		                             [NotNull] Linestring ring2,
		                             double tolerance,
		                             out bool ringsAreDisjoint,
		                             bool disregardRingOrientation = false,
		                             bool ring2CanHaveLinearSelfIntersections = false)
		{
			Assert.ArgumentCondition(ring1.IsClosed && ring2.IsClosed,
			                         "Both rings must be closed.");

			IEnumerable<SegmentIntersection> segmentIntersections =
				SegmentIntersectionUtils.GetSegmentIntersectionsXY(
					ring1, ring2, tolerance);

			ringsAreDisjoint = true;

			var allIntersections = new List<SegmentIntersection>();

			// Quick checks and list collection
			bool? linearIntersectionsInverted = null;
			foreach (SegmentIntersection intersection in segmentIntersections)
			{
				ringsAreDisjoint = false;

				if (intersection.SingleInteriorIntersectionFactor != null)
				{
					return false;
				}

				if (intersection.HasLinearIntersection)
				{
					//// TODO/Experimental: Test with more exception cases
					//if (intersection.IsPotentialPseudoLinearIntersection(
					//	    ring1[intersection.SourceIndex], ring2[intersection.TargetIndex],
					//	    tolerance))
					//{
					//	continue;
					//}
					if (! intersection.IsSegmentZeroLength2D)
					{
						if (! disregardRingOrientation &&
						    ! intersection.LinearIntersectionInOppositeDirection)
						{
							// Optimization if the ring orientation is known to be correct
							return false;
						}

						if (linearIntersectionsInverted == null)
						{
							linearIntersectionsInverted =
								intersection.LinearIntersectionInOppositeDirection;
						}
						else if (linearIntersectionsInverted.Value !=
						         intersection.LinearIntersectionInOppositeDirection &&
						         ! ring2CanHaveLinearSelfIntersections)
						{
							return false;
						}
					}
				}

				allIntersections.Add(intersection);
			}

			IList<IntersectionPoint3D> intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
				ring1, ring2, tolerance, allIntersections, false);

			if (HasSourceCrossingIntersections(ring1, ring2, intersectionPoints))
			{
				return false;
			}

			// No intersection or no deviation of target from source or all deviations to the same side
			bool contained = RingsContainEachOther(ring1, ring2, intersectionPoints, tolerance,
			                                       disregardRingOrientation,
			                                       ring2CanHaveLinearSelfIntersections);

			ringsAreDisjoint = ringsAreDisjoint && ! contained;

			return ! contained;
		}

		private static bool HasSourceCrossingIntersections(
			[NotNull] Linestring ring1,
			[NotNull] Linestring ring2,
			[NotNull] IList<IntersectionPoint3D> intersectionPoints)
		{
			var leftDeviationCount = 0;
			var rightDeviationCount = 0;

			// Determine on which side of the source the target joins the intersection point.
			// If different sides are detected -> no touching
			foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
			{
				if (intersectionPoint.Type == IntersectionPointType.Crossing)
				{
					return true;
				}

				bool? targetDeviatesToLeft =
					intersectionPoint.TargetDeviatesToLeftOfSourceRing(ring1, ring2);

				if (targetDeviatesToLeft == null)
				{
					continue;
				}

				if (targetDeviatesToLeft.Value)
					leftDeviationCount++;
				else
					rightDeviationCount++;

				if (leftDeviationCount > 0 && rightDeviationCount > 0)
				{
					return true;
				}
			}

			return false;
		}

		private static bool RingsContainEachOther(
			[NotNull] Linestring ring1,
			[NotNull] Linestring ring2,
			[NotNull] IList<IntersectionPoint3D> intersectionPoints,
			double tolerance,
			bool disregardRingOrientation,
			bool ring2IsVertical = false)
		{
			bool? ring1ContainsRing2 =
				AreaContainsXY(ring1, ring2, intersectionPoints, tolerance,
				               disregardRingOrientation);

			if (ring1ContainsRing2 == true)
			{
				return true;
			}

			bool? ring2ContainsRing1 = false;

			if (! ring2IsVertical)
			{
				ring2ContainsRing1 = WithinAreaXY(ring1, ring2, intersectionPoints, tolerance,
				                                  disregardRingOrientation);

				if (ring2ContainsRing1 == true)
				{
					return true;
				}
			}

			if (ring1ContainsRing2 == null && ring2ContainsRing1 == null)
			{
				// All points on each other's boundary
				return disregardRingOrientation ||
				       ring1.ClockwiseOriented == ring2.ClockwiseOriented;
			}

			return false;
		}

		private static Pnt3D GetNonIntersectingSourcePoint(
			[NotNull] Linestring sourceRing,
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPoints)
		{
			foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
			{
				if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionIntermediate)
				{
					continue;
				}

				if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart &&
				    MathUtils.AreEqual(intersectionPoint.VirtualSourceVertex, 0))
				{
					// Do not use the first point, there might be no proper deviation 
					// because it is actually an intermediate intersection point in a closed ring.
					continue;
				}

				Pnt3D nonIntersectingSourcePnt =
					intersectionPoint.GetNonIntersectingSourcePoint(sourceRing, 0.5);

				return nonIntersectingSourcePnt;
			}

			return null;
		}

		private static Pnt3D GetNonIntersectingTargetPoint(
			Linestring targetRing,
			IEnumerable<IntersectionPoint3D> intersectionPoints)
		{
			foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
			{
				if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionIntermediate)
				{
					continue;
				}

				if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart &&
				    MathUtils.AreEqual(intersectionPoint.VirtualSourceVertex, 0))
				{
					// Do not use the first point, there might be no proper deviation 
					// because it is actually an intermediate intersection point in a closed ring.
					continue;
				}

				Pnt3D nonIntersectingTargetPnt =
					intersectionPoint.GetNonIntersectingTargetPoint(targetRing, 0.5);

				return nonIntersectingTargetPnt;
			}

			return null;
		}

		private static bool? Contains([NotNull] Linestring containingRing,
		                              [NotNull] IEnumerable<Pnt3D> allPoints,
		                              double tolerance,
		                              bool disregardRingOrientation)
		{
			if (containingRing.SegmentCount == 0)
			{
				return false;
			}

			var anyPoint = false;
			var allOnBoundary = true;
			foreach (Pnt3D containedPoint in allPoints)
			{
				anyPoint = true;

				bool? isPointWithinXY = AreaContainsXY(
					containingRing, containedPoint, tolerance, disregardRingOrientation);

				if (isPointWithinXY != null)
				{
					allOnBoundary = false;

					if (! isPointWithinXY.Value)
					{
						return false;
					}
				}
			}

			if (anyPoint && allOnBoundary)
			{
				return null;
			}

			return true;
		}

		/// <summary>
		/// Determines whether the horizontal ray from the test point to XMin crosses
		/// the specified segments an odd number of times.
		/// NOTE: For test points exactly on the boundary the result depends on whether
		/// the the point is on the right or the left boundary!
		/// </summary>
		/// <param name="segments"></param>
		/// <param name="testPoint"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		private static bool HasRayOddCrossingNumber([NotNull] ISegmentList segments,
		                                            [NotNull] IPnt testPoint,
		                                            double tolerance)
		{
			bool result = false;

			// Get the intersecting segments along the horizontal line from XMin to the testPoint
			IEnumerable<KeyValuePair<int, Line3D>> intersectingSegments =
				segments.FindSegments(segments.XMin, testPoint.Y, testPoint.X, testPoint.Y,
				                      tolerance);

			foreach (KeyValuePair<int, Line3D> path2Segment in intersectingSegments.OrderBy(
				kvp => kvp.Key))
			{
				Line3D segment = path2Segment.Value;

				Pnt3D previous = segment.StartPoint;
				Pnt3D vertex = segment.EndPoint;

				if (vertex.Y < testPoint.Y && previous.Y >= testPoint.Y || // downward crossing
				    previous.Y < testPoint.Y && vertex.Y >= testPoint.Y) // upward crossing
				{
					double dX = previous.X - vertex.X;
					double dY = previous.Y - vertex.Y;

					double y = testPoint.Y - vertex.Y;

					if (vertex.X + y * dX / dY < testPoint.X)
					{
						result = ! result;
					}
				}
			}

			return result;
		}
	}
}

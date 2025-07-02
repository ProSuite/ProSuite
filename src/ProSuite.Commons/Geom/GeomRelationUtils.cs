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

		public static bool EnvelopeInteriorIntersects(
			double boxXMin, double boxYMin, double boxXMax, double boxYMax,
			double pointX, double pointY, double tolerance)
		{
			// x|y must be within the box minus the tolerance:

			if (pointX > boxXMin + tolerance &&
			    pointX < boxXMax - tolerance &&
			    pointY > boxYMin + tolerance &&
			    pointY < boxYMax - tolerance)
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
		/// Determines whether the bounding envelope of <paramref name="geometry1"/> is contained
		/// in the bounding envelope of <paramref name="geometry2"/>.
		/// </summary>
		/// <param name="geometry1">The test geometry.</param>
		/// <param name="geometry2">The containing geometry.</param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static bool AreBoundsContained([NotNull] IBoundedXY geometry1,
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
		                                  [NotNull] ICoordinates testPoint,
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

		public static bool LinesInteriorIntersectXY([NotNull] ISegmentList segments,
		                                            [NotNull] IPnt testPoint,
		                                            double tolerance)
		{
			foreach (KeyValuePair<int, Line3D> segmentsAroundPoint in
			         segments.FindSegments(testPoint, tolerance))
			{
				Line3D segment = segmentsAroundPoint.Value;

				if (segment.IntersectsPointXY(testPoint, tolerance))
				{
					// Unless it is a start/end point

					for (int i = 0; i < segments.PartCount; i++)
					{
						Linestring part = segments.GetPart(i);

						if (! part.IsEmpty &&
						    ! part.StartPoint.EqualsXY(testPoint, tolerance) &&
						    ! part.EndPoint.EqualsXY(testPoint, tolerance))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static bool HaveLinearIntersectionsXY([NotNull] ISegmentList segments1,
		                                             [NotNull] ISegmentList segments2,
		                                             double tolerance)
		{
			IEnumerable<SegmentIntersection> intersections =
				SegmentIntersectionUtils.GetSegmentIntersectionsXY(
					segments1, segments2, tolerance);

			foreach (SegmentIntersection linearIntersection in intersections)
			{
				Line3D line = linearIntersection.TryGetIntersectionLine(segments1);

				if (line == null)
				{
					continue;
				}

				if (line.Length2D <= tolerance)
				{
					// TODO: Sum length of consecutive sub-tolerance lines
					continue;
				}

				return true;
			}

			return false;
		}

		#region AreaContains, IsContained

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
				                                 ? new[] { nonIntersectingTargetPnt }
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
		                                       [NotNull] ICoordinates testPoint,
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
		/// Determines whether the specified closed polycurve contains (including the boundary) the
		/// specified test geometry.
		/// </summary>
		/// <param name="closedPolycurve">The closed and properly oriented polycurve.</param>
		/// <param name="targetSegments">The target curve to be checked whether it is contained or not.</param>
		/// <param name="tolerance"></param>
		/// <param name="intersectionPoints">The known intersection points, including linear intersection
		/// breaks at the start end point (if congruence should be detected correctly).</param>
		/// <param name="filterTargetByPartIndex">Specify the target part, that should be checked.
		/// If null, the entire target geometry is checked.</param>
		/// <param name="intersectionClusters"></param>
		/// <returns>true, if the <paramref name="targetSegments"/> are contained inside the
		/// <paramref name="closedPolycurve"/>.
		/// false, if the <paramref name="targetSegments"/> is not contained inside the
		/// <paramref name="closedPolycurve"/>.
		/// null, if the <paramref name="targetSegments"/> are congruent with the
		/// <paramref name="closedPolycurve"/></returns>
		public static bool? AreaContainsXY(
			[NotNull] ISegmentList closedPolycurve,
			[NotNull] ISegmentList targetSegments,
			double tolerance,
			IList<IntersectionPoint3D> intersectionPoints = null,
			int? filterTargetByPartIndex = null,
			IntersectionClusters intersectionClusters = null)
		{
			if (closedPolycurve.IsEmpty)
			{
				return false;
			}

			if (targetSegments.IsEmpty)
			{
				return true;
			}

			Assert.True(closedPolycurve.IsClosed, "Input containing polygon is not closed.");

			if (AreBoundsDisjoint(closedPolycurve, targetSegments, tolerance))
			{
				return false;
			}

			Predicate<IntersectionPoint3D> predicate =
				filterTargetByPartIndex != null
					? new Predicate<IntersectionPoint3D>(
						i => i.TargetPartIndex == filterTargetByPartIndex)
					: null;

			intersectionPoints =
				GetRealIntersectionPoints(closedPolycurve, targetSegments, tolerance,
				                          intersectionPoints, predicate);

			// if there is no intersection, the boundaries do not intersect: check a single point
			if (intersectionPoints.Count == 0)
			{
				int partIndex = filterTargetByPartIndex ?? 0;
				Pnt3D anyPoint = targetSegments.GetPart(partIndex).GetSegment(0).StartPoint;

				return AreaContainsXY(closedPolycurve, anyPoint, tolerance);
			}

			// First check the non-touching points. If there is any left-side deviation of the target
			// it is not contained. If there are only right-side deviations it is contained.
			List<IntersectionPoint3D> touchPoints = new List<IntersectionPoint3D>();
			bool hasAnyRightSideDeviation = false;
			foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
			{
				if (intersectionPoint.Type == IntersectionPointType.TouchingInPoint)
				{
					// Touching points can be misleading because it could be a different part that
					// touches the target ring from the outside but the target could nevertheless
					// be contained within the main part of the source.
					touchPoints.Add(intersectionPoint);
					continue;
				}

				DetermineTargetDeviationAtIntersection(
					intersectionPoint, closedPolycurve, targetSegments, tolerance,
					intersectionClusters,
					out bool hasRightSideDeviation, out bool hasLeftSideDeviation);

				if (hasLeftSideDeviation)
				{
					return false;
				}

				hasAnyRightSideDeviation |= hasRightSideDeviation;
			}

			// There were boundary intersections, but none has a target deviation to the left
			if (hasAnyRightSideDeviation)
			{
				return true;
			}

			if (touchPoints.Count == 0)
			{
				// No deviations to the left nor to the right -> congruent
				return null;
			}

			// There are only touch points, now determine on which side:
			if (touchPoints.Count > 1 &&
			    AreTouchingExteriorAndInteriorRings(touchPoints, closedPolycurve,
			                                        ip => ip.SourcePartIndex))
			{
				// Special case:
				// An interior ring could be touching its exterior ring and the same point is the
				// touch point of the target ring. Just checking the deviation from the touch
				// point can lead to wrong results -> Use point-in-polygon check.
				int targetPartIndex = touchPoints.First().TargetPartIndex;
				Linestring targetRing = targetSegments.GetPart(targetPartIndex);

				Pnt3D nonIntersectingTargetPnt =
					GetNonIntersectingTargetPoint(
						targetRing,
						intersectionPoints.Where(ip => ip.TargetPartIndex == targetPartIndex));

				Assert.NotNull(nonIntersectingTargetPnt,
				               $"No point to check in target ring {targetPartIndex}.");

				return PolycurveContainsXY(closedPolycurve, nonIntersectingTargetPnt, tolerance);
			}

			// No decisive deviation so far -> use the touch points
			return IsTargetTouchingFromInside(closedPolycurve, targetSegments, touchPoints,
			                                  tolerance);
		}

		private static bool AreTouchingExteriorAndInteriorRings(
			[NotNull] IList<IntersectionPoint3D> touchPoints,
			[NotNull] ISegmentList rings,
			[NotNull] Func<IntersectionPoint3D, int> getPartFunc)
		{
			// TODO: This method could be simplified or even removed if we had
			// the IntersectionPointNavigator with its knowledge about dupicate/clustered
			// intersections. We could then do this check on a per touch point basis.
			List<int> partIndexes = touchPoints.Select(getPartFunc).ToList();

			if (partIndexes.Distinct().Count() == 1)
			{
				// all from the same part
				return false;
			}

			bool allHaveSameOrientation =
				partIndexes.Select(i => rings.GetPart(i).ClockwiseOriented)
				           .Distinct().Count() == 1;

			return ! allHaveSameOrientation;
		}

		/// <summary>
		/// Determines whether the target ring is touching the source area from the inside in one
		/// of the provided touchPoints. TODO: Duplication with above method
		/// -> Use record for left/right deviation, provide a Func and the grouping
		/// </summary>
		/// <param name="closedPolycurve"></param>
		/// <param name="targetSegments"></param>
		/// <param name="touchPoints"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		private static bool? IsTargetTouchingFromInside(
			ISegmentList closedPolycurve,
			ISegmentList targetSegments,
			IEnumerable<IntersectionPoint3D> touchPoints,
			double tolerance)
		{
			// This must be done per source ring to avoid getting a left side deviation from a different
			// touching source ring despite the target being fully within the adjacent source ring.
			bool hasAnyLeftSideDeviation = false;

			foreach (IGrouping<int, IntersectionPoint3D> intersectionPointsPerPart in
			         touchPoints.GroupBy(i => i.SourcePartIndex))
			{
				bool hasAnyRightSideDeviation = false;
				foreach (IntersectionPoint3D intersectionPoint in intersectionPointsPerPart)
				{
					DetermineTargetDeviationAtIntersection(intersectionPoint, closedPolycurve,
					                                       targetSegments, tolerance, null,
					                                       out bool hasRightSideDeviation,
					                                       out bool hasLeftSideDeviation);

					if (hasLeftSideDeviation)
					{
						hasAnyLeftSideDeviation = true;
					}

					hasAnyRightSideDeviation |= hasRightSideDeviation;
				}

				if (hasAnyRightSideDeviation)
				{
					// Completely within this source ring: contained
					return true;
				}
			}

			// No source ring had right-side-only touch points. It must be outside if there was
			// any touch point with left-side deviation.
			return ! hasAnyLeftSideDeviation;
		}

		private static bool? IsSourceTouchingFromInside(ISegmentList sourceArea,
		                                                ISegmentList targetArea,
		                                                IEnumerable<IntersectionPoint3D>
			                                                touchPoints,
		                                                double tolerance)
		{
			// This must be done per target ring to avoid getting a left side deviation from a different
			// touching target ring despite the source being fully within the adjacent target ring.
			bool hasAnyLeftSideDeviation = false;

			foreach (IGrouping<int, IntersectionPoint3D> intersectionPointsPerPart in
			         touchPoints.GroupBy(i => i.TargetPartIndex))
			{
				bool hasAnyRightSideDeviation = false;
				foreach (IntersectionPoint3D intersectionPoint in intersectionPointsPerPart)
				{
					DetermineSourceDeviationAtIntersection(intersectionPoint, sourceArea,
					                                       targetArea, tolerance,
					                                       out bool hasRightSideDeviation,
					                                       out bool hasLeftSideDeviation);

					if (hasLeftSideDeviation)
					{
						hasAnyLeftSideDeviation = true;
					}

					hasAnyRightSideDeviation |= hasRightSideDeviation;
				}

				if (hasAnyRightSideDeviation)
				{
					// Completely within this target ring: contained
					return true;
				}
			}

			// No target ring had right-side-only touch points. It must be outside if there was
			// any touch point with left-side deviation.
			return ! hasAnyLeftSideDeviation;
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
		                                   [NotNull] ICoordinates testPoint,
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
		/// on the boundary of the specified rings (i.e. intersecting the boundary).
		/// </summary>
		/// <param name="closedRings">The containing rings.</param>
		/// <param name="testPoint"></param>
		/// <param name="tolerance"></param>
		/// <returns>Null, if the point is on the boundary, true if the point is inside the ring.</returns>
		public static bool? AreaContainsXY([NotNull] ISegmentList closedRings,
		                                   [NotNull] ICoordinates testPoint,
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
		/// Determines whether the source ring contains the target. The source ring can be negative.
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
			List<IntersectionPoint3D> intersectionPointList = intersectionPoints.ToList();

			if (! disregardRingOrientation && sourceRing.ClockwiseOriented == true)
			{
				return AreaContainsXY(sourceRing, target, tolerance, intersectionPointList);
			}

			if (sourceRing.IsEmpty)
			{
				return false;
			}

			if (sourceRing.IsEmpty)
			{
				return true;
			}

			Assert.True(sourceRing.IsClosed, "Input source ring is not closed.");

			if (AreBoundsDisjoint(sourceRing, target, tolerance))
			{
				if (disregardRingOrientation)
				{
					return false;
				}

				return sourceRing.ClockwiseOriented == false;
			}

			if (intersectionPointList.Count == 0)
			{
				Pnt3D anyPoint = target.GetSegment(0).StartPoint;

				bool? pointContained =
					AreaContainsXY(sourceRing, anyPoint, tolerance, disregardRingOrientation);

				if (pointContained == null)
				{
					// Make sure we have not used the (filtered out) start/end point intersection
					anyPoint = target.GetSegment(0).EndPoint;
					pointContained =
						AreaContainsXY(sourceRing, anyPoint, tolerance, disregardRingOrientation);
				}

				return pointContained;
			}

			bool? properOrientationResult =
				AreaContainsXY(sourceRing, target, tolerance, intersectionPointList);

			if (! disregardRingOrientation || sourceRing.ClockwiseOriented == true)
			{
				return properOrientationResult;
			}

			if (properOrientationResult == null)
			{
				return null;
			}

			// The source ring is an island. In case the proper orientation result is false, the
			// target is actually inside the ring:
			return properOrientationResult == false;
		}

		/// <summary>
		/// Determines whether the specified closed polycurve contains (including the boundary) the
		/// specified test geometry.
		/// </summary>
		/// <param name="containedSource">The source curve to be checked whether it is contained or not.</param>
		/// <param name="containingClosedTarget">The closed and properly oriented polycurve.</param>
		/// <param name="tolerance"></param>
		/// <param name="intersectionPoints">The known intersection points between source and target,
		/// including linear intersection breaks at the start end point (if congruence should be
		/// detected correctly).</param>
		/// <param name="filterSourceByPartIndex">Specify the source (contained) part, that should be
		/// checked. If null, the entire source geometry is checked.</param>
		/// <returns>true, if the <paramref name="containedSource"/> is contained inside the
		/// <paramref name="containingClosedTarget"/>.
		/// false, if the <paramref name="containedSource"/> is not contained inside the
		/// <paramref name="containingClosedTarget"/>.
		/// null, if <paramref name="containingClosedTarget"/> is congruent with <paramref name="containedSource"/></returns>
		public static bool? IsContainedXY(
			[NotNull] ISegmentList containedSource,
			[NotNull] ISegmentList containingClosedTarget,
			double tolerance,
			IList<IntersectionPoint3D> intersectionPoints = null,
			int? filterSourceByPartIndex = null)
		{
			if (containingClosedTarget.IsEmpty)
			{
				return false;
			}

			if (containedSource.IsEmpty)
			{
				return true;
			}

			Assert.True(containingClosedTarget.IsClosed, "Input within-polygon is not closed.");

			if (AreBoundsDisjoint(containedSource, containingClosedTarget, tolerance))
			{
				return false;
			}

			Predicate<IntersectionPoint3D> predicate =
				filterSourceByPartIndex != null
					? new Predicate<IntersectionPoint3D>(
						i => i.SourcePartIndex == filterSourceByPartIndex)
					: null;

			intersectionPoints = GetRealIntersectionPoints(containedSource, containingClosedTarget,
			                                               tolerance, intersectionPoints,
			                                               predicate);

			// TODO: This can be wrong if a short linear intersection has been filtered out as pseudo-breaks
			//       and the respective point is selected because it is the start point -> null
			//       Either we need to be less aggressive with pseudo-break filtering or more points
			//       need to be tested (ideally one that has no intersection whatsoever).
			// if there is no real intersection, the boundaries do not intersect at all or everywhere
			if (intersectionPoints.Count == 0)
			{
				int partIndex = filterSourceByPartIndex ?? 0;
				Pnt3D anyPoint = containedSource.GetPart(partIndex).GetSegment(0).StartPoint;

				return AreaContainsXY(containingClosedTarget, anyPoint, tolerance);
			}

			// TODO: Duplication with AreaContainsXY!
			List<IntersectionPoint3D> touchPoints = new List<IntersectionPoint3D>();
			bool hasAnyRightSideDeviation = false;
			foreach (IntersectionPoint3D intersectionPoint in intersectionPoints)
			{
				if (intersectionPoint.Type == IntersectionPointType.TouchingInPoint)
				{
					// Touching points can be misleading because it could be a different target part
					// that touches the source ring from the outside but the source could
					// nevertheless be contained within the main part of the target.
					touchPoints.Add(intersectionPoint);
					continue;
				}

				DetermineSourceDeviationAtIntersection(intersectionPoint, containedSource,
				                                       containingClosedTarget, tolerance,
				                                       out bool hasRightSideDeviation,
				                                       out bool hasLeftSideDeviation);
				if (hasLeftSideDeviation)
				{
					return false;
				}

				hasAnyRightSideDeviation |= hasRightSideDeviation;
			}

			// There were boundary intersections, but none has a target deviation to the left
			if (hasAnyRightSideDeviation)
			{
				return true;
			}

			if (touchPoints.Count == 0)
			{
				// No deviations to the left nor to the right -> congruent
				return null;
			}

			// No decisive deviation so far -> use the touch points
			return IsSourceTouchingFromInside(containedSource, containingClosedTarget, touchPoints,
			                                  tolerance);
		}

		/// <summary>
		/// Determines whether the source ring is contained within the target.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="containingTargetRing"></param>
		/// <param name="intersectionPoints"></param>
		/// <param name="tolerance"></param>
		/// <param name="disregardRingOrientation"></param>
		/// <returns></returns>
		private static bool? IsWithinAreaXY(
			[NotNull] Linestring source,
			[NotNull] Linestring containingTargetRing,
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPoints,
			double tolerance, bool disregardRingOrientation)
		{
			var intersectionPointList = intersectionPoints.ToList();

			if (! disregardRingOrientation && containingTargetRing.ClockwiseOriented == true)
			{
				return IsContainedXY(source, containingTargetRing, tolerance,
				                     intersectionPointList);
			}

			if (containingTargetRing.IsEmpty)
			{
				return false;
			}

			if (source.IsEmpty)
			{
				return true;
			}

			Assert.True(containingTargetRing.IsClosed, "Input containing-ring is not closed.");

			if (AreBoundsDisjoint(source, containingTargetRing, tolerance))
			{
				if (disregardRingOrientation)
				{
					return false;
				}

				return containingTargetRing.ClockwiseOriented == false;
			}

			if (intersectionPointList.Count == 0)
			{
				Pnt3D anyPoint = source.GetSegment(0).StartPoint;

				return AreaContainsXY(containingTargetRing, anyPoint, tolerance,
				                      disregardRingOrientation);
			}

			bool? properOrientationResult =
				IsContainedXY(source, containingTargetRing, tolerance, intersectionPointList);

			if (! disregardRingOrientation || containingTargetRing.ClockwiseOriented != false)
			{
				return properOrientationResult;
			}

			// The target ring is an island. In case the proper orientation result is false, the
			// target is source is actually inside the ring:
			if (properOrientationResult == null)
			{
				return null;
			}

			return properOrientationResult != true;
		}

		#endregion

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
		                                       double tolerance,
		                                       bool disregardRingOrientation = false,
		                                       bool ring2CanHaveLinearSelfIntersections = false)
		{
			bool disjoint;
			bool touchXY = TouchesXY(poly1, poly2, tolerance, out disjoint,
			                         disregardRingOrientation, ring2CanHaveLinearSelfIntersections);

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
		                             out bool disjoint,
		                             bool disregardRingOrientation = false,
		                             bool ring2CanHaveLinearSelfIntersections = false)
		{
			bool exteriorRingsTouch =
				TouchesXY(poly1.ExteriorRing, poly2.ExteriorRing, tolerance, out disjoint,
				          disregardRingOrientation, ring2CanHaveLinearSelfIntersections);

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
			if (TouchesXY(poly1.ExteriorRing, poly2.InteriorRings, tolerance, out disjoint,
			              disregardRingOrientation, ring2CanHaveLinearSelfIntersections))
			{
				return true;
			}

			if (disjoint)
			{
				return false;
			}

			if (TouchesXY(poly2.ExteriorRing, poly1.InteriorRings, tolerance, out disjoint,
			              disregardRingOrientation, ring2CanHaveLinearSelfIntersections))
			{
				return true;
			}

			return false;
		}

		public static bool TouchesXY([NotNull] Linestring ring1,
		                             [NotNull] IEnumerable<Linestring> interiorRings,
		                             double tolerance,
		                             out bool disjoint,
		                             bool disregardRingOrientation = false,
		                             bool ring2CanHaveLinearSelfIntersections = false)
		{
			disjoint = false;

			bool polyTouchesAnyInnerRing = false;

			foreach (Linestring interiorRing in interiorRings)
			{
				// NOTE: disjoint with interior ring means the outer ring is inside:
				bool ring1WithinInterior;
				if (TouchesXY(ring1, interiorRing, tolerance, out ring1WithinInterior,
				              disregardRingOrientation, ring2CanHaveLinearSelfIntersections))
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

			// Determine if the second ring really is partially or fully vertical:
			bool ring2HasLinarSelfIntersections =
				ring2CanHaveLinearSelfIntersections &&
				GeomTopoOpUtils.GetLinearSelfIntersectionsXY(ring2, tolerance).Any();

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
					// Zero-length segment intersections have random opposite direction property values!
					if (! intersection.IsSegmentZeroLength2D)
					{
						if (! ring2HasLinarSelfIntersections &&
						    ! intersection.LinearIntersectionInOppositeDirection)
						{
							// Optimization if the ring orientation is known to be correct or both
							// rings have the same orientation.
							if (! disregardRingOrientation ||
							    ring1.ClockwiseOriented != null &&
							    ring1.ClockwiseOriented == ring2.ClockwiseOriented)
							{
								return false;
							}
						}

						if (linearIntersectionsInverted == null)
						{
							linearIntersectionsInverted =
								intersection.LinearIntersectionInOppositeDirection;
						}
						else if (linearIntersectionsInverted.Value !=
						         intersection.LinearIntersectionInOppositeDirection &&
						         ! ring2HasLinarSelfIntersections)
						{
							return false;
						}
					}
				}

				allIntersections.Add(intersection);
			}

			IList<IntersectionPoint3D> intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
				ring1, ring2, tolerance, allIntersections, false);

			if (HasSourceCrossingIntersections(ring1, ring2, intersectionPoints,
			                                   ring2HasLinarSelfIntersections, tolerance))
			{
				return false;
			}

			// No intersection or no deviation of target from source or all deviations to the same side
			bool contained = RingsContainEachOther(ring1, ring2, intersectionPoints, tolerance,
			                                       disregardRingOrientation,
			                                       ring2HasLinarSelfIntersections);

			ringsAreDisjoint = ringsAreDisjoint && ! contained;

			return ! contained;
		}

		private static bool HasSourceCrossingIntersections(
			[NotNull] Linestring ring1,
			[NotNull] Linestring ring2,
			[NotNull] IList<IntersectionPoint3D> intersectionPoints,
			bool ring2HasSelfIntersections,
			double tolerance)
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

				if (ring2HasSelfIntersections)
				{
					// Left/right determination cannot be used for vertical rings
					continue;
				}

				bool? continuesToRightSide =
					intersectionPoint.TargetContinuesToRightSide(ring1, ring2, tolerance);

				bool? arrivesFromRightSide =
					intersectionPoint.TargetArrivesFromRightSide(ring1, ring2, tolerance);

				bool? targetDeviatesToLeft = null;

				if (continuesToRightSide == true || arrivesFromRightSide == true)
				{
					targetDeviatesToLeft = false;
				}
				else if (continuesToRightSide == false || arrivesFromRightSide == false)
				{
					targetDeviatesToLeft = true;
				}

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

			// TODO: Simplify ring2 and check contains?
			if (! ring2IsVertical)
			{
				//ring2ContainsRing1 = IsContainedXY(ring1, ring2, tolerance, intersectionPoints,

				//									disregardRingOrientation);

				//if (ring2ContainsRing1 == true)
				//{
				//	return true;
				//}

				ring2ContainsRing1 = IsWithinAreaXY(ring1, ring2, intersectionPoints, tolerance,
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

				if (nonIntersectingTargetPnt != null)
				{
					return nonIntersectingTargetPnt;
				}
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
		                                            [NotNull] ICoordinates testPoint,
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

		private static void DetermineTargetDeviationAtIntersection(
			[NotNull] IntersectionPoint3D intersection,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			double tolerance,
			[CanBeNull] IntersectionClusters intersectionClusters,
			out bool hasRightSideDeviation,
			out bool hasLeftSideDeviation)
		{
			if (intersection.Type == IntersectionPointType.Crossing &&
			    ! intersection.DisallowTargetForward &&
			    ! intersection.DisallowTargetBackward &&
			    ! intersection.DisallowSourceForward &&
			    ! intersection.DisallowSourceBackward)
			{
				// Shortcut:
				hasLeftSideDeviation = true;
				hasRightSideDeviation = true;

				return;
			}

			// If there is a cluster at a spike (that would collapse when cracking) treat it as
			// touch point, but determine the deviations on the proper intersection points:
			DetermineRelevantIntersections(intersection, intersectionClusters, target,
			                               out IntersectionPoint3D targetContinuationPoint,
			                               out IntersectionPoint3D targetArrivalPoint);

			// If the target arrives or continues to the outside (i.e. left side) then it's not contained
			bool? continuesToRightSide =
				targetContinuationPoint.TargetContinuesToRightSide(source, target, tolerance);

			hasLeftSideDeviation =
				continuesToRightSide == false && ! targetContinuationPoint.DisallowTargetForward;
			hasRightSideDeviation =
				continuesToRightSide == true && ! targetContinuationPoint.DisallowTargetForward;

			bool? arrivesFromRightSide =
				targetArrivalPoint.TargetArrivesFromRightSide(source, target, tolerance);

			if (arrivesFromRightSide == false && ! targetArrivalPoint.DisallowTargetBackward)
			{
				hasLeftSideDeviation = true;
			}

			if (arrivesFromRightSide == true && ! targetArrivalPoint.DisallowTargetBackward)
			{
				hasRightSideDeviation = true;
			}
		}

		/// <summary>
		/// Determines the intersection points that are relevant to determine the deviation of the target
		/// or backward direction.
		/// </summary>
		/// <param name="intersection"></param>
		/// <param name="intersectionClusters"></param>
		/// <param name="target"></param>
		/// <param name="targetContinuationPoint"></param>
		/// <param name="targetArrivalPoint"></param>
		private static void DetermineRelevantIntersections(
			[NotNull] IntersectionPoint3D intersection,
			[CanBeNull] IntersectionClusters intersectionClusters,
			ISegmentList target,
			out IntersectionPoint3D targetContinuationPoint,
			out IntersectionPoint3D targetArrivalPoint)
		{
			// The intersection point that is relevant to determine the deviation of the target forward direction
			targetContinuationPoint = intersection;
			targetArrivalPoint = intersection;

			if (intersectionClusters?.HasUnClusteredIntersections == true)
			{
				foreach (IntersectionPoint3D otherIntersection in
				         intersectionClusters.GetOtherIntersections(intersection))
				{
					if (otherIntersection.TargetPartIndex != intersection.TargetPartIndex)
					{
						// Different parts, no spike
						continue;
					}

					if (intersection.Point.EqualsXY(otherIntersection.Point, double.Epsilon))
					{
						// Exactly equal, not un-clustered (should probably have been removed already, or boundary loop)
						continue;
					}

					// Is it a spike that could be cracked?
					// Determine actual non-intersecting point, do not navigate inside the cluster
					double deltaAlongTarget =
						SegmentIntersectionUtils.GetVirtualVertexRatioDistance(
							intersection.VirtualTargetVertex,
							otherIntersection.VirtualTargetVertex,
							target.GetPart(intersection.TargetPartIndex).SegmentCount);

					if (Math.Abs(deltaAlongTarget) < 2)
					{
						if (deltaAlongTarget > 0)
						{
							// other is after this intersection
							targetContinuationPoint = otherIntersection;
						}

						if (deltaAlongTarget < 0)
						{
							// other is before this intersection
							targetArrivalPoint = otherIntersection;
						}
					}
				}
			}
		}

		private static void DetermineSourceDeviationAtIntersection(
			[NotNull] IntersectionPoint3D intersection,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			double tolerance,
			out bool hasRightSideDeviation,
			out bool hasLeftSideDeviation)
		{
			if (intersection.Type == IntersectionPointType.Crossing &&
			    ! intersection.DisallowSourceForward &&
			    ! intersection.DisallowSourceBackward)
			{
				hasLeftSideDeviation = true;
				hasRightSideDeviation = true;

				return;
			}

			// If the source arrives or continues to the outside (i.e. left side) then it's not contained
			bool? continuesToRightSide =
				intersection.SourceContinuesToRightSide(source, target, tolerance);

			hasLeftSideDeviation =
				continuesToRightSide == false && ! intersection.DisallowSourceForward;
			hasRightSideDeviation =
				continuesToRightSide == true && ! intersection.DisallowSourceForward;

			bool? arrivesFromRightSide =
				intersection.SourceArrivesFromRightSide(source, target, tolerance);

			if (arrivesFromRightSide == false && ! intersection.DisallowSourceBackward)
			{
				hasLeftSideDeviation = true;
			}

			if (arrivesFromRightSide == true && ! intersection.DisallowSourceBackward)
			{
				hasRightSideDeviation = true;
			}
		}

		private static IList<IntersectionPoint3D> GetRealIntersectionPoints(
			ISegmentList source,
			ISegmentList target,
			double tolerance,
			IList<IntersectionPoint3D> knownIntersections = null,
			Predicate<IntersectionPoint3D> predicate = null)
		{
			IList<IntersectionPoint3D> intersectionPoints =
				knownIntersections ??
				GeomTopoOpUtils.GetIntersectionPoints(source, target, tolerance);

			if (predicate != null)
			{
				intersectionPoints = intersectionPoints
				                     .Where(i => predicate(i))
				                     .ToList();
			}

			//Filter pseudo breaks of linear intersection stretches(e.g.at ring start/end)
			var unusable =
				GeomTopoOpUtils.GetAllLinearIntersectionBreaks(source, target, intersectionPoints)
				               .ToList();

			intersectionPoints = intersectionPoints.Where(i => ! unusable.Contains(i)).ToList();

			return intersectionPoints;
		}
	}
}

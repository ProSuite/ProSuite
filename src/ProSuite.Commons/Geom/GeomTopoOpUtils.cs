using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Geom
{
	public static class GeomTopoOpUtils
	{
		/// <summary>
		/// Returns the lines that are part of <paramref name="linestring1"></paramref> but have
		/// no linear intersection in the XY plane with <paramref name="multiLinestring2"/>.
		/// Optionally the sequence of lines that exist in both geometries but differ in Z
		/// are calculated (having vertices from both inputs but Z values of <paramref name="linestring1"/>).
		/// </summary>
		/// <param name="linestring1"></param>
		/// <param name="multiLinestring2"></param>
		/// <param name="tolerance"></param>
		/// <param name="zOnlyDifferences"></param>
		/// <param name="zTolerance"></param>
		/// <returns></returns>
		public static IList<Linestring> GetDifferenceLinesXY(
			[NotNull] Linestring linestring1,
			[NotNull] MultiLinestring multiLinestring2,
			double tolerance,
			[CanBeNull] IList<Linestring> zOnlyDifferences = null,
			double zTolerance = double.NaN)
		{
			// TODO:
			// - Additional option to merge results at source start/ends (especially where the difference covers ring end/start)
			// - Additional option to split result at crossings (e.g. for reshape cut subcurves)
			var zOnlyDiffLines = zOnlyDifferences != null && ! double.IsNaN(zTolerance)
				                     ? new List<Line3D>()
				                     : null;

			IEnumerable<Line3D> resultSegments = GetDifferenceLinesXY(
				linestring1, multiLinestring2, tolerance, zOnlyDiffLines, zTolerance);

			if (zOnlyDiffLines != null)
			{
				foreach (Linestring zOnlyLinestring in CollectIntersectionPaths(
					zOnlyDiffLines))
				{
					zOnlyDifferences.Add(zOnlyLinestring);
				}
			}

			return CollectIntersectionPaths(resultSegments);
		}

		public static bool IsLinearIntersectionDifferentInZ(
			[NotNull] SegmentIntersection intersection,
			[NotNull] Line3D intersectionLine,
			[NotNull] ISegmentList target,
			double toleranceSquared)
		{
			Line3D targetSegment = target[intersection.TargetIndex];

			Pnt3D fromPointOnTarget =
				intersection.GetLinearIntersectionStartOnTarget(targetSegment);
			Pnt3D toPointOnTarget =
				intersection.GetLinearIntersectionEndOnTarget(targetSegment);

			Assert.NotNull(fromPointOnTarget, nameof(fromPointOnTarget));
			Assert.NotNull(toPointOnTarget, nameof(toPointOnTarget));

			double startDistance2 =
				intersectionLine.StartPoint.Dist2(fromPointOnTarget, 3);

			double endDistance2 = intersectionLine.EndPoint.Dist2(toPointOnTarget, 3);

			bool differentInZ =
				! double.IsNaN(startDistance2) && startDistance2 > toleranceSquared ||
				! double.IsNaN(endDistance2) && endDistance2 > toleranceSquared;

			return differentInZ;
		}

		public static IList<Linestring> GetZOnlyDifferences(
			[NotNull] Linestring linestring1,
			[NotNull] MultiPolycurve multiLinestring2,
			double tolerance)
		{
			IEnumerable<Line3D> zOnlyDifferenceLines =
				GetZOnlyDifferenceLines(linestring1, multiLinestring2, tolerance);

			IList<Linestring> result =
				CollectIntersectionPaths(zOnlyDifferenceLines);

			return result;
		}

		public static IList<Linestring> GetIntersectionLinesXY(
			[NotNull] Linestring linestring1,
			[NotNull] Linestring linestring2,
			double tolerance)
		{
			IEnumerable<SegmentIntersection> intersections =
				SegmentIntersectionUtils.GetSegmentIntersectionsXY(
					linestring1, linestring2, tolerance);

			// The intersections are ordered along segmentList1 but if a source-line has several
			// linear intersections, they should also be ordered along the source segment
			IEnumerable<SegmentIntersection> orderedIntersections =
				OrderAlongSourceSegments(intersections.Where(i => i.HasLinearIntersection));

			// Consider getting intersection points and using segmentList1.GetSubcurve()
			// between linear intersection points. This would yield another ~100ms per 100K segments.
			IList<Linestring> result = CollectIntersectionPaths(orderedIntersections, linestring1);

			return result;
		}

		public static Linestring MergeConnectedLinestrings(
			[NotNull] IList<Linestring> connectingCurves,
			[CanBeNull] Pnt3D startPoint,
			double tolerance)
		{
			return new Linestring(
				GetConnectedPoints(connectingCurves, startPoint, tolerance));
		}

		/// <summary>
		/// Cuts the provided source rings using the provided cutLine and returns the list 
		/// of result rings.
		/// </summary>
		/// <param name="sourceRings"></param>
		/// <param name="cutLines"></param>
		/// <param name="tolerance"></param>
		/// <param name="avoidMultipartResults"></param>
		/// <param name="subcurveNavigator"></param>
		/// <returns></returns>
		public static IList<MultiLinestring> CutXY([NotNull] ISegmentList sourceRings,
		                                           [NotNull] ISegmentList cutLines,
		                                           double tolerance,
		                                           bool avoidMultipartResults = false,
		                                           SubcurveNavigator subcurveNavigator = null)
		{
			Assert.ArgumentCondition(sourceRings.IsClosed,
			                         "sourceRings must be closed.");

			subcurveNavigator = subcurveNavigator == null
				                    ? new MultipleRingNavigator(sourceRings, cutLines, tolerance)
				                    : subcurveNavigator.Clone();

			subcurveNavigator.PreferTargetZsAtIntersections = true;

			var ringOperator = new RingOperator(subcurveNavigator);

			IList<RingGroup> leftResult;
			IList<RingGroup> rightResult;
			IList<MultiLinestring> undeterminedSidePolys;
			MultiLinestring unCutParts;
			IList<RingGroup> clipPolys;
			bool cutSuccessful =
				ringOperator.CutXY(out leftResult, out rightResult, out clipPolys,
				                   out undeterminedSidePolys, out unCutParts);

			// The out-of-the-box cut polygon tool creates (somewhat illogical) multipart polygons
			// if the input is multipart (i.e. several outer rings) -> use parameter

			var result = new List<MultiLinestring>();
			if (cutSuccessful)
			{
				AddToList(leftResult, result, ! avoidMultipartResults);
				AddToList(rightResult, result, ! avoidMultipartResults);
				AddToList(undeterminedSidePolys, result, ! avoidMultipartResults);
			}

			// sort descending:
			result.Sort((a, b) => b.GetArea2D().CompareTo(a.GetArea2D()));

			if (result.Count > 0)
			{
				// now merge the first with the un-changed areas
				foreach (var linestring in unCutParts.GetLinestrings())
				{
					result[0].AddLinestring(linestring);
				}
			}

			// and finally add the clipped interiors (they should be always new)
			AddToList(clipPolys, result, ! avoidMultipartResults);

			return result;
		}

		/// <summary>
		/// Cuts the provided source ring group using the provided cutLines and returns the list of
		/// result ring groups on both sides of the cut line. Cutting is done in the XY plane
		/// (preserving the Z-values of the input cut lines unless the source ring is vertical.
		/// This method supports cutting vertical, but planar ring groups by both vertical
		/// (matching) and non-vertical cutLines. Non-vertical cut lines that intersect (i.e. touch
		/// or cross) source segments will be used to create a vertical cut line between the lowest
		/// and the highest source segment.
		/// </summary>
		/// <param name="sourcePoly"></param>
		/// <param name="cutLines"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<RingGroup> CutPlanar(
			[NotNull] RingGroup sourcePoly,
			[NotNull] ISegmentList cutLines,
			double tolerance)
		{
			IList<Linestring> cutLinestrings = GeomUtils.GetLinestrings(cutLines).ToList();

			bool cutLinesAreVertical =
				cutLinestrings.All(l => CutLineIsVertical(l, tolerance));

			if (cutLinesAreVertical)
			{
				return CutVerticalRingGroup(sourcePoly, cutLines, tolerance).ToList();
			}

			MultipleRingNavigator ringNavigator =
				new MultipleRingNavigator(sourcePoly, cutLines, tolerance);

			List<List<Pnt3D>> xyClusters;
			bool hasVerticalPoints = HasVerticalPoints(
				ringNavigator.IntersectionsAlongTarget.Select(i => i.Point),
				tolerance, out xyClusters);

			if (hasVerticalPoints)
			{
				var verticalCutlines = new MultiPolycurve(
					xyClusters
						.Select(c => new Linestring(c.OrderBy(p => p.Z)))
						.ToList());

				return CutVerticalRingGroup(sourcePoly, verticalCutlines, tolerance).ToList();
			}

			var resultXY = CutXY(sourcePoly, cutLines, tolerance, true, ringNavigator);

			return resultXY.SelectMany(r => GetConnectedComponents(r, tolerance)).ToList();
		}

		public static MultiLinestring GetDifferenceAreasXY([NotNull] MultiLinestring sourceRings,
		                                                   [NotNull] MultiLinestring targetRings,
		                                                   double tolerance)
		{
			Assert.ArgumentCondition(sourceRings.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(targetRings.IsClosed, "Target must be closed.");

			var subcurveNavigator = new MultipleRingNavigator(sourceRings, targetRings, tolerance);
			var ringOperator = new RingOperator(subcurveNavigator);

			return ringOperator.DifferenceXY();
		}

		public static IList<RingGroup> RemoveXY([NotNull] RingGroup fromSource,
		                                        [NotNull] Linestring ringToRemove,
		                                        double tolerance)
		{
			return RemoveXY(fromSource, new List<Linestring> {ringToRemove}, tolerance);
		}

		public static IList<IntersectionArea3D> GetIntersectionAreasXY(
			[NotNull] MultiLinestring sourceRings,
			[NotNull] Polyhedron targetPolyhedron,
			double tolerance,
			ChangeAlongZSource zSource = ChangeAlongZSource.Target)
		{
			// TODO: Implement that target rings can intersect each other as this is the case for polyhedrons.
			//       -> performs much better because a single spatial index could be used.

			var result = new List<IntersectionArea3D>();
			foreach (var targetRingGroup in targetPolyhedron.RingGroups)
			{
				MultiLinestring intersection =
					GetIntersectionAreasXY(sourceRings, targetRingGroup, tolerance, zSource);

				if (intersection.IsEmpty)
				{
					continue;
				}

				result.Add(new IntersectionArea3D(intersection, sourceRings, targetRingGroup));
			}

			return result;
		}

		public static MultiLinestring GetIntersectionMultipolygonXY(
			[NotNull] MultiLinestring sourceRings,
			[NotNull] Polyhedron targetPolyhedron,
			double tolerance,
			ChangeAlongZSource zSource = ChangeAlongZSource.Target)
		{
			// TODO: Implement that target rings can intersect each other as this is the case for polyhedrons.
			//       -> performs much better because a single spatial index could be used.

			var result = new List<MultiLinestring>();
			foreach (var targetRingGroup in targetPolyhedron.RingGroups)
			{
				MultiLinestring intersection =
					GetIntersectionAreasXY(sourceRings, targetRingGroup, tolerance, zSource);

				result.Add(intersection);
			}

			return new MultiPolycurve(result);
		}

		public static MultiLinestring GetIntersectionAreasXY(
			[NotNull] MultiLinestring sourceRings,
			[NotNull] MultiLinestring targetRings,
			double tolerance,
			ChangeAlongZSource zSource = ChangeAlongZSource.Target)
		{
			Assert.ArgumentCondition(sourceRings.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(targetRings.IsClosed, "Target must be closed.");

			if (zSource != ChangeAlongZSource.Target)
			{
				targetRings = targetRings.Clone();
				targetRings.DropZs();
			}

			Plane3D plane = null;
			if (zSource == ChangeAlongZSource.SourcePlane)
			{
				plane = ChangeZUtils.GetPlane(sourceRings, tolerance);
			}

			var subcurveNavigator = new MultipleRingNavigator(sourceRings, targetRings, tolerance);

			var ringOperator = new RingOperator(subcurveNavigator);

			MultiLinestring result = ringOperator.IntersectXY();

			if (plane != null)
			{
				result.AssignUndefinedZs(plane);
			}
			else
			{
				result.InterpolateUndefinedZs();
			}

			return result;
		}

		public static MultiLinestring GetUnionAreasXY([NotNull] MultiLinestring sourceRings,
		                                              [NotNull] MultiLinestring targetRings,
		                                              double tolerance)
		{
			Assert.ArgumentCondition(sourceRings.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(targetRings.IsClosed, "Target must be closed.");

			var subcurveNavigator = new MultipleRingNavigator(sourceRings, targetRings, tolerance);

			var ringOperator = new RingOperator(subcurveNavigator);

			return ringOperator.UnionXY();
		}

		/// <summary>
		/// Returns the areas which are part of the source source but not part of the
		/// specified ringsToRemove (difference).
		/// </summary>
		/// <param name="fromSource"></param>
		/// <param name="ringsToRemove"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static IList<RingGroup> RemoveXY([NotNull] RingGroup fromSource,
		                                        [NotNull] List<Linestring> ringsToRemove,
		                                        double tolerance)
		{
			Linestring mainRing = fromSource.ExteriorRing;

			Assert.True(mainRing.ClockwiseOriented == true,
			            "The source is not oriented clockwise or it is vertical.");

			List<Linestring> allRingsToRemove = ringsToRemove.ToList();
			allRingsToRemove.AddRange(fromSource.InteriorRings);

			// Remove all the overlapping (boundary crossing) inner rings (i.e. non-contained, non-disjoint)
			var usedTargets = new HashSet<Linestring>();
			IList<Linestring> remainingPositiveRings = TryProcessAll(
				RemoveOverlapXY, mainRing, allRingsToRemove, tolerance,
				usedTargets);

			List<Linestring> disjointOrContainedIslands =
				allRingsToRemove.Where(i => ! usedTargets.Contains(i)).ToList();

			List<RingGroup> result = new List<RingGroup>();
			foreach (Linestring remainingPositiveRing in remainingPositiveRings)
			{
				Func<Linestring, bool> completelyWithin =
					r => GeomRelationUtils.AreaContainsXY(remainingPositiveRing, r.StartPoint,
					                                      tolerance, true) == true;

				var containedRings =
					disjointOrContainedIslands.Where(completelyWithin).ToList();

				IList<Linestring> innerRings;
				if (containedRings.Count > 1)
				{
					innerRings = UnionInteriorRings(containedRings, tolerance);
				}
				else
				{
					innerRings = containedRings;
				}

				var resultRingGroup = CreateRingGroup(remainingPositiveRing,
				                                      innerRings,
				                                      tolerance);

				result.Add(resultRingGroup);
			}

			return result;
		}

		public static IList<Linestring> RemoveOverlapXY(Linestring sourceRing,
		                                                Linestring ringToRemove,
		                                                double tolerance)
		{
			var ringOperator = new RingOperator(sourceRing, ringToRemove, tolerance);

			IList<Linestring> result = ringOperator.RemoveOverlapXY();

			foreach (Linestring linestring in result)
			{
				linestring.SpatialIndex =
					SpatialHashSearcher<int>.CreateSpatialSearcher(linestring);
			}

			return result;
		}

		[NotNull]
		public static IList<Linestring> IntersectPlanar(
			[NotNull] Linestring sourceRing,
			[NotNull] Linestring targetRing,
			double tolerance)
		{
			Assert.ArgumentCondition(sourceRing.IsClosed, "sourceRing must be closed.");
			Assert.ArgumentCondition(targetRing.IsClosed, "targetRing must be closed.");

			if (sourceRing.ClockwiseOriented == false)
			{
				// Currently not implemented - interior single ring cannot be intersected
				return new List<Linestring>(0);
			}

			if (sourceRing.ClockwiseOriented == null)
			{
				// vertical ring
				RotationAxis rotationAxis =
					GetPreferredRotationAxis(sourceRing);

				Linestring rotatedSource = Rotate(sourceRing, rotationAxis);

				var rotatedTarget = Rotate(targetRing, rotationAxis);

				var rotatedResults =
					RemoveOverlapXY(rotatedSource, rotatedTarget, tolerance);

				return rotatedResults
				       .Select(rotatedResult =>
					               Rotate(rotatedResult, rotationAxis))
				       .ToList();
			}

			return IntersectXY(sourceRing, targetRing, tolerance);
		}

		public static IList<Linestring> IntersectXY(Linestring sourceRing,
		                                            Linestring target,
		                                            double tolerance)
		{
			MultipleRingNavigator ringNavigator =
				new MultipleRingNavigator(sourceRing, target, tolerance);

			var ringOperator = new RingOperator(ringNavigator);

			return ringOperator.IntersectXY().GetLinestrings().ToList();
		}

		/// <summary>
		/// Cuts the provided source ring using the provided cutLine and returns separate lists 
		/// of result rings on the left/right side of the cut line.
		/// </summary>
		/// <param name="sourceRing"></param>
		/// <param name="cutLine"></param>
		/// <param name="tolerance"></param>
		/// <param name="leftRings">Result rings on the left side of the cut line.</param>
		/// <param name="rightRings">Result rings on the right side of the cut line.</param>
		/// <returns>Whether the cut operation was successful or not.</returns>
		public static bool CutRingXY([NotNull] Linestring sourceRing,
		                             [NotNull] Linestring cutLine,
		                             double tolerance,
		                             [NotNull] out IList<Linestring> leftRings,
		                             [NotNull] out IList<Linestring> rightRings)
		{
			RingOperator ringOperator = new RingOperator(sourceRing, cutLine, tolerance);

			return ringOperator.CutXY(out leftRings, out rightRings);
		}

		#region 3D Cut Ring

		/// <summary>
		/// Determines whether a ring can be cut by the provided cut line. The ring can be vertical and have
		/// clockwise or counter-clockwise orientation.
		/// </summary>
		/// <param name="ringPoints"></param>
		/// <param name="cutLine"></param>
		/// <param name="tolerance"></param>
		/// <param name="isVerticalRing"></param>
		/// <returns></returns>
		public static bool CanCutRing3D([NotNull] IList<Pnt3D> ringPoints,
		                                [NotNull] Line3D cutLine,
		                                double tolerance,
		                                bool isVerticalRing = false)
		{
			int startIndex;
			int endIndex;

			GetCutIndices(ringPoints, cutLine, out startIndex, out endIndex, tolerance);

			if (startIndex < 0 || endIndex < 0)
			{
				return false;
			}

			if (startIndex == endIndex)
			{
				return false;
			}

			int startPlus1 = NextIndexInRing(startIndex, ringPoints.Count);

			if (startPlus1 == endIndex)
			{
				return false;
			}

			int endPlus1 = NextIndexInRing(endIndex, ringPoints.Count);

			if (endPlus1 == startIndex)
			{
				return false;
			}

			if (! isVerticalRing)
			{
				// Check if the cutLine intersects an existing segment which would result in non-simple rings
				for (var i = 0; i < ringPoints.Count - 1; i++)
				{
					Pnt3D segmentStart = ringPoints[i];
					Pnt3D segmentEnd = ringPoints[NextIndexInRing(i, ringPoints.Count)];

					if (GeomRelationUtils.SourceInteriorIntersectsXY(
						cutLine, new Line3D(segmentStart, segmentEnd),
						tolerance))
					{
						return false;
					}
				}
			}

			return CutLineCutsInsideRing(ringPoints, startIndex, endIndex,
			                             isVerticalRing);
		}

		public static void CutRing3D([NotNull] IList<Pnt3D> ringPoints,
		                             [NotNull] Line3D cutLine,
		                             [NotNull] out List<Pnt3D> resultRing1,
		                             [NotNull] out List<Pnt3D> resultRing2,
		                             double tolerance = 0)
		{
			int startIndex;
			int endIndex;
			GetCutIndices(ringPoints, cutLine, out startIndex, out endIndex, tolerance);

			Assert.True(startIndex >= 0, "Cut line does not intersect at start point.");
			Assert.True(endIndex >= 0, "Cut line does not intersect at end point.");

			int vertexCount = endIndex - startIndex;

			CutRing3D(ringPoints, startIndex, vertexCount, out resultRing1,
			          out resultRing2);
		}

		public static void CutRing3D([NotNull] IList<Pnt3D> ringPnts3D,
		                             int startIndex,
		                             int vertexCount,
		                             [NotNull] out List<Pnt3D> resultRing1,
		                             [NotNull] out List<Pnt3D> resultRing2)
		{
			resultRing1 = new List<Pnt3D>();
			resultRing2 = new List<Pnt3D>();

			int originalVertexCount = ringPnts3D.Count;

			for (int i = startIndex; i < startIndex + originalVertexCount; i++)
			{
				if (i <= startIndex + vertexCount)
				{
					resultRing1.Add(ringPnts3D[i % (originalVertexCount - 1)]);
				}

				if (i >= startIndex + vertexCount)
				{
					resultRing2.Add(ringPnts3D[i % (originalVertexCount - 1)]);
				}
			}

			// close the rings
			resultRing1.Add((Pnt3D) resultRing1[0].Clone());
			resultRing2.Add((Pnt3D) resultRing2[0].Clone());
		}

		private static void GetCutIndices(IList<Pnt3D> ringPoints, Line3D cutLine,
		                                  out int startIndex, out int endIndex,
		                                  double tolerance)
		{
			startIndex = -1;
			endIndex = -1;

			for (var i = 0; i < ringPoints.Count; i++)
			{
				if (GeomUtils.Equals3D(ringPoints[i], cutLine.StartPoint, tolerance))
				{
					startIndex = i;
				}

				if (GeomUtils.Equals3D(ringPoints[i], cutLine.EndPoint, tolerance))
				{
					endIndex = i;
				}
			}

			if (startIndex > endIndex)
			{
				int tmp = startIndex;
				startIndex = endIndex;
				endIndex = tmp;
			}
		}

		///  <summary>
		///  Ensure that the cut line cuts to the inside of the ring.
		///  If the angle is a right turn: the end of the cut line must be on the right of both adjacent segments
		///  If the angle is concave: the end of the cut line most NOT be on the left of either adjacent segment:
		///  This is the case for both clockwise and anti-clockwise oriented rings.
		/// 
		///  convex:      -------------------------------->
		///              /                     inside
		///   outside   /                      of polygon
		///   of       /________cut line
		///   polygon  \
		///             \
		///              \
		///               ----------------------------------
		///  </summary>
		///  <param name="ringPoints"></param>
		///  <param name="cutlineStartIdx"></param>
		///  <param name="cutlineEndIdx"></param>
		/// <param name="isVerticalRing"></param>
		/// <returns></returns>
		private static bool CutLineCutsInsideRing(
			[NotNull] IList<Pnt3D> ringPoints, int cutlineStartIdx, int cutlineEndIdx,
			bool isVerticalRing)
		{
			if (isVerticalRing)
			{
				// if vertical, rotate XZ or YZ plane to XY
				IBox boundingBox3D = GeomUtils.GetBoundingBox3D(ringPoints);
				double dx = Math.Abs(boundingBox3D.Max.X - boundingBox3D.Min.X);
				double dy = Math.Abs(boundingBox3D.Max.Y - boundingBox3D.Min.Y);

				List<Pnt3D> deepCopy = dx > dy
					                       ? GeomUtils.RotateX90(ringPoints, true).ToList()
					                       : GeomUtils.RotateY90(ringPoints, true).ToList();

				ringPoints = deepCopy;
			}

			Pnt3D testPoint = ringPoints[cutlineEndIdx];

			bool toTheInside = IsOnTheRightSide(ringPoints, cutlineStartIdx, testPoint);

			if (GeomUtils.GetOrientation(ringPoints) < 0)
			{
				toTheInside = ! toTheInside;
			}

			return toTheInside;
		}

		private static bool IsOnTheRightSide([NotNull] IList<Pnt3D> ringPoints,
		                                     int ringPointIdx,
		                                     [NotNull] Pnt3D testPoint)
		{
			if (ringPoints == null)
			{
				throw new ArgumentNullException(nameof(ringPoints));
			}

			int startPlus1 = NextIndexInRing(ringPointIdx, ringPoints.Count);
			int startMinus1 = ringPointIdx == 0 ? ringPoints.Count - 2 : ringPointIdx - 1;

			Pnt3D ringVertex = ringPoints[ringPointIdx];
			Pnt3D previousRingVertex = ringPoints[startMinus1];
			Pnt3D nextRingVertex = ringPoints[startPlus1];

			return IsOnTheRightSide(previousRingVertex, ringVertex, nextRingVertex,
			                        testPoint);
		}

		/// <summary>
		/// Determines whether the test point is on the right side (inside of positive ring) of the
		/// two provided segments of a ring.
		/// </summary>
		/// <param name="previousRingVertex">The segment 1 start point</param>
		/// <param name="ringVertex">The segment 1 endpoint / segment 2 start point</param>
		/// <param name="nextRingVertex">The segment 2 end point</param>
		/// <param name="testPoint">The test point whose location shall be tested.</param>
		/// <returns>True, if the test point is on the right side, i.e. inside of a positive ring.</returns>
		public static bool IsOnTheRightSide([NotNull] Pnt3D previousRingVertex,
		                                    [NotNull] Pnt3D ringVertex,
		                                    [NotNull] Pnt3D nextRingVertex,
		                                    [NotNull] IPnt testPoint)
		{
			bool isRightOfNextSegment = GeomUtils.IsLeftXY(
				                            ringVertex,
				                            nextRingVertex,
				                            testPoint) < 0;

			bool isRightOfPreviousSegment = GeomUtils.IsLeftXY(
				                                previousRingVertex,
				                                ringVertex,
				                                testPoint) < 0;

			bool isRightTurn = GeomUtils.IsLeftXY(previousRingVertex, ringVertex,
			                                      nextRingVertex) < 0;

			var toTheInside = true;
			if (isRightTurn)
			{
				if (! isRightOfPreviousSegment || ! isRightOfNextSegment)
				{
					// clockwise convex: both must be on the right
					toTheInside = false;
				}
			}
			else
			{
				// clockwise concave: must not be on the left of either
				if (! isRightOfPreviousSegment && ! isRightOfNextSegment)
				{
					toTheInside = false;
				}
			}

			return toTheInside;
		}

		/// <summary>
		/// Determines whether the test point is on the right side (inside of positive ring) of the
		/// two provided segments of a ring.
		/// </summary>
		/// <param name="previousSegment">The segment 1</param>
		/// <param name="nextSegment">The segment 2</param>
		/// <param name="testPoint">The test point whose location shall be tested.</param>
		/// <param name="tolerance"></param>
		/// <returns>True, if the test point is on the right side, i.e. inside of a positive ring.
		/// False, if the test point is on the left side.
		/// Null if the test point is within the tolerance of one of the segments.</returns>
		public static bool? IsOnTheRightSide([NotNull] Line3D previousSegment,
		                                     [NotNull] Line3D nextSegment,
		                                     [NotNull] IPnt testPoint,
		                                     double tolerance)
		{
			double distanceFromNext = nextSegment.GetDistanceXYPerpendicularSigned(
				testPoint, out double distanceAlongNextRatio);

			if (Math.Abs(distanceFromNext) < tolerance &&
			    distanceAlongNextRatio >= 0 && distanceAlongNextRatio <= 1)
			{
				return null;
			}

			bool isRightOfNextSegment = distanceFromNext < 0;

			double distanceAlongPreviousRatio;
			double distanceFromPrevious = previousSegment.GetDistanceXYPerpendicularSigned(
				testPoint, out distanceAlongPreviousRatio);

			if (Math.Abs(distanceFromPrevious) < tolerance &&
			    distanceAlongPreviousRatio >= 0 && distanceAlongPreviousRatio <= 1)
			{
				return null;
			}

			bool isRightOfPreviousSegment = distanceFromPrevious < 0;

			bool isRightTurn = GeomUtils.IsLeftXY(previousSegment.StartPoint,
			                                      nextSegment.StartPoint,
			                                      nextSegment.EndPoint) < 0;

			var toTheInside = true;
			if (isRightTurn)
			{
				if (! isRightOfPreviousSegment || ! isRightOfNextSegment)
				{
					// clockwise convex: both must be on the right
					toTheInside = false;
				}
			}
			else
			{
				// clockwise concave: must not be on the left of either
				if (! isRightOfPreviousSegment && ! isRightOfNextSegment)
				{
					toTheInside = false;
				}
			}

			return toTheInside;
		}

		#endregion

		#region 3D Intersection

		[CanBeNull]
		public static IBox IntersectBoxes([NotNull] IBox box1, [NotNull] IBox box2)
		{
			Assert.ArgumentNotNull(box1, nameof(box1));
			Assert.ArgumentNotNull(box2, nameof(box2));

			Assert.ArgumentCondition(box1.Dimension == box2.Dimension,
			                         "Box dimensions are not equal");

			// Consider separate and explicit 2D/3D imlementations...
			var min = new Vector(box1.Dimension);
			var max = new Vector(box1.Dimension);
			for (var i = 0; i < box1.Dimension; i++)
			{
				min[i] = Math.Max(box1.Min[i], box2.Min[i]);
				max[i] = Math.Min(box1.Max[i], box2.Max[i]);

				if (max[i] < min[i])
				{
					// No intersection
					return null;
				}
			}

			return new Box(min, max);
		}

		public static IBox UnionBoxes([NotNull] IBox box1, [NotNull] IBox box2)
		{
			Assert.ArgumentNotNull(box1, nameof(box1));
			Assert.ArgumentNotNull(box2, nameof(box2));

			Assert.ArgumentCondition(box1.Dimension == box2.Dimension,
			                         "Box dimensions are not equal");

			// Consider separate and explicit 2D/3D imlementations...
			var min = new Vector(box1.Dimension);
			var max = new Vector(box1.Dimension);
			for (var i = 0; i < box1.Dimension; i++)
			{
				min[i] = Math.Min(box1.Min[i], box2.Min[i]);
				max[i] = Math.Max(box1.Max[i], box2.Max[i]);
			}

			return new Box(min, max);
		}

		/// <summary>
		/// Gets the intersecting straight line r of two planes in vector form:
		/// <b><i>r</i></b> = p0 + s * <b><i>v</i></b>
		/// </summary>
		/// <param name="plane1"></param>
		/// <param name="plane2"></param>
		/// <param name="p0">The point P0 on the straight line</param>
		/// <returns>The vector v that determines the direction of the straight line</returns>
		[CanBeNull]
		public static Vector IntersectPlanes([NotNull] Plane3D plane1,
		                                     [NotNull] Plane3D plane2,
		                                     [CanBeNull] out Pnt3D p0)
		{
			Vector normal1 = plane1.Normal;
			Vector normal2 = plane2.Normal;

			// Cross product to get the direction of the intersection line
			Vector direction = GeomUtils.CrossProduct(normal1, normal2);

			// Get any point in plane2 as start point of the intersection direction vector:

			// Solve the linear equations for the planes to find a point on the intersection line
			// http://geomalgorithms.com/a05-_intersect-1.html

			// Translate to a*X + b*Y + c*Z + d = 0:

			double a1 = plane1.A;
			double a2 = plane2.A;
			double b1 = plane1.B;
			double b2 = plane2.B;
			double c1 = plane1.C;
			double c2 = plane2.C;
			double d1 = -plane1.D;
			double d2 = -plane2.D;

			p0 = null;

			// For most robust computation, set the coordinate with the largest absolute value in the direction vector to 0:
			double dirAbsX = Math.Abs(direction[0]);
			double dirAbsY = Math.Abs(direction[1]);
			double dirAbsZ = Math.Abs(direction[2]);

			// TODO: Use plane.Epsilon?
			if (MathUtils.AreEqual(dirAbsX, 0) &&
			    MathUtils.AreEqual(dirAbsY, 0) &&
			    MathUtils.AreEqual(dirAbsZ, 0))
			{
				return null;
			}

			p0 = new Pnt3D();

			if (dirAbsZ >= dirAbsX && dirAbsZ >= dirAbsY)
			{
				// solve the plain equation for both planes for point (x, y, 0):
				p0.Z = 0;

				double denominator = a1 * b2 - a2 * b1;

				p0.X = (b1 * d2 - b2 * d1) / denominator;
				p0.Y = (a2 * d1 - a1 * d2) / denominator;
			}
			else if (dirAbsY >= dirAbsX && dirAbsY >= dirAbsZ)
			{
				// solve the plain equation for both planes for point (x, 0, z):
				p0.Y = 0;

				double denominator = a1 * c2 - a2 * c1;

				p0.X = (c1 * d2 - c2 * d1) / denominator;
				p0.Z = (a2 * d1 - a1 * d2) / denominator;
			}
			else if (dirAbsX >= dirAbsY && dirAbsX >= dirAbsZ)
			{
				// solve the plain equation for both planes for point (0, y, z):
				p0.X = 0;

				double denominator = b1 * c2 - b2 * c1;

				p0.Y = (c1 * d2 - c2 * d1) / denominator;
				p0.Z = (b2 * d1 - b1 * d2) / denominator;
			}

			return direction;
		}

		[CanBeNull]
		public static Line3D IntersectLines3D([NotNull] Line3D line1,
		                                      [NotNull] Line3D line2,
		                                      double tolerance)
		{
			if (! line1.ExtentIntersects(line2.Extent, tolerance))
			{
				return null;
			}

			// check if the lines are collinear within the tolerance:
			if (line1.GetDistancePerpendicular(line2.StartPoint) > tolerance)
			{
				return null;
			}

			if (line1.GetDistancePerpendicular(line2.EndPoint) > tolerance)
			{
				// alternatively, calculate the distance along where the tolerance threshold is crossed (AO-style), but that would need to be a choice.
				return null;
			}

			// solve the line1 equation for the start/end points of line2 (l20, l21) and check intervals
			const double line1P0 = 0;
			const double line1P1 = 1;

			// solve by using the vector's largest dimension
			double line2P0 =
				line1.DirectionVector.GetFactor(line2.StartPoint - line1.StartPoint);
			double line2P1 =
				line1.DirectionVector.GetFactor(line2.EndPoint - line1.StartPoint);

			if (line2P0 > line2P1)
			{
				// the vectors have inverse orientation:
				double temp = line2P1;
				line2P1 = line2P0;
				line2P0 = temp;
			}

			if (line2P0 > line1P1 || line2P1 < line1P0)
			{
				// disjoint
				return null;
			}

			double r0 = Math.Max(line1P0, line2P0);
			double r1 = Math.Min(line1P1, line2P1);

			return new Line3D(line1.GetPointAlong(r0, true),
			                  line1.GetPointAlong(r1, true));
		}

		/// <summary>
		/// Calculates the XY-intersected source rings and then 3D-cuts them with the target rings.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="tolerance"></param>
		/// <param name="nonInteriorIntersectingAreas"></param>
		/// <returns></returns>
		public static IList<RingGroup> GetIntersectionAreas3D(
				[NotNull] Polyhedron source,
				[NotNull] Polyhedron target,
				double tolerance)
			// TODO: out IList<RingGroup> nonInteriorIntersectingAreas)
		{
			// First: Split source and target at boundaries in XY.
			// The 2D intersection assumes 'XY-correct' orientation for the time being (positive outer rings).
			var xySplitSourceRings = new List<RingGroup>();
			var xySplitTargetRings = new List<RingGroup>();
			foreach (RingGroup sourceRingGroup in source.RingGroups)
			{
				foreach (IntersectionArea3D intersectionArea in
					GetIntersectionAreasXY(sourceRingGroup, target, tolerance,
					                       ChangeAlongZSource.SourcePlane))
				{
					xySplitSourceRings.AddRange(GetConnectedComponents(
						                            intersectionArea.IntersectionArea, tolerance));

					// and the intersection within the target ring's plane:
					var targetPlane = ChangeZUtils.GetPlane(intersectionArea.Target, tolerance);

					MultiLinestring intersectionInTargetPlane =
						intersectionArea.IntersectionArea.Clone();

					intersectionInTargetPlane.DropZs();
					intersectionInTargetPlane.AssignUndefinedZs(targetPlane);

					xySplitTargetRings.AddRange(
						GetConnectedComponents(intersectionInTargetPlane, tolerance));
				}
			}

			// Second: Calculate 3D cut lines and cut the source along them
			// Build a new polyhedron to leverage the spatial index:
			Polyhedron xyCutTarget = new Polyhedron(xySplitTargetRings);

			var result = new List<RingGroup>();
			foreach (RingGroup sourceRing in xySplitSourceRings)
			{
				var intersectionPaths = new List<IntersectionPath3D>();

				foreach (RingGroup targetRing in xyCutTarget.FindRingGroups(sourceRing, tolerance))
				{
					IList<IntersectionPath3D> intersections = GetCoplanarPolygonIntersectionLines3D(
						sourceRing, targetRing, tolerance, true);

					if (intersections != null)
					{
						intersectionPaths.AddRange(intersections);
					}
				}

				if (intersectionPaths.Count == 0)
				{
					result.Add(sourceRing);
				}
				else
				{
					var cutLines = new MultiPolycurve(intersectionPaths.Select(ip => ip.Segments));

					result.AddRange(CutPlanar(sourceRing, cutLines, tolerance));
				}
			}

			return result;
		}

		/// <summary>
		/// Calculates the 3D intersection lines between coplanar source and coplanar target rings.
		/// </summary>
		/// <param name="ringGroup1"></param>
		/// <param name="ringGroup2"></param>
		/// <param name="tolerance"></param>
		/// <param name="excludeBoundaryIntersections"></param>
		/// <returns></returns>
		public static IList<IntersectionPath3D> GetCoplanarPolygonIntersectionLines3D(
			[NotNull] RingGroup ringGroup1,
			[NotNull] RingGroup ringGroup2,
			double tolerance,
			bool excludeBoundaryIntersections = false)
		{
			if (ringGroup1 == null)
			{
				throw new ArgumentNullException(nameof(ringGroup1));
			}

			var ring1Points = ringGroup1.ExteriorRing.GetPoints().ToList();
			var ring2Points = ringGroup2.ExteriorRing.GetPoints().ToList();

			IBox box1 = GeomUtils.GetBoundingBox3D(ring1Points);

			IBox box2 = GeomUtils.GetBoundingBox3D(ring2Points);

			IBox bbIntersection = IntersectBoxes(box1, box2);

			if (bbIntersection == null)
			{
				return null;
			}

			Plane3D plane1 = Plane3D.FitPlane(ring1Points, true);
			Plane3D plane2 = Plane3D.FitPlane(ring2Points, true);

			AssertCoplanarity(ring1Points, plane1, tolerance);
			AssertCoplanarity(ring2Points, plane2, tolerance);

			Pnt3D p0;
			Vector direction = IntersectPlanes(plane1, plane2, out p0);

			if (direction == null || MathUtils.AreEqual(direction.LengthSquared, 0))
			{
				if (! plane1.IsCoincident(plane2))
				{
					// Planes are parallel but not coincident - no intersection
					return null;
				}

				// else: coincident. TODO: Rotate if vertical (2 vertical coincident planes!).
				MultiLinestring resultRings = GetIntersectionAreasXY(
					ringGroup1, ringGroup2, tolerance);

				var result = new List<IntersectionPath3D>();
				foreach (Linestring resultRing in resultRings.GetLinestrings())
				{
					result.Add(
						new IntersectionPath3D(resultRing, RingPlaneTopology.InPlane));
				}

				return result;
			}

			IBox bbUnion = UnionBoxes(box1, box2);

			Line3D planePlaneIntersectionInBox =
				Line3D.ConstructInBox(Assert.NotNull(p0), direction, bbUnion);

			if (planePlaneIntersectionInBox == null ||
			    planePlaneIntersectionInBox.Length3D < tolerance)
			{
				return null;
			}

			Linestring cutStraight = new Linestring(new[] {planePlaneIntersectionInBox});

			var cutLines1 =
				GetRingIntersectionLinesPlanar(cutStraight, ringGroup1, tolerance,
				                               excludeBoundaryIntersections)
					.ToList();
			var cutLines2 =
				GetRingIntersectionLinesPlanar(cutStraight, ringGroup2, tolerance,
				                               excludeBoundaryIntersections)
					.ToList();

			IList<IntersectionPath3D> ringGroup1PlaneIntersections =
				cutLines1.Select(cl => new IntersectionPath3D(cl, RingPlaneTopology.InPlane))
				         .ToList();
			IList<IntersectionPath3D> ringGroup2PlaneIntersections =
				cutLines2.Select(cl => new IntersectionPath3D(cl, RingPlaneTopology.InPlane))
				         .ToList();

			IList<IntersectionPath3D> intersections = GetIntersectionLines3D(
				ringGroup1PlaneIntersections, ringGroup2PlaneIntersections, tolerance);

			return intersections.Count == 0 ? null : intersections;
		}

		/// <summary>
		/// Calculates the intersection lines of two planar rings. If coplanarity of the input points
		/// in each ring is validated or the rings do not define a plane an exception is thrown. 
		/// Null is returned if the two rings do not intersect, if they are parallel or the intersection 
		/// is an area rather than a line.
		/// </summary>
		/// <param name="ring1Pnts3D"></param>
		/// <param name="ring2Pnts3D"></param>
		/// <param name="tolerance"></param>
		/// <param name="boundaryIntersectionsOnly">Only return intersections in which both rings have
		/// a linear intersection within the plane of the other ring. The result is the 3D intersection 
		/// of the ring boundaries. Intersections of a boundary with the interior of the other ring are 
		/// filtered.</param>
		/// <returns></returns>
		[CanBeNull]
		public static IList<IntersectionPath3D> IntersectRings3D(
			[NotNull] List<Pnt3D> ring1Pnts3D,
			[NotNull] List<Pnt3D> ring2Pnts3D,
			double tolerance,
			bool boundaryIntersectionsOnly = false)
		{
			IBox box1 = GeomUtils.GetBoundingBox3D(ring1Pnts3D);
			IBox box2 = GeomUtils.GetBoundingBox3D(ring2Pnts3D);

			IBox bbIntersection = IntersectBoxes(box1, box2);

			if (bbIntersection == null)
			{
				return null;
			}

			if (boundaryIntersectionsOnly)
			{
				// Allow linear intersection of boundaries also for non-planar rings:
				return GetIntersectionLines3D(ring1Pnts3D, ring2Pnts3D, tolerance);
			}

			Plane3D plane1 = Plane3D.FitPlane(ring1Pnts3D, true);
			Plane3D plane2 = Plane3D.FitPlane(ring2Pnts3D, true);

			AssertCoplanarity(ring1Pnts3D, plane1, tolerance);
			AssertCoplanarity(ring2Pnts3D, plane2, tolerance);

			Pnt3D p0;
			Vector direction = IntersectPlanes(plane1, plane2, out p0);

			if (direction == null || MathUtils.AreEqual(direction.LengthSquared, 0))
			{
				if (! plane1.IsCoincident(plane2))
				{
					// Planes are parallel - no intersection
					return null;
				}

				// else: coincident
				IList<Linestring> resultRings = IntersectPlanar(
					new Linestring(ring1Pnts3D, true),
					new Linestring(ring2Pnts3D, true),
					tolerance);

				var result = new List<IntersectionPath3D>();
				foreach (Linestring resultRing in resultRings)
				{
					result.Add(
						new IntersectionPath3D(resultRing, RingPlaneTopology.InPlane));
				}

				return result;
			}

			// Optimization for non-coincident planes:
			Line3D planePlaneIntersectionInBox =
				Line3D.ConstructInBox(Assert.NotNull(p0), direction, bbIntersection);

			if (planePlaneIntersectionInBox == null ||
			    planePlaneIntersectionInBox.Length3D < tolerance)
			{
				return null;
			}

			// Actual processing of the ring segments:
			IList<IntersectionPath3D> ring1PlaneIntersections =
				GetRingPlaneIntersectionPaths(ring1Pnts3D, plane2, tolerance);
			IList<IntersectionPath3D> ring2PlaneIntersections =
				GetRingPlaneIntersectionPaths(ring2Pnts3D, plane1, tolerance);

			IList<IntersectionPath3D> intersections = GetIntersectionLines3D(
				ring1PlaneIntersections, ring2PlaneIntersections,
				tolerance);

			return intersections.Count == 0 ? null : intersections;
		}

		private static void AssertCoplanarity([NotNull] IEnumerable<Pnt3D> points,
		                                      [NotNull] Plane3D plane,
		                                      double tolerance)
		{
			if (! plane.IsDefined)
			{
				throw new ArgumentException(
					"The plane is not sufficiently defined by the input points");
			}

			foreach (Pnt3D pnt3D in points)
			{
				double d = plane.GetDistanceSigned(pnt3D);

				if (! MathUtils.AreEqual(d, 0, tolerance))
				{
					throw new ArgumentException(
						$"Coplanarity of point {pnt3D} with plane {plane} is violated: {d}m");
				}
			}
		}

		private static IList<IntersectionPath3D> GetIntersectionLines3D(
			[NotNull] IEnumerable<Pnt3D> ring1Pnts3D,
			[NotNull] IEnumerable<Pnt3D> ring2Pnts3D,
			double tolerance)
		{
			IList<Linestring> intersections = IntersectPaths3D(
				new Linestring(ring1Pnts3D),
				new Linestring(ring2Pnts3D),
				tolerance);

			return
				intersections
					.Select(path => new IntersectionPath3D(
						        path, RingPlaneTopology.InPlane))
					.ToList();
		}

		private static IList<IntersectionPath3D> GetIntersectionLines3D(
			[NotNull] IList<IntersectionPath3D> pathCollection1,
			[NotNull] IList<IntersectionPath3D> pathCollection2,
			double tolerance)
		{
			var result = new List<IntersectionPath3D>();

			foreach (IntersectionPath3D path1 in pathCollection1)
			{
				foreach (IntersectionPath3D path2 in pathCollection2)
				{
					IList<Linestring> intersections = IntersectPaths3D(
						path1.Segments, path2.Segments,
						tolerance);

					result.AddRange(
						intersections.Select(
							linestring =>
								new IntersectionPath3D(
									linestring, path1.RingPlaneTopology)));
				}
			}

			return result;
		}

		[NotNull]
		private static IList<Linestring> IntersectPaths3D(
			[NotNull] Linestring path1,
			[NotNull] Linestring path2,
			double tolerance)
		{
			// Assuming intersection has been checked previously

			var resultPaths = new List<Linestring>();

			var nextResultPath = new List<Line3D>();

			ICollection<Line3D> path2Collection =
				CollectionUtils.GetCollection(path2.Segments);

			foreach (Line3D line1 in path1.Segments)
			{
				foreach (Line3D line2 in path2Collection)
				{
					Line3D intersectionLine = IntersectLines3D(line1, line2, tolerance);

					if (intersectionLine != null && intersectionLine.Length3D > 0)
					{
						// add to next result path if it is empty or if the intersection line is adjacent to the previous intersection line:
						int previous = nextResultPath.Count - 1;
						if (nextResultPath.Count == 0 ||
						    nextResultPath[previous]
							    .EndPoint.Equals(intersectionLine.StartPoint))
						{
							nextResultPath.Add(intersectionLine);
						}
						else
						{
							// add the path, re-initialize
							resultPaths.Add(new Linestring(nextResultPath));

							nextResultPath = new List<Line3D> {intersectionLine};
						}
					}
				}
			}

			if (nextResultPath.Count > 0)
			{
				resultPaths.Add(new Linestring(nextResultPath));
			}

			return resultPaths;
		}

		private static IntersectionPath3D ToIntersectionPath(
			[NotNull] IList<Line3D> connectedLines,
			RingPlaneTopology ringPlaneTopology)
		{
			var linestring = new Linestring(connectedLines);

			var result = new IntersectionPath3D(linestring, ringPlaneTopology);

			return result;
		}

		/// <summary>
		/// Calculates the connected intersection points with the plane. The tolerance is applied as follows:
		/// - If a vertex is closer to the plane than the tolerance, it is added to the intersection 
		///   point list, unless the previous and the next segment are on the same side of the plane (not crossing).
		/// - No additional, exact intersection points are added for a segment, if the to-point is
		///   within the tolerance.
		/// The connection points are grouped into path groups which could directly be used to reshape.
		/// </summary>
		/// <param name="ringPoints"></param>
		/// <param name="plane"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		[NotNull]
		private static IList<IntersectionPath3D> GetRingPlaneIntersectionPaths(
			[NotNull] IList<Pnt3D> ringPoints,
			[NotNull] Plane3D plane,
			double tolerance)
		{
			List<double> vertexDistances = GetSnappedVertexDistances(ringPoints, plane,
				tolerance);

			if (vertexDistances.TrueForAll(d => Math.Abs(d) < tolerance))
			{
				// The planes are coincident and hence the intersection that respectes the tolerance is a polygon.
				// These cases could probabyl be better supported.
				return new List<IntersectionPath3D>
				       {
					       new IntersectionPath3D(new Linestring(ringPoints),
					                              RingPlaneTopology.InPlane)
				       };
			}

			List<IntersectionPoint3D> intersectionPoints =
				GetRingPlaneIntersectionPoints(ringPoints, plane, vertexDistances);

			List<IntersectionPath3D> intersectionPaths = GetInsideRingPaths(
				ringPoints, intersectionPoints, vertexDistances);

			return intersectionPaths;
		}

		private static List<double> GetSnappedVertexDistances(
			[NotNull] ICollection<Pnt3D> ringPoints,
			[NotNull] Plane3D plane, double tolerance)
		{
			var vertexDistances = new List<double>(ringPoints.Count);

			foreach (double distance in ringPoints.Select(plane.GetDistanceSigned))
			{
				vertexDistances.Add(Math.Abs(distance) < tolerance ? 0d : distance);
			}

			return vertexDistances;
		}

		/// <summary>
		/// Gets the intersection points between the ring boundary and the specified plane.
		/// </summary>
		/// <param name="ringPoints">The ring vertices</param>
		/// <param name="plane">The cut plane</param>
		/// <param name="vertexDistances">The snapped vertex distances between the respective
		/// ring points and the plane. If the point is less than the tolerance from the plane
		/// the vertex distance must be 0 ("snapped to plane").</param>
		/// <returns></returns>
		private static List<IntersectionPoint3D> GetRingPlaneIntersectionPoints(
			[NotNull] IList<Pnt3D> ringPoints, [NotNull] Plane3D plane,
			[NotNull] IList<double> vertexDistances)
		{
			var intersectionPoints = new List<IntersectionPoint3D>();

			// assuming closed ring (last == first), skipping the last vertex
			for (var i = 0; i < ringPoints.Count - 1; i++)
			{
				int previousIndex = i == 0 ? ringPoints.Count - 2 : i - 1;
				int nextIndex = i == ringPoints.Count - 1 ? 0 : i + 1;

				if (MathUtils.AreEqual(vertexDistances[i], 0))
				{
					if (vertexDistances[previousIndex] * vertexDistances[nextIndex] > 0)
					{
						// Exclude if the adjacent segements are both on the same side (the ring just visits the intersection plane at 1 vertex)
						continue;
					}

					// Including intermediate vertices (that were in the original geometry)
					intersectionPoints.Add(new IntersectionPoint3D(ringPoints[i], i));
				}
				else
				{
					// Not on the plane, check previous distance
					double previousDistance = vertexDistances[previousIndex];

					if (MathUtils.AreEqual(previousDistance, 0))
					{
						// the previous point was already added, do not add any intermediate 'high-precision' intersections,
						// even if the intersection would be at a large distance of the previous point (small angle between cut line and segment)
						continue;
					}

					if (previousDistance * vertexDistances[i] < 0)
					{
						// both on different side of the plane, calculate the actual intersection point on this segment
						Pnt3D intersectionPoint = plane.GetIntersectionPoint(
							ringPoints[previousIndex], ringPoints[i]);

						intersectionPoints.Add(new IntersectionPoint3D(
							                       Assert.NotNull(intersectionPoint),
							                       previousIndex + 0.5));
					}

					// else: no intersection
				}
			}

			return intersectionPoints;
		}

		///  <summary>
		///  Returns the list of paths that are within the specified ring.
		/// 
		///                 /\
		///                /  \    /\
		///     ----------*====*--*==*---------------the cut line (intersection of the two planes)----
		///              /      \/    \
		///             /______________\
		/// 
		///  IntersectionPoints: *
		///  Result: ====    ==
		/// 
		///  </summary>
		///  <param name="ringVertices">The vertices that build the ring.</param>
		/// <param name="intersectionPoints">The points of intersection between the ring and the cut line.</param>
		///  <param name="vertexDistances">Snapped vertex distances between the ring vertices and the cutting plain.</param>
		/// <returns></returns>
		private static List<IntersectionPath3D> GetInsideRingPaths(
			[NotNull] IList<Pnt3D> ringVertices,
			[NotNull] IList<IntersectionPoint3D> intersectionPoints,
			[NotNull] IList<double> vertexDistances)
		{
			var intersectionPaths = new List<IntersectionPath3D>();

			if (intersectionPoints.Count == 0)
			{
				return intersectionPaths;
			}

			Assert.False(intersectionPoints.Count == 1,
			             "Only one intersection point exists.");

			if (intersectionPoints.Count == 2)
			{
				AddIntersectionPath(intersectionPoints, vertexDistances,
				                    intersectionPaths);
			}
			else
			{
				// Order the points along the infinite plane intersection line:
				var line = new Line3D(intersectionPoints[0].Point,
				                      intersectionPoints[1].Point);

				Assert.True(line.IsDefined,
				            "The first and the second intersection points are equal. The ring might contain duplicate vertices or boundary loops.");

				// The first and the last intersection point along the infinite intersection line are 
				// actual start / end points of intersection lines that rung along the inside of the ring:
				// The general rule for finishing off an intersection line is that the ring boundary crosses the cut
				// line towards the same side as it initially arrived from.
				// For snail shell type geometries: The previous/next logic might change)

				IList<IntersectionPoint3D> orderedAlongCutline =
					intersectionPoints
						.OrderBy(tuple => line.GetDistanceAlong(tuple.Point, true))
						.ToList();

				// First arrival from left or right? 
				int previousIdx = orderedAlongCutline[0].GetPreviousRingVertexIndex(
					ringVertices.Count);

				double entryFrom = vertexDistances[previousIdx];

				if (MathUtils.AreEqual(entryFrom, 0))
				{
					// walk backwards along the ring (invert the lists rather than the logic):
					ringVertices = ringVertices.Reverse().ToList();
					vertexDistances = vertexDistances.Reverse().ToList();

					for (var i = 0; i < vertexDistances.Count; i++)
					{
						vertexDistances[i] *= -1;
					}

					foreach (IntersectionPoint3D intersection in orderedAlongCutline)
					{
						intersection.VirtualSourceVertex =
							ringVertices.Count - 1 - intersection.VirtualSourceVertex;
					}
				}

				AddInsideRingIntersectionPaths(ringVertices, vertexDistances,
				                               orderedAlongCutline,
				                               intersectionPaths);
			}

			return intersectionPaths;
		}

		/// <summary>
		/// Adds the intersection paths that connect the specified ordered intersection points
		/// which are inside the specified ring, or on its boundary.
		/// </summary>
		/// <param name="ringVertices">The vertices of the ring</param>
		/// <param name="vertexDistances">The ring vertices' distance to the cut plane</param>
		/// <param name="intersectionPointsAlongCutline">The ordered intersection points</param>
		/// <param name="intersectionPaths">Result list</param>
		private static void AddInsideRingIntersectionPaths(
			[NotNull] IList<Pnt3D> ringVertices,
			[NotNull] IList<double> vertexDistances,
			[NotNull] IList<IntersectionPoint3D> intersectionPointsAlongCutline,
			[NotNull] ICollection<IntersectionPath3D> intersectionPaths)
		{
			// New idea: walk along the polygon from intersection point to intersection point and
			//           classify the direct line between intersection point accordingly. If the
			//           ordered intersection points are not visited sequentially, the previously 
			//           detected interior intersection has to be split;
			// Alternatively we could just walk along the cut line and alternately classify as inside-outside-inside etc.
			// However, this would require testing each 

			List<IntersectionPoint3D> intersectionPointsAlongRing =
				intersectionPointsAlongCutline.OrderBy(ip => ip.VirtualSourceVertex).ToList();

			// Ring:    1         4
			//          /\        /\
			//         /  \______/  \    ______________________cut line/plane______________________
			//        /   2      3   \
			//       /________________\
			//       0/6               5

			IntersectionPoint3D firstIntersection = intersectionPointsAlongCutline[0];
			int firstIntersectedSegmentFromPointIdx =
				firstIntersection.GetPreviousRingVertexIndex(ringVertices.Count);
			double outside = vertexDistances[firstIntersectedSegmentFromPointIdx];

			int iCount = intersectionPointsAlongCutline.Count;

			var segmentsInPlane = new List<Line3D>();
			double entryFrom = outside;
			for (var currentAlongLineIdx = 1;
			     currentAlongLineIdx < intersectionPointsAlongCutline.Count;
			     currentAlongLineIdx++)
			{
				IntersectionPoint3D currentAlongIntersection =
					intersectionPointsAlongCutline[currentAlongLineIdx];
				IntersectionPoint3D previousAlongIntersection =
					intersectionPointsAlongCutline[currentAlongLineIdx - 1];

				if (IsRingSegmentInPlane(previousAlongIntersection,
				                         currentAlongIntersection,
				                         ringVertices.Count))
				{
					segmentsInPlane.Add(new Line3D(previousAlongIntersection.Point,
					                               currentAlongIntersection.Point));

					continue;
				}

				if (segmentsInPlane.Count > 0)
				{
					intersectionPaths.Add(ToIntersectionPath(segmentsInPlane,
					                                         RingPlaneTopology.InPlane));
					segmentsInPlane.Clear();

					// determine whether this in-plane stretch should be viewed as a crossing or a touching relationship:
					// Touching: 1         4                                    // Crossing: 1
					//          /\        /\									//          /\
					//         /  \______/  \    _____cut line/plane_____		//         /  \______
					//        /   2      3   \									//        /   2      \3 
					//       /________________\									//       /____________\
					//       0/6               5								//       0/5           4

					// in case of a touching, the inside-outside logic needs to be flipped:
					int exitToIdx =
						previousAlongIntersection.GetNextRingVertexIndex(
							ringVertices.Count);
					if (vertexDistances[exitToIdx] * entryFrom > 0)
					{
						// The ring exits the plane to the same side it has entered it
						outside *= -1;
					}
				}

				int previousAlongRingIdx =
					intersectionPointsAlongRing.IndexOf(previousAlongIntersection);

				if (currentAlongLineIdx == NextIndexInRing(previousAlongRingIdx, iCount))
				{
					// all normal, the ring hits the plane in sync with the ordered intersections
					if (entryFrom * outside > 0)
					{
						AddIntersectionPath(previousAlongIntersection,
						                    currentAlongIntersection, vertexDistances,
						                    intersectionPaths);
					}

					int enteredFromIdx =
						currentAlongIntersection.GetPreviousRingVertexIndex(
							ringVertices.Count);
					entryFrom = vertexDistances[enteredFromIdx];
				}
				else
				{
					// something else? just invert the ring-plane-topology?
					AddIntersectionPath(previousAlongIntersection,
					                    currentAlongIntersection, vertexDistances,
					                    intersectionPaths);

					int enteredFromIdx =
						currentAlongIntersection.GetPreviousRingVertexIndex(
							ringVertices.Count);
					entryFrom = vertexDistances[enteredFromIdx];
				}
			}

			if (segmentsInPlane.Count > 0)
			{
				intersectionPaths.Add(ToIntersectionPath(segmentsInPlane,
				                                         RingPlaneTopology.InPlane));
				segmentsInPlane.Clear();
			}
		}

		private static int NextIndexInRing(int currentIndex, int count)
		{
			return currentIndex == count - 1 ? 0 : currentIndex + 1;
		}

		private static void AddIntersectionPath(IntersectionPoint3D fromPoint,
		                                        IntersectionPoint3D toPoint,
		                                        IList<double> vertexDistances,
		                                        ICollection<IntersectionPath3D> resultList)
		{
			var intersectionPoints = new List<IntersectionPoint3D> {fromPoint, toPoint};

			AddIntersectionPath(intersectionPoints, vertexDistances, resultList);
		}

		/// <summary>
		/// Classifies the provided intersection path according the topological position
		/// (RingPlaneTopology.LeftNegative, RingPlaneTopology.InPlane, RingPlaneTopology.LeftPositive)
		/// of the ring vertices on the left of the cut line relative to the plane. If necessary, the
		/// provided intersection path is split into several paths.
		/// Ring:          1
		///                /\
		///               /  \
		///              /    \____3         ______________________cut line/plane______________________
		///             /          \
		///            /____________\
		///           0/5            4
		/// Ring with intersection path:
		///
		///                /\
		///               /  \
		///              /----\==== 
		///             /          \
		///            /____________\
		///
		///               ---- intersection within ring, the segments left of the intersection line above the plane (RingPlaneTopology.LeftPositive)
		///                    ==== intersection segment consisting of ring segment within plane (RingPlaneTopology.WithinPlane)
		/// </summary>
		/// <param name="intersectionPath"></param>
		/// <param name="vertexDistances"></param>
		/// <param name="resultList"></param>
		private static void AddIntersectionPath(
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPath,
			[NotNull] IList<double> vertexDistances,
			ICollection<IntersectionPath3D> resultList)
		{
			var result = new List<IntersectionPoint3D>();
			var resultInPlane = new List<Line3D>();
			IntersectionPoint3D previous = null;

			foreach (IntersectionPoint3D current in intersectionPath)
			{
				if (previous != null)
				{
					bool ringHasSegmentInPlane = IsRingSegmentInPlane(previous, current,
						vertexDistances
							.Count);

					if (ringHasSegmentInPlane)
					{
						resultInPlane.Add(new Line3D(previous.Point, current.Point));

						// Ring segment is in other cut plane - if there is already a start point, finish the path
						if (result.Count > 0)
						{
							resultList.Add(FinishIntersectionPath3D(
								               result, previous, vertexDistances));
							result.Clear();
						}
					}
					else
					{
						result.Add(previous);

						if (resultInPlane.Count > 0)
						{
							resultList.Add(ToIntersectionPath(resultInPlane,
							                                  RingPlaneTopology.InPlane));
							resultInPlane.Clear();
						}
					}
				}

				previous = current;
			}

			if (result.Count > 0 && previous != null)
			{
				resultList.Add(FinishIntersectionPath3D(result, previous, vertexDistances));
			}

			if (resultInPlane.Count > 0)
			{
				resultList.Add(ToIntersectionPath(resultInPlane, RingPlaneTopology.InPlane));
			}
		}

		private static bool IsRingSegmentInPlane(IntersectionPoint3D fromIntersection,
		                                         IntersectionPoint3D toIntersection,
		                                         int vertexCount)
		{
			// intersection points cutting the ring segment between index 7 and 8 have an index of 7.5
			bool bothIntersectionsAreRingVertices = fromIntersection.IsSourceVertex() &&
			                                        toIntersection.IsSourceVertex();

			if (! bothIntersectionsAreRingVertices)
			{
				return false;
			}

			// TODO: Generic method (in Linestring?): AreAdjacentVertices()
			int previousIndex = toIntersection.GetPreviousRingVertexIndex(vertexCount);

			double thisIndex = fromIntersection.VirtualSourceVertex;

			bool fromToIsSegment =
				MathUtils.AreEqual(thisIndex, previousIndex) ||
				previousIndex == 0 && MathUtils.AreEqual(thisIndex, vertexCount - 1);

			int nextIndex = toIntersection.GetNextRingVertexIndex(vertexCount);

			bool toFromIsSegment =
				MathUtils.AreEqual(thisIndex, nextIndex) ||
				nextIndex == vertexCount - 1 && MathUtils.AreEqual(thisIndex, 0);

			bool result = fromToIsSegment || toFromIsSegment;

			return result;
		}

		private static IntersectionPath3D FinishIntersectionPath3D(
			[NotNull] IList<IntersectionPoint3D> result,
			[NotNull] IntersectionPoint3D endPoint,
			[NotNull] IList<double> vertexDistances)
		{
			result.Add(endPoint);

			// Could be backward, e.g. in TLM_GEBAEUDE {FB35C6A4-99DD-41AB-B118-88211A0F0BF1}
			// TODO: could it also go across the 0-point?
			int firstIntersectionIdxInRing =
				result[0].VirtualSourceVertex < result[result.Count - 1].VirtualSourceVertex
					? 0
					: result.Count - 1;

			int vertexIndex =
				result[firstIntersectionIdxInRing]
					.GetNextRingVertexIndex(vertexDistances.Count);

			// find index with non-0 vertex distance:
			while (vertexIndex < vertexDistances.Count - 1 &&
			       MathUtils.AreEqual(vertexDistances[vertexIndex], 0))
			{
				vertexIndex++;
			}

			RingPlaneTopology ringPlaneTopology;
			if (MathUtils.AreEqual(vertexDistances[vertexIndex], 0))
			{
				ringPlaneTopology = RingPlaneTopology.InPlane;
			}
			else
			{
				ringPlaneTopology = vertexDistances[vertexIndex] > 0
					                    ? RingPlaneTopology.LeftPositive
					                    : RingPlaneTopology.LeftNegative;
			}

			var intersectionPath3D =
				new IntersectionPath3D(new Linestring(result.Select(i => i.Point)),
				                       ringPlaneTopology);

			return intersectionPath3D;
		}

		private static bool AreLinesParallel([NotNull] Line3D line1,
		                                     [NotNull] Line3D line2,
		                                     double tolerance)
		{
			double ratio = double.NaN;

			// TODO: Test for numerical issues for lines almost along an axis (x/y/z)

			if (MathUtils.AreEqual(line2.DirectionVector.X, 0))
			{
				if (! MathUtils.AreEqual(line1.DirectionVector.X, 0))
				{
					return false;
				}
			}
			else
			{
				ratio = line1.DirectionVector.X / line2.DirectionVector.X;
			}

			if (MathUtils.AreEqual(line2.DirectionVector.Y, 0))
			{
				if (! MathUtils.AreEqual(line1.DirectionVector.Y, 0))
				{
					return false;
				}
			}
			else
			{
				double yRatio = line1.DirectionVector.Y / line2.DirectionVector.Y;

				if (double.IsNaN(ratio))
				{
					ratio = yRatio;
				}
				else if (! MathUtils.AreEqual(ratio, yRatio, tolerance))
				{
					return false;
				}
			}

			double line1VectorZ = line1.DirectionVector[2];
			double line2VectorZ = line2.DirectionVector[2];

			if (MathUtils.AreEqual(line2VectorZ, 0))
			{
				return MathUtils.AreEqual(line1VectorZ, 0, tolerance);
			}

			if (! double.IsNaN(ratio))
			{
				double zRatio = line1VectorZ / line2VectorZ;
				return MathUtils.AreEqual(ratio, zRatio, tolerance);
			}

			return true;
		}

		#endregion

		#region Difference

		/// <summary>
		/// Returns the points from the source point list that are different from the target segment list.
		/// The target segments are interpreted as linestring, i.e. even in case it is a 2-dimensional
		/// geometry (i.e. closed rings), points completely within are not considered intersecting.
		/// X,Y,Z values are taken from the source.
		/// </summary>
		/// <param name="sourcePoints"></param>
		/// <param name="targetPoints"></param>
		/// <param name="tolerance"></param>
		/// <param name="in3d"></param>
		/// <returns></returns>
		public static IEnumerable<IPnt> GetDifferencePoints(
			[NotNull] IPointList sourcePoints,
			[NotNull] IPointList targetPoints,
			double tolerance,
			bool in3d)
		{
			foreach (var sourcePoint in sourcePoints.AsEnumerablePoints())
			{
				if (! PointExists(targetPoints, sourcePoint, tolerance, in3d))
				{
					yield return sourcePoint;
				}
			}
		}

		public static bool PointExists(IPointList inPointList,
		                               IPnt atPoint,
		                               double tolerance,
		                               bool compareZs)
		{
			foreach (var pointIdx in
				inPointList.FindPointIndexes(atPoint, tolerance, true))
			{
				if (compareZs)
				{
					// Compare z values:
					Pnt3D targetPnt3d = inPointList.GetPoint(pointIdx) as Pnt3D;
					Pnt3D sourcePnt3d = atPoint as Pnt3D;

					if (sourcePnt3d != null && targetPnt3d != null)
					{
						if (double.IsNaN(sourcePnt3d.Z) && double.IsNaN(targetPnt3d.Z))
						{
							return true;
						}

						if (MathUtils.AreEqual(sourcePnt3d.Z, targetPnt3d.Z, tolerance))
						{
							return true;
						}
					}

					if (sourcePnt3d == null && targetPnt3d == null)
					{
						// Both have no Z value
						return true;
					}
				}
				else
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		#region 2D Intersection

		/// <summary>
		/// Returns the points from the source point list that intersect the target point.
		/// X,Y,Z values are taken from the source.
		/// </summary>
		/// <param name="sourcePoints"></param>
		/// <param name="targetPoint"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] IPointList sourcePoints,
			[NotNull] IPnt targetPoint,
			double tolerance)
		{
			foreach (IntersectionPoint3D invertedIntersectionPoint in
				GetIntersectionPoints(targetPoint, sourcePoints, tolerance))
			{
				// The source was used as target, therefore the intersection point must be adapted:
				int sourceVertex = (int) invertedIntersectionPoint.VirtualTargetVertex;
				IPnt sourcePoint = sourcePoints.GetPoint(sourceVertex);
				var result = new IntersectionPoint3D(new Pnt3D(sourcePoint), sourceVertex)
				             {
					             VirtualTargetVertex =
						             invertedIntersectionPoint.VirtualSourceVertex,
					             Type = IntersectionPointType.TouchingInPoint
				             };

				yield return result;
			}
		}

		/// <summary>
		/// Returns the points from the source point list that intersect the target point list.
		/// X,Y,Z values are taken from the source.
		/// </summary>
		/// <param name="sourcePoints"></param>
		/// <param name="targetPoints"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] IPointList sourcePoints,
			[NotNull] IPointList targetPoints,
			double tolerance)
		{
			for (int i = 0; i < sourcePoints.PointCount; i++)
			{
				IPnt sourcePoint = sourcePoints.GetPoint(i);

				foreach (var intersectionPoint in GetIntersectionPoints(
					sourcePoint, targetPoints, tolerance, i))
				{
					yield return intersectionPoint;
				}
			}
		}

		/// <summary>
		/// Returns the points from the source point list that intersect the target segment list.
		/// The target segments are interpreted as linestring, i.e. even in case it is a 2-dimensional
		/// geometry (i.e. closed rings), points completely within are not considered intersecting.
		/// X,Y,Z values are taken from the source.
		/// </summary>
		/// <param name="sourcePoints"></param>
		/// <param name="targetSegments"></param>
		/// <param name="tolerance"></param>
		/// <param name="includeRingInteriorPoints"></param>
		/// <param name="includeRingStartEndPointDuplicates">Whether the intersections at the
		/// start- and end-point of a closed target should be reported as two distinct intersection
		/// points or whether only the start point intersection should be reported</param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] IPointList sourcePoints,
			[NotNull] ISegmentList targetSegments,
			double tolerance,
			bool includeRingInteriorPoints,
			bool includeRingStartEndPointDuplicates = false)
		{
			for (int i = 0; i < sourcePoints.PointCount; i++)
			{
				IPnt sourcePoint = sourcePoints.GetPoint(i);

				foreach (var intersectionPoint in GetIntersectionPoints(
					sourcePoint, i, targetSegments, tolerance, includeRingInteriorPoints,
					includeRingStartEndPointDuplicates))
				{
					yield return intersectionPoint;
				}
			}
		}

		/// <summary>
		/// Returns all the intersection points between the source segment list and the target point.
		/// Z values are taken from the source.
		/// </summary>
		/// <param name="sourceSegments">The source point which will determine the XYZ values of the found
		/// intersection point(s).</param>
		/// <param name="targetPoint">The target point</param>
		/// <param name="targetPointIndex">The index of the target point</param>
		/// <param name="tolerance"></param>
		/// <param name="includeRingInteriorPoints"></param>
		/// <param name="includeRingStartEndPointDuplicates">Whether the intersections at the
		/// start- and end-point of a closed target should be reported as two distinct intersection
		/// points or whether only the start point intersection should be reported.</param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] ISegmentList sourceSegments,
			[NotNull] IPnt targetPoint,
			int targetPointIndex,
			double tolerance,
			bool includeRingInteriorPoints,
			bool includeRingStartEndPointDuplicates = false)
		{
			bool foundSegmentIntersection = false;
			IntersectionPoint3D sourceFromPointIntersection = null;
			IntersectionPoint3D previousIntersection = null;
			foreach (KeyValuePair<int, Line3D> segmentByIndex in
				sourceSegments.FindSegments(targetPoint, tolerance))
			{
				int sourceIndex = segmentByIndex.Key;

				IntersectionPoint3D intersectionPoint =
					IntersectionPoint3D.CreateLinePointIntersection(
						sourceSegments, targetPoint, sourceIndex, targetPointIndex, tolerance);

				if (intersectionPoint == null)
				{
					previousIntersection = null;
					continue;
				}

				if (! includeRingStartEndPointDuplicates)
				{
					// Filter duplicate intersections at ring From/To point:
					if (intersectionPoint.VirtualSourceVertex == 0)
					{
						// Remember intersection of from point:
						sourceFromPointIntersection = intersectionPoint;
					}
					else if (sourceFromPointIntersection != null &&
					         intersectionPoint.SourcePartIndex ==
					         sourceFromPointIntersection.SourcePartIndex &&
					         intersectionPoint.IsAtSourceRingEndPoint(sourceSegments))
					{
						previousIntersection = intersectionPoint;
						continue;
					}
				}

				// Filter duplicates of the type (previous segment end point == this segment start point).
				if (previousIntersection == null ||
				    // ReSharper disable once CompareOfFloatsByEqualityOperator
				    previousIntersection.VirtualSourceVertex !=
				    intersectionPoint.VirtualSourceVertex ||
				    previousIntersection.SourcePartIndex != intersectionPoint.SourcePartIndex)
				{
					yield return intersectionPoint;
					foundSegmentIntersection = true;
				}

				previousIntersection = intersectionPoint;
			}

			if (includeRingInteriorPoints && ! foundSegmentIntersection &&
			    sourceSegments.IsClosed &&
			    GeomRelationUtils.PolycurveContainsXY(sourceSegments, targetPoint, tolerance))
			{
				// TODO: Consider finding out the source part index
				yield return new IntersectionPoint3D(new Pnt3D(targetPoint), double.NaN)
				             {
					             Type = IntersectionPointType.AreaInterior
				             };
			}
		}

		/// <summary>
		/// Returns the points from the source segment list that intersect the target points.
		/// The target segments are interpreted as linestring, i.e. even in case it is a 2-dimensional
		/// geometry (i.e. closed rings), points completely within are not considered intersecting.
		/// X,Y,Z values are taken from the source.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="targetPoints"></param>
		/// <param name="tolerance"></param>
		/// <param name="includeRingInteriorPoints"></param>
		/// <param name="includeRingStartEndPointDuplicates">Whether the intersections at the
		/// start- and end-point of a closed target should be reported as two distinct intersection
		/// points or whether only the start point intersection should be reported</param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] ISegmentList sourceSegments,
			[NotNull] IPointList targetPoints,
			double tolerance,
			bool includeRingInteriorPoints,
			bool includeRingStartEndPointDuplicates = false)
		{
			if (GeomRelationUtils.AreBoundsDisjoint(sourceSegments, targetPoints, tolerance))
			{
				yield break;
			}

			for (int i = 0; i < targetPoints.PointCount; i++)
			{
				IPnt targetPoint = targetPoints.GetPoint(i);

				foreach (var intersectionPoint in GetIntersectionPoints(
					sourceSegments, targetPoint, i, tolerance, includeRingInteriorPoints,
					includeRingStartEndPointDuplicates))
				{
					yield return intersectionPoint;
				}
			}
		}

		/// <summary>
		/// Gets the intersection points between 2 segment lists, such as line strings, polylines or polygon boundaries.
		/// Z values are taken from the source.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments"></param>
		/// <param name="tolerance"></param>
		/// <param name="includeLinearIntersectionIntermediateRingStartEndPoints">
		/// Whether the start/end points of rings that are on the interior of a linear intersection
		/// should be included in the result as linear start/end intersection points.</param>
		/// <param name="includeLinearIntersectionIntermediatePoints">Whether all intermediate vertices
		/// along linear intersections should be included in the result.</param>
		/// <returns></returns>
		public static IList<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments,
			double tolerance,
			bool includeLinearIntersectionIntermediateRingStartEndPoints = true,
			bool includeLinearIntersectionIntermediatePoints = false)
		{
			List<SegmentIntersection> intersections =
				SegmentIntersectionUtils.GetSegmentIntersectionsXY(
					sourceSegments, targetSegments, tolerance).ToList();

			if (intersections.Count == 0)
			{
				return new List<IntersectionPoint3D>(0);
			}

			IList<IntersectionPoint3D> intersectionPoints = GetIntersectionPoints(
				sourceSegments, targetSegments, tolerance, intersections,
				includeLinearIntersectionIntermediateRingStartEndPoints,
				includeLinearIntersectionIntermediatePoints);

			return intersectionPoints;
		}

		/// <summary>
		/// Gets the intersection points between 2 linestrings where the segment intersections
		/// have already been pre-calculated.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments"></param>
		/// <param name="tolerance"></param>
		/// <param name="allIntersections">The segment intersections, must be sorted along the source.</param>
		/// <param name="includeLinearIntersectionIntermediateRingStartEndPoints"></param>
		/// <param name="includeLinearIntersectionIntermediatePoints"></param>
		/// <returns></returns>
		public static IList<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments,
			double tolerance,
			[NotNull] IEnumerable<SegmentIntersection> allIntersections,
			bool includeLinearIntersectionIntermediateRingStartEndPoints = true,
			bool includeLinearIntersectionIntermediatePoints = false)
		{
			IEnumerable<SegmentIntersection> sortedRelevantIntersections =
				SegmentIntersectionUtils.GetFilteredIntersectionsOrderedAlongSourceSegments(
					allIntersections, sourceSegments);

			IList<IntersectionPoint3D> intersectionPoints =
				SegmentIntersectionUtils.CollectIntersectionPoints(
					sortedRelevantIntersections, sourceSegments, targetSegments, tolerance,
					includeLinearIntersectionIntermediatePoints);

			if (! includeLinearIntersectionIntermediateRingStartEndPoints)
			{
				FilterLinearIntersectionBreaksAtRingStart(
					sourceSegments, targetSegments, intersectionPoints);
			}

			return intersectionPoints;
		}

		/// <summary>
		/// Returns all the intersection points between the source point and the target point list.
		/// Z values are taken from the source.
		/// </summary>
		/// <param name="sourcePoint">The source point which will determine the XYZ values of the found
		/// intersection point(s).</param>
		/// <param name="sourceIndex">The index of the source point</param>
		/// <param name="targetPoints">The target point list</param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] IPnt sourcePoint,
			[NotNull] IPointList targetPoints,
			double tolerance,
			int sourceIndex = 0)
		{
			foreach (int targetIndex in targetPoints.FindPointIndexes(
				sourcePoint, tolerance))
			{
				IntersectionPoint3D intersectionPoint =
					IntersectionPoint3D.CreatePointPointIntersection(
						sourcePoint, sourceIndex, targetPoints, targetIndex, tolerance);

				if (intersectionPoint != null)
				{
					yield return intersectionPoint;
				}
			}
		}

		/// <summary>
		/// Returns all the intersection points between the source point and the target a segment list.
		/// Z values are taken from the source.
		/// </summary>
		/// <param name="sourcePoint">The source point which will determine the XYZ values of the found
		/// intersection point(s).</param>
		/// <param name="sourceIndex">The index of the source point</param>
		/// <param name="targetSegments">The target segment list</param>
		/// <param name="tolerance"></param>
		/// <param name="includeRingInteriorPoints"></param>
		/// <param name="includeRingStartEndPointDuplicates">Whether the intersections at the
		/// start- and end-point of a closed target should be reported as two distinct intersection
		/// points or whether only the start point intersection should be reported.</param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPoint3D> GetIntersectionPoints(
			[NotNull] IPnt sourcePoint,
			int sourceIndex,
			[NotNull] ISegmentList targetSegments,
			double tolerance,
			bool includeRingInteriorPoints,
			bool includeRingStartEndPointDuplicates = false)
		{
			bool foundSegmentIntersection = false;
			IntersectionPoint3D targetFromPointIntersection = null;
			IntersectionPoint3D previousIntersection = null;
			foreach (KeyValuePair<int, Line3D> segmentByIndex in targetSegments.FindSegments(
				sourcePoint, tolerance))
			{
				int targetIndex = segmentByIndex.Key;

				IntersectionPoint3D intersectionPoint =
					IntersectionPoint3D.CreatePointLineIntersection(
						sourcePoint, targetSegments, sourceIndex, targetIndex, tolerance);

				if (intersectionPoint == null)
				{
					previousIntersection = null;
					continue;
				}

				if (! includeRingStartEndPointDuplicates)
				{
					// Filter duplicate intersections at ring From/To point:
					if (intersectionPoint.VirtualTargetVertex == 0)
					{
						// Remember intersection of from point:
						targetFromPointIntersection = intersectionPoint;
					}
					else if (targetFromPointIntersection != null &&
					         intersectionPoint.TargetPartIndex ==
					         targetFromPointIntersection.TargetPartIndex &&
					         intersectionPoint.IsAtTargetRingEndPoint(targetSegments))
					{
						previousIntersection = intersectionPoint;
						continue;
					}
				}

				// Filter duplicates of the type (previous segment end point == this segment start point).
				if (previousIntersection == null ||
				    // ReSharper disable once CompareOfFloatsByEqualityOperator
				    previousIntersection.VirtualTargetVertex !=
				    intersectionPoint.VirtualTargetVertex ||
				    previousIntersection.TargetPartIndex != intersectionPoint.TargetPartIndex)
				{
					yield return intersectionPoint;
					foundSegmentIntersection = true;
				}

				previousIntersection = intersectionPoint;
			}

			if (includeRingInteriorPoints && ! foundSegmentIntersection &&
			    targetSegments.IsClosed &&
			    GeomRelationUtils.PolycurveContainsXY(targetSegments, sourcePoint, tolerance))
			{
				// TODO: Consider finding out the source part index
				yield return new IntersectionPoint3D(new Pnt3D(sourcePoint), double.NaN)
				             {
					             Type = IntersectionPointType.AreaInterior
				             };
			}
		}

		public static IList<IntersectionPoint3D> GetSelfIntersectionPoints(
			[NotNull] ISegmentList linestring,
			double tolerance)
		{
			var filteredSelfIntersections = new List<SegmentIntersection>();

			for (int i = 0; i < linestring.SegmentCount; i++)
			{
				var linearSelfIntersections = new List<SegmentIntersection>(
					SegmentIntersectionUtils.GetRelevantSelfIntersectionsXY(i, linestring[i],
						                        linestring, tolerance)
					                        .Where(li => li.HasLinearIntersection));

				filteredSelfIntersections.AddRange(linearSelfIntersections);
			}

			IList<IntersectionPoint3D> intersectionPoints = GetIntersectionPoints(
				linestring, linestring, tolerance,
				filteredSelfIntersections,
				true, true);
			return intersectionPoints;
		}

		private static void FilterLinearIntersectionBreaksAtRingStart(
			ISegmentList sourceSegments,
			ISegmentList targetSegments,
			IList<IntersectionPoint3D> intersectionPoints)
		{
			if (intersectionPoints.Count == 0)
			{
				return;
			}

			if (! sourceSegments.IsClosed && ! targetSegments.IsClosed)
			{
				return;
			}

			IntersectionPoint3D previousLinearEnd = null;

			// The last linear intersection can be connected to the first via ring null point:
			var intersectionPointsRing = new List<IntersectionPoint3D>(intersectionPoints);

			intersectionPointsRing.Add(intersectionPoints[0]);

			foreach (IntersectionPoint3D intersectionPoint in intersectionPointsRing)
			{
				if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart &&
				    previousLinearEnd != null &&
				    intersectionPoint.Point.Equals(previousLinearEnd.Point) &&
				    IsIntersectionAtPartStartOrEnd(sourceSegments, targetSegments,
				                                   intersectionPoint))
				{
					intersectionPoints.Remove(previousLinearEnd);
					intersectionPoints.Remove(intersectionPoint);
				}

				previousLinearEnd =
					intersectionPoint.Type == IntersectionPointType.LinearIntersectionEnd
						? intersectionPoint
						: null;
			}
		}

		private static bool IsIntersectionAtPartStartOrEnd(
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			[NotNull] IntersectionPoint3D intersectionPoint)
		{
			return IsPointAtPartStartOrEnd(source, intersectionPoint.SourcePartIndex,
			                               intersectionPoint.VirtualSourceVertex) ||
			       IsPointAtPartStartOrEnd(target, intersectionPoint.TargetPartIndex,
			                               intersectionPoint.VirtualTargetVertex);
		}

		private static bool IsPointAtPartStartOrEnd(ISegmentList segmentList,
		                                            int partIndex,
		                                            double localPointAlongIndex)
		{
			var vertexIndexIntegral = (int) localPointAlongIndex;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			bool isVertex = localPointAlongIndex == vertexIndexIntegral;

			if (! isVertex)
			{
				return false;
			}

			// TODO: Add GetGlobalPointIndex and IsVertexIndexAtLinestringStart etc.?
			//       Or even add GetLinestring(int index) to ISegmentList?

			Linestring linestring = segmentList as Linestring;

			if (linestring == null)
			{
				linestring = ((MultiLinestring) segmentList).GetLinestring(partIndex);
			}

			return linestring.IsFirstPointInPart(vertexIndexIntegral) ||
			       linestring.IsLastPointInPart(vertexIndexIntegral);
		}

		#region Cut / Intersect

		public static RotationAxis GetPreferredRotationAxis(IBoundedXY geometry)
		{
			bool rotationAxisX = geometry.XMax - geometry.XMin >
			                     geometry.YMax - geometry.YMin;

			return rotationAxisX ? RotationAxis.X : RotationAxis.Y;
		}

		public static IEnumerable<Linestring> Rotate(
			[NotNull] IEnumerable<Linestring> linestrings,
			RotationAxis rotationAxis)
		{
			return linestrings.Select(ring => Rotate(ring, rotationAxis));
		}

		public static Linestring Rotate([NotNull] Linestring linestring,
		                                RotationAxis rotationAxis)
		{
			// TODO: Some rings have startPoint reference-equals endPoint which messes up when updating:
			// -> for the moment, copy everything

			return new Linestring(rotationAxis == RotationAxis.X
				                      ? GeomUtils.RotateX90(linestring.GetPoints(), true)
				                      : GeomUtils.RotateY90(linestring.GetPoints(), true));
		}

		public static IEnumerable<Linestring> RotateBack(
			[NotNull] IEnumerable<Linestring> linestrings,
			RotationAxis rotationAxis,
			bool reverseOrientation = false)
		{
			foreach (Linestring ring in linestrings)
			{
				if (reverseOrientation)
					ring.ReverseOrientation();

				yield return RotateBack(ring, rotationAxis);
			}
		}

		public static Linestring RotateBack([NotNull] Linestring linestring,
		                                    RotationAxis rotationAxis)
		{
			// TODO: Some rings have startPoint == endPoint which messes up when updating:
			IEnumerable<Pnt3D> copiedPoints = linestring.GetPoints(0, null, true);

			return new Linestring(rotationAxis == RotationAxis.X
				                      ? GeomUtils.RotateX90Back(copiedPoints)
				                      : GeomUtils.RotateY90Back(copiedPoints));
		}

		#endregion

		/// <summary>
		/// Determines whether the provided linestrings are coincident in the XY plane.
		/// Additional coincidence in Z can be specified. However, this method always
		/// tests in the XY plane and does not support 3D geometries (that are non-simple
		/// in XY).
		/// </summary>
		/// <param name="ring1"></param>
		/// <param name="ring2"></param>
		/// <param name="tolerance"></param>
		/// <param name="coincidentAlsoInZ"></param>
		/// <returns></returns>
		public static bool AreEqualXY([NotNull] Linestring ring1,
		                              [NotNull] Linestring ring2,
		                              double tolerance,
		                              bool coincidentAlsoInZ = false)
		{
			IEnumerable<SegmentIntersection> linearIntersections =
				SegmentIntersectionUtils.GetSegmentIntersectionsXY(
					                        ring1, ring2, tolerance)
				                        .Where(i => i.HasLinearIntersection);

			Predicate<SegmentIntersection> extraPredicate = null;

			if (coincidentAlsoInZ)
			{
				var toleranceSquared = tolerance * tolerance;

				extraPredicate =
					linearIntersection =>
						! IsLinearIntersectionDifferentInZ(
							linearIntersection,
							Assert.NotNull(
								linearIntersection.TryGetIntersectionLine(ring1)),
							ring2, toleranceSquared);
			}

			return AreEqualXY(ring1.Segments, linearIntersections, extraPredicate);
		}

		private static bool AreEqualXY(
			[NotNull] IList<Line3D> ring1,
			[NotNull] IEnumerable<SegmentIntersection> linearIntersections,
			Predicate<SegmentIntersection> extraPredicate = null)
		{
			int currentSegmentIdx = -1;
			double currentSourceCoverageFactor = 1;
			foreach (SegmentIntersection linearIntersection in linearIntersections)
			{
				if (currentSegmentIdx == linearIntersection.SourceIndex - 1)
				{
					currentSegmentIdx = linearIntersection.SourceIndex;

					if (currentSourceCoverageFactor < 1)
					{
						// The previous segment was not fully covered
						return false;
					}

					currentSourceCoverageFactor = 0;
				}
				else if (currentSegmentIdx < linearIntersection.SourceIndex - 1)
				{
					// missing intersection
					return false;
				}
				else
				{
					Assert.True(linearIntersection.SourceIndex >= currentSegmentIdx,
					            "Linear intersections must be ordered.");
				}

				double linearIntersectionStartFactor =
					linearIntersection.GetLinearIntersectionStartFactor(true);

				double linearIntersectionEndFactor =
					linearIntersection.GetLinearIntersectionEndFactor(true);

				if (currentSourceCoverageFactor < linearIntersectionStartFactor)
				{
					return false;
				}

				if (extraPredicate != null && ! extraPredicate(linearIntersection))
				{
					return false;
				}

				currentSourceCoverageFactor = linearIntersectionEndFactor;
			}

			return currentSegmentIdx == ring1.Count - 1 &&
			       MathUtils.AreEqual(1.0, currentSourceCoverageFactor);
		}

		/// <summary>
		/// Orders the input list (which is already ordered by source index) 
		/// along the source segments, if there are several items per source index.
		/// </summary>
		/// <param name="intersections">The intersections.</param>
		/// <returns></returns>
		public static IEnumerable<SegmentIntersection> OrderAlongSourceSegments(
			[NotNull] IEnumerable<SegmentIntersection> intersections)
		{
			// This could probably be optimized for the 99.9% where each source has only 1 target line
			var intersectionItemsForCurrentSourceSegment =
				new List<SegmentIntersection>(3);

			int currentIndex = -1;
			foreach (SegmentIntersection intersection in intersections)
			{
				if (intersection.SourceIndex != currentIndex)
				{
					// New source index,
					currentIndex = intersection.SourceIndex;

					// emit the collected intersections
					foreach (SegmentIntersection collectedIntersection in
						intersectionItemsForCurrentSourceSegment.OrderBy(
							i => i.GetFirstIntersectionAlongSource()))
					{
						yield return collectedIntersection;
					}

					intersectionItemsForCurrentSourceSegment.Clear();
				}

				intersectionItemsForCurrentSourceSegment.Add(intersection);
			}

			foreach (SegmentIntersection collectedIntersection in
				intersectionItemsForCurrentSourceSegment.OrderBy(
					i => i.GetFirstIntersectionAlongSource()))
			{
				yield return collectedIntersection;
			}
		}

		#region 2D Linear intersections (lines / areas)

		/// <summary>
		/// Gets the linear intersection result between the source lines and the target area.
		/// Optionally the intersection between the target boundary can be excluded.
		/// Vertical (but co-planar) target rings and vertical source lines are supported.
		/// </summary>
		/// <param name="sourceLines">Source lines</param>
		/// <param name="targetRings">Target area (ring/polygon)</param>
		/// <param name="tolerance"></param>
		/// <param name="excludeTargetBoundaryIntersections">Whether the intersection of source
		/// lines with the boundary of the target should be excluded.</param>
		/// <returns></returns>
		public static IEnumerable<Linestring> GetRingIntersectionLinesPlanar(
			ISegmentList sourceLines,
			ISegmentList targetRings,
			double tolerance,
			bool excludeTargetBoundaryIntersections = false)
		{
			if (sourceLines.IsEmpty || targetRings.IsEmpty)
			{
				yield break;
			}

			bool sourceIsVertical = GeomUtils.IsVertical(sourceLines, tolerance);
			bool targetIsVertical = GeomUtils.IsVertical(targetRings, tolerance);

			if (sourceIsVertical || targetIsVertical)
			{
				RotationAxis rotationAxis = GetPreferredRotationAxis(sourceLines);

				ISegmentList sourceRotated = RotateSegments(sourceLines, rotationAxis);
				ISegmentList targetRotated = RotateSegments(targetRings, rotationAxis);

				bool orientationReversed = false;
				if (targetRotated.IsClosed)
				{
					Linestring firstRing = targetRotated.GetPart(0);

					if (firstRing.ClockwiseOriented == false)
					{
						targetRotated.ReverseOrientation();
						orientationReversed = true;
					}
				}

				IEnumerable<IntersectionPath3D> rotatedResult = GetRingIntersectionLinesXY(
					sourceRotated, targetRotated, tolerance, excludeTargetBoundaryIntersections);

				foreach (Linestring linestring in RotateBack(
					rotatedResult.Select(rr => rr.Segments), rotationAxis, orientationReversed))
				{
					yield return linestring;
				}
			}
			else
			{
				// TODO: Re-use intersection points
				foreach (IntersectionPath3D intersectionPath in GetRingIntersectionLinesXY(
					sourceLines, targetRings, tolerance, excludeTargetBoundaryIntersections))
				{
					yield return intersectionPath.Segments;
				}
			}
		}

		/// <summary>
		/// Gets the linear intersection result between the source lines and the target area.
		/// Optionally the intersection between the target boundary can be excluded.
		/// </summary>
		/// <param name="sourceLines">Source lines</param>
		/// <param name="targetRings">Target area (ring/polygon)</param>
		/// <param name="tolerance"></param>
		/// <param name="excludeTargetBoundaryIntersections">Whether the intersection of source
		/// lines with the boundary of the target should be excluded.</param>
		/// <returns></returns>
		public static IEnumerable<IntersectionPath3D> GetRingIntersectionLinesXY(
			ISegmentList sourceLines,
			ISegmentList targetRings,
			double tolerance,
			bool excludeTargetBoundaryIntersections = false)
		{
			if (GeomRelationUtils.AreBoundsDisjoint(sourceLines, targetRings, tolerance))
			{
				yield break;
			}

			Assert.True(targetRings.IsClosed,
			            "Target is must be closed if ring interior is required.");

			IList<IntersectionPoint3D> intersectionPoints =
				GetIntersectionPoints(sourceLines, targetRings, tolerance);

			var intersectionsPerSourcePartIdx = intersectionPoints.GroupBy(ip => ip.SourcePartIndex)
				.ToDictionary(g => g.Key, g => g.ToList());

			for (int i = 0; i < sourceLines.PartCount; i++)
			{
				Linestring sourceLinestring = sourceLines.GetPart(i);

				if (! intersectionsPerSourcePartIdx.TryGetValue(
					    i, out List<IntersectionPoint3D> intersections) ||
				    intersections.Count == 0)
				{
					// The source ring is completely inside or completely outside the target area:
					if (GeomRelationUtils.PolycurveContainsXY(
						targetRings, sourceLinestring.StartPoint, tolerance))
					{
						yield return
							new IntersectionPath3D(sourceLinestring,
							                       RingPlaneTopology.InPlane)
							{
								StartIntersection =
									IntersectionPoint3D.CreateAreaInteriorIntersection(
										sourceLinestring.StartPoint, 0),
								EndIntersection =
									IntersectionPoint3D.CreateAreaInteriorIntersection(
										sourceLinestring.EndPoint,
										sourceLinestring.PointCount - 1)
							};
					}
				}
				else
				{
					foreach (IntersectionPath3D intersectionPath3D in
						FollowIntersectionsThroughTargetRings(
							intersections, sourceLinestring, targetRings, tolerance,
							excludeTargetBoundaryIntersections))
					{
						yield return intersectionPath3D;
					}
				}
			}
		}

		private static IEnumerable<IntersectionPath3D> FollowIntersectionsThroughTargetRings(
			[NotNull] List<IntersectionPoint3D> intersections,
			[NotNull] Linestring sourceLinestring,
			[NotNull] ISegmentList targetRings,
			double tolerance,
			bool excludeTargetBoundaryIntersections)
		{
			IntersectionPoint3D linearStart = null;

			if (intersections[0].VirtualSourceVertex > 0)
			{
				// Start is no intersection point. Start point within target?
				if (GeomRelationUtils.PolycurveContainsXY(
					targetRings, sourceLinestring.StartPoint, tolerance))
				{
					linearStart = IntersectionPoint3D.CreateAreaInteriorIntersection(
						sourceLinestring.StartPoint, 0);
				}
			}

			foreach (IntersectionPoint3D intersectionPoint in intersections)
			{
				if (linearStart != null)
				{
					yield return EmitSourceSubcurve(sourceLinestring, linearStart,
					                                intersectionPoint);

					if (RestartLinearIntersection(sourceLinestring, intersectionPoint, targetRings,
					                              excludeTargetBoundaryIntersections))
					{
						linearStart = intersectionPoint;
					}
					else
					{
						linearStart = null;
					}
				}
				else
				{
					if (intersectionPoint.Type == IntersectionPointType.Crossing)
					{
						linearStart = intersectionPoint;
					}

					if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart &&
					    ! excludeTargetBoundaryIntersections)
					{
						linearStart = intersectionPoint;
					}

					if (intersectionPoint.Type == IntersectionPointType.TouchingInPoint ||
					    intersectionPoint.Type == IntersectionPointType.LinearIntersectionEnd)
					{
						if (intersectionPoint.SourceContinuesInbound(
							    sourceLinestring, targetRings) == true)
						{
							linearStart = intersectionPoint;
						}
					}
				}
			}

			IntersectionPoint3D lastIntersection = intersections[intersections.Count - 1];

			if (linearStart != null &&
			    lastIntersection.VirtualSourceVertex < sourceLinestring.PointCount - 1 &&
			    GeomRelationUtils.PolycurveContainsXY(
				    targetRings, sourceLinestring.EndPoint, tolerance))
			{
				// Dangling to the inside:
				var insideEnd =
					IntersectionPoint3D.CreateAreaInteriorIntersection(
						sourceLinestring.EndPoint, sourceLinestring.PointCount - 1);

				yield return EmitSourceSubcurve(sourceLinestring, linearStart, insideEnd);
			}
		}

		private static bool RestartLinearIntersection(
			Linestring sourceLinestring, IntersectionPoint3D intersectionPoint,
			ISegmentList targetRings,
			bool excludeTargetBoundaryIntersections)
		{
			if (intersectionPoint.Type == IntersectionPointType.TouchingInPoint)
			{
				// Touching from the inside, re-start
				return true;
			}

			if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart)
			{
				return ! excludeTargetBoundaryIntersections;
			}

			if (intersectionPoint.Type == IntersectionPointType.Crossing)
			{
				return false;
			}

			if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionEnd)
			{
				// But if it's not the last point, it could continue inbound after the stretch along the boundary
				if (intersectionPoint.VirtualSourceVertex < sourceLinestring.PointCount - 1)
				{
					if (intersectionPoint.SourceContinuesInbound(
						    sourceLinestring, targetRings) == true)
					{
						return true;
					}
				}
			}

			return false;
		}

		private static IntersectionPath3D EmitSourceSubcurve(Linestring sourceLinestring,
		                                                     IntersectionPoint3D linearStart,
		                                                     IntersectionPoint3D linearEnd)
		{
			int startSegmentIdx =
				linearStart.GetLocalSourceIntersectionSegmentIdx(
					sourceLinestring, out double startRatio);

			int endSegmentIdx =
				linearEnd.GetLocalSourceIntersectionSegmentIdx(
					sourceLinestring, out double endRatio);

			Linestring resultSubcurve = sourceLinestring.GetSubcurve(
				startSegmentIdx, startRatio, endSegmentIdx, endRatio, false);

			// TODO: The side should be determined by the plane!
			RingPlaneTopology ringPlaneTopology =
				linearStart.Type == IntersectionPointType.LinearIntersectionStart
					? RingPlaneTopology.InPlane
					: RingPlaneTopology.LeftPositive;

			return new IntersectionPath3D(resultSubcurve,
			                              ringPlaneTopology)
			       {
				       StartIntersection = linearStart,
				       EndIntersection = linearEnd
			       };
		}

		#endregion

		#region 2D Linear intersections (lines / lines)

		public static IList<Linestring> GetIntersectionLinesXY(
			[NotNull] Linestring linestring1,
			[NotNull] ISegmentList targetSegments,
			double tolerance)
		{
			if (! linestring1.ExtentsIntersectXY(
				    targetSegments.XMin, targetSegments.YMin,
				    targetSegments.XMax, targetSegments.YMax,
				    tolerance))
			{
				return new List<Linestring>(0);
			}

			var intersections = SegmentIntersectionUtils.GetSegmentIntersectionsXY(
				linestring1, targetSegments, tolerance, true);

			IEnumerable<SegmentIntersection> orderedIntersections =
				OrderAlongSourceSegments(intersections.Where(i => i.HasLinearIntersection));

			// Consider getting intersection points and using segmentList1.GetSubcurve()
			// between linear intersection points. This would yield another ~100ms per 100K segments.
			IList<Linestring> result = CollectIntersectionPaths(orderedIntersections, linestring1);

			return result;
		}

		[NotNull]
		public static IList<IntersectionPoint3D> GetSelfIntersections(
			[NotNull] ISegmentList segments,
			double tolerance,
			bool includeLinearIntersectionIntermediatePoints = false)
		{
			var selfIntersections = new List<SegmentIntersection>();

			int globalIndex = 0;
			foreach (Line3D sourceLine in segments)
			{
				selfIntersections.AddRange(
					SegmentIntersectionUtils.GetRelevantSelfIntersectionsXY(
						globalIndex++, sourceLine, segments, tolerance));
			}

			IEnumerable<SegmentIntersection> sortedRelevantIntersections =
				SegmentIntersectionUtils.GetFilteredIntersectionsOrderedAlongSourceSegments(
					selfIntersections, segments);

			IList<IntersectionPoint3D> intersectionPoints =
				SegmentIntersectionUtils.CollectIntersectionPoints(
					sortedRelevantIntersections, segments, segments, tolerance,
					includeLinearIntersectionIntermediatePoints);

			return intersectionPoints;
		}

		[NotNull]
		public static IList<Linestring> GetLinearSelfIntersectionsXY(
			[NotNull] ISegmentList segmentList,
			int sourceSegmentIndex,
			double tolerance,
			bool in3D = false)
		{
			Line3D sourceLine = segmentList[sourceSegmentIndex];

			var linearSelfIntersections = new List<SegmentIntersection>(
				SegmentIntersectionUtils.GetRelevantSelfIntersectionsXY(
					sourceSegmentIndex, sourceLine, segmentList,
					tolerance).Where(i => i.HasLinearIntersection));

			if (linearSelfIntersections.Count == 0)
			{
				return new List<Linestring>(0);
			}

			IEnumerable<SegmentIntersection> orderedIntersections =
				OrderAlongSourceSegments(linearSelfIntersections);

			IList<Linestring> segmentIntersectingLines =
				in3D
					? CollectIntersectionPaths3D(orderedIntersections, segmentList, segmentList,
					                             tolerance)
					: CollectIntersectionPaths(orderedIntersections, segmentList);

			return segmentIntersectingLines;
		}

		public static bool HasDuplicateSegment([NotNull] Linestring linestring,
		                                       int segmentIndex,
		                                       double tolerance,
		                                       IList<SegmentIntersection> duplicates = null)
		{
			// This (only) finds exact duplicates. In order to find non-equal covering segments
			// the ring must be cracked first.
			Line3D currentSegment = linestring[segmentIndex];

			var linearSelfIntersections = new List<SegmentIntersection>(
				SegmentIntersectionUtils.GetRelevantSelfIntersectionsXY(
					                        segmentIndex, currentSegment, linestring, tolerance)
				                        .Where(si => si.HasLinearIntersection));

			var candidates = linearSelfIntersections.Where(
				linearSelfIntersection => segmentIndex == linearSelfIntersection.SourceIndex);

			bool result = false;
			foreach (SegmentIntersection candidate in candidates)
			{
				Line3D candidateSegment = linestring[candidate.TargetIndex];

				if (currentSegment.IsCoincident3D(candidateSegment, tolerance))
				{
					duplicates?.Add(candidate);

					result = true;
				}
			}

			return result;
		}

		private static IList<Linestring> CollectIntersectionPaths3D(
			[NotNull] IEnumerable<SegmentIntersection> intersections,
			[NotNull] ISegmentList segmentList1,
			[NotNull] ISegmentList segmentList2,
			double tolerance)
		{
			return CollectIntersectionPaths(
				intersections.Select(i => IntersectLines3D(segmentList1[i.SourceIndex],
				                                           segmentList2[i.TargetIndex], tolerance))
				             .Where(l => l != null));
		}

		[NotNull]
		private static IList<Linestring> CollectIntersectionPaths(
			[NotNull] IEnumerable<SegmentIntersection> intersections,
			[NotNull] ISegmentList segmentList1)
		{
			return CollectIntersectionPaths(
				intersections.Select(i => i.TryGetIntersectionLine(segmentList1))
				             .Where(l => l != null));
		}

		[NotNull]
		public static IList<Linestring> CollectIntersectionPaths(
			[NotNull] IEnumerable<Line3D> intersectionLines)
		{
			var result = new List<Linestring>();

			var nextResultPath = new List<Line3D>();

			var reversed = false;
			foreach (Line3D segment in intersectionLines)
			{
				if (nextResultPath.Count == 0 ||
				    nextResultPath[nextResultPath.Count - 1].EndPoint.Equals(segment.StartPoint))
				{
					Assert.False(reversed, "Unexpected segment orientation. Zigzag?");
					nextResultPath.Add(segment);
				}
				else if (nextResultPath[nextResultPath.Count - 1]
				         .StartPoint.Equals(segment.EndPoint))
				{
					reversed = true;
					nextResultPath.Add(segment);
				}
				else
				{
					if (reversed)
					{
						nextResultPath.Reverse();
					}

					result.Add(new Linestring(nextResultPath));
					nextResultPath = new List<Line3D> {segment};
					reversed = false;
				}
			}

			if (nextResultPath.Count > 0)
			{
				if (reversed)
				{
					nextResultPath.Reverse();
				}

				result.Add(new Linestring(nextResultPath));
			}

			return result;
		}

		/// <summary>
		/// Deletes segments that are considered having a linear intersection w.r.t the tolerance.
		/// These are typically spikes or narrow straits in rings, such as these:
		/// ---------------------------        -------*          ---------
		/// |      *-----------       |        |      *          |       |
		/// |      |          |       |   ->   |      |          |       |
		/// |      |          |       |        |      |          |       |
		/// |______|          |_______|        |______|          |_______|
		/// The minimum segment length, if provided, determines whether a vertex will be
		/// kept on both sides of the strait (as depicted in the left result part) if the
		/// minimum segment length is not violated. The advantage of extra vertices is that
		/// the existing lines are kept in place which might be desired, especially if the
		/// tolerance is large.
		/// </summary>
		/// <param name="ring"></param>
		/// <param name="tolerance"></param>
		/// <param name="results"></param>
		/// <param name="minimumSegmentLength"></param>
		/// <returns></returns>
		public static bool TryDeleteLinearSelfIntersectionsXY(
			[NotNull] Linestring ring,
			double tolerance,
			[NotNull] List<Linestring> results,
			double? minimumSegmentLength = null)
		{
			// Basic idea:
			// 1. Self-cracking with (potentially large) tolerance
			// 2. Detect duplicate segments (linear self intersections with small tolerance)
			// 3. Build new geometry with remaining segments

			Linestring crackedSelfIntersections;

			if (! TryCrackLinearSelfIntersections(ring, tolerance, minimumSegmentLength,
			                                      out crackedSelfIntersections))
			{
				return false;
			}

			// Now keep only the segments that do not have self-intersections (with a minimum tolerance)
			double epsilon =
				MathUtils.GetDoubleSignificanceEpsilon(ring.XMax, ring.YMax);

			List<Line3D> allSegments = new List<Line3D>();
			for (int i = 0; i < crackedSelfIntersections.SegmentCount; i++)
			{
				// Skip exact duplicate segments that have opposite direction (they neutralize each other)
				List<SegmentIntersection> duplicates = new List<SegmentIntersection>();
				if (HasDuplicateSegment(crackedSelfIntersections, i, epsilon, duplicates))
				{
					if (duplicates.Count == 1)
					{
						continue;
					}

					// Attention: triplets (zig-zag lines): filter 2 but not all. Add one of the
					// 2 lines with the same direction
					if (! duplicates.Any(d => ! d.LinearIntersectionInOppositeDirection &&
					                          d.SourceIndex < d.TargetIndex))
					{
						continue;
					}
				}

				Line3D currentSegment = crackedSelfIntersections[i];

				if (MathUtils.AreEqual(0, currentSegment.Length3D))
				{
					continue;
				}

				allSegments.Add(currentSegment);
			}

			results.AddRange(GroupSegmentsIntoLinestrings(allSegments));

			return allSegments.Count < crackedSelfIntersections.SegmentCount;
		}

		#endregion

		#endregion

		#region Simplify

		/// <summary>
		/// Performs basic clustering on the specified points using the provided tolerances as
		/// minimum distance. M values and PointIDs are disregarded and lost in the output!
		/// </summary>
		/// <param name="multipoint"></param>
		/// <param name="xyTolerance">The xy tolerance</param>
		/// <param name="zTolerance">The z tolerance or NaN, if no vertical clustering should be
		/// performed.</param>
		public static void Simplify<T>([NotNull] Multipoint<T> multipoint,
		                               double xyTolerance,
		                               double zTolerance = double.NaN) where T : IPnt
		{
			// NOTE: AO-Simplify on the multipoint cannot be used because it uses the resolution instead
			// of the tolerance:
			// "For multipoints, Simplify snaps all x-, y-, z-, and m-coordinates to the grid of the associated 
			// spatial reference, and removes identical points. A point is identical to another point when the 
			// two have identical x,y coordinates (after snapping) and when attributes for which it is aware are 
			// identical to the attributes for which the other point is aware."

			if (multipoint.PointCount == 0)
			{
				return;
			}

			// First cluster the XY coordinates for all Z values, then cluster the Z values:
			List<T> coords = multipoint.GetPoints().ToList();

			IList<KeyValuePair<IPnt, List<T>>> clusters3D =
				Cluster(coords, pnt => pnt, xyTolerance, zTolerance);

			IEnumerable<T> newPoints = clusters3D.Select(c => (T) c.Key);

			ReplacePoints(multipoint, newPoints);
		}

		/// <summary>
		/// Performs basic clustering on the specified points using the provided tolerances as
		/// minimum distance. The clustering is performed separately and independently in xy and
		/// in z.
		/// </summary>
		/// <param name="items"></param>
		/// <param name="getPoint">The function that gets the item's point to be used for
		/// clustering.</param>
		/// <param name="xyTolerance">The xy clustering tolerance</param>
		/// <param name="zTolerance">The z tolerance or NaN, if no vertical clustering should be
		/// performed.</param>
		public static IList<KeyValuePair<IPnt, List<T>>> Cluster<T>(
			[NotNull] List<T> items,
			[NotNull] Func<T, IPnt> getPoint,
			double xyTolerance,
			double zTolerance = double.NaN)
		{
			var result = new List<KeyValuePair<IPnt, List<T>>>();

			if (items.Count == 0)
			{
				return result;
			}

			// First cluster the XY coordinates for all Z values, then cluster the Z values:

			IList<KeyValuePair<IPnt, List<T>>> clusters2D =
				Group(items, getPoint, xyTolerance, double.NaN);

			if (clusters2D.Count == items.Count)
			{
				// No actual clustering has taken place
				return clusters2D;
			}

			// Consider maintaining the awareness on the high-level geometry:
			if (double.IsNaN(zTolerance) || ! (getPoint(items[0]) is Pnt3D))
			{
				// No clustering by Zs
				return clusters2D;
			}

			var xySnapped = new List<KeyValuePair<IPnt, T>>();
			foreach (KeyValuePair<IPnt, List<T>> cluster2D in clusters2D)
			{
				// Include the various Z-levels for the xy-snapped-groups
				IPnt xyCenter = cluster2D.Key;

				foreach (T itemInCluster2D in cluster2D.Value)
				{
					Pnt3D pntZ = (Pnt3D) getPoint(itemInCluster2D);

					xySnapped.Add(
						new KeyValuePair<IPnt, T>(new Pnt3D(xyCenter.X, xyCenter.Y, pntZ.Z),
						                          itemInCluster2D));
				}
			}

			foreach (var cluster3D in Group(xySnapped, pair => pair.Key, xyTolerance,
			                                zTolerance))
			{
				List<T> clusterItems = cluster3D.Value.Select(kvp => kvp.Value).ToList();

				result.Add(new KeyValuePair<IPnt, List<T>>(cluster3D.Key, clusterItems));
			}

			return result;
		}

		[NotNull]
		private static IList<KeyValuePair<IPnt, List<T>>> Group<T>(
			[NotNull] List<T> items,
			Func<T, IPnt> getPoint,
			double xyTolerance,
			double zTolerance)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			IComparer<IPnt> comparer = new PntComparer<IPnt>(! double.IsNaN(zTolerance));

			items.Sort((item1, item2) => comparer.Compare(getPoint(item1), getPoint(item2)));

			var finishedGroups = new List<List<T>>();
			var activeGroups = new List<List<T>>();

			var currentGroup = new List<T> {items[0]};
			activeGroups.Add(currentGroup);

			// line sweeping while maintaining groups within the xy tolerance band:
			for (var i = 1; i < items.Count; i++)
			{
				IPnt currentPoint = getPoint(items[i]);

				bool assigned = false;
				// if the right-most (i.e. last) item of an active group has dX > xyTolerance,
				// it cannot grow any more -> emit to finished groups
				foreach (List<T> activeGroup in activeGroups.ToList())
				{
					T rightMostItem = activeGroup[activeGroup.Count - 1];
					IPnt rightMostPoint = getPoint(rightMostItem);

					double deltaX = currentPoint.X - rightMostPoint.X;

					if (deltaX > xyTolerance)
					{
						finishedGroups.Add(activeGroup);
						activeGroups.Remove(activeGroup);
					}
					else if (GeomRelationUtils.AreEqual(currentPoint, rightMostPoint,
					                                    xyTolerance, zTolerance))
					{
						// add the point (theoretically comparison should be done with the group's center)
						// and all other groups should be considered, but our tolerance is typically small...
						activeGroup.Add(items[i]);
						assigned = true;
						break;
					}
				}

				if (! assigned)
				{
					var newGroup = new List<T> {items[i]};
					activeGroups.Add(newGroup);
				}
			}

			finishedGroups.AddRange(activeGroups);

			var result = new List<KeyValuePair<IPnt, List<T>>>();

			// For strict interpretation of tolerance: Consider splitting clusters that are too large (Divisive Hierarchical clustering)
			foreach (List<T> groupedPoints in finishedGroups)
			{
				if (groupedPoints.Count == 1)
				{
					IPnt singleGroupPoint = getPoint(groupedPoints[0]);
					result.Add(new KeyValuePair<IPnt, List<T>>(singleGroupPoint, groupedPoints));
				}
				else
				{
					IPnt center = GetCenter(groupedPoints, getPoint);

					result.Add(new KeyValuePair<IPnt, List<T>>(center, groupedPoints));
				}
			}

			return result;
		}

		private static void ReplacePoints<T>([NotNull] Multipoint<T> multipoint,
		                                     [NotNull] IEnumerable<T> newPoints) where T : IPnt
		{
			multipoint.SetEmpty();

			foreach (T newPoint in newPoints)
			{
				multipoint.AddPoint(newPoint);
			}
		}

		private static IPnt GetCenter<T>([NotNull] IList<T> items,
		                                 [NotNull] Func<T, IPnt> getPoint)
		{
			var points = items.Select(i => getPoint(i)).ToList();

			var centerX = points.Average(p => p.X);
			var centerY = points.Average(p => p.Y);

			double centerZ = 0;
			foreach (IPnt point in points)
			{
				if (point is Pnt3D pnt3D)
				{
					centerZ += pnt3D.Z;
				}
				else
				{
					centerZ = double.NaN;
				}
			}

			centerZ /= points.Count;

			return double.IsNaN(centerZ)
				       ? (IPnt) new Pnt2D(centerX, centerY)
				       : new Pnt3D(centerX, centerY, centerZ);
		}

		#endregion

		#region Self intersections

		private static bool TryCrackLinearSelfIntersections(
			[NotNull] Linestring ring,
			double tolerance,
			double? minimumSegmentLength,
			out Linestring result)
		{
			result = null;

			IList<IntersectionPoint3D> intersectionPoints =
				GetSelfIntersectionPoints(ring, tolerance);

			var allCrackPoints = new List<CrackPoint>();
			foreach (var intersectionPoint in intersectionPoints.OrderBy(
				ip => ip.VirtualSourceVertex))
			{
				AddLinearIntersectionCrackPoints(intersectionPoint, ring, allCrackPoints,
				                                 tolerance);
			}

			if (allCrackPoints.Count == 0)
			{
				return false;
			}

			double? minSegmentLengthSquared = minimumSegmentLength * minimumSegmentLength;

			result = CrackLinestring(ring, allCrackPoints, minSegmentLengthSquared);

			return true;
		}

		private static void AddLinearIntersectionCrackPoints(IntersectionPoint3D intersectionPoint,
		                                                     Linestring linestring,
		                                                     List<CrackPoint> allCrackPoints,
		                                                     double tolerance)
		{
			var linearIntersection = intersectionPoint.SegmentIntersection;

			Line3D sourceSegment = linestring[linearIntersection.SourceIndex];
			Line3D targetSegment = linestring[linearIntersection.TargetIndex];

			CrackPoint result;
			if (linearIntersection.SourceStartIntersects &&
			    intersectionPoint.Point.Equals(sourceSegment.StartPoint))
			{
				double targetStartFactor;
				Pnt3D snapTarget =
					linearIntersection.GetLinearIntersectionStartOnTarget(
						targetSegment, out targetStartFactor);

				result = new CrackPoint(intersectionPoint, Assert.NotNull(snapTarget))
				         {
					         SnapVertexIndex = linearIntersection.SourceIndex,
					         SegmentSplitFactor =
						         TryGetSplitFactor(linearIntersection.TargetIndex,
						                           targetStartFactor)
				         };

				TryAddCrackPoint(result, allCrackPoints, tolerance);
			}

			if (linearIntersection.SourceEndIntersects &&
			    intersectionPoint.Point.Equals(sourceSegment.EndPoint))
			{
				double targetEndFactor;
				Pnt3D snapTarget = linearIntersection.GetLinearIntersectionEndOnTarget(
					targetSegment, out targetEndFactor);

				int nextSourceVertex =
					linestring.IsClosed
						? linestring.NextIndexInRing(linearIntersection.SourceIndex)
						: Assert.NotNull(linestring.NextVertexIndex(linearIntersection.SourceIndex))
						        .Value;

				result = new CrackPoint(intersectionPoint, Assert.NotNull(snapTarget))
				         {
					         SnapVertexIndex = nextSourceVertex,
					         SegmentSplitFactor =
						         TryGetSplitFactor(linearIntersection.TargetIndex, targetEndFactor)
				         };

				TryAddCrackPoint(result, allCrackPoints, tolerance);
			}
		}

		private static void TryAddCrackPoint(CrackPoint crackPoint,
		                                     List<CrackPoint> toCrackPointsList,
		                                     double tolerance)
		{
			var existing = FindBySnapVertex(toCrackPointsList, crackPoint.SnapVertexIndex);

			// TODO: improve de-duplication / aggregation of split points (or avoid dictionaries while cracking)
			if (existing != null)
			{
				// Duplicate for vertex snapping, check split for other (zig-zag) segment:
				if (crackPoint.SegmentSplitFactor == null)
				{
					// no additional information
					return;
				}

				if (existing.SegmentSplitFactor == null ||
				    MathUtils.AreEqual(existing.SegmentSplitFactor.Value,
				                       crackPoint.SegmentSplitFactor.Value))
				{
					existing.SegmentSplitFactor = crackPoint.SegmentSplitFactor;
					return;
				}

				if (crackPoint.SegmentSplitFactor != null)
				{
					// Add the point, but it must not have duplicate snap vertex indexes
					crackPoint.SnapVertexIndex = null;
				}
			}
			else
			{
				existing = FindBySplitFactor(toCrackPointsList, crackPoint.SegmentSplitFactor);

				if (existing != null)
				{
					Assert.True(crackPoint.TargetPoint.Equals(existing.TargetPoint),
					            "Divergent snap target point.");

					if (crackPoint.SnapVertexIndex == null)
					{
						return;
					}

					if (existing.SnapVertexIndex == null ||
					    existing.SnapVertexIndex == crackPoint.SnapVertexIndex)
					{
						existing.SnapVertexIndex = crackPoint.SnapVertexIndex;
						return;
					}

					if (crackPoint.SnapVertexIndex != null)
					{
						// Add the point, but there must not be duplicate split factors
						crackPoint.SegmentSplitFactor = null;
					}
				}
			}

			if (existing == null)
			{
				existing = toCrackPointsList.Find(
					cp => cp.TargetPoint.EqualsXY(crackPoint.IntersectionPoint.Point, tolerance));
			}

			if (existing != null)
			{
				// There is an existing crack point that could be snapped to
				// -> possibly the actual source point which was snapped to from another point
				crackPoint.TargetPoint = existing.TargetPoint;
			}

			toCrackPointsList.Add(crackPoint);
		}

		private static CrackPoint FindBySnapVertex(List<CrackPoint> toCrackPointsList,
		                                           int? snapVertex)
		{
			if (snapVertex == null)
			{
				return null;
			}

			return toCrackPointsList.Find(cp => cp.SnapVertexIndex == snapVertex);
		}

		private static CrackPoint FindBySplitFactor(List<CrackPoint> toCrackPointsList,
		                                            double? splitFactor)
		{
			if (splitFactor == null)
			{
				return null;
			}

			return toCrackPointsList.Find(
				cp => cp.SegmentSplitFactor != null &&
				      MathUtils.AreEqual(cp.SegmentSplitFactor.Value, splitFactor.Value));
		}

		private static Linestring CrackLinestring(Linestring linestring,
		                                          List<CrackPoint> crackPoints,
		                                          double? minSegmentLengthSquared)
		{
			List<Pnt3D> newPoints = new List<Pnt3D>();

			IDictionary<double, CrackPoint> splitPointBySplitLocation =
				crackPoints.Where(cp => cp.SegmentSplitFactor != null)
				           .ToDictionary(cp => cp.SegmentSplitFactor.Value);

			List<double> orderedSplitLocationsAlong =
				splitPointBySplitLocation.Keys.OrderBy(l => l).ToList();

			IDictionary<int, CrackPoint> snapPointsByVertex =
				crackPoints.Where(cp => cp.SnapVertexIndex != null)
				           .ToDictionary(cp => cp.SnapVertexIndex.Value);

			for (int currentIdx = 0; currentIdx < linestring.PointCount; currentIdx++)
			{
				// Insert the next split points between the previous and the current vertex:
				while (orderedSplitLocationsAlong.Count > 0 &&
				       currentIdx > orderedSplitLocationsAlong[0])
				{
					var splitSegmentFactor = orderedSplitLocationsAlong[0];
					orderedSplitLocationsAlong.RemoveAt(0);

					newPoints.Add(splitPointBySplitLocation[splitSegmentFactor].TargetPoint);
				}

				// Last point in ring: same as first
				if (currentIdx == linestring.PointCount - 1 &&
				    linestring.IsClosed && newPoints.Count > 0)
				{
					newPoints.Add(newPoints[0]);
					continue;
				}

				// Snap the current vertex
				if (snapPointsByVertex.ContainsKey(currentIdx))
				{
					CrackPoint crackPoint = snapPointsByVertex[currentIdx];

					Pnt3D snapPoint = crackPoint.TargetPoint;

					// Special logic for original point: to avoid changing the basic shape a small
					// minimum segment length can be specified in order to cut of the spike with an
					// orthogonal cut
					Pnt3D origPoint = linestring.GetPoint3D(currentIdx, true);

					if (crackPoint.IntersectionPoint.Type ==
					    IntersectionPointType.LinearIntersectionStart &&
					    origPoint.Dist2(snapPoint, 2) > minSegmentLengthSquared &&
					    ! IsAdjacentPoint(linestring, currentIdx, snapPoint))
					{
						newPoints.Add(origPoint);
					}

					newPoints.Add(snapPoint);

					if (crackPoint.IntersectionPoint.Type ==
					    IntersectionPointType.LinearIntersectionEnd &&
					    origPoint.Dist2(snapPoint, 2) > minSegmentLengthSquared)
					{
						newPoints.Add(origPoint);
					}
				}
				else
				{
					newPoints.Add(linestring.GetPoint3D(currentIdx, true));
				}
			}

			var result = new Linestring(newPoints);

			return result;
		}

		private static bool IsAdjacentPoint([NotNull] Linestring inLinestring,
		                                    int currentVertexIdx,
		                                    [NotNull] Pnt3D testPoint)
		{
			int? nextIdx = inLinestring.NextVertexIndex(currentVertexIdx);

			if (nextIdx != null && inLinestring.GetPoint3D(nextIdx.Value).Equals(testPoint))
			{
				return true;
			}

			int? previousIdx = inLinestring.PreviousVertexIndex(currentVertexIdx);

			if (previousIdx != null && inLinestring.GetPoint3D(previousIdx.Value).Equals(testPoint))
			{
				return true;
			}

			return false;
		}

		private static IList<Linestring> GroupSegmentsIntoLinestrings(List<Line3D> allSegments)
		{
			// Currently, the orientation is not corrected!

			List<IList<Line3D>> linestrings = new List<IList<Line3D>>();

			foreach (Line3D segment in allSegments)
			{
				AppendSegment(segment, linestrings);

				// TODO: For the general case, another round (first point matches last point) would be needed
			}

			return linestrings.Select(l => new Linestring(l)).ToList();
		}

		private static void AppendSegment(Line3D segment, List<IList<Line3D>> toLinestrings)
		{
			foreach (IList<Line3D> result in toLinestrings)
			{
				Pnt3D endPoint = result[result.Count - 1].EndPoint;

				if (endPoint.Equals(segment.StartPoint))
				{
					result.Add(segment);
					return;
				}
			}

			// Segment cannot be directly appended
			toLinestrings.Add(new List<Line3D> {segment});
		}

		private static double? TryGetSplitFactor(int segmentIndex, double alongSegmentFactor)
		{
			if (double.IsNaN(alongSegmentFactor))
			{
				return null;
			}

			if (alongSegmentFactor > 0 && alongSegmentFactor < 1)
			{
				double virtualSplitIndex = segmentIndex + alongSegmentFactor;

				return virtualSplitIndex;
			}

			return null;
		}

		#endregion

		[NotNull]
		private static IList<Linestring> TryProcessAll(
			Func<Linestring, Linestring, double, IList<Linestring>> func,
			[NotNull] Linestring sourceRing,
			[NotNull] IList<Linestring> targetLines,
			double tolerance,
			HashSet<Linestring> usedTargets)
		{
			var input = new List<Linestring> {sourceRing};

			var result = new List<Linestring>();

			// TODO: while-loop that re-tries un-used targets after each removal
			foreach (Linestring target in targetLines)
			{
				result.Clear();

				foreach (Linestring inputRing in input)
				{
					IList<Linestring> cutRings =
						func(inputRing, target, tolerance);

					if (cutRings.Count > 0)
					{
						result.AddRange(cutRings);
						usedTargets.Add(target);
					}
					else
					{
						result.Add(inputRing);
					}
				}

				input = result.ToList();
			}

			return result;
		}

		private static bool HasVerticalPoints(
			IEnumerable<Pnt3D> orderedPoints,
			double tolerance,
			out List<List<Pnt3D>> xyClusters)
		{
			bool result = false;

			xyClusters = new List<List<Pnt3D>>();
			Pnt3D previous = null;
			List<Pnt3D> currentCluster = null;
			foreach (Pnt3D point in orderedPoints)
			{
				if (previous == null ||
				    ! previous.EqualsXY(point, tolerance))
				{
					currentCluster = new List<Pnt3D> {point};
					xyClusters.Add(currentCluster);
				}
				else
				{
					//  (previous.EqualsXY(point, tolerance))
					if (! MathUtils.AreEqual(point.Z, previous.Z, tolerance))
					{
						result = true;
					}

					currentCluster.Add(point);
				}

				previous = point;
			}

			return result;
		}

		private static ISegmentList RotateSegments([NotNull] ISegmentList segmentList,
		                                           RotationAxis rotationAxis)
		{
			if (segmentList is Linestring linestring)
			{
				return Rotate(linestring, rotationAxis);
			}
			else if (segmentList is RingGroup ringGroup)
			{
				return RotateRingGroup(ringGroup, rotationAxis);
			}

			throw new NotImplementedException("Rotation for geometry type not yet supported");
		}

		private static RingGroup RotateRingGroup(RingGroup ringGroup,
		                                         RotationAxis rotationAxis)
		{
			RingGroup rotateRingGroup =
				new RingGroup(Rotate(ringGroup.ExteriorRing, rotationAxis),
				              Rotate(ringGroup.InteriorRings, rotationAxis))
				{
					Id = ringGroup.Id
				};

			return rotateRingGroup;
		}

		private static IEnumerable<RingGroup> CutVerticalRingGroup(
			[NotNull] RingGroup source,
			[NotNull] ISegmentList matchingCutLines,
			double tolerance)
		{
			RotationAxis verticalCutRotation =
				GetPreferredRotationAxis(source);

			IList<MultiLinestring> cutResult =
				CutVerticalRingGroup(source, matchingCutLines, tolerance, verticalCutRotation)
					.ToList();

			foreach (RingGroup ringGroup in cutResult.SelectMany(
				r => GetConnectedComponents(r, tolerance)))
			{
				yield return ringGroup;
			}
		}

		public static IEnumerable<RingGroup> GetConnectedComponents(MultiLinestring rings,
			double tolerance)
		{
			RingGroup singleResult = rings as RingGroup;

			if (singleResult != null)
			{
				return new List<RingGroup> {singleResult};
			}

			var result = new List<RingGroup>();

			foreach (Linestring ring in rings.GetLinestrings())
			{
				Assert.True(ring.IsClosed, "Unclosed ring.");
				Assert.NotNull(ring.ClockwiseOriented, "Undefined orientation");

				if (ring.ClockwiseOriented == true)
				{
					result.Add(new RingGroup(ring));
				}
			}

			foreach (Linestring ring in rings.GetLinestrings()
			                                 .Where(r => r.ClockwiseOriented == false))
			{
				foreach (RingGroup ringGroup in result)
				{
					if (GeomRelationUtils.PolycurveContainsXY(
						ringGroup, ring.StartPoint, tolerance))
					{
						ringGroup.AddInteriorRing(ring);
					}
				}
			}

			return result;
		}

		private static IEnumerable<MultiLinestring> CutVerticalRingGroup(
			[NotNull] RingGroup source,
			[NotNull] ISegmentList matchingCutLines,
			double tolerance,
			RotationAxis rotationAxis)
		{
			var sourceToCutXY = RotateRingGroup(source, rotationAxis);

			MultiPolycurve cutLinesXY = new MultiPolycurve(
				Rotate(GeomUtils.GetLinestrings(matchingCutLines), rotationAxis));

			bool reverseOrientation = sourceToCutXY.ClockwiseOriented == false;

			if (reverseOrientation)
			{
				sourceToCutXY.ReverseOrientation();

				if (cutLinesXY.IsClosed)
				{
					// keep relative orientation
					cutLinesXY.ReverseOrientation();
				}
			}

			var resultXY = CutXY(sourceToCutXY, cutLinesXY, tolerance);

			foreach (MultiLinestring resultRings in resultXY)
			{
				foreach (RingGroup resultGroup in GetConnectedComponents(resultRings, tolerance))
				{
					yield return RotateRingGroupBack(resultGroup, rotationAxis, reverseOrientation);
				}
			}
		}

		private static RingGroup RotateRingGroupBack(RingGroup ringGroup,
		                                             RotationAxis rotationAxis,
		                                             bool reverseOrientation)
		{
			var result = new RingGroup(
				RotateBack(ringGroup.ExteriorRing, rotationAxis),
				RotateBack(ringGroup.InteriorRings, rotationAxis,
				           reverseOrientation));

			if (reverseOrientation)
			{
				result.ReverseOrientation();
			}

			return result;
		}

		private static IList<Linestring> UnionInteriorRings(
			List<Linestring> interiorRings, double tolerance)
		{
			List<Linestring> other = interiorRings.GetRange(1, interiorRings.Count - 1);

			HashSet<Linestring> usedInMerge = new HashSet<Linestring>();
			IList<Linestring> merged = TryProcessAll(RemoveOverlapXY, interiorRings[0],
			                                         other, tolerance, usedInMerge);

			return merged.Concat(other.Where(o => ! usedInMerge.Contains(o))).ToList();
		}

		private static RingGroup CreateRingGroup(
			[NotNull] Linestring mainRing,
			[NotNull] IEnumerable<Linestring> nonBoundaryCrossingPotentialIslands,
			double tolerance)
		{
			var resultRingGroup = new RingGroup(mainRing);

			foreach (Linestring disjointOrWithin in nonBoundaryCrossingPotentialIslands)
			{
				bool contained =
					GeomRelationUtils.AreaContainsXY(mainRing, disjointOrWithin.StartPoint,
					                                 tolerance, true) == true;
				if (contained)
				{
					// TODO: Decide on copy policy. Currently the cut segments are not cloned 
					resultRingGroup.AddInteriorRing(disjointOrWithin);
				}
			}

			return resultRingGroup;
		}

		private static IEnumerable<Pnt3D> GetConnectedPoints(
			[NotNull] IList<Linestring> connectedLinestrings,
			[CanBeNull] Pnt3D startPoint,
			double tolerance)
		{
			int startCurveIdx = -1;
			int startPointIdx = -1;

			if (startPoint != null)
			{
				for (var thisCurveIdx = 0;
				     thisCurveIdx < connectedLinestrings.Count;
				     thisCurveIdx++)
				{
					Linestring thisCurve = connectedLinestrings[thisCurveIdx];

					int? startIdxThisCurve =
						thisCurve.FindPointIdx(startPoint, inXY: true);

					if (startIdxThisCurve != null)
					{
						startCurveIdx = thisCurveIdx;

						startPointIdx = startIdxThisCurve.Value;
						break;
					}
				}

				Assert.True(startPointIdx >= 0,
				            "startPoint not found in provided linestrings.");
			}
			else
			{
				startCurveIdx = 0;
				startPointIdx = 0;
			}

			Pnt3D lastPoint = null;
			for (int i = startCurveIdx;
			     i < connectedLinestrings.Count + startCurveIdx;
			     i++)
			{
				int currentIdx = i % connectedLinestrings.Count;

				Linestring current = connectedLinestrings[currentIdx];

				IEnumerable<Pnt3D> vertices =
					currentIdx == startCurveIdx
						? current.GetPoints(startPointIdx)
						: current.GetPoints();

				foreach (Pnt3D point in Append(vertices, lastPoint, tolerance))
				{
					yield return point;

					lastPoint = point;
				}
			}

			if (startPointIdx > 0)
			{
				// add remaining points 
				Linestring current = connectedLinestrings[startCurveIdx];

				foreach (
					Pnt3D pnt in Append(current.GetPoints(0, startPointIdx + 1), lastPoint,
					                    tolerance))
				{
					yield return pnt;
				}
			}
		}

		private static IEnumerable<Pnt3D> Append(IEnumerable<Pnt3D> vertices,
		                                         Pnt3D previousEnd,
		                                         double tolerance)
		{
			Pnt3D lastPointInThisLinestring = null;

			foreach (Pnt3D point in vertices)
			{
				if (previousEnd != null && lastPointInThisLinestring == null)
				{
					// first point in the new linestring - must connect, but avoid duplicates:
					Assert.True(point.EqualsXY(previousEnd, tolerance),
					            "Provided linestrings are not connected within tolerance {0}",
					            tolerance);
				}
				else
				{
					yield return point;
				}

				lastPointInThisLinestring = point;
			}
		}

		private static IEnumerable<Line3D> GetZOnlyDifferenceLines(
			[NotNull] Linestring linestring1,
			[NotNull] MultiLinestring multiLinestring2,
			double tolerance)
		{
			if (! linestring1.ExtentsIntersectXY(
				    multiLinestring2.XMin, multiLinestring2.YMin,
				    multiLinestring2.XMax, multiLinestring2.YMax,
				    tolerance))
			{
				yield break;
			}

			IEnumerable<SegmentIntersection> intersections =
				SegmentIntersectionUtils.GetSegmentIntersectionsXY(
					linestring1, multiLinestring2, tolerance, true);

			double tolSquared = tolerance * tolerance;

			foreach (SegmentIntersection intersection in intersections)
			{
				Line3D intersectionLine =
					intersection.TryGetIntersectionLine(linestring1);

				if (intersectionLine == null)
				{
					continue;
				}

				bool differentInZ =
					IsLinearIntersectionDifferentInZ(intersection, intersectionLine,
					                                 multiLinestring2, tolSquared);
				if (differentInZ)
				{
					yield return intersectionLine;
				}
			}
		}

		private static void AddSegmentsBetween(
			[NotNull] Linestring source,
			[CanBeNull] SegmentIntersection current,
			[CanBeNull] SegmentIntersection previous,
			[NotNull] List<Line3D> result)
		{
			int startIndex = previous?.SourceIndex ?? 0;
			int endIndex = current?.SourceIndex ?? source.SegmentCount;
			if (previous != null)
			{
				if (startIndex != endIndex &&
				    previous.GetLinearIntersectionEndFactor(true) < 1)
				{
					// add remaining uncovered part of previous source
					result.Add(
						new Line3D(
							source[previous.SourceIndex].GetPointAlong(
								previous.GetLinearIntersectionEndFactor(true),
								true),
							source[previous.SourceIndex].EndPoint));
				}

				startIndex++;
			}

			// add intermediate segments
			for (int i = startIndex; i < endIndex; i++)
			{
				result.Add(source[i]);
			}

			// add uncovered segment stretch of current source segment before current intersection starts
			if (current != null)
			{
				if (previous != null && previous.SourceIndex == current.SourceIndex)
				{
					// A non-covered gap within the current source segment
					if (current.GetLinearIntersectionStartFactor(true) >
					    previous.GetLinearIntersectionEndFactor(true))
					{
						Line3D sourceLine = source[previous.SourceIndex];
						result.Add(
							new Line3D(
								sourceLine.GetPointAlong(
									previous.GetLinearIntersectionEndFactor(true), true),
								sourceLine.GetPointAlong(
									current.GetLinearIntersectionStartFactor(true), true)));
					}
				}
				else if (current.GetLinearIntersectionStartFactor(true) > 0)
				{
					// The un-covered part is between from-point and the start factor
					Line3D sourceLine = source[current.SourceIndex];
					result.Add(
						new Line3D(
							sourceLine.StartPoint,
							sourceLine.GetPointAlong(
								current.GetLinearIntersectionStartFactor(true), true)));
				}
			}
		}

		private static IEnumerable<Line3D> GetDifferenceLinesXY(
			[NotNull] Linestring linestring1,
			[NotNull] MultiLinestring multiLinestring2,
			double xytolerance,
			[CanBeNull] IList<Line3D> zOnlyDifferences,
			double zTolerance)
		{
			if (! linestring1.ExtentsIntersectXY(
				    multiLinestring2.XMin, multiLinestring2.YMin,
				    multiLinestring2.XMax, multiLinestring2.YMax,
				    xytolerance))
			{
				return linestring1.Segments;
			}

			var result = new List<Line3D>();

			var intersections = SegmentIntersectionUtils.GetSegmentIntersectionsXY(
				linestring1, multiLinestring2, xytolerance, true);

			var orderedIntersections =
				OrderAlongSourceSegments(
					intersections.Where(i => i.HasLinearIntersection));

			double zTolerance2 = zTolerance * zTolerance;

			SegmentIntersection previousIntersection = null;
			foreach (SegmentIntersection intersection in orderedIntersections)
			{
				// Add non-intersecting source lines between the previous and this intersection:
				AddSegmentsBetween(linestring1, intersection, previousIntersection,
				                   result);

				previousIntersection = intersection;

				if (zOnlyDifferences == null)
				{
					continue;
				}

				var intersectionLine = intersection.TryGetIntersectionLine(linestring1);

				if (intersectionLine == null)
				{
					continue;
				}

				bool differentInZ =
					IsLinearIntersectionDifferentInZ(intersection, intersectionLine,
					                                 multiLinestring2, zTolerance2);
				if (differentInZ)
				{
					zOnlyDifferences.Add(intersectionLine);
				}
			}

			AddSegmentsBetween(linestring1, null, previousIntersection,
			                   result);

			return result;
		}

		private static void AddToList(IEnumerable<MultiLinestring> itemsToAdd,
		                              List<MultiLinestring> list,
		                              bool asMultipart)
		{
			var collectionToAdd = CollectionUtils.GetCollection(itemsToAdd);

			if (collectionToAdd.Count == 0)
			{
				return;
			}

			if (asMultipart && collectionToAdd.Count > 1)
			{
				list.Add(new MultiPolycurve(collectionToAdd));
			}
			else
			{
				list.AddRange(collectionToAdd);
			}
		}

		private static bool CutLineIsVertical(Linestring cutLine, double tolerance)
		{
			bool cutLineIsVertical =
				cutLine.GetLength2D() < tolerance &&
				! MathUtils.AreEqual(cutLine.StartPoint.Z, cutLine.EndPoint.Z, tolerance);
			return cutLineIsVertical;
		}
	}
}

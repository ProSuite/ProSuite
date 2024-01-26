using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public static class SegmentIntersectionUtils
	{
		/// <summary>
		/// Returns all self intersections except the intersection between consecutive segments.
		/// </summary>
		/// <param name="sourceLineGlobalIdx"></param>
		/// <param name="sourceLine"></param>
		/// <param name="containingSegmentList"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static IEnumerable<SegmentIntersection> GetRelevantSelfIntersectionsXY(
			int sourceLineGlobalIdx,
			[NotNull] Line3D sourceLine,
			[NotNull] ISegmentList containingSegmentList,
			double tolerance)
		{
			// a predicate, i.e. i != sourceGlobalIdx && i != sourceGlobalIdx - 1 && i != sourceGlobalIdx + 1
			Predicate<int> predicate = i => i != sourceLineGlobalIdx;

			IEnumerable<KeyValuePair<int, Line3D>> segmentsByGlobalIdx =
				containingSegmentList.FindSegments(sourceLine, tolerance, true,
				                                   predicate);

			foreach (SegmentIntersection intersection in
			         IntersectLineWithLinestringXY(
				         sourceLine, sourceLineGlobalIdx, segmentsByGlobalIdx, tolerance))
			{
				int? nextSegmentIndex =
					containingSegmentList.NextSegmentIndex(intersection.SourceIndex);

				if (nextSegmentIndex == intersection.TargetIndex &&
				    ! intersection.HasLinearIntersection)
				{
					continue;
				}

				int? previousSegmentIndex =
					containingSegmentList.PreviousSegmentIndex(intersection.SourceIndex);
				if (previousSegmentIndex == intersection.TargetIndex &&
				    ! intersection.HasLinearIntersection)
				{
					continue;
				}

				yield return intersection;
			}
		}

		/// <summary>
		/// Gets the segment intersections between the specified segment lists.
		/// </summary>
		/// <param name="segmentList1"></param>
		/// <param name="segmentList2"></param>
		/// <param name="tolerance"></param>
		/// <param name="optimizeLinearIntersections">If true, an optimization is employed that improves
		/// the performance when linear intersections are present. If a segmentList1 segment is equal
		/// to a linestring2 segment it will directly be used. Other intersections will be missed.
		/// </param>
		/// <returns>The segment intersections with the index of the cut part of multiLinestring2</returns>
		public static IEnumerable<SegmentIntersection> GetSegmentIntersectionsXY(
			[NotNull] ISegmentList segmentList1,
			[NotNull] ISegmentList segmentList2,
			double tolerance,
			bool optimizeLinearIntersections = false)
		{
			if (GeomRelationUtils.AreBoundsDisjoint(segmentList1, segmentList2, tolerance))
			{
				yield break;
			}

			SegmentIntersection previousLinearIntersection = null;

			var lineIdx = -1;
			foreach (Line3D line in segmentList1)
			{
				lineIdx++;

				if (GeomRelationUtils.AreBoundsDisjoint(segmentList2, line, tolerance))
				{
					continue;
				}

				foreach (SegmentIntersection result in GetSegmentIntersectionsXY(
					         lineIdx, line, segmentList2, tolerance,
					         previousLinearIntersection))
				{
					if (optimizeLinearIntersections && result.SegmentsAreEqualInXy)
					{
						previousLinearIntersection = result;
					}

					yield return result;
				}
			}
		}

		/// <summary>
		/// Gets the segment intersections between the specified segment list and the boundary of
		/// the specified envelope.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="envelopeBoundary"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static IEnumerable<SegmentIntersection> GetSegmentIntersectionsXY(
			[NotNull] ISegmentList sourceSegments,
			[NotNull] IBoundedXY envelopeBoundary,
			double tolerance)
		{
			if (GeomRelationUtils.AreBoundsDisjoint(sourceSegments, envelopeBoundary, tolerance))
			{
				yield break;
			}

			Linestring envelopeRing = GeomFactory.CreateRing(envelopeBoundary);

			if ((sourceSegments is MultiLinestring mls && mls.SpatialIndex != null) ||
			    (sourceSegments is Linestring ls && ls.SpatialIndex != null))
			{
				// Use the spatial index, i.e. the segmentList1 must be the target!
				for (var envelopeLineIdx = 0;
				     envelopeLineIdx < envelopeRing.SegmentCount;
				     envelopeLineIdx++)
				{
					Line3D envelopeLine = envelopeRing.GetSegment(envelopeLineIdx);

					if (GeomRelationUtils.AreBoundsDisjoint(envelopeLine, sourceSegments,
					                                        tolerance))
					{
						continue;
					}

					IEnumerable<KeyValuePair<int, Line3D>> segmentsByIndex =
						sourceSegments.FindSegments(envelopeLine, tolerance);

					foreach (KeyValuePair<int, Line3D> sourceSegment in
					         segmentsByIndex.OrderBy(kvp => kvp.Key))
					{
						int sourceIdx = sourceSegment.Key;
						Line3D sourceLine = sourceSegment.Value;

						SegmentIntersection intersection =
							SegmentIntersection.CalculateIntersectionXY(
								sourceIdx, envelopeLineIdx, sourceLine, envelopeLine, tolerance);

						if (intersection.HasIntersection)
						{
							yield return intersection;
						}
					}
				}
			}
			else
			{
				// No spatial index! We might as well loop through the segments just once:

				for (var lineIdx = 0; lineIdx < sourceSegments.SegmentCount; lineIdx++)
				{
					Line3D line = sourceSegments.GetSegment(lineIdx);

					if (GeomRelationUtils.AreBoundsDisjoint(line, envelopeBoundary, tolerance))
					{
						continue;
					}

					foreach (SegmentIntersection result in GetSegmentIntersectionsXY(
						         lineIdx, line, envelopeRing, tolerance, null))
					{
						yield return result;
					}
				}
			}
		}

		public static IEnumerable<SegmentIntersection> GetFilteredIntersectionsOrderedAlongSource(
			[NotNull] IEnumerable<SegmentIntersection> intersections,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target)
		{
			var intersectionFilter = new SegmentIntersectionFilter(source, target);

			return intersectionFilter.GetFilteredIntersectionsOrderedAlongSourceSegments(
				intersections);
		}

		/// <summary>
		/// Collects the intersection points from the sorted and filtered intersections.
		/// </summary>
		/// <param name="sortedRelevantIntersections"></param>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments"></param>
		/// <param name="tolerance"></param>
		/// <param name="includeLinearIntersectionIntermediatePoints">
		/// Includes all intermediate points along linear intersections.
		/// NOTE: They will not be ordered along the source segments when compared
		/// to other intersection point types.</param>
		/// <returns></returns>
		public static IList<IntersectionPoint3D> CollectIntersectionPoints(
			[NotNull] IEnumerable<SegmentIntersection> sortedRelevantIntersections,
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments,
			double tolerance,
			bool includeLinearIntersectionIntermediatePoints = false)
		{
			var result = new List<IntersectionPoint3D>();

			IntersectionPoint3D startPoint = null;

			IntersectionPoint3D previousLinearEnd = null;

			foreach (SegmentIntersection intersection in sortedRelevantIntersections)
			{
				if (! intersection.HasLinearIntersection)
				{
					// emit previous linear stretch (to maintain order along source)
					TryAddLinearIntersectionStretch(startPoint, previousLinearEnd, result);
					startPoint = null;
					previousLinearEnd = null;

					result.Add(
						IntersectionPoint3D.CreateSingleIntersectionPoint(
							intersection, sourceSegments, targetSegments,
							tolerance));
					continue;
				}

				IntersectionPoint3D fromPoint =
					IntersectionPoint3D.GetLinearIntersectionStart(
						intersection, sourceSegments, targetSegments);

				IntersectionPoint3D toPoint =
					IntersectionPoint3D.GetLinearIntersectionEnd(
						intersection, sourceSegments, targetSegments);

				if (previousLinearEnd == null)
				{
					startPoint = fromPoint;
				}

				if (previousLinearEnd == null ||
				    ContinueLinearIntersectionStretch(
					    previousLinearEnd, fromPoint, sourceSegments, targetSegments))
				{
					// first, or connected to previous -> continue:
					if (previousLinearEnd != null &&
					    includeLinearIntersectionIntermediatePoints)
					{
						previousLinearEnd.Type =
							IntersectionPointType.LinearIntersectionIntermediate;
						result.Add(previousLinearEnd);
					}

					previousLinearEnd = toPoint;
				}
				else
				{
					// emit
					TryAddLinearIntersectionStretch(startPoint, previousLinearEnd, result);

					// re-start with current from/to
					startPoint = fromPoint; // isTargetRingNullPoint ? fromPoint : null;
					previousLinearEnd = toPoint; // isTargetRingNullPoint ? toPoint : null;
				}
			}

			TryAddLinearIntersectionStretch(startPoint, previousLinearEnd, result);

			return result;
		}

		private static bool ContinueLinearIntersectionStretch(
			[NotNull] IntersectionPoint3D previousLinearEnd,
			[NotNull] IntersectionPoint3D currentLinearStart,
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments)
		{
			if (! AreIntersectionsAdjacent(previousLinearEnd, currentLinearStart, sourceSegments,
			                               targetSegments, out _, out _))
			{
				return false;
			}

			// Connected segments should be exactly equal
			if (! previousLinearEnd.Point.Equals(currentLinearStart.Point))
			{
				return false;
			}

			// For symmetry reasons, also the target ring start/end should break linear intersections.
			// Otherwise the result is different in XY depending on the order of the arguments.
			bool isTargetRingNullPoint = IsTargetRingNullPoint(
				currentLinearStart.SegmentIntersection,
				previousLinearEnd.SegmentIntersection,
				targetSegments);

			if (isTargetRingNullPoint)
			{
				return false;
			}

			// Connected segments should be exactly equal
			return previousLinearEnd.Point.Equals(currentLinearStart.Point);
		}

		public static bool AreIntersectionsAdjacent(
			[NotNull] IntersectionPoint3D previousLinearIntersectionEnd,
			[NotNull] IntersectionPoint3D currentLinearIntersectionStart,
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments,
			out bool isSourceBoundaryLoop,
			out bool isTargetBoundaryLoop,
			double tolerance = double.NaN)
		{
			isSourceBoundaryLoop = false;
			isTargetBoundaryLoop = false;

			if (previousLinearIntersectionEnd.SourcePartIndex !=
			    currentLinearIntersectionStart.SourcePartIndex)
			{
				return false;
			}

			if (previousLinearIntersectionEnd.TargetPartIndex !=
			    currentLinearIntersectionStart.TargetPartIndex)
			{
				return false;
			}

			Linestring sourcePart =
				sourceSegments.GetPart(previousLinearIntersectionEnd.SourcePartIndex);

			bool sameDistanceAlongSource =
				currentLinearIntersectionStart.ReferencesSameSourceVertex(
					previousLinearIntersectionEnd, sourceSegments, tolerance);

			if (! sameDistanceAlongSource)
			{
				// Exclude source boundary loops, but include very acute angle linear intersections
				// as in CanGetIntersectionAreaXYWithLinearIntersectionWithinToleranceAcuteAngle()
				if (currentLinearIntersectionStart.ReferencesSameTargetVertex(
					    previousLinearIntersectionEnd, targetSegments))
				{
					double segmentRatioDistance = GetVirtualVertexRatioDistance(
						previousLinearIntersectionEnd.VirtualSourceVertex,
						currentLinearIntersectionStart.VirtualSourceVertex,
						sourcePart.SegmentCount);

					// Typically it is very very small, but theoretically it could be almost the entire segments
					// if the angle is extremely acute.
					if (Math.Abs(segmentRatioDistance) < 2)
					{
						return true;
					}

					isSourceBoundaryLoop = true;
				}

				return false;
			}

			Linestring targetPart =
				targetSegments.GetPart(previousLinearIntersectionEnd.TargetPartIndex);

			double targetSegmentsBetween = TargetSegmentCountBetween(
				previousLinearIntersectionEnd, currentLinearIntersectionStart, targetPart);

			// Exclude target boundary loops: More than one segment (and probably we should also
			// make sure to call ! ReferencesSameTargetVertex which now checks for the distance > tolerance.
			if (targetSegmentsBetween > 1)
			{
				isTargetBoundaryLoop = true;
				return false;
			}

			// Connected lines must match exactly (they are typically reference-equal)
			return previousLinearIntersectionEnd.Point.Equals(currentLinearIntersectionStart.Point);
		}

		public static double GetVirtualVertexRatioDistance(
			double priorVirtualVertex, double subsequentVirtualVertex, int ringSegmentCount)
		{
			// TODO: Proper count of source segments between, probably deal with short segments
			// check if it's a real source boundary loop
			double segmentRatioDistance = subsequentVirtualVertex - priorVirtualVertex;

			// Sometimes (see CanGetIntersectionAreaWithLinearIntersectionWithinToleranceAcuteAngleTop5502)
			// The linear intersections starts just after the start point and ends just
			// after the last point. This happens with acute angles and the actual start
			// point is just outside the tolerance. For the time being, they shall be
			// considered adjacent anyway (but not boundary loops!)
			if (segmentRatioDistance < 0)
			{
				segmentRatioDistance =
					MathUtils.Modulo(segmentRatioDistance, ringSegmentCount, true);
			}

			return segmentRatioDistance;
		}

		/// <summary>
		/// Corrects the LinearIntersectionInOppositeDirection property for zero-length
		/// segments for either the previous or the current intersection point.
		/// </summary>
		/// <param name="previous"></param>
		/// <param name="current"></param>
		private static void EnsureLinearIntersectionDirection(
			[NotNull] IntersectionPoint3D previous,
			[NotNull] IntersectionPoint3D current)
		{
			// Not sure if correcting this property is really necessary.
			bool startIsZeroLength = previous.SegmentIntersection.IsSegmentZeroLength2D;
			bool endIsZeroLength = current.SegmentIntersection.IsSegmentZeroLength2D;

			if (endIsZeroLength && ! startIsZeroLength)
			{
				// current's LinearIntersectionInOppositeDirection is random, correct it
				current.LinearIntersectionInOppositeDirection =
					previous.LinearIntersectionInOppositeDirection;
			}
			else if (startIsZeroLength && ! endIsZeroLength)
			{
				previous.LinearIntersectionInOppositeDirection =
					current.LinearIntersectionInOppositeDirection;
			}

			if (startIsZeroLength && endIsZeroLength &&
			    ! MathUtils.AreEqual(previous.VirtualTargetVertex,
			                         current.VirtualTargetVertex))
			{
				// Both intersections have zero length segments. Fall back, if possible:
				bool oppositeDirection = previous.VirtualTargetVertex > current.VirtualTargetVertex;

				previous.LinearIntersectionInOppositeDirection = oppositeDirection;
				current.LinearIntersectionInOppositeDirection = oppositeDirection;
			}
		}

		public static int SourceSegmentCountBetween(
			[NotNull] ISegmentList source,
			[NotNull] IntersectionPoint3D firstIntersection,
			[NotNull] IntersectionPoint3D secondIntersection)
		{
			Assert.AreEqual(firstIntersection.SourcePartIndex, secondIntersection.SourcePartIndex,
			                "Intersections are not from the same part.");

			double result = secondIntersection.VirtualSourceVertex -
			                firstIntersection.VirtualSourceVertex;

			if (result < 0)
			{
				Linestring sourcePart = source.GetPart(firstIntersection.SourcePartIndex);
				result += sourcePart.SegmentCount;
			}

			return (int) Math.Floor(result);
		}

		public static int TargetSegmentCountBetween(
			[NotNull] ISegmentList target,
			[NotNull] IntersectionPoint3D firstIntersection,
			[NotNull] IntersectionPoint3D secondIntersection)
		{
			Assert.AreEqual(firstIntersection.TargetPartIndex, secondIntersection.TargetPartIndex,
			                "Intersections are not from the same part.");

			double result = secondIntersection.VirtualTargetVertex -
			                firstIntersection.VirtualTargetVertex;

			if (result < 0)
			{
				Linestring targetPart = target.GetPart(firstIntersection.TargetPartIndex);
				result += targetPart.SegmentCount;
			}

			return (int) Math.Floor(result);
		}

		private static double TargetSegmentCountBetween(
			[NotNull] IntersectionPoint3D firstIntersection,
			[NotNull] IntersectionPoint3D secondIntersection,
			[NotNull] Linestring targetPart)
		{
			Assert.AreEqual(firstIntersection.TargetPartIndex, secondIntersection.TargetPartIndex,
			                "Intersections are not from the same target part.");

			double forwardSegmentCount = secondIntersection.VirtualTargetVertex -
			                             firstIntersection.VirtualTargetVertex;

			if (MathUtils.AreEqual(forwardSegmentCount, 0))
			{
				return 0;
			}

			// Special case: last and first point or vice-versa
			bool firstIsExactlyOnTargetVertex =
				firstIntersection.IsTargetVertex(out int firstVertex);

			if (firstIsExactlyOnTargetVertex)
			{
				if (targetPart.IsLastPointInPart(firstVertex) &&
				    secondIntersection.VirtualTargetVertex == 0)
				{
					if (targetPart.IsClosed)
					{
						return 0;
					}
				}

				bool secondIsOnTargetVertex =
					secondIntersection.IsTargetVertex(out int secondVertex);

				if (secondIsOnTargetVertex &&
				    targetPart.IsLastPointInPart(secondVertex) &&
				    targetPart.IsFirstPointInPart(firstVertex))
				{
					if (targetPart.IsClosed)
					{
						return 0;
					}
				}
			}

			if (! targetPart.IsClosed)
			{
				return Math.Abs(forwardSegmentCount);
			}

			// If the ring is closed, make sure to test the backward and forward path
			double backwardSegmentCount = firstIntersection.VirtualTargetVertex -
			                              secondIntersection.VirtualTargetVertex;

			backwardSegmentCount =
				MathUtils.Modulo(backwardSegmentCount, targetPart.SegmentCount, true);

			if (forwardSegmentCount < 0)
			{
				forwardSegmentCount += targetPart.SegmentCount;
			}

			return forwardSegmentCount < backwardSegmentCount
				       ? forwardSegmentCount
				       : backwardSegmentCount;
		}

		/// <summary>
		/// Returns the distance of the point along the line expressed as ratio (factor).
		/// If the point is within the tolerance of the line's start or end point, the 
		/// factor will be snapped to 0 or 1, respectively. If the point is not on the line,
		/// NaN is returned.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="point"></param>
		/// <param name="tolerance">The distance tolerance to check start/end-point proximity.</param>
		/// <returns></returns>
		public static double GetPointFactorWithinLine([NotNull] Line3D line,
		                                              [NotNull] IPnt point,
		                                              double tolerance)
		{
			double pointFactorOnLine;

			double pointDistanceToLine = line.GetDistanceXYPerpendicularSigned(
				point, out pointFactorOnLine);

			// Consider remembering the side of the point for subsequent operations
			pointDistanceToLine = Math.Abs(pointDistanceToLine);

			if (pointDistanceToLine > tolerance)
			{
				// Too far off, no intersection
				return double.NaN;
			}

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (pointFactorOnLine == 0 ||
			    // ReSharper disable once CompareOfFloatsByEqualityOperator
			    pointFactorOnLine == 1)
			{
				return pointFactorOnLine;
			}

			// If within tolerance to From/To-point: snap to 0/1
			double tolerance2 = tolerance * tolerance;

			if (pointFactorOnLine < 0.5 &&
			    IsWithinDistanceXY(point, line.StartPoint, tolerance2))
			{
				// line.Start == point, use line.Start
				pointFactorOnLine = 0;
			}
			else if (IsWithinDistanceXY(point, line.EndPoint, tolerance2))
			{
				// line.End == point, use line.End
				pointFactorOnLine = 1;
			}

			return pointFactorOnLine;
		}

		private static IEnumerable<SegmentIntersection> GetSegmentIntersectionsXY(
			int sourceLineIdx,
			[NotNull] Line3D sourceLine,
			[NotNull] ISegmentList segmentList,
			double tolerance,
			[CanBeNull] SegmentIntersection previousLinearIntersection)
		{
			if (previousLinearIntersection != null)
			{
				// speculation: this segment also runs along the next / previous target
				SegmentIntersection continuousIntersection =
					TryGetContinuousLinearIntersection(
						sourceLineIdx, sourceLine, segmentList, previousLinearIntersection,
						tolerance);

				if (continuousIntersection != null)
				{
					yield return continuousIntersection;

					yield break;
				}
			}

			IEnumerable<KeyValuePair<int, Line3D>> segmentsByIndex =
				segmentList.FindSegments(sourceLine, tolerance);

			foreach (SegmentIntersection intersection in
			         IntersectLineWithLinestringXY(
				         sourceLine, sourceLineIdx, segmentsByIndex, tolerance))
			{
				yield return intersection;
			}
		}

		private static void TryAddLinearIntersectionStretch(
			[CanBeNull] IntersectionPoint3D startPoint,
			[CanBeNull] IntersectionPoint3D endPoint,
			[NotNull] List<IntersectionPoint3D> result)
		{
			if (startPoint != null)
			{
				result.Add(startPoint);
			}

			if (endPoint != null)
			{
				result.Add(endPoint);
			}

			if (startPoint != null && endPoint != null)
			{
				EnsureLinearIntersectionDirection(startPoint, endPoint);
			}
		}

		public static bool IsTargetRingNullPoint(
			[NotNull] SegmentIntersection thisLinearIntersection,
			[CanBeNull] SegmentIntersection previousLinearIntersection,
			ISegmentList targetSegments)
		{
			if (previousLinearIntersection == null)
			{
				return false;
			}

			if (thisLinearIntersection.TargetStartIntersects &&
			    targetSegments.IsFirstSegmentInPart(thisLinearIntersection.TargetIndex) &&
			    targetSegments.IsLastSegmentInPart(previousLinearIntersection.TargetIndex) &&
			    targetSegments.IsClosed)
			{
				return true;
			}

			// The linear intersections can be inverted, i.e. travelling backwards along target:
			if (thisLinearIntersection.TargetStartIntersects && // TargetEndIntersects??
			    targetSegments.IsLastSegmentInPart(thisLinearIntersection.TargetIndex) &&
			    targetSegments.IsFirstSegmentInPart(previousLinearIntersection.TargetIndex) &&
			    targetSegments.IsClosed)
			{
				return true;
			}

			return false;
		}

		private static IEnumerable<SegmentIntersection> IntersectLineWithLinestringXY(
			[NotNull] Line3D line1, int line1Index,
			[NotNull] IEnumerable<KeyValuePair<int, Line3D>> linestring2Segments,
			double tolerance)
		{
			// Tolerance rules:
			// If there is an accurate intersection along the segment's interior but the source start AND end are
			// within the tolerance, the source line is used

			// If there is no accurate intersection:
			// The line1 start or the line1 end could be within distance of the other line -> add source point
			// the other line's start or end could be within distance of this line -> add target point
			// if at least 2 start/end points are within distance: create Line3D result from source line

			// TODO: Test for 0-length lines

			foreach (KeyValuePair<int, Line3D> path2Segment in
			         linestring2Segments.OrderBy(kvp => kvp.Key))
			{
				int targetIdx = path2Segment.Key;
				Line3D otherLine = path2Segment.Value;

				// With the speculative optimization for continued linear intersections
				// providing a known target end intersection factor has no extra benefit.
				SegmentIntersection intersection =
					SegmentIntersection.CalculateIntersectionXY(
						line1Index, targetIdx, line1, otherLine, tolerance);

				if (intersection.HasIntersection)
				{
					yield return intersection;
				}
			}
		}

		[CanBeNull]
		private static SegmentIntersection TryGetContinuousLinearIntersection(
			int lineIdx,
			[NotNull] Line3D line1,
			[NotNull] ISegmentList segmentList2,
			[CanBeNull] SegmentIntersection previous,
			double tolerance)
		{
			if (previous == null ||
			    previous.SourceIndex + 1 != lineIdx)
			{
				// We jumped over some source segments
				return null;
			}

			int? targetIdx = previous.LinearIntersectionInOppositeDirection
				                 ? segmentList2.PreviousSegmentIndex(previous.TargetIndex)
				                 : segmentList2.NextSegmentIndex(previous.TargetIndex);

			if (targetIdx == null)
			{
				return null;
			}

			Line3D targetSegment =
				segmentList2.GetSegment(targetIdx.Value);

			if (targetSegment.EqualsXY(line1, tolerance))
			{
				SegmentIntersection resultIntersection =
					SegmentIntersection.CreateCoincidenceIntersectionXY(
						lineIdx, targetIdx.Value,
						previous.LinearIntersectionInOppositeDirection);

				return resultIntersection;
			}

			return null;
		}

		private static bool IsWithinDistanceXY(IPnt point1, IPnt point2,
		                                       double distanceSquared)
		{
			return GeomUtils.GetDistanceSquaredXY(point1, point2) <= distanceSquared;
		}
	}
}

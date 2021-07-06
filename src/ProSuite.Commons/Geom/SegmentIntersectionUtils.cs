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
		/// Gets the segment intersections between the specified path and multiline-string.
		/// </summary>
		/// <param name="segmentList1"></param>
		/// <param name="segmentList2"></param>
		/// <param name="tolerance"></param>
		/// <param name="optimizeLinearIntersections">If true, an optimiziation that improves the
		/// performance when linear intersections are present. If a segmentList1 segment is equal
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

			for (var lineIdx = 0; lineIdx < segmentList1.SegmentCount; lineIdx++)
			{
				Line3D line = segmentList1.GetSegment(lineIdx);

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

		public static IEnumerable<SegmentIntersection>
			GetFilteredIntersectionsOrderedAlongSourceSegments(
				[NotNull] IEnumerable<SegmentIntersection> intersections,
				[NotNull] ISegmentList source)
		{
			var intersectionsForCurrentSourceSegment =
				new List<SegmentIntersection>(3);

			var allLinearIntersectionFactors = new Dictionary<int, HashSet<double>>();

			int currentIndex = -1;
			foreach (SegmentIntersection intersection in intersections)
			{
				if (intersection.SourceIndex != currentIndex)
				{
					currentIndex = intersection.SourceIndex;

					foreach (
						SegmentIntersection collectedIntersection in
						FilterIntersections(intersectionsForCurrentSourceSegment,
						                    source, allLinearIntersectionFactors)
							.OrderBy(i => i.GetFirstIntersectionAlongSource()))
					{
						yield return collectedIntersection;
					}

					intersectionsForCurrentSourceSegment.Clear();
				}

				CollectIntersection(intersection, source, allLinearIntersectionFactors,
				                    intersectionsForCurrentSourceSegment);
			}

			foreach (
				SegmentIntersection collectedIntersection in
				FilterIntersections(intersectionsForCurrentSourceSegment,
				                    source, allLinearIntersectionFactors)
					.OrderBy(i => i.GetFirstIntersectionAlongSource()))
			{
				yield return collectedIntersection;
			}
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

				// For symmetry reasons, also the target ring start/end should break linear intersections.
				// Otherwise the result is different in XY depending on the order of the arguments.
				bool isTargetRingNullPoint = IsTargetRingNullPoint(
					fromPoint.SegmentIntersection,
					previousLinearEnd?.SegmentIntersection,
					targetSegments);

				if (previousLinearEnd == null ||
				    ! isTargetRingNullPoint && previousLinearEnd.Point.Equals(fromPoint.Point))
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

		private static IEnumerable<SegmentIntersection> FilterIntersections(
			[NotNull] IEnumerable<SegmentIntersection> intersectionsForSourceSegment,
			[NotNull] ISegmentList source,
			[NotNull] IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart)
		{
			foreach (SegmentIntersection intersection in intersectionsForSourceSegment)
			{
				var filter = false;

				if (! intersection.HasLinearIntersection &&
				    intersection.SingleInteriorIntersectionFactor == null)
				{
					double intersectionFactor;
					if (intersection.SourceStartIntersects)
					{
						intersectionFactor = 0;
					}
					else if (intersection.SourceEndIntersects)
					{
						intersectionFactor = 1;
					}
					else if (intersection.TargetStartIntersects)
					{
						intersectionFactor =
							Assert.NotNull(intersection.TargetStartFactor).Value;
					}
					else
					{
						intersectionFactor =
							Assert.NotNull(intersection.TargetEndFactor).Value;
					}

					int partIndex;
					int localSegmentIndex =
						source.GetLocalSegmentIndex(intersection.SourceIndex, out partIndex);

					double intersectionFactorPartGlobal = localSegmentIndex + intersectionFactor;

					// TODO: Extract class IntersectionFactors which encapsulates all the
					// Contains, Add etc. methods, probably even the filtering
					if (ContainsIntersection(usedIntersectionFactorsByPart, partIndex,
					                         intersectionFactorPartGlobal))
					{
						filter = true;
					}
					else
					{
						AddIntersectionFactor(intersectionFactorPartGlobal, partIndex,
						                      usedIntersectionFactorsByPart, source);
					}
				}

				if (! filter)
				{
					yield return intersection;
				}
			}
		}

		private static bool ContainsIntersection(
			IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart,
			int sourcePartIndex, double intersectionFactor)
		{
			HashSet<double> usedIntersectionFactors;
			return usedIntersectionFactorsByPart.TryGetValue(
				       sourcePartIndex, out usedIntersectionFactors) &&
			       usedIntersectionFactors.Contains(intersectionFactor);
		}

		private static void AddIntersectionFactor(
			double localIntersectionFactor,
			int partIndex,
			[NotNull] IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart)
		{
			HashSet<double> localIntersectionFactors;

			if (! usedIntersectionFactorsByPart.TryGetValue(partIndex, out
			                                                localIntersectionFactors))
			{
				localIntersectionFactors = new HashSet<double>();
				usedIntersectionFactorsByPart.Add(partIndex, localIntersectionFactors);
			}

			localIntersectionFactors.Add(localIntersectionFactor);
		}

		private static void AddIntersectionFactor(
			double localIntersectionFactor,
			int partIndex,
			[NotNull] IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart,
			[NotNull] ISegmentList source)
		{
			Linestring part = source.GetPart(partIndex);

			if (IsRingStartOrEnd(part, localIntersectionFactor))
			{
				// add both start and end point
				AddIntersectionFactor(0, partIndex, usedIntersectionFactorsByPart);
				AddIntersectionFactor(part.SegmentCount, partIndex, usedIntersectionFactorsByPart);
			}
			else
			{
				AddIntersectionFactor(localIntersectionFactor, partIndex,
				                      usedIntersectionFactorsByPart);
			}
		}

		private static bool IsRingStartOrEnd([NotNull] Linestring linestring,
		                                     double localVertexIndex)
		{
			Assert.False(double.IsNaN(localVertexIndex), "localVertexIndex is NaN");

			var vertexIndexIntegral = (int) localVertexIndex;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			bool isVertex = localVertexIndex == vertexIndexIntegral;

			if (! isVertex)
			{
				return false;
			}

			if (linestring.IsFirstPointInPart(vertexIndexIntegral) ||
			    linestring.IsLastPointInPart(vertexIndexIntegral))
			{
				return linestring.IsClosed;
			}

			return false;
		}

		private static void CollectIntersection(
			[NotNull] SegmentIntersection intersection,
			[NotNull] ISegmentList source,
			[NotNull] IDictionary<int, HashSet<double>> allLinearIntersectionFactors,
			[NotNull] ICollection<SegmentIntersection> intersectionsForCurrentSourceSegment)
		{
			// Collect segments for current index in list, unless they are clearly not needed 
			// (and would need to be filtered by a later linear intersection if added)

			bool isLinear = intersection.HasLinearIntersection;

			if (isLinear)
			{
				int partIndex;
				int localSegmentIndex =
					source.GetLocalSegmentIndex(intersection.SourceIndex, out partIndex);

				double linearIntersectionStartFactor =
					localSegmentIndex +
					intersection.GetLinearIntersectionStartFactor(true);

				double linearIntersectionEndFactor =
					localSegmentIndex +
					intersection.GetLinearIntersectionEndFactor(true);

				if (intersection.IsSourceZeroLength2D &&
				    ContainsIntersection(allLinearIntersectionFactors,
				                         partIndex, linearIntersectionEndFactor))
				{
					// avoid double linear segments if the source segment is vertical
					return;
				}

				AddIntersectionFactor(linearIntersectionStartFactor, partIndex,
				                      allLinearIntersectionFactors, source);

				AddIntersectionFactor(linearIntersectionEndFactor, partIndex,
				                      allLinearIntersectionFactors, source);
			}

			if (! isLinear && intersection.SourceStartIntersects &&
			    source.IsFirstSegmentInPart(intersection.SourceIndex) && source.IsClosed)
			{
				// will be reported again at the end
				return;
			}

			if (! isLinear && intersection.SourceEndIntersects &&
			    ! source.IsLastSegmentInPart(intersection.SourceIndex))
			{
				// will be reported again at next segment
				return;
			}

			intersectionsForCurrentSourceSegment.Add(intersection);
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
		}

		private static bool IsTargetRingNullPoint(
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
	}
}
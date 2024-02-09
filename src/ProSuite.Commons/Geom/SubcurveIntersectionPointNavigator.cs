using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class SubcurveIntersectionPointNavigator
	{
		private IList<IntersectionPoint3D> _intersectionsAlongSource;

		private IList<IntersectionPoint3D> _intersectionsAlongTarget;

		private IntersectionClusters _intersectionClusters;

		private IList<IntersectionPoint3D> _intersectionsInboundTarget;
		private IList<IntersectionPoint3D> _intersectionsOutboundTarget;
		private IList<IntersectionPoint3D> _intersectionsInboundSource;
		private IList<IntersectionPoint3D> _intersectionsOutboundSource;

		private IList<KeyValuePair<IntersectionPoint3D, RelativeTrajectory>> _targetTrajectories;

		private IntersectionPoint3D _currentStartIntersection;
		private IList<IntersectionPoint3D> _navigableIntersections;

		public SubcurveIntersectionPointNavigator(
			[NotNull] IList<IntersectionPoint3D> intersectionPoints,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			double tolerance)
		{
			Source = source;
			Target = target;
			Tolerance = tolerance;

			HashSet<IntersectionPoint3D> pointsToIgnore =
				GetLinearIntersectionPointsWithinOtherLinearIntersection(intersectionPoints);

			if (pointsToIgnore.Count > 0)
			{
				// This means that the target has several linear intersections in the same source
				// intersection stretch. The target might be narrower than the tolerance, resulting
				// in wrong intersection subcurve navigation!
				PotentiallyNonSimple = true;
			}

			IntersectionPoints = intersectionPoints.Where(p => ! pointsToIgnore.Contains(p))
			                                       .ToList();
		}

		private HashSet<IntersectionPoint3D>
			GetLinearIntersectionPointsWithinOtherLinearIntersection(
				IEnumerable<IntersectionPoint3D> intersections)
		{
			HashSet<IntersectionPoint3D> result = new HashSet<IntersectionPoint3D>();

			// Filter linear intersections within linear intersections:
			IntersectionPoint3D previousPoint = null;
			Tuple<IntersectionPoint3D, IntersectionPoint3D> previousLinear = null;
			foreach (IntersectionPoint3D current in intersections)
			{
				if (previousPoint != null &&
				    previousPoint.SourcePartIndex == current.SourcePartIndex &&
				    previousPoint.TargetPartIndex == current.TargetPartIndex &&
				    previousPoint.Type == IntersectionPointType.LinearIntersectionStart &&
				    current.Type == IntersectionPointType.LinearIntersectionEnd)
				{
					if (IsWithinCurrentLinearIntersection(previousPoint, current, previousLinear) &&
					    GeomUtils.GetDistanceXY(previousPoint.Point, current.Point) > Tolerance)
					{
						// It is a non-short linear intersection bracketed by the previous linear intersection.
						// Ignore, most likely an 'inverted' intersection with the other side of a very acute angle
						// See CanGetDifferenceAreaWithLinearIntersectionWithVertexOnAcuteAngle()
						result.Add(previousPoint);
						result.Add(current);
					}

					previousLinear =
						new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
							previousPoint, current);
				}

				previousPoint = current;
			}

			return result;
		}

		private static bool IsWithinCurrentLinearIntersection(
			[NotNull] IntersectionPoint3D fromPoint,
			[NotNull] IntersectionPoint3D toPoint,
			[CanBeNull] Tuple<IntersectionPoint3D, IntersectionPoint3D> previousLinearStretch)
		{
			if (previousLinearStretch == null)
			{
				return false;
			}

			IntersectionPoint3D previousLinearStart = previousLinearStretch.Item1;
			IntersectionPoint3D previousLinearEnd = previousLinearStretch.Item2;

			if (previousLinearStart == null || previousLinearEnd == null)
			{
				return false;
			}

			if (fromPoint.SourcePartIndex != previousLinearStart.SourcePartIndex)
			{
				return false;
			}

			if (fromPoint.TargetPartIndex != previousLinearStart.TargetPartIndex)
			{
				return false;
			}

			// The to-point must be before (or equal) the previous end...
			if (toPoint.VirtualSourceVertex > previousLinearEnd.VirtualSourceVertex)
			{
				return false;
			}

			// ... and the from point must come after the previous start.
			// Ignore equal 1-segments linear intersections (likely inverted) at pointy angles
			return fromPoint.VirtualSourceVertex > previousLinearStart.VirtualSourceVertex;
		}

		/// <summary>
		/// Whether the source is a ring that is guaranteed to be closed in which case the start
		/// and end points will be skipped if they are on the target.
		/// If set to false, the properties <see cref="IntersectionsInboundTarget"/> and
		/// <see cref="IntersectionsOutboundTarget"/> will throw an exception.
		/// </summary>
		public bool AssumeSourceRings { get; set; } = true;

		public bool AllowBoundaryLoops { get; set; } = true;

		public ISegmentList Source { get; }
		public ISegmentList Target { get; }
		public double Tolerance { get; }

		public IList<IntersectionPoint3D> IntersectionPoints { get; }

		public bool HasUnClusteredIntersectionPoints =>
			IntersectionClusters.HasUnClusteredIntersections;

		private IList<IntersectionPoint3D> IntersectionsNotUsedForNavigation { get; } =
			new List<IntersectionPoint3D>();

		public IList<IntersectionPoint3D> IntersectionsAlongSource
		{
			get
			{
				if (_intersectionsAlongSource == null)
				{
					CalculateIntersections();
				}

				return _intersectionsAlongSource;
			}
		}

		public IList<IntersectionPoint3D> IntersectionsAlongTarget
		{
			get
			{
				if (_intersectionsAlongTarget == null)
				{
					CalculateIntersections();
				}

				return _intersectionsAlongTarget;
			}
		}

		/// <summary>
		/// The intersections that allow navigating between source and target in a meaningful way.
		/// The intersections are ordered along the source.
		/// </summary>
		public IList<IntersectionPoint3D> NavigableIntersections
		{
			get
			{
				if (_navigableIntersections == null)
				{
					_navigableIntersections = GetNavigableIntersections(AssumeSourceRings);
				}

				return _navigableIntersections;
			}
		}

		private Dictionary<IntersectionPoint3D, KeyValuePair<int, int>> IntersectionOrders
		{
			get;
			set;
		}

		/// <summary>
		/// Intersections at which the target 'arrives' at the source ring boundary
		/// from the inside. The target linestring does not necessarily have to continue
		/// to the outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> IntersectionsOutboundTarget
		{
			get
			{
				if (_intersectionsOutboundTarget == null)
				{
					ClassifyIntersectionsTargetTrajectories(Source, Target,
					                                        out _intersectionsInboundTarget,
					                                        out _intersectionsOutboundTarget);
				}

				return Assert.NotNull(_intersectionsOutboundTarget);
			}
		}

		/// <summary>
		/// Intersections at which the target 'departs' from the source ring boundary
		/// to the inside. The target linestring does not necessarily have to arrive
		/// from the outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> IntersectionsInboundTarget
		{
			get
			{
				if (_intersectionsInboundTarget == null)
				{
					ClassifyIntersectionsTargetTrajectories(Source, Target,
					                                        out _intersectionsInboundTarget,
					                                        out _intersectionsOutboundTarget);
				}

				return _intersectionsInboundTarget;
			}
		}

		/// <summary>
		/// Intersections at which the source 'departs' from the target ring boundary
		/// to the outside. The source linestring does not necessarily have to arrive
		/// from the inside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> IntersectionsOutboundSource
		{
			get
			{
				if (_intersectionsOutboundSource == null)
				{
					ClassifyIntersectionsSourceTrajectories(
						Source, Target,
						out _intersectionsInboundSource,
						out _intersectionsOutboundSource);
				}

				return _intersectionsOutboundSource;
			}
		}

		/// <summary>
		/// Intersections at which the source 'departs' from the target ring boundary
		/// to the inside. The source linestring does not necessarily have to arrive
		/// from the outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> IntersectionsInboundSource
		{
			get
			{
				if (_intersectionsInboundSource == null)
				{
					ClassifyIntersectionsSourceTrajectories(
						Source, Target,
						out _intersectionsInboundSource,
						out _intersectionsOutboundSource);
				}

				return _intersectionsInboundSource;
			}
		}

		public HashSet<IntersectionPoint3D> VisitedIntersections { get; } =
			new HashSet<IntersectionPoint3D>();

		public HashSet<IntersectionPoint3D> FirstIntersectionsPerPart
		{
			get
			{
				var result = new HashSet<IntersectionPoint3D>();

				int? currentTargetPart = null;

				foreach (IntersectionPoint3D intersection in IntersectionsAlongTarget)
				{
					if (currentTargetPart != intersection.TargetPartIndex)
					{
						result.Add(intersection);
					}

					currentTargetPart = intersection.TargetPartIndex;
				}

				return result;
			}
		}

		public HashSet<IntersectionPoint3D> LastIntersectionsPerPart
		{
			get
			{
				var result = new HashSet<IntersectionPoint3D>();

				IntersectionPoint3D previous = null;
				foreach (IntersectionPoint3D intersection in IntersectionsAlongTarget)
				{
					if (previous != null &&
					    intersection.TargetPartIndex != previous.TargetPartIndex)
					{
						// The previous was the last in part
						result.Add(previous);
					}

					previous = intersection;
				}

				return result;
			}
		}

		private IntersectionClusters IntersectionClusters
		{
			get
			{
				if (_intersectionClusters == null)
				{
					_intersectionClusters =
						GetMultiIntersectionClusters(IntersectionsAlongSource,
						                             IntersectionsAlongTarget);
				}

				return _intersectionClusters;
			}
		}

		public bool PotentiallyNonSimple { get; set; }

		public IEnumerable<IntersectionPoint3D> GetIntersectionsWithOutBoundTarget(
			Predicate<IntersectionPoint3D> touchPredicate = null)
		{
			if (_targetTrajectories == null)
			{
				_targetTrajectories = ClassifyIntersectionsTargetTrajectories(Source, Target);
			}

			foreach (KeyValuePair<IntersectionPoint3D, RelativeTrajectory> kvp in
			         _targetTrajectories)
			{
				IntersectionPoint3D intersection = kvp.Key;
				RelativeTrajectory targetTrajectory = kvp.Value;

				if (targetTrajectory == RelativeTrajectory.None)
				{
					continue;
				}

				if (targetTrajectory == RelativeTrajectory.FromRight &&
				    ! intersection.DisallowTargetForward)
				{
					yield return intersection;
				}

				if (targetTrajectory == RelativeTrajectory.Both &&
				    ! intersection.DisallowTargetForward)
				{
					// Touching from inside
					if (touchPredicate == null || touchPredicate(intersection))
					{
						yield return intersection;
					}
				}
			}
		}

		public IEnumerable<IntersectionPoint3D> GetIntersectionsWithInBoundTarget(
			Predicate<IntersectionPoint3D> touchPredicate = null)
		{
			if (_targetTrajectories == null)
			{
				_targetTrajectories = ClassifyIntersectionsTargetTrajectories(Source, Target);
			}

			foreach (KeyValuePair<IntersectionPoint3D, RelativeTrajectory> kvp in
			         _targetTrajectories)
			{
				IntersectionPoint3D intersection = kvp.Key;
				RelativeTrajectory targetTrajectory = kvp.Value;

				if (targetTrajectory == RelativeTrajectory.None)
				{
					continue;
				}

				if (targetTrajectory == RelativeTrajectory.ToRight)
				{
					yield return intersection;
				}

				if (targetTrajectory == RelativeTrajectory.Both)
				{
					// Touching from inside
					if (touchPredicate == null || touchPredicate(intersection))
					{
						yield return intersection;
					}
				}
			}
		}

		public void SetStartIntersection(IntersectionPoint3D startIntersection)
		{
			_currentStartIntersection = startIntersection;
			VisitedIntersections.Add(startIntersection);
		}

		public IntersectionPoint3D GetNextIntersection(
			[NotNull] IntersectionPoint3D previousIntersection,
			bool continueOnSource, bool continueForward)
		{
			IntersectionPoint3D nextIntersection;
			IntersectionPoint3D subcurveStart = previousIntersection;

			HashSet<IntersectionPoint3D> previousCluster =
				IntersectionClusters.GetOtherIntersections(previousIntersection);

			int circuitBreaker = 0;
			do
			{
				if (circuitBreaker++ > 10000)
				{
					throw new StackOverflowException(
						"Breaking the circuit of skipping intersections. " +
						"The input is probably not simple.");
				}

				nextIntersection =
					continueOnSource
						? GetNextIntersectionAlongSource(previousIntersection)
						: GetNextIntersectionAlongTarget(
							previousIntersection, continueForward);

				previousIntersection = nextIntersection;

				if (nextIntersection == null)
				{
					return null;
				}

				// Skip pseudo-breaks to avoid going astray due to minimal angle-differences:
				// Example: GeomTopoOpUtilsTest.CanGetIntersectionAreaXYWithLinearBoundaryIntersection()
			} while (SkipIntersection(subcurveStart, nextIntersection, previousCluster));

			// Boundary loops that turned into interior rings could be detected here, if desired.
			VisitedIntersections.Add(nextIntersection);

			return nextIntersection;
		}

		public bool CanConnectToSourcePartAlongTargetForward(
			[NotNull] IntersectionPoint3D fromIntersection,
			int initialSourcePartIdx)
		{
			int targetIdx = IntersectionOrders[fromIntersection].Value;

			// Any following intersection along the same target part that intersects the required source part?
			while (++targetIdx < IntersectionsAlongTarget.Count)
			{
				IntersectionPoint3D laterIntersection =
					IntersectionsAlongTarget[targetIdx];

				if (laterIntersection.TargetPartIndex == fromIntersection.TargetPartIndex &&
				    laterIntersection.SourcePartIndex == initialSourcePartIdx)
				{
					return true;
				}
			}

			return false;
		}

		public bool CanConnectToSourcePartAlongTargetBackwards(
			IntersectionPoint3D fromIntersection,
			int initialSourcePartIdx)
		{
			int targetIdx = IntersectionOrders[fromIntersection].Value;

			// Any previous intersection in the same part that intersects the same source part?
			while (--targetIdx >= 0)
			{
				IntersectionPoint3D previousIntersection =
					IntersectionsAlongTarget[targetIdx];

				if (previousIntersection.TargetPartIndex == fromIntersection.TargetPartIndex &&
				    previousIntersection.SourcePartIndex == initialSourcePartIdx)
				{
					return true;
				}
			}

			return false;
		}

		public bool AllowConnectToSourcePartAlongOtherSourcePart(
			[NotNull] IntersectionPoint3D fromOtherSourceIntersection,
			int sourcePartToConnectTo)
		{
			IntersectionPoint3D current = fromOtherSourceIntersection;

			bool isLoop = false;
			bool isBoundaryLoop = false;

			// Any following intersection along the same target part that intersects the required source part?
			while ((current = GetNextIntersectionAlongSource(
				        current, fromOtherSourceIntersection)) != null)
			{
				// Only use intersections in the same part
				if (fromOtherSourceIntersection.SourcePartIndex != current.SourcePartIndex)
				{
					continue;
				}

				if (IntersectionsNotUsedForNavigation.Contains(current))
				{
					continue;
				}

				if (current.Equals(fromOtherSourceIntersection) &&
				    SourceSegmentCountBetween(current, fromOtherSourceIntersection) > 3)
				{
					// we've come around the ring and are back where we started
					isLoop = true;
				}

				foreach (IntersectionPoint3D duplicateOnNext in
				         GetOtherSourceIntersections(current))
				{
					if (duplicateOnNext.Equals(fromOtherSourceIntersection) &&
					    SourceSegmentCountBetween(current, duplicateOnNext) > 3)
					{
						// we've come around the ring and are back where we started
						isLoop = true;
					}
				}

				if (! isLoop)
				{
					// There was an intermediate source intersection from which we could potentially
					// travel back via a target (this requires a unit test!)
					return true;
				}

				isBoundaryLoop = IntersectionClusters.GetSourceBoundaryLoops().Any(
					bl => bl.Start.ReferencesSameTargetVertex(
						fromOtherSourceIntersection, Target, Tolerance));

				if (! isBoundaryLoop)
				{
					// Do not jump onto other rings that only touch in a single point
					return false;
				}
			}

			// All intermediate source intersections where from a different part or were equal to the start
			// Boundary loops can be traversed!
			return isBoundaryLoop;
		}

		/// <summary>
		/// Whether any of the intersection points between start and end point have been used by
		/// any of the specified used subcurves. The end intersection point itself is excluded.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="andEnd"></param>
		/// <param name="usedSubcurves"></param>
		/// <param name="alongSource"></param>
		/// <returns></returns>
		/// <exception cref="StackOverflowException"></exception>
		internal bool IsAnyIntersectionUsedBetween(
			[NotNull] IntersectionPoint3D start,
			[NotNull] IntersectionPoint3D andEnd,
			[NotNull] IList<IntersectionRun> usedSubcurves,
			bool alongSource)
		{
			IntersectionPoint3D previousIntersection = start;
			IntersectionPoint3D nextIntersection;

			if (usedSubcurves.Any(
				    s => IsIntersectionUsed(start, s, alongSource)))
			{
				// The start point has been used already:
				return true;
			}

			int circuitBreaker = 0;
			do
			{
				if (circuitBreaker++ > 10000)
				{
					throw new StackOverflowException(
						"Breaking the circuit of skipping intersections. " +
						"The input is probably not simple.");
				}

				nextIntersection = alongSource
					                   ? GetNextIntersectionAlongSource(previousIntersection)
					                   : GetNextIntersectionAlongTarget(previousIntersection, true);

				Assert.NotNull(nextIntersection, "No next intersection");

				bool usedBySubcurveStart = usedSubcurves.Any(
					s => IsIntersectionUsed(nextIntersection, s, alongSource));

				if (usedBySubcurveStart)
				{
					return true;
				}

				if (nextIntersection.Equals(andEnd))
				{
					// Back at the start
					return false;
				}

				// TODO: Check this:
				if (alongSource && nextIntersection.SourcePartIndex != start.SourcePartIndex)
				{
					// We're following the wrong ring
					return false;
				}

				if (! alongSource && nextIntersection.TargetPartIndex != start.TargetPartIndex)
				{
					// We're following the wrong ring
					return false;
				}

				previousIntersection = nextIntersection;
			} while (true);
		}

		private bool IsIntersectionUsed(IntersectionPoint3D intersection,
		                                IntersectionRun byIntersectionRun,
		                                bool alongSource)
		{
			if (alongSource && ! byIntersectionRun.RunsAlongSource)
			{
				return false;
			}

			bool alongTarget = ! alongSource;

			if (alongTarget && ! byIntersectionRun.RunsAlongTarget)
			{
				return false;
			}

			if (byIntersectionRun.RunsAlongForward &&
			    intersection.Equals(byIntersectionRun.PreviousIntersection))
			{
				return true;
			}

			// backwards:
			if (! byIntersectionRun.RunsAlongForward &&
			    intersection.Equals(byIntersectionRun.NextIntersection))
			{
				return true;
			}

			// This should be re-considered thoroughly. The reason for this is that in boundary
			// loops sometimes the intersection run is not broken up at the intersection cluster.
			// In these cases it must be checked whether the intersection point is in the interior
			// of the subcurve:
			if (IntersectionClusters.SourceClusterContains(intersection) &&
			    GeomRelationUtils.LinesInteriorIntersectXY(
				    byIntersectionRun.Subcurve, intersection.Point, Tolerance))
			{
				return true;
			}

			return false;
		}

		private IntersectionPoint3D GetNextIntersectionAlongSource(
			IntersectionPoint3D current, IntersectionPoint3D startedAt)
		{
			if (current == null)
			{
				current = startedAt;
			}

			int currentIdx = IntersectionOrders[current].Key;

			int nextAlongSourceIdx = currentIdx + 1;

			if (nextAlongSourceIdx == IntersectionsAlongSource.Count)
			{
				nextAlongSourceIdx = 0;
			}

			IntersectionPoint3D nextAlongSource = IntersectionsAlongSource[nextAlongSourceIdx];

			if (nextAlongSource == startedAt)
			{
				return null;
			}

			return nextAlongSource;
		}

		public bool HasMultipleSourceIntersections([NotNull] IntersectionPoint3D atIntersection)
		{
			return IntersectionClusters.HasMultipleSourceIntersections(atIntersection);
		}

		public IEnumerable<IntersectionPoint3D> GetOtherSourceIntersections(
			[NotNull] IntersectionPoint3D atIntersection)
		{
			return IntersectionClusters.GetOtherSourceIntersections(atIntersection);
		}

		public IEnumerable<IntersectionPoint3D> GetOtherTargetIntersections(
			[NotNull] IntersectionPoint3D atIntersection,
			bool allowSourcePartJump = false)
		{
			return IntersectionClusters.GetOtherTargetIntersections(
				atIntersection, allowSourcePartJump);
		}

		public bool EqualsStartIntersection([NotNull] IntersectionPoint3D intersection,
		                                    bool avoidShortSegments = false)
		{
			if (intersection.Equals(_currentStartIntersection))
			{
				return true;
			}

			foreach (IntersectionPoint3D samePlaceIntersection in
			         GetOtherSourceIntersections(intersection))
			{
				if (samePlaceIntersection.Equals(_currentStartIntersection))
				{
					if (intersection.SourcePartIndex != samePlaceIntersection.SourcePartIndex)
					{
						return true;
					}

					if (IsSourceBoundaryLoopIntersectionAtStart(
						    intersection, samePlaceIntersection))
					{
						return true;
					}

					// If it's on the same rings we'll get there by curve navigation
					// We could however, avoid short segments here:
					if (avoidShortSegments)
					{
						return true;
					}
				}
			}

			foreach (IntersectionPoint3D samePlaceIntersection in
			         GetOtherTargetIntersections(intersection))
			{
				if (samePlaceIntersection.Equals(_currentStartIntersection))
				{
					if (IsSourceBoundaryLoopIntersectionAtStart(
						    intersection, samePlaceIntersection))
					{
						return true;
					}

					if (AlternateTargetEqualsStartIntersection(intersection, samePlaceIntersection))
					{
						// Identify extra rings (that are not cut by anything else)
						return true;
					}

					// If it's on the same rings we'll get there by curve navigation
					// We could however, avoid short segments here:
					if (avoidShortSegments)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gets all the first intersection along the source for each source part.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public IEnumerable<IntersectionPoint3D> GetFirstSourceIntersectionsPerPart()
		{
			int? currentSourcePart = null;
			foreach (IntersectionPoint3D intersection in NavigableIntersections)
			{
				if (currentSourcePart != intersection.SourcePartIndex)
				{
					yield return intersection;
					currentSourcePart = intersection.SourcePartIndex;
				}
			}
		}

		public IEnumerable<BoundaryLoop> GetSourceBoundaryLoops(
			[CanBeNull] Predicate<BoundaryLoop> predicate = null)
		{
			foreach (BoundaryLoop boundaryLoop in IntersectionClusters.GetSourceBoundaryLoops())
			{
				if (predicate != null && ! predicate(boundaryLoop))
				{
					continue;
				}

				yield return boundaryLoop;
			}
		}

		public IEnumerable<BoundaryLoop> GetTargetBoundaryLoops()
		{
			foreach (BoundaryLoop boundaryLoop in IntersectionClusters.GetTargetBoundaryLoops())
			{
				yield return boundaryLoop;
			}
		}

		public bool IsBoundaryLoopIntersectionAtStart([NotNull] IntersectionPoint3D intersection)
		{
			foreach (IntersectionPoint3D samePlaceIntersection in
			         GetOtherSourceIntersections(intersection))
			{
				if (samePlaceIntersection.Equals(_currentStartIntersection))
				{
					if (IsSourceBoundaryLoopIntersectionAtStart(
						    intersection, samePlaceIntersection))
					{
						return true;
					}
				}
			}

			foreach (IntersectionPoint3D samePlaceIntersection in
			         GetOtherTargetIntersections(intersection))
			{
				if (samePlaceIntersection.Equals(_currentStartIntersection))
				{
					if (IsTargetBoundaryLoopIntersectionAtStart(
						    intersection, samePlaceIntersection))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool IsSourceBoundaryLoopIntersectionAtStart(
			[NotNull] IntersectionPoint3D intersectionPoint,
			[NotNull] IntersectionPoint3D samePlaceAsStartIntersection)
		{
			IntersectionPoint3D otherIntersection;

			if (_currentStartIntersection.Equals(intersectionPoint))
			{
				otherIntersection = samePlaceAsStartIntersection;
			}
			else if (_currentStartIntersection.Equals(samePlaceAsStartIntersection))
			{
				otherIntersection = intersectionPoint;
			}
			else
			{
				return false;
			}

			if (intersectionPoint.SourcePartIndex != _currentStartIntersection.SourcePartIndex)
			{
				// TODO: Can we travel (only along the source) between the intersection points?
				return true;
			}

			if (_currentStartIntersection.Type == IntersectionPointType.TouchingInPoint)
			{
				// NOTE: Sometimes the start point is not in the OutboundTarget points.
				// This should probably get additional unit-testing
				return true;
			}

			if (! AllowBoundaryLoops)
			{
				// Union: Remaining boundary loops will be cleaned up in ring-ring processing
				return true;
			}

			// If the start intersection is both outbound and inbound,
			// it is presumably a boundary loop:
			// - If at the start intersection the target continues to the right (intersect, right-side rings)
			//   and the source boundary loop goes to the outside of the target:
			//   The outbound targets contains the start, the inbound target contains the intersection
			if (IntersectionsOutboundTarget.Contains(_currentStartIntersection) &&
			    IntersectionsInboundTarget.Contains(otherIntersection) &&
			    SourceSegmentCountBetween(otherIntersection, _currentStartIntersection) > 1)
			{
				return true;
			}

			// ...
			// - If at the start intersection the target continues to the left (difference, left-side rings)
			//   and the source boundary loop goes to the outside of the target:
			//   The inbound targets contains the start, the outbound target contains the intersection
			if (IntersectionsInboundTarget.Contains(_currentStartIntersection) &&
			    IntersectionsOutboundTarget.Contains(otherIntersection) &&
			    SourceSegmentCountBetween(_currentStartIntersection, otherIntersection) > 1)
			{
				return true;
			}

			// If the target intersects both segments of the boundary loop (i.e. partially 'fills'
			// the boundary loop)
			if (_currentStartIntersection.Type == IntersectionPointType.LinearIntersectionEnd &&
			    otherIntersection.Type == IntersectionPointType.LinearIntersectionStart &&
			    SourceSegmentCountBetween(_currentStartIntersection, otherIntersection) > 1)
			{
				return true;
			}

			return false;
		}

		private bool IsTargetBoundaryLoopIntersectionAtStart(
			[NotNull] IntersectionPoint3D intersectionPoint,
			[NotNull] IntersectionPoint3D samePlaceAsStartIntersection)
		{
			IntersectionPoint3D otherIntersection;

			if (_currentStartIntersection.Equals(intersectionPoint))
			{
				otherIntersection = samePlaceAsStartIntersection;
			}
			else if (_currentStartIntersection.Equals(samePlaceAsStartIntersection))
			{
				otherIntersection = intersectionPoint;
			}
			else
			{
				return false;
			}

			if (intersectionPoint.TargetPartIndex != _currentStartIntersection.TargetPartIndex)
			{
				// TODO: Can we travel (only along the target) between the intersection points?
				return false;
			}

			if (_currentStartIntersection.Type == IntersectionPointType.TouchingInPoint)
			{
				// NOTE: Sometimes the start point is not in the OutboundTarget points.
				// This should probably get additional unit-testing
				return true;
			}

			// If the target intersects both segments of the boundary loop (i.e. partially 'fills'
			// the boundary loop)
			if (_currentStartIntersection.Type == IntersectionPointType.LinearIntersectionEnd &&
			    otherIntersection.Type == IntersectionPointType.LinearIntersectionStart &&
			    TargetSegmentCountBetween(_currentStartIntersection, otherIntersection) > 1)
			{
				return true;
			}

			return false;
		}

		private bool AlternateTargetEqualsStartIntersection(
			[NotNull] IntersectionPoint3D intersectionPoint,
			[NotNull] IntersectionPoint3D samePlaceAsStartIntersection)
		{
			IntersectionPoint3D otherIntersection;

			if (! TryGetNonStartIntersection(intersectionPoint, samePlaceAsStartIntersection,
			                                 out otherIntersection))
			{
				// Neither intersectionPoint nor samePlaceAsStartIntersection is the start intersection
				return false;
			}

			if (intersectionPoint.TargetPartIndex != _currentStartIntersection.TargetPartIndex)
			{
				// For example target rings that touch each other in a point which are connected
				// into a ring by a source ring that touches both along a line.
				// See CanGetIntersectionAreaXYTargetCutsAndTouchesFromInside()
				return true;
			}

			// TODO: Check for known boundary loops here, delete the rest of this method

			if (_currentStartIntersection.Type == IntersectionPointType.TouchingInPoint)
			{
				// NOTE: Sometimes the start point is not in the OutboundTarget points.
				// This should probably get additional unit-testing
				return true;
			}

			// If the target intersects both segments of the boundary loop (i.e. partially 'fills'
			// the boundary loop)
			if (_currentStartIntersection.Type == IntersectionPointType.LinearIntersectionEnd &&
			    otherIntersection.Type == IntersectionPointType.LinearIntersectionStart &&
			    TargetSegmentCountBetween(_currentStartIntersection, otherIntersection) > 1)
			{
				return true;
			}

			return false;
		}

		private bool TryGetNonStartIntersection(
			[NotNull] IntersectionPoint3D intersectionPoint,
			[NotNull] IntersectionPoint3D samePlaceAsStartIntersection,
			out IntersectionPoint3D otherIntersection)
		{
			otherIntersection = null;

			if (_currentStartIntersection.Equals(intersectionPoint))
			{
				otherIntersection = samePlaceAsStartIntersection;
			}
			else if (_currentStartIntersection.Equals(samePlaceAsStartIntersection))
			{
				otherIntersection = intersectionPoint;
			}
			else
			{
				return false;
			}

			return true;
		}

		private double SourceSegmentCountBetween([NotNull] IntersectionPoint3D firstIntersection,
		                                         [NotNull] IntersectionPoint3D secondIntersection)
		{
			return SegmentIntersectionUtils.SourceSegmentCountBetween(
				Source, firstIntersection, secondIntersection);
		}

		private double TargetSegmentCountBetween([NotNull] IntersectionPoint3D firstIntersection,
		                                         [NotNull] IntersectionPoint3D secondIntersection)
		{
			return SegmentIntersectionUtils.TargetSegmentCountBetween(
				Target, firstIntersection, secondIntersection);
		}

		private bool SkipIntersection([NotNull] IntersectionPoint3D subcurveStartIntersection,
		                              [NotNull] IntersectionPoint3D nextIntersection,
		                              HashSet<IntersectionPoint3D> previousCluster)
		{
			// Instead of all of this it would be better to try the Martinez-2009
			// approach but by intersection-run instead by segment!
			// However, boundary loops might still require some special logic for
			// the composition of the result polygons...
			if (IntersectionsNotUsedForNavigation.Contains(nextIntersection))
			{
				return true;
			}

			if (! AssumeSourceRings)
			{
				// Line / Ring intersections: Just follow the line without special logic
				return false;
			}

			if (EqualsStartIntersection(nextIntersection))
			{
				// Always allow navigation back to start
				return false;
			}

			// In touch-points allow jumping only between rings with the same orientation:
			if (AllowBoundaryLoops &&
			    nextIntersection.Type == IntersectionPointType.TouchingInPoint &&
			    subcurveStartIntersection.SourcePartIndex != nextIntersection.SourcePartIndex &&
			    Source.GetPart(subcurveStartIntersection.SourcePartIndex).ClockwiseOriented !=
			    Source.GetPart(nextIntersection.SourcePartIndex).ClockwiseOriented &&
			    ! IsUnclosedTargetEnd(nextIntersection))
			{
				return true;
			}

			if (ClusterContains(previousCluster, nextIntersection))
			{
				// Make sure to leave the intersection cluster containing the previous intersection
				// and do not run in circles or emit 0-length subcurves:
				return true;
			}

			if (IntersectionClusters.SourceClusterContains(nextIntersection) &&
			    HasDuplicateBeenVisited(nextIntersection))
			{
				// Skip it
				return true;
			}

			return false;
		}

		private bool HasDuplicateBeenVisited(IntersectionPoint3D intersection)
		{
			foreach (IntersectionPoint3D visitedIntersection in VisitedIntersections)
			{
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (visitedIntersection != intersection &&
				    visitedIntersection.SourcePartIndex == intersection.SourcePartIndex &&
				    visitedIntersection.ReferencesSameTargetVertex(intersection, Target, Tolerance))
				{
					// Only skip the intersection if it has been visited AND it is relevant for the same
					// ring. Otherwise we might miss intersections where rings touch each other.
					return true;
				}
			}

			return false;
		}

		private bool IsUnclosedTargetEnd([NotNull] IntersectionPoint3D intersectionPoint)
		{
			Linestring targetPart = Target.GetPart(intersectionPoint.TargetPartIndex);

			if (targetPart.IsClosed)
			{
				return false;
			}

			if (intersectionPoint.VirtualTargetVertex > 0 &&
			    intersectionPoint.VirtualTargetVertex < targetPart.PointCount - 1)
			{
				return false;
			}

			return true;
		}

		[CanBeNull]
		private IntersectionPoint3D GetNextIntersectionAlongSource(
			[NotNull] IntersectionPoint3D currentIntersection)
		{
			int currentSourceIdx = IntersectionOrders[currentIntersection].Key;
			int count = 0;
			IntersectionPoint3D next;

			do
			{
				int nextAlongSourceIdx = currentSourceIdx + 1;

				if (nextAlongSourceIdx == IntersectionsAlongSource.Count)
				{
					// Continue at the beginning
					nextAlongSourceIdx = 0;
				}

				if (! AssumeSourceRings && nextAlongSourceIdx <= currentSourceIdx)
				{
					return null;
				}

				Assert.True(count++ <= IntersectionsAlongSource.Count,
				            "Cannot find next intersection in same source part");

				currentSourceIdx = nextAlongSourceIdx;
				next = IntersectionsAlongSource[nextAlongSourceIdx];
			} while (next.SourcePartIndex != currentIntersection.SourcePartIndex);

			return next;
		}

		private IntersectionPoint3D GetNextIntersectionAlongTarget(
			[NotNull] IntersectionPoint3D current, bool continueForward)
		{
			int count = 0;

			int currentTargetIdx = IntersectionOrders[current].Value;

			IntersectionPoint3D next;
			do
			{
				int nextAlongTargetIdx = (currentTargetIdx + (continueForward ? 1 : -1)) %
				                         IntersectionsAlongTarget.Count;

				// TODO: CollectionUtils.GetPreviousInCircularList()
				if (nextAlongTargetIdx < 0)
				{
					nextAlongTargetIdx += IntersectionsAlongTarget.Count;
				}

				Assert.True(count++ <= IntersectionsAlongTarget.Count,
				            "Cannot find next intersection in same target part");

				currentTargetIdx = nextAlongTargetIdx;
				next = IntersectionsAlongTarget[nextAlongTargetIdx];
			} while (next.TargetPartIndex != current.TargetPartIndex);

			return next;
		}

		private static bool ClusterContains(HashSet<IntersectionPoint3D> cluster,
		                                    IntersectionPoint3D next)
		{
			if (cluster.Contains(next))
			{
				return true;
			}

			return false;
		}

		private void CalculateIntersections()
		{
			IntersectionOrders = GetOrderedIntersectionPoints(
				IntersectionPoints,
				out _intersectionsAlongSource,
				out _intersectionsAlongTarget);
		}

		private Dictionary<IntersectionPoint3D, KeyValuePair<int, int>>
			GetOrderedIntersectionPoints(
				[NotNull] IList<IntersectionPoint3D> intersectionPoints,
				out IList<IntersectionPoint3D> intersectionsAlongSource,
				out IList<IntersectionPoint3D> intersectionsAlongTarget)
		{
			intersectionsAlongSource =
				intersectionPoints.OrderBy(i => i.SourcePartIndex)
				                  .ThenBy(i => i.VirtualSourceVertex).ToList();

			intersectionsAlongTarget =
				intersectionPoints.OrderBy(i => i.TargetPartIndex)
				                  .ThenBy(i => i.VirtualTargetVertex).ToList();

			var intersectionOrders =
				new Dictionary<IntersectionPoint3D, KeyValuePair<int, int>>();

			var sourceIndex = 0;
			foreach (IntersectionPoint3D intersection in intersectionsAlongSource)
			{
				intersectionOrders.Add(intersection,
				                       new KeyValuePair<int, int>(sourceIndex++, -1));
			}

			var targetIndex = 0;
			foreach (IntersectionPoint3D intersection in intersectionsAlongTarget)
			{
				sourceIndex = intersectionOrders[intersection].Key;
				intersectionOrders[intersection] =
					new KeyValuePair<int, int>(sourceIndex, targetIndex++);
			}

			return intersectionOrders;
		}

		private IntersectionClusters GetMultiIntersectionClusters(
			[NotNull] IList<IntersectionPoint3D> intersectionsAlongSource,
			[NotNull] IList<IntersectionPoint3D> intersectionsAlongTarget)
		{
			var result = new IntersectionClusters(Source, Target, Tolerance);

			result.InitializeClusters(intersectionsAlongSource, intersectionsAlongTarget);

			return result;
		}

		private IList<IntersectionPoint3D> GetNavigableIntersections(bool sourceIsClosedRing)
		{
			IntersectionsNotUsedForNavigation.Clear();

			// Filter all non-real linear intersections (i. e. those where no deviation between
			// source and target exists. This is important to avoid incorrect inbound/outbound
			// and turn-direction decisions because the two lines continue (almost at the same
			// angle.
			var usableIntersections = IntersectionsAlongSource.ToList();

			foreach (IntersectionPoint3D unusable in GetIntersectionsNotUsedForNavigation(
				         IntersectionsAlongSource, Source, Target, sourceIsClosedRing))
			{
				usableIntersections.Remove(unusable);
				IntersectionsNotUsedForNavigation.Add(unusable);
			}

			return usableIntersections;
		}

		private void ClassifyIntersectionsTargetTrajectories(
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			[NotNull] out IList<IntersectionPoint3D> intersectionsInboundTarget,
			[NotNull] out IList<IntersectionPoint3D> intersectionsOutboundTarget)
		{
			intersectionsInboundTarget = new List<IntersectionPoint3D>();
			intersectionsOutboundTarget = new List<IntersectionPoint3D>();

			foreach (IntersectionPoint3D intersectionPoint3D in NavigableIntersections)
			{
				intersectionPoint3D.ClassifyTargetTrajectory(source, target,
				                                             out bool? targetContinuesToRightSide,
				                                             out bool? targetArrivesFromRightSide,
				                                             Tolerance);

				// In-bound takes precedence because if the target is both inbound and outbound (i.e. touching from inside)
				// the resulting part is on the left of the cut line which is consistent with other in-bound intersections.
				if (targetContinuesToRightSide == true)
				{
					intersectionsInboundTarget.Add(intersectionPoint3D);
				}
				else if (targetArrivesFromRightSide == true)
				{
					// targetContinuesToRightSide == false or null, i.e. it continues to the left or
					// it's probably the end point.
					intersectionsOutboundTarget.Add(intersectionPoint3D);
				}
			}

			if (! target.IsClosed)
			{
				// Remove dangles that cannot cut and would lead to duplicate result rings
				RemoveDeadEndIntersections(intersectionsInboundTarget, intersectionsOutboundTarget);
			}
		}

		private List<KeyValuePair<IntersectionPoint3D, RelativeTrajectory>>
			ClassifyIntersectionsTargetTrajectories(
				[NotNull] ISegmentList source,
				[NotNull] ISegmentList target)
		{
			var targetTrajectories =
				new List<KeyValuePair<IntersectionPoint3D, RelativeTrajectory>>();

			foreach (IntersectionPoint3D intersectionPoint in NavigableIntersections)
			{
				intersectionPoint.ClassifyTargetTrajectory(source, target,
				                                           out bool? targetContinuesToRightSide,
				                                           out bool? targetArrivesFromRightSide,
				                                           Tolerance);

				RelativeTrajectory targetTrajectory = RelativeTrajectory.None;

				if (targetContinuesToRightSide == true)
				{
					// The target is in-bound, i.e. it departs to the inside
					targetTrajectory = RelativeTrajectory.ToRight;
				}

				if (targetArrivesFromRightSide == true)
				{
					// The target arrives from the inside, i.e. it is 'out-bound'
					targetTrajectory = targetTrajectory == RelativeTrajectory.None
						                   ? RelativeTrajectory.FromRight
						                   : RelativeTrajectory.Both;
				}

				targetTrajectories.Add(
					new KeyValuePair<IntersectionPoint3D, RelativeTrajectory>(
						intersectionPoint, targetTrajectory));
			}

			return targetTrajectories;
		}

		private void ClassifyIntersectionsSourceTrajectories(
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			[NotNull] out IList<IntersectionPoint3D> intersectionsInboundSource,
			[NotNull] out IList<IntersectionPoint3D> intersectionsOutboundSource)
		{
			intersectionsInboundSource = new List<IntersectionPoint3D>();
			intersectionsOutboundSource = new List<IntersectionPoint3D>();

			foreach (IntersectionPoint3D intersectionPoint in NavigableIntersections)
			{
				intersectionPoint.ClassifySourceTrajectory(source, target,
				                                           out bool? sourceContinuesToRightSide,
				                                           out bool? sourceArrivesFromRightSide,
				                                           Tolerance);

				// TODO: Why not symmetrical with ClassifyIntersectionsTargetTrajectories?
				if (sourceContinuesToRightSide == false)
				{
					// The source continues to the left -> outbound
					intersectionsOutboundSource.Add(intersectionPoint);
				}
				else if (sourceContinuesToRightSide == true)
				{
					intersectionsInboundSource.Add(intersectionPoint);
				}
			}

			// TODO: To support total spaghetti, remove source dangles here
			//if (!target.IsClosed)
			//{
			//	// Remove dangles that cannot cut and would lead to duplicate result rings
			//	RemoveDeadEndIntersections(intersectionsInboundTarget, intersectionsOutboundTarget);
			//}
		}

		/// <summary>
		/// Removes the inbound/outbound target intersections that would allow going into a dead-end.
		/// This analysis has to be performed on a per-source-ring basis.
		/// </summary>
		/// <param name="intersectionsInboundTarget"></param>
		/// <param name="intersectionsOutboundTarget"></param>
		private void RemoveDeadEndIntersections(
			IList<IntersectionPoint3D> intersectionsInboundTarget,
			IList<IntersectionPoint3D> intersectionsOutboundTarget)
		{
			var firstAlongTarget =
				IntersectionsAlongTarget.FirstOrDefault();

			if (firstAlongTarget != null &&
			    intersectionsOutboundTarget.Contains(firstAlongTarget))
			{
				intersectionsOutboundTarget.Remove(firstAlongTarget);
			}

			var lastAlongTarget =
				IntersectionsAlongTarget.LastOrDefault();

			if (lastAlongTarget != null &&
			    intersectionsInboundTarget.Contains(lastAlongTarget))
			{
				// dangle at the end of the cut line
				intersectionsInboundTarget.Remove(lastAlongTarget);
			}
		}

		private IEnumerable<IntersectionPoint3D> GetIntersectionsNotUsedForNavigation(
			[NotNull] IList<IntersectionPoint3D> intersectionsAlongSource,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			bool includeSourceRingStartEnd)
		{
			var linearIntersectionBreakEvaluator = new LinearIntersectionBreakEvaluator();

			if (includeSourceRingStartEnd)
			{
				// The 'standard' linear intersection breaks at ring start/end:
				foreach (LinearIntersectionPseudoBreak linearBreak in
				         linearIntersectionBreakEvaluator.GetLinearIntersectionBreaksAtRingStart(
					         source, target, intersectionsAlongSource, Tolerance))
				{
					yield return linearBreak.PreviousEnd;
					yield return linearBreak.Restart;
				}
			}

			// Other linear intersection breaks that are not real (from a 2D perspective)
			foreach (var linearBreak in linearIntersectionBreakEvaluator
				         .GetLinearIntersectionPseudoBreaks(
					         intersectionsAlongSource, source, target))
			{
				yield return linearBreak.PreviousEnd;
				yield return linearBreak.Restart;
			}

			if (! AllowBoundaryLoops)
			{
				// Filter the single touching points of a ring when union-ing,
				// including boundary loops
				foreach (IGrouping<int, IntersectionPoint3D> groupedTouchPoints in
				         IntersectionsAlongSource.GroupBy(i => i.SourcePartIndex))
				{
					if (groupedTouchPoints.Count() == 1)
					{
						IntersectionPoint3D intersectionPoint = groupedTouchPoints.Single();

						if (intersectionPoint.Type == IntersectionPointType.TouchingInPoint)
						{
							yield return intersectionPoint;
						}
					}
				}
			}
		}
	}

	public enum RelativeTrajectory
	{
		// TODO: Consider Flags
		None,

		FromRight,

		/// <summary>
		/// The curve continues to the right side of the relative geometry, i.e. the subject curve
		/// 'departs' from the relative ring boundary to the inside in case of a ring.
		/// The subject's linestring does not necessarily have to arrive from the left side/outside.
		/// </summary>
		ToRight,

		Both
	}
}

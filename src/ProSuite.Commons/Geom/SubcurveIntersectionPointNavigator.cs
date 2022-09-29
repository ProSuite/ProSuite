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

		private HashSet<IntersectionPoint3D> _multipleSourceIntersections;
		private HashSet<IntersectionPoint3D> _multipleTargetIntersections;

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
			IntersectionPoints = intersectionPoints;
			Source = source;
			Target = target;
			Tolerance = tolerance;
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

					GetMultiIntersectionPoints(IntersectionsAlongSource, IntersectionsAlongTarget,
					                           out _multipleSourceIntersections,
					                           out _multipleTargetIntersections);
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

		public HashSet<Tuple<IntersectionPoint3D, IntersectionPoint3D>>
			SourceBoundaryLoopIntersections { get; } =
			new HashSet<Tuple<IntersectionPoint3D, IntersectionPoint3D>>();

		public HashSet<Tuple<IntersectionPoint3D, IntersectionPoint3D>>
			TargetBoundaryLoopIntersections { get; } =
			new HashSet<Tuple<IntersectionPoint3D, IntersectionPoint3D>>();

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

				if (targetTrajectory == RelativeTrajectory.FromRight)
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
			} while (SkipIntersection(subcurveStart, nextIntersection));

			// Boundary loops that turned into interior rings could be detected here, if desired.
			VisitedIntersections.Add(nextIntersection);

			return nextIntersection;
		}

		public IntersectionPoint3D GetNextIntersectionSimplifiedSkipping(
			[NotNull] IntersectionPoint3D previousIntersection,
			bool continueOnSource, bool continueForward)
		{
			IntersectionPoint3D nextIntersection;

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
			} while (IntersectionsNotUsedForNavigation.Contains(nextIntersection));

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

				if (current.Equals(fromOtherSourceIntersection))
				{
					// we're back where we started
					isLoop = true;
				}

				foreach (IntersectionPoint3D duplicateOnNext in
				         GetOtherSourceIntersections(current))
				{
					if (duplicateOnNext.Equals(fromOtherSourceIntersection))
					{
						// we're back
						isLoop = true;
					}
				}

				if (! isLoop)
				{
					// There was an intermediate source intersection from which we could potentially
					// travel back via a target (this requires a unit test!)
					return true;
				}

				return false;
			}

			// All intermediate source intersections where from a different part or were equal to the start
			return false;
		}

		public bool IsNextSourceIntersection([NotNull] IntersectionPoint3D thisIntersection,
		                                     [NotNull] IntersectionPoint3D nextCandidate)
		{
			IntersectionPoint3D realNext =
				GetNextIntersectionSimplifiedSkipping(thisIntersection, true, true);

			return Assert.NotNull(realNext).Equals(nextCandidate);
		}

		public bool IsNextTargetIntersection([NotNull] IntersectionPoint3D thisIntersection,
		                                     [NotNull] IntersectionPoint3D nextCandidate,
		                                     bool forward)
		{
			IntersectionPoint3D realNext =
				GetNextIntersectionSimplifiedSkipping(thisIntersection, false, forward);

			return Assert.NotNull(realNext).Equals(nextCandidate);
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
			if (_multipleSourceIntersections == null || _multipleSourceIntersections.Count == 0)
			{
				return false;
			}

			return _multipleSourceIntersections.Contains(atIntersection);
		}

		public bool HasMultipleTargetIntersections([NotNull] IntersectionPoint3D atIntersection)
		{
			if (_multipleTargetIntersections == null || _multipleTargetIntersections.Count == 0)
			{
				return false;
			}

			return _multipleTargetIntersections.Contains(atIntersection);
		}

		public IEnumerable<IntersectionPoint3D> GetOtherSourceIntersections(
			[NotNull] IntersectionPoint3D atIntersection)
		{
			if (_multipleSourceIntersections == null || _multipleSourceIntersections.Count == 0)
			{
				yield break;
			}

			foreach (IntersectionPoint3D other in _multipleSourceIntersections)
			{
				if (other == atIntersection)
				{
					continue;
				}

				if (other.ReferencesSameTargetVertex(atIntersection, Target))
				{
					yield return other;
				}
			}
		}

		public IEnumerable<IntersectionPoint3D> GetOtherTargetIntersections(
			[NotNull] IntersectionPoint3D atIntersection)
		{
			if (_multipleTargetIntersections == null)
			{
				yield break;
			}

			foreach (IntersectionPoint3D other in _multipleTargetIntersections)
			{
				if (other == atIntersection)
				{
					continue;
				}

				if (other.ReferencesSameSourceVertex(atIntersection, Source))
				{
					yield return other;
				}
			}
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

					if (IsSourceBoundaryLoopIntersection(intersection, samePlaceIntersection))
					{
						SourceBoundaryLoopIntersections.Add(
							new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
								intersection, samePlaceIntersection));

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
					if (IsSourceBoundaryLoopIntersection(intersection, samePlaceIntersection))
					{
						SourceBoundaryLoopIntersections.Add(
							new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
								intersection, samePlaceIntersection));

						return true;
					}

					if (IsTargetBoundaryLoopIntersection(intersection, samePlaceIntersection))
					{
						// Identify extra rings (that are not cut by anything else)

						TargetBoundaryLoopIntersections.Add(
							new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
								intersection, samePlaceIntersection));
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

		public bool IsBoundaryLoopIntersection([NotNull] IntersectionPoint3D intersection)
		{
			foreach (IntersectionPoint3D samePlaceIntersection in
			         GetOtherSourceIntersections(intersection))
			{
				if (samePlaceIntersection.Equals(_currentStartIntersection))
				{
					if (IsSourceBoundaryLoopIntersection(intersection, samePlaceIntersection))
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
					if (IsTargetBoundaryLoopIntersection(intersection, samePlaceIntersection))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool IsSourceBoundaryLoopIntersection(
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

		private bool IsTargetBoundaryLoopIntersection(
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
				return true;
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

		private double SourceSegmentCountBetween([NotNull] IntersectionPoint3D firstIntersection,
		                                         [NotNull] IntersectionPoint3D secondIntersection)
		{
			Assert.AreEqual(firstIntersection.SourcePartIndex, secondIntersection.SourcePartIndex,
			                "Intersections are not from the same part.");

			double result = secondIntersection.VirtualSourceVertex -
			                firstIntersection.VirtualSourceVertex;

			if (result < 0)
			{
				Linestring sourcePart = Source.GetPart(firstIntersection.SourcePartIndex);
				result += sourcePart.SegmentCount;
			}

			return Math.Floor(result);
		}

		private double TargetSegmentCountBetween([NotNull] IntersectionPoint3D firstIntersection,
		                                         [NotNull] IntersectionPoint3D secondIntersection)
		{
			Assert.AreEqual(firstIntersection.TargetPartIndex, secondIntersection.TargetPartIndex,
			                "Intersections are not from the same part.");

			double result = secondIntersection.VirtualTargetVertex -
			                firstIntersection.VirtualTargetVertex;

			if (result < 0)
			{
				Linestring targetPart = Target.GetPart(firstIntersection.TargetPartIndex);
				result += targetPart.SegmentCount;
			}

			return Math.Floor(result);
		}

		private bool SkipIntersection([NotNull] IntersectionPoint3D subcurveStartIntersection,
		                              [NotNull] IntersectionPoint3D nextIntersection)
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

			if (EqualsStartIntersection(nextIntersection))
			{
				// Always allow navigation back to start
				return false;
			}

			if (_multipleSourceIntersections != null &&
			    _multipleSourceIntersections.Contains(nextIntersection) &&
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
				    visitedIntersection.ReferencesSameTargetVertex(intersection, Target, Tolerance))
				{
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
			int nextAlongSourceIdx;
			int currentSourceIdx = IntersectionOrders[currentIntersection].Key;
			int count = 0;

			do
			{
				nextAlongSourceIdx = currentSourceIdx + 1;

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
			} while (IntersectionsAlongSource[nextAlongSourceIdx].SourcePartIndex !=
			         currentIntersection.SourcePartIndex);

			IntersectionPoint3D result = IntersectionsAlongSource[nextAlongSourceIdx];

			return result;
		}

		private IntersectionPoint3D GetNextIntersectionAlongTarget(
			[NotNull] IntersectionPoint3D current, bool continueForward)
		{
			int nextAlongTargetIdx;
			int count = 0;

			int currentTargetIdx = IntersectionOrders[current].Value;

			do
			{
				nextAlongTargetIdx = (currentTargetIdx + (continueForward ? 1 : -1)) %
				                     IntersectionsAlongTarget.Count;

				// TODO: CollectionUtils.GetPreviousInCircularList()
				if (nextAlongTargetIdx < 0)
				{
					nextAlongTargetIdx += IntersectionsAlongTarget.Count;
				}

				Assert.True(count++ <= IntersectionsAlongTarget.Count,
				            "Cannot find next intersection in same target part");

				currentTargetIdx = nextAlongTargetIdx;
			} while (IntersectionsAlongTarget[nextAlongTargetIdx].TargetPartIndex !=
			         current.TargetPartIndex);

			var result = IntersectionsAlongTarget[nextAlongTargetIdx];

			return result;
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

		private void GetMultiIntersectionPoints(IList<IntersectionPoint3D> intersectionsAlongSource,
		                                        IList<IntersectionPoint3D> intersectionsAlongTarget,
		                                        out HashSet<IntersectionPoint3D>
			                                        multipleSourceIntersections,
		                                        out HashSet<IntersectionPoint3D>
			                                        multipleTargetIntersections)
		{
			// TODO: Probably the known boundary loop intersections would be enough?
			multipleSourceIntersections =
				DetermineDuplicateSourceIntersections(intersectionsAlongTarget);
			multipleTargetIntersections =
				DetermineDuplicateTargetIntersections(intersectionsAlongSource);
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
				// Boundary loop intersections are important to navigate but not as start points
				if (SourceBoundaryLoopIntersections.Any(
					    bl => bl.Item1.Equals(intersectionPoint3D) ||
					          bl.Item2.Equals(intersectionPoint3D)))
				{
					continue;
				}

				if (TargetBoundaryLoopIntersections.Any(
					    bl => bl.Item1.Equals(intersectionPoint3D) ||
					          bl.Item2.Equals(intersectionPoint3D)))
				{
					continue;
				}

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
				// Boundary loop intersections are important to navigate but not as start points
				if (SourceBoundaryLoopIntersections.Any(
					    bl => bl.Item1.Equals(intersectionPoint) ||
					          bl.Item2.Equals(intersectionPoint)))
				{
					continue;
				}

				if (TargetBoundaryLoopIntersections.Any(
					    bl => bl.Item1.Equals(intersectionPoint) ||
					          bl.Item2.Equals(intersectionPoint)))
				{
					continue;
				}

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
			if (includeSourceRingStartEnd)
			{
				// The 'standard' linear intersection breaks at ring start/end:
				foreach (LinearIntersectionPseudoBreak linearBreak in
				         GeomTopoOpUtils.GetLinearIntersectionBreaksAtRingStart(
					         source, target, intersectionsAlongSource, true, Tolerance))
				{
					foreach (IntersectionPoint3D intersectionPoint3D in EvaluateLinearBreak(
						         linearBreak))
					{
						yield return intersectionPoint3D;
					}
				}
			}

			// Other linear intersection breaks that are not real (from a 2D perspective)
			foreach (var linearBreak in GeomTopoOpUtils.GetLinearIntersectionPseudoBreaks(
				         intersectionsAlongSource, source, target, true, Tolerance))
			{
				foreach (IntersectionPoint3D intersectionPoint3D in
				         EvaluateLinearBreak(linearBreak))
				{
					yield return intersectionPoint3D;
				}
			}
		}

		private IEnumerable<IntersectionPoint3D> EvaluateLinearBreak(
			LinearIntersectionPseudoBreak linearBreak)
		{
			if (! linearBreak.IsBoundaryLoop)
			{
				yield return linearBreak.PreviousEnd;
				yield return linearBreak.Restart;
			}

			if (linearBreak.IsSourceBoundaryLoop)
			{
				SourceBoundaryLoopIntersections.Add(
					new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
						linearBreak.Restart, linearBreak.PreviousEnd));
			}

			if (linearBreak.IsTargetBoundaryLoop)
			{
				TargetBoundaryLoopIntersections.Add(
					new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
						linearBreak.Restart, linearBreak.PreviousEnd));
			}
		}

		/// <summary>
		/// Finds intersections at the same location which reference the same
		/// target location. These could be just a break in a linear intersection
		/// or a boundary loop in the source
		/// </summary>
		/// <param name="intersectionsAlongTarget"></param>
		/// <returns></returns>
		private HashSet<IntersectionPoint3D> DetermineDuplicateSourceIntersections(
			IList<IntersectionPoint3D> intersectionsAlongTarget)
		{
			HashSet<IntersectionPoint3D> result = new HashSet<IntersectionPoint3D>();

			foreach (var intersectionsPerRing in
			         intersectionsAlongTarget.GroupBy(ip => ip.TargetPartIndex))
			{
				WithRingIntersectionPairs(
					intersectionsPerRing,
					(p1, p2) =>
					{
						if (p1.ReferencesSameTargetVertex(p2, Target, Tolerance))
						{
							// Possibly also check if we really can jump from one source part to the other
							// It can be jumped where the source vertices are exactly on top of each other
							// or there is a boundary loop? But
							// NOT: with a very thin spike where both sides intersect

							result.Add(p1);
							result.Add(p2);
						}
					});
			}

			return result.Count == 0 ? null : result;
		}

		/// <summary>
		/// Finds intersections at the same location which reference the same
		/// target location. These could be just a break in a linear intersection
		/// or a boundary loop in the source
		/// </summary>
		/// <param name="intersectionsAlongSource"></param>
		/// <returns></returns>
		private HashSet<IntersectionPoint3D> DetermineDuplicateTargetIntersections(
			IList<IntersectionPoint3D> intersectionsAlongSource)
		{
			HashSet<IntersectionPoint3D> result = new HashSet<IntersectionPoint3D>();

			foreach (var pointsPerRing in
			         intersectionsAlongSource.GroupBy(ip => ip.SourcePartIndex))
			{
				WithRingIntersectionPairs(
					pointsPerRing,
					(p1, p2) =>
					{
						if (p1.ReferencesSameSourceVertex(p2, Source, Tolerance))
						{
							result.Add(p1);
							result.Add(p2);
						}
					});
			}

			return result.Count == 0 ? null : result;
		}

		private static void WithRingIntersectionPairs(
			IEnumerable<IntersectionPoint3D> intersectionsPerRing,
			Action<IntersectionPoint3D, IntersectionPoint3D> pairAction)
		{
			IntersectionPoint3D previous = null;
			IntersectionPoint3D first = null;
			foreach (IntersectionPoint3D intersection in intersectionsPerRing)
			{
				if (first == null)
				{
					first = intersection;
				}

				if (previous != null)
				{
					pairAction(previous, intersection);
				}

				previous = intersection;
			}

			if (first != null && ! first.Equals(previous))
			{
				// Compare last with first

				pairAction(previous, first);
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

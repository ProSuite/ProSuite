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

		private HashSet<IntersectionPoint3D> _multiIntersection;

		private IList<IntersectionPoint3D> _intersectionsInboundTarget;
		private IList<IntersectionPoint3D> _intersectionsOutboundTarget;

		public SubcurveIntersectionPointNavigator(
			IList<IntersectionPoint3D> intersectionPoints, ISegmentList source, ISegmentList target)
		{
			IntersectionPoints = intersectionPoints;
			Source = source;
			Target = target;
		}

		private ISegmentList Source { get; }

		private ISegmentList Target { get; }

		private IList<IntersectionPoint3D> IntersectionPoints { get; }

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

		public Dictionary<IntersectionPoint3D, KeyValuePair<int, int>> IntersectionOrders
		{
			get;
			private set;
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
					ClassifyIntersections(Source, Target,
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
					ClassifyIntersections(Source, Target,
					                      out _intersectionsInboundTarget,
					                      out _intersectionsOutboundTarget);
				}

				return _intersectionsInboundTarget;
			}
		}

		private HashSet<IntersectionPoint3D> VisitedIntersections { get; } =
			new HashSet<IntersectionPoint3D>();

		public void SetStartIntersection(IntersectionPoint3D startIntersection)
		{
			VisitedIntersections.Add(startIntersection);
		}

		public IntersectionPoint3D GetNextIntersection(IntersectionPoint3D previousIntersection,
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

				// Skip pseudo-breaks to avoid going astray due to minimal angle-differences:
				// Example: GeomTopoOpUtilsTest.CanGetIntersectionAreaXYWithLinearBoundaryIntersection()
			} while (SkipIntersection(subcurveStart, nextIntersection));

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

		private bool SkipIntersection(IntersectionPoint3D subcurveStartIntersection,
		                              IntersectionPoint3D nextIntersection)
		{
			if (IntersectionsNotUsedForNavigation.Contains(nextIntersection))
			{
				return true;
			}

			if (nextIntersection.Type == IntersectionPointType.TouchingInPoint &&
			    subcurveStartIntersection.SourcePartIndex != nextIntersection.SourcePartIndex &&
			    ! IsUnclosedTargetEnd(nextIntersection))
			{
				return true;
			}

			if (_multiIntersection != null &&
			    _multiIntersection.Contains(nextIntersection) &&
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
				    visitedIntersection.ReferencesSameTargetVertex(intersection, Target))
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

		private IntersectionPoint3D GetNextIntersectionAlongSource(
			IntersectionPoint3D thisIntersection)
		{
			IntersectionPoint3D result;
			int previousSourceIdx = IntersectionOrders[thisIntersection].Key;

			int nextSourceIdx = previousSourceIdx + 1;

			if (nextSourceIdx == IntersectionsAlongSource.Count)
			{
				nextSourceIdx = 0;
			}

			int thisPartIdx = thisIntersection.SourcePartIndex;

			if (nextSourceIdx < IntersectionsAlongSource.Count &&
			    IntersectionsAlongSource[nextSourceIdx].SourcePartIndex == thisPartIdx)
			{
				result = IntersectionsAlongSource[nextSourceIdx];
			}
			else
			{
				result = IntersectionsAlongSource.First(i => i.SourcePartIndex == thisPartIdx);
			}

			return result;
		}

		private IntersectionPoint3D GetNextIntersectionAlongTarget(
			IntersectionPoint3D current, bool continueForward)
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
				out _intersectionsAlongTarget,
				out _multiIntersection);
		}

		private Dictionary<IntersectionPoint3D, KeyValuePair<int, int>>
			GetOrderedIntersectionPoints(
				[NotNull] IList<IntersectionPoint3D> intersectionPoints,
				out IList<IntersectionPoint3D> intersectionsAlongSource,
				out IList<IntersectionPoint3D> intersectionsAlongTarget,
				out HashSet<IntersectionPoint3D> multipleIntersections)
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

			multipleIntersections = DetermineDuplicateIntersections(intersectionsAlongTarget);

			return intersectionOrders;
		}

		private void ClassifyIntersections(
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			[NotNull] out IList<IntersectionPoint3D> intersectionsInboundTarget,
			[NotNull] out IList<IntersectionPoint3D> intersectionsOutboundTarget)
		{
			intersectionsInboundTarget = new List<IntersectionPoint3D>();
			intersectionsOutboundTarget = new List<IntersectionPoint3D>();
			IntersectionsNotUsedForNavigation.Clear();

			// Filter all non-real linear intersections (i. e. those where no deviation between
			// source and target exists. This is important to avoid incorrect inbound/outbound
			// and turn-direction decisions because the two lines continue (almost at the same
			// angle.
			var usableIntersections = IntersectionsAlongSource.ToList();

			foreach (IntersectionPoint3D unusable in GetIntersectionsNotUsedForNavigation(
				         IntersectionsAlongSource, Source, Target))
			{
				usableIntersections.Remove(unusable);
				IntersectionsNotUsedForNavigation.Add(unusable);
			}

			foreach (IntersectionPoint3D intersectionPoint3D in usableIntersections)
			{
				intersectionPoint3D.ClassifyTargetTrajectory(source, target,
				                                             out bool? targetContinuesToRightSide,
				                                             out bool? targetArrivesFromRightSide);

				// In-bound takes precedence because if the target is both inbound and outbound (i.e. touching from inside)
				// the resulting part is on the left of the cut line which is consistent with other in-bound intersections.
				if (targetContinuesToRightSide == true)
				{
					intersectionsInboundTarget.Add(intersectionPoint3D);
				}
				else if (targetArrivesFromRightSide == true)
				{
					intersectionsOutboundTarget.Add(intersectionPoint3D);
				}
			}

			if (! target.IsClosed)
			{
				// Remove dangles that cannot cut and would lead to duplicate result rings
				RemoveDeadEndIntersections(intersectionsInboundTarget, intersectionsOutboundTarget);
			}
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

		private static IEnumerable<IntersectionPoint3D> GetIntersectionsNotUsedForNavigation(
			[NotNull] IList<IntersectionPoint3D> intersectionPoints,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target)
		{
			// The 'standard' linear intersection breaks at ring start/end:
			foreach (IntersectionPoint3D linearStartBreak in
			         GeomTopoOpUtils.GetLinearIntersectionBreaksAtRingStart(
				         source, target, intersectionPoints))
			{
				yield return linearStartBreak;
			}

			// Other linear intersection breaks that are not real (from a 2D perspective)
			foreach (var pseudoBreak in GeomTopoOpUtils.GetLinearIntersectionPseudoBreaks(
				         intersectionPoints))
			{
				yield return pseudoBreak;
			}
		}

		private HashSet<IntersectionPoint3D> DetermineDuplicateIntersections(
			IList<IntersectionPoint3D> intersectionsAlongTarget)
		{
			HashSet<IntersectionPoint3D> result = new HashSet<IntersectionPoint3D>();

			IntersectionPoint3D previous = null;
			foreach (IntersectionPoint3D intersection in intersectionsAlongTarget)
			{
				if (previous != null)
				{
					// ReSharper disable once CompareOfFloatsByEqualityOperator
					if (previous.ReferencesSameTargetVertex(intersection, Target))
					{
						result.Add(previous);
						result.Add(intersection);
					}
				}

				previous = intersection;
			}

			if (intersectionsAlongTarget.Count > 2 && previous != null)
			{
				// Compare last with first

				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (previous.ReferencesSameTargetVertex(intersectionsAlongTarget[0], Target))
				{
					result.Add(previous);
					result.Add(intersectionsAlongTarget[0]);
				}
			}

			return result.Count == 0 ? null : result;
		}
	}
}

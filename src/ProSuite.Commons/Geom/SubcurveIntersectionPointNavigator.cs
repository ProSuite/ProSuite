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

		public SubcurveIntersectionPointNavigator(
			IList<IntersectionPoint3D> intersectionPoints, ISegmentList source, ISegmentList target)
		{
			IntersectionPoints = intersectionPoints;
			Source = source;
			Target = target;
		}

		public ISegmentList Source { get; }

		public ISegmentList Target { get; }

		public IList<IntersectionPoint3D> IntersectionPoints { get; }

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

		public IList<IntersectionPoint3D> IntersectionsNotUsedForNavigation { get; set; } =
			new List<IntersectionPoint3D>();

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

		public void SetStartIntersection(IntersectionPoint3D startIntersection)
		{
			VisitedIntersections.Add(startIntersection);
		}

		public IntersectionPoint3D GetNextIntersection(IntersectionPoint3D previousIntersection,
		                                               bool continueOnSource, bool continueForward)
		{
			IntersectionPoint3D nextIntersection;
			IntersectionPoint3D subcurveStart = previousIntersection;

			do
			{
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

		public HashSet<IntersectionPoint3D> VisitedIntersections { get; } =
			new HashSet<IntersectionPoint3D>();

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

		public IntersectionPoint3D GetNextIntersectionAlongSource(
			IntersectionPoint3D thisIntersection)
		{
			IntersectionPoint3D result = GetNextIntersectionInSourceList(thisIntersection);

			//if (_multiIntersection != null &&
			//    _multiIntersection.Contains(thisIntersection) && _multiIntersection.Contains(result))
			//{
			//	// Skip it
			//	result = GetNextIntersectionInSourceList(result);
			//}

			return result;
		}

		public IntersectionPoint3D GetNextIntersectionAlongTarget(
			IntersectionPoint3D current, bool continueForward)
		{
			var result = GetNextIntersectionInTargetList(current, continueForward);

			//if (_multiIntersection != null &&
			//    _multiIntersection.Contains(current) && _multiIntersection.Contains(result))
			//{
			//	// Skip it
			//	result = GetNextIntersectionInTargetList(result, continueForward);
			//}

			return result;
		}

		private IntersectionPoint3D GetNextIntersectionInSourceList(
			IntersectionPoint3D thisIntersection)
		{
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
				return IntersectionsAlongSource[nextSourceIdx];
			}

			return IntersectionsAlongSource.First(i => i.SourcePartIndex == thisPartIdx);
		}

		public IntersectionPoint3D GetNextIntersectionInTargetList(
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

			return IntersectionsAlongTarget[nextAlongTargetIdx];
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

using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry
{
	public class MultipleRingNavigator : SubcurveNavigator
	{
		private readonly bool _allowTargetTargetIntersections;

		private IList<IntersectionPoint3D> _intersectionPoints;
		private IList<IntersectionPoint3D> _targetTargetIntersectionPoints;

		public MultipleRingNavigator([NotNull] MultiLinestring sourceRings,
		                             [NotNull] MultiLinestring targets,
		                             double tolerance,
		                             bool allowTargetTargetIntersections = false)
			: base(sourceRings, targets, tolerance)
		{
			Assert.ArgumentCondition(sourceRings.IsClosed, "Source rings must be closed.");

			// TODO: Implement target-self-intersecting navigation (vertical, spaghetti, multipatch rings,
			// and especially simplified self-intersecting lines)
			// TODO: Implement Reshape and avoid phantom points
			_allowTargetTargetIntersections = allowTargetTargetIntersections;
		}

		public override IList<IntersectionPoint3D> IntersectionPoints
		{
			get
			{
				if (_intersectionPoints == null)
				{
					_intersectionPoints = new List<IntersectionPoint3D>();

					const bool includeLinearIntersectionIntermediateRingStartEndPoints = true;

					for (int i = 0; i < Source.PartCount; i++)
					{
						Linestring sourceRing = Source.GetPart(i);

						// NOTE: In order to properly process identical rings, there must be
						//       at least some intersection point ->
						// includeLinearIntersectionIntermediateRingStartEndPoints must be true
						var intersectionsForSource =
							GeomTopoOpUtils.GetIntersectionPoints(sourceRing, Target, Tolerance,
							                                      includeLinearIntersectionIntermediateRingStartEndPoints);

						foreach (IntersectionPoint3D intersectionPoint in intersectionsForSource)
						{
							intersectionPoint.SourcePartIndex = i;
							_intersectionPoints.Add(intersectionPoint);
						}
					}
				}

				return _intersectionPoints;
			}
		}

		public IList<IntersectionPoint3D> TargetTargetIntersectionPoints
		{
			get
			{
				if (_targetTargetIntersectionPoints == null)
				{
					if (! _allowTargetTargetIntersections)
					{
						_targetTargetIntersectionPoints = new List<IntersectionPoint3D>(0);
					}
					else
					{
						_targetTargetIntersectionPoints =
							GeomTopoOpUtils.GetSelfIntersections(Target, Tolerance);
					}
				}

				return _targetTargetIntersectionPoints;
			}
		}

		protected override Linestring GetSourcePart(int partIndex)
		{
			return Source.GetPart(partIndex);
		}

		protected override void SetTurnDirection(
			IntersectionPoint3D startIntersection,
			IntersectionPoint3D intersection,
			ref bool alongSource, ref int partIndex, ref bool forward)
		{
			SetTurnDirection(startIntersection, intersection, ref alongSource, ref partIndex,
			                 ref forward, PreferredTurnDirection);
		}

		private void SetTurnDirection(
			IntersectionPoint3D startIntersection,
			IntersectionPoint3D intersection,
			ref bool alongSource, ref int partIndex, ref bool forward,
			TurnDirection preferredDirection)
		{
			// First set the base line, along which we're arriving at the junction:
			Linestring sourceRing = Source.GetPart(intersection.SourcePartIndex);
			Linestring target = Target.GetPart(intersection.TargetPartIndex);

			Line3D entryLine = GetEntryLine(intersection, sourceRing, target,
			                                alongSource, forward);

			double distanceAlongSource;
			int sourceSegmentIdx =
				intersection.GetLocalSourceIntersectionSegmentIdx(sourceRing,
				                                                  out distanceAlongSource);

			Line3D alongSourceLine = distanceAlongSource < 1
				                         ? sourceRing[sourceSegmentIdx]
				                         : sourceRing.NextSegmentInRing(sourceSegmentIdx);

			double? sourceForwardDirection =
				GetDirectionChange(entryLine, alongSourceLine);

			double? targetForwardDirection;
			double? targetBackwardDirection;
			GetAlongTargetDirectionChanges(startIntersection.SourcePartIndex, intersection,
			                               entryLine,
			                               out targetForwardDirection,
			                               out targetBackwardDirection);

			// Order the direction change
			if (true == IsMore(preferredDirection, sourceForwardDirection, targetForwardDirection))
			{
				if (true == IsMore(preferredDirection, sourceForwardDirection,
				                   targetBackwardDirection))
				{
					alongSource = true;
					forward = true;
				}
				else if (true == IsMore(preferredDirection, targetBackwardDirection,
				                        sourceForwardDirection))
				{
					alongSource = false;
					forward = false;
				}
			}
			else if (true == IsMore(preferredDirection, targetForwardDirection,
			                        sourceForwardDirection))
			{
				if (true == IsMore(preferredDirection, targetForwardDirection,
				                   targetBackwardDirection))
				{
					alongSource = false;
					forward = true;
				}
				else if (true == IsMore(preferredDirection, targetBackwardDirection,
				                        targetForwardDirection))
				{
					alongSource = false;
					forward = false;
				}
			}
		}

		[NotNull]
		protected override IntersectionPoint3D FollowUntilNextIntersection(
			IntersectionPoint3D previousIntersection,
			bool continueOnSource,
			int partIndex,
			bool continueForward,
			out Linestring subcurve)
		{
			IntersectionPoint3D nextIntersection;

			if (continueOnSource)
			{
				nextIntersection = GetNextIntersectionAlongSource(previousIntersection);

				subcurve = GetSourceSubcurve(previousIntersection, nextIntersection);
			}
			else
			{
				// TODO: Handle verticals?
				nextIntersection =
					GetNextIntersectionAlongTarget(previousIntersection, continueForward);

				Linestring targetPart = Target.GetPart(previousIntersection.TargetPartIndex);

				subcurve = GetTargetSubcurve(targetPart, previousIntersection,
				                             nextIntersection, continueForward);
			}

			return nextIntersection;
		}

		protected override void RemoveDeadEndIntersections(
			IList<IntersectionPoint3D> intersectionsInboundTarget,
			IList<IntersectionPoint3D> intersectionsOutboundTarget)
		{
			foreach (int sourcePartIdx in _intersectionPoints
			                              .Select(ip => ip.SourcePartIndex).Distinct())
			{
				var firstAlongTarget =
					IntersectionsAlongTarget.FirstOrDefault(
						ip => ip.SourcePartIndex == sourcePartIdx);

				if (firstAlongTarget != null &&
				    intersectionsOutboundTarget.Contains(firstAlongTarget))
				{
					// The first intersection is outbound -> cannot cut, unless this part is closed
					if (! Target.GetPart(firstAlongTarget.TargetPartIndex).IsClosed)
					{
						intersectionsOutboundTarget.Remove(firstAlongTarget);
					}
				}

				var lastAlongTarget =
					IntersectionsAlongTarget.LastOrDefault(
						ip => ip.SourcePartIndex == sourcePartIdx);

				if (lastAlongTarget != null &&
				    intersectionsInboundTarget.Contains(lastAlongTarget))
				{
					// dangle at the end of the cut line: cannot cut, unless the part is closed
					if (! Target.GetPart(lastAlongTarget.TargetPartIndex).IsClosed)
					{
						intersectionsInboundTarget.Remove(lastAlongTarget);
					}
				}
			}
		}

		protected override IntersectionPoint3D GetNextIntersectionAlongSource(
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

		public override IEnumerable<Linestring> GetNonIntersectedSourceRings()
		{
			return GetUnused(Source, IntersectedSourcePartIndexes);
		}

		public override IEnumerable<Linestring> GetNonIntersectedTargets()
		{
			return GetUnused(Target, IntersectedTargetPartIndexes);
		}

		public override bool AreIntersectionPointsNonSequential()
		{
			if (IntersectionPoints.Count < 4)
			{
				return false;
			}

			var alongSourceIndexes = new List<double>();

			foreach (var intersectionPoint in IntersectionsAlongTarget)
			{
				alongSourceIndexes.Add(intersectionPoint.VirtualSourceVertex);
			}

			int startIdx = alongSourceIndexes.IndexOf(alongSourceIndexes.Min());

			int nextIdx = startIdx == IntersectionsAlongSource.Count - 1
				              ? 0
				              : startIdx + 1;

			bool increasing = alongSourceIndexes[nextIdx] > alongSourceIndexes[startIdx];

			int previous = -1;
			for (int i = startIdx; i < startIdx + alongSourceIndexes.Count; i++)
			{
				if (previous < 0)
				{
					previous = i;
					continue;
				}

				int current = i % 4;

				if (increasing && alongSourceIndexes[current] < alongSourceIndexes[previous])
				{
					return true;
				}

				if (! increasing && alongSourceIndexes[current] > alongSourceIndexes[previous])
				{
					return true;
				}

				previous = current;
			}

			return false;
		}

		private void GetAlongTargetDirectionChanges(
			int initialSourcePart,
			[NotNull] IntersectionPoint3D startingAt,
			[NotNull] Line3D entryLine,
			out double? targetForwardDirection,
			out double? targetBackwardDirection)
		{
			targetForwardDirection = null;
			targetBackwardDirection = null;

			if (CanFollowTarget(startingAt, true, initialSourcePart))
			{
				int? forwardSegmentIdx =
					startingAt.GetNonIntersectingTargetSegmentIndex(Target, true);

				if (forwardSegmentIdx != null)
				{
					Line3D targetForward = Target[forwardSegmentIdx.Value];

					targetForwardDirection = GetDirectionChange(entryLine, targetForward);
				}
			}

			if (CanFollowTarget(startingAt, false, initialSourcePart))
			{
				int? backwardSegmentIdx =
					startingAt.GetNonIntersectingTargetSegmentIndex(Target, false);

				if (backwardSegmentIdx != null)
				{
					Line3D targetBackward = Target[backwardSegmentIdx.Value].Clone();
					targetBackward.ReverseOrientation();

					targetBackwardDirection =
						GetDirectionChange(entryLine, targetBackward);
				}
			}
		}

		private bool CanFollowTarget(IntersectionPoint3D startingAt,
		                             bool forward,
		                             int initialSourcePart)
		{
			if (Target.GetPart(startingAt.TargetPartIndex).IsClosed)
			{
				return true;
			}

			if (forward &&
			    ! CanConnectToSourcePartAlongTargetForward(startingAt, initialSourcePart))
			{
				// last intersection along a non-closed target (dangle!), cannot follow
				return false;
			}

			if (! forward &&
			    ! CanConnectToSourcePartAlongTargetBackwards(startingAt, initialSourcePart))
			{
				// first intersection along a non-closed target, cannot follow
				return false;
			}

			return true;
		}

		private bool CanConnectToSourcePartAlongTargetForward(
			[NotNull] IntersectionPoint3D fromIntersection,
			int initialSourcePartIdx)
		{
			int targetIdx = IntersectionOrders[fromIntersection].Value;

			// Any following intersection along the same target part that intersects the required source part?
			while (++targetIdx < IntersectionsAlongTarget.Count)
			{
				IntersectionPoint3D laterIntersection = IntersectionsAlongTarget[targetIdx];

				if (laterIntersection.TargetPartIndex == fromIntersection.TargetPartIndex &&
				    laterIntersection.SourcePartIndex == initialSourcePartIdx)
				{
					return true;
				}
			}

			return false;
		}

		private bool IsLastIntersectionInTargetPart(IntersectionPoint3D intersection,
		                                            int initialSourcePartIdx)
		{
			int targetIdx = IntersectionOrders[intersection].Value;

			// Any following intersection in the same target part that intersects the same source part?
			while (++targetIdx < IntersectionsAlongTarget.Count)
			{
				IntersectionPoint3D laterIntersection = IntersectionsAlongTarget[targetIdx];

				if (laterIntersection.TargetPartIndex == intersection.TargetPartIndex &&
				    laterIntersection.SourcePartIndex == initialSourcePartIdx)
				{
					return false;
				}
			}

			return true;
		}

		private bool CanConnectToSourcePartAlongTargetBackwards(
			IntersectionPoint3D fromIntersection,
			int initialSourcePartIdx)
		{
			int targetIdx = IntersectionOrders[fromIntersection].Value;

			// Any previous intersection in the same part that intersects the same source part?
			while (--targetIdx >= 0)
			{
				IntersectionPoint3D previousIntersection = IntersectionsAlongTarget[targetIdx];

				if (previousIntersection.TargetPartIndex == fromIntersection.TargetPartIndex &&
				    previousIntersection.SourcePartIndex == initialSourcePartIdx)
				{
					return true;
				}
			}

			return false;
		}

		private bool IsFirstIntersectionInTargetPart(IntersectionPoint3D intersection)
		{
			int targetIdx = IntersectionOrders[intersection].Value;

			// Any previous intersection in the same part that intersects the same source part?
			while (--targetIdx >= 0)
			{
				IntersectionPoint3D previousIntersection = IntersectionsAlongTarget[targetIdx];

				if (previousIntersection.TargetPartIndex == intersection.TargetPartIndex &&
				    previousIntersection.SourcePartIndex == intersection.SourcePartIndex)
				{
					return false;
				}
			}

			return true;
		}

		private static IEnumerable<Linestring> GetUnused(ISegmentList linestrings,
		                                                 HashSet<int> usedIndexes)
		{
			for (int i = 0; i < linestrings.PartCount; i++)
			{
				if (usedIndexes.Contains(i))
				{
					continue;
				}

				Linestring cutLine = linestrings.GetPart(i);

				yield return cutLine;
			}
		}

		protected IntersectionPoint3D GetNextIntersectionAlongTarget(
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
			//int currentTargetIdx = IntersectionOrders[current].Value;

			//int nextAlongTargetIdx = (currentTargetIdx + (continueForward ? 1 : -1)) %
			//                         IntersectionsAlongTarget.Count;

			//// TODO: CollectionUtils.GetPreviousInCircularList()
			//if (nextAlongTargetIdx < 0)
			//{
			//	nextAlongTargetIdx += IntersectionsAlongTarget.Count;
			//}

			//IntersectionPoint3D next = IntersectionsAlongTarget[nextAlongTargetIdx];

			//int count = 0;
			//while (next.TargetPartIndex != partIndex)
			//{
			//	next = GetNextIntersectionAlongTarget(next, continueForward, partIndex);
			//	Assert.True(count < IntersectionsAlongTarget.Count,
			//	            "Cannot find next intersection in same target part");
			//}

			return IntersectionsAlongTarget[nextAlongTargetIdx];
		}
	}
}
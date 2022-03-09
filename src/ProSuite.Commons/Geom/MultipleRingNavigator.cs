using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class MultipleRingNavigator : SubcurveNavigator
	{
		private readonly bool _allowTargetTargetIntersections;

		private IList<IntersectionPoint3D> _intersectionPoints;
		private IList<IntersectionPoint3D> _targetTargetIntersectionPoints;

		public MultipleRingNavigator([NotNull] ISegmentList sourceRings,
		                             [NotNull] ISegmentList targets,
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
							GeomTopoOpUtils.GetIntersectionPoints(
								(ISegmentList) sourceRing, Target, Tolerance,
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

		public override SubcurveNavigator Clone()
		{
			var result = new MultipleRingNavigator(Source, Target, Tolerance,
			                                       _allowTargetTargetIntersections)
			             {
				             _intersectionPoints = _intersectionPoints,
				             _targetTargetIntersectionPoints = _targetTargetIntersectionPoints,
				             PreferredTurnDirection = PreferredTurnDirection,
				             PreferTargetZsAtIntersections = PreferTargetZsAtIntersections
			             };

			return result;
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
			IntersectionPoint3D nextIntersection =
				IntersectionPointNavigator.GetNextIntersection(previousIntersection, continueOnSource, continueForward);

			if (continueOnSource)
			{
				subcurve = GetSourceSubcurve(previousIntersection, nextIntersection);
			}
			else
			{
				// TODO: Handle verticals?

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
					IntersectionPointNavigator.IntersectionsAlongTarget.FirstOrDefault(
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
					IntersectionPointNavigator.IntersectionsAlongTarget.LastOrDefault(
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

		public override IEnumerable<Linestring> GetNonIntersectedSourceRings()
		{
			return GetUnused(Source, IntersectedSourcePartIndexes);
		}

		public override IEnumerable<Linestring> GetEqualSourceRings()
		{
			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				// No inbound/outbound, but possibly linear intersection starting/ending in the same point:

				var intersectionPoints = IntersectionPointNavigator.IntersectionsAlongSource
				                         .Where(i => i.SourcePartIndex == unCutSourceIdx)
				                         .ToList();

				if (intersectionPoints.Count == 2 &&
				    intersectionPoints[0].TargetPartIndex ==
				    intersectionPoints[1].TargetPartIndex &&
				    intersectionPoints.Any(
					    i => i.Type == IntersectionPointType.LinearIntersectionStart) &&
				    intersectionPoints.Any(
					    i => i.Type == IntersectionPointType.LinearIntersectionEnd) &&
				    intersectionPoints[0].Point
				                         .EqualsXY(intersectionPoints[1].Point,
				                                   Tolerance))
				{
					yield return Source.GetPart(unCutSourceIdx);
				}
			}
		}

		public override IEnumerable<Linestring> GetUncutSourceRings(bool includeCongruent,
			bool withSameOrientation,
			bool includeContained,
			bool includeNotContained)
		{
			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				// No inbound/outbound, but possibly touching or linear intersections

				bool? isContainedXY = GeomRelationUtils.IsContainedXY(
					Source, Target, Tolerance, IntersectionPointNavigator.IntersectionsAlongSource, unCutSourceIdx);

				if (isContainedXY == null && includeCongruent)
				{
					// congruent
					Linestring sourceRing = Source.GetPart(unCutSourceIdx);

					int targetIndex =
						IntersectionPointNavigator.IntersectionsAlongSource
						                          .Where(
							                          i => i.SourcePartIndex == unCutSourceIdx &&
							                               i.Type == IntersectionPointType.LinearIntersectionStart &&
							                               i.VirtualSourceVertex == 0)
						                          .GroupBy(i => i.TargetPartIndex)
						                          .Single().Key;

					Linestring targetRing = Target.GetPart(targetIndex);

					if (withSameOrientation &&
					    sourceRing.ClockwiseOriented != null &&
					    sourceRing.ClockwiseOriented == targetRing.ClockwiseOriented)
					{
						yield return sourceRing;
					}

					if (! withSameOrientation &&
					    sourceRing.ClockwiseOriented != null &&
					    sourceRing.ClockwiseOriented != targetRing.ClockwiseOriented)
					{
						// The interior of a positive ring is on the left side of a negative ring
						yield return sourceRing;
					}
				}
				else if (isContainedXY == true && includeContained)
				{
					yield return GetSourcePart(unCutSourceIdx);
				}
				else if (isContainedXY == false && includeNotContained)
				{
					yield return GetSourcePart(unCutSourceIdx);
				}
			}
		}

		public override IEnumerable<IntersectionPoint3D> GetEqualRingsSourceStartIntersection()
		{
			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				// No inbound/outbound, but possibly linear intersection starting/ending in the same point:

				var intersectionPoints = IntersectionPointNavigator.IntersectionsAlongSource
				                         .Where(i => i.SourcePartIndex == unCutSourceIdx)
				                         .ToList();

				if (IsSourceCongruentWithTargetXY(intersectionPoints))
				{
					yield return intersectionPoints.First(
						i => i.Type == IntersectionPointType.LinearIntersectionStart);
				}
			}
		}

		public override IEnumerable<Linestring> GetSourceRingsOutsideTarget()
		{
			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				if (false == GeomRelationUtils.IsContainedXY(
					    Source, Target, Tolerance, IntersectionPointNavigator.IntersectionsAlongSource, unCutSourceIdx))
				{
					yield return GetSourcePart(unCutSourceIdx);
				}
			}
		}

		public override IEnumerable<Linestring> GetSourceRingsCompletelyWithinTarget()
		{
			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				// No inbound/outbound, but possibly touching or linear intersections

				if (true == GeomRelationUtils.IsContainedXY(
					    Source, Target, Tolerance, IntersectionPointNavigator.IntersectionsAlongSource, unCutSourceIdx))
				{
					yield return GetSourcePart(unCutSourceIdx);
				}
			}
		}

		/// <summary>
		/// Returns the target rings that are within a source ring but not equal to a
		/// source ring
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<Linestring> GetTargetRingsCompletelyWithinSource()
		{
			foreach (int unCutTargetIdx in GetUnusedIndexes(
				         Target.PartCount, IntersectedTargetPartIndexes))
			{
				// No inbound/outbound, but possibly touching or linear intersections

				Linestring targetRing = Target.GetPart(unCutTargetIdx);

				if (true == GeomRelationUtils.AreaContainsXY(Source, Target, Tolerance,
				                                             IntersectionPointNavigator.IntersectionsAlongTarget,
				                                             unCutTargetIdx))
				{
					yield return targetRing;
				}
			}
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

			foreach (var intersectionPoint in IntersectionPointNavigator.IntersectionsAlongTarget)
			{
				alongSourceIndexes.Add(intersectionPoint.VirtualSourceVertex);
			}

			int startIdx = alongSourceIndexes.IndexOf(alongSourceIndexes.Min());

			int nextIdx = startIdx == IntersectionPointNavigator.IntersectionsAlongSource.Count - 1
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
			int? initialSourcePartForRingResult,
			[NotNull] IntersectionPoint3D startingAt,
			[NotNull] Line3D entryLine,
			out double? targetForwardDirection,
			out double? targetBackwardDirection)
		{
			targetForwardDirection = null;
			targetBackwardDirection = null;

			if (CanFollowTarget(startingAt, true, initialSourcePartForRingResult))
			{
				int? forwardSegmentIdx =
					startingAt.GetNonIntersectingTargetSegmentIndex(Target, true);

				if (forwardSegmentIdx != null)
				{
					Line3D targetForward = Target[forwardSegmentIdx.Value];

					targetForwardDirection = GetDirectionChange(entryLine, targetForward);
				}
			}

			if (CanFollowTarget(startingAt, false, initialSourcePartForRingResult))
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
		                             int? initialSourcePartForRingResult)
		{
			if (initialSourcePartForRingResult == null)
			{
				return true;
			}

			if (Target.GetPart(startingAt.TargetPartIndex).IsClosed)
			{
				return true;
			}

			if (forward &&
			    ! CanConnectToSourcePartAlongTargetForward(startingAt,
			                                               initialSourcePartForRingResult.Value))
			{
				// last intersection along a non-closed target (dangle!), cannot follow
				return false;
			}

			if (! forward &&
			    ! CanConnectToSourcePartAlongTargetBackwards(
				    startingAt, initialSourcePartForRingResult.Value))
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
			int targetIdx = IntersectionPointNavigator.IntersectionOrders[fromIntersection].Value;

			// Any following intersection along the same target part that intersects the required source part?
			while (++targetIdx < IntersectionPointNavigator.IntersectionsAlongTarget.Count)
			{
				IntersectionPoint3D laterIntersection = IntersectionPointNavigator.IntersectionsAlongTarget[targetIdx];

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
			int targetIdx = IntersectionPointNavigator.IntersectionOrders[intersection].Value;

			// Any following intersection in the same target part that intersects the same source part?
			while (++targetIdx < IntersectionPointNavigator.IntersectionsAlongTarget.Count)
			{
				IntersectionPoint3D laterIntersection = IntersectionPointNavigator.IntersectionsAlongTarget[targetIdx];

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
			int targetIdx = IntersectionPointNavigator.IntersectionOrders[fromIntersection].Value;

			// Any previous intersection in the same part that intersects the same source part?
			while (--targetIdx >= 0)
			{
				IntersectionPoint3D previousIntersection = IntersectionPointNavigator.IntersectionsAlongTarget[targetIdx];

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
			int targetIdx = IntersectionPointNavigator.IntersectionOrders[intersection].Value;

			// Any previous intersection in the same part that intersects the same source part?
			while (--targetIdx >= 0)
			{
				IntersectionPoint3D previousIntersection = IntersectionPointNavigator.IntersectionsAlongTarget[targetIdx];

				if (previousIntersection.TargetPartIndex == intersection.TargetPartIndex &&
				    previousIntersection.SourcePartIndex == intersection.SourcePartIndex)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Determines whether the source segments referenced by the specified intersection points
		/// are congruent with the respective target part. Make sure that the specified intersection
		/// points
		/// - reference a specific source-part/target-part combination
		/// - are complete, i.e. include linear intersection breaks at the ring start/end
		/// </summary>
		/// <param name="intersectionPoints"></param>
		/// <returns></returns>
		private static bool IsSourceCongruentWithTargetXY(
			[NotNull] ICollection<IntersectionPoint3D> intersectionPoints)
		{
			IPnt startPoint = null;
			IntersectionPoint3D previous = null;

			if (intersectionPoints.Count == 0)
			{
				return false;
			}

			foreach (IntersectionPoint3D intersectionPoint in
			         intersectionPoints.OrderBy(i => i.VirtualSourceVertex))
			{
				if (startPoint == null)
				{
					if (intersectionPoint.Type != IntersectionPointType.LinearIntersectionStart)
					{
						return false;
					}

					startPoint = intersectionPoint.Point;
				}

				if (previous != null)
				{
					if (previous.Type == IntersectionPointType.LinearIntersectionEnd)
					{
						if (intersectionPoint.Type != IntersectionPointType.LinearIntersectionStart)
						{
							return false;
						}

						if (! previous.Point.Equals(intersectionPoint.Point))
						{
							return false;
						}
					}
					else if (intersectionPoint.Type != IntersectionPointType.LinearIntersectionEnd)
					{
						return false;
					}
				}

				previous = intersectionPoint;
			}

			if (! Assert.NotNull(startPoint).Equals(previous.Point))
			{
				return false;
			}

			return true;
		}

		private static IEnumerable<Linestring> GetUnused(ISegmentList linestrings,
		                                                 HashSet<int> usedIndexes)
		{
			foreach (int i in GetUnusedIndexes(linestrings.PartCount, usedIndexes))
			{
				Linestring cutLine = linestrings.GetPart(i);

				yield return cutLine;
			}
		}

		private static IEnumerable<int> GetUnusedIndexes(int partCount,
		                                                 HashSet<int> usedIndexes)
		{
			for (int i = 0; i < partCount; i++)
			{
				if (usedIndexes.Contains(i))
				{
					continue;
				}

				yield return i;
			}
		}
		
		private static IEnumerable<int> GetIntersectingTargetPartIndexes(
			IEnumerable<IntersectionPoint3D> intersectionPoints)
		{
			return intersectionPoints.GroupBy(i => i.TargetPartIndex)
			                         .Select(i => i.First().TargetPartIndex);
		}
	}
}

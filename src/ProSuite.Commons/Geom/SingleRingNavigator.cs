using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Geom
{
	public class SingleRingNavigator : SubcurveNavigator
	{
		private readonly Linestring _sourceRing;
		private readonly Linestring _target;

		private IList<IntersectionPoint3D> _intersectionPoints;

		public SingleRingNavigator([NotNull] Linestring sourceRing,
		                           [NotNull] Linestring target,
		                           double tolerance)
			: base(sourceRing, target, tolerance)
		{
			Assert.ArgumentCondition(sourceRing.IsClosed, "Source ring must be closed.");

			_sourceRing = sourceRing;
			_target = target;
		}

		public override IList<IntersectionPoint3D> IntersectionPoints
		{
			get
			{
				if (_intersectionPoints == null)
				{
					if (_target.SpatialIndex == null)
					{
						_target.SpatialIndex =
							SpatialHashSearcher<int>.CreateSpatialSearcher(_target);
					}

					// NOTE: In order to properly process identical rings, there must be
					//       at least some intersection point ->
					// includeLinearIntersectionIntermediateRingStartEndPoints must be true
					_intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) _sourceRing, (ISegmentList) _target, Tolerance);
				}

				return _intersectionPoints;
			}
		}

		public override SubcurveNavigator Clone()
		{
			var result = new SingleRingNavigator(_sourceRing, _target, Tolerance);

			result._intersectionPoints = _intersectionPoints;

			result.PreferredTurnDirection = PreferredTurnDirection;
			result.PreferTargetZsAtIntersections = PreferTargetZsAtIntersections;

			return result;
		}

		protected override Linestring GetSourcePart(int partIndex)
		{
			return _sourceRing;
		}

		protected override void SetTurnDirection(
			IntersectionPoint3D startIntersection,
			IntersectionPoint3D intersection,
			ref bool alongSource, ref int partIndex, ref bool forward)
		{
			Assert.AreEqual(TurnDirection.Right, PreferredTurnDirection,
			                "Unsupported turn direction for single ring navigator");

			// First set the base line, along which we're arriving at the junction:
			double distanceAlongSource;
			int sourceSegmentIdx =
				intersection.GetLocalSourceIntersectionSegmentIdx(_sourceRing,
					out distanceAlongSource);
			Line3D entryLine =
				GetEntryLine(intersection, _sourceRing, _target, alongSource, forward);

			Line3D alongSourceLine = distanceAlongSource < 1
				                         ? _sourceRing[sourceSegmentIdx]
				                         : _sourceRing.NextSegmentInRing(sourceSegmentIdx);

			double? sourceForwardDirection =
				GetDirectionChange(entryLine, alongSourceLine);

			double? targetForwardDirection;
			double? targetBackwardDirection;
			GetAlongTargetDirectionChanges(intersection, entryLine,
			                               out targetForwardDirection,
			                               out targetBackwardDirection);

			// Order the direction change: the largest is the right-most
			if (IsMoreRight(sourceForwardDirection, targetForwardDirection))
			{
				if (IsMoreRight(sourceForwardDirection, targetBackwardDirection))
				{
					alongSource = true;
					forward = true;
				}
				else if (IsMoreRight(targetBackwardDirection, sourceForwardDirection))
				{
					alongSource = false;
					forward = false;
				}
			}
			else if (IsMoreRight(targetForwardDirection, sourceForwardDirection))
			{
				if (IsMoreRight(targetForwardDirection, targetBackwardDirection))
				{
					alongSource = false;
					forward = true;
				}
				else if (IsMoreRight(targetBackwardDirection, targetForwardDirection))
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
				int previousTargetIdx = IntersectionOrders[previousIntersection].Value;

				// If there are vertical rings, there can be 2 intersections at the exact same target distance
				int otherDirectionIdx = GetNextAlongTargetIdx(
					previousTargetIdx, ! continueForward,
					IntersectionsAlongTarget);

				double epsilon = MathUtils.GetDoubleSignificanceEpsilon(
					previousIntersection.Point.X,
					previousIntersection.Point.Y);

				int nextAlongTargetIdx;
				if (MathUtils.AreEqual(
					IntersectionsAlongTarget[otherDirectionIdx].VirtualTargetVertex,
					IntersectionsAlongTarget[previousTargetIdx].VirtualTargetVertex,
					epsilon))
				{
					// vertical ring, 2 intersections at same XY location
					nextAlongTargetIdx = otherDirectionIdx;
				}
				else
				{
					nextAlongTargetIdx = GetNextAlongTargetIdx(
						previousTargetIdx, continueForward,
						IntersectionsAlongTarget);
				}

				nextIntersection = IntersectionsAlongTarget[nextAlongTargetIdx];

				subcurve = GetTargetSubcurve(_target, previousIntersection,
				                             nextIntersection, continueForward);
			}

			return nextIntersection;
		}

		protected override IntersectionPoint3D GetNextIntersectionAlongSource(
			IntersectionPoint3D thisIntersection)
		{
			int previousSourceIdx = IntersectionOrders[thisIntersection].Key;
			int nextAlongSourceIdx = (previousSourceIdx + 1) % IntersectionsAlongSource.Count;

			IntersectionPoint3D nextIntersection = IntersectionsAlongSource[nextAlongSourceIdx];

			return nextIntersection;
		}

		public override IEnumerable<Linestring> GetNonIntersectedSourceRings()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<Linestring> GetNonIntersectedTargets()
		{
			throw new NotImplementedException();
		}

		public override bool AreIntersectionPointsNonSequential()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<Linestring> GetEqualSourceRings()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<IntersectionPoint3D> GetEqualRingsSourceStartIntersection()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<Linestring> GetSourceRingsCompletelyWithinTarget()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<Linestring> GetTargetRingsCompletelyWithinSource()
		{
			throw new NotImplementedException();
		}

		private void GetAlongTargetDirectionChanges(
			[NotNull] IntersectionPoint3D intersection,
			[NotNull] Line3D entryLine,
			out double? targetForwardDirection,
			out double? targetBackwardDirection)
		{
			targetForwardDirection = null;
			targetBackwardDirection = null;

			if (CanFollowTarget(intersection, true))
			{
				int? forwardSegmentIdx =
					intersection.GetNonIntersectingTargetSegmentIndex(_target, true);

				if (forwardSegmentIdx != null)
				{
					Line3D targetForward = _target[forwardSegmentIdx.Value];

					targetForwardDirection = GetDirectionChange(entryLine, targetForward);
				}
			}

			if (CanFollowTarget(intersection, false))
			{
				int? backwardSegmentIdx =
					intersection.GetNonIntersectingTargetSegmentIndex(_target, false);

				if (backwardSegmentIdx != null)
				{
					Line3D targetBackward = _target[backwardSegmentIdx.Value].Clone();
					targetBackward.ReverseOrientation();

					targetBackwardDirection =
						GetDirectionChange(entryLine, targetBackward);
				}
			}
		}

		private bool CanFollowTarget(IntersectionPoint3D startingAt, bool forward)
		{
			if (_target.IsClosed)
			{
				return true;
			}

			int targetIdx = IntersectionOrders[startingAt].Value;

			if (forward && targetIdx == IntersectionsAlongTarget.Count - 1)
			{
				// last intersection along a non-closed target (dangle!), cannot follow
				return false;
			}

			if (! forward && targetIdx == 0)
			{
				// first intersection along a non-closed target, cannot follow
				return false;
			}

			return true;
		}

		protected IntersectionPoint3D GetNextIntersectionAlongTarget(
			IntersectionPoint3D current, bool continueForward)
		{
			int currentTargetIdx = IntersectionOrders[current].Value;

			int nextAlongTargetIdx = (currentTargetIdx + (continueForward ? 1 : -1)) %
			                         IntersectionsAlongTarget.Count;

			// TODO: CollectionUtils.GetPreviousInCircularList()
			if (nextAlongTargetIdx < 0)
			{
				nextAlongTargetIdx += IntersectionsAlongTarget.Count;
			}

			return IntersectionsAlongTarget[nextAlongTargetIdx];
		}

		private static int GetNextAlongTargetIdx(
			int currentTargetIdx,
			bool continueForward,
			[NotNull] ICollection<IntersectionPoint3D> intersectionsAlongTarget)
		{
			int nextAlongTargetIdx = (currentTargetIdx + (continueForward ? 1 : -1)) %
			                         intersectionsAlongTarget.Count;

			// TODO: CollectionUtils.GetPreviousInCircularList()
			if (nextAlongTargetIdx < 0)
			{
				nextAlongTargetIdx += intersectionsAlongTarget.Count;
			}

			return nextAlongTargetIdx;
		}

		private static Linestring GetSourceSubcurve(
			[NotNull] Linestring source,
			[NotNull] IntersectionPoint3D fromIntersection,
			[NotNull] IntersectionPoint3D toIntersection)
		{
			double fromDistanceAlongAsRatio;
			int fromIndex = fromIntersection.GetLocalSourceIntersectionSegmentIdx(
				source, out fromDistanceAlongAsRatio);

			double toDistanceAlongAsRatio;
			int toIndex = toIntersection.GetLocalSourceIntersectionSegmentIdx(
				source, out toDistanceAlongAsRatio);

			Linestring subcurve = source.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false);

			return subcurve;
		}
	}
}

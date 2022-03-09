using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class SubcurveNavigator
	{
		private SubcurveIntersectionPointNavigator _intersectionPointNavigator;
		private readonly bool _allowTargetTargetIntersections;
		private IList<IntersectionPoint3D> _intersectionPoints;
		private IList<IntersectionPoint3D> _targetTargetIntersectionPoints;

		public SubcurveNavigator([NotNull] ISegmentList sourceRings,
		                         [NotNull] ISegmentList targets,
		                         double tolerance,
		                         bool allowTargetTargetIntersections)
			: this(sourceRings, targets, tolerance)
		{
			Assert.ArgumentCondition(sourceRings.IsClosed, "Source rings must be closed.");

			// TODO: Implement target-self-intersecting navigation (vertical, spaghetti, multipatch rings,
			// and especially simplified self-intersecting lines)
			// TODO: Implement Reshape and avoid phantom points
			_allowTargetTargetIntersections = allowTargetTargetIntersections;
		}

		public SubcurveNavigator(ISegmentList source,
		                         ISegmentList target,
		                         double tolerance)
		{
			Source = source;
			Target = target;
			Tolerance = tolerance;
		}

		/// <summary>
		/// Whether the cut rings should get the target's Z values at the intersection points.
		/// </summary>
		public bool PreferTargetZsAtIntersections { get; set; }

		public ISegmentList Source { get; }

		public ISegmentList Target { get; }

		public double Tolerance { get; }

		private HashSet<int> IntersectedSourcePartIndexes { get; } = new HashSet<int>();
		private HashSet<int> IntersectedTargetPartIndexes { get; } = new HashSet<int>();

		private HashSet<IntersectionPoint3D> VisitedOutboundTarget { get; } =
			new HashSet<IntersectionPoint3D>();

		private HashSet<IntersectionPoint3D> VisitedInboundTarget { get; } =
			new HashSet<IntersectionPoint3D>();

		/// <summary>
		/// Intersections at which the target 'arrives' at the source ring boundary
		/// from the inside. The target linestring does not necessarily have to continue
		/// to the outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> IntersectionsOutboundTarget =>
			Assert.NotNull(IntersectionPointNavigator.IntersectionsOutboundTarget);

		/// <summary>
		/// Intersections at which the target 'departs' from the source ring boundary
		/// to the inside. The target linestring does not necessarily have to arrive
		/// from the outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> IntersectionsInboundTarget =>
			Assert.NotNull(IntersectionPointNavigator.IntersectionsInboundTarget);

		// TODO: Once the SingleRingNavigator is removed, this could be a parameter of
		//       SetTurnDirection()
		internal TurnDirection PreferredTurnDirection { get; set; } = TurnDirection.Right;

		public SubcurveIntersectionPointNavigator IntersectionPointNavigator
		{
			get
			{
				if (_intersectionPointNavigator == null)
				{
					_intersectionPointNavigator =
						new SubcurveIntersectionPointNavigator(IntersectionPoints, Source, Target);
				}

				return _intersectionPointNavigator;
			}
		}

		public IList<IntersectionPoint3D> IntersectionPoints
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

		/// <summary>
		/// Moves from one intersection to the next by
		/// - first following the source
		/// - at each intersection taking the right-most (alternatively, the lef-most, depending
		/// on <see cref="PreferredTurnDirection"/>) turn until reaching the start again.
		/// </summary>
		/// <param name="startIntersections"></param>
		/// <returns></returns>
		public IList<Linestring> FollowSubcurvesClockwise(
			[NotNull] ICollection<IntersectionPoint3D> startIntersections)
		{
			Assert.ArgumentCondition(Source.IsClosed, "Source ring(s) must be closed.");

			IList<Linestring> result = new List<Linestring>();
			var subcurveInfos = new List<IntersectionRun>();

			while (startIntersections.Count > 0)
			{
				subcurveInfos.Clear();
				bool onlyFollowingSource = true;

				IntersectionPoint3D startIntersection = startIntersections.First();
				startIntersections.Remove(startIntersection);

				Pnt3D ringStart = null;
				foreach (IntersectionRun next in NavigateSubcurves(startIntersection))
				{
					subcurveInfos.Add(next);

					if (next.ContainsSourceStart(out Pnt3D startPoint))
					{
						ringStart = startPoint;
					}

					if (! next.ContinuingOnSource)
					{
						onlyFollowingSource = false;
					}
				}

				// At some point the result must deviate from source otherwise the target does not cut it
				if (! onlyFollowingSource)
				{
					// Finish ring
					result.Add(GeomTopoOpUtils.MergeConnectedLinestrings(
						           subcurveInfos.Select(i => i.Subcurve).ToList(), ringStart,
						           Tolerance));

					foreach (int sourceIdx in subcurveInfos.Select(
						         i => i.NextIntersection.SourcePartIndex))
					{
						IntersectedSourcePartIndexes.Add(sourceIdx);
					}

					foreach (int targetIdx in subcurveInfos.Select(
						         i => i.NextIntersection.TargetPartIndex))
					{
						IntersectedTargetPartIndexes.Add(targetIdx);
					}
				}
			}

			return result;
		}

		public SubcurveNavigator Clone()
		{
			var result = new SubcurveNavigator(Source, Target, Tolerance,
			                                   _allowTargetTargetIntersections)
			             {
				             _intersectionPoints = _intersectionPoints,
				             _targetTargetIntersectionPoints = _targetTargetIntersectionPoints,
				             PreferredTurnDirection = PreferredTurnDirection,
				             PreferTargetZsAtIntersections = PreferTargetZsAtIntersections
			             };

			return result;
		}

		public IEnumerable<Linestring> GetNonIntersectedSourceRings()
		{
			return GetUnused(Source, IntersectedSourcePartIndexes);
		}

		public IEnumerable<Linestring> GetUncutSourceRings(bool includeCongruent,
		                                                   bool withSameOrientation,
		                                                   bool includeContained,
		                                                   bool includeNotContained)
		{
			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				// No inbound/outbound, but possibly touching or linear intersections

				bool? isContainedXY = GeomRelationUtils.IsContainedXY(
					Source, Target, Tolerance, IntersectionPointNavigator.IntersectionsAlongSource,
					unCutSourceIdx);

				if (isContainedXY == null && includeCongruent)
				{
					// congruent
					Linestring sourceRing = Source.GetPart(unCutSourceIdx);

					int targetIndex =
						IntersectionPointNavigator.IntersectionsAlongSource
						                          .Where(
							                          i => i.SourcePartIndex == unCutSourceIdx &&
							                               i.Type == IntersectionPointType
								                               .LinearIntersectionStart &&
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

		/// <summary>
		/// Returns the target rings that are within a source ring but not equal to a
		/// source ring
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Linestring> GetTargetRingsCompletelyWithinSource()
		{
			foreach (int unCutTargetIdx in GetUnusedIndexes(
				         Target.PartCount, IntersectedTargetPartIndexes))
			{
				// No inbound/outbound, but possibly touching or linear intersections

				Linestring targetRing = Target.GetPart(unCutTargetIdx);

				if (true == GeomRelationUtils.AreaContainsXY(Source, Target, Tolerance,
				                                             IntersectionPointNavigator
					                                             .IntersectionsAlongTarget,
				                                             unCutTargetIdx))
				{
					yield return targetRing;
				}
			}
		}

		public IEnumerable<Linestring> GetNonIntersectedTargets()
		{
			return GetUnused(Target, IntersectedTargetPartIndexes);
		}

		public bool AreIntersectionPointsNonSequential()
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

		private IEnumerable<IntersectionRun> NavigateSubcurves(
			IntersectionPoint3D startIntersection)
		{
			if (VisitedOutboundTarget.Contains(startIntersection) ||
			    VisitedInboundTarget.Contains(startIntersection))
			{
				yield break;
			}

			IntersectionPoint3D previousIntersection = startIntersection;
			IntersectionPoint3D nextIntersection = null;

			IntersectionPointNavigator.SetStartIntersection(startIntersection);

			int count = 0;
			// Start by following the source:
			bool continueOnSource = true;
			bool forward = true;
			int partIndex = startIntersection.SourcePartIndex;
			while (nextIntersection == null ||
			       ! nextIntersection.Point.Equals(startIntersection.Point))
			{
				Assert.True(count++ <= IntersectionPoints.Count,
				            "Intersections seen twice. Make sure there are no self intersections of the target.");

				if (nextIntersection != null)
				{
					// Determine if at the next intersection we must
					// - continue along the source (e.g. because the source touches from the inside)
					// - continue along the target (forward or backward)
					SetTurnDirection(startIntersection, previousIntersection,
					                 ref continueOnSource, ref forward);
				}

				nextIntersection = FollowUntilNextIntersection(
					previousIntersection, continueOnSource, partIndex, forward,
					out Linestring subcurve);

				Pnt3D containedSourceStart =
					GetSourceStartBetween(previousIntersection, nextIntersection, continueOnSource,
					                      forward);

				if (continueOnSource)
				{
					// Remove, if we follow the source through an intersection from other start.
					// This happens with vertical rings and multiple targets.
					if (IntersectionsOutboundTarget.Contains(previousIntersection))
					{
						// TODO: if the current startIntersection is inbound make sure the left/right assignment is cleared!
						VisitedOutboundTarget.Add(previousIntersection);
					}
					else if (IntersectionsInboundTarget.Contains(previousIntersection))
					{
						// TODO: if the current startIntersection is outbound make sure the left/right assignment is cleared!
						VisitedInboundTarget.Add(previousIntersection);
					}
				}

				IntersectionRun next =
					new IntersectionRun(nextIntersection, subcurve, containedSourceStart)
					{
						ContinuingOnSource = continueOnSource
					};

				yield return next;

				previousIntersection = nextIntersection;
			}
		}

		private void SetTurnDirection(
			IntersectionPoint3D startIntersection,
			IntersectionPoint3D intersection,
			ref bool alongSource, ref bool forward)
		{
			TurnDirection preferredDirection = PreferredTurnDirection;
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

			GetAlongTargetDirectionChanges(startIntersection.SourcePartIndex, intersection,
			                               entryLine,
			                               out double? targetForwardDirection,
			                               out double? targetBackwardDirection);

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

		/// <summary>
		///   Gets the line that enters the intersection point when moving along as specified
		///   with the direction parameters alongSource and forward.
		/// </summary>
		/// <param name="intoIntersection"></param>
		/// <param name="target"></param>
		/// <param name="alongSource"></param>
		/// <param name="forward"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		private static Line3D GetEntryLine([NotNull] IntersectionPoint3D intoIntersection,
		                                   [NotNull] Linestring source,
		                                   [NotNull] Linestring target,
		                                   bool alongSource, bool forward)
		{
			Line3D entryLine;

			if (alongSource)
			{
				double distanceAlongSource;
				int sourceSegmentIdx =
					intoIntersection.GetLocalSourceIntersectionSegmentIdx(
						source, out distanceAlongSource);

				entryLine = distanceAlongSource > 0
					            ? source[sourceSegmentIdx]
					            : source.PreviousSegmentInRing(sourceSegmentIdx);
			}
			else
			{
				double distanceAlongTarget;
				int targetSegmentIdx = intoIntersection.GetLocalTargetIntersectionSegmentIdx(
					target, out distanceAlongTarget);

				if (forward)
				{
					if (distanceAlongTarget > 0)
					{
						entryLine = target[targetSegmentIdx];
					}
					else
					{
						// There must be a previous segment if we have come along the target
						int previousTargetIdx =
							Assert.NotNull(target.PreviousSegmentIndex(targetSegmentIdx)).Value;

						entryLine = target[previousTargetIdx];
					}
				}
				else
				{
					if (distanceAlongTarget < 1)
					{
						entryLine = target[targetSegmentIdx];
					}
					else
					{
						// There must be a next segment if we have come backwards along the target
						int nextTargetIdx =
							Assert.NotNull(target.NextSegmentIndex(targetSegmentIdx)).Value;

						entryLine = target[nextTargetIdx];
					}

					entryLine = entryLine.Clone();
					entryLine.ReverseOrientation();
				}
			}

			return entryLine;
		}

		private static double? GetDirectionChange(Line3D baseLine, Line3D compareLine)
		{
			double angleDifference = baseLine.GetDirectionAngleXY() -
			                         compareLine.GetDirectionAngleXY();

			// Normalize to -PI .. +PI
			if (angleDifference <= -Math.PI)
			{
				angleDifference += 2 * Math.PI;
			}
			else if (angleDifference > Math.PI)
			{
				angleDifference -= 2 * Math.PI;
			}

			// exclude 180-degree turns
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(baseLine.XMax);

			if (MathUtils.AreEqual(angleDifference, -Math.PI, epsilon) ||
			    MathUtils.AreEqual(angleDifference, Math.PI, epsilon))
			{
				return null;
			}

			return angleDifference;
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
			    ! IntersectionPointNavigator.CanConnectToSourcePartAlongTargetForward(startingAt,
				    initialSourcePartForRingResult.Value))
			{
				// last intersection along a non-closed target (dangle!), cannot follow
				return false;
			}

			if (! forward &&
			    ! IntersectionPointNavigator.CanConnectToSourcePartAlongTargetBackwards(
				    startingAt, initialSourcePartForRingResult.Value))
			{
				// first intersection along a non-closed target, cannot follow
				return false;
			}

			return true;
		}

		[NotNull]
		private IntersectionPoint3D FollowUntilNextIntersection(
			IntersectionPoint3D previousIntersection,
			bool continueOnSource,
			int partIndex,
			bool continueForward,
			out Linestring subcurve)
		{
			IntersectionPoint3D nextIntersection =
				IntersectionPointNavigator.GetNextIntersection(
					previousIntersection, continueOnSource, continueForward);

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

		private Pnt3D GetSourceStartBetween([NotNull] IntersectionPoint3D previousIntersection,
		                                    [NotNull] IntersectionPoint3D nextIntersection,
		                                    bool continueOnSource,
		                                    bool forward)
		{
			if (! continueOnSource)
			{
				return null;
			}

			Assert.True(forward, "Continuation on source backward is not allowed!");

			if (previousIntersection.SourcePartIndex != nextIntersection.SourcePartIndex)
			{
				return null;
			}

			Pnt3D containedSourceStart = null;

			if (MathUtils.AreEqual(previousIntersection.VirtualSourceVertex, 0) ||
			    previousIntersection.VirtualSourceVertex > nextIntersection.VirtualSourceVertex)
			{
				Linestring sourcePart = GetSourcePart(previousIntersection.SourcePartIndex);
				containedSourceStart = sourcePart.StartPoint;
			}

			return containedSourceStart;
		}

		/// <summary>
		/// Determines whether the directionChange1 angle points more towards the required
		/// direction than directionChange2 angle.
		/// </summary>
		/// <param name="towards"></param>
		/// <param name="directionChange1"></param>
		/// <param name="directionChange2"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		private static bool? IsMore(TurnDirection towards,
		                            double? directionChange1,
		                            double? directionChange2,
		                            double tolerance = double.Epsilon)
		{
			if (directionChange1 == null)
			{
				return false;
			}

			if (directionChange2 == null)
			{
				return true;
			}

			if (towards == TurnDirection.Right && directionChange1.Value > directionChange2.Value)
			{
				return true;
			}

			if (towards == TurnDirection.Left && directionChange1.Value < directionChange2.Value)
			{
				return true;
			}

			if (MathUtils.AreEqual(directionChange1.Value, directionChange2.Value, tolerance))
			{
				return null;
			}

			return false;
		}

		private Linestring GetSourceSubcurve(
			[NotNull] IntersectionPoint3D fromIntersection,
			[NotNull] IntersectionPoint3D toIntersection)
		{
			Assert.ArgumentCondition(
				fromIntersection.SourcePartIndex == toIntersection.SourcePartIndex,
				"Cannot jump between source parts");

			Linestring source = GetSourcePart(fromIntersection.SourcePartIndex);

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

			if (PreferTargetZsAtIntersections)
			{
				Pnt3D startPoint = subcurve.StartPoint.ClonePnt3D();
				Pnt3D endPoint = subcurve.EndPoint.ClonePnt3D();

				PreferTargetZ(fromIntersection, startPoint);
				PreferTargetZ(toIntersection, endPoint);

				subcurve.ReplacePoint(0, startPoint);
				subcurve.ReplacePoint(subcurve.SegmentCount, endPoint);
			}

			return subcurve;
		}

		private Linestring GetTargetSubcurve(
			[NotNull] Linestring target,
			[NotNull] IntersectionPoint3D fromIntersection,
			[NotNull] IntersectionPoint3D toIntersection,
			bool forward)
		{
			double fromDistanceAlongAsRatio;
			int fromIndex = fromIntersection.GetLocalTargetIntersectionSegmentIdx(
				target, out fromDistanceAlongAsRatio);

			double toDistanceAlongAsRatio;
			int toIndex = toIntersection.GetLocalTargetIntersectionSegmentIdx(
				target, out toDistanceAlongAsRatio);

			if (! forward &&
			    fromIntersection.VirtualTargetVertex > toIntersection.VirtualTargetVertex) { }

			Linestring subcurve = target.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false, ! forward);

			// Replace the start / end with the actual intersection (correct source Z, exactly matching previous subcurve end)
			Pnt3D startPoint = fromIntersection.Point.ClonePnt3D();
			Pnt3D endPoint = toIntersection.Point.ClonePnt3D();

			// But set the preferred Z from the target, if desired:
			if (PreferTargetZsAtIntersections)
			{
				PreferTargetZ(fromIntersection, startPoint);
				PreferTargetZ(toIntersection, endPoint);
			}

			subcurve.ReplacePoint(0, startPoint);
			subcurve.ReplacePoint(subcurve.SegmentCount, endPoint);

			return subcurve;
		}

		private Linestring GetSourcePart(int partIndex)
		{
			return Source.GetPart(partIndex);
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

		private void PreferTargetZ(IntersectionPoint3D atIntersection, Pnt3D resultPoint)
		{
			Pnt3D targetPointAtFrom = atIntersection.GetTargetPoint(Target);

			if (! double.IsNaN(targetPointAtFrom.Z))
			{
				resultPoint.Z = targetPointAtFrom.Z;
			}
		}

		private class IntersectionRun
		{
			private readonly Pnt3D _includedRingStartPoint;

			public IntersectionRun(IntersectionPoint3D nextIntersection,
			                       Linestring subcurve,
			                       Pnt3D includedRingStartPoint)
			{
				_includedRingStartPoint = includedRingStartPoint;
				NextIntersection = nextIntersection;
				Subcurve = subcurve;
			}

			public IntersectionPoint3D NextIntersection { get; }
			public Linestring Subcurve { get; }
			public bool ContinuingOnSource { get; set; }

			public bool ContainsSourceStart(out Pnt3D startPoint)
			{
				if (_includedRingStartPoint != null)
				{
					startPoint = _includedRingStartPoint;

					return true;
				}

				startPoint = null;
				return false;
			}
		}

		internal enum TurnDirection
		{
			Left,
			Right
		}
	}
}

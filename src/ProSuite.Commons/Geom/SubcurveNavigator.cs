using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public abstract class SubcurveNavigator
	{
		private IList<IntersectionPoint3D> _intersectionsInboundTarget;
		private IList<IntersectionPoint3D> _intersectionsOutboundTarget;
		private SubcurveIntersectionPointNavigator _intersectionPointNavigator;

		protected SubcurveNavigator(ISegmentList source,
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

		/// <summary>
		/// All intersection points between the source and the target.
		/// </summary>
		public abstract IList<IntersectionPoint3D> IntersectionPoints { get; }
		
		protected HashSet<int> IntersectedSourcePartIndexes { get; } = new HashSet<int>();
		protected HashSet<int> IntersectedTargetPartIndexes { get; } = new HashSet<int>();

		private HashSet<IntersectionPoint3D> VisitedOutboundTarget { get; } =
			new HashSet<IntersectionPoint3D>();

		private HashSet<IntersectionPoint3D> VisitedInboundTarget { get; } =
			new HashSet<IntersectionPoint3D>();

		/// <summary>
		/// Intersections at which the target 'arrives' at the source ring boundary
		/// from the inside. The target linestring does not necessarily have to continue
		/// to the outside.
		/// </summary>
		public virtual IEnumerable<IntersectionPoint3D> IntersectionsOutboundTarget
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
		public virtual IEnumerable<IntersectionPoint3D> IntersectionsInboundTarget
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
					                 ref continueOnSource, ref partIndex, ref forward);
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

		public abstract SubcurveNavigator Clone();

		protected abstract Linestring GetSourcePart(int partIndex);

		protected abstract void SetTurnDirection(
			[NotNull] IntersectionPoint3D startIntersection,
			[NotNull] IntersectionPoint3D intersection,
			ref bool alongSource, ref int partIndex, ref bool forward);

		protected abstract IntersectionPoint3D FollowUntilNextIntersection(
			[NotNull] IntersectionPoint3D previousIntersection,
			bool continueOnSource,
			int partIndex,
			bool continueForward,
			out Linestring subcurve);

		/// <summary>
		/// Removes the inbound/outbound target intersections that would allow going into a dead-end.
		/// This analysis has to be performed on a per-source-ring basis.
		/// </summary>
		/// <param name="intersectionsInboundTarget"></param>
		/// <param name="intersectionsOutboundTarget"></param>
		protected virtual void RemoveDeadEndIntersections(
			IList<IntersectionPoint3D> intersectionsInboundTarget,
			IList<IntersectionPoint3D> intersectionsOutboundTarget)
		{
			var firstAlongTarget = IntersectionPointNavigator.IntersectionsAlongTarget.FirstOrDefault();

			if (firstAlongTarget != null &&
			    intersectionsOutboundTarget.Contains(firstAlongTarget))
			{
				intersectionsOutboundTarget.Remove(firstAlongTarget);
			}

			var lastAlongTarget = IntersectionPointNavigator.IntersectionsAlongTarget.LastOrDefault();

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

		private static Dictionary<IntersectionPoint3D, KeyValuePair<int, int>>
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
		protected static Line3D GetEntryLine([NotNull] IntersectionPoint3D intoIntersection,
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

		protected static bool IsMoreRight(double? directionChange1,
		                                  double? directionChange2)
		{
			if (directionChange1 == null)
			{
				return false;
			}

			if (directionChange2 == null)
			{
				return true;
			}

			return directionChange1.Value > directionChange2.Value;
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
		internal static bool? IsMore(TurnDirection towards,
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

		protected static double? GetDirectionChange(Line3D baseLine, Line3D compareLine)
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

		protected Linestring GetSourceSubcurve(
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

		protected Linestring GetTargetSubcurve(
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

		public abstract IEnumerable<Linestring> GetNonIntersectedSourceRings();

		public abstract IEnumerable<Linestring> GetNonIntersectedTargets();

		public abstract bool AreIntersectionPointsNonSequential();

		public abstract IEnumerable<Linestring> GetEqualSourceRings();

		public abstract IEnumerable<IntersectionPoint3D> GetEqualRingsSourceStartIntersection();

		/// <summary>
		/// Gets the source rings that are completely within the target geometry. Source
		/// rings that are equal to a target ring are excluded.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<Linestring> GetSourceRingsCompletelyWithinTarget();

		public abstract IEnumerable<Linestring> GetTargetRingsCompletelyWithinSource();

		private void ClassifyIntersections(
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			[NotNull] out IList<IntersectionPoint3D> intersectionsInboundTarget,
			[NotNull] out IList<IntersectionPoint3D> intersectionsOutboundTarget)
		{
			intersectionsInboundTarget = new List<IntersectionPoint3D>();
			intersectionsOutboundTarget = new List<IntersectionPoint3D>();
			IntersectionPointNavigator.IntersectionsNotUsedForNavigation.Clear();

			// Filter all non-real linear intersections (i. e. those where no deviation between
			// source and target exists. This is important to avoid incorrect inbound/outbound
			// and turn-direction decisions because the two lines continue (almost at the same
			// angle.
			var usableIntersections = IntersectionPointNavigator.IntersectionsAlongSource.ToList();

			foreach (IntersectionPoint3D unusable in GetIntersectionsNotUsedForNavigation(
				         IntersectionPointNavigator.IntersectionsAlongSource, Source, Target))
			{
				usableIntersections.Remove(unusable);
				IntersectionPointNavigator.IntersectionsNotUsedForNavigation.Add(unusable);
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

		public abstract IEnumerable<Linestring> GetSourceRingsOutsideTarget();

		public abstract IEnumerable<Linestring> GetUncutSourceRings(bool includeCongruent,
			bool withSameOrientation,
			bool includeContained,
			bool includeNotContained);

	}
}

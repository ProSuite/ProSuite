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
		private readonly IList<IntersectionRun> _congruentRings = new List<IntersectionRun>(0);

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

		public SubcurveNavigator([NotNull] ISegmentList source,
		                         [NotNull] ISegmentList target,
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

		private HashSet<IntersectionPoint3D> VisitedIntersectionsAlongSource { get; set; }

		private IList<IntersectionRun> UsedSubcurves { get; } = new List<IntersectionRun>();

		/// <summary>
		/// Intersections to be used as start-intersections for right-side ring operations, i.e.
		/// area-intersection.
		/// These are the intersections at which the target 'arrives' at the source ring boundary
		/// from the inside. The target linestring does not necessarily have to continue to the
		/// outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> RightSideRingStartIntersections
		{
			get
			{
				// If the target touches from the inside, include it only if the target cuts the
				// source into two touching rings, which is only the case if the source is on the
				// right side (inside) of the target.

				Predicate<IntersectionPoint3D> onlyIfSourceCutting =
					ip =>
						ip.SourceArrivesFromRightSide(Source, Target, Tolerance) == true ||
						ip.SourceContinuesToRightSide(Source, Target, Tolerance) == true;

				IEnumerable<IntersectionPoint3D> startIntersections =
					IntersectionPointNavigator.GetIntersectionsWithOutBoundTarget(
						onlyIfSourceCutting);

				return Target.IsClosed
					       ? startIntersections
					       : FilterIntersections(startIntersections,
					                             IntersectionPointNavigator
						                             .FirstIntersectionsPerPart);
			}
		}

		/// <summary>
		/// Intersections to be used as start-intersections for left-side ring operations, i.e.
		/// area-difference.
		/// These are the intersections at which the target 'departs' from the source ring boundary
		/// to the inside. The target linestring does not necessarily have to arrive from the
		/// outside.
		/// </summary>
		public IEnumerable<IntersectionPoint3D> LeftSideRingStartIntersections
		{
			get
			{
				// If the target touches from the inside, include it only if the source cuts the
				// target into two touching rings, which is only the case if the source touches
				// from the left (outside) of the target.
				Predicate<IntersectionPoint3D> onlyIfTargetCutting =
					ip =>
						ip.SourceArrivesFromRightSide(Source, Target, Tolerance) == false ||
						ip.SourceContinuesToRightSide(Source, Target, Tolerance) == false;

				IEnumerable<IntersectionPoint3D> startIntersections =
					IntersectionPointNavigator.GetIntersectionsWithInBoundTarget(
						onlyIfTargetCutting);

				return Target.IsClosed
					       ? startIntersections
					       : FilterIntersections(startIntersections,
					                             IntersectionPointNavigator
						                             .LastIntersectionsPerPart);
			}
		}

		private TurnDirection PreferredTurnDirection { get; set; } = TurnDirection.Right;

		public SubcurveIntersectionPointNavigator IntersectionPointNavigator
		{
			get
			{
				if (_intersectionPointNavigator == null)
				{
					_intersectionPointNavigator =
						new SubcurveIntersectionPointNavigator(
							IntersectionPoints, Source, Target, Tolerance);
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

					// TODO: Why not calculate the entire source and get the correct (global) segment
					//       indexes on the SegmentIntersections?
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

					_intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
						Source, Target, Tolerance,
						includeLinearIntersectionIntermediateRingStartEndPoints);
				}

				return _intersectionPoints;
			}
			set
			{
				_intersectionPoints = value;
				_intersectionPointNavigator = null;
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

		public bool RingsCouldContainEachOther { get; private set; }

		/// <summary>
		/// Cut operations use both the left and the right side rings of the target. However, 
		/// cutting with non-closed targets can result in rings being both on the left and the
		/// right side of the target. To avoid duplicate rings, the start intersections need to
		/// be tracked across the two operations (otherwise duplicate rings need to be filtered
		/// afterwards).
		/// </summary>
		public void PrepareForCutOperation()
		{
			if (! Target.IsClosed)
			{
				VisitedIntersectionsAlongSource = new HashSet<IntersectionPoint3D>();
			}
		}

		/// <summary>
		/// Starting at outbound source intersections, follows the subcurves in a clockwise manner
		/// to get the union of both the source and the target by preferring a left turn at
		/// intersections.
		/// </summary>
		/// <returns></returns>
		public IList<Linestring> FollowSubcurvesTurningLeft()
		{
			TurnDirection originalTurnDirection = PreferredTurnDirection;

			try
			{
				PreferredTurnDirection = TurnDirection.Left;

				IntersectionPointNavigator.AllowBoundaryLoops = false;

				IEnumerable<IntersectionPoint3D> startPoints =
					IntersectionPointNavigator.IntersectionsOutboundSource;

				return FollowSubcurvesClockwise(startPoints.ToList());
			}
			finally
			{
				PreferredTurnDirection = originalTurnDirection;
				IntersectionPointNavigator.AllowBoundaryLoops = true;
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

			// Reset state:
			_congruentRings.Clear();
			RingsCouldContainEachOther = false;

			while (startIntersections.Count > 0)
			{
				subcurveInfos.Clear();

				Pnt3D ringStart = null;
				foreach (IntersectionRun next in NavigateSubcurves(startIntersections))
				{
					if (next.Subcurve.GetLength2D() == 0)
					{
						continue;
					}

					if (next.RunsAlongSource && next.RunsAlongTarget &&
					    next.Subcurve.IsClosed)
					{
						// It's a congruent ring. Remember for subsequent operation? directly add to result?
						// Probably a lot of special cases treatment w.r.t intersection points can be eliminated
						// if we do the right thing here!?
						_congruentRings.Add(next);
						continue;
					}

					subcurveInfos.Add(next);

					if (next.ContainsSourceStart(out Pnt3D startPoint))
					{
						ringStart = startPoint;
					}
				}

				if (CanMakeRing(subcurveInfos))
				{
					// Finish ring
					Linestring finishedRing = CreateClosedRing(subcurveInfos, ringStart);

					result.Add(finishedRing);

					RememberUsedIntersectionRuns(subcurveInfos);
					RememberUsedSourceParts(subcurveInfos);
					RememberUsedTargetParts(subcurveInfos);
				}
			}

			return result;
		}

		public IEnumerable<Linestring> FollowIntersectionsThroughTargetRings(
			bool excludeTargetBoundaryIntersections)
		{
			IntersectionPointNavigator.AssumeSourceRings = false;

			foreach (IntersectionPoint3D firstInPart in
			         IntersectionPointNavigator.GetFirstSourceIntersectionsPerPart())
			{
				Linestring sourceLinestring =
					Source.GetPart(firstInPart.SourcePartIndex);

				IntersectedSourcePartIndexes.Add(firstInPart.SourcePartIndex);

				IntersectionPointNavigator.SetStartIntersection(firstInPart);

				foreach (Linestring result in FollowSourcePartThroughTargetRings(
					         sourceLinestring, firstInPart, excludeTargetBoundaryIntersections))
				{
					yield return result;
				}
			}

			foreach (Linestring containedSourcePart in GetUnprocessedSourceParts(
				         false, false, includeContained: true, includeNotContained: false))
			{
				// The source ring is completely inside or completely outside the target area:
				yield return containedSourcePart;
			}
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

		public IEnumerable<Linestring> GetUnprocessedSourceParts(bool includeCongruent,
		                                                         bool withSameOrientation,
		                                                         bool includeContained,
		                                                         bool includeNotContained)
		{
			var unCutSourceIndexes = GetUnusedIndexes(
				Source.PartCount, IntersectedSourcePartIndexes).ToList();

			// Process the boundary loops of partially 'used' source parts, where only the other
			// loop has been used. Additionally process all boundary loops if they loop to the
			// outside - they must be split up because subcurve navigation does not really support
			// loops to the outside.
			foreach (BoundaryLoop boundaryLoop in GetSourceBoundaryLoops())
			{
				//BoundaryLoop boundaryLoop =
				//	new BoundaryLoop(start, end, Source.GetPart(start.SourcePartIndex), true);

				if (! boundaryLoop.IsLoopingToOutside &&
				    unCutSourceIndexes.Contains(boundaryLoop.Start.SourcePartIndex))
				{
					// Completely uncut boundary loops to the inside can be handled like
					// normal rings (below)
					continue;
				}

				foreach (Linestring resultRing in
				         ProcessSourceBoundaryLoops(boundaryLoop, includeCongruent,
				                                    withSameOrientation,
				                                    includeContained, includeNotContained))
				{
					yield return resultRing;

					IntersectedSourcePartIndexes.Add(boundaryLoop.Start.SourcePartIndex);
				}
			}

			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				foreach (BoundaryLoop boundaryLoop in
				         GetTargetBoundaryLoops()
					         .Where(bl => bl.Start.SourcePartIndex == unCutSourceIdx))
				{
					foreach (Linestring sourceRing in
					         CompareToTargetBoundaryLoops(unCutSourceIdx, boundaryLoop,
					                                      includeCongruent,
					                                      withSameOrientation, includeContained,
					                                      includeNotContained))
					{
						yield return sourceRing;
						yield break;
					}
				}
			}

			foreach (int unCutSourceIdx in GetUnusedIndexes(
				         Source.PartCount, IntersectedSourcePartIndexes))
			{
				{
					bool? isContainedXY = GeomRelationUtils.IsContainedXY(
						Source, Target, Tolerance,
						IntersectionPointNavigator.IntersectionsAlongSource,
						unCutSourceIdx);

					Linestring sourceRing = Source.GetPart(unCutSourceIdx);
					Linestring targetRing = null;

					if (isContainedXY == null)
					{
						int targetIndex =
							IntersectionPointNavigator.IntersectionsAlongSource
							                          .Where(
								                          i =>
									                          i.SourcePartIndex == unCutSourceIdx &&
									                          i.Type == IntersectionPointType
										                          .LinearIntersectionStart &&
									                          i.VirtualSourceVertex == 0)
							                          .GroupBy(i => i.TargetPartIndex)
							                          .Single().Key;

						targetRing = Target.GetPart(targetIndex);
					}

					if (CheckRingRelation(sourceRing, targetRing, isContainedXY, includeCongruent,
					                      withSameOrientation, includeContained,
					                      includeNotContained))
					{
						yield return sourceRing;
					}
				}
			}

			// Add the congruent source rings found during subcurve navigation (typically with extra boundary loop)
			if (includeCongruent)
			{
				foreach (IntersectionRun congruentRun in _congruentRings)
				{
					int sourcePartIdx = congruentRun.NextIntersection.SourcePartIndex;

					if (unCutSourceIndexes.Contains(sourcePartIdx))
					{
						// already dealt with above
						continue;
					}

					int targetPart = congruentRun.NextIntersection.TargetPartIndex;

					Linestring sourceRing = GetKnownCongruentSourceRing(
						sourcePartIdx, targetPart, withSameOrientation);

					if (sourceRing != null)
					{
						yield return congruentRun.Subcurve;
					}
				}
			}
		}

		public bool HasBoundaryLoops()
		{
			return IntersectionPointNavigator.TargetBoundaryLoopIntersections.Count != 0 ||
			       IntersectionPointNavigator.SourceBoundaryLoopIntersections.Count != 0;
		}

		#region Boundary loop handling

		private IEnumerable<Linestring> ProcessSourceBoundaryLoops(
			[NotNull] BoundaryLoop boundaryLoop, bool includeCongruent,
			bool withSameOrientation, bool includeContained,
			bool includeNotContained)
		{
			// If the end is the next intersection from the start it means the ring is un-used:
			if (! HasLoop1BeenUsed(boundaryLoop, true))
			{
				Linestring loop1 = boundaryLoop.Loop1;

				int targetIndex = boundaryLoop.Start.TargetPartIndex;

				if (ProcessSourceRing(loop1, targetIndex, includeCongruent,
				                      withSameOrientation, includeContained,
				                      includeNotContained))
				{
					yield return loop1;
				}
			}

			// And check the other loop too:
			if (! HasLoop2BeenUsed(boundaryLoop, true))
			{
				Linestring loop2 = boundaryLoop.Loop2;

				int targetIndex = boundaryLoop.End.TargetPartIndex;

				if (ProcessSourceRing(loop2, targetIndex, includeCongruent,
				                      withSameOrientation, includeContained,
				                      includeNotContained))
				{
					yield return loop2;
				}
			}
		}

		/// <summary>
		/// Determines whether the segments of the loop1 of the specified boundary loop have been
		/// used already by any uf the <see cref="UsedSubcurves"/> as they were used during the
		/// subcurve navigation.
		/// </summary>
		/// <param name="boundaryLoop"></param>
		/// <param name="isSource">Whether the boundary loop is a source boundary loop and
		/// hence it is checked whether the usage runs along the source</param>
		/// <returns></returns>
		private bool HasLoop1BeenUsed(BoundaryLoop boundaryLoop,
		                              bool isSource)
		{
			return IntersectionPointNavigator.IsAnyIntersectionUsedBetween(
				boundaryLoop.Start, boundaryLoop.End, UsedSubcurves, isSource, true);
		}

		/// <summary>
		/// Determines whether the segments of the loop2 of the specified boundary loop have been
		/// used already by any uf the <see cref="UsedSubcurves"/> as they were used during the
		/// subcurve navigation.
		/// </summary>
		/// <param name="boundaryLoop"></param>
		/// <param name="isSource">Whether the boundary loop is a source boundary loop and
		/// hence it is checked whether the usage runs along the source</param>
		/// <returns></returns>
		private bool HasLoop2BeenUsed(BoundaryLoop boundaryLoop,
		                              bool isSource)
		{
			return IntersectionPointNavigator.IsAnyIntersectionUsedBetween(
				boundaryLoop.End, boundaryLoop.Start, UsedSubcurves, isSource, true);
		}

		private IEnumerable<Linestring> CompareToTargetBoundaryLoops(
			int sourceRingIndex,
			[NotNull] BoundaryLoop boundaryLoop, bool includeCongruent,
			bool withSameOrientation, bool includeContained,
			bool includeNotContained)
		{
			Linestring sourceRing = Source.GetPart(sourceRingIndex);

			Linestring loop1 = boundaryLoop.Loop1;
			RingRingRelation loop1Relation = GetTargetRelation(sourceRingIndex, loop1);

			Linestring loop2 = boundaryLoop.Loop2;
			RingRingRelation loop2Relation = GetTargetRelation(sourceRingIndex, loop2);

			// If No exact boundary match has been found, the contains/not contains logic must be prioritized first:
			RingRingRelation priorityRelation = QualifyContainmentRelations(
				boundaryLoop, includeContained, includeNotContained, loop1Relation, loop2Relation);

			if (priorityRelation == RingRingRelation.Undefined)
			{
				// Both have relevant relations:
				priorityRelation = GetPriorityRelation(loop1Relation, loop2Relation,
				                                       includeCongruent, includeContained,
				                                       includeNotContained);
			}

			if (CheckRingRelation(priorityRelation,
			                      includeCongruent, withSameOrientation, includeContained,
			                      includeNotContained))
			{
				yield return sourceRing;
			}

			// Even if the ring was not yielded, the source ring should be marked as processed:
			if (priorityRelation == RingRingRelation.CongruentSameOrientation ||
			    priorityRelation == RingRingRelation.CongruentDifferentOrientation)
			{
				// It should not be detected any more as contained target:
				IntersectedSourcePartIndexes.Add(sourceRingIndex);
			}
		}

		/// <summary>
		/// Ensures that a relation is only used if it is relevant. E.g. the outer boundary loop is
		/// irrelevant if the inner loop contains the source ring.
		/// </summary>
		/// <param name="boundaryLoop"></param>
		/// <param name="includeContained"></param>
		/// <param name="includeNotContained"></param>
		/// <param name="loop1Relation"></param>
		/// <param name="loop2Relation"></param>
		/// <returns></returns>
		private static RingRingRelation QualifyContainmentRelations(
			[NotNull] BoundaryLoop boundaryLoop,
			bool includeContained,
			bool includeNotContained,
			RingRingRelation loop1Relation,
			RingRingRelation loop2Relation)
		{
			// Use a relation only if it is relevant! I.e. the outer boundary loop is irrelevant
			// if the inner loop contains the source ring

			// NOTE: Both loops being negative is not supported (should not happen)

			// First exclude the cases that are not handled here:
			if (loop1Relation == RingRingRelation.CongruentDifferentOrientation ||
			    loop1Relation == RingRingRelation.CongruentSameOrientation ||
			    loop2Relation == RingRingRelation.CongruentDifferentOrientation ||
			    loop2Relation == RingRingRelation.CongruentSameOrientation)
			{
				return RingRingRelation.Undefined;
			}

			RingRingRelation priorityRelation = RingRingRelation.Undefined;

			Linestring loop1 = boundaryLoop.Loop1;
			Linestring loop2 = boundaryLoop.Loop2;

			if (! boundaryLoop.IsLoopingToOutside &&
			    loop1.ClockwiseOriented != loop2.ClockwiseOriented)
			{
				if (boundaryLoop.Loop1ContainsLoop2 &&
				    loop2Relation == RingRingRelation.IsNotContained &&
				    loop2.ClockwiseOriented == false)
				{
					// It is 'inside' the 'inner' loop2 (i.e. outside): loop1Relation is irrelevant
					priorityRelation = loop2Relation;
				}

				if (boundaryLoop.Loop2ContainsLoop1 &&
				    loop1Relation == RingRingRelation.IsNotContained &&
				    loop1.ClockwiseOriented == false)
				{
					// It is 'inside' the 'inner' loop1 (i.e. outside): loop2Relation is irrelevant
					priorityRelation = loop1Relation;
				}
			}
			else
			{
				// Both have the same orientation: Use the correct logic for Contains/NotContains:
				// For 'Contained':
				// Any positive loop that contains the target should result in 'Contained'
				if (includeContained &&
				    loop1.ClockwiseOriented == true &&
				    loop2.ClockwiseOriented == true)
				{
					if (loop1Relation == RingRingRelation.IsContained ||
					    loop2Relation == RingRingRelation.IsContained)
					{
						priorityRelation = RingRingRelation.IsContained;
					}
				}

				// For 'NotContained':
				// No positive loop must contain the target
				if (includeNotContained &&
				    loop1.ClockwiseOriented == true &&
				    loop2.ClockwiseOriented == true)
				{
					if (loop1Relation == RingRingRelation.IsContained ||
					    loop2Relation == RingRingRelation.IsContained)
					{
						// ignore  it
						priorityRelation = RingRingRelation.IsContained;
					}
				}
			}

			return priorityRelation;
		}

		private static RingRingRelation GetPriorityRelation(RingRingRelation loop1Relation,
		                                                    RingRingRelation loop2Relation,
		                                                    bool isCongruent,
		                                                    bool isContained,
		                                                    bool isNotContained)
		{
			if (loop1Relation == RingRingRelation.Undefined)
			{
				return loop2Relation;
			}

			if (loop2Relation == RingRingRelation.Undefined)
			{
				return loop1Relation;
			}

			if (isCongruent)
			{
				if (loop1Relation == RingRingRelation.CongruentSameOrientation ||
				    loop1Relation == RingRingRelation.CongruentDifferentOrientation)
				{
					return loop1Relation;
				}

				if (loop2Relation == RingRingRelation.CongruentSameOrientation ||
				    loop2Relation == RingRingRelation.CongruentDifferentOrientation)
				{
					return loop2Relation;
				}
			}

			if (isContained)
			{
				if (loop1Relation == RingRingRelation.IsContained)
				{
					return loop1Relation;
				}

				if (loop2Relation == RingRingRelation.IsContained)
				{
					return loop2Relation;
				}
			}

			if (isNotContained)
			{
				if (loop1Relation == RingRingRelation.IsNotContained)
				{
					return loop1Relation;
				}

				if (loop2Relation == RingRingRelation.IsNotContained)
				{
					return loop2Relation;
				}
			}

			return loop1Relation;
		}

		private IEnumerable<BoundaryLoop> GetSourceBoundaryLoops()
		{
			return IntersectionPointNavigator.GetSourceBoundaryLoops(true);
		}

		private IEnumerable<BoundaryLoop> GetTargetBoundaryLoops()
		{
			List<BoundaryLoop> result = new List<BoundaryLoop>();

			foreach ((IntersectionPoint3D start, IntersectionPoint3D end) in
			         IntersectionPointNavigator.TargetBoundaryLoopIntersections)
			{
				BoundaryLoop boundaryLoop = new BoundaryLoop(
					start, end, Target.GetPart(start.TargetPartIndex), false);

				result.Add(boundaryLoop);
			}

			return result.Distinct(new BoundaryLoopComparer());
		}

		/// <summary>
		/// Determines the target's boundary loops that have an intersection with a source part that,
		/// has not yet been processed. The result can be used to determine e.g. equal rings that have
		/// not yet been detected/processed.
		/// </summary>
		/// <returns></returns>
		private IList<Tuple<IntersectionPoint3D, Linestring>> GetUnprocessedSourceBoundaryLoops(
			[NotNull] out ICollection<int> targetPartIndexes)
		{
			// In case of touching multipart target rings, the source boundary loop is duplicated!
			targetPartIndexes = new HashSet<int>();
			var result = new List<Tuple<IntersectionPoint3D, Linestring>>();

			foreach (BoundaryLoop boundaryLoop in GetSourceBoundaryLoops())
			{
				IntersectionPoint3D start = boundaryLoop.Start;
				IntersectionPoint3D end = boundaryLoop.End;

				if (! HasLoop1BeenUsed(boundaryLoop, true))
				{
					Linestring loop = boundaryLoop.Loop1;
					targetPartIndexes.Add(start.TargetPartIndex);

					if (! result.Any(r => r.Item2.Equals(loop)))
					{
						result.Add(new Tuple<IntersectionPoint3D, Linestring>(start, loop));
					}
				}

				// And check the other loop too:
				if (! HasLoop2BeenUsed(boundaryLoop, true))
				{
					Linestring loop = boundaryLoop.Loop2;
					targetPartIndexes.Add(start.TargetPartIndex);

					if (! result.Any(r => r.Item2.Equals(loop)))
					{
						result.Add(new Tuple<IntersectionPoint3D, Linestring>(end, loop));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Determines the target's boundary loops that have an intersection with a source part that,
		/// has not yet been processed. The result can be used to determine e.g. equal rings that have
		/// not yet been detected/processed.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<Tuple<IntersectionPoint3D, Linestring>>
			GetUnprocessedTargetBoundaryLoops()
		{
			// TODO: Same logic as source boundary loops: Return all intersected source rings

			// In some cases the boundary loops are duplicated!
			List<Linestring> yieldedLoops = new List<Linestring>();
			foreach (BoundaryLoop boundaryLoop in GetTargetBoundaryLoops())
			{
				if (IntersectionPointNavigator.IsNextTargetIntersection(
					    boundaryLoop.Start, boundaryLoop.End))
				{
					Linestring loop = boundaryLoop.Loop1;
					if (! yieldedLoops.Contains(loop))
					{
						yieldedLoops.Add(loop);
						yield return new Tuple<IntersectionPoint3D, Linestring>(
							boundaryLoop.Start, loop);
					}
				}

				// And check the other loop too:
				if (IntersectionPointNavigator.IsNextTargetIntersection(
					    boundaryLoop.End, boundaryLoop.Start))
				{
					Linestring loop = boundaryLoop.Loop2;
					if (! yieldedLoops.Contains(loop))
					{
						yieldedLoops.Add(loop);
						yield return new Tuple<IntersectionPoint3D, Linestring>(
							boundaryLoop.End, loop);
					}
				}
			}
		}

		#endregion

		private bool ProcessSourceRing([NotNull] Linestring sourceRing,
		                               int targetIndex,
		                               bool includeCongruent, bool withSameOrientation,
		                               bool includeContained, bool includeNotContained)
		{
			Linestring targetRing = Target.GetPart(targetIndex);

			bool? isContainedXY = GeomRelationUtils.IsContainedXY(
				sourceRing, targetRing, Tolerance);

			RingRingRelation relation = GetRingRelation(sourceRing, targetRing, isContainedXY);

			bool result = CheckRingRelation(relation, includeCongruent, withSameOrientation,
			                                includeContained, includeNotContained);

			if (includeCongruent &&
			    (relation == RingRingRelation.CongruentSameOrientation ||
			     relation == RingRingRelation.CongruentDifferentOrientation))
			{
				// Congruent rings are always handled conclusively (either ignored or added)
				// Regardless of whether is is used in the result geometry, it should not be detected any more as contained target:
				IntersectedTargetPartIndexes.Add(targetIndex);
			}

			return result;
		}

		private RingRingRelation GetTargetRelation(int sourceRingIndex,
		                                           [NotNull] Linestring targetRing)
		{
			Linestring sourceRing = Source.GetPart(sourceRingIndex);

			bool? isContainedXY = GeomRelationUtils.IsContainedXY(
				sourceRing, targetRing, Tolerance);

			return GetRingRelation(sourceRing, targetRing, isContainedXY);
		}

		private static bool CheckRingRelation([NotNull] Linestring sourceRing,
		                                      [CanBeNull] Linestring targetRing,
		                                      bool? isContainedXY, bool isCongruent,
		                                      bool withSameOrientation, bool isContained,
		                                      bool isNotContained)
		{
			if (isContainedXY == null && isCongruent)
			{
				// congruent
				Assert.NotNull(targetRing);
				if (IsKnownCongruentSourceRingOriented(sourceRing, targetRing, withSameOrientation))
				{
					return true;
				}
			}
			else if (isContainedXY == true && isContained)
			{
				return true;
			}
			else if (isContainedXY == false && isNotContained)
			{
				return true;
			}

			return false;
		}

		private static bool CheckRingRelation(RingRingRelation relation,
		                                      bool isCongruent,
		                                      bool withSameOrientation,
		                                      bool isContained,
		                                      bool isNotContained)
		{
			if (relation == RingRingRelation.CongruentSameOrientation)
			{
				if (isCongruent && withSameOrientation)
				{
					return true;
				}
			}

			if (relation == RingRingRelation.CongruentDifferentOrientation)
			{
				if (isCongruent && ! withSameOrientation)
				{
					return true;
				}
			}

			if (relation == RingRingRelation.IsContained)
			{
				if (isContained)
				{
					return true;
				}
			}

			if (relation == RingRingRelation.IsNotContained)
			{
				if (isNotContained)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the ring relation of the source relative to the target
		/// </summary>
		/// <param name="sourceRing"></param>
		/// <param name="targetRing"></param>
		/// <param name="isContainedXY"></param>
		/// <returns></returns>
		private static RingRingRelation GetRingRelation([NotNull] Linestring sourceRing,
		                                                [CanBeNull] Linestring targetRing,
		                                                bool? isContainedXY)
		{
			if (isContainedXY == null)
			{
				// congruent
				Assert.NotNull(targetRing);
				return IsKnownCongruentSourceRingOriented(sourceRing, targetRing, true)
					       ? RingRingRelation.CongruentSameOrientation
					       : RingRingRelation.CongruentDifferentOrientation;
			}

			if (isContainedXY == true)
			{
				// The source is contained within the target
				return sourceRing.ClockwiseOriented == true
					       ? RingRingRelation.IsContained
					       : RingRingRelation.IsNotContained;
			}

			// The source is not contained within the target (target orientation already taken into account)
			return RingRingRelation.IsNotContained;
		}

		/// <summary>
		/// Identifies source rings that are equal to other rings in the target.
		/// Identifies source rings that are not contained by the target and vice-versa.
		/// </summary>
		/// <param name="equalOrientationCongruentRings"></param>
		/// <param name="outsideOtherPolygonRings"></param>
		public void DetermineExtraRingRelations(
			out IList<Linestring> equalOrientationCongruentRings,
			out IList<Linestring> outsideOtherPolygonRings)
		{
			equalOrientationCongruentRings = new List<Linestring>();
			outsideOtherPolygonRings = new List<Linestring>();
			var removedInteriorBoundaryLoops = new List<Linestring>();

			// Add the boundary loops of 'used' source parts, but only the other loop has been used.
			// This could probably go to the subcurve navigation and be added to congruent or un-used subcurves?
			// Source boundary loops:
			foreach ((IntersectionPoint3D startIntersection, Linestring sourceLoop) in
			         GetUnprocessedSourceBoundaryLoops(out ICollection<int> targetPartIndexes))
			{
				int sourcePartIndex = startIntersection.SourcePartIndex;

				Linestring targetRing = null;

				bool containedByAny = false;
				foreach (int targetPartIndex in targetPartIndexes)
				{
					targetRing = Target.GetPart(targetPartIndex);

					bool? isContainedXY =
						GeomRelationUtils.IsContainedXY(sourceLoop, targetRing, Tolerance);

					if (isContainedXY == false)
					{
						continue;
					}

					containedByAny = true;
					DetermineRingRelation(sourceLoop, targetRing, isContainedXY,
					                      sourcePartIndex, targetPartIndex,
					                      equalOrientationCongruentRings, outsideOtherPolygonRings);

					if (isContainedXY == true)
					{
						removedInteriorBoundaryLoops.Add(sourceLoop);
					}
				}

				if (containedByAny)
				{
					IntersectedSourcePartIndexes.Add(sourcePartIndex);
				}

				if (! containedByAny && targetRing != null)
				{
					DetermineRingRelation(sourceLoop, targetRing, false,
					                      sourcePartIndex, targetPartIndexes.Last(),
					                      equalOrientationCongruentRings, outsideOtherPolygonRings);
				}
			}

			// Target boundary loops:
			foreach ((IntersectionPoint3D startIntersection, Linestring targetLoop) in
			         GetUnprocessedTargetBoundaryLoops())
			{
				int sourcePartIndex = startIntersection.SourcePartIndex;
				int targetPartIndex = startIntersection.TargetPartIndex;
				Linestring sourceRing = Source.GetPart(sourcePartIndex);

				bool? isContainedXY = GeomRelationUtils.IsContainedXY(
					sourceRing, targetLoop, Tolerance);

				DetermineRingRelation(sourceRing, targetLoop, isContainedXY,
				                      sourcePartIndex, targetPartIndex,
				                      equalOrientationCongruentRings, outsideOtherPolygonRings);

				if (false == GeomRelationUtils.AreaContainsXY(
					    sourceRing, targetLoop, Tolerance))
				{
					outsideOtherPolygonRings.Add(targetLoop);
				}
				else
				{
					IntersectedTargetPartIndexes.Add(targetPartIndex);
					IntersectedSourcePartIndexes.Add(sourcePartIndex);
				}
			}

			var unCutSourceIndexes = GetUnusedIndexes(
				Source.PartCount, IntersectedSourcePartIndexes).ToList();

			// Non-boundary loops, not yet used:
			foreach (int unCutSourcePartIdx in unCutSourceIndexes)
			{
				// No inbound/outbound, but possibly touching or linear intersections

				bool? isSourceContainedXY = GeomRelationUtils.IsContainedXY(
					Source, Target, Tolerance, IntersectionPointNavigator.IntersectionsAlongSource,
					unCutSourcePartIdx);

				Linestring sourceRing = Source.GetPart(unCutSourcePartIdx);

				int targetIndex = -1;
				Linestring targetRing = null;

				if (isSourceContainedXY == null)
				{
					targetIndex =
						IntersectionPointNavigator.IntersectionsAlongSource
						                          .Where(
							                          i => i.SourcePartIndex ==
							                               unCutSourcePartIdx &&
							                               i.Type == IntersectionPointType
								                               .LinearIntersectionStart &&
							                               i.VirtualSourceVertex == 0)
						                          .GroupBy(i => i.TargetPartIndex)
						                          .Single().Key;

					targetRing = Target.GetPart(targetIndex);
				}

				DetermineRingRelation(sourceRing, targetRing, isSourceContainedXY,
				                      unCutSourcePartIdx, targetIndex,
				                      equalOrientationCongruentRings, outsideOtherPolygonRings);
			}

			foreach (int unCutTargetIdx in GetUnusedIndexes(
				         Target.PartCount, IntersectedTargetPartIndexes))
			{
				// Congruent rings would have been found already 
				Linestring targetRing = Target.GetPart(unCutTargetIdx);

				if (false == GeomRelationUtils.AreaContainsXY(
					    Source, Target, Tolerance,
					    IntersectionPointNavigator.IntersectionsAlongTarget, unCutTargetIdx))
				{
					// Except if it is contained by a previously removed island:
					bool outsideRemovedIslands = removedInteriorBoundaryLoops.All(
						removedIsland =>
							GeomRelationUtils.AreaContainsXY(
								removedIsland, targetRing, Tolerance) == true);

					if (outsideRemovedIslands)
					{
						outsideOtherPolygonRings.Add(targetRing);

						// But it needs extra checking in case it interior intersects a removed island:
						if (removedInteriorBoundaryLoops.Any(
							    removedIsland =>
								    GeomRelationUtils.InteriorIntersectXY(
									    removedIsland, targetRing, Tolerance)))
						{
							RingsCouldContainEachOther = true;
						}
					}
				}
			}
		}

		private void DetermineRingRelation(Linestring sourceRing, Linestring targetRing,
		                                   bool? sourceIsKnownContainedXY,
		                                   int sourcePartIndex, int targetPartIndex,
		                                   IList<Linestring> equalOrientationCongruentRings,
		                                   IList<Linestring> outsideOtherPolygonRings)
		{
			if (sourceIsKnownContainedXY == null)
			{
				// Even if they have opposite orientation: They cancel each other out and should
				// be disregarded in subsequent operations.
				IntersectedSourcePartIndexes.Add(sourcePartIndex);
				IntersectedTargetPartIndexes.Add(targetPartIndex);
			}

			if (CheckRingRelation(sourceRing, targetRing, isContainedXY: sourceIsKnownContainedXY,
			                      isCongruent: true, withSameOrientation: true,
			                      isContained: false, isNotContained: false))
			{
				equalOrientationCongruentRings.Add(sourceRing);
			}
			else if (CheckRingRelation(sourceRing, targetRing, sourceIsKnownContainedXY,
			                           false, false, false, isNotContained: true))
			{
				outsideOtherPolygonRings.Add(sourceRing);

				IntersectedSourcePartIndexes.Add(sourcePartIndex);
			}
		}

		[CanBeNull]
		private Linestring GetKnownCongruentSourceRing(int sourceIdx,
		                                               int targetIndex,
		                                               bool withSameOrientation)
		{
			Linestring sourceRing = Source.GetPart(sourceIdx);
			if (IsKnownCongruentSourceRingOriented(sourceRing, targetIndex, withSameOrientation))
			{
				return sourceRing;
			}

			return null;
		}

		private bool IsKnownCongruentSourceRingOriented(Linestring sourceRing, int targetIndex,
		                                                bool withSameOrientationAsTarget)
		{
			Linestring targetRing = Target.GetPart(targetIndex);

			return IsKnownCongruentSourceRingOriented(sourceRing, targetRing,
			                                          withSameOrientationAsTarget);
		}

		private static bool IsKnownCongruentSourceRingOriented([NotNull] Linestring sourceRing,
		                                                       [NotNull] Linestring targetRing,
		                                                       bool withSameOrientationAsTarget)
		{
			if (withSameOrientationAsTarget &&
			    sourceRing.ClockwiseOriented != null &&
			    sourceRing.ClockwiseOriented == targetRing.ClockwiseOriented)
			{
				return true;
			}

			if (! withSameOrientationAsTarget &&
			    sourceRing.ClockwiseOriented != null &&
			    sourceRing.ClockwiseOriented != targetRing.ClockwiseOriented)
			{
				// The interior of a positive ring is on the left side of a negative ring
				return true;
			}

			return false;
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

				if (true == GeomRelationUtils.AreaContainsXY(
					    Source, Target, Tolerance,
					    IntersectionPointNavigator.IntersectionsAlongTarget, unCutTargetIdx))
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

		private Linestring CreateClosedRing(List<IntersectionRun> subcurveInfos, Pnt3D ringStart)
		{
			// Make sure the ring closes (in case there are multiple intersections within the tolerance)

			Pnt3D firstPoint = subcurveInfos[0].Subcurve.StartPoint;
			Linestring lastSubcurve = subcurveInfos[subcurveInfos.Count - 1].Subcurve;
			Line3D lastSegment = lastSubcurve.Segments[lastSubcurve.SegmentCount - 1];

			lastSegment.SetEndPoint(firstPoint.ClonePnt3D());

			Linestring finishedRing = GeomTopoOpUtils.MergeConnectedLinestrings(
				subcurveInfos.Select(i => i.Subcurve).ToList(), ringStart, Tolerance);

			Assert.True(finishedRing.IsClosed, "The ring is not closed.");

			return finishedRing;
		}

		private IEnumerable<IntersectionRun> NavigateSubcurves(
			ICollection<IntersectionPoint3D> startIntersections)
		{
			IntersectionPoint3D startIntersection = startIntersections.First();

			if (VisitedIntersectionsAlongSource?.Contains(startIntersection) == true)
			{
				startIntersections.Remove(startIntersection);
				yield break;
			}

			IntersectionPoint3D currentIntersection = startIntersection;
			IntersectionPoint3D nextIntersection = null;

			IntersectionPointNavigator.SetStartIntersection(startIntersection);

			int count = 0;
			// Always start by following the source:
			bool continueOnSource = true;
			bool forward = true;
			while (nextIntersection == null ||
			       ! IntersectionPointNavigator.EqualsStartIntersection(nextIntersection))
			{
				Assert.True(count++ <= IntersectionPoints.Count,
				            "Intersections seen twice. Make sure the input has no self intersections.");

				if (nextIntersection != null)
				{
					// Determine if at the next intersection we must
					// - continue along the source (e.g. because the source touches from the inside)
					// - continue along the target (forward or backward)
					SetTurnDirection(startIntersection, PreferredTurnDirection,
					                 ref currentIntersection, ref continueOnSource, ref forward);
				}

				nextIntersection = FollowUntilNextIntersection(
					currentIntersection, continueOnSource, forward, out Linestring subcurve);

				Pnt3D containedSourceStart =
					GetSourceStartBetween(currentIntersection, nextIntersection, continueOnSource,
					                      forward);

				bool isBoundaryLoopIntersection =
					IntersectionPointNavigator.IsBoundaryLoopIntersection(nextIntersection);

				if (continueOnSource)
				{
					startIntersections.Remove(currentIntersection);

					// Cut operations with un-closed targets: A ring can be both on the right and the
					// left side! -> Remember the start intersections along the source to avoid using
					// an intersection twice which would result in duplicate rings.
					VisitedIntersectionsAlongSource?.Add(currentIntersection);
				}

				if (isBoundaryLoopIntersection)
				{
					// In the case of a boundary loop the same intersection can be visited twice:
					IntersectionPointNavigator.VisitedIntersections.Remove(nextIntersection);
				}

				IntersectionRun next =
					new IntersectionRun(startIntersection, nextIntersection, subcurve,
					                    containedSourceStart)
					{
						RunsAlongSource = continueOnSource,
						RunsAlongTarget = SubcurveRunsAlongTarget(
							continueOnSource, currentIntersection, nextIntersection,
							isBoundaryLoopIntersection, subcurve),
						IsBoundaryLoop = isBoundaryLoopIntersection
					};

				yield return next;

				currentIntersection = nextIntersection;
			}
		}

		private bool SubcurveRunsAlongTarget(
			bool continueOnSource,
			[NotNull] IntersectionPoint3D currentIntersection,
			[NotNull] IntersectionPoint3D nextIntersection,
			bool isBoundaryLoopIntersection,
			Linestring intersectionCurve)
		{
			if (! continueOnSource)
			{
				return true;
			}

			// Linear intersection start/end is always with respect to source
			if (currentIntersection.Type == IntersectionPointType.LinearIntersectionStart &&
			    nextIntersection.Type == IntersectionPointType.LinearIntersectionEnd)
			{
				if (currentIntersection.LinearIntersectionInOppositeDirection == false)
				{
					// same directon, target forward
					return true;
				}
			}

			if (currentIntersection.Type == IntersectionPointType.LinearIntersectionEnd &&
			    nextIntersection.Type == IntersectionPointType.LinearIntersectionStart)
			{
				// At boundary loops, there could be a direction change (inside loop: opposite, outside loop: not)
				if (isBoundaryLoopIntersection)
				{
					if (IntersectionPointNavigator.TargetBoundaryLoopIntersections.Contains(
						    new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
							    currentIntersection, nextIntersection)))
					{
						// We're going along the 
					}

					Linestring targetPart =
						IntersectionPointNavigator.Target.GetPart(
							currentIntersection.TargetPartIndex);

					double halfWay = 0.5;

					IPnt pointAlong = intersectionCurve.GetPointAlong(halfWay, true);

					if (GeomRelationUtils.LinesContainXY(targetPart, pointAlong, Tolerance))
					{
						return true;
					}
				}

				// It's the end of the linear intersection going forward along the source:
				// true, if the target goes in the opposite direction

				else if (currentIntersection.LinearIntersectionInOppositeDirection == true)
				{
					return true;
				}
			}

			return false;
		}

		private void SetTurnDirection(
			IntersectionPoint3D startIntersection,
			TurnDirection preferredDirection,
			ref IntersectionPoint3D intersection,
			ref bool alongSource, ref bool forward)
		{
			// First set the base line, along which we're arriving at the junction:
			Linestring sourceRing = Source.GetPart(intersection.SourcePartIndex);
			Linestring target = Target.GetPart(intersection.TargetPartIndex);

			Line3D entryLine = GetEntryLine(intersection, sourceRing, target,
			                                alongSource, forward);

			IntersectionPoint3D actualSourceIntersection = intersection;
			double? sourceForwardDirection = GetAlongSourceDirectionChange(preferredDirection,
				ref actualSourceIntersection, entryLine);

			IntersectionPoint3D actualTargetIntersection = intersection;
			GetAlongTargetDirectionChanges(preferredDirection, startIntersection.SourcePartIndex,
			                               ref actualTargetIntersection, entryLine,
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
			else
			{
				// targetForwardDirection and sourceForwardDirection are equal
				if (true == IsMore(preferredDirection, targetBackwardDirection,
				                   sourceForwardDirection))
				{
					alongSource = false;
					forward = false;
				}
			}

			intersection = alongSource ? actualSourceIntersection : actualTargetIntersection;
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
		private Line3D GetEntryLine([NotNull] IntersectionPoint3D intoIntersection,
		                            [NotNull] Linestring source,
		                            [NotNull] Linestring target,
		                            bool alongSource, bool forward)
		{
			Line3D entryLine;

			if (alongSource)
			{
				int sourceSegmentIdx =
					intoIntersection.GetLocalSourceIntersectionSegmentIdx(
						source, out double distanceAlongSource);

				entryLine = GetIncomingDirectionLine(
					source, sourceSegmentIdx, distanceAlongSource,
					Tolerance);
			}
			else
			{
				int targetSegmentIdx = intoIntersection.GetLocalTargetIntersectionSegmentIdx(
					target, out double distanceAlongTarget);

				if (forward)
				{
					entryLine = GetIncomingDirectionLine(
						target, targetSegmentIdx, distanceAlongTarget, Tolerance);
				}
				else
				{
					entryLine =
						GetContinuingDirectionLine(target, targetSegmentIdx, distanceAlongTarget,
						                           Tolerance);

					entryLine = entryLine.Clone();
					entryLine.ReverseOrientation();
				}
			}

			return entryLine;
		}

		private double? GetAlongSourceDirectionChange(TurnDirection preferredDirection,
		                                              ref IntersectionPoint3D intersection,
		                                              Line3D entryLine)
		{
			double? directionChange =
				GetAlongSourceDirectionChange(intersection, entryLine);

			if (! IntersectionPointNavigator.HasMultipleSourceIntersections(intersection))
			{
				return directionChange;
			}

			// Allow jumping between intersection points at the same location. This is necessary
			// for source rings touching another source ring.
			foreach (IntersectionPoint3D otherSourceIntersection in
			         IntersectionPointNavigator.GetOtherSourceIntersections(intersection))
			{
				if (CanFollowSource(otherSourceIntersection, intersection.SourcePartIndex))
				{
					double? otherIntersectionDirectionChange =
						GetAlongSourceDirectionChange(otherSourceIntersection, entryLine);

					if (true == IsMore(preferredDirection,
					                   otherIntersectionDirectionChange,
					                   directionChange))
					{
						intersection = otherSourceIntersection;
						directionChange = otherIntersectionDirectionChange;
					}
				}
			}

			return directionChange;
		}

		private double? GetAlongSourceDirectionChange(IntersectionPoint3D intersection,
		                                              Line3D entryLine)
		{
			Linestring sourceRing = Source.GetPart(intersection.SourcePartIndex);

			int sourceSegmentIdx =
				intersection.GetLocalSourceIntersectionSegmentIdx(sourceRing,
					out double distanceAlongSource);

			Line3D alongSourceLine =
				GetContinuingDirectionLine(sourceRing, sourceSegmentIdx, distanceAlongSource,
				                           Tolerance);

			double? sourceForwardDirection =
				GeomUtils.GetDirectionChange(entryLine, alongSourceLine);

			return sourceForwardDirection;
		}

		private void GetAlongTargetDirectionChanges(
			TurnDirection preferredDirection,
			int? initialSourcePartForRingResult,
			[NotNull] ref IntersectionPoint3D intersection,
			[NotNull] Line3D entryLine,
			out double? targetForwardDirection,
			out double? targetBackwardDirection)
		{
			GetAlongTargetDirectionChanges(initialSourcePartForRingResult, intersection, entryLine,
			                               out targetForwardDirection, out targetBackwardDirection);

			// Allow jumping between intersection points at the same location. This is necessary
			// for target rings touching another target ring or target boundary loops
			// or geometries with intersection points within the tolerance (alternatively
			// both geometries would need to be fully clustered in the 2D plane!)
			foreach (IntersectionPoint3D otherTargetIntersection in
			         IntersectionPointNavigator.GetOtherTargetIntersections(intersection, true))
			{
				if (IntersectionPointNavigator.VisitedIntersections.Contains(
					    otherTargetIntersection))
				{
					// Never turn back
					continue;
				}

				double? otherTargetForwardDirection;
				double? otherTargetBackwardDirection;

				GetAlongTargetDirectionChanges(
					initialSourcePartForRingResult, otherTargetIntersection, entryLine,
					out otherTargetForwardDirection, out otherTargetBackwardDirection);

				// Which intersection one has the most preferred direction?
				// 1. Get the maximum from the current intersection:
				double? intersectionMax =
					true == IsMore(preferredDirection, targetForwardDirection,
					               targetBackwardDirection)
						? targetForwardDirection
						: targetBackwardDirection;

				// 2. Compare the other directions to the maximum of the current
				if (true == IsMore(preferredDirection, otherTargetForwardDirection,
				                   intersectionMax) ||
				    true == IsMore(preferredDirection, otherTargetBackwardDirection,
				                   intersectionMax))
				{
					// In boundary loops both conditions should be true
					intersection = otherTargetIntersection;
					targetForwardDirection = otherTargetForwardDirection;
					targetBackwardDirection = otherTargetBackwardDirection;
				}
			}
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
					Line3D targetForward =
						GetNonShortContinuingLine(Target, forwardSegmentIdx.Value, Tolerance);

					targetForwardDirection = GeomUtils.GetDirectionChange(entryLine, targetForward);
				}
			}

			if (CanFollowTarget(startingAt, false, initialSourcePartForRingResult))
			{
				int? backwardSegmentIdx =
					startingAt.GetNonIntersectingTargetSegmentIndex(Target, false);

				if (backwardSegmentIdx != null)
				{
					Line3D targetBackward =
						GetNonShortIncomingLine(Target, backwardSegmentIdx.Value, Tolerance);

					targetBackward = targetBackward.Clone();
					targetBackward.ReverseOrientation();

					targetBackwardDirection =
						GeomUtils.GetDirectionChange(entryLine, targetBackward);
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

		private bool CanFollowSource(IntersectionPoint3D fromSourceIntersection,
		                             int initialSourcePartToReturnTo)
		{
			return IntersectionPointNavigator.AllowConnectToSourcePartAlongOtherSourcePart(
				fromSourceIntersection,
				initialSourcePartToReturnTo);
		}

		[NotNull]
		private IntersectionPoint3D FollowUntilNextIntersection(
			IntersectionPoint3D previousIntersection,
			bool continueOnSource,
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

		private static Line3D GetIncomingDirectionLine([NotNull] Linestring linestring,
		                                               int toSegmentIdx,
		                                               double toDistanceAlongRatio,
		                                               double minSegmentLength)
		{
			int segmentIdx =
				toDistanceAlongRatio > 0
					? toSegmentIdx
					: Assert.NotNull(linestring.PreviousSegmentIndex(toSegmentIdx)).Value;

			Line3D entryLine =
				GetNonShortIncomingLine(linestring, segmentIdx, minSegmentLength);

			return entryLine;
		}

		private static Line3D GetContinuingDirectionLine([NotNull] Linestring linestring,
		                                                 int fromSegmentIdx,
		                                                 double fromDistanceAlongRatio,
		                                                 double minSegmentLength)
		{
			int segmentIdx;
			if (fromDistanceAlongRatio < 1)
			{
				segmentIdx = fromSegmentIdx;
			}
			else
			{
				segmentIdx = Assert.NotNull(linestring.NextSegmentIndex(fromSegmentIdx)).Value;
			}

			Line3D entryLine =
				GetNonShortContinuingLine(linestring, segmentIdx, minSegmentLength);

			return entryLine;
		}

		private static Line3D GetNonShortIncomingLine([NotNull] ISegmentList linestring,
		                                              int segmentIdx,
		                                              double minSegmentLength)
		{
			int? usableToSegmentIdx = segmentIdx;

			var entryLine = linestring[segmentIdx];

			int count = 0;
			int maxSegmentCount = linestring.SegmentCount;

			while (usableToSegmentIdx != null &&
			       entryLine.Length2D < minSegmentLength &&
			       count < maxSegmentCount)
			{
				// The entry line is 0 which might result in a wrong direction
				// -> Add the previous segment to the line
				usableToSegmentIdx = linestring.PreviousSegmentIndex(usableToSegmentIdx.Value);

				if (usableToSegmentIdx != null)
				{
					// From the previous' segment's start point to the original intersection
					entryLine = new Line3D(linestring[usableToSegmentIdx.Value].StartPoint,
					                       entryLine.EndPoint);
				}

				count++;
			}

			return entryLine;
		}

		private static Line3D GetNonShortContinuingLine([NotNull] ISegmentList linestring,
		                                                int segmentIdx,
		                                                double minSegmentLength)
		{
			int? usableToSegmentIdx = segmentIdx;

			var entryLine = linestring[segmentIdx];

			int count = 0;
			int maxSegmentCount = linestring.SegmentCount;

			while (usableToSegmentIdx != null &&
			       entryLine.Length2D < minSegmentLength &&
			       count < maxSegmentCount)
			{
				// The entry line is 0 which might result in a wrong direction
				// -> Add the next segment to the line
				usableToSegmentIdx = linestring.NextSegmentIndex(usableToSegmentIdx.Value);

				if (usableToSegmentIdx != null)
				{
					// From the original intersection point to the next segment's end point
					entryLine = new Line3D(entryLine.StartPoint,
					                       linestring[usableToSegmentIdx.Value].EndPoint);
				}

				count++;
			}

			return entryLine;
		}

		private bool CanMakeRing(List<IntersectionRun> fromSubcurves)
		{
			foreach (IntersectionRun intersectionRun in fromSubcurves)
			{
				if (! intersectionRun.RunsAlongSource)
				{
					return true;
				}
			}

			// All run along source: Make a ring if several source parts
			// are involved (e.g. double-touching islands that have been combined,
			// into a bigger island:
			int distinctSourcePartCount =
				fromSubcurves.GroupBy(s => s.NextIntersection.SourcePartIndex).Count();

			if (distinctSourcePartCount > 1)
			{
				RingsCouldContainEachOther = true;
				return true;
			}

			return false;
		}

		#region Source paths intersecting target rings

		private IEnumerable<Linestring> FollowSourcePartThroughTargetRings(
			[NotNull] Linestring sourceLinestring,
			[NotNull] IntersectionPoint3D firstIntersectionInSourcePart,
			bool excludeTargetBoundaryIntersections)
		{
			IntersectionPoint3D linearStart = null;

			if (firstIntersectionInSourcePart.VirtualSourceVertex > 0)
			{
				// The intersection is not at the start of the linestring
				bool? sourceComingFromInside =
					firstIntersectionInSourcePart.SourceArrivesFromRightSide(
						Source, Target, Tolerance);

				if (sourceComingFromInside == true)
				{
					linearStart = IntersectionPoint3D.CreateAreaInteriorIntersection(
						sourceLinestring.StartPoint, 0,
						firstIntersectionInSourcePart.SourcePartIndex);
				}
			}

			IntersectionPoint3D previous;
			IntersectionPoint3D current = firstIntersectionInSourcePart;
			do
			{
				if (linearStart != null)
				{
					yield return GetSourceSubcurve(linearStart, current);

					if (RestartLinearIntersection(sourceLinestring, current, Target,
					                              excludeTargetBoundaryIntersections))
					{
						linearStart = current;
					}
					else
					{
						linearStart = null;
					}
				}
				else
				{
					if (EndLinearIntersection(current, sourceLinestring,
					                          excludeTargetBoundaryIntersections))
					{
						linearStart = current;
					}
				}

				previous = current;
			} while ((current = IntersectionPointNavigator.GetNextIntersection(
				          previous, true, true)) != null);

			IntersectionPoint3D lastIntersection = previous;

			if (linearStart != null &&
			    lastIntersection.VirtualSourceVertex < sourceLinestring.PointCount - 1 &&
			    GeomRelationUtils.PolycurveContainsXY(
				    Target, sourceLinestring.EndPoint, Tolerance))
			{
				// Dangling to the inside:
				var insideEnd =
					IntersectionPoint3D.CreateAreaInteriorIntersection(
						sourceLinestring.EndPoint, sourceLinestring.PointCount - 1,
						linearStart.SourcePartIndex);

				yield return GetSourceSubcurve(linearStart, insideEnd);
			}
		}

		private bool EndLinearIntersection([NotNull] IntersectionPoint3D current,
		                                   [NotNull] Linestring sourceLinestring,
		                                   bool excludeTargetBoundaryIntersections)
		{
			if (current.Type == IntersectionPointType.Crossing)
			{
				return true;
			}

			if (current.Type == IntersectionPointType.LinearIntersectionStart &&
			    ! excludeTargetBoundaryIntersections)
			{
				return true;
			}

			if (current.Type == IntersectionPointType.TouchingInPoint ||
			    current.Type == IntersectionPointType.LinearIntersectionEnd)
			{
				if (current.SourceContinuesInbound(
					    sourceLinestring, Target) == true)
				{
					return true;
				}
			}

			return false;
		}

		private static bool RestartLinearIntersection(
			Linestring sourceLinestring, IntersectionPoint3D intersectionPoint,
			ISegmentList targetRings,
			bool excludeTargetBoundaryIntersections)
		{
			if (intersectionPoint.Type == IntersectionPointType.TouchingInPoint)
			{
				// Touching from the inside, re-start
				return true;
			}

			if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart)
			{
				return ! excludeTargetBoundaryIntersections;
			}

			if (intersectionPoint.Type == IntersectionPointType.Crossing)
			{
				return false;
			}

			if (intersectionPoint.Type == IntersectionPointType.LinearIntersectionEnd)
			{
				// But if it's not the last point, it could continue inbound after the stretch along the boundary
				if (intersectionPoint.VirtualSourceVertex < sourceLinestring.PointCount - 1)
				{
					if (intersectionPoint.SourceContinuesInbound(
						    sourceLinestring, targetRings) == true)
					{
						return true;
					}
				}
			}

			return false;
		}

		#endregion

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

			bool touching = (fromIntersection.Type == IntersectionPointType.TouchingInPoint &&
			                 toIntersection.Type == IntersectionPointType.TouchingInPoint) ||
			                fromIntersection.TargetPartIndex != toIntersection.TargetPartIndex;

			bool preferFullRingToZeroLength = ! touching && source.IsClosed;

			Linestring subcurve = source.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false, preferFullRingToZeroLength);

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

			// If different source parts are involved, they should be touching (otherwise the source is non-simple)
			bool touching = (fromIntersection.Type == IntersectionPointType.TouchingInPoint &&
			                 toIntersection.Type == IntersectionPointType.TouchingInPoint) ||
			                fromIntersection.SourcePartIndex != toIntersection.SourcePartIndex;

			bool preferFullRingToZeroLength = ! touching && target.IsClosed;

			Linestring subcurve = target.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false, ! forward, preferFullRingToZeroLength);

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

		private void RememberUsedTargetParts(IReadOnlyCollection<IntersectionRun> subcurveInfos)
		{
			// Normal case:
			bool targetSubcurveUsed = subcurveInfos.Any(s => ! s.RunsAlongSource);

			// Boundary rings:
			if (subcurveInfos.Any(s => s.IsBoundaryLoop) &&
			    subcurveInfos.All(s => s.RunsAlongTarget))
			{
				// If the finished ring is equal to one of the two boundary loops and the
				// target runs along the finished ring, the target should also be considered used
				targetSubcurveUsed = true;
			}

			if (targetSubcurveUsed)
			{
				// At some point the result must deviate from source otherwise the target does not cut it
				foreach (int targetIdx in subcurveInfos.Select(
					         i => i.NextIntersection.TargetPartIndex))
				{
					IntersectedTargetPartIndexes.Add(targetIdx);
				}
			}
		}

		private void RememberUsedSourceParts(IEnumerable<IntersectionRun> subcurveInfos)
		{
			foreach (IntersectionRun intersectionRun in subcurveInfos)
			{
				if (intersectionRun.RunsAlongSource)
				{
					int sourceIdx = intersectionRun.NextIntersection.SourcePartIndex;
					IntersectedSourcePartIndexes.Add(sourceIdx);
				}
			}
		}

		private void RememberUsedIntersectionRuns(List<IntersectionRun> subcurveInfos)
		{
			foreach (IntersectionRun intersectionRun in subcurveInfos)
			{
				UsedSubcurves.Add(intersectionRun);
			}
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

		private static IEnumerable<IntersectionPoint3D> FilterIntersections(
			[NotNull] IEnumerable<IntersectionPoint3D> intersections,
			[NotNull] HashSet<IntersectionPoint3D> intersectionsToFilter)
		{
			foreach (IntersectionPoint3D intersection in intersections)
			{
				if (intersectionsToFilter.Contains(intersection))
				{
					continue;
				}

				yield return intersection;
			}
		}

		private enum RingRingRelation
		{
			Undefined,
			CongruentSameOrientation,
			CongruentDifferentOrientation,
			IsContained,
			IsNotContained
		}

		private enum TurnDirection
		{
			Left,
			Right
		}
	}
}

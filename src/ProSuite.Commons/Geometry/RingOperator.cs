﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry
{
	public class RingOperator
	{
		// TODO: Remove single-ring operations, get rid of these fields:
		private readonly Linestring _sourceRing;
		private readonly Linestring _target;

		private readonly SubcurveNavigator _subcurveNavigator;

		public RingOperator([NotNull] Linestring sourceRing,
		                    [NotNull] Linestring target,
		                    double tolerance)
			: this(new SingleRingNavigator(sourceRing, target, tolerance))
		{
			Assert.ArgumentCondition(sourceRing.IsClosed, "Source ring must be closed.");

			_sourceRing = sourceRing;
			_target = target;
		}

		public RingOperator(SubcurveNavigator subcurveNavigator)
		{
			_subcurveNavigator = subcurveNavigator;
		}

		/// <summary>
		/// Returns the difference between source and target.
		/// </summary>
		/// <returns>The difference, i.e. source areas that are not part of the target.</returns>
		public MultiLinestring DifferenceXY()
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(_subcurveNavigator.Target.IsClosed, "Target must be closed.");

			// Based on Weiler–Atherton clipping algorithm, added specific logic for linear intersections and multi-parts.

			IList<Linestring> leftRings = GetLeftSideRings();

			// Assign the cut inner rings (anti-clockwise) to un-cut outer rings...

			var unCutOuterRings = _subcurveNavigator.GetNonIntersectedSourceRings()
			                                        .Where(r => r.ClockwiseOriented != false)
			                                        .ToList();

			IList<RingGroup> leftPolys = AssignToResultRingGroups(leftRings, unCutOuterRings);

			var unCutParts =
				unCutOuterRings.Count == 1
					? (MultiLinestring) new RingGroup(unCutOuterRings[0])
					: new MultiPolycurve(unCutOuterRings);

			// Assign the remaining interior rings;
			AssignInteriorRings(leftRings, leftPolys, unCutParts, _subcurveNavigator.Tolerance);

			// Assign the inner rings from the original
			var unCutIslands = _subcurveNavigator.GetNonIntersectedSourceRings()
			                                     .Where(r => r.ClockwiseOriented == false);

			AssignInteriorRings(unCutIslands, leftPolys, unCutParts, _subcurveNavigator.Tolerance);

			// Assign closed cut lines completely contained by an outer ring (and not by an inner ring)
			List<MultiLinestring> results = new List<MultiLinestring>(leftPolys);

			var unusedCutRings =
				_subcurveNavigator.GetNonIntersectedTargets()
				                  .Where(t => t.ClockwiseOriented == true);

			results.Add(unCutParts);

			foreach (Linestring unusedCutRing in unusedCutRings)
			{
				foreach (MultiLinestring resultPoly in results)
				{
					if (resultPoly.IsEmpty)
					{
						continue;
					}

					MultiLinestring updatedUnCut;
					RingGroup cookie;
					TryCutCookie(unusedCutRing, unCutParts, out updatedUnCut, out cookie, true);
				}
			}

			return new MultiPolycurve(results);
		}

		public MultiLinestring UnionXY()
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(_subcurveNavigator.Target.IsClosed, "Target must be closed.");

			IList<Linestring> processedRingsResult = GetBothSideRings();

			List<Linestring> equalRingSelection = GetRingsEqualOtherRing();

			// The non-intersecting outer rings...
			var unprocessedOuterRings =
				GetRingsDisjointFromOtherPoly(r => r.ClockwiseOriented == true).ToList();

			unprocessedOuterRings.AddRange(
				equalRingSelection.Where(r => r.ClockwiseOriented == true));

			// ... can be used where necessary to aggregate the processed inner rings into ring groups
			IList<RingGroup> resultRingGroups =
				AssignToResultRingGroups(processedRingsResult, unprocessedOuterRings);

			var unprocessedParts =
				unprocessedOuterRings.Count == 1
					? (MultiLinestring) new RingGroup(unprocessedOuterRings[0])
					: new MultiPolycurve(unprocessedOuterRings);

			// Assign the remaining interior rings
			AssignInteriorRings(processedRingsResult, resultRingGroups, unprocessedParts,
			                    _subcurveNavigator.Tolerance);

			// Assign the un-intersected inner rings not inside the other input poly:
			List<Linestring> remainingIslands =
				GetRingsDisjointFromOtherPoly(r => r.ClockwiseOriented == false).ToList();

			remainingIslands.AddRange(equalRingSelection.Where(r => r.ClockwiseOriented == false));

			AssignInteriorRings(remainingIslands, resultRingGroups, unprocessedParts,
			                    _subcurveNavigator.Tolerance);

			List<MultiLinestring> results = new List<MultiLinestring>(resultRingGroups);

			results.Add(unprocessedParts);

			return new MultiPolycurve(results);
		}

		/// <summary>
		/// Cuts the source ring using the target and returns separate lists 
		/// of result rings on the left/right side of the cut line.
		/// </summary>
		/// <param name="leftPolys">Result polygons on the left side of the cut line.</param>
		/// <param name="rightPolys">Result polygons on the right side of the cut line.</param>
		/// <param name="clipPolys"></param>
		/// <param name="undefinedSidePolys"></param>
		/// <param name="unCutParts"></param>
		/// <returns>Whether the cut operation was successful or not.</returns>
		public bool CutXY([NotNull] out IList<RingGroup> leftPolys,
		                  [NotNull] out IList<RingGroup> rightPolys,
		                  [NotNull] out IList<RingGroup> clipPolys,
		                  [NotNull] out IList<MultiLinestring> undefinedSidePolys,
		                  [NotNull] out MultiLinestring unCutParts)
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "source must be closed.");

			// Based on Weiler–Atherton clipping algorithm, added specific logic for
			// linear intersections, un-closed target lines and multi-parts.
			// Potential enhancements: Do not insert phantom points!

			IList<Linestring> rightRings = GetRightSideRings();
			IList<Linestring> leftRings = GetLeftSideRings();

			IList<Linestring> duplicates = new List<Linestring>();
			if (! _subcurveNavigator.Target.IsClosed &&
			    _subcurveNavigator.AreIntersectionPointsNonSequential())
			{
				// Cut backs result in duplicates which are both on the left and the right!
				duplicates = RemoveDuplicateRings(leftRings, rightRings);
			}

			// Assign the cut inner rings (anti-clockwise) to un-cut outer rings...
			var unCutOuterRings = _subcurveNavigator.GetNonIntersectedSourceRings()
			                                        .Where(r => r.ClockwiseOriented != false)
			                                        .ToList();

			rightPolys = AssignToResultRingGroups(rightRings, unCutOuterRings);
			leftPolys = AssignToResultRingGroups(leftRings, unCutOuterRings);

			IList<RingGroup> bothSidePolys = AssignToResultRingGroups(duplicates, unCutOuterRings);

			unCutParts =
				unCutOuterRings.Count == 1
					? (MultiLinestring) new RingGroup(unCutOuterRings[0])
					: new MultiPolycurve(unCutOuterRings);

			// Assign the remaining interior rings;
			AssignInteriorRings(rightRings, leftPolys, rightPolys, bothSidePolys, unCutParts,
			                    _subcurveNavigator.Tolerance);
			AssignInteriorRings(leftRings, leftPolys, rightPolys, bothSidePolys, unCutParts,
			                    _subcurveNavigator.Tolerance);
			AssignInteriorRings(duplicates, leftPolys, rightPolys, bothSidePolys, unCutParts,
			                    _subcurveNavigator.Tolerance);

			// Assign the inner rings from the original
			var unCutIslands = _subcurveNavigator.GetNonIntersectedSourceRings()
			                                     .Where(r => r.ClockwiseOriented == false);

			AssignInteriorRings(unCutIslands, leftPolys, rightPolys, bothSidePolys, unCutParts,
			                    _subcurveNavigator.Tolerance);

			// Assign closed cut lines completely contained by an outer ring (and not by an inner ring)
			var unusedCutRings =
				_subcurveNavigator.GetNonIntersectedTargets().Where(t => t.IsClosed);

			undefinedSidePolys = bothSidePolys.Cast<MultiLinestring>().ToList();

			clipPolys = new List<RingGroup>();
			foreach (Linestring unusedCutRing in unusedCutRings)
			{
				MultiLinestring updatedUnCut;
				RingGroup cookie;
				if (! unCutParts.IsEmpty &&
				    TryCutCookie(unusedCutRing, unCutParts, out updatedUnCut, out cookie))
				{
					unCutParts = MultiPolycurve.CreateEmpty();
					undefinedSidePolys.Add(updatedUnCut);
					clipPolys.Add(cookie);
					continue;
				}

				RingGroup updatedCut;
				if (TryCutCookie(unusedCutRing, leftPolys, out updatedCut, out cookie))
				{
					clipPolys.Add(cookie);
					continue;
				}

				if (TryCutCookie(unusedCutRing, rightPolys, out updatedCut, out cookie))
				{
					clipPolys.Add(cookie);
					continue;
				}

				if (TryCutCookie(unusedCutRing, bothSidePolys, out updatedCut, out cookie))
				{
					clipPolys.Add(cookie);
				}
			}

			return (rightPolys.Count > 0 && leftPolys.Count > 0) ||
			       undefinedSidePolys.Count > 1 || clipPolys.Count > 0;
		}

		#region Single ring operations - remove dependency on correct subcurve navigator!

		/// <summary>
		/// Removes the overlap between the source and the target from the source.
		/// - Vertical targets are not supported.
		/// - Counter-clockwise oriented (interior) rings are supported. The counter-clockwise
		///   rings have their 'interior' on the outside, i.e. their overlap 'removed' on the
		///   outside, if the target is also counter-clockwise (i.e. inner rings are unioned).
		/// - If the target is equal to the source in XY, the result is one empty Linestring.
		/// - If the target is disjoint or completely within the source, an empty list is returned.
		/// </summary>
		/// <returns></returns>
		public IList<Linestring> RemoveOverlapXY()
		{
			Assert.True(_target.IsClosed, "The target is not a closed linestring.");

			Assert.NotNull(_target.ClockwiseOriented, "The target is vertical");

			if (SourceEqualsTargetXY())
			{
				// Target equals source in XY - positive rings cancel each other out:
				Linestring resultRing =
					IsExterior(_sourceRing) && IsExterior(_target)
						? Linestring.CreateEmpty()
						: _sourceRing.Clone();

				return new List<Linestring> {resultRing};
			}

			return _target.ClockwiseOriented.Value
				       ? GetLeftSideRings()
				       : GetRightSideRings();
		}

		/// <summary>
		/// Calculates the overlap between the source and the target. If the target is disjoint or within
		/// the source, an empty list is returned.
		/// </summary>
		/// <returns></returns>
		public IList<Linestring> IntersectXY()
		{
			Assert.True(_sourceRing.ClockwiseOriented == true,
			            "The source is not oriented clockwise or it is vertical.");
			Assert.True(_target.IsClosed, "The target is not a closed linestring.");

			Assert.NotNull(_target.ClockwiseOriented, "The target is vertical");

			if (SourceEqualsTargetXY())
			{
				// Target equals source in XY - return source if both are positive:
				Linestring resultRing =
					IsExterior(_sourceRing) && IsExterior(_target)
						? _sourceRing.Clone()
						: Linestring.CreateEmpty();

				return new List<Linestring> {resultRing};
			}

			return _target.ClockwiseOriented.Value
				       ? GetRightSideRings()
				       : GetLeftSideRings();
		}

		/// <summary>
		/// Cuts the source ring using the target and returns separate lists 
		/// of result rings on the left/right side of the cut line.
		/// </summary>
		/// <param name="leftRings">Result rings on the left side of the cut line.</param>
		/// <param name="rightRings">Result rings on the right side of the cut line.</param>
		/// <returns>Whether the cut operation was successful or not.</returns>
		public bool CutXY([NotNull] out IList<Linestring> leftRings,
		                  [NotNull] out IList<Linestring> rightRings)
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "source must be closed.");

			// Based on Weiler–Atherton clipping algorithm, added specific logic for linear intersections.

			rightRings = GetRightSideRings();
			leftRings = GetLeftSideRings();

			return rightRings.Count > 0 && leftRings.Count > 0;
		}

		#endregion

		private static void AssignInteriorRings([NotNull] IEnumerable<Linestring> interiorRings,
		                                        [NotNull] IList<RingGroup> leftPolys,
		                                        [NotNull] IList<RingGroup> rightPolys,
		                                        [NotNull] IList<RingGroup> bothSidePolys,
		                                        [NotNull] MultiLinestring unCutParts,
		                                        double tolerance)
		{
			foreach (Linestring unCutIsland in interiorRings)
			{
				// Assuming no conflicts (island within island) between un-cut islands because
				// they all come from the same source.
				int assignmentCount = 0;
				assignmentCount +=
					AssignInteriorRing(unCutIsland, rightPolys, tolerance);

				assignmentCount +=
					AssignInteriorRing(unCutIsland, leftPolys, tolerance);

				assignmentCount +=
					AssignInteriorRing(unCutIsland, bothSidePolys, tolerance);

				if (assignmentCount == 0)
				{
					unCutParts.AddLinestring(unCutIsland);
				}

				Assert.True(assignmentCount < 2, "Multiple inner ring assignments!");
			}
		}

		private static void AssignInteriorRings([NotNull] IEnumerable<Linestring> interiorRings,
		                                        [NotNull] ICollection<RingGroup> polygons,
		                                        [NotNull] MultiLinestring unassignedParts,
		                                        double tolerance)
		{
			foreach (Linestring interiorRing in interiorRings)
			{
				// Assuming no conflicts (island within island) between un-cut islands because
				// they all come from the same source.
				int assignmentCount =
					AssignInteriorRing(interiorRing, polygons, tolerance);

				if (assignmentCount == 0)
				{
					unassignedParts.AddLinestring(interiorRing);
				}

				Assert.True(assignmentCount < 2, "Multiple inner ring assignments!");
			}
		}

		private static int AssignInteriorRing([NotNull] Linestring interiorRing,
		                                      IEnumerable<RingGroup> resultPolys,
		                                      double tolerance)
		{
			int assignmentCount = 0;
			foreach (RingGroup resultPoly in resultPolys)
			{
				if (GeomRelationUtils.PolycurveContainsXY(
					resultPoly.ExteriorRing, interiorRing.StartPoint, tolerance))
				{
					resultPoly.AddInteriorRing(interiorRing);
					assignmentCount++;
				}
			}

			return assignmentCount;
		}

		/// <summary>
		/// Creates the list of result ring groups from the processed inner/outer rings and the
		/// unprocessed outer rings by:
		/// - Adding processed outer rings to the output and remove them from the input collection
		/// - For each processed inner rings, that is contained in an un-processed outer ring:
		///   - Add the unprocessed outer ring together with the inner ring to the result.
		///   - Remove both the unprocessed outer and the inner ring from the respective input collection. 
		/// </summary>
		/// <param name="processedResultRings"></param>
		/// <param name="unprocessedOuterRings"></param>
		/// <returns></returns>
		private IList<RingGroup> AssignToResultRingGroups(
			ICollection<Linestring> processedResultRings,
			ICollection<Linestring> unprocessedOuterRings)
		{
			var result = new List<RingGroup>();

			foreach (Linestring processedResultRing in processedResultRings.ToList())
			{
				if (processedResultRing.ClockwiseOriented != false)
				{
					result.Add(new RingGroup(processedResultRing));
					processedResultRings.Remove(processedResultRing);
				}
				else
				{
					// Intersected (processed) inner rings:
					// Find the containing un-cut outer ring, assign and remove from un-processed list
					Linestring containing = unprocessedOuterRings.FirstOrDefault(
						o => o.ClockwiseOriented == true &&
						     GeomRelationUtils.PolycurveContainsXY(
							     o, processedResultRing.StartPoint, _subcurveNavigator.Tolerance));

					if (containing != null)
					{
						unprocessedOuterRings.Remove(containing);

						// Add at the beginning to boost performance, assuming a few (or one) large rings contains everything
						result.Insert(0, new RingGroup(containing, new[] {processedResultRing}));

						// remove from the list, the remaining inner rings will be assigned afterwards;
						processedResultRings.Remove(processedResultRing);
					}
				}
			}

			return result;
		}

		private IList<Linestring> RemoveDuplicateRings(
			IList<Linestring> leftRings,
			IList<Linestring> rightRings)
		{
			var ringComparer = new RingComparer(_subcurveNavigator.Tolerance);

			var duplicates = new List<Linestring>();

			foreach (Linestring leftRing in leftRings)
			{
				if (rightRings.Contains(leftRing, ringComparer))
				{
					duplicates.Add(leftRing);
				}
			}

			foreach (Linestring duplicate in duplicates)
			{
				leftRings.Remove(duplicate);
				rightRings.Remove(duplicate);
			}

			return duplicates;
		}

		private bool SourceEqualsTargetXY()
		{
			return _subcurveNavigator.IntersectionPoints.Count == 2 &&
			       _subcurveNavigator.IntersectionPoints[0]
			                         .IsLinearIntersectionStartAtStartPoint(_sourceRing) &&
			       _subcurveNavigator.IntersectionPoints[1]
			                         .IsLinearIntersectionEndAtEndPoint(_sourceRing);
		}

		private static bool IsExterior(Linestring ring)
		{
			return ring.ClockwiseOriented == true;
		}

		/// <summary>
		/// Returns the 'union' of the intersecting input rings, i.e. following the subcurves
		/// by using a left turn at intersections.
		/// </summary>
		/// <returns></returns>
		private IList<Linestring> GetBothSideRings()
		{
			SubcurveNavigator.TurnDirection originalTurnDirection =
				_subcurveNavigator.PreferredTurnDirection;

			try
			{
				_subcurveNavigator.PreferredTurnDirection = SubcurveNavigator.TurnDirection.Left;

				return _subcurveNavigator.FollowSubcurvesClockwise(
					_subcurveNavigator.IntersectionsInboundTarget.ToList());
			}
			finally
			{
				_subcurveNavigator.PreferredTurnDirection = originalTurnDirection;
			}
		}

		private IList<Linestring> GetLeftSideRings()
		{
			return _subcurveNavigator.FollowSubcurvesClockwise(
				_subcurveNavigator.IntersectionsInboundTarget.ToList());
		}

		private IList<Linestring> GetRightSideRings()
		{
			return _subcurveNavigator.FollowSubcurvesClockwise(
				_subcurveNavigator.IntersectionsOutboundTarget.ToList());
		}

		private IList<Tuple<Linestring, Linestring>> GetEqualBoundaryRingPairs()
		{
			var boundaryIntersectingRings = new List<Tuple<Linestring, Linestring>>(0);

			// W.r.t. AreaContainsXY returning null: if the start point is on the boundary there
			// are duplicate rings (otherwise they would be 'intesected' and we would not get here.

			for (int j = 0; j < _subcurveNavigator.Source.PartCount; j++)
			{
				var sourceIntersections = _subcurveNavigator
				                          .IntersectionPoints.Where(ip => ip.SourcePartIndex == j)
				                          .ToList();

				if (sourceIntersections.Count != 2)
				{
					continue;
				}

				if (sourceIntersections[0].TargetPartIndex !=
				    sourceIntersections[1].TargetPartIndex)
				{
					continue;
				}

				if (sourceIntersections[0].Type != IntersectionPointType.LinearIntersectionStart ||
				    sourceIntersections[1].Type != IntersectionPointType.LinearIntersectionEnd)
				{
					continue;
				}

				if (! sourceIntersections[0].Point.EqualsXY(
					    sourceIntersections[1].Point,
					    _subcurveNavigator.Tolerance))
				{
					continue;
				}

				// If there are exactly 2 intersection points both linear start/end in the same point:
				// The ring is covered by the respective target index
				Linestring sourceRing =
					_subcurveNavigator.Source.GetPart(sourceIntersections[0].SourcePartIndex);
				Linestring targetRing =
					_subcurveNavigator.Target.GetPart(sourceIntersections[0].TargetPartIndex);

				boundaryIntersectingRings.Add(
					new Tuple<Linestring, Linestring>(sourceRing, targetRing));
			}

			return boundaryIntersectingRings;
		}

		/// <summary>
		/// Returns source rings disjoint from target and target rings disjoint from source.
		/// </summary>
		/// <param name="ringPredicate"></param>
		/// <returns></returns>
		private IEnumerable<Linestring> GetRingsDisjointFromOtherPoly(
			Predicate<Linestring> ringPredicate)
		{
			var result = new List<Linestring>();

			// W.r.t. AreaContainsXY returning null: if the start point is on the boundary there
			// are duplicate rings which need to be identified elsewhere.
			ISegmentList target = _subcurveNavigator.Target;
			foreach (Linestring ring in _subcurveNavigator.GetNonIntersectedSourceRings()
			                                              .Where(r => ringPredicate(r)))
			{
				bool? contains = GeomRelationUtils.AreaContainsXY(
					target, ring.StartPoint, _subcurveNavigator.Tolerance);

				if (contains == false)
				{
					// disjoint
					result.Add(ring);
				}
			}

			ISegmentList source = _subcurveNavigator.Source;
			foreach (Linestring ring in _subcurveNavigator.GetNonIntersectedTargets()
			                                              .Where(r => ringPredicate(r)))
			{
				bool? contains = GeomRelationUtils.AreaContainsXY(
					source, ring.StartPoint, _subcurveNavigator.Tolerance);

				if (contains == false)
				{
					result.Add(ring);
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the source ring where an XY-equal ring in the target exists with the same
		/// orientation. Non-equal oriented source-target ring pairs cancel each other out.
		/// </summary>
		/// <returns></returns>
		private List<Linestring> GetRingsEqualOtherRing()
		{
			var equalRingSelection = new List<Linestring>();

			IList<Tuple<Linestring, Linestring>> equalBoundaryRingPairs =
				GetEqualBoundaryRingPairs();

			foreach (Tuple<Linestring, Linestring> equalBoundaryRingPair in equalBoundaryRingPairs)
			{
				Linestring sourceRing = equalBoundaryRingPair.Item1;
				Linestring targetRing = equalBoundaryRingPair.Item2;

				if (sourceRing.ClockwiseOriented != targetRing.ClockwiseOriented)
				{
					continue;
				}

				equalRingSelection.Add(sourceRing);
			}

			return equalRingSelection;
		}

		#region Cookie cutting

		private bool TryCutCookie<T>(Linestring cutRing,
		                             IList<T> origPolygons,
		                             out T updatedOriginal,
		                             out RingGroup innerResult) where T : MultiLinestring
		{
			updatedOriginal = null;
			innerResult = null;

			foreach (T origPolygon in origPolygons.ToList())
			{
				if (TryCutCookie(cutRing, origPolygon, out updatedOriginal, out innerResult))
				{
					return true;
				}
			}

			return false;
		}

		private bool TryCutCookie<T>(Linestring cutRing, T origPolygon, out T updatedOriginal,
		                             out RingGroup innerResult,
		                             bool allowEmptyResults = false) where T : MultiLinestring
		{
			updatedOriginal = null;

			if (TryCutCookie(origPolygon, cutRing, _subcurveNavigator.Tolerance, out innerResult,
			                 allowEmptyResults))
			{
				updatedOriginal = origPolygon;
				return true;
			}

			return false;
		}

		private static bool TryCutCookie<T>([NotNull] T polygon,
		                                    [NotNull] Linestring cookieCutter,
		                                    double tolerance,
		                                    out RingGroup resultCookie,
		                                    bool allowEmptyResults = false)
			where T : MultiLinestring
		{
			resultCookie = null;

			if (! GeomRelationUtils.PolycurveContainsXY(
				    polygon, cookieCutter, tolerance))
			{
				return false;
			}

			// Remove pre-existing interior rings that are completely within cookie cutter
			List<Linestring> containedExistingIslands =
				RemoveContainedExistingIslands(polygon, cookieCutter, tolerance);

			Linestring interiorRing = cookieCutter.Clone();

			Assert.True(interiorRing.IsClosed, "Interior ring is not closed");

			interiorRing.TryOrientAnticlockwise();

			IntersectionPoint3D outerRingIntersection;
			bool ringsAreEqual;
			int parentRingIdx = GetContainingRingIndex(
				polygon, interiorRing, tolerance, out ringsAreEqual, out outerRingIntersection);

			Assert.False(parentRingIdx < 0, "No parent ring found");

			Linestring containingRing = polygon.GetLinestring(parentRingIdx);

			if (containingRing.ClockwiseOriented == false)
			{
				// The cutter is completely within an existing island -> ignore (keep existing ring)
				return false;
			}

			if (ringsAreEqual)
			{
				// The cutter is equal to the found ring. Positive rings cancel each other out:
				if (! allowEmptyResults)
				{
					return false;
				}

				polygon.RemoveLinestring(containingRing);
			}
			else if (outerRingIntersection == null)
			{
				// The cutter is completely within an existing outer ring -> add as island
				polygon.AddLinestring(interiorRing);
			}
			else
			{
				// create boundary loop:
				polygon.RemoveLinestring(containingRing);

				Linestring withBoundaryLoop = CreateWithBoundaryLoop(containingRing, interiorRing,
				                                                     outerRingIntersection,
				                                                     tolerance);

				polygon.InsertLinestring(parentRingIdx, withBoundaryLoop);
			}

			resultCookie = RingGroup.CreateProperlyOriented(cookieCutter.Clone());

			foreach (Linestring unusedCutRing in containedExistingIslands)
			{
				RingGroup cookieInCookie;
				Assert.True(TryCutCookie(resultCookie, unusedCutRing, tolerance,
				                         out cookieInCookie),
				            "Inner ring in cookie cutter cannot be cut from result cookie");
			}

			return true;
		}

		private static List<Linestring> RemoveContainedExistingIslands<T>(
			[NotNull] T fromPolygon,
			[NotNull] Linestring cookieCutter,
			double tolerance) where T : MultiLinestring
		{
			List<Linestring> existingInnerRings = fromPolygon
			                                      .GetLinestrings()
			                                      .Where(l => l.ClockwiseOriented == false)
			                                      .ToList();

			Assert.NotNull(cookieCutter.ClockwiseOriented);

			var containedExistingIslands = new List<Linestring>();

			foreach (Linestring existingInnerRing in existingInnerRings)
			{
				bool? islandWithinCookie = GeomRelationUtils.AreaContainsXY(
					cookieCutter, existingInnerRing.StartPoint, tolerance, true);

				// TODO: handle the case where the inner ring touches cutter in StartPoint
				if (islandWithinCookie == true)
				{
					containedExistingIslands.Add(existingInnerRing);
					fromPolygon.RemoveLinestring(existingInnerRing);
				}
			}

			return containedExistingIslands;
		}

		private static Linestring CreateWithBoundaryLoop(Linestring containingSourceRing,
		                                                 Linestring touchingInteriorRing,
		                                                 IntersectionPoint3D touchIntersection,
		                                                 double tolerance)
		{
			double sourceTouchPointRatio;
			int sourceTouchSegmentIdx =
				touchIntersection.GetLocalSourceIntersectionSegmentIdx(
					containingSourceRing, out sourceTouchPointRatio);
			List<Linestring> subcurves = new List<Linestring>();
			subcurves.Add(containingSourceRing.GetSubcurve(
				              0, 0, sourceTouchSegmentIdx, sourceTouchPointRatio,
				              false));

			double targetTouchPointRatio;
			int targetTouchSegmentIdx =
				touchIntersection.GetLocalTargetIntersectionSegmentIdx(
					touchingInteriorRing, out targetTouchPointRatio);

			subcurves.Add(touchingInteriorRing.GetSubcurve(
				              targetTouchSegmentIdx, targetTouchPointRatio, false,
				              true));

			subcurves.Add(containingSourceRing.GetSubcurve(
				              sourceTouchSegmentIdx, sourceTouchPointRatio,
				              containingSourceRing.SegmentCount - 1, 1, false));

			Linestring withBoundaryLoop =
				GeomTopoOpUtils.MergeConnectedLinestrings(subcurves, null, tolerance);
			return withBoundaryLoop;
		}

		private static int GetContainingRingIndex([NotNull] MultiLinestring polygon,
		                                          [NotNull] Linestring containedRing,
		                                          double tolerance,
		                                          out bool ringsAreEqual,
		                                          out IntersectionPoint3D touchPoint)
		{
			IList<IntersectionPoint3D> intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints(polygon, containedRing, tolerance);

			ringsAreEqual = false;
			touchPoint = null;

			// Equal to outer ring -> removes outer ring in original
			// or equal to inner ring -> ignore cookie cutter
			if (intersectionPoints.Count == 2 &&
			    intersectionPoints[0].SourcePartIndex == intersectionPoints[1].SourcePartIndex &&
			    intersectionPoints[0].Point.Equals(intersectionPoints[1].Point) &&
			    intersectionPoints[0].Type == IntersectionPointType.LinearIntersectionStart &&
			    intersectionPoints[1].Type == IntersectionPointType.LinearIntersectionEnd)
			{
				ringsAreEqual = true;
				return intersectionPoints[0].SourcePartIndex;
			}

			var outerRingIntersections =
				intersectionPoints
					.Where(i => polygon.GetLinestring(i.SourcePartIndex).ClockwiseOriented == true)
					.ToList();

			// Touching outer ring in one point -> boundary loop in original
			if (outerRingIntersections.Count > 0)
			{
				Assert.True(outerRingIntersections.Count < 2,
				            "Unexpected number of touching points.");

				touchPoint = outerRingIntersections[0];

				return touchPoint.SourcePartIndex;
			}

			// Inside an inner ring -> ignore cookie cutter
			for (int i = 0; i < polygon.PartCount; i++)
			{
				Linestring ring = polygon.GetLinestring(i);

				if (ring.ClockwiseOriented == true)
				{
					continue;
				}

				int currentIdx = i;

				bool? areaContainsXY = GeomRelationUtils.AreaContainsXY(
					ring, containedRing,
					intersectionPoints.Where(ip => ip.SourcePartIndex == currentIdx),
					tolerance, true);

				if (areaContainsXY == true)
				{
					return i;
				}
			}

			// Inside an outer ring but not an inner ring: Add as island
			for (int i = 0; i < polygon.PartCount; i++)
			{
				Linestring ring = polygon.GetLinestring(i);

				if (ring.ClockwiseOriented == false)
				{
					continue;
				}

				if (GeomRelationUtils.AreaContainsXY(ring, containedRing.StartPoint,
				                                     tolerance) == true)
				{
					return i;
				}
			}

			return -1;
		}

		#endregion

		private class RingComparer : IEqualityComparer<Linestring>
		{
			private readonly double _tolerance;

			public RingComparer(double tolerance)
			{
				_tolerance = tolerance;
			}

			public bool Equals(Linestring x, Linestring y)
			{
				if (x == null && y == null)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				return GeomTopoOpUtils.AreEqualXY(x, y, _tolerance);
			}

			public int GetHashCode(Linestring linestring)
			{
				return 31 * linestring.SegmentCount +
				       (linestring.ClockwiseOriented == true).GetHashCode();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class RingOperator
	{
		private readonly SubcurveNavigator _subcurveNavigator;

		public RingOperator(SubcurveNavigator subcurveNavigator)
		{
			_subcurveNavigator = subcurveNavigator;
		}

		public RingOperator([NotNull] ISegmentList source,
		                    [NotNull] ISegmentList target,
		                    double tolerance)
			: this(new SubcurveNavigator(source, target, tolerance)) { }

		/// <summary>
		/// Returns the difference between source and target.
		/// </summary>
		/// <returns>The difference, i.e. source areas that are not part of the target.</returns>
		public MultiLinestring IntersectXY()
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(_subcurveNavigator.Target.IsClosed, "Target must be closed.");

			// Based on Weiler–Atherton clipping algorithm, added specific logic for linear intersections and multi-parts.
			IList<Linestring> rightRings = GetRightSideRings(true, true);

			// Build the result polygons from the outer rings:
			List<RingGroup> resultPolys = ExtractOuterRings(rightRings);

			var containedTargetRings =
				_subcurveNavigator.GetTargetRingsCompletelyWithinSource().ToList();

			resultPolys.AddRange(ExtractOuterRings(containedTargetRings));

			// Now assign the left over inner rings from the rightRings (cut rings and source islands inside the target)
			// and from the containedTargetRings (i.e. target islands inside the source).
			var unassignedInnerRings = new MultiPolycurve(new List<Linestring>(0));
			AssignInteriorRings(rightRings, resultPolys, unassignedInnerRings,
			                    _subcurveNavigator.Tolerance);
			AssignInteriorRings(containedTargetRings, resultPolys, unassignedInnerRings,
			                    _subcurveNavigator.Tolerance);

			// Assign closed cut lines completely contained by an outer ring (and not by an inner ring)
			List<MultiLinestring> results = new List<MultiLinestring>(resultPolys);

			return new MultiPolycurve(results);
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

			IList<Linestring> leftRings = GetLeftSideRings(true, true);

			// Build the result polygons from the outer rings:
			List<RingGroup> resultPolys = ExtractOuterRings(leftRings);

			var containedTargetRings =
				_subcurveNavigator.GetTargetRingsCompletelyWithinSource().Select(r => r.Clone())
				                  .ToList();

			foreach (Linestring containedTargetRing in containedTargetRings)
			{
				containedTargetRing.ReverseOrientation();
			}

			resultPolys.AddRange(ExtractOuterRings(containedTargetRings));

			// Now assign the left over inner rings from the leftRings (cut rings and source islands outside the target)
			// and from the containedTargetRings (i.e. target islands inside the source).
			var unassignedInnerRings = new MultiPolycurve(new List<Linestring>(0));
			AssignInteriorRings(leftRings, resultPolys, unassignedInnerRings,
			                    _subcurveNavigator.Tolerance,
			                    _subcurveNavigator.RingsCouldContainEachOther);

			AssignInteriorRings(containedTargetRings, resultPolys, unassignedInnerRings,
			                    _subcurveNavigator.Tolerance,
			                    _subcurveNavigator.RingsCouldContainEachOther);

			List<MultiLinestring> results = new List<MultiLinestring>(resultPolys);

			return new MultiPolycurve(results);
		}

		public MultiLinestring UnionXY()
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(_subcurveNavigator.Target.IsClosed, "Target must be closed.");

			IList<Linestring> processedRingsResult =
				_subcurveNavigator.FollowSubcurvesTurningLeft();

			_subcurveNavigator.DetermineExtraRingRelations(
				out IList<Linestring> equalRings,
				out IList<Linestring> ringsOutsideOtherPoly);

			// The non-intersecting outer rings...
			var unprocessedOuterRings =
				ringsOutsideOtherPoly.Where(r => r.ClockwiseOriented == true).ToList();

			unprocessedOuterRings.AddRange(
				equalRings.Where(r => r.ClockwiseOriented == true));

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
				ringsOutsideOtherPoly.Where(r => r.ClockwiseOriented == false).ToList();

			remainingIslands.AddRange(equalRings.Where(r => r.ClockwiseOriented == false));

			AssignInteriorRings(remainingIslands, resultRingGroups, unprocessedParts,
			                    _subcurveNavigator.Tolerance);

			if (unprocessedParts.Count > 0)
			{
				AddUnprocessedRings(unprocessedParts, resultRingGroups);
			}

			var result = new MultiPolycurve(resultRingGroups);

			// Guard for TOP-5727:
			if (_subcurveNavigator.IntersectionPointNavigator.PotentiallyNonSimple)
			{
				AssertSimple(result);
			}

			// TOP-5731: Guard for general Barrel Roof 'eternal footprint' types: Count outer rings
			//           because inner rings can be created by combining outer rings (2 bananas)
			int resultOuterRingCount = result.PartCount -
			                           result.GetLinestrings()
			                                 .Count(r => r.ClockwiseOriented == false);

			int resultOuterRingMaxCountTheoreticalMax =
				GetExteriorRingCount(_subcurveNavigator.Source) +
				GetExteriorRingCount(_subcurveNavigator.Target) +
				_subcurveNavigator.GetBoundaryLoopCount();

			if (resultOuterRingCount > resultOuterRingMaxCountTheoreticalMax)
			{
				throw new AssertionException(
					"Failure to calculate union. The input is likely non-simple");
			}

			return result;
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

			_subcurveNavigator.PrepareForCutOperation();

			IList<Linestring> rightRings = GetRightSideRings();
			IList<Linestring> leftRings = GetLeftSideRings();

			IList<Linestring> duplicates = new List<Linestring>();
			if (! _subcurveNavigator.Target.IsClosed &&
			    _subcurveNavigator.AreIntersectionPointsNonSequential())
			{
				// Delete this, when no assertion is thrown ever again...
				// Cut backs and non-planar cut lines result in duplicates which are both on the
				// left and the right! -> Planarize cut lines first (TOP-)!
				duplicates = RemoveDuplicateRings(leftRings, rightRings);
				Assert.AreEqual(0, duplicates.Count,
				                "Duplicate results. Make sure the input is simple.");
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

				if (TryCutCookie(unusedCutRing, leftPolys, out RingGroup _, out cookie))
				{
					clipPolys.Add(cookie);
					continue;
				}

				if (TryCutCookie(unusedCutRing, rightPolys, out RingGroup _, out cookie))
				{
					clipPolys.Add(cookie);
					continue;
				}

				if (TryCutCookie(unusedCutRing, bothSidePolys, out RingGroup _, out cookie))
				{
					clipPolys.Add(cookie);
				}
			}

			return rightPolys.Count > 0 && leftPolys.Count > 0 ||
			       undefinedSidePolys.Count > 1 ||
			       clipPolys.Count > 0;
		}

		#region Single ring operations

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

			// Based on Weiler–Atherton clipping algorithm, with added logic for extra features,
			// such as Z-value selection, linear intersections, avoiding duplicate rings in cut.

			_subcurveNavigator.PrepareForCutOperation();

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
		                                        double tolerance,
		                                        bool checkRelationToExistingInteriorRings = false)
		{
			foreach (Linestring interiorRing in interiorRings)
			{
				// Assuming no conflicts (island within island) between un-cut islands because
				// they all come from the same source.
				int assignmentCount =
					AssignInteriorRing(interiorRing, polygons, tolerance,
					                   checkRelationToExistingInteriorRings);

				if (assignmentCount == 0)
				{
					unassignedParts.AddLinestring(interiorRing);
				}

				Assert.True(assignmentCount < 2, "Multiple inner ring assignments!");
			}
		}

		private static int AssignInteriorRing([NotNull] Linestring interiorRing,
		                                      ICollection<RingGroup> resultPolys,
		                                      double tolerance,
		                                      bool checkRelationToExistingInteriorRings = false)
		{
			RingGroup assignedPoly = GetContainingRingGroup(interiorRing, resultPolys, tolerance);

			// This is the fast option that works if the inputs are simple and no touching rings
			// from the same geometry turn into a new ring that contains some other ring.

			if (assignedPoly == null)
			{
				return 0;
			}

			// TODO: If several result polys contain the interior ring, choose the smallest

			// This is the no-shortcuts option:
			if (checkRelationToExistingInteriorRings)
			{
				// Full assessment including inner rings:
				bool? newRingIsContained =
					GeomRelationUtils.IsContainedXY(interiorRing, assignedPoly, tolerance);

				// The new, to-be-assigned interior ring is inside an existing interior ring
				// (i.e. outside) -> do not assign it
				if (newRingIsContained == false)
				{
					return 0;
				}

				// It could also be that the result already has a ring that is contained by
				// the new ring (add unit test!) -> remove the existing ring.
				foreach (Linestring existingInteriorRing in assignedPoly.InteriorRings)
				{
					if (RingContains(interiorRing, existingInteriorRing, tolerance))
					{
						// replace the existing interior ring
						assignedPoly.RemoveLinestring(existingInteriorRing);
					}
				}
			}

			assignedPoly.AddInteriorRing(interiorRing);

			return 1;
		}

		private void AddUnprocessedRings(MultiLinestring unprocessed,
		                                 ICollection<RingGroup> resultRingGroups)
		{
			if (! _subcurveNavigator.HasBoundaryLoops() &&
			    ! _subcurveNavigator.RingsCouldContainEachOther)
			{
				foreach (var linestring in unprocessed.GetLinestrings())
				{
					resultRingGroups.Add(new RingGroup(linestring));
				}

				return;
			}

			// By now we know that the rings are not cutting each other, but extra checks are
			// necessary because the unprocessed parts could be inside a processed part or within
			// another unprocessed part.
			foreach (var unprocessedRing in unprocessed.GetLinestrings()
			                                           .Where(l => l.ClockwiseOriented != false)
			                                           .OrderByDescending(l => l.GetArea2D()))
			{
				if (resultRingGroups.All(r => GeomRelationUtils.AreaContainsXY(
					                              r, unprocessedRing,
					                              _subcurveNavigator.Tolerance) == false))
				{
					// Not contained by any result ring: keep it
					resultRingGroups.Add(new RingGroup(unprocessedRing));
				}
			}

			// Assign the unprocessed interior rings:
			MultiLinestring nonassignable = MultiPolycurve.CreateEmpty();

			AssignInteriorRings(
				unprocessed.GetLinestrings().Where(l => l.ClockwiseOriented == false),
				resultRingGroups, nonassignable, _subcurveNavigator.Tolerance);

			Assert.AreEqual(0, nonassignable.PartCount,
			                "Not all interior rings could be assigned.");
		}

		private static RingGroup GetContainingRingGroup(
			[NotNull] Linestring interiorRing,
			[NotNull] ICollection<RingGroup> candidateRingGroups,
			double tolerance)
		{
			List<RingGroup> allContaining = new List<RingGroup>(1);

			foreach (RingGroup candidatePoly in candidateRingGroups)
			{
				if (RingContains(candidatePoly.ExteriorRing, interiorRing, tolerance))
				{
					allContaining.Add(candidatePoly);
				}
			}

			// For better performance:
			if (allContaining.Count == 1)
			{
				return allContaining[0];
			}

			return allContaining.MinElementOrDefault(r => r.ExteriorRing.GetArea2D());
		}

		/// <summary>
		/// Determines whether the specified interior ring which is known not to cross but only
		/// touch the exterior ring or be completely inside or disjoint. 
		/// </summary>
		/// <param name="exteriorRing"></param>
		/// <param name="unCutInteriorRing"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		private static bool RingContains([NotNull] Linestring exteriorRing,
		                                 [NotNull] Linestring unCutInteriorRing,
		                                 double tolerance)
		{
			foreach (Pnt3D interiorRingPoint in unCutInteriorRing.GetPoints())
			{
				bool? contained =
					GeomRelationUtils.AreaContainsXY(exteriorRing, interiorRingPoint, tolerance,
					                                 true);

				if (contained != null)
				{
					return contained.Value;
				}
			}

			return false;
		}

		/// <summary>
		/// Creates the list of result ring groups with the rings from the collection of input
		/// rings that have positive orientation (outer rings). They are removed from the input
		/// list.
		/// </summary>
		/// <param name="fromRings"></param>
		/// <returns></returns>
		private static List<RingGroup> ExtractOuterRings(
			[NotNull] ICollection<Linestring> fromRings)
		{
			var result = new List<RingGroup>();

			foreach (Linestring processedResultRing in fromRings.ToList())
			{
				if (processedResultRing.ClockwiseOriented != false)
				{
					result.Add(new RingGroup(processedResultRing));
					fromRings.Remove(processedResultRing);
				}
			}

			return result;
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
						result.Insert(0, new RingGroup(containing, new[] { processedResultRing }));

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

		private IList<Linestring> GetLeftSideRings(bool includeEqualRings = false,
		                                           bool includeNotContained = false)
		{
			List<IntersectionPoint3D> startPoints =
				_subcurveNavigator.LeftSideRingStartIntersections.ToList();

			IList<Linestring> result = _subcurveNavigator.FollowSubcurvesClockwise(startPoints);

			if (includeEqualRings || includeNotContained)
			{
				foreach (Linestring uncutSourceRing in
				         _subcurveNavigator.GetUnprocessedSourceParts(includeEqualRings, false,
					         false, includeNotContained))
				{
					result.Add(uncutSourceRing);
				}
			}

			return result;
		}

		private IList<Linestring> GetRightSideRings(bool includeEqualRings = false,
		                                            bool includeContainedSourceRings = false)
		{
			List<IntersectionPoint3D> startPoints =
				_subcurveNavigator.RightSideRingStartIntersections.ToList();

			IList<Linestring> result = _subcurveNavigator.FollowSubcurvesClockwise(startPoints);

			if (! includeEqualRings && ! includeContainedSourceRings)
			{
				return result;
			}

			foreach (Linestring uncutSourceRing in
			         _subcurveNavigator.GetUnprocessedSourceParts(
				         includeEqualRings, true,
				         includeContainedSourceRings, false))
			{
				result.Add(uncutSourceRing);
			}

			return result;
		}

		private void AssertSimple(MultiLinestring result)
		{
			foreach (Linestring linestring in result.GetLinestrings())
			{
				if (GeomTopoOpUtils.GetLinearSelfIntersectionsXY(
					    linestring, _subcurveNavigator.Tolerance).Any())
				{
					throw new InvalidOperationException(
						"Result has self-intersections. The input geometries are likely " +
						"non-simple with respect to tolerance.");
				}
			}
		}

		private static int GetExteriorRingCount(ISegmentList areaGeometry)
		{
			int result = 0;

			for (int i = 0; i < areaGeometry.PartCount; i++)
			{
				if (areaGeometry.GetPart(i).ClockwiseOriented != false)

				{
					result++;
				}
			}

			return result;
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

			if (false == GeomRelationUtils.AreaContainsXY(
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
				Assert.True(TryCutCookie(resultCookie, unusedCutRing, tolerance, out _),
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
				              false, false));

			double targetTouchPointRatio;
			int targetTouchSegmentIdx =
				touchIntersection.GetLocalTargetIntersectionSegmentIdx(
					touchingInteriorRing, out targetTouchPointRatio);

			subcurves.Add(touchingInteriorRing.GetSubcurve(
				              targetTouchSegmentIdx, targetTouchPointRatio, false,
				              true));

			subcurves.Add(containingSourceRing.GetSubcurve(
				              sourceTouchSegmentIdx, sourceTouchPointRatio,
				              containingSourceRing.SegmentCount - 1, 1,
				              false, false));

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
				GeomTopoOpUtils.GetIntersectionPoints((ISegmentList) polygon, containedRing,
				                                      tolerance);

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

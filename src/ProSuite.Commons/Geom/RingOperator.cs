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
		private static double ToleranceFactor => Math.Sqrt(2);

		private readonly SubcurveNavigator _subcurveNavigator;

		public RingOperator(SubcurveNavigator subcurveNavigator)
		{
			_subcurveNavigator = subcurveNavigator;
		}

		public RingOperator([NotNull] ISegmentList source,
		                    [NotNull] ISegmentList target,
		                    double tolerance)
			: this(new SubcurveNavigator(source, target, tolerance)) { }

		// TODO: Test with true for all cases, consider implementation without geometry updates and remove.
		public bool AllowPointClustering { get; set; }

		/// <summary>
		/// Distance (typically the data resolution) within which two near-coincident, near-
		/// parallel edge runs of source and target - bounded by two point-touches and enclosing
		/// a sub-resolution sliver - are snapped together so the navigator sees a clean linear
		/// intersection instead of two touch points it cannot walk. Only takes effect when
		/// greater than the navigator tolerance; 0 (default) disables the parallel-run snap and
		/// preserves the legacy behavior. See <see cref="SnapNearCoincidentParallelRuns"/>.
		/// </summary>
		public double MergeTolerance { get; set; }

		/// <summary>
		/// Whether a degenerate union walk result (see
		/// <see cref="SubcurveNavigator.HasDisallowedSourceEntries"/>) may be repaired by
		/// re-unioning the emitted rings with each other (see
		/// <see cref="ReUnionProcessedRings"/>). Disabled on the re-union calls themselves
		/// to guarantee termination; if such a nested union is degenerate again, the
		/// nested-exterior-ring guard remains as the safety net.
		/// </summary>
		public bool AllowReUnionRepair { get; set; } = true;

		/// <summary>
		/// Returns the difference between source and target.
		/// </summary>
		/// <returns>The difference, i.e. source areas that are not part of the target.</returns>
		public MultiLinestring IntersectXY()
		{
			Assert.ArgumentCondition(_subcurveNavigator.Source.IsClosed, "Source must be closed.");
			Assert.ArgumentCondition(_subcurveNavigator.Target.IsClosed, "Target must be closed.");

			// Based on Weiler–Atherton clipping algorithm, added specific logic for linear intersections and multi-parts.
			ClusterPointsIfNecessary();

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

			ClusterPointsIfNecessary();

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

			ClusterPointsIfNecessary();

			IList<Linestring> processedRingsResult =
				_subcurveNavigator.FollowSubcurvesTurningLeft();

			if (AllowReUnionRepair && _subcurveNavigator.HasDisallowedSourceEntries)
			{
				// Degenerate situation (source rings touching/pinching within the tolerance):
				// the union walk was prevented from re-entering an already departed source
				// ring and hence emitted separate exterior rings that can overlap or even
				// contain each other (each walk traced its own loop around the shared
				// target). Repair this immediately, BEFORE the nested-exterior-ring guard and
				// the ring-relationship handling below - both require simple, non-overlapping
				// rings. Unlike the original operands, the emitted rings no longer pinch, so
				// unioning them with each other yields the proper outline(s).
				processedRingsResult = ReUnionProcessedRings(processedRingsResult);
			}

			// A correct union never produces an exterior (CW) ring fully contained within
			// another exterior ring - that would double-count the inner ring's area. If it
			// happens, the navigation was derailed (typically by a near-coincident-vertex
			// artifact, e.g. a spurious crossing at a sub-resolution spike apex - see Grancy,
			// now handled at the root by ClusterGeometries cracking). Assert for now so such
			// cases surface as bugs rather than silently inflating the area.
			AssertNoNestedExteriorRings(processedRingsResult);

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

			if (result.GetLinestrings().All(l => l.ClockwiseOriented != true))
			{
				if (! _subcurveNavigator.Source.IsEmpty &&
				    ! _subcurveNavigator.Target.IsEmpty)
				{
					throw new AssertionException(
						"Non-simple result: No exterior ring or result has become empty.");
				}
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

			// Interior rings that have not yet been assigned to a result ring group. An
			// unprocessed outer ring that sits inside one of these (e.g. an island in a
			// courtyard whose surrounding building is itself still unprocessed) is NOT a
			// duplicate of the building and must be kept - the containing ring group does
			// not have its hole assigned yet, so a plain AreaContainsXY against it would
			// wrongly report containment (TOP: garden_center_giubiasco).
			List<Linestring> unassignedInteriorRings =
				unprocessed.GetLinestrings().Where(l => l.ClockwiseOriented == false).ToList();

			foreach (var unprocessedRing in unprocessed.GetLinestrings()
			                                           .Where(l => l.ClockwiseOriented != false)
			                                           .OrderByDescending(l => l.GetArea2D()))
			{
				bool containedByResultRing =
					resultRingGroups.Any(r => GeomRelationUtils.AreaContainsXY(
						                          r, unprocessedRing,
						                          _subcurveNavigator.Tolerance) == true);

				bool insideUnassignedInteriorRing =
					unassignedInteriorRings.Any(island => RingContainsRobust(
						                            island, unprocessedRing,
						                            _subcurveNavigator.Tolerance));

				if (! containedByResultRing || insideUnassignedInteriorRing)
				{
					// Not contained by any result ring, or contained only because the
					// relevant interior ring (hole) has not been assigned yet: keep it.
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
		/// Re-unions the rings emitted by a degenerate union walk (see
		/// <see cref="SubcurveNavigator.HasDisallowedSourceEntries"/>) with each other: the
		/// suppressed source re-entries make each walk trace its own loop around the shared
		/// target, so the emitted exterior rings can overlap or even contain each other.
		/// Unlike the original operands they no longer pinch, so unioning them yields the
		/// proper outline(s). Interior rings contained in an emitted exterior ring take part
		/// in the union (and are correctly filled where another emitted ring covers them);
		/// interior rings whose containing outer ring was NOT processed are kept as-is for
		/// the regular downstream assignment.
		/// </summary>
		private IList<Linestring> ReUnionProcessedRings(
			[NotNull] IList<Linestring> processedRings)
		{
			double tolerance = _subcurveNavigator.Tolerance;

			List<RingGroup> components =
				GeomTopoOpUtils.GetConnectedComponents(
					               new MultiPolycurve(processedRings), tolerance)
				               .OrderByDescending(c => c.GetArea2D())
				               .ToList();

			if (components.Count < 2)
			{
				return processedRings;
			}

			var inComponents = new HashSet<Linestring>(
				components.SelectMany(c => c.GetLinestrings()));

			List<Linestring> unassignedInteriorRings =
				processedRings.Where(r => ! inComponents.Contains(r)).ToList();

			MultiLinestring reUnion = components[0];

			foreach (RingGroup component in components.Skip(1))
			{
				reUnion = GeomTopoOpUtils.GetUnionAreasXY(
					reUnion, component, tolerance, MergeTolerance,
					allowReUnionRepair: false);
			}

			List<Linestring> result = reUnion.GetLinestrings().ToList();
			result.AddRange(unassignedInteriorRings);

			return result;
		}

		private void AssertNoNestedExteriorRings(
			[NotNull] IList<Linestring> processedRings)
		{
			double tolerance = _subcurveNavigator.Tolerance;

			List<Linestring> exteriorRings =
				processedRings.Where(r => r.ClockwiseOriented == true).ToList();

			for (var i = 0; i < exteriorRings.Count; i++)
			{
				for (var j = 0; j < exteriorRings.Count; j++)
				{
					if (i == j)
					{
						continue;
					}

					if (RingContains(exteriorRings[i], exteriorRings[j], tolerance))
					{
						throw new AssertionException(
							"Union produced an exterior ring contained within another exterior " +
							"ring (double-counted area). The input is likely non-simple or the " +
							"navigation was derailed by a near-coincident-vertex artifact.");
					}
				}
			}
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
		/// Like <see cref="RingContains"/>, but when every vertex of
		/// <paramref name="unCutInteriorRing"/> lies on the boundary of
		/// <paramref name="exteriorRing"/> (all vertices boundary-ambiguous), it falls back to
		/// probing the inner ring's segment midpoints, which lie off the boundary wherever the
		/// inner ring dips into the interior. This resolves the case of an island whose corners
		/// ALL touch the surrounding courtyard hole boundary, where the plain vertex test is
		/// inconclusive and falls through to "not contained"
		/// (TOP: brutalismus_in_duedingen / garden_center_giubiasco).
		/// </summary>
		private static bool RingContainsRobust([NotNull] Linestring exteriorRing,
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

			// Every vertex is on the exterior ring's boundary: probe segment midpoints, which
			// lie clearly inside (or outside) wherever the inner ring deviates from the boundary.
			foreach (Line3D segment in unCutInteriorRing.Segments)
			{
				Pnt3D midPoint = segment.GetPointAlong(0.5, asRatio: true);

				bool? contained =
					GeomRelationUtils.AreaContainsXY(exteriorRing, midPoint, tolerance, true);

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
					// Use a full-ring containment test (not just the start point): the
					// processed inner ring's start point can land exactly on the boundary
					// of a small island that does NOT actually contain the hole (e.g. a
					// pinch vertex shared by an island and the reshaped courtyard). A
					// single-point test then wrongly nests a large courtyard hole inside a
					// tiny island; RingContains skips boundary-ambiguous vertices and
					// decides on the first conclusive (interior/exterior) one
					// (TOP: vallee_de_la_jeunesse). Holes left unassigned here are picked up
					// by the AssignInteriorRings call (smallest containing ring group).
					Linestring containing =
						unprocessedOuterRings.FirstOrDefault(o => o.ClockwiseOriented == true &&
							                                     RingContains(
								                                     o, processedResultRing,
								                                     _subcurveNavigator.Tolerance));

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

		private void ClusterPointsIfNecessary()
		{
			// Desired side-effect: determine target navigability
			bool hasUnClusteredIntersectionPoints =
				_subcurveNavigator.IntersectionPointNavigator.HasUnClusteredIntersectionPoints;

			// The parallel-run snap (MergeTolerance) targets near-coincident parallel edge runs
			// bounded by two point-touches that are too far apart to cluster, so it must run even
			// when the point-clustering gate is not tripped (footprint path). Gate it on a cheap
			// scan for an actual candidate so the - per incremental footprint step - work of
			// ClusterGeometries (point clustering + subcurve coincidence tests + clone/recompute)
			// is skipped on the many steps that have nothing to snap.
			bool wantParallelRunSnap =
				MergeTolerance > _subcurveNavigator.Tolerance &&
				HasParallelRunSnapCandidate(_subcurveNavigator.IntersectionPoints, MergeTolerance);

			if (AllowPointClustering && (hasUnClusteredIntersectionPoints || wantParallelRunSnap))
			{
				if (ClusterGeometries(pointClustering: true, parallelRunSnap: wantParallelRunSnap,
				                      out ISegmentList newSource, out ISegmentList newTarget))
				{
					// Snapping near-coincident vertices to a common cluster point can fold
					// a sub-resolution spike into a duplicate (out-and-back) segment. Once
					// points have been clustered such linear self-intersections are always
					// spurious, so remove them here; this leaves the navigator with simple,
					// navigable rings instead of spike artefacts (Thanhalten needle, and a
					// class of "intersection seen twice" navigator failures).
					double tolerance = _subcurveNavigator.Tolerance;

					if (newSource != null)
					{
						newSource = RemoveLinearSelfIntersections(newSource, tolerance);
					}

					if (newTarget != null)
					{
						newTarget = RemoveLinearSelfIntersections(newTarget, tolerance);
					}

					// Recalculate intersections with updated geometries:
					_subcurveNavigator.Invalidate(
						newSource ?? _subcurveNavigator.Source,
						newTarget ?? _subcurveNavigator.Target);
				}
			}
		}

		/// <summary>
		/// Removes linear self-intersections (duplicate out-and-back segments) from each
		/// ring of <paramref name="clustered"/>. Such artefacts appear after
		/// <see cref="ClusterGeometries"/> snaps near-coincident vertices together,
		/// collapsing a sub-resolution spike into a degenerate segment. Returns the input
		/// unchanged when nothing was removed.
		/// </summary>
		private static ISegmentList RemoveLinearSelfIntersections(
			[NotNull] ISegmentList clustered, double tolerance)
		{
			var cleanedParts = new List<Linestring>(clustered.PartCount);
			bool changed = false;

			for (int i = 0; i < clustered.PartCount; i++)
			{
				Linestring part = clustered.GetPart(i);

				var results = new List<Linestring>();
				if (part.IsClosed &&
				    GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
					    part, tolerance, results))
				{
					changed = true;
					cleanedParts.AddRange(results);
				}
				else
				{
					cleanedParts.Add(part);
				}
			}

			return changed ? new MultiPolycurve(cleanedParts) : clustered;
		}

		/// <param name="pointClustering">Whether to snap/crack near-coincident intersection
		/// points (the Sqrt(2)*tolerance vertex clustering).</param>
		/// <param name="parallelRunSnap">Whether to snap near-coincident parallel edge runs
		/// (the <see cref="MergeTolerance"/> sliver collapse).</param>
		private bool ClusterGeometries(bool pointClustering, bool parallelRunSnap,
		                               [CanBeNull] out ISegmentList newSource,
		                               [CanBeNull] out ISegmentList newTarget)
		{
			newSource = null;
			newTarget = null;

			List<IntersectionPoint3D> intersectionList =
				(List<IntersectionPoint3D>) _subcurveNavigator.IntersectionPoints;

			// Crack points to be applied per source / target part. We do not just snap the
			// existing intersection vertices to the cluster point: we also crack (split) a
			// segment that runs past the cluster without a vertex there, inserting a vertex
			// at the cluster point. This welds two near-coincident segments (e.g. the flanks
			// of a sub-resolution spike, Grancy needle) into a coincident pair, so the
			// subsequent RemoveLinearSelfIntersections can collapse the resulting duplicate
			// out-and-back segment and the recalculated intersections no longer contain the
			// spurious crossing that derailed the navigator.
			//
			// Collected against the ORIGINAL geometry (the crack points reference virtual vertex
			// positions that index into it); the - potentially large - source/target are only
			// cloned below if there is actually something to apply, so a union step that needs no
			// snapping pays no clone/recompute cost (this matters: the footprint runs this on every
			// incremental step).
			ISegmentList originalTarget = _subcurveNavigator.Target;
			var sourceCrackPointsByPart = new Dictionary<int, List<CrackPoint>>();
			var targetCrackPointsByPart = new Dictionary<int, List<CrackPoint>>();

			if (pointClustering)
			{
				// Sqrt(2)*tolerance (instead of plain tolerance) lets a vertex pair that
				// straddles the tolerance/resolution gap (e.g. the flanks of a sub-resolution
				// spike, 0.0078 m apart at tol 0.00625) cluster to a common point, so the
				// post-cluster RemoveLinearSelfIntersections can collapse the spike. The plain
				// tolerance leaves such spikes intact and the navigator mis-handles them.
				double clusterDistance = ToleranceFactor * _subcurveNavigator.Tolerance;
				IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> clusteredIntersections =
					GeomTopoOpUtils.Cluster(intersectionList, ip => ip.Point,
					                        clusterDistance);

				foreach (KeyValuePair<IPnt, List<IntersectionPoint3D>> cluster in
				         clusteredIntersections)
				{
					if (cluster.Value.Count == 1)
					{
						continue;
					}

					IPnt clusterPoint = cluster.Key;

					foreach (IntersectionPoint3D intersection in cluster.Value)
					{
						CollectSourceCrackPoint(intersection, clusterPoint,
						                        sourceCrackPointsByPart);
						CollectTargetCrackPoint(intersection, clusterPoint, originalTarget,
						                        targetCrackPointsByPart);
					}
				}
			}

			// Snap near-coincident parallel edge runs (bounded by two point-touches enclosing a
			// sub-resolution sliver) so the navigator sees a clean linear intersection. The two
			// bounding touches are too far apart to share a cluster, so this is handled here in
			// addition to the point clustering above.
			if (parallelRunSnap)
			{
				CollectParallelRunCrackPoints(intersectionList, sourceCrackPointsByPart,
				                              targetCrackPointsByPart, originalTarget);

				CollectDegenerateLinearRunCrackPoints(intersectionList, sourceCrackPointsByPart,
				                                      targetCrackPointsByPart, originalTarget);

				CollectNearCoincidentCrossingCrackPoints(
					intersectionList, sourceCrackPointsByPart, targetCrackPointsByPart,
					originalTarget);
			}

			if (sourceCrackPointsByPart.Count == 0 && targetCrackPointsByPart.Count == 0)
			{
				// Nothing to snap - avoid cloning the (possibly large) accumulated geometry.
				return false;
			}

			ISegmentList source = Clone(_subcurveNavigator.Source);
			ISegmentList target = Clone(originalTarget);

			bool sourceUpdated = ApplyCrackPoints(ref source, sourceCrackPointsByPart);
			bool targetUpdated = ApplyCrackPoints(ref target, targetCrackPointsByPart);

			if (sourceUpdated)
			{
				newSource = source;
			}

			if (targetUpdated)
			{
				newTarget = target;
			}

			return sourceUpdated || targetUpdated;
		}

		/// <summary>
		/// Cheap pre-check (no subcurve construction) for whether any of the parallel-run /
		/// degenerate-linear / near-coincident-crossing snaps could possibly apply: an adjacent
		/// same-part point-touch pair, a sub-merge-tolerance linear run, or a sub-merge-tolerance
		/// crossing pair. Used to skip the (per incremental footprint step) ClusterGeometries work
		/// on the many steps that have nothing to snap.
		/// </summary>
		private static bool HasParallelRunSnapCandidate(
			[NotNull] IList<IntersectionPoint3D> intersectionPoints, double mergeTolerance)
		{
			List<IntersectionPoint3D> ordered =
				intersectionPoints
					.OrderBy(ip => ip.SourcePartIndex)
					.ThenBy(ip => ip.VirtualSourceVertex)
					.ToList();

			for (var i = 0; i < ordered.Count - 1; i++)
			{
				IntersectionPoint3D a = ordered[i];
				IntersectionPoint3D b = ordered[i + 1];

				if (a.SourcePartIndex != b.SourcePartIndex ||
				    a.TargetPartIndex != b.TargetPartIndex)
				{
					continue;
				}

				// Adjacent point-touch pair: a potential near-coincident parallel run.
				if (a.Type == IntersectionPointType.TouchingInPoint &&
				    b.Type == IntersectionPointType.TouchingInPoint)
				{
					return true;
				}

				bool withinMerge =
					a.Point.GetDistance(b.Point, inXY: true) <= mergeTolerance;

				// Degenerate (sub-merge-tolerance) linear run, or near-coincident crossing pair.
				if (withinMerge &&
				    ((a.Type == IntersectionPointType.LinearIntersectionStart &&
				      b.Type == IntersectionPointType.LinearIntersectionEnd) ||
				     (a.Type == IntersectionPointType.Crossing &&
				      b.Type == IntersectionPointType.Crossing)))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Detects near-coincident, near-parallel edge runs between source and target that are
		/// bounded by two point-touches (<see cref="IntersectionPointType.TouchingInPoint"/>) and
		/// enclose only a sub-resolution sliver, and collects crack points that snap both sides to
		/// the touch points. After cracking, the run carries matching vertices on source and
		/// target, so the recomputed intersection is a clean linear intersection instead of two
		/// touches the turning-left walk cannot close ("Intersections seen twice"). This is the
		/// fine-tolerance analogue of what the classifier does for free at coarser tolerances,
		/// where the perpendicular offset of the two edges still falls within tolerance.
		/// </summary>
		private void CollectParallelRunCrackPoints(
			[NotNull] List<IntersectionPoint3D> intersectionList,
			[NotNull] Dictionary<int, List<CrackPoint>> sourceCrackPointsByPart,
			[NotNull] Dictionary<int, List<CrackPoint>> targetCrackPointsByPart,
			[NotNull] ISegmentList target)
		{
			List<IntersectionPoint3D> touches =
				intersectionList
					.Where(ip => ip.Type == IntersectionPointType.TouchingInPoint &&
					             ! double.IsNaN(ip.VirtualTargetVertex))
					.OrderBy(ip => ip.SourcePartIndex)
					.ThenBy(ip => ip.VirtualSourceVertex)
					.ToList();

			for (var i = 0; i < touches.Count - 1; i++)
			{
				IntersectionPoint3D a = touches[i];
				IntersectionPoint3D b = touches[i + 1];

				// Adjacent along the same source part and referencing the same target part.
				if (a.SourcePartIndex != b.SourcePartIndex ||
				    a.TargetPartIndex != b.TargetPartIndex)
				{
					continue;
				}

				if (! SubPathsAreNearCoincident(a, b, MergeTolerance))
				{
					continue;
				}

				foreach (IntersectionPoint3D touch in new[] { a, b })
				{
					CollectSourceCrackPoint(touch, touch.Point, sourceCrackPointsByPart);
					CollectTargetCrackPoint(touch, touch.Point, target, targetCrackPointsByPart);
				}
			}
		}

		/// <summary>
		/// Detects degenerate (sub-merge-tolerance) linear intersection runs - a
		/// <see cref="IntersectionPointType.LinearIntersectionStart"/> immediately followed by its
		/// <see cref="IntersectionPointType.LinearIntersectionEnd"/> whose two points are within
		/// <see cref="MergeTolerance"/> - and collects crack points that snap both ends (source and
		/// target) to the run midpoint, collapsing the spurious micro-run to a single point. Such a
		/// run is the near-coincident-vertex artefact that survives the Sqrt(2)*tolerance point
		/// clustering (the two ends sit just beyond that distance) and derails the turning-left walk
		/// at fine tolerance. The inverse situation of <see cref="CollectParallelRunCrackPoints"/>.
		/// </summary>
		private void CollectDegenerateLinearRunCrackPoints(
			[NotNull] List<IntersectionPoint3D> intersectionList,
			[NotNull] Dictionary<int, List<CrackPoint>> sourceCrackPointsByPart,
			[NotNull] Dictionary<int, List<CrackPoint>> targetCrackPointsByPart,
			[NotNull] ISegmentList target)
		{
			List<IntersectionPoint3D> ordered =
				intersectionList
					.OrderBy(ip => ip.SourcePartIndex)
					.ThenBy(ip => ip.VirtualSourceVertex)
					.ToList();

			for (var i = 0; i < ordered.Count - 1; i++)
			{
				IntersectionPoint3D start = ordered[i];
				IntersectionPoint3D end = ordered[i + 1];

				if (start.Type != IntersectionPointType.LinearIntersectionStart ||
				    end.Type != IntersectionPointType.LinearIntersectionEnd ||
				    start.SourcePartIndex != end.SourcePartIndex)
				{
					continue;
				}

				double runLength = start.Point.GetDistance(end.Point, inXY: true);
				if (runLength <= 0 || runLength > MergeTolerance)
				{
					continue;
				}

				var midpoint = new Pnt3D(
					(start.Point.X + end.Point.X) / 2,
					(start.Point.Y + end.Point.Y) / 2,
					(start.Point.Z + end.Point.Z) / 2);

				foreach (IntersectionPoint3D linearEnd in new[] { start, end })
				{
					CollectSourceCrackPoint(linearEnd, midpoint, sourceCrackPointsByPart);
					CollectTargetCrackPoint(linearEnd, midpoint, target, targetCrackPointsByPart);
				}
			}
		}

		/// <summary>
		/// Detects a pair of near-coincident <see cref="IntersectionPointType.Crossing"/> points on
		/// the same source part that reference the same target vertex and lie within
		/// <see cref="MergeTolerance"/> of each other (a single crossing duplicated by fine-tolerance
		/// vertex/segment classification), and snaps both to their midpoint so the recomputed
		/// intersection has a single crossing. Like the degenerate-linear collapse, these survive the
		/// Sqrt(2)*tolerance point clustering because they sit just beyond that distance.
		/// </summary>
		private void CollectNearCoincidentCrossingCrackPoints(
			[NotNull] List<IntersectionPoint3D> intersectionList,
			[NotNull] Dictionary<int, List<CrackPoint>> sourceCrackPointsByPart,
			[NotNull] Dictionary<int, List<CrackPoint>> targetCrackPointsByPart,
			[NotNull] ISegmentList target)
		{
			List<IntersectionPoint3D> crossings =
				intersectionList
					.Where(ip => ip.Type == IntersectionPointType.Crossing &&
					             ! double.IsNaN(ip.VirtualTargetVertex))
					.OrderBy(ip => ip.SourcePartIndex)
					.ThenBy(ip => ip.VirtualSourceVertex)
					.ToList();

			for (var i = 0; i < crossings.Count - 1; i++)
			{
				IntersectionPoint3D a = crossings[i];
				IntersectionPoint3D b = crossings[i + 1];

				if (a.SourcePartIndex != b.SourcePartIndex ||
				    a.TargetPartIndex != b.TargetPartIndex)
				{
					continue;
				}

				double distance = a.Point.GetDistance(b.Point, inXY: true);
				if (distance <= 0 || distance > MergeTolerance)
				{
					continue;
				}

				var midpoint = new Pnt3D(
					(a.Point.X + b.Point.X) / 2,
					(a.Point.Y + b.Point.Y) / 2,
					(a.Point.Z + b.Point.Z) / 2);

				foreach (IntersectionPoint3D crossing in new[] { a, b })
				{
					CollectSourceCrackPoint(crossing, midpoint, sourceCrackPointsByPart);
					CollectTargetCrackPoint(crossing, midpoint, target, targetCrackPointsByPart);
				}
			}
		}

		/// <summary>
		/// Whether the source sub-path and the target sub-path between two intersection points run
		/// (almost) coincident within <paramref name="mergeTolerance"/> over a meaningful length -
		/// i.e. they describe the same edge (a shared wall) separated only by a sub-resolution
		/// perpendicular offset.
		/// </summary>
		private bool SubPathsAreNearCoincident([NotNull] IntersectionPoint3D a,
		                                       [NotNull] IntersectionPoint3D b,
		                                       double mergeTolerance)
		{
			// Require a run meaningfully longer than the merge tolerance. A genuine shared wall is
			// many times the resolution; a degenerate corner-touch split into two sub-resolution
			// touches (e.g. OID_4357598: srcV1.0 / srcV1.0002) is not a parallel run and must not
			// be snapped (doing so derails the navigator).
			double minRunLength = mergeTolerance;

			// On a closed ring the run between the two intersections can go either way (direct or
			// wrapping past the ring start), so try every source/target candidate and accept any
			// combination that describes the same edge (comparable length + mutual coincidence).
			foreach (Linestring sourceSub in GetRunSubcurveCandidates(
				         _subcurveNavigator.Source, a.SourcePartIndex,
				         a.VirtualSourceVertex, b.VirtualSourceVertex))
			{
				double sourceLength = sourceSub.GetLength2D();
				if (sourceLength <= minRunLength)
				{
					continue;
				}

				foreach (Linestring targetSub in GetRunSubcurveCandidates(
					         _subcurveNavigator.Target, a.TargetPartIndex,
					         a.VirtualTargetVertex, b.VirtualTargetVertex))
				{
					if (targetSub.GetLength2D() <= minRunLength)
					{
						continue;
					}

					// A genuine shared wall has comparable lengths on both sides; a real (large)
					// overlap lens does not.
					if (! MathUtils.AreEqual(sourceLength, targetSub.GetLength2D(),
					                         mergeTolerance))
					{
						continue;
					}

					if (CurvesWithin(sourceSub, targetSub, mergeTolerance) &&
					    CurvesWithin(targetSub, sourceSub, mergeTolerance))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Whether every vertex and sampled interior point of <paramref name="curve"/> lies within
		/// <paramref name="tolerance"/> of <paramref name="other"/>.
		/// </summary>
		private static bool CurvesWithin([NotNull] Linestring curve,
		                                 [NotNull] Linestring other,
		                                 double tolerance)
		{
			for (var i = 0; i < curve.PointCount; i++)
			{
				if (! GeomRelationUtils.LinesContainXY(other, curve.GetPoint3D(i), tolerance))
				{
					return false;
				}
			}

			foreach (double ratio in new[] { 0.25, 0.5, 0.75 })
			{
				IPnt along = curve.GetPointAlong(ratio, asRatio: true);
				if (! GeomRelationUtils.LinesContainXY(other, along, tolerance))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the candidate sub-curves of the given part between the two virtual vertex
		/// positions: the direct (low-to-high) run, and - for a closed ring - the complementary
		/// run that wraps past the ring start. Degenerate candidates are omitted.
		/// </summary>
		[NotNull]
		private static IEnumerable<Linestring> GetRunSubcurveCandidates(
			[NotNull] ISegmentList segments, int partIndex,
			double virtualVertexFrom, double virtualVertexTo)
		{
			if (partIndex < 0 || partIndex >= segments.PartCount)
			{
				yield break;
			}

			double lo = Math.Min(virtualVertexFrom, virtualVertexTo);
			double hi = Math.Max(virtualVertexFrom, virtualVertexTo);

			if (hi - lo <= 0)
			{
				yield break;
			}

			Linestring part = segments.GetPart(partIndex);

			Linestring direct = GetSubcurveBetween(part, lo, hi);
			if (direct != null)
			{
				yield return direct;
			}

			if (part.IsClosed)
			{
				// The complementary run: from hi forward to the ring end, then from the ring
				// start to lo.
				Linestring toEnd = GetSubcurveBetween(part, hi, part.SegmentCount);
				Linestring fromStart = GetSubcurveBetween(part, 0, lo);

				if (toEnd != null && fromStart != null)
				{
					double epsilon =
						MathUtils.GetDoubleSignificanceEpsilon(part.XMax, part.YMax);
					yield return GeomTopoOpUtils.MergeConnectedLinestrings(
						new List<Linestring> { toEnd, fromStart }, null, epsilon);
				}
				else if (toEnd != null)
				{
					yield return toEnd;
				}
				else if (fromStart != null)
				{
					yield return fromStart;
				}
			}
		}

		[CanBeNull]
		private static Linestring GetSubcurveBetween([NotNull] Linestring part,
		                                             double lo, double hi)
		{
			if (hi - lo <= 0)
			{
				return null;
			}

			int fromSegment = (int) Math.Floor(lo);
			double fromRatio = lo - fromSegment;
			int toSegment = (int) Math.Floor(hi);
			double toRatio = hi - toSegment;

			if (toSegment >= part.SegmentCount)
			{
				toSegment = part.SegmentCount - 1;
				toRatio = 1;
			}

			if (fromSegment >= part.SegmentCount)
			{
				return null;
			}

			return part.GetSubcurve(fromSegment, fromRatio, toSegment, toRatio, true, false);
		}

		private static void CollectSourceCrackPoint(
			[NotNull] IntersectionPoint3D intersection,
			[NotNull] IPnt clusterPoint,
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart)
		{
			var targetPoint = new Pnt3D(clusterPoint.X, clusterPoint.Y,
			                            intersection.Point.Z);

			var crackPoint = new CrackPoint(intersection, targetPoint);

			if (intersection.IsSourceVertex())
			{
				crackPoint.SnapVertexIndex = (int) intersection.VirtualSourceVertex;
			}
			else
			{
				crackPoint.SegmentSplitFactor = intersection.VirtualSourceVertex;
			}

			AddCrackPoint(crackPointsByPart, intersection.SourcePartIndex, crackPoint);
		}

		private static void CollectTargetCrackPoint(
			[NotNull] IntersectionPoint3D intersection,
			[NotNull] IPnt clusterPoint,
			[NotNull] ISegmentList target,
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart)
		{
			if (double.IsNaN(intersection.VirtualTargetVertex))
			{
				// Source-only intersection (e.g. a touching point): nothing to crack on the
				// target side.
				return;
			}

			int partIndex = intersection.TargetPartIndex;
			Linestring linestring = target.GetPart(partIndex);

			CrackPoint crackPoint;
			if (intersection.IsTargetVertex(out int targetVertexIdx))
			{
				double origZ = linestring.GetPoint3D(targetVertexIdx).Z;
				crackPoint = new CrackPoint(
					             intersection,
					             new Pnt3D(clusterPoint.X, clusterPoint.Y, origZ))
				             {
					             SnapVertexIndex = targetVertexIdx
				             };
			}
			else
			{
				crackPoint = new CrackPoint(
					             intersection,
					             new Pnt3D(clusterPoint.X, clusterPoint.Y,
					                       intersection.Point.Z))
				             {
					             SegmentSplitFactor = intersection.VirtualTargetVertex
				             };
			}

			AddCrackPoint(crackPointsByPart, partIndex, crackPoint);
		}

		/// <summary>
		/// Adds <paramref name="crackPoint"/> to the per-part list, skipping a duplicate that
		/// would snap the same vertex twice (<see cref="GeomTopoOpUtils.CrackLinestring"/>
		/// keys snap points by vertex index and cannot take duplicates). Duplicate segment
		/// split factors are tolerated - CrackLinestring already de-duplicates those.
		/// </summary>
		private static void AddCrackPoint(
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart,
			int partIndex, [NotNull] CrackPoint crackPoint)
		{
			if (! crackPointsByPart.TryGetValue(partIndex, out List<CrackPoint> partList))
			{
				partList = new List<CrackPoint>();
				crackPointsByPart.Add(partIndex, partList);
			}

			if (crackPoint.SnapVertexIndex != null &&
			    partList.Exists(cp => cp.SnapVertexIndex == crackPoint.SnapVertexIndex))
			{
				return;
			}

			partList.Add(crackPoint);
		}

		/// <summary>
		/// Applies the collected crack points to the affected parts of
		/// <paramref name="segments"/> (snapping vertices and splitting segments at the
		/// cluster points). Returns true and replaces <paramref name="segments"/> with the
		/// cracked result if anything changed.
		/// </summary>
		private static bool ApplyCrackPoints(
			[NotNull] ref ISegmentList segments,
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart)
		{
			if (crackPointsByPart.Count == 0)
			{
				return false;
			}

			var newParts = new List<Linestring>(segments.PartCount);
			bool changed = false;

			for (int i = 0; i < segments.PartCount; i++)
			{
				Linestring part = segments.GetPart(i);

				if (crackPointsByPart.TryGetValue(i, out List<CrackPoint> crackPoints) &&
				    crackPoints.Count > 0)
				{
					newParts.Add(GeomTopoOpUtils.CrackLinestring(part, crackPoints, null));
					changed = true;
				}
				else
				{
					newParts.Add(part);
				}
			}

			if (changed)
			{
				segments = new MultiPolycurve(newParts);
			}

			return changed;
		}

		private static ISegmentList Clone(ISegmentList source)
		{
			if (source is MultiLinestring multiLinestring)
			{
				source = multiLinestring.Clone();
			}
			else if (source is Linestring linestring)
			{
				source = linestring.Clone();
			}
			else
			{
				throw new ArgumentException("Unexpected segment list type");
			}

			return source;
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

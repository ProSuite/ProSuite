using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Makes a geometry simple at a given tolerance: every place where two segments come
	/// closer to each other than the tolerance is turned into a shared vertex (cracking),
	/// the participating vertices are moved onto a common location (clustering), and what
	/// collapses in the process is removed.
	/// </summary>
	/// <remarks>
	/// <para>Two levels of API:</para>
	/// <list type="bullet">
	/// <item>The crack point primitives (<see cref="CollectSourceCrackPoint"/>,
	/// <see cref="CollectTargetCrackPoint"/>, <see cref="ApplyCrackPoints(IList{Linestring},
	/// Dictionary{int, List{CrackPoint}}, double?)"/>, <see cref="RemoveDegenerateSegments"/>),
	/// which turn clustered <see cref="IntersectionPoint3D"/>s into per-part
	/// <see cref="CrackPoint"/>s and apply them. Used by <see cref="RingOperator"/> during
	/// the union and by <see cref="CrackAndCluster(Polyhedron,double,CrackAndClusterOptions,out int)"/>
	/// below.</item>
	/// <item><see cref="CrackAndCluster(Polyhedron,double,CrackAndClusterOptions,out int)"/>,
	/// which iterates those primitives to a fixpoint over ALL rings of a polyhedron, i.e.
	/// makes the input simple BEFORE any boolean operation runs.</item>
	/// </list>
	/// <para>NOTE on the name: this is tolerance-based simplification (snapping/noding),
	/// not the exact OGC simple feature definition - which knows no tolerance and also
	/// covers ring nesting and orientation, neither of which is addressed here.</para>
	/// </remarks>
	public static class SimplificationUtils
	{
		#region Crack points

		/// <summary>
		/// Collects the crack point for the SOURCE side of the intersection: either a snap of
		/// the existing source vertex onto the cluster point, or a split of the source segment
		/// at the cluster point.
		/// </summary>
		public static void CollectSourceCrackPoint(
			[NotNull] IntersectionPoint3D intersection,
			[NotNull] IPnt clusterPoint,
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart)
		{
			// IntersectionPoint3D.Point is documented as the point taken from the SOURCE,
			// i.e. it already carries the source's Z: only XY moves to the cluster point.
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

		/// <summary>
		/// Collects the crack point for the TARGET side of the intersection: either a snap of
		/// the existing target vertex onto the cluster point, or a split of the target segment
		/// at the cluster point.
		/// </summary>
		public static void CollectTargetCrackPoint(
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

			// The Z must come from the TARGET: IntersectionPoint3D.Point is documented as
			// the point taken from the SOURCE. GetTargetPoint() returns the existing vertex
			// on a snap and the Z interpolated along the target segment on a split.
			Pnt3D targetPoint = intersection.GetTargetPoint(target);

			var crackPoint = new CrackPoint(
				intersection, new Pnt3D(clusterPoint.X, clusterPoint.Y, targetPoint.Z));

			if (intersection.IsTargetVertex(out int targetVertexIdx))
			{
				crackPoint.SnapVertexIndex = targetVertexIdx;
			}
			else
			{
				crackPoint.SegmentSplitFactor = intersection.VirtualTargetVertex;
			}

			AddCrackPoint(crackPointsByPart, intersection.TargetPartIndex, crackPoint);
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
		/// cluster points) and replaces <paramref name="segments"/> with the cracked result.
		/// </summary>
		/// <returns>Whether any crack point was applied. Note the difference to the
		/// <see cref="ApplyCrackPoints(IList{Linestring},Dictionary{int,List{CrackPoint}},double?)"/>
		/// overload, which reports whether any coordinate actually changed.</returns>
		public static bool ApplyCrackPoints(
			[NotNull] ref ISegmentList segments,
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart,
			double? minSegmentLengthSquared = null)
		{
			if (! crackPointsByPart.Values.Any(cps => cps.Count > 0))
			{
				return false;
			}

			var parts = new List<Linestring>(segments.PartCount);

			for (int i = 0; i < segments.PartCount; i++)
			{
				parts.Add(segments.GetPart(i));
			}

			ApplyCrackPoints(parts, crackPointsByPart, minSegmentLengthSquared);

			segments = new MultiPolycurve(parts);

			// NOTE: true whenever crack points were APPLIED, even if the cracked result is
			// coordinate-identical to the input. RingOperator uses the return value to decide
			// whether to run the spike cleanup (RemoveLinearSelfIntersections) and recalculate
			// the intersections, and both are needed regardless of whether this particular
			// part moved (PolyhedronTest.CanGetFootprintForHotelWaldhorn).
			return true;
		}

		/// <summary>
		/// Applies the collected crack points to the affected parts (snapping vertices and
		/// splitting segments at the cluster points). The part count and part order are
		/// preserved, so any caller-side per-part bookkeeping stays valid.
		/// </summary>
		/// <returns>Whether anything changed.</returns>
		public static bool ApplyCrackPoints(
			[NotNull] IList<Linestring> parts,
			[NotNull] Dictionary<int, List<CrackPoint>> crackPointsByPart,
			double? minSegmentLengthSquared = null)
		{
			var changed = false;

			foreach (KeyValuePair<int, List<CrackPoint>> pair in crackPointsByPart)
			{
				if (pair.Value.Count == 0)
				{
					continue;
				}

				Linestring original = parts[pair.Key];

				Linestring cracked = GeomTopoOpUtils.CrackLinestring(
					original, pair.Value, minSegmentLengthSquared);

				// Only an ACTUAL change counts: a crack point that snaps a vertex onto the
				// place it already occupies is reported on every pass, and a caller that
				// iterates to a fixpoint would never terminate.
				if (original.Equals(cracked))
				{
					continue;
				}

				parts[pair.Key] = cracked;

				changed = true;
			}

			return changed;
		}

		/// <summary>
		/// Removes vertices that coincide with their predecessor (and, in a closed part, the
		/// trailing vertices that coincide with the first). This is where the zero-length
		/// segments introduced by snapping two adjacent vertices onto the same cluster point
		/// disappear. A closed part that keeps fewer than three distinct vertices is replaced
		/// by an EMPTY linestring rather than removed, so the part indexes stay stable.
		/// </summary>
		/// <returns>Whether any vertex was removed.</returns>
		public static bool RemoveDegenerateSegments([NotNull] IList<Linestring> parts,
		                                            double minimumSegmentLength)
		{
			var removed = false;

			for (var partIndex = 0; partIndex < parts.Count; partIndex++)
			{
				Linestring part = parts[partIndex];

				if (part.IsEmpty)
				{
					continue;
				}

				bool isClosed = part.IsClosed;

				// One open vertex list (no duplicated closing point):
				int pointCount = isClosed ? part.PointCount - 1 : part.PointCount;

				var kept = new List<Pnt3D>(pointCount);

				foreach (Pnt3D vertex in part.GetPoints(0, pointCount, true))
				{
					if (kept.Count > 0 &&
					    GeomUtils.GetDistanceXY(kept[kept.Count - 1], vertex) <=
					    minimumSegmentLength)
					{
						continue;
					}

					kept.Add(vertex);
				}

				if (isClosed)
				{
					// The ring closes back onto its first vertex.
					while (kept.Count > 1 &&
					       GeomUtils.GetDistanceXY(kept[kept.Count - 1], kept[0]) <=
					       minimumSegmentLength)
					{
						kept.RemoveAt(kept.Count - 1);
					}
				}

				if (kept.Count == pointCount)
				{
					continue;
				}

				removed = true;

				if (isClosed && kept.Count < 3)
				{
					// The ring was thinner than the tolerance in all directions.
					parts[partIndex] = Linestring.CreateEmpty();
					continue;
				}

				if (isClosed)
				{
					kept.Add(kept[0].ClonePnt3D());
				}

				parts[partIndex] = new Linestring(kept);
			}

			return removed;
		}

		#endregion

		#region Crack and cluster

		/// <summary>
		/// Cracks and clusters all rings of the polyhedron against each other.
		/// </summary>
		/// <param name="polyhedron">The input, which is not modified.</param>
		/// <param name="tolerance">The XY tolerance.</param>
		/// <param name="options">The options, or null for the defaults.</param>
		/// <param name="iterations">The number of iterations the fixpoint needed.</param>
		/// <returns>The cracked and clustered polyhedron, or the unchanged input if the pass
		/// is disabled, did not converge or removed everything.</returns>
		[NotNull]
		public static Polyhedron CrackAndCluster([NotNull] Polyhedron polyhedron,
		                                         double tolerance,
		                                         [CanBeNull] CrackAndClusterOptions options,
		                                         out int iterations)
		{
			List<RingGroup> ringGroups = polyhedron.RingGroups.ToList();

			IList<RingGroup> result =
				CrackAndCluster(ringGroups, tolerance, options, out iterations);

			// Unchanged (disabled, not converged or everything collapsed): keep the input.
			return ReferenceEquals(result, ringGroups)
				       ? polyhedron
				       : new Polyhedron(result);
		}

		/// <summary>
		/// Cracks and clusters all rings of all ring groups against each other. The pass is
		/// the fixpoint of three steps:
		/// <list type="number">
		/// <item>SNAP AND CRACK: <see cref="GeomTopoOpUtils.GetSelfIntersections"/> finds
		/// every place where two segments come closer than the cluster tolerance - including
		/// true crossings and linear overlaps, not just vertex-to-segment proximity.
		/// <see cref="GeomTopoOpUtils.Cluster{T}"/> groups those intersections and the crack
		/// point primitives above snap the existing vertices onto the cluster point and split
		/// the segments that run past it without a vertex.</item>
		/// <item>DROP DEGENERATE SEGMENTS: <see cref="RemoveDegenerateSegments"/> removes the
		/// zero-length segments the snapping introduced.</item>
		/// <item>COLLAPSE: <see cref="GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY"/>
		/// cancels out the duplicate out-and-back runs (spikes, sub-tolerance rings) that the
		/// snapping produced. A ring whose segments all cancel out disappears here.</item>
		/// </list>
		/// Only XY is moved: the Z of each side is preserved by the crack point primitives.
		/// </summary>
		/// <returns>The cracked and clustered ring groups, or the unchanged input if the pass
		/// is disabled, did not converge or removed everything.</returns>
		[NotNull]
		public static IList<RingGroup> CrackAndCluster(
			[NotNull] IList<RingGroup> ringGroups, double tolerance,
			[CanBeNull] CrackAndClusterOptions options, out int iterations)
		{
			iterations = 0;

			if (options == null)
			{
				options = new CrackAndClusterOptions();
			}

			if (! options.Enabled || ringGroups.Count == 0)
			{
				return ringGroups;
			}

			double clusterTolerance = options.GetClusterTolerance(tolerance);
			double crackTolerance = options.GetCrackTolerance(tolerance);

			if (clusterTolerance <= crackTolerance)
			{
				throw new ArgumentException(
					"The cluster tolerance must be larger than the crack tolerance, " +
					"otherwise the vertices inserted by cracking are never absorbed.",
					nameof(options));
			}

			var parts = new List<Linestring>();
			var tags = new List<PartTag>();

			for (var groupIndex = 0; groupIndex < ringGroups.Count; groupIndex++)
			{
				RingGroup ringGroup = ringGroups[groupIndex];

				var isExterior = true;

				foreach (Linestring ring in ringGroup.GetLinestrings())
				{
					if (! ring.IsClosed || ring.PointCount < 4)
					{
						// Not a proper ring: leave the whole input alone rather than
						// guessing what was meant.
						return ringGroups;
					}

					parts.Add(ring.Clone());
					tags.Add(new PartTag(groupIndex, isExterior, ringGroup.Id));

					isExterior = false;
				}
			}

			if (! TryReachFixpoint(parts, tags, crackTolerance, clusterTolerance,
			                       options.MaxIterations, out iterations))
			{
				// A half-snapped geometry is worse than the original.
				return ringGroups;
			}

			IList<RingGroup> result = Reassemble(parts, tags);

			// Everything collapsed - that cannot be what the input meant.
			return result.Count == 0 ? ringGroups : result;
		}

		private static bool TryReachFixpoint([NotNull] List<Linestring> parts,
		                                     [NotNull] List<PartTag> tags,
		                                     double crackTolerance,
		                                     double clusterTolerance,
		                                     int maxIterations,
		                                     out int iterations)
		{
			// The crack tolerance: a segment shorter than it cannot carry a crack point
			// anyway, so anything shorter is a leftover of the snap, not a segment the input
			// meant to have. For the Aggressive strategy this is exactly half the
			// cluster tolerance (2*sqrt(2)*tol / 2 == sqrt(2)*tol).
			double minimumSegmentLength = crackTolerance;

			for (iterations = 0; iterations < maxIterations; iterations++)
			{
				// Both must be no-ops: snapping moves vertices onto cluster points, which can
				// bring previously separate vertices within the tolerance of each other, so
				// "nothing left to crack" alone is NOT a fixpoint.
				bool snapped = SnapAndCrack(parts, clusterTolerance);
				bool dropped = RemoveDegenerateSegments(parts, minimumSegmentLength);

				// RemoveDegenerateSegments empties a part instead of removing it (the part
				// indexes must stay stable while the crack points are applied). The empties
				// must not reach the next SnapAndCrack: an empty part breaks the global
				// segment indexing of the MultiPolycurve
				// (PolyhedronTest.CanGetFootprintForLugano8711144).
				if (dropped)
				{
					RemoveEmptyParts(parts, tags);
				}

				if (snapped || dropped)
				{
					continue;
				}

				// The spike / sub-tolerance ring cleanup runs ONCE, after the fixpoint - not
				// as a member of the loop. Inside the loop it fights the cracking: the crack
				// step inserts the vertex that makes two near-coincident runs coincident, the
				// cleanup deletes the run again, and the next pass re-inserts it
				// (friedhofsmauer_roggwil at the Uniform tolerances grows one vertex per pass
				// and never converges).
				// NOT optional: without it the Lugano sweep over 41'231 TLM_GEBAEUDEKOERPER
				// degrades from 110 to 462 footprint failures - worse than doing no cracking
				// at all (330). The snapping makes near-coincident runs exactly coincident,
				// and the union cannot navigate those until they are cancelled out here.
				CollapseRings(parts, tags, clusterTolerance);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Snaps every near-coincident intersection of the input onto its cluster point and
		/// cracks the segments that pass the cluster point without a vertex.
		/// </summary>
		/// <param name="parts">The parts to snap and crack. Modified in place.</param>
		/// <param name="clusterTolerance">The distance within which intersections are
		/// detected and snapped onto a common point.</param>
		/// <param name="areaOfInterest">If specified, only the intersections within this area
		/// are snapped and cracked.</param>
		/// <param name="knownSimpleSegmentCount">If greater than zero, the first this many
		/// segments of <paramref name="parts"/> are known not to intersect each other, so
		/// those pairs are not looked for. Both parameters are approximations, see
		/// <see cref="GeomTopoOpUtils.GetSelfIntersections"/>.</param>
		public static bool SnapAndCrack([NotNull] IList<Linestring> parts,
		                                double clusterTolerance,
		                                [CanBeNull] IBoundedXY areaOfInterest = null,
		                                int knownSimpleSegmentCount = 0)
		{
			ISegmentList segments = new MultiPolycurve(parts);

			// Detection runs at the CLUSTER tolerance, not at the crack tolerance: a vertex
			// pair is only seen here if the two segments carrying it come within the
			// detection radius, so detecting at the smaller crack tolerance under-clusters
			// relative to what the cluster tolerance promises. Measured over the 41'231
			// TLM_GEBAEUDEKOERPER of the Lugano extent: 110 footprint failures / 30 areas off
			// by more than 1 m2 when detecting at the cluster tolerance, against 141 / 37
			// when detecting at the crack tolerance (and 154 / 21 for the previous
			// vertex-clustering implementation, 330 / 75 without any cracking).
			// includeLinearIntersectionIntermediatePoints: the vertices INSIDE a linear
			// intersection are exactly the T-junctions where the other side has no vertex
			// yet (same flag as CrackUtils.AddSelfIntersectionCrackPoints3d passes).
			var intersections = (List<IntersectionPoint3D>)
				GeomTopoOpUtils.GetSelfIntersections(segments, clusterTolerance, true,
				                                     areaOfInterest,
				                                     knownSimpleSegmentCount);

			if (intersections.Count == 0)
			{
				return false;
			}

			// XY-clustering only (zTolerance NaN): the Z of each side is preserved by the
			// crack point primitives, so vertically stacked vertices are not flattened.
			IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> clusters =
				GeomTopoOpUtils.Cluster(intersections, ip => ip.Point, clusterTolerance);

			var crackPointsByPart = new Dictionary<int, List<CrackPoint>>();

			foreach (KeyValuePair<IPnt, List<IntersectionPoint3D>> cluster in clusters)
			{
				IPnt snapTo = GetClusterRepresentative(cluster.Value, cluster.Key);

				// NOTE: Singleton clusters are NOT skipped: a single intersection is
				//       exactly the T-junction case where the passing segment must be
				//       cracked. Same as RingOperator.ClusterGeometries.
				foreach (IntersectionPoint3D intersection in cluster.Value)
				{
					CollectSourceCrackPoint(intersection, snapTo, crackPointsByPart);
					CollectTargetCrackPoint(intersection, snapTo, segments,
					                        crackPointsByPart);
				}
			}

			return ApplyCrackPoints(parts, crackPointsByPart);
		}

		/// <summary>
		/// The location a cluster of intersections is snapped to: the EXISTING coordinate of
		/// the cluster that is closest to the cluster's centroid, an existing source vertex
		/// winning over an intersection in a segment interior.
		/// </summary>
		/// <remarks>
		/// Deliberately NOT the centroid the clustering computed
		/// (<see cref="GeomTopoOpUtils.Cluster{T}"/> returns it as the key): snapping onto a
		/// newly computed coordinate moves the geometry, so the next pass recomputes the
		/// intersections at slightly different places and the centroid shifts again. The
		/// result is a geometric drift (each pass moves the vertices ~20% of the remaining
		/// gap on kirchweg_turgi) that approaches, but never reaches, a fixpoint. Snapping to
		/// an existing coordinate keeps the set of distinct coordinates from growing, so the
		/// iteration terminates exactly.
		/// </remarks>
		[NotNull]
		private static IPnt GetClusterRepresentative(
			[NotNull] List<IntersectionPoint3D> cluster, [NotNull] IPnt center)
		{
			Pnt3D best = null;
			var bestIsVertex = false;
			double bestDistance = double.MaxValue;

			foreach (IntersectionPoint3D intersection in cluster)
			{
				Pnt3D candidate = intersection.Point;

				// An existing vertex beats an intersection in a segment interior: snapping
				// onto it leaves the shape alone and inserts no new coordinate anywhere.
				bool isVertex = intersection.IsSourceVertex();

				if (bestIsVertex && ! isVertex)
				{
					continue;
				}

				double distance = GeomUtils.GetDistanceXY(candidate, center);

				if (best != null && isVertex == bestIsVertex)
				{
					// Closest to the centroid, so the choice is not biased in any direction.
					if (distance > bestDistance)
					{
						continue;
					}

					// Deterministic tie-break: equidistant candidates must not depend on the
					// order the intersections happen to be reported in.
					if (distance == bestDistance &&
					    ! (candidate.X < best.X ||
					       (candidate.X == best.X && candidate.Y < best.Y)))
					{
						continue;
					}
				}

				best = candidate;
				bestIsVertex = isVertex;
				bestDistance = distance;
			}

			return Assert.NotNull(best);
		}

		/// <summary>
		/// Drops the parts that <see cref="RemoveDegenerateSegments"/> emptied, together with
		/// their tags.
		/// </summary>
		private static void RemoveEmptyParts([NotNull] List<Linestring> parts,
		                                     [NotNull] List<PartTag> tags)
		{
			for (int partIndex = parts.Count - 1; partIndex >= 0; partIndex--)
			{
				if (! parts[partIndex].IsEmpty)
				{
					continue;
				}

				parts.RemoveAt(partIndex);
				tags.RemoveAt(partIndex);
			}
		}

		/// <summary>
		/// Cancels out the duplicate out-and-back runs produced by the snapping (spikes and
		/// sub-tolerance rings) and removes the parts that disappear entirely. Parts that are
		/// split into several rings by the operation inherit the tag of their origin.
		/// </summary>
		private static bool CollapseRings([NotNull] List<Linestring> parts,
		                                  [NotNull] List<PartTag> tags,
		                                  double tolerance)
		{
			var newParts = new List<Linestring>(parts.Count);
			var newTags = new List<PartTag>(parts.Count);

			var changed = false;

			for (var partIndex = 0; partIndex < parts.Count; partIndex++)
			{
				Linestring part = parts[partIndex];
				PartTag tag = tags[partIndex];

				if (part.IsEmpty)
				{
					// Emptied by RemoveDegenerateSegments.
					changed = true;
					continue;
				}

				var fragments = new List<Linestring>();

				if (! GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
					    part, tolerance, fragments))
				{
					newParts.Add(part);
					newTags.Add(tag);
					continue;
				}

				changed = true;

				foreach (Linestring fragment in fragments)
				{
					// Non-closed fragments are the vertical remains of the ring.
					if (! fragment.IsClosed || fragment.PointCount < 4)
					{
						continue;
					}

					newParts.Add(fragment);
					newTags.Add(tag);
				}
			}

			if (! changed)
			{
				return false;
			}

			parts.Clear();
			parts.AddRange(newParts);

			tags.Clear();
			tags.AddRange(newTags);

			return true;
		}

		[NotNull]
		private static IList<RingGroup> Reassemble([NotNull] List<Linestring> parts,
		                                           [NotNull] List<PartTag> tags)
		{
			var result = new List<RingGroup>();

			foreach (IGrouping<int, int> group in Enumerable
			                                      .Range(0, parts.Count)
			                                      .GroupBy(i => tags[i].GroupIndex)
			                                      .OrderBy(g => g.Key))
			{
				List<Linestring> exteriors =
					group.Where(i => tags[i].IsExterior).Select(i => parts[i]).ToList();

				if (exteriors.Count == 0)
				{
					// The exterior ring collapsed: the interior rings go with it.
					continue;
				}

				List<Linestring> interiors =
					group.Where(i => ! tags[i].IsExterior).Select(i => parts[i]).ToList();

				int? groupId = tags[group.First()].GroupId;

				// When the exterior ring was split, the interior rings are attached to the
				// first fragment only (same simplification as Polyhedron.SimplifyRings).
				for (var i = 0; i < exteriors.Count; i++)
				{
					RingGroup ringGroup = i == 0 && interiors.Count > 0
						                      ? new RingGroup(exteriors[i], interiors)
						                      : new RingGroup(exteriors[i]);

					ringGroup.Id = groupId;

					result.Add(ringGroup);
				}
			}

			return result;
		}

		/// <summary>
		/// Where a part came from, so that the ring groups can be reassembled after the
		/// rings have been cracked, split and dropped.
		/// </summary>
		private class PartTag
		{
			public PartTag(int groupIndex, bool isExterior, int? groupId)
			{
				GroupIndex = groupIndex;
				IsExterior = isExterior;
				GroupId = groupId;
			}

			public int GroupIndex { get; }

			public bool IsExterior { get; }

			public int? GroupId { get; }
		}

		#endregion
	}
}

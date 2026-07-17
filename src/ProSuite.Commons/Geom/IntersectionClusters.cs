using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Encapsulates the clusters of intersection points. In other implementations
	/// the geometries must be fully clustered if there are short segments or
	/// intersections within the tolerance. We try to explicitly model these situations.
	/// </summary>
	public class IntersectionClusters
	{
		private static double ToleranceFactor => Math.Sqrt(2);

		private HashSet<IntersectionPoint3D> _multipleSourceIntersections;
		private HashSet<IntersectionPoint3D> _multipleTargetIntersections;

		private readonly ISegmentList _source;
		private readonly ISegmentList _target;
		private readonly double _tolerance;

		// Boundary loop cache (there can be very many intersections)
		private List<BoundaryLoop> _sourceBoundaryLoops;
		private List<BoundaryLoop> _targetBoundaryLoops;

		public IntersectionClusters(
			ISegmentList source, ISegmentList target, double tolerance)
		{
			_source = source;
			_target = target;

			_tolerance = tolerance;
		}

		public IntersectionClusters(
			ISegmentList source, ISegmentList target, double tolerance,
			[CanBeNull] HashSet<IntersectionPoint3D> multipleSourceIntersections,
			[CanBeNull] HashSet<IntersectionPoint3D> multipleTargetIntersections)
			: this(source, target, tolerance)
		{
			_multipleSourceIntersections = multipleSourceIntersections;
			_multipleTargetIntersections = multipleTargetIntersections;
		}

		public void InitializeClusters(
			[NotNull] IList<IntersectionPoint3D> intersectionsAlongSource,
			[NotNull] IList<IntersectionPoint3D> intersectionsAlongTarget)
		{
			_multipleSourceIntersections =
				DetermineDuplicateSourceIntersections(intersectionsAlongTarget);
			_multipleTargetIntersections =
				DetermineDuplicateTargetIntersections(intersectionsAlongSource);
		}

		/// <summary>
		/// Finds intersections at the same (or very similar) source location which reference the
		/// same location along the target. These could be just a break in a linear intersection,
		/// a boundary loop in the source or two intersections in two adjacent source segments.
		/// </summary>
		/// <param name="intersectionsAlongTarget"></param>
		/// <returns></returns>
		private HashSet<IntersectionPoint3D> DetermineDuplicateSourceIntersections(
			[NotNull] IList<IntersectionPoint3D> intersectionsAlongTarget)
		{
			HashSet<IntersectionPoint3D> result = new HashSet<IntersectionPoint3D>();

			foreach (var intersectionsPerRing in
			         intersectionsAlongTarget.GroupBy(ip => ip.TargetPartIndex))
			{
				WithRingIntersectionPairs(
					intersectionsPerRing,
					(p1, p2) =>
					{
						if (p1.ReferencesSameTargetVertex(p2, _target, _tolerance))
						{
							// Possibly also check if we really can jump from one source part to the other
							// It can be jumped where the source vertices are exactly on top of each other
							// or there is a boundary loop? But
							// NOT: with a very thin spike where both sides intersect

							result.Add(p1);
							result.Add(p2);

							if (! p1.Point.EqualsXY(p2.Point, double.Epsilon))
							{
								// The geometries are not cracked / clustered
								HasUnClusteredIntersections = true;
							}

							// Prevent navigation within the cluster (zig-zag back to the same cluster), as in
							// GeomTopoUtilsTest.CanUnionUnCrackedRingAtSmallOvershootVertex():
							//
							//       target
							//    \  |
							//     \ |
							//      \|
							//       |\
							//       |/
							//      /|
							//     / |
							//    /
							// source

							// TODO: The side-effect of setting the DisallowSource properties on the intersection points should be removed!
							// We could extract a GeometryIntersections class/interface from SubcurveIntersectionPointNavigator (or rename it)
							// Alternatively we could maintain the relevant information in this class for each intersection.
							// This class should be interrogated for disallowed navigation at specific intersections 
							// also by RelationalOperators. -> Remove the DisallowSourceForward flags on the intersection point
							if (p1.SourcePartIndex == p2.SourcePartIndex)
							{
								// The pairs are ordered along the source and the target order might be swapped
								double p1Along = p1.VirtualSourceVertex;
								double p2Along = p2.VirtualSourceVertex;

								Linestring ring = _source.GetPart(p1.SourcePartIndex);

								double distanceAlong =
									SegmentIntersectionUtils.GetVirtualVertexRatioDistance(
										p1Along, p2Along, ring.SegmentCount);

								// Typically it is very very small, but theoretically it could be almost the entire segments
								// if the angle is extremely acute.
								if (Math.Abs(distanceAlong) < 2)
								{
									if (distanceAlong > 0)
									{
										// p1 is just before p2 along target
										p1.DisallowSourceForward = true;
										p2.DisallowSourceBackward = true;
										// TODO:
										//DisableSourceNavigationBetween(p1, p2, pointsPerRing);
									}
									else if (distanceAlong < 0)
									{
										// p1 is just after p2 along target
										p1.DisallowSourceBackward = true;
										p2.DisallowSourceForward = true;
										// TODO:
										//DisableSourceNavigationBetween(p2, p1, pointsPerRing);
									}
								}
							}
						}
						else if (PointsClusterButVertexCheckMissed(p1, p2))
						{
							// The two consecutive intersections are within the cluster distance
							// in XY but their along-target distance is (just) above tolerance, so
							// ReferencesSameTargetVertex missed them (e.g. a sub-resolution spike
							// whose flanks reference the same target vertex yet sit ~a resolution
							// apart in XY, cluster_crash_repro). Flag only so RingOperator
							// clustering snaps them and the collapsed spike is cleaned up; do NOT
							// add them to the duplicate set, leaving boundary-loop detection
							// unaffected.
							HasUnClusteredIntersections = true;
						}
					});
			}

			return result.Count == 0 ? null : result;
		}

		/// <summary>
		/// Two consecutive intersections that the vertex-reference check did not group, but
		/// which lie within the RingOperator cluster distance (Sqrt(2)*tolerance) of each other
		/// in XY and are not already coincident - i.e. clustering would snap them together.
		/// </summary>
		private bool PointsClusterButVertexCheckMissed(
			[NotNull] IntersectionPoint3D p1, [NotNull] IntersectionPoint3D p2)
		{
			return ! p1.Point.EqualsXY(p2.Point, double.Epsilon) &&
			       p1.Point.GetDistance(p2.Point, inXY: true) <= ToleranceFactor * _tolerance;
		}

		/// <summary>
		/// Finds intersections at the same (or very similar) target location which reference the
		/// same location along the source. These could be just a break in a linear intersection,
		/// a boundary loop in the target or two intersections in two adjacent target segments.
		/// </summary>
		/// <param name="intersectionsAlongSource"></param>
		/// <returns></returns>
		private HashSet<IntersectionPoint3D> DetermineDuplicateTargetIntersections(
			IList<IntersectionPoint3D> intersectionsAlongSource)
		{
			HashSet<IntersectionPoint3D> result = new HashSet<IntersectionPoint3D>();

			foreach (var pointsPerRing in
			         intersectionsAlongSource.GroupBy(ip => ip.SourcePartIndex))
			{
				WithRingIntersectionPairs(
					pointsPerRing,
					(p1, p2) =>
					{
						if (p1.ReferencesSameSourceVertex(p2, _source, _tolerance))
						{
							result.Add(p1);
							result.Add(p2);

							if (! p1.Point.EqualsXY(p2.Point, double.Epsilon))
							{
								// The geometries are not cracked / clustered
								HasUnClusteredIntersections = true;
							}

							// Prevent navigation within the cluster (zig-zag back to the same cluster), as in
							// GeomTopoUtilsTest.CanUnionUnCrackedRingAtSmallOvershootVertex():
							//
							//       source
							//    \  |
							//     \ |
							//      \|
							//       |\
							//       |/
							//      /|
							//     / |
							//    /
							// target

							// TODO: The side-effect of setting the DisallowTarget properties on the intersection points should be removed!
							// TODO: Extract GeometryIntersections class/interface from SubcurveIntersectionPointNavigator (or rename it)
							// This class should be interrogated for disallowed navigation at specific intersections 
							// also by RelationalOperators. -> Remove the DisallowTargetForward flags on the intersection point
							if (p1.TargetPartIndex == p2.TargetPartIndex)
							{
								// The pairs are ordered along the source and the target order might be swapped
								double p1AlongTarget = p1.VirtualTargetVertex;
								double p2AlongTarget = p2.VirtualTargetVertex;

								double distanceAlongTarget =
									SegmentIntersectionUtils.GetVirtualVertexRatioDistance(
										p1AlongTarget, p2AlongTarget,
										_target.GetPart(p1.TargetPartIndex).SegmentCount);

								// Typically it is very very small, but theoretically it could be almost the entire segments
								// if the angle is extremely acute.
								if (Math.Abs(distanceAlongTarget) < 2)
								{
									if (distanceAlongTarget > 0)
									{
										// p1 is just before p2 along target
										p1.DisallowTargetForward = true;
										p2.DisallowTargetBackward = true;
										DisableTargetNavigationBetween(p1, p2, pointsPerRing);
									}
									else if (distanceAlongTarget < 0)
									{
										// p1 is just after p2 along target
										p1.DisallowTargetBackward = true;
										p2.DisallowTargetForward = true;
										DisableTargetNavigationBetween(p2, p1, pointsPerRing);
									}
								}
							}
						}
						else if (PointsClusterButVertexCheckMissed(p1, p2))
						{
							// Symmetric to the source side: flag near-coincident consecutive
							// intersections (within the cluster distance in XY) that the
							// vertex-reference check missed, so RingOperator clustering runs.
							HasUnClusteredIntersections = true;
						}
					});
			}

			return result.Count == 0 ? null : result;
		}

		private static void DisableTargetNavigationBetween(IntersectionPoint3D p1,
		                                                   IntersectionPoint3D p2,
		                                                   IGrouping<int, IntersectionPoint3D>
			                                                   pointsPerRing)
		{
			List<IntersectionPoint3D> orderedAlongTarget =
				pointsPerRing.OrderBy(i => i.VirtualTargetVertex).ToList();

			bool disableTargetNavigation = false;
			foreach (IntersectionPoint3D intersectionPoint in CollectionUtils.Cycle(
				         orderedAlongTarget, 2))
			{
				if (intersectionPoint == p2 && disableTargetNavigation)
				{
					return;
				}

				if (disableTargetNavigation)
				{
					intersectionPoint.DisallowTargetBackward = true;
					intersectionPoint.DisallowTargetForward = true;
				}

				if (intersectionPoint == p1)
				{
					disableTargetNavigation = true;
				}
			}
		}

		public bool HasUnClusteredIntersections { get; set; }

		private static void WithRingIntersectionPairs(
			IEnumerable<IntersectionPoint3D> intersectionsPerRing,
			Action<IntersectionPoint3D, IntersectionPoint3D> pairAction)
		{
			IntersectionPoint3D previous = null;
			IntersectionPoint3D first = null;
			foreach (IntersectionPoint3D intersection in intersectionsPerRing)
			{
				if (first == null)
				{
					first = intersection;
				}

				if (previous != null)
				{
					pairAction(previous, intersection);
				}

				previous = intersection;
			}

			if (first != null && ! first.Equals(previous))
			{
				// Compare last with first

				pairAction(previous, first);
			}
		}

		public HashSet<IntersectionPoint3D> GetOtherIntersections(
			[NotNull] IntersectionPoint3D atIntersection)
		{
			var resultSet = new HashSet<IntersectionPoint3D>();

			foreach (IntersectionPoint3D intersection in
			         GetOtherSourceIntersections(atIntersection))
			{
				if (resultSet.Contains(intersection))
				{
					continue;
				}

				resultSet.Add(intersection);
			}

			foreach (IntersectionPoint3D intersection in
			         GetOtherTargetIntersections(atIntersection))
			{
				if (resultSet.Contains(intersection))
				{
					continue;
				}

				resultSet.Add(intersection);
			}

			return resultSet;
		}

		public bool HasMultipleSourceIntersections([NotNull] IntersectionPoint3D atIntersection)
		{
			if (_multipleSourceIntersections == null || _multipleSourceIntersections.Count == 0)
			{
				return false;
			}

			return _multipleSourceIntersections.Contains(atIntersection);
		}

		public bool HasMultipleTargetIntersections([NotNull] IntersectionPoint3D atIntersection)
		{
			if (_multipleTargetIntersections == null || _multipleTargetIntersections.Count == 0)
			{
				return false;
			}

			return _multipleTargetIntersections.Contains(atIntersection);
		}

		public IEnumerable<IntersectionPoint3D> GetOtherSourceIntersections(
			[NotNull] IntersectionPoint3D atIntersection)
		{
			if (_multipleSourceIntersections == null || _multipleSourceIntersections.Count == 0)
			{
				yield break;
			}

			foreach (IntersectionPoint3D other in _multipleSourceIntersections)
			{
				if (other == atIntersection)
				{
					continue;
				}

				if (other.ReferencesSameTargetVertex(atIntersection, _target, _tolerance))
				{
					yield return other;
				}
			}
		}

		public bool HasOtherTargetIntersections(
			[NotNull] IntersectionPoint3D atIntersection,
			out List<IntersectionPoint3D> cluster)
		{
			cluster = null;

			if (_multipleSourceIntersections == null)
			{
				return false;
			}

			cluster = GetOtherTargetIntersections(atIntersection, true).ToList();

			return cluster.Count != 0;
		}

		public IEnumerable<IntersectionPoint3D> GetOtherTargetIntersections(
			[NotNull] IntersectionPoint3D atIntersection,
			bool allowSourcePartJump = false)
		{
			if (_multipleTargetIntersections == null)
			{
				yield break;
			}

			foreach (IntersectionPoint3D other in _multipleTargetIntersections)
			{
				if (other == atIntersection)
				{
					continue;
				}

				if (other.ReferencesSameSourceVertex(atIntersection, _source, _tolerance))
				{
					yield return other;
				}
			}

			if (! allowSourcePartJump ||
			    _multipleSourceIntersections == null ||
			    ! _multipleSourceIntersections.Contains(atIntersection))
			{
				yield break;
			}

			// The source also has multiple intersections at the same place (e.g. touching rings):
			// Get another other source intersection to find same target index intersection from there
			foreach (IntersectionPoint3D other in GetOtherSourceIntersections(atIntersection))
			{
				if (other == atIntersection)
				{
					// Prevent stack overflow
					continue;
				}

				foreach (IntersectionPoint3D otherPartTarget in GetOtherTargetIntersections(other))
				{
					if (otherPartTarget == atIntersection)
					{
						continue;
					}

					yield return otherPartTarget;
				}
			}
		}

		public bool SourceClusterContains(IntersectionPoint3D intersection)
		{
			return _multipleSourceIntersections != null &&
			       _multipleSourceIntersections.Contains(intersection);
		}

		/// <summary>
		/// Returns the source boundary loops, if their connection point is part of the cluster,
		/// i.e. if there is an intersection with the target at the connection point.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<BoundaryLoop> GetSourceBoundaryLoops()
		{
			if (_multipleSourceIntersections == null)
			{
				yield break;
			}

			if (_sourceBoundaryLoops == null)
			{
				_sourceBoundaryLoops = CalculateSourceBoundaryLoops().ToList();
			}

			foreach (BoundaryLoop sourceBoundaryLoop in _sourceBoundaryLoops)
			{
				yield return sourceBoundaryLoop;
			}
		}

		/// <summary>
		/// Returns the source boundary loops, if their connection point is part of the cluster,
		/// i.e. if there is an intersection with the target at the connection point.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<BoundaryLoop> GetTargetBoundaryLoops()
		{
			if (_multipleTargetIntersections == null)
			{
				yield break;
			}

			if (_targetBoundaryLoops == null)
			{
				_targetBoundaryLoops = CalculateTargetBoundaryLoops().ToList();
				//_targetBoundaryLoops = CalculateTargetBoundaryLoops().Distinct(new BoundaryLoopComparer()).ToList();
			}

			foreach (BoundaryLoop boundaryLoop in _targetBoundaryLoops)
			{
				yield return boundaryLoop;
			}
		}

		private IEnumerable<BoundaryLoop> CalculateSourceBoundaryLoops()
		{
			if (_multipleSourceIntersections == null)
			{
				yield break;
			}

			// One N-ary BoundaryLoop per same-XY pinch group on each source ring (a
			// ring that visits one pinch XY location N times yields ONE BoundaryLoop emitting
			// N atomic sub-rings. Multiple pinch groups on the same source
			// ring are consolidated via legacy chain-merge:
			//  - Groups whose start references the same source vertex as the start or
			//    end of an already-kept BL are kept as a SEPARATE BL (legacy: pinch
			//    pairs at the same source pinch location for different target rings).
			//  - Other groups (cross-source-XY) are recorded as ExtraLoopIntersections
			//    on ALL already-kept BLs so their GetLoopSubcurves can recurse through
			//    them.
			foreach (var perPart in _multipleSourceIntersections
				         .GroupBy(i => i.SourcePartIndex))
			{
				Linestring sourceRing = _source.GetPart(perPart.Key);

				List<List<IntersectionPoint3D>> pinchGroups =
					GroupBySameTargetVertex(perPart, _target, _tolerance)
						.Where(g => g.Count >= 2 &&
						            g.All(p => p.TargetPartIndex == g[0].TargetPartIndex))
						.Select(g => g.OrderBy(p => p.VirtualSourceVertex).ToList())
						.Where(g => IsRealLoopAlongSource(g, sourceRing))
						.OrderBy(g => g[0].Point.X)
						.ThenBy(g => g[0].Point.Y)
						.ToList();

				if (pinchGroups.Count == 0)
				{
					continue;
				}

				var keptBoundaryLoops = new List<BoundaryLoop>();

				foreach (List<IntersectionPoint3D> pinchGroup in pinchGroups)
				{
					var candidate = new BoundaryLoop(pinchGroup, sourceRing,
					                                 isSourceRing: true,
					                                 tolerance: _tolerance);

					if (keptBoundaryLoops.Count == 0)
					{
						keptBoundaryLoops.Add(candidate);
						continue;
					}

					bool sharesSourcePinch =
						keptBoundaryLoops.Any(bl =>
							                      bl.Pinches.Any(p => p.ReferencesSameSourceVertex(
								                                     candidate.Start, _source,
								                                     _tolerance)));

					if (sharesSourcePinch)
					{
						// Same source pinch location for a different target vertex →
						// keep as a separate BL (legacy backward-compat).
						keptBoundaryLoops.Add(candidate);
					}
					else
					{
						// Cross-source-XY pinch group → record as extras on all kept BLs.
						foreach (BoundaryLoop kept in keptBoundaryLoops)
						{
							kept.AddExtraLoopIntersections(candidate.Start, candidate.End);
						}
					}
				}

				foreach (BoundaryLoop bl in keptBoundaryLoops)
				{
					yield return bl;
				}
			}
		}

		private IEnumerable<BoundaryLoop> CalculateTargetBoundaryLoops()
		{
			if (_multipleTargetIntersections == null)
			{
				yield break;
			}

			foreach (var perPart in _multipleTargetIntersections
				         .GroupBy(i => i.TargetPartIndex))
			{
				Linestring targetRing = _target.GetPart(perPart.Key);

				List<List<IntersectionPoint3D>> pinchGroups =
					GroupBySameSourceVertex(perPart, _source, _tolerance)
						.Where(g => g.Count >= 2 &&
						            g.All(p => p.SourcePartIndex == g[0].SourcePartIndex))
						.Select(g => g.OrderBy(p => p.VirtualTargetVertex).ToList())
						.Where(g => IsRealLoopAlongTarget(g, targetRing))
						.OrderBy(g => g[0].Point.X)
						.ThenBy(g => g[0].Point.Y)
						.ToList();

				foreach (List<IntersectionPoint3D> pinchGroup in pinchGroups)
				{
					yield return new BoundaryLoop(pinchGroup, targetRing,
					                              isSourceRing: false,
					                              tolerance: _tolerance);
				}
			}
		}

		/// <summary>
		/// Every consecutive pinch point (in source-vertex order, with wrap-around) must
		/// be separated by more than one source segment. Filters out colocated source
		/// intersections that share a target vertex but do not actually loop along the
		/// source (e.g., a single source position crossed twice by a target pinch).
		/// </summary>
		private static bool IsRealLoopAlongSource(
			[NotNull] IList<IntersectionPoint3D> pinchPoints,
			[NotNull] Linestring sourceRing)
		{
			int n = pinchPoints.Count;
			for (int i = 0; i < n; i++)
			{
				IntersectionPoint3D p1 = pinchPoints[i];
				IntersectionPoint3D p2 = pinchPoints[(i + 1) % n];

				double dist = p2.VirtualSourceVertex - p1.VirtualSourceVertex;
				if (dist < 0)
				{
					dist += sourceRing.SegmentCount;
				}

				if (Math.Floor(dist) <= 1)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Target-side counterpart of <see cref="IsRealLoopAlongSource"/>: every
		/// consecutive pinch in the group (in target-vertex order, with wrap-around)
		/// must be separated by more than one target segment.
		/// </summary>
		private static bool IsRealLoopAlongTarget(
			[NotNull] IList<IntersectionPoint3D> pinches,
			[NotNull] Linestring targetRing)
		{
			int n = pinches.Count;
			for (int i = 0; i < n; i++)
			{
				IntersectionPoint3D p1 = pinches[i];
				IntersectionPoint3D p2 = pinches[(i + 1) % n];

				double dist = p2.VirtualTargetVertex - p1.VirtualTargetVertex;
				if (dist < 0)
				{
					dist += targetRing.SegmentCount;
				}

				if (Math.Floor(dist) <= 1)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Partitions the supplied intersections into clusters that reference the same
		/// target vertex (within tolerance). Used to group coinciding source pinch points
		/// that share an opposite target point.
		/// </summary>
		private static IEnumerable<List<IntersectionPoint3D>> GroupBySameTargetVertex(
			[NotNull] IEnumerable<IntersectionPoint3D> intersections,
			[NotNull] ISegmentList target,
			double tolerance)
		{
			var clusters = new List<List<IntersectionPoint3D>>();

			foreach (IntersectionPoint3D ip in intersections)
			{
				List<IntersectionPoint3D> match =
					clusters.FirstOrDefault(c => c[0].ReferencesSameTargetVertex(
						                        ip, target, tolerance));

				if (match != null)
				{
					match.Add(ip);
				}
				else
				{
					clusters.Add(new List<IntersectionPoint3D> { ip });
				}
			}

			return clusters;
		}

		/// <summary>
		/// Symmetric counterpart of <see cref="GroupBySameTargetVertex"/> for target
		/// pinch points: partition by source-vertex equivalence.
		/// </summary>
		private static IEnumerable<List<IntersectionPoint3D>> GroupBySameSourceVertex(
			[NotNull] IEnumerable<IntersectionPoint3D> intersections,
			[NotNull] ISegmentList source,
			double tolerance)
		{
			var clusters = new List<List<IntersectionPoint3D>>();

			foreach (IntersectionPoint3D ip in intersections)
			{
				List<IntersectionPoint3D> match =
					clusters.FirstOrDefault(c => c[0].ReferencesSameSourceVertex(
						                        ip, source, tolerance));

				if (match != null)
				{
					match.Add(ip);
				}
				else
				{
					clusters.Add(new List<IntersectionPoint3D> { ip });
				}
			}

			return clusters;
		}
	}
}

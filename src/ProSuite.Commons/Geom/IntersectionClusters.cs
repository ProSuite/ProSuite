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
					});
			}

			return result.Count == 0 ? null : result;
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
			foreach (var sourceLoopIntersections in GetSourceBoundaryLoopIntersections()
				         .GroupBy(t => t.Item1.SourcePartIndex))
			{
				// Process intersections grouped by source part - there could be multiple adjoining loops in one ring!
				foreach (BoundaryLoop boundaryLoop in BoundaryLoop.CreateSourceBoundaryLoops(
					         sourceLoopIntersections, _source, _tolerance))
				{
					yield return boundaryLoop;
				}
			}
		}

		private IEnumerable<Tuple<IntersectionPoint3D, IntersectionPoint3D>>
			GetSourceBoundaryLoopIntersections()
		{
			foreach (var intersectionPairs
			         in CollectionUtils.GetAllTuples(_multipleSourceIntersections))
			{
				IntersectionPoint3D intersection1 = intersectionPairs.Key;
				IntersectionPoint3D intersection2 = intersectionPairs.Value;

				if (intersection1.SourcePartIndex != intersection2.SourcePartIndex)
				{
					continue;
				}

				if (intersection1.TargetPartIndex != intersection2.TargetPartIndex)
				{
					continue;
				}

				if (! intersection1.ReferencesSameTargetVertex(intersection2, _target, _tolerance))
				{
					continue;
				}

				if (SegmentIntersectionUtils.SourceSegmentCountBetween(
					    _source, intersection1, intersection2) > 1 &&
				    SegmentIntersectionUtils.SourceSegmentCountBetween(
					    _source, intersection2, intersection1) > 1)
				{
					yield return new Tuple<IntersectionPoint3D, IntersectionPoint3D>(
						intersection1, intersection2);
				}
			}
		}

		private IEnumerable<BoundaryLoop> CalculateTargetBoundaryLoops()
		{
			foreach (var intersectionPairs
			         in CollectionUtils.GetAllTuples(_multipleTargetIntersections))
			{
				var intersection1 = intersectionPairs.Key;
				var intersection2 = intersectionPairs.Value;

				if (intersection1.SourcePartIndex != intersection2.SourcePartIndex)
				{
					continue;
				}

				if (intersection1.TargetPartIndex != intersection2.TargetPartIndex)
				{
					continue;
				}

				if (! intersection1.ReferencesSameSourceVertex(intersection2, _source, _tolerance))
				{
					continue;
				}

				if (SegmentIntersectionUtils.TargetSegmentCountBetween(
					    _target, intersection1, intersection2) > 1 &&
				    SegmentIntersectionUtils.TargetSegmentCountBetween(
					    _target, intersection2, intersection1) > 1)
				{
					Linestring fullRing = _target.GetPart(intersection1.TargetPartIndex);

					yield return new BoundaryLoop(intersection1, intersection2, fullRing, false);
				}
			}
		}
	}
}

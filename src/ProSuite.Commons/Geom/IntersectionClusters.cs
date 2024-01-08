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
	internal class IntersectionClusters
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
		/// Finds intersections at the same location which reference the same
		/// target location. These could be just a break in a linear intersection
		/// or a boundary loop in the source
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
						}
					});
			}

			return result.Count == 0 ? null : result;
		}

		/// <summary>
		/// Finds intersections at the same location which reference the same
		/// target location. These could be just a break in a linear intersection
		/// or a boundary loop in the source
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
							// TODO: Extract GeometryIntersections class/interrace from SubcurveIntersectionPointNavigator (or rename it)
							// This class should be interrogated for disallowed navigation at specific intersections 
							// also by RelationalOperators. -> Remove the DisallowTargetForward flags on the intersection point
							if (p1.TargetPartIndex == p2.TargetPartIndex)
							{
								double segmentRatioDistance =
									SegmentIntersectionUtils.GetVirtualVertexRatioDistance(
										p1.VirtualTargetVertex, p2.VirtualTargetVertex,
										_target.GetPart(p1.TargetPartIndex).SegmentCount);

								// Typically it is very very small, but theoretically it could be almost the entire segments
								// if the angle is extremely acute.
								if (Math.Abs(segmentRatioDistance) < 2)
								{
									if (segmentRatioDistance > 0)
									{
										// p1 is just before p2 along target
										p1.DisallowTargetForward = true;
										p2.DisallowTargetBackward = true;
									}
									else if (segmentRatioDistance < 0)
									{
										// p1 is just after p2 along target
										p1.DisallowTargetBackward = true;
										p2.DisallowTargetForward = true;
									}
								}
							}
						}
					});
			}

			return result.Count == 0 ? null : result;
		}

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

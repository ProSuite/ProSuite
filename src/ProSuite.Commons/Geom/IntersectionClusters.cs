using System;
using System.Collections.Generic;
using System.Linq;
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
	}
}
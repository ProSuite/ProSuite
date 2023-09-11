using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Provides functionality that determines whether two intersection points are a
	/// <see cref="LinearIntersectionPseudoBreak"/> which can be ignored in some situations.
	/// Additionally, boundary loops are identified during the processing of intersection pairs.
	/// </summary>
	internal class LinearIntersectionBreakEvaluator
	{
		/// <summary>
		/// Returns the intersection points at the ring start/end points which exist only because
		/// of the ring start/end point. When the intersection points are calculated these
		/// interceptions of linear intersection stretch through the ring's null points can
		/// optionally be included and excluded dynamically for some operations.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments"></param>
		/// <param name="intersectionPoints"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		internal IEnumerable<LinearIntersectionPseudoBreak> GetLinearIntersectionBreaksAtRingStart(
			ISegmentList sourceSegments,
			ISegmentList targetSegments,
			ICollection<IntersectionPoint3D> intersectionPoints,
			double tolerance = double.NaN)
		{
			if (intersectionPoints.Count == 0)
			{
				yield break;
			}

			if (! sourceSegments.IsClosed && ! targetSegments.IsClosed)
			{
				yield break;
			}

			foreach (IGrouping<int, IntersectionPoint3D> intersectionsByPart in
			         intersectionPoints.GroupBy(p => p.SourcePartIndex))
			{
				var orderedIntersections =
					intersectionsByPart.OrderBy(i => i.VirtualSourceVertex).ToList();

				if (orderedIntersections.Count < 2)
				{
					continue;
				}

				const bool includeBoundaryLoops = false;
				// Target end/start break:
				foreach (LinearIntersectionPseudoBreak pseudoBreak in
				         GetLinearIntersectionPseudoBreaks(
					         sourceSegments, targetSegments, orderedIntersections, true,
					         includeBoundaryLoops, tolerance))
				{
					yield return pseudoBreak;
				}

				// Now check the source-end to source-start break
				IntersectionPoint3D ringEnd = orderedIntersections.Last();
				IntersectionPoint3D ringStart = orderedIntersections[0];

				if (IsLinearIntersectionPseudoBreak(
					    ringEnd, ringStart, sourceSegments, targetSegments,
					    tolerance,
					    out LinearIntersectionPseudoBreak ringNullPointBreak))
				{
					yield return ringNullPointBreak;
				}
			}
		}

		/// <summary>
		/// Returns linear intersections end/start points that are within a linear intersection
		/// stretch and do not start or end the linear intersection from a 2D perspective.
		/// </summary>
		/// <param name="intersectionPoints"></param>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		internal IEnumerable<LinearIntersectionPseudoBreak> GetLinearIntersectionPseudoBreaks(
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPoints,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target)
		{
			var orderedIntersections =
				intersectionPoints.OrderBy(i => i.SourcePartIndex)
				                  .ThenBy(i => i.VirtualSourceVertex).ToList();

			foreach (LinearIntersectionPseudoBreak pseudoBreak in
			         GetLinearIntersectionPseudoBreaks(source, target, orderedIntersections))
			{
				yield return pseudoBreak;
			}
		}

		private IEnumerable<LinearIntersectionPseudoBreak> GetLinearIntersectionPseudoBreaks(
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target,
			[NotNull] IEnumerable<IntersectionPoint3D> orderedIntersections,
			bool onlyOnTargetNullPoint = false,
			bool includeBoundaryLoops = false,
			double tolerance = double.NaN)
		{
			IntersectionPoint3D previous = null;
			foreach (IntersectionPoint3D current in orderedIntersections)
			{
				if (! onlyOnTargetNullPoint ||
				    SegmentIntersectionUtils.IsTargetRingNullPoint(
					    current.SegmentIntersection, previous?.SegmentIntersection,
					    target))
				{
					if (IsLinearIntersectionPseudoBreak(
						    previous, current, source, target, tolerance,
						    out LinearIntersectionPseudoBreak pseudoBreak))
					{
						yield return pseudoBreak;
					}
					else if (includeBoundaryLoops && pseudoBreak?.IsBoundaryLoop == true)
					{
						yield return pseudoBreak;
					}
				}

				previous = current;
			}
		}

		private bool IsLinearIntersectionPseudoBreak(
			IntersectionPoint3D previousIntersection,
			IntersectionPoint3D currentIntersection,
			ISegmentList source,
			ISegmentList target,
			double tolerance,
			out LinearIntersectionPseudoBreak pseudoBreak)
		{
			pseudoBreak = null;

			if (previousIntersection == null ||
			    previousIntersection.Type != IntersectionPointType.LinearIntersectionEnd ||
			    currentIntersection.Type != IntersectionPointType.LinearIntersectionStart)
			{
				return false;
			}

			bool adjacent = SegmentIntersectionUtils.AreIntersectionsAdjacent(
				previousIntersection, currentIntersection,
				source, target, out bool isSourceBoundaryLoop, out bool isTargetBoundaryLoop,
				tolerance);

			if (adjacent || isSourceBoundaryLoop || isTargetBoundaryLoop)
			{
				pseudoBreak =
					new LinearIntersectionPseudoBreak(previousIntersection, currentIntersection)
					{
						IsSourceBoundaryLoop = isSourceBoundaryLoop,
						IsTargetBoundaryLoop = isTargetBoundaryLoop
					};
			}

			return adjacent;
		}
	}
}

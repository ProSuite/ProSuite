using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	internal class SegmentIntersectionFilter
	{
		private readonly ISegmentList _source;

		private readonly Dictionary<int, HashSet<double>> _usedIntersectionFactorsByPart =
			new Dictionary<int, HashSet<double>>();

		private static readonly List<int> _collectedShortSourceSegments = new List<int>();

		public SegmentIntersectionFilter(ISegmentList source)
		{
			_source = source;
		}

		public IEnumerable<SegmentIntersection> GetFilteredIntersectionsOrderedAlongSourceSegments(
			[NotNull] IEnumerable<SegmentIntersection> intersections)
		{
			var intersectionsForCurrentSourceSegment =
				new List<SegmentIntersection>(3);

			int currentIndex = -1;
			foreach (SegmentIntersection intersection in intersections)
			{
				if (intersection.SourceIndex != currentIndex)
				{
					currentIndex = intersection.SourceIndex;

					foreach (
						SegmentIntersection collectedIntersection in
						FilterIntersections(intersectionsForCurrentSourceSegment, _source)
							.OrderBy(i => i.GetFirstIntersectionAlongSource()))
					{
						yield return collectedIntersection;
					}

					intersectionsForCurrentSourceSegment.Clear();
				}

				CollectIntersection(intersection, _source, _usedIntersectionFactorsByPart,
				                    intersectionsForCurrentSourceSegment);
			}

			foreach (
				SegmentIntersection collectedIntersection in
				FilterIntersections(intersectionsForCurrentSourceSegment, _source)
					.OrderBy(i => i.GetFirstIntersectionAlongSource()))
			{
				yield return collectedIntersection;
			}
		}

		private IEnumerable<SegmentIntersection> FilterIntersections(
			[NotNull] IEnumerable<SegmentIntersection> intersectionsForSourceSegment,
			[NotNull] ISegmentList source)
		{
			foreach (SegmentIntersection intersection in intersectionsForSourceSegment)
			{
				var filter = false;

				if (! intersection.HasLinearIntersection &&
				    intersection.SingleInteriorIntersectionFactor == null)
				{
					double intersectionFactor;
					if (intersection.SourceStartIntersects)
					{
						intersectionFactor = 0;
					}
					else if (intersection.SourceEndIntersects)
					{
						intersectionFactor = 1;
					}
					else if (intersection.TargetStartIntersects)
					{
						intersectionFactor =
							Assert.NotNull(intersection.TargetStartFactor).Value;
					}
					else
					{
						intersectionFactor =
							Assert.NotNull(intersection.TargetEndFactor).Value;
					}

					int partIndex;
					int localSegmentIndex =
						source.GetLocalSegmentIndex(intersection.SourceIndex, out partIndex);

					double intersectionFactorPartGlobal = localSegmentIndex + intersectionFactor;

					// TODO: Extract class IntersectionFactors which encapsulates all the
					// Contains, Add etc. methods, probably even the filtering
					if (ContainsIntersection(_usedIntersectionFactorsByPart, partIndex,
					                         intersectionFactorPartGlobal))
					{
						filter = true;
					}
					else
					{
						AddIntersectionFactor(intersectionFactorPartGlobal, partIndex,
						                      _usedIntersectionFactorsByPart, source);
					}
				}

				if (! filter)
				{
					yield return intersection;
				}
			}
		}

		private static bool ContainsIntersection(
			IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart,
			int sourcePartIndex, double intersectionFactor)
		{
			HashSet<double> usedIntersectionFactors;
			return usedIntersectionFactorsByPart.TryGetValue(
				       sourcePartIndex, out usedIntersectionFactors) &&
			       usedIntersectionFactors.Contains(intersectionFactor);
		}

		private static void AddIntersectionFactor(
			double localIntersectionFactor,
			int partIndex,
			[NotNull] IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart)
		{
			HashSet<double> localIntersectionFactors;

			if (! usedIntersectionFactorsByPart.TryGetValue(partIndex, out
			                                                localIntersectionFactors))
			{
				localIntersectionFactors = new HashSet<double>();
				usedIntersectionFactorsByPart.Add(partIndex, localIntersectionFactors);
			}

			localIntersectionFactors.Add(localIntersectionFactor);
		}

		private static void AddIntersectionFactor(
			double localIntersectionFactor,
			int partIndex,
			[NotNull] IDictionary<int, HashSet<double>> usedIntersectionFactorsByPart,
			[NotNull] ISegmentList source)
		{
			Linestring part = source.GetPart(partIndex);

			if (IsRingStartOrEnd(part, localIntersectionFactor))
			{
				// add both start and end point
				AddIntersectionFactor(0, partIndex, usedIntersectionFactorsByPart);
				AddIntersectionFactor(part.SegmentCount, partIndex, usedIntersectionFactorsByPart);
			}
			else
			{
				AddIntersectionFactor(localIntersectionFactor, partIndex,
				                      usedIntersectionFactorsByPart);
			}
		}

		private static bool IsRingStartOrEnd([NotNull] Linestring linestring,
		                                     double localVertexIndex)
		{
			Assert.False(double.IsNaN(localVertexIndex), "localVertexIndex is NaN");

			var vertexIndexIntegral = (int) localVertexIndex;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			bool isVertex = localVertexIndex == vertexIndexIntegral;

			if (! isVertex)
			{
				return false;
			}

			if (linestring.IsFirstPointInPart(vertexIndexIntegral) ||
			    linestring.IsLastPointInPart(vertexIndexIntegral))
			{
				return linestring.IsClosed;
			}

			return false;
		}

		private static void CollectIntersection(
			[NotNull] SegmentIntersection intersection,
			[NotNull] ISegmentList source,
			[NotNull] IDictionary<int, HashSet<double>> allLinearIntersectionFactors,
			[NotNull] ICollection<SegmentIntersection> resultIntersections)
		{
			// Collect segments for current index in list, unless they are clearly not needed 
			// (and would need to be filtered by a later linear intersection if added)

			bool isLinear = intersection.HasLinearIntersection;

			if (isLinear)
			{
				int partIndex;
				int localSegmentIndex =
					source.GetLocalSegmentIndex(intersection.SourceIndex, out partIndex);

				double linearIntersectionStartFactor =
					localSegmentIndex +
					intersection.GetLinearIntersectionStartFactor(true);

				double linearIntersectionEndFactor =
					localSegmentIndex +
					intersection.GetLinearIntersectionEndFactor(true);

				if (intersection.IsSourceZeroLength2D &&
				    ContainsIntersection(allLinearIntersectionFactors,
				                         partIndex, linearIntersectionEndFactor))
				{
					if (intersection.SourceEndIntersects &&
					    source.IsLastSegmentInPart(intersection.SourceIndex) &&
					    ! _collectedShortSourceSegments.Contains(intersection.SourceIndex))
					{
						// Use it to make sure to connect back to start point,
						// but only use it once (they tend to have 'linear' intersection
						// with multiple target segments because both the short segment's
						// start and end points are within the tolerance of several target
						// segments. This requires more unit tests with vertical segments!
						_collectedShortSourceSegments.Add(intersection.SourceIndex);
					}
					else
					{
						return;
					}
				}

				if (intersection.IsTargetZeroLength2D &&
				    ContainsIntersection(allLinearIntersectionFactors,
				                         partIndex, linearIntersectionEndFactor))
				{
					// avoid double linear segments if the target segment is 2D-'short' (or vertical) 
					return;
				}

				AddIntersectionFactor(linearIntersectionStartFactor, partIndex,
				                      allLinearIntersectionFactors, source);

				AddIntersectionFactor(linearIntersectionEndFactor, partIndex,
				                      allLinearIntersectionFactors, source);
			}

			if (! isLinear && intersection.SourceStartIntersects &&
			    source.IsFirstSegmentInPart(intersection.SourceIndex) && source.IsClosed)
			{
				// will be reported again at the end
				return;
			}

			if (! isLinear && intersection.SourceEndIntersects &&
			    ! source.IsLastSegmentInPart(intersection.SourceIndex))
			{
				// will be reported again at next segment
				return;
			}

			resultIntersections.Add(intersection);
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	internal class SegmentIntersectionFilter
	{
		private readonly ISegmentList _source;

		private readonly IntersectionFactors _usedIntersectionFactors;

		private static readonly List<int> _collectedShortSourceSegments = new List<int>();

		public SegmentIntersectionFilter(ISegmentList source)
		{
			_source = source;

			_usedIntersectionFactors =
				new IntersectionFactors(
					MathUtils.GetDoubleSignificanceEpsilon(_source.XMax, _source.YMax));
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

				CollectIntersection(intersection, _source, _usedIntersectionFactors,
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

					int localSegmentIndex =
						source.GetLocalSegmentIndex(intersection.SourceIndex, out int partIndex);

					double intersectionFactorPartGlobal = localSegmentIndex + intersectionFactor;

					if (_usedIntersectionFactors.Contains(partIndex, intersectionFactorPartGlobal))
					{
						filter = true;
					}
					else
					{
						AddIntersectionFactor(intersectionFactorPartGlobal, partIndex,
						                      _usedIntersectionFactors, source);
					}
				}

				if (! filter)
				{
					yield return intersection;
				}
			}
		}

		private static void AddIntersectionFactor(
			double localIntersectionFactor,
			int partIndex,
			[NotNull] IntersectionFactors intersectionFactors,
			[NotNull] ISegmentList source)
		{
			Linestring part = source.GetPart(partIndex);

			if (IsRingStartOrEnd(part, localIntersectionFactor))
			{
				// add both start and end point
				intersectionFactors.Add(partIndex, 0);
				intersectionFactors.Add(partIndex, part.SegmentCount);
			}
			else
			{
				intersectionFactors.Add(partIndex, localIntersectionFactor);
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
			[NotNull] IntersectionFactors allLinearIntersectionFactors,
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
				    allLinearIntersectionFactors.Contains(
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
				    allLinearIntersectionFactors.Contains(
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

		private class IntersectionFactors
		{
			private readonly IntersectionFactorComparer _comparer;

			private readonly Dictionary<int, SortedList<double, int>> _intersectionFactorsByPart =
				new Dictionary<int, SortedList<double, int>>();

			internal IntersectionFactors(double epsilon)
			{
				_comparer = new IntersectionFactorComparer(epsilon);
			}

			internal void Add(int partIndex, double localIntersectionFactor)
			{
				SortedList<double, int> localIntersectionFactors;

				if (! _intersectionFactorsByPart.TryGetValue(partIndex, out
				                                             localIntersectionFactors))
				{
					localIntersectionFactors = new SortedList<double, int>(_comparer);
					_intersectionFactorsByPart.Add(partIndex, localIntersectionFactors);
				}

				if (! localIntersectionFactors.ContainsKey(localIntersectionFactor))
				{
					localIntersectionFactors.Add(localIntersectionFactor, 0);
				}
			}

			internal bool Contains(
				int sourcePartIndex, double intersectionFactor)
			{
				SortedList<double, int> usedIntersectionFactors;

				if (! _intersectionFactorsByPart.TryGetValue(
					    sourcePartIndex, out usedIntersectionFactors))
				{
					return false;
				}

				return usedIntersectionFactors.ContainsKey(intersectionFactor);
			}
		}

		private class IntersectionFactorComparer : IComparer<double>
		{
			private readonly double _epsilon;

			public IntersectionFactorComparer(double epsilon)
			{
				_epsilon = epsilon;
			}

			#region Implementation of IComparer<in double>

			public int Compare(double x, double y)
			{
				double delta = x - y;

				if (delta < -_epsilon)
					return -1;

				if (delta > _epsilon)
					return 1;

				return 0;
			}

			#endregion
		}
	}
}

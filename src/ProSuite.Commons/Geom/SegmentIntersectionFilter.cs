using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	internal class SegmentIntersectionFilter
	{
		[NotNull] private readonly ISegmentList _source;
		[NotNull] private readonly ISegmentList _target;

		private readonly IntersectionFactors _usedIntersectionFactors;

		private static readonly List<int> _collectedShortSourceSegments = new List<int>();

		public SegmentIntersectionFilter([NotNull] ISegmentList source,
		                                 [NotNull] ISegmentList target)
		{
			_source = source;
			_target = target;

			_usedIntersectionFactors =
				new IntersectionFactors(
					MathUtils.GetDoubleSignificanceEpsilon(_source.XMax, _source.YMax));
		}

		public IEnumerable<SegmentIntersection> GetFilteredIntersectionsOrderedAlongSourceSegments(
			[NotNull] IEnumerable<SegmentIntersection> intersections)
		{
			var intersectionsForCurrentSourceSegment =
				new List<SegmentIntersection>(3);

			IComparer<SegmentIntersection> segmentIntersectionComparer =
				new SegmentIntersectionAlongSourceComparer(_source, _target);

			int currentIndex = -1;
			foreach (SegmentIntersection intersection in intersections)
			{
				if (intersection.SourceIndex != currentIndex)
				{
					currentIndex = intersection.SourceIndex;

					foreach (
						SegmentIntersection collectedIntersection in
						FilterIntersections(intersectionsForCurrentSourceSegment, _source, _target)
							.OrderBy(i => i, segmentIntersectionComparer))
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
				FilterIntersections(intersectionsForCurrentSourceSegment, _source, _target)
					.OrderBy(i => i, segmentIntersectionComparer))
			{
				yield return collectedIntersection;
			}
		}

		private IEnumerable<SegmentIntersection> FilterIntersections(
			[NotNull] IEnumerable<SegmentIntersection> intersectionsForSourceSegment,
			[NotNull] ISegmentList source,
			[NotNull] ISegmentList target)
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

					if (_usedIntersectionFactors.Contains(
						    partIndex, intersectionFactorPartGlobal,
						    out List<double> alongTargetVertex)
					    && ! IsIntersectionOnUnCrackedLine(intersection, alongTargetVertex, target))
					{
						// Typically the other one is a linear intersection that takes precedence. However,
						// only filter if it is not a cluster of intersections with different target segments
						filter = true;
					}
					else
					{
						double alongTarget = intersection.TargetIndex +
						                     intersection.GetIntersectionPointFactorAlongTarget();
						AddIntersectionFactor(intersectionFactorPartGlobal, partIndex,
						                      _usedIntersectionFactors, alongTarget, source);
					}
				}

				if (! filter)
				{
					yield return intersection;
				}
			}
		}

		private bool IsIntersectionOnUnCrackedLine(SegmentIntersection singleIntersection,
		                                           List<double> existingTargetIntersections,
		                                           [NotNull] ISegmentList target)
		{
			double intersection1AlongTargetSegment =
				singleIntersection.GetIntersectionPointFactorAlongTarget();

			double virtualTargetVertex1 =
				singleIntersection.TargetIndex + intersection1AlongTargetSegment;

			if (existingTargetIntersections.Contains(virtualTargetVertex1))
			{
				return false;
			}

			int localSegmentIndex1 = (int)
				(target.GetLocalSegmentIndex(singleIntersection.TargetIndex,
				                             out int part1Index)
				 + intersection1AlongTargetSegment);

			int alongTargetVertexInt = (int) existingTargetIntersections.First();
			int localSegmentIndex2 =
				target.GetLocalSegmentIndex(alongTargetVertexInt, out int part2Index);

			if (part1Index != part2Index)
			{
				// We're not interested in cross-part clustering here:
				return false;
			}

			// On the same part: 
			Linestring targetPart = _target.GetPart(part1Index);

			Pnt3D p1 = targetPart.GetPoint3D(localSegmentIndex1);
			Pnt3D p2 = targetPart.GetPoint3D(localSegmentIndex2);

			if (localSegmentIndex1 > 0 &&
			    localSegmentIndex1 < targetPart.SegmentCount)
			{
				// true if not quite matching (otherwise it could be a boundary loop)
				return ! p1.EqualsXY(p2, double.Epsilon);
			}

			if (localSegmentIndex1 == 0)
			{
				if (targetPart.IsLastPointInPart(localSegmentIndex2))
				{
					return false;
				}
			}
			else if (targetPart.IsLastPointInPart(localSegmentIndex1))
			{
				if (localSegmentIndex2 == 0)
				{
					return false;
				}
			}

			return ! p1.EqualsXY(p2, double.Epsilon);
		}

		private static void AddIntersectionFactor(
			double localIntersectionFactor,
			int partIndex,
			[NotNull] IntersectionFactors toIntersectionFactors,
			double targetFactor,
			[NotNull] ISegmentList source)
		{
			Linestring part = source.GetPart(partIndex);

			if (IsRingStartOrEnd(part, localIntersectionFactor))
			{
				// add both start and end point
				toIntersectionFactors.Add(partIndex, 0, targetFactor);
				toIntersectionFactors.Add(partIndex, part.SegmentCount, targetFactor);
			}
			else
			{
				toIntersectionFactors.Add(partIndex, localIntersectionFactor, targetFactor);
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
					    partIndex, linearIntersectionEndFactor, out _))
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
					    partIndex, linearIntersectionEndFactor, out _))
				{
					// avoid double linear segments if the target segment is 2D-'short' (or vertical) 
					return;
				}

				double startAlongTargetFactor = intersection.TargetIndex +
				                                intersection.GetRatioAlongTargetLinearStart();
				double endAlongTargetFactor = intersection.TargetIndex +
				                              intersection.GetRatioAlongTargetLinearEnd();

				AddIntersectionFactor(linearIntersectionStartFactor, partIndex,
				                      allLinearIntersectionFactors, startAlongTargetFactor, source);
				AddIntersectionFactor(linearIntersectionEndFactor, partIndex,
				                      allLinearIntersectionFactors, endAlongTargetFactor, source);
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

			private readonly Dictionary<int, SortedList<double, List<double>>>
				_intersectionFactorsByPart =
					new Dictionary<int, SortedList<double, List<double>>>();

			internal IntersectionFactors(double epsilon)
			{
				_comparer = new IntersectionFactorComparer(epsilon);
			}

			internal void Add(int partIndex,
			                  double localIntersectionFactor,
			                  double targetFactor)
			{
				SortedList<double, List<double>> localIntersectionFactors;

				if (! _intersectionFactorsByPart.TryGetValue(partIndex,
				                                             out localIntersectionFactors))
				{
					localIntersectionFactors =
						new SortedList<double, List<double>>(_comparer);
					_intersectionFactorsByPart.Add(partIndex, localIntersectionFactors);
				}

				List<double> targetFactors;
				if (! localIntersectionFactors.TryGetValue(localIntersectionFactor,
				                                           out targetFactors))
				{
					localIntersectionFactors.Add(localIntersectionFactor,
					                             new List<double> { targetFactor });
				}
				else
				{
					// ensure all target factors are in the values
					targetFactors.Add(targetFactor);
				}
			}

			internal bool Contains(
				int sourcePartIndex,
				double intersectionFactor,
				out List<double> alongTargetFactor)
			{
				SortedList<double, List<double>> usedIntersectionFactors;

				if (! _intersectionFactorsByPart.TryGetValue(
					    sourcePartIndex, out usedIntersectionFactors))
				{
					alongTargetFactor = new List<double>();
					return false;
				}

				bool result =
					usedIntersectionFactors.TryGetValue(intersectionFactor,
					                                    out alongTargetFactor);

				return result;
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

	internal class SegmentIntersectionAlongSourceComparer : IComparer<SegmentIntersection>
	{
		private readonly ISegmentList _source;
		private readonly ISegmentList _target;

		public SegmentIntersectionAlongSourceComparer(ISegmentList source, ISegmentList target)
		{
			_source = source;
			_target = target;
		}

		public int Compare(SegmentIntersection x, SegmentIntersection y)
		{
			if (ReferenceEquals(x, y))
				return 0;

			// This is probably one of the few cases where the null-check can have an impact on performance.
			double xAlong = x.GetFirstIntersectionAlongSource();
			double yAlong = y.GetFirstIntersectionAlongSource();

			double delta = xAlong - yAlong;

			if (delta < 0)
				return -1;

			if (delta > 0)
				return 1;

			// Now make sure we're not breaking up a linear intersection by a crossing just before.
			// The problem is that the intersection factors get snapped to 0, 1 so we need to be
			// careful. Theoretically it would be better to use the un-snapped intersection factor
			// but that would be a bit of a risky change.

			xAlong = GetExactRatioAlongSourceSegment(x);
			yAlong = GetExactRatioAlongSourceSegment(y);

			double exactDelta = xAlong - yAlong;

			if (exactDelta < 0)
				return -1;

			if (exactDelta > 0)
				return 1;

			return 0;
		}

		private double GetExactRatioAlongSourceSegment(
			[NotNull] SegmentIntersection segmentIntersection)
		{
			if (segmentIntersection.SingleInteriorIntersectionFactor != null)
			{
				return segmentIntersection.SingleInteriorIntersectionFactor.Value;
			}

			var thisLine = _source.GetSegment(segmentIntersection.SourceIndex);
			var otherLine = _target.GetSegment(segmentIntersection.TargetIndex);

			Pnt3D targetPoint = otherLine.GetPointAlong(
				segmentIntersection.GetIntersectionPointFactorAlongTarget(), true);

			thisLine.GetDistanceXYPerpendicularSigned(
				targetPoint, out double result);

			return result;
		}
	}
}

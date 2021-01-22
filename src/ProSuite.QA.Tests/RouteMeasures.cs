using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class RouteMeasures
	{
		[NotNull] private readonly IList<double> _xyTolerances;
		[NotNull] private readonly IList<double> _mTolerances;

		[NotNull] private readonly Dictionary<object, List<CurveMeasureRange>> _rangesByRoute =
			new Dictionary<object, List<CurveMeasureRange>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RouteMeasures"/> class.
		/// </summary>
		/// <param name="mTolerances">The m tolerances.</param>
		/// <param name="xyTolerances">The xy tolerances.</param>
		public RouteMeasures([NotNull] IEnumerable<double> mTolerances,
		                     [NotNull] IEnumerable<double> xyTolerances)
		{
			Assert.ArgumentNotNull(mTolerances, nameof(mTolerances));
			Assert.ArgumentNotNull(xyTolerances, nameof(xyTolerances));

			_mTolerances = new List<double>(mTolerances);
			_xyTolerances = new List<double>(xyTolerances);
		}

		public void Add([NotNull] object routeId,
		                [NotNull] CurveMeasureRange curveMeasureRange)
		{
			Assert.ArgumentNotNull(routeId, nameof(routeId));
			Assert.ArgumentNotNull(curveMeasureRange, nameof(curveMeasureRange));

			List<CurveMeasureRange> ranges;
			if (! _rangesByRoute.TryGetValue(routeId, out ranges))
			{
				ranges = new List<CurveMeasureRange>();

				_rangesByRoute.Add(routeId, ranges);
			}

			ranges.Add(curveMeasureRange);
		}

		[NotNull]
		public IEnumerable<OverlappingMeasures> GetOverlaps()
		{
			return _rangesByRoute.SelectMany(pair => GetOverlaps(pair.Key, pair.Value));
		}

		public override string ToString()
		{
			int intervalCount = _rangesByRoute.Values.Sum(intervals => intervals.Count);

			return string.Format("Routes: {0} Intervals: {1}",
			                     _rangesByRoute.Count, intervalCount);
		}

		[NotNull]
		private IEnumerable<OverlappingMeasures> GetOverlaps(
			[NotNull] object routeId,
			[NotNull] IEnumerable<CurveMeasureRange> ranges)
		{
			// TODO recursively intersect the overlaps? --> get all features in one overlap that have a given range in common

			IList<CurveMeasureRange> sorted = GetSorted(ranges);

			for (int i = 0; i < sorted.Count - 1; i++)
			{
				CurveMeasureRange range1 = sorted[i];

				int start = i + 1;
				for (int j = start; j < sorted.Count; j++)
				{
					CurveMeasureRange range2 = sorted[j];

					OverlappingMeasures overlap = GetOverlap(range1, range2, routeId);
					if (overlap != null)
					{
						yield return overlap;
					}
				}
			}
		}

		[NotNull]
		private static IList<CurveMeasureRange> GetSorted(
			[NotNull] IEnumerable<CurveMeasureRange> ranges)
		{
			List<CurveMeasureRange> result = ranges.ToList();

			result.Sort((r1, r2) => r1.MMin.CompareTo(r2.MMin));

			return result;
		}

		[CanBeNull]
		private OverlappingMeasures GetOverlap([NotNull] CurveMeasureRange r1,
		                                       [NotNull] CurveMeasureRange r2,
		                                       [NotNull] object routeId)
		{
			Assert.ArgumentCondition(r1.MMin <= r2.MMin,
			                         "r1.MMin must not be larger than r2.MMin");

			double mTolerance = GetMTolerance(r1, r2);

			if (r1.MMax < r2.MMin)
			{
				// not overlapping
				return null;
			}

			if (r1.MMax - r2.MMin < mTolerance)
			{
				// r1 overlaps r2 by less than the m tolerance
				if (AreConnectedAtMMaxMMin(r1, r2))
				{
					// ... and the end points are connected and correspond to r1.MMax and r2.Min
					return null;
				}
			}

			double mMin = Math.Max(r1.MMin, r2.MMin);
			double mMax = Math.Min(r1.MMax, r2.MMax);

			var result = new OverlappingMeasures(routeId, mMin, mMax);

			result.Add(GetTestRowReference(r1));
			result.Add(GetTestRowReference(r2));

			return result;
		}

		private bool AreConnectedAtMMaxMMin([NotNull] CurveMeasureRange r1,
		                                    [NotNull] CurveMeasureRange r2)
		{
			if (r1.MMaxEndPoint == null || r2.MMinEndPoint == null)
			{
				return false;
			}

			double distanceSquared =
				r1.MMaxEndPoint.GetSquaredDistanceTo(r2.MMinEndPoint);

			return distanceSquared <= GetSquaredXyTolerance(r1, r2);
		}

		private double GetSquaredXyTolerance([NotNull] CurveMeasureRange r1,
		                                     [NotNull] CurveMeasureRange r2)
		{
			double xyTolerance = GetXyTolerance(r1, r2);

			return xyTolerance * xyTolerance;
		}

		private double GetMTolerance([NotNull] CurveMeasureRange r1,
		                             [NotNull] CurveMeasureRange r2)
		{
			return Math.Max(_mTolerances[r1.TableIndex],
			                _mTolerances[r2.TableIndex]);
		}

		private double GetXyTolerance([NotNull] CurveMeasureRange r1,
		                              [NotNull] CurveMeasureRange r2)
		{
			return Math.Max(_xyTolerances[r1.TableIndex],
			                _xyTolerances[r2.TableIndex]);
		}

		[NotNull]
		private static TestRowReference GetTestRowReference(
			[NotNull] CurveMeasureRange curveMeasureRange)
		{
			return new TestRowReference(curveMeasureRange.ObjectId,
			                            curveMeasureRange.TableIndex);
		}
	}
}

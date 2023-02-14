using System;
using System.Collections.Generic;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// Maintain a list of non-overlapping "gaps" along a curve.
	/// Each gap is identified by the distance along the curve of its two endpoints.
	/// </summary>
	public class GapList
	{
		private readonly List<Interval> _gaps; // ordered by gap start

		public GapList()
		{
			_gaps = new List<Interval>();
		}

		public int Count => _gaps.Count;

		public IEnumerable<Interval> Gaps => _gaps;

		public void AddGap(double start, double end)
		{
			var gap = Interval.Create(start, end);
			int index = _gaps.BinarySearch(gap, new GapComparer());
			if (index < 0) index = ~index;

			int imin = index;
			if (index > 0 && _gaps[index - 1].Overlaps(gap))
			{
				// exists gap before and it overlaps:
				imin -= 1;
			}

			int imax = imin;
			while (imax < _gaps.Count && _gaps[imax].Overlaps(gap))
			{
				gap = Merge(gap, _gaps[imax]);
				imax += 1;
			}

			if (imax > imin)
			{
				// gap touches existing gaps: update first of them, remove remaining
				_gaps[imin] = gap;
				_gaps.RemoveRange(imin + 1, imax - imin - 1); // O(N)
			}
			else
			{
				// lonely gap: insert
				_gaps.Insert(index, gap); // O(N)
			}
		}

		/// <summary>
		/// Merge gaps that are closer than the given <paramref name="tolerance"/>.
		/// </summary>
		/// <returns>How many merges occurred.</returns>
		public int MergeNearGaps(double tolerance)
		{
			int merged = 0;

			// from the end so we can remove:
			for (int i = _gaps.Count - 1; i > 0; i--)
			{
				if (_gaps[i].Min - _gaps[i - 1].Max < tolerance)
				{
					_gaps[i - 1] = Merge(_gaps[i - 1], _gaps[i]);
					_gaps.RemoveAt(i);
					merged += 1;
				}
			}

			return merged;
		}

		/// <summary>
		/// Drop gaps that are shorter than the given <paramref name="tolerance"/>.
		/// </summary>
		/// <returns>How many gaps were dropped.</returns>
		public int DropShortGaps(double tolerance)
		{
			int dropped = 0;

			// from the end so we can remove:
			for (int i = _gaps.Count - 1; i > 0; i--)
			{
				var gap = _gaps[i];
				if (gap.Max - gap.Min < tolerance)
				{
					_gaps.RemoveAt(i); // O(N)
					dropped += 1;
				}
			}

			return dropped;
		}

		private static Interval Merge(Interval x, Interval y)
		{
			// assert: x.Start <= y.Start
			// assert: x.Start+x.Length >= y.Start
			return Interval.Create(Math.Min(x.Min, y.Min), Math.Max(x.Max, y.Max));
		}

		private static int CompareGaps(Interval x, Interval y)
		{
			if (x.Min < y.Min) return -1;
			if (x.Min > y.Min) return +1;
			// assert: x.Min == y.Min
			if (x.Max < y.Max) return -1;
			if (x.Max > y.Max) return +1;
			// assert: x.Max == y.Max
			return 0;
		}

		private class GapComparer : IComparer<Interval>
		{
			public int Compare(Interval x, Interval y)
			{
				return CompareGaps(x, y);
			}
		}
	}
}

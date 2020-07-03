using System;
using System.Collections.Generic;

namespace ProSuite.Commons.Collections
{
	/// <summary>
	/// A string comparer that compares runs of non-digits lexicographically,
	/// but runs of digits numerically, i.e., runs of digits are interpreted
	/// as the integer numbers they represent.
	/// <para/>
	/// For example, this comparer considers "x2" less than "x11".
	/// </summary>
	// Donated.
	public class NumericStringComparer : IComparer<string>
	{
		private readonly StringComparison _comparison;

		public NumericStringComparer(
			StringComparison comparison = StringComparison.OrdinalIgnoreCase)
		{
			_comparison = comparison;
		}

		public int Compare(string x, string y)
		{
			// By .NET conventions, null sorts before anything,
			// even before the empty string:
			if (x == null && y == null)
				return 0; // both null
			if (x == null)
				return -1;
			if (y == null)
				return +1;

			if (x.Length == 0 && y.Length == 0)
				return 0; // both empty
			if (x.Length == 0)
				return -1;
			if (y.Length == 0)
				return +1;

			int ix = 0, iy = 0;
			while (ix < x.Length && iy < y.Length)
			{
				double? xx, yy;
				int nx = ScanRange(x, ix, out xx);
				int ny = ScanRange(y, iy, out yy);

				if (xx.HasValue && yy.HasValue) // both numeric
				{
					if (xx < yy)
						return -1;
					if (xx > yy)
						return +1;
					// Difference in leading zeros?
					if (nx < ny)
						return -1;
					if (nx > ny)
						return +1;
					// Truly the same numbers
					ix += nx;
					iy += ny;
					continue;
				}

				if (xx.HasValue) // x numeric, y text
				{
					return -1;
				}

				if (yy.HasValue) // x text, y numeric
				{
					return +1;
				}

				// x and y non-numeric
				int len = Math.Min(nx, ny);
				int r = string.Compare(x, ix, y, iy, len, _comparison);
				if (r != 0)
					return r;
				ix += nx;
				iy += ny;
			}

			if (ix < x.Length)
				return -1;
			if (iy < y.Length)
				return +1;
			return 0;
		}

		private static int ScanRange(string text, int index, out double? value)
		{
			int anchor = index;

			if (char.IsDigit(text, index))
			{
				value = 0;
				while (index < text.Length && char.IsDigit(text, index))
				{
					value *= 10;
					value += char.GetNumericValue(text, index);
					index += 1;
				}
			}
			else
			{
				while (index < text.Length && ! char.IsDigit(text, index))
					++index;
				value = null;
			}

			return index - anchor;
		}
	}
}
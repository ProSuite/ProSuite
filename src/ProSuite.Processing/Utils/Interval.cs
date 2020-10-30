using System;
using System.Diagnostics.Contracts;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// An immutable closed (Min and Max are included) interval.
	/// </summary>
	public readonly struct Interval
	{
		public readonly double Min;
		public readonly double Max;

		private Interval(double min, double max)
		{
			// Allow min > max, this denotes the empty interval
			Min = min;
			Max = max;
		}

		/// <remarks>
		/// This is a *closed* interval: when Min == Max
		/// then it contains this one point and is not empty!
		/// </remarks>
		public bool IsEmpty => Min > Max;

		[Pure]
		public bool Contains(double value)
		{
			return Min <= value && value <= Max;
		}

		[Pure]
		public bool Contains(Interval other)
		{
			return Min <= other.Min && other.Max <= Max;
		}

		[Pure]
		public bool Overlaps(Interval other)
		{
			return other.Min <= Max && other.Max >= Min;
		}

		public override string ToString()
		{
			if (IsEmpty) return "[]";
			return Math.Abs(Min - Max) < double.Epsilon
				       ? $"[{Min}]"
				       : $"[{Min}, {Max}]";
		}

		public static Interval Create(double value)
		{
			return new Interval(value, value);
		}

		public static Interval Create(double min, double max)
		{
			return new Interval(Math.Min(min, max), Math.Max(min, max));
		}

		public static Interval Empty => new Interval(1, 0); // min > max
	}
}

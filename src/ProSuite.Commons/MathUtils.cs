using System;

namespace ProSuite.Commons
{
	public static class MathUtils
	{
		private const double _significantFloatDigits = 1E-7;
		private const double _significantDoubleDigits = 1E-14;

		public static double RoundToSignificantDigits(double value, int digits)
		{
			if (AreEqual(value, 0.0))
			{
				return 0.0;
			}

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1);

			return scale * Math.Round(value / scale, digits);
		}

		public static double ToDegrees(double radians)
		{
			return radians * 180.0 / Math.PI;
		}

		public static double ToRadians(double degrees)
		{
			return degrees * Math.PI / 180;
		}

		public static bool AreEqual(double v1, double v2,
		                            double tolerance = double.Epsilon)
		{
			return Math.Abs(v1 - v2) <= tolerance;
		}

		public static bool AreEqual(float v1, float v2, double tolerance = float.Epsilon)
		{
			return Math.Abs(v1 - v2) <= tolerance;
		}

		public static bool AreSignificantDigitsEqual(float v1, float v2)
		{
			double epsilon = Math.Max(Math.Abs(v1), Math.Abs(v2)) *
			                 _significantFloatDigits;

			return AreEqual(v1, v2, epsilon);
		}

		public static double GetDoubleSignificanceEpsilon(double value)
		{
			return Math.Abs(value) * _significantDoubleDigits;
		}

		public static double GetDoubleSignificanceEpsilon(double v0, double v1)
		{
			return GetDoubleSignificanceEpsilon(Math.Max(Math.Abs(v0), Math.Abs(v1)));
		}

		public static double GetDoubleSignificanceEpsilon(params double[] values)
		{
			double max = 0;

			// ReSharper disable once ForCanBeConvertedToForeach
			for (var index = 0; index < values.Length; index++)
			{
				max = Math.Max(max, Math.Abs(values[index]));
			}

			return GetDoubleSignificanceEpsilon(max);
		}

		public static float GetFloatSignificanceEpsilon(float value)
		{
			return (float) (Math.Abs(value) * _significantFloatDigits);
		}

		public static bool AreSignificantDigitsEqual(double v1, double v2)
		{
			return AreDigitsEqual(v1, v2, _significantDoubleDigits);
		}

		/// <summary>
		/// Determines if two double values are equal for a given value of significant digits
		/// </summary>
		/// <param name="v1">The first value to compare</param>
		/// <param name="v2">The second value to compare</param>
		/// <param name="fractionalDigits">Significant digits (e.g. 1E-7 to indicate 7 significant digits)</param>
		/// <returns></returns>
		public static bool AreDigitsEqual(double v1, double v2, double fractionalDigits)
		{
			double epsilon = Math.Max(Math.Abs(v1), Math.Abs(v2)) * fractionalDigits;

			return AreEqual(v1, v2, epsilon);
		}

		public static bool AreSignificantDigitsEqual(float v1, double v2)
		{
			double v1Double = Convert.ToDouble(v1);

			double epsilon = Math.Max(Math.Abs(v1Double), Math.Abs(v2)) *
			                 _significantFloatDigits;

			return AreEqual(v1Double, v2, epsilon);
		}

		public static bool AreSignificantDigitsEqual(double v1, float v2)
		{
			return AreSignificantDigitsEqual(v2, v1);
		}

		// see remark below 
		public static bool AreSignificantDigitsEqual(double v1, int v2)
		{
			return AreSignificantDigitsEqual(v1, Convert.ToDouble(v2));
		}

		// remark: if this method would not be defined, 
		// calling AreSignificantDigitsEqual(0, doubleValue) 
		// would use AreSignificantDigitsEqual(float, double) and a value of 1e-10 would be considered equal to 0
		public static bool AreSignificantDigitsEqual(int v1, double v2)
		{
			return AreSignificantDigitsEqual(Convert.ToDouble(v1), v2);
		}

		/// <summary>
		/// Determines whether a value is smaller than or equal to tolerance, 
		/// applying an epsilon value to ignore non-significant digits.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="tolerance">The tolerance.</param>
		/// <param name="epsilon">The epsilon.</param>
		/// <returns>
		///   <c>true</c> if the specified value is within the tolerance; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsWithinTolerance(double value, double tolerance,
		                                     double epsilon)
		{
			// value is either smaller than the tolerance, or equal to it
			// (applying an epsilon to ignore insignificant digits)
			return value < tolerance || Math.Abs(value - tolerance) <= epsilon;
		}

		/// <summary>
		/// Determines the 'correct' (i.e. without double-floating point issues) modulo with the
		/// option to enforce the result to be positive despite negative inputs.
		/// </summary>
		/// <param name="dividend"></param>
		/// <param name="divisor"></param>
		/// <param name="enforcePositive"></param>
		/// <returns></returns>
		public static double Modulo(double dividend, double divisor, bool enforcePositive = false)
		{
			// modulo on doubles can be 'wrong':
			// https://stackoverflow.com/questions/906564/why-is-modulus-operator-not-working-for-double-in-c

			decimal remainder = (decimal) dividend % (decimal) divisor;

			if (enforcePositive && remainder < 0)
			{
				remainder += (decimal) divisor;
			}

			return (double) remainder;
		}
	}
}

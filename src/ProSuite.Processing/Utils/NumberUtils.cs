using System;
using System.Diagnostics;
using System.Globalization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// A container for static utility methods for running carto processes.
	/// </summary>
	public static class NumberUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static double PointsToMillimeters(double points)
		{
			return points / Constants.PointsPerMillimeter;
		}

		public static double MillimetersToPoints(double millimeters)
		{
			return millimeters * Constants.PointsPerMillimeter;
		}

		public static double Clamp(this double value, double min, double max, string name = null)
		{
			if (value < min)
			{
				if (! string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (! string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, max);
				}

				return max;
			}

			return value;
		}

		public static int Clamp(this int value, int min, int max, string name = null)
		{
			Debug.Assert(min < max);

			if (value < min)
			{
				if (! string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (! string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, max);
				}

				return max;
			}

			return value;
		}

		/// <summary>
		/// Normalize the given <paramref name="angle"/> (in degrees)
		/// so that it is in the range 0 (inclusive) to 360 (exclusive).
		/// </summary>
		/// <param name="angle">in degrees</param>
		/// <returns>angle, in degrees, normalized to 0..360</returns>
		public static double ToPositiveDegrees(double angle)
		{
			angle %= 360;

			if (angle < 0)
			{
				angle += 360;
			}

			return angle;
		}

		/// <summary>
		/// Normalize the given <paramref name="angle"/> (in radians)
		/// so that it is in the range -pi to pi (both inclusive).
		/// </summary>
		/// <param name="angle">in radians</param>
		/// <returns>angle, in radians, normalized to -pi..pi</returns>
		public static double NormalizeRadians(double angle)
		{
			const double twoPi = Math.PI * 2;

			angle %= twoPi; // -2pi .. 2pi

			if (angle > Math.PI)
			{
				angle -= twoPi;
			}
			else if (angle < -Math.PI)
			{
				angle += twoPi;
			}

			return angle; // -pi .. pi
		}

		/// <remarks>A number is finite if it is not NaN and not infinity</remarks>
		public static bool IsFinite(this double number)
		{
			return ! double.IsNaN(number) && ! double.IsInfinity(number);
		}

		[NotNull]
		public static int[] ParseIntegerList([CanBeNull] string text, char separator)
		{
			if (text == null)
			{
				return _emptyIntArray;
			}

			text = text.Trim();
			if (text.Length < 1)
			{
				return _emptyIntArray;
			}

			string[] parts = text.Split(separator);

			if (parts.Length < 1)
			{
				return _emptyIntArray;
			}

			var result = new int[parts.Length];

			const NumberStyles numberStyle = NumberStyles.Integer;
			CultureInfo invariant = CultureInfo.InvariantCulture;

			for (var index = 0; index < parts.Length; index++)
			{
				string part = parts[index];
				result[index] = int.Parse(part, numberStyle, invariant);
			}

			return result;
		}

		[NotNull]
		public static double[] ParseDoubleList([CanBeNull] string text, char separator)
		{
			if (text == null)
			{
				return _emptyDoubleArray;
			}

			text = text.Trim();
			if (text.Length < 1)
			{
				return _emptyDoubleArray;
			}

			string[] parts = text.Split(separator);

			if (parts.Length < 1)
			{
				return _emptyDoubleArray;
			}

			var result = new double[parts.Length];

			const NumberStyles numberStyle = NumberStyles.Float;
			CultureInfo invariant = CultureInfo.InvariantCulture;

			for (var index = 0; index < parts.Length; index++)
			{
				string part = parts[index];
				result[index] = double.Parse(part, numberStyle, invariant);
			}

			return result;
		}

		private static readonly int[] _emptyIntArray = Array.Empty<int>();
		private static readonly double[] _emptyDoubleArray = Array.Empty<double>();
	}
}

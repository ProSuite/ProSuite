using System;
using System.Diagnostics;
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
	}
}
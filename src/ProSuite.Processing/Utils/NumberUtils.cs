using System;
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

		public static double Clip(double value, double min, double max,
		                          [CanBeNull] string parameter = null)
		{
			if (value < min)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, max);
				}

				return max;
			}

			return value;
		}

		public static int Clip(int value, int min, int max,
		                       [CanBeNull] string parameter = null)
		{
			if (value < min)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, max);
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
	}
}

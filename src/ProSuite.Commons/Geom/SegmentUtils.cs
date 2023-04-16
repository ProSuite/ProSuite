using ProSuite.Commons.Essentials.CodeAnnotations;
using System;

namespace ProSuite.Commons.Geom
{
	public static class SegmentUtils
	{
		public static double GetAlongFraction([NotNull] Pnt nearPoint,
		                                        [NotNull] Pnt segmentLine)
		{
			return nearPoint * segmentLine / segmentLine.OrigDist2();
		}

		public static double GetOffset([NotNull] Pnt nearPoint, [NotNull] Pnt segmentLine)
		{
			double segmentLength = Math.Sqrt(segmentLine.OrigDist2());
			return nearPoint.VectorProduct(segmentLine) / segmentLength;
		}


		public static bool CutLineCircle([NotNull] Pnt p0,
		                                   [NotNull] Pnt l0,
		                                   [NotNull] Pnt center,
		                                   double r2,
		                                   out double tMin,
		                                   out double tMax)
		{
			Pnt p = p0 - center;

			double a = l0 * l0; // a = square length of l0
			double b = 2 * (p * l0);
			double c = p * p - r2;

			if (a > 0)
			{
				if (SolveSqr(a, b, c, out tMin, out tMax))
				{
					if (tMin > tMax)
					{
						throw new InvalidProgramException(
							string.Format(
								"error in software design assumption: {0} <= {1} is false!",
								tMin, tMax));
					}

					return true;
				}

				return false;
			}

			if (a == 0)
			{
				if (c > 0)
				{
					tMin = double.MaxValue;
					tMax = double.MaxValue;

					return false;
				}

				tMin = double.MinValue;
				tMax = double.MaxValue;

				return true;
			}

			// (a < 0)
			throw new InvalidOperationException(
				string.Format("sqare length of line = {0}", a));
		}

		/// <summary>
		/// Lösung der Quadratischen Gleichung: a * x^2 + b * x + c = 0
		/// r1, r2 = (-b -+ ((b^2 - 4ac)^0.5))/2a<br/>
		/// </summary>
		public static bool SolveSqr(double a, double b, double c,
		                            out double x0, out double x1)
		{
			// Lösung der quadratischen Gleichung:
			//r1, r2 = (-b -+ ((b^2 - 4ac)^0.5))/2a

			double dDet = b * b - 4.0 * a * c;

			if (dDet >= 0)
			{
				dDet = Math.Sqrt(dDet);
				x0 = (-b - dDet) / (2.0 * a);
				x1 = (-b + dDet) / (2.0 * a);

				return true;
			}

			x0 = dDet;
			x1 = 0;
			return false;
		}

	}
}

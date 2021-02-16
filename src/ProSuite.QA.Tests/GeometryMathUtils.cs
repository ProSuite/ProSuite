using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public static class GeometryMathUtils
	{
		public static double CalculateSmoothness([NotNull] ISegment segment0,
		                                         [NotNull] ISegment segment1,
		                                         [NotNull] ISegment segment2)
		{
			double z1;
			double z2;
			double z3;
			double z4;
			((ISegmentZ) segment0).GetZs(out z1, out z2);
			((ISegmentZ) segment2).GetZs(out z3, out z4);

			double length1 = segment0.Length;
			double length2 = segment1.Length;
			double length3 = segment2.Length;

			return CalculateAngleSecondDerivative(length1, length2, length3, z1, z2, z3, z4);
		}

		public static double CalculateMaxCurvature([NotNull] ISegment segment0,
		                                           [NotNull] ISegment segment1,
		                                           [NotNull] ISegment segment2)
		{
			double z1;
			double z2;
			double z3;
			double z4;
			((ISegmentZ) segment0).GetZs(out z1, out z2);
			((ISegmentZ) segment2).GetZs(out z3, out z4);

			double length1 = segment0.Length;
			double length2 = segment1.Length;
			double length3 = segment2.Length;

			double maxCurvature = CalculateMaximumCurvature(length1, length2, length3,
			                                                z1, z2, z3, z4);
			return maxCurvature;
		}

		public static double CalculateAngleSecondDerivative(double length1,
		                                                    double length2,
		                                                    double length3,
		                                                    double z1,
		                                                    double z2,
		                                                    double z3,
		                                                    double z4)
		{
			// TODO revise...
			return 1 / (length2 * length2) *
			       (Math.Atan2(z2 - z1, length1) - 2 * Math.Atan2(z3 - z2, length2) +
			        Math.Atan2(z4 - z3, length3));
		}

		public static double CalculateMaximumCurvature(double length1,
		                                               double length2,
		                                               double length3,
		                                               double z1,
		                                               double z2,
		                                               double z3,
		                                               double z4)
		{
			// get function of 3rd grad z = a x^3 + b x^2 + c x + d
			// z(0) = z1
			// z(length1) = z2
			// z(length1 + length2) = z3
			// z(lenght1 + length2 + length3) = z4

			double d = z1;

			double x2 = length1;
			double x3 = x2 + length2;
			double x4 = x3 + length3;

			var a2 = new[] {x2, x3, x4};
			var a1 = new[] {a2[0] * x2, a2[1] * x3, a2[2] * x4};
			var a0 = new[] {a1[0] * x2, a1[1] * x3, a1[2] * x4};
			var y = new[] {z2 - d, z3 - d, z4 - d};

			double det = Determinante(new[] {a0, a1, a2});
			double a = Determinante(new[] {y, a1, a2}) / det;
			double b = Determinante(new[] {a0, y, a2}) / det;
			double c = Determinante(new[] {a0, a1, y}) / det;

			//double u = 0;
			//double t = (((a * u) + b) * u + c) * u  + d;

			// Curvature :
			// (x'y'' - y'x'') / (x'^2 + y'^2)^(3/2)
			// -> (6 a x + 2 b) / (1 + (3 a x^2 + 2 b x + c)^2)^(3/2) 
			// Curvature extremum leads to:
			// 3 a x^2 + 2 b x + c + 1 = 0

			double maxCurvature;
			if (a == 0 && b == 0)
			{
				maxCurvature = 0;
			}
			else
			{
				double max = Math.Abs(GetCurvature(a, b, c, 0));
				max = Math.Max(max, Math.Abs(GetCurvature(a, b, c, x4)));
				if (a == 0)
				{
					double xMax = (c + 1) / (2 * b);
					if (xMax >= 0 && xMax <= x4)
					{
						max = Math.Max(max, Math.Abs(GetCurvature(a, b, c, xMax)));
					}
				}
				else
				{
					double xMax0, xMax1;
					bool isReal = SolveSqr(3 * a, 2 * b, c + 1, out xMax0, out xMax1);
					if (isReal)
					{
						if (xMax0 >= 0 && xMax0 <= x4)
						{
							max = Math.Max(max, Math.Abs(GetCurvature(a, b, c, xMax0)));
						}

						if (xMax1 >= 0 && xMax1 <= x4)
						{
							max = Math.Max(max, Math.Abs(GetCurvature(a, b, c, xMax1)));
						}
					}
				}

				maxCurvature = max;
			}

			return maxCurvature;
		}

		private static double GetCurvature(double a, double b, double c, double x)
		{
			double deri = 3 * x * x + 2 * b * x + c;
			double curvature = (6 * a * x + 2 * b) /
			                   Math.Pow(deri * deri + 1, 3.0 / 2.0);
			return curvature;
		}

		private static double Determinante(double[][] matrix3x3)
		{
			double d = matrix3x3[0][0] *
			           (matrix3x3[1][1] * matrix3x3[2][2] - matrix3x3[1][2] * matrix3x3[2][1])
			           + matrix3x3[0][1] *
			           (matrix3x3[1][2] * matrix3x3[2][0] - matrix3x3[1][0] * matrix3x3[2][2])
			           + matrix3x3[0][2] *
			           (matrix3x3[1][0] * matrix3x3[2][1] - matrix3x3[1][1] * matrix3x3[2][0]);
			return d;
		}

		/// <summary>
		/// Lösung der Quadratischen Gleichung: a * x^2 + b * x + c = 0
		/// r1, r2 = (-b -+ ((b^2 - 4ac)^0.5))/2a<br/>
		/// </summary>
		private static bool SolveSqr(double a, double b, double c,
		                             out double x0, out double x1)
		{
			// Lösung der quadratischen Gleichung:
			//r1, r2 = (-b -+ ((b^2 - 4ac)^0.5))/2a

			double dDet = (b * b) - 4.0 * a * c;

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

		public static double GetDistanceSquared([NotNull] IPoint p0,
		                                        [NotNull] IPoint p1,
		                                        bool get3DDistance)
		{
			double x0;
			double y0;
			p0.QueryCoords(out x0, out y0);

			double x1;
			double y1;
			p1.QueryCoords(out x1, out y1);

			double d = x0 - x1;
			double d2 = d * d;

			d = y0 - y1;
			d2 += d * d;

			if (get3DDistance)
			{
				double z0 = p0.Z;
				double z1 = p1.Z;

				d = z0 - z1;

				if (! double.IsNaN(d))
				{
					d2 += d * d;
				}
			}

			return d2;
		}

		public static double CalculateSlope([NotNull] ISegment segment)
		{
			double length = ((ICurve) segment).Length;

			double fromZ;
			double toZ;
			((ISegmentZ) segment).GetZs(out fromZ, out toZ);

			return CalculateSlope(fromZ, toZ, length);
		}

		public static double CalculateSlope(double fromZ, double toZ, double length)
		{
			double dZ = fromZ - toZ;

			return Math.Abs(Math.Atan2(dZ, length));
		}
	}
}


using System;

namespace ProSuite.Commons.Geom
{
	public class LineHullPart : IHullPart
	{
		private readonly Pnt _p0;
		private readonly Pnt _p1;

		private readonly Lin2D _lin;

		public LineHullPart(Pnt p0, Pnt p1)
		{
			_p0 = p0;
			_p1 = p1;

			_lin = new Lin2D(p0, p1);
		}

		public CutPart CutPart { get; set; }

		public bool Cut(HullLineSimple line, ref double tMin, ref double tMax)
		{
			return CutLin(_lin, line.Lin, ref tMin, ref tMax);
		}

		public bool Cut(HullLineArc line, ref double tMin, ref double tMax)
		{
			double angS = line.StartDirection;
			double r = line.Radius;

			// Handle start point of arc
			var aS = new Pnt2D(r * Math.Cos(angS), r * Math.Sin(angS));
			Lin2D s = line.Lin.GetParallel(aS);
			bool intersects = CutLin(_lin, s, ref tMin, ref tMax);

			// Handle end point of arc
			double angE = angS + line.Angle;
			var aE = new Pnt2D(r * Math.Cos(angE), r * Math.Sin(angE));
			Lin2D e = line.Lin.GetParallel(aE);
			intersects |= CutLin(_lin, e, ref tMin, ref tMax);

			// Handle this start/end point
			intersects |= CutArcLine(_lin.Ps, line, ref tMin, ref tMax);
			intersects |= CutArcLine(_lin.Pe, line, ref tMin, ref tMax);

			// handle sides
			double arcAngle = GetNormedAngle(line.Angle);
			Pnt normal = _lin.LNormal;
			double normalDir = _lin.DirectionAngle + Math.PI / 2;
			// handle left side
			if (GetNormedAngle(normalDir - line.StartDirection) < arcAngle)
			{
				intersects |= CutLin(_lin.GetParallel(r * normal), line.Lin, ref tMin, ref tMax);
			}

			// handle right side
			if (GetNormedAngle(normalDir + Math.PI - line.StartDirection) < arcAngle)
			{
				intersects |= CutLin(_lin.GetParallel(-r * normal), line.Lin, ref tMin, ref tMax);
			}

			return intersects;
		}

		private static bool CutArcLine(Pnt p, HullLineArc line, ref double tMin, ref double tMax)
		{
			bool intersects = false;
			double r = line.Radius;
			double tCircleMin, tCircleMax;
			if (SegmentUtils.CutLineCircle(line.Lin.Ps, line.Lin.L, p, r * r,
										   out tCircleMin, out tCircleMax))
			{
				intersects |= IsPointInArcDirection(p, line, tCircleMin, ref tMin, ref tMax);
				intersects |= IsPointInArcDirection(p, line, tCircleMax, ref tMin, ref tMax);
			}

			return intersects;
		}

		private static bool IsPointInArcDirection(Pnt p, HullLineArc line, double lineAt,
												  ref double tMin, ref double tMax)
		{
			Pnt pointAt = line.Lin.Ps + lineAt * line.Lin.L;
			Pnt dirToP = p - pointAt;
			double minAngle = GetNormedAngle(Math.Atan2(dirToP.Y, dirToP.X) - line.StartDirection);
			double arcAngle = GetNormedAngle(line.Angle);
			if (minAngle >= arcAngle)
			{
				return false;
			}

			tMin = Math.Min(tMin, lineAt);
			tMax = Math.Max(tMax, lineAt);
			return true;
		}

		private static double GetNormedAngle(double angle)
		{
			while (angle < 0)
				angle += 2 * Math.PI;
			while (angle > 2 * Math.PI)
				angle -= 2 * Math.PI;
			return angle;
		}

		public bool Cut(HullLineLine line, ref double tMin, ref double tMax)
		{
			Lin2D s = line.Lin.GetParallel(line.EndPart.Ps);
			bool intersects = CutLin(_lin, s, ref tMin, ref tMax);

			Lin2D e = line.Lin.GetParallel(line.EndPart.Pe);
			intersects |= CutLin(_lin, e, ref tMin, ref tMax);

			var thisAtStart = new Lin2D(_lin.Ps, _lin.Ps + line.EndPart.L);
			intersects |= CutLin(thisAtStart, e, ref tMin, ref tMax);

			var thisAtEnd = new Lin2D(_lin.Pe, _lin.Pe + line.EndPart.L);
			intersects |= CutLin(thisAtEnd, e, ref tMin, ref tMax);

			return intersects;
		}

		public override string ToString()
		{
			return _lin.ToString();
		}

		internal static bool CutLin(Lin2D lin, Lin2D other, ref double tMin, ref double tMax)
		{
			var pair = new LinPair2D(lin, other);
			double fThis;
			if (pair.CutLineLine(out fThis))
			{
				if (fThis < 0 || fThis > 1)
				{
					return false;
				}

				Pnt cut = lin.Ps + fThis * lin.L;
				Pnt lOther = cut - other.Ps;
				double fLine = lOther * other.L / other.L2;

				tMin = Math.Min(tMin, fLine);
				tMax = Math.Max(tMax, fLine);
			}
			else
			{
				// Parallel
				if (fThis != 0)
				{
					return false;
				}

				Pnt lThisS = lin.Ps - other.Ps;
				double fLine = lThisS * other.L / other.L2;

				tMin = Math.Min(tMin, fLine);
				tMax = Math.Max(tMax, fLine);

				Pnt lThisE = lin.Pe - other.Ps;
				fLine = lThisE * other.L / other.L2;

				tMin = Math.Min(tMin, fLine);
				tMax = Math.Max(tMax, fLine);
			}

			return true;
		}

		public void Cut(Pnt segStart, Pnt segLine, double offset, ref double tMin,
						ref double tMax)
		{
			Pnt near0 = _p0 - segStart;
			double offset0 = SegmentUtils.GetOffset(near0, segLine);
			double along0 = SegmentUtils.GetAlongFraction(near0, segLine);

			Pnt near1 = _p1 - segStart;
			double offset1 = SegmentUtils.GetOffset(near1, segLine);
			double along1 = SegmentUtils.GetAlongFraction(near1, segLine);

			if (Math.Abs(offset0) <= offset)
			{
				double t = along0;
				tMin = Math.Min(t, tMin);
				tMax = Math.Max(t, tMax);
			}

			if (Math.Abs(offset1) <= offset)
			{
				double t = along1;
				tMin = Math.Min(t, tMin);
				tMax = Math.Max(t, tMax);
			}

			if ((offset0 < offset) == (offset1 > offset))
			{
				double f0 = offset0 - offset;
				double f1 = offset1 - offset;
				double sumF = Math.Abs(f0) + Math.Abs(f1);
				if (sumF > 0)
				{
					double t = along0 + Math.Abs(f0) / sumF * (along1 - along0);
					tMin = Math.Min(t, tMin);
					tMax = Math.Max(t, tMax);
				}
			}

			if (offset > 0 && (offset0 < -offset) == (offset1 > -offset))
			{
				double f0 = offset0 + offset;
				double f1 = offset1 + offset;
				double sumF = Math.Abs(f0) + Math.Abs(f1);
				if (sumF > 0)
				{
					double t = along0 + Math.Abs(f0) / sumF * (along1 - along0);
					tMin = Math.Min(t, tMin);
					tMax = Math.Max(t, tMax);
				}
			}
		}
	}
}

using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public class LinPair2D : LinPair
	{
		public LinPair2D([NotNull] Lin2D l0, [NotNull] Lin2D l1)
			: base(l0, l1) { }

		public new Lin2D L0 => (Lin2D) base.L0;
		public new Lin2D L1 => (Lin2D) base.L1;

		private double? _t1;

		public double T1
		{
			get { return _t1 ?? (_t1 = L0.L.X * L1.L.Y - L0.L.Y * L1.L.X).Value; }
		}

		private double? _segmentDistance;

		public double SegmentDistance
		{
			get { return _segmentDistance ?? (_segmentDistance = GetSegmentDistance()).Value; }
		}

		protected override bool GetIsParallel()
		{
			bool isParallel = T1 == 0;
			return isParallel;
		}

		public bool CutLineLine(out double f0)
		{
			if (IsParallel)
			{
				f0 = SegmentDistance;
				return false;
			}

			f0 = (L1.L.Y * (L1.Ps.X - L0.Ps.X) - L1.L.X * (L1.Ps.Y - L0.Ps.Y)) / T1;
			return true;
		}

		protected double GetSegmentDistance()
		{
			double l02 = L0.L.OrigDist2();
			if (IsParallel)
			{
				Pnt s = L1.Ps - L0.Ps;
				double f0 = s * L0.L / l02;

				if (f0 >= 0 && f0 <= 1)
				{
					return Math.Abs(L0.L.VectorProduct(s) / Math.Sqrt(l02));
				}

				Pnt e = L1.Pe - L0.Ps;
				double f1 = e * L0.L / l02;
				if ((f1 >= 0 && f1 <= 1) || (f0 < 0 != f1 < 0))
				{
					return Math.Abs(L0.L.VectorProduct(s) / Math.Sqrt(l02));
				}

				double d2 = Math.Min(s.OrigDist2(), e.OrigDist2());
				d2 = Math.Min(d2, (L1.Ps - L0.Pe).OrigDist2());
				d2 = Math.Min(d2, (L1.Pe - L0.Pe).OrigDist2());

				return Math.Sqrt(d2);
			}

			{
				Pnt s = L1.Ps - L0.Ps;
				double f1S = L0.L.VectorProduct(s);
				Pnt e = L1.Pe - L0.Ps;
				double f1E = L0.L.VectorProduct(e);
				double f1 = -f1S / (f1E - f1S);
				Pnt cut = L1.Ps + f1 * L1.L;

				double f0 = L0.L * (cut - L0.Ps) / l02;
				if (f0 >= 0 && f0 <= 1 && f1 >= 0 && f1 <= 1)
				{
					return 0;
				}

				double d2 = Math.Min(GetPointDistance(L0.L, l02, s, f1S),
				                     GetPointDistance(L0.L, l02, e, f1E));
				d2 = Math.Min(d2, GetPointDistance(L1.L, L1.L2, L0.Ps - L1.Ps, null));
				d2 = Math.Min(d2, GetPointDistance(L1.L, L1.L2, L0.Pe - L1.Ps, null));

				return Math.Sqrt(d2);
			}
		}

		private static double GetPointDistance([NotNull] Pnt line,
		                                       double line2,
		                                       [NotNull] Pnt p,
		                                       double? vectorProd)
		{
			double d2;
			double f = line * p / line2;
			if (f >= 0 && f <= 1)
			{
				double v = vectorProd ?? line.VectorProduct(p);
				d2 = v * v / line2;
			}
			else
			{
				d2 = Math.Min(p.OrigDist2(), (p - line).OrigDist2());
			}

			return d2;
		}
	}
}

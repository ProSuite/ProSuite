using ProSuite.Commons.Essentials.CodeAnnotations;
using System;

namespace ProSuite.Commons.Geom
{
	public class Lin2D : Lin
	{
		public Lin2D([NotNull] Pnt ps, [NotNull] Pnt pe)
			: base(ps, pe) { }

		public Lin2D GetParallel(Pnt offset)
		{
			var parallel = new Lin2D(Ps + offset, Pe + offset);
			parallel._l2 = _l2;
			parallel._lNormal = _lNormal;
			parallel._dir = _dir;

			return parallel;
		}

		public override string ToString()
		{
			return $"{Ps.X:N2} {Ps.Y:N2}, {Pe.X:N2}, {Pe.Y:N2}";
		}

		private double? _l2;

		public double L2
		{
			get { return _l2 ?? (_l2 = L.X * L.X + L.Y * L.Y).Value; }
		}

		private Pnt _lNormal;

		/// <summary>
		/// perpendicular unit vector to L
		/// </summary>
		public Pnt LNormal
		{
			get
			{
				if (_lNormal == null)
				{
					double x = -L.Y;
					double y = L.X;
					double length = Math.Sqrt(L2);
					_lNormal = new Pnt2D(x / length, y / length);
				}

				return _lNormal;
			}
		}

		private double? _dir;

		public double DirectionAngle
		{
			get { return _dir ?? (_dir = Math.Atan2(L.Y, L.X)).Value; }
		}
	}
}

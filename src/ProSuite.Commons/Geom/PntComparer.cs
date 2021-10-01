using System.Collections.Generic;

namespace ProSuite.Commons.Geom
{
	public class PntComparer<T> : IComparer<T> where T : IPnt
	{
		private readonly bool _includeZ;

		public PntComparer(bool includeZ = false)
		{
			_includeZ = includeZ;
		}

		public int Compare(T a, T b)
		{
			// dont't use the tolerance to make sure the sorting is correct also for small distances
			if (a.X < b.X)
			{
				return -1;
			}

			if (a.X > b.X)
			{
				return +1;
			}

			// a.X == b.X

			if (a.Y < b.Y)
			{
				return -1;
			}

			if (a.Y > b.Y)
			{
				return +1;
			}

			// a.Y == b.Y

			if (_includeZ && a is Pnt3D aPnt3D && b is Pnt3D bPnt3D)
			{
				if (aPnt3D.Z < bPnt3D.Z)
				{
					return -1;
				}

				if (aPnt3D.Z > bPnt3D.Z)
				{
					return 1;
				}

				if (aPnt3D.Z == bPnt3D.Z)
				{
					return 0;
				}

				// NOTE: imporant to use the same z-logic as in GeometryUtils.IsSamePoint regarding NaN:
				if (double.IsNaN(aPnt3D.Z) && double.IsNaN(bPnt3D.Z))
				{
					return 0;
				}

				if (double.IsNaN(aPnt3D.Z) && ! double.IsNaN(bPnt3D.Z))
				{
					return -1;
				}

				if (! double.IsNaN(aPnt3D.Z) && double.IsNaN(bPnt3D.Z))
				{
					return 1;
				}
			}

			// a == b with 2D comparison
			return 0;
		}
	}
}

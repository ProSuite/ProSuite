using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	public sealed class Location3D
	{
		private readonly double _x;
		private readonly double _y;
		private readonly double _z;

		/// <summary>
		/// Initializes a new instance of the <see cref="Location3D"/> class.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <param name="z">The z.</param>
		public Location3D(double x, double y, double z)
		{
			_x = x;
			_y = y;
			_z = z;
		}

		public override string ToString()
		{
			return string.Format("X: {0}, Y: {1}, Z: {2}", _x, _y, _z);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = _x.GetHashCode();
				result = (result * 397) ^ _y.GetHashCode();
				result = (result * 397) ^ _z.GetHashCode();
				return result;
			}
		}

		public double GetSquaredDistanceTo([NotNull] Location3D other)
		{
			double dx = _x - other._x;
			double dy = _y - other._y;
			double dz = _z - other._z;

			return dx * dx + dy * dy + dz * dz;
		}
	}
}

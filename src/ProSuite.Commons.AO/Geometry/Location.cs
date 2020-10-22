using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	public sealed class Location
	{
		private readonly double _x;
		private readonly double _y;

		/// <summary>
		/// Initializes a new instance of the <see cref="Location"/> class.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		public Location(double x, double y)
		{
			_x = x;
			_y = y;
		}

		public override string ToString()
		{
			return string.Format("X: {0}, Y: {1}", _x, _y);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
			}
		}

		public double GetSquaredDistanceTo([NotNull] Location other)
		{
			double dx = _x - other._x;
			double dy = _y - other._y;

			return dx * dx + dy * dy;
		}
	}
}

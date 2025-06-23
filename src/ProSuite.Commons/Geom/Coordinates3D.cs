using System.Runtime.InteropServices;

namespace ProSuite.Commons.Geom
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Coordinates3D : ICoordinates
	{
		private readonly double _x;
		private readonly double _y;
		private readonly double _z;
		public Coordinates3D(double x, double y, double z)
		{
			_x = x;
			_y = y;
			_z = z;
		}
		public double X => _x;
		public double Y => _y;
		public double? Z => _z;
		public bool HasZ => true;
	}
}


using System.Runtime.InteropServices;

namespace ProSuite.Commons.Geom
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Coordinates2D : ICoordinates
	{
		public double X { get; }
		public double Y { get; }

		public Coordinates2D(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double? Z => null;
		public bool HasZ => false;
	}
}



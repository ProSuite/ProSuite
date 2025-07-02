using System.Runtime.InteropServices;

namespace ProSuite.Commons.Geom
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Coordinates3D : ICoordinates
	{
		private double _x;
		private double _y;
		private double _z;

		public Coordinates3D(double x, double y, double z)
		{
			_x = x;
			_y = y;
			_z = z;
		}

		#region ICoordinates Members

		public double X
		{
			get => _x;
			set => _x = value;
		}

		public double Y
		{
			get => _y;
			set => _y = value;
		}

		public double Z
		{
			get => _z;
			set => _z = value;
		}

		public int Dimension => 3;

		#endregion

		#region IBoundedXY members

		public double XMin => X;

		public double YMin => Y;

		public double XMax => X;

		public double YMax => Y;

		#endregion
	}
}

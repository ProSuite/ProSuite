using System;
using System.Runtime.InteropServices;

namespace ProSuite.Commons.Geom
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Coordinates2D : ICoordinates

	{
		private double _x;
		private double _y;

		public Coordinates2D(double x, double y)
		{
			_x = x;
			_y = y;
		}

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
			get => double.NaN;
			set => throw new NotSupportedException("Cannot set Z on 2D coordinates");
		}

		public int Dimension => 2;

		public static implicit operator Coordinates3D(Coordinates2D coord2D)
		{
			return new Coordinates3D(coord2D.X, coord2D.Y, 0);
		}

		#region IBoundedXY members

		public double XMin => X;

		public double YMin => Y;

		public double XMax => X;

		public double YMax => Y;

		#endregion
	}
}

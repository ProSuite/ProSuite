using System;
using System.Runtime.InteropServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Coordinates2D : IPnt, IBox

	{
		private double _x;
		private double _y;

		public Coordinates2D(double x, double y)
		{
			_x = x;
			_y = y;
		}

		#region IPnt Members

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

		/// <summary>
		/// returns coordinate of dimension index
		/// </summary>
		public double this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return _x;
					case 1:
						return _y;
					default:
						throw new ArgumentOutOfRangeException(
							nameof(index), "Index must be 0, 1, or 2 for X, Y, or Z respectively.");
				}
			}
			set
			{
				switch (index)
				{
					case 0:
						_x = value;
						break;
					case 1:
						_y = value;
						break;
					default:
						throw new ArgumentOutOfRangeException(
							nameof(index), "Index must be 0, 1, or 2 for X, Y, or Z respectively.");
				}
			}
		}

		public IPnt Clone()
		{
			return CloneCoordinates2D();
		}

		IBox IGmtry.Extent => new Box(new Pnt2D(X, Y), new Pnt2D(X, Y));

		public IGmtry Border => null;

		public bool Intersects(IBox box)
		{
			return box.Contains((IPnt) this);
		}

		public int Dimension => 2;

		#endregion

		#region IBox Members

		bool IBox.Contains(IPnt p)
		{
			return false;
		}

		bool IBox.Contains(IPnt p, int[] dimensions)
		{
			return false;
		}

		bool IBox.Contains(IBox box)
		{
			return false;
		}

		bool IBox.Contains(IBox box, int[] dimensions)
		{
			return false;
		}

		IPnt IBox.Max => this;

		IPnt IBox.Min => this;

		void IBox.Include(IBox box)
		{
			throw new InvalidOperationException("this is a Point instance");
		}

		IBox IBox.Clone()
		{
			return CloneCoordinates2D();
		}

		double IBox.GetMaxExtent()
		{
			return 0;
		}

		#endregion

		#region IBoundedXY members

		public double XMin => X;

		public double YMin => Y;

		public double XMax => X;

		public double YMax => Y;

		#endregion

		[NotNull]
		public Coordinates2D CloneCoordinates2D()
		{
			return new Coordinates2D(X, Y);
		}
	}
}

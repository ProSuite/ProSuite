using System.Diagnostics;

namespace ProSuite.Commons.Geom
{
	public sealed class Pnt2D : Pnt
	{
		[DebuggerStepThrough]
		public Pnt2D() : base(2) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Pnt2D"/> class.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public Pnt2D(double x, double y) : base(2)
		{
			Coordinates[0] = x;
			Coordinates[1] = y;
		}

		public override int Dimension => 2;

		public override Box Extent => new Box(this, this);

		public override bool Equals(object obj)
		{
			var cmpr = obj as Pnt2D;
			if (cmpr == null)
			{
				return false;
			}

			if (cmpr.Dimension != Dimension)
			{
				return false;
			}

			return Coordinates[0] == cmpr.X && Coordinates[1] == cmpr.Y;
		}

		public override int GetHashCode()
		{
			return Coordinates[0].GetHashCode() ^ Coordinates[1].GetHashCode();
		}

		public override string ToString()
		{
			return $"{X};{Y}";
		}

		public override Pnt ClonePnt()
		{
			return new Pnt2D(Coordinates[0], Coordinates[1]);
		}
	}
}

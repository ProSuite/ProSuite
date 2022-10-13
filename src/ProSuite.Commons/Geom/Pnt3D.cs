using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public sealed class Pnt3D : Pnt
	{
		public Pnt3D() : base(3) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Pnt3D"/> class.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <param name="z">The z.</param>
		public Pnt3D(double x, double y, double z) : base(3)
		{
			Coordinates[0] = x;
			Coordinates[1] = y;
			Coordinates[2] = z;
		}

		public Pnt3D(Vector v) : base(3)
		{
			Assert.AreEqual(3, v.Dimension, "Input vector must be 3D");

			Coordinates[0] = v[0];
			Coordinates[1] = v[1];
			Coordinates[2] = v[2];
		}

		public Pnt3D(IPnt pnt) : base(3)
		{
			Coordinates[0] = pnt[0];
			Coordinates[1] = pnt[1];
			Coordinates[2] = pnt.Dimension == 3 ? pnt[2] : double.NaN;
		}

		public double Z
		{
			get { return Coordinates[2]; }
			set { Coordinates[2] = value; }
		}

		public override int Dimension => 3;

		public override Box Extent => new Box(this, this);

		public override int GetHashCode()
		{
			return Coordinates.GetHashCode();
		}

		[NotNull]
		public Pnt3D VectorProduct(Pnt3D other)
		{
			return new Pnt3D(
				Y * other.Z - Z * other.Y,
				Z * other.X - X * other.Z,
				X * other.Y - Y * other.X
			);
		}

		public override string ToString()
		{
			return $"{X};{Y};{Z}";
		}

		public override Pnt ClonePnt()
		{
			return ClonePnt3D();
		}

		public Pnt3D ClonePnt3D()
		{
			return new Pnt3D(Coordinates[0], Coordinates[1], Coordinates[2]);
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			var cmpr = other as Pnt3D;
			if (cmpr?.Dimension != Dimension)
			{
				return false;
			}

			// Z could be NaN, calculate epsilon from X, Y only
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(cmpr.X, cmpr.Y);

			return Equals(cmpr, epsilon);
		}

		public bool Equals(Pnt3D other, double tolerance)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			if (other.Dimension != Dimension)
			{
				return false;
			}

			return MathUtils.AreEqual(Coordinates[0], other.X, tolerance) &&
			       MathUtils.AreEqual(Coordinates[1], other.Y, tolerance) &&
			       EqualOrBothNan(Coordinates[2], other.Z, tolerance);
		}

		public bool EqualsXY(IPnt other, double tolerance)
		{
			if (other == null)
			{
				return false;
			}

			return MathUtils.AreEqual(Coordinates[0], other.X, tolerance) &&
			       MathUtils.AreEqual(Coordinates[1], other.Y, tolerance);
		}

		public double GetDistance(Pnt3D otherPoint, bool inXY = false)
		{
			double distanceSquared = Dist2(otherPoint, inXY ? 2 : 3);

			return Math.Sqrt(distanceSquared);
		}

		public static Pnt3D operator +(Pnt3D p0, Pnt3D p1)
		{
			return (Pnt3D) ((Pnt) p0 + p1);
		}

		public static Pnt3D operator -(Pnt3D p0, Pnt3D p1)
		{
			return (Pnt3D) ((Pnt) p0 - p1);
		}

		public static Pnt3D operator *(Pnt3D p, double s)
		{
			return new Pnt3D(p.X * s, p.Y * s, p.Z * s);
		}

		public static Pnt3D operator /(Pnt3D p, double s)
		{
			return new Pnt3D(p.X / s, p.Y / s, p.Z / s);
		}

		private static bool EqualOrBothNan(double v1, double v2,
		                                   double tolerance = double.Epsilon)
		{
			if (double.IsNaN(v1) && double.IsNaN(v2))
			{
				return true;
			}

			return MathUtils.AreEqual(v1, v2, tolerance);
		}
	}
}

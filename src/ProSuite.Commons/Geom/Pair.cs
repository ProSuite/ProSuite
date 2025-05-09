using System;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A lightweight X,Y pair that can be used as a 2D point
	/// or vector, with a few common operations and operators.
	/// Angles in degrees (due to our Carto Processing origin).
	/// </summary>
	public readonly struct Pair
	{
		public readonly double X;
		public readonly double Y;

		public Pair(double x, double y)
		{
			X = x;
			Y = y;
		}

		public static Pair Null => new Pair(0, 0);

		public double Length => Math.Sqrt(X * X + Y * Y); // cf https://en.wikipedia.org/wiki/Pythagorean_addition#Implementation

		public double AngleDegrees => Math.Atan2(Y, X) * 180.0 / Math.PI;

		public Pair Normalized()
		{
			var invNorm = 1.0 / Length;
			return new Pair(X * invNorm, Y * invNorm);
		}

		public Pair Shifted(double dx, double dy)
		{
			return new Pair(X + dx, Y + dy);
		}

		public Pair Rotated(double angleDegrees)
		{
			// optimise 90/180/270/360 rotations? caller's duty?
			double rad = angleDegrees * Math.PI / 180.0;
			double cos = Math.Cos(rad);
			double sin = Math.Sin(rad);
			double x = X * cos - Y * sin;
			double y = X * sin + Y * cos;
			return new Pair(x, y);
		}

		public Pair Rotated(double angleDegrees, Pair pivot)
		{
			return Shifted(-pivot.X, -pivot.Y)
				.Rotated(angleDegrees)
				.Shifted(pivot.X, pivot.Y);
		}

		public static double Dot(Pair a, Pair b)
		{
			return a.X * b.X + a.Y * b.Y;
		}

		public static Pair Lerp(double t, Pair a, Pair b)
		{
			return (1 - t) * a + t * b;
		}

		public static double DistanceSquared(Pair a, Pair b)
		{
			double dx = b.X - a.X;
			double dy = b.Y - a.Y;
			return dx * dx + dy * dy;
		}

		/// <summary>
		/// Twice the signed area of the given triangle: positive
		/// if the points are counterclockwise, negative otherwise.
		/// </summary>
		/// <remarks>Use this for a quick left-of test</remarks>
		public static double Area2(Pair a, Pair b, Pair c)
		{
			// Compute the cross product (b-a)x(p-a), which is twice the
			// signed area of the triangle defined by the three points.
			// If this area is positive, c is left of the line ab, if zero
			// c is on ab, otherwise c is right of ab.
			return (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
			// or equivalently: a.X*b.Y - b.X*a.Y + c.X*a.Y - a.X*c.Y + b.X*c.Y - c.X*b.Y

		}

		public static Pair operator +(Pair p, Pair q)
		{
			return new Pair(p.X + q.X, p.Y + q.Y);
		}

		public static Pair operator -(Pair p, Pair q)
		{
			return new Pair(p.X - q.X, p.Y - q.Y);
		}

		public static Pair operator *(Pair p, double s)
		{
			return new Pair(p.X * s, p.Y * s);
		}

		public static Pair operator *(double s, Pair p)
		{
			return new Pair(p.X * s, p.Y * s);
		}

		public static Pair operator /(Pair p, double s)
		{
			return new Pair(p.X / s, p.Y / s);
		}

		public override string ToString()
		{
			return $"X={X}, Y={Y}";
		}
	}
}

using System;
using System.Diagnostics;

namespace ProSuite.Commons.AGP.Core.Carto
{
	/// <summary>
	/// A lightweight X,Y pair that can function as a
	/// point or vector, with a few common operations.
	/// </summary>
	[DebuggerDisplay("X={X}, Y={Y}")]
	public readonly struct Pair // TODO move to ProSuite.Commons/Geom
	{
		public readonly double X;
		public readonly double Y;

		public Pair(double x, double y)
		{
			X = x;
			Y = y;
		}

		public static Pair Null => new(0, 0);

		public double Length => Math.Sqrt(X * X + Y * Y);

		public double AngleDegrees => Math.Atan2(Y, X) * 180.0 / Math.PI;

		public Pair Normalized()
		{
			var invNorm = 1.0 / Length;
			return new Pair(X * invNorm, Y * invNorm);
		}

		public Pair Translated(double dx, double dy) // TODO rename Shifted? (shorter, MF/MP)
		{
			return new Pair(X + dx, Y + dy);
		}

		public Pair Rotated(double angleDegrees)
		{
			// (optimise 90/180/270/360 rotations? caller's duty?)
			double rad = angleDegrees * Math.PI / 180.0;
			double cos = Math.Cos(rad);
			double sin = Math.Sin(rad);
			double x = X * cos - Y * sin;
			double y = X * sin + Y * cos;
			return new Pair(x, y);
		}

		public Pair Rotated(double angleDegrees, Pair pivot)
		{
			return Translated(-pivot.X, -pivot.Y)
				.Rotated(angleDegrees)
				.Translated(pivot.X, pivot.Y);
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

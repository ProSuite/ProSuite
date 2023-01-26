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

		//public static Pair FromXY(double x, double y)
		//{
		//	return new Pair(x, y);
		//}

		//public static Pair FromPoint(MapPoint point)
		//{
		//	return new Pair(point.X, point.Y);
		//}

		//public MapPoint ToPoint(SpatialReference sref = null)
		//{
		//	return MapPointBuilderEx.CreateMapPoint(X, Y, sref);
		//}

		public double Length => Math.Sqrt(LengthSquared);

		public double LengthSquared => X * X + Y * Y;

		public Pair Translated(double dx, double dy) // TODO rename Shifted? (shorter, MF/MP)
		{
			return new Pair(X + dx, Y + dy);
		}

		public Pair Rotated(double angleDegrees)
		{
			// TODO optimise the simple cases 90/180/270/360?
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

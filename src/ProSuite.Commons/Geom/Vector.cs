using System;
using System.Globalization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Summary description for Vector.
	/// </summary>
	public class Vector : Pnt
	{
		private readonly int _miDimension;

		public Vector(int dimension) : base(dimension)
		{
			Coordinates = new double[dimension];
			_miDimension = dimension;
		}

		public Vector([NotNull] double[] coordinates) : base(0)
		{
			Assert.ArgumentNotNull(coordinates, nameof(coordinates));

			Coordinates = coordinates;
			_miDimension = coordinates.Length;
		}

		public override Box Extent => new Box(this, this);

		public override int Dimension => _miDimension;

		public override Pnt ClonePnt()
		{
			return new Vector((double[]) Coordinates.Clone());
		}

		public override bool Equals(object obj)
		{
			var cmpr = obj as Pnt;
			if (cmpr == null)
			{
				return false;
			}

			if (cmpr.Dimension != _miDimension)
			{
				return false;
			}

			for (var i = 0; i < _miDimension; i++)
			{
				if (Math.Abs(cmpr[i] - Coordinates[i]) > double.Epsilon)
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode()
		{
			if (_miDimension == 1)
			{
				return Coordinates[0].GetHashCode();
			}

			return Coordinates[0].GetHashCode() ^ Coordinates[1].GetHashCode();
		}

		public double LengthSquared => GetLengthSquared(Dimension);

		public double Length2DSquared => GetLengthSquared(dimension: 2);

		private double GetLengthSquared(int dimension)
		{
			double result = 0;
			for (var i = 0; i < dimension; i++)
			{
				double d = this[i];
				result += d * d;
			}

			return result;
		}

		public double Length => Math.Sqrt(LengthSquared);

		public static Vector operator +(Vector v0, Vector v1)
		{
			AssertSameDimensions(v0, v1);

			int iDim = v0.Dimension;
			var v = new Vector(iDim);
			for (var i = 0; i < iDim; i++)
			{
				v[i] = v0[i] + v1[i];
			}

			return v;
		}

		public static Vector operator -(Vector v0, Vector v1)
		{
			AssertSameDimensions(v0, v1);

			int iDim = v0.Dimension;
			var v = new Vector(iDim);
			for (var i = 0; i < iDim; i++)
			{
				v[i] = v0[i] - v1[i];
			}

			return v;
		}

		public static Vector operator *(Vector v, double s)
		{
			var result = new Vector(v.Dimension);
			for (var i = 0; i < v.Dimension; i++)
			{
				result[i] = v[i] * s;
			}

			return result;
		}

		public static Vector operator /(Vector v, double s)
		{
			var result = new Vector(v.Dimension);
			for (var i = 0; i < v.Dimension; i++)
			{
				result[i] = v[i] / s;
			}

			return result;
		}

		public double Dist2(Vector v)
		{
			AssertSameDimensions(this, v);

			double d2 = 0;
			for (var i = 0; i < Dimension; i++)
			{
				double d = this[i] - v[i];
				d2 += d * d;
			}

			return d2;
		}

		public override string ToString()
		{
			if (Dimension > 2)
			{
				return $"{X};{Y};{Coordinates[2]}";
			}

			if (Dimension > 1)
			{
				return $"{X};{Y}";
			}

			return Dimension > 0
				       ? X.ToString(CultureInfo.InvariantCulture)
				       : "null";
		}

		private static void AssertSameDimensions(Vector v0, Vector v1)
		{
			Assert.ArgumentCondition(v0.Dimension == v1.Dimension, "Dimensions differ");
		}
	}
}

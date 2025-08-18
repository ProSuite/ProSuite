using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Summary description for Point.
	/// </summary>
	public abstract class Pnt : IPnt, IBox
	{
		[NotNull] private double[] _coordinates;

		protected Pnt(int dimension)
		{
			_coordinates = new double[dimension];
		}

		public abstract Box Extent { get; }

		[NotNull]
		protected internal double[] Coordinates
		{
			get { return _coordinates; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				_coordinates = value;
			}
		}

		#region IBoundedXY members

		public double XMin => X;

		public double YMin => Y;

		public double XMax => X;

		public double YMax => Y;

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
			return ClonePnt();
		}

		double IBox.GetMaxExtent()
		{
			return 0;
		}

		#endregion

		#region IPnt Members

		/// <summary>
		/// returns coordinate of dimension index
		/// </summary>
		public double this[int index]
		{
			get { return _coordinates[index]; }
			set { _coordinates[index] = value; }
		}

		public double X
		{
			get { return _coordinates[0]; }
			set { _coordinates[0] = value; }
		}

		public double Y
		{
			get { return _coordinates[1]; }
			set { _coordinates[1] = value; }
		}

		public double Z
		{
			get
			{
				if (_coordinates.Length < 3)
				{
					return double.NaN;
				}

				return _coordinates[2];
			}
			set
			{
				if (_coordinates.Length < 3)
				{
					throw new InvalidOperationException("This point does not have a Z coordinate.");
				}

				_coordinates[2] = value;
			}
		}

		public IPnt Clone()
		{
			return ClonePnt();
		}

		IBox IGmtry.Extent => Extent;

		public IGmtry Border => null;

		public bool Intersects(IBox box)
		{
			return box.Contains((IPnt) this);
		}

		//public int Topology
		//{
		//    get { return 0; }
		//}

		public abstract int Dimension { get; }

		#endregion

		public bool Intersects([NotNull] IBox box, [NotNull] int[] dimensionList)
		{
			return box.Contains((IPnt) this, dimensionList);
		}

		[NotNull]
		public abstract Pnt ClonePnt();

		[NotNull]
		public static Pnt Create(int dim)
		{
			if (dim == 2)
			{
				return new Pnt2D();
			}

			if (dim == 3)
			{
				return new Pnt3D();
			}

			return new Vector(dim);
		}

		[NotNull]
		public static Pnt Create([NotNull] IPnt point)
		{
			int dimension = point.Dimension;

			Pnt p = Create(dimension);
			for (var i = 0; i < dimension; i++)
			{
				p[i] = point[i];
			}

			return p;
		}

		public double Dist2([NotNull] ICoordinates point)
		{
			// Old implementation if an IPnt is passed.
			if (point is IPnt pnt)
			{
				return Dist2(pnt, pnt.Dimension);
			}

			// New implementation if an ICoordinates is passed.
			if (double.IsNaN(point.Z) || double.IsNaN(Z))
			{
				return Dist2(point, 2);
			}
			return Dist2(point, Math.Min(Dimension, 3));
		}

		public double Dist2([NotNull] ICoordinates point, int dimension)
		{
			if (dimension < 2 || dimension > 3)
			{
				throw new ArgumentException("Maximum supported dimension is 3", nameof(dimension));
			}

			double squaredDistance = 0;

			double deltaX = X - point.X;
			double deltaY = Y - point.Y;
			squaredDistance += (deltaX * deltaX) + (deltaY * deltaY);

			// Add Z distance calculation for 3D
			if (dimension < 3)
			{
				return squaredDistance;
			}

			if (double.IsNaN(Z) || double.IsNaN(point.Z))
			{
				throw new ArgumentException("Point has NaN Z - cannot calculate 3D distance");
			}

			double deltaZ = Z - point.Z;
			squaredDistance += deltaZ * deltaZ;

			return squaredDistance;
		}

		public double OrigDist2()
		{
			return OrigDist2(Dimension);
		}

		public double OrigDist2(int dimension)
		{
			double dDist2 = 0;
			for (var i = 0; i < dimension; i++)
			{
				double d = this[i];
				dDist2 += d * d;
			}

			return dDist2;
		}

		public static Pnt operator +(Pnt p0, IPnt p1)
		{
			int iDim = Math.Min(p0.Dimension, p1.Dimension);
			Pnt pSum = Create(iDim);
			for (var i = 0; i < iDim; i++)
			{
				pSum[i] = p0[i] + p1[i];
			}

			return pSum;
		}

		public static Pnt operator -(Pnt p0, IPnt p1)
		{
			int iDim = Math.Min(p0.Dimension, p1.Dimension);
			Pnt pDiff = Create(iDim);
			for (var i = 0; i < iDim; i++)
			{
				pDiff[i] = p0[i] - p1[i];
			}

			return pDiff;
		}

		/// <summary>
		/// linear product
		/// </summary>
		public static Pnt operator *(double f, Pnt p)
		{
			int iDim = p.Dimension;
			Pnt pSkalar = Create(iDim);
			for (var i = 0; i < iDim; i++)
			{
				pSkalar[i] = f * p[i];
			}

			return pSkalar;
		}

		/// <summary>
		/// scalar product
		/// </summary>
		public static double operator *(Pnt p0, Pnt p1)
		{
			int iDim = p0.Dimension;
			double d = 0;
			for (var i = 0; i < iDim; i++)
			{
				d += p0[i] * p1[i];
			}

			return d;
		}

		/// <summary>
		/// Vector product
		/// </summary>
		public double VectorProduct(Pnt p1)
		{
			return X * p1.Y - Y * p1.X;
		}

		/// <summary>
		/// Calculate the factor, for which linearPoint = factor * this
		/// </summary>
		/// <param name="linearPoint">Point that is linear product of this</param>
		/// <returns>factor, for which linearPoint = factor * this</returns>
		public double GetFactor(Pnt linearPoint)
		{
			int iDim = Dimension;
			int iMax = -1;
			double dMax = 0;
			for (var i = 0; i < iDim; i++)
			{
				double dAbs = Math.Abs(this[i]);
				if (dMax < dAbs)
				{
					dMax = dAbs;
					iMax = i;
				}
			}

			if (iMax == -1)
			{
				// linearPoint is [0,0,0]
				return 0;
			}

			return linearPoint[iMax] / this[iMax];
		}
	}
}

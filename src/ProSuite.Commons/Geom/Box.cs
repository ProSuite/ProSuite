using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class Box : IBox
	{
		public class BoxComparer : IEqualityComparer<IBox>
		{
			public bool Equals(IBox x, IBox y)
			{
				if (x == y) return true;
				if (x == null) return false;
				if (y == null) return false;

				double d = Math.Max(x.Max.X - x.Min.X, x.Max.Y - x.Min.Y);
				double dMax = 0.1 * d;
				if (Math.Abs(x.Min.X - y.Min.X) > dMax) return false;
				if (Math.Abs(x.Min.Y - y.Min.Y) > dMax) return false;
				if (Math.Abs(x.Max.X - y.Max.X) > dMax) return false;
				if (Math.Abs(x.Max.Y - y.Max.Y) > dMax) return false;

				return true;
			}

			public int GetHashCode(IBox obj)
			{
				return obj.Min.X.GetHashCode() ^ (29 * obj.Min.Y.GetHashCode());
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Box"/> class.
		/// </summary>
		/// <param name="min">The minimum point.</param>
		/// <param name="max">The maximum point.</param>
		public Box([NotNull] Pnt min, [NotNull] Pnt max)
		{
			Dimension = min.Dimension;

			Min = min;
			Max = max;
		}

		public override string ToString()
		{
			return string.Format("Min {0}; Max {1}", Min, Max);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Box"/> class.
		/// </summary>
		/// <param name="p0">The first point.</param>
		/// <param name="p1">The second point.</param>
		/// <param name="verify">if set to <c>true</c> the minimum and maximum point 
		/// are determined based on the point coordinates.</param>
		public Box([NotNull] Pnt p0, [NotNull] Pnt p1, bool verify)
		{
			Dimension = p0.Dimension < p1.Dimension
				            ? p0.Dimension
				            : p1.Dimension;

			if (verify)
			{
				for (var i = 0; i < Dimension; i++)
				{
					if (p0[i] <= p1[i])
					{
						continue;
					}

					// swap the coordinate values for this dimension
					double t = p1[i];
					p1[i] = p0[i];
					p0[i] = t;
				}
			}

			Min = p0;
			Max = p1;
		}

		[NotNull]
		public Pnt Min { get; private set; }

		[NotNull]
		public Pnt Max { get; private set; }

		public Box Border => this;

		#region IBox Members

		IPnt IBox.Min => Min;

		IPnt IBox.Max => Max;

		public double GetMaxExtent()
		{
			return GetMaxExtent(GeomUtils.GetDimensionList(Dimension));
		}

		public bool Contains(IPnt p, int[] dimensionList)
		{
			foreach (int i in dimensionList)
			{
				if (p[i] < Min[i])
				{
					return false;
				}

				if (p[i] > Max[i])
				{
					return false;
				}
			}

			return true;
		}

		public bool Contains(IPnt p)
		{
			return Contains(p, GeomUtils.GetDimensionList(Dimension));
		}

		/// <summary>
		/// Indicates if box is within this
		/// </summary>
		/// <returns></returns>
		public bool Contains(IBox box, int[] dimensionList)
		{
			return ExceedDimension(box, dimensionList) == 0;
		}

		/// <summary>
		/// Indicates if box is within this
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
		public bool Contains(IBox box)
		{
			return Contains(box, GeomUtils.GetDimensionList(Dimension));
		}

		public bool Intersects(IBox box)
		{
			return Intersects(box, GeomUtils.GetDimensionList(Dimension));
		}

		/// <summary>
		/// set this box to the bounding box of this and box
		/// </summary>
		/// <param name="box"></param>
		public void Include(IBox box)
		{
			Include(box, Math.Min(Dimension, box.Dimension));
		}

		public int Dimension { get; }

		public IBox Extent => this;

		IGmtry IGmtry.Border => Border;

		IBox IBox.Clone()
		{
			return Clone();
		}

		#endregion

		public double GetMaxExtent([NotNull] IEnumerable<int> dimensions)
		{
			Assert.ArgumentNotNull(dimensions, nameof(dimensions));

			double maxExtent = 0;

			foreach (int i in dimensions)
			{
				double dExtent = Max[i] - Min[i];
				if (dExtent > maxExtent)
				{
					maxExtent = dExtent;
				}
			}

			return maxExtent;
		}

		public int ExceedDimension([NotNull] IBox box, [NotNull] int[] dimensionList)
		{
			IPnt boxMin = box.Min;
			IPnt boxMax = box.Max;
			foreach (int i in dimensionList)
			{
				if (boxMin[i] < Min[i])
				{
					return -i - 1;
				}

				if (boxMax[i] > Max[i])
				{
					return i + 1;
				}
			}

			return 0;
		}

		public int ExceedDimension([NotNull] IBox box)
		{
			return ExceedDimension(box, GeomUtils.GetDimensionList(Dimension));
		}

		public bool Intersects([NotNull] IBox box, [NotNull] int[] dimensions)
		{
			foreach (int i in dimensions)
			{
				if (box.Min[i] > Max[i] || box.Max[i] < Min[i])
				{
					return false;
				}
			}

			return true;
		}

		public bool Intersects([NotNull] IGmtry other)
		{
			return other.Intersects(this);
		}

		public override bool Equals(object obj)
		{
			var cmpr = obj as Box;
			if (cmpr == null)
			{
				return false;
			}

			return Min.Equals(cmpr.Min) && Max.Equals(cmpr.Max);
		}

		public override int GetHashCode()
		{
			return Min.GetHashCode();
		}

		/// <summary>
		/// set this box to the bounding box of this and box
		/// </summary>
		/// <param name="box"></param>
		/// <param name="dimension"></param>
		public void Include([NotNull] IBox box, int dimension)
		{
			if (Min == Max)
			{
				Min = Min.ClonePnt();
				Max = Max.ClonePnt();
			}

			for (var i = 0; i < dimension; i++)
			{
				if (box.Min[i] < Min[i])
				{
					Min[i] = box.Min[i];
				}

				if (box.Max[i] > Max[i])
				{
					Max[i] = box.Max[i];
				}
			}
		}

		/// <summary>
		/// set this box to the bounding box of this and point
		/// </summary>
		public void Include([NotNull] Pnt point, int dimension)
		{
			if (Min == Max)
			{
				Min = Min.ClonePnt();
				Max = Max.ClonePnt();
			}

			for (var i = 0; i < dimension; i++)
			{
				if (point[i] < Min[i])
				{
					Min[i] = point[i];
				}
				else if (point[i] > Max[i])
				{
					Max[i] = point[i];
				}
			}
		}

		/// <summary>
		/// set this box to the bounding box of this and point
		/// </summary>
		public void Include([NotNull] Pnt point)
		{
			Include(point, Math.Min(Dimension, point.Dimension));
		}

		[NotNull]
		public Box Clone()
		{
			return new Box(Min.ClonePnt(), Max.ClonePnt());
		}
	}
}

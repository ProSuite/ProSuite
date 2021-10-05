using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// Comparer for WKSPointZs. 
	/// Note for use in dictionaries:
	/// A. The point coordinates should always be snapped to the spatial reference
	/// B. If the tolerance is set to a number higher than the resolution: Points further apart 
	///    than the resolution but closer than the tolerance can still be considered different.
	///    if they are on different side of the 'rounding boundary'. 
	/// </summary>
	public class WKSPointZComparer : IComparer<WKSPointZ>, IEqualityComparer<WKSPointZ>
	{
		private double _xyTolerance;
		private double _zTolerance;

		private double _xOrigin;
		private double _yOrigin;
		private double _zOrigin;

		public WKSPointZComparer() : this(0, 0, 0, 0, 0) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="WKSPointZComparer"/> class.
		/// The comparisons are based on the *resolution* of the spatial reference
		/// as opposed to the tolerance (used by IClone/IRelationalOperator)
		/// </summary>
		/// <param name="spatialReference"></param>
		/// <param name="compare3D"></param>
		public WKSPointZComparer([NotNull] ISpatialReference spatialReference, bool compare3D)
			: this(SpatialReferenceUtils.GetXyResolution(spatialReference),
			       compare3D
				       ? GeometryUtils.GetZResolution(spatialReference)
				       : double.NaN,
			       spatialReference) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="WKSPointZComparer"/> class. 
		/// </summary>
		/// <param name="xyTolerance">The XY-tolerance. If the tolerance is set to a number higher 
		/// than the resolution of the coordinates's spatial reference: Points further apart than 
		/// the resolution but closer than the tolerance can still be considered different.</param>
		/// <param name="zTolerance">The Z-tolerance. Use NaN to ignore Z values</param>
		/// <param name="spatialReference"></param>
		public WKSPointZComparer(double xyTolerance, double zTolerance,
		                         [NotNull] ISpatialReference spatialReference)
		{
			double xMin;
			double yMin;
			double zMin;

			spatialReference.GetDomain(out xMin, out double _, out yMin, out double _);

			spatialReference.GetZDomain(out zMin, out double _);

			Initialize(xyTolerance, zTolerance, xMin, yMin, zMin);
		}

		public WKSPointZComparer(double xyTolerance, double zTolerance, double xOrigin,
		                         double yOrigin, double zOrigin)
		{
			Initialize(xyTolerance, zTolerance, xOrigin, yOrigin, zOrigin);
		}

		private void Initialize(double xyTolerance, double zTolerance, double xOrigin,
		                        double yOrigin, double zOrigin)
		{
			_xyTolerance = xyTolerance;
			_zTolerance = zTolerance;

			_xOrigin = xOrigin;
			_yOrigin = yOrigin;
			_zOrigin = zOrigin;
		}

		#region IComparer<WKSPointZ> Members

		public int Compare(WKSPointZ a, WKSPointZ b)
		{
			// dont't use the tolerance to make sure the sorting is correct also for small distances
			if (a.X < b.X)
			{
				return -1;
			}

			if (a.X > b.X)
			{
				return +1;
			}

			// a.X == b.X

			if (a.Y < b.Y)
			{
				return -1;
			}

			if (a.Y > b.Y)
			{
				return +1;
			}

			// a.Y == b.Y

			if (! double.IsNaN(_zTolerance))
			{
				if (a.Z < b.Z)
				{
					return -1;
				}

				if (a.Z > b.Z)
				{
					return 1;
				}

				if (a.Z == b.Z)
				{
					return 0;
				}

				// NOTE: imporant to use the same z-logic as in GeometryUtils.IsSamePoint regarding NaN:
				if (double.IsNaN(a.Z) && double.IsNaN(b.Z))
				{
					return 0;
				}

				if (double.IsNaN(a.Z) && ! double.IsNaN(b.Z))
				{
					return -1;
				}

				if (! double.IsNaN(a.Z) && double.IsNaN(b.Z))
				{
					return 1;
				}
			}

			// a == b with 2D comparison
			return 0;
		}

		#endregion

		#region Implementation of IEqualityComparer<WKSPointZ>

		public bool Equals(WKSPointZ x, WKSPointZ y)
		{
			return GeometryUtils.IsSamePoint(x, y, _xyTolerance, _zTolerance);
		}

		public int GetHashCode(WKSPointZ point)
		{
			// - NOTE: double.GetHashCode is not just its integer representation, so it's no problem to use it also for lat/long
			// - snap to grid to make sure the 'same' points get the same hash -> alternatively make sure the input geometries are snapped!
			// -> if the tolerance is > the resolution there is a risk that 2 points closer than the tolerance are still considered 
			//    different because they are on the close to each other but get rounded to a different grid value. 
			// TODO: Consider using 10 times the tolerance which results in more hash collisions but less differences smaller than the tolerance
			unchecked
			{
				int x, y;

				if (_xyTolerance > 0)
				{
					x = Math.Round((point.X - _xOrigin) / _xyTolerance).GetHashCode();
					y = Math.Round((point.Y - _yOrigin) / _xyTolerance).GetHashCode();
				}
				else
				{
					x = point.X.GetHashCode();
					y = point.Y.GetHashCode();
				}

				int result = x;
				result = (result * 397) ^ y;

				if (! double.IsNaN(_zTolerance))
				{
					int z;
					if (_zTolerance > 0)
					{
						z = Math.Round((point.Z - _zOrigin) / _zTolerance).GetHashCode();
					}
					else
					{
						z = point.Z.GetHashCode();
					}

					result = (result * 397) ^ z;
				}

				return result;
			}
		}

		#endregion
	}
}

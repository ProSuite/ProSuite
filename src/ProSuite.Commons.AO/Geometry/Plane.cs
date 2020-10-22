using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	public class Plane
	{
		private readonly double _c;
		private readonly double _cx;
		private readonly double _cy;

		private readonly double _nx;
		private readonly double _ny;
		private readonly double _nz;
		private readonly double _nf;

		/// <summary>
		/// Initializes a new instance of the <see cref="Plane"/> class.
		/// </summary>
		/// <param name="c">The c.</param>
		/// <param name="cx">The cx.</param>
		/// <param name="cy">The cy.</param>
		public Plane(double c, double cx, double cy)
		{
			_c = c;
			_cx = cx;
			_cy = cy;

			GetN(c, cx, cy, out _nx, out _ny, out _nz, out _nf);
		}

		/// <summary>
		/// Compute the plane's coefficients from a set of points. This method use
		/// a linear regression in the least-square sense. Result is undertermined
		/// if all points are colinear.
		/// </summary>
		/// <param name="x">The x coordinates.</param>
		/// <param name="y">The y coordinates.</param>
		/// <param name="z">The z values.</param>
		public Plane([NotNull] IList<double> x,
		             [NotNull] IList<double> y,
		             [NotNull] IList<double> z)
		{
			Assert.ArgumentNotNull(x, nameof(x));
			Assert.ArgumentNotNull(y, nameof(y));
			Assert.ArgumentNotNull(z, nameof(z));

			int size = x.Count;

			Assert.ArgumentCondition(size == y.Count, "y value count mismatch");
			Assert.ArgumentCondition(size == z.Count, "z value count mismatch");

			double sum_x = 0;
			double sum_y = 0;
			double sum_z = 0;
			double sum_xx = 0;
			double sum_yy = 0;
			double sum_xy = 0;
			double sum_zx = 0;
			double sum_zy = 0;
			double sum_zz = 0;

			for (var i = 0; i < size; i++)
			{
				double xi = x[i] - x[0]; // reduce numerical problems, move to first point
				double yi = y[i] - y[0];
				double zi = z[i] - z[0];
				sum_x += xi;
				sum_y += yi;
				sum_z += zi;
				sum_xx += xi * xi;
				sum_yy += yi * yi;
				sum_zz += zi * zi;
				sum_xy += xi * yi;
				sum_zx += zi * xi;
				sum_zy += zi * yi;
			}

			//  ( sum_zx - sum_z*sum_x )  =  cx*(sum_xx - sum_x*sum_x) + cy*(sum_xy - sum_x*sum_y)
			//  ( sum_zy - sum_z*sum_y )  =  cx*(sum_xy - sum_x*sum_y) + cy*(sum_yy - sum_y*sum_y)

			double meanX = sum_x / size + x[0];
			double meanY = sum_y / size + y[0];
			double meanZ = sum_z / size + z[0];

			double xx = sum_xx - sum_x * sum_x / size;
			double yy = sum_yy - sum_y * sum_y / size;
			double zz = sum_zz - sum_z * sum_z / size;
			double e = (xx + yy + zz);
			double epsilon = 1.0e-8 * (e * e);

			double denZ;
			GetCxy(size,
			       sum_x, sum_y, sum_z,
			       xx, yy,
			       sum_xy, sum_zx, sum_zy,
			       out _cx, out _cy, out denZ);
			_c = GetC(meanX, meanY, meanZ, _cx, _cy);

			if (Math.Abs(denZ) > epsilon)
			{
				GetN(_c, _cx, _cy, out _nx, out _ny, out _nz, out _nf);
			}
			else
			{
				double denX;
				double ay;
				double az;
				GetCxy(size,
				       sum_y, sum_z, sum_x,
				       yy, zz,
				       sum_zy, sum_xy, sum_zx,
				       out ay, out az, out denX);
				double a = GetC(meanY, meanZ, meanX, ay, az);

				if (Math.Abs(denX) > epsilon)
				{
					GetN(a, ay, az, out _ny, out _nz, out _nx, out _nf);
				}
				else
				{
					double denY;
					double bx;
					double bz;
					GetCxy(size,
					       sum_x, sum_z, sum_y,
					       xx, zz,
					       sum_zx, sum_xy, sum_zy,
					       out bx, out bz, out denY);
					double b = GetC(meanX, meanZ, meanY, bx, bz);

					if (Math.Abs(denY) > epsilon)
					{
						GetN(b, bx, bz, out _nx, out _nz, out _ny, out _nf);
					}
				}
			}
		}

		private static void GetCxy(int size, double sum_x, double sum_y, double sum_z,
		                           double xx, double yy, double sum_xy,
		                           double sum_zx,
		                           double sum_zy,
		                           out double cx, out double cy, out double den)
		{
			double zx = sum_zx - sum_z * sum_x / size;
			double zy = sum_zy - sum_z * sum_y / size;
			double xy = sum_xy - sum_x * sum_y / size;

			den = (xy * xy - xx * yy);

			cy = (zx * xy - zy * xx) / den;
			cx = (zy * xy - zx * yy) / den;
		}

		private static double GetC(double meanX, double meanY, double meanZ, double cx,
		                           double cy)
		{
			double c = meanZ - (cx * meanX + cy * meanY);
			return c;
		}

		private static void GetN(double c, double cx, double cy,
		                         out double nx, out double ny, out double nz, out double nf)
		{
			double length = Math.Sqrt(cx * cx + cy * cy + 1);

			nx = cx / length;
			ny = cy / length;
			nz = -1.0 / length;
			nf = c / length;
		}

		/// <summary>
		/// Compute the <c>z</c> value for the specified (<c>x</c>,<c>y</c>) point.
		/// </summary>
		/// <param name="x">The x value.</param>
		/// <param name="y">The y value.</param>
		/// <returns>The z value.</returns>
		public double Z(double x, double y)
		{
			return _c + _cx * x + _cy * y;
		}

		/// <summary>
		/// The <c>c</c> coefficient for this plane. This coefficient appears in the plane 
		/// equation <c><strong>c</strong></c>+<c>cx</c>*<c>x</c>+<c>cy</c>*<c>y</c>
		/// </summary>
		public double C => _c;

		/// <summary>
		/// The <c>cx</c> coefficient for this plane. This coefficient appears in the plane 
		/// equation <c><strong>c</strong></c>+<c>cx</c>*<c>x</c>+<c>cy</c>*<c>y</c>
		/// </summary>
		public double Cx => _cx;

		/// <summary>
		/// The <c>cy</c> coefficient for this plane. This coefficient appears in the plane 
		/// equation <c><strong>c</strong></c>+<c>cx</c>*<c>x</c>+<c>cy</c>*<c>y</c>
		/// </summary>
		public double Cy => _cy;

		/// <summary>
		/// The <c>x</c> component of the normal vector for this plane. This coefficient appears in the plane 
		/// equation <c><strong>nf</strong></c>+<c>nx</c>*<c>x</c>+<c>ny</c>*<c>y</c>+<c>nz</c>*<c>z</c>=0 
		/// where nx*nx + ny*ny + nz*nz = 1
		/// </summary>
		public double Nx => _nx;

		/// <summary>
		/// The <c>y</c> component of the normal vector for this plane. This coefficient appears in the plane 
		/// equation <c><strong>nf</strong></c>+<c>nx</c>*<c>x</c>+<c>ny</c>*<c>y</c>+<c>nz</c>*<c>z</c>=0 
		/// where nx*nx + ny*ny + nz*nz = 1
		/// </summary>
		public double Ny => _ny;

		/// <summary>
		/// The <c>z</c> component of the normal vector for this plane. This coefficient appears in the plane 
		/// equation <c><strong>nf</strong></c>+<c>nx</c>*<c>x</c>+<c>ny</c>*<c>y</c>+<c>nz</c>*<c>z</c>=0 
		/// where nx*nx + ny*ny + nz*nz = 1
		/// </summary>
		public double Nz => _nz;

		/// <summary>
		/// The <c>distance</c> from the origin of this plane. This coefficient appears in the plane 
		/// equation <c><strong>nf</strong></c>+<c>nx</c>*<c>x</c>+<c>ny</c>*<c>y</c>+<c>nz</c>*<c>z</c>=0 
		/// where nx*nx + ny*ny + nz*nz = 1
		/// </summary>
		public double Nf => _nf;

		public bool Valid => ! double.IsNaN(_c) &&
		                     ! double.IsNaN(_cx) &&
		                     ! double.IsNaN(_cy);

		public bool IsDefined => Math.Abs(_nx) > double.Epsilon ||
		                         Math.Abs(_ny) > double.Epsilon ||
		                         Math.Abs(_nz) > double.Epsilon;

		[CLSCompliant(false)]
		public WKSPointZ GetNormalVector()
		{
			WKSPointZ normal;

			normal.X = Nx;
			normal.Y = Ny;
			normal.Z = Nz;

			return normal;
		}

		public double GetDistance(double x, double y, double z)
		{
			double f = Nx * x + Ny * y + Nz * z + Nf;

			return Math.Abs(f);
		}

		[CLSCompliant(false)]
		public WKSPointZ GetPlaneVector()
		{
			WKSPointZ normal = GetNormalVector();

			WKSPointZ planeVector;
			if (TryGetNormal(normal, WKSPointZUtils.CreatePoint(1, 0, 0), out planeVector)) { }
			else if (TryGetNormal(normal, WKSPointZUtils.CreatePoint(0, 1, 0), out planeVector)) { }
			else if (TryGetNormal(normal, WKSPointZUtils.CreatePoint(0, 0, 1), out planeVector)) { }
			else
			{
				throw new InvalidOperationException("unable to create plane vector");
			}

			WKSPointZ normedPlaneVector = WKSPointZUtils.GetNormed(planeVector);

			return normedPlaneVector;
		}

		[CLSCompliant(false)]
		public void GetPlaneVectors(out WKSPointZ planeVector1, out WKSPointZ planeVector2)
		{
			WKSPointZ normal = GetNormalVector();

			if (TryGetPlaneVectors(normal, WKSPointZUtils.CreatePoint(1, 0, 0),
			                       out planeVector1, out planeVector2))
			{
				return;
			}

			if (TryGetPlaneVectors(normal, WKSPointZUtils.CreatePoint(0, 1, 0),
			                       out planeVector1, out planeVector2))
			{
				return;
			}

			if (TryGetPlaneVectors(normal, WKSPointZUtils.CreatePoint(0, 0, 1),
			                       out planeVector1, out planeVector2))
			{
				return;
			}

			throw new InvalidOperationException("unable to create plane vectors");
		}

		private static bool TryGetPlaneVectors(WKSPointZ normal, WKSPointZ vector,
		                                       out WKSPointZ planeVector1,
		                                       out WKSPointZ planeVector2)
		{
			planeVector1 = WKSPointZUtils.GetVectorProduct(normal, vector);

			const double e = 0.1;
			if (GetLength2(planeVector1) < e)
			{
				planeVector2 = new WKSPointZ();
				return false;
			}

			planeVector1 = WKSPointZUtils.GetNormed(planeVector1);

			planeVector2 = WKSPointZUtils.GetVectorProduct(normal, planeVector1);

			return GetLength2(planeVector2) >= e;
		}

		private static bool TryGetNormal(WKSPointZ v0, WKSPointZ v1, out WKSPointZ normal)
		{
			normal = WKSPointZUtils.GetVectorProduct(v0, v1);

			return GetLength2(normal) > 0.1;
		}

		private static double GetLength2(WKSPointZ vector)
		{
			return (vector.X * vector.X +
			        vector.Y * vector.Y +
			        vector.Z * vector.Z);
		}
	}
}

using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A plane that consists of a normal vector and a distance d from the origin.
	/// The plane equation used is ax + by + cz = d
	/// </summary>
	public class Plane3D
	{
		// NOTE: MathNet also has a good plane class. However when switching, the sign of D must be considered. 
		// The plane equation can be stated as ax + bx + cz + d = 0 or ax + bx + cz = d
		// Additionally, the directon of the normal is often defined differently (ccw for outer rings)
		// Consider merging with Commons.AO.Plane

		/// <summary>
		/// The normal vector. Consider implementing Vector3D
		/// </summary>
		[NotNull]
		public Vector Normal { get; }

		/// <summary>
		/// 
		/// </summary>
		public double D { get; }

		/// <summary>
		/// Creates a Plane object from the components of the plane equation ax + by + cz = d
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <param name="d"></param>
		public Plane3D(double a, double b, double c, double d)
			: this(new Vector(new[] {a, b, c}), d) { }

		public Plane3D([NotNull] Vector normal, double d)
		{
			Normal = normal;
			D = d;

			InitializeProperties();
		}

		public Plane3D([NotNull] Vector normal, [NotNull] Pnt3D planePoint)
		{
			Normal = normal;
			D = A * planePoint.X + B * planePoint.Y + C * planePoint.Z;

			InitializeProperties();
		}

		[CanBeNull]
		public static Plane3D TryFitPlane([NotNull] IList<Pnt3D> points, bool isRing = false)
		{
			int usablePointCount = isRing ? points.Count - 1 : points.Count;

			if (usablePointCount < 3)
			{
				return null;
			}

			return FitPlane(points, isRing);
		}

		///  <summary>
		///  Using linear least squares as described in http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
		/// 	- Supports vertical planes.
		/// 	- For improved floating point accuracy / numerical stability the largest determinant is used and
		/// 	  the origin is set to the centroid of the points.
		///  </summary>
		///  <param name="points">The points that define the plane</param>
		/// <param name="isRing">Whether the provided points define a closed ring, i.e. the last point in the list
		/// of points is equal to the first point. If true, the plane normal will be oriented such that the ring area 
		/// is positive, i.e. the points have clockwise orientation and the plane normal will be oriented according 
		/// to the view direction.</param>
		/// <returns></returns>
		[NotNull]
		public static Plane3D FitPlane([NotNull] IList<Pnt3D> points, bool isRing = false)
		{
			int n = isRing ? points.Count - 1 : points.Count;

			Assert.ArgumentCondition(n >= 3,
			                         "At least 3 points (4 points in a ring) are required to define a plane.");

			// Origin shift for better numerical accuracy:
			// Rather than just subtracting the first point, use the actual centroid to keep the coordinates close to the origin
			var sum = new Pnt3D();
			for (var i = 0; i < n; i++)
			{
				Pnt3D point = points[i];
				sum += point;
			}

			Pnt3D centroid = sum / n;

			// 3x3 covariance matrix (excluding symmetries):
			double xx = 0;
			double xy = 0;
			double xz = 0;
			double yy = 0;
			double yz = 0;
			double zz = 0;

			foreach (Pnt3D point in points)
			{
				Pnt3D r = point - centroid;

				double x = r.X;
				double y = r.Y;
				double z = r.Z;

				xx += x * x;
				xy += x * y;
				xz += x * z;
				yy += y * y;
				yz += y * z;
				zz += z * z;
			}

			// Avoid wasting significant digits by letting the numbers gow too large!
			// Otherwise the D-value easily grows to 10^15 if the the input points are spaced
			// by a few 100m. See CanDetermineInclinedPlaneCoincidenceMirroredAt0
			// Consider using Kahan summation as well.
			double div = GetDivisorToImproveEpsilon(xx, yy, zz);

			xx /= div;
			xy /= div;
			xz /= div;
			yy /= div;
			yz /= div;
			zz /= div;

			// Use the largest determinant for better numerical conditioning
			double detX = yy * zz - yz * yz;
			double detY = xx * zz - xz * xz;
			double detZ = xx * yy - xy * xy;

			double maxDet = Math.Max(detX, Math.Max(detY, detZ));

			var normal = new Vector(3);

			if (MathUtils.AreEqual(detZ, maxDet))
			{
				normal[0] = xy * yz - xz * yy;
				normal[1] = xy * xz - yz * xx;
				normal[2] = detZ;
			}
			else if (MathUtils.AreEqual(detX, maxDet))
			{
				normal[0] = detX;
				normal[1] = xz * yz - xy * zz;
				normal[2] = xy * yz - xz * yy;
			}
			else
			{
				normal[0] = xz * yz - xy * zz;
				normal[1] = detY;
				normal[2] = xy * xz - yz * xx;
			}

			normal = OrientNormal(normal, points, isRing);

			var result = new Plane3D(normal, centroid);

			// It is important to remember the worst-case epsilon, especially for collinear points
			result.Epsilon = Math.Max(
				result.Epsilon,
				MathUtils.GetDoubleSignificanceEpsilon(xx, xy, xz, yy, yz, zz));

			return result;
		}

		/// <summary>
		/// The a component of the plane equation ax + by + cz = d
		/// </summary>
		public double A => Normal.X;

		/// <summary>
		/// The b component of the plane equation ax + by + cz = d
		/// </summary>
		public double B => Normal.Y;

		/// <summary>
		/// The c component of the plane equation ax + by + cz = d
		/// </summary>
		public double C => Normal[2];

		public bool IsDefined => Math.Abs(A) > Epsilon ||
		                         Math.Abs(B) > Epsilon ||
		                         Math.Abs(C) > Epsilon;

		public double LengthOfNormal { get; private set; }

		public double LengthOfNormalSquared { get; private set; }

		public double Epsilon { get; set; }

		public Vector GetUnitNormal()
		{
			if (! IsDefined)
			{
				throw new InvalidOperationException("Plane is not defined.");
			}

			return Normal / LengthOfNormal;
		}

		public double GetDistanceSigned(Pnt3D point)
		{
			return GetDistanceSigned(point.X, point.Y, point.Z);
		}

		public double GetDistanceSigned(double x, double y, double z)
		{
			if (! IsDefined)
			{
				throw new InvalidOperationException("Plane is not defined.");
			}

			var v = new Pnt3D(x, y, z);

			double dotProd = GeomUtils.DotProduct(v, A, B, C);

			return (dotProd - D) / LengthOfNormal;
		}

		public double GetDistanceAbs(double x, double y, double z)
		{
			return Math.Abs(GetDistanceSigned(x, y, z));
		}

		/// <summary>
		/// Returns the intersection point of the plane with the line between the specified endpoints,
		/// or null
		/// - if the line does not intersect the plane
		/// - if the line is parallel or completely lies in the plane.
		/// </summary>
		/// <param name="lineStart"></param>
		/// <param name="lineEnd"></param>
		/// <returns></returns>
		[CanBeNull]
		public Pnt3D GetIntersectionPoint([NotNull] Pnt3D lineStart,
		                                  [NotNull] Pnt3D lineEnd)
		{
			double? t = GetIntersectionFactor(lineStart, lineEnd);

			if (t == null)
			{
				return null;
			}

			if (t < 0 || t > 1)
			{
				// The intersection is outside of the line
				return null;
			}

			Pnt3D result = lineStart + (lineEnd - lineStart) * t.Value;

			return result;
		}

		public double? GetIntersectionFactor(Pnt3D lineStart, Pnt3D lineEnd)
		{
			if (! IsDefined)
			{
				throw new InvalidOperationException("Plane is not defined.");
			}

			// Required: any point on the plane p:
			var p = new Pnt3D(Normal * D / LengthOfNormalSquared);

			double denominator = GeomUtils.DotProduct(lineEnd - lineStart, Normal);

			if (MathUtils.AreEqual(denominator, 0))
			{
				// The line is parallel to the plane.
				return null;
			}

			double t = GeomUtils.DotProduct(p - lineStart, Normal) /
			           denominator;
			return t;
		}

		public double GetZ(double x, double y)
		{
			if (! IsDefined)
			{
				throw new InvalidOperationException("Plane is not defined.");
			}

			if (MathUtils.AreEqual(C, 0))
			{
				throw new InvalidOperationException(
					"Cannot intersect vertical straight line with vertical plane.");
			}

			double result = (D - A * x - B * y) / C;

			return result;
		}

		public bool Equals(Plane3D other, double tolerance = 0)
		{
			return MathUtils.AreEqual(A, other.A, tolerance) &&
			       MathUtils.AreEqual(B, other.B, tolerance) &&
			       MathUtils.AreEqual(C, other.C, tolerance) &&
			       MathUtils.AreEqual(D, other.D, tolerance);
		}

		public bool IsCoincident([NotNull] Plane3D other)
		{
			if (! IsParallel(other))
			{
				return false;
			}

			// They are parallel. Now check the distance to the origin:
			double thisDistance = GetDistanceSigned(0, 0, 0);
			double otherDistance = other.GetDistanceSigned(0, 0, 0);

			// get the sign of any non-zero component of the normal:
			var one = new Pnt3D(1, 1, 1);
			bool oppositeDirectionNormals =
				other.Normal.GetFactor(one) * Normal.GetFactor(one) < 0;

			if (oppositeDirectionNormals)
			{
				otherDistance *= -1;
			}

			double e = Math.Max(Epsilon, other.Epsilon);

			return MathUtils.AreEqual(thisDistance, otherDistance, e);
		}

		public bool IsParallel(Plane3D other)
		{
			double e = Math.Max(Epsilon, other.Epsilon);

			Vector crossProduct = GeomUtils.CrossProduct(Normal, other.Normal);

			return MathUtils.AreEqual(crossProduct.X, 0, e) &&
			       MathUtils.AreEqual(crossProduct.Y, 0, e) &&
			       MathUtils.AreEqual(crossProduct[2], 0, e);
		}

		/// <summary>
		/// Determines whether the plane is vertical within the provided tolerance.
		/// The tolerance is applied to the Z component of the unit normal.
		/// </summary>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public bool IsVertical(double? tolerance = null)
		{
			double unitZcomponent = GetUnitNormal()[2];

			return MathUtils.AreEqual(unitZcomponent, 0, tolerance ?? Epsilon);
		}

		public override string ToString()
		{
			return $"Normal: {Normal}, distance: {D}";
		}

		private void InitializeProperties()
		{
			LengthOfNormalSquared = A * A + B * B + C * C;
			LengthOfNormal = Math.Sqrt(LengthOfNormalSquared);

			Epsilon = MathUtils.GetDoubleSignificanceEpsilon(A, B, C, D);
		}

		private static int GetDivisorToImproveEpsilon(double xx, double yy, double zz)
		{
			xx = Math.Abs(xx);
			yy = Math.Abs(yy);
			zz = Math.Abs(zz);

			double max = Math.Max(xx, Math.Max(yy, zz));

			// Consider also the smallest number? -> Requires more experiments with nearly-horizontal/vertical planes
			// Theoretically a left-shift could make sense if there are lots of leading zeroes in all the fractional parts,
			// such as 0.00000012345. However, this is unlikely in a cartesian system.
			const double threshold = 500;

			if (max > threshold)
			{
				double log2 = Math.Log(max, 2);

				double pow = Math.Floor(log2 - 1);

				return (int) Math.Pow(2, pow);
			}

			return 1;
		}

		[NotNull]
		private static Vector OrientNormal([NotNull] Vector normal,
		                                   [NotNull] IList<Pnt3D> points,
		                                   bool isRing)
		{
			if (isRing)
			{
				double area3D = GeomUtils.GetArea3D(points, new Pnt3D(normal));

				if (area3D < 0)
				{
					normal *= -1;
				}
			}

			return normal;
		}
	}
}

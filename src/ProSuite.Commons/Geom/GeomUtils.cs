using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.Wkb;

namespace ProSuite.Commons.Geom
{
	public static class GeomUtils
	{
		private static readonly int[][] _dimensionLists;
		private const double _epsi = 1e-12;

		/// <summary>
		/// Initializes the <see cref="GeomUtils"/> class.
		/// </summary>
		static GeomUtils()
		{
			var dimensionX = new[] {0};
			var dimensionXy = new[] {0, 1};
			var dimensionXyz = new[] {0, 1, 2};
			var dimensionEmpty = new int[] { };

			_dimensionLists =
				new[]
				{
					dimensionEmpty,
					dimensionX,
					dimensionXy,
					dimensionXyz
				};
		}

		[NotNull]
		[DebuggerStepThrough]
		internal static int[] GetDimensionList(int dimension)
		{
			if (dimension < 4) // 3)
			{
				return _dimensionLists[dimension];
			}

			var result = new int[dimension];

			for (var i = 0; i < dimension; i++)
			{
				result[i] = i;
			}

			return result;
		}

		[NotNull]
		public static Box CreateBox(double xMin, double yMin,
		                            double xMax, double yMax)
		{
			return new Box(new Pnt2D(xMin, yMin),
			               new Pnt2D(xMax, yMax));
		}

		[NotNull]
		public static IBox CreateBox([NotNull] IBox box, double expansionDistance = 0)
		{
			Assert.ArgumentNotNull(box, nameof(box));

			IBox clone = box.Clone();
			for (var dim = 0; dim < clone.Dimension; dim++)
			{
				clone.Min[dim] = box.Min[dim] - expansionDistance;
				clone.Max[dim] = box.Max[dim] + expansionDistance;
			}

			return clone;
		}

		[NotNull]
		public static IBox GetExpanded([NotNull] IBox box,
		                               double expansionDistance)
		{
			IPnt min = box.Min;
			IPnt max = box.Max;

			return new Box(
				new Pnt2D(min.X - expansionDistance, min.Y - expansionDistance),
				new Pnt2D(max.X + expansionDistance, max.Y + expansionDistance));
		}

		public static IBox GetBoundingBox3D([NotNull] IEnumerable<Pnt3D> points)
		{
			const int resultDimension = 3;

			var min = new Vector(resultDimension);
			var max = new Vector(resultDimension);

			for (var i = 0; i < resultDimension; i++)
			{
				min[i] = double.MaxValue;
				max[i] = double.MinValue;
			}

			foreach (Pnt3D point in points)
			{
				for (var i = 0; i < resultDimension; i++)
				{
					min[i] = Math.Min(min[i], point[i]);
					max[i] = Math.Max(max[i], point[i]);
				}
			}

			return new Box(min, max);
		}

		public static bool Equals2D([NotNull] Pnt p0,
		                            [NotNull] Pnt p1,
		                            double tolerance)
		{
			return MathUtils.IsWithinTolerance(Math.Abs(p0.X - p1.X), tolerance,
			                                   MathUtils.GetDoubleSignificanceEpsilon(
				                                   p0.X,
				                                   p1.X)) &&
			       MathUtils.IsWithinTolerance(Math.Abs(p0.Y - p1.Y), tolerance,
			                                   MathUtils.GetDoubleSignificanceEpsilon(
				                                   p0.Y,
				                                   p1.Y));
		}

		public static bool Equals3D([NotNull] Pnt3D p0,
		                            [NotNull] Pnt3D p1,
		                            double tolerance)
		{
			if (tolerance <= 0)
			{
				// Equals also if difference is non-significant
				return MathUtils.AreSignificantDigitsEqual(p0.X, p1.X) &&
				       MathUtils.AreSignificantDigitsEqual(p0.Y, p1.Y) &&
				       MathUtils.AreSignificantDigitsEqual(p0.Z, p1.Z);
			}

			return MathUtils.IsWithinTolerance(Math.Abs(p0.X - p1.X), tolerance,
			                                   MathUtils.GetDoubleSignificanceEpsilon(
				                                   p0.X,
				                                   p1.X)) &&
			       MathUtils.IsWithinTolerance(Math.Abs(p0.Y - p1.Y), tolerance,
			                                   MathUtils.GetDoubleSignificanceEpsilon(
				                                   p0.Y,
				                                   p1.Y)) &&
			       MathUtils.IsWithinTolerance(Math.Abs(p0.Z - p1.Z), tolerance,
			                                   MathUtils.GetDoubleSignificanceEpsilon(
				                                   p0.Z,
				                                   p1.Z));
		}

		/// <summary>
		/// Cut two lines
		/// </summary>
		/// <param name="s0">start point of 1. line</param>
		/// <param name="dir0">direction of 1. line</param>
		/// <param name="s1">start point of 2. line</param>
		/// <param name="dir1">direction of 2. line</param>
		/// <param name="f">factor of 1. line or square distance between parallel lines</param>
		/// <returns>intersection point or null if directions are parallel</returns>
		[CanBeNull]
		public static Pnt2D CutDirDir([NotNull] Pnt s0,
		                              [NotNull] Pnt dir0,
		                              [NotNull] Pnt s1,
		                              [NotNull] Pnt dir1,
		                              out double f)
			/*
(x1)     (a1)     (x2)      (a2)     (x)
(  ) + t*(  )  =  (  )  + v*(  ) ==> ( )
(y1)     (b1)     (y2)      (b2)     (y)

return : Point2D : lines cut each other at Point (non parallel)
	 null    : lines are parallel
			   t = square of distance between the parallels
*/
		{
			Pnt p = s1 - s0;

			double det = dir0.VectorProduct(dir1);

			if (Math.Abs(det) > _epsi * (Math.Abs(s0.X) + Math.Abs(s1.X) +
			                             Math.Abs(s0.Y) + Math.Abs(s1.Y)) / 1000.0)
			{
				f = p.VectorProduct(dir1) / det;
				return (Pnt2D) (s0 + f * dir0);
			}

			// parallel lines 
			det = dir0.VectorProduct(p);
			f = det * det / dir0.OrigDist2(2);
			return null;
		}

		[CanBeNull]
		public static Pnt2D OutCircleCenter([NotNull] Pnt p0,
		                                    [NotNull] Pnt p1,
		                                    [NotNull] Pnt p2)
		{
			Pnt s0 = 0.5 * (p0 + p1);
			Pnt dir0 = p1 - p0;
			Pnt s1 = 0.5 * (p1 + p2);
			Pnt dir1 = p2 - p1;

			return CutDirDir(s0,
			                 new Pnt2D(dir0.Y, -dir0.X), s1,
			                 new Pnt2D(dir1.Y, -dir1.X), out double _);
		}

		public static double TriArea([NotNull] Pnt2D p0,
		                             [NotNull] Pnt2D p1,
		                             [NotNull] Pnt2D p2)
		{
			var d0 = (Pnt2D) (p1 - p0);
			var d1 = (Pnt2D) (p2 - p0);

			return d0.VectorProduct(d1);
		}

		/// <summary>
		/// Calculates the cross product (vector product) of two vectors.
		/// </summary>
		/// <param name="u">The first vector.</param>
		/// <param name="v">The second vector.</param>
		/// <returns></returns>
		public static Vector CrossProduct(Vector u, Vector v)
		{
			double uX = u[0];
			double uY = u[1];
			double uZ = u[2];

			double vX = v[0];
			double vY = v[1];
			double vZ = v[2];

			return CrossProduct(uX, uY, uZ, vX, vY, vZ);
		}

		public static Vector CrossProduct(double uX, double uY, double uZ,
		                                  double vX, double vY, double vZ)
		{
			double x = uY * vZ - uZ * vY;
			double y = uZ * vX - uX * vZ;
			double z = uX * vY - uY * vX;

			var result = new Vector(new[] {x, y, z});

			return result;
		}

		/// <summary>
		/// The dot product (scalar product) of u and v.
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public static double DotProduct(Pnt3D u, Vector v)
		{
			Assert.AreEqual(u.Dimension, v.Dimension,
			                "Dimensions of input are not equal");

			return DotProduct(u[0], u[1], u[2], v[0], v[1], v[2]);
		}

		/// <summary>
		/// The dot product (scalar product) of u and v.
		/// </summary>
		/// <param name="u"></param>
		/// <param name="vX">The x component of v</param>
		/// <param name="vY">The y component of v</param>
		/// <param name="vZ">The z component of v</param>
		/// <returns></returns>
		public static double DotProduct(Pnt3D u, double vX, double vY, double vZ)
		{
			return u.X * vX + u.Y * vY + u.Z * vZ;
		}

		/// <summary>
		/// The dot product (scalar product) of u and v.
		/// </summary>
		/// <param name="uX"></param>
		/// <param name="uY"></param>
		/// <param name="uZ"></param>
		/// <param name="vX"></param>
		/// <param name="vY"></param>
		/// <param name="vZ"></param>
		/// <returns></returns>
		public static double DotProduct(double uX, double uY, double uZ,
		                                double vX, double vY, double vZ)
		{
			return uX * vX + uY * vY + uZ * vZ;
		}

		/// <summary>
		/// Returns the angle in radians at point b between the vectors ba and bc.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static double GetAngle3DInRad([NotNull] Pnt3D a,
		                                     [NotNull] Pnt3D b,
		                                     [NotNull] Pnt3D c)
		{
			// Get the 2 vectors
			double v1X = a.X - b.X;
			double v1Y = a.Y - b.Y;
			double v1Z = a.Z - b.Z;

			double v2X = c.X - b.X;
			double v2Y = c.Y - b.Y;
			double v2Z = c.Z - b.Z;

			return GetAngle3DInRad(v1X, v1Y, v1Z, v2X, v2Y, v2Z);
		}

		/// <summary>
		/// Returns the angle in radians at point b between the vectors ba and bc.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static double GetAngle2DInRad([NotNull] Pnt3D a,
		                                     [NotNull] Pnt3D b,
		                                     [NotNull] Pnt3D c)
		{
			// Get the 2 vectors
			double v1X = a.X - b.X;
			double v1Y = a.Y - b.Y;
			double v1Z = 0;

			double v2X = c.X - b.X;
			double v2Y = c.Y - b.Y;
			double v2Z = 0;

			return GetAngle3DInRad(v1X, v1Y, v1Z, v2X, v2Y, v2Z);
		}

		/// <summary>
		/// Returns the angle in radians between the vectors v1 and v2.
		/// </summary>
		/// <returns></returns>
		public static double GetAngle3DInRad(double v1X, double v1Y, double v1Z,
		                                     double v2X, double v2Y, double v2Z)
		{
			// normalize the vectors
			double v1Magnitude = Math.Sqrt(v1X * v1X + v1Y * v1Y + v1Z * v1Z);
			double v2Magnitude = Math.Sqrt(v2X * v2X + v2Y * v2Y + v2Z * v2Z);

			Assert.True(v1Magnitude > 0, "Vector ba has zero length.");
			Assert.True(v2Magnitude > 0, "Vector bc has zero length.");

			double v1NormX = v1X / v1Magnitude;
			double v1NormY = v1Y / v1Magnitude;
			double v1NormZ = v1Z / v1Magnitude;

			double v2NormX = v2X / v2Magnitude;
			double v2NormY = v2Y / v2Magnitude;
			double v2NormZ = v2Z / v2Magnitude;

			// scalar product:
			double dotProd = DotProduct(v1NormX, v1NormY, v1NormZ, v2NormX, v2NormY, v2NormZ);

			// account for floating-point issues, avoid NaN result from arc-cos
			if (dotProd < -1)
			{
				dotProd = -1;
			}

			if (dotProd > 1)
			{
				dotProd = 1;
			}

			return Math.Acos(dotProd);
		}

		/// <summary>
		/// Gets the change in direction between the baseLine and the compareLine between
		/// -PI (180 degrees left) and +PI (180 degrees right). However, an exact 180 direction
		/// change results in null.
		/// </summary>
		/// <param name="baseLine"></param>
		/// <param name="compareLine"></param>
		/// <returns></returns>
		public static double? GetDirectionChange(Line3D baseLine, Line3D compareLine)
		{
			double angleDifference = baseLine.GetDirectionAngleXY() -
			                         compareLine.GetDirectionAngleXY();

			// Normalize to -PI .. +PI
			if (angleDifference <= -Math.PI)
			{
				angleDifference += 2 * Math.PI;
			}
			else if (angleDifference > Math.PI)
			{
				angleDifference -= 2 * Math.PI;
			}

			// exclude 180-degree turns
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(baseLine.XMax);

			if (MathUtils.AreEqual(angleDifference, -Math.PI, epsilon) ||
			    MathUtils.AreEqual(angleDifference, Math.PI, epsilon))
			{
				return null;
			}

			return angleDifference;
		}

		/// <summary>
		/// Returns the azimuth of the line from p1 to p2
		/// </summary>
		/// <param name="p1X"></param>
		/// <param name="p1Y"></param>
		/// <param name="p2X"></param>
		/// <param name="p2Y"></param>
		/// <returns></returns>
		public static double GetAzimuth(double p1X, double p1Y,
		                                double p2X, double p2Y)
		{
			double x = p2X - p1X;
			double y = p2Y - p1Y;

			int sign = x > 0
				           ? -1
				           : 1;

			double d2 = x * x + y * y;

			double d = Math.Sqrt(d2);

			return sign *
			       Math.Acos(d * y / d2);
		}

		/// <summary>
		/// Rotate the provided points about the X axis by 90 degrees.
		/// </summary>
		/// <param name="points">The points to rotate.</param>
		/// <param name="copy">Whether the provided points should be copied or the coordinate
		/// values of the provided points should be changed directly.</param>
		/// <returns></returns>
		public static IEnumerable<Pnt3D> RotateX90(IEnumerable<Pnt3D> points,
		                                           bool copy = false)
		{
			foreach (Pnt3D point in points)
			{
				Pnt3D resultPoint = copy ? (Pnt3D) point.Clone() : point;

				double origY = resultPoint.Y;
				resultPoint.Y = resultPoint.Z;
				resultPoint.Z = -origY;

				yield return resultPoint;
			}
		}

		/// <summary>
		/// Rotate the provided points about the X axis by 90 degrees in the opposite direction. 
		/// </summary>
		/// <param name="points">The points to rotate.</param>
		/// <param name="copy">Whether the provided points should be copied or the coordinate
		/// values of the provided points should be changed directly.</param>
		/// <returns></returns>
		public static IEnumerable<Pnt3D> RotateX90Back(IEnumerable<Pnt3D> points,
		                                               bool copy = false)
		{
			foreach (Pnt3D point in points)
			{
				Pnt3D resultPoint = copy ? (Pnt3D) point.Clone() : point;

				double origY = resultPoint.Y;
				resultPoint.Y = -resultPoint.Z;
				resultPoint.Z = origY;

				yield return resultPoint;
			}
		}

		/// <summary>
		/// Rotate the provided points about the Y axis by 90 degrees.
		/// </summary>
		/// <param name="points">The points to rotate.</param>
		/// <param name="copy">Whether the provided points should be copied or the coordinate
		/// values of the provided points should be changed directly.</param>
		/// <returns></returns>
		public static IEnumerable<Pnt3D> RotateY90(IEnumerable<Pnt3D> points,
		                                           bool copy = false)
		{
			foreach (Pnt3D point in points)
			{
				Pnt3D resultPoint = copy ? (Pnt3D) point.Clone() : point;

				double origX = resultPoint.X;
				resultPoint.X = resultPoint.Z;
				resultPoint.Z = -origX;

				yield return resultPoint;
			}
		}

		/// <summary>
		/// Rotate the provided points about the Y axis by 90 degrees in the opposite direction. 
		/// </summary>
		/// <param name="points">The points to rotate.</param>
		/// <param name="copy">Whether the provided points should be copied or the coordinate
		/// values of the provided points should be changed directly.</param>
		/// <returns></returns>
		public static IEnumerable<Pnt3D> RotateY90Back(IEnumerable<Pnt3D> points,
		                                               bool copy = false)
		{
			foreach (Pnt3D point in points)
			{
				Pnt3D resultPoint = copy ? (Pnt3D) point.Clone() : point;

				double origX = resultPoint.X;
				resultPoint.X = -resultPoint.Z;
				resultPoint.Z = origX;

				yield return resultPoint;
			}
		}

		public static IEnumerable<Pnt3D> Move(IEnumerable<Pnt3D> points,
		                                      double dX, double dY, double dZ,
		                                      bool copy = false)
		{
			foreach (Pnt3D point in points)
			{
				if (copy)
				{
					yield return new Pnt3D(point.X + dX, point.Y + dY, point.Z + dZ);
				}
				else
				{
					point.X += dX;
					point.Y += dY;
					point.Z += dZ;

					yield return point;
				}
			}
		}

		/// <summary>
		/// Returns - larger 0 for testPoint left of the line from lineStart to lineEnd
		///         - 0 for testPoint on the line
		///         - smaller 0 for test point right of the line
		/// </summary>
		/// <param name="lineStart"></param>
		/// <param name="lineEnd"></param>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		public static double IsLeftXY(IPnt lineStart, IPnt lineEnd, IPnt testPoint)
		{
			return (lineEnd.X - lineStart.X) * (testPoint.Y - lineStart.Y)
			       - (testPoint.X - lineStart.X) * (lineEnd.Y - lineStart.Y);
		}

		/// <summary>
		/// Returns a positive number for clockwise, a negative number for anti-clockwise oriented rings.
		/// </summary>
		/// <param name="ringVertices"></param>
		/// <returns></returns>
		public static double GetOrientation([NotNull] IList<Pnt3D> ringVertices)
		{
			int n = ringVertices.Count - 1;

			// first find rightmost lowest vertex of the polygon
			var rmin = 0;
			double xmin = ringVertices[0].X;
			double ymin = ringVertices[0].Y;

			for (var i = 1; i < n; i++)
			{
				if (ringVertices[i].Y > ymin)
					continue;

				if (MathUtils.AreEqual(ringVertices[i].Y, ymin))
				{
					// just as low
					if (ringVertices[i].X < xmin) // and to left
						continue;
				}

				rmin = i; // a new rightmost lowest vertex
				xmin = ringVertices[i].X;
				ymin = ringVertices[i].Y;
			}

			// test orientation at the rmin vertex
			// ccw <=> the edge leaving V[rmin] is left of the entering edge
			double ogcResult = rmin == 0
				                   ? IsLeftXY(ringVertices[n - 1], ringVertices[0],
				                              ringVertices[1])
				                   : IsLeftXY(ringVertices[rmin - 1], ringVertices[rmin],
				                              ringVertices[rmin + 1]);

			return ogcResult * -1;
		}

		/// <summary>
		/// Adjusts the given co-planarity tolerance for a plane based on a z and xy resolution.
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="coplanarityTolerance"></param>
		/// <param name="zResolution"></param>
		/// <param name="xyResolution"></param>
		/// <returns></returns>
		public static double AdjustCoplanarityTolerance([NotNull] Plane3D plane,
		                                                double coplanarityTolerance,
		                                                double zResolution,
		                                                double xyResolution)
		{
			if (coplanarityTolerance >= zResolution + xyResolution)
			{
				// value is large enough, use as is
				return coplanarityTolerance;
			}

			var normal = plane.GetUnitNormal();

			double dx = normal.X * xyResolution;
			double dy = normal.Y * xyResolution;
			double dz = normal[2] * zResolution;

			var minDistance = Math.Sqrt(dx * dx + dy * dy + dz * dz);

			return Math.Max(minDistance, coplanarityTolerance);
		}

		public static Pnt2D GetCentroid2D(IBoundedXY bounds)
		{
			return new Pnt2D((bounds.XMax + bounds.XMin) / 2, (bounds.YMax + bounds.YMin) / 2);
		}

		public static double GetArea2D([NotNull] IList<Pnt3D> ringVertices)
		{
			double area = 0;
			int i, j, k; // indices

			int n = ringVertices.Count - 1;

			if (n < 3)
				return 0; // a degenerate polygon

			// offset x to avoid losing precision unnecessarily:
			double xOffset = ringVertices[0].X;
			for (i = 1, j = 2, k = 0; i < n; i++, j++, k++)
			{
				area += (ringVertices[i].X - xOffset) *
				        (ringVertices[j].Y - ringVertices[k].Y);
			}

			area += (ringVertices[n].X - xOffset) *
			        (ringVertices[1].Y - ringVertices[n - 1].Y);
			// wrap-around term

			double ogcResult = area / 2.0;

			return ogcResult * -1;
		}

		/// <summary>
		/// Calculates the area of the specified closed ring using the fast method
		/// described in http://geomalgorithms.com/a01-_area.html#2D%20Polygons.
		/// Copyright notice:
		/// Copyright 2000 softSurfer, 2012 Dan Sunday
		/// This code may be freely used and modified for any purpose
		/// providing that this copyright notice is included with it.
		/// iSurfer.org makes no warranty for this code, and cannot be held
		/// liable for any real or imagined damage resulting from its use.
		/// Users of this code must verify correctness for their application.
		/// </summary>
		/// <param name="vertices">The list of vertices with the last vertex equal the first.</param>
		/// <param name="normal">The plane normal of the ring.</param>
		/// <returns></returns>
		public static double GetArea3D([NotNull] IList<Pnt3D> vertices,
		                               [NotNull] Pnt3D normal)
		{
			double area = 0;
			int i, j, k; // loop indices

			int n = vertices.Count - 1;

			if (n < 3)
				return 0; // a degenerate polygon

			// select largest abs coordinate of normal to ignore for projection
			double ax = Math.Abs(normal.X);
			double ay = Math.Abs(normal.Y);
			double az = Math.Abs(normal.Z);

			var coord = 3;
			if (ax > ay)
			{
				if (ax > az)
					coord = 1; // ignore x-coord
			}
			else if (ay > az)
				coord = 2; // ignore y-coord

			// compute area of the 2D projection
			switch (coord)
			{
				case 1:
					for (i = 1, j = 2, k = 0; i < n; i++, j++, k++)
						area += vertices[i].Y * (vertices[j].Z - vertices[k].Z);

					break;
				case 2:
					for (i = 1, j = 2, k = 0; i < n; i++, j++, k++)
						area += vertices[i].Z * (vertices[j].X - vertices[k].X);

					break;
				case 3:
					for (i = 1, j = 2, k = 0; i < n; i++, j++, k++)
						area += vertices[i].X * (vertices[j].Y - vertices[k].Y);

					break;
			}

			switch (coord)
			{
				// wrap-around term
				case 1:
					area += vertices[n].Y * (vertices[1].Z - vertices[n - 1].Z);
					break;
				case 2:
					area += vertices[n].Z * (vertices[1].X - vertices[n - 1].X);
					break;
				case 3:
					area += vertices[n].X * (vertices[1].Y - vertices[n - 1].Y);
					break;
			}

			// scale to get area before projection
			double an = Math.Sqrt(ax * ax + ay * ay + az * az);
			switch (coord)
			{
				case 1:
					area *= an / (2 * normal.X);
					break;
				case 2:
					area *= an / (2 * normal.Y);
					break;
				case 3:
					area *= an / (2 * normal.Z);
					break;
			}

			return area;
		}

		public static double GetDistanceSquaredXYZ([NotNull] IPnt point1, [NotNull] IPnt point2)
		{
			double dx = point2.X - point1.X;

			double result = dx * dx;

			double dy = point2.Y - point1.Y;

			result += dy * dy;

			if (point1 is Pnt3D pnt3d1 && point2 is Pnt3D pnt3d2)
			{
				double dz = pnt3d2.Z - pnt3d1.Z;

				if (! double.IsNaN(dz))
				{
					result += dz * dz;
				}
			}

			return result;
		}

		public static double GetDistanceXYZ([NotNull] IPnt point1, [NotNull] IPnt point2)
		{
			double dx = point2.X - point1.X;

			double result = dx * dx;

			double dy = point2.Y - point1.Y;

			result += dy * dy;

			if (point1 is Pnt3D pnt3d1 && point2 is Pnt3D pnt3d2)
			{
				double dz = pnt3d2.Z - pnt3d1.Z;

				if (! double.IsNaN(dz))
				{
					result += dz * dz;
				}
			}

			return result;
		}

		public static double GetDistanceSquaredXY([NotNull] ICoordinates point1, [NotNull] ICoordinates point2)
		{
			double dx = point2.X - point1.X;

			double result = dx * dx;

			double dy = point2.Y - point1.Y;

			result += dy * dy;

			return result;
		}

		public static double GetDistanceXY([NotNull] ICoordinates point1, [NotNull] ICoordinates point2)
		{
			double distanceSquared = GetDistanceSquaredXY(point1, point2);

			return Math.Sqrt(distanceSquared);
		}

		public static IEnumerable<Linestring> GetLinestrings([NotNull] ISegmentList segmentList)
		{
			for (int i = 0; i < segmentList.PartCount; i++)
			{
				yield return segmentList.GetPart(i);
			}
		}

		public static IEnumerable<SegmentIndex> GetShortSegmentIndexes(
			ISegmentList segmentList,
			double minimumSegmentLength,
			bool use3dLength,
			[CanBeNull] IBoundedXY perimeter)
		{
			for (int partIdx = 0; partIdx < segmentList.PartCount; partIdx++)
			{
				Linestring linestring = segmentList.GetPart(partIdx);

				foreach (int localIndex in GetShortSegmentIndexes(
					         linestring, minimumSegmentLength, use3dLength, perimeter))
				{
					yield return new SegmentIndex(partIdx, localIndex);
				}
			}
		}

		public static IEnumerable<int> GetShortSegmentIndexes(
			Linestring linestring,
			double minimumSegmentLength,
			bool use3dLength,
			[CanBeNull] IBoundedXY perimeter)
		{
			double minLengthSquared = minimumSegmentLength * minimumSegmentLength;

			int index = 0;
			foreach (Line3D segment in linestring.Segments)
			{
				if (perimeter == null ||
				    ! GeomRelationUtils.AreBoundsDisjoint(segment, perimeter, 0))
				{
					double lengthSquared = segment.Length2DSquared;

					if (use3dLength)
					{
						double dZ = segment.DeltaZ;

						Assert.False(double.IsNaN(dZ), "Segment has NaN (Z) coordinates");

						lengthSquared += dZ * dZ;
					}

					if (lengthSquared < minLengthSquared)
					{
						yield return index;
					}
				}

				index++;
			}
		}

		public static MultiPolycurve Generalize([NotNull] MultiPolycurve multiPolycurve,
		                                        double maxDeviation,
		                                        bool inXY)
		{
			var result = multiPolycurve.GetLinestrings()
			                           .Select(linestring =>
				                                   Generalize(linestring, maxDeviation, inXY))
			                           .ToList();

			return new MultiPolycurve(result);
		}

		public static Linestring Generalize(Linestring linestring, double maxDeviation, bool inXY)
		{
			var pointList = linestring.GetPoints().ToList();

			// NOTE: The standard Ramer-Douglas-Peucker is sensitive to the point order (and the start point)
			//       Make sure that at least for identical stretches the result is always the same.
			bool reversed = false;
			if (linestring.IsClosed)
			{
				if (! linestring.ClockwiseOriented == true)
				{
					pointList.Reverse();
					reversed = true;
				}
			}
			else if (! IsMoreSouthEast(linestring.StartPoint, linestring.EndPoint))
			{
				pointList.Reverse();
				reversed = true;
			}

			var result = new List<Pnt3D>();
			RamerDouglasPeucker(pointList, maxDeviation, result, inXY);

			if (reversed)
			{
				result.Reverse();
			}

			// For closed rings: requires extra check for start/end point:
			if (WeedFromToPoint(result, maxDeviation, inXY))
			{
				// Drop last point
				result.RemoveAt(result.Count - 1);

				// Update first to re-close the ring:
				result[0] = result[result.Count - 1].ClonePnt3D();
			}

			return new Linestring(result);
		}

		public static void RamerDouglasPeucker([NotNull] IList<Pnt3D> pointList,
		                                       double maxAllowedDeviation,
		                                       [NotNull] List<Pnt3D> result,
		                                       bool inXY)
		{
			if (pointList.Count < 2)
			{
				throw new ArgumentOutOfRangeException(nameof(pointList),
				                                      $"Too few points ({pointList.Count})");
			}

			// Ensure all points have Z values if 3D distance is used
			if (! inXY && pointList.Any(p => double.IsNaN(p.Z)))
			{
				throw new ArgumentException(
					"Cannot generalize points without Z using 3D distance.");
			}

			// Find the point with the maximum perpendicular distance to the base line
			double maxDist = 0.0;
			int maxDistIdx = 0;
			int lastIdx = pointList.Count - 1;

			for (int i = 1; i < lastIdx; ++i)
			{
				var baseLine = new Line3D(pointList[0], pointList[lastIdx]);

				double dist = baseLine.GetDistancePerpendicular(pointList[i], inXY);

				if (dist < maxDist)
				{
					continue;
				}

				if (dist > maxDist)
				{
					maxDistIdx = i;
					maxDist = dist;
				}
				else
				{
					//// Equal distance: use more north-western point
					//if (pointList[i].X > pointList[maxDistIdx].X)
					//{
					//	maxDistIdx = i;
					//	maxDist = dist;
					//}
					//else if (pointList[i].X < pointList[maxDistIdx].X)
					//{
					//	continue;
					//}

					//// same x value:
					//if (pointList[i].Y > pointList[maxDistIdx].Y)
					//{
					//	maxDistIdx = i;
					//	maxDist = dist;
					//}
				}
			}

			// If maxDist is greater than maxAllowedDeviation: recursively generalize
			if (maxDist > maxAllowedDeviation)
			{
				// Split the line sequence:
				List<Pnt3D> recResults1 = new List<Pnt3D>();
				List<Pnt3D> recResults2 = new List<Pnt3D>();
				List<Pnt3D> firstSequence = pointList.Take(maxDistIdx + 1).ToList();
				List<Pnt3D> lastSequence = pointList.Skip(maxDistIdx).ToList();
				RamerDouglasPeucker(firstSequence, maxAllowedDeviation, recResults1, inXY);
				RamerDouglasPeucker(lastSequence, maxAllowedDeviation, recResults2, inXY);

				// Add to final result
				result.AddRange(recResults1.Take(recResults1.Count - 1));
				result.AddRange(recResults2);
				Assert.False(result.Count < 0, "Unexpected result point count");
			}
			else
			{
				// All points are within max deviation: Only start and end point
				result.Clear();
				result.Add(pointList[0]);
				result.Add(pointList[pointList.Count - 1]);
			}
		}

		public static bool IsVertical([NotNull] ISegmentList segmentList, double tolerance)
		{
			for (int i = 0; i < segmentList.PartCount; i++)
			{
				Linestring linestring = segmentList.GetPart(i);

				if (! linestring.IsVerticalRing(tolerance))
				{
					return false;
				}
			}

			return true;
		}

		public static byte[] ToWkb([NotNull] ISegmentList geometry)
		{
			WkbGeomWriter geometryWriter = new WkbGeomWriter();

			return geometryWriter.WriteGeometry(geometry);
		}

		public static byte[] ToWkb<T>([NotNull] Multipoint<T> multipoint) where T : IPnt
		{
			WkbGeomWriter geometryWriter = new WkbGeomWriter();

			Ordinates ordinates = typeof(T) == typeof(Pnt3D) ? Ordinates.Xyz : Ordinates.Xy;

			return geometryWriter.WriteMultipoint(multipoint, ordinates);
		}

		public static byte[] ToWkb<T>([NotNull] T point) where T : IPnt
		{
			WkbGeomWriter geometryWriter = new WkbGeomWriter();

			Ordinates ordinates = typeof(T) == typeof(Pnt3D) ? Ordinates.Xyz : Ordinates.Xy;

			return geometryWriter.WritePoint(point, ordinates);
		}

		public static IBoundedXY FromWkb([NotNull] byte[] wkb,
		                                 out WkbGeometryType wkbType)
		{
			WkbGeomReader reader = new WkbGeomReader();

			IBoundedXY geometry = reader.ReadGeometry(new MemoryStream(wkb), out wkbType);

			return geometry;
		}

		public static void ToWkbFile([NotNull] ISegmentList geometry,
		                             [NotNull] string filePath)
		{
			byte[] bytes = ToWkb(geometry);

			File.WriteAllBytes(filePath, bytes);
		}

		public static void ToWkbFile<T>([NotNull] Multipoint<T> multipoint,
		                                [NotNull] string filePath) where T : IPnt
		{
			byte[] bytes = ToWkb(multipoint);

			File.WriteAllBytes(filePath, bytes);
		}

		public static void ToWkbFile<T>([NotNull] T point,
		                                [NotNull] string filePath) where T : IPnt
		{
			byte[] bytes = ToWkb(point);

			File.WriteAllBytes(filePath, bytes);
		}

		public static IBoundedXY FromWkbFile(string filePath,
		                                     out WkbGeometryType wkbType)
		{
			byte[] bytes = File.ReadAllBytes(filePath);

			return FromWkb(bytes, out wkbType);
		}

		private static bool IsMoreSouthEast(Pnt3D point1, Pnt3D point2)
		{
			if (point1.Y < point2.Y)
			{
				// It's more south
				return true;
			}

			if (point1.Y > point2.Y)
			{
				// It's more north
				return false;
			}

			// Same Y coordinate - Check X:
			if (point1.X < point2.X)
			{
				// It's more east:
				return true;
			}

			return false;
		}

		private static bool WeedFromToPoint([NotNull] List<Pnt3D> pointList,
		                                    double maxAllowedDeviation,
		                                    bool inXY)
		{
			if (pointList.Count <= 3)
			{
				return false;
			}

			int lastIdx = pointList.Count - 1;

			if (! pointList[0].Equals(pointList[lastIdx]))
			{
				return false;
			}

			var baseLine = new Line3D(pointList[lastIdx - 1], pointList[1]);

			double dist = baseLine.GetDistancePerpendicular(pointList[0], inXY);

			return dist < maxAllowedDeviation;
		}
	}
}

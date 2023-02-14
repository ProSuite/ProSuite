using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class Plane3DTest
	{
		[Test]
		public void CanCreatePlane3DFrom3Points()
		{
			var points = new List<Pnt3D>
			             {
				             new Pnt3D(2577627.794, 1183434.723, 635.800000000003),
				             new Pnt3D(2577627.643, 1183437.291, 635.804999999993),
				             new Pnt3D(2577629.794, 1183438.723, 635.800000000003)
			             };

			Plane3D plane = Plane3D.FitPlane(points, isRing: false);

			Assert.True(plane.IsDefined);
			Assert.False(MathUtils.AreEqual(0, plane.LengthOfNormalSquared, plane.Epsilon));

			points.Add(points[0]);
			plane = Plane3D.FitPlane(points, isRing: true);

			Assert.True(plane.IsDefined);
			Assert.False(MathUtils.AreEqual(0, plane.LengthOfNormalSquared, plane.Epsilon));
		}

		[Test]
		public void CanCreateUndefinedPlaneFromCollinearPoints()
		{
			// first and last point is identical:
			var points = new List<Pnt3D>
			             {
				             new Pnt3D(2577627.794, 1183434.723, 635.800000000003),
				             new Pnt3D(2577627.643, 1183437.291, 635.804999999993),
				             new Pnt3D(2577627.794, 1183434.723, 635.800000000003)
			             };

			Plane3D plane = Plane3D.FitPlane(points, isRing: false);

			Assert.False(plane.IsDefined);
			Assert.AreEqual(0, plane.LengthOfNormalSquared, plane.Epsilon);

			// generally collinear:
			points = new List<Pnt3D>
			         {
				         new Pnt3D(2577627.794, 1183434.723, 635.800000000003),
				         new Pnt3D(2577629.643, 1183437.291, 635.804999999993)
			         };

			Pnt3D pnt3D = points[0] + (points[1] - points[0]) * 3.5;
			points.Add(pnt3D);

			plane = Plane3D.FitPlane(points, isRing: false);

			Assert.False(plane.IsDefined);
			Assert.AreEqual(0, plane.LengthOfNormalSquared, plane.Epsilon);

			points.Add(points[0]);
			plane = Plane3D.FitPlane(points, isRing: true);

			Assert.False(plane.IsDefined);
			Assert.AreEqual(0, plane.LengthOfNormalSquared, plane.Epsilon);
		}

		[Test]
		public void CanFitHorizontalPlane3D()
		{
			var x = new double[] {0, 0, 0, 1, 1, 1, 2, 2, 2};
			var y = new double[] {1, 2, 3, 1, 2, 3, 1, 2, 3};
			var z = new double[] {1, 1, 1, 1, 1, 1, 1, 1, 1};

			Plane3D plane = FitPlane3D(x, y, z);

			Assert.AreEqual(1, plane.GetZ(-1000, -1000));
			Assert.AreEqual(1, plane.GetZ(1000, 1000));
			Assert.AreEqual(1, plane.GetDistanceAbs(1000, 1000, 0));
			Assert.AreEqual(1, plane.GetUnitNormal().LengthSquared);
		}

		[Test]
		public void CanGetDistanceToVerticalPlane3D()
		{
			List<Pnt3D> points = GetVerticalTrianglePoints();

			Plane3D plane = Plane3D.FitPlane(points);

			Console.WriteLine(@"A: " + plane.A);
			Console.WriteLine(@"B: " + plane.B);
			Console.WriteLine(@"C: " + plane.C);
			Console.WriteLine(@"D: " + plane.D);

			Assert.AreEqual(0, plane.C, plane.Epsilon); // vertical
			Assert.AreEqual(1, plane.GetUnitNormal().LengthSquared);

			Assert.AreEqual(0.89606641473951276, plane.GetUnitNormal().X, plane.Epsilon);
			Assert.AreEqual(0.44392001574143475, plane.GetUnitNormal().Y, plane.Epsilon);

			foreach (Pnt3D point in points)
			{
				double distance = plane.GetDistanceAbs(point.X, point.Y, point.Z);
				Console.WriteLine(@"{0}: {1}", point, distance);
				Assert.AreEqual(0, distance, plane.Epsilon);
			}

			Assert.Catch<InvalidOperationException>(() => plane.GetZ(100, 100));
		}

		[Test]
		public void CanGetDistanceToNearlyVerticalPlane3D()
		{
			List<Pnt3D> points = GetVerticalTrianglePoints();

			points[0].Y += 0.01;
			Plane3D plane = Plane3D.FitPlane(points);

			foreach (Pnt3D point in points)
			{
				double distance = plane.GetDistanceAbs(point.X, point.Y, point.Z);
				Console.WriteLine(@"{0}: {1}", point, distance);
				Assert.AreEqual(0, distance, plane.Epsilon);
			}

			double definedZ = plane.GetZ(100, 100);

			Assert.AreEqual(0, plane.GetDistanceAbs(100, 100, definedZ), plane.Epsilon);
		}

		[Test]
		public void CanGetZOfNearlyHorizontalPlane3D()
		{
			var points = new List<Pnt3D>();

			points.Add(new Pnt3D {X = 2723729.625, Y = 1251631.61625, Z = 601});
			points.Add(new Pnt3D {X = 2723531.44625, Y = 1251727.94, Z = 601});
			points.Add(new Pnt3D {X = 2723633.2675, Y = 1251824.26375, Z = 601});

			points[0].Z += 0.001;

			points.Add(points[0]);

			Plane3D plane = Plane3D.FitPlane(points, true);

			foreach (Pnt3D point in points)
			{
				double z = plane.GetZ(point.X, point.Y);
				Console.WriteLine(@"{0}: {1}", point, z);
				Assert.AreEqual(point.Z, z, plane.Epsilon);
			}

			const double farAwayX = -1000000.12345;
			const double farAwayY = -1000000.6789;
			double z0 = plane.GetZ(farAwayX, farAwayY);
			Assert.AreEqual(0, plane.GetDistanceSigned(farAwayX, farAwayY, z0), plane.Epsilon);
		}

		[Test]
		public void CanGetOrientedNormalFromRingPoints()
		{
			// positive (clockwise) orientation;
			var x = new double[] {0, 0, 1, 1, 0};
			var y = new double[] {0, 1, 1, 0, 0};
			var z = new double[] {0, 0, 0, 0, 0};

			Plane3D plane = FitPlane3D(x, y, z, true);

			Assert.True(plane.Normal[2] < 0);

			x = x.Reverse().ToArray();
			y = y.Reverse().ToArray();
			z = z.Reverse().ToArray();

			plane = FitPlane3D(x, y, z, true);

			Assert.True(plane.Normal[2] > 0);
		}

		[Test]
		public void CanDeterminePlaneCoincidenceHorizontal()
		{
			var x = new double[] {0, 0, 1, 1, 0};
			var y = new double[] {0, 1, 1, 0, 0};
			var z = new double[] {7, 7, 7, 7, 7};

			Plane3D plane1 = FitPlane3D(x, y, z, true);

			x = new double[] {460, 24350, 23451, 451, 2210};
			y = new double[] {65476570, 351, 2341, 2340, 98760};
			z = new double[] {7, 7, 7, 7, 7};

			Plane3D plane2 = FitPlane3D(x, y, z, true);

			Assert.False(plane1.Equals(plane2));
			Assert.True(plane1.IsCoincident(plane2));
		}

		[Test]
		public void CanDeterminePlaneCoincidence()
		{
			var x = new double[] {0, 0, 1, 1, 0};
			var y = new double[] {0, 1, 1, 0, 0};
			var z = new double[] {0, 10, 10, 0, 0};

			Plane3D plane1 = FitPlane3D(x, y, z, true);

			var points2 = new List<Pnt3D>();
			points2.Add(new Pnt3D(234, 567, plane1.GetZ(234, 567)));
			points2.Add(new Pnt3D(987, 654, plane1.GetZ(987, 654)));
			points2.Add(new Pnt3D(-432, 881, plane1.GetZ(432, 881)));

			Plane3D plane2 = Plane3D.FitPlane(points2);

			Assert.False(plane1.Equals(plane2));
			Assert.True(plane1.IsCoincident(plane2));
		}

		[Test]
		public void CanDeterminePlaneCoincidenceMirroredAt0()
		{
			var x = new double[] {0, 0, 1, 1, 0};
			var y = new double[] {0, 1, 1, 0, 0};
			var z = new double[] {10, 10, 10, 10, 10};

			Plane3D plane1 = FitPlane3D(x, y, z, true);

			x = new double[] {0, 0, 1, 1, 0};
			y = new double[] {0, 1, 1, 0, 0};
			z = new double[] {10, 10, 10, 10, 10};

			for (var i = 0; i < z.Length; i++)
			{
				z[i] = z[i] * -1;
			}

			x = x.Reverse().ToArray();
			y = y.Reverse().ToArray();

			Plane3D plane2 = FitPlane3D(x, y, z, true);

			Assert.False(plane1.Equals(plane2));
			Assert.False(plane1.IsCoincident(plane2));
		}

		[Test]
		public void CanDetermineInclinedPlaneCoincidenceMirroredAt0()
		{
			var result = new List<Pnt3D>();

			result.Add(new Pnt3D {X = 2723729.625, Y = 1251631.61625, Z = 601.388749999984});
			result.Add(new Pnt3D {X = 2723531.44625, Y = 1251727.94, Z = 615.443749999991});
			result.Add(new Pnt3D {X = 2723633.2675, Y = 1251824.26375, Z = 661.388749999984});

			result.Add(result[0]);

			Plane3D plane1 = Plane3D.FitPlane(result, true);

			for (var i = 0; i < result.Count; i++)
			{
				result[i] = result[i] * -1;
			}

			result.Reverse();

			Plane3D plane2 = Plane3D.FitPlane(result, true);

			Assert.False(plane1.Equals(plane2));
			Assert.True(plane1.IsParallel(plane2));
			Assert.False(plane1.IsCoincident(plane2));
		}

		[Test]
		public void CanKeepEpsilonAtReasonableLevel()
		{
			var result = new List<Pnt3D>();
			// points spaced by a few 100m
			result.Add(new Pnt3D {X = 2723729.625, Y = 1251631.61625, Z = 601.388749999984});
			result.Add(new Pnt3D {X = 2723531.44625, Y = 1251727.94, Z = 615.443749999991});
			result.Add(new Pnt3D {X = 2723633.2675, Y = 1251824.26375, Z = 661.388749999984});

			result.Add(result[0]);

			Plane3D plane1 = Plane3D.FitPlane(result, true);

			result.Reverse();

			Plane3D plane2 = Plane3D.FitPlane(result, true);

			Assert.Less(plane1.Epsilon, 0.5);

			Assert.True(plane1.IsParallel(plane2));

			// opposite direction normal:
			Assert.False(plane1.Equals(plane2));

			Assert.True(plane1.IsCoincident(plane2));
		}

		private static Plane3D FitPlane3D(double[] x, double[] y, double[] z,
		                                  bool isRing = false)
		{
			var points = new List<Pnt3D>();
			for (var i = 0; i < x.Length; i++)
			{
				points.Add(new Pnt3D(x[i], y[i], z[i]));
			}

			Plane3D plane = Plane3D.FitPlane(points, isRing);
			return plane;
		}

		private static List<Pnt3D> GetVerticalTrianglePoints()
		{
			var result = new List<Pnt3D>();

			result.Add(new Pnt3D {X = 2723629.625, Y = 1251831.61625, Z = 601.388749999984});
			result.Add(new Pnt3D {X = 2723631.44625, Y = 1251827.94, Z = 604.443749999991});
			result.Add(new Pnt3D {X = 2723633.2675, Y = 1251824.26375, Z = 601.388749999984});

			return result;
		}
	}
}

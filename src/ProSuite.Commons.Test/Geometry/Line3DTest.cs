using System;
using NUnit.Framework;
using ProSuite.Commons.Geometry;

namespace ProSuite.Commons.Test.Geometry
{
	[TestFixture]
	public class Line3DTest
	{
		[Test]
		public void CanDetermineIntersectionPointXY()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(10, 10, 0));
			var line2 = new Line3D(new Pnt3D(2, 8, 0), new Pnt3D(10, 1, 0));

			double line1Factor, line2Factor;
			Assert.True(
				line1.HasIntersectionPointXY(line2, 0, out line1Factor, out line2Factor));
			Assert.Greater(line1Factor, line2Factor);

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(10);
			Pnt3D pointAlong1 = line1.GetPointAlong(line1Factor, true);
			Pnt3D pointAlong2 = line2.GetPointAlong(line2Factor, true);

			Assert.True(pointAlong1.Equals(pointAlong2, epsilon));
		}

		[Test]
		public void CanDetermineIntersectionPointXYOutsideLineEnds()
		{
			var line1 = new Line3D(new Pnt3D(0, 6, 0), new Pnt3D(10, 10, 0));
			var line2 = new Line3D(new Pnt3D(0, 4, 0), new Pnt3D(10, 1, 0));

			double line1Factor, line2Factor;
			Assert.False(line1.HasIntersectionPointXY(line2, 2, out line1Factor,
			                                          out line2Factor));

			Pnt3D pointAlong1 = line1.GetPointAlong(line1Factor, true);

			Assert.Less(pointAlong1.X, -2);
		}

		[Test]
		public void CanDetermineDistancePerpendicular()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(10, 10, 0));
			var point = new Pnt3D(10, 0, 0);

			double d = line1.GetDistancePerpendicular(point);

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(10);

			double expectedDist2D = Math.Sqrt(100 + 100) / 2;

			Assert.AreEqual(expectedDist2D, d, epsilon);

			point = new Pnt3D(10, 0, 10);
			d = line1.GetDistancePerpendicular(point);
			Assert.AreEqual(Math.Sqrt(expectedDist2D * expectedDist2D + 100), d, epsilon);

			point = new Pnt3D(0, 0, 0);
			d = line1.GetDistancePerpendicular(point);
			Assert.AreEqual(0, d, epsilon);

			point = new Pnt3D(6, 6, 0);
			d = line1.GetDistancePerpendicular(point);
			Assert.AreEqual(0, d, epsilon);
		}

		[Test]
		public void CanDetermineDistanceAlong()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(10, 10, 0));
			var point = new Pnt3D(2, 2, 0);

			double d = line1.GetDistanceAlong(point);

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(10);

			double expectedDist2D = Math.Sqrt(4 + 4);

			Assert.AreEqual(expectedDist2D, d, epsilon);

			point = new Pnt3D(2, 2, 24);
			d = line1.GetDistancePerpendicular(point);

			Assert.AreEqual(24, d, epsilon);
		}

		[Test]
		public void CanDetermineIntersectionPointXYDifferentZ()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 7), new Pnt3D(10, 10, 7));
			var line2 = new Line3D(new Pnt3D(2, 8, 0), new Pnt3D(10, 1, 0));

			double line1Factor, line2Factor;
			Assert.True(
				line1.HasIntersectionPointXY(line2, 0, out line1Factor, out line2Factor));
			Assert.Greater(line1Factor, line2Factor);

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(10);
			Pnt3D pointAlong1 = line1.GetPointAlong(line1Factor, true);
			Pnt3D pointAlong2 = line2.GetPointAlong(line2Factor, true);

			Assert.True(pointAlong1.EqualsXY(pointAlong2, epsilon));

			Assert.AreEqual(7, pointAlong1.Z);
			Assert.AreEqual(0, pointAlong2.Z);
		}

		[Test]
		public void CanGetDistanceXYPerpendicularSigned()
		{
			var line1 = new Line3D(new Pnt3D(0, 0, 0), new Pnt3D(10, 10, 0));
			var point = new Pnt3D(2, 2, 0);

			double distanceAlong;
			Assert.AreEqual(0, line1.GetDistanceXYPerpendicularSigned(point));
			Assert.AreEqual(
				0, line1.GetDistanceXYPerpendicularSigned(point, out distanceAlong));
			Assert.AreEqual(0.2, distanceAlong);

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(5);

			// slightly on the left:
			point = new Pnt3D(5.0, 5.01, 17);
			double expectedOffset = Math.Sqrt(2) * 0.01 / 2;
			double expectedOffsetAlong = expectedOffset / line1.Length2D;

			Assert.AreEqual(expectedOffset, line1.GetDistanceXYPerpendicularSigned(point),
			                epsilon);
			Assert.AreEqual(expectedOffset,
			                line1.GetDistanceXYPerpendicularSigned(
				                point, out distanceAlong),
			                epsilon);
			Assert.AreEqual(0.5 + expectedOffsetAlong, distanceAlong, epsilon);

			// slightly on the right:
			point = new Pnt3D(5.0, 4.99, 17);
			Assert.AreEqual(-expectedOffset,
			                line1.GetDistanceXYPerpendicularSigned(point),
			                epsilon);
			Assert.AreEqual(-expectedOffset,
			                line1.GetDistanceXYPerpendicularSigned(
				                point, out distanceAlong),
			                epsilon);
			Assert.AreEqual(0.5 - expectedOffsetAlong, distanceAlong, epsilon);
		}
	}
}
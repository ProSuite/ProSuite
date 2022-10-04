using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class LinestringTest
	{
		[Test]
		public void CanGetPointsFromLinestring()
		{
			var startPoint = new Pnt3D(-5, -15, -25);
			var endPoint = new Pnt3D(10, 20, 30);

			var line1 = new Line3D(startPoint, endPoint);

			Linestring linestring = new Linestring(new List<Line3D> {line1});

			Pnt3D start = linestring.GetPoint3D(0);
			Assert.True(ReferenceEquals(startPoint, start));

			Pnt3D end = linestring.GetPoint3D(1);
			Assert.True(ReferenceEquals(endPoint, end));

			var list = linestring.GetPoints(0, 1).ToList();
			Assert.AreEqual(1, list.Count);
			Assert.True(ReferenceEquals(startPoint, list[0]));

			list = linestring.GetPoints(1, 1).ToList();
			Assert.AreEqual(1, list.Count);
			Assert.True(ReferenceEquals(endPoint, list[0]));

			list = linestring.GetPoints(0, 2).ToList();
			Assert.AreEqual(2, list.Count);
			Assert.True(ReferenceEquals(startPoint, list[0]));
			Assert.True(ReferenceEquals(endPoint, list[1]));

			// The same using default values
			list = linestring.GetPoints().ToList();
			Assert.AreEqual(2, list.Count);
			Assert.True(Equals(startPoint, list[0]));
			Assert.True(Equals(endPoint, list[1]));

			// The same with clone
			list = linestring.GetPoints(0, null, true).ToList();
			Assert.AreEqual(2, list.Count);
			Assert.False(ReferenceEquals(startPoint, list[0]));
			Assert.True(startPoint.Equals(list[0]));
			Assert.False(ReferenceEquals(endPoint, list[1]));
			Assert.True(endPoint.Equals(list[1]));

			var intermediatePoint = new Pnt3D(0, 0, 0);

			linestring = new Linestring(new[] {startPoint, intermediatePoint, endPoint});

			start = linestring.GetPoint3D(0);
			Assert.True(ReferenceEquals(startPoint, start));

			end = linestring.GetPoint3D(2);
			Assert.True(ReferenceEquals(endPoint, end));

			list = linestring.GetPoints(0, 1).ToList();
			Assert.AreEqual(1, list.Count);
			Assert.True(ReferenceEquals(startPoint, list[0]));

			list = linestring.GetPoints(2, 1).ToList();
			Assert.AreEqual(1, list.Count);
			Assert.True(ReferenceEquals(endPoint, list[0]));

			list = linestring.GetPoints(0, 2).ToList();
			Assert.AreEqual(2, list.Count);
			Assert.True(ReferenceEquals(startPoint, list[0]));
			Assert.True(ReferenceEquals(intermediatePoint, list[1]));

			list = linestring.GetPoints().ToList();
			Assert.AreEqual(3, list.Count);
			Assert.True(ReferenceEquals(startPoint, list[0]));
			Assert.True(ReferenceEquals(intermediatePoint, list[1]));
			Assert.True(ReferenceEquals(endPoint, list[2]));
		}

		[Test]
		public void CanGetPointsFromEmptyLinestring()
		{
			var startPoint = new Pnt3D(-5, -15, -25);
			var endPoint = new Pnt3D(10, 20, 30);

			var line1 = new Line3D(startPoint, endPoint);

			Linestring linestring = new Linestring(Array.Empty<Pnt3D>());

			List<Pnt3D> returnedPoints = linestring.GetPoints().ToList();

			Assert.AreEqual(0, returnedPoints.Count);
		}

		[Test]
		public void CanCalculateExtent()
		{
			var line1 = new Line3D(new Pnt3D(-5, -15, -25), new Pnt3D(10, 20, 30));

			Linestring linestring = new Linestring(new List<Line3D> {line1});

			// TODO: Fast Extent2D class that takes box as constructor parameter
			//Assert.AreEqual(line1.Extent, singleSegmentLinestring.Extent2D);
			Assert.AreEqual(line1.XMin, linestring.XMin);
			Assert.AreEqual(line1.YMin, linestring.YMin);

			Assert.AreEqual(line1.XMax, linestring.XMax);
			Assert.AreEqual(line1.YMax, linestring.YMax);

			var line2 = new Line3D(new Pnt3D(10, 20, 30), new Pnt3D(110, 120, 130));

			linestring = new Linestring(new List<Line3D> {line1, line2});

			// TODO: Line3D.Extent2D
			IBox expected = new Box(new Pnt2D(line1.StartPoint.X, line1.StartPoint.Y),
			                        new Pnt2D(line2.EndPoint.X, line2.EndPoint.Y));

			Assert.AreEqual(expected, linestring.Extent2D);

			linestring.ReverseOrientation();
			Assert.AreEqual(expected, linestring.Extent2D);

			linestring = new Linestring(linestring.Segments);
			Assert.AreEqual(expected, linestring.Extent2D);

			// Creation with points:
			linestring = new Linestring(linestring.GetPoints());
			Assert.AreEqual(expected, linestring.Extent2D);

			linestring =
				new Linestring(new List<Pnt3D> {line1.StartPoint, line1.EndPoint});

			Assert.AreEqual(line1.XMin, linestring.XMin);
			Assert.AreEqual(line1.YMin, linestring.YMin);

			Assert.AreEqual(line1.XMax, linestring.XMax);
			Assert.AreEqual(line1.YMax, linestring.YMax);
		}

		[Test]
		public void CanCalculateOrientation()
		{
			var ring1 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 9));
			ring1.Add(new Pnt3D(0, 100, 9));
			ring1.Add(new Pnt3D(100, 100, 9));
			ring1.Add(new Pnt3D(70, 20, 9));
			ring1.Add(new Pnt3D(100, 0, 9));

			for (var i = 0; i < ring1.Count; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				Linestring original = new Linestring(rotatedRing);

				Pnt3D rightMostBottomPoint = GetRightMostBottomPoint(original);

				Linestring linestring = original.Clone();
				Assert.True(linestring.IsClosed);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));
				Assert.True(linestring.ClockwiseOriented == true);
				linestring.ReverseOrientation();
				Assert.True(linestring.ClockwiseOriented == false);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));

				linestring = new Linestring(original.Segments);
				Assert.True(linestring.IsClosed);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));
				Assert.True(linestring.ClockwiseOriented == true);
				linestring.ReverseOrientation();
				Assert.True(linestring.ClockwiseOriented == false);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));

				rotatedRing.Reverse();
				linestring = new Linestring(rotatedRing);
				Assert.True(linestring.IsClosed);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));
				Assert.True(linestring.ClockwiseOriented == false);
				linestring.ReverseOrientation();
				Assert.True(linestring.ClockwiseOriented == true);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));

				// reverse, back, use (referenced!) segments again:
				linestring.ReverseOrientation();
				linestring = new Linestring(linestring.Segments);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));
				Assert.True(linestring.IsClosed);
				Assert.True(linestring.ClockwiseOriented == false);
				linestring.ReverseOrientation();
				Assert.True(linestring.ClockwiseOriented == true);
				Assert.True(
					rightMostBottomPoint.Equals(GetRightMostBottomPoint(linestring)));
			}
		}

		[Test]
		public void CanCalculateOrientationVertical()
		{
			var ring = new List<Pnt3D>();

			// ring1: horizontal:
			ring.Add(new Pnt3D(2600000, 1200000, 500));
			ring.Add(new Pnt3D(2600080, 1200060, 500));
			ring.Add(new Pnt3D(2600080, 1200060, 530));
			ring.Add(new Pnt3D(2600040, 1200030, 540));
			ring.Add(new Pnt3D(2600000, 1200000, 530));

			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				Linestring original = new Linestring(rotatedRing);

				Pnt3D rightMostBottomPoint = GetRightMostBottomPoint(original);

				Linestring linestring = original.Clone();
				Assert.True(linestring.IsClosed);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));
				AssertVerticalRing(linestring);
				linestring.ReverseOrientation();
				AssertVerticalRing(linestring);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));

				// New linestring from segments
				linestring = new Linestring(original.Segments);
				Assert.True(linestring.IsClosed);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));
				AssertVerticalRing(linestring);
				linestring.ReverseOrientation();
				AssertVerticalRing(linestring);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));

				// New linestring from segments (reversed)
				rotatedRing.Reverse();
				linestring = new Linestring(rotatedRing);
				Assert.True(linestring.IsClosed);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));
				AssertVerticalRing(linestring);
				linestring.ReverseOrientation();
				AssertVerticalRing(linestring);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));

				// reverse, back, use (referenced!) segments again:
				linestring.ReverseOrientation();
				linestring = new Linestring(linestring.Segments);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));
				Assert.True(linestring.IsClosed);
				AssertVerticalRing(linestring);
				linestring.ReverseOrientation();
				AssertVerticalRing(linestring);
				Assert.True(
					rightMostBottomPoint.EqualsXY(GetRightMostBottomPoint(linestring),
					                              double.Epsilon));
			}

			// And a version with some slight deviations
			ring.Add(ring[0]);
			ring.Insert(1, new Pnt3D(2600040.01, 1200030.01, 500));
			Linestring verticalWithin1mm = new Linestring(ring);

			Assert.True(verticalWithin1mm.IsVerticalRing(0.01));
		}

		[Test]
		public void CanDetermineNonVerticalRingWithSelfIntersectionAtBottomRight()
		{
			var ring = new List<Pnt3D>();

			// ring1: horizontal:
			ring.Add(new Pnt3D(0, 0, 500));
			ring.Add(new Pnt3D(0, 100, 500));
			ring.Add(new Pnt3D(100, 100, 530));
			ring.Add(new Pnt3D(100, 0, 540));
			ring.Add(new Pnt3D(110, -10, 540));
			ring.Add(new Pnt3D(90, 10, 550));

			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				Linestring original = new Linestring(rotatedRing);

				Assert.IsNull(original.ClockwiseOriented);
				Assert.IsFalse(original.IsVerticalRing(0.001));
			}
		}

		[Test]
		public void CanInterpolateUndefinedZsInRing()
		{
			var ring1 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 10));
			ring1.Add(new Pnt3D(0, 100, 20));
			ring1.Add(new Pnt3D(100, 100, double.NaN));
			ring1.Add(new Pnt3D(50, 50, double.NaN));
			ring1.Add(new Pnt3D(100, 0, 20 + 10 + 10 * Math.Sqrt(2)));

			Pnt3D testPoint1 = new Pnt3D(100, 100, double.NaN);
			Pnt3D testPoint2 = new Pnt3D(50, 50, double.NaN);

			Linestring linestring = null;

			for (var i = 0; i < ring1.Count + 1; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				linestring = new Linestring(rotatedRing);

				Assert.True(linestring.TryInterpolateUndefinedZs());

				int? pt1Idx = linestring.FindPointIdx(testPoint1, true);
				Assert.NotNull(pt1Idx);

				Pnt3D found = linestring.GetPoint3D(pt1Idx.Value);

				// incline: 10m per 100m:
				double testPoint1ExpectedZ = 20 + 10;
				Assert.AreEqual(testPoint1ExpectedZ, found.Z);

				int? pt2Idx = linestring.FindPointIdx(testPoint2, true);
				Assert.NotNull(pt2Idx);

				found = linestring.GetPoint3D(pt2Idx.Value);

				testPoint1ExpectedZ += 5 * Math.Sqrt(2);
				Assert.AreEqual(testPoint1ExpectedZ, found.Z);
			}

			Assert.NotNull(linestring);
			Pnt3D point = linestring[2].StartPoint;
			linestring.UpdatePoint(2, point.X, point.Y, double.NaN);

			Assert.True(linestring.TryInterpolateUndefinedZs());
			Assert.False(double.IsNaN(linestring[2].StartPoint.Z));
		}

		[Test]
		public void CanInterpolateUndefinedZsInLinestring()
		{
			var ring1 = new List<Pnt3D>();

			// ring1: horizontal:
			ring1.Add(new Pnt3D(0, 0, 10));
			ring1.Add(new Pnt3D(0, 100, 20));
			ring1.Add(new Pnt3D(100, 100, double.NaN));
			ring1.Add(new Pnt3D(50, 50, double.NaN));
			ring1.Add(new Pnt3D(100, 0, 20 + 10 + 10 * Math.Sqrt(2)));

			Pnt3D testPoint1 = new Pnt3D(100, 100, double.NaN);
			Pnt3D testPoint2 = new Pnt3D(50, 50, double.NaN);

			Linestring linestring = null;

			for (var i = 0; i < ring1.Count; i++)
			{
				Pnt3D[] array1 = ring1.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				linestring = new Linestring(rotatedRing);

				bool nanStartOrEnd = double.IsNaN(linestring.StartPoint.Z) ||
				                     double.IsNaN(linestring.EndPoint.Z);

				Assert.True(linestring.TryInterpolateUndefinedZs());

				int? pt1Idx = linestring.FindPointIdx(testPoint1, true);
				Assert.NotNull(pt1Idx);

				Pnt3D foundPt1 = linestring.GetPoint3D(pt1Idx.Value);

				int? pt2Idx = linestring.FindPointIdx(testPoint2, true);
				Assert.NotNull(pt2Idx);

				Pnt3D foundPt2 = linestring.GetPoint3D(pt2Idx.Value);

				if (! nanStartOrEnd)
				{
					// incline: 10m per 100m:
					double testPoint1ExpectedZ = 20 + 10;
					Assert.AreEqual(testPoint1ExpectedZ, foundPt1.Z);

					testPoint1ExpectedZ += 5 * Math.Sqrt(2);

					Assert.AreEqual(testPoint1ExpectedZ, foundPt2.Z);
				}

				Assert.False(linestring.GetPoints().Any(p => double.IsNaN(p.Z)));
			}

			Assert.NotNull(linestring);
			Pnt3D point = linestring[2].StartPoint;
			linestring.UpdatePoint(2, point.X, point.Y, double.NaN);

			Assert.True(linestring.TryInterpolateUndefinedZs());
			Assert.False(double.IsNaN(linestring[2].StartPoint.Z));
		}

		[Test]
		public void CanSnapToResolution()
		{
			var ring = new List<Pnt3D>();

			// ring1: horizontal:
			ring.Add(new Pnt3D(2600000.0001234, 1200000.0004321, 500.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200060.0004321, 500.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200030.0004321, 540.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200000.0004321, 530.0004321));

			EnvelopeXY bbAfterSnap = new EnvelopeXY(2600000, 1200000, 2600080, 1200060);

			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				Linestring original = new Linestring(rotatedRing);
				Linestring snapped = original.Clone();

				int rightMostBottom = snapped.RightMostBottomIndex;

				snapped.SnapToResolution(0.001, 2000000, 1000000, 0);

				Assert.AreEqual(rightMostBottom, snapped.RightMostBottomIndex);

				Assert.IsTrue(GeomRelationUtils.AreBoundsEqual(original, snapped, 0.001));
				Assert.IsTrue(GeomRelationUtils.AreBoundsEqual(bbAfterSnap, snapped, 0));
			}
		}

		[Test]
		public void CanSnapToResolutionChangingRightMostBottomVertex()
		{
			var ring = new List<Pnt3D>();

			// ring1: horizontal:
			ring.Add(new Pnt3D(2600000.000, 1200000.00, 500.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200060.0004321, 500.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200030.0004321, 540.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200000.0004321, 530.0004321));

			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);
				var rotatedRing = new List<Pnt3D>(array1);

				rotatedRing.Add((Pnt3D) rotatedRing[0].Clone());

				Linestring original = new Linestring(rotatedRing);
				Linestring snapped = original.Clone();

				int rightMostBottom = snapped.RightMostBottomIndex;

				Assert.AreEqual((ring.Count - i) % ring.Count, rightMostBottom);
				snapped.SnapToResolution(0.001, 2000000, 1000000, 0);

				int expected = (ring.Count - i - 1) % ring.Count;
				Assert.AreEqual(expected, snapped.RightMostBottomIndex);

				Assert.AreEqual(540d, snapped.GetPoints().Max(p => p.Z));
				Assert.AreEqual(500d, snapped.GetPoints().Min(p => p.Z));
			}
		}

		[Test]
		public void CanCloseInXY()
		{
			var points = new List<Pnt3D>();

			// ring1: horizontal:
			points.Add(new Pnt3D(2600000.000, 1200000.00, 500.0004321));
			points.Add(new Pnt3D(2600080.000, 1200060.000, 500.0004321));
			points.Add(new Pnt3D(2600080.000, 1200030.000, 540.0004321));
			points.Add(new Pnt3D(2600080.000, 1200000.0004321, 530.0004321));

			Linestring linestring = new Linestring(points);
			Assert.IsFalse(linestring.IsClosed);

			linestring.Close();
			Assert.IsTrue(linestring.IsClosed);

			Assert.AreEqual(4, linestring.SegmentCount);
		}

		[Test]
		public void CanCloseInXYAlreadyWithinTolerance()
		{
			var points = new List<Pnt3D>();

			points.Add(new Pnt3D(2600000.000, 1200000.00, 500.000));
			points.Add(new Pnt3D(2600080.000, 1200060.000, 500.000));
			points.Add(new Pnt3D(2600080.000, 1200030.000, 540.000));
			points.Add(new Pnt3D(2600080.000, 1200000.0004321, 530.000));
			points.Add(new Pnt3D(2600000.000123, 1200000.003, 500.0004321));

			Linestring linestring = new Linestring(points);
			Assert.IsFalse(linestring.IsClosed);

			linestring.Close(0.005);
			Assert.IsTrue(linestring.IsClosed);

			Assert.AreEqual(4, linestring.SegmentCount);
		}

		[Test]
		public void CanCloseInZ()
		{
			var points = new List<Pnt3D>();

			points.Add(new Pnt3D(2600000.000, 1200000.00, 500.0004321));
			points.Add(new Pnt3D(2600080.000, 1200060.000, 500.0004321));
			points.Add(new Pnt3D(2600080.000, 1200030.000, 540.0004321));
			points.Add(new Pnt3D(2600080.000, 1200000.0004321, 530.0004321));
			points.Add(new Pnt3D(2600000.000, 1200000.00, 300));

			Linestring linestring = new Linestring(points);
			Assert.IsFalse(linestring.IsClosed);

			linestring.Close();
			Assert.IsTrue(linestring.IsClosed);

			Assert.AreEqual(4, linestring.SegmentCount);
		}

		[Test]
		public void CanCloseInZAlreadyWithinTolerance()
		{
			var points = new List<Pnt3D>();

			points.Add(new Pnt3D(2600000.000, 1200000.00, 500.0004321));
			points.Add(new Pnt3D(2600080.000, 1200060.000, 500.0004321));
			points.Add(new Pnt3D(2600080.000, 1200030.000, 540.0004321));
			points.Add(new Pnt3D(2600080.000, 1200000.0004321, 530.0004321));
			points.Add(new Pnt3D(2600000.000, 1200000.00, 500));

			Linestring linestring = new Linestring(points);
			Assert.IsFalse(linestring.IsClosed);

			linestring.Close();
			Assert.IsTrue(linestring.IsClosed);

			Assert.AreEqual(4, linestring.SegmentCount);
		}

		private static Pnt3D GetRightMostBottomPoint(Linestring linestring)
		{
			return linestring.GetPoints(linestring.RightMostBottomIndex, 1, true).First();
		}

		private static void AssertVerticalRing(Linestring linestring, double tolerance = 0.00001)
		{
			Assert.IsTrue(linestring.IsVerticalRing(tolerance));

			// If the ring is vertical, the orientation must be null (but not vice-versa!)
			Assert.IsTrue(linestring.ClockwiseOriented == null);
		}
	}
}

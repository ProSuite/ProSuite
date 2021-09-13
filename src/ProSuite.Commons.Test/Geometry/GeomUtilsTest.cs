using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Test.Geometry
{
	[TestFixture]
	public class GeomUtilsTest
	{
		[Test]
		public void CanCreateExpandedBox4D()
		{
			var box = new Box(new Vector(new double[] {100, 200, 300, 400}),
			                  new Vector(new double[] {101, 201, 301, 401}));

			IBox expanded = GeomUtils.CreateBox(box, 1);

			Assert.AreNotSame(expanded, box);
			Assert.AreEqual(box.Dimension, expanded.Dimension);

			Assert.AreEqual(99, expanded.Min[0]);
			Assert.AreEqual(199, expanded.Min[1]);
			Assert.AreEqual(299, expanded.Min[2]);
			Assert.AreEqual(399, expanded.Min[3]);

			Assert.AreEqual(102, expanded.Max[0]);
			Assert.AreEqual(202, expanded.Max[1]);
			Assert.AreEqual(302, expanded.Max[2]);
			Assert.AreEqual(402, expanded.Max[3]);
		}

		[Test]
		public void CanCreateExpandedBox2D()
		{
			var box = new Box(new Pnt2D(100, 200), new Pnt2D(101, 201));

			IBox expanded = GeomUtils.CreateBox(box, 1);

			Assert.AreNotSame(expanded, box);
			Assert.AreEqual(box.Dimension, expanded.Dimension);

			Assert.AreEqual(99, expanded.Min.X);
			Assert.AreEqual(199, expanded.Min.Y);

			Assert.AreEqual(102, expanded.Max.X);
			Assert.AreEqual(202, expanded.Max.Y);
		}

		[Test]
		public void CanGetAngle3DInRad90()
		{
			var a = new Pnt3D(0, 0, 0);
			var b = new Pnt3D(0, 100, 0);
			var c = new Pnt3D(50, 100, 0);

			double expected = MathUtils.ToRadians(90);
			double calculated = GeomUtils.GetAngle3DInRad(a, b, c);
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(3);

			Assert.AreEqual(expected, calculated, epsilon);

			var d = new Pnt3D(-50, 100, 0);
			calculated = GeomUtils.GetAngle3DInRad(a, b, d);
			Assert.AreEqual(expected, calculated, epsilon);
		}

		[Test]
		public void CanGetAngle3DInRad180()
		{
			var a = new Pnt3D(0, 0, 0);
			var b = new Pnt3D(100, 100, 60);
			var c = new Pnt3D(200, 200, 120);

			Assert.AreEqual(MathUtils.ToRadians(180), GeomUtils.GetAngle3DInRad(a, b, c),
			                MathUtils.GetDoubleSignificanceEpsilon(3));
		}

		[Test]
		public void CanGetAngle3DInRad0()
		{
			var a = new Pnt3D(3333.33, 4444.44444, 5555.55555);
			var b = new Pnt3D(12345.6789, 9876.54321, 5678.90123);

			double calculated = GeomUtils.GetAngle3DInRad(a, b, a);

			// Problem: How should the client code know the applicable epsilon whithout knowledge of the internal algorithm?
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(a.X * b.X, a.Y * b.Y);
			Assert.AreEqual(0, calculated, epsilon);
		}

		[Test]
		public void CompareSpatialIndexPerformance()
		{
			const int pointCount = 100000;

			Linestring linestring1 =
				new Linestring(CreateRandomPoints(pointCount, 2600000, 1200000, 400));

			Linestring linestring2 =
				new Linestring(CreateRandomPoints(pointCount, 2600000, 1200000, 400));

			double tolerance = 0.01;

			MemoryUsageInfo memUsageBox = new MemoryUsageInfo();
			var watch = Stopwatch.StartNew();

			linestring2.SpatialIndex =
				BoxTreeSearcher<int>.CreateSpatialSearcher(linestring2);

			watch.Stop();

			Console.WriteLine(
				$"Linestring ({pointCount}) - Box tree created: {watch.ElapsedMilliseconds}ms. PB: {memUsageBox.Refresh().PrivateBytesDelta:N0}");

			watch = Stopwatch.StartNew();
			var foundCountBoxtree = 0;
			foreach (Line3D line3D in linestring1)
			{
				int foundInEnv = linestring2.FindSegments(
					line3D.XMin, line3D.YMin, line3D.XMax,
					line3D.YMax, tolerance).Count();

				if (foundInEnv > 0) { }

				foundCountBoxtree += foundInEnv;
			}

			watch.Stop();

			Console.WriteLine(
				$"Linestring ({pointCount}) - Box tree ({foundCountBoxtree} segments found): {watch.ElapsedMilliseconds}ms");

			MemoryUsageInfo memUsageHash = new MemoryUsageInfo();
			watch = Stopwatch.StartNew();

			linestring2.SpatialIndex = CreateSpatialHash(linestring2);

			watch.Stop();
			Console.WriteLine(
				$"Linestring ({pointCount}) - spatial hash created: {watch.ElapsedMilliseconds}ms. PB: {memUsageHash.Refresh().PrivateBytesDelta:N0}");

			watch = Stopwatch.StartNew();
			var foundSpatialHash = 0;
			foreach (Line3D line3D in linestring1)
			{
				int foundInEnv = linestring2.FindSegments(
					line3D.XMin, line3D.YMin, line3D.XMax,
					line3D.YMax, tolerance).Count();

				if (foundInEnv > 0) { }

				foundSpatialHash += foundInEnv;
			}

			watch.Stop();

			Console.WriteLine(
				$"Linestring ({pointCount}) - Spatial hash ({foundSpatialHash} segments found): {watch.ElapsedMilliseconds}ms");

			Assert.AreEqual(foundCountBoxtree, foundSpatialHash);

			MultiPolycurve poly2 = new MultiPolycurve(new List<Linestring> {linestring2});

			memUsageHash.Refresh();
			watch = Stopwatch.StartNew();

			poly2.SpatialIndex =
				SpatialHashSearcher<SegmentIndex>.CreateSpatialSearcher(poly2);

			watch.Stop();

			Console.WriteLine(
				$"Linestring ({pointCount}) - spatial hash (using multi-part index) created: {watch.ElapsedMilliseconds}ms. PB: {memUsageHash.Refresh().PrivateBytesDelta:N0}");

			watch = Stopwatch.StartNew();
			foundSpatialHash = 0;
			foreach (Line3D line3D in linestring1)
			{
				int foundInEnv = poly2.FindSegments(
					line3D.XMin, line3D.YMin, line3D.XMax,
					line3D.YMax, tolerance).Count();

				if (foundInEnv > 0) { }

				foundSpatialHash += foundInEnv;
			}

			watch.Stop();

			Console.WriteLine(
				$"Linestring ({pointCount}) - Spatial hash on poly ({foundSpatialHash} segments found): {watch.ElapsedMilliseconds}ms");
			Assert.AreEqual(foundCountBoxtree, foundSpatialHash);
		}

		private static ISpatialSearcher<int> CreateSpatialHash(Linestring linestring)
		{
			MemoryUsageInfo memUsageHash = new MemoryUsageInfo();
			var watch = Stopwatch.StartNew();

			var gridSize =
				SpatialHashSearcher<int>.EstimateOptimalGridSize(new[] {linestring});

			watch.Stop();
			Console.WriteLine(
				$"Linestring ({linestring.PointCount}) - Spatial hash: calculated grid size: {watch.ElapsedMilliseconds}ms. PB: {memUsageHash.Refresh().PrivateBytesDelta:N0}");

			watch = Stopwatch.StartNew();

			var result =
				SpatialHashSearcher<int>.CreateSpatialSearcher(linestring, gridSize);

			watch.Stop();
			Console.WriteLine(
				$"Linestring ({linestring.PointCount}) - Spatial hash created: {watch.ElapsedMilliseconds}ms. PB: {memUsageHash.Refresh().PrivateBytesDelta:N0}");

			return result;
		}

		private static IEnumerable<Pnt3D> CreateRandomPoints(int pointCount,
		                                                     double x, double y, double z)
		{
			Random random = new Random();

			Pnt3D result = new Pnt3D(x, y, z);

			const double averageLength = 1.5;

			for (int i = 0; i < pointCount; i++)
			{
				result = result.ClonePnt3D();

				result.X += (random.NextDouble() - 0.5) * averageLength;
				result.Y += (random.NextDouble() - 0.5) * averageLength;
				result.Z = random.NextDouble() * averageLength;

				yield return result;
			}
		}
	}
}

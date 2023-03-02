using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test;

[TestFixture]
public class ClusterUtilsTest
{
	[Test]
	public void CanClusterSampleData()
	{
		var points = new[]
		             {
			             new Point(35, 42, "Chicago"),
			             new Point(52, 10, "Mobile"),
			             new Point(62, 77, "Toronto"),
			             new Point(82, 65, "Buffalo"),
			             new Point(5, 45, "Denver"),
			             new Point(27, 35, "Omaha"),
			             new Point(85, 15, "Atlanta"),
			             new Point(90, 5, "Miami"),
			             new Point(2, 2, "Zwo"),
			             new Point(3, 3, "Three"),
			             new Point(1, 1, "One"),
			             new Point(2, 2, "Two")
		             };

		// With maxdist 0, only ties are clustered:
		var c1 = Cluster(points, 0.0);
		Assert.AreEqual(11, c1.Count());

		// Two|Zwo clusters with One or Three (same dist, undecided):
		var c2 = Cluster(points, 2.0);
		Assert.AreEqual(10, c2.Count());

		var c3 = Cluster(points, 11 * 11);
		Assert.AreEqual(8, c3.Count());

		var c4 = Cluster(points, 12 * 12);
		Assert.AreEqual(7, c4.Count());

		var c5 = Cluster(points, 20 * 20);
		Assert.AreEqual(7, c5.Count());

		var c6 = Cluster(points, 24 * 24);
		Assert.AreEqual(6, c6.Count());

		// With such a large maxdist, all points in one cluster:
		var c7 = Cluster(points, 100 * 100);
		Assert.AreEqual(1, c7.Count());
	}

	#region Test utils

	private class Point
	{
		public readonly double X;
		public readonly double Y;
		public readonly string Text;

		public Point(double x, double y, string text)
		{
			X = x;
			Y = y;
			Text = text;
		}

		public override string ToString()
		{
			return Text ?? string.Empty;
		}
	}

	private static double DistanceFunc(Point a, Point b)
	{
		double dx = a.X - b.X;
		double dy = a.Y - b.Y;
		return dx * dx + dy * dy;
	}

	private static Point MergeFunc(Point a, Point b)
	{
		return new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2, string.Concat(a.Text, "|", b.Text));
	}

	private static IEnumerable<Point> Cluster(IEnumerable<Point> points, double maxdist)
	{
		var clusters = ClusterUtils.Cluster(points, maxdist, DistanceFunc, MergeFunc);

		int n = points.Count();
		int m = clusters.Count();

		Console.WriteLine(@"maxdist={0}, n={1}, m={2}", maxdist, n, m);

		foreach (var point in clusters)
		{
			Console.WriteLine(@" {0}", point);
		}

		return clusters;
	}

	#endregion
}

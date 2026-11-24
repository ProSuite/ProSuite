using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.AGP.Editing.Test
{
	[TestFixture]
	public class VertexHeightsTest
	{
		private const double _tolerance = 0.01;

		[Test]
		public void CanInterpolateBetweenOwnHeights()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddOwn(0, 0);
			heights.AddInterpolated(10, 0);
			heights.AddOwn(30, 0);

			List<Pnt3D> result = heights.Apply(Line((0, 0, 10), (10, 0, 100), (30, 0, 40)),
			                                   out bool endsKnown);

			Assert.AreEqual(new[] { 10.0, 20, 40 }, GetZs(result));
			Assert.True(endsKnown);
		}

		[Test]
		public void CanInterpolateAlongTheLine()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(3, 4);

			// 5 m to the middle vertex, 10 m from there to the end
			List<Pnt3D> result = heights.Apply(Line((0, 0, 0), (3, 4, 100), (3, 14, 15)),
			                                   out _);

			Assert.AreEqual(new[] { 0.0, 5, 15 }, GetZs(result));
		}

		[Test]
		public void CanUseSnappedHeight()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(10, 0);
			heights.AddSnapped(20, 0, 50);
			heights.AddInterpolated(30, 0);

			List<Pnt3D> result = heights.Apply(
				Line((0, 0, 10), (10, 0, 100), (20, 0, 100), (30, 0, 100), (40, 0, 10)),
				out bool endsKnown);

			Assert.AreEqual(new[] { 10.0, 30, 50, 30, 10 }, GetZs(result));
			Assert.True(endsKnown);
		}

		[Test]
		public void CanKeepOwnHeightsOfUnrecordedVertices()
		{
			var heights = new VertexHeights(_tolerance);

			List<Pnt3D> result = heights.Apply(Line((0, 0, 10), (10, 0, 77), (20, 0, 30)),
			                                   out bool endsKnown);

			Assert.AreEqual(new[] { 10.0, 77, 30 }, GetZs(result));
			Assert.True(endsKnown);
		}

		[Test]
		public void CanReportInterpolatedEnd()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddOwn(0, 0);
			heights.AddInterpolated(10, 0);

			List<Pnt3D> result = heights.Apply(Line((0, 0, 10), (10, 0, 100)),
			                                   out bool endsKnown);

			Assert.False(endsKnown);
			// Shown at the height of the nearest known vertex until the line gets a known end
			Assert.AreEqual(new[] { 10.0, 10 }, GetZs(result));
		}

		[Test]
		public void CanReportInterpolatedStart()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(0, 0);

			heights.Apply(Line((0, 0, 10), (10, 0, 20)), out bool endsKnown);

			Assert.False(endsKnown);
		}

		[Test]
		public void CanAcceptEndWithOwnHeightNotYetKnown()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddOwn(0, 0);
			heights.AddInterpolated(10, 0);
			heights.AddOwn(20, 0);

			// The surface height of the last click has not arrived yet
			List<Pnt3D> result = heights.Apply(
				Line((0, 0, 10), (10, 0, 100), (20, 0, double.NaN)), out bool endsKnown);

			Assert.True(endsKnown);
			Assert.AreEqual(new[] { 10.0, 10, 10 }, GetZs(result));
		}

		[Test]
		public void CanKeepOwnHeightsWithoutKnownVertex()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(0, 0);
			heights.AddInterpolated(10, 0);

			List<Pnt3D> result = heights.Apply(Line((0, 0, 10), (10, 0, 20)), out _);

			Assert.AreEqual(new[] { 10.0, 20 }, GetZs(result));
		}

		[Test]
		public void CanHandleSingleVertex()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(0, 0);

			List<Pnt3D> result = heights.Apply(Line((0, 0, 10)), out bool endsKnown);

			Assert.AreEqual(new[] { 10.0 }, GetZs(result));
			Assert.False(endsKnown);
		}

		[Test]
		public void CanFindVertexAfterInsertion()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(20, 0);

			// A vertex inserted before it moves its index, not its position
			List<Pnt3D> result = heights.Apply(
				Line((0, 0, 0), (10, 0, 10), (20, 0, 99), (40, 0, 40)), out _);

			Assert.AreEqual(new[] { 0.0, 10, 20, 40 }, GetZs(result));
		}

		[Test]
		public void CanFindVertexWithinTolerance()
		{
			var heights = new VertexHeights(_tolerance);
			heights.AddInterpolated(10.005, 0);

			Assert.True(heights.Contains(10, 0));
			Assert.False(heights.Contains(10.05, 0));
		}

		private static List<Pnt3D> Line(params (double x, double y, double z)[] vertices)
		{
			return vertices.Select(v => new Pnt3D(v.x, v.y, v.z)).ToList();
		}

		private static double[] GetZs(IEnumerable<Pnt3D> points)
		{
			return points.Select(p => p.Z).ToArray();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	[TestFixture]
	public class PolyhedronTest
	{
		[Test]
		public void CanGetFootprint()
		{
			// 1----------------2
			// |                |
			// |                |
			// |                |
			// |                |
			// 0/4______________3

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0),
			           };

			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l), 0.01, 1, 5, 100 * 100));
		}

		[Test]
		public void CanGetFootprintWithIsland()
		{
			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 0, 0),
			           };

			var island = new Linestring(new[]
			                            {
				                            new Pnt3D(20, 20, 0),
				                            new Pnt3D(40, 20, 0),
				                            new Pnt3D(40, 40, 0),
				                            new Pnt3D(20, 40, 0),
				                            new Pnt3D(20, 20, 0)
			                            });

			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l, island), 0.01, 2, 10, 100 * 100 - 20 * 20));
		}

		[Test]
		public void CanGetFootprintWithAlmostDuplicateVertex()
		{
			// 1----------------2/3
			// |                |
			// |                |
			// |                |
			// |                |
			// 0________________4

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 100, 0),
				           new Pnt3D(100, 100, 0),
				           new Pnt3D(100, 100.001, 0),
				           new Pnt3D(100, 0, 0),
			           };

			// The removal of duplicate segments (verticals) also cleans almost-duplicates
			const int expectedFootprintVertices = 5;
			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l), 0.01, 1, expectedFootprintVertices, 100 * 100.0005));
		}

		[Test]
		public void CanGetFootprintFromVerticalNeedle()
		{
			// 1        ^ z
			// |        |
			// 2        |
			// 3        |
			// |        |
			// 0        |

			var ring = new List<Pnt3D>
			           {
				           new Pnt3D(0, 0, 0),
				           new Pnt3D(0, 0, 100),
				           new Pnt3D(0, 0, 70),
				           new Pnt3D(0, 0, 60),
				           new Pnt3D(0, 0, 0),
			           };

			WithRotatedRing(
				ring, l => AssertCanCreateFootprint(
					CreatePolyhedron(l), 0.001, 0, 0, 0));
		}

		private static Polyhedron CreatePolyhedron(Linestring linestring,
		                                           params Linestring[] islands)
		{
			var ringGroup = new RingGroup(linestring);

			foreach (Linestring island in islands)
			{
				ringGroup.AddInteriorRing(island);
			}

			return new Polyhedron(new List<RingGroup>(new[] { ringGroup }));
		}

		private static void AssertCanCreateFootprint(Polyhedron polyhedron,
		                                             double tolerance,
		                                             int expectedPartCount,
		                                             int expectedPointCount,
		                                             double expectedArea)
		{
			MultiLinestring footprint =
				polyhedron.GetXYFootprint(tolerance, out List<Linestring> verticalRings);

			Assert.AreEqual(polyhedron.Count, footprint.Count + verticalRings.Count);
			Assert.NotNull(footprint);

			Assert.True(footprint.GetLinestrings().All(l => l.IsClosed));

			if (! footprint.IsEmpty)
			{
				Assert.True(footprint.GetLinestrings().FirstOrDefault()?.ClockwiseOriented);
			}

			Assert.AreEqual(expectedPartCount, footprint.PartCount);
			Assert.AreEqual(expectedPointCount, footprint.PointCount);
			Assert.AreEqual(expectedArea, footprint.GetArea2D(), 0.001);
		}

		private static void WithRotatedRing(IList<Pnt3D> ring,
		                                    Action<Linestring> proc)
		{
			for (var i = 0; i < ring.Count; i++)
			{
				Pnt3D[] array1 = ring.ToArray();
				CollectionUtils.Rotate(array1, i);

				Linestring linestring = GeomTestUtils.CreateRing(array1.ToList());

				proc(linestring);
			}
		}
	}
}

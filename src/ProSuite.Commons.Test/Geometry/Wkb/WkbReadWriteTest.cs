using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.Wkb;

namespace ProSuite.Commons.Test.Geometry.Wkb
{
	[TestFixture]
	public class WkbReadWriteTest
	{
		[Test]
		public void CanWritePoint()
		{
			Pnt3D point = new Pnt3D(2600001.123, 1200000.987, 432.1);

			WkbGeomWriter writer = new WkbGeomWriter();

			byte[] bytes = writer.WritePoint(point);

			WkbGeomReader reader = new WkbGeomReader();

			IPnt deserializedPoint = reader.ReadPoint(new MemoryStream(bytes));

			Assert.IsTrue(deserializedPoint.Equals(point));
		}

		[Test]
		public void CanWriteMultiPoint()
		{
			Pnt3D point1 = new Pnt3D(2600001.123, 1200000.987, 432.1);
			Pnt3D point2 = new Pnt3D(2600002.234, 1200002.876, 321.98);

			IList<IPnt> multipoint = new IPnt[]
			                         {
				                         point1,
				                         point2
			                         };

			WkbGeomWriter writer = new WkbGeomWriter();

			byte[] bytes = writer.WriteMultipoint(multipoint);

			WkbGeomReader reader = new WkbGeomReader();

			List<IPnt> deserizalized = reader.ReadMultiPoint(new MemoryStream(bytes)).ToList();

			Assert.AreEqual(multipoint.Count, deserizalized.Count);

			for (int i = 0; i < multipoint.Count; i++)
			{
				Assert.IsTrue(deserizalized[i].Equals(multipoint[i]));
			}
		}

		[Test]
		public void CanWritePolygonWithInnerRing()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 8),
				            new Pnt3D(100, 100, 5),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup polygon = CreatePoly(ring1);
			polygon.AddInteriorRing(new Linestring(new[]
			                                       {
				                                       new Pnt3D(25, 50, 0),
				                                       new Pnt3D(50, 50, 0),
				                                       new Pnt3D(50, 75, 0),
				                                       new Pnt3D(25, 75, 0),
				                                       new Pnt3D(25, 50, 0)
			                                       }
			                        ));

			WkbGeomWriter writer = new WkbGeomWriter();

			byte[] bytes = writer.WritePolygon(polygon);

			WkbGeomReader reader = new WkbGeomReader();
			RingGroup deserialized = reader.ReadPolygon(new MemoryStream(bytes));

			Assert.IsTrue(deserialized.Equals(polygon));
		}

		[Test]
		public void CanWriteAndReadMultiPolygonWithInnerRing()
		{
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 8),
				            new Pnt3D(100, 100, 5),
				            new Pnt3D(100, 20, 9)
			            };

			var disjoint = new List<Pnt3D>();
			disjoint.Add(new Pnt3D(140, -10, 0));
			disjoint.Add(new Pnt3D(140, 30, 23));
			disjoint.Add(new Pnt3D(300, 30, 56));
			disjoint.Add(new Pnt3D(300, -10, 0));

			RingGroup poly1 = CreatePoly(ring1);
			poly1.AddInteriorRing(new Linestring(new[]
			                                     {
				                                     new Pnt3D(25, 50, 0),
				                                     new Pnt3D(50, 50, 0),
				                                     new Pnt3D(50, 75, 0),
				                                     new Pnt3D(25, 75, 0),
				                                     new Pnt3D(25, 50, 0)
			                                     }
			                      ));

			Linestring disjointRing = CreateRing(disjoint);

			var poly2 = new RingGroup(disjointRing);

			var multipolygon = new List<RingGroup>(new[] {poly1, poly2});

			WkbGeomWriter writer = new WkbGeomWriter();

			byte[] bytes = writer.WriteMultipolygon(multipolygon);

			WkbGeomReader reader = new WkbGeomReader();
			IList<RingGroup> deserialized = reader.ReadMultiPolygon(new MemoryStream(bytes));

			Assert.IsTrue(deserialized[0].Equals(multipolygon[0]));
			Assert.IsTrue(deserialized[1].Equals(multipolygon[1]));
		}

		[Test]
		public void CanWriteAndReadMultiLinestring()
		{
			var points1 = new List<Pnt3D>
			              {
				              new Pnt3D(0, 0, 9),
				              new Pnt3D(0, 100, 8),
				              new Pnt3D(100, 100, 5)
			              };

			var points2 = new List<Pnt3D>();
			points2.Add(new Pnt3D(140, -10, 0));
			points2.Add(new Pnt3D(140, 30, 23));
			points2.Add(new Pnt3D(300, 30, 56));
			points2.Add(new Pnt3D(300, -10, 0));

			MultiPolycurve polycurve =
				new MultiPolycurve(new[] {new Linestring(points1), new Linestring(points2)});

			WkbGeomWriter writer = new WkbGeomWriter();

			byte[] bytes = writer.WriteMultiLinestring(polycurve);

			WkbGeomReader reader = new WkbGeomReader();
			MultiPolycurve deserialized = reader.ReadMultiPolycurve(new MemoryStream(bytes));

			Assert.IsTrue(deserialized.Equals(polycurve));
		}

		[Test]
		public void CanReadClockwiseWindingPolygon()
		{
			// Some OGC 1.1 implementations do not have counter-clockwise polygon winding order:
			var ring1 = new List<Pnt3D>
			            {
				            new Pnt3D(0, 0, 9),
				            new Pnt3D(0, 100, 8),
				            new Pnt3D(100, 100, 5),
				            new Pnt3D(100, 20, 9)
			            };

			RingGroup polygon = CreatePoly(ring1);

			polygon.AddInteriorRing(new Linestring(new[]
			                                       {
				                                       new Pnt3D(25, 50, 0),
				                                       new Pnt3D(50, 50, 0),
				                                       new Pnt3D(50, 75, 0),
				                                       new Pnt3D(25, 75, 0),
				                                       new Pnt3D(25, 50, 0)
			                                       }
			                        ));

			RingGroup inverted = (RingGroup) polygon.Clone();
			inverted.ReverseOrientation();

			WkbGeomWriter writer = new WkbGeomWriter();

			byte[] bytes = writer.WritePolygon(inverted);

			const bool assumeWkbPolygonsClockwise = true;
			WkbGeomReader reader = new WkbGeomReader(assumeWkbPolygonsClockwise);

			RingGroup deserialized = reader.ReadPolygon(new MemoryStream(bytes));

			Assert.IsTrue(deserialized.Equals(polygon));
		}

		private static RingGroup CreatePoly(List<Pnt3D> points)
		{
			Linestring ring = CreateRing(points);

			RingGroup poly = new RingGroup(ring);

			return poly;
		}

		private static Linestring CreateRing(List<Pnt3D> points)
		{
			if (! points[0].Equals(points[points.Count - 1]))
			{
				points = new List<Pnt3D>(points);
				points.Add(points[0].ClonePnt3D());
			}

			var ring = new Linestring(points);
			return ring;
		}
	}
}

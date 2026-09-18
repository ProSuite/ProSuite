using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;
using ProSuite.Commons.Geom.Wkt;

namespace ProSuite.Commons.Test.Geom.Wkt
{
	[TestFixture]
	public class WktReadTest
	{
		#region Points

		[Test]
		public void CanReadPoint()
		{
			IPnt point = new WktGeomReader().ReadPoint("POINT (2600001.5 1200000.25)");

			Assert.AreEqual(2600001.5, point.X);
			Assert.AreEqual(1200000.25, point.Y);
		}

		[Test]
		public void CanReadPointZ()
		{
			var reader = new WktGeomReader();

			foreach (string wkt in new[]
			                       {
				                       "POINT Z (2600001 1200000 432.1)",
				                       // Untagged three-ordinate form, which is how most
				                       // producers write XYZ.
				                       "POINT (2600001 1200000 432.1)"
			                       })
			{
				var point = (Pnt3D) reader.ReadPoint(wkt);

				Assert.AreEqual(2600001, point.X, wkt);
				Assert.AreEqual(1200000, point.Y, wkt);
				Assert.AreEqual(432.1, point.Z, wkt);
			}
		}

		[Test]
		public void CanReadPointEmpty()
		{
			Assert.IsNull(new WktGeomReader().ReadPoint("POINT EMPTY"));
		}

		[Test]
		public void MeasuredPointIsRecognisedButNotBuilt()
		{
			// Recognised and its ordinates read, but Pnt3D carries no measure - the same limit
			// the WKB side has, and for the same reason.
			var reader = new WktGeomReader();

			Assert.Throws<NotImplementedException>(
				() => reader.ReadPoint("POINT M (2600001 1200000 5)"));
			Assert.Throws<NotImplementedException>(
				() => reader.ReadPoint("POINT ZM (2600001 1200000 400 5)"));
		}

		[Test]
		public void CanReadMultiPointInBothSpellings()
		{
			var reader = new WktGeomReader();

			foreach (string wkt in new[]
			                       {
				                       "MULTIPOINT (10 20, 30 40)",
				                       "MULTIPOINT ((10 20), (30 40))"
			                       })
			{
				Multipoint<IPnt> multipoint = reader.ReadMultiPoint(wkt);

				Assert.AreEqual(2, multipoint.PointCount, wkt);
				Assert.AreEqual(10, multipoint.GetPoint(0).X, wkt);
				Assert.AreEqual(40, multipoint.GetPoint(1).Y, wkt);
			}
		}

		[Test]
		public void CanReadMultiPointEmpty()
		{
			Assert.AreEqual(0, new WktGeomReader().ReadMultiPoint("MULTIPOINT EMPTY").PointCount);
		}

		#endregion

		#region Linestrings

		[Test]
		public void CanReadLinestring()
		{
			MultiPolycurve polycurve =
				new WktGeomReader().ReadMultiPolycurve("LINESTRING (0 0, 10 0, 10 10)");

			Assert.AreEqual(1, polycurve.PartCount);
			Assert.AreEqual(3, polycurve.GetLinestring(0).PointCount);
			Assert.AreEqual(10, polycurve.GetLinestring(0).GetPoint(2).Y);
		}

		[Test]
		public void CanReadMultiLinestring()
		{
			MultiPolycurve polycurve = new WktGeomReader().ReadMultiPolycurve(
				"MULTILINESTRING ((0 0, 10 0), (20 20, 30 30, 40 40))");

			Assert.AreEqual(2, polycurve.PartCount);
			Assert.AreEqual(2, polycurve.GetLinestring(0).PointCount);
			Assert.AreEqual(3, polycurve.GetLinestring(1).PointCount);
		}

		[Test]
		public void CanReadLinestringEmpty()
		{
			var reader = new WktGeomReader();

			Assert.IsTrue(reader.ReadMultiPolycurve("LINESTRING EMPTY").IsEmpty);
			Assert.IsTrue(reader.ReadMultiPolycurve("MULTILINESTRING EMPTY").IsEmpty);
		}

		[Test]
		public void LinestringDirectionIsTheTextsOwn()
		{
			// A linestring has no inside, so nothing is turned around.
			Linestring line = new WktGeomReader()
			                  .ReadMultiPolycurve("LINESTRING (0 0, 10 0, 10 10)")
			                  .GetLinestring(0);

			Assert.AreEqual(0, line.GetPoint(0).X);
			Assert.AreEqual(10, line.GetPoint(2).X);
		}

		#endregion

		#region Polygons

		[Test]
		public void CanReadPolygon()
		{
			RingGroup polygon = new WktGeomReader().ReadPolygon(
				"POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))");

			Assert.NotNull(polygon);
			Assert.IsFalse(polygon.InteriorRings.Any());
			Assert.AreEqual(4, polygon.ExteriorRing.SegmentCount);
			Assert.IsTrue(polygon.ExteriorRing.IsClosed);
		}

		[Test]
		public void CanReadPolygonWithInteriorRing()
		{
			RingGroup polygon = new WktGeomReader().ReadPolygon(
				"POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2))");

			Assert.NotNull(polygon);
			Assert.AreEqual(1, polygon.InteriorRings.Count());
			Assert.AreEqual(4, polygon.InteriorRings.Single().SegmentCount);
		}

		[Test]
		public void CanReadPolygonEmpty()
		{
			// Null, as WkbGeomReader gives for a polygon with no rings.
			Assert.IsNull(new WktGeomReader().ReadPolygon("POLYGON EMPTY"));
		}

		[Test]
		public void CanReadMultiPolygon()
		{
			IList<RingGroup> parts = new WktGeomReader().ReadMultiPolygon(
				"MULTIPOLYGON (((0 0, 0 10, 10 10, 10 0, 0 0)), " +
				"((20 20, 20 30, 30 30, 30 20, 20 20)))");

			Assert.AreEqual(2, parts.Count);
			Assert.AreEqual(0, parts[0].ExteriorRing.GetPoint(0).X);
			Assert.AreEqual(20, parts[1].ExteriorRing.GetPoint(0).X);
		}

		[Test]
		public void CanReadSinglePolygonAsMultiPolygon()
		{
			IList<RingGroup> parts = new WktGeomReader().ReadMultiPolygon(
				"POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))");

			Assert.AreEqual(1, parts.Count);
		}

		[Test]
		public void CanReadMultiPolygonEmpty()
		{
			Assert.AreEqual(0, new WktGeomReader().ReadMultiPolygon("MULTIPOLYGON EMPTY").Count);
		}

		[Test]
		public void RingWindingFollowsTheAssumedOrientation()
		{
			const string wkt = "POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))";

			// The text winds this ring clockwise. Assuming OGC's counter-clockwise exterior turns
			// it around; saying the text is clockwise leaves it as written.
			Linestring turned = new WktGeomReader().ReadPolygon(wkt).ExteriorRing;
			Linestring asWritten =
				new WktGeomReader(assumeWktPolygonsClockwise: true).ReadPolygon(wkt).ExteriorRing;

			Assert.AreEqual(0, asWritten.GetPoint(1).X);
			Assert.AreEqual(10, asWritten.GetPoint(1).Y);

			Assert.AreEqual(10, turned.GetPoint(1).X);
			Assert.AreEqual(0, turned.GetPoint(1).Y);
		}

		#endregion

		#region Surfaces

		[Test]
		public void CanReadPolyhedralSurface()
		{
			IList<Polyhedron> surfaces = new WktGeomReader().ReadMultiSurface(
				"POLYHEDRALSURFACE Z (((0 0 0, 0 1 0, 1 1 0, 1 0 0, 0 0 0)), " +
				"((0 0 0, 0 1 0, 0 1 1, 0 0 1, 0 0 0)))");

			Assert.AreEqual(1, surfaces.Count);
			Assert.AreEqual(2, surfaces[0].RingGroups.Count);
		}

		[Test]
		public void CanReadTin()
		{
			IList<Polyhedron> surfaces = new WktGeomReader().ReadMultiSurface(
				"TIN (((0 0, 0 1, 1 1, 0 0)), ((0 0, 1 1, 1 0, 0 0)))");

			Assert.AreEqual(1, surfaces.Count);
			Assert.AreEqual(2, surfaces[0].RingGroups.Count);
		}

		[Test]
		public void CanReadPolyhedralSurfaceEmpty()
		{
			Assert.AreEqual(
				0, new WktGeomReader().ReadMultiSurface("POLYHEDRALSURFACE EMPTY").Count);
		}

		#endregion

		#region ReadGeometry

		[Test]
		public void ReadGeometryDispatchesOnTheTaggedType()
		{
			var reader = new WktGeomReader();

			var cases = new Dictionary<string, WkbGeometryType>
			            {
				            { "POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))", WkbGeometryType.Polygon },
				            {
					            "MULTIPOLYGON (((0 0, 0 10, 10 10, 10 0, 0 0)))",
					            WkbGeometryType.MultiPolygon
				            },
				            {
					            "MULTILINESTRING ((0 0, 10 0))",
					            WkbGeometryType.MultiLineString
				            },
				            { "MULTIPOINT (10 20, 30 40)", WkbGeometryType.MultiPoint },
				            {
					            "TIN (((0 0, 0 1, 1 1, 0 0)))", WkbGeometryType.Tin
				            }
			            };

			foreach (KeyValuePair<string, WkbGeometryType> testCase in cases)
			{
				IBoundedXY geometry =
					reader.ReadGeometry(testCase.Key, out WkbGeometryType geometryType);

				Assert.AreEqual(testCase.Value, geometryType, testCase.Key);
				Assert.NotNull(geometry, testCase.Key);
			}
		}

		#endregion

		#region Agreement with the WKB reader

		[Test]
		public void PolygonAgreesWithTheWkbReader()
		{
			AssertAgreesWithWkb("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))");
		}

		[Test]
		public void PolygonWithInteriorRingAgreesWithTheWkbReader()
		{
			AssertAgreesWithWkb(
				"POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0), (2 2, 2 4, 4 4, 4 2, 2 2))");
		}

		[Test]
		public void MultiPolygonAgreesWithTheWkbReader()
		{
			AssertAgreesWithWkb(
				"MULTIPOLYGON (((0 0, 0 10, 10 10, 10 0, 0 0)), " +
				"((20 20, 20 30, 30 30, 30 20, 20 20)))");
		}

		/// <summary>
		/// WKT to geometry to WKB and back again: the two readers must produce the same rings, in
		/// the same order, with the same points.
		/// </summary>
		private static void AssertAgreesWithWkb(string wkt)
		{
			IList<RingGroup> fromWkt = new WktGeomReader().ReadMultiPolygon(wkt);

			byte[] wkb = new WkbGeomWriter().WriteMultipolygon(fromWkt, Ordinates.Xy);

			IList<RingGroup> fromWkb =
				new WkbGeomReader().ReadMultiPolygon(new MemoryStream(wkb));

			Assert.AreEqual(fromWkt.Count, fromWkb.Count, "part count");

			for (var part = 0; part < fromWkt.Count; part++)
			{
				Assert.AreEqual(fromWkt[part].PartCount, fromWkb[part].PartCount,
				                $"ring count of part {part}");

				for (var ring = 0; ring < fromWkt[part].PartCount; ring++)
				{
					Linestring expected = fromWkt[part].GetLinestring(ring);
					Linestring actual = fromWkb[part].GetLinestring(ring);

					Assert.AreEqual(expected.PointCount, actual.PointCount,
					                $"point count of part {part} ring {ring}");

					for (var i = 0; i < expected.PointCount; i++)
					{
						Assert.AreEqual(expected.GetPoint(i).X, actual.GetPoint(i).X);
						Assert.AreEqual(expected.GetPoint(i).Y, actual.GetPoint(i).Y);
					}
				}
			}
		}

		#endregion

		#region Malformed input

		[Test]
		public void MalformedInputSaysWhereItGaveUp()
		{
			var reader = new WktGeomReader();

			// Each case names the entry point that would accept it if it were well formed.
			var cases = new (string Wkt, string Expected, TestDelegate Read)[]
			            {
				            ("", "expected a word",
				             () => reader.ReadGeometry("", out WkbGeometryType _)),
				            ("RECTANGLE (0 0, 10 10)", "not a known geometry type",
				             () => reader.ReadGeometry("RECTANGLE (0 0, 10 10)",
				                                       out WkbGeometryType _)),
				            ("POLYGON ((0 0, 0 10, 10 10", "expected ')'",
				             () => reader.ReadPolygon("POLYGON ((0 0, 0 10, 10 10")),
				            ("POLYGON 0 0, 0 10", "expected '('",
				             () => reader.ReadPolygon("POLYGON 0 0, 0 10")),
				            ("LINESTRING (0 0, ten 0)", "expected a number",
				             () => reader.ReadMultiPolycurve("LINESTRING (0 0, ten 0)")),
				            ("POINT (1 2) POINT (3 4)", "expected the end of the text",
				             () => reader.ReadPoint("POINT (1 2) POINT (3 4)"))
			            };

			foreach ((string wkt, string expected, TestDelegate read) in cases)
			{
				var ex = Assert.Throws<FormatException>(read, $"'{wkt}' should not parse");

				Assert.That(ex.Message, Does.Contain(expected), wkt);
				Assert.That(ex.Message, Does.Contain("position"), wkt);
			}
		}

		[Test]
		public void ReadingAsTheWrongTypeIsRefused()
		{
			var reader = new WktGeomReader();

			Assert.Throws<NotSupportedException>(
				() => reader.ReadPolygon("LINESTRING (0 0, 10 0)"));
			Assert.Throws<NotSupportedException>(
				() => reader.ReadPoint("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))"));
			Assert.Throws<NotSupportedException>(
				() => reader.ReadMultiSurface("MULTIPOLYGON (((0 0, 0 10, 10 10, 10 0, 0 0)))"));
		}

		#endregion
	}
}

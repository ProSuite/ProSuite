using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;
using Wkx;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Point = Wkx.Point;
using Polygon = Wkx.Polygon;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class WkbSerializerTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanReadWritePoint()
		{
			IPoint pointXy = GeometryFactory.CreatePoint(2600000, 1200000);
			IPoint pointXyz = GeometryFactory.CreatePoint(2600000, 1200000, 400);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb2D = writer.WritePoint(pointXy);
			byte[] wkb3D = writer.WritePoint(pointXyz);

			// Compare with AO (2D only)
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(pointXy);
			Assert.AreEqual(arcObjectsWkb, wkb2D);

			// Compare with Wkx
			byte[] wkx2D = ToChristianSchwarzWkb(pointXy);
			Assert.AreEqual(wkx2D, wkb2D);

			byte[] wkx3D = ToChristianSchwarzWkb(pointXyz);
			Assert.AreEqual(wkx3D, wkb3D);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			Pnt3D pnt2D = new Pnt3D(pointXy.X, pointXy.Y, double.NaN);
			byte[] bytesGeom2D = geomWriter.WritePoint(pnt2D, Ordinates.Xy);
			Assert.AreEqual(wkb2D, bytesGeom2D);

			Pnt3D pnt3D = new Pnt3D(pointXyz.X, pointXyz.Y, pointXyz.Z);
			byte[] bytesGeom3D = geomWriter.WritePoint(pnt3D);
			Assert.AreEqual(wkb3D, bytesGeom3D);

			// NOTE: Comparison with ST_Geometry SDE.ST_AsBinary(SHAPE) using nHibernate
			// connection is performed in StGeometryPerimeterRepositoryTest

			// Read back:
			WkbGeometryReader reader = new WkbGeometryReader();

			IPoint restored = reader.ReadPoint(new MemoryStream(wkb2D));
			Assert.IsTrue(GeometryUtils.AreEqual(pointXy, restored));

			restored = reader.ReadPoint(new MemoryStream(wkb3D));
			Assert.IsTrue(GeometryUtils.AreEqual(pointXyz, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(pnt2D.Equals(geomReader.ReadPoint(new MemoryStream(bytesGeom2D))));

			Assert.IsTrue(pnt3D.Equals(geomReader.ReadPoint(new MemoryStream(bytesGeom3D))));
		}

		[Test]
		public void CanReadWriteMultipointXy()
		{
			var points = new WKSPointZ[4];

			points[0] = new WKSPointZ {X = 2600000, Y = 1200000, Z = double.NaN};
			points[1] = new WKSPointZ {X = 2600030, Y = 1200020, Z = double.NaN};
			points[2] = new WKSPointZ {X = 2600020, Y = 1200030, Z = double.NaN};
			points[3] = new WKSPointZ {X = 2600040, Y = 1200040, Z = double.NaN};

			ISpatialReference sr =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IMultipoint multipoint = GeometryFactory.CreateMultipoint(points, sr);
			GeometryUtils.MakeNonZAware(multipoint);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WriteMultipoint(multipoint);

			// ArcObjects
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(multipoint);
			Assert.AreEqual(wkb, arcObjectsWkb);

			// Wkx
			byte[] wkx = ToChristianSchwarzWkb(ToWkxMultipoint(points, Ordinates.Xy));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			Multipoint<IPnt> multipnt = GeometryConversionUtils.CreateMultipoint(multipoint);
			byte[] wkbGeom = geomWriter.WriteMultipoint(multipnt, Ordinates.Xy);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IMultipoint restored = reader.ReadMultipoint(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(multipoint, restored));

			// Geom
			WkbGeomReader geomReader = new WkbGeomReader();

			Multipoint<IPnt> deserializedPnts =
				geomReader.ReadMultiPoint(new MemoryStream(wkbGeom));

			Assert.IsTrue(
				GeomRelationUtils.AreEqualXY(multipnt, deserializedPnts,
				                             double.Epsilon));
		}

		[Test]
		public void CanReadWriteMultipointXyz()
		{
			var points = new WKSPointZ[4];

			points[0] = new WKSPointZ {X = 2600000, Y = 1200000, Z = 456};
			points[1] = new WKSPointZ {X = 2600030, Y = 1200020, Z = 457};
			points[2] = new WKSPointZ {X = 2600020, Y = 1200030, Z = 459};
			points[3] = new WKSPointZ {X = 2600010, Y = 1200010, Z = 416};

			ISpatialReference sr =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IMultipoint multipoint = GeometryFactory.CreateMultipoint(points, sr);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WriteMultipoint(multipoint);

			// Wkx
			byte[] wkx = ToChristianSchwarzWkb(ToWkxMultipoint(points, Ordinates.Xyz));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			Multipoint<IPnt> multipnt = GeometryConversionUtils.CreateMultipoint(multipoint);
			byte[] wkbGeom = geomWriter.WriteMultipoint(multipnt, Ordinates.Xyz);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IMultipoint restored = reader.ReadMultipoint(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(multipoint, restored));

			// Geom
			WkbGeomReader geomReader = new WkbGeomReader();

			Multipoint<IPnt> deserializedPnts =
				geomReader.ReadMultiPoint(new MemoryStream(wkbGeom));
			Assert.IsTrue(multipnt.Equals(deserializedPnts));
		}

		[Test]
		public void CanReadWriteSinglePartPolylineXy()
		{
			var points = new WKSPointZ[4];

			points[0] = new WKSPointZ {X = 2600000, Y = 1200000, Z = double.NaN};
			points[1] = new WKSPointZ {X = 2600030, Y = 1200020, Z = double.NaN};
			points[2] = new WKSPointZ {X = 2600020, Y = 1200030, Z = double.NaN};
			points[3] = new WKSPointZ {X = 2600040, Y = 1200040, Z = double.NaN};

			IPolyline polyline = GeometryFactory.CreatePolyline(points, null);

			GeometryUtils.MakeNonZAware(polyline);

			GeometryUtils.Simplify(polyline);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolyline(polyline);

			// ArcObjects
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(polyline);
			Assert.AreEqual(wkb, arcObjectsWkb);

			// Wkx
			byte[] wkx = ToChristianSchwarzWkb(ToWkxLineString(points, Ordinates.Xy));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			MultiPolycurve multiPlycurve = GeometryConversionUtils.CreateMultiPolycurve(polyline);
			byte[] wkbGeom = geomWriter.WriteMultiLinestring(multiPlycurve, Ordinates.Xy);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolyline restored = reader.ReadPolyline(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(polyline, restored));

			// Geom
			WkbGeomReader geomReader = new WkbGeomReader();

			Assert.IsTrue(
				multiPlycurve.Equals(geomReader.ReadMultiPolycurve(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteSinglePartPolylineXyz()
		{
			var points = new WKSPointZ[4];

			points[0] = new WKSPointZ {X = 2600000, Y = 1200000, Z = 456};
			points[1] = new WKSPointZ {X = 2600030, Y = 1200020, Z = 457};
			points[2] = new WKSPointZ {X = 2600020, Y = 1200030, Z = 459};
			points[3] = new WKSPointZ {X = 2600010, Y = 1200010, Z = 416};

			IPolyline polyline = GeometryFactory.CreatePolyline(points, null);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolyline(polyline);

			// Wkx
			byte[] wkx = ToChristianSchwarzWkb(ToWkxLineString(points, Ordinates.Xyz));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			MultiPolycurve multiPlycurve = GeometryConversionUtils.CreateMultiPolycurve(polyline);
			byte[] wkbGeom = geomWriter.WriteMultiLinestring(multiPlycurve);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolyline restored = reader.ReadPolyline(new MemoryStream(wkb));

			Assert.IsTrue(GeometryUtils.AreEqual(polyline, restored));

			// Geom
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				multiPlycurve.Equals(geomReader.ReadMultiPolycurve(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteMultiPartPolylineXy()
		{
			// The spatial reference is important to avoid small differences in 
			// coordinates (snap to spatial reference!)
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			var points1 = new WKSPointZ[4];

			points1[0] = new WKSPointZ {X = 2600000, Y = 1200000, Z = double.NaN};
			points1[1] = new WKSPointZ {X = 2600030, Y = 1200020, Z = double.NaN};
			points1[2] = new WKSPointZ {X = 2600020, Y = 1200030, Z = double.NaN};
			points1[3] = new WKSPointZ {X = 2600040, Y = 1200040, Z = double.NaN};

			IPolyline polyline1 = GeometryFactory.CreatePolyline(points1, sr);

			var points2 = new WKSPointZ[4];

			points2[0] = new WKSPointZ {X = 2610000, Y = 1200000, Z = double.NaN};
			points2[1] = new WKSPointZ {X = 2610030, Y = 1200020, Z = double.NaN};
			points2[2] = new WKSPointZ {X = 2610020, Y = 1200030, Z = double.NaN};
			points2[3] = new WKSPointZ {X = 2610040, Y = 1200040, Z = double.NaN};

			IPolyline polyline2 = GeometryFactory.CreatePolyline(points2, sr);

			IPolyline polyline = (IPolyline) GeometryUtils.Union(polyline1, polyline2);

			GeometryUtils.MakeNonZAware(polyline);

			GeometryUtils.Simplify(polyline);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolyline(polyline);

			// ArcObjects
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(polyline);
			Assert.AreEqual(arcObjectsWkb, wkb);

			// Wkx
			var ordinates = Ordinates.Xy;
			MultiLineString multiLineString = new MultiLineString(
				new List<LineString>
				{ToWkxLineString(points1, ordinates), ToWkxLineString(points2, ordinates)});

			byte[] wkx = ToChristianSchwarzWkb(multiLineString);
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			MultiPolycurve multiPlycurve = GeometryConversionUtils.CreateMultiPolycurve(polyline);
			byte[] wkbGeom = geomWriter.WriteMultiLinestring(multiPlycurve, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolyline restored = reader.ReadPolyline(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(polyline, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				multiPlycurve.Equals(geomReader.ReadMultiPolycurve(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteMultiPartPolylineXyz()
		{
			// The spatial reference is important to avoid small differences in 
			// coordinates (snap to spatial reference!)
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			var points1 = new WKSPointZ[4];

			points1[0] = new WKSPointZ {X = 2600000, Y = 1200000, Z = 456};
			points1[1] = new WKSPointZ {X = 2600030, Y = 1200020, Z = 457};
			points1[2] = new WKSPointZ {X = 2600020, Y = 1200030, Z = 459};
			points1[3] = new WKSPointZ {X = 2600040, Y = 1200040, Z = 416};

			IPolyline polyline1 = GeometryFactory.CreatePolyline(points1, sr);

			var points2 = new WKSPointZ[4];

			points2[0] = new WKSPointZ {X = 2610000, Y = 1200000, Z = 656};
			points2[1] = new WKSPointZ {X = 2610030, Y = 1200020, Z = 657};
			points2[2] = new WKSPointZ {X = 2610020, Y = 1200030, Z = 659};
			points2[3] = new WKSPointZ {X = 2610040, Y = 1200040, Z = 616};

			IPolyline polyline2 = GeometryFactory.CreatePolyline(points2, sr);

			IPolyline polyline = (IPolyline) GeometryUtils.Union(polyline1, polyline2);

			GeometryUtils.Simplify(polyline);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolyline(polyline);

			// Wkx
			var ordinates = Ordinates.Xyz;
			MultiLineString multiLineString = new MultiLineString(
				new List<LineString>
				{ToWkxLineString(points1, ordinates), ToWkxLineString(points2, ordinates)});

			byte[] wkx = ToChristianSchwarzWkb(multiLineString);
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			MultiPolycurve multiPlycurve = GeometryConversionUtils.CreateMultiPolycurve(polyline);
			byte[] wkbGeom = geomWriter.WriteMultiLinestring(multiPlycurve, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolyline restored = reader.ReadPolyline(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(polyline, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				multiPlycurve.Equals(geomReader.ReadMultiPolycurve(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteSingleRingPolygonXy()
		{
			IPolygon poly = GeometryFactory.CreatePolygon(2600000, 1200000, 2601000, 1201000);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolygon(poly);

			// ArcObjects
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(poly);
			Assert.AreEqual(arcObjectsWkb, wkb);

			// Wkx
			var ordinates = Ordinates.Xy;
			LinearRing wkxLinearRing =
				ToWkxLinearRing(GeometryUtils.GetWKSPointZs(poly), ordinates);
			byte[] wkx = ToChristianSchwarzWkb(ToWkxPolygon(wkxLinearRing));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			RingGroup ringGroup = GeometryConversionUtils.CreateRingGroup(poly);
			byte[] wkbGeom = geomWriter.WritePolygon(ringGroup, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolygon restored = reader.ReadPolygon(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(poly, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				ringGroup.Equals(geomReader.ReadPolygon(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteSingleRingPolygonXyz()
		{
			IPolygon poly = GeometryFactory.CreatePolygon(2600000, 1200000, 2601000, 1201000, 400);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolygon(poly);

			// Wkx
			var ordinates = Ordinates.Xyz;
			LinearRing wkxLinearRing =
				ToWkxLinearRing(GeometryUtils.GetWKSPointZs(poly), ordinates);
			byte[] wkx = ToChristianSchwarzWkb(ToWkxPolygon(wkxLinearRing));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			RingGroup ringGroup = GeometryConversionUtils.CreateRingGroup(poly);
			byte[] wkbGeom = geomWriter.WritePolygon(ringGroup, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolygon restored = reader.ReadPolygon(new MemoryStream(wkb));

			Assert.IsTrue(GeometryUtils.AreEqual(poly, restored));

			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				ringGroup.Equals(geomReader.ReadPolygon(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteSingleExteriorRingWithIslandsPolygonXy()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPolygon outerRing =
				GeometryFactory.CreatePolygon(2600000, 1200000, 2601000, 1201000, sr);
			IPolygon innerRing =
				GeometryFactory.CreatePolygon(2600100, 1200100, 2600200, 1200200, sr);

			IPolygon polyWithHole = (IPolygon) IntersectionUtils.Difference(outerRing, innerRing);

			WkbGeometryWriter writer = new WkbGeometryWriter();
			byte[] wkb = writer.WritePolygon(polyWithHole);

			// ArcObjects
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(polyWithHole);
			Assert.AreEqual(arcObjectsWkb, wkb);

			// Wkx
			var ordinates = Ordinates.Xy;
			IGeometry exteriorRing = GeometryUtils.GetParts(polyWithHole).First();
			IGeometry interiorRing = GeometryUtils.GetPaths(polyWithHole).Last();

			LinearRing wkxOuterRing = ToWkxLinearRing(exteriorRing, ordinates);
			LinearRing wkxInnerRing = ToWkxLinearRing(interiorRing, ordinates);

			byte[] wkx = ToChristianSchwarzWkb(ToWkxPolygon(wkxOuterRing, new[] {wkxInnerRing}));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			RingGroup ringGroup = GeometryConversionUtils.CreateRingGroup(polyWithHole);
			byte[] wkbGeom = geomWriter.WritePolygon(ringGroup, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolygon restored = reader.ReadPolygon(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(polyWithHole, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				ringGroup.Equals(geomReader.ReadPolygon(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteSingleExteriorRingWithIslandsPolygonXyz()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPolygon outerRing =
				GeometryFactory.CreatePolygon(2600000, 1200000, 2601000, 1201000, 432);
			outerRing.SpatialReference = sr;

			IPolygon innerRing =
				GeometryFactory.CreatePolygon(2600100, 1200100, 2600200, 1200200, 321);
			innerRing.SpatialReference = sr;

			IPolygon polyWithHole = (IPolygon) IntersectionUtils.Difference(outerRing, innerRing);

			WkbGeometryWriter writer = new WkbGeometryWriter();

			byte[] wkb = writer.WritePolygon(polyWithHole);

			// Wkx
			var ordinates = Ordinates.Xyz;
			IGeometry exteriorRing = GeometryUtils.GetParts(polyWithHole).First();
			IGeometry interiorRing = GeometryUtils.GetPaths(polyWithHole).Last();

			LinearRing wkxOuterRing = ToWkxLinearRing(exteriorRing, ordinates);
			LinearRing wkxInnerRing = ToWkxLinearRing(interiorRing, ordinates);

			byte[] wkx = ToChristianSchwarzWkb(ToWkxPolygon(wkxOuterRing, new[] {wkxInnerRing}));
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			RingGroup ringGroup = GeometryConversionUtils.CreateRingGroup(polyWithHole);
			byte[] wkbGeom = geomWriter.WritePolygon(ringGroup, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolygon restored = reader.ReadPolygon(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(polyWithHole, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			Assert.IsTrue(
				ringGroup.Equals(geomReader.ReadPolygon(new MemoryStream(wkbGeom))));
		}

		[Test]
		public void CanReadWriteMultipartPolygonXy()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPolygon outerRing =
				GeometryFactory.CreatePolygon(2600000, 1200000, 2601000, 1201000, sr);
			IPolygon innerRing =
				GeometryFactory.CreatePolygon(2600100, 1200100, 2600200, 1200200, sr);

			IPolygon polyWithHole = (IPolygon) IntersectionUtils.Difference(outerRing, innerRing);

			IPolygon poly2 = GeometryFactory.CreatePolygon(2610000, 1200000, 2611000, 1201000, sr);

			IPolygon multiPolygon = (IPolygon) GeometryUtils.Union(polyWithHole, poly2);

			Assert.IsFalse(GeometryUtils.IsZAware(multiPolygon));

			GeometryUtils.Simplify(multiPolygon);

			WkbGeometryWriter writer = new WkbGeometryWriter();
			byte[] wkb = writer.WritePolygon(multiPolygon);

			// ArcObjects
			byte[] arcObjectsWkb = GeometryUtils.ToWkb(multiPolygon);
			Assert.AreEqual(arcObjectsWkb, wkb);

			// Wkx
			var ordinates = Ordinates.Xy;
			IList<IGeometry> parts = GeometryUtils.GetParts(multiPolygon).ToList();
			IGeometry exteriorRing = parts[0];
			IGeometry interiorRing = parts[1];
			IGeometry secondExteriorRing = parts[2];

			LinearRing wkxOuterRing = ToWkxLinearRing(exteriorRing, ordinates);
			LinearRing wkxInnerRing = ToWkxLinearRing(interiorRing, ordinates);

			Polygon wkxPolygon1 = ToWkxPolygon(wkxOuterRing, new[] {wkxInnerRing});
			Polygon wkxPolygon2 = ToWkxPolygon(ToWkxLinearRing(secondExteriorRing, ordinates));

			MultiPolygon wkxMultiPolygon = new MultiPolygon(new[] {wkxPolygon1, wkxPolygon2});

			byte[] wkx = ToChristianSchwarzWkb(wkxMultiPolygon);
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			MultiPolycurve multiPly = GeometryConversionUtils.CreateMultiPolycurve(multiPolygon);
			byte[] wkbGeom = geomWriter.WriteMultipolygon(multiPly, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolygon restored = reader.ReadPolygon(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(multiPolygon, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			IList<RingGroup> readMultiPolygon =
				geomReader.ReadMultiPolygon(new MemoryStream(wkbGeom));
			var readMultiPolycurve = new MultiPolycurve(
				readMultiPolygon.SelectMany(g => g.GetLinestrings()));

			Assert.IsTrue(multiPly.Equals(readMultiPolycurve));
		}

		[Test]
		public void CanReadWriteMultipartPolygonXyz()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPolygon outerRing =
				GeometryFactory.CreatePolygon(2600000, 1200000, 2601000, 1201000, 432);
			outerRing.SpatialReference = sr;
			IPolygon innerRing =
				GeometryFactory.CreatePolygon(2600100, 1200100, 2600200, 1200200, 421);
			innerRing.SpatialReference = sr;

			IPolygon polyWithHole = (IPolygon) IntersectionUtils.Difference(outerRing, innerRing);

			IPolygon poly2 = GeometryFactory.CreatePolygon(2610000, 1200000, 2611000, 1201000, 543);
			poly2.SpatialReference = sr;

			IPolygon multiPolygon = (IPolygon) GeometryUtils.Union(polyWithHole, poly2);

			Assert.IsTrue(GeometryUtils.IsZAware(multiPolygon));

			GeometryUtils.Simplify(multiPolygon);

			WkbGeometryWriter writer = new WkbGeometryWriter();
			byte[] wkb = writer.WritePolygon(multiPolygon);

			// Wkx
			var ordinates = Ordinates.Xyz;
			IList<IGeometry> parts = GeometryUtils.GetParts(multiPolygon).ToList();
			IGeometry exteriorRing = parts[0];
			IGeometry interiorRing = parts[1];
			IGeometry secondExteriorRing = parts[2];

			LinearRing wkxOuterRing = ToWkxLinearRing(exteriorRing, ordinates);
			LinearRing wkxInnerRing = ToWkxLinearRing(interiorRing, ordinates);

			Polygon wkxPolygon1 = ToWkxPolygon(wkxOuterRing, new[] {wkxInnerRing});
			Polygon wkxPolygon2 = ToWkxPolygon(ToWkxLinearRing(secondExteriorRing, ordinates));

			MultiPolygon wkxMultiPolygon = new MultiPolygon(new[] {wkxPolygon1, wkxPolygon2});

			byte[] wkx = ToChristianSchwarzWkb(wkxMultiPolygon);
			Assert.AreEqual(wkx, wkb);

			// Bonus test: Geom
			WkbGeomWriter geomWriter = new WkbGeomWriter();
			MultiPolycurve multiPly = GeometryConversionUtils.CreateMultiPolycurve(multiPolygon);
			byte[] wkbGeom = geomWriter.WriteMultipolygon(multiPly, ordinates);
			Assert.AreEqual(wkb, wkbGeom);

			WkbGeometryReader reader = new WkbGeometryReader();

			IPolygon restored = reader.ReadPolygon(new MemoryStream(wkb));
			Assert.IsTrue(GeometryUtils.AreEqual(multiPolygon, restored));

			// Geom:
			WkbGeomReader geomReader = new WkbGeomReader();
			IList<RingGroup> readMultiPolygon =
				geomReader.ReadMultiPolygon(new MemoryStream(wkbGeom));
			var readMultiPolycurve = new MultiPolycurve(
				readMultiPolygon.SelectMany(g => g.GetLinestrings()));

			Assert.IsTrue(multiPly.Equals(readMultiPolycurve));

			// As multipatch
			IMultiPatch multipatch = GeometryFactory.CreateMultiPatch(multiPolygon);

			wkb = writer.WriteMultipatch(multipatch);

			IMultiPatch readMultipatch = (IMultiPatch) reader.ReadGeometry(new MemoryStream(wkb));

			// TODO: Implement AreEqual for multipatch that is more permissive regarding First/Outer ring type
			AssertEqual(multipatch, readMultipatch);
		}

		[Test]
		public void CanConvertMultipatchWithOuterInnerRingSequence()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IRing outerRing0 = GeometryUtils.GetRings(
				                                GeometryFactory.CreatePolygon(
					                                2601000, 1200000, 2602000, 1201000, 432))
			                                .Single();
			outerRing0.SpatialReference = sr;

			IRing outerRing1 = GeometryUtils.GetRings(
				                                GeometryFactory.CreatePolygon(
					                                2600000, 1200000, 2601000, 1201000, 432))
			                                .Single();
			outerRing1.SpatialReference = sr;

			IRing innerRing = GeometryFactory.CreateRing(new[]
			                                             {
				                                             new WKSPointZ()
				                                             {X = 2600100, Y = 1200100, Z = 432},
				                                             new WKSPointZ()
				                                             {X = 2600200, Y = 1200100, Z = 432},
				                                             new WKSPointZ()
				                                             {X = 2600200, Y = 1200200, Z = 432},
				                                             new WKSPointZ()
				                                             {X = 2600100, Y = 1200200, Z = 432},
				                                             new WKSPointZ()
				                                             {X = 2600100, Y = 1200100, Z = 432},
			                                             });
			innerRing.SpatialReference = sr;

			IMultiPatch multipatch = GeometryFactory.CreateEmptyMultiPatch(outerRing0);

			object emptyRef = Type.Missing;

			((IGeometryCollection) multipatch).AddGeometry(outerRing0, ref emptyRef, ref emptyRef);
			multipatch.PutRingType(outerRing0, esriMultiPatchRingType.esriMultiPatchFirstRing);

			((IGeometryCollection) multipatch).AddGeometry(outerRing1, ref emptyRef, ref emptyRef);
			multipatch.PutRingType(outerRing1, esriMultiPatchRingType.esriMultiPatchOuterRing);

			((IGeometryCollection) multipatch).AddGeometry(innerRing, ref emptyRef, ref emptyRef);
			multipatch.PutRingType(innerRing, esriMultiPatchRingType.esriMultiPatchInnerRing);

			WkbGeometryWriter writer = new WkbGeometryWriter();
			byte[] wkb = writer.WriteMultipatch(multipatch);

			WkbGeometryReader reader = new WkbGeometryReader();
			IGeometry rehydrated = reader.ReadGeometry(new MemoryStream(wkb));

			Assert.IsTrue(GeometryUtils.AreEqual(multipatch, rehydrated));
		}

		[Test]
		public void CanConvertMultipatchesSpecialCasesWithInnerRings()
		{
			// NOTE: XML serialization for multipatches loses inner rings
			//       -> Use wkb in the first place.
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			var mockFeature = TestUtils.CreateMockFeature("mediamarktMuriBB.wkb", sr);

			IMultiPatch rehydrated = AssertWkbSerialization(mockFeature, false);
			Assert.AreEqual(1, CountInnerRings(rehydrated));
			rehydrated = AssertWkbSerialization(mockFeature, true);
			Assert.AreEqual(1, CountInnerRings(rehydrated));
		}

		private static int CountInnerRings(IMultiPatch multipatch)
		{
			int innerRingCount = 0;
			foreach (IRing ring in GeometryUtils.GetRings(multipatch))
			{
				bool isBeginning = true;
				esriMultiPatchRingType ringType = multipatch.GetRingType(ring, ref isBeginning);

				if (ringType == esriMultiPatchRingType.esriMultiPatchInnerRing)
				{
					innerRingCount++;
				}
			}

			return innerRingCount;
		}

		[Ignore(
			"Takes a long time and fails because IMultipatch.GetRingType() can return the wrong type if there are identical rings")]
		[Test]
		public void CanConvertMultipatchesAllBuildings()
		{
			IWorkspace workspace = TestUtils.OpenSDEWorkspaceOracle();

			IFeatureClass featureClass = DatasetUtils.OpenFeatureClass(
				workspace, "TOPGIS_TLM.TLM_GEBAEUDE");

			int count = 0;
			foreach (IFeature feature in GdbQueryUtils.GetFeatures(featureClass, true))
			{
				count++;

				AssertWkbSerialization(feature, false);
			}
		}

		[Ignore("Takes a long time and fails because some buildings have gaps in their point IDs")]
		[Test]
		public void CanConvertMultipatchesAllBuildingsGroupedByPointId()
		{
			IWorkspace workspace = TestUtils.OpenSDEWorkspaceOracle();

			IFeatureClass featureClass = DatasetUtils.OpenFeatureClass(
				workspace, "TOPGIS_TLM.TLM_GEBAEUDE");

			int count = 0;
			foreach (IFeature feature in GdbQueryUtils.GetFeatures(featureClass, true))
			{
				count++;

				AssertWkbSerialization(feature, true);
			}
		}

		private IMultiPatch AssertWkbSerialization(IFeature multipatchFeature,
		                                           bool groupPartsByPointId)
		{
			IMultiPatch multipatch = (IMultiPatch) multipatchFeature.Shape;
			IMultiPatch rehydrated = null;
			groupPartsByPointId = groupPartsByPointId && GeometryUtils.IsPointIDAware(multipatch);

			if (! groupPartsByPointId)
			{
				var pointIDAware = (IPointIDAware) multipatch;
				pointIDAware.PointIDAware = false;
			}

			try
			{
				WkbGeometryWriter writer = new WkbGeometryWriter();
				byte[] wkb = writer.WriteMultipatch(multipatch, groupPartsByPointId);

				WkbGeometryReader reader = new WkbGeometryReader()
				                           {
					                           GroupPolyhedraByPointId = groupPartsByPointId
				                           };

				rehydrated = reader.ReadMultipatch(new MemoryStream(wkb));

				rehydrated.SpatialReference = multipatch.SpatialReference;

				AssertEqual(multipatch, rehydrated);

				// This does not compare ring types
				Assert.AreEqual(GeometryUtils.ToXmlString(multipatch),
				                GeometryUtils.ToXmlString(rehydrated),
				                $"GEBAEUDE {multipatchFeature.OID} failed xml test");

				// Most of the features fail this test, even though they are identical
				//Assert.IsTrue(GeometryUtils.AreEqual(multipatch, rehydrated),
				//              $"GEBAEUDE {feature.OID} failed equality test");
			}
			catch
			{
				Console.WriteLine("Error serializing/deserializing feature {0}",
				                  multipatchFeature.OID);

				GeometryUtils.ToXmlFile(multipatch, @"C:\temp\orig.xml");

				if (rehydrated != null)
				{
					GeometryUtils.ToXmlFile(rehydrated, @"C:\temp\rehydrated.xml");
				}

				throw;
			}

			return rehydrated;
		}

		private static void AssertEqual(IMultiPatch multipatch1, IMultiPatch multipatch2)
		{
			var originalCollection = (IGeometryCollection) multipatch1;
			Assert.AreEqual(originalCollection.GeometryCount,
			                ((IGeometryCollection) multipatch2).GeometryCount);

			for (int i = 0; i < originalCollection.GeometryCount; i++)
			{
				IRing originalRing = (IRing) originalCollection.Geometry[i];
				IRing rehydratedRing = (IRing) ((IGeometryCollection) multipatch2).Geometry[i];

				// This does not compare point IDs
				GeometryUtils.AreEqual(originalRing, rehydratedRing);

				bool isBeginning = false;
				esriMultiPatchRingType origType =
					multipatch1.GetRingType(originalRing, ref isBeginning);
				esriMultiPatchRingType rehydratedType =
					multipatch2.GetRingType(rehydratedRing, ref isBeginning);

				if (origType == esriMultiPatchRingType.esriMultiPatchInnerRing)
					Assert.AreEqual(origType, rehydratedType);
				else if (origType == esriMultiPatchRingType.esriMultiPatchOuterRing ||
				         origType == esriMultiPatchRingType.esriMultiPatchFirstRing)
				{
					Assert.IsTrue(
						rehydratedType == esriMultiPatchRingType.esriMultiPatchOuterRing ||
						rehydratedType == esriMultiPatchRingType.esriMultiPatchFirstRing);
				}
				else
				{
					Assert.AreEqual(origType, rehydratedType);
				}
			}
		}

		private static byte[] ToChristianSchwarzWkb(IPoint point)
		{
			Point wkxPoint = GeometryUtils.IsZAware(point)
				                 ? new Point(point.X, point.Y, point.Z)
				                 : new Point(point.X, point.Y);

			WkbSerializer serializer = new WkbSerializer();

			MemoryStream stream = new MemoryStream();
			serializer.Serialize(wkxPoint, stream);

			return stream.ToArray();
		}

		private static byte[] ToChristianSchwarzWkb(Wkx.Geometry wkxGeometry)
		{
			WkbSerializer serializer = new WkbSerializer();

			MemoryStream stream = new MemoryStream();
			serializer.Serialize(wkxGeometry, stream);

			return stream.ToArray();
		}

		private static LineString ToWkxLineString(WKSPointZ[] wksPoints,
		                                          Ordinates ordinates)
		{
			return new LineString(
				wksPoints.Select(wkp => ToWkxPoint(wkp, ordinates)));
		}

		private static LinearRing ToWkxLinearRing(IGeometry ringGeometry,
		                                          Ordinates ordinates)
		{
			WKSPointZ[] wksPointZs = GeometryUtils.GetWKSPointZs(ringGeometry);

			return ToWkxLinearRing(wksPointZs, ordinates);
		}

		private static LinearRing ToWkxLinearRing(WKSPointZ[] wksPoints,
		                                          Ordinates ordinates,
		                                          bool reverse = true)

		{
			// Reverse all rings except outer rings to be added as inner rings.
			// wkxGeometry uses OGC 1.2 winding order

			IEnumerable<WKSPointZ> pointEnum =
				reverse ? wksPoints.Reverse() : wksPoints;

			return new LinearRing(
				pointEnum.Select(wkp => ToWkxPoint(wkp, ordinates)));
		}

		private static Polygon ToWkxPolygon([NotNull] LinearRing exteriorRing,
		                                    [CanBeNull] IEnumerable<LinearRing> interiorRings =
			                                    null)

		{
			return interiorRings != null
				       ? new Polygon(exteriorRing, interiorRings)
				       : new Polygon(exteriorRing);
		}

		private static Point ToWkxPoint(WKSPointZ wksPoint,
		                                Ordinates ordinates)
		{
			switch (ordinates)
			{
				case Ordinates.Xy:
					return new Point(wksPoint.X, wksPoint.Y);
				case Ordinates.Xyz:
					return new Point(wksPoint.X, wksPoint.Y, wksPoint.Z);
				default:
					throw new NotImplementedException();
			}
		}

		private static MultiPoint ToWkxMultipoint(WKSPointZ[] points, Ordinates xy)
		{
			var result = new MultiPoint(points.Select(p => ToWkxPoint(p, xy)));

			return result;
		}
	}
}

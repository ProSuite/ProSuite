﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.Wkb;
using Wkx;
using Point = Wkx.Point;
using Polygon = Wkx.Polygon;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class WkbSerializerTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
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
		}

		private static byte[] ToChristianSchwarzWkb(IPoint point)
		{
			Point wkxPoint = (GeometryUtils.IsZAware(point))
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
	}
}
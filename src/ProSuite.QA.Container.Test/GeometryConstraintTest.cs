using System;
using System.Diagnostics;
using System.Globalization;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class GeometryConstraintTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanTestMultiPatch()
		{
			IPolygon polygon =
				GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(0, 0, 10, 10,
				                                                             100, 100));

			IMultiPatch multiPatch = GeometryFactory.CreateMultiPatch(polygon, 10);
			multiPatch.SpatialReference = CreateSpatialReference();
			multiPatch.SnapToSpatialReference();

			AssertFulfilled("$ISCLOSED IS NULL", multiPatch);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", multiPatch);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", multiPatch);
			AssertFulfilled("$BEZIERCOUNT = 0", multiPatch);
			AssertFulfilled("$SEGMENTCOUNT = 0", multiPatch);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", multiPatch);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", multiPatch);
			AssertFulfilled("$PARTCOUNT = 3", multiPatch); // 2 rings, one triangle strip
			AssertFulfilled("$VERTEXCOUNT = 20", multiPatch);
			AssertFulfilled("$ISMULTIPART", multiPatch);
			AssertFulfilled("$AREA = 100", multiPatch);
			AssertFulfilled("$LENGTH = 40", multiPatch);
			AssertFulfilled("$SLIVERRATIO = 16", multiPatch);
			AssertFulfilled("$DIMENSION = 2", multiPatch);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", multiPatch);
			AssertFulfilled("$EXTERIORRINGCOUNT = 2", multiPatch);
			AssertFulfilled("$RINGCOUNT = 2", multiPatch);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", multiPatch);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 1", multiPatch);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", multiPatch);
			AssertFulfilled("NOT $ISEXTERIORRING", multiPatch);
			AssertFulfilled("NOT $ISINTERIORRING", multiPatch);
			AssertFulfilled("$XMIN = 0", multiPatch);
			AssertFulfilled("$YMIN = 0", multiPatch);
			AssertFulfilled("$XMAX = 10", multiPatch);
			AssertFulfilled("$YMAX = 10", multiPatch);
			AssertFulfilled("$ZMIN = 100", multiPatch);
			AssertFulfilled("$ZMAX = 110", multiPatch);
			AssertFulfilled("$MMIN IS NULL", multiPatch);
			AssertFulfilled("$MMAX IS NULL", multiPatch);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 20", multiPatch);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", multiPatch);
		}

		[Test]
		public void CanTestEmptyMultiPatch()
		{
			var geometry = new MultiPatchClass();

			AssertFulfilled("$ISCLOSED IS NULL", geometry);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", geometry);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", geometry);
			AssertFulfilled("$BEZIERCOUNT = 0", geometry);
			AssertFulfilled("$SEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$PARTCOUNT = 0", geometry);
			AssertFulfilled("$VERTEXCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISMULTIPART", geometry);
			AssertFulfilled("$AREA = 0", geometry);
			AssertFulfilled("$LENGTH = 0", geometry);
			AssertFulfilled("$SLIVERRATIO IS NULL", geometry);
			AssertFulfilled("$DIMENSION = 2", geometry);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$RINGCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISEXTERIORRING", geometry);
			AssertFulfilled("NOT $ISINTERIORRING", geometry);
			AssertFulfilled("$XMIN IS NULL", geometry);
			AssertFulfilled("$YMIN IS NULL", geometry);
			AssertFulfilled("$XMAX IS NULL", geometry);
			AssertFulfilled("$YMAX IS NULL", geometry);
			AssertFulfilled("$ZMIN IS NULL", geometry);
			AssertFulfilled("$ZMAX IS NULL", geometry);
			AssertFulfilled("$MMIN IS NULL", geometry);
			AssertFulfilled("$MMAX IS NULL", geometry);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", geometry);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", geometry);
		}

		[Test]
		public void CanTestEmptyMultiPoint()
		{
			var geometry = new MultipointClass();

			AssertFulfilled("$ISCLOSED IS NULL", geometry);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", geometry);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", geometry);
			AssertFulfilled("$BEZIERCOUNT = 0", geometry);
			AssertFulfilled("$SEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$PARTCOUNT = 0", geometry);
			AssertFulfilled("$VERTEXCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISMULTIPART", geometry);
			AssertFulfilled("$AREA = 0", geometry);
			AssertFulfilled("$LENGTH = 0", geometry);
			AssertFulfilled("$SLIVERRATIO IS NULL", geometry);
			AssertFulfilled("$DIMENSION = 0", geometry);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$RINGCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISEXTERIORRING", geometry);
			AssertFulfilled("NOT $ISINTERIORRING", geometry);
			AssertFulfilled("$XMIN IS NULL", geometry);
			AssertFulfilled("$YMIN IS NULL", geometry);
			AssertFulfilled("$XMAX IS NULL", geometry);
			AssertFulfilled("$YMAX IS NULL", geometry);
			AssertFulfilled("$ZMIN IS NULL", geometry);
			AssertFulfilled("$ZMAX IS NULL", geometry);
			AssertFulfilled("$MMIN IS NULL", geometry);
			AssertFulfilled("$MMAX IS NULL", geometry);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", geometry);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", geometry);
		}

		[Test]
		public void CanTestEmptyPoint()
		{
			var geometry = new PointClass();

			AssertFulfilled("$ISCLOSED IS NULL", geometry);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", geometry);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", geometry);
			AssertFulfilled("$BEZIERCOUNT = 0", geometry);
			AssertFulfilled("$SEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$PARTCOUNT = 0", geometry);
			AssertFulfilled("$VERTEXCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISMULTIPART", geometry);
			AssertFulfilled("$AREA = 0", geometry);
			AssertFulfilled("$LENGTH = 0", geometry);
			AssertFulfilled("$SLIVERRATIO IS NULL", geometry);
			AssertFulfilled("$DIMENSION = 0", geometry);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$RINGCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISEXTERIORRING", geometry);
			AssertFulfilled("NOT $ISINTERIORRING", geometry);
			AssertFulfilled("$XMIN IS NULL", geometry);
			AssertFulfilled("$YMIN IS NULL", geometry);
			AssertFulfilled("$XMAX IS NULL", geometry);
			AssertFulfilled("$YMAX IS NULL", geometry);
			AssertFulfilled("$ZMIN IS NULL", geometry);
			AssertFulfilled("$ZMAX IS NULL", geometry);
			AssertFulfilled("$MMIN IS NULL", geometry);
			AssertFulfilled("$MMAX IS NULL", geometry);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", geometry);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", geometry);
		}

		[Test]
		public void CanTestEmptyPolygon()
		{
			var geometry = new PolygonClass();

			AssertFulfilled("$ISCLOSED", geometry); // Note: True for empty polygon!
			AssertFulfilled("$CIRCULARARCCOUNT = 0", geometry);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", geometry);
			AssertFulfilled("$BEZIERCOUNT = 0", geometry);
			AssertFulfilled("$SEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$PARTCOUNT = 0", geometry);
			AssertFulfilled("$VERTEXCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISMULTIPART", geometry);
			AssertFulfilled("$AREA = 0", geometry);
			AssertFulfilled("$LENGTH = 0", geometry);
			AssertFulfilled("$SLIVERRATIO IS NULL", geometry);
			AssertFulfilled("$DIMENSION = 2", geometry);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$RINGCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISEXTERIORRING", geometry);
			AssertFulfilled("NOT $ISINTERIORRING", geometry);
			AssertFulfilled("$XMIN IS NULL", geometry);
			AssertFulfilled("$YMIN IS NULL", geometry);
			AssertFulfilled("$XMAX IS NULL", geometry);
			AssertFulfilled("$YMAX IS NULL", geometry);
			AssertFulfilled("$ZMIN IS NULL", geometry);
			AssertFulfilled("$ZMAX IS NULL", geometry);
			AssertFulfilled("$MMIN IS NULL", geometry);
			AssertFulfilled("$MMAX IS NULL", geometry);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", geometry);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", geometry);
		}

		[Test]
		public void CanTestEmptyPolyline()
		{
			var geometry = new PolylineClass();

			AssertFulfilled("$ISCLOSED", geometry); // Note: True for empty polyline!
			AssertFulfilled("$CIRCULARARCCOUNT = 0", geometry);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", geometry);
			AssertFulfilled("$BEZIERCOUNT = 0", geometry);
			AssertFulfilled("$SEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", geometry);
			AssertFulfilled("$PARTCOUNT = 0", geometry);
			AssertFulfilled("$VERTEXCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISMULTIPART", geometry);
			AssertFulfilled("$AREA = 0", geometry);
			AssertFulfilled("$LENGTH = 0", geometry);
			AssertFulfilled("$SLIVERRATIO IS NULL", geometry);
			AssertFulfilled("$DIMENSION = 1", geometry);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", geometry);
			AssertFulfilled("$RINGCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", geometry);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", geometry);
			AssertFulfilled("NOT $ISEXTERIORRING", geometry);
			AssertFulfilled("NOT $ISINTERIORRING", geometry);
			AssertFulfilled("$XMIN IS NULL", geometry);
			AssertFulfilled("$YMIN IS NULL", geometry);
			AssertFulfilled("$XMAX IS NULL", geometry);
			AssertFulfilled("$YMAX IS NULL", geometry);
			AssertFulfilled("$ZMIN IS NULL", geometry);
			AssertFulfilled("$ZMAX IS NULL", geometry);
			AssertFulfilled("$MMIN IS NULL", geometry);
			AssertFulfilled("$MMAX IS NULL", geometry);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", geometry);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", geometry);
		}

		[Test]
		public void CanTestNullGeometry()
		{
			AssertFulfilled("$ISCLOSED IS NULL", null);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", null);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", null);
			AssertFulfilled("$BEZIERCOUNT = 0", null);
			AssertFulfilled("$SEGMENTCOUNT = 0", null);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", null);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", null);
			AssertFulfilled("$PARTCOUNT = 0", null);
			AssertFulfilled("$VERTEXCOUNT = 0", null);
			AssertFulfilled("NOT $ISMULTIPART", null);
			AssertFulfilled("$AREA = 0", null);
			AssertFulfilled("$LENGTH = 0", null);
			AssertFulfilled("$SLIVERRATIO IS NULL", null);
			AssertFulfilled("$DIMENSION IS NULL", null);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", null);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", null);
			AssertFulfilled("$RINGCOUNT = 0", null);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", null);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", null);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", null);
			AssertFulfilled("NOT $ISEXTERIORRING", null);
			AssertFulfilled("NOT $ISINTERIORRING", null);
			AssertFulfilled("$XMIN IS NULL", null);
			AssertFulfilled("$YMIN IS NULL", null);
			AssertFulfilled("$XMAX IS NULL", null);
			AssertFulfilled("$YMAX IS NULL", null);
			AssertFulfilled("$ZMIN IS NULL", null);
			AssertFulfilled("$ZMAX IS NULL", null);
			AssertFulfilled("$MMIN IS NULL", null);
			AssertFulfilled("$MMAX IS NULL", null);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", null);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", null);
		}

		[Test]
		public void CanDetectClosedCircularArc()
		{
			IPolygon polygon = GeometryFactory.CreateCircleArcPolygon(
				GeometryFactory.CreatePoint(100, 100), 10);

			AssertFulfilled("$ISCLOSED AND $CIRCULARARCCOUNT = 1 AND $SEGMENTCOUNT = 1",
			                polygon);
			AssertNotFulfilled(
				"NOT ($ISCLOSED AND $CIRCULARARCCOUNT = 1 AND $SEGMENTCOUNT = 1)", polygon);

			// NOTE: for some reason, the constructed polygon is not simple.
			//       However the geometry properties can deal with that.
			AssertFulfilled("$ISCLOSED AND $CIRCULARARCCOUNT = 1 AND " +
			                "$SEGMENTCOUNT = 1 AND $PARTCOUNT = 1 AND " +
			                "$VERTEXCOUNT = 2  AND NOT $ISMULTIPART AND " +
			                "$AREA > 10 AND $LENGTH > 10 AND $DIMENSION = 2 AND " +
			                "$INTERIORRINGCOUNT = 0 AND $EXTERIORRINGCOUNT = 1",
			                polygon);
		}

		[Test]
		public void CanDetectClosedCircularArcInteriorRing()
		{
			IPolygon polygon = GeometryFactory.CreateCircleArcPolygon(
				GeometryFactory.CreatePoint(100, 100), 10, isCcw: true);

			// NOTE: for some reason, the constructed polygon is not simple.
			//       However the geometry properties can deal with that.
			AssertFulfilled("$ISCLOSED AND $CIRCULARARCCOUNT = 1 AND " +
			                "$SEGMENTCOUNT = 1 AND $PARTCOUNT = 1 AND " +
			                "$VERTEXCOUNT = 2  AND NOT $ISMULTIPART AND " +
			                "$AREA > 10 AND $LENGTH > 10 AND $DIMENSION = 2 AND " +
			                "$INTERIORRINGCOUNT = 1 AND $EXTERIORRINGCOUNT = 0",
			                polygon);
		}

		[Test]
		public void CanCheckPolyline()
		{
			IPolyline line = GeometryFactory.CreatePolyline(0, 100, 1000,
			                                                10, 100, 1010);
			line.SpatialReference = CreateSpatialReference();
			line.SnapToSpatialReference(); // to allow check for double equality

			AssertFulfilled("NOT $ISCLOSED", line);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", line);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", line);
			AssertFulfilled("$BEZIERCOUNT = 0", line);
			AssertFulfilled("$SEGMENTCOUNT = 1", line);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 1", line);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", line);
			AssertFulfilled("$PARTCOUNT = 1", line);
			AssertFulfilled("$VERTEXCOUNT = 2", line);
			AssertFulfilled("NOT $ISMULTIPART", line);
			AssertFulfilled("$AREA = 0", line);
			AssertFulfilled("$LENGTH = 10", line);
			AssertFulfilled("$SLIVERRATIO IS NULL", line);
			AssertFulfilled("$DIMENSION = 1", line);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", line);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", line);
			AssertFulfilled("$RINGCOUNT = 0", line);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", line);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", line);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", line);
			AssertFulfilled("NOT $ISEXTERIORRING", line);
			AssertFulfilled("NOT $ISINTERIORRING", line);
			AssertFulfilled("$XMIN = 0", line);
			AssertFulfilled("$YMIN = 100", line);
			AssertFulfilled("$XMAX = 10", line);
			AssertFulfilled("$YMAX = 100", line);
			AssertFulfilled("$ZMIN = 1000", line);
			AssertFulfilled("$ZMAX = 1010", line);
			AssertFulfilled("$MMIN IS NULL", line);
			AssertFulfilled("$MMAX IS NULL", line);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 2", line);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", line);

			AssertFulfilled("1 = 1", line);
			AssertFulfilled(" ", line);

			AssertNotFulfilled("1 = 2", line);
			AssertNotFulfilled("$AREA > 200 AND $LENGTH > 30", line);
			AssertNotFulfilled("$SLIVERRATIO > 100", line);

			AssertFulfilled("NOT ($ISCLOSED AND $CIRCULARARCCOUNT = 1 AND $SEGMENTCOUNT = 1)",
			                line);
		}

		[Test]
		public void CanCheckPolylineMValues()
		{
			IPolyline line = GeometryFactory.CreatePolyline(0, 100, 1000,
			                                                10, 100, 1010);
			line.SpatialReference = CreateSpatialReference();

			GeometryUtils.MakeMAware(line);
			((IMSegmentation) line).SetMsAsDistance(asRatio: true);

			line.SnapToSpatialReference(); // to allow check for double equality

			AssertFulfilled("$MMIN = 0", line);
			AssertFulfilled("$MMAX = 1", line);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", line);
		}

		[Test]
		public void CanCheckPolylineControlPoints()
		{
			IPolyline line = GeometryFactory.CreatePolyline(0, 100, 1000,
			                                                10, 100, 1010);
			line.SpatialReference = CreateSpatialReference();

			GeometryUtils.MakePointIDAware(line);

			const int vertexIdx = 1;

			IPoint point = ((IPointCollection) line).Point[vertexIdx];
			point.ID = 1;
			((IPointCollection) line).UpdatePoint(vertexIdx, point);

			line.SnapToSpatialReference(); // to allow check for double equality

			AssertFulfilled("$CONTROLPOINTCOUNT = 1", line);
		}

		[Test]
		public void CanCheckPolygon()
		{
			IPolygon area = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			area.SpatialReference = CreateSpatialReference();
			area.SnapToSpatialReference(); // to allow check for double equality

			AssertFulfilled("$ISCLOSED", area);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", area);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", area);
			AssertFulfilled("$BEZIERCOUNT = 0", area);
			AssertFulfilled("$SEGMENTCOUNT = 4", area);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 4", area);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", area);
			AssertFulfilled("$PARTCOUNT = 1", area);
			AssertFulfilled("$VERTEXCOUNT = 5", area);
			AssertFulfilled("NOT $ISMULTIPART", area);
			AssertFulfilled("$AREA = 100", area);
			AssertFulfilled("$LENGTH = 40", area);
			AssertFulfilled("$SLIVERRATIO = 16", area);
			AssertFulfilled("$DIMENSION = 2", area);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", area);
			AssertFulfilled("$EXTERIORRINGCOUNT = 1", area);
			AssertFulfilled("$RINGCOUNT = 1", area);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", area);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", area);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", area);
			AssertFulfilled("NOT $ISEXTERIORRING", area);
			AssertFulfilled("NOT $ISINTERIORRING", area);
			AssertFulfilled("$XMIN = 0", area);
			AssertFulfilled("$YMIN = 0", area);
			AssertFulfilled("$XMAX = 10", area);
			AssertFulfilled("$YMAX = 10", area);
			AssertFulfilled("$ZMIN IS NULL", area);
			AssertFulfilled("$ZMAX IS NULL", area);
			AssertFulfilled("$MMIN IS NULL", area);
			AssertFulfilled("$MMAX IS NULL", area);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 5", area);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", area);

			AssertFulfilled("$SLIVERRATIO = $LENGTH * $LENGTH / $AREA", area);
			AssertFulfilled("1 = 1", area);
			AssertFulfilled(" ", area);

			AssertNotFulfilled("1 = 2", area);
			AssertNotFulfilled("$AREA > 200 AND $LENGTH > 30", area);
			AssertNotFulfilled("$SLIVERRATIO > 100", area);
		}

		[Test]
		public void CanFormatValues()
		{
			IPolygon area = GeometryFactory.CreatePolygon(0, 0, 10, 10);

			var constraint = new GeometryConstraint("$AREA > 50 AND $LENGTH > 30 AND " +
			                                        "$SLIVERRATIO < 100 AND $PARTCOUNT = 1 AND " +
			                                        "$VERTEXCOUNT = 5 AND $ISCLOSED AND NOT $ISMULTIPART");

			string values = constraint.FormatValues(area, CultureInfo.InvariantCulture);

			Console.WriteLine(values);
			// columns are formatted in alphabetic order
			Assert.AreEqual(
				"$Area=100; $IsClosed=True; $IsMultipart=False; $Length=40; $PartCount=1; $SliverRatio=16.000; $VertexCount=5",
				values);
		}

		[Test]
		public void CanFormatValuesDecimalDegrees()
		{
			IPolygon area = GeometryFactory.CreatePolygon(
				0, 0, 0.000123, 0.00000123,
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.WGS84));

			var constraint = new GeometryConstraint("$Length > 10");

			string values = constraint.FormatValues(area, CultureInfo.InvariantCulture);

			Console.WriteLine(values);
			Assert.AreEqual("$Length=0.00024846", values);
		}

		[Test]
		public void CanFormatValuesMeters()
		{
			IPolygon area = GeometryFactory.CreatePolygon(
				0, 0, 1.234, 1.234,
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

			var constraint = new GeometryConstraint("$Length > 10");

			string values = constraint.FormatValues(area, CultureInfo.InvariantCulture);

			Console.WriteLine(values);
			Assert.AreEqual("$Length=4.94", values);
		}

		[Test]
		public void CanFormatNullValue()
		{
			var geometry = new PointClass();

			string isClosedValues;
			AssertFulfilled("$ISCLOSED IS NULL", geometry, out isClosedValues);
			Assert.AreEqual("$IsClosed=<NULL>", isClosedValues);
		}

		[Test]
		public void CanCheckPoint()
		{
			IPoint point = GeometryFactory.CreatePoint(0, 10, 100, 1000);
			point.SpatialReference = CreateSpatialReference();
			point.SnapToSpatialReference();

			AssertFulfilled("$ISCLOSED IS NULL", point);
			AssertFulfilled("$CIRCULARARCCOUNT = 0", point);
			AssertFulfilled("$ELLIPTICARCCOUNT = 0", point);
			AssertFulfilled("$BEZIERCOUNT = 0", point);
			AssertFulfilled("$SEGMENTCOUNT = 0", point);
			AssertFulfilled("$LINEARSEGMENTCOUNT = 0", point);
			AssertFulfilled("$NONLINEARSEGMENTCOUNT = 0", point);
			AssertFulfilled("$PARTCOUNT = 1", point);
			AssertFulfilled("$VERTEXCOUNT = 1", point);
			AssertFulfilled("NOT $ISMULTIPART", point);
			AssertFulfilled("$AREA = 0", point);
			AssertFulfilled("$LENGTH = 0", point);
			AssertFulfilled("$SLIVERRATIO IS NULL", point);
			AssertFulfilled("$DIMENSION = 0", point);
			AssertFulfilled("$INTERIORRINGCOUNT = 0", point);
			AssertFulfilled("$EXTERIORRINGCOUNT = 0", point);
			AssertFulfilled("$RINGCOUNT = 0", point);
			AssertFulfilled("$TRIANGLEFANCOUNT = 0", point);
			AssertFulfilled("$TRIANGLESTRIPCOUNT = 0", point);
			AssertFulfilled("$TRIANGLESPATCHCOUNT = 0", point);
			AssertFulfilled("NOT $ISEXTERIORRING", point);
			AssertFulfilled("NOT $ISINTERIORRING", point);
			AssertFulfilled("$XMIN = 0", point);
			AssertFulfilled("$YMIN = 10", point);
			AssertFulfilled("$XMAX = 0", point);
			AssertFulfilled("$YMAX = 10", point);
			AssertFulfilled("$ZMIN = 100", point);
			AssertFulfilled("$ZMAX = 100", point);
			AssertFulfilled("$MMIN = 1000", point);
			AssertFulfilled("$MMAX = 1000", point);
			AssertFulfilled("$UNDEFINEDMVALUECOUNT = 0", point);
			AssertFulfilled("$CONTROLPOINTCOUNT = 0", point);

			AssertFulfilled("$AREA = 0 AND $LENGTH = 0 AND $SLIVERRATIO IS NULL AND " +
			                "$PARTCOUNT = 1 AND $VERTEXCOUNT = 1 AND $DIMENSION = 0 AND " +
			                "$ISCLOSED IS NULL AND NOT $ISMULTIPART",
			                point);
			AssertFulfilled("$SLIVERRATIO IS NULL", point);
			AssertFulfilled("1 = 1", point);
			AssertFulfilled(" ", point);

			AssertNotFulfilled("1 = 2", point);
			AssertNotFulfilled("$AREA > 200 AND $LENGTH > 30", point);
			AssertNotFulfilled("$SLIVERRATIO > 100", point);
		}

		[Test]
		public void CanCheckPolygonFastEnough()
		{
			IPolygon area = GeometryFactory.CreatePolygon(0, 0, 10, 10);

			var constraint = new GeometryConstraint("$AREA > 50 AND $LENGTH > 30 AND " +
			                                        "$SLIVERRATIO < 100 AND NOT $ISMULTIPART AND " +
			                                        "$PARTCOUNT = 1 AND $VERTEXCOUNT = 5 AND " +
			                                        "$SEGMENTCOUNT = 4 AND $ELLIPTICARCCOUNT = 0");

			var watch = new Stopwatch();
			watch.Start();

			const int count = 10000;
			for (var i = 0; i < count; i++)
			{
				Assert.True(constraint.IsFulfilled(area));
			}

			watch.Stop();
			long milliseconds = watch.ElapsedMilliseconds;

			Console.WriteLine(@"{0} s for {1:N0} operations", milliseconds / 1000d, count);

			double millisecondsPerOperation = (double) milliseconds / count;
			Console.WriteLine(@"{0} ms per operation", millisecondsPerOperation);

			const double maximumMilliseconds = 0.15;
			Assert.LessOrEqual(millisecondsPerOperation, maximumMilliseconds);
		}

		[NotNull]
		private static ISpatialReference CreateSpatialReference()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95, true);
			((ISpatialReferenceResolution) sref).set_XYResolution(true, 0.01);
			return sref;
		}

		private static void AssertFulfilled([NotNull] string expression,
		                                    [CanBeNull] IGeometry geometry)
		{
			AssertFulfilled(expression, geometry, out string _);
		}

		private static void AssertFulfilled([NotNull] string expression,
		                                    [CanBeNull] IGeometry geometry,
		                                    [NotNull] out string values)
		{
			Assert.True(IsFulfilled(expression, geometry, out values), values);
		}

		private static void AssertNotFulfilled([NotNull] string expression,
		                                       [CanBeNull] IGeometry geometry)
		{
			string values;
			Assert.False(IsFulfilled(expression, geometry, out values), values);
		}

		private static bool IsFulfilled([NotNull] string expression,
		                                [CanBeNull] IGeometry geometry,
		                                [NotNull] out string values)
		{
			var constraint = new GeometryConstraint(expression);

			values = constraint.FormatValues(geometry, CultureInfo.InvariantCulture);

			return constraint.IsFulfilled(geometry);
		}
	}
}

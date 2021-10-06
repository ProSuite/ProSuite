using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.EsriShape;
using Array = System.Array;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class GeometryUtilsTest
	{
		#region Setup/Teardown

		[SetUp]
		public void SetUp()
		{
			_spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					(int) esriSRVerticalCSType.esriSRVertCS_Landeshohennetz1995);
			SpatialReferenceUtils.SetXYDomain(_spatialReference,
			                                  -100, -100, 1000, 1000,
			                                  0.0001, 0.001);
			SpatialReferenceUtils.SetZDomain(_spatialReference,
			                                 -100, 5000,
			                                 0.0001, 0.001);

			// Be independent of the "Regional Settings" (number formatting, etc):
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		}

		#endregion

		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private ISpatialReference _spatialReference;
		private readonly string _newLine = Environment.NewLine;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriExtension.ThreeDAnalyst);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanDetectClosedRingsEqualInXY_ZUnaware_WithoutZ()
		{
			// NOTE: With 10.2.2 this situation is correctly detected by ArcObjects

			IPolygon ring1Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			IPolygon ring2Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100);

			const bool zAware = false;
			IPolygon polygon = CreatePolygon(ring1Polygon, ring2Polygon, zAware);

			string reason;
			Assert.IsFalse(GeometryUtils.IsGeometrySimple(
				               polygon, polygon.SpatialReference, false, out reason));

			bool detectedByArcObjects = reason == "The geometry has self intersections";
			bool detectedByWorkaround = reason == "The polygon has identical rings";

			Assert.IsTrue(detectedByArcObjects || detectedByWorkaround);
		}

		[Test]
		public void CanDetectClosedRingsEqualInXY_ZUnaware_WithoutZ_3Rings()
		{
			// NOTE: With 10.2.2 this situation is correctly detected by ArcObjects

			IPolygon ring1Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			IPolygon ring2Polygon = GeometryFactory.CreatePolygon(200, 200, 300, 300);
			IPolygon ring3Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100);

			const bool zAware = false;
			IPolygon polygon =
				CreatePolygon(ring1Polygon, ring2Polygon, ring3Polygon, zAware);

			string reason;
			Assert.IsFalse(GeometryUtils.IsGeometrySimple(
				               polygon, polygon.SpatialReference, false, out reason));

			bool detectedByArcObjects = reason == "The geometry has self intersections";
			bool detectedByWorkaround = reason == "The polygon has identical rings";

			Assert.IsTrue(detectedByArcObjects || detectedByWorkaround);
		}

		[Test]
		public void CanDetectClosedRingsEqualInXY_ZUnaware_WithDifferentZ()
		{
			// NOTE: With 10.2.2 this situation is correctly detected by ArcObjects

			IPolygon ring1Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100, 0);
			IPolygon ring2Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100, 100);

			const bool zAware = false;
			IPolygon polygon = CreatePolygon(ring1Polygon, ring2Polygon, zAware);

			string reason;
			Assert.IsFalse(GeometryUtils.IsGeometrySimple(
				               polygon, polygon.SpatialReference, false, out reason));

			bool detectedByArcObjects = reason == "The geometry has self intersections";
			bool detectedByWorkaround = reason == "The polygon has identical rings";

			Assert.IsTrue(detectedByArcObjects || detectedByWorkaround);
		}

		[Test]
		public void CanDetectClosedRingsEqualInXY_ZAware_WithDifferentZ()
		{
			// NOTE: With 10.2.2 this situation is correctly detected by ArcObjects

			IPolygon ring1Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100, 0);
			IPolygon ring2Polygon = GeometryFactory.CreatePolygon(0, 0, 100, 100, 100);

			const bool zAware = true;
			IPolygon polygon = CreatePolygon(ring1Polygon, ring2Polygon, zAware);

			string reason;
			Assert.IsFalse(GeometryUtils.IsGeometrySimple(
				               polygon, polygon.SpatialReference, false, out reason));

			bool detectedByArcObjects = reason == "The geometry has self intersections";
			bool detectedByWorkaround = reason == "The polygon has identical rings";

			Assert.IsTrue(detectedByArcObjects || detectedByWorkaround);
		}

		[NotNull]
		private static IPolygon CreatePolygon([NotNull] IPolygon ring1Polygon,
		                                      [NotNull] IPolygon ring2Polygon,
		                                      bool zAware)
		{
			const bool mAware = false;
			IPolygon result = GeometryFactory.CreatePolygon(
				zAware, mAware,
				(IRing) ((IGeometryCollection) ring1Polygon).get_Geometry(0),
				(IRing) ((IGeometryCollection) ring2Polygon).get_Geometry(0));

			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			((ISpatialReferenceTolerance) sref).XYTolerance = 0.01;
			((ISpatialReferenceResolution) sref).set_XYResolution(true, 0.001);

			result.SpatialReference = sref;
			return result;
		}

		[NotNull]
		private static IPolygon CreatePolygon([NotNull] IPolygon ring1Polygon,
		                                      [NotNull] IPolygon ring2Polygon,
		                                      [NotNull] IPolygon ring3Polygon,
		                                      bool zAware)
		{
			const bool mAware = false;
			IPolygon result = GeometryFactory.CreatePolygon(
				zAware, mAware,
				(IRing) ((IGeometryCollection) ring1Polygon).get_Geometry(0),
				(IRing) ((IGeometryCollection) ring2Polygon).get_Geometry(0),
				(IRing) ((IGeometryCollection) ring3Polygon).get_Geometry(0));

			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			((ISpatialReferenceTolerance) sref).XYTolerance = 0.01;
			((ISpatialReferenceResolution) sref).set_XYResolution(true, 0.001);

			result.SpatialReference = sref;
			return result;
		}

		[Test]
		public void CanUnionGeometriesWithCorrectSpatialReference()
		{
			const bool standardUnits = true;
			const double xyTol1 = 0.1;
			const double xyTol2 = 0.2;
			const double xyRes1 = 0.01;
			const double xyRes2 = 0.02;

			ISpatialReference sref1 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			((ISpatialReferenceTolerance) sref1).XYTolerance = xyTol1;
			((ISpatialReferenceResolution) sref1).set_XYResolution(standardUnits, xyRes1);

			ISpatialReference sref2 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			((ISpatialReferenceTolerance) sref2).XYTolerance = xyTol2;
			((ISpatialReferenceResolution) sref2).set_XYResolution(standardUnits, xyRes2);

			IPolygon polygon1 = GeometryFactory.CreatePolygon(100, 100, 200, 200);
			polygon1.SpatialReference = sref1;

			IPolygon polygon2 = GeometryFactory.CreatePolygon(200, 100, 300, 200);
			polygon2.SpatialReference = sref2;

			var input = new List<IGeometry>();
			input.Add(polygon1);
			input.Add(polygon2);

			var result = (IPolygon) GeometryUtils.UnionGeometries(input);

			Assert.AreSame(sref1, result.SpatialReference);
			Assert.AreEqual(xyTol1,
			                ((ISpatialReferenceTolerance) result.SpatialReference)
			                .XYTolerance);
			Assert.AreEqual(xyRes1,
			                ((ISpatialReferenceResolution) result.SpatialReference)
			                .get_XYResolution(standardUnits));
		}

		[Test]
		public void CanAreCongruentWithinToleranceFastEnough()
		{
			long milliseconds;

			const int sixteenHoles = 16;
			const int hundredHoles = 100;
			const int thousandHoles = 1000;

			IPolygon poly1a = CreatePunchedSquarePolygon("1a", 40, sixteenHoles, 1);
			IPolygon poly1b = CreatePunchedSquarePolygon("1b", 400, sixteenHoles, -1);
			//WriteGeometryToXML(poly1a, @"C:\Documents and Settings\ujr\Desktop\p16-40.xml");
			//WriteGeometryToXML(poly1b, @"C:\Documents and Settings\ujr\Desktop\p16-400.xml");

			TimedCongruenceTestOld(poly1a, poly1b);
			TimedCongruenceTest(poly1a, poly1b, 1, out milliseconds);
			TimedCongruenceTestOld(poly1a, poly1b);
			bool congruent = TimedCongruenceTest(poly1a, poly1b, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 500, "too slow");

			Console.WriteLine();

			IPolygon poly2a = CreatePunchedSquarePolygon("2a", 40, hundredHoles, 1);
			IPolygon poly2b = CreatePunchedSquarePolygon("2b", 400, hundredHoles, -1);
			//WriteGeometryToXML(poly2a, @"C:\Documents and Settings\ujr\Desktop\p100-40.xml");
			//WriteGeometryToXML(poly2b, @"C:\Documents and Settings\ujr\Desktop\p100-200.xml");

			TimedCongruenceTestOld(poly2a, poly2b);
			TimedCongruenceTest(poly2a, poly2b, 1, out milliseconds);
			TimedCongruenceTestOld(poly2a, poly2b);
			congruent = TimedCongruenceTest(poly2a, poly2b, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 1000, "too slow");

			Console.WriteLine();

			IPolygon poly3a = CreatePunchedSquarePolygon("3a", 40, thousandHoles, 1);
			IPolygon poly3b = CreatePunchedSquarePolygon("3b", 40, thousandHoles, -1);
			//WriteGeometryToXML(poly3a, @"C:\Documents and Settings\ujr\Desktop\p1000-40-lt.xml");
			//WriteGeometryToXML(poly3b, @"C:\Documents and Settings\ujr\Desktop\p1000-40-rt.xml");

			TimedCongruenceTestOld(poly3a, poly3b);
			TimedCongruenceTestOld(poly3a, poly3b);
			TimedCongruenceTest(poly3a, poly3b, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly3a, poly3b, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 3000, "too slow");

			Console.WriteLine();

			IPolygon poly4a = CreatePunchedSquarePolygon("4a", 200, thousandHoles, 1);
			IPolygon poly4b = CreatePunchedSquarePolygon("4b", 200, thousandHoles, -1);
			//WriteGeometryToXML(poly4a, @"C:\Documents and Settings\ujr\Desktop\p1000-200-lt.xml");
			//WriteGeometryToXML(poly4b, @"C:\Documents and Settings\ujr\Desktop\p1000-200-rt.xml");

			TimedCongruenceTestOld(poly4a, poly4b);
			TimedCongruenceTestOld(poly4a, poly4b);
			TimedCongruenceTest(poly4a, poly4b, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly4a, poly4b, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 10000, "too slow");

			// Note:
			// With even bigger polygons I get undue delays creating them (most of
			// the time is in Simplify), or worse, I get AccessViolationExceptions,
			// sometimes from IClone.Clone(), sometimes from SetWKSPointZs()...
		}

		[Test]
		public void CanAreCongruentWithinToleranceNoZ()
		{
			var p00 = new Pt(0, 0);
			var p10 = new Pt(1, 0);
			var p01 = new Pt(0, 1);
			var p11 = new Pt(1, 1);
			var px0 = new Pt(0.5, 0.05);

			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);
			var polygonBuilder = new GeometryBuilder(
				sr, esriGeometryType.esriGeometryPolygon, false, false);

			var poly1 =
				(IPolygon) polygonBuilder.CreateGeometry(p00, p01, p11, p10, p00);
			Console.WriteLine(@"Poly1 = {0}", GetVertexString(poly1));

			var poly2 =
				(IPolygon) polygonBuilder.CreateGeometry(px0, p00, p01, p11, p10, px0);
			Console.WriteLine(@"Poly2 = {0}", GetVertexString(poly2));

			// For this test, we want poly1 and poly2 to start at different points
			// to exercise the vertex-order-independence of AreCongruentWithinTolerance().
			// Assert this is really the case (Simplify() tends to reorder vertices):
			bool sameFromPoint = GeometryUtils.AreEqual(poly1.FromPoint, poly2.FromPoint);
			Assert.IsFalse(
				sameFromPoint,
				"Oops, poly1 and poly2 have same FromPoint.");

			bool congruent = TimedCongruenceTest(poly1, poly2, 1, out long _);
			Assert.IsTrue(congruent);
		}

		[Test]
		public void CanAreCongruentWithinToleranceOnBigForestPolygonFastEnough()
		{
			Console.WriteLine(@"Loading big forest polygon...");
			string filePath = TestData.GetBigForestPolygonPath();
			var poly1 = (IPolygon) ReadGeometryFromXML(filePath);
			Console.WriteLine(GetVertexString(poly1));

			Console.WriteLine(@"Reordering polygon vertices...");
			IPolygon poly2 = RotateRings(poly1, 1);
			Console.WriteLine(GetVertexString(poly2));

			Console.WriteLine(@"Reordering polygon vertices...");
			IPolygon poly3 = RotateRings(poly1, -1);
			Console.WriteLine(GetVertexString(poly3));

			Console.WriteLine(@"Interpolating vertices...");
			IPolygon poly4 = InterpolateVertices(poly1);
			Console.WriteLine(GetVertexString(poly4));

			bool congruent;
			long milliseconds;

			Console.WriteLine(@"{0}Testing 1 ~ 2 ...", _newLine);
			TimedCongruenceTest(poly1, poly2, 1, out milliseconds);
			TimedCongruenceTest(poly1, poly2, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly1, poly2, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 5000, "too slow");

			Console.WriteLine(@"{0}Testing 1 ~ 3 ...", _newLine);
			TimedCongruenceTest(poly1, poly3, 1, out milliseconds);
			TimedCongruenceTest(poly1, poly3, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly1, poly3, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 5000, "too slow");

			Console.WriteLine(@"{0}Testing 2 ~ 3 ...", _newLine);
			TimedCongruenceTest(poly2, poly3, 1, out milliseconds);
			TimedCongruenceTest(poly2, poly3, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly2, poly3, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 5000, "too slow");

			// Note:
			// Need a tolerance factor of 2 for the next tests to be congruent:
			// One of the inner rings is such that with the interpolated extra
			// vertex, Weed() considers one of the other vertices insignificant,
			// but without the interpolated vertex, all vertices remain. Closer
			// inspection revealed a small Z difference in the vertices around
			// start/end of ring #19.

			Console.WriteLine(@"{0}Testing 1 ~ 4 ...", _newLine);
			TimedCongruenceTest(poly1, poly4, 2, out milliseconds);
			TimedCongruenceTest(poly1, poly4, 2, out milliseconds);
			congruent = TimedCongruenceTest(poly1, poly4, 2, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 5000, "too slow");

			Console.WriteLine(@"{0}Testing 2 ~ 4 ...", _newLine);
			TimedCongruenceTest(poly2, poly4, 2, out milliseconds);
			TimedCongruenceTest(poly2, poly4, 2, out milliseconds);
			congruent = TimedCongruenceTest(poly2, poly4, 2, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 5000, "too slow");
		}

		[Test]
		public void CanAreCongruentWithinToleranceOnHugeLockergesteinPolygonFastEnough()
		{
			Console.WriteLine(@"Loading huge Lockergestein polygon...");
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			var poly1 = (IPolygon) ReadGeometryFromXML(filePath);
			Console.WriteLine(GetVertexString(poly1));

			Console.WriteLine(@"Reordering polygon vertices...");
			IPolygon poly2 = RotateRings(poly1, 1);
			Console.WriteLine(GetVertexString(poly2));

			Console.WriteLine(@"Interpolating vertices...");
			IPolygon poly3 = InterpolateVertices(poly1);
			Console.WriteLine(GetVertexString(poly3));

			bool congruent;
			long milliseconds;

			Console.WriteLine(@"{0}Testing 1 ~ 2 ...", _newLine);
			TimedCongruenceTest(poly1, poly2, 1, out milliseconds);
			TimedCongruenceTest(poly1, poly2, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly1, poly2, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 10000, "too slow");

			Console.WriteLine(@"{0}Testing 1 ~ 3 ...", _newLine);
			TimedCongruenceTest(poly1, poly3, 1, out milliseconds);
			TimedCongruenceTest(poly1, poly3, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly1, poly3, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 10000, "too slow");

			Console.WriteLine(@"{0}Testing 2 ~ 3 ...", _newLine);
			TimedCongruenceTest(poly2, poly3, 1, out milliseconds);
			TimedCongruenceTest(poly2, poly3, 1, out milliseconds);
			congruent = TimedCongruenceTest(poly2, poly3, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 10000, "too slow");
		}

		[Test]
		public void CanAreCongruentWithinToleranceOnPolygons()
		{
			bool congruent;

			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;
			ISpatialReference sr = CreateSpatialReference(xyTolerance, zTolerance);

			var polyBuilder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolygon, true);

			var p00 = new Pt(0, 0, 99);
			var p10 = new Pt(1, 0, 99);
			var p01 = new Pt(0, 1, 99);
			var p11 = new Pt(1, 1, 99);
			var px0 = new Pt(0.5, 0.05, 99);
			var p00z = new Pt(0, 0, 99.05);

			var poly1 = (IPolygon) polyBuilder.CreateGeometry(p00, p01, p11, p10);
			Console.WriteLine(@"Poly1 = {0}", GetVertexString(poly1));
			IPolygon poly2 = GeometryFactory.Clone(poly1);
			Console.WriteLine(@"Poly2 = {0}", GetVertexString(poly2));
			var poly3 = (IPolygon) polyBuilder.CreateGeometry(p00, p10, p11, p01);
			Console.WriteLine(@"Poly3 = {0}", GetVertexString(poly3));
			var poly4 = (IPolygon) polyBuilder.CreateGeometry(px0, p00, p01, p11, p10);
			Console.WriteLine(@"Poly4 = {0}", GetVertexString(poly4));
			var poly5 = (IPolygon) polyBuilder.CreateGeometry(p00z, p01, p11, p10);
			Console.WriteLine(@"Poly5 = {0}", GetVertexString(poly5));

			// Polygon is congruent with its clone, even with weedFactor=0:
			Console.WriteLine(@"{0}Testing 1 ~ 2", _newLine);
			congruent = TimedCongruenceTest(poly1, poly2, 0, out long _);
			Assert.IsTrue(congruent);

			// Polygons 1 and 3 have different vertex ordering but are congruent:
			Console.WriteLine(@"{0}Testing 1 ~ 3", _newLine);
			congruent = TimedCongruenceTest(poly1, poly3, 0, out long _);
			Assert.IsTrue(congruent);

			// Polygons 1 and 4 are congruent (within tolerance), but to assert this,
			// the congruence testing routine must weed the start/end point of poly4!
			Console.WriteLine(@"{0}Testing 1 ~ 4 (no tolerance)", _newLine);
			congruent = TimedCongruenceTest(poly1, poly4, 0, out long _);
			Assert.IsFalse(congruent);
			Console.WriteLine(@"{0}Testing 1 ~ 4", _newLine);
			congruent = TimedCongruenceTest(poly1, poly4, 1, out long _);
			Assert.IsTrue(congruent);

			// Polygon 5 has a vertex that differs in Z only:
			Console.WriteLine(@"{0}Testing 1 ~ 5 (no tolerance)", _newLine);
			congruent = TimedCongruenceTest(poly1, poly5, 0, out long _);
			Assert.IsFalse(congruent);
			Console.WriteLine(@"{0}Testing 1 ~ 5", _newLine);
			congruent = TimedCongruenceTest(poly1, poly5, 1, out long _);
			Assert.IsTrue(congruent);
		}

		[Test]
		public void CanAreCongruentWithinToleranceOnPolylines()
		{
			bool congruent;

			ISpatialReference sr = CreateSpatialReference(0.0125, 0.0125);

			var lineBuilder = new GeometryBuilder(
				sr, esriGeometryType.esriGeometryPolyline,
				true);

			var line1 = (IPolyline) lineBuilder.CreateGeometry(
				new Pt(0, 0, 99), new Pt(1, 1, 99), new Pt(2, 2, 99));
			Console.WriteLine(@"Line1 = {0}", GetVertexString(line1));

			IPolyline line2 = GeometryFactory.Clone(line1);
			Console.WriteLine(@"Line2 = {0}", GetVertexString(line2));

			var line3 = (IPolyline) lineBuilder.CreateGeometry(
				new Pt(0, 0, 99), new Pt(1.1, 0.9, 99), new Pt(2, 2, 99));
			Console.WriteLine(@"Line3 = {0}", GetVertexString(line3));

			// Line is congruent with itself (same instance):
			Console.WriteLine(@"{0}Testing 1 ~ 1 (no tolerance)", _newLine);
			congruent = TimedCongruenceTest(line1, line1, 0, out long _);
			Assert.IsTrue(congruent);

			// Line is congruent with itself (cloned instance):
			Console.WriteLine(@"{0}Testing 1 ~ 2 (no tolerance)", _newLine);
			congruent = TimedCongruenceTest(line1, line2, 0, out long _);
			Assert.IsTrue(congruent);

			// Lines 1 and 3 are different (small tolerance):
			Console.WriteLine(@"{0}Testing 1 ~ 3 (small tolerance)", _newLine);
			congruent = TimedCongruenceTest(line1, line3, 1, out long _);
			Assert.IsFalse(congruent);

			// Lines 1 and 3 are congruent (much larger tolerance):
			Console.WriteLine(@"{0}Testing 1 ~ 3 (much more tolerance)", _newLine);
			congruent = TimedCongruenceTest(line1, line3, 20, out long _);
			Assert.IsTrue(congruent);
		}

		[Test]
		public void CanGetIsSimplePolylineWithShortSegments()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(20, 0.0005), // half the xy tolerance
				GeometryFactory.CreatePoint(30, 10));

			// Self intersections are reported by ArcObjects but the description is 'translated' to short-segments
			const bool allowNonPlanarLines = false;
			GeometryNonSimpleReason? nonSimpleReason;
			bool isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                               polyline.SpatialReference,
			                                               allowNonPlanarLines,
			                                               out string _,
			                                               out nonSimpleReason);
			Assert.IsFalse(isSimple);
			Assert.AreEqual(GeometryNonSimpleReason.ShortSegments, nonSimpleReason);

			// now with reduced tolerance:
			ISpatialReference lv95ReducedTolerance =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			((ISpatialReferenceTolerance) lv95ReducedTolerance).XYTolerance = 0.0004;

			isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                          lv95ReducedTolerance,
			                                          allowNonPlanarLines,
			                                          out string _,
			                                          out nonSimpleReason);

			// Now it is still reported as non-simple but not as short segment any more (0.0005 > 0.0004)
			Assert.IsFalse(isSimple);
			Assert.AreEqual(GeometryNonSimpleReason.SelfIntersections, nonSimpleReason);

			GeometryUtils.Simplify(polyline, false, ! allowNonPlanarLines);

			isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                          lv95,
			                                          allowNonPlanarLines,
			                                          out string _,
			                                          out nonSimpleReason);
			Assert.IsTrue(isSimple);
		}

		[Test]
		public void CanGetIsSimplePolylineWithShortishSegments()
		{
			// Determines the necessary reduction to find segments shorter than the actual XY tolerance
			// in this specific case.
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(20, 0.001), // exactly the xy tolerance
				GeometryFactory.CreatePoint(30, 10));

			// Self intersections are reported because the segments are not actually smaller than the tolerance:
			const bool allowNonPlanarLines = false;
			bool isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                               polyline.SpatialReference,
			                                               allowNonPlanarLines,
			                                               out _, out _);
			Assert.IsFalse(isSimple);

			// now with reduced tolerance:
			ISpatialReference lv95ReducedTolerance =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			((ISpatialReferenceTolerance) lv95ReducedTolerance).XYTolerance = 0.0004;

			isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                          lv95ReducedTolerance,
			                                          allowNonPlanarLines,
			                                          out _, out _);

			Assert.IsFalse(isSimple);

			// reduce further:
			((ISpatialReferenceTolerance) lv95ReducedTolerance).XYTolerance = 0.00036;
			isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                          lv95ReducedTolerance,
			                                          allowNonPlanarLines,
			                                          out _, out _);

			Assert.IsFalse(isSimple);

			// reduce further:
			((ISpatialReferenceTolerance) lv95ReducedTolerance).XYTolerance = 0.00035;
			isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                          lv95ReducedTolerance,
			                                          allowNonPlanarLines,
			                                          out _, out _);

			Assert.IsTrue(isSimple);

			GeometryUtils.Simplify(polyline, false, ! allowNonPlanarLines);

			isSimple = GeometryUtils.IsGeometrySimple(polyline,
			                                          lv95,
			                                          allowNonPlanarLines,
			                                          out _, out _);
			Assert.IsTrue(isSimple);
		}

		[Test]
		public void CanAssignZToContainedPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000001, 1000001,
			                                                 2000009, 1000009, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, true, 5);
		}

		[Test]
		public void CanAssignZToContainedPolygonDrape()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000001, 1000001,
			                                                 2000009, 1000009, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, true, 5);
		}

		[Test]
		public void CanAssignZToContainedPolylineDrape()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(2000001, 1000005, 10,
			                                                    2000009, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			CanAssignZToPolyline(polyline, false, double.NaN, true, 3);
		}

		[Test]
		public void CanAssignZToContainedPolylineDrapeDensify()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(2000001, 1000005, 10,
			                                                    2000009, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			IPolyline result =
				CanAssignZToPolyline(polyline, false, double.NaN, true, 11, 1);

			Assert.AreEqual(120, result.FromPoint.Z, 0.001);
			Assert.AreEqual(120, result.ToPoint.Z, 0.001);
		}

		[Test]
		public void CanAssignZToContainingPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(1999999, 999999,
			                                                 2000011, 1000011, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, false, 5);
		}

		[Test]
		public void CanAssignZToContainingPolygonDrape()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(1999999, 999999,
			                                                 2000011, 1000011, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, true, 5);
		}

		[Test]
		public void CanAssignZToEndToEndPolylineDrape()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(2000000, 1000005, 10,
			                                                    2000010, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			IPolyline result = CanAssignZToPolyline(polyline, false, double.NaN, true, 3);

			Assert.AreEqual(100, result.FromPoint.Z, 0.001);
			Assert.AreEqual(100, result.ToPoint.Z, 0.001);
		}

		[Test]
		public void CanAssignZToOneEndUncontainedPolylineDrape()
		{
			IPolyline polyline =
				GeometryFactory.CreatePolyline(2000000, 1000005, double.NaN,
				                               2000011, 1000005, double.NaN);
			polyline.SpatialReference = _spatialReference;

			IPolyline result = CanAssignZToPolyline(polyline, false, double.NaN, true, 3);

			var points = (IPointCollection) result;

			const double zOnBufferedDomainBoundary = 102.222;
			const double e = 0.01;
			Assert.AreEqual(1, GeometryUtils.GetPartCount(result));
			Assert.AreEqual(zOnBufferedDomainBoundary, result.FromPoint.Z, e);
			Assert.AreEqual(zOnBufferedDomainBoundary, result.ToPoint.Z, e);
			Assert.AreEqual(200, points.get_Point(1).Z, e);
		}

		[Test]
		public void CanAssignZToPolylineVertices()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(2000001, 1000005, 10,
			                                                    2000009, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			IPolyline result =
				CanAssignZToPolyline(polyline, false, double.NaN, false, 2);

			Assert.AreEqual(120, result.FromPoint.Z, 0.001);
			Assert.AreEqual(120, result.ToPoint.Z, 0.001);
		}

		[Test]
		public void CanAssignZToTouchingPolygon1Point()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000010, 999995,
			                                                 2000020, 1000000, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, false, 5);
		}

		[Test]
		public void CanAssignZToTouchingPolygon1Segment()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000010, 999995,
			                                                 2000020, 1000005, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, false, 5);
			// depends on shrinking of domain
		}

		[Test]
		public void CanAssignZToTouchingPolyline()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(2000010, 1000005, 10,
			                                                    2000020, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			CanAssignZToPolyline(polyline, false, double.NaN, false, 2);
		}

		[Test]
		public void CanAssignZToTouchingPolylineDrape()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(2000010, 1000005, 10,
			                                                    2000020, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			CanAssignZToPolyline(polyline, false, double.NaN, true, 2);
		}

		[Test]
		public void CanAssignZToUncontainedPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000001, 999995,
			                                                 2000009, 1000004, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, false, 5); // 7);
		}

		[Test]
		public void CanAssignZToUncontainedPolygonDrape()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000001, 999995,
			                                                 2000009, 1000004, 999);
			polygon.SpatialReference = _spatialReference;

			CanAssignZToPolygon(polygon, false, double.NaN, true, 9);
		}

		[Test]
		public void CanAssignZToUncontainedPolylineDrape()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(1999999, 1000005, 10,
			                                                    2000011, 1000005, 10);
			polyline.SpatialReference = _spatialReference;

			CanAssignZToPolyline(polyline, false, double.NaN, true, 3);
		}

		[Test]
		public void CanCalculateNonSimpleZIfNoZValues()
		{
			IPolyline polyline =
				GeometryFactory.CreatePolyline(2000000, 1000005, double.NaN,
				                               2000011, 1000005, double.NaN);
			GeometryUtils.MakeZAware(polyline);

			bool simplified = GeometryUtils.TrySimplifyZ(polyline);

			Assert.False(simplified);

			Assert.IsFalse(((IZAware) polyline).ZSimple);
			Console.WriteLine(GeometryUtils.ToString(polyline));
		}

		[Test]
		public void CannotCalculateNonSimpleZIfOneZValue()
		{
			IPolyline polyline =
				GeometryFactory.CreatePolyline(2000000, 1000005, 500.0,
				                               2000011, 1000005, double.NaN);
			GeometryUtils.MakeZAware(polyline);

			// Consider changing the implementation to extrapolate single values, to be consistent
			// with CanCalculateNonSimpleZInPolygonIfOneZValue()
			bool simplified = GeometryUtils.TrySimplifyZ(polyline);

			Assert.IsFalse(simplified);

			Assert.IsFalse(((IZAware) polyline).ZSimple);
			Console.WriteLine(GeometryUtils.ToString(polyline));
		}

		[Test]
		public void CanCalculateNonSimpleZInPolygonIfNoZValues()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);
			IPolygon polygon =
				GeometryFactory.CreatePolygon(
					GeometryFactory.CreatePolyline(
						sref,
						GeometryFactory.CreatePoint(2000000, 1000005, double.NaN),
						GeometryFactory.CreatePoint(2000011, 1000005, double.NaN),
						GeometryFactory.CreatePoint(2000011, 1000000, double.NaN)));

			GeometryUtils.MakeZAware(polygon);

			bool simplified = GeometryUtils.TrySimplifyZ(polygon);
			Assert.IsFalse(simplified);

			Assert.IsFalse(((IZAware) polygon).ZSimple);
			Console.WriteLine(GeometryUtils.ToString(polygon));
		}

		[Test]
		public void CanCalculateNonSimpleZInPolygonIfOneZValue()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);
			IPolygon polygon =
				GeometryFactory.CreatePolygon(
					GeometryFactory.CreatePolyline(
						sref,
						GeometryFactory.CreatePoint(2000000, 1000005, double.NaN),
						GeometryFactory.CreatePoint(2000011, 1000005, 450),
						GeometryFactory.CreatePoint(2000011, 1000000, double.NaN)));

			GeometryUtils.MakeZAware(polygon);

			bool simplified = GeometryUtils.TrySimplifyZ(polygon);
			Assert.IsTrue(simplified);

			Assert.IsTrue(((IZAware) polygon).ZSimple);
			Console.WriteLine(GeometryUtils.ToString(polygon));
		}

		[Test]
		public void CanTryConvertGeometryMultipatchToPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(2000010, 999995,
			                                                 2000020, 1000005, 999);

			polygon.SpatialReference = _spatialReference;

			IMultiPatch multipatch =
				GeometryFactory.CreateMultiPatch(polygon);

			IGeometry converted;
			bool canConvert = GeometryUtils.TryConvertGeometry(
				multipatch, esriGeometryType.esriGeometryPolygon, out converted);

			Assert.IsTrue(canConvert);
			Assert.IsTrue(GeometryUtils.AreEqualInXY(polygon, converted));
		}

		[Test]
		public void CanTryConvertGeometryEmptyMultipatchToPolygon()
		{
			var multipatch = (IMultiPatch)
				GeometryFactory.CreateEmptyGeometry(
					esriGeometryType.esriGeometryMultiPatch);

			bool canConvert = GeometryUtils.TryConvertGeometry(
				multipatch, esriGeometryType.esriGeometryPolygon, out IGeometry _);

			Assert.IsFalse(canConvert);
		}

		[Test]
		public void CanDetectNoSelfIntersectionPolygon()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			var builder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolygon, true,
				                    false);
			builder.SimplifyResult = false;

			IGeometry geometry = builder.CreateGeometry(new Pt(0, 0, 0),
			                                            new Pt(0, 100, 0),
			                                            new Pt(100, 100, 0),
			                                            new Pt(0, 0, 0));

			Assert.IsFalse(GeometryUtils.IsSelfIntersecting(geometry));
		}

		[Test]
		public void CanDetectSelfIntersectionPolygon()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			var builder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolygon, true,
				                    false);
			builder.SimplifyResult = false;

			IGeometry geometry = builder.CreateGeometry(new Pt(0, 0, 0),
			                                            new Pt(0, 100, 0),
			                                            new Pt(100, 100, 0),
			                                            new Pt(-10, 50, 0),
			                                            new Pt(0, 0, 0));

			Assert.IsTrue(GeometryUtils.IsSelfIntersecting(geometry));
		}

		[Test]
		public void CanDetectSelfIntersectionPolygonMAware()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			var builder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolygon, true, true);
			builder.SimplifyResult = false;

			IGeometry geometry = builder.CreateGeometry(new Pt(0, 0, 0, 1),
			                                            new Pt(0, 100, 0, 2),
			                                            new Pt(100, 100, 0, 3),
			                                            new Pt(-10, 50, 0, 4),
			                                            new Pt(0, 0, 0, 5));

			Assert.IsTrue(GeometryUtils.IsSelfIntersecting(geometry));
		}

		[Test]
		public void CanMove3DGeometryIn3d()
		{
			IGeometry geometry3d = GeometryFactory.CreatePolygon(0, 0, 100, 100, 100);

			GeometryUtils.MoveGeometry(geometry3d, 50, 60, 70);

			Assert.AreEqual(50, geometry3d.Envelope.XMin);
			Assert.AreEqual(60, geometry3d.Envelope.YMin);
			Assert.AreEqual(170, geometry3d.Envelope.ZMin);

			Assert.AreEqual(150, geometry3d.Envelope.XMax);
			Assert.AreEqual(160, geometry3d.Envelope.YMax);
			Assert.AreEqual(170, geometry3d.Envelope.ZMax);
		}

		[Test]
		public void CanMove3DGeometryIn2d()
		{
			IGeometry geometry3d = GeometryFactory.CreatePolygon(0, 0, 100, 100, 100);

			GeometryUtils.MoveGeometry(geometry3d, 50, 60);

			Assert.AreEqual(50, geometry3d.Envelope.XMin);
			Assert.AreEqual(60, geometry3d.Envelope.YMin);
			Assert.AreEqual(100, geometry3d.Envelope.ZMin);

			Assert.AreEqual(150, geometry3d.Envelope.XMax);
			Assert.AreEqual(160, geometry3d.Envelope.YMax);
			Assert.AreEqual(100, geometry3d.Envelope.ZMax);
		}

		[Test]
		public void CanMove2DGeometryIn2d()
		{
			IGeometry geometry2d = GeometryFactory.CreatePolygon(0, 0, 100, 100);

			Assert.IsFalse(GeometryUtils.IsZAware(geometry2d));

			GeometryUtils.MoveGeometry(geometry2d, 50, 60);

			Assert.AreEqual(50, geometry2d.Envelope.XMin);
			Assert.AreEqual(60, geometry2d.Envelope.YMin);

			Assert.AreEqual(150, geometry2d.Envelope.XMax);
			Assert.AreEqual(160, geometry2d.Envelope.YMax);
		}

		[Test]
		public void CanDetectNoSelfIntersectionClosedPolyline()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			var builder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolyline, true,
				                    false);
			builder.SimplifyResult = false;

			IGeometry geometry = builder.CreateGeometry(new Pt(0, 0, 0),
			                                            new Pt(0, 100, 0),
			                                            new Pt(100, 100, 0),
			                                            new Pt(0, 0, 0));

			Assert.IsFalse(GeometryUtils.IsSelfIntersecting(geometry));
		}

		[Test]
		public void CanDetectSelfIntersectionPolyline()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			var builder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolyline, true,
				                    false);
			builder.SimplifyResult = false;

			IGeometry geometry = builder.CreateGeometry(new Pt(0, 0, 0),
			                                            new Pt(0, 100, 0),
			                                            new Pt(100, 100, 0),
			                                            new Pt(-10, 50, 0));

			Assert.IsTrue(GeometryUtils.IsSelfIntersecting(geometry));
		}

		[Test]
		public void CanDetectSelfIntersectionPolylineMAware()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			var builder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolyline, true,
				                    true);
			builder.SimplifyResult = false;

			IGeometry geometry = builder.CreateGeometry(new Pt(0, 0, 0, 1),
			                                            new Pt(0, 100, 0, 2),
			                                            new Pt(100, 100, 0, 3),
			                                            new Pt(-10, 50, 0, 4));

			Assert.IsTrue(((IMAware) geometry).MAware);
			Assert.IsTrue(GeometryUtils.IsSelfIntersecting(geometry));
		}

		[Test]
		public void CanDistanceSquared2D()
		{
			const double xP = 1;
			const double yP = 1;
			const double xA = 1;
			const double yA = 3;
			const double xB = 3;
			const double yB = 1;
			const double xC = 3;
			const double yC = 3;

			Assert.AreEqual(2, GeometryUtils.DistanceSquaredXY(xP, yP, xA, yA, xB, yB));

			Assert.AreEqual(8, GeometryUtils.DistanceSquaredXY(xP, yP, xC, yC, xC, yC));

			Assert.AreEqual(0, GeometryUtils.DistanceSquaredXY(xP, yP, xP, yP, xP, yP));
		}

		[Test]
		public void CanDistanceSquaredZ()
		{
			double dz;
			double expected;
			const double xP = 1;
			const double yP = 1;
			const double zP = 2.1;
			const double xA = 1;
			const double yA = 3;
			const double zA = 1;
			const double xB = 3;
			const double yB = 1;
			const double zB = 3;
			const double xC = 3;
			const double yC = 3;
			const double zC = double.NaN;

			expected = Math.Pow(0.1, 2);
			dz = GeometryUtils.DistanceSquaredZ(xP, yP, zP, xA, yA, zA, xB, yB, zB);
			Assert.IsTrue(Math.Abs(dz - expected) < 0.0001);

			expected = Math.Pow(1.1, 2);
			dz = GeometryUtils.DistanceSquaredZ(xP, yP, zP, xA, yA, zA, xA, yA, zA);
			Assert.IsTrue(Math.Abs(dz - expected) < 0.0001);

			expected = 0.0;
			dz = GeometryUtils.DistanceSquaredZ(xP, yP, zP, xP, yP, zP, xP, yP, zP);
			Assert.AreEqual(expected, dz);

			dz = GeometryUtils.DistanceSquaredZ(xP, yP, zP, xA, yA, zA, xC, yC, zC);
			Assert.IsNaN(dz);
		}

		[Test]
		[Ignore("to be implemented")]
		public void CanEnsureSpatialReferenceCopy()
		{
			Assert.Fail("to be implemented");
		}

		[Test]
		[Ignore("to be implemented")]
		public void CanEnsureSpatialReferenceNoCopy()
		{
			Assert.Fail("to be implemented");
		}

		[Test]
		[Ignore("to be implemented")]
		public void CanEnsureSpatialReferenceSameCodeDifferentPrecision()
		{
			Assert.Fail("to be implemented");
		}

		[Test]
		public void CanEnsureSchemaZMMakeUnaware()
		{
			IGeometryDefEdit geoDef = new GeometryDefClass();
			geoDef.HasM_2 = false;
			geoDef.HasZ_2 = false;

			IGeometry poly = GeometryFactory.CreatePolygon(100, 200, 400, 500, 700);
			((IMAware) poly).MAware = true;

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(poly, geoDef, out awareGeometry);

			Assert.IsFalse(((IZAware) awareGeometry).ZAware);
			Assert.IsFalse(((IMAware) awareGeometry).MAware);
		}

		[Test]
		public void CanEnsureSchemaZMMakeAware()
		{
			IGeometryDefEdit geoDef = new GeometryDefClass();
			geoDef.HasM_2 = true;
			geoDef.HasZ_2 = true;

			IGeometry poly = GeometryFactory.CreatePolygon(100, 200, 400, 500);

			((IZAware) poly).ZAware = false;
			((IMAware) poly).MAware = false;

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(poly, geoDef, out awareGeometry);

			Assert.IsTrue(((IZAware) awareGeometry).ZAware);
			Assert.IsTrue(((IMAware) awareGeometry).MAware);

			var awarePoints = (IPointCollection) awareGeometry;
			for (var i = 0; i < awarePoints.PointCount; i++)
			{
				Assert.IsTrue(awarePoints.get_Point(i).Z == 0);
				Assert.IsTrue(awarePoints.get_Point(i).M == 0);
			}
		}

		[Test]
		public void CanEnsureSchemaZMMakeAwareMultipatch()
		{
			IGeometryDefEdit geoDef = new GeometryDefClass();
			geoDef.HasM_2 = true;
			geoDef.HasZ_2 = true;

			IPolygon poly = GeometryFactory.CreatePolygon(100, 200, 400, 500, 1000);

			((IZAware) poly).ZAware = true;
			((IMAware) poly).MAware = false;

			IMultiPatch multiPatch = GeometryFactory.CreateMultiPatch(poly);

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(multiPatch, geoDef, out awareGeometry);

			Assert.IsTrue(((IZAware) awareGeometry).ZAware);
			Assert.IsTrue(((IMAware) awareGeometry).MAware);

			var awarePoints = (IPointCollection) awareGeometry;
			for (var i = 0; i < awarePoints.PointCount; i++)
			{
				Assert.AreEqual(awarePoints.get_Point(i).Z, 1000);
				Assert.AreEqual(awarePoints.get_Point(i).M, 0);
			}
		}

		[Test]
		public void CanEnsureSchemaZMMakeAwareWithPreExistingZs()
		{
			IGeometryDefEdit geoDef = new GeometryDefClass();
			geoDef.HasM_2 = true;
			geoDef.HasZ_2 = true;

			IGeometry poly = GeometryFactory.CreatePolygon(100, 200, 400, 500, 700);

			((IZAware) poly).ZAware = false;
			((IMAware) poly).MAware = false;

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(poly, geoDef, out awareGeometry);

			Assert.IsTrue(((IZAware) awareGeometry).ZAware);
			Assert.IsTrue(((IMAware) awareGeometry).MAware);

			var awarePoints = (IPointCollection) awareGeometry;
			for (var i = 0; i < awarePoints.PointCount; i++)
			{
				// must preserve Zs
				Assert.AreEqual(awarePoints.get_Point(i).Z, 700);
				Assert.AreEqual(awarePoints.get_Point(i).M, 0);
			}
		}

		[Test]
		public void CanEnsureSchemaZMMakeAwareWithSomePreExistingZMs()
		{
			IGeometryDefEdit geoDef = new GeometryDefClass();
			geoDef.HasM_2 = true;
			geoDef.HasZ_2 = true;

			IGeometry poly = GeometryFactory.CreatePolygon(100, 200, 400, 500);

			IPoint point2 = ((IPointCollection) poly).get_Point(2);
			point2.Z = 800;
			point2.M = 0.5;

			((IPointCollection) poly).UpdatePoint(2, point2);

			((IZAware) poly).ZAware = false;
			((IMAware) poly).MAware = false;

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(poly, geoDef, out awareGeometry);

			Assert.IsTrue(((IZAware) awareGeometry).ZAware);
			Assert.IsTrue(((IMAware) awareGeometry).MAware);

			var awarePoints = (IPointCollection) awareGeometry;
			for (var i = 0; i < awarePoints.PointCount; i++)
			{
				Assert.AreEqual(awarePoints.get_Point(i).Z, 800);

				if (i == 2)
				{
					Assert.AreEqual(awarePoints.get_Point(i).M, 0.5);
				}
				else
				{
					Assert.IsTrue(awarePoints.get_Point(i).M == 0);
				}
			}
		}

		[Test]
		public void CanEnsureSchemaZMMakeAwareEnvelope()
		{
			IGeometryDefEdit geoDef = new GeometryDefClass();
			geoDef.HasM_2 = true;
			geoDef.HasZ_2 = true;

			IEnvelope env = GeometryFactory.CreateEnvelope(100, 200, 400, 500);

			((IZAware) env).ZAware = false;
			((IMAware) env).MAware = false;

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(env, geoDef, out awareGeometry);

			Assert.IsTrue(((IZAware) awareGeometry).ZAware);
			Assert.IsTrue(((IMAware) awareGeometry).MAware);

			Assert.AreEqual(0, ((IEnvelope) awareGeometry).MMax);
			Assert.AreEqual(0, ((IEnvelope) awareGeometry).MMin);
		}

		[Test]
		public void CanInterpolateVertices()
		{
			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);
			var polygonBuilder =
				new GeometryBuilder(sr, esriGeometryType.esriGeometryPolygon, false);

			var polygon = (IPolygon) polygonBuilder.CreateGeometry(
				new Pt(0, 0), new Pt(0, 1), new Pt(1, 1), new Pt(1, 0));
			Console.WriteLine(@"Original polygon:");
			Console.WriteLine(GetVertexString(polygon));

			IPolygon result = InterpolateVertices(polygon);
			Console.WriteLine(@"With interpolated vertex:");
			Console.WriteLine(GetVertexString(result, 3, 7));

			long milliseconds;
			bool congruent = TimedCongruenceTest(polygon, result, 1, out milliseconds);
			Assert.IsTrue(congruent);
			Assert.IsTrue(milliseconds < 100);
		}

		[Test]
		// learning test
		public void CanIntersectEmptyPolygon()
		{
			IPolygon polygon1 = new PolygonClass();
			IPolygon polygon2 = GeometryFactory.CreatePolygon(100, 100, 200, 200);

			polygon1.SpatialReference = _spatialReference;
			polygon2.SpatialReference = _spatialReference;

			IGeometry result =
				((ITopologicalOperator) polygon1).Intersect(polygon2,
				                                            esriGeometryDimension
					                                            .esriGeometry2Dimension);

			Assert.IsTrue(result.IsEmpty);
			Assert.IsTrue(result is IPolygon);
		}

		[Test]
		public void CanToStringMultipoint()
		{
			IMultipoint multipoint = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(0, 100, 10, 1000000),
				GeometryFactory.CreatePoint(100, 100, 10, 1000000),
				GeometryFactory.CreatePoint(200, 100, 10, 1000000));

			Console.WriteLine(GeometryUtils.ToString(multipoint));
		}

		[Test]
		public void CanGetInteriorIntersects()
		{
			object missing = Type.Missing;

			IPolygon polygon1 =
				GeometryFactory.CreatePolygon(_spatialReference, true, false);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000000, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000010, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000005, 1000005, 200), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000010, 1000010, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000010, 1000000, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000000, 100), ref missing,
			                                       ref missing);
			// single point touches:
			IPolygon polygon2 = GeometryFactory.CreatePolygon(2000010, 999995,
			                                                  2000020, 1000000, 999);

			Assert.IsTrue(GeometryUtils.Touches(polygon1, polygon2, true));
			Assert.IsTrue(GeometryUtils.Intersects(polygon1, polygon2));

			Assert.IsFalse(GeometryUtils.InteriorIntersects(polygon1, polygon2, false));

			// single segment touches:
			polygon2 = GeometryFactory.CreatePolygon(2000010, 999995,
			                                         2000020, 1000005, 999);

			Assert.IsTrue(GeometryUtils.Touches(polygon1, polygon2, true));
			Assert.IsTrue(GeometryUtils.Intersects(polygon1, polygon2));

			Assert.IsFalse(GeometryUtils.InteriorIntersects(polygon1, polygon2, false));
		}

		[Test]
		public void CanGetInteriorIntersectsMultipartPolygon()
		{
			object missing = Type.Missing;

			IPolygon polygon1 =
				GeometryFactory.CreatePolygon(_spatialReference, true, false);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000000, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000010, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000005, 1000005, 200), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000010, 1000010, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000010, 1000000, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000000, 100), ref missing,
			                                       ref missing);
			// single point touches:
			IPolygon polygon2Part0 = GeometryFactory.CreatePolygon(2000010, 999995,
			                                                       2000020, 1000000, 999);

			Assert.IsTrue(GeometryUtils.Touches(polygon1, polygon2Part0, true));
			Assert.IsTrue(GeometryUtils.Intersects(polygon1, polygon2Part0));

			// intersects polygon
			IPolygon polygon2Part1 = GeometryFactory.CreatePolygon(
				1999998, 1000003, 2000004,
				1000006, 888);

			Assert.IsFalse(GeometryUtils.Touches(polygon1, polygon2Part1, true));
			Assert.IsTrue(GeometryUtils.Intersects(polygon1, polygon2Part1));

			var polygon2 = (IPolygon) GeometryUtils.Union(polygon2Part0, polygon2Part1);

			Assert.IsFalse(GeometryUtils.Touches(polygon1, polygon2, false));
			Assert.IsTrue(GeometryUtils.InteriorIntersects(polygon1, polygon2, false));

			// single segment touches:
			polygon2Part0 = GeometryFactory.CreatePolygon(2000010, 999995,
			                                              2000020, 1000005, 999);

			Assert.IsTrue(GeometryUtils.Touches(polygon1, polygon2Part0, true));
			Assert.IsFalse(
				GeometryUtils.InteriorIntersects(polygon1, polygon2Part0, false));

			polygon2 = (IPolygon) GeometryUtils.Union(polygon2Part0, polygon2Part1);

			Assert.IsFalse(GeometryUtils.Touches(polygon1, polygon2, true));
			Assert.IsTrue(GeometryUtils.InteriorIntersects(polygon1, polygon2, false));
		}

		[Test]
		public void CanGetInteriorIntersectsMultipartPolyline()
		{
			object missing = Type.Missing;

			IPolygon polygon1 =
				GeometryFactory.CreatePolygon(_spatialReference, true, false);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000000, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000010, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000005, 1000005, 200), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000010, 1000010, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000010, 1000000, 100), ref missing,
			                                       ref missing);
			((IPointCollection) polygon1).AddPoint(Pt(2000000, 1000000, 100), ref missing,
			                                       ref missing);
			// single point touches:
			IPolyline polyline2part1 = GeometryFactory.CreatePolyline(_spatialReference,
			                                                          Pt(2000010, 1000000,
			                                                             100),
			                                                          Pt(2000040, 1000000,
			                                                             100));

			Assert.IsTrue(GeometryUtils.Touches(polygon1, polyline2part1, true));
			Assert.IsTrue(GeometryUtils.Intersects(polygon1, polyline2part1));

			// intersects polygon
			IPolyline polyline2part2 = GeometryFactory.CreatePolyline(_spatialReference,
			                                                          Pt(1999998, 1000003,
			                                                             300),
			                                                          Pt(2000004, 1000006,
			                                                             300));

			Assert.IsFalse(GeometryUtils.Touches(polygon1, polyline2part2, true));
			Assert.IsTrue(GeometryUtils.Intersects(polygon1, polyline2part2));

			var polyline2 =
				(IPolyline) GeometryUtils.Union(polyline2part1, polyline2part2);

			Assert.IsFalse(GeometryUtils.Touches(polygon1, polyline2, false));
			Assert.IsTrue(GeometryUtils.InteriorIntersects(polygon1, polyline2, false));

			// segment runs along the polygon:
			polyline2part1 = GeometryFactory.CreatePolyline(_spatialReference,
			                                                Pt(2000010, 999995, 384),
			                                                Pt(2000010, 1000005, 999));

			Assert.IsTrue(GeometryUtils.Touches(polygon1, polyline2part1, true));
			Assert.IsFalse(
				GeometryUtils.InteriorIntersects(polygon1, polyline2part1, false));

			polyline2 = (IPolyline) GeometryUtils.Union(polyline2part1, polyline2part2);

			Assert.IsFalse(GeometryUtils.Touches(polygon1, polyline2, true));
			Assert.IsTrue(GeometryUtils.InteriorIntersects(polygon1, polyline2, false));
		}

		[Test]
		// learning test
		public void CanDifference3DMultipointWithSnapped2DPolygon()
		{
			IMultipoint multipoint = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(0, 100, 10, 1000000),
				GeometryFactory.CreatePoint(100, 100, 10, 1000000),
				GeometryFactory.CreatePoint(200, 100, 10, 1000000));

			IPolygon polygon = GeometryFactory.CreatePolygon(200, 0, 300, 200);

			var result =
				(IMultipoint) ((ITopologicalOperator) multipoint).Difference(polygon);
			((IMAware) result).MAware = true;

			Console.Out.WriteLine(GeometryUtils.ToString(result));

			Assert.IsTrue(GeometryUtils.IsZAware(multipoint));
			Assert.IsFalse(GeometryUtils.IsZAware(polygon));
			Assert.IsFalse(GeometryUtils.IsZAware(result));

			Assert.AreEqual(2, ((IPointCollection) result).PointCount);
		}

		[Test]
		// learning test
		public void CanDifference3DMultipointWithSnapped3DPolygon()
		{
			IMultipoint multipoint = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(0, 100, 10, 1000000),
				GeometryFactory.CreatePoint(100, 100, 10, 1000000),
				GeometryFactory.CreatePoint(200, 100, 10, 1000000));

			IPolygon polygon = GeometryFactory.CreatePolygon(200, 0, 300, 200, 20);

			var result =
				(IMultipoint) ((ITopologicalOperator) multipoint).Difference(polygon);
			((IMAware) result).MAware = true;
			((IZAware) result).ZAware = true;

			Console.Out.WriteLine(GeometryUtils.ToString(result));

			Assert.IsTrue(GeometryUtils.IsZAware(multipoint));
			Assert.IsTrue(GeometryUtils.IsZAware(polygon));
			Assert.IsTrue(GeometryUtils.IsZAware(result));

			Assert.AreEqual(2, ((IPointCollection) result).PointCount);
		}

		[Test]
		// learning test
		public void CanIntersectTouchingEdgePolygons()
		{
			IPolygon polygon1 = GeometryFactory.CreatePolygon(100, 100, 200, 200);
			IPolygon polygon2 = GeometryFactory.CreatePolygon(110, 200, 190, 300);

			polygon1.SpatialReference = _spatialReference;
			polygon2.SpatialReference = _spatialReference;

			IGeometry result =
				((ITopologicalOperator) polygon1).Intersect(polygon2,
				                                            esriGeometryDimension
					                                            .esriGeometry2Dimension);

			Assert.IsTrue(result.IsEmpty);
			Assert.IsTrue(result is IPolygon);
		}

		[Test]
		// learning test
		public void CanIntersectTouchingVertexPolygons()
		{
			IPolygon polygon1 = GeometryFactory.CreatePolygon(100, 100, 200, 200);
			IPolygon polygon2 = GeometryFactory.CreatePolygon(200, 200, 300, 300);

			polygon1.SpatialReference = _spatialReference;
			polygon2.SpatialReference = _spatialReference;

			IGeometry result =
				((ITopologicalOperator) polygon1).Intersect(polygon2,
				                                            esriGeometryDimension
					                                            .esriGeometry2Dimension);

			Assert.IsTrue(result.IsEmpty);
			Assert.IsTrue(result is IPolygon);
		}

		[Test]
		// learning test
		public void CanIntersectWithEmptyPolygon()
		{
			IPolygon polygon1 = GeometryFactory.CreatePolygon(100, 100, 200, 200);
			IPolygon polygon2 = new PolygonClass();

			polygon1.SpatialReference = _spatialReference;
			polygon2.SpatialReference = _spatialReference;

			IGeometry result =
				((ITopologicalOperator) polygon1).Intersect(polygon2,
				                                            esriGeometryDimension
					                                            .esriGeometry2Dimension);

			Assert.IsTrue(result.IsEmpty);
			Assert.IsTrue(result is IPolygon);
		}

		[Test]
		public void CanIsSamePoint()
		{
			bool isSame;
			WKSPointZ p, a, b, c;

			p.X = 1;
			p.Y = 1;
			p.Z = 9;

			a.X = 1.1;
			a.Y = 1.1;
			a.Z = 9;

			b.X = 1.1;
			b.Y = 1.1;
			b.Z = 9;

			c.X = 1.1;
			c.Y = 1.1;
			c.Z = double.NaN;

			// Use 0.101, not 0.1, to account for roundoff
			double xyTolerance = 0.101 * Math.Sqrt(2);
			const double zTolerance = 0.1;

			// Compare P and P:
			isSame = GeometryUtils.IsSamePoint(p, p, xyTolerance, zTolerance);
			Assert.IsTrue(isSame);
			isSame = GeometryUtils.IsSamePoint(p, p, 0.0, 0.0);
			Assert.IsTrue(isSame);
			isSame = GeometryUtils.IsSamePoint(p, p, 0.0, double.NaN);
			Assert.IsTrue(isSame);

			// Compare A and P:
			isSame = GeometryUtils.IsSamePoint(a, p, xyTolerance, zTolerance);
			Assert.IsTrue(isSame);
			isSame = GeometryUtils.IsSamePoint(a, p, 0.0, zTolerance);
			Assert.IsFalse(isSame);

			// Compare B and P:
			isSame = GeometryUtils.IsSamePoint(b, p, xyTolerance, zTolerance);
			Assert.IsTrue(isSame);
			isSame = GeometryUtils.IsSamePoint(b, p, xyTolerance, double.NaN);
			Assert.IsTrue(isSame);

			// Compare C and P:
			isSame = GeometryUtils.IsSamePoint(c, p, xyTolerance, zTolerance);
			Assert.IsFalse(isSame);
			isSame = GeometryUtils.IsSamePoint(c, p, xyTolerance, double.NaN);
			Assert.IsTrue(isSame);
		}

		[Test]
		public void CanGetPointDistance3D()
		{
			IPoint point1 = GeometryFactory.CreatePoint(1000, 1000, 1000);
			IPoint point2 = GeometryFactory.CreatePoint(1001, 1000, 1001);

			double distance3D = GeometryUtils.GetPointDistance3D(point1, point2);

			Assert.AreEqual(Math.Sqrt(2), distance3D);

			point1 = GeometryFactory.CreatePoint(1000, 1000);
			point2 = GeometryFactory.CreatePoint(1001, 1001);

			distance3D = GeometryUtils.GetPointDistance3D(point1, point2);

			Assert.AreEqual(Math.Sqrt(2), distance3D);
		}

		[Test]
		public void CanGetShortSegments()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				1000, 1000, 1000, 1001, 1000,
				1001);

			IList<esriSegmentInfo> segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 1.4, null);

			Assert.AreEqual(0, segmentInfos.Count);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 1.5, null);

			Assert.AreEqual(1, segmentInfos.Count);

			polyline = GeometryFactory.CreatePolyline(1000, 1000, 1001, 1001);
			((IZAware) polyline).ZAware = false;

			// non-Z-aware geometry uses 2D length
			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 1.4, null);

			Assert.AreEqual(0, segmentInfos.Count);

			polyline = GeometryFactory.CreatePolyline(1000, 1000, 1001, 1000);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 1.1);

			Assert.AreEqual(1, segmentInfos.Count);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 0.9);

			Assert.AreEqual(0, segmentInfos.Count);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 1.0);

			Assert.AreEqual(0, segmentInfos.Count);
		}

		[Test]
		public void CanGetLongestSegment()
		{
			IPolyline polyline1 = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(0, 10),
				GeometryFactory.CreatePoint(0, 20));

			IPolyline polyline2 = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(10, 100),
				GeometryFactory.CreatePoint(10, 120));

			IGeometry union = GeometryUtils.Union(polyline1, polyline2);

			var segments = (ISegmentCollection) union;
			Assert.AreEqual(2, GeometryUtils.GetPartCount(union));
			Assert.AreEqual(4, segments.SegmentCount);

			ISegment longestSegment = GeometryUtils.GetLongestSegment(segments);

			Assert.IsNotNull(longestSegment);
			Assert.AreEqual(100, longestSegment.Length);
		}

		[Test]
		public void CanGetShortNonLinearSegments()
		{
			// non-linear 3D segments
			var constructionArc = (IConstructCircularArc) new CircularArc();

			IPoint from = GeometryFactory.CreatePoint(1000, 1000, 1000);
			IPoint middle = GeometryFactory.CreatePoint(1000, 1001, 2000);
			IPoint to = GeometryFactory.CreatePoint(1001, 1001, 1000);

			constructionArc.ConstructThreePoints(from, middle, to, false);

			IPolyline polyline =
				GeometryFactory.CreatePolyline((IGeometry) constructionArc);

			IList<esriSegmentInfo> segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.0);
			Assert.AreEqual(0, segmentInfos.Count);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.5);
			Assert.AreEqual(1, segmentInfos.Count);

			object missing = Type.Missing;
			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1002, 1001, 1000), ref missing, ref missing);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.5);
			Assert.AreEqual(2, segmentInfos.Count);

			// linear segments are measured correctly: 53.2...
			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1003, 1001, 1050), ref missing, ref missing);

			// added segment is long in 3D
			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.5);
			Assert.AreEqual(2, segmentInfos.Count);

			// .. but short in 2D
			segmentInfos =
				GeometryUtils.GetShortSegments((ISegmentCollection) polyline, 2.5, false,
				                               null);
			Assert.AreEqual(3, segmentInfos.Count);

			// non-linear 2D segments
			constructionArc = (IConstructCircularArc) new CircularArc();

			from = GeometryFactory.CreatePoint(1000, 1000);
			middle = GeometryFactory.CreatePoint(1000, 1001);
			to = GeometryFactory.CreatePoint(1001, 1001);

			constructionArc.ConstructThreePoints(from, middle, to, false);

			polyline = GeometryFactory.CreatePolyline((IGeometry) constructionArc);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.0);
			Assert.AreEqual(0, segmentInfos.Count);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.5);
			Assert.AreEqual(1, segmentInfos.Count);

			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1002, 1001, 1000), ref missing, ref missing);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.5);
			Assert.AreEqual(2, segmentInfos.Count);

			// linear segments are measured correctly: 53.2...
			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1003, 1001, 1050), ref missing, ref missing);

			segmentInfos =
				GeometryUtils.GetShortSegments(polyline, 2.5);
			Assert.AreEqual(3, segmentInfos.Count);
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForPath()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 10),
				GeometryFactory.CreatePoint(20, 0));

			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(3, angles.Length);
			Assert.IsNaN(angles[0]);
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[1]));
			Assert.IsNaN(angles[2]);
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForClosedPath()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 10),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(0, 0));

			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(4, angles.Length);
			Assert.AreEqual(45, MathUtils.ToDegrees(angles[0]));
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(45, MathUtils.ToDegrees(angles[2]));
			Assert.AreEqual(45, MathUtils.ToDegrees(angles[3]));
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForClosedPathWithObtuseAngle()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 2),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(0, 0));

			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(4, angles.Length);
			Assert.AreEqual(11.309932474020195, MathUtils.ToDegrees(angles[0]));
			Assert.AreEqual(157.38013505195957, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(11.309932474020195, MathUtils.ToDegrees(angles[2]));
			Assert.AreEqual(11.309932474020195, MathUtils.ToDegrees(angles[3]));
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForClosedPathWithIgnoredSegment()
		{
			// slightly sqewed rectangle
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(100, 1),
				GeometryFactory.CreatePoint(100, 11),
				GeometryFactory.CreatePoint(0, 10),
				GeometryFactory.CreatePoint(0, 0));

			const double ignoredAngleValue = 9999;
			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0),
				                                         new[] {2}, ignoredAngleValue);

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(5, angles.Length);

			Assert.AreEqual(89.427061302316531, MathUtils.ToDegrees(angles[0]));
			Assert.AreEqual(90.572938697683483, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(ignoredAngleValue, angles[2]);
			Assert.AreEqual(0, MathUtils.ToDegrees(angles[3])); // sides are parallel
			Assert.AreEqual(89.427061302316531, MathUtils.ToDegrees(angles[4]));
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForClosedPathWithIgnoredSegments()
		{
			// slightly sqewed rectangle
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(100, 1),
				GeometryFactory.CreatePoint(100, 11),
				GeometryFactory.CreatePoint(0, 11),
				GeometryFactory.CreatePoint(0, 0));

			const double ignoredAngleValue = 9999;
			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0),
				                                         new[] {1, 2}, ignoredAngleValue);

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(5, angles.Length);

			Assert.AreEqual(89.427061302316531, MathUtils.ToDegrees(angles[0]));
			Assert.AreEqual(ignoredAngleValue, angles[1]);
			Assert.AreEqual(ignoredAngleValue, angles[2]);
			Assert.AreEqual(89.427061302316531, MathUtils.ToDegrees(angles[3]));
			Assert.AreEqual(89.427061302316531, MathUtils.ToDegrees(angles[4]));
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForClosedPathWithIgnoredStartSegment()
		{
			// slightly sqewed rectangle
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(100, 1),
				GeometryFactory.CreatePoint(100, 11),
				GeometryFactory.CreatePoint(0, 11),
				GeometryFactory.CreatePoint(0, 0));

			const double ignoredAngleValue = 9999;
			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0),
				                                         new[] {0}, ignoredAngleValue);

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(5, angles.Length);

			Assert.AreEqual(ignoredAngleValue, angles[0]);
			Assert.AreEqual(0, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[2]));
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[3]));
			Assert.AreEqual(ignoredAngleValue, angles[4]);
		}

		[Test]
		public void
			CanGetLinearizedSegmentAnglesForClosedPathWithIgnoredStartEndSegments()
		{
			// slightly sqewed rectangle
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(100, 1),
				GeometryFactory.CreatePoint(100, 11),
				GeometryFactory.CreatePoint(0, 11),
				GeometryFactory.CreatePoint(0, 0));

			const double ignoredAngleValue = 9999;
			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0),
				                                         new[] {0, 3}, ignoredAngleValue);

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(5, angles.Length);

			Assert.AreEqual(ignoredAngleValue, angles[0]);
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[2]));
			Assert.AreEqual(ignoredAngleValue, angles[3]);
			Assert.AreEqual(ignoredAngleValue, angles[4]);
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForClosedPathWithAcuteAngle()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 100),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(0, 0));

			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(4, angles.Length);
			Assert.AreEqual(84.289406862500371, MathUtils.ToDegrees(angles[0]));
			Assert.AreEqual(11.421186274999291, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(84.289406862500371, MathUtils.ToDegrees(angles[2]));
			Assert.AreEqual(84.289406862500371, MathUtils.ToDegrees(angles[3]));
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForCollinearPath()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(5, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(15, 0));

			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(4, angles.Length);
			Assert.IsNaN(angles[0]);
			Assert.AreEqual(180, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(180, MathUtils.ToDegrees(angles[2]));
			Assert.IsNaN(angles[3]);
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForNonSimplePath()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(5, 0));

			var paths = (IGeometryCollection) polyline;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IPath) paths.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(3, angles.Length);
			Assert.IsNaN(angles[0]);
			Assert.AreEqual(0, MathUtils.ToDegrees(angles[1]));
			Assert.IsNaN(angles[2]);
		}

		[Test]
		public void CanGetLinearizedSegmentAnglesForRing()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 10),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(0, 0));

			IPolygon polygon = GeometryFactory.CreatePolygon(polyline);

			var rings = (IGeometryCollection) polygon;
			double[] angles =
				GeometryUtils.GetLinearizedSegmentAngles((IRing) rings.get_Geometry(0));

			foreach (double radians in angles)
			{
				Console.WriteLine(MathUtils.ToDegrees(radians));
			}

			Assert.AreEqual(4, angles.Length);
			Assert.AreEqual(45, MathUtils.ToDegrees(angles[0]));
			Assert.AreEqual(90, MathUtils.ToDegrees(angles[1]));
			Assert.AreEqual(45, MathUtils.ToDegrees(angles[2]));
			Assert.AreEqual(45, MathUtils.ToDegrees(angles[3]));
		}

		[Test]
		public void CanGetLength3D()
		{
			// non-linear segments
			var constructionArc = (IConstructCircularArc) new CircularArc();

			IPoint from = GeometryFactory.CreatePoint(1000, 1000, 1000);
			IPoint middle = GeometryFactory.CreatePoint(1000, 1001, 2000);
			IPoint to = GeometryFactory.CreatePoint(1001, 1001, 1000);

			constructionArc.ConstructThreePoints(from, middle, to, false);

			// non-linear segments assume constant slope: 2.2...
			Assert.Greater(GeometryUtils.GetLength3D((ICurve) constructionArc), 2);

			IPolyline polyline =
				GeometryFactory.CreatePolyline((IGeometry) constructionArc);
			Assert.Greater(GeometryUtils.GetLength3D(polyline), 2);

			object missing = Type.Missing;
			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1002, 1001, 1000), ref missing, ref missing);

			Assert.Greater(GeometryUtils.GetLength3D(polyline), 3);

			// linear segments are measured correctly: 53.2...
			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1003, 1001, 1050), ref missing, ref missing);
			Assert.Greater(GeometryUtils.GetLength3D(polyline), 53);

			// TODO: multiparts, polygon
		}

		[Test]
		public void CanCrackCircularArcPolycurve()
		{
			var constructionArc = (IConstructCircularArc) new CircularArc();

			IPoint centre = GeometryFactory.CreatePoint(2600000, 1200000);
			const double radius = 1000000d;

			IPoint from = GeometryFactory.CreatePoint(centre.X, centre.Y - radius);
			IPoint to = GeometryFactory.CreatePoint(centre.X + radius, centre.Y);

			constructionArc.ConstructEndPointsRadius(from, to, true, radius, true);

			IPolyline polyline =
				GeometryFactory.CreatePolyline((IGeometry) constructionArc);
			polyline.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var points = new List<IPoint>();

			const double deg135 = Math.PI * 0.75;
			points.Add(CreatePointOnCircle(centre.X, centre.Y, radius, deg135));

			// and a short-ish segment just above the tolerance
			const double deg135plus = Math.PI * 0.7500000005;
			points.Add(CreatePointOnCircle(centre.X, centre.Y, radius, deg135plus));

			const double deg120 = Math.PI * 2d / 3d;
			IPoint pointAt120Deg =
				CreatePointOnCircle(centre.X, centre.Y, radius, deg120);
			points.Add(pointAt120Deg);

			// and a point that is too far away with respect to cutOffDistance
			IPoint cutOffPoint = GeometryFactory.Clone(pointAt120Deg);
			cutOffPoint.X += 0.3;
			points.Add(cutOffPoint);

			var pointCollection =
				(IPointCollection) GeometryFactory.CreateMultipoint(points);

			IPoint origFrom = polyline.FromPoint;
			IPoint origTo = polyline.ToPoint;
			double origLenth = polyline.Length;

			IList<IPoint> splitPoints = GeometryUtils.SplitPolycurve(polyline,
			                                                         pointCollection,
			                                                         true,
			                                                         true, 0.01);

			var lineCollection = (IGeometryCollection) polyline;

			Assert.AreEqual(4, lineCollection.GeometryCount);
			Assert.IsTrue(GeometryUtils.AreEqualInXY(origFrom, polyline.FromPoint));
			Assert.IsTrue(GeometryUtils.AreEqualInXY(origTo, polyline.ToPoint));

			Assert.AreEqual(3, splitPoints.Count);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[0],
				              ((ICurve) lineCollection.get_Geometry(0)).ToPoint));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[0],
				              ((ICurve) lineCollection.get_Geometry(1)).FromPoint));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[1],
				              ((ICurve) lineCollection.get_Geometry(1)).ToPoint));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[1],
				              ((ICurve) lineCollection.get_Geometry(2)).FromPoint));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[2],
				              ((ICurve) lineCollection.get_Geometry(2)).ToPoint));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[2],
				              ((ICurve) lineCollection.get_Geometry(3)).FromPoint));

			Assert.AreEqual(Math.Round(origLenth, 1), Math.Round(polyline.Length, 1));
		}

		[Test]
		public void CanSplitAtPointWithNonLinearSegmentsAndCloseIntersectionPoints()
		{
			var constructionArc = (IConstructCircularArc) new CircularArc();

			IPoint centre = GeometryFactory.CreatePoint(2600000, 1200000);
			const double radius = 1000000d;

			IPoint from = GeometryFactory.CreatePoint(centre.X, centre.Y - radius);
			IPoint to = GeometryFactory.CreatePoint(centre.X + radius, centre.Y);

			constructionArc.ConstructEndPointsRadius(from, to, true, radius, true);

			IPolyline polyline =
				GeometryFactory.CreatePolyline((IGeometry) constructionArc);
			polyline.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			var points = new List<IPoint>();

			const double deg135 = Math.PI * 0.75;
			points.Add(CreatePointOnCircle(centre.X, centre.Y, radius, deg135));

			// and a short segment below the tolerance
			const double deg135plus = Math.PI * 0.75000000005;
			points.Add(CreatePointOnCircle(centre.X, centre.Y, radius, deg135plus));

			const double deg120 = Math.PI * 2d / 3d;
			IPoint pointAt120Deg =
				CreatePointOnCircle(centre.X, centre.Y, radius, deg120);
			points.Add(pointAt120Deg);

			// and a point that is too far away with respect to cutOffDistance
			IPoint cutOffPoint = GeometryFactory.Clone(pointAt120Deg);
			cutOffPoint.X += 0.3;
			points.Add(cutOffPoint);

			var pointCollection =
				(IPointCollection) GeometryFactory.CreateMultipoint(points);

			IPoint origFrom = polyline.FromPoint;
			IPoint origTo = polyline.ToPoint;
			double origLenth = polyline.Length;

			IList<IPoint> splitPoints = GeometryUtils.SplitPolycurve(polyline,
			                                                         pointCollection,
			                                                         true,
			                                                         true, 0.01);

			var lineCollection = (IGeometryCollection) polyline;

			Assert.AreEqual(3, lineCollection.GeometryCount);
			Assert.IsTrue(GeometryUtils.AreEqualInXY(origFrom, polyline.FromPoint));
			Assert.IsTrue(GeometryUtils.AreEqualInXY(origTo, polyline.ToPoint));

			Assert.AreEqual(3, splitPoints.Count);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[0],
				              ((ICurve) lineCollection.get_Geometry(0)).ToPoint));

			// but the split point is unused
			Assert.IsFalse(((ICurve) lineCollection.get_Geometry(1)).IsEmpty);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[1],
				              ((ICurve) lineCollection.get_Geometry(1)).FromPoint));
			double deltaXsqr = Math.Pow(
				((ICurve) lineCollection.get_Geometry(1)).FromPoint.X -
				splitPoints[1].X, 2);
			double deltaYsqr = Math.Pow(
				((ICurve) lineCollection.get_Geometry(1)).FromPoint.Y -
				splitPoints[1].Y, 2);

			double distance = Math.Sqrt(deltaXsqr + deltaYsqr);

			Assert.IsTrue(distance < GeometryUtils.GetXyTolerance(polyline));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[2],
				              ((ICurve) lineCollection.get_Geometry(1)).ToPoint));

			Assert.IsTrue(GeometryUtils.AreEqualInXY(
				              splitPoints[2],
				              ((ICurve) lineCollection.get_Geometry(2)).FromPoint));

			Assert.AreEqual(Math.Round(origLenth, 1), Math.Round(polyline.Length, 1));
		}

		private static IPoint CreatePointOnCircle(double centerX, double centerY,
		                                          double radius, double radians)
		{
			double sin = Math.Sin(radians);
			double cos = Math.Cos(radians);

			return GeometryFactory.CreatePoint(centerX + sin * radius,
			                                   centerY + cos * radius,
			                                   double.NaN);
		}

		[Test]
		public void CanCrackAtFromToPoint()
		{
			IPolyline polylineOriginal =
				GeometryFactory.CreatePolyline(1000, 1000, 1001, 1001);
			IPolyline polyline = GeometryFactory.Clone(polylineOriginal);

			var splitPoint = (IPointCollection) GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(1000, 1000, 40),
				GeometryFactory.CreatePoint(1001, 1001));

			IList<IPoint> ensuredVertices = GeometryUtils.CrackPolycurve(
				polyline, splitPoint, true, false);
			Assert.AreEqual(2, ensuredVertices.Count);

			polyline = GeometryFactory.Clone(polylineOriginal);

			const bool createParts = true;
			ensuredVertices = GeometryUtils.CrackPolycurve(polyline, splitPoint, true,
			                                               createParts);
			Assert.AreEqual(2, ensuredVertices.Count);
		}

		[Test]
		public void CanGetHasUndefinedZValuesAfterCrackingCurve()
		{
			// NOTE: updating non-linear segments by splitting existing segments
			//		 requires a call to SegmentsChanged to obtain correct results from ZSimple
			// non-linear segments
			var constructionArc = (IConstructCircularArc) new CircularArc();

			IPoint from = GeometryFactory.CreatePoint(1000, 1000, 1000);
			IPoint middle = GeometryFactory.CreatePoint(1000, 1001, 1000);
			IPoint to = GeometryFactory.CreatePoint(1001, 1001, double.NaN);

			constructionArc.ConstructThreePoints(from, middle, to, false);

			IPolyline polyline =
				GeometryFactory.CreatePolyline((IGeometry) constructionArc);
			polyline.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			GeometryUtils.MakeZAware(polyline);

			object missing = Type.Missing;
			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1002, 1001, 1000), ref missing, ref missing);

			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1003, 1002, 1000), ref missing, ref missing);

			Assert.IsTrue(GeometryUtils.HasUndefinedZValues(polyline));

			GeometryUtils.SimplifyZ(polyline);

			Assert.IsFalse(GeometryUtils.HasUndefinedZValues(polyline));

			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(1004, 1002, double.NaN), ref missing,
				ref missing);

			Assert.IsTrue(GeometryUtils.HasUndefinedZValues(polyline));

			GeometryUtils.SimplifyZ(polyline);

			Assert.IsFalse(GeometryUtils.HasUndefinedZValues(polyline));

			var splitPoint = (IPointCollection) GeometryFactory.CreateMultipoint(
				new List<IPoint> {GeometryFactory.CreatePoint(1002.5, 1001, double.NaN)});

			GeometryUtils.CrackPolycurve(polyline, splitPoint, false, false, null);

			Assert.IsTrue(GeometryUtils.HasUndefinedZValues(polyline));
		}

		[Test]
		public void CanGetIsSimple()
		{
			// might be simple in ArcGIS 10 (taking into account Z)
			IPolyline polyline = GeometryFactory.CreatePolyline(
				1000, 1000, 1010, 1000.003,
				1000,
				1000);
			polyline.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			((ISpatialReferenceResolution) polyline.SpatialReference).set_XYResolution(
				true,
				0.00125);
			((ISpatialReferenceResolution) polyline.SpatialReference).set_ZResolution(
				true,
				0.00125);
			((ISpatialReferenceTolerance) polyline.SpatialReference).XYTolerance = 0.0125;
			((ISpatialReferenceTolerance) polyline.SpatialReference).ZTolerance = 0.0125;

			bool simple = GeometryUtils.IsGeometrySimple(
				polyline, polyline.SpatialReference,
				false, out string _);

			Assert.IsFalse(simple);
		}

		[Test]
		public void CanGetIsSimpleMAware()
		{
			IPolyline polyline = new PolylineClass();

			((IZAware) polyline).ZAware = true;
			((IMAware) polyline).MAware = true;

			var pointCollection = (IPointCollection4) polyline;

			object missing = Type.Missing;

			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1000, 1000, 2),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1002, 1000, 2.3),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1002, 1000, 7),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1001, 1000, 8.3),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(990, 1001, 1000, 7),
			                         ref missing,
			                         ref missing);

			polyline.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			((ISpatialReferenceResolution) polyline.SpatialReference).set_XYResolution(
				true,
				0.00125);
			((ISpatialReferenceResolution) polyline.SpatialReference).set_ZResolution(
				true,
				0.00125);
			((ISpatialReferenceResolution) polyline.SpatialReference).MResolution = 0.001;
			((ISpatialReferenceTolerance) polyline.SpatialReference).XYTolerance = 0.0125;
			((ISpatialReferenceTolerance) polyline.SpatialReference).ZTolerance = 0.0125;
			((ISpatialReferenceTolerance) polyline.SpatialReference).MTolerance = 0.01;

			bool simple = GeometryUtils.IsGeometrySimple(
				polyline, polyline.SpatialReference,
				false, out string _);

			// detects self-intersections despite M awareness
			Assert.IsFalse(simple);
		}

		[Test]
		public void CanGetIsSimpleSelfIntersecting()
		{
			string filePath = TestData.GetSelfIntersectingPolygonPath();
			var poly = (IPolygon) ReadGeometryFromXML(filePath);

			GeometryNonSimpleReason? nonSimpleReason;
			Assert.False(GeometryUtils.IsGeometrySimple(poly, poly.SpatialReference, true,
			                                            out string _,
			                                            out nonSimpleReason));

			Assert.AreEqual(GeometryNonSimpleReason.SelfIntersections, nonSimpleReason);

			// Self-intersecting plus duplicate vertex:
			var extraPoint = new IPoint[1];
			extraPoint[0] = GeometryFactory.Clone(((IPointCollection) poly).get_Point(2));

			GeometryUtils.GeometryBridge.InsertPoints((IPointCollection4) poly, 2,
			                                          extraPoint);

			// Short segments wins (because normally self-intersections are also reported for short segments
			// TODO: add overload with bool ignoreShortSegments
			Assert.False(GeometryUtils.IsGeometrySimple(poly, poly.SpatialReference, true,
			                                            out string _,
			                                            out nonSimpleReason));

			Assert.AreEqual(GeometryNonSimpleReason.ShortSegments, nonSimpleReason);
		}

		[Test]
		public void CanGetIsSimpleUnclosedPolygon()
		{
			string filePath = TestData.GetUnclosedPolygonPath();
			var poly = (IPolygon) ReadGeometryFromXML(filePath);

			GeometryNonSimpleReason? nonSimpleReason;
			Assert.False(GeometryUtils.IsGeometrySimple(poly, poly.SpatialReference, true,
			                                            out string _, out nonSimpleReason));

			Assert.IsTrue(nonSimpleReason == GeometryNonSimpleReason.UnclosedRing);

			// unclosed ring plus duplicate vertex:
			var extraPoint = new IPoint[1];
			extraPoint[0] = GeometryFactory.Clone(((IPointCollection) poly).get_Point(2));

			GeometryUtils.GeometryBridge.InsertPoints((IPointCollection4) poly, 2,
			                                          extraPoint);

			// TODO: add overload with bool ignoreShortSegments
			Assert.False(GeometryUtils.IsGeometrySimple(poly, poly.SpatialReference, true,
			                                            out string _, out nonSimpleReason));

			Assert.IsTrue(
				nonSimpleReason == GeometryNonSimpleReason.IncorrectSegmentOrientation ||
				nonSimpleReason == GeometryNonSimpleReason.UnclosedRing);
		}

		[Test]
		public void CanGetIsSimpleInvertedSegmentOrientation()
		{
			string filePath = TestData.GetDuplicateVertexPolygonPath();
			var poly = (IPolygon) ReadGeometryFromXML(filePath);

			GeometryNonSimpleReason? nonSimpleReason;

			// NOTE: Duplicate points are currently (10.2.2) not found by get_isSimpleEx
			// (see Repro_ITopologicalOperator3GetIsSimpleExDoesNotFind0LengthSegments)
			// But the clean tool does (because it does not rely on IsGeometrySimple)
			Assert.True(GeometryUtils.IsGeometrySimple(poly, poly.SpatialReference, true,
			                                           out string _, out nonSimpleReason));

			// Simplify to make it correct:
			GeometryUtils.Simplify(poly);

			// Reverse a segment:
			((ISegmentCollection) poly).get_Segment(2).ReverseOrientation();

			Assert.False(GeometryUtils.IsGeometrySimple(poly, poly.SpatialReference, true,
			                                            out string _, out nonSimpleReason));

			Assert.AreEqual(GeometryNonSimpleReason.IncorrectSegmentOrientation,
			                nonSimpleReason);
		}

		[Test]
		public void CanGetIsSimpleInvertedRingOrientation()
		{
			string filePath = TestData.GetInvertedRingPolygonPath();
			var poly = (IPolygon) ReadGeometryFromXML(filePath);

			GeometryNonSimpleReason? nonSimpleReason;

			Assert.IsFalse(GeometryUtils.IsGeometrySimple(
				               poly, poly.SpatialReference, true,
				               out string _, out nonSimpleReason));

			Assert.AreEqual(GeometryNonSimpleReason.IncorrectRingOrientation,
			                nonSimpleReason);

			// Reverse the outer ring:
			((IRing) ((IGeometryCollection) poly).get_Geometry(0)).ReverseOrientation();

			// The inner ring is still wrong:
			Assert.IsFalse(GeometryUtils.IsGeometrySimple(
				               poly, poly.SpatialReference, true,
				               out string _, out nonSimpleReason));

			Assert.AreEqual(GeometryNonSimpleReason.IncorrectRingOrientation,
			                nonSimpleReason);

			((IRing) ((IGeometryCollection) poly).get_Geometry(1)).ReverseOrientation();

			Assert.IsTrue(GeometryUtils.IsGeometrySimple(
				              poly, poly.SpatialReference, true,
				              out string _, out _));
		}

		[Test]
		public void CanWeed()
		{
			string xmlFile = TestData.GetDensifiedWorkUnitPerimeterPath();

			var densifiedPolyZ = (IPolygon) GeometryUtils.FromXmlFile(xmlFile);

			//
			// Current work-around for tolerance factor 0
			//
			var weeded0PolyWorkAround = (IPolygon) ((IClone) densifiedPolyZ).Clone();
			GeometryUtils.Weed(weeded0PolyWorkAround, 0.0);

			Assert.IsTrue(
				((IRelationalOperator) weeded0PolyWorkAround).Equals(densifiedPolyZ),
				"Weed with 0.0 in GeometryUtils changed the geometry significantly (more than the tolerance)");

			Assert.AreEqual(6, ((IPointCollection) weeded0PolyWorkAround).PointCount,
			                "Unexpected point count.");

			//
			// Weed in Z
			//
			var weeded0PolyZWorkAround = (IPolygon) ((IClone) densifiedPolyZ).Clone();

			int originalCount = ((IPointCollection) weeded0PolyZWorkAround).PointCount;

			GeometryUtils.Weed3D(weeded0PolyZWorkAround, 0.0);

			Assert.IsTrue(
				((IRelationalOperator) weeded0PolyZWorkAround).Equals(densifiedPolyZ),
				"Weed with 0.0 in GeometryUtils changed the geometry significantly (more than the tolerance)");

			Assert.AreEqual(originalCount,
			                ((IPointCollection) weeded0PolyZWorkAround).PointCount,
			                "Unexpected point count.");

			// do some actual weeding:
			GeometryUtils.Weed3D(weeded0PolyZWorkAround, 25.0);

			Assert.IsTrue(
				((IRelationalOperator) weeded0PolyZWorkAround).Equals(densifiedPolyZ),
				"Weed with 0.0 in GeometryUtils changed the geometry significantly (more than the tolerance)");

			Assert.AreEqual(1895, ((IPointCollection) weeded0PolyZWorkAround).PointCount,
			                "Unexpected point count.");

			// 2D only:
			GeometryUtils.Weed(weeded0PolyZWorkAround, 25.0);
			Assert.AreEqual(6, ((IPointCollection) weeded0PolyZWorkAround).PointCount,
			                "Unexpected point count.");
		}

		[Test]
		public void CanWeedNonLinearSegments()
		{
			// NOTE: when weeeding without the densify-work-around in weed, 
			//		 a tolerance is used which is far too large!
			IPolygon circle =
				GeometryFactory.CreateCircleArcPolygon(
					GeometryFactory.CreatePoint(2600000, 1200000), 50);

			circle.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolygon clone = GeometryFactory.Clone(circle);
			GeometryUtils.Weed(clone, 0.1);

			Assert.IsTrue(((IPointCollection) clone).PointCount > 700);

			IPolygon clone2 = GeometryFactory.Clone(circle);
			GeometryUtils.MakeZAware(clone2);
			GeometryUtils.ApplyConstantZ(clone2, 500);

			GeometryUtils.Weed(clone2, 0.1);

			Assert.IsTrue(((IPointCollection) clone2).PointCount > 700);

			Assert.IsTrue(((IPointCollection) clone).PointCount ==
			              ((IPointCollection) clone2).PointCount);
		}

		[Test]
		public void CanWeed3DNonLinearSegments()
		{
			// NOTE: when weeeding without the densify-work-around in weed, 
			//		 a tolerance is used which is far too large!
			IPolygon circle =
				GeometryFactory.CreateCircleArcPolygon(
					GeometryFactory.CreatePoint(2600000, 1200000), 50);

			circle.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolygon clone = GeometryFactory.Clone(circle);
			GeometryUtils.MakeZAware(clone);
			GeometryUtils.ApplyConstantZ(clone, 500);

			GeometryUtils.Weed3D(clone, 0.1);

			Assert.IsTrue(((IPointCollection) clone).PointCount > 700);
		}

		[Test]
		public void CanGeneralizeNonLinearSegments()
		{
			IPolygon circle =
				GeometryFactory.CreateCircleArcPolygon(
					GeometryFactory.CreatePoint(2600000, 1200000), 50);

			circle.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolygon clone = GeometryFactory.Clone(circle);
			GeometryUtils.Generalize(clone, 0.01);

			Assert.IsTrue(((IPointCollection) clone).PointCount > 250);

			IPolygon clone2 = GeometryFactory.Clone(circle);
			GeometryUtils.MakeZAware(clone2);
			GeometryUtils.ApplyConstantZ(clone2, 500);

			GeometryUtils.Generalize(clone2, 0.01);

			Assert.IsTrue(((IPointCollection) clone2).PointCount > 250);

			Assert.IsTrue(((IPointCollection) clone).PointCount ==
			              ((IPointCollection) clone2).PointCount);
		}

		[Test]
		public void CanLinearize()
		{
			IPoint point1 = GeometryFactory.CreatePoint(
				2600000, 1200000, 400);

			IPoint point2 = GeometryFactory.CreatePoint(
				2600100, 1200000); // z value of middle point is irrelevant

			IPoint point3 = GeometryFactory.CreatePoint(
				2600100, 1200100, 800);

			ICircularArc arc = GeometryFactory.CreateCircularArc(point1, point2, point3);

			IGeometry arcPolyline = GeometryUtils.GetHighLevelGeometry(arc);
			arcPolyline.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			GeometryUtils.MakeZAware(arcPolyline);

			GeometryUtils.Linearize((IPolycurve) arcPolyline, 0.001);

			Assert.IsTrue(((IPointCollection) arcPolyline).PointCount > 200);

			int middlePointIdx =
				Convert.ToInt32(((IPointCollection) arcPolyline).PointCount / 2);

			double zMiddle = ((IPointCollection) arcPolyline).get_Point(middlePointIdx).Z;

			Assert.IsTrue(599 < zMiddle && zMiddle < 601, "Zs not properly interpolated");
		}

		[Test]
		public void CanCalculateRegularPolyVertices()
		{
			IPoint centre = GeometryFactory.CreatePoint(100, 100);

			WKSPointZ[] fourPoints = GeometryUtils.CalculateRegularPolyVertices(
				centre, 10, 4,
				0);

			Assert.AreEqual(110, fourPoints[0].X);
			Assert.AreEqual(100, fourPoints[0].Y);

			Assert.AreEqual(100, fourPoints[1].X);
			Assert.AreEqual(110, fourPoints[1].Y);

			// rotate so the start point is at 12 o'clock
			fourPoints =
				GeometryUtils.CalculateRegularPolyVertices(centre, 10, 4, Math.PI / 2);

			Assert.AreEqual(100, fourPoints[0].X);
			Assert.AreEqual(110, fourPoints[0].Y);

			Assert.AreEqual(90, fourPoints[1].X);
			Assert.AreEqual(100, fourPoints[1].Y);

			// same with an intersecting vertex
			fourPoints = GeometryUtils.CalculateRegularPolyVertices(centre, 10, 4,
			                                                        GeometryFactory
				                                                        .CreatePoint
					                                                        (100, 110));

			Assert.AreEqual(100, fourPoints[0].X);
			Assert.AreEqual(110, fourPoints[0].Y);

			Assert.AreEqual(90, fourPoints[1].X);
			Assert.AreEqual(100, fourPoints[1].Y);

			// rotate just a little clockwise - ca. 4 o'clock
			IPoint fourOClock = GeometryFactory.CreatePoint(118, 98);
			fourPoints =
				GeometryUtils.CalculateRegularPolyVertices(centre, 10, 4, fourOClock);

			// fourPoints[0] should be on the line between centre and start point 
			IPolyline line = GeometryFactory.CreateLine(centre, fourOClock);

			IPoint nearestPoint = new PointClass();
			double distance =
				GeometryUtils.GetDistanceFromCurve(
					GeometryFactory.CreatePoint(fourPoints[0]),
					line, nearestPoint);

			Assert.AreEqual(0, distance);
		}

		[Test]
		public void CanConvertToFromWkbGeometry()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPoint point = GeometryFactory.CreatePoint(2601234.567, 1205678.901, lv95);

			byte[] aoBytes = AssertWkbConversionAO(point);

			WkbGeometryWriter writer = new WkbGeometryWriter();
			byte[] psBytes = writer.WritePoint(point);

			Assert.AreEqual(aoBytes, psBytes);

			WkbGeometryReader reader = new WkbGeometryReader();
			IPoint restoredFromPsBytes = reader.ReadPoint(new MemoryStream(psBytes));
			Assert.IsTrue(GeometryUtils.AreEqual(point, restoredFromPsBytes));

			point = GeometryFactory.CreatePoint(2601234.567, 1205678.901, 401.234);
			point.SpatialReference = lv95;

			// In ArcObjects (10.6.1) Z & M coordinates are lost when converting to WKB
			AssertWkbConversionAO(point, true);

			point = GeometryFactory.CreatePoint(2601234.567, 1205678.901, 401.234, 12345);
			point.SpatialReference = lv95;

			// In ArcObjects (10.6.1) Z & M coordinates are lost when converting to WKB
			AssertWkbConversionAO(point, true);

			var pointCollection =
				(IPointCollection4)
				GeometryFactory.CreateEmptyGeometry(
					esriGeometryType.esriGeometryPolyline);

			object missing = Type.Missing;

			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1000),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1002),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1002),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1001),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(990, 1001),
			                         ref missing,
			                         ref missing);

			var polyline = (IPolyline) pointCollection;
			polyline.SpatialReference = lv95;
			GeometryUtils.Simplify(polyline, true, true);

			aoBytes = AssertWkbConversionAO(polyline);
			psBytes = writer.WritePolyline(polyline);

			Assert.AreEqual(aoBytes, psBytes);

			IPolyline restoredLineFromPsBytes = reader.ReadPolyline(new MemoryStream(psBytes));
			Assert.IsTrue(GeometryUtils.AreEqual(polyline, restoredLineFromPsBytes));

			IPolygon polygon = GeometryFactory.CreatePolygon(polyline);
			GeometryUtils.Simplify(polygon);

			aoBytes = AssertWkbConversionAO(polygon);
			psBytes = writer.WritePolygon(polygon);

			Assert.AreEqual(aoBytes, psBytes);

			IPolygon restoredPoly = reader.ReadPolygon(new MemoryStream(psBytes));

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, restoredPoly));
		}

		[Test]
		public void CanConvertFromToEsriShapeType()
		{
			// Guard against changes in future versions:

			Assert.AreEqual(Enum.GetNames(typeof(EsriShapeType)).Length,
			                Enum.GetNames(typeof(esriShapeType)).Length);

			foreach (object value in Enum.GetValues(typeof(esriShapeType)))
			{
				int intValue = (int) value;

				var psShapeType = (EsriShapeType) intValue;

				Assert.AreEqual(psShapeType.ToString().ToUpper(), value.ToString().ToUpper());
			}
		}

		[Test]
		public void CanConvertFromToEsriGeometryType()
		{
			// Guard against changes in future versions:

			Assert.AreEqual(Enum.GetNames(typeof(ProSuiteGeometryType)).Length,
			                Enum.GetNames(typeof(esriGeometryType)).Length);

			foreach (object value in Enum.GetValues(typeof(esriGeometryType)))
			{
				int intValue = (int) value;

				var psGeometryType = (ProSuiteGeometryType) intValue;

				string removedPrefix = value.ToString().Substring(12);

				Assert.AreEqual(psGeometryType.ToString(), removedPrefix);
			}
		}

		[Test]
		public void CanConvertFromMultipointArray()
		{
			// Real-worlds multipoint:
			var bytes = Encoding.Default.GetBytes(
				"\n\0\0\0]\u0002J)7ˆDA){Wd1)3AˆëQØ8ˆDAÐMb°4)3A\u0001\0\0\0\u0002\0\0\0\0\0\0\0ˆëQØ8ˆDAÐMb°4)3A]\u0002J)7ˆDA){Wd1)3AÊ¶\"iNð~@\05^ºIô~@\05^ºIô~@Ê¶\"iNð~@");

			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

			const int count = 8;

			// First run on MTA thread-pool threads, expect decent error message
			Task[] tasks = new Task[count];
			for (int i = 0; i < count; i++)
			{
				Task<IGeometry> task =
					Task.Factory.StartNew(
						() =>
						{
							try
							{
								var result = GeometryUtils.FromEsriShapeBuffer(bytes);

								// oddly, it sometimes succeeds even on MTA!
								// Assert.Fail("Should not succeed on MTA");

								return result;
							}
							catch (Exception exception)
							{
								Console.WriteLine(exception.Message);
								throw;
							}
						});

				tasks[i] = task;
			}

			Assert.Throws<AggregateException>(() => Task.WaitAll(tasks));

			// Works on STA:
			var staScheduler = new StaTaskScheduler(count);

			for (int i = 0; i < count; i++)
			{
				Task<IGeometry> task =
					Task.Factory.StartNew(
						() => GeometryUtils.FromEsriShapeBuffer(bytes),
						cancellationTokenSource.Token, TaskCreationOptions.LongRunning,
						staScheduler);

				tasks[i] = task;
			}

			Task.WaitAll(tasks);
		}

		[Test]
		public void CanConvertToFromEsriShapeBuffer()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPoint point = GeometryFactory.CreatePoint(2601234.567, 1205678.901, lv95);

			AssertExactEsriShapeBufferConversion(point);

			point = GeometryFactory.CreatePoint(2601234.567, 1205678.901, 401.234);
			point.SpatialReference = lv95;

			AssertExactEsriShapeBufferConversion(point);

			point = GeometryFactory.CreatePoint(2601234.567, 1205678.901, 401.234, 12345);
			point.SpatialReference = lv95;
			try
			{
				AssertExactEsriShapeBufferConversion(point);
			}
			catch (Exception)
			{
				// Unfortunately, at 10.2.2 Z & M coordinates are lost when converting to WKB
			}

			var pointCollection =
				(IPointCollection4)
				GeometryFactory.CreateEmptyGeometry(
					esriGeometryType.esriGeometryPolyline);

			object missing = Type.Missing;

			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1000),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1002),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1002),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1001),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(990, 1001),
			                         ref missing,
			                         ref missing);

			var polyline = (IPolyline) pointCollection;
			polyline.SpatialReference = lv95;
			GeometryUtils.Simplify(polyline, true, true);

			AssertExactEsriShapeBufferConversion(polyline);

			IPolygon polygon = GeometryFactory.CreatePolygon(polyline);
			GeometryUtils.Simplify(polygon);
			AssertExactEsriShapeBufferConversion(polygon);

			GeometryUtils.MakeZAware(polygon);
			GeometryUtils.ConstantZ(polygon, 17.4);
			AssertExactEsriShapeBufferConversion(polygon);

			// Multipatches return with empty geometry (10.6.1)!
			IMultiPatch multipatch = GeometryFactory.CreateMultiPatch(polygon);
			
			byte[] bytes = GeometryUtils.ToEsriShapeBuffer(multipatch);
			IGeometry restoredGeometry = GeometryUtils.FromEsriShapeBuffer(bytes);
			Assert.IsTrue(restoredGeometry.IsEmpty);
		}

		[Test]
		public void WkbGeometryConversionPerformance()
		{
			Console.WriteLine(@"Loading huge Lockergestein polygon...");
			string filePath = TestData.GetHugeLockergesteinPolygonPath();

			var polygon = (IPolygon) ReadGeometryFromXML(filePath);

			Stopwatch watch = Stopwatch.StartNew();

			var writer = new WkbGeometryWriter();

			byte[] bytes = writer.WritePolygon(polygon);
			//byte[] bytes = GeometryUtils.ToWkb(polygon);

			watch.Stop();

			Console.WriteLine(@"Exported huge Lockergestein polygon to WKB  in {0}ms",
			                  watch.Elapsed.TotalMilliseconds);

			watch = Stopwatch.StartNew();

			var reader = new WkbGeometryReader();

			IPolygon restored1 = reader.ReadPolygon(new MemoryStream(bytes));
			//IGeometry restored = GeometryUtils.FromWkb(bytes);

			IPoint point = ((IPointCollection) restored1).get_Point(123);

			watch.Stop();

			Console.WriteLine(@"Imported huge Lockergestein polygon from WKB in {0}ms (1. Run)",
			                  watch.Elapsed.TotalMilliseconds);

			watch.Restart();
			reader = new WkbGeometryReader();
			IPolygon restored2 = reader.ReadPolygon(new MemoryStream(bytes));
			point = ((IPointCollection) restored2).get_Point(123);
			watch.Stop();

			Console.WriteLine(@"Imported huge Lockergestein polygon from WKB in {0}ms (2. Run)",
			                  watch.Elapsed.TotalMilliseconds);

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, restored1));
			Assert.IsTrue(GeometryUtils.AreEqual(polygon, restored2));
			// ArcObjects implementation (which loses Z-Values):
			// Loading huge Lockergestein polygon...
			// Exported huge Lockergestein polygon to WKB  in 5.3186ms
			// Imported huge Lockergestein polygon from WKB  in 19.5364ms

			// New implementation (WkbGeometryWriter/-Reader):
			// Loading huge Lockergestein polygon...
			// Exported huge Lockergestein polygon to WKB  in 76.5022ms
			// Imported huge Lockergestein polygon from WKB in 329.6099ms (1. Run)
			// Imported huge Lockergestein polygon from WKB in 76.5898ms (2. Run)

			// NOTE on the performance of the WkbGeometryWriter implementation:
			// Getting IGeometry parts from IGeometryCollection: 286ms
			// -> replaced with GeometryUtils.GetPointCountPerPart
			// Getting WKSPointZs: 30ms
			// Actual writing (WritePointCore(point, ordinates)) takes 38ms

			// NOTE on the performance of the WkbGeometryReader implementation:
			// Actual reading of the linestrings: 27ms
			// Creation of first ring: 250ms (!) but only in the first run.
		}

		[Test]
		public void EsriShapeBufferGeometryConversionPerformance()
		{
			Console.WriteLine(@"Loading huge Lockergestein polygon...");
			string filePath = TestData.GetHugeLockergesteinPolygonPath();

			var polygon = (IPolygon) ReadGeometryFromXML(filePath);

			Stopwatch watch = Stopwatch.StartNew();

			byte[] bytes = GeometryUtils.ToEsriShapeBuffer(polygon);

			watch.Stop();

			Console.WriteLine(
				@"Exported huge Lockergestein polygon to ESRI shape buffer in {0}ms",
				watch.Elapsed.TotalMilliseconds);

			watch = Stopwatch.StartNew();

			IGeometry restored = GeometryUtils.FromEsriShapeBuffer(bytes);

			// without reading a point it takes less than 3ms! -> force actual unpacking
			IPoint point = ((IPointCollection) restored).get_Point(123);

			watch.Stop();

			Console.WriteLine(
				@"Imported huge Lockergestein polygon from ESRI shape buffer in {0}ms",
				watch.Elapsed.TotalMilliseconds);

			// Exported huge Lockergestein polygon to ESRI shape buffer in 46.1223ms (surprisingly slow, compared to WKB)
			// Imported huge Lockergestein polygon from ESRI shape buffer in 29.4113ms (includes internal un-packing of points. Othwerwise only 3ms)
		}

		[Test]
		public void CanGetExteriorRingCountForNonSimplePolygon()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			((ISpatialReferenceTolerance) lv95).XYTolerance = 0.01;
			((ISpatialReferenceResolution) lv95).set_XYResolution(true, 0.001);

			var pointCollection =
				(IPointCollection4)
				GeometryFactory.CreateEmptyGeometry(esriGeometryType.esriGeometryPolygon);

			object missing = Type.Missing;

			pointCollection.AddPoint(
				GeometryFactory.CreatePoint(2577058.6860000007, 1182017.2150000036,
				                            644.83299999999872),
				ref missing,
				ref missing);
			pointCollection.AddPoint(
				GeometryFactory.CreatePoint(2577055.7829999998, 1182026.2690000013,
				                            644.83299999999872),
				ref missing,
				ref missing);
			pointCollection.AddPoint(
				GeometryFactory.CreatePoint(2577055.8079999983, 1182026.2770000026,
				                            644.86403706394265),
				ref missing,
				ref missing);
			pointCollection.AddPoint(
				GeometryFactory.CreatePoint(2577058.6860000007, 1182017.2150000036,
				                            644.83299999999872),
				ref missing,
				ref missing);

			var polygon = (IPolygon) pointCollection;
			polygon.SpatialReference = lv95;

			int ringCount = GeometryUtils.GetExteriorRingCount(polygon, false);

			Assert.AreEqual(0, ringCount);
			Assert.IsFalse(polygon.IsEmpty);

			ringCount = GeometryUtils.GetExteriorRingCount(polygon);

			Assert.AreEqual(0, ringCount);
			Assert.IsTrue(polygon.IsEmpty);
		}

		[Test]
		public void CanGetBoundaryAndCreatePolygonFromSmallMultipatch()
		{
			string multipatchLocation =
				TestUtils.GetGeometryTestDataPath("MultipatchWithWrongBoundary.xml");
			var multipatch = (IMultiPatch) GeometryUtils.FromXmlFile(multipatchLocation);

			var boundary = (IPolyline) GeometryUtils.GetBoundary(multipatch);

			var onlyRing = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			// The lenght of the ring should be equal the length of the boundary of the multipatch. 
			// But it is not:
			Assert.AreEqual(onlyRing.Length, boundary.Length);

			IPolygon polygon = GeometryFactory.CreatePolygon(multipatch);

			Assert.AreEqual(((IArea) onlyRing).Area, ((IArea) polygon).Area);
		}

		[Test]
		public void CanExtendLine()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			var pointCollection =
				(IPointCollection4)
				GeometryFactory.CreateEmptyGeometry(
					esriGeometryType.esriGeometryPolyline);

			object missing = Type.Missing;

			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1001),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1000, 1002),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1001, 1002),
			                         ref missing,
			                         ref missing);
			pointCollection.AddPoint(GeometryFactory.CreatePoint(1002, 1001),
			                         ref missing,
			                         ref missing);

			var polyline = (IPolyline) pointCollection;
			polyline.SpatialReference = lv95;

			var targetPoints =
				(IPointCollection4)
				GeometryFactory.CreateEmptyGeometry(
					esriGeometryType.esriGeometryPolyline);

			targetPoints.AddPoint(GeometryFactory.CreatePoint(1000, 1000),
			                      ref missing,
			                      ref missing);
			targetPoints.AddPoint(GeometryFactory.CreatePoint(1010, 1000),
			                      ref missing,
			                      ref missing);

			IPolyline targetPolyline = (IPolyline) targetPoints;
			targetPolyline.SpatialReference = lv95;

			// Extend at to-point:
			TestExtendedLine(polyline, targetPolyline, LineEnd.To, Math.Sqrt(2));

			// at from Point:
			TestExtendedLine(polyline, targetPolyline, LineEnd.From, 1.0);

			// at from and to point:
			TestExtendedLine(polyline, targetPolyline, LineEnd.Both, 1 + Math.Sqrt(2));

			targetPolyline.SetEmpty();
			targetPoints.AddPoint(GeometryFactory.CreatePoint(1000, 1000),
			                      ref missing,
			                      ref missing);
			targetPoints.AddPoint(GeometryFactory.CreatePoint(1001, 1000),
			                      ref missing,
			                      ref missing);

			// Now the To-End does not intersect any more:
			TestExtendedLine(polyline, targetPolyline, LineEnd.To, double.NaN);

			TestExtendedLine(polyline, targetPolyline, LineEnd.From, 1.0);
			TestExtendedLine(polyline, targetPolyline, LineEnd.Both, 1.0);
		}

		[Test]
		public void HitTestMultipointPerformance()
		{
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			var bigPoly = (IPolygon) TestUtils.ReadGeometryFromXml(filePath);

			IMultipoint multipoint = GeometryFactory.CreateMultipoint((IPointCollection) bigPoly);

			IPoint testPoint = new PointClass();
			testPoint.X = 2734500;
			testPoint.Y = 1200123;
			testPoint.SpatialReference = bigPoly.SpatialReference;

			IPoint hitPoint = GeometryFactory.Clone(testPoint);

			double hitDistance = 0;
			int partIndex = 0;
			int segmentIndex = 0;
			bool rightSide = false;

			IHitTest hitTest = (IHitTest) multipoint;

			Stopwatch watch = Stopwatch.StartNew();

			// This makes no difference - it seems that multipoint hit test does not use the spatial index
			GeometryUtils.AllowIndexing(multipoint);

			bool found = hitTest.HitTest(
				testPoint, 0.01,
				esriGeometryHitPartType.esriGeometryPartVertex, hitPoint,
				ref hitDistance, ref partIndex, ref segmentIndex, ref rightSide);

			Assert.IsFalse(found);

			watch.Stop();

			Console.WriteLine("First HitTest (AO): {0}", watch.ElapsedMilliseconds);

			watch.Restart();
			for (int i = 1000; i < 10000; i += 100)
			{
				((IPointCollection) multipoint).QueryPoint(i, testPoint);

				found = hitTest.HitTest(
					testPoint, 0.01,
					esriGeometryHitPartType.esriGeometryPartVertex, hitPoint,
					ref hitDistance, ref partIndex, ref segmentIndex, ref rightSide);

				Assert.IsTrue(found);
				//index = GeometryUtils.FindHitVertexIndex(multipoint, testPoint, 0.01, out partIdx);
			}

			watch.Stop();
			Console.WriteLine("Next 100 HitTests (AO): {0}", watch.ElapsedMilliseconds);

			Multipoint<Commons.Geom.IPnt> multipnt =
				GeometryConversionUtils.CreateMultipoint(multipoint);

			Pnt2D testPnt = new Pnt2D(2734500, 1200123);

			watch.Restart();
			bool anyHit = multipnt
			              .FindPointIndexes(testPnt, 0.01, useSearchCircle: false,
			                                allowIndexing: true)
			              .Any();

			Assert.IsFalse(anyHit);

			watch.Stop();

			Console.WriteLine("First HitTest (Geom): {0}", watch.ElapsedMilliseconds);

			watch.Restart();
			for (int i = 1000; i < 10000; i += 100)
			{
				Commons.Geom.IPnt pnt = multipnt.GetPoint(i);

				anyHit = multipnt
				         .FindPointIndexes(pnt, 0.01, useSearchCircle: false,
				                           allowIndexing: true)
				         .Any();

				Assert.IsTrue(anyHit);
			}

			watch.Stop();
			Console.WriteLine("Next 100 HitTests (Geom): {0}", watch.ElapsedMilliseconds);

			multipnt.SpatialIndex = null;
			watch.Restart();
			for (int i = 1000; i < 10000; i += 100)
			{
				Commons.Geom.IPnt pnt = multipnt.GetPoint(i);

				anyHit = multipnt
				         .FindPointIndexes(pnt, 0.01, useSearchCircle: false,
				                           allowIndexing: false)
				         .Any();

				Assert.IsTrue(anyHit);
			}

			watch.Stop();
			Console.WriteLine("Next 100 HitTests (Geom, without spatial index): {0}",
			                  watch.ElapsedMilliseconds);

			// Results:
			// First HitTest(AO): 10
			// Next 100 HitTests(AO): 704
			// First HitTest(Geom): 42
			// Next 100 HitTests(Geom): 2
			// Next 100 HitTests(Geom, without spatial index): 21
		}

		private static void TestExtendedLine(IPolyline polyline,
		                                     IPolyline targetPolyline,
		                                     LineEnd atLineEnd,
		                                     double deltaLengthExpected)
		{
			IPolyline result = GeometryFactory.CreateEmptyPolyline(polyline);

			bool extended = GeometryUtils.TryGetExtendedPolyline(
				polyline, targetPolyline, atLineEnd,
				result);

			if (! double.IsNaN(deltaLengthExpected))
			{
				Assert.True(extended);
			}
			else
			{
				Assert.False(extended);
				return;
			}

			Assert.AreEqual(polyline.Length + deltaLengthExpected, result.Length,
			                0.00001);

			IPolyline result2 = GeometryFactory.CreateEmptyPolyline(polyline);

			Assert.AreEqual(extended, GeometryUtils.TryGetExtendedPolyline(
				                polyline, GeometryUtils.GetPaths(targetPolyline).First(),
				                atLineEnd, result2));

			Assert.True(GeometryUtils.AreEqual(result, result2));
		}

		[Test]
		public void LearningTestTinDomain()
		{
			ITin tin = new TinClass();
			var tinEdit = (ITinEdit) tin;
			IEnvelope extent = GeometryFactory.CreateEnvelope(0, 0,
			                                                  100, 100,
			                                                  -100, 1000);
			// extent.SpatialReference = SpatialReferenceUtils.;
			tinEdit.InitNew(extent);

			tinEdit.AddPointZ(Pt(5, 0, 100), 1);
			tinEdit.AddPointZ(Pt(10, 0, 100), 1);
			tinEdit.AddPointZ(Pt(10, 10, 200), 1);
			tinEdit.AddPointZ(Pt(5, 10, 100), 1);
			tinEdit.AddPointZ(Pt(0, 0, 100), 1);

			tinEdit.AddPointZ(Pt(5, 1, 100), 1);

			tinEdit.Refresh();

			IPolygon domain = ((ISurface) tin).Domain;

			var domainRelOp = (IRelationalOperator) domain;

			IPoint pointInside = GeometryFactory.CreatePoint(4, 2, 100);
			IPoint pointOutside = GeometryFactory.CreatePoint(0, 10, 100);

			Assert.IsTrue(domainRelOp.Contains(pointInside));
			Assert.IsFalse(domainRelOp.Contains(pointOutside));
		}

		[Test]
		public void LearningTestIsSimplePerformance()
		{
			Console.WriteLine(@"Loading huge Lockergestein polygon...");
			string filePath = TestData.GetHugeLockergesteinPolygonPath();

			var polygon = (IPolygon) ReadGeometryFromXML(filePath);

			var watch = new Stopwatch();
			watch.Start();

			bool simple = GeometryUtils.IsGeometrySimple(
				polygon, polygon.SpatialReference,
				true, out string _, out GeometryNonSimpleReason? _);

			Assert.True(simple);

			watch.Stop();

			Console.WriteLine(
				@"Checked GeometryUtils.IsGeometrySimple on huge polygon in {0}ms",
				watch.ElapsedMilliseconds);

			Console.WriteLine(@"Re-loading huge Lockergestein polygon...");
			polygon = (IPolygon) ReadGeometryFromXML(filePath);

			watch.Reset();
			watch.Start();

			if (polygon is ITopologicalOperator3 topoOp)
			{
				topoOp.IsKnownSimple_2 = false;
				simple = topoOp.get_IsSimpleEx(out esriNonSimpleReasonEnum _);
				Assert.True(simple);
			}

			watch.Stop();

			Console.WriteLine(
				@"Checked ITopologicalOperator.get_IsSimpleEx on huge polygon in {0}ms",
				watch.ElapsedMilliseconds);

			Console.WriteLine(@"Re-loading huge Lockergestein polygon...");
			polygon = (IPolygon) ReadGeometryFromXML(filePath);

			watch.Reset();
			watch.Start();

			GeometryUtils.Simplify(polygon, true, true);

			watch.Stop();
			Console.WriteLine(@"Simplified huge polygon in {0}ms",
			                  watch.ElapsedMilliseconds);
		}

		[Test]
		public void LearningTestContainsClosedPolyline()
		{
			// The contained line is covered by the closed polyline despite the fact that
			// it traverses the From/To point of the closed polyline.
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline closedPolyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(10, 10),
				GeometryFactory.CreatePoint(0, 10),
				GeometryFactory.CreatePoint(0, 0));

			IPolyline coveredLine = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 10),
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0));

			// Somewhat surprising but consistent with TopologicalOperator.Intersect of two congruent closed polylines: 
			// see IntersectionUtils.GetIntersectionLines() (the from/to point is not where it is expected)
			Assert.True(GeometryUtils.Contains(closedPolyline, coveredLine));
		}

		private static ISpatialReference CreateSpatialReference(double xyTolerance,
		                                                        double zTolerance)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// Need to initialize resolution in XY and Z:
			((ISpatialReferenceResolution) sr).ConstructFromHorizon();
			((ISpatialReferenceResolution) sr).SetDefaultZResolution();

			// Only now can set tolerance values:
			((ISpatialReferenceTolerance) sr).XYTolerance = xyTolerance;
			((ISpatialReferenceTolerance) sr).ZTolerance = zTolerance;

			return sr;
		}

		private IPolyline CanAssignZToPolyline(IPolyline polyline, bool useDefaultZ,
		                                       double defaultZ,
		                                       bool drape, int expectedPointCount)
		{
			return CanAssignZToPolyline(polyline, useDefaultZ, defaultZ,
			                            drape, expectedPointCount, -1);
		}

		private IPolyline CanAssignZToPolyline(IPolyline polyline, bool useDefaultZ,
		                                       double defaultZ,
		                                       bool drape, int expectedPointCount,
		                                       int stepSize)
		{
			ISurface surface = CreateMiniSurface(_spatialReference);

			IPolyline result = GeometryUtils.AssignZ(polyline, surface,
			                                         useDefaultZ, defaultZ, drape,
			                                         stepSize);

			Console.WriteLine(GeometryUtils.ToString(result));

			Assert.AreEqual(1, GeometryUtils.GetPartCount(result));
			Assert.AreEqual(expectedPointCount,
			                ((IPointCollection) result).PointCount);

			return result;
		}

		private void CanAssignZToPolygon(IPolygon polygon, bool useDefaultZ,
		                                 double defaultZ,
		                                 bool drape, int expectedPointCount)
		{
			ISurface surface = CreateMiniSurface(_spatialReference);

			IPolygon result = GeometryUtils.AssignZ(polygon, surface,
			                                        useDefaultZ, defaultZ, drape);

			Console.WriteLine(GeometryUtils.ToString(result));

			Assert.AreEqual(1, GeometryUtils.GetPartCount(result));
			Assert.AreEqual(expectedPointCount,
			                ((IPointCollection) result).PointCount);
			Assert.IsTrue(result.IsClosed);
		}

		private static ISurface CreateMiniSurface(ISpatialReference sref)
		{
			ITin tin = new TinClass();
			var tinEdit = (ITinEdit) tin;
			IEnvelope extent = GeometryFactory.CreateEnvelope(1999990, 1999990,
			                                                  1000110, 1000110,
			                                                  -100, 5000);
			extent.SpatialReference = sref;
			tinEdit.InitNew(extent);

			tinEdit.AddPointZ(Pt(2000000, 1000000, 100), 1);
			tinEdit.AddPointZ(Pt(2000000, 1000010, 100), 1);
			tinEdit.AddPointZ(Pt(2000005, 1000005, 200), 1);
			tinEdit.AddPointZ(Pt(2000010, 1000010, 100), 1);
			tinEdit.AddPointZ(Pt(2000010, 1000000, 100), 1);
			tinEdit.Refresh();

			return (ISurface) tin;
		}

		private static IPoint Pt(double x, double y, double z)
		{
			return GeometryFactory.CreatePoint(x, y, z);
		}

		private static bool TimedCongruenceTest(IPolycurve poly1, IPolycurve poly2,
		                                        double toleranceFactor,
		                                        out long elapsedMilliseconds)
		{
			var watch = new Stopwatch();

			watch.Start();
			bool congruent = GeometryUtils.AreCongruentWithinTolerance(
				poly1, poly2, toleranceFactor);
			watch.Stop();

			elapsedMilliseconds = watch.ElapsedMilliseconds;
			Console.WriteLine(@"Congruent? {0} [{1} ms]", congruent,
			                  watch.ElapsedMilliseconds);

			return congruent;
		}

		private static void TimedCongruenceTestOld(IPolycurve poly1, IPolycurve poly2)
		{
			var watch = new Stopwatch();

			watch.Start();
			const double WeedToleranceFactor = 1;
			bool congruent = GeometryUtils.AreCongruentWithinToleranceOld(
				poly1, poly2, WeedToleranceFactor);
			watch.Stop();

			Console.WriteLine(@"Old method result: {0} [{1} ms]", congruent,
			                  watch.ElapsedMilliseconds);
		}

		private static IPolygon RotateRings(IPolygon polygon, int steps)
		{
			IPolygon result = new PolygonClass();
			result.SpatialReference = polygon.SpatialReference;

			var resultParts = (IGeometryCollection) result;

			if (GeometryUtils.IsZAware(polygon))
			{
				GeometryUtils.MakeZAware(result);
			}

			if (GeometryUtils.IsMAware(polygon))
			{
				GeometryUtils.MakeMAware(result);
			}

			var parts = (IGeometryCollection) polygon;
			for (var partNum = 0; partNum < parts.GeometryCount; partNum++)
			{
				IGeometry part = parts.get_Geometry(partNum);
				WKSPointZ[] pointZs = GeometryUtils.GetWKSPointZs(part);

				const int pointIndex = 0;
				int pointCount = pointZs.Length - 1;
				CollectionUtils.Rotate(pointZs, steps, pointIndex, pointCount);
				pointZs[pointZs.Length - 1] = pointZs[0]; // set last = first

				IRing newRing = new RingClass();
				var ringPoints = (IPointCollection4) newRing;

				GeometryUtils.SetWKSPointZs(ringPoints, pointZs);

				object missing = Type.Missing;
				resultParts.AddGeometry(newRing, ref missing, ref missing);
			}

			resultParts.GeometriesChanged();
			result.SimplifyPreserveFromTo();

			return result;
		}

		private static IPolygon InterpolateVertices(IPolygon polygon)
		{
			IPolygon result = new PolygonClass();
			result.SpatialReference = polygon.SpatialReference;

			var resultParts = (IGeometryCollection) result;

			if (GeometryUtils.IsZAware(polygon))
			{
				GeometryUtils.MakeZAware(result);
			}

			if (GeometryUtils.IsMAware(polygon))
			{
				GeometryUtils.MakeMAware(result);
			}

			var parts = (IGeometryCollection) polygon;
			for (var partNum = 0; partNum < parts.GeometryCount; partNum++)
			{
				IRing newRing = new RingClass();
				var ringPoints = (IPointCollection4) newRing;

				IGeometry part = parts.get_Geometry(partNum);
				WKSPointZ[] pointZs = GeometryUtils.GetWKSPointZs(part);

				if (pointZs.Length > 3)
				{
					//if (partNum == 19)
					//{
					//    Console.WriteLine("Aaachtung!");

					//    IPolygon poly = GeometryFactory.Clone(polygon);
					//    IGeometryCollection coll = (IGeometryCollection) poly;
					//    coll.RemoveGeometries(0, parts.GeometryCount);
					//    object Missing = Type.Missing;
					//    coll.AddGeometry(part, ref Missing, ref Missing);
					//    coll.GeometriesChanged();
					//    poly.SimplifyPreserveFromTo();

					//    string filePath = @"C:\Documents and Settings\ujr\Desktop\ring.xml";
					//    WriteGeometryToXML(polygon, filePath);
					//}

					WKSPointZ a = pointZs[pointZs.Length - 2];
					WKSPointZ b = pointZs[pointZs.Length - 1];

					var newPoints = new WKSPointZ[pointZs.Length + 1];
					pointZs.CopyTo(newPoints, 1);

					WKSPointZ p;
					p.X = (a.X + b.X) / 2;
					p.Y = (a.Y + b.Y) / 2;
					p.Z = (a.Z + b.Z) / 2;

					newPoints[0] = p;
					newPoints[newPoints.Length - 1] = p;

					GeometryUtils.SetWKSPointZs(ringPoints, newPoints);
				}
				else
				{
					Console.WriteLine(@"InterpolateVertices: found <4pt ring");
					GeometryUtils.SetWKSPointZs(ringPoints, pointZs);
				}

				if (! newRing.IsClosed)
				{
					newRing.Close();
				}

				object missing = Type.Missing;
				resultParts.AddGeometry(newRing, ref missing, ref missing);
			}

			resultParts.GeometriesChanged();
			result.SimplifyPreserveFromTo();

			return result;
		}

		public static IPolygon CreatePunchedSquarePolygon(
			string name, int pointsPerPart, int numberOfHoles, int rotateSteps)
		{
			const double LowerLeftX = 2600000;
			const double LowerLeftY = 1200000;
			const double OuterSideLength = 9000;
			const double ConstantZ = 99;

			ISpatialReference sr = CreateSpatialReference(0.1, 0.1);

			IPolygon polygon = CreatePunchedSquarePolygon(
				LowerLeftX, LowerLeftY, OuterSideLength,
				numberOfHoles, pointsPerPart, ConstantZ, rotateSteps, sr);

			Console.WriteLine(@"{0}: {1}", name, GetVertexString(polygon, 0, 0));

			return polygon;
		}

		public static IPolygon CreatePunchedSquarePolygon(
			double xOffset, double yOffset, double sideLength,
			int holes, int pointsPerPart, double z, int rotateSteps,
			ISpatialReference sr)
		{
			var holesInXAndY = (int) Math.Round(Math.Sqrt(holes));
			var pointsPerSide = (int) Math.Round(pointsPerPart / 4.0);

			IPolygon polygon = new PolygonClass();
			polygon.SpatialReference = sr;
			GeometryUtils.MakeZAware(polygon);

			var geoColl = (IGeometryCollection) polygon;

			object missing = Type.Missing;

			// Create and add the outer ring
			WKSPointZ[] pointArray = CreateSquare(xOffset, yOffset, sideLength,
			                                      pointsPerSide,
			                                      true,
			                                      z);
			CollectionUtils.Rotate(pointArray, rotateSteps);

			IRing ring = new RingClass();

			GeometryUtils.SetWKSPointZs((IPointCollection4) ring, pointArray);

			ring.Close();

			geoColl.AddGeometry(ring, ref missing, ref missing);

			// Create and add inner rings (= holes)
			double innerSideLength = sideLength / (1 + 2 * holesInXAndY);
			for (var i = 0; i < holesInXAndY; i++)
			{
				double llx = xOffset + (innerSideLength + 2 * i * innerSideLength);

				for (var j = 0; j < holesInXAndY; j++)
				{
					double lly = yOffset + (innerSideLength + 2 * j * innerSideLength);

					pointArray =
						CreateSquare(llx, lly, innerSideLength, pointsPerSide, false, z);
					CollectionUtils.Rotate(pointArray, rotateSteps);

					ring = new RingClass();
					GeometryUtils.SetWKSPointZs(ring, pointArray);
					ring.Close();

					geoColl.AddGeometry(ring, ref missing, ref missing);
				}
			}

			geoColl.GeometriesChanged();
			polygon.SimplifyPreserveFromTo();

			// We closed each ring: poly must be closed too!
			Assert.IsTrue(polygon.IsClosed);

			return polygon;
		}

		private static WKSPointZ[] CreateSquare(
			double lowerLeftX, double lowerLeftY, double sideLength,
			int pointsPerSide, bool clockWise, double z)
		{
			pointsPerSide = Math.Max(1, pointsPerSide);
			var points = new WKSPointZ[4 * pointsPerSide];

			double dist = sideLength / pointsPerSide;
			for (var i = 0; i < pointsPerSide; i++)
			{
				//if (clockWise)
				//{
				points[i].X = lowerLeftX;
				points[i].Y = lowerLeftY + i * dist;
				points[i].Z = z;

				points[i + pointsPerSide].X = lowerLeftX + i * dist;
				points[i + pointsPerSide].Y = lowerLeftY + sideLength;
				points[i + pointsPerSide].Z = z;

				points[i + 2 * pointsPerSide].X = lowerLeftX + sideLength;
				points[i + 2 * pointsPerSide].Y = lowerLeftY + sideLength - i * dist;
				points[i + 2 * pointsPerSide].Z = z;

				points[i + 3 * pointsPerSide].X = lowerLeftX + sideLength - i * dist;
				points[i + 3 * pointsPerSide].Y = lowerLeftY;
				points[i + 3 * pointsPerSide].Z = z;
				//}
				//else
				//{
				//    points[i].X = lowerLeftX + i * dist;
				//    points[i].Y = lowerLeftY;
				//    points[i].Z = z;

				//    points[i + pointsPerSide].X = lowerLeftX + sideLength;
				//    points[i + pointsPerSide].Y = lowerLeftY + i * dist;
				//    points[i + pointsPerSide].Z = z;

				//    points[i + 2 * pointsPerSide].X = lowerLeftX + sideLength - i * dist;
				//    points[i + 2 * pointsPerSide].Y = lowerLeftY + sideLength;
				//    points[i + 2 * pointsPerSide].Z = z;

				//    points[i + 3 * pointsPerSide].X = lowerLeftX;
				//    points[i + 3 * pointsPerSide].Y = lowerLeftY + sideLength - i * dist;
				//    points[i + 3 * pointsPerSide].Z = z;
				//}
			}

			if (! clockWise)
			{
				Array.Reverse(points);
			}

			return points;
		}

		private static object ReadGeometryFromXML(string filePath)
		{
			IXMLSerializer serializer = new XMLSerializerClass();

			IXMLReader reader = new XMLReaderClass();

			IXMLStream stream = new XMLStreamClass();

			stream.LoadFromFile(filePath);

			reader.ReadFrom((IStream) stream);

			return serializer.ReadObject(reader, null, null);
		}

		//private static void WriteGeometryToXML(IGeometry geometry, string filePath)
		//{
		//    Console.WriteLine("Storing geometry to: {0}", filePath);

		//    IXMLStream stream = new XMLStreamClass();
		//    IXMLWriter writer = new XMLWriterClass();
		//    writer.WriteTo((IStream)stream);

		//    IXMLSerializer serializer = new XMLSerializerClass();

		//    serializer.WriteObject(writer, null, null, "", "", geometry);

		//    stream.SaveToFile(filePath);
		//}

		private static string GetVertexString(IGeometry geometry)
		{
			return GetVertexString(geometry, 3, 5);
		}

		private static string GetVertexString(IGeometry geometry, int numPartsToShow,
		                                      int numPointsToShow)
		{
			var sb = new StringBuilder();

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryNull:
					return "(null)";

				case esriGeometryType.esriGeometryPoint:
					return GetVertexString((IPoint) geometry);

				case esriGeometryType.esriGeometryPolyline:
					sb.AppendFormat("(line parts={0} points={1}",
					                GeometryUtils.GetPartCount(geometry),
					                GeometryUtils.GetPointCount(geometry));
					break;

				case esriGeometryType.esriGeometryPolygon:
					sb.AppendFormat("(polygon parts={0} points={1}",
					                GeometryUtils.GetPartCount(geometry),
					                GeometryUtils.GetPointCount(geometry));
					break;

				case esriGeometryType.esriGeometryEnvelope:
					return GetVertexString((IEnvelope) geometry);

				default:
					throw new ArgumentException("geometry type not supported");
			}

			var parts = geometry as IGeometryCollection;
			if (parts != null)
			{
				int partCount = parts.GeometryCount;
				int numParts = Math.Min(partCount, numPartsToShow);
				for (var i = 0; i < numParts; i++)
				{
					sb.AppendFormat("{0}  (part{1}", Environment.NewLine, i);

					IGeometry part = parts.get_Geometry(i);
					AppendPoints(sb, part, numPointsToShow);

					sb.Append(")"); // close part
				}

				if (partCount > numParts && numPartsToShow > 0)
				{
					sb.AppendFormat("{0}  ...", Environment.NewLine);
				}
			}
			else
			{
				AppendPoints(sb, geometry, numPointsToShow);
			}

			sb.Append(")"); // close geometry

			return sb.ToString();
		}

		private static void AppendPoints(StringBuilder sb, IGeometry part,
		                                 int numPointsToShow)
		{
			bool isZAware = GeometryUtils.IsZAware(part);
			bool isMAware = GeometryUtils.IsMAware(part);

			var points = (IPointCollection) part;
			int pointCount = points.PointCount;
			int numPoints = Math.Min(pointCount, numPointsToShow - 1);
			for (var j = 0; j < numPoints; j++)
			{
				IPoint point = points.get_Point(j);
				AppendPoint(sb, point, isZAware, isMAware);
			}

			if (pointCount > numPoints && numPointsToShow > 0)
			{
				if (pointCount > numPoints + 1)
				{
					sb.Append(" ...");
				}

				IPoint point = points.get_Point(points.PointCount - 1);
				AppendPoint(sb, point, isZAware, isMAware);
			}
		}

		private static void AppendPoint(StringBuilder sb, IPoint point, bool isZAware,
		                                bool isMAware)
		{
			sb.AppendFormat(" ({0:F2} {1:F2}", point.X, point.Y);

			if (isZAware)
			{
				sb.AppendFormat(" {0:F2}", point.Z);
			}

			if (isMAware)
			{
				sb.AppendFormat(" {0:G}", point.M);
			}

			sb.Append(")");
		}

		private static string GetVertexString(IEnvelope envelope)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("(envelope (x {0} .. {1}) (y {2} .. {3})",
			                envelope.XMin, envelope.XMax, envelope.YMin, envelope.YMax);

			if (GeometryUtils.IsZAware(envelope))
			{
				sb.AppendFormat(" (z {0} .. {1})", envelope.ZMin, envelope.ZMax);
			}

			if (GeometryUtils.IsMAware(envelope))
			{
				sb.AppendFormat(" (m {0} .. {1})", envelope.MMin, envelope.MMax);
			}

			sb.Append(")");

			return sb.ToString();
		}

		private static string GetVertexString(IPoint point)
		{
			bool isZAware = GeometryUtils.IsZAware(point);
			bool isMAware = GeometryUtils.IsMAware(point);

			var sb = new StringBuilder("(point");
			AppendPoint(sb, point, isZAware, isMAware);
			sb.Append("))");

			return sb.ToString();
		}

		private static byte[] AssertWkbConversionAO(IGeometry geometry, bool onlyIn2D = false)
		{
			// ArcObjects
			byte[] wkbAO = GeometryUtils.ToWkb(geometry);

			IGeometry restoredGeometry = GeometryUtils.FromWkb(wkbAO);

			if (onlyIn2D)
			{
				Assert.True(GeometryUtils.AreEqualInXY(geometry, restoredGeometry));
			}
			else
			{
				Assert.True(GeometryUtils.AreEqual(geometry, restoredGeometry));

				Assert.AreEqual(GeometryUtils.IsZAware(geometry),
				                GeometryUtils.IsZAware(restoredGeometry));
				Assert.AreEqual(GeometryUtils.IsMAware(geometry),
				                GeometryUtils.IsMAware(restoredGeometry));
			}

			return wkbAO;
		}

		private static void AssertExactEsriShapeBufferConversion(IGeometry geometry)
		{
			byte[] bytes = GeometryUtils.ToEsriShapeBuffer(geometry);

			IGeometry restoredGeometry = GeometryUtils.FromEsriShapeBuffer(bytes);

			Assert.True(GeometryUtils.AreEqual(geometry, restoredGeometry));

			Assert.AreEqual(GeometryUtils.IsZAware(geometry),
			                GeometryUtils.IsZAware(restoredGeometry));
			Assert.AreEqual(GeometryUtils.IsMAware(geometry),
			                GeometryUtils.IsMAware(restoredGeometry));
		}
	}
}

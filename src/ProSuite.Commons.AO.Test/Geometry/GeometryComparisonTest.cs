using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class GeometryComparisonTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		#region Setup/Teardown

		[SetUp]
		public void SetUp() { }

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		#endregion

		[Test]
		public void CanGetChangedVerticesNoChanges()
		{
			const int fewPointsPerPart = 400;
			const int holes = 3;

			IPolygon poly1a = GeometryUtilsTest.CreatePunchedSquarePolygon("1a",
				fewPointsPerPart,
				holes, 1);
			IPolygon poly1b = GeometryUtilsTest.CreatePunchedSquarePolygon("1b",
				fewPointsPerPart,
				holes, -1);

			var vertexComparer = new GeometryComparison(poly1a, poly1b, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = false;
			IList<WKSPointZ> changes =
				vertexComparer.GetDifferentVertices(symmetric, false);

			Assert.AreEqual(0, changes.Count);

			//bool reportDuplicateVertices = true;
			changes = vertexComparer.GetDifferentVertices(symmetric, true);

			int ringCount = ((IGeometryCollection) poly1a).GeometryCount +
			                ((IGeometryCollection) poly1b).GeometryCount;

			Assert.AreEqual(ringCount, changes.Count);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetChangedVerticesNonZaware()
		{
			string path = TestData.GetIntersectingLineNonZawarePath();

			IGeometry baseGeometry = GeometryUtils.FromXmlFile(path);
			IGeometry simpleGeometry = GeometryFactory.Clone(baseGeometry);

			GeometryUtils.Simplify(simpleGeometry, true, true);

			double xyTolerance = GeometryUtils.GetXyResolution(baseGeometry);
			const double zTolerance = double.NaN;

			var vertexComparer = new GeometryComparison(baseGeometry, simpleGeometry,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> changes = vertexComparer.GetDifferentVertices(true, false);

			// NOTE: the feature gets split into 2 parts which is, however not detected
			//		 due to the duplicate vertices being ignored.
			Assert.AreEqual(0, changes.Count);

			var basePoints = (IPointCollection) baseGeometry;

			// simulate the following behavior: when a FGDB-Feature is fiddled around with
			// it is returned with Z=0 (probably because it got the map's spatial ref)
			for (int i = 0; i < basePoints.PointCount; i++)
			{
				IPoint point = basePoints.get_Point(i);
				point.Z = 0;
				basePoints.UpdatePoint(i, point);
			}

			changes = vertexComparer.GetDifferentVertices(true, false);

			Assert.AreEqual(0, changes.Count);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetChangedVerticesInLargeGeometry()
		{
			const int manyPointsPerPart = 123456;
			const int holes = 3;

			IPolygon poly2a = GeometryUtilsTest.CreatePunchedSquarePolygon("2a",
				manyPointsPerPart,
				holes, 1);
			IPolygon poly2b = GeometryUtilsTest.CreatePunchedSquarePolygon("2b",
				manyPointsPerPart,
				holes, -1);

			var watch = new Stopwatch();
			watch.Start();

			var vertexComparer = new GeometryComparison(poly2a, poly2b, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = false;
			IList<WKSPointZ> changes =
				vertexComparer.GetDifferentVertices(symmetric, false);

			watch.Stop();

			Assert.AreEqual(0, changes.Count);
			Assert.Less(watch.ElapsedMilliseconds, 1000.0);
			Console.WriteLine(@"Calculate changed vertices (no changes) in {0} ms",
			                  watch.ElapsedMilliseconds);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetChangedVerticesInLargeGeometryAllDifferent()
		{
			const int manyPointsPerPart = 123456;
			const int holes = 3;

			IPolygon poly2a = GeometryUtilsTest.CreatePunchedSquarePolygon("2a",
				manyPointsPerPart,
				holes, 1);
			IPolygon poly2b = GeometryUtilsTest.CreatePunchedSquarePolygon("2b",
				manyPointsPerPart,
				holes, -1);

			GeometryUtils.MoveGeometry(poly2b, 7, 6);

			int totalPointCount = ((IPointCollection) poly2a).PointCount +
			                      ((IPointCollection) poly2b).PointCount;

			var watch = new Stopwatch();
			watch.Start();

			var vertexComparer = new GeometryComparison(poly2a, poly2b, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes =
				vertexComparer.GetDifferentVertices(symmetric, true);

			watch.Stop();

			Assert.AreEqual(totalPointCount, changes.Count);
			Assert.Less(watch.ElapsedMilliseconds, 1000.0);
			Console.WriteLine(@"Calculate changed vertexes (all different) in {0} ms",
			                  watch.ElapsedMilliseconds);

			//bool reportDuplicateVertices = false;
			changes = vertexComparer.GetDifferentVertices(symmetric, false);

			int ringCount = ((IGeometryCollection) poly2a).GeometryCount +
			                ((IGeometryCollection) poly2b).GeometryCount;

			Assert.AreEqual(totalPointCount - ringCount, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesAllDifferent()
		{
			const int fewPointsPerPart = 400;

			IPolygon poly3a = GeometryUtilsTest.CreatePunchedSquarePolygon("3a",
				fewPointsPerPart,
				0,
				1);
			IPolygon poly3b = GeometryUtilsTest.CreatePunchedSquarePolygon("3b",
				fewPointsPerPart *
				2,
				0, 2);

			GeometryUtils.MoveGeometry(poly3b, 7.77, 6.66);

			var vertexComparer = new GeometryComparison(poly3a, poly3b, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes = vertexComparer.GetDifferentVertices(symmetric,
				true);

			int totalPointCount = ((IPointCollection) poly3a).PointCount +
			                      ((IPointCollection) poly3b).PointCount;

			Assert.AreEqual(totalPointCount, changes.Count);

			//bool reportDuplicateVertices = false;
			changes = vertexComparer.GetDifferentVertices(symmetric, false);
			Assert.AreEqual(totalPointCount - 2, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesAllDifferentFlipped()
		{
			const int fewPointsPerPart = 400;

			IPolygon poly3a = GeometryUtilsTest.CreatePunchedSquarePolygon("3a",
				fewPointsPerPart,
				0,
				1);
			IPolygon poly3b = GeometryUtilsTest.CreatePunchedSquarePolygon("3b",
				fewPointsPerPart *
				2,
				0, 2);

			GeometryUtils.MoveGeometry(poly3b, 7.77, 6.66);

			var vertexComparer = new GeometryComparison(poly3b, poly3a, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes = vertexComparer.GetDifferentVertices(symmetric,
				true);

			int totalPointCount = ((IPointCollection) poly3a).PointCount +
			                      ((IPointCollection) poly3b).PointCount;

			Assert.AreEqual(totalPointCount, changes.Count);

			//bool reportDuplicateVertices = false;
			changes = vertexComparer.GetDifferentVertices(symmetric, false);
			Assert.AreEqual(totalPointCount - 2, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesAllVeryDisjoint()
		{
			const int fewPointsPerPart = 400;

			IPolygon poly3a = GeometryUtilsTest.CreatePunchedSquarePolygon("3a",
				fewPointsPerPart,
				0,
				1);
			IPolygon poly3b = GeometryUtilsTest.CreatePunchedSquarePolygon("3b",
				fewPointsPerPart *
				2,
				0, 2);

			GeometryUtils.MoveGeometry(poly3b, 700000, 600000);

			var vertexComparer = new GeometryComparison(poly3a, poly3b, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes = vertexComparer.GetDifferentVertices(symmetric,
				true);

			int totalPointCount = ((IPointCollection) poly3a).PointCount +
			                      ((IPointCollection) poly3b).PointCount;

			Assert.AreEqual(totalPointCount, changes.Count);

			// reportDuplicateVertices = false;
			changes = vertexComparer.GetDifferentVertices(symmetric,
			                                              false);

			// Each geometries' start/end point is duplicate and will not be reported
			Assert.AreEqual(totalPointCount - 2, changes.Count);

			//const bool symmetric = false;
			changes = vertexComparer.GetDifferentVertices(false, true);

			Assert.AreEqual(((IPointCollection) poly3a).PointCount, changes.Count);

			// reportDuplicateVertices = false;
			changes = vertexComparer.GetDifferentVertices(false, false);

			// The geometry's start/end point is duplicate and will not be reported
			Assert.AreEqual(((IPointCollection) poly3a).PointCount - 1, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesAllVeryDisjointFlipped()
		{
			const int fewPointsPerPart = 400;

			IPolygon poly3a = GeometryUtilsTest.CreatePunchedSquarePolygon("3a",
				fewPointsPerPart,
				0,
				1);
			IPolygon poly3b = GeometryUtilsTest.CreatePunchedSquarePolygon("3b",
				fewPointsPerPart *
				2,
				0, 2);

			GeometryUtils.MoveGeometry(poly3b, 700000, 600000);

			var vertexComparer = new GeometryComparison(poly3b, poly3a, 0.001, 0.01);

			const bool symmetric = true;
			//bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes = vertexComparer.GetDifferentVertices(symmetric,
				true);

			int totalPointCount = ((IPointCollection) poly3a).PointCount +
			                      ((IPointCollection) poly3b).PointCount;

			Assert.AreEqual(totalPointCount, changes.Count);

			// reportDuplicateVertices = false;
			changes = vertexComparer.GetDifferentVertices(symmetric,
			                                              false);

			// Each geometry's start/end point is duplicate and will not be reported
			Assert.AreEqual(totalPointCount - 2, changes.Count);
		}

		[Test]
		public void CanGetChangedDuplicateVerticesPolygonWithSameStartPoint()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(30, 10));

			IPolygon sourcePolygon = GeometryFactory.CreatePolygon(polyline);
			IPolygon simplifiedPolygon = GeometryFactory.Clone(sourcePolygon);

			const bool allowNonPlanarLines = false;
			bool isSimple = GeometryUtils.IsGeometrySimple(sourcePolygon,
			                                               lv95,
			                                               allowNonPlanarLines,
			                                               out _, out _);
			Assert.IsFalse(isSimple);

			// do not allow reorder so we can check that parallel duplicates are not
			GeometryUtils.Simplify(simplifiedPolygon, false, ! allowNonPlanarLines);

			var geometryComparison = new GeometryComparison(
				sourcePolygon, simplifiedPolygon, 0.00125, 0.0125);

			const bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes = geometryComparison.GetDifferentVertices(false,
				reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);
			Assert.True(geometryComparison.HaveSameVertices());
			Assert.False(geometryComparison.HaveSameVertices(false));

			changes =
				geometryComparison.GetDifferentVertices(true, reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(true, false);
			Assert.AreEqual(0, changes.Count);

			geometryComparison = new GeometryComparison(
				simplifiedPolygon, sourcePolygon, 0.00125, 0.0125);

			changes =
				geometryComparison.GetDifferentVertices(false, reportDuplicateVertices);
			Assert.AreEqual(0, changes.Count);

			changes =
				geometryComparison.GetDifferentVertices(true, reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(true, false);
			Assert.AreEqual(0, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesPolylineWithDifferencesAtLastCoord()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline referencePolyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(30, 10));

			IPolyline comparePolyline = GeometryFactory.Clone(referencePolyline);

			var geometryComparison = new GeometryComparison(
				referencePolyline, comparePolyline, 0.0125, 0.0125);

			// now make the difference really big
			IPoint point = ((IPointCollection) referencePolyline).get_Point(2);
			point.X = 50.0;
			((IPointCollection) referencePolyline).UpdatePoint(2, point);

			const bool reportDuplicateVertices = true;
			const bool ignoreDuplicateVertices = false;

			const bool symmetric = true;
			const bool nonSymmetric = false;

			IList<WKSPointZ> changes = geometryComparison.GetDifferentVertices(symmetric,
				reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(symmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  reportDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			// Now swap the geometries
			geometryComparison = new GeometryComparison(
				comparePolyline, referencePolyline, 0.0125, 0.0125);

			changes = geometryComparison.GetDifferentVertices(symmetric,
			                                                  reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(symmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  reportDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(0, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesPolylineWithDuplicateDifferencesAtLastCoord()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline referencePolyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(30, 10));

			IPolyline comparePolyline = GeometryFactory.Clone(referencePolyline);

			var geometryComparison = new GeometryComparison(
				referencePolyline, comparePolyline, 0.0125, 0.0125);

			// now make the difference really big
			IPoint point = ((IPointCollection) referencePolyline).get_Point(2);
			point.X = 50.0;
			((IPointCollection) referencePolyline).UpdatePoint(2, point);

			// And an additional point with a duplicate coordinate:
			((IPointCollection) referencePolyline).AddPoint(point);

			const bool reportDuplicateVertices = true;
			const bool ignoreDuplicateVertices = false;

			const bool symmetric = true;
			const bool nonSymmetric = false;

			IList<WKSPointZ> changes = geometryComparison.GetDifferentVertices(symmetric,
				reportDuplicateVertices);
			Assert.AreEqual(3, changes.Count);

			Assert.False(geometryComparison.HaveSameVertices());
			Assert.False(geometryComparison.HaveSameVertices(false));

			changes = geometryComparison.GetDifferentVertices(symmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			// Now swap the geometries
			geometryComparison = new GeometryComparison(
				comparePolyline, referencePolyline, 0.0125, 0.0125);

			changes = geometryComparison.GetDifferentVertices(symmetric,
			                                                  reportDuplicateVertices);
			Assert.AreEqual(3, changes.Count);
			Assert.False(geometryComparison.HaveSameVertices());
			Assert.False(geometryComparison.HaveSameVertices(false));

			changes = geometryComparison.GetDifferentVertices(symmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  reportDuplicateVertices);
			Assert.AreEqual(1, changes.Count);

			changes = geometryComparison.GetDifferentVertices(nonSymmetric,
			                                                  ignoreDuplicateVertices);
			Assert.AreEqual(0, changes.Count);
		}

		[Test]
		public void CanGetChangedVerticesSimplifiedZigZag()
		{
			// the source polyline visits the same points several times by going back and forth
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IPolyline sourcePolyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(30, 10));

			IPolyline simplifiedPolyline = GeometryFactory.Clone(sourcePolyline);

			const bool allowNonPlanarLines = false;
			bool isSimple = GeometryUtils.IsGeometrySimple(sourcePolyline,
			                                               lv95,
			                                               allowNonPlanarLines,
			                                               out _, out _);
			Assert.IsFalse(isSimple);

			GeometryUtils.Simplify(simplifiedPolyline, true, ! allowNonPlanarLines);

			var geometryComparison = new GeometryComparison(
				sourcePolyline, simplifiedPolyline, 0.00125, 0.0125);

			const bool reportDuplicateVertices = true;
			IList<WKSPointZ> changes = geometryComparison.GetDifferentVertices(false,
				reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes =
				geometryComparison.GetDifferentVertices(true, reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(true, false);
			Assert.AreEqual(0, changes.Count);

			geometryComparison = new GeometryComparison(
				simplifiedPolyline, sourcePolyline, 0.00125, 0.0125);

			changes =
				geometryComparison.GetDifferentVertices(false, reportDuplicateVertices);
			Assert.AreEqual(0, changes.Count);

			changes =
				geometryComparison.GetDifferentVertices(true, reportDuplicateVertices);
			Assert.AreEqual(2, changes.Count);

			changes = geometryComparison.GetDifferentVertices(true, false);
			Assert.AreEqual(0, changes.Count);
		}

		[Test]
		public void CanGetNoDuplicatesMultipointPoints()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000),
				GeometryFactory.CreatePoint(200, 1000, 5000),
				GeometryFactory.CreatePoint(300, 1000, 5000),
				GeometryFactory.CreatePoint(100, 2000, 5000),
				GeometryFactory.CreatePoint(100, 3000, 5000)
			);

			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(0, result.Count);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);

			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(0, result.Count);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.True(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetOneDuplicateMultipointPoints()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(200, 1000, 5000),
				GeometryFactory.CreatePoint(300, 1000, 5000),
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(100, 2000, 5000),
				GeometryFactory.CreatePoint(100, 3000, 5000)
			);

			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);

			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetTwoDuplicateMultipointPoints()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			// the second duplicate IS the last one in the sorted coordinate list...
			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate 1
				GeometryFactory.CreatePoint(200, 1000, 5000),
				GeometryFactory.CreatePoint(300, 1000, 5000), // duplicate 2
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate 1
				GeometryFactory.CreatePoint(100, 2000, 5000),
				GeometryFactory.CreatePoint(100, 3000, 5000),
				GeometryFactory.CreatePoint(300, 1000, 5000) // duplicate 2
			);

			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(300, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);

			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(300, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetTwoDuplicateMultipointPointsInSequence()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			// the second duplicate is NOT the last one in the sorted coordinate list...
			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate 1
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate 1
				GeometryFactory.CreatePoint(150, 1000, 5000), // duplicate 2
				GeometryFactory.CreatePoint(150, 1000, 5000), // duplicate 2
				GeometryFactory.CreatePoint(200, 1000, 5000),
				GeometryFactory.CreatePoint(100, 2000, 5000),
				GeometryFactory.CreatePoint(100, 3000, 5000)
			);

			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(150, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);

			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(150, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetDuplicatedMultipointPoint()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(100, 1000, 5000) // duplicate
			);

			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);

			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetTriplicatedMultipointPoint()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(200, 1000, 5000),
				GeometryFactory.CreatePoint(300, 1000, 5000),
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(100, 2000, 5000),
				GeometryFactory.CreatePoint(100, 3000, 5000),
				GeometryFactory.CreatePoint(100, 1000, 5000) // duplicate
			);
			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(100, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);
			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(100, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetTriplicatedMultipointPointInSequence()
		{
			const double xyTolerance = 0.1;
			const double zTolerance = 0.1;

			IMultipoint original = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(100, 1000, 5000), // duplicate
				GeometryFactory.CreatePoint(200, 1000, 5000),
				GeometryFactory.CreatePoint(300, 1000, 5000),
				GeometryFactory.CreatePoint(100, 2000, 5000),
				GeometryFactory.CreatePoint(100, 3000, 5000)
			);

			original.SpatialReference = CreateSpatialReference(xyTolerance, zTolerance);

			IMultipoint clone = GeometryFactory.Clone(original);
			GeometryUtils.Simplify(clone);

			var vertexComparer = new GeometryComparison(original, clone,
			                                            xyTolerance, zTolerance);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(100, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			// Flip geometries:
			vertexComparer = new GeometryComparison(clone, original,
			                                        xyTolerance, zTolerance);
			result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(100, result[0].X);
			Assert.AreEqual(1000, result[0].Y);
			Assert.AreEqual(5000, result[0].Z);
			Assert.AreEqual(100, result[1].X);
			Assert.AreEqual(1000, result[1].Y);
			Assert.AreEqual(5000, result[1].Z);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetNoChangedVerticesSingleSegmentLine()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(400, 1000, 400, 2000);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(400, 1000, 400, 2000);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			var vertexComparer = new GeometryComparison(
				sourcePolyline, targetPolyline, 0.001,
				0.01);

			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(0, result.Count);

			Assert.True(vertexComparer.HaveSameVertices());
			Assert.True(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetChangedVerticesSingleSegmentLine()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(450, 10, 50, 100, 80, 50);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 20, 100, 0, 20);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			var vertexComparer = new GeometryComparison(
				sourcePolyline, targetPolyline, 0.001,
				0.01);
			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(4, result.Count);

			Assert.False(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
		}

		[Test]
		public void CanGetZChangedVerticesSingleSegmentLine()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 20, 100, 0, 20);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			var vertexComparer = new GeometryComparison(
				sourcePolyline, targetPolyline, 0.001,
				0.01);
			IList<WKSPointZ> result = vertexComparer.GetDifferentVertices(true);

			Assert.AreEqual(4, result.Count);

			Assert.False(vertexComparer.HaveSameVertices());
			Assert.False(vertexComparer.HaveSameVertices(false));
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
	}
}

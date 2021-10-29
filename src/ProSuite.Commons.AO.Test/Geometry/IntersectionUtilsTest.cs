using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ExtractParts;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class IntersectionUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

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

		[Test]
		public void CanGetDifferenceWithWorkaroundForSlivers_WithLargeDifference()
		{
			string coveredXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveredpoly_largediff.xml");
			string coveringXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveringpoly.xml");

			var difference = (IPolygon4) IntersectionUtils.Difference(
				GeometryUtils.FromXmlFile(coveringXml),
				GeometryUtils.FromXmlFile(coveredXml));

			Assert.IsTrue(difference.IsEmpty || ((IArea) difference).Area > 0);
			Assert.AreEqual(1, ((IGeometryCollection) difference).GeometryCount);
		}

		[Test]
		public void CanGetDifferenceWithWorkaroundForSlivers()
		{
			string coveredXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveredpoly.xml");
			string coveringXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveringpoly.xml");

			var difference = (IPolygon4) IntersectionUtils.Difference(
				GeometryUtils.FromXmlFile(coveringXml),
				GeometryUtils.FromXmlFile(coveredXml));

			Assert.IsTrue(difference.IsEmpty || ((IArea) difference).Area > 0);
			Assert.AreEqual(0, ((IGeometryCollection) difference).GeometryCount);
		}

		[Test]
		public void CanWorkAroundClusterToleranceException()
		{
			ISpatialReference sr = CreateSpatialReference(0.001, 0.0001);

			// ReSharper disable JoinDeclarationAndInitializer
			IPolyline p1;
			IPolyline p2;
			// ReSharper restore JoinDeclarationAndInitializer

			// regular case (length much longer than xy tolerance)
			// second polyline has an y offset of <= the xy tolerance
			p1 = CreatePolyline(sr, x: 0, y: 0, length: 10);
			p2 = CreatePolyline(sr, x: 1, y: 0.001, length: 10);

			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);

			// length is equal to 5x the tolerance (or greater)
			// second polyline has an y offset of <= the xy tolerance
			// --> NO exception
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.005);
			p2 = CreatePolyline(sr, x: 1, y: 1.001, length: 0.005);

			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);

			// length is less than 5x the tolerance
			// second polyline has an y offset of <= the xy tolerance
			// --> EXCEPTION
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.004);
			p2 = CreatePolyline(sr, x: 1, y: 1.001, length: 0.004);

			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);

			// length is less than 5x the tolerance
			// second polyline has an y offset of > the xy tolerance
			// --> NO exception for difference, but EXCEPTION for linear and point intersection
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.004);
			p2 = CreatePolyline(sr, x: 1, y: 1.0011, length: 0.004);

			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);

			// length: 10x the xy tolerance
			// second polyline has a y offset of <= the xy tolerance
			// --> NO exception
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.005);
			p2 = CreatePolyline(sr, x: 1, y: 1.0011, length: 0.005);
			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);

			// length: < 5x xy tolerance
			// second polyline has y offset of <= 2x the xy tolerance
			// --> EXCEPTION
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.004);
			p2 = CreatePolyline(sr, x: 1, y: 1.002, length: 0.004);
			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);

			// length: < 5x xy tolerance
			// second polyline has y offset of > 2x the xy tolerance
			// --> NO exception
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.004);
			p2 = CreatePolyline(sr, x: 1, y: 1.0021, length: 0.004);
			IntersectionUtils.Difference(p1, p2);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry0Dimension);
			IntersectionUtils.Intersect(
				p1, p2, esriGeometryDimension.esriGeometry1Dimension);
		}

		[Test]
		public void CanIntersectEmptyPoints()
		{
			IPoint g1 = new PointClass();
			IPoint g2 = new PointClass();

			IGeometry intersection = IntersectionUtils.GetIntersection(g1, g2);

			Assert.IsTrue(intersection.IsEmpty);
		}

		[Test]
		public void CanIntersectNonEmptyWithEmptyPoint()
		{
			IPoint g1 = GeometryFactory.CreatePoint(10, 20);
			IPoint g2 = new PointClass();

			IGeometry intersection = IntersectionUtils.GetIntersection(g1, g2);

			Assert.IsTrue(intersection.IsEmpty);
		}

		[Test]
		public void CanGetPointPointIntersections()
		{
			IPoint g1 = GeometryFactory.CreatePoint(10, 20);
			IPoint g2 = GeometryFactory.CreatePoint(10, 20);

			IList<IGeometry> result = IntersectionUtils.GetAllIntersectionList(g1, g2);

			Assert.AreEqual(1, result.Count);
			var intersection = (IPoint) result[0];
			Assert.AreEqual(10, intersection.X);
			Assert.AreEqual(20, intersection.Y);
		}

		[Test]
		public void CanGetPolygonPointIntersections()
		{
			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 30);
			IPoint g2 = GeometryFactory.CreatePoint(10, 20);

			IList<IGeometry> result = IntersectionUtils.GetAllIntersectionList(g1, g2);

			Assert.AreEqual(1, result.Count);
			var intersection = (IPoint) result[0];
			Assert.AreEqual(10, intersection.X);
			Assert.AreEqual(20, intersection.Y);
		}

		[Test]
		public void CanGetPolygonPolylineIntersections()
		{
			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 30);
			IPolyline g2 = GeometryFactory.CreatePolyline(5, 5, 20, 5);

			IList<IGeometry> result = IntersectionUtils.GetAllIntersectionList(g1, g2);
			const double e = 0.0001;

			Assert.AreEqual(2, result.Count);

			Console.WriteLine(result[0].GeometryType); // Multipoint
			Console.WriteLine(result[1].GeometryType); // Polyline

			var pointIntersection = (IMultipoint) result[0];
			var point =
				(IPoint) ((IGeometryCollection) pointIntersection).get_Geometry(0);

			Assert.AreEqual(5, point.Y, e);
			Assert.AreEqual(10, point.X, e);

			var lineIntersection = (IPolyline) result[1];
			Assert.AreEqual(5, lineIntersection.FromPoint.X, e);
			Assert.AreEqual(10, lineIntersection.ToPoint.X, e);
		}

		[Test]
		public void CanUse9IMMatrixWithMaxDimension()
		{
			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			IPolygon g2 = GeometryFactory.CreatePolygon(10, 0, 20, 10);

			GeometryUtils.AllowIndexing(g1);
			GeometryUtils.AllowIndexing(g2);

			var relOp = (IRelationalOperator) g1;
			Assert.IsTrue(relOp.Relation(g2, GetMatrixExpression("****T****")), "T");
			Assert.IsTrue(relOp.Relation(g2, GetMatrixExpression("****1****")), "dim(1)");

			Assert.IsFalse(relOp.Relation(g2, GetMatrixExpression("****0****")),
			               "dim(0)");
			Assert.IsFalse(relOp.Relation(g2, GetMatrixExpression("****2****")),
			               "dim(2)");
		}

		[Test]
		public void CanIgnorePointIntersectionForLinearSelfIntersection()
		{
			var polyline = new PolylineClass();

			AddPoint(polyline, 100, 100);
			AddPoint(polyline, 200, 100);
			AddPoint(polyline, 200, 200);
			AddPoint(polyline, 100, 200);
			AddPoint(polyline, 100, 100);

			GeometryUtils.Simplify(polyline);

			IPolyline intersection =
				IntersectionUtils.GetLinearSelfIntersection(polyline);

			Assert.IsNull(intersection);
		}

		[Test]
		public void CanGetLinearSelfIntersectionAtEndpoint()
		{
			var polyline = new PolylineClass();

			AddPoint(polyline, 100, 100);
			AddPoint(polyline, 200, 100);
			AddPoint(polyline, 200, 200);
			AddPoint(polyline, 100, 200);
			AddPoint(polyline, 100, 100);
			AddPoint(polyline, 101, 100); // linear self int, 1m

			GeometryUtils.Simplify(polyline);

			IPolyline intersection =
				IntersectionUtils.GetLinearSelfIntersection(polyline);

			Console.WriteLine(GeometryUtils.ToString(intersection));

			Assert.IsNotNull(intersection);
			Assert.AreEqual(1, intersection.Length);
		}

		[Test]
		public void CanGetLinearSelfIntersectionForCoincidentSegments()
		{
			var polyline = new PolylineClass();

			AddPoint(polyline, 100, 100);
			AddPoint(polyline, 200, 100);
			AddPoint(polyline, 100, 100);

			GeometryUtils.Simplify(polyline);

			IPolyline intersection =
				IntersectionUtils.GetLinearSelfIntersection(polyline);

			Console.WriteLine(GeometryUtils.ToString(intersection));

			Assert.IsNotNull(intersection);
			Assert.AreEqual(100, intersection.Length);
		}

		[Test]
		public void CanGetLinearSelfIntersectionForMultipart()
		{
			var part1 = new PolylineClass();

			AddPoint(part1, 100, 100);
			AddPoint(part1, 200, 100);
			AddPoint(part1, 200, 150);
			GeometryUtils.Simplify(part1);

			var part2 = new PolylineClass();

			AddPoint(part2, 150, 200);
			AddPoint(part2, 100, 200);
			AddPoint(part2, 100, 101); // No self intersection
			GeometryUtils.Simplify(part2);

			var multipart = (IPolyline) ((ITopologicalOperator) part1).Union(part2);

			Console.WriteLine(GeometryUtils.ToString(multipart));

			IPolyline intersection =
				IntersectionUtils.GetLinearSelfIntersection(multipart);

			Assert.IsNull(intersection);
		}

		[Test]
		public void
			CanGetIntersectionPointsForMultipartLinesAlmostTouchingPolyBoundaryFromInside()
		{
			// such situations were problematic in the past
			IPolygon polygon = new PolygonClass();
			polygon.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			AddPoint(polygon, 100, 100);
			AddPoint(polygon, 200, 100);
			AddPoint(polygon, 200, 200);
			AddPoint(polygon, 100, 200);
			AddPoint(polygon, 100, 100);
			GeometryUtils.Simplify(polygon);

			var polyline = new PolylineClass();
			polyline.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			double resolution = GeometryUtils.GetXyResolution(polyline);
			AddPoint(polyline, 150, 200 - 2 * resolution);
			AddPoint(polyline, 150, 150);
			AddPoint(polyline, 150, 50);
			GeometryUtils.Simplify(polyline);

			var result =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(
					polyline, GeometryFactory.CreatePolyline(polygon), true,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			Assert.AreEqual(2, result.PointCount);

			// now try multi-part line
			var polylinePart2 = new PolylineClass();
			polylinePart2.SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			AddPoint(polylinePart2, 160, 150);
			AddPoint(polylinePart2, 250, 150);
			AddPoint(polylinePart2, 250, 350);
			GeometryUtils.Simplify(polylinePart2);

			object missing = Type.Missing;
			((IGeometryCollection) polyline).AddGeometry(
				((IGeometryCollection) polylinePart2).get_Geometry(0), ref missing,
				ref missing);

			result =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(
					polyline, GeometryFactory.CreatePolyline(polygon), true,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			Assert.AreEqual(3, result.PointCount);
		}

		[Test]
		public void CanCalculateSymmetricIntersectionLinesForTouchingClosedPaths()
		{
			var polyWithHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml"));

			var polyFillingHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonFillingHole.xml"));

			IPolycurve polyWithHoleAsLine = GeometryFactory.CreatePolyline(polyWithHole);
			IPolycurve polyFillingHoleAsLine =
				GeometryFactory.CreatePolyline(polyFillingHole);

			// intersection point test:
			IPolyline intersection1 = IntersectionUtils.GetIntersectionLines(
				polyWithHoleAsLine, polyFillingHoleAsLine, true, false);

			IPolyline intersection2 = IntersectionUtils.GetIntersectionLines(
				polyFillingHoleAsLine, polyWithHoleAsLine, true, false);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(intersection1, intersection2));
		}

		[Test]
		public void CanCalculateSymmetricIntersectionPointsForTouchingClosedPaths()
		{
			var polyWithHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml"));

			var polyFillingHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonFillingHole.xml"));

			IPolycurve polyWithHoleAsLine = GeometryFactory.CreatePolyline(polyWithHole);
			IPolycurve polyFillingHoleAsLine =
				GeometryFactory.CreatePolyline(polyFillingHole);

			// intersection point test:
			IMultipoint intersection1 = IntersectionUtils.GetIntersectionPoints(
				polyWithHoleAsLine, polyFillingHoleAsLine, true,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			IMultipoint intersection2 = IntersectionUtils.GetIntersectionPoints(
				polyFillingHoleAsLine, polyWithHoleAsLine, true,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			{
				Assert.IsTrue(GeometryUtils.AreEqualInXY(intersection1, intersection2));
			}
		}

		[Test]
		public void CanCalculateSymmetricIntersectionPointsForTouchingRings()
		{
			var polyWithHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml"));

			var polyFillingHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonFillingHole.xml"));

			// intersection point test:
			var intersection1 =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					polyWithHole, polyFillingHole, false,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			var intersection2 =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					polyFillingHole, polyWithHole, false,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			{
				Assert.IsTrue(GeometryUtils.AreEqualInXY((IGeometry) intersection1,
				                                         (IGeometry) intersection2));
			}
		}

		[Test]
		public void CanCalculateSymmetricIntersectionPointsForIdenticalRings()
		{
			var polyWithHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml"));

			// intersection point test:
			IPolygon polyWithHoleClone = GeometryFactory.Clone(polyWithHole);
			var intersection1 =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					polyWithHole, polyWithHoleClone, false,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			var intersection2 =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					polyWithHoleClone, polyWithHole, false,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			Assert.IsTrue(GeometryUtils.AreEqualInXY((IGeometry) intersection1,
			                                         (IGeometry) intersection2));
		}

		[Test]
		public void CalculateIntersectionPointsPerformance()
		{
			var polyWithHole =
				(IPolygon)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml"));

			IPolygon polyWithHoleClone = GeometryFactory.Clone(polyWithHole);

			Console.WriteLine(@"Intersection geometry point count: {0}",
			                  ((IPointCollection) polyWithHole).PointCount);

			Stopwatch watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;

			var intersection1 =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					polyWithHole, polyWithHoleClone, false,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			watch.Stop();

			Console.WriteLine(@"TopologicalOperator.Intersect: {0}",
			                  watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();
			IntersectionUtils.UseCustomIntersect = true;
			var intersection2 =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					polyWithHole, polyWithHoleClone, false,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			watch.Stop();

			Console.WriteLine(
				"GeomUtils.GetIntersectionPointsXY: {0}",
				watch.ElapsedMilliseconds);

			// The AO-implementation reports the correct start point + a random intersection
			// (despite the option to fix random linear intersection start points in rings!)
			//Assert.AreEqual(intersection1.PointCount, intersection2.PointCount);
			//Assert.True(GeometryUtils.AreEqual((IGeometry) intersection1,
			//                                   (IGeometry) intersection2));
			Assert.True(
				GeometryUtils.Contains((IGeometry) intersection1, (IGeometry) intersection2));

			polyWithHole = (IPolygon) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml"));

			GeometryUtils.MoveGeometry(polyWithHoleClone, 1.8, 0.9);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;
			intersection1 = (IPointCollection) IntersectionUtils.GetIntersectionPoints(
				polyWithHoleClone, polyWithHole, false,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			watch.Stop();

			Console.WriteLine();
			Console.WriteLine("Fewer intersections:");
			Console.WriteLine("TopologicalOperator.Intersect: {0}",
			                  watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			intersection2 = (IPointCollection) IntersectionUtils.GetIntersectionPoints(
				polyWithHoleClone, polyWithHole, false,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			watch.Stop();

			Console.WriteLine("GeomUtils.GetIntersectionPointsXY: {0}",
			                  watch.ElapsedMilliseconds);

			var mp1 = GeometryConversionUtils.CreateMultipoint((IMultipoint) intersection1);
			var mp2 = GeometryConversionUtils.CreateMultipoint((IMultipoint) intersection2);

			Assert.True(GeomRelationUtils.AreEqual(mp1, mp2, 0.00001));

			// Otherwise the point order is not the same and IRelationalOp.AreEqual is false
			// (because multipoints use IClone equals)
			GeometryUtils.Simplify((IGeometry) intersection2);
			Assert.True(GeometryUtils.AreEqual((IGeometry) intersection1,
			                                   (IGeometry) intersection2));

			// Huge Lockergestein
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			var bigPoly = (IPolygon) TestUtils.ReadGeometryFromXml(filePath);
			IPolygon bigPoly2 = GeometryFactory.Clone(bigPoly);

			GeometryUtils.MoveGeometry(bigPoly2, 2.5, 6.3);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;
			intersection1 = (IPointCollection) IntersectionUtils.GetIntersectionPoints(
				bigPoly, bigPoly2, false,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			watch.Stop();

			Console.WriteLine();
			Console.WriteLine(
				"Huge Lockergestein point count: {0} (intersected with moved clone)",
				((IPointCollection) bigPoly).PointCount);
			Console.WriteLine("TopologicalOperator.Intersect ({0} intersections): {1}",
			                  intersection1.PointCount, watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			intersection2 = (IPointCollection) IntersectionUtils.GetIntersectionPoints(
				bigPoly, bigPoly2, false,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			watch.Stop();

			Console.WriteLine(
				"GeomUtils.GetIntersectionPointsXY ({0} intersections): {1}",
				intersection2.PointCount, watch.ElapsedMilliseconds);

			//Output
			//Intersection geometry point count: 132
			//TopologicalOperator.Intersect: 91
			//GeomUtils.GetIntersectionPointsXY: 60

			//Fewer intersections:
			//TopologicalOperator.Intersect: 5
			//GeomUtils.GetIntersectionPointsXY: 23

			//Huge Lockergestein point count: 92436 (intersected with moved clone)
			//TopologicalOperator.Intersect (29599 intersections): 4407
			//GeomUtils.GetIntersectionPointsXY (29593 intersections): 1486

			// The actual GeomUtils-intersection is ~800ms, same as a single ITopologicalOp call
			// Creating the spatial index is ~100ms
		}

		[Test]
		public void CalculateIntersectionLinesPerformance()
		{
			IPolyline polyWithHole =
				GeometryFactory.CreatePolyline(
					TestUtils.ReadGeometryFromXml(
						TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml")));

			IPolyline polyWithHoleClone = GeometryFactory.Clone(polyWithHole);

			Console.WriteLine(@"Intersection geometry point count: {0}",
			                  ((IPointCollection) polyWithHole).PointCount);

			Stopwatch watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;

			IPolyline intersection1 = IntersectionUtils.GetIntersectionLines(
				polyWithHole, polyWithHoleClone, true, true);

			watch.Stop();

			Console.WriteLine(@"TopologicalOperator.Intersect: {0}",
			                  watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();
			IntersectionUtils.UseCustomIntersect = true;

			IPolyline intersection2 = IntersectionUtils.GetIntersectionLines(
				polyWithHole, polyWithHoleClone, true, true);

			watch.Stop();

			Console.WriteLine("Custom intersect: {0}",
			                  watch.ElapsedMilliseconds);

			Assert.True(GeometryUtils.AreEqualInXY(intersection1, intersection2));

			Console.WriteLine();
			Console.WriteLine("No linear intersections:");

			polyWithHole = GeometryFactory.CreatePolyline(
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml")));

			GeometryUtils.MoveGeometry(polyWithHoleClone, 1.8, 0.9);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;
			intersection1 = IntersectionUtils.GetIntersectionLines(
				polyWithHole, polyWithHoleClone, true, true);

			watch.Stop();

			Console.WriteLine("TopologicalOperator.Intersect: {0}",
			                  watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			intersection2 = IntersectionUtils.GetIntersectionLines(
				polyWithHole, polyWithHoleClone, true, true);

			watch.Stop();

			Console.WriteLine("Custom intersect: {0}",
			                  watch.ElapsedMilliseconds);

			Assert.True(GeometryUtils.AreEqual(intersection1, intersection2));

			// Huge Lockergestein
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			IPolyline bigPoly = GeometryFactory.CreatePolyline(
				TestUtils.ReadGeometryFromXml(filePath));
			IPolyline bigPoly2 = GeometryFactory.Clone(bigPoly);

			//GeometryUtils.MoveGeometry(bigPoly2, 2.5, 6.3);

			Console.WriteLine();
			Console.WriteLine(
				"Huge Lockergestein point count: {0} (intersected with clone)",
				((IPointCollection) bigPoly).PointCount);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;
			intersection1 = IntersectionUtils.GetIntersectionLines(
				bigPoly, bigPoly2, true, true);

			watch.Stop();

			Console.WriteLine(
				"TopologicalOperator.Intersect ({0}m intersections length): {1}",
				intersection1.Length, watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			intersection2 = IntersectionUtils.GetIntersectionLines(
				bigPoly, bigPoly2, true, true);

			watch.Stop();

			Console.WriteLine(
				"GeomUtils.GetIntersectionLinesXY ({0}m intersections length): {1}",
				intersection2.Length, watch.ElapsedMilliseconds);

			Assert.True(GeometryUtils.AreEqualInXY(intersection1, intersection2));

			//Output
			//Intersection geometry point count: 132
			//TopologicalOperator.Intersect: 48
			//Custom intersect: 45

			//No linear intersections:
			//TopologicalOperator.Intersect: 1
			//Custom intersect: 15

			//Huge Lockergestein point count: 92436 (intersected with clone) 
			//TopologicalOperator.Intersect (1282991.00391786m intersections length): 857
			//GeomUtils.GetIntersectionLinesXY (1282991.00391787m intersections length): 1214

			// The actual GeomUtils-intersection is ~800ms, same as a single ITopologicalOp call
		}

		[Test]
		public void CalculateDifferenceLinesPerformance()
		{
			IPolyline polyWithHole =
				GeometryFactory.CreatePolyline(
					TestUtils.ReadGeometryFromXml(
						TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml")));

			IPolyline polyWithHoleClone = GeometryFactory.Clone(polyWithHole);

			double xyTolerance = GeometryUtils.GetXyTolerance(polyWithHole);

			Console.WriteLine(@"Target geometry point count: {0}",
			                  ((IPointCollection) polyWithHole).PointCount);

			Stopwatch watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;

			IPolyline difference1 = (IPolyline) IntersectionUtils.Difference(
				polyWithHole, polyWithHoleClone);

			watch.Stop();

			Console.WriteLine(@"TopologicalOperator.Difference: {0}",
			                  watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();
			IntersectionUtils.UseCustomIntersect = true;

			IPolyline difference2 = IntersectionUtils.GetDifferenceLinesXY(
				polyWithHole, polyWithHoleClone, xyTolerance);

			watch.Stop();

			Console.WriteLine("Custom difference: {0}",
			                  watch.ElapsedMilliseconds);

			// NOTE: GeometryUtils.AreEqualInXY(geometry1, geometry2) is false for 2 emptry geometries!
			// Assert.True(GeometryUtils.AreEqualInXY(difference1, difference2));
			Assert.True(difference1.IsEmpty && difference2.IsEmpty);

			Console.WriteLine();
			Console.WriteLine("No linear intersections:");

			polyWithHole = GeometryFactory.CreatePolyline(
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithHole.xml")));

			GeometryUtils.MoveGeometry(polyWithHoleClone, 1.8, 0.9);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;
			difference1 =
				(IPolyline) IntersectionUtils.Difference(polyWithHole, polyWithHoleClone);

			watch.Stop();

			Console.WriteLine("TopologicalOperator.Difference: {0}",
			                  watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			difference2 = IntersectionUtils.GetDifferenceLinesXY(
				polyWithHole, polyWithHoleClone, xyTolerance);

			watch.Stop();

			Console.WriteLine("Custom difference: {0}",
			                  watch.ElapsedMilliseconds);

			//Assert.True(GeometryUtils.AreEqual(difference1, difference2));
			IPolyline zDifference = GeometryFactory.CreateEmptyPolyline(difference1);
			IPolyline difference = IntersectionUtils.GetDifferenceLinesXY(
				difference1, difference2, xyTolerance, zDifference, 0.001);

			Assert.True(zDifference.IsEmpty && difference.IsEmpty);

			// Huge Lockergestein
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			IPolyline bigPoly = GeometryFactory.CreatePolyline(
				TestUtils.ReadGeometryFromXml(filePath));
			IPolyline bigPoly2 = GeometryFactory.Clone(bigPoly);

			Console.WriteLine();
			Console.WriteLine(
				"Huge Lockergestein point count: {0} (compared with clone)",
				((IPointCollection) bigPoly).PointCount);

			watch = Stopwatch.StartNew();

			// Interestingly, the spatial index (ISpatialIndex) has no effect.
			// The second difference is ca. 50ms faster regardless of AllowIndexing property.
			IntersectionUtils.UseCustomIntersect = false;
			difference1 = (IPolyline) IntersectionUtils.Difference(bigPoly, bigPoly2);

			watch.Stop();

			Console.WriteLine(
				"TopologicalOperator.Difference ({0}m difference length): {1}",
				difference1.Length, watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			difference2 =
				IntersectionUtils.GetDifferenceLinesXY(bigPoly, bigPoly2, xyTolerance);

			watch.Stop();

			Console.WriteLine(
				"GeomTopoOpUtils.GetDifferenceLinesXY ({0}m difference length): {1}",
				difference2.Length, watch.ElapsedMilliseconds);

			// NOTE: GeometryUtils.AreEqualInXY(geometry1, geometry2) is false for 2 emptry geometries!
			// Assert.True(GeometryUtils.AreEqualInXY(difference1, difference2));
			Assert.True(difference1.IsEmpty && difference2.IsEmpty);

			Console.WriteLine();
			Console.WriteLine(
				"Huge Lockergestein point count: {0} (compared with moved clone)",
				((IPointCollection) bigPoly).PointCount);

			GeometryUtils.MoveGeometry(bigPoly2, 2.5, 6.3);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = false;
			difference1 = (IPolyline) IntersectionUtils.Difference(bigPoly, bigPoly2);

			watch.Stop();

			Console.WriteLine(
				"TopologicalOperator.Difference ({0}m difference length): {1}",
				difference1.Length, watch.ElapsedMilliseconds);

			watch = Stopwatch.StartNew();

			IntersectionUtils.UseCustomIntersect = true;
			difference2 =
				IntersectionUtils.GetDifferenceLinesXY(bigPoly, bigPoly2, 0.01);

			watch.Stop();

			// NOTE: The second operation on the large geometry has an advantage (~250ms) probably because the 
			//       large array used for geometry conversion is already created by WksPointArrayProvider.
			Console.WriteLine(
				"GeomTopoOpUtils.GetDifferenceLinesXY ({0}m difference length): {1}",
				difference2.Length, watch.ElapsedMilliseconds);

			//Output
			//Target geometry point count: 132
			//TopologicalOperator.Difference: 19
			//Custom difference: 84

			//No linear intersections:
			//TopologicalOperator.Difference: 2
			//Custom difference: 18

			//Huge Lockergestein point count: 92436 (compared with clone)
			//TopologicalOperator.Difference (0m difference length): 607
			//GeomTopoOpUtils.GetDifferenceLinesXY (0m difference length): 844

			//Huge Lockergestein point count: 92436 (compared with moved clone)
			//TopologicalOperator.Difference (1281785.80707641m difference length): 1879
			//GeomTopoOpUtils.GetDifferenceLinesXY (1282557.49223316m difference length): 1489
		}

		[Test]
		public void CanGetMultipatchIntersectionPointsXY()
		{
			// {FE286920-3D4C-4CB3-AC22-51056B97A23F} from TLM:
			IMultiPatch multipatch =
				(IMultiPatch) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("MultipatchWithVerticalWalls.xml"));

			ISpatialReference lv95 = multipatch.SpatialReference;

			// NOTE: AO multipatch intersections return non-z-aware points
			//		 -> compare to planar intersection points
			// NOTE: AO multipatch intersection uses the footprint rather than each ring separately
			//		 -> must be accounted for in general method, otherwise extra intersection points
			//          are reported on touching boundaries.

			//
			// Multipatch-Polyline
			//
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2574923.000, 1196869.000, 500, 0.1, lv95),
				GeometryFactory.CreatePoint(2574920.000, 1196878.000, 500, 0.3, lv95),
				GeometryFactory.CreatePoint(2574910.000, 1196870.000, 500, 0.3, lv95));

			double tolerance = GeometryUtils.GetXyTolerance(multipatch);

			IntersectionUtils.UseCustomIntersect = false;
			IMultipoint intersectionPointsMultipatchLineAo =
				IntersectionUtils.GetIntersectionPoints(multipatch, cutLine);

			// Expected: 2 points at the outer boundary
			Assert.AreEqual(2, GeometryUtils.GetPointCount(intersectionPointsMultipatchLineAo));

			// Compare with 
			IntersectionUtils.UseCustomIntersect = true;
			IMultipoint intersectionPointsMultipatchLineCustom =
				IntersectionUtils.GetIntersectionPoints(multipatch, cutLine);

			// Expected: 2 points at the outer boundary
			Assert.AreEqual(
				2, GeometryUtils.GetPointCount(intersectionPointsMultipatchLineCustom));

			// NOTE from the documentation: "For multipoints, IRelationalOperator::Equals delegates to IClone::IsEqual"
			// Therefore GeometryUtils.AreEqualInXY is false!
			Assert.IsTrue(
				GeomRelationUtils.AreEqualXY
				(GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchLineAo),
				 GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchLineCustom),
				 tolerance));

			// Extra functionality in GetIntersectionPointsXY: PLANAR
			IMultipoint intersectionPointsMultipatchPolylinePlanar =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, cutLine, tolerance, true);

			// Has extra point because all rings are intersected with all rings:
			Assert.AreEqual(
				3, GeometryUtils.GetPointCount(intersectionPointsMultipatchPolylinePlanar));
			Assert.IsTrue(GeometryUtils.Contains(intersectionPointsMultipatchPolylinePlanar,
			                                     intersectionPointsMultipatchLineAo));

			// Extra functionality in GetIntersectionPointsXY: NON-PLANAR (reporting multiple intersections on different Zs)
			IMultipoint intersectionPointsMultipatchPolylineNonPlanar =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, cutLine, tolerance, false);

			// Has extra point because at the touching boundaries both rings report an intersection:
			Assert.AreEqual(
				4, GeometryUtils.GetPointCount(intersectionPointsMultipatchPolylineNonPlanar));

			Assert.IsTrue(GeometryUtils.Contains(intersectionPointsMultipatchPolylineNonPlanar,
			                                     intersectionPointsMultipatchLineAo));

			//
			// Multipatch-Multipoint
			IMultipoint targetMultipoint = intersectionPointsMultipatchLineAo;
			IntersectionUtils.UseCustomIntersect = false;
			IMultipoint intersectionPointsMultipatchMultipointAo =
				IntersectionUtils.GetIntersectionPoints(multipatch, targetMultipoint);

			IntersectionUtils.UseCustomIntersect = true;
			IMultipoint intersectionPointsMultipatchMultipointXY =
				IntersectionUtils.GetIntersectionPoints(multipatch, targetMultipoint);

			// AO output is non-Z-aware. Use Geom-Equals because IRelationalOperator::Equals delegates to IClone
			Assert.IsTrue(
				GeomRelationUtils.AreEqualXY
				(GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchMultipointAo),
				 GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchMultipointXY),
				 tolerance));

			Assert.AreEqual(GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipointAo),
			                GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipointXY));

			// GetIntersectionPointsXY (PLANAR: same as AO but with Z values):
			intersectionPointsMultipatchMultipointXY =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, targetMultipoint, tolerance,
				                                          true);

			Assert.AreEqual(
				2, GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipointXY));

			// The targetMultipatch is not Z-aware
			Assert.IsTrue(
				GeomRelationUtils.AreEqualXY(
					GeometryConversionUtils.CreateMultipoint(targetMultipoint),
					GeometryConversionUtils.CreateMultipoint(
						intersectionPointsMultipatchMultipointXY),
					tolerance));

			GeometryUtils.MakeNonZAware(intersectionPointsMultipatchMultipointXY);

			Assert.IsTrue(
				GeomRelationUtils.AreEqual
				(GeometryConversionUtils.CreateMultipoint(targetMultipoint),
				 GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchMultipointXY),
				 tolerance, tolerance));

			// GetIntersectionPointsXY (NON-PLANAR: has extra point at differenz Z):
			intersectionPointsMultipatchMultipointXY =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, targetMultipoint, tolerance,
				                                          false);

			Assert.AreEqual(
				3, GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipointXY));

			//
			// Multipatch-Point
			IPoint pointTarget =
				((IPointCollection) intersectionPointsMultipatchMultipointXY).get_Point(0);

			IntersectionUtils.UseCustomIntersect = false;
			IMultipoint intersectionPointsMultipatchPointAo =
				IntersectionUtils.GetIntersectionPoints(multipatch, pointTarget);

			IntersectionUtils.UseCustomIntersect = true;
			IMultipoint intersectionPointsMultipatchPointXY =
				IntersectionUtils.GetIntersectionPoints(multipatch, pointTarget);

			GeometryUtils.AreEqual(intersectionPointsMultipatchPointAo,
			                       intersectionPointsMultipatchPointXY);

			intersectionPointsMultipatchPointXY =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, pointTarget, tolerance);

			GeometryUtils.AreEqualInXY(pointTarget, intersectionPointsMultipatchPointXY);

			//
			// Multipatch-Multipatch
			IMultiPatch multipatchTarget = GeometryFactory.Clone(multipatch);
			GeometryUtils.MoveGeometry(multipatchTarget, 3, 0);

			IntersectionUtils.UseCustomIntersect = false;
			IMultipoint intersectionPointsMultipatchMultipatchAo =
				IntersectionUtils.GetIntersectionPoints(multipatch, multipatchTarget);

			IntersectionUtils.UseCustomIntersect = true;
			IMultipoint intersectionPointsMultipatchMultipatchXY =
				IntersectionUtils.GetIntersectionPoints(multipatch, multipatchTarget);

			// AO output is non-Z-aware. Use Geom-Equals because IRelationalOperator::Equals delegates to IClone
			Assert.IsTrue(
				GeomRelationUtils.AreEqualXY
				(GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchMultipatchAo),
				 GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchMultipatchXY),
				 tolerance));

			Assert.AreEqual(GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipatchAo),
			                GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipatchXY));

			// GetIntersectionPointsXY (PLANAR): Extra points on the touching ring boundaries in the entierior
			intersectionPointsMultipatchMultipatchXY =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, multipatchTarget, tolerance,
				                                          true);

			Assert.AreEqual(
				4, GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipatchXY));

			//
			// Multipatch-Polygon
			IPolygon polygonTarget = GeometryFactory.CreatePolygon(multipatchTarget);
			IntersectionUtils.UseCustomIntersect = false;
			IMultipoint intersectionPointsMultipatchPolygonAo =
				IntersectionUtils.GetIntersectionPoints(multipatch, polygonTarget);

			IntersectionUtils.UseCustomIntersect = true;
			IMultipoint intersectionPointsMultipatchPolygonXY =
				IntersectionUtils.GetIntersectionPoints(multipatch, polygonTarget);

			// AO output is non-Z-aware. Use Geom-Equals because IRelationalOperator::Equals delegates to IClone
			Assert.IsTrue(
				GeomRelationUtils.AreEqualXY
				(GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchPolygonAo),
				 GeometryConversionUtils.CreateMultipoint(intersectionPointsMultipatchPolygonXY),
				 tolerance));

			Assert.AreEqual(GeometryUtils.GetPointCount(intersectionPointsMultipatchPolygonAo),
			                GeometryUtils.GetPointCount(intersectionPointsMultipatchPolygonXY));

			// GetIntersectionPointsXY (PLANAR): Extra points on the touching ring boundaries in the entierior
			intersectionPointsMultipatchMultipatchXY =
				IntersectionUtils.GetIntersectionPointsXY(multipatch, polygonTarget, tolerance,
				                                          true);

			Assert.AreEqual(
				3, GeometryUtils.GetPointCount(intersectionPointsMultipatchMultipatchXY));
		}

		[Test]
		public void IntersectionResultKeepsAwarenessDespiteEmptyOtherGeometry()
		{
			// At 10.4.1 intersect (without work-around) with an empty other geometry results in loss of
			// - M-awareness
			// - PointID-awareness
			// even though the source geometry was aware!
			ISpatialReference sRef =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			// multipoint
			IGeometry sourceMultipoint =
				GeometryFactory.CreateMultipoint(
					GeometryFactory.CreatePoint(2599999, 1299999, 600, 2, sRef));

			Assert.IsTrue(GeometryUtils.IsMAware(sourceMultipoint));

			GeometryUtils.MakePointIDAware(sourceMultipoint);

			// Awareness of the other should make no difference, the result takes awareness from the source
			IGeometry other = GeometryFactory.CreateEmptyMultipoint(sourceMultipoint);

			IGeometry intersection = IntersectionUtils.Intersect(
				sourceMultipoint, other, esriGeometryDimension.esriGeometry0Dimension);

			Assert.IsTrue(GeometryUtils.IsMAware(intersection));
			Assert.IsTrue(GeometryUtils.IsPointIDAware(intersection));

			// The same happens for polylines / polygons:
			WKSEnvelope wksEnvelope = WksGeometryUtils.CreateWksEnvelope(2600000, 1200000,
			                                                             2600100,
			                                                             1200100);

			IPolygon perimeter =
				GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(wksEnvelope),
				                              sRef);

			GeometryUtils.MakeMAware(perimeter);
			GeometryUtils.MakePointIDAware(perimeter);

			other = GeometryFactory.Clone(perimeter);
			other.SetEmpty();

			intersection = IntersectionUtils.Intersect(perimeter, other,
			                                           perimeter.Dimension);

			Assert.AreEqual(GeometryUtils.IsMAware(perimeter),
			                GeometryUtils.IsMAware(intersection));

			Assert.AreEqual(GeometryUtils.IsPointIDAware(perimeter),
			                GeometryUtils.IsPointIDAware(intersection));
		}

		[Test]
		public void DifferenceResultKeepsAwareness()
		{
			// Difference of multipoint source (without work-around) loses
			// - Z-awareness
			// - M-awareness
			// - PointID-awareness
			// even though the source geometry was aware (observed at 10.4.1).
			// Repro case: GeometryIssuesReproTest.Repro_ITopologicalOperatorDifferenceResultLosesAwareness()
			ISpatialReference sRef =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			// polygon
			WKSEnvelope wksEnvelope = WksGeometryUtils.CreateWksEnvelope(2600000, 1200000,
			                                                             2600100,
			                                                             1200100);
			// Z/M/ID Awareness of the other should make no difference, the result takes awareness from the source
			IPolygon perimeter =
				GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(wksEnvelope),
				                              sRef);

			// multipoint (intersecting)
			IGeometry sourceMultipoint = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(2599999, 1299999, 600, 2, sRef),
				GeometryFactory.CreatePoint(2600001, 1200001, 1400, 5, sRef));

			Assert.IsTrue(GeometryUtils.IsMAware(sourceMultipoint));
			Assert.IsTrue(GeometryUtils.IsZAware(sourceMultipoint));
			GeometryUtils.MakePointIDAware(sourceMultipoint);

			IGeometry difference = IntersectionUtils.Difference(
				sourceMultipoint, perimeter);

			Assert.IsTrue(GeometryUtils.IsZAware(difference));
			Assert.IsTrue(GeometryUtils.IsMAware(difference));
			Assert.IsTrue(GeometryUtils.IsPointIDAware(difference));

			// (it all works fine for polylines / polygons)
		}

		[Test]
		public void CanGetVerticalLineNonplanarIntersections()
		{
			ISpatialReference sRef =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			IPath path = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600000, 1200000, 500, double.NaN, sRef),
				GeometryFactory.CreatePoint(2600040, 1200040, 500, double.NaN, sRef),
				GeometryFactory.CreatePoint(2600040, 1200040, 600, double.NaN, sRef),
				GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, sRef),
				GeometryFactory.CreatePoint(2600000, 1200000, 500, double.NaN, sRef));

			IPolyline verticalMultipatchAsPolyline = GeometryFactory.CreatePolyline(path);
			GeometryUtils.MakeZAware(verticalMultipatchAsPolyline);

			IPolyline almostVerticalPolyline = GeometryFactory.CreatePolyline(
				sRef,
				GeometryFactory.CreatePoint(2600020, 1200020.003, 500),
				GeometryFactory.CreatePoint(2600020, 1200020, 600));
			GeometryUtils.MakeZAware(almostVerticalPolyline);

			IPolyline verticalPolyline = GeometryFactory.CreatePolyline(
				sRef,
				GeometryFactory.CreatePoint(2600020, 1200020.00, 500),
				GeometryFactory.CreatePoint(2600020, 1200020, 600));
			GeometryUtils.MakeZAware(verticalPolyline);

			IPoint intersectingPoint = GeometryFactory.CreatePoint(2600020, 1200020, 600);

			Assert.IsTrue(GeometryUtils.Intersects(intersectingPoint,
			                                       verticalMultipatchAsPolyline));

			var intersectionWithPoint =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(verticalMultipatchAsPolyline,
				                                        intersectingPoint);
			Assert.AreEqual(1,
			                intersectionWithPoint.PointCount);

			var intersectionPointsPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPoints(almostVerticalPolyline,
				                                        verticalMultipatchAsPolyline);

			Assert.AreEqual(1, intersectionPointsPlanar.PointCount);

			var intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(almostVerticalPolyline,
				                                                 verticalMultipatchAsPolyline);

			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);

			// with the almost-vertical polyline it all works as expected.
			// Now the completely vertical polyline, it requires work-arounds (because of the issue
			// described in Repro_ITopologicalOperator_Intersect_ReturnsEmptyResultForVerticalPolyline()
			// NOTE: for GeomUtils.GetIntersectionPointsXY this is not the case
			intersectionPointsPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPoints(verticalPolyline,
				                                        verticalMultipatchAsPolyline);

			if (! IntersectionUtils.UseCustomIntersect)
			{
				Assert.AreEqual(1, intersectionPointsPlanar.PointCount);
			}
			else
			{
				Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);
			}

			// The remainder is about Arc-Objects specific work-around:

			// Correct
			intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(verticalPolyline,
				                                                 verticalMultipatchAsPolyline);

			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);

			// Now test a vertical polyline, that is within the XY-tolerance:
			IPolyline verticalPolylineWithinXyTol = GeometryFactory.CreatePolyline(
				sRef,
				GeometryFactory.CreatePoint(2600020.00, 1200020.0019, 500),
				GeometryFactory.CreatePoint(2600020.00, 1200020.0019, 600));
			GeometryUtils.MakeZAware(verticalPolylineWithinXyTol);

			Assert.IsTrue(GeometryUtils.Intersects(verticalPolylineWithinXyTol,
			                                       verticalPolylineWithinXyTol));

			intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(
					verticalPolylineWithinXyTol,
					verticalMultipatchAsPolyline);

			// Misteriously, this all works without work-around. But see next test...
			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);

			IMultipoint verticalPoints =
				GeometryFactory.CreateMultipoint((IPointCollection) verticalPolyline);

			intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(
					(IPointCollection) verticalPoints,
					verticalMultipatchAsPolyline);

			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);
		}

		[Test]
		public void CanGetVerticalLineNonPlanarIntersectionRealData()
		{
			var verticalWallBoundaryPolyline = (IPolyline) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("verticalRingBoundarypolyline.xml"));

			var verticalPolyline = (IPolyline) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("verticalPolyline.xml"));

			Assert.True(GeometryUtils.Intersects(verticalPolyline.FromPoint,
			                                     verticalWallBoundaryPolyline));

			bool intersects3D =
				! ((IRelationalOperator3D) verticalWallBoundaryPolyline).Disjoint3D(
					verticalPolyline.FromPoint);

			Assert.True(intersects3D);

			var intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(
					verticalWallBoundaryPolyline,
					verticalPolyline);

			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);

			// Check multipoint-non-planar intersection
			IMultipoint multipoint =
				GeometryFactory.CreateMultipoint(
					(IPointCollection) verticalWallBoundaryPolyline);

			((IPointCollection) multipoint).AddPoint(verticalPolyline.FromPoint);

			IMultipoint intersectionPointNonPlanar =
				IntersectionUtils.GetIntersectionPointsNonPlanar(
					(IPointCollection) multipoint,
					verticalPolyline.FromPoint);

			// Version with OutOfMemoryException:
			//(IMultipoint)
			//((ITopologicalOperator6) multipoint).IntersectEx(
			//	verticalPolyline, true, esriGeometryDimension.esriGeometry0Dimension);

			Assert.AreEqual(
				1, ((IPointCollection) intersectionPointNonPlanar).PointCount);
		}

		[Test]
		public void CanGetNonZAwareNonPlanarIntersection()
		{
			// Originally this resulted in an endless loop - it's important that the non-planar intersection point count is correct!
			IMultiPatch multipatch = GetSingleRingMultipatch();

			// Non-Z-aware reshape line, cuts the multipatch ring almost along a boundary
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2578314.9090000018, 1183246.2400000021),
				GeometryFactory.CreatePoint(2578307.4299999997, 1183270.4310000017));

			cutLine.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			var unReshaped = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			var highLevelRing =
				(IPolycurve) GeometryUtils.GetHighLevelGeometry(unReshaped);

			// ITopologicalOperator6.IntersectEx with nonPlanar==true works correct if the first geometry
			// is the polygon and the second is the line (even without simplify)
			var intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(highLevelRing, cutLine);
			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);

			// However, ITopologicalOperator6.IntersectEx with nonPlanar==true returns 3 points
			// if the line is the first argument and the polygon the second -> simplify is needed
			// This probably only happens if the intersection point is close to the polygon's start/end points.
			// It happens regardless of the cutLine's Z awareness
			intersectionPointsNonPlanar = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(cutLine, highLevelRing);
			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);
		}

		[Test]
		public void CanGetNonZAwareIntersectionPointsXy()
		{
			IMultiPatch multipatch = GetSingleRingMultipatch();

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2578314.9090000018, 1183246.2400000021),
				GeometryFactory.CreatePoint(2578307.4299999997, 1183270.4310000017));

			cutLine.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// XY intersection points, using the non-Z-aware cut line as the first polycurve 
			double xyTolerance = GeometryUtils.GetXyTolerance(multipatch);

			var unReshaped = (IRing) ((IGeometryCollection) multipatch).Geometry[0];
			var highLevelRing =
				(IPolycurve) GeometryUtils.GetHighLevelGeometry(unReshaped);

			IMultipoint intersectionPointsXy = GetIntersectionPointsXy(
				cutLine, highLevelRing, xyTolerance, false);

			Assert.False(GeometryUtils.IsZAware(intersectionPointsXy));
			Assert.True(GeometryUtils.GetPoints(intersectionPointsXy)
			                         .All(p => double.IsNaN(p.Z)));

			IMultipoint intersectionPointsXyCustom = GetIntersectionPointsXy(
				cutLine, highLevelRing, xyTolerance, true);

			Assert.False(GeometryUtils.IsZAware(intersectionPointsXyCustom));
			Assert.True(
				GeometryUtils.GetPoints(intersectionPointsXyCustom)
				             .All(p => double.IsNaN(p.Z)));

			Assert.True(
				GeometryUtils.AreEqual(intersectionPointsXy, intersectionPointsXyCustom));

			// XY intersection points, using the Z-aware multipatch ring as the first polycurve 
			intersectionPointsXy = GetIntersectionPointsXy(
				highLevelRing, cutLine, xyTolerance, false);

			Assert.True(GeometryUtils.IsZAware(intersectionPointsXy));
			Assert.True(GeometryUtils.GetPoints(intersectionPointsXy)
			                         .All(p => ! double.IsNaN(p.Z)));

			intersectionPointsXyCustom = GetIntersectionPointsXy(
				highLevelRing, cutLine, xyTolerance, true);

			Assert.True(GeometryUtils.IsZAware(intersectionPointsXyCustom));
			Assert.True(
				GeometryUtils.GetPoints(intersectionPointsXyCustom)
				             .All(p => ! double.IsNaN(p.Z)));

			Assert.True(
				GeometryUtils.AreEqual(intersectionPointsXy, intersectionPointsXyCustom));
		}

		[Test]
		public void CanGetMultipatchIntersectionLines()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2600050, 1200020, lv95));

			GeometryUtils.MakeZAware(originalPoly);

			IMultiPatch multiPatch1 =
				GeometryFactory.CreateMultiPatch(
					GeometryUtils.ConstantZ(originalPoly, 500));

			// The same original poly, but lopsided, cutting through the horizontal multipatch

			List<IPoint> points = GeometryUtils.GetPoints(originalPoly).ToList();
			var pointCollection = (IPointCollection) originalPoly;

			points[0].Z = points[4].Z = 480d;
			points[1].Z = 480d;
			points[2].Z = 520d;
			points[3].Z = 520d;

			for (var i = 0; i < pointCollection.PointCount; i++)
			{
				pointCollection.UpdatePoint(i, points[i]);
			}

			IMultiPatch multiPatch2 =
				GeometryFactory.CreateMultiPatch(originalPoly);

			List<CutPolyline> cutPolylines = IntersectionUtils.GetIntersectionLines3D(
				                                                  multiPatch1,
				                                                  multiPatch2)
			                                                  .ToList();

			Assert.AreEqual(1, cutPolylines.Count);

			IPolyline intersectionPolyline = cutPolylines[0].Polyline;

			Assert.AreEqual(20, intersectionPolyline.Length);
			Assert.AreEqual(500, intersectionPolyline.FromPoint.Z);
			Assert.AreEqual(500, intersectionPolyline.ToPoint.Z);
			Assert.AreEqual(2600025, intersectionPolyline.FromPoint.X);
			Assert.AreEqual(2600025, intersectionPolyline.ToPoint.X);
		}

		[Test]
		public void CanGetVerticalAndNonVerticalMultipatchIntersectionLine()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2600050, 1200020, lv95));

			GeometryUtils.MakeZAware(originalPoly);

			IMultiPatch multiPatch1 =
				GeometryFactory.CreateMultiPatch(
					GeometryUtils.ConstantZ(originalPoly, 500));

			IRing ring = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200020, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200020, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN,
					                            lv95)));

			IMultiPatch multiPatch2 = GeometryFactory.CreateMultiPatch(ring);
			// The same original poly, but lopsided, cutting through the horizontal multipatch

			List<CutPolyline> cutPolylines = IntersectionUtils.GetIntersectionLines3D(
				                                                  multiPatch1,
				                                                  multiPatch2)
			                                                  .ToList();

			Assert.AreEqual(1, cutPolylines.Count);

			IPolyline intersectionPolyline = cutPolylines[0].Polyline;

			Assert.AreEqual(20 * 20 + 50 * 50,
			                intersectionPolyline.Length * intersectionPolyline.Length);

			Assert.AreEqual(500, intersectionPolyline.FromPoint.Z);
			Assert.AreEqual(500, intersectionPolyline.ToPoint.Z);
			Assert.AreEqual(2600000, intersectionPolyline.FromPoint.X);
			Assert.AreEqual(1200000, intersectionPolyline.FromPoint.Y);
		}

		[Test]
		public void CanGetVerticalVerticalMultipatchIntersectionLine()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IRing ring1 = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200020, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200020, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200000, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200020, 400, double.NaN,
					                            lv95)));

			IMultiPatch multiPatch1 = GeometryFactory.CreateMultiPatch(ring1);

			IRing ring2 = GeometryFactory.CreateRing(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200020, 600, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600050, 1200020, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN,
					                            lv95)));

			IMultiPatch multiPatch2 = GeometryFactory.CreateMultiPatch(ring2);

			List<CutPolyline> cutPolylines = IntersectionUtils.GetIntersectionLines3D(
				                                                  multiPatch1,
				                                                  multiPatch2)
			                                                  .ToList();

			Assert.AreEqual(1, cutPolylines.Count);

			IPolyline intersectionPolyline = cutPolylines[0].Polyline;

			Assert.AreEqual(200, ((ICurve3D) intersectionPolyline).Length3D);

			Assert.AreEqual(600, intersectionPolyline.Envelope.ZMax);
			Assert.AreEqual(400, intersectionPolyline.Envelope.ZMin);
			Assert.AreEqual(0, intersectionPolyline.Envelope.Width);
			Assert.AreEqual(0, intersectionPolyline.Envelope.Height);
		}

		[Test]
		public void CanGetDifferenceExactlyAlongTarget()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			const double xyTolerance = 0.01;
			((ISpatialReferenceTolerance) lv95).XYTolerance = xyTolerance;

			// The middle vertex of the source is just below 1cm from the target line's interior
			// ITopologicalOperator.Difference does not exactly return the source geometry even
			// though it should. This results in sub-tolerance differences (unless the target vertices
			// are inserted).
			IPoint touchingVertex = GeometryFactory.CreatePoint(
				2794801.49, 1177166.708, 2420.791, double.NaN, lv95);

			IPath sourcePath =
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2794796.648, 1177163.426, 2420.151,
					                            double.NaN, lv95),
					touchingVertex,
					GeometryFactory.CreatePoint(2794798.199, 1177179.766,
					                            2412.47100000001, double.NaN, lv95));

			IPolyline source = GeometryFactory.CreatePolyline(sourcePath, lv95, true);

			IPath targetPath =
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2794741.351, 1177048.435, 2432.336,
					                            double.NaN, lv95),
					GeometryFactory.CreatePoint(2794811.995, 1177187.343,
					                            2435.02499999999, double.NaN, lv95));
			IPolyline target = GeometryFactory.CreatePolyline(targetPath, lv95, true);

			IGeometry difference = IntersectionUtils.Difference(source, target);
			Assert.True(GeometryUtils.AreEqual(source, difference));

			// NOTE: with the standard implementation the difference is more than half a tolerance off!
			IPoint closePoint = ((IPointCollection) difference).Point[1];
			Assert.IsTrue(
				GeometryUtils.IsSamePoint(touchingVertex, closePoint, xyTolerance,
				                          xyTolerance));

			IPolyline differenceLinesXY = IntersectionUtils.GetDifferenceLinesXY(
				source, target, xyTolerance);

			// Exactly the same:
			Assert.True(GeometryUtils.AreEqual(source, differenceLinesXY));
			IPoint closePointCustom = ((IPointCollection) differenceLinesXY).Point[1];
			Assert.IsTrue(
				GeometryUtils.IsSamePoint(touchingVertex, closePointCustom, 0.0001,
				                          0.0001));
		}

		[Test]
		public void CanRemoveRingLinearSelfIntersections()
		{
			// This functionality is mainly for Andi (footprints), but probably it would also make sense
			// for non-vertical multipatches.

			var multipatchWithDangle = (IMultiPatch) GeometryUtils.GetHighLevelGeometry(
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("MultipatchWithCutBack.xml")));

			foreach (GeometryPart part in PartExtractionUtils.GetGeometryParts(
				multipatchWithDangle, false))
			{
				double tolerance = 0.01;

				Assert.NotNull(part.MainOuterRing);

				Linestring linestring =
					GeometryConversionUtils.CreateLinestring(part.MainOuterRing);

				List<Linestring> results = new List<Linestring>();
				Assert.True(
					GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
						linestring, tolerance, results));

				Assert.AreEqual(1, results.Count);

				Assert.False(GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
					             results[0], tolerance, results));
			}

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			var polygon = (IPolygon) GeometryUtils.GetHighLevelGeometry(
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithSpike.xml")));

			polygon.SpatialReference = sr;

			var result = IntersectionUtils.RemoveRingLinearSelfIntersections(
				polygon, 0.1);

			Assert.AreEqual(6.96580967114, ((IArea) result).Area, 0.000001);
			Assert.IsTrue(GeometryUtils.IsGeometrySimple(result, sr, false, out _));

			var resultSecondRun =
				IntersectionUtils.RemoveRingLinearSelfIntersections(result, 0.1, 0.02);

			Assert.True(resultSecondRun == result);

			polygon = (IPolygon) GeometryUtils.GetHighLevelGeometry(
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("PolygonWithNarrowStrait.xml")));
			polygon.SpatialReference = sr;

			result = IntersectionUtils.RemoveRingLinearSelfIntersections(polygon, 0.13);

			Assert.IsTrue(GeometryUtils.IsGeometrySimple(result, sr, false, out _));
			Assert.AreEqual(7.06456, ((IArea) result).Area, 0.0001);

			// With a nice orthogonal cut-off:
			result = IntersectionUtils.RemoveRingLinearSelfIntersections(polygon, 0.13, 0.02);

			Assert.AreEqual(7.063891, ((IArea) result).Area, 0.0001);
			Assert.IsTrue(GeometryUtils.IsGeometrySimple(result, sr, false, out _));

			// Second time (requires smaller tolerance because of small segment length at cut-off
			resultSecondRun =
				IntersectionUtils.RemoveRingLinearSelfIntersections(result, 0.02);

			Assert.True(resultSecondRun == result);
		}

		[Test]
		public void CanGetDifferenceUsingCustomIntersectClosedPolyline()
		{
			// Found by Deborah. Tests the case of both crossings AND a linear intersection
			// on the same segment.
			var poly1 = (IPolygon) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("MixedIntersectionTypesOnSameSegmentPoly1.xml"));

			var poly2 = (IPolygon) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("MixedIntersectionTypesOnSameSegmentPoly2.xml"));

			IPolyline sourcePolyline = GeometryFactory.CreatePolyline(poly1);
			IPolyline targetPolyline = GeometryFactory.CreatePolyline(poly2);

			double xyTolerance = GeometryUtils.GetXyTolerance(sourcePolyline);

			IntersectionUtils.UseCustomIntersect = false;

			Stopwatch watch = Stopwatch.StartNew();

			IPointCollection intersectionPointsClassic = (IPointCollection)
				IntersectionUtils.GetIntersectionPoints(sourcePolyline, targetPolyline);

			IPolyline difference =
				(IPolyline) ((ITopologicalOperator) targetPolyline).Difference(sourcePolyline);
			//IntersectionUtils.GetDifferenceLinesXY(targetPolyline, sourcePolyline, xyTolerance);

			watch.Stop();
			Console.WriteLine(
				"IntersectionPoints+Difference (ArcObjects): {0}", watch.ElapsedMilliseconds);

			IntersectionUtils.UseCustomIntersect = true;

			watch.Restart();

			var intersectionPointsCustom = (IPointCollection)
				IntersectionUtils.GetIntersectionPoints(sourcePolyline, targetPolyline);

			Assert.AreEqual(
				intersectionPointsClassic.PointCount, intersectionPointsCustom.PointCount);

			IPolyline customDifference =
				IntersectionUtils.GetDifferenceLinesXY(targetPolyline, sourcePolyline, xyTolerance);

			watch.Stop();
			Console.WriteLine(
				"IntersectionPoints+Difference (Custom): {0}", watch.ElapsedMilliseconds);

			Assert.AreEqual(
				GeometryUtils.GetLength(difference),
				GeometryUtils.GetLength(customDifference), 0.0001);
		}

		private static IMultipoint GetIntersectionPointsXy(IPolycurve polycurve1,
		                                                   IPolycurve polycurve2,
		                                                   double tolerance,
		                                                   bool useCustomIntersect)
		{
			bool originalValue = IntersectionUtils.UseCustomIntersect;

			IMultipoint result;

			try
			{
				IntersectionUtils.UseCustomIntersect = useCustomIntersect;

				result = IntersectionUtils.GetIntersectionPointsXY(
					polycurve1, polycurve2, tolerance);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = originalValue;
			}

			return result;
		}

		private static IMultiPatch GetSingleRingMultipatch()
		{
			// Returns a single, non-vertical ring multipatch (TLM_GEBAEUDE {68B2C5AA-27BE-497C-BF60-A4D4987C2679})

			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// The problem cannot be reproduced with standard resolution
			var srResolution = (ISpatialReferenceResolution) lv95;
			srResolution.set_XYResolution(true, 0.001);

			var srTolerance = (ISpatialReferenceTolerance) lv95;
			srTolerance.XYTolerance = 0.01;

			// Vertical triangle, oriented towards the south:
			var points = new WKSPointZ[6];
			points[0] = new WKSPointZ
			            {
				            X = 2578309.3000000007,
				            Y = 1183264.3999999985,
				            Z = 619.14500000000407
			            };

			points[1] = new WKSPointZ
			            {
				            X = 2578295.6829999983,
				            Y = 1183260.568,
				            Z = 619.14500000000407
			            };

			points[2] = new WKSPointZ
			            {
				            X = 2578293.9990000017,
				            Y = 1183266.5500000007,
				            Z = 619.14500000000407
			            };

			points[3] = new WKSPointZ
			            {
				            X = 2578295.9070000015,
				            Y = 1183267.1559999995,
				            Z = 619.14500000000407
			            };

			points[4] = new WKSPointZ
			            {
				            X = 2578307.5989999995,
				            Y = 1183270.4450000003,
				            Z = 619.14500000000407
			            };

			points[5] = new WKSPointZ
			            {
				            X = 2578309.3000000007,
				            Y = 1183264.3999999985,
				            Z = 619.14500000000407
			            };

			IRing ring = new RingClass();
			((IGeometry) ring).SpatialReference = lv95;
			GeometryUtils.MakeZAware(ring);
			GeometryUtils.SetWKSPointZs(ring, points);

			IMultiPatch multipatch = new MultiPatchClass();
			((IGeometry) multipatch).SpatialReference = lv95;

			GeometryUtils.MakeZAware(multipatch);
			GeometryUtils.MakeMAware(multipatch);

			GeometryUtils.MakePointIDAware(multipatch);

			GeometryFactory.AddRingToMultiPatch(ring, multipatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchOuterRing);
			return multipatch;
		}

		private static string GetMatrixExpression(string matrixString)
		{
			return string.Format("RELATE (G1, G2, '{0}')", matrixString);
		}

		[NotNull]
		private static ISpatialReference CreateSpatialReference(double xyTolerance,
		                                                        double xyResolution)
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, setDefaultXyDomain: true);

			((ISpatialReferenceResolution) result).XYResolution[true] = xyResolution;
			((ISpatialReferenceTolerance) result).XYTolerance = xyTolerance;

			return result;
		}

		[NotNull]
		private static IPolyline CreatePolyline(ISpatialReference spatialReference,
		                                        double x,
		                                        double y, double length)
		{
			IPolyline result = GeometryFactory.CreatePolyline(x, y, x + length, y);
			result.SpatialReference = spatialReference;
			return result;
		}

		private static void AddPoint([NotNull] IGeometry target, double x, double y)
		{
			var points = (IPointCollection) target;

			object ndef = Type.Missing;
			points.AddPoint(GeometryFactory.CreatePoint(x, y), ref ndef, ref ndef);
		}
	}
}

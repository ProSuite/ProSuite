using System;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Tests.Test.Construction;

namespace ProSuite.QA.Tests.Test
{
	public class ZDifferenceStrategyIntersectionPointsTest
	{
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanIntersectPolygonWithPolygon()
		{
			var g1 = CurveConstruction.StartPoly(0, 0, 0)
			                          .LineTo(10, 0, 0)
			                          .LineTo(10, 10, 5)
			                          .LineTo(0, 10, 5)
			                          .ClosePolygon();
			var g2 =
				CurveConstruction.StartPoly(5, 5, 10)
				                 .LineTo(15, 5, 10)
				                 .LineTo(15, 15, 10)
				                 .LineTo(5, 15, 10)
				                 .ClosePolygon();

			Check(g1, g2, 2,
			      (ip, index) =>
				      At(index == 0, Equals(5, 10, 5, 10, ip)) &&
				      At(index == 1, Equals(10, 5, 2.5, 10, ip)));
			Check(g2, g1, 2,
			      (ip, index) =>
				      At(index == 0, Equals(10, 5, 10, 2.5, ip)) &&
				      At(index == 1, Equals(5, 10, 10, 5, ip)));
		}

		[Test]
		public void CanIntersectPolygonWithTouchingPolygon()
		{
			var g1 = CurveConstruction.StartPoly(0, 0, 0)
			                          .LineTo(10, 0, 0)
			                          .LineTo(10, 10, 5)
			                          .LineTo(0, 10, 5)
			                          .ClosePolygon();
			var g2 = CurveConstruction.StartPoly(10, 5, 10)
			                          .LineTo(20, 5, 10)
			                          .LineTo(20, 15, 10)
			                          .LineTo(10, 15, 10)
			                          .ClosePolygon();

			Check(g1, g2, 0);
		}

		[Test]
		public void CanIntersectPoints()
		{
			const double z1 = 10;
			const double z2 = 20;
			var g1 = GeometryFactory.CreatePoint(100, 200, z1);
			var g2 = GeometryFactory.CreatePoint(100, 200, z2);

			Check(g1, g2, 1, (p, i) => Equals(100, 200, z1, p.Point));
			Check(g2, g1, 1, (p, i) => Equals(100, 200, z2, p.Point));
		}

		[Test]
		public void CanIntersectPointWithTouchingLine()
		{
			const double z1 = 10;
			const double z2 = 20;
			var g1 = GeometryFactory.CreatePoint(100, 200, z1);
			var g2 = CurveConstruction.StartLine(100, 200, z2)
			                          .LineTo(100, 300, z2)
			                          .Curve;

			Check(g1, g2, 0);
			Check(g2, g1, 0);
		}

		[Test]
		public void CanIntersectPointWithLine()
		{
			const double z1 = 10;
			const double z2 = 20;
			var g1 = GeometryFactory.CreatePoint(100, 0, z1);
			var g2 = CurveConstruction.StartLine(0, 0, z2)
			                          .LineTo(200, 0, z2)
			                          .Curve;

			Check(g1, g2, 1, (p, i) => Equals(100, 0, z1, p.Point));
			Check(g2, g1, 1, (p, i) => Equals(100, 0, z2, p.Point));
		}

		[Test]
		public void CanIntersectCircles()
		{
			const double z1 = 200;
			const double z2 = 300;
			var g1 = GeometryFactory.CreateCircleArcPolygon(
				GeometryFactory.CreatePoint(100, 100, z1), 10);

			// NOTE: A single segment circle always has Z == NaN, no matter what!
			IMultipoint splitPoints = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(90, 100, z1),
				GeometryFactory.CreatePoint(110, 100, z1));
			GeometryUtils.CrackPolycurve(g1, (IPointCollection) splitPoints, false, false);

			GeometryUtils.MakeZAware(g1);
			GeometryUtils.ConstantZ(g1, z1);

			// Otherwise it is Z aware but the Z values remain NaN
			GeometryUtils.SimplifyZ(g1);

			var g2 = GeometryFactory.CreateCircleArcPolygon(
				GeometryFactory.CreatePoint(90, 90, z2), 10);

			// NOTE: A single segment circle always has Z == NaN, no matter what!
			splitPoints = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(80, 90, z2),
				GeometryFactory.CreatePoint(100, 90, z2));
			GeometryUtils.CrackPolycurve(g2, (IPointCollection) splitPoints, false, false);

			GeometryUtils.MakeZAware(g2);
			GeometryUtils.ConstantZ(g2, z2);
			GeometryUtils.SimplifyZ(g2);

			Check(g1, g2, 2,
			      (p, i) =>
				      At(i == 0, Equals(100, 90, z1, p.Point)) &&
				      At(i == 1, Equals(90, 100, z1, p.Point)));
		}

		[Test]
		public void CanIntersectCircleWithRectangle()
		{
			var z1 = 100.0;
			var z2 = 200.0;
			var g1 = GeometryFactory.CreateCircleArcPolygon(
				GeometryFactory.CreatePoint(100, 100, z1), 10);

			// NOTE: A single segment circle always has Z == NaN, no matter what!
			IMultipoint splitPoints = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(90, 100, z1),
				GeometryFactory.CreatePoint(110, 100, z1));
			GeometryUtils.CrackPolycurve(g1, (IPointCollection) splitPoints, false, false);

			GeometryUtils.MakeZAware(g1);
			GeometryUtils.ConstantZ(g1, z1);

			// Otherwise it is Z aware but the Z values remain NaN
			GeometryUtils.SimplifyZ(g1);

			var g2 = GeometryFactory.CreatePolygon(0, 0, 100, 200, z2);

			Assert.IsTrue(GeometryUtils.IsZAware(g1));
			Assert.IsTrue(GeometryUtils.IsZAware(g2));

			Check(g1, g2, 2,
			      (p, i) =>
				      At(i == 0, Equals(100, 90, z1, p.Point)) &&
				      At(i == 1, Equals(100, 110, z1, p.Point)));
			Check(g2, g1, 2,
			      (p, i) =>
				      At(i == 0, Equals(100, 110, z2, p.Point)) && // TODO revise
				      At(i == 1, Equals(100, 90, z2, p.Point))); // TODO revise
		}

		[Test]
		public void CanGetDistanceToPlanarPolygon()
		{
			const double z1 = 10;
			const double z2 = 20;
			var g1 = GeometryFactory.CreatePoint(100, 0, z1);
			var g2 = GeometryFactory.CreatePolygon(0, 0, 100, 200);
			GeometryUtils.MakeZAware(g2);
			g2 = GeometryUtils.ConstantZ(g2, z2);

			var polygonClass = new FeatureClassMock(
				"polygon", esriGeometryType.esriGeometryPolygon,
				1,
				esriFeatureType.esriFTSimple,
				SpatialReferenceUtils.CreateSpatialReference(
					WellKnownHorizontalCS.LV95));

			var feature = polygonClass.CreateFeature(g2);

			var intersectionPoints =
				ZDifferenceStrategyIntersectionPoints.GetDistanceToPlane(
					g1, ReadOnlyRow.Create(feature), 0.01);

			foreach (var either in intersectionPoints)
			{
				either.Match(e =>
				             {
					             Assert.Fail($"unexpected nonplanar error: {e.Message}");
					             return -1;
				             },
				             pts =>
				             {
					             var points = pts.ToList();
					             Assert.AreEqual(1, points.Count);
					             Assert.AreEqual(z2 - z1, points[0].Distance);
					             return 0;
				             });
			}
		}

		[Test]
		public void CantGetDistanceToNonPlanarPolygon()
		{
			const double z1 = 10;
			var g1 = GeometryFactory.CreatePoint(100, 0, z1);
			var polygon =
				CurveConstruction.StartPoly(0, 0, 10)
				                 .LineTo(10, 0, 10)
				                 .LineTo(10, 10, 11)
				                 .LineTo(0, 10, 10)
				                 .ClosePolygon();

			var polygonClass = new FeatureClassMock(
				"polygon", esriGeometryType.esriGeometryPolygon,
				1,
				esriFeatureType.esriFTSimple,
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

			var feature = polygonClass.CreateFeature(polygon);

			var intersectionPoints =
				ZDifferenceStrategyIntersectionPoints.GetDistanceToPlane(
					g1, ReadOnlyRow.Create(feature), 0.01);

			foreach (var either in intersectionPoints)
			{
				either.Match(e =>
				             {
					             Console.WriteLine(
						             $@"expected nonplanar error: {e.Message} ({e.MaximumOffset})");
					             Assert.AreEqual(0.3324801391253977, e.MaximumOffset);
					             return 0;
				             },
				             pts =>
				             {
					             Assert.Fail("Unexpected intersection point");
					             return -1;
				             });
			}
		}

		[Test]
		public void CanIntersectLineWithPolygon()
		{
			const int z1 = 100;
			const int z2 = 200;
			var g1 = CurveConstruction.StartLine(-100, 50, z1)
			                          .LineTo(200, 50, z1)
			                          .Curve;
			var g2 = GeometryFactory.CreatePolygon(0, 0, 100, 200);
			GeometryUtils.MakeZAware(g2);
			GeometryUtils.ConstantZ(g2, z2);

			Check(g1, g2, 2,
			      (p, i) =>
				      At(i == 1, Equals(100, 50, z1, p.Point)) &&
				      At(i == 0, Equals(0, 50, z1, p.Point)));
		}

		[Test]
		public void CanIntersectLineWithTouchingPolygon()
		{
			const int z1 = 100;
			const int z2 = 200;
			var g1 = CurveConstruction.StartLine(100, 50, z1)
			                          .LineTo(200, 50, z1)
			                          .Curve;
			var g2 = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			GeometryUtils.MakeZAware(g2);
			GeometryUtils.ConstantZ(g2, z2);

			Check(g1, g2, 0);
			Check(g2, g1, 0);
		}

		[Test]
		public void CanIntersectLineWithLine()
		{
			const int z1 = 100;
			const int z2 = 200;
			var g1 = CurveConstruction.StartLine(0, 50, z1)
			                          .LineTo(100, 50, z1)
			                          .Curve;
			var g2 = CurveConstruction.StartLine(50, 0, z2)
			                          .LineTo(50, 100, z2)
			                          .Curve;

			Check(g1, g2, 1, (p, i) => Equals(50, 50, z1, p.Point));
			Check(g2, g1, 1, (p, i) => Equals(50, 50, z2, p.Point));
		}

		[Test]
		public void CanIntersectLineWithTouchingLine()
		{
			const int z1 = 100;
			const int z2 = 200;
			var g1 = CurveConstruction.StartLine(0, 0, z1)
			                          .LineTo(100, 0, z1)
			                          .Curve;
			var g2 = CurveConstruction.StartLine(100, 0, z2)
			                          .LineTo(200, 0, z2)
			                          .Curve;

			Check(g1, g2, 0);
			Check(g2, g1, 0);
		}

		[Test]
		public void CanIntersectLineWithTouchingAndCrossingLine()
		{
			const int z1 = 100;
			const int z2 = 200;
			var g1 = CurveConstruction.StartLine(0, 0, z1)
			                          .LineTo(100, 0, z1)
			                          .Curve;
			var g2 = CurveConstruction.StartLine(100, 0, z2)
			                          .LineTo(100, 10, z2)
			                          .LineTo(50, 10, z2)
			                          .LineTo(50, -10, z2)
			                          .Curve;

			Check(g1, g2, 1, (p, i) => Equals(50, 0, z1, z2, p));
			Check(g2, g1, 1, (p, i) => Equals(50, 0, z2, z1, p));
		}

		[Test]
		public void CanIntersectPolygonWithTouchingPolygonInCorner()
		{
			var g1 = CurveConstruction.StartPoly(0, 0, 0)
			                          .LineTo(10, 0, 0)
			                          .LineTo(10, 10, 5)
			                          .LineTo(0, 10, 5)
			                          .ClosePolygon();
			var g2 = CurveConstruction.StartPoly(10, 10, 10)
			                          .LineTo(20, 10, 10)
			                          .LineTo(20, 20, 10)
			                          .LineTo(10, 20, 10)
			                          .ClosePolygon();

			Check(g1, g2, 0);
			Check(g2, g1, 0);
		}

		[Test]
		public void CanIntersectPolygonWithMultiPatch()
		{
			const double z1 = 10;
			const double z2 = 20;
			var g1 = CurveConstruction.StartPoly(5, 5, z1)
			                          .LineTo(15, 5, z1)
			                          .LineTo(15, 15, z1)
			                          .LineTo(5, 15, z1)
			                          .ClosePolygon();
			var g2 = new MultiPatchConstruction().StartOuterRing(0, 0, z2)
			                                     .Add(10, 0, z2)
			                                     .Add(10, 10, z2)
			                                     .Add(0, 10, z2)
			                                     .MultiPatch;

			Check(g1, g2, 2,
			      (p, i) =>
				      At(i == 1, Equals(5, 10, z1, p.Point)) &&
				      At(i == 0, Equals(10, 5, z1, p.Point)));
			// inverse order in separate test
		}

		[Test]
		public void CanIntersectMultiPatchWithPolygon()
		{
			const double z1 = 10;
			const double z2 = 20;
			var g1 = CurveConstruction.StartPoly(5, 5, z1)
			                          .LineTo(15, 5, z1)
			                          .LineTo(15, 15, z1)
			                          .LineTo(5, 15, z1)
			                          .ClosePolygon();
			var g2 = new MultiPatchConstruction().StartOuterRing(0, 0, z2)
			                                     .Add(10, 0, z2)
			                                     .Add(10, 10, z2)
			                                     .Add(0, 10, z2)
			                                     .MultiPatch;

			Check(g2, g1, 2,
			      (p, i) =>
				      At(i == 1, Equals(5, 10, z2, p.Point)) &&
				      At(i == 0, Equals(10, 5, z2, p.Point)));
		}

		// TODO
		// - line/line - collinear, endpoint-to-interior
		// - line/polygon - collinear
		// - polygon/point
		// - multipatch/point
		// - everything also in reversed order

		private static bool At(bool condition, bool check) => ! condition || check;

		private static bool Equals(double? x, double? y, double? z, [NotNull] IPoint p)
		{
			ISpatialReference sr = p.SpatialReference;
			double t = SpatialReferenceUtils.GetXyTolerance(sr);
			return
				(x == null || Math.Abs(x.Value - p.X) < t) &&
				(y == null || Math.Abs(y.Value - p.Y) < t) &&
				(z == null || Math.Abs(z.Value - p.Z) < SpatialReferenceUtils.GetZTolerance(sr));
		}

		private static bool Equals(
			double? x, double? y, double? z, double? otherZ,
			[NotNull] ZDifferenceStrategyIntersectionPoints.IIntersectionPoint p)
		{
			return (x == null || Equals(x, p.Point.X)) &&
			       (y == null || Equals(y, p.Point.Y)) &&
			       (z == null || Equals(z, p.Point.Z)) &&
			       (otherZ == null || OtherZEquals(otherZ.Value, p));
		}

		private static bool OtherZEquals(double value,
		                                 ZDifferenceStrategyIntersectionPoints.IIntersectionPoint p)
		{
			var spatialReference = p.Point.SpatialReference;
			var zTolerance = ((ISpatialReferenceTolerance) spatialReference).ZTolerance;

			return Math.Abs(value - p.OtherZ) <= zTolerance;
		}

		private void Check(
			[NotNull] IGeometry g1, [NotNull] IGeometry g2,
			int count,
			[CanBeNull] Func<ZDifferenceStrategyIntersectionPoints.IIntersectionPoint, int, bool>
				predicate = null)
		{
			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;
			double xyTolerance = GeometryUtils.GetXyTolerance(g1);

			Assert.True(GeometryUtils.IsZAware(g1));
			Assert.True(GeometryUtils.IsZAware(g2));

			var points = ZDifferenceStrategyIntersectionPoints
			             .GetIntersections(g1, g2, _spatialReference, xyTolerance)
			             .ToList();

			points.ForEach(p => Console.WriteLine(GeometryUtils.ToString(p.Point)));

			Assert.AreEqual(count, points.Count, "intersection count");

			Assert.True(
				points.TrueForAll(g => g.Point.SpatialReference == _spatialReference),
				"unexpected spatial reference");
			Assert.True(points.TrueForAll(ip => GeometryUtils.IsZAware(ip.Point)),
			            "points must be Z aware");

			if (predicate != null)
			{
				var errors = points.Select((ip, i) => predicate(ip, i)
					                                      ? string.Empty
					                                      : $"- point {i}: {ip.Point.X}, {ip.Point.Y}, {ip.Point.Z}, other Z: {ip.OtherZ}")
				                   .Where(e => ! string.IsNullOrEmpty(e))
				                   .ToList();

				Assert.True(errors.Count == 0,
				            $"Predicate not fulfilled:{Environment.NewLine}" +
				            $"{StringUtils.Concatenate(errors, Environment.NewLine)}");
			}
		}

		// [NotNull]
		// private List<IPoint> CheckIntersections([NotNull] IGeometry g1,
		//                                         [NotNull] IGeometry g2,
		//                                         int expectedIntersectionCount)
		// {
		// 	g1.SpatialReference = _spatialReference;
		// 	g2.SpatialReference = _spatialReference;
		//
		// 	Assert.True(GeometryUtils.IsZAware(g1));
		// 	Assert.True(GeometryUtils.IsZAware(g2));
		//
		// 	var intersections =
		// 		ZDifferenceStrategyIntersectionPoints
		// 			.GetIntersections(g1, g2, recyclePoint : true)
		// 			.Select(GeometryFactory.Clone)
		// 			.ToList();
		//
		// 	foreach (IPoint intersection in intersections)
		// 	{
		// 		Console.WriteLine(GeometryUtils.ToString(intersection));
		// 	}
		//
		// 	Assert.AreEqual(expectedIntersectionCount, intersections.Count,
		// 	                "intersection count");
		// 	Assert.True(
		// 		intersections.TrueForAll(g => g.SpatialReference == _spatialReference),
		// 		"unexpected spatial reference");
		// 	Assert.True(intersections.TrueForAll(GeometryUtils.IsZAware),
		// 	            "intersection points must be Z aware");
		// 	return intersections;
		// }
	}
}

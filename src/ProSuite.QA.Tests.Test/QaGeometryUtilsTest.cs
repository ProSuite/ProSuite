using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Tests.Test.Construction;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGeometryUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanCalculateVerticalProjectedAreaConstantX2()
		{
			// using coordinates from an actual building face (exactly north-south oriented)
			var wksPointZs =
				new List<WKSPointZ>
				{
					new WKSPointZ { X = 2722584.8725, Y = 1252311.12875, Z = 618.42624999999 },
					new WKSPointZ { X = 2722584.8725, Y = 1252309.3725, Z = 619.654999999984 },
					new WKSPointZ { X = 2722584.8725, Y = 1252304.75375, Z = 616.388749999984 },
					new WKSPointZ { X = 2722584.8725, Y = 1252311.12875, Z = 618.42624999999 }
				};

			List<Pnt> points =
				wksPointZs.Select(wksPointZ => QaGeometryUtils.CreatePoint3D(wksPointZ)).ToList();

			var pointsWithoutEndPoint = new List<Pnt>(points);
			pointsWithoutEndPoint.RemoveAt(points.Count - 1);

			Plane plane = QaGeometryUtils.CreatePlane(pointsWithoutEndPoint);

			double area;
			double perimeter;
			QaGeometryUtils.CalculateProjectedArea(plane, points, out area, out perimeter);

			Console.WriteLine(@"area: {0} perimeter: {1}", area, perimeter);

			const double e = 0.000001;
			Assert.AreEqual(5.70582031258122, Math.Abs(area), e);
			Assert.AreEqual(14.4930667883504, perimeter, e);
		}

		[Test]
		public void CanCalculateSmallTris()
		{
			var p0 = new WKSPointZ { X = 679845.1814, Y = 253110.2683, Z = 447.21000000000004 };
			var p1 = new WKSPointZ { X = 679845.468, Y = 253110.3961, Z = 447.21000000000004 };
			var p2 = new WKSPointZ { X = 679845.4679, Y = 253110.3961, Z = 447.21000000000004 };

			var q2 = new WKSPointZ { X = 679845.46795, Y = 253110.3961, Z = 447.21000000000004 };

			foreach (double f in new[] { 0.001, 0.03, 0.5, 1, 7, 15, 76, 1000 })
			{
				ValidateTri(Scale(new List<WKSPointZ> { p0, p1, p2, p0 }, f), true);
				ValidateTri(Scale(new List<WKSPointZ> { p0, p1, q2, p0 }, f), false);
			}
		}

		private void ValidateTri(List<WKSPointZ> wksPointZs, bool isDefined)
		{
			List<Pnt> points =
				wksPointZs.Select(wksPointZ => QaGeometryUtils.CreatePoint3D(wksPointZ)).ToList();

			var pointsWithoutEndPoint = new List<Pnt>(points);
			pointsWithoutEndPoint.RemoveAt(points.Count - 1);

			Plane plane = QaGeometryUtils.CreatePlane(pointsWithoutEndPoint);
			Assert.True(plane.IsDefined == isDefined);
		}

		private List<WKSPointZ> Scale(List<WKSPointZ> ps, double f)
		{
			List<WKSPointZ> scaled = new List<WKSPointZ>(ps.Count);
			foreach (WKSPointZ p in ps)
			{
				scaled.Add(Scale(p, f));
			}

			return scaled;
		}

		private WKSPointZ Scale(WKSPointZ p, double f)
		{
			return new WKSPointZ { X = f * p.X, Y = f * p.Y, Z = f * p.Z };
		}

		[Test]
		public void CanCalculateVerticalProjectedAreaConstantX()
		{
			var wksPointZs =
				new List<WKSPointZ>
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 10 },
					new WKSPointZ { X = 0, Y = 0, Z = 10 },
					new WKSPointZ { X = 0, Y = 0, Z = 0 }
				};

			List<Pnt> points =
				wksPointZs.Select(wksPointZ => QaGeometryUtils.CreatePoint3D(wksPointZ)).ToList();

			var pointsWithoutEndPoint = new List<Pnt>(points);
			pointsWithoutEndPoint.RemoveAt(points.Count - 1);

			Plane plane = QaGeometryUtils.CreatePlane(pointsWithoutEndPoint);

			double area;
			double perimeter;
			QaGeometryUtils.CalculateProjectedArea(plane, points, out area, out perimeter);

			Console.WriteLine(@"area: {0} perimeter: {1}", area, perimeter);

			Assert.AreEqual(100, Math.Abs(area));
			Assert.AreEqual(40, perimeter);
		}

		[Test]
		public void CanCalculateVerticalProjectedAreaConstantY()
		{
			var wksPointZs =
				new List<WKSPointZ>
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 0, Z = 10 },
					new WKSPointZ { X = 0, Y = 0, Z = 10 },
					new WKSPointZ { X = 0, Y = 0, Z = 0 }
				};

			List<Pnt> points =
				wksPointZs.Select(wksPointZ => QaGeometryUtils.CreatePoint3D(wksPointZ)).ToList();

			var pointsWithoutEndPoint = new List<Pnt>(points);
			pointsWithoutEndPoint.RemoveAt(points.Count - 1);

			Plane plane = QaGeometryUtils.CreatePlane(pointsWithoutEndPoint);

			double area;
			double perimeter;
			QaGeometryUtils.CalculateProjectedArea(plane, points, out area, out perimeter);

			Console.WriteLine(@"area: {0} perimeter: {1}", area, perimeter);

			Assert.AreEqual(100, Math.Abs(area));
			Assert.AreEqual(40, perimeter);
		}

		[Test]
		public void CanCalculateVerticalProjectedAreaDiagonalXY()
		{
			var wksPointZs =
				new List<WKSPointZ>
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 10, Z = 0 },
					new WKSPointZ { X = 10, Y = 10, Z = 10 },
					new WKSPointZ { X = 0, Y = 0, Z = 10 },
					new WKSPointZ { X = 0, Y = 0, Z = 0 }
				};

			List<Pnt> points =
				wksPointZs.Select(wksPointZ => QaGeometryUtils.CreatePoint3D(wksPointZ)).ToList();

			var pointsWithoutEndPoint = new List<Pnt>(points);
			pointsWithoutEndPoint.RemoveAt(points.Count - 1);

			Plane plane = QaGeometryUtils.CreatePlane(pointsWithoutEndPoint);

			double area;
			double perimeter;
			QaGeometryUtils.CalculateProjectedArea(plane, points, out area, out perimeter);

			Console.WriteLine(@"area: {0} perimeter: {1}", area, perimeter);

			const double e = 0.0000001;
			Assert.AreEqual(141.421356237309, Math.Abs(area), e);
			Assert.AreEqual(48.2842712474619, perimeter, e);
		}

		[Test]
		public void CanCalculateHorizontalProjectedArea()
		{
			var wksPointZs =
				new List<WKSPointZ>
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 0, Z = 0 }
				};

			List<Pnt> points =
				wksPointZs.Select(wksPointZ => QaGeometryUtils.CreatePoint3D(wksPointZ)).ToList();

			var pointsWithoutEndPoint = new List<Pnt>(points);
			pointsWithoutEndPoint.RemoveAt(points.Count - 1);

			Plane plane = QaGeometryUtils.CreatePlane(pointsWithoutEndPoint);

			double area;
			double perimeter;
			QaGeometryUtils.CalculateProjectedArea(plane, points, out area, out perimeter);

			Console.WriteLine(@"area: {0} perimeter: {1}", area, perimeter);

			Assert.AreEqual(100, Math.Abs(area));
			Assert.AreEqual(40, perimeter);
		}

		[Test]
		public void IsPlaneVerticalTest()
		{
			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 5)
			            .Add(0, 0, 5);
			IMultiPatch multiPatch = construction.MultiPatch;
			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			Plane plane = QaGeometryUtils.CreatePlane(indexedMultiPatch.GetSegments());
			WKSPointZ normal = plane.GetNormalVector();
			Assert.AreEqual(0, normal.Z);
		}

		[Test]
		public void CanProjectToPlaneTest()
		{
			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5, 0, 5)
			            .Add(0, 0, 5);
			IMultiPatch multiPatch = construction.MultiPatch;
			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			IList<Pnt> points = QaGeometryUtils.GetPoints(indexedMultiPatch.GetSegments());
			Plane plane = QaGeometryUtils.CreatePlane(points);
			IList<WKSPointZ> projected = QaGeometryUtils.ProjectToPlane(plane, points);

			ValidateForm(indexedMultiPatch, projected);
		}

		private static void ValidateForm([NotNull] IIndexedMultiPatch indexedMultiPatch,
		                                 [NotNull] IEnumerable<WKSPointZ> projected)
		{
			var pre = new WKSPointZ();
			bool notFirst = false;
			IEnumerator<SegmentProxy> segments =
				indexedMultiPatch.GetSegments().GetEnumerator();

			foreach (WKSPointZ wksPoint in projected)
			{
				Assert.AreEqual(0, wksPoint.Z);

				if (notFirst)
				{
					double dx = wksPoint.X - pre.X;
					double dy = wksPoint.Y - pre.Y;
					double length = Math.Sqrt(dx * dx + dy * dy);

					Assert.IsTrue(segments.MoveNext());
					Assert.IsNotNull(segments.Current);

					const bool as3D = true;
					IPnt start = segments.Current.GetStart(as3D);
					IPnt end = segments.Current.GetEnd(as3D);
					double segDx = end.X - start.X;
					double segDy = end.Y - start.Y;
					double segDz = end[2] - start[2];
					double segmentLength = Math.Sqrt(segDx * segDx + segDy * segDy + segDz * segDz);
					Assert.IsTrue(Math.Abs(segmentLength - length) < 1.0e-8);
				}

				pre = wksPoint;
				notFirst = true;
			}
		}
	}
}

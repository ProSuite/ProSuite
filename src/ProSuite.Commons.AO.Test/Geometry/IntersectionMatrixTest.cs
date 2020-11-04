using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class IntersectionMatrixTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

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
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanCreateValid()
		{
			const string validMatrix = "TF012****";

			new IntersectionMatrix(validMatrix);
		}

		[Test]
		public void CanDetermineSymmetry()
		{
			Assert.IsTrue(new IntersectionMatrix("T********").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("TTTTTTTTT").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("T*T*T*T*T").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("*********").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("*T*T*****").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("**T***T**").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("T*******T").Symmetric);
			Assert.IsTrue(new IntersectionMatrix("********T").Symmetric);

			Assert.IsFalse(new IntersectionMatrix("*T*******").Symmetric);
			Assert.IsFalse(new IntersectionMatrix("*T*****T*").Symmetric);
			Assert.IsFalse(new IntersectionMatrix("T*T*T*TFT").Symmetric);
		}

		[Test]
		public void CanGetBoundaryBoundaryIntersectionLinesAndPoints()
		{
			const string matrixString = "****T****";

			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);
			IPolygon g2 = GeometryFactory.CreatePolygon(5, 0, 15, 15, 1000);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(2, intersections.Count);

			var polyline = (IPolyline) intersections[0];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(polyline));

			var multipoint = (IMultipoint) intersections[1];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(multipoint));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             multipoint.SpatialReference));
		}

		[Test]
		public void CanGetBoundaryBoundaryIntersectionPoints()
		{
			const string matrixString = "****T****";

			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);
			IPolygon g2 = GeometryFactory.CreatePolygon(5, 5, 15, 15, 1000);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(1, intersections.Count);

			var multipoint = (IMultipoint) intersections[0];

			Assert.AreEqual(2, GeometryUtils.GetPartCount(multipoint));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             multipoint.SpatialReference));
		}

		[Test]
		public void CanGetBoundaryExteriorIntersections()
		{
			const string matrixString = "*****T***";

			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);
			IPolygon g2 = GeometryFactory.CreatePolygon(5, 0, 15, 15, 1000);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(1, intersections.Count);

			var polyline = (IPolyline) intersections[0];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(polyline));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             polyline.SpatialReference));
		}

		[Test]
		public void CanGetInteriorBoundaryIntersections()
		{
			const string matrixString = "*T*******";

			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);
			IPolyline g2line = GeometryFactory.CreatePolyline(
				_spatialReference,
				GeometryFactory.CreatePoint(0, 10, 100), // start/endpoint touches polygon
				GeometryFactory.CreatePoint(0, 20, 100),
				GeometryFactory
					.CreatePoint(10, 10, 100), // next segment touches the polygon
				GeometryFactory
					.CreatePoint(10, 9, 100), // next segment is inside the polygon
				GeometryFactory
					.CreatePoint(6, 9, 100), // next segment is partly inside the polygon
				GeometryFactory.CreatePoint(6, 11, 100),
				GeometryFactory.CreatePoint(4, 11, 100),
				GeometryFactory.CreatePoint(4, 10, 100), // next segment touches polygon
				GeometryFactory.CreatePoint(2, 10, 100),
				GeometryFactory.CreatePoint(2, 11, 100),
				GeometryFactory.CreatePoint(1, 11, 100),
				GeometryFactory
					.CreatePoint(0, 10, 100)); // start/endpoint touches polygon
			GeometryUtils.MakeZAware(g2line);

			IPolygon g2 = GeometryFactory.CreatePolygon(g2line);
			GeometryUtils.Simplify(g2);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(1, intersections.Count);

			var polyline = (IPolyline) intersections[0];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(polyline));
			Assert.AreEqual(3, GeometryUtils.GetPointCount(polyline));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             polyline.SpatialReference));
		}

		[Test]
		public void CanGetCellConstraints()
		{
			const string matrixString = "T012F*21T";

			var matrix = new IntersectionMatrix(matrixString);

			Assert.AreEqual(IntersectionConstraint.MustIntersect,
			                matrix.GetConstraint(PointSet.Interior, PointSet.Interior));
			Assert.AreEqual(IntersectionConstraint.MustIntersectWithMaxDimension0,
			                matrix.GetConstraint(PointSet.Interior, PointSet.Boundary));
			Assert.AreEqual(IntersectionConstraint.MustIntersectWithMaxDimension1,
			                matrix.GetConstraint(PointSet.Interior, PointSet.Exterior));

			Assert.AreEqual(IntersectionConstraint.MustIntersectWithMaxDimension2,
			                matrix.GetConstraint(PointSet.Boundary, PointSet.Interior));
			Assert.AreEqual(IntersectionConstraint.MustNotIntersect,
			                matrix.GetConstraint(PointSet.Boundary, PointSet.Boundary));
			Assert.AreEqual(IntersectionConstraint.NotChecked,
			                matrix.GetConstraint(PointSet.Boundary, PointSet.Exterior));

			Assert.AreEqual(IntersectionConstraint.MustIntersectWithMaxDimension2,
			                matrix.GetConstraint(PointSet.Exterior, PointSet.Interior));
			Assert.AreEqual(IntersectionConstraint.MustIntersectWithMaxDimension1,
			                matrix.GetConstraint(PointSet.Exterior, PointSet.Boundary));
			Assert.AreEqual(IntersectionConstraint.MustIntersect,
			                matrix.GetConstraint(PointSet.Exterior, PointSet.Exterior));
		}

		[Test]
		public void CanGetComplexMatrixIntersections()
		{
			const string matrixString = "T****T***";

			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);
			IPolygon g2 = GeometryFactory.CreatePolygon(5, 0, 15, 15, 1000);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(2, intersections.Count);

			var polygon = (IPolygon) intersections[0];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(polygon));

			var polyline = (IPolyline) intersections[1];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(polyline));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             polyline.SpatialReference));
		}

		[Test]
		public void CanGetInteriorInteriorPolygon()
		{
			const string matrixString = "T********";

			IPolygon g1 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);
			IPolygon g2 = GeometryFactory.CreatePolygon(5, 0, 15, 15, 1000);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(1, intersections.Count);

			var polygon = (IPolygon) intersections[0];
			Assert.AreEqual(1, GeometryUtils.GetPartCount(polygon));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             polygon.SpatialReference));
		}

		[Test]
		public void CanGetPolygonPointIntersections()
		{
			const string matrixString = "T********";

			IPoint g1 = GeometryFactory.CreatePoint(5, 5, 1000);
			IPolygon g2 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(1, intersections.Count);
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             intersections[0]
				                                             .SpatialReference));
		}

		[Test]
		public void CanGetPolygonPolylineIntersections()
		{
			const string matrixString = "T****T***";

			IPolyline g1 = GeometryFactory.CreatePolyline(5, 5, 1000, 15, 5, 1000);
			IPolygon g2 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 100);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(2, intersections.Count);
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             intersections[0]
				                                             .SpatialReference));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             intersections[1]
				                                             .SpatialReference));
		}

		[Test]
		public void CanGetPolylinePointIntersections()
		{
			const string matrixString = "T********";

			IPoint g1 = GeometryFactory.CreatePoint(10, 5, 1000);
			IPolyline g2 = GeometryFactory.CreatePolyline(5, 5, 1000, 15, 5, 1000);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			var matrix = new IntersectionMatrix(matrixString);

			IList<IGeometry> intersections = matrix.GetIntersections(g1, g2);

			WriteGeometries(intersections);

			Assert.AreEqual(1, intersections.Count);
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             intersections[0]
				                                             .SpatialReference));
		}

		[Test]
		public void CanGetLinePolygonCrossesIntersection()
		{
			var matrix = new IntersectionMatrix("T*T******");

			IPolyline g1 = GeometryFactory.CreatePolyline(5, 5, 1000, 15, 5, 1000);
			IPolygon g2 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 0);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			IList<IGeometry> result12 = matrix.GetIntersections(g1, g2);

			Assert.AreEqual(1, result12.Count);
			Console.WriteLine(GeometryUtils.ToString(result12[0]));
			Assert.AreEqual(3, GeometryUtils.GetPointCount(result12));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             result12[0].SpatialReference));

			IList<IGeometry> result21 = matrix.GetIntersections(g2, g1);
			Assert.AreEqual(1, result21.Count);
			Console.WriteLine(GeometryUtils.ToString(result21[0]));
			Assert.IsTrue(
				GeometryUtils.AreEqualInXY(result21[0],
				                           g2)); // can't subtract, same as g2
		}

		[Test]
		public void CanGetMultipointPolygonCrossesIntersection()
		{
			var matrix = new IntersectionMatrix("T*T******");

			IMultipoint g1 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(5, 5, 0),
				GeometryFactory.CreatePoint(10, 5, 0),
				GeometryFactory.CreatePoint(15, 5, 0));
			IPolygon g2 = GeometryFactory.CreatePolygon(0, 0, 10, 10, 0);

			g1.SpatialReference = _spatialReference;
			g2.SpatialReference = _spatialReference;

			IList<IGeometry> result12 = matrix.GetIntersections(g1, g2);

			Assert.AreEqual(1, result12.Count);
			Console.WriteLine(GeometryUtils.ToString(result12[0]));
			Assert.AreEqual(2, GeometryUtils.GetPointCount(result12));
			Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			                                             result12[0].SpatialReference));

			IList<IGeometry> result21 = matrix.GetIntersections(g2, g1);
			Assert.AreEqual(1, result21.Count);
			Console.WriteLine(GeometryUtils.ToString(result21[0]));
			Assert.IsTrue(
				GeometryUtils.AreEqualInXY(result21[0],
				                           g2)); // can't subtract, same as g2
		}

		[Test]
		public void CanGetMultipointPolylineCrossesIntersection()
		{
			var matrix = new IntersectionMatrix("T*T******");

			IMultipoint g1 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(0, 5, 0),
				GeometryFactory.CreatePoint(15, 5, 0));
			IPolyline g2 = GeometryFactory.CreatePolyline(5, 5, 1000, 15, 5, 1000);

			// TODO with the spatial reference assigned, the intersection contains only one point (0,5)
			// without spatial reference, it contains two points (0,5 and 15,5)
			//g1.SpatialReference = _spatialReference;
			//g2.SpatialReference = _spatialReference;

			IList<IGeometry> result12 = matrix.GetIntersections(g1, g2);

			Assert.AreEqual(1, result12.Count);
			Console.WriteLine(GeometryUtils.ToString(result12[0]));
			Assert.AreEqual(2, GeometryUtils.GetPointCount(result12));
			//Assert.IsTrue(SpatialReferenceUtils.AreEqual(_spatialReference,
			//                                             result12[0].SpatialReference));

			IList<IGeometry> result21 = matrix.GetIntersections(g2, g1);
			Assert.AreEqual(1, result21.Count);
			Console.WriteLine(GeometryUtils.ToString(result21[0]));
			Assert.IsTrue(
				GeometryUtils.AreEqualInXY(result21[0],
				                           g2)); // can't subtract, same as g2
		}

		[Test]
		public void LearningTestCanSubtractPointFromEqualPoint()
		{
			IPoint g1 = GeometryFactory.CreatePoint(10, 5, 1000);
			IPoint g2 = GeometryFactory.CreatePoint(10, 5, 1000);

			IGeometry result = ((ITopologicalOperator) g1).Difference(g2);

			Console.WriteLine(GeometryUtils.ToString(result));
			Assert.IsTrue(result.IsEmpty);
		}

		[Test]
		public void LearningTestCanSubtractPointFromNonEqualPoint()
		{
			IPoint g1 = GeometryFactory.CreatePoint(10, 5, 1000);
			IPoint g2 = GeometryFactory.CreatePoint(11, 5, 1000);

			IGeometry result = ((ITopologicalOperator) g1).Difference(g2);

			Console.WriteLine(GeometryUtils.ToString(result));
			Assert.IsFalse(result.IsEmpty);
			Assert.IsTrue(GeometryUtils.AreEqual(g1, result));
		}

		[Test]
		public void LearningTestCantSubtractMultipointFromPoint()
		{
			IPoint g1 = GeometryFactory.CreatePoint(10, 5, 1000);
			IMultipoint g2 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(10, 5, 1000),
				GeometryFactory.CreatePoint(10, 6, 1000));

			((ITopologicalOperator2) g2).IsKnownSimple_2 = false;
			((ITopologicalOperator) g2).Simplify();

			try
			{
				IGeometry result = ((ITopologicalOperator) g1).Difference(g2);
				if (RuntimeUtils.Is10_4orHigher)
				{
					Assert.True(result.IsEmpty, "empty result expected");
				}
				else
				{
					Assert.Fail("expected: AccessViolationException");
				}
			}
			catch (AccessViolationException)
			{
				if (RuntimeUtils.Is10_4orHigher)
				{
					// no longer expected >= 10.4
					throw;
				}

				// else: expected
			}
		}

		[Test]
		public void LearningTestCantSubtractMultipointFromMultipoint()
		{
			IMultipoint g1 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(11, 5, 1000),
				GeometryFactory.CreatePoint(11, 6, 1000));
			IMultipoint g2 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(10, 5, 1000),
				GeometryFactory.CreatePoint(10, 6, 1000));

			IGeometry result = ((ITopologicalOperator) g1).Difference(g2);

			Console.WriteLine(GeometryUtils.ToString(result));
			Assert.IsFalse(result.IsEmpty);
			Assert.IsTrue(GeometryUtils.AreEqual(g1, result));
		}

		[Test]
		public void LearningTestCantSubtractMultipointFromEqualMultipoint()
		{
			IMultipoint g1 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(10, 5, 1000),
				GeometryFactory.CreatePoint(10, 6, 1000));
			IMultipoint g2 = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(10, 5, 1000),
				GeometryFactory.CreatePoint(10, 6, 1000));

			IGeometry result = ((ITopologicalOperator) g1).Difference(g2);

			Console.WriteLine(GeometryUtils.ToString(result));
			Assert.IsTrue(result.IsEmpty);
		}

		[Test]
		public void LearningTestCanSubtractPointFromMultipoint()
		{
			IPoint p1 = GeometryFactory.CreatePoint(10, 5, 1000);
			IPoint p2 = GeometryFactory.CreatePoint(10, 6, 1000);

			IMultipoint g1 = GeometryFactory.CreateMultipoint(p1, p2);

			IPoint g2 = GeometryFactory.Clone(p2);

			IGeometry result = ((ITopologicalOperator) g1).Difference(g2);

			Console.WriteLine(@"result:");
			Console.WriteLine(GeometryUtils.ToString(result));
			Console.WriteLine(@"p1:");
			Console.WriteLine(GeometryUtils.ToString(p1));

			Assert.IsFalse(result.IsEmpty);
			var resultPoints = (IPointCollection) result;
			Assert.IsTrue(resultPoints.PointCount == 1);
			Assert.IsTrue(GeometryUtils.AreEqual(resultPoints.Point[0], p1));
		}

		[Test]
		public void CantCreateInvalidCell()
		{
			const string invalidMatrix = "TF0123***";

			try
			{
				new IntersectionMatrix(invalidMatrix);
				Assert.Fail("Exception expected");
			}
			catch (ArgumentOutOfRangeException)
			{
				// expected
			}
		}

		[Test]
		public void CantCreateInvalidLength()
		{
			const string invalidMatrix = "TF012*****";

			try
			{
				new IntersectionMatrix(invalidMatrix);
				Assert.Fail("Exception expected");
			}
			catch (ArgumentException)
			{
				// expected
			}
		}

		private static void WriteGeometries(IEnumerable<IGeometry> geometries)
		{
			foreach (IGeometry intersection in geometries)
			{
				Console.WriteLine(GeometryUtils.ToString(intersection));
			}
		}
	}
}

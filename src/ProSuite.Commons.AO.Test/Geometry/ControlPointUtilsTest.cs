using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class ControlPointUtilsTest
	{
		#region Setup/Teardown

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

		#endregion

		[Test]
		public void CanCountControlPoints()
		{
			const int any = 0;
			const int value = 123;

			IPolygon polygon = CreateMultipartPolygon();
			Assert.AreEqual(0, ControlPointUtils.CountControlPoints(polygon, any));
			Assert.AreEqual(0, ControlPointUtils.CountControlPoints(polygon, value));
			Assert.IsFalse(ControlPointUtils.HasControlPoints(polygon));

			ControlPointUtils.SetControlPoint(polygon, 0, 0, value);
			Assert.AreEqual(1, ControlPointUtils.CountControlPoints(polygon, any));
			Assert.AreEqual(1, ControlPointUtils.CountControlPoints(polygon, value));
			Assert.AreEqual(0, ControlPointUtils.CountControlPoints(polygon, value + 1));
			Assert.IsTrue(ControlPointUtils.HasControlPoints(polygon));

			ControlPointUtils.SetControlPoint(polygon, 1, 2, value);
			Assert.AreEqual(2, ControlPointUtils.CountControlPoints(polygon, any));
			Assert.AreEqual(2, ControlPointUtils.CountControlPoints(polygon, value));
			Assert.AreEqual(0, ControlPointUtils.CountControlPoints(polygon, value + 1));
			Assert.IsTrue(ControlPointUtils.HasControlPoints(polygon));
		}

		[Test]
		public void CanGetAndSetControlPoint()
		{
			IPolygon polygon = CreateMultipartPolygon();

			// CP at vertex#1 on part#0: initially zero
			Assert.AreEqual(0, ControlPointUtils.GetControlPoint(polygon, 0, 1));
			ControlPointUtils.SetControlPoint(polygon, 0, 1, 1);
			Assert.AreEqual(1, ControlPointUtils.GetControlPoint(polygon, 0, 1));

			// Set CP at FromPoint of part#0:
			ControlPointUtils.SetControlPoint(polygon, 0, 0, 2);
			Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0, 0));
			// Setting FromPoint also sets ToPoint on a closed geometry:
			Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0, 4));

			// Set CP at ToPoint of part#1:
			ControlPointUtils.SetControlPoint(polygon, 1, 4, 3);
			Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 1, 4));
			// Setting ToPoint also sets FromPoint on a closed geometry:
			Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 1, 0));

			// We have now 3 CPs, two on part#0 and one on part#1:
			int reset = ControlPointUtils.ResetControlPoints(polygon, 1);
			Assert.AreEqual(1, reset);
			reset = ControlPointUtils.ResetControlPoints(polygon); // all values
			Assert.AreEqual(2, reset);
		}

		[Test]
		public void CanNonPointIDAware()
		{
			IPolygon polygon = CreateMultipartPolygon();

			var idAware = GeometryUtils.IsPointIDAware(polygon);
			Assert.False(idAware);

			Assert.AreEqual(0, ControlPointUtils.GetControlPoint(polygon, 1, 1));
			Assert.AreEqual(0, ControlPointUtils.GetControlPoint(polygon, 2));

			Assert.False(ControlPointUtils.HasControlPoints(polygon));
			Assert.AreEqual(0, ControlPointUtils.CountControlPoints(polygon));

			Assert.AreEqual(0, ControlPointUtils.ResetControlPoints(polygon));
			Assert.AreEqual(0, ControlPointUtils.ResetControlPointPairs(polygon));

			// Setting a Control Point makes the geometry ID aware
			const int value = 99;
			Assert.False(GeometryUtils.IsPointIDAware(polygon));
			ControlPointUtils.SetControlPoint(polygon, 2, value);
			Assert.True(GeometryUtils.IsPointIDAware(polygon));
			Assert.AreEqual(value, ControlPointUtils.GetControlPoint(polygon, 2));
			Assert.AreEqual(1, ControlPointUtils.CountControlPoints(polygon));
			Assert.True(ControlPointUtils.HasControlPoints(polygon));
		}

		[Test]
		public void PreservesNonLinearSegments()
		{
			IPolygon polygon = CreateMultipartPolygon();
			var segments = (ISegmentCollection) polygon;

			// Set a CP between the circular (5) and bezier (6) segments on part#1:
			ControlPointUtils.SetControlPoint(polygon, 1, 2, 1);

			// Setting the CP must not have destroyed the segments!
			var type5 = segments.get_Segment(5).GeometryType;
			var type6 = segments.get_Segment(6).GeometryType;
			Assert.AreEqual(esriGeometryType.esriGeometryBezier3Curve, type5);
			Assert.AreEqual(esriGeometryType.esriGeometryCircularArc, type6);

			// Set a CP at "same" place on part#0 using "global index" method:
			ControlPointUtils.SetControlPoint(polygon, 2, 1);

			// Of course, this mustn't destroy segments neither!
			var type1 = segments.get_Segment(1).GeometryType;
			var type2 = segments.get_Segment(2).GeometryType;
			Assert.AreEqual(esriGeometryType.esriGeometryCircularArc, type1);
			Assert.AreEqual(esriGeometryType.esriGeometryBezier3Curve, type2);
		}

		[Test]
		public void CanUseGlobalVertexIndex()
		{
			IPolygon polygon = CreateMultipartPolygon();

			// Set CP at vertex#7 (which is vertex#2 of part#1):
			ControlPointUtils.SetControlPoint(polygon, 7, 1);
			Assert.AreEqual(1, ControlPointUtils.GetControlPoint(polygon, 7));

			// Retrieve through part and local index:
			Assert.AreEqual(1, ControlPointUtils.GetControlPoint(polygon, 1, 2));

			// And test a few boundary cases:

			// Set CP on first = last vertex of part#0:
			ControlPointUtils.SetControlPoint(polygon, 0, 2);
			Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0));
			Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 4));

			// Retrieve through part and local index:
			Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0, 0));
			Assert.AreEqual(2, ControlPointUtils.GetControlPoint(polygon, 0, 4));

			// Set CP on first = last vertex of part#1:
			ControlPointUtils.SetControlPoint(polygon, 5, 3);
			Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 5));
			Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 9));

			// Retrieve through part and local index:
			Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 1, 0));
			Assert.AreEqual(3, ControlPointUtils.GetControlPoint(polygon, 1, 4));
		}

		[Test]
		public void EnumVertexTest()
		{
			int partIndex, vertexIndex;
			IPoint vertex = new PointClass();

			IPolygon polygon = CreateMultipartPolygon();
			var vertices = (IPointCollection) polygon;
			var enumVertex = vertices.EnumVertices;

			int vertexCount = vertices.PointCount;

			int currentVertex = 0;
			enumVertex.Reset();

			while (currentVertex < vertexCount)
			{
				enumVertex.QueryNext(vertex, out partIndex, out vertexIndex);
				bool lastInPart = enumVertex.IsLastInPart();

				Console.WriteLine(@"QueryNext: P {0}, V {1}, lastInPart={2}", partIndex,
				                  vertexIndex, lastInPart);
				currentVertex += 1;
			}

			// QueryNext() after last gives indices of -1 (by experiment):
			enumVertex.QueryNext(vertex, out partIndex, out vertexIndex);
			Assert.IsTrue(partIndex < 0);
			Assert.IsTrue(vertexIndex < 0);

			currentVertex = 0;
			enumVertex.Reset();

			while (currentVertex < vertexCount)
			{
				enumVertex.Next(out vertex, out partIndex, out vertexIndex);
				bool lastInPart = enumVertex.IsLastInPart();

				Console.WriteLine(@"Next: P {0}, V {1}, lastInPart={2}", partIndex, vertexIndex,
				                  lastInPart);
				currentVertex += 1;
			}

			// Next() after last gives null and indices of -1 (by experiment):
			enumVertex.Next(out vertex, out partIndex, out vertexIndex);
			Assert.IsNull(vertex);
			Assert.IsTrue(partIndex < 0);
			Assert.IsTrue(vertexIndex < 0);
		}

		/// <summary>
		/// Create a two-part polygon according to the sketch below.
		/// Each ring consists of four segments; segment AB is linear,
		/// segment BC is a clockwise half-circle around center M,
		/// segment CD is a bezier curve with control points P and Q,
		/// and segment DA is the result of ring.Close().
		/// <code>
		/// 20 B----M----C
		/// 15 |  b-m-c  |
		/// 10 |  |   |  |P
		///  5 |  a---d Q|
		///  0 A---------D
		///    0   10   20
		/// </code>
		/// </summary>
		/// <remarks>
		/// Exterior rings are clockwise; interior rings are counterclockwise.
		/// </remarks>
		private static IPolygon CreateMultipartPolygon()
		{
			IPoint A = new PointClass();
			IPoint B = new PointClass();
			IPoint C = new PointClass();
			IPoint D = new PointClass();
			IPoint M = new PointClass();
			IPoint P = new PointClass();
			IPoint Q = new PointClass();

			A.PutCoords(0, 0);
			B.PutCoords(0, 20);
			C.PutCoords(20, 20);
			D.PutCoords(20, 0);
			M.PutCoords(10, 20);
			P.PutCoords(25, 10);
			Q.PutCoords(15, 5);

			var AB = new LineClass();
			AB.PutCoords(A, B);

			var BC = new CircularArcClass();
			BC.PutCoords(M, B, C, esriArcOrientation.esriArcClockwise);

			var CD = new BezierCurveClass();
			var bezierPoints = new[] {C, P, Q, D};
			((IBezierCurveGEN) CD).PutCoords(ref bezierPoints);

			object missing = Type.Missing;

			var part0 = new RingClass();
			part0.AddSegment(AB, ref missing, ref missing);
			part0.AddSegment(BC, ref missing, ref missing);
			part0.AddSegment(CD, ref missing, ref missing);
			part0.Close();

			Assert.IsTrue(part0.IsClosed);

			var part1 = (IRing) ((IClone) part0).Clone();
			((ITransform2D) part1).Scale(A, 0.5, 0.5);
			((ITransform2D) part1).Move(5, 5);
			part1.ReverseOrientation();
			Assert.IsTrue(part1.IsClosed);
			Assert.AreEqual(5, part1.FromPoint.X);
			Assert.AreEqual(5, part1.FromPoint.Y);

			var polygon = new PolygonClass();
			polygon.AddGeometry(part0, ref missing, ref missing);
			polygon.AddGeometry(part1, ref missing, ref missing);

			// Simplify or ExteriorRingCount throws!
			// Preserve From/To to be faithful to our ASCII sketch!
			polygon.SimplifyPreserveFromTo();

			Assert.AreEqual(1, polygon.ExteriorRingCount);
			Assert.AreEqual(2, polygon.GeometryCount);

			return polygon;
		}
	}
}

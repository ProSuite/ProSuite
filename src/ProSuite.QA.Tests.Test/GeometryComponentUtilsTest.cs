using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class GeometryComponentUtilsTest
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
		public void CanGetInteriorVerticesFrom2PointLine()
		{
			var polyLine3 = GeometryFactory.CreatePolyline(
				new[]
				{
					new WKSPointZ { X = 100, Y = 1000, Z = 10 },
					new WKSPointZ { X = 200, Y = 2000, Z = 20 }
				}, null);

			var points =
				GeometryComponentUtils.GetGeometryComponent(
					polyLine3, GeometryComponent.InteriorVertices);
			Assert.IsNotNull(points);
			Assert.IsTrue(points.IsEmpty);
		}

		[Test]
		public void CanGetInteriorVerticesFrom3PointLine()
		{
			var polyLine3 = GeometryFactory.CreatePolyline(
				new[]
				{
					new WKSPointZ { X = 100, Y = 1000, Z = 10 },
					new WKSPointZ { X = 200, Y = 2000, Z = 20 },
					new WKSPointZ { X = 300, Y = 3000, Z = 30 }
				}, null);

			var points =
				(IPointCollection) GeometryComponentUtils.GetGeometryComponent(
					polyLine3, GeometryComponent.InteriorVertices);

			Assert.IsNotNull(points);
			Assert.AreEqual(1, points.PointCount);
			Assert.AreEqual(200, points.Point[0].X);
			Assert.AreEqual(2000, points.Point[0].Y);
			Assert.AreEqual(20, points.Point[0].Z);
		}

		[Test]
		public void CanGetComponentsOfEmpty()
		{
			var line = new PolylineClass();
			var polygon = new PolygonClass();
			var point = new PointClass();
			var multiPoint = new MultipointClass();
			var multiPatch = new MultiPatchClass();

			AssertEmpty(GeometryComponent.Boundary, polygon);
			AssertEmpty(GeometryComponent.Boundary, line);
			AssertEmpty(GeometryComponent.Boundary, point);
			AssertEmpty(GeometryComponent.Boundary, multiPatch);

			AssertEmpty(GeometryComponent.Centroid, polygon);
			AssertEmpty(GeometryComponent.Centroid, multiPatch);

			AssertEmpty(GeometryComponent.EntireGeometry, polygon);
			AssertEmpty(GeometryComponent.EntireGeometry, line);
			AssertEmpty(GeometryComponent.EntireGeometry, point);
			AssertEmpty(GeometryComponent.EntireGeometry, multiPoint);
			AssertEmpty(GeometryComponent.EntireGeometry, multiPatch);

			AssertEmpty(GeometryComponent.InteriorVertices, line);

			AssertEmpty(GeometryComponent.LabelPoint, polygon);
			AssertEmpty(GeometryComponent.LabelPoint, multiPatch);

			AssertEmpty(GeometryComponent.LineEndPoint, line);

			AssertEmpty(GeometryComponent.LineStartPoint, line);

			AssertEmpty(GeometryComponent.Vertices, polygon);
			AssertEmpty(GeometryComponent.Vertices, line);
			AssertEmpty(GeometryComponent.Vertices, point);
			AssertEmpty(GeometryComponent.Vertices, multiPoint);
			AssertEmpty(GeometryComponent.Vertices, multiPatch);
		}

		private static void AssertEmpty(GeometryComponent geometryComponent,
		                                IGeometry geometry)
		{
			var component = GeometryComponentUtils.GetGeometryComponent(geometry,
				geometryComponent);

			Assert.NotNull(component);
			Assert.IsTrue(component.IsEmpty);
		}
	}
}

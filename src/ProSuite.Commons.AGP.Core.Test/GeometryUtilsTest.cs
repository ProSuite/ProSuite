using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class GeometryUtilsTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanPolygonBoundaryNull()
		{
			Assert.Null(GeometryUtils.Boundary(null));
		}

		[Test]
		public void CanPolygonBoundary()
		{
			var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 50);
			var polygon = GeometryFactory.CreatePolygon(envelope);
			var boundary = GeometryUtils.Boundary(polygon);
			Assert.NotNull(boundary);
			Assert.AreEqual(GeometryType.Polyline, boundary.GeometryType);
			Assert.AreEqual(polygon.PointCount, boundary.PointCount);
		}

		[Test]
		public void CanPolygonBoundaryPreserveCurves()
		{
			var polygon = GeometryFactory.CreateBezierCircle();
			Assert.True(polygon.HasCurves, "Oops, original polygon has no curves");
			var boundary = GeometryUtils.Boundary(polygon);

			Assert.NotNull(boundary);
			Assert.AreEqual(GeometryType.Polyline, boundary.GeometryType);
			Assert.True(boundary.HasCurves, "did not preserve curves");
			Assert.AreEqual(polygon.PointCount, boundary.PointCount);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test
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

		[Test]
		public void Can_get_nearest_vertex()
		{
			var coords = new List<MapPoint>
			             {
				             MapPointBuilder.CreateMapPoint(140, 0, SpatialReferences.WebMercator),
				             MapPointBuilder.CreateMapPoint(160, 0, SpatialReferences.WebMercator),
				             MapPointBuilder.CreateMapPoint(175, 0, SpatialReferences.WebMercator),
				             MapPointBuilder.CreateMapPoint(
					             -175, 10, SpatialReferences.WebMercator),
				             MapPointBuilder.CreateMapPoint(
					             -145, 10, SpatialReferences.WebMercator),
				             MapPointBuilder.CreateMapPoint(-125, 10, SpatialReferences.WebMercator)
			             };

			//var coords = new List<MapPoint>
			//			 {
			//				 MapPointBuilder.CreateMapPoint(140, 0, SpatialReferences.WebMercator),
			//				 MapPointBuilder.CreateMapPoint(160, 0, SpatialReferences.WebMercator),
			//				 MapPointBuilder.CreateMapPoint(175, 0, SpatialReferences.WebMercator),
			//				 MapPointBuilder.CreateMapPoint(185, 10, SpatialReferences.WebMercator),
			//				 MapPointBuilder.CreateMapPoint(215, 10, SpatialReferences.WebMercator),
			//				 MapPointBuilder.CreateMapPoint(225, 10, SpatialReferences.WebMercator)
			//			 };

			Polyline line =
				PolylineBuilder.CreatePolyline(coords);

			Polyline dateline =
				PolylineBuilder.CreatePolyline(new List<MapPoint>
				                               {
					                               MapPointBuilder.CreateMapPoint(
						                               180, 90, SpatialReferences.WebMercator),
					                               MapPointBuilder.CreateMapPoint(
						                               180, -90, SpatialReferences.WebMercator)
				                               });

			Geometry intersection =
				GeometryEngine.Instance.Intersection(line, dateline,
				                                     GeometryDimension.esriGeometry0Dimension);

			var multipoint = intersection as Multipoint;

			if (multipoint != null)
			{
				for (var i = 0; i < multipoint.PointCount; i++)
				{
					MapPoint point = multipoint.Points[i];
					ProximityResult result =
						GeometryEngine.Instance.NearestVertex(intersection, point);

					MapPoint resultPoint = result.Point;

					Console.WriteLine($"x: {resultPoint.X}");
					Console.WriteLine($"y: {resultPoint.Y}");
				}
			}
		}
	}
}

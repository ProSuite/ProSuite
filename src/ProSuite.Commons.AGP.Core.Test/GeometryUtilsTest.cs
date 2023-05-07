using System;
using System.Collections.Generic;
using System.Linq;
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
		public void Can_get_distance_between_geometries()
		{
			var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
			var polygon = GeometryFactory.CreatePolygon(envelope);
			var mapPoint = MapPointBuilder.CreateMapPoint(50, 50);

			double distance = GeometryEngine.Instance.Distance(polygon, mapPoint);
			Assert.AreEqual(0, distance);
			Assert.False(GeometryUtils.Disjoint(polygon, mapPoint));

			ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);
			Assert.AreEqual(50, result.Distance);

			mapPoint = MapPointBuilder.CreateMapPoint(110, 100);

			distance = GeometryEngine.Instance.Distance(polygon, mapPoint);
			Assert.AreEqual(10, distance);
			Assert.True(GeometryUtils.Disjoint(polygon, mapPoint));
		}

		[Test]
		public void Can_get_nearest_point_to_geometry__point_inside_geometry()
		{
			var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
			var polygon = GeometryFactory.CreatePolygon(envelope);
			var mapPoint = MapPointBuilder.CreateMapPoint(75, 50);

			ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);

			Assert.NotNull(result);
			Assert.AreEqual(25, result.Distance);
			Assert.AreEqual(0, result.PartIndex);
			Assert.AreEqual(100, result.Point.X);
			Assert.AreEqual(50, result.Point.Y);
			Assert.True(result.RightSide);
		}

		[Test]
		public void Can_get_nearest_point_to_geometry__point_outside_geometry()
		{
			var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
			var polygon = GeometryFactory.CreatePolygon(envelope);
			var mapPoint = MapPointBuilder.CreateMapPoint(110, 100);

			ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);

			Assert.NotNull(result);
			Assert.AreEqual(10, result.Distance);
			Assert.AreEqual(0, result.PartIndex);
			Assert.AreEqual(100, result.Point.X);
			Assert.AreEqual(100, result.Point.Y);
			Assert.False(result.RightSide);
		}

		[Test]
		public void Can_get_nearest_point_to_geometry__point_on_geometry()
		{
			var envelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);
			var polygon = GeometryFactory.CreatePolygon(envelope);
			var mapPoint = MapPointBuilder.CreateMapPoint(100, 50);

			ProximityResult result = GeometryEngine.Instance.NearestPoint(polygon, mapPoint);

			Assert.NotNull(result);
			Assert.AreEqual(0, result.Distance);
			Assert.AreEqual(0, result.PartIndex);
			Assert.AreEqual(100, result.Point.X);
			Assert.AreEqual(50, result.Point.Y);
			Assert.False(result.RightSide);
		}

		[Test]
		public void CheckLineSegmentAngle()
		{
			// About LineSegment.Angle property:
			// Documentation: angle in radians, ccw from positive x axis.
			// Empirical: angle is in range -pi..pi (not 0..2pi).

			var start = MapPointBuilder.CreateMapPoint(0, 0);
			var end = MapPointBuilder.CreateMapPoint(5, -5);
			var builder = new LineBuilder(start, end);
			var line = builder.ToSegment();

			const double delta = 0.000001;
			var angle = line.Angle;
			Assert.AreEqual(-Math.PI / 4, angle, delta);
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
		public void CanDisjointEmpty()
		{
			var point = MapPointBuilderEx.CreateMapPoint(1.0, 1.0);
			var empty = PolygonBuilderEx.CreatePolygon();

			Assert.False(point.IsEmpty);
			Assert.True(empty.IsEmpty);

			Assert.True(GeometryUtils.Disjoint(point, empty));
			Assert.True(GeometryUtils.Disjoint(empty, point));
		}

		[Test]
		public void CanConnectedComponents()
		{
			var empty = PolygonBuilderEx.CreatePolygon();
			Assert.True(empty.IsEmpty);

			var r0 = GeometryUtils.ConnectedComponents(empty);
			Assert.NotNull(r0);
			Assert.IsEmpty(r0);

			var square = CreateSquarePolygon(); // one (exterior) ring
			Assert.AreEqual(1, square.PartCount);

			var r1 = GeometryUtils.ConnectedComponents(square);
			Assert.NotNull(r1);
			Assert.AreEqual(1, r1.Count);
			Assert.AreEqual(1, r1.Single().PartCount);

			var polygon = CreateMultiPolygon();
			Assert.AreEqual(7, polygon.PartCount);

			var r2 = GeometryUtils.ConnectedComponents(polygon);
			Assert.NotNull(r2);
			Assert.AreEqual(4, r2.Count);
			var l2 = r2.OrderBy(p => p.Area).ToList();
			Assert.AreEqual(1, l2[0].PartCount);
			Assert.AreEqual(1, l2[1].PartCount);
			Assert.AreEqual(2, l2[2].PartCount);
			Assert.AreEqual(3, l2[3].PartCount);
		}

		[Test]
		public void CanRemoveHoles()
		{
			var empty = PolygonBuilderEx.CreatePolygon();
			Assert.True(empty.IsEmpty);

			var r0 = GeometryUtils.RemoveHoles(empty);
			Assert.NotNull(r0);
			Assert.True(r0.IsEmpty);
			Assert.AreEqual(0, r0.PartCount);

			var poly = CreateMultiPolygon();

			var r1 = GeometryUtils.RemoveHoles(poly);
			Assert.NotNull(r1);
			Assert.False(r1.IsEmpty);
			Assert.AreEqual(3, r1.PartCount);
			Assert.AreEqual(15, r1.PointCount);
			Assert.AreEqual(9 + 35 + 1, r1.Area, 0.001);
		}

		#region Creating test geometries

		private static Polygon CreateSquarePolygon()
		{
			var builder = new PolygonBuilderEx();

			builder.AddPart(MakeCoords(0, 0, 0, 1, 1, 1, 1, 0, 0, 0));

			return (Polygon) builder.ToGeometry();
		}

		private static Polygon CreateMultiPolygon()
		{
			// 5 . . . . . # # # # # # # . . .
			// 4 . # # # . # . . . # # # . . .
			// 3 . # . # . # . # . # . # . # .
			// 2 . # # # . # . . . # # # . . .
			// 1 . . . . . # # # # # # # . . .
			// 0 . . . . . . . . . . . . . . .
			//   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4

			var builder = new PolygonBuilderEx();

			builder.AddPart(MakeCoords(1, 2,  1, 5,  4, 5,  4, 2,  1, 2)); // outer
			builder.AddPart(MakeCoords(2, 3,  3, 3,  3, 4,  2, 4,  2, 3)); // inner

			builder.AddPart(MakeCoords(5, 1, 5, 6, 12, 6, 12, 1, 5, 1)); // outer
			builder.AddPart(MakeCoords(6, 2, 9, 2, 9, 5, 6, 5, 6, 2)); // inner
			builder.AddPart(MakeCoords(7, 3, 7, 4, 8, 4, 8, 3, 7, 3)); // outer in inner
			builder.AddPart(MakeCoords(10, 3, 11, 3, 11, 4, 10, 4, 10, 3)); // inner

			builder.AddPart(MakeCoords(13, 3,  13, 4,  14, 4,  14, 3,  13, 3)); // outer

			return (Polygon) builder.ToGeometry();
		}

		private static IEnumerable<Coordinate2D> MakeCoords(params double[] coords)
		{
			for (int i = 1; i < coords.Length; i += 2)
			{
				yield return new Coordinate2D(coords[i - 1], coords[i]);
			}
		}

		#endregion

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

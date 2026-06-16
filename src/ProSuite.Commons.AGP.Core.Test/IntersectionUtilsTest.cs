using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test
{
	[TestFixture]
	public class IntersectionUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void
			CanGetIntersectionPointsForMultipartLinesAlmostTouchingPolyBoundaryFromInside()
		{
			// such situations were problematic in the past
			SpatialReference sref = SpatialReferenceBuilder.CreateSpatialReference(2056); // LV95
			double resolution = sref.XYResolution;

			Polygon polygon = PolygonBuilderEx.CreatePolygon(new[]
			                                                 {
				                                                 MapPointBuilderEx.CreateMapPoint(
					                                                 100, 100, sref),
				                                                 MapPointBuilderEx.CreateMapPoint(
					                                                 200, 100, sref),
				                                                 MapPointBuilderEx.CreateMapPoint(
					                                                 200, 200, sref),
				                                                 MapPointBuilderEx.CreateMapPoint(
					                                                 100, 200, sref),
				                                                 MapPointBuilderEx.CreateMapPoint(
					                                                 100, 100, sref),
			                                                 }, sref);
			polygon = GeometryUtils.Simplify(polygon);

			Polyline polyline = PolylineBuilderEx.CreatePolyline(new[]
			                                                     {
				                                                     MapPointBuilderEx
					                                                     .CreateMapPoint(
						                                                     150,
						                                                     200 - 2 * resolution,
						                                                     sref),
				                                                     MapPointBuilderEx
					                                                     .CreateMapPoint(
						                                                     150, 150, sref),
				                                                     MapPointBuilderEx
					                                                     .CreateMapPoint(
						                                                     150, 50, sref),
			                                                     }, AttributeFlags.None, sref);
			polyline = GeometryUtils.Simplify(polyline);

			Polyline polygonBoundary = GeometryUtils.Boundary(polygon);

			Multipoint result = IntersectionUtils.GetIntersectionPoints(
				polyline, polygonBoundary, true,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			Assert.AreEqual(2, result.PointCount);

			// now try multi-part line
			var builder = new PolylineBuilderEx(polyline);
			builder.AddPart(new[]
			                {
				                MapPointBuilderEx.CreateMapPoint(160, 150, sref),
				                MapPointBuilderEx.CreateMapPoint(250, 150, sref),
				                MapPointBuilderEx.CreateMapPoint(250, 350, sref),
			                });
			polyline = builder.ToGeometry();

			result = IntersectionUtils.GetIntersectionPoints(
				polyline, polygonBoundary, true,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			Assert.AreEqual(3, result.PointCount);
		}
	}
}

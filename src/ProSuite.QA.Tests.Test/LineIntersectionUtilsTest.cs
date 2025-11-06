using System;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class LineIntersectionUtilsTest
	{
		private IPoint _pointTemplate1;
		private IPoint _pointTemplate2;
		private const double _tolerance = 0.01234567;
		private const double _resolution = 0.001234567;

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

		[SetUp]
		public void Setup()
		{
			_pointTemplate1 = new PointClass();
			_pointTemplate2 = new PointClass();
		}

		[Test]
		public void CanDetectEndPointSegmentIntersection()
		{
			IPolyline polyline1 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(100, 100));
			IPolyline polyline2 = CreatePolyline(
				CreatePoint(100, 0),
				CreatePoint(50.001, 50));

			const bool reportOverlaps = false;
			Assert.IsTrue(HasInvalidIntersection(polyline1, polyline2,
			                                     AllowedEndpointInteriorIntersections.None,
			                                     reportOverlaps,
			                                     _tolerance));

			IMultipoint intersections = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				AllowedEndpointInteriorIntersections.None,
				AllowedLineInteriorIntersections.None,
				reportOverlaps,
				_tolerance);

			Assert.AreEqual(1, GeometryUtils.GetPointCount(intersections));
			AssertPoint(intersections, 0, CreatePoint(50.001, 50));
		}

		[Test]
		public void CanDetectLinearIntersection()
		{
			IPolyline polyline1 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(100, 100));
			IPolyline polyline2 = CreatePolyline(
				CreatePoint(100, 0),
				CreatePoint(49, 49),
				CreatePoint(51, 51),
				CreatePoint(0, 100));

			const bool reportOverlaps = true;
			Assert.IsTrue(HasInvalidIntersection(polyline1, polyline2,
			                                     AllowedEndpointInteriorIntersections.None,
			                                     reportOverlaps,
			                                     _tolerance));

			IMultipoint intersections = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				AllowedEndpointInteriorIntersections.None,
				AllowedLineInteriorIntersections.None,
				reportOverlaps,
				_tolerance);

			Assert.AreEqual(2, GeometryUtils.GetPointCount(intersections));
			AssertPoint(intersections, 0, CreatePoint(49, 49));
			AssertPoint(intersections, 1, CreatePoint(51, 51));
		}

		[Test]
		public void CanDetectLinearIntersectionAndCrossing()
		{
			IPolyline polyline1 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(100, 100));
			IPolyline polyline2 = CreatePolyline(
				CreatePoint(100, 0),
				CreatePoint(49, 49),
				CreatePoint(51, 51),
				CreatePoint(0, 100),
				CreatePoint(10, 0));

			const bool reportOverlaps = true;
			Assert.IsTrue(HasInvalidIntersection(polyline1, polyline2,
			                                     AllowedEndpointInteriorIntersections.None,
			                                     reportOverlaps,
			                                     _tolerance));

			IMultipoint intersections = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				AllowedEndpointInteriorIntersections.None,
				AllowedLineInteriorIntersections.None,
				reportOverlaps,
				_tolerance);

			Assert.AreEqual(3, GeometryUtils.GetPointCount(intersections));
			AssertPoint(intersections, 0, CreatePoint(9.0918, 9.0916));
			AssertPoint(intersections, 1, CreatePoint(49, 49));
			AssertPoint(intersections, 2, CreatePoint(51, 51));
		}

		[Test]
		public void CanDetectEndPointSegmentIntersectionWithConnectedEndpoint1()
		{
			IPolyline polyline1 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(100, 100),
				CreatePoint(100, 0));
			IPolyline polyline2 = CreatePolyline(
				CreatePoint(100, 0),
				CreatePoint(50.001, 50));

			const bool reportOverlaps = false;
			Assert.IsTrue(HasInvalidIntersection(polyline1, polyline2,
			                                     AllowedEndpointInteriorIntersections.None,
			                                     reportOverlaps,
			                                     _tolerance));

			IMultipoint intersections = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				AllowedEndpointInteriorIntersections.None,
				AllowedLineInteriorIntersections.None,
				reportOverlaps,
				_tolerance);

			Assert.AreEqual(1, GeometryUtils.GetPointCount(intersections));
			AssertPoint(intersections, 0, CreatePoint(50.001, 50));
		}

		[Test]
		public void CanDetectEndPointSegmentIntersectionWithConnectedEndpoint2()
		{
			IPolyline polyline1 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(100, 100));
			IPolyline polyline2 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(100, 0),
				CreatePoint(50.001, 50));

			const bool reportOverlaps = false;
			Assert.IsTrue(HasInvalidIntersection(polyline1, polyline2,
			                                     AllowedEndpointInteriorIntersections.None,
			                                     reportOverlaps,
			                                     _tolerance));

			IMultipoint intersections = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				AllowedEndpointInteriorIntersections.None,
				AllowedLineInteriorIntersections.None,
				reportOverlaps,
				_tolerance);

			Assert.AreEqual(1, GeometryUtils.GetPointCount(intersections));
			AssertPoint(intersections, 0, CreatePoint(50.001, 50));
		}

		[Test]
		public void CanAllowEndPointVertexIntersection()
		{
			IPolyline polyline1 = CreatePolyline(
				CreatePoint(0, 0),
				CreatePoint(50, 50),
				CreatePoint(100, 100));
			IPolyline polyline2 = CreatePolyline(
				CreatePoint(100, 0),
				CreatePoint(50.005, 50));

			const bool reportOverlaps = false;
			Assert.IsFalse(HasInvalidIntersection(polyline1, polyline2,
			                                      AllowedEndpointInteriorIntersections.Vertex,
			                                      reportOverlaps,
			                                      _tolerance));
			IMultipoint intersections = LineIntersectionUtils.GetInvalidIntersections(
				polyline1, polyline2,
				AllowedEndpointInteriorIntersections.Vertex,
				AllowedLineInteriorIntersections.None,
				reportOverlaps,
				_tolerance);

			Assert.AreEqual(0, GeometryUtils.GetPointCount(intersections));
		}

		private bool HasInvalidIntersection(
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			bool reportOverlaps,
			double vertexSearchDistance)
		{
			return LineIntersectionUtils.HasInvalidIntersection(
				polyline1, polyline2,
				allowedEndpointInteriorIntersections,
				reportOverlaps,
				_pointTemplate1,
				_pointTemplate2,
				vertexSearchDistance,
				out IPoint _);
		}

		[NotNull]
		private static IPolyline CreatePolyline(
			[NotNull] IPoint fromPoint,
			[NotNull] IPoint secondPoint,
			params IPoint[] additionalPoints)
		{
			ISpatialReference spatialReference = CreateSpatialReference();
			IPolyline result = GeometryFactory.CreatePolyline(spatialReference, fromPoint,
			                                                  secondPoint, additionalPoints);
			GeometryUtils.Simplify(result);
			GeometryUtils.AllowIndexing(result);

			return result;
		}

		private static IPoint CreatePoint(double x, double y)
		{
			return GeometryFactory.CreatePoint(x, y);
		}

		private static ISpatialReference CreateSpatialReference()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, true);

			SpatialReferenceUtils.SetXYDomain(result, -100000, -100000, 100000, 100000,
			                                  _resolution, _tolerance);

			return result;
		}

		private static void AssertPoint(IMultipoint points, int index, IPoint expectedPoint)
		{
			expectedPoint.SpatialReference = CreateSpatialReference();
			expectedPoint.SnapToSpatialReference();

			IPoint point = ((IPointCollection) points).get_Point(index);
			point.SnapToSpatialReference();
			double t = SpatialReferenceUtils.GetXyTolerance(expectedPoint.SpatialReference);
			Assert.IsTrue(Math.Abs(expectedPoint.X - point.X) < t, $"point {index}, X");
			Assert.IsTrue(Math.Abs(expectedPoint.Y - point.Y) < t, $"point {index}, Y");
		}
	}
}

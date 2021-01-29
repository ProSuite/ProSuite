using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Tests.Test.Construction;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class TopologicalLineTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private const int _segmentPerformanceCount = 300000;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanGetSingleSegmentOrientationHorizontalRight()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 10)
			                                            .LineTo(20, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(2, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentOrientationHorizontalLeft()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(20, 10)
			                                            .LineTo(10, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-2, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentOrientationVerticalUp()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 10)
			                                            .LineTo(10, 20)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-2, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentOrientationVerticalDown()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 20)
			                                            .LineTo(10, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-2, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentOrientationToLR()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 10)
			                                            .LineTo(20, 0)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(2, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentOrientationToUL()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(20, 0)
			                                            .LineTo(10, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-2, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetClockwiseCircularArcOrientation()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(9, 10)
			                                            .LineTo(10, 10)
			                                            .CircleTo(10, 0)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetCounterClockwiseCircularArcOrientation()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(9, 0)
			                                            .LineTo(10, 0)
			                                            .CircleTo(10, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentClockwiseCircularArcOrientation()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(9, 10)
			                                            .LineTo(10, 10)
			                                            .CircleTo(10, 0)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetSingleSegmentCounterClockwiseCircularArcOrientation()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(9, 0)
			                                            .LineTo(10, 0)
			                                            .CircleTo(10, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetClockwiseOrientation()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 10)
			                                            .LineTo(15, 5)
			                                            .LineTo(10, 0)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetCounterClockwiseOrientation()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 0)
			                                            .LineTo(15, 5)
			                                            .LineTo(10, 10)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();

			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);

			Assert.AreEqual(-1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetOrientationsForLinearTopologicalLines()
		{
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 0)
			                                            .LineTo(15, 5)
			                                            .Curve;
			polyline.SpatialReference = CreateSpatialReference();
			TopologicalLine topologicalLine = CreateTopologicalLine(polyline);
			Assert.AreEqual(2, topologicalLine.Orientation);

			polyline = (IPolyline) CurveConstruction.StartLine(15, 5)
			                                        .LineTo(10, 0)
			                                        .Curve;
			polyline.SpatialReference = CreateSpatialReference();
			topologicalLine = CreateTopologicalLine(polyline);
			Assert.AreEqual(-2, topologicalLine.Orientation);

			polyline = (IPolyline) CurveConstruction.StartLine(10, 0)
			                                        .LineTo(15, 5)
			                                        .LineTo(10, 10)
			                                        .Curve;
			polyline.SpatialReference = CreateSpatialReference();
			topologicalLine = CreateTopologicalLine(polyline);
			Assert.AreEqual(-1, topologicalLine.Orientation);

			polyline = (IPolyline) CurveConstruction.StartLine(10, 10)
			                                        .LineTo(15, 5)
			                                        .LineTo(10, 0)
			                                        .Curve;
			polyline.SpatialReference = CreateSpatialReference();
			topologicalLine = CreateTopologicalLine(polyline);
			Assert.AreEqual(1, topologicalLine.Orientation);
		}

		[Test]
		public void CanGetOrientationsForLinearLines()
		{
			double yMax;
			const double resol = 0.0001;
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 0)
			                                            .LineTo(15, 5)
			                                            .Curve;
			Assert.AreEqual(2, TopologicalLineUtils.CalculateOrientation(polyline, resol,
			                                                             out yMax));

			polyline = (IPolyline) CurveConstruction.StartLine(15, 5)
			                                        .LineTo(10, 0)
			                                        .Curve;
			Assert.AreEqual(-2, TopologicalLineUtils.CalculateOrientation(polyline, resol,
			                                                              out yMax));

			polyline = (IPolyline) CurveConstruction.StartLine(10, 0)
			                                        .LineTo(15, 5)
			                                        .LineTo(10, 10)
			                                        .Curve;
			Assert.AreEqual(-1, TopologicalLineUtils.CalculateOrientation(
				                polyline, resol, out yMax));

			polyline = (IPolyline) CurveConstruction.StartLine(10, 10)
			                                        .LineTo(15, 5)
			                                        .LineTo(10, 0)
			                                        .Curve;
			Assert.AreEqual(1, TopologicalLineUtils.CalculateOrientation(
				                polyline, resol, out yMax));
		}

		[Test]
		public void CanGetOrientationsForNonLinearLines()
		{
			const double resol = 0.0001;
			double yMax;
			var polyline = (IPolyline) CurveConstruction.StartLine(10, 0)
			                                            .BezierTo(12, 2, 12, 4, 10, 6)
			                                            .Curve;
			Assert.AreEqual(-1, TopologicalLineUtils.CalculateOrientation(polyline, resol,
			                                                              out yMax));
		}

		[Test]
		public void SegmentLinearTangentPerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .LineTo(11, 15)
			                                   .Curve;
			ISegment segment = ((ISegmentCollection) line).Segment[0];
			ILine tangent = new LineClass();

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				segment.QueryTangent(esriSegmentExtension.esriExtendAtFrom, 0, false, 1, tangent);
			}
		}

		[Test]
		public void SegmentNonLinearTangentPerformance()
		{
			var line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                                 .BezierTo(11, 12, 11, 13, 12, 15)
			                                                 .Curve;
			ISegment segment = line.Segment[line.SegmentCount - 1];
			ILine tangent = new LineClass();

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				segment.QueryTangent(esriSegmentExtension.esriExtendAtFrom, 0, false, 1, tangent);
			}
		}

		[Test]
		public void SegmentsLinearToAnglePerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10).LineTo(11, 15).Curve;

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				double angle;
				TopologicalLineUtils.CalculateToAngle((ISegmentCollection) line, out angle);
			}
		}

		[Test]
		public void SegmentLinearToAnglePerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .LineTo(11, 15)
			                                   .Curve;

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				var segs = (ISegmentCollection) line;
				ISegment seg = segs.Segment[segs.SegmentCount - 1];
				double angle;
				TopologicalLineUtils.CalculateToAngle(seg, out angle);
			}
		}

		[Test]
		public void SegmentsNonLinearToAnglePerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .BezierTo(11, 12, 11, 13, 12, 15)
			                                   .Curve;

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				double angle;
				TopologicalLineUtils.CalculateToAngle((ISegmentCollection) line, out angle);
			}
		}

		[Test]
		public void SegmentNonLinearToAnglePerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .BezierTo(11, 12, 11, 13, 12, 15)
			                                   .Curve;

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				var segs = (ISegmentCollection) line;
				ISegment seg = segs.Segment[segs.SegmentCount - 1];
				double angle;
				TopologicalLineUtils.CalculateToAngle(seg, out angle);
			}
		}

		[Test]
		public void SegmentFromPointPerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .LineTo(11, 15)
			                                   .Curve;
			ISegment segment = ((ISegmentCollection) line).Segment[0];

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				double x, y;
				IPoint f = segment.FromPoint;
				f.QueryCoords(out x, out y);
			}
		}

		[Test]
		public void SegmentQueryFromPointPerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .LineTo(11, 15)
			                                   .Curve;
			ISegment segment = ((ISegmentCollection) line).Segment[0];
			IPoint p = new PointClass();
			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				double x, y;
				segment.QueryFromPoint(p);
				p.QueryCoords(out x, out y);
			}
		}

		[Test]
		public void SegmentWksFromPointPerformance()
		{
			IPolycurve line = CurveConstruction.StartLine(10, 10)
			                                   .LineTo(11, 15)
			                                   .Curve;
			ISegment segment = ((ISegmentCollection) line).Segment[0];

			for (var i = 0; i < _segmentPerformanceCount; i++)
			{
				WKSPoint p;
				segment.QueryWKSFromPoint(out p);
			}
		}

		[Test]
		public void CanGetSegmentsAngle()
		{
			var line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                                 .LineTo(11, 15)
			                                                 .Curve;
			double fromAngle;
			double toAngle;

			Assert.IsTrue(TopologicalLineUtils.CalculateFromAngle(line, out fromAngle));
			Assert.IsTrue(TopologicalLineUtils.CalculateToAngle(line, out toAngle));
			Assert.IsTrue(Math.Abs(fromAngle - toAngle - Math.PI) < 1e-12);

			line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                             .LineTo(11, 11)
			                                             .LineTo(12, 15)
			                                             .Curve;
			Assert.IsTrue(TopologicalLineUtils.CalculateFromAngle(line, out fromAngle));
			Assert.IsTrue(TopologicalLineUtils.CalculateToAngle(line, out toAngle));
			Assert.IsTrue(Math.Abs(fromAngle - toAngle - Math.PI) > 1e-12);

			line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                             .LineTo(10, 10)
			                                             .LineTo(11, 15)
			                                             .Curve;

			Assert.IsTrue(TopologicalLineUtils.CalculateFromAngle(line, out fromAngle));
			Assert.IsTrue(TopologicalLineUtils.CalculateToAngle(line, out toAngle));
			Assert.IsTrue(Math.Abs(fromAngle - toAngle - Math.PI) < 1e-12);

			line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                             .LineTo(10, 10)
			                                             .LineTo(10, 10)
			                                             .Curve;

			Assert.IsFalse(TopologicalLineUtils.CalculateFromAngle(line, out fromAngle));
			Assert.IsFalse(TopologicalLineUtils.CalculateToAngle(line, out toAngle));

			line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                             .BezierTo(10, 10, 11, 11, 11, 11)
			                                             .Curve;
			Assert.IsTrue(TopologicalLineUtils.CalculateFromAngle(line, out fromAngle));
			Assert.IsTrue(TopologicalLineUtils.CalculateToAngle(line, out toAngle));
			Assert.IsTrue(Math.Abs(fromAngle - toAngle - Math.PI) < 1e-12);

			line = (ISegmentCollection) CurveConstruction.StartLine(10, 10)
			                                             .BezierTo(11, 10, 12, 11, 12, 12)
			                                             .Curve;
			Assert.IsTrue(TopologicalLineUtils.CalculateFromAngle(line, out fromAngle));
			Assert.IsTrue(TopologicalLineUtils.CalculateToAngle(line, out toAngle));
			Assert.IsTrue(Math.Abs(fromAngle) < 1e-12);
			Assert.IsTrue(Math.Abs(toAngle + Math.PI / 2) < 1e-12);
		}

		[NotNull]
		private static TopologicalLine CreateTopologicalLine([NotNull] IPolyline polyline)
		{
			const bool hasZ = false;
			const bool hasM = false;
			var featureClassMock = new FeatureClassMock(1, "test_polylines",
			                                            esriGeometryType.esriGeometryPolyline,
			                                            esriFeatureType.esriFTSimple,
			                                            polyline.SpatialReference,
			                                            hasZ, hasM);
			IFeature feature = featureClassMock.CreateFeature(polyline);

			const int tableIndex = 0;
			return new TopologicalLine(new TableIndexRow(feature, tableIndex), -1);
		}

		private static ISpatialReference CreateSpatialReference()
		{
			const bool defaultXyDomain = true;
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, defaultXyDomain);

			SpatialReferenceUtils.SetXYDomain(spatialReference,
			                                  -10000, -10000, 10000, 10000,
			                                  0.001, 0.01);
			return spatialReference;
		}
	}
}

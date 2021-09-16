using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Collections;
using Array = System.Array;

namespace ProSuite.Commons.AO.Test.Geometry.ChangeAlong
{
	[TestFixture]
	public class ReshapeUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanCalculateZOnlyDifference_SimpleLine()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 20, 100, 0, 20);

			double zTolerance = 0.0125;
			ISpatialReference lv95 = CreateSpatialReference(0.0125, zTolerance);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			const int expectedLength = 100;

			VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
			                       zTolerance);
		}

		[Test]
		public void CanCalculateZOnlyDifference_SimpleLineNoDifference()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);

			double zTolerance = 0.0125;
			ISpatialReference lv95 = CreateSpatialReference(0.0125, zTolerance);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			const int expectedLength = 0;

			var result =
				VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
				                       zTolerance);

			Assert.IsTrue(result.IsEmpty);
		}

		[Test]
		public void CanCalculateZOnlyDifference_SimpleLineMinimalZDifference()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50.001, 100, 0, 50);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);

			double zTolerance = 0.0125;
			ISpatialReference lv95 = CreateSpatialReference(0.0125, zTolerance);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			int expectedLength = 0;
			var result =
				VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
				                       zTolerance);

			//IPolyline result = ReshapeUtils.GetZOnlyDifference(sourcePolyline, targetPolyline);
			Assert.True(result.IsEmpty);

			// and now with z-tolerance 0:
			expectedLength = 100;
			result = VerifyZOnlyDifferences(sourcePolyline, targetPolyline,
			                                expectedLength, 0);

			Assert.IsFalse(result == null || result.IsEmpty);
			//Assert.AreEqual(100, Math.Round(result.Length, 3));
		}

		[Test]
		public void CanCalculateZOnlyDifference_StraightLineNoDifference()
		{
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			sourcePolyline.SpatialReference = lv95;
			targetPolyline.SpatialReference = lv95;

			object missing = Type.Missing;

			((IPointCollection) targetPolyline).AddPoint(
				GeometryFactory.CreatePoint(20, 0, 50), ref missing, ref missing);

			((IPointCollection) targetPolyline).AddPoint(
				GeometryFactory.CreatePoint(60, 0, 50), ref missing, ref missing);

			GeometryUtils.Simplify(targetPolyline, true, true);

			int expectedLength = 0;
			var result =
				VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
				                       0.0125);

			Assert.True(result.IsEmpty);
		}

		[Test]
		public void
			CanCalculateZOnlyDifference_StraightLineNoDifferenceFlipSourceAndTarget()
		{
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			targetPolyline.SpatialReference = lv95;
			sourcePolyline.SpatialReference = lv95;

			object missing = Type.Missing;

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(20, 0, 50), ref missing, ref missing);

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(60, 0, 50), ref missing, ref missing);

			GeometryUtils.Simplify(sourcePolyline, true, true);

			int expectedLength = 0;
			var result =
				VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
				                       0.0125);
			Assert.True(result.IsEmpty);
			//IPolyline result = ReshapeUtils.GetZOnlyDifference(sourcePolyline,
			//                                                   targetPolyline);
			//Assert.IsNull(result);
		}

		[Test]
		public void CanCalculateZOnlyDifference_StraightLineWithIntermediateDifference()
		{
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			targetPolyline.SpatialReference = lv95;
			sourcePolyline.SpatialReference = lv95;

			object missing = Type.Missing;

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(20, 0, 50), ref missing, ref missing);

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(40, 0, 80), ref missing, ref missing);

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(60, 0, 50), ref missing, ref missing);

			GeometryUtils.Simplify(sourcePolyline, true, true);

			int expectedLength = 40;
			VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
			                       0.0125);
		}

		[Test]
		public void
			CanCalculateZOnlyDifference_StraightLineWithIntermediateDifferenceFlipSourceAndTarget
			()
		{
			IPolyline targetPolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);
			IPolyline sourcePolyline =
				GeometryFactory.CreatePolyline(0, 0, 50, 100, 0, 50);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			targetPolyline.SpatialReference = lv95;
			sourcePolyline.SpatialReference = lv95;

			object missing = Type.Missing;

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(20, 0, 50), ref missing, ref missing);

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(40, 0, 80), ref missing, ref missing);

			((IPointCollection) sourcePolyline).AddPoint(
				GeometryFactory.CreatePoint(60, 0, 50), ref missing, ref missing);

			GeometryUtils.Simplify(sourcePolyline, true, true);

			int expectedLength = 40;
			VerifyZOnlyDifferences(sourcePolyline, targetPolyline, expectedLength,
			                       0.0125);
		}

		[Test]
		public void CanCalculateDifferenceZAwareCircularArcsWithMinimumTolerance()
		{
			var poly1 =
				(IPolygon) ReadGeometryFromXML(
					TestUtils.GetGeometryTestDataPath("polyWithCircularArcs.xml"));

			var targetPoly =
				(IPolygon) ReadGeometryFromXML(
					TestUtils.GetGeometryTestDataPath("circleWithSeveralArcs.xml"));

			var calculator = new ReshapableSubcurveCalculator();
			calculator.UseMinimumTolerance = true;

			var cutSubcurves = new List<CutSubcurve>();
			calculator.CalculateSubcurves(poly1,
			                              GeometryFactory.CreatePolyline(targetPoly),
			                              cutSubcurves, null);

			int count = cutSubcurves.Count;
			double totalLength = cutSubcurves.Sum(subcurve => subcurve.Path.Length);

			// NOTE: There is no actual Z-difference. For the more sophisticated version see
			// GeometryReshaper.CanReshapeAndAdjustAlongNonLinearSegmentsPolygonCircleWithMinimumToleranceAndTargetPointInsertion()()
			Assert.AreEqual(targetPoly.Length, totalLength, 0.001);
		}

		[Test]
		public void CanCalculateDifferenceInLargeGeometry()
		{
			const int manyPointsPerPart = 123456;
			const int holes = 3;

			IPolygon poly2a =
				CreatePunchedSquarePolygon("2a", manyPointsPerPart, holes, 1);
			GeometryUtils.Simplify(poly2a, true, true);

			IPolygon poly2b = GeometryFactory.Clone(poly2a);

			var watch = new Stopwatch();
			watch.Start();

			IPolyline line2a = GeometryFactory.CreatePolyline(poly2a);
			IPolyline line2b = GeometryFactory.CreatePolyline(poly2b);

			watch.Stop();

			Console.WriteLine(@"Created polylines in {0} ms", watch.ElapsedMilliseconds);

			watch.Reset();
			watch.Start();

			IPolyline result = ReshapeUtils.GetZOnlyDifference(line2a,
			                                                   line2b);

			watch.Stop();

			Assert.IsNull(result);

			Console.WriteLine(@"Calculate Z-only difference (no changes) in {0} ms",
			                  watch.ElapsedMilliseconds);

			var comparison = new GeometryComparison(line2a, line2b);

			watch.Reset();
			watch.Start();

			IDictionary<WKSPointZ, VertexIndex> differences =
				comparison.GetDifference(true);

			watch.Stop();

			Console.WriteLine(@"Calculate Difference (no changes) in {0} ms",
			                  watch.ElapsedMilliseconds);

			Assert.AreEqual(0, differences.Count);
		}

		[Test]
		public void CanCalculateDifferenceInHugeLockergestein()
		{
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			var poly1 = (IPolygon) ReadGeometryFromXML(filePath);

			Console.WriteLine(@"{0}: {1}",
			                  @"Intersection Tests with 'Huge Lockergestein'",
			                  GetVertexString(poly1));

			IPolygon poly2 = GeometryFactory.Clone(poly1);

			var watch = new Stopwatch();
			watch.Start();

			IPolyline result =
				ReshapeUtils.GetZOnlyDifference(
					GeometryFactory.CreatePolyline(poly1),
					GeometryFactory.CreatePolyline(poly2));

			watch.Stop();

			Console.WriteLine(
				@"Calculated Z difference in huge Lockergestein (no difference) in {0} ms",
				watch.ElapsedMilliseconds);

			Assert.IsNull(result);

			// Change few points in Z
			IPoint point7 = ((IPointCollection) poly2).get_Point(7);
			point7.Z += 0.01;
			((IPointCollection) poly2).UpdatePoint(7, point7);

			IPoint point8 = ((IPointCollection) poly2).get_Point(8);
			point8.Z += 0.01;
			((IPointCollection) poly2).UpdatePoint(8, point8);

			watch.Reset();
			watch.Start();
			result =
				ReshapeUtils.GetZOnlyDifference(
					GeometryFactory.CreatePolyline(poly1),
					GeometryFactory.CreatePolyline(poly2), 0.0);

			watch.Stop();

			Console.WriteLine(
				@"Calculated Z difference in huge Lockergestein (2 different) in {0} ms",
				watch.ElapsedMilliseconds);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsEmpty);

			double changedLength = ((ISegmentCollection) poly2).Segment[6].Length +
			                       ((ISegmentCollection) poly2).Segment[7].Length +
			                       ((ISegmentCollection) poly2).Segment[8].Length;

			Assert.AreEqual(Math.Round(changedLength, 5), Math.Round(result.Length, 5));

			GeometryUtils.MoveGeometry(poly2, 0, 0, 0.5);

			watch.Reset();
			watch.Start();
			result =
				ReshapeUtils.GetZOnlyDifference(
					GeometryFactory.CreatePolyline(poly1),
					GeometryFactory.CreatePolyline(poly2));

			watch.Stop();

			Console.WriteLine(
				@"Calculated Z difference in huge Lockergestein (all different) in {0} ms",
				watch.ElapsedMilliseconds);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsEmpty);
			Assert.AreEqual(Math.Round(poly2.Length, 5), Math.Round(result.Length, 5));
		}

		[Test]
		public void CanReshapeMultipatch()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon polygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 50, 20, lv95));

			IMultiPatch multiPatch = GeometryFactory.CreateMultiPatch(
				polygon);

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 - 100, 1200000),
				GeometryFactory.CreatePoint(2600000 + 100, 1200000));
			cutLine.SpatialReference = lv95;

			var reshapePath = (IPath) ((IGeometryCollection) cutLine).Geometry[0];

			var reshapeInfo = new ReshapeInfo(multiPatch, reshapePath, null);

			IList<ReshapeInfo> singleReshapes;

			ReshapeUtils.ReshapeAllGeometryParts(reshapeInfo, reshapePath,
			                                     out singleReshapes);

			var reshapedRing = (IRing) ((IGeometryCollection) multiPatch).Geometry[0];

			Assert.True(MathUtils.AreEqual(((IArea) reshapedRing).Area,
			                               ((IArea) polygon).Area / 2,
			                               0.1));
		}

		[Test]
		public void CanReshapeVerticalSquareRingInMultipatch()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			//IRing ring = GeometryFactory.CreateRing(
			//	GeometryFactory.CreatePath())
			var points = new WKSPointZ[5];
			points[0] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 500
			            };

			points[1] = new WKSPointZ
			            {
				            X = 2600100,
				            Y = 1200000,
				            Z = 500
			            };

			points[2] = new WKSPointZ
			            {
				            X = 2600100,
				            Y = 1200000,
				            Z = 1000
			            };

			points[3] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 1000
			            };

			points[4] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 500
			            };

			IRing ring = new RingClass();
			((IGeometry) ring).SpatialReference = lv95;
			GeometryUtils.MakeZAware(ring);
			GeometryUtils.SetWKSPointZs((IPointCollection4) ring, points);

			IMultiPatch multipatch = new MultiPatchClass();
			((IGeometry) multipatch).SpatialReference = lv95;

			GeometryUtils.MakeZAware(multipatch);
			GeometryUtils.MakeMAware(multipatch);

			GeometryUtils.MakePointIDAware(multipatch);

			GeometryFactory.AddRingToMultiPatch(ring, multipatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchOuterRing);

			var unReshaped = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			int originalPointCount = ((IPointCollection) unReshaped).PointCount;

			// Left reshape is slightly larger -> vertical reshape side is determined by size only
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600051, 1200000 - 100, 222),
				GeometryFactory.CreatePoint(2600051, 1200000 + 100, 222));
			cutLine.SpatialReference = lv95;

			GeometryUtils.MakeZAware(cutLine);

			var reshapePath = (IPath) ((IGeometryCollection) cutLine).Geometry[0];

			Assert.IsTrue(((ICurve3D) unReshaped).IsClosed3D);

			var reshapeInfo = new ReshapeInfo(multipatch, reshapePath, null)
			                  {
				                  NonPlanar = true
			                  };

			IList<ReshapeInfo> singleReshapes;

			ReshapeUtils.ReshapeAllGeometryParts(reshapeInfo, reshapePath,
			                                     out singleReshapes);

			var reshapedRing = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			Assert.AreEqual(originalPointCount,
			                ((IPointCollection) reshapedRing).PointCount);

			Assert.IsTrue(((ICurve3D) reshapedRing).IsClosed3D);
			Assert.AreEqual(2 * 500 + 2 * 51, ((ICurve3D) reshapedRing).Length3D);

			var newPoints = new WKSPointZ[5];
			GeometryUtils.QueryWKSPointZs((IPointCollection4) reshapedRing, newPoints);

			// first, fourth and last
			Assert.IsTrue(GeometryUtils.IsSamePoint(points[0], newPoints[0], 0, 0));
			Assert.IsTrue(GeometryUtils.IsSamePoint(points[3], newPoints[3], 0, 0));
			Assert.IsTrue(GeometryUtils.IsSamePoint(points[4], newPoints[4], 0, 0));

			// the new cut points
			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600051,
				                                        Y = 1200000,
				                                        Z = 500
			                                        }, newPoints[1], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600051,
				                                        Y = 1200000,
				                                        Z = 1000
			                                        }, newPoints[2], 0, 0));
		}

		[Test]
		public void CanReshapeVerticalTriangularRingInMultipatch()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			//IRing ring = GeometryFactory.CreateRing(
			//	GeometryFactory.CreatePath())
			var points = new WKSPointZ[4];
			points[0] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 500
			            };

			points[1] = new WKSPointZ
			            {
				            X = 2600100,
				            Y = 1200000,
				            Z = 500
			            };

			points[2] = new WKSPointZ
			            {
				            X = 2600050,
				            Y = 1200000,
				            Z = 1000
			            };

			points[3] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 500
			            };

			IRing ring = new RingClass();
			((IGeometry) ring).SpatialReference = lv95;
			GeometryUtils.MakeZAware(ring);
			GeometryUtils.SetWKSPointZs((IPointCollection4) ring, points);

			IMultiPatch multipatch = new MultiPatchClass();
			((IGeometry) multipatch).SpatialReference = lv95;

			GeometryUtils.MakeZAware(multipatch);
			GeometryUtils.MakeMAware(multipatch);

			GeometryUtils.MakePointIDAware(multipatch);

			GeometryFactory.AddRingToMultiPatch(ring, multipatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchOuterRing);

			var unReshaped = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			int originalPointCount = ((IPointCollection) unReshaped).PointCount;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600075, 1200000 - 100, 222),
				GeometryFactory.CreatePoint(2600075, 1200000 + 100, 222));
			cutLine.SpatialReference = lv95;

			GeometryUtils.MakeZAware(cutLine);

			var reshapePath = (IPath) ((IGeometryCollection) cutLine).Geometry[0];

			Assert.IsTrue(((ICurve3D) unReshaped).IsClosed3D);

			var reshapeInfo = new ReshapeInfo(multipatch, reshapePath, null)
			                  {NonPlanar = true};

			IList<ReshapeInfo> singleReshapes;
			ReshapeUtils.ReshapeAllGeometryParts(reshapeInfo, reshapePath,
			                                     out singleReshapes);

			var reshapedRing = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			Assert.AreEqual(originalPointCount + 1,
			                ((IPointCollection) reshapedRing).PointCount);

			Assert.IsTrue(((ICurve3D) reshapedRing).IsClosed3D);

			double expectedLength = Math.Sqrt(50 * 50 + 500 * 500) * 1.5 + 75 + 250;
			Assert.AreEqual(expectedLength, ((ICurve3D) reshapedRing).Length3D, 0.001);

			var newPoints = new WKSPointZ[5];
			GeometryUtils.QueryWKSPointZs((IPointCollection4) reshapedRing, newPoints);

			// first, fourth and last
			Assert.IsTrue(GeometryUtils.IsSamePoint(points[0], newPoints[0], 0, 0));
			Assert.IsTrue(GeometryUtils.IsSamePoint(points[2], newPoints[3], 0, 0));
			Assert.IsTrue(GeometryUtils.IsSamePoint(points[3], newPoints[4], 0, 0));

			// the new cut points
			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600075,
				                                        Y = 1200000,
				                                        Z = 500
			                                        }, newPoints[1], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600075,
				                                        Y = 1200000,
				                                        Z = 750
			                                        }, newPoints[2], 0, 0));

			// And now reshape right through the vertex at the top, this time reshape the left
			cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600050, 1200000 - 100, 222),
				GeometryFactory.CreatePoint(2600050, 1200000 + 100, 222));
			cutLine.SpatialReference = lv95;

			GeometryUtils.MakeZAware(cutLine);

			reshapePath = (IPath) ((IGeometryCollection) cutLine).Geometry[0];

			reshapeInfo = new ReshapeInfo(multipatch, reshapePath, null)
			              {NonPlanar = true};

			// keep the right (small part)
			reshapeInfo.RingReshapeSide = RingReshapeSideOfLine.Right;

			ReshapeUtils.ReshapeAllGeometryParts(reshapeInfo, reshapePath,
			                                     out singleReshapes);

			reshapedRing = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			Assert.AreEqual(5, ((IPointCollection) reshapedRing).PointCount);
			Assert.IsTrue(((ICurve3D) reshapedRing).IsClosed3D);

			expectedLength = Math.Sqrt(50 * 50 + 500 * 500) * 0.5 + 25 + 500 + 250;
			Assert.AreEqual(expectedLength, ((ICurve3D) reshapedRing).Length3D, 0.001);

			newPoints = new WKSPointZ[5];
			GeometryUtils.QueryWKSPointZs((IPointCollection4) reshapedRing, newPoints);

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600050,
				                                        Y = 1200000,
				                                        Z = 500
			                                        }, newPoints[0], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600075,
				                                        Y = 1200000,
				                                        Z = 500
			                                        }, newPoints[1], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600075,
				                                        Y = 1200000,
				                                        Z = 750
			                                        }, newPoints[2], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600050,
				                                        Y = 1200000,
				                                        Z = 1000
			                                        }, newPoints[3], 0, 0));
		}

		[Test]
		public void CanReshapeVerticalRingWithMutipleReshapeLineCrossings()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			// Vertical triangle, oriented towards the south:
			var points = new WKSPointZ[4];
			points[0] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 500
			            };

			points[1] = new WKSPointZ
			            {
				            X = 2600100,
				            Y = 1200000,
				            Z = 500
			            };

			points[2] = new WKSPointZ
			            {
				            X = 2600050,
				            Y = 1200000,
				            Z = 1000
			            };

			points[3] = new WKSPointZ
			            {
				            X = 2600000,
				            Y = 1200000,
				            Z = 500
			            };

			IRing ring = new RingClass();
			((IGeometry) ring).SpatialReference = lv95;
			GeometryUtils.MakeZAware(ring);
			GeometryUtils.SetWKSPointZs((IPointCollection4) ring, points);

			IMultiPatch multipatch = new MultiPatchClass();
			((IGeometry) multipatch).SpatialReference = lv95;

			GeometryUtils.MakeZAware(multipatch);
			GeometryUtils.MakeMAware(multipatch);

			GeometryUtils.MakePointIDAware(multipatch);

			GeometryFactory.AddRingToMultiPatch(ring, multipatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchOuterRing);

			var unReshaped = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600075, 1200000 - 100, 222),
				GeometryFactory.CreatePoint(2600075, 1200000 + 100, 222),
				GeometryFactory.CreatePoint(2600025, 1200000 + 100, 222),
				GeometryFactory.CreatePoint(2600025, 1200000 - 100, 222));

			cutLine.SpatialReference = lv95;

			GeometryUtils.MakeZAware(cutLine);

			var reshapePath = (IPath) ((IGeometryCollection) cutLine).Geometry[0];

			Assert.IsTrue(((ICurve3D) unReshaped).IsClosed3D);

			var reshapeInfo = new ReshapeInfo(multipatch, reshapePath, null);

			IList<IPath> verticalPaths;
			Assert.IsTrue(reshapeInfo.IsVerticalRingReshape(0, out verticalPaths));

			Assert.AreEqual(2, verticalPaths.Count);

			// Currently it is the caller's responsability to make 2 different reshapes using the desired side...

			// We want the middle part:

			// verticalPaths[0] is the one at X=2600025
			var reshape1 = new ReshapeInfo(multipatch, verticalPaths[0], null);
			reshape1.RingReshapeSide = RingReshapeSideOfLine.Right;
			reshape1.NonPlanar = true;

			ReshapeUtils.ReshapeGeometry(reshape1, verticalPaths[0]);

			// verticalPaths[1] is the one at X=2600075
			var reshape2 = new ReshapeInfo(multipatch, verticalPaths[1], null);
			reshape2.RingReshapeSide = RingReshapeSideOfLine.Left;
			reshape2.NonPlanar = true;

			ReshapeUtils.ReshapeGeometry(reshape2, verticalPaths[1]);

			var reshapedRing = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			Assert.AreEqual(6, ((IPointCollection) reshapedRing).PointCount);

			Assert.IsTrue(((ICurve3D) reshapedRing).IsClosed3D);

			double expectedLength = Math.Sqrt(50 * 50 + 500 * 500) * 1.0 + 50 + 2 * 250;
			Assert.AreEqual(expectedLength, ((ICurve3D) reshapedRing).Length3D, 0.001);

			var newPoints = new WKSPointZ[6];
			GeometryUtils.QueryWKSPointZs((IPointCollection4) reshapedRing, newPoints);

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600025,
				                                        Y = 1200000,
				                                        Z = 500
			                                        }, newPoints[0], 0, 0));

			// the new cut points
			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600075,
				                                        Y = 1200000,
				                                        Z = 500
			                                        }, newPoints[1], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600075,
				                                        Y = 1200000,
				                                        Z = 750
			                                        }, newPoints[2], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600050,
				                                        Y = 1200000,
				                                        Z = 1000
			                                        }, newPoints[3], 0, 0));

			Assert.IsTrue(GeometryUtils.IsSamePoint(new WKSPointZ
			                                        {
				                                        X = 2600025,
				                                        Y = 1200000,
				                                        Z = 750
			                                        }, newPoints[4], 0, 0));
		}

		[Test]
		public void CannotPerformVerticalReshapeOnFlatRing()
		{
			// Originally this geometry resulted in an endless loop because 
			// IntersectionUtils.IntersectNonPlanar returned the incorrect result
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// The problem cannot be reproduced with standard resolution
			var srResolution = (ISpatialReferenceResolution) lv95;
			srResolution.set_XYResolution(true, 0.001);

			var srTolerance = (ISpatialReferenceTolerance) lv95;
			srTolerance.XYTolerance = 0.01;

			// Vertical triangle, oriented towards the south:
			var points = new WKSPointZ[6];
			points[0] = new WKSPointZ
			            {
				            X = 2578309.3000000007,
				            Y = 1183264.3999999985,
				            Z = 619.14500000000407
			            };

			points[1] = new WKSPointZ
			            {
				            X = 2578295.6829999983,
				            Y = 1183260.568,
				            Z = 619.14500000000407
			            };

			points[2] = new WKSPointZ
			            {
				            X = 2578293.9990000017,
				            Y = 1183266.5500000007,
				            Z = 619.14500000000407
			            };

			points[3] = new WKSPointZ
			            {
				            X = 2578295.9070000015,
				            Y = 1183267.1559999995,
				            Z = 619.14500000000407
			            };

			points[4] = new WKSPointZ
			            {
				            X = 2578307.5989999995,
				            Y = 1183270.4450000003,
				            Z = 619.14500000000407
			            };

			points[5] = new WKSPointZ
			            {
				            X = 2578309.3000000007,
				            Y = 1183264.3999999985,
				            Z = 619.14500000000407
			            };

			IRing ring = new RingClass();
			((IGeometry) ring).SpatialReference = lv95;
			GeometryUtils.MakeZAware(ring);
			GeometryUtils.SetWKSPointZs((IPointCollection4) ring, points);

			IMultiPatch multipatch = new MultiPatchClass();
			((IGeometry) multipatch).SpatialReference = lv95;

			GeometryUtils.MakeZAware(multipatch);
			GeometryUtils.MakeMAware(multipatch);

			GeometryUtils.MakePointIDAware(multipatch);

			GeometryFactory.AddRingToMultiPatch(ring, multipatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchOuterRing);

			var unReshaped = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2578314.9090000018, 1183246.2400000021),
				GeometryFactory.CreatePoint(2578307.4299999997, 1183270.4310000017));

			cutLine.SpatialReference = lv95;

			//GeometryUtils.MakeZAware(cutLine);

			var reshapePath = (IPath) ((IGeometryCollection) cutLine).Geometry[0];

			Assert.IsTrue(((ICurve3D) unReshaped).IsClosed3D);

			var reshapeInfo = new ReshapeInfo(multipatch, reshapePath, null);

			IList<IPath> verticalPaths;
			Assert.IsFalse(reshapeInfo.IsVerticalRingReshape(0, out verticalPaths));

			Assert.AreEqual(0, verticalPaths.Count);

			Assert.IsTrue(ReshapeUtils.ReshapeGeometry(reshapeInfo, reshapePath));

			var reshapedRing = (IRing) ((IGeometryCollection) multipatch).Geometry[0];

			Assert.AreEqual(6, ((IPointCollection) reshapedRing).PointCount);

			Assert.IsTrue(((ICurve3D) reshapedRing).IsClosed3D);
		}

		private static IPolyline VerifyZOnlyDifferences(IPolyline sourcePolyline,
		                                                IPolyline targetPolyline,
		                                                int expectedResultLength,
		                                                double zTolerance = 0)
		{
			IPolyline geomOpResult = IntersectionUtils.GetZOnlyDifferenceLines(
				sourcePolyline, targetPolyline, zTolerance);

			Assert.IsNotNull(geomOpResult);
			Assert.AreEqual(expectedResultLength, Math.Round(geomOpResult.Length, 3));

			IPolyline legacyResult =
				ReshapeUtils.GetZOnlyDifferenceLegacy(sourcePolyline,
				                                      targetPolyline, zTolerance);

			Assert.IsNotNull(legacyResult);

			Assert.AreEqual(expectedResultLength, Math.Round(legacyResult.Length, 3));

			GeometryUtils.AreEqual(geomOpResult, legacyResult);

			return geomOpResult;
		}

		private static ISpatialReference CreateSpatialReference(double xyTolerance,
		                                                        double zTolerance)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// Need to initialize resolution in XY and Z:
			((ISpatialReferenceResolution) sr).ConstructFromHorizon();
			((ISpatialReferenceResolution) sr).SetDefaultZResolution();

			// Only now can set tolerance values:
			((ISpatialReferenceTolerance) sr).XYTolerance = xyTolerance;
			((ISpatialReferenceTolerance) sr).ZTolerance = zTolerance;

			return sr;
		}

		private static object ReadGeometryFromXML(string filePath)
		{
			IXMLSerializer serializer = new XMLSerializerClass();

			IXMLReader reader = new XMLReaderClass();

			IXMLStream stream = new XMLStreamClass();

			stream.LoadFromFile(filePath);

			reader.ReadFrom((IStream) stream);

			return serializer.ReadObject(reader, null, null);
		}

		private static IPolygon CreatePunchedSquarePolygon(
			string name, int pointsPerPart, int numberOfHoles, int rotateSteps)
		{
			const double LowerLeftX = 2600000;
			const double LowerLeftY = 1200000;
			const double OuterSideLength = 9000;
			const double ConstantZ = 99;

			ISpatialReference sr = CreateSpatialReference(0.0125, 0.0125);

			IPolygon polygon = CreatePunchedSquarePolygon(
				LowerLeftX, LowerLeftY, OuterSideLength,
				numberOfHoles, pointsPerPart, ConstantZ, rotateSteps, sr);

			Console.WriteLine(@"{0}: {1}", name, GetVertexString(polygon));

			return polygon;
		}

		private static IPolygon CreatePunchedSquarePolygon(
			double xOffset, double yOffset, double sideLength,
			int holes, int pointsPerPart, double z, int rotateSteps,
			ISpatialReference sr)
		{
			var holesInXAndY = (int) Math.Round(Math.Sqrt(holes));
			var pointsPerSide = (int) Math.Round(pointsPerPart / 4.0);

			IPolygon polygon = new PolygonClass();
			polygon.SpatialReference = sr;
			GeometryUtils.MakeZAware(polygon);

			var geoColl = (IGeometryCollection) polygon;

			object missing = Type.Missing;

			// Create and add the outer ring
			WKSPointZ[] pointArray = CreateSquare(xOffset, yOffset, sideLength,
			                                      pointsPerSide,
			                                      true,
			                                      z);
			CollectionUtils.Rotate(pointArray, rotateSteps);

			IRing ring = new RingClass();
			GeometryUtils.SetWKSPointZs((IPointCollection4) ring, pointArray);
			ring.Close();

			geoColl.AddGeometry(ring, ref missing, ref missing);

			// Create and add inner rings (= holes)
			double innerSideLength = sideLength / (1 + 2 * holesInXAndY);
			for (var i = 0; i < holesInXAndY; i++)
			{
				double llx = xOffset + (innerSideLength + 2 * i * innerSideLength);

				for (var j = 0; j < holesInXAndY; j++)
				{
					double lly = yOffset + (innerSideLength + 2 * j * innerSideLength);

					pointArray =
						CreateSquare(llx, lly, innerSideLength, pointsPerSide, false, z);
					CollectionUtils.Rotate(pointArray, rotateSteps);

					ring = new RingClass();
					GeometryUtils.SetWKSPointZs((IPointCollection4) ring, pointArray);
					ring.Close();

					geoColl.AddGeometry(ring, ref missing, ref missing);
				}
			}

			geoColl.GeometriesChanged();
			polygon.SimplifyPreserveFromTo();

			// We closed each ring: poly must be closed too!
			Assert.IsTrue(polygon.IsClosed);

			return polygon;
		}

		private static WKSPointZ[] CreateSquare(
			double lowerLeftX, double lowerLeftY, double sideLength,
			int pointsPerSide, bool clockWise, double z)
		{
			pointsPerSide = Math.Max(1, pointsPerSide);
			var points = new WKSPointZ[4 * pointsPerSide];

			double dist = sideLength / pointsPerSide;
			for (var i = 0; i < pointsPerSide; i++)
			{
				//if (clockWise)
				//{
				points[i].X = lowerLeftX;
				points[i].Y = lowerLeftY + i * dist;
				points[i].Z = z;

				points[i + pointsPerSide].X = lowerLeftX + i * dist;
				points[i + pointsPerSide].Y = lowerLeftY + sideLength;
				points[i + pointsPerSide].Z = z;

				points[i + 2 * pointsPerSide].X = lowerLeftX + sideLength;
				points[i + 2 * pointsPerSide].Y = lowerLeftY + sideLength - i * dist;
				points[i + 2 * pointsPerSide].Z = z;

				points[i + 3 * pointsPerSide].X = lowerLeftX + sideLength - i * dist;
				points[i + 3 * pointsPerSide].Y = lowerLeftY;
				points[i + 3 * pointsPerSide].Z = z;
				//}
				//else
				//{
				//    points[i].X = lowerLeftX + i * dist;
				//    points[i].Y = lowerLeftY;
				//    points[i].Z = z;

				//    points[i + pointsPerSide].X = lowerLeftX + sideLength;
				//    points[i + pointsPerSide].Y = lowerLeftY + i * dist;
				//    points[i + pointsPerSide].Z = z;

				//    points[i + 2 * pointsPerSide].X = lowerLeftX + sideLength - i * dist;
				//    points[i + 2 * pointsPerSide].Y = lowerLeftY + sideLength;
				//    points[i + 2 * pointsPerSide].Z = z;

				//    points[i + 3 * pointsPerSide].X = lowerLeftX;
				//    points[i + 3 * pointsPerSide].Y = lowerLeftY + sideLength - i * dist;
				//    points[i + 3 * pointsPerSide].Z = z;
				//}
			}

			if (! clockWise)
			{
				Array.Reverse(points);
			}

			return points;
		}

		private static string GetVertexString(IPolygon geometry)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("(polygon parts={0} points={1}",
			                GeometryUtils.GetPartCount(geometry),
			                GeometryUtils.GetPointCount(geometry));

			return sb.ToString();
		}
	}
}

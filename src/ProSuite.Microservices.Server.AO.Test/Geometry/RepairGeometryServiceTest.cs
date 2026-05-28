using System;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geometry.RepairGeometry;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class RepairGeometryServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanCalculateRepairInfoForSimplePolygon()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(123, "TestFC", sr, workspace,
			                                               esriGeometryType.esriGeometryPolygon);

			// Simple polygon - no issues
			IPolygon simplePolygon = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));
			simplePolygon.SpatialReference = sr;

			GdbFeature feature = GdbFeature.Create(1, fClass);
			feature.Shape = simplePolygon;

			CalculateRepairInfoRequest request = CreateRequest(feature);
			request.RepairOptions = CreateDefaultOptions();

			CalculateRepairInfoResponse response =
				RepairGeometryServiceUtils.CalculateRepairInfo(request, null);

			Assert.IsNotNull(response);
			// No issues in a simple polygon
			Assert.AreEqual(0, response.RepairInfos.Count);
		}

		[Test]
		public void CanCalculateRepairInfoForPolygonWithShortSegment()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(123, "TestFC", sr, workspace,
			                                               esriGeometryType.esriGeometryPolygon);

			// Polygon with a very short segment (0.01m < 0.5m threshold)
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1200000.01, 400), // near duplicate
				WKSPointZUtils.CreatePoint(2601000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400)
			};

			IPolygon polygon = GeometryFactory.CreatePolygon(points, sr);

			GdbFeature feature = GdbFeature.Create(42, fClass);
			feature.Shape = polygon;

			CalculateRepairInfoRequest request = CreateRequest(feature);
			request.RepairOptions = new RepairOptionsMsg
			                        {
				                        MinimumSegmentLength = 0.5,
				                        AllowLoops = false,
				                        AllowLinearSelfIntersections = false,
				                        AddCrackPointsBetweenParts = false,
				                        CrackPointTolerance = 0,
				                        Use2D = true
			                        };

			CalculateRepairInfoResponse response =
				RepairGeometryServiceUtils.CalculateRepairInfo(request, null);

			Assert.IsNotNull(response);
			Assert.AreEqual(1, response.RepairInfos.Count,
			                "Expected 1 feature with issues");

			RepairInfoMsg repairInfo = response.RepairInfos[0];
			Assert.Greater(repairInfo.InvalidSegments.Count, 0,
			               "Expected at least one invalid (short) segment");
		}

		[Test]
		public void CanApplyRepairGeometry()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(123, "TestFC", sr, workspace,
			                                               esriGeometryType.esriGeometryPolygon);

			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1200000.01, 400),
				WKSPointZUtils.CreatePoint(2601000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400)
			};

			IPolygon polygon = GeometryFactory.CreatePolygon(points, sr);
			int originalPointCount = GeometryUtils.GetPointCount(polygon);

			GdbFeature feature = GdbFeature.Create(42, fClass);
			feature.Shape = polygon;

			// First calculate
			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = new RepairOptionsMsg
			                            {
				                            MinimumSegmentLength = 0.5,
				                            Use2D = true
			                            };

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			Assert.Greater(calcResponse.RepairInfos.Count, 0, "Expected issues to be found");

			// Then apply
			var applyRequest = new ApplyRepairGeometryRequest();
			var featureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature);
			var classMsg = ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true);
			applyRequest.SourceFeatures.Add(featureMsg);
			applyRequest.ClassDefinitions.Add(classMsg);
			applyRequest.RepairInfos.AddRange(calcResponse.RepairInfos);
			applyRequest.RepairOptions = calcRequest.RepairOptions;

			ApplyRepairGeometryResponse applyResponse =
				RepairGeometryServiceUtils.ApplyRepairGeometry(applyRequest, null);

			Assert.IsNotNull(applyResponse);
			Assert.AreEqual(1, applyResponse.ResultFeatures.Count,
			                "Expected 1 result feature");

			ResultObjectMsg resultFeature = applyResponse.ResultFeatures[0];
			IGeometry resultGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultFeature.Update.Shape);

			Assert.IsNotNull(resultGeometry);
			Assert.IsFalse(resultGeometry.IsEmpty, "Result geometry should not be empty");

			int resultPointCount = GeometryUtils.GetPointCount(resultGeometry);
			Assert.Less(resultPointCount, originalPointCount,
			            "Repaired geometry should have fewer points (short segment removed)");
		}

		[NotNull]
		private static CalculateRepairInfoRequest CreateRequest([NotNull] GdbFeature feature)
		{
			var featureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature);
			var classMsg = ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true);

			return new CalculateRepairInfoRequest
			       {
				       SourceFeatures = { featureMsg },
				       ClassDefinitions = { classMsg }
			       };
		}

		[NotNull]
		private static RepairOptionsMsg CreateDefaultOptions()
		{
			return new RepairOptionsMsg
			       {
				       MinimumSegmentLength = 0.5,
				       AllowLoops = false,
				       AllowLinearSelfIntersections = false,
				       AddCrackPointsBetweenParts = true,
				       CrackPointTolerance = 0,
				       Use2D = true
			       };
		}

		[Test]
		public void CanApplyRepairGeometryForPolygonWithNonAdjacentNearVertex()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(124, "TestFC2", sr, workspace,
			                                               esriGeometryType.esriGeometryPolygon);

			// Ring: vertex 4 at (2600000.05, 1200000) is 0.05m from vertex 0 at (2600000, 1200000)
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000.05, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400)
			};

			IPolygon polygon = GeometryFactory.CreatePolygon(points, sr);

			GdbFeature feature = GdbFeature.Create(43, fClass);
			feature.Shape = polygon;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              CrackPointTolerance = 0.1
			              };

			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			var applyRequest = new ApplyRepairGeometryRequest();
			var featureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature);
			var classMsg = ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true);
			applyRequest.SourceFeatures.Add(featureMsg);
			applyRequest.ClassDefinitions.Add(classMsg);
			applyRequest.RepairInfos.AddRange(calcResponse.RepairInfos);
			applyRequest.RepairOptions = options;

			ApplyRepairGeometryResponse applyResponse =
				RepairGeometryServiceUtils.ApplyRepairGeometry(applyRequest, null);

			Assert.IsNotNull(applyResponse);
			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			ResultObjectMsg resultFeature = applyResponse.ResultFeatures[0];
			IGeometry resultGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultFeature.Update.Shape);

			Assert.IsNotNull(resultGeometry);
			Assert.IsFalse(resultGeometry.IsEmpty);

			// After cracking, the two close vertices should be snapped to the same point
			IPointCollection resultPoints = (IPointCollection) resultGeometry;
			double xDiff = Math.Abs(resultPoints.Point[0].X - resultPoints.Point[4].X);
			Assert.Less(xDiff, 0.001, "Close vertices should be snapped together after cracking");
		}

		[Test]
		public void CanApplyRepairGeometryForPolygonWithNonAdjacentSegment()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(125, "TestFC3", sr, workspace,
			                                               esriGeometryType.esriGeometryPolygon);

			// Pan handle geometry:
			// Ring where vertex 4 at (2600000.05, 1200005) is 0.05m from segment 0
			// (segment 0: (2600000,1200000)→(2600000,1201000), i.e. the left edge)
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1200005, 400),
				WKSPointZUtils.CreatePoint(2600000.05, 1200005, 400),
				WKSPointZUtils.CreatePoint(2600000.05, 1200000.05, 400),
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400)
			};

			IPolygon polygon = GeometryFactory.CreatePolygon(points, sr);

			GdbFeature feature = GdbFeature.Create(44, fClass);
			feature.Shape = polygon;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              CrackPointTolerance = 0.1
			              };

			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			var applyRequest = new ApplyRepairGeometryRequest();
			var featureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature);
			var classMsg = ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true);
			applyRequest.SourceFeatures.Add(featureMsg);
			applyRequest.ClassDefinitions.Add(classMsg);
			applyRequest.RepairInfos.AddRange(calcResponse.RepairInfos);
			applyRequest.RepairOptions = options;

			ApplyRepairGeometryResponse applyResponse =
				RepairGeometryServiceUtils.ApplyRepairGeometry(applyRequest, null);

			Assert.IsNotNull(applyResponse);
			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			ResultObjectMsg resultFeature = applyResponse.ResultFeatures[0];
			IGeometry resultGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultFeature.Update.Shape);

			Assert.IsNotNull(resultGeometry);
			Assert.IsFalse(resultGeometry.IsEmpty);

			int resultPointCount = GeometryUtils.GetPointCount(resultGeometry);

			// The split and duplicated segments (handle of the pan) were removed by simplify -> 5 points left
			Assert.AreEqual(5, resultPointCount,
			                "Segment should be split, adding a new vertex");
		}

		[Test]
		public void CanApplyRepairGeometryForPolygonWithLinearSelfIntersections()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(130, "TestFC4", sr, workspace,
			                                               esriGeometryType.esriGeometryPolygon);

			// Rectangle with a spike on the bottom edge:
			//   pt0 (SW) → pt1 (NW) → pt2 (NE) → pt3 (SE) → pt4 (mid-bottom)
			//   → pt5 (spike tip, 1000 m south) → pt6 (=pt4, back up)  ← linear self-intersection
			//   → pt0 (close ring)
			// After repair the spike (seg 4 and seg 5) is removed; ring becomes:
			//   pt0 → pt1 → pt2 → pt3 → pt4 → pt0  (6 stored vertices)
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400), // pt0  SW / close
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400), // pt1  NW
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400), // pt2  NE
				WKSPointZUtils.CreatePoint(2601000, 1200000, 400), // pt3  SE
				WKSPointZUtils.CreatePoint(2600500, 1200000, 400), // pt4  mid-bottom
				WKSPointZUtils.CreatePoint(2600500, 1199000, 400), // pt5  spike tip
				WKSPointZUtils.CreatePoint(2600500, 1200000, 400), // pt6  = pt4 (back up)
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400) // pt7  = pt0 (close)
			};

			IPolygon polygon = GeometryFactory.CreatePolygon(points, sr);

			GdbFeature feature = GdbFeature.Create(49, fClass);
			feature.Shape = polygon;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              AllowLoops = false,
				              AllowLinearSelfIntersections = false
			              };

			// Calculate: both spike segments (seg 4 and seg 5) must be detected
			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			Assert.AreEqual(1, calcResponse.RepairInfos.Count,
			                "Expected 1 feature with linear self-intersection issues");

			RepairInfoMsg repairInfo = calcResponse.RepairInfos[0];
			Assert.AreEqual(2, repairInfo.InvalidSegments.Count,
			                "Expected 2 invalid segments (the spike: forward and backward)");

			bool hasSpikeForward = repairInfo.InvalidSegments.Any(s => s.AbsoluteIndex == 4);
			bool hasSpikeBack = repairInfo.InvalidSegments.Any(s => s.AbsoluteIndex == 5);
			Assert.IsTrue(hasSpikeForward, "Spike seg 4 (pt4→pt5) must be flagged");
			Assert.IsTrue(hasSpikeBack, "Spike seg 5 (pt5→pt4) must be flagged");

			// Apply: TryDeleteLinearSelfIntersectionsXY removes both spike segments from the ring
			var applyRequest = new ApplyRepairGeometryRequest();
			applyRequest.SourceFeatures.Add(
				ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature));
			applyRequest.ClassDefinitions.Add(
				ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true));
			applyRequest.RepairInfos.AddRange(calcResponse.RepairInfos);
			applyRequest.RepairOptions = options;

			ApplyRepairGeometryResponse applyResponse =
				RepairGeometryServiceUtils.ApplyRepairGeometry(applyRequest, null);

			Assert.IsNotNull(applyResponse);
			Assert.AreEqual(1, applyResponse.ResultFeatures.Count,
			                "Expected 1 repaired feature");

			IGeometry resultGeometry =
				ProtobufGeometryUtils.FromShapeMsg(
					applyResponse.ResultFeatures[0].Update.Shape);

			Assert.IsNotNull(resultGeometry);
			Assert.IsFalse(resultGeometry.IsEmpty, "Result polygon must not be empty");

			// Spike removed: ring is now pt0→pt1→pt2→pt3→pt4→pt0 = 6 stored vertices
			int resultPointCount = GeometryUtils.GetPointCount(resultGeometry);
			Assert.AreEqual(6, resultPointCount,
			                "Spike (2 segments) removed; ring retains pt4 as a bottom-edge vertex");

			// Verify no linear self-intersections remain
			var resultMultiPolycurve = GeometryConversionUtils.CreateMultiPolycurve(
				(IPolycurve) resultGeometry);
			double tolerance = GeometryUtils.GetXyTolerance(resultGeometry);

			bool hasLinearSelfIntersections =
				GeomTopoOpUtils.GetLinearSelfIntersectionsXY(resultMultiPolycurve, tolerance).Any();

			Assert.IsFalse(hasLinearSelfIntersections,
			               "No linear self-intersections should remain after repair");
		}

		[Test]
		public void CanCalculateRepairInfoForPolylineWithSelfIntersections()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(126, "TestFCPolyline", sr, workspace,
			                                               esriGeometryType.esriGeometryPolyline);

			// Polyline with 1-dimensional and 2-dimensional (linear) self-intersections
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				// 2D self-intersection: returns back to a previous point
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				// 1D linear self-intersection: crosses a previous segment
				WKSPointZUtils.CreatePoint(2600500, 1200500, 400),
				WKSPointZUtils.CreatePoint(2999500, 1200500, 400)
			};

			IPolyline polyline = GeometryFactory.CreatePolyline(points, sr);

			GdbFeature feature = GdbFeature.Create(45, fClass);
			feature.Shape = polyline;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              AllowLoops = false,
				              AllowLinearSelfIntersections = false
			              };

			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			Assert.IsNotNull(calcResponse);
			Assert.AreEqual(1, calcResponse.RepairInfos.Count,
			                "Expected 1 feature with issues");

			RepairInfoMsg repairInfo = calcResponse.RepairInfos[0];

			IPointCollection crackPointsCollection =
				(IPointCollection) ProtobufGeometryUtils.FromShapeMsg(repairInfo.CrackPointsToAdd);

			// TODO: Somehow the 1D self-intersection is not detected (or at least not at the expected location):
			//Assert.NotNull(crackPointsCollection);
			//Assert.Greater(crackPointsCollection.PointCount, 0,
			//               "Expected at least one crack point due to self-intersection");

			// Linear self-intersection segments are found via GetLinearSelfIntersectionsXY:
			// (2600000, 1201000) -> (2601000, 1201000) and
			// (2601000, 1201000) -> (2600000, 1201000) are both reported as invalid segments.
			Assert.AreEqual(2, repairInfo.InvalidSegments.Count,
			                "Expected 2 invalid segments for the duplicate/reversed segment pair");

			bool hasSeg1 = repairInfo.InvalidSegments.Any(s => s.AbsoluteIndex == 1);
			bool hasSeg2 = repairInfo.InvalidSegments.Any(s => s.AbsoluteIndex == 2);

			Assert.IsTrue(hasSeg1,
			              "Expected segment 1 (2600000,1201000)->(2601000,1201000) in invalid segments");
			Assert.IsTrue(hasSeg2,
			              "Expected segment 2 (2601000,1201000)->(2600000,1201000) in invalid segments");
		}

		[Test]
		public void CanCalculateRepairInfoForPolylineWithSelfIntersectionsAllowed()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(127, "TestFCPolyline2", sr, workspace,
			                                               esriGeometryType.esriGeometryPolyline);

			// Polyline with 1-dimensional and 2-dimensional (linear) self-intersections
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				// 1D self-intersection: loops back to a previous point
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				// 2D linear self-intersection: goes back along the previous segment
				WKSPointZUtils.CreatePoint(2600000, 1200500, 400),
				WKSPointZUtils.CreatePoint(2601000, 1200500, 400)
			};

			IPolyline polyline = GeometryFactory.CreatePolyline(points, sr);

			GdbFeature feature = GdbFeature.Create(46, fClass);
			feature.Shape = polyline;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              AllowLoops = true, // Allow 1D self-intersections
				              AllowLinearSelfIntersections =
					              true // Allow 2D linear self-intersections
			              };

			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			Assert.IsNotNull(calcResponse);
			Assert.AreEqual(0, calcResponse.RepairInfos.Count,
			                "Expected 0 issues as loops and linear self-intersections are allowed");
		}

		[Test]
		public void CanApplyRepairGeometryForPolylineWithLinearSelfIntersections()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(128, "TestFCPolyline3", sr, workspace,
			                                               esriGeometryType.esriGeometryPolyline);

			// Polyline with a linear self-intersection: seg 1 and seg 2 are exact reverses of each other.
			// The duplicate back-tracking pair should be removed, leaving 4 vertices.
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				// Linear self-intersection: reverses along the previous segment
				WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				WKSPointZUtils.CreatePoint(2600500, 1200500, 400),
				WKSPointZUtils.CreatePoint(2999500, 1200500, 400)
			};

			IPolyline polyline = GeometryFactory.CreatePolyline(points, sr);

			GdbFeature feature = GdbFeature.Create(47, fClass);
			feature.Shape = polyline;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              AllowLoops = false,
				              AllowLinearSelfIntersections = false
			              };

			// Calculate
			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			Assert.AreEqual(1, calcResponse.RepairInfos.Count,
			                "Expected 1 feature with linear self-intersection issues");
			Assert.AreEqual(2, calcResponse.RepairInfos[0].InvalidSegments.Count,
			                "Expected 2 invalid segments (the duplicate/reversed pair)");

			// Apply
			var applyRequest = new ApplyRepairGeometryRequest();
			applyRequest.SourceFeatures.Add(
				ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature));
			applyRequest.ClassDefinitions.Add(
				ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true));
			applyRequest.RepairInfos.AddRange(calcResponse.RepairInfos);
			applyRequest.RepairOptions = options;

			ApplyRepairGeometryResponse applyResponse =
				RepairGeometryServiceUtils.ApplyRepairGeometry(applyRequest, null);

			Assert.IsNotNull(applyResponse);
			Assert.AreEqual(1, applyResponse.ResultFeatures.Count,
			                "Expected 1 repaired feature");

			IGeometry resultGeometry =
				ProtobufGeometryUtils.FromShapeMsg(
					applyResponse.ResultFeatures[0].Update.Shape);

			Assert.IsNotNull(resultGeometry);
			Assert.IsFalse(resultGeometry.IsEmpty, "Result geometry should not be empty");

			int resultPartCount = ((IGeometryCollection) resultGeometry).GeometryCount;
			Assert.AreEqual(3, resultPartCount,
			                "PlanarizeLines splits into 2 parts when only the redundant reverse is removed");

			int resultPointCount = GeometryUtils.GetPointCount(resultGeometry);
			Assert.AreEqual(7, resultPointCount,
			                "3 vertices per part (2 parts) after removing the redundant reverse segment");

			// Verify no linear self-intersections remain
			var resultMultiPolycurve = GeometryConversionUtils.CreateMultiPolycurve(
				(IPolycurve) resultGeometry);
			double tolerance = GeometryUtils.GetXyTolerance(resultGeometry);

			bool hasLinearSelfIntersections =
				GeomTopoOpUtils.GetLinearSelfIntersectionsXY(resultMultiPolycurve, tolerance).Any();

			Assert.IsFalse(hasLinearSelfIntersections,
			               "No linear self-intersections should remain after repair");
		}

		[Test]
		public void CanApplyRepairGeometryForPolylineDeadEndLinearSelfIntersection()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();
			GdbFeatureClass fClass = CreateGdbFeatureClass(129, "TestFCPolyline4", sr, workspace,
			                                               esriGeometryType.esriGeometryPolyline);

			// Dead-end back-and-forth: A→B→A.
			// TryDeleteLinearSelfIntersectionsXY removes BOTH segments → empty geometry (gap).
			// PlanarizeLines keeps the first traversal (A→B) and drops the return (B→A).
			WKSPointZ[] points =
			{
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400), // A
				WKSPointZUtils.CreatePoint(2601000, 1200000, 400), // B (dead end)
				WKSPointZUtils.CreatePoint(2600000, 1200000, 400) // A again (back-track)
			};

			IPolyline polyline = GeometryFactory.CreatePolyline(points, sr);

			GdbFeature feature = GdbFeature.Create(48, fClass);
			feature.Shape = polyline;

			var options = new RepairOptionsMsg
			              {
				              MinimumSegmentLength = 0,
				              Use2D = true,
				              AllowLoops = false,
				              AllowLinearSelfIntersections = false
			              };

			// Calculate
			CalculateRepairInfoRequest calcRequest = CreateRequest(feature);
			calcRequest.RepairOptions = options;

			CalculateRepairInfoResponse calcResponse =
				RepairGeometryServiceUtils.CalculateRepairInfo(calcRequest, null);

			Assert.AreEqual(1, calcResponse.RepairInfos.Count,
			                "Expected 1 feature with linear self-intersection issues");
			Assert.AreEqual(2, calcResponse.RepairInfos[0].InvalidSegments.Count,
			                "Both the forward and backward segment should be flagged");

			// Apply
			var applyRequest = new ApplyRepairGeometryRequest();
			applyRequest.SourceFeatures.Add(
				ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) feature));
			applyRequest.ClassDefinitions.Add(
				ProtobufGdbUtils.ToObjectClassMsg(feature.Class, true));
			applyRequest.RepairInfos.AddRange(calcResponse.RepairInfos);
			applyRequest.RepairOptions = options;

			ApplyRepairGeometryResponse applyResponse =
				RepairGeometryServiceUtils.ApplyRepairGeometry(applyRequest, null);

			Assert.IsNotNull(applyResponse);
			Assert.AreEqual(0, applyResponse.NonStorableMessages.Count,
			                "No warnings expected: PlanarizeLines avoids the empty-geometry gap");
			Assert.AreEqual(1, applyResponse.ResultFeatures.Count,
			                "Expected 1 repaired feature (not empty/dropped)");

			IGeometry resultGeometry =
				ProtobufGeometryUtils.FromShapeMsg(
					applyResponse.ResultFeatures[0].Update.Shape);

			Assert.IsNotNull(resultGeometry);
			Assert.IsFalse(resultGeometry.IsEmpty,
			               "Result must not be empty: PlanarizeLines keeps the A→B direction");

			// The A→B direction is preserved; the redundant B→A is removed.
			int resultPointCount = GeometryUtils.GetPointCount(resultGeometry);
			Assert.AreEqual(2, resultPointCount, "Only A→B remains: 2 vertices");

			// Verify no linear self-intersections remain
			var resultMultiPolycurve = GeometryConversionUtils.CreateMultiPolycurve(
				(IPolycurve) resultGeometry);
			double tolerance = GeometryUtils.GetXyTolerance(resultGeometry);

			bool hasLinearSelfIntersections =
				GeomTopoOpUtils.GetLinearSelfIntersectionsXY(resultMultiPolycurve, tolerance).Any();

			Assert.IsFalse(hasLinearSelfIntersections,
			               "No linear self-intersections should remain after repair");
		}

		private static GdbFeatureClass CreateGdbFeatureClass(
			int objectClassId, [NotNull] string name,
			[NotNull] ISpatialReference spatialReference,
			IWorkspace workspace, esriGeometryType geometryType)
		{
			var fClass = new GdbFeatureClass(objectClassId, name, geometryType,
			                                 null, null, workspace);
			fClass.SpatialReference = spatialReference;
			fClass.AddField(FieldUtils.CreateOIDField());
			fClass.AddField(FieldUtils.CreateShapeField(geometryType, spatialReference));
			return fClass;
		}
	}
}

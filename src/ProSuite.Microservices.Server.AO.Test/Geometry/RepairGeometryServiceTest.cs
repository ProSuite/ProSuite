using System;
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
		public void CanApplyRepairGeometryForPolygonWithNonAdjacentVertex()
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

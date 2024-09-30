using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geometry.Cracker;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class CrackServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanCrackPolygon()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();

			GdbFeatureClass fClass1 = CreateGdbFeatureClass(123, "TestFC1", sr, workspace);

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			GdbFeature sourceFeature = GdbFeature.Create(42, fClass1);
			sourceFeature.Shape = polygon1;

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			GdbFeature targetFeature = GdbFeature.Create(43, fClass1);
			targetFeature.Shape = polygon2;

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(targetFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class, true);

			CalculateCrackPointsRequest calculationRequest =
				new CalculateCrackPointsRequest()
				{
					ClassDefinitions = { objectClassMsg },
					SourceFeatures = { sourceFeatureMsg },
					TargetFeatures = { targetFeatureMsg }
				};

			CrackOptionsMsg options = new CrackOptionsMsg()
			                          {
				                          SnapToTargetVertices = true,
				                          SnapTolerance = 0.1,
				                          CrackOnlyWithinSameClass = true
			                          };

			calculationRequest.CrackOptions = options;

			CalculateCrackPointsResponse response =
				CrackServiceUtils.CalculateCrackPoints(calculationRequest, null);

			Assert.AreEqual(1, response.CrackPoints.Count);

			List<CrackPointMsg> allCrackPoints =
				response.CrackPoints.SelectMany(kvp => kvp.CrackPoints).ToList();

			List<IPoint> resultPoints =
				ProtobufGeometryUtils.FromShapeMsgList<IPoint>(
					allCrackPoints.Select(cp => cp.Point).ToList());

			Assert.AreEqual(2, resultPoints.Count);

			// Do not depend on the order of the points:
			IPoint point1 = resultPoints.MinElement(p => p.X);
			IPoint point2 = resultPoints.MaxElement(p => p.X);

			Assert.AreEqual(2600500, point1.X, 0.001);
			Assert.AreEqual(1201000, point1.Y, 0.001);

			Assert.AreEqual(2601000, point2.X, 0.001);
			Assert.AreEqual(1200500, point2.Y, 0.001);

			// Now apply:
			var applyRequest = new ApplyCrackPointsRequest();

			applyRequest.ClassDefinitions.AddRange(calculationRequest.ClassDefinitions);
			applyRequest.SourceFeatures.AddRange(calculationRequest.SourceFeatures);
			applyRequest.CrackPoints.Add(response.CrackPoints);
			applyRequest.CrackOptions = options;

			ApplyCrackPointsResponse applyResponse =
				CrackServiceUtils.ApplyCrackPoints(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);
			ResultObjectMsg resultByFeature = applyResponse.ResultFeatures[0];

			GdbObjectReference originalObjRef = new GdbObjectReference(
				(int) resultByFeature.Update.ClassHandle,
				(int) resultByFeature.Update.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), originalObjRef);

			var updatedGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultByFeature.Update.Shape);

			Assert.IsNotNull(updatedGeometry);

			// Source:
			Assert.AreEqual(5, GeometryUtils.GetPointCount(polygon1));

			Assert.AreEqual(1000 * 1000, ((IArea) polygon1).Area);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(7, GeometryUtils.GetPointCount(updatedGeometry));
			Assert.AreEqual(1000 * 1000, ((IArea) updatedGeometry).Area);

			Assert.IsNull(resultByFeature.Insert);
		}

		[Test]
		public void CanCrackBetweenClasses()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();

			GdbFeatureClass fClass1 = CreateGdbFeatureClass(123, "TestFC1", sr, workspace);
			GdbFeatureClass fClass2 = CreateGdbFeatureClass(124, "TestFC2", sr, workspace);

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			GdbFeature sourceFeature = GdbFeature.Create(42, fClass1);
			sourceFeature.Shape = polygon1;

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			GdbFeature targetFeature = GdbFeature.Create(43, fClass2);
			targetFeature.Shape = polygon2;

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(targetFeature);

			var objectClassMsg1 = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class, true);
			var objectClassMsg2 = ProtobufGdbUtils.ToObjectClassMsg(targetFeature.Class, true);

			CalculateCrackPointsRequest calculationRequest =
				new CalculateCrackPointsRequest()
				{
					ClassDefinitions = { objectClassMsg1, objectClassMsg2 },
					SourceFeatures = { sourceFeatureMsg },
					TargetFeatures = { targetFeatureMsg }
				};

			CrackOptionsMsg options = new CrackOptionsMsg()
			                          {
				                          SnapToTargetVertices = true,
				                          SnapTolerance = 0.1,
				                          CrackOnlyWithinSameClass = true
			                          };

			calculationRequest.CrackOptions = options;

			CalculateCrackPointsResponse response =
				CrackServiceUtils.CalculateCrackPoints(calculationRequest, null);

			// Not 'within same class'
			Assert.AreEqual(0, response.CrackPoints.Count);

			// with adapted option:
			options.CrackOnlyWithinSameClass = false;
			response =
				CrackServiceUtils.CalculateCrackPoints(calculationRequest, null);

			Assert.AreEqual(1, response.CrackPoints.Count);

			List<CrackPointMsg> allCrackPoints =
				response.CrackPoints.SelectMany(kvp => kvp.CrackPoints).ToList();

			List<IPoint> resultPoints =
				ProtobufGeometryUtils.FromShapeMsgList<IPoint>(
					allCrackPoints.Select(cp => cp.Point).ToList());

			Assert.AreEqual(2, resultPoints.Count);

			// Do not depend on the order of the points:
			IPoint point1 = resultPoints.MinElement(p => p.X);
			IPoint point2 = resultPoints.MaxElement(p => p.X);

			Assert.AreEqual(2600500, point1.X, 0.001);
			Assert.AreEqual(1201000, point1.Y, 0.001);

			Assert.AreEqual(2601000, point2.X, 0.001);
			Assert.AreEqual(1200500, point2.Y, 0.001);

			// Now apply:
			var applyRequest = new ApplyCrackPointsRequest();

			applyRequest.ClassDefinitions.AddRange(calculationRequest.ClassDefinitions);
			applyRequest.SourceFeatures.AddRange(calculationRequest.SourceFeatures);
			applyRequest.CrackPoints.Add(response.CrackPoints);
			applyRequest.CrackOptions = options;

			ApplyCrackPointsResponse applyResponse =
				CrackServiceUtils.ApplyCrackPoints(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);
			ResultObjectMsg resultByFeature = applyResponse.ResultFeatures[0];

			GdbObjectReference originalObjRef = new GdbObjectReference(
				(int) resultByFeature.Update.ClassHandle,
				(int) resultByFeature.Update.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), originalObjRef);

			var updatedGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultByFeature.Update.Shape);

			Assert.IsNotNull(updatedGeometry);

			// Source:
			Assert.AreEqual(5, GeometryUtils.GetPointCount(polygon1));

			Assert.AreEqual(1000 * 1000, ((IArea) polygon1).Area);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(7, GeometryUtils.GetPointCount(updatedGeometry));
			Assert.AreEqual(1000 * 1000, ((IArea) updatedGeometry).Area);

			Assert.IsNull(resultByFeature.Insert);
		}

		private static GdbFeatureClass CreateGdbFeatureClass(
			int objectClassId, [NotNull] string name,
			[NotNull] ISpatialReference spatialReference,
			IWorkspace workspace)
		{
			var fClass1 =
				new GdbFeatureClass(objectClassId, name, esriGeometryType.esriGeometryPolygon,
				                    null, null, workspace);

			fClass1.SpatialReference = spatialReference;

			fClass1.AddField(FieldUtils.CreateOIDField());
			fClass1.AddField(
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
				                            spatialReference));
			return fClass1;
		}
	}
}

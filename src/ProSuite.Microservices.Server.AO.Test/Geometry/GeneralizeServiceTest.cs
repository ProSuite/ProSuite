using System.Collections.Generic;
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
using ProSuite.Microservices.Server.AO.Geometry.AdvancedGeneralize;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class GeneralizeServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanGeneralizePolygon()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();

			GdbFeatureClass fClass1 =
				CreateGdbFeatureClass(123, "TestFC1", sr, workspace,
				                      esriGeometryType.esriGeometryPolygon);

			var pointArray = new WKSPointZ[]
			                 {
				                 WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				                 WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				                 WKSPointZUtils.CreatePoint(2600500, 1201000, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200499, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200500, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200501, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200000, 400),
				                 WKSPointZUtils.CreatePoint(2600999, 1200000, 400),
				                 WKSPointZUtils.CreatePoint(2600000, 1200000, 400)
			                 };

			// polygon1 has a topologically protected point where it intersects polygon 2
			IPolygon polygon1 = GeometryFactory.CreatePolygon(pointArray, sr);

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

			CalculateRemovableSegmentsRequest calculationRequest =
				new CalculateRemovableSegmentsRequest()
				{
					ClassDefinitions = { objectClassMsg },
					SourceFeatures = { sourceFeatureMsg },
					TargetFeatures = { targetFeatureMsg }
				};

			GeneralizeOptionsMsg options = new GeneralizeOptionsMsg()
			                               {
				                               WeedTolerance = 0.1,
				                               MinimumSegmentLength = 2.0,
				                               ProtectOnlyWithinSameClass = false,
				                               ProtectTopologicalVertices = false
			                               };

			calculationRequest.GeneralizeOptions = options;

			CalculateRemovableSegmentsResponse response =
				GeneralizeServiceUtils.CalculateRemovableSegments(calculationRequest, null);

			// 1 Feature
			Assert.AreEqual(1, response.RemovableSegments.Count);

			IPointCollection deletablePoints = (IPointCollection)
				ProtobufGeometryUtils.FromShapeMsg(response.RemovableSegments[0].PointsToDelete);

			Assert.AreEqual(5, deletablePoints!.PointCount);

			ICollection<ShortSegmentMsg> removableSegments =
				response.RemovableSegments[0].ShortSegments;

			Assert.AreEqual(3, removableSegments.Count);

			// Now with topological protection:
			options.ProtectTopologicalVertices = true;

			response = GeneralizeServiceUtils.CalculateRemovableSegments(calculationRequest, null);

			// 1 Feature
			Assert.AreEqual(1, response.RemovableSegments.Count);

			IPointCollection protectedPoints =
				(IPointCollection) ProtobufGeometryUtils.FromShapeMsg(
					response.RemovableSegments[0].ProtectedPoints);

			Assert.AreEqual(2, protectedPoints!.PointCount);

			deletablePoints = (IPointCollection)
				ProtobufGeometryUtils.FromShapeMsg(response.RemovableSegments[0].PointsToDelete);

			// 5 - 2 protected = 3
			Assert.AreEqual(3, deletablePoints!.PointCount);

			removableSegments = response.RemovableSegments[0].ShortSegments;

			// Two short segments before/after the protected point. Both segments can be deleted by deleting the to/from point.
			Assert.AreEqual(3, removableSegments.Count);

			// Now apply:
			var applyRequest = new ApplySegmentRemovalRequest();

			applyRequest.ClassDefinitions.AddRange(calculationRequest.ClassDefinitions);
			applyRequest.SourceFeatures.AddRange(calculationRequest.SourceFeatures);
			applyRequest.RemovableSegments.Add(response.RemovableSegments);
			applyRequest.GeneralizeOptions = options.Clone();

			// First just weed (3 points)
			applyRequest.GeneralizeOptions.MinimumSegmentLength = -1;

			ApplySegmentRemovalResponse applyResponse =
				GeneralizeServiceUtils.ApplySegmentRemoval(applyRequest, null);

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
			Assert.AreEqual(10, GeometryUtils.GetPointCount(polygon1));

			Assert.AreEqual(1000 * 1000, ((IArea) polygon1).Area);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(7, GeometryUtils.GetPointCount(updatedGeometry));
			Assert.AreEqual(1000 * 1000, ((IArea) updatedGeometry).Area);

			Assert.IsNull(resultByFeature.Insert);

			// Now remove short segments (3 segments)
			applyRequest.GeneralizeOptions.MinimumSegmentLength = -1;
			applyRequest.GeneralizeOptions.MinimumSegmentLength = 2.0;

			applyResponse =
				GeneralizeServiceUtils.ApplySegmentRemoval(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);
			resultByFeature = applyResponse.ResultFeatures[0];
			updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(resultByFeature.Update.Shape);

			Assert.IsNotNull(updatedGeometry);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(7, GeometryUtils.GetPointCount(updatedGeometry));
			Assert.AreEqual(1000 * 1000, ((IArea) updatedGeometry).Area);

			Assert.IsNull(resultByFeature.Insert);
		}

		[Test]
		public void CanGeneralizeWithProtectionBetweenClasses()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			WorkspaceMock workspace = new WorkspaceMock();

			GdbFeatureClass fClass1 =
				CreateGdbFeatureClass(123, "TestFC1", sr, workspace,
				                      esriGeometryType.esriGeometryPolygon);
			GdbFeatureClass fClass2 =
				CreateGdbFeatureClass(124, "TestFC2", sr, workspace,
				                      esriGeometryType.esriGeometryPolygon);

			var pointArray = new WKSPointZ[]
			                 {
				                 WKSPointZUtils.CreatePoint(2600000, 1200000, 400),
				                 WKSPointZUtils.CreatePoint(2600000, 1201000, 400),
				                 WKSPointZUtils.CreatePoint(2600500, 1201000, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1201000, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200499, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200500, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200501, 400),
				                 WKSPointZUtils.CreatePoint(2601000, 1200000, 400),
				                 WKSPointZUtils.CreatePoint(2600999, 1200000, 400),
				                 WKSPointZUtils.CreatePoint(2600000, 1200000, 400)
			                 };

			// polygon1 has a topologically protected point where it intersects polygon 2
			IPolygon polygon1 = GeometryFactory.CreatePolygon(pointArray, sr);

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

			CalculateRemovableSegmentsRequest calculationRequest =
				new CalculateRemovableSegmentsRequest()
				{
					ClassDefinitions = { objectClassMsg1, objectClassMsg2 },
					SourceFeatures = { sourceFeatureMsg },
					TargetFeatures = { targetFeatureMsg }
				};

			GeneralizeOptionsMsg options = new GeneralizeOptionsMsg()
			                               {
				                               WeedTolerance = 0.1,
				                               MinimumSegmentLength = 2.0,
				                               ProtectOnlyWithinSameClass = true,
				                               ProtectTopologicalVertices = true
			                               };

			calculationRequest.GeneralizeOptions = options;

			CalculateRemovableSegmentsResponse response =
				GeneralizeServiceUtils.CalculateRemovableSegments(calculationRequest, null);

			// 1 Feature
			Assert.AreEqual(1, response.RemovableSegments.Count);

			IPointCollection deletablePoints = (IPointCollection)
				ProtobufGeometryUtils.FromShapeMsg(response.RemovableSegments[0].PointsToDelete);

			// No protection from target in different class:
			Assert.AreEqual(5, deletablePoints!.PointCount);

			ICollection<ShortSegmentMsg> removableSegments =
				response.RemovableSegments[0].ShortSegments;

			Assert.AreEqual(3, removableSegments.Count);

			IPointCollection protectedPoints =
				(IPointCollection) ProtobufGeometryUtils.FromShapeMsg(
					response.RemovableSegments[0].ProtectedPoints);

			Assert.AreEqual(0, protectedPoints?.PointCount ?? 0);

			// Now apply:
			var applyRequest = new ApplySegmentRemovalRequest();

			applyRequest.ClassDefinitions.AddRange(calculationRequest.ClassDefinitions);
			applyRequest.SourceFeatures.AddRange(calculationRequest.SourceFeatures);
			applyRequest.RemovableSegments.Add(response.RemovableSegments);
			applyRequest.GeneralizeOptions = options.Clone();

			// First just weed (3 points)
			applyRequest.GeneralizeOptions.MinimumSegmentLength = -1;

			ApplySegmentRemovalResponse applyResponse =
				GeneralizeServiceUtils.ApplySegmentRemoval(applyRequest, null);

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
			Assert.AreEqual(10, GeometryUtils.GetPointCount(polygon1));

			Assert.AreEqual(1000 * 1000, ((IArea) polygon1).Area);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(5, GeometryUtils.GetPointCount(updatedGeometry));
			Assert.AreEqual(1000 * 1000, ((IArea) updatedGeometry).Area);

			Assert.IsNull(resultByFeature.Insert);

			// Now remove short segments (3 segments)
			applyRequest.GeneralizeOptions.WeedTolerance = -1;
			applyRequest.GeneralizeOptions.MinimumSegmentLength = 2.0;

			applyResponse =
				GeneralizeServiceUtils.ApplySegmentRemoval(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);
			resultByFeature = applyResponse.ResultFeatures[0];
			updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(resultByFeature.Update.Shape);

			Assert.IsNotNull(updatedGeometry);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(5, GeometryUtils.GetPointCount(updatedGeometry));
			Assert.AreEqual(1000 * 1000, ((IArea) updatedGeometry).Area);

			Assert.IsNull(resultByFeature.Insert);
		}

		private static GdbFeatureClass CreateGdbFeatureClass(
			int objectClassId, [NotNull] string name,
			[NotNull] ISpatialReference spatialReference,
			IWorkspace workspace, esriGeometryType geometryType)
		{
			var fClass1 =
				new GdbFeatureClass(objectClassId, name, geometryType,
				                    null, null, workspace);

			fClass1.SpatialReference = spatialReference;

			fClass1.AddField(FieldUtils.CreateOIDField());
			fClass1.AddField(
				FieldUtils.CreateShapeField(geometryType,
				                            spatialReference));
			return fClass1;
		}
	}
}

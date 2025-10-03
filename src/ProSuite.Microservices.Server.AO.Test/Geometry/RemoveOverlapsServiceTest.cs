using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class RemoveOverlapsServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanCalculateOverlaps()
		{
			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon);

			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			fClass.SpatialReference = sr;

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			GdbFeature sourceFeature = GdbFeature.Create(42, fClass);
			sourceFeature.Shape = polygon1;

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			GdbFeature targetFeature = GdbFeature.Create(43, fClass);
			targetFeature.Shape = polygon2;

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) targetFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class);

			CalculateOverlapsRequest calculationRequest = new CalculateOverlapsRequest()
			                                              {
				                                              ClassDefinitions =
				                                              {
					                                              objectClassMsg
				                                              },
				                                              SourceFeatures =
				                                              {
					                                              sourceFeatureMsg
				                                              },
				                                              TargetFeatures =
				                                              {
					                                              targetFeatureMsg
				                                              }
			                                              };

			CalculateOverlapsResponse response =
				RemoveOverlapsServiceUtils.CalculateOverlaps(calculationRequest, null);

			Assert.AreEqual(1, response.Overlaps.Count);

			List<ShapeMsg> shapeBufferList =
				response.Overlaps.SelectMany(kvp => kvp.Overlaps).ToList();

			List<IPolygon> resultPolys =
				ProtobufGeometryUtils.FromShapeMsgList<IPolygon>(shapeBufferList);

			Assert.AreEqual(1, resultPolys.Count);

			Assert.AreEqual(1000 * 1000 / 4, ((IArea) resultPolys[0]).Area);

			// Now the removal:
			RemoveOverlapsRequest removeRequest = new RemoveOverlapsRequest();

			removeRequest.ClassDefinitions.AddRange(calculationRequest.ClassDefinitions);
			removeRequest.SourceFeatures.AddRange(calculationRequest.SourceFeatures);
			removeRequest.Overlaps.Add(response.Overlaps);

			RemoveOverlapsResponse removeResponse =
				RemoveOverlapsServiceUtils.RemoveOverlaps(removeRequest);

			Assert.AreEqual(1, removeResponse.ResultsByFeature.Count);
			ResultGeometriesByFeature resultByFeature = removeResponse.ResultsByFeature[0];

			GdbObjectReference originalObjRef = new GdbObjectReference(
				(int) resultByFeature.OriginalFeatureRef.ClassHandle,
				(int) resultByFeature.OriginalFeatureRef.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), originalObjRef);

			var updatedGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultByFeature.UpdatedGeometry);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedGeometry).Area);

			Assert.AreEqual(0, resultByFeature.NewGeometries.Count);
		}

		[Test]
		public void CanRemoveOverlaps()
		{
			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon)
				{
					SpatialReference = sr
				};

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			GdbFeature sourceFeature = GdbFeature.Create(42, fClass);
			sourceFeature.Shape = polygon1;

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			GdbFeature targetFeature = GdbFeature.Create(43, fClass);
			targetFeature.Shape = polygon2;

			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			overlap.SpatialReference = sr;

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) targetFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class);

			var removeRequest = new RemoveOverlapsRequest()
			                    {
				                    ClassDefinitions =
				                    {
					                    objectClassMsg
				                    },
				                    SourceFeatures =
				                    {
					                    sourceFeatureMsg
				                    },
				                    UpdatableTargetFeatures =
				                    {
					                    targetFeatureMsg
				                    }
			                    };

			var overlapsMsg = new OverlapMsg();
			overlapsMsg.OriginalFeatureRef = new GdbObjRefMsg()
			                                 {
				                                 ClassHandle = sourceFeatureMsg.ClassHandle,
				                                 ObjectId = sourceFeatureMsg.ObjectId
			                                 };

			overlapsMsg.Overlaps.Add(ProtobufGeometryUtils.ToShapeMsg(overlap));

			removeRequest.Overlaps.Add(overlapsMsg);

			RemoveOverlapsResponse removeResponse =
				RemoveOverlapsServiceUtils.RemoveOverlaps(removeRequest);

			Assert.AreEqual(1, removeResponse.ResultsByFeature.Count);
			ResultGeometriesByFeature resultByFeature = removeResponse.ResultsByFeature[0];

			GdbObjectReference originalObjRef = new GdbObjectReference(
				(int) resultByFeature.OriginalFeatureRef.ClassHandle,
				(int) resultByFeature.OriginalFeatureRef.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), originalObjRef);

			IGeometry updatedGeometry =
				ProtobufGeometryUtils.FromShapeMsg(resultByFeature.UpdatedGeometry);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedGeometry).Area);

			Assert.AreEqual(0, resultByFeature.NewGeometries.Count);

			IFeature updatedTarget =
				ProtobufConversionUtils.FromGdbObjectMsgList(removeResponse.TargetFeaturesToUpdate,
				                                             removeRequest.ClassDefinitions)
				                       .Single();

			int pointCount = GeometryUtils.GetPointCount(updatedTarget.Shape);
			Assert.AreEqual(5 + 2, pointCount);
		}
	}
}

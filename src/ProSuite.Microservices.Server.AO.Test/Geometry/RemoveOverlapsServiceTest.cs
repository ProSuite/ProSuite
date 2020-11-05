using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Server.AO.Geodatabase;
using ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class RemoveOverlapsServiceTest
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
		public void CanCalculateOverlaps()
		{
			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon, null);

			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			GdbFeature sourceFeature = new GdbFeature(42, fClass)
			                           {
				                           Shape = polygon1
			                           };

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			GdbFeature targetFeature = new GdbFeature(43, fClass)
			                           {
				                           Shape = polygon2
			                           };

			var sourceFeatureMsg = ProtobufConversionUtils.ToGdbObjectMsg(sourceFeature);
			var targetFeatureMsg = ProtobufConversionUtils.ToGdbObjectMsg(targetFeature);

			var objectClassMsg = ProtobufConversionUtils.ToObjectClassMsg(sourceFeature.Class);

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

			List<IPolygon> resultPolys =
				ProtobufConversionUtils.FromShapeMsgList<IPolygon>(response.Overlaps);

			Assert.AreEqual(1, resultPolys.Count);

			Assert.AreEqual(1000 * 1000 / 4, ((IArea) resultPolys[0]).Area);

			// Now the removal:
			RemoveOverlapsRequest removeRequest = new RemoveOverlapsRequest();

			removeRequest.ClassDefinitions.AddRange(calculationRequest.ClassDefinitions);
			removeRequest.SourceFeatures.AddRange(calculationRequest.SourceFeatures);
			removeRequest.Overlaps.AddRange(response.Overlaps);

			RemoveOverlapsResponse removeResponse =
				RemoveOverlapsServiceUtils.RemoveOverlaps(removeRequest);

			Assert.AreEqual(1, removeResponse.ResultsByFeature.Count);
			ResultGeometriesByFeature resultByFeature = removeResponse.ResultsByFeature[0];

			GdbObjectReference originalObjRef = new GdbObjectReference(
				resultByFeature.OriginalFeatureRef.ClassHandle,
				resultByFeature.OriginalFeatureRef.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), originalObjRef);

			var updatedGeometry =
				ProtobufConversionUtils.FromShapeMsg(resultByFeature.UpdatedGeometry);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedGeometry).Area);

			Assert.AreEqual(0, resultByFeature.NewGeometries.Count);
		}

		[Test]
		public void CanRemoveOverlaps()
		{
			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon, null);

			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			GdbFeature sourceFeature = new GdbFeature(42, fClass)
			                           {
				                           Shape = polygon1
			                           };

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			GdbFeature targetFeature = new GdbFeature(43, fClass)
			                           {
				                           Shape = polygon2
			                           };

			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			overlap.SpatialReference = sr;

			var sourceFeatureMsg = ProtobufConversionUtils.ToGdbObjectMsg(sourceFeature);
			var targetFeatureMsg = ProtobufConversionUtils.ToGdbObjectMsg(targetFeature);

			var objectClassMsg = ProtobufConversionUtils.ToObjectClassMsg(sourceFeature.Class);

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
				                    },
				                    Overlaps =
				                    {
					                    ProtobufConversionUtils.ToShapeMsg(overlap)
				                    }
			                    };

			RemoveOverlapsResponse removeResponse =
				RemoveOverlapsServiceUtils.RemoveOverlaps(removeRequest);

			Assert.AreEqual(1, removeResponse.ResultsByFeature.Count);
			ResultGeometriesByFeature resultByFeature = removeResponse.ResultsByFeature[0];

			GdbObjectReference originalObjRef = new GdbObjectReference(
				resultByFeature.OriginalFeatureRef.ClassHandle,
				resultByFeature.OriginalFeatureRef.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), originalObjRef);

			IGeometry updatedGeometry =
				ProtobufConversionUtils.FromShapeMsg(resultByFeature.UpdatedGeometry);

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

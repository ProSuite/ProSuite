using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geometry.AdvancedReshape;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class AdvancedReshapeServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanAdvancedReshapePolygon()
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

			IFeature sourceFeature = GdbFeature.Create(42, fClass);
			sourceFeature.Shape = polygon1;

			IPath reshapePath = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600500, 1200000, sr),
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601000, 1200500, sr));

			reshapePath.SpatialReference = sr;

			IPolyline reshapePolyline = GeometryFactory.CreatePolyline(reshapePath);

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var reshapePaths = ProtobufGeometryUtils.ToShapeMsg(reshapePolyline);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class);

			AdvancedReshapeRequest request = new AdvancedReshapeRequest()
			                                 {
				                                 ClassDefinitions =
				                                 {
					                                 objectClassMsg
				                                 },
				                                 Features =
				                                 {
					                                 sourceFeatureMsg
				                                 },
				                                 ReshapePaths = reshapePaths
			                                 };

			AdvancedReshapeResponse response = AdvancedReshapeServiceUtils.Reshape(request);

			Assert.AreEqual(1, response.ResultFeatures.Count);

			GdbObjectMsg resultFeatureMsg = response.ResultFeatures[0].Update;

			Assert.AreEqual(sourceFeature.OID, resultFeatureMsg.ObjectId);
			Assert.AreEqual(sourceFeature.Class.ObjectClassID, resultFeatureMsg.ClassHandle);

			var resultPoly = (IPolygon) ProtobufGeometryUtils.FromShapeMsg(resultFeatureMsg.Shape);

			Assert.NotNull(resultPoly);

			double oneQuarter = 1000d * 1000d / 4d;
			Assert.AreEqual(3 * oneQuarter, ((IArea) resultPoly).Area);

			// Non-default side:
			request.UseNonDefaultReshapeSide = true;

			response = AdvancedReshapeServiceUtils.Reshape(request);

			Assert.AreEqual(1, response.ResultFeatures.Count);
			resultFeatureMsg = response.ResultFeatures[0].Update;

			resultPoly = (IPolygon) ProtobufGeometryUtils.FromShapeMsg(resultFeatureMsg.Shape);

			Assert.NotNull(resultPoly);

			Assert.AreEqual(oneQuarter, ((IArea) resultPoly).Area);
		}

		[Test]
		public void CanAdvancedReshapePolyline()
		{
			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolyline);

			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			fClass.SpatialReference = sr;

			IPolyline sourcePolyline = CreatePolyline(
				GeometryFactory.CreatePoint(2600500, 1200000, sr),
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601000, 1200500, sr));

			IFeature sourceFeature = GdbFeature.Create(42, fClass);
			sourceFeature.Shape = sourcePolyline;

			IPolyline sourceAdjacentPolyline = CreatePolyline(
				GeometryFactory.CreatePoint(2601000, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1200000, sr));

			IFeature sourceAdjacentFeature = GdbFeature.Create(43, fClass);
			sourceAdjacentFeature.Shape = sourceAdjacentPolyline;

			IPolyline reshapePolyline = CreatePolyline(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2600500, 1201000, sr));

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var reshapePaths = ProtobufGeometryUtils.ToShapeMsg(reshapePolyline);
			var sourceAdjacentFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceAdjacentFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class);

			AdvancedReshapeRequest request = new AdvancedReshapeRequest()
			                                 {
				                                 ClassDefinitions =
				                                 {
					                                 objectClassMsg
				                                 },
				                                 Features =
				                                 {
					                                 sourceFeatureMsg
				                                 },
				                                 ReshapePaths = reshapePaths,
				                                 AllowOpenJawReshape = true,
				                                 MoveOpenJawEndJunction = true,
				                                 PotentiallyConnectedFeatures =
				                                 {
					                                 sourceAdjacentFeatureMsg
				                                 }
			                                 };

			AdvancedReshapeResponse response = AdvancedReshapeServiceUtils.Reshape(request);

			Assert.AreEqual(2, response.ResultFeatures.Count);

			GdbObjectMsg resultFeatureMsg = response.ResultFeatures[1].Update;

			Assert.AreEqual(sourceFeature.OID, resultFeatureMsg.ObjectId);
			Assert.AreEqual(sourceFeature.Class.ObjectClassID, resultFeatureMsg.ClassHandle);

			var resultPolyline =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(resultFeatureMsg.Shape);

			Assert.NotNull(resultPolyline);

			Assert.IsTrue(GeometryUtils.AreEqual(resultPolyline.ToPoint, reshapePolyline.ToPoint));

			GdbObjectMsg resultAdjacentFeatureMsg = response.ResultFeatures[0].Update;
			var resultAdjacentPolyline =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(resultAdjacentFeatureMsg.Shape);

			Assert.NotNull(resultAdjacentPolyline);
			Assert.IsTrue(
				GeometryUtils.AreEqual(resultAdjacentPolyline.FromPoint, reshapePolyline.ToPoint));

			// Non-default side:
			request.UseNonDefaultReshapeSide = true;

			response = AdvancedReshapeServiceUtils.Reshape(request);

			Assert.AreEqual(1, response.ResultFeatures.Count);
			resultFeatureMsg = response.ResultFeatures[0].Update;

			resultPolyline = (IPolyline) ProtobufGeometryUtils.FromShapeMsg(resultFeatureMsg.Shape);

			Assert.NotNull(resultPolyline);

			Assert.IsTrue(
				GeometryUtils.AreEqual(resultPolyline.FromPoint, reshapePolyline.ToPoint));
		}

		private static IPolyline CreatePolyline(params IPoint[] vertices)
		{
			IPath path = GeometryFactory.CreatePath(vertices);

			IPolyline polyline = GeometryFactory.CreatePolyline(path);

			return polyline;
		}

		[Test]
		public void CanGetOpenJawReshapeLineReplaceEndPoint()
		{
			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolyline);

			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LN02);

			fClass.SpatialReference = sr;

			IPath sourcePath = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600500, 1200000, sr),
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601000, 1200500, sr));

			sourcePath.SpatialReference = sr;

			IPolyline sourcePolyline = GeometryFactory.CreatePolyline(sourcePath);

			IFeature sourceFeature = GdbFeature.Create(42, fClass);
			sourceFeature.Shape = sourcePolyline;

			IPath reshapePath = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2600500, 1201000, sr));

			reshapePath.SpatialReference = sr;

			IPolyline reshapePolyline = GeometryFactory.CreatePolyline(reshapePath);

			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var reshapePathMsg = ProtobufGeometryUtils.ToShapeMsg(reshapePolyline);

			var request = new OpenJawReshapeLineReplacementRequest
			              {
				              Feature = sourceFeatureMsg,
				              ReshapePath = reshapePathMsg
			              };

			ShapeMsg response =
				AdvancedReshapeServiceUtils.GetOpenJawReshapeReplaceEndPoint(request);

			IPoint resultPoint = (IPoint) ProtobufGeometryUtils.FromShapeMsg(response);

			Assert.IsTrue(GeometryUtils.AreEqual(sourcePolyline.ToPoint, resultPoint));

			// Non-default side:
			request.UseNonDefaultReshapeSide = true;

			response =
				AdvancedReshapeServiceUtils.GetOpenJawReshapeReplaceEndPoint(request);
			resultPoint = (IPoint) ProtobufGeometryUtils.FromShapeMsg(response);

			Assert.IsTrue(GeometryUtils.AreEqual(sourcePolyline.FromPoint, resultPoint));
		}
	}
}

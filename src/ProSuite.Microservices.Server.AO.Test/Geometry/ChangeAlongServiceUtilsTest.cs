using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.Microservices.Server.AO.Geodatabase;
using ProSuite.Microservices.Server.AO.Geometry.ChangeAlong;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class ChangeAlongServiceUtilsTest
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
		public void CanReshapeAlongNonDefaultSide()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateReshapeLinesRequest calculationRequest =
				CreateCalculateReshapeLinesRequest(sourceFeature, targetFeature);

			CalculateReshapeLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                calculateResponse.ReshapeLinesUsability);
			AssertReshapeLineCount(calculateResponse.ReshapeLines, 2, 2);

			List<ShapeMsg> reshapePathMsgs =
				calculateResponse.ReshapeLines.Select(l => l.Path).ToList();

			List<IPolyline> resultLines =
				ProtobufGeometryUtils.FromShapeMsgList<IPolyline>(reshapePathMsgs);

			int insideLineIndex = resultLines
			                      .Select((reshapePath, index) => (reshapePath, index))
			                      .First(rl => GeometryUtils.InteriorIntersects(
				                             rl.reshapePath, sourceFeature.Shape)).index;

			var insideLines =
				resultLines.Where(rl => GeometryUtils.InteriorIntersects(rl, sourceFeature.Shape))
				           .ToList();

			Assert.AreEqual(1, insideLines.Count);

			Assert.AreEqual(1000, (insideLines[0]).Length);

			//
			// Reshape the default side:
			//
			ApplyReshapeLinesRequest applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			applyRequest.UseNonDefaultReshapeSide = false;

			ApplyReshapeLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			GdbObjectReference resultFeatureObjRef = new GdbObjectReference(
				updatedFeatureMsg.ClassHandle, updatedFeatureMsg.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), resultFeatureObjRef);

			IGeometry updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedGeometry).Area);

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                applyResponse.ReshapeLinesUsability);

			//
			// Reshape the non-default side:
			//
			applyRequest.UseNonDefaultReshapeSide = true;
			applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			resultFeatureObjRef = new GdbObjectReference(
				updatedFeatureMsg.ClassHandle, updatedFeatureMsg.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), resultFeatureObjRef);

			updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual((double) 1000 * 1000 * 1 / 4, ((IArea) updatedGeometry).Area);
		}

		[Test]
		public void CanReshapeAlongInsertTargetVertices()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateReshapeLinesRequest calculationRequest =
				CreateCalculateReshapeLinesRequest(sourceFeature, targetFeature);

			CalculateReshapeLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                calculateResponse.ReshapeLinesUsability);
			AssertReshapeLineCount(calculateResponse.ReshapeLines, 2, 2);

			int insideLineIndex =
				GetInsideReshapeLineIndex(calculateResponse.ReshapeLines, sourceFeature.Shape);

			IPolyline insideLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(
					calculateResponse.ReshapeLines[insideLineIndex].Path);

			Assert.NotNull(insideLine);
			Assert.AreEqual(1000.0, insideLine.Length);

			//
			// Reshape the default side:
			//
			ApplyReshapeLinesRequest applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = true;
			applyRequest.UseNonDefaultReshapeSide = false;

			ApplyReshapeLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(2, applyResponse.ResultFeatures.Count);

			ResultFeatureMsg sourceResultMsg =
				applyResponse.ResultFeatures.First(
					f => f.UpdatedFeature.ObjectId == sourceFeature.OID);

			IGeometry updatedSourceGeometry =
				ProtobufGeometryUtils.FromShapeMsg(sourceResultMsg.UpdatedFeature.Shape);

			Assert.IsNotNull(updatedSourceGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedSourceGeometry).Area);

			ResultFeatureMsg targetResultMsg =
				applyResponse.ResultFeatures.First(
					f => f.UpdatedFeature.ObjectId == targetFeature.OID);

			IGeometry updatedTargetGeometry =
				ProtobufGeometryUtils.FromShapeMsg(targetResultMsg.UpdatedFeature.Shape);

			Assert.AreEqual(GeometryUtils.GetPointCount(targetFeature.Shape) + 2,
			                GeometryUtils.GetPointCount(updatedTargetGeometry));

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1);
			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                applyResponse.ReshapeLinesUsability);
		}

		[Test]
		public void CanReshapeAlongMinimalTolerance()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			// Insert an extra point:
			IPoint targetAlmostIntersectPoint = GeometryFactory.CreatePoint(
				2601000.001, 1200500.0, sourceFeature.Shape.SpatialReference);

			bool pointInserted = ReshapeUtils.EnsurePointsExistInTarget(
				targetFeature.Shape,
				new[] {targetAlmostIntersectPoint}, 0.001);

			Assert.IsTrue(pointInserted);
			Assert.AreEqual(6, GeometryUtils.GetPointCount(targetFeature.Shape));

			CalculateReshapeLinesRequest calculationRequest =
				CreateCalculateReshapeLinesRequest(sourceFeature, targetFeature);

			CalculateReshapeLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                calculateResponse.ReshapeLinesUsability);
			AssertReshapeLineCount(calculateResponse.ReshapeLines, 2, 2);

			int insideLineIndex =
				GetInsideReshapeLineIndex(calculateResponse.ReshapeLines, sourceFeature.Shape);

			IPolyline insideLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(
					calculateResponse.ReshapeLines[insideLineIndex].Path);

			Assert.NotNull(insideLine);
			Assert.AreNotEqual(1000.0, (insideLine).Length);

			// NOTE: If IntersectionUtils.UseCustomIntersect = true the from point is exactly on the inserted point
			//       Otherwise it's somewhere between - so we cannot do the exact comparison
			Assert.AreNotEqual(insideLine.FromPoint.X, 2601000.0);
			Assert.AreNotEqual(insideLine.ToPoint.X, 2601000.0);

			//
			// Reshape the default side:
			ApplyReshapeLinesRequest applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			applyRequest.UseNonDefaultReshapeSide = false;

			ApplyReshapeLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;
			IGeometry updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.Greater(((IArea) updatedGeometry).Area, 1000.0 * 1000.0 * 3 / 4);

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                applyResponse.ReshapeLinesUsability);

			//
			//
			// The same using the minimum tolerance:
			//
			calculationRequest.Tolerance = 0;

			calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                calculateResponse.ReshapeLinesUsability);
			AssertReshapeLineCount(calculateResponse.ReshapeLines, 2, 2);

			insideLineIndex =
				GetInsideReshapeLineIndex(calculateResponse.ReshapeLines, sourceFeature.Shape);

			insideLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(
					calculateResponse.ReshapeLines[insideLineIndex].Path);

			Assert.NotNull(insideLine);
			Assert.AreEqual(1000.0, insideLine.Length, 0.000000001);

			// NOTE: If IntersectionUtils.UseCustomIntersect = true the from point is exactly on the inserted point
			//       Otherwise it's somewhere between - so we cannot do the exact comparison
			Assert.AreEqual(2601000.0, insideLine.FromPoint.X);
			Assert.AreEqual(1200500.0, insideLine.FromPoint.Y);

			//
			// Apply with minimum tolerance:
			applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			applyRequest.UseNonDefaultReshapeSide = false;

			applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedGeometry).Area);

			// Check the new remaining reshape line (outside)
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1);
			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                applyResponse.ReshapeLinesUsability);

			IPolyline outsideLine = (IPolyline) ProtobufGeometryUtils.FromShapeMsg(
				applyResponse.NewReshapeLines[0].Path);

			Assert.NotNull(outsideLine);
			Assert.AreEqual(3000.0, outsideLine.Length, 0.000000001);

			// NOTE: If IntersectionUtils.UseCustomIntersect = true the from point is exactly on the inserted point
			//       Otherwise it's somewhere between - so we cannot do the exact comparison
			Assert.AreEqual(2601000.0, outsideLine.ToPoint.X);
			Assert.AreEqual(1200500.0, outsideLine.ToPoint.Y);
		}

		[Test]
		public void CanReshapeAlongBufferedTarget()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateReshapeLinesRequest calculationRequest =
				CreateCalculateReshapeLinesRequest(sourceFeature, targetFeature);

			double minSegmentLength = 2;

			calculationRequest.TargetBufferOptions = new TargetBufferOptionsMsg();
			calculationRequest.TargetBufferOptions.BufferDistance = 10;
			calculationRequest.TargetBufferOptions.BufferMinimumSegmentLength = minSegmentLength;

			CalculateReshapeLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                calculateResponse.ReshapeLinesUsability);

			AssertReshapeLineCount(calculateResponse.ReshapeLines, 4, 4);

			List<ShapeMsg> reshapePathMsgs =
				calculateResponse.ReshapeLines.Select(l => l.Path).ToList();

			List<IPolyline> resultLines =
				ProtobufGeometryUtils.FromShapeMsgList<IPolyline>(reshapePathMsgs);

			int insideLineIndex = resultLines
			                      .Select((reshapePath, index) => (reshapePath, index))
			                      .OrderBy(rl => rl.reshapePath.Length)
			                      .First(rl => GeometryUtils.InteriorIntersects(
				                             rl.reshapePath, sourceFeature.Shape)).index;

			var insideLines =
				resultLines.Where(rl => GeometryUtils.InteriorIntersects(rl, sourceFeature.Shape))
				           .ToList();

			Assert.AreEqual(2, insideLines.Count);

			foreach (IPolyline resultLine in resultLines)
			{
				Assert.AreNotEqual(1000, resultLine.Length);
				Assert.AreEqual(
					0, GeometryUtils.GetShortSegments(resultLine, minSegmentLength).Count);
			}

			//
			// Reshape the default side:
			//
			ApplyReshapeLinesRequest applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			applyRequest.UseNonDefaultReshapeSide = false;

			ApplyReshapeLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			GdbObjectReference resultFeatureObjRef = new GdbObjectReference(
				updatedFeatureMsg.ClassHandle, updatedFeatureMsg.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), resultFeatureObjRef);

			IGeometry updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			// The shortest reshape line results in a greater remaining area:
			Assert.IsNotNull(updatedGeometry);
			Assert.Greater(((IArea) updatedGeometry).Area, 1000 * 1000 * 3 / (double) 4);

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 3, 3);
			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                applyResponse.ReshapeLinesUsability);
		}

		[Test]
		public void CanReshapeAlongFilteredLinesByExtent()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateReshapeLinesRequest calculationRequest =
				CreateCalculateReshapeLinesRequest(sourceFeature, targetFeature);

			IEnvelope visibleExtent = sourceFeature.Shape.Envelope;
			visibleExtent.Expand(10, 10, false);
			calculationRequest.FilterOptions = new ReshapeLineFilterOptionsMsg();
			calculationRequest.FilterOptions.ClipLinesOnVisibleExtent = true;
			calculationRequest.FilterOptions.VisibleExtents.Add(
				ProtobufGeometryUtils.ToEnvelopeMsg(visibleExtent));

			CalculateReshapeLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual((int) ReshapeAlongCurveUsability.CanReshape,
			                calculateResponse.ReshapeLinesUsability);
			// 1 inside-line and 2 extent-intersecting dangles to the outside within the extent.
			AssertReshapeLineCount(calculateResponse.ReshapeLines, 3, 1);

			int insideLineIndex =
				GetInsideReshapeLineIndex(calculateResponse.ReshapeLines, sourceFeature.Shape);

			IPolyline insideLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(
					calculateResponse.ReshapeLines[insideLineIndex].Path);

			Assert.NotNull(insideLine);
			Assert.AreEqual(1000, insideLine.Length);

			//
			// Reshape the non-default side (should be possible):
			//
			ApplyReshapeLinesRequest applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			applyRequest.UseNonDefaultReshapeSide = true;

			ApplyReshapeLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			GdbObjectReference resultFeatureObjRef = new GdbObjectReference(
				updatedFeatureMsg.ClassHandle, updatedFeatureMsg.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), resultFeatureObjRef);

			IGeometry updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 1 / 4, ((IArea) updatedGeometry).Area);

			// Check the new reshape line - there should be 2 non-reshapable dangles
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 2, 0);
			Assert.AreEqual(ReshapeAlongCurveUsability.InsufficientOrAmbiguousReshapeCurves,
			                (ReshapeAlongCurveUsability) applyResponse.ReshapeLinesUsability);
		}

		[Test]
		public void CanReshapeAlongFilteredLinesByNoTargetOverlap()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateReshapeLinesRequest calculationRequest =
				CreateCalculateReshapeLinesRequest(sourceFeature, targetFeature);

			//
			// Filter by 'no target overlaps':
			//
			calculationRequest.FilterOptions = new ReshapeLineFilterOptionsMsg();
			calculationRequest.FilterOptions.ExcludeResultingInOverlaps = true;

			CalculateReshapeLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateReshapeLines(calculationRequest, null);

			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) calculateResponse.ReshapeLinesUsability);

			// 1 inside-source line and 1 (reshapable, but filtered) outside-source line.
			AssertReshapeLineCount(calculateResponse.ReshapeLines, 2, 2, 1);

			int insideLineIndex =
				GetInsideReshapeLineIndex(calculateResponse.ReshapeLines, sourceFeature.Shape);

			IPolyline insideLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(
					calculateResponse.ReshapeLines[insideLineIndex].Path);

			Assert.NotNull(insideLine);
			Assert.AreEqual(1000, insideLine.Length);

			//
			// Reshape the default-side:
			ApplyReshapeLinesRequest applyRequest = new ApplyReshapeLinesRequest();

			applyRequest.ReshapeLines.Add(calculateResponse.ReshapeLines[insideLineIndex]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			applyRequest.UseNonDefaultReshapeSide = false;

			ApplyReshapeLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			IGeometry updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedGeometry).Area);

			// Check the new reshape line - there should be 1 reshapable, but filtered line
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1, 1);

			// Technically the curve could be used to reshape (filtered out is not evaluated for usability)
			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) applyResponse.ReshapeLinesUsability);

			// Reshape the non-default side. This should probably not be possible but as it is currently
			// just labelled a reshape line filter, it can be justified to reshape anyway...
			applyRequest.UseNonDefaultReshapeSide = true;

			applyResponse =
				ChangeAlongServiceUtils.ApplyReshapeLines(applyRequest, null);

			Assert.AreEqual(1, applyResponse.ResultFeatures.Count);

			updatedFeatureMsg = applyResponse.ResultFeatures[0].UpdatedFeature;

			updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 1 / 4, ((IArea) updatedGeometry).Area);

			// Check the new reshape line - there should be 1 reshapable, but filtered line
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1, 1);

			// Technically the curve could be used to reshape (filtered out is not evaluated for usability)
			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) applyResponse.ReshapeLinesUsability);
		}

		///  <summary>
		///  Returns two overlapping square polygons:
		/// 
		///         _____________
		///        |             |
		///        |             |
		///  ______|______       |
		/// |      |    target   |
		/// |      |      |      |
		/// |      |______|______|
		/// |             |
		/// |	source    |
		/// |_____________|
		///  
		///  </summary>
		///  <param name="sourceFeature"></param>
		///  <param name="targetFeature"></param>
		///  <returns></returns>
		private static void GetOverlappingPolygons(out GdbFeature sourceFeature,
		                                           out GdbFeature targetFeature)
		{
			var fClass =
				new GdbFeatureClass(123, "TestFC", esriGeometryType.esriGeometryPolygon);

			var sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LN02);

			fClass.SpatialReference = sr;

			IPolygon polygon1 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600000, 1200000, sr),
				GeometryFactory.CreatePoint(2601000, 1201000, sr));

			polygon1.SpatialReference = sr;

			sourceFeature = new GdbFeature(42, fClass)
			                {
				                Shape = polygon1
			                };

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			targetFeature = new GdbFeature(43, fClass)
			                {
				                Shape = polygon2
			                };
		}

		private static void AssertReshapeLineCount(RepeatedField<ReshapeLineMsg> reshapeLines,
		                                           int expectedTotal,
		                                           int expectedReshapable,
		                                           int expectedFiltered = -1)
		{
			Assert.AreEqual(expectedTotal, reshapeLines.Count);
			Assert.AreEqual(expectedReshapable, reshapeLines.Count(l => l.CanReshape));

			if (expectedFiltered >= 0)
			{
				Assert.AreEqual(expectedFiltered, reshapeLines.Count(l => l.IsFiltered));
			}
		}

		private static int GetInsideReshapeLineIndex(RepeatedField<ReshapeLineMsg> reshapeLines,
		                                             IGeometry intersectingGeometry)
		{
			List<ShapeMsg> reshapePathMsgs = reshapeLines.Select(l => l.Path).ToList();

			List<IPolyline> resultLines =
				ProtobufGeometryUtils.FromShapeMsgList<IPolyline>(reshapePathMsgs);

			int insideLineIndex = resultLines
			                      .Select((reshapePath, index) => (reshapePath, index))
			                      .First(rl => GeometryUtils.InteriorIntersects(
				                             rl.reshapePath, intersectingGeometry)).index;

			return insideLineIndex;
		}

		private static CalculateReshapeLinesRequest CreateCalculateReshapeLinesRequest(
			GdbFeature sourceFeature, GdbFeature targetFeature)
		{
			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(targetFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class);

			CalculateReshapeLinesRequest calculationRequest =
				new CalculateReshapeLinesRequest();

			calculationRequest.ClassDefinitions.Add(objectClassMsg);

			calculationRequest.TargetFeatures.Add(targetFeatureMsg);
			calculationRequest.SourceFeatures.Add(sourceFeatureMsg);

			// Use the data tolerance
			calculationRequest.Tolerance = -1;

			return calculationRequest;
		}
	}
}

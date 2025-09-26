using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Geom;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geometry.ChangeAlong;

namespace ProSuite.Microservices.Server.AO.Test.Geometry
{
	[TestFixture]
	public class ChangeAlongServiceUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
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

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

			GdbObjectReference resultFeatureObjRef = new GdbObjectReference(
				(int) updatedFeatureMsg.ClassHandle, (int) updatedFeatureMsg.ObjectId);

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

			updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

			resultFeatureObjRef = new GdbObjectReference(
				(int) updatedFeatureMsg.ClassHandle, (int) updatedFeatureMsg.ObjectId);

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

			ResultObjectMsg sourceResultMsg =
				applyResponse.ResultFeatures.First(
					f => f.Update.ObjectId == sourceFeature.OID);

			IGeometry updatedSourceGeometry =
				ProtobufGeometryUtils.FromShapeMsg(sourceResultMsg.Update.Shape);

			Assert.IsNotNull(updatedSourceGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) updatedSourceGeometry).Area);

			ResultObjectMsg targetResultMsg =
				applyResponse.ResultFeatures.First(
					f => f.Update.ObjectId == targetFeature.OID);

			IGeometry updatedTargetGeometry =
				ProtobufGeometryUtils.FromShapeMsg(targetResultMsg.Update.Shape);

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
				new[] { targetAlmostIntersectPoint }, 0.001);

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

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;
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

			updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

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

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

			GdbObjectReference resultFeatureObjRef = new GdbObjectReference(
				(int) updatedFeatureMsg.ClassHandle, (int) updatedFeatureMsg.ObjectId);

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

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

			GdbObjectReference resultFeatureObjRef = new GdbObjectReference(
				(int) updatedFeatureMsg.ClassHandle, (int) updatedFeatureMsg.ObjectId);

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

			GdbObjectMsg updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

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

			updatedFeatureMsg = applyResponse.ResultFeatures[0].Update;

			updatedGeometry = ProtobufGeometryUtils.FromShapeMsg(updatedFeatureMsg.Shape);

			Assert.IsNotNull(updatedGeometry);
			Assert.AreEqual(1000 * 1000 * 1 / 4, ((IArea) updatedGeometry).Area);

			// Check the new reshape line - there should be 1 reshapable, but filtered line
			AssertReshapeLineCount(applyResponse.NewReshapeLines, 1, 1, 1);

			// Technically the curve could be used to reshape (filtered out is not evaluated for usability)
			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) applyResponse.ReshapeLinesUsability);
		}

		[Test]
		public void CanCutAlong()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateCutLinesRequest calculationRequest =
				CreateCalculateCutLinesRequest(sourceFeature, targetFeature);

			CalculateCutLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateCutLines(calculationRequest, null);

			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) calculateResponse.ReshapeLinesUsability);

			AssertReshapeLineCount(calculateResponse.CutLines, 1, 1);

			IPolyline reshapeLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(calculateResponse.CutLines[0].Path);

			Assert.NotNull(reshapeLine);
			Assert.AreEqual(1000, (reshapeLine).Length);

			//
			// Cutting
			//
			var applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.Add(calculateResponse.CutLines[0]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;

			ApplyCutLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(2, applyResponse.ResultFeatures.Count);

			List<IGeometry> geometries = applyResponse.ResultFeatures.Select(GetShape).ToList();

			Assert.AreEqual(1000 * 1000, geometries.Sum(g => ((IArea) g).Area));

			ResultObjectMsg updateResultMsg =
				applyResponse.ResultFeatures.First(
					r => r.FeatureCase == ResultObjectMsg.FeatureOneofCase.Update);

			GdbObjectReference updateObjRef =
				new GdbObjectReference((int) updateResultMsg.Update.ClassHandle,
				                       (int) updateResultMsg.Update.ObjectId);

			Assert.AreEqual(new GdbObjectReference(sourceFeature), updateObjRef);

			IGeometry firstGeometry =
				ProtobufGeometryUtils.FromShapeMsg(updateResultMsg.Update.Shape);

			Assert.IsNotNull(firstGeometry);
			Assert.AreEqual(1000 * 1000 * 3 / 4, ((IArea) firstGeometry).Area);

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewCutLines, 0, 0);

			Assert.AreEqual(ReshapeAlongCurveUsability.NoReshapeCurves,
			                (ReshapeAlongCurveUsability) applyResponse.CutLinesUsability);
		}

		[Test]
		public void CanCutAlongInsertTargetVertices()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			CalculateCutLinesRequest calculationRequest =
				CreateCalculateCutLinesRequest(sourceFeature, targetFeature);

			CalculateCutLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateCutLines(calculationRequest, null);

			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) calculateResponse.ReshapeLinesUsability);

			AssertReshapeLineCount(calculateResponse.CutLines, 1, 1);

			IPolyline reshapeLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(calculateResponse.CutLines[0].Path);

			Assert.NotNull(reshapeLine);
			Assert.AreEqual(1000, (reshapeLine).Length);

			//
			// Cutting
			//
			var applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.Add(calculateResponse.CutLines[0]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = true;

			ApplyCutLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(3, applyResponse.ResultFeatures.Count);

			List<IGeometry> cutSourceGeometries =
				applyResponse.ResultFeatures
				             .Where(f => GetObjectId(f) != targetFeature.OID)
				             .Select(GetShape)
				             .ToList();

			Assert.AreEqual(1000 * 1000, cutSourceGeometries.Sum(g => ((IArea) g).Area));

			IGeometry updatedTargetGeometry =
				applyResponse.ResultFeatures
				             .Where(f => f.Update?.ObjectId == targetFeature.OID)
				             .Select(GetShape)
				             .Single();

			Assert.AreEqual(GeometryUtils.GetPointCount(targetFeature.Shape) + 2,
			                GeometryUtils.GetPointCount(updatedTargetGeometry));

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewCutLines, 0, 0);

			Assert.AreEqual(ReshapeAlongCurveUsability.NoReshapeCurves,
			                (ReshapeAlongCurveUsability) applyResponse.CutLinesUsability);
		}

		[Test]
		public void CanCutAlongUsingZSources()
		{
			GetOverlappingPolygons(out GdbFeature sourceFeature, out GdbFeature targetFeature);

			GeometryUtils.MakeZAware(sourceFeature.Shape);
			GeometryUtils.MakeZAware(targetFeature.Shape);

			var normal = new Vector(new[] { 0.5, 0.5, 2 });
			Pnt3D planePoint = new Pnt3D(2600000, 1200000, 600);

			Plane3D sourcePlane = new Plane3D(normal, planePoint);

			ChangeAlongZUtils.AssignZ((IPointCollection) sourceFeature.Shape, sourcePlane);

			GeometryUtils.ApplyConstantZ(targetFeature.Shape, 500);

			CalculateCutLinesRequest calculationRequest =
				CreateCalculateCutLinesRequest(sourceFeature, targetFeature);

			CalculateCutLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateCutLines(calculationRequest, null);

			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) calculateResponse.ReshapeLinesUsability);

			AssertReshapeLineCount(calculateResponse.CutLines, 1, 1);

			IPolyline reshapeLine =
				(IPolyline) ProtobufGeometryUtils.FromShapeMsg(calculateResponse.CutLines[0].Path);

			Assert.NotNull(reshapeLine);
			Assert.AreEqual(1000, (reshapeLine).Length);

			Linestring cutLinestring =
				GeometryConversionUtils.GetLinestring(GeometryUtils.GetPaths(reshapeLine).Single());

			Pnt3D midPoint = (Pnt3D) cutLinestring.GetPointAlong(0.5, true);

			List<Pnt3D> points = GeometryConversionUtils.GetPntList(reshapeLine);

			Assert.IsTrue(points.All(p => MathUtils.AreEqual(p.Z, 500.0)));

			//
			// Cutting - TargetZ
			//
			var applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.Add(calculateResponse.CutLines[0]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			SetZSource(applyRequest, ChangeAlongZSource.Target);

			ApplyCutLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(2, applyResponse.ResultFeatures.Count);

			List<IGeometry> cutGeometries =
				applyResponse.ResultFeatures.Select(GetShape).ToList();

			Assert.AreEqual(1000 * 1000, cutGeometries.Sum(g => ((IArea) g).Area));

			List<MultiPolycurve> multiPolycurves =
				cutGeometries
					.Select(g => GeometryConversionUtils.CreateMultiPolycurve((IPolycurve) g))
					.ToList();

			foreach (MultiPolycurve multiPolycurve in multiPolycurves)
			{
				Line3D segment = multiPolycurve.FindSegments(midPoint, 0.001).First().Value;
				Assert.AreEqual(500, segment.StartPoint.Z);
				Assert.AreEqual(500, segment.EndPoint.Z);
			}

			//
			// Cutting - Interpolate
			//
			applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.Add(calculateResponse.CutLines[0]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;

			SetZSource(applyRequest, ChangeAlongZSource.InterpolatedSource);

			applyResponse = ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(2, applyResponse.ResultFeatures.Count);

			cutGeometries = applyResponse.ResultFeatures.Select(GetShape).ToList();

			Assert.AreEqual(1000 * 1000, cutGeometries.Sum(g => ((IArea) g).Area));

			multiPolycurves =
				cutGeometries
					.Select(g => GeometryConversionUtils.CreateMultiPolycurve((IPolycurve) g))
					.ToList();

			foreach (MultiPolycurve multiPolycurve in multiPolycurves)
			{
				List<Line3D> segments
					= multiPolycurve.FindSegments(midPoint, 0.001)
					                .Select(kvp => kvp.Value).ToList();

				Assert.AreEqual(2, segments.Count);

				// Check if they are properly ordered and same length
				Assert.AreEqual(segments[0].EndPoint, segments[1].StartPoint);
				Assert.AreEqual(segments[0].Length2D, segments[1].Length2D);

				// Check if they are interpolated
				double average = (segments[0].StartPoint.Z + segments[1].EndPoint.Z) / 2;

				Assert.AreEqual(average, segments[0].EndPoint.Z);
			}

			//
			// Cutting - Plane
			//
			applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.Add(calculateResponse.CutLines[0]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;
			SetZSource(applyRequest, ChangeAlongZSource.SourcePlane);

			applyResponse = ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(2, applyResponse.ResultFeatures.Count);

			cutGeometries = applyResponse.ResultFeatures.Select(GetShape).ToList();

			Assert.AreEqual(1000 * 1000, cutGeometries.Sum(g => ((IArea) g).Area));

			multiPolycurves =
				cutGeometries
					.Select(g => GeometryConversionUtils.CreateMultiPolycurve((IPolycurve) g))
					.ToList();

			foreach (MultiPolycurve multiPolycurve in multiPolycurves)
			{
				bool? coplanar = ChangeZUtils.AreCoplanar(
					multiPolycurve.GetPoints().ToList(), sourcePlane,
					0.01, out double _, out string _);

				Assert.IsTrue(coplanar);
			}
		}

		[Test]
		public void CanCutMultipleSourcesAlong()
		{
			GetOverlappingPolygons(out GdbFeature source1Feature, out GdbFeature source2Feature);

			IFeatureClass fClass = (IFeatureClass) source1Feature.Class;

			IFeature cutFeature = fClass.CreateFeature();
			cutFeature.Shape =
				GeometryFactory.CreatePolygon(
					GeometryFactory.CreatePoint(2600000, 1200750),
					GeometryFactory.CreatePoint(2602000, 1202000));
			cutFeature.Store();

			var source1FeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) source1Feature);
			var source2FeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) source2Feature);

			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(cutFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(source1Feature.Class);

			var calculationRequest = new CalculateCutLinesRequest();

			calculationRequest.ClassDefinitions.Add(objectClassMsg);
			calculationRequest.SourceFeatures.Add(source1FeatureMsg);
			calculationRequest.SourceFeatures.Add(source2FeatureMsg);

			calculationRequest.TargetFeatures.Add(targetFeatureMsg);

			calculationRequest.Tolerance = -1;

			CalculateCutLinesResponse calculateResponse =
				ChangeAlongServiceUtils.CalculateCutLines(calculationRequest, null);

			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) calculateResponse.ReshapeLinesUsability);

			AssertReshapeLineCount(calculateResponse.CutLines, 2, 2);

			Assert.AreEqual(source1Feature.OID, calculateResponse.CutLines[0].Source.ObjectId);
			Assert.AreEqual(source2Feature.OID, calculateResponse.CutLines[1].Source.ObjectId);

			foreach (ReshapeLineMsg cutLineMsg in calculateResponse.CutLines)
			{
				IPolyline reshapeLine =
					(IPolyline) ProtobufGeometryUtils.FromShapeMsg(cutLineMsg.Path);

				Assert.NotNull(reshapeLine);
				Assert.AreEqual(1000, reshapeLine.Length);
			}

			//
			// Cutting using just one of the lines:
			//
			var applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.Add(calculateResponse.CutLines[0]);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;

			ApplyCutLinesResponse applyResponse =
				ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(2, applyResponse.ResultFeatures.Count);

			GdbObjectMsg updatedFeatureMsg =
				applyResponse.ResultFeatures.First(
					r => r.FeatureCase == ResultObjectMsg.FeatureOneofCase.Update).Update;

			GdbObjectReference updatedObjRef =
				new GdbObjectReference((int) updatedFeatureMsg.ClassHandle,
				                       (int) updatedFeatureMsg.ObjectId);

			Assert.AreEqual(new GdbObjectReference(source1Feature), updatedObjRef);

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewCutLines, 1, 1);

			Assert.AreEqual(ReshapeAlongCurveUsability.CanReshape,
			                (ReshapeAlongCurveUsability) applyResponse.CutLinesUsability);

			//
			// Cutting using both lines:
			//
			applyRequest = new ApplyCutLinesRequest();

			applyRequest.CutLines.AddRange(calculateResponse.CutLines);
			applyRequest.CalculationRequest = calculationRequest;
			applyRequest.InsertVerticesInTarget = false;

			applyResponse = ChangeAlongServiceUtils.ApplyCutLines(applyRequest, null);

			Assert.AreEqual(4, applyResponse.ResultFeatures.Count);

			// Check the new reshape line:
			AssertReshapeLineCount(applyResponse.NewCutLines, 0, 0);

			Assert.AreEqual(ReshapeAlongCurveUsability.NoReshapeCurves,
			                (ReshapeAlongCurveUsability) applyResponse.CutLinesUsability);
		}

		///  <summary>
		///  Returns two overlapping square polygons:
		/// 
		///         _____________
		///        |             |
		///        |    target   |
		///  ______|______       |
		/// |      |      |      |
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

			sourceFeature = GdbFeature.Create(42, fClass);
			sourceFeature.Shape = polygon1;

			IPolygon polygon2 = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2600500, 1200500, sr),
				GeometryFactory.CreatePoint(2601500, 1201500, sr));

			polygon2.SpatialReference = sr;

			targetFeature = GdbFeature.Create(43, fClass);
			targetFeature.Shape = polygon2;
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
			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg((IReadOnlyRow) targetFeature);

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

		private static CalculateCutLinesRequest CreateCalculateCutLinesRequest(
			IObject sourceFeature, IObject targetFeature)
		{
			var sourceFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(sourceFeature);
			var targetFeatureMsg = ProtobufGdbUtils.ToGdbObjectMsg(targetFeature);

			var objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(sourceFeature.Class);

			var calculationRequest = new CalculateCutLinesRequest();

			calculationRequest.ClassDefinitions.Add(objectClassMsg);

			calculationRequest.TargetFeatures.Add(targetFeatureMsg);
			calculationRequest.SourceFeatures.Add(sourceFeatureMsg);

			// Use the data tolerance
			calculationRequest.Tolerance = -1;

			return calculationRequest;
		}

		private static IGeometry GetShape(ResultObjectMsg resultObjectMsg)
		{
			ShapeMsg shapeMsg;

			switch (resultObjectMsg.FeatureCase)
			{
				case ResultObjectMsg.FeatureOneofCase.Update:
					shapeMsg = resultObjectMsg.Update.Shape;
					break;
				case ResultObjectMsg.FeatureOneofCase.Insert:
					shapeMsg = resultObjectMsg.Insert.InsertedObject.Shape;
					break;
				default:
					return null;
			}

			return shapeMsg == null ? null : ProtobufGeometryUtils.FromShapeMsg(shapeMsg);
		}

		private static int GetObjectId(ResultObjectMsg resultObjectMsg)
		{
			switch (resultObjectMsg.FeatureCase)
			{
				case ResultObjectMsg.FeatureOneofCase.Update:
					return (int) resultObjectMsg.Update.ObjectId;
				case ResultObjectMsg.FeatureOneofCase.Insert:
					return (int) resultObjectMsg.Insert.InsertedObject.ObjectId;
				case ResultObjectMsg.FeatureOneofCase.Delete:
					return (int) resultObjectMsg.Delete.ObjectId;
				default:
					return -1;
			}
		}

		private static void SetZSource(ApplyCutLinesRequest applyRequest,
		                               ChangeAlongZSource zSource)
		{
			applyRequest.CalculationRequest.ZSources.Clear();

			int intValue = (int) zSource;

			var zSourceMsg = new DatasetZSource { ZSource = intValue };

			applyRequest.CalculationRequest.ZSources.Add(zSourceMsg);
		}
	}
}

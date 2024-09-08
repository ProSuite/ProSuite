using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geometry.Cracker
{
	public static class CrackServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static CalculateCrackPointsResponse CalculateCrackPoints(
			[NotNull] CalculateCrackPointsRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			CrackOptionsMsg optionsMsg =
				Assert.NotNull(request.CrackOptions, "Crack options is null");

			GeometryProcessingUtils.GetFeatures(
				request.SourceFeatures, request.TargetFeatures, request.ClassDefinitions,
				out IList<IFeature> sourceFeatures, out IList<IFeature> targetFeatures);

			_msg.DebugStopTiming(watch, "Unpacked feature lists from request params");

			IGeometry calculationPerimeter = ProtobufGeometryUtils.FromShapeMsg(request.Perimeter);

			double? snapTolerance = optionsMsg.SnapToTargetVertices
				                        ? optionsMsg.SnapTolerance
				                        : null;

			double? minimumSegmentLength = optionsMsg.RespectMinimumSegmentLength
				                               ? optionsMsg.MinimumSegmentLength
				                               : null;

			bool useSourceZs = optionsMsg.UseSourceZs;

			bool onlyWithinSameClass = optionsMsg.CrackOnlyWithinSameClass;

			IList<FeatureVertexInfo> vertexInfos =
				CrackUtils.CreateFeatureVertexInfos(sourceFeatures, calculationPerimeter?.Envelope,
				                                    snapTolerance, minimumSegmentLength);

			CrackPointCalculator crackPointCalculator = CreateCrackPointCalculator(
				snapTolerance, minimumSegmentLength, useSourceZs, calculationPerimeter);

			if (targetFeatures.Count == 0)
			{
				_msg.Debug(
					"No target features provided, calculating crack points between source features");

				CrackUtils.AddFeatureIntersectionCrackPoints(
					vertexInfos, crackPointCalculator, trackCancel);
			}
			else
			{
				foreach (FeatureVertexInfo featureVertexInfo in vertexInfos)
				{
					if (trackCancel != null && ! trackCancel.Continue())
					{
						return new CalculateCrackPointsResponse();
					}

					CrackUtils.AddTargetIntersectionCrackPoints(
						featureVertexInfo, targetFeatures, onlyWithinSameClass,
						crackPointCalculator, trackCancel);
				}
			}

			watch = Stopwatch.StartNew();

			CalculateCrackPointsResponse result = PackCrackPoints(vertexInfos);

			_msg.DebugStopTiming(watch, "Packed overlaps into response");

			return result;
		}

		public static ApplyCrackPointsResponse ApplyCrackPoints(ApplyCrackPointsRequest request,
		                                                        ITrackCancel trackCancel)
		{
			// Unpack request
			CrackOptionsMsg options = request.CrackOptions;

			double? snapTolerance =
				Assert.NotNull(options, "CrackOptions are null").SnapToTargetVertices
					? options.SnapTolerance
					: null;

			IList<IFeature> sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(
				Assert.NotNull(request.SourceFeatures, "SourceFeatures are null"),
				Assert.NotNull(request.ClassDefinitions, "ClassDefinitions are null"));

			Dictionary<GdbObjectReference, IFeature> featureByObjRef =
				sourceFeatures.ToDictionary(s => new GdbObjectReference(s), s => s);

			var updateGeometryByFeature = new Dictionary<IFeature, IGeometry>();

			foreach (CrackPointsMsg crackPointsMsg in request.CrackPoints)
			{
				if (crackPointsMsg.CrackPoints.Count == 0)
				{
					continue;
				}

				// TODO: Class handle in GdbObjectReference: long!
				GdbObjectReference gdbObjRef = new GdbObjectReference(
					Convert.ToInt32(crackPointsMsg.OriginalFeatureRef.ClassHandle),
					crackPointsMsg.OriginalFeatureRef.ObjectId);

				//IFeatureClass fClass =
				//	sourceFeatures
				//		.Select(f => f.Class)
				//		.First(c => c.ObjectClassID == gdbObjRef.ClassId) as IFeatureClass;

				IPointCollection pointsToAdd =
					CreateCrackPointCollection(crackPointsMsg.CrackPoints);

				IFeature feature = featureByObjRef[gdbObjRef];

				IGeometry updateGeometry = feature.ShapeCopy;
				CrackUtils.AddRemovePoints(updateGeometry, pointsToAdd, null, snapTolerance);

				Simplify(updateGeometry);

				updateGeometryByFeature[feature] = updateGeometry;
			}

			// Pack response
			var response = new ApplyCrackPointsResponse();

			foreach (var kvp in updateGeometryByFeature)
			{
				IFeature feature = kvp.Key;
				IGeometry newGeometry = kvp.Value;

				var resultObject = new ResultObjectMsg();

				resultObject.Update = ProtobufGdbUtils.ToGdbObjectMsg(
					feature, newGeometry, feature.Class.ObjectClassID);

				response.ResultFeatures.Add(resultObject);
			}

			return response;
		}

		private static void Simplify(IGeometry newGeometry)
		{
			// NOTE: In a Gdb the features have the actual tolerance,
			//		 however, simplify also joins points that are slightly further apart 
			//		 than the tolerance (e.g. observed for up to 2.9 times the tolerance between
			//		 vertices at 10.2)
			// TODO: Possible solutions:
			//		 - Use a minimum snap tolerance of 3 * the tolerance (even if no snapping specified)
			//		 - Artificially reduce the tolerance (if not done by the system anyway) and deal with
			//		   the possibly resulting short segments (w.r.t the gdb tolerance) before storing
			//		 - Do not simplify and manually deal with potential issues such as short segments
			GeometryUtils.Simplify(newGeometry);

			// typically the line has a Z value but CalculateNonSimpleZs fails to extrapolate
			double defaultZ = newGeometry.Envelope.ZMax;
			if (double.IsNaN(defaultZ))
			{
				defaultZ = 0d;
			}

			GeometryUtils.SimplifyZ(newGeometry, defaultZ);
		}

		private static IPointCollection CreateCrackPointCollection(
			IReadOnlyCollection<CrackPointMsg> crackPoints)
		{
			IMultipoint multipoint = GeometryFactory.CreateMultipoint(CreatePoints(crackPoints));

			return (IPointCollection) multipoint;
		}

		private static IEnumerable<IPoint> CreatePoints(IEnumerable<CrackPointMsg> crackPoints)
		{
			List<IPoint> points = new List<IPoint>();

			foreach (CrackPointMsg crackPointMsg in crackPoints)
			{
				IPoint point = (IPoint) ProtobufGeometryUtils.FromShapeMsg(crackPointMsg.Point);

				points.Add(Assert.NotNull(point));
			}

			return points;
		}

		private static CalculateCrackPointsResponse PackCrackPoints(
			IEnumerable<FeatureVertexInfo> vertexInfos)
		{
			var result = new CalculateCrackPointsResponse();

			foreach (FeatureVertexInfo resultFeatureInfo in vertexInfos)
			{
				IList<CrackPoint> crackPoints = resultFeatureInfo.CrackPoints;

				if (crackPoints == null || crackPoints.Count == 0)
				{
					continue;
				}

				CrackPointsMsg crackPointsPerFeatureMsg = new CrackPointsMsg();

				result.CrackPoints.Add(crackPointsPerFeatureMsg);

				crackPointsPerFeatureMsg.OriginalFeatureRef =
					ProtobufGdbUtils.ToGdbObjRefMsg(resultFeatureInfo.Feature);

				foreach (CrackPoint crackPoint in crackPoints)
				{
					CrackPointMsg crackPointMsg = new CrackPointMsg();

					crackPointMsg.Point = ProtobufGeometryUtils.ToShapeMsg(crackPoint.Point);
					crackPointMsg.ViolatesMinimumSegmentLength =
						crackPoint.ViolatesMinimumSegmentLength;
					crackPointMsg.TargetVertexDifferentWithinTolerance =
						crackPoint.TargetVertexDifferentWithinTolerance;
					crackPointMsg.TargetVertexOnlyDifferentInZ =
						crackPoint.TargetVertexOnlyDifferentInZ;

					crackPointsPerFeatureMsg.CrackPoints.Add(crackPointMsg);
				}
			}

			return result;
		}

		private static CrackPointCalculator CreateCrackPointCalculator(
			double? snapTolerance,
			double? minimumSegmentLength,
			bool useSourceZs,
			[CanBeNull] IGeometry perimeter)
		{
			IEnvelope inExtent = perimeter?.Envelope;

			var cracker = new CrackPointCalculator(
				snapTolerance, minimumSegmentLength, addCrackPointsOnExistingVertices: false,
				useSourceZs, IntersectionPointOptions.IncludeLinearIntersectionAllPoints, inExtent);

			// Special handling of multipatch targets:
			cracker.TargetTransformation = CrackUtils.ExtractBoundariesForMultipatches;

			return cracker;
		}
	}
}

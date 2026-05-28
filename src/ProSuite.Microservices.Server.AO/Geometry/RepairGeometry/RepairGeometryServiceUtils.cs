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
using ProSuite.Commons.AO.Geometry.Generalize;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using CrackPoint = ProSuite.Commons.AO.Geometry.Cracking.CrackPoint;

namespace ProSuite.Microservices.Server.AO.Geometry.RepairGeometry
{
	public static class RepairGeometryServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static CalculateRepairInfoResponse CalculateRepairInfo(
			[NotNull] CalculateRepairInfoRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			RepairOptionsMsg optionsMsg =
				Assert.NotNull(request.RepairOptions, "Repair options are null");

			IList<IFeature> sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(
				Assert.NotNull(request.SourceFeatures, "SourceFeatures are null"),
				Assert.NotNull(request.ClassDefinitions, "ClassDefinitions are null"));

			double minimumSegmentLength = optionsMsg.MinimumSegmentLength;
			bool allowLoops = optionsMsg.AllowLoops;
			bool allowLinearSelfIntersections = optionsMsg.AllowLinearSelfIntersections;
			bool addCrackPointsBetweenParts = optionsMsg.AddCrackPointsBetweenParts;
			double crackPointTolerance = optionsMsg.CrackPointTolerance;
			bool use2D = optionsMsg.Use2D;

			bool allowPathSplitAtIntersections = ! (allowLoops || allowLinearSelfIntersections);

			double? minSegLen = minimumSegmentLength > 0 ? (double?) minimumSegmentLength : null;
			IList<FeatureVertexInfo> featureVertexInfos =
				CrackUtils.CreateFeatureVertexInfos(sourceFeatures, null, null, minSegLen);

			CrackPointCalculator crackPointCalculator = null;
			if (addCrackPointsBetweenParts)
			{
				crackPointCalculator = CreateCrackPointCalculator(crackPointTolerance, use2D);
			}

			var notificationMessages = new List<string>();

			// Track only the true min-length segments per feature (from CalculateShortSegments).
			// CalculateSimplifyDeltaPoints also appends "bracket segments" to ShortSegments, but
			// those are internal artefacts handled by Simplify in the apply step and must NOT be
			// shown to the user as InvalidSegments.
			var minLengthSegments =
				new Dictionary<FeatureVertexInfo, IList<esriSegmentInfo>>();

			foreach (FeatureVertexInfo vertexInfo in featureVertexInfos)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					break;
				}

				var featureNotifications = new NotificationCollection();

				if (addCrackPointsBetweenParts && crackPointCalculator != null)
				{
					ISpatialReference dataSpatialRef =
						DatasetUtils.GetSpatialReference((IFeatureClass) vertexInfo.Feature.Class);
					CrackUtils.AddGeometryPartIntersectionCrackPoints(
						vertexInfo, crackPointCalculator, dataSpatialRef, trackCancel);
				}

				if (minimumSegmentLength > 0)
				{
					vertexInfo.MinimumSegmentLength = minimumSegmentLength;
					GeneralizeUtils.CalculateShortSegments(vertexInfo, use2D, null);
				}

				if (! allowLinearSelfIntersections)
				{
					AddLinearSelfIntersectionSegments(vertexInfo, use2D);
				}

				// Snapshot the min-length segments BEFORE CalculateSimplifyDeltaPoints can add
				// bracket segments via AddSegmentsToRemoveBetweenPointsToRemove.
				minLengthSegments[vertexInfo] = vertexInfo.ShortSegments != null
					                                ? new List<esriSegmentInfo>(
						                                vertexInfo.ShortSegments)
					                                : null;

				CalculateSimplifyDeltaPoints(
					vertexInfo, allowPathSplitAtIntersections, featureNotifications);

				if (featureNotifications.Count > 0)
				{
					string featureLabel =
						GeometryProcessingUtils.GetGdbObjectLabel(vertexInfo.Feature);
					notificationMessages.Add(
						$"{featureLabel}: {NotificationUtils.Concatenate(featureNotifications, " ")}");
				}
			}

			CalculateRepairInfoResponse response =
				PackRepairInfoResponse(featureVertexInfos, minLengthSegments);
			response.Notifications.AddRange(notificationMessages);

			_msg.DebugStopTiming(watch,
			                     "Calculated repair info for {0} features ({1} with issues).",
			                     sourceFeatures.Count, response.RepairInfos.Count);

			return response;
		}

		public static ApplyRepairGeometryResponse ApplyRepairGeometry(
			[NotNull] ApplyRepairGeometryRequest request,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var watch = Stopwatch.StartNew();

			IList<IFeature> sourceFeatures = ProtobufConversionUtils.FromGdbObjectMsgList(
				Assert.NotNull(request.SourceFeatures, "SourceFeatures are null"),
				Assert.NotNull(request.ClassDefinitions, "ClassDefinitions are null"));

			// Unpack options — these were set from the original CalculateRepairInfo options.
			RepairOptionsMsg optionsMsg = request.RepairOptions;
			double minimumSegmentLength = optionsMsg?.MinimumSegmentLength ?? -1;
			bool allowLoops = optionsMsg?.AllowLoops ?? false;
			bool allowLinearSelfIntersections = optionsMsg?.AllowLinearSelfIntersections ?? false;
			double crackPointTolerance = optionsMsg?.CrackPointTolerance ?? 0;
			bool use2D = optionsMsg?.Use2D ?? false;

			bool allowPathSplitAtIntersections = ! (allowLoops || allowLinearSelfIntersections);

			Dictionary<GdbObjectReference, IFeature> featureByObjRef =
				sourceFeatures.ToDictionary(s => new GdbObjectReference(s), s => s);

			var updateGeometryByFeature = new Dictionary<IFeature, IGeometry>();
			var nonStorableMessages = new List<string>();

			foreach (RepairInfoMsg repairInfoMsg in request.RepairInfos)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					break;
				}

				GdbObjectReference gdbObjRef = new GdbObjectReference(
					Convert.ToInt32(repairInfoMsg.OriginalFeatureRef.ClassHandle),
					repairInfoMsg.OriginalFeatureRef.ObjectId);

				if (! featureByObjRef.TryGetValue(gdbObjRef, out IFeature feature))
				{
					_msg.WarnFormat("Feature not found for repair info: {0}/{1}",
					                gdbObjRef.ClassId, gdbObjRef.ObjectId);
					continue;
				}

				try
				{
					IGeometry updateGeometry = ApplyRepairToFeature(
						feature, minimumSegmentLength, crackPointTolerance, use2D,
						allowPathSplitAtIntersections, allowLinearSelfIntersections);

					if (updateGeometry != null && ! updateGeometry.IsEmpty)
					{
						updateGeometryByFeature[feature] = updateGeometry;
					}
					else if (updateGeometry != null && updateGeometry.IsEmpty)
					{
						string message =
							$"Feature {GeometryProcessingUtils.GetGdbObjectLabel(feature)} would become empty after repair. " +
							"The feature was not changed.";
						_msg.Warn(message);
						nonStorableMessages.Add(message);
					}
				}
				catch (Exception e)
				{
					string message =
						$"Error repairing {GeometryProcessingUtils.GetGdbObjectLabel(feature)}: {e.Message}. " +
						"The feature was not changed.";
					_msg.Warn(message, e);
					nonStorableMessages.Add(message);

					throw;
				}
			}

			// When cracking is requested, also process source features not covered by RepairInfos.
			// CalculateRepairInfo only flags features with short segments or inter-part issues,
			// but a feature may have proximity-based self-intersections that need cracking even
			// if it was otherwise topologically clean.
			if (crackPointTolerance > 0)
			{
				var processedFeatures = new HashSet<IFeature>(updateGeometryByFeature.Keys);

				foreach (IFeature feature in sourceFeatures)
				{
					if (trackCancel != null && ! trackCancel.Continue())
					{
						break;
					}

					if (processedFeatures.Contains(feature))
					{
						continue;
					}

					try
					{
						IGeometry updateGeometry = ApplyRepairToFeature(
							feature, minimumSegmentLength,
							crackPointTolerance, use2D, allowPathSplitAtIntersections,
							allowLinearSelfIntersections);

						if (updateGeometry != null && ! updateGeometry.IsEmpty)
						{
							updateGeometryByFeature[feature] = updateGeometry;
						}
						else if (updateGeometry != null && updateGeometry.IsEmpty)
						{
							string message =
								$"Feature {GeometryProcessingUtils.GetGdbObjectLabel(feature)} would become empty after repair. " +
								"The feature was not changed.";
							_msg.Warn(message);
							nonStorableMessages.Add(message);
						}
					}
					catch (Exception e)
					{
						string message =
							$"Error cracking {GeometryProcessingUtils.GetGdbObjectLabel(feature)}: {e.Message}. " +
							"The feature was not changed.";
						_msg.Warn(message, e);
						nonStorableMessages.Add(message);

						throw;
					}
				}
			}

			ApplyRepairGeometryResponse response =
				PackApplyRepairGeometryResponse(updateGeometryByFeature, nonStorableMessages);

			_msg.DebugStopTiming(watch, "Applied repair geometry to {0} features.",
			                     updateGeometryByFeature.Count);

			return response;
		}

		private static CrackPointCalculator CreateCrackPointCalculator(double crackPointTolerance,
			bool use2D)
		{
			var crackOptions = new RepairGeometryCrackingOptions(crackPointTolerance);

			var crackPointCalculator =
				new CrackPointCalculator(
					crackOptions,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
					null)
				{
					In3D = ! use2D,
					TargetTransformation = ExtractVertices
				};

			return crackPointCalculator;
		}

		[CanBeNull]
		private static IGeometry ApplyRepairToFeature(
			[NotNull] IFeature feature,
			double minimumSegmentLength,
			double crackPointTolerance,
			bool use2D,
			bool allowPathSplitAtIntersections,
			bool allowLinearSelfIntersections)
		{
			IGeometry updateGeometry = feature.ShapeCopy;

			// Step 0: Remove linear self-intersection segments (duplicate / back-tracking segments).
			// The strategy differs between geometry types:
			// - Polygons: TryDeleteLinearSelfIntersectionsXY per ring, which is designed for
			//   closed rings and correctly removes BOTH sides of a spike (the right behaviour
			//   since one side of the spike is redundant in a closed ring).
			// - Polylines: PlanarizeLines, which preserves the first traversal of each overlapping
			//   segment rather than removing both (TryDeleteLinearSelfIntersectionsXY would create
			//   a gap / empty geometry for dead-end back-tracking polylines).
			// This also handles allowPathSplitAtIntersections=false (loops allowed) where
			// simplify (Step 3) would not remove linear overlaps on its own.
			if (! allowLinearSelfIntersections && updateGeometry is IPolycurve polycurve)
			{
				double tolerance = GeometryUtils.GetXyTolerance(feature);

				updateGeometry =
					RemoveSelfIntersectingSegments(polycurve, tolerance, minimumSegmentLength);
			}

			// Step 1: Remove short segments.
			// Re-scan the current (post-Step-0) geometry rather than consuming the stale segment
			// indices from the proto message: after Step 0 restructures the geometry those indices
			// no longer point to the right segments. Linear self-intersections are already gone;
			// only truly short segments (length < minimumSegmentLength) remain to be removed here.
			if (minimumSegmentLength > 0)
			{
				IList<esriSegmentInfo> shortSegments = GeneralizeUtils.GetShortSegments(
					(IPolycurve) updateGeometry, null, minimumSegmentLength, use2D);

				if (shortSegments.Count > 0)
				{
					var featureVertexInfo = new FeatureVertexInfo(feature, null)
					                        {
						                        MinimumSegmentLength = minimumSegmentLength,
						                        ShortSegments = shortSegments
					                        };

					int removedShortSegments = GeneralizeUtils.DeleteShortSegments(
						(IPolycurve) updateGeometry, featureVertexInfo, use2D, null);

					_msg.VerboseDebug(() => $"Removed {removedShortSegments} short segments");
				}
			}

			if (updateGeometry.IsEmpty)
			{
				return updateGeometry;
			}

			// Step 2: Crack self-intersections using the crack point tolerance.
			if (crackPointTolerance > 0)
			{
				updateGeometry =
					CrackAtSelfIntersections(crackPointTolerance, minimumSegmentLength,
					                         updateGeometry);
			}

			// Step 3: Simplify — use the same allowPathSplitAtIntersections value as during
			// CalculateRepairInfo so the apply step is consistent with what was analyzed.
			if (updateGeometry.GeometryType == esriGeometryType.esriGeometryPolygon ||
			    updateGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				GeometryUtils.Simplify(updateGeometry, false, allowPathSplitAtIntersections);
			}

			return updateGeometry;
		}

		private static IGeometry CrackAtSelfIntersections(double crackPointTolerance,
		                                                  double minimumSegmentLength,
		                                                  IGeometry updateGeometry)
		{
			var multiPolycurve =
				GeometryConversionUtils.CreateMultiPolycurve((IPolycurve) updateGeometry);

			double? minSegLen = minimumSegmentLength > 0
				                    ? minimumSegmentLength
				                    : (double?) null;

			bool anyRingCracked = false;
			var crackedLinestrings = new List<Linestring>();

			foreach (Linestring linestring in multiPolycurve.GetLinestrings())
			{
				if (GeomTopoOpUtils.TryCrackAtSelfIntersections(
					    linestring, crackPointTolerance, minSegLen,
					    out Linestring cracked))
				{
					crackedLinestrings.Add(cracked);
					anyRingCracked = true;
				}
				else
				{
					crackedLinestrings.Add(linestring);
				}
			}

			if (anyRingCracked)
			{
				updateGeometry = ReCreateGeometry(crackedLinestrings, updateGeometry);
			}

			return updateGeometry;
		}

		private static IPolycurve RemoveSelfIntersectingSegments([NotNull] IPolycurve polycurve,
		                                                         double tolerance,
		                                                         double minimumSegmentLength)
		{
			MultiPolycurve multiPolycurve =
				GeometryConversionUtils.CreateMultiPolycurve(polycurve);

			double? minSegLen =
				minimumSegmentLength > 0 ? (double?) minimumSegmentLength : null;

			IPolycurve resultGeometry = polycurve;

			if (polycurve.GeometryType == esriGeometryType.esriGeometryPolygon)
			{
				bool anyChanged = false;
				var resultRings = new List<Linestring>();

				foreach (Linestring ring in multiPolycurve.GetLinestrings())
				{
					var results = new List<Linestring>();
					if (GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
						    ring, tolerance, results, minSegLen))
					{
						resultRings.AddRange(results);
						anyChanged = true;
					}
					else
					{
						resultRings.Add(ring);
					}
				}

				if (anyChanged)
				{
					resultGeometry = (IPolycurve) ReCreateGeometry(resultRings, polycurve);
				}
			}
			else
			{
				MultiLinestring planarized =
					GeomTopoOpUtils.PlanarizeLines(multiPolycurve, tolerance);

				if (planarized.SegmentCount < multiPolycurve.SegmentCount)
				{
					List<Linestring> planarizedLinestrings = planarized.GetLinestrings().ToList();

					resultGeometry =
						(IPolycurve) ReCreateGeometry(planarizedLinestrings, polycurve);
				}
			}

			return resultGeometry;
		}

		private static IGeometry ReCreateGeometry([NotNull] List<Linestring> fromCrackedLinestrings,
		                                          [NotNull] IGeometry updateGeometry)
		{
			if (updateGeometry.GeometryType == esriGeometryType.esriGeometryPolygon)
			{
				return GeometryConversionUtils.CreatePolygon(
					updateGeometry, fromCrackedLinestrings);
			}

			return GeometryConversionUtils.CreatePolyline(fromCrackedLinestrings,
			                                              updateGeometry.SpatialReference);
		}

		private static IEnumerable<Commons.Geom.CrackPoint> GetCrackPoints3d(
			FeatureVertexInfo vertexInfo)
		{
			if (vertexInfo?.CrackPoints == null)
			{
				yield break;
			}

			foreach (CrackPoint crackPoint in vertexInfo.CrackPoints)
			{
				IntersectionPoint3D intersectionPoint3D =
					crackPoint.Intersections?.FirstOrDefault();

				if (intersectionPoint3D == null)
				{
					continue;
				}

				Pnt3D targetPnt = crackPoint.Point3d;

				yield return new Commons.Geom.CrackPoint(intersectionPoint3D, targetPnt);
			}
		}

		private static void CalculateSimplifyDeltaPoints(
			[NotNull] FeatureVertexInfo vertexInfo,
			bool allowPathSplitAtIntersections,
			[NotNull] NotificationCollection notifications)
		{
			IGeometry geometry = vertexInfo.Feature.Shape;

			GeometryUtils.EnsureSpatialReference(geometry, vertexInfo.Feature);

			IGeometry simplified = GeometryFactory.Clone(geometry);

			const bool allowReorder = false;
			GeometryUtils.Simplify(simplified, allowReorder, allowPathSplitAtIntersections);

			if (GeometryUtils.AreEqual(geometry, simplified))
			{
				return;
			}

			if (simplified.IsEmpty)
			{
				vertexInfo.ShortSegments =
					GeometryUtils.GetShortSegments((IPolycurve) geometry, double.MaxValue);
				NotificationUtils.Add(notifications,
				                      "The geometry has not enough points. " +
				                      "Consider deleting this feature or rebuild it.");
				return;
			}

			bool allowNonPlanarLines = ! allowPathSplitAtIntersections;
			string description;
			GeometryNonSimpleReason? nonSimpleReason;
			GeometryUtils.IsGeometrySimple(geometry, geometry.SpatialReference,
			                               allowNonPlanarLines,
			                               out description, out nonSimpleReason);

			NotificationUtils.Add(notifications, description);

			if (nonSimpleReason == GeometryNonSimpleReason.IncorrectRingOrientation ||
			    nonSimpleReason == GeometryNonSimpleReason.EmptyPart)
			{
				vertexInfo.ShortSegments =
					GeometryUtils.GetShortSegments((IPolycurve) geometry, double.MaxValue);
				return;
			}

			if (nonSimpleReason == GeometryNonSimpleReason.UnclosedRing)
			{
				var missingEndPoints = new List<CrackPoint>();
				foreach (IRing ring in GeometryUtils.GetRings((IPolygon) geometry))
				{
					if (! ring.IsClosed)
					{
						missingEndPoints.Add(new CrackPoint(ring.FromPoint));
					}
				}

				vertexInfo.AddCrackPoints(missingEndPoints);
				return;
			}

			double xyResolution = GeometryUtils.GetXyResolution(vertexInfo.Feature) / 2;
			double zResolution = GeometryUtils.GetZResolution(geometry) / 2;

			var geometryComparison =
				new GeometryComparison(geometry, simplified, xyResolution, zResolution);

			const bool symmetric = false;
			const bool reportDuplicateVertices = true;

			IList<WKSPointZ> removedPoints =
				geometryComparison.GetDifferentVertices(symmetric, reportDuplicateVertices);

			IList<WKSPointZ> pointsToRemove =
				removedPoints.Where(p => ! IsOnShortSegment(p, vertexInfo)).ToList();

			AddSegmentsToRemoveBetweenPointsToRemove(vertexInfo, pointsToRemove);

			vertexInfo.PointsToDelete =
				(IPointCollection) GeometryFactory.CreateMultipoint(
					pointsToRemove.Where(p => ! IsOnShortSegment(p, vertexInfo)).ToList(),
					DatasetUtils.GetGeometryDef(vertexInfo.Feature));

			var geometryComparison2 =
				new GeometryComparison(simplified, geometry, xyResolution, zResolution);

			IList<WKSPointZ> pointsToAdd =
				geometryComparison2.GetDifferentVertices(symmetric, reportDuplicateVertices);

			if (nonSimpleReason == GeometryNonSimpleReason.Unknown && pointsToAdd.Count > 0)
			{
				NotificationUtils.Add(notifications,
				                      "The polygon's or one of its rings' start point is moved by simplification.");
			}

			IList<CrackPoint> crackPoints =
				GetFilteredCrackPoints(vertexInfo, geometry, simplified, pointsToAdd);
			vertexInfo.AddCrackPoints(crackPoints);
		}

		private static CalculateRepairInfoResponse PackRepairInfoResponse(
			[NotNull] IList<FeatureVertexInfo> featureVertexInfos,
			[NotNull] Dictionary<FeatureVertexInfo, IList<esriSegmentInfo>> minLengthSegments)
		{
			var response = new CalculateRepairInfoResponse();

			foreach (FeatureVertexInfo featureInfo in featureVertexInfos)
			{
				// Only report the min-length segments (from CalculateShortSegments) as
				// InvalidSegments. Bracket segments added by CalculateSimplifyDeltaPoints are
				// NOT included here — they are handled by Simplify in the apply step.
				IList<esriSegmentInfo> invalidSegments;
				if (! minLengthSegments.TryGetValue(featureInfo, out invalidSegments))
				{
					invalidSegments = null;
				}

				bool hasData = (featureInfo.PointsToDelete != null &&
				                ((IGeometry) featureInfo.PointsToDelete).IsEmpty == false) ||
				               featureInfo.CrackPointCollection != null ||
				               (invalidSegments != null && invalidSegments.Count > 0);

				if (! hasData)
				{
					continue;
				}

				var repairInfoMsg = new RepairInfoMsg();

				repairInfoMsg.OriginalFeatureRef =
					ProtobufGdbUtils.ToGdbObjRefMsg(featureInfo.Feature);

				repairInfoMsg.PointsToDelete =
					ProtobufGeometryUtils.ToShapeMsg((IGeometry) featureInfo.PointsToDelete);

				repairInfoMsg.CrackPointsToAdd =
					ProtobufGeometryUtils.ToShapeMsg((IGeometry) featureInfo.CrackPointCollection);

				if (invalidSegments != null)
				{
					foreach (esriSegmentInfo segmentInfo in invalidSegments)
					{
						repairInfoMsg.InvalidSegments.Add(ToInvalidSegmentMsg(segmentInfo));
					}
				}

				response.RepairInfos.Add(repairInfoMsg);
			}

			return response;
		}

		private static ApplyRepairGeometryResponse PackApplyRepairGeometryResponse(
			[NotNull] Dictionary<IFeature, IGeometry> updateGeometryByFeature,
			[NotNull] List<string> nonStorableMessages)
		{
			var response = new ApplyRepairGeometryResponse();

			foreach (KeyValuePair<IFeature, IGeometry> kvp in updateGeometryByFeature)
			{
				IFeature feature = kvp.Key;
				IGeometry newGeometry = kvp.Value;

				var resultObject = new ResultObjectMsg();
				resultObject.Update = ProtobufGdbUtils.ToGdbObjectMsg(
					feature, newGeometry, feature.Class.ObjectClassID);

				response.ResultFeatures.Add(resultObject);
			}

			response.NonStorableMessages.AddRange(nonStorableMessages);

			return response;
		}

		private static InvalidSegmentMsg ToInvalidSegmentMsg(esriSegmentInfo segmentInfo)
		{
			var msg = new InvalidSegmentMsg();

			ISegment segment = segmentInfo.pSegment;

			if (segment != null)
			{
				msg.FromPoint = ProtobufGeometryUtils.ToShapeMsg(segment.FromPoint);
				msg.ToPoint = ProtobufGeometryUtils.ToShapeMsg(segment.ToPoint);
			}

			msg.AbsoluteIndex = segmentInfo.iAbsSegment;
			msg.PartIndex = segmentInfo.iPart;
			msg.RelativeIndex = segmentInfo.iRelSegment;

			return msg;
		}

		private static IGeometry ExtractVertices(IGeometry originalGeometry)
		{
			IPointCollection pointCollection = originalGeometry as IPointCollection;
			return pointCollection != null
				       ? GeometryFactory.CreateMultipoint(pointCollection)
				       : originalGeometry;
		}

		private static bool IsOnShortSegment(
			WKSPointZ wksPoint, [NotNull] FeatureVertexInfo vertexInfo)
		{
			IList<esriSegmentInfo> shortSegments = vertexInfo.ShortSegments;
			if (shortSegments == null) return false;

			IPoint point = GeometryFactory.CreatePoint(
				wksPoint, ((IGeoDataset) vertexInfo.Feature.Class).SpatialReference);

			return IsOnShortSegment(point, shortSegments);
		}

		private static bool IsOnShortSegment(
			[NotNull] IPoint point,
			[CanBeNull] IEnumerable<esriSegmentInfo> shortSegments)
		{
			if (shortSegments == null) return false;

			foreach (esriSegmentInfo segmentInfo in shortSegments)
			{
				IGeometry highLevelSegment =
					GeometryUtils.GetHighLevelGeometry(segmentInfo.pSegment, true);

				if (GeometryUtils.Intersects(point, highLevelSegment))
				{
					return true;
				}
			}

			return false;
		}

		private static void AddSegmentsToRemoveBetweenPointsToRemove(
			[NotNull] FeatureVertexInfo vertexInfo,
			[NotNull] IList<WKSPointZ> pointsToRemove)
		{
			IGeometry featureShape = vertexInfo.Feature.Shape;
			ISegmentCollection segments = featureShape as ISegmentCollection;
			if (segments == null) return;

			IMultipoint removeMultipoint = GeometryFactory.CreateMultipoint(
				pointsToRemove, DatasetUtils.GetGeometryDef(vertexInfo.Feature));

			double searchTolerance = GeometryUtils.GetXyTolerance(vertexInfo.Feature);
			var hitSegmentIndexes = new List<int>();

			foreach (WKSPointZ wksPointZ in pointsToRemove)
			{
				IPoint searchPoint = GeometryFactory.CreatePoint(
					wksPointZ, ((IGeometry) segments).SpatialReference);

				foreach (int index in
				         GeometryUtils.FindSegmentIndices(segments, searchPoint, searchTolerance))
				{
					if (! hitSegmentIndexes.Contains(index))
					{
						hitSegmentIndexes.Add(index);
					}
				}
			}

			foreach (int segmentIndex in hitSegmentIndexes)
			{
				IList<esriSegmentInfo> existing = vertexInfo.ShortSegments;
				bool alreadyInList = existing != null &&
				                     existing.Any(si => si.iAbsSegment == segmentIndex);
				if (alreadyInList) continue;

				ISegment segment = segments.Segment[segmentIndex];

				if (GeometryUtils.Intersects(segment.FromPoint, removeMultipoint) &&
				    GeometryUtils.Intersects(segment.ToPoint, removeMultipoint))
				{
					var segInfo = new esriSegmentInfo();
					segInfo.pSegment = segment;
					segInfo.iAbsSegment = segmentIndex;

					int partIdx;
					segInfo.iRelSegment = GetLocalSegmentIndex(segments, segmentIndex, out partIdx);
					segInfo.iPart = partIdx;

					if (vertexInfo.ShortSegments == null)
					{
						vertexInfo.ShortSegments = new List<esriSegmentInfo>();
					}

					vertexInfo.ShortSegments.Add(segInfo);
				}
			}
		}

		private static void AddLinearSelfIntersectionSegments(
			[NotNull] FeatureVertexInfo vertexInfo, bool use2D)
		{
			IGeometry geometry = vertexInfo.Feature.Shape;

			if (geometry == null || geometry.IsEmpty)
			{
				return;
			}

			var polycurve = geometry as IPolycurve;
			if (polycurve == null)
			{
				return;
			}

			double tolerance = GeometryUtils.GetXyTolerance(vertexInfo.Feature);
			bool in3D = ! use2D;

			MultiPolycurve multiPolycurve = GeometryConversionUtils.CreateMultiPolycurve(polycurve);
			var segmentCollection = (ISegmentCollection) geometry;

			int absSegOffset = 0;
			for (int partIdx = 0; partIdx < multiPolycurve.PartCount; partIdx++)
			{
				Linestring linestring = multiPolycurve.GetPart(partIdx);

				for (int localSegIdx = 0; localSegIdx < linestring.SegmentCount; localSegIdx++)
				{
					IList<Linestring> selfIntersections =
						GeomTopoOpUtils.GetLinearSelfIntersectionsXY(
							linestring, localSegIdx, tolerance, in3D);

					if (selfIntersections.Count == 0)
					{
						continue;
					}

					int absSegIdx = absSegOffset + localSegIdx;

					bool alreadyAdded = vertexInfo.ShortSegments != null &&
					                    vertexInfo.ShortSegments
					                              .Any(s => s.iAbsSegment == absSegIdx);
					if (alreadyAdded)
					{
						continue;
					}

					ISegment segment = segmentCollection.Segment[absSegIdx];

					var segInfo = new esriSegmentInfo
					              {
						              pSegment = segment,
						              iAbsSegment = absSegIdx,
						              iPart = partIdx,
						              iRelSegment = localSegIdx
					              };

					if (vertexInfo.ShortSegments == null)
					{
						vertexInfo.ShortSegments = new List<esriSegmentInfo>();
					}

					vertexInfo.ShortSegments.Add(segInfo);
				}

				absSegOffset += linestring.SegmentCount;
			}
		}

		private static int GetLocalSegmentIndex(
			[NotNull] ISegmentCollection segments, int globalSegmentIndex, out int partIndex)
		{
			partIndex = -1;

			IGeometryCollection geometryCollection = segments as IGeometryCollection;
			if (geometryCollection == null)
			{
				partIndex = 0;
				return globalSegmentIndex;
			}

			int currentPartIndex = 0;
			int currentPartStartIdx = 0;

			for (int i = 0; i < geometryCollection.GeometryCount; i++)
			{
				ISegmentCollection partSegments =
					(ISegmentCollection) geometryCollection.Geometry[i];

				if (globalSegmentIndex < currentPartStartIdx + partSegments.SegmentCount)
				{
					partIndex = currentPartIndex;
					return globalSegmentIndex - currentPartStartIdx;
				}

				currentPartIndex++;
				currentPartStartIdx += partSegments.SegmentCount;
			}

			Assert.CantReach("Unexpected global segment index");
			return -1;
		}

		private static IList<CrackPoint> GetFilteredCrackPoints(
			[NotNull] FeatureVertexInfo vertexInfo,
			[NotNull] IGeometry geometry,
			[NotNull] IGeometry simplified,
			[NotNull] IList<WKSPointZ> pointsToAdd)
		{
			var result = new List<CrackPoint>(pointsToAdd.Count);

			IMultipoint changedRingFromPoints = GetChangedStartPoint(geometry, simplified);

			foreach (WKSPointZ wksPointZ in pointsToAdd)
			{
				IPoint point = GeometryFactory.CreatePoint(
					wksPointZ, ((IGeoDataset) vertexInfo.Feature.Class).SpatialReference);

				if (! IsOnShortSegment(point, vertexInfo.ShortSegments) &&
				    ! GeometryUtils.Intersects(point, changedRingFromPoints))
				{
					result.Add(new CrackPoint(point));
				}
			}

			return result;
		}

		[NotNull]
		private static IMultipoint GetChangedStartPoint(
			[NotNull] IGeometry original, [NotNull] IGeometry simplified)
		{
			if (original.GeometryType != esriGeometryType.esriGeometryPolygon)
			{
				return GeometryFactory.CreateEmptyMultipoint(original);
			}

			IPolygon simplifiedRings = simplified as IPolygon;
			if (simplifiedRings == null)
			{
				return GeometryFactory.CreateEmptyMultipoint(original);
			}

			var resultList = new List<IPoint>();
			foreach (IRing ring in GeometryUtils.GetRings(simplifiedRings))
			{
				IPoint ringFromPoint = ring.FromPoint;

				int? originalIdx = GeometryUtils.FindHitVertexIndex(
					original, ringFromPoint, GeometryUtils.GetXyTolerance(original), out int _);

				if (originalIdx > 0)
				{
					resultList.Add(ringFromPoint);
				}
			}

			return GeometryFactory.CreateMultipoint(resultList);
		}

		/// <summary>
		/// Minimal ICrackingOptions implementation for part intersection crack calculation.
		/// </summary>
		private class RepairGeometryCrackingOptions : ICrackingOptions
		{
			private readonly double _snapTolerance;

			public RepairGeometryCrackingOptions(double snapTolerance)
			{
				_snapTolerance = snapTolerance;
			}

			public TargetFeatureSelection TargetFeatureSelection =>
				TargetFeatureSelection.SelectedFeatures;

			public bool RespectMinimumSegmentLength => false;
			public double MinimumSegmentLength => 0;
			public bool SnapToTargetVertices => _snapTolerance > 0;
			public double SnapTolerance => _snapTolerance;
			public bool UseSourceZs => false;

			public string GetLocalOverridesMessage() => null;
		}
	}
}

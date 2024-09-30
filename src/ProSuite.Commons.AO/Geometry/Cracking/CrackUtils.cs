using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using IPnt = ProSuite.Commons.Geom.IPnt;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public static class CrackUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static CrackPointCalculator CreateChopPointCalculator(
			[NotNull] ICrackingOptions crackingOptions,
			bool excludeInteriorInteriorIntersections,
			[CanBeNull] IEnvelope inExtent = null)
		{
			var cracker = new CrackPointCalculator(
				              crackingOptions,
				              IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
				              inExtent)
			              {
				              // chopping mode
				              AddCrackPointsOnExistingVertices = true
			              };

			if (excludeInteriorInteriorIntersections)
			{
				// only use line end points as intersection targets
				cracker.TargetTransformation =
					originalTarget =>
						originalTarget.Dimension == esriGeometryDimension.esriGeometry1Dimension
							? GeometryUtils.GetBoundary(originalTarget)
							: null;
			}

			return cracker;
		}

		public static CrackPointCalculator CreateCrackPointCalculator(
			ICrackingOptions crackingOptions,
			[CanBeNull] IEnvelope inExtent = null)
		{
			var cracker = new CrackPointCalculator(
				crackingOptions,
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints,
				inExtent);

			// Special handling of multipatch targets:
			cracker.TargetTransformation = ExtractBoundariesForMultipatches;

			return cracker;
		}

		public static CrackPointCalculator CreateMultipatchCrackPointCalculator(
			ICrackingOptions crackingOptions)
		{
			// Use Extent == null to include all selected features, even the ones outside the extent and those that are
			// so kaput that they cannot be found by relational operator (such as single vertical walls).
			var cracker = new CrackPointCalculator(
				crackingOptions,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
				null);

			// Only crack in 2D because roofs can have multiple Z values at the same XY location
			// Slight Z-differences within Z-tolerance are still corrected (at the cost of coplanarity and horizontal ridges!)
			// In3D does not work well with UseSourceZ.
			cracker.In3D = false;

			// only use vertices as intersection targets, do not crack at interior intersections
			cracker.TargetTransformation = ExtractVertices;

			// Intersect for multipatch rings is very slow with ArcObjects implementation:
			cracker.UseCustomIntersect = true;

			return cracker;
		}

		#region Crack point calculation

		[NotNull]
		public static IList<FeatureVertexInfo> CreateFeatureVertexInfos(
			[NotNull] IEnumerable<IFeature> selectedFeatures,
			[CanBeNull] IEnvelope inExtent,
			[NotNull] ICrackingOptions crackingOptions)
		{
			double? snapTolerance = crackingOptions.SnapToTargetVertices
				                        ? (double?) crackingOptions.SnapTolerance
				                        : null;

			double? minimumSegmentLength = crackingOptions.RespectMinimumSegmentLength
				                               ? (double?)
				                               crackingOptions.MinimumSegmentLength
				                               : null;

			IList<FeatureVertexInfo> result = CreateFeatureVertexInfos(
				selectedFeatures, inExtent, snapTolerance, minimumSegmentLength);

			return result;
		}

		[NotNull]
		public static IList<FeatureVertexInfo> CreateFeatureVertexInfos(
			[NotNull] IEnumerable<IFeature> selectedFeatures,
			[CanBeNull] IEnvelope inExtent,
			double? snapTolerance,
			double? minimumSegmentLength)
		{
			var result =
				new List<FeatureVertexInfo>();

			foreach (IFeature selectedFeature in selectedFeatures)
			{
				FeatureVertexInfo vertexInfo = CreateFeatureVertexInfo(selectedFeature, inExtent,
					snapTolerance,
					minimumSegmentLength);
				if (vertexInfo != null)
				{
					result.Add(vertexInfo);
				}
			}

			return result;
		}

		[NotNull]
		public static IList<FeatureVertexInfo> CalculateFeatureVertexInfos(
			[NotNull] IEnumerable<IFeature> selectedFeatures,
			[CanBeNull] IEnumerable<IFeature> targetFeatures,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[NotNull] ICrackingOptions crackingOptions,
			[NotNull] IEnvelope inExtent,
			[CanBeNull] ITrackCancel trackCancel)
		{
			TargetFeatureSelection targetSelectionType = crackingOptions.TargetFeatureSelection;

			IList<FeatureVertexInfo> result =
				CreateFeatureVertexInfos(selectedFeatures, inExtent, crackingOptions);

			if (targetSelectionType == TargetFeatureSelection.SelectedFeatures)
			{
				AddFeatureIntersectionCrackPoints(
					result, crackPointCalculator, trackCancel);
			}
			else
			{
				if (targetFeatures == null)
				{
					throw new InvalidOperationException(
						"Target features can only be null if target feature selection type is SelectedFeatures");
				}

				AddTargetIntersectionCrackPoints(
					result, targetFeatures, targetSelectionType, crackPointCalculator, trackCancel);
			}

			return result;
		}

		[CanBeNull]
		public static FeatureVertexInfo CreateFeatureVertexInfo(
			[NotNull] IFeature selectedFeature,
			[CanBeNull] IEnvelope inExtent,
			double? snapTolerance,
			double? minimumSegmentLength)
		{
			// TODO: in theory multipoints might be supportable
			if (selectedFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint ||
			    selectedFeature.Shape.GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				_msg.VerboseDebug(
					() => $"Feature {GdbObjectUtils.ToString(selectedFeature)} is not of a " +
					      "supported geometry type. It will be disregarded.");

				return null;
			}

			if (inExtent != null && GeometryUtils.Disjoint(selectedFeature.Shape, inExtent))
			{
				_msg.VerboseDebug(
					() => $"Feature {GdbObjectUtils.ToString(selectedFeature)} is outside " +
					      "the extent of interest. It will be disregarded.");

				return null;
			}

			return new FeatureVertexInfo(selectedFeature, inExtent, snapTolerance,
			                             minimumSegmentLength);
		}

		/// <summary>
		/// Adds intersection- and crack-points for the specified features in vertexInfos
		/// with other target features.
		/// </summary>
		/// <param name="toVertexInfos"></param>
		/// <param name="targetFeatures">The target features, assuming none of them is also referenced
		/// by a feature vertex info.</param>
		/// <param name="targetFeatureSelection"></param>
		/// <param name="crackPointCalculator"></param>
		/// <param name="trackCancel"></param>
		public static void AddTargetIntersectionCrackPoints(
			[NotNull] IEnumerable<FeatureVertexInfo> toVertexInfos,
			[NotNull] IEnumerable<IFeature> targetFeatures,
			TargetFeatureSelection targetFeatureSelection,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[CanBeNull] ITrackCancel trackCancel)
		{
			ICollection<IFeature> targetFeatureCollection =
				CollectionUtils.GetCollection(targetFeatures);

			foreach (FeatureVertexInfo generalizationInfo in toVertexInfos)
			{
				AddTargetIntersectionCrackPoints(generalizationInfo, targetFeatureCollection,
				                                 targetFeatureSelection, crackPointCalculator,
				                                 trackCancel);
			}
		}

		/// <summary>
		/// Adds intersection- and crack-points for the specified features in vertexInfos
		/// with other target features.
		/// </summary>
		/// <param name="toVertexInfo"></param>
		/// <param name="targetFeatures">The target features, assuming none of them is also referenced</param>
		/// <param name="targetFeatureSelection"></param>
		/// <param name="crackPointCalculator"></param>
		/// <param name="trackCancel"></param>
		public static void AddTargetIntersectionCrackPoints(
			[NotNull] FeatureVertexInfo toVertexInfo,
			[NotNull] IEnumerable<IFeature> targetFeatures,
			TargetFeatureSelection targetFeatureSelection,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[CanBeNull] ITrackCancel trackCancel)
		{
			bool crackPointsOnlyWithinSameClass =
				targetFeatureSelection == TargetFeatureSelection.SameClass;

			AddTargetIntersectionCrackPoints(toVertexInfo, targetFeatures,
			                                 crackPointsOnlyWithinSameClass, crackPointCalculator,
			                                 trackCancel);
		}

		/// <summary>
		/// Adds intersection- and crack-points for the specified features in vertexInfos
		/// with other target features.
		/// </summary>
		/// <param name="toVertexInfo"></param>
		/// <param name="targetFeatures">The target features, assuming none of them is also referenced</param>
		/// <param name="crackPointsOnlyWithinSameClass">Whether cracking is only performed within the same class.</param>
		/// <param name="crackPointCalculator"></param>
		/// <param name="trackCancel"></param>
		public static void AddTargetIntersectionCrackPoints(
			[NotNull] FeatureVertexInfo toVertexInfo,
			[NotNull] IEnumerable<IFeature> targetFeatures,
			bool crackPointsOnlyWithinSameClass,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[CanBeNull] ITrackCancel trackCancel)
		{
			foreach (IFeature targetFeature in targetFeatures)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				if (crackPointsOnlyWithinSameClass &&
				    ! DatasetUtils.IsSameObjectClass(targetFeature.Class,
				                                     toVertexInfo.Feature.Class))
				{
					continue;
				}

				if (IsSameFeature(targetFeature, toVertexInfo.Feature))
				{
					continue;
				}

				IGeometry sourceShape = toVertexInfo.Feature.Shape;
				IGeometry targetShape = targetFeature.Shape;
				bool cannotIntersect = crackPointCalculator.CannotIntersect(sourceShape,
					targetShape);
				Marshal.ReleaseComObject(sourceShape);
				Marshal.ReleaseComObject(targetShape);

				if (cannotIntersect)
				{
					continue;
				}

				AddCrackPoints(toVertexInfo, targetFeature, crackPointCalculator);
			}
		}

		public static void AddFeatureIntersectionCrackPoints(
			[NotNull] IEnumerable<FeatureVertexInfo> vertexInfos,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[CanBeNull] ITrackCancel trackCancel)
		{
			foreach (
				KeyValuePair<FeatureVertexInfo, FeatureVertexInfo> pair in
				CollectionUtils.GetAllTuples(vertexInfos))
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				FeatureVertexInfo vertexInfo1 = pair.Key;
				FeatureVertexInfo vertexInfo2 = pair.Value;

				AddCrackPoints(vertexInfo1, vertexInfo2.Feature, crackPointCalculator);
				AddCrackPoints(vertexInfo2, vertexInfo1.Feature, crackPointCalculator);
			}
		}

		public static void AddSelfIntersectionCrackPoints3d(
			[NotNull] FeatureVertexInfo toVertexInfo,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[CanBeNull] ISpatialReference processingSpatialReference = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			// TODO: Consider moving this to CrackPointCalculator
			crackPointCalculator.SetDataResolution(toVertexInfo.Feature);

			IGeometry inputGeometry =
				EnsureInputGeometryProjection(toVertexInfo, processingSpatialReference);

			var inputMultipatch = inputGeometry as IMultiPatch;

			var segmentSalad =
				inputMultipatch != null
					? (ISegmentList) GeometryConversionUtils.CreatePolyhedron(inputMultipatch)
					: GeometryConversionUtils.CreateMultiPolycurve((IPolycurve) inputGeometry);

			bool useSnapping =
				crackPointCalculator.SnapTolerance != null &&
				! double.IsNaN((double) crackPointCalculator.SnapTolerance) &&
				crackPointCalculator.SnapTolerance > 0.0;

			double tolerance = useSnapping
				                   ? (double) crackPointCalculator.SnapTolerance
				                   : GeometryUtils.GetXyTolerance(inputGeometry);

			IList<IntersectionPoint3D> intersectionPoints =
				GeomTopoOpUtils.GetSelfIntersections(segmentSalad, tolerance, true);

			// Filter the segment interior-segment interior intersections:
			var vertexIntersections =
				intersectionPoints.Where(
					ip => ip.IsSourceVertex() || ip.VirtualTargetVertex % 1 == 0).ToList();

			// 3D-clustering, even if In3D == false, otherwise the averaged Z values result in
			// crack points where no crack point should be detected.
			IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> clusteredIntersections =
				GeomTopoOpUtils.Cluster(vertexIntersections, ip => ip.Point, tolerance, tolerance);

			// Legacy - directly use ISegmentList also in crackPointCalculator.DetermineCrackPoints
			IPolyline polylineSalad = inputMultipatch != null
				                          ? (IPolyline) GeometryFactory.CreatePolyline(
					                          inputMultipatch)
				                          : GeometryFactory.CreatePolyline(inputGeometry);

			IList<CrackPoint> crackPoints = crackPointCalculator.DetermineCrackPoints3d(
				clusteredIntersections, segmentSalad, inputGeometry, polylineSalad,
				(IPointList) segmentSalad);

			toVertexInfo.AddCrackPoints(crackPoints);
		}

		/// <summary>
		/// Calculates the crack points between the geometry parts of the specified FeatureVertexInfo's feature
		/// and adds them to its CrackPoints.
		/// </summary>
		/// <param name="toVertexInfo"></param>
		/// <param name="crackPointCalculator"></param>
		/// <param name="processingSpatialReference"></param>
		/// <param name="trackCancel"></param>
		public static void AddGeometryPartIntersectionCrackPoints(
			[NotNull] FeatureVertexInfo toVertexInfo,
			[NotNull] CrackPointCalculator crackPointCalculator,
			[CanBeNull] ISpatialReference processingSpatialReference = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			IGeometry inputGeometry =
				EnsureInputGeometryProjection(toVertexInfo, processingSpatialReference);

			if (IntersectionUtils.UseCustomIntersect &&
			    ! GeometryUtils.HasNonLinearSegments(inputGeometry) &&
			    ! GeometryUtils.IsMAware(inputGeometry))
			{
				AddSelfIntersectionCrackPoints3d(toVertexInfo, crackPointCalculator,
				                                 processingSpatialReference, trackCancel);

				return;
			}

			var inputMultipatch = inputGeometry as IMultiPatch;

			crackPointCalculator.SetDataResolution(toVertexInfo.Feature);

			IGeometry polylineSalad = inputMultipatch != null
				                          ? GeometryFactory.CreatePolyline(inputMultipatch)
				                          : GeometryFactory.CreatePolyline(inputGeometry);

			List<IPolyline> highLevelPaths =
				GeometryUtils.GetParts((IGeometryCollection) polylineSalad)
				             .Select(path => (IPolyline) GeometryUtils.GetHighLevelGeometry(path))
				             .ToList();

			var allIntersections =
				(IPointCollection) GeometryFactory.CreateEmptyMultipoint(inputGeometry);

			var calculationCount = 0;
			foreach (
				KeyValuePair<IPolyline, IPolyline> tuple in
				CollectionUtils.GetAllTuples(highLevelPaths))
			{
				IPolyline geo1 = tuple.Key;
				IPolyline geo2 = tuple.Value;

				if (crackPointCalculator.CannotIntersect(geo1, geo2))
				{
					continue;
				}

				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				// Due to the target transformation (extracting only vertices) the intersection is not symmetrical
				// and has to be performed in both directions:
				var intersectionPnts = crackPointCalculator.GetIntersectionPoints(
					geo1, geo2, out IGeometry _);

				IPointCollection intersectionPoints = GeometryConversionUtils.CreatePointCollection(
					geo1, intersectionPnts.Select(kvp => kvp.Key).ToList());

				allIntersections.AddPointCollection(intersectionPoints);

				intersectionPnts = crackPointCalculator.GetIntersectionPoints(
					geo2, geo1, out IGeometry _);

				intersectionPoints = GeometryConversionUtils.CreatePointCollection(
					geo1, intersectionPnts.Select(kvp => kvp.Key).ToList());

				allIntersections.AddPointCollection(intersectionPoints);

				calculationCount++;
			}

			if (allIntersections.PointCount == 0)
			{
				return;
			}

			IPointCollection clustered = ClusterPoints(allIntersections,
			                                           toVertexInfo.SnapTolerance);

			_msg.VerboseDebug(
				() =>
					$"Calculated {allIntersections.PointCount} intersections in {calculationCount} " +
					"ring pairs.");

			IList<CrackPoint> crackPoints = crackPointCalculator.DetermineCrackPoints(
				clustered, inputGeometry, (IPolyline) polylineSalad, inputGeometry);

			toVertexInfo.AddCrackPoints(crackPoints);
		}

		public static IPolyline CreatePolylineSalad(IGeometry geometry)
		{
			IPolyline result = GeometryFactory.CreatePolyline(geometry.SpatialReference,
			                                                  GeometryUtils.IsZAware(geometry),
			                                                  GeometryUtils.IsMAware(geometry));

			var resultLineCollection = (IGeometryCollection) result;
			var sourceCollection = (IGeometryCollection) geometry;

			object missing = Type.Missing;

			for (var i = 0; i < sourceCollection.GeometryCount; i++)
			{
				IPath path = GeometryFactory.CreatePath((IRing) sourceCollection.get_Geometry(i));

				resultLineCollection.AddGeometry(path, ref missing, ref missing);
			}

			return result;
		}

		public static void AddCrackPoints(
			FeatureVertexInfo featureVertexInfo,
			[NotNull] IFeature targetFeature,
			[NotNull] CrackPointCalculator crackPointCalculator)
		{
			IFeature sourceFeature = featureVertexInfo.Feature;

			Stopwatch watch =
				_msg.DebugStartTiming("Calculating intersection points between {0} and {1}",
				                      GdbObjectUtils.ToString(sourceFeature),
				                      GdbObjectUtils.ToString(targetFeature));

			IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> intersectionPnts = null;
			try
			{
				IGeometry targetGeometry = targetFeature.ShapeCopy;
				IGeometry originalGeometry = sourceFeature.Shape;
				IPolyline clippedSource = featureVertexInfo.OriginalClippedPolyline;

				GeometryUtils.EnsureSpatialReference(targetGeometry,
				                                     clippedSource.SpatialReference);

				crackPointCalculator.SetDataResolution(sourceFeature);

				IGeometry intersectionTarget;
				intersectionPnts = crackPointCalculator.GetIntersectionPoints(
					clippedSource, targetGeometry, out intersectionTarget);

				var intersectionPoints = GeometryConversionUtils.CreatePointCollection(
					originalGeometry, intersectionPnts.Select(kvp => kvp.Key).ToList());

				featureVertexInfo.AddIntersectionPoints(intersectionPoints);

				IList<CrackPoint> crackPoints = crackPointCalculator.DetermineCrackPoints(
					intersectionPnts, originalGeometry, clippedSource, intersectionTarget);

				// TODO: rename to AddNonCrackablePoints / sort out whether drawing can happen straight from List<CrackPoint>
				featureVertexInfo.AddCrackPoints(crackPoints);

				if (intersectionTarget != null && intersectionTarget != targetGeometry)
				{
					Marshal.ReleaseComObject(intersectionTarget);
				}

				Marshal.ReleaseComObject(targetGeometry);
			}
			catch (Exception e)
			{
				string message =
					$"Error calculationg crack points with target feature {RowFormat.Format(targetFeature)}: {e.Message}";

				_msg.Debug(message, e);

				if (crackPointCalculator.ContinueOnException)
				{
					crackPointCalculator.FailedOperations.Add(sourceFeature.OID, message);
				}
				else
				{
					throw;
				}
			}

			_msg.DebugStopTiming(watch, "Calculated and processed {0} intersection points",
			                     intersectionPnts?.Count);
		}

		#endregion

		#region Add or remove crack / weed points from feature vertex info

		public static void RemovePoints(
			[NotNull] ICollection<FeatureVertexInfo> vertexInfos,
			[NotNull] IDictionary<IFeature, IGeometry> resultGeometries,
			[CanBeNull] IProgressFeedback progressFeedback,
			[CanBeNull] ITrackCancel cancel,
			[CanBeNull] IGeometry withinArea = null)
		{
			AddRemovePoints(vertexInfos, resultGeometries, true, progressFeedback, cancel,
			                withinArea);
		}

		public static void AddRemovePoints(
			[NotNull] ICollection<FeatureVertexInfo> vertexInfos,
			[NotNull] IDictionary<IFeature, IGeometry> resultGeometries,
			[CanBeNull] IProgressFeedback progressFeedback,
			[CanBeNull] ITrackCancel cancel,
			[CanBeNull] IGeometry withinArea = null)
		{
			AddRemovePoints(vertexInfos, resultGeometries, false, progressFeedback, cancel,
			                withinArea);
		}

		public static void CrackMultipatch([NotNull] IMultiPatch multiPatch,
		                                   [NotNull] IPointCollection pointsToAdd,
		                                   [CanBeNull] double? maxSnapTolerance)
		{
			double searchTolerance = maxSnapTolerance ??
			                         GeometryUtils.GetXyTolerance(multiPatch);

			// otherwise point ids are not set
			GeometryUtils.MakePointIDAware(multiPatch);

			foreach (IPoint point in GeometryUtils.GetPoints(pointsToAdd))
			{
				TryInsertPointIntoMultipatch(multiPatch, point, searchTolerance);

				// make sure that new vertices on target geometries
				// get interpolated z values (if they are z aware and the inserted point had NaN Z) 
				var zValues = multiPatch as IZ;
				if (zValues != null && GeometryUtils.IsZAware(multiPatch))
				{
					zValues.CalculateNonSimpleZs();
				}
			}

			// otherwise the changes are not stored:
			((IGeometryCollection) multiPatch).GeometriesChanged();

			multiPatch.InvalXYFootprint();
		}

		public static void CrackMultipatchRing([NotNull] IRing ring,
		                                       [NotNull] IPoint crackPoint,
		                                       double tolerance,
		                                       double? coplanarityTolerance = null,
		                                       [CanBeNull] List<Plane3D> relevantPlanes = null,
		                                       [CanBeNull] Plane3D thisRingPlane = null)
		{
			List<int> verticesToUpdate =
				GeometryUtils.FindVertexIndices((IPointCollection) ring, crackPoint, tolerance)
				             .ToList();

			verticesToUpdate.Sort();
			verticesToUpdate.Reverse();

			// Within the ring the XY of the actual crack point should be maintained. This is particularly
			// relevant, when a segment is split.
			IPoint pointToInsert = crackPoint;
			foreach (int vertexToUpdate in verticesToUpdate)
			{
				IPoint existingVertex = ((IPointCollection) ring).Point[vertexToUpdate];

				pointToInsert = relevantPlanes != null
					                ? GetCoplanarCrackPoint(
						                pointToInsert, existingVertex, thisRingPlane,
						                relevantPlanes,
						                tolerance, Assert.NotNull(coplanarityTolerance).Value)
					                : GeometryFactory.Clone(pointToInsert);

				UpdateExistingVertex(vertexToUpdate, (IPointCollection) ring, pointToInsert,
				                     tolerance);
			}

			const bool excludeBoundaryMatches = true;
			List<int> segmentsToUpdate =
				GeometryUtils.FindSegmentIndices((ISegmentCollection) ring, crackPoint, tolerance,
				                                 excludeBoundaryMatches).ToList();

			if (segmentsToUpdate.Count > 1 &&
			    Math.Abs(segmentsToUpdate[0] - segmentsToUpdate[1]) == 1)
			{
				// TOP-5227: Never insert the same point in 2 (adjacent) segments to avoid cut-backs
				_msg.WarnFormat("Crack point {0}|{1} is within tolerance of 2 different " +
				                "segments' interior. It was skipped to avoid a cut-back.",
				                crackPoint.X, crackPoint.Y);
				return;
			}

			segmentsToUpdate.Sort();
			segmentsToUpdate.Reverse();
			foreach (int segmentIndex in segmentsToUpdate)
			{
				IPoint pointOnSegment = GetPointOnSegment(ring, segmentIndex, pointToInsert);

				pointToInsert = relevantPlanes != null
					                ? GetCoplanarCrackPoint(
						                pointToInsert, pointOnSegment, thisRingPlane,
						                relevantPlanes,
						                tolerance, Assert.NotNull(coplanarityTolerance).Value)
					                : GeometryFactory.Clone(pointToInsert);

				CrackExistingSegment(segmentIndex, ring, pointToInsert, tolerance);
			}
		}

		public static IPoint GetPointOnSegment(IRing ring, int segmentIndex, IPoint atLocation)
		{
			ISegment segment = ((ISegmentCollection) ring).Segment[segmentIndex];

			IPoint pointOnSegment = segment.FromPoint;

			if (segment.Length > 0)
			{
				GeometryUtils.GetDistanceFromCurve(atLocation, segment, pointOnSegment);
			}

			return pointOnSegment;
		}

		#endregion

		#region Line splitting

		/// <summary>
		/// Calculates the split points for geometric network edges. These need to be split
		/// by inserting the junction rather than updating the edge geometries.
		/// </summary>
		/// <param name="featureVertexInfos"></param>
		/// <param name="cancel"></param>
		/// <param name="withinArea"></param>
		/// <returns></returns>
		public static IDictionary<IFeature, IGeometry> GetSplitPoints(
			[NotNull] IEnumerable<FeatureVertexInfo> featureVertexInfos,
			[CanBeNull] ITrackCancel cancel,
			[CanBeNull] IGeometry withinArea)
		{
			IDictionary<IFeature, IGeometry> result = new Dictionary<IFeature, IGeometry>();

			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				IGeometry originalPolyline = featureVertexInfo.Feature.Shape;

				Assert.True(originalPolyline.GeometryType ==
				            esriGeometryType.esriGeometryPolyline,
				            "Only polyline features are supported for split.");

				IPointCollection crackPoints = featureVertexInfo.GetCrackPoints(withinArea);

				if (crackPoints == null || crackPoints.PointCount == 0)
				{
					continue;
				}

				// add all points, even those that are slightly off the edge -> the edge will 
				// be modified before inserting the junction
				result.Add(featureVertexInfo.Feature,
				           GeometryFactory.CreateMultipoint(crackPoints));
			}

			return result;
		}

		/// <summary>
		/// Returns the specified feature vertex info's features with the associated split line.
		/// Additional features are created for the shorter part of the line to split.
		/// NOTE: Use GetSplitPoints for geometric network edge features
		/// </summary>
		/// <param name="featureVertexInfos"></param>
		/// <param name="cancel"></param>
		/// <param name="withinArea"></param>
		/// <returns></returns>
		public static IDictionary<FeatureVertexInfo, IEnumerable<IPolyline>> GetSplitLineGeometries(
			[NotNull] IEnumerable<FeatureVertexInfo> featureVertexInfos,
			[CanBeNull] ITrackCancel cancel,
			[CanBeNull] IGeometry withinArea)
		{
			IDictionary<FeatureVertexInfo, IEnumerable<IPolyline>> result =
				new Dictionary<FeatureVertexInfo, IEnumerable<IPolyline>>();

			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				IGeometry originalPolyline = featureVertexInfo.Feature.Shape;

				Assert.True(originalPolyline.GeometryType ==
				            esriGeometryType.esriGeometryPolyline,
				            "Only polyline features are supported for split.");

				IPointCollection crackPoints = featureVertexInfo.GetCrackPoints(withinArea);

				if (crackPoints == null)
				{
					continue;
				}

				IEnumerable<IPolyline> splitLines = GetSplitPolycurves(
					featureVertexInfo.Feature, crackPoints, featureVertexInfo.SnapTolerance);

				result.Add(featureVertexInfo, splitLines);
			}

			return result;
		}

		/// <summary>
		/// Returns the crack points ordered such that the split points that result in 
		/// short pieces come first. This ensures that the longest piece will keep the
		/// identity of the original feature when using geometric network splits. 
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static IList<IPoint> GetOrderedChopPoints(
			[NotNull] IPointCollection chopPoints,
			[NotNull] IPolyline polylineToSplit)
		{
			// general idea:
			// continuously chop off the smaller end part until the longest part remains

			IPolyline lineClone = GeometryFactory.Clone(polylineToSplit);

			var choppedCollection = (IGeometryCollection) lineClone;

			Assert.AreEqual(1, choppedCollection.GeometryCount,
			                "Input geometry count is not 1. Multipart lines are not supported.");

			GeometryUtils.CrackPolycurve(lineClone, chopPoints, false, true, null);

			var result = new List<IPoint>();
			while (choppedCollection.GeometryCount > 1)
			{
				var firstPart = (ICurve) choppedCollection.get_Geometry(0);

				var lastPart = (ICurve) choppedCollection.get_Geometry(
					choppedCollection.GeometryCount - 1);

				IPoint splitPoint;

				if (firstPart.Length < lastPart.Length)
				{
					GeometryUtils.RemoveParts(choppedCollection, new List<int> { 0 });
					splitPoint = firstPart.ToPoint;
				}
				else
				{
					GeometryUtils.RemoveParts(choppedCollection,
					                          new List<int>
					                          { choppedCollection.GeometryCount - 1 });
					splitPoint = lastPart.FromPoint;
				}

				choppedCollection.GeometriesChanged();

				result.Add(splitPoint);
			}

			Marshal.ReleaseComObject(lineClone);

			return result;
		}

		#endregion

		#region Weeding

		/// <summary>
		/// Gets the points that are removed by the Douglas-Peucker algorithm using the specified
		/// tolerance. Non-linear segments are handled according to the <paramref name="omitNonLinearSegments"/>
		/// parameter.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="weedTolerance">The tolerance (max allowable offset of the original geometry).</param>
		/// <param name="only2D">Whether the 2D distance should be compared with the weed tolerance
		/// even if the polycurve is z-aware.</param>
		/// <param name="inPerimeter">The area of interest</param>
		/// <param name="omitNonLinearSegments">Whether non-linear segments should be ignored or linearized</param>
		/// <param name="dataSpatialReference">The data spatial reference used to snap coordinates.
		/// This is important when non-linear segments are linearized.</param>
		/// <returns></returns>
		public static IPointCollection GetWeedPoints(
			[NotNull] IPolycurve polycurve,
			double weedTolerance,
			bool only2D,
			[CanBeNull] IGeometry inPerimeter = null,
			bool omitNonLinearSegments = true,
			ISpatialReference dataSpatialReference = null)
		{
			// NOTE regarding standard weed/generalize:
			// For non-linear segments, it is not implemented in 3D and inserts arbitrary
			// results depending on the subdivision of curves. Additionally, removing segments
			// would require a simplify which also inserts undesired vertices at self intersections
			// For additional issues see repro-tests.
			MultiPolycurve multiPolycurve = GeometryConversionUtils.CreateMultiPolycurve(
				polycurve, omitNonLinearSegments);

			if (! omitNonLinearSegments && dataSpatialReference != null)
			{
				// Linearization results in un-even and slighty different values. Must be snapped
				// to avoid non-deterministic generalization.
				Assert.NotNull(dataSpatialReference)
				      .GetDomain(out double xOrigin, out _, out double yOrigin, out _);
				dataSpatialReference.GetZDomain(out double zOrigin, out _);
				double resolution = SpatialReferenceUtils.GetXyResolution(dataSpatialReference);

				multiPolycurve.SnapToResolution(resolution, xOrigin, yOrigin, zOrigin);
			}

			if (! GeometryUtils.IsZAware(polycurve))
			{
				only2D = true;
			}

			MultiPolycurve weededPolycurve =
				GeomUtils.Generalize(multiPolycurve, weedTolerance, only2D);

			if (multiPolycurve.IsEmpty)
			{
				return new MultipointClass();
			}

			double xyTolerance = GeometryUtils.GetXyTolerance(polycurve);

			EnvelopeXY envelopeXY =
				inPerimeter != null
					? GeometryConversionUtils.CreateEnvelopeXY(inPerimeter.Envelope)
					: null;

			IEnumerable<IPnt> weededPoints =
				GeomTopoOpUtils.GetDifferencePoints(
					multiPolycurve, weededPolycurve, xyTolerance, ! only2D);

			Multipoint<IPnt> weededMultipoint = new Multipoint<IPnt>(weededPoints);

			GeomTopoOpUtils.Simplify(weededMultipoint, xyTolerance);

			IPointCollection result =
				GeometryConversionUtils.CreatePointCollection(
					polycurve, weededMultipoint.GetPoints(), envelopeXY, xyTolerance);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(
					() =>
						$"Identified {result.PointCount} points to weed. Weeded geometry: " +
						$"{weededPolycurve}");
			}

			return result;
		}

		/// <summary>
		/// Gets the points that are removed by the Douglas-Peucker algorithm using the specified
		/// tolerance. Non-linear segments are ignored as IPolycurve.Generalize adds additional vertices. 
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="weedTolerance"></param>
		/// <param name="only2D"></param>
		/// <param name="inPerimeter"></param>
		/// <returns></returns>
		public static IPointCollection GetWeedPointsLegacy(
			[NotNull] IPolycurve polycurve, double weedTolerance, bool only2D,
			[CanBeNull] IGeometry inPerimeter)
		{
			if (GeometryUtils.HasNonLinearSegments(polycurve))
			{
				polycurve = GeometryFactory.Clone(polycurve);

				// remove non-linear segments to avoid not-implemented exception (if 3D)
				// and to avoid arbitrary results depending on the subdivision of curves:
				// this method only reports vertices that existed in the old curve.
				IList<int> nonLinearSegments = GetNonLinearSegments(polycurve);

				if (nonLinearSegments.Count > 0)
				{
					_msg.DebugFormat(
						"The polycurve contains {0} non-linear segments. They are excluded from generalization",
						nonLinearSegments.Count);
				}

				const bool closeGap = false;
				GeometryUtils.RemoveSegments((ISegmentCollection) polycurve, nonLinearSegments,
				                             closeGap);

				// Do not simplify here to avoid the lines split at crack points to be re-merged 
				// (which would result in phantom non-deletable points)
			}

			if (((IGeometry) polycurve).IsEmpty)
			{
				return new MultipointClass();
			}

			IPolycurve weededCurve = GeometryFactory.Clone(polycurve);

			Generalize(weededCurve, weedTolerance, only2D);

			// WORK-AROUND:
			// sometimes IPolycurve3D.Generalize3D() changes the From/To point's Z values (within the tolerance, but still)
			// which thus results in a weed point -> but never weed From/To points!
			weededCurve.FromPoint = polycurve.FromPoint;
			weededCurve.ToPoint = polycurve.ToPoint;
			// END WORK-AROUND

			var weededPoints =
				(IPointCollection) GetWeededPoints(polycurve, weededCurve,
				                                   inPerimeter);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(
					() =>
						$"Identified {weededPoints.PointCount} points to weed. Weeded geometry: " +
						$"{GeometryUtils.ToString(weededCurve)}");
			}

			return weededPoints;
		}

		public static IPointCollection RemovePoints(
			[NotNull] IPointCollection fromPointCollection,
			[NotNull] IPointCollection pointsToRemove)
		{
			var removeCount = 0;

			var removedPoints = new List<IPoint>();

			for (var i = 0; i < fromPointCollection.PointCount; i++)
			{
				IPoint pointToTest = fromPointCollection.get_Point(i);

				if (GeometryUtils.Contains((IGeometry) pointsToRemove, pointToTest))
				{
					removedPoints.Add(pointToTest);

					fromPointCollection.RemovePoints(i, 1);
					i--;
					removeCount++;
				}
			}

			if (removeCount > 0)
			{
				_msg.DebugFormat("RemoveProtectedPoints had to remove {0} points!", removeCount);
			}

			return (IPointCollection) GeometryFactory.CreateMultipoint(removedPoints);
		}

		public static void AddUnnecessaryVerticesToDelete(
			[NotNull] IEnumerable<FeatureVertexInfo> featureVertexInfos,
			[CanBeNull] IPolygon perimeter)
		{
			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				// use tolerance from shape, not from feature (map SR)
				double tolerance = GeometryUtils.GetXyTolerance(featureVertexInfo.Feature.Shape);

				AddUnnecessaryVerticesToDelete(featureVertexInfo, tolerance, perimeter);
			}
		}

		public static void AddUnnecessaryVerticesToDelete(FeatureVertexInfo featureVertexInfo,
		                                                  double tolerance,
		                                                  IPolygon perimeter)
		{
			IPointCollection weededPoints;

			if (featureVertexInfo.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				// Generalize simplified polylines to 
				// - remove vertices on straight lines in 2 coincident line strings
				// - avoid deleting a vertex on a straight line string that is also a (necessary) vertex in a touching ring
				IPolyline multipatchAsPolyline =
					GeometryFactory.Clone(featureVertexInfo.OriginalClippedPolyline);

				GeometryUtils.Simplify(multipatchAsPolyline, true, true);

				weededPoints =
					GetWeedPoints(multipatchAsPolyline, tolerance, false, perimeter);
			}
			else
			{
				weededPoints =
					GetWeedPoints(featureVertexInfo.OriginalClippedPolyline, tolerance, false,
					              perimeter);
			}

			if (featureVertexInfo.IntersectionPoints != null)
			{
				// Exclude at the intersection points with other geometries (IntersectionPoints are added in target-intersection calculation)
				RemovePoints(weededPoints, featureVertexInfo.IntersectionPoints);
			}

			featureVertexInfo.PointsToDelete = weededPoints;
		}

		#endregion

		#region Private methods

		private static bool IsSameFeature(IFeature firstFeature, IFeature secondeFeature)
		{
			if (firstFeature.OID == secondeFeature.OID)
			{
				return DatasetUtils.IsSameObjectClass(firstFeature.Class, secondeFeature.Class,
				                                      ObjectClassEquality.SameTableSameVersion);
			}

			return false;
		}

		/// <summary>
		/// Splits the specified polyline feature. A new feature with the shorter part is created.
		/// This method should be called within a gdb transaction. 
		/// </summary>
		/// <param name="polylineFeature">The original feature.</param>
		/// <param name="splitPoint">The split point</param>
		/// <param name="projectSplitPointOntoLine">Whether the split point should be projected onto the polyline geometry
		/// or not. If not, the two geometries might form a kink at the split point. This could be desireable if the split
		/// point is for example an existing junction which should not be moved.</param>
		/// <returns></returns>
		public static IList<IFeature> SplitPolylineFeature(IFeature polylineFeature,
		                                                   IPoint splitPoint,
		                                                   bool projectSplitPointOntoLine)
		{
			var originalLine = (IPolyline) polylineFeature.ShapeCopy;

			IPolyline shorterGeometry;
			IPolyline longerGeometry;

			if (! GeometryUtils.TrySplitPolyline(originalLine, splitPoint,
			                                     projectSplitPointOntoLine,
			                                     out shorterGeometry, out longerGeometry))
			{
				return null;
			}

			const bool exceptShape = true;
			IFeature newFeature = GdbObjectUtils.DuplicateFeature(polylineFeature, exceptShape);

			GdbObjectUtils.SetFeatureShape(newFeature, shorterGeometry);
			GdbObjectUtils.SetFeatureShape(polylineFeature, longerGeometry);

			newFeature.Store();
			polylineFeature.Store();

			IList<IFeature> splitFeatures = new List<IFeature>(2) { newFeature, polylineFeature };

			return splitFeatures;
		}

		private static void Generalize(IPolycurve polycurve, double tolerance, bool only2D)
		{
			var zAware = polycurve as IZAware;

			if (only2D || zAware == null || ! zAware.ZAware)
			{
				polycurve.Generalize(tolerance);
			}
			else
			{
				((IPolycurve3D) polycurve).Generalize3D(tolerance);
			}
		}

		private static IList<int> GetNonLinearSegments(IPolycurve polycurve)
		{
			var nonLinearSegmentIndexes = new List<int>();

			var allSegments = (ISegmentCollection) polycurve;

			for (var i = 0; i < allSegments.SegmentCount; i++)
			{
				if (allSegments.get_Segment(i).GeometryType != esriGeometryType.esriGeometryLine)
				{
					nonLinearSegmentIndexes.Add(i);
				}
			}

			return nonLinearSegmentIndexes;
		}

		[NotNull]
		private static IMultipoint GetWeededPoints([NotNull] IGeometry originalGeometry,
		                                           [NotNull] IGeometry weededGeometry,
		                                           [CanBeNull] IGeometry inPerimeter)
		{
			IMultipoint result;

			const double tolerance = 0d;
			var geometryComparison = new GeometryComparison(originalGeometry, weededGeometry,
			                                                tolerance, tolerance);

			IDictionary<WKSPointZ, VertexIndex> weededPointsDictionary =
				geometryComparison.GetDifference(GeometryUtils.IsZAware(originalGeometry));

			int origPointCount = ((IPointCollection) originalGeometry).PointCount;
			int weededPointCount = ((IPointCollection) weededGeometry).PointCount;

			if (origPointCount - weededPointCount != weededPointsDictionary.Count)
			{
				_msg.DebugFormat(
					"Generalization algorithm changed points. Before: {0}{1} After: {2}",
					GeometryUtils.ToString(originalGeometry), Environment.NewLine,
					GeometryUtils.ToString(weededGeometry));

				throw new InvalidDataException(
					"The geometry could not be generalized. It might contain boundary loops or other characteristics that prevent correct generalization.");
			}

			ISpatialReference spatialReference = originalGeometry.SpatialReference;

			if (inPerimeter != null)
			{
				ICollection<WKSPointZ> weededPoints = weededPointsDictionary.Keys;

				IList<WKSPointZ> filteredPoints = FilterPointsOutsidePerimeter(weededPoints,
					inPerimeter);

				result = CreateMultipoint(filteredPoints, spatialReference);
			}
			else
			{
				result = CreateMultipoint(weededPointsDictionary.Keys, spatialReference);
			}

			return result;
		}

		private static IMultipoint CreateMultipoint(ICollection<WKSPointZ> points,
		                                            ISpatialReference spatialReference)
		{
			var wksPointZs = new WKSPointZ[points.Count];

			points.CopyTo(wksPointZs, 0);

			return GeometryFactory.CreateMultipoint(wksPointZs, spatialReference);
		}

		private static IList<WKSPointZ> FilterPointsOutsidePerimeter(
			[NotNull] ICollection<WKSPointZ> points,
			[NotNull] IGeometry perimeter)
		{
			IPoint point = new PointClass();

			IList<WKSPointZ> filteredPoints = new List<WKSPointZ>(points.Count);

			foreach (WKSPointZ wksPointZ in points)
			{
				point.PutCoords(wksPointZ.X, wksPointZ.Y);

				if (GeometryUtils.Contains(perimeter, point))
				{
					filteredPoints.Add(wksPointZ);
				}
			}

			return filteredPoints;
		}

		private static string GetMessage(int totalAddCount, int totalRemovedCount,
		                                 int featureCount)
		{
			string message;
			if (totalAddCount > 0 && totalRemovedCount > 0)
			{
				message =
					string.Format("Updating {2} geometries by adding {0} and removing {1} point(s)",
					              totalAddCount, totalRemovedCount, featureCount);
			}
			else
			{
				if (totalAddCount > 0)
				{
					message = totalAddCount == 1
						          ? "Added 1 point to "
						          : string.Format("Added {0} points to ", totalAddCount);
				}
				else
				{
					message = totalRemovedCount == 1
						          ? "Removed 1 point from "
						          : string.Format("Removed {0} points from ", totalRemovedCount);
				}

				message += featureCount == 1
					           ? "1 geometry"
					           : string.Format("{0} geometries", featureCount);
			}

			return message;
		}

		private static void AddRemovePoints(
			[NotNull] ICollection<FeatureVertexInfo> vertexInfos,
			[NotNull] IDictionary<IFeature, IGeometry> resultGeometries,
			bool removePointsOnly,
			[CanBeNull] IProgressFeedback progressFeedback,
			[CanBeNull] ITrackCancel cancel,
			[CanBeNull] IGeometry withinArea)
		{
			progressFeedback?.SetRange(0, vertexInfos.Count + 1);

			var featureCount = 0;
			var totalRemovedCount = 0;
			var totalAddCount = 0;

			foreach (FeatureVertexInfo vertexInfo in vertexInfos)
			{
				if (cancel != null && ! cancel.Continue())
				{
					return;
				}

				IPointCollection pointsToRemove = vertexInfo.GetPointsToDelete(withinArea);

				IPointCollection pointsToEnsure = removePointsOnly
					                                  ? null
					                                  : vertexInfo.GetCrackPoints(withinArea);

				if (pointsToEnsure == null && pointsToRemove == null)
				{
					continue;
				}

				IFeature feature = vertexInfo.Feature;
				IGeometry updateGeometry;
				if (! resultGeometries.TryGetValue(feature, out updateGeometry))
				{
					updateGeometry = feature.ShapeCopy;

					if (vertexInfo.LinearizeSegments && updateGeometry is IPolycurve polycurve)
					{
						GeometryUtils.EnsureLinearized(polycurve, 0);
					}

					resultGeometries.Add(feature, updateGeometry);
				}

				try
				{
					AddRemovePoints(updateGeometry, pointsToEnsure, pointsToRemove,
					                vertexInfo.SnapTolerance);

					if (updateGeometry.IsEmpty)
					{
						_msg.WarnFormat(
							"Geometry for {0} would become empty. The feature was not changed.",
							GdbObjectUtils.ToString(feature));
					}
					else
					{
						if (pointsToRemove != null)
						{
							totalRemovedCount += pointsToRemove.PointCount;
						}

						if (pointsToEnsure != null)
						{
							totalAddCount += pointsToEnsure.PointCount;
						}

						featureCount++;
					}
				}
				catch (Exception)
				{
					_msg.InfoFormat(removePointsOnly
						                ? "Error removing points in {0}"
						                : "Error adding / removing points in {0}",
					                GdbObjectUtils.ToString(feature));
					throw;
				}

				progressFeedback?.Advance("Updated geometry for {0} of {1} features",
				                          featureCount, vertexInfos.Count);
			}

			string message = GetMessage(totalAddCount, totalRemovedCount, featureCount);

			_msg.InfoFormat(message);
		}

		/// <summary>
		/// Adds / removes the specified points to / from the specified geometry within the specified area
		/// </summary>
		/// <param name="toFromGeometry"></param>
		/// <param name="pointsToAdd"></param>
		/// <param name="pointsToRemove"></param>
		/// <param name="maxSnapTolerance"></param>
		/// <returns></returns>
		public static void AddRemovePoints(IGeometry toFromGeometry,
		                                   [CanBeNull] IPointCollection pointsToAdd,
		                                   [CanBeNull] IPointCollection pointsToRemove,
		                                   [CanBeNull] double? maxSnapTolerance)
		{
			// ensure topology points first, otherwise they might not be found in the geometry again
			// (removing points can change the segment on which the topology point is found)
			if (pointsToAdd != null && pointsToAdd.PointCount > 0)
			{
				var polycurve = toFromGeometry as IPolycurve;
				var multiPatch = toFromGeometry as IMultiPatch;

				if (polycurve != null)
				{
					CrackPolycurve(polycurve, pointsToAdd, maxSnapTolerance);
				}
				else if (multiPatch != null)
				{
					CrackMultipatch(multiPatch, pointsToAdd, maxSnapTolerance);
				}
				else
				{
					throw new InvalidOperationException("Unsupported geometry type");
				}
			}

			if (pointsToRemove != null)
			{
				RemoveCutPointsService.RemovePoints(toFromGeometry,
				                                    GeometryUtils.GetPoints(pointsToRemove));
			}
		}

		private static void CrackPolycurve(IPolycurve polycurve,
		                                   IPointCollection pointsToAdd,
		                                   double? maxSnapTolerance)
		{
			double searchTolerance = maxSnapTolerance ??
			                         GeometryUtils.GetXyTolerance(polycurve);

			var remainingPointsToAdd =
				(IPointCollection) ((IClone) pointsToAdd).Clone();

			for (var i = 0; i < remainingPointsToAdd.PointCount; i++)
			{
				IPoint point = remainingPointsToAdd.Point[i];

				List<int> existingVertices =
					GeometryUtils.FindVertexIndices((IPointCollection) polycurve, point,
					                                searchTolerance).ToList();

				// start at the end ensure correctness of the indexes
				existingVertices.Sort();
				existingVertices.Reverse();
				foreach (int existingVertex in existingVertices)
				{
					UpdateExistingVertex(existingVertex, (IPointCollection) polycurve, point);
				}

				if (existingVertices.Count > 0)
				{
					// If an existing vertex was moved, remove the point from the list of points to add, otherwise a vertex 
					// can be added at the same location as an existing vertex in GeometryUtils.CrackPolycurve() below. In the 
					// case of elliptic arcs, this creates an additional, closed segment (instead of a 0-length segment). That 
					// new segment even covers the existing segment!
					remainingPointsToAdd.RemovePoints(i--, 1);
				}
			}

			// crack all remaining points that are in the middle of a segment
			GeometryUtils.CrackPolycurve(polycurve, remainingPointsToAdd, false, false, null);
		}

		private static void TryInsertPointIntoMultipatch(IMultiPatch multipatch,
		                                                 IPoint point,
		                                                 double searchTolerance)
		{
			var geometryCollection = (IGeometryCollection) multipatch;

			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				var ring = geometryCollection.get_Geometry(i) as IRing;

				if (ring == null)
				{
					_msg.DebugFormat("Non-ring multipatch parts are not supported.");
					continue;
				}

				CrackMultipatchRing(ring, point, searchTolerance);
			}
		}

		private static void CrackExistingSegment(int segmentIndex,
		                                         IRing ring,
		                                         IPoint crackPoint,
		                                         double maxExistingPointZUpdate = 0)
		{
			ISegment segment =
				((ISegmentCollection) ring).Segment[segmentIndex];

			IPoint fromPoint = segment.FromPoint;
			IPoint toPoint = segment.ToPoint;

			if (GeometryUtils.AreEqualInXY(crackPoint, fromPoint) ||
			    GeometryUtils.AreEqualInXY(crackPoint, toPoint))
			{
				_msg.DebugFormat(
					"Crack point at {0} | {1} not inserted into segment because equal to from- or to-point",
					crackPoint.X, crackPoint.Y);
				return;
			}

			IPoint pointToInsert = GeometryFactory.Clone(crackPoint);

			double originalZ = GeometryUtils.GetZValueFromSegment(crackPoint, segment);

			if (maxExistingPointZUpdate > 0)
			{
				double newZ = crackPoint.Z;

				double dZ = Math.Abs(originalZ - newZ);

				if (dZ > maxExistingPointZUpdate)
				{
					pointToInsert.Z = originalZ;
				}
			}

			if (double.IsNaN(pointToInsert.Z))
			{
				pointToInsert.Z = originalZ;
			}

			// potentially also interpolate M-values
			if (fromPoint.ID == toPoint.ID)
			{
				pointToInsert.ID = fromPoint.ID;
			}

			GeometryUtils.InsertPoints((IPointCollection4) ring, segmentIndex + 1,
			                           pointToInsert);
		}

		private static void UpdateExistingVertex(
			int existingVertexIdx,
			[NotNull] IPointCollection inPointCollection,
			[NotNull] IPoint newPoint,
			double maxExistingPointZUpdate = double.MaxValue)
		{
			IPoint existingVertex = inPointCollection.Point[existingVertexIdx];

			double updateDistanceXy = 0;
			double updateDistanceZ = 0;

			if (! MathUtils.AreEqual(existingVertex.X, newPoint.X) ||
			    ! MathUtils.AreEqual(existingVertex.Y, newPoint.Y))
			{
				// snap in XY only
				updateDistanceXy = GeometryUtils.GetPointDistance(existingVertex, newPoint);
				existingVertex.X = newPoint.X;
				existingVertex.Y = newPoint.Y;
			}

			if (maxExistingPointZUpdate > 0)
			{
				double newZ = newPoint.Z;

				double dZ = Math.Abs(existingVertex.Z - newZ);

				if (dZ > 0 && dZ < maxExistingPointZUpdate)
				{
					existingVertex.Z = newZ;
					updateDistanceZ = dZ;
				}
			}

			if (updateDistanceXy > 0 || updateDistanceZ > 0)
			{
				inPointCollection.UpdatePoint(existingVertexIdx, existingVertex);
				_msg.DebugFormat(
					"Updated vertex index {0} of current ring by {1} (XY) and {2} (Z)",
					existingVertexIdx, updateDistanceXy, updateDistanceZ);
			}
		}

		private static IPoint GetCoplanarCrackPoint([NotNull] IPoint crackPoint,
		                                            [NotNull] IPoint existingVertex,
		                                            [CanBeNull] Plane3D thisRingPlane,
		                                            [NotNull] IList<Plane3D> relevantPlanes,
		                                            double tolerance,
		                                            double coplanarityTolerance)
		{
			List<Plane3D> involvedPlanes =
				relevantPlanes.Where(
					              p =>
						              p.GetDistanceAbs(existingVertex.X, existingVertex.Y,
						                               existingVertex.Z) <
						              tolerance)
				              .ToList();

			if (involvedPlanes.Count <= 1)
			{
				return GetFallbackCrackPointToInsert(crackPoint, existingVertex, thisRingPlane,
				                                     tolerance);
			}

			// First try to keep the XY of the crack point (at its clustered location)
			IPoint snappedCrackPoint = GeometryFactory.Clone(crackPoint);
			snappedCrackPoint.SnapToSpatialReference();

			// Increase the tolerance because of snapping. For the actual correct result the plane equations
			// would need to be re-calculated with the updated (or new) crack point.  
			coplanarityTolerance += GeometryUtils.GetZResolution(crackPoint) / 2;

			if (IsPointInAllPlanes(snappedCrackPoint, involvedPlanes, coplanarityTolerance))
			{
				return crackPoint;
			}

			// Typically if there was no vertex along a segment to crack:
			snappedCrackPoint.Z = existingVertex.Z;

			if (IsPointInAllPlanes(snappedCrackPoint, involvedPlanes, coplanarityTolerance))
			{
				return snappedCrackPoint;
			}

			return GetPlanesIntersection(crackPoint, existingVertex, thisRingPlane, involvedPlanes,
			                             tolerance, coplanarityTolerance);
		}

		private static bool IsPointInAllPlanes([NotNull] IPoint point,
		                                       [NotNull] IList<Plane3D> planes,
		                                       double tolerance)
		{
			return planes.All(p => p.GetDistanceAbs(point.X, point.Y, point.Z) <= tolerance);
		}

		private static IPoint GetPlanesIntersection([NotNull] IPoint crackPoint,
		                                            [NotNull] IPoint existingVertex,
		                                            [CanBeNull] Plane3D thisRingPlane,
		                                            [NotNull] IList<Plane3D> allInvolvedPlanes,
		                                            double tolerance,
		                                            double coplanarityTolerance)
		{
			IPoint result = GeometryFactory.Clone(existingVertex);

			// Calculate the intersection straight between the first 2:
			Plane3D plane1 = allInvolvedPlanes[0];
			Plane3D plane2 = allInvolvedPlanes[1];

			Pnt3D p0;
			Vector direction = GeomTopoOpUtils.IntersectPlanes(plane1, plane2, out p0);

			if (direction == null)
			{
				return GetFallbackCrackPointToInsert(crackPoint, existingVertex, thisRingPlane,
				                                     tolerance);
			}

			var vertexPnt = new Pnt3D(existingVertex.X, existingVertex.Y, existingVertex.Z);
			IBox box = new Box(vertexPnt, vertexPnt);
			box = GeomUtils.CreateBox(box, Math.Max(100, tolerance));
			Line3D planePlaneIntersection =
				Assert.NotNull(Line3D.ConstructInBox(Assert.NotNull(p0), direction, box));

			if (allInvolvedPlanes.Count == 2)
			{
				// snap to line
				Pnt3D pointOnLine;
				if (planePlaneIntersection.GetDistancePerpendicular(
					    vertexPnt, false, out double _, out pointOnLine) > tolerance)
				{
					return GetFallbackCrackPointToInsert(crackPoint, existingVertex, thisRingPlane,
					                                     tolerance);
				}

				result.PutCoords(pointOnLine.X, pointOnLine.Y);
				result.Z = pointOnLine.Z;

				return result;
			}

			// Check all remaining planes
			Pnt3D intersection = null;
			var planeIdx = 2;
			double? factor;
			while (planeIdx < allInvolvedPlanes.Count &&
			       (factor = allInvolvedPlanes[planeIdx++].GetIntersectionFactor(
				        planePlaneIntersection.StartPoint,
				        planePlaneIntersection.EndPoint)) != null)
			{
				Pnt3D thisPlanePoint = planePlaneIntersection.GetPointAlong(factor.Value, true);

				if (intersection == null ||
				    thisPlanePoint.Equals(intersection, coplanarityTolerance))
				{
					intersection = thisPlanePoint;
				}
				else
				{
					return GetFallbackCrackPointToInsert(crackPoint, existingVertex, thisRingPlane,
					                                     tolerance);
				}
			}

			if (intersection == null)
			{
				return GetFallbackCrackPointToInsert(crackPoint, existingVertex, thisRingPlane,
				                                     tolerance);
			}

			if (intersection.GetDistance(vertexPnt) > tolerance)
			{
				return GetFallbackCrackPointToInsert(crackPoint, existingVertex, thisRingPlane,
				                                     tolerance);
			}

			result.PutCoords(intersection.X, intersection.Y);
			result.Z = intersection.Z;

			return result;
		}

		private static IPoint GetFallbackCrackPointToInsert(
			[NotNull] IPoint crackPoint,
			[NotNull] IPoint vertex,
			[CanBeNull] Plane3D thisRingPlane,
			double tolerance)
		{
			double newZ;

			if (thisRingPlane != null && thisRingPlane.IsDefined &&
			    ! thisRingPlane.IsVertical(GeometryUtils.GetZResolution(vertex)))
			{
				newZ = thisRingPlane.GetZ(vertex.X, vertex.Y);
			}
			else
			{
				newZ = crackPoint.Z;
			}

			double dZ = Math.Abs(vertex.Z - newZ);

			// Get XY from crack point, Z from plane or crack point, if within tolerance:
			IPoint result = GeometryFactory.Clone(crackPoint);

			result.Z = dZ > 0 && dZ < tolerance ? newZ : vertex.Z;

			return result;
		}

		private static IEnumerable<IPolyline> GetSplitPolycurves(
			[NotNull] IFeature lineFeature,
			[NotNull] IPointCollection splitPoints,
			double? maxSplitPointDistanceToLine)
		{
			Assert.ArgumentNotNull(lineFeature, nameof(lineFeature));
			Assert.ArgumentNotNull(splitPoints, nameof(splitPoints));

			var lineToSplit = (IPolyline) lineFeature.ShapeCopy;

			if (maxSplitPointDistanceToLine > 0)
			{
				// For proper snapping of the existing vertices to the split point, otherwise short segments could result
				CrackPolycurve(lineToSplit, splitPoints, maxSplitPointDistanceToLine);
			}

			bool projectPointsOntoPathToSplit = maxSplitPointDistanceToLine == null;
			const bool createParts = true;

			GeometryUtils.CrackPolycurve(lineToSplit, splitPoints, projectPointsOntoPathToSplit,
			                             createParts, maxSplitPointDistanceToLine);

			IList<IGeometry> splitGeometries = GeometryUtils.Explode(lineToSplit);

			int originalPartCount = ((IGeometryCollection) lineToSplit).GeometryCount;

			_msg.DebugFormat("Split original line with {0} parts into {1} new lines",
			                 originalPartCount, splitGeometries.Count);

			return splitGeometries.Cast<IPolyline>();

			// TODO: Use same multipart explosion policy as CutGeometryUtils.TryCut(Polyline) - or make configurable
		}

		/// <summary>
		/// Performs basic clustering on the specified points using the provided tolerance as minimum
		/// distance. M values and PointIDs are disregarded and lost in the output!
		/// </summary>
		/// <param name="points"></param>
		/// <param name="tolerance"></param>
		private static IPointCollection ClusterPoints([NotNull] IPointCollection points,
		                                              double? tolerance)
		{
			// NOTE: Simplify on the multipoint cannot be used (directly) because it uses the resolution instead
			// of the tolerance:
			// "For multipoints, Simplify snaps all x-, y-, z-, and m-coordinates to the grid of the associated 
			// spatial reference, and removes identical points. A point is identical to another point when the 
			// two have identical x,y coordinates (after snapping) and when attributes for which it is aware are 
			// identical to the attributes for which the other point is aware."

			var pointGeometry = (IGeometry) points;

			if (points.PointCount == 0)
			{
				return (IPointCollection) GeometryFactory.CreateEmptyMultipoint(pointGeometry);
			}

			if (! tolerance.HasValue)
			{
				tolerance = GeometryUtils.GetXyTolerance(pointGeometry);
			}

			// First cluster the XY coordinates for all Z values, then cluster the Z values:
			WKSPointZ[] coords = GeometryUtils.GetWKSPointZs(pointGeometry);

			IList<KeyValuePair<WKSPointZ, List<WKSPointZ>>> clusters2D =
				WKSPointZUtils.GroupPoints(coords, tolerance.Value, double.NaN);

			var i = 0;

			if (! GeometryUtils.IsZAware(pointGeometry))
			{
				return ToMultipoint(clusters2D, pointGeometry.SpatialReference);
			}

			foreach (KeyValuePair<WKSPointZ, List<WKSPointZ>> cluster2D in clusters2D)
			{
				WKSPointZ xyCenter = cluster2D.Key;

				foreach (WKSPointZ wksPointZ in cluster2D.Value)
				{
					coords[i++] = WKSPointZUtils.CreatePoint(xyCenter.X, xyCenter.Y, wksPointZ.Z);
				}
			}

			IList<KeyValuePair<WKSPointZ, List<WKSPointZ>>> clusters3D =
				WKSPointZUtils.GroupPoints(coords, tolerance.Value, tolerance.Value);

			return ToMultipoint(clusters3D, pointGeometry.SpatialReference);
		}

		private static IPointCollection ToMultipoint(
			[NotNull] ICollection<KeyValuePair<WKSPointZ, List<WKSPointZ>>> clusteredPoints,
			[NotNull] ISpatialReference sr)
		{
			var clusteredWksPoints = new WKSPointZ[clusteredPoints.Count];

			var i = 0;
			foreach (KeyValuePair<WKSPointZ, List<WKSPointZ>> keyValuePair in clusteredPoints)
			{
				WKSPointZ centerPoint = keyValuePair.Key;

				clusteredWksPoints[i++] = centerPoint;
			}

			return (IPointCollection) GeometryFactory.CreateMultipoint(clusteredWksPoints, sr);
		}

		private static IGeometry EnsureInputGeometryProjection(FeatureVertexInfo toVertexInfo,
		                                                       ISpatialReference
			                                                       processingSpatialReference)
		{
			IGeometry inputGeometry;
			if (processingSpatialReference != null)
			{
				inputGeometry = toVertexInfo.Feature.ShapeCopy;
				GeometryUtils.EnsureSpatialReference(inputGeometry, toVertexInfo.Feature);
			}
			else
			{
				inputGeometry = toVertexInfo.Feature.Shape;
			}

			return inputGeometry;
		}

		private static IGeometry ExtractVertices(IGeometry originalGeometry)
		{
			var pointCollection = originalGeometry as IPointCollection;

			return pointCollection != null
				       ? GeometryFactory.CreateMultipoint(pointCollection)
				       : originalGeometry;
		}

		public static IGeometry ExtractBoundariesForMultipatches(IGeometry targetGeometry)
		{
			var multipatch = targetGeometry as IMultiPatch;

			if (multipatch == null)
			{
				return targetGeometry;
			}

			return CreatePolylineSalad(targetGeometry);
		}

		#endregion
	}
}

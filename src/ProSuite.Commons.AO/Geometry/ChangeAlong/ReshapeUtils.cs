using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using Path = System.IO.Path;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public static class ReshapeUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Calculate Curves to Reshape with

		/// <summary>
		/// Calculates the 2D difference between two polylines.
		/// </summary>
		/// <param name="onPolyline">The polyline on which the result segments are located</param>
		/// <param name="differentFrom">The polyline from which the result segments must be different</param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline GetDifferencePolyline([NotNull] IPolyline onPolyline,
		                                              [NotNull] IPolyline differentFrom)
		{
			IPolyline projectedSource;
			if (GeometryUtils.EnsureSpatialReference(onPolyline,
			                                         differentFrom.SpatialReference,
			                                         out projectedSource))
			{
				// e.g. if the map SR is different from data SR (just after store the feature has the data SR)
				_msg.DebugFormat(
					"Target geometry had to be projected to conform to source geometry");
			}

			var topoOp = (ITopologicalOperator) projectedSource;

			Stopwatch watch = _msg.DebugStartTiming("Calculating 2D difference...");

			IPolyline difference;
			if (GeometryUtils.Disjoint((IGeometry) topoOp, differentFrom))
			{
				// difference is equal to source
				difference = GeometryFactory.Clone(projectedSource);
			}
			else
			{
				difference = (IPolyline) topoOp.Difference(differentFrom);
			}

			// Work-around for issue reported in Repro_IRelationalOperatorDisjointIncorrectAfterOtherDisjoint:
			GeometryUtils.DisallowIndexing(onPolyline);
			try
			{
				onPolyline.SnapToSpatialReference();
			}
			catch (Exception e)
			{
				_msg.Debug("Error in work-around will be ignored", e);
			}
			// End work-around

			_msg.DebugStopTiming(watch, "Calculated 2D difference");

			// TODO: test if/why/when there are different spatial refs
			bool projected = GeometryUtils.EnsureSpatialReference(
				difference, differentFrom.SpatialReference);

			if (projected)
			{
				_msg.DebugFormat(
					"The difference geometry had to be projected to conform to source geometry.");
			}

			if (projectedSource != onPolyline)
			{
				Marshal.ReleaseComObject(projectedSource);
			}

			return difference;
		}

		public static IPolyline GetDifferencePolylineXyz(
			[NotNull] IPolyline onPolyline,
			[NotNull] IPolyline differentFrom,
			double xyTolerance,
			double zTolerance)
		{
			IPolyline difference;
			IPolyline zOnlyDifference;

			// Ignoring M-values. They are never taken from the target.
			if (IntersectionUtils.UseCustomIntersect &&
			    ! GeometryUtils.HasNonLinearSegments(onPolyline) &&
			    ! GeometryUtils.HasNonLinearSegments(differentFrom))
			{
				zOnlyDifference = GeometryFactory.CreateEmptyPolyline(onPolyline);
				difference = IntersectionUtils.GetDifferenceLinesXY(
					onPolyline, differentFrom, xyTolerance,
					zOnlyDifference, zTolerance);
			}
			else
			{
				difference =
					GetDifferencePolyline(onPolyline, differentFrom);
				zOnlyDifference =
					GetZOnlyDifferenceLegacy(onPolyline, differentFrom,
					                         zTolerance);
			}

			if (! zOnlyDifference.IsEmpty)
			{
				difference = (IPolyline) GeometryUtils.Union(difference, zOnlyDifference);
			}

			// adjacent lines parts get ordered and create one single reshapable part
			GeometryUtils.Simplify(difference, true, true);

			return difference;
		}

		/// <summary>
		/// Calculates the Z-difference between two polylines at linear intersection, i.e. where
		/// they are coincident in XY.
		/// </summary>
		/// <param name="polyline"></param>
		/// <param name="differentFrom"></param>
		/// <param name="zTolerance"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IPolyline GetZOnlyDifference([NotNull] IPolyline polyline,
		                                           [NotNull] IPolyline differentFrom,
		                                           double zTolerance = double.NaN)
		{
			if (double.IsNaN(zTolerance))
			{
				zTolerance = GeometryUtils.GetZTolerance(polyline);
			}

			if (! GeometryUtils.IsZAware(polyline))
			{
				return null;
			}

			if (! GeometryUtils.IsZAware(differentFrom))
			{
				return null;
			}

			if (GeometryUtils.Disjoint(polyline, differentFrom))
			{
				return null;
			}

			IPolyline result;

			using (_msg.IncrementIndentation())
			{
				Stopwatch zWatch = _msg.DebugStartTiming("Calculating Z-difference...");

				if (! GeometryUtils.HasNonLinearSegments(polyline) &&
				    ! GeometryUtils.HasNonLinearSegments(differentFrom))
				{
					result = IntersectionUtils.GetZOnlyDifferenceLines(
						polyline, differentFrom, zTolerance);
				}
				else
				{
					_msg.Debug(
						"Using legacy z-only difference calculation due to non-linear segments or M-awareness.");

					result = GetZOnlyDifferenceLegacy(polyline, differentFrom,
					                                  zTolerance);
				}

				_msg.DebugStopTiming(zWatch,
				                     "Calculated Z-only difference. Found {0} differences.",
				                     ((IGeometryCollection) result).GeometryCount);
			}

			return result.IsEmpty
				       ? null
				       : result;
		}

		[NotNull]
		public static IPolyline GetZOnlyDifferenceLegacy(
			[NotNull] IPolyline polyline,
			[NotNull] IPolyline differentFrom,
			double zTolerance)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			if (! GeometryUtils.IsZAware(polyline) ||
			    ! GeometryUtils.IsZAware(differentFrom))
			{
				return GeometryFactory.CreateEmptyPolyline(polyline);
			}

			IPolyline intersections1, intersections2;
			GetSymmetric2DIntersections(polyline, differentFrom,
			                            out intersections1, out intersections2);

			if (intersections1 == null || intersections2 == null)
			{
				return GeometryFactory.CreateEmptyPolyline(polyline);
			}

			IPolyline result = GetZDifferenceBetweenCongruentPolylines(
				intersections1, intersections2,
				zTolerance);

			_msg.DebugStopTiming(watch, "Calculated z-only difference ({0} map units)",
			                     result.Length);

			return result;
		}

		[NotNull]
		public static IPolyline GetZDifferenceBetweenCongruentPolylines(
			[NotNull] IPolyline onPolyline,
			[NotNull] IPolyline differentFrom)
		{
			double zTolerance = GeometryUtils.GetZTolerance(onPolyline);
			return GetZDifferenceBetweenCongruentPolylines(onPolyline, differentFrom,
			                                               zTolerance);
		}

		[NotNull]
		public static IPolyline GetZDifferenceBetweenCongruentPolylines(
			[NotNull] IPolyline onPolyline,
			[NotNull] IPolyline differentFrom,
			double zTolerance)
		{
			IDictionary<WKSPointZ, VertexIndex> differentOnPolyline;

			var geometryComparison = new GeometryComparison(
				onPolyline, differentFrom,
				GeometryUtils.GetXyTolerance(onPolyline),
				zTolerance);

			// Get the segments (and out points) in the source that do not exist (measured in 3D) in the target
			IPolyline sourceDifferences =
				geometryComparison.GetBaseSegmentZDifferences(out differentOnPolyline);

			ISegmentCollection additionalDifferences;
			ISegmentCollection removeDifferences;

			// Check points that exist in the target but not in the source
			CalculateAdditionalDifferences(onPolyline, differentFrom, differentOnPolyline,
			                               out additionalDifferences,
			                               out removeDifferences);

			// remove the not-different (sub-)segments from the initially added differences
			GeometryUtils.Simplify(sourceDifferences, true, true);
			GeometryUtils.Simplify((IGeometry) removeDifferences, true, true);

			IGeometry reducedDifferences =
				IntersectionUtils.Difference(sourceDifferences,
				                             (IGeometry) removeDifferences);

			// take the additional differences from the original source geometry
			IGeometry highLevelOnPath = GeometryUtils.GetHighLevelGeometry(onPolyline);

			IGeometry additionalDifferencesOnSource =
				IntersectionUtils.Intersect(
					highLevelOnPath, (IGeometry) additionalDifferences,
					esriGeometryDimension.esriGeometry1Dimension);

			((ISegmentCollection) reducedDifferences).AddSegmentCollection(
				(ISegmentCollection) additionalDifferencesOnSource);

			GeometryUtils.Simplify(reducedDifferences, true, true);

			return (IPolyline) reducedDifferences;
		}

		/// <summary>
		/// Creates the intersections between the two polycurves 'in each direction'
		/// Intersection1 has the z-values of the source (onPolycurve), intersection2 has the
		/// z-values of the target (differentFrom). Except in some situations with minimal
		/// tolerance the two geometries should have the same number of parts and should be equal in xy.
		/// </summary>
		/// <param name="onPolycurve"></param>
		/// <param name="differentFrom"></param>
		/// <param name="intersections1"></param>
		/// <param name="intersections2"></param>
		private static void GetSymmetric2DIntersections(
			[NotNull] IPolycurve onPolycurve, [NotNull] IPolycurve differentFrom,
			[CanBeNull] out IPolyline intersections1,
			[CanBeNull] out IPolyline intersections2)
		{
			Stopwatch watch =
				_msg.DebugStartTiming("GetZOnlyDifference: Calculating intersections...");

			// NOTE: Intersect here is about as expensive as difference. It is not much faster
			//		 to use difference between target and already calculated difference.
			const bool assumeIntersecting = true;
			const bool allowRandomStartForClosedIntersections = true;
			intersections1 = IntersectionUtils.GetIntersectionLines(
				onPolycurve, differentFrom, assumeIntersecting,
				allowRandomStartForClosedIntersections);

			if (intersections1.IsEmpty)
			{
				intersections1 = null;
				intersections2 = null;
				return;
			}

			_msg.DebugStopTiming(watch, "Calculated intersection.");

			// Get the Z-values from the target
			// In the case of non-linear segments, using intersections1 is results in a geometry that is not
			// congurent in 2D with intersections1. Typically it has even a different length / part count
			// regarding part count and geometry.
			if (GeometryUtils.HasNonLinearSegments(intersections1))
			{
				// make sure the result is symmetric at least in 2D (use onPolycurve)
				intersections2 = IntersectionUtils.GetIntersectionLines(
					differentFrom, onPolycurve, assumeIntersecting,
					allowRandomStartForClosedIntersections);
			}
			else
			{
				// use the fast option (use previous intersection)
				intersections2 = IntersectionUtils.GetIntersectionLines(
					differentFrom, intersections1, assumeIntersecting,
					allowRandomStartForClosedIntersections);
			}

			if (((IGeometryCollection) intersections1).GeometryCount !=
			    ((IGeometryCollection) intersections2).GeometryCount)
			{
				// this happens as the output of intersect is not always simple (even though IsSimple returns true)
				// and sometimes two adjacent parts are not connected (only in one geometry)
				// NOTE: allow path split at intersections (i.e. true, true) is necessary in some situations
				//		 when a target vertex is very close (but seemingly more than the tolerance) to the intersection
				//		 between source and target.
				GeometryUtils.Simplify(intersections1, true, true);
				GeometryUtils.Simplify(intersections2, true, true);
			}
		}

		private static void CalculateAdditionalDifferences(
			IGeometry onSegments, IGeometry differentFromSegments,
			IDictionary<WKSPointZ, VertexIndex> differentOnPathPoints,
			out ISegmentCollection additionalDifferences,
			out ISegmentCollection removeDifferences)
		{
			var targetSegments = (ISegmentCollection) differentFromSegments;

			ISpatialReference spatialRef = onSegments.SpatialReference;
			const bool zAware = true;
			bool mAware = GeometryUtils.IsMAware(onSegments);

			removeDifferences =
				(ISegmentCollection) GeometryFactory.CreatePolyline(
					spatialRef, zAware, mAware);
			additionalDifferences =
				(ISegmentCollection) GeometryFactory.CreatePolyline(
					spatialRef, zAware, mAware);

			var targetComparison = new GeometryComparison(
				differentFromSegments, onSegments,
				GeometryUtils.GetXyTolerance(onSegments),
				GeometryUtils.GetZTolerance(onSegments));

			IDictionary<WKSPointZ, VertexIndex> differentOnTarget =
				targetComparison.GetDifference(true);

			Dictionary<WKSPointZ, VertexIndex> sourceDifferences2D =
				GetSourceDifferences2D(onSegments.SpatialReference,
				                       differentOnPathPoints);

			foreach (
				KeyValuePair<WKSPointZ, VertexIndex> differentPointOnTarget in
				differentOnTarget)
			{
				// point also in source, but handled by the previous source differences check -> skip
				if (sourceDifferences2D.ContainsKey(differentPointOnTarget.Key))
				{
					continue;
				}

				// No vertex in source but could still be ok in 3D -> 
				if (targetComparison.CompareGeometryContainsPoint3D(
					differentPointOnTarget.Key))
				{
					continue;
				}

				// point exists in target but not in source, target point is different from source source
				VertexIndex vertexIndex = differentPointOnTarget.Value;

				int previousSegmentIdx = vertexIndex.VertexIndexInPart - 1;
				int nextSegmentIdx = vertexIndex.VertexIndexInPart;

				if (previousSegmentIdx >= 0)
				{
					int globalPreviousSegmentIdx =
						GeometryUtils.GetGlobalSegmentIndex((IGeometry) targetSegments,
						                                    vertexIndex.PartIndex,
						                                    previousSegmentIdx);

					HandleTargetSegment(globalPreviousSegmentIdx, targetSegments,
					                    targetComparison,
					                    additionalDifferences, removeDifferences);
				}

				if (! vertexIndex.IsLastInPart)
				{
					int globalNextSegmentIdx =
						GeometryUtils.GetGlobalSegmentIndex((IGeometry) targetSegments,
						                                    vertexIndex.PartIndex,
						                                    nextSegmentIdx);

					HandleTargetSegment(globalNextSegmentIdx, targetSegments,
					                    targetComparison,
					                    additionalDifferences, removeDifferences);
				}
			}
		}

		private static Dictionary<WKSPointZ, VertexIndex> GetSourceDifferences2D(
			ISpatialReference spatialReference,
			IDictionary<WKSPointZ, VertexIndex> differentOnPathPoints)
		{
			var sourceDifferences2D =
				new Dictionary<WKSPointZ, VertexIndex>(
					differentOnPathPoints.Count,
					new WKSPointZComparer(spatialReference, false));

			foreach (KeyValuePair<WKSPointZ, VertexIndex> keyValuePair in
				differentOnPathPoints
			)
			{
				// beware of vertical segments in 10.0
				if (! sourceDifferences2D.ContainsKey(keyValuePair.Key))
				{
					sourceDifferences2D.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}

			return sourceDifferences2D;
		}

		private static void HandleTargetSegment(int? segmentIndex,
		                                        ISegmentCollection targetSegments,
		                                        GeometryComparison targetComparison,
		                                        ISegmentCollection additionalDifferences,
		                                        ISegmentCollection notDifferent)
		{
			object missing = Type.Missing;

			if (segmentIndex != null)
			{
				ISegment segment = targetSegments.Segment[(int) segmentIndex];

				if (
					targetComparison.CompareGeometryContainsPoint3D(
						GetWksPointZ(segment.FromPoint)) &&
					targetComparison.CompareGeometryContainsPoint3D(
						GetWksPointZ(segment.ToPoint)))
				{
					notDifferent.AddSegment(segment, ref missing, ref missing);
				}
				else
				{
					additionalDifferences.AddSegment(segment, ref missing, ref missing);
				}
			}
		}

		private static WKSPointZ GetWksPointZ(IPoint point)
		{
			var wksPointZ = new WKSPointZ {X = point.X, Y = point.Y, Z = point.Z};

			return wksPointZ;
		}

		#endregion

		#region Target Point Insertion

		/// <summary>
		/// Inserts the intersection points between the source and the target features
		/// into the target geometry. if the resultingTargets contains the target already,
		/// the respective geometry will be updated, otherwise it will be added.
		/// </summary>
		/// <param name="targetFeatures"></param>
		/// <param name="sourceGeometry"></param>
		/// <param name="resultingTargets">The result dictionary containing the resulting 
		/// target geometry/feature pairs. It can be re-used for serveral calls with verious 
		/// source geometries.</param>
		public static void InsertIntersectingVerticesInTargets(
			[NotNull] IEnumerable<IFeature> targetFeatures,
			[NotNull] IGeometry sourceGeometry,
			[NotNull] IDictionary<IFeature, IGeometry> resultingTargets)
		{
			Assert.ArgumentNotNull(targetFeatures, nameof(targetFeatures));
			Assert.ArgumentNotNull(sourceGeometry, nameof(sourceGeometry));

			// Actual snapped points are provided: Points inside the polygon but not on the boundary should be excluded
			bool useTargetPolygonsBoundary =
				sourceGeometry.GeometryType == esriGeometryType.esriGeometryMultipoint ||
				sourceGeometry.GeometryType == esriGeometryType.esriGeometryPoint;

			IEnumerable<KeyValuePair<IFeature, IPointCollection>> crackPointsByTargets =
				GetIntersectionPoints(targetFeatures, sourceGeometry,
				                      useTargetPolygonsBoundary);

			foreach (
				KeyValuePair<IFeature, IPointCollection> crackPointsByTarget in
				crackPointsByTargets)
			{
				IFeature targetFeature = crackPointsByTarget.Key;

				if (! DatasetUtils.IsBeingEdited(targetFeature.Class))
				{
					continue;
				}

				_msg.DebugFormat("Ensuring {0} intersection points in {1}",
				                 crackPointsByTarget.Value.PointCount,
				                 GdbObjectUtils.ToString(targetFeature));

				IGeometry targetGeometryToUpdate =
					resultingTargets.ContainsKey(targetFeature)
						? resultingTargets[targetFeature]
						: targetFeature.ShapeCopy;

				const bool allowZDifference = true;

				if (EnsurePointsExistInTarget(
					targetGeometryToUpdate,
					GeometryUtils.GetPoints(crackPointsByTarget.Value),
					GeometryUtils.GetXyTolerance(targetGeometryToUpdate),
					allowZDifference))
				{
					if (resultingTargets.ContainsKey(targetFeature))
					{
						resultingTargets[targetFeature] = targetGeometryToUpdate;
					}
					else
					{
						resultingTargets.Add(targetFeature, targetGeometryToUpdate);
					}
				}
			}
		}

		public static bool EnsurePointsExistInTarget(
			[NotNull] IGeometry targetGeometry,
			[NotNull] IEnumerable<IPoint> points,
			double xyTolerance,
			bool allowZDifference = false)
		{
			var pointEnsured = false;

			double simplificationDistance = xyTolerance * 2 * Math.Sqrt(2);

			if (targetGeometry.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				IMultipoint crackPoints = GeometryFactory.CreateMultipoint(points);

				if (GeometryUtils.Intersects(crackPoints, targetGeometry))
				{
					CrackUtils.CrackMultipatch((IMultiPatch) targetGeometry,
					                           (IPointCollection) crackPoints,
					                           simplificationDistance);
					pointEnsured = true;
				}
			}
			else
			{
				bool originalKnownSimple =
					((ITopologicalOperator) targetGeometry).IsKnownSimple;

				foreach (IPoint potentialTargetPoint in points)
				{
					GeometryUtils.EnsureSpatialReference(potentialTargetPoint,
					                                     targetGeometry.SpatialReference);

					// If indexing is not suppressed here, we're affected by the bug reproduced by 
					// Repro_IRelationalOperatorDisjointIncorrectAfterOtherDisjoint which results in TOP-4726
					const bool suppressIndexing = true;
					if (GeometryUtils.Disjoint(potentialTargetPoint, targetGeometry,
					                           suppressIndexing))
					{
						continue;
					}

					const bool pointIsSegmentFromPoint = false;
					const bool allowNoMatch = true;
					int partIndex;
					int? segmentIdx = SegmentReplacementUtils.GetSegmentIndex(
						targetGeometry, potentialTargetPoint, xyTolerance, out partIndex,
						pointIsSegmentFromPoint,
						allowNoMatch);

					if (segmentIdx == null)
					{
						continue;
					}

					SegmentReplacementUtils.EnsureVertexExists(
						potentialTargetPoint, targetGeometry,
						segmentIdx.Value, partIndex, allowZDifference,
						simplificationDistance);

					// make sure that new vertices on target geometries
					// get interpolated z values (if they are z aware and the inserted point had NaN Z) 
					var zValues = targetGeometry as IZ;
					if (zValues != null && GeometryUtils.IsZAware(targetGeometry))
					{
						zValues.CalculateNonSimpleZs();
					}

					pointEnsured = true;
				}

				if (pointEnsured && originalKnownSimple)
				{
					// By maintaining the simplification distance we can avoid a Simplify(), however
					// subsequent IPolygon.ExteriorRingCount calls would fail if isKnownSimple is false:
					var topoOp = (ITopologicalOperator2) targetGeometry;
					if (! topoOp.IsKnownSimple)
					{
						topoOp.IsKnownSimple_2 = true;
					}
				}
			}

			return pointEnsured;
		}

		private static IEnumerable<KeyValuePair<IFeature, IPointCollection>>
			GetIntersectionPoints([NotNull] IEnumerable<IFeature> features,
			                      [NotNull] IGeometry geometry,
			                      bool useTargetPolygonsBoundary)
		{
			IDictionary<IFeature, IPointCollection> result =
				new Dictionary<IFeature, IPointCollection>();

			foreach (IFeature targetFeature in features)
			{
				IGeometry targetShape = targetFeature.Shape;

				// NOTE: Intersection points with multipatches are not found
				if (targetShape.GeometryType == esriGeometryType.esriGeometryMultiPatch ||
				    (useTargetPolygonsBoundary &&
				     targetShape.GeometryType == esriGeometryType.esriGeometryPolygon))
				{
					IGeometry boundary = GeometryUtils.GetBoundary(targetShape);
					Marshal.ReleaseComObject(targetShape);
					targetShape = boundary;
				}

				if (! GeometryUtils.Disjoint(targetShape, geometry))
				{
					IPointCollection intersectionPoints =
						SegmentReplacementUtils.GetIntersectionPoints(
							geometry, targetShape,
							IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

					if (! ((IGeometry) intersectionPoints).IsEmpty)
					{
						result.Add(targetFeature, intersectionPoints);
					}
				}

				Marshal.ReleaseComObject(targetShape);
			}

			return result;
		}

		#endregion

		#region Reshape

		public static bool ReshapeGeometry([NotNull] IGeometry geometryToReshape,
		                                   [NotNull] IPath reshapePath,
		                                   bool tryReshapeRingNonDefaultSide,
		                                   [CanBeNull] NotificationCollection notifications)
		{
			ReshapeInfo reshapeInfo;

			bool reshaped = ReshapeGeometry(geometryToReshape, reshapePath,
			                                tryReshapeRingNonDefaultSide, notifications,
			                                out reshapeInfo);

			if (reshapeInfo != null)
			{
				reshapeInfo.Dispose();
			}

			return reshaped;
		}

		/// <summary>
		/// Reshapes the geometry to reshape with the reshape path. Difference to standard reshape:
		/// - Avoid extra phantom points in reshaped line
		/// - Rings: does not flip the geometry to the wrong side if reshape has at least one reshape-to-the-outside of the ring
		/// - Control the reshape side to use if the reshape line only cuts the ring in two pieces.
		/// - Rings that are within the area added / removed by the reshape are removed rather than flipped.
		/// - Combined reshape of outer and inner ring possible (connect the island to the main land), if the reshape line starts at the outside of the outer ring
		/// - Connect an outside-reshape back to the reshaped ring to construct an island
		/// - Connect several rings into one ring by reshaping a link
		/// - Many special cases handled as good as possible
		/// </summary>
		/// <param name="geometryToReshape">Polygon or Polyline geometry</param>
		/// <param name="reshapePath">Path to be used to reshape the geometryToReshape</param>
		/// <param name="tryReshapeRingNonDefaultSide">For polygons, overrides the default behaviour 
		/// regarding the choice of the left/right ring to be used, if possible.
		/// The default side is the larger part if the reshape path is within the polygon
		/// and for reshape paths that reshape both on the outside and the inside it is the result
		/// that has only 1 ring.</param>
		/// <param name="notifications"></param>
		/// <param name="reshapeInfo">The reshape info for the single reshaped part, or, if several parts
		/// were reshaped, the 'master reshape info' containging only the most basic information and notifications. 
		/// If the reshape infos for each part are needed, use <see cref="ReshapeAllGeometryParts"/> which
		/// returns the reshape infos for all performed reshapes.</param>
		/// <returns></returns>
		public static bool ReshapeGeometry([NotNull] IGeometry geometryToReshape,
		                                   [NotNull] IPath reshapePath,
		                                   bool tryReshapeRingNonDefaultSide,
		                                   [CanBeNull] NotificationCollection notifications,
		                                   out ReshapeInfo reshapeInfo)
		{
			Assert.NotNull(geometryToReshape);
			Assert.NotNull(reshapePath);

			reshapeInfo = new ReshapeInfo(geometryToReshape, reshapePath, notifications)
			              {
				              ReshapeResultFilter =
					              new ReshapeResultFilter(tryReshapeRingNonDefaultSide)
			              };

			return ReshapeGeometry(reshapeInfo, reshapePath);
		}

		/// <summary>
		/// Reshapes all parts of the geometry that intersect the reshapePath, if possible.
		/// </summary>
		/// <param name="reshapeInfo"></param>
		/// <param name="reshapePath"></param>
		/// <returns></returns>
		public static bool ReshapeGeometry([NotNull] ReshapeInfo reshapeInfo,
		                                   [NotNull] IPath reshapePath)
		{
			IList<ReshapeInfo> singlePartReshapes;

			bool result = ReshapeAllGeometryParts(reshapeInfo, reshapePath,
			                                      out singlePartReshapes);

			// dispose all non-master reshape infos
			foreach (ReshapeInfo partReshape in singlePartReshapes)
			{
				if (partReshape != reshapeInfo)
				{
					partReshape.Dispose();
				}
			}

			return result;
		}

		public static bool ReshapeAllGeometryParts(
			[NotNull] ReshapeInfo reshapeInfo,
			[NotNull] IPath reshapePath,
			[NotNull] out IList<ReshapeInfo> singlePartReshapeInfos)
		{
			IGeometry geometryToReshape = reshapeInfo.GeometryToReshape;

			IList<int> allReshapablePartIndexes;
			int? uniquePartIndexToReshape =
				reshapeInfo.IdentifyUniquePartIndexToReshape(
					out allReshapablePartIndexes);

			if (uniquePartIndexToReshape == null)
			{
				if (allReshapablePartIndexes.Count == 0)
				{
					singlePartReshapeInfos = new List<ReshapeInfo>(0);
					return false;
				}

				bool result = ReshapeAllParts(reshapeInfo, reshapePath,
				                              allReshapablePartIndexes,
				                              out singlePartReshapeInfos);

				return result;
			}

			// single part reshape:
			singlePartReshapeInfos = new List<ReshapeInfo> {reshapeInfo};

			return ReshapeGeometryPart(geometryToReshape, reshapeInfo);
		}

		/// <summary>
		/// Collects all reshapeCurves in a polyline geometry, simplifies it and then reshapes using
		/// each path of the simplified polyline.
		/// </summary>
		/// <param name="geometryToReshape"></param>
		/// <param name="reshapeCurves"></param>
		/// <param name="resultFilter"></param>
		/// <param name="notifications"></param>
		/// <param name="reshapeInfos"></param>
		/// <returns></returns>
		public static bool ReshapeGeometry(
			[NotNull] IGeometry geometryToReshape,
			[NotNull] ICollection<IPath> reshapeCurves,
			ReshapeResultFilter resultFilter,
			[CanBeNull] NotificationCollection notifications,
			[NotNull] out ICollection<ReshapeInfo> reshapeInfos)
		{
			reshapeInfos = new List<ReshapeInfo>(reshapeCurves.Count);

			if (reshapeCurves.Count == 0)
			{
				NotificationUtils.Add(notifications, "No reshape line selected.");

				return false;
			}

			Stopwatch watch = _msg.DebugStartTiming("Trying to reshape {0} subcurves",
			                                        reshapeCurves.Count);

			var reshapedPathCount = 0;

			double originalLength = GeometryUtils.GetLength(geometryToReshape);
			double reshapePathLength = 0;

			foreach (IPath path in reshapeCurves)
			{
				var reshapeInfo = new ReshapeInfo(geometryToReshape, path, notifications)
				                  {
					                  ReshapeResultFilter = resultFilter
				                  };

				if (ReshapeGeometry(reshapeInfo, path))
				{
					reshapedPathCount++;
					reshapePathLength += path.Length;

					reshapeInfos.Add(reshapeInfo);
				}

				// TODO: Marshal.Release path? or reshapePathCollection? -> path might be referenced by new geometry?
			}

			if (reshapedPathCount > 0)
			{
				string lengthText =
					GetLengthDifferenceText(geometryToReshape, originalLength);

				_msg.DebugFormat(
					"Reshaped geometry with {0} line(s) along {1}m. It is now {2}.",
					reshapedPathCount, Math.Round(reshapePathLength, 2),
					lengthText);
			}

			bool reshaped = reshapedPathCount > 0;

			string message = notifications?.Concatenate(" ");

			_msg.DebugStopTiming(watch, "Reshaping successful: {0}. Notifications: {1}",
			                     reshaped, message);

			return reshaped;
		}

		/// <summary>
		/// Returns the selected subcurves merged into one simplified geometry that
		/// is non-M-aware and only Z-aware if all reshapeCurves are Z-simple.
		/// </summary>
		/// <param name="reshapeCurves">The reshape curves.</param>
		/// <param name="spatialReference">The result's spatial reference.</param>
		/// <param name="useMinimumTolerance"></param>
		/// <returns></returns>
		public static IGeometryCollection GetSimplifiedReshapeCurves(
			[NotNull] IEnumerable<CutSubcurve> reshapeCurves,
			[CanBeNull] ISpatialReference spatialReference = null,
			bool useMinimumTolerance = false)
		{
			Assert.ArgumentNotNull(reshapeCurves, nameof(reshapeCurves));

			List<CutSubcurve> selectedSubcurves =
				GetSubcurves(reshapeCurves, null).ToList();

			// NOTE: M-values should never be taken from a target geometry. Keep them NaN to allow the user to decide 
			//       when and how they are updated.
			const bool mAware = false;

			var zAware = true;

			foreach (IPath path in selectedSubcurves.Select(subcurve => subcurve.Path))
			{
				if (! GeometryUtils.IsZAware(path) ||
				    GeometryUtils.HasUndefinedZValues(path))
				{
					zAware = false;
				}
			}

			if (selectedSubcurves.Count == 0)
			{
				return (IGeometryCollection) GeometryFactory.CreateEmptyPolyline(
					spatialReference, zAware);
			}

			IPolyline reshapePaths = GeometryFactory.CreatePolyline(
				selectedSubcurves.Select(subcurve => subcurve.Path).ToList(),
				spatialReference, zAware, mAware);

			var reshapePathCollection = (IGeometryCollection) reshapePaths;

			// Simplify also moves the non-endpoint vertices of the reshape line which is a problem
			// in minimum-tolerance mode. -> Minimizing the tolerance in minimum-tolerance mode.
			const bool allowReorder = true;
			const bool allowPathSplitAtIntersections = true;
			if (useMinimumTolerance)
			{
				ExecuteWithMinimumTolerance(
					() =>
						GeometryUtils.Simplify(reshapePaths, allowReorder,
						                       allowPathSplitAtIntersections),
					reshapePaths);
			}
			else
			{
				GeometryUtils.Simplify(reshapePaths, allowReorder,
				                       allowPathSplitAtIntersections);
			}

			// Simplify leaves the vertices on the end points of the joined curves (Union as well) -> remove
			IEnumerable<KeyValuePair<IPoint, ISegment>> removePoints =
				GetStitchPointsToRemove(reshapePathCollection, selectedSubcurves);

			RemovePoints((IPolycurve) reshapePathCollection, removePoints);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Simplified reshape curves: {0}",
				                 GeometryUtils.ToString(reshapePaths));
			}

			return reshapePathCollection;
		}

		/// <summary>
		/// Reshapes a polygon part and removes flipped rings. The reshapeInfo's PartIndexToReshape
		/// must be already set.
		/// </summary>
		/// <param name="reshapeInfo"></param>
		/// <returns></returns>
		public static bool ReshapePolygonOrMultipatch([NotNull] ReshapeInfo reshapeInfo)
		{
			Assert.ArgumentNotNull(reshapeInfo);
			Assert.ArgumentCondition(reshapeInfo.PartIndexToReshape != null,
			                         "PartIndexToReshape is null.");

			IGeometry polygonToReshape = reshapeInfo.GeometryToReshape;

			IRing newRing = ReshapeOrReplaceRing(reshapeInfo);

			if (newRing != null)
			{
				reshapeInfo.GeometryChanged();

				// remove interior rings that are now 'on the outside' (and vice versa)
				RemoveFlippedRings(reshapeInfo, newRing);

				VerboseLogNonSimple(polygonToReshape);
			}

			if (newRing != null)
			{
				Marshal.ReleaseComObject(newRing);

				return true;
			}

			return false;
		}

		[CanBeNull]
		public static IPath GetProlongation([NotNull] ICurve sourceCurveToReshape,
		                                    [NotNull] ISegment sourceSegment,
		                                    [NotNull] IPath reshapePath,
		                                    [CanBeNull] out IPoint targetConnectPoint)
		{
			return GetProlongation(sourceCurveToReshape, sourceSegment, reshapePath,
			                       LineEnd.Both, out targetConnectPoint);
		}

		[CanBeNull]
		public static IPath GetProlongation([NotNull] ICurve sourceCurveToReshape,
		                                    [NotNull] ISegment sourceSegment,
		                                    [NotNull] IPath reshapePath,
		                                    LineEnd segmentEndToTry,
		                                    [CanBeNull] out IPoint targetConnectPoint)
		{
			IPolyline sourceSegmentPolyline =
				GeometryFactory.CreatePolyline(sourceSegment);

			var highLevelTarget =
				(ICurve) GeometryUtils.GetHighLevelGeometry(reshapePath, true);

			IPolyline extendedCurveAtFrom = null, extendedCurveAtTo = null;

			var isExtensionPerfomedAtFrom = false;
			var isExtensionPerfomedAtTo = false;

			if (segmentEndToTry == LineEnd.From || segmentEndToTry == LineEnd.Both)
			{
				isExtensionPerfomedAtFrom = CreateExtendedCurve(
					sourceSegmentPolyline, highLevelTarget,
					LineEnd.From, out extendedCurveAtFrom);
			}

			if (segmentEndToTry == LineEnd.To || segmentEndToTry == LineEnd.Both)
			{
				isExtensionPerfomedAtTo = CreateExtendedCurve(
					sourceSegmentPolyline, highLevelTarget,
					LineEnd.To, out extendedCurveAtTo);
			}

			targetConnectPoint = null;

			if (isExtensionPerfomedAtFrom && isExtensionPerfomedAtTo)
			{
				_msg.DebugFormat(
					"Connection curve calculation: The reshape curve is intersected by both extensions.");
				return null;
			}

			if (! isExtensionPerfomedAtFrom && ! isExtensionPerfomedAtTo)
			{
				_msg.DebugFormat(
					"Connection curve calculation: The reshape curve is not intersected by any extension.");
				return null;
			}

			// cut off the part that is also on the source segment otherwise the length calculation
			// is incorrect
			IGeometry outsideLine;
			if (isExtensionPerfomedAtFrom)
			{
				// NOTE: Difference can also flip the geometry!
				// NOTE: Sometimes the start point of the outside line not on the source any more (IRelationalOperator.Disjoint): Simplify the source first!
				outsideLine = ((ITopologicalOperator) extendedCurveAtFrom).Difference(
					GeometryUtils.GetHighLevelGeometry(sourceCurveToReshape));
			}
			else
			{
				outsideLine = ((ITopologicalOperator) extendedCurveAtTo).Difference(
					GeometryUtils.GetHighLevelGeometry(sourceCurveToReshape));
			}

			// TODO: Detect this situation by different reshape sides of the individual reshapes
			// -> add the original Union part that was 'released by one reshape to the target of the other.
			if (outsideLine.IsEmpty)
			{
				_msg.DebugFormat(
					"Inside-Reshape / shared boundary reshape of multiple polygons not yet properly supported.");
				return null;
			}

			Assert.False(outsideLine.IsEmpty,
			             "Unexpected geometric situation: The source prolongation is fully within the source.");

			if (((IGeometryCollection) outsideLine).GeometryCount > 1)
			{
				// TODO: test: there might be several cut-offs in the source geometry that result in a multipart difference
				//       -> take the one that intersects the target and forget the others
				_msg.DebugFormat(
					"Currently unsupported prolongation situation (multipart).");
				return null;
			}

			var result = (IPath) ((IGeometryCollection) outsideLine).Geometry[0];

			// note the result of Difference can be flipped!
			if (GeometryUtils.Intersects(result.ToPoint, highLevelTarget))
			{
				targetConnectPoint = result.ToPoint;
			}
			else if (GeometryUtils.Intersects(result.FromPoint, highLevelTarget))
			{
				targetConnectPoint = result.FromPoint;
			}
			else
			{
				// This happens in some geometric situations: the source cuts back in from the prolongation and then cuts it back off again:
				// \
				//  \
				//  |
				//  \____
				//       \
				// _______________________target line here
				_msg.DebugFormat(
					"Extended curve outside source does not start or end on target.");
				return null;
			}

			return result;
		}

		/// <summary>
		/// Ensures Z-values of vertices in adjacent polygons that were not reshaped
		/// </summary>
		/// <param name="reshapePath"></param>
		/// <param name="nonReshapableGeometries"></param>
		/// <param name="reshapedGeometries"></param>
		public static void EnsureZInNonReshapedNeighbors(
			[NotNull] IPath reshapePath,
			[NotNull] IEnumerable<IGeometry> nonReshapableGeometries,
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries)
		{
			foreach (IGeometry nonReshapableGeometry in nonReshapableGeometries)
			{
				IGeometry highLevelReshapePath =
					GeometryUtils.GetHighLevelGeometry(reshapePath);

				var intersectPoints =
					(IPointCollection) IntersectionUtils.GetIntersectionPoints(
						highLevelReshapePath,
						nonReshapableGeometry);

				//NOTE: if intersection count > 1 it was also reshaped and updated anyway
				if (intersectPoints.PointCount == 1)
				{
					// update Z
					int partIndex;
					int? vertexToUpdate = FindHitVertexIndex(nonReshapableGeometry,
					                                         intersectPoints.Point[0],
					                                         GeometryUtils.GetXyTolerance(
						                                         nonReshapableGeometry),
					                                         out partIndex);
					if (vertexToUpdate != null)
					{
						IPoint pathEndPoint;

						if (GeometryUtils.AreEqualInXY(intersectPoints.Point[0],
						                               pathEndPoint =
							                               reshapePath.FromPoint))
						{
							AddUpdatedGeometry(nonReshapableGeometry,
							                   (int) vertexToUpdate, partIndex,
							                   pathEndPoint, reshapedGeometries);
						}
						else if (GeometryUtils.AreEqualInXY(intersectPoints.Point[0],
						                                    pathEndPoint =
							                                    reshapePath.ToPoint))
						{
							AddUpdatedGeometry(nonReshapableGeometry,
							                   (int) vertexToUpdate, partIndex,
							                   pathEndPoint, reshapedGeometries);
						}

						// do not update random touch points
					}
					else
					{
						_msg.DebugFormat(
							"Adjacent non-reshapable geometry was not updated (no vertex exists).");
					}
				}
			}
		}

		public static bool ReshapeGeometryPart(IGeometry geometryToReshape,
		                                       ReshapeInfo reshapeInfo)
		{
			var reshapeSuccessful = false;

			Assert.NotNull(reshapeInfo.PartIndexToReshape,
			               "PartIndexToReshape was not assigned.");

			_msg.DebugFormat("Reshaping part <index> {0}...",
			                 reshapeInfo.PartIndexToReshape);

			if (_msg.IsVerboseDebugEnabled)
			{
				LogReshapeParameters(geometryToReshape, reshapeInfo.ReshapePath);
			}

			try
			{
				if (! reshapeInfo.ValidateReshapePath())
				{
					return false;
				}

				if (geometryToReshape.GeometryType ==
				    esriGeometryType.esriGeometryPolygon ||
				    geometryToReshape.GeometryType ==
				    esriGeometryType.esriGeometryMultiPatch)
				{
					reshapeSuccessful = ReshapePolygonOrMultipatch(reshapeInfo);
				}
				else if (geometryToReshape.GeometryType ==
				         esriGeometryType.esriGeometryPolyline)
				{
					reshapeSuccessful = ReshapePolylinePart(reshapeInfo);
				}
				else
				{
					Assert.CantReach("Unsupported geometry type for reshape: {0}.",
					                 geometryToReshape.GeometryType);
				}
			}
			catch (Exception)
			{
				try
				{
					LogReshapeParameters(geometryToReshape, reshapeInfo.ReshapePath);
				}
				catch (Exception e)
				{
					_msg.Warn("Error logging reshape parameters.", e);
				}

				throw;
			}

			// important for shape's length property to be correct:
			var segments = geometryToReshape as ISegmentCollection;

			if (segments != null)
			{
				segments.SegmentsChanged();
			}

			return reshapeSuccessful;
		}

		public static void ExecuteWithMinimumTolerance(
			[NotNull] Action procedure,
			[NotNull] params IGeometry[] geometries)
		{
			double originalTolerance = double.NaN;

			foreach (IGeometry geometry in geometries)
			{
				if (double.IsNaN(originalTolerance))
				{
					originalTolerance = GeometryUtils.GetXyTolerance(geometry);
				}
				else
				{
					Assert.True(
						MathUtils.AreEqual(originalTolerance,
						                   GeometryUtils.GetXyTolerance(geometry)),
						"The xy tolerances do not match");
				}

				GeometryUtils.SetMinimumXyTolerance(geometry);
			}

			try
			{
				procedure();
			}
			finally
			{
				foreach (IGeometry geometry in geometries)
				{
					GeometryUtils.SetXyTolerance(geometry, originalTolerance);
				}
			}
		}

		public static void ExecuteWithMinimumTolerance([NotNull] Action procedure,
		                                               [NotNull] IGeometry geometry)
		{
			ExecuteWithMinimumTolerance(procedure, new[] {geometry});
		}

		/// <summary>
		/// Identifies those points among the non-target-vertex points which can be safely removed,
		/// i.e. those that are no from/to points of any of the simplifiedPaths and were only inserted
		/// by cutting by the source geometry and hence do not contribute to the geometry.
		/// </summary>
		/// <param name="simplifiedPaths">The simplified (i.e. merged) reshape paths</param>
		/// <param name="selectedSubcurves">The selected subcurves to reshape along</param>
		/// <returns>The points that can be removed with the respective segment from the original
		/// target which can be used as the stitched-together result. By just removing the point
		/// from the point collection, ArcObjects would insert a linear segment even if the adjacent 
		/// segments are non-linear.</returns>
		[CanBeNull]
		private static IEnumerable<KeyValuePair<IPoint, ISegment>>
			GetStitchPointsToRemove(
				[NotNull] IGeometryCollection simplifiedPaths,
				[NotNull] IEnumerable<CutSubcurve> selectedSubcurves)
		{
			var nonTargetVertexPoints = new Dictionary<IPoint, ISegment>();

			foreach (CutSubcurve cutSubcurve in selectedSubcurves)
			{
				if (cutSubcurve.FromPointIsStitchPoint)
				{
					nonTargetVertexPoints.Add(cutSubcurve.Path.FromPoint,
					                          cutSubcurve.TargetSegmentAtFromPoint);
				}

				if (cutSubcurve.ToPointIsStitchPoint)
				{
					nonTargetVertexPoints.Add(cutSubcurve.Path.ToPoint,
					                          cutSubcurve.TargetSegmentAtToPoint);
				}
			}

			object missing = Type.Missing;
			var protectPoints = new List<IPoint>();

			foreach (IGeometry geometry in GeometryUtils.GetParts(simplifiedPaths))
			{
				var path = (IPath) geometry;

				if (! path.IsClosed)
				{
					protectPoints.Add(path.FromPoint);
					protectPoints.Add(path.ToPoint);
				}
			}

			IMultipoint protectMultipoint =
				GeometryFactory.CreateMultipoint(protectPoints);

			foreach (
				KeyValuePair<IPoint, ISegment> nonTargetVertexSegment in
				nonTargetVertexPoints)
			{
				IPoint nonTargetStitchPoint = nonTargetVertexSegment.Key;

				if (! GeometryUtils.Intersects(nonTargetStitchPoint, protectMultipoint))
				{
					yield return nonTargetVertexSegment;

					// only yield each non-target-vertex point once
					((IPointCollection) protectMultipoint).AddPoint(
						nonTargetStitchPoint, ref missing,
						ref missing);
				}
			}
		}

		private static void RemovePoints([NotNull] IPolycurve polycurve,
		                                 [CanBeNull] IEnumerable<KeyValuePair<IPoint, ISegment>>
			                                 pointsToRemoveWithReplacement)
		{
			if (pointsToRemoveWithReplacement == null)
			{
				return;
			}

			double tolerance = GeometryUtils.GetXyTolerance(polycurve);

			foreach (
				KeyValuePair<IPoint, ISegment> pointWithReplacement in
				pointsToRemoveWithReplacement)
			{
				IPoint point = pointWithReplacement.Key;
				ISegment suggestedReplacement = pointWithReplacement.Value;

				int partIdx;
				int? vertexIndex = GeometryUtils.FindHitVertexIndex(
					polycurve, point, tolerance,
					out partIdx);

				// in case the CutSubcurves are extremely short and isolated (e.g. with beziers and minimum-tolerance)
				// the point might not even exist in the simplified reshape line:
				if (vertexIndex == null)
				{
					continue;
				}

				if (suggestedReplacement == null ||
				    suggestedReplacement.GeometryType ==
				    esriGeometryType.esriGeometryLine)
				{
					// just remove the point
					RemoveCutPointsService.RemovePoint(polycurve, partIdx,
					                                   (int) Assert.NotNull(vertexIndex));
				}
				else
				{
					// use the replacement (we can't reliably merge two non-linear segments)
					// But sometimes (esp. with non-linear, almost-equal curves and minimum tolerance)
					// there are false stitch-points where the curve actually starts on the segment
					SegmentReplacementUtils.TryReplaceSegments(
						polycurve, point, suggestedReplacement);
				}
			}
		}

		[NotNull]
		private static IEnumerable<CutSubcurve> GetSubcurves(
			[NotNull] IEnumerable<CutSubcurve> cutSubcurves,
			[CanBeNull] Predicate<CutSubcurve> selectCurvePredicate)
		{
			foreach (CutSubcurve cutSubcurve in cutSubcurves)
			{
				if (selectCurvePredicate == null || selectCurvePredicate(cutSubcurve))
				{
					yield return cutSubcurve;
				}
			}
		}

		private static string GetLengthDifferenceText(
			[NotNull] IGeometry reshapedGeometry,
			double originalLength)
		{
			double lengthDifference = GeometryUtils.GetLength(reshapedGeometry) -
			                          originalLength;

			string unit;
			int roundDigits;

			var pcs =
				reshapedGeometry.SpatialReference as IProjectedCoordinateSystem;

			if (pcs != null)
			{
				unit = pcs.CoordinateUnit.Abbreviation;
				roundDigits = (int) Math.Round(2.0 / pcs.CoordinateUnit.MetersPerUnit);
			}
			else
			{
				// this doesn't really make sense
				unit = "deg.";
				roundDigits = 5;
			}

			string lengthText;
			if (lengthDifference > 0)
			{
				lengthText = string.Format("{0}{1} longer",
				                           Math.Round(lengthDifference, roundDigits),
				                           unit);
			}
			else
			{
				lengthText = string.Format("{0}{1} shorter",
				                           -Math.Round(lengthDifference, roundDigits),
				                           unit);
			}

			return lengthText;
		}

		private static int? FindHitVertexIndex([NotNull] IGeometry geometry,
		                                       [NotNull] IPoint point,
		                                       double searchTolerance,
		                                       out int partIndex)
		{
			IHitTest hitTest = GeometryUtils.GetHitTest(geometry, true);

			IPoint hitPoint = new PointClass();
			double hitDist = -1;
			partIndex = -1;
			int segmentIndex = -1;
			var rightSide = false;

			int? hitIndex = null;

			if (hitTest.HitTest(point, searchTolerance,
			                    esriGeometryHitPartType.esriGeometryPartVertex, hitPoint,
			                    ref hitDist, ref partIndex, ref segmentIndex,
			                    ref rightSide))
			{
				hitIndex = segmentIndex;
			}

			return hitIndex;
		}

		private static bool CreateExtendedCurve([NotNull] IPolyline originalCurve,
		                                        [NotNull] ICurve highLevelTarget,
		                                        LineEnd atLineEnd,
		                                        [NotNull] out IPolyline constructedCurve)
		{
			constructedCurve = new PolylineClass();

			((IGeometry) constructedCurve).SpatialReference =
				highLevelTarget.SpatialReference;

			return GeometryUtils.TryGetExtendedPolyline(
				originalCurve, highLevelTarget, atLineEnd, constructedCurve);
		}

		private static void LogReshapeParameters([NotNull] IGeometry geometryToReshape,
		                                         [NotNull] IPath reshapePath)
		{
			_msg.DebugFormat(
				"Reshape called with the following parameters: Geometry to reshape: {0}, Reshape path: {1}",
				GeometryUtils.ToString(geometryToReshape),
				GeometryUtils.ToString(reshapePath));

			const string varName = "PROSUITE_GEOMETRY_ERROR_PATH";

			string path = Environment.GetEnvironmentVariable(varName);

			if (! string.IsNullOrEmpty(path) && Directory.Exists(path))
			{
				GeometryUtils.ToXmlFile(geometryToReshape,
				                        Path.Combine(path, "GeometryToReshape.xml"));
				GeometryUtils.ToXmlFile(reshapePath,
				                        Path.Combine(path, "ReshapePath.xml"));
			}
		}

		private static void VerboseLogNonSimple([NotNull] IGeometry geometry)
		{
			if (! _msg.IsVerboseDebugEnabled)
			{
				return;
			}

			string nonSimpleEx;
			bool simple = GeometryUtils.IsGeometrySimple(geometry,
			                                             geometry.SpatialReference,
			                                             true, out nonSimpleEx);
			_msg.DebugFormat("Simple: {0}. Non simple reason: {1}", simple, nonSimpleEx);

			LogNonSimpleSegmentOrientation(geometry);
		}

		private static void LogNonSimpleSegmentOrientation(
			[NotNull] IGeometry geometryToReshape)
		{
			var segments = geometryToReshape as ISegmentCollection;

			if (segments == null)
			{
				return;
			}

			IEnumSegment enumSegments = segments.EnumSegments;

			enumSegments.Reset();

			IPoint fromPoint = new PointClass();

			int currentPartIdx = -999;
			IPoint lastToPoint = null;

			ISegment segment;
			int outPartIndex = -1;
			int segmentIndex = -1;
			enumSegments.Next(out segment, ref outPartIndex, ref segmentIndex);

			while (segment != null)
			{
				if (currentPartIdx != outPartIndex)
				{
					currentPartIdx = outPartIndex;
				}
				else
				{
					if (lastToPoint != null)
					{
						segment.QueryFromPoint(fromPoint);

						if (! GeometryUtils.AreEqual(fromPoint, lastToPoint))
						{
							_msg.WarnFormat(
								"Non simple segment: {0} {1} {2} vs. {3} {4} {5}",
								lastToPoint.X, lastToPoint.Y, lastToPoint.Z,
								fromPoint.X, fromPoint.Y, fromPoint.Z);
						}
					}
				}

				lastToPoint = segment.ToPoint;

				Marshal.ReleaseComObject(segment);
				enumSegments.Next(out segment, ref outPartIndex, ref segmentIndex);
			}
		}

		private static void RemoveFlippedRings(
			[NotNull] ReshapeInfo reshapeInfo,
			[NotNull] IRing newRing)
		{
			if (reshapeInfo.AllowFlippedRings)
			{
				return;
			}

			List<int> deletedParts = RemoveFlippedRings(newRing, reshapeInfo);

			if (deletedParts.Count > 0)
			{
				string messageFormat = deletedParts.Count == 1
					                       ? "Deleted ring <part index> {1} because it was made reduntant by the reshape"
					                       : "Deleted {0} ring(s) because they were made redundant by the reshape <part indexes>: {1}";

				string message = string.Format(messageFormat, deletedParts.Count,
				                               StringUtils.Concatenate(
					                               deletedParts, ", "));

				_msg.DebugFormat(message);

				NotificationUtils.Add(reshapeInfo.Notifications, message);
			}

			// NOTE: The part index of this and other reshape infos will be corrected when
			//		 accessing it the next time.
		}

		[NotNull]
		private static List<int> RemoveFlippedRings([NotNull] IRing reshapedRing,
		                                            [NotNull] ReshapeInfo reshapeInfo)
		{
			var result = new List<int>();

			if (reshapeInfo.ReplacedSegments == null)
			{
				_msg.Warn(
					"Replaced segments not recorded, unable to calculate flipped rings");
				return result;
			}

			var allRings = (IGeometryCollection) reshapeInfo.GeometryToReshape;
			if (allRings.GeometryCount <= 0)
			{
				return result;
			}

			Stopwatch watch = _msg.DebugStartTiming(
				"Checking {0} rings for inversion due to reshape",
				allRings.GeometryCount);

			IPolygon changedAreas = GetChangedAreas(reshapeInfo, reshapedRing);

			result = DeleteFlippedRings(allRings, reshapedRing, changedAreas);

			Marshal.ReleaseComObject(changedAreas);

			_msg.DebugStopTiming(watch,
			                     "Deleted {0} rings because they were made redundant by reshape",
			                     result.Count);

			return result;
		}

		[NotNull]
		private static IPolygon GetChangedAreas([NotNull] ReshapeInfo reshapeInfo,
		                                        [NotNull] IRing newRing)
		{
			if (reshapeInfo.ReplacedSegments.IsClosed)
			{
				// NOTE: if the reshape line is a closed ring the entire ring was replaced!
				//		 -> use difference between new ring and old ring!

				_msg.Debug(
					"Complete ring was replaced, reverting to ITopologicalOperator.Difference");

				IGeometry newPoly = GeometryUtils.GetHighLevelGeometry(newRing);
				IGeometry originalPoly =
					GeometryFactory.CreatePolygon(reshapeInfo.ReplacedSegments);

				GeometryUtils.Simplify(newPoly, true);
				GeometryUtils.Simplify(originalPoly, true);

				IGeometry added = IntersectionUtils.Difference(newPoly, originalPoly);
				IGeometry removed = IntersectionUtils.Difference(originalPoly, newPoly);

				GeometryUtils.Simplify(added, true);
				GeometryUtils.Simplify(removed, true);

				return (IPolygon) GeometryUtils.Union(added, removed);
			}

			// standard case, use faster implementation
			IPolygon changedAreas =
				GeometryFactory.CreateEmptyPolygon(reshapeInfo.ReplacedSegments);

			var changedAreaSegments = (ISegmentCollection) changedAreas;

			changedAreaSegments.AddSegmentCollection(
				(ISegmentCollection) reshapeInfo.ReplacedSegments);

			IPath reshapePath = reshapeInfo.CutReshapePath == null
				                    ? reshapeInfo.ReshapePath
				                    : reshapeInfo.CutReshapePath.Path;

			changedAreaSegments.AddSegmentCollection(
				(ISegmentCollection) GeometryFactory.Clone(reshapePath));

			GeometryUtils.Simplify(changedAreas, true);
			return changedAreas;
		}

		[NotNull]
		private static List<int> DeleteFlippedRings(
			[NotNull] IGeometryCollection fromRings,
			[NotNull] IRing reshapedRing,
			[NotNull] IPolygon changedAreas)
		{
			var result = new List<int>();

			if (changedAreas.IsEmpty)
			{
				return result;
			}

			var deleteCount = 0;

			int originalGeometryCount = fromRings.GeometryCount;

			for (var i = 0; i < originalGeometryCount; i++)
			{
				var ring = (IRing) fromRings.Geometry[i - deleteCount];

				if (! GeometryUtils.AreEqual(ring, reshapedRing))
				{
					if (DeleteContainedInvertableRing(changedAreas, ring, i - deleteCount,
					                                  fromRings,
					                                  reshapedRing))
					{
						deleteCount++;
						result.Add(i);
					}
				}

				Marshal.ReleaseComObject(ring);
			}

			return result;
		}

		private static bool DeleteContainedInvertableRing(
			[NotNull] IGeometry containingPoly,
			[NotNull] IRing ring,
			int ringIndex,
			[NotNull] IGeometryCollection allRings,
			IRing reshapedRing)
		{
			//// NOTE: this is very prone to yield wrong results (e.g. if the envelope is not contained!)
			//// NOTE: This was not reproduced since the reshaped geometries segments are all oriented correctly
			//IGeometry highLevelRing = GeometryUtils.GetHighLevelGeometry(ring);
			//if (((IRelationalOperator) containingPoly).Contains(highLevelRing))
			//{
			//    allRings.RemoveGeometries(ringIndex, 1);
			//    return true;
			//}

			// -> currently only one ring at the time is reshaped therefore if one point of the ring is 
			// inside the entire ring is inside (assuming the geometry is simple to start with)
			if (GeometryUtils.Contains(containingPoly, ring.FromPoint))
			{
				// now determine if it is invertable, i.e. 
				// same orientation ring, must not be contained by added area
				// different orientation ring, most not be contained by removed area

				// here we can do more expensive things as this happens quite rarely
				// NOTE: using allRings returns the wrong result! -> use reshapedRing
				IGeometry highLevelReshapedRing = GeometryUtils.GetHighLevelGeometry(
					reshapedRing, true);

				bool ringIsInAddedArea = GeometryUtils.Contains(highLevelReshapedRing,
				                                                ring.FromPoint);

				Marshal.ReleaseComObject(highLevelReshapedRing);

				// remove exterior rings that are in the added area of exterior rings
				// remove holes that are in added area of holes
				// remove holes that are in removd area of exterior ring &&
				// remove exterior rings that are in removed areas of a hole

				bool ringIsExterior = IsExteriorRing(allRings, ring);
				bool reshapedIsExterior = IsExteriorRing(allRings, reshapedRing);
				if ((reshapedIsExterior == ringIsExterior && ringIsInAddedArea) ||
				    (reshapedIsExterior != ringIsExterior && ! ringIsInAddedArea))
				{
					allRings.RemoveGeometries(ringIndex, 1);
					return true;
				}
			}

			return false;
		}

		private static bool IsExteriorRing(IGeometry parentGeometry, IRing ring)
		{
			return IsExteriorRing((IGeometryCollection) parentGeometry, ring);
		}

		private static bool IsExteriorRing(IGeometryCollection parentGeometry, IRing ring)
		{
			var multipatch = parentGeometry as IMultiPatch2;

			if (multipatch != null)
			{
				var isBeginningRing = false;
				esriMultiPatchRingType ringType =
					multipatch.GetRingType(ring, ref isBeginningRing);

				return ringType != esriMultiPatchRingType.esriMultiPatchInnerRing;
			}

			return ring.IsExterior;
		}

		private static void AddUpdatedGeometry(
			[NotNull] IGeometry geometry,
			int localVertexIdxToUpdate,
			int partIdxToUpdate,
			[NotNull] IPoint newPoint,
			[NotNull] IDictionary<IGeometry, NotificationCollection> updatedGeometries)
		{
			int globalVertexIdx = GeometryUtils.GetGlobalIndex(geometry,
			                                                   partIdxToUpdate,
			                                                   localVertexIdxToUpdate);

			((IPointCollection) geometry).UpdatePoint(globalVertexIdx, newPoint);

			var notification = new NotificationCollection
			                   {"Updated Z value in adjacent geometry."};

			updatedGeometries.Add(geometry, notification);
		}

		#region Reshape multiple parts

		private static bool ReshapeAllParts(
			[NotNull] ReshapeInfo masterReshapeInfo,
			[NotNull] IPath reshapePath,
			[NotNull] ICollection<int> allReshapablePartIndexes,
			out IList<ReshapeInfo> singlePartReshapes)
		{
			var reshapeSuccessful = false;

			IGeometry geometryToReshape = masterReshapeInfo.GeometryToReshape;
			NotificationCollection notifications = masterReshapeInfo.Notifications;

			List<ReshapeInfo> allPartReshapeInfos = CreateAllPartReshapeInfos(
				masterReshapeInfo, reshapePath,
				allReshapablePartIndexes);

			bool canCombinedReshape = TryPrepareCombinedReshapes(geometryToReshape,
			                                                     allPartReshapeInfos,
			                                                     notifications);

			_msg.DebugFormat(
				canCombinedReshape
					? "Reshaping {0} remaining parts in a combined island-cut-off..."
					: "Reshaping all {0} parts individually...",
				allPartReshapeInfos.Count);

			singlePartReshapes = new List<ReshapeInfo>(allPartReshapeInfos.Count);

			using (_msg.IncrementIndentation())
			{
				// reshape all (remaining) parts (both from combined and non-combined reshapes)
				foreach (ReshapeInfo reshapePartInfo in allPartReshapeInfos)
				{
					if (reshapePartInfo.PartIndexToReshape == null)
					{
						// the (inner) part was deleted by the combined reshape
						continue;
					}

					bool successful =
						ReshapeGeometryPart(geometryToReshape, reshapePartInfo);

					if (successful)
					{
						reshapeSuccessful = true;
						singlePartReshapes.Add(reshapePartInfo);
					}
				}
			}

			return reshapeSuccessful;
		}

		private static List<ReshapeInfo> CreateAllPartReshapeInfos(
			[NotNull] ReshapeInfo masterReshapeInfo,
			[NotNull] IPath reshapePath,
			[NotNull] ICollection<int> allReshapablePartIndexes)
		{
			var partReshapes =
				new List<ReshapeInfo>(allReshapablePartIndexes.Count);

			Stopwatch watch =
				_msg.DebugStartTiming("Creating reshape infos for {0} parts to reshape..",
				                      allReshapablePartIndexes.Count);

			using (_msg.IncrementIndentation())
			{
				foreach (int partIndex in allReshapablePartIndexes)
				{
					var partReshapeInfo =
						new ReshapeInfo(masterReshapeInfo.GeometryToReshape, reshapePath,
						                masterReshapeInfo.Notifications)
						{
							PartIndexToReshape = partIndex,
							ReshapeResultFilter = masterReshapeInfo.ReshapeResultFilter,
							AllowOpenJawReshape = masterReshapeInfo.AllowOpenJawReshape,
							RingReshapeSide = masterReshapeInfo.RingReshapeSide
						};

					partReshapeInfo.CutReshapePath = GetCutReshapePath(partReshapeInfo);

					partReshapes.Add(partReshapeInfo);
				}
			}

			_msg.DebugStopTiming(watch, "Created {0} reshape infos",
			                     allReshapablePartIndexes.Count);

			return partReshapes;
		}

		/// <summary>
		/// Changes the outer reshape's reshape lines to include the remaining part
		/// of the inner reshaped rings and deletes the inner rings.
		/// </summary>
		/// <param name="geometryToReshape"></param>
		/// <param name="partReshapes"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		private static bool TryPrepareCombinedReshapes(
			IGeometry geometryToReshape,
			IList<ReshapeInfo> partReshapes,
			NotificationCollection notifications)
		{
			if (geometryToReshape.GeometryType != esriGeometryType.esriGeometryPolygon &&
			    geometryToReshape.GeometryType != esriGeometryType.esriGeometryMultiPatch)
			{
				return false;
			}

			Stopwatch watch =
				_msg.DebugStartTiming("Processing combined reshape infos...");

			Dictionary<ReshapeInfo, ReshapeInfo> combinedReshapes;

			using (_msg.IncrementIndentation())
			{
				if (! TryGetCombinedExteriorInteriorReshapes(
					    partReshapes, notifications, out combinedReshapes))
				{
					return false;
				}

				List<int> ringsToRemove;
				if (! TryProcessCombinedReshapeInfos(
					    combinedReshapes, out ringsToRemove, notifications))
				{
					return false;
				}

				// remove inner rings that participate in combined reshape
				GeometryUtils.RemoveParts((IGeometryCollection) geometryToReshape,
				                          ringsToRemove);
			}

			_msg.DebugStopTiming(watch, "Processed {0} combined reshape infos",
			                     combinedReshapes.Count);

			return true;
		}

		private static bool TryProcessCombinedReshapeInfos(
			Dictionary<ReshapeInfo, ReshapeInfo> combinedReshapes,
			out List<int> ringsToRemove,
			NotificationCollection notifications)
		{
			ringsToRemove = new List<int>();

			foreach (KeyValuePair<ReshapeInfo, ReshapeInfo> combinedReshape in
				combinedReshapes)
			{
				ReshapeInfo innerReshape = combinedReshape.Key;
				ReshapeInfo outerReshape = combinedReshape.Value;

				Assert.NotNull(innerReshape.PartIndexToReshape,
				               "Inner reshape part null.");
				Assert.NotNull(outerReshape.PartIndexToReshape,
				               "Outer reshape part null.");

				_msg.DebugFormat(
					"Processing combined reshape - inner part: {0} / outer part: {1}",
					innerReshape.PartIndexToReshape, outerReshape.PartIndexToReshape);

				innerReshape.RingReshapeSide = DetermineInnerRingReshapeSide(innerReshape,
				                                                             outerReshape);

				if (innerReshape.RingReshapeSide == RingReshapeSideOfLine.Undefined)
				{
					// NOTE: we cannot safely process any other combined reshapes because the reshape of the
					//		 outer part (which might reshape several inner parts) would result in wrong inner
					//		 reshape results (the island would be completely lost).
					NotificationUtils.Add(notifications,
					                      "Unable to process combined reshape of parts {0} and {1}. The island's reshape might be too complex.",
					                      innerReshape.PartIndexToReshape,
					                      outerReshape.PartIndexToReshape);

					return false;
				}

				IPath reshapedOuterReshapePath = GetPathReshapedAlongInnerReshape(
					Assert.NotNull(outerReshape.CutReshapePath).Path,
					innerReshape);

				outerReshape.CutReshapePath = new CutSubcurve(
					reshapedOuterReshapePath, true, true);

				ringsToRemove.Add(Assert.NotNull(innerReshape.PartIndexToReshape).Value);
			}

			return true;
		}

		/// <summary>
		/// Reshapes the inner ring first and then reshapes the reshapePath
		/// to follow the outline of the remainders of the inner ring.
		/// </summary>
		/// <param name="reshapePath"></param>
		/// <param name="innerReshape"></param>
		/// <returns></returns>
		[NotNull]
		private static IPath GetPathReshapedAlongInnerReshape(
			[NotNull] IPath reshapePath,
			[NotNull] ReshapeInfo innerReshape)
		{
			// performance improvement:
			innerReshape.AllowFlippedRings = true;

			innerReshape.ReshapeResultFilter = new ReshapeResultFilter(false);
			ReshapePolygonOrMultipatch(innerReshape);

			var highLevelOuterReshapePath =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(reshapePath);

			ReshapeAlongInnerReshape(highLevelOuterReshapePath, innerReshape);

			return (IPath) ((IGeometryCollection) highLevelOuterReshapePath).Geometry[0];
		}

		/// <summary>
		/// Reshapes the high level reshape path along the inner reshape's ring (which previously was
		/// reshaped with the same reshape path.
		/// </summary>
		/// <param name="highLevelReshapePath"></param>
		/// <param name="innerReshape"></param>
		private static void ReshapeAlongInnerReshape(
			[NotNull] IPolyline highLevelReshapePath,
			[NotNull] ReshapeInfo innerReshape)
		{
			IPolyline highLevelInnerRingBoundary = GeometryFactory.CreatePolyline(
				innerReshape.GetGeometryPartToReshape());

			IGeometry nonReshapedInnerRingPart =
				GetDifferencePolyline(highLevelInnerRingBoundary,
				                      highLevelReshapePath);

			var nonReshapedInnerRingPath =
				(IPath) ((IGeometryCollection) nonReshapedInnerRingPart).Geometry[0];

			ReshapeGeometry(highLevelReshapePath, nonReshapedInnerRingPath, false, null);
		}

		/// <summary>
		/// Determines the side of the inner ring reshape in a combined ring reshape. 
		/// The side that is within the outer reshape will be returned, i.e. the result is
		/// contained by the outer reshape result.
		/// </summary>
		/// <param name="innerReshape"></param>
		/// <param name="outerReshape"></param>
		/// <returns></returns>
		private static RingReshapeSideOfLine DetermineInnerRingReshapeSide(
			[NotNull] ReshapeInfo innerReshape,
			[NotNull] ReshapeInfo outerReshape)
		{
			RingReshapeSideOfLine innerRingReshapeSide;

			IPolygon leftPoly, rightPoly;

			if (! TryGetInsideOnlyReshapeResults(innerReshape, out leftPoly,
			                                     out rightPoly))
			{
				innerRingReshapeSide = RingReshapeSideOfLine.Undefined;
			}
			else
			{
				IGeometry outerReshapedRing =
					GetClonedReshapedHighLevelRing(outerReshape);

				// if inner reshape along boundary: one of the polys is empty, the other one contained
				if (! rightPoly.IsEmpty &&
				    GeometryUtils.Contains(outerReshapedRing, rightPoly))
				{
					innerRingReshapeSide = RingReshapeSideOfLine.Right;
				}
				else
				{
					Assert.True(
						! leftPoly.IsEmpty &&
						GeometryUtils.Contains(outerReshapedRing, leftPoly),
						"Interior ring reshape has no part in outer reshape.");

					innerRingReshapeSide = RingReshapeSideOfLine.Left;
				}
			}

			return innerRingReshapeSide;
		}

		/// <summary>
		/// Determines the reshape results but only those parts that are completely within the original ring to reshape.
		/// </summary>
		/// <param name="reshapeInfo"></param>
		/// <param name="leftPoly"></param>
		/// <param name="rightPoly"></param>
		private static bool TryGetInsideOnlyReshapeResults(ReshapeInfo reshapeInfo,
		                                                   out IPolygon leftPoly,
		                                                   out IPolygon rightPoly)
		{
			var ringToReshape = (IRing) reshapeInfo.GetGeometryPartToReshape();

			reshapeInfo.GetBothSideReshapePolygons(out leftPoly, out rightPoly);

			RingReshapeType reshapeType = reshapeInfo.GetRingReshapeType(
				leftPoly, rightPoly,
				ringToReshape);

			_msg.DebugFormat("Inner ring reshape type is: {0}", reshapeType);
			if (reshapeType != RingReshapeType.InsideOnly &&
			    reshapeType != RingReshapeType.InsideAndOutside &&
			    reshapeType != RingReshapeType.AlongBoundary)
			{
				_msg.DebugFormat("Unsupported reshape type of inner ring: {0}",
				                 reshapeType);

				leftPoly.SetEmpty();
				rightPoly.SetEmpty();

				return false;
			}

			if (reshapeType == RingReshapeType.InsideAndOutside)
			{
				// cut off / disregart the outside parts:
				IGeometry highLevelInnerRing =
					GeometryUtils.GetHighLevelGeometry(ringToReshape);

				// simplify is needed for correct intersect result
				GeometryUtils.Simplify(highLevelInnerRing);

				leftPoly =
					(IPolygon)
					((ITopologicalOperator) leftPoly).Intersect(
						highLevelInnerRing, esriGeometryDimension.esriGeometry2Dimension);

				rightPoly =
					(IPolygon)
					((ITopologicalOperator) rightPoly).Intersect(
						highLevelInnerRing, esriGeometryDimension.esriGeometry2Dimension);
			}

			return true;
		}

		private static IGeometry GetClonedReshapedHighLevelRing(
			ReshapeInfo originalReshape)
		{
			IGeometry geometryClone =
				GeometryFactory.Clone(originalReshape.GeometryToReshape);

			var notifications = new NotificationCollection();

			// determine reshape side on original (only once) and use for all clones
			originalReshape.DetermineReshapeSide();

			ReshapeInfo outerReshapeClone =
				originalReshape.CreateCopy(geometryClone, null);

			outerReshapeClone.AllowFlippedRings = true;
			outerReshapeClone.AllowPhantomIntersectionPoints = true;

			Assert.True(ReshapePolygonOrMultipatch(outerReshapeClone),
			            "Expected reshape not possible: {0}",
			            notifications.Concatenate(" "));

			IGeometry result =
				GeometryUtils.GetHighLevelGeometry(
					outerReshapeClone.GetGeometryPartToReshape());

			Marshal.ReleaseComObject(geometryClone);

			outerReshapeClone.Dispose();

			return result;
		}

		/// <summary>
		/// Returns the dictionary of the combined reshapes, i.e. two connected rings are reshaped.
		/// The inside (island) ring reshapes are the keys of the dictionary, the outer ring reshapes
		/// the values.
		/// </summary>
		/// <param name="partReshapes"></param>
		/// <param name="predicate">The predicate, having as first parameter the reshape with the containing
		/// reshape path and as the second parameted the reshape with the contained reshape path.</param>
		/// <param name="combinedReshapes"></param>
		/// <returns></returns>
		private static bool TryGetCombinedReshapes(
			[NotNull] IList<ReshapeInfo> partReshapes,
			[CanBeNull] Func<ReshapeInfo, ReshapeInfo, bool> predicate,
			[NotNull] out Dictionary<ReshapeInfo, ReshapeInfo> combinedReshapes)
		{
			combinedReshapes = new Dictionary<ReshapeInfo, ReshapeInfo>();

			foreach (
				KeyValuePair<ReshapeInfo, ReshapeInfo> pair in
				CollectionUtils.GetAllTuples(partReshapes))
			{
				ReshapeInfo thisReshapeInfo = pair.Key;
				ReshapeInfo otherReshapeInfo = pair.Value;

				_msg.DebugFormat("Testing combination between part {0} and {1}...",
				                 thisReshapeInfo.PartIndexToReshape,
				                 otherReshapeInfo.PartIndexToReshape);

				IGeometry otherReshapePath = GeometryUtils.GetHighLevelGeometry(
					Assert.NotNull(otherReshapeInfo.CutReshapePath).Path, true);

				IGeometry thisReshapePath = GeometryUtils.GetHighLevelGeometry(
					Assert.NotNull(thisReshapeInfo.CutReshapePath).Path, true);

				// so far the only supported case: the inner ring is the shorter path and contained by the outer part's reshape path
				if (GeometryUtils.Contains(thisReshapePath, otherReshapePath))
				{
					if (predicate == null || predicate(thisReshapeInfo, otherReshapeInfo))
					{
						// the inner reshape can never chose:
						otherReshapeInfo.RingReshapeSide =
							RingReshapeSideOfLine.Undefined;
						combinedReshapes.Add(otherReshapeInfo, thisReshapeInfo);
					}
					else
					{
						return false;
					}
				}
				else if (GeometryUtils.Contains(otherReshapePath, thisReshapePath))
				{
					if (predicate == null || predicate(otherReshapeInfo, thisReshapeInfo))
					{
						// the inner reshape can never chose:
						thisReshapeInfo.RingReshapeSide = RingReshapeSideOfLine.Undefined;
						combinedReshapes.Add(thisReshapeInfo, otherReshapeInfo);
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the dictionary of the combined reshapes, i.e. two connected rings are reshaped.
		/// The inside (island) ring reshapes are the keys of the dictionary, the outer ring reshapes
		/// the values.
		///         |   |
		///   ------|---|-------
		///   |   --|---|--    |
		///   |   | |___| |    |
		///   |   |       |    |
		///   |   ---------    |
		///   |                |
		///   ------------------
		/// 
		/// The sketch is the U
		/// 
		/// </summary>
		/// <param name="partReshapes"></param>
		/// <param name="notifications"></param>
		/// <param name="combinedReshapes"></param>
		/// <returns></returns>
		private static bool TryGetCombinedExteriorInteriorReshapes(
			IList<ReshapeInfo> partReshapes,
			NotificationCollection notifications,
			out Dictionary<ReshapeInfo, ReshapeInfo> combinedReshapes)
		{
			return TryGetCombinedReshapes(
				partReshapes,
				(outer, inner) =>
					IsExteriorInteriorRingCombination(outer, inner, notifications),
				out combinedReshapes);
		}

		private static bool IsExteriorInteriorRingCombination(
			ReshapeInfo exteriorRingReshape,
			ReshapeInfo interiorRingReshape,
			NotificationCollection notifications)
		{
			var thisRing = (IRing) exteriorRingReshape.GetGeometryPartToReshape();
			bool thisRingIsExterior = IsExteriorRing(
				exteriorRingReshape.GeometryToReshape,
				thisRing);

			var otherRing = (IRing) interiorRingReshape.GetGeometryPartToReshape();
			bool otherRingIsExterior = IsExteriorRing(
				interiorRingReshape.GeometryToReshape,
				otherRing);

			if (thisRingIsExterior && ! otherRingIsExterior)
			{
				return true;
			}

			if (thisRingIsExterior && otherRingIsExterior)
			{
				_msg.Debug(
					"It's a combined reshape between two exterior rings - it's supported!");
			}
			else if (! thisRingIsExterior && ! otherRingIsExterior)
			{
				_msg.Debug(
					"It's a combined reshape between two interior rings - it's supported!");
			}
			else
			{
				NotificationUtils.Add(notifications,
				                      "Combined reshapes starting within inner ring are not supported. Try start reshape from outer ring");
			}

			return false;
		}

		#endregion

		#endregion

		#region Reshape Polygon Ring

		[CanBeNull]
		private static IRing ReshapeOrReplaceRing([NotNull] ReshapeInfo reshapeInfo)
		{
			Assert.ArgumentNotNull(reshapeInfo, nameof(reshapeInfo));

			IPath reshapePath = reshapeInfo.ReshapePath;

			// Reshape path is closed curve - replace the ring ...
			if (reshapeInfo.IsReshapePathClosed())
			{
				if (reshapeInfo.IntersectionPoints.PointCount == 1)
				{
					// ...unless source and target only intersect in a single point from the outside
					IGeometry highLevelRing = GeometryFactory.CreatePolygon(reshapePath);

					bool reshapePathTouches = GeometryUtils.Touches(
						reshapeInfo.GeometryToReshape,
						highLevelRing);

					Marshal.ReleaseComObject(highLevelRing);

					if (reshapePathTouches)
					{
						NotificationUtils.Add(reshapeInfo.Notifications,
						                      "The reshape line is a closed ring outside the geometry to reshape");
						return null;
					}
				}

				var ringToReshape = (IRing) reshapeInfo.GetGeometryPartToReshape();

				// Note: proper orientation is important to avoid incorrect IRel-Op results later on in flip rings
				IRing replacement = GeometryFactory.CreateRing(reshapePath,
				                                               ! ringToReshape
					                                               .IsExterior);

				ReplaceRingToReshape(reshapeInfo, replacement);

				return replacement;
			}

			// remove the dangling bits of the reshapePath
			if (reshapeInfo.CutReshapePath == null)
			{
				reshapeInfo.CutReshapePath = GetCutReshapePath(reshapeInfo);
			}

			IRing reshapedRing = ReshapeRing(reshapeInfo);

			return reshapedRing;
		}

		private static CutSubcurve GetCutReshapePath([NotNull] ReshapeInfo reshapeInfo)
		{
			IPoint firstCutPoint, lastCutPoint;

			IPath trimmedReplacement = GetTrimmedReshapeLine(reshapeInfo,
			                                                 out firstCutPoint,
			                                                 out lastCutPoint);

			if (reshapeInfo.GeometryToReshape.GeometryType ==
			    esriGeometryType.esriGeometryPolygon)
			{
				AssertFromToPointsEqual(firstCutPoint, lastCutPoint, trimmedReplacement);
			}

			return new CutSubcurve(trimmedReplacement, true, true);
		}

		private static void AssertFromToPointsEqual(IPoint firstCutPoint,
		                                            IPoint lastCutPoint,
		                                            IPath trimmedReplacement)
		{
			// NOTE: In situations such as MultipleGeometriesReshaperTest.CanReshapeMixedSelectionAlongThirdPoly
			//	     topological operator finds intersection points between the source and the target (with a distance
			//		 of 1.3 times the tolerance between the intersection point and the source/target geometry. But the
			//		 way the trimmed replacements are obtained (split at points) can result in segments not being found
			//		 with hit test later on...

			// NOTE regarding 'exact' comparison using GeometryUtils.AreEqual: Rarely the Z values
			//		vary slightly more than the tolerance -> use AreEqualInXY
			if (! GeometryUtils.AreEqualInXY(firstCutPoint, trimmedReplacement.FromPoint))
			{
				_msg.DebugFormat("First point {0} not equal to From Point of {1}: ",
				                 GeometryUtils.ToString(firstCutPoint),
				                 GeometryUtils.ToString(trimmedReplacement));

				throw new AssertionException(
					"First cut point not equal to from-point. See debug log.");
			}

			if (! GeometryUtils.AreEqualInXY(lastCutPoint, trimmedReplacement.ToPoint))
			{
				_msg.DebugFormat("Last point {0} not equal to To Point of {1}: ",
				                 GeometryUtils.ToString(lastCutPoint),
				                 GeometryUtils.ToString(trimmedReplacement));

				throw new AssertionException(
					"Last cut point not equal to to-point. See debug log.");
			}
		}

		private static IRing ReshapeRing([NotNull] ReshapeInfo reshapeInfo)
		{
			Assert.ArgumentNotNull(reshapeInfo, nameof(reshapeInfo));
			Assert.ArgumentCondition(reshapeInfo.CutReshapePath != null,
			                         "CutReshapePath (trimmed replacement path) is null.");

			IRing result;

			RingReshapeSideOfLine reshapeSide = GetReshapeSideOfPath(reshapeInfo,
			                                                         out result);

			if (reshapeSide == RingReshapeSideOfLine.Undefined)
			{
				Assert.NotNull(result,
				               "Undefined reshape side of line without unclear-result-ring");

				// this is a non-intuitive reshape path (normal users wouldn't do this)
				// just use the largest part (probably same as standard reshape)
				NotificationUtils.Add(reshapeInfo.Notifications,
				                      "Using largest part only");

				ReplaceRingToReshape(reshapeInfo, result);
			}
			else
			{
				IPath cutReshapePath = reshapeInfo.CutReshapePath.Path;

				// use the Left/Right poly directly from ReshapeInfo? and delete phantom points?

				IPointCollection potentialPhantomPoints = null;
				if (! reshapeInfo.AllowPhantomIntersectionPoints)
				{
					potentialPhantomPoints = reshapeInfo.IntersectionPoints;
				}

				result = (IRing) reshapeInfo.GetGeometryPartToReshape();

				// store the replaced path
				reshapeInfo.ReplacedSegments =
					SegmentReplacementUtils.GetSegmentsToReplace(
						result, cutReshapePath.FromPoint, cutReshapePath.ToPoint,
						reshapeSide);

				// TODO: if simplified reshape side determination was used and a multipart results -> revisit reshape side determination

				bool nonPlanar = reshapeInfo.NonPlanar;

				SegmentReplacementUtils.ReplaceSegments(
					result, cutReshapePath, reshapeSide, nonPlanar,
					potentialPhantomPoints);
			}

			return result;
		}

		private static void ReplaceRingToReshape([NotNull] ReshapeInfo reshapeInfo,
		                                         [NotNull] IRing replacementRing)
		{
			Assert.ArgumentCondition(reshapeInfo.PartIndexToReshape != null,
			                         "PartIndexToReshape is null.");

			var partIndexToReplace = (int) reshapeInfo.PartIndexToReshape;

			var ringToReplace = (IRing) reshapeInfo.GetGeometryPartToReshape();

			reshapeInfo.ReplacedSegments = GeometryFactory.Clone(ringToReplace);

			GeometryUtils.ReplaceGeometryPart(reshapeInfo.GeometryToReshape,
			                                  partIndexToReplace,
			                                  replacementRing);

			// important to update also the cached geometry part to reshape 
			// (used for reference-equal comparison when checking part index)
			reshapeInfo.PartIndexToReshape = partIndexToReplace;
		}

		/// <summary>
		/// Determines whether the left side should be reshaped (or the right side). If it is not
		/// clear which side should be reshaped an (out) result polygon is provided.
		/// </summary>
		/// <param name="reshapeInfo"></param>
		/// <param name="unclearResultRing"></param>
		/// <returns></returns>
		private static RingReshapeSideOfLine GetReshapeSideOfPath(
			[NotNull] ReshapeInfo reshapeInfo,
			[CanBeNull] out IRing unclearResultRing)
		{
			Assert.ArgumentCondition(reshapeInfo.CutReshapePath != null,
			                         "CutReshapePath is null.");

			unclearResultRing = null;

			RingReshapeSideOfLine proposedSide = reshapeInfo.RingReshapeSide;

			if (proposedSide != RingReshapeSideOfLine.Undefined)
			{
				_msg.DebugFormat("GetReshapeSideOfPath: returning pre-assigned value {0}",
				                 proposedSide);

				return proposedSide;
			}

			return reshapeInfo.DetermineReshapeSide(out unclearResultRing);
		}

		#endregion

		#region Replace Curve Segments

		[CanBeNull]
		public static IPoint GetOpenJawReshapeLineReplaceEndPoint(
			[NotNull] IPolyline lineToReshape,
			[NotNull] IPolyline reshapeLine,
			bool tryNonDefaultSide)
		{
			var intersectionPoints =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					lineToReshape, reshapeLine);

			if (intersectionPoints.PointCount == 0)
			{
				return null;
			}

			var pathToReshape = (IPath) GeometryUtils.GetHitGeometryPart(
				intersectionPoints.Point[0], lineToReshape,
				GeometryUtils.GetXyTolerance(lineToReshape));

			Assert.NotNull(pathToReshape, "No path to reshape found");

			bool useFromPoint;

			if (intersectionPoints.PointCount == 1)
			{
				IPoint splitPoint;
				IPath singleCutReshapePath = GetSingleCutReshapePath(
					reshapeLine, intersectionPoints, out splitPoint);

				string message;
				useFromPoint = ReplaceFromPointInSingleCutOpenJaw(
					pathToReshape, singleCutReshapePath, splitPoint, tryNonDefaultSide,
					out message);
			}
			else
			{
				// there are possibly dangling lines before the first and/or after the last split point
				var reshapePath = (IPath) ((IGeometryCollection) reshapeLine).Geometry[0];

				// re-clone to avoid cutting the reshape path
				IGeometry highLevelReshapePath =
					GeometryUtils.GetHighLevelGeometry(reshapePath);

				IPoint firstSplitPoint, lastSplitPoint;
				IGeometryCollection splittedReshapeLine = SplitReshapePath(
					highLevelReshapePath, intersectionPoints, out firstSplitPoint,
					out lastSplitPoint);

				IPath startDangle, endDangle;
				TrimReshapePath(reshapePath, splittedReshapeLine, firstSplitPoint,
				                lastSplitPoint,
				                out startDangle, out endDangle);

				if (! CanReshapeOpenJaw(startDangle, endDangle, reshapePath))
				{
					return null;
				}

				useFromPoint = ReplaceFromPointInMultiIntersectionOpenJaw(pathToReshape,
				                                                          firstSplitPoint,
				                                                          lastSplitPoint,
				                                                          startDangle,
				                                                          endDangle);
			}

			return useFromPoint
				       ? pathToReshape.FromPoint
				       : pathToReshape.ToPoint;
		}

		[CanBeNull]
		public static IPath GetTwoPolygonOpenJawReshapeSegments(
			[NotNull] IPath reshapePath,
			[NotNull] IPoint betweenTargetIntersectionPoint,
			[NotNull] IPolygon andPolygonToReshape)
		{
			// re-clone to avoid cutting the reshape path
			var highLevelReshapePath =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(reshapePath);

			var intersectionPoints =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					andPolygonToReshape, highLevelReshapePath);

			if (intersectionPoints.PointCount == 0)
			{
				return null;
			}

			// the target intersection point is expected on one of the dangling lines before the first or after the last split point
			// -> add the target intersection points to the intersection points to get the actual segments between the last polygon
			// intersection and the target intersection point
			object missing = Type.Missing;
			intersectionPoints.AddPoint(betweenTargetIntersectionPoint, ref missing,
			                            ref missing);

			IPoint firstSplitPoint, lastSplitPoint;
			IGeometryCollection splittedReshapeLine = SplitReshapePath(
				highLevelReshapePath, intersectionPoints, out firstSplitPoint,
				out lastSplitPoint);

			IPath startDangle, endDangle;
			TrimReshapePath(reshapePath, splittedReshapeLine, firstSplitPoint,
			                lastSplitPoint,
			                out startDangle, out endDangle);

			foreach (IPath path in GeometryUtils.GetPaths((IGeometry) splittedReshapeLine)
			)
			{
				IGeometry highLevelPath = GeometryUtils.GetHighLevelGeometry(path, true);

				if (GeometryUtils.Intersects(highLevelPath,
				                             betweenTargetIntersectionPoint) &&
				    GeometryUtils.Intersects(highLevelPath, andPolygonToReshape))
				{
					return path;
				}
			}

			Assert.CantReach(
				"Target intersection point does not intersect the used reshape path.");

			return null;
		}

		private static bool ReshapePolylinePart([NotNull] ReshapeInfo reshapeInfo)
		{
			Assert.ArgumentNotNull(reshapeInfo, nameof(reshapeInfo));

			if (reshapeInfo.ReshapePath.IsClosed)
			{
				NotificationUtils.Add(reshapeInfo.Notifications,
				                      "Reshape line is closed. This is not supported for polyline reshape");
				return false;
			}

			var partToReshape = (IPath) reshapeInfo.GetGeometryPartToReshape();

			// remove the dangling bits of the reshapePath
			IPoint firstCutPoint, lastCutPoint;

			IPath trimmedReplacement = GetTrimmedReshapeLine(
				reshapeInfo, out firstCutPoint,
				out lastCutPoint);

			// add the available information to reshapeInfo for target point insertion
			reshapeInfo.CutReshapePath = new CutSubcurve(trimmedReplacement, true, true);

			reshapeInfo.ReplacedSegments =
				SegmentReplacementUtils.GetSegmentsBetween(firstCutPoint, lastCutPoint,
				                                           partToReshape);

			// Edge case: first cut point equals last cut point equals pathToReshape's from or to point
			//            this can happen in open-jaw reshape when snapping to the end for prolongation
			if (GeometryUtils.AreEqualInXY(firstCutPoint, lastCutPoint))
			{
				// asserts that they are connected at start/end points:
				SegmentReplacementUtils.JoinConnectedPaths(
					trimmedReplacement, partToReshape);
			}
			else
			{
				SegmentReplacementUtils.ReplaceSegments(partToReshape, trimmedReplacement,
				                                        firstCutPoint, lastCutPoint);
			}

			Marshal.ReleaseComObject(partToReshape);

			return true;
		}

		/// <summary>
		/// Trims the reshape line to make sure it touches the splitting curve (curve to be reshaped)
		/// at the ends (except if OpenJawReshapes are allowed in which case the line is only trimmed
		/// at the intersection-end. 
		/// NOTE: always returns a new geometry.
		/// </summary>
		/// <param name="reshapeInfo"></param>
		/// <param name="firstSplitPoint"></param>
		/// <param name="lastSplitPoint"></param>
		/// <returns></returns>
		[NotNull]
		private static IPath GetTrimmedReshapeLine([NotNull] ReshapeInfo reshapeInfo,
		                                           [NotNull] out IPoint firstSplitPoint,
		                                           [NotNull] out IPoint lastSplitPoint)
		{
			IGeometry highLevelCurveToSplit =
				GeometryUtils.GetHighLevelGeometry(reshapeInfo.ReshapePath);

			// It could be that the curve already fits. However the saving of time & memory
			// is more significant when re-using intersection points and thus not creating the 
			// high-level geometry and calculating the intersection points
			IPointCollection intersectionPoints = GetIntersectionPoints(reshapeInfo,
			                                                            highLevelCurveToSplit);

			// open-jaw reshape has single cut point:
			if (IsOpenJawReshapeAllowed(reshapeInfo) &&
			    intersectionPoints.PointCount == 1)
			{
				reshapeInfo.IsOpenJawReshape = true;

				return GetSingleCutReshapePath(
					reshapeInfo, (IPolyline) highLevelCurveToSplit, intersectionPoints,
					out firstSplitPoint, out lastSplitPoint);
			}

			IGeometryCollection splittedReshapeLine = SplitReshapePath(
				highLevelCurveToSplit, intersectionPoints, out firstSplitPoint,
				out lastSplitPoint);

			IPath reshapePath = reshapeInfo.ReshapePath;
			IPath trimmedLine;

			if (splittedReshapeLine.GeometryCount == 1)
			{
				// without intermediate splits, use the reshape path directly (but adjust orientation):
				trimmedLine = GeometryFactory.Clone(reshapePath);

				if (! GeometryUtils.AreEqualInXY(firstSplitPoint, trimmedLine.FromPoint))
				{
					trimmedLine.ReverseOrientation();
				}
			}
			else
			{
				// there are possibly dangling lines before the first and/or after the last split point
				IPath startDangle, endDangle;
				trimmedLine = TrimReshapePath(reshapePath, splittedReshapeLine,
				                              firstSplitPoint,
				                              lastSplitPoint, out startDangle,
				                              out endDangle);

				if (IsOpenJawReshapeAllowed(reshapeInfo) &&
				    CanReshapeOpenJaw(startDangle, endDangle, reshapePath))
				{
					reshapeInfo.IsOpenJawReshape = true;

					trimmedLine = AddOpenJawSection(
						(IPath) reshapeInfo.GetGeometryPartToReshape(),
						trimmedLine, startDangle, endDangle,
						ref firstSplitPoint, ref lastSplitPoint,
						reshapeInfo.Notifications);
				}
			}

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebugFormat(
					"The reshape path was split into {0} parts by the geometry to reshape, the trimmed reshape path's length is {1}",
					splittedReshapeLine.GeometryCount, trimmedLine.Length);
			}

			Marshal.ReleaseComObject(splittedReshapeLine);

			return trimmedLine;
		}

		private static bool IsOpenJawReshapeAllowed(ReshapeInfo reshapeInfo)
		{
			return reshapeInfo.AllowOpenJawReshape &&
			       reshapeInfo.GeometryToReshape.GeometryType ==
			       esriGeometryType.esriGeometryPolyline;
		}

		private static IPath AddOpenJawSection([NotNull] IPath pathToReshape,
		                                       [NotNull] IPath trimmedLine,
		                                       IPath startDangle, IPath endDangle,
		                                       ref IPoint firstSplitPoint,
		                                       ref IPoint lastSplitPoint,
		                                       NotificationCollection notifications)
		{
			IPath result;

			// only add start OR end section
			bool useStartDangle = UseStartDangleForOpenJawReshape(startDangle, endDangle);

			IPoint danglingReshapePathEnd, danglingReshapePathIntersection;
			if (useStartDangle)
			{
				Assert.NotNull(startDangle);

				// extract points before modifying the dangle:
				danglingReshapePathEnd = startDangle.FromPoint;
				danglingReshapePathIntersection = startDangle.ToPoint;

				// avoid sub-tolerance differences that result in multi-part result with non-network simplify
				trimmedLine.FromPoint = danglingReshapePathIntersection;
				result = startDangle;

				((ISegmentCollection) result).AddSegmentCollection(
					(ISegmentCollection) trimmedLine);
			}
			else
			{
				Assert.NotNull(endDangle);

				// extract points before modifying the dangle:
				danglingReshapePathIntersection = endDangle.FromPoint;
				danglingReshapePathEnd = endDangle.ToPoint;

				// avoid sub-tolerance differences that result in multi-part result with non-network simplify
				trimmedLine.ToPoint = danglingReshapePathIntersection;
				result = trimmedLine;

				((ISegmentCollection) result).AddSegmentCollection(
					(ISegmentCollection) endDangle);
			}

			// TOP-5072: Avoid phantom points between the normal reshape line and the dangle (consider getting it right when trimming by using GetSubCurve())
			IMultipoint potentialPhantomPoint =
				GeometryFactory.CreateMultipoint(new List<IPoint>
				                                 {danglingReshapePathIntersection});

			SegmentReplacementUtils.RemovePhantomPointInserts(
				result, (IPointCollection) potentialPhantomPoint, pathToReshape);

			// NOTE: with multiple-crossing-open-jaw not the pathToReshape's end point closer to the
			//       dangling end should be used but the one closer (along the line) to the used dangle's 
			//       intersection.
			bool fromPointIsCloser = IsFromPointCloserAlongLine(pathToReshape,
			                                                    danglingReshapePathIntersection,
			                                                    firstSplitPoint,
			                                                    lastSplitPoint);

			string distanceMessage = GetEndPointMovingMessage(pathToReshape,
			                                                  danglingReshapePathEnd,
			                                                  fromPointIsCloser);

			if (useStartDangle)
			{
				// replace the first split point with the closer end of the path to reshape
				firstSplitPoint = fromPointIsCloser
					                  ? pathToReshape.FromPoint
					                  : pathToReshape.ToPoint;
			}
			else
			{
				// replace the last split point with the respective original
				lastSplitPoint = fromPointIsCloser
					                 ? pathToReshape.FromPoint
					                 : pathToReshape.ToPoint;
			}

			NotificationUtils.Add(notifications, "Y-reshape: {0}", distanceMessage);

			return result;
		}

		private static bool UseStartDangleForOpenJawReshape(IPath startDangle,
		                                                    IPath endDangle)
		{
			double startDangleLength = startDangle?.Length ?? 0;

			return (! CanUseForOpenJaw(endDangle)) ||
			       endDangle.Length < startDangleLength;
		}

		private static bool ReplaceFromPointInSingleCutOpenJaw(
			[NotNull] IPath pathToReshape,
			[NotNull] IPath singleCutReshapePath,
			[NotNull] IPoint splitPoint,
			bool tryNonDefaultSide,
			out string message)
		{
			IPoint danglingReshapePathEnd =
				GeometryUtils.AreEqualInXY(singleCutReshapePath.FromPoint, splitPoint)
					? singleCutReshapePath.ToPoint
					: singleCutReshapePath.FromPoint;

			// Conflicting issues (TOP-4584 / TOP-4692):
			// TOP-4584:
			// if it's a prolongation at the path-to-reshape's start or end, use the respective point
			// (even if the reshape path goes back to the other end)
			// TOP-4692:
			// Y-Reshape from end point: Apply same logic as in normal case and allow flip
			// Current implementation: TOP-4692 (same as normal case)

			// use the point that is closer to the 'dangling' end of the reshape path
			bool result = DetermineFromPointIsCloser(
				pathToReshape, danglingReshapePathEnd, out message);

			if (tryNonDefaultSide)
			{
				result = ! result;

				message += ". Reshaped polyline at non-default side";
			}

			return result;
		}

		/// <summary>
		/// Determine if pointOnLine is closest to the pathToReshape.FromPoint compared to the other split point
		/// </summary>
		/// <param name="pathToReshape"></param>
		/// <param name="pointOnLine"></param>
		/// <param name="firstSplitPoint"></param>
		/// <param name="lastSplitPoint"></param>
		/// <returns></returns>
		private static bool IsFromPointCloserAlongLine(IPath pathToReshape,
		                                               IPoint pointOnLine,
		                                               IPoint firstSplitPoint,
		                                               IPoint lastSplitPoint)
		{
			double pointDistanceToFrom = GeometryUtils.GetDistanceAlongCurve(
				pathToReshape,
				pointOnLine,
				false);

			double otherPointDistanceFrom;
			if (GeometryUtils.AreEqualInXY(pointOnLine, firstSplitPoint))
			{
				otherPointDistanceFrom = GeometryUtils.GetDistanceAlongCurve(
					pathToReshape,
					lastSplitPoint, false);
			}
			else if (GeometryUtils.AreEqualInXY(pointOnLine, lastSplitPoint))
			{
				otherPointDistanceFrom = GeometryUtils.GetDistanceAlongCurve(
					pathToReshape,
					firstSplitPoint,
					false);
			}
			else
			{
				// most likely the reshape-line is funny, i.e. does not cut the path to reshape in sequential order
				// -> should probably not support open-jaw? -> take closer end point
				otherPointDistanceFrom = pathToReshape.Length - pointDistanceToFrom;
			}

			return pointDistanceToFrom < otherPointDistanceFrom;
		}

		private static bool IsFromPointCloser(IPath path, IPoint point)
		{
			double fromPointDistance = GeometryUtils.GetPointDistance(
				path.FromPoint, point);

			double toPointDistance = GeometryUtils.GetPointDistance(
				path.ToPoint, point);

			return fromPointDistance < toPointDistance;
		}

		private static bool DetermineFromPointIsCloser(IPath pathToReshape,
		                                               IPoint toDanglingReshapePathEnd,
		                                               out string distanceMessage)
		{
			bool fromPointIsCloser =
				IsFromPointCloser(pathToReshape, toDanglingReshapePathEnd);

			distanceMessage = GetEndPointMovingMessage(
				pathToReshape, toDanglingReshapePathEnd,
				fromPointIsCloser);

			return fromPointIsCloser;
		}

		private static string GetEndPointMovingMessage(IPath pathToReshape,
		                                               IPoint toDanglingReshapePathEnd,
		                                               bool replaceFromPoint)
		{
			string distanceMessage;

			const int significantDigits = 3;

			if (replaceFromPoint)
			{
				double fromPointDistance = GeometryUtils.GetPointDistance(
					pathToReshape.FromPoint, toDanglingReshapePathEnd);

				// source from point is closer
				distanceMessage =
					string.Format("The line's start point is moved by {0} <map units>",
					              MathUtils.RoundToSignificantDigits(fromPointDistance,
					                                                 significantDigits));
			}
			else
			{
				double toPointDistance = GeometryUtils.GetPointDistance(
					pathToReshape.ToPoint, toDanglingReshapePathEnd);

				distanceMessage =
					string.Format("The line's end point is moved by {0} <map units>",
					              MathUtils.RoundToSignificantDigits(toPointDistance,
					                                                 significantDigits));
			}

			return distanceMessage;
		}

		private static bool CanReshapeOpenJaw([CanBeNull] IPath startDangle,
		                                      [CanBeNull] IPath endDangle,
		                                      [NotNull] IPath reshapePath)
		{
			// Assumption: if the first/last segment crosses the geometry to reshape
			// the user does not want to open-jaw reshape
			// Correction (Deborah): if the user snaps on one end of the sketch and does
			// not snap on the other she wants an open-jaw reshape in even if only the last 
			// segment crosses the original line
			// -> new assumption: The users consistently snap or do not snap, if Y-reshape is on.
			bool canUseStart = CanUseForOpenJaw(startDangle);
			bool canUseEnd = CanUseForOpenJaw(endDangle);

			if (canUseStart && canUseEnd)
			{
				// If the user works without snapping and no Y-reshape is intended, only the last segments cross,
				// i.e. the last segments are not fully part of the dangle. We cannot just use the point count on 
				// the dangle because the dangles always start/end on the polyline, regardless whether it was 
				// snapped or not.
				var reshapePathSegments = (ISegmentCollection) reshapePath;

				bool endSegmentIsDangling = IsSegmentPartOfDangle(
					reshapePathSegments, reshapePathSegments.SegmentCount - 1, endDangle);

				bool firstSegmentIsDangling = IsSegmentPartOfDangle(
					reshapePathSegments, 0, startDangle);

				return firstSegmentIsDangling || endSegmentIsDangling;
			}

			return canUseStart || canUseEnd;
		}

		private static bool IsSegmentPartOfDangle([NotNull] ISegmentCollection segments,
		                                          int segmentIndex,
		                                          [CanBeNull] IPath dangle)
		{
			if (dangle == null)
			{
				return false;
			}

			double resolution = GeometryUtils.GetXyResolution((IGeometry) segments);

			ISegment segment = segments.Segment[segmentIndex];

			return segment.Length <= dangle.Length + resolution;
		}

		private static bool CanUseForOpenJaw(IPath dangle)
		{
			return dangle != null && ((IPointCollection) dangle).PointCount > 1;
		}

		[NotNull]
		private static IPath TrimReshapePath(
			IPath reshapePath, [NotNull] IGeometryCollection splittedReshapePath,
			[NotNull] IPoint firstSplitPoint, [NotNull] IPoint lastSplitPoint,
			[CanBeNull] out IPath startDangle, [CanBeNull] out IPath endDangle)
		{
			IPath trimmedLine;
			var useSubcurve = false;
			double firstPointDistance, lastPointDistance;

			if (GeometryUtils.AreEqualInXY(firstSplitPoint, reshapePath.FromPoint) ||
			    GeometryUtils.AreEqualInXY(firstSplitPoint, reshapePath.ToPoint))
			{
				firstPointDistance = 0;
				startDangle = null;
			}
			else
			{
				useSubcurve = true;
				startDangle = (IPath) splittedReshapePath.Geometry[0];
				firstPointDistance = startDangle.Length;
			}

			if (GeometryUtils.AreEqualInXY(lastSplitPoint, reshapePath.FromPoint) ||
			    GeometryUtils.AreEqualInXY(lastSplitPoint, reshapePath.ToPoint))
			{
				lastPointDistance = reshapePath.Length;
				endDangle = null;
			}
			else
			{
				useSubcurve = true;
				endDangle =
					(IPath) splittedReshapePath.Geometry[
						splittedReshapePath.GeometryCount - 1];
				lastPointDistance = reshapePath.Length - endDangle.Length;
			}

			if (useSubcurve)
			{
				// NOTE: For multipatch reshapes (Z values of the reshape line are ignored)
				//       create a vertical line connecting the first and the last split point

				ICurve subCurve;
				reshapePath.GetSubcurve(firstPointDistance, lastPointDistance, false,
				                        out subCurve);
				trimmedLine = (IPath) subCurve;

				if (subCurve.IsEmpty &&
				    ! MathUtils.AreEqual(firstSplitPoint.Z, lastSplitPoint.Z))
				{
					// Create a vertical reshape path
					trimmedLine.FromPoint = firstSplitPoint;
					trimmedLine.ToPoint = lastSplitPoint;
					trimmedLine.SpatialReference = firstSplitPoint.SpatialReference;
					GeometryUtils.MakeZAware(trimmedLine);
				}
			}
			else
			{
				trimmedLine = GeometryFactory.Clone(reshapePath);

				if (! GeometryUtils.AreEqualInXY(firstSplitPoint, trimmedLine.FromPoint))
				{
					trimmedLine.ReverseOrientation();
				}
			}

			return trimmedLine;
		}

		/// <summary>
		/// Splits the reshape path at the specified intersection points
		/// (i.e intersections with the GeometryPartToReshape).
		/// </summary>
		/// <param name="highLevelReshapePath"></param>
		/// <param name="intersectionPoints"></param>
		/// <param name="firstSplitPoint"></param>
		/// <param name="lastSplitPoint"></param>
		/// <returns></returns>
		[NotNull]
		private static IGeometryCollection SplitReshapePath(
			[NotNull] IGeometry highLevelReshapePath,
			[NotNull] IPointCollection intersectionPoints,
			[NotNull] out IPoint firstSplitPoint,
			[NotNull] out IPoint lastSplitPoint)
		{
			Assert.True(intersectionPoints.PointCount > 1,
			            "splittingCurve must intersect pathToSplit at least twice");

			Stopwatch watch = _msg.DebugStartTiming();

			IList<IPoint> splitPoints =
				GeometryUtils.SplitPolycurve(
					(IPolycurve) highLevelReshapePath, intersectionPoints, false, true,
					GeometryUtils.GetXyTolerance(highLevelReshapePath));

			_msg.DebugStopTiming(watch,
			                     "Splitted reshape path at {0} intersections with geometry part to reshape.",
			                     splitPoints.Count);

			Assert.True(splitPoints.Count >= 2,
			            "Reshape path was not split at least twice at intersections with curve to reshape");

			firstSplitPoint = splitPoints[0];
			lastSplitPoint = splitPoints[splitPoints.Count - 1];

			return (IGeometryCollection) highLevelReshapePath;
		}

		private static IPointCollection GetIntersectionPoints(ReshapeInfo reshapeInfo,
		                                                      IGeometry
			                                                      highLevelReshapePath)
		{
			// calculating the intersection is very expensive in terms of time and memory

			IPointCollection intersectionPoints;
			if (reshapeInfo.IntersectionPoints != null)
			{
				intersectionPoints = reshapeInfo.IntersectionPoints;
			}
			else
			{
				IGeometry partToReshape = reshapeInfo.GetGeometryPartToReshape();
				IPolyline highLevelSplittingCurve =
					GeometryFactory.CreatePolyline(partToReshape);

				intersectionPoints =
					(IPointCollection) IntersectionUtils.GetIntersectionPoints(
						highLevelReshapePath, highLevelSplittingCurve, true);

				reshapeInfo.IntersectionPoints = intersectionPoints;

				Marshal.ReleaseComObject(partToReshape);
				Marshal.ReleaseComObject(highLevelSplittingCurve);
			}

			return intersectionPoints;
		}

		private static bool ReplaceFromPointInMultiIntersectionOpenJaw(
			IPath pathToReshape,
			IPoint
				firstSplitPoint,
			IPoint lastSplitPoint,
			IPath startDangle,
			IPath endDangle)
		{
			bool useStartDangle = UseStartDangleForOpenJawReshape(startDangle, endDangle);

			IPoint danglingReshapePathIntersection;
			if (useStartDangle)
			{
				Assert.NotNull(startDangle);
				danglingReshapePathIntersection = startDangle.ToPoint;
			}
			else
			{
				Assert.NotNull(endDangle);
				danglingReshapePathIntersection = endDangle.FromPoint;
			}

			bool useFromPoint = IsFromPointCloserAlongLine(
				pathToReshape, danglingReshapePathIntersection, firstSplitPoint,
				lastSplitPoint);

			return useFromPoint;
		}

		private static IPath GetSingleCutReshapePath(
			[NotNull] ReshapeInfo reshapeInfo,
			[NotNull] IPolyline highLevelCurveToSplit,
			[NotNull] IPointCollection intersectionPoints,
			[NotNull] out IPoint firstSplitPoint,
			[NotNull] out IPoint lastSplitPoint)
		{
			Assert.ArgumentCondition(reshapeInfo.GeometryToReshape is IPolyline,
			                         "Open-Jaw reshapes only supported by polylines");

			IPoint splitPoint;
			IPath singleCutReshapePath = GetSingleCutReshapePath(highLevelCurveToSplit,
			                                                     intersectionPoints,
			                                                     out splitPoint);

			// determine the split points, in this case one 'split' point is the path-to-reshape-end-point
			DetermineSingleCutSplitPoints(reshapeInfo, singleCutReshapePath, splitPoint,
			                              out firstSplitPoint, out lastSplitPoint);

			return singleCutReshapePath;
		}

		private static IPath GetSingleCutReshapePath(IPolyline highLevelCurveToSplit,
		                                             IPointCollection intersectionPoints,
		                                             out IPoint splitPoint)
		{
			const bool projectPointsOntoPathToSplit = false;
			const bool createParts = true;

			IList<IPoint> splitPoints =
				GeometryUtils.CrackPolycurve(highLevelCurveToSplit, intersectionPoints,
				                             projectPointsOntoPathToSplit, createParts,
				                             GeometryUtils.GetXyTolerance(
					                             highLevelCurveToSplit));

			Assert.True(splitPoints.Count == 1,
			            "Unexpected nuber of split points: {0}", splitPoints.Count);

			splitPoint = splitPoints[0];

			// the cut reshape line:
			var splitResult = (IGeometryCollection) highLevelCurveToSplit;

			if (splitResult.GeometryCount != 1)
			{
				Assert.AreEqual(2, splitResult.GeometryCount,
				                "Unexpected number of split result lines in single-cut reshape");

				// return the longer part if the reshape line cuts across the polyline to reshape
				double part0Length = ((IPath) splitResult.Geometry[0]).Length;
				double part1Length = ((IPath) splitResult.Geometry[1]).Length;
				List<int> removePart = (part0Length < part1Length)
					                       ? new List<int> {0}
					                       : new List<int> {1};

				GeometryUtils.RemoveParts(splitResult, removePart);
				splitResult.GeometriesChanged();
			}

			return (IPath) splitResult.Geometry[0];
		}

		private static void DetermineSingleCutSplitPoints(
			[NotNull] ReshapeInfo reshapeInfo,
			[NotNull] IPath singleCutReshapePath,
			[NotNull] IPoint splitPoint,
			out IPoint firstSplitPoint,
			out IPoint lastSplitPoint)
		{
			var pathToReshape = (IPath) reshapeInfo.GetGeometryPartToReshape();

			string distanceMessage;

			bool useFromPoint = ReplaceFromPointInSingleCutOpenJaw(
				pathToReshape, singleCutReshapePath, splitPoint,
				reshapeInfo.ReshapeResultFilter.UseNonDefaultReshapeSide,
				out distanceMessage);

			if (useFromPoint)
			{
				// source from point is closer
				firstSplitPoint = pathToReshape.FromPoint;
				lastSplitPoint = splitPoint;
			}
			else
			{
				firstSplitPoint = splitPoint;
				lastSplitPoint = pathToReshape.ToPoint;
			}

			// TODO: adapt message / change when tryNonDefaultSide
			NotificationUtils.Add(reshapeInfo.Notifications, "Y-reshape: {0}",
			                      distanceMessage);
		}

		#endregion

		public static IDictionary<IPoint, IPoint> PairByDistance(
			[NotNull] IPointCollection sourcePoints,
			[NotNull] IPointCollection targetPoints)
		{
			IPointCollection unpairedSourcePoints;

			IDictionary<IPoint, IPoint> result = PairByDistance(
				sourcePoints, targetPoints,
				out unpairedSourcePoints);

			if (unpairedSourcePoints != null)
			{
				Marshal.ReleaseComObject(unpairedSourcePoints);
			}

			return result;
		}

		public static IDictionary<IPoint, IPoint> PairByDistance(
			[NotNull] IPointCollection sourcePoints,
			[NotNull] IPointCollection targetPoints,
			[CanBeNull] out IPointCollection unpairedSourcePoints)
		{
			var sourceTargetPairs = new Dictionary<IPoint, IPoint>();
			unpairedSourcePoints = null;

			// Now pair them
			if (sourcePoints.PointCount <= targetPoints.PointCount)
			{
				// Matching point counts or too many target intersection points defined
				var unpairedTargetPoints =
					(IPointCollection) GeometryFactory.Clone((IGeometry) targetPoints);

				foreach (IPoint sourcePoint in GeometryUtils.GetPoints(sourcePoints))
				{
					sourceTargetPairs.Add(sourcePoint,
					                      RemoveNearestPoint(
						                      unpairedTargetPoints, sourcePoint));
				}

				Marshal.ReleaseComObject(unpairedTargetPoints);
			}
			else if (targetPoints.PointCount < sourcePoints.PointCount)
			{
				// Too few target intersection points defined

				unpairedSourcePoints =
					(IPointCollection) GeometryFactory.Clone((IGeometry) sourcePoints);

				foreach (IPoint targetPoint in GeometryUtils.GetPoints(targetPoints))
				{
					sourceTargetPairs.Add(
						RemoveNearestPoint(unpairedSourcePoints, targetPoint),
						targetPoint);
				}
			}

			return sourceTargetPairs;
		}

		public static double GetAreaOrLength(IGeometry geometry)
		{
			var area = geometry as IArea;

			double result = area?.Area ?? GeometryUtils.GetLength(geometry);

			return result;
		}

		[CanBeNull]
		public static IPolygon CreateUnionPolygon(
			[NotNull] IList<IGeometry> targetGeometries,
			bool useMinimumTolerance)
		{
			// TODO: Test performance with large polygons, consider clipping to extent

			// Use polygon and multipatch boundaries as target areas
			List<IGeometry> targetPolygons =
				targetGeometries.OfType<IPolygon>().Cast<IGeometry>().ToList();

			IEnumerable<IMultiPatch> onlyMultipatches =
				targetGeometries.OfType<IMultiPatch>();

			targetPolygons.AddRange(
				onlyMultipatches.Select(GeometryFactory.CreatePolygon).Cast<IGeometry>());

			if (targetPolygons.Count == 0)
			{
				return null;
			}

			IPolygon result;

			// Always clone the target shape, it could be projected!
			if (targetPolygons.Count == 1)
			{
				result = (IPolygon) GeometryFactory.Clone(targetPolygons[0]);
			}
			else
			{
				result = GeometryUtils.UnionGeometries(targetPolygons) as IPolygon;
			}

			foreach (IGeometry targetGeometry in targetGeometries)
			{
				Marshal.ReleaseComObject(targetGeometry);
			}

			if (result != null && useMinimumTolerance)
			{
				GeometryUtils.SetMinimumXyTolerance(result);
			}

			return result;
		}

		public static bool ResultsInOverlapWithTarget([NotNull] ReshapeInfo reshapeInfo,
		                                              [NotNull] IGeometry proposal,
		                                              [CanBeNull] IPolygon target)
		{
			// so far only used to exclude overlaps with target:
			if (reshapeInfo.GeometryToReshape.GeometryType !=
			    esriGeometryType.esriGeometryPolygon)
			{
				return false;
			}

			if (target != null)
			{
				if (! GeometryUtils.InteriorIntersects(proposal, target))
				{
					return false;
				}

				// Reshape one out of several reshape lines: the result might still intersect 
				// the target at another location than this reshape line.

				// To find out whether this particular reshape is 'good' the reshape line must be 'between' 
				// the resulting source and the target, i.e. interior-intersect the new source-target union

				var proposalSourceTargetUnion =
					(IPolygon) GeometryUtils.Union(proposal, target);

				IGeometry highLevelReshapePath =
					GeometryUtils.GetHighLevelGeometry(reshapeInfo.ReshapePath, true);

				bool intersectionWasCutBack =
					GeometryUtils.InteriorIntersects(highLevelReshapePath,
					                                 proposalSourceTargetUnion);

				Marshal.ReleaseComObject(highLevelReshapePath);

				// Invert logic for inner rings -> the hole should remain on top of the target!
				var ringToReshape = (IRing) reshapeInfo.GetGeometryPartToReshape();

				return ringToReshape.IsExterior
					       ? ! intersectionWasCutBack
					       : intersectionWasCutBack;
			}

			return false;
		}

		private static IPoint RemoveNearestPoint(IPointCollection inPointCollection,
		                                         IPoint searchPoint)
		{
			int nearestIndex = -1;
			double nearestDistance = double.MaxValue;
			for (var i = 0; i < inPointCollection.PointCount; i++)
			{
				double pointDistance =
					GeometryUtils.GetPointDistance(inPointCollection.get_Point(i),
					                               searchPoint);

				if (pointDistance < nearestDistance)
				{
					nearestIndex = i;
					nearestDistance = pointDistance;
				}
			}

			IPoint nearestPoint = inPointCollection.get_Point(nearestIndex);

			inPointCollection.RemovePoints(nearestIndex, 1);

			return nearestPoint;
		}
	}
}

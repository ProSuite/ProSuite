using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class StickyIntersectionsMultiplePolygonReshaper
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Dictionary<IGeometry, IGeometry> _reshapeGeometryCloneByOriginal;

		private readonly Dictionary<IGeometry, IList<ReshapeInfo>> _individualReshapes;

		[NotNull] private readonly StickyIntersections _stickyIntersections;

		private List<KeyValuePair<IPoint, ISegment>> _stitchPoints;

		public StickyIntersectionsMultiplePolygonReshaper(
			[NotNull] Dictionary<IGeometry, IGeometry> reshapeGeometryCloneByOriginal,
			[NotNull] Dictionary<IGeometry, IList<ReshapeInfo>> individualReshapes,
			[NotNull] StickyIntersections stickyIntersections)
		{
			_reshapeGeometryCloneByOriginal = reshapeGeometryCloneByOriginal;
			_individualReshapes = individualReshapes;
			_stickyIntersections = stickyIntersections;

			UsedTargetIntersections = new List<IPoint>();
		}

		public IEnvelope RefreshArea { get; set; }

		public bool AddAutomaticSourceTargetPairs { get; set; }

		public IList<IPoint> UsedTargetIntersections { get; private set; }

		/// <summary>
		/// Reshapes several polygons or several polylines along the specified reshape path.
		/// For best results the source geometries should be planar, i.e. have no interior 
		/// intersections.
		/// </summary>
		/// <param name="reshapePath"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[NotNull]
		public IDictionary<IGeometry, NotificationCollection>
			ReshapeGeometries(
				[NotNull] IPath reshapePath,
				[CanBeNull] NotificationCollection notifications)
		{
			IList<IGeometry> geometriesToReshape =
				_reshapeGeometryCloneByOriginal.Values.ToList();

			// TODO: out/ref parameter with non-reshapable geometries/notifications dictionary
			Assert.ArgumentCondition(geometriesToReshape.Count > 0,
			                         "No geometries to reshape provided.");

			IDictionary<IGeometry, NotificationCollection> reshapedGeometries =
				new Dictionary<IGeometry, NotificationCollection>();

			// General idea:
			// - Additionally to normal reshapes the intersection points of the sources
			//   are connected to the defined target intersections (the pairing is by distance)
			//   and thus additional reshapes are made possible, even if the source polygon
			//   is only cut once or never. Moreover, when a boundary is shortened (cut back)
			//   by the original (individual) reshape, the intersection point is moved to the target
			//   intersection point (similar to multi-reshape for lines when a joining road is cut back)
			//   The cut-back logic is further enhanced for cases at the outer boundary of the 
			//   selected polygons to allow 'cutting back' even if no regular reshape happened,
			//   because the first target intersection is inside the polygon.

			var highLevelReshapePath =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(reshapePath);

			IPointCollection sketchOriginalIntersectionPoints = Assert.NotNull(
				GetSketchIntersectionPointsWithOriginalGeometries(geometriesToReshape,
				                                                  highLevelReshapePath));

			IList<KeyValuePair<IPoint, IPoint>> sourceTargetPairs =
				AddAutomaticSourceTargetPairs
					? _stickyIntersections.AddAutomaticSourceTargetPairs(reshapePath,
					                                                     geometriesToReshape,
					                                                     sketchOriginalIntersectionPoints)
					: _stickyIntersections.SourceTargetPairs;

			var connectLinesAtCutOffs = new Dictionary<AdjustedCutSubcurve, IGeometry>();

			foreach (
				KeyValuePair<IGeometry, IGeometry> keyValuePair in _reshapeGeometryCloneByOriginal
			)
			{
				// Add the source-target connection to the reshape path for each feature that intersects a source point
				// and the target point is on an open jaw (dangling) part of the sketch, i.e does not re-connect 
				// back to the source. For each usable piece of the split reshape path, create an AdjustCurve.

				// Work-around: re-snap after each feature - ITopologicalOperator.Intersect seems to change the
				// topo-op geometry in a way that makes Disjoint() lie except it's snapped to the spatial ref
				highLevelReshapePath.SnapToSpatialReference();

				IGeometry originalGeometry = keyValuePair.Key;
				IGeometry geometryToReshape = keyValuePair.Value;

				List<KeyValuePair<IPoint, IPoint>> intersectingSourceTargetPoints =
					sourceTargetPairs.Where(
						sourceTargetPair =>
							CanUseSourceTargetPair(
								sourceTargetPair, highLevelReshapePath,
								originalGeometry)).ToList();

				// Get the reshape line split at all source intersection points and on all intersections with any original geometry
				IPolyline splitReshapeLine = GetSplitReshapePath(reshapePath,
				                                                 intersectingSourceTargetPoints,
				                                                 sketchOriginalIntersectionPoints);

				// 1. Calculate the subcurves that reshape in a way that connects the source and the target intersection point
				//    (open-jaw style)
				StickyIntersectionConnectLineCalculator connectLineCalculator;
				IEnumerable<AdjustedCutSubcurve> adjustCurves =
					CalculateSourceTargetPointAdjustSubcurves(
						geometryToReshape, highLevelReshapePath,
						splitReshapeLine,
						intersectingSourceTargetPoints,
						out connectLineCalculator);

				// Apply the adjust curves
				foreach (AdjustedCutSubcurve adjustedCutSubcurve in adjustCurves)
				{
					bool reshaped = ApplyReshapePath(geometryToReshape, adjustedCutSubcurve,
					                                 notifications, reshapedGeometries);

					if (reshaped)
					{
						foreach (IPoint point in
							adjustedCutSubcurve.GetPotentialTargetInsertPoints())
						{
							AddUsedTargetIntersection(point, sourceTargetPairs);
						}
					}
				}

				// Add potential connect lines to target points at cut-backs
				AddCutBackBoundariesConnectLines(geometryToReshape, connectLineCalculator,
				                                 splitReshapeLine,
				                                 connectLinesAtCutOffs);

				connectLineCalculator.Dispose();
			}

			// apply the relevant connect lines at the outer boundary (2-Ländereck) (no shared boundary cut back)
			ApplySharedBoundaryOuterEndReshape(connectLinesAtCutOffs, reshapedGeometries,
			                                   notifications);

			// apply the relevant connect lines at shared boundary cut-backs
			ApplySharedBoundaryCutBackConnectAtTriangleIntersections(
				connectLinesAtCutOffs, sourceTargetPairs, reshapedGeometries, notifications);

			RemoveStitchPoints(geometriesToReshape);

			return reshapedGeometries;
		}

		private void AddUsedTargetIntersection(
			IPoint candidate,
			IEnumerable<KeyValuePair<IPoint, IPoint>> sourceTargetPairs)
		{
			bool fromPointIsTarget = sourceTargetPairs.Any(
				sourceTargetPair =>
					GeometryUtils.AreEqualInXY(sourceTargetPair.Value, candidate));

			if (fromPointIsTarget)
			{
				UsedTargetIntersections.Add(candidate);
			}
		}

		private static IPolyline GetSplitReshapePath(
			[NotNull] IPath reshapePath,
			[NotNull] IEnumerable<KeyValuePair<IPoint, IPoint>> intersectingSourceTargetPoints,
			[NotNull] IPointCollection sketchOriginalIntersectionPoints)
		{
			var splitReshapeLine =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(reshapePath);

			var targetIntersectionPointCollection =
				(IPointCollection)
				GeometryFactory.CreateMultipoint(
					intersectingSourceTargetPoints.Select(sourceTargets => sourceTargets.Value));

			// additionally crack at the intersection points of the sketch with the feature
			targetIntersectionPointCollection.AddPointCollection(
				sketchOriginalIntersectionPoints);

			GeometryUtils.CrackPolycurve(splitReshapeLine,
			                             targetIntersectionPointCollection, false, true, null);
			return splitReshapeLine;
		}

		/// <summary>
		///  _____________
		///  |             |
		///  |             |
		///  |_____________|
		///  |  \   |  /   |
		///  |   \  | /    |
		///  |    o-|-     |
		///  |      |      |
		///  |      |      |
		///  |      |      |
		///  |______|___ __|
		///
		/// Reshape line: 
		///    \      /
		///     \    /
		///      o---
		/// Applies the relevant reshapes of the provided adjust curves at a cut-back of a shared boundary in a border
		/// triangle situation by connecting the last vertex before the cut-off (in this case the lower end of the 
		/// shared boundary between the two lower polygons) with the target intersection point.
		/// </summary>
		/// <param name="connectLinesAtCutOffs"></param>
		/// <param name="sourceTargetPairs"></param>
		/// <param name="reshapedGeometries"></param>
		/// <param name="notifications"></param>
		private void ApplySharedBoundaryCutBackConnectAtTriangleIntersections(
			Dictionary<AdjustedCutSubcurve, IGeometry> connectLinesAtCutOffs,
			IList<KeyValuePair<IPoint, IPoint>> sourceTargetPairs,
			IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			NotificationCollection notifications)
		{
			foreach (
				KeyValuePair<AdjustedCutSubcurve, AdjustedCutSubcurve> keyValuePair in
				CollectionUtils.GetAllTuples(connectLinesAtCutOffs.Keys))
			{
				AdjustedCutSubcurve adjustCurve1 = keyValuePair.Key;
				AdjustedCutSubcurve adjustCurve2 = keyValuePair.Value;

				IGeometry geometry1 = connectLinesAtCutOffs[adjustCurve1];
				IGeometry geometry2 = connectLinesAtCutOffs[adjustCurve2];

				// Two (different) neighbouring geometries 
				if (geometry1 == geometry2)
				{
					continue;
				}

				// must have the same connect line geometry
				IPath connectLine1 = adjustCurve1.ConnectLineAtFromPoint ??
				                     adjustCurve1.ConnectLineAtToPoint;
				IPath connectLine2 = adjustCurve2.ConnectLineAtFromPoint ??
				                     adjustCurve2.ConnectLineAtToPoint;

				// compare in xy only, they can be flipped
				IGeometry highLevelConnectLine1 = GeometryUtils.GetHighLevelGeometry(
					connectLine1, true);

				IGeometry highLevelConnectLine2 = GeometryUtils.GetHighLevelGeometry(
					connectLine2, true);

				if (! GeometryUtils.AreEqualInXY(highLevelConnectLine1, highLevelConnectLine2))
				{
					continue;
				}

				// The connect lines are equal. Now analyze the geometries with respect to the relevant source-target pair
				KeyValuePair<IPoint, IPoint> sourceTargetPair = sourceTargetPairs.FirstOrDefault(
					pair =>
						GeometryUtils.AreEqualInXY(connectLine1.ToPoint, pair.Value) ||
						GeometryUtils.AreEqualInXY(connectLine1.FromPoint, pair.Value));

				IPoint sourcePoint = sourceTargetPair.Key;

				if (sourcePoint == null)
				{
					continue;
				}

				// It must have been a shared-boundary cut-back, i.e. none of the geometries was extended 
				// and now contains the original source intersection point:
				if (GeometryUtils.Intersects(geometry1, sourcePoint) ||
				    GeometryUtils.Intersects(geometry2, sourcePoint))
				{
					continue;
				}

				// The cut back shared boundary must be *between* the two geometries
				// i.e. the following must be the case
				// - One geometry touches the target intersection point. The original geometry contained the point.
				// - AND the other geometry must not touch the target intersection point (but be disjoint). The original geometry is also disjoint.
				if (GeometryUtils.Touches(geometry1, sourceTargetPair.Value) ^
				    GeometryUtils.Touches(geometry2, sourceTargetPair.Value))
				{
					ApplyReshapePath(geometry1, adjustCurve1, notifications, reshapedGeometries);
					ApplyReshapePath(geometry2, adjustCurve2, notifications, reshapedGeometries);
				}
			}
		}

		/// <summary>
		///     o
		///   ___\_________
		///  |    \ |      |
		///  |     \|      |
		///  |      |      |
		///  |      |      |
		///  |      |      |
		///  |      |      |
		///  |______|___ __|
		///
		/// Reshape line: o
		///                 \
		///                  \
		/// Applies the relevant reshapes of the provided adjust curves at the outer boundary of two adjacent polygons
		/// by connecting the last vertex before the cut-off (in this case the top left corner of the left polygon)
		/// with the target intersection point.
		/// </summary>
		/// <param name="connectLinesAtCutOffs"></param>
		/// <param name="reshapedGeometries"></param>
		/// <param name="notifications"></param>
		private void ApplySharedBoundaryOuterEndReshape(
			Dictionary<AdjustedCutSubcurve, IGeometry> connectLinesAtCutOffs,
			IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			NotificationCollection notifications)
		{
			IList<IGeometry> allGeometriesToReshape =
				_reshapeGeometryCloneByOriginal.Values.ToList();

			foreach (
				KeyValuePair<AdjustedCutSubcurve, IGeometry> connectLinesAtCutOff in
				connectLinesAtCutOffs)
			{
				AdjustedCutSubcurve adjustedCurve = connectLinesAtCutOff.Key;
				IGeometry geometryToReshape = connectLinesAtCutOff.Value;

				IPath connectLine = adjustedCurve.ConnectLineAtFromPoint ??
				                    adjustedCurve.ConnectLineAtToPoint;

				IGeometry highLevelPathOnTarget =
					GeometryUtils.GetHighLevelGeometry(adjustedCurve.PathOnTarget, true);

				IPoint targetPointOnSketch =
					GeometryUtils.Touches(connectLine.FromPoint, highLevelPathOnTarget)
						? connectLine.FromPoint
						: connectLine.ToPoint;

				Marshal.ReleaseComObject(highLevelPathOnTarget);

				// it must connect to an actual target intersection point:
				if (! GeometryUtils.Intersects(
					    targetPointOnSketch,
					    (IGeometry) _stickyIntersections.GetTargetPointCollection()))
				{
					continue;
				}

				if (PointIntersectsBoundary(targetPointOnSketch, geometryToReshape))
				{
					// no need to connect, target point already on polygon boundary (it could only destroy a nicely reshaped geometry)
					continue;
				}

				var highLevelAdjustLine =
					(IPolyline) GeometryUtils.GetHighLevelGeometry(adjustedCurve.Path);

				var boundaryCutBackWithTargetPointOutsideCount = 0;

				foreach (IGeometry geometry in allGeometriesToReshape)
				{
					bool interiorIntersects = GeometryUtils.InteriorIntersects(geometry,
					                                                           highLevelAdjustLine);

					bool boundaryConnectWithTargetOutside =
						geometry == geometryToReshape &&
						! interiorIntersects &&
						! GeometryUtils.Intersects(targetPointOnSketch, geometry) &&
						BothEndsTouchGeometry(highLevelAdjustLine, (IPolygon) geometry);

					if (boundaryConnectWithTargetOutside)
					{
						boundaryCutBackWithTargetPointOutsideCount++;
					}
				}

				// To connect to a target intersection point at the outer boundary of a set of polygons it must be
				// 

				if (boundaryCutBackWithTargetPointOutsideCount == 1 &&
				    ! IsWithinAnyOriginalPolygon(targetPointOnSketch))
				{
					ApplyReshapePath(geometryToReshape, adjustedCurve.Path, notifications, null,
					                 reshapedGeometries);
				}
			}
		}

		private static bool PointIntersectsBoundary(IPoint point, IGeometry geometry)
		{
			// Getting boundary is much faster and uses less memory than GeometryUtils.CreatePolyline!
			IGeometry boundary = GeometryUtils.GetBoundary(geometry);

			bool result = GeometryUtils.Intersects(point, boundary);

			Marshal.ReleaseComObject(boundary);

			return result;
		}

		private IEnumerable<AdjustedCutSubcurve> CalculateSourceTargetPointAdjustSubcurves(
			IGeometry geometryToReshape, IPolyline highLevelReshapePath,
			IPolyline splitHighLevelReshapePath,
			List<KeyValuePair<IPoint, IPoint>> intersectingSourceTargetPoints,
			out StickyIntersectionConnectLineCalculator connectLineCalculator)
		{
			var polyToReshapeBoundary =
				(IPolyline) GeometryUtils.GetBoundary(geometryToReshape);

			connectLineCalculator =
				CreateStickyIntersectionsConnectLineCalculator(polyToReshapeBoundary,
				                                               highLevelReshapePath,
				                                               intersectingSourceTargetPoints);

			IPath usablePathOnTarget = null;
			IPath startConnectLine = null;

			var adjustCurves = new List<AdjustedCutSubcurve>();

			foreach (IPath pathOnTarget in GeometryUtils.GetPaths(splitHighLevelReshapePath))
			{
				if (HasLengthZero(pathOnTarget))
				{
					// duplicate split points result in 0-length parts
					continue;
				}

				if (usablePathOnTarget == null)
				{
					startConnectLine =
						connectLineCalculator.FindConnection(polyToReshapeBoundary,
						                                     pathOnTarget, true);
				}

				IPath endConnectLine =
					connectLineCalculator.FindConnection(polyToReshapeBoundary,
					                                     pathOnTarget, false);

				if (usablePathOnTarget != null)
				{
					// the last path on target did not properly connect back to source - add this part plus its end connect line
					// and add a stitch point between the paths on the target
					IPoint potentialStitchpoint = usablePathOnTarget.ToPoint;

					AddPotentialStitchPoint(highLevelReshapePath, potentialStitchpoint);

					((ISegmentCollection) usablePathOnTarget).AddSegmentCollection(
						(ISegmentCollection) pathOnTarget);
				}
				else
				{
					usablePathOnTarget = GeometryFactory.Clone(pathOnTarget);
				}

				if (startConnectLine == null)
				{
					// completely useless
					usablePathOnTarget = null;
					continue;
				}

				if (endConnectLine == null)
				{
					// try adding the next part of the sketch to the usable part
					continue;
				}

				if (HasLengthZero(startConnectLine) && HasLengthZero(endConnectLine))
				{
					// already reshaped normally, could result in invalid reshape line error!
					usablePathOnTarget = null;
					continue;
				}

				if (HasLengthZero(startConnectLine))
				{
					AddPotentialStitchPoint(highLevelReshapePath, usablePathOnTarget.FromPoint);
				}

				if (HasLengthZero(endConnectLine))
				{
					AddPotentialStitchPoint(highLevelReshapePath, usablePathOnTarget.ToPoint);
				}

				// Theoretically consecutive adjustCurves could be merged to reduce the amount of reshapes
				// but this results in more frequent cases of reshapes to the wrong side.
				adjustCurves.Add(
					new AdjustedCutSubcurve(usablePathOnTarget, startConnectLine, endConnectLine));

				usablePathOnTarget = null;
			}

			Marshal.ReleaseComObject(polyToReshapeBoundary);

			return adjustCurves;
		}

		[CanBeNull]
		private static IPointCollection GetSketchIntersectionPointsWithOriginalGeometries(
			[NotNull] IEnumerable<IGeometry> geometriesToReshape,
			[NotNull] IPolyline highLevelReshapePath)
		{
			IPointCollection sketchOriginalIntersectionPoints = null;

			foreach (IGeometry geometry in geometriesToReshape)
			{
				if (sketchOriginalIntersectionPoints == null)
				{
					sketchOriginalIntersectionPoints =
						(IPointCollection) IntersectionUtils.GetIntersectionPoints(
							highLevelReshapePath, geometry);
				}
				else
				{
					sketchOriginalIntersectionPoints.AddPointCollection(
						(IPointCollection) IntersectionUtils.GetIntersectionPoints(
							highLevelReshapePath, geometry));
				}
			}

			return sketchOriginalIntersectionPoints;
		}

		private void RemoveStitchPoints(IEnumerable<IGeometry> geometriesToReshape)
		{
			// TODO: maintain the stitch points by geometry to reshape
			if (_stitchPoints == null)
			{
				return;
			}

			foreach (IGeometry geometry in geometriesToReshape)
			{
				foreach (KeyValuePair<IPoint, ISegment> keyValuePair in _stitchPoints)
				{
					IPoint stitchPoint = keyValuePair.Key;
					ISegment replacementSegment = keyValuePair.Value;

					SegmentReplacementUtils.TryReplaceSegments((IPolycurve) geometry, stitchPoint,
					                                           replacementSegment);
				}
			}
		}

		private void AddPotentialStitchPoint(IPolyline highLevelReshapePath,
		                                     IPoint potentialStitchpoint)
		{
			int partIdx;
			double searchTolerance = GeometryUtils.GetXyTolerance(highLevelReshapePath);

			int? targetPointIdx = GeometryUtils.FindHitVertexIndex(
				highLevelReshapePath, potentialStitchpoint, searchTolerance, out partIdx);

			if (targetPointIdx == null)
			{
				if (_stitchPoints == null)
				{
					_stitchPoints = new List<KeyValuePair<IPoint, ISegment>>();
				}

				int? segmentIndex = GeometryUtils.FindHitSegmentIndex(
					highLevelReshapePath, potentialStitchpoint, searchTolerance, out partIdx);

				_stitchPoints.Add(new KeyValuePair<IPoint, ISegment>(potentialStitchpoint,
				                                                     GeometryUtils.GetSegment(
					                                                     (ISegmentCollection)
					                                                     highLevelReshapePath,
					                                                     partIdx,
					                                                     (int)
					                                                     Assert.NotNull(
						                                                     segmentIndex))));
			}
		}

		private bool IsWithinAnyOriginalPolygon(IPoint point)
		{
			foreach (IGeometry originalGeometry in _reshapeGeometryCloneByOriginal.Keys)
			{
				bool within = GeometryUtils.Contains(originalGeometry, point);

				if (within)
				{
					return true;
				}
			}

			return false;
		}

		private static bool BothEndsTouchGeometry([NotNull] ICurve curve,
		                                          [NotNull] IPolygon polygon)
		{
			// Sometimes touches results in the wrong result (Simplify seems to help)

			return Touches(curve.FromPoint, polygon) &&
			       Touches(curve.ToPoint, polygon);
		}

		private static bool Touches(IPoint point, IPolygon polygon)
		{
			bool result = GeometryUtils.Touches(point, polygon);

			if (! result)
			{
				IPoint nearPoint = new PointClass();
				double tolerance = GeometryUtils.GetXyTolerance(polygon);

				if (GeometryUtils.GetDistanceFromCurve(point, polygon, nearPoint) < tolerance)
				{
					// work around (not reproducible once the geometries are saved and re-read
					result = true;
				}
			}

			return result;
		}

		private StickyIntersectionConnectLineCalculator
			CreateStickyIntersectionsConnectLineCalculator(
				[NotNull] IPolyline geometryToReshapeAsPolyline,
				[NotNull] IPolyline highLevelReshapePath,
				[NotNull] List<KeyValuePair<IPoint, IPoint>> intersectingSourceTargetPoints)
		{
			var connectLineCalculator =
				new StickyIntersectionConnectLineCalculator(highLevelReshapePath,
				                                            geometryToReshapeAsPolyline,
				                                            intersectingSourceTargetPoints,
				                                            _individualReshapes)
				{
					GeometriesToReshape = _reshapeGeometryCloneByOriginal.Values
				};

			return connectLineCalculator;
		}

		private static bool CanUseSourceTargetPair(
			KeyValuePair<IPoint, IPoint> sourceTargetPair,
			IPolyline highLevelDanglingReshapeLine,
			IGeometry originalShape)
		{
			bool targetPointOnReshapePath = GeometryUtils.Intersects(sourceTargetPair.Value,
			                                                         highLevelDanglingReshapeLine);

			// NOTE: Corner points (<< tolerance outside the envelope) are sometimes considered disjoint
			// Most likely related to the issue in Repro_IRelationalOperatorDisjointIncorrectAfterOtherDisjoint()
			// TODO: generic fix in GeometryUtils.Disjoint? 
			originalShape.SnapToSpatialReference();

			bool sourcePointOnOriginalGeometry =
				GeometryUtils.Intersects(originalShape, sourceTargetPair.Key);

			return targetPointOnReshapePath && sourcePointOnOriginalGeometry;
		}

		private static bool HasLengthZero(ICurve curve)
		{
			return MathUtils.AreEqual(curve.Length, 0);
		}

		private void AddCutBackBoundariesConnectLines(
			IGeometry geometryToReshape,
			StickyIntersectionConnectLineCalculator connectLineCalculator,
			IPolyline splitReshapeLine,
			Dictionary<AdjustedCutSubcurve, IGeometry> toResult)
		{
			IPath usablePathOnTarget = null;

			foreach (IPath pathOnTarget in GeometryUtils.GetPaths(splitReshapeLine))
			{
				if (HasLengthZero(pathOnTarget))
				{
					// duplicate split points result in 0-length parts
					continue;
				}

				TryAddCutBackBoundaryConnectLine(geometryToReshape,
				                                 pathOnTarget, connectLineCalculator, true,
				                                 toResult);

				TryAddCutBackBoundaryConnectLine(geometryToReshape,
				                                 pathOnTarget, connectLineCalculator, false,
				                                 toResult);

				if (usablePathOnTarget == null)
				{
					usablePathOnTarget = GeometryFactory.Clone(pathOnTarget);
				}
				else
				{
					// usablePathOnTarget != null:
					// the last path on target did not properly connect back to source - add this part plus its end connect line
					((ISegmentCollection) usablePathOnTarget).AddSegmentCollection(
						(ISegmentCollection) pathOnTarget);

					TryAddCutBackBoundaryConnectLine(geometryToReshape,
					                                 usablePathOnTarget, connectLineCalculator,
					                                 true,
					                                 toResult);

					TryAddCutBackBoundaryConnectLine(geometryToReshape,
					                                 usablePathOnTarget, connectLineCalculator,
					                                 false,
					                                 toResult);
				}

				// always calculate both ends, because it depends on the other end's position on the unreshaped from/to point!

				//IPath connectLineAtEnd;
				//connectLineCalculator.FindConnection(polylineToReshape,
				//									 usablePathOnTarget, false,
				//									 out connectLineAtEnd);

				IPoint sourcePoint;
				if (! connectLineCalculator.IsTargetIntersectionPoint(usablePathOnTarget.ToPoint,
				                                                      out sourcePoint))
				{
					// try adding the next part of the sketch to the usable part
					continue;
				}

				//if (connectLineAtStart != null && HasLengthZero(connectLineAtStart) &&
				//	HasLengthZero(connectLineAtEnd))
				//{
				//	//consolidatedPathsOnTarget.Add(usablePathOnTarget);

				//	// already reshaped normally, could result in invalid reshape line error!
				//	usablePathOnTarget = null;
				//	continue;
				//}

				//if (connectLineAtEnd != null)
				//{
				//	AddReshapeConnect(connectLineAtEnd, usablePathOnTarget, toResult,
				//					  geometryToReshape);
				//}

				usablePathOnTarget = null;
			}
		}

		private static void TryAddCutBackBoundaryConnectLine(
			IGeometry geometryToReshape,
			IPath usablePathOnTarget,
			StickyIntersectionConnectLineCalculator connectLineCalculator,
			bool searchForward,
			IDictionary<AdjustedCutSubcurve, IGeometry> toResult)
		{
			if (usablePathOnTarget == null)
			{
				return;
			}

			IPath connectLine =
				connectLineCalculator.FindConnectionAtBoundaryCutOff(usablePathOnTarget,
				                                                     searchForward);

			if (connectLine != null)
			{
				// TODO: zero-length checks
				AddReshapeConnect(connectLine, usablePathOnTarget,
				                  toResult, geometryToReshape);
			}
		}

		private static void AddReshapeConnect(IPath connectionLineAtCutOff,
		                                      IPath pathOnTarget,
		                                      IDictionary<AdjustedCutSubcurve, IGeometry>
			                                      toConnectPaths,
		                                      IGeometry geometryToReshape)
		{
			bool connectsAtFromPoint =
				GeometryUtils.AreEqualInXY(connectionLineAtCutOff.ToPoint, pathOnTarget.FromPoint);

			IPath connectLineAtFrom = connectsAtFromPoint
				                          ? connectionLineAtCutOff
				                          : null;

			IPath connectLineAtTo = connectsAtFromPoint
				                        ? null
				                        : connectionLineAtCutOff;

			var adjustedCutSubcurve = new AdjustedCutSubcurve(
				pathOnTarget, connectLineAtFrom, connectLineAtTo);

			//if (GeometryUtils.AreEqualInXY(connectionLineAtCutOff.FromPoint,
			//							   pathOnTarget.FromPoint))
			//{
			//	connectionLineAtCutOff.ReverseOrientation();
			//}

			//ISegmentCollection pathOnTargetClone =
			//	(ISegmentCollection) GeometryFactory.Clone(pathOnTarget);

			//((ISegmentCollection) connectionLineAtCutOff).AddSegmentCollection(
			//	pathOnTargetClone);

			toConnectPaths.Add(adjustedCutSubcurve, geometryToReshape);
		}

		private bool ApplyReshapePath(
			[NotNull] IGeometry geometryToReshape,
			[NotNull] AdjustedCutSubcurve adjustCurve,
			NotificationCollection notifications,
			IDictionary<IGeometry, NotificationCollection> reshapedGeometries)
		{
			// if the connect line crosses the geometry's boundary only reshape to the first intersection
			// and subsequently try the remaining bits. Otherwise this could result in a flipped geometry (reshape to the wrong side)

			IPath remainderAtFrom, remainderAtTo;

			IPath cutConnectLineAtFrom = SplitConnectline(adjustCurve, geometryToReshape, true,
			                                              out remainderAtFrom);
			IPath cutConnectLineAtTo = SplitConnectline(adjustCurve, geometryToReshape, false,
			                                            out remainderAtTo);

			var mainSubcurve = new AdjustedCutSubcurve(
				adjustCurve.PathOnTarget, cutConnectLineAtFrom,
				cutConnectLineAtTo);

			IPath reshapePath = mainSubcurve.Path;

			bool reshaped = Reshape(geometryToReshape, reshapePath);

			reshaped |= remainderAtFrom != null &&
			            Reshape(geometryToReshape, remainderAtFrom);

			reshaped |= remainderAtTo != null &&
			            Reshape(geometryToReshape, remainderAtTo);

			if (reshaped)
			{
				// to avoid incorrect relational operator results in for the next path on target
				((ISegmentCollection) geometryToReshape).SegmentsChanged();
			}

			NotificationCollection reshapeNotifications = null;
			// TODO: get from out ReshapeInfo;

			// move adding notfications to caller?
			if (reshaped && ! reshapedGeometries.ContainsKey(geometryToReshape))
			{
				reshapedGeometries.Add(geometryToReshape, reshapeNotifications);
			}
			else
			{
				if (reshapeNotifications != null)
				{
					NotificationUtils.Add(notifications, reshapeNotifications.Concatenate(" "));
				}
			}

			return reshaped;
		}

		private bool Reshape([NotNull] IGeometry geometryToReshape,
		                     [NotNull] IPath reshapePath)
		{
			ReshapeInfo reshapeInfo;
			bool reshaped = ReshapeUtils.ReshapeGeometry(geometryToReshape, reshapePath, false,
			                                             null, out reshapeInfo);

			if (reshaped)
			{
				AddToRefreshArea(reshapeInfo);
			}

			return reshaped;
		}

		private void AddToRefreshArea([CanBeNull] IGeometry geometry)
		{
			if (RefreshArea != null && geometry != null)
			{
				RefreshArea.Union(geometry.Envelope);
			}
		}

		protected void AddToRefreshArea(ReshapeInfo reshapeInfo)
		{
			AddToRefreshArea(reshapeInfo.ReshapePath);

			// using replaced segments is an important optimization, especially for large polygons
			AddToRefreshArea(reshapeInfo.ReplacedSegments ?? reshapeInfo.GeometryToReshape);
		}

		private static IPath SplitConnectline(AdjustedCutSubcurve adjustCurve,
		                                      IGeometry geometryToReshape, bool atFrom,
		                                      out IPath remainder)
		{
			IPath connectLine = atFrom
				                    ? adjustCurve.ConnectLineAtFromPoint
				                    : adjustCurve.ConnectLineAtToPoint;

			remainder = null;

			if (connectLine == null)
			{
				return null;
			}

			if (((IPointCollection) connectLine).PointCount > 2)
			{
				// it's along a real geometry
				return connectLine;
			}

			var highLevelConnectline =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(connectLine);

			IPointCollection crossingPoints = GetCrossingPoints(highLevelConnectline,
			                                                    geometryToReshape);

			if (crossingPoints.PointCount == 0)
			{
				return connectLine;
			}

			IPoint connectPoint = atFrom
				                      ? adjustCurve.PathOnTarget.FromPoint
				                      : adjustCurve.PathOnTarget.ToPoint;

			IPoint nearestPoint = ((IProximityOperator) crossingPoints).ReturnNearestPoint(
				connectPoint, esriSegmentExtension.esriNoExtension);

			bool splitHappened;
			int newPartIdx;
			int newSegmentIdx;
			highLevelConnectline.SplitAtPoint(nearestPoint, false, true,
			                                  out splitHappened, out newPartIdx,
			                                  out newSegmentIdx);

			var connectLineParts = ((IGeometryCollection) highLevelConnectline);

			IPath result;
			if (GeometryUtils.AreEqualInXY(highLevelConnectline.FromPoint, connectPoint))
			{
				result = (IPath) connectLineParts.get_Geometry(0);
				remainder = (IPath) connectLineParts.get_Geometry(1);
			}
			else
			{
				result = (IPath) connectLineParts.get_Geometry(1);
				remainder = (IPath) connectLineParts.get_Geometry(0);
			}

			return result;
		}

		/// <summary>
		/// Gets the intersection points (also including linear intersection end points) between crossingLine crosses the
		/// and polycurveToCross excluding start and end point of the crossingLine. This is not the same as interior intersection.
		/// </summary>
		/// <param name="crossingLine"></param>
		/// <param name="polycurveToCross"></param>
		/// <returns></returns>
		private static IPointCollection GetCrossingPoints([NotNull] ICurve crossingLine,
		                                                  [NotNull] IGeometry
			                                                  polycurveToCross)
		{
			// TODO: extracted from GetCrossingPointCount (various copies) -> move to ReshapeUtils

			IGeometry highLevelCrossingLine =
				GeometryUtils.GetHighLevelGeometry(crossingLine);

			// NOTE: IRelationalOperator.Crosses() does not find cases where the start point is tangential and
			//		 neither it finds cases where there is also a 1-dimensional intersection in addition to a point-intersection
			// NOTE: use GeometryUtils.GetIntersectionPoints to find also these cases where some intersections are 1-dimensional
			var intersectionPoints =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(polycurveToCross,
				                                        highLevelCrossingLine);

			// count the points excluding start and end point of the crossing line
			var result =
				(IPointCollection) GeometryFactory.CreateEmptyMultipoint(polycurveToCross);

			foreach (IPoint point in GeometryUtils.GetPoints(intersectionPoints))
			{
				if (GeometryUtils.AreEqualInXY(point, crossingLine.FromPoint) ||
				    GeometryUtils.AreEqualInXY(point, crossingLine.ToPoint))
				{
					continue;
				}

				result.AddPoint(point);
			}

			return result;
		}

		private bool ApplyReshapePath(IGeometry geometryToReshape, IPath reshapePath,
		                              NotificationCollection notifications,
		                              [CanBeNull] CutSubcurve cutSubcurve,
		                              IDictionary<IGeometry, NotificationCollection>
			                              reshapedGeometries)
		{
			var reshapeInfo =
				new ReshapeInfo(geometryToReshape, reshapePath,
				                notifications)
				{
					PartIndexToReshape = 0, // TODO
					CutReshapePath = cutSubcurve
				};

			// TODO: make ReshapeSingleGeometryInUnion not depend on unionReshapeInfo and use the same general workflow?

			bool reshaped = ReshapeSinglePolygonInUnion(reshapeInfo);

			if (reshaped)
			{
				// to avoid incorrect relational operator results in for the next path on target
				((ISegmentCollection) geometryToReshape).SegmentsChanged();

				AddToRefreshArea(reshapeInfo);
			}

			// move adding notfications to caller?
			if (reshaped && ! reshapedGeometries.ContainsKey(reshapeInfo.GeometryToReshape))
			{
				reshapedGeometries.Add(reshapeInfo.GeometryToReshape, reshapeInfo.Notifications);
			}
			else
			{
				if (reshapeInfo.Notifications != null)
				{
					NotificationUtils.Add(notifications,
					                      reshapeInfo.Notifications.Concatenate(" "));
				}
			}

			return reshaped;
		}

		private static bool ReshapeSinglePolygonInUnion(ReshapeInfo reshapeInfo)
		{
			bool reshaped;
			if (reshapeInfo.CutReshapePath != null)
			{
				if (reshapeInfo.CutReshapePath.Path.Length > 0)
				{
					// reshape the known polygon part with the known cut reshape path
					reshaped = ReshapeUtils.ReshapePolygonOrMultipatch(reshapeInfo);
				}
				else
				{
					_msg.DebugFormat(
						"No need to reshape geometry with 0-length reshape path at {0} | {1}",
						reshapeInfo.CutReshapePath.Path.FromPoint.X,
						reshapeInfo.CutReshapePath.Path.FromPoint.Y);
					reshaped = false;
				}
			}
			else
			{
				Assert.NotNull(reshapeInfo.ReshapePath,
				               "Reshape path and cut reshape path are undefined");

				// try reshape if possible (typically for union-reshapes to the inside)
				var requiredPartIndexToReshape =
					(int) Assert.NotNull(reshapeInfo.PartIndexToReshape);

				IList<int> currentlyReshapableParts;
				reshapeInfo.IdentifyUniquePartIndexToReshape(out currentlyReshapableParts);

				if (currentlyReshapableParts.Contains(requiredPartIndexToReshape))
				{
					// reset to make sure no other parts are reshaped here:
					reshapeInfo.PartIndexToReshape = requiredPartIndexToReshape;
					reshaped = ReshapeUtils.ReshapeGeometryPart(reshapeInfo.GeometryToReshape,
					                                            reshapeInfo);
				}
				else
				{
					reshaped = false;
				}
			}

			return reshaped;
		}
	}
}

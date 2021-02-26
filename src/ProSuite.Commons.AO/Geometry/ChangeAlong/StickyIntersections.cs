using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class StickyIntersections
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly List<IPoint> _targetPoints;

		private List<IFeature> _sourceFeatures;

		[CanBeNull] private IPolyline _sourceIntersectionLines;

		private IPointCollection _unpairedSourceIntersectionPoints;

		public StickyIntersections()
		{
			_targetPoints = new List<IPoint>();

			SourceTargetPairs = new List<KeyValuePair<IPoint, IPoint>>();
		}

		public StickyIntersections([NotNull] IEnumerable<IFeature> sourceFeatures) : this()
		{
			_sourceFeatures = sourceFeatures.ToList();

			_unpairedSourceIntersectionPoints =
				(IPointCollection)
				CalculateSourceIntersections(
					GdbObjectUtils.GetGeometries(_sourceFeatures).Cast<IPolycurve>());
		}

		[NotNull]
		public IEnumerable<IPoint> TargetPoints => _targetPoints;

		public List<KeyValuePair<IPoint, IPoint>> SourceTargetPairs { get; private set; }

		public bool HasTargetPoints()
		{
			return _targetPoints.Count > 0;
		}

		[NotNull]
		public IPointCollection GetTargetPointCollection()
		{
			IGeometry result = GeometryFactory.CreateMultipoint(TargetPoints);

			return (IPointCollection) result;
		}

		[CanBeNull]
		public IPolyline GetSourceTargetConnectLines()
		{
			IGeometryCollection result = null;

			object missing = Type.Missing;

			foreach (KeyValuePair<IPoint, IPoint> sourceTargetPair in SourceTargetPairs)
			{
				IPath connectLine = GeometryFactory.CreatePath(sourceTargetPair.Key,
				                                               sourceTargetPair.Value);

				if (result == null)
				{
					result = (IGeometryCollection) GeometryFactory.CreatePolyline(connectLine);
				}
				else
				{
					result.AddGeometry(connectLine, ref missing, ref missing);
				}
			}

			return (IPolyline) result;
		}

		public void UpdateSourceFeatures([NotNull] IEnumerable<IFeature> features)
		{
			_sourceFeatures = features.ToList();

			UpdateSourceTargetPairsAndUnpairedSourceIntersections();
		}

		/// <summary>
		/// Tries to find target intersection points for source intersection points that have 
		/// no target point defined yet and adds these points to the source-target pairs.
		/// The automatically determined target points are at the intersection of the prolonged
		/// shared boundary of polgons and the reshape path.
		/// </summary>
		/// <param name="reshapePath">The reshape path.</param>
		/// <param name="geometriesToReshape">The geometries to reshape.</param>
		/// <param name="sketchOriginalIntersectionPoints">The intersection points between the
		/// sketch and the original geometries to reshape.</param>
		/// <returns></returns>
		public IList<KeyValuePair<IPoint, IPoint>> AddAutomaticSourceTargetPairs(
			[NotNull] IPath reshapePath,
			[NotNull] IList<IGeometry> geometriesToReshape,
			[NotNull] IPointCollection sketchOriginalIntersectionPoints)
		{
			// TODO: Consider moving the automatic pair calculation elsewhere
			int origPairCount = SourceTargetPairs.Count;

			Stopwatch watch =
				_msg.DebugStartTiming("Calculating automatic source-target pairs.");

			IEnumerable<KeyValuePair<IPoint, IPoint>> automaticPairs =
				MatchSinglePointsWithExtendedSourceLines(reshapePath, geometriesToReshape,
				                                         sketchOriginalIntersectionPoints);

			SourceTargetPairs.AddRange(automaticPairs);

			_msg.DebugStopTiming(watch,
			                     "Found addtitional {0} source-target pairs by automatic matching. Total: {1}",
			                     SourceTargetPairs.Count - origPairCount,
			                     SourceTargetPairs.Count);

			return SourceTargetPairs;
		}

		public void Clear()
		{
			ClearTargetPoints();

			_sourceFeatures.Clear();
		}

		public void ClearTargetPoints()
		{
			_targetPoints.Clear();

			SourceTargetPairs.Clear();
		}

		public void RemoveObsoleteTargetPoints([CanBeNull] IGeometry sketchGeometry)
		{
			if (sketchGeometry == null)
			{
				ClearTargetPoints();

				return;
			}

			var pointsToRemove = new List<IPoint>();
			foreach (IPoint targetIntersectionPoint in _targetPoints)
			{
				if (! GeometryUtils.Intersects(sketchGeometry, targetIntersectionPoint))
				{
					pointsToRemove.Add(targetIntersectionPoint);
				}
			}

			foreach (IPoint pointToRemove in pointsToRemove)
			{
				_targetPoints.Remove(pointToRemove);
			}

			if (pointsToRemove.Count > 0)
			{
				UpdateSourceTargetPairsAndUnpairedSourceIntersections();
			}
		}

		public void ToggleTargetPoint([NotNull] IPoint atPoint)
		{
			Assert.ArgumentNotNull(atPoint, nameof(atPoint));

			IPoint pointToRemove = null;
			foreach (IPoint point in _targetPoints)
			{
				if (GeometryUtils.AreEqualInXY(point, atPoint))
				{
					pointToRemove = point;
				}
			}

			if (pointToRemove != null)
			{
				_targetPoints.Remove(pointToRemove);
			}
			else
			{
				_targetPoints.Add(atPoint);
			}

			UpdateSourceTargetPairsAndUnpairedSourceIntersections();
		}

		/// <summary>
		/// Updates the source target pairs AND the _unpairedSourceIntersectionPoints
		/// </summary>
		private void UpdateSourceTargetPairsAndUnpairedSourceIntersections()
		{
			if (_sourceFeatures.Count == 0 || _targetPoints.Count == 0)
			{
				SourceTargetPairs.Clear();
			}

			IList<IPolycurve> sourceGeometries =
				GdbObjectUtils.GetGeometries(_sourceFeatures).Cast<IPolycurve>().ToList();

			var targetPointCollection =
				(IPointCollection) GeometryFactory.CreateMultipoint(_targetPoints);

			SourceTargetPairs = CalculateSourceTargetPairs(
				sourceGeometries, targetPointCollection).ToList();
		}

		private IEnumerable<KeyValuePair<IPoint, IPoint>> CalculateSourceTargetPairs(
			[NotNull] IList<IPolycurve> sourceGeometries,
			[NotNull] IPointCollection targetIntersectionPoints)
		{
			// for all tuples -> calculate standard intersection points (possibly clip first and only use original shapes if none found?)

			if (sourceGeometries.Count <= 1)
			{
				return new List<KeyValuePair<IPoint, IPoint>>(0);
			}

			IMultipoint sourceIntersectionPoints =
				CalculateSourceIntersections(sourceGeometries);

			// TODO: re-use SourceIntersections if selected features have not changed

			IPointCollection unpairedSourcePoints;
			IDictionary<IPoint, IPoint> sourceTargetPairs =
				ReshapeUtils.PairByDistance((IPointCollection) sourceIntersectionPoints,
				                            targetIntersectionPoints, out unpairedSourcePoints);

			_unpairedSourceIntersectionPoints = unpairedSourcePoints;

			return sourceTargetPairs;
		}

		private IMultipoint CalculateSourceIntersections(
			[NotNull] IEnumerable<IPolycurve> sourceGeometries)
		{
			IMultipoint sourceIntersectionPoints = null;
			IPolyline sourceIntersectionLines = null;

			foreach (
				KeyValuePair<IPolycurve, IPolycurve> pair in
				CollectionUtils.GetAllTuples(sourceGeometries))
			{
				IPolycurve polycurve1 = pair.Key;
				IPolycurve polycurve2 = pair.Value;

				IPolyline intersectionLines =
					GeometryFactory.CreateEmptyPolyline(polycurve1);

				IMultipoint intersectionPoints =
					IntersectionUtils.GetIntersectionPoints(
						polycurve1, polycurve2, false,
						IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
						intersectionLines);

				if (sourceIntersectionPoints == null)
				{
					sourceIntersectionPoints = intersectionPoints;
				}
				else
				{
					((IPointCollection) sourceIntersectionPoints).AddPointCollection(
						(IPointCollection) intersectionPoints);
				}

				if (intersectionLines != null && ! intersectionLines.IsEmpty)
				{
					if (sourceIntersectionLines == null)
					{
						sourceIntersectionLines = intersectionLines;
					}
					else
					{
						((IGeometryCollection) sourceIntersectionLines).AddGeometryCollection(
							(IGeometryCollection) intersectionLines);
					}
				}
			}

			Assert.NotNull(sourceIntersectionPoints);

			GeometryUtils.Simplify(sourceIntersectionPoints);

			// un-simplified!
			_sourceIntersectionLines = sourceIntersectionLines;

			return sourceIntersectionPoints;
		}

		private IList<IPath> GetExtendedSourceLinesForSinglePoints(
			[NotNull] IPath reshapePath,
			[NotNull] IList<IGeometry> geometriesToReshape,
			[NotNull] IPointCollection sketchOriginalIntersectionPoints)
		{
			if (_unpairedSourceIntersectionPoints == null ||
			    _unpairedSourceIntersectionPoints.PointCount == 0)
			{
				return new List<IPath>(0);
			}

			var result = new List<IPath>(_unpairedSourceIntersectionPoints.PointCount);

			foreach (
				IPoint unpairedSourcePoint in
				GeometryUtils.GetPoints(_unpairedSourceIntersectionPoints))
			{
				IEnumerable<IPath> connectLineCandidates =
					GetConnectLineCandidates(unpairedSourcePoint, reshapePath, geometriesToReshape,
					                         sketchOriginalIntersectionPoints);

				// for the moment - later on, consider checks for intersections with other connect lines etc
				IPath connectLine = GeometryUtils.GetSmallestGeometry(connectLineCandidates);

				if (connectLine != null)
				{
					if (GeometryUtils.AreEqualInXY(connectLine.ToPoint,
					                               unpairedSourcePoint))
					{
						connectLine.ReverseOrientation();
					}

					result.Add(connectLine);
				}
			}

			return result;
		}

		private IEnumerable<KeyValuePair<IPoint, IPoint>>
			MatchSinglePointsWithExtendedSourceLines(
				[NotNull] IPath reshapePath,
				[NotNull] IList<IGeometry> geometriesToReshape,
				[NotNull] IPointCollection sketchOriginalIntersectionPoints)
		{
			IList<IPath> connectLines = GetExtendedSourceLinesForSinglePoints(reshapePath,
			                                                                  geometriesToReshape,
			                                                                  sketchOriginalIntersectionPoints);
			// Filter those connect lines that cross each other.
			var crossingLines = new List<IPath>(connectLines.Count);

			for (var i = 0; i < connectLines.Count; i++)
			{
				IGeometry highLevelPath = GeometryUtils.GetHighLevelGeometry(connectLines[i],
				                                                             true);

				for (int j = i + 1; j < connectLines.Count; j++)
				{
					if (Crosses(highLevelPath, connectLines[j]))
					{
						// Theoretically one could remain. The shorter one? Some might also be irrelevant 
						// (no source intersection any more) after cutting off an area from a polygon.
						// and there would be no crossing any more -> consider keeping the crossing lines
						// and re-evaluate crossings for each polygon.
						crossingLines.Add(connectLines[i]);
						crossingLines.Add(connectLines[j]);
					}
				}

				Marshal.ReleaseComObject(highLevelPath);
			}

			// And filter those that cross the existing manual connect lines:
			IPolyline manualConnectLines = GetSourceTargetConnectLines();

			foreach (
				IPath connectLine in connectLines.Where(line => ! crossingLines.Contains(line)))
			{
				if (manualConnectLines == null ||
				    ! Crosses(connectLine, manualConnectLines))
				{
					yield return
						new KeyValuePair<IPoint, IPoint>(connectLine.FromPoint,
						                                 connectLine.ToPoint);
				}
			}
		}

		private static bool Crosses([NotNull] IGeometry geometry1,
		                            [NotNull] IGeometry geometry2)
		{
			Assert.ArgumentNotNull(geometry1, nameof(geometry1));
			Assert.ArgumentNotNull(geometry2, nameof(geometry2));

			IGeometry highLevelGeometry1 = GeometryUtils.GetHighLevelGeometry(geometry1, true);
			IGeometry highLevelGeometry2 = GeometryUtils.GetHighLevelGeometry(geometry2, true);

			bool result = GeometryUtils.Crosses(highLevelGeometry1, highLevelGeometry2);

			if (highLevelGeometry1 != geometry1)
			{
				Marshal.ReleaseComObject(highLevelGeometry1);
			}

			if (highLevelGeometry2 != geometry2)
			{
				Marshal.ReleaseComObject(highLevelGeometry2);
			}

			return result;
		}

		private IEnumerable<IPath> GetConnectLineCandidates(
			[NotNull] IPoint forUnpairedSourcePoint,
			[NotNull] IPath reshapePath,
			[NotNull] IList<IGeometry> geometriesToReshape,
			IPointCollection sketchOriginalIntersectionPoints)
		{
			var touchingPaths = new List<IPath>();

			if (_sourceIntersectionLines != null)
			{
				foreach (IPath intersectionPath in
					GeometryUtils.GetPaths(_sourceIntersectionLines))
				{
					if (
						GeometryUtils.AreEqualInXY(intersectionPath.FromPoint,
						                           forUnpairedSourcePoint) ||
						GeometryUtils.AreEqualInXY(intersectionPath.ToPoint, forUnpairedSourcePoint)
					)
					{
						touchingPaths.Add(intersectionPath);
					}
				}
			}

			var connectLineCandidates = new List<IPath>();
			foreach (IPath sharedBoundary in touchingPaths)
			{
				if (IntersectsOtherSourceTargetConnection(sharedBoundary,
				                                          sketchOriginalIntersectionPoints,
				                                          SourceTargetPairs))
				{
					continue;
				}

				IList<IGeometry> sharedBoundaryPolys;
				if (PathRunsAlongSeveralPolygons(sharedBoundary, geometriesToReshape,
				                                 forUnpairedSourcePoint,
				                                 out sharedBoundaryPolys))
				{
					IGeometry sharedBoundaryPoly = sharedBoundaryPolys[0];

					double xyTolerance = GeometryUtils.GetXyTolerance(sharedBoundaryPoly);

					var curveToReshape =
						(ICurve)
						GeometryUtils.GetHitGeometryPart(forUnpairedSourcePoint, sharedBoundaryPoly,
						                                 xyTolerance);

					Assert.NotNull(curveToReshape);

					IPoint targetConnectPoint;
					IPath connectLineCandidate = GetSharedBoundaryProlongation(
						curveToReshape, sharedBoundary, forUnpairedSourcePoint, reshapePath,
						xyTolerance,
						out targetConnectPoint);

					// Only change those polygons that have not already been reshaped using the sketch.
					if (connectLineCandidate != null &&
					    AllPolygonsAreDisjoint(sharedBoundaryPolys, targetConnectPoint) &&
					    ! CrossesOtherGeometry(connectLineCandidate, geometriesToReshape))
					{
						connectLineCandidates.Add(connectLineCandidate);
					}
				}
			}

			return connectLineCandidates;
		}

		#region Copies

		private static bool PathRunsAlongSeveralPolygons(
			[NotNull] IPath path, [NotNull] IList<IGeometry> allPolygons,
			[NotNull] IPoint startingAt, out IList<IGeometry> alongPolygons)
		{
			alongPolygons = new List<IGeometry>(allPolygons.Count);

			var touchCount = 0;

			IGeometry highLevelPath = GeometryUtils.GetHighLevelGeometry(path, true);

			foreach (IGeometry polygon in allPolygons)
			{
				// NOTE: Using GeometryUtils.Touches and Intersection of highLevelPath with polygon is very slow!
				//       -> extract only the relevant segments
				IPolyline polygonOutlinePart = TryGetAdjacentSegmentsAsPolyline(
					(IPolygon) polygon, startingAt);

				if (polygonOutlinePart == null || polygonOutlinePart.IsEmpty)
				{
					continue;
				}

				IPolyline lineAlongBoundary = IntersectionUtils.GetIntersectionLines(
					polygonOutlinePart, (IPolycurve) highLevelPath, true, true);

				if (! lineAlongBoundary.IsEmpty)
				{
					touchCount++;

					alongPolygons.Add(polygon);
				}

				Marshal.ReleaseComObject(polygonOutlinePart);
				Marshal.ReleaseComObject(lineAlongBoundary);
			}

			Marshal.ReleaseComObject(highLevelPath);

			return touchCount > 1;
		}

		[CanBeNull]
		private static IPolyline TryGetAdjacentSegmentsAsPolyline(
			[NotNull] IPolygon polygon,
			[NotNull] IPoint startingAt)
		{
			double xyTolerance = GeometryUtils.GetXyTolerance(startingAt);

			int partIdx;
			const bool allowNoMatch = true;
			IList<int> adjacentSegments = SegmentReplacementUtils.GetAdjacentSegmentIndexes(
				polygon, startingAt, xyTolerance, out partIdx, allowNoMatch);

			if (adjacentSegments.Count == 0)
			{
				return null;
			}

			var segmentList = new List<ISegment>();

			foreach (int segmentIdx in adjacentSegments)
			{
				ISegment segment = GeometryUtils.GetSegment((ISegmentCollection) polygon, partIdx,
				                                            segmentIdx);
				segmentList.Add(GeometryFactory.Clone(segment));
			}

			if (segmentList.Count == 0)
			{
				return null;
			}

			ISegment[] segments = segmentList.ToArray();

			// create a polyline
			IPolyline result = GeometryFactory.CreateEmptyPolyline(polygon);

			GeometryUtils.GeometryBridge.AddSegments((ISegmentCollection) result,
			                                         ref segments);

			GeometryUtils.Simplify(result);

			return result;
		}

		[CanBeNull]
		private static IPath GetSharedBoundaryProlongation(
			[NotNull] ICurve curveToReshape,
			[NotNull] IPath sharedBoundary,
			[NotNull] IPoint sourceConnectPoint,
			[NotNull] IPath reshapePath,
			double tolerance,
			[CanBeNull] out IPoint targetConnectPoint)
		{
			// try elongate the shared boundary's last segment
			ISegment sourceSegment = GetConnectSegment(sharedBoundary,
			                                           sourceConnectPoint, tolerance);

			LineEnd segmentEndToProlong = GeometryUtils.AreEqualInXY(sourceSegment.FromPoint,
			                                                         sourceConnectPoint)
				                              ? LineEnd.From
				                              : LineEnd.To;

			IPath result = ReshapeUtils.GetProlongation(curveToReshape, sourceSegment,
			                                            reshapePath, segmentEndToProlong,
			                                            out targetConnectPoint);

			if (result != null && segmentEndToProlong == LineEnd.From)
			{
				result.ReverseOrientation();
			}

			// TODO: The ITopoOp.Difference with the source performed in ReshapeUtils.GetProlongation might handle some 
			//       difficult situations, but also prevent a solution where the source intersection is on the 
			//       interiour of a segment in one polygon and on a vertex in the other. Currently this must be 
			//       dealt with by the user (placing a target intersection point).

			return result;
		}

		private static ISegment GetConnectSegment([NotNull] IPath ofPath,
		                                          [NotNull] IPoint sourceConnectPoint,
		                                          double tolerance)
		{
			// Always gets the previous segment in case of To-Point (even for the 0th)
			int partIndex;
			int segmentIndex = SegmentReplacementUtils.GetSegmentIndex(
				ofPath, sourceConnectPoint, tolerance, out partIndex);

			ISegment sourceSegment =
				((ISegmentCollection) ofPath).Segment[segmentIndex];

			return sourceSegment;
		}

		#endregion

		private static bool IntersectsOtherSourceTargetConnection(
			[NotNull] IPath touchingPath,
			[NotNull] IPointCollection sketchOriginalIntersectionPoints,
			[NotNull] IEnumerable<KeyValuePair<IPoint, IPoint>> sourceTargetPairs)
		{
			IGeometry highLevelTouchingPath = GeometryUtils.GetHighLevelGeometry(touchingPath,
			                                                                     true);

			if (GeometryUtils.Intersects(highLevelTouchingPath,
			                             (IGeometry) sketchOriginalIntersectionPoints))
			{
				return true;
			}

			return sourceTargetPairs.Any(
				sourceTargetPair =>
					GeometryUtils.Intersects(highLevelTouchingPath, sourceTargetPair.Key));
		}

		private static bool CrossesOtherGeometry(IPath path,
		                                         IEnumerable<IGeometry> otherGeometries)
		{
			foreach (IGeometry otherGeometry in otherGeometries)
			{
				if (GetCrossingPoints(path, otherGeometry).PointCount > 0)
				{
					return true;
				}
			}

			return false;
		}

		private static bool AllPolygonsAreDisjoint(IEnumerable<IGeometry> polygons,
		                                           IPoint point)
		{
			foreach (IGeometry geometry in polygons)
			{
				if (GeometryUtils.Intersects(geometry, point))
				{
					return false;
				}
			}

			return true;
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
	}
}

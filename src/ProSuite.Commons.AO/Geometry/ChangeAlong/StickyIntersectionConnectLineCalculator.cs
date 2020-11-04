using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public class StickyIntersectionConnectLineCalculator : IDisposable
		// : ConnectLineCalculatorBase
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly IPoint _pointTemplate = new PointClass();

		private readonly IPolyline _highlevelSketchGeometry;
		private readonly List<KeyValuePair<IPoint, IPoint>> _intersectingSourceTargetPoints;

		private readonly Dictionary<IGeometry, IList<ReshapeInfo>> _individualReshapes;

		public StickyIntersectionConnectLineCalculator(
			[NotNull] IPolyline highLevelSketch,
			[NotNull] IPolyline individuallyReshapedPolyline,
			[NotNull] List<KeyValuePair<IPoint, IPoint>> intersectingSourceTargetPoints,
			[NotNull] Dictionary<IGeometry, IList<ReshapeInfo>> individualReshapes)
		{
			_highlevelSketchGeometry = highLevelSketch;

			_intersectingSourceTargetPoints = intersectingSourceTargetPoints;
			_individualReshapes = individualReshapes;

			UnreshapedBoundaryPart =
				ReshapeUtils.GetDifferencePolyline(individuallyReshapedPolyline,
				                                   _highlevelSketchGeometry);
		}

		public void Dispose()
		{
			Marshal.ReleaseComObject(UnreshapedBoundaryPart);
		}

		private IPolyline UnreshapedBoundaryPart { get; }

		public ICollection<IGeometry> GeometriesToReshape { get; set; }

		public bool IsTargetIntersectionPoint(IPoint point,
		                                      out IPoint respectiveSource)
		{
			respectiveSource = null;
			foreach (
				KeyValuePair<IPoint, IPoint> sourceTargetPoint in _intersectingSourceTargetPoints)
			{
				if (GeometryUtils.AreEqualInXY(sourceTargetPoint.Value, point))
				{
					respectiveSource = sourceTargetPoint.Key;
					return true;
				}
			}

			return false;
		}

		public IPath FindConnection(IPolyline geometryToReshapeAsPolyline,
		                            ICurve pathOnTarget, bool searchForward)
		{
			IPoint toTargetPoint = searchForward
				                       ? pathOnTarget.FromPoint
				                       : pathOnTarget.ToPoint;

			if (_intersectingSourceTargetPoints.Count == 0)
			{
				return null;
			}

			IPath result = null;

			IPoint sourcePoint;

			// if the target point is already on the geometry (due to individual reshape) still
			// try to connect to source to be consistent with adjacent polygons...
			//// the target intersection point could have already been met by individual reshape:
			//if (GeometryUtils.Intersects(geometryToReshapeAsPolyline, toTargetPoint))
			//{
			//	// The targetPoint actually intersects the source -> no connect line needed, but return a usable connect line
			//	IPath singlePointPath = CreateSinglePointPath(toTargetPoint);

			//	result = singlePointPath;
			//}
			if (IsTargetIntersectionPoint(toTargetPoint, out sourcePoint))
			{
				//it's an actual target intersection point
				if (GeometryUtils.Intersects(geometryToReshapeAsPolyline, sourcePoint))
				{
					//TEST: 
					// TODO: inject otherGeometriesIndividualReshapes from outside (using geometryToReshapeAsPolyline is not correct!)
					IPath otherPolysReplacedSegments =
						GetReplacedSegmentsForOtherGeometry(geometryToReshapeAsPolyline,
						                                    sourcePoint,
						                                    toTargetPoint);

					if (otherPolysReplacedSegments != null)
					{
						result = otherPolysReplacedSegments;
					}

					// END TEST
					else
					{
						// if the source point is still part of the current geometry, but an additional connectionLineAtCutOff
						// might be found that will later be relevant when this path might have been reshaped away again
						result = GeometryFactory.CreatePath(sourcePoint, toTargetPoint);

						// TODO: evaluate blocking just before applying - if it is blocked, retry after all other geometries have been reshaped...
						if (CrossesBlockingGeometry(result))
						{
							result = null;
						}
					}
				}
				else
				{
					// if the target point is on the border between an already reshaped and an unreshaped part:
					bool targetPointTouchesUnreshapedPart = GeometryUtils.Touches(
						UnreshapedBoundaryPart, toTargetPoint);

					if (targetPointTouchesUnreshapedPart)
					{
						// Return a usable connection. Do not try to connect back to last unreshaped
						// vertex because this might have already been done for the adjacent pathOnTarget
						// and now the toTargetPoint is already ok
						result = CreateSinglePointPath(toTargetPoint);
					}
				}

				if (result == null && GeometryUtils.AreEqualInXY(sourcePoint, toTargetPoint))
				{
					// TODO: is this ever needed???
					// The targetPoint actually intersects the source -> no connect line needed, but return a usable connect line
					IPath singlePointPath = CreateSinglePointPath(toTargetPoint);

					return singlePointPath;
				}
			}

			// the toTargetPoint point could have already been met by individual reshape:
			if (result == null &&
			    GeometryUtils.Intersects(geometryToReshapeAsPolyline, toTargetPoint))
			{
				// The targetPoint actually intersects the source -> no connect line needed, but return a usable connect line
				IPath singlePointPath = CreateSinglePointPath(toTargetPoint);

				result = singlePointPath;
			}

			return result;
		}

		private bool CrossesBlockingGeometry(IPath result)
		{
			bool crossesBlockingGeometry =
				GetCrossingPointCount(result, _highlevelSketchGeometry) != 0;

			return crossesBlockingGeometry;
		}

		public IPath FindConnectionAtBoundaryCutOff(
			ICurve pathOnTarget, bool searchForward)
		{
			IPoint toTargetPoint = searchForward
				                       ? pathOnTarget.FromPoint
				                       : pathOnTarget.ToPoint;

			bool isUsableTarget = _intersectingSourceTargetPoints.Any(
				intersectingSourceTargetPoint =>
					GeometryUtils.Intersects(intersectingSourceTargetPoint.Value, toTargetPoint));

			if (! isUsableTarget)
			{
				return null;
			}

			IPath result = GetCutOffBoundaryConnectLine(pathOnTarget, toTargetPoint);

			if (result != null &&
			    CrossesBlockingGeometry(result))
			{
				result = null;
			}

			return result;
		}

		private IPath GetReplacedSegmentsForOtherGeometry(
			[NotNull] IGeometry geometryToReshape,
			[NotNull] IPoint betweenSourceIntersectionPoint,
			[NotNull] IPoint andTargetIntersectionPoint)
		{
			Assert.NotNull(_individualReshapes);

			foreach (
				KeyValuePair<IGeometry, IList<ReshapeInfo>> individualReshape in
				_individualReshapes)
			{
				if (individualReshape.Key == geometryToReshape)
				{
					continue;
				}

				foreach (ReshapeInfo reshapeInfo in individualReshape.Value)
				{
					IPath replacedSegments = reshapeInfo.ReplacedSegments;

					if (replacedSegments != null)
					{
						IGeometry highLevelReplacedSegments =
							GeometryUtils.GetHighLevelGeometry(replacedSegments, true);

						if (
							GeometryUtils.Intersects(highLevelReplacedSegments,
							                         betweenSourceIntersectionPoint) &&
							GeometryUtils.Intersects(highLevelReplacedSegments,
							                         andTargetIntersectionPoint))
						{
							return SegmentReplacementUtils.GetSegmentsBetween(
								betweenSourceIntersectionPoint, andTargetIntersectionPoint,
								replacedSegments);
						}
					}
				}
			}

			return null;
		}

		public IPath GetCutOffBoundaryConnectLine(ICurve pathOnTarget,
		                                          IPoint toTargetPoint)
		{
			IPath result = null;

			// disregard the part that does not connect to the un-reshaped part
			if (! GeometryUtils.Disjoint(UnreshapedBoundaryPart,
			                             GeometryUtils.GetHighLevelGeometry(pathOnTarget, true)))
			{
				IPoint lastUnreshapedVertex = null;

				IPoint otherPathOnTargetEnd = null;
				if (GeometryUtils.AreEqualInXY(pathOnTarget.FromPoint, toTargetPoint))
				{
					otherPathOnTargetEnd = pathOnTarget.ToPoint;
				}
				else if (GeometryUtils.AreEqualInXY(pathOnTarget.ToPoint, toTargetPoint))
				{
					otherPathOnTargetEnd = pathOnTarget.FromPoint;
				}

				foreach (IPath unreshapedPath in GeometryUtils.GetPaths(UnreshapedBoundaryPart))
				{
					var unreshapedPoints = (IPointCollection) unreshapedPath;

					if (GeometryUtils.AreEqualInXY(unreshapedPath.FromPoint, otherPathOnTargetEnd))
					{
						lastUnreshapedVertex = unreshapedPoints.Point[1];
					}
					else if (GeometryUtils.AreEqualInXY(unreshapedPath.ToPoint,
					                                    otherPathOnTargetEnd))
					{
						lastUnreshapedVertex =
							unreshapedPoints.Point[unreshapedPoints.PointCount - 2];
					}
				}

				if (lastUnreshapedVertex != null)
				{
					result = GeometryFactory.CreatePath(lastUnreshapedVertex,
					                                    toTargetPoint);
				}
			}

			return result;
		}

		private static IPath CreateSinglePointPath(IPoint toTargetPoint)
		{
			IPath singlePointPath = new PathClass();
			singlePointPath.SpatialReference = toTargetPoint.SpatialReference;
			singlePointPath.FromPoint = toTargetPoint;
			singlePointPath.ToPoint = toTargetPoint;
			return singlePointPath;
		}

		/// <summary>
		/// Gets the number of times the crossingLine crosses the polylineToCross excluding start and end point
		/// of the crossingLine. This is not the same as interior-intersects.
		/// </summary>
		/// <param name="crossingLine"></param>
		/// <param name="polylineToCross"></param>
		/// <returns></returns>
		private static int GetCrossingPointCount([NotNull] ICurve crossingLine,
		                                         [NotNull] IPolyline polylineToCross)
		{
			// TODO: copy from ConnectLineCalculatorBase -> move to ReshapeUtils
			IGeometry highLevelCrossingLine =
				GeometryUtils.GetHighLevelGeometry(crossingLine, true);

			// NOTE: IRelationalOperator.Crosses() does not find cases where the start point is tangential and
			//		 neither it finds cases where there is also a 1-dimensional intersection in addition to a point-intersection
			// NOTE: use GeometryUtils.GetIntersectionPoints to find also these cases where some intersections are 1-dimensional
			var intersectionPoints =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(polylineToCross,
				                                        highLevelCrossingLine);

			// count the points excluding start and end point of the crossing line
			var crossingPointCount = 0;

			for (var i = 0; i < intersectionPoints.PointCount; i++)
			{
				intersectionPoints.QueryPoint(i, _pointTemplate);

				if (GeometryUtils.AreEqualInXY(_pointTemplate, crossingLine.FromPoint) ||
				    GeometryUtils.AreEqualInXY(_pointTemplate, crossingLine.ToPoint))
				{
					continue;
				}

				crossingPointCount++;
			}

			if (crossingPointCount > 0 && _msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat(
					"Intersections between crossing line {0} and polylineToCross: {1}",
					GeometryUtils.ToString(crossingLine),
					GeometryUtils.ToString((IMultipoint) intersectionPoints));
			}

			if (highLevelCrossingLine != crossingLine)
			{
				Marshal.ReleaseComObject(highLevelCrossingLine);
			}

			return crossingPointCount;
		}
	}
}

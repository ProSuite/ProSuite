using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public abstract class ConnectLineCalculatorBase : IConnectLineCalculator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IPolyline _blockingGeometry;

		private static readonly IPoint _pointTemplate = new PointClass();

		protected ConnectLineCalculatorBase([CanBeNull] IPolyline blockingGeometry)
		{
			if (blockingGeometry != null)
			{
				GeometryUtils.AllowIndexing(blockingGeometry);
			}

			_blockingGeometry = blockingGeometry;
		}

		/// <summary>
		/// Finds the first possible connection to the adjust target line.
		/// </summary>
		/// <param name="curveToConnectTo">Source part curve</param>
		/// <param name="curveToSearch"></param>
		/// <param name="searchForward">Direction to search</param>
		/// <param name="fallBackConnection"></param>
		/// <returns>Index of the first adjustable point</returns>
		public abstract IPath FindConnection(ICurve curveToConnectTo,
		                                     ICurve curveToSearch,
		                                     bool searchForward,
		                                     out IPath fallBackConnection);

		public bool IsValidConnectablePoint(IPoint candidatePoint,
		                                    ICurve curveToConnectTo,
		                                    out IPath connectionLine,
		                                    out IPoint pointToConnectTo)
		{
			connectionLine = GetConnectionLine(curveToConnectTo, candidatePoint,
			                                   out pointToConnectTo);

			// NOTE: if other algorithm than nearest point on source is used: also check if source geometry parts are crossed
			// TODO: consider adding buffer line to blocking geometry to avoid lines passing outside the buffer

			return ! CrossesBlockingGeometry(connectionLine);
		}

		[NotNull]
		protected static IEnumerable<IPoint> GetPointsAlongCurve([NotNull] ICurve curve,
		                                                         bool goForward)
		{
			IPoint pointOnCurve = new PointClass();

			// TODO: Make configurable
			double densifyDeviation = GeometryUtils.GetXyTolerance(curve) * 5;

			var segmentsToSearch = (ISegmentCollection) curve;

			ISegment checkSegment = null;

			for (int i = 0; i < segmentsToSearch.SegmentCount; i++)
			{
				int segmentIndex = goForward
					                   ? i
					                   : segmentsToSearch.SegmentCount - i - 1;

				checkSegment = segmentsToSearch.get_Segment(segmentIndex);

				if (checkSegment.GeometryType == esriGeometryType.esriGeometryLine)
				{
					if (goForward)
					{
						checkSegment.QueryFromPoint(pointOnCurve);
					}
					else
					{
						checkSegment.QueryToPoint(pointOnCurve);
					}

					yield return pointOnCurve;
				}
				else
				{
					foreach (
						ILine line in
						GetDensifiedSegment(checkSegment, densifyDeviation, goForward))
					{
						if (goForward)
						{
							line.QueryFromPoint(pointOnCurve);
						}
						else
						{
							line.QueryToPoint(pointOnCurve);
						}

						yield return pointOnCurve;
					}
				}
			}

			if (checkSegment != null)
			{
				if (goForward)
				{
					checkSegment.QueryFromPoint(pointOnCurve);
				}
				else
				{
					checkSegment.QueryToPoint(pointOnCurve);
				}

				yield return pointOnCurve;
			}
		}

		protected bool CrossesBlockingGeometry([NotNull] ICurve crossingLine)
		{
			return _blockingGeometry != null &&
			       GetCrossingPointCount(crossingLine, _blockingGeometry) != 0;
		}

		[NotNull]
		protected static IPath GetConnectionLine([NotNull] ICurve curveToConnectTo,
		                                         [NotNull] IPoint connectPoint,
		                                         [NotNull] out IPoint pointToConnectTo)
		{
			// TODO: for other implementations (extended reshape type) also the target curve (on which the connect point lies) is needed

			return GeometryUtils.GetShortestConnection(connectPoint, curveToConnectTo,
			                                           out pointToConnectTo);
		}

		/// <summary>
		/// Gets the number of times the crossingLine crosses the polylineToCross excluding start and end point
		/// of the crossingLine.
		/// </summary>
		/// <param name="crossingLine"></param>
		/// <param name="polylineToCross"></param>
		/// <returns></returns>
		private static int GetCrossingPointCount([NotNull] ICurve crossingLine,
		                                         [NotNull] IPolyline polylineToCross)
		{
			IGeometry highLevelCrossingLine =
				GeometryUtils.GetHighLevelGeometry(crossingLine);

			// NOTE: IRelationalOperator.Crosses() does not find cases where the start point is tangential and
			//		 neither it finds cases where there is also a 1-dimensional intersection in addition to a point-intersection
			// NOTE: use GeometryUtils.GetIntersectionPoints to find also these cases where some intersections are 1-dimensional
			var intersectionPoints =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(polylineToCross,
				                                        highLevelCrossingLine);

			// count the points excluding start and end point of the crossing line
			int crossingPointCount = 0;

			for (int i = 0; i < intersectionPoints.PointCount; i++)
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

		[NotNull]
		private static IEnumerable<ILine> GetDensifiedSegment([NotNull] ISegment segment,
		                                                      double maxDeviation,
		                                                      bool forward)
		{
			// TODO: how to determine the appropriate number of segments? just use a very large number?

			var outSegmentsArray = new ILine[100];

			int outSegments = 0;

			GeometryUtils.GeometryBridge.Densify(segment, maxDeviation, ref outSegments,
			                                     ref outSegmentsArray);

			for (int i = 0; i < outSegments; i++)
			{
				int segmentIndex = forward
					                   ? i
					                   : outSegments - i - 1;

				yield return outSegmentsArray[segmentIndex];
			}

			// TODO Marshal release them all
		}
	}
}

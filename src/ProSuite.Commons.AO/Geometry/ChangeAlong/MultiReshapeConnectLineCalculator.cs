using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class MultiReshapeConnectLineCalculator : ConnectLineCalculatorBase
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IGeometry _originalUnion;
		private readonly IGeometry _geometryPartToReshape;
		private readonly ReshapeInfo _unionReshapeInfo;
		private readonly double _maxProlongationLengthFactor;
		private readonly double _tolerance;

		public MultiReshapeConnectLineCalculator(IGeometry originalUnion,
		                                         IGeometry geometryPartToReshape,
		                                         ReshapeInfo unionReshapeInfo,
		                                         double maxProlongationLengthFactor)
			: base(GeometryFactory.CreatePolyline(originalUnion))
		{
			_originalUnion = originalUnion;
			_geometryPartToReshape = geometryPartToReshape;
			_unionReshapeInfo = unionReshapeInfo;
			_maxProlongationLengthFactor = maxProlongationLengthFactor;

			_tolerance = GeometryUtils.GetXyTolerance(originalUnion);

			Notifications = new NotificationCollection();
			FallbackNotifications = new NotificationCollection();
		}

		public IPoint TargetConnectPointFrom { set; private get; }
		public IPoint TargetConnectPointTo { set; private get; }

		public NotificationCollection Notifications { get; }

		public NotificationCollection FallbackNotifications { get; }

		public override IPath FindConnection(ICurve sourceReplacementPath,
		                                     ICurve reshapePath, bool searchForward,
		                                     out IPath fallBackConnection)
		{
			// If searchForward: start point, else end point
			IPoint sourceReplacementPoint = searchForward
				                                ? sourceReplacementPath.FromPoint
				                                : sourceReplacementPath.ToPoint;

			int? sourceReplacementEndIdxOnTarget = GeometryUtils.FindHitSegmentIndex(
				reshapePath, sourceReplacementPoint, _tolerance, out int _);

			IPath result;

			if (sourceReplacementEndIdxOnTarget == null)
			{
				result = GetConnectLine((IPath) reshapePath, (IPath) sourceReplacementPath,
				                        searchForward, (ICurve) _geometryPartToReshape, _tolerance,
				                        _maxProlongationLengthFactor, out fallBackConnection);
			}
			else
			{
				// set both results to zero-length line
				fallBackConnection = result = CreateSinglePointPath(sourceReplacementPoint);
			}

			return result;
		}

		[CanBeNull]
		private IPath GetConnectLine([NotNull] IPath reshapePath,
		                             [NotNull] IPath sourceReplacementPath,
		                             bool atFromPoint,
		                             [NotNull] ICurve partToReshape,
		                             double tolerance,
		                             double maxProlongationLengthFactor,
		                             [CanBeNull] out IPath fallBackConnection)
		{
			IPath connectLine = null;

			IPoint targetPointShortest;

			IPoint sourceConnectPoint = atFromPoint
				                            ? sourceReplacementPath.FromPoint
				                            : sourceReplacementPath.ToPoint;

			IPoint targetConnectPoint = atFromPoint
				                            ? TargetConnectPointFrom
				                            : TargetConnectPointTo;

			IPath shortestConnection = GeometryUtils.GetShortestConnection(
				sourceConnectPoint, reshapePath,
				out targetPointShortest);

			if (targetConnectPoint != null)
			{
				fallBackConnection = shortestConnection;
				return CalculateSourceTargetPointsConnectLine(sourceConnectPoint,
				                                              targetConnectPoint);
			}

			IPath prolongation = null;
			if (_originalUnion.GeometryType == esriGeometryType.esriGeometryPolygon)
			{
				fallBackConnection = ValidatePolygonConnectLine(shortestConnection,
				                                                targetPointShortest,
				                                                FallbackNotifications);

				IPoint targetPointProlongation;
				prolongation = GetSharedBoundaryProlongation(
					partToReshape, sourceReplacementPath, atFromPoint, reshapePath,
					tolerance, out targetPointProlongation);

				prolongation = ValidatePolygonConnectLine(prolongation, targetPointProlongation,
				                                          Notifications);
			}
			else
			{
				// for line-union-reshapes the proportionate distribution method provides the fallback
				fallBackConnection = null;
				connectLine = shortestConnection;
			}

			double maxProlongationLength =
				maxProlongationLengthFactor * shortestConnection.Length;

			if (prolongation != null && prolongation.Length < maxProlongationLength)
			{
				connectLine = prolongation;
			}

			return connectLine;
		}

		private IPath CalculateSourceTargetPointsConnectLine(IPoint sourceConnectPoint,
		                                                     IPoint targetConnectPoint)
		{
			IPath result;

			var unionReplacedPolyline =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(_unionReshapeInfo.ReplacedSegments);

			if (GeometryUtils.Intersects(targetConnectPoint, unionReplacedPolyline))
			{
				// use the connection along the replaced segments
				result = SegmentReplacementUtils.GetSegmentsBetween(
					sourceConnectPoint, targetConnectPoint, _unionReshapeInfo.ReplacedSegments);
			}
			else
			{
				result = GeometryFactory.CreatePath(sourceConnectPoint, targetConnectPoint);
			}

			return result;
		}

		private IPath ValidatePolygonConnectLine([CanBeNull] IPath connectLine,
		                                         [NotNull] IPoint targetPointShortest,
		                                         [CanBeNull] NotificationCollection
			                                         notifications)
		{
			if (connectLine == null)
			{
				return null;
			}

			if (CrossesBlockingGeometry(connectLine))
			{
				_msg.DebugFormat("Connect line {0} crosses original union. Setting null",
				                 GeometryUtils.ToString(connectLine));

				connectLine = null;
				NotificationUtils.Add(notifications,
				                      "Unable to reshape as union because the connection between the shared boundary and the reshape line crosses existing polygons");
			}

			// NOTE: Contains is wrong when just outside the polygon, but within the tolerance
			if (GeometryUtils.Intersects(_originalUnion, targetPointShortest))
			{
				connectLine = null;
				NotificationUtils.Add(notifications,
				                      "Reshape as union not required in reshape to the inside");
			}

			return connectLine;
		}

		[CanBeNull]
		private static IPath GetSharedBoundaryProlongation([NotNull] ICurve curveToReshape,
		                                                   [NotNull] IPath
			                                                   sourceReplacementPath,
		                                                   bool atSourceReplacementFromPoint,
		                                                   [NotNull] IPath reshapePath,
		                                                   double tolerance,
		                                                   out IPoint targetConnectPoint)
		{
			IPoint sourceConnectPoint = atSourceReplacementFromPoint
				                            ? sourceReplacementPath.FromPoint
				                            : sourceReplacementPath.ToPoint;

			// try elongate the curveToReshape's last segment that's not part of the replacement path
			ISegment sourceSegment = GetConnectSegment(sourceReplacementPath, curveToReshape,
			                                           sourceConnectPoint, tolerance);

			IPath result = ReshapeUtils.GetProlongation(curveToReshape, sourceSegment,
			                                            reshapePath,
			                                            out targetConnectPoint);

			if (result == null)
			{
				// check if the reshape path intersects the source segment:
				IGeometry highLevelSourceSegment =
					GeometryUtils.GetHighLevelGeometry(sourceSegment, true);

				IGeometry highLevelReshapePath = GeometryUtils.GetHighLevelGeometry(reshapePath,
				                                                                    true);

				var intersectionPoints =
					(IPointCollection) IntersectionUtils.GetIntersectionPoints(
						highLevelReshapePath,
						highLevelSourceSegment, false);

				if (intersectionPoints.PointCount == 1)
				{
					result = CreateSinglePointPath(intersectionPoints.Point[0]);
					targetConnectPoint = intersectionPoints.Point[0];
				}
				else
				{
					_msg.DebugFormat(
						"The source last shared segment has {0} intersection points with the reshape path. Currently not supported",
						intersectionPoints.PointCount);
				}
			}

			return result;
		}

		private static ISegment GetConnectSegment([NotNull] IPath sourceReplacementPath,
		                                          [NotNull] ICurve curveToReshape,
		                                          [NotNull] IPoint sourceConnectPoint,
		                                          double tolerance)
		{
			// Always gets the previous segment in case of To-Point (even for the 0th)
			int segmentIndex = SegmentReplacementUtils.GetSegmentIndex(
				curveToReshape, sourceConnectPoint, tolerance, out int _);

			ISegment sourceSegment =
				((ISegmentCollection) curveToReshape).Segment[segmentIndex];

			IGeometry highLevelSourceSegment = GeometryUtils.GetHighLevelGeometry(
				sourceSegment, true);

			// if the reshape line connects in the segment's interior, it should be always used
			bool sourceConnectPointIsSegmentToPoint =
				GeometryUtils.Touches(highLevelSourceSegment, sourceConnectPoint);

			IGeometry highLevelSourceReplacement =
				GeometryUtils.GetHighLevelGeometry(sourceReplacementPath, true);

			// however, if the reshape line connects in the segment's to-point, the segment that
			// is not part of the source replacement should be used -> it's part of the shared boundary
			if (sourceConnectPointIsSegmentToPoint &&
			    GeometryUtils.InteriorIntersects(
				    highLevelSourceReplacement, highLevelSourceSegment))
			{
				if (segmentIndex == ((ISegmentCollection) curveToReshape).SegmentCount - 1)
				{
					segmentIndex = 0;
				}
				else
				{
					segmentIndex++;
				}
			}

			sourceSegment = ((ISegmentCollection) curveToReshape).Segment[segmentIndex];

			Marshal.ReleaseComObject(highLevelSourceSegment);
			Marshal.ReleaseComObject(highLevelSourceReplacement);

			return sourceSegment;
		}

		[NotNull]
		private static IPath CreateSinglePointPath(IPoint point)
		{
			IPath path = new PathClass
			             {
				             FromPoint = point,
				             ToPoint = point,
				             SpatialReference = point.SpatialReference
			             };
			return path;
		}
	}
}

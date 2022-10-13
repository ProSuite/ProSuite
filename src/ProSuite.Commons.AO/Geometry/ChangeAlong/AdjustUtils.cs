using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public static class AdjustUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly ThreadLocal<IPoint> _point =
			new ThreadLocal<IPoint>(() => new PointClass());

		[CanBeNull]
		public static AdjustedCutSubcurve CalculateAdjustedPath(
			[NotNull] IPath adjustLine,
			[NotNull] ICurve sourcePart,
			[NotNull] IConnectLineCalculator connectCalculator)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			AdjustedCutSubcurve adjustedSubcurve = null;

			IPath startFallback;
			IPath endFallback;

			IPath startSourceConnection = connectCalculator.FindConnection(
				sourcePart, adjustLine, true, out startFallback);

			IPath endSourceConnection = connectCalculator.FindConnection(
				sourcePart, adjustLine, false, out endFallback);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("CalculatedAdjustedPath start connection: {0}",
				                 GeometryUtils.ToString(startSourceConnection));

				_msg.DebugFormat("CalculateAdjustedPath: end connection: {0}",
				                 GeometryUtils.ToString(endSourceConnection));
			}

			// Possible criteria to consider the path valid:
			// - length of connections vs. length of path
			// - angle between path and connections
			// - size of the reshape-area (difference before/after reshape) vs. length of path (sliver condition?)
			ValidateConnectLines(sourcePart, ref startSourceConnection, ref endSourceConnection,
			                     startFallback, endFallback);

			if (startSourceConnection != null && endSourceConnection != null)
			{
				adjustedSubcurve = CreateAdjustedCutSubcurve(adjustLine,
				                                             startSourceConnection,
				                                             endSourceConnection);
			}

			_msg.DebugStopTiming(watch,
			                     "Calculated adjusted subcurve including connection lines to target");

			return adjustedSubcurve;
		}

		[CanBeNull]
		public static AdjustedCutSubcurve CreateAdjustedCutSubcurve(
			[NotNull] IPath fullAdjustLine,
			[NotNull] IPath startSourceConnection,
			[NotNull] IPath endSourceConnection)
		{
			IPoint startPointOnTarget, endPointOnTarget;
			double startDistance = GetConnectDistanceAlong(fullAdjustLine,
			                                               startSourceConnection,
			                                               out startPointOnTarget);

			double endDistance = GetConnectDistanceAlong(fullAdjustLine, endSourceConnection,
			                                             out endPointOnTarget);

			ICurve reshapableAdjustCurve;

			bool startDistanceEqualEndDistance = Math.Abs(startDistance - endDistance) <
			                                     double.Epsilon;

			if (startDistanceEqualEndDistance)
			{
				// in  case of 0-length line on the target we still want a proper line (that has From==To-Point)
				// but only if the two source connections are different (or both have 0-length which is ok
				// for multiple-source-reshape line calculation)
				if (GeometryUtils.AreEqual(startSourceConnection, endSourceConnection) &&
				    startSourceConnection.Length > 0)
				{
					_msg.Debug(
						"Unable to create adjust line becaue it would result in a 'spike' " +
						"(both connections between source and target are the same). This also happens for closed lines.");
					return null;
				}

				_msg.DebugFormat("CalculateAdjustedPath: 0-length adjustable curve detected.");
				reshapableAdjustCurve = new PathClass
				                        {
					                        FromPoint = startPointOnTarget,
					                        ToPoint = endPointOnTarget,
					                        SpatialReference = startPointOnTarget.SpatialReference
				                        };
			}
			else
			{
				fullAdjustLine.GetSubcurve(startDistance, endDistance, false,
				                           out reshapableAdjustCurve);
			}

			return new AdjustedCutSubcurve((IPath) reshapableAdjustCurve,
			                               startSourceConnection,
			                               endSourceConnection);
		}

		public static bool TryBuffer(
			[NotNull] IGeometry geometry,
			double tolerance,
			[CanBeNull] int? logInfoPointCountThreshold,
			[CanBeNull] string bufferingMessage,
			[CanBeNull] NotificationCollection notifications,
			[CanBeNull] out IPolygon bufferedPolygon)
		{
			bufferedPolygon = null;

			if (logInfoPointCountThreshold >= 0 &&
			    ((IPointCollection) geometry).PointCount > logInfoPointCountThreshold)
			{
				_msg.Info(bufferingMessage);
			}

			if (notifications == null)
			{
				notifications = new NotificationCollection();
			}

			if (! ValidateBufferDistance(geometry, tolerance, notifications))
			{
				_msg.DebugFormat("{0}: {1}.",
				                 bufferingMessage, notifications.Concatenate(". "));
				return false;
			}

			bufferedPolygon = GetOutlineBuffer(geometry, tolerance);

			return true;
		}

		/// <summary>
		/// Buffers the geometry if it is a polyline or path, buffers the boundary if geometry is a polygon or ring
		/// </summary>
		/// <param name="oneOrTwoDimensionalGeometry"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon GetOutlineBuffer(
			[NotNull] IGeometry oneOrTwoDimensionalGeometry,
			double tolerance)
		{
			Assert.ArgumentCondition(
				oneOrTwoDimensionalGeometry.GeometryType == esriGeometryType.esriGeometryPolygon ||
				oneOrTwoDimensionalGeometry.GeometryType == esriGeometryType.esriGeometryRing ||
				oneOrTwoDimensionalGeometry.GeometryType == esriGeometryType.esriGeometryPolyline ||
				oneOrTwoDimensionalGeometry.GeometryType == esriGeometryType.esriGeometryPath,
				"Input Geometry must be Polyline/Path or Polygon/Ring");

			// TODO: for performance reduce the geometry to be buffered
			Stopwatch watch = _msg.DebugStartTiming("Buffering geometry with {0}...", tolerance);

			IGeometry sourcePolyline = oneOrTwoDimensionalGeometry as IPolyline ??
			                           GeometryFactory.CreatePolyline(
				                           oneOrTwoDimensionalGeometry);

			const bool explodeBuffers = false;
			const bool densify = true;
			var bufferFactory = new BufferFactory(explodeBuffers, densify);

			IList<IPolygon> buffers = bufferFactory.Buffer(sourcePolyline, tolerance);

			// Expected: one (possibly multipart) polygon
			Assert.AreEqual(1, buffers.Count, "Unexpected number of buffer polygons");

			IPolygon result = buffers[0];

			if (sourcePolyline != oneOrTwoDimensionalGeometry)
			{
				Marshal.ReleaseComObject(sourcePolyline);
			}

			_msg.DebugStopTiming(watch, "Buffered");

			return result;
		}

		public static bool ValidateBufferDistance(
			[NotNull] IGeometry lineOrPolygon,
			double tolerance,
			[CanBeNull] NotificationCollection notifications)
		{
			_msg.DebugFormat("Validating buffer tolerance {0}", tolerance);

			if (double.IsNaN(tolerance))
			{
				NotificationUtils.Add(notifications,
				                      "Buffer distance ({0}) is not a valid number", tolerance);

				return false;
			}

			if (tolerance < GeometryUtils.GetXyTolerance(lineOrPolygon))
			{
				NotificationUtils.Add(notifications,
				                      "Buffer distance ({0}) is too small (smaller than the spatial reference's tolerance)",
				                      tolerance);

				return false;
			}

			double xMin;
			double xMax;
			double yMin;
			double yMax;
			lineOrPolygon.SpatialReference.GetDomain(out xMin, out xMax, out yMin, out yMax);

			if (lineOrPolygon.Envelope.XMax + tolerance > xMax ||
			    lineOrPolygon.Envelope.XMin - tolerance < xMin ||
			    lineOrPolygon.Envelope.YMax + tolerance > yMax ||
			    lineOrPolygon.Envelope.YMin - tolerance < yMin)
			{
				NotificationUtils.Add(
					notifications,
					"Buffer distance ({0}) is tool large. The resulting buffer would have " +
					"coordinates outside the spatial domain",
					tolerance);

				return false;
			}

			return true;
		}

		private static void ValidateConnectLines([NotNull] ICurve sourcePart,
		                                         [CanBeNull] ref IPath startSourceConnection,
		                                         [CanBeNull] ref IPath endSourceConnection,
		                                         [CanBeNull] IPath startFallback,
		                                         [CanBeNull] IPath endFallback)
		{
			startSourceConnection = startSourceConnection ?? startFallback;
			endSourceConnection = endSourceConnection ?? endFallback;

			var fallbackAssigned = false;

			// crossing source connections
			if (startSourceConnection != null && startSourceConnection.Length > 0 &&
			    endSourceConnection != null && endSourceConnection.Length > 0 &&
			    InteriorIntersects(startSourceConnection, endSourceConnection))
			{
				// can happen if one (or both) is (are) prolongated
				_msg.InfoFormat(
					"Unable to use adjust line with source prolongation. Resulting line would cross.");

				if (TryAssignFallback(ref startSourceConnection, startFallback))
				{
					fallbackAssigned = true;
				}

				if (TryAssignFallback(ref endSourceConnection, endFallback))
				{
					fallbackAssigned = true;
				}
			}

			if (startSourceConnection != null && endSourceConnection != null)
			{
				if (EnsureMinimumSourceReplacementLength(sourcePart, ref startSourceConnection,
				                                         ref endSourceConnection, startFallback,
				                                         endFallback))
				{
					fallbackAssigned = true;
				}
			}

			if (fallbackAssigned)
			{
				// validate again -> fall backs could cross too
				ValidateConnectLines(sourcePart, ref startSourceConnection,
				                     ref endSourceConnection, startFallback, endFallback);
			}
		}

		private static bool EnsureMinimumSourceReplacementLength(
			[NotNull] ICurve sourcePart,
			ref IPath startSourceConnection,
			ref IPath endSourceConnection,
			IPath startFallback,
			IPath endFallback)
		{
			var fallbackAssigned = false;

			if (SourceReplacementPathSmallerTolerance(startSourceConnection,
			                                          endSourceConnection,
			                                          sourcePart))
			{
				if (startFallback != null &&
				    ! SourceReplacementPathSmallerTolerance(startFallback, endSourceConnection,
				                                            sourcePart))
				{
					return TryAssignFallback(ref startSourceConnection, startFallback);
				}

				if (endFallback != null &&
				    ! SourceReplacementPathSmallerTolerance(startSourceConnection,
				                                            endFallback, sourcePart))
				{
					return TryAssignFallback(ref endSourceConnection, endFallback);
				}

				_msg.InfoFormat(
					"Unable to use adjust line with source prolongation. Adjust line starts and ends in same point.");

				// fallback on both
				if (TryAssignFallback(ref startSourceConnection, startFallback))
				{
					fallbackAssigned = true;
				}

				if (TryAssignFallback(ref endSourceConnection, endFallback))
				{
					fallbackAssigned = true;
				}
			}

			return fallbackAssigned;
		}

		private static bool SourceReplacementPathSmallerTolerance(
			[NotNull] IPath startSourceConnection,
			[NotNull] IPath endSourceConnection,
			[NotNull] ICurve sourcePart)
		{
			IPoint startSourcePoint = GetConnectPointOnLine((IPath) sourcePart,
			                                                startSourceConnection);
			IPoint endSourcePoint = GetConnectPointOnLine((IPath) sourcePart,
			                                              endSourceConnection);

			double tolerance = GeometryUtils.GetXyTolerance(sourcePart);

			return GeometryUtils.GetPointDistance(startSourcePoint, endSourcePoint) < tolerance;
		}

		private static bool TryAssignFallback(ref IPath sourceConnection, IPath fallback)
		{
			var fallbackAssigned = false;

			if (sourceConnection != fallback)
			{
				sourceConnection = fallback;

				fallbackAssigned = true;
			}
			else
			{
				sourceConnection = null;
			}

			return fallbackAssigned;
		}

		private static bool InteriorIntersects(
			[NotNull] IGeometry geometry1,
			[NotNull] IGeometry geometry2)
		{
			const bool dontClone = true;
			IGeometry highLevelGeo1 = GeometryUtils.GetHighLevelGeometry(geometry1, dontClone);
			IGeometry highLevelGeo2 = GeometryUtils.GetHighLevelGeometry(geometry2, dontClone);

			return GeometryUtils.InteriorIntersects(highLevelGeo1, highLevelGeo2);
		}

		[NotNull]
		private static IPoint GetConnectPointOnLine([NotNull] IPath line,
		                                            [NotNull] IPath connectLine)
		{
			var highLevelLine = (IPolyline) GeometryUtils.GetHighLevelGeometry(line);

			IPoint pointOnLine = GetIntersectingEndpoint(connectLine, highLevelLine);

			Marshal.ReleaseComObject(highLevelLine);

			Assert.NotNull(pointOnLine,
			               "The connection line does not start or end on the source geometry.");

			return pointOnLine;
		}

		[CanBeNull]
		private static IPoint GetIntersectingEndpoint(
			[NotNull] IPath line,
			[NotNull] IPolyline intersectedGeometry)
		{
			IPoint pointOnLine = null;

			if (Intersects(line.FromPoint, intersectedGeometry))
			{
				pointOnLine = line.FromPoint;
			}
			else if (Intersects(line.ToPoint, intersectedGeometry))
			{
				pointOnLine = line.ToPoint;
			}

			return pointOnLine;
		}

		private static bool Intersects(IPoint point, IPolyline polyline)
		{
			// NOTE: In some cases (TOP-4588) the ((IRelationalOperator)point).Disjoint(polyline) 
			//       returns a wrong result (but only in ArcMap, not in the stand-alone unit test!)
			if (GeometryUtils.Intersects(point, polyline))
			{
				return true;
			}

			// NOTE: IRelationalOperator.Disjoint() sometimes returns false instead of true
			//       - sometimes if the intersectedGeometry is a circular arc
			//		 - but also in other situations, e.g. with the Test Data in Lat/Long from TOP-4588
			//			-> Unfortunately only reproducible in memory, not with persisted XML geometries!
			// WORK-AROUND:

			if (GeometryUtils.GetDistanceFromCurve(point, polyline, _point.Value) <
			    GeometryUtils.GetXyTolerance(polyline))
			{
				_msg.DebugFormat(
					"Used work-around to determine intersection between {0} and {1}",
					GeometryUtils.ToString(point), GeometryUtils.ToString(polyline));
				return true;
			}
			// END WORK-AROUND

			return false;
		}

		private static double GetConnectDistanceAlong([NotNull] IPath fullAdjustLine,
		                                              [NotNull] IPath connection,
		                                              [NotNull] out IPoint connectPoint)
		{
			IPoint pointOnAdjustLine = GetConnectPointOnLine(fullAdjustLine, connection);

			Assert.NotNull(pointOnAdjustLine, "Connection does not touch adjust line.");

			double distance = GeometryUtils.GetDistanceAlongCurve(
				fullAdjustLine, pointOnAdjustLine);

			connectPoint = pointOnAdjustLine;

			return distance;
		}
	}
}

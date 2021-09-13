using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	/// <summary>
	/// Contains information about the reshape of a specific geometry part
	/// </summary>
	public class ReshapeInfo : IDisposable
	{
		// TODO: consider renaming to RingReshape (might derive from PathReshape?) and add the actual reshaping as well?

		#region Field Declarations

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const int _useSimplifiedReshapeSideDeterminationVertexThreshold = 7500;

		private int? _partIndexToReshape;

		private IGeometry _geometryPartToReshape;

		private readonly bool _simplifyOnBothSideReshapes;

		#endregion

		public ReshapeInfo([NotNull] IGeometry geometryToReshape,
		                   [NotNull] IPath reshapePath,
		                   [CanBeNull] NotificationCollection notifications)
		{
			ReshapePath = GeometryFactory.Clone(reshapePath);

			// BUG https://issuetracker02.eggits.net/browse/TOP-4574
			// In 10.2.2 when projecting from GCS_ETRS_1989 resolution x to GCS_ETRS_1989 resolution y
			// an error occurs when projecting a path -> project the high-level geometry
			// Error: The Project method cannot do a datum transformation (Error code: -2147220946)
			const bool comparePrecisionAndTolerance = true;
			const bool verticalCoordinateSystemDifferent = false;
			if (! SpatialReferenceUtils.AreEqual(ReshapePath.SpatialReference,
			                                     geometryToReshape.SpatialReference,
			                                     comparePrecisionAndTolerance,
			                                     verticalCoordinateSystemDifferent))
			{
				const bool dontClonePath = true;
				IGeometry highLevelPath = GeometryUtils.GetHighLevelGeometry(ReshapePath,
				                                                             dontClonePath);

				GeometryUtils.EnsureSpatialReference(highLevelPath,
				                                     geometryToReshape.SpatialReference);

				Marshal.ReleaseComObject(highLevelPath);

				_msg.Debug("Reshape path was projected to fit the SR of the geometry to reshape");
			}

			GeometryToReshape = geometryToReshape;
			Notifications = notifications;

			_simplifyOnBothSideReshapes =
				EnvironmentUtils.GetBooleanEnvironmentVariableValue("PROSUITE_RESHAPE_SIMPLIFY");
		}

		[NotNull]
		public IGeometry GeometryToReshape { get; }

		public IPath ReshapePath { get; private set; }

		[CanBeNull]
		public CutSubcurve CutReshapePath { get; set; }

		public int? PartIndexToReshape
		{
			get
			{
				// check if deleted parts have messed up the index in the mean while

				if (_partIndexToReshape != null)
				{
					var geometryCollection = (IGeometryCollection) GeometryToReshape;

					IGeometry testPart = null;
					if (_partIndexToReshape > geometryCollection.GeometryCount - 1)
					{
						_msg.DebugFormat("Geometry part index to reshape ({0}) is out of range.",
						                 _partIndexToReshape);
						_partIndexToReshape = null;
					}
					else if (! IsSamePart(_geometryPartToReshape,
					                      testPart =
						                      geometryCollection.get_Geometry(
							                      (int) _partIndexToReshape)))
					{
						_msg.DebugFormat("Geometry part index to reshape ({0}) is stale.",
						                 _partIndexToReshape);
						_partIndexToReshape = null;
					}

					// otherwise the RCW-reference count explodes!
					if (testPart != null)
					{
						Marshal.ReleaseComObject(testPart);
					}

					if (_partIndexToReshape == null && _geometryPartToReshape != null)
					{
						RepairPartIndexToReshape();
					}
				}

				return _partIndexToReshape;
			}
			set
			{
				_partIndexToReshape = value;

				if (_partIndexToReshape != null)
				{
					if (_geometryPartToReshape != null)
					{
						Marshal.ReleaseComObject(_geometryPartToReshape);
					}

					_geometryPartToReshape =
						((IGeometryCollection) GeometryToReshape).get_Geometry(
							(int) _partIndexToReshape);
				}
			}
		}

		public RingReshapeSideOfLine RingReshapeSide { get; set; }

		public RingReshapeType RingReshapeType { get; private set; }

		[CanBeNull]
		public ReshapeResultFilter ReshapeResultFilter { get; set; }

		public bool AllowFlippedRings { get; set; }

		public bool AllowPhantomIntersectionPoints { get; set; }

		public bool AllowSimplifiedReshapeSideDetermination { get; set; }

		public bool AllowOpenJawReshape { get; set; }

		public bool IsOpenJawReshape { get; set; }

		public bool NonPlanar { get; set; }

		[CanBeNull]
		public NotificationCollection Notifications { get; }

		/// <summary>
		/// Storing the intersection points is a performance improvement
		/// </summary>
		internal IPointCollection IntersectionPoints { get; set; }

		internal IPolygon LeftReshapePolygon { get; private set; }
		internal IPolygon RightReshapePolygon { get; private set; }

		/// <summary>
		/// The segments that were replaced by the reshape.
		/// </summary>
		public IPath ReplacedSegments { get; set; }

		internal bool NotificationIsWarning { get; set; }

		public ReshapeInfo CreateCopy(IGeometry geometryToReshape,
		                              NotificationCollection notifications)
		{
			// Avoid the case where the reshape info is reused with the flipped CutReshapePath
			// and hence incorrect reshape side! Only use the reshapePath
			IPath reshapePath = GeometryFactory.Clone(ReshapePath);

			var clone = new ReshapeInfo(geometryToReshape, reshapePath, notifications)
			            {
				            PartIndexToReshape = PartIndexToReshape,
				            RingReshapeSide = RingReshapeSide,
				            RingReshapeType = RingReshapeType,
				            ReshapeResultFilter = ReshapeResultFilter,
				            AllowFlippedRings = AllowFlippedRings,
				            AllowPhantomIntersectionPoints = AllowPhantomIntersectionPoints,
				            AllowSimplifiedReshapeSideDetermination =
					            AllowSimplifiedReshapeSideDetermination
			            };

			if (IntersectionPoints != null)
			{
				clone.IntersectionPoints =
					(IPointCollection) GeometryFactory.Clone((IMultipoint) IntersectionPoints);
			}

			if (LeftReshapePolygon != null)
			{
				clone.LeftReshapePolygon = GeometryFactory.Clone(LeftReshapePolygon);
			}

			if (RightReshapePolygon != null)
			{
				clone.RightReshapePolygon = GeometryFactory.Clone(RightReshapePolygon);
			}

			if (ReplacedSegments != null)
			{
				clone.ReplacedSegments = GeometryFactory.Clone(ReplacedSegments);
			}

			return clone;
		}

		/// <summary>
		/// Returns the low-level geometry part to reshape
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IGeometry GetGeometryPartToReshape()
		{
			Assert.NotNull(PartIndexToReshape);

			int? verifiedPartIndex = PartIndexToReshape;

			Assert.NotNull(verifiedPartIndex, "No part index to reshape.");

			return ((IGeometryCollection) GeometryToReshape).get_Geometry(
				(int) verifiedPartIndex);
		}

		/// <summary>
		/// Identify the single geometry part to reshape. Currently only one part can be reshaped at the time
		/// unlike the standard reshape which inverts polygons and (randomly) connects polyline parts.
		/// </summary>
		/// <returns>The single geometry part that can be reshaped or null.</returns>
		public int? IdentifyUniquePartIndexToReshape(out IList<int> allReshapablePartIndexes)
		{
			// consider allowing multiple parts that do not intersect each other
			// if multiple intersected rings should be allowed to reshape that contain each other: make sure the 
			// inner rings are reshaped correctly, i.e. there is no 'polygon inversion'
			var geometryCollection = (IGeometryCollection) GeometryToReshape;

			allReshapablePartIndexes = new List<int>(geometryCollection.GeometryCount);

			IGeometry highLevelReshapePath =
				GeometryUtils.GetHighLevelGeometry(ReshapePath);

			int? result = null;

			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				if (CanReshapePart(i, highLevelReshapePath))
				{
					allReshapablePartIndexes.Add(i);
				}
			}

			Marshal.ReleaseComObject(highLevelReshapePath);

			switch (allReshapablePartIndexes.Count)
			{
				case 0:
					NotificationUtils.Add(Notifications,
					                      "Not enough intersection points");
					break;
				case 1:
					result = allReshapablePartIndexes[0];
					_msg.DebugFormat("Path index to reshape: {0}", result);
					PartIndexToReshape = result;
					break;
				default:
					NotificationUtils.Add(Notifications,
					                      "Reshape line intersects {0} parts of the feature to reshape",
					                      allReshapablePartIndexes.Count);
					break;
			}

			return result;
		}

		public bool CanReshapePart(int partIdx,
		                           IGeometry highLevelReshapePath)
		{
			var canReshape = false;

			var part = (ICurve) ((IGeometryCollection) GeometryToReshape).get_Geometry(partIdx);

			// Avoid clone of input part:
			IGeometry highLevelPart = GeometryFactory.CreatePolyline(
				(ISegmentCollection) part, true);

			if (GeometryUtils.Intersects(highLevelReshapePath, highLevelPart))
			{
				// TODO: Consider performance optimization also here: clip the geometries first (e.g. with the unioned envelopes + Tolerance)
				//		 Especially with the preview option this could be quite relevant 
				var intersectPoints =
					(IPointCollection) IntersectionUtils.GetIntersectionPoints(
						highLevelReshapePath, highLevelPart, true);

				if (GeometryToReshape.GeometryType == esriGeometryType.esriGeometryMultiPatch)
				{
					// could eventually also be used for vertical polyline segments' reshape
					GeometryUtils.MakeZAware(highLevelPart);
					var nonPlanarIntersections = (IPointCollection)
						IntersectionUtils.GetIntersectionPointsNonPlanar(
							(IPolycurve) highLevelReshapePath,
							(IPolycurve) highLevelPart);

					if (nonPlanarIntersections.PointCount > intersectPoints.PointCount)
					{
						intersectPoints = nonPlanarIntersections;
					}
				}

				if (intersectPoints.PointCount == 0)
				{
					// this is the case if the line runs along the geometry to reshape but never crosses it
					// this should not happen if GetIntersectionPoints also gets the start/end of the line intersections
					NotificationUtils.Add(Notifications,
					                      "Reshape line runs along part {0} but does not cross it",
					                      partIdx);
				}
				else if (intersectPoints.PointCount == 1)
				{
					if (BothEndsIntersectInSamePoint((ICurve) highLevelReshapePath, intersectPoints)
					)
					{
						canReshape = true;
					}
					else if (GeometryToReshape.GeometryType ==
					         esriGeometryType.esriGeometryPolyline &&
					         AllowOpenJawReshape)
					{
						canReshape = true;
					}
					else
					{
						// consider separate notification only used if intersected parts == 1
						// to avoid huge Warnings in the log
						NotificationUtils.Add(Notifications,
						                      "Reshape line intersects part {0} only once",
						                      partIdx);
					}
				}
				else
				{
					canReshape = true;
				}

				if (canReshape)
				{
					// Save them for later. Intersection is the most expensive operation in the reshape
					IntersectionPoints = intersectPoints;
				}
				else
				{
					Marshal.ReleaseComObject(intersectPoints);
				}
			}

			Marshal.ReleaseComObject(part);
			Marshal.ReleaseComObject(highLevelPart);

			return canReshape;
		}

		public bool IsVerticalRingReshape(int partIdx, out IList<IPath> trimmedVerticalPaths)
		{
			var part = (ICurve) ((IGeometryCollection) GeometryToReshape).get_Geometry(partIdx);

			// Avoid clone of input part:
			IPolyline highLevelPart = GeometryFactory.CreatePolyline(
				(ISegmentCollection) part, true);

			var highLevelReshapePath = (IPolyline)
				GeometryUtils.GetHighLevelGeometry(ReshapePath);

			var intersectPoints =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					highLevelReshapePath, highLevelPart, true);

			GeometryUtils.MakeZAware(highLevelPart);

			var nonPlanarIntersections = (IPointCollection)
				IntersectionUtils.GetIntersectionPointsNonPlanar(
					highLevelReshapePath, highLevelPart);

			bool result = nonPlanarIntersections.PointCount > intersectPoints.PointCount;

			trimmedVerticalPaths = new List<IPath>();

			if (result)
			{
				foreach (IPoint planarPoint in GeometryUtils.GetPoints(intersectPoints))
				{
					// Get all levels in the source by using 2D-intersection test:
					GeometryUtils.MakeNonZAware(planarPoint);

					var verticalPoints = (IPointCollection)
						IntersectionUtils.GetIntersectionPointsNonPlanar(nonPlanarIntersections,
						                                                 planarPoint);

					if (verticalPoints.PointCount < 2)
					{
						continue;
					}

					// NOTE: In order to properly support multiple vertical ring-crossings the specific use cases
					//       should be considered. Assumption: Add a path for each point pair to be (theoretically)
					//       able to select cut-back vs. enlarging reshapes
					List<IPoint> orderedPoints = GeometryUtils.GetPoints(verticalPoints).ToList();

					orderedPoints.Sort((x, y) => y.Z.CompareTo(x.Z));

					for (var i = 0; i < orderedPoints.Count - 1; i++)
					{
						IPath verticalPath = GeometryFactory.CreatePath(orderedPoints[i],
						                                                orderedPoints[++i]);
						GeometryUtils.MakeZAware(verticalPath);

						trimmedVerticalPaths.Add(verticalPath);
					}
				}
			}

			return result;
		}

		public void GetBothSideReshapePolygons(
			out IPolygon leftPolygon, out IPolygon rightPolygon)
		{
			const bool simplifyResult = false;

			GetBothSideReshapePolygons(simplifyResult, out leftPolygon,
			                           out rightPolygon);
		}

		/// <summary>
		/// Gets both reshape side results. The resulting polygon's orientation is positive,
		/// unless the result is multi-part with a ring contained in another. They can be
		/// considered simple enough in order to use relational operator methods, except for
		/// vertical rings, which need special treatement in segment replacement anyway.
		/// </summary>
		/// <param name="simplifyResult">Whether or not the simplify (most costly operation)
		/// should be done or not.</param>
		/// <param name="leftPolygon"></param>
		/// <param name="rightPolygon"></param>
		public void GetBothSideReshapePolygons(bool simplifyResult,
		                                       out IPolygon leftPolygon,
		                                       out IPolygon rightPolygon)
		{
			Assert.NotNull(CutReshapePath, "CutReshapePath is null.");
			Assert.True(
				GeometryToReshape.GeometryType == esriGeometryType.esriGeometryPolygon ||
				GeometryToReshape.GeometryType == esriGeometryType.esriGeometryMultiPatch,
				"No supported geometry");

			IPath cutReshapePath = CutReshapePath.Path;

			if (LeftReshapePolygon != null && RightReshapePolygon != null)
			{
				leftPolygon = LeftReshapePolygon;
				rightPolygon = RightReshapePolygon;

				return;
			}

			var ringToReshape = (IRing) GetGeometryPartToReshape();

			ReshapeBothSides(ringToReshape, cutReshapePath,
			                 out leftPolygon, out rightPolygon, simplifyResult);

			LeftReshapePolygon = leftPolygon;
			RightReshapePolygon = rightPolygon;

			Marshal.ReleaseComObject(ringToReshape);
		}

		/// <summary>
		/// Determines which side should be used by the reshape operation. If it is not
		/// clear which side should be reshaped an (out) result ring is provided that can
		/// be used directly. This is the case when multiple parts would result.
		/// </summary>
		/// <param name="unclearResultRing"></param>
		/// <returns></returns>
		public RingReshapeSideOfLine DetermineReshapeSide(out IRing unclearResultRing)
		{
			unclearResultRing = null;

			if (RingReshapeSide != RingReshapeSideOfLine.Undefined)
			{
				return RingReshapeSide;
			}

			if (AllowSimplifiedReshapeSideDetermination)
			{
				var pointsToReshape = (IPointCollection) GetGeometryPartToReshape();

				if (pointsToReshape.PointCount >
				    _useSimplifiedReshapeSideDeterminationVertexThreshold)
				{
					RingReshapeSide = DetermineLongerReshapeSide();
				}

				Marshal.ReleaseComObject(pointsToReshape);
			}

			if (RingReshapeSide != RingReshapeSideOfLine.Undefined)
			{
				return RingReshapeSide;
			}

			IPolygon leftPoly, rightPoly;

			GetBothSideReshapePolygons(_simplifyOnBothSideReshapes,
			                           out leftPoly, out rightPoly);

			RingReshapeSide = DetermineReshapeSide(
				leftPoly, rightPoly, Notifications);

			if (RingReshapeSide == RingReshapeSideOfLine.Undefined)
			{
				unclearResultRing = GetLargestRingCopy(leftPoly, rightPoly);
			}

			return RingReshapeSide;
		}

		/// <summary>
		/// Determines which side should be used by the reshape operation, if possible.
		/// </summary>
		/// <returns></returns>
		public RingReshapeSideOfLine DetermineReshapeSide()
		{
			IRing unclearRing;

			RingReshapeSideOfLine result = DetermineReshapeSide(out unclearRing);

			if (unclearRing != null)
			{
				Marshal.ReleaseComObject(unclearRing);
			}

			return result;
		}

		/// <summary>
		/// Determines which side should be used by the reshape operation, if possible.
		/// In case both sides result in multiple rings being created, 'Undefined' is returned.
		/// </summary>
		/// <param name="leftPoly"></param>
		/// <param name="rightPoly"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		private RingReshapeSideOfLine DetermineReshapeSide(
			[NotNull] IPolygon leftPoly,
			[NotNull] IPolygon rightPoly,
			[CanBeNull] NotificationCollection notifications)
		{
			if (NonPlanar)
			{
				// RelationalOperator used in GetRingReshapeType results in wrong results.
				// So far the reshape side should generally be controlled by the caller.
				// Otherwise use simplified reshape side determination:
				return DetermineReshapeSideNonPlanar(leftPoly, rightPoly);
			}

			var ringToReshape = (IRing) GetGeometryPartToReshape();

			RingReshapeType = GetRingReshapeType(
				leftPoly, rightPoly, ringToReshape);

			Marshal.ReleaseComObject(ringToReshape);

			return DetermineReshapeSide(leftPoly, rightPoly,
			                            RingReshapeType, notifications);
		}

		private RingReshapeSideOfLine DetermineReshapeSideNonPlanar(
			[NotNull] IPolygon leftPoly,
			[NotNull] IPolygon rightPoly)
		{
			var leftGeoCollection = (IGeometryCollection) leftPoly;
			var rightGeoCollection = (IGeometryCollection) rightPoly;

			int leftRingCount = leftGeoCollection.GeometryCount;
			int rightRingCount = rightGeoCollection.GeometryCount;

			Assert.True(leftRingCount == 1 && rightRingCount == 1,
			            "Unsupported reshape type for non-planar rings.");

			return GetLargerResultSide(leftPoly, rightPoly);
		}

		/// <summary>
		/// Determines which side should be used by the reshape operation, if possible.
		/// In case both sides result in multiple rings being created, null is returned.
		/// </summary>
		/// <param name="leftPoly"></param>
		/// <param name="rightPoly"></param>
		/// <param name="reshapeType"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		private RingReshapeSideOfLine DetermineReshapeSide(
			[NotNull] IPolygon leftPoly,
			[NotNull] IPolygon rightPoly,
			RingReshapeType reshapeType,
			[CanBeNull] NotificationCollection notifications)
		{
			var leftGeoCollection = (IGeometryCollection) leftPoly;
			var rightGeoCollection = (IGeometryCollection) rightPoly;

			var reshapeSide = RingReshapeSideOfLine.Undefined;

			int leftRingCount = leftGeoCollection.GeometryCount;
			int rightRingCount = rightGeoCollection.GeometryCount;

			_msg.DebugFormat(
				"Reshape results: left polygon has {0} rings, right polygon has {1} rings. Reshape type is {2}",
				leftRingCount, rightRingCount, reshapeType);

			Assert.False(leftRingCount == 0 && rightRingCount == 0,
			             "Both reshape options are empty. This is an indication of an invalid reshape curve.");

			// TODO: consider removing duplication of logic with GetRingReshapeType

			bool canUseOtherSide = reshapeType == RingReshapeType.InsideOnly;

			if (leftRingCount == 0 || rightRingCount == 0)
			{
				// One of the reshape options is empty: line runs along the edge of the ring
				reshapeSide = rightPoly.IsEmpty
					              ? RingReshapeSideOfLine.Left
					              : RingReshapeSideOfLine.Right;
			}
			else if (leftRingCount == 1 && rightRingCount == 1)
			{
				if (reshapeType == RingReshapeType.InsideAndOutside)
				{
					if (ReshapePathCrossCutsReplacedSegments(out _))
					{
						// This should not happen, because there is always one of the two with no boundary loops
						NotificationUtils.Add(notifications,
						                      "Ambiguous reshape line might result in boundary loops. The reshape line does not cut the polygon boundary in sequential order");
					}
					else
					{
						NotificationUtils.Add(notifications,
						                      "Self-contradicting reshape line might produce unexpected result or boundary loop");
					}

					NotificationIsWarning = true;

					// allow swapping by the user using non-default side
					reshapeSide = GetLargerResultSide(leftPoly, rightPoly);
					canUseOtherSide = true;
				}
				else
				{
					// it's either an 'outside-only' (one poly includes the original ring, the other not)
					// or an 'inside-only' reshape: in both cases the larger should win by default.

					// use absolute size to handle originally inner rings that were not simplified:

					reshapeSide = GetLargerResultSide(leftPoly, rightPoly);
				}
			}
			else if (leftRingCount > 1 && rightRingCount > 1)
			{
				// it's a wild zig-zag or/and some reshapes are 'covered' by others
				// -> the standard reshape just uses the largest polygon
				NotificationUtils.Add(notifications,
				                      "Reshape would result in several inconsistent parts");
				reshapeSide = RingReshapeSideOfLine.Undefined;
			}
			else if (leftRingCount > 1 || rightRingCount > 1)
			{
				if (reshapeType == RingReshapeType.InsideOnlySeveralParts)
				{
					// This is an inside-only reshape where the path touches (or runs along)
					// the ring from the inside.
					//  __________
					// | \    /   |
					// |  \  /    |
					// |___\/_____|
					//
					// The original polygon is the sqare shape, the reshape line the V-shape
					// This is a border case between inside only -> using the outer 2 parts
					// and inside and outside -> using the inner part

					// but also:
					//  __________
					// | \  /\  / |
					// |  \/  \/  |
					// |__________|
					//

					// NOTE on island rings:
					// For islands this is quite a difficult decision: on an island, both sides can make sense.
					// If the two parts are disjoint, the multi-part solution would be a symmetry to the 
					// cutting-off-island reshape (see below). However the single-part is probably more
					// intuitive in all other cases and then we have a consistent rule that can be swapped
					// by the user (and by autocomplete) to get the other solution.
					// |----------------|
					// |    ________    |
					// |   /  _____ | h |  (the two h are the multipart islands in the multipart-solution)
					// |  /__/  h / |   |
					// |----------------|

					// use the single-part inside rather than the 2 outside parts, but allow swapping
					reshapeSide = rightRingCount > 1
						              ? RingReshapeSideOfLine.Left
						              : RingReshapeSideOfLine.Right;

					canUseOtherSide = true;
				}
				else if (reshapeType == RingReshapeType.OutsideOnly)
				{
					if (rightPoly.ExteriorRingCount == 1 && leftPoly.ExteriorRingCount == 1)
					{
						// this is a cutting-off-island reshape where a new island is created by touching back to
						// the polygon, going along the polygon for a few segments and then returning outside to another touch point
						//   ________
						//  /  _____ |
						// /__/ h  / |
						//---------------
						// The original polygon is below the reshape line (dashed-line ---)
						// h: new hole to be created
						// chose the part with the hole, i.e. with > 1 total parts
						reshapeSide = rightRingCount > 1
							              ? RingReshapeSideOfLine.Right
							              : RingReshapeSideOfLine.Left;
					}
					else
					{
						// this is an outside-only reshape where the reshape line touches the polygon several times
						//  /\  /\
						// /  \/  \
						//---------------
						// The original polygon is below the reshape line (dashed-line ---)
						reshapeSide = rightRingCount > 1
							              ? RingReshapeSideOfLine.Left
							              : RingReshapeSideOfLine.Right;
					}
				}
				else if (reshapeType == RingReshapeType.InsideAndOutsideWithCrossCut)
				{
					// crazy stuff on the brink of zig-zag
					NotificationUtils.Add(notifications,
					                      "Ambiguous reshape line might result in boundary loops. The reshape line does not cut the polygon boundary in sequential order");
					reshapeSide = RingReshapeSideOfLine.Undefined;
				}
				else
				{
					// it is a classic 'outside-and-inside' reshape - the correct side can be clearly determined
					reshapeSide = rightRingCount > 1
						              ? RingReshapeSideOfLine.Left
						              : RingReshapeSideOfLine.Right;
				}
			}
			else
			{
				Assert.CantReach(
					"Unknown situation: Left reshape poly has {0} parts, right reshape poly has {1} parts.",
					leftRingCount, rightRingCount);
			}

			// TODO: Consider checking ReshapeResultFilter.IsReshapeSideAllowed also for non-default side
			//       Currently it is labelled a reshape line filter only, so it is not really expected (or is it?)
			//       Consider a warning at least.
			bool tryNonDefaultSide = ReshapeResultFilter != null &&
			                         (ReshapeResultFilter.UseNonDefaultReshapeSide ||
			                          ! ReshapeResultFilter.IsReshapeSideAllowed(this, reshapeSide,
			                                                                     notifications));

			if (tryNonDefaultSide && reshapeSide != RingReshapeSideOfLine.Undefined)
			{
				if (canUseOtherSide)
				{
					reshapeSide = SwapReshapeSide(reshapeSide);
					NotificationUtils.Add(notifications,
					                      "Reshaped polygon with the non-default side of the reshape line");
				}
				else
				{
					NotificationUtils.Add(notifications,
					                      "The desired override of the reshape side was not honoured to avoid an incorrect result");
					NotificationIsWarning = true;
				}
			}

			_msg.DebugFormat("Reshaping side of line: {0}", reshapeSide);

			if (reshapeSide == RingReshapeSideOfLine.Undefined &&
			    (reshapeType == RingReshapeType.InsideAndOutsideWithCrossCut ||
			     reshapeType == RingReshapeType.Zigzag ||
			     reshapeType == RingReshapeType.InsideOnlySeveralParts))
			{
				// allows warning the user later on
				NotificationIsWarning = true;
			}

			return RingReshapeSide = reshapeSide;
		}

		public RingReshapeSideOfLine GetOppositeReshapeSide()
		{
			if (RingReshapeSide == RingReshapeSideOfLine.Left)
			{
				return RingReshapeSideOfLine.Right;
			}

			if (RingReshapeSide == RingReshapeSideOfLine.Right)
			{
				return RingReshapeSideOfLine.Left;
			}

			return RingReshapeSideOfLine.Undefined;
		}

		public RingReshapeType GetRingReshapeType([NotNull] IPolygon leftPoly,
		                                          [NotNull] IPolygon rightPoly,
		                                          IRing originalRing)
		{
			Assert.False(NonPlanar,
			             "Currently this method is not supported for non-planar reshapes.");

			var leftGeoCollection = (IGeometryCollection) leftPoly;
			var rightGeoCollection = (IGeometryCollection) rightPoly;

			RingReshapeType result;

			int leftRingCount = leftGeoCollection.GeometryCount;
			int rightRingCount = rightGeoCollection.GeometryCount;

			_msg.DebugFormat(
				"Reshape results: left polygon has {0} rings, right polygon has {1} rings.",
				leftRingCount, rightRingCount);

			Assert.False(leftRingCount == 0 && rightRingCount == 0,
			             "Both reshape options are empty. This is an indication of an invalid reshape curve.");

			const bool dontClone = true;
			IGeometry highLevelOriginalRing = GeometryUtils.GetHighLevelGeometry(originalRing,
			                                                                     dontClone);

			bool isInterior = ! originalRing.IsExterior;

			if (isInterior)
			{
				((IPolygon) highLevelOriginalRing).ReverseOrientation();
			}

			try
			{
				result = GetRingReshapeType(leftPoly, rightPoly,
				                            highLevelOriginalRing);
			}
			finally
			{
				// reverse back, because the high-level geometry is not a clone (saving memory)
				if (isInterior)
				{
					((IPolygon) highLevelOriginalRing).ReverseOrientation();
				}
			}

			return result;
		}

		public bool IsReshapePathClosed()
		{
			bool planar = GeometryToReshape.GeometryType !=
			              esriGeometryType.esriGeometryMultiPatch ||
			              ! GeometryUtils.IsZAware(ReshapePath);

			if (planar)
			{
				return ReshapePath.IsClosed;
			}

			return ((ICurve3D) ReshapePath).IsClosed3D;
		}

		/// <summary>
		/// Determines the reshape type. The orientation of the provided rings must be correct
		/// otherwise relational operator returns the wrong results.
		/// </summary>
		/// <param name="leftPoly">The left polygon in correct orientation</param>
		/// <param name="rightPoly">The right polygon in correct orientation</param>
		/// <param name="highLevelExteriorOriginalRing">The original ring in exterior orientation</param>
		/// <returns></returns>
		private RingReshapeType GetRingReshapeType(
			IPolygon leftPoly, IPolygon rightPoly, IGeometry highLevelExteriorOriginalRing)
		{
			int leftRingCount = ((IGeometryCollection) leftPoly).GeometryCount;
			int rightRingCount = ((IGeometryCollection) rightPoly).GeometryCount;

			RingReshapeType reshapeType;
			if (leftRingCount == 0 || rightRingCount == 0)
			{
				// One of the reshape options is empty: line runs along the edge of the ring
				// TODO: test
				reshapeType = RingReshapeType.AlongBoundary;
			}
			else if (leftRingCount == 1 && rightRingCount == 1)
			{
				// TODO: consider performance improvement by comparing areas: leftPoly + rightPoly == originalRing +- Tolerance in case of insideonly
				// TODO: make more explicitly robust regarding non-simple (i.e. inverted rings) input
				//		 so far it just seems to work as the inner ring always has the same orientation as the 
				if (GeometryUtils.Contains(leftPoly, highLevelExteriorOriginalRing) ||
				    GeometryUtils.Contains(rightPoly, highLevelExteriorOriginalRing))
				{
					reshapeType = RingReshapeType.OutsideOnly;
				}
				else if (! GeometryUtils.InteriorIntersects(leftPoly, rightPoly))
				{
					reshapeType = RingReshapeType.InsideOnly;
				}
				else
				{
					// somewhat self-contradicting reshape line (but in some cases correct)
					// outside-reshape that 'goes to the inside as well' -> potential boundary loops
					// CrossCut in one of the two solutions
					//     _
					//    / \
					//   /   \
					//--/--\--\-----
					//      \__\
					// The original polygon is below the horizontal dashed-line: ---

					// or
					//     ___
					//    /   \
					//   / /\  \
					//--/-/--\--\-----
					//        \__\ 
					// The original polygon is below the horizontal dashed-line: ---

					// but also correct and standard:
					//   _                   _
					//  / \                 / \
					//--\--\-----/--        \  \-----/
					//   \      /      ->    \      /    (logical, can use standard solution)
					//    \____/              \____/
					// The original polygon is below the horizontal dashed-line: ---
					reshapeType = RingReshapeType.InsideAndOutside;
				}
			}
			else if (leftRingCount > 1 && rightRingCount > 1)
			{
				// it's a wild zig-zag or and some reshapes are 'covered' by others
				// -> the standard reshape just uses the largest polygon
				reshapeType = RingReshapeType.Zigzag;
			}
			else if (leftRingCount > 1 || rightRingCount > 1)
			{
				if (GeometryUtils.Contains(highLevelExteriorOriginalRing, leftPoly) ||
				    GeometryUtils.Contains(highLevelExteriorOriginalRing, rightPoly))
				{
					// This is an inside-only reshape where the path touches (or runs along)
					// the ring from the inside. Currently handled similar to ZigZag using the largest part.
					//  __________
					// | \    /   |
					// |  \  /    |
					// |___\/_____|
					//
					// The original polygon is the sqare shape, the reshape line the V-shape
					reshapeType = RingReshapeType.InsideOnlySeveralParts;
				}
				else if (GeometryUtils.Contains(leftPoly, highLevelExteriorOriginalRing) ||
				         GeometryUtils.Contains(rightPoly, highLevelExteriorOriginalRing))
				{
					// Cutting off a peninsula (to make it an island): 
					// It is an outside-only reshape where two cutSubcurves touch right on
					// the ring to reshape, i.e. the reshape line goes out of the ring to reshape,
					// then runs along one or more segments of the ring to reshape and then back
					// TODO: consider separate reshape type 'OutsideOnlyCutOffPeninsula'
					reshapeType = RingReshapeType.OutsideOnly;
				}
				else if (ReshapePathCrossCutsReplacedSegments())
				{
					// the reshape line cuts the line to reshape not in proper sequence
					//     _____            
					//    / __  \               
					//   / /  \  \       
					//--/-/-/--\--\----- 
					//    \/    \__\    
					// The original polygon is below the horizontal reshape line (dashed-line ---)
					reshapeType = RingReshapeType.InsideAndOutsideWithCrossCut;
				}
				else
				{
					// it's an 'outside-and-inside' reshape - the correct side can be clearly determined
					reshapeType = RingReshapeType.InsideAndOutside;
				}
			}
			else
			{
				reshapeType = RingReshapeType.Undefined;

				Assert.CantReach(
					"Unknown situation: Left reshape poly has {0} parts, right reshape poly has {1} parts.",
					leftRingCount, rightRingCount);
			}

			return reshapeType;
		}

		private RingReshapeSideOfLine GetLargerResultSide([NotNull] IPolygon leftPoly,
		                                                  [NotNull] IPolygon rightPoly)
		{
			double leftArea, rightArea;
			if (NonPlanar)
			{
				leftArea = Math.Abs(((IArea3D) leftPoly).Area3D);
				rightArea = Math.Abs(((IArea3D) rightPoly).Area3D);
			}
			else
			{
				leftArea = Math.Abs(((IArea) leftPoly).Area);
				rightArea = Math.Abs(((IArea) rightPoly).Area);
			}

			RingReshapeSideOfLine reshapeSide = leftArea > rightArea
				                                    ? RingReshapeSideOfLine.Left
				                                    : RingReshapeSideOfLine.Right;
			return reshapeSide;
		}

		private static RingReshapeSideOfLine SwapReshapeSide(
			RingReshapeSideOfLine reshapeSide)
		{
			if (reshapeSide == RingReshapeSideOfLine.Undefined)
			{
				return RingReshapeSideOfLine.Undefined;
			}

			if (reshapeSide == RingReshapeSideOfLine.Right)
			{
				reshapeSide = RingReshapeSideOfLine.Left;
			}
			else if (reshapeSide == RingReshapeSideOfLine.Left)
			{
				reshapeSide = RingReshapeSideOfLine.Right;
			}

			return reshapeSide;
		}

		/// <summary>
		/// Whether or not both left and right reshape side have intersections with un-changed segments,
		/// i.e. the reshape path cuts back into reshaped segements.
		/// </summary>
		/// <returns></returns>
		private bool ReshapePathCrossCutsReplacedSegments()
		{
			return ReshapePathCrossCutsReplacedSegments(out _);
		}

		/// <summary>
		/// Whether or not both left and right reshape side have intersections with un-changed segments,
		/// i.e. the reshape path cuts back into reshaped segements.
		/// </summary>
		/// <param name="uniqueOkSide">If only one side is ok: The ok side.</param>
		/// <returns></returns>
		private bool ReshapePathCrossCutsReplacedSegments(
			out RingReshapeSideOfLine uniqueOkSide)
		{
			var originalRing = (IRing) GetGeometryPartToReshape();

			IPath reshapePath = Assert.NotNull(CutReshapePath).Path;
			var highLevelReshapePath = (IPolyline)
				GeometryUtils.GetHighLevelGeometry(reshapePath, true);
			// 3 intersesction points: There is always a solution
			// if only one solution of the two crosses the unchanged segments, chose the one
			// - with only one ring, if the other has several rings:
			//   _                   _
			//  / \                 / \
			//--\--\-----/--        \  \-----/
			//   \      /      ->    \      /    (logical, can use standard solution)
			//    \____/              \____/
			//       
			// - or the one with no boundary loop respectively, if both have just one ring:
			//     __                 __
			//    /  \               /  \
			//   /    \        ->   /    \       (not logical, the more logical solution has a boundary loop -> warn!)
			//--/---\--\-----      /---\  \      (for inner rings, the boundary loop solution makes sense)
			//       \__\               \__\

			// 4 intersection points:
			//     ___                 ___
			//    /   \               /   \
			//   / /\  \        ->   / /\  \     (not logical, the logical solution has a boundary loop -> warn!)
			//--/-/--\--\-----      /-/  \  \    (for inner rings, the boundary loop solution makes sense)
			//        \__\                \__\

			// more intersection points, both sides have cut-back -> return true
			// crazy crazy:
			//     _____            
			//    / __  \               
			//   / /  \  \        -> largest result part
			//--/-/-/--\--\----- 
			//    \/    \__\    

			// -> use the other solution (i.e. the one with 1 ring, or the one without boundary loop)
			bool crossesUnchangedSegmentsRightSide =
				CrossesUnchangedSegments(highLevelReshapePath, originalRing,
				                         RingReshapeSideOfLine.Right);

			bool crossesUnchangedSegmentsLeftSide =
				CrossesUnchangedSegments(highLevelReshapePath, originalRing,
				                         RingReshapeSideOfLine.Left);

			bool result = crossesUnchangedSegmentsRightSide && crossesUnchangedSegmentsLeftSide;

			// only one of the two is ok:
			if (crossesUnchangedSegmentsRightSide ^ crossesUnchangedSegmentsLeftSide)
			{
				uniqueOkSide = crossesUnchangedSegmentsLeftSide
					               ? RingReshapeSideOfLine.Right
					               : RingReshapeSideOfLine.Left;
				_msg.DebugFormat(
					"One of the two reshape sides has a reshape line that intersects unchanged segments. Using the other side: {0}",
					uniqueOkSide);
			}
			else
			{
				uniqueOkSide = RingReshapeSideOfLine.Undefined;
			}

			Marshal.ReleaseComObject(highLevelReshapePath);
			Marshal.ReleaseComObject(originalRing);

			return result;
		}

		private static bool CrossesUnchangedSegments(
			IPolyline highLevelReshapePath,
			IRing ringToReshape,
			RingReshapeSideOfLine ringReshapeSideOfLine)
		{
			IPoint startPoint = highLevelReshapePath.FromPoint;
			IPoint endPoint = highLevelReshapePath.ToPoint;

			IPath unchangedSegments =
				SegmentReplacementUtils.GetUnreplacedSegments(ringToReshape, startPoint, endPoint,
				                                              ringReshapeSideOfLine);

			IGeometry highLevelUnchangedSegments =
				GeometryUtils.GetHighLevelGeometry(unchangedSegments, true);

			bool result = GeometryUtils.InteriorIntersects(highLevelUnchangedSegments,
			                                               highLevelReshapePath);

			Marshal.ReleaseComObject(highLevelUnchangedSegments);
			Marshal.ReleaseComObject(unchangedSegments);

			return result;
		}

		private void RepairPartIndexToReshape()
		{
			var geometryCollection = (IGeometryCollection) GeometryToReshape;

			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				IGeometry testPart = geometryCollection.get_Geometry(i);
				if (IsSamePart(testPart, _geometryPartToReshape))
				{
					_partIndexToReshape = i;

					_msg.DebugFormat("Assigned new geometry part index to reshape: {0}",
					                 _partIndexToReshape);
				}

				Marshal.ReleaseComObject(testPart);
			}
		}

		private bool IsSamePart(IGeometry part1, IGeometry part2)
		{
			if (GeometryToReshape.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				// Every call to IGeometryCollection.GeometriesChanged() results in new instances being loaded
				// Fallback to geometric equality
				return GeometryUtils.AreEqual(part1, part2);
			}

			return part1 == part2;
		}

		/// <summary>
		/// Creates both possible solutions to a ring reshape. The created polygons are
		/// simple (i.e. they can have different orientation that the input ring) but 
		/// have intermediate phantom intersection points.
		/// </summary>
		/// <param name="ring"></param>
		/// <param name="replacement"></param>
		/// <param name="leftPolygon"></param>
		/// <param name="rightPolygon"></param>
		/// <param name="simplifyBothResults"></param>
		private void ReshapeBothSides([NotNull] IRing ring,
		                              [NotNull] IPath replacement,
		                              [NotNull] out IPolygon leftPolygon,
		                              [NotNull] out IPolygon rightPolygon,
		                              bool simplifyBothResults)
		{
			// NOTE: IRing2.ReshapeEx crashes ArcMap (not only in non-simple situations) and interpolates Z values
			// NOTE: just replacing segments without intersection-point-removal is almost free
			//		 -> simplify is the most expensive part.
			leftPolygon = GeometryFactory.CreatePolygon(ring);
			rightPolygon = GeometryFactory.CreatePolygon(ring);

			var leftRing = (IRing) ((IGeometryCollection) leftPolygon).get_Geometry(0);
			var rightRing = (IRing) ((IGeometryCollection) rightPolygon).get_Geometry(0);

			// important: if no simplify is done, we have to make sure the replacement path is a copy
			// otherwise we'll end up with inconsistent orientations and wrong results in the real reshape

			IPath leftReplacement = GeometryFactory.Clone(replacement);
			IPath rightReplacement = GeometryFactory.Clone(replacement);

			SegmentReplacementUtils.ReplaceSegments(leftRing, leftReplacement,
			                                        RingReshapeSideOfLine.Left, NonPlanar, null);

			SegmentReplacementUtils.ReplaceSegments(rightRing, rightReplacement,
			                                        RingReshapeSideOfLine.Right, NonPlanar,
			                                        null);

			// NOTE: The result of replaced segments can have an incorrect orientation or undetected rings,
			//       e.g. a simple InsideAndOutside reshape (geometry count always 1)
			// TODO: Remove simplifyBothResults for good
			if (simplifyBothResults)
			{
				Stopwatch watchSimplify = _msg.DebugStartTiming();

				GeometryUtils.Simplify(leftPolygon);
				GeometryUtils.Simplify(rightPolygon);

				_msg.DebugStopTiming(watchSimplify, "Simplified both possible results");
			}
			else if (! NonPlanar)
			{
				EnsureRingCountAndOrientation(leftPolygon, ring, replacement,
				                              RingReshapeSideOfLine.Left);
				EnsureRingCountAndOrientation(rightPolygon, ring, replacement,
				                              RingReshapeSideOfLine.Right);
			}

			Marshal.ReleaseComObject(leftRing);
			Marshal.ReleaseComObject(rightRing);
		}

		private static void EnsureRingCountAndOrientation(IPolygon ofResultPoly,
		                                                  IRing originalRing,
		                                                  IPath reshapePath,
		                                                  RingReshapeSideOfLine reshapeSide)
		{
			if (NeedsSimplify(originalRing, reshapePath, reshapeSide))
			{
				GeometryUtils.Simplify(ofResultPoly);

				_msg.DebugFormat("Simplified {0} polygon", reshapeSide);
			}
			else if (((IArea) ofResultPoly).Area < 0)
			{
				ofResultPoly.ReverseOrientation();

				int ringCount = AssertCanGetRingCount(ofResultPoly);

				_msg.DebugFormat("Reversed orientation of {0} polygon. It has {1} exterior rings",
				                 reshapeSide, ringCount);
			}
			else
			{
				_msg.DebugFormat("No simplify needed for {0} polygon, checking ring count...",
				                 reshapeSide);

				// necessary for this (https://issuetracker02.eggits.net/browse/TOP-4484):
				//  /\  /\
				// /  \/  \
				//---------------
				int ringCount = AssertCanGetRingCount(ofResultPoly);

				_msg.DebugFormat("{0} rings", ringCount);
			}
		}

		private static int AssertCanGetRingCount(IPolygon polygon)
		{
			return GeometryUtils.GetExteriorRingCount(polygon);
		}

		private static bool NeedsSimplify(IRing originalRing, IPath replacementPath,
		                                  RingReshapeSideOfLine reshapeSide)
		{
			var highLevelReplacement =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(replacementPath, true);

			return CrossesUnchangedSegments(highLevelReplacement, originalRing,
			                                reshapeSide);
		}

		private static bool BothEndsIntersectInSamePoint(ICurve reshapePath,
		                                                 IPointCollection intersectPoints)
		{
			bool intersectsTwiceInSamePoint;

			// only support closed reshape path touching ring in one point
			// NOTE: if it's not the from and to points we shouldn't get here because non-simple restriction
			if (GeometryUtils.AreEqual(reshapePath.FromPoint, reshapePath.ToPoint) &&
			    GeometryUtils.AreEqual(reshapePath.FromPoint, intersectPoints.get_Point(0)))
			{
				intersectsTwiceInSamePoint = true;
			}
			else
			{
				intersectsTwiceInSamePoint = false;
			}

			return intersectsTwiceInSamePoint;
		}

		private static IRing GetLargestRingCopy(params IPolygon[] polygons)
		{
			IArea largestRing = null;

			foreach (IPolygon polygon in polygons)
			{
				foreach (
					IGeometry geometry in GeometryUtils.GetParts((IGeometryCollection) polygon))
				{
					var currentRing = (IArea) geometry;

					if (largestRing == null || currentRing.Area > largestRing.Area)
					{
						largestRing = currentRing;
					}
				}
			}

			if (largestRing != null)
			{
				return GeometryFactory.Clone((IRing) largestRing);
			}

			return null;
		}

		private RingReshapeSideOfLine DetermineLongerReshapeSide()
		{
			RingReshapeSideOfLine result;

			IPath cutReshapePath = Assert.NotNull(CutReshapePath).Path;

			var ringToReshape = (IRing) GetGeometryPartToReshape();

			const double maxReplacementToRingRatio = 0.05;

			if (cutReshapePath.Length / ringToReshape.Length > maxReplacementToRingRatio)
			{
				// be on the very safe side - only do this if it is absolutely clear
				result = RingReshapeSideOfLine.Undefined;
			}
			else
			{
				double firstPointDistance = GeometryUtils.GetDistanceAlongCurve(
					ringToReshape, cutReshapePath.FromPoint, false);

				double secondPointDistance = GeometryUtils.GetDistanceAlongCurve(
					ringToReshape, cutReshapePath.ToPoint, false);

				NotificationUtils.Add(Notifications,
				                      "Used simplified determination of reshape side");

				result = GetLongerRemainingSide(ringToReshape, firstPointDistance,
				                                secondPointDistance, maxReplacementToRingRatio);
			}

			Marshal.ReleaseComObject(ringToReshape);

			return result;
		}

		/// <summary>
		/// Returns the longer remaining side if the ratio between replacement to total
		/// length of the ring is smaller than the specified maximum replacement-to-ring
		/// ratio.
		/// </summary>
		/// <param name="ringToReshape"></param>
		/// <param name="firstCutPointDistance"></param>
		/// <param name="secondCutPointDistance"></param>
		/// <param name="maxReplacementToRingRatio"></param>
		/// <returns></returns>
		private static RingReshapeSideOfLine GetLongerRemainingSide(
			IRing ringToReshape, double firstCutPointDistance, double secondCutPointDistance,
			double maxReplacementToRingRatio)
		{
			var pointsWereSwapped = false;
			if (firstCutPointDistance > secondCutPointDistance)
			{
				double secondTemp = secondCutPointDistance;
				secondCutPointDistance = firstCutPointDistance;
				firstCutPointDistance = secondTemp;

				pointsWereSwapped = true;
			}

			double leftSideLength = secondCutPointDistance - firstCutPointDistance;
			double rightSideLength = firstCutPointDistance +
			                         (ringToReshape.Length - secondCutPointDistance);

			double replacementRatio = leftSideLength / ringToReshape.Length;

			//const double maxReplacementToRingRatio = 0.1;
			if (replacementRatio > maxReplacementToRingRatio &&
			    replacementRatio < 1.0 - maxReplacementToRingRatio)
			{
				// be extremely conservative: if the two options are anywhere close to each other
				// -> use conventional mechanism.
				// Normally we only get here for extremely large rings and therefore for normal 
				// reshape lines the ratio is typically 0.01 or smaller (0.99 or higher)
				return RingReshapeSideOfLine.Undefined;
			}

			RingReshapeSideOfLine result;
			if (leftSideLength > rightSideLength)
			{
				result = RingReshapeSideOfLine.Left;
			}
			else
			{
				result = RingReshapeSideOfLine.Right;
			}

			if (pointsWereSwapped)
			{
				result = result == RingReshapeSideOfLine.Left
					         ? RingReshapeSideOfLine.Right
					         : RingReshapeSideOfLine.Left;
			}

			return result;
		}

		#region Implementation of IDisposable

		public void Dispose()
		{
			if (ReshapePath != null)
			{
				Marshal.ReleaseComObject(ReshapePath);
				ReshapePath = null;
			}

			if (CutReshapePath != null)
			{
				Marshal.ReleaseComObject(CutReshapePath.Path);
				CutReshapePath = null;
			}

			if (LeftReshapePolygon != null)
			{
				Marshal.ReleaseComObject(LeftReshapePolygon);
				LeftReshapePolygon = null;
			}

			if (RightReshapePolygon != null)
			{
				Marshal.ReleaseComObject(RightReshapePolygon);
				RightReshapePolygon = null;
			}

			if (IntersectionPoints != null)
			{
				Marshal.ReleaseComObject(IntersectionPoints);
				IntersectionPoints = null;
			}

			if (ReplacedSegments != null)
			{
				Marshal.ReleaseComObject(ReplacedSegments);
				ReplacedSegments = null;
			}

			if (_geometryPartToReshape != null)
			{
				_msg.DebugFormat("Remaining references to geometryPartToReshape: {0}",
				                 Marshal.ReleaseComObject(_geometryPartToReshape));
			}
		}

		#endregion

		public bool ValidateReshapePath()
		{
			if (ReshapePath.IsEmpty)
			{
				NotificationUtils.Add(Notifications, "Reshape path is empty");
				return false;
			}

			if (IsReshapePathClosed())
			{
				// The definition of closed is FromPoint equals ToPoint which is true even for
				// segments that are longer than the tolerance but dX < tolerance && dY < tolerance
				if (ReshapePath.Length < GeometryUtils.GetXyTolerance(ReshapePath) * Math.Sqrt(2))
				{
					// If such a reshape path would be used, the entire ring would be replaced by this mini-segment
					// or, if it was used as regular path, only one intersection would be found by topological operator.
					NotificationUtils.Add(Notifications, "Reshape path is too short");
					return false;
				}
			}

			return true;
		}

		public void GeometryChanged()
		{
			int? partIdxToReshape = PartIndexToReshape;
			// NOTE: Multipatches create new instances of geometry parts, when calling this:
			((IGeometryCollection) GeometryToReshape).GeometriesChanged();

			// re-load _geometryPartToReshape cache
			PartIndexToReshape = partIdxToReshape;
		}
	}

	public enum RingReshapeSideOfLine
	{
		Undefined,
		Left,
		Right
	}

	public enum RingReshapeType
	{
		Undefined,

		/// <summary>
		/// The reshape path adds an area to the ring to reshape.
		/// </summary>
		OutsideOnly,

		/// <summary>
		/// The reshape path crosses the ring to reshape once and cuts it
		/// into two parts.
		/// </summary>
		InsideOnly,

		/// <summary>
		/// The reshape path crosses the ring to reshape more than once and cuts
		/// the ring to reshape into 3 or more parts.
		/// </summary>
		InsideOnlySeveralParts,

		/// <summary>
		/// The reshape path runs exactly along the boundary and, if at all, only 
		/// differs in Z values.
		/// </summary>
		AlongBoundary,

		/// <summary>
		/// Reshape path cuts into the ring and adds area(s) to the ring.
		/// </summary>
		InsideAndOutside,

		/// <summary>
		/// A not-quite logical reshape path that does not cut the polygon boundary
		/// in sequential order but cuts back into segments already replaced by a 
		/// different cut-subcurve. Such reshape paths are somewhat self-contradictory
		/// and can result in a different result polygon depending on the direction
		/// one looks at the reshape path.
		/// The result contains boundary loops or is multipart!
		/// </summary>
		InsideAndOutsideWithCrossCut,

		/// <summary>
		/// An degenerate reshape path crosses the ring to reshape in a way that
		/// cuts it into several parts.
		/// </summary>
		Zigzag
	}
}

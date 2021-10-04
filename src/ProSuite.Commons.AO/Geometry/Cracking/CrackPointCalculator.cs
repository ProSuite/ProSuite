using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public class CrackPointCalculator
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private IEnvelope _envelopeTemplate1;
		private IEnvelope _envelopeTemplate2;

		public CrackPointCalculator([NotNull] ICrackingOptions crackingOptions,
		                            IntersectionPointOptions intersectionPointOption,
		                            [CanBeNull] IEnvelope perimeter)
		{
			Perimeter = perimeter;

			if (crackingOptions.SnapToTargetVertices)
			{
				// If 0.0 -> use the tolerance - disallow any snap tolerance < XY tolerance?
				SnapTolerance = crackingOptions.SnapTolerance > 0
					                ? (double?) crackingOptions.SnapTolerance
					                : null;
			}

			if (crackingOptions.RespectMinimumSegmentLength)
			{
				// If 0.0 -> use the tolerance - disallow any segment length < XY tolerance?
				MinimumSegmentLength = crackingOptions.MinimumSegmentLength > 0
					                       ? (double?) crackingOptions.MinimumSegmentLength
					                       : null;
			}

			UseSourceZs = crackingOptions.UseSourceZs;
			IntersectionPointOption = intersectionPointOption;
		}

		public CrackPointCalculator(double? snapTolerance, double? minimumSegmentLength,
		                            bool addCrackPointsOnExistingVertices, bool useSourceZs,
		                            IntersectionPointOptions intersectionPointOption,
		                            [CanBeNull] IEnvelope perimeter)
		{
			Perimeter = perimeter;
			SnapTolerance = snapTolerance;
			MinimumSegmentLength = minimumSegmentLength;
			AddCrackPointsOnExistingVertices = addCrackPointsOnExistingVertices;
			UseSourceZs = useSourceZs;
			IntersectionPointOption = intersectionPointOption;
		}

		[CanBeNull]
		public IEnvelope Perimeter { get; set; }

		public double? SnapTolerance { get; set; }

		public double? MinimumSegmentLength { get; set; }

		/// <summary>
		/// Whether or not a crack vertex should also be added if there is already a vertex in
		/// the source. Needed e.g. in 'chopping-mode' to allow cutting features at existing vertices.
		/// </summary>
		public bool AddCrackPointsOnExistingVertices { get; set; }

		public bool UseSourceZs { get; set; }

		public IntersectionPointOptions IntersectionPointOption { get; set; }

		// TODO: invert logic, rename to MultipatchCracking or PlanarCracking or CrackAllSegmentsRegardlessOfZ or CrackAtExpectedSelfIntersectingSegments
		public bool In3D { get; set; }

		public double? DataXyResolution { get; set; }

		public double? DataZResolution { get; set; }

		[CanBeNull]
		public Func<IGeometry, IGeometry> TargetTransformation { get; set; }

		private IPointCollection AllCrackPoints { get; set; }

		public IDictionary<int, string> FailedOperations { get; } =
			new Dictionary<int, string>();

		public bool ContinueOnException { get; set; }

		public bool UseCustomIntersect { get; set; } = true;

		/// <summary>
		/// Sets the current data resolution to the resolution of the provided feature's feature class. This improves
		/// the detection of almost-coincident points (closer than tolerance) in ArcMap (where the resolution is 
		/// artificially minimized).
		/// </summary>
		/// <param name="forFeature"></param>
		public void SetDataResolution(IFeature forFeature)
		{
			DataXyResolution = GeometryUtils.GetXyResolution(forFeature);

			var featureClass = (IFeatureClass) forFeature.Class;

			if (DatasetUtils.HasZ(featureClass))
			{
				ISpatialReference spatialReference = DatasetUtils.GetSpatialReference(featureClass);

				Assert.NotNull(spatialReference);
				DataZResolution = GeometryUtils.GetZResolution(spatialReference);
			}
		}

		/// <summary>
		/// Calculates the intersection points as accurately as possible by the current minimum tolerance.
		/// If a <see cref="TargetTransformation"/> is set the transformed intersection target will be used
		/// and returned as out parameter.
		/// </summary>
		/// <param name="sourceGeometry"></param>
		/// <param name="intersectionTarget"></param>
		/// <param name="transformedIntersectionTarget"></param>
		/// <returns></returns>
		[NotNull]
		public IPointCollection GetIntersectionPoints(
			[NotNull] IPolyline sourceGeometry,
			[NotNull] IGeometry intersectionTarget,
			[CanBeNull] out IGeometry transformedIntersectionTarget)
		{
			if (TargetTransformation != null)
			{
				transformedIntersectionTarget = TargetTransformation(intersectionTarget);

				if (transformedIntersectionTarget == null)
				{
					IGeometry emptyResult =
						GeometryFactory.CreateEmptyGeometry(
							esriGeometryType.esriGeometryMultipoint);

					emptyResult.SpatialReference = sourceGeometry.SpatialReference;

					return (IPointCollection) emptyResult;
				}
			}
			else
			{
				transformedIntersectionTarget = intersectionTarget;
			}

			IPointCollection result;

			bool origIntersect = IntersectionUtils.UseCustomIntersect;

			try
			{
				if (UseCustomIntersect)
				{
					IntersectionUtils.UseCustomIntersect = true;
				}

				result = GetIntersectionPoints(sourceGeometry, transformedIntersectionTarget);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = origIntersect;
			}

			return result;
		}

		public IList<CrackPoint> DetermineCrackPoints(
			[NotNull] IPointCollection intersectionPoints,
			[NotNull] IGeometry originalGeometry,
			[NotNull] IPolyline optimizedPolyline,
			[CanBeNull] IGeometry snapTarget)
		{
			var result = new List<CrackPoint>(intersectionPoints.PointCount);

			// TODO: re-verify this
			// for multipatch-ring comparison it's important to check against the full geometry
			// for other cases (only cracking between features) this could be the optimized polyline:
			IMultipoint originalPoints =
				GeometryFactory.CreateMultipoint((IPointCollection) originalGeometry);

			for (var i = 0; i < intersectionPoints.PointCount; i++)
			{
				IPoint point = intersectionPoints.Point[i];

				if (Perimeter != null && GeometryUtils.Disjoint(Perimeter, point))
				{
					continue;
				}

				var targetVertexDifferentInZ = false;
				var targetVertexDifferentWithinTolerance = false;

				if (AddCrackPointsOnExistingVertices)
				{
					// if chopping: only skip crack points that are line end points
					var originalPolyline = originalGeometry as IPolyline;

					// except on end points of the original polylines
					if (originalPolyline != null &&
					    (GeometryUtils.AreEqualInXY(originalPolyline.FromPoint, point) ||
					     GeometryUtils.AreEqualInXY(originalPolyline.ToPoint, point)))
					{
						continue;
					}
				}
				else
				{
					// if not chopping: skip crack points that are existing vertices
					bool hasSourcePoint = HasMatchingPoint(originalPoints, point,
					                                       out targetVertexDifferentWithinTolerance,
					                                       out targetVertexDifferentInZ);

					// Create only 1 crack point per XY location (which will be used to crack all segments at all Z-levels)
					bool hasUncrackedSegment = HasUncrackedExtraMultipatchSegments(
						optimizedPolyline, point);

					// Uncracked segments always win over (almost-matching) points
					if (hasUncrackedSegment)
					{
						targetVertexDifferentInZ = false;
						targetVertexDifferentWithinTolerance = false;
					}

					if (hasSourcePoint && ! targetVertexDifferentWithinTolerance &&
					    ! hasUncrackedSegment)
					{
						// there is a perfectly matching source point and no other crackable segments
						continue;
					}
				}

				IPoint pointToInsert = Snap(point, snapTarget);

				CrackPoint crackPoint = CreateCrackPoint(point, pointToInsert, optimizedPolyline,
				                                         targetVertexDifferentInZ,
				                                         targetVertexDifferentWithinTolerance);

				result.Add(crackPoint);

				// also add to planar crack points
				if (! crackPoint.ViolatesMinimumSegmentLength)
				{
					if (AllCrackPoints == null)
					{
						AllCrackPoints =
							(IPointCollection) GeometryFactory.CreateEmptyMultipoint(
								originalGeometry);
					}

					TryAddCrackPoint(crackPoint, AllCrackPoints);
				}
			}

			return result;
		}

		/// <summary>
		/// Whether it is theoretically impossible that these two geometries have an intersection with respect to 
		/// the current snap tolerance.
		/// </summary>
		/// <param name="geometry1"></param>
		/// <param name="geometry2"></param>
		/// <returns></returns>
		public bool CannotIntersect([CanBeNull] IGeometry geometry1,
		                            [CanBeNull] IGeometry geometry2)
		{
			// NOTE: RelationalOperator.Disjoint on polylines tends to miss touching lines that are disjoint 
			//       by a distance smaller than the tolerance and therefore are not disjoint! Their envelopes 
			//       do not intersect calculated in absolute numbers but relational operator correctly finds
			//       that they intersect.

			// Calculate certainly-disjoint geometries using the envelope (w.r.t. large tolerance), the rest must be intersected

			if (geometry1 == null || geometry1.IsEmpty ||
			    geometry2 == null || geometry2.IsEmpty)
			{
				return true;
			}

			if (_envelopeTemplate1 == null)
			{
				_envelopeTemplate1 = geometry2.Envelope;
			}
			else
			{
				geometry2.QueryEnvelope(_envelopeTemplate1);
			}

			if (_envelopeTemplate2 == null)
			{
				_envelopeTemplate2 = geometry1.Envelope;
			}
			else
			{
				geometry1.QueryEnvelope(_envelopeTemplate2);
			}

			double largeTolerance = SnapTolerance ?? GeometryUtils.GetXyTolerance(geometry1);

			bool envelopesDisjoint = GeometryUtils.Disjoint(
				_envelopeTemplate1, _envelopeTemplate2, largeTolerance);

			return envelopesDisjoint;
		}

		/// <summary>
		/// Whether there are additional segments (apart from an expected vertex) that are not cracked
		/// at the specified point. This makes sense for multipatch cracking, where geometries self-intersect
		/// at various Z-levels.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="atPoint"></param>
		/// <returns></returns>
		private bool HasUncrackedExtraMultipatchSegments([NotNull] IPolyline sourceSegments,
		                                                 [NotNull] IPoint atPoint)
		{
			if (In3D)
			{
				return false;
			}

			if (UseSourceZs)
			{
				double xyTolerance = SnapTolerance ??
				                     GeometryUtils.GetXyTolerance(sourceSegments);

				// check for segments that are not cracked, even though there are other good vertices at different Z in the same part
				IList<int> unsplitSegments =
					GeometryUtils.FindSegmentIndices(
						(ISegmentCollection) sourceSegments,
						atPoint, xyTolerance, true);

				return unsplitSegments.Count > 0;
			}

			return false;
		}

		private bool HasMatchingPoint([NotNull] IMultipoint originalPoints,
		                              [NotNull] IPoint atPoint,
		                              out bool differentWithinTolerance,
		                              out bool differentInZ)
		{
			differentWithinTolerance = false;
			differentInZ = false;

			if (! GeometryUtils.Contains(originalPoints, atPoint))
			{
				return false;
			}

			IList<IPoint> existingPoints =
				GeometryUtils.FindPoints(originalPoints, atPoint,
				                         GeometryUtils.GetXyTolerance(originalPoints)).ToList();

			if (existingPoints.Count == 0)
			{
				// NOTE: This sometimes happens because IRelationalOperator.Contains is too relaxed regarding the tolerance
				//       -> RelationalOperator seems to check the tolerance against delta-x and delta-y separately
				_msg.DebugFormat(
					"The point is not found in the multipoint. Point: {0}, Multipoint: {1}",
					GeometryUtils.ToString(atPoint), GeometryUtils.ToString(originalPoints));

				return false;
			}

			double xyResolution = GetXyResolution(atPoint);
			double zResolution = GetZResolution(atPoint);

			var pointFound = false;
			foreach (IPoint existingPoint in existingPoints)
			{
				bool thisPointWithinTolerance, thisPointDifferentInZ;
				if (IsPerfectlyMatching(
					atPoint, existingPoint, xyResolution, zResolution, out thisPointWithinTolerance,
					out thisPointDifferentInZ))
				{
					pointFound = true;
					continue;
				}

				if (thisPointWithinTolerance)
				{
					// always fix points that are almost coincident:
					differentWithinTolerance = true;
				}

				if (! In3D)
				{
					// 2D intersection is good enough
					continue;
				}

				if (thisPointDifferentInZ)
				{
					// perfect match
					differentInZ = true;
				}
			}

			return pointFound;
		}

		/// <summary>
		/// Determines whether the two points that are expected to be expected relational-operator-equal
		/// are exactly equal (i.e. equal to the point that they would end up exactly on store)
		/// </summary>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <param name="xyResolution"></param>
		/// <param name="zResolution"></param>
		/// <param name="differentWithinTolerance"></param>
		/// <param name="differentInZ"></param>
		/// <returns></returns>
		private static bool IsPerfectlyMatching([NotNull] IPoint point1,
		                                        [NotNull] IPoint point2,
		                                        double xyResolution, double zResolution,
		                                        out bool differentWithinTolerance,
		                                        out bool differentInZ)
		{
			// within resolution is ok, there can be very small differences
			// but in the real world they can be 0.99999 of the resolution (see unit test
			// CanCalculateCrackPoints_Multipatch_UnsnappedVerticesDifferentBy1Resolution)
			// The resolution of the geometry is sometimes (FGDB in issues.mxd) the data resolution
			// and sometimes (SDE) a very small number.
			// Half a resolution seems safe and reasonable

			differentInZ = false;

			// The crack point can be exactly between the two vertices that are different by 1 resolution:
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(point1.X, point1.Y);
			double equalToleranceXY = xyResolution / 2 - epsilon;

			differentWithinTolerance =
				! MathUtils.AreEqual(point1.X, point2.X, equalToleranceXY) ||
				! MathUtils.AreEqual(point1.Y, point2.Y, equalToleranceXY);

			if (GeometryUtils.IsZAware(point2) && GeometryUtils.IsZAware(point1))
			{
				double dZ = Math.Abs(point1.Z - point2.Z);

				differentInZ = dZ >= zResolution / 2 - epsilon;

				differentWithinTolerance |= differentInZ &&
				                            dZ < GeometryUtils.GetZTolerance(point1);
			}

			return ! differentWithinTolerance && ! differentInZ;
		}

		private static void TryAddCrackPoint(CrackPoint crackPoint,
		                                     IPointCollection toPlanarCrackPoints)
		{
			if (crackPoint.ViolatesMinimumSegmentLength)
			{
				return;
			}

			// Add all points, they should already coincide in XY if snapped, in Z they should be allowed to differ (3D cracking)
			AddPoint(crackPoint.Point, toPlanarCrackPoints);
		}

		[NotNull]
		private IPointCollection GetIntersectionPoints(
			[NotNull] IPolyline sourceGeometry,
			[NotNull] IGeometry intersectionTarget)
		{
			// NOTE: Expecially if the two features come from a different spatial reference the intersection
			//		 point can be quite far off the actual points, which can result in Crack points still being
			//		 shown even though they got already inserted. Setting the minimum tolerance helps against this
			//		 However, e.g. when a line ends on the interior of another line, setting the minimum tolerance
			//		 can result in no intersection point being found!
			// TODO: Use large (snap distance?) tolerance to find all points. Then re-place the points using the minimum tolerance if they are still found
			//		 until then: set minimum tolerance only if not snapping

			// TODO: if snapping, consider adapting tolerance to snap distance to find 'near hits'
			// but that means also identifying the closest point on the source to start snapping from

			// The problem with the minimum snap tolerance is that it results in no-intersections between
			// touching geometries!

			double originalTolerance = GeometryUtils.GetXyTolerance(intersectionTarget);

			// minimal tolerance results in less unexpected intersection points if 
			// the cut point is close (within tolerance) to a vertex of the target (in between!)
			// This is more prone to happen at small-angle intersections!

			// However: using minimal tolerance misses some end points, especially with touching polygons

			// the ideal solution might be to get the crossing points with the small tolerance but the touching points 
			// (including polygon outline's) with the normal tolerance or even the snap tolerance

			SetMinimumTolerance(intersectionTarget);
			SetMinimumTolerance(sourceGeometry);

			// get the Z values from the targets -> first geometry
			IPointCollection smallToleranceIntersections =
				GetIntersectionPoints(intersectionTarget, sourceGeometry, IntersectionPointOption, out _);

			bool useSnapping = SnapTolerance != null &&
			                   ! double.IsNaN((double) SnapTolerance) &&
			                   SnapTolerance > 0.0;

			double largeTolerance = useSnapping
				                        ? (double) SnapTolerance
				                        : originalTolerance;

			try
			{
				//// NOTE: The large-tol. intersection points are not in the same position as the (same) small-tol intersection points
				////		 The respective points can differ by a large distance (spiky angles!) easily more than 6 times the XY tolerance
				////		 additionally 2 intersection points (depending on the vertex locations) can be reported at large tolerances, even
				////		 though in reality there is just one.
				////		 -> Therefore use small tolerance intersections if there are any and only snap if there are no small-tolerance
				////		 intersections. This also improves performance over getting and comparing the two intersection sets. The disadvantage 
				////		 is that in the almost-intersecting point is not found if there is another intersection
				////		 The alternative would be to find intersecting segments with the large tolerance and then do a small-tolerance intersection on them
				// TODO: Change the implementation of get intersection points completely to get
				// - exact intersections in the first place
				// - the option to specify the search (cluster) tolerance when testing vertices against (close but non-intersecting segments)
				SetTolerance(intersectionTarget, largeTolerance);
				SetTolerance(sourceGeometry, largeTolerance);

				IPolyline largeToleranceIntersectionLines;
				IPointCollection largeToleranceIntersections =
					GetLargeToleranceIntersectionPoints(sourceGeometry, intersectionTarget,
					                                    originalTolerance,
					                                    out largeToleranceIntersectionLines);

				RemoveSmallToleranceIntersectionPointsOnLinearIntersections(
					smallToleranceIntersections, largeToleranceIntersectionLines);

				// NOTE: Check every single large-tolerance intersection. It could be that the intersection point count is equal
				//       but the points are in very different locations (small tolerance intersection also reports wrong points)
				_msg.VerboseDebugFormat(
					"Comparing large tolerance intersecion points with small tolerance: {0}. Intersection points with (snap-)tolerance: {1}",
					smallToleranceIntersections.PointCount, largeToleranceIntersections.PointCount);

				for (var i = 0; i < largeToleranceIntersections.PointCount; i++)
				{
					IPoint snappedExtraPoint =
						GetSnappedExtraPoint(largeToleranceIntersections.get_Point(i),
						                     sourceGeometry,
						                     intersectionTarget, largeTolerance,
						                     smallToleranceIntersections);

					if (snappedExtraPoint != null)
					{
						_msg.VerboseDebugFormat(
							"Using extra large-tolerance intersection point at {0} | {1}",
							snappedExtraPoint.X, snappedExtraPoint.Y);

						AddPoint(snappedExtraPoint, smallToleranceIntersections);
					}
				}
			}
			finally
			{
				SetTolerance(intersectionTarget, originalTolerance);
				SetTolerance(sourceGeometry, originalTolerance);
				SetTolerance((IGeometry) smallToleranceIntersections, originalTolerance);
			}

			return smallToleranceIntersections;
		}

		private static void RemoveSmallToleranceIntersectionPointsOnLinearIntersections(
			IPointCollection smallToleranceIntersections,
			[CanBeNull] IPolyline largeToleranceIntersectionLines)
		{
			if (largeToleranceIntersectionLines == null)
			{
				return;
			}

			GeometryUtils.RemovePoints(
				smallToleranceIntersections,
				point => GeometryUtils.InteriorIntersects(largeToleranceIntersectionLines, point));
		}

		[NotNull]
		private IPointCollection GetLargeToleranceIntersectionPoints(
			[NotNull] IPolyline sourceGeometry,
			[NotNull] IGeometry intersectionTarget,
			double originalTolerance,
			[CanBeNull] out IPolyline linearIntersections)
		{
			IPointCollection result;

			linearIntersections = null;

			try
			{
				result = GetIntersectionPoints(intersectionTarget, sourceGeometry,
				                               IntersectionPointOption, out linearIntersections);
			}
			catch (COMException comException)
			{
				// This exception has started to appear quite often with small geometries and relatively
				// large snap tolerance (10.2?). It is not sufficient to ensure the tolerance does not 
				// exceed some ration to length / envelope size.
				// Once we have a better intersection point implementation (multipoint - segment 
				// collection with tolerance) this is all obsolete anyway
				// WORKAROUND:
				_msg.Debug("Exception intersecting geometries with large tolerance", comException);

				const int xyClusterTolTooLargeForExtent = -2147220888;

				if (comException.ErrorCode == xyClusterTolTooLargeForExtent)
				{
					SetTolerance(sourceGeometry, originalTolerance);
					SetTolerance(intersectionTarget, originalTolerance);

					result = GetIntersectionPoints(intersectionTarget, sourceGeometry,
					                               IntersectionPointOption,
					                               out linearIntersections);
				}
				else
				{
					throw;
				}
			}

			return result;
		}

		[NotNull]
		private IPoint Snap([NotNull] IPoint point, [CanBeNull] IGeometry intersectedTarget)
		{
			double snapTolerance = SnapTolerance ?? GeometryUtils.GetXyTolerance(point);

			// if the local crack point is exactly on an existing crack point (AllCrackPoints)
			//    -> Use this matching previously created crack point to avoid swapping Z values
			bool sourcePointDifferentToCrackPoint;
			IPoint crackPointAtSourcePoint = FindCrackPoint(
				AllCrackPoints, point, snapTolerance, out sourcePointDifferentToCrackPoint);

			if (crackPointAtSourcePoint != null && ! sourcePointDifferentToCrackPoint)
			{
				return crackPointAtSourcePoint;
			}

			// snap to the target geometry's *vertices* (consider snapping to edges too?)
			IPoint snappedPoint = SnapToGeometry(point, intersectedTarget, snapTolerance,
			                                     esriGeometryHitPartType.esriGeometryPartVertex);

			if (snappedPoint != null)
			{
				if (UseSourceZs)
				{
					// fix Z
					snappedPoint.Z = point.Z;
				}

				// if the target point is exactly on an existing crack point -> use it!
				bool targetPointDifferentToCrackPoint;
				IPoint crackPointAtTargetPoint = FindCrackPoint(
					AllCrackPoints, snappedPoint, snapTolerance,
					out targetPointDifferentToCrackPoint);

				if (crackPointAtTargetPoint != null && ! targetPointDifferentToCrackPoint)
				{
					return snappedPoint;
				}

				if (crackPointAtTargetPoint != null && crackPointAtSourcePoint != null)
				{
					// crack point nearby, what to do: snap to it, when the same as the crackPointAtSourcePoint? When within snap tolerance?
					if (GeometryUtils.GetPointDistance3D(point, crackPointAtSourcePoint) <
					    GeometryUtils.GetPointDistance3D(point, crackPointAtTargetPoint))
					{
						return crackPointAtSourcePoint;
					}

					if (GeometryUtils.GetPointDistance3D(point, crackPointAtTargetPoint) <
					    snapTolerance)
					{
						// TODO: only if intersection point is also within snap distance of crackPointAtTarget (avoid double-snapping)
						return crackPointAtTargetPoint;
					}
				}
			}

			return crackPointAtSourcePoint ?? snappedPoint ?? point;
		}

		[CanBeNull]
		private IPoint FindCrackPoint([CanBeNull] IPointCollection allCrackPoints,
		                              [NotNull] IPoint atPoint,
		                              double snapTolerance,
		                              out bool differentWithinTolerance)
		{
			differentWithinTolerance = false;

			if (allCrackPoints == null)
			{
				return null;
			}

			IPoint result = null;

			double xyResolution = GetXyResolution(atPoint);
			double zResolution = GetZResolution(atPoint);

			foreach (
				int vertexIndex in
				GeometryUtils.FindVertexIndices(allCrackPoints, atPoint, snapTolerance))
			{
				IPoint crackPoint = allCrackPoints.get_Point(vertexIndex);

				if (IsPerfectlyMatching(crackPoint, atPoint, xyResolution, zResolution,
				                        out differentWithinTolerance, out bool _))
				{
					// there is a perfectly matching crack point -> add it anyway, just in case there is no existing vertex in the source
					return crackPoint;
				}

				if (result != null)
				{
					// TODO: identify the one with the smallest difference in XY / Z, if there are several with different Zs
					// TODO: ensure the total distance between the original source point and the snapped result is not > snap distance
				}

				result = crackPoint;
			}

			return result;
		}

		private double GetXyResolution([NotNull] IGeometry fallbackGeometry)
		{
			double xyResolution = DataXyResolution ??
			                      GeometryUtils.GetXyResolution(fallbackGeometry);

			return xyResolution;
		}

		private double GetZResolution([NotNull] IGeometry fallbackGeometry)
		{
			double zResolution = DataZResolution ??
			                     GeometryUtils.GetZResolution(fallbackGeometry);

			return zResolution;
		}

		[NotNull]
		private CrackPoint CreateCrackPoint([NotNull] IPoint intersectionPoint,
		                                    [NotNull] IPoint pointToInsert,
		                                    [NotNull] IPolycurve optimizedOriginalPolyline,
		                                    bool targetVertexOnlyDifferentInZ,
		                                    bool targetVertexDifferentWithinTolerance)
		{
			CrackPoint crackPoint;
			if (ViolatesMinimumSegmentLength(intersectionPoint, pointToInsert,
			                                 optimizedOriginalPolyline))
			{
				// add to not-possible points
				crackPoint = new CrackPoint(intersectionPoint, true);
			}
			else
			{
				if (GeometryUtils.IsMAware(optimizedOriginalPolyline))
				{
					// set M value to the interpolated value of the source polyline to avoid taking M from the target
					SetInterpolatedM(pointToInsert, optimizedOriginalPolyline);
				}

				crackPoint = new CrackPoint(pointToInsert);

				crackPoint.TargetVertexOnlyDifferentInZ = targetVertexOnlyDifferentInZ;
				crackPoint.TargetVertexDifferentWithinTolerance =
					targetVertexDifferentWithinTolerance;
			}

			return crackPoint;
		}

		private static void SetInterpolatedM([NotNull] IPoint point,
		                                     [NotNull] IPolycurve polycurve)
		{
			double distanceAlong = GeometryUtils.GetDistanceAlongCurve(polycurve, point);

			var mValues =
				((IMSegmentation) polycurve).GetMsAtDistance(distanceAlong, false) as double[];

			if (mValues != null && mValues.Length > 0)
			{
				point.M = mValues[0];
			}
		}

		private static void AddPoint([NotNull] IPoint point,
		                             [NotNull] IPointCollection toCollection)
		{
			Assert.ArgumentNotNull(point, nameof(point));
			Assert.ArgumentNotNull(toCollection, nameof(toCollection));

			object missing = Type.Missing;
			toCollection.AddPoint(point, ref missing, ref missing);
		}

		private static void SetMinimumTolerance([NotNull] IGeometry geometry)
		{
			// Copy from ReshapeUtils!
			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) geometry.SpatialReference).Clone();

			srTolerance.SetMinimumXYTolerance();

			GeometryUtils.EnsureSpatialReference(geometry, (ISpatialReference) srTolerance);
		}

		private static void SetTolerance([NotNull] IGeometry geometry, double tolerance)
		{
			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) geometry.SpatialReference).Clone();

			srTolerance.XYTolerance = tolerance;

			GeometryUtils.EnsureSpatialReference(geometry, (ISpatialReference) srTolerance);
		}

		[CanBeNull]
		private IPoint GetSnappedExtraPoint([NotNull] IPoint intersectionPoint,
		                                    [CanBeNull] IPolyline sourceGeometry,
		                                    [NotNull] IGeometry targetGeometry,
		                                    double largeTolerance,
		                                    [NotNull] IPointCollection
			                                    smallToleranceIntersections)
		{
			if (Perimeter != null &&
			    ! GeometryUtils.Contains(Perimeter, intersectionPoint))
			{
				_msg.VerboseDebugFormat(
					"Filtering large tolerance point because it is outside the perimeter: {0}",
					GeometryUtils.ToString(intersectionPoint));

				return null;
			}

			// NOTE: the extra intersection point should be on a target vertex, if there is one. This allows
			//		 cracking exactly where the neighbour feature has a vertex and hence making it a shared vertex.
			var sourceCurve = sourceGeometry as ICurve;

			// for correct distance-measuring start from closest point on the actual (source) geometry, 
			// because with large tolerance the intersection is between the 'intersecting' geometries 
			// (for points the intersections seem to be on the point)
			IPoint sourcePoint = sourceCurve == null
				                     ? intersectionPoint
				                     : GetPointOnTargetWithSourceZ(intersectionPoint,
				                                                   sourceCurve);

			IPoint snappedPoint = SnapToGeometry(
				sourcePoint, targetGeometry, largeTolerance,
				esriGeometryHitPartType.esriGeometryPartBoundary);

			IPoint snappedExtraPoint = null;

			// NOTE: with large tolerances the intersection points can be quite far off the small-tolerance points
			// TODO: Remove this once factor once the large-tolerance intersection is calculated differently
			//		 and remove the 'Faustregel' from the documentation
			const int exclusionToleranceFactor = 5;

			double largePointExclusionDistance = largeTolerance * exclusionToleranceFactor;

			if (snappedPoint != null &&
			    ! PointIsNear(snappedPoint, smallToleranceIntersections,
			                  largePointExclusionDistance))
			{
				snappedExtraPoint = snappedPoint;

				if (UseSourceZs)
				{
					snappedExtraPoint.Z = sourcePoint.Z;
				}
			}

			return snappedExtraPoint;
		}

		private bool ViolatesMinimumSegmentLength([NotNull] IPoint intersectionPoint,
		                                          [NotNull] IPoint pointToInsert,
		                                          [NotNull] IPolycurve originalPolycurve)
		{
			if (MinimumSegmentLength == null || double.IsNaN((double) MinimumSegmentLength))
			{
				return false;
			}

			// TODO: if a point is found on the source that is within snap distance -> ok, it will snap
			//		 otherwise find the closest segment that would be split and assess distance to from/to point

			// Use a reasonable tolerance: not the feature (could be projected) and not the target / intersection points (could be minimum tolerance):
			// use the larger tolerance to avoid assertion violation
			double sourceTolerance = GeometryUtils.GetXyTolerance(originalPolycurve);
			double targetTolerance = GeometryUtils.GetXyTolerance(pointToInsert);

			double tolerance = sourceTolerance > targetTolerance
				                   ? sourceTolerance
				                   : targetTolerance;

			double searchTolerance = SnapTolerance != null && SnapTolerance > tolerance
				                         ? (double) SnapTolerance
				                         : tolerance;

			// Make sure only one segment interior is matched. Otherwise non-simple geometries
			// would result:
			// TODO: Change to GeomUtils entirely, keep SegmentIntersections of all intersections
			// For now (probably very slow):
			var geometryCollection = (IGeometryCollection) originalPolycurve;

			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				var path = geometryCollection.get_Geometry(i) as IPath;

				if (path == null)
				{
					continue;
				}

				const bool excludeBoundaryMatches = true;
				List<int> segmentsToUpdate =
					GeometryUtils.FindSegmentIndices(
						(ISegmentCollection) path, pointToInsert, searchTolerance,
						excludeBoundaryMatches).ToList();

				if (segmentsToUpdate.Count > 1)
				{
					_msg.DebugFormat("Crack point {0}|{1} is within tolerance of 2 different " +
					                 "segments' interior. It is marked as 'violating minimum " +
					                 "segment length' avoid a cut-back.",
					                 pointToInsert.X, pointToInsert.Y);
					return true;
				}
			}

			int partIndex;
			int? vertexIndex = GeometryUtils.FindHitVertexIndex(originalPolycurve,
			                                                    intersectionPoint,
			                                                    searchTolerance, out partIndex);

			if (vertexIndex != null)
			{
				// A vertex of the source is within the snap distance of the crack point
				// -> it will be snapped
				return false;
			}

			// TODO: ensure that even with large search tolerance always the closest segment is found
			int? segmentIndex = GeometryUtils.FindHitSegmentIndex(originalPolycurve,
			                                                      intersectionPoint,
			                                                      searchTolerance,
			                                                      out partIndex);

			// This can happen because of  Repro_IHitTest_HitTest_DoesNotFindHitSegmentIfSearchPointCloseToVertex 
			// -> work-around? re-implement with box tree!
			Assert.NotNull(segmentIndex, "Source segment not found any more");

			ISegment segment = GeometryUtils.GetSegment((ISegmentCollection) originalPolycurve,
			                                            partIndex, (int) segmentIndex);

			// TODO: consider non-linear segments
			double distanceToFrom = GeometryUtils.GetPointDistance3D(pointToInsert,
			                                                         segment.FromPoint);

			double distanceToTo = GeometryUtils.GetPointDistance3D(pointToInsert,
			                                                       segment.ToPoint);

			// TODO: handle Z-only difference by making sure the Z values get updated in targets too
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("ViolatesMinimumSegmentLength: Checking point to insert {0}",
				                 GeometryUtils.ToString(pointToInsert));
				_msg.DebugFormat(
					"ViolatesMinimumSegmentLength: Distance to segment-from-point: {0}, to segment-to-point: {1}",
					distanceToFrom, distanceToTo);
			}

			// TODO: take into account points to be deleted on this very segment?

			// NOTE: right on the vertex is ok (in chopping mode)
			return distanceToFrom > searchTolerance && distanceToFrom < MinimumSegmentLength ||
			       distanceToTo > searchTolerance && distanceToTo < MinimumSegmentLength;
		}

		[CanBeNull]
		private static IPoint SnapToGeometry([NotNull] IPoint point,
		                                     [CanBeNull] IGeometry targetGeometry,
		                                     [CanBeNull] double? snapTolerance,
		                                     esriGeometryHitPartType snapType)
		{
			Assert.ArgumentNotNull(point, nameof(point));

			if (targetGeometry == null || snapTolerance == null ||
			    double.IsNaN((double) snapTolerance))
			{
				return null;
			}

			IPoint targetVertex;
			double snapDistance;
			bool snapped = TryGetTargetPoint(point, targetGeometry, (double) snapTolerance,
			                                 snapType,
			                                 out targetVertex, out snapDistance);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebugFormat(
					"Point {0}|{1} was snapped: {2}. snapDistance: {3}. New location: {4}",
					point.X, point.Y, snapped, snapDistance, GeometryUtils.ToString(targetVertex));
			}

			if (! snapped)
			{
				return null;
			}

			return targetVertex;
		}

		private static bool TryGetTargetPoint([NotNull] IPoint point,
		                                      [NotNull] IGeometry targetGeometry,
		                                      double snapTolerance,
		                                      esriGeometryHitPartType snapType,
		                                      [NotNull] out IPoint targetPoint,
		                                      out double snapDistance)
		{
			targetPoint = new Point();

			IHitTest hitTest = GeometryUtils.GetHitTest(targetGeometry, true);

			snapDistance = -1;
			int hitPart = -1, hitSegment = -1;
			var rightSide = false;
			return hitTest.HitTest(point, snapTolerance,
			                       snapType, targetPoint,
			                       ref snapDistance, ref hitPart,
			                       ref hitSegment, ref rightSide);
		}

		private static bool PointIsNear([NotNull] IPoint point,
		                                [CanBeNull] IPointCollection pointCollection,
		                                double tolerance)
		{
			var result = false;

			if (pointCollection != null && pointCollection.PointCount > 0)
			{
				// TODO: ReturnDistance3D
				double closestCrackPointDistance =
					((IProximityOperator) pointCollection).ReturnDistance(point);

				if (closestCrackPointDistance < tolerance)
				{
					// there is a closer snap point
					result = true;
				}

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.VerboseDebugFormat(
						closestCrackPointDistance < tolerance
							? "PointIsNear: Found closer point at {0} units of {1}"
							: "PointIsNear: Found no closer point at {0} units of {1}",
						closestCrackPointDistance, point);
				}
			}

			return result;
		}

		[NotNull]
		private static IPoint GetPointOnTargetWithSourceZ([NotNull] IPoint nearPoint,
		                                                  [NotNull] ICurve onTargetCurve)
		{
			IPoint targetPoint = new PointClass
			                     {
				                     SpatialReference = nearPoint.SpatialReference
			                     };

			GeometryUtils.GetDistanceFromCurve(nearPoint,
			                                   onTargetCurve, targetPoint);

			if (GeometryUtils.IsZAware(targetPoint))
			{
				targetPoint.Z = nearPoint.Z;
			}

			return targetPoint;
		}

		[NotNull]
		private IPointCollection GetIntersectionPoints(
			[NotNull] IGeometry target,
			[NotNull] IGeometry source,
			IntersectionPointOptions intersectionPointOption,
			[CanBeNull] out IPolyline linearIntersections)
		{
			linearIntersections = GeometryFactory.CreateEmptyPolyline(source);

			if (CannotIntersect(target, source))
			{
				IMultipoint emptyResult = GeometryFactory.CreateMultipoint();
				emptyResult.SpatialReference = target.SpatialReference;

				return (IPointCollection) emptyResult;
			}

			GeometryUtils.AllowIndexing(source);
			GeometryUtils.AllowIndexing(target);

			IPointCollection result;

			var targetPolycurve = target as IPolycurve;
			var sourcePolycurve = source as IPolycurve;

			if (sourcePolycurve != null && targetPolycurve != null)
			{
				result = UseSourceZs
					         ? (IPointCollection) IntersectionUtils.GetIntersectionPoints(
						         sourcePolycurve, targetPolycurve, true, intersectionPointOption,
						         linearIntersections)
					         : (IPointCollection) IntersectionUtils.GetIntersectionPoints(
						         targetPolycurve, sourcePolycurve, true, intersectionPointOption,
						         linearIntersections);
			}
			else
			{
				result = UseSourceZs
					         ? SegmentReplacementUtils.GetIntersectionPoints(
						         source, target, intersectionPointOption)
					         : SegmentReplacementUtils.GetIntersectionPoints(
						         target, source, intersectionPointOption);
			}

			if (In3D)
			{
				// add the other intersection set as well - this is to get the intersections 
				// at the same XY-location with different Z values. For example there could be
				// a missing point in the source at Z1 but because at Z2 there is already a
				// vertex that is reported with UseSourceZ we would miss the extra intersection at Z1
				IPointCollection result2 = UseSourceZs
					                           ? SegmentReplacementUtils.GetIntersectionPoints(
						                           target, source, intersectionPointOption)
					                           : SegmentReplacementUtils.GetIntersectionPoints(
						                           source, target, intersectionPointOption);

				result.AddPointCollection(result2);

				GeometryUtils.Simplify((IGeometry) result);
			}

			return result;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using IPnt = ProSuite.Commons.Geom.IPnt;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public class CrackPointCalculator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private IEnvelope _envelopeTemplate1;
		private IEnvelope _envelopeTemplate2;

		public CrackPointCalculator([NotNull] ICrackingOptions crackingOptions,
		                            IntersectionPointOptions intersectionPointOption,
		                            bool addCrackPointsAlsoOnExistingVertices,

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
		                            bool useSourceZs,
		                            IntersectionPointOptions intersectionPointOption,
		                            bool addCrackPointsOnExistingVertices,
									[CanBeNull] IEnvelope perimeter)
		{
			Perimeter = perimeter;
			SnapTolerance = snapTolerance;
			MinimumSegmentLength = minimumSegmentLength;
			UseSourceZs = useSourceZs;
			IntersectionPointOption = intersectionPointOption;
			AddCrackPointsOnExistingVertices = addCrackPointsOnExistingVertices;
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

		/// <summary>
		/// An optional xy-tolerance that will be used to determine whether two coordinates are
		/// 'exactly' equal, i.e. no cracking should be performed. If it is not set, half the
		/// data resolution will be used.
		/// </summary>
		public double? EqualityToleranceXY { get; set; }

		/// <summary>
		/// An optional z-tolerance that will be used to determine whether two coordinates are
		/// 'exactly' equal, i.e. no cracking should be performed. If it is not set, half the
		/// data resolution will be used.
		/// </summary>
		public double? EqualityToleranceZ { get; set; }

		[CanBeNull]
		public Func<IGeometry, IGeometry> TargetTransformation { get; set; }

		private IPointCollection AllCrackPoints { get; set; }

		public IDictionary<long, string> FailedOperations { get; } =
			new Dictionary<long, string>();

		public bool ContinueOnException { get; set; }

		public bool UseCustomIntersect { get; set; } = IntersectionUtils.UseCustomIntersect;

		public NonLinearSegmentHandling NonLinearSegmentTreatment { get; set; } =
			NonLinearSegmentHandling.UseLegacyIntersect;

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
		public IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> GetIntersectionPoints(
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

					return new List<KeyValuePair<IPnt, List<IntersectionPoint3D>>>(0);
				}
			}
			else
			{
				transformedIntersectionTarget = intersectionTarget;
			}

			IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> result;

			bool origIntersect = IntersectionUtils.UseCustomIntersect;

			bool useCustomIntersect = UseCustomIntersect;

			if (NonLinearSegmentTreatment == NonLinearSegmentHandling.UseLegacyIntersect &&
			    (GeometryUtils.HasNonLinearSegments(sourceGeometry) ||
			     GeometryUtils.HasNonLinearSegments(intersectionTarget)))
			{
				useCustomIntersect = false;
			}

			try
			{
				if (useCustomIntersect)
				{
					IntersectionUtils.UseCustomIntersect = true;
					result = GetClusteredIntersectionPoints(
						sourceGeometry, transformedIntersectionTarget);
				}
				else
				{
					var legacyIntersections =
						GetIntersectionPoints(sourceGeometry, transformedIntersectionTarget);

					result = GeometryUtils
					         .GetPoints(legacyIntersections)
					         .Select(p => new KeyValuePair<IPnt, List<IntersectionPoint3D>>(
						                 new Pnt3D(p.X, p.Y, p.Z), null))
					         .ToList();
				}
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = origIntersect;
			}

			return result;
		}

		public IList<CrackPoint> DetermineCrackPoints(
			[NotNull] IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> intersectionPnts,
			[NotNull] IGeometry originalGeometry,
			[NotNull] IPolyline optimizedPolyline,
			[CanBeNull] IGeometry snapTarget)
		{
			if (intersectionPnts.Count == 0)
			{
				return new List<CrackPoint>(0);
			}

			if (IntersectionUtils.UseCustomIntersect &&
			    ! GeometryUtils.HasNonLinearSegments(originalGeometry) &&
			    (snapTarget == null || ! GeometryUtils.HasNonLinearSegments(snapTarget)))
			{
				ISegmentList originalSegments =
					originalGeometry is IPolycurve originalPolycurve
						? (ISegmentList) GeometryConversionUtils.CreateMultiPolycurve(
							originalPolycurve)
						: GeometryConversionUtils.CreatePolyhedron((IMultiPatch) originalGeometry);

				IPointList snapTargetPoints = ToPointList(snapTarget);

				return DetermineCrackPoints3d(intersectionPnts, originalSegments, originalGeometry,
				                              optimizedPolyline, snapTargetPoints);
			}

			IPointCollection intersectionPoints = GeometryConversionUtils.CreatePointCollection(
				originalGeometry, intersectionPnts.Select(kvp => kvp.Key).ToList());

			return DetermineCrackPoints(intersectionPoints, originalGeometry, optimizedPolyline,
			                            snapTarget);
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

		public IList<CrackPoint> DetermineCrackPoints3d(
			[NotNull] IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> clusteredIntersections,
			[NotNull] ISegmentList originalSegments,
			[NotNull] IGeometry originalGeometry,
			[NotNull] IPolyline optimizedPolyline,
			[CanBeNull] IPointList snapTarget)
		{
			var result = new List<CrackPoint>(clusteredIntersections.Count);

			EnvelopeXY envelopeXY = null;
			if (Perimeter != null)
			{
				envelopeXY = GeometryConversionUtils.CreateEnvelopeXY(Perimeter);
			}

			double tolerance = GeometryUtils.GetXyTolerance(originalGeometry);

			double snapTolerance = SnapTolerance ?? GeometryUtils.GetXyTolerance(originalGeometry);

			foreach (var kvp in clusteredIntersections)
			{
				IPnt point = kvp.Key;
				double z = point is Pnt3D pnt3D ? pnt3D.Z : double.NaN;
				IPoint aoPoint =
					GeometryFactory.CreatePoint(point.X, point.Y, z, double.NaN,
					                            originalGeometry.SpatialReference);

				if (envelopeXY != null &&
				    GeomRelationUtils.AreDisjoint(envelopeXY, point, snapTolerance))
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

					// TODO: Implement points touching lines:
					// GeomRelationUtils.TouchesXY()

					if (originalPolyline != null &&
					    (GeometryUtils.AreEqualInXY(originalPolyline.FromPoint, aoPoint) ||
					     GeometryUtils.AreEqualInXY(originalPolyline.ToPoint, aoPoint)))
					{
						continue;
					}
				}
				else
				{
					// if not chopping: skip crack points that are existing vertices
					bool hasSourcePoint = HasMatchingPoint3d(originalSegments, point, snapTolerance,
					                                         out
					                                         targetVertexDifferentWithinTolerance,
					                                         out targetVertexDifferentInZ);

					// With the new intersection logic we have a crack point at each Z-level
					// But un-cracked segments can still be there (even if a source point exists
					// an uncracked segment might co-exist) in multipatches...
					bool hasUncrackedSegment = HasUncrackedExtraMultipatchSegments3d(
						originalSegments, point, tolerance);

					// Uncracked segments always win over (almost-matching) points
					if (hasUncrackedSegment)
					{
						targetVertexDifferentInZ = false;
						targetVertexDifferentWithinTolerance = false;
					}

					if (hasSourcePoint &&
					    ! targetVertexDifferentWithinTolerance && ! hasUncrackedSegment)
					{
						// there is a perfectly matching source point and no other crackable segments
						continue;
					}
				}

				IPnt pntToInsert = Snap3d(point, snapTarget, snapTolerance);

				IPoint pointToInsert =
					GeometryConversionUtils.CreatePoint(pntToInsert,
					                                    originalGeometry.SpatialReference);

				bool violatesMinSegLength =
					ViolatesMinimumSegmentLength3d(point, pntToInsert, originalSegments,
					                               tolerance);

				CrackPoint crackPoint = CreateCrackPoint(
					aoPoint, pointToInsert, optimizedPolyline, violatesMinSegLength,
					targetVertexDifferentInZ, targetVertexDifferentWithinTolerance);

				crackPoint.Intersections = kvp.Value;

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

		/// <summary>
		/// Whether there are additional segments (apart from an expected vertex) that are not cracked
		/// at the specified point. This makes sense for multipatch cracking, where geometries self-intersect
		/// at various Z-levels.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="atPoint"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		private bool HasUncrackedExtraMultipatchSegments3d([NotNull] ISegmentList sourceSegments,
		                                                   [NotNull] IPnt atPoint,
		                                                   double tolerance)
		{
			if (In3D)
			{
				return false;
			}

			if (UseSourceZs)
			{
				double xyTolerance = SnapTolerance ?? tolerance;

				// check for segments that are not cracked, even though there are other good vertices at different Z in the same part
				var unsplitSegments =
					FindSegmentsPerpendicular(atPoint, sourceSegments, xyTolerance);

				int unsplitSegmentCount = unsplitSegments
					.Count(s => ! HasEndPointWithinDistance(
						            s.Value, atPoint, xyTolerance));

				return unsplitSegmentCount > 0;
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
					    atPoint, existingPoint, xyResolution, zResolution,
					    out thisPointWithinTolerance,
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

		private bool HasMatchingPoint3d([NotNull] ISegmentList originalSegments,
		                                [NotNull] IPnt atPoint,
		                                double tolerance,
		                                out bool differentWithinTolerance,
		                                out bool differentInZ)
		{
			differentWithinTolerance = false;
			differentInZ = false;

			// TODO: Separate segments from vertices, include the HasUncrackedSegments method
			// TODO: Use the original intersection points from the cluster
			var existingPoints =
				GeomTopoOpUtils.GetIntersectionPoints(
					               (IPointList) originalSegments, atPoint, tolerance)
				               .Select(ip => ip.Point)
				               .ToList();

			// Alternative:
			//IPointList originalPoints = (IPointList) originalSegments;
			//var existingPointIndexes = originalPoints.FindPointIndexes(atPoint,
			//	                                         tolerance, true)
			//                                         .ToList();

			//var existingPoints = new List<IPnt>();
			//foreach (int existingPointIndex in existingPointIndexes)
			//{
			//	existingPoints.Add(originalPoints.GetPoint(existingPointIndex));
			//}

			//IList<IPoint> existingPoints =
			//	GeometryUtils.FindPoints(originalPoints, atPoint,
			//	                         GeometryUtils.GetXyTolerance(originalPoints)).ToList();

			if (existingPoints.Count == 0)
			{
				_msg.DebugFormat("No point found in the original geometry. Point: {0}.", atPoint);

				return false;
			}

			double xyResolution = GetXyResolution(atPoint);
			double zResolution = GetZResolution(atPoint);

			var pointFound = false;
			foreach (Pnt3D existingPoint in existingPoints)
			{
				bool thisPointWithinTolerance, thisPointDifferentInZ;
				if (IsPerfectlyMatching(
					    atPoint, existingPoint, xyResolution, zResolution,
					    tolerance, out thisPointWithinTolerance, out thisPointDifferentInZ))
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
		/// <param name="equalityToleranceXY"></param>
		/// <param name="equalityToleranceZ"></param>
		/// <param name="differentWithinTolerance"></param>
		/// <param name="differentInZ"></param>
		/// <returns></returns>
		private static bool IsPerfectlyMatching([NotNull] IPoint point1,
		                                        [NotNull] IPoint point2,
		                                        double equalityToleranceXY,
		                                        double equalityToleranceZ,
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

			differentWithinTolerance =
				! MathUtils.AreEqual(point1.X, point2.X, equalityToleranceXY) ||
				! MathUtils.AreEqual(point1.Y, point2.Y, equalityToleranceXY);

			if (GeometryUtils.IsZAware(point2) && GeometryUtils.IsZAware(point1))
			{
				double dZ = Math.Abs(point1.Z - point2.Z);

				differentInZ = dZ >= equalityToleranceZ;

				differentWithinTolerance |= differentInZ &&
				                            dZ < GeometryUtils.GetZTolerance(point1);
			}

			return ! differentWithinTolerance && ! differentInZ;
		}

		/// <summary>
		/// Determines whether the two points that are expected to be expected relational-operator-equal
		/// are exactly equal (i.e. equal to the point that they would end up exactly on store)
		/// </summary>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <param name="equalityToleranceXY"></param>
		/// <param name="equalityToleranceZ"></param>
		/// <param name="tolerance"></param>
		/// <param name="differentWithinTolerance"></param>
		/// <param name="differentInZ"></param>
		/// <returns></returns>
		private static bool IsPerfectlyMatching([NotNull] IPnt point1,
		                                        [NotNull] IPnt point2,
		                                        double equalityToleranceXY,
		                                        double equalityToleranceZ,
		                                        double tolerance,
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

			differentWithinTolerance =
				! MathUtils.AreEqual(point1.X, point2.X, equalityToleranceXY) ||
				! MathUtils.AreEqual(point1.Y, point2.Y, equalityToleranceXY);

			double z1 = point1 is Pnt3D pnt3d1 ? pnt3d1.Z : double.NaN;
			double z2 = point2 is Pnt3D pnt3d2 ? pnt3d2.Z : double.NaN;

			if (! double.IsNaN(z1) && ! double.IsNaN(z2))
			{
				double dZ = Math.Abs(z1 - z2);

				differentInZ = dZ >= equalityToleranceZ;

				differentWithinTolerance |= differentInZ && dZ < tolerance;
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
				GetIntersectionPoints(intersectionTarget, sourceGeometry, IntersectionPointOption,
				                      out _);

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
				_msg.VerboseDebug(
					() => $"Comparing large tolerance intersecion points with small " +
					      $"tolerance: {smallToleranceIntersections.PointCount}. Intersection " +
					      $"points with (snap-)tolerance: {largeToleranceIntersections.PointCount}");

				for (var i = 0; i < largeToleranceIntersections.PointCount; i++)
				{
					IPoint snappedExtraPoint =
						GetSnappedExtraPoint(largeToleranceIntersections.get_Point(i),
						                     sourceGeometry,
						                     intersectionTarget, largeTolerance,
						                     smallToleranceIntersections);

					if (snappedExtraPoint != null)
					{
						_msg.VerboseDebug(
							() =>
								$"Using extra large-tolerance intersection point at " +
								$"{snappedExtraPoint.X} | {snappedExtraPoint.Y}");

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

		[NotNull]
		private IPnt Snap3d([NotNull] IPnt point,
		                    [CanBeNull] IPointList snapTarget,
		                    double snapTolerance)
		{
			List<Pnt3D> allCrackPoints = null;

			if (AllCrackPoints != null)
			{
				allCrackPoints = GeometryUtils.GetPoints(AllCrackPoints, true)
				                              .Select(p => new Pnt3D(p.X, p.Y, p.Z)).ToList();
			}

			// if the local crack point is exactly on an existing crack point (AllCrackPoints)
			//    -> Use this matching previously created crack point to avoid swapping Z values
			bool sourcePointDifferentToCrackPoint;
			IPnt crackPointAtSourcePoint = FindCrackPoint(
				allCrackPoints, point, snapTolerance, false, out sourcePointDifferentToCrackPoint);

			if (crackPointAtSourcePoint != null && ! sourcePointDifferentToCrackPoint)
			{
				return crackPointAtSourcePoint;
			}

			// snap to the target geometry's *vertices* (consider snapping to edges too?)
			Pnt3D snappedPoint = SnapToGeometry3d(point, snapTarget, snapTolerance,
			                                      esriGeometryHitPartType.esriGeometryPartVertex);

			if (snappedPoint != null)
			{
				if (UseSourceZs)
				{
					// fix Z
					double sourceZ = point is Pnt3D pnt3d ? pnt3d.Z : double.NaN;
					snappedPoint.Z = sourceZ;
				}

				// if the target point is exactly on an existing crack point -> use it!
				bool targetPointDifferentToCrackPoint;
				IPnt crackPointAtTargetPoint = FindCrackPoint(
					allCrackPoints, snappedPoint, snapTolerance, false,
					out targetPointDifferentToCrackPoint);

				if (crackPointAtTargetPoint != null && ! targetPointDifferentToCrackPoint)
				{
					return snappedPoint;
				}

				if (crackPointAtTargetPoint != null && crackPointAtSourcePoint != null)
				{
					// crack point nearby, what to do: snap to it, when the same as the crackPointAtSourcePoint? When within snap tolerance?
					if (GeomUtils.GetDistanceXYZ(point, crackPointAtSourcePoint) <
					    GeomUtils.GetDistanceXYZ(point, crackPointAtTargetPoint))
					{
						return crackPointAtSourcePoint;
					}

					if (GeomUtils.GetDistanceXYZ(point, crackPointAtTargetPoint) < snapTolerance)
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

		[CanBeNull]
		private IPnt FindCrackPoint([CanBeNull] IEnumerable<IPnt> allCrackPoints,
		                            [NotNull] IPnt atPoint,
		                            double snapTolerance,
		                            bool mustMatchInZ,
		                            out bool differentWithinTolerance)
		{
			differentWithinTolerance = false;

			if (allCrackPoints == null)
			{
				return null;
			}

			IPnt result = null;

			double xyResolution = GetXyResolution(atPoint);
			double zResolution = GetZResolution(atPoint);

			foreach (IPnt crackPoint in allCrackPoints)
			{
				if (! GeomRelationUtils.IsWithinTolerance(crackPoint, atPoint, snapTolerance, true))
				{
					continue;
				}

				if (IsPerfectlyMatching(crackPoint, atPoint, xyResolution, zResolution,
				                        snapTolerance, out differentWithinTolerance,
				                        out bool differentInZ))
				{
					// there is a perfectly matching crack point -> add it anyway, just in case there is no existing vertex in the source
					return crackPoint;
				}

				if (result != null)
				{
					// TODO: identify the one with the smallest difference in XY / Z, if there are several with different Zs
					// TODO: ensure the total distance between the original source point and the snapped result is not > snap distance
				}

				if (! mustMatchInZ || ! differentInZ)
				{
					result = crackPoint;
				}
			}

			return result;
		}

		private double GetXyResolution([NotNull] IGeometry fallbackGeometry)
		{
			if (_envelopeTemplate1 == null)
			{
				_envelopeTemplate1 = fallbackGeometry.Envelope;
			}
			else
			{
				fallbackGeometry.QueryEnvelope(_envelopeTemplate1);
			}

			return GetXyResolution(
				GeometryConversionUtils.CreateEnvelopeXY(_envelopeTemplate1),
				GeometryUtils.GetXyResolution(fallbackGeometry));
		}

		private double GetXyResolution(IBoundedXY envelope,
		                               double fallBackResolution = double.NaN)
		{
			// Externally defined equality tolerance:
			if (EqualityToleranceXY != null)
			{
				return EqualityToleranceXY.Value;
			}

			double xyResolution = DataXyResolution ?? fallBackResolution;

			Assert.NotNaN(xyResolution,
			              "Neither the EqualityToleranceXY nor the DataXyResolution is defined.");

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(envelope.XMax, envelope.YMax);

			// The crack point can be exactly between the two vertices that are different by 1 resolution.
			// Therefore, subtract an epsilon:
			double equalToleranceXY = xyResolution / 2 - epsilon;

			return equalToleranceXY;
		}

		private double GetZResolution([NotNull] IGeometry fallbackGeometry)
		{
			if (_envelopeTemplate1 == null)
			{
				_envelopeTemplate1 = fallbackGeometry.Envelope;
			}
			else
			{
				fallbackGeometry.QueryEnvelope(_envelopeTemplate1);
			}

			return GetZResolution(
				GeometryConversionUtils.CreateEnvelopeXY(_envelopeTemplate1),
				GeometryUtils.GetZResolution(fallbackGeometry));
		}

		private double GetZResolution(IBoundedXY envelope,
		                              double fallBackResolution = double.NaN)
		{
			// Externally defined equality tolerance:
			if (EqualityToleranceZ != null)
			{
				return EqualityToleranceZ.Value;
			}

			double zResolution = DataZResolution ?? fallBackResolution;

			if (double.IsNaN(zResolution))
			{
				return zResolution;
			}

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(envelope.XMax, envelope.YMax);

			double equalToleranceZ = zResolution / 2 - epsilon;

			return equalToleranceZ;
		}

		[NotNull]
		private CrackPoint CreateCrackPoint([NotNull] IPoint intersectionPoint,
		                                    [NotNull] IPoint pointToInsert,
		                                    [NotNull] IPolycurve optimizedOriginalPolyline,
		                                    bool targetVertexOnlyDifferentInZ,
		                                    bool targetVertexDifferentWithinTolerance)
		{
			bool violatesMinimumSegmentLength = ViolatesMinimumSegmentLength(
				intersectionPoint, pointToInsert, optimizedOriginalPolyline);

			return CreateCrackPoint(intersectionPoint, pointToInsert, optimizedOriginalPolyline,
			                        violatesMinimumSegmentLength, targetVertexOnlyDifferentInZ,
			                        targetVertexDifferentWithinTolerance);
		}

		[NotNull]
		private static CrackPoint CreateCrackPoint([NotNull] IPoint intersectionPoint,
		                                           [NotNull] IPoint pointToInsert,
		                                           [NotNull] IPolycurve optimizedOriginalPolyline,
		                                           bool violatesMinimumSegmentLength,
		                                           bool targetVertexOnlyDifferentInZ,
		                                           bool targetVertexDifferentWithinTolerance)
		{
			CrackPoint crackPoint;
			if (violatesMinimumSegmentLength)
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
				_msg.VerboseDebug(
					() =>
						$"Filtering large tolerance point because it is outside the perimeter: {GeometryUtils.ToString(intersectionPoint)}");

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

			double searchTolerance = SnapTolerance > tolerance
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
					                 "segment length' to avoid a cut-back.",
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

		private bool ViolatesMinimumSegmentLength3d([NotNull] IPnt intersectionPoint,
		                                            [NotNull] IPnt pointToInsert,
		                                            [NotNull] ISegmentList originalSegments,
		                                            double tolerance)
		{
			if (MinimumSegmentLength == null || double.IsNaN((double) MinimumSegmentLength))
			{
				return false;
			}

			// TODO: if a point is found on the source that is within snap distance -> ok, it will snap
			//		 otherwise find the closest segment that would be split and assess distance to from/to point

			// Use a reasonable tolerance: not the feature (could be projected) and not the target / intersection points (could be minimum tolerance):
			// use the larger tolerance to avoid assertion violation
			//double sourceTolerance = GeometryUtils.GetXyTolerance(originalPolycurve);
			//double targetTolerance = GeometryUtils.GetXyTolerance(pointToInsert);

			//double tolerance = sourceTolerance > targetTolerance
			//					   ? sourceTolerance
			//					   : targetTolerance;

			double searchTolerance = SnapTolerance > tolerance
				                         ? (double) SnapTolerance
				                         : tolerance;

			var segmentsByIndex =
				FindSegmentsPerpendicular(pointToInsert, originalSegments, searchTolerance)
					.ToList();

			// NOTE: Especially in multipatches it could be possible to violate a short segment length
			//       in one of the rings but not in the other. For the time being let's be conservative and
			//       do not crack anything if any of the involved rings would get a short segment

			foreach (var segmentsByPart
			         in segmentsByIndex.GroupBy(kvp =>
			         {
				         originalSegments.GetLocalSegmentIndex(kvp.Key, out int partIdx);
				         return partIdx;
			         }))
			{
				if (segmentsByPart.Any(
					    kvp => HasEndPointWithinDistance(kvp.Value, intersectionPoint,
					                                     searchTolerance)))
				{
					// A vertex of the source is within the snap distance of the crack point
					// -> it will be snapped
					return false;
				}

				IEnumerable<KeyValuePair<int, Line3D>> interiorCutSegments = segmentsByPart
					.Where(kvp => ! HasEndPointWithinDistance(
						              kvp.Value, pointToInsert, tolerance));

				if (interiorCutSegments.Count() > 1)
				{
					_msg.DebugFormat("Crack point {0}|{1} is within tolerance of 2 different " +
					                 "segments' interior of part {2}. It is marked as 'violating minimum " +
					                 "segment length' to avoid a cut-back.",
					                 pointToInsert.X, pointToInsert.Y, segmentsByPart.Key);
					return true;
				}
			}

			// Use the closest or check each segment? In case of multipatches the point might be
			// inserted in several segments:
			foreach (Line3D segment in segmentsByIndex.Select(kvp => kvp.Value))
			{
				if (HasEndPointWithinDistance(segment, pointToInsert, MinimumSegmentLength.Value))
				{
					return true;
				}
			}

			return false;
		}

		private static IEnumerable<KeyValuePair<int, Line3D>> FindSegmentsPerpendicular(
			IPnt toPoint,
			ISegmentList inSegmentList,
			double searchTolerance)
		{
			if (! (toPoint is Pnt3D pnt3d))
			{
				pnt3d = new Pnt3D(toPoint);
			}

			foreach (KeyValuePair<int, Line3D> kvp in inSegmentList.FindSegments(
				         toPoint, searchTolerance))
			{
				Line3D segment = kvp.Value;

				double distanceFrom =
					segment.GetDistancePerpendicular(pnt3d, true, out double alongRatio, out _);

				if (alongRatio < 0 || alongRatio > 1)
				{
					continue;
				}

				if (distanceFrom < searchTolerance)
				{
					yield return kvp;
				}
			}
		}

		private static bool HasEndPointWithinDistance([NotNull] Line3D segment,
		                                              [NotNull] IPnt ofPoint,
		                                              double distance)
		{
			if (GeomUtils.GetDistanceXY(ofPoint, segment.StartPoint) <= distance)
			{
				return true;
			}

			if (GeomUtils.GetDistanceXY(ofPoint, segment.EndPoint) <= distance)
			{
				return true;
			}

			return false;
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

			_msg.VerboseDebug(
				() =>
					$"Point {point.X}|{point.Y} was snapped: {snapped}. snapDistance: {snapDistance}. New location: {GeometryUtils.ToString(targetVertex)}");

			if (! snapped)
			{
				return null;
			}

			return targetVertex;
		}

		[CanBeNull]
		private static Pnt3D SnapToGeometry3d([NotNull] IPnt point,
		                                      [CanBeNull] IPointList targetGeometry,
		                                      [CanBeNull] double? snapTolerance,
		                                      esriGeometryHitPartType snapType)
		{
			Assert.ArgumentNotNull(point, nameof(point));

			if (targetGeometry == null || snapTolerance == null ||
			    double.IsNaN((double) snapTolerance))
			{
				return null;
			}

			IPnt closestTargetVertex = null;
			double closestDistanceSquared = double.MaxValue;

			//foreach (IntersectionPoint3D intersectionPoint in GeomTopoOpUtils.GetIntersectionPoints(
			//	         point, 0, targetGeometry, snapTolerance.Value, false))
			foreach (IntersectionPoint3D intersectionPoint in GeomTopoOpUtils.GetIntersectionPoints(
				         point, targetGeometry, snapTolerance.Value))
			{
				if (snapType == esriGeometryHitPartType.esriGeometryPartBoundary ||
				    snapType == esriGeometryHitPartType.esriGeometryPartVertex &&
				    intersectionPoint.VirtualTargetVertex % 1 == 0)
				{
					IPnt targetPnt = intersectionPoint.GetTargetPoint(targetGeometry);

					double distanceSquared = GeomUtils.GetDistanceSquaredXY(point, targetPnt);

					if (distanceSquared < closestDistanceSquared)
					{
						closestDistanceSquared = distanceSquared;
						closestTargetVertex = targetPnt;
					}
				}
			}

			bool snapped = (closestDistanceSquared < snapTolerance.Value * snapTolerance.Value);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(
					() => $"Point {point.X}|{point.Y} was snapped: {snapped}. " +
					      $"3D-snapDistance squared: {closestDistanceSquared}. " +
					      $"New location: {closestTargetVertex}");
			}

			if (! snapped)
			{
				return null;
			}

			return new Pnt3D(closestTargetVertex);
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

				_msg.VerboseDebug(() =>
					                  closestCrackPointDistance < tolerance
						                  ? $"PointIsNear: Found closer point at {closestCrackPointDistance} units of {point}"
						                  : $"PointIsNear: Found no closer point at {closestCrackPointDistance} units of {point}");
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

		[NotNull]
		private IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> GetClusteredIntersectionPoints(
			[NotNull] IGeometry sourceGeometry,
			[NotNull] IGeometry intersectionTarget)
		{
			if (CannotIntersect(intersectionTarget, sourceGeometry))
			{
				IMultipoint emptyResult = GeometryFactory.CreateMultipoint();
				emptyResult.SpatialReference = intersectionTarget.SpatialReference;

				return new List<KeyValuePair<IPnt, List<IntersectionPoint3D>>>(0);
			}

			double snapTolerance = SnapTolerance ?? GeometryUtils.GetXyTolerance(sourceGeometry);

			bool omitNonLinearSegments = NonLinearSegmentTreatment == NonLinearSegmentHandling.Omit;

			bool includeIntermediatePoints =
				IntersectionPointOption ==
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints;

			List<IntersectionWithTargetPoint> intersectionPointsWithTargetPoint =
				GetIntersectionPoints3d(sourceGeometry, intersectionTarget, snapTolerance,
				                        omitNonLinearSegments, includeIntermediatePoints);

			List<IntersectionPoint3D> intersectionsWithTargetZ =
				intersectionPointsWithTargetPoint.Select(
					CreateIntersectionPointWithTargetZ).ToList();

			if (In3D)
			{
				// TODO: This is not covered by a unit test. Is it still relevant at all?
				// both sets of intersection points:
				// Use both intersection sets - this is to get the intersections 
				// at the same XY-location with different Z values. For example there could be
				// a missing point in the source at Z1 but because at Z2 there is already a
				// vertex that is reported with UseSourceZ we would miss the extra intersection at Z1
				//List<IntersectionPoint3D> intersectionPoints = intersectionPointsWithTargetPoint.Select(
				//	ip => ip.Intersection).ToList();

				//intersectionPoints.AddRange(intersectionsWithTargetZ);
			}

			// NOTE: The resulting key's XY should be on the target (because the source shall be snapped onto the target)
			//       The Z depends on the setting...

			Func<IntersectionWithTargetPoint, IPnt> getPointFunc;
			if (UseSourceZs)
			{
				getPointFunc = CreateTargetPointWithSourceZ;
			}
			else
			{
				getPointFunc = ip => ip.TargetPoint;
			}

			IList<KeyValuePair<IPnt, List<IntersectionPoint3D>>> clusteredIntersections =
				GeomTopoOpUtils.Cluster(intersectionPointsWithTargetPoint, getPointFunc,
				                        snapTolerance, snapTolerance)
				               .Select(c => new KeyValuePair<IPnt, List<IntersectionPoint3D>>(
					                       c.Key, c.Value.Select(v => v.Intersection).ToList()))
				               .ToList();

			return clusteredIntersections;
		}

		private static List<IntersectionWithTargetPoint> GetIntersectionPoints3d(
			[NotNull] IGeometry sourceGeometry,
			[NotNull] IGeometry intersectionTarget,
			double snapTolerance,
			bool omitNonLinearSegments,
			bool includeIntermediatePoints)
		{
			ISegmentList sourceSegments = ToSegmentList(sourceGeometry, omitNonLinearSegments);

			ISegmentList targetSegments = ToSegmentList(intersectionTarget, omitNonLinearSegments);

			if (sourceSegments == null &&
			    sourceGeometry is IMultipoint sourceMultipoint)
			{
				// Source is multipoint
				var sourcePnts = GeometryConversionUtils.CreateMultipoint(sourceMultipoint);

				var intersections = GeomTopoOpUtils.GetIntersectionPoints(
					sourcePnts, targetSegments,
					snapTolerance, false).ToList();

				return intersections.Select(i => new IntersectionWithTargetPoint(
					                            i, i.GetTargetPoint(targetSegments))).ToList();
			}

			Assert.NotNull(sourceSegments, "Unsupported source geometry type: {0}",
			               sourceGeometry.GeometryType);

			List<IntersectionPoint3D> intersectionPoints;
			Func<IntersectionPoint3D, Pnt3D> getTargetPointFunc;

			if (targetSegments != null)
			{
				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						sourceSegments, targetSegments, snapTolerance, false,
						includeIntermediatePoints).ToList();

				getTargetPointFunc = ip => ip.GetTargetPoint(targetSegments);
			}
			else if (intersectionTarget is IMultipoint multipoint)
			{
				IPointList targetPoints = GeometryConversionUtils.CreateMultipoint(multipoint);

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(sourceSegments, targetPoints,
					                                      snapTolerance, false).ToList();

				getTargetPointFunc = ip => new Pnt3D(ip.GetTargetPoint(targetPoints));
			}
			else
			{
				IPoint targetPoint = intersectionTarget as IPoint;
				Assert.True(targetPoint != null, "Unsupported target geometry type");

				IPnt pnt = GeometryConversionUtils.CreatePnt(targetPoint, true);

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						               sourceSegments, pnt, 0, snapTolerance, false)
					               .ToList();

				getTargetPointFunc = i => new Pnt3D(pnt);
			}

			return CreateIntersectionsWithTargetPoint(intersectionPoints, getTargetPointFunc);
		}

		private static ISegmentList ToSegmentList([NotNull] IGeometry polycurveOrMultipatch,
		                                          bool omitNonLinearSegments)
		{
			ISegmentList result = null;

			if (polycurveOrMultipatch is IMultiPatch targetMultipatch)
			{
				result = GeometryConversionUtils.CreatePolyhedron(targetMultipatch);
			}
			else if (polycurveOrMultipatch is IPolycurve targetPolycurve)
			{
				result =
					GeometryConversionUtils.CreateMultiPolycurve(
						targetPolycurve, omitNonLinearSegments);
			}

			return result;
		}

		[CanBeNull]
		private static IPointList ToPointList([CanBeNull] IGeometry geometry)
		{
			if (geometry == null)
			{
				return null;
			}

			if (geometry is IPolycurve polycurve)
			{
				return GeometryConversionUtils.CreateMultiPolycurve(polycurve);
			}

			if (geometry is IMultiPatch multipatch)
			{
				return GeometryConversionUtils.CreatePolyhedron(multipatch);
			}

			if (geometry is IPoint point)
			{
				return GeometryConversionUtils.CreateMultipoint(point);
			}

			return GeometryConversionUtils.CreateMultipoint((IMultipoint) geometry);
		}

		private static List<IntersectionWithTargetPoint> CreateIntersectionsWithTargetPoint(
			[NotNull] IEnumerable<IntersectionPoint3D> intersectionPoints,
			[NotNull] Func<IntersectionPoint3D, Pnt3D> getTargetPointFunc)
		{
			var result =
				intersectionPoints.Select(
					                  ip => new IntersectionWithTargetPoint(
						                  ip, getTargetPointFunc(ip)))
				                  .ToList();

			return result;
		}

		private static IntersectionPoint3D CreateIntersectionPointWithTargetZ(
			IntersectionWithTargetPoint intersectionWithTarget)
		{
			var result = intersectionWithTarget.Intersection.Clone();

			result.Point.Z = intersectionWithTarget.TargetPoint.Z;

			return result;
		}

		private static IPnt CreateTargetPointWithSourceZ(
			IntersectionWithTargetPoint intersectionWithTarget)
		{
			Pnt3D targetPoint = intersectionWithTarget.TargetPoint;

			return new Pnt3D(targetPoint.X, targetPoint.Y,
			                 intersectionWithTarget.Intersection.Point.Z);
		}

		private class IntersectionWithTargetPoint
		{
			public IntersectionWithTargetPoint(
				[NotNull] IntersectionPoint3D intersection,
				[NotNull] Pnt3D targetPoint)
			{
				Intersection = intersection;
				TargetPoint = targetPoint;
			}

			public IntersectionPoint3D Intersection { get; }

			public Pnt3D TargetPoint { get; }
		}
	}
}

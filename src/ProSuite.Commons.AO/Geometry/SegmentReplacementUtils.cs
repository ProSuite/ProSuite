using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry
{
	public static class SegmentReplacementUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IPointCollection GetIntersectionPoints(
			[NotNull] IGeometry geometry1,
			[NotNull] IGeometry geometry2)
		{
			return (IPointCollection)
				IntersectionUtils.GetIntersectionPoints(geometry1, geometry2);
		}

		/// <summary>
		/// Gets the intersection points between two geometries and implements a 
		/// work-around for some situations where the standard mechanisms don't find
		/// certain intersections. 
		/// The Z values of the resulting points are taken from geometry1 except for
		/// the option IncludeLinearIntersectionAllPoints where the points from both
		/// geometries (if they have different Z values) are returned.
		/// NOTE: This method assumes that the geometries are intersecting.
		/// For good performance check if the geometries are disjoint before.
		/// </summary>
		/// <param name="geometry1"></param>
		/// <param name="geometry2"></param>
		/// <param name="intersectionPointOption"></param>
		/// <returns></returns>
		[NotNull]
		public static IPointCollection GetIntersectionPoints(
			[NotNull] IGeometry geometry1,
			[NotNull] IGeometry geometry2,
			IntersectionPointOptions intersectionPointOption)
		{
			const bool assumeIntersecting = true;

			var result =
				(IPointCollection)
				IntersectionUtils.GetIntersectionPoints(geometry1, geometry2, assumeIntersecting,
				                                        intersectionPointOption);

			return result;
		}

		public static void ReplaceSegments([NotNull] IPath pathToReshape,
		                                   [NotNull] IPath trimmedReplacement,
		                                   [NotNull] IPoint firstCutPoint,
		                                   [NotNull] IPoint lastCutPoint)
		{
			double tolerance = GeometryUtils.GetXyTolerance(pathToReshape);

			// NOTE: the Z value of the first/last cut point (which are part of the source)
			//		 can be different from the trimmedReplacement's connect point's Z values
			// 1. ensure the first/last connect points exist in the source (could be on a segment)
			// 2. ensure that if the trimmed replacement connects nicely the Z values of the
			//	  trimmed replacement is applied to the first/last cut point (or vice versa?)

			// Add the two cut points to the origional part - if there are no points yet
			EnsureConnectVerticesExist(pathToReshape, firstCutPoint, lastCutPoint, tolerance,
			                           null);

			const bool pointIsSegmentFromPoint = true;
			int firstSegmentIndex = GetSegmentIndex(pathToReshape, firstCutPoint, tolerance,
			                                        out int _, pointIsSegmentFromPoint);

			int lastSegmentIndex = GetSegmentIndex(pathToReshape, lastCutPoint, tolerance, out _);

			var segments = (ISegmentCollection) pathToReshape;

			if (firstSegmentIndex > lastSegmentIndex)
			{
				// Flip segment indexes. The replacement curve is flipped further down, if needed
				FlipSegmentIndexes(segments, firstCutPoint, lastCutPoint,
				                   ref firstSegmentIndex, ref lastSegmentIndex);
			}

			ReplaceSegments(pathToReshape, (ISegmentCollection) trimmedReplacement,
			                firstSegmentIndex, lastSegmentIndex);
		}

		public static void ReplaceSegments([NotNull] IPath curve,
		                                   [NotNull] ISegmentCollection replacement,
		                                   int firstSegmentIndex,
		                                   int lastSegmentIndex)
		{
			ReplaceSegments(curve, replacement, firstSegmentIndex,
			                lastSegmentIndex, null);
		}

		/// <summary>
		/// Replaces segments of the original ring which are between the first and the last
		/// cut point provided.
		/// </summary>
		/// <param name="originalRing"></param>
		/// <param name="trimmedReplacementPath"></param>
		/// <param name="reshapeSide"></param>
		/// <param name="nonPlanar">Whether or not the geometry contains vertically coincident parts and the
		/// selection of the segments to replace has to be considered in 3D.
		/// and the </param>
		/// <param name="potentialPhantomPoints"></param>
		public static void ReplaceSegments(
			[NotNull] IRing originalRing,
			[NotNull] IPath trimmedReplacementPath,
			RingReshapeSideOfLine reshapeSide,
			bool nonPlanar,
			[CanBeNull] IPointCollection potentialPhantomPoints)
		{
			Assert.ArgumentCondition(reshapeSide != RingReshapeSideOfLine.Undefined,
			                         "Reshape side is not defined.");

			int firstSegmentIndex;
			int lastSegmentIndex;

			IPoint firstCutPoint = trimmedReplacementPath.FromPoint;
			IPoint lastCutPoint = trimmedReplacementPath.ToPoint;

			// TODO: Measure performance impact of EnsureConnectVerticesExist (could be optimised in case of ReshapeBothSides)

			double xyTolerance = GeometryUtils.GetXyTolerance(originalRing);
			double? zTolerance = nonPlanar
				                     ? (double?) GeometryUtils.GetZTolerance(originalRing)
				                     : null;

			EnsureConnectVerticesExist(originalRing, firstCutPoint, lastCutPoint, xyTolerance,
			                           zTolerance);

			var originalSegments = (ISegmentCollection) originalRing;

			if (reshapeSide == RingReshapeSideOfLine.Left) // swap the first and the last point
			{
				firstSegmentIndex = GetSegmentIndex(originalSegments, lastCutPoint, xyTolerance,
				                                    zTolerance, true);
				lastSegmentIndex = GetSegmentIndex(originalSegments, firstCutPoint, xyTolerance,
				                                   zTolerance, false);
			}
			else
			{
				firstSegmentIndex = GetSegmentIndex(originalSegments, firstCutPoint, xyTolerance,
				                                    zTolerance, true);
				lastSegmentIndex = GetSegmentIndex(originalSegments, lastCutPoint, xyTolerance,
				                                   zTolerance, false);
			}

			ReplaceSegments(originalRing, (ISegmentCollection) trimmedReplacementPath,
			                firstSegmentIndex, lastSegmentIndex, potentialPhantomPoints,
			                nonPlanar);
		}

		/// <summary>
		/// Tries to replace the 2 segments adjacent to the specified point with the specified replacement segment.
		/// Returns false if the two segments adjacent to the specified point cannot be seamlessly replaced by the 
		/// replacement segment.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="adjacentToPoint"></param>
		/// <param name="replacementSegment"></param>
		public static bool TryReplaceSegments([NotNull] IPolycurve polycurve,
		                                      [NotNull] IPoint adjacentToPoint,
		                                      [NotNull] ISegment replacementSegment)
		{
			double tolerance = GeometryUtils.GetXyTolerance(polycurve);

			int partIndex;
			const bool isSegmentFromPoint = true;
			const bool allowNoMatch = true;
			int? secondSegmentIndex = GetSegmentIndex(
				polycurve, adjacentToPoint, tolerance, out partIndex, isSegmentFromPoint,
				allowNoMatch);

			if (secondSegmentIndex == null)
			{
				return false;
			}

			if (secondSegmentIndex == 0 &&
			    polycurve.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				// Adjacent point is the first point -> cannot get previous
				return false;
			}

			var partSegments =
				(ISegmentCollection) ((IGeometryCollection) polycurve).Geometry[partIndex];

			int firstSegmentIndex = (int) secondSegmentIndex > 0
				                        ? (int) secondSegmentIndex - 1
				                        : partSegments.SegmentCount - 1;

			ISegment firstSegment = partSegments.Segment[firstSegmentIndex];
			ISegment secondSegment = partSegments.Segment[(int) secondSegmentIndex];

			if (! GeometryUtils.AreEqualInXY(secondSegment.FromPoint, adjacentToPoint))
			{
				// unexpected situation: The ajdacent point is not the point between the segments
				return false;
			}

			// NOTE: It is important not to insert the provided replacement segment but a clone.
			// The replacement segment could be used also for other geometries which would result in nasty bugs such as inverted segments.
			ISegment actualReplacement;
			if (GeometryUtils.AreEqual(replacementSegment.FromPoint, firstSegment.FromPoint) &&
			    GeometryUtils.AreEqual(replacementSegment.ToPoint, secondSegment.ToPoint))
			{
				actualReplacement = GeometryFactory.Clone(replacementSegment);
			}
			else if (
				GeometryUtils.AreEqual(replacementSegment.FromPoint, secondSegment.ToPoint) &&
				GeometryUtils.AreEqual(replacementSegment.ToPoint, firstSegment.FromPoint))
			{
				// simplify could have reversed the segments' orientation
				actualReplacement = GeometryFactory.Clone(replacementSegment);
				actualReplacement.ReverseOrientation();
			}
			else if (IsPointOnSegment(firstSegment.FromPoint, replacementSegment) &&
			         IsPointOnSegment(secondSegment.ToPoint, replacementSegment))
			{
				// NOTE: Closed reshape path consisting of a single segment (circle): this actually works
				// but later on ITopologicalOperator.Intersect() incorrectly only finds 1 point which results
				// in a warning (not enough intersection points)
				actualReplacement = (ISegment) GetCurveBetween(
					firstSegment.FromPoint, secondSegment.ToPoint, replacementSegment);
			}
			else
			{
				// the start / end of the replacement is not even on the geometry
				actualReplacement = null;
			}

			if (actualReplacement != null)
			{
				ISegment[] replacementArray = {actualReplacement};

				if (((IPath) partSegments).IsClosed &&
				    GeometryUtils.AreEqual(((IPath) partSegments).FromPoint, adjacentToPoint))
				{
					// replace the last and the first segment with the replacement
					GeometryUtils.GeometryBridge.ReplaceSegments(partSegments,
					                                             partSegments.SegmentCount - 1, 1,
					                                             replacementArray);

					partSegments.RemoveSegments(0, 1, false);
				}
				else
				{
					GeometryUtils.GeometryBridge.ReplaceSegments(partSegments, firstSegmentIndex, 2,
					                                             replacementArray);
				}

				partSegments.SegmentsChanged();
			}

			return actualReplacement != null;
		}

		/// <summary>
		/// Whether the point lies anywhere on the polycurve, i.e. the polygon boundary or on the polyline.
		/// Polygon: touches
		/// Polyline: intersects
		/// </summary>
		/// <param name="point"></param>
		/// <param name="polycurve"></param>
		/// <returns></returns>
		private static bool IsPointOnPolycurve(IPoint point, IPolycurve polycurve)
		{
			return
				polycurve.GeometryType == esriGeometryType.esriGeometryPolygon
					? GeometryUtils.Touches(polycurve, point)
					: GeometryUtils.Intersects(polycurve, point);
		}

		private static bool IsPointOnSegment(IPoint point, ISegment segment)
		{
			// TODO: add option to not clone the segment and re-use a template polyline
			var highLevelSegment =
				(IPolyline) GeometryUtils.GetHighLevelGeometry(segment);

			bool result = IsPointOnPolycurve(point, highLevelSegment);

			Marshal.ReleaseComObject(highLevelSegment);

			return result;
		}

		[NotNull]
		public static IPath GetSegmentsToReplace(
			[NotNull] IRing ring,
			[NotNull] IPoint firstCutPoint,
			[NotNull] IPoint lastCutPoint,
			RingReshapeSideOfLine reshapeSide)
		{
			Assert.ArgumentCondition(reshapeSide != RingReshapeSideOfLine.Undefined,
			                         "Undefined reshape side");

			if (reshapeSide == RingReshapeSideOfLine.Left)
			{
				// swap the points
				IPoint tempFirst = firstCutPoint;
				firstCutPoint = lastCutPoint;
				lastCutPoint = tempFirst;
			}

			return GetSegmentsBetween(firstCutPoint, lastCutPoint, ring);
		}

		[NotNull]
		public static IPath GetUnreplacedSegments(
			[NotNull] IRing ring,
			[NotNull] IPoint firstCutPoint,
			[NotNull] IPoint lastCutPoint,
			RingReshapeSideOfLine reshapeSide)
		{
			Assert.ArgumentCondition(reshapeSide != RingReshapeSideOfLine.Undefined,
			                         "Undefined reshape side");

			if (reshapeSide == RingReshapeSideOfLine.Left)
			{
				// swap the points
				IPoint tempFirst = firstCutPoint;
				firstCutPoint = lastCutPoint;
				lastCutPoint = tempFirst;
			}

			return GetSegmentsBetween(lastCutPoint, firstCutPoint, ring);
		}

		[NotNull]
		public static IPath GetSegmentsBetween([NotNull] IPoint firstPoint,
		                                       [NotNull] IPoint lastPoint,
		                                       [NotNull] IRing ring)
		{
			double startDistance = GeometryUtils.GetDistanceAlongCurve(
				ring, firstPoint);

			double endDistance = GeometryUtils.GetDistanceAlongCurve(
				ring, lastPoint);

			const bool useRingOrientation = false;

			var result = (IPath) ring.GetSubcurveEx(
				startDistance, endDistance, false, ! ring.IsExterior, useRingOrientation);

			if (MathUtils.AreEqual(startDistance, endDistance) && ! result.IsEmpty)
			{
				// WORK-AROUND: The Z value of the start/end point is NAN
				IPoint startPoint = result.FromPoint;

				if (double.IsNaN(startPoint.Z))
				{
					_msg.DebugFormat("Fixing start/end point Z of extracted ring...");

					IGeometry highLevelRing = GeometryUtils.GetHighLevelGeometry(ring, true);

					startPoint.Z = GeometryUtils.GetZValueFromGeometry(highLevelRing,
						result.FromPoint);

					Marshal.ReleaseComObject(highLevelRing);

					((IPointCollection) result).UpdatePoint(0, startPoint);
				}
			}

			return result;
		}

		[NotNull]
		public static IPath GetSegmentsBetween([NotNull] IPoint firstPoint,
		                                       [NotNull] IPoint lastPoint,
		                                       [NotNull] IPath path)
		{
			return (IPath) GetCurveBetween(firstPoint, lastPoint, path);
		}

		[NotNull]
		public static ICurve GetCurveBetween([NotNull] IPoint firstPoint,
		                                     [NotNull] IPoint lastPoint,
		                                     [NotNull] ICurve curve)
		{
			Assert.ArgumentNotNull(firstPoint);
			Assert.ArgumentNotNull(lastPoint);
			Assert.ArgumentNotNull(curve);

			const bool asRatio = false;

			double startDistance = GeometryUtils.GetDistanceAlongCurve(
				curve, firstPoint, asRatio, out IPoint _);

			double endDistance = GeometryUtils.GetDistanceAlongCurve(
				curve, lastPoint, asRatio, out IPoint _);

			ICurve result;
			curve.GetSubcurve(startDistance, endDistance, asRatio, out result);

			// NOTE: If startDistance==endDistance this returns an empty result (not just a line with length==0)
			//       That empty result is not Z aware or M aware even if the input was
			if (! result.IsEmpty)
			{
				AssertZProperties(result, curve);
				AssertMProperties(result, curve);
			}

			return result;
		}

		/// <summary>
		/// Gets the segment index of the provided point from the geometry.
		/// The point is expected to be the segment's ToPoint or an intermediate
		/// point. Therefore if a segment's FromPoint is provided the previous
		/// segment index is returned.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="segmentToPoint">The segment to point.</param>
		/// <param name="searchTolerance">The search tolerance.</param>
		/// <param name="partIndex">Index of the part.</param>
		/// <returns></returns>
		public static int GetSegmentIndex([NotNull] IGeometry geometry,
		                                  [NotNull] IPoint segmentToPoint,
		                                  double searchTolerance,
		                                  out int partIndex)
		{
			const bool pointIsSegmentFromPoint = false;
			return GetSegmentIndex(geometry,
			                       segmentToPoint,
			                       searchTolerance,
			                       out partIndex,
			                       pointIsSegmentFromPoint);
		}

		public static int GetSegmentIndex([NotNull] IGeometry geometry,
		                                  [NotNull] IPoint segmentPoint,
		                                  double searchTolerance,
		                                  out int partIndex,
		                                  bool pointIsSegmentFromPoint)
		{
			const bool allowNoMatch = false;
			int? segmentIndex = GetSegmentIndex(geometry,
			                                    segmentPoint,
			                                    searchTolerance,
			                                    out partIndex,
			                                    pointIsSegmentFromPoint,
			                                    allowNoMatch);

			if (segmentIndex == null)
			{
				throw new InvalidOperationException(string.Format("No segment found at {0}, {1}",
					                                    segmentPoint.X, segmentPoint.Y));
			}

			return segmentIndex.Value;
		}

		public static int GetSegmentIndex([NotNull] ISegmentCollection segmentCollection,
		                                  [NotNull] IPoint segmentPoint,
		                                  double searchToleranceXy,
		                                  double? searchToleranceZ,
		                                  bool pointIsSegmentFromPoint)
		{
			int? segmentIndex;

			if (searchToleranceZ != null && GeometryUtils.IsZAware(segmentPoint))
			{
				// TODO: Proper implementation using indexed geometry, improved 3D intersection test
				segmentIndex = GetSegmentIndex3D(segmentCollection, segmentPoint,
				                                 searchToleranceXy, pointIsSegmentFromPoint);
			}
			else
			{
				const bool allowNoMatch = false;
				segmentIndex = GetSegmentIndex(
					(IGeometry) segmentCollection, segmentPoint, searchToleranceXy,
					out int _, pointIsSegmentFromPoint, allowNoMatch);
			}

			if (segmentIndex == null)
			{
				throw new InvalidOperationException(string.Format("No segment found at {0}, {1}",
					                                    segmentPoint.X, segmentPoint.Y));
			}

			return segmentIndex.Value;
		}

		/// <summary>
		/// Returns the segment index of the segment point which is the to- or from-point
		/// of a segment in the geometry.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="segmentPoint">The from- or to-point of the searched segment.</param>
		/// <param name="searchTolerance"></param>
		/// <param name="partIndex">The part index of the segment</param>
		/// <param name="pointIsSegmentFromPoint">Whether the segmentPoint is the segment from-point or not. 
		/// If false, it is considered the segment to-point.</param>
		/// <param name="allowNoMatch"></param>
		/// <returns>The (local) segment index of the part specified by part index or null if no segment was
		/// found.</returns>
		public static int? GetSegmentIndex([NotNull] IGeometry geometry,
		                                   [NotNull] IPoint segmentPoint,
		                                   double searchTolerance,
		                                   out int partIndex,
		                                   bool pointIsSegmentFromPoint,
		                                   bool allowNoMatch)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(segmentPoint, nameof(segmentPoint));
			Assert.True(geometry is ISegmentCollection,
			            "geometry parameter must be a segment collection.");
			Assert.False(geometry.IsEmpty, "geometry must not be empty.");
			Assert.False(segmentPoint.IsEmpty, "segmentPoint parameter must not be empty.");

			int? segmentIndex = GeometryUtils.FindHitSegmentIndex(geometry, segmentPoint,
				searchTolerance,
				out partIndex);

			if (segmentIndex == null)
			{
				// NOTE: this happens sometimes (even if simple, regardless of AllowIndexing property etc.)
				//		 the resolution is very (artificially) small so it shouldn't be snap to SR either
				// Example: Tolerance is 0.0004, distance between the point and a vertex is 0.00044 but
				//			the point was generated by IPolycurve2.SplitAtPoints with the same tolerance
				//			and IRelationalOperator.IsEqual returns true (!) for the segmentPoint and the
				//			hit vertex when using hit test with 0.00044.
				// TODO: investigate the original split points created by ITopologicalOperator.Intersect()

				// WORK-AROUND
				double expandedSearchTolerance = searchTolerance + searchTolerance * 0.5;

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("The segment point {0} was not found in the geometry {1}.",
					                 GeometryUtils.ToString(segmentPoint),
					                 GeometryUtils.ToString(geometry));
				}
				else
				{
					_msg.DebugFormat(
						"The segment point {0}|{1} was not found in the geometry. Trying again with expanded search tolerance {2}.",
						segmentPoint.X, segmentPoint.Y, expandedSearchTolerance);
				}

				segmentIndex = GeometryUtils.FindHitSegmentIndex(
					geometry, segmentPoint, expandedSearchTolerance, out partIndex);

				if (segmentIndex == null && allowNoMatch)
				{
					return null;
				}

				Assert.NotNull(segmentIndex, "The segment point is not on the geometry.");

				// END WORK-AROUND
			}

			return GetAdaptedSegmentIndex(geometry, segmentPoint, (int) segmentIndex, partIndex,
			                              pointIsSegmentFromPoint);
		}

		public static IList<int> GetAdjacentSegmentIndexes(
			[NotNull] IPolycurve geometry,
			[NotNull] IPoint segmentPoint,
			double searchTolerance,
			out int partIndex,
			bool allowNoMatch = false)
		{
			const bool pointIsSegmentFromPoint = false;

			int? previousSegmentIdx = GetSegmentIndex(geometry, segmentPoint, searchTolerance,
			                                          out partIndex, pointIsSegmentFromPoint,
			                                          allowNoMatch);
			var result = new List<int>(2);

			if (previousSegmentIdx != null)
			{
				result.Add(previousSegmentIdx.Value);

				// check whether the point is on the segment interior or the segment's to-point
				ISegment previousSegment = GetSegment(geometry, partIndex,
				                                      previousSegmentIdx.Value);

				if (GeometryUtils.AreEqualInXY(previousSegment.ToPoint, segmentPoint))
				{
					// add the next segment
					int? nextSegment = null;

					int partSegmentCount =
						((ISegmentCollection) GetPart(geometry, partIndex)).SegmentCount;

					if (previousSegmentIdx != partSegmentCount - 1)
					{
						nextSegment = previousSegmentIdx.Value + 1;
					}
					else
					{
						// it's the last segment, add the first if the path is closed:
						var curve = GetPart(geometry, partIndex) as ICurve;

						if (curve != null && curve.IsClosed)
						{
							nextSegment = 0;
						}
					}

					if (nextSegment != null)
					{
						result.Add(nextSegment.Value);
					}
				}
			}

			return result;
		}

		public static void EnsureVertexExists([NotNull] IPoint vertex,
		                                      [NotNull] IGeometry inGeometry,
		                                      int localSegmentIndex,
		                                      int partIndex)
		{
			// only leave old Z if there is a better Z value in the original geometry
			bool leaveOldZ = GeometryUtils.IsZAware(inGeometry) && double.IsNaN(vertex.Z);

			EnsureVertexExists(vertex, inGeometry, localSegmentIndex, partIndex, leaveOldZ);
		}

		public static void EnsureVertexExists([NotNull] IPoint vertex,
		                                      [NotNull] IGeometry inGeometry,
		                                      int localSegmentIndex,
		                                      int partIndex,
		                                      bool leaveOldZ)
		{
			TryEnsureVertexExists(vertex, inGeometry, localSegmentIndex, partIndex, leaveOldZ,
			                      double.NaN);
		}

		public static bool TryEnsureVertexExists([NotNull] IPoint vertex,
		                                         [NotNull] IGeometry inGeometry,
		                                         int localSegmentIndex,
		                                         int partIndex,
		                                         bool leaveOldZ,
		                                         double minimumSegmentLength)
		{
			Assert.ArgumentNotNull(vertex, nameof(vertex));
			Assert.ArgumentNotNull(inGeometry, nameof(inGeometry));
			Assert.ArgumentCondition(inGeometry is ISegmentCollection,
			                         "inGeometry must be segment collection");

			ISegment cutSegment = GetSegment(inGeometry, partIndex, localSegmentIndex);

			bool vertexIsToPoint = GeometryUtils.AreEqualInXY(cutSegment.ToPoint, vertex);
			bool vertexIsFromPoint = GeometryUtils.AreEqualInXY(cutSegment.FromPoint, vertex);

			var collection = inGeometry as IGeometryCollection;
			IGeometry geometry = collection == null
				                     ? inGeometry
				                     : collection.Geometry[partIndex];

			var result = true;
			if (! vertexIsToPoint && ! vertexIsFromPoint)
			{
				// the point is on the segment but neither the to nor the from point -> add the point
				result = TryInsertVertex(vertex, localSegmentIndex, partIndex,
				                         (ISegmentCollection) inGeometry,
				                         leaveOldZ, minimumSegmentLength);

				// TODO: if (! result) -> Update the Vertex that causes the minimum segment
				// (often vertexIsFromPoint/vertexIsToPoint is not quite correct!
			}
			else if (! leaveOldZ)
			{
				// ensure Z value is identical to avoid gaps resulting in non-simple geometry
				EnsureVertex(vertex, localSegmentIndex, (ICurve) geometry, vertexIsFromPoint);
			}

			return result;
		}

		public static bool EnsureVertexExists([NotNull] IPoint vertex,
		                                      [NotNull] IGeometry inGeometry,
		                                      int localSegmentIndex,
		                                      int partIndex,
		                                      bool leaveOldZ,
		                                      double existingVertexSearchTolerance)
		{
			Assert.ArgumentNotNull(vertex, nameof(vertex));
			Assert.ArgumentNotNull(inGeometry, nameof(inGeometry));
			Assert.ArgumentCondition(inGeometry is ISegmentCollection,
			                         "inGeometry must be segment collection");

			ISegment cutSegment = GetSegment(inGeometry, partIndex, localSegmentIndex);

			IPoint fromPoint = cutSegment.FromPoint;
			IPoint toPoint = cutSegment.ToPoint;

			double vertexDistanceToFromPoint = GeometryUtils.GetPointDistance3D(vertex,
				fromPoint);
			double vertexDistanceToToPoint = GeometryUtils.GetPointDistance3D(vertex, toPoint);

			var collection = inGeometry as IGeometryCollection;
			IGeometry geometry = collection == null
				                     ? inGeometry
				                     : collection.Geometry[partIndex];

			if (vertexDistanceToFromPoint > existingVertexSearchTolerance &&
			    vertexDistanceToToPoint > existingVertexSearchTolerance)
			{
				// the point is on the segment but neither the to nor the from point -> add the point
				InsertVertex(vertex, localSegmentIndex, partIndex, (ISegmentCollection) inGeometry,
				             leaveOldZ);
				return true;
			}

			bool updateFromPoint = vertexDistanceToFromPoint < vertexDistanceToToPoint;
			return EnsureVertex(vertex, localSegmentIndex, (ICurve) geometry, updateFromPoint);
		}

		/// <summary>
		/// Inserts a vertex into the segment collection by keeping the characteristics of the segment hit by the 
		/// vertex to be inserted.
		/// </summary>
		/// <param name="vertex"></param>
		/// <param name="localSegmentIndex"></param>
		/// <param name="partIndex"></param>
		/// <param name="allSegments"></param>
		/// <param name="interpolateZ"></param>
		public static void InsertVertex([NotNull] IPoint vertex, int localSegmentIndex,
		                                int partIndex,
		                                [NotNull] ISegmentCollection allSegments,
		                                bool interpolateZ = false)
		{
			const double minimumSegmentLength = double.NaN;

			TryInsertVertex(vertex, localSegmentIndex, partIndex, allSegments, interpolateZ,
			                minimumSegmentLength);
		}

		/// <summary>
		/// Inserts a vertex into the segment collection by keeping the characteristics of the segment hit by the 
		/// vertex to be inserted.
		/// </summary>
		/// <param name="vertex"></param>
		/// <param name="localSegmentIndex"></param>
		/// <param name="partIndex"></param>
		/// <param name="allSegments"></param>
		/// <param name="interpolateZ"></param>
		/// <param name="minimumSegmentLength"></param>
		public static bool TryInsertVertex([NotNull] IPoint vertex,
		                                   int localSegmentIndex,
		                                   int partIndex,
		                                   [NotNull] ISegmentCollection allSegments,
		                                   bool interpolateZ,
		                                   double minimumSegmentLength)
		{
			Assert.ArgumentNotNull(vertex, nameof(vertex));
			Assert.ArgumentNotNull(allSegments, nameof(allSegments));

			int globalSegmentIndex = GeometryUtils.GetGlobalSegmentIndex(
				(IGeometry) allSegments, partIndex, localSegmentIndex);

			ISegment segment = allSegments.Segment[globalSegmentIndex];

			double distanceAlong = GeometryUtils.GetDistanceAlongCurve(
				segment, vertex, false);

			if (! double.IsNaN(minimumSegmentLength))
			{
				if (distanceAlong < minimumSegmentLength ||
				    segment.Length - distanceAlong < minimumSegmentLength)
				{
					_msg.VerboseDebug(
						() =>
							$"Unable to insert vertex because of violated minimum segment length. Distance along segment: {distanceAlong}, segment: {segment.Length}");

					return false;
				}
			}

			ISegment newFrom, newTo;
			segment.SplitAtDistance(distanceAlong, false, out newFrom, out newTo);

			var newSegments = new[] {newFrom, newTo};

			GeometryUtils.GeometryBridge.ReplaceSegments(allSegments, globalSegmentIndex, 1,
			                                             ref newSegments);

			if (! interpolateZ)
			{
				// not the same as global segment index!
				int globalPointIndex = GeometryUtils.GetGlobalIndex((IGeometry) allSegments,
					partIndex, localSegmentIndex);

				EnsureVertex(vertex, globalPointIndex, (ICurve) allSegments, false);
			}

			return true;
		}

		public static void InsertSegments([NotNull] ISegment[] segments,
		                                  [NotNull] ISegmentCollection toSegmentCollection,
		                                  int atIndex)
		{
			if (atIndex == toSegmentCollection.SegmentCount)
			{
				// NOTE: InsertSegments() throws error when inserting at the end of the collection
				GeometryUtils.GeometryBridge.AddSegments(toSegmentCollection, ref segments);
			}
			else
			{
				GeometryUtils.GeometryBridge.InsertSegments(toSegmentCollection, atIndex,
				                                            ref segments);
			}
		}

		/// <summary>
		/// Joins the specified segment collection to another segment collection assuming that
		/// they share a start/end point
		/// </summary>
		/// <param name="path"></param>
		/// <param name="toPath"></param>
		public static void JoinConnectedPaths([NotNull] IPath path, [NotNull] IPath toPath)
		{
			IPoint updatePathFrom = toPath.FromPoint;
			IPoint updatePathTo = toPath.ToPoint;

			if (GeometryUtils.AreEqualInXY(path.FromPoint, updatePathFrom))
			{
				path.ReverseOrientation();
			}

			if (GeometryUtils.AreEqualInXY(path.ToPoint, updatePathTo))
			{
				path.ReverseOrientation();
			}

			var updateCollection = (ISegmentCollection) toPath;

			const int partIdx = 0;

			if (GeometryUtils.AreEqualInXY(path.ToPoint, updatePathFrom))
			{
				// ensure Z value of old geometry to avoid creating multipart geometry:
				const int firstSegmentIdx = 0;
				const bool leaveOldZ = false;

				EnsureVertexExists(path.ToPoint, toPath, firstSegmentIdx, partIdx, leaveOldZ);

				updateCollection.InsertSegmentCollection(firstSegmentIdx,
				                                         (ISegmentCollection) path);
			}
			else if (GeometryUtils.AreEqualInXY(updatePathTo, path.FromPoint))
			{
				int lastSegmentIdx = updateCollection.SegmentCount - 1;
				EnsureVertexExists(path.FromPoint, toPath, lastSegmentIdx, partIdx);

				updateCollection.InsertSegmentCollection(lastSegmentIdx + 1,
				                                         (ISegmentCollection) path);
			}
			else
			{
				throw new AssertionException(
					string.Format(
						"Unable to join the specified paths. They are not connected. Path: {0}, toPath: {1}",
						GeometryUtils.ToString(path), GeometryUtils.ToString(toPath)));
			}
		}

		/// <summary>
		/// Removes the phantom points inserted by ReplaceSegmentCollection method.
		/// </summary>
		/// <param name="processedCurve"></param>
		/// <param name="originalGeometry"></param>
		/// <param name="replacementGeometry"></param>
		public static void RemovePhantomPointInserts([NotNull] ICurve processedCurve,
		                                             [NotNull] IGeometry originalGeometry,
		                                             [NotNull] IGeometry replacementGeometry)
		{
			// identify extra points that 'remain' after replacement:
			const bool assumeIntersecting = true;
			var intersections =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(originalGeometry,
					replacementGeometry,
					assumeIntersecting);

			_msg.DebugFormat(
				"Found {0} intersections (potential phantom point inserts) between original and replacement geometry",
				intersections.PointCount);

			// NOTE: use replacement geometry as original in RemoveCutPointsService.GetPointsToRemove otherwise the detection 
			// whether the point was really inserted is not correct: the original geometry can have a vertex at the intersection, 
			// we only want to remove a vertex that was inserted into the processed curve with respect to the replacement geometry
			RemovePhantomPointInserts(processedCurve, intersections, replacementGeometry);

			Marshal.ReleaseComObject(intersections);
		}

		/// <summary>
		/// Removes the phantom points inserted by ReplaceSegmentCollection method. If the intersection
		/// points were pre-calculated the memory/performance cost of calculating intersection points
		/// can be saved.
		/// </summary>
		/// <param name="processedCurve"></param>
		/// <param name="potentialPhantomPoints"></param>
		/// <param name="replacementGeometry"></param>
		public static void RemovePhantomPointInserts(
			[NotNull] ICurve processedCurve,
			[NotNull] IPointCollection potentialPhantomPoints,
			[NotNull] IGeometry replacementGeometry)
		{
			Stopwatch removePointsWatch =
				_msg.DebugStartTiming("Removing phantom point inserts...");

			IList<IPoint> removePoints = RemoveCutPointsService.GetPointsToRemove(
				processedCurve, potentialPhantomPoints, replacementGeometry);

			_msg.DebugFormat(
				"Removing {0} inserted intersections between original and replacement geometry",
				potentialPhantomPoints.PointCount);

			RemoveCutPointsService.RemovePoints(processedCurve, removePoints);

			_msg.DebugStopTiming(removePointsWatch, "Removed {0} phantom point inserts",
			                     removePoints?.Count ?? 0);
		}

		/// <summary>
		/// Replaces the segments in curve between the first and the last provided segment index.
		/// Removes phantom intersection points that remain after replacing segments.
		/// Ensures orientation and exact fit of replacement with the original curve to avoid
		/// creation of multiparts due to small (or Z-) deviations. Additionally Z values of the
		/// replacement are interpolated if the curve is z-aware and the replacement is not.
		/// </summary>
		/// <param name="curve">The curve which must be a ring or a path</param>
		/// <param name="replacement">The replacement path. Its orientation can be reversed, if necessary.</param>
		/// <param name="firstSegmentIndex">The first segment index to replace in the curve</param>
		/// <param name="lastSegmentIndex">The last segment index to replace in the curve</param>
		/// <param name="potentialPhantomPoints">Potential phantom intersect points that should be 
		/// removed if they do exist in the result. This should be the intersecting points between
		/// the original ring to reshape and the reshape line. 
		/// NOTE: without removal of the intermediate phantom intersection points this method is almost free.</param>
		/// <param name="nonPlanar">Whether the geometry can have vertically collinear segments that
		/// have to be dealt with.</param>
		private static void ReplaceSegments(
			[NotNull] IPath curve,
			[NotNull] ISegmentCollection replacement,
			int firstSegmentIndex,
			int lastSegmentIndex,
			[CanBeNull] IPointCollection potentialPhantomPoints,
			bool nonPlanar = false)
		{
			Stopwatch watch = _msg.DebugStartTiming();
			var originalSegments = (ISegmentCollection) curve;

			EnsureReplacementCurveFits(curve, (ICurve) replacement, firstSegmentIndex,
			                           lastSegmentIndex, nonPlanar);

			// actual replacement
			if (curve is IRing)
			{
				// TODO: https://issuetracker02.eggits.net/browse/TOP-4086
				//       check if all the intersection points are on the subcurve to be 
				//		 replaced in the source, i.e. between firstSegmentIndex and lastSegmentIndex
				ReplaceSegmentsInRing(originalSegments, replacement, firstSegmentIndex,
				                      lastSegmentIndex);
			}
			else
			{
				ReplaceSegmentsInPath(originalSegments, replacement, firstSegmentIndex,
				                      lastSegmentIndex);
			}

			// NOTE: ReplaceSegmentCollection also inserts the intersecting points between the
			//		 newSegments and the origional segment collection. This also happens when using
			//		 Remove and Insert instead: 
			if (potentialPhantomPoints != null)
			{
				// TODO: non-planar implementation
				RemovePhantomPointInserts(curve, potentialPhantomPoints,
				                          (IGeometry) replacement);
			}

			_msg.DebugStopTiming(watch, "Replaced segments (removed intersections: {0})",
			                     potentialPhantomPoints != null);
		}

		private static void ReplaceSegmentsInPath([NotNull] ISegmentCollection pathSegments,
		                                          [NotNull] ISegmentCollection replacement,
		                                          int firstSegmentIndex,
		                                          int lastSegmentIndex)
		{
			Assert.ArgumentNotNull(pathSegments, nameof(pathSegments));
			Assert.ArgumentNotNull(replacement, nameof(replacement));
			Assert.ArgumentNotNull(replacement, nameof(replacement));

			Assert.True(firstSegmentIndex <= lastSegmentIndex,
			            "First segment index must be smaller than last segment index.");

			int replaceCount = lastSegmentIndex - firstSegmentIndex + 1;

			ReplaceSegmentCollection(pathSegments, replacement, firstSegmentIndex,
			                         replaceCount);
		}

		private static void ReplaceSegmentsInRing([NotNull] ISegmentCollection ringSegments,
		                                          [NotNull] ISegmentCollection replacement,
		                                          int firstSegmentIndex,
		                                          int lastSegmentIndex)
		{
			Assert.ArgumentNotNull(ringSegments, nameof(ringSegments));
			Assert.ArgumentNotNull(replacement, nameof(replacement));
			Assert.ArgumentNotNull(replacement, nameof(replacement));

			if (firstSegmentIndex > lastSegmentIndex)
			{
				int replaceCount = ringSegments.SegmentCount - firstSegmentIndex;

				Assert.False(firstSegmentIndex >= ringSegments.SegmentCount,
				             "Unexpected first segment index.");

				ReplaceSegmentCollection(ringSegments, replacement, firstSegmentIndex,
				                         replaceCount);

				ringSegments.RemoveSegments(0, lastSegmentIndex + 1, false);
			}
			else
			{
				int replaceCount = lastSegmentIndex - firstSegmentIndex + 1;

				ReplaceSegmentCollection(ringSegments, replacement, firstSegmentIndex,
				                         replaceCount);
			}
		}

		/// <summary>
		/// Replaces part of the segment collection by ensuring the orientation of the
		/// new segments to avoid a non-simple result. The replacement must touch the
		/// old segments at existing vertex positions.
		/// </summary>
		/// <param name="segments"></param>
		/// <param name="replacement"></param>
		/// <param name="startIndex"></param>
		/// <param name="goingAway"></param>
		private static void ReplaceSegmentCollection(
			[NotNull] ISegmentCollection segments,
			[NotNull] ISegmentCollection replacement,
			int startIndex,
			int goingAway)
		{
			// ensure the orientation of the replacement is correct
			//var replacementCurve = (ICurve) replacement;

			//IPoint firstReplacedPoint = segments.get_Segment(startIndex).FromPoint;

			// correct replacement orientation if necessary
			// NOTE: in case of un-connected replacement the orientation needs to be corrected previously
			//EnsureCurveOrientation(segments, replacementCurve);

			segments.ReplaceSegmentCollection(startIndex, goingAway, replacement);
		}

		private static void FlipSegmentIndexes(
			[NotNull] ISegmentCollection segments,
			[NotNull] IPoint firstCutPoint,
			[NotNull] IPoint lastCutPoint,
			ref int firstSegmentIndex,
			ref int lastSegmentIndex)
		{
			int oldFirst = firstSegmentIndex;
			firstSegmentIndex = lastSegmentIndex;
			lastSegmentIndex = oldFirst;

			// adapt segment indexes unless its the very first / the very last vertex
			if (firstSegmentIndex > 0 ||
			    GeometryUtils.AreEqualInXY(segments.Segment[0].ToPoint, lastCutPoint))
			{
				firstSegmentIndex++;
			}

			if (lastSegmentIndex < segments.SegmentCount - 1 ||
			    GeometryUtils.AreEqualInXY(
				    segments.Segment[segments.SegmentCount - 1].FromPoint, firstCutPoint))
			{
				lastSegmentIndex--;
			}
		}

		/// <summary>
		/// Ensures that the replacement curve fits into the gap between first and last segment index if
		/// it starts / ends within the tolerance of an existing point on the original curve.
		/// Specifically it is ensured that the replacement curve is oriented correctly and starts / ends
		/// exactly at the respective connection points in x, y and z. Additionally Z values are interpolated
		/// if the original curve is Z-aware and the replacement curve is not.
		/// </summary>
		/// <param name="originalCurve"></param>
		/// <param name="replacementCurve"></param>
		/// <param name="firstSegmentIndex"></param>
		/// <param name="lastSegmentIndex"></param>
		/// <param name="nonPlanar">Whether or not the Z value should be regarded when comparing points.  Use 
		/// true for rings that can be vertical.</param>
		private static void EnsureReplacementCurveFits(
			[NotNull] ICurve originalCurve,
			[NotNull] ICurve replacementCurve,
			int firstSegmentIndex,
			int lastSegmentIndex,
			bool nonPlanar = false)
		{
			var originalSegments = (ISegmentCollection) originalCurve;

			IPoint replacementStartsAt =
				originalSegments.get_Segment(firstSegmentIndex).FromPoint;

			IPoint replacementEndsAt = originalSegments.get_Segment(lastSegmentIndex).ToPoint;

			// connection at first segment index
			if (AreEqual(replacementStartsAt, replacementCurve.FromPoint, nonPlanar))
			{
				// no re-orientation needed, just update vertex
				EnsureVertex(replacementCurve.FromPoint, firstSegmentIndex, originalCurve, true);
			}
			else if (AreEqual(replacementStartsAt, replacementCurve.ToPoint, nonPlanar))
			{
				EnsureVertex(replacementCurve.ToPoint, firstSegmentIndex, originalCurve, true);

				_msg.Debug(
					"EnsureReplacementCurveFits: Flipping replacement curve because to point equals replacement start.");

				replacementCurve.ReverseOrientation();
			}
			else if (firstSegmentIndex > 0)
				// otherwise it might be the option that allows moving the start point in lines
			{
				_msg.Debug(
					"EnsureReplacementCurveFits: Neither from- nor to-point of replacement curve fits first connect point of curve. There might result multiple parts.");
			}

			// connection at last segment index 
			if (AreEqual(replacementEndsAt, replacementCurve.ToPoint, nonPlanar))
			{
				EnsureVertex(replacementCurve.ToPoint, lastSegmentIndex, originalCurve, false);
			}
			else if (AreEqual(replacementEndsAt, replacementCurve.FromPoint, nonPlanar))
			{
				EnsureVertex(replacementCurve.FromPoint, lastSegmentIndex, originalCurve, false);

				_msg.Debug(
					"EnsureReplacementCurveFits: Flipping replacement curve because from point equals replacement end.");

				replacementCurve.ReverseOrientation();
			}
			else if (lastSegmentIndex < ((ISegmentCollection) originalCurve).SegmentCount - 1)
				// otherwise it might be the option that allows moving the end point in lines
			{
				_msg.Debug(
					"EnsureReplacementCurveFits: Neither from- nor to-point of replacement curve fits last connect point of curve. There might result multiple parts.");
			}

			if (GeometryUtils.IsZAware(originalCurve) &&
			    ! GeometryUtils.IsZAware(replacementCurve))
			{
				EnsureZs(replacementCurve, replacementStartsAt, replacementEndsAt, nonPlanar);
			}
		}

		private static void EnsureZs([NotNull] ICurve replacementCurve,
		                             [NotNull] IPoint replacementStartsAt,
		                             [NotNull] IPoint replacementEndsAt,
		                             bool nonPlanar = false)
		{
			GeometryUtils.MakeZAware(replacementCurve);

			IGeometry highLevelReplacementCurve =
				GeometryUtils.GetHighLevelGeometry(replacementCurve, true);

			// theoretically there could still be good Z values, even in the non-Z-aware replacement (e.g from mixed RAEF targets)
			if (GeometryUtils.TrySimplifyZ(highLevelReplacementCurve))
			{
				return;
			}

			// interpolate Zs (could also be done before storing, but this is more correct, especially when the reshpape curve 
			// is used in subsequent operations, such as single reshape in union.
			double fromZ, toZ;
			if (AreEqual(replacementStartsAt, replacementCurve.FromPoint, nonPlanar) &&
			    AreEqual(replacementEndsAt, replacementCurve.ToPoint, nonPlanar))
			{
				fromZ = replacementStartsAt.Z;
				toZ = replacementEndsAt.Z;
			}
			else if (AreEqual(replacementStartsAt, replacementCurve.ToPoint, nonPlanar) &&
			         AreEqual(replacementEndsAt, replacementCurve.FromPoint, nonPlanar))
			{
				fromZ = replacementEndsAt.Z;
				toZ = replacementStartsAt.Z;
			}
			else
			{
				_msg.DebugFormat(
					"No Z value interpolation of reshape path because no 2 intersections with original curve.");
				return;
			}

			var highLevelPoints = (IPointCollection) highLevelReplacementCurve;

			IPoint first = highLevelPoints.Point[0];
			first.Z = fromZ;

			highLevelPoints.UpdatePoint(0, first);

			int lastIdx = highLevelPoints.PointCount - 1;
			IPoint last = highLevelPoints.Point[lastIdx];
			last.Z = toZ;

			highLevelPoints.UpdatePoint(lastIdx, last);

			((IZ) highLevelPoints).InterpolateZsBetween(0, 0, 0, lastIdx);
		}

		private static bool AreEqual(IPoint point1, IPoint point2, bool nonPlanar)
		{
			bool planarEquals = GeometryUtils.AreEqualInXY(point1, point2);

			if (planarEquals && nonPlanar &&
			    GeometryUtils.IsZAware(point1) && GeometryUtils.IsZAware(point2))
			{
				double zTolerance1 = GeometryUtils.GetZTolerance(point1);
				double zTolerance2 = GeometryUtils.GetZTolerance(point2);

				Assert.AreEqual(zTolerance1, zTolerance2, "Z tolerance is not the same");

				return MathUtils.AreEqual(point1.Z, point2.Z, zTolerance1);
			}

			return planarEquals;
		}

		/// <summary>
		/// Adapts the segment index retrieved from HitTest depending on the indicated location
		/// of the point on the segment.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="segmentPoint">Point checked with HitTest</param>
		/// <param name="segmentIndex">The segment index provided by HitTest</param>
		/// <param name="partIndex"></param>
		/// <param name="pointIsSegmentFromPoint">Whether the segmentPoint is the from-point. If false
		/// it is considered the segment to-point.</param>
		/// <returns></returns>
		private static int GetAdaptedSegmentIndex([NotNull] IGeometry geometry,
		                                          [NotNull] IPoint segmentPoint,
		                                          int segmentIndex, int partIndex,
		                                          bool pointIsSegmentFromPoint)
		{
			// NOTE: the 0th segment is an exception: HitTest returns index 0 for
			//		 both the From- and the To-Point -> be consistent in rings and closed paths

			if (segmentIndex == 0)
			{
				ISegment firstSegment = GetSegment(geometry, partIndex, 0);

				if (GeometryUtils.AreEqualInXY(firstSegment.FromPoint, segmentPoint))
				{
					if (pointIsSegmentFromPoint)
					{
						return 0;
					}

					if (TryGetLastSegmentIdxInRing(geometry, partIndex,
					                               out int adaptedSegmentIndex))
					{
						return adaptedSegmentIndex;
					}
				}
			}

			if (GeometryUtils.HasNonLinearSegments(geometry))
			{
				// For non-linear segments, not always the first hit segment is reported
				ISegment segment = GetSegment(geometry, partIndex, segmentIndex);

				// Better play it safe:
				if (pointIsSegmentFromPoint &&
				    GeometryUtils.AreEqualInXY(segment.ToPoint, segmentPoint))
				{
					segmentIndex =
						TryGetNextSegmentIndex(geometry, segmentPoint, segmentIndex, partIndex);
				}

				if (! pointIsSegmentFromPoint &&
				    GeometryUtils.AreEqualInXY(segment.FromPoint, segmentPoint))
				{
					if (segmentIndex > 0)
					{
						segmentIndex--;
					}
					else if (TryGetLastSegmentIdxInRing(geometry, partIndex,
					                                    out int adaptedSegmentIndex))
					{
						return adaptedSegmentIndex;
					}
				}
			}
			else if (pointIsSegmentFromPoint)
			{
				// In linear segments (so far) always the first hit segment is reported
				segmentIndex =
					TryGetNextSegmentIndex(geometry, segmentPoint, segmentIndex, partIndex);
			}

			return segmentIndex;
		}

		private static bool TryGetLastSegmentIdxInRing(IGeometry geometry, int partIndex,
		                                               out int adaptedSegmentIndex)
		{
			// it's the ToPoint of the last segment:
			var curve = GetPart(geometry, partIndex) as ICurve;

			if (curve != null && curve.IsClosed)
			{
				int partSegmentCount = ((ISegmentCollection) curve).SegmentCount;

				{
					adaptedSegmentIndex = partSegmentCount - 1;
					return true;
				}
			}

			adaptedSegmentIndex = -1;
			return false;
		}

		private static int TryGetNextSegmentIndex([NotNull] IGeometry geometry,
		                                          [NotNull] IPoint segmentPoint,
		                                          int segmentIndex,
		                                          int partIndex)
		{
			int partSegmentCount =
				((ISegmentCollection) GetPart(geometry, partIndex)).SegmentCount;

			if (segmentIndex != partSegmentCount - 1)
			{
				segmentIndex++;
			}
			else
			{
				// Don't throw! Client code does not always check for this situation! (same for 1. vertex and ! pointIsSegmentFromPoint)
				_msg.DebugFormat(
					"The point {0} is the last point of the geometry {1} and cannot be segment from point.",
					GeometryUtils.ToString(segmentPoint), GeometryUtils.ToString(geometry));
			}

			return segmentIndex;
		}

		private static IGeometry GetPart([NotNull] IGeometry geometry,
		                                 int partIndex)
		{
			var geometryCollection = geometry as IGeometryCollection;

			IGeometry result = geometryCollection == null
				                   ? geometry
				                   : geometryCollection.Geometry[partIndex];

			return result;
		}

		[NotNull]
		public static ISegment GetSegment([NotNull] IGeometry geometry,
		                                  int partIndex,
		                                  int localSegmentIndex)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentCondition(geometry is ISegmentCollection,
			                         "geometry must be ISegmentCollection");

			_msg.VerboseDebug(
				() =>
					$"Retrieving segment {localSegmentIndex} from part {partIndex} in geometry {GeometryUtils.ToString(geometry)}");

			var geometryCollection = geometry as IGeometryCollection;

			// NOTE: not using segmentCollection.EnumSegments because we cannot determine if it clones or not

			ISegmentCollection segmentsOfPart;

			if (geometryCollection == null)
			{
				segmentsOfPart = (ISegmentCollection) geometry;
			}
			else
			{
				segmentsOfPart = (ISegmentCollection) geometryCollection.Geometry[partIndex];
			}

			return segmentsOfPart.Segment[localSegmentIndex];
		}

		private static int? GetSegmentIndex3D(
			[NotNull] ISegmentCollection segmentCollection,
			[NotNull] IPoint segmentPoint,
			double searchToleranceXy,
			bool pointIsSegmentFromPoint)
		{
			IList<int> foundIndexes2D = GeometryUtils.FindSegmentIndices(segmentCollection,
				segmentPoint,
				searchToleranceXy);

			foreach (int foundIndex in foundIndexes2D)
			{
				ISegment foundSegment = segmentCollection.Segment[foundIndex];

				IGeometry highLevelSegment = GeometryUtils.GetHighLevelGeometry(foundSegment);

				bool disjoint3D =
					((IRelationalOperator3D) highLevelSegment).Disjoint3D(segmentPoint);

				if (disjoint3D)
				{
					continue;
				}

				if (pointIsSegmentFromPoint &&
				    ! AreEqual(segmentPoint, foundSegment.FromPoint, true))
				{
					continue;
				}

				return foundIndex;
			}

			return null;
		}

		private static void EnsureConnectVerticesExist([NotNull] ICurve curveToReshape,
		                                               [NotNull] IPoint firstCutPoint,
		                                               [NotNull] IPoint lastCutPoint,
		                                               double xyTolerance,
		                                               double? zTolerance)
		{
			var segmentsToReshape = (ISegmentCollection) curveToReshape;

			// TODO: implement own hit-test method to be used everywhere and make pointIsSegmentFromPoint bool unnecessary
			int firstSegmentIndex = GetSegmentIndex(segmentsToReshape, firstCutPoint,
			                                        xyTolerance, zTolerance, false);

			const int partIndex = 0;
			EnsureVertexExists(firstCutPoint, curveToReshape, firstSegmentIndex, partIndex);

			int lastSegmentIndex = GetSegmentIndex(segmentsToReshape, lastCutPoint, xyTolerance,
			                                       zTolerance, false);

			EnsureVertexExists(lastCutPoint, curveToReshape, lastSegmentIndex, partIndex);
		}

		private static bool EnsureVertex([NotNull] IPoint vertex,
		                                 int segmentIndex,
		                                 [NotNull] ICurve inGeometry,
		                                 bool vertexIsFromPoint)
		{
			int pointIdx;

			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(vertex.X, vertex.Y);

			if (vertexIsFromPoint)
			{
				pointIdx = segmentIndex;
			}
			else if (inGeometry is IRing &&
			         segmentIndex == ((ISegmentCollection) inGeometry).SegmentCount - 1)
			{
				pointIdx = 0;
			}
			else
			{
				pointIdx = segmentIndex + 1;
			}

			IPoint point = ((IPointCollection) inGeometry).Point[pointIdx];

			bool pointEnsured = false;
			if (! MathUtils.AreEqual(point.X, vertex.X, epsilon))
			{
				point.X = vertex.X;
				pointEnsured = true;
			}

			if (! MathUtils.AreEqual(point.Y, vertex.Y, epsilon))
			{
				point.Y = vertex.Y;
				pointEnsured = true;
			}

			if (! double.IsNaN(vertex.Z) &&
			    ! MathUtils.AreEqual(point.Z, vertex.Z, epsilon))
			{
				// keep the old Z if the new vertex is not from a Z-aware geometry
				point.Z = vertex.Z;
				pointEnsured = true;
			}

			if (! double.IsNaN(vertex.M) &&
			    ! MathUtils.AreEqual(point.M, vertex.M, epsilon))
			{
				// keep the old M value, if the new value is NaN. But otherwise, if the old value is not 
				// updated, the end point of the last non-replaced segment does not fit the start point (different M)
				// of the first replaced segment -> simplify will create separate parts!
				point.M = vertex.M;
				pointEnsured = true;
			}

			if (pointEnsured)
			{
				((IPointCollection) inGeometry).UpdatePoint(pointIdx, point);
			}

			return pointEnsured;
		}

		#region Remove Short Segments

		/// <summary>
		/// Removes the short segments from a provided polycurve using 3D length if the
		/// geometry is Z-aware and 2D length otherwise.
		/// </summary>
		/// <param name="fromGeometry"></param>
		/// <param name="minimumSegmentLength">The minimum segment length</param>
		/// <param name="inPerimeter">Perimeter in which segments should be removed</param>
		/// <param name="notifications">The notifications</param>
		public static void RemoveShortSegments(
			[NotNull] IPolycurve fromGeometry,
			double minimumSegmentLength,
			[CanBeNull] IPolygon inPerimeter,
			[CanBeNull] NotificationCollection notifications)
		{
			const bool use2DLengthOnly = false;
			RemoveShortSegments(fromGeometry, minimumSegmentLength, use2DLengthOnly,
			                    inPerimeter, notifications);
		}

		/// <summary>
		/// Removes the short segments from a provided polycurve,
		/// </summary>
		/// <param name="fromGeometry"></param>
		/// <param name="minimumSegmentLength">The minimum segment length</param>
		/// <param name="only2D">Wether only the 2D length should be used, also for Z-aware geometries</param>
		/// <param name="inPerimeter">Perimeter in which segments should be removed</param>
		/// <param name="notifications">The notifications</param>
		public static void RemoveShortSegments(
			[NotNull] IPolycurve fromGeometry,
			double minimumSegmentLength, bool only2D,
			[CanBeNull] IPolygon inPerimeter,
			[CanBeNull] NotificationCollection notifications)
		{
			Stopwatch watch = _msg.DebugStartTiming("Removing segments shorter than {0}...",
			                                        minimumSegmentLength);

			bool use3DLength = ! only2D && GeometryUtils.IsZAware(fromGeometry);

			IList<esriSegmentInfo> shortSegments =
				GeometryUtils.GetShortSegments((ISegmentCollection) fromGeometry,
				                               minimumSegmentLength, use3DLength, inPerimeter);

			_msg.DebugFormat("Identified {0} short segments to remove.", shortSegments.Count);

			RemoveShortSegments(fromGeometry, shortSegments,
			                    minimumSegmentLength, only2D, null,
			                    inPerimeter, notifications);

			_msg.DebugStopTiming(watch, "Removed short segments");
		}

		/// <summary>
		/// Removes the short segments from a provided polycurve.
		/// </summary>
		/// <param name="fromGeometry"></param>
		/// <param name="shortSegmentInfos">The list of short segments</param>
		/// <param name="minimumSegmentLength">The minimum segment length</param>
		/// <param name="use2DLengthOnly"></param>
		/// <param name="protectedPoints"></param>
		/// <param name="inPerimeter">Polygon or envelope to which segment removal should be restricted</param>
		/// <param name="notifications">The notifications</param>
		public static void RemoveShortSegments(
			[NotNull] IPolycurve fromGeometry,
			[NotNull] IList<esriSegmentInfo> shortSegmentInfos,
			double minimumSegmentLength,
			bool use2DLengthOnly,
			[CanBeNull] IPointCollection protectedPoints,
			[CanBeNull] IGeometry inPerimeter,
			[CanBeNull] NotificationCollection notifications)
		{
			// TODO: separate class ShortSegmentsRemover
			const bool allowDegeneratingPaths = false;
			RemoveShortSegments(
				fromGeometry, shortSegmentInfos, minimumSegmentLength, use2DLengthOnly,
				allowDegeneratingPaths, protectedPoints, inPerimeter, notifications);
		}

		/// <summary>
		/// Removes the short segments from a provided polycurve.
		/// </summary>
		/// <param name="fromGeometry"></param>
		/// <param name="shortSegmentInfos">The list of short segments</param>
		/// <param name="minimumSegmentLength">The minimum segment length</param>
		/// <param name="use2DLengthOnly"></param>
		/// <param name="allowDegeneratingPaths">Whether the segments should even be removed if the path becomes invalid
		/// by removing it, such as a ring with less than 3 segments or a path with less than 1 segment.</param>
		/// <param name="protectedPoints"></param>
		/// <param name="inPerimeter">Polygon or envelope to which segment removal should be restricted</param>
		/// <param name="notifications">The notifications</param>
		public static void RemoveShortSegments(
			[NotNull] IPolycurve fromGeometry,
			[NotNull] IList<esriSegmentInfo> shortSegmentInfos,
			double minimumSegmentLength,
			bool use2DLengthOnly,
			bool allowDegeneratingPaths,
			[CanBeNull] IPointCollection protectedPoints,
			[CanBeNull] IGeometry inPerimeter,
			[CanBeNull] NotificationCollection notifications)
		{
			bool use3DLength = GeometryUtils.IsZAware(fromGeometry) && ! use2DLengthOnly;

			bool allRemoved = RemoveStandAloneShortSegments(fromGeometry,
			                                                shortSegmentInfos,
			                                                minimumSegmentLength, use3DLength,
			                                                allowDegeneratingPaths,
			                                                protectedPoints);

			// second run - remove the shortest to avoid deleting the longer one of two adjacent short segments
			if (! allRemoved)
			{
				_msg.Debug(
					"Some segments were skipped. Removing short segments ordered by length.");

				RemoveShortSegmentsOrderedByShortest(fromGeometry, protectedPoints,
				                                     minimumSegmentLength, inPerimeter,
				                                     use3DLength, allowDegeneratingPaths,
				                                     notifications);
			}
		}

		/// <summary>
		/// Removes short segments that are not adjacent to other short segments.
		/// </summary>
		/// <param name="fromPolycurve"></param>
		/// <param name="shortSegmentInfos"></param>
		/// <param name="minimumSegmentLength"></param>
		/// <param name="use3DLength"></param>
		/// <param name="allowDegeneratingPaths"></param>
		/// <param name="protectedPoints"></param>
		/// <returns>True, if all short segments are removed, false if some segments were skipped.</returns>
		private static bool RemoveStandAloneShortSegments(
			[NotNull] IPolycurve fromPolycurve,
			[NotNull] IList<esriSegmentInfo> shortSegmentInfos,
			double minimumSegmentLength,
			bool use3DLength,
			bool allowDegeneratingPaths,
			[CanBeNull] IPointCollection protectedPoints)
		{
			var segmentsRemoved = 0;

			// first run - only the 'stand-alone' short segments.
			var segmentsSkipped = false;
			for (var i = 0; i < shortSegmentInfos.Count; i++)
			{
				esriSegmentInfo segmentInfo = shortSegmentInfos[i];

				// NOTE: The segmentInfos are copies! -> changes in the geometry are not reflected.
				//       The part index should remain stable as no parts are deleted.
				int absSegmentIndex = segmentInfo.iAbsSegment - segmentsRemoved;

				if (! allowDegeneratingPaths &&
				    IsShortPart(segmentInfo.iPart, fromPolycurve, minimumSegmentLength,
				                use3DLength))
				{
					segmentsSkipped = true;
				}
				else
				{
					// check if the segment is still too short (deleting the previous could have elongated it)
					// consider deleting the shortest remaining to avoid cases that are hard to explain to the user
					ISegment currentSegment =
						((ISegmentCollection) fromPolycurve).Segment[absSegmentIndex];

					if (GeometryUtils.GetLength(currentSegment, use3DLength) < minimumSegmentLength)
					{
						// previous was also short or next is also short
						if (! IsStandaloneShortSegment(i, shortSegmentInfos))
						{
							segmentsSkipped = true;
							continue;
						}

						int localSegmentIndex = GetLocalSegmentIndex(fromPolycurve,
							absSegmentIndex,
							segmentInfo.iPart);

						if (TryRemoveSegment(fromPolycurve, localSegmentIndex,
						                     segmentInfo.iPart, protectedPoints))
						{
							segmentsRemoved++;
						}
					}
				}
			}

			return ! segmentsSkipped;
		}

		private static int GetLocalSegmentIndex([NotNull] IGeometry geometry,
		                                        int globalIndex,
		                                        int geometryPartIndex)
		{
			var geometryCollection = (IGeometryCollection) geometry;

			var previousPartsPointCount = 0;
			for (var i = 0; i < geometryPartIndex; i++)
			{
				var partSegments = (ISegmentCollection) geometryCollection.Geometry[i];

				previousPartsPointCount += partSegments.SegmentCount;
			}

			return globalIndex - previousPartsPointCount;
		}

		private static void RemoveShortSegmentsOrderedByShortest(
			[NotNull] IPolycurve polycurve,
			[CanBeNull] IPointCollection protectedPoints,
			double minimumSegmentLength,
			[CanBeNull] IGeometry inPerimeter,
			bool use3DLength,
			bool allowDegeneratingPaths,
			[CanBeNull] NotificationCollection notifications)
		{
			IList<int> shortPathsToProtect = allowDegeneratingPaths
				                                 ? null
				                                 : new List<int>();
			var shortSegmentDeleted = true;

			// as long as there is something to delete...
			while (shortSegmentDeleted)
			{
				IList<esriSegmentInfo> remainingShortSegmentInfos =
					GeometryUtils.GetShortSegments(polycurve,
					                               minimumSegmentLength, inPerimeter, use3DLength);

				shortSegmentDeleted = DeleteShortestSegment(polycurve,
				                                            remainingShortSegmentInfos,
				                                            protectedPoints,
				                                            shortPathsToProtect, use3DLength);
			}

			if (shortPathsToProtect != null)
			{
				foreach (int shortPathIndex in shortPathsToProtect)
				{
					NotificationUtils.Add(notifications,
					                      "Geometry part {0} would become empty or invalid by segment removal. Consider manually deleting the part.",
					                      shortPathIndex);
				}
			}
		}

		private static bool DeleteShortestSegment(
			[NotNull] IPolycurve segmentCollection,
			[NotNull] IEnumerable<esriSegmentInfo> allShortSegmentInfos,
			[CanBeNull] IPointCollection protectedPoints,
			[CanBeNull] ICollection<int> shortPathsToProtect,
			bool use3DLength)
		{
			double shortestSegmentLength = double.NaN;
			int shortestLocalSegmentIndex = -1;
			int shortestSegmentPart = -1;
			foreach (esriSegmentInfo shortSegmentInfo in allShortSegmentInfos)
			{
				// TODO: this is very inefficient when many adjacent short segments exist!
				if (! CanRemoveSegment(segmentCollection, shortSegmentInfo.iRelSegment,
				                       shortSegmentInfo.iPart, protectedPoints))
				{
					continue;
				}

				if (double.IsNaN(shortestSegmentLength) ||
				    GeometryUtils.GetLength(shortSegmentInfo.pSegment, use3DLength) <
				    shortestSegmentLength)
				{
					// exclude short parts with segment count = 1
					var partCurve = (ICurve)
						((IGeometryCollection) segmentCollection).Geometry[
							shortSegmentInfo.iPart];

					var partSegments = (ISegmentCollection) partCurve;

					if (shortPathsToProtect != null &&
					    partSegments.SegmentCount <= GetMinSegmentCount(partCurve))
					{
						if (! shortPathsToProtect.Contains(shortSegmentInfo.iPart))
						{
							shortPathsToProtect.Add(shortSegmentInfo.iPart);
						}

						continue;
					}

					shortestLocalSegmentIndex = shortSegmentInfo.iRelSegment;
					shortestSegmentPart = shortSegmentInfo.iPart;
					shortestSegmentLength =
						GeometryUtils.GetLength(shortSegmentInfo.pSegment, use3DLength);
				}
			}

			if (shortestLocalSegmentIndex >= 0 && shortestSegmentPart >= 0)
			{
				TryRemoveSegment(segmentCollection, shortestLocalSegmentIndex, shortestSegmentPart,
				                 protectedPoints);

				// return true even if it wasn't deleted, otherwise processing stops
				return true;
			}

			_msg.DebugFormat("No short segment found to delete.");
			return false;
		}

		private static int GetMinSegmentCount([NotNull] ICurve forPart)
		{
			// NOTE: IsClosed returns the wrong value for parts shorter than tolerance - add method
			return forPart.IsClosed
				       ? 3
				       : 1;
		}

		private static bool IsShortPart(int partIndex,
		                                [NotNull] IGeometry geometry,
		                                double minimumSegmentLength,
		                                bool use3DLength)
		{
			var partCurve = (ICurve) ((IGeometryCollection) geometry).Geometry[partIndex];

			var partSegments = (ISegmentCollection) partCurve;

			if (GeometryUtils.GetLength(partCurve, use3DLength) < minimumSegmentLength ||
			    partSegments.SegmentCount <= GetMinSegmentCount(partCurve))
			{
				return true;
			}

			return false;
		}

		private static bool CanRemoveSegment([NotNull] IPolycurve fromGeometry,
		                                     int localSegmentIndex,
		                                     int partIndex,
		                                     [CanBeNull] IPointCollection protectedPoints)
		{
			bool removeEntireSegment;
			int? localVertexToRemove = GetLocalVertexIndexToRemove(localSegmentIndex, partIndex,
				fromGeometry,
				protectedPoints,
				out removeEntireSegment);

			return localVertexToRemove != null || removeEntireSegment;
		}

		public static bool TryRemoveSegment([NotNull] IPolycurve fromGeometry,
		                                    int localSegmentIndex,
		                                    int partIndex,
		                                    [CanBeNull] IPointCollection protectedPoints)
		{
			// In case the previous / or next segment is non-linear, the segment should be removed rather than the vertex
			// because point removal (sometimes) leads to the destruction of adjacent non-linear segments
			bool removeEntireSegment;
			int? localVertexToRemove = GetLocalVertexIndexToRemove(localSegmentIndex, partIndex,
				fromGeometry,
				protectedPoints,
				out removeEntireSegment);

			if (removeEntireSegment)
			{
				const bool closeGap = true;
				RemoveSegment(fromGeometry, localSegmentIndex, partIndex, closeGap);
			}

			if (localVertexToRemove == null)
			{
				_msg.DebugFormat("Cannot remove segment index {0} in part index {1}",
				                 localSegmentIndex, partIndex);

				return false;
			}

			_msg.DebugFormat("Removing vertex {0} in part {1}", localVertexToRemove, partIndex);

			RemoveCutPointsService.RemovePoint(fromGeometry, partIndex,
			                                   (int) localVertexToRemove);

			return true;
		}

		private static void RemoveSegment(IPolycurve fromGeometry, int localSegmentIndex,
		                                  int partIndex, bool closeGap)
		{
			var segmentCollection = (ISegmentCollection) fromGeometry;

			int globalSegmentIndex = GeometryUtils.GetGlobalSegmentIndex(fromGeometry,
				partIndex,
				localSegmentIndex);

			var segmentsToRemove = new List<int> {globalSegmentIndex};

			GeometryUtils.RemoveSegments(segmentCollection, segmentsToRemove, closeGap);
		}

		private static int? GetLocalVertexIndexToRemove(
			int localSegmentIndex,
			int partIndex,
			[NotNull] IPolycurve fromGeometry,
			[CanBeNull] IPointCollection protectedPoints,
			out bool removeEntireSegment)
		{
			int? alternativeVertexToRemove;
			int? localVertexToRemove = GetLocalVertexIndexToRemove(
				fromGeometry, partIndex, localSegmentIndex, out alternativeVertexToRemove,
				out removeEntireSegment);

			if (localVertexToRemove == null && ! removeEntireSegment)
			{
				// nothing can be removed (stand-alone segment)
				return null;
			}

			if (protectedPoints == null)
			{
				// no point protection
				return localVertexToRemove;
			}

			if (removeEntireSegment)
			{
				if (HasProtectedEndpoint(fromGeometry, partIndex, localSegmentIndex,
				                         protectedPoints))
				{
					removeEntireSegment = false;
				}

				return null;
			}

			IPoint point1;
			if (! IsPointProtected(fromGeometry, partIndex, (int) localVertexToRemove,
			                       protectedPoints, out point1))
			{
				// point is not protected, remove it
				return localVertexToRemove;
			}

			if (alternativeVertexToRemove == null)
			{
				// TINA, and we cannot remove the protected point
				return null;
			}

			IPoint point2;
			if (! IsPointProtected(fromGeometry, partIndex, (int) alternativeVertexToRemove,
			                       protectedPoints, out point2))
			{
				return alternativeVertexToRemove;
			}

			// the alternative is also protected, only allow 0-length segments to be removed
			return GeometryUtils.IsSamePoint(point1, point2, double.Epsilon, double.Epsilon)
				       ? localVertexToRemove
				       : null;
		}

		private static int? GetLocalVertexIndexToRemove([NotNull] IPolycurve fromGeometry,
		                                                int partIndex,
		                                                int localSegmentIndex,
		                                                out int? alternativeVertexToRemove,
		                                                out bool removeEntireSegment)
		{
			IEnumSegment enumSegments = ((ISegmentCollection) fromGeometry).EnumSegments;

			int outPartIndex = -1;
			int outSegmentIndex = -1;

			ISegment thisSegment, previousSegment = null, nextSegment = null;

			enumSegments.SetAt(partIndex, localSegmentIndex);
			enumSegments.Next(out thisSegment, ref outPartIndex, ref outSegmentIndex);

			Assert.NotNull(thisSegment, "Segment {0} not found in part {1}", localSegmentIndex,
			               partIndex);

			// not last in a line-part:
			if (! enumSegments.IsLastInPart() || fromGeometry is IPolygon)
			{
				// in rings, go to the first after the last part:
				enumSegments.NextInPart(out nextSegment, ref outSegmentIndex);
			}

			// not first in a line-part:
			if (localSegmentIndex > 0 || fromGeometry is IPolygon)
			{
				if (localSegmentIndex > 0)
				{
					enumSegments.SetAt(partIndex, localSegmentIndex - 1);
				}
				else
				{
					// first in ring, set at last in ring
					int ringSegmentCount =
						((ISegmentCollection)
							((IGeometryCollection) fromGeometry).Geometry[partIndex]).SegmentCount -
						1;

					enumSegments.SetAt(partIndex, ringSegmentCount);
				}

				enumSegments.Previous(out previousSegment, ref outPartIndex, ref outSegmentIndex);
			}

			int? vertexToRemove = GetLocalVertexIndexToRemove(localSegmentIndex, thisSegment,
			                                                  previousSegment, nextSegment,
			                                                  out alternativeVertexToRemove,
			                                                  out removeEntireSegment);

			Marshal.ReleaseComObject(thisSegment);

			if (nextSegment != null)
			{
				Marshal.ReleaseComObject(nextSegment);
			}

			if (previousSegment != null)
			{
				Marshal.ReleaseComObject(previousSegment);
			}

			return vertexToRemove;
		}

		private static int? GetLocalVertexIndexToRemove(int localSegmentIndex,
		                                                [NotNull] ISegment thisSegment,
		                                                [CanBeNull] ISegment previousSegment,
		                                                [CanBeNull] ISegment nextSegment,
		                                                [CanBeNull] out int?
			                                                alternativeVertexToRemove,
		                                                out bool removeEntireSegment)
		{
			int? vertexToRemove = null;
			alternativeVertexToRemove = null;

			removeEntireSegment = thisSegment.GeometryType != esriGeometryType.esriGeometryLine;

			if (previousSegment == null)
			{
				// keep the from-point, just remove the to-point
				if (IsNonLinear(nextSegment))
				{
					// unless the next segment is non-linear: remove this entire segment
					removeEntireSegment = true;
				}
				else
				{
					vertexToRemove = localSegmentIndex + 1;
				}
			}

			if (nextSegment == null)
			{
				// keep the to-point, just remove the from-point
				if (IsNonLinear(previousSegment))
				{
					// unless the previous segment is non-linear: remove this entire segment
					removeEntireSegment = true;
				}
				else
				{
					vertexToRemove = localSegmentIndex;
				}
			}

			if (! removeEntireSegment && previousSegment != null && nextSegment != null)
			{
				if (IsNonLinear(previousSegment) && ! IsNonLinear(nextSegment))
				{
					// remove to-point
					vertexToRemove = localSegmentIndex + 1;
				}
				else if (IsNonLinear(nextSegment) && ! IsNonLinear(previousSegment))
				{
					// remove from-point
					vertexToRemove = localSegmentIndex;
				}
				else if (IsNonLinear(previousSegment) && IsNonLinear(nextSegment))
				{
					// both neighbours are non-linear:
					removeEntireSegment = true;
				}
				else
				{
					vertexToRemove = GetLargerDeflectionAngleVertexIndex(
						localSegmentIndex, thisSegment, previousSegment, nextSegment);

					if (vertexToRemove == localSegmentIndex)
					{
						alternativeVertexToRemove = vertexToRemove + 1;
					}
					else
					{
						alternativeVertexToRemove = vertexToRemove - 1;
					}
				}
			}

			return vertexToRemove;
		}

		private static bool IsNonLinear(ISegment segment)
		{
			return segment != null &&
			       segment.GeometryType != esriGeometryType.esriGeometryLine;
		}

		private static bool IsPointProtected([NotNull] IGeometry geometry,
		                                     int partIndex,
		                                     int pointIndex,
		                                     [NotNull] IPointCollection protectedPoints,
		                                     out IPoint pointToRemove)
		{
			int globalVertexIndex = GeometryUtils.GetGlobalIndex(geometry, partIndex,
			                                                     pointIndex);

			pointToRemove =
				((IPointCollection) geometry).Point[globalVertexIndex];

			return GeometryUtils.Contains((IGeometry) protectedPoints, pointToRemove);
		}

		private static bool HasProtectedEndpoint(IPolycurve polycurve, int partIndex,
		                                         int localSegmentIndex,
		                                         IPointCollection protectedPoints)
		{
			var partSegments =
				(ISegmentCollection) ((IGeometryCollection) polycurve).Geometry[partIndex];

			ISegment segment = partSegments.Segment[localSegmentIndex];

			return GeometryUtils.Contains((IGeometry) protectedPoints, segment.FromPoint) ||
			       GeometryUtils.Contains((IGeometry) protectedPoints, segment.ToPoint);
		}

		private static int GetPointIndexToKeep(int absoluteSegmentIndex,
		                                       ISegmentCollection fromGeometry,
		                                       out ISegment previousSegment,
		                                       out ISegment nextSegment)
		{
			ISegment thisSegment = fromGeometry.Segment[absoluteSegmentIndex];

			previousSegment = null;
			if (absoluteSegmentIndex > 0)
			{
				previousSegment = fromGeometry.Segment[absoluteSegmentIndex - 1];
			}

			nextSegment = null;
			if (absoluteSegmentIndex < fromGeometry.SegmentCount - 1)
			{
				nextSegment = fromGeometry.Segment[absoluteSegmentIndex + 1];
			}

			// TODO: consider closed ring
			if (previousSegment == null)
			{
				return absoluteSegmentIndex;
			}

			if (nextSegment == null)
			{
				return absoluteSegmentIndex + 1;
			}

			if (! GeometryUtils.AreEqual(previousSegment.ToPoint, thisSegment.FromPoint))
			{
				// first segment in part
				return absoluteSegmentIndex;
			}

			if (! GeometryUtils.AreEqualInXY(thisSegment.ToPoint, nextSegment.FromPoint))
			{
				// last segment in part
				return absoluteSegmentIndex + 1;
			}

			return GetSmallerDeflectionAngleVertexIndex(absoluteSegmentIndex, thisSegment,
			                                            previousSegment, nextSegment);
		}

		private static int GetLargerDeflectionAngleVertexIndex(int segIndex,
		                                                       ISegment segment,
		                                                       ISegment previousSegment,
		                                                       ISegment nextSegment)
		{
			int removePointIndex;

			const double maxDeflection = 180;

			double fromPointDeflectionAngle =
				GeometryUtils.GetAngle(previousSegment, segment) - maxDeflection;

			double toPointDeflectionAngle =
				GeometryUtils.GetAngle(segment, nextSegment) - maxDeflection;

			if (Math.Abs(fromPointDeflectionAngle) > Math.Abs(toPointDeflectionAngle))
			{
				// the segment start point
				removePointIndex = segIndex;
			}
			else
			{
				// the segment end point
				removePointIndex = segIndex + 1;
			}

			return removePointIndex;
		}

		private static int GetSmallerDeflectionAngleVertexIndex(int segIndex,
		                                                        ISegment segment,
		                                                        ISegment previousSegment,
		                                                        ISegment nextSegment)
		{
			int removePointIndex;

			const double maxDeflection = 180;

			double fromPointDeflectionAngle =
				GeometryUtils.GetAngle(previousSegment, segment) - maxDeflection;

			double toPointDeflectionAngle =
				GeometryUtils.GetAngle(segment, nextSegment) - maxDeflection;

			if (Math.Abs(fromPointDeflectionAngle) < Math.Abs(toPointDeflectionAngle))
			{
				removePointIndex = segIndex;
			}
			else
			{
				removePointIndex = segIndex + 1;
			}

			return removePointIndex;
		}

		private static bool IsStandaloneShortSegment(int currentSegmentIndex,
		                                             IList<esriSegmentInfo>
			                                             allShortSegmentInfos)
		{
			esriSegmentInfo currentSegmentInfo = allShortSegmentInfos[currentSegmentIndex];

			// check previous
			if (currentSegmentIndex - 1 >= 0)
			{
				esriSegmentInfo previousSegmentInfo =
					allShortSegmentInfos[currentSegmentIndex - 1];

				if (previousSegmentInfo.iAbsSegment == currentSegmentInfo.iAbsSegment - 1 &&
				    previousSegmentInfo.iPart == currentSegmentInfo.iPart)
				{
					return false;
				}
			}

			// check next
			if (currentSegmentIndex + 1 < allShortSegmentInfos.Count)
			{
				esriSegmentInfo nextSegmentInfo =
					allShortSegmentInfos[currentSegmentIndex + 1];

				if (nextSegmentInfo.iAbsSegment == currentSegmentInfo.iAbsSegment + 1 &&
				    nextSegmentInfo.iPart == currentSegmentInfo.iPart)
				{
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Replace segments with prolongation of gap-adjacent segments

		/// <summary>
		/// Currently not in use. Could be useful later in GeometryUtils.CreateOffsetBuffer
		/// </summary>
		/// <param name="polygon"></param>
		/// <param name="ringIndex"></param>
		/// <param name="segmentsToRemove"></param>
		public static void ReplaceSegmentsWithProlongedAdjacentSegments(
			[NotNull] IPolygon polygon, int ringIndex, List<ISegment> segmentsToRemove)
		{
			var ring = (IRing) ((IGeometryCollection) polygon).Geometry[ringIndex];
			// assemble consecutive segments to identify the gaps to be created and filled with single segment:
			IEnumerable<IList<ISegment>> gaps = IdentifyGaps(segmentsToRemove, ring);

			foreach (IList<ISegment> list in gaps)
			{
				var gap = (List<ISegment>) list;
				// TODO: don't remove entire segments that are too close to boundary - part of it might be useful here to build up new geometry

				IList<int> gapIndexes = GetSegmentIndexes(gap, (ISegmentCollection) ring);

				// prolongation approach: prolong the segments adjacent to the gap until they intersesct.
				//						  fallback if this does not work / they don't intersect: let it close by simplify
				// TODO: improve (performance!?) -> use different logic to find right extension side
				const esriCurveExtension extensionTypeAtFrom =
					esriCurveExtension.esriRelocateEnds |
					esriCurveExtension.esriNoExtendAtTo;

				const esriCurveExtension extensionTypeAtTo = esriCurveExtension.esriRelocateEnds |
				                                             esriCurveExtension.esriNoExtendAtFrom;

				IPolyline nextExtended, previousExtended;

				int lastExistingIndex = gapIndexes[0] == 0
					                        ? ((ISegmentCollection) ring).SegmentCount - 1
					                        : gapIndexes[0] - 1;

				ISegment lastExistingSegment =
					((ISegmentCollection) ring).Segment[lastExistingIndex];

				int firstUnremovedIndex = gapIndexes[gapIndexes.Count - 1] ==
				                          ((ISegmentCollection) ring).SegmentCount - 1
					                          ? 0
					                          : gapIndexes[gapIndexes.Count - 1] + 1;

				ISegment firstUnremovedSegment =
					((ISegmentCollection) ring).Segment[firstUnremovedIndex];

				// TODO: support non-linear segments
				bool isExtensionPerfomedAtFrom = TryCreateExtendedLineToStraight(
					lastExistingSegment, CreateInfinitelyExtendedSegment(
						(ILine) firstUnremovedSegment, esriSegmentExtension.esriExtendTangents),
					extensionTypeAtTo, out previousExtended);

				bool isExtensionPerfomedAtTo = TryCreateExtendedLineToStraight(
					firstUnremovedSegment,
					CreateInfinitelyExtendedSegment((ILine) lastExistingSegment,
					                                esriSegmentExtension.esriExtendTangents),
					extensionTypeAtFrom, out nextExtended);

				GeometryUtils.RemoveSegments((ISegmentCollection) ring, gapIndexes, false);

				if (isExtensionPerfomedAtFrom)
				{
					((ISegmentCollection) ring).AddSegmentCollection(
						(ISegmentCollection) previousExtended);
				}

				if (isExtensionPerfomedAtTo)
				{
					((ISegmentCollection) ring).AddSegmentCollection(
						(ISegmentCollection) nextExtended);
				}

				// simplify the geometry after each gap closure otherwise it can become empty
				GeometryUtils.Simplify(polygon);

				//Never compare segments/rings by reference after a simplify. All instances have changed.
				ring = (IRing) ((IGeometryCollection) polygon).Geometry[ringIndex];
			}
		}

		private static ILine CreateInfinitelyExtendedSegment(ILine inputLine,
		                                                     esriSegmentExtension
			                                                     segmentExtension)
		{
			IConstructLine constructLine = new LineClass();

			((IGeometry) constructLine).SpatialReference =
				inputLine.SpatialReference;

			constructLine.ConstructExtended(inputLine, segmentExtension);

			return (ILine) constructLine;
		}

		private static bool TryCreateExtendedLineToStraight(
			ISegment inputLine,
			ILine targetStraight,
			esriCurveExtension curveExtension,
			out IPolyline constructedCurve)
		{
			IPolyline lineToExtend = GeometryFactory.CreatePolyline(inputLine);

			var highLevelTarget =
				(ICurve) GeometryUtils.GetHighLevelGeometry(targetStraight);

			bool couldExtend = TryCreateExtendedCurve(lineToExtend, highLevelTarget,
			                                          curveExtension,
			                                          out constructedCurve);

			bool extended = constructedCurve.Length > inputLine.Length;

			return couldExtend && extended;
		}

		private static bool TryCreateExtendedCurve(IPolyline originalCurve,
		                                           ICurve highLevelTarget,
		                                           esriCurveExtension extensionType,
		                                           out IPolyline constructedCurve)
		{
			constructedCurve = new PolylineClass();

			((IGeometry) constructedCurve).SpatialReference =
				highLevelTarget.SpatialReference;

			return GeometryUtils.TryGetExtendedPolyline(originalCurve, highLevelTarget,
			                                            extensionType, constructedCurve);
		}

		private static IEnumerable<IList<ISegment>> IdentifyGaps(
			IEnumerable<ISegment> segmentsToRemove, IRing fromRing)
		{
			var gaps = new List<IList<ISegment>>();

			List<int> segmentIndexesToRemove = GetSegmentIndexes(segmentsToRemove,
			                                                     (ISegmentCollection) fromRing);

			segmentIndexesToRemove.Sort();

			int segmentCount = ((ISegmentCollection) fromRing).SegmentCount;
			int stopSegment = segmentCount - 1;
			int currentStartSegment = -1;

			if (segmentIndexesToRemove[0] == 0)
			{
				// there is a gap across the start/end-point:
				var startGap = new List<int>();

				AddConsecutiveSegments(startGap, segmentIndexesToRemove,
				                       segmentIndexesToRemove.Count - 1, segmentCount - 1, true);

				// get the lowest already used segment
				startGap.Sort();
				if (startGap.Count > 0)
				{
					stopSegment = startGap[startGap.Count - 1];
				}

				AddConsecutiveSegments(startGap, segmentIndexesToRemove, 0, 0, false);

				Assert.False(startGap.Count == segmentCount,
				             "All segments cannot be replaced with direct connection");

				gaps.Add(GetSegments(startGap, (ISegmentCollection) fromRing));
				currentStartSegment = startGap[startGap.Count - 1] + 1;
			}

			for (var listIndex = 0; listIndex < segmentIndexesToRemove.Count; listIndex++)
			{
				int segmentIndex = segmentIndexesToRemove[listIndex];

				if (segmentIndex < currentStartSegment || segmentIndex >= stopSegment) { }

				var nextGap = new List<int>();

				AddConsecutiveSegments(nextGap, segmentIndexesToRemove, listIndex, segmentIndex,
				                       false);
				gaps.Add(GetSegments(nextGap, (ISegmentCollection) fromRing));
				currentStartSegment = nextGap[nextGap.Count - 1] + 1;
			}

			return gaps;
		}

		private static int? GetSegmentIndex(ISegment segment,
		                                    ISegmentCollection inSegmentCollection,
		                                    double compareTolerance)
		{
			int? localIndex =
				GeometryUtils.FindHitSegmentIndex((IGeometry) inSegmentCollection,
				                                  segment.FromPoint, compareTolerance, out int _);

			Assert.NotNull(localIndex, "Segment not found any more.");

			ISegment foundSegment = inSegmentCollection.Segment[(int) localIndex];

			if (GeometryUtils.AreEqualInXY(foundSegment.ToPoint, segment.ToPoint))
			{
				return localIndex;
			}

			// could be a boundary loop: use slow but reliable comparison:
			IGeometry highLevelSegment = GeometryUtils.GetHighLevelGeometry(segment, true);
			for (var i = 0; i < inSegmentCollection.SegmentCount; i++)
			{
				ISegment candidate = inSegmentCollection.Segment[i];
				IGeometry highLevelCandidate = GeometryUtils.GetHighLevelGeometry(candidate, true);

				if (GeometryUtils.AreEqualInXY(highLevelSegment, highLevelCandidate))
				{
					return i;
				}
			}

			return null;
		}

		private static List<int> GetSegmentIndexes([NotNull] IEnumerable<ISegment> segments,
		                                           ISegmentCollection inSegmentCollection)
		{
			if (segments == null)
			{
				throw new ArgumentNullException(nameof(segments));
			}

			// NOTE: comparing using IRelationalOperator because
			// - simplify exchanges the instances - don't compare references
			// - remove segments changes the indexes - don't compare by index
			var result = new List<int>();

			double xyTolerance = GeometryUtils.GetXyTolerance((IGeometry) inSegmentCollection);

			foreach (ISegment segment in segments)
			{
				int? segmentIndex = GetSegmentIndex(segment, inSegmentCollection, xyTolerance);

				Assert.NotNull(segmentIndex, "Segment not found");

				result.Add((int) segmentIndex);
			}

			return result;
		}

		private static IList<ISegment> GetSegments(IList<int> segmentIndexes,
		                                           ISegmentCollection inSegmentCollection)
		{
			IList<ISegment> result = new List<ISegment>(segmentIndexes.Count);

			foreach (int segmentIndex in segmentIndexes)
			{
				result.Add(inSegmentCollection.Segment[segmentIndex]);
			}

			return result;
		}

		private static void AddConsecutiveSegments(List<int> toList, List<int> fromList,
		                                           int startAtListIndex,
		                                           int startSegmentIndex, bool descending)
		{
			int currentListIndex = startAtListIndex;
			int currentSegmentIndex = startSegmentIndex;

			while (currentListIndex >= 0 && currentListIndex < fromList.Count &&
			       fromList[currentListIndex] == currentSegmentIndex)
			{
				toList.Add(currentSegmentIndex);

				if (descending)
				{
					currentListIndex--;
					currentSegmentIndex--;
				}
				else
				{
					currentListIndex++;
					currentSegmentIndex++;
				}
			}
		}

		#endregion

		private static void AssertZProperties([NotNull] IGeometry geometry,
		                                      [NotNull] IGeometry original)
		{
			// To find issues such as the one resulting in TOP-4646 more efficiently
			if (GeometryUtils.IsZAware(original) && ((IZAware) original).ZSimple)
			{
				if (! GeometryUtils.IsZAware(geometry))
				{
					Assert.Fail("GetSubcurve: The result is not Z-aware.");
				}

				if (! ((IZAware) geometry).ZSimple)
				{
					var originalPoints = original as IPointCollection;

					if (originalPoints != null)
					{
						// Test all the original points for Z awareness.
						// The individual points of a Z-aware path can be non-z-aware. This happens when adding the path using AddSegmentCollection
						// (which only references the input path) to a non-z-aware poly -> all vertices are made non-Z-aware, even though the
						// path remains Z-aware.
						foreach (IPoint point in GeometryUtils.GetPoints(originalPoints))
						{
							if (! GeometryUtils.IsZAware(point))
							{
								// Most of the time even this situation result in valid, Z-simple result geometries
								// However, with single-segment paths this is not the case.
								Assert.Fail(
									"GetSubcurve: The original geometry is Z-simple, the test geometry is not. Not all points of the original geometry are Z-aware.");
							}
						}
					}

					Assert.Fail("The original geometry is Z-simple, the test geometry is not.");
				}
			}
		}

		private static void AssertMProperties([NotNull] IGeometry geometry,
		                                      [NotNull] IGeometry original)
		{
			if (GeometryUtils.IsMAware(original) && ((IMAware) original).MSimple)
			{
				if (! GeometryUtils.IsMAware(geometry))
				{
					Assert.Fail("GetSubcurve: The result is not M-aware.");
				}

				if (! ((IMAware) geometry).MSimple)
				{
					Assert.Fail(
						"GetSubcurve: The original geometry is M-simple, the test geometry is not.");
				}
			}
		}
	}
}

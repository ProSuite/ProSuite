using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	[CLSCompliant(false)]
	public static class RemoveCutPointsService
	{
		#region Public methods

		/// <summary>
		/// Remove points where old target intersects old source, 
		/// and which are not part of original geometries
		/// </summary>
		/// <param name="originalSourceGeometry">The original source geometry.</param>
		/// <param name="newSourceGeometry">The new source geometry.</param>
		/// <param name="originalTargetGeometries">The original target geometries.</param>
		/// <param name="newTargetGeometries">The new target geometries.</param>
		[CLSCompliant(false)]
		public static void RemoveMiddlePoints(
			[NotNull] IGeometry originalSourceGeometry,
			[NotNull] IGeometry newSourceGeometry,
			[NotNull] IList<IGeometry> originalTargetGeometries,
			[NotNull] IList<IGeometry> newTargetGeometries)
		{
			var orgTopoOp = (ITopologicalOperator) originalSourceGeometry;

			for (var i = 0; i < originalTargetGeometries.Count; i++)
			{
				// TODO should probably use i instead of 0...
				// only used from old "Adjust" tool
				IGeometry originalTargetGeometry = originalTargetGeometries[0];
				IGeometry newTargetGeometry = newTargetGeometries[0];

				IGeometry intersection = orgTopoOp.Intersect(
					originalTargetGeometry, esriGeometryDimension.esriGeometry0Dimension);

				var oldIntersectPoints = (IPointCollection) intersection;

				IList<IPoint> removedPointsSource = GetPointsToRemove(
					newSourceGeometry, oldIntersectPoints,
					originalSourceGeometry);

				// remove added middle points from source and target
				RemovePoints(newSourceGeometry, removedPointsSource);
				RemovePoints(newTargetGeometry, removedPointsSource);
			}
		}

		/// <summary>
		/// Remove points where border intersects the new geometry but not the original
		/// </summary>
		/// <param name="originalGeometry">The original geometry.</param>
		/// <param name="newGeometry">The new geometry.</param>
		/// <param name="border">The border.</param>
		[CLSCompliant(false)]
		public static void RemoveBorderPoints([NotNull] IGeometry originalGeometry,
		                                      [NotNull] IGeometry newGeometry,
		                                      [NotNull] IGeometry border)
		{
			if (! (newGeometry is IPolygon || newGeometry is IPolyline))
			{
				return;
			}

			// Get points where border intersects new geometry
			IPointCollection bufferPoints = GeometryUtils.GetIntersectPoints(
				(ITopologicalOperator) newGeometry, border);

			IList<IPoint> points = GetPointsToRemove(
				newGeometry, bufferPoints,
				originalGeometry);

			RemovePoints(newGeometry, points);
		}

		/// <summary>
		/// Removes points from a geometry while maintainging closed-ness of paths and rings.
		/// NOTE: This method introduces linear segments between the adjacent vertices of a removed point.
		/// </summary>
		/// <param name="geometry">High- or low-level geometry from which the points should be removed.</param>
		/// <param name="removedPoints">The points to remove.</param>
		[CLSCompliant(false)]
		public static void RemovePoints([NotNull] IGeometry geometry,
		                                [CanBeNull] IEnumerable<IPoint> removedPoints)
		{
			if (removedPoints == null)
			{
				return;
			}

			var multipatch = geometry as IMultiPatch;

			if (multipatch != null)
			{
				RemovePoints(multipatch, removedPoints);
				return;
			}

			double searchRadius = GeometryUtils.GetSearchRadius(geometry);

			// NOTE if wholeGeometry is ring / path: This is important to avoid extreme memory peaks if many points need to be removed:
			// - do not get hitTest within loop (creates high-level geometry)
			// - but do not hitTest on a (stale) copy (when creating high-level geometry only once)
			// -> create a high-level geometry that references the geometry part that is being edited

			IGeometry highLevelGeometry = GeometryUtils.GetHighLevelGeometry(geometry, true);

			GeometryUtils.AllowIndexing(highLevelGeometry);

			foreach (IPoint point in removedPoints)
			{
				RemovePoint(highLevelGeometry, point, searchRadius);
			}

			if (highLevelGeometry != geometry)
			{
				Marshal.ReleaseComObject(highLevelGeometry);
			}
		}

		[CLSCompliant(false)]
		public static void RemovePoints([NotNull] IMultiPatch multipatch,
		                                [NotNull] IEnumerable<IPoint> pointsToRemove)
		{
			double searchRadius = GeometryUtils.GetSearchRadius(multipatch);

			var geometryCollection = (IGeometryCollection) multipatch;

			foreach (IPoint point in pointsToRemove)
			{
				// NOTE: Never use hit test with multipatches! It's wrong.
				foreach (
					int partIndex in
					GeometryUtils.FindPartIndices(geometryCollection, point, searchRadius))
				{
					var ring = ((IGeometryCollection) multipatch).Geometry[partIndex] as IRing;

					Assert.NotNull(ring, "Multipatch has non-ring geometry");

					IGeometry highLevelRing = GeometryUtils.GetHighLevelGeometry(ring,
					                                                             dontClonePath:
					                                                             true);
					RemovePoint(highLevelRing, point, searchRadius);
				}
			}

			((IGeometryCollection) multipatch).GeometriesChanged();
		}

		/// <summary>
		/// Removes a point from a high-level geometry while maintaining the closed-ness of a closed paths / rings.
		/// </summary>
		/// <param name="highLevelGeometry"></param>
		/// <param name="partIndex">The part index from which the point shall be removed</param>
		/// <param name="vertexIndex">The part-local vertex index</param>
		public static void RemovePoint([NotNull] IGeometry highLevelGeometry,
		                               int partIndex,
		                               int vertexIndex)
		{
			Assert.ArgumentNotNull(highLevelGeometry, nameof(highLevelGeometry));

			var geometryCollection = highLevelGeometry as IGeometryCollection;

			Assert.NotNull(geometryCollection,
			               "The input geometry is not a high-level geometry");

			IGeometry partGeometry =
				((IGeometryCollection) highLevelGeometry).get_Geometry(partIndex);

			RemovePoint(highLevelGeometry, partIndex, vertexIndex, partGeometry);
		}

		private static void RemovePoint(IGeometry geometry, IPoint point, double searchRadius)
		{
			double distance = 0;
			var right = false;
			IPoint hitPoint = new PointClass();
			int hitSegmentIndex = -2;
			int hitPartIndex = -2;
			IHitTest hitTest = GeometryUtils.GetHitTest(geometry, true);

			bool found = hitTest.HitTest(point, searchRadius,
			                             esriGeometryHitPartType.esriGeometryPartVertex, hitPoint,
			                             ref distance, ref hitPartIndex,
			                             ref hitSegmentIndex, ref right);

			if (! found)
			{
				return;
			}

			IGeometry partGeometry = geometry is IGeometryCollection
				                         ? GeometryUtils.GetHitGeometryPart(
					                         point, geometry, searchRadius)
				                         : geometry;

			Assert.NotNull(partGeometry, "Geometry part at point {0} not found",
			               GeometryUtils.ToString(point));

			RemovePoint(geometry, hitPartIndex, hitSegmentIndex, partGeometry);
		}

		/// <summary>
		/// Removes a point from a high- or low-level geometry while maintaining the closed-ness of a closed path or ring.
		/// </summary>
		/// <param name="geometry">The high- or low-level geometry</param>
		/// <param name="partIndex">The part index (0 in case of a low-level geometry) from which the point shall be removed</param>
		/// <param name="vertexIndex">The part-local vertex index</param>
		/// <param name="partGeometry">The geometry part from which the point should be removed. In case of a low-level
		/// input geometry, the geometry itself.
		/// </param>
		private static void RemovePoint([NotNull] IGeometry geometry,
		                                int partIndex,
		                                int vertexIndex,
		                                [NotNull] IGeometry partGeometry)
		{
			var points = (IPointCollection) geometry;

			if (IsClosedPathStartOrEnd(vertexIndex, partGeometry, geometry.SpatialReference))
			{
				RemoveClosedPathNullPoint(geometry, partIndex, partGeometry);
			}
			else
			{
				int realIndex = GeometryUtils.GetGlobalIndex(geometry, partIndex,
				                                             vertexIndex);

				points.RemovePoints(realIndex, 1);
			}
		}

		/// <summary>
		/// Removes the start/end point of a closed path while maintaining the closedness.
		/// </summary>
		/// <param name="wholeGeometry"></param>
		/// <param name="hitPartIndex"></param>
		/// <param name="partGeometry"></param>
		private static void RemoveClosedPathNullPoint([NotNull] IGeometry wholeGeometry,
		                                              int hitPartIndex,
		                                              [NotNull] IGeometry partGeometry)
		{
			// remove 0 and max, add new max

			var points = (IPointCollection) wholeGeometry;

			int lastIndex = ((IPointCollection) partGeometry).PointCount - 1;

			int partEndRealIndex = GeometryUtils.GetGlobalIndex(wholeGeometry, hitPartIndex,
			                                                    lastIndex);
			points.RemovePoints(partEndRealIndex, 1);

			int partStartRealIndex = GeometryUtils.GetGlobalIndex(wholeGeometry, hitPartIndex,
			                                                      0);
			points.RemovePoints(partStartRealIndex, 1);

			IPoint newPoint =
				GeometryFactory.Clone(points.get_Point(partStartRealIndex));

			object refMissing = Type.Missing;

			((IPointCollection) partGeometry).AddPoint(
				newPoint, ref refMissing, ref refMissing);
		}

		/// <summary>
		/// Determines if the provided vertex is the start or end of a closed path
		/// </summary>
		/// <param name="vertexIndex">The vertex</param>
		/// <param name="lowLevelGeometry">The path</param>
		/// <param name="spatialReference">The spatial reference. Cannot use the SR of the lowLevelGeometry
		/// because that is sometimes (when edited in stereo) null.</param>
		/// <returns></returns>
		private static bool IsClosedPathStartOrEnd(
			int vertexIndex,
			[NotNull] IGeometry lowLevelGeometry,
			[NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(lowLevelGeometry, nameof(lowLevelGeometry));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			int pointCount = ((IPointCollection) lowLevelGeometry).PointCount;

			if (pointCount <= 2)
			{
				// RemoveClosedPathNullPoint() cannot deal with degenerate parts
				return false;
			}

			int lastIndex = pointCount - 1;

			var path = lowLevelGeometry as IPath;

			if (vertexIndex != 0 && vertexIndex != lastIndex)
			{
				return false;
			}

			if (path != null && path.IsClosed)
			{
				// NOTE: path.IsClosed returns an incorrect value if the part / geometry is very small (below tolerance)
				double xyTolerance = ((ISpatialReferenceTolerance) spatialReference).XYTolerance;

				if (path.Length < xyTolerance)
				{
					// TODO: test with non-z aware data, move to geometry utils
					double xyResolution = SpatialReferenceUtils.GetXyResolution(spatialReference);
					double zResolution = GeometryUtils.GetZResolution(spatialReference);

					WKSPointZ fromPoint = CreateWksPointZ(path.FromPoint);
					WKSPointZ toPoint = CreateWksPointZ(path.ToPoint);

					return GeometryUtils.IsSamePoint(fromPoint, toPoint, xyResolution, zResolution);
				}

				return true;
			}

			return false;
		}

		private static WKSPointZ CreateWksPointZ(IPoint point)
		{
			WKSPointZ wksPointZ;

			wksPointZ.X = point.X;
			wksPointZ.Y = point.Y;
			wksPointZ.Z = point.Z;

			return wksPointZ;
		}

		/// <summary>
		/// Selects the points from the testPoints that were inserted into the new
		/// geometry but do not contribute to the shape of the new geometry.
		/// </summary>
		/// <param name="newGeometry"></param>
		/// <param name="testPoints"></param>
		/// <param name="originalGeometry"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IList<IPoint> GetPointsToRemove(
			[NotNull] IGeometry newGeometry,
			[CanBeNull] IPointCollection testPoints,
			[NotNull] IGeometry originalGeometry)
		{
			if (testPoints == null || testPoints.PointCount == 0)
			{
				return null;
			}

			IList<IPoint> pointsToRemove = new List<IPoint>();

			IPoint testPoint = new PointClass();

			IHitTest orgHitTest = GeometryUtils.GetHitTest(originalGeometry, true);
			IHitTest newHitTest = GeometryUtils.GetHitTest(newGeometry, true);

			double searchRadius = GeometryUtils.GetSearchRadius(originalGeometry);

			ILine line = new LineClass();
			IPoint prevPoint = new PointClass();
			IPoint nextPoint = new PointClass();

			for (var i = 0; i < testPoints.PointCount; i++)
			{
				testPoints.QueryPoint(i, testPoint);

				int hitSegmentIndex;
				bool isValidBoundaryPoint = IsValidBoundaryPoint(testPoint,
				                                                 orgHitTest, searchRadius,
				                                                 newHitTest,
				                                                 out hitSegmentIndex);

				if (! isValidBoundaryPoint)
				{
					continue;
				}

				IGeometry geometryPart;
				if (newGeometry is IGeometryCollection)
				{
					geometryPart = Assert.NotNull(GeometryUtils.GetHitGeometryPart(
						                              testPoint, newGeometry, searchRadius));
				}
				else
				{
					geometryPart = newGeometry;
				}

				int lastIndex = ((IPointCollection) geometryPart).PointCount - 1;

				// If point is start or end point of a line, can not be removed
				bool pointValid = ! PointIsEndOrStart(newGeometry,
				                                      lastIndex, hitSegmentIndex);

				if (! pointValid)
				{
					continue;
				}

				// If removing point would change the the shape of the geometry, can not be removed
				bool pointAffectsShape = PointAffectsShape(testPoint,
				                                           searchRadius,
				                                           (IPointCollection) geometryPart,
				                                           prevPoint,
				                                           nextPoint, line, hitSegmentIndex);

				if (pointAffectsShape)
				{
					continue;
				}

				pointsToRemove.Add(GeometryFactory.Clone(testPoint));
			}

			return pointsToRemove;
		}

		#endregion

		#region Private methods

		private static bool PointIsEndOrStart(IGeometry newGeometry,
		                                      int lastIndex, int hitSegmentIndex)
		{
			return newGeometry is IPolyline &&
			       (hitSegmentIndex == 0 || hitSegmentIndex == lastIndex);
		}

		private static bool IsValidBoundaryPoint([NotNull] IPoint testPoint,
		                                         [NotNull] IHitTest orgHitTest,
		                                         double searchRadius,
		                                         [NotNull] IHitTest newHitTest,
		                                         out int hitSegmentIndex)
		{
			// init out parameters to invalid value
			int hitPartIndex = -2;
			hitSegmentIndex = 2;

			// If points exists on the original, can not be removed
			bool inOriginal = ExistsInOriginal(testPoint, orgHitTest, searchRadius);

			if (inOriginal)
			{
				return false;
			}

			// If point doesnt not exists on the new, can not be removed
			bool inNew = ExistsInNew(testPoint, searchRadius, newHitTest,
			                         ref hitPartIndex, ref hitSegmentIndex);

			if (! inNew)
			{
				return false;
			}

			return true;
		}

		private static bool ExistsInNew([NotNull] IPoint testPoint,
		                                double searchRadius, [NotNull] IHitTest newHitTest,
		                                ref int hitPartIndex,
		                                ref int hitSegmentIndex)
		{
			double distance = 0;
			var right = false;
			IPoint hitPoint = new PointClass();

			bool inNew = newHitTest.HitTest(testPoint, searchRadius,
			                                esriGeometryHitPartType.esriGeometryPartVertex,
			                                hitPoint,
			                                ref distance, ref hitPartIndex, ref hitSegmentIndex,
			                                ref right);
			return inNew;
		}

		private static bool PointAffectsShape([NotNull] IPoint testPoint,
		                                      double searchRadius,
		                                      [NotNull] IPointCollection newPoints,
		                                      [NotNull] IPoint prevPoint,
		                                      [NotNull] IPoint nextPoint,
		                                      [NotNull] ILine line,
		                                      int pointIndex)
		{
			GetPreviousPoint(newPoints, pointIndex, ref prevPoint);
			GetNextPoint(newPoints, pointIndex, ref nextPoint);
			line.PutCoords(prevPoint, nextPoint);

			double distance = 0;
			var right = false;
			IPoint hitPoint = new PointClass();

			var lineHitPartIndex = 0;
			var lineHitSegmentIndex = 0;

			IHitTest lineHitTest = GeometryUtils.GetHitTest(line, true);

			bool inLine = lineHitTest.HitTest(testPoint, searchRadius,
			                                  esriGeometryHitPartType.esriGeometryPartBoundary,
			                                  hitPoint, ref distance,
			                                  ref lineHitPartIndex,
			                                  ref lineHitSegmentIndex, ref right);
			return ! inLine;
		}

		/// <summary>
		/// Gets the next point.
		/// </summary>
		/// <param name="pointColl">The point coll.</param>
		/// <param name="currentIndex">Index of the current.</param>
		/// <param name="point">The point.</param>
		private static void GetNextPoint([NotNull] IPointCollection pointColl,
		                                 int currentIndex,
		                                 [NotNull] ref IPoint point)
		{
			int nextIndex;

			if (currentIndex == pointColl.PointCount - 1)
			{
				nextIndex = 0;
			}
			else
			{
				nextIndex = currentIndex + 1;
			}

			pointColl.QueryPoint(nextIndex, point);
		}

		/// <summary>
		/// Set previous index and point to refs
		/// </summary>
		/// <param name="pointColl">The point coll.</param>
		/// <param name="currentIndex">Index of the current.</param>
		/// <param name="point">The point.</param>
		private static void GetPreviousPoint([NotNull] IPointCollection pointColl,
		                                     int currentIndex,
		                                     [NotNull] ref IPoint point)
		{
			int prevIndex;
			if (currentIndex == 0)
			{
				prevIndex = pointColl.PointCount - 2;
			}
			else
			{
				prevIndex = currentIndex - 1;
			}

			pointColl.QueryPoint(prevIndex, point);
		}

		private static bool ExistsInOriginal([NotNull] IPoint testPoint,
		                                     [NotNull] IHitTest orgHitTest,
		                                     double searchRadius)
		{
			IPoint hitPoint = new PointClass();
			double distance = 0;
			var hitPartIndex = 0;
			var hitSegmentIndex = 0;
			var right = false;

			bool inOriginal = orgHitTest.HitTest(testPoint, searchRadius,
			                                     esriGeometryHitPartType.esriGeometryPartVertex,
			                                     hitPoint,
			                                     ref distance, ref hitPartIndex,
			                                     ref hitSegmentIndex,
			                                     ref right);
			return inOriginal;
		}

		#endregion
	}
}

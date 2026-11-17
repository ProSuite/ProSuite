using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Spatial;

public static class RemoveCutPointsService
{
	/// <summary>
	/// Selects the points from the testPoints that were inserted into the new
	/// geometry but do not contribute to the shape of the new geometry.
	/// </summary>
	/// <param name="newGeometry"></param>
	/// <param name="testPoints"></param>
	/// <param name="originalGeometry"></param>
	/// <returns></returns>
	[CanBeNull]
	public static IList<MapPoint> GetPointsToRemove([NotNull] Geometry newGeometry,
	                                                [CanBeNull] IList<MapPoint> testPoints,
	                                                [NotNull] Geometry originalGeometry)
	{
		if (testPoints == null || testPoints.Count == 0)
		{
			return null;
		}

		IList<MapPoint> pointsToRemove = new List<MapPoint>();

		double searchRadius = GeometryUtils.GetSearchRadius(originalGeometry);

		foreach (MapPoint testPoint in testPoints)
		{
			int? hitSegmentIndex;
			bool isValidBoundaryPoint = IsValidBoundaryPoint(testPoint,
			                                                 originalGeometry, //orgHitTest,
			                                                 searchRadius,
			                                                 newGeometry, //newHitTest,
			                                                 out hitSegmentIndex);

			if (!isValidBoundaryPoint)
			{
				continue;
			}

			Multipart geometryPart;
			if (((MultipartBuilderEx)newGeometry.ToBuilder()).PartCount > 1)
			{
				geometryPart = (Multipart) Assert.NotNull(GeometryUtils.GetHitGeometryPart(
					                                          testPoint, newGeometry, searchRadius));
			}
			else
			{
				geometryPart = (Multipart)newGeometry;
			}

			int lastIndex = geometryPart.PointCount - 1;

			// If point is start or end point of a line, can not be removed
			bool pointValid2 = ! (newGeometry.GeometryType == GeometryType.Polyline &&
			                      (hitSegmentIndex == 0 || hitSegmentIndex == lastIndex));
			bool pointValid = !PointIsEndOrStart(newGeometry, lastIndex, hitSegmentIndex);


			if (!pointValid)
			{
				continue;
			}

			// If removing point would change the the shape of the geometry, can not be removed
			bool pointAffectsShape = PointAffectsShape(testPoint,
			                                           searchRadius,
			                                           geometryPart.Points,
			                                           (int)hitSegmentIndex);

			if (pointAffectsShape)
			{
				continue;
			}

			pointsToRemove.Add(GeometryFactory.Clone(testPoint));
		}

		return pointsToRemove;
	}

	private static bool PointIsEndOrStart(Geometry newGeometry,
	                                      int lastIndex, int? hitSegmentIndex)
	{
		return newGeometry is Polyline &&
		       (hitSegmentIndex == 0 || hitSegmentIndex == lastIndex);
	}


	/// <summary>
	/// Removes points from a geometry while maintainging closed-ness of paths and rings.
	/// NOTE: This method introduces linear segments between the adjacent vertices of a removed point.
	/// </summary>
	/// <param name="geometry">High- or low-level geometry from which the points should be removed.</param>
	/// <param name="removedPoints">The points to remove.</param>
	public static void RemovePoints([NotNull] MultipartBuilderEx geometry, [CanBeNull] IList<MapPoint> removedPoints)
	{
		if (removedPoints == null)
		{
			return;
		}

		double searchRadius = GeometryUtils.GetSearchRadius(geometry.ToGeometry());

		foreach (MapPoint point in removedPoints)
		{
			RemovePoint(geometry, point, searchRadius);
		}
	}

	private static void RemovePoint(MultipartBuilderEx geometry, MapPoint point, double searchRadius)
	{
		var poly = geometry.ToGeometry();

		//HitTest
		ProximityResult proximity = GeometryEngine.Instance.NearestVertex(poly, point);
		if (proximity.Distance <= searchRadius)
		{
			int globalVertexIndex = proximity.PointIndex ??
			                       throw new InvalidOperationException(
				                       "NearestVertex() must return non-null PointIndex");
			var hitPartIndex = proximity.PartIndex;

			int partIndex = -1;
			int localVertexIndex = GeometryUtils.GetLocalVertexIndex((Geometry)poly, (int)globalVertexIndex, out partIndex);

			RemovePoint(geometry, hitPartIndex, (int)localVertexIndex);
		}
	}

	/// <summary>
	/// Removes a point from a high- or low-level geometry while maintaining the closed-ness of a closed path or ring.
	/// </summary>
	/// <param name="geometry">The high- or low-level geometry</param>
	/// <param name="partIndex">The part index (0 in case of a low-level geometry) from which the point shall be removed</param>
	/// <param name="vertexIndex">The part-local vertex index</param>
	/// input geometry, the geometry itself.
	/// </param>
	private static void RemovePoint([NotNull] MultipartBuilderEx geometry,
	                                int partIndex,
	                                int vertexIndex)
	{
		List<Segment> partSegments = geometry.Parts[partIndex];
		MultipartBuilderEx partGeometry = geometry.GeometryType == GeometryType.Polygon ? new PolygonBuilderEx() : new PolylineBuilderEx();

		partGeometry.AddSegments(partSegments);

		if (IsClosedPathStartOrEnd(vertexIndex, partGeometry.ToGeometry(), geometry.SpatialReference))
		{
			RemoveClosedPathNullPoint(geometry, partIndex, partGeometry);
			//ReplacePart
			geometry.Parts[partIndex] = partGeometry.Parts[0];
		}
		else
		{
			GeometryUtils.RemoveVertices(geometry, partIndex, vertexIndex);
		}
	}

	/// <summary>
	/// Removes the start/end point of a closed path while maintaining the closedness.
	/// </summary>
	/// <param name="wholeGeometry"></param>
	/// <param name="hitPartIndex"></param>
	/// <param name="partGeometry"></param>
	private static void RemoveClosedPathNullPoint([NotNull] MultipartBuilderEx wholeGeometry,
	                                              int hitPartIndex,
	                                              [NotNull] MultipartBuilderEx partGeometry)
	{
		// remove 0 and max, add new max

		//partGeometry has one part
		int lastIndex = partGeometry.GetPointCount(0) - 1;

		int partEndRealIndex = GeometryUtils.GetGlobalIndex(wholeGeometry, hitPartIndex,
		                                                      lastIndex);
		GeometryUtils.RemoveVertices(wholeGeometry, hitPartIndex, partEndRealIndex);

		int partStartRealIndex = GeometryUtils.GetGlobalIndex(wholeGeometry, hitPartIndex, 0);
		GeometryUtils.RemoveVertices(wholeGeometry, hitPartIndex, partStartRealIndex);

		MapPoint newPoint = wholeGeometry.GetPoint(hitPartIndex, partStartRealIndex);

		GeometryUtils.AddVertex(partGeometry, newPoint, 0);
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
		[NotNull] Geometry lowLevelGeometry,
		[NotNull] SpatialReference spatialReference)
	{
		Assert.ArgumentNotNull(lowLevelGeometry, nameof(lowLevelGeometry));
		Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

		int pointCount = lowLevelGeometry.PointCount;

		if (pointCount <= 2)
		{
			// RemoveClosedPathNullPoint() cannot deal with degenerate parts
			return false;
		}

		int lastIndex = pointCount - 1;

		if (vertexIndex != 0 && vertexIndex != lastIndex)
		{
			return false;
		}

		if (lowLevelGeometry.IsClosed())
		{
			// NOTE: path.IsClosed returns an incorrect value if the part / geometry is very small (below tolerance)
			double xyTolerance = spatialReference.XYTolerance;

			if (lowLevelGeometry.Length < xyTolerance)
			{
				// TODO: test with non-z aware data, move to geometry utils
				double xyResolution = spatialReference.XYResolution;
				//double zResolution = spatialReference.ZTolerance;

				MapPoint fromPoint = GeometryUtils.GetStartPoint((Multipart) lowLevelGeometry);
				MapPoint toPoint = GeometryUtils.GetEndPoint((Multipart) lowLevelGeometry);

				return GeometryUtils.IsSamePointXY(fromPoint, toPoint, xyResolution);
			}

			return true;
		}

		return false;
	}

	private static bool PointAffectsShape([NotNull] MapPoint testPoint,
	                                      double searchRadius,
	                                      [NotNull] ReadOnlyPointCollection newPoints,
	                                      int pointIndex)
	{

		MapPoint prevPoint = GetPreviousPoint(newPoints, pointIndex);
		MapPoint nextPoint = GetNextPoint(newPoints, pointIndex);

		var lineSegment = LineBuilderEx.CreateLineSegment(prevPoint, nextPoint, testPoint.SpatialReference);
		PolylineBuilderEx line = new PolylineBuilderEx(testPoint.SpatialReference);
		line.AddSegment(lineSegment);

		double distance = 0;

		bool found = false;

		//HitTest
		ProximityResult proximity = GeometryEngine.Instance.NearestPoint(line.ToGeometry(), testPoint);
		//Assert.True(proximity.Distance <= distance, "proximity.Distance <= searchTolerance");
		int? segmentIndex = proximity.SegmentIndex;
		int partIndex = proximity.PartIndex;
		if (proximity.Distance <= distance)
		{
			found = true;
		}
		else
		{
			Assert.True(proximity.Distance <= distance, "proximity.Distance <= searchTolerance");
		}

		return !found;
	}

	/// <summary>
	/// Gets the next point.
	/// </summary>
	/// <param name="pointColl">The point coll.</param>
	/// <param name="currentIndex">Index of the current.</param>
	private static MapPoint GetNextPoint([NotNull] ReadOnlyPointCollection pointColl,
	                                     int currentIndex)
	{
		int nextIndex;

		if (currentIndex == pointColl.Count - 1)
		{
			nextIndex = 0;
		}
		else
		{
			nextIndex = currentIndex + 1;
		}

		return pointColl[nextIndex];
	}

	/// <summary>
	/// Set previous index and point to refs
	/// </summary>
	/// <param name="pointColl">The point coll.</param>
	/// <param name="currentIndex">Index of the current.</param>
	private static MapPoint GetPreviousPoint([NotNull] ReadOnlyPointCollection pointColl,
	                                         int currentIndex)
	{
		int prevIndex;
		if (currentIndex == 0)
		{
			prevIndex = pointColl.Count - 2;
		}
		else
		{
			prevIndex = currentIndex - 1;
		}

		return pointColl[prevIndex];
	}

	private static bool IsValidBoundaryPoint([NotNull] MapPoint testPoint,
	                                         [NotNull] Geometry orgHitTest,
	                                         double searchRadius,
	                                         [NotNull] Geometry newHitTest,
	                                         out int? hitSegmentIndex)
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

		if (!inNew)
		{
			return false;
		}

		return true;
	}

	private static bool ExistsInNew([NotNull] MapPoint testPoint,
	                                double searchRadius, [NotNull] Geometry newHitTest,
	                                ref int hitPartIndex,
	                                ref int? hitSegmentIndex)
	{
		bool found = false;

		//HitTest
		ProximityResult proximity2 = GeometryEngine.Instance.NearestPoint(newHitTest, testPoint);
		ProximityResult proximity = GeometryEngine.Instance.NearestVertex(newHitTest, testPoint);
		//Assert.True(proximity.Distance <= searchRadius, "proximity.Distance <= searchRadius");
			
		hitPartIndex = proximity.PartIndex;
		int? vertexIndex = null;
		if (proximity.Distance <= searchRadius)
		{
			bool areEqualInXY = GeometryUtils.AreEqualInXY((Geometry)proximity.Point, (Geometry)proximity2.Point);
			if (areEqualInXY)
			{
				hitSegmentIndex = proximity2.SegmentIndex;
			}

			vertexIndex = proximity.PointIndex ??
			              throw new InvalidOperationException(
				              "NearestVertex() must return non-null PointIndex");
		}

		if (vertexIndex != null)
		{
			found = true;
		}
		else
		{
			//Assert.True(segmentIndex != null, "segmentIndex != null");
			//Assert.Fail("vertexIndex == null");
		}

		return found;
	}

	private static bool ExistsInOriginal([NotNull] MapPoint testPoint,
	                                     [NotNull] Geometry orgHitTest,
	                                     double searchRadius)
	{
		bool found = false;

		//HitTest
		//ProximityResult proximity = GeometryEngine.Instance.NearestPoint(orgHitTest, testPoint);
		ProximityResult proximity = GeometryEngine.Instance.NearestVertex(orgHitTest, testPoint);
		//Assert.True(proximity.Distance <= searchRadius, "proximity.Distance <= searchRadius");
		int? segmentIndex = proximity.SegmentIndex;
		int partIndex = proximity.PartIndex;
		int? vertexIndex = null;
		if (proximity.Distance <= searchRadius)
		{
			vertexIndex = proximity.PointIndex ??
			              throw new InvalidOperationException(
				              "NearestVertex() must return non-null PointIndex");
		}

		if (vertexIndex != null)
		{
			found = true;
		}
		else
		{
			//Assert.True(segmentIndex != null, "segmentIndex != null");
			//Assert.Fail("vertexIndex == null");
		}

		return found;
	}

}

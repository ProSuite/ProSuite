using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Spatial;

public static class IntersectionUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public static bool UseCustomIntersect { get; set; } =
		EnvironmentUtils.GetBooleanEnvironmentVariableValue(
			"PROSUITE_USE_CUSTOM_INTERSECT", false);

	/// <summary>
	/// Gets the intersection points between two geometries. For polycurves
	/// the start and end point of linear intersections are included.
	/// The Z values of the resulting points are taken from geometry1.
	/// </summary>
	/// <param name="geometry1"></param>
	/// <param name="geometry2"></param>
	/// <returns></returns>
	[NotNull]
	public static Multipoint GetIntersectionPoints(Geometry geometry1, Geometry geometry2)
	{
		bool equal = SpatialReference.AreEqual(geometry1.SpatialReference,
		                                       geometry2.SpatialReference, true,
		                                       false);
		if (! equal)
		{
			geometry1 = GeometryUtils.EnsureSpatialReference(geometry1, geometry2.SpatialReference);
		}

		const bool assumeIntersecting = false;
		var intersectionPoints = GetIntersectionPoints(geometry1, geometry2, assumeIntersecting);

		return intersectionPoints;
	}

	/// <summary>
	/// Gets the intersection points between two geometries and implements a 
	/// work-around for some situations where the standard mechanisms don't find
	/// certain intersections. 
	/// The Z values of the resulting points are taken from geometry1 except for
	/// the option IncludeLinearIntersectionAllPoints where the points from both
	/// geometries (if they have different Z values) are returned.
	/// </summary>
	/// <param name="geometry1"></param>
	/// <param name="geometry2"></param>
	/// <param name="assumeIntersecting"></param>
	/// <param name="intersectionPointOption"></param>
	/// <returns></returns>
	[NotNull]
	public static Multipoint GetIntersectionPoints([NotNull] Geometry geometry1,
	                                               [NotNull] Geometry geometry2,
	                                               bool assumeIntersecting,
	                                               IntersectionPointOptions
		                                               intersectionPointOption =
		                                               IntersectionPointOptions
			                                               .IncludeLinearIntersectionEndpoints)

	{
		if (! assumeIntersecting && GeometryUtils.Disjoint(geometry1, geometry2))
		{
			return MultipointBuilderEx.CreateMultipoint(geometry1.SpatialReference);
		}

		if (UseCustomIntersect &&
		    ! GeometryUtils.HasNonLinearSegments(geometry1) &&
		    ! GeometryUtils.HasNonLinearSegments(geometry2) &&
		    ! geometry1.HasM &&
		    intersectionPointOption ==
		    IntersectionPointOptions.IncludeLinearIntersectionEndpoints)
		{
			if (geometry1 is Multipoint multipointSource)
			{
				// TODO Reimplement GetIntersectionPointsXY
				//return GetIntersectionPointsXY(multipointSource, geometry2, xyTolerance, planar);
				return (Multipoint) GeometryEngine.Instance.Intersection(
					multipointSource, geometry2,
					GeometryDimensionType
						.EsriGeometry0Dimension);
			}

			if (geometry1 is Polyline polyline)
			{
				// TODO Reimplement GetIntersectionPointsXY
				//return GetIntersectionPointsXY(polyline, geometry2, xyTolerance, planar);
				return (Multipoint) GeometryEngine.Instance.Intersection(polyline, geometry2,
					GeometryDimensionType
						.EsriGeometry0Dimension);
			}

			// MultiPatch not supported
			if (geometry1 is Multipatch multipatchSource)
			{
				// Use footprint for consistency with AO implementation:
				Polygon sourceFootprint = GeometryFactory.CreatePolygon(multipatchSource, null);

				if (geometry2 is Multipatch multipatch2)
				{
					// Use footprint for consistency with AO implementation:
					geometry2 = GeometryFactory.CreatePolygon(multipatch2, null);
				}

				// TODO Reimplement GetIntersectionPointsXY
				//return GetIntersectionPointsXY(sourceFootprint, geometry2, xyTolerance);
				return (Multipoint) GeometryEngine.Instance.Intersection(sourceFootprint, geometry2,
					GeometryDimensionType
						.EsriGeometry0Dimension);
			}
		}

		// Point argument -> Point result
		if (geometry1.GeometryType == GeometryType.Point ||
		    geometry2.GeometryType == GeometryType.Point)
		{
			return (Multipoint) GeometryEngine.Instance.Intersection(geometry1, geometry2,
				GeometryDimensionType
					.EsriGeometry0Dimension);
		}

		// Multipoint argument -> needs work-around
		if (geometry1.GeometryType == GeometryType.Multipoint ||
		    geometry2.GeometryType == GeometryType.Multipoint)
		{
			var multipoint = (Multipoint) GeometryEngine.Instance.Intersection(geometry1, geometry2,
				GeometryDimensionType
					.EsriGeometry0Dimension);

			// WORK-AROUND
			if (multipoint.IsEmpty && geometry1 is Polyline || geometry2 is Polyline)
			{
				// check point-by-point
				Multipoint points;
				Polyline polycurve;
				if (geometry1.GeometryType == GeometryType.Multipoint)
				{
					points = (Multipoint) geometry1;
					polycurve = (Polyline) geometry2;
				}
				else
				{
					points = (Multipoint) geometry2;
					polycurve = (Polyline) geometry1;
				}

				return (Multipoint) GeometryEngine.Instance.Intersection(
					points, polycurve, GeometryDimensionType
						.EsriGeometry0Dimension);
			}

			return multipoint;
		}

		// Both arguments are polycurves -> redirect
		var polycurve1 = geometry1 as Multipart;
		var polycurve2 = geometry2 as Multipart;

		Assert.NotNull(polycurve1, "geometry1 is not of a supported geometry type");
		Assert.NotNull(polycurve2, "geometry2 is not of a supported geometry type");

		Multipoint intersectPoints =
			GetIntersectionPoints(polycurve1, polycurve2, true, intersectionPointOption, null);

		return intersectPoints;
	}

	/// <summary>
	/// Returns the intersection points between polycurve1 and polycurve2. The Z values from
	/// polycurve1 will be preserved in the result.
	/// </summary>
	/// <param name="polycurve1"></param>
	/// <param name="polycurve2"></param>
	/// <param name="assumeIntersecting"></param>
	/// <param name="intersectionPointOptions"></param>
	/// <param name="linearIntersectionsResult">The linear intersections in case their end points or all their vertices (depending on the intersectionPointOptions) are used.</param>
	/// <returns></returns>
	[NotNull]
	public static Multipoint GetIntersectionPoints(
		[NotNull] Multipart polycurve1,
		[NotNull] Multipart polycurve2,
		bool assumeIntersecting,
		IntersectionPointOptions intersectionPointOptions,
		[CanBeNull] PolylineBuilderEx linearIntersectionsResult)
	{
		if (! assumeIntersecting && GeometryUtils.Disjoint(polycurve1, polycurve2))
		{
			return MultipointBuilderEx.CreateMultipoint(polycurve1.SpatialReference);
		}

		if (UseCustomIntersect &&
		    intersectionPointOptions ==
		    IntersectionPointOptions.IncludeLinearIntersectionEndpoints &&
		    ! GeometryUtils.HasNonLinearSegments(polycurve1) &&
		    ! GeometryUtils.HasNonLinearSegments(polycurve2) &&
		    ! polycurve1.HasM)
		{
			//return GetIntersectionPointsXY(polycurve1, polycurve2,
			//							   GeometryUtils.GetXyTolerance(polycurve1),
			//							   linearIntersectionsResult);
			throw new NotImplementedException(
				"GetIntersectionPoints (UseCustomIntersect !HasNonLinearSegments) not yet implemented");
		}

		// NOTE: the resulting intersection point can have a larger distance to the actual intersection than the tolerance
		//		 if two lines intersect at a small angle and one has a vertex relatively close (but further than the tolerance)
		//		 to the actual intersection
		// NOTE 2: The result can be a (empty) polyline if there are no intersections
		Geometry intersectionGeometry =
			GeometryUtils.Intersection(polycurve1, polycurve2,
			                           GeometryDimensionType.EsriGeometry0Dimension);

		if (intersectionGeometry == null || intersectionGeometry.IsEmpty)
		{
			// Known problem: vertical line:
			if (IsVerticalWithoutXyExtent(polycurve1))
			{
				// Check individual points of the vertical geometry:
				intersectionGeometry =
					GetIntersectionPointsWithVerticalGeometry(polycurve1, polycurve2, false);
			}

			if (IsVerticalWithoutXyExtent(polycurve2))
			{
				intersectionGeometry =
					GetIntersectionPointsWithVerticalGeometry(polycurve2, polycurve1, false);
			}
		}

		Multipoint resultGeom =
			intersectionGeometry == null || intersectionGeometry.IsEmpty
				? new MultipointBuilderEx().Configure(polycurve1).ToGeometry()
				: (Multipoint) intersectionGeometry;

		MultipointBuilderEx result = new MultipointBuilderEx(resultGeom);

		if (intersectionPointOptions ==
		    IntersectionPointOptions.DisregardLinearIntersections)
		{
			return result.ToGeometry();
		}

		// Make sure we're not intersecting a line with a polygon. Always use the polygon outlines.
		Polyline polyline1;
		if (polycurve1.GeometryType != GeometryType.Polyline)
		{
			polyline1 = GeometryFactory.CreatePolyline(polycurve1);
		}
		else
		{
			polyline1 = (Polyline) polycurve1;
		}

		Polyline polyline2;
		if (polycurve2.GeometryType != GeometryType.Polyline)
		{
			polyline2 = GeometryFactory.CreatePolyline(polycurve2);
		}
		else
		{
			polyline2 = (Polyline) polycurve2;
		}

		// get shared lines, were not included in intersect points
		// NOTE: do not assume intersecting because the outlines do not necessarily intersect when the polygons do (e.g. contained poly)
		bool intersecting = polyline1 != polycurve1 || polyline2 != polycurve2
			                    ? GeometryUtils.Intersects(polyline1, polycurve2)
			                    : true;

		const bool allowRandomStartPointsForClosedIntersections = false;

		var linearIntersections = GetIntersectionLines(
			polyline1, polyline2, intersecting,
			allowRandomStartPointsForClosedIntersections);

		if (intersectionPointOptions ==
		    IntersectionPointOptions.IncludeLinearIntersectionEndpoints)
		{
			// add start and end point of shared lines to intersect points
			var lineParts = linearIntersections;
			int lineCount = lineParts.PartCount;

			for (var partIndex = 0; partIndex < lineCount; partIndex++)
			{
				var linePart = lineParts.Parts[partIndex];
				var line = PolylineBuilderEx.CreatePolyline(linePart);

				if (line == null)
				{
					continue;
				}

				result.AddPoint(GeometryUtils.GetEndPoint(line));
				result.AddPoint(GeometryUtils.GetStartPoint(line));
			}
		}
		else if (intersectionPointOptions ==
		         IntersectionPointOptions.IncludeLinearIntersectionAllPoints)
		{
			ReadOnlyPointCollection readOnlyPointCollection = linearIntersections.Points;
			result.AddPoints(readOnlyPointCollection);
		}

		Multipoint multipoint = GeometryUtils.Simplify(result.ToGeometry());
		result = new MultipointBuilderEx(multipoint);

		// WORKAROUND 
		// TODO: Analyze these situations - at 10.0 they are most likely the following problem:
		//       - Very small overshoot, the intersection point is found on the line but the end-point
		//         also 'intersects' with respect to tolerance
		//         -> Find a more general solution (e.g. additional parameter 'accurate')
		// Add from- and to-point if not already added
		var geometry2Line = polycurve2 as Polyline;
		if (geometry2Line != null &&
		    ! polycurve1.IsEmpty && ! polycurve1.IsClosed())
		{
			foreach (Geometry geometry in GeometryUtils.GetParts(polycurve1))
			{
				var path = (Polyline) geometry;

				MapPoint fromPoint = GeometryUtils.GetStartPoint(path);
				AddIntersectingPoint(fromPoint, geometry2Line, result);

				MapPoint toPoint = GeometryUtils.GetEndPoint(path);
				AddIntersectingPoint(toPoint, geometry2Line, result);
			}
		}
		// END WORKAROUND

		linearIntersectionsResult?.AddSegmentCollection(linearIntersections);

		return result.ToGeometry();
	}

	/// <summary>
	/// Returns the intersection lines between polycurve1 and polycurve2. 
	/// With allowRandomStartPointsForClosedIntersections == false, this method is  symmetric regarding XY,
	/// i.e. the two polycurve parameters can be swapped and the result remains equal in XY.
	/// The Z values from polycurve1 will be preserved in the result. 
	/// </summary>
	/// <param name="polycurve1"></param>
	/// <param name="polycurve2"></param>
	/// <param name="assumeIntersecting"></param>
	/// <param name="allowRandomStartPointsForClosedIntersections">When true is specified, the resulting line's 
	/// from/to points are not corrected when they are closed and the method performs better when many 
	/// intersections are closed lines (important for coincident polygons converted to lines).</param>
	/// <returns></returns>
	[NotNull]
	public static Polyline GetIntersectionLines(
		[NotNull] Multipart polycurve1,
		[NotNull] Multipart polycurve2,
		bool assumeIntersecting,
		bool allowRandomStartPointsForClosedIntersections)
	{
		if (! assumeIntersecting && GeometryUtils.Disjoint(polycurve1, polycurve2))
		{
			return PolylineBuilderEx.CreatePolyline(polycurve1.SpatialReference);
		}

		if (UseCustomIntersect &&
		    ! GeometryUtils.HasNonLinearSegments(polycurve1) &&
		    ! GeometryUtils.HasNonLinearSegments(polycurve2) &&
		    ! polycurve1.HasM &&
		    polycurve1 is Polyline && polycurve2 is Polyline)
		{
			//see region GeometryConversionUtils
			throw new NotImplementedException("UseCustomIntersect");
			//return GetIntersectionLinesXY(polycurve1, polycurve2,
			//							  GeometryUtils.GetXyTolerance(polycurve1));
		}

		Polyline linearIntersections =
			(Polyline) GeometryEngine.Instance.Intersection(polycurve1, polycurve2,
			                                                GeometryDimensionType
				                                                .EsriGeometry1Dimension);

		if (polycurve1 is Polygon || polycurve2 is Polygon ||
		    allowRandomStartPointsForClosedIntersections)
		{
			// TODO: research if the correction below makes also sense in
			//       some polygon cases
			return linearIntersections;
		}

		PolylineBuilderEx linearIntersectionsBuilder = linearIntersections.ToBuilder();
		FixClosedPathIntersectionPoints((Polyline) polycurve1,
		                                (Polyline) polycurve2,
		                                linearIntersectionsBuilder);

		return linearIntersectionsBuilder.ToGeometry();
	}

	private static void AddIntersectingPoint(MapPoint point,
	                                         Polyline intersectedGeometry,
	                                         MultipointBuilderEx toIntersectionPoints)
	{
		if (! GeometryUtils.Contains(toIntersectionPoints.ToGeometry(), point) &&
		    GeometryUtils.Intersects(point, intersectedGeometry))
		{
			_msg.DebugFormat(
				"TopologicalOperator.Intersects did not find intersection point {0}",
				point.ToString());

			toIntersectionPoints.AddPoint(point);
		}
	}

	/// <summary>
	/// Make sure the closed intersection paths between two polyline geometries are split at
	/// the original paths' from/to points rather than having any random start/end point.
	/// </summary>
	/// <param name="polyline1"></param>
	/// <param name="polyline2"></param>
	/// <param name="linearIntersections"></param>
	private static void FixClosedPathIntersectionPoints(
		[NotNull] Polyline polyline1,
		[NotNull] Polyline polyline2,
		[NotNull] PolylineBuilderEx linearIntersections)
	{
		for (var i = 0; i < linearIntersections.PartCount; i++)
		{
			var part = linearIntersections.Parts[i];

			Polyline path = PolylineBuilderEx.CreatePolyline(part);

			if (path.IsClosed())
			{
				// fully coincident rings: The result line should be split at the original rings' 
				// start/end points and not at a random location as it is done by ArcObjects.

				// find the corresponding path in both geometries

				// Increase tolerance by approximately sqrt(2) to avoid no-hits resulting in not null violation
				const double sqrt2Approx = 1.5;
				double searchTolerance =
					GeometryUtils.GetXyTolerance(polyline1) * sqrt2Approx;

				var partInCurve1 = (Polyline) GeometryUtils.GetCoincidentPath(
					polyline2, path, searchTolerance);

				var partInCurve2 = (Polyline) GeometryUtils.GetCoincidentPath(
					polyline1, path, searchTolerance);

				// add their start points
				var cutPoints = new List<MapPoint>();

				if (partInCurve1.IsClosed() &&
				    ! GeometryUtils.AreEqualInXY(GeometryUtils.GetStartPoint(partInCurve1),
				                                 GeometryUtils.GetStartPoint(path)))
				{
					cutPoints.Add(GeometryUtils.GetStartPoint(partInCurve1));
				}

				if (partInCurve2.IsClosed() &&
				    ! GeometryUtils.AreEqualInXY(GeometryUtils.GetStartPoint(partInCurve2),
				                                 GeometryUtils.GetStartPoint(path)))
				{
					cutPoints.Add(GeometryUtils.GetStartPoint(partInCurve2));
				}

				if (cutPoints.Count > 0)
				{
					const bool projectPointsOntoPathToSplit = false;
					double cutOffDistance = GeometryUtils.GetXyTolerance(path);
					Geometry splitPaths =
						GeometryUtils.SplitPath(path,
						                        GeometryFactory.CreateMultipoint(
							                        cutPoints),
						                        projectPointsOntoPathToSplit,
						                        cutOffDistance,
						                        splitCurves => true);

					GeometryUtils.ReplaceGeometryPart(
						linearIntersections, i, (Multipart) splitPaths);
				}
			}
		}
	}

	private static bool IsVerticalWithoutXyExtent(Geometry geometry)
	{
		if (! geometry.HasZ)
		{
			return false;
		}

		Envelope envelope = geometry.Extent; //.Envelope;

		return MathUtils.AreEqual(envelope.Width, 0) &&
		       MathUtils.AreEqual(envelope.Height, 0) &&
		       envelope.Depth > 0;
	}

	private static Multipoint GetIntersectionPointsWithVerticalGeometry(
		[NotNull] Geometry verticalGeometry,
		[NotNull] Geometry target,
		bool nonPlanar)
	{
		var resultList = new List<MapPoint>();
		foreach (MapPoint sourcePoint in GetPoints(verticalGeometry))
		{
			if (GeometryUtils.Disjoint(sourcePoint, target))
			{
				continue;
			}

			if (! nonPlanar)
			{
				// Planar: take the first intersection point, skip all others
				resultList.Add(sourcePoint);
				break;
			}

			if (IntersectsNonPlanar(sourcePoint, verticalGeometry))
			{
				resultList.Add(sourcePoint);
			}
		}

		return GeometryFactory.CreateMultipoint(resultList);
	}

	private static bool IntersectsNonPlanar([NotNull] MapPoint sourcePoint,
	                                        [NotNull] Geometry intersectedTarget)
	{
		if (sourcePoint.HasZ)
		{
			// 3D intersection if target is also Z-aware, otherwise 2D intersection
			if (intersectedTarget.HasZ)
			{
				return ! GeometryEngine.Instance.Disjoint3D(sourcePoint, intersectedTarget);
			}
			else
			{
				return true;
			}
		}

		// non-Z-aware source: Non-planar intersection probably not very useful (unless geometry is and should be non-simple)
		return true;
	}

	private static IEnumerable<MapPoint> GetPoints(Geometry geometry)
	{
		ReadOnlyPointCollection pointCollection = null;
		if (geometry is MapPoint mapPoint)
		{
			yield return mapPoint;
		}
		else if (geometry is Multipoint multipoint)
		{
			pointCollection = multipoint.Points;
		}
		else if (geometry is Polyline polyline)
		{
			pointCollection = polyline.Points;
		}
		else if (geometry is Polygon polygon)
		{
			pointCollection = polygon.Points;
		}
		else if (geometry is Multipart multipart)
		{
			pointCollection = multipart.Points;
		}

		if (pointCollection != null)
		{
			foreach (MapPoint point in pointCollection)
			{
				yield return point;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ExtractParts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry
{
	public static class IntersectionUtils
	{
		private static readonly esriGeometryDimension[] _intersectDimensions0 =
		{
			esriGeometryDimension.esriGeometry0Dimension
		};

		private static readonly esriGeometryDimension[] _intersectDimensions1 =
		{
			esriGeometryDimension.esriGeometry1Dimension,
			esriGeometryDimension.esriGeometry0Dimension
		};

		private static readonly esriGeometryDimension[] _intersectDimensions2 =
		{
			esriGeometryDimension.esriGeometry2Dimension,
			esriGeometryDimension.esriGeometry1Dimension,
			esriGeometryDimension.esriGeometry0Dimension
		};

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static bool UseCustomIntersect { get; set; } =
			EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				"PROSUITE_USE_CUSTOM_INTERSECT", true);

		[NotNull]
		public static IList<IGeometry> GetAllIntersectionList([NotNull] IGeometry g1,
		                                                      [NotNull] IGeometry g2)
		{
			return new List<IGeometry>(GetAllIntersections(g1, g2));
		}

		[NotNull]
		public static IEnumerable<IGeometry> GetAllIntersections([NotNull] IGeometry g1,
		                                                         [NotNull] IGeometry g2)
		{
			Assert.ArgumentNotNull(g1, nameof(g1));
			Assert.ArgumentNotNull(g2, nameof(g2));

			esriGeometryType g1Type = g1.GeometryType;
			esriGeometryType g2Type = g2.GeometryType;

			if (g1Type == esriGeometryType.esriGeometryMultiPatch)
			{
				g1 = GeometryFactory.CreatePolygon((IMultiPatch) g1);

				g1Type = esriGeometryType.esriGeometryPolygon;
			}

			if (g2Type == esriGeometryType.esriGeometryMultiPatch)
			{
				g2 = GeometryFactory.CreatePolygon((IMultiPatch) g2);

				g2Type = esriGeometryType.esriGeometryPolygon;
			}

			if (CanUseIntersectMultidimension(g1Type, g2Type))
			{
				GeometryUtils.AllowIndexing(g1);
				GeometryUtils.AllowIndexing(g2);

				IGeometry intersections =
					((ITopologicalOperator2) g1).IntersectMultidimension(g2);

				if (intersections.IsEmpty)
				{
					yield break;
				}

				var bag = intersections as IGeometryBag;
				if (bag != null)
				{
					var collection = (IGeometryCollection) bag;
					int geometryCount = collection.GeometryCount;
					for (var i = 0; i < geometryCount; i++)
					{
						IGeometry geometry = collection.Geometry[i];

						yield return geometry;
					}
				}
				else if (intersections is IMultipoint)
				{
					yield return intersections;
				}
				else
				{
					throw new InvalidOperationException(
						"Multipoint or geometry bag expected");
				}
			}
			else
			{
				if (g1Type == esriGeometryType.esriGeometryPoint &&
				    g2Type == esriGeometryType.esriGeometryPoint &&
				    ! GeometryUtils.Disjoint(g1, g2))
				{
					// intersection of point/point usually returns empty geometry
					// -> return first point if points are not disjoint
					// TODO verify
					yield return g1;
				}
				else
				{
					GeometryUtils.AllowIndexing(g1);

					foreach (esriGeometryDimension dimension in
					         GetIntersectDimensions(GetMaximumIntersectDimension(g1, g2)))
					{
						GeometryUtils.AllowIndexing(g2);
						IGeometry intersection = Intersect(g1, g2, dimension);

						if (! intersection.IsEmpty)
						{
							yield return intersection;
						}
					}
				}
			}
		}

		[NotNull]
		public static IGeometry GetIntersection([NotNull] IGeometry g1,
		                                        [NotNull] IGeometry g2)
		{
			GeometryUtils.AllowIndexing(g1);
			GeometryUtils.AllowIndexing(g2);

			return Intersect(g1, g2, GetMaximumIntersectDimension(g1, g2));
		}

		[NotNull]
		public static IGeometry GetLineCrossings([NotNull] IPolyline polyline1,
		                                         [NotNull] IPolyline polyline2,
		                                         bool reportEndPointTouchingInterior =
			                                         false)
		{
			Assert.ArgumentNotNull(polyline1, nameof(polyline1));
			Assert.ArgumentNotNull(polyline2, nameof(polyline2));

			// NOTE: this does not reliably find endpoint/interior intersections -> empty!! (even if Disjoint does return false)
			IGeometry intersection = Intersect(polyline1, polyline2,
			                                   esriGeometryDimension
				                                   .esriGeometry0Dimension);

			GeometryUtils.Simplify(intersection);

			if (intersection.IsEmpty)
			{
				return intersection;
			}

			if (GeometryUtils.GetPointCount(intersection) == 1)
			{
				// exactly one intersection; no need to try removing end points
				// (NOTE this assumes that the intersecting polylines are not only "touching", e.g. was searched using "crosses")
				return intersection;
			}

			// remove end points (if one end of line with two intersections crosses, the other is reported also)
			try
			{
				IGeometry innerIntersections;

				if (reportEndPointTouchingInterior)
				{
					// this fails if polylines are non-simple
					IGeometry shape1Boundary =
						((ITopologicalOperator) polyline1).Boundary;
					IGeometry shape2Boundary =
						((ITopologicalOperator) polyline2).Boundary;

					IGeometry touchingBoundary = Intersect(
						shape1Boundary, shape2Boundary,
						esriGeometryDimension.esriGeometry0Dimension);

					// get the reduced intersection
					try
					{
						innerIntersections = Difference(intersection, touchingBoundary);
					}
					finally
					{
						Marshal.ReleaseComObject(shape1Boundary);
						Marshal.ReleaseComObject(shape2Boundary);
					}
				}
				else
				{
					// this fails if polyline1 is non-simple
					IGeometry shape1Boundary =
						((ITopologicalOperator) polyline1).Boundary;

					// get the reduced intersection
					try
					{
						innerIntersections = Difference(intersection, shape1Boundary);
					}
					finally
					{
						Marshal.ReleaseComObject(shape1Boundary);
					}
				}

				GeometryUtils.Simplify(innerIntersections);

				// successfully reduced intersection, now release the unneeded full intersection
				try
				{
					Marshal.ReleaseComObject(intersection);
				}
				catch (Exception e)
				{
					// log and ignore
					_msg.DebugFormat(
						"Error releasing intersection geometry: {0}",
						e.Message);
				}

				return innerIntersections;
			}
			catch (Exception e)
			{
				// fall back to returning the full intersection
				_msg.DebugFormat(
					"Error subtracting end points from intersection: {0}",
					e.Message);

				return intersection;
			}
		}

		/// <summary>
		/// Gets the linear self intersections of a line
		/// </summary>
		/// <param name="polyline">The polyline.</param>
		/// <returns></returns>
		/// <remarks>Does not yet detect linear self intersections entirely within one half of the line.</remarks>
		[CanBeNull]
		public static IPolyline GetLinearSelfIntersection([NotNull] IPolyline polyline)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			if (polyline.IsEmpty)
			{
				return null;
			}

			var polycurve = (IPolycurve3) polyline;

			ICurve firstHalf;
			ICurve secondHalf;
			const bool asRatio = true;
			polycurve.GetSubcurve(0, 0.5, asRatio, out firstHalf);
			if (firstHalf == null || firstHalf.IsEmpty)
			{
				return null;
			}

			polycurve.GetSubcurve(0.5, 1, asRatio, out secondHalf);
			if (secondHalf == null || secondHalf.IsEmpty)
			{
				return null;
			}

			GeometryUtils.Simplify(firstHalf,
			                       allowReorder: false,
			                       allowPathSplitAtIntersections: false);
			GeometryUtils.Simplify(secondHalf,
			                       allowReorder: false,
			                       allowPathSplitAtIntersections: false);

			var intersection = Intersect(
					                   firstHalf, secondHalf,
					                   esriGeometryDimension.esriGeometry1Dimension) as
				                   IPolyline;

			return intersection == null || intersection.IsEmpty
				       ? null
				       : intersection;
		}

		/// <summary>
		/// Gets the intersection points between two geometries. For polycurves
		/// the start and end point of linear intersections are included.
		/// The Z values of the resulting points are taken from geometry1.
		/// </summary>
		/// <param name="geometry1"></param>
		/// <param name="geometry2"></param>
		/// <returns></returns>
		[NotNull]
		public static IMultipoint GetIntersectionPoints(
			[NotNull] IGeometry geometry1,
			[NotNull] IGeometry geometry2)
		{
			const bool assumeIntersecting = false;

			return GetIntersectionPoints(geometry1, geometry2, assumeIntersecting);
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
		public static IMultipoint GetIntersectionPoints(
			[NotNull] IGeometry geometry1,
			[NotNull] IGeometry geometry2,
			bool assumeIntersecting,
			IntersectionPointOptions intersectionPointOption =
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints)
		{
			if (! assumeIntersecting && GeometryUtils.Disjoint(geometry1, geometry2))
			{
				return new MultipointClass
				       { SpatialReference = geometry1.SpatialReference };
			}

			var highLevelGeometry1 =
				(ITopologicalOperator) GeometryUtils
					.GetHighLevelGeometry(geometry1, true);

			IGeometry highLevelGeometry2 =
				GeometryUtils.GetHighLevelGeometry(geometry2, true);

			GeometryUtils.AllowIndexing((IGeometry) highLevelGeometry1);

			double xyTolerance = GeometryUtils.GetXyTolerance(geometry1);

			if (UseCustomIntersect &&
			    ! GeometryUtils.HasNonLinearSegments(geometry1) &&
			    ! GeometryUtils.HasNonLinearSegments(geometry2) &&
			    ! GeometryUtils.IsMAware(geometry1) &&
			    intersectionPointOption ==
			    IntersectionPointOptions.IncludeLinearIntersectionEndpoints)
			{
				bool planar = ! GeometryUtils.IsZAware(geometry1);

				if (geometry1 is IMultipoint multipointSource)
				{
					return GetIntersectionPointsXY(multipointSource, geometry2, xyTolerance,
					                               planar);
				}

				if (geometry1 is IPolyline polyline)
				{
					return GetIntersectionPointsXY(polyline, geometry2, xyTolerance, planar);
				}

				if (geometry1 is IMultiPatch multipatchSource)
				{
					// Use footprint for consistency with AO implementation:
					IPolygon sourceFootprint = GeometryFactory.CreatePolygon(multipatchSource);

					if (geometry2 is IMultiPatch multipatch2)
					{
						// Use footprint for consistency with AO implementation:
						geometry2 = GeometryFactory.CreatePolygon(multipatch2);
					}

					return GetIntersectionPointsXY(sourceFootprint, geometry2, xyTolerance);
				}
			}

			// Point argument -> Point result
			if (geometry1.GeometryType == esriGeometryType.esriGeometryPoint ||
			    geometry2.GeometryType == esriGeometryType.esriGeometryPoint)
			{
				var point = (IPoint) Intersect(
					highLevelGeometry1, highLevelGeometry2,
					esriGeometryDimension.esriGeometry0Dimension);

				return GeometryFactory.CreateMultipoint(new List<IPoint> { point });
			}

			// Multipoint argument -> needs work-around
			if (geometry1.GeometryType == esriGeometryType.esriGeometryMultipoint ||
			    geometry2.GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				// NOTE: At 10.0 it was observed that the intersection between a multipoint and a polygon
				//		 was empty unless the multipoint contained only one point
				// TODO: repro case, if it ever happens again
				var multipoint = (IMultipoint) Intersect(
					highLevelGeometry1, highLevelGeometry2,
					esriGeometryDimension.esriGeometry0Dimension);

				// WORK-AROUND
				if (multipoint.IsEmpty &&
				    (geometry1 is IPolycurve || geometry2 is IPolycurve))
				{
					// TODO: Return correct Z values
					// check point-by-point
					IMultipoint points;
					IPolycurve polycurve;
					if (geometry1.GeometryType == esriGeometryType.esriGeometryMultipoint)
					{
						points = (IMultipoint) geometry1;
						polycurve = (IPolycurve) geometry2;
					}
					else
					{
						points = (IMultipoint) geometry2;
						polycurve = (IPolycurve) geometry1;
					}

					IEnumerable<IPoint> intersectionPoints = Intersect(
						points, polycurve, ((IPointCollection) multipoint).PointCount);

					IMultipoint resultMultipoint =
						GeometryFactory.CreateMultipoint(intersectionPoints);

					if (resultMultipoint.SpatialReference == null)
					{
						// in case there were 0 intersection points:
						resultMultipoint.SpatialReference = points.SpatialReference;
					}

					return resultMultipoint;
				}
				// END WORK-AROUND

				return multipoint;
			}

			// Multipatch argument -> special treatment
			if (geometry1.GeometryType == esriGeometryType.esriGeometryMultiPatch ||
			    geometry2.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				// NOTE: intersection results from multipatches have generally no Z (at 10.0)
				// TODO: restore Z from geometry 1
				IGeometry intersection = Intersect(
					highLevelGeometry1, highLevelGeometry2,
					esriGeometryDimension.esriGeometry0Dimension);

				// if the highLevelGeometry1 is a multipatch the intersection can be of type Polyline,
				// especially when empty
				var intersectionMultipoint = intersection as IMultipoint;
				if (intersectionMultipoint != null)
				{
					return intersectionMultipoint;
				}

				if (intersection.IsEmpty)
				{
					return new MultipointClass
					       { SpatialReference = geometry1.SpatialReference };
				}

				var polyline = (IPolyline) intersection;
				return GeometryFactory.CreateMultipoint(
					polyline.FromPoint, polyline.ToPoint);
			}

			// Both arguments are polycurves -> redirect
			var polycurve1 = highLevelGeometry1 as IPolycurve;
			var polycurve2 = highLevelGeometry2 as IPolycurve;

			Assert.NotNull(polycurve1, "geometry1 is not of a supported geometry type");
			Assert.NotNull(polycurve2, "geometry2 is not of a supported geometry type");

			IMultipoint intersectPoints =
				GetIntersectionPoints(polycurve1, polycurve2, true,
				                      intersectionPointOption);

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
		/// <returns></returns>
		[NotNull]
		public static IMultipoint GetIntersectionPoints(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IPolycurve polycurve2,
			bool assumeIntersecting,
			IntersectionPointOptions intersectionPointOptions)
		{
			return GetIntersectionPoints(polycurve1, polycurve2, assumeIntersecting,
			                             intersectionPointOptions, null);
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
		public static IMultipoint GetIntersectionPoints(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IPolycurve polycurve2,
			bool assumeIntersecting,
			IntersectionPointOptions intersectionPointOptions,
			[CanBeNull] IPolyline linearIntersectionsResult)
		{
			object emptyRef = Type.Missing;

			if (! assumeIntersecting && GeometryUtils.Disjoint(polycurve1, polycurve2))
			{
				return new MultipointClass
				       { SpatialReference = polycurve1.SpatialReference };
			}

			if (UseCustomIntersect &&
			    intersectionPointOptions ==
			    IntersectionPointOptions.IncludeLinearIntersectionEndpoints &&
			    ! GeometryUtils.HasNonLinearSegments(polycurve1) &&
			    ! GeometryUtils.HasNonLinearSegments(polycurve2) &&
			    ! GeometryUtils.IsMAware(polycurve1))
			{
				return GetIntersectionPointsXY(polycurve1, polycurve2,
				                               GeometryUtils.GetXyTolerance(polycurve1),
				                               linearIntersectionsResult);
			}

			// NOTE: the resulting intersection point can have a larger distance to the actual intersection than the tolerance
			//		 if two lines intersect at a small angle and one has a vertex relatively close (but further than the tolerance)
			//		 to the actual intersection
			// NOTE 2: The result can be a (empty) polyline if there are no intersections
			IGeometry intersectionGeometry = Intersect(
				polycurve1, polycurve2,
				esriGeometryDimension.esriGeometry0Dimension);

			if (intersectionGeometry.IsEmpty)
			{
				// Known problem: vertical line:
				if (IsVerticalWithoutXyExtent(polycurve1))
				{
					// Check individual points of the vertical geometry:
					intersectionGeometry =
						GetIntersectionPointsWithVerticalGeometry(
							(IPointCollection) polycurve1,
							polycurve2, false);
				}

				if (IsVerticalWithoutXyExtent(polycurve2))
				{
					intersectionGeometry =
						GetIntersectionPointsWithVerticalGeometry(
							(IPointCollection) polycurve2,
							polycurve1, false);
				}
			}

			IMultipoint result =
				intersectionGeometry.IsEmpty
					? GeometryFactory.CreateEmptyMultipoint(polycurve1)
					: (IMultipoint) intersectionGeometry;

			if (intersectionPointOptions ==
			    IntersectionPointOptions.DisregardLinearIntersections)
			{
				return result;
			}

			// Make sure we're not intersecting a line with a polygon. Always use the polygon outlines.
			IPolyline polyline1;
			if (polycurve1.GeometryType != esriGeometryType.esriGeometryPolyline)
			{
				polyline1 = GeometryFactory.CreatePolyline(polycurve1);
			}
			else
			{
				polyline1 = (IPolyline) polycurve1;
			}

			IPolyline polyline2;
			if (polycurve2.GeometryType != esriGeometryType.esriGeometryPolyline)
			{
				polyline2 = GeometryFactory.CreatePolyline(polycurve2);
			}
			else
			{
				polyline2 = (IPolyline) polycurve2;
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

			if (polyline1 != polycurve1)
			{
				Marshal.ReleaseComObject(polyline1);
			}

			if (polyline2 != polycurve2)
			{
				Marshal.ReleaseComObject(polyline2);
			}

			var points = (IPointCollection) result;

			if (intersectionPointOptions ==
			    IntersectionPointOptions.IncludeLinearIntersectionEndpoints)
			{
				// add start and end point of shared lines to intersect points
				var lineParts = (IGeometryCollection) linearIntersections;
				int lineCount = lineParts.GeometryCount;

				for (var partIndex = 0; partIndex < lineCount; partIndex++)
				{
					var line = lineParts.Geometry[partIndex] as IPath;

					if (line == null)
					{
						continue;
					}

					points.AddPoint(line.ToPoint, ref emptyRef, ref emptyRef);
					points.AddPoint(line.FromPoint, ref emptyRef, ref emptyRef);
				}
			}
			else if (intersectionPointOptions ==
			         IntersectionPointOptions.IncludeLinearIntersectionAllPoints)
			{
				var intersectionVertices =
					(IPointCollection)
					GeometryFactory.CreateMultipoint(
						(IPointCollection) linearIntersections);

				points.AddPointCollection(intersectionVertices);
			}

			GeometryUtils.Simplify(result);

			// WORKAROUND 
			// TODO: Analyze these situations - at 10.0 they are most likely the following problem:
			//       - Very small overshoot, the intersection point is found on the line but the end-point
			//         also 'intersects' with respect to tolerance
			//         -> Find a more general solution (e.g. additional parameter 'accurate')
			// Add from- and to-point if not already added
			var geometry2Line = polycurve2 as IPolyline;
			if (geometry2Line != null &&
			    ! polycurve1.IsEmpty && ! polycurve1.IsClosed)
			{
				foreach (IGeometry geometry in
				         GeometryUtils.GetParts((IGeometryCollection) polycurve1))
				{
					var path = (IPath) geometry;

					IPoint fromPoint = path.FromPoint;
					AddIntersectingPoint(fromPoint, geometry2Line, points);

					IPoint toPoint = path.ToPoint;
					AddIntersectingPoint(toPoint, geometry2Line, points);
				}
			}
			// END WORKAROUND

			((IGeometryCollection) linearIntersectionsResult)?.AddGeometryCollection(
				(IGeometryCollection) linearIntersections);

			return result;
		}

		public static IMultipoint GetIntersectionPointsNonPlanar(
			IPolycurve polycurve1,
			IPolycurve polycurve2)
		{
			// NOTE: In case of vertical polylines, the result is rather un-expected:
			// Sometimes all works fine (see unit test with constructed geometries)
			// Sometimes no 0-dimensional intersections are found. However, 1-dimensional intersections are found, but
			// they are strange: 3 parts, of which 2 are short paths that just exceed the tolerance on the horizontal(ish) segments
			// and 1 path that is equal to the vertical line, which is probably expected. We could probably also use that last
			// result part as basis of a work-around.

			if (IsVerticalWithoutXyExtent(polycurve1))
			{
				return GetIntersectionPointsWithVerticalGeometry(
					(IPointCollection) polycurve1,
					polycurve1, nonPlanar: true);
			}

			if (IsVerticalWithoutXyExtent(polycurve2))
			{
				return GetIntersectionPointsWithVerticalGeometry(
					(IPointCollection) polycurve2,
					polycurve1, nonPlanar: true);
			}

			const esriGeometryDimension dimension0 =
				esriGeometryDimension.esriGeometry0Dimension;

			IGeometry result = IntersectNonPlanar((ITopologicalOperator6) polycurve1,
			                                      polycurve2, dimension0);

			// The result can be an empty polyline!
			return result.IsEmpty
				       ? GeometryFactory.CreateEmptyMultipoint(polycurve1)
				       : (IMultipoint) result;
		}

		public static IMultipoint GetIntersectionPointsNonPlanar(
			[NotNull] IPointCollection sourcePoints,
			[NotNull] IGeometry targetGeometry)
		{
			var resultList = new List<IPoint>();

			foreach (IPoint sourcePoint in GeometryUtils.GetPoints(sourcePoints))
			{
				if (GeometryUtils.Disjoint(sourcePoint, targetGeometry))
				{
					continue;
				}

				if (IntersectsNonPlanar(sourcePoint, targetGeometry))
				{
					resultList.Add(sourcePoint);
				}
			}

			IMultipoint result = GeometryFactory.CreateMultipoint(resultList);

			return result;
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
		public static IPolyline GetIntersectionLines(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IPolycurve polycurve2,
			bool assumeIntersecting,
			bool allowRandomStartPointsForClosedIntersections)
		{
			if (! assumeIntersecting && GeometryUtils.Disjoint(polycurve1, polycurve2))
			{
				return new PolylineClass { SpatialReference = polycurve1.SpatialReference };
			}

			if (UseCustomIntersect &&
			    ! GeometryUtils.HasNonLinearSegments(polycurve1) &&
			    ! GeometryUtils.HasNonLinearSegments(polycurve2) &&
			    ! GeometryUtils.IsMAware(polycurve1) &&
			    polycurve1 is IPolyline && polycurve2 is IPolyline)
			{
				return GetIntersectionLinesXY(polycurve1, polycurve2,
				                              GeometryUtils.GetXyTolerance(polycurve1));
			}

			var topoOp1 = (ITopologicalOperator) polycurve1;

			var linearIntersections =
				(IPolyline) Intersect(topoOp1, polycurve2,
				                      esriGeometryDimension.esriGeometry1Dimension);

			if (polycurve1 is IPolygon || polycurve2 is IPolygon ||
			    allowRandomStartPointsForClosedIntersections)
			{
				// TODO: research if the correction below makes also sense in
				//       some polygon cases
				return linearIntersections;
			}

			FixClosedPathIntersectionPoints((IPolyline) polycurve1,
			                                (IPolyline) polycurve2,
			                                linearIntersections);

			return linearIntersections;
		}

		public static IMultipoint GetIntersectionPointsXY(
			[NotNull] IMultipoint multipoint1,
			[NotNull] IGeometry geometry2,
			double tolerance,
			bool planar = false)
		{
			// TODO: Use clone to improve performance
			IMultipoint result = GeometryFactory.CreateEmptyMultipoint(multipoint1);

			IEnvelope curve1Envelope = multipoint1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance (directly from GDB):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(
					curve1Envelope.XMax, curve1Envelope.YMax);

			var pntList = GeometryConversionUtils.CreateMultipoint(multipoint1);

			IEnumerable<IntersectionPoint3D> intersectionPoints;
			if (geometry2 is IPolycurve polycurve2)
			{
				// Note: Getting the paths from the GeometryCollection takes a large percentage of the entire method
				MultiPolycurve otherLinestrings =
					GeometryConversionUtils.CreateMultiPolycurve(
						polycurve2, tolerance, curve1Envelope);

				bool includeRingInteriorPoints = polycurve2 is IPolygon;

				intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
					pntList, otherLinestrings, tolerance, includeRingInteriorPoints);
			}
			else if (geometry2 is IMultiPatch multipatch2)
			{
				var intersectionPointList = new List<IntersectionPoint3D>();
				foreach (RingGroup ringGroup in GeometryConversionUtils.CreateRingGroups(
					         multipatch2))
				{
					intersectionPointList.AddRange(
						GeomTopoOpUtils.GetIntersectionPoints(
							pntList, ringGroup, tolerance, true));
				}

				intersectionPoints = intersectionPointList;
			}
			else if (geometry2 is IMultipoint multipoint2)
			{
				Multipoint<IPnt> otherPoints =
					GeometryConversionUtils.CreateMultipoint(multipoint2);

				intersectionPoints = GeomTopoOpUtils.GetIntersectionPoints(
					pntList, otherPoints, tolerance);
			}
			else if (geometry2 is IPoint point)
			{
				IPnt targetPoint =
					GeometryConversionUtils.CreatePnt(point, GeometryUtils.IsZAware(point));

				intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(pntList, targetPoint, tolerance);
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					$"Unsupported geometry type: {geometry2.GeometryType}");
			}

			Multipoint<IPnt> resultMultipnt =
				new Multipoint<IPnt>(intersectionPoints.Select(ip => ip.Point));

			double zTolerance = planar ? double.NaN : tolerance;

			GeomTopoOpUtils.Simplify(resultMultipnt, tolerance, zTolerance);

			GeometryConversionUtils.AddPoints(resultMultipnt.GetPoints(), result);

			return result;
		}

		public static IMultipoint GetIntersectionPointsXY(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IGeometry geometry2,
			double tolerance,
			bool planar = false)
		{
			if (geometry2 is IPolycurve polycurve2)
			{
				return GetIntersectionPointsXY(polycurve1, polycurve2, tolerance, null, planar);
			}

			if (geometry2 is IMultipoint multipoint2)
			{
				return GetIntersectionPointsXY(polycurve1, multipoint2, tolerance);
			}

			if (geometry2 is IPoint point)
			{
				// TODO: Proper implementation
				return GetIntersectionPointsXY(polycurve1, GeometryFactory.CreateMultipoint(point),
				                               tolerance, planar);
			}

			if (geometry2 is IMultiPatch multipatch2)
			{
				return GetIntersectionPointsXY(polycurve1, multipatch2, tolerance, planar);
			}

			throw new ArgumentOutOfRangeException(
				$"Unsupported geometry type: {geometry2.GeometryType}");
		}

		public static IMultipoint GetIntersectionPointsXY(
			[NotNull] IMultiPatch multipatch1,
			[NotNull] IGeometry geometry2,
			double tolerance,
			bool planar = false)
		{
			// TODO: Use clone to improve performance
			IMultipoint result = GeometryFactory.CreateEmptyMultipoint(multipatch1);

			IEnvelope curve1Envelope = multipatch1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance (directly from GDB):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(
					curve1Envelope.XMax, curve1Envelope.YMax);

			var intersectionPointList = new List<IntersectionPoint3D>();

			ISegmentList otherLinestrings = null;
			IPointList otherPoints = null;
			IPnt otherPoint = null;

			if (geometry2 is IMultiPatch multipatch2)
			{
				otherLinestrings = GeometryConversionUtils.CreatePolyhedron(multipatch2);
			}
			else if (geometry2 is IPolycurve polycurve2)
			{
				otherLinestrings =
					GeometryConversionUtils.CreateMultiPolycurve(
						polycurve2, tolerance, curve1Envelope);
			}
			else if (geometry2 is IMultipoint multipoint2)
			{
				otherPoints = GeometryConversionUtils.CreateMultipoint(multipoint2);
			}
			else if (geometry2 is IPoint point)
			{
				otherPoint =
					GeometryConversionUtils.CreatePnt(point, GeometryUtils.IsZAware(point));
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					$"Unsupported geometry type: {geometry2.GeometryType}");
			}

			Polyhedron sourcePolyhedron = GeometryConversionUtils.CreatePolyhedron(multipatch1);

			if (otherLinestrings != null)
			{
				intersectionPointList.AddRange(
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) sourcePolyhedron, (ISegmentList) otherLinestrings, tolerance,
						false));
			}
			else if (otherPoints != null)
			{
				intersectionPointList.AddRange(
					GeomTopoOpUtils.GetIntersectionPoints(
						sourcePolyhedron, otherPoints, tolerance, true));
			}
			else if (otherPoint != null)
			{
				intersectionPointList.AddRange(
					GeomTopoOpUtils.GetIntersectionPoints(
						sourcePolyhedron, otherPoint, 0, tolerance, false));
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					$"Unsupported geometry type: {geometry2.GeometryType}");
			}

			Multipoint<Pnt3D> resultMultipnt =
				new Multipoint<Pnt3D>(intersectionPointList.Select(ip => ip.Point));

			double zTolerance = planar ? double.NaN : tolerance;

			GeomTopoOpUtils.Simplify(resultMultipnt, tolerance, zTolerance);

			GeometryConversionUtils.AddPoints(resultMultipnt.GetPoints(), result);

			return result;
		}

		public static IMultipoint GetIntersectionPointsXY(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IPolycurve polycurve2,
			double tolerance,
			[CanBeNull] IPolyline linearIntersectionResult = null,
			bool planar = false)
		{
			IEnvelope curve1Envelope = polycurve1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance (directly from GDB):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(
					curve1Envelope.XMax, curve1Envelope.YMax);

			// Note: Getting the paths from the GeometryCollection takes a large percentage of the entire method
			MultiPolycurve otherLinestrings =
				GeometryConversionUtils.CreateMultiPolycurve(
					polycurve2, tolerance, curve1Envelope);

			Multipoint<IPnt> resultMultipnt = Multipoint<IPnt>.CreateEmpty();

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve1);

			// Note: For polygons with many rings it is more efficient to have 1 spatial index
			//       and process the segment intersections across rings
			foreach (IPath path1 in GeometryUtils.GetPaths(polycurve1))
			{
				Linestring path1Linestring = GeometryConversionUtils.GetLinestring(path1, ignoreZs);

				var intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) path1Linestring, (ISegmentList) otherLinestrings, tolerance,
						false);

				if (intersectionPoints.Count == 0)
				{
					continue;
				}

				resultMultipnt.AddPoints(intersectionPoints.Select(ip => ip.Point));

				if (linearIntersectionResult != null)
				{
					IList<Linestring> intersectionLines =
						GeomTopoOpUtils.GetIntersectionLinesXY(
							path1Linestring, otherLinestrings, tolerance);

					GeometryConversionUtils.AddPaths(intersectionLines,
					                                 linearIntersectionResult);
				}
			}

			double zTolerance = planar ? double.NaN : tolerance;

			// Snapping to spatial reference causes TOP-5470. Intersection points must be as
			// accurate as possible, otherwise the participating segments will not be found any more
			// in downstream operations! This simplify only clusters but does not snap. As opposed
			// to multipoint simplification (which uses the resolution) this uses the tolerance.
			GeomTopoOpUtils.Simplify(resultMultipnt, tolerance, zTolerance);

			// TODO: Use clone to improve performance
			IMultipoint result = GeometryFactory.CreateEmptyMultipoint(polycurve1);
			GeometryConversionUtils.AddPoints(resultMultipnt.GetPoints(), result);

			return result;
		}

		public static IMultipoint GetIntersectionPointsXY(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IMultipoint multipoint2,
			double tolerance,
			bool planar = false)
		{
			// TODO: Use clone to improve performance
			IMultipoint result = GeometryFactory.CreateEmptyMultipoint(polycurve1);

			IEnvelope curve1Envelope = polycurve1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance (directly from GDB):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(curve1Envelope.XMax, curve1Envelope.YMax);

			// TODO: Make segment finding symmetrical in order to profit from a potential spatial index
			//       on the source
			Multipoint<IPnt> otherPoints = GeometryConversionUtils.CreateMultipoint(multipoint2);

			bool includeRingInteriorPoints = polycurve1 is IPolygon;

			Multipoint<IPnt> resultMultipnt = Multipoint<IPnt>.CreateEmpty();

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve1);

			foreach (IPath path1 in GeometryUtils.GetPaths(polycurve1))
			{
				Linestring path1Linestring = GeometryConversionUtils.GetLinestring(path1, ignoreZs);

				var intersectionPoints =
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) path1Linestring, (IPointList) otherPoints, tolerance,
						includeRingInteriorPoints);

				resultMultipnt.AddPoints(intersectionPoints.Select(ip => ip.Point));
			}

			double zTolerance = planar ? double.NaN : tolerance;

			GeomTopoOpUtils.Simplify(resultMultipnt, tolerance, zTolerance);

			GeometryConversionUtils.AddPoints(resultMultipnt.GetPoints(), result);

			return result;
		}

		public static IMultipoint GetIntersectionPointsXY(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IMultiPatch multipatch2,
			double tolerance,
			bool planar = false)
		{
			// TODO: Use clone to improve performance
			IMultipoint result = GeometryFactory.CreateEmptyMultipoint(polycurve1);

			IEnvelope curve1Envelope = polycurve1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance (directly from GDB):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(curve1Envelope.XMax, curve1Envelope.YMax);

			ISegmentList sourceSegments = GeometryConversionUtils.CreateMultiPolycurve(polycurve1);

			var intersectionPointList = new List<IntersectionPoint3D>();
			foreach (RingGroup ringGroup in GeometryConversionUtils.CreateRingGroups(multipatch2))
			{
				intersectionPointList.AddRange(
					GeomTopoOpUtils.GetIntersectionPoints(
						(ISegmentList) sourceSegments, (ISegmentList) ringGroup, tolerance, true));
			}

			Multipoint<Pnt3D> resultMultipnt =
				new Multipoint<Pnt3D>(intersectionPointList.Select(ip => ip.Point));

			double zTolerance = planar ? double.NaN : tolerance;

			GeomTopoOpUtils.Simplify(resultMultipnt, tolerance, zTolerance);

			GeometryConversionUtils.AddPoints(resultMultipnt.GetPoints(), result);

			return result;
		}

		public static IPolyline GetIntersectionLinesXY(
			IPolycurve polycurve1,
			IPolycurve polycurve2,
			double tolerance)
		{
			IPolyline result = GeometryFactory.CreateEmptyPolyline(polycurve1);

			IEnvelope curve1Envelope = polycurve1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance (directly from GDB):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(
					curve1Envelope.XMax, curve1Envelope.YMax);

			MultiPolycurve otherLinestrings =
				GeometryConversionUtils.CreateMultiPolycurve(
					polycurve2, tolerance, curve1Envelope);

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve1);

			foreach (IPath path1 in GeometryUtils.GetPaths(polycurve1))
			{
				Linestring path1Linestring = GeometryConversionUtils.GetLinestring(path1, ignoreZs);

				IList<Linestring> intersectionLines =
					GeomTopoOpUtils.GetIntersectionLinesXY(path1Linestring, otherLinestrings,
					                                       tolerance);

				GeometryConversionUtils.AddPaths(intersectionLines, result);
			}

			GeometryUtils.Simplify(result);

			return result;
		}

		/// <summary>
		/// Calculates the segments where the provided polycurves differ only in Z.
		/// </summary>
		/// <param name="polycurve1"></param>
		/// <param name="polycurve2"></param>
		/// <param name="tolerance"></param>
		/// <remarks>Polycurves with non-linear segments are currently not supported!</remarks>
		/// <returns></returns>
		public static IPolyline GetZOnlyDifferenceLines(
			IPolycurve polycurve1,
			IPolycurve polycurve2,
			double tolerance)
		{
			IPolyline result = GeometryFactory.CreateEmptyPolyline(polycurve1);

			IEnvelope curve1Envelope = polycurve1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance
			// and there has been no significant loss of precision (e.g. due to subtractions):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(
					curve1Envelope.XMax, curve1Envelope.YMax);

			MultiPolycurve otherLinestrings =
				GeometryConversionUtils.CreateMultiPolycurve(
					polycurve2, tolerance, curve1Envelope);

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve1);

			foreach (IPath path1 in GeometryUtils.GetPaths(polycurve1))
			{
				Linestring path1Linestring = GeometryConversionUtils.GetLinestring(path1, ignoreZs);

				IList<Linestring> intersectionLines =
					GeomTopoOpUtils.GetZOnlyDifferences(path1Linestring, otherLinestrings,
					                                    tolerance);

				GeometryConversionUtils.AddPaths(intersectionLines, result);
			}

			GeometryUtils.Simplify(result);

			return result;
		}

		/// <summary>
		/// Calculates the segments where the provided polycurves differ in XY. Additionally
		/// the segments where they differ only in Z are returned.
		/// </summary>
		/// <param name="polycurve1"></param>
		/// <param name="polycurve2"></param>
		/// <param name="tolerance"></param>
		/// <param name="zOnlyDifferences">Empty input polyline to be populated with the z-only
		///   differences, if desired.</param>
		/// <param name="zTolerance"></param>
		/// <remarks>Polycurves with non-linear segments are currently not supported!</remarks>
		/// <returns></returns>
		[NotNull]
		public static IPolyline GetDifferenceLinesXY(
			[NotNull] IPolycurve polycurve1,
			[NotNull] IPolycurve polycurve2,
			double tolerance,
			[CanBeNull] IPolyline zOnlyDifferences = null,
			double zTolerance = double.NaN)
		{
			Stopwatch totalTime = _msg.DebugStartTiming();

			IPolyline result = GeometryFactory.CreateEmptyPolyline(polycurve1);

			IEnvelope curve1Envelope = polycurve1.Envelope;

			// Currently assuming the input comes snapped to resolution/tolerance
			// and there has been no significant loss of precision (e.g. due to subtractions):
			tolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(
					curve1Envelope.XMax, curve1Envelope.YMax);

			MultiPolycurve otherLinestrings =
				GeometryConversionUtils.CreateMultiPolycurve(
					polycurve2, tolerance, curve1Envelope);

			Stopwatch calculationTime = new Stopwatch();

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve1);

			foreach (IPath path1 in GeometryUtils.GetPaths(polycurve1))
			{
				Linestring path1Linestring = GeometryConversionUtils.GetLinestring(path1, ignoreZs);

				calculationTime.Start();

				IList<Linestring> zOnlyDiffLinestrings =
					zOnlyDifferences != null ? new List<Linestring>() : null;

				IList<Linestring> differenceLinesXY =
					GeomTopoOpUtils.GetDifferenceLinesXY(
						path1Linestring, otherLinestrings,
						tolerance, zOnlyDiffLinestrings, zTolerance);

				calculationTime.Stop();

				GeometryConversionUtils.AddPaths(differenceLinesXY, result);

				if (zOnlyDifferences != null)
				{
					GeometryConversionUtils.AddPaths(zOnlyDiffLinestrings,
					                                 zOnlyDifferences);
				}
			}

			// Currently needed to merge source ring start/end points that are part of different result linestrings
			GeometryUtils.Simplify(result);

			_msg.DebugStopTiming(totalTime,
			                     "Custom difference (calculation time only: {0}): XY: {1} map units, Z-only: {2}",
			                     calculationTime.ElapsedMilliseconds, result.Length,
			                     zOnlyDifferences?.Length ?? double.NaN);

			return result;
		}

		public static IEnumerable<CutPolyline> GetIntersectionLines3D(
			[NotNull] IMultiPatch multiPatch1,
			[NotNull] IMultiPatch multiPatch2)
		{
			if (GeometryUtils.Disjoint(multiPatch1, multiPatch2))
			{
				yield break;
			}

			var intersections = new List<IntersectionPath3D>();

			// Process each pair of 'connected components' / ring group
			foreach (
				GeometryPart mp1Part in GeometryPart.FromGeometry(
					multiPatch1, false))
			{
				foreach (GeometryPart mp2Part in
				         GeometryPart.FromGeometry(multiPatch2, false))
				{
					// Main outer ring is the first part by definition...
					var mp1Ring = (IRing) mp1Part.LowLevelGeometries[0];
					var mp2Ring = (IRing) mp2Part.LowLevelGeometries[0];

					IList<IntersectionPath3D> intersection = GetIntersectionLines3D(
						mp1Ring, mp2Ring,
						false);

					if (intersection != null)
					{
						intersections.AddRange(intersection);
					}
				}
			}

			ISpatialReference sr = multiPatch1.SpatialReference;

			foreach (
				IGrouping<RingPlaneTopology, IntersectionPath3D> groupedIntersections in
				intersections.GroupBy(ip => ip.RingPlaneTopology))
			{
				var cutPolyline = new CutPolyline(
					GeometryConversionUtils.CreatePolyline(groupedIntersections, sr),
					groupedIntersections.Key);

				yield return cutPolyline;
			}
		}

		[CanBeNull]
		public static IList<IntersectionPath3D> GetIntersectionLines3D(
			[NotNull] IRing planarRing1,
			[NotNull] IRing planarRing2,
			bool boundaryIntersectionsOnly)
		{
			Assert.ArgumentCondition(GeometryUtils.IsZAware(planarRing1),
			                         "ring1 is not z-aware");
			Assert.ArgumentCondition(GeometryUtils.IsZAware(planarRing2),
			                         "ring2 is not z-aware");

			List<Pnt3D> ring1Pnts3D = GeometryConversionUtils.GetPntList(planarRing1);
			List<Pnt3D> ring2Pnts3D = GeometryConversionUtils.GetPntList(planarRing2);

			double tolerance = GeometryUtils.GetXyTolerance(planarRing1);

			// Idea: Filter intersection lines where the multipatches do not actually cross in 3D but only touch
			//       This might avoid the problem of non-simple intersection lines with 'junctions' which result 
			//       in problems of multiple assignment to footprint parts.
			IList<IntersectionPath3D> intersections =
				GeomTopoOpUtils.IntersectRings3D(ring1Pnts3D, ring2Pnts3D, tolerance,
				                                 boundaryIntersectionsOnly);

			return intersections?.Count > 0 ? intersections : null;
		}

		public static IMultipoint GetUnionPoints(
			IMultipoint multipoint1,
			IMultipoint multipoint2,
			double tolerance,
			bool in3d = false)
		{
			Multipoint<IPnt> multipoint =
				GeometryConversionUtils.CreateMultipoint(multipoint1, multipoint2);

			double zTolerance = in3d ? tolerance : double.NaN;
			IList<KeyValuePair<IPnt, List<IPnt>>> clusters =
				GeomTopoOpUtils.Cluster(multipoint.GetPoints().ToList(), p => p, tolerance,
				                        zTolerance);

			IMultipoint result = (IMultipoint) GeometryConversionUtils.CreatePointCollection(
				multipoint1, clusters.Select(c => c.Key));

			return result;
		}

		[NotNull]
		public static IGeometry Difference([NotNull] IGeometry g1, [NotNull] IGeometry g2)
		{
			return Difference((ITopologicalOperator) g1, g2);
		}

		[NotNull]
		public static IGeometry Difference([NotNull] ITopologicalOperator topoOp,
		                                   [NotNull] IGeometry g2)
		{
			try
			{
				return GetDifferenceCore(topoOp, g2);
			}
			catch (COMException comEx)
			{
				if (comEx.ErrorCode != -2147220888)
				{
					_msg.DebugFormat("Error in Difference(): {0}", comEx.Message);

					_msg.Debug(GeometryUtils.ToString(topoOp as IGeometry));
					_msg.Debug(GeometryUtils.ToString(g2));

					throw;
				}

				// The xy cluster tolerance was too large for the extent of the data.

				var geometry = topoOp as IGeometry;

				if (geometry?.SpatialReference == null)
				{
					_msg.DebugFormat("Error in Difference(): {0}", comEx.Message);

					_msg.Debug(GeometryUtils.ToString(topoOp as IGeometry));
					_msg.Debug(GeometryUtils.ToString(g2));

					throw;
				}

				_msg.DebugFormat("Difference(): workaround for '{0}'", comEx.Message);

				ISpatialReference origSRef = geometry.SpatialReference;

				ISpatialReference highResSRef =
					SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
						origSRef, resolutionFactor: 10);

				geometry.SpatialReference = highResSRef;

				try
				{
					IGeometry result = GetDifferenceCore(topoOp, g2);

					result.SpatialReference = origSRef;

					if (result.Dimension != esriGeometryDimension.esriGeometry0Dimension)
					{
						GeometryUtils.Simplify(result, allowReorder: true,
						                       allowPathSplitAtIntersections: false);
					}

					return result;
				}
				catch (Exception ex)
				{
					_msg.DebugFormat("Error in workaround for Difference(): {0}",
					                 ex.Message);

					_msg.Debug(GeometryUtils.ToString((IGeometry) topoOp));
					_msg.Debug(GeometryUtils.ToString(g2));

					throw;
				}
			}
			catch (Exception ex)
			{
				_msg.DebugFormat("Error in Difference(): {0}", ex.Message);

				_msg.Debug(GeometryUtils.ToString(topoOp as IGeometry));
				_msg.Debug(GeometryUtils.ToString(g2));

				throw;
			}
		}

		[NotNull]
		public static IGeometry Intersect([NotNull] IGeometry g1,
		                                  [NotNull] IGeometry g2,
		                                  esriGeometryDimension dimension)
		{
			if (g1 is ITopologicalOperator topoOp)
			{
				return Intersect(topoOp, g2, dimension);
			}

			if (g1 is IEnvelope envelope1 && g2 is IEnvelope envelope2 &&
			    dimension == esriGeometryDimension.esriGeometry2Dimension)
			{
				IEnvelope result = GeometryFactory.Clone(envelope1);
				result.Intersect(envelope2);

				return result;
			}

			throw new ArgumentException(
				$"Unsupported geometry type(s): {g1.GeometryType} / {g2.GeometryType}.");
		}

		[NotNull]
		public static IGeometry Intersect([NotNull] ITopologicalOperator topoOp,
		                                  [NotNull] IGeometry g2,
		                                  esriGeometryDimension dimension)
		{
			IGeometry result = GetIntersectionCore(topoOp, g2, dimension);

			if (g2.IsEmpty)
			{
				// The result becomes M-unaware (observed at 10.4.4) - see IntersectionUtilsTest.IntersectionResultKeepsAwarenessDespiteEmptyOtherGeometry()
				// The result is empty but should keep its properties nevertheless:
				if (GeometryUtils.IsMAware((IGeometry) topoOp) &&
				    ! GeometryUtils.IsMAware(result))
				{
					GeometryUtils.MakeMAware(result);
				}

				if (GeometryUtils.IsPointIDAware((IGeometry) topoOp) &&
				    ! GeometryUtils.IsPointIDAware(result))
				{
					GeometryUtils.MakePointIDAware(result);
				}
			}

			return result;
		}

		/// <summary>
		/// Deletes segments that are considered having a linear intersection w.r.t the tolerance.
		/// These are typically spikes or narrow straits in rings, such as these:
		/// ---------------------------        -------*          ---------
		/// |      *-----------       |        |      *          |       |
		/// |      |          |       |   ->   |      |          |       |
		/// |      |          |       |        |      |          |       |
		/// |______|          |_______|        |______|          |_______|
		/// The minimum segment length, if provided, determines whether a vertex will be
		/// kept on both sides of the strait (as depicted in the left result part) if the
		/// minimum segment length is not violated. The advantage of extra vertices is that
		/// the existing lines are kept in place which might be desired, especially if the
		/// tolerance is large.
		/// </summary>
		/// <param name="polygon"></param>
		/// <param name="tolerance"></param>
		/// <param name="minimumSegmentLength"></param>
		/// <returns>The resulting polygon with deleted self-intersections, or the input polygon
		/// if no self-intersections are present.</returns>
		[NotNull]
		public static IPolygon RemoveRingLinearSelfIntersections(
			[NotNull] IPolygon polygon,
			double tolerance,
			double? minimumSegmentLength = null)
		{
			var multiPolycurve = GeometryConversionUtils.CreateMultiPolycurve(polygon);

			var resultRings = new List<Linestring>();

			bool anyChange = false;
			foreach (Linestring linestring in multiPolycurve.GetLinestrings())
			{
				var thisRingResult = new List<Linestring>();

				if (GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
					    linestring, tolerance, thisRingResult, minimumSegmentLength))
				{
					anyChange = true;
					resultRings.AddRange(thisRingResult);
				}
				else
				{
					resultRings.Add(linestring);
				}
			}

			return anyChange
				       ? GeometryConversionUtils.CreatePolygon(polygon, resultRings)
				       : polygon;
		}

		#region Non-public

		[NotNull]
		private static IGeometry GetIntersectionCore(
			[NotNull] ITopologicalOperator topoOp,
			[NotNull] IGeometry g2,
			esriGeometryDimension dimension)
		{
			try
			{
				return topoOp.Intersect(g2, dimension);
			}
			catch (COMException comEx)
			{
				if (comEx.ErrorCode != -2147220888)
				{
					// unhandled COM exception

					_msg.DebugFormat("Error in Intersect() for {0}: {1}",
					                 dimension, comEx.Message);

					_msg.Debug(GeometryUtils.ToString(topoOp as IGeometry));
					_msg.Debug(GeometryUtils.ToString(g2));

					throw;
				}

				// The xy cluster tolerance was too large for the extent of the data.

				// TODO this occurs with vertical lines, the workaround does not help in that case

				var geometry = topoOp as IGeometry;
				if (geometry?.SpatialReference == null)
				{
					_msg.DebugFormat("Error in Intersect() for {0}: {1}",
					                 dimension, comEx.Message);

					_msg.Debug(GeometryUtils.ToString(geometry));
					_msg.Debug(GeometryUtils.ToString(g2));

					throw;
				}

				_msg.DebugFormat("Intersect(): Workaround for '{0}'", comEx.Message);

				ISpatialReference origSRef = geometry.SpatialReference;

				ISpatialReference highResSRef =
					SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
						origSRef, resolutionFactor: 10);

				geometry.SpatialReference = highResSRef;

				try
				{
					IGeometry result = topoOp.Intersect(g2, dimension);

					result.SpatialReference = origSRef;

					if (result.Dimension != esriGeometryDimension.esriGeometry0Dimension)
					{
						GeometryUtils.Simplify(result, allowReorder: true,
						                       allowPathSplitAtIntersections: false);
					}

					return result;
				}
				catch (Exception ex)
				{
					_msg.DebugFormat("Error in Intersect() workaround for {0}: {1}",
					                 dimension, ex.Message);

					_msg.Debug(GeometryUtils.ToString(geometry));
					_msg.Debug(GeometryUtils.ToString(g2));

					throw;
				}
				finally
				{
					geometry.SpatialReference = origSRef;
				}
			}
			catch (Exception ex)
			{
				_msg.DebugFormat("Error in Intersect() for {0}: {1}",
				                 dimension, ex.Message);

				_msg.Debug(GeometryUtils.ToString(topoOp as IGeometry));
				_msg.Debug(GeometryUtils.ToString(g2));

				throw;
			}
		}

		[NotNull]
		private static IGeometry GetDifferenceCore([NotNull] ITopologicalOperator topoOp,
		                                           [NotNull] IGeometry g2)
		{
			IGeometry difference = topoOp.Difference(g2);

			if (((IGeometry) topoOp).GeometryType ==
			    esriGeometryType.esriGeometryMultipoint)
			{
				// work-around for multipoint result
				difference = RestoreAwareness(topoOp, difference);
			}

			if (! (difference is IPolygon))
			{
				return difference;
			}

			if (difference.IsEmpty)
			{
				return difference;
			}

			if (! GeometryUtils.HasNonLinearSegments(difference))
			{
				return difference;
			}

			double? xyTolerance =
				((ISpatialReferenceTolerance) difference.SpatialReference)?.XYTolerance;

			var ringIndexesToRemove = new List<int>();

			var geometryCollection = (IGeometryCollection) difference;
			int ringCount = geometryCollection.GeometryCount;

			for (var ringIndex = 0; ringIndex < ringCount; ringIndex++)
			{
				var ring = (IRing) geometryCollection.Geometry[ringIndex];

				if (ring.IsExterior)
				{
					continue;
				}

				// interior ring

				if (! GeometryUtils.HasNonLinearSegments(ring))
				{
					// ring has no non-linear segments, consider it ok
					continue;
				}

				if (xyTolerance != null)
				{
					double minimumAreaForValidRing = xyTolerance.Value *
					                                 ((ring.Length -
					                                   2 * xyTolerance.Value) / 2);

					double ringArea = Math.Abs(((IArea) ring).Area);
					if (ringArea > minimumAreaForValidRing * 10)
					{
						continue; // large enough, consider it ok
					}
				}

				// small inner ring with non-linear segments: 
				// - convert it to a polygon and simplify it 
				// - if the result is empty then the ring was invalid
				IPolygon ringPolygon = GeometryFactory.CreatePolygon(ring);
				GeometryUtils.Simplify(ringPolygon);

				if (ringPolygon.IsEmpty)
				{
					ringIndexesToRemove.Add(ringIndex);
				}
			}

			if (ringIndexesToRemove.Count > 0)
			{
				ringIndexesToRemove.Reverse(); // remove highest indexes first

				foreach (int ringIndex in ringIndexesToRemove)
				{
					geometryCollection.RemoveGeometries(ringIndex, 1);
				}

				GeometryUtils.Simplify(difference); // maybe not needed
			}

			return difference;
		}

		private static IGeometry RestoreAwareness(ITopologicalOperator topoOp,
		                                          IGeometry difference)
		{
			bool zAwarenessLost = GeometryUtils.IsZAware((IGeometry) topoOp) &&
			                      ! GeometryUtils.IsZAware(difference);

			bool mAwarenessLost = GeometryUtils.IsMAware((IGeometry) topoOp) &&
			                      ! GeometryUtils.IsMAware(difference);

			bool pointIdAwarenessLost =
				GeometryUtils.IsPointIDAware((IGeometry) topoOp) &&
				! GeometryUtils.IsPointIDAware(difference);

			// May be it would work to just restore the awareness. In simple cases the actual coodinate values are correct
			// but this would need to be very well tested! Safe option:
			if (zAwarenessLost || mAwarenessLost || pointIdAwarenessLost)
			{
				// Intersect works ok (has simpler work-around):
				difference = Intersect(topoOp, difference, difference.Dimension);
			}

			return difference;
		}

		[NotNull]
		private static IEnumerable<esriGeometryDimension> GetIntersectDimensions(
			esriGeometryDimension maximumDimension)
		{
			switch (maximumDimension)
			{
				case esriGeometryDimension.esriGeometry0Dimension:
					return _intersectDimensions0;

				case esriGeometryDimension.esriGeometry1Dimension:
					return _intersectDimensions1;

				case esriGeometryDimension.esriGeometry2Dimension:
					return _intersectDimensions2;

				case esriGeometryDimension.esriGeometry25Dimension:
				case esriGeometryDimension.esriGeometry3Dimension:
				case esriGeometryDimension.esriGeometryNoDimension:
					throw new ArgumentException("dimension not valid for intersection");
				default:
					throw new ArgumentOutOfRangeException(nameof(maximumDimension));
			}
		}

		private static esriGeometryDimension GetMaximumIntersectDimension(
			[NotNull] IGeometry g1,
			[NotNull] IGeometry g2)
		{
			esriGeometryDimension g1Dim = g1.Dimension;
			esriGeometryDimension g2Dim = g2.Dimension;

			Assert.False(g1Dim == esriGeometryDimension.esriGeometryNoDimension,
			             "g1 dimension not defined");
			Assert.False(g2Dim == esriGeometryDimension.esriGeometryNoDimension,
			             "g2 dimension not defined");

			if (g1Dim == esriGeometryDimension.esriGeometry0Dimension ||
			    g2Dim == esriGeometryDimension.esriGeometry0Dimension)
			{
				return esriGeometryDimension.esriGeometry0Dimension;
			}

			if (g1Dim == esriGeometryDimension.esriGeometry1Dimension ||
			    g2Dim == esriGeometryDimension.esriGeometry1Dimension)
			{
				return esriGeometryDimension.esriGeometry1Dimension;
			}

			if (g1Dim == esriGeometryDimension.esriGeometry2Dimension ||
			    g2Dim == esriGeometryDimension.esriGeometry2Dimension)
			{
				return esriGeometryDimension.esriGeometry2Dimension;
			}

			throw new ArgumentException(
				string.Format(
					"Unsupported input dimensions for intersection: g1:{0}, g2:{1}",
					g1Dim, g2Dim));
		}

		private static bool CanUseIntersectMultidimension(esriGeometryType type1,
		                                                  esriGeometryType type2)
		{
			switch (type1)
			{
				case esriGeometryType.esriGeometryMultipoint:
					switch (type2)
					{
						case esriGeometryType.esriGeometryPolyline:
						case esriGeometryType.esriGeometryPolygon:
							return true;
						default:
							return false;
					}

				case esriGeometryType.esriGeometryPolyline:
					switch (type2)
					{
						case esriGeometryType.esriGeometryMultipoint:
						case esriGeometryType.esriGeometryPolygon:
							return true;
						default:
							return false;
					}

				case esriGeometryType.esriGeometryPolygon:
					switch (type2)
					{
						case esriGeometryType.esriGeometryMultipoint:
						case esriGeometryType.esriGeometryPolyline:
							return true;
						default:
							return false;
					}

				default:
					return false;
			}
		}

		/// <summary>
		/// Gets the intersection between the multipoint and the polygon.
		/// ITopologicalOperator.Intersect does not return the correct 
		/// result in this case.
		/// </summary>
		/// <param name="multipoint"></param>
		/// <param name="geometry"></param>
		/// <param name="expectedCount"></param>
		/// <returns></returns>
		private static IEnumerable<IPoint> Intersect(IMultipoint multipoint,
		                                             IGeometry geometry,
		                                             int expectedCount)
		{
			var result = new List<IPoint>();

			foreach (IPoint point in GeometryUtils.GetPoints(
				         (IPointCollection) multipoint))
			{
				if (GeometryUtils.Intersects(geometry, point))
				{
					result.Add(GeometryFactory.Clone(point));
				}
			}

			if (result.Count != expectedCount)
			{
				// TODO: repro unit test, remove warning
				_msg.WarnFormat(
					"Employed work-around to find multipoint-polygon intersection points. Please report this incident.");
				_msg.DebugFormat(GeometryUtils.ToXmlString(geometry));
				_msg.DebugFormat(GeometryUtils.ToXmlString(multipoint));
			}

			return result;
		}

		private static void AddIntersectingPoint(IPoint point,
		                                         IPolyline intersectedGeometry,
		                                         IPointCollection toIntersectionPoints)
		{
			if (! GeometryUtils.Contains((IGeometry) toIntersectionPoints, point) &&
			    GeometryUtils.Intersects(point, intersectedGeometry))
			{
				object missing = Type.Missing;

				_msg.DebugFormat(
					"TopologicalOperator.Intersects did not find intersection point {0}",
					GeometryUtils.ToString(point));

				toIntersectionPoints.AddPoint(point, ref missing, ref missing);
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
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			[NotNull] IPolyline linearIntersections)
		{
			var intersectionPaths = (IGeometryCollection) linearIntersections;

			for (var i = 0; i < intersectionPaths.GeometryCount; i++)
			{
				var path = (IPath) intersectionPaths.Geometry[i];

				if (path.IsClosed)
				{
					// fully coincident rings: The result line should be split at the original rings' 
					// start/end points and not at a random location as it is done by ArcObjects.

					// find the corresponding path in both geometries

					// Increase tolerance by approximately sqrt(2) to avoid no-hits resulting in not null violation
					const double sqrt2Approx = 1.5;
					double searchTolerance =
						GeometryUtils.GetXyTolerance(polyline1) * sqrt2Approx;

					var partInCurve1 = (IPath) GeometryUtils.GetCoincidentPath(
						(IGeometryCollection) polyline2, path, searchTolerance);

					var partInCurve2 = (IPath) GeometryUtils.GetCoincidentPath(
						(IGeometryCollection) polyline1, path, searchTolerance);

					// add their start points
					var cutPoints = new List<IPoint>();

					if (partInCurve1.IsClosed &&
					    ! GeometryUtils.AreEqualInXY(partInCurve1.FromPoint,
					                                 path.FromPoint))
					{
						cutPoints.Add(partInCurve1.FromPoint);
					}

					if (partInCurve2.IsClosed &&
					    ! GeometryUtils.AreEqualInXY(partInCurve2.FromPoint,
					                                 path.FromPoint))
					{
						cutPoints.Add(partInCurve2.FromPoint);
					}

					if (cutPoints.Count > 0)
					{
						const bool projectPointsOntoPathToSplit = false;
						double cutOffDistance = GeometryUtils.GetXyTolerance(path);
						IGeometryCollection splitPaths =
							GeometryUtils.SplitPath(path,
							                        (IPointCollection)
							                        GeometryFactory.CreateMultipoint(
								                        cutPoints),
							                        projectPointsOntoPathToSplit,
							                        cutOffDistance,
							                        splitCurves => true);

						GeometryUtils.ReplaceGeometryPart(
							linearIntersections, i, splitPaths);
					}
				}
			}
		}

		private static IGeometry IntersectNonPlanar(
			[NotNull] ITopologicalOperator6 topoOp6,
			[NotNull] IGeometry other,
			esriGeometryDimension resultDimension)
		{
			const bool nonPlanar = true;

			IGeometry result = topoOp6.IntersectEx(
				other, nonPlanar, resultDimension);

			if (GeometryUtils.IsZAware(other) &&
			    ! GeometryUtils.IsZAware(result))
			{
				// The result has perfectly correct Z values but is un-aware
				GeometryUtils.MakeZAware(result);
			}

			// Work-around to protect against duplicate result points
			// (see IntersectionUtilsTest.CanGetNonZAwareNonPlanarIntersection):
			GeometryUtils.Simplify(result);

			return result;
		}

		private static bool IsVerticalWithoutXyExtent(IGeometry geometry)
		{
			if (! GeometryUtils.IsZAware(geometry))
			{
				return false;
			}

			IEnvelope envelope = geometry.Envelope;

			return MathUtils.AreEqual(envelope.Width, 0) &&
			       MathUtils.AreEqual(envelope.Height, 0) &&
			       envelope.Depth > 0;
		}

		private static IMultipoint GetIntersectionPointsWithVerticalGeometry(
			[NotNull] IPointCollection verticalGeometry,
			[NotNull] IGeometry target,
			bool nonPlanar)
		{
			var resultList = new List<IPoint>();

			foreach (IPoint sourcePoint in GeometryUtils.GetPoints(verticalGeometry))
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

				if (IntersectsNonPlanar(sourcePoint, (IGeometry) verticalGeometry))
				{
					resultList.Add(sourcePoint);
				}
			}

			IMultipoint result =
				GeometryFactory.CreateMultipoint(resultList);

			return result;
		}

		private static bool IntersectsNonPlanar([NotNull] IPoint sourcePoint,
		                                        [NotNull] IGeometry intersectedTarget)
		{
			bool result;

			if (GeometryUtils.IsZAware(sourcePoint))
			{
				// 3D intersection if target is also Z-aware, otherwise 2D intersection
				if (GeometryUtils.IsZAware(intersectedTarget))
				{
					result = ! ((IRelationalOperator3D) sourcePoint).Disjoint3D(
						         intersectedTarget);
				}
				else
				{
					result = true;
				}
			}
			else
			{
				// non-Z-aware source: Non-planar intersection probably not very useful (unless geometry is and should be non-simple)
				result = true;
			}

			return result;
		}

		#endregion
	}
}

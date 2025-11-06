using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	public static class LineIntersectionUtils
	{
		[ThreadStatic] private static IPoint _templatePoint1;
		[ThreadStatic] private static IPoint _templatePoint2;

		private static IPoint TemplatePoint1 =>
			_templatePoint1 ?? (_templatePoint1 = new PointClass());

		private static IPoint TemplatePoint2 =>
			_templatePoint2 ?? (_templatePoint2 = new PointClass());

		public static IEnumerable<LineIntersection> GetIntersections(
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			bool is3D)
		{
			const bool assumeIntersecting = true;
			IMultipoint intersections =
				IntersectionUtils.GetIntersectionPoints(
					polyline1, polyline2,
					assumeIntersecting,
					IntersectionPointOptions.DisregardLinearIntersections);

			if (intersections.IsEmpty)
			{
				yield break;
			}

			IEnumVertex enumIntersectionPoints =
				((IPointCollection) intersections).EnumVertices;

			do
			{
				int vertexIndex;
				int outPartIndex;
				enumIntersectionPoints.QueryNext(TemplatePoint1,
				                                 out outPartIndex,
				                                 out vertexIndex);

				if (outPartIndex < 0 || vertexIndex < 0)
				{
					break;
				}

				var intersection = new LineIntersection(polyline1, polyline2,
				                                        TemplatePoint1, is3D);
				yield return intersection;
			} while (true);
		}

		[NotNull]
		public static IMultipoint GetInvalidIntersections(
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			AllowedLineInteriorIntersections allowedLineInteriorIntersections,
			bool reportOverlaps,
			double vertexSearchDistance)
		{
			Assert.ArgumentNotNull(polyline1, nameof(polyline1));
			Assert.ArgumentNotNull(polyline2, nameof(polyline2));

			IntersectionPointOptions intersectionPointOptions =
				reportOverlaps
					? IntersectionPointOptions.IncludeLinearIntersectionEndpoints
					: IntersectionPointOptions.DisregardLinearIntersections;

			// NOTE: this does not reliably find endpoint/interior intersections -> empty!! (even if Disjoint does return false)
			const bool assumeIntersecting = true;
			IMultipoint intersections = IntersectionUtils.GetIntersectionPoints(polyline1,
				polyline2,
				assumeIntersecting,
				intersectionPointOptions);

			// TODO catch missed end point/interior intersections by checking end points explicitly

			if (intersections.IsEmpty)
			{
				return intersections;
			}

			intersections = RemoveAllowedEndPointIntersections(intersections,
			                                                   polyline1, polyline2,
			                                                   allowedEndpointInteriorIntersections,
			                                                   vertexSearchDistance);

			return RemoveAllowedInteriorIntersections(intersections,
			                                          polyline1, polyline2,
			                                          allowedLineInteriorIntersections,
			                                          vertexSearchDistance);
		}

		[NotNull]
		private static IMultipoint RemoveAllowedInteriorIntersections(
			[NotNull] IMultipoint intersections,
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			AllowedLineInteriorIntersections allowedLineInteriorIntersections,
			double vertexSearchDistance)
		{
			switch (allowedLineInteriorIntersections)
			{
				case AllowedLineInteriorIntersections.None:
					return intersections;

				case AllowedLineInteriorIntersections.AtVertexOnBothLines:
					return RemoveVertexIntersections(intersections, polyline1, polyline2,
					                                 vertexSearchDistance);

				default:
					throw new ArgumentOutOfRangeException(nameof(allowedLineInteriorIntersections));
			}
		}

		[NotNull]
		private static IMultipoint RemoveAllowedEndPointIntersections(
			[NotNull] IMultipoint intersections,
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			double vertexSearchDistance)
		{
			if (allowedEndpointInteriorIntersections ==
			    AllowedEndpointInteriorIntersections.None &&
			    GeometryUtils.GetPointCount(intersections) == 1)
			{
				// NOTE this assumes that HasInvalidIntersections has returned 'true' for these lines
				// --> if there is a unique intersection point, we know it's not at two touching end points
				return intersections;
			}

			// this fails if polylines are non-simple
			IGeometry shape1Endpoints = ((ITopologicalOperator) polyline1).Boundary;
			IGeometry shape2Endpoints = ((ITopologicalOperator) polyline2).Boundary;

			IMultipoint innerIntersections = GetIntersectionsInvolvingInterior(intersections,
				shape1Endpoints,
				shape2Endpoints);

			if (innerIntersections.IsEmpty)
			{
				return innerIntersections;
			}

			switch (allowedEndpointInteriorIntersections)
			{
				case AllowedEndpointInteriorIntersections.All:
					// remove all intersection points that coincide with any end point
					return RemoveAnyEndpoints(innerIntersections, shape1Endpoints, shape2Endpoints);

				case AllowedEndpointInteriorIntersections.Vertex:
					// remove intersections that coincide with an end point of one polyline and a vertex of the other
					return RemoveEndPointVertexIntersections(innerIntersections,
					                                         polyline1, shape1Endpoints,
					                                         polyline2, shape2Endpoints,
					                                         vertexSearchDistance);

				case AllowedEndpointInteriorIntersections.None:
					return innerIntersections;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(allowedEndpointInteriorIntersections));
			}
		}

		[NotNull]
		private static IMultipoint GetIntersectionsInvolvingInterior(
			[NotNull] IMultipoint intersections,
			[NotNull] IGeometry shape1Endpoints,
			[NotNull] IGeometry shape2Endpoints)
		{
			IGeometry matchingEndpoints = ((ITopologicalOperator) shape1Endpoints).Intersect(
				shape2Endpoints, esriGeometryDimension.esriGeometry0Dimension);

			IMultipoint innerIntersections =
				matchingEndpoints.IsEmpty
					? intersections
					: (IMultipoint)
					((ITopologicalOperator) intersections).Difference(matchingEndpoints);
			return innerIntersections;
		}

		public static bool HasInvalidIntersection(
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			AllowedEndpointInteriorIntersections allowedEndpointInteriorIntersections,
			bool reportOverlaps,
			[NotNull] IPoint pointTemplate1,
			[NotNull] IPoint pointTemplate2,
			double vertexSearchDistance,
			[CanBeNull] out IPoint knownInvalidIntersection)
		{
			var relOp1 = (IRelationalOperator) polyline1;

			// NOTE: if Overlaps is true then Crosses is false 
			// EVEN IF THERE IS A CROSSING IN ADDITION TO THE OVERLAP
			if (relOp1.Crosses(polyline2))
			{
				knownInvalidIntersection = null;
				return true;
			}

			if (reportOverlaps && relOp1.Overlaps(polyline2))
			{
				knownInvalidIntersection = null;
				return true;
			}

			if (allowedEndpointInteriorIntersections ==
			    AllowedEndpointInteriorIntersections.All)
			{
				knownInvalidIntersection = null;
				return false;
			}

			bool allowVertexIntersections = allowedEndpointInteriorIntersections ==
			                                AllowedEndpointInteriorIntersections.Vertex;

			foreach (IPoint p2 in QueryEndpoints(polyline2, pointTemplate1))
			{
				if (relOp1.Disjoint(p2) || IntersectsEndPoint(p2, polyline1, pointTemplate2))
				{
					continue;
				}

				// end point of polyline2 intersects the interior of polyline1
				if (allowVertexIntersections &&
				    IntersectsVertex(p2, polyline1, pointTemplate2, vertexSearchDistance))
				{
					continue;
				}

				knownInvalidIntersection = GeometryFactory.Clone(p2);
				return true;
			}

			var relOp2 = (IRelationalOperator) polyline2;

			foreach (IPoint p1 in QueryEndpoints(polyline1, pointTemplate1))
			{
				if (relOp2.Disjoint(p1) || IntersectsEndPoint(p1, polyline2, pointTemplate2))
				{
					continue;
				}

				// end point of polyline1 intersects the interior of polyline2

				if (allowVertexIntersections &&
				    IntersectsVertex(p1, polyline2, pointTemplate2, vertexSearchDistance))
				{
					continue;
				}

				knownInvalidIntersection = GeometryFactory.Clone(p1);
				return true;
			}

			knownInvalidIntersection = null;
			return false;
		}

		[NotNull]
		private static IMultipoint RemoveVertexIntersections(
			[NotNull] IMultipoint intersectionPoints,
			[NotNull] IPolyline polyline1,
			[NotNull] IPolyline polyline2,
			double vertexSearchDistance)
		{
			var remainingPoints = new List<IPoint>();

			foreach (IPoint intersectionPoint in
			         QueryPoints(intersectionPoints, TemplatePoint1))
			{
				if (IntersectsVertex(intersectionPoint, polyline2, TemplatePoint2,
				                     vertexSearchDistance) &&
				    IntersectsVertex(intersectionPoint, polyline1, TemplatePoint2,
				                     vertexSearchDistance))
				{
					// intersection point intersects vertex on both polylines
					// -> skip
					continue;
				}

				remainingPoints.Add(GeometryFactory.Clone(intersectionPoint));
			}

			return GeometryFactory.CreateMultipoint(remainingPoints);
		}

		[NotNull]
		private static IMultipoint RemoveEndPointVertexIntersections(
			[NotNull] IMultipoint intersectionPoints,
			[NotNull] IPolyline polyline1,
			[NotNull] IGeometry polyline1Endpoints,
			[NotNull] IPolyline polyline2,
			[NotNull] IGeometry polyline2Endpoints,
			double vertexSearchDistance)
		{
			var polyline1EndpointsRelOp = (IRelationalOperator) polyline1Endpoints;
			var polyline2EndpointsRelOp = (IRelationalOperator) polyline2Endpoints;

			var remainingPoints = new List<IPoint>();

			foreach (IPoint intersectionPoint in
			         QueryPoints(intersectionPoints, TemplatePoint1))
			{
				if (! polyline1EndpointsRelOp.Disjoint(intersectionPoint))
				{
					// end point of polyline 1
					if (IntersectsVertex(intersectionPoint, polyline2, TemplatePoint2,
					                     vertexSearchDistance))
					{
						// intersection of end point of polyline 1 with vertex of polyline 2
						// -> skip
						continue;
					}
				}
				else if (! polyline2EndpointsRelOp.Disjoint(intersectionPoint))
				{
					// end point of polyline 2
					if (IntersectsVertex(intersectionPoint, polyline1, TemplatePoint2,
					                     vertexSearchDistance))
					{
						// intersection of end point of polyline 2 with vertex of polyline 1
						// -> skip
						continue;
					}
				}

				remainingPoints.Add(GeometryFactory.Clone(intersectionPoint));
			}

			return GeometryFactory.CreateMultipoint(remainingPoints);
		}

		[NotNull]
		private static IEnumerable<IPoint> QueryPoints(
			[NotNull] IGeometry intersectionPoints,
			[NotNull] IPoint templatePoint)
		{
			var points = intersectionPoints as IPointCollection;
			if (points != null)
			{
				int pointCount = points.PointCount;
				for (var i = 0; i < pointCount; i++)
				{
					points.QueryPoint(i, templatePoint);
					yield return templatePoint;
				}
			}
			else
			{
				var point = intersectionPoints as IPoint;
				if (point == null)
				{
					yield break;
				}

				yield return point;
			}
		}

		[NotNull]
		private static IEnumerable<IPoint> QueryEndpoints([NotNull] IPolyline polyline,
		                                                  [NotNull] IPoint template)
		{
			var parts = (IGeometryCollection) polyline;
			int partCount = parts.GeometryCount;

			if (partCount == 0)
			{
				yield break;
			}

			if (partCount == 1)
			{
				polyline.QueryFromPoint(template);
				yield return template;

				polyline.QueryToPoint(template);
				yield return template;
			}
			else
			{
				for (var i = 0; i < partCount; i++)
				{
					var path = (IPath) parts.Geometry[i];

					path.QueryFromPoint(template);
					yield return template;

					path.QueryToPoint(template);
					yield return template;
				}
			}
		}

		private static bool IntersectsVertex([NotNull] IPoint point,
		                                     [NotNull] IPolyline polyline,
		                                     [NotNull] IPoint pointTemplate,
		                                     double searchDistance)
		{
			double hitDistance = 0;
			int partIndex = -1;
			int segmentIndex = -1;
			var rightSide = false;

			var hitTest = (IHitTest) polyline;
			return hitTest.HitTest(point, searchDistance,
			                       esriGeometryHitPartType.esriGeometryPartVertex,
			                       pointTemplate,
			                       ref hitDistance, ref partIndex, ref segmentIndex,
			                       ref rightSide);
		}

		private static bool IntersectsEndPoint(
			[NotNull] IPoint point,
			[NotNull] IPolyline polyline,
			[NotNull] IPoint template)
		{
			var pointRelOp = (IRelationalOperator) point;

			foreach (IPoint endPoint in QueryEndpoints(polyline, template))
			{
				if (pointRelOp.Equals(endPoint))
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		private static IMultipoint RemoveAnyEndpoints(
			[NotNull] IMultipoint intersections,
			[NotNull] IGeometry shape1Endpoints,
			[NotNull] IGeometry shape2Endpoints)
		{
			var remainder = (IMultipoint)
				((ITopologicalOperator) intersections).Difference(shape1Endpoints);

			return remainder.IsEmpty
				       ? remainder
				       : (IMultipoint)
				       ((ITopologicalOperator) remainder).Difference(shape2Endpoints);
		}
	}
}

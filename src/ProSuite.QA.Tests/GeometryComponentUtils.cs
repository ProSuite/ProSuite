using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	public static class GeometryComponentUtils
	{
		[NotNull]
		public static string GetDisplayText(GeometryComponent geometryComponent)
		{
			switch (geometryComponent)
			{
				case GeometryComponent.EntireGeometry:
					return "entire geometry";

				case GeometryComponent.Boundary:
					return "boundary";

				case GeometryComponent.Vertices:
					return "vertices";

				case GeometryComponent.LineEndPoints:
					return "end points";

				case GeometryComponent.LineStartPoint:
					return "start point";

				case GeometryComponent.LineEndPoint:
					return "end point";

				case GeometryComponent.Centroid:
					return "centroid";

				case GeometryComponent.LabelPoint:
					return "label point";

				case GeometryComponent.InteriorVertices:
					return "interior vertices";

				default:
					throw new ArgumentOutOfRangeException(
						nameof(geometryComponent), geometryComponent,
						$@"Unsupported geometry component: {geometryComponent}");
			}
		}

		[CanBeNull]
		public static IGeometry GetGeometryComponent([NotNull] IReadOnlyFeature feature,
		                                             GeometryComponent component)
		{
			IGeometry shape = feature.Shape;

			return shape == null ? null : GetGeometryComponent(shape, component);
		}

		[CanBeNull]
		public static IGeometry GetGeometryComponent([NotNull] IGeometry shape,
		                                             GeometryComponent component)
		{
			switch (component)
			{
				case GeometryComponent.EntireGeometry:
					return shape;

				case GeometryComponent.Boundary:
					return GetBoundary(shape, component);

				case GeometryComponent.Vertices:
					return GetVertices(shape, component);

				case GeometryComponent.LineEndPoints:
					return GetLineEndPoints(shape, component);

				case GeometryComponent.LineStartPoint:
					return GetLineStartPoint(shape, component);

				case GeometryComponent.LineEndPoint:
					return GetLineEndPoint(shape, component);

				case GeometryComponent.Centroid:
					return GetCentroidPoint(shape, component);

				case GeometryComponent.LabelPoint:
					return GetLabelPoint(shape, component);

				case GeometryComponent.InteriorVertices:
					return GetInteriorVertices(shape, component);

				default:
					throw new ArgumentOutOfRangeException(
						$"Illegal geometry component: {component}");
			}
		}

		[CanBeNull]
		private static IGeometry GetCentroidPoint([NotNull] IGeometry shape,
		                                          GeometryComponent component)
		{
			var area = shape as IArea;
			Assert.NotNull(area, GetNotSupportedMessage(shape, component));

			return shape.IsEmpty
				       ? new PointClass {SpatialReference = shape.SpatialReference}
				       : area.Centroid;
		}

		[CanBeNull]
		private static IGeometry GetLabelPoint([NotNull] IGeometry shape,
		                                       GeometryComponent component)
		{
			var area = shape as IArea;
			Assert.NotNull(area, GetNotSupportedMessage(shape, component));

			return shape.IsEmpty
				       ? new PointClass {SpatialReference = shape.SpatialReference}
				       : area.LabelPoint;
		}

		[CanBeNull]
		private static IGeometry GetBoundary([NotNull] IGeometry shape,
		                                     GeometryComponent component)
		{
			var topoOp = shape as ITopologicalOperator;
			Assert.NotNull(topoOp, GetNotSupportedMessage(shape, component));

			// note: calling Boundary on an empty MultiPatch returns a NON-EMPTY polyline
			//       with 5 empty vertices and length = NaN

			return shape.IsEmpty
				       ? new PolylineClass {SpatialReference = shape.SpatialReference}
				       : topoOp.Boundary;
		}

		[CanBeNull]
		private static IGeometry GetVertices([NotNull] IGeometry shape,
		                                     GeometryComponent component)
		{
			switch (shape.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return GeometryFactory.Clone(shape);

				case esriGeometryType.esriGeometryMultipoint:
				case esriGeometryType.esriGeometryMultiPatch:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryPolyline:
					var points = (IPointCollection) GeometryFactory.Clone(shape);
					IMultipoint multipoint = GeometryFactory.CreateMultipoint(points);
					GeometryUtils.Simplify(multipoint);

					return multipoint;

				default:
					throw new ArgumentException(GetNotSupportedMessage(shape, component));
			}
		}

		[CanBeNull]
		private static IGeometry GetInteriorVertices([NotNull] IGeometry shape,
		                                             GeometryComponent component)
		{
			Assert.True(shape.GeometryType == esriGeometryType.esriGeometryPolyline,
			            GetNotSupportedMessage(shape, component));

			if (shape.IsEmpty)
			{
				return new MultipointClass {SpatialReference = shape.SpatialReference};
			}

			var points = (IPointCollection) GeometryFactory.Clone(shape);
			points.RemovePoints(points.PointCount - 1, 1);
			points.RemovePoints(0, 1);

			IMultipoint multipoint = GeometryFactory.CreateMultipoint(points);
			GeometryUtils.Simplify(multipoint);

			return multipoint;
		}

		[CanBeNull]
		private static IGeometry GetLineStartPoint([NotNull] IGeometry shape,
		                                           GeometryComponent component)
		{
			var polyCurve = shape as IPolycurve;
			Assert.NotNull(polyCurve, GetNotSupportedMessage(shape, component));

			return polyCurve.IsEmpty
				       ? new PointClass {SpatialReference = shape.SpatialReference}
				       : polyCurve.FromPoint;
		}

		[CanBeNull]
		private static IGeometry GetLineEndPoint([NotNull] IGeometry shape,
		                                         GeometryComponent component)
		{
			var polyCurve = shape as IPolycurve;
			Assert.NotNull(polyCurve, GetNotSupportedMessage(shape, component));

			return polyCurve.IsEmpty
				       ? new PointClass {SpatialReference = shape.SpatialReference}
				       : polyCurve.ToPoint;
		}

		[CanBeNull]
		private static IGeometry GetLineEndPoints([NotNull] IGeometry shape,
		                                          GeometryComponent component)
		{
			var polyCurve = shape as IPolycurve;
			Assert.NotNull(polyCurve, GetNotSupportedMessage(shape, component));

			return polyCurve.IsEmpty
				       ? new MultipointClass {SpatialReference = shape.SpatialReference}
				       : GeometryFactory.CreateMultipoint(
					       polyCurve.FromPoint, polyCurve.ToPoint);
		}

		[NotNull]
		private static string GetNotSupportedMessage([NotNull] IGeometry shape,
		                                             GeometryComponent component)
		{
			return string.Format("{0} not supported for geometry type {1}",
			                     component, shape.GeometryType);
		}
	}
}

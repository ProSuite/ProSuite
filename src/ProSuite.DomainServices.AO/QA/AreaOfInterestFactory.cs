using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.DomainServices.AO.QA
{
	public static class AreaOfInterestFactory
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static AreaOfInterest CreateAreaOfInterest(
			[NotNull] IFeatureClass featureClass,
			[NotNull] string featureSource,
			[CanBeNull] string whereClause,
			double generalizationTolerance,
			double? bufferDistance,
			[CanBeNull] IEnvelope processingExtent,
			[CanBeNull] string areaOfInterestDescription)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentCondition(generalizationTolerance >= 0,
			                         "Invalid generalization tolerance: {0}",
			                         generalizationTolerance);
			esriGeometryType shapeType = featureClass.ShapeType;
			Assert.ArgumentCondition(shapeType == esriGeometryType.esriGeometryPoint ||
			                         shapeType == esriGeometryType.esriGeometryMultipoint ||
			                         shapeType == esriGeometryType.esriGeometryPolyline ||
			                         shapeType == esriGeometryType.esriGeometryPolygon,
			                         "Unsupported shape type for area of interest feature layer: {0}",
			                         shapeType);
			if (shapeType != esriGeometryType.esriGeometryPolygon)
			{
				Assert.NotNull(bufferDistance,
				               "Buffer distance must be specified for shape type of area of interest feature class");
				Assert.True(bufferDistance.Value > 0,
				            "Buffer distance for area of interest must be greater than 0");
			}

			ISpatialReference spatialReference =
				DatasetUtils.GetSpatialReference(featureClass);
			IEnvelope projectedExtent = GetProjectedExtent(processingExtent, spatialReference);
			IQueryFilter queryFilter = GetQueryFilter(featureClass.ShapeFieldName,
			                                          whereClause,
			                                          projectedExtent);

			Stopwatch watch = _msg.DebugStartTiming("Reading AOI features from {0}",
			                                        DatasetUtils.GetName(featureClass));

			_msg.DebugFormat("- Where clause: '{0}'", whereClause);
			if (projectedExtent != null)
			{
				_msg.DebugFormat("- Features intersecting processing extent: {0}",
				                 GeometryUtils.Format(projectedExtent));
			}

			List<IGeometry> shapes =
				GdbQueryUtils.GetFeatures(featureClass, queryFilter, recycle: true)
				             .Select(feature => feature.ShapeCopy)
				             .ToList();

			_msg.DebugStopTiming(watch, "{0} AOI feature(s) read", shapes.Count);

			if (shapes.Count == 0)
			{
				_msg.WarnFormat("No AOI features found for where clause '{0}' in {1}",
				                whereClause,
				                DatasetUtils.GetName(featureClass));

				return new AreaOfInterest(new PolygonClass(),
				                          areaOfInterestDescription,
				                          featureSource,
				                          whereClause,
				                          bufferDistance ?? 0,
				                          generalizationTolerance,
				                          processingExtent);
			}

			watch = _msg.DebugStartTiming("Unioning aoi features");

			IGeometry union = GeometryUtils.Union(shapes);

			_msg.DebugStopTiming(watch, "Union created. Total vertex count: {0}",
			                     GeometryUtils.GetPointCount(union));

			var polyCurve = union as IPolycurve;
			if (polyCurve != null && generalizationTolerance > 0)
			{
				watch =
					_msg.DebugStartTiming(
						"Generalizing unioned AOI with tolerance {0}. Initial vertex count: {1}",
						generalizationTolerance,
						GeometryUtils.GetPointCount(polyCurve));

				polyCurve.Generalize(generalizationTolerance);

				_msg.DebugStopTiming(watch,
				                     "Unioned AOI generalized. Resulting vertex count: {0}",
				                     GeometryUtils.GetPointCount(polyCurve));
			}

			double xyTolerance = GeometryUtils.GetXyTolerance(featureClass);
			double densifyDeviation = Math.Max(xyTolerance, generalizationTolerance);

			// TODO test negative buffer distance
			IPolygon aoiPolygon = bufferDistance == null
				                      ? (IPolygon) union
				                      : CreateBuffer(new[] {union},
				                                     bufferDistance.Value,
				                                     densifyDeviation);

			if (projectedExtent != null)
			{
				watch = _msg.DebugStartTiming(
					"Clipping AOI polygon with processing extent. Initial vertex count: {0}",
					GeometryUtils.GetPointCount(aoiPolygon));

				((ITopologicalOperator) aoiPolygon).Clip(projectedExtent);

				_msg.DebugStopTiming(watch,
				                     "AOI polygon clipped. Resulting point count: {0}",
				                     GeometryUtils.GetPointCount(aoiPolygon));
			}

			if (generalizationTolerance > 0)
			{
				watch = _msg.DebugStartTiming(
					"Generalizing AOI polygon with tolerance {0}. Initial vertex count: {1}",
					generalizationTolerance,
					GeometryUtils.GetPointCount(aoiPolygon));

				aoiPolygon.Generalize(generalizationTolerance);

				_msg.DebugStopTiming(
					watch, "AOI polygon generalized. Resulting vertex count: {0}",
					GeometryUtils.GetPointCount(aoiPolygon));
			}

			GeometryUtils.AllowIndexing(aoiPolygon);

			return new AreaOfInterest(aoiPolygon,
			                          areaOfInterestDescription,
			                          featureSource,
			                          whereClause,
			                          bufferDistance ?? 0,
			                          generalizationTolerance,
			                          processingExtent);
		}

		[NotNull]
		public static IPolygon CreateBuffer([NotNull] ICollection<IGeometry> shapes,
		                                    double bufferDistance,
		                                    double densifyDeviation)
		{
			Assert.ArgumentNotNull(shapes, nameof(shapes));
			Assert.ArgumentCondition(shapes.Count > 0, "at least one input geometry expected");

			using (var bufferFactory = new BufferFactory
			                           {
				                           GenerateCurves = false,
				                           UnionOverlappingBuffers = true,
				                           ExplodeBuffers = false,
				                           DensifyDeviation = densifyDeviation
			                           })
			{
				Stopwatch watch =
					_msg.DebugStartTiming("Buffering by {0} with densify deviation {1}",
					                      bufferDistance, densifyDeviation);

				List<IPolygon> results = bufferFactory.Buffer(shapes, bufferDistance).ToList();
				Assert.AreEqual(1, results.Count, "Unexpected buffer result count");
				IPolygon result = results[0];

				_msg.DebugStopTiming(watch, "Buffer created with {0} vertices",
				                     GeometryUtils.GetPointCount(result));
				return result;
			}
		}

		[CanBeNull]
		private static IEnvelope GetProjectedExtent(
			[CanBeNull] IEnvelope processingExtent,
			[CanBeNull] ISpatialReference spatialReference)
		{
			if (processingExtent == null || processingExtent.IsEmpty)
			{
				return null;
			}

			IEnvelope result;
			GeometryUtils.EnsureSpatialReference(processingExtent,
			                                     spatialReference,
			                                     true,
			                                     out result);
			return result;
		}

		[NotNull]
		private static IQueryFilter GetQueryFilter([NotNull] string shapeFieldName,
		                                           [CanBeNull] string whereClause,
		                                           [CanBeNull] IEnvelope processingExtent)
		{
			IQueryFilter result;
			if (processingExtent != null)
			{
				result = new SpatialFilterClass
				         {
					         SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects,
					         GeometryField = shapeFieldName,
					         Geometry = processingExtent
				         };
			}
			else
			{
				result = new QueryFilterClass();
			}

			GdbQueryUtils.SetSubFields(result, shapeFieldName);

			if (StringUtils.IsNotEmpty(whereClause))
			{
				result.WhereClause = whereClause;
			}

			return result;
		}
	}
}

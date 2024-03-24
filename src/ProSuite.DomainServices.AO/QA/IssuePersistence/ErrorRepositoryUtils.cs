using System;
using System.Collections.Generic;
using System.Threading;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	public static class ErrorRepositoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private static readonly ThreadLocal<IEnvelope> _envelopeTemplate =
			new ThreadLocal<IEnvelope>(() => new EnvelopeClass());

		[CanBeNull]
		public static IGeometry GetGeometryToStore(
			[CanBeNull] IGeometry geometry,
			[CanBeNull] ISpatialReference spatialReference,
			[NotNull] ICollection<esriGeometryType> storedGeometryTypes,
			bool forPre10Geodatabase = false,
			bool forceSimplify = false)
		{
			Assert.ArgumentNotNull(storedGeometryTypes, nameof(storedGeometryTypes));

			if (geometry == null || geometry.IsEmpty)
			{
				return null;
			}

			Assert.ArgumentNotNull(spatialReference,
			                       $"{nameof(spatialReference)} is null. Either background verification inputs or known spatial reference must be specified");

			IGeometry result = CreateGeometryToStore(geometry, storedGeometryTypes);

			if (result == null)
			{
				return null;
			}

			const bool schemaHasZ = true;
			const bool schemaHasM = false;
			GeometryUtils.EnsureSchemaZM(result, schemaHasZ, schemaHasM);

			try
			{
				// Revert to useProjectEx: false due to
				// BUG: TOP-5806, TOP-5805, TOP-5804
				GeometryUtils.EnsureSpatialReference(result, spatialReference, useProjectEx: false);

				// this was previously needed for the exact coordinate comparison with allowed errors:
				result.SnapToSpatialReference();
				// NOTE: no effect observed for SnapToSpatialReference applied to lines with length < xy resolution!

				esriGeometryType geometryType = result.GeometryType;

				if (geometryType == esriGeometryType.esriGeometryPoint)
				{
					// points can always be used as is
					return result;
				}

				IEnvelope envelope = QueryEnvelope(result);

				if (IsPointEnvelope(envelope, spatialReference))
				{
					return CreateMultipointFromEnvelopeCentroid(
						envelope, ((IZAware) result).ZAware);
				}

				if (forceSimplify)
				{
					// simplify the issue geometry
					GeometryUtils.Simplify(result, allowReorder: false,
					                       allowPathSplitAtIntersections: true);

					if (result.IsEmpty)
					{
						// the geometry has become empty due to simplification
						// -> use center of original envelope (which was not empty)
						return CreateMultipointFromEnvelopeCentroid(envelope,
							((IZAware) result).ZAware);
					}
				}

				if (geometryType == esriGeometryType.esriGeometryPolygon &&
				    IsPolygonTooSmall((IPolygon) result, spatialReference))
				{
					return CreateApproximatePolygonGeometry((IPolygon) result, envelope);
				}

				// the XY geometry should be ok for storing; remove vertical segments if writing to pre-10.0 gdb
				if (forPre10Geodatabase)
				{
					if (geometryType == esriGeometryType.esriGeometryPolyline)
					{
						GeometryUtils.RemoveVerticalSegments((IPolyline) result);
					}
				}

				return result;
			}
			catch
			{
				_msg.Debug(GeometryUtils.ToString(result));
				throw;
			}
		}

		[NotNull]
		private static IGeometry CreateApproximatePolygonGeometry(
			[NotNull] IPolygon polygon, [NotNull] IEnvelope envelope)
		{
			IPolyline polyline = GeometryFactory.CreatePolyline((ISegmentCollection) polygon,
			                                                    doNotCloneInput: false);

			GeometryUtils.Simplify(polyline, allowReorder: false,
			                       allowPathSplitAtIntersections: true);

			if (! polyline.IsEmpty)
			{
				return polyline;
			}

			return CreateMultipointFromEnvelopeCentroid(envelope, ((IZAware) polygon).ZAware);
		}

		[NotNull]
		private static IGeometry CreateMultipointFromEnvelopeCentroid(
			[NotNull] IEnvelope envelope, bool zAware)
		{
			double xmin;
			double ymin;
			double xmax;
			double ymax;
			envelope.QueryCoords(out xmin, out ymin,
			                     out xmax, out ymax);

			IPoint point;
			if (zAware)
			{
				point = GeometryFactory.CreatePoint((xmax + xmin) / 2,
				                                    (ymax + ymin) / 2,
				                                    (envelope.ZMax + envelope.ZMin) / 2);
			}
			else
			{
				point = GeometryFactory.CreatePoint((xmax + xmin) / 2,
				                                    (ymax + ymin) / 2);
			}

			point.SpatialReference = envelope.SpatialReference;

			return GeometryFactory.CreateMultipoint(point);
		}

		[NotNull]
		private static IEnvelope QueryEnvelope([NotNull] IGeometry geometry)
		{
			geometry.QueryEnvelope(_envelopeTemplate.Value);

			return _envelopeTemplate.Value;
		}

		private static bool IsPolygonTooSmall([NotNull] IPolygon polygon,
		                                      [NotNull] ISpatialReference spatialReference)
		{
			double absArea = Math.Abs(((IArea) polygon).Area);

			if (absArea < double.Epsilon)
			{
				return true;
			}

			double xyTolerance = ((ISpatialReferenceTolerance) spatialReference).XYTolerance;

			double perimeter = polygon.Length;

			return IsPolygonTooSmall(perimeter, absArea, xyTolerance);
		}

		private static bool IsPolygonTooSmall(double perimeter, double area,
		                                      double xyTolerance)
		{
			if (perimeter < xyTolerance)
			{
				return true;
			}

			// the area of a hypothetical rectangle with that perimeter and the xy tolerance as height
			// -> width = perimeter / 2 - xyTolerance
			double minimumRectangularArea = (perimeter / 2 - xyTolerance) * xyTolerance;

			// if the observed area is smaller or equal to the area of that rectangle, then there's a chance
			// that it can not be written correctly
			return area <= minimumRectangularArea;
		}

		private static bool IsPointEnvelope([NotNull] IEnvelope envelope,
		                                    [NotNull] ISpatialReference spatialReference)
		{
			if (envelope.IsEmpty)
			{
				return false;
			}

			double width = envelope.Width;
			double height = envelope.Height;

			if (width < double.Epsilon &&
			    height < double.Epsilon)
			{
				return true;
			}

			double xyResolution = SpatialReferenceUtils.GetXyResolution(spatialReference);

			return width < xyResolution &&
			       height < xyResolution;
		}

		[CanBeNull]
		private static IGeometry CreateGeometryToStore(
			[NotNull] IGeometry geometry,
			[NotNull] ICollection<esriGeometryType> storedGeometryTypes)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(storedGeometryTypes, nameof(storedGeometryTypes));

			esriGeometryType geometryType = geometry.GeometryType;

			if (storedGeometryTypes.Contains(geometryType))
			{
				return GeometryFactory.Clone(geometry);
			}

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return storedGeometryTypes.Contains(esriGeometryType.esriGeometryMultipoint)
						       ? GeometryFactory.CreateMultipoint((IPoint) geometry)
						       : null;

				case esriGeometryType.esriGeometryMultiPatch:
					return storedGeometryTypes.Contains(esriGeometryType.esriGeometryPolygon)
						       ? GeometryFactory.CreatePolygon((IMultiPatch) geometry)
						       : null;

				case esriGeometryType.esriGeometryEnvelope:
					return storedGeometryTypes.Contains(esriGeometryType.esriGeometryPolygon)
						       ? GeometryFactory.CreatePolygon((IEnvelope) geometry)
						       : null;

				default:
					return null;
			}
		}
	}
}

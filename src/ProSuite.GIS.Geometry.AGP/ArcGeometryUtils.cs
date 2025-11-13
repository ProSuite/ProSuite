using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public static class ArcGeometryUtils
	{
		public static MapPoint CreateMapPoint(IPoint point, SpatialReference spatialReference)
		{
			bool hasID = false; // point.HasID;

			return MapPointBuilderEx.CreateMapPoint(
				point.X, point.Y, point.ZAware, point.Z, point.MAware, point.M,
				hasID, point.ID,
				spatialReference);
		}

		public static void QueryPoint(IPoint result,
		                              MapPoint mapPoint,
		                              ISpatialReference spatialReference = null)
		{
			result.X = mapPoint.X;
			result.Y = mapPoint.Y;
			result.Z = mapPoint.Z;
			result.M = mapPoint.M;
			result.ID = mapPoint.ID;

			result.SpatialReference = spatialReference ??
			                          (mapPoint.SpatialReference == null
				                           ? null
				                           : new ArcSpatialReference(mapPoint.SpatialReference));
		}

		public static ArcGIS.Core.Geometry.Geometry ToProGeometry(
			[NotNull] IGeometry geometry)
		{
			ArcGIS.Core.Geometry.Geometry result;

			if (geometry is ArcGeometry arcGeometry)
			{
				result = arcGeometry.ProGeometry;
			}
			else if (geometry is IMutableGeometry mutable)
			{
				result = (ArcGIS.Core.Geometry.Geometry) mutable.ToNativeImplementation();
			}
			else
			{
				result = TryConvertToProGeometry(geometry);
			}

			return result;
		}

		public static ArcGIS.Core.Geometry.Geometry TryConvertToProGeometry(
			[NotNull] IGeometry geometry)
		{
			ArcSpatialReference arcSpatialReference =
				geometry.SpatialReference as ArcSpatialReference;

			SpatialReference sr = arcSpatialReference?.ProSpatialReference;

			if (geometry is IPoint point)
			{
				return CreateMapPoint(point, sr);
			}

			if (geometry is IEnvelope envelope)
			{
				return CreateProEnvelope(envelope);
			}

			if (geometry is IPolyline polyline)
			{
				throw new NotImplementedException("Polyline is not yet supported");
			}

			throw new ArgumentOutOfRangeException("Unsupported geometry type");
		}

		public static Envelope CreateProEnvelope([NotNull] IEnvelope envelope)
		{
			SpatialReference sr = ((ArcSpatialReference) envelope.SpatialReference)
				.ProSpatialReference;

			return EnvelopeBuilderEx.CreateEnvelope(
				envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax, sr);
		}

		public static ISegment CreateSegment(Segment proSegment)
		{
			if (proSegment is LineSegment lineSegment)
			{
				return new ArcLineSegment(lineSegment);
			}

			if (proSegment is EllipticArcSegment ellipticArc)
			{
				return new ArcEllipticSegment(ellipticArc);
			}

			if (proSegment is CubicBezierSegment cubicSegment)
			{
				return new ArcBezierSegment(cubicSegment);
			}

			throw new NotSupportedException("Unsupported segment type");
		}
	}
}

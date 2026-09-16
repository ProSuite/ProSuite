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

		/// <summary>
		/// The 2D distance between the specified point and the closest point on the specified
		/// multipart geometry. See <see cref="ICurve.GetDistance2d"/>.
		/// </summary>
		public static double GetDistance2d([NotNull] Multipart multipart,
		                                   [NotNull] MapPoint toPoint,
		                                   out CurveLocation location)
		{
			ProximityResult proximity = GeometryEngine.Instance.NearestPoint(multipart, toPoint);

			MapPoint nearestPoint = proximity?.Point;

			if (nearestPoint == null || proximity.SegmentIndex == null)
			{
				location = CurveLocation.None;

				return proximity?.Distance ?? double.NaN;
			}

			int partIndex = proximity.PartIndex;
			int segmentInPartIndex = proximity.SegmentIndex.Value;

			Segment closestSegment = multipart.Parts[partIndex][segmentInPartIndex];

			location = new CurveLocation(
				GetGlobalSegmentIndex(multipart, partIndex, segmentInPartIndex),
				GetAlongSegmentRatio(closestSegment, toPoint, multipart.SpatialReference),
				new ArcPoint(nearestPoint));

			return proximity.Distance;
		}

		/// <summary>
		/// The 2D distance from the start of the specified multipart geometry to the specified
		/// location on it. See <see cref="ICurve.GetDistanceAlongCurve2d"/>.
		/// </summary>
		public static double GetDistanceAlongCurve2d([NotNull] Multipart multipart,
		                                             CurveLocation location)
		{
			double result = 0;
			var index = 0;

			foreach (ReadOnlySegmentCollection part in multipart.Parts)
			{
				foreach (Segment segment in part)
				{
					if (index == location.SegmentIndex)
					{
						return result + location.AlongSegmentRatio * segment.Length;
					}

					result += segment.Length;
					index++;
				}
			}

			throw new ArgumentOutOfRangeException(
				nameof(location),
				$"The geometry has {index} segments, hence the index {location.SegmentIndex} is out of range.");
		}

		private static int GetGlobalSegmentIndex([NotNull] Multipart multipart,
		                                         int partIndex,
		                                         int segmentInPartIndex)
		{
			int result = segmentInPartIndex;

			for (var i = 0; i < partIndex; i++)
			{
				result += multipart.Parts[i].Count;
			}

			return result;
		}

		private static double GetAlongSegmentRatio([NotNull] Segment segment,
		                                           [NotNull] MapPoint toPoint,
		                                           [CanBeNull] SpatialReference spatialReference)
		{
			// The engine determines a position within a curve only for high-level geometries
			Polyline segmentAsPolyline =
				PolylineBuilderEx.CreatePolyline(segment, spatialReference);

			GeometryEngine.Instance.QueryPointAndDistance(
				segmentAsPolyline, SegmentExtensionType.NoExtension, toPoint,
				AsRatioOrLength.AsRatio, out double alongSegmentRatio, out double _,
				out LeftOrRightSide _);

			return alongSegmentRatio;
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

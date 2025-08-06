using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public abstract class ArcPolycurve : ArcGeometry, IPolycurve
	{
		private readonly Multipart _proPolycurve;

		public ArcPolycurve(Multipart proPolyline) : base(proPolyline)
		{
			_proPolycurve = proPolyline;
		}

		#region Implementation of IPolyline

		public double Length => _proPolycurve.Length;

		public IPoint FromPoint
		{
			// No need for cloning because the geometries are immutable
			get
			{
				MapPoint proStartPoint = _proPolycurve.Points[0];
				return new ArcPoint(proStartPoint);
			}
			set => throw new NotImplementedException(
				       "Immutable geometry. Use other implementation.");
		}

		public void QueryFromPoint(IPoint result)
		{
			MapPoint fromPoint = _proPolycurve.Points[0];
			ArcGeometryUtils.QueryPoint(result, fromPoint);
		}

		public IPoint ToPoint
		{
			get
			{
				MapPoint endPoint = _proPolycurve.Points[_proPolycurve.PointCount - 1];
				return new ArcPoint(endPoint);
			}
			set => throw new NotImplementedException(
				       "Immutable geometry. Use other implementation.");
		}

		public void QueryToPoint(IPoint result)
		{
			MapPoint endPoint = _proPolycurve.Points[_proPolycurve.PointCount - 1];
			ArcGeometryUtils.QueryPoint(result, endPoint);
		}

		public ICurve GetSubcurve(double fromDistance, double toDistance)
		{
			throw new NotImplementedException();
		}

		public void ReverseOrientation()
		{
			throw new NotImplementedException();
		}

		public bool IsClosed => FromPoint.Equals(ToPoint);

		public IPoint GetPointAlong(double distanceAlong2d, bool asRatio)
		{
			throw new NotImplementedException();
		}

		public double GetDistancePerpendicular2d(IPoint ofPoint, out double distanceAlongRatio,
		                                         out IPoint pointOnLine)
		{
			var proPoint = (MapPoint) ofPoint.NativeImplementation;

			MapPoint nearestPoint = GeometryEngine.Instance.QueryPointAndDistance(
				_proPolycurve, SegmentExtensionType.NoExtension, proPoint, AsRatioOrLength.AsRatio,
				out distanceAlongRatio, out double distanceFromCurve, out LeftOrRightSide _);

			pointOnLine = nearestPoint != null ? new ArcPoint(nearestPoint) : null;

			return distanceFromCurve;
		}

		#endregion

		#region Implementation of IGeometryCollection

		public int GeometryCount => _proPolycurve.Parts.Count;

		public IGeometry get_Geometry(int index)
		{
			ReadOnlySegmentCollection segmentCollection = _proPolycurve.Parts[index];
			bool asRing = ProGeometry is Polygon;

			return new ArcPath(segmentCollection, asRing, SpatialReference);
		}

		public IEnumerable<KeyValuePair<int, ISegment>> FindSegments(
			double xMin, double yMin, double xMax, double yMax, double tolerance,
			bool allowIndexing = true, Predicate<int> predicate = null)
		{
			int index = 0;

			foreach (ReadOnlySegmentCollection segmentCollection in _proPolycurve.Parts)
			{
				foreach (ISegment segment in GetSegments(segmentCollection,
				                                         SpatialReference as ArcSpatialReference))
				{
					index++;

					if (! segment.ExtentIntersectsXY(xMin, yMin, xMax, yMax, tolerance))
					{
						continue;
					}

					if (predicate == null || predicate(index))
					{
						yield return new KeyValuePair<int, ISegment>(index, segment);
					}
				}
			}
		}

		public bool HasNonLinearSegments()
		{
			return _proPolycurve.HasCurves;
		}

		#endregion

		protected static IEnumerable<ISegment> GetSegments([NotNull] ReadOnlySegmentCollection part,
		                                                   [CanBeNull] ArcSpatialReference sr)
		{
			foreach (Segment segment in part)
			{
				if (segment is LineSegment lineSegment)
				{
					yield return new ArcLineSegment(lineSegment, sr);
				}
				else if (segment is EllipticArcSegment arcSegment)
				{
					yield return new ArcEllipticSegment(arcSegment, sr);
				}
				else if (segment is CubicBezierSegment bezierSegment)
				{
					yield return new ArcBezierSegment(bezierSegment, sr);
				}
			}
		}
	}
}

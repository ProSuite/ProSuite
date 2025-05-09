using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
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
			get => new ArcPoint(_proPolycurve.Points[0]);
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
			get => new ArcPoint(_proPolycurve.Points[_proPolycurve.PointCount - 1]);
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

			throw new NotImplementedException();
		}

		public IEnumerable<KeyValuePair<int, ISegment>> FindSegments(
			double xMin, double yMin, double xMax, double yMax, double tolerance,
			bool allowIndexing = true, Predicate<int> predicate = null)
		{
			throw new NotImplementedException();
		}

		public bool HasNonLinearSegments()
		{
			return _proPolycurve.HasCurves;
		}

		#endregion
	}
}

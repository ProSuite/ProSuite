using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP;

public class ArcPath : ArcGeometry, IPath
{
	private readonly ReadOnlySegmentCollection _segmentCollection;

	public ArcPath(ReadOnlySegmentCollection segmentCollection,
	               bool asRing,
	               ISpatialReference spatialReference) : base(
		GetPartGeometry(segmentCollection, asRing))
	{
		_segmentCollection = segmentCollection;
		SpatialReference = spatialReference;
	}

	private static ArcGIS.Core.Geometry.Geometry GetPartGeometry(
		ReadOnlySegmentCollection segmentCollection,
		bool asRing)
	{
		return asRing
			       ? PolygonBuilderEx.CreatePolygon(segmentCollection)
			       : PolylineBuilderEx.CreatePolyline(segmentCollection);
	}

	#region Implementation of ICurve

	public double Length => ProGeometry.Length;

	public IPoint FromPoint
	{
		get
		{
			MapPoint proStartPoint = _segmentCollection[0].StartPoint;
			return new ArcPoint(proStartPoint);
		}
		set => throw new NotImplementedException(
			       "Immutable geometry. Use other implementation.");
	}

	public void QueryFromPoint(IPoint result)
	{
		MapPoint fromPoint = _segmentCollection[0].StartPoint;
		ArcGeometryUtils.QueryPoint(result, fromPoint);
	}

	public IPoint ToPoint
	{
		get
		{
			MapPoint endPoint = _segmentCollection[_segmentCollection.Count - 1].EndPoint;
			return new ArcPoint(endPoint);
		}
		set => throw new NotImplementedException(
			       "Immutable geometry. Use other implementation.");
	}

	public void QueryToPoint(IPoint result)
	{
		MapPoint endPoint = _segmentCollection[_segmentCollection.Count - 1].EndPoint;
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

	public bool IsClosed => ProGeometry is Polygon;

	public IPoint GetPointAlong(double distanceAlong2d, bool asRatio)
	{
		throw new NotImplementedException();
	}

	public double GetDistancePerpendicular2d(IPoint ofPoint, out double distanceAlongRatio,
	                                         out IPoint pointOnLine)
	{
		var proPoint = (MapPoint) ofPoint.NativeImplementation;

		MapPoint nearestPoint = GeometryEngine.Instance.QueryPointAndDistance(
			(Multipart) ProGeometry, SegmentExtensionType.NoExtension, proPoint,
			AsRatioOrLength.AsRatio,
			out distanceAlongRatio, out double distanceFromCurve, out LeftOrRightSide _);

		pointOnLine = nearestPoint != null ? new ArcPoint(nearestPoint) : null;

		return distanceFromCurve;
	}

	#endregion

	#region Overrides of ArcGeometry

	public override IGeometry Clone()
	{
		Multipart clone = (Multipart) ProGeometry.Clone();

		ReadOnlySegmentCollection clonedSegments = clone.Parts[0];

		return new ArcPath(clonedSegments, clone is Polygon, SpatialReference);
	}

	#endregion

	#region Implementation of IPath

	public double GetDistanceAlong2D(int vertexIndex, int startVertexIndex = 0)
	{
		throw new NotImplementedException();
	}

	public bool HasNonLinearSegments()
	{
		Multipart multipart = (Multipart) ProGeometry;

		return multipart.HasCurves;
	}

	#endregion
}

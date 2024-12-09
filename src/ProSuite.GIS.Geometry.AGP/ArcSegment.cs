using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP;

public abstract class ArcSegment : ISegment
{
	public Segment ProSegment { get; }

	public ArcSpatialReference ArcSpatialReference { get; set; }

	public ArcSegment([NotNull] Segment proSegment,
	                  [CanBeNull] ArcSpatialReference arcSpatialReference)
	{
		ProSegment = proSegment;

		if (arcSpatialReference == null && proSegment.SpatialReference != null)
		{
			arcSpatialReference = new ArcSpatialReference(proSegment.SpatialReference);
		}

		ArcSpatialReference = arcSpatialReference;
	}

	public abstract esriGeometryType GeometryType { get; }

	public esriGeometryDimension Dimension => esriGeometryDimension.esriGeometry1Dimension;

	public ISpatialReference SpatialReference
	{
		get => ArcSpatialReference;
		set => throw new NotImplementedException();
	}

	public bool IsEmpty => ProSegment.StartPoint.IsEmpty && ProSegment.EndPoint.IsEmpty;

	public void SetEmpty()
	{
		throw new NotImplementedException();
	}

	public abstract void QueryEnvelope(IEnvelope outEnvelope);

	public abstract IEnvelope Envelope { get; }

	public void SnapToSpatialReference()
	{
		throw new NotImplementedException();
	}

	public abstract IGeometry Clone();

	#region Implementation of ISegment

	public double Length => ProSegment.Length;

	public IPoint FromPoint
	{
		get => new ArcPoint(ProSegment.StartPoint);
		set => throw new NotImplementedException();
	}

	public void QueryFromPoint(IPoint result)
	{
		MapPoint mapPoint = ProSegment.StartPoint;
		ArcGeometryUtils.QueryPoint(result, mapPoint);
	}

	public IPoint ToPoint
	{
		get => new ArcPoint(ProSegment.EndPoint);
		set => throw new NotImplementedException();
	}

	public void QueryToPoint(IPoint result)
	{
		MapPoint mapPoint = ProSegment.StartPoint;
		ArcGeometryUtils.QueryPoint(result, mapPoint);
	}

	public ICurve GetSubcurve(double fromDistance,
	                          double toDistance)
	{
		return GetSubcurve(fromDistance, toDistance, false);
	}

	public IPoint GetPointAlong(double distance, bool asRatio)
	{
		Multipart multipart =
			PolylineBuilderEx.CreatePolyline(ProSegment,
			                                 ArcSpatialReference.ProSpatialReference);
		MapPoint resultProPoint = GeometryEngine.Instance.MovePointAlongLine(
			multipart, distance, asRatio, 0, SegmentExtensionType.NoExtension);

		return new ArcPoint(resultProPoint);
	}

	public double GetDistanceAlong3D(IPoint ofPoint, bool asRatio)
	{
		throw new NotImplementedException();
	}

	public double GetDistanceAlong2D(IPoint ofPoint, bool asRatio)
	{
		Multipart multipart =
			PolylineBuilderEx.CreatePolyline(ProSegment,
			                                 ArcSpatialReference.ProSpatialReference);

		ArcSpatialReference asr = ofPoint.SpatialReference as ArcSpatialReference;

		MapPoint ofMapPoint =
			ArcGeometryUtils.CreateMapPoint(ofPoint, asr?.ProSpatialReference);

		AsRatioOrLength ratioOrLength =
			asRatio ? AsRatioOrLength.AsRatio : AsRatioOrLength.AsLength;

		GeometryEngine.Instance.QueryPointAndDistance(multipart,
		                                              SegmentExtensionType.NoExtension,
		                                              ofMapPoint, ratioOrLength,
		                                              out double result,
		                                              out double _,
		                                              out LeftOrRightSide whichSide);

		return result;
	}

	public double GetDistancePerpendicular3D(IPoint ofPoint,
	                                         out double distanceAlongRatio,
	                                         out IPoint pointOnLine)
	{
		throw new NotImplementedException();
	}

	public double GetDistancePerpendicular2d(IPoint ofPoint,
	                                         out double distanceAlongRatio,
	                                         out IPoint pointOnLine)
	{
		throw new NotImplementedException();
	}

	public void QueryTangent(double distanceAlongCurve, bool asRatio, double length,
	                         ILine tangent)
	{
		throw new NotImplementedException();
		//Multipart multipart =
		//	PolylineBuilderEx.CreatePolyline(Segment, ArcSpatialReference.ProSpatialReference);

		//GeometryEngine.Instance.QueryTangent(multipart, SegmentExtensionType.NoExtension, )
	}

	public ISegment GetSubcurve(double fromDistance, double toDistance, bool asRatio)
	{
		Multipart multipart =
			PolylineBuilderEx.CreatePolyline(ProSegment,
			                                 ArcSpatialReference.ProSpatialReference);

		var subcurve =
			GeometryEngine.Instance.GetSubCurve(multipart, fromDistance, toDistance,
			                                    asRatio
				                                    ? AsRatioOrLength.AsRatio
				                                    : AsRatioOrLength.AsLength);

		Segment resultSegment = subcurve.Parts[0][0];

		return ArcGeometryUtils.CreateSegment(resultSegment);
	}

	public void ReverseOrientation()
	{
		throw new NotImplementedException();
	}

	public bool IsClosed =>
		ProSegment.IsCurve && ProSegment.StartPoint.IsEqual(ProSegment.EndPoint);

	public abstract void QueryWksEnvelope(ref WKSEnvelope envelope);

	public void SplitAtDistance(double distanceAlong2D, bool asRatio,
	                            out ISegment fromSegment,
	                            out ISegment toSegment)
	{
		Multipart multipart =
			PolylineBuilderEx.CreatePolyline(ProSegment,
			                                 ArcSpatialReference.ProSpatialReference);

		MapPoint splitPoint = GeometryEngine.Instance.MovePointAlongLine(
			multipart, distanceAlong2D, asRatio, 0, SegmentExtensionType.NoExtension);

		Multipart result = GeometryEngine.Instance.SplitAtPoint(
			multipart, splitPoint, false, false, out bool splitOccurred, out _, out _);

		fromSegment = ArcGeometryUtils.CreateSegment(result.Parts[0][0]);
		toSegment = ArcGeometryUtils.CreateSegment(result.Parts[0][1]);
	}

	public bool IsVertical()
	{
		return Length <= SpatialReference.XYTolerance;
	}

	public bool ExtentIntersectsXY(
		double xMin, double yMin, double xMax, double yMax,
		double tolerance)
	{
		return ! GeomRelationUtils.AreBoundsDisjoint(Envelope.XMin, Envelope.YMin,
		                                             Envelope.XMax, Envelope.YMax,
		                                             xMin, yMin, xMax, yMax,
		                                             tolerance);
	}

	#endregion
}

using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP;

public abstract class ArcSegment : ISegment
{
	public Segment ProSegment { get; private set; }

	public ArcSpatialReference ArcSpatialReference { get; set; }

	private IEnvelope _envelope;

	private Polyline _highLevelSegment;

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

	private IEnvelope Envelope2D
	{
		get
		{
			if (_envelope == null)
			{
				_envelope = new ArcEnvelope(GetEnvelope());
			}

			return _envelope;
		}
	}

	private Polyline HighLevelSegment
	{
		get
		{
			if (_highLevelSegment == null)
			{
				_highLevelSegment =
					PolylineBuilderEx.CreatePolyline(ProSegment,
					                                 ArcSpatialReference.ProSpatialReference);
			}

			return _highLevelSegment;
		}
	}

	protected abstract Envelope GetEnvelope();

	#region Implementation of IGeometry

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

	public void QueryEnvelope(IEnvelope outEnvelope)
	{
		outEnvelope.XMin = Envelope2D.XMin;
		outEnvelope.XMax = Envelope2D.XMax;
		outEnvelope.YMin = Envelope2D.YMin;
		outEnvelope.YMax = Envelope2D.YMax;

		outEnvelope.SpatialReference = SpatialReference;
	}

	public IEnvelope Envelope => new ArcEnvelope(GetEnvelope());

	public IGeometry Project(ISpatialReference outputSpatialReference)
	{
		throw new NotImplementedException();
	}

	public void SnapToSpatialReference()
	{
		throw new NotImplementedException();
	}

	public abstract IGeometry Clone();

	public object NativeImplementation => ProSegment;

	#endregion

	#region Implementation of ISegment

	public double Length => ProSegment.Length;

	public IPoint FromPoint
	{
		get => new ArcPoint(ProSegment.StartPoint);
		set
		{
			SegmentBuilderEx segmentBuilder = SegmentBuilderEx.ConstructSegmentBuilder(ProSegment);

			if (segmentBuilder is EllipticArcBuilderEx arcBuilder)
			{
				// NOTE: For elliptic arcs we cannot change just the start point!
				segmentBuilder = ConstructEllipticArc(arcBuilder,
				                                      (MapPoint) value.NativeImplementation,
				                                      arcBuilder.EndPoint);
			}
			else
			{
				segmentBuilder.StartPoint = (MapPoint) value.NativeImplementation;
			}

			ReplaceProSegment(segmentBuilder);
		}
	}

	public IPoint ToPoint
	{
		get => new ArcPoint(ProSegment.EndPoint);
		set
		{
			SegmentBuilderEx segmentBuilder = SegmentBuilderEx.ConstructSegmentBuilder(ProSegment);

			if (segmentBuilder is EllipticArcBuilderEx arcBuilder)
			{
				segmentBuilder = ConstructEllipticArc(arcBuilder,
				                                      arcBuilder.StartPoint,
				                                      (MapPoint) value.NativeImplementation);
			}
			else
			{
				segmentBuilder.EndPoint = (MapPoint) value.NativeImplementation;
			}

			ReplaceProSegment(segmentBuilder);
		}
	}

	public bool IsClosed =>
		ProSegment.IsCurve && ProSegment.StartPoint.IsEqual(ProSegment.EndPoint);

	public void QueryFromPoint(IPoint result)
	{
		MapPoint mapPoint = ProSegment.StartPoint;
		ArcGeometryUtils.QueryPoint(result, mapPoint);
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
		MapPoint proPoint = (MapPoint) ofPoint.NativeImplementation;

		MapPoint nearestPoint = GeometryEngine.Instance.QueryPointAndDistance(
			HighLevelSegment,
			SegmentExtensionType.NoExtension,
			proPoint, AsRatioOrLength.AsRatio,
			out distanceAlongRatio,
			out double distanceFromCurve,
			out LeftOrRightSide _);

		pointOnLine = nearestPoint != null ? new ArcPoint(nearestPoint) : null;

		return distanceFromCurve;
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

	public void QueryWksEnvelope(ref WKSEnvelope result)
	{
		result.XMin = Envelope2D.XMin;
		result.XMax = Envelope2D.XMax;
		result.YMin = Envelope2D.YMin;
		result.YMax = Envelope2D.YMax;
	}

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

	private void ReplaceProSegment(SegmentBuilderEx segmentBuilder)
	{
		ProSegment = segmentBuilder.ToSegment();
		_envelope = null;
		_highLevelSegment = null;
	}

	/// <summary>
	/// Construct a new elliptic arc with the same center point / orientation but a new start/end point.
	/// </summary>
	/// <param name="template"></param>
	/// <param name="newStartPoint"></param>
	/// <param name="newEndPoint"></param>
	/// <returns></returns>
	private static SegmentBuilderEx ConstructEllipticArc(EllipticArcBuilderEx template,
	                                                     MapPoint newStartPoint,
	                                                     MapPoint newEndPoint)
	{
		var result = new EllipticArcBuilderEx(newStartPoint, newEndPoint,
		                                      template.CenterPoint, template.Orientation,
		                                      template.SpatialReference);

		return result;
	}
}

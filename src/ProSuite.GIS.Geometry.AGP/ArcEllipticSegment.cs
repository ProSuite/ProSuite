using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP;

public class ArcEllipticSegment : ArcSegment
{
	public EllipticArcSegment EllipticArc { get; }

	public ArcEllipticSegment(EllipticArcSegment ellipticArc,
	                          ArcSpatialReference arcSpatialReference = null)
		: base(ellipticArc, arcSpatialReference)
	{
		EllipticArc = ellipticArc;
	}

	#region Implementation of IGeometry

	public override esriGeometryType GeometryType => esriGeometryType.esriGeometryEllipticArc;

	public override void QueryEnvelope(IEnvelope outEnvelope)
	{
		Envelope envelope = EllipticArc.Get2DEnvelope();

		outEnvelope.XMin = envelope.XMin;
		outEnvelope.XMax = envelope.XMax;
		outEnvelope.YMin = envelope.YMin;
		outEnvelope.YMax = envelope.YMax;

		outEnvelope.SpatialReference = SpatialReference;
	}

	public override IEnvelope Envelope
	{
		get
		{
			var result = new ArcEnvelope(EllipticArc.Get2DEnvelope());

			QueryEnvelope(result);

			return result;
		}
	}

	public override IGeometry Clone()
	{
		EllipticArcSegment clone = EllipticArcBuilderEx.CreateEllipticArcSegment(EllipticArc);
		return new ArcEllipticSegment(clone, (ArcSpatialReference) SpatialReference);
	}

	#endregion

	#region Implementation of ISegment

	public override void QueryWksEnvelope(ref WKSEnvelope result)
	{
		Envelope envelope = EllipticArc.Get2DEnvelope();
		result.XMin = envelope.XMin;
		result.XMax = envelope.XMax;
		result.YMin = envelope.YMin;
		result.YMax = envelope.YMax;
	}

	#endregion
}

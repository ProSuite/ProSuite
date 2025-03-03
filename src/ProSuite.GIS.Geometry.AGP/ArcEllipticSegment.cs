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

	protected override Envelope GetEnvelope()
	{
		return EllipticArc.Get2DEnvelope();
	}

	public override esriGeometryType GeometryType => esriGeometryType.esriGeometryEllipticArc;

	public override IGeometry Clone()
	{
		EllipticArcSegment clone = EllipticArcBuilderEx.CreateEllipticArcSegment(EllipticArc);
		return new ArcEllipticSegment(clone, (ArcSpatialReference) SpatialReference);
	}
}

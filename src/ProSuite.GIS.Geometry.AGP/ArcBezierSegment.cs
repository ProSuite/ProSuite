using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP;

public class ArcBezierSegment : ArcSegment
{
	public CubicBezierSegment CubicBezierSegment { get; }

	public ArcBezierSegment(CubicBezierSegment cubicBezierSegment,
	                        ArcSpatialReference arcSpatialReference = null)
		: base(cubicBezierSegment, arcSpatialReference)
	{
		CubicBezierSegment = cubicBezierSegment;
	}

	public override esriGeometryType GeometryType => esriGeometryType.esriGeometryBezier3Curve;

	protected override Envelope GetEnvelope()
	{
		return CubicBezierSegment.Get2DEnvelope();
	}

	public override IGeometry Clone()
	{
		CubicBezierSegment clone =
			CubicBezierBuilderEx.CreateCubicBezierSegment(CubicBezierSegment);
		return new ArcBezierSegment(clone, (ArcSpatialReference) SpatialReference);
	}
}

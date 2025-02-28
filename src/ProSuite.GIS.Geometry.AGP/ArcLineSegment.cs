using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geometry.API;
using Envelope = ArcGIS.Core.Geometry.Envelope;

namespace ProSuite.GIS.Geometry.AGP;

public class ArcLineSegment : ArcSegment
{
	private readonly LineSegment _proLine;

	public ArcLineSegment([NotNull] LineSegment proLine,
	                      [CanBeNull] ArcSpatialReference arcSpatialReference = null)
		: base(proLine, arcSpatialReference)
	{
		_proLine = proLine;
	}

	protected override Envelope GetEnvelope()
	{
		return _proLine.Get2DEnvelope();
	}

	public override esriGeometryType GeometryType => esriGeometryType.esriGeometryEllipticArc;

	public override IGeometry Clone()
	{
		LineSegment clone = LineBuilderEx.CreateLineSegment(_proLine);
		return new ArcLineSegment(clone, ArcSpatialReference);
	}
}

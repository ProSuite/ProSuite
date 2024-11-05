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

	public override esriGeometryType GeometryType => esriGeometryType.esriGeometryEllipticArc;

	public override void QueryEnvelope(IEnvelope outEnvelope)
	{
		Envelope envelope = _proLine.Get2DEnvelope();

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
			var result = new ArcEnvelope(_proLine.Get2DEnvelope());

			QueryEnvelope(result);

			return result;
		}
	}

	public override IGeometry Clone()
	{
		LineSegment clone = LineBuilderEx.CreateLineSegment(_proLine);
		return new ArcLineSegment(clone, ArcSpatialReference);
	}

	public override void QueryWksEnvelope(ref WKSEnvelope result)
	{
		Envelope envelope = _proLine.Get2DEnvelope();
		result.XMin = envelope.XMin;
		result.XMax = envelope.XMax;
		result.YMin = envelope.YMin;
		result.YMax = envelope.YMax;
	}
}

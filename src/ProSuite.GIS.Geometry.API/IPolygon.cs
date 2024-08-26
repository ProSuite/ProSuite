namespace ESRI.ArcGIS.Geometry
{
	public interface IPolygon : IPolycurve
	{
		int ExteriorRingCount { get; }

		void SimplifyPreserveFromTo();

		double GetArea();
	}
}

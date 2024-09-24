namespace ProSuite.GIS.Geometry.API
{
	public interface IPolygon : IPolycurve
	{
		int ExteriorRingCount { get; }

		void SimplifyPreserveFromTo();

		double GetArea();
	}
}

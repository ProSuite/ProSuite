namespace ESRI.ArcGIS.Geometry
{
	public interface IPolygon : IGeometry
	{
		int ExteriorRingCount { get; }

		void SimplifyPreserveFromTo();
	}
}

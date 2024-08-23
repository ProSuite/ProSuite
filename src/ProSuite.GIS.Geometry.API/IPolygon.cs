namespace ESRI.ArcGIS.Geometry
{
	public interface IPolygon : IGeometry, IGeometryCollection
	{
		int ExteriorRingCount { get; }

		void SimplifyPreserveFromTo();

		double GetArea();
	}
}

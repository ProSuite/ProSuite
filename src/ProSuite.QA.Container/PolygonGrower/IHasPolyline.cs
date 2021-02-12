using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public interface IHasPolyline
	{
		IPolyline Polyline { get; }
	}
}

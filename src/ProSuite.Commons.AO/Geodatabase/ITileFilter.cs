using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface ITileFilter
	{
		IEnvelope TileExtent { get; set; }
	}
}

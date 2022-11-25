using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IFeatureClassFilter : ITableFilter
	{
		esriSpatialRelEnum SpatialRelationship { get; set; }
		IGeometry FilterGeometry { get; set; }
	}
}

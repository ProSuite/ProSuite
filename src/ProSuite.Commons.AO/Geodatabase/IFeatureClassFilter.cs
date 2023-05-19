using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Db;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IFeatureClassFilter : ITableFilter
	{
		esriSpatialRelEnum SpatialRelationship { get; set; }
		string SpatialRelDescription { get; set; }
		IGeometry FilterGeometry { get; set; }
	}
}

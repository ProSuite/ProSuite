using ProSuite.Commons.GeoDb;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IQueryFilter : ITableFilter
	{
		ISpatialReference get_OutputSpatialReference(string fieldName);

		void set_OutputSpatialReference(
			string fieldName,
			ISpatialReference outputSpatialReference);
	}

	public interface ISpatialFilter : IQueryFilter
	{
		esriSpatialRelEnum SpatialRel { get; set; }

		IGeometry Geometry { get; set; }

		string SpatialRelDescription { get; set; }
	}

	public enum esriSpatialRelEnum
	{
		esriSpatialRelUndefined,

		esriSpatialRelIntersects,

		esriSpatialRelEnvelopeIntersects,

		esriSpatialRelIndexIntersects,

		esriSpatialRelTouches,

		esriSpatialRelOverlaps,

		esriSpatialRelCrosses,

		esriSpatialRelWithin,

		esriSpatialRelContains,

		esriSpatialRelRelation,
	}
}

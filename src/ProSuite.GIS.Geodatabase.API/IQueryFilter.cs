using ESRI.ArcGIS.Geometry;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IQueryFilter
	{
		string SubFields { get; set; }

		void AddField(string subField);

		string WhereClause { get; set; }

		ISpatialReference get_OutputSpatialReference(string fieldName);

		void set_OutputSpatialReference(
			string fieldName,
			ISpatialReference outputSpatialReference);
	}
}

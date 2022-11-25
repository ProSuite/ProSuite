using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface ITableFilter
	{
		string SubFields { get; set; }
		string WhereClause { get; set; }

		object ToNativeFilterImpl(IFeatureClass featureClass = null);
	}
}

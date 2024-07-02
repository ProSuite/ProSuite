using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	extern alias EsriGeodatabase;

	internal static class ArcUtils
	{
		internal static IEnumerable<IRow> GetArcRows(
			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ICursor cursor)
		{
			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow row;
			while ((row = cursor.NextRow()) != null)
			{
				yield return ToArcObject(row);
			}
		}

		internal static ITable ToArcTable(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable aoTable)
		{
			var result = aoTable is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass
				? new ArcFeatureClass(featureClass)
				: (ITable)new ArcTable(aoTable);

			return result;
		}


		internal static IRow ToArcObject(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow aoRow)
		{
			var result = aoRow is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeature feature
				? (IRow)new ArcFeature(feature)
				: new ArcRow(aoRow);

			return result;
		}
	}
}

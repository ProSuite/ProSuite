using System.Collections.Generic;
using ArcGIS.Core.Data;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	internal static class ArcUtils
	{
		internal static IEnumerable<IRow> GetArcRows(
			RowCursor cursor)
		{
			Row row;
			while (cursor.MoveNext())
			{
				row = cursor.Current;

				yield return ToArcObject(row);
			}
		}

		internal static ITable ToArcTable(Table proTable)
		{
			var result = proTable is FeatureClass featureClass
				? new ArcFeatureClass(featureClass)
				: (ITable)new ArcTable(proTable);

			return result;
		}


		internal static IRow ToArcObject(Row proRow, ITable parent = null)
		{
			var result = proRow is Feature feature
				             ? (IRow) new ArcFeature(feature, parent as IFeatureClass)
				             : new ArcRow(proRow, parent);

			return result;
		}
	}
}

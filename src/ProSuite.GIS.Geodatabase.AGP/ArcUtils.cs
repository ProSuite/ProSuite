using System.Collections.Generic;
using ArcGIS.Core.Data;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	public static class ArcUtils
	{
		internal static IEnumerable<IRow> GetArcRows(
			RowCursor cursor)
		{
			Row row;
			while (cursor.MoveNext())
			{
				row = cursor.Current;

				yield return ToArcRow(row);
			}
		}

		public static ArcTable ToArcTable(Table proTable)
		{
			ArcTable result = proTable is FeatureClass featureClass
				                  ? new ArcFeatureClass(featureClass)
				                  : new ArcTable(proTable);

			return result;
		}

		internal static ArcRow ToArcRow(Row proRow, ITable parent = null)
		{
			return ArcRow.Create(proRow, parent);
		}
	}
}

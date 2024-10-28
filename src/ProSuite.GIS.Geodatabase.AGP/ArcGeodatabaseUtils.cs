using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public static class ArcGeodatabaseUtils
	{
		internal static IEnumerable<IRow> GetArcRows(
			RowCursor cursor, ITable sourceTable = null)
		{
			Row row;
			while (cursor.MoveNext())
			{
				row = cursor.Current;

				yield return ToArcRow(row, sourceTable);
			}
		}

		public static ArcTable ToArcTable(
			[NotNull] Table proTable)
		{
			Table databaseTable =
				Commons.AGP.Core.Geodatabase.DatasetUtils.GetDatabaseTable(proTable);

			ArcTable result = databaseTable is FeatureClass featureClass
				                  ? new ArcFeatureClass(featureClass)
				                  : new ArcTable(proTable);

			return result;
		}

		public static ArcRow ToArcRow([NotNull] Row proRow,
		                              [CanBeNull] ITable parent = null)
		{
			if (parent == null)
			{
				parent = ToArcTable(proRow.GetTable());
			}

			return ArcRow.Create(proRow, parent);
		}
	}
}

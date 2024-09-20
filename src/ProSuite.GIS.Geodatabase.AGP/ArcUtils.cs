using System.Collections.Generic;
using ArcGIS.Core.Data;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	public static class ArcUtils
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

		public static ArcTable ToArcTable([NotNull] Table proTable)
		{
			Table databaseTable = DatasetUtils.GetDatabaseTable(proTable);

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

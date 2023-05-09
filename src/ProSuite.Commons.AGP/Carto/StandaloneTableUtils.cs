using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class StandaloneTableUtils
	{
		public static bool HasSelection([CanBeNull] StandaloneTable table)
		{
			return table?.SelectionCount > 0;
		}

		public static IEnumerable<Row> GetSelectedRows([CanBeNull] StandaloneTable table)
		{
			ArcGIS.Core.Data.Selection selection = table?.GetSelection();

			if (selection == null)
			{
				yield break;
			}

			using (RowCursor cursor = selection.Search(null, false))
			{
				while (cursor.MoveNext())
				{
					yield return cursor.Current;
				}
			}
		}
	}
}

using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto;

public static class StandaloneTableUtils
{
	public static bool HasSelection([CanBeNull] StandaloneTable table)
	{
		return table?.SelectionCount > 0;
	}

	public static bool IsStandaloneTableValid([CanBeNull] StandaloneTable table)
	{
		// ReSharper disable once UseNullPropagation
		if (table == null)
		{
			return false;
		}

		if (table.GetTable() == null)
		{
			return false;
		}

		return true;
	}

	public static IEnumerable<Row> GetSelectedRows([CanBeNull] StandaloneTable table,
	                                               bool recycling = false)
	{
		ArcGIS.Core.Data.Selection selection = table?.GetSelection();

		if (selection == null)
		{
			yield break;
		}

		using (RowCursor cursor = selection.Search(null, recycling))
		{
			while (cursor.MoveNext())
			{
				yield return cursor.Current;
			}
		}
	}
}

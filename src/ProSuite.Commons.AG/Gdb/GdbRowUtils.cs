using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AG.Gdb
{
	[CLSCompliant(false)]
	public static class GdbRowUtils
	{
		public static Row GetRow([NotNull] Table table, long oid)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			string oidField = table.GetDefinition().GetObjectIDField();

			using (RowCursor cursor =
				table.Search(new QueryFilter {WhereClause = $"{oidField} = {oid}"}, false))
			{
				if (! cursor.MoveNext())
				{
					return null;
				}

				// todo daro: remove later when GetRow is used intensively throughout the solution
				Assert.False(cursor.MoveNext(), "more than one row found");

				return cursor.Current;
			}
		}

		public static IEnumerable<T> GetRows<T>([NotNull] Table table,
		                                        [CanBeNull] QueryFilter filter, bool recycle)
			where T : Row
		{
			Assert.ArgumentNotNull(table, nameof(table));

			using (RowCursor cursor = table.Search(filter, recycle))
			{
				while (cursor.MoveNext())
				{
					yield return (T) cursor.Current;
				}
			}
		}
	}
}

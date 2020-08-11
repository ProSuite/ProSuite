using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	[CLSCompliant(false)]
	public static class DatasetUtils
	{
		[NotNull]
		public static string GetTableDisplayName([NotNull] Table table)
		{
			TableDefinition definition = table.GetDefinition();
			string name = definition.GetName();
			string alias = definition.GetAliasName();

			if (! name.Equals(alias, StringComparison.CurrentCultureIgnoreCase))
			{
				return alias;
			}

			using (Datastore datastore = table.GetDatastore())
			{
				return GetTableName(datastore, alias);
			}
		}

		[NotNull]
		private static string GetTableName([NotNull] Datastore datastore,
		                                   [NotNull] string fullTableName)
		{
			string name;

			SQLSyntax sqlSyntax = datastore.GetSQLSyntax();
			if (sqlSyntax == null)
			{
				name = fullTableName;
			}
			else
			{
				Tuple<string, string, string> parsedTableName =
					sqlSyntax.ParseTableName(fullTableName);

				name = parsedTableName.Item3;
			}

			return name;
		}
	}
}

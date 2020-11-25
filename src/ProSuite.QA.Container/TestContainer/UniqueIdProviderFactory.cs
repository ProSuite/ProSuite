using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public static class UniqueIdProviderFactory
	{
		[CLSCompliant(false)]
		[CanBeNull]
		public static UniqueIdProvider Create([NotNull] ITable table)
		{
			var dataset = (IDataset) table;
			var queryName = dataset.FullName as IQueryName2;

			if (queryName?.PrimaryKey == null)
			{
				return null;
			}

			if (queryName.PrimaryKey.Contains(","))
			{
				return null;
			}

			Dictionary<int, ITable> baseTablePerOidFieldIndex =
				GetBaseTablePerOIDFieldIndex(table);

			return baseTablePerOidFieldIndex.Count < 2
				       ? null
				       : new UniqueIdProvider(baseTablePerOidFieldIndex);
		}

		[NotNull]
		private static Dictionary<int, ITable> GetBaseTablePerOIDFieldIndex(
			[NotNull] ITable table)
		{
			var workspace = (IFeatureWorkspace) DatasetUtils.GetWorkspace(table);

			var result = new Dictionary<int, ITable>();
			var tableDict = new Dictionary<string, ITable>();

			IFields fields = table.Fields;
			int fieldCount = fields.FieldCount;

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				IField field = fields.Field[fieldIndex];
				string fieldName = field.Name;

				int splitPos = fieldName.LastIndexOf('.');
				if (splitPos < 0)
				{
					continue;
				}

				string tableName = fieldName.Substring(0, splitPos);
				string unqualifiedFieldName = fieldName.Substring(splitPos + 1);

				ITable joinedTable;

				if (! tableDict.TryGetValue(tableName, out joinedTable))
				{
					string typedName = tableName;
					joinedTable = ExistsDataset(workspace, tableName)
						              ? workspace.OpenTable(typedName)
						              : null;

					tableDict.Add(tableName, joinedTable);
				}

				if (joinedTable == null)
				{
					continue;
				}

				if (joinedTable.HasOID &&
				    joinedTable.OIDFieldName.Equals(unqualifiedFieldName,
				                                    StringComparison.InvariantCultureIgnoreCase))
				{
					result.Add(fieldIndex, joinedTable);
				}
			}

			return result;
		}

		private static bool ExistsDataset([NotNull] IFeatureWorkspace workspace,
		                                  [NotNull] string tableName)
		{
			var workspace2 = (IWorkspace2) workspace;

			return workspace2.NameExists[esriDatasetType.esriDTTable, tableName] ||
			       workspace2.NameExists[esriDatasetType.esriDTFeatureClass, tableName];
		}
	}
}

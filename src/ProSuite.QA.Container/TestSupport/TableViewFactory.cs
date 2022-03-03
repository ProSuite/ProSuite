using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public static class TableViewFactory
	{
		private const char _tableSeparator = '.';

		public static MultiTableView Create(
			[NotNull] IList<ITable> tables,
			[NotNull] IList<string> tableAliases,
			[NotNull] string expression,
			bool caseSensitive = false,
			[CanBeNull] Action<Action<string, Type>, IList<ITable>> customizeDataTable = null,
			bool useAsConstraint = true)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentNotNull(tableAliases, nameof(tableAliases));
			Assert.ArgumentNotNull(expression, nameof(expression));

			if (tables.Count != tableAliases.Count)
			{
				throw new ArgumentException(
					string.Format("Equal number of tables({0}) and aliases({1}) expected",
					              tables.Count, tableAliases.Count));
			}

			// read/remove case sensitivity override from expression
			bool? caseSensitivityOverride;
			expression = ExpressionUtils.ParseCaseSensitivityHint(expression,
				out caseSensitivityOverride);

			if (caseSensitivityOverride != null)
			{
				caseSensitive = caseSensitivityOverride.Value;
			}

			List<string> upperCaseTableAliases = tableAliases.Select(alias => alias.ToUpper())
			                                                 .ToList();

			var tableAliasIndexes = new List<int>();

			var dataTable = new DataTable {CaseSensitive = caseSensitive};

			List<ColumnInfoFactory> factories =
				tables.Select(table => new ColumnInfoFactory(table)).ToList();

			var columnInfos = new List<ColumnInfo>();
			var addedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			const bool toUpper = true;
			foreach (string expressionToken in
				ExpressionUtils.GetExpressionTokens(expression, toUpper))
			{
				if (addedFields.Contains(expressionToken))
				{
					continue;
				}

				int tableAliasIndex;
				ColumnInfo columnInfo = TryGetColumnInfo(upperCaseTableAliases,
				                                         factories,
				                                         expressionToken,
				                                         out tableAliasIndex);
				if (columnInfo == null)
				{
					continue;
				}

				columnInfos.Add(columnInfo);

				// add column using fully qualified name
				dataTable.Columns.Add(expressionToken, columnInfo.ColumnType);
				tableAliasIndexes.Add(tableAliasIndex);

				addedFields.Add(expressionToken);
			}

			customizeDataTable?.Invoke(
				(columnName, type) => dataTable.Columns.Add(columnName, type),
				tables);

			var dataView = new DataView(dataTable);
			if (useAsConstraint)
			{
				dataView.RowFilter = expression;
			}

			return new MultiTableView(columnInfos, tableAliasIndexes, dataView);
		}

		[CanBeNull]
		private static ColumnInfo TryGetColumnInfo(
			[NotNull] IList<string> aliases,
			[NotNull] IList<ColumnInfoFactory> factories,
			[NotNull] string expressionToken,
			out int tableAliasIndex)
		{
			int tableSeparatorIndex = expressionToken.IndexOf(_tableSeparator, 0);

			tableAliasIndex = -1;

			if (tableSeparatorIndex < 0)
			{
				// there is no separator in the token --> ignore
				return null;
			}

			string tableNameOrAlias = expressionToken.Substring(0, tableSeparatorIndex);

			int aliasIndex = aliases.IndexOf(tableNameOrAlias);

			if (aliasIndex < 0)
			{
				return null;
			}

			// the qualified field name references one of the aliases ("L.FIELD1")

			string tokenWithoutQualifier =
				expressionToken.Substring(tableSeparatorIndex + 1);

			ColumnInfoFactory factory = factories[aliasIndex];

			ColumnInfo columnInfo = factory.GetColumnInfo(tokenWithoutQualifier);
			if (columnInfo == null)
			{
				return null;
			}

			tableAliasIndex = aliasIndex;
			return columnInfo;
		}

		[NotNull]
		public static TableView Create([NotNull] ITable table,
		                               [CanBeNull] string constraint)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return Create(table, constraint, useAsConstraint: true);
		}

		[NotNull]
		public static TableView Create([NotNull] ITable table,
		                               [NotNull] Dictionary<string, string> expressionDict,
		                               [NotNull] Dictionary<string, string> aliasFieldDict,
		                               bool isGrouped)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(expressionDict, nameof(expressionDict));
			Assert.ArgumentNotNull(aliasFieldDict, nameof(aliasFieldDict));

			string joined = string.Concat(expressionDict.Values.Select(x => $"{x} ")) +
			                string.Concat(aliasFieldDict.Values.Select(x => $"{x} "));

			Dictionary<string, string> fieldAliasDict =
				new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (KeyValuePair<string, string> pair in aliasFieldDict)
			{
				fieldAliasDict.Add(pair.Value, pair.Key);
			}

			TableView tv = Create(table, joined, aliasDict: fieldAliasDict, useAsConstraint: false);

			if (expressionDict.Count > 0)
			{
				tv.AddDummyRow();

				foreach (KeyValuePair<string, string> pair in expressionDict)
				{
					tv.AddExpressionColumn(pair.Key, pair.Value, isGrouped);
				}

				tv.ClearRows();
			}

			return tv;
		}

		[NotNull]
		public static TableView Create([NotNull] ITable table,
		                               [CanBeNull] string expression,
		                               bool useAsConstraint,
		                               bool caseSensitive = false)
			=> Create(table, expression, null, useAsConstraint, caseSensitive);

		private static TableView Create([NotNull] ITable table,
		                                [CanBeNull] string expression,
		                                [CanBeNull] Dictionary<string, string> aliasDict,
		                                bool useAsConstraint,
		                                bool caseSensitive = false)

		{
			Assert.ArgumentNotNull(table, nameof(table));

			if (string.IsNullOrWhiteSpace(expression))
			{
				// this filter helper won't do much
				return new TableView(new ColumnInfo[] { }, null);
			}

			// read/remove case sensitivity override from expression
			expression = ExpressionUtils.ParseCaseSensitivityHint(expression,
				out bool? caseSensitivityOverride);

			if (caseSensitivityOverride != null)
			{
				caseSensitive = caseSensitivityOverride.Value;
			}

			var columnInfos = new List<ColumnInfo>();
			var addedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			IFields fields = table.Fields;

			if (table.HasOID)
			{
				string oidFieldName = table.OIDFieldName.ToUpper();
				int fieldIndex = table.FindField(oidFieldName);
				IField field = fields.Field[fieldIndex];

				columnInfos.Add(new FieldColumnInfo(table, field, fieldIndex));
				addedFields.Add(oidFieldName);
			}

			var factory = new ColumnInfoFactory(table);

			foreach (string token in ExpressionUtils.GetExpressionTokens(expression))
			{
				if (addedFields.Contains(token))
				{
					continue;
				}

				ColumnInfo columnInfo = factory.GetColumnInfo(token);

				if (columnInfo != null)
				{
					columnInfos.Add(columnInfo);
					addedFields.Add(token);
				}
			}

			var dataTable = new DataTable(DatasetUtils.GetName(table))
			                {
				                CaseSensitive = caseSensitive
			                };

			foreach (ColumnInfo columnInfo in columnInfos)
			{
				string alias = null;
				if (aliasDict?.TryGetValue(columnInfo.ColumnName, out alias) != true)
				{
					alias = columnInfo.ColumnName;
				}

				Type dataColumnType = columnInfo.ColumnType != typeof(Guid)
					                      ? columnInfo.ColumnType
					                      : typeof(object);
				dataTable.Columns.Add(alias, dataColumnType);
			}

			var dataView = new DataView(dataTable);
			if (useAsConstraint)
			{
				dataView.RowFilter = expression;
			}

			return new TableView(columnInfos, dataView);
		}
	}
}

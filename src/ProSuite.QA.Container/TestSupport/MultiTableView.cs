using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class MultiTableView : TableViewBase, IColumnNames
	{
		[NotNull] private readonly IList<int> _tableAliasIndexes;

		internal MultiTableView([NotNull] IEnumerable<ColumnInfo> columnInfos,
		                        [NotNull] IList<int> tableAliasIndexes,
		                        [CanBeNull] DataView constraintView)
			: base(columnInfos, constraintView)
		{
			Assert.ArgumentNotNull(tableAliasIndexes, nameof(tableAliasIndexes));

			_tableAliasIndexes = tableAliasIndexes;
		}

		public bool MatchesConstraint(params IReadOnlyRow[] rows)
		{
			return MatchesConstraint(null, rows);
		}

		public bool MatchesConstraint(
			[CanBeNull] IDictionary<string, object> overridingFieldValues,
			params IReadOnlyRow[] rows)
		{
			DataView view = ConstraintView;
			if (view == null)
			{
				return true;
			}

			ClearRows();
			DataRow row = Add(rows);

			if (row != null && overridingFieldValues != null)
			{
				WriteFieldValues(row, overridingFieldValues);
			}

			return view.Count == 1;
		}

		private static void WriteFieldValues(
			[NotNull] DataRow row,
			[NotNull] IDictionary<string, object> fieldValues)
		{
			foreach (KeyValuePair<string, object> pair in fieldValues)
			{
				row[pair.Key] = pair.Value ?? DBNull.Value;
			}
		}

		public List<string> GetInvolvedColumnNames(
			bool showWorkspaceName = false,
			bool showWorkspaceNameIfDiffers = false,
			bool showTableName = false,
			bool showTableNameIfDiffers = false,
			bool showTableAliasName = false,
			bool showTableAliasNameIfDiffers = false)
		{
			var result = new List<string>();

			if (ConstraintView == null)
			{
				return result;
			}

			var workspaceNamesDiffer = false;
			var tableNamesDiffer = false;
			var aliasNameDiffers = false;
			var columnIndex = 0;

			if (showWorkspaceNameIfDiffers ||
			    showTableNameIfDiffers ||
			    showTableAliasNameIfDiffers)
			{
				IWorkspace commonWorkspace = null;
				IReadOnlyDataset commonDataset = null;
				string commonAlias = null;
				foreach (ColumnInfo columnInfo in ColumnInfos)
				{
					var dataset = columnInfo.Table;

					if (commonWorkspace == null || commonWorkspace == dataset.Workspace)
					{
						commonWorkspace = dataset.Workspace;
					}
					else
					{
						workspaceNamesDiffer = true;
					}

					if (commonDataset == null || commonDataset.Equals(dataset))
					{
						commonDataset = dataset;
					}
					else
					{
						tableNamesDiffer = true;
					}

					string alias = GetDataTableColumnName(columnIndex);

					int l = alias.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);

					if (l > 0)
					{
						alias = alias.Substring(0, l);
					}

					if (commonAlias == null || alias.ToUpper() == commonAlias)
					{
						commonAlias = alias.ToUpper();
					}
					else
					{
						aliasNameDiffers = true;
					}

					columnIndex++;
				}
			}

			if (! showWorkspaceNameIfDiffers)
			{
				workspaceNamesDiffer = false;
			}

			if (! showTableAliasName)
			{
				tableNamesDiffer = false;
			}

			if (! showTableAliasNameIfDiffers)
			{
				aliasNameDiffers = false;
			}

			columnIndex = 0;
			foreach (ColumnInfo columnInfo in ColumnInfos)
			{
				var name = new StringBuilder();
				if (showWorkspaceName || workspaceNamesDiffer)
				{
					name.AppendFormat("{0}.", columnInfo.Table.Workspace.PathName);
				}

				if (showTableName || tableNamesDiffer)
				{
					name.AppendFormat("{0}.", columnInfo.Table.Name);
				}

				if (showTableAliasName || aliasNameDiffers)
				{
					name.Append(GetDataTableColumnName(columnIndex));
				}
				else
				{
					name.Append(columnInfo.ColumnName);
				}

				result.Add(name.ToString());

				columnIndex++;
			}

			return result;
		}

		public string ToString(params IReadOnlyRow[] rows)
		{
			return ToString(false, rows);
		}

		public string ToString(bool concise, params IReadOnlyRow[] rows)
		{
			// TODO assertions? rows order matching table order?

			if (ConstraintView == null)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();
			CreateDataRow(ConstraintView, rows);

			string separator = concise ? ";" : "; ";
			string comparisonFormat = concise ? "{0}:{1}" : "{0} = {1}";

			var columnIndex = 0;
			foreach (ColumnInfo columnInfo in ColumnInfos)
			{
				if (sb.Length > 0)
				{
					sb.Append(separator);
				}

				IReadOnlyRow row = GetRowForColumn(rows, columnIndex);

				sb.AppendFormat(comparisonFormat,
				                GetDataTableColumnName(columnIndex), columnInfo.FormatValue(row));
				columnIndex++;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Adds the specified rows.
		/// </summary>
		/// <param name="rows">The collection of rows. The order must match that of the tables/aliases collections.</param>
		/// <returns></returns>
		[CanBeNull]
		public DataRow Add(params IReadOnlyRow[] rows)
		{
			// TODO assertions? matching table order?

			if (ConstraintView == null)
			{
				return null;
			}

			DataTable dataTable = ConstraintView.Table;

			DataRow dataRow = CreateDataRow(ConstraintView, rows);

			dataTable.Rows.Add(dataRow);

			return dataRow;
		}

		[NotNull]
		private DataRow CreateDataRow([NotNull] DataView constraintView,
		                              [NotNull] IList<IReadOnlyRow> rows)
		{
			DataTable dataTable = constraintView.Table;

			DataRow result = dataTable.NewRow();

			var columnIndex = 0;
			foreach (ColumnInfo columnInfo in ColumnInfos)
			{
				IReadOnlyRow row = GetRowForColumn(rows, columnIndex);

				Assert.AreEqual(row.Table, columnInfo.Table,
				                "row order does not match table alias order");

				result[columnIndex] = columnInfo.ReadValue(row);

				columnIndex++;
			}

			return result;
		}

		[NotNull]
		private IReadOnlyRow GetRowForColumn([NotNull] IList<IReadOnlyRow> rows, int columnIndex)
		{
			// the rows MUST be in the same order as the table alias list

			int tableAliasIndex = _tableAliasIndexes[columnIndex];
			IReadOnlyRow row = rows[tableAliasIndex];

			return row;
		}
	}
}

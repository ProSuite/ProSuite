using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class TableView : TableViewBase
	{
		internal TableView(
			[NotNull] IEnumerable<ColumnInfo> columnInfos,
			[CanBeNull] DataView constraintView)
			: base(columnInfos, constraintView) { }

		[NotNull]
		public TableView Clone()
		{
			// new view for same table --> case sensitivity is maintained in clone
			DataView dataView = ConstraintView == null
				                    ? null
				                    : new DataView(ConstraintView.Table)
				                      {
					                      RowFilter = ConstraintView.RowFilter,
					                      Sort = ConstraintView.Sort
				                      };

			return new TableView(ColumnInfos, dataView);
		}

		[CanBeNull]
		public DataRow Add([NotNull] IRow row,
		                   [CanBeNull] ICollection<ColumnInfo> columnInfos = null)
		{
			if (ConstraintView == null)
			{
				return null;
			}

			columnInfos = columnInfos ?? ColumnInfos;

			DataTable dataTable = ConstraintView.Table;
			DataRow dataRow = CreateDataRow(row, columnInfos);
			Assert.NotNull(dataRow, "dataRow");

			dataTable.Rows.Add(dataRow);

			return dataRow;
		}

		public bool MatchesConstraint([NotNull] IRow row)
		{
			DataView dataView = ConstraintView;

			if (dataView == null)
			{
				return true;
			}

			ClearRows();
			Add(row);

			return dataView.Count == 1;
		}

		[NotNull]
		public string ToString([NotNull] IRow row, bool constraintOnly = false)
		{
			HashSet<string> fieldNames = constraintOnly
				                             ? new HashSet<string>(
					                             StringComparer.OrdinalIgnoreCase)
				                             : null;

			return ToString(row, constraintOnly, fieldNames);
		}

		[NotNull]
		public string ToString([NotNull] IRow row,
		                       bool constraintOnly,
		                       [CanBeNull] ICollection<string> addedConstraintFieldNames)
		{
			DataView dataView = ConstraintView;

			if (dataView == null)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();
			CreateDataRow(row, ColumnInfos);
			DataTable dataTable = dataView.Table;

			if (constraintOnly)
			{
				foreach (string fieldNameCandidate in
					ExpressionUtils.GetExpressionTokens(dataView.RowFilter))
				{
					int columnIndex = dataTable.Columns.IndexOf(fieldNameCandidate);

					if (columnIndex < 0 || columnIndex >= ColumnInfos.Count)
					{
						continue;
					}

					// the candidate matches an existing column

					if (addedConstraintFieldNames != null)
					{
						if (addedConstraintFieldNames.Contains(fieldNameCandidate))
						{
							continue;
						}

						addedConstraintFieldNames.Add(fieldNameCandidate);
					}

					if (sb.Length > 0)
					{
						sb.Append("; ");
					}

					ColumnInfo columnInfo = GetColumnInfo(columnIndex);
					sb.AppendFormat("{0} = {1}",
					                GetDataTableColumnName(columnIndex),
					                columnInfo.FormatValue(row));
				}
			}
			else
			{
				var columnIndex = 0;
				foreach (ColumnInfo columnInfo in ColumnInfos)
				{
					if (sb.Length > 0)
					{
						sb.Append("; ");
					}

					sb.AppendFormat("{0} = {1}",
					                GetDataTableColumnName(columnIndex),
					                columnInfo.FormatValue(row));
					columnIndex++;
				}
			}

			return sb.ToString();
		}

		[CanBeNull]
		private DataRow CreateDataRow([NotNull] IRow row,
		                              [NotNull] IEnumerable<ColumnInfo> columnInfos)
		{
			if (ConstraintView == null)
			{
				return null;
			}

			DataTable table = ConstraintView.Table;
			DataRow dataRow = table.NewRow();

			var index = 0;
			foreach (ColumnInfo columnInfo in columnInfos)
			{
				if (columnInfo != null)
				{
					dataRow[index] = columnInfo.ReadValue(row);
				}

				index++;
			}

			return dataRow;
		}
	}
}

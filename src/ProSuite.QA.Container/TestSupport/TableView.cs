using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ProSuite.Commons.AO.Geodatabase;
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
		public DataRow Add([NotNull] IReadOnlyRow row,
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

		internal void AddDummyRow()
		{
			if (ConstraintView == null)
			{
				return;
			}

			DataRow r = ConstraintView.Table.NewRow();
			foreach (DataColumn col in ConstraintView.Table.Columns)
			{
				if (! string.IsNullOrEmpty(col.Expression))
				{
					continue;
				}

				if (col.DataType == typeof(short))
					r[col] = 1;
				else if (col.DataType == typeof(int))
					r[col] = 1;
				else if (col.DataType == typeof(float))
					r[col] = 0.1f;
				else if (col.DataType == typeof(double))
					r[col] = 0.1;
				else if (col.DataType == typeof(string))
					r[col] = "a";
				else if (col.DataType == typeof(bool))
					r[col] = true;
				else if (col.DataType == typeof(DateTime))
					r[col] = DateTime.Now;
				else if (col.DataType == typeof(Guid))
					r[col] = Guid.NewGuid();
				else if (col.DataType == typeof(object))
					r[col] = new object();
				else
					throw new NotImplementedException();
			}

			ConstraintView.Table.Rows.Add(r);
		}

		[CanBeNull]
		public DataColumn AddExpressionColumn([NotNull] string columnName,
		                                      [NotNull] string expression,
		                                      bool isGroupExpression)
		{
			if (ConstraintView == null)
			{
				return null;
			}

			Type columnType;
			if (isGroupExpression)
			{
				try
				{
					object dummy = ConstraintView.Table.Compute(expression, null);
					columnType = dummy.GetType();
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException($"Invalid aggregate '{expression}'",
					                                    exception);
				}
			}
			else
			{
				DataView v = new DataView(ConstraintView.Table);
				v.RowFilter = "false"; // ensure v.Count == 0
				try
				{
					v.RowFilter = $"({expression} < 0) OR ({expression} >= 0)";
				}
				catch { }

				if (v.Count == v.Table.Rows.Count)
				{
					columnType = typeof(double);
				}
				else
				{
					v.RowFilter = "false";
					try
					{
						v.RowFilter = $"({expression} = true) OR ({expression} = false)";
					}
					catch { }

					if (v.Count == v.Table.Rows.Count)
					{
						columnType = typeof(bool);
					}
					else
					{
						columnType = typeof(string);
					}
				}
			}

			if (columnName.Equals(expression.Trim(), StringComparison.InvariantCultureIgnoreCase))
			{
				return ConstraintView.Table.Columns[columnName];
			}

			DataColumn added =
				ConstraintView.Table.Columns.Add(columnName, columnType, expression);

			return added;
		}

		public bool MatchesConstraint([NotNull] IReadOnlyRow row)
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
		public string ToString([NotNull] IReadOnlyRow row, bool constraintOnly = false)
		{
			HashSet<string> fieldNames = constraintOnly
				                             ? new HashSet<string>(
					                             StringComparer.OrdinalIgnoreCase)
				                             : null;

			return ToString(row, constraintOnly, fieldNames);
		}

		[NotNull]
		public string ToString([NotNull] IReadOnlyRow row,
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
		private DataRow CreateDataRow([NotNull] IReadOnlyRow row,
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

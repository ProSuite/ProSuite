using System;
using System.Collections.Generic;
using System.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Container.TestSupport
{
	public abstract class TableViewBase
	{
		[NotNull] private readonly List<ColumnInfo> _columnInfos;
		[CanBeNull] private string _subFields; // always access via property

		/// <summary>
		/// Initializes a new instance of the <see cref="TableViewBase"/> class.
		/// </summary>
		/// <param name="columnInfos"></param>
		/// <param name="constraintView">The constraint view.</param>
		[CLSCompliant(false)]
		protected TableViewBase([NotNull] IEnumerable<ColumnInfo> columnInfos,
		                        [CanBeNull] DataView constraintView)
		{
			Assert.ArgumentNotNull(columnInfos, nameof(columnInfos));

			_columnInfos = new List<ColumnInfo>(columnInfos);
			ConstraintView = constraintView;
		}

		[NotNull]
		public string SubFields => _subFields ?? (_subFields = GetSubfields(_columnInfos));

		[CanBeNull]
		protected DataView ConstraintView { get; }

		public bool CaseSensitive
		{
			get { return ConstraintView != null && ConstraintView.Table.CaseSensitive; }
			set
			{
				DataView view = Assert.NotNull(ConstraintView, "constraint view not initialized");

				view.Table.CaseSensitive = value;
			}
		}

		[CanBeNull]
		public string Constraint
		{
			get
			{
				return ConstraintView == null
					       ? string.Empty
					       : ConstraintView.RowFilter;
			}
			set
			{
				DataView view = Assert.NotNull(ConstraintView, "constraint view not initialized");

				if (value != null)
				{
					// parse/remove/use case sensitivity hint
					bool? caseSensitivityOverride;
					value = ExpressionUtils.ParseCaseSensitivityHint(value,
					                                                 out caseSensitivityOverride);
					if (caseSensitivityOverride != null)
					{
						view.Table.CaseSensitive = caseSensitivityOverride.Value;
					}
				}

				view.RowFilter = value;
			}
		}

		public int FilteredRowCount => ConstraintView?.Count ?? 0;

		[NotNull]
		public IEnumerable<DataRowView> GetFilteredRows()
		{
			if (ConstraintView == null)
			{
				yield break;
			}

			foreach (DataRowView dataRowView in ConstraintView)
			{
				yield return dataRowView;
			}
		}

		public DataColumn AddColumn([NotNull] string columnName,
		                            [NotNull] Type type)
		{
			DataView view = Assert.NotNull(ConstraintView, "constraint view not initialized");

			return view.Table.Columns.Add(columnName, type);
		}

		public void ClearRows()
		{
			if (ConstraintView == null)
			{
				return;
			}

			DataTable table = ConstraintView.Table;
			table.Clear();
		}

		public int ColumnCount => ConstraintView?.Table.Columns.Count ?? 0;

		public int GetColumnIndex([NotNull] string columnName)
		{
			if (ConstraintView == null)
			{
				return -1;
			}

			return ConstraintView.Table.Columns.IndexOf(columnName);
		}

		public string GetColumnName(int columnIndex)
		{
			return ConstraintView?.Table.Columns[columnIndex].ColumnName;
		}

		public void Sort([CanBeNull] string expression)
		{
			DataView view = Assert.NotNull(ConstraintView, "constraint view not initialized");

			view.Sort = expression;
		}

		[NotNull]
		[CLSCompliant(false)]
		protected ICollection<ColumnInfo> ColumnInfos => _columnInfos;

		[NotNull]
		[CLSCompliant(false)]
		protected ColumnInfo GetColumnInfo(int columnIndex)
		{
			return _columnInfos[columnIndex];
		}

		[NotNull]
		protected string GetDataTableColumnName(int columnIndex)
		{
			DataView view = Assert.NotNull(ConstraintView, "constraint view is null");

			return view.Table.Columns[columnIndex].ColumnName;
		}

		[NotNull]
		private static string GetSubfields([NotNull] IEnumerable<ColumnInfo> columnInfos)
		{
			var fieldNames = new List<string>();
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (ColumnInfo columnInfo in columnInfos)
			{
				foreach (string fieldName in columnInfo.BaseFieldNames)
				{
					if (set.Add(fieldName))
					{
						fieldNames.Add(fieldName);
					}
				}
			}

			return StringUtils.Concatenate(fieldNames, ",");
		}
	}
}
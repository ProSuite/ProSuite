using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Helper for working with a <see cref="DataGridView"/> that is bound to
	/// objects of a given type.
	/// </summary>
	/// <typeparam name="ROW">The type of the bound rows.</typeparam>
	public class BoundDataGridHandler<ROW> : IDisposable where ROW : class
	{
		[NotNull] private readonly DataGridView _dataGridView;
		[CanBeNull] private BoundDataGridSelectionState<ROW> _savedSelection;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundDataGridHandler{ROW}"/> class.
		/// </summary>
		/// <param name="dataGridView">The data grid view.</param>
		public BoundDataGridHandler([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			_dataGridView = dataGridView;
			_dataGridView.SelectionChanged += _dataGridView_SelectionChanged;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundDataGridHandler{ROW}"/> class.
		/// </summary>
		/// <param name="dataGridView">The data grid view (must be sort-aware).</param>
		/// <param name="restoreSelectionAfterUserSort">Indicates if the grid handler restores the selection after interactive sorts.
		/// <b>Important:</b> if this option is selected, then the <see cref="SelectionChanged"/> event should be consumed from this class, and not
		/// the underlying <see cref="DataGridView"/>. This class filters out events caused by the restoring of the selection. An alternative is
		/// to ignore events raised by the <see cref="DataGridView"/> if <see cref="HasPendingUserSort"/> is <c>true</c>.</param>
		public BoundDataGridHandler([NotNull] SortAwareDataGridView dataGridView,
		                            bool restoreSelectionAfterUserSort = false)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			_dataGridView = dataGridView;

			if (restoreSelectionAfterUserSort)
			{
				dataGridView.SortingColumn += _dataGridView_SortingColumn;
				dataGridView.Sorted += _dataGridView_Sorted;
			}

			_dataGridView.SelectionChanged += _dataGridView_SelectionChanged;
		}

		#endregion

		public event EventHandler SelectionChanged;

		[NotNull]
		public DataGridView DataGridView => _dataGridView;

		/// <summary>
		/// Binds a data grid view to the given binding list of rows
		/// </summary>
		/// <param name="rows">The rows.</param>
		/// <param name="autoGenerateColumns">Indicates if columns should be dynamically added based on the bound type</param>
		/// <param name="sortStateOverride">Optional sort state to apply regardless of the current sort state of the grid.</param>
		/// <param name="defaultSortState">Optional default sort state, to be applied when there is no current or override sort state.</param>
		/// <param name="selectionStateOverride"></param>
		public bool BindTo([NotNull] IList<ROW> rows,
		                   bool autoGenerateColumns = false,
		                   DataGridViewSortState sortStateOverride = null,
		                   DataGridViewSortState defaultSortState = null,
		                   BoundDataGridSelectionState<ROW> selectionStateOverride = null)
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			return DataGridViewUtils.BindTo(_dataGridView, rows,
			                                autoGenerateColumns,
			                                sortStateOverride,
			                                defaultSortState,
			                                selectionStateOverride);
		}

		public void ClearRows()
		{
			_dataGridView.Rows.Clear();
		}

		public int SelectedRowCount => _dataGridView.SelectedRows.Count;

		public bool HasSelectedRows => _dataGridView.SelectedRows.Count > 0;

		public bool HasSingleSelectedRow => _dataGridView.SelectedRows.Count == 1;

		public int FirstSelectedRowIndex
		{
			get
			{
				if (_dataGridView.SelectedRows.Count == 0)
				{
					return -1;
				}

				int minimumIndex = int.MaxValue;

				foreach (var row in _dataGridView.SelectedRows
				                                 .Cast<DataGridViewRow>()
				                                 .Where(r => r.Index < minimumIndex))
				{
					minimumIndex = row.Index;
				}

				return minimumIndex;
			}
		}

		public int LastSelectedRowIndex
		{
			get
			{
				if (_dataGridView.SelectedRows.Count == 0)
				{
					return -1;
				}

				int maximumIndex = int.MinValue;

				foreach (DataGridViewRow row in _dataGridView.SelectedRows)
				{
					if (row.Index > maximumIndex)
					{
						maximumIndex = row.Index;
					}
				}

				return maximumIndex;
			}
		}

		public void AddColumns([NotNull] IEnumerable<ColumnDescriptor> columnDescriptors)
		{
			AddColumns(columnDescriptors, new string[] { });
		}

		public void AddColumns([NotNull] IEnumerable<ColumnDescriptor> columnDescriptors,
		                       [NotNull] IEnumerable<string> hiddenProperties)
		{
			Assert.ArgumentNotNull(columnDescriptors, nameof(columnDescriptors));
			Assert.ArgumentNotNull(hiddenProperties, nameof(hiddenProperties));

			_dataGridView.AutoGenerateColumns = false;
			_dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

			ICollection<string> hiddenPropertiesCollection =
				CollectionUtils.GetCollection(hiddenProperties);

			var anyFillColumn = false;
			foreach (ColumnDescriptor descriptor in columnDescriptors)
			{
				if (IsHidden(descriptor, hiddenPropertiesCollection))
				{
					continue;
				}

				DataGridViewColumn column = descriptor.CreateColumn<ROW>();

				if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
				{
					anyFillColumn = true;
				}

				_dataGridView.Columns.Add(column);
			}

			Assert.True(_dataGridView.ColumnCount > 0, "no visible columns added");

			if (! anyFillColumn)
			{
				// make the last column stretch
				_dataGridView.Columns[_dataGridView.ColumnCount - 1].AutoSizeMode =
					DataGridViewAutoSizeColumnMode.Fill;
			}
		}

		[NotNull]
		public IList<ROW> GetSelectedRows()
		{
			var list = new List<ROW>();

			foreach (DataGridViewRow row in _dataGridView.SelectedRows)
			{
				list.Add(GetTableRow(row));
			}

			return list;
		}

		[NotNull]
		public IList<ROW> GetAllRows(bool excludeInvisible = false)
		{
			var list = new List<ROW>();

			foreach (DataGridViewRow row in _dataGridView.Rows)
			{
				if (excludeInvisible && ! row.Visible)
				{
					continue;
				}

				list.Add(GetTableRow(row));
			}

			return list;
		}

		[NotNull]
		public IList<ROW> GetUnselectedRows()
		{
			var list = new List<ROW>();

			foreach (DataGridViewRow row in _dataGridView.Rows)
			{
				if (row.Selected)
				{
					continue;
				}

				list.Add(GetTableRow(row));
			}

			return list;
		}

		[CanBeNull]
		public ROW GetFirstSelectedRow()
		{
			int firstSelectedRowIndex = FirstSelectedRowIndex;

			return firstSelectedRowIndex < 0
				       ? null
				       : GetRow(firstSelectedRowIndex);
		}

		public void SelectRows([NotNull] Predicate<ROW> predicate)
		{
			Assert.ArgumentNotNull(predicate, nameof(predicate));

			DataGridViewRow firstSelectedRow = null;

			_dataGridView.ClearSelection();

			foreach (DataGridViewRow row in _dataGridView.Rows)
			{
				if (! predicate(GetTableRow(row)))
				{
					continue;
				}

				if (! row.Visible)
				{
					continue;
				}

				row.Selected = true;
				if (firstSelectedRow == null)
				{
					firstSelectedRow = row;
				}
			}

			if (firstSelectedRow != null && ! firstSelectedRow.Displayed)
			{
				_dataGridView.FirstDisplayedScrollingRowIndex =
					firstSelectedRow.Index;
			}
		}

		public void RestoreSelection(
			[NotNull] BoundDataGridSelectionState<ROW> selectionState)
		{
			Assert.ArgumentNotNull(selectionState, nameof(selectionState));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(() => $"Restoring selection ({selectionState.SelectionCount} row(s))");
			}

			DataGridViewUtils.RestoreSelection(_dataGridView, selectionState);
		}

		// This overload would be useful, but the scrolling to the first selected
		// row does not work. Further analysis needed. Possibly the FirstDisplayedScrollingRowIndex
		// call must be in a later event?

		//public void RestoreSelection(BoundDataGridSelectionState<ROW> selectionState,
		//                     bool ensureFirstSelectedRowIsVisible)
		//{
		//    Assert.ArgumentNotNull(selectionState, "selectedRows");

		//    if (selectionState.SelectionCount == 0)
		//    {
		//        return;
		//    }

		//    DataGridViewRow firstSelectedRow = null;

		//    foreach (DataGridViewRow row in _dataGridView.Rows)
		//    {
		//        ROW tableRow = row.DataBoundItem as ROW;

		//        if (tableRow != null && selectionState.IsSelected(tableRow))
		//        {
		//            if (firstSelectedRow == null)
		//            {
		//                firstSelectedRow = row;
		//            }

		//            row.Selected = true;
		//        }
		//    }

		//    if (ensureFirstSelectedRowIsVisible && firstSelectedRow != null)
		//    {
		//        if (!firstSelectedRow.Displayed)
		//        {
		//            _dataGridView.CurrentCell = firstSelectedRow.Cells[0];
		//            _dataGridView.FirstDisplayedScrollingRowIndex = firstSelectedRow.Index;
		//        }
		//    }
		//}

		public bool IsSortingColumn([NotNull] DataGridViewCellMouseEventArgs e)
		{
			return DataGridViewUtils.IsColumnSortEvent(_dataGridView, e);
		}

		/// <summary>
		/// Indicates if the there is a pending column sort operation by the user
		/// (only if <c>true</c> was passed to <c>restoreSelectionAfterUserSort</c> in <see cref="BoundDataGridHandler{ROW}(SortAwareDataGridView, bool)"/>)
		/// </summary>
		public bool HasPendingUserSort => _savedSelection != null;

		[NotNull]
		public BoundDataGridSelectionState<ROW> GetSelectionState()
		{
			return DataGridViewUtils.GetSelectionState<ROW>(_dataGridView);
		}

		[CanBeNull]
		public ROW GetRow(int rowIndex)
		{
			return rowIndex < 0 || rowIndex >= _dataGridView.RowCount
				       ? null
				       : GetTableRow(_dataGridView.Rows[rowIndex]);
		}

		public void ClearSelection()
		{
			_dataGridView.ClearSelection();
		}

		#region Non-public members

		private static bool IsHidden([NotNull] ColumnDescriptor descriptor,
		                             [NotNull] IEnumerable<string> hiddenProperties)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));
			Assert.ArgumentNotNull(hiddenProperties, nameof(hiddenProperties));

			return hiddenProperties.Any(
				p => string.Equals(p, descriptor.FieldName,
				                   StringComparison.OrdinalIgnoreCase));
		}

		[NotNull]
		private static ROW GetTableRow([NotNull] DataGridViewRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			return (ROW) row.DataBoundItem;
		}

		private void _dataGridView_Sorted(object sender, EventArgs e)
		{
			_msg.VerboseDebug(() => "BoundDataGridHandler._dataGridView_Sorted");

			// if there are selected rows to restore, do it now
			if (_savedSelection == null)
			{
				return;
			}

			RestoreSelection(_savedSelection);

			_savedSelection = null;
		}

		private void _dataGridView_SortingColumn(object sender, SortingColumnEventArgs e)
		{
			_msg.VerboseDebug(() => "BoundDataGridHandler._dataGridView_SortingColumn");

			// the user is sorting on a column - remember the selected rows
			_savedSelection = GetSelectionState();
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			_msg.VerboseDebug(() => "BoundDataGridHandler._dataGridView_SelectionChanged");

			// while sorting the grid, this event is also raised

			if (_savedSelection != null)
			{
				// ignore during sorting
				return;
			}

			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		public void Dispose()
		{
			var sortAwareGrid = _dataGridView as SortAwareDataGridView;
			if (sortAwareGrid != null)
			{
				sortAwareGrid.SortingColumn -= _dataGridView_SortingColumn;
			}

			_dataGridView.Sorted -= _dataGridView_Sorted;
			_dataGridView.SelectionChanged -= _dataGridView_SelectionChanged;
		}
	}
}

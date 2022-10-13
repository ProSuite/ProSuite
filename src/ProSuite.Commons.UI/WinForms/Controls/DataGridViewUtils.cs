using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Helper methods for dealing with any instance of <see cref="DataGridView"/>
	/// </summary>
	public static class DataGridViewUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Determines whether a given event is triggers sorting on a column.
		/// </summary>
		/// <param name="dataGridView">The data grid view.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellMouseEventArgs"/>
		/// instance containing the event data.</param>
		/// <returns>
		///   <c>true</c> if the mouse click event triggers sorting on a column of the
		/// specified data grid; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsColumnSortEvent([NotNull] DataGridView dataGridView,
		                                     [NotNull] DataGridViewCellMouseEventArgs e)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));
			Assert.ArgumentNotNull(e, nameof(e));

			if (e.RowIndex == -1 && e.Button == MouseButtons.Left && e.ColumnIndex >= 0)
			{
				DataGridViewColumn column = dataGridView.Columns[e.ColumnIndex];

				if (column.SortMode == DataGridViewColumnSortMode.Automatic)
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		public static DataGridViewColumn CreateColumn([NotNull] DataColumn dataColumn,
		                                              bool fill)
		{
			Assert.ArgumentNotNull(dataColumn, nameof(dataColumn));

			return new DataGridViewColumn(new DataGridViewTextBoxCell())
			       {
				       DataPropertyName = dataColumn.ColumnName,
				       Name = dataColumn.ColumnName,
				       HeaderText = dataColumn.Caption,
				       AutoSizeMode = fill
					                      ? DataGridViewAutoSizeColumnMode.Fill
					                      : DataGridViewAutoSizeColumnMode.AllCells
			       };
		}

		/// <summary>
		/// Sets the minimum width of a column such that at least the entire header text fits in the column.
		/// </summary>
		/// <param name="column">The column.</param>
		/// <remarks>The column must already be added to a data grid view, otherwise the preferred 
		/// size of the header cannot be determined (font etc. not known)</remarks>
		public static void SetMinimumWidthForHeader([NotNull] DataGridViewColumn column)
		{
			Assert.ArgumentNotNull(column, nameof(column));
			Assert.NotNull(column.DataGridView,
			               "column must be added to datagridview to calculate preferred width");

			const bool fixedHeight = true;
			int headerWidth = column.GetPreferredWidth(
				DataGridViewAutoSizeColumnMode.ColumnHeader, fixedHeight);

			if (headerWidth > 0)
			{
				column.MinimumWidth = headerWidth;
			}
		}

		public static bool TryApplySortState([NotNull] DataGridView dataGridView,
		                                     [NotNull] ColumnSortState sortState)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));
			Assert.ArgumentNotNull(sortState, nameof(sortState));

			ListSortDirection? direction = sortState.GetListSortDirection();

			return direction != null &&
			       TryApplySortState(dataGridView, sortState.SortedColumnName,
			                         direction.Value);
		}

		public static bool TryApplySortState([NotNull] DataGridView dataGridView,
		                                     [CanBeNull] string sortedColumnName,
		                                     ListSortDirection direction)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			if (string.IsNullOrEmpty(sortedColumnName))
			{
				return false;
			}

			try
			{
				if (dataGridView.Columns.Contains(sortedColumnName))
				{
					DataGridViewColumn columnToSort =
						dataGridView.Columns[sortedColumnName];

					Assert.NotNull(columnToSort, nameof(columnToSort));

					// NOTE this is very slow (not due to the actual binding to row properties or sorting the binding list, but
					// apparently due to the datagridview implementation itself (observed for standard data grid views also)
					dataGridView.Sort(columnToSort, direction);

					return true;
				}

				_msg.DebugFormat("Sort column not found: {0}", sortedColumnName);
			}
			catch (Exception e)
			{
				_msg.Warn($"Error applying data grid sort order: {e.Message}", e);
			}

			return false;
		}

		public static bool TrySortBindingList<T>([NotNull] IBindingList list,
		                                         [NotNull] DataGridView grid,
		                                         [CanBeNull] ColumnSortState sortState)
			where T : class
		{
			var sortDirection = sortState?.GetListSortDirection();

			if (sortState?.SortedColumnName == null || sortDirection == null)
			{
				return false;
			}

			return TrySortBindingList<T>(list,
			                             grid,
			                             sortState.SortedColumnName,
			                             sortDirection.Value);
		}

		public static bool TrySortBindingList<T>([NotNull] IBindingList list,
		                                         [NotNull] DataGridView grid,
		                                         [NotNull] string sortedColumnName,
		                                         ListSortDirection sortDirection) where T : class
		{
			if (! grid.Columns.Contains(sortedColumnName))
			{
				return false;
			}

			var column = Assert.NotNull(grid.Columns[sortedColumnName]);
			if (column.DataPropertyName == null)
			{
				return false;
			}

			var descriptor = TypeDescriptor.GetProperties(typeof(T))
			                               .Find(column.DataPropertyName, true);
			list.ApplySort(descriptor, sortDirection);
			return true;
		}

		[NotNull]
		public static ColumnSortState GetSortState([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			DataGridViewColumn sortedColumn = dataGridView.SortedColumn;
			string sortedColumnName = sortedColumn?.Name;

			return new ColumnSortState(sortedColumnName, dataGridView.SortOrder);
		}

		public static bool TrySetFirstDisplayedScrollingRow(
			[NotNull] DataGridView dataGridView,
			int rowIndex)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			int maxRowIndex = dataGridView.RowCount - 1;

			if (rowIndex < 0 || maxRowIndex < 0)
			{
				return false;
			}

			int scrollRowIndex = rowIndex > maxRowIndex
				                     ? maxRowIndex
				                     : rowIndex;

			if (dataGridView.FirstDisplayedScrollingRowIndex == scrollRowIndex)
			{
				return true;
			}

			for (int i = scrollRowIndex; i <= maxRowIndex; i++)
			{
				DataGridViewRow row = dataGridView.Rows[i];
				if (row.Visible)
				{
					dataGridView.FirstDisplayedScrollingRowIndex = i;
					return true;
				}
			}

			for (int i = scrollRowIndex; i >= 0; i--)
			{
				DataGridViewRow row = dataGridView.Rows[i];
				if (row.Visible)
				{
					dataGridView.FirstDisplayedScrollingRowIndex = i;
					return true;
				}
			}

			return false;
		}

		public static bool TrySetFirstDisplayedScrollinColumn(
			[NotNull] DataGridView dataGridView,
			int columnIndex)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			int maxIndex = dataGridView.ColumnCount - 1;

			if (columnIndex < 0 || maxIndex < 0)
			{
				return false;
			}

			int scrollIndex = columnIndex > maxIndex
				                  ? maxIndex
				                  : columnIndex;

			if (dataGridView.FirstDisplayedScrollingColumnIndex == scrollIndex)
			{
				return true;
			}

			for (int i = scrollIndex; i <= maxIndex; i++)
			{
				DataGridViewColumn column = dataGridView.Columns[i];

				if (column.Visible)
				{
					dataGridView.FirstDisplayedScrollingColumnIndex = i;
					return true;
				}
			}

			for (int i = scrollIndex; i >= 0; i--)
			{
				DataGridViewColumn column = dataGridView.Columns[i];

				if (column.Visible)
				{
					dataGridView.FirstDisplayedScrollingColumnIndex = i;
					return true;
				}
			}

			return false;
		}

		public static Point GetMouseLocation([NotNull] DataGridView dataGridView,
		                                     [NotNull] DataGridViewCellMouseEventArgs e)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));
			Assert.ArgumentNotNull(e, nameof(e));

			Rectangle cellRect = dataGridView.GetCellDisplayRectangle(
				e.ColumnIndex, e.RowIndex, cutOverflow: false);

			Point location = e.Location;

			location.Offset(cellRect.Left, cellRect.Top);

			return location;
		}

		/// <summary>
		/// Ensures at least one of the selected rows (if any) is visible.
		/// </summary>
		/// <param name="dataGridView">The data grid view.</param>
		public static bool EnsureRowSelectionIsVisible([NotNull] DataGridView dataGridView)
		{
			if (dataGridView.SelectedRows.Count == 0)
			{
				return false;
			}

			int? firstSelectedRowIndex = null;

			foreach (DataGridViewRow row in dataGridView.SelectedRows)
			{
				if (row.Displayed)
				{
					return false;
				}

				if (! row.Visible)
				{
					// ignore hidden rows
					continue;
				}

				firstSelectedRowIndex = firstSelectedRowIndex == null
					                        ? row.Index
					                        : Math.Min(row.Index, firstSelectedRowIndex.Value);
			}

			// no selected row is currently displayed

			if (firstSelectedRowIndex == null)
			{
				return false;
			}

			dataGridView.FirstDisplayedScrollingRowIndex = firstSelectedRowIndex.Value;

			return true;
		}

		/// <summary>
		/// Binds a data grid view to the given list of rows
		/// </summary>
		/// <param name="grid">The data grid view to bind to the list</param>
		/// <param name="rows">The rows.</param>
		/// <param name="autoGenerateColumns">Indicates if columns should be dynamically added based on the bound type</param>
		/// <param name="sortStateOverride">Optional sort state to apply regardless of the current sort state of the grid.</param>
		/// <param name="defaultSortState">Optional default sort state, to be applied when there is no current or override sort state.</param>
		/// <param name="selectionStateOverride"></param>
		public static bool BindTo<TRow>(
			[NotNull] DataGridView grid,
			[NotNull] IList<TRow> rows,
			bool autoGenerateColumns = false,
			[CanBeNull] DataGridViewSortState sortStateOverride = null,
			DataGridViewSortState defaultSortState = null,
			[CanBeNull] BoundDataGridSelectionState<TRow> selectionStateOverride = null)
			where TRow : class
		{
			Assert.ArgumentNotNull(grid, nameof(grid));
			Assert.ArgumentNotNull(rows, nameof(rows));

			var sortState = sortStateOverride ??
			                (defaultSortState != null &&
			                 (grid.SortedColumn == null ||
			                  grid.SortOrder == SortOrder.None)
				                 ? defaultSortState
				                 : new DataGridViewSortState(grid));

			var bindingList = rows as IBindingList;
			bool presorted = bindingList != null &&
			                 TrySortBindingList<TRow>(bindingList, grid, sortState);

			var selectionState = selectionStateOverride ?? GetSelectionState<TRow>(grid);

			grid.AutoGenerateColumns = autoGenerateColumns;

			grid.DataSource = typeof(TRow);
			grid.DataSource = rows;

			bool sortingApplied = presorted || TryApplySortState(grid, sortState);

			RestoreSelection(grid, selectionState);

			return sortingApplied;
		}

		public static void MakeColumnsResizable([NotNull] DataGridView grid,
		                                        int? maxColumnWidth)
		{
			Assert.ArgumentNotNull(grid, nameof(grid));
			Assert.ArgumentCondition(maxColumnWidth == null || maxColumnWidth > 0,
			                         "value must be > 0 or null",
			                         nameof(maxColumnWidth));

			for (int i = 0; i < grid.ColumnCount; i++)
			{
				var column = grid.Columns[i];

				if (! (IsAutoSizingToContent(column.AutoSizeMode) ??
				       IsAutoSizingToContent(column.InheritedAutoSizeMode) ??
				       false))
				{
					continue;
				}

				int origWidth = column.Width;
				column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				column.Width = Math.Min(origWidth, maxColumnWidth ?? int.MaxValue);
			}
		}

		[NotNull]
		public static IDictionary<string, int> GetColumnWidths(
			[NotNull] DataGridView grid, bool onlyResizable = true)
		{
			return grid.Columns
			           .OfType<DataGridViewColumn>()
			           .Where(c => ! onlyResizable ||
			                       c.Resizable == DataGridViewTriState.True)
			           .ToDictionary(c => c.Name, c => c.Width);
		}

		public static void RestoreColumnWidths(
			[NotNull] DataGridView grid,
			[NotNull] IDictionary<string, int> columnWidths,
			bool onlyResizable = true)
		{
			Assert.ArgumentNotNull(grid, nameof(grid));
			Assert.ArgumentNotNull(columnWidths, nameof(columnWidths));

			foreach (var column in grid.Columns
			                           .OfType<DataGridViewColumn>()
			                           .Where(c => ! onlyResizable ||
			                                       c.Resizable ==
			                                       DataGridViewTriState.True))
			{
				int width;
				if (columnWidths.TryGetValue(column.Name, out width) && width > 0)
				{
					column.Width = width;
				}
			}
		}

		private static bool? IsAutoSizingToContent(DataGridViewAutoSizeColumnMode autoSizeMode)
		{
			switch (autoSizeMode)
			{
				case DataGridViewAutoSizeColumnMode.NotSet:
					return null;
				case DataGridViewAutoSizeColumnMode.None:
				case DataGridViewAutoSizeColumnMode.Fill:
				case DataGridViewAutoSizeColumnMode.ColumnHeader:
					return false;
				case DataGridViewAutoSizeColumnMode.AllCells:
				case DataGridViewAutoSizeColumnMode.AllCellsExceptHeader:
				case DataGridViewAutoSizeColumnMode.DisplayedCells:
				case DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader:
					return true;

				default:
					throw new ArgumentOutOfRangeException(nameof(autoSizeMode), autoSizeMode, null);
			}
		}

		[NotNull]
		public static BoundDataGridSelectionState<TRow> GetSelectionState<TRow>(
			[NotNull] DataGridView grid, bool rowsMustAlsoBeVisible = true)
			where TRow : class
		{
			var set = new HashSet<TRow>();

			string firstErrorMessage = null;
			int errorCount = 0;

			foreach (DataGridViewRow row in grid.SelectedRows)
			{
				if (rowsMustAlsoBeVisible && ! row.Visible)
				{
					continue;
				}

				TRow item;
				string message;
				if (! TryGetDataBoundItem(row, out item, out message))
				{
					errorCount++;
					if (firstErrorMessage == null)
					{
						firstErrorMessage = message;
					}
				}
				else if (item != null)
				{
					set.Add(item);
				}
			}

			if (errorCount > 0)
			{
				_msg.WarnFormat("{0} error(s) getting bound items. First message: {1}",
				                errorCount, firstErrorMessage);
			}

			return new BoundDataGridSelectionState<TRow>(set);
		}

		public static bool TryGetDataBoundItem<T>([NotNull] DataGridViewRow row,
		                                          [CanBeNull] out T item,
		                                          [NotNull] out string message)
			where T : class
		{
			try
			{
				if (row.DataBoundItem == null)
				{
					// there is really no bound item -> success case
					item = null;
					message = string.Empty;
					return true;
				}

				item = row.DataBoundItem as T;
				if (item == null)
				{
					message = string.Format("Bound item is of unexpected type ({0})",
					                        row.DataBoundItem.GetType().FullName);
					return false;
				}

				message = string.Empty;
				return true;
			}
			catch (Exception e)
			{
				item = null;
				message = $"Error getting bound item: {e.Message}";
				return false;
			}
		}

		public static void RestoreSelection<TRow>(
			[NotNull] DataGridView grid,
			[NotNull] BoundDataGridSelectionState<TRow> selectionState,
			bool forceSelectedRowsVisible = true) where TRow : class
		{
			grid.ClearSelection();

			if (selectionState.SelectionCount == 0)
			{
				return;
			}

			foreach (DataGridViewRow row in grid.Rows)
			{
				var tableRow = row.DataBoundItem as TRow;

				if (tableRow != null && selectionState.IsSelected(tableRow))
				{
					// Note: during sorting, rows have Visible == false in *some* situations
					if (forceSelectedRowsVisible)
					{
						// Ensure that the previously selected row is again visible. Otherwise the row may be
						// immediately unselected if there is a filter defined for it
						// (if the grid is a FilterableDataGridView)
						row.Visible = true;
					}

					row.Selected = true;
				}
			}
		}
	}
}

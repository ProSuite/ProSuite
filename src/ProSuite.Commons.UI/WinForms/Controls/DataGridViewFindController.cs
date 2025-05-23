using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	// TODO: instantiate AND dispose inside toolstrip, to avoid leaking GC handles
	public class DataGridViewFindController : IDataGridViewFindObserver
	{
		//TODO: Many issues with selection (Filter, adding rows, typing additional letters,...)
		//TODO: Test cases with current and selection (multiple selection/single selection, contains current/does not contain current)

		[NotNull] private readonly DataGridView _dataGridView;
		[NotNull] private readonly IDataGridViewFindToolsView _toolsView;

		[CanBeNull] private DataGridViewFindResults _findResults;

		private string _findText;
		private readonly Color _notFoundColor = Color.OrangeRed;
		private readonly Color _selectedFindResultBackColor;
		private readonly Color _findResultBackColor;
		private readonly Color _activeFindResultBackColor;
		private bool _matchCase;
		private bool _filterRows;

		private bool _hasFilteredRows;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _sortingEventRegistered;
		private bool _canFilterRows = true;

		public event EventHandler FindResultChanged;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DataGridViewFindController"/> class.
		/// </summary>
		/// <param name="dataGridView">The data grid view.</param>
		/// <param name="toolsView">The tools view.</param>
		public DataGridViewFindController([NotNull] DataGridView dataGridView,
		                                  [NotNull] IDataGridViewFindToolsView toolsView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));
			Assert.ArgumentNotNull(toolsView, nameof(toolsView));

			_dataGridView = dataGridView;
			_toolsView = toolsView;

			_toolsView.Observer = this;

			_selectedFindResultBackColor = Color.FromArgb(152, 204, 202);
			_findResultBackColor = Color.FromArgb(255, 255, 150);
			_activeFindResultBackColor = Color.FromArgb(255, 150, 50);

			RenderFindResultOnToolsView();

			_dataGridView.CellPainting += _dataGridView_CellPainting;

			var sortAwareDataGridView = _dataGridView as SortAwareDataGridView;
			if (sortAwareDataGridView != null)
			{
				sortAwareDataGridView.SortingColumn += _dataGridView_SortingColumn;
				_dataGridView.Sorted += _dataGridView_Sorted;
			}

			_dataGridView.Paint += _dataGridView_Paint;

			CanFilterRows = SupportsRowFiltering;
		}

		#endregion

		public int CurrentFindResultIndex { get; set; } = -1;

		public int FindResultCount => _findResults?.FindResultCells.Count ?? 0;

		public bool MatchCase
		{
			get { return _matchCase; }
			set
			{
				if (_matchCase == value)
				{
					return;
				}

				_matchCase = value;

				Find(_findText);
			}
		}

		public bool CanFilterRows
		{
			get { return _canFilterRows; }
			set
			{
				if (! SupportsRowFiltering)
				{
					Assert.ArgumentCondition(
						! value, "Row filtering is not supported by data grid", nameof(value));
				}

				_canFilterRows = value;

				_toolsView.FilterRowsButtonVisible = _canFilterRows;
			}
		}

		public bool FilterRows
		{
			get { return _filterRows; }
			set
			{
				if (_filterRows == value)
				{
					return;
				}

				if (value && ! CanFilterRows)
				{
					throw new InvalidOperationException("Filtering rows is disabled");
				}

				_filterRows = value;
				_toolsView.FilterRows = value;

				if (! string.IsNullOrEmpty(_findText))
				{
					Find(_findText, keepSelection: true);
				}
			}
		}

		public void Find(string text)
		{
			Find(text, keepSelection: false);
		}

		public void Find([CanBeNull] string text, bool keepSelection)
		{
			if (_findResults == null)
			{
				_findResults = new DataGridViewFindResults();
			}
			else
			{
				_findResults.Clear();
			}

			_findResults.Clear();
			CurrentFindResultIndex = -1;
			_findText = text;

			if (! string.IsNullOrEmpty(text))
			{
				IEnumerable<string> expressions = StringUtils.Split(text, "|", '\\',
					removeEmptyEntries: true);

				List<Regex> regexes =
					expressions.Select(exp => RegexUtils.GetWildcardMatchRegex(exp, MatchCase))
					           .ToList();

				int columnCount = _dataGridView.ColumnCount;
				var columnIndexMatches = new bool[columnCount];

				foreach (DataGridViewRow row in _dataGridView.Rows)
				{
					Array.Clear(columnIndexMatches, 0, columnIndexMatches.Length);

					var anyUnmatchedRegex = false;

					foreach (Regex regex in regexes)
					{
						var regexMatchesAnyColumn = false;

						for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
						{
							if (Matches(row, columnIndex, regex))
							{
								regexMatchesAnyColumn = true;
								columnIndexMatches[columnIndex] = true;
							}
						}

						if (! regexMatchesAnyColumn)
						{
							anyUnmatchedRegex = true;
						}
					}

					if (anyUnmatchedRegex)
					{
						continue;
					}

					for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
					{
						if (columnIndexMatches[columnIndex])
						{
							_findResults.AddFindResult(row.Index, columnIndex);
						}
					}
				}

				CurrentFindResultIndex = _findResults.FindResultCells.Count <= 0
					                         ? -1
					                         : 0;
			}

			TrySetFindResultIndexToFirstInSelection();
			RenderFindResultOnDataGrid(keepSelection);
			RenderFindResultOnToolsView();
			RenderFilterRows();
			_toolsView.ClearFilterEnabled = ! string.IsNullOrEmpty(_findText);

			OnFindResultChanged(null);

			_sortingEventRegistered = false;
		}

		public void ClearFilter()
		{
			_toolsView.FindText = null;
		}

		public void MoveToNext()
		{
			if (FindResultCount <= 0)
			{
				CurrentFindResultIndex = -1;
				return;
			}

			CurrentFindResultIndex++;

			if (CurrentFindResultIndex >= FindResultCount)
			{
				CurrentFindResultIndex = 0;
			}

			RenderFindResultOnDataGrid();
			RenderFindResultOnToolsView();
		}

		public void MoveToPrevious()
		{
			if (FindResultCount <= 0)
			{
				CurrentFindResultIndex = -1;
				return;
			}

			CurrentFindResultIndex--;

			if (CurrentFindResultIndex < 0)
			{
				CurrentFindResultIndex = FindResultCount - 1;
			}

			RenderFindResultOnDataGrid();
			RenderFindResultOnToolsView();
		}

		public void HandleFindKeyEvent(KeyEventArgs e)
		{
			if (FindResultCount == 0)
			{
				return;
			}

			bool shiftPressed = KeyboardUtils.IsModifierPressed(Keys.Shift);

			if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Return && ! shiftPressed)
			{
				MoveToNext();
				e.Handled = true;
			}

			if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Return && shiftPressed)
			{
				MoveToPrevious();
				e.Handled = true;
			}
		}

		public void SelectAllRows()
		{
			if (_findResults == null)
			{
				return;
			}

			foreach (DataGridViewRow row in _dataGridView.Rows)
			{
				if (_findResults.HasFindResult(row.Index))
				{
					row.Selected = true;
				}
			}
		}

		#region Non-public

		private bool SupportsRowFiltering => _dataGridView is IFilterableDataGridView;

		private static bool Matches([NotNull] DataGridViewRow row,
		                            int columnIndex,
		                            [NotNull] Regex regex)
		{
			DataGridViewCell cell = row.Cells[columnIndex];

			object value = cell.FormattedValue;
			var stringValue = value as string;

			if (stringValue == null)
			{
				return false;
			}

			Match match = regex.Match(stringValue);

			return match.Success;
		}

		private void RenderFilterRows()
		{
			var rowFilter = _dataGridView as IFilterableDataGridView;

			if (rowFilter != null)
			{
				if (! _filterRows ||
				    _findResults == null ||
				    _findResults.FindResultCells.Count == 0)
				{
					rowFilter.ShowAllRows();
					_hasFilteredRows = false;
				}
				else
				{
					rowFilter.FilterRows(row => _findResults.HasFindResult(row.Index));
					_hasFilteredRows = true;
				}
			}
			else
			{
				_hasFilteredRows = false;
			}

			_toolsView.DisplayFilteredRowsState(_hasFilteredRows);
		}

		private void RenderFindResultOnToolsView()
		{
			int findResultCount = FindResultCount;
			int currentFindIndex = CurrentFindResultIndex;

			if (findResultCount == 0 && ! string.IsNullOrEmpty(_findText))
			{
				_toolsView.SetFindResultStatusColor(_notFoundColor);
			}
			else
			{
				_toolsView.ClearFindResultStatusColor();
			}

			int currentFindResult = currentFindIndex + 1;

			_toolsView.FindResultStatusText = GetFindResultStatusText(findResultCount,
				currentFindResult);

			bool moveButtonsEnabled = findResultCount > 1;

			_toolsView.MoveNextEnabled = moveButtonsEnabled;
			_toolsView.MovePreviousEnabled = moveButtonsEnabled;
		}

		[NotNull]
		private string GetFindResultStatusText(int findResultCount, int currentFindResult)
		{
			var rowCount = 0;
			if (_findResults != null)
			{
				rowCount = _findResults.FindResultRows.Count;
			}

			if (findResultCount == rowCount)
			{
				return string.Format("{0} of {1}", currentFindResult, findResultCount);
			}

			return string.Format("{0} of {1} (on {2} row{3})",
			                     currentFindResult, findResultCount, rowCount,
			                     rowCount == 1
				                     ? string.Empty
				                     : "s");
		}

		private void RenderFindResultOnDataGrid()
		{
			const bool keepSelection = false;
			RenderFindResultOnDataGrid(keepSelection);
		}

		private void RenderFindResultOnDataGrid(bool keepSelection)
		{
			if (FindResultCount > 0)
			{
				Assert.ArgumentCondition(
					CurrentFindResultIndex < _findResults?.FindResultCells.Count,
					"invalid find index");

				SetCurrentCell(keepSelection);
			}

			_dataGridView.Refresh();
		}

		private void SetCurrentCell(bool restoreSelection)
		{
			DataGridViewSelectedRowCollection selectedRows = _dataGridView.SelectedRows;

			DataGridViewFindResultCell firstFindCell =
				Assert.NotNull(_findResults).FindResultCells[CurrentFindResultIndex];
		   
			DataGridViewRow row = _dataGridView.Rows[firstFindCell.RowIndex];

			if (!row.Visible)
			{
				return;
			}

			DataGridViewColumn column = _dataGridView.Columns[firstFindCell.ColumnIndex];
			if (!column.Visible)
			{
				return;
			}

			// Scroll to ensure the cell is visible
			_dataGridView.FirstDisplayedScrollingRowIndex = row.Index;
			_dataGridView.FirstDisplayedScrollingColumnIndex = column.Index;

			_dataGridView.CurrentCell = row.Cells[firstFindCell.ColumnIndex];

			if (restoreSelection)
			{
				RestoreSelection(selectedRows);
			}
		}

		private void RestoreSelection(DataGridViewSelectedRowCollection selectedRows)
		{
			_dataGridView.CurrentCell.Selected = false;

			foreach (DataGridViewRow selectedRow in selectedRows)
			{
				_dataGridView.Rows[selectedRow.Index].Selected = true;
			}
		}

		private void TrySetFindResultIndexToFirstInSelection()
		{
			if (_findResults == null)
			{
				return;
			}

			DataGridViewSelectedRowCollection selectedRows = _dataGridView.SelectedRows;

			int minimumRowIndex = int.MaxValue;

			foreach (DataGridViewRow row in selectedRows)
			{
				if (! _findResults.HasFindResult(row.Index)
				    || row.Index >= minimumRowIndex)
				{
					continue;
				}

				foreach (DataGridViewCell cell in row.Cells)
				{
					if (! _findResults.HasFindResult(row.Index, cell.ColumnIndex))
					{
						continue;
					}

					CurrentFindResultIndex = _findResults.GetFindCellIndex(row.Index,
						cell.ColumnIndex);
					minimumRowIndex = row.Index;
					break;
				}
			}
		}

		private bool IsActiveCell(int rowIndex, int columnIndex)
		{
			if (_findResults == null ||
			    CurrentFindResultIndex >= _findResults.FindResultCells.Count ||
			    CurrentFindResultIndex < 0)
			{
				return false;
			}

			DataGridViewFindResultCell currentFind =
				_findResults.FindResultCells[CurrentFindResultIndex];

			return currentFind.RowIndex == rowIndex &&
			       currentFind.ColumnIndex == columnIndex;
		}

		private void OnFindResultChanged(EventArgs e)
		{
			FindResultChanged?.Invoke(this, e);
		}

		#region Event handlers

		private void _dataGridView_CellPainting(object sender,
		                                        DataGridViewCellPaintingEventArgs e)
		{
			if (_findResults == null ||
			    ! _findResults.HasFindResult(e.RowIndex, e.ColumnIndex))
			{
				return;
			}

			bool isCurrentCell = IsActiveCell(e.RowIndex, e.ColumnIndex);

			e.CellStyle.BackColor = isCurrentCell
				                        ? _activeFindResultBackColor
				                        : _findResultBackColor;
			e.CellStyle.SelectionBackColor = isCurrentCell
				                                 ? _activeFindResultBackColor
				                                 : _selectedFindResultBackColor;
			e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
		}

		private bool _hasPendingFind;

		private void _dataGridView_SortingColumn(object sender, SortingColumnEventArgs e)
		{
			_sortingEventRegistered = true;
		}

		private void _dataGridView_Sorted(object sender, EventArgs e)
		{
			_msg.VerboseDebug(() => "DataGridViewFindController._dataGridView_Sorted");

			if (! string.IsNullOrEmpty(_findText))
			{
				_hasPendingFind = true;
			}
		}

		private void _dataGridView_Paint(object sender, PaintEventArgs e)
		{
			_msg.VerboseDebug(() => "DataGridViewFindController._dataGridView_Paint");

			if (! _hasPendingFind)
			{
				return;
			}

			_hasPendingFind = false;

			if (! string.IsNullOrEmpty(_findText))
			{
				_msg.VerboseDebug(() => "Applying filter (delayed)");

				// ReSharper disable once RedundantArgumentName
				Find(_findText, keepSelection: _sortingEventRegistered);
			}
		}

		#endregion

		#endregion
	}
}

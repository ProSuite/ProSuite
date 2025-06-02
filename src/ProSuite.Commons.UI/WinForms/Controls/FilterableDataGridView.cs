using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class FilterableDataGridView : SortAwareDataGridView, IFilterableDataGridView
	{
		private bool _ignoreRowStateChange;

		private readonly Latch _latch = new Latch();

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public event EventHandler FilteredRowsChanged;

		#region IFilterableDataGridView

		void IFilterableDataGridView.FilterRows(
			Func<DataGridViewRow, bool> isRowVisible)
		{
			Assert.ArgumentNotNull(isRowVisible, nameof(isRowVisible));

			FilterRowsCore(isRowVisible);
		}

		void IFilterableDataGridView.ShowAllRows()
		{
			FilterRowsCore(row => true);
		}

		#endregion

		protected override void OnRowStateChanged(int rowIndex,
		                                          DataGridViewRowStateChangedEventArgs e)
		{
			if (_ignoreRowStateChange)
			{
				return;
			}

			base.OnRowStateChanged(rowIndex, e);
		}

		protected override void OnSelectionChanged(EventArgs e)
		{
			if (! _latch.IsLatched)
			{
				// when doing a range selection, in filtered state, 
				// the invisible rows are selected also --> unselect them again

				_latch.RunInsideLatch(
					delegate
					{
						foreach (DataGridViewRow row in SelectedRows)
						{
							if (! row.Visible)
							{
								row.Selected = false;
							}
						}
					});
			}

			base.OnSelectionChanged(e);
		}

		protected override bool SetCurrentCellAddressCore(int columnIndex,
		                                                  int rowIndex,
		                                                  bool setAnchorCellAddress,
		                                                  bool validateCurrentCell,
		                                                  bool throughMouseClick)
		{
			if (rowIndex >= 0)
			{
				DataGridViewRow row = Rows[rowIndex];
				if (! row.Visible)
				{
					return false;
				}
			}

			return base.SetCurrentCellAddressCore(columnIndex, rowIndex,
			                                      setAnchorCellAddress,
			                                      validateCurrentCell,
			                                      throughMouseClick);
		}

		private void FilterRowsCore(
			[NotNull] Func<DataGridViewRow, bool> isRowVisible)
		{
			_msg.VerboseDebug(() => "FilterableDataGridView.FilterRowsCore");

			bool wasIgnored = _ignoreRowStateChange;

			_ignoreRowStateChange = true;

			var selectionChanged = false;
			var visibleRowsChanged = false;
			CurrencyManager currencyManager = null; // This fixes ProSuite #236
			try
			{
				BindingContext bindingContext = Assert.NotNull(BindingContext);

				currencyManager = (CurrencyManager) bindingContext[DataSource];
				currencyManager.SuspendBinding();

				foreach (DataGridViewRow row in Rows)
				{
					if (row.IsNewRow)
					{
						continue;
					}

					bool newVisible = isRowVisible(row);
					if (newVisible != row.Visible)
					{
						row.Visible = newVisible;
						visibleRowsChanged = true;
					}

					// make sure that invisible rows are not selected
					if (! row.Visible && row.Selected)
					{
						row.Selected = false;
						selectionChanged = true;
					}
				}
			}
			finally
			{
				currencyManager?.ResumeBinding();

				_ignoreRowStateChange = wasIgnored;
				if (! wasIgnored)
				{
					ForcePendingRowStateChanges();
				}

				if (selectionChanged)
				{
					OnSelectionChanged(EventArgs.Empty);
				}

				if (visibleRowsChanged)
				{
					OnFilteredRowsChanged();
				}
			}
		}

		private void ForcePendingRowStateChanges()
		{
			if (Rows.Count <= 0)
			{
				return;
			}

			int currentRowIndex = CurrentCell?.RowIndex ?? -1;

			foreach (DataGridViewRow row in Rows)
			{
				if (row.Index == currentRowIndex)
				{
					continue;
				}

				ForceRowStateChange(row);

				break;
			}
		}

		private void ForceRowStateChange([NotNull] DataGridViewRow row)
		{
			bool visible = row.Visible;

			bool restoreScrollIndex = visible && row.Index == FirstDisplayedScrollingRowIndex;

			row.Visible = ! visible;
			row.Visible = visible;

			if (restoreScrollIndex)
			{
				FirstDisplayedScrollingRowIndex = row.Index;
			}
		}

		protected virtual void OnFilteredRowsChanged()
		{
			FilteredRowsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}

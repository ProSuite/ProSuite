using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public class TableStateManager<T> where T : class
	{
		[NotNull] private readonly BoundDataGridHandler<T> _gridHandler;
		[CanBeNull] private readonly DataGridViewFindToolStrip _findToolStrip;

		public TableStateManager(
			[NotNull] BoundDataGridHandler<T> gridHandler,
			[CanBeNull] DataGridViewFindToolStrip findToolStrip = null)
		{
			Assert.ArgumentNotNull(gridHandler, nameof(gridHandler));

			_gridHandler = gridHandler;
			_findToolStrip = findToolStrip;
		}

		public void SaveState([NotNull] TableState state)
		{
			Assert.ArgumentNotNull(state, nameof(state));

			var grid = _gridHandler.DataGridView;

			state.TableSortState = new DataGridViewSortState(grid);

			if (_findToolStrip != null)
			{
				state.FilterRows = _findToolStrip.FilterRows;
				state.FindText = _findToolStrip.FindText;
				state.MatchCase = _findToolStrip.MatchCase;
			}

			SaveSelection(state);

			// save the scroll position
			state.FirstDisplayedScrollingRowIndex = grid.FirstDisplayedScrollingRowIndex;
			state.FirstDisplayedScrollingColumnIndex = grid.FirstDisplayedScrollingColumnIndex;
		}

		public void ApplyState([NotNull] TableState state, bool presorted)
		{
			Assert.ArgumentNotNull(state, nameof(state));

			var grid = _gridHandler.DataGridView;

			if (! presorted)
			{
				state.TableSortState?.TryApplyState(grid);
			}

			if (_findToolStrip != null)
			{
				_findToolStrip.FilterRows = state.FilterRows;
				_findToolStrip.MatchCase = state.MatchCase;

				if (StringUtils.IsNotEmpty(state.FindText))
				{
					_findToolStrip.FindText = state.FindText;
				}
			}

			if (state.FirstDisplayedScrollingRowIndex > 0)
			{
				DataGridViewUtils.TrySetFirstDisplayedScrollingRow(
					grid, state.FirstDisplayedScrollingRowIndex);
			}

			if (state.FirstDisplayedScrollingColumnIndex > 0)
			{
				DataGridViewUtils.TrySetFirstDisplayedScrollinColumn(
					grid, state.FirstDisplayedScrollingColumnIndex);
			}

			RestoreSelection(state);
		}

		private void SaveSelection([NotNull] TableState state)
		{
			if (! typeof(IEntityRow).IsAssignableFrom(typeof(T)))
			{
				return;
			}

			state.ClearEntitySelection();

			foreach (T row in _gridHandler.GetSelectedRows())
			{
				if (row is IEntityRow entityRow)
				{
					state.AddSelectedEntity(entityRow.Entity);
				}
			}
		}

		private void RestoreSelection([NotNull] TableState state)
		{
			if (! typeof(IEntityRow).IsAssignableFrom(typeof(T)) || state.SelectedEntityCount <= 0)
			{
				return;
			}

			_gridHandler.SelectRows(row => row is IEntityRow entityRow &&
			                               state.IsSelected(entityRow.Entity));
		}
	}
}

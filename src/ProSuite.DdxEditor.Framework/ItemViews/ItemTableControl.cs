using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Menus;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public partial class ItemTableControl<T> : UserControl, IItemTableView<T>,
	                                           IMenuManagerAware
		where T : class
	{
		[NotNull] private readonly Func<IEnumerable<T>> _getRows;
		[NotNull] private readonly TableState _state;
		[NotNull] private readonly Latch _latch = new Latch();
		[NotNull] private readonly BoundDataGridHandler<T> _gridHandler;
		[NotNull] private readonly TableStateManager<T> _stateManager;

		[CanBeNull] private IItemTableObserver<T> _observer;
		[NotNull] private SortableBindingList<T> _rows;
		[CanBeNull] private IMenuManager _menuManager;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemTableControl&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="getRows">The function to get rows.</param>
		/// <param name="state">The item table state.</param>
		/// <param name="hiddenProperties">The hidden properties.</param>
		public ItemTableControl([NotNull] Func<IEnumerable<T>> getRows,
		                        [NotNull] TableState state,
		                        params string[] hiddenProperties)
			: this(getRows, state, ColumnDescriptor.GetColumns<T>(), hiddenProperties) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemTableControl&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="getRows">The function to get rows.</param>
		/// <param name="state">The item table state.</param>
		/// <param name="columnDescriptors">The column descriptors.</param>
		/// <param name="hiddenProperties">The hidden properties.</param>
		public ItemTableControl([NotNull] Func<IEnumerable<T>> getRows,
		                        [NotNull] TableState state,
		                        [NotNull] IEnumerable<ColumnDescriptor> columnDescriptors,
		                        params string[] hiddenProperties)
		{
			Assert.ArgumentNotNull(getRows, nameof(getRows));
			Assert.ArgumentNotNull(columnDescriptors, nameof(columnDescriptors));
			Assert.ArgumentNotNull(state, nameof(state));

			_getRows = getRows;
			_state = state;

			InitializeComponent();

			_gridHandler = new BoundDataGridHandler<T>(
				_dataGridView,
				restoreSelectionAfterUserSort: true);

			new DataGridViewFindController(_dataGridView, _dataGridViewFindToolStrip);

			// configure the datagrid
			_dataGridView.SuspendLayout();

			_dataGridView.MultiSelect = true; //  false;
			_dataGridView.RowHeadersVisible = false;
			_dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			_dataGridView.BackgroundColor = _dataGridView.DefaultCellStyle.BackColor;

			_gridHandler.AddColumns(columnDescriptors, hiddenProperties);

			_rows = new SortableBindingList<T>(); // initialize empty, for NotNull

			bool sorted;
			UpdateRowsCore(state.TableSortState, out sorted);

			_stateManager = new TableStateManager<T>(_gridHandler, _dataGridViewFindToolStrip);
			_stateManager.ApplyState(state, sorted);

			_dataGridView.ResumeLayout(true);
		}

		#endregion

		public bool HideGridLines
		{
			get { return _dataGridView.CellBorderStyle == DataGridViewCellBorderStyle.None; }
			set
			{
				_dataGridView.CellBorderStyle = value
					                                ? DataGridViewCellBorderStyle.None
					                                : DataGridViewCellBorderStyle.Single;
			}
		}

		#region IItemsTableView<T> Members

		void IItemTableView<T>.UpdateRows()
		{
			_stateManager.SaveState(_state);

			bool sorted;
			UpdateRowsCore(_state.TableSortState, out sorted);

			_stateManager.ApplyState(_state, sorted);
		}

		void IItemTableView<T>.ShowItemCommands(Item item,
		                                        IList<Item> selectedChildren,
		                                        Point location)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(selectedChildren, nameof(selectedChildren));

			if (_menuManager == null)
			{
				return;
			}

			// add menu items to the context menu
			_contextMenuStrip.SuspendLayout();

			try
			{
				_contextMenuStrip.Items.Clear();
				_menuManager.AddMenuItems(_contextMenuStrip, item, selectedChildren);
			}
			finally
			{
				_contextMenuStrip.ResumeLayout(true);
			}

			// invoke context menu
			if (_contextMenuStrip.Items.Count > 0)
			{
				_contextMenuStrip.Show(this, location);
			}
		}

		ICollection<T> IItemTableView<T>.Rows => _rows;

		public ICollection<T> GetSelectedRows()
		{
			return _gridHandler.GetSelectedRows();
		}

		bool IItemTableView<T>.RemoveRow(T row)
		{
			return _rows.Remove(row);
		}

		IItemTableObserver<T> IItemTableView<T>.Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		#endregion

		#region IMenuManagerAware Members

		IMenuManager IMenuManagerAware.MenuManager
		{
			get { return _menuManager; }
			set { _menuManager = value; }
		}

		#endregion

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_stateManager.SaveState(_state);

				components?.Dispose();
			}

			base.Dispose(disposing);
		}

		private void UpdateRowsCore([CanBeNull] DataGridViewSortState sortState, out bool sorted)
		{
			// create sortable list
			_rows = new SortableBindingList<T>(GetRows());

			// assign the data source, use sortable list
			sorted = _latch.RunInsideLatch(
				() => _gridHandler.BindTo(_rows, sortStateOverride: sortState));
		}

		[NotNull]
		private IList<T> GetRows()
		{
			Stopwatch watch = _msg.IsVerboseDebugEnabled
				                  ? _msg.DebugStartTiming()
				                  : null;

			var result = new List<T>(_getRows());

			_msg.DebugStopTiming(watch, "retrieved {0} row(s)", result.Count);

			return result;
		}

		[CanBeNull]
		private T GetRow(int rowIndex)
		{
			return _dataGridView.Rows[rowIndex].DataBoundItem as T;
		}

		#region Event handlers

		private void _dataGridView_CellDoubleClick(object sender,
		                                           DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0)
			{
				return;
			}

			T row = GetRow(e.RowIndex);

			if (row == null)
			{
				return;
			}

			_observer?.RowDoubleClicked(row);
		}

		private void _dataGridView_CellMouseClick(object sender,
		                                          DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex < 0)
			{
				return;
			}

			if (e.Button != MouseButtons.Right)
			{
				return;
			}

			T row = GetRow(e.RowIndex);

			if (row == null)
			{
				return;
			}

			Point location = DataGridViewUtils.GetMouseLocation(_dataGridView, e);

			_observer?.RowRightClicked(row, location);
		}

		#endregion
	}
}

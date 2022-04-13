using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public partial class QualitySpecificationControl : UserControl,
	                                                   IQualitySpecificationView
	{
		[NotNull] private readonly ScreenBinder<QualitySpecification> _binder;
		[NotNull] private readonly Latch _latch = new Latch();

		[NotNull] private readonly BoundDataGridHandler<QualitySpecificationElementTableRow>
			_gridHandler;

		[CanBeNull] private static string _lastSelectedDetailsTab;
		[CanBeNull] private static IDictionary<string, int> _columnWidths;

		[CanBeNull]
		private TableStateManager<QualitySpecificationElementTableRow> _qconStateManager;

		[CanBeNull] private IList<QualitySpecificationElementTableRow> _initialTableRows;

		[NotNull] private readonly TableState _tableState;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationControl"/> class.
		/// </summary>
		public QualitySpecificationControl([NotNull] TableState tableState)
		{
			Assert.ArgumentNotNull(tableState, nameof(tableState));

			_tableState = tableState;

			InitializeComponent();

			_numericUpDownListOrder.Maximum = int.MaxValue;

			_gridHandler =
				new BoundDataGridHandler<QualitySpecificationElementTableRow>(
					_dataGridView, restoreSelectionAfterUserSort: true);
			_gridHandler.SelectionChanged += _gridHandler_SelectionChanged;

			new DataGridViewFindController(_dataGridView, _dataGridViewFindToolStrip);

			_binder = new ScreenBinder<QualitySpecification>(
				new ErrorProviderValidationMonitor(_errorProvider));

			_binder.Bind(m => m.Name)
			       .To(_textBoxName)
			       .WithLabel(_labelName);

			_binder.Bind(m => m.Description)
			       .To(_textBoxDescription)
			       .WithLabel(_labelDescription);

			_binder.Bind(m => m.Url)
			       .To(_textBoxUrl)
			       .WithLabel(_labelUrl);

			_binder.Bind(m => m.Uuid)
			       .To(_textBoxUuid)
			       .WithLabel(_labelUuid)
			       .AsReadOnly();

			_binder.Bind(m => m.ListOrder)
			       .To(_numericUpDownListOrder)
			       .WithLabel(_labelListOrder);

			_binder.Bind(m => m.Hidden)
			       .To(_checkBoxHidden);

			_binder.Bind(m => m.Notes)
			       .To(_textBoxNotes);

			_binder.AddElement(new NumericUpDownNullableElement(
				                   _binder.GetAccessor(m => m.TileSize),
				                   _numericUpDownTileSize));

			_binder.OnChange = BinderChanged;

			_dataGridView.AutoGenerateColumns = false;

			NullableBooleanItems.UseFor(_columnIssueType,
			                            trueText: "Warning",
			                            falseText: "Error");
			NullableBooleanItems.UseFor(_columnStopOnError);

			TabControlUtils.SelectTabPage(_tabControlDetails, _lastSelectedDetailsTab);

			if (_columnWidths != null)
			{
				DataGridViewUtils.RestoreColumnWidths(_dataGridView, _columnWidths);
			}
		}

		#endregion

		#region IBoundView<QualitySpecification,IViewObserver> Members

		[CanBeNull]
		public IQualitySpecificationObserver Observer { get; set; }

		public void BindTo(QualitySpecification target)
		{
			_binder.BindToModel(target);
		}

		#endregion

		#region IQualitySpecificationView Members

		void IQualitySpecificationView.SaveState()
		{
			_qconStateManager?.SaveState(_tableState);
			_columnWidths = DataGridViewUtils.GetColumnWidths(_dataGridView);
		}

		void IQualitySpecificationView.RenderCategory(string categoryText)
		{
			_textBoxCategory.Text = categoryText;

			if (categoryText != null)
			{
				_textBoxCategory.SelectionStart = categoryText.Length;
			}

			_toolTip.SetToolTip(_textBoxCategory, categoryText);
		}

		public bool HasSelectedElements => _gridHandler.HasSelectedRows;

		public int LastSelectedElementIndex => _gridHandler.LastSelectedRowIndex;

		public int ElementCount => _dataGridView.Rows.Count;

		public bool RemoveElementsEnabled
		{
			get { return _toolStripButtonRemoveQualityConditions.Enabled; }
			set { _toolStripButtonRemoveQualityConditions.Enabled = value; }
		}

		public bool AssignToCategoryEnabled
		{
			get { return _toolStripButtonAssignToCategory.Enabled; }
			set { _toolStripButtonAssignToCategory.Enabled = value; }
		}

		public bool HasSingleSelectedElement => _gridHandler.HasSingleSelectedRow;

		public IList<QualitySpecificationElementTableRow> GetSelectedElementTableRows()
		{
			return _gridHandler.GetSelectedRows();
		}

		public void RefreshElements()
		{
			_dataGridView.Refresh();
		}

		public void BindToElements(IList<QualitySpecificationElementTableRow> tableRows)
		{
			if (_qconStateManager == null)
			{
				// first time; initialize state manager, delay bind to tableRows to first paint event
				_qconStateManager =
					new TableStateManager<QualitySpecificationElementTableRow>(
						_gridHandler, _dataGridViewFindToolStrip);
				_initialTableRows = tableRows;
				return;
			}

			// already initialized. Save the current state, to reapply it after the bind
			_qconStateManager.SaveState(_tableState);

			BindTo(tableRows);
		}

		void IQualitySpecificationView.SelectElements(
			IEnumerable<QualitySpecificationElement> elementsToSelect)
		{
			Assert.ArgumentNotNull(elementsToSelect, nameof(elementsToSelect));

			var selectable = new HashSet<QualitySpecificationElement>(elementsToSelect);

			_latch.RunInsideLatch(() => _gridHandler.SelectRows(
				                      row => selectable.Contains(row.Element)));

			_qconStateManager?.SaveState(_tableState);

			OnSelectionChanged();
		}

		public IList<QualitySpecificationElement> GetSelectedElements()
		{
			return _gridHandler.GetSelectedRows().Select(row => row.Element).ToList();
		}

		#endregion

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
			Observer?.BinderChanged();
		}

		private void OnSelectionChanged()
		{
			Observer?.ElementSelectionChanged();
		}

		private void BindTo(
			[NotNull] IList<QualitySpecificationElementTableRow> tableRows)
		{
			Assert.NotNull(_qconStateManager, nameof(_qconStateManager));

			_latch.RunInsideLatch(
				delegate
				{
					bool sorted = _gridHandler.BindTo(
						tableRows,
						defaultSortState: new DataGridViewSortState(_columnName.Name),
						sortStateOverride: _tableState.TableSortState);

					_qconStateManager.ApplyState(_tableState, sorted);
				});
		}

		private void _toolStripButtonAssignQualityConditions_Click(object sender,
			EventArgs e)
		{
			Observer?.AddQualityConditionsClicked();
		}

		private void _toolStripButtonRemoveQualityConditions_Click(object sender,
			EventArgs e)
		{
			Observer?.RemoveElementsClicked();
		}

		private void _toolStripButtonAssignToCategory_Click(object sender, EventArgs e)
		{
			Observer?.AssignToCategoryClicked();
		}

		private void _gridHandler_SelectionChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			OnSelectionChanged();
		}

		private void _dataGridView_CellValueChanged(object sender,
		                                            DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
			{
				return;
			}

			Observer?.NotifyChanged(true);
		}

		private void _dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			_dataGridView.InvalidateRow(e.RowIndex);
		}

		private void _dataGridView_CellDoubleClick(object sender,
		                                           DataGridViewCellEventArgs e)
		{
			if (Observer == null)
			{
				return;
			}

			if (_dataGridView.IsCurrentCellInEditMode)
			{
				return; // ignore                
			}

			QualitySpecificationElementTableRow
				tableRow = _gridHandler.GetRow(e.RowIndex);

			if (tableRow != null)
			{
				Observer.ElementDoubleClicked(tableRow);
			}
		}

		private void _dataGridView_CellFormatting(object sender,
		                                          DataGridViewCellFormattingEventArgs e)
		{
			QualitySpecificationElementTableRow
				tableRow = _gridHandler.GetRow(e.RowIndex);

			if (tableRow != null && tableRow.InvolvesDeletedDatasets)
			{
				e.CellStyle.ForeColor = Color.Gray;
			}
		}

		private void _dataGridView_CellToolTipTextNeeded(object sender,
		                                                 DataGridViewCellToolTipTextNeededEventArgs
			                                                 e)
		{
			QualitySpecificationElementTableRow
				tableRow = _gridHandler.GetRow(e.RowIndex);

			if (tableRow != null && tableRow.InvolvesDeletedDatasets)
			{
				e.ToolTipText = "The quality condition is based on deleted datasets";
			}
		}

		private void _tabControlDetails_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedDetailsTab =
				TabControlUtils.GetSelectedTabPageName(_tabControlDetails);
		}

		private void _buttonOpenUrl_Click(object sender, EventArgs e)
		{
			Observer?.OpenUrlClicked();
		}

		private void QualitySpecificationControl_Paint(object sender, PaintEventArgs e)
		{
			// on the initial load, the bind to the table rows (applying stored state) must be delayed to 
			// the first paint event.
			// Otherwise:
			// - the selection is set to the first row (regardless of state)
			// - the "filter rows" setting is not correctly applied

			if (_initialTableRows != null)
			{
				var tableRows = _initialTableRows;
				_initialTableRows = null;

				BindTo(tableRows);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public partial class QualityConditionControl : UserControl, IQualityConditionView
	{
		[NotNull] private readonly ScreenBinder<QualityCondition> _binder;
		[NotNull] private readonly Latch _latch = new Latch();

		[NotNull] private readonly DataTable _parameterTbl;

		[NotNull]
		private readonly
			BoundDataGridHandler<QualitySpecificationReferenceTableRow> _qSpecGridHandler;

		[NotNull]
		private readonly
			BoundDataGridHandler<InstanceConfigurationReferenceTableRow> _issueFilterGridHandler;

		[CanBeNull] private static string _lastSelectedDetailsTab;
		[CanBeNull] private static string _lastSelectedParameterValuesTab;
		private readonly bool _tableViewShown;

		private TableStateManager<QualitySpecificationReferenceTableRow> _qSpecStateManager;
		private TableStateManager<InstanceConfigurationReferenceTableRow> _issueFilterStateManager;

		[NotNull] private readonly TableState _tableStateQSpec;
		[NotNull] private readonly TableState _tableStateIssueFilter;

		private IList<QualitySpecificationReferenceTableRow> _initialQSpecTableRows;
		private IList<InstanceConfigurationReferenceTableRow> _initialIssueFilterTableRows;

		[NotNull] private readonly IInstanceConfigurationTableViewControl _tableViewControl;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionControl"/> class.
		/// </summary>
		public QualityConditionControl(
			[NotNull] TableState tableStateQSpec,
			[NotNull] TableState tableStateIssueFilter,
			[NotNull] IInstanceConfigurationTableViewControl tableViewControl,
			bool ignoreLastDetailsTab = false)
		{
			Assert.ArgumentNotNull(tableStateQSpec, nameof(tableStateQSpec));
			Assert.ArgumentNotNull(tableViewControl, nameof(tableViewControl));

			_tableStateQSpec = tableStateQSpec;
			_tableStateIssueFilter = tableStateIssueFilter;
			_tableViewControl = tableViewControl;
			var qualityConditionTableViewControl = (Control)tableViewControl;

			InitializeComponent();

#if NET6_0_OR_GREATER
			// hack!
			_panelParameters.SuspendLayout();

			_panelParameters.Controls.RemoveByKey("_qualityConditionTableViewControlPanel");
			_panelParameters.Controls.RemoveByKey("_exportButtonPanel");

			_instanceParameterConfigControl.Visible = true;
			_instanceParameterConfigControl.AddBlazorControl(tableViewControl);

			_panelParameters.ResumeLayout(false);

			qualityConditionTableViewControl.ResumeLayout(false);
#endif

			NullableBooleanItems.UseFor(_columnIssueType,
										trueText: "Warning",
										falseText: "Error");
			NullableBooleanItems.UseFor(_columnStopOnError);

			_binder = new ScreenBinder<QualityCondition>(
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

			_binder.Bind(m => m.VersionUuid)
				   .To(_textBoxVersionUuid)
				   .WithLabel(_labelVersionUuid)
				   .AsReadOnly();

			_binder.Bind(m => m.Notes)
				   .To(_textBoxNotes);

			_binder.AddElement(new NullableBooleanComboboxElement(
								   _binder.GetAccessor(m => m.StopOnErrorOverride),
								   _nullableBooleanComboboxStopOnError));

			_binder.AddElement(new NullableBooleanComboboxElement(
								   _binder.GetAccessor(m => m.AllowErrorsOverride),
								   _nullableBooleanComboboxIssueType));

			_binder.AddElement(new ObjectReferenceScreenElement(
								   _binder.GetAccessor(m => m.TestDescriptor),
								   _objectReferenceControlTestDescriptor));

			_objectReferenceControlTestDescriptor.FormatTextDelegate =
				FormatTestDescriptor;

			_binder.Bind(m => m.NeverFilterTableRowsUsingRelatedGeometry)
				   .To(_checkBoxNeverFilterTableRowsUsingRelatedGeometry);

			_binder.Bind(m => m.NeverStoreRelatedGeometryForTableRowIssues)
				   .To(_checkBoxNeverStoreRelatedGeometryForTableRowIssues);

			_binder.OnChange = BinderChanged;

			_parameterTbl = TestParameterGridUtils.BindParametersDataGridView(
				_dataGridViewParamGrid);

			_propertyGrid.ToolbarVisible = false;

			// Make sure the item image is updated when allow error/stop on error comboboxes change 
			// (force Dirty notification) 
			// https://issuetracker02.eggits.net/browse/TOP-3667
			// NOTE: events must be wired *after* setting up the elements
			// (elements must handle the event first to apply the change to the target)
			_nullableBooleanComboboxIssueType.ValueChanged += delegate { NotifyDirty(); };
			_nullableBooleanComboboxStopOnError.ValueChanged += delegate { NotifyDirty(); };

			_qSpecGridHandler =
				new BoundDataGridHandler<QualitySpecificationReferenceTableRow>(
					_dataGridViewQualitySpecifications, restoreSelectionAfterUserSort: true);
			_qSpecGridHandler.SelectionChanged += _qSpecGridHandler_SelectionChanged;

			_issueFilterGridHandler =
				new BoundDataGridHandler<InstanceConfigurationReferenceTableRow>(
					_dataGridViewIssueFilters, restoreSelectionAfterUserSort: true);
			_issueFilterGridHandler.SelectionChanged += _issueFilterGridHandler_SelectionChanged;

			_binder.Bind(m => m.IssueFilterExpression)
				   .To(_textBoxFilterExpression)
				   .WithLabel(_labelFilterExpression);

			if (!ignoreLastDetailsTab)
			{
				TabControlUtils.SelectTabPage(_tabControlDetails, _lastSelectedDetailsTab);
				TabControlUtils.SelectTabPage(_tabControlParameterValues,
											  _lastSelectedParameterValuesTab);
			}

			_tableViewShown = _tabControlParameterValues.SelectedTab == _tabPageTableView;
		}

		#endregion

		#region IQualityConditionView Members

		public void SaveState()
		{
			_qSpecStateManager?.SaveState(_tableStateQSpec);
			_issueFilterStateManager?.SaveState(_tableStateIssueFilter);
		}

		Func<object> IQualityConditionView.FindTestDescriptorDelegate
		{
			get => _objectReferenceControlTestDescriptor.FindObjectDelegate;
			set => _objectReferenceControlTestDescriptor.FindObjectDelegate = value;
		}

		void IQualityConditionView.SetTestDescription(string value)
		{
			_textBoxDescGrid.Text = FormatNewLine(value);
		}

		void IQualityConditionView.SetParameterDescriptions(
			IList<TestParameter> testParameters)
		{
			TestParameterGridUtils.PopulateDataTable(_parameterTbl, testParameters);

			_dataGridViewParamGrid.ClearSelection();
		}

		bool IQualityConditionView.GoToTestDescriptorEnabled
		{
			get => _buttonGoToTestDescriptor.Enabled;
			set => _buttonGoToTestDescriptor.Enabled = value;
		}

		bool IQualityConditionView.ExportEnabled
		{
			get => _buttonExport.Enabled;
			set
			{
				_buttonExport.Enabled = value;

				if (value == false)
				{
					_exportButtonPanel.Height = 0;
				}
			}
		}

		bool IQualityConditionView.ImportEnabled
		{
			get => _buttonImport.Enabled;
			set
			{
				_buttonImport.Enabled = value;

				if (value == false)
				{
					_exportButtonPanel.Height = 0;
				}
			}
		}

		string IQualityConditionView.IssueTypeDefault
		{
			get => _textBoxIssueTypeDefault.Text;
			set => _textBoxIssueTypeDefault.Text = value;
		}

		string IQualityConditionView.StopOnErrorDefault
		{
			get => _textBoxStopOnErrorDefault.Text;
			set => _textBoxStopOnErrorDefault.Text = value;
		}

		bool IQualityConditionView.HasSelectedQualitySpecificationReferences
			=> _qSpecGridHandler.HasSelectedRows;

		bool IQualityConditionView.HasSelectedIssueFilter =>
			_issueFilterGridHandler.HasSelectedRows;

		public bool RemoveFromQualitySpecificationsEnabled
		{
			get => _toolStripButtonRemoveFromQualitySpecifications.Enabled;
			set => _toolStripButtonRemoveFromQualitySpecifications.Enabled = value;
		}

		public bool RemoveIssueFilterEnabled
		{
			get => _toolStripButtonRemoveIssueFilter.Enabled;
			set => _toolStripButtonRemoveIssueFilter.Enabled = value;
		}

		int IQualityConditionView.FirstQualitySpecificationReferenceIndex
			=> _qSpecGridHandler.FirstSelectedRowIndex;

		int IQualityConditionView.FirstIssueFilterIndex =>
			_issueFilterGridHandler.FirstSelectedRowIndex;

		void IQualityConditionView.BindToQualitySpecificationReferences(
			IList<QualitySpecificationReferenceTableRow> tableRows)
		{
			if (_qSpecStateManager == null)
			{
				// first time; initialize state manager, delay bind to tableRows to first paint event
				_qSpecStateManager =
					new TableStateManager<QualitySpecificationReferenceTableRow>(
						_qSpecGridHandler);
				_initialQSpecTableRows = tableRows;
				return;
			}

			// already initialized. Save the current state, to reapply it after the bind
			_qSpecStateManager.SaveState(_tableStateQSpec);

			BindTo(tableRows);
		}

		void IQualityConditionView.BindToIssueFilters(
			SortableBindingList<InstanceConfigurationReferenceTableRow> issueFilterTableRows)
		{
			if (_issueFilterStateManager == null)
			{
				// first time; initialize state manager, delay bind to tableRows to first paint event
				_issueFilterStateManager =
					new TableStateManager<InstanceConfigurationReferenceTableRow>(
						_issueFilterGridHandler);
				_initialIssueFilterTableRows = issueFilterTableRows;
				return;
			}

			// already initialized. Save the current state, to reapply it after the bind
			_issueFilterStateManager.SaveState(_tableStateIssueFilter);

			BindTo(issueFilterTableRows);
		}

		void IQualityConditionView.SelectQualitySpecifications(
			IEnumerable<QualitySpecification> specsToSelect)
		{
			Assert.ArgumentNotNull(specsToSelect, nameof(specsToSelect));

			var selectable = new HashSet<QualitySpecification>(specsToSelect);

			_latch.RunInsideLatch(
				() => _qSpecGridHandler.SelectRows(
					row => selectable.Contains(row.QualitySpecification)));

			_qSpecStateManager?.SaveState(_tableStateQSpec);

			OnSpecificationSelectionChanged();
		}

		void IQualityConditionView.SelectIssueFilters(
			IEnumerable<IssueFilterConfiguration> filtersToSelect)
		{
			Assert.ArgumentNotNull(filtersToSelect, nameof(filtersToSelect));

			var selectable = new HashSet<IssueFilterConfiguration>(filtersToSelect);

			_latch.RunInsideLatch(
				() => _issueFilterGridHandler.SelectRows(
					row => selectable.Contains((IssueFilterConfiguration)row.InstanceConfig)));

			_issueFilterStateManager?.SaveState(_tableStateIssueFilter);

			OnIssueFilterSelectionChanged();
		}

		string IQualityConditionView.QualitySpecificationSummary
		{
			get => _textBoxQualitySpecifications.Text;
			set => _textBoxQualitySpecifications.Text = value;
		}

		[NotNull]
		IInstanceConfigurationTableViewControl IQualityConditionView.TableViewControl =>
			_tableViewControl;

		IList<QualitySpecificationReferenceTableRow> IQualityConditionView.
			GetSelectedQualitySpecificationReferenceTableRows()
		{
			return _qSpecGridHandler.GetSelectedRows();
		}

		IList<InstanceConfigurationReferenceTableRow> IQualityConditionView.
			GetSelectedIssueFilterTableRows()
		{
			return _issueFilterGridHandler.GetSelectedRows();
		}

		public void SetConfigurator(ITestConfigurator configurator)
		{
			// configurator may be null

			if (_propertyGrid.SelectedObject is ITestConfigurator old)
			{
				old.DataChanged -= configurator_DataChanged;
			}

			try
			{
				_propertyGrid.SelectedObject = configurator;
			}
			catch (NullReferenceException)
			{
				// Bug in property Grid !?, do it again
				_propertyGrid.SelectedObject = configurator;
			}

			if (configurator != null)
			{
				configurator.DataChanged += configurator_DataChanged;
				_textBoxDescProps.Text = FormatNewLine(configurator.GetTestDescription());
			}
			else
			{
				_textBoxDescProps.Text = null;
			}

			_propertyGrid.ExpandAllGridItems();
		}

		public void BindToParameterValues(
			BindingList<ParameterValueListItem> parameterValueItems)
		{
			_tableViewControl.BindToParameterValues(parameterValueItems);
		}

		[CanBeNull]
		public IQualityConditionObserver Observer { get; set; }

		public void BindTo(QualityCondition target)
		{
			_binder.BindToModel(target);
		}

		void IQualityConditionView.RenderCategory(string categoryText)
		{
			_textBoxCategory.Text = categoryText;

			if (categoryText != null)
			{
				_textBoxCategory.SelectionStart = categoryText.Length;
			}

			_toolTip.SetToolTip(_textBoxCategory, categoryText);
		}

		bool IQualityConditionView.Confirm(string message, string title)
		{
			return Dialog.YesNo(this, title, message);
		}

		void IQualityConditionView.UpdateScreen()
		{
			_binder.UpdateScreen();
		}

		#endregion

		public void HideIssueFilterTab()
		{
			_tabControlDetails.TabPages.Remove(_tabPageIssueFilters);
		}

		private void BindTo(
			[NotNull] IList<QualitySpecificationReferenceTableRow> tableRows)
		{
			_latch.RunInsideLatch(
				() =>
				{
					bool sorted = _qSpecGridHandler.BindTo(
						tableRows,
						defaultSortState: new DataGridViewSortState(_columnName.Name),
						sortStateOverride: _tableStateQSpec.TableSortState);

					_qSpecStateManager.ApplyState(_tableStateQSpec, sorted);
				}
			);
		}

		private void BindTo(
			[NotNull] IList<InstanceConfigurationReferenceTableRow> tableRows)
		{
			_latch.RunInsideLatch(
				() =>
				{
					bool sorted = _issueFilterGridHandler.BindTo(
						tableRows,
						defaultSortState: new DataGridViewSortState(_dataGridIssueColumnName.Name),
						sortStateOverride: _tableStateIssueFilter.TableSortState);

					_issueFilterStateManager.ApplyState(_tableStateIssueFilter, sorted);
				}
			);
		}

		private static string FormatTestDescriptor(object obj)
		{
			var testDescriptor = obj as TestDescriptor;

			if (testDescriptor == null)
			{
				return $"{obj}";
			}

			try
			{
				IInstanceInfo instanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(testDescriptor);

				return $"{testDescriptor.Name} ( {InstanceUtils.GetTestSignature(instanceInfo)} )";
			}
			catch (Exception e)
			{
				return $"Error: {e.Message}";
			}
		}

		private void NotifyDirty()
		{
			Observer?.NotifyChanged(true);
		}

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
			Observer?.OnTestDescriptorChanged();
		}

		[NotNull]
		private static string FormatNewLine([CanBeNull] string value)
		{
			return value == null
					   ? string.Empty
					   : value.Replace("\n", "\r\n")
							  .Replace("\r\r", "\r");
		}

		private void OnSpecificationSelectionChanged()
		{
			Observer?.QualitySpecificationSelectionChanged();
		}

		private void OnIssueFilterSelectionChanged()
		{
			Observer?.IssueFilterSelectionChanged();
		}

		private void configurator_DataChanged(object sender, EventArgs e)
		{
			//ITestConfigurator configurator = (ITestConfigurator)sender;
			//_observer.SetTestParameterValues(configurator.GetTestParameterValues());

			Observer?.NotifyChanged(true);
		}

		private void _buttonExport_Click(object sender, EventArgs e)
		{
			if (saveFileDialogExport.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			using (new WaitCursor())
			{
				Observer?.ExportQualityCondition(saveFileDialogExport.FileName);
			}
		}

		private void _buttonImport_Click(object sender, EventArgs e)
		{
			if (openFileDialogImport.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			using (new WaitCursor())
			{
				Observer?.ImportQualityCondition(openFileDialogImport.FileName);
			}
		}

		private void _tabControlDetails_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedDetailsTab = TabControlUtils.GetSelectedTabPageName(_tabControlDetails);
		}

		private void _tabControlParameterValues_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_tabControlParameterValues.SelectedTab == tabPageProperties)
			{
				SetConfigurator(Observer?.GetTestConfigurator());
			}
			else if (_tabControlParameterValues.SelectedTab == _tabPageTableView)
			{
				BindToParameterValues(Observer?.GetTestParameterItems() ??
									  new BindingList<ParameterValueListItem>());

				if (!_tableViewShown)
				{
					_dataGridViewParamGrid.ClearSelection();
				}
			}

			_lastSelectedParameterValuesTab =
				TabControlUtils.GetSelectedTabPageName(_tabControlParameterValues);
		}

		private void _propertyGrid_PropertyValueChanged(object s,
														PropertyValueChangedEventArgs e)
		{
			var configurator = (ITestConfigurator)_propertyGrid.SelectedObject;
			if (_propertyGrid.SelectedGridItem.Expandable)
			{
				_propertyGrid.SelectedGridItem.Expanded = true;
			}

			Observer?.SetTestParameterValues(configurator.GetTestParameterValues());

			Observer?.NotifyChanged(true);
		}

		private void _toolStripButtonAssignToQualitySpecifications_Click(object sender, EventArgs e)
		{
			Observer?.AssignToQualitySpecificationsClicked();
		}

		private void _toolStripButtonRemoveQualityConditions_Click(object sender, EventArgs e)
		{
			Observer?.RemoveFromQualitySpecificationsClicked();
		}

		private void _toolStripButtonAddIssueFilter_Click(object sender, EventArgs e)
		{
			Observer?.AddIssueFilterClicked();
		}

		private void _toolStripButtonRemoveIssueFilter_Click(object sender, EventArgs e)
		{
			Observer?.RemoveIssueFilterClicked();
		}

		private void _qSpecGridHandler_SelectionChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			OnSpecificationSelectionChanged();
		}

		private void _issueFilterGridHandler_SelectionChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			OnIssueFilterSelectionChanged();
		}

		private void _dataGridViewQualitySpecifications_CellValueChanged(
			object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
			{
				return;
			}

			Observer?.NotifyChanged(true);
		}

		private void _dataGridViewIssueFilters_CellValueChanged(
			object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
			{
				return;
			}

			Observer?.NotifyChanged(true);
		}

		private void _dataGridViewQualitySpecifications_CellEndEdit(
			object sender, DataGridViewCellEventArgs e)
		{
			_dataGridViewQualitySpecifications.InvalidateRow(e.RowIndex);
		}

		private void _dataGridViewIssueFilters_CellEndEdit(object sender,
														   DataGridViewCellEventArgs e)
		{
			_dataGridViewIssueFilters.InvalidateRow(e.RowIndex);
		}

		private void _dataGridViewQualitySpecifications_CellDoubleClick(
			object sender, DataGridViewCellEventArgs e)
		{
			if (_dataGridViewQualitySpecifications.IsCurrentCellInEditMode)
			{
				return; // ignore
			}

			QualitySpecificationReferenceTableRow tableRow = _qSpecGridHandler.GetRow(e.RowIndex);

			if (tableRow != null)
			{
				Observer?.QualitySpecificationReferenceDoubleClicked(tableRow);
			}
		}

		private void _dataGridViewIssueFilters_CellDoubleClick(
			object sender, DataGridViewCellEventArgs e)
		{
			if (_dataGridViewIssueFilters.IsCurrentCellInEditMode)
			{
				return; // ignore
			}

			InstanceConfigurationReferenceTableRow tableRow =
				_issueFilterGridHandler.GetRow(e.RowIndex);

			if (tableRow != null)
			{
				Observer?.IssueFilterDoubleClicked(tableRow);
			}
		}

		private void _buttonGoToTestDescriptor_Clicked(object sender, EventArgs e)
		{
			var testDescriptor = _objectReferenceControlTestDescriptor.DataSource as TestDescriptor;

			Observer?.GoToTestDescriptorClicked(testDescriptor);
		}

		private void _buttonOpenUrl_Click(object sender, EventArgs e)
		{
			Observer?.OpenUrlClicked();
		}

		private void _buttonNewVersionUuid_Click(object sender, EventArgs e)
		{
			Observer?.NewVersionUuidClicked();
		}

		private void QualityConditionControl_Load(object sender, EventArgs e)
		{
			_dataGridViewParamGrid.ClearSelection();
		}

		private void _dataGridViewParamGrid_DataBindingComplete(object sender,
																DataGridViewBindingCompleteEventArgs
																	e)
		{
			_dataGridViewParamGrid.ClearSelection();
		}

		private void QualityConditionControl_Paint(object sender, PaintEventArgs e)
		{
			// on the initial load, the bind to the table rows (applying stored state) must be delayed to 
			// the first paint event.
			if (_initialQSpecTableRows != null)
			{
				var tableRows = _initialQSpecTableRows;
				_initialQSpecTableRows = null;

				BindTo(tableRows);
			}

			if (_initialIssueFilterTableRows != null)
			{
				var filterTableRows = _initialIssueFilterTableRows;

				_initialIssueFilterTableRows = null;
				BindTo(filterTableRows);
			}
		}

		private void _instanceParameterConfigControl_DocumentationLinkClicked(
			object sender, EventArgs e)
		{
			Observer?.DescriptorDocumentationLinkClicked();
		}

		private void _textBoxName_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			bool tabKeyPressed = e.KeyData == Keys.Tab;

			if (tabKeyPressed &&
				string.IsNullOrEmpty(_textBoxName.Text))
			{
				// Generate a name
				_textBoxName.Text = Observer?.GenerateName();
			}

			if (!tabKeyPressed ||
				!string.IsNullOrEmpty(_textBoxName.Text))
			{
				// Do not show the tooltip once the user has started typing or the name has bee generated:
				_toolTip.SetToolTip(_textBoxName, null);
			}
		}
	}
}

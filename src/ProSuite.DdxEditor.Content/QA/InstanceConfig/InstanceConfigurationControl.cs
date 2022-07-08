using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.Controls;
#if NET6_0_OR_GREATER
using System.Drawing;
#endif

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public partial class InstanceConfigurationControl : UserControl, IInstanceConfigurationView
	{
		[NotNull] private readonly ScreenBinder<InstanceConfiguration> _binder;
		[NotNull] private readonly Latch _latch = new Latch();

		[NotNull] private readonly DataTable _parameterTbl;

		[NotNull] private readonly
			BoundDataGridHandler<InstanceConfigurationReferenceTableRow> _qSpecGridHandler;

		[CanBeNull] private static string _lastSelectedDetailsTab;
		[CanBeNull] private static string _lastSelectedParameterValuesTab;
		private readonly bool _tableViewShown;

		private TableStateManager<InstanceConfigurationReferenceTableRow> _qSpecStateManager;

		[NotNull] private readonly TableState _tableState;
		private IList<InstanceConfigurationReferenceTableRow> _initialTableRows;

		[NotNull] private readonly IInstanceConfigurationTableViewControl _tableViewControl;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationControl"/> class.
		/// </summary>
		public InstanceConfigurationControl([NotNull] TableState tableState,
		                                    [NotNull]
		                                    IInstanceConfigurationTableViewControl tableViewControl)
		{
			Assert.ArgumentNotNull(tableState, nameof(tableState));
			Assert.ArgumentNotNull(tableViewControl, nameof(tableViewControl));

			_tableState = tableState;
			_tableViewControl = tableViewControl;
			var instanceConfigTableViewControl = (Control) tableViewControl;

#if NET6_0
			instanceConfigTableViewControl.SuspendLayout();
			instanceConfigTableViewControl.Dock = DockStyle.Fill;
			instanceConfigTableViewControl.Location = new Point(0, 0);
			instanceConfigTableViewControl.Name = "_instanceConfigTableViewControl";
			instanceConfigTableViewControl.Size = new Size(569, 123);
			instanceConfigTableViewControl.TabIndex = 0;
#endif

			InitializeComponent();

#if NET6_0
			// hack!
			_splitContainer.SuspendLayout();
			_instanceConfigTableViewControlPanel.SuspendLayout();

			_splitContainer.Panel2.Controls.Add(instanceConfigTableViewControl);
			_splitContainer.Size = new Size(569, 282);
			_splitContainer.SplitterDistance = 155;
			_instanceConfigTableViewControlPanel.Controls.RemoveByKey(
				"_tabControlParameterValues");
			_instanceConfigTableViewControlPanel.Controls.Add(_splitContainer);

			_splitContainer.ResumeLayout(false);
			instanceConfigTableViewControl.ResumeLayout(false);
			_instanceConfigTableViewControlPanel.ResumeLayout(false);
#endif

			_binder = new ScreenBinder<InstanceConfiguration>(
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

			//_binder.Bind(m => m.Notes)
			//       .To(_textBoxNotes);

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(m => m.InstanceDescriptor),
				                   _objectReferenceControlInstanceDescriptor));

			_objectReferenceControlInstanceDescriptor.FormatTextDelegate = FormatInstanceDescriptor;

			_binder.OnChange = BinderChanged;

			_parameterTbl = TestParameterGridUtils.BindParametersDataGridView(
				_dataGridViewParamGrid);

			_propertyGrid.ToolbarVisible = false;

			_qSpecGridHandler =
				new BoundDataGridHandler<InstanceConfigurationReferenceTableRow>(
					_dataGridViewReferences, restoreSelectionAfterUserSort: true);

			TabControlUtils.SelectTabPage(_tabControlDetails, _lastSelectedDetailsTab);
			TabControlUtils.SelectTabPage(_tabControlParameterValues,
			                              _lastSelectedParameterValuesTab);

			_tableViewShown = _tabControlParameterValues.SelectedTab == _tabPageTableView;
		}

		#endregion

		#region IInstanceConfigurationView Members

		public void SaveState()
		{
			_qSpecStateManager?.SaveState(_tableState);
		}

		Func<object> IInstanceConfigurationView.FindInstanceDescriptorDelegate
		{
			get => _objectReferenceControlInstanceDescriptor.FindObjectDelegate;
			set => _objectReferenceControlInstanceDescriptor.FindObjectDelegate = value;
		}

		void IInstanceConfigurationView.SetDescription(string value)
		{
			_textBoxDescGrid.Text = FormatNewLine(value);
		}

		void IInstanceConfigurationView.SetParameterDescriptions(
			IList<TestParameter> testParameters)
		{
			TestParameterGridUtils.PopulateDataTable(_parameterTbl, testParameters);

			_dataGridViewParamGrid.ClearSelection();
		}

		bool IInstanceConfigurationView.InstanceDescriptorLinkEnabled
		{
			get => ! _labelInstanceDescriptor.LinkArea.IsEmpty;
			set =>
				_labelInstanceDescriptor.LinkArea =
					value
						? new LinkArea(0, _labelInstanceDescriptor.Text.Length)
						: new LinkArea(0, 0);
		}

		void IInstanceConfigurationView.BindToQualityConditionReferences(
			IList<InstanceConfigurationReferenceTableRow> tableRows)
		{
			if (_qSpecStateManager == null)
			{
				// first time; initialize state manager, delay bind to tableRows to first paint event
				_qSpecStateManager =
					new TableStateManager<InstanceConfigurationReferenceTableRow>(
						_qSpecGridHandler);
				_initialTableRows = tableRows;
				return;
			}

			// already initialized. Save the current state, to reapply it after the bind
			_qSpecStateManager.SaveState(_tableState);

			BindTo(tableRows);
		}

		string IInstanceConfigurationView.ReferenceingInstancesSummary
		{
			get => _textBoxQualitySpecifications.Text;
			set => _textBoxQualitySpecifications.Text = value;
		}

		[NotNull]
		IInstanceConfigurationTableViewControl IInstanceConfigurationView.TableViewControl =>
			_tableViewControl;

		public void BindToParameterValues(
			BindingList<ParameterValueListItem> parameterValueItems)
		{
			_tableViewControl.BindToParameterValues(parameterValueItems);
		}

		[CanBeNull]
		public IInstanceConfigurationObserver Observer { get; set; }

		public void BindTo(InstanceConfiguration target)
		{
			_binder.BindToModel(target);
		}

		void IInstanceConfigurationView.RenderCategory(string categoryText)
		{
			_textBoxCategory.Text = categoryText;

			if (categoryText != null)
			{
				_textBoxCategory.SelectionStart = categoryText.Length;
			}

			_toolTip.SetToolTip(_textBoxCategory, categoryText);
		}

		#endregion

		private void BindTo(
			[NotNull] IList<InstanceConfigurationReferenceTableRow> tableRows)
		{
			_latch.RunInsideLatch(
				() =>
				{
					bool sorted = _qSpecGridHandler.BindTo(
						tableRows,
						defaultSortState: new DataGridViewSortState(_columnName.Name),
						sortStateOverride: _tableState.TableSortState);

					_qSpecStateManager.ApplyState(_tableState, sorted);
				}
			);
		}

		private static string FormatInstanceDescriptor(object obj)
		{
			var descriptor = obj as InstanceDescriptor;

			if (descriptor == null)
			{
				return $"{obj}";
			}

			try
			{
				ClassDescriptor instanceClass = descriptor.Class;

				if (instanceClass == null)
				{
					return $"{descriptor}";
				}

				var instanceInfo = new InstanceInfo(instanceClass.AssemblyName,
				                                    instanceClass.TypeName,
				                                    descriptor.ConstructorId);

				return $"{descriptor.Name} ( {InstanceUtils.GetTestSignature(instanceInfo)} )";
			}
			catch (Exception e)
			{
				return $"Error: {e.Message}";
			}
		}

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
			Observer?.OnInstanceDescriptorChanged();
		}

		[NotNull]
		private static string FormatNewLine([CanBeNull] string value)
		{
			return value == null
				       ? string.Empty
				       : value.Replace("\n", "\r\n")
				              .Replace("\r\r", "\r");
		}

		private void _tabControlDetails_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedDetailsTab = TabControlUtils.GetSelectedTabPageName(_tabControlDetails);
		}

		private void _tabControlParameterValues_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_tabControlParameterValues.SelectedTab == _tabPageTableView)
			{
				BindToParameterValues(Observer?.GetTestParameterItems() ??
				                      new BindingList<ParameterValueListItem>());

				if (! _tableViewShown)
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
			var configurator = (ITestConfigurator) _propertyGrid.SelectedObject;
			if (_propertyGrid.SelectedGridItem.Expandable)
			{
				_propertyGrid.SelectedGridItem.Expanded = true;
			}

			Observer?.SetTestParameterValues(configurator.GetTestParameterValues());

			Observer?.NotifyChanged(true);
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

		private void _dataGridViewQualitySpecifications_CellEndEdit(
			object sender, DataGridViewCellEventArgs e)
		{
			_dataGridViewReferences.InvalidateRow(e.RowIndex);
		}

		private void _dataGridViewQualitySpecifications_CellDoubleClick(
			object sender, DataGridViewCellEventArgs e)
		{
			if (_dataGridViewReferences.IsCurrentCellInEditMode)
			{
				return; // ignore
			}

			InstanceConfigurationReferenceTableRow tableRow = _qSpecGridHandler.GetRow(e.RowIndex);

			if (tableRow != null)
			{
				Observer?.InstanceReferenceDoubleClicked(tableRow);
			}
		}

		private void _labelInstanceDescriptor_LinkClicked(object sender,
		                                                  LinkLabelLinkClickedEventArgs e)
		{
			var descriptor =
				_objectReferenceControlInstanceDescriptor.DataSource as InstanceDescriptor;

			Observer?.InstanceDescriptorLinkClicked(descriptor);
		}

		private void _buttonOpenUrl_Click(object sender, EventArgs e)
		{
			Observer?.OpenUrlClicked();
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
			if (_initialTableRows != null)
			{
				var tableRows = _initialTableRows;
				_initialTableRows = null;

				BindTo(tableRows);
			}
		}
	}
}

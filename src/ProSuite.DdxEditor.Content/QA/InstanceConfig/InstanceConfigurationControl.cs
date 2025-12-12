using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.Controls;
#if NET6_0_OR_GREATER
using System.Drawing;
#endif

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public partial class InstanceConfigurationControl : UserControl, IInstanceConfigurationView
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly ScreenBinder<InstanceConfiguration> _binder;
		[NotNull] private readonly Latch _latch = new Latch();

		[NotNull] private readonly
			BoundDataGridHandler<InstanceConfigurationReferenceTableRow> _qSpecGridHandler;

		[CanBeNull] private static string _lastSelectedDetailsTab;

		private TableStateManager<InstanceConfigurationReferenceTableRow> _qSpecStateManager;

		[NotNull] private readonly TableState _tableState;
		private IList<InstanceConfigurationReferenceTableRow> _initialTableRows;

		[NotNull] private readonly IInstanceConfigurationTableViewControl _tableViewControl;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationControl"/> class.
		/// </summary>
		public InstanceConfigurationControl(
			[NotNull] TableState tableState,
			[NotNull] IInstanceConfigurationTableViewControl tableViewControl,
			bool ignoreLastDetailsTab = false)
		{
			Assert.ArgumentNotNull(tableState, nameof(tableState));
			Assert.ArgumentNotNull(tableViewControl, nameof(tableViewControl));

			_tableState = tableState;
			_tableViewControl = tableViewControl;
			var instanceConfigTableViewControl = (Control) tableViewControl;

#if NET6_0_OR_GREATER
			instanceConfigTableViewControl.SuspendLayout();
			instanceConfigTableViewControl.Dock = DockStyle.Fill;
			instanceConfigTableViewControl.Location = new Point(0, 0);
			instanceConfigTableViewControl.Name = "_instanceConfigTableViewControl";
			instanceConfigTableViewControl.Size = new Size(569, 123);
			instanceConfigTableViewControl.TabIndex = 0;
#endif

			InitializeComponent();

#if NET6_0_OR_GREATER
			// hack!
			_splitContainer.SuspendLayout();
			_instanceConfigTableViewControlPanel.SuspendLayout();

			_panelParametersEdit.Controls.Add(instanceConfigTableViewControl);

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

			_binder.Bind(m => m.Notes)
			       .To(_textBoxNotes);

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(m => m.InstanceDescriptor),
				                   _objectReferenceControlInstanceDescriptor));

			_objectReferenceControlInstanceDescriptor.FormatTextDelegate = FormatInstanceDescriptor;

			_binder.OnChange = BinderChanged;

			_qSpecGridHandler =
				new BoundDataGridHandler<InstanceConfigurationReferenceTableRow>(
					_dataGridViewReferences, restoreSelectionAfterUserSort: true);

			if (! ignoreLastDetailsTab)
			{
				TabControlUtils.SelectTabPage(_tabControlDetails, _lastSelectedDetailsTab);
			}
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
			// No legacy implementation for instance configuration control
		}

		bool IInstanceConfigurationView.GoToInstanceDescriptorEnabled
		{
			get => _buttonGoToInstanceDescriptor.Enabled;
			set => _buttonGoToInstanceDescriptor.Enabled = value;
		}

		void IInstanceConfigurationView.BindToInstanceConfigReferences(
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
				IInstanceInfo instanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(descriptor);

				string signature = instanceInfo == null
					                   ? "Error: Cannot create descriptor (missing class?)"
					                   : $"( {InstanceUtils.GetTestSignature(instanceInfo)} )";

				return $"{descriptor.Name} {signature}";
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

		private void _buttonGoToInstanceDescriptor_Clicked(object sender, EventArgs e)
		{
			var descriptor =
				_objectReferenceControlInstanceDescriptor.DataSource as InstanceDescriptor;

			Observer?.GoToInstanceDescriptorClicked(descriptor);
		}

		private void _buttonOpenUrl_Click(object sender, EventArgs e)
		{
			Observer?.OpenUrlClicked();
		}

		private void QualityConditionControl_Paint(object sender, PaintEventArgs e)
		{
			// on the initial load, the bind to the table rows (applying stored state) must be delayed to 
			// the first paint event.
			if (_initialTableRows != null)
			{
				try
				{
					var tableRows = _initialTableRows;
					_initialTableRows = null;

					BindTo(tableRows);
				}
				catch (Exception ex)
				{
					_msg.Warn("Error binding table rows", ex);
				}
			}
		}

		private void _linkDocumentation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
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

			if (! tabKeyPressed ||
			    ! string.IsNullOrEmpty(_textBoxName.Text))
			{
				// Do not show the tooltip once the user has started typing or the name has bee generated:
				_toolTip.SetToolTip(_textBoxName, null);
			}
		}
	}
}

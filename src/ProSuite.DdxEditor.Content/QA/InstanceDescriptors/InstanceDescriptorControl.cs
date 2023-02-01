using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	// TODO handle reflection errors
	// TODO handle obsolete constructors and classes (validation, defaults etc)
	// TODO Validation: check if implementation is being changed but referencing quality conditions exist
	public partial class InstanceDescriptorControl : UserControl, IInstanceDescriptorView
	{
		[NotNull] private readonly ScreenBinder<InstanceDescriptor> _binder;
		[NotNull] private readonly DataTable _parametersDataTable;

		private bool _suspend;

		[NotNull] private readonly BoundDataGridHandler<ReferencingInstanceConfigurationTableRow>
			_qConGridHandler;

		[NotNull] private readonly DataGridViewFindController _findController;
		[NotNull] private readonly ToolStripButton _toolStripButtonSelectFindResultRows;

		[NotNull] private readonly Latch _latch = new Latch();
		[NotNull] private readonly Latch _bindingLatch = new Latch();

		[CanBeNull] private static string _lastSelectedTab;

		private TableStateManager<ReferencingInstanceConfigurationTableRow> _qConStateManager;

		[NotNull] private readonly TableState _tableState;
		[CanBeNull] private IList<ReferencingInstanceConfigurationTableRow> _initialTableRows;

		// the same splitter distance is used for all test descriptors
		// NOTE large splitter distances do not restore correctly, since the control first renders with a reduced size 
		// and is only then docked into the container
		private static int _splitterDistance;

		private bool _loaded;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptorControl"/> class.
		/// </summary>
		public InstanceDescriptorControl([NotNull] TableState tableState)
		{
			Assert.ArgumentNotNull(tableState, nameof(tableState));

			_tableState = tableState;

			InitializeComponent();

			_binder = new ScreenBinder<InstanceDescriptor>(
				new ErrorProviderValidationMonitor(_errorProvider));

			//TestDescriptor td;
			_binder.Bind(m => m.Name)
			       .To(_textBoxName)
			       .WithLabel(_labelName);

			_binder.Bind(m => m.Description)
			       .To(_textBoxDescription)
			       .WithLabel(_labelDescription);

			//_binder.Bind(m => m.AllowErrors)
			//       .To(_booleanComboboxAllowErrors);

			//_binder.Bind(m => m.StopOnError)
			//       .To(_booleanComboboxStopOnError);

			//_binder.AddElement(new NumericUpDownNullableElement(
			//	                   _binder.GetAccessor(m => m.ExecutionPriority),
			//	                   _numericUpDownExecutionPriority));

			//_binder.AddElement(new ObjectReferenceScreenElement(
			//	                   _binder.GetAccessor(m => m.TestFactoryDescriptor),
			//	                   _objectReferenceControlTestFactory));

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(m => m.Class),
				                   _objectReferenceControlTestClass));

			//_binder.AddElement(new ObjectReferenceScreenElement(
			//	                   _binder.GetAccessor(m => m.TestConfigurator),
			//	                   _objectReferenceControlTestConfigurator));

			_objectReferenceControlTestClass.Changed += TestClass_Changed;
			_comboBoxConstructorIndex.SelectedIndexChanged +=
				TestConstructorIndex_Changed;

			_binder.OnChange = BinderChanged;

			// set up the parameters data table, and bind the grid to it
			_parametersDataTable =
				TestParameterGridUtils.BindParametersDataGridView(_dataGridViewParameter);

			_dataGridViewQualityConditions.AutoGenerateColumns = false;
			_qConGridHandler =
				new BoundDataGridHandler<ReferencingInstanceConfigurationTableRow>(
					_dataGridViewQualityConditions);

			// set up controller for find toolbar for quality conditions
			_findController = new DataGridViewFindController(
				_dataGridViewQualityConditions,
				_dataGridViewFindToolStrip);
			_findController.FindResultChanged += _findController_FindResultChanged;

			// add "Select Rows" button to find toolbar
			_toolStripButtonSelectFindResultRows =
				new ToolStripButton("Select Rows")
				{
					Enabled = false,
					ImageScaling = ToolStripItemImageScaling.None,
					Image = Resources.SelectAll
				};

			_toolStripButtonSelectFindResultRows.Click +=
				_toolStripButtonSelectFindResultRows_Click;

			_dataGridViewFindToolStrip.Items.Add(_toolStripButtonSelectFindResultRows);
			
			TabControlUtils.SelectTabPage(_tabControl, _lastSelectedTab);
		}

		#endregion

		void IInstanceDescriptorView.SaveState()
		{
			_qConStateManager.SaveState(_tableState);
		}

		void IInstanceDescriptorView.BindToInstanceConfigurations(
			IList<ReferencingInstanceConfigurationTableRow> tableRows)
		{
			if (_qConStateManager == null)
			{
				// first time; initialize state manager, delay bind to tableRows to first paint event
				_qConStateManager =
					new TableStateManager<ReferencingInstanceConfigurationTableRow>(
						_qConGridHandler, _dataGridViewFindToolStrip);
				_initialTableRows = tableRows;
				return;
			}

			// already initialized. Save the current state, to reapply it after the bind
			_qConStateManager.SaveState(_tableState);

			BindTo(tableRows);
		}

		IList<ReferencingInstanceConfigurationTableRow> IInstanceDescriptorView.
			GetSelectedInstanceConfigurationTableRows()
		{
			return _qConGridHandler.GetSelectedRows();
		}

		public Func<object> FindClassDelegate
		{
			get { return _objectReferenceControlTestClass.FindObjectDelegate; }
			set { _objectReferenceControlTestClass.FindObjectDelegate = value; }
		}

		public void BindTo(InstanceDescriptor target)
		{
			BindTestConstructor(target);

			_bindingLatch.RunInsideLatch(() => _binder.BindToModel(target));
		}

		public IInstanceDescriptorObserver Observer { get; set; }

		public void RefreshFactoryElements()
		{
			_bindingLatch.RunInsideLatch(() => _binder.UpdateScreen());

			_parametersDataTable.Clear();
		}

		public void RenderInstanceDescription(string value)
		{
			_textBoxTestDescription.WordWrap = true;
			_textBoxTestDescription.Text = value;
		}

		public void RenderInstanceCategories(string[] categories)
		{
			_textBoxCategories.Text = StringUtils.ConcatenateSorted(categories, ", ");
		}

		public void RenderTestParameters(IEnumerable<TestParameter> testParameters)
		{
			TestParameterGridUtils.PopulateDataTable(_parametersDataTable, testParameters);

			_dataGridViewParameter.AutoResizeRows();
			_dataGridViewParameter.ClearSelection();
		}

		private void BindTo([NotNull] IList<ReferencingInstanceConfigurationTableRow> tableRows)
		{
			_latch.RunInsideLatch(
				() =>
				{
					bool sorted = _qConGridHandler.BindTo(
						tableRows,
						defaultSortState: new DataGridViewSortState(_columnName.Name),
						sortStateOverride: _tableState.TableSortState);

					_qConStateManager.ApplyState(_tableState, sorted);
				}
			);
		}

		[NotNull]
		private static IEnumerable<TestConstructorItem> GetTestConstructorItems(
			[NotNull] Type testType)
		{
			foreach (int ctorIndex in InstanceUtils.GetConstructorIndexes(testType))
			{
				var testInfo = new InstanceInfo(testType, ctorIndex);

				string signature = InstanceUtils.GetTestSignature(testInfo);

				string formattedSignature = $"{ctorIndex}: {signature}";

				yield return new TestConstructorItem(ctorIndex, formattedSignature);
			}
		}

		private void BindTestConstructor([NotNull] InstanceDescriptor instanceDescriptor)
		{
			_binder.RemoveElementForControl(_comboBoxConstructorIndex);

			if (instanceDescriptor.Class == null)
			{
				_comboBoxConstructorIndex.Items.Clear();
				_comboBoxConstructorIndex.Enabled = false;

				return;
			}

			Picklist<TestConstructorItem> pickList;
			try
			{
				Type testType = instanceDescriptor.Class.GetInstanceType();

				pickList =
					new Picklist<TestConstructorItem>(GetTestConstructorItems(testType))
					{
						ValueMember = "Index",
						DisplayMember = "Signature"
					};
			}
			catch (Exception e)
			{
				_msg.WarnFormat(e.Message);

				pickList = new Picklist<TestConstructorItem>();
			}

			_binder.Bind(m => m.ConstructorId)
			       .To(_comboBoxConstructorIndex)
			       .FillWith(pickList)
			       .WithLabel(_labelConstructorId);
			_comboBoxConstructorIndex.Enabled = true;

			SetDropDownWidth(_comboBoxConstructorIndex);
		}

		private static void SetDropDownWidth([NotNull] ComboBox comboBox)
		{
			if (comboBox.IsHandleCreated)
			{
				comboBox.DropDownWidth = ComboBoxUtils.GetAutoFitDropDownWidth(comboBox);
			}
		}

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
		}

		private void UpdateImages()
		{
			// make sure the column exists (https://issuetracker02.eggits.net/browse/TOP-2717)
			// probably not needed (TOP-2717 no longer reproduces), but does no harm...

			if (_dataGridViewQualityConditions.Columns.Contains(
				    _columnImage))
			{
				_dataGridViewQualityConditions.InvalidateColumn(
					_columnImage.Index);
			}

			// by signaling a change the item image is updated
			Observer?.NotifyChanged(true);
		}

		#region Event handlers

		private void TestDescriptorControl_Load(object sender, EventArgs e)
		{
			if (_loaded)
			{
				return;
			}

			if (_splitterDistance > 0)
			{
				_splitContainerDescription.SplitterDistance = _splitterDistance;
			}

			SetDropDownWidth(_comboBoxConstructorIndex);

			_loaded = true;
		}

		private void TestClass_Changed(object sender, EventArgs e)
		{
			if (_bindingLatch.IsLatched)
			{
				return;
			}

			BindTestConstructor(_binder.Model);

			NotifyTestImplementationChanged();
		}

		private void TestConstructorIndex_Changed(object sender, EventArgs e)
		{
			NotifyTestImplementationChanged();
		}

		private void NotifyTestImplementationChanged()
		{
			if (Observer == null || _suspend)
			{
				return;
			}

			try
			{
				_suspend = true;
				Observer.NotifyFactoryChanged();
			}
			finally
			{
				_suspend = false;
			}
		}

		private void _tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedTab = TabControlUtils.GetSelectedTabPageName(_tabControl);

			if (_tabControl.SelectedIndex == 0)
			{
				SetDropDownWidth(_comboBoxConstructorIndex);
			}
		}

		private void _dataGridViewQualityConditions_CellEndEdit(object sender,
		                                                        DataGridViewCellEventArgs
			                                                        e)
		{
			_dataGridViewQualityConditions.InvalidateRow(e.RowIndex);
		}

		private void _dataGridViewQualityConditions_CellValueChanged(object sender,
			DataGridViewCellEventArgs
				e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
			{
				return;
			}

			Observer?.NotifyChanged(true);
		}

		private void _dataGridViewQualityConditions_CellDoubleClick(object sender,
			DataGridViewCellEventArgs
				e)
		{
			if (Observer == null)
			{
				return;
			}

			if (_dataGridViewQualityConditions.IsCurrentCellInEditMode)
			{
				return; // ignore
			}

			ReferencingInstanceConfigurationTableRow configRow =
				_qConGridHandler.GetRow(e.RowIndex);

			if (configRow != null)
			{
				Observer.InstanceConfigurationDoubleClicked(configRow);
			}
		}

		private void _toolStripButtonSelectFindResultRows_Click(
			object sender, EventArgs e)
		{
			_findController.SelectAllRows();
		}

		private void _findController_FindResultChanged(object sender, EventArgs e)
		{
			_toolStripButtonSelectFindResultRows.Enabled =
				_findController.FindResultCount > 0;
		}

		private void _splitContainerDescription_SplitterMoved(object sender,
		                                                      SplitterEventArgs e)
		{
			if (_loaded)
			{
				_splitterDistance = _splitContainerDescription.SplitterDistance;
			}
		}

		#endregion

		#region Nested types

		private class TestConstructorItem : IComparable<TestConstructorItem>, IComparable
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="TestConstructorItem"/> class.
			/// </summary>
			/// <param name="index">The index.</param>
			/// <param name="signature">The signature.</param>
			public TestConstructorItem(int index, [NotNull] string signature)
			{
				Index = index;
				Signature = signature;
			}

			[UsedImplicitly]
			public int Index { get; }

			[UsedImplicitly]
			public string Signature { get; }

			public int CompareTo(TestConstructorItem other)
			{
				if (other == null)
				{
					return -1;
				}

				return Index == other.Index
					       ? string.Compare(Signature, other.Signature,
					                        StringComparison.Ordinal)
					       : Index.CompareTo(other.Index);
			}

			public int CompareTo(object obj)
			{
				var other = obj as TestConstructorItem;

				return other == null
					       ? -1
					       : CompareTo(other);
			}
		}

		#endregion

		private void TestDescriptorControl_Paint(object sender, PaintEventArgs e)
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

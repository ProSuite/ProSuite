using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
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
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	// TODO handle reflection errors
	// TODO handle obsolete constructors and classes (validation, defaults etc)
	// TODO Validation: check if implementation is being changed but referencing quality conditions exist
	public partial class TestDescriptorControl : UserControl, ITestDescriptorView
	{
		[NotNull] private readonly ScreenBinder<TestDescriptor> _binder;
		[NotNull] private readonly DataTable _parametersDataTable;

		private bool _suspend;

		[NotNull] private readonly BoundDataGridHandler<ReferencingQualityConditionTableRow>
			_qConGridHandler;

		[NotNull] private readonly DataGridViewFindController _findController;
		[NotNull] private readonly ToolStripButton _toolStripButtonSelectFindResultRows;

		[NotNull] private readonly Latch _latch = new Latch();
		[NotNull] private readonly Latch _bindingLatch = new Latch();

		[CanBeNull] private static string _lastSelectedTab;

		private TableStateManager<ReferencingQualityConditionTableRow> _qConStateManager;

		[NotNull] private readonly TableState _tableState;
		[CanBeNull] private IList<ReferencingQualityConditionTableRow> _initialTableRows;

		// the same splitter distance is used for all test descriptors
		// NOTE large splitter distances do not restore correctly, since the control first renders with a reduced size 
		// and is only then docked into the container
		private static int _splitterDistance;

		private bool _loaded;

		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDescriptorControl"/> class.
		/// </summary>
		public TestDescriptorControl([NotNull] TableState tableState)
		{
			Assert.ArgumentNotNull(tableState, nameof(tableState));

			_tableState = tableState;

			InitializeComponent();

			_binder = new ScreenBinder<TestDescriptor>(
				new ErrorProviderValidationMonitor(_errorProvider));

			//TestDescriptor td;
			_binder.Bind(m => m.Name)
			       .To(_textBoxName)
			       .WithLabel(_labelName);

			_binder.Bind(m => m.Description)
			       .To(_textBoxDescription)
			       .WithLabel(_labelDescription);

			_binder.Bind(m => m.AllowErrors)
			       .To(_booleanComboboxAllowErrors);

			_binder.Bind(m => m.StopOnError)
			       .To(_booleanComboboxStopOnError);

			_binder.AddElement(new NumericUpDownNullableElement(
				                   _binder.GetAccessor(m => m.ExecutionPriority),
				                   _numericUpDownExecutionPriority));

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(m => m.TestFactoryDescriptor),
				                   _objectReferenceControlTestFactory));

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(m => m.TestClass),
				                   _objectReferenceControlTestClass));

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(m => m.TestConfigurator),
				                   _objectReferenceControlTestConfigurator));

			_objectReferenceControlTestClass.Changed += TestClass_Changed;
			_objectReferenceControlTestFactory.Changed += TestFactory_Changed;
			_objectReferenceControlTestConfigurator.Changed += TestConfigurator_Changed;
			_comboBoxConstructorIndex.SelectedIndexChanged +=
				TestConstructorIndex_Changed;

			// Make sure the item image and quality condition images are updated when 
			// allow error/stop on error comboboxes change (force Dirty notification) 
			// https://issuetracker02.eggits.net/browse/TOP-3666
			// NOTE: events must be wired *after* setting up the elements
			// (elements must handle the event first to apply the change to the target)
			_booleanComboboxAllowErrors.ValueChanged += delegate { UpdateImages(); };
			_booleanComboboxStopOnError.ValueChanged += delegate { UpdateImages(); };

			_binder.OnChange = BinderChanged;

			// set up the parameters data table, and bind the grid to it
			_parametersDataTable =
				TestParameterGridUtils.BindParametersDataGridView(_dataGridViewParameter);

			_dataGridViewQualityConditions.AutoGenerateColumns = false;
			_qConGridHandler =
				new BoundDataGridHandler<ReferencingQualityConditionTableRow>(
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

			// configure the nullable boolean columns in the quality conditions grid
			NullableBooleanItems.UseFor(_columnStopOnError);
			NullableBooleanItems.UseFor(_columnIssueType,
			                            trueText: "Warning",
			                            falseText: "Error");

			TabControlUtils.SelectTabPage(_tabControl, _lastSelectedTab);
		}

		#endregion

		void ITestDescriptorView.SaveState()
		{
			_qConStateManager.SaveState(_tableState);
		}

		void ITestDescriptorView.BindToQualityConditions(
			IList<ReferencingQualityConditionTableRow> tableRows)
		{
			if (_qConStateManager == null)
			{
				// first time; initialize state manager, delay bind to tableRows to first paint event
				_qConStateManager =
					new TableStateManager<ReferencingQualityConditionTableRow>(
						_qConGridHandler, _dataGridViewFindToolStrip);
				_initialTableRows = tableRows;
				return;
			}

			// already initialized. Save the current state, to reapply it after the bind
			_qConStateManager.SaveState(_tableState);

			BindTo(tableRows);
		}

		IList<ReferencingQualityConditionTableRow> ITestDescriptorView.
			GetSelectedQualityConditionTableRows()
		{
			return _qConGridHandler.GetSelectedRows();
		}

		public Func<object> FindTestFactoryDelegate
		{
			get { return _objectReferenceControlTestFactory.FindObjectDelegate; }
			set { _objectReferenceControlTestFactory.FindObjectDelegate = value; }
		}

		public Func<object> FindTestClassDelegate
		{
			get { return _objectReferenceControlTestClass.FindObjectDelegate; }
			set { _objectReferenceControlTestClass.FindObjectDelegate = value; }
		}

		public Func<object> FindTestConfiguratorDelegate
		{
			get { return _objectReferenceControlTestConfigurator.FindObjectDelegate; }
			set { _objectReferenceControlTestConfigurator.FindObjectDelegate = value; }
		}

		public void BindTo(TestDescriptor target)
		{
			BindTestConstructor(target);

			_bindingLatch.RunInsideLatch(() => _binder.BindToModel(target));
		}

		public ITestDescriptorObserver Observer { get; set; }

		public void RefreshFactoryElements()
		{
			_bindingLatch.RunInsideLatch(() => _binder.UpdateScreen());

			_parametersDataTable.Clear();
		}

		public void RenderTestDescription(string value)
		{
			_textBoxTestDescription.WordWrap = true;
			_textBoxTestDescription.Text = value;
		}

		public void RenderTestCategories(string[] categories)
		{
			_textBoxCategories.Text = StringUtils.ConcatenateSorted(categories, ", ");
		}

		public void RenderTestParameters(IEnumerable<TestParameter> testParameters)
		{
			TestParameterGridUtils.PopulateDataTable(_parametersDataTable, testParameters);

			_dataGridViewParameter.AutoResizeRows();
			_dataGridViewParameter.ClearSelection();
		}

		private void BindTo([NotNull] IList<ReferencingQualityConditionTableRow> tableRows)
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
			const bool includeObsolete = false;
			const bool includeInternallyUsed = false;

			foreach (int ctorIndex in InstanceFactoryUtils.GetConstructorIndexes(
				         testType, includeObsolete, includeInternallyUsed))
			{
				var testInfo = new InstanceInfo(testType, ctorIndex);

				string signature = InstanceUtils.GetTestSignature(testInfo);

				string formattedSignature = $"{ctorIndex}: {signature}";

				yield return new TestConstructorItem(ctorIndex, formattedSignature);
			}
		}

		private void BindTestConstructor([NotNull] TestDescriptor testDescriptor)
		{
			_binder.RemoveElementForControl(_comboBoxConstructorIndex);

			if (testDescriptor.TestClass == null)
			{
				_comboBoxConstructorIndex.Items.Clear();
				_comboBoxConstructorIndex.Enabled = false;

				return;
			}

			Picklist<TestConstructorItem> pickList;
			try
			{
				Type testType = testDescriptor.TestClass.GetInstanceType();

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

			_binder.Bind(m => m.TestConstructorId)
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
				    _columnQualityConditionImage))
			{
				_dataGridViewQualityConditions.InvalidateColumn(
					_columnQualityConditionImage.Index);
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

		private void TestFactory_Changed(object sender, EventArgs e)
		{
			if (_bindingLatch.IsLatched)
			{
				return;
			}

			BindTestConstructor(_binder.Model);

			NotifyTestImplementationChanged();
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

		private void TestConfigurator_Changed(object sender, EventArgs e)
		{
			// TODO specific notifications to observer
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

			ReferencingQualityConditionTableRow qcon =
				_qConGridHandler.GetRow(e.RowIndex);

			if (qcon != null)
			{
				Observer.QualityConditionDoubleClicked(qcon);
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	public partial class CustomizeQASpecForm :
		Form,
		IFormStateAware<CustomizeQASpecFormState>,
		ICustomizeQASpezificationView
	{
		/// <summary>
		/// Optional creator instance. In AO contexts, this should be set to an
		/// instance of ProSuite.UI.QA.Controls.QualityConditionTestConfigurationCreator.
		/// </summary>
		public ITestConfigurationCreator TestConfigurationCreator = null;

		#region nested classes

		private class DisplayModeItem
		{
			[NotNull]
			private string Name { get; }

			public DisplayMode Mode { get; }

			public DisplayModeItem([NotNull] string name, DisplayMode mode)
			{
				Name = name;
				Mode = mode;
			}

			public override string ToString()
			{
				return Name;
			}
		}

		private abstract class TreeConditionsView : IConditionsView
		{
			[CanBeNull] private QualitySpecification _qualitySpecification;
			private TreeNodeState _treeState;

			bool IConditionsView.FilterRows { get; set; }

			bool IConditionsView.MatchCase { get; set; }

			protected TreeConditionsView([NotNull] ConditionsLayerViewControl treeViewControl)
			{
				TreeViewControl = treeViewControl;
			}

			protected ConditionsLayerViewControl TreeViewControl { get; }

			public Control Control => TreeViewControl;

			protected void SetTreeState(TreeNodeState treeState)
			{
				_treeState = treeState;
				TreeViewControl.ApplyTreeState(_treeState);
			}

			public void RefreshAll()
			{
				TreeViewControl.RefreshAll();
			}

			public ICustomizeQASpezificationView CustomizeView
			{
				get { return TreeViewControl.CustomizeView; }
				set { TreeViewControl.CustomizeView = value; }
			}

			public ICollection<QualitySpecificationElement> GetSelectedElements()
			{
				return TreeViewControl.GetSelectedElements();
			}

			public ICollection<QualitySpecificationElement> GetFilteredElements()
			{
				return _qualitySpecification == null
						   ? new List<QualitySpecificationElement>()
						   : _qualitySpecification.Elements;
			}

			public void SetSelectedElements(ICollection<QualitySpecificationElement> selected,
											bool forceVisible)
			{
				TreeViewControl.SetSelectedElements(selected, forceVisible);
			}

			public void PushTreeState()
			{
				if (_treeState == null)
				{
					return;
				}

				TreeViewControl.PushTreeState(_treeState);
			}

			void IConditionsView.SetSpecification(QualitySpecification qualitySpecification)
			{
				_qualitySpecification = qualitySpecification;

				SetSpecificationCore(qualitySpecification);
			}

			protected abstract void SetSpecificationCore(
				[NotNull] QualitySpecification qualitySpecification);
		}

		private class QualityConditionView : TreeConditionsView
		{
			public QualityConditionView(ConditionsLayerViewControl treeViewControl)
				: base(treeViewControl) { }

			protected override void SetSpecificationCore(
				QualitySpecification qualitySpecification)
			{
				TreeViewControl.SetSpecificationByQualityCondition(qualitySpecification);
			}
		}

		private class DatasetsView : TreeConditionsView
		{
			public DatasetsView(ConditionsLayerViewControl treeViewControl)
				: base(treeViewControl) { }

			protected override void SetSpecificationCore(
				QualitySpecification qualitySpecification)
			{
				TreeViewControl.SetSpecificationByDatasets(qualitySpecification);
				SetTreeState(_datasetsTreeState);
			}
		}

		private class HierarchicDatasetsView : TreeConditionsView
		{
			public HierarchicDatasetsView(ConditionsLayerViewControl treeViewControl)
				: base(treeViewControl) { }

			protected override void SetSpecificationCore(
				QualitySpecification qualitySpecification)
			{
				TreeViewControl.SetSpecificationByHierarchicDatasets(qualitySpecification);
				SetTreeState(_hierarchicTreeState);
			}
		}

		private class CategoriesView : TreeConditionsView
		{
			public CategoriesView(ConditionsLayerViewControl treeViewControl)
				: base(treeViewControl) { }

			protected override void SetSpecificationCore(
				QualitySpecification qualitySpecification)
			{
				TreeViewControl.SetSpecificationByCategories(qualitySpecification);
				SetTreeState(_categoriesTreeState);
			}
		}

		#endregion

		private readonly FormStateManager<CustomizeQASpecFormState> _formStateManager;
		private QualitySpecification _qualitySpecification;

		private bool _setting;

		private ITestParameterDatasetProvider _testParameterDatasetProvider;

		private IConditionsView _conditionsView;
		[NotNull] private HashSet<QualitySpecificationElement> _selectedElements;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Font _partiallyEnabledFont;
		private readonly Font _allOrNothingEnabledFont;

		private static readonly TreeNodeState _datasetsTreeState = new TreeNodeState();
		private static readonly TreeNodeState _hierarchicTreeState = new TreeNodeState();
		private static readonly TreeNodeState _categoriesTreeState = new TreeNodeState();

		#region Constructors

		public CustomizeQASpecForm()
		{
			InitializeComponent();

			_selectedElements = new HashSet<QualitySpecificationElement>();

			ComboBox.ObjectCollection items = _toolStripComboBoxView.Items;
			items.Add(new DisplayModeItem("Quality Conditions",
										  DisplayMode.QualityConditionList));
			items.Add(new DisplayModeItem("Datasets", DisplayMode.DatasetList));
			// items.Add(new DisplayModeItem("Quality Conditions as Treeview", DisplayMode.Plain));
			items.Add(new DisplayModeItem("Quality Conditions by Dataset", DisplayMode.Layer));
			items.Add(new DisplayModeItem("Quality Conditions by Involved Datasets",
										  DisplayMode.Hierarchic));
			items.Add(new DisplayModeItem("Quality Conditions by Category",
										  DisplayMode.Category));

			_formStateManager = new FormStateManager<CustomizeQASpecFormState>(this);

			_panelConditions.Controls.Clear();

			_allOrNothingEnabledFont = _toolStripButtonWarningConditions.Font;
			_partiallyEnabledFont = new Font(_allOrNothingEnabledFont, FontStyle.Italic);
		}

		#endregion

		public void SetSpecification(
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] ITestParameterDatasetProvider testParameterDatasetProvider)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			_testParameterDatasetProvider = testParameterDatasetProvider;

			_qualitySpecification = qualitySpecification.GetCustomizable();
			_qualitySpecification.Name = qualitySpecification.Name;
		}

		[NotNull]
		public QualitySpecification GetSpecification()
		{
			var result = new CustomQualitySpecification(
				_qualitySpecification.BaseSpecification, _qualitySpecification.Name);

			foreach (QualitySpecificationElement element in _qualitySpecification.Elements)
			{
				QualitySpecificationElement resElem = result.AddElement(
					element.QualityCondition,
					element.StopOnErrorOverride,
					element.AllowErrorsOverride);
				resElem.Enabled = element.Enabled;
			}

			return result;
		}

		#region Non-public members

		#region Implementation of IFormStateAware<CustomizeQASpecFormState>

		public void RestoreState(CustomizeQASpecFormState formState)
		{
			if (formState.ListHeight > 0)
			{
				_splitContainerSpecification.SplitterDistance = formState.ListHeight;
			}

			if (formState.ListWidth > 0)
			{
				_splitContainerConditions.SplitterDistance = formState.ListWidth;
			}

			_conditionListControl.RestoreSortState(formState.ConditionsSortState);
			_conditionDatasetsControl.RestoreSortState(formState.DatasetsSortState);

			_matchCase = formState.MatchCase;
			_filterRows = formState.FilterRows;

			DisplayModeItem selected = null;
			foreach (object item in _toolStripComboBoxView.Items)
			{
				var dspItem = item as DisplayModeItem;
				if (dspItem?.Mode == formState.ActiveMode)
				{
					selected = dspItem;
				}
			}

			if (selected != null)
			{
				_toolStripComboBoxView.SelectedItem = selected;
			}
			else
			{
				_toolStripComboBoxView.SelectedIndex = 0;
			}
		}

		public void GetState(CustomizeQASpecFormState formState)
		{
			formState.ListHeight = _splitContainerSpecification.SplitterDistance;
			formState.ListWidth = _splitContainerConditions.SplitterDistance;

			formState.ActiveMode =
				(_toolStripComboBoxView.SelectedItem as DisplayModeItem)?.Mode ??
				DisplayMode.QualityConditionList;

			formState.ConditionsSortState = _conditionListControl.GetSortState();
			formState.DatasetsSortState = _conditionDatasetsControl.GetSortState();

			GetFilterSettingsFromVisibleControl();

			formState.MatchCase = _matchCase;
			formState.FilterRows = _filterRows;
		}

		#endregion

		private bool _matchCase;
		private bool _filterRows;

		private void GetFilterSettingsFromVisibleControl()
		{
			_matchCase = _conditionsView?.MatchCase ?? false;
			_filterRows = _conditionsView?.FilterRows ?? false;
			_conditionsView?.PushTreeState();
		}

		#region events

		private void CustomizeQASpecForm_Load(object sender, EventArgs e)
		{
			Try(nameof(CustomizeQASpecForm_Load), () =>
			{
				// restore in Load event due to splitter distance problem when maximized
				// http://social.msdn.microsoft.com/forums/en-US/winforms/thread/57f38145-b3b1-488d-8988-da8c397e4d80/
				_formStateManager.RestoreState();

				if (_toolStripComboBoxView.SelectedIndex < 0)
				{
					_toolStripComboBoxView.SelectedIndex = 0;
				}

				// ... and this is for http://social.msdn.microsoft.com/Forums/en/winformsdesigner/thread/ee6abc76-f35a-41a4-a1ff-5be942ae3425
				_splitContainer.Panel1MinSize = 200;
				_splitContainer.Panel2MinSize = 170;
				_splitContainerConditions.Panel1MinSize = 200;
				_splitContainerConditions.Panel2MinSize = 150;
				_splitContainerSpecification.Panel1MinSize = 200;
				_splitContainerSpecification.Panel2MinSize = 180;

				_textBoxSpecification.Text = _qualitySpecification.Name;
				_textBoxDescription.Text = _qualitySpecification.Description;

				// NOTE: without the next line, the first enabled element (right list) will be selected
				// --> condition details area is initially populated
				// _dataGridViewEnabledConditions.ClearSelection();

				SetSelectionFromEnabledConditions(forceVisibleInConditionsView: false);
			});
		}

		private void CustomizeQASpecForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			Try(nameof(CustomizeQASpecForm_FormClosed), () => _formStateManager.SaveState(), true);
		}

		private void CustomizeQASpecForm_KeyDown(object sender, KeyEventArgs e)
		{
			Try(nameof(CustomizeQASpecForm_KeyDown),
				() =>
				{
					if (e.KeyCode == Keys.Escape)
					{
						e.Handled = true;

						DialogResult = DialogResult.Cancel;
						Close();
					}
				}, true);
		}

		private void _toolStripButtonWarningConditions_CheckedChanged(object sender,
			EventArgs e)
		{
			Try(nameof(_toolStripButtonWarningConditions_CheckedChanged),
				() =>
				{
					ChangeEnabledElements(
						() => ApplyCheckedChanged(((ToolStripButton)sender).Checked,
												  QualityConditionType.Allowed));
				});
		}

		private void _toolStripButtonErrorConditions_CheckedChanged(object sender,
			EventArgs e)
		{
			Try(nameof(_toolStripButtonErrorConditions_CheckedChanged),
				() =>
				{
					ChangeEnabledElements(
						() => ApplyCheckedChanged(((ToolStripButton)sender).Checked,
												  QualityConditionType.ContinueOnError));
				});
		}

		private void _toolStripButtonStopConditions_CheckedChanged(object sender,
			EventArgs e)
		{
			Try(nameof(_toolStripButtonStopConditions_CheckedChanged),
				() =>
				{
					ChangeEnabledElements(
						() => ApplyCheckedChanged(((ToolStripButton)sender).Checked,
												  QualityConditionType.StopOnError));
				});
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			_conditionsView.PushTreeState();
			Close();
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void _dataGridViewEnabledConditions_SelectionChanged(object sender,
			EventArgs e)
		{
			Try(nameof(_dataGridViewEnabledConditions_SelectionChanged),
				() =>
				{
					if (_setting)
					{
						return;
					}

					SetSelectionFromEnabledConditions(forceVisibleInConditionsView: true);
				});
		}

		private void _groupBoxSelectedParameters_EnabledChanged(object sender, EventArgs e)
		{
			Try(nameof(_groupBoxSelectedParameters_EnabledChanged),
				() =>
				{
					if (_groupBoxSelectedParameters.Enabled)
					{
						return;
					}

					_qualityConditionControl.QualityCondition = null;
					_qualityConditionTableViewControl.SetQualityCondition(null);
					_testDescriptorControl.TestDescriptor = null;
				}, true);
		}

		private void _toolStripButtonEnableAll_Click(object sender, EventArgs e)
		{
			Try(nameof(_toolStripButtonEnableAll_Click), () => ChangeEnabledElements(CheckAll));
		}

		private void _toolStripButtonEnableNone_Click(object sender, EventArgs e)
		{
			Try(nameof(_toolStripButtonEnableNone_Click), () => ChangeEnabledElements(UncheckAll));
		}

		#endregion events

		private void ChangeEnabledElements([NotNull] Action procedure)
		{
			if (_setting || _qualitySpecification == null)
			{
				return;
			}

			bool oldSetting = _setting;
			_setting = true;

			try
			{
				procedure();

				RenderViewContent();
			}
			finally
			{
				_setting = oldSetting;
			}
		}

		private void RenderElement([CanBeNull] QualitySpecificationElement element)
		{
			QualityCondition qualityCondition = element?.QualityCondition;

			_qualityConditionControl.QualityCondition = qualityCondition;
			_qualityConditionTableViewControl.SetQualityCondition(qualityCondition);

			_testDescriptorControl.TestDescriptor = qualityCondition?.TestDescriptor;
			_toolStripButtonReset.Enabled = qualityCondition?.Updated ?? false;
		}

		void ICustomizeQASpezificationView.RenderViewContent()
		{
			try
			{
				_setting = true;
				RenderViewContent();
			}
			finally
			{
				_setting = false;
			}
		}

		// TODO call also when filter in view changes
		private void RenderViewContent()
		{
			InitUpdate(_toolStripButtonWarningConditions);
			InitUpdate(_toolStripButtonErrorConditions);
			InitUpdate(_toolStripButtonStopConditions);

			if (_qualitySpecification != null)
			{
				RenderEnabledElements();

				foreach (QualitySpecificationElement elem in
						 _conditionsView.GetFilteredElements())
				{
					if (elem.AllowErrors)
					{
						RenderCheckState(_toolStripButtonWarningConditions, elem);
					}
					else if (!elem.StopOnError)
					{
						RenderCheckState(_toolStripButtonErrorConditions, elem);
					}
					else
					{
						RenderCheckState(_toolStripButtonStopConditions, elem);
					}
				}
			}

			IConditionsView conditionsView = _conditionsView;
			if (conditionsView != null)
			{
				conditionsView.RefreshAll();
				var selectedElements = conditionsView.GetSelectedElements();
				if (selectedElements.Count == 0)
				{
					SetSelectionFromEnabledConditions(forceVisibleInConditionsView: false);
				}
				else
				{
					RenderSelection(selectedElements);
				}
			}
		}

		private void RenderEnabledElements()
		{
			List<SpecificationDataset> list =
				_qualitySpecification.Elements
									 .Where(e => e.Enabled && e.QualityCondition != null)
									 .OrderBy(e => e.QualityCondition.Name,
											  StringComparer.CurrentCulture)
									 .Select(e => new SpecificationDataset(e))
									 .ToList();

			var bindList = new SortableBindingList<SpecificationDataset>(list);

			_labelEnabledConditions.Text = string.Format(
				"{0} of {1} quality condition{2} enabled",
				list.Count,
				_qualitySpecification.Elements.Count,
				_qualitySpecification.Elements.Count == 1
					? string.Empty
					: "s");

			_bindingSourceEnabledConditions.DataSource = typeof(SpecificationDataset);
			_bindingSourceEnabledConditions.DataSource = bindList;
		}

		private void RenderCheckState([NotNull] ToolStripButton toolStripButton,
									  [NotNull] QualitySpecificationElement element)
		{
			if (!element.Enabled)
			{
				if (toolStripButton.Tag == null)
				{
					if (toolStripButton.CheckState != CheckState.Unchecked)
					{
						toolStripButton.CheckState = CheckState.Unchecked;
						toolStripButton.Font = _allOrNothingEnabledFont;
					}
				}
				else if (toolStripButton.CheckState != CheckState.Unchecked)
				{
					if (toolStripButton.CheckState != CheckState.Indeterminate)
					{
						toolStripButton.CheckState = CheckState.Indeterminate;
						toolStripButton.Font = _partiallyEnabledFont;
					}
				}
			}
			else if (toolStripButton.CheckState != CheckState.Checked)
			{
				if (toolStripButton.Tag != null)
				{
					if (toolStripButton.CheckState != CheckState.Indeterminate)
					{
						toolStripButton.CheckState = CheckState.Indeterminate;
						toolStripButton.Font = _partiallyEnabledFont;
					}
				}
			}

			toolStripButton.Tag = 1;
		}

		private void InitUpdate([NotNull] ToolStripButton toolStripButton)
		{
			toolStripButton.Tag = null;
			toolStripButton.CheckState = CheckState.Checked;
			toolStripButton.Font = _allOrNothingEnabledFont;
		}

		private void Render(DisplayMode displayMode)
		{
			_msg.VerboseDebug(() => $"Render({displayMode})");

			try
			{
				_setting = true;
				_panelConditions.Controls.Clear();
				if (_conditionsView != null)
				{
					GetFilterSettingsFromVisibleControl();
				}

				_conditionsView = GetConditionsView(displayMode);

				_conditionsView.CustomizeView = this;
				_conditionsView.FilterRows = _filterRows;
				_conditionsView.MatchCase = _matchCase;

				if (_qualitySpecification != null)
				{
					_conditionsView.SetSpecification(_qualitySpecification);
				}

				Control control = _conditionsView.Control;
				_panelConditions.Controls.Add(control);
				control.Dock = DockStyle.Fill;

				// synchronize selection, but don't change treenode collapsed state
				SynchronizeConditionsView(_selectedElements, forceVisible: false);

				RenderViewContent();
			}
			finally
			{
				_setting = false;
			}
		}

		[NotNull]
		private IConditionsView GetConditionsView(DisplayMode displayMode)
		{
			switch (displayMode)
			{
				case DisplayMode.QualityConditionList:
					return _conditionListControl;

				case DisplayMode.Plain:
					return new QualityConditionView(_conditionsLayerView);

				case DisplayMode.Layer:
					return new DatasetsView(_conditionsLayerView);

				case DisplayMode.Hierarchic:
					return new HierarchicDatasetsView(_conditionsLayerView);

				case DisplayMode.DatasetList:
					return _conditionDatasetsControl;

				case DisplayMode.Category:
					return new CategoriesView(_conditionsLayerView);

				default:
					throw new ArgumentOutOfRangeException(nameof(displayMode));
			}
		}

		private void RenderSelectedElements(
			[NotNull] ICollection<QualitySpecificationElement> elements)
		{
			bool oldSetting = _setting;

			try
			{
				_setting = true;
				var set = new HashSet<QualitySpecificationElement>(elements);

				if (elements.Count > 0)
				{
					foreach (DataGridViewRow row in _dataGridViewEnabledConditions.Rows)
					{
						SpecificationDataset specificationDataset =
							CustomizeUtils.GetSpecificationDataset(row);

						bool select = set.Contains(
							specificationDataset.QualitySpecificationElement);

						// ReSharper disable once RedundantCheckBeforeAssignment
						if (select != row.Selected)
						{
							row.Selected = select;
						}
					}
				}

				_selectedElements = set;

				//SynchronizeConditionsView(_selectedElements);

				DataGridViewUtils.EnsureRowSelectionIsVisible(_dataGridViewEnabledConditions);
			}
			finally
			{
				_setting = oldSetting;
			}
		}

		private void SetSelectionFromEnabledConditions(bool forceVisibleInConditionsView)
		{
			var selectedElements = new HashSet<QualitySpecificationElement>();

			foreach (DataGridViewRow selectedRow in
					 _dataGridViewEnabledConditions.SelectedRows)
			{
				selectedElements.Add(CustomizeUtils.GetSpecificationDataset(selectedRow)
												   .QualitySpecificationElement);
			}

			_selectedElements = selectedElements;

			SynchronizeConditionsView(_selectedElements,
									  forceVisible: forceVisibleInConditionsView);

			RenderSelectedElement();
		}

		private void SynchronizeConditionsView(
			[NotNull] ICollection<QualitySpecificationElement> selectedElements,
			bool forceVisible)
		{
			_conditionsView.SetSelectedElements(selectedElements, forceVisible);
		}

		private void RenderSelectedElement()
		{
			QualitySpecificationElement specificationElement =
				GetSingleSelectedSpecificationElement();

			_msg.VerboseDebug(
				() =>
					$"RenderSelectedElement: {(specificationElement?.QualityCondition == null ? "no condition" : specificationElement.QualityCondition.Name)}");

			if (specificationElement == null)
			{
				_qualityConditionControl.Clear();
				_qualityConditionTableViewControl.SetQualityCondition(null);
				_groupBoxSelectedParameters.Enabled = false;
				_toolStripButtonCustomizeTestParameterValues.Enabled = false;
				_toolStripButtonReset.Enabled = false;
			}
			else
			{
				RenderElement(specificationElement);
				_groupBoxSelectedParameters.Enabled = true;
				_toolStripButtonCustomizeTestParameterValues.Enabled = _testParameterDatasetProvider != null;
			}
		}

		void ICustomizeQASpezificationView.RenderConditionsViewSelection(
			ICollection<QualitySpecificationElement> qualitySpecificationElements)
		{
			RenderSelection(qualitySpecificationElements);
		}

		private void RenderSelection(
			[NotNull] ICollection<QualitySpecificationElement> qualitySpecificationElements)
		{
			RenderSelectedElements(qualitySpecificationElements);

			RenderSelectedElement();
		}

		[CanBeNull]
		private QualitySpecificationElement GetSingleSelectedSpecificationElement()
		{
			return _selectedElements.Count == 1
					   ? _selectedElements.First()
					   : null;
		}

		private void CheckAll()
		{
			// TODO in case of data grid: only the ones that match the filter (if any)
			foreach (QualitySpecificationElement element in _conditionsView.GetFilteredElements())
			{
				element.Enabled = true;
			}
		}

		private void UncheckAll()
		{
			// TODO in case of data grid: only the ones that match the filter (if any)
			foreach (QualitySpecificationElement element in _conditionsView.GetFilteredElements())
			{
				element.Enabled = false;
			}
		}

		private void ApplyCheckedChanged(bool check, QualityConditionType type)
		{
			foreach (QualitySpecificationElement element in
					 _conditionsView.GetFilteredElements())
			{
				if (element.AllowErrors)
				{
					if (type == QualityConditionType.Allowed)
					{
						element.Enabled = check;
					}
				}
				else if (element.StopOnError == false)
				{
					if (type == QualityConditionType.ContinueOnError)
					{
						element.Enabled = check;
					}
				}
				else if (element.StopOnError)
				{
					if (type == QualityConditionType.StopOnError)
					{
						element.Enabled = check;
					}
				}
				else
				{
					throw new ArgumentException("Unhandled ErrorType " + type);
				}
			}
		}

		#endregion

		private void _toolStripButtonCustomizeTestParameterValues_Click(object sender,
			EventArgs e)
		{
			Try(nameof(_toolStripButtonCustomizeTestParameterValues_Click),
				() =>
				{
					QualitySpecificationElement specificationElement =
						GetSingleSelectedSpecificationElement();
					if (specificationElement?.QualityCondition == null)
					{
						return;
					}

					QualityCondition editCopy =
						(QualityCondition)specificationElement.QualityCondition.CreateCopy();

					using (var form = new TestParameterValuesEditorForm())
					{
						form.TestConfigurationCreator = TestConfigurationCreator;
						form.SetQualityCondition(editCopy, _testParameterDatasetProvider);
						if (form.ShowDialog(this) != DialogResult.OK)
						{
							return;
						}
					}

					specificationElement.QualityCondition.UpdateParameterValuesFrom(editCopy);

					RenderElement(specificationElement);
					Refresh();
				});
		}

		private void _toolStripButtonReset_Click(object sender, EventArgs e)
		{
			Try(nameof(_toolStripButtonReset_Click),
				() =>
				{
					QualitySpecificationElement specificationElement =
						GetSingleSelectedSpecificationElement();

					if (specificationElement?.QualityCondition == null)
					{
						return;
					}

					foreach (QualitySpecificationElement element in
							 _qualitySpecification.BaseSpecification.GetCustomizable().Elements)
					{
						if (element.QualityCondition.Uuid !=
							specificationElement.QualityCondition.Uuid)
						{
							continue;
						}

						specificationElement.QualityCondition.UpdateParameterValuesFrom(
							element.QualityCondition, updateIsOriginal: true);
					}

					RenderElement(specificationElement);
					Refresh();
				});
		}

		private void _toolStripComboBoxView_SelectedIndexChanged(object sender, EventArgs e)
		{
			Try(nameof(_toolStripComboBoxView_SelectedIndexChanged),
				() =>
				{
					var item = _toolStripComboBoxView.SelectedItem as DisplayModeItem;
					if (item != null)
					{
						Render(item.Mode);
					}
				});
		}

		private void _dataGridViewEnabledConditions_CellFormatting(
			object sender,
			DataGridViewCellFormattingEventArgs e)
		{
			Try(nameof(_dataGridViewEnabledConditions_CellFormatting),
				() =>
				{
					Font font = CustomizeUtils.GetFont(
						GetSpecificationDataset((DataGridView)sender, e)?.QualityCondition,
						e.CellStyle.Font);

					if (font != null)
					{
						e.CellStyle.Font = font;
					}
				}, true);
		}

		[CanBeNull]
		private static SpecificationDataset GetSpecificationDataset(
			[NotNull] DataGridView dataGridView, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.RowIndex >= dataGridView.RowCount)
			{
				return null;
			}

			return dataGridView.Rows[e.RowIndex].DataBoundItem as SpecificationDataset;
		}

		private static void Try([NotNull] string method, [NotNull] Action procedure,
								bool suppressMessageBox = false)
		{
			try
			{
				_msg.VerboseDebug(() => $"{nameof(CustomizeQASpecForm)}.{method}");

				procedure();
			}
			catch (Exception e)
			{
				if (suppressMessageBox)
				{
					_msg.Error(ExceptionUtils.FormatMessage(e), e);
				}
				else
				{
					ErrorHandler.HandleError(e, _msg);
				}
			}
		}
	}
}

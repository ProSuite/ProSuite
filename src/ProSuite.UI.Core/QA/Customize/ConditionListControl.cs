using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.Properties;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	public partial class ConditionListControl : UserControl, IConditionsView
	{
		#region Nested type: QualitySpecificationElementItem

		private class QualitySpecificationElementItem
		{
			private static readonly Image _allowImage =
				(Bitmap) TestTypeImages.TestTypeWarning.Clone();

			private static readonly Image _continueImage =
				(Bitmap) TestTypeImages.TestTypeError.Clone();

			private static readonly Image _stopImage =
				(Bitmap) TestTypeImages.TestTypeStop.Clone();

			static QualitySpecificationElementItem()
			{
				_allowImage.Tag = QualityConditionType.Allowed;
				_continueImage.Tag = QualityConditionType.ContinueOnError;
				_stopImage.Tag = QualityConditionType.StopOnError;
			}

			public QualitySpecificationElementItem([NotNull] QualitySpecificationElement element)
			{
				Element = element;

				QualityCondition qualityCondition = element.QualityCondition;
				TestDescriptor testDescriptor = qualityCondition.TestDescriptor;

				ConditionName = qualityCondition.Name;
				Category = qualityCondition.Category?.GetQualifiedName();
				TestDescriptorName = testDescriptor.Name;
				TestCategories = GetTestCategories(testDescriptor);
				DatasetNames = GetDatasetNames(qualityCondition);
			}

			[NotNull]
			public QualitySpecificationElement Element { get; }

			[UsedImplicitly]
			public bool Enabled
			{
				get => Element.Enabled;
				set => Element.Enabled = value;
			}

			[UsedImplicitly]
			public Image Type => Element.AllowErrors
				                     ? _allowImage
				                     : Element.StopOnError
					                     ? _stopImage
					                     : _continueImage;

			[UsedImplicitly]
			public string ConditionName { get; }

			[UsedImplicitly]
			public string Category { get; }

			[UsedImplicitly]
			public string TestDescriptorName { get; }

			[UsedImplicitly]
			public string TestCategories { get; }

			[UsedImplicitly]
			public string DatasetNames { get; }

			[NotNull]
			private static string GetTestCategories([NotNull] TestDescriptor testDescriptor)
			{
				var testInfo = InstanceDescriptorUtils.GetInstanceInfo(testDescriptor);

				return testInfo == null
					       ? string.Empty
					       : StringUtils.ConcatenateSorted(testInfo.TestCategories, ", ");
			}

			[NotNull]
			private static string GetDatasetNames([NotNull] QualityCondition qualityCondition)
			{
				var names = new SimpleSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(false, true))
				{
					names.TryAdd(dataset.Name);
				}

				var list = new List<string>(names);

				list.Sort();

				return StringUtils.Concatenate(list, ", ");
			}
		}

		#endregion

		#region Nested type: QualitySpecificationElementComparer

		private class
			QualitySpecificationElementComparer : IComparer<QualitySpecificationElementItem>
		{
			[NotNull] private readonly SpecificationDatasetComparer _specComparer =
				new SpecificationDatasetComparer();

			public int Compare(QualitySpecificationElementItem x, QualitySpecificationElementItem y)
			{
				if (x == null && y == null)
				{
					return 0;
				}

				if (x == null)
				{
					return -1;
				}

				if (y == null)
				{
					return 1;
				}

				return _specComparer.Compare(new SpecificationDataset(x.Element),
				                             new SpecificationDataset(y.Element));
			}
		}

		#endregion

		[NotNull] private readonly DataGridViewFindController _findController;
		private QualitySpecification _qualitySpecification;
		private bool _setting;
		private bool _wasFiltered;

		public ConditionListControl()
		{
			InitializeComponent();

			_findController = new DataGridViewFindController(_dataGridViewAllConditions,
			                                                 _dataGridViewFindToolStrip)
			                  {
				                  CanFilterRows = true
			                  };
			_findController.FindResultChanged += _findController_FindResultChanged;
		}

		Control IConditionsView.Control => this;

		public bool FilterRows
		{
			get { return _findController.FilterRows; }
			set
			{
				if (_findController.CanFilterRows)
				{
					_findController.FilterRows = value;
				}
			}
		}

		public bool MatchCase
		{
			get { return _dataGridViewFindToolStrip.MatchCase; }
			set { _dataGridViewFindToolStrip.MatchCase = value; }
		}

		void IConditionsView.PushTreeState() { }

		public ICustomizeQASpezificationView CustomizeView { get; set; }

		public void SetSpecification(QualitySpecification qualitySpecification)
		{
			_setting = true;
			try
			{
				_qualitySpecification = qualitySpecification;

				List<QualitySpecificationElementItem> list =
					_qualitySpecification.Elements
					                     .Select(e => new QualitySpecificationElementItem(e))
					                     .OrderBy(e => e, new QualitySpecificationElementComparer())
					                     .ToList();

				DataGridViewUtils.BindTo(_dataGridViewAllConditions,
				                         new SortableBindingList<QualitySpecificationElementItem>(
					                         list));

				_dataGridViewAllConditions.Visible = true;
			}
			finally
			{
				_setting = false;
			}
		}

		public void RestoreSortState([CanBeNull] DataGridViewSortState sortState)
		{
			if (sortState == null)
			{
				sortState = new DataGridViewSortState(_columnQualityCondition.Name);
			}

			sortState.TryApplyState(_dataGridViewAllConditions);
		}

		[NotNull]
		public DataGridViewSortState GetSortState()
		{
			return new DataGridViewSortState(_dataGridViewAllConditions);
		}

		private void _dataGridViewAllConditions_CellValueChanged(object sender,
		                                                         DataGridViewCellEventArgs e)
		{
			if (_setting || e.RowIndex < 0)
			{
				return;
			}

			try
			{
				_setting = true;

				Debug.Assert(e.ColumnIndex == _columnEnabled.Index,
				             "Error in software design assumption");

				QualitySpecificationElement element = GetQualitySpecificationElement(
					_dataGridViewAllConditions.Rows[e.RowIndex]);

				bool enable = element?.Enabled ?? false;
				foreach (DataGridViewRow row in _dataGridViewAllConditions.SelectedRows)
				{
					element = GetQualitySpecificationElement(row);
					if (element != null)
					{
						element.Enabled = enable;
					}
				}

				_dataGridViewAllConditions.Invalidate();
				_dataGridViewAllConditions.Refresh();

				CustomizeView?.RenderViewContent();
			}
			finally
			{
				_setting = false;
			}
		}

		private void _dataGridViewAllConditions_CellMouseUp(object sender,
		                                                    DataGridViewCellMouseEventArgs e)
		{
			_dataGridViewAllConditions.CommitEdit(DataGridViewDataErrorContexts.Commit);
			_dataGridViewAllConditions.EndEdit();
		}

		private void _dataGridViewAllConditions_SelectionChanged(object sender, EventArgs e)
		{
			if (_setting)
			{
				return;
			}

			CustomizeView?.RenderConditionsViewSelection(GetSelectedElements());
		}

		public ICollection<QualitySpecificationElement> GetSelectedElements()
		{
			return _dataGridViewAllConditions.SelectedRows
			                                 .Cast<DataGridViewRow>()
			                                 .Select(GetQualitySpecificationElement)
			                                 .Where(element => element != null)
			                                 .ToList();
		}

		public ICollection<QualitySpecificationElement> GetFilteredElements()
		{
			return _dataGridViewAllConditions.Rows
			                                 .Cast<DataGridViewRow>()
			                                 .Where(row => row.Visible)
			                                 .Select(GetQualitySpecificationElement)
			                                 .Where(element => element != null)
			                                 .ToList();
		}

		public void SetSelectedElements(ICollection<QualitySpecificationElement> selected,
		                                bool forceVisible)
		{
			bool oldSetting = _setting;
			try
			{
				_setting = true;
				foreach (DataGridViewRow row in _dataGridViewAllConditions.Rows)
				{
					QualitySpecificationElement element = GetQualitySpecificationElement(row);

					bool select = selected.Contains(element);
					if (row.Selected != select)
					{
						row.Selected = select;
					}
				}

				DataGridViewUtils.EnsureRowSelectionIsVisible(_dataGridViewAllConditions);
			}
			finally
			{
				_setting = oldSetting;
			}
		}

		[CanBeNull]
		private static QualitySpecificationElement GetQualitySpecificationElement(
			[NotNull] DataGridViewRow row)
		{
			var selected = row.DataBoundItem as QualitySpecificationElementItem;
			return selected?.Element;
		}

		public void RefreshAll()
		{
			Invalidate();
			_dataGridViewAllConditions.Invalidate();
		}

		private void _dataGridViewAllConditions_CellFormatting(object sender,
		                                                       DataGridViewCellFormattingEventArgs
			                                                       e)
		{
			Font font = CustomizeUtils.GetFont(
				GetQualityCondition((DataGridView) sender, e),
				e.CellStyle.Font);

			if (font != null)
			{
				e.CellStyle.Font = font;
			}
		}

		private void _findController_FindResultChanged(object sender, EventArgs e)
		{
			if (FilterRows || FilterRows != _wasFiltered)
			{
				CustomizeView?.RenderViewContent();
			}

			_wasFiltered = FilterRows;
		}

		[CanBeNull]
		private static QualityCondition GetQualityCondition(
			[NotNull] DataGridView dataGridView, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.RowIndex >= dataGridView.RowCount)
			{
				return null;
			}

			var viewmodel =
				dataGridView.Rows[e.RowIndex].DataBoundItem as QualitySpecificationElementItem;
			return viewmodel?.Element.QualityCondition;
		}
	}
}

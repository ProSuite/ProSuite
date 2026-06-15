using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	public partial class ConditionDatasetsControl : UserControl, IConditionsView
	{
		[CanBeNull] private QualitySpecification _qualitySpecification;
		private bool _setting;
		[NotNull] private readonly DataGridViewFindController _findController;
		private bool _wasFiltered;

		public ConditionDatasetsControl()
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

				var list = new List<SpecificationDataset>();

				foreach (QualitySpecificationElement element in _qualitySpecification.Elements)
				{
					list.AddRange(SpecificationDataset.CreateList(element));
				}

				list.Sort(new SpecificationDatasetComparer());

				var bindList = new SortableBindingList<SpecificationDataset>(list);

				DataGridViewUtils.BindTo(_dataGridViewAllConditions, bindList);

				_dataGridViewAllConditions.Visible = true;
			}
			finally
			{
				_setting = false;
			}
		}

		public void SetSelectedElements(ICollection<QualitySpecificationElement> selected,
		                                bool forceVisible)
		{
			if (_setting)
			{
				return;
			}

			try
			{
				_setting = true;
				foreach (DataGridViewRow row in _dataGridViewAllConditions.Rows)
				{
					SpecificationDataset specificationDataset = GetSpecificationDataset(row);
					QualitySpecificationElement element =
						specificationDataset.QualitySpecificationElement;

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
				_setting = false;
			}
		}

		public void RestoreSortState([CanBeNull] DataGridViewSortState sortState)
		{
			if (sortState == null)
			{
				sortState = new DataGridViewSortState(_columnDataset.Name);
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

				SpecificationDataset specificationDataset = GetSpecificationDataset(
					_dataGridViewAllConditions.Rows[e.RowIndex]);

				bool enable = specificationDataset.Enabled;
				foreach (DataGridViewRow row in _dataGridViewAllConditions.SelectedRows)
				{
					specificationDataset = GetSpecificationDataset(row);
					specificationDataset.Enabled = enable;
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

		private void _dataGridViewAllConditions_SelectionChanged(
			object sender, EventArgs e)
		{
			if (_setting)
			{
				return;
			}

			try
			{
				_setting = true;
				CustomizeView?.RenderConditionsViewSelection(GetSelectedElements());
			}
			finally
			{
				_setting = false;
			}
		}

		private void _findController_FindResultChanged(object sender, EventArgs e)
		{
			// render the view content when filtered, or when the result changed because the 
			// filter mode was switched off

			if (FilterRows || FilterRows != _wasFiltered)
			{
				CustomizeView?.RenderViewContent();
			}

			_wasFiltered = FilterRows;
		}

		[NotNull]
		private static SpecificationDataset GetSpecificationDataset(
			[NotNull] DataGridViewRow row)
		{
			return (SpecificationDataset) row.DataBoundItem;
		}

		public ICollection<QualitySpecificationElement> GetSelectedElements()
		{
			return new HashSet<QualitySpecificationElement>(
				_dataGridViewAllConditions.SelectedRows
				                          .Cast<DataGridViewRow>()
				                          .Select(row => GetSpecificationDataset(row)
					                                  .QualitySpecificationElement)
				                          .Where(el => el != null));
		}

		public ICollection<QualitySpecificationElement> GetFilteredElements()
		{
			return new HashSet<QualitySpecificationElement>(
				_dataGridViewAllConditions.Rows
				                          .Cast<DataGridViewRow>()
				                          .Where(row => row.Visible)
				                          .Select(row => GetSpecificationDataset(row)
					                                  .QualitySpecificationElement)
				                          .Where(el => el != null));
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
				GetSpecificationDataSet((DataGridView) sender, e)?.QualityCondition,
				e.CellStyle.Font);

			if (font != null)
			{
				e.CellStyle.Font = font;
			}
		}

		[CanBeNull]
		private SpecificationDataset GetSpecificationDataSet(
			[NotNull] DataGridView dataGridView, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.RowIndex >= dataGridView.RowCount)
			{
				return null;
			}

			return dataGridView.Rows[e.RowIndex].DataBoundItem as SpecificationDataset;
		}
	}
}

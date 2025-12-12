using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	public partial class ConditionsLayerViewControl : UserControl
	{
		private bool _setting;
		private HashSet<QualitySpecificationElement> _selectedElements;

		public ConditionsLayerViewControl()
		{
			InitializeComponent();
		}

		public ICustomizeQASpezificationView CustomizeView { get; set; }

		internal void ApplyTreeState([NotNull] TreeNodeState treeNodeState)
		{
			try
			{
				_treeViewControlConditions.SuspendLayout();
				ApplyTreeState(_treeViewControlConditions.Nodes, treeNodeState.ChildrenStates);
			}
			finally
			{
				_treeViewControlConditions.ResumeLayout();
			}
		}

		private static void ApplyTreeState([NotNull] TreeNodeCollection nodes,
		                                   [NotNull] IEnumerable<TreeNodeState> states)
		{
			foreach (TreeNodeState state in states)
			{
				foreach (TreeNode node in nodes)
				{
					if (node.Text.Equals(state.Id))
					{
						if (state.Expanded)
						{
							node.Expand();
						}

						ApplyTreeState(node.Nodes, state.ChildrenStates);
						break;
					}
				}
			}
		}

		internal void PushTreeState([NotNull] TreeNodeState treeNodeState)
		{
			treeNodeState.ChildrenStates.Clear();
			PushTreeState(_treeViewControlConditions.Nodes, treeNodeState.ChildrenStates);
		}

		private static void PushTreeState([NotNull] TreeNodeCollection nodes,
		                                  [NotNull] IList<TreeNodeState> states)
		{
			foreach (TreeNode node in nodes)
			{
				var state = new TreeNodeState
				            {
					            Id = node.Text,
					            Expanded = node.IsExpanded
				            };
				states.Add(state);
				PushTreeState(node.Nodes, state.ChildrenStates);
			}
		}

		private void _treeViewConditions_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (_setting)
			{
				return;
			}

			_setting = true;
			try
			{
				CustomizeUtils.CheckRecursive(e.Node);
				if (e.Action != TreeViewAction.Unknown)
				{
					CustomizeView?.RenderViewContent();
				}
			}
			finally
			{
				_setting = false;
			}
		}

		private void _treeViewConditions_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (_setting)
			{
				return;
			}

			RenderNodeSelection();
		}

		private void RenderNodeSelection()
		{
			ICollection<QualitySpecificationElement> selection = CustomizeUtils.GetSelection(
				_treeViewControlConditions.SelectedNode);

			CustomizeView?.RenderConditionsViewSelection(selection);

			_treeViewControlConditions.SetSelectedElements(selection);

			Refresh();
		}

		[NotNull]
		public ICollection<QualitySpecificationElement> GetSelectedElements()
		{
			if (_treeViewControlConditions.SelectedNode == null)
			{
				return _selectedElements ?? new HashSet<QualitySpecificationElement>();
			}

			return CustomizeUtils.GetSelection(_treeViewControlConditions.SelectedNode);
		}

		public void SetSpecificationByQualityCondition(
			[NotNull] QualitySpecification qualitySpecification)
		{
			try
			{
				_setting = true;
				IList<SpecificationDataset> dsList =
					CustomizeUtils.GetSpecificationDatasets(qualitySpecification,
					                                        new SpecificationDatasetComparer());

				_treeViewControlConditions.ShowPlain(dsList);
			}
			finally
			{
				_setting = false;
			}
		}

		public void SetSpecificationByDatasets(
			[NotNull] QualitySpecification qualitySpecification)
		{
			try
			{
				_setting = true;
				IList<SpecificationDataset> dsList =
					CustomizeUtils.GetSpecificationDatasets(qualitySpecification, null);

				_treeViewControlConditions.ShowLayer(dsList,
				                                     new DatasetCategoryItemComparer(),
				                                     new DatasetComparer(),
				                                     new SpecificationDatasetComparer());
			}
			finally
			{
				_setting = false;
			}
		}

		public void SetSpecificationByHierarchicDatasets(
			[NotNull] QualitySpecification qualitySpecification)
		{
			try
			{
				_setting = true;
				IList<SpecificationDataset> dsList =
					CustomizeUtils.GetSpecificationDatasets(qualitySpecification, null);
				_treeViewControlConditions.ShowHierarchic(dsList,
				                                          new DatasetCategoryItemComparer(),
				                                          new DatasetComparer(),
				                                          new SpecificationDatasetComparer());
			}
			finally
			{
				_setting = false;
			}
		}

		public void SetSpecificationByCategories(
			[NotNull] QualitySpecification qualitySpecification)
		{
			bool oldSetting = _setting;

			_setting = true;

			try
			{
				_treeViewControlConditions.ShowQualityConditionsByCategory(
					qualitySpecification.Elements.Select(x => new SpecificationDataset(x)));
			}
			finally
			{
				_setting = oldSetting;
			}
		}

		public void SetSelectedElements(
			[NotNull] ICollection<QualitySpecificationElement> selected,
			bool forceVisible)
		{
			bool oldSetting = _setting;

			_setting = true;
			try
			{
				_treeViewControlConditions.SetSelectedElements(selected);
				_selectedElements = new HashSet<QualitySpecificationElement>(selected);

				var element =
					_treeViewControlConditions.SelectedNode?.Tag as QualitySpecificationElement;
				if (element == null || ! _selectedElements.Contains(element))
				{
					_treeViewControlConditions.SelectedNode = null;
				}

				if (selected.Count > 0)
				{
					SetFirstVisible(_treeViewControlConditions.Nodes,
					                selected,
					                allowExpand: forceVisible);
				}

				Refresh();
			}
			finally
			{
				_setting = oldSetting;
			}
		}

		private static bool SetFirstVisible(
			[NotNull] TreeNodeCollection nodes,
			[NotNull] ICollection<QualitySpecificationElement> selected,
			bool allowExpand)
		{
			foreach (TreeNode node in nodes)
			{
				var element = node.Tag as QualitySpecificationElement;

				if (selected.Contains(element))
				{
					node.EnsureVisible();
					return true;
				}

				if (node.IsExpanded || allowExpand)
				{
					if (SetFirstVisible(node.Nodes, selected, allowExpand))
					{
						return true;
					}
				}
			}

			return false;
		}

		public void RefreshAll()
		{
			try
			{
				_setting = true;
				foreach (TreeNode node in _treeViewControlConditions.Nodes)
				{
					CustomizeUtils.UpdateChecks(node, _treeViewControlConditions);
				}
			}
			finally
			{
				_setting = false;
			}
		}

		private void _toolStripMenuItemCollapseAll_Click(object sender, EventArgs e)
		{
			_treeViewControlConditions.CollapseAll();

			// Note: selection may change due to collapse (if there was a selected child node - only for *real* node selection)
			RenderNodeSelection();
		}
	}
}

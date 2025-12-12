using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	internal static class CustomizeUtils
	{
		[CanBeNull]
		public static Font GetFont([CanBeNull] QualityCondition condition,
		                           [NotNull] Font font)
		{
			if (condition?.Updated ?? false)
			{
				return new Font(font, FontStyle.Italic);
			}

			return null;
		}

		public static CheckState UpdateChecks([NotNull] TreeNode node,
		                                      [NotNull] TestTreeViewControl treeView)
		{
			if (node.Nodes.Count == 0)
			{
				// there are no children
				var elem = (QualitySpecificationElement) node.Tag;

				node.Checked = elem.Enabled;

				return elem.Enabled
					       ? CheckState.Checked
					       : CheckState.Unchecked;
			}

			var first = true;
			var state = CheckState.Checked;
			foreach (TreeNode child in node.Nodes)
			{
				if (first)
				{
					state = UpdateChecks(child, treeView);
				}
				else if (UpdateChecks(child, treeView) != state)
				{
					state = CheckState.Indeterminate;
				}

				first = false;
			}

			treeView.SetState(node, state);

			return state;
		}

		[NotNull]
		public static IList<SpecificationDataset> GetSpecificationDatasets(
			[NotNull] QualitySpecification specification,
			[CanBeNull] IComparer<SpecificationDataset> sorter)
		{
			List<SpecificationDataset> result =
				specification.Elements
				             .Select(element => new SpecificationDataset(element))
				             .ToList();

			if (sorter != null)
			{
				result.Sort(sorter);
			}

			return result;
		}

		public static void CheckRecursive([NotNull] TreeNode node)
		{
			var qualitySpecificationElement = node.Tag as QualitySpecificationElement;
			if (qualitySpecificationElement != null &&
			    qualitySpecificationElement.Enabled != node.Checked)
			{
				qualitySpecificationElement.Enabled = node.Checked;
			}

			foreach (TreeNode child in node.Nodes)
			{
				child.Checked = node.Checked;
				CheckRecursive(child);
			}
		}

		[NotNull]
		public static SpecificationDataset GetSpecificationDataset(
			[NotNull] DataGridViewRow row)
		{
			return (SpecificationDataset) row.DataBoundItem;
		}

		[NotNull]
		public static ICollection<QualitySpecificationElement> GetSelection(
			[CanBeNull] TreeNode node)
		{
			var result = new HashSet<QualitySpecificationElement>();

			if (node != null)
			{
				FillSelection(node, result);
			}

			return result;
		}

		private static void FillSelection(
			[NotNull] TreeNode node,
			// ReSharper disable once SuggestBaseTypeForParameter
			[NotNull] HashSet<QualitySpecificationElement> qualitySpecificationElements)
		{
			var qualitySpecificationElement = node.Tag as QualitySpecificationElement;

			qualitySpecificationElements.Add(qualitySpecificationElement);

			foreach (TreeNode child in node.Nodes)
			{
				FillSelection(child, qualitySpecificationElements);
			}
		}
	}
}

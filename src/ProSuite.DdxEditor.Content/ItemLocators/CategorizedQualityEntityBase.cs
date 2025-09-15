using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public abstract class CategorizedQualityEntityBase : ItemLocatorBase
	{
		public override Item Locate(Entity entity, IEnumerable<IItemTreeNode> rootNodes)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(rootNodes, nameof(rootNodes));

			DataQualityCategory category = GetCategory(entity);

			IItemTreeNode categoryNode = GetCategoryNode(category, rootNodes);

			if (categoryNode == null)
			{
				return null;
			}

			IItemTreeNode containerNode = GetContainerNode(entity, category, categoryNode);

			return containerNode != null
				       ? FindItem(containerNode, node => node.IsBasedOnEntity(entity))
				       : null;
		}

		[CanBeNull]
		private static IItemTreeNode GetCategoryNode(
			[CanBeNull] DataQualityCategory category,
			[NotNull] IEnumerable<IItemTreeNode> rootNodes)
		{
			if (category == null)
			{
				return FindNode(rootNodes, node => node.Item is QAItem);
			}

			IItemTreeNode parentNode = GetCategoryNode(category.ParentCategory, rootNodes);

			return parentNode == null
				       ? null
				       : FindNode(parentNode, node => node.IsBasedOnEntity(category));
		}

		[CanBeNull]
		protected abstract DataQualityCategory GetCategory([NotNull] Entity entity);

		[CanBeNull]
		protected abstract IItemTreeNode GetContainerNode([NotNull] Entity entity,
		                                                  [CanBeNull] DataQualityCategory category,
		                                                  [NotNull] IItemTreeNode categoryNode);
	}
}

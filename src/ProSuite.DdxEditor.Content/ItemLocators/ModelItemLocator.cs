using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class ModelItemLocator : ItemLocatorBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is DdxModel;
		}

		public override Item Locate(Entity entity, IEnumerable<IItemTreeNode> rootNodes)
		{
			IItemTreeNode rootNode = FindNode(rootNodes, n => n.Item is ModelsItemBase, true);

			if (rootNode == null)
			{
				return null;
			}

			IItemTreeNode entityTypeNode = FindNode(
				rootNode.ChildNodes,
				n => n.IsBasedOnEntityType<DdxModel>(),
				true);

			return entityTypeNode == null
				       ? null
				       : FindItem(entityTypeNode, n => n.IsBasedOnEntity(entity));
		}
	}
}

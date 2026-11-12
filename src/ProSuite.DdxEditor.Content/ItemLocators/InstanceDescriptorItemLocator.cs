using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Content.QA;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class InstanceDescriptorItemLocator : ItemLocatorBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is InstanceDescriptor;
		}

		public override Item Locate(Entity entity, IEnumerable<IItemTreeNode> rootNodes)
		{
			IItemTreeNode rootNode = FindNode(rootNodes, n => n.Item is QAItem);

			if (rootNode == null)
			{
				return null;
			}

			// The folders are typed by the concrete descriptor class (test, transformer or
			// issue filter descriptor). None of them accepts the base class
			// InstanceDescriptor, therefore match by the type of the searched entity.
			IItemTreeNode entityTypeNode = FindNode(
				rootNode.ChildNodes,
				n => n.IsBasedOnEntityType(entity),
				true);

			return entityTypeNode == null
				       ? null
				       : FindItem(entityTypeNode, n => n.IsBasedOnEntity(entity));
		}
	}
}

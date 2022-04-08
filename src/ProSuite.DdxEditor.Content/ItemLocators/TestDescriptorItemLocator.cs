using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Content.QA;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class TestDescriptorItemLocator : ItemLocatorBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is TestDescriptor;
		}

		public override Item Locate(Entity entity, IEnumerable<IItemTreeNode> rootNodes)
		{
			IItemTreeNode rootNode = FindNode(rootNodes, n => n.Item is QAItem);

			if (rootNode == null)
			{
				return null;
			}

			IItemTreeNode entityTypeNode = FindNode(rootNode.ChildNodes,
			                                        n => n.IsBasedOnEntityType<TestDescriptor>());

			return entityTypeNode == null
				       ? null
				       : FindItem(entityTypeNode, n => n.IsBasedOnEntity(entity));
		}
	}
}
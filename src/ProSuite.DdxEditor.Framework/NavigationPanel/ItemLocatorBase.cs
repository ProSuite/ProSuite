using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public abstract class ItemLocatorBase : IItemLocator
	{
		public abstract bool CanLocate(Entity entity);

		public abstract Item Locate(Entity entity, IEnumerable<IItemTreeNode> rootNodes);

		[CanBeNull]
		protected static Item FindItem([NotNull] IItemTreeNode node,
		                               [NotNull] Predicate<IItemTreeNode> match)
		{
			Assert.ArgumentNotNull(node, nameof(node));
			Assert.ArgumentNotNull(match, nameof(match));

			return node.ChildNodes
			           .Where(childNode => match(childNode))
			           .Select(childNode => childNode.Item)
			           .FirstOrDefault();
		}

		[CanBeNull]
		protected static IItemTreeNode FindNode([NotNull] IItemTreeNode node,
		                                        [NotNull] Predicate<IItemTreeNode> match)
		{
			Assert.ArgumentNotNull(node, nameof(node));
			Assert.ArgumentNotNull(match, nameof(match));

			return FindNode(node.ChildNodes, match);
		}

		[CanBeNull]
		protected static IItemTreeNode FindNode([NotNull] IEnumerable<IItemTreeNode> nodes,
		                                        [NotNull] Predicate<IItemTreeNode> match)
		{
			Assert.ArgumentNotNull(nodes, nameof(nodes));
			Assert.ArgumentNotNull(match, nameof(match));

			return nodes.FirstOrDefault(childNode => match(childNode));
		}
	}
}

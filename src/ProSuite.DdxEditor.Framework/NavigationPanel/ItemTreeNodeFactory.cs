using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public static class ItemTreeNodeFactory
	{
		[NotNull]
		public static ItemTreeNode CreateNode([NotNull] Item item,
		                                      [NotNull] IImageProvider imageProvider)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(imageProvider, nameof(imageProvider));

			var node = new ItemTreeNode(item, imageProvider);

			node.Collapse();

			if (item.Children.Count > 0)
			{
				node.Nodes.Add(new DummyTreeNode());
			}

			return node;
		}
	}
}

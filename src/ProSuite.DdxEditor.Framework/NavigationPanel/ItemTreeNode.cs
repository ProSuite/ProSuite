using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public class ItemTreeNode : TreeNode, IDisposable, IItemTreeNode
	{
		[NotNull] private readonly IImageProvider _imageProvider;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemTreeNode"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="imageProvider"></param>
		public ItemTreeNode([NotNull] Item item, [NotNull] IImageProvider imageProvider)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(imageProvider, nameof(imageProvider));

			Item = item;
			_imageProvider = imageProvider;

			WireEvents();

			UpdateAppearance();
		}

		#endregion

		IEnumerable<IItemTreeNode> IItemTreeNode.ChildNodes
		{
			get
			{
				EnsureChildNodesAdded();

				return Nodes.Cast<IItemTreeNode>();
			}
		}

		public bool IsBasedOnEntityType<T>() where T : Entity
		{
			return IsBasedOnEntityType(typeof(T));
		}

		public bool IsBasedOnEntityType(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			return IsBasedOnEntityType(entity.GetType());
		}

		public bool IsBasedOnEntity(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			var entityItem = Item as IEntityItem;

			return entityItem != null && entityItem.IsBasedOn(entity);
		}

		public Item Item { get; }

		public void EnsureChildNodesAdded()
		{
			if (Nodes.Count > 0 && Nodes[0] is DummyTreeNode)
			{
				RefreshChildNodes(_imageProvider);
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			UnwireEvents();
		}

		#endregion

		private void UpdateAppearance()
		{
			Text = Item.Text;

			UpdateNodeImage();
		}

		private void UpdateNodeImage()
		{
			ImageKey = _imageProvider.GetImageKey(Item, Item.Image);

			const bool selected = true;
			SelectedImageKey =
				Item.SelectedImage != Item.Image
					? _imageProvider.GetImageKey(Item, Item.SelectedImage, selected)
					: ImageKey;
		}

		private void RefreshChildNodes([NotNull] IImageProvider imageProvider)
		{
			Assert.ArgumentNotNull(imageProvider, nameof(imageProvider));

			Nodes.Clear();

			foreach (Item child in Item.Children)
			{
				Nodes.Add(ItemTreeNodeFactory.CreateNode(child, imageProvider));
			}
		}

		private bool IsBasedOnEntityType([NotNull] Type type)
		{
			var entityTypeItem = Item as IEntityTypeItem;

			return entityTypeItem != null && entityTypeItem.IsBasedOn(type);
		}

		private TreeNode GetChildNode(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			foreach (TreeNode child in Nodes)
			{
				var itemNode = child as ItemTreeNode;
				if (itemNode != null && itemNode.Item == item)
				{
					return itemNode;
				}
			}

			return null;
		}

		private void WireEvents()
		{
			Item.Changed += _item_Changed;
			Item.ChildAdded += _item_ChildAdded;
			Item.ChildrenRefreshed += _item_ChildrenRefreshed;
			Item.Deleted += _item_Deleted;
		}

		private void UnwireEvents()
		{
			Item.Changed -= _item_Changed;
			Item.ChildAdded -= _item_ChildAdded;
			Item.ChildrenRefreshed -= _item_ChildrenRefreshed;
			Item.Deleted -= _item_Deleted;
		}

		private void _item_Deleted(object sender, EventArgs e)
		{
			Remove();

			Dispose();
		}

		private void _item_Changed(object sender, EventArgs e)
		{
			UpdateAppearance();
		}

		private void _item_ChildAdded(object sender, ItemEventArgs e)
		{
			if (! IsExpanded)
			{
				// this already picks up the new item
				Expand();
			}

			TreeNode newNode = GetChildNode(e.Item);

			if (newNode == null)
			{
				newNode = ItemTreeNodeFactory.CreateNode(e.Item, _imageProvider);

				Nodes.Add(newNode);
			}

			TreeView.SelectedNode = newNode;
		}

		private void _item_ChildrenRefreshed(object sender, EventArgs e)
		{
			RefreshChildNodes(_imageProvider);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public class ItemTreeNode : TreeNode, IDisposable, IItemTreeNode
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

			if (Nodes.Count > 0)
			{
				TreeViewUpdateScope.EnsureNodeRemovalAllowed(TreeView, Text);

				using (TreeViewUpdateScope.EnterNodeRemoval(TreeView))
				{
					Nodes.Clear();
				}
			}

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

		/// <summary>
		/// Reports an exception instead of letting it escape from an item event
		/// handler. These handlers are typically called while the tree view is
		/// processing a windows message, where an escaping exception ends the process.
		/// </summary>
		private static void Try([NotNull] Action proc)
		{
			try
			{
				proc();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		private void _item_Deleted(object sender, EventArgs e)
		{
			Try(delegate
			{
				TreeViewUpdateScope.EnsureNodeRemovalAllowed(TreeView, Text);

				MoveSelectionToParent();

				using (TreeViewUpdateScope.EnterNodeRemoval(TreeView))
				{
					Remove();
				}

				Dispose();
			});
		}

		/// <summary>
		/// Moves the selection to the parent node if this node holds it, so that the
		/// item to be selected next is determined here, while the tree view is still
		/// intact: once this node is removed, the native tree view moves the selection
		/// on its own, and the resulting events must be ignored (see
		/// <see cref="TreeViewUpdateScope"/>).
		/// </summary>
		private void MoveSelectionToParent()
		{
			TreeView treeView = TreeView;

			if (treeView == null || Parent == null)
			{
				return;
			}

			if (TreeViewUpdateScope.IsChangingSelection(treeView))
			{
				// the tree view is already selecting another node (this node is
				// typically removed because its new item is discarded on navigating
				// away from it); don't interfere with that selection
				return;
			}

			if (! ContainsNode(treeView.SelectedNode))
			{
				return;
			}

			treeView.SelectedNode = Parent;
		}

		private bool ContainsNode([CanBeNull] TreeNode node)
		{
			for (TreeNode candidate = node; candidate != null; candidate = candidate.Parent)
			{
				if (candidate == this)
				{
					return true;
				}
			}

			return false;
		}

		private void _item_Changed(object sender, EventArgs e)
		{
			Try(UpdateAppearance);
		}

		private void _item_ChildAdded(object sender, ItemEventArgs e)
		{
			Try(delegate
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
			});
		}

		private void _item_ChildrenRefreshed(object sender, EventArgs e)
		{
			Try(() => RefreshChildNodes(_imageProvider));
		}
	}
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Menus;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public partial class NavigationControl : UserControl, INavigationView
	{
		[CanBeNull] private readonly IEnumerable<IItemLocator> _itemLocators;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull] private readonly ImageListBuilder _imageListBuilder;

		private IMenuManager _menuManager;
		private INavigationObserver _observer;

		private bool _inBeforeSelection;
		private bool _afterSelectionSkipped;

		private TreeNode _nodeSelectedBeforeOpeningContextMenu;

		//private static TreeNode _toSelectAfterExpand;

		/// <summary>
		/// Initializes a new instance of the <see cref="NavigationControl"/> class.
		/// </summary>
		/// <param name="itemLocators"></param>
		public NavigationControl(
			[CanBeNull] IEnumerable<IItemLocator> itemLocators = null)
		{
			_itemLocators = itemLocators;

			InitializeComponent();

			_imageListBuilder = new ImageListBuilder(_imageList);
		}

		public IMenuManager MenuManager
		{
			get { return _menuManager; }
			set { _menuManager = value; }
		}

		#region INavigationView Members

		public INavigationObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		public void RenderItems(IEnumerable<Item> rootItems)
		{
			Assert.ArgumentNotNull(rootItems, nameof(rootItems));

			_treeView.Nodes.Clear();

			foreach (Item navigationItem in rootItems)
			{
				_treeView.Nodes.Add(ItemTreeNodeFactory.CreateNode(navigationItem,
					                    _imageListBuilder));
			}
		}

		public bool GoToItem(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			_msg.VerboseDebug(() => $"GoToItem {item.Text}");

			ItemTreeNode node = FindNode(item);

			if (node == null)
			{
				_msg.DebugFormat("Node not found for item: {0}", item.Text);
				return false;
			}

			node.EnsureVisible();

			_treeView.SelectedNode = node;
			return true;
		}

		public IEnumerable<I> FindItems<I>() where I : Item
		{
			return FindItems(candidate => candidate is I).Cast<I>();
		}

		public I FindFirstItem<I>() where I : Item
		{
			// return the first one found
			return FindItems(candidate => candidate is I).Cast<I>().FirstOrDefault();
		}

		public Item FindItem(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			if (_itemLocators != null)
			{
				List<IItemTreeNode> rootNodes =
					_treeView.Nodes.Cast<IItemTreeNode>().ToList();

				// try to locate using item locators
				foreach (IItemLocator locator in _itemLocators)
				{
					if (! locator.CanLocate(entity))
					{
						continue;
					}

					Item item = locator.Locate(entity, rootNodes);
					if (item != null)
					{
						return item;
					}
				}
			}

			// try to locate using generic strategy
			return FindItems(candidate => IsItemBasedOnEntity(candidate, entity),
			                 candidate => IsItemBasedOnEntityType(candidate, entity))
				.FirstOrDefault();
		}

		#endregion

		[NotNull]
		private IEnumerable<Item> FindItems(
			[NotNull] Predicate<Item> match,
			[CanBeNull] Predicate<Item> expandChildren = null)
		{
			Assert.ArgumentNotNull(match, nameof(match));

			return FindNodes(match, expandChildren).Select(node => node.Item);
		}

		[NotNull]
		private IEnumerable<ItemTreeNode> FindNodes(
			[NotNull] Predicate<Item> match,
			[CanBeNull] Predicate<Item> expandChildren = null)
		{
			Assert.ArgumentNotNull(match, nameof(match));

			return FindNodes(_treeView.Nodes, match, expandChildren);
		}

		[NotNull]
		private IEnumerable<ItemTreeNode> FindNodes(
			[NotNull] TreeNodeCollection nodes,
			[NotNull] Predicate<Item> match,
			[CanBeNull] Predicate<Item> expandChildren = null)
		{
			Assert.ArgumentNotNull(nodes, nameof(nodes));
			Assert.ArgumentNotNull(match, nameof(match));

			foreach (TreeNode node in nodes)
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("Node: {0}", node.Text);
				}

				var itemTreeNode = node as ItemTreeNode;

				if (itemTreeNode == null)
				{
					continue;
				}

				if (match(itemTreeNode.Item))
				{
					yield return itemTreeNode;
				}

				// NOTE: this can make this operation very expensive
				if (expandChildren != null && expandChildren(itemTreeNode.Item))
				{
					itemTreeNode.EnsureChildNodesAdded();
				}

				foreach (ItemTreeNode matchingDescendant in
				         FindNodes(node.Nodes, match, expandChildren))
				{
					yield return matchingDescendant;
				}
			}
		}

		[CanBeNull]
		private ItemTreeNode FindNode([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			if (item.Parent == null)
			{
				// top-level item
				// get the corresponding root node, make sure it has 
				// its child nodes added
				foreach (TreeNode rootNode in _treeView.Nodes)
				{
					var itemTreeNode = (ItemTreeNode) rootNode;

					if (Equals(item, itemTreeNode.Item))
					{
						return itemTreeNode;
					}
				}

				throw new ArgumentException(
					string.Format("Root node not found for root-level item: {0}",
					              item.Text), nameof(item));
			}

			// not a top level item; get the parent node
			ItemTreeNode parentNode = FindNode(item.Parent);

			if (parentNode != null)
			{
				// find the matching node from the parent node's children
				parentNode.EnsureChildNodesAdded();

				return parentNode.Nodes.Cast<ItemTreeNode>()
				                 .FirstOrDefault(node => Equals(item, node.Item));
			}

			return null;
		}

		private static bool IsItemBasedOnEntityType([NotNull] Item item,
		                                            [NotNull] Entity entity)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(entity, nameof(entity));

			var entityTypeItem = item as IEntityTypeItem;

			return entityTypeItem != null && entityTypeItem.IsBasedOn(entity.GetType());
		}

		private static bool IsItemBasedOnEntity([NotNull] Item item,
		                                        [NotNull] Entity entity)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(entity, nameof(entity));

			var entityItem = item as IEntityItem;

			return entityItem != null && entityItem.IsBasedOn(entity);
		}

		private bool IgnoreSelectionEvents =>
			_nodeSelectedBeforeOpeningContextMenu != null;

		private void Try([NotNull] Action proc, [CanBeNull] Cursor cursor = null)
		{
			Cursor resetCursor = null;

			try
			{
				if (cursor != null)
				{
					resetCursor = Cursor;
					Cursor = cursor;
				}

				proc();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
			finally
			{
				if (resetCursor != null)
				{
					Cursor = resetCursor;
				}
			}
		}

		private void _treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			_msg.VerboseDebug(() => "_treeView_AfterSelect enter");

			if (IgnoreSelectionEvents)
			{
				_msg.VerboseDebug(() => "ignoring _treeView_AfterSelect event");
				return;
			}

			_afterSelectionSkipped = false;

			if (_inBeforeSelection)
			{
				_msg.VerboseDebug(() => "_treeView_AfterSelect within BeforeSelect call");
				_afterSelectionSkipped = true;
				return;
			}

			Try(delegate
			{
				if (_observer != null)
				{
					var node = (ItemTreeNode) e.Node;
					_observer.HandleItemSelected(node.Item);
				}
			}, Cursors.WaitCursor);

			_msg.VerboseDebug(() => "_treeView_AfterSelect exit");
		}

		private void _treeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			_msg.VerboseDebug(() => "_treeView_BeforeSelect enter");

			if (IgnoreSelectionEvents)
			{
				_msg.VerboseDebug(() => "ignoring _treeView_BeforeSelect event");
				return;
			}

			if (_inBeforeSelection)
			{
				// recursion
				_msg.VerboseDebug(() => "recursive _treeView_BeforeSelect call");
				return;
			}

			try
			{
				_inBeforeSelection = true;

				Try(delegate
				{
					if (_observer == null)
					{
						return;
					}

					// NOTE: if a dialog is shown, then the AfterSelect event is
					//       raised before the pending changes can be discarded (when showing the dialog)
					var node = (ItemTreeNode) e.Node;
					e.Cancel = ! _observer.PrepareItemSelection(node.Item);

					if (_afterSelectionSkipped)
					{
						_observer.HandleItemSelected(node.Item);
					}
				});
			}
			finally
			{
				_inBeforeSelection = false;
			}

			_msg.VerboseDebug(() => "_treeView_BeforeSelect exit");
		}

		private void _treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			Try(() => ((ItemTreeNode) e.Node).EnsureChildNodesAdded(),
			    Cursors.WaitCursor);
		}

		private void _treeView_AfterExpand(object sender, TreeViewEventArgs e)
		{
			//if (_toSelectAfterExpand != null)
			//{
			//    _treeView.SelectedNode = _toSelectAfterExpand;
			//    _toSelectAfterExpand = null;
			//}
		}

		private void _treeView_NodeMouseClick(object sender,
		                                      TreeNodeMouseClickEventArgs e)
		{
			Try(delegate
			{
				if (e.Button != MouseButtons.Right)
				{
					return;
				}

				if (_menuManager == null)
				{
					return;
				}

				var node = (ItemTreeNode) e.Node;

				// add menu items to the context menu
				_contextMenuStrip.SuspendLayout();

				try
				{
					_contextMenuStrip.Items.Clear();
					_menuManager.AddMenuItems(_contextMenuStrip, node.Item);
				}
				finally
				{
					_contextMenuStrip.ResumeLayout(true);
				}

				// invoke context menu
				if (_contextMenuStrip.Items.Count <= 0)
				{
					return;
				}

				_nodeSelectedBeforeOpeningContextMenu = _treeView.SelectedNode;

				_treeView.SelectedNode = node;

				_contextMenuStrip.Show(this, e.Location);

				// when the context menu is closed, the previous selection will be restored
			});
		}

		private void _contextMenuStrip_Closed(object sender,
		                                      ToolStripDropDownClosedEventArgs e)
		{
			if (_nodeSelectedBeforeOpeningContextMenu == null)
			{
				return;
			}

			_treeView.SelectedNode = _nodeSelectedBeforeOpeningContextMenu;

			_nodeSelectedBeforeOpeningContextMenu = null;
		}

		#region Nested type: DummyTreeNode

		#endregion

		#region Nested type: ImageListBuilder

		private class ImageListBuilder : IImageProvider
		{
			private readonly ImageList _imageList;

			/// <summary>
			/// Initializes a new instance of the <see cref="ImageListBuilder"/> class.
			/// </summary>
			/// <param name="imageList">The image list.</param>
			public ImageListBuilder([NotNull] ImageList imageList)
			{
				Assert.ArgumentNotNull(imageList, nameof(imageList));

				_imageList = imageList;
			}

			public string GetImageKey(Item item, Image image)
			{
				return GetImageKey(item, image, false);
			}

			public string GetImageKey(Item item, Image image, bool selected)
			{
				Assert.ArgumentNotNull(item, nameof(item));

				if (image == null)
				{
					return null;
				}

				string key = FormatImageKey(item, selected);

				if (! _imageList.Images.ContainsKey(key))
				{
					_imageList.Images.Add(key, image);
				}

				return key;
			}

			private static string FormatImageKey(Item item, bool selected)
			{
				return string.Format("{0}#{1}", selected, item.ImageKey);
			}
		}

		#endregion
	}
}

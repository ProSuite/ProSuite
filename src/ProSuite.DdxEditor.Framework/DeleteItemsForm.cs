using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework
{
	// TODO
	// - Make "entity type" easier to read
	// - revise treenode image state (also for navigator tree)
	// - revise names of depending items
	internal partial class DeleteItemsForm : Form, IFormStateAware<DeleteItemsFormState>
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Latch _latch = new Latch();
		private readonly BoundDataGridHandler<DependingItemTableRow> _gridHandler;

		private const string _messageDelete = "Deletable items";
		private const string _messageUnableDelete = "Non-deletable items";

		public DeleteItemsForm(
			[NotNull] ICollection<ItemDeletionCandidate> deletableItems,
			[NotNull] ICollection<ItemDeletionCandidate> nonDeletableItems)
		{
			Assert.ArgumentNotNull(deletableItems, nameof(deletableItems));
			Assert.ArgumentNotNull(nonDeletableItems, nameof(nonDeletableItems));

			InitializeComponent();

			var imageListBuilder = new ImageListBuilder(_imageList);

			var formStateManager = new FormStateManager<DeleteItemsFormState>(this);
			formStateManager.RestoreState(FormStateRestoreOption.Normal);

			FormClosed += delegate { formStateManager.SaveState(); };

			_gridHandler = new BoundDataGridHandler<DependingItemTableRow>(_dataGridView);

			int distinctDeletableDependenciesCount;
			int distinctNonDeletableDependenciesCount;

			PopulateTreeView(_treeView, imageListBuilder,
			                 deletableItems, nonDeletableItems,
			                 out distinctDeletableDependenciesCount,
			                 out distinctNonDeletableDependenciesCount);

			UpdateFormLayout(nonDeletableItems.Count, deletableItems.Count,
			                 distinctNonDeletableDependenciesCount,
			                 distinctDeletableDependenciesCount);
		}

		#region Implementation of IFormStateAware<DeleteFormState>

		void IFormStateAware<DeleteItemsFormState>.RestoreState(
			DeleteItemsFormState formState)
		{
			_splitContainer.SplitterDistance = formState.SplitterDistance;
		}

		void IFormStateAware<DeleteItemsFormState>.GetState(DeleteItemsFormState formState)
		{
			formState.SplitterDistance = _splitContainer.SplitterDistance;
		}

		#endregion

		private static void PopulateTreeView(
			[NotNull] TreeView treeView,
			[NotNull] IImageProvider imageProvider,
			[NotNull] ICollection<ItemDeletionCandidate> deletableItems,
			[NotNull] ICollection<ItemDeletionCandidate> nonDeletableItems,
			out int distinctDeletableDependenciesCount,
			out int distinctNonDeletableDependenciesCount)
		{
			distinctNonDeletableDependenciesCount = 0;

			treeView.BeginUpdate();

			try
			{
				if (nonDeletableItems.Count > 0)
				{
					ICollection<DependingItem> distinctDependencies;
					IEnumerable<DeletionCandidateTreeNode> treeNodes =
						CreateChildNodes(nonDeletableItems, imageProvider,
						                 out distinctDependencies);

					distinctNonDeletableDependenciesCount =
						distinctDependencies.Count(dependency => ! dependency.CanRemove);

					TreeNodeBase nonDeletableRootNode = CreateRootNode(treeNodes,
						distinctDependencies,
						_messageUnableDelete,
						imageProvider);

					treeView.Nodes.Add(nonDeletableRootNode);
				}

				distinctDeletableDependenciesCount = 0;

				if (deletableItems.Count > 0)
				{
					ICollection<DependingItem> distinctDependencies;
					IEnumerable<DeletionCandidateTreeNode> treeNodes =
						CreateChildNodes(deletableItems, imageProvider,
						                 out distinctDependencies);

					distinctDeletableDependenciesCount =
						distinctDependencies.Count(dependency => dependency.CanRemove);

					TreeNodeBase deleteableRootNode = CreateRootNode(
						treeNodes, distinctDependencies,
						_messageDelete, imageProvider);

					treeView.Nodes.Add(deleteableRootNode);
				}

				treeView.ExpandAll();
			}
			finally
			{
				treeView.EndUpdate();
			}
		}

		[NotNull]
		private static TreeNodeBase CreateRootNode(
			[NotNull] IEnumerable<DeletionCandidateTreeNode> childNodes,
			[NotNull] IEnumerable<DependingItem> distinctDependencies,
			[NotNull] string text,
			[NotNull] IImageProvider imageProvider)
		{
			Assert.ArgumentNotNull(childNodes, nameof(childNodes));
			Assert.ArgumentNotNull(distinctDependencies, nameof(distinctDependencies));
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));
			Assert.ArgumentNotNull(imageProvider, nameof(imageProvider));

			var result = new TreeNodeBase(text, distinctDependencies);

			UpdateNodeImage(result, imageProvider);

			result.Nodes.AddRange(childNodes.Cast<TreeNode>().ToArray());

			return result;
		}

		[NotNull]
		private static IEnumerable<DeletionCandidateTreeNode> CreateChildNodes(
			[NotNull] IEnumerable<ItemDeletionCandidate> deletionCandidates,
			[NotNull] IImageProvider imageProvider,
			[NotNull] out ICollection<DependingItem> distinctDependingItems)
		{
			Assert.ArgumentNotNull(deletionCandidates, nameof(deletionCandidates));
			Assert.ArgumentNotNull(imageProvider, nameof(imageProvider));

			var result = new List<DeletionCandidateTreeNode>();
			var distinctDependingItemsSet = new HashSet<DependingItem>();

			foreach (ItemDeletionCandidate deletionCandidate in deletionCandidates)
			{
				IEnumerable<DependingItem> confirmableDependencies =
					GetConfirmableDependencies(deletionCandidate);

				var node = new DeletionCandidateTreeNode(deletionCandidate,
				                                         confirmableDependencies);

				foreach (DependingItem dependingItem in confirmableDependencies)
				{
					distinctDependingItemsSet.Add(dependingItem);
				}

				UpdateNodeImage(node, imageProvider);

				result.Add(node);
			}

			distinctDependingItems = new List<DependingItem>(distinctDependingItemsSet);

			return result;
		}

		[NotNull]
		private static ICollection<DependingItem> GetConfirmableDependencies(
			[NotNull] ItemDeletionCandidate deletionCandidate)
		{
			return deletionCandidate.DependingItems
			                        .Where(dependingItem => dependingItem.RequiresConfirmation)
			                        .ToList();
		}

		private void UpdateFormLayout(int nonDeleteableItemsCount,
		                              int deletableItemsCount,
		                              int distinctNonDeletableDependenciesCount,
		                              int distinctDeletableDependenciesCount)
		{
			var header = new StringBuilder();
			var footer = new StringBuilder();

			const string unableToDeleteTitle = "Unable to Delete";
			const string confirmDeletionTitle = "Confirm Deletion";

			if (deletableItemsCount == 0)
			{
				// none of the items can be deleted

				Text = unableToDeleteTitle;
				_pictureBoxImage.Image = SystemIcons.Error.ToBitmap();
				SetYesButton(false);

				if (nonDeleteableItemsCount == 1)
				{
					header.AppendFormat(
						distinctNonDeletableDependenciesCount <= 1
							? "Unable to delete item. There is {0} non-deletable reference to this item"
							: "Unable to delete item. There are {0} non-deletable references to this item",
						distinctNonDeletableDependenciesCount,
						nonDeleteableItemsCount);
				}
				else
				{
					header.AppendFormat(
						distinctNonDeletableDependenciesCount <= 1
							? "Unable to delete items. There is {0} non-deletable reference to {1} of these items"
							: "Unable to delete items. There are {0} non-deletable references to {1} of these items",
						distinctNonDeletableDependenciesCount,
						nonDeleteableItemsCount);
				}
			}
			else
			{
				// there is at least one deletable item

				Text = confirmDeletionTitle;
				SetYesButton(true);

				if (nonDeleteableItemsCount == 0)
				{
					// all items can be deleted

					if (distinctDeletableDependenciesCount == 0)
					{
						// there are no dependencies

						_pictureBoxImage.Image = SystemIcons.Information.ToBitmap();

						header.Append(deletableItemsCount == 1
							              ? "There are no references to this item"
							              : "There are no references to these items");
					}
					else
					{
						// there are some dependencies

						_pictureBoxImage.Image = SystemIcons.Warning.ToBitmap();

						if (deletableItemsCount == 1)
						{
							// a single deletable item

							header.AppendFormat(
								distinctDeletableDependenciesCount == 1
									? "There is {0} deletable reference to this item"
									: "There are {0} deletable references to this item",
								distinctDeletableDependenciesCount);

							footer.Append("Are you sure you want to delete this item?");
						}
						else
						{
							// multiple deletable items

							header.AppendFormat(
								distinctDeletableDependenciesCount == 1
									? "There is {0} deletable reference to these items"
									: "There are {0} deletable references to these items",
								distinctDeletableDependenciesCount);

							footer.AppendFormat(
								"Are you sure you want to delete all deletable items ({0})?",
								deletableItemsCount);
						}
					}
				}
				else
				{
					// some items cannot be deleted

					_pictureBoxImage.Image = SystemIcons.Warning.ToBitmap();

					header.AppendFormat(
						distinctNonDeletableDependenciesCount <= 1
							? "Unable to delete some of the items. There is {0} non-deletable reference to {1} of these items"
							: "Unable to delete some of the items. There are {0} non-deletable references to {1} of these items",
						distinctNonDeletableDependenciesCount,
						nonDeleteableItemsCount);

					footer.AppendFormat(
						"Are you sure you want to delete all deletable items ({0})?",
						deletableItemsCount);
				}
			}

			_labelHeader.Text = header.ToString();
			_labelFooter.Text = footer.ToString();
		}

		private void SetYesButton(bool visible)
		{
			if (visible)
			{
				_buttonNoClose.Text = @"No";
			}
			else
			{
				_buttonYes.Visible = false;
				_buttonNoClose.Text = @"Close";
			}
		}

		private static void UpdateNodeImage([NotNull] TreeNodeBase node,
		                                    [NotNull] IImageProvider imageProvider)
		{
			Assert.ArgumentNotNull(node, nameof(node));
			Assert.ArgumentNotNull(imageProvider, nameof(imageProvider));

			var deletionCandidate = node as DeletionCandidateTreeNode;

			const bool selected = true;

			if (deletionCandidate != null)
			{
				Item item = deletionCandidate.Item;
				node.ImageKey = imageProvider.GetImageKey(deletionCandidate);

				node.SelectedImageKey =
					item.SelectedImage == item.Image
						? node.ImageKey
						: imageProvider.GetImageKey(deletionCandidate, selected);

				return;
			}

			node.ImageKey = imageProvider.GetImageKey(node, Resources.GroupItem);
			node.SelectedImageKey = imageProvider.GetImageKey(
				node, Resources.GroupItemSelected,
				selected);
		}

		private void Try([NotNull] Action proc, [CanBeNull] Cursor cursor)
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

		private void BindDependingItems(
			[NotNull] IList<DependingItemTableRow> itemDependencies)
		{
			_latch.RunInsideLatch(
				() => _gridHandler.BindTo(
					new SortableBindingList<DependingItemTableRow>(itemDependencies)));
		}

		private void DeleteItemsForm_Load(object sender, EventArgs e)
		{
			if (_treeView.Nodes.Count > 0)
			{
				_treeView.SelectedNode = _treeView.Nodes[0];
			}
		}

		private void _treeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			var rootNode = e.Node as TreeNodeBase;

			if (rootNode != null)
			{
				Try(() => BindDependingItems(rootNode.ItemDependencies), Cursors.WaitCursor);
			}

			var candidate = e.Node as DeletionCandidateTreeNode;

			if (candidate != null)
			{
				Try(() => BindDependingItems(candidate.ItemDependencies), Cursors.WaitCursor);
			}
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (_dataGridView.SelectedRows.Count > 0)
			{
				_dataGridView.ClearSelection();
			}
		}

		private void _buttonYes_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Yes;
		}

		private void _buttonNo_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.No;
		}

		#region nested types

		private interface IImageProvider
		{
			[CanBeNull]
			string GetImageKey([NotNull] DeletionCandidateTreeNode node);

			[CanBeNull]
			string GetImageKey([NotNull] DeletionCandidateTreeNode node,
			                   bool selected);

			[NotNull]
			string GetImageKey([NotNull] TreeNodeBase node,
			                   [NotNull] Image image);

			[NotNull]
			string GetImageKey([NotNull] TreeNodeBase node,
			                   [NotNull] Image image,
			                   bool selected);
		}

		private class ImageListBuilder : IImageProvider
		{
			private readonly ImageList _imageList;

			public ImageListBuilder([NotNull] ImageList imageList)
			{
				Assert.ArgumentNotNull(imageList, nameof(imageList));

				_imageList = imageList;
			}

			public string GetImageKey(DeletionCandidateTreeNode node)
			{
				return GetImageKey(node, false);
			}

			public string GetImageKey(DeletionCandidateTreeNode node,
			                          bool selected)
			{
				Assert.ArgumentNotNull(node, nameof(node));

				if (node.Item.Image == null)
				{
					return null;
				}

				string key = FormatImageKey(node.Item, selected);

				AddImage(key, node.Item.Image);

				return key;
			}

			public string GetImageKey(TreeNodeBase node,
			                          Image image)
			{
				return GetImageKey(node, image, false);
			}

			public string GetImageKey(TreeNodeBase node,
			                          Image image,
			                          bool selected)
			{
				Assert.ArgumentNotNull(node, nameof(node));
				Assert.ArgumentNotNull(image, nameof(image));

				string key = string.Format("{0}#{1}", selected, node.GetType().FullName);

				AddImage(key, image);

				return key;
			}

			private void AddImage(string key, [NotNull] Image image)
			{
				Assert.ArgumentNotNull(image, nameof(image));

				if (! _imageList.Images.ContainsKey(key))
				{
					_imageList.Images.Add(key, image);
				}
			}

			private static string FormatImageKey([NotNull] Item item, bool selected)
			{
				return string.Format("{0}#{1}", selected, item.ImageKey);
			}
		}

		private class TreeNodeBase : TreeNode
		{
			private readonly List<DependingItemTableRow> _itemDependencies;

			public TreeNodeBase([NotNull] string text,
			                    [NotNull] IEnumerable<DependingItem> itemDependencies)
			{
				Text = text;
				_itemDependencies =
					itemDependencies.Select(item => new DependingItemTableRow(item))
					                .ToList();

				_itemDependencies.Sort(SortItemDependencies);
			}

			[NotNull]
			public IList<DependingItemTableRow> ItemDependencies => _itemDependencies;

			private static int SortItemDependencies(DependingItemTableRow row0,
			                                        DependingItemTableRow row1)
			{
				int imageSortOrderComparison = row0.ImageSortOrder.CompareTo(row1.ImageSortOrder);

				return imageSortOrderComparison != 0
					       ? imageSortOrderComparison
					       : string.Compare(row0.Name, row1.Name, StringComparison.CurrentCulture);
			}
		}

		private class DeletionCandidateTreeNode : TreeNodeBase
		{
			public DeletionCandidateTreeNode(
				[NotNull] ItemDeletionCandidate deletionCandidate,
				[NotNull] IEnumerable<DependingItem> itemDependencies)
				: base(deletionCandidate.Item.Text, itemDependencies)
			{
				Assert.ArgumentNotNull(deletionCandidate, nameof(deletionCandidate));

				Item = deletionCandidate.Item;
			}

			public Item Item { get; }
		}

		private class DependingItemTableRow
		{
			private readonly DateTime? _createdDate;
			private readonly DateTime? _lastChangedDate;

			private static readonly Image _canRemoveByCascadingDeletion =
				Resources.RemoveDependencyByCascadingDeletion;

			private static readonly Image _canRemoveByDeletingAssociation =
				Resources.RemoveDependencyByDeletingAssociation;

			private static readonly Image _cannotRemoveDependency =
				Resources.CannotRemoveDependency;

			public DependingItemTableRow([NotNull] DependingItem dependingItem)
			{
				Assert.ArgumentNotNull(dependingItem, nameof(dependingItem));

				Entity entity = dependingItem.Entity;

				if (! dependingItem.CanRemove)
				{
					Image = _cannotRemoveDependency;
					ImageSortOrder = 0;
					Action = "Cannot remove dependency";
				}
				else
				{
					if (dependingItem.RemovedByCascadingDeletion)
					{
						Image = _canRemoveByCascadingDeletion;
						ImageSortOrder = 1;
						Action = "Delete depending item";
					}
					else
					{
						Image = _canRemoveByDeletingAssociation;
						ImageSortOrder = 2;
						Action = "Remove relationship";
					}
				}

				Name = dependingItem.Name;

				Type = GetTypeDisplayName(entity.GetType());

				var annotatedEntity = entity as IAnnotated;
				if (annotatedEntity != null)
				{
					Description = annotatedEntity.Description;
				}

				var entityMetadata = entity as IEntityMetadata;
				if (entityMetadata != null)
				{
					CreatedBy = entityMetadata.CreatedByUser;
					_createdDate = entityMetadata.CreatedDate;
					LastChangedBy = entityMetadata.LastChangedByUser;
					_lastChangedDate = entityMetadata.LastChangedDate;
				}
			}

			[NotNull]
			private static string GetTypeDisplayName([NotNull] Type type)
			{
				return type.Name;

				// TODO
				// - tokenize at lowercase/uppercase change
				// - convert first uppercase character of tokens after the first to lowercase, if not followed by another uppercase character
			}

			[UsedImplicitly]
			public Image Image { get; }

			[UsedImplicitly]
			public string Name { get; }

			[UsedImplicitly]
			public string Type { get; }

			[UsedImplicitly]
			public string Action { get; }

			[UsedImplicitly]
			public string Description { get; }

			[UsedImplicitly]
			public object CreatedDate => _createdDate;

			[UsedImplicitly]
			public string CreatedBy { get; }

			[UsedImplicitly]
			public object LastChangedDate => _lastChangedDate;

			[UsedImplicitly]
			public string LastChangedBy { get; }

			public int ImageSortOrder { get; }
		}

		#endregion
	}
}

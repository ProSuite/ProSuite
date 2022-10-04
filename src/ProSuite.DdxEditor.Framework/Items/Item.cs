using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Items
{
	public abstract class Item
	{
		[NotNull] private static readonly IEnumerable<Item> _emptyItemList =
			new List<Item>();

		[NotNull] private static readonly List<DependingItem> _emptyDependingItemList =
			new List<DependingItem>();

		private const string _untitledText = "<Untitled>";
		[CanBeNull] private List<Item> _children;
		[CanBeNull] private string _text;
		[NotNull] private static readonly Image _image;

		[CanBeNull] private TableState _tableState;

		private bool _discardingChanges;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes the <see cref="Item"/> class.
		/// </summary>
		static Item()
		{
			_image = Resources.DefaultItemImage;
		}

		#region Constructors

		protected Item() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Item"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		protected Item([NotNull] string text) : this(text, string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Item"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="description">The description.</param>
		protected Item([NotNull] string text,
		               [CanBeNull] string description)
		{
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));

			_text = text;
			Description = description;
		}

		#endregion

		[NotNull]
		public IList<Item> Children
		{
			get
			{
				if (_children == null)
				{
					LoadChildren();
				}

				return Assert.NotNull(_children);
			}
		}

		public bool HasChildrenLoaded => _children != null;

		public void RefreshChildren()
		{
			LoadChildren();

			OnChildrenRefreshed(EventArgs.Empty);
		}

		[NotNull]
		public string Text => string.IsNullOrEmpty(_text)
			                      ? _untitledText
			                      : _text;

		public string Description { get; private set; }

		public virtual Image Image => _image;

		public virtual Image SelectedImage => Image;

		public virtual string ImageKey => GetType().FullName;

		public bool IsDirty { get; private set; }

		public abstract bool IsNew { get; }

		public bool CanDelete => CanDeleteCore;

		public IItemNavigation ItemNavigation { get; set; }

		public void SetText([NotNull] string value)
		{
			_text = value;
		}

		public void SetDescription([CanBeNull] string value)
		{
			Description = value;
		}

		public void NotifyChanged()
		{
			if (! _discardingChanges)
			{
				IsDirty = true;
			}

			OnChanged(EventArgs.Empty);
		}

		public bool IsValidForPersistence([NotNull] out Notification notification)
		{
			return IsValidForPersistenceCore(out notification);
		}

		/// <summary>
		/// Gets the depending items for this item
		/// </summary>
		/// <returns></returns>
		/// <remarks>To be called within a domain transaction</remarks>
		[NotNull]
		public virtual IList<DependingItem> GetDependingItems()
		{
			return _emptyDependingItemList;
		}

		public override string ToString()
		{
			return Text;
		}

		#region Non-public members

		[CanBeNull]
		protected internal Item Parent { get; private set; }

		internal event EventHandler Changed;

		internal event EventHandler Deleted;

		internal event EventHandler<ItemEventArgs> ChildAdded;

		internal event EventHandler<ItemEventArgs> ChildRemoved;

		internal event EventHandler ChildrenRefreshed;

		internal event EventHandler Unloaded;

		/// <summary>
		/// Creates the content control for the item
		/// </summary>
		/// <param name="itemNavigation"></param>
		/// <returns></returns>
		/// <remarks>Called within a domain transaction</remarks>
		[NotNull]
		internal Control CreateControl([NotNull] IItemNavigation itemNavigation)
		{
			return CreateControlCore(itemNavigation);
		}

		internal event EventHandler SavedChanges;

		internal event EventHandler DiscardedChanges;

		[NotNull]
		internal IList<ICommand> GetCommands(
			[NotNull] IApplicationController applicationController,
			[NotNull] ICollection<Item> selectedChildren)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));
			Assert.ArgumentNotNull(selectedChildren, nameof(selectedChildren));

			var result = new List<ICommand>();

			CollectDefaultCommands(result, applicationController);

			CollectCommands(result, applicationController, selectedChildren);

			if (AllowDeleteSelectedChildren &&
			    ! result.OfType<DeleteSelectedItemsCommand>().Any())
			{
				result.Add(new DeleteSelectedItemsCommand(selectedChildren,
				                                          applicationController));
			}

			return result;
		}

		[NotNull]
		internal IList<ICommand> GetCommands(
			[NotNull] IApplicationController applicationController)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			var result = new List<ICommand>();

			CollectDefaultCommands(result, applicationController);

			CollectCommands(result, applicationController);

			if (CanDelete && AllowDelete)
			{
				if (! result.OfType<IDeleteItemCommand>().Any())
				{
					result.Add(new DeleteItemCommand(this, applicationController));
				}
			}

			return result;
		}

		private void CollectDefaultCommands(
			[NotNull] IList<ICommand> commands,
			[NotNull] IApplicationController applicationController)
		{
			commands.Insert(0, new RefreshItemCommand(this, applicationController));

			if (Parent != null)
			{
				commands.Insert(0, new GoToParentCommand(this, applicationController));
			}
		}

		internal void StartEditing()
		{
			StartEditingCore();
		}

		internal void Unload()
		{
			OnUnloaded(EventArgs.Empty);
		}

		internal void PrepareDelete()
		{
			Assert.True(CanDelete, "Cannot delete item");

			DeleteCore();

			// item will be removed from its parent AFTER commit
			// event will be raised later (via NotifyDeleted)
		}

		internal void ApplyDelete()
		{
			// this is slow for many items --> would be faster when
			// setting the tree in edit mode during these changes
			Parent?.RemoveChild(this);

			NotifyDeleted();
		}

		internal void RequestCommit()
		{
			RequestCommitCore();
		}

		internal void EndCommit()
		{
			EndCommitCore();
		}

		internal void DiscardChanges()
		{
			DiscardChangesCore();

			IsDirty = false;
			OnChanged(EventArgs.Empty);
		}

		internal void Refresh()
		{
			OnChanged(EventArgs.Empty);
		}

		internal void NotifySavedChanges()
		{
			IsDirty = false;

			OnSavedChanges(EventArgs.Empty);
		}

		internal void NotifyDiscardedChanges()
		{
			IsDirty = false;

			bool oldDiscardingChanges = _discardingChanges;

			try
			{
				_discardingChanges = true;

				OnDiscardedChanges(EventArgs.Empty);
			}
			finally
			{
				_discardingChanges = oldDiscardingChanges;
			}
		}

		internal void NotifyDeleted()
		{
			IsDirty = false;

			OnDeleted(EventArgs.Empty);
		}

		protected virtual bool AllowDelete => false;

		protected virtual bool AllowDeleteSelectedChildren => false;

		protected virtual bool CanDeleteCore => false;

		protected internal bool RemoveChild([NotNull] Item child)
		{
			Assert.ArgumentNotNull(child, nameof(child));
			Assert.AreEqual(this, child.Parent, "Not a child of this item");

			if (! Children.Remove(child))
			{
				return false;
			}

			OnChildRemoved(new ItemEventArgs(child));
			return true;
		}

		[NotNull]
		protected Item RegisterChild([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.Null(item.Parent, "Item already has parent");

			item.Parent = this;

			return item;
		}

		protected virtual bool IsValidForPersistenceCore(
			[NotNull] out Notification notification)
		{
			notification = Notification.Valid();
			return true;
		}

		/// <summary>
		/// Creates the content control for the item
		/// </summary>
		/// <param name="itemNavigation"></param>
		/// <returns></returns>
		/// <remarks>Called within a domain transaction</remarks>
		[NotNull]
		protected abstract Control CreateControlCore(
			[NotNull] IItemNavigation itemNavigation);

		[NotNull]
		protected virtual IEnumerable<Item> GetChildren()
		{
			return _emptyItemList;
		}

		protected virtual void StartEditingCore() { }

		protected virtual void RequestCommitCore() { }

		protected virtual void EndCommitCore() { }

		protected virtual void DeleteCore() { }

		protected virtual void DiscardChangesCore() { }

		protected virtual void OnUnloaded(EventArgs e)
		{
			Unloaded?.Invoke(this, e);
		}

		protected virtual void OnChanged(EventArgs e)
		{
			Changed?.Invoke(this, e);
		}

		protected virtual void OnDeleted(EventArgs e)
		{
			Deleted?.Invoke(this, e);
		}

		protected virtual void OnChildAdded(ItemEventArgs e)
		{
			ChildAdded?.Invoke(this, e);
		}

		protected virtual void OnChildRemoved(ItemEventArgs e)
		{
			ChildRemoved?.Invoke(this, e);
		}

		protected virtual void OnChildrenRefreshed(EventArgs e)
		{
			ChildrenRefreshed?.Invoke(this, e);
		}

		protected virtual void OnSavedChanges(EventArgs e)
		{
			SavedChanges?.Invoke(this, e);
		}

		protected virtual void OnDiscardedChanges(EventArgs e)
		{
			DiscardedChanges?.Invoke(this, e);
		}

		protected virtual void CollectCommands(
			[NotNull] List<ICommand> commands,
			[NotNull] IApplicationController applicationController) { }

		protected virtual void CollectCommands(
			[NotNull] List<ICommand> commands,
			[NotNull] IApplicationController applicationController,
			[NotNull] ICollection<Item> selectedChildren) { }

		protected void AddChild([NotNull] Item child)
		{
			Assert.ArgumentNotNull(child, nameof(child));
			Assert.Null(child.Parent, "Item to be added already has parent");

			Children.Add(child);
			child.Parent = this;

			OnChildAdded(new ItemEventArgs(child));
		}

		[NotNull]
		protected ItemTableControl<T> CreateTableControl<T>(
			[NotNull] Func<IEnumerable<T>> getRows,
			[NotNull] IItemNavigation itemNavigation,
			bool hideGridLines)
			where T : class
		{
			return CreateTableControl(getRows,
			                          itemNavigation, hideGridLines,
			                          new string[] { });
		}

		[NotNull]
		protected ItemTableControl<T> CreateTableControl<T>(
			[NotNull] Func<IEnumerable<T>> getRows,
			[NotNull] IItemNavigation itemNavigation) where T : class
		{
			const bool hideGridLines = false;
			return CreateTableControl(getRows, itemNavigation, hideGridLines, new string[] { });
		}

		[NotNull]
		protected ItemTableControl<T> CreateTableControl<T>(
			[NotNull] Func<IEnumerable<T>> getRows,
			[NotNull] IItemNavigation itemNavigation,
			bool hideGridLines,
			[NotNull] IEnumerable<ColumnDescriptor> columnDescriptors) where T : class
		{
			Assert.ArgumentNotNull(getRows, nameof(getRows));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));
			Assert.ArgumentNotNull(columnDescriptors, nameof(columnDescriptors));

			if (_tableState == null)
			{
				_tableState = new TableState();
			}

			return WireControl(new ItemTableControl<T>(getRows,
			                                           _tableState,
			                                           columnDescriptors),
			                   itemNavigation, hideGridLines);
		}

		[NotNull]
		protected ItemTableControl<T> CreateTableControl<T>(
			[NotNull] Func<IEnumerable<T>> getRows,
			[NotNull] IItemNavigation itemNavigation,
			params string[] hiddenProperties) where T : class
		{
			return CreateTableControl(getRows, itemNavigation,
			                          false, hiddenProperties);
		}

		[NotNull]
		protected ItemTableControl<T> CreateTableControl<T>(
			[NotNull] Func<IEnumerable<T>> getRows,
			[NotNull] IItemNavigation itemNavigation,
			bool hideGridLines,
			params string[] hiddenProperties) where T : class
		{
			Assert.ArgumentNotNull(getRows, nameof(getRows));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));
			Assert.ArgumentNotNull(hiddenProperties, nameof(hiddenProperties));

			if (_tableState == null)
			{
				_tableState = new TableState();
			}

			Stopwatch watch = _msg.IsVerboseDebugEnabled
				                  ? _msg.DebugStartTiming()
				                  : null;

			var control = new ItemTableControl<T>(getRows,
			                                      _tableState,
			                                      hiddenProperties);

			_msg.DebugStopTiming(watch, "ItemTableControl created");

			return WireControl(control, itemNavigation, hideGridLines);
		}

		[NotNull]
		private ItemTableControl<T> WireControl<T>([NotNull] ItemTableControl<T> control,
		                                           [NotNull] IItemNavigation itemNavigation,
		                                           bool hideGridLines) where T : class
		{
			control.HideGridLines = hideGridLines;

			new ItemTablePresenter<T>(control, this, itemNavigation);

			return control;
		}

		private void LoadChildren()
		{
			_children = new List<Item>(GetChildren());

			if (SortChildren)
			{
				_children.Sort(CompareChildren);
			}

			Assert.NotNull(_children).ForEach(child => child.Parent = this);
		}

		protected virtual int CompareChildren([CanBeNull] Item child1,
		                                      [CanBeNull] Item child2)
		{
			if (child1 == null)
			{
				return -1;
			}

			if (child2 == null)
			{
				return 1;
			}

			return string.Compare(child1.Text, child2.Text,
			                      StringComparison.CurrentCulture);
		}

		protected virtual bool SortChildren => false;

		#endregion
	}
}

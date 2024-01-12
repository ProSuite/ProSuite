using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Env;
using ProSuite.Commons.UI.Keyboard;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Help;
using ProSuite.DdxEditor.Framework.History;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Options;
using ProSuite.DdxEditor.Framework.Search;

namespace ProSuite.DdxEditor.Framework
{
	public class ApplicationController : IApplicationShellObserver,
	                                     IApplicationController
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly IOptionsManager _optionsManager;
		[CanBeNull] private readonly IConfigurationManager _configurationManager;
		[NotNull] private readonly ItemHistory _history = new ItemHistory();
		[NotNull] private readonly IMessageBox _messageBox;
		[NotNull] private readonly IUnitOfWork _unitOfWork;
		[NotNull] private readonly IApplicationShell _view;
		[CanBeNull] private Item _currentItem;
		[CanBeNull] private HtmlHelpForm _htmlHelpForm;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationController"/> class.
		/// </summary>
		/// <param name="view">The shell.</param>
		/// <param name="unitOfWork">The unit of work.</param>
		/// <param name="messageBox">The message box.</param>
		/// <param name="helpProviders">The help providers.</param>
		/// <param name="optionsManager">The options manager.</param>
		/// <param name="searchProviders">The search providers.</param>
		/// <param name="configurationManager"></param>
		public ApplicationController(
			[NotNull] IApplicationShell view,
			[NotNull] IUnitOfWork unitOfWork,
			[NotNull] IMessageBox messageBox,
			[CanBeNull] IEnumerable<IHelpProvider> helpProviders = null,
			[CanBeNull] IOptionsManager optionsManager = null,
			[CanBeNull] IEnumerable<ISearchProvider> searchProviders = null,
			[CanBeNull] IConfigurationManager configurationManager = null)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));
			Assert.ArgumentNotNull(messageBox, nameof(messageBox));

			_view = view;
			_unitOfWork = unitOfWork;
			_messageBox = messageBox;

			if (helpProviders != null)
			{
				_view.AddHelpMenuItems(helpProviders);
			}

			if (searchProviders != null)
			{
				_view.AddSearchMenuItems(searchProviders, this);
			}

			_optionsManager = optionsManager;

			optionsManager?.RestoreOptions();

			_configurationManager = configurationManager;

			_view.Observer = this;

			UpdateAppearance();
		}

		#endregion

		#region IApplicationController Members

		IWin32Window IApplicationController.Window => _view;

		Item IApplicationController.CurrentItem => _currentItem;

		bool IApplicationController.CanLoadItem(Item item)
		{
			return CanLoadItem(item);
		}

		bool IApplicationController.PrepareItemSelection(Item item)
		{
			if (! HasPendingChanges)
			{
				return true;
			}

			const bool canCancel = true;
			return HandlePendingChanges(canCancel);
		}

		void IApplicationController.LoadItem(Item item)
		{
			LoadItemCore(item);
		}

		void IApplicationController.ReloadCurrentItem()
		{
			ReloadCurrentItemCore();
		}

		bool IApplicationController.HasPendingChanges => HasPendingChanges;

		bool IApplicationController.CanDeleteItem(Item item)
		{
			return CanDeleteItem(item);
		}

		bool IApplicationController.DeleteItems(IEnumerable<Item> items)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			var deletableItems = new List<ItemDeletionCandidate>();
			var nonDeletableItems = new List<ItemDeletionCandidate>();
			var deletableDependencies = new List<DependingItem>();

			var itemsCount = 0;

			_unitOfWork.UseTransaction(
				delegate
				{
					foreach (Item item in GetUniqueItems(items))
					{
						itemsCount++;

						var deletionCandidate = new ItemDeletionCandidate(item);

						bool deletable = CanDeleteItem(item);

						foreach (DependingItem dependingItem in item.GetDependingItems())
						{
							deletionCandidate.AddDependingItem(dependingItem);

							if (dependingItem.CanRemove)
							{
								deletableDependencies.Add(dependingItem);
							}
							else
							{
								deletable = false;
							}
						}

						if (deletable)
						{
							deletableItems.Add(deletionCandidate);
						}
						else
						{
							nonDeletableItems.Add(deletionCandidate);
						}
					}
				});

			if (deletableItems.Count == 0 && nonDeletableItems.Count == 0)
			{
				return false;
			}

			if (! _view.ConfirmItemDeletion(deletableItems, nonDeletableItems))
			{
				return false;
			}

			// TODO: consider returning items to delete from shell (as confirmation), delete only the returned items

			string itemsText = itemsCount == 1
				                   ? "the selected item"
				                   : $"the {itemsCount} selected items";

			using (_msg.IncrementIndentation("Deleting {0}", itemsText))
			{
				// remove from parent item, delete the entity
				try
				{
					_unitOfWork.UseTransaction(
						delegate
						{
							foreach (DependingItem dependingItem in deletableDependencies)
							{
								_msg.InfoFormat("Removing reference from {0}",
								                dependingItem.Name);

								dependingItem.RemoveDependency();
							}

							foreach (ItemDeletionCandidate itemDeletionCandidate in deletableItems)
							{
								itemDeletionCandidate.Item.PrepareDelete();
							}

							_unitOfWork.Commit();
						});
				}
				catch (Exception)
				{
					_unitOfWork.Reset();
					throw;
				}

				foreach (ItemDeletionCandidate itemDeletionCandidate in deletableItems)
				{
					itemDeletionCandidate.Item.ApplyDelete();
				}
			}

			return true;
		}

		bool IApplicationController.DeleteItem(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.True(CanDeleteItem(item), "Cannot delete item {0}", item.Text);

			IList<DependingItem> dependingItems =
				_unitOfWork.ReadOnlyTransaction(item.GetDependingItems);

			var deletableItems = new List<ItemDeletionCandidate>();
			var nonDeletableItems = new List<ItemDeletionCandidate>();

			var itemDeletionCandidate = new ItemDeletionCandidate(item);

			var canRemoveDependencies = true;
			foreach (DependingItem dependingItem in dependingItems)
			{
				if (! dependingItem.CanRemove)
				{
					canRemoveDependencies = false;
				}

				itemDeletionCandidate.AddDependingItem(dependingItem);
			}

			if (canRemoveDependencies)
			{
				deletableItems.Add(itemDeletionCandidate);
			}
			else
			{
				nonDeletableItems.Add(itemDeletionCandidate);
			}

			bool confirmed;

			if (deletableItems.Count == 1 &&
			    ! deletableItems[0].DependingItems.Any(
				    dependingItem => dependingItem.RequiresConfirmation))
			{
				DialogResult result = _messageBox.Show(_view,
				                                       string.Format(
					                                       "Are you sure you want to delete this item?{0}{0}{1}",
					                                       Environment.NewLine,
					                                       deletableItems[0].Item.Text),
				                                       "Confirm Deletion",
				                                       MessageBoxButtons.YesNo,
				                                       MessageBoxIcon.Question,
				                                       MessageBoxDefaultButton.Button1);

				confirmed = result == DialogResult.Yes;
			}
			else
			{
				confirmed = _view.ConfirmItemDeletion(deletableItems, nonDeletableItems);
			}

			if (! confirmed)
			{
				return false;
			}

			using (_msg.IncrementIndentation("Deleting {0}", item.Text))
			{
				// try to delete the entity
				try
				{
					_unitOfWork.UseTransaction(
						delegate
						{
							foreach (DependingItem dependingItem in dependingItems)
							{
								_msg.InfoFormat("Removing reference from {0}",
								                dependingItem.Name);

								dependingItem.RemoveDependency();
							}

							item.PrepareDelete();

							_unitOfWork.Commit();
						});
				}
				catch (Exception)
				{
					_unitOfWork.Reset();
					throw;
				}
			}

			// remove from tree *after* successful commit
			item.ApplyDelete();

			// this throws an exception (AdoTransaction disposed), not sure why
			// _unitOfWork.ReadOnlyTransaction(delegate { item.NotifyDeleted(); });

			return true;
		}

		T IApplicationController.ReadInTransaction<T>(Func<T> function)
		{
			return _unitOfWork.ReadOnlyTransaction(function);
		}

		#endregion

		#region IItemNavigation

		IEnumerable<I> IItemNavigation.FindItems<I>()
		{
			return _view.FindItems<I>();
		}

		I IItemNavigation.FindFirstItem<I>()
		{
			return _view.FindFirstItem<I>();
		}

		Item IItemNavigation.FindItem(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			return _view.FindItem(entity);
		}

		void IItemNavigation.RefreshFirstItem<I>()
		{
			var item = _view.FindFirstItem<I>();

			if (item == null)
			{
				throw new InvalidOperationException($"Item not found for type {typeof(I)}");
			}

			item.RefreshChildren();
		}

		void IItemNavigation.RefreshItem(Item item)
		{
			RefreshItemCore(item);
		}

		bool IItemNavigation.RefreshItem(Entity entity)
		{
			Item item = _view.FindItem(entity);
			if (item == null)
			{
				return false;
			}

			RefreshItemCore(item);
			return true;
		}

		bool IItemNavigation.GoToItem(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			return _view.GoToItem(item);
		}

		bool IItemNavigation.GoToItem(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			Item item = _view.FindItem(entity);

			return item != null && _view.GoToItem(item);
		}

		void IItemNavigation.ShowItemHelp(string title, string html)
		{
			if (_htmlHelpForm == null || _htmlHelpForm.IsDisposed)
			{
				_htmlHelpForm = new HtmlHelpForm(html);
				_htmlHelpForm.Show(_view);
			}
			else
			{
				_htmlHelpForm.NavigateToString(html);
			}

			_htmlHelpForm.Text = title;
		}

		void IItemNavigation.UpdateItemHelp(string title, string html)
		{
			if (_htmlHelpForm is { IsDisposed: false })
			{
				_htmlHelpForm.NavigateToString(html);
				_htmlHelpForm.Text = title;
			}
		}

		#endregion

		#region IApplicationShellObserver Members

		bool IApplicationShellObserver.TrySavePendingChanges()
		{
			return TrySavePendingChangesCore();
		}

		void IApplicationShellObserver.DiscardPendingChanges()
		{
			const string message = "Are you sure you want to discard all pending changes?";

			DialogResult result =
				_messageBox.Show(_view, message, "Discard changes",
				                 MessageBoxButtons.YesNo,
				                 MessageBoxIcon.Question,
				                 MessageBoxDefaultButton.Button1);

			if (result != DialogResult.Yes)
			{
				return;
			}

			DiscardPendingChangesCore();
		}

		bool IApplicationShellObserver.HandleFormClosing(bool canCancel)
		{
			return ! HasPendingChanges || HandlePendingChanges(canCancel);
		}

		void IApplicationShellObserver.FormClosed()
		{
			if (_optionsManager != null)
			{
				_optionsManager.SaveOptions();
			}
		}

		void IApplicationShellObserver.GoBack()
		{
			Item item = _history.GoBack();

			_view.GoToItem(item);
		}

		void IApplicationShellObserver.GoForward()
		{
			Item item = _history.GoForward();

			_view.GoToItem(item);
		}

		public void ShowConfiguration()
		{
			IConfigurationManager configManager = Assert.NotNull(_configurationManager);

			configManager.ShowConfigurationsDialog();
		}

		void IApplicationShellObserver.ShowOptions()
		{
			IOptionsManager optionsManager = Assert.NotNull(_optionsManager,
			                                                "Unable to show options");

			optionsManager.ShowOptionsDialog(this, _view);
		}

		void IApplicationShellObserver.ShowAbout()
		{
			UIEnvironment.ShowDialog(new AboutForm(_view.Title), _view);
		}

		void IApplicationShellObserver.KeyDownPreview(KeyEventArgs e)
		{
			const bool exclusive = true;

			// check for CTRL+S --> save pending changes
			if (e.KeyCode == Keys.S &&
			    KeyboardUtils.IsModifierPressed(e, Keys.Control, exclusive))
			{
				if (HasPendingChanges)
				{
					TrySavePendingChangesCore();

					e.Handled = true;
					return;
				}
			}

			// check for F5 -> refresh
			if (e.KeyCode == Keys.F5)
			{
				if (_currentItem != null && ! HasPendingChanges)
				{
					RefreshItemCore(_currentItem);
					e.Handled = true;
				}
			}
		}

		#endregion

		#region Non-public methods

		private bool HasPendingChanges => _currentItem != null && _currentItem.IsDirty;

		/// <summary>
		/// Gets the unique items from the collection (filters out duplicate entity items)
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		/// <remarks>This method assumes that all items are of the same type.</remarks>
		[NotNull]
		private static IEnumerable<Item> GetUniqueItems([NotNull] IEnumerable<Item> items)
		{
			// collects unique entity ids (assumes all items are of same type)
			var entityIds = new HashSet<int>();

			foreach (Item item in items)
			{
				var entityItem = item as IEntityItem;

				if (entityItem == null)
				{
					yield return item;
				}
				else
				{
					if (entityIds.Add(entityItem.EntityId))
					{
						yield return item;
					}
				}
			}
		}

		private void RefreshItemCore([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			if (HasPendingChanges)
			{
				throw new InvalidOperationException(
					"there are pending changes, unable to refresh");
			}

			using (new WaitCursor())
			{
				if (_currentItem == item)
				{
					ReloadCurrentItemCore();
				}

				item.Refresh();

				item.RefreshChildren();
			}

			_msg.InfoFormat("Refreshed '{0}'", item.Text);
		}

		private void ReloadCurrentItemCore()
		{
			if (_currentItem != null)
			{
				LoadItemCore(_currentItem);
			}
		}

		private void LoadItemCore([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.True(CanLoadItem(item), "Cannot load item {0}", item);

			_msg.VerboseDebug(() => $"Loading item {item.Text}");

			if (IsEditing())
			{
				StopEditing();
			}

			if (_currentItem != null)
			{
				_currentItem.Unload();
				_currentItem.Changed -= _currentItem_Changed;
				_currentItem = null;
			}

			_currentItem = item;
			_currentItem.Changed += _currentItem_Changed;

			StartEditing();

			// Important: binding must happen AFTER StartEditing: the Refresh done in 
			//            StartEditing() creates NEW instances of nh component classes

			// use read-only transaction to load item content
			// TODO test; was using rw-transaction before:
			// _unitOfWork.UseTransaction(() => _view.LoadContent(item, this));
			_unitOfWork.ReadOnlyTransaction(() => _view.LoadContent(item, this));

			_history.Add(item);

			AddCommandButtons(item);

			UpdateAppearance();
		}

		private void AddCommandButtons([NotNull] Item item)
		{
			var commands = new List<ICommand>();

			foreach (ICommand command in item.GetCommands(this))
			{
				if (command.Image != null)
				{
					commands.Add(command);
				}
			}

			_view.SetCommandButtons(commands);
		}

		private bool CanDeleteItem([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			// enabled if the item to be deleted supports deletion at all
			// and if there are no pending changes
			return item.CanDelete && ! HasPendingChanges;
		}

		private bool CanLoadItem([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			return ! HasPendingChanges;
		}

		private bool TrySavePendingChangesCore()
		{
			Assert.True(HasPendingChanges, "no pending changes");
			AssertIsEditing();

			Item currentItem = Assert.NotNull(_currentItem);

			var valid = true;
			Notification notification = null;
			_unitOfWork.ReadOnlyTransaction(
				() => valid = currentItem.IsValidForPersistence(out notification));

			if (! valid)
			{
				ReportValidationErrors(notification);
				return false;
			}

			bool wasNew = currentItem.IsNew;
			try
			{
				_unitOfWork.UseTransaction(
					delegate
					{
						currentItem.RequestCommit();

						_unitOfWork.Flush();

						_unitOfWork.Commit();

						// flush succeeded, finalize commit (update item state)
						currentItem.EndCommit();
					});
			}
			catch (Exception e)
			{
				_msg.Debug($"{nameof(TrySavePendingChangesCore)} failed", e);

				// Would be nicer: keep the changes in the UI, apply them again to the entity
				// But: items should anyway check if valid for persistence, 
				//      ORA constraint violations should not occur

				DiscardPendingChangesCore(wasNew);

				if (! wasNew)
				{
					// failed update; need to reload the item in a new session
					LoadItemCore(currentItem);
				}

				throw;
			}

			_unitOfWork.ReadOnlyTransaction(currentItem.NotifySavedChanges);

			//StopEditing();
			//StartEditing();

			UpdateAppearance();

			_msg.InfoFormat("Changes saved");

			return true;
		}

		private void StartEditing()
		{
			Item currentItem = Assert.NotNull(_currentItem,
			                                  "currentItem is null");
			Assert.False(HasPendingChanges && ! currentItem.IsNew,
			             "pending changes only valid for new items");
			AssertIsNotEditing();

			_unitOfWork.Start();

			_unitOfWork.UseTransaction(() => currentItem.StartEditing());
		}

		private bool IsEditing()
		{
			return _unitOfWork.Started;
		}

		private void StopEditing()
		{
			AssertIsEditing();

			_unitOfWork.Stop();
		}

		private void DiscardEdits()
		{
			AssertIsEditing();

			_unitOfWork.Reset();
		}

		private void AssertIsEditing()
		{
			Assert.True(IsEditing(), "Not currently editing");
		}

		private void AssertIsNotEditing()
		{
			Assert.False(IsEditing(), "Already editing");
		}

		private void ReportValidationErrors([NotNull] Notification notification)
		{
			Assert.ArgumentNotNull(notification, nameof(notification));

			string notificationText = ValidationUtils.FormatNotification(notification);

			_messageBox.Show(_view,
			                 string.Format("Unable to save changes:{0}{0}{1}",
			                               Environment.NewLine,
			                               notificationText),
			                 "Save Changes",
			                 MessageBoxButtons.OK,
			                 MessageBoxIcon.Warning,
			                 MessageBoxDefaultButton.Button1);
		}

		private void DiscardPendingChangesCore()
		{
			const bool isFailedInsert = false;
			DiscardPendingChangesCore(isFailedInsert);

			_msg.Info("Changes discarded");
		}

		private void DiscardPendingChangesCore(bool isFailedInsert)
		{
			Assert.True(HasPendingChanges, "no pending changes");
			Assert.True(IsEditing(), "not currently editing");
			Item currentItem = Assert.NotNull(_currentItem);

			bool delete = isFailedInsert
				              ? true
				              : currentItem.IsNew;

			_unitOfWork.ReadOnlyTransaction(currentItem.DiscardChanges);

			DiscardEdits();

			if (delete)
			{
				// TODO remove from parent's children collection?
				currentItem.NotifyDeleted();
			}
			else
			{
				_unitOfWork.ReadOnlyTransaction(currentItem.NotifyDiscardedChanges);
			}

			// TODO: the item should be re-validated and the UI updated 
		}

		/// <summary>
		/// Handles the pending changes.
		/// </summary>
		/// <param name="canCancel">if set to <c>true</c> the user should be presented
		/// with the option to "Cancel".</param>
		/// <returns><c>true</c> if the changes where handled successfully, <c>false</c>
		/// if the user cancelled the operation.</returns>
		private bool HandlePendingChanges(bool canCancel)
		{
			const string caption = "Data Dictionary Editor - Pending Changes";
			const string messageText = "Do you want to save changes?";

			DialogResult result;
			if (canCancel)
			{
				result = _messageBox.Show(_view, messageText, caption,
				                          MessageBoxButtons.YesNoCancel,
				                          MessageBoxIcon.Question,
				                          MessageBoxDefaultButton.Button3);
			}
			else
			{
				result = _messageBox.Show(_view, messageText, caption,
				                          MessageBoxButtons.YesNo,
				                          MessageBoxIcon.Question,
				                          MessageBoxDefaultButton.Button1);
			}

			switch (result)
			{
				case DialogResult.Cancel:
					return false;

				case DialogResult.Yes:
					bool saved = TrySavePendingChangesCore();
					if (saved)
					{
						return true;
					}

					if (canCancel)
					{
						return false;
					}

					DiscardPendingChangesCore();
					return true;

				case DialogResult.No:
					DiscardPendingChangesCore();
					return true;

				default:
					throw new NotSupportedException(
						string.Format("Unsupported dialog result: {0}", result));
			}
		}

		private void UpdateAppearance()
		{
			// TODO privileges
			_view.SaveEnabled = HasPendingChanges;
			_view.DiscardChangesEnabled = HasPendingChanges;

			_view.GoBackEnabled = _history.CanGoBack;
			_view.GoForwardEnabled = _history.CanGoForward;
			_view.ShowOptionsVisible = _optionsManager != null;
			_view.ShowConfigurationVisible = _configurationManager != null;

			_view.UpdateCommandButtonAppearance();
		}

		#region Event handlers

		private void _currentItem_Changed(object sender, EventArgs e)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("ApplicationController._currentItem_Changed");
			}

			UpdateAppearance();
		}

		#endregion

		#endregion
	}
}

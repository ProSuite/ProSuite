using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.ContentPanel;
using ProSuite.DdxEditor.Framework.Help;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Menus;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DdxEditor.Framework.Options;
using ProSuite.DdxEditor.Framework.Search;

namespace ProSuite.DdxEditor.Framework
{
	public partial class ApplicationShell : Form, IApplicationShell,
	                                        IFormStateAware<ApplicationShellState>
	{
		private readonly ContentControl _contentControl;

		private readonly FormStateManager<ApplicationShellState> _formStateManager;
		private readonly NavigationControl _navigationControl;
		private IApplicationShellObserver _observer;

		private readonly List<CommandToolStripButton> _commandButtons =
			new List<CommandToolStripButton>();

		#region Factory methods

		[NotNull]
		public static ApplicationShell Create(
			[NotNull] Control logWindowControl,
			[NotNull] string title,
			[NotNull] IItemModelBuilder modelBuilder,
			[NotNull] IUnitOfWork unitOfWork,
			[CanBeNull] IHelpProvider helpProvider = null,
			[CanBeNull] IOptionsManager optionsManager = null)
		{
			return Create(logWindowControl, title, modelBuilder, unitOfWork,
			              helpProvider == null
				              ? null
				              : new[] {helpProvider},
			              optionsManager);
		}

		public static ApplicationShell Create(
			[NotNull] Control logWindowControl,
			[NotNull] string title,
			[NotNull] IItemModelBuilder modelBuilder,
			[NotNull] IUnitOfWork unitOfWork,
			[CanBeNull] IEnumerable<IHelpProvider> helpProviders = null,
			[CanBeNull] IOptionsManager optionsManager = null,
			[CanBeNull] IEnumerable<IItemLocator> itemLocators = null,
			[CanBeNull] IEnumerable<ISearchProvider> searchProviders = null)
		{
			Assert.ArgumentNotNull(logWindowControl, nameof(logWindowControl));
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			var navigationControl = new NavigationControl(itemLocators);
			var contentControl = new ContentControl();

			var shell = new ApplicationShell(navigationControl,
			                                 contentControl,
			                                 logWindowControl)
			            {Text = title};

			var applicationController =
				new ApplicationController(shell, unitOfWork,
				                          new MessageBoxImpl(),
				                          helpProviders,
				                          optionsManager,
				                          searchProviders);

			IMenuManager menuManager = new MenuManager(applicationController);
			navigationControl.MenuManager = menuManager;
			contentControl.MenuManager = menuManager;

			new NavigationController(navigationControl, applicationController, modelBuilder);
			new ContentController(contentControl, applicationController);

			return shell;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationShell"/> class.
		/// </summary>
		/// <param name="navigationControl">The navigation control.</param>
		/// <param name="contentControl">The content control.</param>
		/// <param name="logWindow">The log window control.</param>
		public ApplicationShell([NotNull] NavigationControl navigationControl,
		                        [NotNull] ContentControl contentControl,
		                        [NotNull] Control logWindow)
		{
			Assert.ArgumentNotNull(navigationControl, nameof(navigationControl));
			Assert.ArgumentNotNull(contentControl, nameof(contentControl));
			Assert.ArgumentNotNull(logWindow, nameof(logWindow));

			InitializeComponent();

			_formStateManager = new FormStateManager<ApplicationShellState>(this);
			_formStateManager.RestoreState();

			_navigationControl = navigationControl;
			_contentControl = contentControl;

			navigationControl.Dock = DockStyle.Fill;
			_splitContainerOuter.Panel1.Controls.Add(navigationControl);

			contentControl.Dock = DockStyle.Fill;
			_splitContainerInner.Panel1.Controls.Add(contentControl);

			logWindow.Dock = DockStyle.Fill;
			_splitContainerInner.Panel2.Controls.Add(logWindow);
		}

		#endregion

		#region IApplicationShell Members

		IApplicationShellObserver IApplicationShell.Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		string IApplicationShell.Title
		{
			get { return Text; }
			set { Text = value; }
		}

		void IApplicationShell.LoadContent(Item item, IItemNavigation itemNavigation)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_contentControl.LoadContent(item, itemNavigation);
		}

		bool IApplicationShell.SaveEnabled
		{
			get { return _toolStripButtonSave.Enabled; }
			set { _toolStripButtonSave.Enabled = value; }
		}

		bool IApplicationShell.DiscardChangesEnabled
		{
			get { return _toolStripButtonDiscardChanges.Enabled; }
			set { _toolStripButtonDiscardChanges.Enabled = value; }
		}

		bool IApplicationShell.GoBackEnabled
		{
			get { return _toolStripButtonBack.Enabled; }
			set { _toolStripButtonBack.Enabled = value; }
		}

		bool IApplicationShell.GoForwardEnabled
		{
			get { return _toolStripButtonForward.Enabled; }
			set { _toolStripButtonForward.Enabled = value; }
		}

		bool IApplicationShell.ShowOptionsVisible
		{
			get { return _toolStripMenuItemOptions.Visible; }
			set { _toolStripMenuItemOptions.Visible = value; }
		}

		bool IApplicationShell.GoToItem(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			return _navigationControl.GoToItem(item);
		}

		IEnumerable<I> IApplicationShell.FindItems<I>()
		{
			return _navigationControl.FindItems<I>();
		}

		I IApplicationShell.FindFirstItem<I>()
		{
			return _navigationControl.FindFirstItem<I>();
		}

		Item IApplicationShell.FindItem(Entity entity)
		{
			return _navigationControl.FindItem(entity);
		}

		void IApplicationShell.SetCommandButtons(IEnumerable<ICommand> commands)
		{
			foreach (CommandToolStripButton button in _commandButtons)
			{
				_toolStrip.Items.Remove(button);
			}

			_commandButtons.Clear();

			foreach (ICommand command in commands)
			{
				var button = new CommandToolStripButton(command);

				if (command is IGenericItemCommand)
				{
					button.Alignment = ToolStripItemAlignment.Left;
				}
				else
				{
					button.Alignment = ToolStripItemAlignment.Right;
				}

				_commandButtons.Add(button);
			}

			for (int i = _commandButtons.Count - 1; i >= 0; i--)
			{
				CommandToolStripButton button = _commandButtons[i];
				_toolStrip.Items.Add(button);
			}
		}

		public void UpdateCommandButtonAppearance()
		{
			foreach (CommandToolStripButton button in _commandButtons)
			{
				button.UpdateAppearance();
			}
		}

		bool IApplicationShell.ConfirmItemDeletion(
			ICollection<ItemDeletionCandidate> deletableItems,
			ICollection<ItemDeletionCandidate> nonDeletableItems)
		{
			Assert.ArgumentNotNull(deletableItems, nameof(deletableItems));
			Assert.ArgumentNotNull(nonDeletableItems, nameof(nonDeletableItems));

			using (var form = new DeleteItemsForm(deletableItems, nonDeletableItems))
			{
				DialogResult result = form.ShowDialog(this);

				bool confirmed = result == DialogResult.Yes;

				if (deletableItems.Count == 0)
				{
					Assert.False(confirmed,
					             "Unexpected confirmation result (there are no deletable items)");
				}

				return confirmed;
			}
		}

		void IApplicationShell.AddHelpMenuItems(
			IEnumerable<IHelpProvider> helpProviders)
		{
			Assert.ArgumentNotNull(helpProviders, nameof(helpProviders));

			var menuItems = new List<ToolStripItem>();

			foreach (IHelpProvider hp in helpProviders)
			{
				if (! (hp is NopHelpProvider))
				{
					menuItems.Add(new CommandMenuItem(new HelpProviderCommand(hp, this)));
				}
				else
				{
					menuItems.Add(new ToolStripSeparator());
				}
			}

			ToolStripItemCollection items = _toolStripMenuItemHelp.DropDownItems;

			var insertionIndex = 0;
			foreach (ToolStripItem menuItem in menuItems)
			{
				items.Insert(insertionIndex, menuItem);
				insertionIndex++;
			}

			if (insertionIndex > 0)
			{
				items.Insert(insertionIndex, new ToolStripSeparator());
			}
		}

		void IApplicationShell.AddSearchMenuItems(
			IEnumerable<ISearchProvider> searchProviders, IItemNavigation itemNavigation)
		{
			Assert.ArgumentNotNull(searchProviders, nameof(searchProviders));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			var menuItems = new List<ToolStripItem>();

			foreach (ISearchProvider sp in searchProviders)
			{
				if (! (sp is NopSearchProvider))
				{
					menuItems.Add(new CommandMenuItem(new SearchCommand(sp, itemNavigation, this)));
				}
				else
				{
					menuItems.Add(new ToolStripSeparator());
				}
			}

			ToolStripMenuItem parent = _toolStripMenuItemSearch;

			ToolStripItemCollection items = parent.DropDownItems;

			foreach (ToolStripItem menuItem in menuItems)
			{
				items.Add(menuItem);
			}

			parent.Visible = parent.DropDownItems.Count > 0;
		}

		#endregion

		#region IFormStateAware<ApplicationShellState> Members

		void IFormStateAware<ApplicationShellState>.RestoreState(
			ApplicationShellState formState)
		{
			if (formState.NavigationPanelWidth > 0)
			{
				_splitContainerOuter.SplitterDistance = formState.NavigationPanelWidth;
			}

			if (formState.LogWindowHeight > 0)
			{
				_splitContainerInner.SplitterDistance = _splitContainerInner.Height -
				                                        formState.LogWindowHeight;
			}
		}

		void IFormStateAware<ApplicationShellState>.GetState(ApplicationShellState formState)
		{
			formState.NavigationPanelWidth = _splitContainerOuter.SplitterDistance;
			formState.LogWindowHeight = _splitContainerInner.Height -
			                            _splitContainerInner.SplitterDistance;
		}

		#endregion

		#region Non-public members

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			_formStateManager.SaveState();
			_observer.FormClosed();
			base.OnFormClosed(e);
		}

		private void ForceFocusLostEvent()
		{
			Validate();
		}

		private static bool CanCancelClosingForm(CloseReason closeReason)
		{
			switch (closeReason)
			{
				case CloseReason.FormOwnerClosing:
				case CloseReason.MdiFormClosing:
				case CloseReason.UserClosing:
					return true;

				case CloseReason.ApplicationExitCall:
				case CloseReason.TaskManagerClosing:
				case CloseReason.WindowsShutDown:
				case CloseReason.None:
					return false;

				default:
					return false;
			}
		}

		#region Event handlers

		private void ApplicationShell_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_observer != null)
			{
				bool canCancel = CanCancelClosingForm(e.CloseReason);

				e.Cancel = ! _observer.HandleFormClosing(canCancel);
			}
		}

		private void ApplicationShell_KeyDown(object sender, KeyEventArgs e)
		{
			_observer.KeyDownPreview(e);
		}

		private void _toolStripButtonSave_Click(object sender, EventArgs e)
		{
			ForceFocusLostEvent();

			if (_observer != null)
			{
				_observer.TrySavePendingChanges();
			}
		}

		private void _toolStripButtonDiscardChanges_Click(object sender, EventArgs e)
		{
			ForceFocusLostEvent();

			if (_observer != null)
			{
				_observer.DiscardPendingChanges();
			}
		}

		private void _toolStripButtonBack_Click(object sender, EventArgs e)
		{
			ForceFocusLostEvent();

			if (_observer != null)
			{
				_observer.GoBack();
			}
		}

		private void _toolStripButtonForward_Click(object sender, EventArgs e)
		{
			ForceFocusLostEvent();

			if (_observer != null)
			{
				_observer.GoForward();
			}
		}

		private void _toolStripMenuItemOptions_Click(object sender, EventArgs e)
		{
			_observer.ShowOptions();
		}

		private void _toolStripMenuItemAbout_Click(object sender, EventArgs e)
		{
			_observer.ShowAbout();
		}

		private void _toolStripMenuItemExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#endregion
	}
}

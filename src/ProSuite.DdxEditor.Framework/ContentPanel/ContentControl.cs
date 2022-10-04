using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Menus;

namespace ProSuite.DdxEditor.Framework.ContentPanel
{
	public partial class ContentControl : UserControl, IContentView
	{
		private Item _item;
		private IMenuManager _menuManager;
		private IContentObserver _observer;

		#region Constructurs

		/// <summary>
		/// Initializes a new instance of the <see cref="ContentControl"/> class.
		/// </summary>
		public ContentControl()
		{
			InitializeComponent();
		}

		#endregion

		public IMenuManager MenuManager
		{
			get { return _menuManager; }
			set { _menuManager = value; }
		}

		#region IContentView Members

		public IContentObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		#endregion

		/// <summary>
		/// Loads the specified item into the content pane
		/// </summary>
		/// <param name="item"></param>
		/// <param name="itemNavigation"></param>
		/// <remarks>Called within a domain transaction</remarks>
		public void LoadContent([NotNull] Item item,
		                        [NotNull] IItemNavigation itemNavigation)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			RemoveContent();
			SetContent(item, itemNavigation);
		}

		#region Non-public members

		private void SetContent([NotNull] Item item,
		                        [NotNull] IItemNavigation itemNavigation)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			// called within a domain transaction

			Control control = item.CreateControl(itemNavigation);

			if (_menuManager != null)
			{
				var menuManagerAware = control as IMenuManagerAware;
				if (menuManagerAware != null)
				{
					menuManagerAware.MenuManager = _menuManager;
				}
			}

			control.Dock = DockStyle.Fill;

			_item = item;
			_item.Changed += _item_Changed;

			_panelContent.Controls.Add(control);

			UpdateHeader();
		}

		private void _item_Changed(object sender, EventArgs e)
		{
			UpdateHeader();
		}

		private void UpdateHeader()
		{
			if (_item == null)
			{
				_labelHeader.Text = string.Empty;
				_labelHeaderImage.Image = null;
			}
			else
			{
				_labelHeader.Text = _item.Text;
				_labelHeaderImage.Image = _item.Image;
			}
		}

		private void RemoveContent()
		{
			if (_item != null)
			{
				_item.Changed -= _item_Changed;
				_item = null;
			}

			var contentControls = new List<Control>();
			foreach (Control control in _panelContent.Controls)
			{
				contentControls.Add(control);
			}

			_panelContent.Controls.Clear();

			// dispose all controls
			foreach (Control control in contentControls)
			{
				if (! control.IsDisposed)
				{
					control.Dispose();
				}
			}
		}

		#endregion
	}
}

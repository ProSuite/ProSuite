using System;
using System.Windows.Forms;

namespace ProSuite.DdxEditor.Content.Controls
{
	public partial class MoveListItemsControl : UserControl
	{
		public MoveListItemsControl()
		{
			InitializeComponent();
		}

		public event EventHandler MoveUpClicked
		{
			add { _toolStripButtonMoveUp.Click += value; }
			remove { _toolStripButtonMoveUp.Click -= value; }
		}

		public event EventHandler MoveDownClicked
		{
			add { _toolStripButtonMoveDown.Click += value; }
			remove { _toolStripButtonMoveDown.Click -= value; }
		}

		public event EventHandler MoveToTopClicked
		{
			add { _toolStripButtonMoveToTop.Click += value; }
			remove { _toolStripButtonMoveToTop.Click -= value; }
		}

		public event EventHandler MoveToBottomClicked
		{
			add { _toolStripButtonMoveToBottom.Click += value; }
			remove { _toolStripButtonMoveToBottom.Click -= value; }
		}

		public bool MoveUpEnabled
		{
			get { return _toolStripButtonMoveUp.Enabled; }
			set { _toolStripButtonMoveUp.Enabled = value; }
		}

		public bool MoveUpVisible
		{
			get { return _toolStripButtonMoveUp.Visible; }
			set { _toolStripButtonMoveUp.Visible = value; }
		}

		public bool MoveDownEnabled
		{
			get { return _toolStripButtonMoveDown.Enabled; }
			set { _toolStripButtonMoveDown.Enabled = value; }
		}

		public bool MoveDownVisible
		{
			get { return _toolStripButtonMoveDown.Visible; }
			set { _toolStripButtonMoveDown.Visible = value; }
		}

		public bool MoveToTopEnabled
		{
			get { return _toolStripButtonMoveToTop.Enabled; }
			set { _toolStripButtonMoveToTop.Enabled = value; }
		}

		public bool MoveToTopVisible
		{
			get { return _toolStripButtonMoveToTop.Visible; }
			set { _toolStripButtonMoveToTop.Visible = value; }
		}

		public bool MoveToBottomEnabled
		{
			get { return _toolStripButtonMoveToBottom.Enabled; }
			set { _toolStripButtonMoveToBottom.Enabled = value; }
		}

		public bool MoveToBottomVisible
		{
			get { return _toolStripButtonMoveToBottom.Visible; }
			set { _toolStripButtonMoveToBottom.Visible = value; }
		}
	}
}

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.Properties;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class ExpanderControl : UserControl
	{
		private bool _collapsed;
		private int _previousVisibleHeight;
		private bool _canCollapse = true;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExpanderControl"/> class.
		/// </summary>
		public ExpanderControl()
		{
			InitializeComponent();
		}

		#endregion

		public event EventHandler CollapsedChanged;

		public event EventHandler CollapsedChanging;

		public bool CanCollapse
		{
			get { return _canCollapse; }
			set
			{
				if (_canCollapse == value)
				{
					return;
				}

				_canCollapse = value;

				_toolStripButtonTogglePanel.Visible = _canCollapse;
			}
		}

		[DefaultValue(false)]
		public bool Collapsed
		{
			get { return _collapsed; }
			set
			{
				if (! _canCollapse && value)
				{
					throw new InvalidOperationException(
						"Collapsing not supported for this control instance");
				}

				if (_collapsed == value)
				{
					return;
				}

				OnCollapsedChanging(EventArgs.Empty);

				try
				{
					_collapsed = value;

					RenderExpansionState();
				}
				finally
				{
					OnCollapsedChanged(EventArgs.Empty);
				}
			}
		}

		public int ExpandedHeight =>
			_collapsed
				? _previousVisibleHeight
				: Height;

		public int CollapsedHeight => _toolStripTitle.Height;

		public int PanelHeight
		{
			get { return _panelContent.Height; }
			set { Height = _toolStripTitle.Height + value; }
		}

		public string Title
		{
			get { return _toolStripLabelTitle.Text; }
			set { _toolStripLabelTitle.Text = value; }
		}

		public Control Content
		{
			get
			{
				switch (_panelContent.Controls.Count)
				{
					case 0:
						return null;

					case 1:
						return _panelContent.Controls[0];

					default:
						throw CreateInvalidControlCountException();
				}
			}
			set
			{
				if (_panelContent.Controls.Count == 0)
				{
					if (value != null)
					{
						AddContentControl(value);
					}
				}
				else if (_panelContent.Controls.Count == 1)
				{
					_panelContent.Controls.RemoveAt(0);
					if (value != null)
					{
						AddContentControl(value);
					}
				}
				else
				{
					throw CreateInvalidControlCountException();
				}
			}
		}

		protected virtual void OnCollapsedChanged(EventArgs e)
		{
			if (CollapsedChanged != null)
			{
				CollapsedChanged(this, e);
			}
		}

		protected virtual void OnCollapsedChanging(EventArgs e)
		{
			if (CollapsedChanging != null)
			{
				CollapsedChanging(this, e);
			}
		}

		private void RenderExpansionState()
		{
			if (_collapsed)
			{
				_previousVisibleHeight = Height;
			}

			SuspendLayout();
			try
			{
				_panelContent.Visible = ! _collapsed;

				if (! _collapsed)
				{
					Height = _previousVisibleHeight;
					_toolStripButtonTogglePanel.Image = Resources.Collapse;
					_toolStripButtonTogglePanel.Text = "Collapse";
				}
				else
				{
					Height = _toolStripTitle.Height;
					_toolStripButtonTogglePanel.Image = Resources.Expand;
					_toolStripButtonTogglePanel.Text = "Expand";
				}
			}
			finally
			{
				ResumeLayout(true);
			}
		}

		private void AddContentControl(Control value)
		{
			value.Dock = DockStyle.Fill;

			_panelContent.Controls.Add(value);
		}

		private void ToggleCollapse()
		{
			Assert.True(_canCollapse, "Collapsing not supported");
			Assert.False(AutoSize, "AutoSize must be false");

			Collapsed = ! Collapsed;
		}

		private Exception CreateInvalidControlCountException()
		{
			return new InvalidOperationException(
				string.Format("Unsupportet content control count: {0}",
				              _panelContent.Controls.Count));
		}

		private void _toolStripButtonTogglePanel_Click(object sender, EventArgs e)
		{
			ToggleCollapse();
		}

		private void ExpanderControl_Paint(object sender, PaintEventArgs e)
		{
			Rectangle borderRectangle = ClientRectangle;

			ControlPaint.DrawBorder3D(e.Graphics, borderRectangle,
			                          Border3DStyle.Etched);
		}

		private void _toolStripTitle_DoubleClick(object sender, EventArgs e)
		{
			if (_canCollapse && ! AutoSize)
			{
				ToggleCollapse();
			}
		}
	}
}

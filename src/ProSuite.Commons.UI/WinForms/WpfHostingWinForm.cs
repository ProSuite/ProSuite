using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WPF;
using UserControl = System.Windows.Controls.UserControl;

namespace ProSuite.Commons.UI.WinForms
{
	/// <summary>
	/// A WinForm that can host a WPF user control
	/// </summary>
	public partial class WpfHostingWinForm : Form
	{
		private readonly UserControl _wpfControl;
		private ElementHost _wpfControlHost;

		public WpfHostingWinForm([NotNull] UserControl wpfControl)
		{
			_wpfControl = wpfControl;

			InitializeComponent();
		}

		public bool FixedWidth { get; set; }

		public bool FixedHeight { get; set; }

		public void SetMinimumSize(int width, int height)
		{
			MinimumSize = new Size(width, height);
		}

		public void SetMaximumSize(int width, int height)
		{
			MaximumSize = new Size(width, height);
		}

		private void WpfHostingWinForm_Load(object sender, EventArgs e)
		{
			_wpfControlHost = new ElementHost {Dock = DockStyle.Fill, Child = _wpfControl};

			Controls.Add(_wpfControlHost);

			AdjustFormSizeToHostedControl();

			FixDesiredDimension();

			if (_wpfControl is IWinFormHostable winFormHostable)
			{
				winFormHostable.HostFormsWindow = this;
			}
		}

		private void AdjustFormSizeToHostedControl()
		{
			int changeHeightBy =
				(int) Math.Ceiling(_wpfControl.DesiredSize.Height) - ClientSize.Height;
			int changeWidthBy =
				(int) Math.Ceiling(_wpfControl.DesiredSize.Width) - ClientSize.Width;

			Height += changeHeightBy;
			Width += changeWidthBy;
		}

		private void FixDesiredDimension()
		{
			if (! FixedWidth && ! FixedHeight)
			{
				return;
			}

			int minWidth = FixedWidth ? Width : MinimumSize.Width;
			int maxWidth = FixedWidth ? Width : MaximumSize.Width;

			if (maxWidth <= 0)
			{
				maxWidth = int.MaxValue;
			}

			int minHeight = FixedHeight ? Height : MinimumSize.Height;
			int maxHeight = FixedHeight ? Height : MaximumSize.Height;

			if (maxHeight <= 0)
			{
				maxHeight = int.MaxValue;
			}

			MinimumSize = new Size(minWidth, minHeight);
			MaximumSize = new Size(maxWidth, maxHeight);
		}
	}
}

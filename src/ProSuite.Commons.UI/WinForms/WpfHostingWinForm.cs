using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WPF;
using UserControl = System.Windows.Controls.UserControl;

namespace ProSuite.Commons.UI.WinForms
{
	/// <summary>
	/// A WinForm that can host a WPF user control
	/// </summary>
	public partial class WpfHostingWinForm : Form
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly UserControl _wpfControl;
		private ElementHost _wpfControlHost;

		private readonly FormStateManager<FormState> _formStateManager;

		public WpfHostingWinForm([NotNull] UserControl wpfControl)
		{
			_wpfControl = wpfControl;

			InitializeComponent();

			string contextId = _wpfControl.GetType().Name;
			_formStateManager = new FormStateManager<FormState>(this, contextId);

			// Avoid problems with high-resolution displays making the (fixed) height too small:
			_formStateManager.RestoreState(FormStateRestoreOption.OnlyLocation);
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
			try
			{
				_msg.DebugFormat("Loading WPF hosting win form...");

				_wpfControlHost = new ElementHost {Dock = DockStyle.Fill, Child = _wpfControl};

				Controls.Add(_wpfControlHost);

				AdjustFormSizeToHostedControl();

				FixDesiredDimension();

				if (_wpfControl is IWinFormHostable winFormHostable)
				{
					winFormHostable.HostFormsWindow = this;
				}
			}
			catch (Exception ex)
			{
				_msg.Debug("Error loading form", ex);

				ErrorHandler.HandleError(ex, _msg, null, "Error showing server state dialog");
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

		private void WpfHostingWinForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_formStateManager?.SaveState();
		}
	}
}

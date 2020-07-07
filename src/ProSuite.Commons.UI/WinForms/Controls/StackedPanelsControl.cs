using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	// NOTE: not yet operational, needs more work. The interaction with the host form is not yet defined
	public partial class StackedPanelsControl : UserControl
	{
		[NotNull] private readonly List<ExpanderControl> _panels =
			new List<ExpanderControl>();

		private readonly Latch _latch = new Latch();

		#region Constructors

		public StackedPanelsControl()
		{
			InitializeComponent();
		}

		#endregion

		[NotNull]
		public ExpanderControl AddPanel([NotNull] Control control, bool fill)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			var panel = new ExpanderControl
			            {
				            PanelHeight = control.Height,
				            Content = control,
				            Dock = fill
					                   ? DockStyle.Fill
					                   : DockStyle.Bottom
			            };

			_panels.Add(panel);

			SetMinimumSize();

			panel.CollapsedChanged += panel_CollapsedChanged;
			panel.CollapsedChanging += panel_CollapsedChanging;

			return panel;
		}

		#region Non-public members

		private void SetMinimumSize()
		{
			MinimumSize = new Size(MinimumSize.Width,
			                       GetMinimumPanelsHeight(null));
		}

		private void ExpandingPanel([NotNull] ExpanderControl expandingPanel)
		{
			Assert.ArgumentNotNull(expandingPanel, nameof(expandingPanel));

			int heightIncrease = expandingPanel.ExpandedHeight -
			                     expandingPanel.CollapsedHeight;

			int newHeight = Height + heightIncrease;

			MinimumSize = new Size(MinimumSize.Width,
			                       GetMinimumPanelsHeight(expandingPanel));

			MaximumSize = new Size(GetMaximumWidth(),
			                       GetMaximumPanelsHeight(expandingPanel));

			Height = newHeight;
		}

		private void CollapsedPanel([NotNull] ExpanderControl collapsedPanel)
		{
			Assert.ArgumentNotNull(collapsedPanel, nameof(collapsedPanel));

			int heightReduction = collapsedPanel.ExpandedHeight -
			                      collapsedPanel.CollapsedHeight;

			int newHeight = Height - heightReduction;

			MinimumSize = new Size(MinimumSize.Width,
			                       GetMinimumPanelsHeight(null));

			MaximumSize = new Size(GetMaximumWidth(),
			                       GetMaximumPanelsHeight(null));

			Height = newHeight;
		}

		private int GetMaximumWidth()
		{
			return MaximumSize.Width == 0
				       ? 2000
				       : MaximumSize.Width;
		}

		private int GetMaximumPanelsHeight([CanBeNull] ExpanderControl expandingPanel)
		{
			var maxHeight = 0;
			foreach (ExpanderControl panel in _panels)
			{
				if (panel.Collapsed && panel != expandingPanel)
				{
					maxHeight = maxHeight + panel.CollapsedHeight;
				}
				else
				{
					// panel is expanded, or is a about to be expanded
					if (panel.Dock == DockStyle.Fill)
					{
						maxHeight = 2000;
						break;
					}

					maxHeight = maxHeight + panel.ExpandedHeight;
				}
			}

			return maxHeight;
		}

		private int GetMinimumPanelsHeight([CanBeNull] ExpanderControl expandingPanel)
		{
			var minHeight = 0;
			foreach (ExpanderControl panel in _panels)
			{
				if (panel.Collapsed && panel != expandingPanel)
				{
					minHeight = minHeight + panel.CollapsedHeight;
				}
				else
				{
					// panel is expanded, or is about to be expanded
					if (panel.Dock == DockStyle.Fill)
					{
						minHeight = minHeight + panel.CollapsedHeight +
						            panel.Content.MinimumSize.Height;
					}
					else
					{
						minHeight = minHeight + panel.ExpandedHeight;
					}
				}
			}

			return minHeight;
		}

		#region Event handlers

		private void panel_CollapsedChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			var control = (ExpanderControl) sender;

			if (control.Collapsed)
			{
				CollapsedPanel(control);
			}

			ResumeLayout(true);
		}

		private void panel_CollapsedChanging(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			SuspendLayout();

			var control = (ExpanderControl) sender;

			if (control.Collapsed)
			{
				ExpandingPanel(control);
			}
		}

		#endregion

		#endregion
	}
}

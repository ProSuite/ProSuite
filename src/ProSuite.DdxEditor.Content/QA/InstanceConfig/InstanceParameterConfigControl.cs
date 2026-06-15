using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	/// <summary>
	/// Container control for .net 6 blazer parameter view and documentation link.
	/// </summary>
	public partial class InstanceParameterConfigControl : UserControl
	{
		public InstanceParameterConfigControl()
		{
			InitializeComponent();
		}

		public event EventHandler DocumentationLinkClicked;

		public void AddBlazorControl(
			[NotNull] IInstanceConfigurationTableViewControl tableViewControl)
		{
			var qualityConditionTableViewControl = (Control) tableViewControl;

			qualityConditionTableViewControl.SuspendLayout();
			qualityConditionTableViewControl.Dock = DockStyle.Fill;
			qualityConditionTableViewControl.Location = new Point(0, 0);
			qualityConditionTableViewControl.Name = "_qualityConditionTableViewControl";
			qualityConditionTableViewControl.Size = new Size(569, 123);
			qualityConditionTableViewControl.TabIndex = 0;

			_panelParametersEdit.Controls.Add(qualityConditionTableViewControl);
		}

		private void _linkDocumentation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			DocumentationLinkClicked?.Invoke(this, e);
		}
	}
}

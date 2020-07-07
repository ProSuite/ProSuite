using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	[ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
	internal class ToolStripCheckBox : ToolStripControlHost
	{
		public ToolStripCheckBox() : base(new CheckBox())
		{
			Control.BackColor = Color.Transparent;
		}

		[NotNull]
		public CheckBox CheckBoxControl => Assert.NotNull((CheckBox) Control, "control is null");

		public bool Checked
		{
			get { return CheckBoxControl.Checked; }
			set { CheckBoxControl.Checked = value; }
		}

		public event EventHandler CheckedChanged;

		public void OnCheckedChanged(object sender, EventArgs e)
		{
			if (CheckedChanged != null)
			{
				CheckedChanged(this, e);
			}
		}

		protected override void OnSubscribeControlEvents(Control control)
		{
			base.OnSubscribeControlEvents(control);

			((CheckBox) control).CheckedChanged += OnCheckedChanged;
		}

		protected override void OnUnsubscribeControlEvents(Control control)
		{
			base.OnUnsubscribeControlEvents(control);

			((CheckBox) control).CheckedChanged -= OnCheckedChanged;
		}
	}
}

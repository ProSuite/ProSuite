using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class CheckboxElement : BoundScreenElement<CheckBox, bool>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckboxElement"/> class.
		/// </summary>
		/// <param name="accessor">The accessor.</param>
		/// <param name="control">The control.</param>
		public CheckboxElement([NotNull] IPropertyAccessor accessor,
		                       [NotNull] CheckBox control)
			: base(accessor, control)
		{
			control.CheckedChanged += control_CheckedChanged;
			Alias = control.Text;
		}

		private void control_CheckedChanged(object sender, EventArgs e)
		{
			ElementValueChanged();
		}

		protected override bool GetValueFromControl()
		{
			return BoundControl.Checked;
		}

		protected override void ResetControl(bool originalValue)
		{
			BoundControl.Checked = originalValue;
		}

		protected override void TearDown()
		{
			BoundControl.CheckedChanged -= control_CheckedChanged;
		}
	}
}

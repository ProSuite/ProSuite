using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class TextboxElement : TextEditingElement<TextBox>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TextboxElement"/> class.
		/// </summary>
		/// <param name="accessor">The accessor.</param>
		/// <param name="control">The control.</param>
		public TextboxElement([NotNull] IPropertyAccessor accessor,
		                      [NotNull] TextBox control)
			: base(accessor, control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			control.GotFocus += control_GotFocus;
		}

		private void control_GotFocus(object sender, EventArgs e)
		{
			BoundControl.SelectAll();
		}

		protected override void SetMaximumLength(int length)
		{
			BoundControl.MaxLength = length;
		}

		protected override void SetRightAligned(TextBox control)
		{
			control.TextAlign = HorizontalAlignment.Right;
		}

		protected override void TearDown()
		{
			base.TearDown();

			BoundControl.GotFocus -= control_GotFocus;
		}

		protected override void PostFocus()
		{
			BoundControl.SelectAll();
		}

		protected override void DisableCore()
		{
			BoundControl.ReadOnly = true;
		}

		protected override void EnableCore()
		{
			BoundControl.ReadOnly = false;
		}
	}
}

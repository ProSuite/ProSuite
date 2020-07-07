using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class LabelElement : TextEditingElement<Label>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LabelElement"/> class.
		/// </summary>
		/// <param name="accessor">The accessor.</param>
		/// <param name="control">The control.</param>
		public LabelElement([NotNull] IPropertyAccessor accessor, [NotNull] Label control)
			: base(accessor, control) { }

		protected override void SetRightAligned(Label control)
		{
			control.TextAlign = ContentAlignment.MiddleRight;
		}

		protected override void SetMaximumLength(int length) { }
	}
}

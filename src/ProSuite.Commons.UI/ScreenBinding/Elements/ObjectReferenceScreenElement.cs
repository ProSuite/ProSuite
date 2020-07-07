using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class ObjectReferenceScreenElement :
		BoundScreenElement<ObjectReferenceControl, object>
	{
		[NotNull] private readonly ObjectReferenceControl _control;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectReferenceScreenElement"/> class.
		/// </summary>
		/// <param name="accessor">The accessor.</param>
		/// <param name="control">The control.</param>
		public ObjectReferenceScreenElement([NotNull] IPropertyAccessor accessor,
		                                    [NotNull] ObjectReferenceControl control)
			: base(accessor, control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			_control = control;
			_control.Changed += _control_Changed;
		}

		private void _control_Changed(object sender, EventArgs e)
		{
			ElementValueChanged();
		}

		protected override object GetValueFromControl()
		{
			return _control.DataSource;
		}

		protected override void ResetControl(object originalValue)
		{
			_control.DataSource = originalValue;
		}

		protected override void TearDown()
		{
			_control.Changed -= _control_Changed;
		}
	}
}

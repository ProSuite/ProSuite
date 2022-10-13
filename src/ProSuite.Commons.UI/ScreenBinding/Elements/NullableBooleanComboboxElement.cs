using System;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class NullableBooleanComboboxElement :
		BoundScreenElement<NullableBooleanCombobox, object>
	{
		private CoerceFunction<bool?> _coercion;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanComboboxElement"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="control">The control.</param>
		public NullableBooleanComboboxElement([NotNull] IPropertyAccessor property,
		                                      [NotNull] NullableBooleanCombobox control)
			: base(property, control)
		{
			WireEvents(control);

			// default coercion
			_coercion = (accessor, rawValue) => CoercionUtility.Coerce(accessor, rawValue);
		}

		#endregion

		public CoerceFunction<bool?> Coercion
		{
			get { return _coercion; }
			set { _coercion = value; }
		}

		protected override void TearDown()
		{
			UnwireEvents();
		}

		protected override object GetValueFromControl()
		{
			object convertedValue = ConvertValue();
			return convertedValue;
		}

		protected override void ResetControl(object originalValue)
		{
			BoundControl.Value = originalValue == null
				                     ? (bool?) null
				                     : Convert.ToBoolean(originalValue);
		}

		protected void EditingComplete(object sender, EventArgs e)
		{
			var eventArgs = new CancelEventArgs(false);
			RaiseChanged(eventArgs);
		}

		protected void RaiseChanged(CancelEventArgs e)
		{
			try
			{
				ElementValueChanged();
			}
			catch (Exception ex)
			{
				e.Cancel = true;
				_msg.Warn(ex.Message, ex);
			}
		}

		protected object ConvertValue()
		{
			return _coercion(Accessor, BoundControl.Value);
		}

		protected void WireEvents(NullableBooleanCombobox control)
		{
			control.Validated += EditingComplete;
			control.ValueChanged += EditingComplete;
		}

		private void UnwireEvents()
		{
			BoundControl.Validated -= EditingComplete;
			BoundControl.ValueChanged -= EditingComplete;
		}
	}
}

using System;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class NumericUpDownNullableElement :
		BoundScreenElement<NumericUpDownNullable, object>
	{
		private CoerceFunction<decimal?> _coercion;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NumericUpDownNullableElement"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="control">The control.</param>
		public NumericUpDownNullableElement(IPropertyAccessor property,
		                                    NumericUpDownNullable control)
			: base(property, control)
		{
			WireEvents(control);

			object maxValue =
				LessThanOrEqualAttribute.GetMaximumValue(property.InnerProperty);
			if (maxValue != null)
			{
				// TODO control.Maximum = (decimal) maxValue;
			}

			_coercion = delegate(IPropertyAccessor accessor, decimal? rawValue)
			{
				// default coercion
				return CoercionUtility.Coerce(accessor, rawValue);
			};
		}

		#endregion

		public CoerceFunction<decimal?> Coercion
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
			if (originalValue == null)
			{
				BoundControl.Value = null;
			}
			else
			{
				BoundControl.Value = Convert.ToDecimal(originalValue);
			}
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
				// TODO -- data does not parse
				//SetError(Notification.INVALID_FORMAT);
			}
		}

		private void SelectAll()
		{
			int length = BoundControl.Text?.Length ?? 0;

			BoundControl.Select(0, length);
		}

		protected object ConvertValue()
		{
			return _coercion(Accessor, BoundControl.Value);
		}

		protected void WireEvents(NumericUpDownNullable control)
		{
			control.Validated += EditingComplete;
			control.ValueChanged += EditingComplete;

			control.KeyDown += control_KeyDown;
			control.GotFocus += control_GotFocus;
		}

		private void UnwireEvents()
		{
			BoundControl.Validated -= EditingComplete;
			BoundControl.ValueChanged -= EditingComplete;

			BoundControl.KeyDown -= control_KeyDown;
			BoundControl.GotFocus -= control_GotFocus;
		}

		private void control_GotFocus(object sender, EventArgs e)
		{
			SelectAll();
		}

		private void control_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				var eventArgs = new CancelEventArgs(false);
				RaiseChanged(eventArgs);
			}
		}
	}
}

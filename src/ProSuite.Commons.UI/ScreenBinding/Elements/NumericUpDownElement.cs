using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class NumericUpDownElement : BoundScreenElement<NumericUpDown, object>
	{
		private CoerceFunction<decimal> _coercion;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TextEditingElement&lt;CONTROLTYPE&gt;"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="control">The control.</param>
		public NumericUpDownElement([NotNull] IPropertyAccessor property,
		                            [NotNull] NumericUpDown control)
			: base(property, control)
		{
			WireEvents(control);

			object maxValue = LessThanOrEqualAttribute.GetMaximumValue(property.InnerProperty);
			if (maxValue != null)
			{
				// TODO control.Maximum = (decimal) maxValue;
			}

			_coercion = delegate(IPropertyAccessor accessor, decimal rawValue)
			            {
				            // default coercion
				            return CoercionUtility.Coerce(accessor, rawValue);
			            };
		}

		#endregion

		public CoerceFunction<decimal> Coercion
		{
			get { return _coercion; }
			set { _coercion = value; }
		}

		#region Non-public methods

		protected override void TearDown()
		{
			UnwireEvents();
		}

		protected override object GetValueFromControl()
		{
			//// TODO: must be converted to property type
			//return BoundControl.Value;
			////if (string.IsNullOrEmpty(GetText()))
			////{
			////    return null;
			////}

			object convertedValue = ConvertValue();
			return convertedValue;
		}

		protected override void ResetControl(object originalValue)
		{
			BoundControl.Value = Convert.ToDecimal(originalValue);
			//string text = originalValue == null ? string.Empty : _format(originalValue);
			//SetText(text);
		}

		protected void EditingComplete(object sender, EventArgs e)
		{
			RaiseChanged(new CancelEventArgs(false));
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

				ShowErrorMessages(ex.Message);

				_msg.Error(ex.Message, ex);
			}
		}

		protected object ConvertValue()
		{
			return _coercion(Accessor, BoundControl.Value);
		}

		protected void WireEvents(NumericUpDown control)
		{
			// Changed this event from validating to LostFocus because the validating
			// event prevents the GUI from transitioning from View Mode to Edit Mode 6/24/07
			control.Validated += EditingComplete;
			control.ValueChanged += EditingComplete;

			control.KeyDown += control_KeyDown;
			control.GotFocus += control_GotFocus;
		}

		private void SelectAll()
		{
			int length = BoundControl.Text == null
				             ? 0
				             : BoundControl.Text.Length;

			BoundControl.Select(0, length);
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
				RaiseChanged(new CancelEventArgs(false));
			}
		}

		#endregion
	}
}

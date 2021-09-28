using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public abstract class TextEditingElement<CONTROLTYPE> :
		BoundScreenElement<CONTROLTYPE, object>, ITextEditingElement
		where CONTROLTYPE : Control
	{
		private CoerceFunction<string> _coercion;
		private FormatValue _format;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TextEditingElement&lt;CONTROLTYPE&gt;"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="control">The control.</param>
		protected TextEditingElement([NotNull] IPropertyAccessor property,
		                             [NotNull] CONTROLTYPE control)
			: base(property, control)
		{
			SetupEvents(control);

			if (CoercionUtility.IsNumeric(property.PropertyType))
			{
				SetRightAligned(control);
			}

			if (property.PropertyType == typeof(string))
			{
				int? length = MaximumStringLengthAttribute.GetLength(
					property.InnerProperty);

				if (length.HasValue)
				{
					SetMaximumLength(length.Value);
				}
			}

			_coercion = delegate(IPropertyAccessor accessor, string rawValue)
			            {
				            // default coercion
				            return CoercionUtility.Coerce(accessor, rawValue);
			            };

			_format = o => o.ToString();
		}

		#endregion

		#region ITextEditingElement Members

		public CoerceFunction<string> Coercion
		{
			get { return _coercion; }
			set { _coercion = value; }
		}

		public FormatValue Format
		{
			get { return _format; }
			set { _format = value; }
		}

		#endregion

		protected void SetupEvents(CONTROLTYPE control)
		{
			// Changed this event from validating to LostFocus because the validating
			// event prevents the GUI from transitioning from View Mode to Edit Mode 6/24/07
			control.Validated += EditingComplete;
			control.KeyDown += control_KeyDown;
			control.TextChanged += control_TextChanged;
		}

		protected abstract void SetMaximumLength(int length);

		protected override void TearDown()
		{
			BoundControl.Validated -= EditingComplete;
			BoundControl.KeyDown -= control_KeyDown;
			BoundControl.TextChanged -= control_TextChanged;
		}

		protected abstract void SetRightAligned(CONTROLTYPE control);

		private void control_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				var args = new CancelEventArgs(false);
				RaiseChanged(args);
			}
		}

		private void control_TextChanged(object sender, EventArgs e)
		{
			RaiseChanged(new CancelEventArgs(false));
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

		protected override object GetValueFromControl()
		{
			return string.IsNullOrEmpty(GetText())
				       ? null
				       : ConvertValue();
		}

		protected object ConvertValue()
		{
			return _coercion(Accessor, GetText());
		}

		protected override void ResetControl(object originalValue)
		{
			string text = originalValue == null
				              ? string.Empty
				              : _format(originalValue);

			SetText(text);
		}

		protected virtual string GetText()
		{
			return BoundControl.Text;
		}

		protected virtual void SetText(string text)
		{
			BoundControl.Text = text;
		}
	}
}

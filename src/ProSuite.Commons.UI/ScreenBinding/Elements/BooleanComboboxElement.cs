using System;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class BooleanComboboxElement :
		BoundScreenElement<BooleanCombobox, object>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private CoerceFunction<bool> _coercion;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanComboboxElement"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="control">The control.</param>
		public BooleanComboboxElement([NotNull] IPropertyAccessor property,
		                              [NotNull] BooleanCombobox control)
			: base(property, control)
		{
			WireEvents(control);

			_coercion = delegate(IPropertyAccessor accessor, bool rawValue)
			{
				// default coercion
				return CoercionUtility.Coerce(accessor, rawValue);
			};
		}

		#endregion

		public CoerceFunction<bool> Coercion
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
			BoundControl.Value = originalValue != null && Convert.ToBoolean(originalValue);
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

		protected void WireEvents(BooleanCombobox control)
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

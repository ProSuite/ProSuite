using System;
using System.ComponentModel;
using ProSuite.Commons.Keyboard;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class KeyboardShortcutTextboxElement :
		BoundScreenElement<KeyboardShortcutTextbox, object>
	{
		private CoerceFunction<KeyboardShortcut> _coercion;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TextEditingElement&lt;CONTROLTYPE&gt;"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="control">The control.</param>
		public KeyboardShortcutTextboxElement(IPropertyAccessor property,
		                                      KeyboardShortcutTextbox control)
			: base(property, control)
		{
			WireEvents(control);

			// default coercion
			_coercion = (accessor, rawValue) => rawValue;
		}

		#endregion

		public CoerceFunction<KeyboardShortcut> Coercion
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
			return ConvertValue();
		}

		protected override void ResetControl(object originalValue)
		{
			BoundControl.Shortcut = originalValue as KeyboardShortcut;
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
				_msg.Warn(ex.Message, ex);
			}
		}

		protected object ConvertValue()
		{
			return _coercion(Accessor, BoundControl.Shortcut);
		}

		protected void WireEvents(KeyboardShortcutTextbox control)
		{
			control.Validated += EditingComplete;
			control.ShortcutChanged += EditingComplete;
		}

		private void UnwireEvents()
		{
			BoundControl.Validated -= EditingComplete;
			BoundControl.ShortcutChanged -= EditingComplete;
		}
	}
}

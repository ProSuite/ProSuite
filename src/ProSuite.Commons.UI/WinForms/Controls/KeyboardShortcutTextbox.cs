using System;
using System.Windows.Forms;
using ProSuite.Commons.Keyboard;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Text box for editing a keyboard shortcut. The shortcut can be entered by
	/// pressing the corresponding key combination.
	/// </summary>
	public class KeyboardShortcutTextbox : TextBox
	{
		private KeyboardShortcut _shortcut;

		public KeyboardShortcut Shortcut
		{
			get { return _shortcut; }
			set
			{
				if (Equals(_shortcut, value))
				{
					return;
				}

				_shortcut = value;

				RenderShortcut();

				OnShortcutChanged(EventArgs.Empty);
			}
		}

		public event EventHandler ShortcutChanged;

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			e.SuppressKeyPress = true;

			if (e.KeyData == Keys.Delete || e.KeyData == Keys.Back)
			{
				Shortcut = null;
			}
			else if (KeyboardUtils.IsValidShortcutKey(e.KeyCode))
			{
				Shortcut = KeyboardUtils.CreateShortcut(e);
			}
		}

		private void RenderShortcut()
		{
			base.Text = _shortcut != null
				            ? KeyboardUtils.GetDisplayText(_shortcut)
				            : string.Empty;
		}

		protected virtual void OnShortcutChanged(EventArgs e)
		{
			if (ShortcutChanged != null)
			{
				ShortcutChanged(this, e);
			}
		}
	}
}

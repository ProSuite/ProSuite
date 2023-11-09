using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Keyboard;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace ProSuite.Commons.UI.Keyboard
{
	public static class KeyboardUtils
	{
		public static KeyboardShortcut CreateShortcut(Keys key,
		                                              bool control = false,
		                                              bool shift = false,
		                                              bool alt = false)
		{
			return new KeyboardShortcut((int) key, control, shift, alt);
		}

		public static KeyboardShortcut CreateShortcut([NotNull] KeyEventArgs e)
		{
			Assert.ArgumentNotNull(e, nameof(e));

			return CreateShortcut(e.KeyCode,
			                      IsModifierPressed(e, Keys.Control),
			                      IsModifierPressed(e, Keys.Shift),
			                      IsModifierPressed(e, Keys.Alt));
		}

		public static bool IsModifierPressed([NotNull] KeyEventArgs e,
		                                     Keys modifier,
		                                     bool exclusive = false)
		{
			Assert.ArgumentNotNull(e, nameof(e));

			return exclusive
				       ? e.Modifiers == modifier
				       : (e.Modifiers & modifier) != 0;
		}

		public static bool IsModifierPressed(Keys modifier,
		                                     bool exclusive = false)
		{
			if (exclusive)
			{
				return Control.ModifierKeys == modifier;
			}

			return (Control.ModifierKeys & modifier) != 0;
		}

		public static bool IsModifierKey(Key key)
		{
			return key is
				       Key.LeftShift or
				       Key.RightShift or
				       Key.LeftCtrl or
				       Key.RightCtrl or
				       Key.LeftAlt or
				       Key.RightAlt;
		}

		/// <summary>
		/// Determines whether all of the specified modifiers are pressed or not.
		/// </summary>
		/// <param name="modifiers"></param>
		/// <returns></returns>
		public static bool AreModifiersPressed(params Keys[] modifiers)
		{
			var result = true;

			foreach (Keys key in modifiers)
			{
				if (! IsModifierPressed(key))
				{
					result = false;
				}
			}

			return result;
		}

		[NotNull]
		public static string GetDisplayText([NotNull] KeyboardShortcut keyboardShortcut)
		{
			Assert.ArgumentNotNull(keyboardShortcut, nameof(keyboardShortcut));

			// TODO interpret as char value, no key value
			var key = (Keys) keyboardShortcut.Key;

			var sb = new StringBuilder();

			AppendModifiers(keyboardShortcut, sb);

			sb.Append(GetDisplayText(key));

			return sb.ToString();
		}

		[NotNull]
		public static string GetDisplayText(Keys key)
		{
			string keyString = string.Format(CultureInfo.InvariantCulture, "{0}", key);

			if (key >= Keys.D0 && key <= Keys.D9)
			{
				return keyString.Replace("D", string.Empty);
			}

			// TODO
			return keyString;
		}

		public static bool IsValidShortcutKey(Keys key)
		{
			char keyChar = Convert.ToChar((int) key);

			return char.IsLetterOrDigit(keyChar) || IsFunctionKey(key);
		}

		public static bool IsFunctionKey(Keys key)
		{
			switch (key)
			{
				case Keys.F1:
				case Keys.F2:
				case Keys.F3:
				case Keys.F4:
				case Keys.F5:
				case Keys.F6:
				case Keys.F7:
				case Keys.F8:
				case Keys.F9:
				case Keys.F10:
				case Keys.F11:
				case Keys.F12:
				case Keys.F13:
				case Keys.F14:
				case Keys.F15:
				case Keys.F16:
				case Keys.F17:
				case Keys.F18:
				case Keys.F19:
				case Keys.F20:
				case Keys.F21:
				case Keys.F22:
				case Keys.F23:
				case Keys.F24:
					return true;

				default:
					return false;
			}
		}

		private static void AppendModifiers([NotNull] KeyboardShortcut keyboardShortcut,
		                                    [NotNull] StringBuilder sb)
		{
			const string modifierFormat = "{0}+";

			if (keyboardShortcut.Control)
			{
				sb.AppendFormat(modifierFormat, "CTRL");
			}

			if (keyboardShortcut.Shift)
			{
				sb.AppendFormat(modifierFormat, "SHIFT");
			}

			if (keyboardShortcut.Alt)
			{
				sb.AppendFormat(modifierFormat, "ALT");
			}
		}
	}
}

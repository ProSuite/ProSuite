using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using static System.Windows.Input.Keyboard;

namespace ProSuite.Commons.UI.Input
{
	/// <summary>
	/// Utils for WPF keyboard input. See ProSuite.Commons.UI.Keyboard.
	/// <see cref="ProSuite.Commons.UI.Keyboard.KeyboardUtils" />
	/// for Windows Forms input.
	/// </summary>
	public static class KeyboardUtils
	{
		private static readonly List<Key> _modifierKeys =
			new List<Key>(8)
			{
				Key.LeftAlt, Key.RightAlt,
				Key.LeftShift, Key.RightShift,
				Key.LeftCtrl, Key.RightCtrl,
				Key.LWin, Key.RWin
			};

		/// <summary>
		/// Check whether the specified key is a modifier key,
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static bool IsModifierKey(Key key)
		{
			return _modifierKeys.Contains(key);
		}

		/// <summary>
		/// Check whether modifier key is down
		/// </summary>
		/// <param name="key">The modifier key</param>
		/// <param name="exclusive">
		/// Only one modifier key exclusively down.
		/// If true: Ctrl + Alt is not allowed, Ctrl + S is allowed.
		/// </param>
		/// <returns></returns>
		public static bool IsModifierDown(Key key,
		                                  bool exclusive = false)
		{
			if (! _modifierKeys.Contains(key))
			{
				return false;
			}

			switch (key)
			{
				case Key.LeftAlt:
				case Key.RightAlt:
					if (exclusive)
					{
						// only one modifier at once
						return Modifiers == ModifierKeys.Alt;
					}

					// several modifiers in combination possible
					return (Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

				case Key.LeftShift:
				case Key.RightShift:
					if (exclusive)
					{
						return Modifiers == ModifierKeys.Shift;
					}

					return (Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

				case Key.LeftCtrl:
				case Key.RightCtrl:
					if (exclusive)
					{
						return Modifiers == ModifierKeys.Control;
					}

					return (Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

				case Key.LWin:
				case Key.RWin:
					if (exclusive)
					{
						return Modifiers == ModifierKeys.Windows;
					}

					return (Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows;
				default:
					return false;
			}
		}

		public static bool IsAnyModifierDown()
		{
			return Modifiers != ModifierKeys.None;
		}

		public static bool IsKeyDown(Key key)
		{
			return System.Windows.Input.Keyboard.IsKeyDown(key);
		}

		public static bool IsAnyKeyDown(params Key[] keys)
		{
			return keys.Any(IsKeyDown);
		}

		public static bool IsCtrlDown()
		{
			return IsKeyDown(Key.LeftCtrl) ||
			       IsKeyDown(Key.RightCtrl);
		}

		public static bool IsShiftDown()
		{
			return IsKeyDown(Key.LeftShift) ||
			       IsKeyDown(Key.RightShift);
		}

		public static bool IsAltDown()
		{
			return IsKeyDown(Key.LeftAlt) ||
			       IsKeyDown(Key.RightAlt);
		}

		public static bool IsKeyUp(Key key)
		{
			return System.Windows.Input.Keyboard.IsKeyUp(key);
		}

		public static bool IsShiftKey(Key key)
		{
			return key == Key.LeftShift || key == Key.RightShift;
		}
	}
}

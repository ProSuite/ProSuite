using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace ProSuite.Commons.UI.WPF
{
	public static class Extensions
	{
		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

		#region Window minimize & maximize buttons

		// Inspired by:
		// https://stackoverflow.com/questions/339620/how-to-remove-minimize-and-maximize-buttons-from-a-resizable-window

		// constants from winuser.h
		private const int GWL_STYLE = -16;
		private const int WS_MAXIMIZEBOX = 0x10000;
		private const int WS_MINIMIZEBOX = 0x20000;

		public static void ShowMinimizeButton(this Window window, bool show)
		{
			var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
			var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
			var newStyle = show ? currentStyle | WS_MINIMIZEBOX : currentStyle & ~WS_MINIMIZEBOX;
			SetWindowLong(hwnd, GWL_STYLE, newStyle); // ignore HRESULT return value
		}

		public static void ShowMaximizeButton(this Window window, bool show)
		{
			var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
			var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
			var newStyle = show ? currentStyle | WS_MAXIMIZEBOX : currentStyle & ~WS_MAXIMIZEBOX;
			SetWindowLong(hwnd, GWL_STYLE, newStyle); // ignore HRESULT return value
		}

		#endregion
	}
}

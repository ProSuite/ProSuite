using System;
using System.Windows.Interop;
using Application = System.Windows.Application;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace ProSuite.Commons.AGP.Windows
{
	public static class WindowsUtils
	{
		private class Win32Window : IWin32Window
		{
			public IntPtr Handle { get; }

			public Win32Window(IntPtr handle)
			{
				Handle = handle;
			}
		}

		public static IWin32Window GetWin32MainWindow()
		{
			var mainWnd = Application.Current.MainWindow;
			if (mainWnd == null)
			{
				return null;
			}

			IntPtr hwnd = new WindowInteropHelper(mainWnd).Handle;

			return new Win32Window(hwnd);
		}
	}
}

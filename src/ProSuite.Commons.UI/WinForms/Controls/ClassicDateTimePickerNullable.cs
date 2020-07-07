using System;
using System.Runtime.InteropServices;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class ClassicDateTimePickerNullable : DateTimePickerNullable
	{
		[DllImport("uxtheme.dll")]
		private static extern int SetWindowTheme(IntPtr hWnd, string appname, string idlist);

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			SetWindowTheme(Handle, string.Empty, string.Empty);
		}
	}
}

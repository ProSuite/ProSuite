using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class ClassicTextBox : TextBox
	{
		[DllImport("uxtheme.dll")]
		private static extern int SetWindowTheme(IntPtr hWnd, string appname, string idlist);

		protected override void OnHandleCreated(EventArgs e)
		{
			SetWindowTheme(Handle, string.Empty, string.Empty);
			base.OnHandleCreated(e);
		}
	}
}

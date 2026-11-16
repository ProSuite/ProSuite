using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms;

namespace ProSuite.Commons.UI.Dialogs
{
	public class DialogService : DialogServiceBase
	{
		/// <summary>
		/// Whether the list dialogs are re-colored to match ArcGIS Pro's dark theme. Set it at
		/// startup when the dark theme is active; Pro changes its theme only on a restart.
		/// </summary>
		public static bool UseDarkTheme { get; set; }

		protected override DialogResult ShowListBox(IWin32Window owner,
		                                            string title,
		                                            string headerMessage,
		                                            IList<string> listLines,
		                                            string footerMessage,
		                                            MessageBoxButtons buttons,
		                                            MessageBoxIcon icon,
		                                            bool middleButtonIsDefault = false)
		{
			using (var form = new MessageListForm())
			{
				if (UseDarkTheme)
				{
					WinFormsThemeUtils.ApplyDarkTheme(form);
				}

				return form.ShowDialog(owner, title, headerMessage, listLines,
				                       footerMessage, icon, buttons, middleButtonIsDefault);
			}
		}

		protected override DialogResult ShowMessageBox(
			IWin32Window owner,
			string title,
			string message,
			MessageBoxButtons buttons,
			MessageBoxIcon icon,
			MessageBoxDefaultButton defaultButton)
		{
			if (owner == null)
			{
				return MessageBox.Show(message, title, buttons, icon,
				                       defaultButton,
				                       MessageBoxOptions.DefaultDesktopOnly);
			}

			return MessageBox.Show(owner, message, title,
			                       buttons, icon,
			                       defaultButton);
		}
	}
}

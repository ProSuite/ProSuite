using System.Collections.Generic;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.Dialogs
{
	public class DialogService : DialogServiceBase
	{
		protected override DialogResult ShowListBox(IWin32Window owner,
		                                            string title,
		                                            string headerMessage,
		                                            IList<string> listLines,
		                                            string footerMessage,
		                                            MessageBoxButtons buttons,
		                                            MessageBoxIcon icon)
		{
			using (var form = new MessageListForm())
			{
				return form.ShowDialog(owner, title, headerMessage, listLines,
				                       footerMessage, icon, buttons);
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

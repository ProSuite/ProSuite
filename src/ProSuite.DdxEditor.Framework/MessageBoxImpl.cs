using System.Windows.Forms;

namespace ProSuite.DdxEditor.Framework
{
	public class MessageBoxImpl : IMessageBox
	{
		#region IMessageBox Members

		public DialogResult Show(IWin32Window owner, string text, string caption,
		                         MessageBoxButtons buttons, MessageBoxIcon icon,
		                         MessageBoxDefaultButton defaultButton)
		{
			return MessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
		}

		#endregion
	}
}

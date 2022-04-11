using System.Windows.Forms;

namespace ProSuite.DdxEditor.Framework
{
	public interface IMessageBox
	{
		DialogResult Show(IWin32Window owner, string text, string caption,
		                  MessageBoxButtons buttons, MessageBoxIcon icon,
		                  MessageBoxDefaultButton defaultButton);
	}
}

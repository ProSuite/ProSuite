using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Env
{
	public interface IModalDialog
	{
		DialogResult ShowDialog();

		DialogResult ShowDialog([CanBeNull] IWin32Window owner);
	}
}

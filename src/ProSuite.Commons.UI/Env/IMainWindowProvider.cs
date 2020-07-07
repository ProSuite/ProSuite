using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Env
{
	public interface IMainWindowProvider
	{
		[CanBeNull]
		IWin32Window GetMainWindow();
	}
}

using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Help
{
	[PublicAPI]
	public class NopHelpProvider : IHelpProvider
	{
		public string Name => string.Empty;

		public void Refresh() { }

		public bool CanShowHelp => false;

		public void ShowHelp(IWin32Window owner) { }
	}
}

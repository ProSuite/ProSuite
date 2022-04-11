using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Framework
{
	[UsedImplicitly]
	public class ApplicationShellState : FormState
	{
		public int NavigationPanelWidth { get; set; }

		public int LogWindowHeight { get; set; }
	}
}

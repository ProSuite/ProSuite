using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Properties;

namespace ProSuite.AGP.Editing.YReshape
{
	public abstract class YReshapeToolBase : AdvancedReshapeToolBase
	{
		protected override string OptionsFileName => "YReshapeToolOptions.xml";

		protected override SelectionCursors GetSelectionCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.YReshapeOverlay);
		}
	}
}

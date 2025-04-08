using System.Windows.Input;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Properties;

namespace ProSuite.AGP.Editing.YReshape
{
	public abstract class YReshapeToolBase : AdvancedReshapeToolBase
	{
		protected override string OptionsFileName => "YReshapeToolOptions.xml";

		protected override Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay, null);
		}

		protected override Cursor GetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Lasso);
		}

		protected override Cursor GetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Lasso,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Polygon);
		}

		protected override Cursor GetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.YReshapeOverlay,
			                              Resources.Polygon,
			                              Resources.Shift);
		}
	}
}

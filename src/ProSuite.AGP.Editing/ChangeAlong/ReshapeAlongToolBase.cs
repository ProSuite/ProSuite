using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ReshapeAlongToolBase : ChangeGeometryAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ReshapeAlongToolBase()
		{
			SelectionCursor = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorShift);

			TargetSelectionCursor = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorProcess);
			TargetSelectionCursorShift =
				ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorProcessShift);
		}

		protected override string EditOperationDescription => "Reshape along";

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon;
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info(LocalizableStrings.ReshapeAlongTool_LogUsingCurrentSelection);
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.ReshapeAlongTool_LogPromptForSelection);
		}
	}
}

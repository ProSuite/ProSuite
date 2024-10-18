using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SelectionSketchTypeToggle
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISketchTool _tool;
	private readonly SketchGeometryType? _defaultSelectionSketchType;

	private SketchGeometryType? _currentSelectionSketchType;

	public SelectionSketchTypeToggle([NotNull] ISketchTool tool,
	                                 SketchGeometryType defaultSelectionSketchType)
	{
		_tool = tool;
		_defaultSelectionSketchType = defaultSelectionSketchType;

		SetSketchType(tool, defaultSelectionSketchType);
	}

	public void Toggle(SketchGeometryType? sketchType)
	{
		SketchGeometryType? type;

		switch (sketchType)
		{
			case SketchGeometryType.Polygon:
				type = _currentSelectionSketchType == SketchGeometryType.Polygon
					       ? _defaultSelectionSketchType
					       : sketchType;
				break;
			case SketchGeometryType.Lasso:
				type = _currentSelectionSketchType == SketchGeometryType.Lasso
					       ? _defaultSelectionSketchType
					       : sketchType;
				break;
			default:
				return;
		}

		SetSketchType(_tool, type);
	}

	private void SetSketchType(ISketchTool tool, SketchGeometryType? type)
	{
		tool.SetSketchType(type);
		_currentSelectionSketchType = type;

		_msg.Info($"{_tool.Caption}: {type} selection sketch");
	}
}

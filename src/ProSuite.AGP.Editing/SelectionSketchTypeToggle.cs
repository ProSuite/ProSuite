using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SelectionSketchTypeToggle
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISketchTool _tool;
	private readonly SketchGeometryType? _defaultSelectionSketchType;

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
				type = _tool.GetSketchType() == SketchGeometryType.Polygon
					       ? _defaultSelectionSketchType
					       : sketchType;
				break;
			case SketchGeometryType.Lasso:
				type = _tool.GetSketchType() == SketchGeometryType.Lasso
					       ? _defaultSelectionSketchType
					       : sketchType;
				break;
			default:
				type = sketchType;
				break;
		}

		TrySetSketchType(_tool, type);
	}

	private void TrySetSketchType(ISketchTool tool, SketchGeometryType? type)
	{
		if (_tool.GetSketchType() == type)
		{
			return;
		}

		SetSketchType(tool, type);
	}

	private void SetSketchType(ISketchTool tool, SketchGeometryType? type)
	{
		tool.SetSketchType(type);

		_msg.Debug($"{type} selection sketch");
	}
}

using System;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SelectionSketchTypeToggle
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISketchTool _tool;
	private readonly SketchGeometryType? _defaultSelectionSketchType;
	private SketchGeometryType? _previousType;

	public SelectionSketchTypeToggle([NotNull] ISketchTool tool,
	                                 SketchGeometryType defaultSelectionSketchType)
	{
		_tool = tool ?? throw new ArgumentNullException(nameof(tool));
		_defaultSelectionSketchType = defaultSelectionSketchType;

		SetSketchType(defaultSelectionSketchType);
	}

	/// <summary>
	/// Resets the sketch type to either last used (rectangle, lasso, polygon)
	/// or default (rectangle)
	/// </summary>
	public void ResetOrDefault()
	{
		if (_previousType == SketchGeometryType.Polygon)
		{
			TrySetSketchType(SketchGeometryType.Polygon);
			return;
		}

		if (_previousType == SketchGeometryType.Lasso)
		{
			TrySetSketchType(SketchGeometryType.Lasso);
			return;
		}

		TrySetSketchType(_defaultSelectionSketchType);
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

		TrySetSketchType(type);
	}

	private void TrySetSketchType(SketchGeometryType? type)
	{
		if (_tool.GetSketchType() == type)
		{
			return;
		}

		SetSketchType(type);
	}

	private void SetSketchType(SketchGeometryType? type)
	{
		_tool.SetSketchType(type);

		_msg.Info($"{_tool.Caption}: {type} selection sketch");

		_previousType = type;
	}
}

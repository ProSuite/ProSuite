using System;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public abstract class SelectionSketchTypeToggleBase : IDisposable
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISketchTool _tool;
	private readonly SketchGeometryType? _defaultSelectionSketchType;

	private SketchGeometryType? _currentSelectionSketchType;

	protected SelectionSketchTypeToggleBase([NotNull] ISketchTool tool,
	                                        SketchGeometryType defaultSelectionSketchType)
	{
		_tool = tool;
		_defaultSelectionSketchType = defaultSelectionSketchType;
	}

	public void ToggleSketchType(SketchGeometryType? sketchType)
	{
		SketchGeometryType? type = _currentSelectionSketchType == sketchType
			                           ? _defaultSelectionSketchType
			                           : sketchType;
		SetSketchType(_tool, type);
		_msg.Debug($"{type} selection sketch");
	}

	protected void ResetSelectionSketchType()
	{
		SetSketchType(_tool, _defaultSelectionSketchType);
	}

	protected void SetCurrentSelectionSketchType()
	{
		SetSketchType(_tool, _currentSelectionSketchType);
	}

	protected void SetSketchType(ISketchTool tool, SketchGeometryType? type)
	{
		tool.SetSketchType(type);
		_currentSelectionSketchType = type;
	}

	public void Dispose()
	{
		DisposeCore();
	}

	protected virtual void DisposeCore() { }
}

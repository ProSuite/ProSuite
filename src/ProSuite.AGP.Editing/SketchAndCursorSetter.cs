using System;
using System.Windows.Input;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SketchAndCursorSetter
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISketchTool _tool;
	[NotNull] private readonly SelectionCursors _cursors;

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public static SketchAndCursorSetter Create([NotNull] ISketchTool tool,
	                                           [NotNull] Cursor cursor,
	                                           [NotNull] Cursor lassoCursor,
	                                           [NotNull] Cursor polygonCursor,
	                                           SketchGeometryType defaultSelectionSketchType,
	                                           bool defaultSketchTypeOnFinishSketch = false)
	{
		return new SketchAndCursorSetter(tool, cursor, lassoCursor,
		                                 polygonCursor, defaultSelectionSketchType)
		       {
			       DefaultSketchTypeOnFinishSketch = defaultSketchTypeOnFinishSketch
		       };
	}

	/// <summary>
	/// Creates a new SketchAndCursorSetter using a SelectionCursors instance.
	/// Must be called on the MCT.
	/// </summary>
	public static SketchAndCursorSetter Create([NotNull] ISketchTool tool,
	                                           [NotNull] SelectionCursors selectionCursors,
	                                           SketchGeometryType defaultSelectionSketchType,
	                                           bool defaultSketchTypeOnFinishSketch = false)
	{
		return new SketchAndCursorSetter(tool, selectionCursors, defaultSelectionSketchType)
		       {
			       DefaultSketchTypeOnFinishSketch = defaultSketchTypeOnFinishSketch
		       };
	}

	private SketchAndCursorSetter([NotNull] ISketchTool tool,
	                              [NotNull] Cursor cursor,
	                              [NotNull] Cursor lassoCursor,
	                              [NotNull] Cursor polygonCursor,
	                              SketchGeometryType defaultSketchType)
	{
		_tool = tool ?? throw new ArgumentNullException(nameof(tool));

		// Create a SelectionCursors instance internally
		_cursors = new SelectionCursors()
		           {
			           RectangleCursor = cursor ?? throw new ArgumentNullException(nameof(cursor)),
			           LassoCursor = lassoCursor ??
			                         throw new ArgumentNullException(nameof(lassoCursor)),
			           PolygonCursor = polygonCursor ??
			                           throw new ArgumentNullException(nameof(polygonCursor))
		           };

		_cursors.DefaultSelectionSketchType = defaultSketchType;

		_tool.SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
		_tool.SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
	}

	private SketchAndCursorSetter([NotNull] ISketchTool tool,
	                              [NotNull] SelectionCursors selectionCursors,
	                              SketchGeometryType defaultSketchType)
	{
		_tool = tool ?? throw new ArgumentNullException(nameof(tool));
		_cursors = selectionCursors ?? throw new ArgumentNullException(nameof(selectionCursors));

		_tool.SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
		_tool.SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
	}

	public void SetSelectionCursorShift([NotNull] Cursor cursor)
	{
		_cursors.RectangleShiftCursor = cursor;
	}

	public void SetSelectionCursorLassoShift(Cursor cursor)
	{
		_cursors.LassoShiftCursor = cursor;
	}

	public void SetSelectionCursorPolygonShift(Cursor cursor)
	{
		_cursors.PolygonShiftCursor = cursor;
	}

	/// <summary>
	/// Resets the sketch type to either last used (rectangle, lasso, polygon)
	/// or default (rectangle)
	/// </summary>
	public void ResetOrDefault()
	{
		SketchGeometryType? previousSketchTypeToUse = null;

		SketchGeometryType? previousSketchType = _cursors.PreviousSelectionSketchType;

		if (! DefaultSketchTypeOnFinishSketch &&
		    previousSketchType is SketchGeometryType.Polygon or SketchGeometryType.Lasso)
		{
			previousSketchTypeToUse = previousSketchType;
		}

		SketchGeometryType? startSketchType =
			_cursors.GetStartSelectionSketchGeometryType(previousSketchTypeToUse);

		TrySetSketchType(startSketchType);
		SetCursor(startSketchType);
	}

	public bool DefaultSketchTypeOnFinishSketch { get; set; }

	public void Toggle(SketchGeometryType? sketchType, bool shiftDown = false)
	{
		try
		{
			SketchGeometryType? newSketchGeometryType =
				ToolUtils.ToggleSketchGeometryType(sketchType, _tool.GetSketchType(),
				                                   _cursors.DefaultSelectionSketchType);

			TrySetSketchType(newSketchGeometryType);
			SetCursor(newSketchGeometryType, shiftDown);
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
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

		_msg.Debug($"{_tool.Caption}: {type} sketch");
		_cursors.PreviousSelectionSketchType = type;
	}

	public void SetCursor(SketchGeometryType? type, bool shiftDown = false)
	{
		Cursor cursor = GetCursor(type, shiftDown);

		_tool.SetCursor(cursor);
	}

	public Cursor GetCursor(SketchGeometryType? geometryType, bool shiftDown)
	{
		return _cursors.GetCursor(geometryType, shiftDown);
	}
}

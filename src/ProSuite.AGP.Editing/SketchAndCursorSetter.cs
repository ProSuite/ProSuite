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
	[NotNull] private readonly Cursor _cursor;
	[NotNull] private readonly Cursor _lassoCursor;
	[NotNull] private readonly Cursor _polygonCursor;

	[CanBeNull] private Cursor _cursorShift;
	[CanBeNull] private Cursor _lassoCursorShift;
	[CanBeNull] private Cursor _polygonCursorShift;

	private readonly SketchGeometryType? _defaultSelectionSketchType;
	private SketchGeometryType? _previousType;

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

	private SketchAndCursorSetter([NotNull] ISketchTool tool,
	                              [NotNull] Cursor cursor,
	                              [NotNull] Cursor lassoCursor,
	                              [NotNull] Cursor polygonCursor,
	                              SketchGeometryType defaultSelectionSketchType)
	{
		_tool = tool ?? throw new ArgumentNullException(nameof(tool));
		_cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
		_lassoCursor = lassoCursor ?? throw new ArgumentNullException(nameof(lassoCursor));
		_polygonCursor = polygonCursor ?? throw new ArgumentNullException(nameof(polygonCursor));

		

		_defaultSelectionSketchType = defaultSelectionSketchType;

		// todo: daro not here? This class is not only for selection sketch anymore.
		_tool.SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
		_tool.SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
	}

	public void SetSelectionCursorShift(Cursor cursor)
	{
		_cursorShift ??= cursor;
	}

	public void SetSelectionCursorLassoShift(Cursor cursor)
	{
		_lassoCursorShift ??= cursor;
	}

	public void SetSelectionCursorPolygonShift(Cursor cursor)
	{
		_polygonCursorShift ??= cursor;
	}

	/// <summary>
	/// Resets the sketch type to either last used (rectangle, lasso, polygon)
	/// or default (rectangle)
	/// </summary>
	public void ResetOrDefault()
	{
		if (! DefaultSketchTypeOnFinishSketch)
		{
			if (_previousType == SketchGeometryType.Polygon)
			{
				TrySetSketchType(SketchGeometryType.Polygon);
				SetCursor(SketchGeometryType.Polygon);
				return;
			}

			if (_previousType == SketchGeometryType.Lasso)
			{
				TrySetSketchType(SketchGeometryType.Lasso);
				SetCursor(SketchGeometryType.Lasso);
				return;
			}
		}

		TrySetSketchType(_defaultSelectionSketchType);
		SetCursor(_defaultSelectionSketchType);
	}

	public bool DefaultSketchTypeOnFinishSketch { get; set; }

	public void Toggle(SketchGeometryType? sketchType, bool shiftDown = false)
	{
		try
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
			SetCursor(type, shiftDown);
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
		_previousType = type;
	}

	public void SetCursor(SketchGeometryType? type, bool shiftDown = false)
	{
		Cursor cursor = GetCursor(type, shiftDown);

		_tool.SetCursor(cursor);
	}

	private Cursor GetCursor(SketchGeometryType? geometryType, bool shiftDown)
	{
		switch (geometryType)
		{
			case SketchGeometryType.Rectangle:
				return shiftDown && _cursorShift != null
					       ? _cursorShift
					       : _cursor;
			case SketchGeometryType.Polygon:
				return shiftDown && _polygonCursorShift != null
					       ? _polygonCursorShift
					       : _polygonCursor;
			case SketchGeometryType.Lasso:
				return shiftDown && _lassoCursorShift != null
					       ? _lassoCursorShift
					       : _lassoCursor;
			default:
				return shiftDown && _cursorShift != null
					       ? _cursorShift
					       : _cursor;
		}
	}
}

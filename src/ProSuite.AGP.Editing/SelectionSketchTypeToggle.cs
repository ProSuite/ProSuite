using System;
using System.Windows.Input;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SelectionSketchTypeToggle
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISketchTool _tool;
	[NotNull] private readonly Cursor _selectionCursor;
	[NotNull] private readonly Cursor _selectionCursorLasso;
	[NotNull] private readonly Cursor _selectionCursorPolygon;

	[CanBeNull] private Cursor _selectionCursorShift;
	[CanBeNull] private Cursor _selectionCursorLassoShift;
	[CanBeNull] private Cursor _selectionCursorPolygonShift;

	private readonly SketchGeometryType? _defaultSelectionSketchType;
	private SketchGeometryType? _previousType;

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public static SelectionSketchTypeToggle Create([NotNull] ISketchTool tool,
	                                               [NotNull] Cursor cursor,
	                                               [NotNull] Cursor lassoCursor,
	                                               [NotNull] Cursor polygonCursor,
	                                               SketchGeometryType defaultSelectionSketchType,
	                                               bool defaultSketchTypeOnFinishSketch = false)
	{
		return new SelectionSketchTypeToggle(tool, cursor, lassoCursor,
		                                     polygonCursor, defaultSelectionSketchType)
		       {
			       DefaultSketchTypeOnFinishSketch = defaultSketchTypeOnFinishSketch
		       };
	}

	private SelectionSketchTypeToggle([NotNull] ISketchTool tool,
	                                  [NotNull] Cursor cursor,
	                                  [NotNull] Cursor lassoCursor,
	                                  [NotNull] Cursor polygonCursor,
	                                  SketchGeometryType defaultSelectionSketchType)
	{
		_tool = tool ?? throw new ArgumentNullException(nameof(tool));
		_selectionCursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
		_selectionCursorLasso = lassoCursor ?? throw new ArgumentNullException(nameof(lassoCursor));
		_selectionCursorPolygon = polygonCursor ?? throw new ArgumentNullException(nameof(polygonCursor));

		_defaultSelectionSketchType = defaultSelectionSketchType;

		SetSketchType(defaultSelectionSketchType);
		SetCursor(defaultSelectionSketchType);

		_tool.SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
		_tool.SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
	}

	public void SetSelectionCursorShift(Cursor cursor)
	{
		_selectionCursorShift ??= cursor;
	}

	public void SetSelectionCursorLassoShift(Cursor cursor)
	{
		_selectionCursorLassoShift ??= cursor;
	}

	public void SetSelectionCursorPolygonShift(Cursor cursor)
	{
		_selectionCursorPolygonShift ??= cursor;
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

	public void Toggle(SketchGeometryType? sketchType)
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
			SetCursor(type);
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

		_msg.Debug($"{_tool.Caption}: {type} selection sketch");
		_previousType = type;
	}

	public void SetCursor(SketchGeometryType? type, bool shiftDown = false)
	{
		Cursor cursor = GetCursor(type, shiftDown);

		_tool.SetCursor(cursor);
	}

	private Cursor GetCursor(SketchGeometryType? geometryType, bool shiftDown)
	{
		//bool shiftDown = false;

		//if (Application.Current.Dispatcher.CheckAccess())
		//{
		//	shiftDown = ShiftDown();
		//}
		//else
		//{
		//	Application.Current.Dispatcher.Invoke(() => { shiftDown = ShiftDown(); });
		//}

		switch (geometryType)
		{
			case SketchGeometryType.Rectangle:
				return shiftDown && _selectionCursorShift != null
					       ? _selectionCursorShift
					       : _selectionCursor;
			case SketchGeometryType.Polygon:
				return shiftDown && _selectionCursorPolygonShift != null
					       ? _selectionCursorPolygonShift
					       : _selectionCursorPolygon;
			case SketchGeometryType.Lasso:
				return shiftDown && _selectionCursorLassoShift != null
					       ? _selectionCursorLassoShift
					       : _selectionCursorLasso;
			default:
				return shiftDown && _selectionCursorShift != null
					       ? _selectionCursorShift
					       : _selectionCursor;
		}
	}
}

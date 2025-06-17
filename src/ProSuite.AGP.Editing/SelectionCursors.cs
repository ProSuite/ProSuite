using System;
using System.Windows.Input;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing
{
	/// <summary>
	/// Encapsulates the typical cursor combinations for a tool for selections. The base image is
	/// combined with the various overlays for polygon, lasso combined with the optional shift
	/// bitmap (+).
	/// </summary>
	public class SelectionCursors
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// Selection phase cursors
		[NotNull] private Cursor _rectangleCursor;
		[NotNull] private Cursor _lassoCursor;
		[NotNull] private Cursor _polygonCursor;
		[NotNull] private Cursor _rectangleShiftCursor;
		[NotNull] private Cursor _lassoShiftCursor;
		[NotNull] private Cursor _polygonShiftCursor;

		public static SelectionCursors CreateArrowCursors([NotNull] byte[] toolOverlay)
		{
			return new SelectionCursors(toolOverlay, Resources.Arrow);
		}

		public static SelectionCursors CreateCrossCursors([NotNull] byte[] toolOverlay)
		{
			const int xHotspot = 10;
			const int yHotspot = 10;
			return new SelectionCursors(toolOverlay, Resources.Cross, xHotspot, yHotspot);
		}

		/// <summary>
		/// Intermediate constructor to allow using setters instead of constructor.
		/// This shall be removed once it is not used any more.
		/// </summary>
		[Obsolete]
		public SelectionCursors() { }

		/// <summary>
		/// Creates the cursor combinations for a tool using the specified tool overlay image
		/// combined with the phase overlay or the default arrow image.
		/// </summary>
		/// <param name="toolOverlay">The tool-specific overlay to be combined with the base cursor and various other overlays</param>
		/// <param name="phaseOverlay">The icon of the selection phase. By default, this is an Arrow.</param>
		/// <param name="xHotspot">The x coordinate of the cursor's hotspot</param>
		/// <param name="yHotspot">The y coordinate of the cursor's hotspot</param>
		public SelectionCursors([NotNull] byte[] toolOverlay,
		                        [CanBeNull] byte[] phaseOverlay = null,
		                        int xHotspot = 0,
		                        int yHotspot = 0)
		{
			if (phaseOverlay == null)
			{
				phaseOverlay = Resources.Arrow;
			}

			// Create selection phase cursor combinations
			_rectangleCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay, xHotspot, yHotspot);
			_lassoCursor = ToolUtils.CreateCursor(phaseOverlay, toolOverlay, Resources.Lasso, null,
			                                      xHotspot, yHotspot);
			_polygonCursor = ToolUtils.CreateCursor(phaseOverlay, toolOverlay, Resources.Polygon,
			                                        null, xHotspot, yHotspot);

			// Selection shift variants
			_rectangleShiftCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay, Resources.Shift, null,
				                       xHotspot, yHotspot);
			_lassoShiftCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay, Resources.Lasso, Resources.Shift,
				                       xHotspot, yHotspot);
			_polygonShiftCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay, Resources.Polygon,
				                       Resources.Shift,
				                       xHotspot, yHotspot);
		}

		public SketchGeometryType DefaultSelectionSketchType { get; set; } =
			SketchGeometryType.Rectangle;

		// NOTE: This should probably be part of the tool state (i.e. a tool property) and most
		// likely the same 'last sketch type' should be used both for the selection phase and the
		// second or third phase.
		/// <summary>
		/// A property to maintain and remember the previously active selection sketch type.
		/// </summary>
		public SketchGeometryType? PreviousSelectionSketchType { get; set; }

		public SketchGeometryType? GetStartSelectionSketchGeometryType(
			SketchGeometryType? previousSketchTypeToUse)
		{
			return previousSketchTypeToUse ?? DefaultSelectionSketchType;
		}

		#region Cursor properties

		/// <summary>
		/// Gets the base cursor without overlay.
		/// </summary>
		[NotNull]
		public Cursor RectangleCursor
		{
			get => _rectangleCursor;
			set => _rectangleCursor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets the cursor with lasso overlay.
		/// </summary>
		[NotNull]
		public Cursor LassoCursor
		{
			get => _lassoCursor;
			set => _lassoCursor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets the cursor with polygon overlay.
		/// </summary>
		[NotNull]
		public Cursor PolygonCursor
		{
			get => _polygonCursor;
			set => _polygonCursor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets the cursor with shift overlay.
		/// </summary>
		[NotNull]
		public Cursor RectangleShiftCursor
		{
			get => _rectangleShiftCursor;
			set => _rectangleShiftCursor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets the cursor with lasso and shift overlays.
		/// </summary>
		[NotNull]
		public Cursor LassoShiftCursor
		{
			get => _lassoShiftCursor;
			set => _lassoShiftCursor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets the cursor with polygon and shift overlays.
		/// </summary>
		[NotNull]
		public Cursor PolygonShiftCursor
		{
			get => _polygonShiftCursor;
			set => _polygonShiftCursor = value ?? throw new ArgumentNullException(nameof(value));
		}

		#endregion

		public Cursor GetCursor(SketchGeometryType? geometryType, bool shiftDown)
		{
			switch (geometryType)
			{
				case SketchGeometryType.Rectangle:
					return shiftDown
						       ? RectangleShiftCursor
						       : RectangleCursor;
				case SketchGeometryType.Polygon:
					return shiftDown
						       ? PolygonShiftCursor
						       : PolygonCursor;
				case SketchGeometryType.Lasso:
					return shiftDown
						       ? LassoShiftCursor
						       : LassoCursor;
				default:
					_msg.Debug($"Unknown geometry type {geometryType} - using rectangle cursor");
					return shiftDown
						       ? RectangleShiftCursor
						       : RectangleCursor;
			}
		}
	}
}

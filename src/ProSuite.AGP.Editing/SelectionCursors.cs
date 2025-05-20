using System;
using System.Windows.Input;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing
{
	/// <summary>
	/// Encapsulates the typical cursor combinations for a tool for selections. The base image is
	/// combined with the various overlays for polygon, lasso combined with the optional shift
	/// bitmap (+).
	/// </summary>
	public class SelectionCursors
	{
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
			return new SelectionCursors(toolOverlay, Resources.Cross);
		}

		/// <summary>
		/// Intermediate constructor to allow using setters instead of constructor.
		/// This shall be removed once it is not used any more.
		/// </summary>
		public SelectionCursors() { }

		/// <summary>
		/// Creates the cursor combinations for a tool with the specified tool overlay image.
		/// </summary>
		/// <param name="toolOverlay">The tool-specific overlay to be combined with the base cursor and various other overlays</param>
		/// <param name="phaseOverlay">The icon of the selection phase. By default, this is an Arrow.</param>
		public SelectionCursors([NotNull] byte[] toolOverlay,
		                        [CanBeNull] byte[] phaseOverlay = null)
		{
			byte[] toolOverlay1 =
				toolOverlay ?? throw new ArgumentNullException(nameof(toolOverlay));

			if (phaseOverlay == null)
			{
				phaseOverlay = Resources.Arrow;
			}

			// Create selection phase cursor combinations
			_rectangleCursor = ToolUtils.CreateCursor(phaseOverlay, toolOverlay1, null);
			_lassoCursor = ToolUtils.CreateCursor(phaseOverlay, toolOverlay1, Resources.Lasso);
			_polygonCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay1, Resources.Polygon);

			// Selection shift variants
			_rectangleShiftCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay1, Resources.Shift);
			_lassoShiftCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay1, Resources.Lasso,
				                       Resources.Shift);
			_polygonShiftCursor =
				ToolUtils.CreateCursor(phaseOverlay, toolOverlay1, Resources.Polygon,
				                       Resources.Shift);
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
					return shiftDown
						       ? RectangleShiftCursor
						       : RectangleCursor;
			}
		}
	}
}

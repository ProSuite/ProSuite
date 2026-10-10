using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.DestroyAndRebuild;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Input;
using Dataset = ArcGIS.Core.Data.Dataset;

namespace ProSuite.AGP.Editing.CreateBufferedLine
{
	/// <summary>
	/// Creates a polygon (or multipatch, depending on the target template) by
	/// buffering a line sketch. The line is buffered by offsetting it to the
	/// requested side(s) at the current buffer width, with rounded corners between the
	/// segments. A two-sided buffer additionally has round end caps; a one-sided buffer
	/// keeps flat (straight) ends. The buffer geometry is built in the SDK-independent
	/// geometry model
	/// (<see cref="GeomTopoOpUtils.GetBufferedLine(MultiLinestring,double,BufferSide,double,out string,bool,bool,double)"/>).
	/// </summary>
	[UsedImplicitly]
	public abstract class CreateBufferedLineToolBase<TOptions, TPartial> : ConstructionToolBase
		where TOptions : BufferedLineToolOptionsBase<TPartial>
		where TPartial : PartialCreateBufferedLineOptions, new()
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const Key _keyIncreaseBufferWidth = Key.D2;
		private const Key _keyDecreaseBufferWidth = Key.D1;

		private const double _bufferWidthIncrement = 0.1;

		private readonly Latch _drawFeedbackLatch = new();

		// Guards against out-of-order buffer preview refreshes. Every refresh takes a ticket
		// and only draws while it still holds the newest one. Rapid sketch changes (holding
		// CTRL+Z down) put several refreshes in flight at once, and one that reads an older
		// sketch but finishes last would otherwise leave that older preview on the map.
		private int _previewRefreshTicket;

		// Per-part full buffer widths, aligned to the sketch parts. New parts inherit
		// the current width; the CTRL measure line and the 1/2 keys change the width of
		// the current part.
		private readonly List<double> _bufferWidths = new();
		private int _currentPart;
		private double _currentBufferWidth = CreateBufferedLineToolOptions.DefaultBufferWidth;

		// CTRL-drag measure line state (measures the full buffer width).
		private bool _measuring;
		private MapPoint _measureStart;

		// True once the second measure click has applied a new width, until CTRL is
		// released. It is what brings the buffer-width circle back - at the new width -
		// while the measure gesture itself keeps the circle hidden.
		private bool _measuredWidthApplied;

		// True while a sketch is being drawn (has at least one vertex). While a sketch is in
		// progress, SHIFT-clicks are left to the sketch engine (e.g. SHIFT + double-click to
		// finish the part) instead of being used for the shift-select-existing-line feature.
		private bool _sketchInProgress;

		// While CTRL is held the edit sketch is suspended: it is taken away from the sketch
		// engine and drawn as a static overlay instead. Without this the sketch engine stays
		// live during the measure, so the last segment keeps rubber banding to the cursor and
		// the snap chip advertises a snap for a vertex that is never added - the measure
		// points come from ClientToMapPoint and are not snapped. Suspending and restoring
		// both go through SketchStates, which also draws the overlay.
		// _suspendedSketch is only kept to test the measure line against the sketch parts.
		private Geometry _suspendedSketch;
		private bool _sketchSuspended;

		// UseSnapping as it was before the measure suspended the sketch. Captured rather
		// than assumed, so a subclass that sketches without snapping is not switched on by
		// the restore.
		private bool _snappingBeforeMeasure = true;

		// Latched while the sketch is taken away and put back, so the sketch-modified and
		// sketch-canceled callbacks caused by our own SetCurrentSketchAsync calls are not
		// mistaken for the user editing (or canceling) the sketch.
		private readonly Latch _sketchSuspensionLatch = new();

		// True once CTRL turned out to be part of a chord (CTRL+Z, CTRL+Y, ...) rather than
		// the bare measure modifier. It suppresses the suspension and the measure until CTRL
		// is released, so application shortcuts keep working while a sketch is in progress.
		private bool _ctrlUsedInChord;

		private readonly List<IDisposable> _overlays = new();
		private readonly List<IDisposable> _measureOverlays = new();
		private CIMLineSymbol _bufferSymbol;
		private CIMLineSymbol _measureLineSymbol;
		private CIMLineSymbol _circleSymbol;

		private TOptions _bufferedLineToolOptions;
		private OverridableSettingsProvider<TPartial> _settingsProvider;

		protected CreateBufferedLineToolBase()
		{
			UseSnapping = true;
			RequiresSelection = false;
			LogSketchVertexZs = false;
			ContextToolbarID = "";

			_bufferedLineToolOptions = CreateToolOptions(null, new TPartial());

			HandledKeys.Add(_keyIncreaseBufferWidth);
			HandledKeys.Add(_keyDecreaseBufferWidth);
		}

		// Cached handle to the selection cursor. FirstPhaseCursors is read (and cached here)
		// during activation, before the sketch phase reassigns the live SelectionCursors to
		// the sketch crosshair.
		private SelectionCursors _lineSelectionCursors;

		// Cursor shown while CTRL is held to measure the buffer width: the cross cursor
		// combined with the Measure overlay.
		private SelectionCursors _measureCursors;

		private SelectionCursors MeasureCursors =>
			_measureCursors ??= SelectionCursors.CreateCrossCursors(Resources.Measure);

		protected TOptions BufferedLineToolOptions =>
			_bufferedLineToolOptions;

		/// <summary>
		/// Creates the tool's options from the (optional) central and local partial options.
		/// Each concrete tool supplies its own options type so the buffered-line and wall
		/// tools can persist and diverge independently.
		/// </summary>
		[NotNull]
		protected abstract TOptions CreateToolOptions([CanBeNull] TPartial centralOptions,
		                                              [CanBeNull] TPartial localOptions);

		/// <summary>
		/// Whether convex corners of the buffer are mitered (and bevelled past the miter
		/// limit) instead of rounded. The polygon buffered-line rounds corners; the wall tool
		/// overrides this to miter them.
		/// </summary>
		protected virtual bool UseMiteredCorners => false;

		/// <summary>
		/// Whether a two-sided buffer is closed with straight (flat) ends instead of round end
		/// caps. The polygon buffered-line uses round caps; the wall tool overrides this to
		/// keep the ends straight.
		/// </summary>
		protected virtual bool UseFlatEndCaps => false;

		// The tool buffers a single existing line at a time (shift-select).
		protected override bool AllowMultiSelection(out string reason)
		{
			reason = "Only a single line can be buffered at a time.";
			return false;
		}

		#region Multipatch Destroy & Rebuild wiring

		/// <summary>
		/// The multipatch "Destroy &amp; Rebuild" helper, supplied by a participating subclass (the
		/// wall tool). Null for buffered-line tools that do not produce a multipatch (the generic
		/// polygon buffered-line tool), which therefore do not take part in the replace mode. The
		/// concrete implementation lives in the roofs assembly, so the subclass instantiates it.
		/// </summary>
		[CanBeNull]
		protected virtual IDestroyAndRebuilder Rebuilder => null;

		// Whether the tool currently replaces a multipatch (a rebuilder is supplied and mode is on).
		private bool ReplaceActive => Rebuilder is { IsActive: true };

		protected override bool AllowSelectionChangeInSketchMode =>
			ReplaceActive || base.AllowSelectionChangeInSketchMode;

		protected override void OnToolMouseMove(MapViewMouseEventArgs args)
		{
			Rebuilder?.EnsureInitialFeedbackRefresh();
			base.OnToolMouseMove(args);
		}

		#endregion

		// Only polylines can be shift-selected as the line to buffer. In multipatch replace mode
		// the shift-selection instead re-picks the multipatch target (handled by the base class).
		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			if (ReplaceActive)
			{
				return Rebuilder.CanSelectTargetGeometryType(geometryType);
			}

			return geometryType == GeometryType.Polyline;
		}

		protected override SelectionCursors FirstPhaseCursors =>
			_lineSelectionCursors ??= base.FirstPhaseCursors;

		[CanBeNull]
		protected abstract string OptionsDockPaneID { get; }

		protected virtual string OptionsFileName => "CreateBufferedLineToolOptions.xml";

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		protected virtual string LocalConfigDir =>
			EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				AppDataFolder.Roaming, "ToolDefaults");

		// The offset applied per side: half the (user-facing) width for a two-sided
		// buffer, the full width for a one-sided buffer.
		private double OffsetRatio =>
			_bufferedLineToolOptions.BufferSide == BufferSide.Both ? 0.5 : 1.0;

		#region Overrides

		protected override void OnCurrentTemplateUpdated()
		{
			UpdateEnabled();
		}

		protected override void OnUpdateCore()
		{
			UpdateEnabled();
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			// In multipatch Destroy & Rebuild mode a rectangle selection lets SHIFT-click pick the
			// target directly; a polygon selection would need several clicks to finish.
			return ReplaceActive
				       ? SketchGeometryType.Rectangle
				       : SketchGeometryType.Polygon;
		}

		protected override SketchGeometryType GetEditSketchGeometryType()
		{
			return SketchGeometryType.Line;
		}

		protected override async Task<bool?> GetEditSketchHasZ()
		{
			Stopwatch watch = Stopwatch.StartNew();

			int selectionCount = 0;
			bool? result = await QueuedTask.Run(() =>
			{
				var selectionByLayer = SelectionUtils.GetSelection(ActiveMapView.Map);

				if (selectionByLayer.Count == 0)
				{
					_msg.Debug($"{Caption}: no feature layer found in selection");
					return null;
				}

				bool? hasAnyZ = false;

				foreach (var selectedOidByLayer in selectionByLayer)
				{
					if (selectedOidByLayer.Key is FeatureLayer layer)
					{
						FeatureClass featureClass = layer.GetFeatureClass();
						bool? layerHasZ = featureClass?.GetDefinition()?.HasZ();

						if (layerHasZ == true)
						{
							hasAnyZ = true;
							break;
						}
					}
				}

				return hasAnyZ;
			});

			_msg.DebugStopTiming(
				watch, "Determined sketch has Z: {0} (evaluated {1} selected layers)", result,
				selectionCount);

			return result;
		}

		protected override void LogPromptForSelection() { }

		protected override void LogEnteringSketchMode()
		{
			_msg.InfoFormat(
				"Buffered line: draw the line to buffer, then finish the sketch. " +
				"Current buffer width: {0}. Hold [CTRL] to measure the width, press [1]/[2] " +
				"to de-/increase it, [O] for options.", _currentBufferWidth);
		}

		protected override bool DefaultSketchTypeOnFinishSketch => true;

		protected override async Task OnToolActivatingCoreAsync()
		{
			_bufferedLineToolOptions = InitializeOptions();
			_currentBufferWidth = _bufferedLineToolOptions.BufferWidth;
			await base.OnToolActivatingCoreAsync();

			if (Rebuilder != null)
			{
				await Rebuilder.ActivateAsync(() => ActiveMapView);
			}

			// If a line is already selected when the tool is activated, buffer it right away.
			await TryBufferInitialSelectionAsync();
		}

		protected override async Task ShiftPressedCoreAsync(MapViewKeyEventArgs keyArgs)
		{
			await base.ShiftPressedCoreAsync(keyArgs);

			// Show the selection cursor to signal that Shift-click selects an existing line.
			SetToolCursor(FirstPhaseCursors.GetCursor(GetSketchType(), shiftDown: true));
		}

		protected override async Task ShiftReleasedCoreAsync()
		{
			await base.ShiftReleasedCoreAsync();

			// Back to the sketch crosshair.
			SetToolCursor(SketchCursors.GetCursor(GetSketchType(), shiftDown: false));
		}

		protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
		{
			if (_bufferedLineToolOptions != null)
			{
				_bufferedLineToolOptions.PropertyChanged -= OptionsPropertyChanged;
			}

			if (_bufferedLineToolOptions != null)
			{
				_settingsProvider?.StoreLocalConfiguration(_bufferedLineToolOptions.LocalOptions);
			}

			DisposeOverlays();
			DisposeMeasureOverlays();
			ResetSuspendedSketchState();
			Rebuilder?.Deactivate();
			return base.OnToolDeactivateCore(hasMapViewChanged);
		}

		protected override Task OnSketchPhaseStartedAsync()
		{
			ResetBufferState();
			return base.OnSketchPhaseStartedAsync();
		}

		protected override async Task<bool> OnSketchModifiedAsyncCore()
		{
			// Suspending and restoring the sketch around a CTRL measure clears and re-sets
			// the sketch: those are our own modifications, not the user's. Ignoring them
			// keeps _sketchInProgress reflecting the real sketch (it gates the SHIFT
			// gesture) and keeps the buffer preview frozen during the measure.
			if (_sketchSuspensionLatch.IsLatched || _sketchSuspended)
			{
				return await base.OnSketchModifiedAsyncCore();
			}

			// During a multipatch replace target reselection the "sketch" is the selection
			// rectangle, not a line to buffer: skip the buffer preview and the in-progress flag.
			if (ReplaceActive && ! IsInSketchMode)
			{
				return await base.OnSketchModifiedAsyncCore();
			}

			Geometry sketch = await GetCurrentSketchAsync();
			_sketchInProgress = sketch is { IsEmpty: false };

			await RefreshBufferPreviewAsync();
			return await base.OnSketchModifiedAsyncCore();
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs args)
		{
			// CTRL is the measure modifier only while it is held alone. As soon as another
			// key joins it the chord belongs to the application (CTRL+Z / CTRL+Y above all),
			// so hand the sketch back at once and do not measure until CTRL is released and
			// pressed again. This has to be done here: HandleKeyDownCoreAsync is only called
			// for modifier keys and for HandledKeys, so it never sees a key such as Z.
			if (KeyboardUtils.IsCtrlDown() && ! KeyboardUtils.IsModifierKey(args.Key))
			{
				_ctrlUsedInChord = true;

				if (_sketchSuspended)
				{
					// Not awaited (this override is synchronous): the sketch type is put back
					// before the first await, the sketch itself follows immediately after.
					_ = ResumeSketchAsync();
				}
			}

			// [1]/[2] change the buffer width only as bare keys. When a modifier is held
			// (e.g. SHIFT+1 / SHIFT+2, the "Synchronize Map Centers [and Scales]" shortcuts)
			// do not claim the key: returning here leaves args.Handled false, so it falls
			// through to the application shortcut instead of changing the buffer width.
			if ((args.Key == _keyIncreaseBufferWidth || args.Key == _keyDecreaseBufferWidth) &&
			    Keyboard.Modifiers != ModifierKeys.None)
			{
				return;
			}

			base.OnToolKeyDown(args);
		}

		protected override async Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
		{
			if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
			{
				// Show the measure cursor (cross + Measure overlay) while CTRL is held.
				SetToolCursor(MeasureCursors.GetCursor(GetSketchType(), shiftDown: false));

				// NOTE: the sketch is deliberately NOT suspended here. CTRL on its own is
				// still ambiguous at this point - it may well be the start of CTRL+Z. The
				// suspension happens on the first CTRL + mouse move instead, which is both
				// the first moment the rubber band would actually be visible and a gesture
				// no keyboard shortcut performs. See OnToolMouseMoveCore.
			}

			if (args.Key == _keyIncreaseBufferWidth)
			{
				SetCurrentBufferWidth(_currentBufferWidth + _bufferWidthIncrement);
				LogBufferWidth();
				await QueuedTask.Run(RefreshBufferPreviewAsync);
				return;
			}

			if (args.Key == _keyDecreaseBufferWidth)
			{
				double decreased = _currentBufferWidth - _bufferWidthIncrement;

				// Keep the current width if decreasing further would reach zero or below.
				if (decreased > 0)
				{
					SetCurrentBufferWidth(decreased);
					LogBufferWidth();
					await QueuedTask.Run(RefreshBufferPreviewAsync);
				}
				else
				{
					_msg.InfoFormat(
						"Buffer width kept at {0:N2}: decreasing it by {1:N2} would reach " +
						"zero or below. Enter a smaller value in the options to go lower.",
						_currentBufferWidth, _bufferWidthIncrement);
				}

				return;
			}

			await base.HandleKeyDownCoreAsync(args);
		}

		protected override async Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
			{
				_ctrlUsedInChord = false;
				_measuredWidthApplied = false;

				// A measure started with one click and never completed ends here: drop the
				// pending start point and its overlay, which would otherwise keep following
				// the cursor across the restored sketch.
				CancelPendingMeasure();

				// Hand the sketch back to the sketch engine and redraw the buffer preview
				// before reading the sketch type for the cursor below.
				await ResumeSketchAsync();

				// Restore the sketch (or, if SHIFT is held, selection) cursor.
				bool shiftDown = KeyboardUtils.IsShiftDown();
				SelectionCursors cursors = shiftDown ? FirstPhaseCursors : SketchCursors;
				SetToolCursor(cursors?.GetCursor(GetSketchType(), shiftDown));
			}

			await base.HandleKeyUpCoreAsync(args);
		}

		protected override void OnToolMouseDownCore(MapViewMouseButtonEventArgs args)
		{
			if (args.ChangedButton != MouseButton.Left)
			{
				return;
			}

			// Intercept CTRL-clicks (measure line) and SHIFT-clicks (select an existing line
			// to buffer): setting Handled prevents the sketch engine from adding a vertex and
			// routes the click to OnToolMouseDownCoreAsync instead. SHIFT is only claimed when
			// no sketch is in progress; while drawing, SHIFT gestures (e.g. SHIFT +
			// double-click to finish the part) must reach the sketch engine.
			// In multipatch replace mode the SHIFT-selection is handled by the base class'
			// intermittent selection (rectangle + picker), so do not claim it here.
			bool shiftSelectExistingLine =
				KeyboardUtils.IsShiftDown() && ! _sketchInProgress && ! ReplaceActive;

			if ((KeyboardUtils.IsCtrlDown() && ! _ctrlUsedInChord) || shiftSelectExistingLine)
			{
				args.Handled = true;
			}
		}

		protected override Task OnToolMouseDownCoreAsync(MapViewMouseButtonEventArgs args)
		{
			if (args.ChangedButton != MouseButton.Left)
			{
				return Task.CompletedTask;
			}

			if (KeyboardUtils.IsShiftDown())
			{
				// In multipatch replace mode the base class handles the shift-selection
				// (intermittent rectangle + picker), so do not run the line shift-select.
				if (ReplaceActive)
				{
					return Task.CompletedTask;
				}

				// Only shift-select an existing line when not drawing; while a sketch is in
				// progress the shift gesture belongs to the sketch engine.
				return _sketchInProgress ? Task.CompletedTask : HandleShiftSelectAsync(args);
			}

			if (! KeyboardUtils.IsCtrlDown() || _ctrlUsedInChord)
			{
				// CTRL is held as part of an application chord, not as the measure modifier.
				return Task.CompletedTask;
			}

			return QueuedTask.Run(async () =>
			{
				MapPoint mapPoint = MapUtils.ClientToMapPoint(MapView.Active, args.ClientPoint);

				if (! _measuring)
				{
					_measureStart = mapPoint;
					_measuring = true;
					_msg.Info("Measuring buffer width.");
				}
				else
				{
					await StopMeasureAsync(mapPoint);
				}
			});
		}

		protected override async void OnToolMouseMoveCore(MapViewMouseEventArgs args)
		{
			try
			{
				// Keyboard.IsKeyDown is a WPF call and belongs on the UI thread, so the
				// measure state is evaluated here and handed to the drawing below, which
				// runs on the MCT.
				bool ctrlDown = KeyboardUtils.IsCtrlDown();

				if (! ctrlDown)
				{
					_ctrlUsedInChord = false;
					_measuredWidthApplied = false;

					if (_sketchSuspended)
					{
						// The CTRL key-up can be lost, e.g. by ALT+TAB while measuring. Hand
						// the sketch back as soon as the mouse moves over the map again, so
						// the user is never left with an overlay and no editable sketch.
						CancelPendingMeasure();

						await ResumeSketchAsync();

						SetToolCursor(
							SketchCursors?.GetCursor(GetSketchType(), shiftDown: false));
						return;
					}
				}
				else if (! _ctrlUsedInChord)
				{
					// First CTRL + mouse move: now the measure gesture is unambiguous (no
					// keyboard shortcut moves the mouse), so take the sketch away from the
					// sketch engine. A no-op once suspended.
					await SuspendSketchAsync();
				}

				if (_drawFeedbackLatch.IsLatched)
				{
					return;
				}

				// The measure gesture runs from CTRL down until the second click applies a
				// width. The circle is hidden for its duration, see DrawCursorFeedback.
				bool measureGestureActive =
					ctrlDown && ! _ctrlUsedInChord && ! _measuredWidthApplied;

				_drawFeedbackLatch.Increment();

				await QueuedTask.Run(() =>
				{
					MapPoint mapPoint =
						MapUtils.ClientToMapPoint(MapView.Active, args.ClientPoint);

					DrawCursorFeedback(mapPoint, measureGestureActive);
				});
			}
			catch (Exception ex)
			{
				_msg.Warn($"Error in mouse move: {ex.Message}", ex);
			}
			finally
			{
				_drawFeedbackLatch.Decrement();
			}
		}

		// The sketch can be canceled without going through ESC, e.g. via the ArcGIS Pro sketch
		// context menu's "Cancel" command. That path does not reach HandleEscapeAsync, so clear
		// the buffer/measure feedback here to make sure it is removed no matter how the sketch
		// was canceled.
		protected override Task<bool> OnSketchCanceledAsyncCore()
		{
			if (_sketchSuspensionLatch.IsLatched || _sketchSuspended)
			{
				// Clearing the sketch in order to suspend it can surface as a sketch cancel.
				// That must not throw away the per-part buffer widths and the preview.
				return base.OnSketchCanceledAsyncCore();
			}

			ClearFeedback();

			return base.OnSketchCanceledAsyncCore();
		}

		protected override async Task HandleEscapeAsync()
		{
			// Before ClearFeedback, which resets the suspension state: ESC while measuring
			// must still put the sketch type back.
			AbandonSuspendedSketch();

			ClearFeedback();

			// With RequiresSelection == false the base only resets the sketch on ESC and
			// never clears the selection. When no sketch is in progress (e.g. right after a
			// feature was created) also clear the selection, so the newly created feature is
			// deselected - the standard construction-tool behaviour.
			Geometry currentSketch = await GetCurrentSketchAsync();
			if (currentSketch == null || currentSketch.IsEmpty)
			{
				await ClearSelectionAsync();
			}

			_msg.Info("Draw the line of the buffer.");

			await base.HandleEscapeAsync();
		}

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			EditingTemplate editTemplate,
			MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			bool created = await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					var sketchLine = sketchGeometry as Polyline;

					await SetCurrentSketchAsync(null);

					return await BufferLineAndCreateFeatureCoreAsync(
						       sketchLine, editTemplate, activeView, cancelableProgressor);
				}
				catch (Exception ex)
				{
					_msg.Error("Error in OnEditSketchCompleteCoreAsync", ex);
					return false;
				}
				finally
				{
					DisposeOverlays();
					DisposeMeasureOverlays();
					await SetCurrentSketchAsync(null);
					ResetBufferState();

					SetToolCursor(SelectionCursors.GetCursor(GetSketchType(), false));
				}
			});

			return created;
		}

		// Builds the buffer polygon for the given line and inserts it as a new feature into
		// the current target class. Must run on the MCT (QueuedTask). Shared by the sketch
		// flow and the shift-select flow.
		private async Task<bool> BufferLineAndCreateFeatureCoreAsync(
			[CanBeNull] Polyline line,
			[CanBeNull] EditingTemplate editTemplate,
			[CanBeNull] MapView activeView,
			[CanBeNull] CancelableProgressor cancelableProgressor)
		{
			Polygon bufferPolygon = BuildBufferPolygon(
				line, out IList<Linestring> sourcePaths, out IList<double> offsetDistances);

			if (bufferPolygon?.IsEmpty != false)
			{
				_msg.Warn("The buffer is null or empty. No feature was created.");
				return false;
			}

			// Apply the final Z to the buffer. The base keeps the Z from the offset geometry;
			// a derived tool may drape it onto the elevation surface (see ApplyResultZ).
			bufferPolygon = ApplyResultZ(bufferPolygon, activeView);

			SetToolCursor(Cursors.Wait);

			FeatureClass currentTargetClass = GetCurrentTargetClass(out Subtype subtype);

			if (currentTargetClass == null)
			{
				_msg.Warn("No valid target feature class found.");
				return false;
			}

			FeatureLayer currentTargetLayer = ToolUtils.CurrentTargetLayer(editTemplate);
			string layerName = currentTargetLayer?.Name ?? currentTargetClass.GetName();
			string subtypeName = subtype?.GetName() ?? layerName;

			Geometry newGeometry =
				CreateResultGeometry(bufferPolygon, sourcePaths, offsetDistances);

			if (newGeometry?.IsEmpty != false)
			{
				_msg.Warn("The buffer geometry is empty. No feature was created.");
				return false;
			}

			if (ReplaceActive)
			{
				// Destroy & Rebuild: replace the selected multipatch instead of inserting a new
				// feature - but only when a single, visible multipatch is selected. If not, fall
				// through and create a new feature as usual.
				ReplaceGeometryResult replaceResult =
					await Rebuilder.TryReplaceSelectedGeometryAsync(
						newGeometry, activeView, Caption);

				if (replaceResult != ReplaceGeometryResult.NoTarget)
				{
					return replaceResult == ReplaceGeometryResult.Replaced;
				}
			}

			IEnumerable<Dataset> datasets = new List<Dataset> { currentTargetClass };

			return await GdbPersistenceUtils.ExecuteInTransactionAsync(
				       editContext =>
				       {
					       var newFeatures = GdbPersistenceUtils
					                         .InsertTx(editContext, currentTargetClass,
					                                   subtype,
					                                   new List<Geometry> { newGeometry },
					                                   GetFieldValue,
					                                   cancelableProgressor)
					                         .ToList();

					       if (newFeatures.Count > 0)
					       {
						       SelectCreatedFeatures(activeView, newFeatures);
					       }

					       List<long> oids = newFeatures.Select(f => f.GetObjectID())
					                                    .ToList();

					       _msg.Info(
						       $"Created new {(oids.Count > 1 ? "features" : "feature")} {layerName} ({subtypeName}) {(oids.Count > 1 ? "IDs" : "ID")}: {StringUtils.Concatenate(oids, ",")}");

					       return newFeatures.Count > 0;
				       }, "Create buffered line", datasets);
		}

		#endregion

		#region DockPane integration

		protected override void ShowOptionsPane()
		{
			var viewModel = GetBufferedLineViewModel();
			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _bufferedLineToolOptions;
			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			GetBufferedLineViewModel()?.Hide();
		}

		[CanBeNull]
		private DockPaneCreateBufferedLineViewModelBase<TOptions, TPartial>
			GetBufferedLineViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneCreateBufferedLineViewModelBase<TOptions, TPartial>;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		#endregion

		#region Buffer geometry

		[CanBeNull]
		private Polygon BuildBufferPolygon([CanBeNull] Polyline sketchLine)
		{
			return BuildBufferPolygon(sketchLine, out _, out _);
		}

		// Also exposes the simplified line parts and the per-part offset distances the buffer
		// was built from, so a derived tool (e.g. the wall) can build a result geometry that
		// needs the per-segment structure rather than just the buffer outline.
		[CanBeNull]
		private Polygon BuildBufferPolygon([CanBeNull] Polyline sketchLine,
		                                   out IList<Linestring> simplifiedPaths,
		                                   out IList<double> offsetDistances)
		{
			simplifiedPaths = new List<Linestring>();
			offsetDistances = new List<double>();

			if (sketchLine == null || sketchLine.IsEmpty)
			{
				return null;
			}

			// Buffer the simplified line. The sketch engine hands the finished sketch to
			// OnEditSketchCompleteCoreAsync already simplified, so a fold-back (a segment that
			// retraces an earlier one) arrives there as a clean line; the live preview, in
			// contrast, buffers the raw sketch from GetCurrentSketchAsync. Simplifying here makes
			// both paths buffer the identical line, so the preview matches the created feature
			// instead of showing the raw offset outline (with intermediate caps and loops).
			if (GeometryEngine.Instance.SimplifyAsFeature(sketchLine, forceSimplify: true) is
				    Polyline simplifiedLine && ! simplifiedLine.IsEmpty)
			{
				sketchLine = simplifiedLine;
			}

			MultiPolycurve line = GeomConversionUtils.CreateMultiPolycurve(sketchLine);
			IList<Linestring> paths = line.GetLinestrings().ToList();

			offsetDistances = GetPerPartOffsetDistances(paths.Count);
			simplifiedPaths = paths;

			double tolerance = GetTolerance(sketchLine);

			MultiLinestring buffer = GeomTopoOpUtils.GetBufferedLine(
				paths, offsetDistances, _bufferedLineToolOptions.BufferSide, tolerance,
				out string message, UseMiteredCorners, UseFlatEndCaps);

			if (buffer == null)
			{
				if (! string.IsNullOrEmpty(message))
				{
					_msg.Debug(message);
				}

				return null;
			}

			if (_bufferedLineToolOptions.EnforceMinimumSegmentLength &&
			    _bufferedLineToolOptions.MinimumSegmentLength > 0)
			{
				buffer = RemoveShortSegments(
					buffer, _bufferedLineToolOptions.MinimumSegmentLength);

				if (buffer.IsEmpty)
				{
					return null;
				}
			}

			Polygon polygon =
				GeomConversionUtils.CreatePolygon(buffer, sketchLine.SpatialReference);

			if (polygon == null || polygon.IsEmpty)
			{
				return null;
			}

			if (_bufferedLineToolOptions.Weed && _bufferedLineToolOptions.WeedTolerance > 0)
			{
				polygon =
					GeometryEngine.Instance.Generalize(
						polygon, _bufferedLineToolOptions.WeedTolerance) as Polygon;
			}

			return polygon;
		}

		protected static double GetTolerance([NotNull] Geometry geometry)
		{
			double tolerance = geometry.SpatialReference?.XYTolerance ?? 0;
			return tolerance > 0 ? tolerance : 0.001;
		}

		/// <summary>
		/// Turns the buffer polygon into the geometry that is stored in the target feature
		/// class. The base tool stores the polygon as-is; a derived tool (e.g. CreateWall)
		/// overrides this to return a different geometry such as a multipatch (see
		/// <see cref="ConvertToMultipatch"/>). The simplified source line parts and the per-part
		/// offset distances the buffer was built from are supplied as well, so a derived tool can
		/// reconstruct the per-segment structure (e.g. to build individually coplanar wall faces)
		/// instead of relying on the buffer outline alone.
		/// </summary>
		[CanBeNull]
		protected virtual Geometry CreateResultGeometry(
			[NotNull] Polygon bufferPolygon,
			[NotNull] IList<Linestring> sourcePaths,
			[NotNull] IList<double> offsetDistances)
		{
			return bufferPolygon;
		}

		/// <summary>
		/// Applies the final Z values to the buffer polygon before the feature is created.
		/// The base keeps the Z that comes from the offset geometry (the sketch or selected
		/// line vertices). A derived tool such as the polygon CreateBufferedLine overrides
		/// this to drape the buffer onto the map's elevation surface (see
		/// <see cref="ApplySurfaceZ"/>), while a multipatch tool (e.g. CreateWall) keeps the
		/// offset-geometry Z by not overriding it.
		/// </summary>
		[NotNull]
		protected virtual Polygon ApplyResultZ([NotNull] Polygon bufferPolygon,
		                                       [CanBeNull] MapView mapView)
		{
			return bufferPolygon;
		}

		// Replaces the Z of each vertex with the elevation of the map's surface at that
		// location. Falls back to the incoming geometry (i.e. the sketch-line Z values) when
		// the map has no surface or the query fails (e.g. outside the surface extent).
		[NotNull]
		protected Polygon ApplySurfaceZ([NotNull] Polygon polygon, [CanBeNull] MapView mapView)
		{
			Map map = mapView?.Map;

			if (map == null || ! MapUtils.MapHasSurface(map))
			{
				return polygon;
			}

			try
			{
				if (MapUtils.ApplyZsFromElevation(polygon, map, out SurfaceZsResult result) &&
				    result.Geometry is Polygon draped && ! draped.IsEmpty)
				{
					return draped;
				}

				_msg.DebugFormat(
					"Could not drape the buffer on the elevation surface (status: {0}). " +
					"Keeping the sketch Z values.", result.Status);
			}
			catch (Exception ex)
			{
				_msg.Warn(
					$"Error draping the buffer on the elevation surface: {ex.Message}. " +
					"Keeping the sketch Z values.", ex);
			}

			return polygon;
		}

		// Turns the buffer polygon into a multipatch. A closed-loop (annulus) buffer yields a
		// polygon with an exterior ring and an interior hole; the conversion keeps that hole
		// (exterior ring as a FirstRing patch, each interior ring as a hole Ring patch) instead
		// of filling it with a separate solid ring.
		[CanBeNull]
		protected static Multipatch ConvertToMultipatch([CanBeNull] Polygon polygon)
		{
			return GeomConversionUtils.CreateMultipatch(polygon);
		}

		// Removes vertices from the buffer outline so that no remaining segment is shorter
		// than the given minimum length (2D). Rings that would collapse below three segments
		// are dropped; if every ring collapses the original buffer is returned unchanged.
		[NotNull]
		private static MultiLinestring RemoveShortSegments([NotNull] MultiLinestring buffer,
		                                                   double minSegmentLength)
		{
			double minLengthSquared = minSegmentLength * minSegmentLength;

			var cleanedRings = new List<Linestring>();

			foreach (Linestring ring in buffer.GetLinestrings())
			{
				Linestring cleaned = RemoveShortSegments(ring, minLengthSquared);

				if (cleaned != null)
				{
					cleanedRings.Add(cleaned);
				}
			}

			return cleanedRings.Count == 0 ? buffer : new MultiPolycurve(cleanedRings);
		}

		[CanBeNull]
		private static Linestring RemoveShortSegments([NotNull] Linestring ring,
		                                              double minLengthSquared)
		{
			IList<Pnt3D> points = ring.GetPoints(clone: true).ToList();

			// A ring has one closing point in addition to its vertices; a triangle therefore
			// has four points. Anything smaller is left untouched.
			if (points.Count < 4)
			{
				return ring;
			}

			// Greedily keep a vertex only if it is far enough from the previously kept one.
			var kept = new List<Pnt3D> { points[0] };

			for (int i = 1; i < points.Count - 1; i++)
			{
				if (DistanceSquaredXY(kept[kept.Count - 1], points[i]) >= minLengthSquared)
				{
					kept.Add(points[i]);
				}
			}

			// Drop trailing vertices whose closing segment back to the start is too short.
			while (kept.Count > 3 &&
			       DistanceSquaredXY(kept[kept.Count - 1], kept[0]) < minLengthSquared)
			{
				kept.RemoveAt(kept.Count - 1);
			}

			if (kept.Count < 3)
			{
				return null;
			}

			kept.Add(kept[0].ClonePnt3D());

			return new Linestring(kept, ensureClockwise: true);
		}

		private static double DistanceSquaredXY([NotNull] Pnt3D p1, [NotNull] Pnt3D p2)
		{
			double dx = p1.X - p2.X;
			double dy = p1.Y - p2.Y;

			return dx * dx + dy * dy;
		}

		#endregion

		#region Buffer width / per-part distances

		private IList<double> GetPerPartOffsetDistances(int partCount)
		{
			EnsurePartWidths(partCount);

			double ratio = OffsetRatio;

			return _bufferWidths.Take(partCount).Select(w => w * ratio).ToList();
		}

		private void EnsurePartWidths(int partCount)
		{
			bool partAdded = partCount > _bufferWidths.Count;

			while (_bufferWidths.Count < partCount)
			{
				_bufferWidths.Add(_currentBufferWidth);
			}

			if (_bufferWidths.Count > partCount)
			{
				_bufferWidths.RemoveRange(partCount, _bufferWidths.Count - partCount);
			}

			if (partAdded)
			{
				// The part currently being drawn (the newest one) becomes the current part,
				// so width changes apply to it while the already drawn parts keep their
				// width. (The CTRL-measure line can still re-select an earlier part.)
				_currentPart = partCount - 1;
			}
			else
			{
				_currentPart = Math.Max(0, Math.Min(_currentPart, partCount - 1));
			}
		}

		private void SetCurrentBufferWidth(double width)
		{
			_currentBufferWidth = width;

			if (_currentPart >= 0 && _currentPart < _bufferWidths.Count)
			{
				_bufferWidths[_currentPart] = width;
			}

			// Reflect the change in the option so the dockpane spinner stays in sync (the
			// [1]/[2] keys and CTRL-measure change the width too, not only the spinner).
			// OptionsPropertyChanged short-circuits when the option already matches the live
			// width, so this does not recurse.
			if (Math.Abs(_bufferedLineToolOptions.BufferWidth - width) > double.Epsilon)
			{
				_bufferedLineToolOptions.BufferWidth = width;
			}
		}

		private void LogBufferWidth()
		{
			_msg.InfoFormat("Buffer width: {0:N2}", _currentBufferWidth);
		}

		// Suspends the edit sketch while CTRL is held for a measure. Three things happen:
		// the sketch is taken away from the sketch engine (no more last segment rubber
		// banding to the cursor), the sketch type is set to None so the engine stops
		// tracking the cursor at all, and snapping is switched off. The measure points come
		// from ClientToMapPoint and are never snapped, so any snap feedback shown during the
		// measure would be a promise the tool does not keep. All three are undone by
		// ResumeSketchAsync. The sketch is drawn as a static overlay (the same one the
		// intermittent SHIFT selection of the other sketch tools uses) so the user keeps
		// seeing the line that is being buffered.
		private async Task SuspendSketchAsync()
		{
			if (_sketchSuspended)
			{
				return;
			}

			MapView mapView = MapView.Active;

			if (mapView == null || ! IsInSketchMode)
			{
				return;
			}

			if (KeyboardUtils.IsShiftDown())
			{
				// SHIFT owns the sketch in this situation (intermittent selection): it has
				// already suspended it and will restore it on its own.
				return;
			}

			IntermediateSketchStates sketchStates = SketchStates;

			if (sketchStates is not { IsInIntermittentSelectionPhase: false })
			{
				// Either the history is not recording yet, or the sketch is already
				// suspended by the SHIFT intermittent selection: leave the sketch alone.
				return;
			}

			_sketchSuspended = true;
			_suspendedSketch = await GetCurrentSketchAsync();

			_snappingBeforeMeasure = UseSnapping;
			UseSnapping = false;

			// Make sure the history ends at the sketch as it really is now. The states are
			// recorded from SketchModifiedEvent, which fires on a background thread and can
			// run before the Z of the newest vertex has been assigned - SketchStack.TryPush
			// then drops that state, and the replay would hand back a sketch missing its
			// last vertex. Reading the sketch here (on the UI thread, after the Z has come
			// in) and pushing it closes that window. TryPush ignores it if it is already on
			// top of the stack.
			sketchStates.SketchStack.TryPush(_suspendedSketch);

			// Draws the sketch as an overlay and suspends recording, so that clearing the
			// sketch below is not recorded as the user emptying it.
			await sketchStates.StartIntermittentSelection();

			_sketchSuspensionLatch.Increment();
			try
			{
				await SetCurrentSketchAsync(null);
				SetSketchType(SketchGeometryType.None);
			}
			finally
			{
				_sketchSuspensionLatch.Decrement();
			}
		}

		// Hands the sketch suspended by <see cref="SuspendSketchAsync"/> back to the sketch
		// engine, removes its overlay and redraws the buffer preview (which was frozen for
		// the duration of the measure and may now use a newly measured width).
		private async Task ResumeSketchAsync()
		{
			if (! _sketchSuspended)
			{
				return;
			}

			// Cleared directly rather than through ResetSuspendedSketchState, which would
			// throw the recorded history away - the history is what is replayed below.
			_sketchSuspended = false;
			_suspendedSketch = null;

			bool sketchRestored = false;

			_sketchSuspensionLatch.Increment();
			try
			{
				UseSnapping = _snappingBeforeMeasure;

				SetSketchType(GetEditSketchGeometryType());

				EditingTemplate sketchTemplate = GetSketchTemplate();

				if (sketchTemplate != null)
				{
					await StartSketchAsync(sketchTemplate);
				}
				else
				{
					await StartSketchAsync();
				}

				// Replays the recorded sketch states one by one instead of putting the
				// sketch back in a single step, and clears the overlay. The replay is what
				// keeps CTRL+Z removing one vertex at a time after a measure: a single-step
				// restore collapses the entire sketch into one sketch operation, so the
				// first undo would drop all of it. Same mechanism the SHIFT intermittent
				// selection of the other sketch tools uses.
				IntermediateSketchStates sketchStates = SketchStates;

				if (sketchStates != null)
				{
					sketchRestored = await sketchStates.StopIntermittentSelectionAsync();
				}

				if (sketchRestored)
				{
					// The replay pushes a fresh series of sketch operations, which leaves
					// Pro's redo stack pointing at states from before the measure: redoing
					// one of those wipes the sketch instead of re-adding a vertex. SketchStack
					// has no redo support (see its TODO), so the only safe thing is to drop
					// the redo stack - CTRL+Y after a measure then does nothing rather than
					// something destructive. Undo keeps working, one vertex at a time.
					OperationManager operationManager =
						MapView.Active?.Map?.OperationManager;

					operationManager?.ClearRedoCategory("SketchOperations");
				}
			}
			catch (Exception ex)
			{
				_msg.Error($"Error restoring the sketch after measuring: {ex.Message}", ex);
			}
			finally
			{
				_sketchSuspensionLatch.Decrement();
			}

			if (sketchRestored)
			{
				// Refreshes the buffer preview and the in-progress flag for the sketch that
				// is back in the sketch engine.
				await OnSketchModifiedAsync();
			}
			else
			{
				await RefreshBufferPreviewAsync();
			}
		}

		// Drops a suspended sketch without handing it back (ESC while measuring). Only the
		// overlay and the sketch type are put back in order - the sketch itself is gone.
		private void AbandonSuspendedSketch()
		{
			if (! _sketchSuspended)
			{
				return;
			}

			ResetSuspendedSketchState();

			// The sketch type was set to None to suspend the sketch: put it back, or the
			// sketch engine stays disabled for the rest of the tool session.
			SetSketchType(GetEditSketchGeometryType());
		}

		// Aborts a measure that was started with a single CTRL-click but never completed,
		// removing the measure line overlay along with the pending start point. Without this
		// the overlay survives the restored sketch and keeps tracking the cursor.
		private void CancelPendingMeasure()
		{
			if (! _measuring)
			{
				return;
			}

			_measuring = false;
			_measureStart = null;

			DisposeMeasureOverlays();

			_msg.Info("Measuring canceled. The buffer width is unchanged.");
		}

		private void ResetSuspendedSketchState()
		{
			if (_sketchSuspended)
			{
				// Drops the overlay and the recorded history without replaying it. Only when
				// actually suspended: otherwise this would throw away the history that the
				// next measure (and the SHIFT intermittent selection) still needs.
				SketchStates?.ResetSketchStates();

				// The sketch is abandoned rather than restored (ESC, tool deactivation), so
				// ResumeSketchAsync never runs: put snapping back here.
				UseSnapping = _snappingBeforeMeasure;
			}

			_sketchSuspended = false;
			_suspendedSketch = null;
		}

		private async Task StopMeasureAsync([NotNull] MapPoint measureEnd)
		{
			_measuring = false;
			DisposeMeasureOverlays();

			MapPoint start = _measureStart;
			_measureStart = null;

			if (start == null)
			{
				return;
			}

			double measuredWidth = GeometryEngine.Instance.Distance(start, measureEnd);
			double tolerance = GetTolerance(measureEnd);

			if (measuredWidth <= tolerance)
			{
				_msg.Warn("Measured width is too small. Keeping the current buffer width.");
				return;
			}

			// If a sketch part is crossed by the measure line, make it the current part.
			// The sketch is suspended for the duration of the measure, so the stashed
			// geometry is the one to test against: the engine's current sketch is empty.
			Geometry sketch = _sketchSuspended
				                  ? _suspendedSketch
				                  : await GetCurrentSketchAsync();

			TrySelectPartByMeasureLine(start, measureEnd, sketch as Polyline);

			SetCurrentBufferWidth(measuredWidth);
			LogBufferWidth();

			// Ends the measure gesture: the circle may be drawn again, now at the width just
			// measured, even though CTRL is typically still held.
			_measuredWidthApplied = true;

			await RefreshBufferPreviewAsync();
		}

		private void TrySelectPartByMeasureLine([NotNull] MapPoint start,
		                                        [NotNull] MapPoint measureEnd,
		                                        [CanBeNull] Polyline sketch)
		{
			if (sketch == null || sketch.IsEmpty)
			{
				return;
			}

			try
			{
				Polyline measureLine = PolylineBuilderEx.CreatePolyline(
					new[] { start, measureEnd }, measureEnd.SpatialReference);

				for (int i = 0; i < sketch.PartCount; i++)
				{
					Polyline part = PolylineBuilderEx.CreatePolyline(sketch.Parts[i]);

					if (! GeometryEngine.Instance.Disjoint(part, measureLine))
					{
						_currentPart = i;
						return;
					}
				}
			}
			catch (Exception ex)
			{
				_msg.Debug("Could not select sketch part by measure line", ex);
			}
		}

		#endregion

		#region Overlay feedback

		private void EnsureSymbolsInitialized()
		{
			// The calculated buffer outline is shown as a thin violet-blue line
			// ("colorline"), matching the TopGIS buffer preview.
			_bufferSymbol ??= SymbolUtils.CreateLineSymbol(80, 80, 255, 1);

			_measureLineSymbol ??= SymbolUtils.CreateLineSymbol(0, 0, 0, 2);

			// The buffer-width indicator circle is drawn as a thin black line.
			_circleSymbol ??= SymbolUtils.CreateLineSymbol(0, 0, 0, 0.5);
		}

		private async Task RefreshBufferPreviewAsync()
		{
			if (MapView.Active == null)
			{
				return;
			}

			if (_sketchSuspended)
			{
				// The sketch is stashed for a CTRL measure and the engine's sketch is empty,
				// so there is nothing to build a preview from: leave the current preview on
				// screen frozen. ResumeSketchAsync redraws it once CTRL is released.
				return;
			}

			int ticket = Interlocked.Increment(ref _previewRefreshTicket);

			Geometry sketch = await GetCurrentSketchAsync();

			await QueuedTask.Run(() =>
			{
				if (ticket != Volatile.Read(ref _previewRefreshTicket))
				{
					// Superseded while this refresh was reading the sketch: the newer one
					// owns the overlays now, so do not touch them.
					return;
				}

				DisposeOverlays();

				if (sketch is not Polyline sketchLine || sketchLine.IsEmpty)
				{
					return;
				}

				Polygon buffer = BuildBufferPolygon(sketchLine);

				if (buffer == null || buffer.IsEmpty)
				{
					return;
				}

				Polyline previewOutline = GetBufferPreviewOutline(buffer, sketchLine);

				if (previewOutline == null || previewOutline.IsEmpty)
				{
					return;
				}

				EnsureSymbolsInitialized();

				_overlays.Add(MapView.Active.AddOverlay(
					              previewOutline, _bufferSymbol.MakeSymbolReference()));
			});
		}

		// The preview shows the buffer as its outline. For a one-sided buffer one edge of
		// the polygon coincides with the sketch line; that edge is removed so the sketch
		// line drawn by ArcGIS Pro stays visible.
		[CanBeNull]
		private Polyline GetBufferPreviewOutline([NotNull] Polygon buffer,
		                                         [NotNull] Polyline sketchLine)
		{
			if (GeometryEngine.Instance.Boundary(buffer) is not Polyline boundary ||
			    boundary.IsEmpty)
			{
				return null;
			}

			if (_bufferedLineToolOptions.BufferSide == BufferSide.Both)
			{
				return boundary;
			}

			var offsetOnly =
				GeometryEngine.Instance.Difference(boundary, sketchLine) as Polyline;

			return offsetOnly == null || offsetOnly.IsEmpty ? boundary : offsetOnly;
		}

		// Draws the transient feedback that follows the cursor: the measure line while a
		// measure is running, and the buffer-width circle around the cursor. Both are drawn
		// in one pass because they share _measureOverlays - drawing them through separate
		// entry points made each one dispose the other.
		// While the measure gesture is running the circle is suppressed: it would be showing
		// the width that is about to be replaced, next to a measure line that means
		// something else. The second click applies the measured width and ends the gesture,
		// so from then on the circle is back and shows the new width - CTRL may still be
		// held at that point.
		private void DrawCursorFeedback([NotNull] MapPoint cursor, bool measureGestureActive)
		{
			DisposeMeasureOverlays();

			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				return;
			}

			EnsureSymbolsInitialized();

			if (_measuring && _measureStart != null)
			{
				AddMeasureLineOverlay(mapView, _measureStart, cursor);
			}

			if (! measureGestureActive && _bufferedLineToolOptions.ShowBufferDistanceCircle)
			{
				AddBufferDistanceCircleOverlay(mapView, cursor);
			}
		}

		private void AddMeasureLineOverlay([NotNull] MapView mapView,
		                                   [NotNull] MapPoint start,
		                                   [NotNull] MapPoint end)
		{
			Polyline line = PolylineBuilderEx.CreatePolyline(
				new[] { start, end }, end.SpatialReference);

			_measureOverlays.Add(
				mapView.AddOverlay(line, _measureLineSymbol.MakeSymbolReference()));
		}

		private void AddBufferDistanceCircleOverlay([NotNull] MapView mapView,
		                                            [NotNull] MapPoint center)
		{
			if (_currentBufferWidth <= 0)
			{
				return;
			}

			// The buffer-width indicator circle is a flat 2D overlay and is only shown in a 2D
			// map. It does not render meaningfully in a stereo map (as in the legacy tool), so
			// it is skipped there.
			if (mapView.ViewingMode != MapViewingMode.Map)
			{
				return;
			}

			double radius = _currentBufferWidth * OffsetRatio;

			if (GeometryEngine.Instance.Buffer(center, radius) is not Polygon circle ||
			    circle.IsEmpty)
			{
				return;
			}

			Polyline outline = PolylineBuilderEx.CreatePolyline(circle);

			_measureOverlays.Add(
				mapView.AddOverlay(outline, _circleSymbol.MakeSymbolReference()));
		}

		private void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay.Dispose();
			}

			_overlays.Clear();
		}

		private void DisposeMeasureOverlays()
		{
			foreach (IDisposable overlay in _measureOverlays)
			{
				overlay.Dispose();
			}

			_measureOverlays.Clear();
		}

		#endregion

		#region Shift-select existing line

		// If a line is already selected when the tool is activated, load it into the sketch
		// (showing the buffer overlay) so the user can adjust the options and finish the
		// sketch when ready. For multiple selected lines, a hint that Shift-click deselects
		// until one line remains.
		private async Task TryBufferInitialSelectionAsync()
		{
			MapView mapView = MapView.Active;
			if (mapView == null)
			{
				return;
			}

			(int count, Polyline single) = await QueuedTaskUtils.Run(() =>
			{
				List<Feature> lines = GetSelectedPolylines(mapView);
				Polyline geometry = lines.Count == 1 ? lines[0].GetShape() as Polyline : null;
				return (lines.Count, geometry);
			});

			if (count == 1 && single != null)
			{
				await LoadLineIntoSketchAsync(single);
			}
			else if (count > 1)
			{
				_msg.Info(
					"Multiple lines are selected. Shift-click a selected line to deselect it; " +
					"the last remaining line is loaded into the sketch.");
			}
		}

		// Handles a Shift + left click: identifies the line under the cursor and either
		// deselects it (when narrowing down a multi-selection) or loads it into the sketch (with
		// buffer preview), leaving the user to finish the sketch (F2) to create the buffer.
		private Task HandleShiftSelectAsync(MapViewMouseButtonEventArgs args)
		{
			return QueuedTask.Run(async () =>
			{
				try
				{
					MapView mapView = MapView.Active;
					if (mapView == null)
					{
						return;
					}

					List<Feature> selectedBefore = GetSelectedPolylines(mapView);

					IPickableFeatureItem picked = await PickPolylineAsync(args.ClientPoint);
					if (picked?.Feature == null)
					{
						return;
					}

					bool pickedIsSelected =
						selectedBefore.Any(f => IsSameFeature(f, picked.Feature));

					if (selectedBefore.Count > 1 && pickedIsSelected)
					{
						// Deselect mode (narrowing down an activation multi-selection): remove
						// the clicked line; load the last remaining line into the sketch.
						SelectionUtils.SelectFeature(mapView, picked.Feature,
						                             SelectionCombinationMethod.Subtract);

						List<Feature> remaining = GetSelectedPolylines(mapView);
						if (remaining.Count == 1 && remaining[0].GetShape() is Polyline line)
						{
							await LoadLineIntoSketchAsync(line);
						}

						return;
					}

					// Select mode: load the clicked line into the sketch (with buffer preview) so
					// the user can adjust the options and finish the sketch (F2) to create the
					// buffer, instead of buffering and creating the feature immediately.
					if (picked.Feature.GetShape() is Polyline pickedLine)
					{
						await LoadLineIntoSketchAsync(pickedLine);
					}
				}
				catch (Exception ex)
				{
					_msg.Warn($"Error selecting line to buffer: {ex.Message}", ex);
				}
			});
		}

		// Identifies the single polyline feature under the given screen point, using the
		// picker to disambiguate when several lines overlap. Must run on the MCT.
		[ItemCanBeNull]
		private async Task<IPickableFeatureItem> PickPolylineAsync(
			Point clientPoint)
		{
			MapPoint mapPoint = MapUtils.ClientToMapPoint(MapView.Active, clientPoint);

			IPickerPrecedence precedence = await CreatePickerPrecedenceAsync(mapPoint);

			var featureFinder =
				new FeatureFinder(MapView.Active, TargetFeatureSelection.VisibleSelectableFeatures);

			List<FeatureSelectionBase> candidates =
				featureFinder.FindFeaturesByFeatureClass(
					             precedence.GetSelectionGeometry(),
					             layer => layer.ShapeType ==
					                      esriGeometryType.esriGeometryPolyline)
				             .ToList();

			if (candidates.Count == 0)
			{
				_msg.Info("No line feature found at the click point.");
				return null;
			}

			return await PickerUtils.PickSingleAsync(candidates, precedence);
		}

		[NotNull]
		private List<Feature> GetSelectedPolylines([NotNull] MapView mapView)
		{
			return GetApplicableSelectedFeatures(mapView)
			       .Where(f => f.GetShape()?.GeometryType == GeometryType.Polyline)
			       .ToList();
		}

		private static bool IsSameFeature([NotNull] Feature a, [NotNull] Feature b)
		{
			return a.GetObjectID() == b.GetObjectID() &&
			       a.GetTable().GetID() == b.GetTable().GetID();
		}

		// Loads an existing line into the edit sketch and shows the buffer overlay, but does
		// not create a feature: the user reviews it (and may change options) and finishes the
		// sketch when ready.
		private async Task LoadLineIntoSketchAsync([CanBeNull] Polyline line)
		{
			if (line == null || line.IsEmpty)
			{
				return;
			}

			// The line is now the sketch; drop the feature selection so it is not shown twice.
			await QueuedTask.Run(() =>
			{
				var map = MapView.Active?.Map;
				if (map != null)
				{
					SelectionUtils.ClearSelection(map);
				}
			});

			await SetCurrentSketchAsync(line);
			await RefreshBufferPreviewAsync();

			_msg.Info(
				"The selected line was loaded into the sketch. Adjust the options if needed, " +
				"then finish the sketch to create the buffer.");
		}

		#endregion

		#region State / helpers

		private void ResetBufferState()
		{
			_bufferWidths.Clear();
			_currentPart = 0;
			_measuring = false;
			_measureStart = null;
			_sketchInProgress = false;
			_ctrlUsedInChord = false;
			_measuredWidthApplied = false;

			ResetSuspendedSketchState();
		}

		// Clears all sketch feedback: the buffer state and both the buffer preview and the
		// measure/distance-circle overlays. Shared by every sketch-cancel path (ESC and the
		// sketch context menu's "Cancel").
		private void ClearFeedback()
		{
			ResetBufferState();
			DisposeOverlays();
			DisposeMeasureOverlays();
		}

		private void UpdateEnabled()
		{
			Enabled = IsApplicableTargetShapeType(GetTargetLayerShapeType());
		}

		/// <summary>
		/// Whether the tool is applicable to (and therefore enabled for) the given target
		/// shape type. The base tool creates polygons and is enabled for a polygon target;
		/// a derived tool (e.g. CreateWall) overrides this together with
		/// <see cref="CreateResultGeometry"/> to target a different geometry type such as
		/// multipatch.
		/// </summary>
		protected virtual bool IsApplicableTargetShapeType(esriGeometryType? shapeType)
		{
			return shapeType == esriGeometryType.esriGeometryPolygon;
		}

		protected virtual esriGeometryType? GetTargetLayerShapeType()
		{
			EditingTemplate editTemplate = EditingTemplate.Current;

			FeatureLayer currentTargetLayer = ToolUtils.CurrentTargetLayer(editTemplate);

			return currentTargetLayer?.ShapeType;
		}

		protected virtual FeatureClass GetCurrentTargetClass(out Subtype subtype)
		{
			try
			{
				return ToolUtils.GetCurrentTargetFeatureClass(true, out subtype);
			}
			catch (Exception ex)
			{
				_msg.Debug("Failed to get target feature class from template", ex);
				subtype = null;
				return null;
			}
		}

		protected virtual object GetFieldValue([NotNull] Field field,
		                                       [NotNull] FeatureClassDefinition featureClassDef,
		                                       [CanBeNull] Subtype subtype)
		{
			if (GdbPersistenceUtils.TryGetFieldValueFromTemplate(
				    field.Name, EditingTemplate.Current, out object result))
			{
				return result;
			}

			return field.GetDefaultValue(subtype);
		}

		private static void SelectCreatedFeatures([CanBeNull] MapView mapView,
		                                          [NotNull] IReadOnlyList<Feature> features)
		{
			if (mapView == null || features.Count == 0)
			{
				return;
			}

			bool isFirst = true;
			foreach (Feature feature in features)
			{
				SelectionUtils.SelectFeature(
					mapView, feature,
					isFirst ? SelectionCombinationMethod.New : SelectionCombinationMethod.Add);
				isFirst = false;
			}
		}

		private TOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			_settingsProvider =
				new OverridableSettingsProvider<TPartial>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			_settingsProvider.GetConfigurations(
				out TPartial localConfiguration,
				out TPartial centralConfiguration);

			TOptions result = CreateToolOptions(centralConfiguration, localConfiguration);

			// Redraw the buffer preview whenever any option changes (buffer width, side,
			// generalization, minimum segment length, ...). OptionsBase funnels every
			// centralizable setting's change into this single PropertyChanged event.
			result.PropertyChanged -= OptionsPropertyChanged;
			result.PropertyChanged += OptionsPropertyChanged;

			_msg.DebugStopTiming(watch, "Buffered line options initialized");

			string optionsMessage = result.GetLocalOverridesMessage();
			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return result;
		}

		private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			try
			{
				// Keep the live width in sync with the option so a changed width takes
				// effect during the current sketch and on the next one.
				double optionWidth = _bufferedLineToolOptions.BufferWidth;

				if (Math.Abs(optionWidth - _currentBufferWidth) > double.Epsilon)
				{
					SetCurrentBufferWidth(optionWidth);
				}

				QueuedTask.Run(() =>
				{
					// The buffer-width indicator circle is only (re)drawn on mouse move, so
					// clear any existing circle right away when the option is switched off.
					if (! _bufferedLineToolOptions.ShowBufferDistanceCircle && ! _measuring)
					{
						DisposeMeasureOverlays();
					}

					return RefreshBufferPreviewAsync();
				});
			}
			catch (Exception ex)
			{
				_msg.Warn($"Error applying option change: {ex.Message}", ex);
			}
		}

		#endregion
	}
}

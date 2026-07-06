using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
	/// (<see cref="GeomTopoOpUtils.GetBufferedLine(MultiLinestring,double,BufferSide,double,out string)"/>).
	/// </summary>
	[UsedImplicitly]
	public abstract class CreateBufferedLineToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const Key _keyIncreaseBufferWidth = Key.D2;
		private const Key _keyDecreaseBufferWidth = Key.D1;

		private const double _bufferWidthIncrement = 0.1;

		private readonly Latch _drawFeedbackLatch = new();

		// Per-part full buffer widths, aligned to the sketch parts. New parts inherit
		// the current width; the CTRL measure line and the 1/2 keys change the width of
		// the current part.
		private readonly List<double> _bufferWidths = new();
		private int _currentPart;
		private double _currentBufferWidth = CreateBufferedLineToolOptions.DefaultBufferWidth;

		// CTRL-drag measure line state (measures the full buffer width).
		private bool _measuring;
		private MapPoint _measureStart;

		private readonly List<IDisposable> _overlays = new();
		private readonly List<IDisposable> _measureOverlays = new();
		private CIMLineSymbol _bufferSymbol;
		private CIMLineSymbol _measureLineSymbol;
		private CIMLineSymbol _circleSymbol;

		private CreateBufferedLineToolOptions _bufferedLineToolOptions;
		private OverridableSettingsProvider<PartialCreateBufferedLineOptions> _settingsProvider;

		protected CreateBufferedLineToolBase()
		{
			UseSnapping = true;
			RequiresSelection = false;
			LogSketchVertexZs = false;
			ContextToolbarID = "";

			_bufferedLineToolOptions =
				new CreateBufferedLineToolOptions(null, new PartialCreateBufferedLineOptions());

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

		protected CreateBufferedLineToolOptions BufferedLineToolOptions =>
			_bufferedLineToolOptions;

		// The tool buffers a single existing line at a time (shift-select).
		protected override bool AllowMultiSelection(out string reason)
		{
			reason = "Only a single line can be buffered at a time.";
			return false;
		}

		// Only polylines can be shift-selected as the line to buffer.
		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
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
			return SketchGeometryType.Polygon;
		}

		protected override SketchGeometryType GetEditSketchGeometryType()
		{
			return SketchGeometryType.Line;
		}

		protected override void LogPromptForSelection() { }

		protected override void LogEnteringSketchMode()
		{
			_msg.InfoFormat(
				"Buffered line: draw the centre line to buffer, then finish the sketch. " +
				"Current buffer width: {0}. Hold [CTRL] to measure the width, press [1]/[2] " +
				"to de-/increase it, [O] for options.", _currentBufferWidth);
		}

		protected override bool DefaultSketchTypeOnFinishSketch => true;

		protected override async Task OnToolActivatingCoreAsync()
		{
			_bufferedLineToolOptions = InitializeOptions();
			_currentBufferWidth = _bufferedLineToolOptions.BufferWidth;
			await base.OnToolActivatingCoreAsync();

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
			return base.OnToolDeactivateCore(hasMapViewChanged);
		}

		protected override Task OnSketchPhaseStartedAsync()
		{
			ResetBufferState();
			return base.OnSketchPhaseStartedAsync();
		}

		protected override async Task<bool> OnSketchModifiedAsyncCore()
		{
			await RefreshBufferPreviewAsync();
			return await base.OnSketchModifiedAsyncCore();
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs args)
		{
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

		protected override Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
		{
			if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
			{
				// Show the measure cursor (cross + Measure overlay) while CTRL is held.
				SetToolCursor(MeasureCursors.GetCursor(GetSketchType(), shiftDown: false));
			}

			if (args.Key == _keyIncreaseBufferWidth)
			{
				SetCurrentBufferWidth(_currentBufferWidth + _bufferWidthIncrement);
				LogBufferWidth();
				QueuedTask.Run(RefreshBufferPreviewAsync);
				return Task.CompletedTask;
			}

			if (args.Key == _keyDecreaseBufferWidth)
			{
				double decreased = _currentBufferWidth - _bufferWidthIncrement;

				// Keep the current width if decreasing further would reach zero or below.
				if (decreased > 0)
				{
					SetCurrentBufferWidth(decreased);
					LogBufferWidth();
					QueuedTask.Run(RefreshBufferPreviewAsync);
				}
				else
				{
					_msg.InfoFormat(
						"Buffer width kept at {0:N2}: decreasing it by {1:N2} would reach " +
						"zero or below. Enter a smaller value in the options to go lower.",
						_currentBufferWidth, _bufferWidthIncrement);
				}

				return Task.CompletedTask;
			}

			return base.HandleKeyDownCoreAsync(args);
		}

		protected override Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
			{
				// CTRL released: restore the sketch (or, if SHIFT is held, selection) cursor.
				bool shiftDown = KeyboardUtils.IsShiftDown();
				SelectionCursors cursors = shiftDown ? FirstPhaseCursors : SketchCursors;
				SetToolCursor(cursors?.GetCursor(GetSketchType(), shiftDown));
			}

			return base.HandleKeyUpCoreAsync(args);
		}

		protected override void OnToolMouseDownCore(MapViewMouseButtonEventArgs args)
		{
			// Intercept CTRL-clicks (measure line) and SHIFT-clicks (select an existing line
			// to buffer): setting Handled prevents the sketch engine from adding a vertex and
			// routes the click to OnToolMouseDownCoreAsync instead.
			if (args.ChangedButton == MouseButton.Left &&
			    (KeyboardUtils.IsCtrlDown() || KeyboardUtils.IsShiftDown()))
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
				return HandleShiftSelectAsync(args);
			}

			if (! KeyboardUtils.IsCtrlDown())
			{
				return Task.CompletedTask;
			}

			return QueuedTask.Run(async () =>
			{
				MapPoint mapPoint = MapUtils.ClientToMapPoint(MapView.Active, args.ClientPoint);

				if (! _measuring)
				{
					_measureStart = mapPoint;
					_measuring = true;
					_msg.Info("Measuring buffer width: click the opposite side of the feature.");
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
				if (_drawFeedbackLatch.IsLatched)
				{
					return;
				}

				_drawFeedbackLatch.Increment();

				await QueuedTask.Run(() =>
				{
					MapPoint mapPoint =
						MapUtils.ClientToMapPoint(MapView.Active, args.ClientPoint);

					if (_measuring && _measureStart != null)
					{
						DrawMeasureLine(_measureStart, mapPoint);
					}
					else if (_bufferedLineToolOptions.ShowBufferDistanceCircle)
					{
						DrawBufferDistanceCircle(mapPoint);
					}
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

		protected override async Task HandleEscapeAsync()
		{
			ResetBufferState();
			DisposeOverlays();
			DisposeMeasureOverlays();

			_msg.Info("Draw the centre line of the buffer.");

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
			Polygon bufferPolygon = BuildBufferPolygon(line);

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

			Geometry newGeometry = CreateResultGeometry(bufferPolygon);

			if (newGeometry?.IsEmpty != false)
			{
				_msg.Warn("The buffer geometry is empty. No feature was created.");
				return false;
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
		private DockPaneCreateBufferedLineViewModelBase GetBufferedLineViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneCreateBufferedLineViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		#endregion

		#region Buffer geometry

		[CanBeNull]
		private Polygon BuildBufferPolygon([CanBeNull] Polyline sketchLine)
		{
			if (sketchLine == null || sketchLine.IsEmpty)
			{
				return null;
			}

			MultiPolycurve line = GeomConversionUtils.CreateMultiPolycurve(sketchLine);
			IList<Linestring> paths = line.GetLinestrings().ToList();

			IList<double> offsetDistances = GetPerPartOffsetDistances(paths.Count);

			double tolerance = GetTolerance(sketchLine);

			MultiLinestring buffer = GeomTopoOpUtils.GetBufferedLine(
				paths, offsetDistances, _bufferedLineToolOptions.BufferSide, tolerance,
				out string message);

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

		private static double GetTolerance([NotNull] Geometry geometry)
		{
			double tolerance = geometry.SpatialReference?.XYTolerance ?? 0;
			return tolerance > 0 ? tolerance : 0.001;
		}

		/// <summary>
		/// Turns the buffer polygon into the geometry that is stored in the target feature
		/// class. The base tool stores the polygon as-is; a derived tool (e.g. CreateWall)
		/// overrides this to return a different geometry such as a multipatch (see
		/// <see cref="ConvertToMultipatch"/>).
		/// </summary>
		[CanBeNull]
		protected virtual Geometry CreateResultGeometry([NotNull] Polygon bufferPolygon)
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
				SurfaceZsResult result = map.GetZsFromSurface(polygon);

				if (result.Status == SurfaceZsResultStatus.Ok &&
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

		[CanBeNull]
		protected static Multipatch ConvertToMultipatch([CanBeNull] Polygon polygon)
		{
			if (polygon == null || polygon.IsEmpty)
			{
				return null;
			}

			SpatialReference sr = polygon.SpatialReference;

			var mpBuilder = new MultipatchBuilderEx(sr);
			var patches = new List<Patch>();

			foreach (ReadOnlySegmentCollection ring in polygon.Parts)
			{
				var coords = new List<Coordinate3D>();

				foreach (Segment segment in ring)
				{
					coords.Add(segment.StartPoint.Coordinate3D);
				}

				if (coords.Count < 3)
				{
					continue;
				}

				Patch patch = mpBuilder.MakePatch(PatchType.FirstRing);
				patch.Coords = coords;
				patches.Add(patch);
			}

			if (patches.Count == 0)
			{
				return null;
			}

			mpBuilder.Patches = patches;

			return mpBuilder.ToGeometry();
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
			while (_bufferWidths.Count < partCount)
			{
				_bufferWidths.Add(_currentBufferWidth);
			}

			if (_bufferWidths.Count > partCount)
			{
				_bufferWidths.RemoveRange(partCount, _bufferWidths.Count - partCount);
			}

			_currentPart = Math.Max(0, Math.Min(_currentPart, partCount - 1));
		}

		private void SetCurrentBufferWidth(double width)
		{
			_currentBufferWidth = width;

			if (_currentPart >= 0 && _currentPart < _bufferWidths.Count)
			{
				_bufferWidths[_currentPart] = width;
			}
		}

		private void LogBufferWidth()
		{
			_msg.InfoFormat("Buffer width: {0:N2}", _currentBufferWidth);
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
			Geometry sketch = await GetCurrentSketchAsync();
			TrySelectPartByMeasureLine(start, measureEnd, sketch as Polyline);

			SetCurrentBufferWidth(measuredWidth);
			LogBufferWidth();

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

			Geometry sketch = await GetCurrentSketchAsync();

			await QueuedTask.Run(() =>
			{
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

		private void DrawMeasureLine([NotNull] MapPoint start, [NotNull] MapPoint end)
		{
			DisposeMeasureOverlays();

			MapView mapView = MapView.Active;
			if (mapView == null)
			{
				return;
			}

			EnsureSymbolsInitialized();

			Polyline line = PolylineBuilderEx.CreatePolyline(
				new[] { start, end }, end.SpatialReference);

			_measureOverlays.Add(
				mapView.AddOverlay(line, _measureLineSymbol.MakeSymbolReference()));
		}

		private void DrawBufferDistanceCircle([NotNull] MapPoint center)
		{
			DisposeMeasureOverlays();

			MapView mapView = MapView.Active;
			if (mapView == null || _currentBufferWidth <= 0)
			{
				return;
			}

			EnsureSymbolsInitialized();

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
		// deselects it (when narrowing down a multi-selection) or makes it the sole selection
		// and buffers it immediately.
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

					// Select mode: the clicked line becomes the sole selection and is buffered.
					SelectionUtils.SelectFeature(mapView, picked.Feature,
					                             SelectionCombinationMethod.New);

					if (picked.Feature.GetShape() is Polyline pickedLine)
					{
						await BufferExistingLineAsync(pickedLine);
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

		// Buffers a given existing line and creates the target feature.
		private async Task BufferExistingLineAsync([CanBeNull] Polyline line)
		{
			if (line == null || line.IsEmpty)
			{
				return;
			}

			MapView activeView = MapView.Active;

			await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					await SetCurrentSketchAsync(null);

					return await BufferLineAndCreateFeatureCoreAsync(
						       line, EditingTemplate.Current, activeView, null);
				}
				catch (Exception ex)
				{
					_msg.Error("Error buffering the selected line", ex);
					return false;
				}
				finally
				{
					DisposeOverlays();
					DisposeMeasureOverlays();
					ResetBufferState();

					// If Shift is still held, stay on the selection cursor so the next line
					// can be selected right away; otherwise return to the sketch crosshair.
					bool shiftDown = KeyboardUtils.IsShiftDown();
					SelectionCursors cursors = shiftDown ? FirstPhaseCursors : SketchCursors;
					SetToolCursor(cursors?.GetCursor(GetSketchType(), shiftDown));
				}
			});
		}

		#endregion

		#region State / helpers

		private void ResetBufferState()
		{
			_bufferWidths.Clear();
			_currentPart = 0;
			_measuring = false;
			_measureStart = null;
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

		private CreateBufferedLineToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			_settingsProvider =
				new OverridableSettingsProvider<PartialCreateBufferedLineOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			_settingsProvider.GetConfigurations(
				out PartialCreateBufferedLineOptions localConfiguration,
				out PartialCreateBufferedLineOptions centralConfiguration);

			var result =
				new CreateBufferedLineToolOptions(centralConfiguration, localConfiguration);

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

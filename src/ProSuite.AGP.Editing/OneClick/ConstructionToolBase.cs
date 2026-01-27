using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick;

public abstract class ConstructionToolBase : OneClickToolBase, ISymbolizedSketchTool
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const Key _keyFinishSketch = Key.F2;
	private const Key _keyRestorePrevious = Key.R;

	private Geometry _previousSketch;

	[CanBeNull] private IntermediateSketchStates _intermediateSketchStates;

	[CanBeNull] private ISymbolizedSketchType _symbolizedSketch;

	[CanBeNull] private MapPoint _lastLoggedVertex;
	private int _lastLoggedVertexIndex = -1;

	protected ConstructionToolBase()
	{
		ContextMenuID = "esri_editing_SketchContextMenu";

		IsSketchTool = true;

		// NOTE: If UseSelection is true, ins some cases the standard selection phase is
		// activated instead of our 'intermittent selection phase', which can result in a
		// mix-up of the selection phase and the edit sketch phase. Symptoms are
		// - InvalidCastExceptions when the edit sketch is a line (and the selection sketch is treated as edit sketch)
		// - Erase areas that are selection rectangles in the Erase tool.
		// Other problems include the standard selection cursor suddenly appearing.
		// Apparently, OnSelectionChangedAsync will not be called if UseSelection is true, but
		// this is not used in the OneClickToolBase hierarchy. Instead, we rely on the event
		// OnMapSelectionChangedAsync
		UseSelection = false;

		GeomIsSimpleAsFeature = false;

		HandledKeys.Add(_keyFinishSketch);
		HandledKeys.Add(_keyRestorePrevious);
	}

	[NotNull]
	protected virtual SelectionCursors FirstPhaseCursors => SelectionCursors;

	protected SelectionCursors SketchCursors { get; set; } =
		SelectionCursors.CreateFromCursor(Resources.EditSketchCrosshair, "Sketch");

	/// <summary>
	/// Whether the geometry sketch (as opposed to the selection sketch) is currently active
	/// and visible and can be manipulated by the user. This property is false during an
	/// intermediate selection (using shift key). <see cref="IsInSketchPhase"/> however
	/// will remain true in an intermediate selection.
	/// </summary>
	protected bool IsInSketchMode => IsInSketchPhase && ! ShiftPressedToSelect;

	/// <summary>
	/// Whether the user is indicating an intermittent selection by pressing the shift key
	/// (exclusively) during the sketch phase.
	/// </summary>
	protected bool ShiftPressedToSelect
	{
		get
		{
			bool selectingDuringSketchPhase =
				RequiresSelection &&
				KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
				KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			return selectingDuringSketchPhase;
		}
	}

	/// <summary>
	/// Property which indicates whether the tool is in the sketch phase. The difference to
	/// <see cref="IsInSketchMode"/> is that this property reflects the general phase of the
	/// tool. Even during the sketch phase an intermittent selection can be performed.
	/// In order to evaluate whether the actual sketch is currently visible and edited,
	/// use <see cref="IsInSketchMode"/>.
	/// </summary>
	protected bool IsInSketchPhase { get; set; }

	/// <summary>
	/// Whether this tool supports adding/removing from the current selection during the sketch
	/// phase.
	/// </summary>
	protected virtual bool SupportIntermediateSelectionPhase => AllowMultiSelection(out _);

	protected bool SupportRestoreLastSketch => true;

	protected bool LogSketchVertexZs { get; set; }

	#region MapTool overrides

	protected override async Task<bool> OnSketchModifiedAsync()
	{
		_msg.VerboseDebug(() => "OnSketchModifiedAsync()");

		if (LogSketchVertexZs && IsInSketchMode)
		{
			Geometry sketch = await GetCurrentSketchAsync();

			await LogLastSketchVertexZ(sketch);
		}

		// Does it make any difference what the return value is?
		if (_intermediateSketchStates?.IsReplayingSketches != true)
		{
			return await OnSketchModifiedAsyncCore();
		}

		return true;
	}

	/// <summary>
	/// Flag to indicate that currently the selection is changed by the <see
	/// cref="OnSketchCompleteCoreAsync"/> method and selection events should be ignored.
	/// </summary>
	protected bool IsCompletingEditSketch { get; set; }

	protected override async Task<bool> OnSketchCanceledAsync()
	{
		return await OnSketchCanceledAsyncCore();
	}

	#endregion

	#region OneClickToolBase overrides

	protected override async Task OnSelectionPhaseStartedAsync()
	{
		await base.OnSelectionPhaseStartedAsync();

		await QueuedTask.Run(async () =>
		{
			SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
			SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);

			if (_symbolizedSketch != null)
			{
				await _symbolizedSketch.ClearSketchSymbol();
			}
		});

		SelectionCursors = FirstPhaseCursors;
		SetToolCursor(SelectionCursors?.GetCursor(GetSketchType(), false));

		IsInSketchPhase = false;
	}

	protected override async Task OnToolActivatingCoreAsync()
	{
		SelectionCursors = FirstPhaseCursors;

		_msg.VerboseDebug(() => "OnToolActivatingCoreAsync");

		// NOTE: If it is really necessary to support immediate switching without changing the
		//       tool, we should request an OptionsChanged event;
		_symbolizedSketch = ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology
			                    ? GetSymbolizedSketch()
			                    : null;

		if (_symbolizedSketch != null)
		{
			await _symbolizedSketch.SetSketchAppearanceAsync();
		}
		else
		{
			SketchSymbol = null;
		}

		if (! RequiresSelection)
		{
			await StartSketchPhaseAsync();
		}
		else
		{
			_intermediateSketchStates = new IntermediateSketchStates();
			await _intermediateSketchStates.ActivateAsync();
		}
	}

	protected override async Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		await RememberSketchAsync();

		await base.OnToolDeactivateCoreAsync(hasMapViewChanged);
	}

	protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
	{
		// TODO: Move as much as possible to OnToolDeactivateCoreAsync
		_intermediateSketchStates?.Deactivate();

		IsInSketchPhase = false;

		_symbolizedSketch?.Dispose();
		_symbolizedSketch = null;

		_lastLoggedVertex = null;

		return base.OnToolDeactivateCore(hasMapViewChanged);
	}

	protected override Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
	{
		if (! RequiresSelection)
		{
			return Task.FromResult(false);
		}

		if (shiftDown && SupportIntermediateSelectionPhase)
		{
			return Task.FromResult(true);
		}

		bool result = ! IsInSketchPhase;

		return Task.FromResult(result);
	}

	protected override void LogUsingCurrentSelection()
	{
		// log is written in LogEnteringSketchMode
	}

	protected override async Task AfterSelectionAsync(IList<Feature> selectedFeatures,
	                                                  CancelableProgressor progressor)
	{
		// Release latch. The tool might not get the shift released when the shift key was
		// released while picker window was visible.
		if (_intermediateSketchStates?.IsInIntermittentSelectionPhase == true &&
		    ! KeyboardUtils.IsShiftDown())
		{
			if (await CanStartSketchPhaseAsync(selectedFeatures))
			{
				await StartSketchPhaseAsync();
				bool sketchRestored =
					await Assert.NotNull(_intermediateSketchStates)
					            .StopIntermittentSelectionAsync();

				if (sketchRestored)
				{
					await OnSketchModifiedAsync();
				}
			}

			return;
		}

		if (! IsInSketchPhase && await CanStartSketchPhaseAsync(selectedFeatures))
		{
			await StartSketchPhaseAsync();
		}
	}

	protected override async Task ShiftPressedCoreAsync(MapViewKeyEventArgs keyArgs)
	{
		if (! RequiresSelection)
		{
			return;
		}

		if (! AllowMultiSelection(out _))
		{
			return;
		}

		// This is called repeatedly while keeping the shift key pressed.
		// Return if intermittent selection phase is running.
		if (_intermediateSketchStates?.IsInIntermittentSelectionPhase == true)
		{
			return;
		}

		if (! KeyboardUtils.IsShiftDown())
		{
			// The key is not held down, but was pressed and released quickly.
			// In this situation the ShiftReleasedCoreAsync will typically not be called, so
			// it is better not to start the intermittent selection phase in the first place.
			return;
		}

		if (! ShiftPressedToSelect)
		{
			// Not exclusively shift, e.g. Ctrl + Shift + Y
			return;
		}

		if (! IsInSketchPhase)
		{
			// In the selection phase already, no intermittent selection needed.
			return;
		}

		try
		{
			// must not be null because of entrance guard RequiresSelection
			Assert.NotNull(_intermediateSketchStates);
			await _intermediateSketchStates.StartIntermittentSelection();

			// During start selection phase the edit sketch is cleared:
			SelectionCursors = FirstPhaseCursors;
			await StartSelectionPhaseAsync();
		}
		catch (Exception e)
		{
			_intermediateSketchStates?.ResetSketchStates();

			_msg.Warn(e.Message, e);
		}
	}

	protected override async Task ToggleSelectionSketchGeometryTypeAsync(
		SketchGeometryType toggleSketchType)
	{
		if (await IsInSelectionPhaseCoreAsync(KeyboardUtils.IsShiftDown()))
		{
			await base.ToggleSelectionSketchGeometryTypeAsync(toggleSketchType);
		}
		// Else do nothing: No selection sketch toggling in edit sketch phase.
	}

	protected override async Task ShiftReleasedCoreAsync()
	{
		if (! RequiresSelection)
		{
			return;
		}

		if (_intermediateSketchStates?.IsInIntermittentSelectionPhase != true)
		{
			// No intermediate sketch phase has been started
			return;
		}

		bool restartSketch = await QueuedTask.Run(() => CanUseSelection(ActiveMapView));

		if (restartSketch)
		{
			// The sketch phase must be restarted 
			await StartSketchPhaseAsync();

			if (_intermediateSketchStates != null)
			{
				bool sketchRestored =
					await _intermediateSketchStates.StopIntermittentSelectionAsync();

				if (sketchRestored)
				{
					await OnSketchModifiedAsync();
				}
			}
		}
		else
		{
			// No sketch phase restart (e.g. because selection gone), reset the sketch states
			_intermediateSketchStates?.ResetSketchStates();
		}
	}

	protected override async Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
	{
		_msg.VerboseDebug(() => $"HandleKeyUpCoreAsync ({Caption})");

		await base.HandleKeyUpCoreAsync(args);

		if (args.Key == _keyFinishSketch)
		{
			// #114: F2 has no effect unless another tool has been used before:
			Geometry currentSketch = await GetCurrentSketchAsync();

			if (CanFinishSketch(currentSketch))
			{
				await FinishSketchAsync();
			}
		}

		if (args.Key == _keyRestorePrevious)
		{
			await RestorePreviousSketchAsync();
		}
	}

	protected override async Task HandleEscapeAsync()
	{
		_msg.VerboseDebug(() => $"{nameof(HandleEscapeAsync)}");

		try
		{
			// In case we did not register the shift-up and the overlay is still lying around:
			_intermediateSketchStates?.ResetSketchStates();

			if (IsInSketchMode)
			{
				if (! RequiresSelection)
				{
					// remain in sketch mode, just reset the sketch
					await ResetSketchAsync();
				}
				else
				{
					// if sketch is empty, also remove selection and return to selection phase
					if (await HasSketchAsync())
					{
						await ResetSketchAsync();
					}
					else
					{
						await ClearSelectionAsync();
						await StartSelectionPhaseAsync();
					}
				}
			}
			else
			{
				await ClearSketchAsync();
				await ClearSelectionAsync();
			}
		}
		catch (Exception e)
		{
			ViewUtils.ShowError(e, _msg);
		}
	}

	protected override async Task<bool> OnMapSelectionChangedCoreAsync(
		MapSelectionChangedEventArgs args)
	{
		_msg.VerboseDebug(() => "OnMapSelectionChangedCoreAsync");

		if (ActiveMapView == null)
		{
			return false;
		}

		if (! RequiresSelection)
		{
			// No selection required, ignore selection changes
			return false;
		}

		if (IsCompletingEditSketch)
		{
			// The sketch phases should be managed by OnEditSketchCompleteCoreAsync()
			return false;
		}

		if (ShiftPressedToSelect)
		{
			// Intermittent selection phase: Selection change should be ignored
			// -> it will be evaluated in ShiftReleasedCoreAsync()
			return false;
		}

		// Short-cut to reduce unnecessary (and very frequent) selection evaluations
		// despite the selection not having changed (and not even being present).
		if (args.Selection.IsEmpty && IsInSketchPhase)
		{
			// Selection is required but removed: return to selection phase
			await StartSelectionPhaseAsync();
			return true;
		}

		Dictionary<BasicFeatureLayer, List<long>> dictionary =
			SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection);

		// TODO: Try to make CanUseSelection run outside QueuedTask.Run (as far as possible)
		bool canUseSelection = await QueuedTask.Run(() => CanUseSelection(dictionary));

		if (! canUseSelection)
		{
			if (IsInSketchPhase)
			{
				await StartSelectionPhaseAsync();
			}
		}
		else
		{
			if (! IsInSketchPhase)
			{
				// In selection phase and can use the selection -> start sketch phase
				await StartSketchPhaseAsync();
			}
			else
			{
				// In sketch phase and can use the selection -> remain in sketch phase, adapt
				// sketch symbol if needed:
				_symbolizedSketch?.SelectionChangedAsync(args);
			}
		}

		return true;
	}

	protected override async Task<bool> OnSketchCompleteCoreAsync(
		Geometry sketchGeometry,
		CancelableProgressor progressor)
	{
		_msg.VerboseDebug(() => "OnSketchCompleteCoreAsync");

		if (IsInSketchMode)
		{
			if (LogSketchVertexZs && sketchGeometry is MapPoint)
			{
				// NOTE: OnSketchModified for Point sketches is dysfunctional. The sketch is already empty!
				//       Work-around:
				_lastLoggedVertexIndex = -1;
				await LogLastSketchVertexZ(sketchGeometry);
			}

			// take snapshots
			//Todo: In Pro3 this.CurrentTemplate is null. Investigate further...
			EditingTemplate currentTemplate = EditingTemplate.Current;
			MapView activeView = ActiveMapView;

			try
			{
				IsCompletingEditSketch = true;

				await RememberSketchAsync(sketchGeometry);

				_lastLoggedVertex = null;

				return await OnEditSketchCompleteCoreAsync(
					       sketchGeometry, currentTemplate, activeView, progressor);
			}
			finally
			{
				IsCompletingEditSketch = false;
			}
		}

		return false;
	}

	#endregion

	protected virtual ISymbolizedSketchType GetSymbolizedSketch()
	{
		return null;
	}

	protected abstract SketchGeometryType GetEditSketchGeometryType();

	protected virtual Task<bool?> GetEditSketchHasZ()
	{
		return null;
	}

	/// <summary>
	/// The template that can optionally be used to set up the sketch properties, such as
	/// z/m-awareness. If the tool uses a template create a feature this method should return
	/// the relevant template.
	/// </summary>
	/// <returns></returns>
	protected virtual EditingTemplate GetSketchTemplate()
	{
		return null;
	}

	protected virtual Task<bool> OnSketchModifiedAsyncCore()
	{
		return Task.FromResult(true);
	}

	protected virtual Task<bool> OnSketchCanceledAsyncCore()
	{
		_lastLoggedVertexIndex = -1;
		return Task.FromResult(true);
	}

	protected virtual bool CanStartSketchPhaseCore(IList<Feature> selectedFeatures)
	{
		return true;
	}

	protected abstract void LogEnteringSketchMode();

	protected abstract Task<bool> OnEditSketchCompleteCoreAsync(
		Geometry sketchGeometry,
		EditingTemplate editTemplate,
		MapView activeView,
		CancelableProgressor cancelableProgressor = null);

	protected virtual CancelableProgressor GetSelectionProgressor()
	{
		return null;
	}

	protected virtual CancelableProgressor GetSketchCompleteProgressor()
	{
		return null;
	}

	protected virtual void OnSketchResetCore() { }

	protected async Task StartSketchPhaseAsync()
	{
		UseSnapping = true;
		CompleteSketchOnMouseUp = false;

		SetSketchType(GetEditSketchGeometryType());

		SelectionCursors = SketchCursors;
		SetToolCursor(SelectionCursors?.GetCursor(GetSketchType(), false));

		await QueuedTask.Run(ResetSketchVertexSymbolOptions);

		EditingTemplate relevanteTemplate = GetSketchTemplate();

		if (relevanteTemplate != null)
		{
			await StartSketchAsync(relevanteTemplate);
		}
		else
		{
			// TODO: Manually set up Z/M-awareness
			await StartSketchAsync();
		}

		LogEnteringSketchMode();

		IsInSketchPhase = true;

		if (_symbolizedSketch != null)
		{
			try
			{
				await QueuedTask.Run(async () =>
				{
					await _symbolizedSketch.SetSketchAppearanceAsync();
				});
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message, ex);
			}
		}

		_lastLoggedVertex = null;

		await OnSketchPhaseStartedAsync();
	}

	protected virtual Task OnSketchPhaseStartedAsync()
	{
		_lastLoggedVertexIndex = -1;
		return Task.CompletedTask;
	}

	private static bool CanFinishSketch(Geometry sketch)
	{
		if (sketch == null || sketch.IsEmpty)
		{
			return false;
		}

		if (sketch.GeometryType == GeometryType.Polygon)
		{
			return sketch.PointCount > 2;
		}

		if (sketch.GeometryType == GeometryType.Polyline)
		{
			return sketch.PointCount > 1;
		}

		return true;
	}

	private async Task<bool> CanStartSketchPhaseAsync(IList<Feature> selectedFeatures)
	{
		if (ShiftPressedToSelect)
		{
			return false;
		}

		if (! await ViewUtils.TryAsync(QueuedTask.Run(() => CanUseSelection(ActiveMapView)),
		                               _msg,
		                               suppressErrorMessageBox: true))
		{
			return false;
		}

		return CanStartSketchPhaseCore(selectedFeatures);
	}

	protected async Task ResetSketchAsync()
	{
		await RememberSketchAsync();

		await ClearSketchAsync();

		await OnSketchModifiedAsyncCore();

		OnSketchResetCore();

		await StartSketchAsync();

		_lastLoggedVertex = null;
	}

	protected async Task RememberSketchAsync(Geometry knownSketch = null)
	{
		if (! SupportRestoreLastSketch)
		{
			return;
		}

		var sketch = knownSketch ?? await GetCurrentSketchAsync();

		if (sketch is { IsEmpty: false })
		{
			_previousSketch = sketch;
		}
	}

	private async Task RestorePreviousSketchAsync()
	{
		if (! SupportRestoreLastSketch)
		{
			return;
		}

		if (_previousSketch == null || _previousSketch.IsEmpty)
		{
			_msg.Warn("There is no previous sketch to restore.");

			return;
		}

		try
		{
			if (! IsInSketchMode)
			{
				// If a non-rectangular sketch is set while SketchType is rectangle (or probably generally the wrong type)
				// sketching is not possible any more and the application appears hanging

				// Try start sketch mode:
				await QueuedTask.Run(async () =>
				{
					var mapView = ActiveMapView; // TODO should be passed in from outside QTR

					IList<Feature> selection =
						GetApplicableSelectedFeatures(mapView).ToList();

					if (CanUseSelection(mapView))
					{
						await AfterSelectionAsync(selection, null);
					}
				});
			}

			if (IsInSketchMode)
			{
				_lastLoggedVertexIndex = -1;
				await SetCurrentSketchAsync(_previousSketch);
			}
			else
			{
				_msg.Warn("Sketch cannot be restored in selection phase. " +
				          "Please try again in the sketch phase.");
			}
		}
		catch (Exception e)
		{
			throw new ApplicationException("Error restoring the previous sketch", e);
		}
	}

	private async Task LogLastSketchVertexZ([NotNull] Geometry sketch)
	{
		if (! sketch.HasZ)
		{
			return;
		}

		if (_intermediateSketchStates?.IsReplayingSketches == true)
		{
			return;
		}

		if (await GetEditSketchHasZ() != true)
		{
			return;
		}

		int currentLastIndex;
		if (sketch is Polygon)
		{
			currentLastIndex = sketch.PointCount - 2;
		}
		else
		{
			currentLastIndex = sketch.PointCount - 1;
		}

		_msg.DebugFormat(
			"Vertex added [{0}], currentLastIndex[{1}], _lastloggedVertexIndex[{2}]",
			sketch.PointCount, currentLastIndex, _lastLoggedVertexIndex);

		//Undo of last vertex
		if (currentLastIndex <= _lastLoggedVertexIndex)
		{
			_lastLoggedVertexIndex = currentLastIndex;
			return;
		}

		//Sketch restore with R
		if (currentLastIndex > 0 && _lastLoggedVertexIndex == -1)
		{
			_lastLoggedVertexIndex = currentLastIndex;
			return;
		}

		await QueuedTask.Run(() =>
		{
			MapPoint lastPoint = GetLastPoint(sketch);

			if (lastPoint == null)
			{
				_msg.VerboseDebug(() => "Last point is null");
				return;
			}

			bool hasSurfaceAtLocation = HasSurfaceAtLocation(lastPoint) &&
			                            ActiveMapView.ViewingMode == MapViewingMode.Map;

			if (double.IsNaN(lastPoint.Z) && hasSurfaceAtLocation)
			{
				_msg.VerboseDebug(() =>
					                  $"Last point has NaN Z but surface exists at ({lastPoint.X:F3} / {lastPoint.Y:F3})");
				return;
			}

			//NOTE: OnSketchModified for Point sketches is dysfunctional. Workaround:
			if (GetSketchType() == SketchGeometryType.Point && lastPoint.Z == 0.00)
			{
				_msg.VerboseDebug(() =>
					                  $"Point sketch with Z=0.00 at ({lastPoint.X:F3} / {lastPoint.Y:F3})");
				return;
			}

			if (double.IsNaN(lastPoint.Z) && ! hasSurfaceAtLocation)
			{
				_msg.InfoFormat("Vertex added, no surface at location, Z=0.00");
				_lastLoggedVertexIndex = currentLastIndex;
				return;
			}

			_msg.InfoFormat("Vertex added, Z={0:N2}", lastPoint.Z);
			_lastLoggedVertexIndex = currentLastIndex;
		});
	}

	private static MapPoint GetLastPoint(Geometry sketch)
	{
		MapPoint lastPoint = null;

		if (sketch is Multipart multipart)
		{
			ReadOnlyPointCollection points = multipart.Points;

			int pointNumFromEnd = 1;
			if (sketch is Polygon)
			{
				// In a polygon sketch the last point is the same as the first, take the second-last:
				pointNumFromEnd = 2;
			}

			if (points.Count > 0)
			{
				lastPoint = points[points.Count - pointNumFromEnd];
			}
		}
		else if (sketch is MapPoint point)
		{
			lastPoint = point;
		}
		else if (sketch is Multipoint multipoint)
		{
			ReadOnlyPointCollection points = multipoint.Points;

			if (points.Count > 0)
			{
				lastPoint = points[points.Count - 1];
			}
		}

		return lastPoint;
	}

	/// <summary>
	/// Checks if there is elevation surface data available at the specified location in the active map view.
	/// </summary>
	/// <param name="point">The point to check for surface availability</param>
	/// <returns>True if surface data is available at the location, false otherwise</returns>
	private bool HasSurfaceAtLocation([NotNull] MapPoint point)
	{
		Map currentMap = ActiveMapView?.Map;

		if (currentMap == null)
		{
			return false;
		}

		// Check if the map has elevation surfaces
		IReadOnlyList<ElevationSurfaceLayer> elevationSurfaceLayers =
			currentMap.GetElevationSurfaceLayers();

		if (elevationSurfaceLayers == null || elevationSurfaceLayers.Count == 0)
		{
			return false;
		}

		try
		{
			// Try to get Z values from the map's elevation surfaces
			SurfaceZsResult result = currentMap.GetZsFromSurface(point);

			// Surface data is available if we got a valid result with geometry
			return result?.Geometry != null && result.Status == SurfaceZsResultStatus.Ok;
		}
		catch (Exception ex)
		{
			_msg.VerboseDebug(() =>
				                  $"Error checking surface at location {point.X:F3}, {point.Y:F3}: {ex.Message}");
			return false;
		}
	}

	public virtual Task<bool> CanSetConstructionSketchSymbol(GeometryType geometryType)
	{
		return Task.FromResult(true);
	}

	void ISymbolizedSketchTool.SetSketchSymbol(CIMSymbolReference symbolReference)
	{
		SketchSymbol = symbolReference;
	}

	public bool CanSelectFromLayer(Layer layer)
	{
		return CanSelectFromLayer(layer, null);
	}

	public bool CanUseSelection(Dictionary<BasicFeatureLayer, List<long>> selectionByLayer)
	{
		return CanUseSelection(selectionByLayer, null);
	}
}

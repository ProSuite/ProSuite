using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick;

// TODO: get rid off NotificationCollection
public abstract class ToolBase : MapToolBase, ISymbolizedSketchTool
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const Key _keyPolygonDraw = Key.P;
	private const Key _keyLassoDraw = Key.L;
	private const Key _keyFinishSketch = Key.F2;

	private readonly Latch _toolActivateLatch = new();
	private readonly Latch _latch = new();

	[CanBeNull] private SymbolizedSketchTypeBasedOnSelection _symbolizedSketch;
	private SelectionCursors _selectionCursors;

	// ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
	protected ToolBase()
	{
		ContextMenuID = "esri_mapping_selection2DContextMenu";

		HandledKeys.Add(_keyLassoDraw);
		HandledKeys.Add(_keyPolygonDraw);
		HandledKeys.Add(_keyFinishSketch);

		// needed to call OnSelectionChangedAsync
		UseSelection = true;
		UseSnapping = false;
		IsSketchTool = true;
		FireSketchEvents = true;
		IsWYSIWYG = true;

		ConstructionCursorCore = ToolUtils.GetCursor(Resources.EditSketchCrosshair);
	}

	[NotNull]
	protected virtual Cursor ConstructionCursorCore { get; }

	protected virtual bool AllowNoSelection => false;

	/// Whether the required selection can only contain selectable features.
	protected bool SelectOnlySelectableFeatures { get; init; } = true;

	protected bool CanSelectOnlyEditFeatures { get; init; } = true;

	#region abstract

	protected abstract void LogPromptForSelection();

	protected abstract SelectionSettings GetSelectionSettings();

	protected abstract bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer layer);

	[CanBeNull]
	protected abstract SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch();

	protected virtual SketchGeometryType GetSelectionSketchGeometryType()
	{
		return SketchGeometryType.Rectangle;
	}

	#endregion

	#region overrides

	protected sealed override async Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		try
		{
			// After on tool activate OnSelectionChangedAsync is fired. But ToolBase just needs
			// OnSelectionChangedAsync when selection is cleared or to react when a selection
			// is made but not by the tool itself, e.g. select row in table. In all other cases
			// we want OnSelectionChangedAsync to be latched. Especially when the tool is activated.
			_toolActivateLatch.Increment();

			_symbolizedSketch = GetSymbolizedSketch();

			_selectionCursors ??= GetSelectionCursors();
			_selectionCursors.DefaultSelectionSketchType = GetSelectionSketchGeometryType();

			await OnToolActivateCoreAsync(hasMapViewChanged);

			if (MapUtils.HasSelection(ActiveMapView))
			{
				if (_symbolizedSketch != null)
				{
					await QueuedTask.Run(
						() => _symbolizedSketch?.SetSketchAppearanceAsync());
				}

				bool selectionProcessed = await ProcessSelectionAsync();

				if (selectionProcessed)
				{
					await StartConstructionPhaseAsync();
				}
				else
				{
					await StartSelectionPhaseAsync();
				}
			}
			else
			{
				await StartSelectionPhaseAsync();
			}
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	/// <summary>
	/// Create the cursor bitmaps to be used in the selection phase of the tool.
	/// The default is the cross with the selection icon.
	/// </summary>
	/// <returns></returns>
	protected virtual SelectionCursors GetSelectionCursors()
	{
		return SelectionCursors.CreateCrossCursors(Resources.SelectOverlay);
	}

	protected abstract bool DefaultSketchTypeOnFinishSketch { get; }

	protected sealed override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		_msg.Debug($"Deactivate {Caption}");

		_symbolizedSketch?.Dispose();

		await ViewUtils.TryAsync(OnToolDeactivateCoreAsync(hasMapViewChanged), _msg);
	}

	protected sealed override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		await ViewUtils.TryAsync(OnSelectionChangedCoreAsync(args), _msg);
	}

	/// <summary>
	/// Is on the GUI thread.
	/// </summary>
	protected sealed override async Task<bool> OnSketchModifiedAsync()
	{
		return await ViewUtils.TryAsync(OnSketchModifiedCoreAsync(), _msg);
	}

	protected sealed override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		return await ViewUtils.TryAsync(OnSketchCompleteCoreAsync(geometry), _msg);
	}

	protected sealed override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		try
		{
			if (args.Key == _keyFinishSketch)
			{
				await FinishSketchAsync();
			}

			if (KeyboardUtils.IsShiftKey(args.Key))
			{
				await ShiftPressedAsync();
			}

			if (args.Key == Key.Escape)
			{
				if (await HasSketchAsync())
				{
					await QueuedTask.Run(() =>
					{
						// For some unknown reason, the SketchSymbol is only correctly
						// updated after a call to ActiveMapView.ClearSketchAsync in
						// a QueuedTask since ArcGis Pro 3.4
						ActiveMapView.ClearSketchAsync();
					});
				}
				else
				{
					await HandleEscapeAsync();

					await StartSelectionPhaseAsync();
				}
			}

			await ViewUtils.TryAsync(HandleKeyDownCoreAsync(args), _msg);
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override async Task ShiftPressedAsync()
	{
		if (! InConstructionPhase())
		{
			SetToolCursor(_selectionCursors.GetCursor(GetSketchType(), shiftDown: true));
		}

		await Task.CompletedTask;
	}

	protected sealed override async Task HandleKeyUpAsync(MapViewKeyEventArgs args)
	{
		try
		{
			if (KeyboardUtils.IsShiftKey(args.Key))
			{
				await ShiftReleasedAsync();
			}

			if (! InConstructionPhase())
			{
				if (args.Key == _keyPolygonDraw)
				{
					await SetupPolygonSketchAsync();
				}

				if (args.Key == _keyLassoDraw)
				{
					await SetupLassoSketchAsync();
				}
			}

			await HandleKeyUpCoreAsync(args);
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	private async Task ShiftReleasedAsync()
	{
		if (! InConstructionPhase())
		{
			SetToolCursor(_selectionCursors.GetCursor(GetSketchType(), shiftDown: false));
		}

		await ShiftReleasedCoreAsync();
	}

	protected virtual Task ShiftReleasedCoreAsync()
	{
		return Task.CompletedTask;
	}

	protected override void OnToolMouseMove(MapViewMouseEventArgs args)
	{
		CurrentMousePosition = args.ClientPoint;

		base.OnToolMouseMove(args);
	}

	protected override async Task OnToolDoubleClickCoreAsync(MapViewMouseButtonEventArgs args)
	{
		// if in selection phase
		if (GetSketchType() == SketchGeometryType.Polygon && ! InConstructionPhase())
		{
			await FinishSketchAsync();
		}
	}

	#endregion

	#region tool

	protected override Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		return Task.CompletedTask;
	}

	protected override Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		return Task.CompletedTask;
	}

	#endregion

	#region sketch

	public bool CanSelectFromLayer(Layer layer)
	{
		return CanSelectFromLayer(layer as BasicFeatureLayer);
	}

	public bool CanUseSelection(Dictionary<BasicFeatureLayer, List<long>> selectionByLayer)
	{
		return CanUseSelection(selectionByLayer, null);
	}

	public virtual Task<bool> CanSetConstructionSketchSymbol(GeometryType geometryType)
	{
		return Task.FromResult(true);
	}

	public void SetSketchSymbol(CIMSymbolReference symbolReference)
	{
		SketchSymbol = symbolReference;
	}

	public void SetSketchType(SketchGeometryType? sketchType)
	{
		SetSketchTypeCore(sketchType);
	}

	protected virtual void SetSketchTypeCore(SketchGeometryType? sketchType)
	{
		SketchType = sketchType;
	}

	public SketchGeometryType? GetSketchType()
	{
		return SketchType;
	}

	public void SetTransparentVertexSymbol(VertexSymbolType vertexSymbolType)
	{
		var options = new VertexSymbolOptions(vertexSymbolType)
		              {
			              Color = ColorUtils.CreateRGB(0, 0, 0, 0),
			              OutlineColor = ColorUtils.CreateRGB(0, 0, 0, 0)
		              };
		SetSketchVertexSymbolOptions(vertexSymbolType, options);
	}

	protected virtual Task<bool> OnSketchModifiedCoreAsync()
	{
		return Task.FromResult(true);
	}

	private async Task<bool> OnSketchCompleteCoreAsync(Geometry geometry)
	{
		try
		{
			if (geometry == null || geometry.IsEmpty)
			{
				_msg.Debug("Sketch is null or empty");
				return await Task.FromResult(false);
			}

			if (MapUtils.HasSelection(ActiveMapView) && InConstructionPhase())
			{
				Dictionary<BasicFeatureLayer, List<long>> selection =
					await GetApplicableSelection<BasicFeatureLayer>();

				if (CanUseSelection(selection, new NotificationCollection()))
				{
					using var source = GetProgressorSource();
					var progressor = source?.Progressor;

					bool constructionProcessed =
						await OnConstructionSketchCompleteAsync(geometry, selection, progressor);

					if (constructionProcessed)
					{
						await StartSelectionPhaseAsync();
						return true; // sketchCompleteEventHandled = true;
					}

					await StartConstructionPhaseAsync();
					return true; // sketchCompleteEventHandled = true;
				}
			}

			try
			{
				// We don't want OnSelectionChangedCoreAsync to react on our selection
				_latch.Increment();
				bool validSelection = await OnSelectionSketchCompleteAsync(geometry);

				if (validSelection)
				{
					// OnSketchCompleteAsync is on the GUI thread. Here is the right place to change the cursor.
					// OnSelectionCompleteAsync is on QueuedTask/MCT thread. Changing cursor there doesn't immediately
					// change it. You would have to move the mouse to trigger cursor change.
					//StartContructionPhase();

					bool selectionProcessed = await ProcessSelectionAsync();

					if (selectionProcessed)
					{
						await StartConstructionPhaseAsync();
					}
					else
					{
						await StartSelectionPhaseAsync();
					}
				}
			}
			finally
			{
				_latch.Decrement();
			}
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}

		return true; // sketchCompleteEventHandled = true;
	}

	/// <summary>
	/// Is on the GUI thread.
	/// </summary>
	/// <returns><b>true</b>: valid selection,
	/// <b>false</b>: no selection.</returns>
	protected virtual async Task<bool> OnSelectionSketchCompleteAsync([NotNull] Geometry geometry)
	{
		try
		{
			using IPickerPrecedence precedence = CreatePickerPrecedence(geometry);

			await QueuedTaskUtils.Run(async () =>
			{
				var candidates =
					FindFeatureSelection(precedence.GetSelectionGeometry(),
					                     precedence.SpatialRelationship).ToList();

				List<IPickableItem> items = await PickerUtils.GetItemsAsync(candidates, precedence);

				PickerUtils.Select(items, precedence.SelectionCombinationMethod);
			});
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}

		return await OnSelectionSketchCompleteCoreAsync(geometry);
	}

	protected virtual Task<bool> OnSelectionSketchCompleteCoreAsync(Geometry geometry)
	{
		return Task.FromResult(MapUtils.HasSelection(ActiveMapView));
	}

	[NotNull]
	protected virtual IPickerPrecedence CreatePickerPrecedence([NotNull] Geometry sketchGeometry)
	{
		return new PickerPrecedence(sketchGeometry,
		                            GetSelectionSettings().SelectionTolerancePixels,
		                            ActiveMapView.ClientToScreen(CurrentMousePosition));
	}

	/// <summary>Is on GUI thread. Use QueuedTask.</summary>
	/// <returns><b>true</b>: construction finished and start selection phase,
	/// <b>false</b>: stay in construction phase.</returns>
	protected virtual Task<bool> OnConstructionSketchCompleteAsync([NotNull] Geometry geometry,
		IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		CancelableProgressor progressor)
	{
		return Task.FromResult(true);
	}

	private async Task<bool> HasSketchAsync()
	{
		Geometry currentSketch = await GetCurrentSketchAsync();

		return currentSketch?.IsEmpty == false;
	}

	private async Task SetupSelectionSketchAsync()
	{
		_symbolizedSketch?.ClearSketchSymbol();
		await ResetSelectionSketchTypeAsync(_selectionCursors);
	}

	protected virtual async Task SetupPolygonSketchAsync()
	{
		await ToggleSelectionSketchGeometryTypeAsync(SketchGeometryType.Polygon, _selectionCursors);
	}

	protected virtual async Task SetupLassoSketchAsync()
	{
		await ToggleSelectionSketchGeometryTypeAsync(SketchGeometryType.Lasso, _selectionCursors);
	}

	protected virtual async Task ToggleSelectionSketchGeometryTypeAsync(
		SketchGeometryType toggleSketchType,
		[CanBeNull] SelectionCursors selectionCursors = null)
	{
		selectionCursors ??= _selectionCursors;

		SketchGeometryType? newSketchGeometryType =
			ToolUtils.ToggleSketchGeometryType(toggleSketchType, SketchType,
			                                   selectionCursors.DefaultSelectionSketchType);

		await SetSelectionSketchTypeAsync(newSketchGeometryType, selectionCursors);
	}

	protected async Task ResetSelectionSketchTypeAsync(SelectionCursors selectionCursors)
	{
		SketchGeometryType? previousSketchTypeToUse = null;

		SketchGeometryType? previousSketchType = selectionCursors.PreviousSelectionSketchType;

		if (! DefaultSketchTypeOnFinishSketch &&
		    previousSketchType is SketchGeometryType.Polygon or SketchGeometryType.Lasso)
		{
			previousSketchTypeToUse = previousSketchType;
		}

		SketchGeometryType? startSketchType =
			selectionCursors.GetStartSelectionSketchGeometryType(previousSketchTypeToUse);

		await SetSelectionSketchTypeAsync(startSketchType, selectionCursors);
	}

	protected async Task SetSelectionSketchTypeAsync(
		SketchGeometryType? newGeometryType,
		[CanBeNull] SelectionCursors selectionCursors = null)
	{
		if (SketchType != newGeometryType)
		{
			SketchType = newGeometryType;
			_msg.Debug($"{Caption} changed sketch type to {newGeometryType}");
		}

		selectionCursors ??= _selectionCursors;

		Cursor newCursor =
			selectionCursors.GetCursor(newGeometryType, KeyboardUtils.IsShiftDown());

		SetToolCursor(newCursor);

		if (newGeometryType == SketchGeometryType.Polygon)
		{
			// If using a polygon sketch as a selection sketch, the vertices should be invisible:
			await QueuedTask.Run(() =>
			{
				SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
				SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
			});
		}

		// Remember the sketch type (consider local field, using last sketch type across tool phases):
		selectionCursors.PreviousSelectionSketchType = newGeometryType;
	}

	#endregion

	#region selection

	protected async Task<bool> ProcessSelectionAsync()
	{
		using var source = GetProgressorSource();
		var progressor = source?.Progressor;

		Task<bool> task = QueuedTaskUtils.Run(() =>
		{
			Dictionary<BasicFeatureLayer, List<long>> dictionary =
				SelectionUtils.GetSelection<BasicFeatureLayer>(ActiveMapView.Map);

			return ProcessSelectionAsync(dictionary);
		}, progressor);

		return await ViewUtils.TryAsync(task, _msg);
	}

	private async Task<bool> ProcessSelectionAsync(
		[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer)
	{
		using var source = GetProgressorSource();
		var progressor = source?.Progressor;

		if (! CanUseSelection(selectionByLayer, new NotificationCollection()))
		{
			return false; // startContructionPhase = false
		}

		Dictionary<BasicFeatureLayer, List<Feature>> applicableSelection =
			SelectionUtils.GetApplicableSelectedFeatures(selectionByLayer, CanSelectFromLayer);

		if (applicableSelection.Count == 0)
		{
			return false; // startContructionPhase = false
		}

		_symbolizedSketch?.SetSketchType(applicableSelection.Keys.First());

		return await ProcessSelectionCoreAsync(applicableSelection, progressor);
	}

	/// <returns><b>true</b>: selection successfully processed and start
	/// construction phase, <b>false</b>: stay in selection phase.</returns>
	protected virtual Task<bool> ProcessSelectionCoreAsync(
		[NotNull] IDictionary<BasicFeatureLayer, List<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		return Task.FromResult(true);
	}

	protected virtual async Task OnSelectionChangedCoreAsync(
		[NotNull] MapSelectionChangedEventArgs args)
	{
		if (_toolActivateLatch.IsLatched)
		{
			_toolActivateLatch.Decrement();
		}

		if (args.Selection.Count == 0)
		{
			LogPromptForSelection();
			await StartSelectionPhaseAsync();
			await QueuedTask.Run(() =>
			{
				// For some unknown reason, the SketchSymbol is only correctly
				// updated after a call to ActiveMapView.ClearSketchAsync in
				// a QueuedTask since ArcGis Pro 3.4
				ActiveMapView.ClearSketchAsync();
			});
		}
		else if (args.Selection.Count > 0)
		{
			// Process selection made not by this tool, e.g. select row in table, etc.
			// Do not react on selection made by this tool.
			if (_latch.IsLatched)
			{
				return;
			}

			Task<bool> task =
				ProcessSelectionAsync(
					SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection));
			bool selectionProcessed = await ViewUtils.TryAsync(task, _msg);

			if (selectionProcessed)
			{
				await StartConstructionPhaseAsync();
			}
			else
			{
				await StartSelectionPhaseAsync();
			}
		}
	}

	protected abstract bool AllowMultiSelection(out string reason);

	protected virtual bool CanUseSelectionCore(
		[NotNull] IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications = null)
	{
		return true;
	}

	protected virtual bool CanSelectGeometryType(GeometryType geometryType)
	{
		return true;
	}

	protected IEnumerable<FeatureSelectionBase> FindFeatureSelection(
		[NotNull] Geometry geometry,
		SpatialRelationship spatialRelationship = SpatialRelationship.Intersects,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		var featureFinder = new FeatureFinder(ActiveMapView)
		                    {
			                    SpatialRelationship = spatialRelationship,
			                    DelayFeatureFetching = true
		                    };

		const Predicate<Feature> featurePredicate = null;
		return featureFinder.FindFeaturesByLayer(geometry, fl => CanSelectFromLayer(fl),
		                                         featurePredicate, progressor);
	}

	private async Task<Dictionary<T, List<long>>> GetApplicableSelection<T>()
		where T : BasicFeatureLayer
	{
		// todo daro rename, revise method

		Dictionary<T, List<long>> selectionByLayer =
			await QueuedTask.Run(() => SelectionUtils.GetSelection<T>(ActiveMapView.Map));

		Dictionary<T, List<long>> result =
			new Dictionary<T, List<long>>(selectionByLayer.Count);

		var notifications = new NotificationCollection();

		foreach (KeyValuePair<T, List<long>> oidsByLayer in selectionByLayer)
		{
			T layer = oidsByLayer.Key;
			List<long> oids = oidsByLayer.Value;

			if (! CanSelectFromLayer(layer, notifications))
			{
				continue;
			}

			result.Add(layer, oids);
		}

		return result;
	}

	private bool CanSelectFromLayer([CanBeNull] BasicFeatureLayer layer,
	                                NotificationCollection notifications = null)
	{
		if (layer == null)
		{
			NotificationUtils.Add(notifications, "No feature layer");
			return false;
		}

		string layerName = layer.Name;

		if (! LayerUtils.IsVisible(layer, ActiveMapView))
		{
			NotificationUtils.Add(notifications, $"Layer is not visible in active map: {layerName}");
			return false;
		}

		if (SelectOnlySelectableFeatures && ! layer.IsSelectable)
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not selectable");
			return false;
		}

		if (CanSelectOnlyEditFeatures && ! layer.IsEditable)
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not editable");
			return false;
		}

		var geometryType = GeometryUtils.TranslateEsriGeometryType(layer.ShapeType);
		if (! CanSelectGeometryType(geometryType))
		{
			NotificationUtils.Add(notifications,
			                      $"Layer {layerName}: Cannot use geometry type {layer.ShapeType}");
			return false;
		}

		return CanSelectFromLayerCore(layer);
	}

	/// <summary>
	/// Checks whether no selection or multi selection is allowed.
	/// </summary>
	/// <param name="selectionByLayer">The selection</param>
	/// <param name="notifications">Pass in a NotificationCollection if you
	/// want the reasons loggad at info level. Pass in null if you want no logging</param>
	/// <returns></returns>
	public bool CanUseSelection(
		[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications)
	{
		void LogInfo(NotificationCollection collection)
		{
			if (collection == null)
			{
				return;
			}

			if (collection.Any()) _msg.Debug("Cannot use selection:");

			foreach (INotification notification in collection)
			{
				_msg.Info(notification.Message);
			}
		}

		int count = SelectionUtils.GetFeatureCount(selectionByLayer);

		if (count == 0 && ! AllowNoSelection)
		{
			_msg.Debug(
				$"Cannot use selection: tool has to have a selection, selection count is {count}");
			return false;
		}

		if (count > 1 && ! AllowMultiSelection(out string reason))
		{
			notifications?.Add(reason);

			_msg.Debug(
				$"Cannot use selection: multi selection not allowed, selection count is {count}");

			LogInfo(notifications);
			return false;
		}

		bool result = CanUseSelectionCore(selectionByLayer, notifications);

		if (notifications == null)
		{
			return result;
		}

		LogInfo(notifications);
		return result;
	}

	#endregion

	protected async Task StartSelectionPhaseAsync()
	{
		await SetupSelectionSketchAsync();

		StartSelectionPhaseCore();

		// don't snap anymore if you cannot use selection
		UseSnapping = false;
	}

	private async Task StartConstructionPhaseAsync()
	{
		StartConstructionPhaseCore();

		UseSnapping = true;

		SetConstructionCursor();

		await QueuedTask.Run(() =>
		{
			// For some unknown reason, the SketchSymbol is only correctly
			// updated after a call to ActiveMapView.ClearSketchAsync in
			// a QueuedTask since ArcGis Pro 3.4
			ActiveMapView.ClearSketchAsync();
		});
	}

	protected virtual void StartSelectionPhaseCore() { }

	protected virtual void StartConstructionPhaseCore() { }

	protected override CancelableProgressorSource GetProgressorSource()
	{
		var message = Caption ?? string.Empty;
		const bool delayedShow = true; // todo daro delayedShow = true
		return new CancelableProgressorSource(message, "Cancelling", delayedShow);
	}

	private void SetConstructionCursor()
	{
		Cursor cursor = ConstructionCursorCore;
		Assert.NotNull(cursor);

		SetToolCursor(cursor);
	}

	private bool InConstructionPhase()
	{
		return Cursor == ConstructionCursorCore;
	}
}

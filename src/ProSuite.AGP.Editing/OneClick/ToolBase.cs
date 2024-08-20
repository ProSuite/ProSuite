using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.OneClick;

public abstract class ToolBase : MapTool
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const Key _keyPolygonDraw = Key.P;
	private const Key _keyLassoDraw = Key.L;

	private readonly SketchGeometryType _defaultSketchGeometryType;
	private readonly Latch _toolActivateLatch = new();
	private readonly Latch _latch = new();

	protected ToolBase(SketchGeometryType sketchGeometryType)
	{
		ContextMenuID = "esri_mapping_selection2DContextMenu";

		// needed to call OnSelectionChangedAsync
		UseSelection = true;
		UseSnapping = false;
		IsSketchTool = true;
		FireSketchEvents = true;
		IsWYSIWYG = true;
		SketchType = sketchGeometryType;

		_defaultSketchGeometryType = sketchGeometryType;
		
		SelectionCursorCore = ToolUtils.GetCursor(Resources.SelectionToolNormal);
		ConstructionCursorCore = ToolUtils.GetCursor(Resources.EditSketchCrosshair);
	}

	private List<Key> HandledKeys { get; } =
		new(4) { Key.Escape, Key.F2, _keyLassoDraw, _keyPolygonDraw };
	
	protected Point CurrentMousePosition;

	[NotNull]
	protected virtual Cursor SelectionCursorCore { get; }

	[NotNull]
	protected virtual Cursor ConstructionCursorCore { get; }

	protected virtual bool AllowNoSelection => false;

	#region abstract
	
	protected abstract void LogPromptForSelection();

	protected abstract SelectionSettings GetSelectionSettings();

	protected abstract Task HandleEscapeAsync();

	protected abstract bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer layer);

	#endregion

	#region overrides

	protected sealed override async Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		_msg.Debug($"Activate {Caption}");

		// After on tool activate OnSelectionChangedAsync is fired. But ToolBase just needs
		// OnSelectionChangedAsync when selection is cleared or to react when a selection
		// is made but not by the tool itself, e.g. select row in table. In all other cases
		// we want OnSelectionChangedAsync to be latched. Especially when the tool is activated.
		_toolActivateLatch.Increment();

		await ViewUtils.TryAsync(OnToolActivateCoreAsync(hasMapViewChanged), _msg);

		if (MapUtils.HasSelection(ActiveMapView))
		{
			bool selectionProcessed = await ViewUtils.TryAsync(ProcessSelectionAsync(), _msg);

			if (selectionProcessed)
			{
				StartConstructionPhase();
			}
			else
			{
				StartSelectionPhase();
			}
		}
		else
		{
			StartSelectionPhase();
		}
	}

	protected sealed override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		_msg.Debug($"Deactivate {Caption}");
		
		await ViewUtils.TryAsync(OnToolDeactivateCoreAsync(hasMapViewChanged), _msg);
	}

	protected sealed override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		await ViewUtils.TryAsync(OnSelectionChangedCoreAsync(args), _msg);
	}

	// todo daro move

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

	protected override void OnToolKeyDown(MapViewKeyEventArgs args)
	{
		if (HandledKeys.Contains(args.Key))
		{
			args.Handled = true;
		}
	}

	protected sealed override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		if (args.Key == Key.F2)
		{
			await FinishSketchAsync();
		}

		if (args.Key == Key.Escape)
		{
			if (await HasSketchAsync())
			{
				await ViewUtils.TryAsync(ClearSketchAsync, _msg);
			}
			else
			{
				await ViewUtils.TryAsync(HandleEscapeAsync, _msg);

				StartSelectionPhase();
			}
		}

		if (! InConstructionPhase())
		{
			if (args.Key == _keyPolygonDraw)
			{
				SetupPolygonSketch();
			}

			if (args.Key == _keyLassoDraw)
			{
				SetupLassoSketch();
			}
		}

		await ViewUtils.TryAsync(HandleKeyDownCoreAsync(args), _msg);
	}

	protected override void OnToolKeyUp(MapViewKeyEventArgs args)
	{
		if (HandledKeys.Contains(args.Key))
		{
			args.Handled = true;
		}
	}

	protected sealed override async Task HandleKeyUpAsync(MapViewKeyEventArgs args)
	{
		_msg.VerboseDebug(() => "HandleKeyUpAsync");

		if (! InConstructionPhase())
		{
			if (args.Key is _keyPolygonDraw or _keyLassoDraw)
			{
				ResetSketchAppearance();
			}
		}

		await ViewUtils.TryAsync(HandleKeyUpCoreAsync(args), _msg);
	}

	protected override void OnToolMouseMove(MapViewMouseEventArgs args)
	{
		CurrentMousePosition = args.ClientPoint;

		base.OnToolMouseMove(args);
	}

	#endregion

	#region tool

	protected virtual Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		return Task.CompletedTask;
	}

	protected virtual Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		return Task.CompletedTask;
	}

	protected virtual Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
	{
		return Task.CompletedTask;
	}

	protected virtual Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
	{
		return Task.CompletedTask;
	}

	#endregion

	#region sketch

	protected virtual Task<bool> OnSketchModifiedCoreAsync()
	{
		return Task.FromResult(true);
	}

	private async Task<bool> OnSketchCompleteCoreAsync(Geometry geometry)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			_msg.Debug("Sketch is null or empty");
			return await Task.FromResult(false);
		}

		if (MapUtils.HasSelection(ActiveMapView) && InConstructionPhase())
		{
			IDictionary<BasicFeatureLayer, List<long>> selection =
				await GetApplicableSelection<BasicFeatureLayer>();

			if (CanUseSelection(selection))
			{
				bool constructionProcessed = await OnConstructionSketchCompleteAsync(geometry, selection);

				if (constructionProcessed)
				{
					StartSelectionPhase();
					return true; // sketchCompleteEventHandled = true;
				}

				StartConstructionPhase();
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
					StartConstructionPhase();
				}
				else
				{
					StartSelectionPhase();
				}
			}
		}
		finally
		{
			_latch.Decrement();
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
		using var pickerPrecedence = CreatePickerPrecedence(geometry);

		await (AllowMultiSelection(out _)
			       ? PickerUtils.ShowAsync(pickerPrecedence, FindFeatureSelection)
			       : PickerUtils.ShowAsync(pickerPrecedence, FindFeatureSelection,
			                               PickerMode.ShowPicker));

		return MapUtils.HasSelection(ActiveMapView);
	}

	protected virtual IPickerPrecedence CreatePickerPrecedence(Geometry sketchGeometry)
	{
		return new PickerPrecedence(sketchGeometry,
		                            GetSelectionSettings().SelectionTolerancePixels,
		                            ActiveMapView.ClientToScreen(CurrentMousePosition));
	}

	/// <summary>Is on GUI thread. Use QueuedTask.</summary>
	/// <returns><b>true</b>: construction finished and start selection phase,
	/// <b>false</b>: stay in construction phase.</returns>
	protected virtual Task<bool> OnConstructionSketchCompleteAsync([NotNull] Geometry geometry,
		IDictionary<BasicFeatureLayer, List<long>> selectionByLayer)
	{
		return Task.FromResult(true);
	}

	private void SetSketchSymbolBasedOnSelection(
		IDictionary<BasicFeatureLayer, List<Feature>> applicableSelection)
	{
		if (SelectionUtils.GetFeatureCount(applicableSelection) != 1)
		{
			return;
		}

		(BasicFeatureLayer layer, IList<Feature> features) = applicableSelection.FirstOrDefault();

		if (layer is not FeatureLayer featureLayer)
		{
			return;
		}

		Feature feature = Assert.NotNull(features.FirstOrDefault());

		SetSketchSymbolBasedOnSelectionCore(featureLayer, feature);
	}

	/// <summary>
	/// Sets sketch symbol based on the first applicable selected feature.
	/// Override it and do nothing in the overridden method
	/// if you do not want to let the selection set the sketch symbol.
	/// </summary>
	/// <param name="layer">The layer of the applicable selected feature</param>
	/// <param name="feature">First applicable selected feature</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	protected virtual void SetSketchSymbolBasedOnSelectionCore(
		[NotNull] FeatureLayer layer, [CanBeNull] Feature feature)
	{
		if (feature == null)
		{
			ResetSketchAppearance();
			return;
		}

		GeometryType geometryType = feature.GetShape().GeometryType;
		long oid = feature.GetObjectID();

		switch (geometryType)
		{
			case GeometryType.Point:
				SketchType = SketchGeometryType.Point;
				break;
			case GeometryType.Polyline:
				SketchType = SketchGeometryType.Line;
				break;
			case GeometryType.Polygon:
				SketchType = SketchGeometryType.Polygon;
				break;
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.Multipoint:
			case GeometryType.Multipatch:
			case GeometryType.GeometryBag:
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}

		CIMSymbol symbol = layer.LookupSymbol(oid, ActiveMapView);

		if (symbol == null)
		{
			_msg.Debug(
				$"Cannot set sketch symbol: no symbol found in layer {layer.Name} for oid {oid}.");
		}
		else
		{
			SketchSymbol = symbol.MakeSymbolReference();
		}
	}

	private void ResetSketchAppearance()
	{
		SetSketchType(_defaultSketchGeometryType);
		SketchSymbol = null;
	}

	private void SetSketchType(SketchGeometryType type)
	{
		SketchType = type;
	}

	private async Task<bool> HasSketchAsync()
	{
		Geometry currentSketch = await GetCurrentSketchAsync();

		return currentSketch?.IsEmpty == false;
	}

	private void SetupPolygonSketch()
	{
		SetSketchType(SketchGeometryType.Polygon);

		SetupPolygonSketchCore();
	}

	protected virtual void SetupPolygonSketchCore() { }

	private void SetupLassoSketch()
	{
		SetSketchType(SketchGeometryType.Lasso);

		SetupLassoSketchCore();
	}

	protected virtual void SetupLassoSketchCore() { }

	#endregion

	#region selection
	
	/// <returns><b>true</b>: selection processed and start construction phase,
	/// <b>false</b>: stay in selection phase.</returns>
	private async Task<bool> ProcessSelectionAsync()
	{
		using var source = GetProgressorSource();
		var progressor = source?.Progressor;

		Task<bool> task = QueuedTaskUtils.Run(() =>
		{
			Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
				SelectionUtils.GetSelection<BasicFeatureLayer>(ActiveMapView.Map);

			if (! CanUseSelection(selectionByLayer, new NotificationCollection()))
			{
				return Task.FromResult(false); // startContructionPhase = false
			}

			IDictionary<BasicFeatureLayer, List<Feature>> applicableSelection =
				GetApplicableSelectedFeatures(selectionByLayer, new NotificationCollection());

			if (applicableSelection.Count == 0)
			{
				return Task.FromResult(false);
			}

			SetSketchSymbolBasedOnSelection(applicableSelection);

			return ProcessSelectionCoreAsync(applicableSelection, progressor);
		}, progressor);

		return await ViewUtils.TryAsync(task, _msg);
	}

	private async Task<bool> ProcessSelectionAsync(SelectionSet selection)
	{
		using var source = GetProgressorSource();
		var progressor = source?.Progressor;

		Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
			SelectionUtils.GetSelection<BasicFeatureLayer>(selection);

		if (! CanUseSelection(selectionByLayer, new NotificationCollection()))
		{
			return false; // startContructionPhase = false
		}

		IDictionary<BasicFeatureLayer, List<Feature>> applicableSelection =
			GetApplicableSelectedFeatures(selectionByLayer, new NotificationCollection());

		if (applicableSelection.Count == 0)
		{
			return false; // startContructionPhase = false
		}

		SetSketchSymbolBasedOnSelection(applicableSelection);

		Task<bool> task = ProcessSelectionCoreAsync(applicableSelection, progressor);

		return await ViewUtils.TryAsync(task, _msg);
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
			StartSelectionPhase();
			await ClearSketchAsync();
		}
		else if (args.Selection.Count > 0)
		{
			// Process selection not by this tool, e.g. select row in table, etc.
			// Do not react on selection made by this tool.
			if (_latch.IsLatched)
			{
				return;
			}

			bool selectionProcessed = await ProcessSelectionAsync(args.Selection);

			if (selectionProcessed)
			{
				StartConstructionPhase();
			}
			else
			{
				StartSelectionPhase();
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

	protected virtual bool CanSelectOnlyEditFeatures()
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

	private async Task<IDictionary<T, List<long>>> GetApplicableSelection<T>()
		where T : BasicFeatureLayer
	{
		// todo daro rename, revise method

		Dictionary<T, List<long>> selectionByLayer =
			await QueuedTask.Run(() => SelectionUtils.GetSelection<T>(ActiveMapView.Map));

		IDictionary<T, List<long>> result =
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

	[NotNull]
	protected IDictionary<BasicFeatureLayer, List<Feature>> GetApplicableSelectedFeatures(
		[NotNull] IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications = null)
	{
		var result = new Dictionary<BasicFeatureLayer, List<Feature>>(selectionByLayer.Count);

		SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

		foreach (KeyValuePair<BasicFeatureLayer, List<long>> oidsByLayer in selectionByLayer)
		{
			BasicFeatureLayer layer = oidsByLayer.Key;
			List<long> oids = oidsByLayer.Value;

			if (! CanSelectFromLayer(layer, notifications))
			{
				continue;
			}

			var features = MapUtils
			               .GetFeatures(layer, oids, withoutJoins: true, recycling: false,
			                            mapSpatialReference).ToList();

			result.Add(layer, features);
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

		if (! LayerUtils.IsVisible(layer))
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not visible");
			return false;
		}

		if (! layer.IsVisibleInView(ActiveMapView))
		{
			// Takes scale range into account (and probably the parent layer too)
			NotificationUtils.Add(notifications, $"Layer {layerName} is not visible on map");
			return false;
		}

		if (! layer.IsSelectable)
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not selectable");
			return false;
		}

		if (CanSelectOnlyEditFeatures() && ! layer.IsEditable)
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
	private bool CanUseSelection(
		[NotNull] IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications = null)
	{
		void LogInfo(NotificationCollection collection)
		{
			if (collection == null)
			{
				return;
			}
			_msg.Debug("Cannot use selection:");
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

	protected void StartSelectionPhase()
	{
		StartSelectionPhaseCore();

		// don't snap anymore if cannot use selection
		UseSnapping = false;

		SetSelectionCursor();

		ResetSketchAppearance();
	}

	protected async void StartConstructionPhase()
	{
		StartConstructionPhaseCore();

		UseSnapping = true;

		SetConstructionCursor();

		await ClearSketchAsync();
	}

	protected virtual void StartSelectionPhaseCore() { }

	protected virtual void StartConstructionPhaseCore() { }

	/// <summary>
	/// Override and return null if no <see cref="CancelableProgressorSource"/>
	/// to show no progressor.
	/// </summary>
	[CanBeNull]
	protected virtual CancelableProgressorSource GetProgressorSource()
	{
		var message = Caption ?? string.Empty;
		const bool delayedShow = true; // todo daro delayedShow = true
		return new CancelableProgressorSource(message, "Cancelling", delayedShow);
	}

	private void SetSelectionCursor()
	{
		Cursor cursor = SelectionCursorCore;
		Assert.NotNull(cursor);

		if (Application.Current.Dispatcher.CheckAccess())
		{
			Cursor = cursor;
		}
		else
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				Cursor = cursor;
			});
		}
	}

	private void SetConstructionCursor()
	{
		Cursor cursor = ConstructionCursorCore;
		Assert.NotNull(cursor);

		if (Application.Current.Dispatcher.CheckAccess())
		{
			Cursor = cursor;
		}
		else
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				Cursor = cursor;
			});
		}
	}

	private bool InConstructionPhase()
	{
		return Cursor == ConstructionCursorCore;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class OneClickToolBase : MapTool
	{
		private const Key _keyShowOptionsPane = Key.O;
		private const Key _keyPolygonDraw = Key.P;
		private const Key _keyLassoDraw = Key.L;

		private readonly TimeSpan _sketchBlockingPeriod = TimeSpan.FromSeconds(1);

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private Geometry _lastSketch;
		private DateTime _lastSketchFinishedTime;

		protected Point CurrentMousePosition;

		protected OneClickToolBase()
		{
			ContextMenuID = "esri_mapping_selection2DContextMenu";

			UseSnapping = false;
			HandledKeys.Add(Key.Escape);
			HandledKeys.Add(_keyLassoDraw);
			HandledKeys.Add(_keyPolygonDraw);
			HandledKeys.Add(_keyShowOptionsPane);
		}

		/// <summary>
		/// Whether this tool requires a selection and the base class should handle the selection phase.
		/// </summary>
		protected bool RequiresSelection { get; init; } = true;

		/// <summary>
		/// Whether the required selection can only contain editable features.
		/// </summary>
		protected bool SelectOnlyEditFeatures { get; init; } = true;

		/// <summary>
		/// Whether the required selection can only contain selectable features.
		/// TODO: maybe refactor/rename IgnoreSelectability or merge with TargetFeatureSelection?
		/// </summary>
		protected bool SelectOnlySelectableFeatures { get; init; } = true;

		/// <summary>
		/// Whether selected features that are not applicable (e.g. due to wrong geometry type) are
		/// allowed. Otherwise, the selection phase will continue until all selected features are
		/// usable by the tool.
		/// </summary>
		protected bool AllowNotApplicableFeaturesInSelection { get; set; } = true;

		/// <summary>
		/// The list of handled keys, i.e. the keys for which <see cref="MapTool.HandleKeyDownAsync" />
		/// will be called (and potentially in the future also MapTool.HandleKeyUpAsync)
		/// </summary>
		protected List<Key> HandledKeys { get; } = new();

		/// <summary>
		/// The currently pressed keys.
		/// </summary>
		protected HashSet<Key> PressedKeys { get; } = new();

		protected virtual Cursor SelectionCursor { get; set; }
		protected virtual Cursor SelectionCursorShift { get; set; }

		protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => "OnToolActivateAsync");

			MapPropertyChangedEvent.Subscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Subscribe(OnMapSelectionChangedAsync);
			EditCompletedEvent.Subscribe(OnEditCompletedAsync);

			PressedKeys.Clear();

			try
			{
				using var source = GetProgressorSource();
				var progressor = source?.Progressor;

				await QueuedTaskUtils.Run(() =>
				{
					OnToolActivatingCore();

					if (RequiresSelection)
					{
						ProcessSelection(progressor);
					}

					return OnToolActivatedCore(hasMapViewChanged);
				}, progressor);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError(ex, _msg);
			}
		}

		protected override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => "OnToolDeactivateAsync");

			MapPropertyChangedEvent.Unsubscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChangedAsync);
			EditCompletedEvent.Unsubscribe(OnEditCompletedAsync);

			ViewUtils.Try(HideOptionsPane, _msg);

			Task task = QueuedTask.Run(() => OnToolDeactivateCore(hasMapViewChanged));

			await ViewUtils.TryAsync(task, _msg);
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs args)
		{
			_msg.VerboseDebug(() => "OnToolKeyDown");

			ViewUtils.Try(() =>
			{
				PressedKeys.Add(args.Key);

				if (KeyboardUtils.IsModifierKey(args.Key) || HandledKeys.Contains(args.Key))
				{
					args.Handled = true;
				}

				if (args.Key == _keyShowOptionsPane)
				{
					ShowOptionsPane();
				}

				if (KeyboardUtils.IsShiftKey(args.Key))
				{
					// todo daro rename to SetShiftCursor?
					// This sets shift cursor. But don't do it in QueuedTask because
					// tool cursor is not updated until mouse is moved for the first time.
					ShiftPressedCore();
				}

				OnKeyDownCore(args);
			}, _msg, suppressErrorMessageBox: true);
		}

		protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
		{
			_msg.VerboseDebug(() => "HandleKeyDownAsync");

			if (args.Key == Key.Escape)
			{
				await ViewUtils.TryAsync(HandleEscapeAsync, _msg);
			}

			if (await IsInSelectionPhaseAsync())
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

		private void SetupLassoSketch()
		{
			SetupSketch(SketchGeometryType.Lasso, enforceSimpleSketch: true);

			SetupLassoSketchCore();
		}

		protected virtual void SetupLassoSketchCore() { }

		private void SetupPolygonSketch()
		{
			SetupSketch(SketchGeometryType.Polygon, enforceSimpleSketch: true);

			// TODO: Sketch symbol: No vertices

			SetupPolygonSketchCore();
		}

		protected virtual void SetupPolygonSketchCore() { }

		protected override void OnToolKeyUp(MapViewKeyEventArgs args)
		{
			_msg.VerboseDebug(() => "OnToolKeyUp");

			try
			{
				ViewUtils.Try(() =>
				{
					if (KeyboardUtils.IsShiftKey(args.Key))
					{
						ShiftReleasedCore();
					}

					OnKeyUpCore(args);

					// NOTE: The HandleKeyUpAsync is only called for handled keys.
					// However, they will not perform the standard functionality devised by the
					// application! Examples: F8 (Toggle stereo fixed cursor mode), B (snap to ground, ...)
					if (KeyboardUtils.IsModifierKey(args.Key) || HandledKeys.Contains(args.Key))
					{
						args.Handled = true;
					}
				}, _msg, suppressErrorMessageBox: true);
			}
			finally
			{
				PressedKeys.Remove(args.Key);
			}
		}

		protected override async Task HandleKeyUpAsync(MapViewKeyEventArgs args)
		{
			_msg.VerboseDebug(() => "HandleKeyUpAsync");

			if (await IsInSelectionPhaseAsync() && args.Key is _keyPolygonDraw or _keyLassoDraw)
			{
				ResetSketch();
			}

			await ViewUtils.TryAsync(HandleKeyUpCoreAsync(args), _msg);
		}

		private void ResetSketch()
		{
			SetupSketch(GetSelectionSketchGeometryType());

			ResetSketchCore();
		}

		protected virtual void ResetSketchCore() { }

		protected override void OnToolMouseMove(MapViewMouseEventArgs args)
		{
			CurrentMousePosition = args.ClientPoint;

			base.OnToolMouseMove(args);
		}

		protected override async Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
		{
			_msg.VerboseDebug(() => $"OnSketchCompleteAsync ({Caption})");

			if (sketchGeometry == null)
			{
				return false;
			}

			if (DateTime.Now - _lastSketchFinishedTime < _sketchBlockingPeriod &&
			    GeometryUtils.Engine.Equals(_lastSketch, sketchGeometry))
			{
				// In some situations, seemingly randomly, this method is called twice
				// - On the same instance
				// - Both times on the UI thread
#if DEBUG
				_msg.Warn($"OnSketchCompleteAsync: Duplicate call is ignored for {Caption}!");
#else
				_msg.Debug($"OnSketchCompleteAsync: Duplicate call is ignored for {Caption}.");
#endif

				return false;
			}

			try
			{
				_lastSketch = sketchGeometry;
				_lastSketchFinishedTime = DateTime.Now;

				ViewUtils.Try(() =>
				{
					if (SketchType == SketchGeometryType.Polygon)
					{
						// Otherwise relational operators and spatial queries return the wrong result
						sketchGeometry = GeometryUtils.Simplify(sketchGeometry);
					}
				}, _msg);

				using var source = GetProgressorSource();
				var progressor = source?.Progressor;

				if (RequiresSelection && IsInSelectionPhase())
				{
					return await OnSelectionSketchCompleteAsync(sketchGeometry, progressor);
				}

				return await OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			}
			catch (Exception e)
			{
				// Consider Task.FromException? --> no, as it throws once awaited!
				ErrorHandler.HandleError(
					$"{Caption}: Error completing sketch ({e.Message})", e, _msg);

				return await Task.FromResult(true);
			}
		}

		protected virtual void ShiftPressedCore()
		{
			if (SelectionCursorShift != null && IsInSelectionPhase(true))
			{
				SetCursor(SelectionCursorShift);
			}
		}

		protected virtual void ShiftReleasedCore()
		{
			if (SelectionCursor != null && IsInSelectionPhase(true))
			{
				SetCursor(SelectionCursor);
			}
		}

		protected void StartSelectionPhase()
		{
			SelectionSettings settings = GetSelectionSettings();

			SetupSketch(GetSelectionSketchGeometryType(), settings.SketchOutputMode);

			bool shiftDown = KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			                 KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			SetCursor(shiftDown ? SelectionCursorShift : SelectionCursor);

			OnSelectionPhaseStarted();
		}

		/// <summary>
		/// Sets up the tool for a sketch that is typically used
		/// to select things (features, graphics, etc.)
		/// </summary>
		protected void SetupRectangleSketch()
		{
			SetupSketch(SketchGeometryType.Rectangle);
		}

		protected void SetupSketch(SketchGeometryType? sketchType,
		                           SketchOutputMode sketchOutputMode = SketchOutputMode.Map,
		                           bool useSnapping = false,
		                           bool completeSketchOnMouseUp = true,
		                           bool enforceSimpleSketch = false)
		{
			SketchOutputMode = sketchOutputMode;

			// Note: set CompleteSketchOnMouseUp before SketchType, or it has no effect
			CompleteSketchOnMouseUp = completeSketchOnMouseUp;

			SketchType = sketchType;

			UseSnapping = useSnapping;

			GeomIsSimpleAsFeature = enforceSimpleSketch;
		}

		protected abstract SketchGeometryType GetSelectionSketchGeometryType();

		protected virtual void OnSelectionPhaseStarted() { }

		private async void OnMapSelectionChangedAsync(MapSelectionChangedEventArgs args)
		{
			// TODO: Use async overload added at 3.0
			// Note: app crashes on uncaught exceptions here

			Task<bool> task = QueuedTask.Run(() => OnMapSelectionChangedCore(args));

			await ViewUtils.TryAsync(task, _msg, suppressErrorMessageBox: true);
		}

		private async Task OnEditCompletedAsync(EditCompletedEventArgs args)
		{
			await ViewUtils.TryAsync(OnEditCompletedAsyncCore(args), _msg,
			                         suppressErrorMessageBox: true);
		}

		/// <summary>
		/// The task to be run on edit complete.
		/// NOTE: This task is run after every edit operation, including undo or delete with DEL key!
		/// In that case there seems to be no catch block observing the potential exception thrown
		/// inside the task execution, which leads to a crash of the application due to the finalizer
		/// thread throwing:
		/// A Task's exception(s) were not observed either by Waiting on the Task or accessing its
		/// Exception property. As a result, the unobserved exception was rethrown by the finalizer thread.
		/// Therefore, any exception must be caught inside the Task execution!
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		protected virtual Task OnEditCompletedAsyncCore(EditCompletedEventArgs args)
		{
			return Task.CompletedTask;
		}

		/// <remarks>Will be called on MCT</remarks>
		protected virtual void OnToolActivatingCore() { }

		/// <remarks>Will be called on MCT</remarks>
		protected virtual bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			return true;
		}

		protected virtual void OnToolDeactivateCore(bool hasMapViewChanged) { }

		/// <remarks>Will be called on MCT</remarks>
		protected virtual bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			return true;
		}

		[CanBeNull]
		protected virtual CancelableProgressorSource GetProgressorSource()
		{
			var message = Caption ?? string.Empty;
			const bool delayedShow = true;
			return new CancelableProgressorSource(message, "Cancelling", delayedShow);
		}

		protected virtual Task<bool> OnSketchCompleteCoreAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			return Task.FromResult(true);
		}

		protected int GetSelectionTolerancePixels()
		{
			return GetSelectionSettings().SelectionTolerancePixels;
		}

		private async Task<bool> OnSelectionSketchCompleteAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			using var pickerPrecedence =
				CreatePickerPrecedence(sketchGeometry);

			await ViewUtils.TryAsync(
				PickerUtils.ShowAsync(pickerPrecedence, FindFeaturesOfAllLayers), _msg);

			await QueuedTaskUtils.Run(() => ProcessSelection(progressor), progressor);

			return true;
		}

		protected virtual IPickerPrecedence CreatePickerPrecedence(Geometry sketchGeometry)
		{
			return new PickerPrecedence(sketchGeometry,
			                            GetSelectionTolerancePixels(),
			                            ActiveMapView.ClientToScreen(CurrentMousePosition));
		}

		private IEnumerable<FeatureSelectionBase> FindFeaturesOfAllLayers(
			[NotNull] Geometry searchGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			var mapView = ActiveMapView;

			if (mapView is null)
			{
				return Enumerable.Empty<FeatureSelectionBase>();
			}

			var featureFinder = new FeatureFinder(mapView)
			                    {
				                    SpatialRelationship = spatialRelationship,
				                    DelayFeatureFetching = true
			                    };

			const Predicate<Feature> featurePredicate = null;
			return featureFinder.FindFeaturesByLayer(searchGeometry, fl => CanSelectFromLayer(fl),
			                                         featurePredicate, progressor);
		}

		// TODO: Make obsolete, always use Async overload?
		protected bool IsInSelectionPhase()
		{
			bool shiftDown = KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			                 KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			return IsInSelectionPhase(shiftDown);
		}

		protected virtual bool IsInSelectionPhase(bool shiftIsPressed)
		{
			return false;
		}

		protected Task<bool> IsInSelectionPhaseAsync()
		{
			bool shiftDown = KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			                 KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			return IsInSelectionPhaseCoreAsync(shiftDown);
		}

		protected virtual Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			return Task.FromResult(false);
		}

		protected virtual void OnKeyDownCore(MapViewKeyEventArgs args) { }

		protected virtual void OnKeyUpCore(MapViewKeyEventArgs args) { }

		protected virtual Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
		{
			return Task.CompletedTask;
		}

		protected virtual Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			return Task.CompletedTask;
		}

		protected virtual void OnPropertyChanged(MapPropertyChangedEventArgs args) { }

		protected virtual void ShowOptionsPane() { }

		protected virtual void HideOptionsPane() { }

		protected abstract SelectionSettings GetSelectionSettings();

		protected abstract Task HandleEscapeAsync();

		protected abstract void LogUsingCurrentSelection();

		protected abstract void LogPromptForSelection();

		protected bool CanSelectFeatureGeometryType([NotNull] Feature feature)
		{
			using var featureClass = feature.GetTable();
			GeometryType shapeType = featureClass.GetShapeType();

			return CanSelectGeometryType(shapeType);
		}

		/// <remarks>Will be called on MCT</remarks>>
		protected virtual void AfterSelection(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor progressor) { }

		/// <remarks>Must be called on MCT</remarks>
		protected void ProcessSelection([CanBeNull] CancelableProgressor progressor = null)
		{
			var selectionByLayer = SelectionUtils.GetSelection(ActiveMapView.Map);

			var notifications = new NotificationCollection();
			List<Feature> applicableSelection =
				GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection,
				                                      notifications)
					.ToList();

			int selectionCount = selectionByLayer.Sum(kvp => kvp.Value.Count);

			if (applicableSelection.Count > 0 &&
			    (AllowNotApplicableFeaturesInSelection ||
			     applicableSelection.Count == selectionCount))
			{
				LogUsingCurrentSelection();

				AfterSelection(applicableSelection, progressor);
			}
			else
			{
				if (selectionCount > 0)
				{
					_msg.InfoFormat(notifications.Concatenate(Environment.NewLine));
				}

				LogPromptForSelection();
				StartSelectionPhase();
			}
		}

		/// <summary>
		/// Whether the selection shall be retrieved without the join even if the layer is joined.
		/// This is important for updating features. Features based on a joined table throw an
		/// exception when setting the shape (GOTOP-190)!
		/// </summary>
		protected bool UnJoinedSelection { get; set; } = true;

		protected void SetCursor([CanBeNull] Cursor cursor)
		{
			if (cursor == null)
			{
				return;
			}

			if (Application.Current.Dispatcher.CheckAccess())
			{
				Cursor = cursor;
			}
			else
			{
				Application.Current.Dispatcher.Invoke(() => { Cursor = cursor; });
			}
		}

		private bool CanSelectFromLayer([CanBeNull] Layer layer,
		                                NotificationCollection notifications = null)
		{
			if (layer is not BasicFeatureLayer featureLayer)
			{
				NotificationUtils.Add(notifications, "Not a feature layer");
				return false;
			}

			string layerName = layer.Name;

			if (! LayerUtils.IsVisible(layer))
			{
				NotificationUtils.Add(notifications, $"Layer is not visible: {layerName}");
				return false;
			}

			if (! layer.IsVisibleInView(ActiveMapView))
			{
				// Takes scale range into account (and probably the parent layer too)
				NotificationUtils.Add(notifications, $"Layer is not visible on map: {layerName}");
				return false;
			}

			if (SelectOnlySelectableFeatures && ! featureLayer.IsSelectable)
			{
				NotificationUtils.Add(notifications, $"Layer is not selectable: {layerName}");
				return false;
			}

			if (SelectOnlyEditFeatures && ! featureLayer.IsEditable)
			{
				NotificationUtils.Add(notifications, $"Layer is not editable: {layerName}");
				return false;
			}

			var geometryType = GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);
			if (! CanSelectGeometryType(geometryType))
			{
				NotificationUtils.Add(notifications,
				                      $"Cannot use geometry type {featureLayer.ShapeType} of layer {layerName}");
				return false;
			}

			using var featureClass = featureLayer.GetFeatureClass();
			if (featureClass is null)
			{
				NotificationUtils.Add(notifications,
				                      $"Layer has no valid data source: {layerName}");
				return false;
			}

			return CanSelectFromLayerCore(featureLayer);
		}

		protected bool CanUseSelection([NotNull] MapView mapView)
		{
			if (mapView is null)
				throw new ArgumentNullException(nameof(mapView));

			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection(mapView.Map);

			return CanUseSelection(selectionByLayer);
		}

		private bool CanUseSelection([NotNull] Dictionary<MapMember, List<long>> selectionByLayer)
		{
			return AllowNotApplicableFeaturesInSelection
				       ? selectionByLayer.Any(l => CanSelectFromLayer(l.Key as Layer))
				       : selectionByLayer.All(l => CanSelectFromLayer(l.Key as Layer));
		}

		protected IEnumerable<Feature> GetDistinctApplicableSelectedFeatures(
			[NotNull] Dictionary<MapMember, List<long>> selectionByLayer,
			bool unJoinedFeaturesForEditing = false,
			[CanBeNull] NotificationCollection notifications = null)
		{
			HashSet<GdbObjectReference> usedRows = new HashSet<GdbObjectReference>();

			foreach (Feature feature in GetApplicableSelectedFeatures(selectionByLayer,
				         unJoinedFeaturesForEditing, notifications))
			{
				var objectReference = new GdbObjectReference(feature);

				if (usedRows.Add(objectReference))
				{
					yield return feature;
				}
			}
		}

		protected IEnumerable<Feature> GetApplicableSelectedFeatures(
			[NotNull] Dictionary<MapMember, List<long>> selectionByLayer,
			bool unJoinedFeaturesForEditing = false,
			[CanBeNull] NotificationCollection notifications = null)
		{
			var filteredCount = 0;
			var selectionCount = 0;

			SpatialReference mapSpatialReference = ActiveMapView.Map.SpatialReference;

			foreach (KeyValuePair<MapMember, List<long>> oidsByLayer in selectionByLayer)
			{
				if (! CanSelectFromLayer(oidsByLayer.Key as Layer, notifications))
				{
					filteredCount += oidsByLayer.Value.Count;
					continue;
				}

				foreach (Feature feature in MapUtils.GetFeatures(
					         oidsByLayer.Key, oidsByLayer.Value, unJoinedFeaturesForEditing, false,
					         mapSpatialReference))
				{
					yield return feature;
					selectionCount++;
				}
			}

			if (filteredCount == 1)
			{
				notifications?.Insert(
					0, new Notification("The selected feature cannot be used by the tool."));
			}

			if (filteredCount > 1)
			{
				notifications?.Insert(
					0,
					new Notification(
						$"{filteredCount} of {selectionCount + filteredCount} selected features cannot be used by the tool."));
			}
		}

		protected IEnumerable<Feature> GetApplicableSelectedFeatures(MapView mapView)
		{
			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection(mapView.Map);

			return GetApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection);
		}

		protected virtual bool CanSelectGeometryType(GeometryType geometryType)
		{
			return true;
		}

		protected virtual bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer basicFeatureLayer)
		{
			return true;
		}

		/// <summary>Clear the selection on the active map</summary>
		/// <remarks>Must call on MCT</remarks>
		protected void ClearSelection()
		{
			var map = ActiveMapView?.Map;
			map?.ClearSelection();
		}
	}
}

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
	// todo daro log more, especially in subclasses
	public abstract class OneClickToolBase : MapTool
	{
		private const Key _keyShowOptionsPane = Key.O;

		private readonly TimeSpan _sketchBlockingPeriod = TimeSpan.FromSeconds(1);

		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private IPickerPrecedence _pickerPrecedence;

		private Geometry _lastSketch;
		private DateTime _lastSketchFinishedTime;

		protected OneClickToolBase()
		{
			ContextMenuID = "esri_mapping_selection2DContextMenu";

			UseSnapping = false;
			HandledKeys.Add(Key.Escape);
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
		/// allowed. Otherwise the selection phase will continue until all selected features are
		/// usable by the tool.
		/// </summary>
		protected bool AllowNotApplicableFeaturesInSelection { get; set; } = true;

		protected virtual IPickerPrecedence PickerPrecedence =>
			_pickerPrecedence ?? (_pickerPrecedence = new StandardPickerPrecedence());

		/// <summary>
		/// The list of handled keys, i.e. the keys for which <see cref="MapTool.HandleKeyDownAsync" />
		/// will be called (and potentially in the future also MapTool.HandleKeyUpAsync)
		/// </summary>
		protected List<Key> HandledKeys { get; } = new();

		/// <summary>
		/// The currently pressed keys.
		/// </summary>
		protected HashSet<Key> PressedKeys { get; } = new();

		protected virtual Cursor SelectionCursor { get; init; }
		protected virtual Cursor SelectionCursorShift { get; init; }

		protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => "OnToolActivateAsync");

			MapPropertyChangedEvent.Subscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Subscribe(OnMapSelectionChangedAsync);
			EditCompletedEvent.Subscribe(OnEditCompletedAsync);

			PressedKeys.Clear();

			Task<bool> task = QueuedTask.Run(() =>
			{
				OnToolActivatingCore();

				if (RequiresSelection)
				{
					ProcessSelection(ActiveMapView);
				}

				return OnToolActivatedCore(hasMapViewChanged);
			});

			await ViewUtils.TryAsync(task, _msg);
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
		}

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

			await ViewUtils.TryAsync(async () => { await HandleKeyUpCoreAsync(args); }, _msg);
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

				Task<bool> task;

				if (RequiresSelection && await IsInSelectionPhaseAsync())
				{
					task = OnSelectionSketchCompleteAsync(sketchGeometry,
					                                      GetCancelableProgressor());
					return await ViewUtils.TryAsync(task, _msg);
				}

				task = OnSketchCompleteCoreAsync(sketchGeometry, GetCancelableProgressor());
				return await ViewUtils.TryAsync(task, _msg);
			}
			catch (Exception e)
			{
				// NOTE: Throwing here results in a process crash (Exception while waiting for a Task to complete)
				// Consider Task.FromException?
				ErrorHandler.HandleError(
					$"{Caption}: Error completing sketch ({e.Message})", e, _msg);
			}

			return await Task.FromResult(true);
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

			SetupSketch(settings.SketchGeometryType, settings.SketchOutputMode);

			bool shiftDown = KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			                 KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			SetCursor(shiftDown ? SelectionCursorShift : SelectionCursor);

			OnSelectionPhaseStarted();
		}

		/// <summary>
		/// Sets up the tool for a sketch that is typically used to select things (features, graphics, etc.)
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

			// NOTE: CompleteSketchOnMouseUp must be set before the sketch geometry type,
			// otherwise it has no effect!
			CompleteSketchOnMouseUp = completeSketchOnMouseUp;

			SketchType = sketchType;

			UseSnapping = useSnapping;

			GeomIsSimpleAsFeature = enforceSimpleSketch;
		}

		protected virtual void OnSelectionPhaseStarted() { }

		private async void OnMapSelectionChangedAsync(MapSelectionChangedEventArgs args)
		{
			// TODO: Use async overload added at 3.0
			// NOTE: If the exception of this event is not caught here, the application crashes!
			// TODO daro: isn't it the responsibility of the calling code to wrap a QueuedTask around it?
			Task<bool> task = QueuedTask.Run(() => OnMapSelectionChangedCore(args));

			await ViewUtils.TryAsync(task, _msg, suppressErrorMessageBox: true);

			//await ViewUtils.TryAsync(OnMapSelectionChangedCoreAsync(args), _msg, suppressErrorMessageBox: true);
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
		/// Therefore any exception must be caught inside the Task execution!
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		protected virtual Task OnEditCompletedAsyncCore(EditCompletedEventArgs args)
		{
			return Task.CompletedTask;
		}

		protected virtual void OnToolActivatingCore() { }

		protected virtual bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			return true;
		}

		protected virtual void OnToolDeactivateCore(bool hasMapViewChanged) { }

		protected virtual bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			return true;
		}

		[CanBeNull]
		protected virtual CancelableProgressor GetCancelableProgressor()
		{
			return new CancelableProgressorSource().Progressor;
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
			SelectionCombinationMethod selectionMethod =
				KeyboardUtils.IsShiftDown()
					? SelectionCombinationMethod.XOR
					: SelectionCombinationMethod.New;

			// Polygon-selection allows for more accurate selection in feature-dense areas using contains
			SpatialRelationship spatialRelationship = SketchType == SketchGeometryType.Polygon
				                                          ? SpatialRelationship.Contains
				                                          : SpatialRelationship.Intersects;

			Geometry selectionGeometry = null;
			var pickerLocation = new Point(0, 0);

			bool singlePick = false;
			List<FeatureSelectionBase> candidatesOfManyLayers =
				await QueuedTaskUtils.Run(() =>
				{
					selectionGeometry = ToolUtils.SketchToSearchGeometry(sketchGeometry,
						GetSelectionTolerancePixels(), out singlePick);

					pickerLocation =
						MapView.Active.MapToScreen(selectionGeometry.Extent.Center);

					return FindFeaturesOfAllLayers(selectionGeometry, spatialRelationship).ToList();
				});

			if (! candidatesOfManyLayers.Any())
			{
				// No candidate (user clicked into empty space):
				if (selectionMethod == SelectionCombinationMethod.XOR)
				{
					// No addition to, and no removal from selection
					return false;
				}

				await QueuedTask.Run(SelectionUtils.ClearSelection);

				return false;
			}

			PickerPrecedence.SelectionGeometry = selectionGeometry;

			// todo daro refactor
			bool result = singlePick
				              ? await SingleSelectAsync(candidatesOfManyLayers,
				                                        pickerLocation,
				                                        PickerPrecedence,
				                                        selectionMethod)
				              : await AreaSelectAsync(candidatesOfManyLayers,
				                                      pickerLocation,
				                                      PickerPrecedence,
				                                      selectionMethod);

			await QueuedTask.Run(() => ProcessSelection(MapView.Active, progressor));

			return result;
		}

		// todo daro when return false?
		private static async Task<bool> SingleSelectAsync(
			[NotNull] IList<FeatureSelectionBase> candidatesOfLayers,
			Point pickerLocation,
			IPickerPrecedence pickerPrecedence,
			SelectionCombinationMethod selectionMethod)
		{
			var orderedSelection =
				PickerUtils.OrderByGeometryDimension(candidatesOfLayers).ToList();

			PickerMode pickerMode = pickerPrecedence.GetPickerMode(orderedSelection);

			// ALT pressed: select all, do not show picker
			if (pickerMode == PickerMode.PickAll)
			{
				await QueuedTask.Run(() =>
				{
					SelectionUtils.SelectFeatures(
						orderedSelection, selectionMethod,
						selectionMethod == SelectionCombinationMethod.New);
				});

				return true;
			}

			// no key pressed: pick best
			if (pickerMode == PickerMode.PickBest)
			{
				await QueuedTask.Run(
					() =>
					{
						// all this code has to be in QueuedTask because
						// IEnumerables are enumerated later
						IEnumerable<IPickableItem> items =
							PickableItemsFactory.CreateFeatureItems(orderedSelection);

						var pickedItem =
							pickerPrecedence.PickBest<IPickableFeatureItem>(items);

						//since SelectionCombinationMethod.New is only applied to
						//the current layer but selections of other layers remain,
						//we manually need to clear all selections first.

						SelectionUtils.SelectFeature(
							pickedItem.Layer, selectionMethod,
							pickedItem.Oid,
							selectionMethod == SelectionCombinationMethod.New);
					});

				return true;
			}

			// CTRL pressed: show picker
			if (pickerMode == PickerMode.ShowPicker)
			{
				IEnumerable<IPickableItem> items =
					await QueuedTask.Run(
						() => PickableItemsFactory.CreateFeatureItems(orderedSelection));

				IPickableFeatureItem pickedItem =
					await ShowPickerAsync<IPickableFeatureItem>(
						items, pickerPrecedence, pickerLocation);

				if (pickedItem == null)
				{
					return false;
				}

				await QueuedTask.Run(() =>
				{
					//since SelectionCombinationMethod.New is only applied to
					//the current layer but selections of other layers remain,
					//we manually need to clear all selections first.

					SelectionUtils.SelectFeature(
						pickedItem.Layer, selectionMethod,
						pickedItem.Oid,
						selectionMethod == SelectionCombinationMethod.New);
				});

				return true;
			}

			return false;
		}

		private static async Task<bool> AreaSelectAsync(
			[NotNull] IList<FeatureSelectionBase> candidatesOfLayers,
			Point pickerLocation,
			IPickerPrecedence pickerPrecedence,
			SelectionCombinationMethod selectionMethod)
		{
			var orderedSelection =
				PickerUtils.OrderByGeometryDimension(candidatesOfLayers).ToList();

			PickerMode pickerMode =
				pickerPrecedence.GetPickerMode(orderedSelection, true);

			//CTRL was pressed: picker shows FC's to select from
			if (pickerMode == PickerMode.ShowPicker)
			{
				IEnumerable<IPickableItem> items =
					await QueuedTask.Run(
						() => PickableItemsFactory.CreateFeatureClassItems(
							PickerUtils.OrderByGeometryDimension(candidatesOfLayers)));

				IPickableFeatureClassItem pickedItem =
					await ShowPickerAsync<IPickableFeatureClassItem>(
						items, pickerPrecedence, pickerLocation);

				if (pickedItem == null)
				{
					return false;
				}

				await QueuedTask.Run(() =>
				{
					foreach (OidSelection featureClassSelection in
					         pickedItem.Layers.Select(layer => new OidSelection(
						                                  pickedItem.Oids.ToList(), layer,
						                                  MapView.Active.Map.SpatialReference)))
					{
						SelectionUtils.SelectFeatures(
							featureClassSelection,
							selectionMethod,
							selectionMethod == SelectionCombinationMethod.New);
					}
				});
			}
			else
			{
				//no modifier pressed: select all in envelope
				await QueuedTask.Run(() =>
				{
					SelectionUtils.SelectFeatures(
						candidatesOfLayers,
						selectionMethod,
						selectionMethod == SelectionCombinationMethod.New);
				});
			}

			return true;
		}

		[NotNull]
		protected static async Task<T> ShowPickerAsync<T>(
			IEnumerable<IPickableItem> items, IPickerPrecedence pickerPrecedence,
			Point pickerLocation)
			where T : class, IPickableItem
		{
			var picker = new PickerService();

			Func<Task<T>> showPickerControl =
				await QueuedTaskUtils.Run(() => picker.PickSingle<T>(
					                          items, pickerLocation,
					                          pickerPrecedence));

			return await ViewUtils.TryAsync(showPickerControl(), _msg);
		}

		private IEnumerable<FeatureSelectionBase> FindFeaturesOfAllLayers(
			[NotNull] Geometry searchGeometry,
			SpatialRelationship spatialRelationship)
		{
			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				return Enumerable.Empty<FeatureSelectionBase>();
			}

			var featureFinder = new FeatureFinder(mapView)
			                    {
				                    SpatialRelationship = spatialRelationship,
				                    DelayFeatureFetching = true
			                    };

			return featureFinder.FindFeaturesByLayer(
				searchGeometry,
				fl => CanSelectFromLayer(fl));
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

		protected virtual void OnKeyDownCore(MapViewKeyEventArgs k) { }

		protected virtual void OnKeyUpCore(MapViewKeyEventArgs mapViewKeyEventArgs) { }

		protected virtual Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			return Task.CompletedTask;
		}

		protected virtual void OnPropertyChanged(MapPropertyChangedEventArgs e) { }

		protected virtual void ShowOptionsPane() { }

		protected virtual void HideOptionsPane() { }

		protected abstract SelectionSettings GetSelectionSettings();

		protected abstract Task HandleEscapeAsync();

		protected abstract void LogUsingCurrentSelection();

		protected abstract void LogPromptForSelection();

		protected bool CanSelectFeatureGeometryType([NotNull] Feature feature)
		{
			GeometryType shapeType = DatasetUtils.GetShapeType(feature.GetTable());

			return CanSelectGeometryType(shapeType);
		}

		protected virtual void AfterSelection(
			[NotNull] Map map, [NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor progressor) { }

		protected void ProcessSelection([NotNull] MapView mapView, // TODO or just a Map?
		                                [CanBeNull] CancelableProgressor progressor = null)
		{
			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection(mapView.Map);

			var notifications = new NotificationCollection();
			List<Feature> applicableSelection =
				GetApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection, notifications)
					.ToList();

			int selectionCount = selectionByLayer.Sum(kvp => kvp.Value.Count);

			if (applicableSelection.Count > 0 &&
			    (AllowNotApplicableFeaturesInSelection ||
			     applicableSelection.Count == selectionCount))
			{
				LogUsingCurrentSelection();

				AfterSelection(mapView.Map, applicableSelection, progressor);
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
			if (cursor != null)
			{
				Cursor = cursor;
			}
		}

		private bool CanSelectFromLayer([CanBeNull] Layer layer,
		                                NotificationCollection notifications = null)
		{
			var basicFeatureLayer = layer as BasicFeatureLayer;

			if (basicFeatureLayer == null)
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

			if (! layer.IsVisibleInView(MapView.Active))
			{
				// Takes scale range into account (and probably the parent layer too)
				NotificationUtils.Add(notifications, $"Layer {layerName} not visible");
				return false;
			}

			if (SelectOnlySelectableFeatures &&
			    ! basicFeatureLayer.IsSelectable)
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} not selectable");
				return false;
			}

			if (SelectOnlyEditFeatures &&
			    ! basicFeatureLayer.IsEditable)
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} not editable");
				return false;
			}

			if (! CanSelectGeometryType(
				    GeometryUtils.TranslateEsriGeometryType(basicFeatureLayer.ShapeType)))
			{
				NotificationUtils.Add(notifications,
				                      $"Layer {layerName}: Cannot use geometry type {basicFeatureLayer.ShapeType}");
				return false;
			}

			if (basicFeatureLayer is FeatureLayer featureLayer)
			{
				if (featureLayer.GetFeatureClass() == null)
				{
					NotificationUtils.Add(notifications, $"Layer {layerName} is invalid");
					return false;
				}
			}

			return CanSelectFromLayerCore(basicFeatureLayer);
		}

		[Obsolete]
		protected virtual bool CanUseSelection([NotNull] IEnumerable<Feature> selectedFeatures)
		{
			return selectedFeatures.Any(CanSelectFeatureGeometryType);
		}

		// TODO Map instead of MapView
		protected bool CanUseSelection([NotNull] MapView activeMapView)
		{
			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection(activeMapView.Map);

			return CanUseSelection(selectionByLayer);
		}

		protected bool CanUseSelection([NotNull] Dictionary<MapMember, List<long>> selectionByLayer)
		{
			return AllowNotApplicableFeaturesInSelection
				       ? selectionByLayer.Any(l => CanSelectFromLayer(l.Key as Layer))
				       : selectionByLayer.All(l => CanSelectFromLayer(l.Key as Layer));
		}

		protected IEnumerable<Feature> GetApplicableSelectedFeatures(
			[NotNull] Dictionary<MapMember, List<long>> selectionByLayer,
			bool unJoinedFeaturesForEditing = false,
			[CanBeNull] NotificationCollection notifications = null)
		{
			var filteredCount = 0;
			var selectionCount = 0;

			SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

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
					0, new Notification("The selected feature cannot be used by the tool:"));
			}

			if (filteredCount > 1)
			{
				notifications?.Insert(
					0,
					new Notification(
						$"{filteredCount} of {selectionCount + filteredCount} selected features cannot be used by the tool:"));
			}
		}

		protected IEnumerable<Feature> GetApplicableSelectedFeatures(MapView activeView)
		{
			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection(activeView.Map);

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
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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
using ProSuite.Commons.UI.Keyboard;
using Cursor = System.Windows.Input.Cursor;

namespace ProSuite.AGP.Editing.OneClick
{
	// TODO DARO is it the duty of the base class to wrap overridable methods into a QueuedTask?
	// Shouldn't it be the caller, superclass to do QueuedTask?
	// compare:
	// OnEditCompletedCoreAsync vs. OnToolDeactivateCore
	public abstract class OneClickToolBase : MapTool
	{
		private const Key _keyShowOptionsPane = Key.O;

		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private IPickerPrecedence _pickerPrecedence;

		protected OneClickToolBase(SketchProperties sketchProperties)
		{
			ContextMenuID = "esri_mapping_selection2DContextMenu";

			UseSnapping = false;
			HandledKeys.Add(Key.Escape);
			HandledKeys.Add(_keyShowOptionsPane);

			SetupSketch(sketchProperties.SketchGeometryType, sketchProperties.SketchOutputMode);
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
		/// Whether selected features that are not applicable (e.g. due to wrong geometry type) are
		/// allowed. Otherwise the selection phase will continue until all selected features are
		/// usable by the tool.
		/// </summary>
		protected bool AllowNotApplicableFeaturesInSelection => true;

		public virtual IPickerPrecedence PickerPrecedence =>
			_pickerPrecedence ??= new SelectionToolPickerPrecedence();

		/// <summary>
		/// The list of handled keys, i.e. the keys for which <see cref="MapTool.HandleKeyDownAsync" />
		/// will be called (and potentially in the future also MapTool.HandleKeyUpAsync)
		/// </summary>
		protected List<Key> HandledKeys { get; } = new();

		protected HashSet<Key> PressedKeys { get; } = new();

		protected virtual Cursor SelectionCursor { get; init; }

		protected Cursor SelectionCursorShift { get; init; }

		#region overrides

		protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => nameof(OnToolActivateAsync));

			MapPropertyChangedEvent.Subscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
			EditCompletedEvent.Subscribe(OnEditCompletedAsync);

			PressedKeys.Clear();

			await ViewUtils.TryAsync(
				QueuedTask.Run(
					() =>
					{
						OnToolActivatingCore();

						if (RequiresSelection)
						{
							ProcessSelection(ActiveMapView);
						}

						return OnToolActivatedCore(hasMapViewChanged);
					}), _msg);
		}

		protected override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => nameof(OnToolDeactivateAsync));

			MapPropertyChangedEvent.Unsubscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);
			EditCompletedEvent.Unsubscribe(OnEditCompletedAsync);

			HideOptionsPane();

			// TODO DARO is it the duty of the base class to wrap overridable methods into a QeuedTask?
			await ViewUtils.TryAsync(
				QueuedTask.Run(() => OnToolDeactivateCore(hasMapViewChanged)), _msg);
		}

		protected override async void OnToolKeyDown(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug(() => nameof(OnToolKeyDown));

			await ViewUtils.TryAsync(() =>
			{
				PressedKeys.Add(k.Key);

				if (KeyboardUtils.IsModifierKey(k.Key) || HandledKeys.Contains(k.Key))
				{
					k.Handled = true;
				}

				if (k.Key == _keyShowOptionsPane)
				{
					ShowOptionsPane();
				}

				// Cancel outside a queued task otherwise the current task that blocks the queue
				// cannot be cancelled.
				if (k.Key == Key.Escape)
				{
					HandleEscape();
				}

				return OnToolKeyDownAsync(k);
			}, _msg);
		}

		protected override async void OnToolKeyUp(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug(() => nameof(OnToolKeyUp));

			try
			{
				await ViewUtils.TryAsync(OnTookKeyUpAsync(k), _msg);
			}
			finally
			{
				PressedKeys.Remove(k.Key);
			}
		}

		protected override async Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
		{
			_msg.VerboseDebug(() => nameof(OnSketchCompleteAsync));

			if (sketchGeometry == null)
			{
				return false;
			}

			if (SketchType == SketchGeometryType.Polygon)
			{
				ViewUtils.Try(() =>
				{
					// Otherwise relational operators and spatial queries return the wrong result
					sketchGeometry = GeometryUtils.Simplify(sketchGeometry);
				}, _msg);
			}

			if (RequiresSelection && IsInSelectionPhase())
			{
				return await ViewUtils.TryAsync(
					       OnSelectionSketchCompleteAsync(sketchGeometry,
					                                      GetCancelableProgressor()), _msg);
			}

			return await ViewUtils.TryAsync(
				       OnSketchCompleteCoreAsync(sketchGeometry, GetCancelableProgressor()), _msg);
		}

		#endregion

		#region privates

		private async Task OnToolKeyDownAsync(MapViewKeyEventArgs k)
		{
			await QueuedTask.Run(() =>
			{
				if (IsShiftKey(k.Key))
				{
					ShiftPressedCore();
				}

				OnKeyDownCore(k);
			});
		}

		private async Task OnTookKeyUpAsync(MapViewKeyEventArgs k)
		{
			await QueuedTask.Run(() =>
			{
				if (IsShiftKey(k.Key))
				{
					ShiftReleasedCore();
				}

				OnKeyUpCore(k);
			});
		}

		private async void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => nameof(OnMapSelectionChanged));

			// NOTE: If the exception of this event is not caught here, the application crashes!
			await ViewUtils.TryAsync(OnMapSelectionChangedAsync(args), _msg);
		}

		private async Task OnMapSelectionChangedAsync(MapSelectionChangedEventArgs args)
		{
			// Used to clear derived geometries etc.
			await QueuedTask.Run(() => OnMapSelectionChangedCore(args));
		}

		private async Task OnEditCompletedAsync(EditCompletedEventArgs args)
		{
			_msg.VerboseDebug(() => nameof(OnEditCompletedAsync));

			await ViewUtils.TryAsync(OnEditCompletedCoreAsync(args), _msg);
		}

		private async Task<bool> OnSelectionSketchCompleteAsync(Geometry sketchGeometry,
		                                                        CancelableProgressor progressor)
		{
			// TODO: Add Utils method to KeyboardUtils to do it in the WPF way
			SelectionCombinationMethod selectionMethod =
				KeyboardUtils.IsModifierPressed(Keys.Shift)
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
						GetSelectionTolerance(), out singlePick);

					pickerLocation =
						MapView.Active.MapToScreen(selectionGeometry.Extent.Center);

					// find all features spatially related with searchGeometry
					// TODO: 1. Find all features in point layers, if count > 0 -> skip the rest
					//       2. Find all features in polyline layers, ...
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
					await ShowPickerCoreAsync<IPickableFeatureItem>(
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
					await ShowPickerCoreAsync<IPickableFeatureClassItem>(
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

			if (! layer.IsVisible())
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} not visible");
				return false;
			}

			if (! basicFeatureLayer.IsSelectable)
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

		private void ProcessSelection([NotNull] MapView activeMapView,
		                              [CanBeNull] CancelableProgressor progressor = null)
		{
			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection(activeMapView.Map);

			var notifications = new NotificationCollection();
			List<Feature> applicableSelection =
				GetApplicableSelectedFeatures(selectionByLayer, notifications).ToList();

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

		[NotNull]
		private static async Task<T> ShowPickerCoreAsync<T>(
			IEnumerable<IPickableItem> items, IPickerPrecedence pickerPrecedence,
			Point pickerLocation)
			where T : class, IPickableItem
		{
			var picker = new PickerService();

			Func<Task<T>> showPickerControl =
				await QueuedTaskUtils.Run(() => picker.PickSingle<T>(
					                          items, pickerLocation,
					                          pickerPrecedence));

			T pickedItem =
				await ViewUtils.TryAsync(showPickerControl(), _msg);

			return pickedItem;
		}

		#endregion

		#region overridables

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

		protected virtual void OnSelectionPhaseStarted() { }

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
		protected virtual async Task OnEditCompletedCoreAsync(EditCompletedEventArgs args)
		{
			await Task.CompletedTask;
		}

		protected virtual void OnToolActivatingCore() { }

		protected virtual bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			return true;
		}

		protected virtual void OnToolDeactivateCore(bool hasMapViewChanged) { }

		protected virtual void OnMapSelectionChangedCore(MapSelectionChangedEventArgs args) { }

		[CanBeNull]
		protected virtual CancelableProgressor GetCancelableProgressor()
		{
			return new CancelableProgressorSource().Progressor;
		}

		protected virtual async Task<bool> OnSketchCompleteCoreAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			return await Task.FromResult(true);
		}

		// todo daro when return false?
		// todo daro ViewUtils.Try araound it?

		protected virtual bool IsInSelectionPhase(bool shiftIsPressed)
		{
			return false;
		}

		protected virtual void OnKeyDownCore(MapViewKeyEventArgs k) { }

		protected virtual void OnKeyUpCore(MapViewKeyEventArgs mapViewKeyEventArgs) { }

		protected virtual void OnPropertyChanged(MapPropertyChangedEventArgs e) { }

		protected virtual void ShowOptionsPane() { }

		protected virtual void HideOptionsPane() { }

		protected abstract bool HandleEscape();

		protected abstract void LogUsingCurrentSelection();

		protected abstract void LogPromptForSelection();

		protected abstract int GetSelectionTolerance();

		protected virtual void AfterSelection([NotNull] IList<Feature> selectedFeatures,
		                                      [CanBeNull] CancelableProgressor progressor) { }
		
		protected virtual bool CanSelectGeometryType(GeometryType geometryType)
		{
			return true;
		}

		protected virtual bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer basicFeatureLayer)
		{
			return true;
		}

		#endregion

		#region protected

		protected void StartSelectionPhase()
		{
			SetCursor(KeyboardUtils.IsModifierPressed(Keys.Shift, true)
				          ? SelectionCursorShift
				          : SelectionCursor);

			OnSelectionPhaseStarted();
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

		protected static bool IsShiftKey(Key key)
		{
			return key == Key.LeftShift ||
			       key == Key.RightShift;
		}

		protected bool IsInSelectionPhase()
		{
			return IsInSelectionPhase(KeyboardUtils.IsModifierPressed(Keys.Shift, true));
		}

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
					         oidsByLayer.Key, oidsByLayer.Value, false, mapSpatialReference))
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

			return GetApplicableSelectedFeatures(selectionByLayer);
		}

		protected void SetCursor([CanBeNull] Cursor cursor)
		{
			if (cursor != null)
			{
				Cursor = cursor;
			}
		}

		protected bool CanSelectFeatureGeometryType([NotNull] Feature feature)
		{
			GeometryType shapeType = DatasetUtils.GetShapeType(feature.GetTable());

			return CanSelectGeometryType(shapeType);
		}

		protected static async Task<T> ShowPickerAsync<T>(IEnumerable<IPickableItem> items,
		                                                  IPickerPrecedence pickerPrecedence,
		                                                  Point pickerLocation)
			where T : class, IPickableItem
		{
			return await ShowPickerCoreAsync<T>(items, pickerPrecedence, pickerLocation);
		}

		#endregion
	}
}

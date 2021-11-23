using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ArcGIS.Core.CIM;
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
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI.Keyboard;
using Cursor = System.Windows.Input.Cursor;
using SelectionMode = ProSuite.AGP.Editing.Selection.SelectionMode;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class OneClickToolBase : MapTool
	{
		private const Key _keyShowOptionsPane = Key.O;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		protected readonly List<IDisposable> _overlays = new List<IDisposable>();

		protected OneClickToolBase()
		{
			UseSnapping = false;
			HandledKeys.Add(Key.Escape);
			HandledKeys.Add(_keyShowOptionsPane);
		}

		private SketchingMoveType SketchingMoveType { get; set; }

		/// <summary>
		/// Whether this tool requires a selection and the base class should handle the selection phase.
		/// </summary>
		protected bool RequiresSelection { get; set; } = true;

		/// <summary>
		/// Whether the required selection can only contain editable fetures.
		/// </summary>
		protected bool SelectOnlyEditFeatures { get; set; } = true;

		/// <summary>
		/// Whether selected features that are not applicable (e.g. due to wrong geometry type) are
		/// allowed. Otherwise the selection phase will continue until all selected features are
		/// usable by the tool. 
		/// </summary>
		protected bool AllowNotApplicableFeaturesInSelection { get; set; } = true;

		protected virtual SelectionSettings SelectionSettings { get; set; } =
			new SelectionSettings();

		/// <summary>
		/// The list of handled keys, i.e. the keys for which <see cref="MapTool.HandleKeyDownAsync"/>
		/// will be called (and potentially in the future also MapTool.HandleKeyUpAsync)
		/// </summary>
		protected List<Key> HandledKeys { get; } = new List<Key>();

		/// <summary>
		/// The currently pressed keys.
		/// </summary>
		protected HashSet<Key> PressedKeys { get; } = new HashSet<Key>();

		protected virtual Cursor SelectionCursor { get; set; }
		protected Cursor SelectionCursorShift { get; set; }
		protected Cursor SelectionCursorNormal { get; set; }
		protected Cursor SelectionCursorNormalShift { get; set; }
		protected Cursor SelectionCursorUser { get; set; }
		protected Cursor SelectionCursorUserShift { get; set; }
		protected Cursor SelectionCursorOriginal { get; set; }
		protected Cursor SelectionCursorOriginalShift { get; set; }

		protected override Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => "OnToolActivateAsync");

			MapPropertyChangedEvent.Subscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
			EditCompletedEvent.Subscribe(OnEditCompleted);

			PressedKeys.Clear();

			try
			{
				return QueuedTask.Run(
					() =>
					{
						OnToolActivatingCore();

						if (RequiresSelection)
						{
							ProcessSelection(ActiveMapView);
						}

						return OnToolActivatedCore(hasMapViewChanged);
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool activation ({Caption}): {e.Message}", e);
			}

			return Task.CompletedTask;
		}

		protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug(() => "OnToolDeactivateAsync");

			MapPropertyChangedEvent.Unsubscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);
			EditCompletedEvent.Unsubscribe(OnEditCompleted);

			try
			{
				HideOptionsPane();

				return QueuedTask.Run(() => OnToolDeactivateCore(hasMapViewChanged));
			}
			catch (Exception e)
			{
				HandleError($"Error in tool deactivation ({Caption}): {e.Message}", e, true);
			}

			return Task.CompletedTask;
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug(() => "OnToolKeyDown");

			try
			{
				PressedKeys.Add(k.Key);

				if (IsModifierKey(k.Key) || HandledKeys.Contains(k.Key))
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

				QueuedTaskUtils.Run(
					delegate
					{
						if (IsShiftKey(k.Key))
						{
							ShiftPressedCore();
						}

						OnKeyDownCore(k);

						return true;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool key down ({Caption}): {e.Message}", e, true);
			}
		}

		protected override void OnToolKeyUp(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug(() => "OnToolKeyUp");

			try
			{
				QueuedTaskUtils.Run(
					delegate
					{
						if (IsShiftKey(k.Key))
						{
							ShiftReleasedCore();
						}

						OnKeyUpCore(k);
						return true;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool key up ({Caption}): {e.Message}", e, true);
			}
			finally
			{
				PressedKeys.Remove(k.Key);
			}
		}

		protected override async Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
		{
			_msg.VerboseDebug(() => "OnSketchCompleteAsync");

			if (sketchGeometry == null)
			{
				// TODO: if in selection phase select at the current mouse location?
				return false;
			}

			try
			{
				CancelableProgressor progressor = GetCancelableProgressor();

				if (SketchType == SketchGeometryType.Polygon)
				{
					// Otherwise relational operators and spatial queries return the wrong result
					sketchGeometry = GeometryUtils.Simplify(sketchGeometry);
				}

				SketchingMoveType = GetSketchingMoveType(sketchGeometry);

				if (RequiresSelection && IsInSelectionPhase())
				{
					return await OnSelectionSketchComplete(sketchGeometry, progressor);
				}

				return await OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			}
			catch (Exception e)
			{
				HandleError($"{Caption}: Error completing sketch ({e.Message})", e);
				// NOTE: Throwing here results in a process crash (Exception while waiting for a Task to complete)
				// Consider Task.FromException?
			}

			return false;
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
			SetupSketch(SelectionSettings.SketchGeometryType, SelectionSettings.SketchOutputMode);
			
			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				SetCursor(SelectionCursorShift);
			}
			else
			{
				SetCursor(SelectionCursor);
			}

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

		private static bool IsModifierKey(Key key)
		{
			return key == Key.LeftShift ||
			       key == Key.RightShift ||
			       key == Key.LeftCtrl ||
			       key == Key.RightCtrl ||
			       key == Key.LeftAlt ||
			       key == Key.RightAlt;
		}

		protected static bool IsShiftKey(Key key)
		{
			return key == Key.LeftShift ||
			       key == Key.RightShift;
		}

		private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnMapSelectionChanged");

			try
			{
				QueuedTaskUtils.Run(
					delegate
					{
						// Used to clear derived geometries etc.
						bool result = OnMapSelectionChangedCore(args);

						return result;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error OnSelectionChanged: {e.Message}", e, true);
			}
		}

		private Task OnEditCompleted(EditCompletedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnEditCompleted");

			try
			{
				return OnEditCompletedCore(args);
			}
			catch (Exception e)
			{
				HandleError($"Error OnEditCompleted: {e.Message}", e, true);

				return Task.FromResult(false);
			}
		}

		protected virtual Task OnEditCompletedCore(EditCompletedEventArgs args)
		{
			return Task.FromResult(true);
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
			return null;
		}

		protected virtual Task<bool> OnSketchCompleteCoreAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			return Task.FromResult(true);
		}

		private static SketchingMoveType GetSketchingMoveType(Geometry geometry)
		{
			if (ToolUtils.IsSingleClickSketch(geometry))
			{
				return SketchingMoveType.Click;
			}

			return SketchingMoveType.Drag;
		}

		protected int GetSelectionTolerancePixels()
		{
			return SelectionSettings.SelectionTolerancePixels;
		}

		private Geometry GetSelectionGeometry(Geometry sketchGeometry)
		{
			if (SketchingMoveType == SketchingMoveType.Click)
			{
				return ToolUtils.GetSinglePickSelectionArea(
					sketchGeometry, SelectionSettings.SelectionTolerancePixels);
			}
			else
			{
				return sketchGeometry;
			}
		}

		private SelectionMode GetSelectionSketchMode()
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Alt))
			{
				return SelectionMode.Original;
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Control))
			{
				return SelectionMode.UserSelect;
			}

			return SelectionMode.Normal;
		}

		private async Task<bool> OnSelectionSketchComplete(Geometry sketchGeometry,
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

			Geometry selectionGeometry;
			var pickerWindowLocation = new Point(0, 0);

			Dictionary<BasicFeatureLayer, List<long>> candidatesOfManyLayers =
				await QueuedTaskUtils.Run(() =>
				{
					DisposeOverlays();

					selectionGeometry = GetSelectionGeometry(sketchGeometry);
					pickerWindowLocation =
						MapView.Active.MapToScreen(selectionGeometry.Extent.Center);

					// find all features spatially related with selectionGeometry
					return FindFeaturesOfAllLayers(selectionGeometry, spatialRelationship);
				});

			if (! candidatesOfManyLayers.Any())
			{
				if (selectionMethod != SelectionCombinationMethod.XOR)
				{
					//no candidate (user clicked into empty space): clear selection
					await QueuedTask.Run(
						() => { SelectionUtils.ClearSelection(); });

					return false;
				}

				return false;
			}

			if (SketchingMoveType == SketchingMoveType.Click)
			{
				//note if necessary add a virtual core method here for overriding 

				if (GetSelectionSketchMode() == SelectionMode.Original)
					//alt was pressed: select all xy
				{
					await QueuedTask.Run(() =>
					{
						Selector.SelectLayersFeaturesByOids(
							candidatesOfManyLayers, selectionMethod);
					});
				}

				// select a single feature using feature reduction and picker
				else
				{
					IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> candidatesOfLayers =
						await QueuedTask.Run(
							() => GeometryReducer.GetReducedset(candidatesOfManyLayers));

					// show picker if more than one candidate
					if (GeometryReducer.ContainsManyFeatures(candidatesOfManyLayers))
					{
						List<IPickableItem> pickables = new List<IPickableItem>();
						foreach (var layerCandidates in candidatesOfLayers)
						{
							pickables.AddRange(
								await QueuedTask.Run(
									() => PickerUI.Picker.CreatePickableFeatureItems(
										layerCandidates)));
						}

						var picker = new PickerUI.Picker(pickables, pickerWindowLocation);

						var item = await picker.PickSingle() as PickableFeatureItem;

						if (item != null)
						{
							var kvp = new KeyValuePair<BasicFeatureLayer, List<long>>(
								item.Layer, new List<long> {item.Oid});

							await QueuedTask.Run(() =>
							{
								Selector.SelectLayersFeaturesByOids(
									kvp, selectionMethod);
							});
						}
					}
					else
					{
						await QueuedTask.Run(() =>
						{
							Selector.SelectLayersFeaturesByOids(
								candidatesOfLayers.First(), selectionMethod);
						});
					}
				}
			}

			if (SketchingMoveType == SketchingMoveType.Drag)
			{
				//CTRL was pressed: picker shows FC's to select from
				if (GetSelectionSketchMode() == SelectionMode.UserSelect)
				{
					List<IPickableItem> pickingCandidates =
						await QueuedTask.Run(
							() => PickableItemAdapter.Get(GetFcCandidates(candidatesOfManyLayers)));

					var picker = new PickerUI.Picker(pickingCandidates, pickerWindowLocation);

					var item = await picker.PickSingle() as PickableFeatureClassItem;

					if (item != null)
					{
						await QueuedTask.Run(() =>
						{
							item.BelongingFeatureLayers.ForEach(layer =>
							{
								List<long> oids = candidatesOfManyLayers[layer];

								SelectionUtils.SelectFeatures(layer, selectionMethod, oids);
							});
						});
					}
				}

				//no modifier pressed: select all in envelope
				else
				{
					await QueuedTask.Run(() =>
					{
						Selector.SelectLayersFeaturesByOids(
							candidatesOfManyLayers, selectionMethod);
					});
				}
			}

			MapView activeMapView = MapView.Active;

			await QueuedTask.Run(() => ProcessSelection(activeMapView, progressor));

			return true;
		}

		private List<FeatureClassInfo> GetFcCandidates(
			Dictionary<BasicFeatureLayer, List<long>> candidatesOfManyLayers)
		{
			List<FeatureClassInfo> featureClassInfos =
				Selector.GetSelectableFeatureclassInfos();
			return featureClassInfos.Where(fcInfo =>
			{
				return fcInfo.BelongingLayers.Any(
					layer => candidatesOfManyLayers.Keys.Contains(layer));
			}).ToList();
		}

		private Dictionary<BasicFeatureLayer, List<long>> FindFeaturesOfAllLayers(
			[NotNull] Geometry selectionGeometry,
			SpatialRelationship spatialRelationship)
		{
			var featuresPerLayer = new Dictionary<BasicFeatureLayer, List<long>>();

			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				return featuresPerLayer;
			}

			foreach (BasicFeatureLayer layer in mapView.Map.GetLayersAsFlattenedList()
			                                           .OfType<BasicFeatureLayer>()
			                                           .Where(layer => CanSelectFromLayer(layer)))
			{
				List<long> oids =
					MapUtils.FilterLayerOidsByGeometry(layer,
					                                   selectionGeometry,
					                                   spatialRelationship).ToList();

				if (oids.Any())
				{
					featuresPerLayer.Add(layer, oids);
				}
			}

			return featuresPerLayer;
		}

		protected bool IsInSelectionPhase()
		{
			return IsInSelectionPhase(KeyboardUtils.IsModifierPressed(Keys.Shift, true));
		}

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

		protected bool CanSelectFeatureGeometryType([NotNull] Feature feature)
		{
			GeometryType shapeType = DatasetUtils.GetShapeType(feature.GetTable());

			return CanSelectGeometryType(shapeType);
		}

		protected virtual void AfterSelection([NotNull] IList<Feature> selectedFeatures,
		                                      [CanBeNull] CancelableProgressor progressor) { }

		private void ProcessSelection([NotNull] MapView activeMapView,
		                              [CanBeNull] CancelableProgressor progressor = null)
		{
			Dictionary<MapMember, List<long>> selectionByLayer = activeMapView.Map.GetSelection();

			NotificationCollection notifications = new NotificationCollection();
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

		protected void HandleError(string message, Exception e, bool noMessageBox = false)
		{
			_msg.Error(message, e);

			if (noMessageBox)
			{
				return;
			}

			ErrorHandler.HandleError(message, null, _msg, "Error");
		}

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
			var featureLayer = layer as FeatureLayer;

			if (featureLayer == null)
			{
				NotificationUtils.Add(notifications, "No feature layer");
				return false;
			}

			string layerName = layer.Name;

			if (! featureLayer.IsVisible)
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} not visible");
				return false;
			}

			if (! featureLayer.IsSelectable)
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} not selectable");
				return false;
			}

			if (SelectOnlyEditFeatures &&
			    ! featureLayer.IsEditable)
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} not editable");
				return false;
			}

			if (! CanSelectGeometryType(
				    GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType)))
			{
				NotificationUtils.Add(notifications,
				                      $"Layer {layerName}: Cannot use geometry type {featureLayer.ShapeType}");
				return false;
			}

			if (featureLayer.GetFeatureClass() == null)
			{
				NotificationUtils.Add(notifications, $"Layer {layerName} is invalid");
				return false;
			}

			return CanSelectFromLayerCore(featureLayer);
		}

		[Obsolete]
		protected virtual bool CanUseSelection([NotNull] IEnumerable<Feature> selectedFeatures)
		{
			return selectedFeatures.Any(CanSelectFeatureGeometryType);
		}

		protected bool CanUseSelection([NotNull] MapView activeMapView)
		{
			Dictionary<MapMember, List<long>> selectionByLayer = activeMapView.Map.GetSelection();

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
			int filteredCount = 0;
			int selectionCount = 0;

			foreach (var oidsByLayer in selectionByLayer)
			{
				if (! CanSelectFromLayer(oidsByLayer.Key as Layer, notifications))
				{
					filteredCount += oidsByLayer.Value.Count;
					continue;
				}

				foreach (Feature feature in MapUtils.GetFeatures(
					oidsByLayer.Key, oidsByLayer.Value))
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
			Dictionary<MapMember, List<long>> selectionByLayer = activeView.Map.GetSelection();

			return GetApplicableSelectedFeatures(selectionByLayer);
		}

		protected virtual bool CanSelectGeometryType(GeometryType geometryType)
		{
			return true;
		}

		protected virtual bool CanSelectFromLayerCore([NotNull] FeatureLayer featureLayer)
		{
			return true;
		}

		private void AddOverlay(Geometry geometry,
		                        CIMSymbol symbol)
		{
			IDisposable addedOverlay =
				MapView.Active.AddOverlay(geometry, symbol.MakeSymbolReference());

			_overlays.Add(addedOverlay);
		}

		public void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay.Dispose();
			}

			_overlays.Clear();
		}

		private CIMPolygonSymbol CreatePolygonSymbol()
		{
			CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

			CIMStroke outline = SymbolFactory.Instance.ConstructStroke(
				magenta, 2, SimpleLineStyle.Solid);

			CIMPolygonSymbol highlightPolygonSymbol =
				SymbolFactory.Instance.ConstructPolygonSymbol(
					magenta, SimpleFillStyle.Null, outline);
			return highlightPolygonSymbol;
		}
	}
}

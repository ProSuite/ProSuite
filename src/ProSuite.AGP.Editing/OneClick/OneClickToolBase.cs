using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using Application = System.Windows.Application;
using Cursor = System.Windows.Input.Cursor;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;
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
		protected bool RequiresSelection { get; set; } = true;

		protected virtual SelectionSettings SelectionSettings { get; set; } =
			new SelectionSettings();

		protected List<Key> HandledKeys { get; } = new List<Key>();
		protected Cursor SelectionCursor { get; set; }
		protected Cursor SelectionCursorShift { get; set; }
		protected Cursor SelectionCursorNormal { get; set; }
		protected Cursor SelectionCursorNormalShift { get; set; }
		protected Cursor SelectionCursorUser { get; set; }
		protected Cursor SelectionCursorUserShift { get; set; }
		protected Cursor SelectionCursorOriginal { get; set; }
		protected Cursor SelectionCursorOriginalShift { get; set; }

		protected override Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug("OnToolActivateAsync");

			MapView.Active.Map.PropertyChanged += Map_PropertyChanged;

			MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);

			try
			{
				return QueuedTask.Run(
					() =>
					{
						OnToolActivatingCore();

						if (RequiresSelection)
						{
							ProcessSelection(SelectionUtils.GetSelectedFeatures(ActiveMapView));
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
			_msg.VerboseDebug("OnToolDeactivateAsync");

			MapView.Active.Map.PropertyChanged -= Map_PropertyChanged;

			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);

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
			_msg.VerboseDebug("OnToolKeyDown");

			try
			{
				if (IsModifierKey(k.Key) || HandledKeys.Contains(k.Key))
				{
					k.Handled = true;
				}

				if (k.Key == _keyShowOptionsPane)
				{
					ShowOptionsPane();
				}

				QueuedTaskUtils.Run(
					delegate
					{
						if (k.Key == Key.Escape)
						{
							return HandleEscape();
						}

						if ((k.Key == Key.LeftShift || k.Key == Key.RightShift) &&
						    SelectionCursorShift != null && IsInSelectionPhase())
						{
							SetCursor(SelectionCursorShift);
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
			_msg.VerboseDebug("OnToolKeyUp");

			// TODO: Key pressed management
			//_shiftIsPressed = false;

			try
			{
				QueuedTaskUtils.Run(
					delegate
					{
						if ((k.Key == Key.LeftShift || k.Key == Key.RightShift) &&
						    SelectionCursor != null && IsInSelectionPhase())
						{
							SetCursor(SelectionCursor);
						}

						OnKeyUpCore(k);
						return true;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool key up ({Caption}): {e.Message}", e, true);
			}
		}

		protected override async Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
		{
			_msg.VerboseDebug("OnSketchCompleteAsync");

			if (sketchGeometry == null)
			{
				// TODO: if in selection phase select at the current mouse location?
				return false;
			}

			try
			{
				CancelableProgressor progressor = GetCancelableProgressor();

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

		protected void StartSelectionPhase()
		{
			SketchOutputMode = SelectionSettings.SketchOutputMode;

			// NOTE: CompleteSketchOnMouseUp must be set before the sketch geometry type,
			// otherwise it has no effect!
			CompleteSketchOnMouseUp = true;

			SketchType = SelectionSettings.SketchGeometryType;

			UseSnapping = false;

			GeomIsSimpleAsFeature = false;

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

		private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug("OnToolActivateAsync");

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
					return FindFeaturesOfAllLayers(selectionGeometry);
				});

			if (! candidatesOfManyLayers.Any())
			{
				//no candidate (user clicked into empty space): clear selection
				await QueuedTask.Run(() =>
				{
					SelectionUtils.ClearSelection(ActiveMapView.Map);
				});

				return false;
			}

			if (SketchingMoveType == SketchingMoveType.Click)
			{
				//note if necessary add a virtual core method here for overriding 

				if (GetSelectionSketchMode() == SelectionMode.Original
				) //alt was pressed: select all xy
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
								item.Layer, new List<long> { item.Oid });

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
					List<IPickableItem> pickingCandidates = await QueuedTask.Run(() =>
					{
						List<FeatureClassInfo> featureClassInfos =
							Selector.GetSelectableFeatureclassInfos();

						return PickableItemAdapter.Get(featureClassInfos);
					});

					var picker = new PickerUI.Picker(pickingCandidates, pickerWindowLocation);

					var item = await picker.PickSingle() as PickableFeatureClassItem;

					if (item != null)
					{
						await QueuedTask.Run(() =>
						{
							item.BelongingFeatureLayers.ForEach(layer =>
							{
								List<long> oids = candidatesOfManyLayers[layer];
								QueryFilter qf = new QueryFilter{ObjectIDs = oids};
								layer.Select(qf, selectionMethod);
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

			await QueuedTask.Run(() =>
				                     ProcessSelection(
					                     SelectionUtils.GetSelectedFeatures(activeMapView),
					                     progressor));

			return true;
		}

		private Dictionary<BasicFeatureLayer, List<long>> FindFeaturesOfAllLayers(
			Geometry selectionGeometry)
		{
			var featuresPerLayer = new Dictionary<BasicFeatureLayer, List<long>>();

			foreach (BasicFeatureLayer layer in
				MapView.Active.Map.Layers.OfType<BasicFeatureLayer>())
			{
				if (CanSelectFromLayer(layer))
				{
					IEnumerable<long> oids =
						MapUtils.FilterLayerOidsByGeometry(layer, selectionGeometry,
						                                   SelectionSettings.SpatialRelationship);
					if (oids.Any())
					{
						featuresPerLayer.Add(layer, oids.ToList());
					}
				}
			}

			return featuresPerLayer;
		}

		protected virtual bool IsInSelectionPhase()
		{
			return false;
		}

		protected virtual void OnKeyDownCore(MapViewKeyEventArgs k) { }

		protected virtual void OnKeyUpCore(MapViewKeyEventArgs mapViewKeyEventArgs) { }

		private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnMapPropertyChangedCore(sender, e);
		}

		protected virtual void
			OnMapPropertyChangedCore(object sender, PropertyChangedEventArgs e) { }

		protected virtual void ShowOptionsPane() { }

		protected virtual void HideOptionsPane() { }

		protected abstract bool HandleEscape();

		protected abstract void LogUsingCurrentSelection();

		protected abstract void LogPromptForSelection();

		protected virtual bool CanUseSelection([NotNull] IEnumerable<Feature> selectedFeatures)
		{
			return selectedFeatures.Any(CanSelectFeatureGeometryType);
		}

		protected bool CanSelectFeatureGeometryType([NotNull] Feature feature)
		{
			GeometryType shapeType = feature.GetTable().GetDefinition().GetShapeType();

			return CanSelectGeometryType(shapeType);
		}

		protected virtual void AfterSelection([NotNull] IList<Feature> selectedFeatures,
		                                      [CanBeNull] CancelableProgressor progressor) { }

		private void ProcessSelection([NotNull] IEnumerable<Feature> selectedFeatures,
		                              [CanBeNull] CancelableProgressor progressor = null)
		{
			// TODO: currently the selection is retrieved twice. Testing the selection should 
			// in the success case return it so that it can be passed to AfterSelection
			// BUT: some genius tools require the selection to be grouped by layer
			IList<Feature> selection = selectedFeatures.ToList();

			if (! CanUseSelection(selection))
			{
				LogPromptForSelection();
				StartSelectionPhase();
				return;
			}

			LogUsingCurrentSelection();

			AfterSelection(selection, progressor);
		}

		protected void HandleError(string message, Exception e, bool noMessageBox = false)
		{
			_msg.Error(message, e);

			if (! noMessageBox)
			{
				Application.Current.Dispatcher.Invoke(
					() =>
					{
						MessageBox.Show(message, "Error", MessageBoxButton.OK,
						                MessageBoxImage.Error);
					});
			}
		}

		protected void SetCursor([CanBeNull] Cursor cursor)
		{
			if (cursor != null)
			{
				Cursor = cursor;
			}
		}

		private bool CanSelectFromLayer(Layer layer)
		{
			var featureLayer = layer as FeatureLayer;

			if (featureLayer == null)
			{
				return false;
			}

			if (! featureLayer.IsVisible)
			{
				return false;
			}

			if (! featureLayer.IsSelectable)
			{
				return false;
			}

			if (! CanSelectGeometryType(
				    GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType)))
			{
				return false;
			}

			if (featureLayer.GetFeatureClass() == null)
			{
				return false;
			}

			return CanSelectFromLayerCore(featureLayer);
		}

		protected IEnumerable<Feature> GetApplicableSelectedFeatures(
			[NotNull] IEnumerable<Feature> selectedFeatures)
		{
			foreach (Feature feature in selectedFeatures)
			{
				GeometryType shapeType = feature.GetTable().GetDefinition().GetShapeType();

				if (! CanSelectGeometryType(shapeType))
				{
					continue;
				}

				yield return feature;
			}
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

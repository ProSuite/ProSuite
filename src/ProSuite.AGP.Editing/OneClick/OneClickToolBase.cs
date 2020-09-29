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

			CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);
		}

		private SketchingMoveType SketchingMoveType { get; set; }

		protected SelectionMode SelectionMode { get; set; }

		protected bool RequiresSelection { get; set; } = true;

		protected virtual SelectionSettings SelectionSettings { get; set; }

		protected List<Key> HandledKeys { get; } = new List<Key>();

		protected Cursor SelectionCursor { get; set; }
		protected Cursor SelectionCursorShift { get; set; }

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
					return await QueuedTaskUtils.Run(() => OnSelectionSketchComplete(
						                                 sketchGeometry, progressor));
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
			SketchType = SelectionSettings.SketchGeometryType;

			UseSnapping = false;

			GeomIsSimpleAsFeature = false;
			CompleteSketchOnMouseUp = true;

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

		private SketchingMoveType GetSketchingMoveType(Geometry geometry)
		{
			if (geometry.Extent.Width > 0 || geometry.Extent.Height > 0)
			{
				return SketchingMoveType.Drag;
			}

			return SketchingMoveType.Click;
		}

		private async Task<bool> OnSelectionSketchComplete(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			DisposeOverlays();
			//3D views only support selecting features interactively using geometry
			//in screen coordinates relative to the top-left corner of the view.

			Geometry selectionGeometry = sketchGeometry;

			CIMPolygonSymbol highlightPolygonSymbol = CreatePolygonSymbol();

			// TODO: Add Utils method to KeyboardUtils to do it in the WPF way
			SelectionCombinationMethod selectionMethod =
				KeyboardUtils.IsModifierPressed(Keys.Shift)
					? SelectionCombinationMethod.XOR
					: SelectionCombinationMethod.New;

			if (SketchingMoveType == SketchingMoveType.Click)
			{
				MapPoint sketchPoint = CreatPointFromSketchPolygon(sketchGeometry);

				selectionGeometry =
					BufferGeometryByPixels(sketchPoint,
					                       SelectionSettings.SelectionTolerancePixels);

				// select all features spatially related with selectionGeometry
				Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer =
					FindFeaturesOfAllLayers(selectionGeometry);

				if (SelectionMode == SelectionMode.Original) //alt was pressed: select all xy
				{
					Selector.SelectLayersFeaturesByOids(featuresPerLayer, selectionMethod);
				}
				else //select a single feature using feature reduction, and picker if necessary
				{
					
					KeyValuePair<BasicFeatureLayer, List<long>> featuresOfLayer =
						ReduceFeatures(featuresPerLayer);

					//TODO if still several selection candidates -> present picker here
					if (featuresOfLayer.Value.Count() > 1)
					{
						List<IPickableItem> pickables = new List<IPickableItem>();

						foreach (var feature in MapUtils.GetFeatures(featuresOfLayer))
						{
							string text = $"{featuresOfLayer.Key.Name}: {feature.GetObjectID()}";
							PickableFeatureItem featureItem = new PickableFeatureItem(featuresOfLayer.Key,feature, text);
							pickables.Add(featureItem);
						}

						Point pickerWindowLocation =
							await QueuedTask.Run(
								() => MapView.Active.MapToScreen(selectionGeometry.Extent.Center));

						var picker = new PickerUI.Picker(pickables, pickerWindowLocation);

						var item = await picker.PickSingle() as PickableFeatureItem;
					}

					Selector.SelectLayersFeaturesByOids(featuresOfLayer, selectionMethod);
				}
			}

			if (SketchingMoveType == SketchingMoveType.Drag)
			{
				selectionGeometry = sketchGeometry;

				//CTRL was pressed: picker shows fclasses to select from
				if (SelectionMode == SelectionMode.UserSelect)
				{
					List<FeatureClassInfo> featureClassInfos =
						Selector.GetSelectableFeatureclassInfos();

					List<IPickableItem> pickableItems = PickableItemAdapter.Get(featureClassInfos);

					Point pickerWindowLocation =
						await QueuedTask.Run(
							() => MapView.Active.MapToScreen(selectionGeometry.Extent.Center));

					var picker = new PickerUI.Picker(pickableItems, pickerWindowLocation);

					var item = await picker.PickSingle() as PickableFeatureClassItem;

					item.BelongingFeatureLayers.ForEach(layer =>
					{
						layer.Select(null, selectionMethod);
					});
				}
				else //select all in envelope
				{
					Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer =
						FindFeaturesOfAllLayers(selectionGeometry);

					Selector.SelectLayersFeaturesByOids(featuresPerLayer, selectionMethod);
				}
			}

			// AddOverlay(selectionGeometry, highlightPolygonSymbol);

			SelectionMode = SelectionMode.Normal;

			ProcessSelection(SelectionUtils.GetSelectedFeatures(ActiveMapView), progressor);

			// else: feedback to the user to keep selecting
			return true;
		}

		private KeyValuePair<BasicFeatureLayer, List<long>> ReduceFeatures(
			Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer)
		{
			//Dictionary<BasicFeatureLayer, List<long> > featuresPerLayer = new Dictionary<BasicFeatureLayer, List<long>>();

			IOrderedEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> ordered =
				featuresPerLayer.OrderBy(el => el.Key.ShapeType, new GeometryTypeComparer());

			foreach (KeyValuePair<BasicFeatureLayer, List<long>> keyValuePair in ordered)
			{
				if (keyValuePair.Value.Any())
				{
					return keyValuePair;
				}
			}

			return featuresPerLayer.First();
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
					IEnumerable<Feature> features =
						MapUtils.FilterFeaturesByGeometry(layer, selectionGeometry,
						                                  SelectionSettings.SpatialRelationship);
					IEnumerable<long> oids = MapUtils.GetFeaturesOidList(features);

					if (oids.Any())
					{
						featuresPerLayer.Add(layer, oids.ToList());
					}
				}
			}

			return featuresPerLayer;
		}

		private MapPoint CreatPointFromSketchPolygon(Geometry sketchGeometry)
		{
			var clickCoord =
				new Coordinate2D(sketchGeometry.Extent.XMin, sketchGeometry.Extent.YMin);

			MapPoint sketchPoint =
				MapPointBuilder.CreateMapPoint(clickCoord, ActiveMapView.Map.SpatialReference);
			return sketchPoint;
		}

		private Geometry BufferGeometryByPixels(Geometry sketchGeometry, int pixelBufferDistance)
		{
			double bufferDistance = MapUtils.ConvertScreenPixelToMapLength(pixelBufferDistance);
			Geometry selectionGeometry =
				GeometryEngine.Instance.Buffer(sketchGeometry, bufferDistance);
			return selectionGeometry;
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
			// TODO
			return selectedFeatures.Any();
		}

		protected virtual void AfterSelection(IList<Feature> selectedFeatures,
		                                      CancelableProgressor progressor) { }

		protected void ProcessSelection(IEnumerable<Feature> selectedFeatures,
		                                CancelableProgressor progressor = null)
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

			if (! featureLayer.IsSelectable)
			{
				return false;
			}

			return featureLayer.GetFeatureClass() != null && CanSelectFromLayerCore(featureLayer);
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

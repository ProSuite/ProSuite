using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class OneClickToolBase : MapToolBase, ISketchTool
	{
		private const Key _keyPolygonDraw = Key.P;
		private const Key _keyLassoDraw = Key.L;

		private readonly TimeSpan _sketchBlockingPeriod = TimeSpan.FromSeconds(1);

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private Geometry _lastSketch;
		private DateTime _lastSketchFinishedTime;

		[NotNull] private SketchAndCursorSetter _selectionSketchCursor;

		// ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
		protected OneClickToolBase()
		{
			ContextMenuID = "esri_mapping_selection2DContextMenu";

			HandledKeys.Add(_keyLassoDraw);
			HandledKeys.Add(_keyPolygonDraw);
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
		/// Flag to indicate that currently the selection is changed by the <see cref="OnSelectionSketchCompleteAsync"/> method.
		/// </summary>
		protected bool IsCompletingSelectionSketch { get; set; }

		#region Overrides of MapToolBase

		protected override async Task OnToolActivateAsyncCore(bool hasMapViewChanged)
		{
			MapPropertyChangedEvent.Subscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Subscribe(OnMapSelectionChangedAsync);
			EditCompletedEvent.Subscribe(OnEditCompletedAsync);

			using var source = GetProgressorSource();
			var progressor = source?.Progressor;

			await QueuedTaskUtils.Run(async () =>
			{
				SetupCursors();

				await OnToolActivatingCoreAsync();

				if (RequiresSelection)
				{
					await ProcessSelectionAsync(progressor);
				}

				// TODO MapToolBase: Consider pulling up (and take out of QueuedTask)
				// ReSharper disable once MethodHasAsyncOverload
				return OnToolActivatedCore(hasMapViewChanged) &&
				       await OnToolActivatedCoreAsync(hasMapViewChanged);
			}, progressor);
		}

		#endregion

		private void SetupCursors()
		{
			_selectionSketchCursor =
				SketchAndCursorSetter.Create(this,
				                             GetSelectionCursor(),
				                             GetSelectionCursorLasso(),
				                             GetSelectionCursorPolygon(),
				                             GetSelectionSketchGeometryType(),
				                             DefaultSketchTypeOnFinishSketch);

			_selectionSketchCursor.SetSelectionCursorShift(GetSelectionCursorShift());
			_selectionSketchCursor.SetSelectionCursorLassoShift(GetSelectionCursorLassoShift());
			_selectionSketchCursor.SetSelectionCursorPolygonShift(GetSelectionCursorPolygonShift());
		}

		protected virtual bool DefaultSketchTypeOnFinishSketch =>
			GetSelectionSettings().PreferRectangleSelectionSketch;

		// TODO: Pull the correct cursor from a cursor collection but do not allow external access
		public void SetCursor(Cursor cursor)
		{
			SetToolCursor(cursor);
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

		protected override async Task OnToolDeactivateAsyncCore(bool hasMapViewChanged)
		{
			MapPropertyChangedEvent.Unsubscribe(OnPropertyChanged);
			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChangedAsync);
			EditCompletedEvent.Unsubscribe(OnEditCompletedAsync);

			// TODO: Async but not in QueuedTask (or separate OnToolDeactivateAsyncCoreQueued
			await QueuedTask.Run(() => OnToolDeactivateCore(hasMapViewChanged));
		}

		protected virtual Task SetupLassoSketchAsync()
		{
			_selectionSketchCursor.Toggle(SketchGeometryType.Lasso, KeyboardUtils.IsShiftDown());

			return Task.CompletedTask;
		}

		protected virtual Task SetupPolygonSketchAsync()
		{
			_selectionSketchCursor.Toggle(SketchGeometryType.Polygon, KeyboardUtils.IsShiftDown());

			return Task.CompletedTask;
		}

		protected override async Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			if (args.Key == _keyPolygonDraw)
			{
				await SetupPolygonSketchAsync();
			}

			if (args.Key == _keyLassoDraw)
			{
				await SetupLassoSketchAsync();
			}

			if (KeyboardUtils.IsShiftKey(args.Key))
			{
				await ShiftReleasedAsync();
			}
		}

		protected override async Task OnToolDoubleClickCoreAsync(MapViewMouseButtonEventArgs args)
		{
			// Typically, shift is pressed which prevents the standard finish-sketch.
			// We want to override this in specific situations, such as in intermittent
			// selection phases with polygon sketches.
			if (await FinishSketchOnDoubleClick())
			{
				_msg.VerboseDebug(() => "Calling finish sketch due to double-click...");
				await FinishSketchAsync();
			}
		}

		/// <summary>
		/// During the selection phase or some other (target selection) phase FinishSketch is not
		/// always automatically called when double-clicking (typically Shift prevents this).
		/// This method allows for defining whether the current double click shall result
		/// in a call to FinishSketchAsync()
		/// </summary>
		/// <returns></returns>
		protected virtual async Task<bool> FinishSketchOnDoubleClick()
		{
			return SketchType == SketchGeometryType.Polygon && await IsInSelectionPhaseAsync();
		}

		protected override async Task<bool> OnSketchFinishedAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			_msg.VerboseDebug(() => $"OnSketchCompleteAsync ({Caption})");

			try
			{
				sketchGeometry = GetSimplifiedSketch(sketchGeometry);

				if (sketchGeometry == null)
				{
					return false;
				}

				if (IsDuplicateSketchCompleteInvocation(sketchGeometry))
				{
					return false;
				}

				if (RequiresSelection && await IsInSelectionPhaseAsync())
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

		protected virtual Task<bool> OnSketchCompleteCoreAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			return Task.FromResult(true);
		}

		protected override async Task ShiftPressedAsync()
		{
			if (await IsInSelectionPhaseAsync())
			{
				_selectionSketchCursor.SetCursor(GetSketchType(), shiftDown: true);
			}

			await ShiftPressedCoreAsync();
		}

		private async Task ShiftReleasedAsync()
		{
			if (await IsInSelectionPhaseAsync())
			{
				_selectionSketchCursor.SetCursor(GetSketchType(), shiftDown: false);
			}

			await ShiftReleasedCoreAsync();
		}

		/// <summary>
		/// Allows implementors to start tasks when the shift key is pressed.
		/// NOTE: ShiftPressedCoreAsync and ShiftReleasedAsync are not necessarily symmetrical!
		/// </summary>
		/// <returns></returns>
		protected virtual Task ShiftPressedCoreAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Allows implementors to start tasks when the shift key is released. Do not Assume that
		/// ShiftPressedCoreAsync has been called before!
		/// </summary>
		/// <returns></returns>
		protected virtual Task ShiftReleasedCoreAsync()
		{
			return Task.CompletedTask;
		}

		protected async Task StartSelectionPhaseAsync()
		{
			await SetupSelectionSketchAsync();

			await OnSelectionPhaseStartedAsync();
		}

		protected async Task<bool> HasSketchAsync()
		{
			Geometry currentSketch = await GetCurrentSketchAsync();

			return currentSketch?.IsEmpty == false;
		}

		protected async Task SetupSelectionSketchAsync()
		{
			if (await HasSketchAsync())
			{
				await ActiveMapView.ClearSketchAsync();
			}

			SetupSketch();

			_selectionSketchCursor.ResetOrDefault();
		}

		protected void SetupSketch(SketchOutputMode sketchOutputMode = SketchOutputMode.Map,
		                           bool useSnapping = false,
		                           bool completeSketchOnMouseUp = true,
		                           bool enforceSimpleSketch = false)
		{
			_msg.VerboseDebug(
				() =>
					$"Setting up sketch with type {SketchType}, output mode {sketchOutputMode}, " +
					$"snapping: {useSnapping}, completeSketchOnMouseUp: {completeSketchOnMouseUp}, " +
					$"enforceSimplifySketch: {enforceSimpleSketch}");

			// screen coords are currently not supported and only relevant
			// when selecting with the View being in 3D viewing mode
			SketchOutputMode = sketchOutputMode;

			// Note: set CompleteSketchOnMouseUp before SketchType, or it has no effect
			CompleteSketchOnMouseUp = completeSketchOnMouseUp;

			UseSnapping = useSnapping;

			GeomIsSimpleAsFeature = enforceSimpleSketch;
		}

		protected abstract SketchGeometryType GetSelectionSketchGeometryType();

		protected virtual Task OnSelectionPhaseStartedAsync()
		{
			return Task.CompletedTask;
		}

		private async void OnMapSelectionChangedAsync(MapSelectionChangedEventArgs args)
		{
			// NOTE: This method is called repeatedly with different selection sets during the
			//       OnSelectionSketchCompleteAsync method. Therefore, the flag is set to prevent
			//       multiple calls to the AfterSelectionMethod with intermediate results!
			//       The ProcessSelectionAsync method is called at the end of the sketch completion.

			try
			{
				_msg.VerboseDebug(() => $"OnMapSelectionChangedAsync ({Caption})");

				if (IsCompletingSelectionSketch)
				{
					return;
				}

				await QueuedTask.Run(() => OnMapSelectionChangedCoreAsync(args));
			}
			catch (Exception e)
			{
				_msg.Error($"Error while handling selection change: {e.Message}", e);
			}
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
		protected virtual Task OnToolActivatingCoreAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Synchronous method called on the MCT after the tool has been activated.
		/// </summary>
		/// <param name="hasMapViewChanged"></param>
		/// <returns></returns>
		protected virtual bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			return true;
		}

		/// <summary>
		/// Async method called on the MCT after the tool has been activated.
		/// </summary>
		/// <param name="hasMapViewChanged"></param>
		/// <returns></returns>
		/// <remarks>Will be called on MCT</remarks>
		protected virtual Task<bool> OnToolActivatedCoreAsync(bool hasMapViewChanged)
		{
			return Task.FromResult(true);
		}

		protected virtual void OnToolDeactivateCore(bool hasMapViewChanged) { }

		/// <remarks>Will be called on MCT</remarks>
		protected virtual Task<bool> OnMapSelectionChangedCoreAsync(
			MapSelectionChangedEventArgs args)
		{
			return Task.FromResult(true);
		}

		protected int GetSelectionTolerancePixels()
		{
			return SelectionEnvironment.SelectionTolerance;
		}

		private async Task<bool> OnSelectionSketchCompleteAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			try
			{
				IsCompletingSelectionSketch = true;

				using var precedence = CreatePickerPrecedence(sketchGeometry);

				await QueuedTaskUtils.Run(async () =>
				{
					var candidates =
						FindFeaturesOfAllLayers(precedence.GetSelectionGeometry(),
						                        precedence.SpatialRelationship).ToList();

					List<IPickableItem> items =
						await PickerUtils.GetItemsAsync(candidates, precedence);

					await OnItemsPickedAsync(items, precedence);

					await ProcessSelectionAsync(progressor);
				}, progressor);
			}
			finally
			{
				IsCompletingSelectionSketch = false;
			}

			return true;
		}

		protected virtual Task OnItemsPickedAsync([NotNull] List<IPickableItem> items,
		                                          [NotNull] IPickerPrecedence precedence)
		{
			OnSelecting();

			PickerUtils.Select(items, precedence.SelectionCombinationMethod);

			return Task.CompletedTask;
		}

		protected virtual void OnSelecting() { }

		protected virtual IPickerPrecedence CreatePickerPrecedence(
			[NotNull] Geometry sketchGeometry)
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

		protected Task<bool> IsInSelectionPhaseAsync()
		{
			bool shiftDown = KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			                 KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			return ViewUtils.TryAsync(IsInSelectionPhaseCoreAsync(shiftDown), _msg);
		}

		protected virtual Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			return Task.FromResult(false);
		}

		protected virtual void OnPropertyChanged(MapPropertyChangedEventArgs args) { }

		protected abstract SelectionSettings GetSelectionSettings();

		protected abstract void LogUsingCurrentSelection();

		protected abstract void LogPromptForSelection();

		protected bool CanSelectFeatureGeometryType([NotNull] Feature feature)
		{
			using var featureClass = feature.GetTable();
			GeometryType shapeType = featureClass.GetShapeType();

			return CanSelectGeometryType(shapeType);
		}

		/// <remarks>Will be called on MCT</remarks>>
		protected virtual Task AfterSelectionAsync([NotNull] IList<Feature> selectedFeatures,
		                                           [CanBeNull] CancelableProgressor progressor)
		{
			return Task.CompletedTask;
		}

		/// <remarks>Must be called on MCT</remarks>
		protected async Task ProcessSelectionAsync(
			[CanBeNull] CancelableProgressor progressor = null)
		{
			var selectionByLayer = SelectionUtils.GetSelection(ActiveMapView.Map);

			var notifications = new NotificationCollection();
			var applicableSelection =
				GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection,
				                                      notifications).ToList();

			int selectionCount = selectionByLayer.Sum(kvp => kvp.Value.Count);

			if (applicableSelection.Count > 0 &&
			    (AllowNotApplicableFeaturesInSelection ||
			     applicableSelection.Count == selectionCount))
			{
				LogUsingCurrentSelection();

				await AfterSelectionAsync(applicableSelection, progressor);
			}
			else
			{
				if (selectionCount > 0)
				{
					_msg.DebugFormat(notifications.Concatenate(Environment.NewLine));
				}

				LogPromptForSelection();
				await StartSelectionPhaseAsync();
			}
		}

		/// <summary>
		/// Whether the selection shall be retrieved without the join even if the layer is joined.
		/// This is important for updating features. Features based on a joined table throw an
		/// exception when setting the shape (GOTOP-190)!
		/// </summary>
		protected bool UnJoinedSelection { get; set; } = true;

		public void UpdateCursors()
		{
			SetupCursors();
			_selectionSketchCursor.ResetOrDefault();
		}

		protected bool CanSelectFromLayer([CanBeNull] Layer layer,
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

			using (FeatureClass featureClass = featureLayer.GetFeatureClass())
			{
				if (featureClass is null)
				{
					NotificationUtils.Add(notifications,
					                      $"Layer has no valid data source: {layerName}");
					return false;
				}
			}

			return CanSelectFromLayerCore(featureLayer, notifications);
		}

		protected bool CanUseSelection([NotNull] MapView mapView)
		{
			if (mapView is null)
				throw new ArgumentNullException(nameof(mapView));

			Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
				SelectionUtils.GetSelection<BasicFeatureLayer>(mapView.Map);

			return CanUseSelection(selectionByLayer);
		}

		protected virtual bool CanUseSelection(
			[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
			[CanBeNull] NotificationCollection notifications = null)
		{
			return AllowNotApplicableFeaturesInSelection
				       ? selectionByLayer.Any(l => CanSelectFromLayer(l.Key, notifications))
				       : selectionByLayer.All(l => CanSelectFromLayer(l.Key, notifications));
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

		protected virtual bool CanSelectFromLayerCore(
			[NotNull] BasicFeatureLayer basicFeatureLayer,
			[CanBeNull] NotificationCollection notifications)
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

		public void SetSketchType(SketchGeometryType? sketchType)
		{
			SketchType = sketchType;
		}

		public SketchGeometryType? GetSketchType()
		{
			return SketchType;
		}

		protected virtual Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Cross,
			                              Resources.SelectOverlay, 10, 10);
		}

		protected virtual Cursor GetSelectionCursorShift()
		{
			return
				ToolUtils.CreateCursor(Resources.Cross, Resources.SelectOverlay,
				                       Resources.Shift, null, 10, 10);
		}

		protected virtual Cursor GetSelectionCursorLasso()
		{
			return
				ToolUtils.CreateCursor(Resources.Cross, Resources.SelectOverlay,
				                       Resources.Lasso, null, 10, 10);
		}

		protected virtual Cursor GetSelectionCursorLassoShift()
		{
			return
				ToolUtils.CreateCursor(Resources.Cross, Resources.SelectOverlay,
				                       Resources.Lasso, Resources.Shift, 10, 10);
		}

		protected virtual Cursor GetSelectionCursorPolygon()
		{
			return
				ToolUtils.CreateCursor(Resources.Cross, Resources.SelectOverlay,
				                       Resources.Polygon, null, 10, 10);
		}

		protected virtual Cursor GetSelectionCursorPolygonShift()
		{
			return
				ToolUtils.CreateCursor(Resources.Cross, Resources.SelectOverlay,
				                       Resources.Polygon, Resources.Shift, 10, 10);
		}

		/// <summary>
		/// Returns a simplified sketch geometry of the correct geometry type.
		/// NOTE: This method can return a different geometry type in the single click case.
		/// </summary>
		/// <param name="sketchGeometry"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		[CanBeNull]
		private Geometry GetSimplifiedSketch([CanBeNull] Geometry sketchGeometry)
		{
			if (sketchGeometry == null || sketchGeometry.IsEmpty)
			{
				_msg.VerboseDebug(() => $"{Caption}: Null or empty sketch");
				return null;
			}

			Geometry simplified = GeometryUtils.Simplify(sketchGeometry);

			if (! simplified.IsEmpty)
			{
				return simplified;
			}

			if (sketchGeometry is Polygon sketchPolygon)
			{
				// Convert polygon sketch to point sketch because the picker does not test for
				// single click anymore, just for point geometry.
				if (ToolUtils.IsSingleClickSketch(simplified))
				{
					Assert.False(sketchGeometry.PointCount == 0,
					             "Non empty single click sketch without points");

					return sketchPolygon.Points.First();
				}
			}

			throw new AssertionException(
				"Empty sketch after simplify in non-single-click scenario.");
		}

		/// <summary>
		/// Determines whether the sketch completion is a duplicate call that should be ignored.
		/// </summary>
		/// <param name="sketchGeometry"></param>
		/// <returns></returns>
		private bool IsDuplicateSketchCompleteInvocation([CanBeNull] Geometry sketchGeometry)
		{
			// NOTE: This still happens occasionally. This is not a reentrancy problem. The sketch
			//       completion is called twice in a row by the framework.

			if (sketchGeometry == null || sketchGeometry.IsEmpty)
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

				return true;
			}

			// Remember state for next call:
			_lastSketch = sketchGeometry;
			_lastSketchFinishedTime = DateTime.Now;

			return false;
		}

		protected async Task<bool> NonEmptySketchAsync()
		{
			return await GetCurrentSketchAsync() is { IsEmpty: false };
		}

		protected async Task<bool> NonEmptyPolygonSketchAsync()
		{
			return SketchType == SketchGeometryType.Polygon && await NonEmptySketchAsync();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.AdvancedReshapeReshape;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class AdvancedReshapeToolBase : ConstructionToolBase, ISymbolizedSketchTool
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// TODO:
		// - Performance improvements for large objects (clip extent)
		// - Performance improvement for (large) polygon feedback (no simplify etc.)
		// - Make sure feedback microservices are called only once
		// - Move to OnKeyUp in reshape side toggle
		// - Options
		// - Circumcision dialog6
		// - Reshape size change logging - use abbreviations for display units
		// - R(estore) sketch
		// - Connected lines reshape
		// - Update feedback on toggle layer visibility

		[CanBeNull] private AdvancedReshapeFeedback _feedback;
		[CanBeNull] private SymbolizedSketchTypeBasedOnSelection _symbolizedSketch;

		private ReshapeToolOptions _advancedReshapeToolOptions;
		private OverridableSettingsProvider<PartialReshapeToolOptions> _settingsProvider;

		private Task<bool> _updateFeedbackTask;

		private bool _nonDefaultSideMode;
		private CancellationTokenSource _cancellationTokenSource;
		private const Key _keyToggleNonDefaultSide = Key.S;
		private const Key _keyToggleMoveEndJunction = Key.M; //Is this needed?

		protected virtual string OptionsFileName => "AdvancedReshapeToolOptions.xml";

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		protected abstract string LocalConfigDir { get; }

		protected AdvancedReshapeToolBase()
		{
			FireSketchEvents = true;

			RequiresSelection = true;

			HandledKeys.Add(_keyToggleNonDefaultSide);
			HandledKeys.Add(_keyToggleMoveEndJunction);
		}

		protected abstract IAdvancedReshapeService MicroserviceClient { get; }

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override Task HandleEscapeAsync()
		{
			_cancellationTokenSource?.Cancel();

			return base.HandleEscapeAsync();
		}

		protected override void LogPromptForSelection()
		{
			// TODO: Only usable selection
			bool hasSelection = ActiveMapView.Map.SelectionCount > 0;

			// If the last reshape was just finished and the feature is still selected, Enter is entering the sketch mode again using the same selection
			string enterMsg =
				hasSelection
					? LocalizableStrings.AdvancedReshapeTool_LogPromptForSelection_Enter
					: string.Empty;

			_msg.InfoFormat(LocalizableStrings.AdvancedReshapeTool_LogPromptForSelection,
			                enterMsg);
		}

		protected override void LogEnteringSketchMode()
		{
			string logText = LocalizableStrings.AdvancedReshapeTool_LogEnteringSketchMode;
			//	"Sketch the reshape line to change the selection.<br>- Press R to restore the sketch from the previous reshape operation.<br>- Press S to change the reshape side of the geometry.<br>- Press ESC to select different features.";

			int selectionCount = ActiveMapView.Map.SelectionCount;

			if (selectionCount > 1)
			{
				// What about lines? Idea: hyperlinks that open the relevant page in the help
				logText +=
					string.Format(
						"{0}- Press N to define the target connection point for the extended shared boundary when reshaping two polygons",
						Environment.NewLine);
			}

			_msg.Info(logText);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon;
		}

		protected override bool CanSelectFromLayerCore(BasicFeatureLayer layer)
		{
			return layer is FeatureLayer;
		}

		protected override async void OnToolActivatingCore()
		{
			InitializeOptions();
			_feedback = new AdvancedReshapeFeedback(_advancedReshapeToolOptions);

			base.OnToolActivatingCore();
		}

		protected void InitializeOptions()
		{
			_settingsProvider = new OverridableSettingsProvider<PartialReshapeToolOptions>(
				CentralConfigDir, LocalConfigDir, OptionsFileName);

			PartialReshapeToolOptions localConfiguration, centralConfiguration;
			_settingsProvider.GetConfigurations(out localConfiguration, out centralConfiguration);

			_advancedReshapeToolOptions =
				new ReshapeToolOptions(centralConfiguration, localConfiguration);
		}

		protected override bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			_symbolizedSketch =
				new SymbolizedSketchTypeBasedOnSelection(this);
			_symbolizedSketch.SetSketchAppearanceBasedOnSelection();

			return base.OnToolActivatedCore(hasMapViewChanged);
		}

		protected override void OnSelectionPhaseStarted()
		{
			base.OnSelectionPhaseStarted();
			_symbolizedSketch?.ClearSketchSymbol();
			_feedback?.Clear();
		}

		protected override void OnSketchPhaseStarted()
		{
			try
			{
				QueuedTask.Run(() => { _symbolizedSketch?.SetSketchAppearanceBasedOnSelection(); });
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message, ex);
			}
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider.StoreLocalConfiguration(_advancedReshapeToolOptions.LocalOptions);

			_symbolizedSketch?.Dispose();
			_feedback?.Clear();
			_feedback = null;

			base.OnToolDeactivateCore(hasMapViewChanged);
		}

		protected override CancelableProgressorSource GetProgressorSource()
		{
			// Disable the progressor because reshaping is typically fast,
			// and the users potentially want to continue working already.
			return null;
		}

		protected override SketchGeometryType GetSketchGeometryType()
		{
			return SketchGeometryType.Line;
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override async Task<bool> OnSketchModifiedAsync()
		{
			_msg.VerboseDebug(() => "OnSketchModifiedAsync");

			// Does it make any difference what the return value is?
			bool result = await ViewUtils.TryAsync(TryUpdateFeedbackAsync(), _msg, true);

			result &= await base.OnSketchModifiedAsync();

			return result;
		}

		protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
		{
			try
			{
				await base.HandleKeyDownAsync(args);

				if (args.Key == _keyToggleNonDefaultSide)
				{
					_nonDefaultSideMode = ! _nonDefaultSideMode;

					_msg.Info(_nonDefaultSideMode
						          ? "Enabled non-default reshape mode. The next reshape to the inside of a polygon will remove the larger area. The next Y-Reshape will use the furthest end-point."
						          : "Disabled non-default reshape mode");

					if (_updateFeedbackTask != null)
					{
						// Still working on the previous update (large polygons!)
						return;
					}

					_updateFeedbackTask = UpdateFeedbackAsync(_nonDefaultSideMode);

					await _updateFeedbackTask;
				}

				if (args.Key == _keyToggleMoveEndJunction)
				{
					_msg.Info("Toggle MoveOpenJawEndJunction");
					_advancedReshapeToolOptions.MoveOpenJawEndJunction =
						! _advancedReshapeToolOptions.MoveOpenJawEndJunction;
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error generating preview", e);
			}
			finally
			{
				_updateFeedbackTask = null;
			}
		}

		//protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		//{
		//	_msg.VerboseDebug(() => "OnKeyUpCore");

		//	if (k.Key == _keyToggleNonDefaultSide)
		//	{
		//		_nonDefaultSideMode = ! _nonDefaultSideMode;

		//		k.Handled = true;
		//	}
		//	else if (k.Key == Key.Space)
		//	{
		//		k.Handled = true;
		//	}

		//	base.OnKeyUpCore(k);
		//}

		//protected override async Task HandleKeyUpAsync(MapViewKeyEventArgs k)
		//{
		//	// At 2.5 this is never called (despite setting k.Handled = true above).
		// TODO: Test in 2.6/2.7
		//	try
		//	{
		//		if (k.Key == _keyToggleNonDefaultSide ||
		//		    k.Key == Key.Space)
		//		{
		//			_updateFeedbackTask = UpdateFeedbackAsync(_nonDefaultSideMode);

		//			await _updateFeedbackTask;
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		_msg.Warn("Error generating preview", e);
		//	}
		//	finally
		//	{
		//		_updateFeedbackTask = null;
		//	}
		//}

		public bool CanSelectFromLayer(Layer layer)
		{
			return base.CanSelectFromLayer(layer);
		}

		public bool CanUseSelection(Dictionary<BasicFeatureLayer, List<long>> selectionByLayer)
		{
			return base.CanUseSelection(selectionByLayer);
		}

		public bool CanSetConstructionSketchSymbol(GeometryType geometryType)
		{
			bool result;
			switch (geometryType)
			{
				case GeometryType.Polyline:
					result = true;
					break;
				case GeometryType.Point:
				case GeometryType.Polygon:
				case GeometryType.Unknown:
				case GeometryType.Envelope:
				case GeometryType.Multipoint:
				case GeometryType.Multipatch:
				case GeometryType.GeometryBag:
					result = false;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
			}

			return result && ! IsInSelectionPhaseAsync().Result;
		}

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry, EditingTemplate editTemplate, MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			_feedback?.Clear();

			// TODO: cancel all running background tasks...

			var polyline = (Polyline) sketchGeometry;

			bool success = await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					SetCursor(Cursors.Wait);

					Dictionary<MapMember, List<long>> selectionByLayer =
						SelectionUtils.GetSelection(activeView.Map);

					List<Feature> selection =
						GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection)
							.ToList();

					var potentiallyAffectedFeatures =
						GetAdjacentFeatures(selection, cancelableProgressor);

					// This timeout should be enough even in extreme circumstances:
					int timeout = selection.Count * 10000;
					_cancellationTokenSource = new CancellationTokenSource(timeout);

					ReshapeResult result = MicroserviceClient.Reshape(
						selection, polyline, potentiallyAffectedFeatures, true, true,
						_nonDefaultSideMode, _cancellationTokenSource.Token);

					if (result == null)
					{
						return false;
					}

					if (result.ResultFeatures.Count == 0)
					{
						if (! string.IsNullOrEmpty(result.FailureMessage))
						{
							_msg.Warn(result.FailureMessage);
							return false;
						}
					}

					HashSet<long> editableClassHandles =
						ToolUtils.GetEditableClassHandles(activeView);

					Dictionary<Feature, Geometry> resultFeatures =
						result.ResultFeatures
						      .Where(r => GdbPersistenceUtils.CanChange(
							             r, editableClassHandles, RowChangeType.Update))
						      .ToDictionary(r => r.OriginalFeature, r => r.NewGeometry);

					success = await SaveAsync(resultFeatures);

					LogReshapeResults(result, selection.Count);

					// At some point, hopefully, read-only operations on the CIM model can run in parallel
					await ToolUtils.FlashResultPolygonsAsync(activeView, resultFeatures);

					return success;
				}
				finally
				{
					// Anything but the Wait cursor
					SetCursor(Cursors.Arrow);
				}
			});

			_nonDefaultSideMode = false;

			//if (!_advancedReshapeOptions.RemainInSketchMode)
			{
				StartSelectionPhase();
			}

			return success; // taskSave.Result;
		}

		protected override void OnSketchResetCore()
		{
			_feedback?.Clear();

			_nonDefaultSideMode = false;
		}

		private async Task<bool> TryUpdateFeedbackAsync()
		{
			if (_updateFeedbackTask != null)
			{
				// Still working on the previous update (large polygons!)
				// -> Consider using latch (but ensure that the overlay must probably be scheduled properly)
				return false;
			}

			bool nonDefaultSide =
				_nonDefaultSideMode || PressedKeys.Contains(_keyToggleNonDefaultSide);

			bool updated = false;

			try
			{
				if (! PressedKeys.Contains(Key.Space))
				{
					// TODO: Exclude finish sketch by double clicking -> should not calculate preview
					//       E.g. wait for SystemInformation.DoubleClickTime for the second click
					//       and only start if it has not occurred
					_updateFeedbackTask = UpdateFeedbackAsync(nonDefaultSide);
					updated = await _updateFeedbackTask;
				}
			}
			catch (Exception e)
			{
				_msg.Warn($"Error generating preview: {e.Message}", e);
				return false;
			}
			finally
			{
				_updateFeedbackTask = null;
			}

			return updated;
		}

		private async Task<bool> UpdateFeedbackAsync(bool nonDefaultSide)
		{
			var sketchPolyline = await GetCurrentSketchAsync() as Polyline;

			if (sketchPolyline == null || sketchPolyline.IsEmpty || sketchPolyline.PointCount < 2)
			{
				_feedback?.Clear();
				return true;
			}

			// Snapshot:
			MapView activeMapView = ActiveMapView;

			List<Feature> polylineSelection;
			List<Feature> polygonSelection;

			bool result =
				await QueuedTaskUtils.Run(
					async () =>
					{
						List<Feature> selection =
							GetApplicableSelectedFeatures(activeMapView).ToList();

						polylineSelection =
							GdbObjectUtils.Filter(selection, GeometryType.Polyline).ToList();

						polygonSelection =
							GdbObjectUtils.Filter(selection, GeometryType.Polygon).ToList();

						bool updated =
							await UpdateOpenJawReplacedEndpointAsync(nonDefaultSide, sketchPolyline,
								polylineSelection);

						updated |= await UpdatePolygonResultPreviewAsync(
							           nonDefaultSide, sketchPolyline,
							           polygonSelection);

						return updated;
					});

			return result;
		}

		[CanBeNull]
		private IList<Feature> GetAdjacentFeatures(
			[NotNull] ICollection<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancellabelProgressor)
		{
			Dictionary<MapMember, List<long>> selection =
				SelectionUtils.GetSelection(ActiveMapView.Map);

			if (! selection.Keys.Any(mm => mm is FeatureLayer fl &&
			                               fl.ShapeType == esriGeometryType.esriGeometryPolyline))
			{
				return null;
			}

			Envelope inExtent = ActiveMapView.Extent;

			// TODO: Use linear network classes as defined in reshape options
			TargetFeatureSelection targetFeatureSelection = TargetFeatureSelection.SameClass;

			var featureFinder = new FeatureFinder(ActiveMapView, targetFeatureSelection)
			                    {
				                    ReturnUnJoinedFeatures = true
			                    };

			IEnumerable<FeatureSelectionBase> featureClassSelections =
				featureFinder.FindIntersectingFeaturesByFeatureClass(
					selection, layer => layer.ShapeType == esriGeometryType.esriGeometryPolyline,
					inExtent, cancellabelProgressor);

			if (cancellabelProgressor != null &&
			    cancellabelProgressor.CancellationToken.IsCancellationRequested)
			{
				return new List<Feature>();
			}

			var foundFeatures = new List<Feature>();

			foreach (var keyValuePair in featureClassSelections)
			{
				foundFeatures.AddRange(keyValuePair.GetFeatures());
			}

			foundFeatures.RemoveAll(
				f => selectedFeatures.Any(s => GdbObjectUtils.IsSameFeature(f, s)));

			return foundFeatures;
		}

		private static void LogReshapeResults([NotNull] ReshapeResult reshapeResult,
		                                      int applicableSelectionCount)
		{
			var result = new Dictionary<Feature, Geometry>();

			foreach (var resultFeature in reshapeResult.ResultFeatures)
			{
				var feature = resultFeature.OriginalFeature;

				result.Add(feature, resultFeature.NewGeometry);

				string message = StringUtils.Concatenate(resultFeature.Messages, ". ");

				if (! string.IsNullOrEmpty(message))
				{
					if (resultFeature.HasWarningMessage)
					{
						_msg.Warn(message);
					}
					else
					{
						_msg.Info(message);
					}
				}
			}

			if (result.Count == 0)
			{
				_msg.Warn(reshapeResult.FailureMessage);
			}
			else
			{
				// Currently the non-reshaped failures (for several features) are in the global message:
				if (applicableSelectionCount > 1 &&
				    ! string.IsNullOrEmpty(reshapeResult.FailureMessage))
				{
					_msg.Warn(reshapeResult.FailureMessage);
				}
			}
		}

		private static async Task<bool> SaveAsync(IDictionary<Feature, Geometry> resultFeatures)
		{
			return await GdbPersistenceUtils.SaveInOperationAsync(
				       "Advanced reshape", resultFeatures);
		}

		private async Task<bool> UpdateOpenJawReplacedEndpointAsync(
			bool useNonDefaultReshapeSide,
			[NotNull] Polyline sketchLine,
			[NotNull] IList<Feature> polylineSelection)
		{
			// TODO: check options (allow/disallow)

			MapPoint endPoint = null;

			if (polylineSelection.Count == 1)
			{
				endPoint = await MicroserviceClient.GetOpenJawReplacementPointAsync(
					           polylineSelection[0], sketchLine, useNonDefaultReshapeSide);
			}

			_feedback?.UpdateOpenJawReplacedEndPoint(endPoint);

			return true;
		}

		private async Task<bool> UpdatePolygonResultPreviewAsync(
			bool nonDefaultSide,
			[NotNull] Polyline sketchPolyline,
			[NotNull] List<Feature> polygonSelection)
		{
			ReshapeResult reshapeResult = null;

			if (polygonSelection.Count != 0)
			{
				// Idea: ReshapeOperation class that contains the options to build the rpc, cancellation token, time out settings and logging.

				// TODO: Keep this source and cancel in case finish sketch happens
				var cancellationTokenSource = new CancellationTokenSource(3000);

				reshapeResult = MicroserviceClient.TryReshape(
					polygonSelection, sketchPolyline, null, false, true,
					nonDefaultSide, cancellationTokenSource.Token);
			}

			// TODO: discard result if the user has since clicked again or some other event (finish / esc) happened

			// TODO in 2.6 and higher try:
			//FrameworkApplication.QueueIdleAction(
			//	() => _feedback?.UpdatePreview(reshapeResult.ResultFeatures));

			return await QueuedTaskUtils.Run(
				       () => _feedback?.UpdatePreview(reshapeResult?.ResultFeatures));
		}

		public void SetSketchSymbol(CIMSymbolReference symbolReference)
		{
			SketchSymbol = symbolReference;
		}

		protected override Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.AdvancedReshapeOverlay, null);
		}

		protected override Cursor GetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.AdvancedReshapeOverlay,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.AdvancedReshapeOverlay,
			                              Resources.Lasso);
		}

		protected override Cursor GetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.AdvancedReshapeOverlay,
			                              Resources.Lasso,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.AdvancedReshapeOverlay,
			                              Resources.Polygon);
		}

		protected override Cursor GetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.AdvancedReshapeOverlay,
			                              Resources.Polygon,
			                              Resources.Shift);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.AdvancedReshape;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class AdvancedReshapeToolBase : ConstructionToolBase
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

		protected ReshapeToolOptions _advancedReshapeToolOptions;

		[CanBeNull]
		private OverridableSettingsProvider<PartialAdvancedReshapeOptions> _settingsProvider;

		private Task<bool> _updateFeedbackTask;

		private bool _nonDefaultSideMode;
		private CancellationTokenSource _cancellationTokenSource;
		private const Key _keyToggleNonDefaultSide = Key.S;
		private const Key _keyToggleMoveEndJunction = Key.M;

		protected virtual string OptionsFileName => "AdvancedReshapeToolOptions.xml";

		[CanBeNull]
		protected virtual string OptionsDockPaneID => null;

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		/// <summary>
		/// By default, the local configuration directory shall be in
		/// %APPDATA%\Roaming\ORGANIZATION\PRODUCT>\ToolDefaults.
		/// </summary>
		protected virtual string LocalConfigDir
			=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				AppDataFolder.Roaming, "ToolDefaults");

		protected AdvancedReshapeToolBase()
		{
			// important for SketchRecorder in base class
			FireSketchEvents = true;

			RequiresSelection = true;

			HandledKeys.Add(_keyToggleNonDefaultSide);
			HandledKeys.Add(_keyToggleMoveEndJunction);
		}

		protected override SelectionCursors FirstPhaseCursors { get; } =
			SelectionCursors.CreateArrowCursors(Resources.AdvancedReshapeOverlay);

		protected abstract IAdvancedReshapeService MicroserviceClient { get; }

		protected override SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch()
		{
			return new SymbolizedSketchTypeBasedOnSelection(this, () => SketchGeometryType.Line);
		}

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

		protected override bool CanSelectFromLayerCore(BasicFeatureLayer layer,
		                                               NotificationCollection notifications)
		{
			return layer is FeatureLayer;
		}

		protected override async Task OnToolActivatingCoreAsync()
		{
			_advancedReshapeToolOptions = InitializeOptions();
			_feedback = new AdvancedReshapeFeedback(_advancedReshapeToolOptions);

			await base.OnToolActivatingCoreAsync();
		}

		private ReshapeToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms
			string _ = CentralConfigDir;
			string __ = LocalConfigDir;

			// Create a new instance only if it doesn't exist yet (New as of 0.1.0, since we don't need to care for a change through ArcMap)
			_settingsProvider ??= new OverridableSettingsProvider<PartialAdvancedReshapeOptions>(
				CentralConfigDir, LocalConfigDir, OptionsFileName);

			PartialAdvancedReshapeOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			var result = new ReshapeToolOptions(centralConfiguration,
			                                    localConfiguration);

			result.PropertyChanged -= _advancedReshapeToolOptions_PropertyChanged;
			result.PropertyChanged += _advancedReshapeToolOptions_PropertyChanged;

			_msg.DebugStopTiming(watch, "Advanced Reshape Options validated / initialized");

			string optionsMessage = result.GetLocalOverridesMessage();

			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return result;
		}

		private void _advancedReshapeToolOptions_PropertyChanged(object sender,
		                                                         PropertyChangedEventArgs eventArgs)
		{
			try
			{
				QueuedTaskUtils.Run(() => ProcessSelectionAsync());
			}
			catch (Exception e)
			{
				_msg.Error($"Error re-calculating preview: {e.Message}", e);
			}
		}

		protected override async Task OnSelectionPhaseStartedAsync()
		{
			await QueuedTask.Run(async () =>
			{
				await base.OnSelectionPhaseStartedAsync();
				_feedback?.Clear();
				await ActiveMapView.ClearSketchAsync();
			});
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_advancedReshapeToolOptions.LocalOptions);

			_feedback?.Clear();
			_feedback = null;

			base.OnToolDeactivateCore(hasMapViewChanged);

			HideOptionsPane();
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override async Task<bool> OnSketchModifiedAsyncCore()
		{
			_msg.VerboseDebug(() => "OnSketchModifiedAsyncCore");

			return await ViewUtils.TryAsync(TryUpdateFeedbackAsync(), _msg, true);
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
					_advancedReshapeToolOptions.MoveOpenJawEndJunction =
						! _advancedReshapeToolOptions.MoveOpenJawEndJunction;

					_updateFeedbackTask = UpdateFeedbackAsync(_nonDefaultSideMode);

					bool currentState = _advancedReshapeToolOptions.MoveOpenJawEndJunction;
					if (currentState)
					{
						_msg.Info("Enabled move end junction option for Y-Reshape");
					}
					else
					{
						_msg.Info("Disabled move end junction option for Y-Reshape");
					}

					await _updateFeedbackTask;
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

		#region Tool Options DockPane

		[CanBeNull]
		private DockPaneAdvancedReshapeViewModelBase GetAdvancedReshapeViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneAdvancedReshapeViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			var viewModel = GetAdvancedReshapeViewModel();

			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _advancedReshapeToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			var viewModel = GetAdvancedReshapeViewModel();
			viewModel?.Hide();
		}

		#endregion

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

		public override async Task<bool> CanSetConstructionSketchSymbol(GeometryType geometryType)
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

			return result && ! await IsInSelectionPhaseAsync();
		}

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry, EditingTemplate editTemplate, MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			_feedback?.Clear();

			var polyline = (Polyline) sketchGeometry;

			SetToolCursor(Cursors.Wait);

			bool success = false;
			try
			{
				success = await QueuedTaskUtils.Run(
					          async () =>
						          await Reshape(polyline, activeView, cancelableProgressor));
			}
			finally
			{
				_nonDefaultSideMode = false;

				if (success && ! _advancedReshapeToolOptions.RemainInSketchMode)
				{
					await StartSelectionPhaseAsync();
				}
				else
				{
					await ActiveMapView.ClearSketchAsync();
					await StartSketchPhaseAsync();
				}
			}

			return success;
		}

		protected override void OnSketchResetCore()
		{
			_feedback?.Clear();

			_nonDefaultSideMode = false;
		}

		private async Task<bool> Reshape(Polyline sketchLine, MapView activeView,
		                                 CancelableProgressor cancelableProgressor)
		{
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

			bool allowOpenJawReshape = _advancedReshapeToolOptions.AllowOpenJawReshape;

			ReshapeResult result = MicroserviceClient.Reshape(
				selection, sketchLine, potentiallyAffectedFeatures, allowOpenJawReshape, true,
				_nonDefaultSideMode, _cancellationTokenSource.Token,
				_advancedReshapeToolOptions.MoveOpenJawEndJunction);

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

			bool success = await SaveAsync(resultFeatures);

			LogReshapeResults(result, selection.Count);

			// At some point, hopefully, read-only operations on the CIM model can run in parallel
			await ToolUtils.FlashResultPolygonsAsync(activeView, resultFeatures);

			return success;
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
	}
}

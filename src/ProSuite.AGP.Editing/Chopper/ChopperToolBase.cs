using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Cracker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Chopper
{
	public abstract class ChopperToolBase : TopologicalCrackingToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private ChopperToolOptions _chopperToolOptions;

		private OverridableSettingsProvider<PartialChopperOptions> _settingsProvider;

		private CrackerResult _resultChopPoints;

		private CrackerFeedback _feedback;

		private Envelope _calculationExtent;

		protected ChopperToolBase()
		{
			GeomIsSimpleAsFeature = false;
		}

		protected string OptionsFileName => "ChopperToolOptions.xml";

		[CanBeNull]
		protected virtual string OptionsDockPaneID => null;

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		/// <summary>
		/// By default, the local configuration directory shall be in
		/// %APPDATA%\Roaming\<organization>\<product>\ToolDefaults.
		/// </summary>
		protected virtual string LocalConfigDir
			=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				AppDataFolder.Roaming, "ToolDefaults");

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;
			if (MicroserviceClient == null)
			{
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
			}
		}

		protected override SelectionCursors FirstPhaseCursors { get; } =
			SelectionCursors.CreateArrowCursors(Resources.ChopperOverlay);

		protected override SelectionCursors SecondPhaseCursors { get; } =
			SelectionCursors.CreateCrossCursors(Resources.ChopperOverlay);

		protected override Task OnToolActivatingCoreAsync()
		{
			_chopperToolOptions = InitializeOptions();
			_feedback = new CrackerFeedback();
			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_chopperToolOptions.LocalOptions);
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.ChopperTool_LogPromptForSelection);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			// Store current map extent
			bool isStereoMap = MapUtils.IsStereoMapView(ActiveMapView);

			_calculationExtent = ActiveMapView.Extent;

			IList<Feature> intersectingFeatures =
				GetIntersectingFeatures(selectedFeatures, _chopperToolOptions, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of chop points was cancelled.");
				return;
			}

			_resultChopPoints =
				CalculateCrackPoints(selectedFeatures, intersectingFeatures, _chopperToolOptions,
				                     IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
				                     true, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of chop points was cancelled.");
				return;
			}

			_feedback.Update(_resultChopPoints, selectedFeatures);

			if (! isStereoMap)
			{
				_feedback.UpdateExtent(_calculationExtent);
			}
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _resultChopPoints != null && _resultChopPoints.ResultsByFeature.Count > 0;
		}

		protected override async Task<bool> SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection,
			Geometry sketch,
			CancelableProgressor progressor)
		{
			Assert.NotNull(_resultChopPoints);

			CrackerResult chopPointsToApply = SelectCrackPointsToApply(_resultChopPoints, sketch);

			if (! chopPointsToApply.HasCrackPoints)
			{
				return false;
			}

			MapView activeMapView = MapView.Active;

			var distinctSelectionByFeatureClass =
				MapUtils.GetDistinctSelectionByTable(selection)
				        .ToDictionary(kvp => (FeatureClass) kvp.Key,
				                      kvp => kvp.Value);

			var selectedFeatures = MapUtils.GetFeatures(
				distinctSelectionByFeatureClass, true, activeMapView.Map.SpatialReference).ToList();

			IList<Feature> intersectingFeatures =
				GetIntersectingFeatures(selectedFeatures, _chopperToolOptions, progressor);

			var result =
				MicroserviceClient.ChopLines(
					selectedFeatures, chopPointsToApply, intersectingFeatures,
					_chopperToolOptions,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
					true, progressor?.CancellationToken ?? new CancellationTokenSource().Token);

			var updates = new Dictionary<Feature, Geometry>();

			var inserts = new Dictionary<Feature, IList<Geometry>>();

			HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

			foreach (ResultFeature resultFeature in result)
			{
				Feature originalFeature = resultFeature.OriginalFeature;

				Geometry newGeometry = resultFeature.NewGeometry;

				if (! IsStoreRequired(originalFeature, newGeometry, editableClassHandles))
				{
					continue;
				}

				if (resultFeature.ChangeType == RowChangeType.Update)
				{
					updates.Add(originalFeature, newGeometry);
				}
				else
				{
					IList<Geometry> newGeometries;

					if (! inserts.TryGetValue(originalFeature, out newGeometries))
					{
						newGeometries = new List<Geometry>();

						inserts.Add(originalFeature, newGeometries);
					}

					newGeometries.Add(newGeometry);
				}
			}

			IEnumerable<Dataset> datasets =
				GdbPersistenceUtils.GetDatasetsNonEmpty(updates.Keys, inserts.Keys);

			var newFeatures = new List<Feature>();

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             _msg.DebugFormat("Saving {0} updates and {1} inserts...",
					                              updates.Count,
					                              inserts.Count);

					             GdbPersistenceUtils.UpdateTx(editContext, updates);

					             newFeatures.AddRange(
						             GdbPersistenceUtils.InsertTx(editContext, inserts));

					             return true;
				             },
				             "Chop Lines", datasets);

			ToolUtils.SelectNewFeatures(newFeatures, activeMapView, false);

			var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();

			CalculateDerivedGeometries(currentSelection, progressor);

			return saved;
		}

		protected override void ResetDerivedGeometries()
		{
			_resultChopPoints = null;
			_calculationExtent = null;
			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			if (_resultChopPoints == null || ! _resultChopPoints.HasCrackPoints)
			{
				_msg.Info(
					"No intersections with other geometries found. Please select several features to calculate chop points.");
			}

			if (_resultChopPoints != null && _resultChopPoints.HasCrackPoints)
			{
				string msg = _resultChopPoints.ResultsByFeature.Count == 1
					             ? "Select the chop points to apply."
					             : $"Chop points have been found in {_resultChopPoints.ResultsByFeature.Count} features. Select one or more chop points. Draw a box to select targets completely within the box.";

				_msg.InfoFormat(LocalizableStrings.RemoveOverlapsTool_AfterSelection, msg);
			}
		}

		private static bool IsStoreRequired(Feature originalFeature, Geometry updatedGeometry,
		                                    HashSet<long> editableClassHandles)
		{
			if (! GdbPersistenceUtils.CanChange(originalFeature,
			                                    editableClassHandles, out string warning))
			{
				_msg.DebugFormat("{0}: {1}",
				                 GdbObjectUtils.ToString(originalFeature),
				                 warning);

				return false;
			}

			Geometry originalGeometry = originalFeature.GetShape();

			if (originalGeometry != null &&
			    originalGeometry.IsEqual(updatedGeometry))
			{
				_msg.DebugFormat("The geometry of feature {0} is unchanged. It will not be stored",
				                 GdbObjectUtils.ToString(originalFeature));

				return false;
			}

			return true;
		}

		private ChopperToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms

			string currentCentralConfigDir = CentralConfigDir;

			string currentLocalConfigDir = LocalConfigDir;

			// Create a new instance only if it doesn't exist yet (New as of 0.1.0, since we don't need to care for a change through ArcMap)

			_settingsProvider ??= new OverridableSettingsProvider<PartialChopperOptions>(
				CentralConfigDir, LocalConfigDir, OptionsFileName);

			PartialChopperOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			var result = new ChopperToolOptions(centralConfiguration,
			                                    localConfiguration);

			result.PropertyChanged -= _chopperToolOptions_PropertyChanged;

			result.PropertyChanged += _chopperToolOptions_PropertyChanged;

			_msg.DebugStopTiming(watch, "Chopper Tool Options validated / initialized");

			string optionsMessage = result.GetLocalOverridesMessage();

			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return result;
		}

		private async void _chopperToolOptions_PropertyChanged(object sender,
		                                                       PropertyChangedEventArgs eventArgs)
		{
			try
			{
				await QueuedTaskUtils.Run(() => ProcessSelectionAsync());
			}
			catch (Exception e)
			{
				_msg.Error($"Error re-calculating chop points: {e.Message}", e);
			}
		}

		#region Tool Options DockPane

		[CanBeNull]
		private DockPaneChopperViewModelBase GetChopperViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneChopperViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			var viewModel = GetChopperViewModel();

			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _chopperToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			var viewModel = GetChopperViewModel();

			viewModel?.Hide();
		}

		#endregion
	}
}

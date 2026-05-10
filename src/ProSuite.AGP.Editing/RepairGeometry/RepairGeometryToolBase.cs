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
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RepairGeometry;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.RepairGeometry;

public abstract class RepairGeometryToolBase : TwoPhaseEditToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private RepairGeometryToolOptions _repairGeometryToolOptions;
	private OverridableSettingsProvider<PartialRepairGeometryOptions> _settingsProvider;

	private RepairGeometryResult _repairGeometryResult;
	private RepairGeometryFeedback _feedback;

	protected abstract IRepairGeometryService MicroserviceClient { get; }

	protected RepairGeometryToolBase()
	{
		GeomIsSimpleAsFeature = false;
	}

	protected string OptionsFileName => "RepairGeometryToolOptions.xml";

	[CanBeNull]
	protected virtual string OptionsDockPaneID => null;

	[CanBeNull]
	protected virtual string CentralConfigDir => null;

	protected virtual string LocalConfigDir
		=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
			AppDataFolder.Roaming, "ToolDefaults");

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.RepairGeometryOverlay);

	protected override SelectionCursors SecondPhaseCursors { get; } =
		SelectionCursors.CreateCrossCursors(Resources.RepairGeometryOverlay);

	protected override void OnUpdateCore()
	{
		Enabled = MicroserviceClient != null;

		if (MicroserviceClient == null)
			DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
	}

	protected override Task OnToolActivatingCoreAsync()
	{
		_repairGeometryToolOptions = InitializeOptions();

		_feedback = new RepairGeometryFeedback();

		return base.OnToolActivatingCoreAsync();
	}

	protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
	{
		_settingsProvider?.StoreLocalConfiguration(_repairGeometryToolOptions.LocalOptions);

		_feedback?.DisposeOverlays();
		_feedback = null;

		HideOptionsPane();

		return base.OnToolDeactivateCore(hasMapViewChanged);
	}

	protected override void LogPromptForSelection()
	{
		_msg.Info(
			$"Select one or more line or polygon features to check for geometry issues. " +
			$"{Environment.NewLine}- Press and hold SHIFT to add or remove features from the existing selection." +
			$"{Environment.NewLine}- Press and hold P to draw a polygon that completely contains the features to be selected. " +
			$"Finish the polygon with double-click.");
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		return geometryType == GeometryType.Polyline || geometryType == GeometryType.Polygon;
	}

	protected override void CalculateDerivedGeometries(
		IList<Feature> selectedFeatures, CancelableProgressor progressor)
	{
		// Re-read features from feature class (instead of using the selected feature's map SR)
		// Optionally, allow a non-standard tolerance
		selectedFeatures = ReReadFeaturesFromFeatureClass(selectedFeatures);

		_repairGeometryResult = CalculateRepairInfo(
			selectedFeatures, _repairGeometryToolOptions, progressor);

		if (progressor?.CancellationToken.IsCancellationRequested == true)
		{
			_msg.Warn("Calculation of repair info was cancelled.");
			return;
		}

		_feedback.Update(_repairGeometryResult, selectedFeatures);
	}

	protected override bool CanUseDerivedGeometries()
	{
		return _repairGeometryResult != null && _repairGeometryResult.HasRepairableFeatures;
	}

	protected override async Task<bool> SelectAndProcessDerivedGeometry(
		Dictionary<MapMember, List<long>> selection,
		Geometry sketch,
		CancelableProgressor progressor)
	{
		Assert.NotNull(_repairGeometryResult);

		IList<RepairableFeature> featuresToRepair =
			SelectFeaturesForRepair(_repairGeometryResult, sketch);

		if (featuresToRepair.Count == 0)
		{
			return false;
		}

		MapView activeMapView = MapView.Active;

		var distinctSelectionByFeatureClass =
			MapUtils.GetDistinctSelectionByTable(selection)
			        .ToDictionary(kvp => (FeatureClass) kvp.Key, kvp => kvp.Value);

		IList<Feature> selectedFeatures = MapUtils.GetFeatures(
			distinctSelectionByFeatureClass, true, activeMapView.Map.SpatialReference).ToList();

		CancellationToken cancellationToken =
			progressor?.CancellationToken ?? new CancellationTokenSource().Token;

		RepairGeometryToolOptions applyOptions = _repairGeometryToolOptions;

		double minimumSegmentLength =
			applyOptions.EnforceMinimumSegmentLength ? applyOptions.MinimumSegmentLength : -1;

		IList<ResultFeature> result =
			MicroserviceClient.ApplyRepairGeometry(
				selectedFeatures, featuresToRepair,
				minimumSegmentLength,
				applyOptions.AllowLoops,
				applyOptions.AllowLinearSelfIntersections,
				applyOptions.CrackPointTolerance,
				applyOptions.Use2D,
				cancellationToken);

		if (result == null)
		{
			return false;
		}

		var updates = new Dictionary<Feature, Geometry>();

		HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

		foreach (ResultFeature resultFeature in result)
		{
			Feature originalFeature = resultFeature.OriginalFeature;
			Geometry newGeometry = resultFeature.NewGeometry;

			if (! ToolUtils.IsStoreRequired(originalFeature, newGeometry, editableClassHandles))
			{
				continue;
			}

			Assert.AreEqual(RowChangeType.Update, resultFeature.ChangeType,
			                $"Unexpected type of change: {resultFeature.ChangeType}");

			updates.Add(originalFeature, newGeometry);
		}

		IEnumerable<Dataset> datasets = GdbPersistenceUtils.GetDatasetsNonEmpty(updates.Keys);

		bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
			             editContext =>
			             {
				             _msg.DebugFormat("Saving {0} geometry repair updates...",
				                              updates.Count);
				             GdbPersistenceUtils.UpdateTx(editContext, updates);
				             return true;
			             },
			             "Repair geometry", datasets);

		var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();
		CalculateDerivedGeometries(currentSelection, progressor);

		return saved;
	}

	protected override void ResetDerivedGeometries()
	{
		_repairGeometryResult = null;
		_feedback?.DisposeOverlays();
	}

	protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
	{
		if (_repairGeometryResult == null || ! _repairGeometryResult.HasRepairableFeatures)
		{
			_msg.Info("No geometry issues found. Select other features to check.");
			return;
		}

		int featureCount = _repairGeometryResult.ResultsByFeature.Count;
		_msg.Info(
			$"Found geometry issues in {featureCount} feature(s). " +
			$"Click on or draw a box over a feature to repair it. " +
			$"Press [ESC] to select different features.");
	}

	[CanBeNull]
	private RepairGeometryResult CalculateRepairInfo(
		[NotNull] IList<Feature> selectedFeatures,
		[NotNull] RepairGeometryToolOptions options,
		[CanBeNull] CancelableProgressor progressor)
	{
		if (MicroserviceClient == null)
		{
			throw new InvalidConfigurationException("Microservice has not been started.");
		}

		CancellationToken cancellationToken = progressor?.CancellationToken
		                                      ?? new CancellationTokenSource().Token;

		_msg.DebugFormat("Calculating repair info with the following options: {0}", options);

		double minimumSegmentLength =
			options.EnforceMinimumSegmentLength ? options.MinimumSegmentLength : -1;

		return MicroserviceClient.CalculateRepairInfo(
			selectedFeatures,
			minimumSegmentLength,
			options.AllowLoops,
			options.AllowLinearSelfIntersections,
			options.AddCrackPointsBetweenParts,
			options.CrackPointTolerance,
			options.Use2D,
			cancellationToken);
	}

	private IList<RepairableFeature> SelectFeaturesForRepair(
		[NotNull] RepairGeometryResult repairResult,
		[NotNull] Geometry sketch)
	{
		var result = new List<RepairableFeature>();

		sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(),
		                                          out bool singlePick);

		foreach (RepairableFeature repairableFeature in repairResult.ResultsByFeature)
		{
			Feature feature = repairableFeature.Feature;
			Geometry featureGeometry = feature.GetShape();

			bool sketchIntersectsFeature =
				ToolUtils.IsSelected(sketch, featureGeometry, singlePick);

			if (! sketchIntersectsFeature &&
			    repairableFeature.PointsToDelete != null &&
			    repairableFeature.PointsToDelete.PointCount > 0)
			{
				sketchIntersectsFeature =
					repairableFeature.PointsToDelete.Points.Any(p => ToolUtils.IsSelected(
						                                            sketch, p, singlePick));
			}

			if (! sketchIntersectsFeature &&
			    repairableFeature.CrackPointsToAdd != null &&
			    repairableFeature.CrackPointsToAdd.PointCount > 0)
			{
				sketchIntersectsFeature =
					repairableFeature.CrackPointsToAdd.Points.Any(p => ToolUtils.IsSelected(
							sketch, p, singlePick));
			}

			if (! sketchIntersectsFeature)
			{
				foreach (InvalidSegment invalidSegment in repairableFeature.InvalidSegments)
				{
					Segment segment = invalidSegment.Segment;
					Polyline segmentPolyline =
						PolylineBuilderEx.CreatePolyline(segment, segment.SpatialReference);

					if (ToolUtils.IsSelected(sketch, segmentPolyline, singlePick))
					{
						sketchIntersectsFeature = true;
						break;
					}
				}
			}

			if (sketchIntersectsFeature)
			{
				result.Add(repairableFeature);
			}
		}

		return result;
	}

	[NotNull]
	private static List<Feature> ReReadFeaturesFromFeatureClass(
		[NotNull] IList<Feature> features)
	{
		var result = new List<Feature>();

		foreach (IGrouping<long, Feature> grouping in features.GroupBy(f => f.GetTable().GetID()))
		{
			FeatureClass featureClass = grouping.FirstOrDefault()?.GetTable();

			if (featureClass == null)
			{
				continue;
			}

			SpatialReference featureClassSpatialReference = featureClass.GetSpatialReference();

			IEnumerable<long> objectIds = grouping.Select(f => f.GetObjectID());

			result.AddRange(
				GdbQueryUtils.GetFeatures(featureClass, objectIds, featureClassSpatialReference,
				                          false));
		}

		return result;
	}

	private RepairGeometryToolOptions InitializeOptions()
	{
		Stopwatch watch = _msg.DebugStartTiming();

		string currentCentralConfigDir = CentralConfigDir;
		string currentLocalConfigDir = LocalConfigDir;

		_settingsProvider =
			new OverridableSettingsProvider<PartialRepairGeometryOptions>(
				currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

		PartialRepairGeometryOptions localConfiguration, centralConfiguration;
		_settingsProvider.GetConfigurations(out localConfiguration, out centralConfiguration);

		var result = new RepairGeometryToolOptions(centralConfiguration, localConfiguration);

		result.PropertyChanged -= OptionsPropertyChanged;
		result.PropertyChanged += OptionsPropertyChanged;

		_msg.DebugStopTiming(watch, "Repair Geometry Tool Options validated / initialized");

		string optionsMessage = result.GetLocalOverridesMessage();
		if (! string.IsNullOrEmpty(optionsMessage))
		{
			_msg.Info(optionsMessage);
		}

		return result;
	}

	private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs args)
	{
		try
		{
			QueuedTaskUtils.Run(() => ProcessSelectionAsync());
		}
		catch (Exception e)
		{
			_msg.Error($"Error re-calculating repair info: {e.Message}", e);
		}
	}

	#region Tool Options Dockpane

	[CanBeNull]
	private DockPaneRepairGeometryViewModelBase GetOptionsViewModel()
	{
		if (OptionsDockPaneID == null)
		{
			return null;
		}

		var viewModel =
			FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
				DockPaneRepairGeometryViewModelBase;

		return Assert.NotNull(viewModel,
		                      "Options DockPane with ID '{0}' not found", OptionsDockPaneID);
	}

	protected override void ShowOptionsPane()
	{
		DockPaneRepairGeometryViewModelBase viewModel = GetOptionsViewModel();
		Assert.NotNull(viewModel);
		viewModel.Options = _repairGeometryToolOptions;
		viewModel.Activate(true);
	}

	protected override void HideOptionsPane()
	{
		DockPaneRepairGeometryViewModelBase viewModel = GetOptionsViewModel();
		viewModel?.Hide();
	}

	#endregion
}

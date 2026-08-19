using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Cracker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Chopper;

public abstract class ChopperToolBase : TopologicalCrackingToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private ChopperToolOptions _chopperToolOptions;

	private OverridableSettingsProvider<PartialChopperOptions> _settingsProvider;

	private CrackerResult _resultChopPoints;

	private CrackerFeedback _feedback;

	private Envelope _calculationExtent;

	private const Key _vertexDisplayModeToggleKey = Key.V;

	// Additional display mode, toggled with [V]
	private VertexCoordinateDisplay _vertexDisplay;

	protected ChopperToolBase()
	{
		GeomIsSimpleAsFeature = false;

		HandledKeys.Add(_vertexDisplayModeToggleKey);
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

	/// <summary>
	/// The scale denominator beyond which (i.e. zoomed further out than) the vertex labels
	/// are hidden. Zooming in again restores them.
	/// </summary>
	protected virtual double LabelMinimumScaleDenominator => 1000;

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

		// The display mode starts off and is toggled with [V]
		_vertexDisplay = new VertexCoordinateDisplay
		                 {
			                 MinimumScaleDenominator = LabelMinimumScaleDenominator
		                 };

		return base.OnToolActivatingCoreAsync();
	}

	protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
	{
		_settingsProvider?.StoreLocalConfiguration(_chopperToolOptions.LocalOptions);
		_feedback?.DisposeOverlays();
		_feedback = null;

		_vertexDisplay?.Dispose();
		_vertexDisplay = null;

		return base.OnToolDeactivateCore(hasMapViewChanged);
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
		// Begin a new calculation generation up front so a superseding request (e.g. another
		// spinner click) or Escape cancels this whole calculation, including the target search
		// below, before the service round-trip is issued.
		CancellationToken cancelToken = BeginCalculation();

		// Store current map extent
		bool isStereoMap = MapUtils.IsStereoMapView(ActiveMapView);

		_calculationExtent = ActiveMapView.Extent;

		IList<Feature> intersectingFeatures =
			GetIntersectingFeatures(selectedFeatures, _chopperToolOptions, progressor);

		if (cancelToken.IsCancellationRequested)
		{
			_msg.Warn("Calculation of chop points was cancelled.");
			return;
		}

		_resultChopPoints =
			CalculateCrackPoints(selectedFeatures, intersectingFeatures, _chopperToolOptions,
			                     IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
			                     true, progressor);

		if (cancelToken.IsCancellationRequested)
		{
			_msg.Warn("Calculation of chop points was cancelled.");
			return;
		}

		_feedback.Update(_resultChopPoints, selectedFeatures);

		if (! isStereoMap)
		{
			_feedback.UpdateExtent(_calculationExtent);
		}

		UpdateActiveDisplayFeatures(selectedFeatures);
	}

	protected override bool CanUseDerivedGeometries()
	{
		return _resultChopPoints != null && _resultChopPoints.ResultsByFeature.Count > 0;
	}

	protected override async Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
	{
		if (args.Key == _vertexDisplayModeToggleKey)
		{
			await ToggleVertexDisplayAsync();
		}

		await base.HandleKeyDownCoreAsync(args);
	}

	// Hands the current selection to the display before switching it on. Unlike the chop
	// point feedback the labels are available in both phases of the tool, i.e. without
	// waiting for the chop points to be calculated. Switching off needs no shapes.
	private async Task ToggleVertexDisplayAsync()
	{
		if (_vertexDisplay == null)
		{
			return;
		}

		if (! _vertexDisplay.IsEnabled)
		{
			await QueuedTaskUtils.Run(() => _vertexDisplay.SetFeatures(
				                          GetApplicableSelectedFeatures(ActiveMapView).ToList()));
		}

		await _vertexDisplay.ToggleAsync();
	}

	// Keeps the display mode in sync with the selection while it is on. If it is off it
	// picks up the selection when it is switched on.
	private void UpdateActiveDisplayFeatures([CanBeNull] IList<Feature> selectedFeatures)
	{
		if (_vertexDisplay?.IsEnabled == true)
		{
			_vertexDisplay.SetFeatures(selectedFeatures);
		}
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

		// New calculation generation for the chop operation (see CalculateDerivedGeometries).
		CancellationToken cancelToken = BeginCalculation();

		IList<Feature> intersectingFeatures =
			GetIntersectingFeatures(selectedFeatures, _chopperToolOptions, progressor);

		var result =
			MicroserviceClient.ChopLines(
				selectedFeatures, chopPointsToApply, intersectingFeatures,
				_chopperToolOptions,
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints,
				true, cancelToken);

		var updates = new Dictionary<Feature, Geometry>();

		var inserts = new Dictionary<Feature, IList<Geometry>>();

		HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

		foreach (ResultFeature resultFeature in result)
		{
			if (cancelToken.IsCancellationRequested)
			{
				_msg.Warn("Chop operation was cancelled.");
				return false;
			}

			Feature originalFeature = resultFeature.OriginalFeature;

			Geometry newGeometry = resultFeature.NewGeometry;

			if (! ToolUtils.IsStoreRequired(originalFeature, newGeometry, editableClassHandles))
			{
				continue;
			}

			// TODO: Find a better place, group by table
			newGeometry =
				MakeGeometryStorable(newGeometry, originalFeature.GetTable().GetDefinition());

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

	private static Geometry MakeGeometryStorable(Geometry newGeometry,
	                                             FeatureClassDefinition featureClassDef)
	{
		// Avoid 'Geometry has null Z values':

		bool classHasZ = featureClassDef.HasZ();
		bool classHasM = featureClassDef.HasM();

		Geometry geometryToStore =
			GeometryUtils.EnsureGeometrySchema(
				newGeometry, classHasZ, classHasM);

		Geometry projected = GeometryUtils.EnsureSpatialReference(
			geometryToStore, featureClassDef.GetSpatialReference());

		return projected;
	}

	protected override void ResetDerivedGeometries()
	{
		_resultChopPoints = null;
		_calculationExtent = null;
		_feedback.DisposeOverlays();

		// The selection is gone (e.g. [ESC]): drop the labels with it. The display mode
		// stays on and picks up the next selection.
		_vertexDisplay?.SetShapes(null);
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

			_msg.InfoFormat(LocalizableStrings.CrackerTool_AfterSelection, msg);
		}
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

	private void _chopperToolOptions_PropertyChanged(object sender,
	                                                 PropertyChangedEventArgs eventArgs)
	{
		// Coalesce rapid option changes (e.g. spinner clicks) and cancel any running calculation
		// so they don't pile up as independent, uncancellable service calls.
		RequestRecalculation();
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

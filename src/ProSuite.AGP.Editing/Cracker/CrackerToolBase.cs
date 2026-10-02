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
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Cracker;

public abstract class CrackerToolBase : TopologicalCrackingToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private CrackerToolOptions _crackerToolOptions;

	private OverridableSettingsProvider<PartialCrackerOptions> _settingsProvider;

	private CrackerResult _resultCrackPoints;

	private CrackerFeedback _feedback;

	private Envelope _calculationExtent;

	private const Key _vertexDisplayModeToggleKey = Key.V;

	// Additional display mode, toggled with [V]
	private VertexCoordinateDisplay _vertexDisplay;

	protected CrackerToolBase()
	{
		GeomIsSimpleAsFeature = false;

		HandledKeys.Add(_vertexDisplayModeToggleKey);
	}

	protected string OptionsFileName => "CrackerToolOptions.xml";

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

			DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
	}

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.CrackerOverlay);

	protected override SelectionCursors SecondPhaseCursors { get; } =
		SelectionCursors.CreateCrossCursors(Resources.CrackerOverlay);

	protected override Task OnToolActivatingCoreAsync()
	{
		_crackerToolOptions = InitializeOptions();

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
		_settingsProvider?.StoreLocalConfiguration(_crackerToolOptions.LocalOptions);

		_feedback?.DisposeOverlays();

		_feedback = null;

		_vertexDisplay?.Dispose();
		_vertexDisplay = null;

		HideOptionsPane();

		return base.OnToolDeactivateCore(hasMapViewChanged);
	}

	protected override void LogPromptForSelection()
	{
		_msg.Info(LocalizableStrings.CrackerTool_LogPromptForSelection);
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		return geometryType == GeometryType.Polyline ||
		       geometryType == GeometryType.Polygon;
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
			GetIntersectingFeatures(selectedFeatures, _crackerToolOptions, progressor);

		// Bail before the (expensive) service call if a newer request or Escape already
		// superseded this calculation during the target-feature search above.
		if (cancelToken.IsCancellationRequested)
		{
			_msg.Warn("Calculation of crack points was cancelled.");

			return;
		}

		_resultCrackPoints =
			CalculateCrackPoints(selectedFeatures, intersectingFeatures, _crackerToolOptions,
			                     IntersectionPointOptions.IncludeLinearIntersectionAllPoints,
			                     false, progressor);

		if (cancelToken.IsCancellationRequested)
		{
			_msg.Warn("Calculation of crack points was cancelled.");

			return;
		}

		_feedback.Update(_resultCrackPoints, selectedFeatures);

		if (! isStereoMap)
		{
			_feedback.UpdateExtent(_calculationExtent);
		}

		UpdateActiveDisplayFeatures(selectedFeatures);
	}

	protected override bool CanUseDerivedGeometries()
	{
		return _resultCrackPoints != null && _resultCrackPoints.ResultsByFeature.Count > 0;
	}

	protected override async Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
	{
		if (args.Key == _vertexDisplayModeToggleKey)
		{
			await ToggleVertexDisplayAsync();
		}

		await base.HandleKeyDownCoreAsync(args);
	}

	// Hands the current selection to the display before switching it on. Unlike the crack
	// point feedback the labels are available in both phases of the tool, i.e. without
	// waiting for the crack points to be calculated. Switching off needs no shapes.
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
		Assert.NotNull(_resultCrackPoints);

		CrackerResult crackPointsToApply = SelectCrackPointsToApply(_resultCrackPoints, sketch);

		if (! crackPointsToApply.HasCrackPoints)
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

		// New calculation generation for the apply operation (see CalculateDerivedGeometries).
		CancellationToken cancelToken = BeginCalculation();

		IList<Feature> intersectingFeatures =
			GetIntersectingFeatures(selectedFeatures, _crackerToolOptions, progressor);

		var result =
			MicroserviceClient.ApplyCrackPoints(
				selectedFeatures, crackPointsToApply, intersectingFeatures,
				_crackerToolOptions,
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints,
				false, cancelToken);

		var updates = new Dictionary<Feature, Geometry>();

		HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

		foreach (ResultFeature resultFeature in result)
		{
			Feature originalFeature = resultFeature.OriginalFeature;
			Geometry updatedGeometry = resultFeature.NewGeometry;
			if (! ToolUtils.IsStoreRequired(originalFeature, updatedGeometry,
			                                editableClassHandles))
			{
				continue;
			}

			updates.Add(originalFeature, updatedGeometry);
		}

		if (updates.Count == 0)
		{
			return false;
		}

		IEnumerable<Dataset> datasets =
			GdbPersistenceUtils.GetDatasetsNonEmpty(updates.Keys);

		bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
			             editContext =>
			             {
				             _msg.DebugFormat("Saving {0} updates...", updates.Count);
				             GdbPersistenceUtils.UpdateTx(editContext, updates);
				             return true;
			             },
			             "Crack feature(s)", datasets);

		var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();

		CalculateDerivedGeometries(currentSelection, progressor);

		return saved;
	}

	protected override void ResetDerivedGeometries()
	{
		_resultCrackPoints = null;

		_calculationExtent = null;

		_feedback.DisposeOverlays();

		// The selection is gone (e.g. [ESC]): drop the labels with it. The display mode
		// stays on and picks up the next selection.
		_vertexDisplay?.SetShapes(null);
	}

	protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
	{
		if (_resultCrackPoints == null || ! _resultCrackPoints.HasCrackPoints)
		{
			_msg.Info(
				"No intersections with other geometries found. Please select several features to calculate crack points.");
		}

		if (_resultCrackPoints != null && _resultCrackPoints.HasCrackPoints)
		{
			string msg = _resultCrackPoints.ResultsByFeature.Count == 1
				             ? "Select the crack points to apply."
				             : $"Crack points have been found in {_resultCrackPoints.ResultsByFeature.Count} features. Select one or more crack points. Draw a box to select targets completely within the box.";

			_msg.InfoFormat(LocalizableStrings.CrackerTool_AfterSelection, msg);
		}
	}

	private CrackerToolOptions InitializeOptions()
	{
		Stopwatch watch = _msg.DebugStartTiming();

		// NOTE: by only reading the file locations we can save a couple of 100ms

		string currentCentralConfigDir = CentralConfigDir;

		string currentLocalConfigDir = LocalConfigDir;

		// Create a new instance only if it doesn't exist yet (New as of 0.1.0, since we don't need to care for a change through ArcMap)

		_settingsProvider ??= new OverridableSettingsProvider<PartialCrackerOptions>(
			CentralConfigDir, LocalConfigDir, OptionsFileName);

		PartialCrackerOptions localConfiguration, centralConfiguration;

		_settingsProvider.GetConfigurations(out localConfiguration,
		                                    out centralConfiguration);

		var result = new CrackerToolOptions(centralConfiguration,
		                                    localConfiguration);

		result.PropertyChanged -= _crackerToolOptions_PropertyChanged;

		result.PropertyChanged += _crackerToolOptions_PropertyChanged;

		_msg.DebugStopTiming(watch, "Cracker Tool Options validated / initialized");

		string optionsMessage = result.GetLocalOverridesMessage();

		if (! string.IsNullOrEmpty(optionsMessage))
		{
			_msg.Info(optionsMessage);
		}

		return result;
	}

	private void _crackerToolOptions_PropertyChanged(object sender,
	                                                 PropertyChangedEventArgs eventArgs)
	{
		// Coalesce rapid option changes (e.g. spinner clicks) and cancel any running calculation
		// so they don't pile up as independent, uncancellable service calls.
		RequestRecalculation();
	}

	#region Tool Options DockPane

	[CanBeNull]
	private DockPaneCrackerViewModelBase GetCrackerViewModel()

	{
		if (OptionsDockPaneID == null)

		{
			return null;
		}

		var viewModel =
			FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
				DockPaneCrackerViewModelBase;

		return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
		                      OptionsDockPaneID);
	}

	protected override void ShowOptionsPane()

	{
		var viewModel = GetCrackerViewModel();

		if (viewModel == null)

		{
			return;
		}

		viewModel.Options = _crackerToolOptions;

		viewModel.Activate(true);
	}

	protected override void HideOptionsPane()

	{
		var viewModel = GetCrackerViewModel();

		viewModel?.Hide();
	}

	#endregion

	#region Search target features

	private static bool CanOverlapGeometryType([CanBeNull] FeatureLayer featureLayer)

	{
		if (featureLayer?.GetFeatureClass() == null)

		{
			return false;
		}

		esriGeometryType shapeType = featureLayer.ShapeType;

		return shapeType == esriGeometryType.esriGeometryPolygon ||
		       shapeType == esriGeometryType.esriGeometryPolyline ||
		       shapeType == esriGeometryType.esriGeometryMultiPatch;
	}

	private static bool IgnoreLayer(Layer layer, IEnumerable<string> ignoredClasses)

	{
		FeatureClass featureClass = (layer as FeatureLayer)?.GetTable() as FeatureClass;

		if (featureClass == null)

		{
			return true;
		}

		string className = featureClass.GetName();

		foreach (string ignoredClass in ignoredClasses)

		{
			if (className.EndsWith(ignoredClass, StringComparison.InvariantCultureIgnoreCase))

			{
				return true;
			}
		}

		return false;
	}

	#endregion
}

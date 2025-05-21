using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
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

namespace ProSuite.AGP.Editing.Cracker
{
	public abstract class CrackerToolBase : TopologicalCrackingToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private CrackerToolOptions _crackerToolOptions;

		private OverridableSettingsProvider<PartialCrackerToolOptions> _settingsProvider;

		private CrackerResult _resultCrackPoints;

		private CrackerFeedback _feedback;

		private Envelope _calculationExtent;

		protected CrackerToolBase()
		{
			GeomIsSimpleAsFeature = false;
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

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)

				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override SelectionCursors GetSelectionCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.CrackerOverlay);
		}

		protected override SelectionCursors GetSecondPhaseCursors()
		{
			return SelectionCursors.CreateCrossCursors(Resources.CrackerOverlay);
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_crackerToolOptions = InitializeOptions();

			_feedback = new CrackerFeedback();

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_crackerToolOptions.LocalOptions);

			_feedback?.DisposeOverlays();

			_feedback = null;

			HideOptionsPane();
		}

		protected override async Task<bool> OnMapSelectionChangedCoreAsync(
			MapSelectionChangedEventArgs args)
		{
			bool result = await base.OnMapSelectionChangedCoreAsync(args);

			//_vertexLabels.UpdateLabels();

			return result;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.CrackerTool_LogPromptForSelection);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon ||
			       geometryType == GeometryType.Multipatch;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			// Store current map extent

			_calculationExtent = ActiveMapView.Extent;

			IList<Feature> intersectingFeatures =
				GetIntersectingFeatures(selectedFeatures, _crackerToolOptions, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of crack points was cancelled.");

				return;
			}

			_resultCrackPoints =
				CalculateCrackPoints(selectedFeatures, intersectingFeatures, _crackerToolOptions,
				                     IntersectionPointOptions.IncludeLinearIntersectionAllPoints,
				                     false, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of crack points was cancelled.");

				return;
			}

			_feedback.Update(_resultCrackPoints, selectedFeatures);

			_feedback.UpdateExtent(_calculationExtent);
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _resultCrackPoints != null && _resultCrackPoints.ResultsByFeature.Count > 0;
		}

		// TODO: Show/hide Vertex labels, maybe impl on TopologicalCrackingToolBase / Shortcut T

		//protected override void ToggleVertices()

		//{

		//	base.ToggleVertices();

		//	try

		//	{

		//		//_vertexLabels.Toggle();

		//		//_vertexLabels.UpdateLabels();

		//	}

		//	catch (Exception ex)

		//	{

		//		_msg.Error($"Toggling Vertices Labels Error: {ex.Message}");

		//	}

		//}

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

			IList<Feature> intersectingFeatures =
				GetIntersectingFeatures(selectedFeatures, _crackerToolOptions, progressor);

			var result =
				MicroserviceClient.ApplyCrackPoints(
					selectedFeatures, crackPointsToApply, intersectingFeatures,
					_crackerToolOptions,
					IntersectionPointOptions.IncludeLinearIntersectionAllPoints,
					false, progressor?.CancellationToken ?? new CancellationTokenSource().Token);

			var updates = new Dictionary<Feature, Geometry>();

			HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

			foreach (ResultFeature resultFeature in result)
			{
				Feature originalFeature = resultFeature.OriginalFeature;
				Geometry updatedGeometry = resultFeature.NewGeometry;
				if (! IsStoreRequired(originalFeature, updatedGeometry, editableClassHandles))
				{
					continue;
				}

				updates.Add(originalFeature, updatedGeometry);
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

			// TODO:

			//_vertexLabels.UpdateLabels();

			return saved;
		}

		protected override void ResetDerivedGeometries()
		{
			_resultCrackPoints = null;

			_calculationExtent = null;

			_feedback.DisposeOverlays();
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

		private CrackerToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms

			string currentCentralConfigDir = CentralConfigDir;

			string currentLocalConfigDir = LocalConfigDir;

			// Create a new instance only if it doesn't exist yet (New as of 0.1.0, since we don't need to care for a change through ArcMap)

			_settingsProvider ??= new OverridableSettingsProvider<PartialCrackerToolOptions>(
				CentralConfigDir, LocalConfigDir, OptionsFileName);

			PartialCrackerToolOptions localConfiguration, centralConfiguration;

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
			try

			{
				QueuedTaskUtils.Run(() => ProcessSelectionAsync());
			}

			catch (Exception e)

			{
				_msg.Error($"Error re-calculating crack points: {e.Message}", e);
			}
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
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Erase;

public abstract class EraseToolBase : ConstructionToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected EraseToolOptions _eraseToolOptions;

	[CanBeNull] private OverridableSettingsProvider<PartialEraseOptions> _settingsProvider;

	protected EraseToolBase()
	{
		// important for SketchRecorder in base class
		FireSketchEvents = true;

		// This is our property:
		RequiresSelection = true;
	}

	protected bool SuppressPolylineErasing => ! (_eraseToolOptions?.AllowPolylineErasing ?? false);

	protected bool SuppressMultipointErasing =>
		! (_eraseToolOptions?.AllowMultipointErasing ?? false);

	protected virtual IRemoveOverlapsService MicroserviceClient { get; } = null;

	protected string OptionsFileName => "EraseToolOptions.xml";

	[CanBeNull]
	protected virtual string OptionsDockPaneID => null;

	[CanBeNull]
	protected virtual string CentralConfigDir => null;

	// ReSharper disable twice InvalidXmlDocComment
	/// <summary>
	/// By default, the local configuration directory shall be in
	/// %APPDATA%\Roaming\<organization>\<product>\ToolDefaults.
	/// </summary>
	protected virtual string LocalConfigDir
		=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
			AppDataFolder.Roaming, "ToolDefaults");

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.EraseOverlay);

	protected override SketchGeometryType GetEditSketchGeometryType()
	{
		return SketchGeometryType.Polygon;
	}

	protected override SketchGeometryType GetSelectionSketchGeometryType()
	{
		return SketchGeometryType.Rectangle;
	}

	protected override async Task<bool?> GetEditSketchHasZ()
	{
		Stopwatch watch = Stopwatch.StartNew();

		int selectionCount = 0;
		bool? result = await QueuedTask.Run(() =>
		{
			var selectionByLayer = SelectionUtils.GetSelection(ActiveMapView.Map);

			if (selectionByLayer.Count == 0)
			{
				_msg.Debug($"{Caption}: no feature layer found in selection");
				return null;
			}

			bool? hasAnyZ = false;

			foreach (var selectedOidByLayer in selectionByLayer)
			{
				if (selectedOidByLayer.Key is FeatureLayer layer)
				{
					FeatureClass featureClass = layer.GetFeatureClass();
					bool? layerHasZ = featureClass?.GetDefinition()?.HasZ();

					if (layerHasZ == true)
					{
						hasAnyZ = true;
						break;
					}
				}
			}

			return hasAnyZ;
		});

		_msg.DebugStopTiming(
			watch, "Determined sketch has Z: {0} (evaluated {1} selected layers)", result,
			selectionCount);

		return result;
	}

	protected override void LogEnteringSketchMode()
	{
		_msg.Info(LocalizableStrings.EraseTool_LogEnteringSketchMode);
	}

	protected override void LogPromptForSelection()
	{
		//string enterMsg = CanUseSelection()
		//		  ? "- To re-use the existing selection, press Enter"
		//		  : string.Empty;

		_msg.InfoFormat(LocalizableStrings.EraseTool_LogPromptForSelection);
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		if (geometryType == GeometryType.Polygon)
		{
			return true;
		}

		if (! SuppressPolylineErasing && geometryType == GeometryType.Polyline)
		{
			return true;
		}

		if (! SuppressMultipointErasing && geometryType == GeometryType.Multipoint)
		{
			return true;
		}

		return MicroserviceClient != null &&
		       geometryType == GeometryType.Multipatch;
	}

	protected override async Task<bool> OnEditSketchCompleteCoreAsync(
		Geometry sketchGeometry,
		EditingTemplate editTemplate,
		MapView activeView,
		CancelableProgressor cancelableProgressor = null)
	{
		var polygon = (Polygon) sketchGeometry;

		var resultFeatures = await QueuedTaskUtils.Run(
			                     () => CalculateResultFeatures(activeView, polygon),
			                     cancelableProgressor);

		if (resultFeatures.Count == 0)
		{
			_msg.Warn("No feature was changed");
			return false;
		}

		var taskSave = QueuedTaskUtils.Run(() => SaveAsync(resultFeatures));
		var taskFlash =
			QueuedTaskUtils.Run(async () => await ToolUtils.FlashResultPolygonsAsync(
				                                activeView, resultFeatures));

		await Task.WhenAll(taskFlash, taskSave);

		// Clear sketch is necessary if finishing sketch by F2. Otherwise, a defunct
		// sketch remains that cannot be cleared with ESC!
		await ClearSketchAsync();
		await StartSketchPhaseAsync();

		return taskSave.Result;
	}

	protected override Task OnToolActivatingCoreAsync()
	{
		InitializeOptions();

		return base.OnToolActivatingCoreAsync();
	}

	protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
	{
		_settingsProvider?.StoreLocalConfiguration(_eraseToolOptions?.LocalOptions);

		HideOptionsPane();

		return base.OnToolDeactivateCore(hasMapViewChanged);
	}

	protected void InitializeOptions()
	{
		Stopwatch watch = _msg.DebugStartTiming();

		string currentCentralConfigDir = CentralConfigDir;
		string currentLocalConfigDir = LocalConfigDir;

		_settingsProvider =
			new OverridableSettingsProvider<PartialEraseOptions>(
				currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

		PartialEraseOptions localConfiguration, centralConfiguration;

		_settingsProvider.GetConfigurations(out localConfiguration,
		                                    out centralConfiguration);

		_eraseToolOptions =
			new EraseToolOptions(centralConfiguration, localConfiguration);

		_eraseToolOptions.PropertyChanged -= OptionsPropertyChanged;
		_eraseToolOptions.PropertyChanged += OptionsPropertyChanged;

		_msg.DebugStopTiming(watch, "Erase Tool Options validated / initialized");

		string optionsMessage = _eraseToolOptions.GetLocalOverridesMessage();

		if (! string.IsNullOrEmpty(optionsMessage))
		{
			_msg.Info(optionsMessage);
		}
	}

	private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs args)
	{
		// Options changed - could implement refresh logic here if needed
	}

	protected override void ShowOptionsPane()
	{
		// Ensure options are initialized
		if (_eraseToolOptions == null)
		{
			InitializeOptions();
		}

		var viewModel = GetEraseViewModel();
		if (viewModel == null)
		{
			return;
		}

		viewModel.Options = _eraseToolOptions;
		viewModel.Activate(true);
	}

	protected override void HideOptionsPane()
	{
		var viewModel = GetEraseViewModel();
		viewModel?.Hide();
	}

	#region Tool Options DockPane

	[CanBeNull]
	private DockPaneEraseViewModelBase GetEraseViewModel()
	{
		if (OptionsDockPaneID == null)
		{
			return null;
		}

		const string optionsDockPaneNotFoundMessage = "Options DockPane with ID '{0}' not found";

		var viewModel =
			FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
				DockPaneEraseViewModelBase;
		return Assert.NotNull(viewModel, optionsDockPaneNotFoundMessage,
		                      OptionsDockPaneID);
	}

	#endregion

	private IDictionary<Feature, IReadOnlyList<Geometry>> CalculateResultFeatures(
		MapView activeView, Polygon sketchPolygon)
	{
		var selectedFeatures = GetApplicableSelectedFeatures(activeView);

		var resultFeatures = CalculateResultFeatures(selectedFeatures, sketchPolygon);

		return resultFeatures;
	}

	private IDictionary<Feature, IReadOnlyList<Geometry>> CalculateResultFeatures(
		[NotNull] IEnumerable<Feature> selectedFeatures,
		[NotNull] Polygon cutPolygon)
	{
		var inputs = selectedFeatures.ToDictionary(f => f, f => f.GetShape());

		if (inputs.Count == 0)
		{
			return new Dictionary<Feature, IReadOnlyList<Geometry>>();
		}

		cutPolygon = (Polygon) GeometryEngine.Instance.SimplifyAsFeature(cutPolygon, true);

		SpatialReference targetSpatialRef = inputs.Values.First().SpatialReference;
		cutPolygon = (Polygon) GeometryEngine.Instance.Project(cutPolygon, targetSpatialRef);

		if (inputs.Values.Any(g => g.GeometryType == GeometryType.Multipatch))
		{
			return CalculateMultipatchResultFeatures(inputs, cutPolygon);
		}

		Dictionary<Feature, IReadOnlyList<Geometry>> result =
			CalculateNonMultipatchResultFeatures(inputs, cutPolygon);

		return result;
	}

	private IDictionary<Feature, IReadOnlyList<Geometry>> CalculateMultipatchResultFeatures(
		[NotNull] Dictionary<Feature, Geometry> inputFeatureGeometries,
		[NotNull] Polygon cutPolygon)
	{
		// TODO Simplified method overload
		var selectedFeatures = new List<Feature>();

		Overlaps overlaps = new Overlaps();

		foreach (var kvp in inputFeatureGeometries)
		{
			Feature feature = kvp.Key;
			Geometry geometry = kvp.Value;

			// TODO: Extract geometry part that touches?
			if (GeometryUtils.Intersects(cutPolygon, geometry))
			{
				selectedFeatures.Add(feature);

				overlaps.AddGeometries(new GdbObjectReference(feature),
				                       new List<Geometry> { cutPolygon });
			}
		}

		var options = new RemoveOverlapsToolOptions(null, new PartialRemoveOverlapsOptions()
		                                                  {
			                                                  ZSource =
				                                                  new OverridableSetting<
					                                                  ChangeAlongZSource>(
					                                                  ChangeAlongZSource
						                                                  .SourcePlane, true)
		                                                  });

		CancellationToken cancellationToken = CancellationToken.None;

		RemoveOverlapsResult result =
			MicroserviceClient.RemoveOverlaps(
				selectedFeatures, overlaps, new List<Feature>(),
				options,
				cancellationToken);

		if (result == null)
		{
			_msg.Warn("No overlaps were removed.");
			return new Dictionary<Feature, IReadOnlyList<Geometry>>();
		}

		var updates = new Dictionary<Feature, IReadOnlyList<Geometry>>();

		foreach (OverlapResultGeometries resultPerFeature in result.ResultsByFeature)
		{
			Feature originalFeature = resultPerFeature.OriginalFeature;
			Geometry updatedGeometry = resultPerFeature.UpdatedGeometry;

			var updatedGeometries = new List<Geometry> { updatedGeometry };

			if (resultPerFeature.InsertGeometries.Count > 0)
			{
				updatedGeometries.AddRange(resultPerFeature.InsertGeometries);
			}

			updates.Add(originalFeature, updatedGeometries);
		}

		return updates;
	}

	private Dictionary<Feature, IReadOnlyList<Geometry>>
		CalculateNonMultipatchResultFeatures(
			Dictionary<Feature, Geometry> inputFeatureGeometries, Polygon cutPolygon)
	{
		var result = new Dictionary<Feature, IReadOnlyList<Geometry>>();

		bool preventMultipartResults = _eraseToolOptions?.PreventMultipartResults ?? false;

		foreach (var kvp in inputFeatureGeometries)
		{
			Feature feature = kvp.Key;
			Geometry featureGeometry = kvp.Value;

			featureGeometry = GeometryEngine.Instance.SimplifyAsFeature(featureGeometry, true);

			Geometry resultGeometry =
				GeometryEngine.Instance.Difference(featureGeometry, cutPolygon);

			if (resultGeometry.IsEmpty)
			{
				throw new Exception("One or more result geometries have become empty.");
			}

			IReadOnlyList<Geometry> resultGeometries;
			if (preventMultipartResults && resultGeometry is Multipart { PartCount: > 1 })
			{
				resultGeometries = GeometryEngine.Instance.MultipartToSinglePart(resultGeometry);
			}
			else
			{
				resultGeometries = new List<Geometry> { resultGeometry };
			}

			result.Add(feature, resultGeometries);
		}

		return result;
	}

	private static async Task<bool> SaveAsync(IDictionary<Feature, IReadOnlyList<Geometry>> result)
	{
		// create an edit operation
		var editOperation = new EditOperation();

		EditorTransaction transaction = new EditorTransaction(editOperation);

		return await transaction.ExecuteAsync(
			       editContext => Store(editContext, result),
			       "Erase polygon from feature(s)", GetDatasets(result.Keys));
	}

	private static void Store(
		EditOperation.IEditContext editContext,
		IDictionary<Feature, IReadOnlyList<Geometry>> result)
	{
		foreach (KeyValuePair<Feature, IReadOnlyList<Geometry>> keyValuePair in result)
		{
			Feature feature = keyValuePair.Key;
			IReadOnlyList<Geometry> geometries = keyValuePair.Value;

			Geometry update = null;
			List<Geometry> inserts = new List<Geometry>();
			foreach (Geometry geometry in geometries.OrderByDescending(
				         GeometryUtils.GetGeometrySize))
			{
				if (geometry.IsEmpty)
				{
					_msg.Warn("One or more result geometries have become empty.");
					continue;
				}

				if (update == null)
				{
					update = geometry;
				}
				else
				{
					inserts.Add(geometry);
				}
			}

			// Update:
			GdbPersistenceUtils.StoreShape(feature, update, editContext);

			foreach (Geometry newGeometry in inserts)
			{
				GdbPersistenceUtils.InsertTx(editContext, feature, newGeometry);
			}

			feature.Dispose();
		}

		_msg.InfoFormat("Successfully stored {0} updated features.", result.Count);
	}

	private static IEnumerable<Dataset> GetDatasets(IEnumerable<Feature> features)
	{
		foreach (Feature feature in features)
		{
			yield return feature.GetTable();
		}
	}
}

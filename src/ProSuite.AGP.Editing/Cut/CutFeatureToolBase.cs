using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Cut;

/// <summary>
/// Tool to cut features by drawing a polyline across them.
/// Similar to the classic "Cut Features" tool from TopGis.
/// </summary>
public abstract class CutFeatureToolBase : ConstructionToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected CutFeatureOptions _cutFeatureOptions;

	[CanBeNull] private OverridableSettingsProvider<PartialCutFeatureOptions> _settingsProvider;

	private List<ResultFeature> _resultFeatures;

	protected string OptionsFileName => "CutFeatureToolOptions.xml";

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
		SelectionCursors.CreateArrowCursors(Resources.CutFeatureOverlay);

	protected virtual IChangeAlongService MicroserviceClient { get; } = null;

	protected virtual string EditOperationDescription =>
		(_resultFeatures?.Count == 1) ? "Cut Feature" : "Cut Features";

	protected override Task OnToolActivatingCoreAsync()
	{
		InitializeOptions();

		return base.OnToolActivatingCoreAsync();
	}

	protected override SketchGeometryType GetSelectionSketchGeometryType()
	{
		return SketchGeometryType.Rectangle;
	}

	protected override SketchGeometryType GetEditSketchGeometryType()
	{
		return SketchGeometryType.Line;
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		return geometryType == GeometryType.Polygon ||
		       geometryType == GeometryType.Polyline ||
		       geometryType == GeometryType.Multipatch;
	}

	protected override void LogPromptForSelection()
	{
		_msg.Info("Select feature(s) to cut");
	}

	protected override void LogEnteringSketchMode()
	{
		_msg.Info("Draw the cut line across the selected features");
	}

	protected override async Task<bool> OnEditSketchCompleteCoreAsync(
		Geometry sketchGeometry,
		EditingTemplate editingTemplate,
		MapView mapView,
		CancelableProgressor progressor = null)
	{
		_msg.Debug(
			$"{nameof(CutFeatureToolBase)}.{nameof(OnSketchCompleteCoreAsync)}: Sketch completed for {Caption}.");

		if (sketchGeometry == null || sketchGeometry.IsEmpty)
		{
			_msg.Warn("The sketch is empty. Please draw a valid cut line.");
			return false;
		}

		if (sketchGeometry.GeometryType != GeometryType.Polyline)
		{
			_msg.Warn("The sketch must be a polyline. Please draw a line to cut the features.");
			return false;
		}

		var polyline = sketchGeometry as Polyline;
		Assert.NotNull(polyline, "Sketch is not a polyline");

		if (polyline.PointCount < 2)
		{
			_msg.Warn("The cut line must have at least 2 points.");
			return false;
		}

		// Move ALL feature processing into QueuedTask.Run to ensure thread safety
		return await QueuedTask.Run(async () =>
		{
			// Get selected features
			IList<Feature> selectedFeatures =
				GetApplicableSelectedFeatures(ActiveMapView).ToList();

			if (selectedFeatures.Count == 0)
			{
				_msg.Warn("No features selected. Please select one or more features to cut.");
				return false;
			}

			// Filter features
			List<Feature> featuresToCut = selectedFeatures
			                              .Where(f => CanSelectGeometryType(
				                                     f.GetShape().GeometryType))
			                              .ToList();

			if (featuresToCut.Count == 0)
			{
				_msg.Warn(
					"No cuttable features selected. Please select polygon, polyline, or multipatch features.");
				return false;
			}

			Geometry simpleGeometry = GeometryUtils.Simplify(polyline);
			if (simpleGeometry.IsEmpty)
			{
				_msg.Warn("The cut line is invalid after simplification.");
				return false;
			}

			polyline = simpleGeometry as Polyline;

			// A closed cut line is supported: it cuts a hole (cookie-cutter),
			// resulting in an inner and an outer feature.
			var cutSubcurve = new CutSubcurve(
				polyline, true, true, false, null, null, null);
			var cutSubcurves = new List<CutSubcurve> { cutSubcurve };

			IChangeAlongService microserviceClient = MicroserviceClient;

			if (microserviceClient == null)
			{
				_msg.Warn("Cut feature service is not available.");
				return false;
			}

			ChangeAlongZSource zValueSource = _cutFeatureOptions.ZValueSource;
			DatasetSpecificSettingProvider<ChangeAlongZSource> zSourceProvider =
				_cutFeatureOptions.GetZSourceOptionProvider();

			Dictionary<Feature, Geometry> updates;
			List<ResultFeature> inserts;

			try
			{
				_resultFeatures = microserviceClient.ApplyCutLines(
					featuresToCut,
					new List<Feature>(0),
					cutSubcurves,
					null,
					null,
					null,
					zValueSource,
					false,
					progressor?.CancellationToken ?? CancellationToken.None,
					out ChangeAlongCurves _,
					zSourceProvider);

				await ActiveMapView.ClearSketchAsync();

				if (_resultFeatures == null || _resultFeatures.Count == 0)
				{
					return false;
				}

				foreach (ResultFeature rf in _resultFeatures)
				{
					_msg.Debug(
						$"Result feature: OID={rf.OriginalFeature.GetObjectID()}, " +
						$"ChangeType={rf.ChangeType}, " +
						$"HasWarning={rf.HasWarningMessage}");
				}

				HashSet<long> editableClassHandles =
					ToolUtils.GetEditableClassHandles(ActiveMapView);

				updates = _resultFeatures
				          .Where(f => GdbPersistenceUtils.CanChange(
					                 f, editableClassHandles, RowChangeType.Update))
				          .ToDictionary(r => r.OriginalFeature, r => r.NewGeometry);

				inserts = _resultFeatures
				          .Where(f => GdbPersistenceUtils.CanChange(
					                 f, editableClassHandles, RowChangeType.Insert))
				          .ToList();

				List<Dataset> datasets = GdbPersistenceUtils
				                         .GetDatasetsNonEmpty(
					                         _resultFeatures.Select(rf => rf.OriginalFeature))
				                         .ToList();

				_msg.Debug($"Result features count: {_resultFeatures?.Count}");

				if (_resultFeatures == null || _resultFeatures.Count == 0)
				{
					_msg.Warn(
						"The selection was not cut. Please draw a sketch that crosses the selected feature(s).");
					return false;
				}

				if (updates.Count == 0 && inserts.Count == 0)
				{
					_msg.Warn(
						"No features to store: the selected features may not be editable.");
					return false;
				}

				_msg.Debug(
					$"Applying {updates.Count} update(s) and {inserts.Count} insert(s) to the database.");

				var newFeatures = new List<Feature>();

				bool success = await GdbPersistenceUtils.ExecuteInTransactionAsync(
					               editContext =>
					               {
						               GdbPersistenceUtils.UpdateTx(editContext, updates);
						               newFeatures.AddRange(
							               GdbPersistenceUtils.InsertTx(editContext, inserts));
						               return true;
					               },
					               EditOperationDescription,
					               datasets);

				if (! success)
				{
					return false;
				}

				await QueuedTask.Run(() => ToolUtils.SelectNewFeatures(
					                     newFeatures, ActiveMapView, false));

				if (_resultFeatures.Count == 1)
				{
					_msg.Info("Cut successful. 1 feature was created.");
				}
				else
				{
					_msg.InfoFormat("Cut successful. {0} features were created.",
					                _resultFeatures.Count);
				}

				return true;
			}
			catch (Exception ex)
			{
				_msg.Error("Error cutting features.", ex);
				return false;
			}
			finally
			{
				_resultFeatures = null;
			}
		});
	}

	protected void InitializeOptions()
	{
		string currentCentralConfigDir = CentralConfigDir;
		string currentLocalConfigDir = LocalConfigDir;

		if (_settingsProvider == null ||
		    _settingsProvider.IsStale(currentCentralConfigDir, currentLocalConfigDir))
		{
			_settingsProvider =
				new OverridableSettingsProvider<PartialCutFeatureOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			_settingsProvider.GetConfigurations(
				out PartialCutFeatureOptions localOptions,
				out PartialCutFeatureOptions centralOptions);

			_cutFeatureOptions = new CutFeatureOptions(centralOptions, localOptions);

			string optionsMessage = _cutFeatureOptions.GetLocalOverridesMessage();
			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}
		}
	}
}

using System;
using System.Collections.Generic;
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
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong;

public abstract class ReshapeAlongToolBase : ChangeAlongToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private static readonly Key _keyToggleNonDefaultReshapeSide = Key.S;

	private bool _nonDefaultReshapeSideMode;

	private readonly AreaDifferencePreview _areaDifferencePreview = new();
	[CanBeNull] private string _loggedNoPreviewReason;

	protected ReshapeAlongToolOptions _reshapeAlongToolOptions;

	[CanBeNull] private OverridableSettingsProvider<PartialReshapeAlongOptions> _settingsProvider;

	protected ReshapeAlongToolBase()
	{
		HandledKeys.Add(_keyToggleNonDefaultReshapeSide);
	}

	protected override bool UseNonDefaultReshapeSide => _nonDefaultReshapeSideMode;

	protected override bool RefreshSubcurvesOnRedraw =>
		_reshapeAlongToolOptions.ClipLinesOnVisibleExtent &&
		_reshapeAlongToolOptions.DisplayRecalculateCutLines;

	protected override string EditOperationDescription => "Reshape along";

	protected string OptionsFileName => "ReshapeAlongToolOptions.xml";

	[CanBeNull]
	protected virtual string OptionsDockPaneID => null;

	protected override TargetFeatureSelection TargetFeatureSelection =>
		_reshapeAlongToolOptions.TargetFeatureSelection;

	protected override SelectionCursors InitialSelectionCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.ReshapeAlongOverlay, "Cut Along Arrow");

	protected override SelectionCursors TargetSelectionCursors { get; } =
		SelectionCursors.CreateCrossCursors(Resources.ReshapeAlongOverlay, "Cut Along Cross");

	protected override Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
	{
		if (args.Key == _keyToggleNonDefaultReshapeSide)
		{
			_nonDefaultReshapeSideMode = ! _nonDefaultReshapeSideMode;

			if (_nonDefaultReshapeSideMode)
			{
				_msg.Info(
					"Enabled non-default reshape mode. The next reshape " +
					"to the inside of a polygon will remove the larger area.");
			}
			else
			{
				_msg.Info("Disabled non-default reshape mode.");
			}

			if (ChangeAlongCurves != null)
			{
				return QueuedTask.Run(
					() => UpdateFeedback(GetApplicableSelectedFeatures(ActiveMapView).ToList()));
			}
		}

		return Task.CompletedTask;
	}

	protected override void UpdatePreview(IList<Feature> selectedFeatures)
	{
		_areaDifferencePreview.Clear();

		IList<Feature> targetFeatures = ChangeAlongCurves?.TargetFeatures;

		if (! _reshapeAlongToolOptions.PreviewDifferenceAreas || targetFeatures == null)
		{
			return;
		}

		if (! GdbObjectUtils.Filter(selectedFeatures, GeometryType.Polygon).Any())
		{
			return;
		}

		List<CutSubcurve> reshapeCurves =
			ChangeAlongCurves.GetSelectedReshapeCurves(c => c.CanReshape && ! c.IsFiltered,
			                                           true);

		if (reshapeCurves.Count == 0)
		{
			return;
		}

		// Applied together, some combinations of reshape lines give a result that differs from
		// applying a part of them. The preview of all lines would be misleading then.
		if (FormsClosedLoop(reshapeCurves))
		{
			LogNoPreview("the reshape lines form a closed loop");
			return;
		}

		try
		{
			var cancellationTokenSource = new CancellationTokenSource(3000);

			AreaDifferences differences =
				GetAreaDifferences(selectedFeatures, targetFeatures, reshapeCurves,
				                   cancellationTokenSource.Token);

			if (reshapeCurves.Count > 1)
			{
				AreaDifferences oneByOne = AreaDifferences.Union(
					reshapeCurves.Select(c => GetAreaDifferences(
						                     selectedFeatures, targetFeatures,
						                     new List<CutSubcurve> { c },
						                     cancellationTokenSource.Token))
					             .ToList());

				if (! differences.IsSameArea(oneByOne))
				{
					LogNoPreview("applying the reshape lines one by one gives a different result");
					return;
				}
			}

			_loggedNoPreviewReason = null;

			_areaDifferencePreview.Update(differences);
		}
		catch (Exception e)
		{
			_msg.Warn($"Error generating preview: {e.Message}", e);
		}
	}

	protected override void ClearPreview()
	{
		_areaDifferencePreview.Clear();
	}

	[NotNull]
	private AreaDifferences GetAreaDifferences([NotNull] IList<Feature> selectedFeatures,
	                                           [NotNull] IList<Feature> targetFeatures,
	                                           [NotNull] List<CutSubcurve> reshapeCurves,
	                                           CancellationToken cancellationToken)
	{
		List<ResultFeature> resultFeatures = ChangeFeaturesAlong(
			selectedFeatures.ToList(), targetFeatures, reshapeCurves,
			UseNonDefaultReshapeSide, cancellationToken, out _);

		// With 'insert vertices on targets' the result also contains the targets
		return AreaDifferences.Calculate(
			resultFeatures.Where(r => selectedFeatures.Any(
				                     s => GdbObjectUtils.IsSameFeature(s, r.OriginalFeature))));
	}

	private void LogNoPreview([NotNull] string reason)
	{
		if (reason != _loggedNoPreviewReason)
		{
			_msg.Info($"No area preview: {reason}, so the result depends on which lines " +
			          "you select.");
			_loggedNoPreviewReason = reason;
		}
	}

	/// <summary>
	/// Whether the curves, connected at their end points, form a closed loop. Applied
	/// together, such curves replace a whole ring, which is not what applying a part of
	/// them does.
	/// </summary>
	private static bool FormsClosedLoop([NotNull] IList<CutSubcurve> curves)
	{
		var nodes = new List<MapPoint>();
		var parents = new List<int>();

		int GetNode(MapPoint point)
		{
			int index = nodes.FindIndex(n => GeometryUtils.IsSamePointXY(n, point));

			if (index < 0)
			{
				index = nodes.Count;
				nodes.Add(point);
				parents.Add(index);
			}

			return index;
		}

		int GetRoot(int node)
		{
			while (parents[node] != node)
			{
				node = parents[node];
			}

			return node;
		}

		foreach (CutSubcurve curve in curves)
		{
			int fromRoot = GetRoot(GetNode(curve.FromPoint));
			int toRoot = GetRoot(GetNode(curve.ToPoint));

			if (fromRoot == toRoot)
			{
				return true;
			}

			parents[fromRoot] = toRoot;
		}

		return false;
	}

	protected override async Task HandleEscapeAsync()
	{
		_nonDefaultReshapeSideMode = false;
		await base.HandleEscapeAsync();
	}

	protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
	{
		_nonDefaultReshapeSideMode = false;
		base.OnToolDeactivateCore(hasMapViewChanged);

		_settingsProvider?.StoreLocalConfiguration(_reshapeAlongToolOptions.LocalOptions);

		HideOptionsPane();

		return base.OnToolDeactivateCore(hasMapViewChanged);
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		return geometryType == GeometryType.Polyline ||
		       geometryType == GeometryType.Polygon;
	}

	protected override void LogUsingCurrentSelection()
	{
		_msg.Info(LocalizableStrings.ReshapeAlongTool_LogUsingCurrentSelection);
	}

	protected override void LogPromptForSelection()
	{
		_msg.Info(LocalizableStrings.ReshapeAlongTool_LogPromptForSelection);
	}

	protected override List<ResultFeature> ChangeFeaturesAlong(
		List<Feature> selectedFeatures,
		IList<Feature> targetFeatures,
		List<CutSubcurve> cutSubcurves,
		bool useNonDefaultReshapeSide,
		CancellationToken cancellationToken,
		out ChangeAlongCurves newChangeAlongCurves)
	{
		TargetBufferOptions targetBufferOptions = _reshapeAlongToolOptions.GetTargetBufferOptions();

		targetBufferOptions.ZSettingsModel = GetZSettingsModel();

		ReshapeCurveFilterOptions filterOptions =
			_reshapeAlongToolOptions.GetReshapeLineFilterOptions(ActiveMapView);

		double? customTolerance = _reshapeAlongToolOptions.UseCustomTolerance
			                          ? _reshapeAlongToolOptions.CustomTolerance
			                          : null;

		bool insertVerticesInTarget = _reshapeAlongToolOptions.InsertVerticesInTarget;

		List<ResultFeature> updatedFeatures = MicroserviceClient.ApplyReshapeLines(
			selectedFeatures, targetFeatures, cutSubcurves, targetBufferOptions, filterOptions,
			customTolerance, insertVerticesInTarget, useNonDefaultReshapeSide,
			cancellationToken, out newChangeAlongCurves);

		return updatedFeatures;
	}

	protected override void LogAfterPickTarget(
		ReshapeAlongCurveUsability reshapeCurveUsability)
	{
		if (reshapeCurveUsability == ReshapeAlongCurveUsability.CanReshape)
		{
			string selectReshapeLinesMsg =
				string.Format(
					"Select a line to reshape along by clicking on it, " +
					"draw a box to select lines completely within the box or press P " +
					"and draw a polygon to select lines completely within" +
					Environment.NewLine +
					"Select additional target features while holding SHIFT" +
					Environment.NewLine +
					"Press S to toggle non-default reshape side " +
					"(reshape in favor of the smaller area)");

			//if (ReshapeAlongCurves.ReshapeAlongOptions.DontShowDialog)
			//{
			//	selectReshapeLinesMsg += Environment.NewLine +
			//							 "Press [O] for additional options.";
			//}

			_msg.Info(selectReshapeLinesMsg);
		}
		else
		{
			if (reshapeCurveUsability == ReshapeAlongCurveUsability.NoTarget)
			{
				_msg.Info(
					"No target feature selected. Select one or more target line or polygon " +
					"features to align with. Press ESC to select a different feature.");
			}
			else if (reshapeCurveUsability ==
			         ReshapeAlongCurveUsability.AlreadyCongruent)
			{
				_msg.Info(
					"Source and target feature are already congruent. Select a different target feature.");
			}
			else
			{
				if (ChangeAlongCurves.HasSelectableCurves)
				{
					_msg.InfoFormat(
						"Not enough or ambiguous reshape lines. " +
						"Add additional targets or select yellow candidate lines.");
				}
				else
				{
					_msg.InfoFormat(
						"Unable to use target(s) to reshape. Add additional targets while holding SHIFT");
				}
			}
		}
	}

	protected override ChangeAlongCurves CalculateChangeAlongCurves(
		IList<Feature> selectedFeatures,
		IList<Feature> targetFeatures,
		CancellationToken cancellationToken)
	{
		TargetBufferOptions targetBufferOptions = _reshapeAlongToolOptions.GetTargetBufferOptions();

		targetBufferOptions.ZSettingsModel = GetZSettingsModel();

		ReshapeCurveFilterOptions filterOptions =
			_reshapeAlongToolOptions.GetReshapeLineFilterOptions(ActiveMapView);

		double? customTolerance = _reshapeAlongToolOptions.UseCustomTolerance
			                          ? _reshapeAlongToolOptions.CustomTolerance
			                          : null;

		ChangeAlongCurves result = MicroserviceClient.CalculateReshapeLines(
			selectedFeatures, targetFeatures, targetBufferOptions, filterOptions,
			customTolerance, cancellationToken);

		return result;
	}

	protected override void InitializeOptions()
	{
		Stopwatch watch = _msg.DebugStartTiming();

		// NOTE: by only reading the file locations we can save a couple of 100ms
		string currentCentralConfigDir = CentralConfigDir;
		string currentLocalConfigDir = LocalConfigDir;

		// For the time being, we always reload the options because they could have been updated in ArcMap
		_settingsProvider =
			new OverridableSettingsProvider<PartialReshapeAlongOptions>(
				currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

		PartialReshapeAlongOptions localConfiguration, centralConfiguration;

		_settingsProvider.GetConfigurations(out localConfiguration,
		                                    out centralConfiguration);

		_reshapeAlongToolOptions =
			new ReshapeAlongToolOptions(centralConfiguration, localConfiguration);

		_reshapeAlongToolOptions.PropertyChanged -= OptionsPropertyChanged;
		_reshapeAlongToolOptions.PropertyChanged += OptionsPropertyChanged;

		_msg.DebugStopTiming(watch, "Reshape Along Tool Options validated / initialized");

		string optionsMessage = _reshapeAlongToolOptions.GetLocalOverridesMessage();

		if (! string.IsNullOrEmpty(optionsMessage))
		{
			_msg.Info(optionsMessage);
		}
	}

	protected override bool AlwaysShowOptionsPane =>
		_reshapeAlongToolOptions?.AlwaysShowOptionsPane == true;

	protected override void ShowOptionsPane()
	{
		// Ensure options are initialized
		if (_reshapeAlongToolOptions == null)
		{
			InitializeOptions();
		}

		DockPaneReshapeAlongViewModelBase viewModel = GetReshapeAlongViewModel();
		if (viewModel == null)
		{
			return;
		}

		viewModel.Options = _reshapeAlongToolOptions;
		viewModel.Activate(true);
	}

	protected override void HideOptionsPane()
	{
		DockPaneReshapeAlongViewModelBase viewModel = GetReshapeAlongViewModel();
		viewModel?.Hide();
	}

	#region Tool Options DockPane

	[CanBeNull]
	private DockPaneReshapeAlongViewModelBase GetReshapeAlongViewModel()
	{
		if (OptionsDockPaneID == null)
		{
			return null;
		}

		var viewModel =
			FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
				DockPaneReshapeAlongViewModelBase;
		return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
		                      OptionsDockPaneID);
	}

	#endregion

	public void Dispose() { }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong;

public abstract class ReshapeAlongToolBase : ChangeAlongToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected ReshapeAlongToolOptions _reshapeAlongToolOptions;

	[CanBeNull]
	private OverridableSettingsProvider<PartialReshapeAlongOptions> _settingsProvider;

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

	protected override void OnToolDeactivateCore(bool hasMapViewChanged)
	{
		base.OnToolDeactivateCore(hasMapViewChanged);

		_settingsProvider?.StoreLocalConfiguration(_reshapeAlongToolOptions.LocalOptions);

		HideOptionsPane();
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
		CancellationToken cancellationToken,
		out ChangeAlongCurves newChangeAlongCurves)
	{
		var targetBufferOptions = _reshapeAlongToolOptions.GetTargetBufferOptions();

		targetBufferOptions.ZSettingsModel = GetZSettingsModel();

		var filterOptions = _reshapeAlongToolOptions.GetReshapeLineFilterOptions(ActiveMapView);

		double? customTolerance = _reshapeAlongToolOptions.UseCustomTolerance
			                          ? _reshapeAlongToolOptions.CustomTolerance
			                          : null;

		bool insertVerticesInTarget = _reshapeAlongToolOptions.InsertVerticesInTarget;

		var updatedFeatures = MicroserviceClient.ApplyReshapeLines(
			selectedFeatures, targetFeatures, cutSubcurves, targetBufferOptions, filterOptions,
			customTolerance, insertVerticesInTarget, cancellationToken,
			out newChangeAlongCurves);

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
					"Select additional target features while holding SHIFT");
			// TODO:
			//+
			//	Environment.NewLine +
			//	"For reshapes to the inside of a polygon press [{1}] + [{2}] " +
			//	"to reshape in favor of the smaller area.",
			//	PolygonScopeKey, ModifierNonDefaultSide1,
			//	ModifierNonDefaultSide2);

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
		var targetBufferOptions = _reshapeAlongToolOptions.GetTargetBufferOptions();

		targetBufferOptions.ZSettingsModel = GetZSettingsModel();

		var filterOptions = _reshapeAlongToolOptions.GetReshapeLineFilterOptions(ActiveMapView);

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

	protected override void ShowOptionsPane()
	{
		// Ensure options are initialized
		if (_reshapeAlongToolOptions == null)
		{
			InitializeOptions();
		}

		var viewModel = GetReshapeAlongViewModel();
		if (viewModel == null)
		{
			return;
		}

		viewModel.Options = _reshapeAlongToolOptions;
		viewModel.Activate(true);
	}

	protected override void HideOptionsPane()
	{
		var viewModel = GetReshapeAlongViewModel();
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
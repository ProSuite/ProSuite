using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.Editing.Generalize;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ReshapeAlongToolBase : ChangeAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ReshapeAlongToolOptions _reshapeAlongToolOptions;
		private ChangeAlongFeedback _feedback;


		[CanBeNull]
		private OverridableSettingsProvider<PartialReshapeAlongToolOptions> _settingsProvider;

		protected override string EditOperationDescription => "Reshape along";

		protected string OptionsFileName => "ReshapeAlongToolOptions.xml";

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_reshapeAlongToolOptions.LocalOptions);

			HideOptionsPane();
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			InitializeOptions();

			_feedback = new ChangeAlongFeedback();

			return base.OnToolActivatingCoreAsync();
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
			var updatedFeatures = MicroserviceClient.ApplyReshapeLines(
				selectedFeatures, targetFeatures, cutSubcurves,
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
			ChangeAlongCurves result = MicroserviceClient.CalculateReshapeLines(
				selectedFeatures, targetFeatures, cancellationToken);

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
				new OverridableSettingsProvider<PartialReshapeAlongToolOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			PartialReshapeAlongToolOptions localConfiguration, centralConfiguration;

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
			//// Create a new instance only if it doesn't exist yet
			//_settingsProvider ??= new OverridableSettingsProvider<PartialChangeAlongToolOptions>(
			//	CentralConfigDir, LocalConfigDir, OptionsFileName);

			//PartialChangeAlongToolOptions localConfiguration, centralConfiguration;
			//_settingsProvider.GetConfigurations(out localConfiguration, out centralConfiguration);

			//_reshapeAlongToolOptions =
			//	new ReshapeAlongToolOptions(centralConfiguration, localConfiguration);

			//// Update the view model with the options
			//var viewModel = GetReshapeAlongViewModel();
			//if (viewModel != null) {
			//	viewModel.Options = _reshapeAlongToolOptions;
			//}
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

		#region first phase selection cursor

		protected override Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.ReshapeAlongOverlay, null);
		}

		protected override Cursor GetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.ReshapeAlongOverlay,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.ReshapeAlongOverlay,
			                              Resources.Lasso);
		}

		protected override Cursor GetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.ReshapeAlongOverlay,
			                              Resources.Lasso,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.ReshapeAlongOverlay,
			                              Resources.Polygon);
		}

		protected override Cursor GetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.ReshapeAlongOverlay,
			                              Resources.Polygon,
			                              Resources.Shift);
		}

		#endregion

		#region second phase target selection cursor

		protected override Cursor GetTargetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.ReshapeAlongOverlay, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.ReshapeAlongOverlay,
			                              Resources.Shift, null, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.ReshapeAlongOverlay,
			                              Resources.Lasso, null, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.ReshapeAlongOverlay,
			                              Resources.Lasso, Resources.Shift, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.ReshapeAlongOverlay,
			                              Resources.Polygon, null, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.ReshapeAlongOverlay,
			                              Resources.Polygon, Resources.Shift, 10, 10);
		}

		#endregion

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
}

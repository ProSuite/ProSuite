using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class CutAlongToolBase : ChangeAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private AdvancedReshapeFeedback _feedback;

		protected CutAlongToolBase()
		{
			DisplayTargetLines = true;
		}

		protected ChangeAlongToolOptions _cutAlongToolOptions;

		[CanBeNull]
		private OverridableSettingsProvider<PartialChangeAlongToolOptions> _settingsProvider;

		protected override string EditOperationDescription => "Cut along";

		protected string OptionsFileName => "CutAlongToolOptions.xml";

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon;
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info(LocalizableStrings.CutPolygonAlongTool_LogUsingCurrentSelection);
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.CutPolygonAlongTool_LogPromptForSelection);
		}

		protected override List<ResultFeature> ChangeFeaturesAlong(
			List<Feature> selectedFeatures, IList<Feature> targetFeatures,
			List<CutSubcurve> cutSubcurves,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves)
		{
			var updatedFeatures = MicroserviceClient.ApplyCutLines(
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
						"Select a line to cut along by clicking on it, " +
						"draw a box to select lines completely within the box or press P " +
						"and draw a polygon to select lines completely within" +
						Environment.NewLine +
						"Select additional target features while holding SHIFT.");

				_msg.Info(selectReshapeLinesMsg);
			}
			else
			{
				if (reshapeCurveUsability == ReshapeAlongCurveUsability.NoTarget)
				{
					_msg.Info(
						"No target feature selected. Select one or more target line or polygon " +
						"features to cut along. Press ESC to select a different feature.");
				}
				else if (reshapeCurveUsability ==
				         ReshapeAlongCurveUsability.AlreadyCongruent)
				{
					_msg.Info(
						"Source and target features are already congruent. Select a different target feature.");
				}
				else
				{
					if (ChangeAlongCurves.HasSelectableCurves)
					{
						_msg.InfoFormat(
							"Not enough or ambiguous cut lines. " +
							"Add additional targets or select yellow candidate lines.");
					}
					else
					{
						_msg.InfoFormat(
							"Unable to use target(s) to cut along. Add additional targets while holding SHIFT");
					}
				}
			}
		}

		protected override ChangeAlongCurves CalculateChangeAlongCurves(
			IList<Feature> selectedFeatures, IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			ChangeAlongCurves result = MicroserviceClient.CalculateCutLines(
				selectedFeatures, targetFeatures, cancellationToken);

			return result;
		}

		protected void InitializeOptions()
		{
			// Create a new instance only if it doesn't exist yet (New as of 0.1.0, since we don't need to care for a change through ArcMap)
			_settingsProvider ??= new OverridableSettingsProvider<PartialChangeAlongToolOptions>(
				CentralConfigDir, LocalConfigDir, OptionsFileName);

			PartialChangeAlongToolOptions localConfiguration, centralConfiguration;
			_settingsProvider.GetConfigurations(out localConfiguration, out centralConfiguration);

			_cutAlongToolOptions =
				new ChangeAlongToolOptions(centralConfiguration, localConfiguration);
		}

		protected override async Task OnToolActivatingCoreAsync()
		{
			InitializeOptions();

			await base.OnToolActivatingCoreAsync();
		}

		#region first phase selection cursor

		protected override Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.CutPolygonAlongOverlay, null);
		}

		protected override Cursor GetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.CutPolygonAlongOverlay,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.CutPolygonAlongOverlay,
			                              Resources.Lasso);
		}

		protected override Cursor GetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.CutPolygonAlongOverlay,
			                              Resources.Lasso,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.CutPolygonAlongOverlay,
			                              Resources.Polygon);
		}

		protected override Cursor GetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.CutPolygonAlongOverlay,
			                              Resources.Polygon,
			                              Resources.Shift);
		}

		#endregion

		#region second phase target selection cursor

		protected override Cursor GetTargetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay, 10,
			                              10);
		}

		protected override Cursor GetTargetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay,
			                              Resources.Shift, null, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay,
			                              Resources.Lasso, null, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay,
			                              Resources.Lasso, Resources.Shift, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay,
			                              Resources.Polygon, null, 10, 10);
		}

		protected override Cursor GetTargetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay,
			                              Resources.Polygon, Resources.Shift, 10, 10);
		}

		#endregion

		#region Tool Options DockPane

		[CanBeNull]
		private DockPaneCutAlongViewModelBase GetCutAlongViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneCutAlongViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			var viewModel = GetCutAlongViewModel();

			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _cutAlongToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			var viewModel = GetCutAlongViewModel();
			viewModel?.Hide();
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class CutAlongToolBase : ChangeGeometryAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected CutAlongToolBase()
		{
			TargetSelectionCursor =
				ToolUtils.CreateCursor(Resources.Cross, Resources.CutPolygonAlongOverlay, 10, 10);
			TargetSelectionCursorShift = ToolUtils.CreateCursor(
				Resources.Cross, Resources.CutPolygonAlongOverlay, Resources.Shift, null, 10, 10);
			;

			DisplayTargetLines = true;
		}

		protected override string EditOperationDescription => "Cut along";

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

				//if (ChangeAlongCurves.ReshapeAlongOptions.DontShowDialog)
				//{
				//	selectReshapeLinesMsg += Environment.NewLine +
				//	                         string.Format(
				//		                         "Press [{0}] for additional options.",
				//		                         OptionsFormKey);
				//}

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
	}
}

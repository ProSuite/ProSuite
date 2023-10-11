using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ReshapeAlongToolBase : ChangeGeometryAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ReshapeAlongToolBase(SelectionSettings selectionSettings) : base(
			selectionSettings)
		{
			SelectionCursor = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorShift);

			TargetSelectionCursor = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorProcess);
			TargetSelectionCursorShift =
				ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorProcessShift);
		}

		protected override string EditOperationDescription => "Reshape along";

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
	}
}

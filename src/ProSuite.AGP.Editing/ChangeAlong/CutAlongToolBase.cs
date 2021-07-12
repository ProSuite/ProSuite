using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class CutAlongToolBase : ChangeGeometryAlongToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected CutAlongToolBase()
		{
			SelectionCursor = ToolUtils.GetCursor(Resources.CutPolygonAlongToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorShift);

			TargetSelectionCursor = ToolUtils.GetCursor(Resources.CutPolygonAlongToolCursorProcess);
			TargetSelectionCursorShift =
				ToolUtils.GetCursor(Resources.CutPolygonAlongToolCursorShift);
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

		protected override ChangeAlongCurves CalculateChangeAlongCurves(
			IList<Feature> selectedFeatures, IList<Feature> targetFeatures,
			CancellationToken cancellationToken)
		{
			ChangeAlongCurves result;
			result =
				MicroserviceClient.CalculateCutLines(
					selectedFeatures, targetFeatures, cancellationToken);

			return result;
		}
	}
}

using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.MergeFeatures;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using System;
using System.Threading.Tasks;

namespace ProSuite.AGP.Editing.SplitAndMerge
{
	public enum MergeAction
	{
		MergeIntoFirstFeature,
		MergeIntoSecondFeature
	}

	public abstract class SplitAndMergeToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		MergeAction _mergeAction = MergeAction.MergeIntoFirstFeature; 


		#region MapToolBase and OneClickToolBase overrides




		protected override async Task HandleEscapeAsync()
		{
			try
			{
				await ClearSelectionAsync();

				LogPromptForSelection();

				await StartSelectionPhaseAsync();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polygon ||
			       geometryType == GeometryType.Polyline ||
			       //geometryType == GeometryType.Multipatch ||
			       geometryType == GeometryType.Multipoint;
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info(LocalizableStrings.MergeFeaturesTool_LogUsingCurrentSelection);
		}

		protected override void LogPromptForSelection()
		{
			string message;

			string survivorText = _mergeAction == MergeAction.MergeIntoFirstFeature
					? LocalizableStrings.MergeFeaturesTool_SurvivorFirstObject
					: LocalizableStrings.MergeFeaturesTool_SurvivorLargerObject;
			//string survivorText =
			//	_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject
			//		? LocalizableStrings.MergeFeaturesTool_SurvivorFirstObject
			//		: LocalizableStrings.MergeFeaturesTool_SurvivorLargerObject;

			int selectedCount = ActiveMapView.Map.SelectionCount;

			if (selectedCount > 1)
			{
				message =
					"Press Enter to merge all selected features. The largest feature will survive after the merge." +
					Environment.NewLine +
					$"Alternatively, select a new first feature to merge{survivorText}";
			}
			else
			{
				message = string.Format(
					LocalizableStrings.MergeFeaturesTool_LogPromptForSelection,
					survivorText);
			}

			_msg.Info(message);
		}


		#endregion MapToolBase and OneClickToolBase overrides
	}
}

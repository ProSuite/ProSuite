using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.Selection
{
	public abstract class SelectionToolBase : OneClickToolBase
	{
		//TODO: ID from Config.daml; make abstract or similar
		private const string ConfigId_SelectionToolButton =
			"ProSuiteTools_Selection_SelectionToolButton";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// todo daro refactor
		protected SelectionToolBase()
		{
			IsSketchTool = true;

			SelectionCursor = ToolUtils.GetCursor(Resources.SelectionToolNormal);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.SelectionToolNormalShift);

			SelectOnlyEditFeatures = false;
			UnJoinedSelection = false;

			SetCursor(SelectionCursor);
		}

		protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			SetCheckState(ConfigId_SelectionToolButton, true);

			await base.OnToolActivateAsync(hasMapViewChanged);
		}

		protected override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			SetCheckState(ConfigId_SelectionToolButton, false);

			await base.OnToolDeactivateAsync(hasMapViewChanged);
		}

		protected override bool IsInSelectionPhase(bool shiftIsPressed)
		{
			return true;
		}

		protected override Task<bool> IsInSelectionPhaseCoreAsync(bool shiftIsPressed)
		{
			return Task.FromResult(true);
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor)
		{
			StartSelectionPhase();
		}

		protected override async Task HandleEscapeAsync()
		{
			Task task = QueuedTask.Run(
				() =>
				{
					ClearSelection();

					StartSelectionPhase();
				});

			await ViewUtils.TryAsync(task, _msg);
		}

		protected override void LogUsingCurrentSelection()
		{
			// throw new NotImplementedException();
		}

		protected override void LogPromptForSelection()
		{
			_msg.InfoFormat(LocalizableStrings.SelectionTool_LogPromptForSelection);
		}

		// todo daro: to DamlUtils?
		private static void SetCheckState(string damlId, bool isChecked)
		{
			IPlugInWrapper plugin =
				FrameworkApplication.GetPlugInWrapper(damlId);

			if (plugin != null)
			{
				plugin.Checked = isChecked;
			}
		}
	}
}

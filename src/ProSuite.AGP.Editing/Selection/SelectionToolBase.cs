using System.Collections.Generic;
using System.Windows.Forms;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionToolBase : OneClickToolBase
	{
		public SelectionToolBase()
		{
			IsSketchTool = true;
			SelectionSettings = new SelectionSettings();
			SelectionCursor = ToolUtils.GetCursor(Resources.SelectionToolNormal);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.SelectionToolNormalShift);

			SelectOnlyEditFeatures = false;

			SetCursor(SelectionCursor);
			
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private SelectionSettings _selectionSettings;

		protected override bool IsInSelectionPhase(bool shiftIsPressed)
		{
			return true;
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures, CancelableProgressor progressor)
		{
			StartSelectionPhase();
		}

		protected override void OnKeyDownCore(MapViewKeyEventArgs k)
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				SetCursor(SelectionCursorShift);
			}
			else
			{
				SetCursor(SelectionCursor);
			}

			_msg.VerboseDebug(() => $"Key {k.Key} was pressed.");
		}

		protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				SetCursor(SelectionCursorShift);
			}
			else
			{
				SetCursor(SelectionCursor);
			}
		}

		protected override bool HandleEscape()
		{
			// throw new NotImplementedException();
			return true;
		}

		protected override void LogUsingCurrentSelection()
		{
			// throw new NotImplementedException();
		}

		protected override void LogPromptForSelection()
		{
			_msg.InfoFormat(
				"Select one or more features by clicking or dragging a box. Options: " +
				"<br>-Press and hold SHIFT to add or remove features from the existing selection." +
				"<br>-Press CTRL and drag a box to show a list of selectable feature classes." +
				"<br>-Press CTRL and click on overlapping features to select a single feature." +
				"<br>-Press ALT and click to select all features at the click point.");
		}

		protected override SelectionSettings SelectionSettings
		{
			get => _selectionSettings;
			set => _selectionSettings = value;
		}
	}
}

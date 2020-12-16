using System.Windows.Forms;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionToolBase: OneClickToolBase
	{
		public SelectionToolBase()
		{
			IsSketchTool = true;
			SelectionSettings = new SelectionSettings();
			SelectionCursor = ToolUtils.GetCursor(Resources.SelectionToolNormal);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.SelectionToolNormalShift);
			
			SetCursor(SelectionCursor);
			//SelectionMode = SelectionMode.Normal;
		}
		
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private SelectionSettings _selectionSettings;
		
		protected override bool IsInSelectionPhase()
		{
			return true;
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
			_msg.VerboseDebug($"Key {k.Key} was pressed.");
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
			_msg.InfoFormat("Select features by clicking or dragging a box");
		}

		protected override SelectionSettings SelectionSettings
		{
			get => _selectionSettings;
			set => _selectionSettings = value;
		}
	}
}

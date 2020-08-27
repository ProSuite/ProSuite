using ProSuite.AGP.Editing.Erase;
using ProSuite.AGP.Editing.Selection;

namespace Clients.AGP.ProSuiteSolution.Editing
{
	public class EraseTool : EraseToolBase
	{
		private SelectionSettings _selectionSettings;

		public EraseTool()
		{
			_selectionSettings = new SelectionSettings();
		}

		protected override SelectionSettings SelectionSettings
		{
			get => _selectionSettings;
			set => _selectionSettings = value;
		}
	}
}

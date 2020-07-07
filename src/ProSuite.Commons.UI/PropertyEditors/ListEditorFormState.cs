using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.Commons.UI.PropertyEditors
{
	public class ListEditorFormState : FormState
	{
		private int _splitterDistance;

		public int SplitterDistance
		{
			get { return _splitterDistance; }
			set { _splitterDistance = value; }
		}
	}
}

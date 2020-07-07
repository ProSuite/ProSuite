using System.Windows.Forms;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	public class BasicFormStateManager : FormStateManager<FormState>
	{
		public BasicFormStateManager(Form form, string callingContextID)
			: base(form, callingContextID) { }

		public BasicFormStateManager(Form form) : base(form) { }
	}
}

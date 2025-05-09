using System.Windows;

namespace ProSuite.Commons.UI.Persistence.WPF
{
	public class BasicFormStateManager : FormStateManager<FormState>
	{
		public BasicFormStateManager(Window form, string callingContextID = null)
			: base(form, callingContextID) { }
	}
}

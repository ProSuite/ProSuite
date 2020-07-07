using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class NulloValidationMonitor : IValidationMonitor
	{
		#region IValidationMonitor Members

		public void ClearErrors() { }

		public void ShowErrorMessages(Notification notification) { }

		public void ShowErrorMessages(IScreenElement element, params string[] messages) { }

		public void ValidateField(IBoundScreenElement element, object model) { }

		public IScreenBinder Binder
		{
			set { }
		}

		#endregion
	}
}

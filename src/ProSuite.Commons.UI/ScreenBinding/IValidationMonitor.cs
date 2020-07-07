using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IValidationMonitor
	{
		IScreenBinder Binder { set; }

		void ClearErrors();

		void ShowErrorMessages([NotNull] Notification notification);

		void ShowErrorMessages([NotNull] IScreenElement element,
		                       params string[] messages);

		void ValidateField([NotNull] IBoundScreenElement element,
		                   [NotNull] object model);
	}
}

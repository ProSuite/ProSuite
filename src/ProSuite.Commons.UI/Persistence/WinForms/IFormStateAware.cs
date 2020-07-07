using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	public interface IFormStateAware<T> where T : IFormState
	{
		void RestoreState([NotNull] T formState);

		void GetState([NotNull] T formState);
	}
}

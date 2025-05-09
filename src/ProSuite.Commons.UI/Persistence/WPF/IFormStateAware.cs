using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Persistence.WPF
{
	public interface IFormStateAware<in T> where T : IFormState
	{
		void SaveState([NotNull] T formState);

		void RestoreState([NotNull] T formState);
	}
}

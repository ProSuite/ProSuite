using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Persistence.WPF;

public interface IFormStateAware<T> where T : IFormState
{
	void SaveState([NotNull] T formState);

	void RestoreState([NotNull] T formState);
}

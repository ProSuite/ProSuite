using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public interface ISettingsPersister<T> where T : class, new()
	{
		[NotNull]
		T Read();

		void Write([NotNull] T settings);
	}
}
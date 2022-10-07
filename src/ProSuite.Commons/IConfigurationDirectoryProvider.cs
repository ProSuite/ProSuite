using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	/// <summary>
	/// Provides a configuration directory of the form [AppDataFolder]\[CompanyName]\[Product Name].
	/// </summary>
	public interface IConfigurationDirectoryProvider
	{
		/// <summary>
		/// Returns the path of the specified subfolder in the application data configuration directory
		/// </summary>
		/// <param name="appDataFolder">The application data folder to be used (roaming or local profile)</param>
		/// <param name="localFolderName">The local folder name appended to the configuration directory</param>
		/// <returns></returns>
		[NotNull]
		string GetDirectory(AppDataFolder appDataFolder,
		                    [CanBeNull] string localFolderName = null);
	}
}

using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Config
{
	public class ConfigFilePathProvider : IConfigFilePathProvider
	{
		[NotNull] private readonly ConfigurationDirectorySearcher _appConfigDirSearcher;
		[NotNull] private readonly ConfigurationDirectorySearcher _loggingConfigDirSearcher;

		public ConfigFilePathProvider(
			[NotNull] ConfigurationDirectorySearcher configDirSearcher)
			: this(configDirSearcher, configDirSearcher) { }

		public ConfigFilePathProvider(
			[NotNull] ConfigurationDirectorySearcher appConfigDirSearcher,
			[NotNull] ConfigurationDirectorySearcher loggingConfigDirSearcher)
		{
			_appConfigDirSearcher = appConfigDirSearcher;
			_loggingConfigDirSearcher = loggingConfigDirSearcher;
		}

		#region Implementation of IConfigFilePathProvider

		public string GetAppConfigFilePath(string configFileName,
		                                   bool required = false)
		{
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			return _appConfigDirSearcher.GetConfigFilePath(configFileName, required);
		}

		public string GetLoggingConfigFilePath(string loggingConfigFileName,
		                                       bool required = false)
		{
			Assert.ArgumentNotNullOrEmpty(loggingConfigFileName, nameof(loggingConfigFileName));

			return _loggingConfigDirSearcher.GetConfigFilePath(loggingConfigFileName, required);
		}

		#endregion
	}
}

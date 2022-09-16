namespace ProSuite.Commons.Config
{
	public interface IConfigFilePathProvider
	{
		string GetAppConfigFilePath(string configFileName,
		                            bool required = false);

		string GetLoggingConfigFilePath(string loggingConfigFileName,
		                                bool required = false);
	}
}

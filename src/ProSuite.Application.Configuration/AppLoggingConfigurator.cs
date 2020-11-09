using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Application.Configuration
{
	public static class AppLoggingConfigurator
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static bool UsePrivateConfiguration
		{
			get { return LoggingConfigurator.UsePrivateConfiguration; }
			set { LoggingConfigurator.UsePrivateConfiguration = value; }
		}

		public static void Configure([NotNull] string configFileName,
		                             bool useDefaultConfiguration = true,
		                             bool dontOverwriteExistingConfiguration = false)
		{
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			var directorySearcher = new AppDirectorySearcher(
				ConfigurationUtils.CompanyName,
				ConfigurationUtils.ProductName,
				ConfigurationUtils.GetInstallDirectory().FullName);

			LoggingConfigurator.Configure(configFileName,
			                              directorySearcher.GetSearchPaths(),
			                              useDefaultConfiguration,
			                              dontOverwriteExistingConfiguration);
		}
	}
}

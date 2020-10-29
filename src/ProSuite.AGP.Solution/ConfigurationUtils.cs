using System.IO;
using System.Reflection;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution
{
	public static class ConfigurationUtils
	{
		private const string _configDirectoryName = "Config";
		private const string _ProSuiteDirectoryName = "ProSuite";

		private const string _companyName = "esri";
		private const string _productName = "prosuite";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[ContractAnnotation("required: true => notnull; required: false => canbenull")]
		public static string GetConfigFilePath([NotNull] string configFileName,
		                                       bool required = true)
		{
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			ProSuiteConfigurationDirectorySearcher searcher =
				GetConfigurationDirectorySearcher();

			return searcher.GetConfigFilePath(configFileName, required);
		}

		[NotNull]
		internal static ProSuiteConfigurationDirectorySearcher
			GetConfigurationDirectorySearcher()
		{
			return new ProSuiteConfigurationDirectorySearcher(
				_companyName,
				_productName,
				GetInstallDirectory().FullName,
				GetProSuiteDirectory().FullName);
		}

		/// <summary>
		/// Gets the Topgis install directory.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static DirectoryInfo GetInstallDirectory()
		{
			return Assert.NotNull(GetRequiredParent(GetExecutingAssemblyPath()).Parent,
			                      "no parent directory");
		}

		/// <summary>
		/// Gets the local config directory, at <c>&lt;install_directory&gt;\Config.</c> 
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static DirectoryInfo GetLocalConfigDirectory()
		{
			return new DirectoryInfo(Path.Combine(GetInstallDirectory().FullName,
			                                      _configDirectoryName));
		}

		/// <summary>
		/// Gets the config directory with the default config files 
		/// (copied/overwritten by the installer), at <c>&lt;install_directory&gt;\bin\Config</c> .
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static DirectoryInfo GetDefaultsConfigDirectory()
		{
			return new DirectoryInfo(Path.Combine(GetBinDirectory().FullName,
			                                      _configDirectoryName));
		}

		/// <summary>
		/// Gets the bin directory, i.e. the directory that contains this assembly.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static DirectoryInfo GetBinDirectory()
		{
			return GetRequiredParent(GetExecutingAssemblyPath());
		}

		/// <summary>
		/// Gets the full path of the provided executable file name.
		/// </summary>
		/// <param name="exeName">The name of the executable.</param>
		/// <returns></returns>
		//[NotNull]
		//public static string GetExecutablePath(string exeName)
		//{
		//	string extension = Path.GetExtension(exeName);

		//	if (string.IsNullOrEmpty(extension) ||
		//	    ! extension.EndsWith("exe", StringComparison.InvariantCultureIgnoreCase))
		//	{
		//		exeName = $"{exeName}.exe";
		//	}

		//	string exeLocation =
		//		Environment.GetEnvironmentVariable(
		//			EnvironmentVariables.TopgisExtraBinDirectory);

		//	if (! string.IsNullOrEmpty(exeLocation))
		//	{
		//		string result = Path.Combine(exeLocation, exeName);

		//		if (File.Exists(result))
		//		{
		//			_msg.DebugFormat(
		//				"Using TOPGIS executable defined by environment variable ({0}): {1}",
		//				EnvironmentVariables.TopgisExtraBinDirectory, exeLocation);

		//			return result;
		//		}

		//		_msg.DebugFormat(
		//			"The file {0} was not found in the directory defined by environment variable {1}: " +
		//			"Using standard installation directory",
		//			exeLocation, EnvironmentVariables.TopgisExtraBinDirectory);
		//	}

		//	DirectoryInfo assemblyDirectory = GetBinDirectory();

		//	exeLocation = Assert.NotNull(assemblyDirectory).FullName;

		//	return Path.Combine(exeLocation, exeName);
		//}

		/// <summary>
		/// Gets the provider of the application's directory for user-specific configuration files.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static IConfigurationDirectoryProvider GetAppDataConfigDirectoryProvider()
		{
			return new ConfigurationDirectoryProvider(_companyName, _productName);
		}

		#region Non-public methods

		/// <summary>
		/// Gets the common TopGen directory.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		private static DirectoryInfo GetProSuiteDirectory()
		{
			DirectoryInfo parent = GetInstallDirectory().Parent;
			Assert.NotNull(parent, "parent directory is null");

			return new DirectoryInfo(Path.Combine(parent.FullName, _ProSuiteDirectoryName));
		}

		[NotNull]
		private static DirectoryInfo GetRequiredParent([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			return Assert.NotNull(Directory.GetParent(path), "parent of path {0} is null",
			                      path);
		}

		[NotNull]
		private static string GetExecutingAssemblyPath()
		{
			return Assert.NotNull(
				Assembly.GetExecutingAssembly().Location,
				"assembly location is null");
		}

		#endregion
	}
}

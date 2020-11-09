using System;
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

		private const string _companyName = "Esri Switzerland";
		private const string _productName = "ProSuite";

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
		/// Gets the installation directory.
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
		/// Gets the provider of the application's directory for user-specific configuration files.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static IConfigurationDirectoryProvider GetAppDataConfigDirectoryProvider()
		{
			return new ConfigurationDirectoryProvider(_companyName, _productName);
		}

		/// <summary>
		/// Gets the full path of the provided executable file name.
		/// </summary>
		/// <param name="exeName">The name of the executable.</param>
		/// <returns></returns>
		[CanBeNull]
		public static string GetProSuiteExecutablePath(string exeName)
		{
			string extension = Path.GetExtension(exeName);

			if (string.IsNullOrEmpty(extension) ||
			    ! extension.EndsWith("exe", StringComparison.InvariantCultureIgnoreCase))
			{
				exeName = $"{exeName}.exe";
			}

			string fullPath;
			if (TryGetExecutablePathFromEnvVar(exeName, out fullPath))
			{
				return fullPath;
			}

			if (TryGetExecutablePathFromRegisteredInstallDir(exeName, out fullPath))
			{
				return fullPath;
			}

			DirectoryInfo assemblyDirectory = GetBinDirectory();

			string exeLocation = Assert.NotNull(assemblyDirectory).FullName;

			fullPath = Path.Combine(exeLocation, exeName);

			if (File.Exists(fullPath))
			{
				return fullPath;
			}

			return null;
		}

		private static bool TryGetExecutablePathFromRegisteredInstallDir([NotNull] string exeName,
		                                                                 out string fullPath)
		{
			fullPath = null;

			string registeredInstallDir = GetRegisteredInstallDirectory();

			if (string.IsNullOrEmpty(registeredInstallDir))
			{
				_msg.DebugFormat("ProSuite QA Extension installation is not installed.");
				return false;
			}

			string exeLocation = Path.Combine(registeredInstallDir, "bin", exeName);

			if (string.IsNullOrEmpty(exeLocation))
			{
				_msg.DebugFormat(
					"Executable {0} was not found in ProSuite QA Extension installation. Please consider installing the latest version.",
					exeName);

				return false;
			}

			string result = Path.Combine(exeLocation, exeName);

			if (File.Exists(result))
			{
				_msg.DebugFormat(
					"Using executable from ProSuite QA Extension install directory: {0}",
					exeLocation);

				fullPath = result;
				return true;
			}

			return false;
		}

		[CanBeNull]
		private static string GetRegisteredInstallDirectory()
		{
			string path =
				RegistryUtils.GetString(RegistryRootKey.LocalMachine,
				                        $@"SOFTWARE\Wow6432Node\{_companyName}\{_productName}",
				                        "InstallDirectory");

			if (string.IsNullOrEmpty(path) || ! Directory.Exists(path))
			{
				return null;
			}

			return path;
		}

		private static bool TryGetExecutablePathFromEnvVar(string exeName, out string fullPath)
		{
			// TODO: Add EnvironmentVaraiables static class (in core)

			fullPath = null;
			const string extraBinDir = "PROSUITE_EXTRA_BIN";

			string exeLocation = Environment.GetEnvironmentVariable(extraBinDir);

			if (! string.IsNullOrEmpty(exeLocation))
			{
				string result = Path.Combine(exeLocation, exeName);

				if (File.Exists(result))
				{
					_msg.DebugFormat(
						"Using executable defined by environment variable ({0}): {1}",
						extraBinDir, exeLocation);

					fullPath = result;
					return true;
				}

				_msg.DebugFormat(
					"The file {0} was not found in the directory defined by environment variable {1}.",
					exeLocation, extraBinDir);
			}

			return false;
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

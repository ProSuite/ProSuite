using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution
{
	/// <summary>
	/// Configuration search order:
	/// <list type="number">
	///    <item>Directory referenced in environment variable <c>PROSUITE_CONFIG_DIR</c></item>
	///    <item><c>&lt;local application data&gt;\esri\prosuite\Config</c></item>
	///    <item><c>&lt;roaming application data&gt;\esri\prosuite\Config</c></item>
	///    <item><c>&lt;common application data&gt;\esri\prosuite\Config</c></item>
	///    <item><c>&lt;installdir&gt;\Config</c></item>
	///    <item><c>&lt;installdir&gt;\bin\..\</c></item>
	///    <item><c>&lt;installdir&gt;\bin\..\..\</c></item>
	///    <item><c>&lt;installdir&gt;\bin\..\..\..\</c></item>
	///    <item><c>&lt;current working directory&gt;\</c></item>
	///    <item><c>&lt;installdir&gt;\bin\Config</c></item>
	///    <item><c>&lt;current working directory&gt;\Config</c></item>
	///    </list>
	/// </summary>
	internal class ProSuiteConfigurationDirectorySearcher : ConfigurationDirectorySearcher
	{
		private readonly string _installDirectory;
		private readonly string _prosuiteDirectory;
		private readonly string _registryKey;
		private const string _registryValueName = "InstallDirectory";
		private const string _configDirectoryName = "Config";
		private const string _configDirectoryVariableName = "PROSUITE_CONFIG_DIR";

		public ProSuiteConfigurationDirectorySearcher([NotNull] string companyName,
		                                            [NotNull] string productName,
		                                            [NotNull] string installDirectory,
		                                            [NotNull] string prosuiteDirectory)
		{
			Assert.ArgumentNotNullOrEmpty(companyName, nameof(companyName));
			Assert.ArgumentNotNullOrEmpty(productName, nameof(productName));
			Assert.ArgumentNotNullOrEmpty(installDirectory, nameof(installDirectory));
			Assert.ArgumentNotNullOrEmpty(prosuiteDirectory, nameof(prosuiteDirectory));

			_installDirectory = installDirectory;
			_prosuiteDirectory = prosuiteDirectory;

			ApplicationDataDirectory = Path.Combine(
				Path.Combine(companyName, productName),
				_configDirectoryName);

			_registryKey = $@"SOFTWARE\Wow6432Node\{companyName}\{productName}";
		}

		protected override string ApplicationDataDirectory { get; }

		protected override void CollectCurrentUserSearchPaths(
			ICollection<string> searchPaths)
		{
			// if environment variable exists, add the path it contains
			string dirPath = Environment.GetEnvironmentVariable(_configDirectoryVariableName);
			if (dirPath != null)
			{
				searchPaths.Add(dirPath);
			}

			base.CollectCurrentUserSearchPaths(searchPaths);
		}

		protected override void CollectAllUserSearchPaths(ICollection<string> searchPaths)
		{
			searchPaths.Add(_prosuiteDirectory);

			// add the path to <installdir>\config directory, as the first one to search
			string overrideConfigDirPath = Path.Combine(_installDirectory,
			                                            _configDirectoryName);
			searchPaths.Add(overrideConfigDirPath);
			// local, stable configuration -> first in list

			// add the path to the config directory defined in the registry.
			// this allows the 64bit installation to use the configuration from the 32bit installation
			DirectoryInfo registeredConfigDirectory = GetRegisteredConfigDirectory();
			if (registeredConfigDirectory != null)
			{
				searchPaths.Add(registeredConfigDirectory.FullName);
			}

			// get base method paths
			base.CollectAllUserSearchPaths(searchPaths);

			// add the path to the <installdir>\bin\config directory
			// default values, overwritten by installer -> later in list
			string defaultsConfigDirPath = Path.Combine(GetBinDirectory().FullName,
			                                            _configDirectoryName);
			searchPaths.Add(defaultsConfigDirPath);

			// add the path to the <cwd>\config directory. Required for 
			// locating the config files on the build server
			string cwdConfigDirPath = Path.Combine(Environment.CurrentDirectory,
			                                       _configDirectoryName);
			searchPaths.Add(cwdConfigDirPath);
		}

		[CanBeNull]
		private DirectoryInfo GetRegisteredConfigDirectory()
		{
			string path =
				RegistryUtils.GetString(RegistryRootKey.LocalMachine,
				                        _registryKey, _registryValueName);

			if (string.IsNullOrEmpty(path) || ! Directory.Exists(path))
			{
				return null;
			}

			string configPath = Path.Combine(path, _configDirectoryName);

			return Directory.Exists(configPath)
				       ? new DirectoryInfo(configPath)
				       : null;
		}
	}
}

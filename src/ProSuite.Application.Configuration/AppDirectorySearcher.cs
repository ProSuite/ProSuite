using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Application.Configuration
{
	/// <summary>
	/// Configuration search order:
	/// <list type="number">
	///    <item>Directory referenced in environment variable <c>PROSUITE_CONFIG_DIR</c></item>
	///    <item><c>&lt;local application data&gt;\{Company Name}\ProSuite\Config</c></item>
	///    <item><c>&lt;application data&gt;\{Company Name}\ProSuite\Config</c></item>
	///    <item><c>&lt;common application data&gt;\{Company Name}\ProSuite\Config</c></item>
	///    <item><c>&lt;installdir&gt;\Config</c></item>
	///    <item><c>&lt;installdir&gt;\bin\..\</c></item>
	///    <item><c>&lt;installdir&gt;\bin\..\..\</c></item>
	///    <item><c>&lt;installdir&gt;\bin\..\..\..\</c></item>
	///    <item><c>&lt;current working directory&gt;\</c></item>
	///    <item><c>&lt;installdir&gt;\bin\Config</c></item>
	///    <item><c>&lt;current working directory&gt;\Config</c></item>
	///    </list>
	/// </summary>
	internal class AppDirectorySearcher : ConfigurationDirectorySearcher
	{
		private readonly string _directoryName;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly string _installDirectory;

		public AppDirectorySearcher([NotNull] string companyName,
		                            [NotNull] string productName,
		                            [NotNull] string installDirectory,
		                            string directoryName = "Config")
		{
			Assert.ArgumentNotNullOrEmpty(companyName, nameof(companyName));
			Assert.ArgumentNotNullOrEmpty(productName, nameof(productName));
			Assert.ArgumentNotNullOrEmpty(installDirectory, nameof(installDirectory));

			_directoryName = directoryName;

			_installDirectory = installDirectory;
			ApplicationDataDirectory = Path.Combine(
				Path.Combine(companyName, productName), _directoryName);
		}

		protected override string ApplicationDataDirectory { get; }

		protected override void CollectCurrentUserSearchPaths(ICollection<string> paths)
		{
			// if environment variable exists, add the path it contains
			string dirPath = Environment.GetEnvironmentVariable(
				EnvironmentVariables.ConfigDirectoryVariableName);

			if (! StringUtils.IsNullOrEmptyOrBlank(dirPath))
			{
				if (FileSystemUtils.HasInvalidPathChars(dirPath))
				{
					_msg.WarnFormat(
						"The path specified in variable {0} contains invalid characters: {1}",
						EnvironmentVariables.ConfigDirectoryVariableName, dirPath);
				}
				else
				{
					paths.Add(dirPath);
				}
			}

			base.CollectCurrentUserSearchPaths(paths);
		}

		protected override void CollectAllUserSearchPaths(ICollection<string> paths)
		{
			// add the path to <installdir>\config directory
			// local configuration -> first in list
			string overrideConfigDirPath = Path.Combine(_installDirectory,
			                                            _directoryName);
			paths.Add(overrideConfigDirPath);

			// get base method paths
			base.CollectAllUserSearchPaths(paths);

			// add the path to the <installdir>\bin\config directory
			// default values, overwritten by installer -> later in list
			string defaultsConfigDirPath = Path.Combine(GetBinDirectory().FullName,
			                                            _directoryName);
			paths.Add(defaultsConfigDirPath);

			// add the path to the <cwd>\config directory. Required for 
			// locating the config files on the build server
			string cwdConfigDirPath = Path.Combine(Environment.CurrentDirectory,
			                                       _directoryName);
			paths.Add(cwdConfigDirPath);
		}
	}
}

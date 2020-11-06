using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;

namespace ProSuite.Microservices.Server.Geometry.Console
{
	public static class ConfigurationUtils
	{
		private const string _configDirectoryName = "Config";

		private const string _companyName = "Esri Switzerland";
		private const string _productName = "ProSuite";

		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull]
		public static string CompanyName => _companyName;

		[NotNull]
		public static string ProductName => _productName;

		public static bool TryGetArgumentsFromConfigFile(string[] args, string configFileName,
		                                                 out MicroserverArguments arguments,
		                                                 out string configFilePath)
		{
			arguments = null;
			configFilePath = null;

			if (args.Length == 0)
			{
				_msg.InfoFormat("Getting server host/port parameters from configuration file.");

				configFilePath = GetConfigFilePath(configFileName, false);

				if (configFilePath != null)
				{
					XmlSerializationHelper<MicroserverArguments> helper =
						new XmlSerializationHelper<MicroserverArguments>();

					arguments = helper.ReadFromFile(configFilePath);
					return true;
				}
			}

			return false;
		}

		public static string GetConfigFilePath([NotNull] string configFileName,
		                                       bool required = true)
		{
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			// TODO: Separate project ProSuite.Application.Configuration with unified ConfigurationUtils
			// AppConfigurationDirectorySearcher, etc. 
			//var searcher = new AppConfigurationDirectorySearcher(CompanyName,
			//                                                     ProductName,
			//                                                     GetInstallDirectory()
			//	                                                     .FullName);

			foreach (string path in GetLogConfigPaths())
			{
				if (path == null)
				{
					continue;
				}

				string filePath = Path.Combine(path, configFileName);

				if (File.Exists(filePath))
				{
					return filePath;
				}
			}

			return null;
		}

		[NotNull]
		public static IConfigurationDirectoryProvider GetAppDataConfigDirectoryProvider()
		{
			return new ConfigurationDirectoryProvider(_companyName, _productName);
		}

		private static IEnumerable<string> GetLogConfigPaths()
		{
			string assemblyPath = Assembly.GetExecutingAssembly().Location;

			string binDir = Assert.NotNullOrEmpty(Path.GetDirectoryName(assemblyPath));

			DirectoryInfo parentDir = Directory.GetParent(binDir);

			if (parentDir != null)
			{
				yield return Path.Combine(parentDir.FullName, _configDirectoryName);
			}

			yield return binDir;
			yield return Path.Combine(binDir, _configDirectoryName);
		}
	}
}

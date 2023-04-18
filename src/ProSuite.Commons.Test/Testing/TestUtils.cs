using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Testing
{
	public static class TestUtils
	{
		private const string _defaultLoggingConfigurationFile = "prosuite.logging.test.xml";

		public static void ConfigureUnitTestLogging(string loggingConfigurationFile = null)
		{
			string log4NetConfig = loggingConfigurationFile ?? _defaultLoggingConfigurationFile;

			List<string> logDirs = new List<string>();

			string currentDir = Environment.CurrentDirectory;

			if (Directory.Exists(currentDir))
			{
				logDirs.Add(currentDir);
			}

			string assemblyDir =
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Assert.NotNull(assemblyDir);

			if (Directory.Exists(assemblyDir))
			{
				logDirs.Add(assemblyDir);

				string testingDir = Path.Combine(assemblyDir, "Testing");

				if (Directory.Exists(testingDir))
				{
					logDirs.Add(testingDir);
				}

				DirectoryInfo parent = Directory.GetParent(assemblyDir);
				if (parent?.Exists == true)
				{
					logDirs.Add(parent.FullName);
				}
			}

			if (! LoggingConfigurator.Configure(log4NetConfig, logDirs))
			{
				Console.WriteLine("Logging configurator failed.");
				Console.WriteLine("logging configuration file: " + log4NetConfig);
				Console.WriteLine("Search directories: " + StringUtils.Concatenate(logDirs, ", "));
			}
			else
			{
				Console.WriteLine("Logging configured.");
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Appender;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;

namespace ProSuite.Commons.Logging
{
	public static class LoggingConfigurator
	{
		public static bool IsConfigured()
		{
			return Log4NetUtils.Log4NetIsConfigured();
		}

		public static bool UsePrivateConfiguration
		{
			get { return Log4NetUtils.UsePrivateRepository; }
			set { Log4NetUtils.UsePrivateRepository = value; }
		}

		/// <summary>
		/// Configures logging using a file name searched in a list of search directories. A value
		/// is returned indicating if the file was found (return value used since a non-default log
		/// configuration is considered optional at this level; a caller may still throw if
		/// it requires such a configuration).
		/// </summary>
		/// <param name="fileName">File name of the configuration file.</param>
		/// <param name="searchDirectories">The directories to search the file name in.</param>
		/// <returns>
		/// 	<c>true</c> if the specified file exists, <c>false</c> otherwise
		/// (in this case the default configuration is used, defined in the app.config file)
		/// </returns>
		public static bool Configure([NotNull] string fileName,
		                             [NotNull] IEnumerable<string> searchDirectories)
		{
			const bool useDefaultConfiguration = true;
			return Configure(fileName, searchDirectories, useDefaultConfiguration);
		}

		/// <summary>
		/// Configures logging using a file name searched in a list of search directories. A value
		/// is returned indicating if the file was found (return value used since a non-default log
		/// configuration is considered optional at this level; a caller may still throw if
		/// it requires such a configuration).
		/// </summary>
		/// <param name="fileName">File name of the configuration file.</param>
		/// <param name="searchDirectories">The directories to search the file name in.</param>
		/// <param name="useDefaultConfiguration">Indicates if the default (app.config)
		/// configuration should be used if the specified is not found.</param>
		/// <param name="dontOverwriteExistingConfiguration">if set to <c>true</c> an already existing configuration is not overwritten.</param>
		/// <returns>
		///   <c>true</c> if the specified file exists, <c>false</c> otherwise
		/// (in this case the default configuration is used, defined in the app.config file)
		/// </returns>
		public static bool Configure([NotNull] string fileName,
		                             [NotNull] IEnumerable<string> searchDirectories,
		                             bool useDefaultConfiguration,
		                             bool dontOverwriteExistingConfiguration = false)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.ArgumentNotNull(searchDirectories, nameof(searchDirectories));

			if (dontOverwriteExistingConfiguration && IsConfigured())
			{
				return false;
			}

			IMsg msg =
				new Msg(Assert.NotNull(MethodBase.GetCurrentMethod().DeclaringType));

			var anySearched = false;
			foreach (string searchDirectory in searchDirectories)
			{
				if (searchDirectory == null)
				{
					continue;
				}

				anySearched = true;
				string configFilePath = Path.Combine(searchDirectory, fileName);

				var xmlFileInfo = new FileInfo(configFilePath);

				if (! xmlFileInfo.Exists)
				{
					continue;
				}

				try
				{
					AppDomain.CurrentDomain.AssemblyResolve +=
						CurrentDomain_AssemblyResolve;

					Log4NetUtils.Configure(xmlFileInfo);
				}
				finally
				{
					AppDomain.CurrentDomain.AssemblyResolve -=
						CurrentDomain_AssemblyResolve;
				}

				msg.InfoFormat("Logging configured based on {0}", configFilePath);

				return true;
			}

			if (useDefaultConfiguration)
			{
				// Not found in search directories, or empty list of search directories.
				// Use default configuration.
				try
				{
					Log4NetUtils.Configure();

					if (anySearched)
					{
						msg.InfoFormat(
							"Logging config file {0} not found, applying default configuration",
							fileName);
					}
					else
					{
						msg.Info("Logging configured (defaults applied)");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(
						"Unable to apply default logging configuration ({0})",
						e.Message);
					Console.WriteLine("Logging not configured");
				}
			}
			else
			{
				if (anySearched)
				{
					Console.WriteLine(
						"Logging config file {0} not found (no default configuration applied)",
						fileName);
				}
				else
				{
					Console.WriteLine(
						"Logging not configured (no search paths, no defaults applied)");
				}
			}

			return false;
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender,
		                                                      ResolveEventArgs args)
		{
			return AssemblyResolveUtils.TryLoadAssembly(
				args.Name, Assembly.GetExecutingAssembly().CodeBase);
		}

		/// <summary>
		/// Configures logging using a file name relative to the assembly directory. A value
		/// is returned indicating if the file was found (return value used since a non-default log
		/// configuration is considered optional at this level; a caller may still throw if
		/// it requires such a configuration).
		/// </summary>
		/// <param name="fileName">File name of the configuration file.</param>
		/// <returns><c>true</c> if the specified file exists, <c>false</c> otherwise 
		/// (in this case the default configuration is used, defined in the app.config file)</returns>
		public static bool Configure([NotNull] string fileName)
		{
			string assemblyPath = Assembly.GetExecutingAssembly().Location;
			string assemblyDirPath = Path.GetDirectoryName(assemblyPath);

			return Configure(fileName, new[] {assemblyDirPath});
		}

		public static void SetGlobalProperty([NotNull] string propertyName,
		                                     object propertyValue)
		{
			GlobalContext.Properties[propertyName] = propertyValue;
		}

		public static object GetGlobalProperty([NotNull] string propertyName)
		{
			return GlobalContext.Properties[propertyName];
		}

		public static void ReplaceFileAppendersDirectory(string newDirectory)
		{
			foreach (IAppender appender in Log4NetUtils.GetAppenders())
			{
				FileAppender fileAppender = appender as FileAppender;

				if (fileAppender != null)
				{
					string origFile = fileAppender.File;

					string fileName = Assert.NotNullOrEmpty(Path.GetFileName(origFile));

					string newFile = Path.Combine(newDirectory, fileName);

					fileAppender.File = newFile;

					fileAppender.ActivateOptions();
				}
			}
		}
	}
}

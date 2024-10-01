using System;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;

namespace ProSuite.Commons.ManagedOptions
{
	public static class ManagedOptionsUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull]
		public static T GetConfiguration<T>([CanBeNull] string directory,
		                                    [NotNull] string fileName,
		                                    [CanBeNull] Action<string> receiveNotification)
			where T : class
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			if (! ConfigurationFileExists(directory, fileName))
			{
				return default;
			}

			string fullConfigFileName = Path.Combine(Assert.NotNull(directory), fileName);

			_msg.DebugFormat("Reading configuration file {0}...", fullConfigFileName);

			try
			{
				var helper = new XmlSerializationHelper<T>();

				return helper.ReadFromFile(fullConfigFileName, receiveNotification);
			}
			catch (InvalidOperationException e)
			{
				_msg.Debug("InvalidOperationException reading configuration file", e);

				// e.g. if the class name changes:
				receiveNotification?.Invoke(
					$"The configuration file {fullConfigFileName} could not be read. The configuration must be stored again with the current software version.");

				return default;
			}
			catch (Exception e)
			{
				_msg.Warn("Exception reading configuration file", e);
				throw;
			}
		}

		public static bool ConfigurationFileExists([CanBeNull] string configDirectory,
		                                           [NotNull] string configFileName)
		{
			if (string.IsNullOrEmpty(configDirectory))
			{
				return false;
			}

			if (! Directory.Exists(configDirectory))
			{
				return false;
			}

			string fullConfigFileName = Path.Combine(configDirectory, configFileName);

			return File.Exists(fullConfigFileName);
		}
	}
}

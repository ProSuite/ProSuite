using System;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Xml;

namespace ProSuite.Commons.ManagedOptions
{
	/// <summary>
	/// Provides settings that can be defined either centrally or locally or both using
	/// XML serialization.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OverridableSettingsProvider<T> where T : PartialOptionsBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private T _centralConfiguration;
		private readonly T _hardCodedDefaults;

		/// <summary>
		/// Initializes a new instance with a central settings directory containing also
		/// an XML file corresponding to settings class T.
		/// </summary>
		/// <param name="centralConfigDirectory"></param>
		/// <param name="localConfigDirectory"></param>
		/// <param name="configFileName"></param>
		public OverridableSettingsProvider([CanBeNull] string centralConfigDirectory,
		                                   [NotNull] string localConfigDirectory,
		                                   [NotNull] string configFileName)
			: this(localConfigDirectory, configFileName)
		{
			Assert.ArgumentNotNullOrEmpty(localConfigDirectory,
			                              nameof(localConfigDirectory));
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			CentralConfigDirectory = centralConfigDirectory;
		}

		/// <summary>
		/// Initializes a new instance with hard-coded default settings.
		/// </summary>
		/// <param name="defaultSettings"></param>
		/// <param name="localConfigDirectory"></param>
		/// <param name="configFileName"></param>
		public OverridableSettingsProvider(T defaultSettings,
		                                   [NotNull] string localConfigDirectory,
		                                   [NotNull] string configFileName)
			: this(localConfigDirectory, configFileName)
		{
			_hardCodedDefaults = defaultSettings;
		}

		/// <summary>
		/// Initializes a new instance without central configuration.
		/// </summary>
		/// <param name="localConfigDirectory"></param>
		/// <param name="configFileName"></param>
		public OverridableSettingsProvider([NotNull] string localConfigDirectory,
		                                   [NotNull] string configFileName)
		{
			Assert.ArgumentNotNullOrEmpty(localConfigDirectory,
			                              nameof(localConfigDirectory));
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			Assert.True(Directory.Exists(localConfigDirectory),
			            "Local configuration directory {0} does not exist.",
			            localConfigDirectory);

			LocalConfigDirectory = localConfigDirectory;
			ConfigFileName = configFileName;
		}

		[CanBeNull]
		public string CentralConfigDirectory { get; }

		[NotNull]
		public string LocalConfigDirectory { get; }

		[NotNull]
		public string ConfigFileName { get; }

		public void GetConfigurations([CanBeNull] out T localConfiguration,
		                              [CanBeNull] out T centralConfiguration,
		                              bool suppressXmlSchemaWarning = false)
		{
			centralConfiguration = null;

			var issueNotifications = new NotificationCollection();

			if (ConfigurationExists(CentralConfigDirectory, ConfigFileName))
			{
				_centralConfiguration = GetConfiguration(
					CentralConfigDirectory, ConfigFileName,
					issueText => issueNotifications.Add(issueText));

				if (issueNotifications.Count > 0 && ! suppressXmlSchemaWarning)
				{
					// This could happen if the schema changes other than just adding new nodes:
					_msg.WarnFormat(
						"The central configuration file {0} in {1} could not be read completely ({2}). Please review its structure and ensure it conforms to the current schema.",
						ConfigFileName, CentralConfigDirectory,
						issueNotifications.Concatenate(". "));
				}

				centralConfiguration = _centralConfiguration;
			}
			else if (_hardCodedDefaults != null)
			{
				centralConfiguration = _hardCodedDefaults;
			}

			localConfiguration = null;

			issueNotifications.Clear();
			if (ConfigurationExists(LocalConfigDirectory, ConfigFileName))
			{
				localConfiguration = GetConfiguration(
					LocalConfigDirectory, ConfigFileName,
					issueText => issueNotifications.Add(issueText));

				if (issueNotifications.Count > 0 && ! suppressXmlSchemaWarning)
				{
					// Could happen if the schema changes other than just adding new nodes:
					_msg.WarnFormat(
						"The local configuration file {0} in {1} could not be read completely ({2}). Please review and accept the current settings in the options dialog.",
						ConfigFileName, CentralConfigDirectory,
						issueNotifications.Concatenate(". "));
				}
			}
		}

		public void StoreLocalConfiguration(T localSettings)
		{
			string fullConfigFileName =
				Path.Combine(LocalConfigDirectory, ConfigFileName);

			var helper = new XmlSerializationHelper<T>();
			helper.SaveToFile(localSettings, fullConfigFileName);
		}

		public string GetXmlLocationLogMessage()
		{
			string localFile;

			if (ConfigurationExists(LocalConfigDirectory, ConfigFileName))
			{
				localFile = Path.Combine(LocalConfigDirectory, ConfigFileName);
			}
			else
			{
				localFile = "<not yet stored>";
			}

			string centralFile;

			bool centralDefaultsExist = ConfigurationExists(CentralConfigDirectory,
			                                                ConfigFileName);

			if (centralDefaultsExist)
			{
				Assert.NotNull(CentralConfigDirectory);
				centralFile = Path.Combine(CentralConfigDirectory, ConfigFileName);
			}
			else
			{
				centralFile = "<no central configuration defined>";
			}

			_msg.DebugFormat(
				"Using local configuration stored in {0}{1}Central defaults: {2}",
				localFile, Environment.NewLine, centralFile);

			return centralDefaultsExist
				       ? $"Using central defaults from {centralFile}"
				       : $"No central default options defined ({ConfigFileName}). Using factory default settings.";
		}

		public bool IsStale(string currentCentralConfigDir, string currentLocalConfigDir)
		{
			// string.Empty considers 2 null values equal
			return ! string.Equals(CentralConfigDirectory, currentCentralConfigDir) ||
			       ! string.Equals(LocalConfigDirectory, currentLocalConfigDir);
		}

		private static bool ConfigurationExists([CanBeNull] string configDirectory,
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

		[CanBeNull]
		private static T GetConfiguration([CanBeNull] string directory,
		                                  [NotNull] string fileName,
		                                  [CanBeNull] Action<string> receiveNotification)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			if (! ConfigurationExists(directory, fileName))
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
	}
}

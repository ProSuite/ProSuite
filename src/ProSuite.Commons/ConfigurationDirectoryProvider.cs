using System;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	/// <summary>
	/// Provides an application data directory for configuration files.
	/// </summary>
	public class ConfigurationDirectoryProvider : IConfigurationDirectoryProvider
	{
		[NotNull] private readonly string _companyName;
		[NotNull] private readonly string _productName;

		public ConfigurationDirectoryProvider([NotNull] string companyName,
		                                      [NotNull] string productName)
		{
			Assert.ArgumentNotNullOrEmpty(companyName, nameof(companyName));
			Assert.ArgumentNotNullOrEmpty(productName, nameof(productName));

			_companyName = companyName;
			_productName = productName;
		}

		#region Implementation of IConfigurationDirectoryProvider

		public string GetDirectory(AppDataFolder appDataFolder,
		                           string localFolderName = null)
		{
			return GetSettingsDirectory(GetSpecialFolder(appDataFolder),
			                            _companyName,
			                            _productName,
			                            localFolderName);
		}

		private static Environment.SpecialFolder GetSpecialFolder(
			AppDataFolder appDataFolder)
		{
			switch (appDataFolder)
			{
				case AppDataFolder.Local:
					return Environment.SpecialFolder.LocalApplicationData;

				case AppDataFolder.Roaming:
					return Environment.SpecialFolder.ApplicationData;

				default:
					throw new ArgumentOutOfRangeException(nameof(appDataFolder),
					                                      "Unknown app data folder type.");
			}
		}

		#endregion

		[NotNull]
		private static string GetSettingsDirectory(Environment.SpecialFolder specialFolder,
		                                           [NotNull] string companyName,
		                                           [NotNull] string productName,
		                                           [CanBeNull] string localFolderName =
			                                           null)
		{
			string localApp = Environment.GetFolderPath(specialFolder);

			string companyFolder = Path.Combine(localApp, companyName);

			string result = Path.Combine(companyFolder, productName);

			if (! string.IsNullOrEmpty(localFolderName))
			{
				result = Path.Combine(result, localFolderName);
			}

			if (! Directory.Exists(result))
			{
				Directory.CreateDirectory(result);
			}

			return result;
		}
	}
}
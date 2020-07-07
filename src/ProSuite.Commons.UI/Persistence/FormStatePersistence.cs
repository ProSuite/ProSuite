using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Persistence
{
	/// <summary>
	/// Gateway for reading/writing form state xml files. The settings are 
	/// returned/expected as a xml-serializable class and written to 
	/// a directory in either the local or roaming profile.
	/// </summary>
	public static class FormStatePersistence
	{
		private static bool _useRoamingProfile;
		private static string _folderPath;
		private static string _legacyFolderPath;
		private const string _folderName = "Forms";
		private const string _fileExtension = "frm.xml";

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static bool UseRoamingProfile
		{
			get { return _useRoamingProfile; }
			set
			{
				if (_useRoamingProfile == value)
				{
					return;
				}

				_useRoamingProfile = value;
				_folderPath = null;
			}
		}

		#region Non-public members

		[NotNull]
		internal static string GetFileName([NotNull] string formID,
		                                   [CanBeNull] string contextID)
		{
			const string format = "{0}{1}.{2}";

			return string.Format(format, formID,
			                     contextID == null
				                     ? string.Empty
				                     : "_" + contextID,
			                     _fileExtension);
		}

		/// <summary>
		/// Reads a object from the Xml file and returns it
		/// </summary>
		/// <param name="fileName">name of file to read settings from</param>
		/// <param name="type">type of object to deserialize from file</param>
		/// <returns></returns>
		[CanBeNull]
		internal static object ReadFromXml([NotNull] string fileName,
		                                   [NotNull] Type type)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.ArgumentNotNull(type, nameof(type));

			TryMoveLegacyFolderContent();

			string filePath = GetFilePathForReading(fileName);

			if (filePath == null)
			{
				return null;
			}

			var serializer = new XmlSerializer(type);

			TextReader reader = new StreamReader(filePath);
			try
			{
				return serializer.Deserialize(reader);
			}
			finally
			{
				reader.Close();
			}
		}

		/// <summary>
		/// Persists an object to the Xml file
		/// </summary>
		internal static void WriteToXml([NotNull] string fileName,
		                                [NotNull] object settings,
		                                [NotNull] Type type)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.ArgumentNotNull(settings, nameof(settings));
			Assert.ArgumentNotNull(type, nameof(type));

			string filePath = GetFilePathForWriting(fileName);

			var serializer = new XmlSerializer(type);
			TextWriter writer = new StreamWriter(filePath);
			try
			{
				serializer.Serialize(writer, settings);
			}
			finally
			{
				writer.Close();
			}

			TryCleanupLegacyFile(fileName);
		}

		[NotNull]
		private static string FolderPath => _folderPath ?? (_folderPath = GetFolderPath());

		[NotNull]
		private static string LegacyFolderPath =>
			_legacyFolderPath ?? (_legacyFolderPath = GetLegacyFolderPath());

		[NotNull]
		private static string GetFolderPath()
		{
			return EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				_useRoamingProfile
					? AppDataFolder.Roaming
					: AppDataFolder.Local,
				_folderName);
		}

		/// <summary>
		/// Gets the full path to the Xml file to de/serialize from/to
		/// </summary>
		/// <returns></returns>
		[NotNull]
		private static string GetFilePathForWriting([NotNull] string fileName)
		{
			if (! Directory.Exists(FolderPath))
			{
				CreateFormStateDirectory(FolderPath);
			}

			return Path.Combine(FolderPath, fileName);
		}

		/// <summary>
		/// Gets the full path to the Xml file to deserialize from.
		/// </summary>
		/// <returns>The path to the existing file, or null if the file does not exist.</returns>
		[CanBeNull]
		private static string GetFilePathForReading([NotNull] string fileName)
		{
			string filePath = Path.Combine(FolderPath, fileName);

			if (File.Exists(filePath))
			{
				return filePath;
			}

			string legacyFilePath = GetLegacyFilePath(fileName);

			return File.Exists(legacyFilePath)
				       ? legacyFilePath
				       : null;
		}

		[NotNull]
		private static string GetLegacyFilePath([NotNull] string fileName)
		{
			return Path.Combine(LegacyFolderPath, fileName);
		}

		private static void TryCleanupLegacyFile([NotNull] string fileName)
		{
			string legacyFilePath = GetLegacyFilePath(fileName);

			if (File.Exists(legacyFilePath))
			{
				TryDeleteLegacyFile(legacyFilePath);
			}
		}

		private static void TryMoveLegacyFolderContent()
		{
			if (! Directory.Exists(LegacyFolderPath))
			{
				return;
			}

			if (! Directory.Exists(FolderPath))
			{
				if (! TryCreateFormStateDirectory())
				{
					return;
				}
			}

			string searchPattern = string.Format("*.{0}", _fileExtension);

			foreach (string legacyFilePath in Directory.GetFiles(
				LegacyFolderPath, searchPattern))
			{
				string fileName = Assert.NotNull(Path.GetFileName(legacyFilePath));

				string targetFilePath = Path.Combine(FolderPath, fileName);

				if (! File.Exists(targetFilePath))
				{
					_msg.DebugFormat("Moving form state file {0} to {1}", legacyFilePath,
					                 FolderPath);

					try
					{
						File.Move(legacyFilePath, targetFilePath);
					}
					catch (Exception ex)
					{
						_msg.WarnFormat("Error moving form state file {0} to {1}: {2}",
						                legacyFilePath, FolderPath, ex.Message);
					}
				}
				else
				{
					TryDeleteLegacyFile(legacyFilePath);
				}
			}

			TryDeleteLegacyFolderIfEmpty();
		}

		private static bool TryCreateFormStateDirectory()
		{
			try
			{
				CreateFormStateDirectory(FolderPath);
			}
			catch (Exception ex)
			{
				_msg.WarnFormat("Error creating form state directory {0}: {1}",
				                FolderPath,
				                ex.Message);
				return false;
			}

			return true;
		}

		private static void TryDeleteLegacyFile([NotNull] string legacyFilePath)
		{
			_msg.DebugFormat("Deleting old form state file {0}", legacyFilePath);
			try
			{
				File.Delete(legacyFilePath);
			}
			catch (Exception ex)
			{
				_msg.WarnFormat("Error deleting old form state file {0}: {1}",
				                legacyFilePath, ex.Message);
			}
		}

		private static void TryDeleteLegacyFolderIfEmpty()
		{
			if (! Directory.Exists(LegacyFolderPath))
			{
				return;
			}

			if (Directory.GetFiles(LegacyFolderPath).Any())
			{
				return;
			}

			_msg.DebugFormat("Deleting old form state directory (empty) {0}", LegacyFolderPath);

			try
			{
				Directory.Delete(LegacyFolderPath);
			}
			catch (Exception ex)
			{
				_msg.WarnFormat("Error deleting old form state directory {0}: {1}",
				                LegacyFolderPath, ex.Message);
			}
		}

		private static void CreateFormStateDirectory(string folderPath)
		{
			_msg.DebugFormat("Creating form state directory: {0}", folderPath);

			Directory.CreateDirectory(folderPath);
		}

		[NotNull]
		private static string GetLegacyFolderPath()
		{
			string localApp = Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData);

			string assemblyPath = Assembly.GetExecutingAssembly().Location;
			string assemblyName = Assert.NotNull(Path.GetFileNameWithoutExtension(assemblyPath));

			return Path.Combine(localApp, assemblyName);
		}

		#endregion
	}
}

using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Data.Realtime;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class WorkspaceUtils
	{
		public static bool IsSameDatastore(Datastore datastore1, Datastore datastore2)
		{
			// todo daro check ProProcessingUtils
			if (ReferenceEquals(datastore1, datastore2)) return true;
			if (Equals(datastore1.Handle, datastore2.Handle)) return true;

			return false;
		}

		[CanBeNull]
		public static Version GetDefaultVersion([NotNull] Datastore datastore)
		{
			Assert.ArgumentNotNull(datastore, nameof(datastore));

			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase &&
			    geodatabase.IsVersioningSupported())
			{
				using (VersionManager versionManager = geodatabase.GetVersionManager())
				{
					Version version = versionManager.GetCurrentVersion();
					Version parent;
					while ((parent = version.GetParent()) != null)
					{
						version = parent;
					}

					return version;
				}
			}

			return null;
		}

		[CanBeNull]
		public static Version GetCurrentVersion([NotNull] Datastore datastore)
		{
			Assert.ArgumentNotNull(datastore, nameof(datastore));

			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase &&
			    geodatabase.IsVersioningSupported())
			{
				VersionManager versionManager = geodatabase.GetVersionManager();

				Version version = versionManager.GetCurrentVersion();

				return version;
			}

			return null;
		}

		/// <summary>
		/// Gets a displayable text describing a given workspace.
		/// </summary>
		/// <param name="datastore">The workspace.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetDatastoreDisplayText([NotNull] Datastore datastore)
		{
			Assert.ArgumentNotNull(datastore, nameof(datastore));

			Connector connector = datastore.GetConnector();

			return GetDatastoreDisplayText(connector);
		}

		/// <summary>
		/// Gets a displayable text describing a given datastore connector.
		/// </summary>
		/// <param name="connector">The connector.</param>
		/// <returns></returns>
		public static string GetDatastoreDisplayText([CanBeNull] Connector connector)
		{
			// TODO: Add parameter bool detailed which includes the full info including user names etc.

			if (connector == null)
			{
				return "<null>";
			}

			const string nullPathText = "<undefined path>";

			switch (connector)
			{
				case DatabaseConnectionFile dbConnection:
					return $"SDE connection {dbConnection.Path?.AbsolutePath ?? nullPathText}";

				case DatabaseConnectionProperties dbConnectionProps:
					return GetConnectionDisplayText(dbConnectionProps);

				case FileGeodatabaseConnectionPath fileGdbConnection:
					return $"File Geodatabase {fileGdbConnection.Path}";

				case FileSystemConnectionPath fileSystemConnection:
					return $"{fileSystemConnection.Type} datastore {fileSystemConnection.Path}";

				case MemoryConnectionProperties memoryConnectionProperties:
					return $"In-Memory Geodatabase {memoryConnectionProperties.Name}";

				case MobileGeodatabaseConnectionPath mobileConnectionProperties:
					return $"Mobile Geodatabase {mobileConnectionProperties.Path}";

				case PluginDatasourceConnectionPath pluginConnectionPath:
					return
						$"Plug-in {pluginConnectionPath.PluginIdentifier} {pluginConnectionPath.DatasourcePath}";

				case RealtimeServiceConnectionProperties realtimeServiceConnection:
					return
						$"Real-Time connection ({realtimeServiceConnection.Type}) {realtimeServiceConnection.URL}";

				case ServiceConnectionProperties serviceConnection:
					return $"Service connection {serviceConnection.URL}";

				case SQLiteConnectionPath sqLiteConnection:
					return $"SQLite Database {sqLiteConnection.Path}";

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported workspace type: {connector?.GetType()}");
			}
		}

		private static string GetConnectionDisplayText(
			[NotNull] DatabaseConnectionProperties dbConnectionProps)
		{
			const string nullVersionText = "<undefined version>";

			string versionName = dbConnectionProps.Version ??
			                     dbConnectionProps.Branch ?? nullVersionText;

			string databaseName = dbConnectionProps.Database;
			string instance = dbConnectionProps.Instance;

			return string.IsNullOrEmpty(databaseName)
				       ? string.Format("{0} - {1}", instance, versionName)
				       : string.Format("{0} ({1}) - {2}", databaseName,
				                       instance, versionName);
		}
	}
}

using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Knowledge;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Data.Realtime;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class WorkspaceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Opens a file geodatabase. This method must be run on the MCT. Use QueuedTask.Run.
		/// </summary>
		/// <returns></returns>
		public static ArcGIS.Core.Data.Geodatabase OpenFileGeodatabase([NotNull] string path)
		{
			var connectionPath = new FileGeodatabaseConnectionPath(new Uri(path));

			return (ArcGIS.Core.Data.Geodatabase) OpenDatastore(connectionPath);
		}

		/// <summary>
		/// Opens a file geodatabase. This method must be run on the MCT. Use QueuedTask.Run.
		/// </summary>
		/// <returns></returns>
		public static Datastore OpenDatastore([NotNull] Connector connector)
		{
			try
			{
				switch (connector)
				{
					case DatabaseConnectionFile dbConnection:
						return new ArcGIS.Core.Data.Geodatabase(dbConnection);

					case DatabaseConnectionProperties dbConnectionProps:
						return new ArcGIS.Core.Data.Geodatabase(dbConnectionProps);

					case FileGeodatabaseConnectionPath fileGdbConnection:
						return new ArcGIS.Core.Data.Geodatabase(fileGdbConnection);

					case FileSystemConnectionPath fileSystemConnection:
						return new FileSystemDatastore(fileSystemConnection);

					case KnowledgeGraphConnectionProperties knowledgeGraphConnection:
						return new KnowledgeGraph(knowledgeGraphConnection);

					case MemoryConnectionProperties memoryConnectionProperties:
						return new ArcGIS.Core.Data.Geodatabase(memoryConnectionProperties);

					case MobileGeodatabaseConnectionPath mobileConnectionProperties:
						return new ArcGIS.Core.Data.Geodatabase(mobileConnectionProperties);

					case PluginDatasourceConnectionPath pluginConnectionPath:
						return new PluginDatastore(pluginConnectionPath);

					case RealtimeServiceConnectionProperties realtimeServiceConnection:
						return new RealtimeDatastore(realtimeServiceConnection);

					case ServiceConnectionProperties serviceConnection:
						return new ArcGIS.Core.Data.Geodatabase(serviceConnection);

					case SQLiteConnectionPath sqLiteConnection:
						return new Database(sqLiteConnection);

					default:
						throw new ArgumentOutOfRangeException(
							$"Unsupported workspace type: {connector?.GetType()}");
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Failed to open Datastore {GetDatastoreDisplayText(connector)}", e);
				throw;
			}
		}

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

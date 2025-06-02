using System;
using System.IO;
using System.Text;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Data.Realtime;
using ProSuite.Commons.Ado;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
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
		/// Opens a geodatabase using the provided catalog path. This method must be run on the MCT.
		/// Use QueuedTask.Run.
		/// </summary>
		/// <param name="catalogPath"></param>
		/// <returns></returns>
		public static ArcGIS.Core.Data.Geodatabase OpenGeodatabase(string catalogPath)
		{
			string extension = Path.GetExtension(catalogPath);

			if (extension
			    .Equals(".sde", StringComparison.InvariantCultureIgnoreCase))
			{
				DatabaseConnectionFile connector = new DatabaseConnectionFile(new Uri(catalogPath));

				return new ArcGIS.Core.Data.Geodatabase(connector);
			}

			if (extension
			    .Equals(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				var connector = new FileGeodatabaseConnectionPath(new Uri(catalogPath));

				return new ArcGIS.Core.Data.Geodatabase(connector);
			}

			// Mobile Geodatabase
			if (extension
			    .Equals(".geodatabase", StringComparison.InvariantCultureIgnoreCase))
			{
				var connector = new MobileGeodatabaseConnectionPath(new Uri(catalogPath));

				return new ArcGIS.Core.Data.Geodatabase(connector);
			}
			
			string message =
				$"Finder: Unsupported geodatabase extension: {extension} for path: {catalogPath}";
			_msg.Debug(message);
			throw new NotSupportedException(message);
		}

		/// <summary>
		/// Opens a file geodatabase. This method must be run on the MCT. Use QueuedTask.Run.
		/// </summary>
		/// <returns></returns>
		[NotNull]
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

					// Only supported starting with Pro 3.2
					//case KnowledgeGraphConnectionProperties knowledgeGraphConnection:
					//	return new KnowledgeGraph(knowledgeGraphConnection);

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
				string message = $"Failed to open Datastore {GetDatastoreDisplayText(connector)}";
				_msg.Debug(message, e);
				throw new IOException($"{message}: {e.Message}", e);
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

		public static DatabaseConnectionProperties GetConnectionProperties(
			[NotNull] string connectionString)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			var builder = new ConnectionStringBuilder(connectionString);

			Assert.True(
				Enum.TryParse(builder["dbclient"], ignoreCase: true,
				              out EnterpriseDatabaseType databaseType),
				$"Cannot parse {nameof(EnterpriseDatabaseType)} from connection string {connectionString}");

			Assert.True(
				Enum.TryParse(builder["authentication_mode"], ignoreCase: true,
				              out AuthenticationMode authMode),
				$"Cannot parse {nameof(AuthenticationMode)} from connection string {connectionString}");

			string instance = builder["instance"];

			// Real-world examples for instance:
			// Oracle:
			// - "sde:oracle11g:TOPGIST:SDE"
			// - "sde:oracle$sde:oracle11g:gdzh"

			// PostgreSQL:
			// sde:postgresql:localhost

			// NOTE: Sometimes the DB_CONNECTION_PROPERTIES contains the single instance name,
			//       but it can also contain the colon-separated components.
			// TODO: Test with other connections!
			string database = builder["database"];
			//if (databaseType == EnterpriseDatabaseType.PostgreSQL)
			//{
			//	database = builder["database"];
			//}
			//else
			//{
			//	database = string.IsNullOrEmpty(builder["server"])
			//		           ? builder["database"]
			//		           : builder["server"];
			//}

			string[] strings = instance?.Split(':');

			if (strings?.Length > 1)
			{
				string lastItem = strings[^1];

				if (lastItem.Equals("SDE", StringComparison.OrdinalIgnoreCase))
				{
					// Take the second last item
					instance = strings[^2];
				}
				else
				{
					instance = lastItem;
				}
			}

			var connectionProperties =
				new DatabaseConnectionProperties(databaseType)
				{
					AuthenticationMode = authMode,
					ProjectInstance = builder["project_instance"],
					Database = database,
					Instance = instance,
					Version = builder["version"],
					Branch = builder["branch"],
					Password = builder["encrypted_password"],
					User = builder["user"]
				};

			return connectionProperties;
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
					return $"File Geodatabase {fileGdbConnection.Path.AbsolutePath}";

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

		public static WorkspaceDbType GetWorkspaceDbType(Datastore datastore)
		{
			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase)
			{
				GeodatabaseType gdbType = geodatabase.GetGeodatabaseType();

				// TODO: Test newer workspace types, such as sqlite, Netezza

				var connector = geodatabase.GetConnector();

				if (gdbType == GeodatabaseType.LocalDatabase)
				{
					return WorkspaceDbType.FileGeodatabase;
				}

				if (gdbType == GeodatabaseType.FileSystem)
				{
					return WorkspaceDbType.FileSystem;
				}

				if (gdbType != GeodatabaseType.RemoteDatabase)
				{
					return WorkspaceDbType.Unknown;
				}

				if (connector is DatabaseConnectionProperties connectionProperties)
				{
					switch (connectionProperties.DBMS)
					{
						case EnterpriseDatabaseType.Oracle:
							return WorkspaceDbType.ArcSDEOracle;
						case EnterpriseDatabaseType.Informix:
							return WorkspaceDbType.ArcSDEInformix;
						case EnterpriseDatabaseType.SQLServer:
							return WorkspaceDbType.ArcSDESqlServer;
						case EnterpriseDatabaseType.PostgreSQL:
							return WorkspaceDbType.ArcSDEPostgreSQL;
						case EnterpriseDatabaseType.DB2:
							return WorkspaceDbType.ArcSDEDB2;
						case EnterpriseDatabaseType.SQLite:
							return WorkspaceDbType.MobileGeodatabase;
						default:
							return WorkspaceDbType.ArcSDE;
					}
				}

				// No connection properties (probably SDE file -> TODO: How to find the connection details? Connection string?)
				return WorkspaceDbType.ArcSDE;
			}

			return WorkspaceDbType.Unknown;
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
				       ? $"{instance} - {versionName}"
				       : $"{databaseName} ({instance}) - {versionName}";
		}

		public static string ConnectionPropertiesToString(
			[NotNull] DatabaseConnectionProperties dbConnectionProps)
		{
			var sb = new StringBuilder();

			sb.Append("DBMS: ").AppendLine(dbConnectionProps.DBMS.ToString());
			sb.Append("Database: ").AppendLine(dbConnectionProps.Database);
			sb.Append("Instance: ").AppendLine(dbConnectionProps.Instance);
			sb.Append("Authentication Mode: ")
			  .AppendLine(dbConnectionProps.AuthenticationMode.ToString());
			sb.Append("User: ").AppendLine(dbConnectionProps.User);
			sb.Append("Version: ").AppendLine(dbConnectionProps.Version);
			sb.Append("Branch: ").AppendLine(dbConnectionProps.Branch);
			sb.Append("Project Instance: ").Append(dbConnectionProps.ProjectInstance);

			return sb.ToString();
		}
	}
}

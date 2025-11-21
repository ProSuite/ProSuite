using System;
using System.IO;
using System.Text;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Knowledge;
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
				$"Unsupported geodatabase extension: {extension} for path: {catalogPath}";
			_msg.Debug(message);
			throw new NotSupportedException(message);
		}

		/// <summary>
		/// Opens a file geodatabase. This method must be run on the MCT. Use QueuedTask.Run.
		/// </summary>
		[NotNull]
		public static Datastore OpenDatastore([NotNull] Connector connector)
		{
			if (connector is null)
				throw new ArgumentNullException(nameof(connector));

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
							$"Unsupported workspace type: {connector.GetType()}");
				}
			}
			catch (Exception e)
			{
				string message =
					$"Failed to open Datastore {GetDatastoreDisplayText(connector)}: {e.Message}";
				_msg.Debug(message, e);

				throw new IOException(message, e);
			}
		}

		/// <summary>
		/// Creates a connector for the specified workspace factory and connection string.
		/// </summary>
		/// <param name="factory">The workspace factory type.</param>
		/// <param name="connectionString">The connection string.</param>
		/// <returns>A connector appropriate for the specified workspace factory.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported workspace factory is specified.</exception>
		[NotNull]
		public static Connector CreateConnector(WorkspaceFactory factory,
		                                        [NotNull] string connectionString)
		{
			Assert.ArgumentNotNull(connectionString, nameof(connectionString));

			switch (factory)
			{
				case WorkspaceFactory.FileGDB:
					string filePath = connectionString;
					// Extract actual path if it has a DATABASE= prefix
					if (connectionString.StartsWith("DATABASE=",
					                                StringComparison.OrdinalIgnoreCase))
					{
						filePath = connectionString.Substring("DATABASE=".Length);
					}

					return new FileGeodatabaseConnectionPath(new Uri(filePath, UriKind.Absolute));

				case WorkspaceFactory.SDE:
					DatabaseConnectionProperties connectionProperties =
						GetConnectionProperties(connectionString);

					return connectionProperties;

				case WorkspaceFactory.Shapefile:
					return new FileSystemConnectionPath(
						new Uri(connectionString, UriKind.Absolute),
						FileSystemDatastoreType.Shapefile);

				// TODO: SQLite, others?

				default:
					throw new ArgumentOutOfRangeException(nameof(factory), factory,
					                                      $"Unsupported workspace factory: {factory}");
			}
		}

		public static bool IsSameDatastore([CanBeNull] Datastore datastore1,
		                                   [CanBeNull] Datastore datastore2,
		                                   DatastoreComparison comparison =
			                                   DatastoreComparison.Exact)
		{
			// Comparison in case of null:
			if (datastore1 == null && datastore2 == null)
			{
				return true;
			}

			if (datastore1 == null || datastore2 == null)
			{
				return false;
			}

			if (comparison == DatastoreComparison.ReferenceEquals)
			{
				return ReferenceEquals(datastore1, datastore2) ||
				       Equals(datastore1.Handle, datastore2.Handle);
			}

			DatastoreName datastoreName1 = new DatastoreName(datastore1.GetConnector());
			DatastoreName datastoreName2 = new DatastoreName(datastore2.GetConnector());

			return datastoreName1.Equals(datastoreName2, comparison);
		}

		public static string GetCatalogPath([NotNull] ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			Uri uri = geodatabase.GetPath();

			// NOTE: AbsolutePath messes up blanks!
			return uri.LocalPath;
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
				else if (lastItem.Contains('$'))
				{
					// Very legacy. E.g. oracle$TOPGIST
					string server = builder["server"];
					if (! string.IsNullOrEmpty(server))
					{
						instance = server;
					}
					else
					{
						instance = lastItem.Split('$')[^1];
					}
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
		/// <param name="datastore">The workspace</param>
		[NotNull]
		public static string GetDatastoreDisplayText(Datastore datastore)
		{
			Connector connector = datastore?.GetConnector();

			return GetDatastoreDisplayText(connector);
		}

		/// <summary>
		/// Gets a displayable text describing a given datastore connector.
		/// </summary>
		/// <param name="connector">The connector</param>
		public static string GetDatastoreDisplayText([CanBeNull] Connector connector)
		{
			// TODO: parameter "detailed" to include full info (but no passwords)

			const string nullPathText = "<undefined path>";
			try
			{
				switch (connector)
				{
					case null:
						return "<null>";

					case DatabaseConnectionFile dbConnection:
						return $"SDE connection {dbConnection.Path?.LocalPath ?? nullPathText}";

					case DatabaseConnectionProperties dbConnectionProps:
						return GetConnectionDisplayText(dbConnectionProps);

					case FileGeodatabaseConnectionPath fileGdbConnection:
						return $"File Geodatabase {fileGdbConnection.Path.LocalPath}";

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
						return $"Unknown connection of type {connector.GetType().Name}";
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Error getting connector display text: {e.Message}", e);
				return $"<error: {e.Message}>";
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

		public static WorkspaceFactory GetWorkspaceFactory([NotNull] Connector connector)
		{
			WorkspaceFactory result;

			switch (connector)
			{
#if ARCGISPRO_GREATER_3_2
				case BimFileConnectionProperties:
					result = WorkspaceFactory.BIMFile;
					break;
#endif
				case DatabaseConnectionFile:
				case DatabaseConnectionProperties:
					result = WorkspaceFactory.SDE;
					break;
				case FileGeodatabaseConnectionPath:
					result = WorkspaceFactory.FileGDB;
					break;
				case FileSystemConnectionPath:
					result = WorkspaceFactory.Shapefile;
					break;
				case KnowledgeGraphConnectionProperties:
					result = WorkspaceFactory.KnowledgeGraph;
					break;
				case MemoryConnectionProperties:
					result = WorkspaceFactory.InMemoryDB;
					break;
				case MobileGeodatabaseConnectionPath:
				case SQLiteConnectionPath:
					result = WorkspaceFactory.SQLite;
					break;
				case PluginDatasourceConnectionPath:
					result = WorkspaceFactory.Custom;
					break;
				case RealtimeServiceConnectionProperties:
					result = WorkspaceFactory.StreamService;
					break;
				case ServiceConnectionProperties:
					result = WorkspaceFactory.FeatureService;
					break;

				default:
					throw new NotImplementedException(
						$"connector {connector.GetType()} is not implemented");
			}

			return result;
		}
	}
}

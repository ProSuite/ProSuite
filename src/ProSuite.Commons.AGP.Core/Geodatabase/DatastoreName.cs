using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Data.Realtime;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	/// <summary>
	/// Name object that represents a thread-safe moniker for a datastore. Equality comparisons are
	/// based on the connection properties.
	/// The datastore can be re-opened from the name object.
	/// Aspirationally, this could be turned into a proper serializable memento
	/// </summary>
	public class DatastoreName : IEquatable<DatastoreName>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Connector _connector;

		public DatastoreName([NotNull] Datastore datastore)
		{
			Assert.ArgumentNotNull(datastore, nameof(datastore));

			// NOTE: Connectors have no thread affinity.
			_connector = datastore.GetConnector();
		}

		public DatastoreName([NotNull] Connector connector)
		{
			Assert.ArgumentNotNull(connector, nameof(connector));

			_connector = connector;
		}

		public Datastore Open()
		{
			try
			{
				switch (_connector)
				{
					case DatabaseConnectionFile dbConnection:
						return new ArcGIS.Core.Data.Geodatabase(dbConnection);

					case DatabaseConnectionProperties dbConnectionProps:
						return new ArcGIS.Core.Data.Geodatabase(dbConnectionProps);

					case FileGeodatabaseConnectionPath fileGdbConnection:
						return new ArcGIS.Core.Data.Geodatabase(fileGdbConnection);

					case FileSystemConnectionPath fileSystemConnection:
						return new FileSystemDatastore(fileSystemConnection);

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
							$"Unsupported workspace type: {_connector?.GetType()}");
				}
			}
			catch (Exception e)
			{
				_msg.Debug(
					$"Error opening datastore {WorkspaceUtils.GetDatastoreDisplayText(_connector)}",
					e);
				throw;
			}
		}

		public string GetDisplayText()
		{
			return _connector != null
				       ? WorkspaceUtils.GetDatastoreDisplayText(_connector)
				       : "<undefined connection>";
		}

		#region Equality members

		public bool Equals(DatastoreName other)
		{
			if (other is null) return false;
			if (_connector.GetType() != other._connector.GetType()) return false;

			switch (_connector)
			{
				case DatabaseConnectionFile dbConnection:
					var otherDbConnection = (DatabaseConnectionFile) other._connector;
					return Equals(dbConnection.Path, otherDbConnection.Path);

				case DatabaseConnectionProperties dbConnectionProps:
					return AreEqual(dbConnectionProps,
					                (DatabaseConnectionProperties) other._connector);

				case FileGeodatabaseConnectionPath fileGdbConnection:
					var otherFgdbConnection = (FileGeodatabaseConnectionPath) other._connector;
					return Equals(fileGdbConnection.Path, otherFgdbConnection.Path);

				case FileSystemConnectionPath fileSystemConnection:
					var otherFsConnection = (FileSystemConnectionPath) other._connector;
					return fileSystemConnection.Type == otherFsConnection.Type &&
					       Equals(fileSystemConnection.Path, otherFsConnection.Path);

				case MemoryConnectionProperties memoryConnectionProperties:
					var otherMemoryConnection = (MemoryConnectionProperties) other._connector;
					return Equals(memoryConnectionProperties.Name, otherMemoryConnection.Name);

				case MobileGeodatabaseConnectionPath mobileConnectionProperties:
					var otherMobileConnection = (MobileGeodatabaseConnectionPath) other._connector;
					return Equals(mobileConnectionProperties.Path, otherMobileConnection.Path);

				case PluginDatasourceConnectionPath pluginConnectionPath:
					var otherPluginConnection = (PluginDatasourceConnectionPath) other._connector;
					return Equals(pluginConnectionPath.PluginIdentifier,
					              otherPluginConnection.PluginIdentifier) &&
					       Equals(pluginConnectionPath.DatasourcePath,
					              otherPluginConnection.DatasourcePath);

				case RealtimeServiceConnectionProperties serviceConnection:
					var otherRealtimeServiceConnection =
						(RealtimeServiceConnectionProperties) other._connector;

					return AreEqual(serviceConnection, otherRealtimeServiceConnection);

				case ServiceConnectionProperties serviceConnection:
					var otherServiceConnection = (ServiceConnectionProperties) other._connector;
					return AreEqual(serviceConnection, otherServiceConnection);

				case SQLiteConnectionPath sqLiteConnection:
					var otherSqliteConnection = (SQLiteConnectionPath) other._connector;
					return Equals(sqLiteConnection.Path, otherSqliteConnection.Path);

				default:
					return false;
			}
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((DatastoreName) obj);
		}

		public override int GetHashCode()
		{
			switch (_connector)
			{
				case DatabaseConnectionFile dbConnection:
					return dbConnection.Path?.GetHashCode() ?? 0;

				case DatabaseConnectionProperties dbConnectionProps:
					return GetHashCode(dbConnectionProps);

				case FileGeodatabaseConnectionPath fileGdbConnection:
					return fileGdbConnection.Path?.GetHashCode() ?? 0;

				case FileSystemConnectionPath fileSystemConnection:
					return HashCode.Combine((int) fileSystemConnection.Type,
					                        fileSystemConnection.Path);

				case MemoryConnectionProperties memoryConnectionProperties:
					return memoryConnectionProperties.Name?.GetHashCode() ?? 0;

				case MobileGeodatabaseConnectionPath mobileConnectionProperties:
					return mobileConnectionProperties.Path?.GetHashCode() ?? 0;

				case PluginDatasourceConnectionPath pluginConnectionPath:
					return HashCode.Combine(pluginConnectionPath.DatasourcePath,
					                        pluginConnectionPath.PluginIdentifier);

				case RealtimeServiceConnectionProperties serviceConnection:
					return GetHashCode(serviceConnection);

				case ServiceConnectionProperties serviceConnection:
					return GetHashCode(serviceConnection);

				case SQLiteConnectionPath sqLiteConnection:
					return sqLiteConnection.Path?.GetHashCode() ?? 0;

				default:
					return 0;
			}
		}

		#endregion

		#region Overrides of Object

		public override string ToString()
		{
			return GetDisplayText();
		}

		#endregion

		private static int GetHashCode(DatabaseConnectionProperties dbConnectionProps)
		{
			var hashCode = new HashCode();
			hashCode.Add(dbConnectionProps.Instance);
			hashCode.Add(dbConnectionProps.DBMS);
			hashCode.Add(dbConnectionProps.AuthenticationMode);
			hashCode.Add(dbConnectionProps.User);
			hashCode.Add(dbConnectionProps.Password);
			hashCode.Add(dbConnectionProps.Database);
			hashCode.Add(dbConnectionProps.ProjectInstance);
			hashCode.Add(dbConnectionProps.Version);
			hashCode.Add(dbConnectionProps.Branch);

			return hashCode.ToHashCode();
		}

		private static int GetHashCode(ServiceConnectionProperties serviceConnection)
		{
			var hashCode = new HashCode();
			hashCode.Add(serviceConnection.URL);
			hashCode.Add(serviceConnection.User);
			hashCode.Add(serviceConnection.Password);

			return hashCode.ToHashCode();
		}

		private static int GetHashCode(RealtimeServiceConnectionProperties serviceConnection)
		{
			var hashCode = new HashCode();
			hashCode.Add(serviceConnection.URL);
			hashCode.Add(serviceConnection.User);
			hashCode.Add(serviceConnection.Password);
			hashCode.Add(serviceConnection.ObserverID);
			hashCode.Add(serviceConnection.Type);
			return hashCode.ToHashCode();
		}

		private static bool AreEqual([NotNull] DatabaseConnectionProperties a,
		                             [CanBeNull] DatabaseConnectionProperties b)
		{
			if (b == null) return false;

			return Equals(a.Instance, b.Instance) &&
			       a.DBMS == b.DBMS &&
			       Equals(a.Database, b.Database) &&
			       a.AuthenticationMode == b.AuthenticationMode &&
			       Equals(a.User, b.User) &&
			       Equals(a.Password, b.Password) &&
			       Equals(a.ProjectInstance, b.ProjectInstance) &&
			       Equals(a.Version, b.Version) &&
			       Equals(a.Branch, b.Branch);
		}

		private static bool AreEqual([NotNull] RealtimeServiceConnectionProperties a,
		                             [CanBeNull] RealtimeServiceConnectionProperties b)
		{
			if (b == null) return false;

			return Equals(a.URL, b.URL) &&
			       Equals(a.User, b.User) &&
			       Equals(a.Password, b.Password) &&
			       a.Type == b.Type &&
			       Equals(a.ObserverID, b.ObserverID);
		}

		private static bool AreEqual([NotNull] ServiceConnectionProperties a,
		                             [CanBeNull] ServiceConnectionProperties b)
		{
			if (b == null) return false;

			return Equals(a.URL, b.URL) &&
			       Equals(a.User, b.User) &&
			       Equals(a.Password, b.Password) &&
			       Equals(a.Version, b.Version);
		}
	}
}

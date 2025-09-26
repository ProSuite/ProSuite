using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Data.Realtime;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

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

		/// <summary>
		/// Opens the associated datastore. This method must be run on the MCT.
		/// </summary>
		/// <remarks>
		/// NOTE: This can lead to different instances of the same workspace
		///       because opening a new Geodatabase with the Connector of an existing Geodatabase
		///       can in some cases result in a different instance!
		/// </remarks>
		[NotNull]
		public Datastore Open()
		{
			return WorkspaceUtils.OpenDatastore(_connector);
		}

		public bool References(Datastore datastore)
		{
			return Equals(datastore.GetConnector());
		}

		public string GetDisplayText()
		{
			return _connector != null
				       ? WorkspaceUtils.GetDatastoreDisplayText(_connector)
				       : "<undefined connection>";
		}

		public IEnumerable<KeyValuePair<string, string>> ConnectionProperties
		{
			get
			{
				if (_connector is DatabaseConnectionProperties properties)
				{
					if (! string.IsNullOrEmpty(properties.Instance))
						yield return new KeyValuePair<string, string>(
							"INSTANCE", properties.Instance);

					if (! string.IsNullOrEmpty(properties.DBMS.ToString()))
						yield return new KeyValuePair<string, string>(
							"DBMS", properties.DBMS.ToString());

					if (! string.IsNullOrEmpty(properties.AuthenticationMode.ToString()))
						yield return new KeyValuePair<string, string>(
							"AUTHENTICATION_MODE", properties.AuthenticationMode.ToString());

					if (! string.IsNullOrEmpty(properties.User))
						yield return new KeyValuePair<string, string>("USER", properties.User);

					if (! string.IsNullOrEmpty(properties.Password))
						yield return new KeyValuePair<string, string>(
							"PASSWORD", properties.Password);

					if (! string.IsNullOrEmpty(properties.Database))
						yield return new KeyValuePair<string, string>(
							"DATABASE", properties.Database);

					if (! string.IsNullOrEmpty(properties.ProjectInstance))
						yield return new KeyValuePair<string, string>(
							"PROJECT_INSTANCE", properties.ProjectInstance);

					if (! string.IsNullOrEmpty(properties.Version))
						yield return
							new KeyValuePair<string, string>("VERSION", properties.Version);

					if (! string.IsNullOrEmpty(properties.Branch))
						yield return new KeyValuePair<string, string>("BRANCH", properties.Branch);
				}

				if (_connector is DatabaseConnectionFile sdeFile)
				{
					yield return new KeyValuePair<string, string>("PATH", sdeFile.Path.ToString());
				}

				if (_connector is FileGeodatabaseConnectionPath fgdbPath)
				{
					yield return new KeyValuePair<string, string>("PATH", fgdbPath.Path.ToString());
				}

				if (_connector is FileSystemConnectionPath fsPath)
				{
					yield return new KeyValuePair<string, string>("TYPE", fsPath.Type.ToString());
					yield return new KeyValuePair<string, string>("PATH", fsPath.Path.ToString());
				}

				if (_connector is MemoryConnectionProperties memoryConnection)
				{
					yield return new KeyValuePair<string, string>("NAME", memoryConnection.Name);
				}

				if (_connector is MobileGeodatabaseConnectionPath mobilePath)
				{
					yield return new KeyValuePair<string, string>(
						"PATH", mobilePath.Path.ToString());
				}

				if (_connector is PluginDatasourceConnectionPath pluginPath)
				{
					yield return new KeyValuePair<string, string>(
						"PLUGIN_IDENTIFIER", pluginPath.PluginIdentifier);
					yield return new KeyValuePair<string, string>(
						"DATASOURCE_PATH", pluginPath.DatasourcePath.ToString());
				}

				if (_connector is RealtimeServiceConnectionProperties serviceConnection)
				{
					yield return new KeyValuePair<string, string>(
						"URL", serviceConnection.URL.AbsolutePath);
				}
			}
		}

		public string ConnectionString
		{
			get
			{
				// TODO: Unit test, verify this logic. Currently just used for comparisons.
				return StringUtils.Concatenate(
					ConnectionProperties.Select(kvp => $"{kvp.Key}={kvp.Value}"), ";");
			}
		}

		#region Equality members

		public bool Equals(DatastoreName other)
		{
			if (other is null) return false;

			Connector otherConnector = other._connector;

			return Equals(otherConnector);
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

		public bool Equals(DatastoreName other, DatastoreComparison comparison)
		{
			if (other is null) return false;

			Connector otherConnector = other._connector;

			return Equals(otherConnector);
		}

		private bool Equals(Connector otherConnector,
		                    DatastoreComparison comparison = DatastoreComparison.Exact)
		{
			_msg.VerboseDebug(() => $"Comparing datastore connectors {this} with {otherConnector}");
			if (_connector.GetType() != otherConnector.GetType()) return false;

			switch (_connector)
			{
				case DatabaseConnectionFile dbConnection:
					var otherDbConnection = (DatabaseConnectionFile) otherConnector;
					return Equals(dbConnection.Path, otherDbConnection.Path);

				case DatabaseConnectionProperties dbConnectionProps:
					return AreEqual(dbConnectionProps,
					                (DatabaseConnectionProperties) otherConnector, comparison);

				case FileGeodatabaseConnectionPath fileGdbConnection:
					var otherFgdbConnection = (FileGeodatabaseConnectionPath) otherConnector;
					return Equals(fileGdbConnection.Path, otherFgdbConnection.Path);

				case FileSystemConnectionPath fileSystemConnection:
					var otherFsConnection = (FileSystemConnectionPath) otherConnector;
					return fileSystemConnection.Type == otherFsConnection.Type &&
					       Equals(fileSystemConnection.Path, otherFsConnection.Path);

				case MemoryConnectionProperties memoryConnectionProperties:
					var otherMemoryConnection = (MemoryConnectionProperties) otherConnector;
					return Equals(memoryConnectionProperties.Name, otherMemoryConnection.Name);

				case MobileGeodatabaseConnectionPath mobileConnectionProperties:
					var otherMobileConnection = (MobileGeodatabaseConnectionPath) otherConnector;
					return Equals(mobileConnectionProperties.Path, otherMobileConnection.Path);

				case PluginDatasourceConnectionPath pluginConnectionPath:
					var otherPluginConnection = (PluginDatasourceConnectionPath) otherConnector;
					return Equals(pluginConnectionPath.PluginIdentifier,
					              otherPluginConnection.PluginIdentifier) &&
					       Equals(pluginConnectionPath.DatasourcePath,
					              otherPluginConnection.DatasourcePath);

				case RealtimeServiceConnectionProperties serviceConnection:
					var otherRealtimeServiceConnection =
						(RealtimeServiceConnectionProperties) otherConnector;

					return AreEqual(serviceConnection, otherRealtimeServiceConnection);

				case ServiceConnectionProperties serviceConnection:
					var otherServiceConnection = (ServiceConnectionProperties) otherConnector;
					return AreEqual(serviceConnection, otherServiceConnection);

				case SQLiteConnectionPath sqLiteConnection:
					var otherSqliteConnection = (SQLiteConnectionPath) otherConnector;
					return Equals(sqLiteConnection.Path, otherSqliteConnection.Path);

				default:
					return false;
			}
		}

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
		                             [CanBeNull] DatabaseConnectionProperties b,
		                             DatastoreComparison comparison)
		{
			if (b == null) return false;

			bool basicEqual = Equals(a.Instance, b.Instance) &&
			                  a.DBMS == b.DBMS &&
			                  Equals(a.Database, b.Database) &&
			                  a.AuthenticationMode == b.AuthenticationMode &&
			                  Equals(a.ProjectInstance, b.ProjectInstance);

			if (comparison == DatastoreComparison.AnyUserAnyVersion)
			{
				return basicEqual;
			}

			if (comparison == DatastoreComparison.AnyUserSameVersion)
			{
				return Equals(a.Version, b.Version) &&
				       Equals(a.Branch, b.Branch);
			}

			return Equals(a.User, b.User) &&
			       Equals(a.Password, b.Password);
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

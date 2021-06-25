using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	// todo daro: check correct handle / instantiation of Uri
	public struct GdbWorkspaceIdentity : IEquatable<GdbWorkspaceIdentity>
	{
		private readonly string _instance;
		private readonly string _version;
		private readonly string _user;
		private readonly EnterpriseDatabaseType _dbms;

		public GdbWorkspaceIdentity([NotNull] Datastore datastore) :
			this(datastore.GetConnector(), datastore.GetConnectionString()) { }

		public GdbWorkspaceIdentity([NotNull] Connector connector, string connectionString)
		{
			Assert.ArgumentNotNull(connector, nameof(connector));

			_instance = null;
			_version = null;
			_user = null;
			_dbms = EnterpriseDatabaseType.Unknown;
			ConnectionString = string.Empty;

			switch (connector)
			{
				case DatabaseConnectionProperties connectionProperties:
					_instance = connectionProperties.Instance;
					_version = connectionProperties.Version;
					_user = connectionProperties.User;
					_dbms = connectionProperties.DBMS;
					ConnectionString = connectionString;
					WorkspaceFactory = WorkspaceFactory.SDE;
					break;
				case FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath:
					// connectionString is "DATABASE=C:\\git\\KtLU.Dabank\\data\\Testdaten\\dabank_test_data\\Default.gdb"
					ConnectionString = fileGeodatabaseConnectionPath.Path.AbsolutePath;
					WorkspaceFactory = WorkspaceFactory.FileGDB;
					break;
				default:
					throw new NotImplementedException(
						$"connector {connector.GetType()} is not implemented");
			}
		}

		[NotNull]
		public string ConnectionString { get; }

		public WorkspaceFactory WorkspaceFactory { get; }

		[CanBeNull]
		public T CreateConnector<T>() where T : Connector
		{
			Type type = typeof(T);
			if (type == typeof(DatabaseConnectionProperties))
			{
				return new DatabaseConnectionProperties(_dbms) as T;
			}

			if (type == typeof(FileGeodatabaseConnectionPath))
			{
				return new FileGeodatabaseConnectionPath(
					       new Uri(ConnectionString, UriKind.Absolute)) as T;
			}

			return null;
		}

		public Geodatabase OpenGeodatabase()
		{
			if (string.IsNullOrEmpty(ConnectionString))
			{
				return new Geodatabase(
					new DatabaseConnectionProperties(_dbms)
					{
						Instance = _instance, Version = _version, User = _user
					});
			}

			return new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(ConnectionString, UriKind.Absolute)));
		}

		public override string ToString()
		{
			return $"instance={_instance} version={_version} user={_user}, path={ConnectionString}";
		}

		#region IEquatable<GdbRowIdentity> implementation

		public bool Equals(GdbWorkspaceIdentity other)
		{
			return string.Equals(_instance, other._instance) &&
			       string.Equals(_version, other._version) &&
			       string.Equals(_user, other._user) &&
			       Equals(ConnectionString, other.ConnectionString);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is GdbWorkspaceIdentity reference && Equals(reference);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = _instance != null ? _instance.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (_version != null ? _version.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (_user != null ? _user.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ConnectionString.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}

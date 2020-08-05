using System;
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
		private readonly string _path;

		public GdbWorkspaceIdentity([NotNull] Datastore datastore) :
			this(datastore.GetConnector()) { }

		public GdbWorkspaceIdentity([NotNull] Connector connector)
		{
			Assert.ArgumentNotNull(connector, nameof(connector));

			_instance = null;
			_version = null;
			_user = null;
			_path = null;
			_dbms = EnterpriseDatabaseType.Unknown;

			switch (connector)
			{
				case DatabaseConnectionProperties connectionProperties:
					_instance = connectionProperties.Instance;
					_version = connectionProperties.Version;
					_user = connectionProperties.User;
					_dbms = connectionProperties.DBMS;
					break;
				case FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath:
					_path = fileGeodatabaseConnectionPath.Path.AbsolutePath;
					break;
				default:
					throw new NotImplementedException(
						$"connector {connector.GetType()} is not implemented");
			}
		}

		[CanBeNull]
		public T CreateConnector<T>() where T: Connector
		{
			Type type = typeof(T);
			if (type == typeof(DatabaseConnectionProperties))
			{
				return new DatabaseConnectionProperties(_dbms) as T;
			}

			if (type == typeof(FileGeodatabaseConnectionPath))
			{
				return new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)) as T;
			}
			return null;
		}

		public Geodatabase OpenGeodatabase()
		{
			if (string.IsNullOrEmpty(_path))
			{
				return new Geodatabase(
					new DatabaseConnectionProperties(_dbms)
					{
						Instance = _instance, Version = _version, User = _user
					});
			}

			return new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)));
		}

		public override string ToString()
		{
			return $"instance={_instance} version={_version} user={_user}, path={_path}";
		}

		#region IEquatable<GdbRowIdentity> implementation

		public bool Equals(GdbWorkspaceIdentity other)
		{
			return string.Equals(_instance, other._instance) &&
			       string.Equals(_version, other._version) &&
			       string.Equals(_user, other._user) && Equals(_path, other._path);
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
				hashCode = (hashCode * 397) ^ (_path != null ? _path.GetHashCode() : 0);
				return hashCode;
			}
		}

		#endregion
	}
}

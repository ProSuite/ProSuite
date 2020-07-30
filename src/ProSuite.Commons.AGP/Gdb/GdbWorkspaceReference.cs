using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	// todo daro: check correct handle / instantiation of Uri
	public struct GdbWorkspaceReference : IEquatable<GdbWorkspaceReference>
	{
		private readonly string _instance;
		private readonly string _version;
		private readonly string _user;
		private readonly string _path;

		public GdbWorkspaceReference([NotNull] Datastore datastore) :
			this(datastore.GetConnector()) { }

		public GdbWorkspaceReference([NotNull] Connector connector)
		{
			Assert.ArgumentNotNull(connector, nameof(connector));

			_instance = null;
			_version = null;
			_user = null;
			_path = null;

			switch (connector)
			{
				case DatabaseConnectionProperties connectionProperties:
					_instance = connectionProperties.Instance;
					_version = connectionProperties.Version;
					_user = connectionProperties.User;
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
				return new DatabaseConnectionProperties(EnterpriseDatabaseType.Unknown) as T;
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
					new DatabaseConnectionProperties(EnterpriseDatabaseType.Unknown)
					{
						Instance = _instance, Version = _version, User = _user
					});
			}

			return new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute)));
		}

		#region IEquatable<GdbRowReference> implementation

		public bool Equals(GdbWorkspaceReference other)
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

			return obj is GdbWorkspaceReference reference && Equals(reference);
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

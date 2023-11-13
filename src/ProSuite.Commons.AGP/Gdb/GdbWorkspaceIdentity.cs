using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	// todo daro: check correct handle / instantiation of Uri
	public struct GdbWorkspaceIdentity : IEquatable<GdbWorkspaceIdentity>
	{
		[NotNull] private readonly DatastoreName _datastoreName;

		private readonly string _instance;
		private readonly string _version;
		private readonly string _user;

		public GdbWorkspaceIdentity([NotNull] Datastore datastore) :
			this(datastore.GetConnector(), datastore.GetConnectionString()) { }

		public GdbWorkspaceIdentity([NotNull] Connector connector, string connectionString)
		{
			Assert.ArgumentNotNull(connector, nameof(connector));

			_datastoreName = new DatastoreName(connector);

			_instance = null;
			_version = null;
			_user = null;
			ConnectionString = string.Empty;

			switch (connector)
			{
				case DatabaseConnectionProperties connectionProperties:
					_instance = connectionProperties.Instance;
					_version = connectionProperties.Version;
					_user = connectionProperties.User;
					ConnectionString = connectionString;
					WorkspaceFactory = WorkspaceFactory.SDE;
					break;
				case FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath:
					// connectionString is "DATABASE=C:\\git\\KtLU.Dabank\\data\\Testdaten\\dabank_test_data\\Default.gdb"
					ConnectionString = fileGeodatabaseConnectionPath.Path.ToString();

					WorkspaceFactory = WorkspaceFactory.FileGDB;
					break;
				case FileSystemConnectionPath fileSystemConnection:
					ConnectionString = fileSystemConnection.Path.ToString();
					WorkspaceFactory = WorkspaceFactory.Shapefile;
					break;
				default:
					throw new NotImplementedException(
						$"connector {connector.GetType()} is not implemented");
			}
		}

		[NotNull]
		public string ConnectionString { get; }

		public WorkspaceFactory WorkspaceFactory { get; }

		// TODO: Currently only used from un-used classes and unit test. Remove?
		public Geodatabase OpenGeodatabase()
		{
			return (Geodatabase) OpenDatastore();
		}

		/// <summary>
		/// Opens the associated datastore
		/// </summary>
		/// <returns></returns>
		public Datastore OpenDatastore()
		{
			return _datastoreName.Open();
		}

		public override string ToString()
		{
			return _datastoreName.GetDisplayText();
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

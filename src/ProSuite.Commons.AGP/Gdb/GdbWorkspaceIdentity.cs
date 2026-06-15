using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb;

public readonly struct GdbWorkspaceIdentity : IEquatable<GdbWorkspaceIdentity>,
                                              IComparable<GdbWorkspaceIdentity>,
                                              IDatastoreReference
{
	[NotNull] private readonly DatastoreName _datastoreName;

	public GdbWorkspaceIdentity([NotNull] Datastore datastore) :
		this(datastore.GetConnector(), datastore.GetConnectionString()) { }

	// TODO: Once we can re-create a valid connector from the connectionString, add overload just using connection string.
	//       Missing functionality: Creating DatabaseConnectionProperties from connection string containing
	//       an encrypted password (GOTOP-224).
	public GdbWorkspaceIdentity([NotNull] Connector connector, string connectionString)
	{
		Assert.ArgumentNotNull(connector, nameof(connector));

		_datastoreName = new DatastoreName(connector);

		ConnectionString = string.Empty;

		switch (connector)
		{
			case DatabaseConnectionProperties:
				ConnectionString = connectionString;
				WorkspaceFactory = WorkspaceFactory.SDE;
				break;
			case FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath:
				ConnectionString = fileGeodatabaseConnectionPath.Path.ToString();
				WorkspaceFactory = WorkspaceFactory.FileGDB;
				break;
			case FileSystemConnectionPath fileSystemConnection:
				ConnectionString = fileSystemConnection.Path.ToString();
				WorkspaceFactory = WorkspaceFactory.Shapefile;
				break;
			case MobileGeodatabaseConnectionPath mobileGeodatabaseConnectionPath:
				ConnectionString = mobileGeodatabaseConnectionPath.Path.ToString();
				WorkspaceFactory = WorkspaceFactory.SQLite;
				break;
			case PluginDatasourceConnectionPath pluginDatasourceConnectionPath:
				ConnectionString = pluginDatasourceConnectionPath.DatasourcePath.ToString();
				WorkspaceFactory = WorkspaceFactory.Custom;
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

	[NotNull]
	public Datastore OpenDatastore()
	{
		return _datastoreName.Open();
	}

	public bool References(Datastore datastore)
	{
		return _datastoreName.References(datastore);
	}

	public bool References(DatastoreName datastoreName)
	{
		return _datastoreName.Equals(datastoreName);
	}

	public override string ToString()
	{
		return _datastoreName.GetDisplayText();
	}

	#region IEquatable<GdbWorkspaceIdentity> implementation

	public bool Equals(GdbWorkspaceIdentity other)
	{
		return _datastoreName.Equals(other._datastoreName);
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
		return _datastoreName.GetHashCode();
	}

	#endregion

	#region IComparable<GdbWorkspaceIdentity> implementation

	public int CompareTo(GdbWorkspaceIdentity other)
	{
		return string.Compare(ConnectionString, other.ConnectionString,
		                      StringComparison.OrdinalIgnoreCase);
	}

	#endregion
}

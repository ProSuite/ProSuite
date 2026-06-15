using System;
using System.Collections.Generic;
using System.IO;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using esriWorkspaceType = ProSuite.GIS.Geodatabase.API.esriWorkspaceType;

namespace ProSuite.GIS.Geodatabase.AGP;

/// <summary>
/// Minimal IWorkspace implementation for non-geodatabase data stores
/// such as shapefiles (FileSystemDatastore) and plug-in datasources (PluginDatastore).
/// </summary>
public class BasicWorkspace : IWorkspace
{
	// Property caching for non CIM-thread access:
	private string _pathName;
	private esriWorkspaceType? _workspaceType;
	private IWorkspaceName _workspaceName;

	[NotNull]
	public Datastore Datastore { get; }

	public BasicWorkspace([NotNull] Datastore datastore,
	                      bool cacheProperties = false)
	{
		Datastore = datastore;

		if (cacheProperties)
		{
			CacheProperties();
		}
	}

	#region Implementation of IWorkspace

	public IEnumerable<IDataset> get_Datasets(esriDatasetType datasetType)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<IName> get_DatasetNames(esriDatasetType datasetType)
	{
		throw new NotImplementedException();
	}

	public string PathName
	{
		get
		{
			if (_pathName != null)
			{
				return _pathName;
			}

			Connector connector = Datastore.GetConnector();

			_pathName = GetPath(connector);

			return _pathName;
		}
	}

	public esriWorkspaceType Type
	{
		get
		{
			if (_workspaceType.HasValue)
			{
				return _workspaceType.Value;
			}

			Connector connector = Datastore.GetConnector();
			_workspaceType = connector is FileSystemConnectionPath
				                 ? esriWorkspaceType.esriFileSystemWorkspace
				                 : esriWorkspaceType.esriLocalDatabaseWorkspace;

			return _workspaceType.Value;
		}
	}

	public bool IsDirectory()
	{
		Connector connector = Datastore.GetConnector();
		return connector is FileSystemConnectionPath;
	}

	public bool Exists()
	{
		string path = PathName;
		return ! string.IsNullOrEmpty(path) && Directory.Exists(path);
	}

	public void ExecuteSql(string sqlStmt) =>
		throw new NotSupportedException(
			$"{nameof(BasicWorkspace)} does not support SQL execution");

	public esriConnectionDBMS DbmsType => esriConnectionDBMS.esriDBMS_Unknown;

	public IWorkspaceName GetWorkspaceName() => _workspaceName ??= new BasicWorkspaceName(this);

	public bool IsSameDatabase(IWorkspace otherWorkspace)
	{
		if (otherWorkspace is BasicWorkspace other)
		{
			return string.Equals(PathName, other.PathName,
			                     StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	public string Description => WorkspaceUtils.GetDatastoreDisplayText(Datastore);

	public object NativeImplementation => Datastore;

	public void CacheProperties()
	{
		_pathName = PathName;
		_workspaceType = Type;

		_workspaceName = GetWorkspaceName();
	}

	#endregion

	private static string GetPath(Connector connector)
	{
		return connector switch
		{
			FileSystemConnectionPath fsc => fsc.Path.LocalPath,
			PluginDatasourceConnectionPath pdc => pdc.DatasourcePath.LocalPath,
			_ => string.Empty
		};
	}
}

public class BasicWorkspaceName : IWorkspaceName
{
	[NotNull] private readonly BasicWorkspace _workspace;
	private readonly DatastoreName _datastoreName;

	public BasicWorkspaceName([NotNull] BasicWorkspace workspace)
	{
		_workspace = workspace;
		_datastoreName = new DatastoreName(workspace.Datastore);
	}

	public string NameString { get; set; }

	public object Open() => _workspace;

	public string PathName => _workspace.PathName;

	public esriWorkspaceType Type => _workspace.Type;

	public string Category => throw new NotImplementedException();
	public string ConnectionString => _datastoreName.ConnectionString;

	public string WorkspaceFactoryProgID => throw new NotImplementedException();

	public string BrowseName => throw new NotImplementedException();

	public IEnumerable<KeyValuePair<string, string>> ConnectionProperties =>
		_datastoreName.ConnectionProperties;
}

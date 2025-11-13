using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.GIS.Geodatabase.AGP;

public class ArcWorkspace : IFeatureWorkspace
{
	private static readonly Dictionary<long, ArcWorkspace> _workspacesByHandle = new();

	private readonly ConcurrentDictionary<string, ArcRelationshipClass> _relationshipClassesByName =
		new();

	private readonly ConcurrentDictionary<string, ArcTable> _tablesByName = new();

	private List<IRelationshipClass> _allRelationshipClasses;

	// Property caching for non CIM-thread access:
	private string _pathName;
	private esriWorkspaceType? _workspaceType;
	private esriConnectionDBMS? _dbmsType;
	private IWorkspaceName _workspaceName;

	private readonly Dictionary<string, ArcDomain> _domains = new();

	[CanBeNull]
	internal static ArcWorkspace GetByHandle(long handle)
	{
		return _workspacesByHandle.GetValueOrDefault(handle);
	}

	public static ArcWorkspace Create(ArcGIS.Core.Data.Geodatabase geodatabase,
	                                  bool cacheProperties = false)
	{
		if (_workspacesByHandle.TryGetValue(geodatabase.Handle.ToInt64(),
		                                    out ArcWorkspace existing))
		{
			if (cacheProperties)
			{
				existing.CacheProperties();
			}

			return existing;
		}

		return geodatabase.IsVersioningSupported()
			       ? new ArcVersionedWorkspace(geodatabase, cacheProperties)
			       : new ArcWorkspace(geodatabase, cacheProperties);
	}

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public ArcGIS.Core.Data.Geodatabase Geodatabase { get; }

	protected ArcWorkspace([NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
	                       bool cacheProperties = false)
	{
		Geodatabase = geodatabase;

		_workspacesByHandle.TryAdd(Geodatabase.Handle.ToInt64(), this);

		if (cacheProperties)
		{
			CacheProperties();
		}
	}

	private void CacheProperties()
	{
		_pathName = PathName;
		_workspaceType = Type;
		_dbmsType = DbmsType;

		_workspaceName = GetWorkspaceName();
	}

	#region Equality members

	protected bool Equals(ArcWorkspace other)
	{
		return Equals(Geodatabase.Handle, other.Geodatabase.Handle);
	}

	/// <summary>
	/// Determines whether this workspace is the same instance as the provided other workspace.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public override bool Equals(object other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		if (other.GetType() != GetType())
		{
			return false;
		}

		return Equals((ArcWorkspace) other);
	}

	public override int GetHashCode()
	{
		return Geodatabase != null ? Geodatabase.Handle.GetHashCode() : 0;
	}

	#endregion

	#region Implementation of IWorkspace

	//public IPropertySet ConnectionProperties => _aoWorkspace.ConnectionProperties;

	//public IWorkspaceFactory WorkspaceFactory => _aoWorkspace.WorkspaceFactory;

	public IEnumerable<IDataset> get_Datasets(esriDatasetType datasetType)
	{
		switch (datasetType)
		{
			case esriDatasetType.esriDTFeatureClass:
				foreach (FeatureClassDefinition definition in Geodatabase
					         .GetDefinitions<FeatureClassDefinition>())
				{
					yield return Open(definition);
				}

				break;
			case esriDatasetType.esriDTTable:
				foreach (TableDefinition definition in Geodatabase
					         .GetDefinitions<TableDefinition>())
				{
					yield return Open(definition);
				}

				break;
			case esriDatasetType.esriDTRelationshipClass:
				foreach (IDataset dataset in GetAllRelationshipClasses())
				{
					yield return dataset;
				}

				break;

			case esriDatasetType.esriDTAny:
			case esriDatasetType.esriDTContainer:
			case esriDatasetType.esriDTGeo:
			case esriDatasetType.esriDTFeatureDataset:
			case esriDatasetType.esriDTPlanarGraph:
			case esriDatasetType.esriDTGeometricNetwork:
			case esriDatasetType.esriDTTopology:
			case esriDatasetType.esriDTText:
			case esriDatasetType.esriDTRasterDataset:
			case esriDatasetType.esriDTRasterBand:
			case esriDatasetType.esriDTTin:
			case esriDatasetType.esriDTCadDrawing:
			case esriDatasetType.esriDTRasterCatalog:
			case esriDatasetType.esriDTToolbox:
			case esriDatasetType.esriDTTool:
			case esriDatasetType.esriDTNetworkDataset:
			case esriDatasetType.esriDTTerrain:
			case esriDatasetType.esriDTRepresentationClass:
			case esriDatasetType.esriDTCadastralFabric:
			case esriDatasetType.esriDTSchematicDataset:
			case esriDatasetType.esriDTLocator:
			case esriDatasetType.esriDTMap:
			case esriDatasetType.esriDTLayer:
			case esriDatasetType.esriDTStyle:
			case esriDatasetType.esriDTMosaicDataset:
			case esriDatasetType.esriDTLasDataset:

				throw new NotImplementedException();

			default:
				throw new ArgumentOutOfRangeException(nameof(datasetType), datasetType, null);
		}
	}

	private IEnumerable<IRelationshipClass> GetAllRelationshipClasses()
	{
		if (_allRelationshipClasses == null)
		{
			_allRelationshipClasses = new List<IRelationshipClass>();

			foreach (RelationshipClassDefinition definition in Geodatabase
				         .GetDefinitions<RelationshipClassDefinition>())
			{
				TryAddRelClass(definition, _allRelationshipClasses);
			}

			foreach (AttributedRelationshipClassDefinition definition in Geodatabase
				         .GetDefinitions<AttributedRelationshipClassDefinition>())
			{
				TryAddRelClass(definition, _allRelationshipClasses);
			}
		}

		foreach (IRelationshipClass relationshipClass in _allRelationshipClasses)
		{
			yield return relationshipClass;
		}
	}

	private void TryAddRelClass(RelationshipClassDefinition definition,
	                            List<IRelationshipClass> toList)
	{
		try
		{
			IDataset result = Open(definition);

			if (result is IRelationshipClass relationshipClass)
			{
				// Ensure that Origin/Destination can be opened as well, otherwise, skip

				Assert.NotNull(relationshipClass.OriginClass);
				Assert.NotNull(relationshipClass.DestinationClass);

				toList.Add(relationshipClass);
			}
		}
		catch (Exception e)
		{
			_msg.Warn(
				$"Cannot open relationship class {definition.GetName()} or one of its related tables. " +
				$"It will be ignored ({e.Message})", e);

			// TODO: Could this also happen due to missing privileges? In which case assuming
			// it does not exist is correct. Or: Add a placeholder relationship class that throws on use?
		}
	}

	private IDataset Open(Definition definition)
	{
		if (definition is FeatureClassDefinition)
		{
			FeatureClass proTable =
				DatasetUtils.OpenDataset<FeatureClass>(Geodatabase, definition.GetName());
			return ArcGeodatabaseUtils.ToArcTable(proTable);
		}

		if (definition is TableDefinition)
		{
			Table proTable = DatasetUtils.OpenDataset<Table>(Geodatabase, definition.GetName());
			return ArcGeodatabaseUtils.ToArcTable(proTable);
		}

		if (definition is RelationshipClassDefinition)
		{
			RelationshipClass proRelClass =
				DatasetUtils.OpenDataset<RelationshipClass>(Geodatabase, definition.GetName());
			return ArcRelationshipClass.Create(proRelClass);
		}

		throw new ArgumentOutOfRangeException();
	}

	//private IEnumerable<T> GetDatasets<T>() where T : Dataset
	//{
	//	IEnumerable<Definition> definitions = _geodatabase.GetDefinitions<TableDefinition>();

	//	foreach (Definition tableDefinition in definitions)
	//	{
	//		yield return (T) _geodatabase.OpenDataset<Table>(tableDefinition.GetName());
	//	}
	//}

	public IEnumerable<IName> get_DatasetNames(esriDatasetType datasetType)
	{
		switch (datasetType)
		{
			case esriDatasetType.esriDTFeatureClass:
				foreach (FeatureClassDefinition definition in Geodatabase
					         .GetDefinitions<FeatureClassDefinition>())
				{
					yield return new ArcTableDefinitionName(definition, this);
				}

				break;
			case esriDatasetType.esriDTTable:
				foreach (TableDefinition definition in Geodatabase
					         .GetDefinitions<TableDefinition>())
				{
					yield return new ArcTableDefinitionName(definition, this);
				}

				break;
			case esriDatasetType.esriDTRelationshipClass:
				foreach (RelationshipClassDefinition definition in Geodatabase
					         .GetDefinitions<RelationshipClassDefinition>())
				{
					yield return new ArcRelationshipClassDefinitionName(definition, this);
				}

				break;

			case esriDatasetType.esriDTAny:
			case esriDatasetType.esriDTContainer:
			case esriDatasetType.esriDTGeo:
			case esriDatasetType.esriDTFeatureDataset:
			case esriDatasetType.esriDTPlanarGraph:
			case esriDatasetType.esriDTGeometricNetwork:
			case esriDatasetType.esriDTTopology:
			case esriDatasetType.esriDTText:
			case esriDatasetType.esriDTRasterDataset:
			case esriDatasetType.esriDTRasterBand:
			case esriDatasetType.esriDTTin:
			case esriDatasetType.esriDTCadDrawing:
			case esriDatasetType.esriDTRasterCatalog:
			case esriDatasetType.esriDTToolbox:
			case esriDatasetType.esriDTTool:
			case esriDatasetType.esriDTNetworkDataset:
			case esriDatasetType.esriDTTerrain:
			case esriDatasetType.esriDTRepresentationClass:
			case esriDatasetType.esriDTCadastralFabric:
			case esriDatasetType.esriDTSchematicDataset:
			case esriDatasetType.esriDTLocator:
			case esriDatasetType.esriDTMap:
			case esriDatasetType.esriDTLayer:
			case esriDatasetType.esriDTStyle:
			case esriDatasetType.esriDTMosaicDataset:
			case esriDatasetType.esriDTLasDataset:

				throw new NotImplementedException();

			default:
				throw new ArgumentOutOfRangeException(nameof(datasetType), datasetType, null);
		}
	}

	public string PathName => _pathName ??= WorkspaceUtils.GetCatalogPath(Geodatabase);

	public esriWorkspaceType Type =>
		_workspaceType ??= (esriWorkspaceType) Geodatabase.GetGeodatabaseType();

	public bool IsDirectory()
	{
		GeodatabaseType geodatabaseType = Geodatabase.GetGeodatabaseType();

		return geodatabaseType == GeodatabaseType.FileSystem ||
		       geodatabaseType == GeodatabaseType.LocalDatabase;
	}

	public bool Exists()
	{
		Uri uri = Geodatabase.GetPath();

		return Directory.Exists(uri.LocalPath);
	}

	public void ExecuteSql(string sqlStmt)
	{
		DatabaseClient.ExecuteStatement(Geodatabase, sqlStmt);
	}

	public esriConnectionDBMS DbmsType
	{
		get
		{
			if (_dbmsType != null)
			{
				return _dbmsType.Value;
			}

			GeodatabaseType geodatabaseType = Geodatabase.GetGeodatabaseType();

			if (geodatabaseType != GeodatabaseType.RemoteDatabase)
			{
				// TODO: Mobile, FGDB, Shapefiles, etc.
				_dbmsType = esriConnectionDBMS.esriDBMS_Unknown;
			}
			else
			{
				if (Geodatabase.GetConnector() is not DatabaseConnectionProperties connectionProps)
				{
					_dbmsType = esriConnectionDBMS.esriDBMS_Unknown;
				}
				else
				{
					_dbmsType = (esriConnectionDBMS) connectionProps.DBMS;
				}
			}

			return _dbmsType.Value;
		}
	}

	public IWorkspaceName GetWorkspaceName()
	{
		return _workspaceName ??= new ArcWorkspaceName(this);
	}

	public object NativeImplementation => Geodatabase;

	#endregion

	#region Implementation of IFeatureWorkspace

	public ITable OpenTable(string name)
	{
		if (_tablesByName.TryGetValue(name, out ArcTable result))
		{
			return result;
		}

		Table proTable = DatasetUtils.OpenDataset<Table>(Geodatabase, name);
		return ArcGeodatabaseUtils.ToArcTable(proTable);
	}

	public IFeatureClass OpenFeatureClass(string name)
	{
		return (IFeatureClass) OpenTable(name);
	}

	public IEnumerable<IRow> EvaluateQuery(string tables,
	                                       string whereClause = null,
	                                       string subFields = "*",
	                                       bool recycling = false)
	{
		var queryDef = new QueryDef
		               {
			               SubFields = subFields,
			               Tables = tables,
			               WhereClause = whereClause
		               };

		using (RowCursor rowCursor = Geodatabase.Evaluate(queryDef, recycling))
		{
			Table table = null;

			while (rowCursor.MoveNext())
			{
				Row row = rowCursor.Current;

				if (table == null)
				{
					table = row.GetTable();
				}

				yield return ArcGeodatabaseUtils.ToArcRow(row);
			}
		}
	}

	public IRelationshipClass OpenRelationshipClass(string name)
	{
		if (_relationshipClassesByName.TryGetValue(name, out ArcRelationshipClass result))
		{
			return result;
		}

		var proRelClass = DatasetUtils.OpenDataset<RelationshipClass>(Geodatabase, name);
		return new ArcRelationshipClass(proRelClass);
	}

	public ITable OpenRelationshipQuery(
		IRelationshipClass relClass,
		bool joinForward,
		IQueryFilter srcQueryFilter,
		ISelectionSet srcSelectionSet,
		string targetColumns,
		bool doNotPushJoinToDb)
	{
		var aoRelClass = ((ArcRelationshipClass) relClass).ProRelationshipClass;
		var aoFilter = (srcQueryFilter as ArcQueryFilter)?.ProQueryFilter;
		var aoSelectionSet = ((ArcSelectionSet) srcSelectionSet)?.ProSelection;

		// TODO: Move RelationshipClassJoinDefinition from Commons.AO to some other namespace (Commons.GIS?).
		//var joinDef = new RelationshipClassJoinDefinition(relationshipClass, joinType);

		//bool ignoreFirstTable = tablesExpression.Length > 0;
		//tablesExpression.Append(joinDef.GetTableJoinStatement(ignoreFirstTable));

		//QueryDef queryDef = new QueryDef
		//                    {
		//	                    Tables = $@"{layer1Name} JOIN {layer2Name} on {layer1Name}.{layer1JoinColumnName} = {layer2Name}.{layer2JoinColumnName}",
		//	                    SubFields = targetColumns
		//                    };

		//QueryTableDescription queryTableDescription = new QueryTableDescription(queryDef)
		//                                              {
		//	                                              Name = "JoinedPointLine",
		//	                                              PrimaryKeys = geodatabase.GetSQLSyntax().QualifyColumnName(layer1Name, layer1JoinColumnName)
		//                                              };

		//Table queryTable = geodatabase.OpenQueryTable(queryTableDescription);

		throw new NotImplementedException();

		//QueryDescription queryDescription = new QueryDescription()

		//var aoTable = _geodatabase.OpenQueryTable(
		//	aoRelClass, joinForward, aoFilter, aoSelectionSet,
		//	targetColumns, doNotPushJoinToDb);

		//return ArcUtils.ToArcTable(aoTable);
	}

	public IEnumerable<IDomain> Domains()
	{
		IReadOnlyList<Domain> proDomains = Geodatabase.GetDomains();

		bool allDomainsAreCached = proDomains.Count == _domains.Count;

		if (allDomainsAreCached)
		{
			foreach (ArcDomain arcDomain in _domains.Values)
			{
				yield return arcDomain;
			}

			yield break;
		}

		foreach (Domain proDomain in proDomains)
		{
			ArcDomain arcDomain = Assert.NotNull(ArcGeodatabaseUtils.ToArcDomain(proDomain, this));
			_domains.TryAdd(arcDomain.Name, arcDomain);

			yield return arcDomain;
		}
	}

	public IDomain get_DomainByName(string domainName)
	{
		if (_domains.TryGetValue(domainName, out ArcDomain foundDomain))
		{
			return foundDomain;
		}

		// Try find it in the Geodatabase:
		IReadOnlyList<Domain> proDomains = Geodatabase.GetDomains();

		bool allDomainsAreCached = proDomains.Count == _domains.Count;

		if (allDomainsAreCached)
		{
			// If all domains are cached, so we can trust the cache:
			return foundDomain;
		}

		return (from proDomain in proDomains
		        where proDomain.GetName()
		                       .Equals(domainName, StringComparison.InvariantCultureIgnoreCase)
		        select ArcGeodatabaseUtils.ToArcDomain(proDomain, this)).FirstOrDefault();
	}

	public bool IsSameDatabase(IWorkspace otherWorkspace)
	{
		if (otherWorkspace == null)
		{
			return false;
		}

		if (Equals(otherWorkspace))
		{
			// Same instance
			return true;
		}

		if (otherWorkspace is ArcWorkspace otherArcWorkspace)
		{
			// Comparing connection properties is less prone to disconnection issues
			DatastoreName thisGdbName = new DatastoreName(Geodatabase.GetConnector());
			DatastoreName otherGdbName =
				new DatastoreName(otherArcWorkspace.Geodatabase.GetConnector());

			if (thisGdbName.Equals(otherGdbName))
			{
				// Same connection properties
				return true;
			}
		}

		// Both are un-versioned workspaces, compare the path:

		var versionedWorkspace1 = this as IVersionedWorkspace;
		var versionedWorkspace2 = otherWorkspace as IVersionedWorkspace;

		if (versionedWorkspace1 == null && versionedWorkspace2 == null)
		{
			// both are not versioned. Compare file paths
			if (string.IsNullOrEmpty(PathName) ||
			    string.IsNullOrEmpty(otherWorkspace.PathName))
			{
				return false;
			}

			//Determines whether two Uri instances have the same value.
			// e.g. these paths are equal
			// C:\Users\daro\AppData\Local\Temp\GdbWorkspaceTest.gdb
			// file:///C:/Users/daro/AppData/Local/Temp/GdbWorkspaceTest.gdb
			return Equals(new Uri(PathName), new Uri(otherWorkspace.PathName));
		}

		return IsSameDatabase(versionedWorkspace1, versionedWorkspace2);
	}

	public string Description
	{
		get
		{
			string result;

			switch (Type)
			{
				case esriWorkspaceType.esriFileSystemWorkspace:
					result = "File System";
					break;
				case esriWorkspaceType.esriLocalDatabaseWorkspace:
					result = "Local Geodatabase";
					break;
				case esriWorkspaceType.esriRemoteDatabaseWorkspace:
					result = "Remote Geodatabase";
					break;
				default:
					throw new ArgumentOutOfRangeException($"Unknown Workspace Type: {Type}");
			}

			if (Type == esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				result += $" ({DbmsType})";
			}

			return result;
		}
	}

	#endregion

	protected static bool IsSameDatabase([CanBeNull] IVersionedWorkspace versionedWorkspace1,
	                                     [CanBeNull] IVersionedWorkspace versionedWorkspace2)
	{
		if (versionedWorkspace1 == null || versionedWorkspace2 == null)
		{
			// One is versioned, the other not.
			return false;
		}

		// Both are versioned. 

		IVersion defaultVersion1 = versionedWorkspace1.DefaultVersion;
		IVersion defaultVersion2 = versionedWorkspace2.DefaultVersion;

		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.Debug("Compare default version instances");
		}

		if (defaultVersion1.Equals(defaultVersion2))
		{
			// the same default version (only equal if same credentials also)
			return true;
		}

		string defaultVersionName1 = defaultVersion1.VersionName ??
		                             string.Empty;
		string defaultVersionName2 = defaultVersion2.VersionName ??
		                             string.Empty;

		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.DebugFormat("Compare default version names ({0}, {1})",
			                 defaultVersionName1, defaultVersionName2);
		}

		if (! defaultVersionName1.Equals(defaultVersionName2,
		                                 StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		// not the same default version. might still be the same database,
		// but different credentials. Compare creation date of the default version.

		IVersionInfo default1Info = defaultVersion1.VersionInfo;
		IVersionInfo default2Info = defaultVersion2.VersionInfo;

		string creationDate1 = default1Info.Created.ToString();
		string creationDate2 = default2Info.Created.ToString();

		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.DebugFormat("Compare default version creation date: {0},{1}",
			                 creationDate1, creationDate2);
		}

		if (! Equals(creationDate1, creationDate2))
		{
			return false;
		}

		string modifyDate1 = default1Info.Modified.ToString();
		string modifyDate2 = default2Info.Modified.ToString();

		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.DebugFormat("Compare default version last modified date: {0},{1}",
			                 modifyDate1, modifyDate2);
		}

		return Equals(modifyDate1, modifyDate2);
	}

	[CanBeNull]
	internal ArcRelationshipClass GetRelClassByName(string name)
	{
		return _relationshipClassesByName.GetValueOrDefault(name);
	}

	internal void Cache([NotNull] ArcRelationshipClass relationshipClass)
	{
		_relationshipClassesByName.TryAdd(relationshipClass.Name, relationshipClass);
	}

	internal ArcTable GetTableByName(string name)
	{
		return _tablesByName.GetValueOrDefault(name);
	}

	internal void Cache([NotNull] ArcTable table)
	{
		_tablesByName.TryAdd(table.Name, table);
	}

	internal ArcDomain GetDomainByName(string name)
	{
		return _domains.GetValueOrDefault(name);
	}

	internal void Cache([NotNull] ArcDomain domain)
	{
		_domains.TryAdd(domain.Name, domain);
	}
}

public class ArcVersionedWorkspace : ArcWorkspace, IVersion, IVersionedWorkspace
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private VersionManager VersionManager { get; set; }
	private Version Version { get; }

	public ArcVersionedWorkspace(ArcGIS.Core.Data.Geodatabase geodatabase,
	                             bool cacheProperties,
	                             string versionName)
		: this(geodatabase, cacheProperties,
		       geodatabase.GetVersionManager().GetVersion(versionName)) { }

	public ArcVersionedWorkspace(ArcGIS.Core.Data.Geodatabase geodatabase,
	                             bool cacheProperties = false,
	                             Version version = null) : base(geodatabase, cacheProperties)
	{
		Assert.True(geodatabase.IsVersioningSupported(),
		            "This geodatabase cannot be used as versioned workspace.");
		VersionManager = Assert.NotNull(geodatabase.GetVersionManager());

		Version = version ?? VersionManager.GetCurrentVersion();
	}

	#region Equality members

	protected bool Equals(ArcVersionedWorkspace other)
	{
		return base.Equals(other) && Equals(Version.Handle, other.Version.Handle);
	}

	/// <summary>
	/// Determines whether this workspace is the same instance as the provided other workspace.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public override bool Equals(object other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		if (other.GetType() != GetType())
		{
			return false;
		}

		return Equals((ArcVersionedWorkspace) other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(base.GetHashCode(), Version.Handle.GetHashCode());
	}

	#endregion

	#region Implementation of IVersion

	public IVersionInfo VersionInfo => new VersionInfo(Version);

	public string VersionName => Version.GetName();

	public string Description => Version.GetDescription();

	public bool HasParent()
	{
		return Version.GetParent() != null;
	}

	public void Delete()
	{
		throw new NotImplementedException();
	}

	public void RefreshVersion()
	{
		Version.Refresh();
	}

	public IVersion CreateVersion(string newName)
	{
		throw new NotImplementedException();
	}

	#endregion

	#region Implementation of IVersionedWorkspace

	//public IEnumerable<IVersionInfo> Versions =>
	//	VersionManager.GetVersions().Select(v => new VersionInfo(v));

	public IVersion DefaultVersion =>
		new ArcVersionedWorkspace(Geodatabase, false, VersionManager.GetDefaultVersion());

	public IVersion FindVersion(string Name)
	{
		Version proVersion = VersionManager.GetVersion(Name);

		return new ArcVersionedWorkspace(Geodatabase, false, proVersion);
	}

	#endregion
}

public class VersionInfo : IVersionInfo
{
	private readonly Version _version;

	public VersionInfo(Version version)
	{
		_version = version;
	}

	#region Implementation of IVersionInfo

	public string VersionName => _version.GetName();
	public string Description => _version.GetDescription();
	public object Created => _version.GetCreatedDate();
	public object Modified => _version.GetModifiedDate();

	public IVersionInfo Parent =>
		_version.GetParent() != null ? new VersionInfo(_version.GetParent()) : null;

	public IEnumerable<IVersionInfo> Children =>
		_version.GetChildren().Select(c => new VersionInfo(c));

	public bool IsOwner()
	{
		return _version.IsOwner();
	}

	#endregion
}

public class ArcWorkspaceName : IWorkspaceName
{
	private readonly ArcWorkspace _arcWorkspace;
	private readonly DatastoreName _datastoreName;

	public ArcWorkspaceName(ArcWorkspace arcWorkspace)
	{
		_arcWorkspace = arcWorkspace;
		_datastoreName = new DatastoreName(arcWorkspace.Geodatabase);
	}

	#region IName members

	public object Open()
	{
		return _arcWorkspace;
	}

	public string NameString { get; set; }

	#endregion

	#region IWorkspaceName members

	public string PathName
	{
		get => _arcWorkspace.PathName;
	}

	public esriWorkspaceType Type => _arcWorkspace.Type;

	public string Category =>
		throw new NotImplementedException("Implement in derived class");

	public string ConnectionString => _datastoreName.ConnectionString;

	public string WorkspaceFactoryProgID => throw new NotImplementedException();

	public string BrowseName => throw new NotImplementedException();

	public IEnumerable<KeyValuePair<string, string>> ConnectionProperties =>
		_datastoreName.ConnectionProperties;

	#endregion
}

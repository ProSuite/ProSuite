using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcWorkspace : IFeatureWorkspace
	{
		public static ArcWorkspace Create(ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			return geodatabase.IsVersioningSupported()
				       ? new ArcVersionedWorkspace(geodatabase)
				       : new ArcWorkspace(geodatabase);
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public ArcGIS.Core.Data.Geodatabase Geodatabase { get; }

		protected ArcWorkspace(ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			Geodatabase = geodatabase;
		}

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
					foreach (RelationshipClassDefinition definition in Geodatabase
						         .GetDefinitions<RelationshipClassDefinition>())
					{
						yield return Open(definition);
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

		private IDataset Open(Definition definition)
		{
			if (definition is FeatureClassDefinition)
			{
				FeatureClass proTable =
					Geodatabase.OpenDataset<FeatureClass>(definition.GetName());
				return ArcGeodatabaseUtils.ToArcTable(proTable);
			}

			if (definition is TableDefinition)
			{
				Table proTable = Geodatabase.OpenDataset<Table>(definition.GetName());
				return ArcGeodatabaseUtils.ToArcTable(proTable);
			}

			if (definition is RelationshipClassDefinition)
			{
				RelationshipClass proRelClass =
					Geodatabase.OpenDataset<RelationshipClass>(definition.GetName());
				return new ArcRelationshipClass(proRelClass);
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
			foreach (IDataset dataset in get_Datasets(datasetType))
			{
				yield return new ArcName(dataset);
			}

			//_geodatabase.GetDefinitions<Definition>();

			//EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDatasetName datasetName;

			//while ((datasetName = enumDatasetName.Next()) != null)
			//{
			//	yield return new ArcName((EsriSystem::ESRI.ArcGIS.esriSystem.IName) datasetName);
			//}
		}

		public string PathName => Geodatabase.GetPath().AbsolutePath;

		public esriWorkspaceType Type => (esriWorkspaceType) Geodatabase.GetGeodatabaseType();

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
				if (Geodatabase.GetGeodatabaseType() != GeodatabaseType.RemoteDatabase)
				{
					return esriConnectionDBMS.esriDBMS_Unknown;
				}

				var connectionProps = Geodatabase.GetConnector() as DatabaseConnectionProperties;

				if (connectionProps == null)
				{
					return esriConnectionDBMS.esriDBMS_Unknown;
				}

				return (esriConnectionDBMS) connectionProps.DBMS;
			}
		}

		public IWorkspaceName GetWorkspaceName()
		{
			return new ArcWorkspaceName(this);
		}

		#endregion

		#region Implementation of IFeatureWorkspace

		public ITable OpenTable(string name)
		{
			return ArcGeodatabaseUtils.ToArcTable(Geodatabase.OpenDataset<Table>(name));
		}

		public IFeatureClass OpenFeatureClass(string name)
		{
			return (IFeatureClass) OpenTable(name);
		}

		public IRelationshipClass OpenRelationshipClass(string name)
		{
			var proRelClass = Geodatabase.OpenDataset<RelationshipClass>(name);

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
			return Geodatabase.GetDomains().Select(ArcGeodatabaseUtils.ToArcDomain);
		}

		public IDomain get_DomainByName(string domainName)
		{
			return (from proDomain in Geodatabase.GetDomains()
			        where proDomain.GetName()
			                       .Equals(domainName, StringComparison.InvariantCultureIgnoreCase)
			        select ArcGeodatabaseUtils.ToArcDomain(proDomain)).FirstOrDefault();
		}

		public bool IsSameDatabase(IFeatureWorkspace otherWorkspace)
		{
			if (otherWorkspace == null)
			{
				return false;
			}

			if (otherWorkspace is not ArcWorkspace otherArcWorkspace)
			{
				return false;
			}

			if (otherArcWorkspace.Geodatabase.Handle == Geodatabase.Handle)
			{
				return true;
			}

			if (Geodatabase.IsVersioningSupported() !=
			    otherArcWorkspace.Geodatabase.IsVersioningSupported())
			{
				return false;
			}

			if (! Geodatabase.IsVersioningSupported())
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

			// Both are versioned. Compare creation date of default version.
			VersionManager thisVersionManager = Geodatabase.GetVersionManager();
			VersionManager otherVersionManager = otherArcWorkspace.Geodatabase.GetVersionManager();

			if (thisVersionManager == null || otherVersionManager == null)
			{
				return false;
			}

			Version thisDefaultVersion = thisVersionManager.GetDefaultVersion();
			Version otherDefaultVersion = otherVersionManager.GetDefaultVersion();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version names ({0}, {1})",
				                 thisDefaultVersion.GetName(), otherDefaultVersion.GetName());
			}

			if (! thisDefaultVersion.GetName().Equals(otherDefaultVersion.GetName()))
			{
				return false;
			}

			DateTime thisCreationDate = thisDefaultVersion.GetCreatedDate();
			DateTime otherCreationDate = otherDefaultVersion.GetCreatedDate();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version creation date: {0},{1}",
				                 thisCreationDate, otherCreationDate);
			}

			if (! Equals(thisCreationDate, otherCreationDate))
			{
				return false;
			}

			DateTime thisModifyDate = thisDefaultVersion.GetModifiedDate();
			DateTime otherModifyDate = otherDefaultVersion.GetModifiedDate();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version last modified date: {0},{1}",
				                 thisModifyDate, otherModifyDate);
			}

			return Equals(thisModifyDate, otherModifyDate);
		}

		#endregion
	}

	public class ArcVersionedWorkspace : ArcWorkspace, IVersion, IVersionedWorkspace
	{
		private VersionManager VersionManager { get; }
		private Version Version { get; }

		public ArcVersionedWorkspace(ArcGIS.Core.Data.Geodatabase geodatabase, string versionName)
			: this(geodatabase, geodatabase.GetVersionManager().GetVersion(versionName)) { }

		public ArcVersionedWorkspace(ArcGIS.Core.Data.Geodatabase geodatabase,
		                             Version version = null) : base(geodatabase)
		{
			Assert.True(geodatabase.IsVersioningSupported(),
			            "This geodatabase cannot be used as versioned workspace.");
			VersionManager = Assert.NotNull(geodatabase.GetVersionManager());

			Version = version ?? VersionManager.GetCurrentVersion();
		}

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
			new ArcVersionedWorkspace(Geodatabase, VersionManager.GetDefaultVersion());

		public IVersion FindVersion(string Name)
		{
			Version proVersion = VersionManager.GetVersion(Name);

			return new ArcVersionedWorkspace(Geodatabase, proVersion);
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
}

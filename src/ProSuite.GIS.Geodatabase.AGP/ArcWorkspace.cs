using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcWorkspace : IFeatureWorkspace
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ArcGIS.Core.Data.Geodatabase _geodatabase;

		public ArcWorkspace(ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			_geodatabase = geodatabase;
		}

		public ArcGIS.Core.Data.Geodatabase Geodatabase => _geodatabase;

		#region Implementation of IWorkspace

		//public IPropertySet ConnectionProperties => _aoWorkspace.ConnectionProperties;

		//public IWorkspaceFactory WorkspaceFactory => _aoWorkspace.WorkspaceFactory;

		public IEnumerable<IDataset> get_Datasets(esriDatasetType datasetType)
		{
			switch (datasetType)
			{
				case esriDatasetType.esriDTFeatureClass:
					foreach (FeatureClassDefinition definition in _geodatabase
						         .GetDefinitions<FeatureClassDefinition>())
					{
						yield return Open(definition);
					}

					break;
				case esriDatasetType.esriDTTable:
					foreach (TableDefinition definition in _geodatabase
						         .GetDefinitions<TableDefinition>())
					{
						yield return Open(definition);
					}

					break;
				case esriDatasetType.esriDTRelationshipClass:
					foreach (RelationshipClassDefinition definition in _geodatabase
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
					_geodatabase.OpenDataset<FeatureClass>(definition.GetName());
				return ArcUtils.ToArcTable(proTable);
			}

			if (definition is TableDefinition)
			{
				Table proTable = _geodatabase.OpenDataset<Table>(definition.GetName());
				return ArcUtils.ToArcTable(proTable);
			}

			if (definition is RelationshipClassDefinition)
			{
				RelationshipClass proRelClass =
					_geodatabase.OpenDataset<RelationshipClass>(definition.GetName());
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

		public string PathName => _geodatabase.GetPath().AbsolutePath;

		public esriWorkspaceType Type => (esriWorkspaceType) _geodatabase.GetGeodatabaseType();

		public bool IsDirectory()
		{
			GeodatabaseType geodatabaseType = _geodatabase.GetGeodatabaseType();

			return geodatabaseType == GeodatabaseType.FileSystem ||
			       geodatabaseType == GeodatabaseType.LocalDatabase;
		}

		public bool Exists()
		{
			Uri uri = _geodatabase.GetPath();

			return Directory.Exists(uri.LocalPath);
		}

		public void ExecuteSql(string sqlStmt)
		{
			DatabaseClient.ExecuteStatement(_geodatabase, sqlStmt);
		}

		public esriConnectionDBMS DbmsType
		{
			get
			{
				if (_geodatabase.GetGeodatabaseType() != GeodatabaseType.RemoteDatabase)
				{
					return esriConnectionDBMS.esriDBMS_Unknown;
				}

				var connectionProps = _geodatabase.GetConnector() as DatabaseConnectionProperties;

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
			return ArcUtils.ToArcTable(_geodatabase.OpenDataset<Table>(name));
		}

		//public ITable CreateTable(string Name, IFields Fields, UID CLSID, UID EXTCLSID, string ConfigKeyword)
		//{
		//	return _aoFeatureWorkspace.CreateTable(Name, Fields, CLSID, EXTCLSID, ConfigKeyword);
		//}

		public IFeatureClass OpenFeatureClass(string name)
		{
			return (IFeatureClass) OpenTable(name);
		}

		//public IFeatureClass CreateFeatureClass(string Name, IFields Fields, UID CLSID, UID EXTCLSID, esriFeatureType FeatureType,
		//	string ShapeFieldName, string ConfigKeyword)
		//{
		//	return _aoFeatureWorkspace.CreateFeatureClass(Name, Fields, CLSID, EXTCLSID, FeatureType, ShapeFieldName, ConfigKeyword);
		//}

		//public IFeatureDataset OpenFeatureDataset(string name)
		//{
		//	return _geodatabase.OpenDataset<FeatureDataset>(name);
		//}

		//public IFeatureDataset CreateFeatureDataset(string name, ISpatialReference spatialReference)
		//{
		//	return _aoFeatureWorkspace.CreateFeatureDataset(name, spatialReference);
		//}

		//public IQueryDef CreateQueryDef()
		//{
		//	return _aoFeatureWorkspace.CreateQueryDef();
		//}

		//public IFeatureDataset OpenFeatureQuery(string queryName, IQueryDef queryDef)
		//{
		//	return _aoFeatureWorkspace.OpenFeatureQuery(queryName, queryDef);
		//}

		public IRelationshipClass OpenRelationshipClass(string name)
		{
			var proRelClass = _geodatabase.OpenDataset<RelationshipClass>(name);

			return new ArcRelationshipClass(proRelClass);
		}

		//public IRelationshipClass CreateRelationshipClass(string relClassName, IObjectClass OriginClass, IObjectClass DestinationClass,
		//	string ForwardLabel, string BackwardLabel, esriRelCardinality Cardinality, esriRelNotification Notification,
		//	bool IsComposite, bool IsAttributed, IFields relAttrFields, string OriginPrimaryKey, string destPrimaryKey,
		//	string OriginForeignKey, string destForeignKey)
		//{
		//	return _aoFeatureWorkspace.CreateRelationshipClass(relClassName, OriginClass, DestinationClass, ForwardLabel, BackwardLabel, Cardinality, Notification, IsComposite, IsAttributed, relAttrFields, OriginPrimaryKey, destPrimaryKey, OriginForeignKey, destForeignKey);
		//}

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
			return _geodatabase.GetDomains().Select(d => new ArcDomain(d));
		}

		public IDomain get_DomainByName(string domainName)
		{
			return (from proDomain in _geodatabase.GetDomains()
			        where proDomain.GetName()
			                       .Equals(domainName, StringComparison.InvariantCultureIgnoreCase)
			        select new ArcDomain(proDomain)).FirstOrDefault();
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

			if (otherArcWorkspace.Geodatabase.Handle == _geodatabase.Handle)
			{
				return true;
			}

			if (_geodatabase.IsVersioningSupported() !=
			    otherArcWorkspace.Geodatabase.IsVersioningSupported())
			{
				return false;
			}

			if (! _geodatabase.IsVersioningSupported())
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
			VersionManager thisVersionManager = _geodatabase.GetVersionManager();
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

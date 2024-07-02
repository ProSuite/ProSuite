extern alias EsriGeodatabase;
extern alias EsriGeometry;
using System.Collections.Generic;
using EsriGeodatabase::ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geodatabase.AO;
using EsriGeometry::ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geodatabase.AO;

namespace ESRI.ArcGIS.Geodatabase
{
	extern alias EsriSystem;

	public class ArcWorkspace : IWorkspace, IFeatureWorkspace
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IWorkspace _aoWorkspace;
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureWorkspace _aoFeatureWorkspace;

		public ArcWorkspace(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IWorkspace aoWorkspace)
		{
			_aoWorkspace = aoWorkspace;
			_aoFeatureWorkspace = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureWorkspace)aoWorkspace;
		}

		public EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IWorkspace AoWorkspace => _aoWorkspace;

		#region Implementation of IWorkspace

		//public IPropertySet ConnectionProperties => _aoWorkspace.ConnectionProperties;

		//public IWorkspaceFactory WorkspaceFactory => _aoWorkspace.WorkspaceFactory;

		public IEnumerable<IDataset> get_Datasets(esriDatasetType datasetType)
		{
			var enumDataset = _aoWorkspace.get_Datasets(
				(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriDatasetType)datasetType);

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset dataset;
			while ((dataset = enumDataset.Next()) != null)
			{
				yield return (IDataset)ToArcTable((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)dataset);
			}
		}

		public IEnumerable<IName> get_DatasetNames(esriDatasetType datasetType)
		{
			IEnumDatasetName enumDatasetName =
				_aoWorkspace.get_DatasetNames(
					(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriDatasetType)datasetType);

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDatasetName datasetName;

			while ((datasetName = enumDatasetName.Next()) != null)
			{
				yield return new ArcName((EsriSystem::ESRI.ArcGIS.esriSystem.IName)datasetName);
			}
		}

		public string PathName => _aoWorkspace.PathName;

		public esriWorkspaceType Type => (esriWorkspaceType)_aoWorkspace.Type;

		public bool IsDirectory()
		{
			return _aoWorkspace.IsDirectory();
		}

		public bool Exists()
		{
			return _aoWorkspace.Exists();
		}

		public void ExecuteSql(string sqlStmt)
		{
			_aoWorkspace.ExecuteSQL(sqlStmt);
		}

		#endregion

		#region Implementation of IFeatureWorkspace

		public ITable OpenTable(string name)
		{
			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable aoTable = _aoFeatureWorkspace.OpenTable(name);

			var result = ToArcTable(aoTable);

			return result;
		}


		//public ITable CreateTable(string Name, IFields Fields, UID CLSID, UID EXTCLSID, string ConfigKeyword)
		//{
		//	return _aoFeatureWorkspace.CreateTable(Name, Fields, CLSID, EXTCLSID, ConfigKeyword);
		//}

		public IFeatureClass OpenFeatureClass(string name)
		{
			return (IFeatureClass)OpenTable(name);
		}

		//public IFeatureClass CreateFeatureClass(string Name, IFields Fields, UID CLSID, UID EXTCLSID, esriFeatureType FeatureType,
		//	string ShapeFieldName, string ConfigKeyword)
		//{
		//	return _aoFeatureWorkspace.CreateFeatureClass(Name, Fields, CLSID, EXTCLSID, FeatureType, ShapeFieldName, ConfigKeyword);
		//}

		public IFeatureDataset OpenFeatureDataset(string Name)
		{
			return _aoFeatureWorkspace.OpenFeatureDataset(Name);
		}

		public IFeatureDataset CreateFeatureDataset(string Name, ISpatialReference SpatialReference)
		{
			return _aoFeatureWorkspace.CreateFeatureDataset(Name, SpatialReference);
		}

		public IQueryDef CreateQueryDef()
		{
			return _aoFeatureWorkspace.CreateQueryDef();
		}

		public IFeatureDataset OpenFeatureQuery(string QueryName, IQueryDef QueryDef)
		{
			return _aoFeatureWorkspace.OpenFeatureQuery(QueryName, QueryDef);
		}

		public IRelationshipClass OpenRelationshipClass(string name)
		{
			var aoRelClass = _aoFeatureWorkspace.OpenRelationshipClass(name);

			return new ArcRelationshipClass(aoRelClass);
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
			var aoRelClass = ((ArcRelationshipClass)relClass).AoRelationshipClass;
			var aoFilter = (srcQueryFilter as ArcQueryFilter)?.AoQueryFilter;
			var aoSelectionSet = ((ArcSelectionSet)srcSelectionSet)?.AoSelectionSet;

			var aoTable = _aoFeatureWorkspace.OpenRelationshipQuery(aoRelClass, joinForward, aoFilter, aoSelectionSet,
				targetColumns, doNotPushJoinToDb);

			return ToArcTable(aoTable);
		}

		#endregion


		private static ITable ToArcTable(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable aoTable)
		{
			var result = aoTable is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass
				? (ITable)new ArcFeatureClass(featureClass)
				: new ArcTable(aoTable);
			return result;
		}
	}
}

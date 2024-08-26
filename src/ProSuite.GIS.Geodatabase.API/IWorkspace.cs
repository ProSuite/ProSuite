using System.Collections.Generic;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IWorkspace
	{
		//IPropertySet ConnectionProperties { get; }

		//IWorkspaceFactory WorkspaceFactory { get; }

		IEnumerable<IDataset> get_Datasets(esriDatasetType datasetType);

		IEnumerable<IName> get_DatasetNames(esriDatasetType datasetType);

		string PathName { get; }

		esriWorkspaceType Type { get; }

		bool IsDirectory();

		bool Exists();

		void ExecuteSql(string sqlStmt);

		/// <summary>
		/// The type of DBMS used by the (remote) workspace.
		/// </summary>
		esriConnectionDBMS DbmsType { get; }

		// TODO:
		// IWorkspaceName GetWorkspaceName()
	}

	public interface IFeatureWorkspace
	{
		ITable OpenTable(string name);

		//ITable CreateTable(
		//	string Name,
		//	IFields Fields,
		//	UID CLSID,
		//	UID EXTCLSID,
		//	string ConfigKeyword);

		IFeatureClass OpenFeatureClass(string name);

		//IFeatureClass CreateFeatureClass(
		//	string Name,
		//	IFields Fields,
		//	UID CLSID,
		//	UID EXTCLSID,
		//	[In] esriFeatureType FeatureType,
		//	string ShapeFieldName,
		//	string ConfigKeyword);

		//IFeatureDataset OpenFeatureDataset(string name);

		//IFeatureDataset CreateFeatureDataset(string name, ISpatialReference spatialReference);

		//IQueryDef CreateQueryDef();

		//IFeatureDataset OpenFeatureQuery(string queryName, IQueryDef queryDef);

		IRelationshipClass OpenRelationshipClass(string name);

		//IRelationshipClass CreateRelationshipClass(
		//	string relClassName,
		//	IObjectClass OriginClass,
		//	IObjectClass DestinationClass,
		//	string ForwardLabel,
		//	string BackwardLabel,
		//	esriRelCardinality Cardinality,
		//	esriRelNotification Notification,
		//	bool IsComposite,
		//	bool IsAttributed,
		//	IFields relAttrFields,
		//	string OriginPrimaryKey,
		//	string destPrimaryKey,
		//	string OriginForeignKey,
		//	string destForeignKey);

		ITable OpenRelationshipQuery(
			IRelationshipClass relClass,
			bool joinForward,
			IQueryFilter srcQueryFilter,
			ISelectionSet srcSelectionSet,
			string targetColumns,
			bool doNotPushJoinToDb);

		// Originally from a separate interface:
		IEnumerable<IDomain> Domains();

		IDomain get_DomainByName(string domainName);
	}

	public enum esriWorkspaceType
	{
		esriFileSystemWorkspace,
		esriLocalDatabaseWorkspace,
		esriRemoteDatabaseWorkspace,
	}
}

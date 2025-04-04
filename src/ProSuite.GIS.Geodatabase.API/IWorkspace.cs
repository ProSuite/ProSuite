using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
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

		IWorkspaceName GetWorkspaceName();

		bool IsSameDatabase(IWorkspace otherWorkspace);

		string Description { get; }
	}

	public interface IFeatureWorkspace : IWorkspace
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

	public interface IVersionedWorkspace
	{
		IVersion DefaultVersion { get; }

		IVersion FindVersion(string name);
	}

	public interface IVersion
	{
		IVersionInfo VersionInfo { get; }

		string VersionName { get; }

		string Description { get; }

		bool HasParent();

		void Delete();

		void RefreshVersion();

		IVersion CreateVersion(string newName);
	}

	public interface IVersionInfo
	{
		string VersionName { get; }

		string Description { get; }

		object Created { get; }

		object Modified { get; }

		IVersionInfo Parent { get; }

		IEnumerable<IVersionInfo> Children { get; }

		bool IsOwner();
	}

	public enum esriWorkspaceType
	{
		esriFileSystemWorkspace,
		esriLocalDatabaseWorkspace,
		esriRemoteDatabaseWorkspace,
	}
}

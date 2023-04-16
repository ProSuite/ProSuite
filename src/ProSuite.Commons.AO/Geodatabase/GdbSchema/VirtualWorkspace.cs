using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class VirtualWorkspace : IWorkspace, IWorkspaceEdit, IFeatureWorkspace,
	                                IWorkspaceDomains, IDataset
	{
		bool IDataset.CanCopy() => CanCopy();

		public virtual bool CanCopy() =>
			throw new NotImplementedException("Implement in derived class");

		IDataset IDataset.Copy(string copyName, IWorkspace copyWorkspace) =>
			Copy(copyName, copyWorkspace);

		public virtual IDataset Copy(string copyName, IWorkspace copyWorkspace) =>
			throw new NotImplementedException("Implement in derived class");

		bool IDataset.CanDelete() => CanDelete();

		public virtual bool CanDelete() =>
			throw new NotImplementedException("Implement in derived class");

		void IDataset.Delete() => Delete();

		public virtual void Delete() =>
			throw new NotImplementedException("Implement in derived class");

		bool IDataset.CanRename() => CanRename();

		public virtual bool CanRename() =>
			throw new NotImplementedException("Implement in derived class");

		void IDataset.Rename(string name) => Rename(name);

		public virtual void Rename(string name) =>
			throw new NotImplementedException("Implement in derived class");

		string IDataset.Name => Name;

		public virtual string Name =>
			throw new NotImplementedException("Implement in derived class");

		IName IDataset.FullName => FullName;

		private IName _name;

		public virtual IName FullName =>
			_name ?? (_name = new WorkspaceName(this));

		protected virtual long? WorkspaceHandle { get; set; }

		string IDataset.BrowseName
		{
			get => BrowseName;
			set => BrowseName = value;
		}

		public virtual string BrowseName
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		esriDatasetType IDataset.Type => Type;

		public virtual esriDatasetType Type =>
			throw new NotImplementedException("Implement in derived class");

		string IDataset.Category => Category;

		public virtual string Category =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDataset IDataset.Subsets => Subsets;

		public virtual IEnumDataset Subsets =>
			throw new NotImplementedException("Implement in derived class");

		IWorkspace IDataset.Workspace => this;

		IPropertySet IDataset.PropertySet => PropertySet;

		public virtual IPropertySet PropertySet =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspace.IsDirectory() => IsDirectory();

		public virtual bool IsDirectory() =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspace.Exists() => Exists();

		public virtual bool Exists() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspace.ExecuteSQL(string sqlStmt) => ExecuteSql(sqlStmt);

		public virtual void ExecuteSql(string sqlStmt) =>
			throw new NotImplementedException("Implement in derived class");

		IPropertySet IWorkspace.ConnectionProperties => ConnectionProperties;

		public virtual IPropertySet ConnectionProperties =>
			throw new NotImplementedException("Implement in derived class");

		IWorkspaceFactory IWorkspace.WorkspaceFactory => WorkspaceFactory;

		public virtual IWorkspaceFactory WorkspaceFactory =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDataset IWorkspace.get_Datasets(esriDatasetType datasetType) =>
			get_Datasets(datasetType);

		public virtual IEnumDataset get_Datasets(esriDatasetType datasetType) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDatasetName IWorkspace.get_DatasetNames(esriDatasetType datasetType) =>
			get_DatasetNames(datasetType);

		public virtual IEnumDatasetName get_DatasetNames(esriDatasetType datasetType) =>
			throw new NotImplementedException("Implement in derived class");

		string IWorkspace.PathName => PathName;

		public virtual string PathName =>
			throw new NotImplementedException("Implement in derived class");

		esriWorkspaceType IWorkspace.Type => WorkspaceType;

		public virtual esriWorkspaceType WorkspaceType =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StartEditing(bool withUndoRedo) => StartEditing(withUndoRedo);

		public virtual void StartEditing(bool withUndoRedo) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StopEditing(bool saveEdits) => StopEditing(saveEdits);

		public virtual void StopEditing(bool saveEdits) =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspaceEdit.IsBeingEdited() => IsBeingEdited();

		public virtual bool IsBeingEdited() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StartEditOperation() => StartEditOperation();

		public virtual void StartEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StopEditOperation() => StopEditOperation();

		public virtual void StopEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.AbortEditOperation() => AbortEditOperation();

		public virtual void AbortEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.HasUndos(ref bool hasUndos) => HasUndos(ref hasUndos);

		public virtual void HasUndos(ref bool hasUndos) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.UndoEditOperation() => UndoEditOperation();

		public virtual void UndoEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.HasRedos(ref bool hasRedos) => HasRedos(ref hasRedos);

		public virtual void HasRedos(ref bool hasRedos) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.RedoEditOperation() => RedoEditOperation();

		public virtual void RedoEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.EnableUndoRedo() => EnableUndoRedo();

		public virtual void EnableUndoRedo() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.DisableUndoRedo() => DisableUndoRedo();

		public virtual void DisableUndoRedo() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.HasEdits(ref bool hasEdits) => HasEdits(ref hasEdits);

		public virtual void HasEdits(ref bool hasEdits) =>
			throw new NotImplementedException("Implement in derived class");

		ITable IFeatureWorkspace.OpenTable(string name) => OpenTable(name);

		public virtual ITable OpenTable(string name) =>
			throw new NotImplementedException("Implement in derived class");

		ITable IFeatureWorkspace.CreateTable(string name, IFields fields, UID clsid, UID extclsid,
		                                     string configKeyword) =>
			CreateTable(name, fields, clsid, extclsid, configKeyword);

		public virtual ITable CreateTable(string name, IFields fields, UID clsid,
		                                  UID extclsid, string configKeyword) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureClass IFeatureWorkspace.OpenFeatureClass(string name) =>
			OpenFeatureClass(name);

		public virtual IFeatureClass OpenFeatureClass(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureClass IFeatureWorkspace.CreateFeatureClass(string name, IFields fields, UID clsid,
		                                                   UID extclsid,
		                                                   esriFeatureType featureType,
		                                                   string shapeFieldName,
		                                                   string configKeyword) =>
			CreateFeatureClass(name, fields, clsid, extclsid, featureType, shapeFieldName,
			                   configKeyword);

		public virtual IFeatureClass CreateFeatureClass(
			string name, IFields fields, UID clsid, UID extclsid,
			esriFeatureType featureType, string shapeFieldName, string configKeyword) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureDataset IFeatureWorkspace.OpenFeatureDataset(string name) =>
			OpenFeatureDataset(name);

		public virtual IFeatureDataset OpenFeatureDataset(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureDataset IFeatureWorkspace.CreateFeatureDataset(
			string name, ISpatialReference spatialReference) =>
			CreateFeatureDataset(name, spatialReference);

		public virtual IFeatureDataset
			CreateFeatureDataset(string name, ISpatialReference spatialReference) =>
			throw new NotImplementedException("Implement in derived class");

		IQueryDef IFeatureWorkspace.CreateQueryDef() => CreateQueryDef();

		public virtual IQueryDef CreateQueryDef() =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureDataset IFeatureWorkspace.OpenFeatureQuery(string queryName, IQueryDef queryDef) =>
			OpenFeatureQuery(queryName, queryDef);

		public virtual IFeatureDataset
			OpenFeatureQuery(string queryName, IQueryDef queryDef) =>
			throw new NotImplementedException("Implement in derived class");

		IRelationshipClass IFeatureWorkspace.OpenRelationshipClass(string name) =>
			OpenRelationshipClass(name);

		public virtual IRelationshipClass OpenRelationshipClass(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IRelationshipClass IFeatureWorkspace.CreateRelationshipClass(
			string relClassName, IObjectClass originClass, IObjectClass destinationClass,
			string forwardLabel, string backwardLabel, esriRelCardinality cardinality,
			esriRelNotification notification,
			bool isComposite, bool isAttributed, IFields relAttrFields,
			string originPrimaryKey, string destPrimaryKey, string originForeignKey,
			string destForeignKey)
			=> CreateRelationshipClass(relClassName, originClass, destinationClass,
			                           forwardLabel, backwardLabel, cardinality,
			                           notification,
			                           isComposite, isAttributed, relAttrFields,
			                           originPrimaryKey, destPrimaryKey, originForeignKey,
			                           destForeignKey);

		public virtual IRelationshipClass CreateRelationshipClass(
			string relClassName, IObjectClass originClass, IObjectClass destinationClass,
			string forwardLabel, string backwardLabel, esriRelCardinality cardinality,
			esriRelNotification notification,
			bool isComposite, bool isAttributed, IFields relAttrFields,
			string originPrimaryKey, string destPrimaryKey, string originForeignKey,
			string destForeignKey) =>
			throw new NotImplementedException("Implement in derived class");

		ITable IFeatureWorkspace.OpenRelationshipQuery(IRelationshipClass relClass,
		                                               bool joinForward,
		                                               IQueryFilter srcQueryFilter,
		                                               ISelectionSet srcSelectionSet,
		                                               string targetColumns, bool doNotPushJoinToDb)
			=> OpenRelationshipQuery(relClass, joinForward, srcQueryFilter, srcSelectionSet,
			                         targetColumns, doNotPushJoinToDb);

		public IDataset OpenExtensionDataset(esriDatasetType extensionDatasetType,
		                                     string extensionDatasetName)
		{
			throw new NotImplementedException();
		}

		public virtual ITable OpenRelationshipQuery(
			IRelationshipClass relClass, bool joinForward,
			IQueryFilter srcQueryFilter, ISelectionSet srcSelectionSet, string targetColumns,
			bool doNotPushJoinToDb) =>
			throw new NotImplementedException("Implement in derived class");

		int IWorkspaceDomains.AddDomain(IDomain domain) => AddDomain(domain);

		public virtual int AddDomain(IDomain domain) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceDomains.DeleteDomain(string domainName) => DeleteDomain(domainName);

		public virtual void DeleteDomain(string domainName) =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspaceDomains.get_CanDeleteDomain(string domainName) =>
			get_CanDeleteDomain(domainName);

		public virtual bool get_CanDeleteDomain(string domainName) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDomain IWorkspaceDomains.Domains => Domains;

		public virtual IEnumDomain Domains =>
			throw new NotImplementedException("Implement in derived class");

		IDomain IWorkspaceDomains.get_DomainByName(string name) => get_DomainByName(name);

		public virtual IDomain get_DomainByName(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDomain IWorkspaceDomains.get_DomainsByFieldType(esriFieldType fieldType) =>
			get_DomainsByFieldType(fieldType);

		public virtual IEnumDomain get_DomainsByFieldType(esriFieldType fieldType) =>
			throw new NotImplementedException("Implement in derived class");

		protected class WorkspaceName : IName, IWorkspaceName2
		{
			private readonly VirtualWorkspace _workspace;

			private string _connectionString;

			public WorkspaceName(VirtualWorkspace workspace)
			{
				_workspace = workspace;

				// Used for comparison. Assumption: model-workspace relationship is 1-1
				// Once various versions should be supported, this will have to be more fancy.
				_connectionString =
					$"Provider=VirtualWorkspace.WorkspaceName;Data Source={workspace.WorkspaceHandle}";
			}

			#region IName members

			public object Open()
			{
				return _workspace;
			}

			public virtual string ConnectionString
			{
				get { return _connectionString; }
				set { _connectionString = value; }
			}

			public string NameString { get; set; }

			#endregion

			#region IWorkspaceName members

			string IWorkspaceName.PathName
			{
				get => PathName;
				set => PathName = value;
			}

			string IWorkspaceName2.PathName
			{
				get => PathName;
				set => PathName = value;
			}

			public virtual string PathName
			{
				get => _workspace.PathName;
				set => throw new NotImplementedException("Implement in derived class");
			}

			string IWorkspaceName.WorkspaceFactoryProgID
			{
				get => WorkspaceFactoryProgID;
				set => WorkspaceFactoryProgID = value;
			}

			string IWorkspaceName2.WorkspaceFactoryProgID
			{
				get => WorkspaceFactoryProgID;
				set => WorkspaceFactoryProgID = value;
			}

			public virtual string WorkspaceFactoryProgID
			{
				get => throw new NotImplementedException("Implement in derived class");
				set => throw new NotImplementedException("Implement in derived class");
			}

			string IWorkspaceName.BrowseName
			{
				get => BrowseName;
				set => BrowseName = value;
			}

			string IWorkspaceName2.BrowseName
			{
				get => BrowseName;
				set => BrowseName = value;
			}

			public virtual string BrowseName
			{
				get => throw new NotImplementedException("Implement in derived class");
				set => throw new NotImplementedException("Implement in derived class");
			}

			IWorkspaceFactory IWorkspaceName.WorkspaceFactory => WorkspaceFactory;
			IWorkspaceFactory IWorkspaceName2.WorkspaceFactory => WorkspaceFactory;

			public virtual IWorkspaceFactory WorkspaceFactory =>
				throw new NotImplementedException("Implement in derived class");

			IPropertySet IWorkspaceName.ConnectionProperties
			{
				get => ConnectionProperties;
				set => ConnectionProperties = value;
			}

			IPropertySet IWorkspaceName2.ConnectionProperties
			{
				get => ConnectionProperties;
				set => ConnectionProperties = value;
			}

			public virtual IPropertySet ConnectionProperties
			{
				get => throw new NotImplementedException("Implement in derived class");
				set => throw new NotImplementedException("Implement in derived class");
			}

			public esriWorkspaceType Type => _workspace.WorkspaceType;

			string IWorkspaceName.Category => Category;
			string IWorkspaceName2.Category => Category;

			public virtual string Category =>
				throw new NotImplementedException("Implement in derived class");

			#endregion
		}
	}
}

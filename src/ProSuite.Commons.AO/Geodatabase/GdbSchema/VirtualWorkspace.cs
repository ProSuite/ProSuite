using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class VirtualWorkspace : IWorkspace, IWorkspaceEdit, IFeatureWorkspace,
	                                IWorkspaceDomains, IDataset
	{
		bool IDataset.CanCopy() => VirtualCanCopy();

		protected virtual bool VirtualCanCopy() =>
			throw new NotImplementedException("Implement in derived class");

		IDataset IDataset.Copy(string copyName, IWorkspace copyWorkspace) =>
			VirtualCopy(copyName, copyWorkspace);

		protected virtual IDataset VirtualCopy(string copyName, IWorkspace copyWorkspace) =>
			throw new NotImplementedException("Implement in derived class");

		bool IDataset.CanDelete() => VirtualCanDelete();

		protected virtual bool VirtualCanDelete() =>
			throw new NotImplementedException("Implement in derived class");

		void IDataset.Delete() => VirtualDelete();

		protected virtual void VirtualDelete() =>
			throw new NotImplementedException("Implement in derived class");

		bool IDataset.CanRename() => VirtualCanRename();

		protected virtual bool VirtualCanRename() =>
			throw new NotImplementedException("Implement in derived class");

		void IDataset.Rename(string name) => VirtualRename(name);

		protected virtual void VirtualRename(string name) =>
			throw new NotImplementedException("Implement in derived class");

		string IDataset.Name => VirtualName;

		protected virtual string VirtualName =>
			throw new NotImplementedException("Implement in derived class");

		IName IDataset.FullName => VirtualFullName;

		private IName _name;
		protected virtual IName VirtualFullName => _name ?? (_name = new Name(this));

		protected virtual long? VirtualWorkspaceHandle { get; set; }

		string IDataset.BrowseName
		{
			get => VirtualBrowseName;
			set => VirtualBrowseName = value;
		}

		protected virtual string VirtualBrowseName
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		esriDatasetType IDataset.Type => VirtualType;

		protected virtual esriDatasetType VirtualType =>
			throw new NotImplementedException("Implement in derived class");

		string IDataset.Category => VirtualCategory;

		protected virtual string VirtualCategory =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDataset IDataset.Subsets => VirtualSubsets;

		protected virtual IEnumDataset VirtualSubsets =>
			throw new NotImplementedException("Implement in derived class");

		IWorkspace IDataset.Workspace => this;

		IPropertySet IDataset.PropertySet => VirtualPropertySet;

		protected virtual IPropertySet VirtualPropertySet =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspace.IsDirectory() => VirtualIsDirectory();

		protected virtual bool VirtualIsDirectory() =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspace.Exists() => VirtualExists();

		protected virtual bool VirtualExists() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspace.ExecuteSQL(string sqlStmt) => VirtualExecuteSql(sqlStmt);

		protected virtual void VirtualExecuteSql(string sqlStmt) =>
			throw new NotImplementedException("Implement in derived class");

		IPropertySet IWorkspace.ConnectionProperties => VirtualConnectionProperties;

		protected virtual IPropertySet VirtualConnectionProperties =>
			throw new NotImplementedException("Implement in derived class");

		IWorkspaceFactory IWorkspace.WorkspaceFactory => VirtualWorkspaceFactory;

		protected virtual IWorkspaceFactory VirtualWorkspaceFactory =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDataset IWorkspace.get_Datasets(esriDatasetType datasetType) =>
			VirtualGet_Datasets(datasetType);

		protected virtual IEnumDataset VirtualGet_Datasets(esriDatasetType datasetType) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDatasetName IWorkspace.get_DatasetNames(esriDatasetType datasetType) =>
			VirtualGet_DatasetNames(datasetType);

		protected virtual IEnumDatasetName VirtualGet_DatasetNames(esriDatasetType datasetType) =>
			throw new NotImplementedException("Implement in derived class");

		string IWorkspace.PathName => VirtualPathName;

		protected virtual string VirtualPathName =>
			throw new NotImplementedException("Implement in derived class");

		esriWorkspaceType IWorkspace.Type => VirtualWorkspaceType;

		protected virtual esriWorkspaceType VirtualWorkspaceType =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StartEditing(bool withUndoRedo) => VirtualStartEditing(withUndoRedo);

		protected virtual void VirtualStartEditing(bool withUndoRedo) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StopEditing(bool saveEdits) => VirtualStopEditing(saveEdits);

		protected virtual void VirtualStopEditing(bool saveEdits) =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspaceEdit.IsBeingEdited() => VirtualIsBeingEdited();

		protected virtual bool VirtualIsBeingEdited() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StartEditOperation() => VirtualStartEditOperation();

		protected virtual void VirtualStartEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.StopEditOperation() => VirtualStopEditOperation();

		protected virtual void VirtualStopEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.AbortEditOperation() => VirtualAbortEditOperation();

		protected virtual void VirtualAbortEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.HasUndos(ref bool hasUndos) => VirtualHasUndos(ref hasUndos);

		protected virtual void VirtualHasUndos(ref bool hasUndos) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.UndoEditOperation() => VirtualUndoEditOperation();

		protected virtual void VirtualUndoEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.HasRedos(ref bool hasRedos) => VirtualHasRedos(ref hasRedos);

		protected virtual void VirtualHasRedos(ref bool hasRedos) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.RedoEditOperation() => VirtualRedoEditOperation();

		protected virtual void VirtualRedoEditOperation() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.EnableUndoRedo() => VirtualEnableUndoRedo();

		protected virtual void VirtualEnableUndoRedo() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.DisableUndoRedo() => VirtualDisableUndoRedo();

		protected virtual void VirtualDisableUndoRedo() =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceEdit.HasEdits(ref bool hasEdits) => VirtualHasEdits(ref hasEdits);

		protected virtual void VirtualHasEdits(ref bool hasEdits) =>
			throw new NotImplementedException("Implement in derived class");

		ITable IFeatureWorkspace.OpenTable(string name) => VirtualOpenTable(name);

		protected virtual ITable VirtualOpenTable(string name) =>
			throw new NotImplementedException("Implement in derived class");

		ITable IFeatureWorkspace.CreateTable(string name, IFields fields, UID clsid, UID extclsid,
		                                     string configKeyword) =>
			VirtualCreateTable(name, fields, clsid, extclsid, configKeyword);

		protected virtual ITable VirtualCreateTable(string name, IFields fields, UID clsid,
		                                            UID extclsid, string configKeyword) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureClass IFeatureWorkspace.OpenFeatureClass(string name) =>
			VirtualOpenFeatureClass(name);

		protected virtual IFeatureClass VirtualOpenFeatureClass(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureClass IFeatureWorkspace.CreateFeatureClass(string name, IFields fields, UID clsid,
		                                                   UID extclsid,
		                                                   esriFeatureType featureType,
		                                                   string shapeFieldName,
		                                                   string configKeyword) =>
			VirtualCreateFeatureClass(name, fields, clsid, extclsid, featureType, shapeFieldName,
			                          configKeyword);

		protected virtual IFeatureClass VirtualCreateFeatureClass(
			string name, IFields fields, UID clsid, UID extclsid,
			esriFeatureType featureType, string shapeFieldName, string configKeyword) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureDataset IFeatureWorkspace.OpenFeatureDataset(string name) =>
			VirtualOpenFeatureDataset(name);

		protected virtual IFeatureDataset VirtualOpenFeatureDataset(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureDataset IFeatureWorkspace.CreateFeatureDataset(
			string name, ISpatialReference spatialReference) =>
			VirtualCreateFeatureDataset(name, spatialReference);

		protected virtual IFeatureDataset
			VirtualCreateFeatureDataset(string name, ISpatialReference spatialReference) =>
			throw new NotImplementedException("Implement in derived class");

		IQueryDef IFeatureWorkspace.CreateQueryDef() => VirtualCreateQueryDef();

		protected virtual IQueryDef VirtualCreateQueryDef() =>
			throw new NotImplementedException("Implement in derived class");

		IFeatureDataset IFeatureWorkspace.OpenFeatureQuery(string queryName, IQueryDef queryDef) =>
			VirtualOpenFeatureQuery(queryName, queryDef);

		protected virtual IFeatureDataset
			VirtualOpenFeatureQuery(string queryName, IQueryDef queryDef) =>
			throw new NotImplementedException("Implement in derived class");

		IRelationshipClass IFeatureWorkspace.OpenRelationshipClass(string name) =>
			VirtualOpenRelationshipClass(name);

		protected virtual IRelationshipClass VirtualOpenRelationshipClass(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IRelationshipClass IFeatureWorkspace.CreateRelationshipClass(
			string relClassName, IObjectClass originClass, IObjectClass destinationClass,
			string forwardLabel, string backwardLabel, esriRelCardinality cardinality,
			esriRelNotification notification,
			bool isComposite, bool isAttributed, IFields relAttrFields,
			string originPrimaryKey, string destPrimaryKey, string originForeignKey,
			string destForeignKey)
			=> VirtualCreateRelationshipClass(relClassName, originClass, destinationClass,
			                                  forwardLabel, backwardLabel, cardinality,
			                                  notification,
			                                  isComposite, isAttributed, relAttrFields,
			                                  originPrimaryKey, destPrimaryKey, originForeignKey,
			                                  destForeignKey);

		protected virtual IRelationshipClass VirtualCreateRelationshipClass(
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
			=> VirtualOpenRelationshipQuery(relClass, joinForward, srcQueryFilter, srcSelectionSet,
			                                targetColumns, doNotPushJoinToDb);

		protected virtual ITable VirtualOpenRelationshipQuery(
			IRelationshipClass relClass, bool joinForward,
			IQueryFilter srcQueryFilter, ISelectionSet srcSelectionSet, string targetColumns,
			bool doNotPushJoinToDb) =>
			throw new NotImplementedException("Implement in derived class");

		int IWorkspaceDomains.AddDomain(IDomain domain) => VirtualAddDomain(domain);

		protected virtual int VirtualAddDomain(IDomain domain) =>
			throw new NotImplementedException("Implement in derived class");

		void IWorkspaceDomains.DeleteDomain(string domainName) => VirtualDeleteDomain(domainName);

		protected virtual void VirtualDeleteDomain(string domainName) =>
			throw new NotImplementedException("Implement in derived class");

		bool IWorkspaceDomains.get_CanDeleteDomain(string domainName) =>
			Virtualget_CanDeleteDomain(domainName);

		protected virtual bool Virtualget_CanDeleteDomain(string domainName) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDomain IWorkspaceDomains.Domains => VirtualDomains;

		protected virtual IEnumDomain VirtualDomains =>
			throw new NotImplementedException("Implement in derived class");

		IDomain IWorkspaceDomains.get_DomainByName(string name) => Virtualget_DomainByName(name);

		protected virtual IDomain Virtualget_DomainByName(string name) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDomain IWorkspaceDomains.get_DomainsByFieldType(esriFieldType fieldType) =>
			Virtualget_DomainsByFieldType(fieldType);

		protected virtual IEnumDomain Virtualget_DomainsByFieldType(esriFieldType fieldType) =>
			throw new NotImplementedException("Implement in derived class");

		private class Name : IName, IWorkspaceName2
		{
			private readonly VirtualWorkspace _workspace;

			private string _connectionString;

			public Name(VirtualWorkspace workspace)
			{
				_workspace = workspace;

				// Used for comparison. Assumption: model-workspace relationship is 1-1
				// Once various versions should be supported, this will have to be more fancy.
				_connectionString =
					$"Provider=VirtualWorkspace.Name;Data Source={workspace.VirtualWorkspaceHandle}";
			}

			#region IName members

			public object Open()
			{
				return _workspace;
			}

			public string ConnectionString
			{
				get { return VirtualConnectionString; }
				set { VirtualConnectionString = value; }
			}

			protected virtual string VirtualConnectionString
			{
				get { return _connectionString; }
				set { _connectionString = value; }
			}

			public string NameString { get; set; }

			#endregion

			#region IWorkspaceName members

			string IWorkspaceName.PathName
			{
				get => VirtualPathName;
				set => VirtualPathName = value;
			}

			string IWorkspaceName2.PathName
			{
				get => VirtualPathName;
				set => VirtualPathName = value;
			}

			protected virtual string VirtualPathName
			{
				get => _workspace.VirtualPathName;
				set => throw new NotImplementedException("Implement in derived class");
			}

			string IWorkspaceName.WorkspaceFactoryProgID
			{
				get => VirtualWorkspaceFactoryProgID;
				set => VirtualWorkspaceFactoryProgID = value;
			}

			string IWorkspaceName2.WorkspaceFactoryProgID
			{
				get => VirtualWorkspaceFactoryProgID;
				set => VirtualWorkspaceFactoryProgID = value;
			}

			protected virtual string VirtualWorkspaceFactoryProgID
			{
				get => throw new NotImplementedException("Implement in derived class");
				set => throw new NotImplementedException("Implement in derived class");
			}

			string IWorkspaceName.BrowseName
			{
				get => VirtualBrowseName;
				set => VirtualBrowseName = value;
			}

			string IWorkspaceName2.BrowseName
			{
				get => VirtualBrowseName;
				set => VirtualBrowseName = value;
			}

			protected virtual string VirtualBrowseName
			{
				get => throw new NotImplementedException("Implement in derived class");
				set => throw new NotImplementedException("Implement in derived class");
			}

			IWorkspaceFactory IWorkspaceName.WorkspaceFactory => VirtualWorkspaceFactory;
			IWorkspaceFactory IWorkspaceName2.WorkspaceFactory => VirtualWorkspaceFactory;

			protected virtual IWorkspaceFactory VirtualWorkspaceFactory =>
				throw new NotImplementedException("Implement in derived class");

			IPropertySet IWorkspaceName.ConnectionProperties
			{
				get => VirtualConnectionProperties;
				set => VirtualConnectionProperties = value;
			}

			IPropertySet IWorkspaceName2.ConnectionProperties
			{
				get => VirtualConnectionProperties;
				set => VirtualConnectionProperties = value;
			}

			protected virtual IPropertySet VirtualConnectionProperties
			{
				get => throw new NotImplementedException("Implement in derived class");
				set => throw new NotImplementedException("Implement in derived class");
			}

			public esriWorkspaceType Type => _workspace.VirtualWorkspaceType;

			string IWorkspaceName.Category => VirtualCategory;
			string IWorkspaceName2.Category => VirtualCategory;

			protected virtual string VirtualCategory =>
				throw new NotImplementedException("Implement in derived class");

			#endregion
		}
	}
}

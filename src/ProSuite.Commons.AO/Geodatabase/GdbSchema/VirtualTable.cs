using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class VirtualFeatureClass : VirtualTable, IFeatureClass, IGeoDataset
	{
		public VirtualFeatureClass(string name) : base(name) { }
	}

	public class VirtualTable : IDataset, ITable, IObjectClass, ISchemaLock
	{
		protected GdbFields _fields;
		private GdbTableName _tableName;
		protected GdbFields GdbFields => _fields ?? (_fields = new GdbFields());
		private string _name;

		public VirtualTable(string name)
		{
			_name = name;
		}

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

		protected virtual void VirtualRename(string name)
		{
			_name = name;
		}

		string IDataset.Name => VirtualName;
		protected virtual string VirtualName => _name;

		IName IDataset.FullName => VirtualFullName;

		protected virtual IName VirtualFullName =>
			_tableName ?? (_tableName = new GdbTableName(this));

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
		protected virtual esriDatasetType VirtualType => esriDatasetType.esriDTTable;

		string IDataset.Category => VirtualCategory;

		protected virtual string VirtualCategory =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDataset IDataset.Subsets => VirtualSubsets;

		protected virtual IEnumDataset VirtualSubsets =>
			throw new NotImplementedException("Implement in derived class");

		IWorkspace IDataset.Workspace => VirtualWorkspace;

		protected virtual IWorkspace VirtualWorkspace =>
			throw new NotImplementedException("Implement in derived class");

		IPropertySet IDataset.PropertySet => VirtualPropertySet;

		protected virtual IPropertySet VirtualPropertySet =>
			throw new NotImplementedException("Implement in derived class");

		int IClass.FindField(string name) => VirtualFindField(name);

		int IObjectClass.FindField(string name) => VirtualFindField(name);

		int ITable.FindField(string name) => VirtualFindField(name);

		public int FindField(string name) => VirtualFindField(name);

		protected virtual int VirtualFindField(string name) => GdbFields.FindField(name);

		void IClass.AddField(IField Field) => VirtualAddField(Field);

		void IObjectClass.AddField(IField Field) => VirtualAddField(Field);

		void ITable.AddField(IField Field) => VirtualAddField(Field);

		public void AddField(IField Field) => VirtualAddField(Field);

		protected virtual int VirtualAddField(IField Field) => GdbFields.AddField(Field);

		void IClass.DeleteField(IField Field) => VirtualDeleteField(Field);

		void IObjectClass.DeleteField(IField Field) => VirtualDeleteField(Field);

		void ITable.DeleteField(IField Field) => VirtualDeleteField(Field);

		public void DeleteField(IField Field) => VirtualDeleteField(Field);

		protected virtual void VirtualDeleteField(IField Field) =>
			throw new NotImplementedException("Implement in derived class");

		void IClass.AddIndex(IIndex Index) => VirtualAddIndex(Index);

		void IObjectClass.AddIndex(IIndex Index) => VirtualAddIndex(Index);

		void ITable.AddIndex(IIndex Index) => VirtualAddIndex(Index);

		public void AddIndex(IIndex Index) => VirtualAddIndex(Index);

		protected virtual void VirtualAddIndex(IIndex Index) =>
			throw new NotImplementedException("Implement in derived class");

		void IClass.DeleteIndex(IIndex Index) => VirtualDeleteIndex(Index);

		void IObjectClass.DeleteIndex(IIndex Index) => VirtualDeleteIndex(Index);

		void ITable.DeleteIndex(IIndex Index) => VirtualDeleteIndex(Index);

		public void DeleteIndex(IIndex Index) => VirtualDeleteIndex(Index);

		protected virtual void VirtualDeleteIndex(IIndex Index) =>
			throw new NotImplementedException("Implement in derived class");

		IFields IClass.Fields => VirtualFields;
		IFields IObjectClass.Fields => VirtualFields;
		IFields ITable.Fields => VirtualFields;
		public IFields Fields => VirtualFields;
		protected virtual IFields VirtualFields => GdbFields;

		IIndexes IClass.Indexes => VirtualIndexes;
		IIndexes IObjectClass.Indexes => VirtualIndexes;
		IIndexes ITable.Indexes => VirtualIndexes;
		public IIndexes Indexes => VirtualIndexes;

		protected virtual IIndexes VirtualIndexes =>
			throw new NotImplementedException("Implement in derived class");

		bool IClass.HasOID => VirtualHasOID;
		bool IObjectClass.HasOID => VirtualHasOID;
		bool ITable.HasOID => VirtualHasOID;
		public bool HasOID => VirtualHasOID;

		protected virtual bool VirtualHasOID =>
			throw new NotImplementedException("Implement in derived class");

		string IClass.OIDFieldName => VirtualOIDFieldName;
		string IObjectClass.OIDFieldName => VirtualOIDFieldName;
		string ITable.OIDFieldName => VirtualOIDFieldName;
		public string OIDFieldName => VirtualOIDFieldName;

		protected virtual string VirtualOIDFieldName =>
			throw new NotImplementedException("Implement in derived class");

		UID IClass.CLSID => VirtualCLSID;
		UID IObjectClass.CLSID => VirtualCLSID;
		UID ITable.CLSID => VirtualCLSID;
		public UID CLSID => VirtualCLSID;

		protected virtual UID VirtualCLSID =>
			throw new NotImplementedException("Implement in derived class");

		UID IClass.EXTCLSID => VirtualEXTCLSID;
		UID IObjectClass.EXTCLSID => VirtualEXTCLSID;
		UID ITable.EXTCLSID => VirtualEXTCLSID;
		public UID EXTCLSID => VirtualEXTCLSID;

		protected virtual UID VirtualEXTCLSID =>
			throw new NotImplementedException("Implement in derived class");

		object IClass.Extension => VirtualExtension;
		object IObjectClass.Extension => VirtualExtension;
		object ITable.Extension => VirtualExtension;
		public object Extension => VirtualExtension;

		protected virtual object VirtualExtension =>
			throw new NotImplementedException("Implement in derived class");

		IPropertySet IClass.ExtensionProperties => VirtualExtensionProperties;
		IPropertySet IObjectClass.ExtensionProperties => VirtualExtensionProperties;
		IPropertySet ITable.ExtensionProperties => VirtualExtensionProperties;
		public IPropertySet ExtensionProperties => VirtualExtensionProperties;

		protected virtual IPropertySet VirtualExtensionProperties =>
			throw new NotImplementedException("Implement in derived class");

		IRow ITable.CreateRow() => VirtualCreateRow();

		public IFeature CreateFeature() => (IFeature) VirtualCreateRow();

		protected virtual IRow VirtualCreateRow() =>
			throw new NotImplementedException("Implement in derived class");

		IRow ITable.GetRow(int OID) => VirtualGetRow(OID);

		public IFeature GetFeature(int OID) => (IFeature) VirtualGetRow(OID);

		protected virtual IRow VirtualGetRow(int OID) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.GetRows(object oids, bool Recycling) => VirtualGetRows(oids, Recycling);

		public IFeatureCursor GetFeatures(object oids, bool Recycling) =>
			(IFeatureCursor) VirtualGetRows(oids, Recycling);

		protected virtual ICursor VirtualGetRows(object oids, bool Recycling) =>
			throw new NotImplementedException("Implement in derived class");

		IRowBuffer ITable.CreateRowBuffer() => VirtualCreateRowBuffer();

		public IFeatureBuffer CreateFeatureBuffer() => (IFeatureBuffer) VirtualCreateRowBuffer();

		protected virtual IRowBuffer VirtualCreateRowBuffer() =>
			throw new NotImplementedException("Implement in derived class");

		void ITable.UpdateSearchedRows(IQueryFilter QueryFilter, IRowBuffer buffer) =>
			VirtualUpdateSearchedRows(QueryFilter, buffer);

		protected virtual void
			VirtualUpdateSearchedRows(IQueryFilter QueryFilter, IRowBuffer buffer) =>
			throw new NotImplementedException("Implement in derived class");

		void ITable.DeleteSearchedRows(IQueryFilter QueryFilter) =>
			VirtualDeleteSearchedRows(QueryFilter);

		protected virtual void VirtualDeleteSearchedRows(IQueryFilter QueryFilter) =>
			throw new NotImplementedException("Implement in derived class");

		int ITable.RowCount(IQueryFilter QueryFilter) => VirtualRowCount(QueryFilter);

		public int FeatureCount(IQueryFilter QueryFilter) => VirtualRowCount(QueryFilter);

		protected virtual int VirtualRowCount(IQueryFilter QueryFilter) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.Search(IQueryFilter QueryFilter, bool Recycling) =>
			VirtualSearch(QueryFilter, Recycling);

		public IFeatureCursor Search(IQueryFilter queryFilter, bool recycling) =>
			VirtualSearch(queryFilter, recycling);

		protected virtual CursorImpl VirtualSearch(IQueryFilter queryFilter, bool recycling) =>
			new CursorImpl(this, VirtualEnumRows(queryFilter, recycling));

		protected virtual IEnumerable<IRow>
			VirtualEnumRows(IQueryFilter queryFilter, bool recycling) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.Update(IQueryFilter QueryFilter, bool Recycling) =>
			VirtualUpdate(QueryFilter, Recycling);

		public IFeatureCursor Update(IQueryFilter QueryFilter, bool Recycling) =>
			(IFeatureCursor) VirtualUpdate(QueryFilter, Recycling);

		protected virtual ICursor VirtualUpdate(IQueryFilter QueryFilter, bool Recycling) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.Insert(bool useBuffering) => VirtualInsert(useBuffering);

		public IFeatureCursor Insert(bool useBuffering) =>
			(IFeatureCursor) VirtualInsert(useBuffering);

		protected virtual ICursor VirtualInsert(bool useBuffering) =>
			throw new NotImplementedException("Implement in derived class");

		ISelectionSet ITable.Select(IQueryFilter QueryFilter, esriSelectionType selType,
		                            esriSelectionOption selOption, IWorkspace selectionContainer) =>
			VirtualSelect(QueryFilter, selType, selOption, selectionContainer);

		public ISelectionSet Select(IQueryFilter QueryFilter, esriSelectionType selType,
		                            esriSelectionOption selOption, IWorkspace selectionContainer) =>
			VirtualSelect(QueryFilter, selType, selOption, selectionContainer);

		protected virtual ISelectionSet VirtualSelect(IQueryFilter QueryFilter,
		                                              esriSelectionType selType,
		                                              esriSelectionOption selOption,
		                                              IWorkspace selectionContainer) =>
			throw new NotImplementedException("Implement in derived class");

		int IObjectClass.ObjectClassID => VirtualObjectClassID;
		public int ObjectClassID => VirtualObjectClassID;

		protected virtual int VirtualObjectClassID =>
			throw new NotImplementedException("Implement in derived class");

		IEnumRelationshipClass IObjectClass.get_RelationshipClasses(esriRelRole role) =>
			Virtualget_RelationshipClasses(role);

		public IEnumRelationshipClass get_RelationshipClasses(esriRelRole role) =>
			Virtualget_RelationshipClasses(role);

		protected virtual IEnumRelationshipClass Virtualget_RelationshipClasses(esriRelRole role) =>
			throw new NotImplementedException("Implement in derived class");

		string IObjectClass.AliasName => VirtualAliasName;
		public string AliasName => VirtualAliasName;

		protected virtual string VirtualAliasName =>
			throw new NotImplementedException("Implement in derived class");

		void ISchemaLock.ChangeSchemaLock(esriSchemaLock schemaLock) =>
			VirtualChangeSchemaLock(schemaLock);

		protected virtual void VirtualChangeSchemaLock(esriSchemaLock schemaLock) =>
			throw new NotImplementedException("Implement in derived class");

		void ISchemaLock.GetCurrentSchemaLocks(out IEnumSchemaLockInfo schemaLockInfo) =>
			VirtualGetCurrentSchemaLocks(out schemaLockInfo);

		protected virtual void
			VirtualGetCurrentSchemaLocks(out IEnumSchemaLockInfo schemaLockInfo) =>
			throw new NotImplementedException("Implement in derived class");

		public esriFeatureType FeatureType => VirtualFeatureType;

		protected virtual esriFeatureType VirtualFeatureType =>
			throw new NotImplementedException("Implement in derived class");

		public IFeatureDataset FeatureDataset => VirtualFeatureDataset;

		protected virtual IFeatureDataset VirtualFeatureDataset =>
			throw new NotImplementedException("Implement in derived class");

		public int FeatureClassID => VirtualFeatureClassID;

		protected virtual int VirtualFeatureClassID =>
			throw new NotImplementedException("Implement in derived class");

		public virtual esriGeometryType ShapeType => VirtualShapeType;

		protected virtual esriGeometryType VirtualShapeType =>
			throw new NotImplementedException("Implement in derived class");

		public string ShapeFieldName => VirtualShapeFieldName;
		protected virtual string VirtualShapeFieldName => "Shape";
		public IField AreaField => VirtualAreaField;
		protected virtual IField VirtualAreaField => VirtualFields.Field[VirtualFindField("Area")];

		public IField LengthField => VirtualLengthField;

		protected virtual IField VirtualLengthField =>
			VirtualFields.Field[VirtualFindField("Length")];

		public ISpatialReference SpatialReference => VirtualSpatialReference;

		protected virtual ISpatialReference VirtualSpatialReference =>
			throw new NotImplementedException("Implement in derived class");

		public IEnvelope Extent => VirtualExtent;

		protected virtual IEnvelope VirtualExtent =>
			throw new NotImplementedException("Implement in derived class");

		private class GdbTableName : IName, IDatasetName, IObjectClassName, ITableName
		{
			private readonly VirtualTable _table;

			public GdbTableName(VirtualTable table)
			{
				_table = table;
				IDataset ds = table;
				Name = ds.Name;

				IWorkspace workspace = ds.Workspace;
				WorkspaceName = (IWorkspaceName) ((IDataset) workspace).FullName;
			}

			#region IName members

			public object Open()
			{
				return _table;
			}

			public string NameString { get; set; }

			#endregion

			#region IDatasetName members

			public string Name { get; set; }

			public esriDatasetType Type => ((IDataset) _table).Type;

			public string Category { get; set; }

			public IWorkspaceName WorkspaceName { get; set; }

			public IEnumDatasetName SubsetNames => throw new NotImplementedException();

			#endregion

			public int ObjectClassID => _table.ObjectClassID;
		}
	}
}

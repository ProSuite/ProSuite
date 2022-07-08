using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public abstract class VirtualFeatureClass : VirtualTable, IFeatureClass, IGeoDataset
	{
		protected VirtualFeatureClass(string name) : base(name) { }
	}

	public abstract class VirtualTable : IDataset, ITable, IObjectClass, IDatasetEdit, ISchemaLock,
	                                     ISubtypes, IReadOnlyTable, IRowCreator<VirtualRow>
	{
		protected GdbFields _fields;
		private TableName _tableName;
		protected GdbFields GdbFields => _fields ?? (_fields = new GdbFields());
		private string _name;

		protected VirtualTable(string name)
		{
			_name = name;
		}

		public override string ToString() => $"{Name} ({base.ToString()})";

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

		public virtual void Rename(string name)
		{
			_name = name;
		}

		string IDataset.Name => Name;
		public virtual string Name => _name;

		IName IDataset.FullName => FullName;

		public virtual IName FullName =>
			_tableName ?? (_tableName = new TableName(this));

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

		esriDatasetType IDataset.Type => DatasetType;
		public virtual esriDatasetType DatasetType => esriDatasetType.esriDTTable;

		string IDataset.Category => Category;

		public virtual string Category =>
			throw new NotImplementedException("Implement in derived class");

		IEnumDataset IDataset.Subsets => Subsets;

		public virtual IEnumDataset Subsets =>
			throw new NotImplementedException("Implement in derived class");

		IWorkspace IDataset.Workspace => Workspace;

		public virtual IWorkspace Workspace =>
			throw new NotImplementedException("Implement in derived class");

		IPropertySet IDataset.PropertySet => PropertySet;

		public virtual IPropertySet PropertySet =>
			throw new NotImplementedException("Implement in derived class");

		int IClass.FindField(string name) => FindField(name);

		int IObjectClass.FindField(string name) => FindField(name);

		int ITable.FindField(string name) => FindField(name);

		public virtual int FindField(string name) => GdbFields.FindField(name);

		void IClass.AddField(IField Field) => AddFieldT(Field);

		void IObjectClass.AddField(IField Field) => AddFieldT(Field);

		void ITable.AddField(IField Field) => AddFieldT(Field);

		public void AddField(IField Field) => AddFieldT(Field);

		public virtual int AddFieldT(IField Field) => GdbFields.AddField(Field);

		void IClass.DeleteField(IField Field) => DeleteField(Field);

		void IObjectClass.DeleteField(IField Field) => DeleteField(Field);

		void ITable.DeleteField(IField Field) => DeleteField(Field);

		public virtual void DeleteField(IField Field) =>
			throw new NotImplementedException("Implement in derived class");

		void IClass.AddIndex(IIndex Index) => AddIndex(Index);

		void IObjectClass.AddIndex(IIndex Index) => AddIndex(Index);

		void ITable.AddIndex(IIndex Index) => AddIndex(Index);

		public virtual void AddIndex(IIndex Index) =>
			throw new NotImplementedException("Implement in derived class");

		void IClass.DeleteIndex(IIndex Index) => DeleteIndex(Index);

		void IObjectClass.DeleteIndex(IIndex Index) => DeleteIndex(Index);

		void ITable.DeleteIndex(IIndex Index) => DeleteIndex(Index);

		public virtual void DeleteIndex(IIndex Index) =>
			throw new NotImplementedException("Implement in derived class");

		IFields IClass.Fields => Fields;
		IFields IObjectClass.Fields => Fields;
		IFields ITable.Fields => Fields;
		public virtual IFields Fields => GdbFields;

		IIndexes IClass.Indexes => Indexes;
		IIndexes IObjectClass.Indexes => Indexes;
		IIndexes ITable.Indexes => Indexes;

		public virtual IIndexes Indexes =>
			throw new NotImplementedException("Implement in derived class");

		bool IClass.HasOID => HasOID;
		bool IObjectClass.HasOID => HasOID;
		bool ITable.HasOID => HasOID;

		public virtual bool HasOID =>
			throw new NotImplementedException("Implement in derived class");

		string IClass.OIDFieldName => OIDFieldName;
		string IObjectClass.OIDFieldName => OIDFieldName;
		string ITable.OIDFieldName => OIDFieldName;

		public virtual string OIDFieldName =>
			throw new NotImplementedException("Implement in derived class");

		UID IClass.CLSID => CLSID;
		UID IObjectClass.CLSID => CLSID;
		UID ITable.CLSID => CLSID;

		public virtual UID CLSID =>
			throw new NotImplementedException("Implement in derived class");

		UID IClass.EXTCLSID => EXTCLSID;
		UID IObjectClass.EXTCLSID => EXTCLSID;
		UID ITable.EXTCLSID => EXTCLSID;

		public virtual UID EXTCLSID =>
			throw new NotImplementedException("Implement in derived class");

		object IClass.Extension => Extension;
		object IObjectClass.Extension => Extension;
		object ITable.Extension => Extension;

		public virtual object Extension =>
			throw new NotImplementedException("Implement in derived class");

		IPropertySet IClass.ExtensionProperties => ExtensionProperties;
		IPropertySet IObjectClass.ExtensionProperties => ExtensionProperties;
		IPropertySet ITable.ExtensionProperties => ExtensionProperties;

		public virtual IPropertySet ExtensionProperties =>
			throw new NotImplementedException("Implement in derived class");

		IRow ITable.CreateRow() => CreateRow();

		public IFeature CreateFeature() => (IFeature) CreateRow();

		public virtual VirtualRow CreateRow() =>
			throw new NotImplementedException("Implement in derived class");

		IRow ITable.GetRow(int OID) => GetRow(OID);

		public IFeature GetFeature(int OID) => (IFeature) GetRow(OID);

		public virtual IRow GetRow(int OID) =>
			throw new NotImplementedException("Implement in derived class");

		IReadOnlyRow IReadOnlyTable.GetRow(int OID) => GetReadOnlyRow(OID);

		public virtual IReadOnlyRow GetReadOnlyRow(int OID) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.GetRows(object oids, bool Recycling) => GetRows(oids, Recycling);

		public IFeatureCursor GetFeatures(object oids, bool Recycling) =>
			(IFeatureCursor) GetRows(oids, Recycling);

		public virtual ICursor GetRows(object oids, bool Recycling)
		{
			if (! (oids is IEnumerable<int> oidList))
			{
				throw new InvalidOperationException(
					$"Cannot convert oids ({oids})to IEnumerable<int>");
			}

			return new CursorImpl(this, oidList.Select(oid => GetRow(oid)));
		}

		IRowBuffer ITable.CreateRowBuffer() => CreateRowBuffer();

		public IFeatureBuffer CreateFeatureBuffer() => (IFeatureBuffer) CreateRowBuffer();

		public virtual IRowBuffer CreateRowBuffer() =>
			throw new NotImplementedException("Implement in derived class");

		void ITable.UpdateSearchedRows(IQueryFilter QueryFilter, IRowBuffer buffer) =>
			UpdateSearchedRows(QueryFilter, buffer);

		public virtual void UpdateSearchedRows(IQueryFilter QueryFilter, IRowBuffer buffer) =>
			throw new NotImplementedException("Implement in derived class");

		void ITable.DeleteSearchedRows(IQueryFilter QueryFilter) =>
			DeleteSearchedRows(QueryFilter);

		public virtual void DeleteSearchedRows(IQueryFilter QueryFilter) =>
			throw new NotImplementedException("Implement in derived class");

		int ITable.RowCount(IQueryFilter QueryFilter) => RowCount(QueryFilter);

		public bool Equals(IReadOnlyTable otherTable)
		{
			VirtualTable other = otherTable as VirtualTable;

			if (other == null)
			{
				return false;
			}

			if (! Equals(Workspace, other.Workspace))
			{
				return false;
			}

			if (ObjectClassID >= 0 && other.ObjectClassID >= 0 &&
			    ObjectClassID == other.ObjectClassID)
			{
				return true;
			}

			// Same workspace but negative class ids:
			return Name.Equals(other.Name);
		}

		public int FeatureCount(IQueryFilter QueryFilter) => RowCount(QueryFilter);

		public virtual int RowCount(IQueryFilter QueryFilter) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.Search(IQueryFilter QueryFilter, bool Recycling) =>
			SearchT(QueryFilter, Recycling);

		public virtual IFeatureCursor Search(IQueryFilter queryFilter, bool recycling) =>
			SearchT(queryFilter, recycling);

		public virtual CursorImpl SearchT(IQueryFilter queryFilter, bool recycling) =>
			new CursorImpl(this, EnumRows(queryFilter, recycling));

		public virtual IEnumerable<IRow>
			EnumRows(IQueryFilter queryFilter, bool recycling) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumerable<IReadOnlyRow> IReadOnlyTable.EnumRows(IQueryFilter filter, bool recycling)
			=> EnumReadOnlyRows(filter, recycling);

		public virtual IEnumerable<IReadOnlyRow>
			EnumReadOnlyRows(IQueryFilter queryFilter, bool recycling) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.Update(IQueryFilter QueryFilter, bool Recycling) =>
			UpdateT(QueryFilter, Recycling);

		public virtual IFeatureCursor Update(IQueryFilter QueryFilter, bool Recycling) =>
			(IFeatureCursor) UpdateT(QueryFilter, Recycling);

		public virtual ICursor UpdateT(IQueryFilter QueryFilter, bool Recycling) =>
			throw new NotImplementedException("Implement in derived class");

		ICursor ITable.Insert(bool useBuffering) => InsertT(useBuffering);

		public IFeatureCursor Insert(bool useBuffering) =>
			(IFeatureCursor) InsertT(useBuffering);

		public virtual ICursor InsertT(bool useBuffering) =>
			throw new NotImplementedException("Implement in derived class");

		ISelectionSet ITable.Select(IQueryFilter QueryFilter, esriSelectionType selType,
		                            esriSelectionOption selOption, IWorkspace selectionContainer) =>
			Select(QueryFilter, selType, selOption, selectionContainer);

		public virtual ISelectionSet Select(IQueryFilter QueryFilter,
		                                    esriSelectionType selType,
		                                    esriSelectionOption selOption,
		                                    IWorkspace selectionContainer) =>
			throw new NotImplementedException("Implement in derived class");

		int IObjectClass.ObjectClassID => ObjectClassID;

		public virtual int ObjectClassID =>
			throw new NotImplementedException("Implement in derived class");

		IEnumRelationshipClass IObjectClass.get_RelationshipClasses(esriRelRole role) =>
			get_RelationshipClasses(role);

		public virtual IEnumRelationshipClass get_RelationshipClasses(esriRelRole role) =>
			throw new NotImplementedException("Implement in derived class");

		string IObjectClass.AliasName => AliasName;

		public virtual string AliasName =>
			throw new NotImplementedException("Implement in derived class");

		void ISchemaLock.ChangeSchemaLock(esriSchemaLock schemaLock) =>
			ChangeSchemaLock(schemaLock);

		public virtual void ChangeSchemaLock(esriSchemaLock schemaLock) =>
			throw new NotImplementedException("Implement in derived class");

		void ISchemaLock.GetCurrentSchemaLocks(out IEnumSchemaLockInfo schemaLockInfo) =>
			GetCurrentSchemaLocks(out schemaLockInfo);

		public virtual void GetCurrentSchemaLocks(out IEnumSchemaLockInfo schemaLockInfo) =>
			throw new NotImplementedException("Implement in derived class");

		public virtual esriFeatureType FeatureType =>
			throw new NotImplementedException("Implement in derived class");

		public virtual IFeatureDataset FeatureDataset =>
			throw new NotImplementedException("Implement in derived class");

		public virtual int FeatureClassID =>
			throw new NotImplementedException("Implement in derived class");

		public virtual esriGeometryType ShapeType => GeometryDef.GeometryType;

		public virtual IGeometryDef GeometryDef =>
			Fields.Field[FindField(ShapeFieldName)].GeometryDef;

		public virtual string ShapeFieldName => "Shape";
		public virtual IField AreaField => Fields.Field[FindField("Area")];

		public virtual IField LengthField =>
			Fields.Field[FindField("Length")];

		private ISpatialReference _sr;

		public virtual ISpatialReference SpatialReference
		{
			get => _sr ?? GeometryDef.SpatialReference;
			set => _sr = value;
		}

		public virtual IEnvelope Extent =>
			throw new NotImplementedException("Implement in derived class");

		bool IDatasetEdit.IsBeingEdited() => IsBeingEdited();

		public virtual bool IsBeingEdited()
			=> throw new NotImplementedException("Implement in derived class");

		void ISubtypes.AddSubtype(int SubtypeCode, string SubtypeName)
			=> AddSubtype(SubtypeCode, SubtypeName);

		public virtual void AddSubtype(int SubtypeCode, string SubtypeName)
		{
			throw new NotImplementedException("Implement in derived class");
		}

		void ISubtypes.DeleteSubtype(int SubtypeCode) => DeleteSubtype(SubtypeCode);

		public virtual void DeleteSubtype(int SubtypeCode)
		{
			throw new NotImplementedException("Implement in derived class");
		}

		bool ISubtypes.HasSubtype => HasSubtype;

		public virtual bool HasSubtype =>
			throw new NotImplementedException("Implement in derived class");

		int ISubtypes.DefaultSubtypeCode
		{
			get => DefaultSubtypeCode;
			set => DefaultSubtypeCode = value;
		}

		public virtual int DefaultSubtypeCode
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		object ISubtypes.get_DefaultValue(int SubtypeCode, string FieldName) =>
			get_DefaultValue(SubtypeCode, FieldName);

		void ISubtypes.set_DefaultValue(int SubtypeCode, string FieldName, object Value) =>
			set_DefaultValue(SubtypeCode, FieldName, Value);

		public virtual object get_DefaultValue(int SubtypeCode, string FieldName) =>
			throw new NotImplementedException("Implement in derived class");

		public virtual void set_DefaultValue(int SubtypeCode, string FieldName, object Value) =>
			throw new NotImplementedException("Implement in derived class");

		public virtual object DefaultValue
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		IDomain ISubtypes.get_Domain(int SubtypeCode, string FieldName) =>
			get_Domain(SubtypeCode, FieldName);

		void ISubtypes.set_Domain(int SubtypeCode, string FieldName, IDomain Domain) =>
			set_Domain(SubtypeCode, FieldName, Domain);

		public virtual IDomain get_Domain(int SubtypeCode, string FieldName)
			=> throw new NotImplementedException("Implement in derived class");

		public virtual void set_Domain(int SubtypeCode, string FieldName, IDomain Domain)
			=> throw new NotImplementedException("Implement in derived class");

		string ISubtypes.SubtypeFieldName
		{
			get => SubtypeFieldName;
			set => SubtypeFieldName = value;
		}

		public virtual string SubtypeFieldName
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		int ISubtypes.SubtypeFieldIndex => SubtypeFieldIndex;

		public virtual int SubtypeFieldIndex =>
			throw new NotImplementedException("Implement in derived class");

		string ISubtypes.get_SubtypeName(int subtypeCode) => get_SubtypeName(subtypeCode);

		public virtual string get_SubtypeName(int subtypeCode) =>
			throw new NotImplementedException("Implement in derived class");

		IEnumSubtype ISubtypes.Subtypes => Subtypes;

		public virtual IEnumSubtype Subtypes =>
			throw new NotImplementedException("Implement in derived class");

		protected class TableName : IName, IDatasetName, IObjectClassName, ITableName
		{
			private readonly VirtualTable _table;

			public TableName(VirtualTable table)
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

			public virtual IEnumDatasetName SubsetNames =>
				throw new NotImplementedException("implement in derived class");

			#endregion

			public int ObjectClassID => _table.ObjectClassID;
		}
	}
}

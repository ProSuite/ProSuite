using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.GeoDb;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class ObjectClassMock : IObjectClass, ITable, IDataset, ISubtypes, IDatasetEdit,
	                               IReadOnlyTable, ITableData
	{
		private static int _nextObjectClassId;

		private readonly FieldsMock _fieldsMock = new FieldsMock();
		private const string _oidFieldName = "OBJECTID";
		private const bool _hasOID = true;
		private readonly string _name;
		private int _lastUsedOID;
		private IWorkspace _workspaceMock;

		public ObjectClassMock(int objectClassId, string name)
			: this(name, name, objectClassId) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectClassMock"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="aliasName">The alias name of the object class.</param>
		/// <param name="objectClassId">The object class id.</param>
		public ObjectClassMock(string name, string aliasName, int? objectClassId)
		{
			ObjectClassID = objectClassId ?? _nextObjectClassId++;
			_name = name;
			AliasName = aliasName;

			_fieldsMock.AddFields(FieldUtils.CreateOIDField(_oidFieldName));
		}

		public int? RowCountResult { set; get; }

		protected int GetNextOID()
		{
			return ++_lastUsedOID;
		}

		public virtual IObject CreateObject(int oid)
		{
			return new ObjectMock(oid, this);
		}

		public void AddFields(params IField[] fields)
		{
			_fieldsMock.AddFields(fields);
		}

		public void AddField(string name, esriFieldType type)
		{
			IFieldEdit fieldEdit = new FieldClass();

			fieldEdit.Name_2 = name;
			fieldEdit.Type_2 = type;

			AddField(fieldEdit);
		}

		#region Implementation of IDbDataset

		string IDatasetDef.Name => _name;

		IDatasetContainer IDatasetDef.DbContainer => new GeoDbWorkspace(_workspaceMock);

		DatasetType IDatasetDef.DatasetType =>
			((IDataset) this).Type == esriDatasetType.esriDTFeatureClass
				? DatasetType.FeatureClass
				: DatasetType.Table;

		bool IDatasetDef.Equals(IDatasetDef otherDataset)
		{
			return Equals(otherDataset);
		}

		#endregion

		#region Implementation of IDbTableSchema

		IReadOnlyList<ITableField> ITableSchemaDef.TableFields =>
			_fieldsMock.FieldList
			           .Select(FieldUtils.ToTableField)
			           .ToList();

		#endregion

		#region Implementation of IDbTable

		IDbRow ITableData.GetRow(long oid)
		{
			throw new NotImplementedException();
		}

		IEnumerable<IDbRow> ITableData.EnumRows(ITableFilter filter, bool recycle)
		{
			throw new NotImplementedException();
		}

		long ITableData.RowCount(ITableFilter filter)
		{
			throw new NotImplementedException();
		}

		#endregion

		public int FindField(string name)
		{
			return _fieldsMock.FindField(name);
		}

		public void AddField(IField Field)
		{
			_fieldsMock.AddFields(Field);
		}

		public void DeleteField(IField field)
		{
			throw new NotImplementedException();
		}

		public void AddIndex(IIndex index)
		{
			throw new NotImplementedException();
		}

		public void DeleteIndex(IIndex index)
		{
			throw new NotImplementedException();
		}

		public IFields Fields => _fieldsMock;

		public IIndexes Indexes
		{
			get { throw new NotImplementedException(); }
		}

		public bool HasOID => _hasOID;

		public string OIDFieldName => _oidFieldName;

		public UID CLSID
		{
			get { throw new NotImplementedException(); }
		}

		public UID EXTCLSID
		{
			get { throw new NotImplementedException(); }
		}

		public object Extension
		{
			get { throw new NotImplementedException(); }
		}

		public IPropertySet ExtensionProperties
		{
			get { throw new NotImplementedException(); }
		}

		public int ObjectClassID { get; }

		public string AliasName { get; }

		public IEnumRelationshipClass get_RelationshipClasses(esriRelRole Role)
		{
			throw new NotImplementedException();
		}

		#region IDataset Members

		bool IDataset.CanCopy()
		{
			return false;
		}

		IDataset IDataset.Copy(string copyName, IWorkspace copyWorkspace)
		{
			throw new NotImplementedException();
		}

		bool IDataset.CanDelete()
		{
			return false;
		}

		void IDataset.Delete()
		{
			throw new NotImplementedException();
		}

		bool IDataset.CanRename()
		{
			return false;
		}

		void IDataset.Rename(string name)
		{
			throw new NotImplementedException();
		}

		string IDataset.Name => _name;
		string IReadOnlyDataset.Name => _name;

		IName IDataset.FullName => null;
		IName IReadOnlyDataset.FullName => null;

		string IDataset.BrowseName
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		esriDatasetType IDataset.Type => GetDatasetType();

		string IDataset.Category
		{
			get { throw new NotImplementedException(); }
		}

		IEnumDataset IDataset.Subsets
		{
			get { throw new NotImplementedException(); }
		}

		IWorkspace IDataset.Workspace => _workspaceMock;
		IWorkspace IReadOnlyDataset.Workspace => _workspaceMock;

		internal void SetWorkspace(WorkspaceMock workspaceMock)
		{
			_workspaceMock = workspaceMock;
		}

		IPropertySet IDataset.PropertySet
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region ITable members

		IRow ITable.CreateRow()
		{
			return CreateObject(GetNextOID());
		}

#if Server11
		IRow ITable.GetRow(long OID) => throw new NotImplementedException();

		public IFeature GetFeature(long OID) => throw new NotImplementedException();
#else
		IRow ITable.GetRow(int OID) => throw new NotImplementedException();

		public IFeature GetFeature(int OID) => throw new NotImplementedException();
#endif

		IReadOnlyRow IReadOnlyTable.GetRow(long OID) => throw new NotImplementedException();

		ICursor ITable.GetRows(object oids, bool Recycling)
		{
			throw new NotImplementedException();
		}

		IRowBuffer ITable.CreateRowBuffer()
		{
			throw new NotImplementedException();
		}

		void ITable.UpdateSearchedRows(IQueryFilter QueryFilter, IRowBuffer buffer)
		{
			throw new NotImplementedException();
		}

		void ITable.DeleteSearchedRows(IQueryFilter QueryFilter)
		{
			throw new NotImplementedException();
		}

		long IReadOnlyTable.RowCount(ITableFilter filter) =>
			RowCount(TableFilterUtils.GetQueryFilter(filter));

#if Server11
		long ITable.RowCount(IQueryFilter QueryFilter) => RowCount(QueryFilter);
#else
		int ITable.RowCount(IQueryFilter QueryFilter) => RowCount(QueryFilter);
#endif

		private int RowCount(IQueryFilter QueryFilter)
		{
			if (RowCountResult.HasValue)
			{
				return RowCountResult.Value;
			}

			throw new InvalidOperationException("No row count result specified for mock");
		}

		IEnumerable<IReadOnlyRow> IReadOnlyTable.EnumRows(ITableFilter filter, bool recycle)
		{
			IQueryFilter queryFilter = null;
			try
			{
				queryFilter = TableFilterUtils.GetQueryFilter(filter);
				foreach (var row in new EnumCursor(this, queryFilter, recycle))
				{
					if (row is IReadOnlyRow roRow)
					{
						yield return roRow;
					}
					else
					{
						throw new NotImplementedException();
					}
				}
			}
			finally
			{
				if (queryFilter != null)
				{
					Marshal.ReleaseComObject(queryFilter);
				}
			}
		}

		ICursor ITable.Search(IQueryFilter QueryFilter, bool Recycling) =>
			Search(QueryFilter, Recycling);

		protected virtual ICursor Search(IQueryFilter QueryFilter, bool Recycling)
			=> throw new NotImplementedException();

		ICursor ITable.Update(IQueryFilter QueryFilter, bool Recycling)
		{
			throw new NotImplementedException();
		}

		ICursor ITable.Insert(bool useBuffering)
		{
			throw new NotImplementedException();
		}

		ISelectionSet ITable.Select(IQueryFilter QueryFilter, esriSelectionType selType,
		                            esriSelectionOption selOption,
		                            IWorkspace selectionContainer)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Non-public members

		protected virtual esriDatasetType GetDatasetType()
		{
			return esriDatasetType.esriDTTable;
		}

		//private IField CreateOIDField()
		//{
		//    IFieldEdit field = new FieldClass();
		//    field.IsNullable_2 = false;
		//    field.Editable_2 = false;
		//    field.Name_2 = _oidFieldName;
		//    field.Required_2 = true;
		//    field.Type_2 = esriFieldType.esriFieldTypeOID;

		//    return field;
		//}

		#endregion

		#region ISubtypes Members

		public void AddSubtype(int SubtypeCode, string SubtypeName)
		{
			throw new NotImplementedException();
		}

		public void DeleteSubtype(int SubtypeCode)
		{
			throw new NotImplementedException();
		}

		public bool HasSubtype => false;

		public int DefaultSubtypeCode
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string SubtypeFieldName
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int SubtypeFieldIndex
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumSubtype Subtypes
		{
			get { throw new NotImplementedException(); }
		}

		public object get_DefaultValue(int SubtypeCode, string FieldName)
		{
			throw new NotImplementedException();
		}

		public void set_DefaultValue(int SubtypeCode, string FieldName, object Value)
		{
			throw new NotImplementedException();
		}

		public IDomain get_Domain(int SubtypeCode, string FieldName)
		{
			throw new NotImplementedException();
		}

		public void set_Domain(int SubtypeCode, string FieldName, IDomain Domain)
		{
			throw new NotImplementedException();
		}

		public string get_SubtypeName(int SubtypeCode)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDatasetEdit Member

		public bool IsBeingEdited()
		{
			return _workspaceMock == null ||
			       ((IWorkspaceEdit2) _workspaceMock).IsBeingEdited();
		}

		#endregion

		public bool Equals(IReadOnlyTable otherTable)
		{
			return Equals(otherTable as IObjectClass);
		}

		protected bool Equals(ObjectClassMock other)
		{
			if (! Equals(_workspaceMock, other._workspaceMock))
				return false;
			if (ObjectClassID != other.ObjectClassID)
				return false;
			if (_workspaceMock == null && ! ReferenceEquals(this, other))
				return false;
			return true;
		}

		public bool Equals(IObjectClass other)
		{
			if (other == null)
			{
				return false;
			}

			if (other is ObjectClassMock otherMock)
			{
				return Equals(otherMock);
			}

			return ObjectClassID == other.ObjectClassID &&
			       WorkspaceUtils.IsSameWorkspace(_workspaceMock, ((IDataset) other).Workspace,
			                                      WorkspaceComparison.Exact);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ObjectClassMock) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_workspaceMock != null ? _workspaceMock.GetHashCode() : 0) * 397) ^
				       ObjectClassID;
			}
		}
	}
}

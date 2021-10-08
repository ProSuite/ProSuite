using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Gdb IObjectClass implementation that can be instantiated in memory that typically
	/// represents an existing table or feature class on the client. Its parent workspace
	/// can be null, a fake <see cref="GdbWorkspace"/> or a real workspace. An optionally
	/// provided  <see cref="BackingDataset"/> allows for actual data-access, such as GetRow()
	/// or Search().
	/// </summary>
	public class GdbTable : IObjectClass, ITable, IDataset, ISubtypes, IDatasetEdit,
	                        IEquatable<IObjectClass>
	{
		// TODO: Extra interfaces IGdbTable, IGdbFeatureClass, etc. in Commons with all the relevant properties
		//private const string _defaultOidFieldName = "OBJECTID";

		private readonly GdbFields _gdbFields = new GdbFields();
		private int _lastUsedOid;
		private readonly IWorkspace _workspace;

		private IName _fullName;

		/// <summary>
		///     Initializes a new instance of the <see cref="GdbTable" /> class.
		/// </summary>
		/// <param name="objectClassId">The object class id.</param>
		/// <param name="name">The name.</param>
		/// <param name="aliasName">The alias name of the object class.</param>
		/// <param name="createBackingDataset">The factory method that creates the backing dataset.</param>
		/// <param name="workspace"></param>
		public GdbTable(int objectClassId,
		                [NotNull] string name,
		                [CanBeNull] string aliasName = null,
		                [CanBeNull] Func<ITable, BackingDataset> createBackingDataset = null,
		                [CanBeNull] IWorkspace workspace = null)
		{
			ObjectClassID = objectClassId;
			Name = name;
			AliasName = aliasName;

			_workspace = workspace;

			if (createBackingDataset == null)
			{
				BackingDataset = new InMemoryDataset(this, new List<IRow>(0));
			}
			else
			{
				BackingDataset = createBackingDataset(this);
			}
		}

		[CanBeNull]
		public BackingDataset BackingDataset { get; }

		#region Non-public members

		protected int GetNextOid()
		{
			return ++_lastUsedOid;
		}

		protected virtual IObject CreateObject(int oid)
		{
			return new GdbRow(oid, this);
		}

		protected virtual esriDatasetType GetDatasetType()
		{
			return esriDatasetType.esriDTTable;
		}

		protected virtual void FieldAddedCore(IField field) { }

		#endregion

		public bool Equals(GdbTable other)
		{
			return Equals(_workspace, other._workspace) &&
			       ObjectClassID == other.ObjectClassID;
		}

		public bool Equals(IObjectClass other)
		{
			if (other == null) return false;

			if (other is GdbTable otherGdbTable && otherGdbTable._workspace == _workspace)
			{
				// Allow both workspaces to be null and hence equal!
				return ObjectClassID == other.ObjectClassID;
			}

			return ObjectClassID == other.ObjectClassID &&
			       WorkspaceUtils.IsSameWorkspace(_workspace, ((IDataset) other).Workspace,
			                                      WorkspaceComparison.Exact);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			if (ReferenceEquals(this, obj)) return true;

			if (obj.GetType() != GetType()) return false;

			return Equals((GdbTable) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_workspace != null ? _workspace.GetHashCode() : 0) * 397) ^
				       ObjectClassID;
			}
		}

		#region IDatasetEdit Member

		public bool IsBeingEdited()
		{
			return ! (_workspace is IWorkspaceEdit workspaceEdit) || workspaceEdit.IsBeingEdited();
		}

		#endregion

		#region IClass members

		public int FindField(string name)
		{
			return _gdbFields.FindField(name);
		}

		public void AddField(IField field)
		{
			_gdbFields.AddFields(field);

			if (field.Type == esriFieldType.esriFieldTypeOID)
			{
				// Probably the same logic as AO (query) classes:
				// The last one to be added determines the OID field
				HasOID = true;
				OIDFieldName = field.Name;
			}

			FieldAddedCore(field);
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

		public IFields Fields => _gdbFields;

		public IIndexes Indexes => throw new NotImplementedException();

		public bool HasOID { get; private set; }

		public string OIDFieldName { get; private set; }

		public UID CLSID => throw new NotImplementedException();

		public UID EXTCLSID => throw new NotImplementedException();

		public object Extension => throw new NotImplementedException();

		public IPropertySet ExtensionProperties => throw new NotImplementedException();

		#endregion

		#region IObjectClass members

		public int ObjectClassID { get; }

		public string AliasName { get; }

		public IEnumRelationshipClass get_RelationshipClasses(esriRelRole role)
		{
			throw new NotImplementedException();
		}

		#endregion

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

		[NotNull]
		public string Name { get; }

		IName IDataset.FullName
		{
			get
			{
				if (_fullName == null)
				{
					_fullName = new GdbTableName(this);
				}

				return _fullName;
			}
		}

		string IDataset.BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => GetDatasetType();

		string IDataset.Category => throw new NotImplementedException();

		IEnumDataset IDataset.Subsets => throw new NotImplementedException();

		IWorkspace IDataset.Workspace => _workspace;

		IPropertySet IDataset.PropertySet => throw new NotImplementedException();

		#endregion

		#region ITable members

		public IRow CreateRow()
		{
			return CreateObject(GetNextOid());
		}

		public IRow GetRow(int id)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for GetRow().");
			}

			return BackingDataset?.GetRow(id);
		}

		ICursor ITable.GetRows(object oids, bool recycling)
		{
			throw new NotImplementedException();
		}

		IRowBuffer ITable.CreateRowBuffer()
		{
			return CreateRow();
		}

		void ITable.UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer)
		{
			throw new NotImplementedException();
		}

		void ITable.DeleteSearchedRows(IQueryFilter queryFilter)
		{
			throw new NotImplementedException();
		}

		public int RowCount(IQueryFilter queryFilter)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for RowCount().");
			}

			return BackingDataset.GetRowCount(queryFilter);
		}

		public ICursor Search(IQueryFilter queryFilter, bool recycling)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for Search().");
			}

			IEnumerable<IRow> rows = BackingDataset.Search(queryFilter, recycling);

			return new CursorImpl(this, rows);
		}

		ICursor ITable.Update(IQueryFilter queryFilter, bool recycling)
		{
			throw new NotImplementedException();
		}

		ICursor ITable.Insert(bool useBuffering)
		{
			throw new NotImplementedException();
		}

		ISelectionSet ITable.Select(IQueryFilter queryFilter, esriSelectionType selType,
		                            esriSelectionOption selOption,
		                            IWorkspace selectionContainer)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region ISubtypes Members

		public void AddSubtype(int subtypeCode, string subtypeName)
		{
			throw new NotImplementedException();
		}

		public void DeleteSubtype(int subtypeCode)
		{
			throw new NotImplementedException();
		}

		public bool HasSubtype { get; set; }

		int ISubtypes.DefaultSubtypeCode { get; set; }

		public string SubtypeFieldName { get; set; }

		int ISubtypes.SubtypeFieldIndex
		{
			get
			{
				if (string.IsNullOrEmpty(SubtypeFieldName))
				{
					return -1;
				}

				return FindField(SubtypeFieldName);
			}
		}

		IEnumSubtype ISubtypes.Subtypes => throw new NotImplementedException();

		object ISubtypes.get_DefaultValue(int subtypeCode, string fieldName)
		{
			throw new NotImplementedException();
		}

		void ISubtypes.set_DefaultValue(int subtypeCode, string fieldName, object value)
		{
			throw new NotImplementedException();
		}

		IDomain ISubtypes.get_Domain(int subtypeCode, string fieldName)
		{
			throw new NotImplementedException();
		}

		void ISubtypes.set_Domain(int subtypeCode, string fieldName, IDomain domain)
		{
			throw new NotImplementedException();
		}

		string ISubtypes.get_SubtypeName(int subtypeCode)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Nested class CursorImpl

		protected class CursorImpl : ICursor
		{
			private readonly IEnumerator<IRow> _rowEnumerator;

			public CursorImpl(ITable table, IEnumerable<IRow> rows)
			{
				Fields = table.Fields;

				_rowEnumerator = rows.GetEnumerator();
			}

			public IFields Fields { get; }

			public int FindField(string name)
			{
				return Fields.FindField(name);
			}

			public IRow NextRow()
			{
				return _rowEnumerator.MoveNext() ? _rowEnumerator.Current : null;
			}

			public void UpdateRow(IRow row)
			{
				throw new NotImplementedException();
			}

			public void DeleteRow()
			{
				throw new NotImplementedException();
			}

			public object InsertRow(IRowBuffer buffer)
			{
				throw new NotImplementedException();
			}

			public void Flush()
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region Nested class GdbTableName

		private class GdbTableName : IName, IDatasetName, IObjectClassName, ITableName
		{
			private readonly GdbTable _table;

			public GdbTableName(GdbTable table)
			{
				_table = table;
				Name = table.Name;

				IWorkspace workspace = ((IDataset) _table).Workspace;
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

			public esriDatasetType Type => _table.Type;

			public string Category { get; set; }

			public IWorkspaceName WorkspaceName { get; set; }

			public IEnumDatasetName SubsetNames => throw new NotImplementedException();

			#endregion

			public int ObjectClassID => _table.ObjectClassID;
		}

		#endregion
	}
}

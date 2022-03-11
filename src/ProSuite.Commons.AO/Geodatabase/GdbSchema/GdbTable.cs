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
	public class GdbTable : VirtualTable, IEquatable<IObjectClass>
	{
		private const string _defaultOidFieldName = "OBJECTID";

		private int _lastUsedOid;
		private readonly IWorkspace _workspace;

		private IName _fullName;
		private bool _hasOID;
		private string _oidFieldName;

		private static int _nextObjectClassId;

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
										[CanBeNull] Func<GdbTable, BackingDataset> createBackingDataset = null,
										[CanBeNull] IWorkspace workspace = null)
				: base(name)
		{
			if (objectClassId > 0)
			{
				ObjectClassID = objectClassId;
			}
			else
			{
				// TODO: this is a workaround for default GdbWorkspace-instances are considered equal in some cases
				// TODO how should GdbWorkspaces be distinguished ? (BackingWorkspace?)
				_nextObjectClassId++;
				ObjectClassID = _nextObjectClassId;
			}

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

		protected virtual VirtualRow CreateObject(int oid)
		{
			return new GdbRow(oid, this);
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
						 WorkspaceUtils.IsSameWorkspace(_workspace, ((IDataset)other).Workspace,
																						WorkspaceComparison.Exact);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			if (ReferenceEquals(this, obj)) return true;

			if (obj.GetType() != GetType()) return false;

			return Equals((GdbTable)obj);
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

		public override bool IsBeingEdited()
		{
			return !(_workspace is IWorkspaceEdit workspaceEdit) || workspaceEdit.IsBeingEdited();
		}

		#endregion

		#region IClass members

		public override int AddFieldT(IField field)
		{
			int i = base.AddFieldT(field);

			if (field.Type == esriFieldType.esriFieldTypeOID)
			{
				// Probably the same logic as AO (query) classes:
				// The last one to be added determines the OID field
				_hasOID = true;
				_oidFieldName = field.Name;
			}

			FieldAddedCore(field);
			return i;
		}

		public override bool HasOID => _hasOID;

		public override string OIDFieldName => _oidFieldName;
		#endregion

		#region IObjectClass members

		public override int ObjectClassID { get; }

		public override string AliasName { get; }

		#endregion

		#region IDataset Members

		public override bool CanCopy()
		{
			return false;
		}

		public override bool CanDelete()
		{
			return false;
		}

		public override IName FullName
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

		public override IWorkspace Workspace => _workspace;

		#endregion

		#region ITable members

		public override VirtualRow CreateRow()
		{
			return CreateObject(GetNextOid());
		}

		public override IReadOnlyRow GetReadOnlyRow(int id)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for GetRow().");
			}

			return BackingDataset?.GetRow(id);
		}

		public override IRowBuffer CreateRowBuffer()
		{
			return CreateRow();
		}

		public override int RowCount(IQueryFilter queryFilter)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for RowCount().");
			}

			return BackingDataset.GetRowCount(queryFilter);
		}

		public override CursorImpl SearchT(IQueryFilter queryFilter, bool recycling)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for Search().");
			}

			IEnumerable<IReadOnlyRow> rows = BackingDataset.Search(queryFilter, recycling);

			return new CursorImpl(this, rows);
		}

		#endregion

		#region ISubtypes Members

		private bool _hasSubtype;
		public override bool HasSubtype => _hasSubtype;

		public override int DefaultSubtypeCode { get; set; }

		public override string SubtypeFieldName { get; set; }

		public override int SubtypeFieldIndex
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

		#endregion

		#region Nested class CursorImpl

		protected class CursorImpl_ : ICursor
		{
			private readonly IEnumerator<IRow> _rowEnumerator;

			public CursorImpl_(ITable table, IEnumerable<IRow> rows)
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

				IWorkspace workspace = ((IDataset)_table).Workspace;
				WorkspaceName = (IWorkspaceName)((IDataset)workspace).FullName;
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

			public esriDatasetType Type => _table.DatasetType;

			public string Category { get; set; }

			public IWorkspaceName WorkspaceName { get; set; }

			public IEnumDatasetName SubsetNames => throw new NotImplementedException();

			#endregion

			public int ObjectClassID => _table.ObjectClassID;
		}

		#endregion
	}
}

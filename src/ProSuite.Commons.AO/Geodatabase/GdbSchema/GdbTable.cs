using System;
using System.Collections.Generic;
using System.Data;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Gdb IObjectClass implementation that can be instantiated in memory that typically
	/// represents an existing table or feature class on the client. Its parent workspace
	/// can be null, a fake <see cref="GdbWorkspace"/> or a real workspace. An optionally
	/// provided  <see cref="BackingDataset"/> allows for actual data-access, such as GetRow()
	/// or Search().
	/// </summary>
	public class GdbTable : VirtualTable
	{
		private const string _defaultOidFieldName = "OBJECTID";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private int _lastUsedOid;
		private readonly IWorkspace _workspace;

		private IName _fullName;

		private string _oidFieldName;

		// TODO: switch to long in RO-table, use separate range (start at Int64.MinValue?)
		private static int _nextObjectClassId;

		/// <summary>
		///     Initializes a new instance of the <see cref="GdbTable" /> class.
		/// </summary>
		/// <param name="objectClassId">The object class id. Specify null to have a process-wide
		/// unique id assigned.</param>
		/// <param name="name">The name.</param>
		/// <param name="aliasName">The alias name of the object class.</param>
		/// <param name="createBackingDataset">The factory method that creates the backing dataset.</param>
		/// <param name="workspace"></param>
		public GdbTable(int? objectClassId,
		                [NotNull] string name,
		                [CanBeNull] string aliasName = null,
		                [CanBeNull] Func<GdbTable, BackingDataset> createBackingDataset = null,
		                [CanBeNull] IWorkspace workspace = null)
			: base(name)
		{
			// NOTE: Do not use -1 as 'special number' because the client could use negative numbers,
			// such as Pro class handles as ObjectClassId. We should not restrict the range to positive
			// numbers only.
			// Additionally, the doc states:
			// Those feature classes and tables that are in the database, but not registered with
			// the geodatabase will always have an object class ID of -1.
			if (objectClassId != null)
			{
				ObjectClassID = objectClassId.Value;
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

		public GdbTable(ITable template, bool useTemplateForQuerying = false)
			: this(GetObjectClassId(template), DatasetUtils.GetName(template),
			       GetAliasName(template), null, DatasetUtils.GetWorkspace(template))
		{
			for (int i = 0; i < template.Fields.FieldCount; i++)
			{
				IField field = template.Fields.Field[i];
				AddField(field);
			}

			if (useTemplateForQuerying)
			{
				BackingDataset = new BackingTable(template, this);
			}
		}

		[CanBeNull]
		public BackingDataset BackingDataset { get; }

		#region Non-public members

		protected int GetNextOid()
		{
			return ++_lastUsedOid;
		}

		/// <summary>
		/// Create a row with a pre-determined OID. Calling this method as opposed to the
		/// GdbRow constructor allows for certain optimizations.
		/// </summary>
		/// <param name="oid"></param>
		/// <param name="valueList"></param>
		/// <returns></returns>
		public virtual GdbRow CreateObject(int oid,
		                                   [CanBeNull] IValueList valueList = null)
		{
			return new GdbRow(oid, this, valueList);
		}

		/// <summary>
		/// Create a row without OID (and hence provided by internal sequence) but with a list of
		/// values.
		/// </summary>
		/// <param name="withValues"></param>
		/// <returns></returns>
		public GdbRow CreateObject(IValueList withValues)
		{
			return CreateObject(GetNextOid(), withValues);
		}

		protected virtual void FieldAddedCore(IField field) { }

		#endregion

		public int OidFieldIndex { get; private set; }

		public void SetOIDFieldName(string fieldName)
		{
			_oidFieldName = fieldName;
			OidFieldIndex = FindField(_oidFieldName);
			Assert.False(OidFieldIndex < 0, "OID field does not exist");
		}

		private static string GetAliasName(ITable template)
		{
			if (template is IObjectClass objectClass)
			{
				return DatasetUtils.GetAliasName(objectClass);
			}

			return null;
		}

		private static int GetObjectClassId(ITable template)
		{
			if (template is IObjectClass objectClass)
			{
				return objectClass.ObjectClassID;
			}

			return -1;
		}

		#region VirtualTable overrides

		public override IEnumerable<IReadOnlyRow> EnumReadOnlyRows(
			IQueryFilter queryFilter, bool recycling)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for Search().");
			}

			try
			{
				return BackingDataset.Search(queryFilter, recycling);
			}
			catch (Exception e)
			{
				// Due to the possibility of nested tables, add the name to the exception: 
				throw new DataException($"Error getting rows from {Name}", e);
			}
		}

		#endregion

		#region IDatasetEdit Member

		public override bool IsBeingEdited()
		{
			return ! (_workspace is IWorkspaceEdit workspaceEdit) || workspaceEdit.IsBeingEdited();
		}

		#endregion

		#region IClass members

		public override int AddFieldT(IField field)
		{
			int i = base.AddFieldT(field);

			// Do not overwrite previous a value that could have been set explicitly!
			if (_oidFieldName == null && field.Type == esriFieldType.esriFieldTypeOID)
			{
				// If nothing was set, the first one to be added determines the OID field.
				SetOIDFieldName(field.Name);
			}

			FieldAddedCore(field);
			return i;
		}

		public override bool HasOID => _oidFieldName != null;

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

		public override IRow GetRow(int OID)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for GetRow().");
			}

			return BackingDataset.GetRow(OID);
		}

		public override IReadOnlyRow GetReadOnlyRow(int id)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for GetRow().");
			}

			return BackingDataset.GetRow(id);
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

			IEnumerable<VirtualRow> rows = BackingDataset.Search(queryFilter, recycling);

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

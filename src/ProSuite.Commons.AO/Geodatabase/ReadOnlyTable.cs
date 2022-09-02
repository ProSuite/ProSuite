using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyTable : IReadOnlyTable, ISubtypes
	{
		public static IEnumerable<IReadOnlyRow> EnumRows(IEnumerable<IRow> rows)
		{
			ITable current = null;
			ReadOnlyTable table = null;
			foreach (var row in rows)
			{
				ITable t = row.Table;
				if (t != current)
				{
					table = CreateReadOnlyTable(row.Table);
					current = t;
				}

				yield return table.CreateRow(row);
			}
		}

		protected static ReadOnlyTable CreateReadOnlyTable(ITable table)
		{
			return new ReadOnlyTable(table);
		}

		private readonly ITable _table;

		private readonly string
			_name; // cache name for debugging purposes (avoid all ArcObjects threading issues)

		protected ReadOnlyTable([NotNull] ITable table)
		{
			_table = table;
			_name = DatasetUtils.GetName(_table);
		}

		public override string ToString() => $"{_name} ({GetType().Name})";

		public ITable BaseTable => _table;
		protected ITable Table => _table;
		IName IReadOnlyDataset.FullName => ((IDataset) _table).FullName;
		IWorkspace IReadOnlyDataset.Workspace => ((IDataset) _table).Workspace;
		public string Name => DatasetUtils.GetName(_table);
		public IFields Fields => _table.Fields;

		public int FindField(string name) => _table.FindField(name);

		public bool HasOID => _table.HasOID;
		public string OIDFieldName => _table.OIDFieldName;

		public IReadOnlyRow GetRow(int oid) => CreateRow(_table.GetRow(oid));

		public int RowCount(IQueryFilter filter) => _table.RowCount(filter);

		public bool Equals(IReadOnlyTable otherTable)
		{
			return Equals((object) otherTable);
		}

		public virtual ReadOnlyRow CreateRow(IRow row)
		{
			return new ReadOnlyRow(this, row);
		}

		public IEnumerable<IReadOnlyRow> EnumRows(IQueryFilter filter, bool recycle)
		{
			foreach (var row in new EnumCursor(_table, filter, recycle))
			{
				yield return CreateRow(row);
			}
		}

		private ISubtypes _subtypes => _table as ISubtypes;

		void ISubtypes.AddSubtype(int SubtypeCode, string SubtypeName)
		{
			_subtypes.AddSubtype(SubtypeCode, SubtypeName);
		}

		void ISubtypes.DeleteSubtype(int SubtypeCode)
		{
			_subtypes.DeleteSubtype(SubtypeCode);
		}

		bool ISubtypes.HasSubtype => _subtypes?.HasSubtype ?? false;

		int ISubtypes.DefaultSubtypeCode
		{
			get => _subtypes.DefaultSubtypeCode;
			set => _subtypes.DefaultSubtypeCode = value;
		}

		object ISubtypes.get_DefaultValue(int subtypeCode, string fieldName)
			=> _subtypes.DefaultValue[subtypeCode, fieldName];

		void ISubtypes.set_DefaultValue(int subtypeCode, string fieldName, object value)
			=> _subtypes.DefaultValue[subtypeCode, fieldName] = value;

		IDomain ISubtypes.get_Domain(int subtypeCode, string fieldName)
			=> _subtypes.Domain[subtypeCode, fieldName];

		void ISubtypes.set_Domain(int subtypeCode, string fieldName, IDomain value)
			=> _subtypes.DefaultValue[subtypeCode, fieldName] = value;

		string ISubtypes.SubtypeFieldName
		{
			get => _subtypes.SubtypeFieldName;
			set => _subtypes.SubtypeFieldName = value;
		}

		int ISubtypes.SubtypeFieldIndex => _subtypes.SubtypeFieldIndex;

		string ISubtypes.get_SubtypeName(int subtypeCode)
			=> _subtypes.SubtypeName[subtypeCode];

		IEnumSubtype ISubtypes.Subtypes => _subtypes.Subtypes;

		#region Equality members

		public bool Equals([NotNull] GdbTable other)
		{
			IWorkspace workspace = ((IDataset) _table).Workspace;

			if (! Equals(workspace, other.Workspace))
			{
				return false;
			}

			if (_table is IObjectClass baseObjectClass)
			{
				return baseObjectClass.ObjectClassID == other.ObjectClassID;
			}

			// Same workspace, potentially 'virtual' classes;
			return Name.Equals(other.Name);
		}

		public bool Equals(ReadOnlyTable other)
		{
			if (other == null)
			{
				return false;
			}

			if (_table is IObjectClass thisClass)
			{
				if (! (other._table is IObjectClass otherClass))
				{
					return false;
				}

				return DatasetUtils.IsSameObjectClass(thisClass, otherClass);
			}

			if (_name != other._name)
			{
				return false;
			}

			return DatasetUtils.GetWorkspace(_table) == DatasetUtils.GetWorkspace(other._table);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			if (ReferenceEquals(this, obj)) return true;

			if (obj is GdbTable gdbTable)
			{
				return Equals(gdbTable);
			}

			if (_table is IObjectClass thisClass && obj is IObjectClass objectClass)
			{
				return DatasetUtils.IsSameObjectClass(thisClass, objectClass);
			}

			if (obj is ReadOnlyTable roTable)
			{
				return Equals(roTable);
			}

			return false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				IWorkspace workspace = ((IDataset) _table).Workspace;

				IObjectClass objectClass = _table as IObjectClass;

				int classHash = objectClass?.ObjectClassID ?? Name.GetHashCode();

				return (workspace.GetHashCode() * 397) ^ classHash;
			}
		}

		#endregion
	}
}

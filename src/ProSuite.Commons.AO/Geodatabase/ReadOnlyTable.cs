using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

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
		{ return new ReadOnlyTable(table); }

		private readonly ITable _table;
		protected ReadOnlyTable(ITable table)
		{
			_table = table;
		}

		public ITable BaseTable => _table;
		protected ITable Table => _table;
		ESRI.ArcGIS.esriSystem.IName IReadOnlyDataset.FullName => ((IDataset)_table).FullName;
		IWorkspace IReadOnlyDataset.Workspace => ((IDataset)_table).Workspace;
		public string Name => DatasetUtils.GetName(_table);
		public IFields Fields => _table.Fields;
		public int FindField(string name) => _table.FindField(name);
		public bool HasOID => _table.HasOID;
		public string OIDFieldName => _table.OIDFieldName;
		public IReadOnlyRow GetRow(int oid) => CreateRow(_table.GetRow(oid));
		public int RowCount(IQueryFilter filter) => _table.RowCount(filter);

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

		private ISubtypes _subtypes => (ISubtypes)_table;

		void ISubtypes.AddSubtype(int SubtypeCode, string SubtypeName)
		{
			_subtypes.AddSubtype(SubtypeCode, SubtypeName);
		}

		void ISubtypes.DeleteSubtype(int SubtypeCode)
		{
			_subtypes.DeleteSubtype(SubtypeCode);
		}

		bool ISubtypes.HasSubtype => _subtypes.HasSubtype;

		int ISubtypes.DefaultSubtypeCode { get => _subtypes.DefaultSubtypeCode; set => _subtypes.DefaultSubtypeCode = value; }

		object ISubtypes.get_DefaultValue(int subtypeCode, string fieldName)
			=> _subtypes.DefaultValue[subtypeCode, fieldName];
		void ISubtypes.set_DefaultValue(int subtypeCode, string fieldName, object value)
			=> _subtypes.DefaultValue[subtypeCode, fieldName] = value;

		IDomain ISubtypes.get_Domain(int subtypeCode, string fieldName)
			=> _subtypes.Domain[subtypeCode, fieldName];
		void ISubtypes.set_Domain(int subtypeCode, string fieldName, IDomain value)
			=> _subtypes.DefaultValue[subtypeCode, fieldName] = value;

		string ISubtypes.SubtypeFieldName { get => _subtypes.SubtypeFieldName; set => _subtypes.SubtypeFieldName = value; }

		int ISubtypes.SubtypeFieldIndex => _subtypes.SubtypeFieldIndex;

		string ISubtypes.get_SubtypeName(int subtypeCode)
			=> _subtypes.SubtypeName[subtypeCode];

		IEnumSubtype ISubtypes.Subtypes => _subtypes.Subtypes;
	}
}

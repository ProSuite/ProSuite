using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyRow : IReadOnlyRow
	{
		public static ReadOnlyFeature Create(IFeature row)
		{
			ReadOnlyTable tbl = ReadOnlyTableFactory.Create(row.Table);
			ReadOnlyFeatureClass fc = (ReadOnlyFeatureClass) tbl;
			return new ReadOnlyFeature(fc, row);
		}

		public static ReadOnlyRow Create(IRow row)
		{
			ReadOnlyTable tbl = ReadOnlyTableFactory.Create(row.Table);
			if (tbl is ReadOnlyFeatureClass fc)
			{
				return new ReadOnlyFeature(fc, (IFeature) row);
			}

			return new ReadOnlyRow(tbl, row);
		}

		public ReadOnlyRow(ReadOnlyTable table, IRow row)
		{
			Table = table;
			Row = row;
		}

		public IRow BaseRow => Row;
		protected IRow Row { get; }
		public bool HasOID => Table.AlternateOidFieldName != null || Row.HasOID;
		public int OID => Table.GetRowOid(Row);

		public object get_Value(int field) => Row.Value[field];

		IReadOnlyTable IReadOnlyRow.Table => Table;
		public ReadOnlyTable Table { get; }
	}
}

using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyRow : IReadOnlyRow, IUniqueIdObject, IUniqueIdObjectEdit
	{
		public static ReadOnlyFeature Create(IFeature row)
		{
			ReadOnlyTable tbl = ReadOnlyTableFactory.Create(row.Table);
			ReadOnlyFeatureClass fc = (ReadOnlyFeatureClass) tbl;
			return ReadOnlyFeature.Create(fc, row);
		}

		public static ReadOnlyRow Create(IRow row)
		{
			ReadOnlyTable tbl = ReadOnlyTableFactory.Create(row.Table);
			if (tbl is ReadOnlyFeatureClass fc)
			{
				return ReadOnlyFeature.Create(fc, (IFeature) row);
			}

			return new ReadOnlyRow(tbl, row);
		}

		public ReadOnlyRow(ReadOnlyTable table, IRow row)
		{
			Table = table;
			Row = row;
		}

		public UniqueId UniqueId { get; set; }

		public IRow BaseRow => Row;
		protected IRow Row { get; }
		public bool HasOID => Table.AlternateOidFieldName != null || Row.HasOID;
		public long OID => Table.GetRowOid(Row);

		public object get_Value(int field)
		{
			object result = Row.Value[field];

#if !ARCGIS_11_0_OR_GREATER
			// NOTE: IReadOnly has long OIDs, IRow has int OID in 10.x
			// Ensure correct return type because comparisons as object
			// are Int32.Equals(Int64) which results in false where
			// the OIDs were actually equal.
			if (result is int intValue && field == Table.OidFieldIndex)
			{
				result = (long) intValue;
			}
#endif
			return result;
		}

		public override string ToString() => $"{Table.Name}; OID:{OID}";

		IReadOnlyTable IReadOnlyRow.Table => Table;

		public ReadOnlyTable Table { get; }

		ITableData IDbRow.DbTable => Table;

		object IDbRow.GetValue(int index)
		{
			return get_Value(index);
		}
	}
}

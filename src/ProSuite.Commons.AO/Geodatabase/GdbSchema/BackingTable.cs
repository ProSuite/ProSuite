using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class BackingTable : BackingDataset
	{
		private readonly ITable _backingTable;
		private readonly GdbTable _gdbTable;
		private readonly int _oidFieldIndex;

		public BackingTable([NotNull] ITable backingTable,
		                    [NotNull] GdbTable gdbTable)
		{
			_backingTable = backingTable;
			_gdbTable = gdbTable;

			_oidFieldIndex = backingTable.HasOID
				                 ? backingTable.FindField(backingTable.OIDFieldName)
				                 : -1;
		}

		public override IEnvelope Extent =>
			_backingTable is IGeoDataset geoDataset ? geoDataset.Extent : null;

		public override VirtualRow GetRow(long id)
		{
#if ARCGIS_11_0_OR_GREATER
			var row = _backingTable.GetRow(id);
#else
			var row = _backingTable.GetRow((int) id);
#endif

			return CreateRow(row);
		}

		public override long GetRowCount(ITableFilter filter)
		{
			IQueryFilter qf = TableFilterUtils.GetQueryFilter(filter);
			return _backingTable.RowCount(qf);
		}

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
		{
			IQueryFilter qf = TableFilterUtils.GetQueryFilter(filter);
			foreach (IRow row in GdbQueryUtils.GetRows(_backingTable, qf, recycling))
			{
				yield return CreateRow(row);
			}
		}

		private GdbRow CreateRow(IRow baseRow)
		{
			var rowValueList = new RowBasedValues(baseRow, _oidFieldIndex);

			long oid = baseRow.HasOID ? baseRow.OID : -1;

			return _gdbTable.CreateObject(oid, rowValueList);
		}
	}
}

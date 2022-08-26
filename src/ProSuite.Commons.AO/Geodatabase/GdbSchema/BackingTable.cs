using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
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

		public override VirtualRow GetRow(int id)
		{
			var row = _backingTable.GetRow(id);

			return CreateRow(row);
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			return _backingTable.RowCount(queryFilter);
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			foreach (IRow row in GdbQueryUtils.GetRows(_backingTable, filter, recycling))
			{
				yield return CreateRow(row);
			}
		}

		private VirtualRow CreateRow(IRow baseRow)
		{
			var rowValueList = new RowBasedValues(baseRow, _oidFieldIndex);

			return new GdbRow(baseRow.OID, _gdbTable, rowValueList);
		}
	}
}

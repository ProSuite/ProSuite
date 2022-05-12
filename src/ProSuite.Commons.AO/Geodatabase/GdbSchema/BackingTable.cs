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

		public BackingTable([NotNull] ITable backingTable,
		                    [NotNull] GdbTable gdbTable)
		{
			_backingTable = backingTable;
			_gdbTable = gdbTable;
		}

		public override IEnvelope Extent =>
			_backingTable is IGeoDataset geoDataset ? geoDataset.Extent : null;

		public override VirtualRow GetRow(int id)
		{
			var row = _backingTable.GetRow(id);
			return new GdbRow(row.OID, _gdbTable, new RowBasedValues(row));
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			return _backingTable.RowCount(queryFilter);
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			foreach (IRow row in GdbQueryUtils.GetRows(_backingTable, filter, recycling))
			{
				IValueList values = new RowBasedValues(row);

				yield return new GdbRow(row.OID, _gdbTable, values);
			}
		}
	}
}

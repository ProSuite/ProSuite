using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container.PolygonGrower
{
	public sealed class TableIndexRow : ITableIndexRow
	{
		public TableIndexRow([NotNull] IRow row, int tableIndex)
		{
			Row = row;
			TableIndex = tableIndex;
		}

		[NotNull]
		public IRow Row { get; }

		IRow ITableIndexRow.GetRow(IList<ITable> tables)
		{
			return Row;
		}

		IRow ITableIndexRow.CachedRow => Row;

		public int RowOID
		{
			get
			{
				var uniqueIdObject = Row as IUniqueIdObject;
				return uniqueIdObject?.UniqueId?.Id ?? Row.OID;
			}
		}

		public int TableIndex { get; }

		public override string ToString()
		{
			return $"OID:{Row.OID}; T:{TableIndex}";
		}
	}
}

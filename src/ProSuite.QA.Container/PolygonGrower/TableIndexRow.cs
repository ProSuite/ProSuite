using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container.PolygonGrower
{
	public sealed class TableIndexRow : ITableIndexRow
	{
		public TableIndexRow([NotNull] IReadOnlyRow row, int tableIndex)
		{
			Row = row;
			TableIndex = tableIndex;
		}

		[NotNull]
		public IReadOnlyRow Row { get; }

		IReadOnlyRow ITableIndexRow.GetRow(IList<IReadOnlyTable> tables)
		{
			return Row;
		}

		IReadOnlyRow ITableIndexRow.CachedRow => Row;

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

using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public interface ITableIndexRow
	{
		long RowOID { get; }

		int TableIndex { get; }

		[NotNull]
		IReadOnlyRow GetRow([NotNull] IList<IReadOnlyTable> tableIndexTables);

		[CanBeNull]
		IReadOnlyRow CachedRow { get; }
	}
}

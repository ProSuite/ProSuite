using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public interface ITableIndexRow
	{
		int RowOID { get; }

		int TableIndex { get; }

		[NotNull]
		IRow GetRow([NotNull] IList<ITable> tableIndexTables);

		[CanBeNull]
		IRow CachedRow { get; }
	}
}

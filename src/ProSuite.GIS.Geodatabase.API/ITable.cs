using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface ITable : IClass
	{
		IRow CreateRow();

		IRow GetRow(long oid);

		IEnumerable<IRow> GetRows(object oids, bool recycling);

		IRowBuffer CreateRowBuffer();

		void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer);

		void DeleteSearchedRows(IQueryFilter queryFilter);

		long RowCount(IQueryFilter queryFilter);

		IEnumerable<IRow> Search(IQueryFilter queryFilter, bool recycling);

		IEnumerable<IRow> Update(IQueryFilter queryFilter, bool recycling);

		IEnumerable<IRow> Insert(bool useBuffering);

		ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer);
	}
}

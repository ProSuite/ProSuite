using System.Collections.Generic;
using ProSuite.Commons.GeoDb;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface ITable : IClass
	{
		IRow CreateRow(int? subtypeCode = null);

		IRow GetRow(long oid);

		IEnumerable<IRow> GetRows(object oids, bool recycling);

		IRowBuffer CreateRowBuffer();

		void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer);

		void DeleteSearchedRows(IQueryFilter queryFilter);

		long RowCount(ITableFilter filter);

		IEnumerable<IRow> Search(ITableFilter filter, bool recycling);

		IEnumerable<IRow> Update(IQueryFilter queryFilter, bool recycling);

		IEnumerable<IRow> Insert(bool useBuffering);

		ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer);
	}
}

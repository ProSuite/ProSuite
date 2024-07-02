
using ProSuite.Commons.Essentials.CodeAnnotations;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface ITable : IClass
	{
		IRow CreateRow();

		IRow GetRow(int oid);

		IEnumerable<IRow> GetRows(object oids, bool recycling);

		IRowBuffer CreateRowBuffer();

		void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer);

		void DeleteSearchedRows(IQueryFilter queryFilter);

		int RowCount(IQueryFilter queryFilter);

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

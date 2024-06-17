using System.Collections.Generic;

namespace ProSuite.Commons.GeoDb
{
	public interface ITableSchemaDef : IDatasetDef
	{
		IReadOnlyList<ITableField> TableFields { get; }

		bool HasOID { get; }

		string OIDFieldName { get; }

		int FindField(string fieldName);
	}

	public interface ITableData : ITableSchemaDef
	{
		IDbRow GetRow(long oid);

		IEnumerable<IDbRow> EnumRows(ITableFilter filter, bool recycle);

		long RowCount(ITableFilter filter);
	}
}

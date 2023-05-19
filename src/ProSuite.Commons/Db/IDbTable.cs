using System.Collections.Generic;

namespace ProSuite.Commons.Db
{
	public interface IDbTableSchema : IDbDataset
	{
		IReadOnlyList<ITableField> TableFields { get; }

		bool HasOID { get; }

		string OIDFieldName { get; }

		int FindField(string fieldName);
	}

	public interface IDbTable : IDbTableSchema
	{
		IDbRow GetRow(long oid);

		IEnumerable<IDbRow> EnumRows(ITableFilter filter, bool recycle);

		long RowCount(ITableFilter filter);
	}
}

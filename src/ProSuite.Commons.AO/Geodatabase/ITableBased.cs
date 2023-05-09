using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Optional interface for IReadOnlyTable implementations that are based on other tables, such as
	/// transformed tables or query tables.
	/// </summary>
	public interface ITableBased
	{
		IList<IReadOnlyTable> GetBaseTables();

		// TODO (for arbitrary number and source of base rows): Add something like:
		//public readonly record struct BaseRow(IReadOnlyTable table, long oid);
		//IEnumerable<BaseRow> GetBaseRowReferences(IReadOnlyRow forTransformedRow);
	}
}

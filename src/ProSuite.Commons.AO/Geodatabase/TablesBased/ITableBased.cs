using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase.TablesBased
{
	/// <summary>
	/// Optional interface for IReadOnlyTable implementations that are based on other tables, such as
	/// transformed tables or query tables.
	/// </summary>
	public interface ITableBased
	{
		/// <summary>
		/// Get the involved tables that are the base tables for this object.
		/// </summary>
		/// <returns></returns>
		IList<IReadOnlyTable> GetInvolvedTables();

		/// <summary>
		/// Get the involved rows on which the <see cref="forTransformedRow"/> is based.
		/// </summary>
		/// <param name="forTransformedRow"></param>
		/// <returns></returns>
		IEnumerable<Involved> GetInvolvedRows(IReadOnlyRow forTransformedRow);
	}
}

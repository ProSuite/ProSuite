using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IRowCache
{
	/// <summary>
	/// Indicates that the cache should be completely invalidated (i.e. rows should 
	/// be re-fetched), based on a given feature workspace.
	/// </summary>
	void Invalidate();

	/// <summary>
	/// Indicates that the specified tables should be completely invalidated, i.e. all
	/// objects from these tables should be re-fetched.
	/// </summary>
	/// <param name="tables">The list of tables containing changes.</param>
	void Invalidate(IEnumerable<Table> tables);

	/// <summary>
	/// Passes all changes that happened in an edit operation to the cache for processing.
	/// </summary>
	/// <param name="inserts">The inserted rows.</param>
	/// <param name="updates">The updated rows.</param>
	/// <param name="deletes">The deleted rows.</param>
	void ProcessChanges([NotNull] Dictionary<Table, List<long>> inserts,
	                    [NotNull] Dictionary<Table, List<long>> deletes,
	                    [NotNull] Dictionary<Table, List<long>> updates);

	/// <summary>
	/// Determines whether the object cache can contain the specified table. 
	/// This is used by the object cache synchronizer to ignore irrelevant change events.
	/// </summary>
	/// <param name="table">The table.</param>
	/// <returns>
	/// 	<c>true</c> if the object cache can contain the specified table; otherwise, <c>false</c>.
	///     Only changes for tables for which this returns <c>true</c> will be passed to
	///     <see cref="ProcessChanges"/>.
	/// </returns>
	/// <remarks>If this check is potentially expensive for a given object cache implementation, 
	/// <c>true</c> can be returned. In this case the implementation of <see cref="ProcessChanges"/> 
	/// should ignore any irrelevant instances.</remarks>
	bool CanContain([NotNull] Table table);
}

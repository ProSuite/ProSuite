using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }

		GdbTableIdentity TableIdentity { get; }

		IAttributeReader AttributeReader { get; set; }

		bool HasGeometry { get; }

		string DefinitionQuery { get; }

		bool Uses(ITableReference tableReference);

		/// <summary>
		/// Opens the table.
		/// </summary>
		/// <remarks>
		/// NOTE: This could be a stale table instance from a stale workspace instance.
		///       Do only use for read-only and don't if the geodatabase is being edited!
		/// </remarks>
		/// <typeparam name="T">ArcGIS.Core.Data.Table</typeparam>
		/// <returns></returns>
		T OpenDataset<T>() where T : Table;
		
		/// <summary>
		/// A table Id that is unique within the work list and that remains stable across sessions.
		/// </summary>
		/// <returns></returns>
		long GetUniqueTableId();

		void EnsureValidFilter(ref QueryFilter filter, WorkItemStatus? statusFilter,
		                       bool excludeGeometry);
	}
}

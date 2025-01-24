using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }

		GdbTableIdentity TableIdentity { get; }

		[CanBeNull]
		IAttributeReader AttributeReader { get; set; }

		bool HasGeometry { get; }

		string DefinitionQuery { get; }

		bool Uses(ITableReference tableReference);

		/// <summary>
		/// Opens the dataset for the table.
		/// NOTE: This could be a stale instance of the table, do not use if the geodatabase is
		/// being edited!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		T OpenDataset<T>() where T : Table;

		string CreateWhereClause(WorkItemStatus? statusFilter);

		/// <summary>
		/// A table Id that is unique within the work list and that remains stable across sessions.
		/// </summary>
		/// <returns></returns>
		long GetUniqueTableId();
	}
}

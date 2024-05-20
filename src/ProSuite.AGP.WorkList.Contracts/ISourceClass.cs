using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISourceClass
	{
		string Name { get; }

		[CanBeNull]
		IAttributeReader AttributeReader { get; set; }

		bool HasGeometry { get; }

		string DefinitionQuery { get; }

		bool Uses(GdbTableIdentity table);

		T OpenDataset<T>() where T : Table;

		string CreateWhereClause(WorkItemStatus? statusFilter);

		/// <summary>
		/// A table Id that is unique within the work list and that remains stable across sessions.
		/// </summary>
		/// <returns></returns>
		long GetUniqueTableId();
	}
}

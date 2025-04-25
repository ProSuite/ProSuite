using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// Interface that encapsulates the persistence of work items, i.e. both the access to the
	/// source classes in the geodatabase and the volatile state in the work list definition
	/// files.
	/// </summary>
	public interface IWorkItemRepository
	{
		IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
			QueryFilter filter = null,
			WorkItemStatus? statusFilter = null,
			bool recycle = true,
			bool excludeGeometry = false);

		IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
			Table table,
			QueryFilter filter = null,
			WorkItemStatus? statusFilter = null,
			bool recycle = true,
			bool excludeGeometry = false);

		IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItemsCore(
			QueryFilter filter, bool excludeGeometry);

		void UpdateState(IWorkItem item);

		void Commit();

		void SetCurrentIndex(int currentIndex);

		int GetCurrentIndex();

		void SetVisited(IWorkItem item);

		Task SetStatusAsync(IWorkItem item, WorkItemStatus status);

		IList<ISourceClass> SourceClasses { get; }

		IWorkItemStateRepository WorkItemStateRepository { get; }

		/// <summary>
		/// Update the table schema once the domain information is available. This only necessary
		/// if the field values of the source classes depend on the DDX attribute roles instead of
		/// being hard-coded.
		/// </summary>
		/// <param name="tableSchemaInfo"></param>
		void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo);

		bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema);

		Row GetSourceRow([NotNull] ISourceClass sourceClass, long oid);

		long GetNextOid();
	}
}

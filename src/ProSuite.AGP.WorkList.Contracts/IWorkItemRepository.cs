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
		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true);

		IEnumerable<IWorkItem> GetItems([CanBeNull] Geometry areaOfInterest,
		                                WorkItemStatus? statusFilter,
		                                bool recycle = true);

		void Refresh(IWorkItem item);

		void UpdateVolatileState(IEnumerable<IWorkItem> items);

		void Commit();

		void SetCurrentIndex(int currentIndex);

		int GetCurrentIndex();

		void SetVisited(IWorkItem item);

		Task SetStatusAsync(IWorkItem item, WorkItemStatus status);

		List<ISourceClass> SourceClasses { get; }

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

		void RefreshGeometry(IWorkItem item);
	}
}

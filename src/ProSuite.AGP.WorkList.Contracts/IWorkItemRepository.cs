using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemRepository
	{
		int GetCount(QueryFilter filter = null);

		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true);

		IEnumerable<IWorkItem> GetItems([CanBeNull] Geometry areaOfInterest,
		                                WorkItemStatus? statusFilter,
		                                bool recycle = true);

		void Refresh(IWorkItem item);

		Task UpdateAsync(IWorkItem item);

		void UpdateVolatileState(IEnumerable<IWorkItem> items);

		void Commit();

		void Discard();

		// todo daro: is this the right way?
		void SetCurrentIndex(int currentIndex);

		int GetCurrentIndex();

		void SetVisited(IWorkItem item);

		Task SetStatus(IWorkItem item, WorkItemStatus status);

		void UpdateStateRepository(string path);

		List<ISourceClass> SourceClasses { get; }
	}
}
